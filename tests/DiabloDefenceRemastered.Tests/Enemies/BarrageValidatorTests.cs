using DiabloDefenceRemastered.Core.Sim.Enemies;
using DiabloDefenceRemastered.Core.Sim.Enemies.Parts;
using static DiabloDefenceRemastered.Tests.Enemies.BossKit;
using static DiabloDefenceRemastered.Tests.Enemies.Wp4Defs;

namespace DiabloDefenceRemastered.Tests.Enemies;

/// <summary>
/// 보스 탄막 검사(설계서 6.20 ②③⑧ — 13.5-13 보탬): 탄막 예고 &lt; 0.4, 순서 패턴에 조건 키, 조건 없는 패턴이 그 페이즈 순서에 없음, 없는 순서 ID,
/// 한 패턴에 <c>wall</c> 둘, <c>dash</c>·<c>teleport</c> 가 마지막이 아니거나 다른 여러 틱 동작과 같이 있음, <c>teleport.appear</c> &lt; 0.4 — 각각 오류 하나.
/// </summary>
public sealed class BarrageValidatorTests(ITestOutputHelper output)
{
    private static readonly PartDef RingAct = Barrage(Ring(8));
    private static readonly PartDef SpiralAct = Barrage(Ring(2) with { Waves = 5, Interval = 0.2f });
    private static readonly PartDef DashAct = A("dash", new DashParams { Speed = 400f, Time = 0.4f, Damage = 25f });
    private static readonly PartDef WallAct = Barrage(new BarrageParams
    {
        Shape = BarrageShape.Wall, Projectile = DefRef<ProjectileDef>.To(Orb), Waves = 2, Interval = 1f, Wall = new BarrageWallParams(),
    });

    private IReadOnlyList<DataError> Errors(EnemyDef def)
    {
        var defs = Defs([def]);
        var ctx = new ValidationContext { Defs = defs };
        new EnemyDataValidator().Validate(defs, ctx);
        foreach (var e in ctx.Errors) output.WriteLine(e.ToString());
        return [.. ctx.Errors.Where(e => e.Severity == DataSeverity.Error && e.Message.Contains($"'{def.Id}'", StringComparison.Ordinal))];
    }

    private static EnemyDef Boss(SequenceDef? seq, params PatternDef[] patterns) =>
        Enemy("val_boss") with { Rank = DefRank.Boss, Radius = 30f, Patterns = patterns, Sequence = seq };

    [Fact]
    public void Clean_boss_has_no_errors()
    {
        var slam = Cooled("slam", 2f, null, A("melee_slam", new MeleeSlamParams { Damage = 10f })) with { RangeBand = [0f, 90f] };
        Assert.Empty(Errors(Boss(Seq(1f, ("a", 0.5f), ("d", 1f)), slam, Pattern("a", Circle, RingAct, SpiralAct), Pattern("d", Line(0.6f), DashAct))));
    }

    [Fact]
    public void Barrage_without_telegraph_is_error()
    {
        var e = Assert.Single(Errors(Boss(null, Cooled("a", 2f, null, RingAct))));
        Assert.Contains("barrage 예고", e.Message);
        // 0.3s 도 모자람
        Assert.Single(Errors(Boss(null, Cooled("a", 2f, Circle with { Time = 0.3f }, RingAct))));
    }

    [Fact]
    public void Sequence_pattern_with_condition_key_is_error()
    {
        var e = Assert.Single(Errors(Boss(Seq(0f, ("a", 0.5f)), Pattern("a", Circle, RingAct) with { Cooldown = 3f })));
        Assert.Contains("조건 키", e.Message);
    }

    [Fact]
    public void Unconditioned_pattern_outside_sequence_is_error()
    {
        var e = Assert.Single(Errors(Boss(Seq(0f, ("a", 0.5f)), Pattern("a", Circle, RingAct), Pattern("b", Circle, RingAct))));
        Assert.Contains("'b'", e.Message);
    }

    [Fact]
    public void Missing_sequence_id_is_error()
    {
        var e = Assert.Single(Errors(Boss(Seq(0f, ("a", 0.5f), ("nope", 0.5f)), Pattern("a", Circle, RingAct))));
        Assert.Contains("'nope'", e.Message);
    }

    [Fact]
    public void Two_walls_in_one_pattern_is_error()
    {
        var e = Assert.Single(Errors(Boss(Seq(0f, ("a", 0.5f)), Pattern("a", Circle, WallAct, WallAct))));
        Assert.Contains("wall", e.Message);
    }

    [Fact]
    public void Dash_not_last_or_with_other_multi_tick_is_error()
    {
        var notLast = Errors(Boss(Seq(0f, ("d", 0.5f)), Pattern("d", Line(0.6f), DashAct, RingAct)));
        Assert.Contains(notLast, e => e.Message.Contains("마지막에만"));
        var together = Errors(Boss(Seq(0f, ("d", 0.5f)), Pattern("d", Line(0.6f), SpiralAct, DashAct)));
        Assert.Contains(together, e => e.Message.Contains("같이 둘 수 없다"));
    }

    [Fact]
    public void Teleport_appear_short_is_error()
    {
        var blink = A("teleport", new TeleportParams { Appear = 0.3f });
        var e = Assert.Single(Errors(Boss(Seq(0f, ("t", 0f)), Pattern("t", null, blink))));
        Assert.Contains("appear", e.Message);
    }
}
