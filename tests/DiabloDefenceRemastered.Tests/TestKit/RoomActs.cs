namespace DiabloDefenceRemastered.Tests.TestKit;

/// <summary>
/// 합성 액트 둘 (v0.7.1 #23-구현 → v0.8 WP1f #20 — WP1f 가 끝날 때까지 WP1f, 그 뒤 WP4). 실제 데이터 없이 <c>RunPlanner</c>·<c>RoomEvents</c>·<c>RoomFlow</c>·<c>RunSession</c> 을 시험한다.
/// <list type="bullet">
/// <item>작은 합성 방 맵 둘(<c>test_room_a</c>·<c>test_room_b</c>, M 16 = N 32 — 작은 방, <c>room_kinds</c> 일반·정예) + 큰 합성 맵 하나(<c>test_boss_room</c>, M 20 — 보스 방).</item>
/// <item>액트 1(<c>test_act1</c>): 일반 1·2·3 → 정예 4(<c>test_miniboss</c>) → 일반 5 → 보스 6(<c>test_boss</c>), <c>next_act</c> = 액트 2.</item>
/// <item>액트 2(<c>test_act2</c>): 일반 1·2 → 보스 3, 마지막 액트.</item>
/// <item>이벤트(액트 1): 0:30 <c>light_change</c>(어둠 0.3) — 방 1 안, 1:30 <c>light_change</c>(주인공 빛 300) — 방 2 안, 2:00 <c>banner</c>(방 2), 4:00 <c>miniboss</c>(정예 4 시작 — 방 시계 0:03).</item>
/// <item>(v0.8) <paramref name="mirror"/>: 짝 표가 있는 합성 타일셋(<see cref="MirrorTileset"/> — <c>wall_v</c> ↔ <c>wall_h</c>, self <c>pillar</c>)과 거울판을 단 맵 셋
/// (원판과 전치가 다르게 가로 벽 한 줄 — <see cref="MirrorWall"/>). (맵, 방향) 후보가 늘어 <c>RunPlanner</c> ① 의 결과가 바뀐다.</item>
/// <item>(v0.8) <paramref name="floors"/>: 층 — 액트 1 방 1~3 = 1층, 4~6 = 2층, 액트 2 = 3층(<c>RoomExit.NextFloor</c> 시험).</item>
/// </list>
/// 보상 = <see cref="TestDefs.Rewards"/>(마법서·보물 상자·회복 문 가중 30·30·15 + 정의 표에만 성소·상인 셋 — v0.9), <c>first_room</c> 마법서, <c>rules.rooms</c> = <see cref="TestDefs.RoomRules"/>(기준 맵 = 보스 맵).
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
    public const string MirrorTilesetId = "test_mirror_tiles";
    /// <summary>짝 표 합성 타일셋의 세로 벽(왼쪽 열 <c>X.X.</c>)·가로 벽(위 줄 <c>XX..</c>) 번호(가짜).</summary>
    public const int WallV = 60, WallH = 61;

    /// <summary>합성 정의 한 벌. <paramref name="twoNormalMaps"/> 가 거짓이면 일반·정예 풀이 맵 하나(<c>test_room_a</c>).</summary>
    public static GameDefs Defs(bool twoNormalMaps = true, bool mirror = false, bool floors = false)
    {
        var basic = TestDefs.Basic();
        var tileset = mirror ? MirrorTileset() : basic.Tilesets[0];
        var roomA = RoomMap(16, RoomA, tileset, [RoomKind.Normal, RoomKind.Elite], mirror);
        var roomB = RoomMap(16, RoomB, tileset, [RoomKind.Normal, RoomKind.Elite], mirror);
        var boss = RoomMap(20, BossRoom, tileset, [RoomKind.Boss], mirror);
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
        RoomDef Floor(RoomDef r, int floor) => floors ? r with { Floor = floor } : r;

        var act2 = new StageDef
        {
            Id = Act2Id,
            Name = "합성 액트 2",
            Rooms =
            [
                Floor(TestDefs.Room(RoomKind.Normal, 0f, 60f, 1), 3),
                Floor(TestDefs.Room(RoomKind.Normal, 60f, 120f, 2), 3),
                Floor(TestDefs.Room(RoomKind.Boss, 120f, null, 3, boss: bossEnemy), 3),
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
                Floor(TestDefs.Room(RoomKind.Normal, 0f, 60f, 1), 1),
                Floor(TestDefs.Room(RoomKind.Normal, 60f, 135f, 2), 1),
                Floor(TestDefs.Room(RoomKind.Normal, 135f, 210f, 3), 1),
                Floor(TestDefs.Room(RoomKind.Elite, 210f, 270f, 4, miniboss: miniboss), 2),
                Floor(TestDefs.Room(RoomKind.Normal, 270f, 360f, 5), 2),
                Floor(TestDefs.Room(RoomKind.Boss, 360f, null, 6, boss: bossEnemy), 2),
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
            Tilesets = new DefTable<TilesetDef>([tileset]),
            Maps = new DefTable<MapDef>([roomA, roomB, boss]),
            Stages = new DefTable<StageDef>([act1, act2]),
            Enemies = new DefTable<EnemyDef>([.. basic.Enemies.All, miniboss, bossEnemy]),
            Rewards = new DefTable<RewardDef>(rewards),
            ItemBases = basic.ItemBases,
            ItemAffixes = basic.ItemAffixes,
            Merchants = new DefTable<MerchantDef>(TestDefs.MerchantsOf(rewards)),
            Pickups = new DefTable<PickupDef>([.. objects, TestDefs.EliteChest()]),
        };
    }

    /// <summary>
    /// 짝 표가 있는 합성 타일셋(v0.8): <see cref="TestDefs.Tileset"/> + 세로 벽 <c>wall_v</c>(<c>X.X.</c>)·가로 벽 <c>wall_h</c>(<c>XX..</c>),
    /// 짝 표 <c>pairs [wall_v, wall_h]</c>·<c>self [pillar]</c>. 둘레·문은 자리 짝 검사를 통과한다(뒷벽 반 칸 <c>X.X.</c> ↔ <c>XX..</c>, 앞둘레·끝 조각 <c>XXXX</c>).
    /// </summary>
    public static TilesetDef MirrorTileset()
    {
        var ts = TestDefs.Tileset(MirrorTilesetId);
        var pieces = new Dictionary<string, int>(ts.Pieces) { ["wall_v"] = WallV, ["wall_h"] = WallH };
        var props = new Dictionary<int, string>(ts.Props) { [WallV] = "X.X.", [WallH] = "XX.." };
        return ts with
        {
            Pieces = pieces,
            Props = props,
            Mirror = new TilesetMirrorDef { Pairs = [["wall_v", "wall_h"]], Self = ["pillar"] },
        };
    }

    /// <summary>
    /// 거울판 맵이 원판과 다르게 하는 가로 벽 한 줄(M 16: 메가 (6..8, 10) → 하위 (12..17, 20), M 20: (8..10, 13)) — 기본 기둥 넷은 대각선 대칭이라 이것이 없으면 거울판 배치가 원판과 같다.
    /// 작은 방 진입점 150px·하위 1칸 틈·갇힌 바닥 검사를 통과한다.
    /// </summary>
    public static PiecePlacementDef MirrorWall(int megaTiles) => megaTiles <= 16
        ? new PiecePlacementDef { Id = "mirror_probe", Piece = "wall_h", Line = [new(6, 10), new(8, 10)] }
        : new PiecePlacementDef { Id = "mirror_probe", Piece = "wall_h", Line = [new(8, 13), new(10, 13)] };

    private static MapDef RoomMap(int megaTiles, string id, TilesetDef tileset, IReadOnlyList<RoomKind> kinds, bool mirror)
    {
        if (!mirror) return TestDefs.Map(megaTiles, id, tileset, roomKinds: kinds);
        var map = TestDefs.Map(megaTiles, id, tileset, place: [.. TestDefs.DefaultPlace(megaTiles), MirrorWall(megaTiles)], roomKinds: kinds);
        return MapMirror.WithMirror(map);
    }

    /// <summary>스테이지 이벤트 하나(매개변수 없음 — 부품 객체는 시험에서 쓰지 않음). <c>At</c> 은 t(초).</summary>
    public static PartDef Event(string type, float at) =>
        new(PartKind.StageEvent, type, null, null, new object(), new object()) { At = new ClockTime(at) };

    /// <summary><c>light_change</c> 이벤트(매개변수 = <see cref="LightChangeParams"/>).</summary>
    public static PartDef LightChange(float at, params LightSet[] set) =>
        new(PartKind.StageEvent, RoomEvents.LightChange, null, null, new LightChangeParams(set), new object()) { At = new ClockTime(at) };
}
