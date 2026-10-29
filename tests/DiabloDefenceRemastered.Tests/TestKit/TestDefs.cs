namespace DiabloDefenceRemastered.Tests.TestKit;

/// <summary>
/// 코드로 만드는 작은 정의 (설계서 10.1 — 실제 데이터에 기대는 시험과 분리). 참조는 모두 연결된 상태로 만든다.
/// </summary>
public static class TestDefs
{
    public const string MapId = "test_map";
    public const string StageId = "test_stage";
    public const string SpriteId = "TestSprite";
    public const string SpriteSetId = "set_test";
    public const string EnemyId = "test_enemy";

    public static SpriteDef Sprite(string id = SpriteId, int dirs = 12, int frames = 8) => new()
    {
        Id = id,
        File = id + ".png",
        Frame = new Int2(96, 96),
        Sheet = new Int2(96 * dirs, 96 * frames),
        Directions = dirs,
        Frames = frames,
        Origin = new Vector2(48, 84),
    };

    public static SpriteSetDef SpriteSet(SpriteDef sprite, string id = SpriteSetId, float baseRadius = 12f) => new()
    {
        Id = id,
        BaseSpeed = 67f,
        BaseRadius = baseRadius,
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

    public static MapDef Map(Vector2 size, string id = MapId) => new()
    {
        Id = id,
        Image = id + ".png",
        Size = new Int2((int)size.X, (int)size.Y),
        HeroStart = new HeroStartDef { Pos = size * 0.5f, Facing = Compass.South },
        TextureSource = new RectI(0, 0, 64, 64),
    };

    public static StageDef Stage(MapDef map, string id = StageId, VictoryDef? victory = null) => new()
    {
        Id = id,
        Name = id,
        Map = DefRef<MapDef>.To(map),
        Victory = victory ?? new VictoryDef { Type = VictoryKind.Boss },
    };

    /// <summary>맵·스테이지·그림 하나·적 정의들로 된 정의 묶음.</summary>
    public static GameDefs Basic(Vector2 mapSize, VictoryDef? victory = null, IEnumerable<EnemyDef>? extraEnemies = null)
    {
        var sprite = Sprite();
        var set = SpriteSet(sprite);
        var map = Map(mapSize);
        var stage = Stage(map, victory: victory);
        var enemies = new List<EnemyDef> { Enemy(set) };
        if (extraEnemies is not null) enemies.AddRange(extraEnemies);
        return new GameDefs
        {
            Sprites = new DefTable<SpriteDef>([sprite]),
            SpriteSets = new DefTable<SpriteSetDef>([set]),
            Maps = new DefTable<MapDef>([map]),
            Stages = new DefTable<StageDef>([stage]),
            Enemies = new DefTable<EnemyDef>(enemies),
        };
    }
}
