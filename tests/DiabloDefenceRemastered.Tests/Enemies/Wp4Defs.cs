using DiabloDefenceRemastered.Core.Sim.Enemies;
using DiabloDefenceRemastered.Core.Sim.Enemies.Parts;
using DiabloDefenceRemastered.Tests.Sim;

namespace DiabloDefenceRemastered.Tests.Enemies;

/// <summary>
/// WP4 합성 정의 도우미: 레지스트리로 부품을 만들고(로더 흉내), <see cref="TestDefs.Basic"/> 에 적·버프·접두사·<c>rules.ranks</c>·<c>rules.spawn</c> 를 더한다.
/// 월드는 EnemyModule + <see cref="FakeArena"/> 가짜 이웃.
/// </summary>
public static class Wp4Defs
{
    private static readonly PartRegistry Registry = MakeRegistry();

    private static PartRegistry MakeRegistry()
    {
        var r = new PartRegistry();
        EnemyModule.RegisterParts(r);
        return r;
    }

    public static readonly SpriteSetDef Set = TestDefs.SpriteSet(TestDefs.Sprite());

    public static PartDef Part<T>(PartKind kind, string type, T p, string? trigger = null, string? id = null) where T : notnull
    {
        if (!Registry.TryGet(kind, type, out var f)) throw new InvalidOperationException($"({kind}, {type}) 미등록");
        var enemies = new List<DefRef<EnemyDef>>();
        foreach (var prop in p.GetType().GetProperties())
        {
            var v = prop.GetValue(p);
            if (v is DefRef<EnemyDef> r) enemies.Add(r);
            if (v is IEnumerable<SummonEntry> list) enemies.AddRange(list.Select(s => s.Enemy));
        }
        return new PartDef(kind, type, id, trigger, p, f.Create(p)) { ReferencedEnemies = enemies };
    }

    public static PartDef B<T>(string type, T p, string? id = null) where T : notnull => Part(PartKind.EnemyBehavior, type, p, null, id);

    public static PartDef A<T>(string type, T p, string? trigger = null) where T : notnull => Part(PartKind.EnemyAction, type, p, trigger);

    public static PartDef E<T>(string type, T p, float at) where T : notnull => Part(PartKind.StageEvent, type, p) with { At = new ClockTime(at) };

    public static EnemyDef Enemy(string id, params PartDef[] behaviors) => TestDefs.Enemy(Set, id) with { Behaviors = behaviors, Tags = ["skeleton"] };

    public static readonly BuffDef Command = new()
    {
        Id = "command", Stats = new Dictionary<EnemyStatKey, float> { [EnemyStatKey.SpeedPct] = 0.25f, [EnemyStatKey.DamagePct] = 0.25f }, FootRing = new Rgb(255, 210, 90),
    };

    public static readonly BuffDef WarCryBuff = new()
    {
        Id = "war_cry", Stats = new Dictionary<EnemyStatKey, float> { [EnemyStatKey.DamagePct] = 0.30f }, FootRing = new Rgb(255, 80, 60),
    };

    public static readonly AffixDef Berserk = new()
    {
        Id = "berserk", Name = "광폭한", Stats = new Dictionary<EnemyStatKey, float> { [EnemyStatKey.SpeedPct] = 0.35f, [EnemyStatKey.AttackCooldownPct] = -0.30f },
        Glow = new Rgb(255, 60, 60),
    };

    public static RanksDef Ranks => new()
    {
        Elite = new RankDef { Hp = 3.5f, Damage = 1.4f, Speed = 1.1f, Xp = 4, Scale = 1.15f, KnockbackResistAdd = 0.3f, Affixes = 1, Glow = new Rgb(90, 140, 255),
                              Pulse = new RankPulseDef { Period = 1.2f, Alpha = [0.25f, 0.45f] } },
        Unique = new RankDef { Hp = 8f, Damage = 1.6f, Speed = 1.1f, Xp = 10, Scale = 1.3f, KnockbackResist = 0.8f, Affixes = 2, Glow = new Rgb(255, 200, 80) },
        Pack = new PackDef { PackSize = [3, 5], UniqueChance = 0.3f, Banner = "정예 무리가 나타났다", BannerTime = 1.5f },
        UniqueNames = new UniqueNamesDef { First = ["썩은", "검은"], Second = ["이빨", "손"], Title = ["도살자", "파수꾼"] },   // 2026-11-14 디아2식 우두머리 이름
        MaxScale = 1.6f,
    };

    public static SpawnRulesDef SpawnRules => new()
    {
        Band = [40f, 200f], Tries = 12, EdgeMargin = 4f, Group = [1, 3], GroupRadius = 40f, AccumulatorHold = 3f, PathDetour = 1.5f,
        RoomPressure = new RoomPressureDef { After = 8f, EveryNth = 3, MinDistance = 150f },
    };

    /// <summary>합성 정의 묶음. <paramref name="stage"/> 를 주면 그 스테이지 하나(맵은 기본 맵).</summary>
    public static GameDefs Defs(IEnumerable<EnemyDef> enemies, Func<MapDef, StageDef>? stage = null, int megaTiles = TestDefs.DefaultMegaTiles,
                                SightRulesDef? sight = null, float heroLight = 0f, IReadOnlyList<PiecePlacementDef>? place = null,
                                IEnumerable<AffixDef>? affixes = null, CapsDef? caps = null,
                                IEnumerable<EvolutionDef>? evolutions = null)
    {
        var basic = TestDefs.Basic(megaTiles, extraEnemies: enemies, sight: sight, heroLight: heroLight, place: place);
        var st = stage is null ? basic.Stages : new DefTable<StageDef>([stage(basic.Maps[0]) with { Lighting = basic.Stages[0].Lighting }]);
        return new GameDefs
        {
            Rules = basic.Rules with { Ranks = Ranks, Spawn = SpawnRules, Caps = caps ?? new CapsDef(), Enemies = new EnemyRulesDef() },
            Sprites = basic.Sprites,
            SpriteSets = basic.SpriteSets,   // Set 은 기본 묶음과 같은 ID·내용
            Weapons = basic.Weapons,
            Passives = basic.Passives,
            HeroClasses = basic.HeroClasses,
            Tilesets = basic.Tilesets,
            Maps = basic.Maps,
            Stages = st,
            Enemies = basic.Enemies,
            Rewards = basic.Rewards,
            Pickups = basic.Pickups,
            Buffs = new DefTable<BuffDef>([Command, WarCryBuff]),
            Affixes = new DefTable<AffixDef>(affixes ?? [Berserk]),
            Evolutions = evolutions is null ? DefTable<EvolutionDef>.Empty : new DefTable<EvolutionDef>(evolutions),
        };
    }

    /// <summary>EnemyModule + 가짜 이웃 월드(무기는 기본 끔 — 적이 죽지 않음).</summary>
    public static TestWorld World(GameDefs defs, ulong seed = 1, bool weapons = false, bool presentation = false) =>
        new TestWorldBuilder().WithDefs(defs).WithSeed(seed).WithPresentation(presentation).WithModules(FakeArena.Modules(weapons: weapons)).Build();

    public static Enemy Spawn(TestWorld t, string id, Vector2 pos, EnemyRank rank = EnemyRank.Normal, SpawnSource source = SpawnSource.Interval) =>
        EnemyFactory.Create(t.World, t.World.Defs.Enemies.Get(id), rank, [], pos, new SpawnContext(source, EntityId.None));

    public static RecordingSpawner Spawner(TestWorld t) => (RecordingSpawner)t.World.Spawner;
}
