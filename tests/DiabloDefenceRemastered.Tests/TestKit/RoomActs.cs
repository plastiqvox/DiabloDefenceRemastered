namespace DiabloDefenceRemastered.Tests.TestKit;

/// <summary>
/// 합성 액트 둘 (v0.7.1 #23-구현 — WP1e-구현 담당, 끝나면 WP4). 실제 데이터 없이 <c>RunPlanner</c>·<c>RoomEvents</c>·<c>RoomFlow</c>·<c>RunSession</c> 을 시험한다.
/// <list type="bullet">
/// <item>작은 합성 방 맵 둘(<c>test_room_a</c>·<c>test_room_b</c>, M 16 = N 32 — 작은 방, <c>room_kinds</c> 일반·정예) + 큰 합성 맵 하나(<c>test_boss_room</c>, M 20 — 보스 방).</item>
/// <item>액트 1(<c>test_act1</c>): 일반 1·2·3 → 정예 4(<c>test_miniboss</c>) → 일반 5 → 보스 6(<c>test_boss</c>), <c>next_act</c> = 액트 2.</item>
/// <item>액트 2(<c>test_act2</c>): 일반 1·2 → 보스 3, 마지막 액트.</item>
/// <item>이벤트(액트 1): 0:30 <c>light_change</c>(어둠 0.3) — 방 1 안, 1:30 <c>light_change</c>(주인공 빛 300) — 방 2 안, 2:00 <c>banner</c>(방 2), 4:00 <c>miniboss</c>(정예 4 시작 — 방 시계 0:03).</item>
/// </list>
/// 보상 = <see cref="TestDefs.Rewards"/>(마법서·보물 상자·물약, 가중 30·30·15), <c>first_room</c> 마법서, <c>rules.rooms</c> = <see cref="TestDefs.RoomRules"/>(기준 맵 = 보스 맵).
/// </summary>
public static class RoomActs
{
    public const string Act1Id = "test_act1";
    public const string Act2Id = "test_act2";
    public const string RoomA = "test_room_a";
    public const string RoomB = "test_room_b";
    public const string BossRoom = "test_boss_room";
    public const string MinibossId = "test_miniboss";
    public const string BossId = "test_boss";

    /// <summary>합성 정의 한 벌. <paramref name="twoNormalMaps"/> 가 거짓이면 일반·정예 풀이 맵 하나(<c>test_room_a</c>).</summary>
    public static GameDefs Defs(bool twoNormalMaps = true)
    {
        var basic = TestDefs.Basic();
        var tileset = basic.Tilesets[0];
        var roomA = TestDefs.Map(16, RoomA, tileset, roomKinds: [RoomKind.Normal, RoomKind.Elite]);
        var roomB = TestDefs.Map(16, RoomB, tileset, roomKinds: [RoomKind.Normal, RoomKind.Elite]);
        var boss = TestDefs.Map(20, BossRoom, tileset, roomKinds: [RoomKind.Boss]);
        var set = basic.SpriteSets[0];
        var miniboss = TestDefs.Enemy(set, MinibossId, hp: 100f);
        var bossEnemy = TestDefs.Enemy(set, BossId, hp: 500f);
        var (rewards, objects) = TestDefs.Rewards();
        IReadOnlyList<DefRef<MapDef>> pool = twoNormalMaps ? [DefRef<MapDef>.To(roomA), DefRef<MapDef>.To(roomB)] : [DefRef<MapDef>.To(roomA)];
        var maps = new RoomMapPoolDef { Normal = pool, Elite = pool, Boss = DefRef<MapDef>.To(boss) };
        var weights = new StageRewardsDef
        {
            Weights =
            [
                new WeightEntry<RewardDef>(DefRef<RewardDef>.To(rewards[0]), 30f),
                new WeightEntry<RewardDef>(DefRef<RewardDef>.To(rewards[1]), 30f),
                new WeightEntry<RewardDef>(DefRef<RewardDef>.To(rewards[2]), 15f),
            ],
            FirstRoom = DefRef<RewardDef>.To(rewards[0]),
        };

        var act2 = new StageDef
        {
            Id = Act2Id,
            Name = "합성 액트 2",
            Rooms =
            [
                TestDefs.Room(RoomKind.Normal, 0f, 60f, 1),
                TestDefs.Room(RoomKind.Normal, 60f, 120f, 2),
                TestDefs.Room(RoomKind.Boss, 120f, null, 3, boss: bossEnemy),
            ],
            RoomMaps = maps,
            Rewards = weights,
        };
        var act1 = new StageDef
        {
            Id = Act1Id,
            Name = "합성 액트 1",
            NextAct = DefRef<StageDef>.To(act2),
            Rooms =
            [
                TestDefs.Room(RoomKind.Normal, 0f, 60f, 1),
                TestDefs.Room(RoomKind.Normal, 60f, 135f, 2),
                TestDefs.Room(RoomKind.Normal, 135f, 210f, 3),
                TestDefs.Room(RoomKind.Elite, 210f, 270f, 4, miniboss: miniboss),
                TestDefs.Room(RoomKind.Normal, 270f, 360f, 5),
                TestDefs.Room(RoomKind.Boss, 360f, null, 6, boss: bossEnemy),
            ],
            RoomMaps = maps,
            Rewards = weights,
            Lighting = new LightingDef { Darkness = 0.1f, HeroLight = new HeroLightDef { Radius = 420f } },
            Events =
            [
                LightChange(30f, new LightSet("darkness", [0.3f])),
                LightChange(90f, new LightSet("hero_light.radius", [300f])),
                Event("banner", 120f),
                Event("miniboss", 210f),
            ],
        };

        return new GameDefs
        {
            Rules = basic.Rules with { Rooms = TestDefs.RoomRules(rewards[1], boss) },
            Sprites = basic.Sprites,
            SpriteSets = basic.SpriteSets,
            Weapons = basic.Weapons,
            Passives = basic.Passives,
            HeroClasses = basic.HeroClasses,
            Tilesets = basic.Tilesets,
            Maps = new DefTable<MapDef>([roomA, roomB, boss]),
            Stages = new DefTable<StageDef>([act1, act2]),
            Enemies = new DefTable<EnemyDef>([.. basic.Enemies.All, miniboss, bossEnemy]),
            Rewards = new DefTable<RewardDef>(rewards),
            Pickups = new DefTable<PickupDef>([.. objects, TestDefs.EliteChest()]),
        };
    }

    /// <summary>스테이지 이벤트 하나(매개변수 없음 — 부품 객체는 시험에서 쓰지 않음). <c>At</c> 은 t(초).</summary>
    public static PartDef Event(string type, float at) =>
        new(PartKind.StageEvent, type, null, null, new object(), new object()) { At = new ClockTime(at) };

    /// <summary><c>light_change</c> 이벤트(매개변수 = <see cref="LightChangeParams"/>).</summary>
    public static PartDef LightChange(float at, params LightSet[] set) =>
        new(PartKind.StageEvent, RoomEvents.LightChange, null, null, new LightChangeParams(set), new object()) { At = new ClockTime(at) };
}
