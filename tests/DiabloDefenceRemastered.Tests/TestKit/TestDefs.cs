namespace DiabloDefenceRemastered.Tests.TestKit;

/// <summary>
/// 코드로 만드는 작은 정의 (설계서 10.1 — 실제 데이터에 기대는 시험과 분리). 참조는 모두 연결된 상태로 만든다.
/// 합성 타일셋은 <b>번호는 가짜, 성질만</b> 원작을 닮게 둔다: 뒷벽은 칸의 반만 막힘(L1 1·2), 앞둘레는 두 줄 두께 <c>X</c>(L1 18·19·20·24),
/// 기둥은 위-왼 한 칸 <c>x</c>(L1 15 — 걷기·투사체, 시야 통과). 실제 MVP 맵에 없는 조각도 둔다 — 그 경우를 시험하려고(v0.5 8글자, 설계서 6.11):
/// 걷기만 <c>w</c>, 투사체만 <c>m</c>, 걷기·시야 <c>W</c>, 시야·투사체 <c>M</c>, 시야만 <c>L</c>, 시야 막는 기둥·안쪽 벽 줄·두 줄 벽 <c>X</c>.
/// 줄 조각(<c>..._wall</c>, <c>sight_only</c>)은 위쪽 두 칸이라 <c>line</c> 을 가로(같은 my)로 놓으면 끊김 없는 한 줄이 된다.
/// </summary>
public static class TestDefs
{
    public const string MapId = "test_map";
    public const string TilesetId = "test_tiles";
    public const string StageId = "test_stage";
    public const string SpriteId = "TestSprite";
    public const string SpriteSetId = "set_test";
    public const string EnemyId = "test_enemy";
    public const string Border = "wall";
    /// <summary>합성 직업(v0.6, 6.16) — 기본 정의의 <c>rules.hero.class</c>.</summary>
    public const string HeroClassId = "test_hero";
    public const string WeaponId = "test_weapon";
    /// <summary>합성 직업의 특성 패시브(<c>trait: true</c>, v0.6.1).</summary>
    public const string TraitId = "test_trait";

    /// <summary>기본 맵 메가타일 수(N = 24, 외곽 상자 1536×896).</summary>
    public const int DefaultMegaTiles = 12;

    // 합성 메가타일 번호(가짜)
    public const int Floor1 = 1, Floor2 = 2, GateOpen = 3;
    public const int CornerN = 11, EdgeNe = 12, CornerE = 13, EdgeSe = 14, CornerS = 15, EdgeSw = 16, CornerW = 17, EdgeNw = 18;
    public const int GateEnd = 20;
    public const int Pillar = 30, LowWall = 31, Glass = 32;
    public const int WalkSightWall = 33, SightMissileWall = 34, SightOnly = 35, InnerWall = 36, ThickWall = 37, Column = 38;

    /// <summary>
    /// <c>rules.sight</c> 시험 값 = 6.10 첫 판(edge 40, memory_ratio 0.5, fade 0.15 → 9틱, refresh_ticks 6, view_inset 16, touch_margin 16, ranged_margin 20).
    /// </summary>
    public static SightRulesDef Sight => new()
    {
        Edge = 40f, MemoryRatio = 0.5f, Fade = 0.15f, RefreshTicks = 6, ViewInset = 16f, TouchMargin = 16f, RangedMargin = 20f,
    };

    /// <summary>시야 시험 기본 빛 반경(px). 기획 스테이지 1 값(420)과 같다.</summary>
    public const float SightRadius = 420f;

    /// <summary>시험 그림 묶음 몸 높이(px, 크기 1).</summary>
    public const float BodyHeight = 72f;

    public static SpriteDef Sprite(string id = SpriteId, int dirs = 8, int frames = 8) => new()
    {
        Id = id,
        Source = SpriteSource.Grid,
        File = id + ".png",
        Frame = new Int2(96, 96),
        Sheet = new Int2(96 * dirs, 96 * frames),
        Directions = dirs,
        Frames = frames,
        ColorKey = Rgb.Black,
        Origin = new Vector2(48, 84),
    };

    public static SpriteSetDef SpriteSet(SpriteDef sprite, string id = SpriteSetId, float baseRadius = 12f) => new()
    {
        Id = id,
        BaseSpeed = 67f,
        BaseRadius = baseRadius,
        BodyHeight = BodyHeight,
        FrameTime = 0.06f,
        Clips = new ClipSetDef
        {
            Idle = new ClipDef { Sprite = DefRef<SpriteDef>.To(sprite) },
            Walk = new ClipDef { Sprite = DefRef<SpriteDef>.To(sprite) },
            Attack = new ClipDef { Sprite = DefRef<SpriteDef>.To(sprite), EventFrame = 7 },
            Die = new ClipDef { Sprite = DefRef<SpriteDef>.To(sprite) },
        },
    };

    public static EnemyDef Enemy(SpriteSetDef set, string id = EnemyId, float hp = 10f, float speed = 70f, float? radius = null) => new()
    {
        Id = id,
        Name = id,
        Sprites = DefRef<SpriteSetDef>.To(set),
        Hp = hp,
        Speed = speed,
        Damage = 5f,
        Xp = 1,
        Radius = radius,
    };

    /// <summary>합성 타일셋. 문은 네 변 모두(열린 칸 "...." + 끝 조각 "XXXX").</summary>
    public static TilesetDef Tileset(string id = TilesetId)
    {
        var gate = new GateTilesDef { Open = GateOpen, EndA = GateEnd, EndB = GateEnd };
        return new TilesetDef
        {
            Id = id,
            ColumnHeight = 160,
            Floor = [Floor1, Floor2],
            Borders = new Dictionary<string, BorderTilesDef>
            {
                [Border] = new()
                {
                    CornerN = CornerN, EdgeNe = EdgeNe, CornerE = CornerE, EdgeSe = EdgeSe,
                    CornerS = CornerS, EdgeSw = EdgeSw, CornerW = CornerW, EdgeNw = EdgeNw,
                },
            },
            Gates = new Dictionary<string, GateSetDef> { [Border] = new() { Nw = gate, Ne = gate, Se = gate, Sw = gate } },
            Pieces = new Dictionary<string, int>
            {
                ["pillar"] = Pillar, ["low_wall"] = LowWall, ["glass"] = Glass,
                ["walk_sight_wall"] = WalkSightWall, ["sight_missile_wall"] = SightMissileWall, ["sight_only"] = SightOnly,
                ["inner_wall"] = InnerWall, ["thick_wall"] = ThickWall, ["column"] = Column,
            },
            Props = new Dictionary<int, string>
            {
                [Floor1] = "....", [Floor2] = "....", [GateOpen] = "....",
                [CornerN] = "XXX.",                 // 맨 위 모서리: 뒤쪽 두 변
                [EdgeNe] = "XX..",                  // 윗줄 뒷벽 — 칸의 반만(L1 2 처럼)
                [EdgeNw] = "X.X.",                  // 왼쪽 열 뒷벽 — 칸의 반만(L1 1 처럼)
                [CornerE] = "XXXX", [EdgeSe] = "XXXX", [CornerS] = "XXXX", [EdgeSw] = "XXXX", [CornerW] = "XXXX",   // 앞둘레 두 줄 두께
                [GateEnd] = "XXXX",
                [Pillar] = "x...",                  // 기둥: 위-왼 한 칸, 걷기·투사체만 — 시야 통과(L1 15 = SOL 13 처럼)
                [LowWall] = "ww..",                 // 걷기만 막힘(합성)
                [Glass] = "mm..",                   // 투사체만 막힘(합성)
                [WalkSightWall] = "WW..",           // 걷기·시야(합성)
                [SightMissileWall] = "MM..",        // 시야·투사체(합성, L1 아치 보정 칸의 뜻)
                [SightOnly] = "LL..",               // 시야만 — 걷기·투사체 트임(합성, WP4 시야 조건 시험의 벽)
                [InnerWall] = "XX..",               // 시야 막는 안쪽 벽 줄(합성)
                [ThickWall] = "XXXX",               // 두 줄 벽(합성, 앞 턱처럼)
                [Column] = "X...",                  // 시야도 막는 기둥(합성 — 기획 열린 질문 22 의 design_diffs 경우)
            },
            Marks = new Dictionary<TileMark, IReadOnlyList<int>> { [TileMark.Pillar] = [Pillar] },
        };
    }

    // ── 직업(v0.6, 6.16) ───────────────────────────────────────────

    /// <summary>합성 무기(발사 패턴 없음 — 직업 시작 무기 자리만 채운다).</summary>
    public static WeaponDef Weapon(string id = WeaponId) => new() { Id = id, Name = id, MaxLevel = 1 };

    /// <summary>합성 특성 패시브: 1레벨, 쿨다운 −5%(6.16 예의 마력 친화 모양).</summary>
    public static PassiveDef Trait(string id = TraitId) => new()
    {
        Id = id, Name = id, Trait = true, MaxLevel = 1,
        PerLevel = new Dictionary<StatKey, float> { [StatKey.CooldownPct] = 0.05f },
    };

    /// <summary>합성 직업: 그림 묶음·시작 무기·특성 패시브(설계서 11.1 WP1d #17).</summary>
    public static HeroClassDef HeroClass(SpriteSetDef set, WeaponDef weapon, PassiveDef? trait = null, string id = HeroClassId,
                                         float radius = 14f, IReadOnlyDictionary<StatKey, float>? stats = null,
                                         IReadOnlyList<ContentBundle>? requires = null) => new()
    {
        Id = id,
        Name = id,
        Sprites = DefRef<SpriteSetDef>.To(set),
        Radius = radius,
        Stats = stats ?? new Dictionary<StatKey, float>(),
        StartWeapons = [DefRef<WeaponDef>.To(weapon)],
        Traits = trait is null ? [] : [DefRef<PassiveDef>.To(trait)],
        Requires = requires ?? [],
    };

    // ── 안쪽 방 합성 타일셋·맵(v0.6, 6.11·13.5-19) ─────────────────────

    /// <summary>안쪽 방 시험 맵 메가타일 수(6.11 예 배치와 같은 M 20).</summary>
    public const int RoomMegaTiles = 20;

    // 안쪽 방 합성 타일셋 번호(가짜)
    public const int RFloor = 1, RGateOpen = 3;
    public const int RCornerN = 11, REdgeNe = 12, RCornerE = 13, REdgeSe = 14, RCornerS = 15, REdgeSw = 16, RCornerW = 17, REdgeNw = 18;
    public const int RPillar = 30, RWallV = 40, RWallH = 41, RWallCorner = 42, RWallEndV = 43, RWallEndH = 44, RGrateH = 45,
                     RBloodWallV = 46, RBloodWallCorner = 47, RBloodWallH = 48, RBloodFloor = 49, RDoorClosed = 50, RArch = 51;

    /// <summary>
    /// 안쪽 방 합성 타일셋 — 6.11 예의 <b>조각 이름</b>(번호는 가짜)과 원본 SOL 을 닮은 성질: 세로 벽 = 왼쪽 열 <c>X.X.</c>, 가로 벽 = 위 줄 <c>XX..</c>,
    /// 모서리 = 위 줄 + 왼쪽 열 <c>XXX.</c>(벽 맞물림), 창살 <c>Xw..</c>(기둥 칸만 셋 다, 나머지 한 칸은 걷기만), 기둥 <c>x...</c>, 닫힌 문 = 가로 벽,
    /// 아치 <c>X...</c>(위 줄 틈이 하위 1칸). 둘레는 L1 처럼 뒤쪽 반 칸(<c>X.X.</c>·<c>XX..</c>·<c>XXX.</c>)·앞쪽 두 줄(<c>XXXX</c>), 문은 끝 조각 없이 열린 칸 둘.
    /// <c>marks</c>: pillar·grate·closed_door·arch.
    /// </summary>
    public static TilesetDef RoomTileset(string id = "test_room_tiles")
    {
        var gate = new GateTilesDef { Open = RGateOpen };
        return new TilesetDef
        {
            Id = id,
            ColumnHeight = 160,
            Floor = [RFloor],
            Borders = new Dictionary<string, BorderTilesDef>
            {
                [Border] = new()
                {
                    CornerN = RCornerN, EdgeNe = REdgeNe, CornerE = RCornerE, EdgeSe = REdgeSe,
                    CornerS = RCornerS, EdgeSw = REdgeSw, CornerW = RCornerW, EdgeNw = REdgeNw,
                },
            },
            Gates = new Dictionary<string, GateSetDef> { [Border] = new() { Nw = gate, Ne = gate, Se = gate, Sw = gate } },
            Pieces = new Dictionary<string, int>
            {
                ["pillar"] = RPillar, ["wall_v"] = RWallV, ["wall_h"] = RWallH, ["wall_corner"] = RWallCorner,
                ["wall_end_v"] = RWallEndV, ["wall_end_h"] = RWallEndH, ["grate_h"] = RGrateH,
                ["blood_wall_v"] = RBloodWallV, ["blood_wall_corner"] = RBloodWallCorner, ["blood_wall_h"] = RBloodWallH,
                ["blood_floor"] = RBloodFloor, ["door_closed"] = RDoorClosed, ["arch"] = RArch,
            },
            Marks = new Dictionary<TileMark, IReadOnlyList<int>>
            {
                [TileMark.Pillar] = [RPillar], [TileMark.Grate] = [RGrateH], [TileMark.ClosedDoor] = [RDoorClosed], [TileMark.Arch] = [RArch],
            },
            Props = new Dictionary<int, string>
            {
                [RFloor] = "....", [RGateOpen] = "....",
                [RCornerN] = "XXX.", [REdgeNe] = "XX..", [REdgeNw] = "X.X.",
                [RCornerE] = "XXXX", [REdgeSe] = "XXXX", [RCornerS] = "XXXX", [REdgeSw] = "XXXX", [RCornerW] = "XXXX",
                [RPillar] = "x...",
                [RWallV] = "X.X.", [RWallH] = "XX..", [RWallCorner] = "XXX.", [RWallEndV] = "X.X.", [RWallEndH] = "XX..",
                [RGrateH] = "Xw..",
                [RBloodWallV] = "X.X.", [RBloodWallCorner] = "XXX.", [RBloodWallH] = "XX..", [RBloodFloor] = "....",
                [RDoorClosed] = "XX..",
                [RArch] = "X...",
            },
        };
    }

    /// <summary>
    /// 안쪽 방 합성 맵: <see cref="RoomTileset"/>, 네 변 가운데 문(M 20 → 열린 칸 9·10), 주인공 시작 = 맵 중심(하위 19.5 → 칸 20).
    /// <paramref name="anchors"/> 는 <c>center</c> 에 더한다(<c>room_*</c> = 방 기준점, 방 영역은 <paramref name="roomAreas"/> — v0.6.3).
    /// </summary>
    public static MapDef RoomMap(IReadOnlyList<PiecePlacementDef> place, string id = "test_room_map", RectI? openCenter = null,
                                 IReadOnlyDictionary<string, TileCoord>? anchors = null, TilesetDef? tileset = null,
                                 int megaTiles = RoomMegaTiles, TileCoord? heroStart = null,
                                 IReadOnlyDictionary<string, RectI>? roomAreas = null)
    {
        var ts = tileset ?? RoomTileset();
        var c = (megaTiles * 2 - 1) / 2f;
        var allAnchors = new Dictionary<string, TileCoord> { ["center"] = new(c, c) };
        if (anchors is not null)
            foreach (var (k, v) in anchors) allAnchors[k] = v;
        return Map(megaTiles, id, ts, place) with
        {
            OpenCenter = openCenter,
            AnchorsTile = allAnchors,
            RoomAreas = roomAreas ?? new Dictionary<string, RectI>(),
            HeroStart = new HeroStartDef { Tile = heroStart ?? new TileCoord(c, c), Facing = Compass.South },
        };
    }

    /// <summary>
    /// 합성 방 맵 ① — 둘레 바로 옆 메가 벽 줄(갇힌 띠): 왼쪽 열 둘레(<c>X.X.</c>) 오른쪽 메가에 세로 벽 줄 (1,2)..(1,6)을 두면
    /// 둘 사이에 하위 1칸 바닥 띠(x = 1)가 생긴다. 띠 두 끝은 둘레 자리 (0,2)·(0,7)에 겹쳐 놓은 모서리로 막았다(띠가 아레나와 이어지지 않음).
    /// 13.5-19 ① 갇힌 바닥·② 하위 1칸 틈 오류, WP2 13.5-5 갇힌 칸 오류.
    /// </summary>
    public static MapDef RoomMapStrip() => RoomMap(
    [
        new() { Id = "strip_wall", Piece = "wall_v", Line = [new(1, 2), new(1, 6)] },
        new() { Id = "strip_caps", Piece = "wall_corner", At = [new(0, 2), new(0, 7)], OverBorder = true },
    ], id: "test_room_strip");

    /// <summary>합성 방 맵 ② — ① 의 같은 벽을 <c>over_border</c> 로 둘레 자리 (0,2)..(0,6)에 겹쳤다(띠 없음 → 13.5-19 통과).</summary>
    public static MapDef RoomMapOverBorder() => RoomMap(
    [
        new() { Id = "strip_wall", Piece = "wall_v", Line = [new(0, 2), new(0, 6)], OverBorder = true },
    ], id: "test_room_over_border");

    /// <summary>
    /// 합성 방 맵 ③ — 문 하나 방: 메가 (3..8, 3..8) 벽, 안쪽 바닥 하위 (7..15, 7..15), 아래 벽 가운데 두 메가 (5..6, 8)가 문(하위 4칸 틈).
    /// 꺾이는 네 자리는 모서리 조각. 기준점 <c>room_a</c> = 방 가운데 (11, 11), 영역 메가 [3,3,8,8](오른쪽·아래 벽 메가 8 은 빼고 — 하위 6..15). <paramref name="corners"/> 를 거짓으로 주면 ⑤.
    /// </summary>
    public static MapDef RoomMapOneDoor(bool corners = true, string id = "test_room_one_door")
    {
        var (tl, tr, bl, br) = corners
            ? ("wall_corner", "wall_corner", "wall_corner", "wall_corner")
            : ("wall_h", "wall_h", "wall_v", "wall_h");
        return RoomMap(
        [
            new()
            {
                Id = "room_a", Origin = new(3, 3),
                Grid =
                [
                    $"{tl} wall_h wall_h wall_h wall_h {tr}",
                    "wall_v .      .      .      .      wall_v",
                    "wall_v .      .      .      .      wall_v",
                    "wall_v .      .      .      .      wall_v",
                    "wall_v .      .      .      .      wall_v",
                    $"{bl} wall_h .      .      wall_h {br}",
                ],
            },
        ], id: id, anchors: new Dictionary<string, TileCoord> { ["room_a"] = new(11, 11) },
        roomAreas: new Dictionary<string, RectI> { ["room_a"] = new(3, 3, 8, 8) });
    }

    /// <summary>합성 방 맵 ⑤ — ③ 의 꺾이는 자리에 가로·세로 조각만 둔 방(하위 1칸 틈 오류).</summary>
    public static MapDef RoomMapNoCorners() => RoomMapOneDoor(corners: false, id: "test_room_no_corners");

    /// <summary>합성 방 맵 ⑥ — 둘레 왼쪽 열 (0,13)을 가로 조각으로 덮은 <c>over_border</c>(v0.6 예의 ㄱ 벽): 아래-왼 하위 칸 막힘이 풀려 배치 풀기 오류.</summary>
    public static MapDef RoomMapBorderNotch() => RoomMap(
    [
        new() { Id = "s1_wall_west", Piece = "wall_h", Line = [new(0, 13), new(5, 13)], OverBorder = true },
    ], id: "test_room_border_notch");

    /// <summary>
    /// 합성 방 맵 ④ — 설계서 6.11 예 배치(<c>cathedral_arena</c>, 기획 v0.7.1 10.3) 좌표 그대로, 이름은 <see cref="RoomTileset"/> 의 합성 이름.
    /// 13.5-19 전체(open_center·방 기준점·방 영역·하위 칸 ①②·over_border 막힘)를 통과해야 한다 — 예와 검사가 다시 어긋나지 않게(10.1 <c>Map</c>).
    /// 방 영역(v0.6.3)은 6.11 예의 <c>room_areas</c> 그대로 — 문 수: 피 방·창살 방 1, ㄱ 안·Γ 안 2.
    /// </summary>
    public static MapDef RoomMapExample() => RoomMap(
    [
        new()
        {
            Id = "s1_room_blood", Origin = new(0, 0), OverBorder = true, DecorWhy = "닫힌 문은 아래 벽 장식(기획 10.3)",
            Grid =
            [
                "blood_wall_corner blood_wall_h blood_wall_h blood_wall_h blood_wall_h blood_wall_corner",
                "blood_wall_v      blood_floor  blood_floor  blood_floor  blood_floor  wall_v",
                "blood_wall_v      blood_floor  blood_floor  blood_floor  blood_floor  wall_end_v",
                "blood_wall_v      blood_floor  blood_floor  blood_floor  blood_floor  .",
                "blood_wall_v      blood_floor  blood_floor  blood_floor  blood_floor  .",
                "blood_wall_corner blood_wall_h door_closed  blood_wall_h blood_wall_h blood_wall_h",
            ],
        },
        new()
        {
            Id = "s1_room_grate", Origin = new(13, 0), OverBorder = true,
            Grid = ["wall_corner", "wall_v", "wall_end_v", ".", ".", "wall_v", "wall_h wall_h grate_h grate_h wall_h wall_h"],
        },
        new() { Id = "s1_wall_west", Piece = "wall_end_h", At = [new(3, 13)] },
        new() { Piece = "wall_h", Line = [new(4, 13), new(5, 13)] },
        new() { Piece = "wall_v", Line = [new(6, 13), new(6, 15)] },
        new() { Piece = "wall_end_v", At = [new(6, 16)] },
        new() { Id = "s1_wall_south", Piece = "wall_corner", At = [new(13, 13)] },
        new() { Piece = "wall_h", Line = [new(14, 13), new(15, 13)] },
        new() { Piece = "wall_end_h", At = [new(16, 13)] },
        new() { Piece = "wall_v", Line = [new(13, 14), new(13, 15)] },
        new() { Piece = "wall_end_v", At = [new(13, 16)] },
        new() { Piece = "pillar", At = [new(15, 16), new(17, 15), new(17, 17), new(7, 9), new(12, 9)] },
    ],
    id: "test_room_example",
    openCenter: new RectI(6, 6, 13, 13),
    anchors: new Dictionary<string, TileCoord>
    {
        ["room_blood"] = new(5, 5), ["room_grate"] = new(32, 7), ["room_west"] = new(6, 32), ["room_south"] = new(31, 31),
    },
    roomAreas: new Dictionary<string, RectI>
    {
        ["room_blood"] = new(0, 0, 5, 5), ["room_grate"] = new(13, 0, 19, 6), ["room_west"] = new(0, 13, 6, 19), ["room_south"] = new(13, 13, 19, 19),
    });

    /// <summary>기본 기둥 자리(M ≥ 8 일 때 네 개).</summary>
    public static IReadOnlyList<PiecePlacementDef> DefaultPlace(int megaTiles) =>
        megaTiles >= 8
            ? [new PiecePlacementDef { Piece = "pillar", At = [new(3, 3), new(megaTiles - 4, 3), new(3, megaTiles - 4), new(megaTiles - 4, megaTiles - 4)] }]
            : [];

    /// <summary>
    /// 합성 맵: 네 변 가운데에 문(입구 ID = 문 ID), 진입점은 뒤 문 4·앞 문 N−6, 바깥 시작점 −4·N+3(6.11 규칙), 주인공 시작·기준점 center = 맵 중심.
    /// </summary>
    public static MapDef Map(int megaTiles = DefaultMegaTiles, string id = MapId, TilesetDef? tileset = null,
                             IReadOnlyList<PiecePlacementDef>? place = null)
    {
        var ts = tileset ?? Tileset();
        var n = megaTiles * 2;
        var c = (n - 1) / 2f;
        return new MapDef
        {
            Id = id,
            Tileset = DefRef<TilesetDef>.To(ts),
            Palette = "test_pal",
            Size = megaTiles,
            Layout = new ArenaLayoutDef
            {
                Border = Border,
                Gates =
                [
                    new() { Id = "gate_nw", Side = GateSide.Nw }, new() { Id = "gate_ne", Side = GateSide.Ne },
                    new() { Id = "gate_se", Side = GateSide.Se }, new() { Id = "gate_sw", Side = GateSide.Sw },
                ],
                Place = place ?? DefaultPlace(megaTiles),
            },
            Entries =
            [
                new() { Id = "gate_nw", OutsideTile = new(-4, c), InsideTile = new(4, c) },
                new() { Id = "gate_ne", OutsideTile = new(c, -4), InsideTile = new(c, 4) },
                new() { Id = "gate_se", OutsideTile = new(n + 3, c), InsideTile = new(n - 6, c) },
                new() { Id = "gate_sw", OutsideTile = new(c, n + 3), InsideTile = new(c, n - 6) },
            ],
            HeroStart = new HeroStartDef { Tile = new TileCoord(c, c), Facing = Compass.South },
            AnchorsTile = new Dictionary<string, TileCoord> { ["center"] = new(c, c) },
            Art = "d1/maps/" + id,
        };
    }

    public static StageDef Stage(MapDef map, string id = StageId, VictoryDef? victory = null, LightingDef? lighting = null) => new()
    {
        Id = id,
        Name = id,
        Map = DefRef<MapDef>.To(map),
        Victory = victory ?? new VictoryDef { Type = VictoryKind.Boss },
        Lighting = lighting ?? new LightingDef(),
    };

    /// <summary>
    /// 타일셋·맵·스테이지·그림 하나·적 정의들·합성 직업(<see cref="HeroClassId"/> = <c>rules.hero.class</c>, 시작 무기 하나·특성 하나, v0.6)으로 된 정의 묶음.
    /// 시야(v0.5): <paramref name="sight"/>(<c>rules.sight</c>)와 빛 반경 &gt; 0(<paramref name="heroLight"/> 또는 <paramref name="lighting"/>)이 둘 다 있어야 켜진다.
    /// <paramref name="place"/> 를 주면 맵의 안쪽 조각을 그것으로(기본 기둥 넷 대신), <paramref name="tileset"/> 을 주면 그 타일셋으로(시험이 조각을 더할 때).
    /// </summary>
    public static GameDefs Basic(int megaTiles = DefaultMegaTiles, VictoryDef? victory = null, IEnumerable<EnemyDef>? extraEnemies = null,
                                 LightingDef? lighting = null, SightRulesDef? sight = null, float heroLight = 0f,
                                 IReadOnlyList<PiecePlacementDef>? place = null, TilesetDef? tileset = null)
    {
        var sprite = Sprite();
        var set = SpriteSet(sprite);
        tileset ??= Tileset();
        var map = Map(megaTiles, tileset: tileset, place: place);
        if (heroLight > 0f)
            lighting = (lighting ?? new LightingDef()) with { HeroLight = (lighting?.HeroLight ?? new HeroLightDef()) with { Radius = heroLight } };
        var stage = Stage(map, victory: victory, lighting: lighting);
        var enemies = new List<EnemyDef> { Enemy(set) };
        if (extraEnemies is not null) enemies.AddRange(extraEnemies);
        var weapon = Weapon();
        var trait = Trait();
        var hero = HeroClass(set, weapon, trait);
        return new GameDefs
        {
            Rules = new RulesDef { Sight = sight, Hero = new HeroRulesDef { Class = DefRef<HeroClassDef>.To(hero) } },
            Sprites = new DefTable<SpriteDef>([sprite]),
            SpriteSets = new DefTable<SpriteSetDef>([set]),
            Weapons = new DefTable<WeaponDef>([weapon]),
            Passives = new DefTable<PassiveDef>([trait]),
            HeroClasses = new DefTable<HeroClassDef>([hero]),
            Tilesets = new DefTable<TilesetDef>([tileset]),
            Maps = new DefTable<MapDef>([map]),
            Stages = new DefTable<StageDef>([stage]),
            Enemies = new DefTable<EnemyDef>(enemies),
        };
    }
}
