using DiabloDefenceRemastered.Core.Sim.Enemies;
using DiabloDefenceRemastered.Core.Sim.Enemies.Parts;
using static DiabloDefenceRemastered.Tests.Enemies.Wp4Defs;

namespace DiabloDefenceRemastered.Tests.Enemies;

/// <summary>
/// WP4 검사기 시험 (설계서 6.14·13.5-13·14·15·17·18·23 ⑤⑥): 실제 단계 1 데이터는 오류·경고 0, 합성으로 일부러 어긴 정의는 그 규칙 번호의 오류·경고 하나.
/// </summary>
public sealed class EnemyDataValidatorTests(ITestOutputHelper output)
{
    private static TelegraphDef Tel(float time) => new() { Effect = DefRef<EffectDef>.To(new EffectDef { Id = "fx_val" }), Time = time };

    private static IReadOnlyList<DataError> Validate(GameDefs defs)
    {
        var ctx = new ValidationContext { Defs = defs };
        new EnemyDataValidator().Validate(defs, ctx);
        return ctx.Errors;
    }

    private static GameDefs WithInterval(EnemyDef def, SightRulesDef? sight = null, float heroLight = 420f) =>
        Defs([def], map => TestDefs.Stage(map) with
        {
            SpawnIntervals = [new SpawnIntervalDef { From = new ClockTime(0f), To = new ClockTime(60f), MaxAlive = 10, PerSecond = 1f,
                                                     Weights = [new WeightEntry<EnemyDef>(DefRef<EnemyDef>.To(def), 1f)] }],
        }, sight: sight, heroLight: heroLight);

    [Fact]
    public void Real_stage1_data_has_no_errors_or_warnings()
    {
        var errors = Validate(Sim.Wp4Data.Defs);
        foreach (var e in errors) output.WriteLine(e.ToString());
        Assert.Empty(errors);
    }

    [Fact]
    public void Synthetic_baseline_is_clean()
    {
        var ok = Enemy("val_ok", B("chase_melee", new ChaseMeleeParams { Cooldown = 1f }));
        Assert.Empty(Validate(WithInterval(ok, TestDefs.Sight)));
    }

    public static TheoryData<string, string, DataSeverity> Broken() => new()
    {
        { "charge_short_telegraph", "13.5-13", DataSeverity.Error },
        { "self_destruct_short_fuse", "13.5-13", DataSeverity.Error },
        { "aura_include_bosses", "13.5-17", DataSeverity.Warning },
        { "scale_over", "13.5-14", DataSeverity.Warning },
        { "kite_band_over_light", "13.5-15", DataSeverity.Error },
        { "light_under_range", "13.5-15", DataSeverity.Error },
    };

    private static SpawnIntervalDef Iv(float from, float to, int maxAlive, float perSecond, EnemyDef e) => new()
    {
        From = new ClockTime(from), To = new ClockTime(to), MaxAlive = maxAlive, PerSecond = perSecond,
        Weights = [new WeightEntry<EnemyDef>(DefRef<EnemyDef>.To(e), 1f)],
    };

    [Fact]
    public void Room_rhythm_warnings_13_5_18()
    {
        // 정예 방(2·3)이 붙어 있고(사이 일반 방 0, t 시작 간격 1:00) 3번 정예 방 구간(120–180)이 바로 앞 구간(60–120)보다 크다 → 13.5-18 경고 셋
        var grunt = Enemy("val_grunt");
        var mini = Enemy("val_mini") with { Rank = DefRank.Miniboss };
        var defs = Defs([grunt, mini], map => TestDefs.Stage(map) with
        {
            Rooms =
            [
                TestDefs.Room(RoomKind.Normal, 0f, 60f, 1),
                TestDefs.Room(RoomKind.Elite, 60f, 120f, 2, miniboss: mini),
                TestDefs.Room(RoomKind.Elite, 120f, 180f, 3, miniboss: mini),
                TestDefs.Room(RoomKind.Normal, 180f, 240f, 4),
            ],
            SpawnIntervals = [Iv(0f, 60f, 40, 2f, grunt), Iv(60f, 120f, 40, 2f, grunt), Iv(120f, 180f, 60, 3f, grunt), Iv(180f, 240f, 60, 3f, grunt)],
        });
        var errors = Validate(defs);
        foreach (var e in errors) output.WriteLine(e.ToString());
        var rhythm = errors.Where(e => e.Message.Contains("13.5-18", StringComparison.Ordinal)).ToList();
        Assert.All(rhythm, e => Assert.Equal(DataSeverity.Warning, e.Severity));
        Assert.Contains(rhythm, e => e.Message.Contains("일반 방이 없다", StringComparison.Ordinal));
        Assert.Contains(rhythm, e => e.Message.Contains("간격", StringComparison.Ordinal));
        Assert.Contains(rhythm, e => e.Message.Contains("바로 앞 구간보다 크다", StringComparison.Ordinal));
    }

    [Theory]
    [MemberData(nameof(Broken))]
    public void Broken_definition_reports_its_rule(string kind, string rule, DataSeverity severity)
    {
        var buff = DefRef<BuffDef>.To(Command);
        GameDefs defs = kind switch
        {
            "charge_short_telegraph" => WithInterval(Enemy("val_ghoul", B("charge", new ChargeParams { Telegraph = Tel(0.2f), Damage = 10f }))),
            "self_destruct_short_fuse" => WithInterval(Enemy("val_bomb", B("self_destruct", new SelfDestructParams { Fuse = 0.3f, Damage = 15f }))),
            "aura_include_bosses" => WithInterval(Enemy("val_aura", B("command_aura", new CommandAuraParams { Buff = buff, IncludeBosses = true }))),
            "scale_over" => WithInterval(Enemy("val_big") with { Scale = 1.7f }),
            "kite_band_over_light" => WithInterval(Enemy("val_kite", B("kite", new KiteParams { RangeBand = [200f, 400f] })), TestDefs.Sight, 420f),
            "light_under_range" => WithInterval(Enemy("val_dim"), heroLight: 300f),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
        var errors = Validate(defs);
        foreach (var e in errors) output.WriteLine(e.ToString());
        var hits = errors.Where(e => e.Message.Contains(rule, StringComparison.Ordinal)).ToList();
        Assert.NotEmpty(hits);
        Assert.All(hits, e => Assert.Equal(severity, e.Severity));
    }
}
