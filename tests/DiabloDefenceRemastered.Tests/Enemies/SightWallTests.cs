using DiabloDefenceRemastered.Core.Sim.Enemies;
using DiabloDefenceRemastered.Core.Sim.Enemies.Parts;
using DiabloDefenceRemastered.Tests.Sim;
using static DiabloDefenceRemastered.Tests.Enemies.Wp4Defs;

namespace DiabloDefenceRemastered.Tests.Enemies;

/// <summary>
/// 시야 조건의 벽 뒤 경우 (설계서 10.1 <c>Enemies</c> v0.5.1): <b>시야만 막는 <c>L</c> 줄</b>(걷기·투사체는 트임) 뒤의 궁수·돌진 적은
/// 쏘거나 돌진을 시작하지 않고, 시야를 끈 같은 배치(대조군)에서는 한다 — <c>X</c> 벽이면 투사체 선·걷기 선이 먼저 막아 시야 조건을 빼먹어도 통과하므로 <c>L</c> 로 짠다.
/// 맵 M 20(N 40), L 줄 = 메가 (6, 8)–(14, 8)(하위 칸 y 16 줄), 주인공 칸 (20, 20), 적 칸 (20, 12) — 286px, 빛 420(R − 40 = 380 안이라 벽만이 가림).
/// </summary>
public sealed class SightWallTests(ITestOutputHelper output)
{
    private const int Mega = 20, WallRow = 16;
    private static readonly PiecePlacementDef LRow = new() { Piece = "sight_only", Line = [new(6, 8), new(14, 8)] };
    private static readonly ProjectileDef Arrow = new() { Id = "pj_wall", Sprite = DefRef<SpriteDef>.To(TestDefs.Sprite("arrow")), Speed = 200f };
    private static readonly TelegraphDef Tel = new() { Effect = DefRef<EffectDef>.To(new EffectDef { Id = "fx_wall" }), Time = 0.5f };

    private static readonly EnemyDef Archer = Enemy("wall_archer",
        B("kite", new KiteParams { RangeBand = [150f, 250f] }),
        B("shoot_projectile", new ShootProjectileParams { Projectile = DefRef<ProjectileDef>.To(Arrow), Cooldown = 0.5f, Range = 320f })) with { Speed = 40f };
    private static readonly EnemyDef Ghoul = Enemy("wall_ghoul",
        B("charge", new ChargeParams { RangeBand = [0f, 300f], Telegraph = Tel, Damage = 10f })) with { Speed = 40f };

    /// <summary>적을 벽 뒤에 두고 3초. 적이 벽 줄 뒤(하위 칸 y &lt; 16)에 있는 동안 낸 투사체·예고 수.</summary>
    private static int ActsBehindWall(EnemyDef def, bool sight, Func<RecordingSpawner, int> count)
    {
        var t = World(Defs([def], megaTiles: Mega, sight: sight ? TestDefs.Sight : null, heroLight: 420f, place: [LRow]));
        Assert.Equal(sight, t.World.Visibility.Enabled);
        t.PlaceHero(t.CellCenter(20, 20));
        var e = Spawn(t, def.Id, t.CellCenter(20, 12), source: SpawnSource.Escort);
        var iso = t.World.Arena!.Iso;
        var sp = Spawner(t);
        var behind = 0;
        for (var i = 0; i < 180; i++)
        {
            var wasBehind = iso.CellOf(e.Pos).Y < WallRow;
            var before = count(sp);
            t.Step();
            if (wasBehind) behind += count(sp) - before;
        }
        return behind;
    }

    [Fact]
    public void Archer_behind_sight_only_row_does_not_shoot()
    {
        var lit = ActsBehindWall(Archer, sight: true, sp => sp.Projectiles.Count);
        var dark = ActsBehindWall(Archer, sight: false, sp => sp.Projectiles.Count);
        output.WriteLine($"shots behind L row: sight={lit} control={dark}");
        Assert.True(dark > 0, "대조군(시야 끔)이 벽 뒤에서 쏘지 않아 대조가 되지 않는다");
        Assert.Equal(0, lit);
    }

    [Fact]
    public void Charger_behind_sight_only_row_does_not_start_charge()
    {
        var lit = ActsBehindWall(Ghoul, sight: true, sp => sp.Telegraphs.Count);
        var dark = ActsBehindWall(Ghoul, sight: false, sp => sp.Telegraphs.Count);
        output.WriteLine($"charges behind L row: sight={lit} control={dark}");
        Assert.True(dark > 0, "대조군(시야 끔)이 벽 뒤에서 돌진하지 않아 대조가 되지 않는다");
        Assert.Equal(0, lit);
    }

    [Fact]
    public void Archer_too_close_behind_sight_only_row_holds_instead_of_jittering()
    {
        // 띠 최소(150)보다 가까운 벽 뒤(143px): 안 보이니 다가가면 L 줄을 지나 보이게 되고 → 너무 가까워 물러나고 → 다시 안 보임을 매 틱 되풀이했다
        // (액트 1 씨앗 7 의 작은 방 — kite_switches 3921 > kite 적 초 2577). 지금은 벽 뒤에서 멈춘다.
        var t = World(Defs([Archer], megaTiles: Mega, sight: TestDefs.Sight, heroLight: 420f, place: [LRow]));
        t.PlaceHero(t.CellCenter(20, 19));
        var e = Spawn(t, Archer.Id, t.CellCenter(20, 15), source: SpawnSource.Escort);
        Assert.True(Vector2.Distance(e.Pos, t.Hero.Pos) < 150f);
        t.Step(300);
        output.WriteLine($"kite_switches={t.World.Fairness.KiteSwitches} shots={Spawner(t).Projectiles.Count} cell={t.World.Arena!.Iso.CellOf(e.Pos)}");
        Assert.True(t.World.Fairness.KiteSwitches <= 2, $"kite 전환 {t.World.Fairness.KiteSwitches} > 2 — 벽 뒤에서 떨었다");
        Assert.Empty(Spawner(t).Projectiles);
    }
}
