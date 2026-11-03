namespace DiabloDefenceRemastered.Tests.Data;

/// <summary>
/// "스테이지에서 나올 수 있는 적" <see cref="StageReach.Enemies"/> (설계서 6.14 v0.6.1, WP1d-구현) — 합성 정의.
/// 부품 매개변수의 적 참조는 로더(WP7)가 <see cref="PartDef.ReferencedEnemies"/> 에 모으므로, 시험은 그 칸을 바로 채운다.
/// </summary>
public class StageReachTests
{
    private static readonly SpriteSetDef Set = TestDefs.SpriteSet(TestDefs.Sprite());

    private static EnemyDef E(string id, IReadOnlyList<PartDef>? behaviors = null, IReadOnlyList<PatternDef>? patterns = null,
                              IReadOnlyList<PhaseDef>? phases = null) =>
        TestDefs.Enemy(Set, id) with { Behaviors = behaviors ?? [], Patterns = patterns ?? [], Phases = phases ?? [] };

    /// <summary>적을 부르는 부품(summon·split_on_death·호위·보스 이벤트 …). 참조는 연결 안 된 ID 그대로 — <paramref name="defs"/> 에서 찾는 길도 시험한다.</summary>
    private static PartDef Calls(PartKind kind, string type, params string[] enemies) =>
        new(kind, type, null, null, new object(), new object()) { ReferencedEnemies = [.. enemies.Select(id => new DefRef<EnemyDef>(id))] };

    private static PatternDef Pattern(params PartDef[] actions) => new() { Actions = actions };

    private static (StageDef Stage, GameDefs Defs) Build(IEnumerable<EnemyDef> enemies, IEnumerable<string> weights, params PartDef[] events)
    {
        var all = enemies.ToList();
        var byId = all.ToDictionary(e => e.Id);
        var map = TestDefs.Map(8);
        var stage = TestDefs.Stage(map) with
        {
            SpawnIntervals = [new SpawnIntervalDef { From = new ClockTime(0), To = new ClockTime(60), Weights = [.. weights.Select(w => new WeightEntry<EnemyDef>(DefRef<EnemyDef>.To(byId[w]), 1f))] }],
            Events = events,
        };
        return (stage, new GameDefs { Enemies = new DefTable<EnemyDef>(all), Stages = new DefTable<StageDef>([stage]), Maps = new DefTable<MapDef>([map]) });
    }

    private static string[] Ids(StageDef stage, GameDefs defs) => [.. StageReach.Enemies(stage, defs).Select(e => e.Id)];

    [Fact]
    public void WeightsAndEvents_AreStartSet_UnusedEnemyIsNot()
    {
        var (stage, defs) = Build(
            [E("zombie"), E("skeleton"), E("king"), E("unused")],
            ["zombie", "skeleton"],
            Calls(PartKind.StageEvent, "boss", "king"));
        Assert.Equal(["king", "skeleton", "zombie"], Ids(stage, defs));   // ID 서수 순
    }

    [Fact]
    public void SummonOnly_SplitOfSplit_PhaseSummon_AreReached()
    {
        var (stage, defs) = Build(
        [
            E("caster", patterns: [Pattern(Calls(PartKind.EnemyAction, "summon", "imp"))]),         // 패턴 동작의 소환
            E("imp"),
            E("mud", behaviors: [Calls(PartKind.EnemyBehavior, "split_on_death", "mudling")]),      // 분열체의
            E("mudling", behaviors: [Calls(PartKind.EnemyBehavior, "split_on_death", "mudlet")]),   // 분열체
            E("mudlet"),
            E("butcher", phases: [new PhaseDef { Add = [Pattern(Calls(PartKind.EnemyAction, "summon", "hf_stinger"))] }]),   // 페이즈 add
            E("hf_stinger"),
            E("lord", phases: [new PhaseDef { Patterns = [Pattern(Calls(PartKind.EnemyAction, "summon", "guard"))] }]),        // 로더가 푼 페이즈 목록
            E("guard"),
            E("never"),
        ],
        ["caster", "mud"],
        Calls(PartKind.StageEvent, "miniboss", "butcher"), Calls(PartKind.StageEvent, "boss", "lord"));
        Assert.Equal(["butcher", "caster", "guard", "hf_stinger", "imp", "lord", "mud", "mudlet", "mudling"], Ids(stage, defs));
    }

    [Fact]
    public void Cycle_Terminates_MissingReference_IsSkipped()
    {
        var (stage, defs) = Build(
        [
            E("a", behaviors: [Calls(PartKind.EnemyBehavior, "summon", "b")]),
            E("b", behaviors: [Calls(PartKind.EnemyBehavior, "summon", "a", "ghost")]),   // ghost 는 정의에 없음(13.5-1 이 따로 오류)
        ],
        ["a"]);
        Assert.Equal(["a", "b"], Ids(stage, defs));
    }

    [Fact]
    public void SameInput_SameOrder()
    {
        var enemies = new[] { E("z1", behaviors: [Calls(PartKind.EnemyBehavior, "summon", "y2", "x3")]), E("y2"), E("x3"), E("w4") };
        var (stage, defs) = Build(enemies, ["z1", "w4"]);
        var (stage2, defs2) = Build(enemies.Reverse(), ["w4", "z1"]);
        Assert.Equal(Ids(stage, defs), Ids(stage2, defs2));
        Assert.Equal(["w4", "x3", "y2", "z1"], Ids(stage, defs));
    }
}
