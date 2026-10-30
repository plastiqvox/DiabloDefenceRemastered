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
        };
    }

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
    /// 타일셋·맵·스테이지·그림 하나·적 정의들로 된 정의 묶음.
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
        return new GameDefs
        {
            Rules = new RulesDef { Sight = sight },
            Sprites = new DefTable<SpriteDef>([sprite]),
            SpriteSets = new DefTable<SpriteSetDef>([set]),
            Tilesets = new DefTable<TilesetDef>([tileset]),
            Maps = new DefTable<MapDef>([map]),
            Stages = new DefTable<StageDef>([stage]),
            Enemies = new DefTable<EnemyDef>(enemies),
        };
    }
}
