namespace DiabloDefenceRemastered.Core.Defs;

/// <summary>맵 <c>maps[]</c> (설계서 6.11). 월드 좌표 = 맵 픽셀.</summary>
public sealed record MapDef : IDef
{
    public required string Id { get; init; }
    public required string Image { get; init; }
    public string? Minimap { get; init; }
    public Int2 Size { get; init; }
    public required HeroStartDef HeroStart { get; init; }
    /// <summary>배경 그림에 이미 있는 고정 충돌 모양(그리지 않음).</summary>
    public IReadOnlyList<MapCollisionDef> Collision { get; init; } = [];
    public IReadOnlyList<EntryDef> Entries { get; init; } = [];
    /// <summary>기준점 ID → 좌표. 스테이지 장애물 <c>circle_at</c>·빛이 참조한다.</summary>
    public IReadOnlyDictionary<string, Vector2> Anchors { get; init; } = new Dictionary<string, Vector2>();
    /// <summary>장애물 질감 원본 영역(7.6).</summary>
    public RectI TextureSource { get; init; }
}

public sealed record HeroStartDef
{
    public Vector2 Pos { get; init; }
    public Compass Facing { get; init; } = Compass.South;
}

public sealed record MapCollisionDef
{
    public required string Id { get; init; }
    public required ShapeDef Shape { get; init; }
    public bool BlocksProjectiles { get; init; }
}

/// <summary>입구: 바깥 시작점(맵 밖) → 진입점(맵 안).</summary>
public sealed record EntryDef
{
    public required string Id { get; init; }
    public Vector2 Outside { get; init; }
    public Vector2 Inside { get; init; }
}

public enum DecorKind : byte { None, Ring, Jagged, Mortar, Bowl, Groove, RoundEnds, Arch }

/// <summary>장애물 종류 <c>obstacle_kinds[]</c>. 굽기 규칙은 7.6(WP5).</summary>
public sealed record ObstacleKindDef : IDef
{
    public required string Id { get; init; }
    public float Height { get; init; }
    public bool BlocksProjectiles { get; init; } = true;
    public DecorKind Decor { get; init; } = DecorKind.None;
    public EffectRef? TopEffect { get; init; }
    public ObstacleLightDef? Light { get; init; }
    /// <summary>false = 충돌만(<c>collision_only</c>), 굽지 않는다.</summary>
    public bool Draw { get; init; } = true;
}

public sealed record ObstacleLightDef
{
    public Rgb Color { get; init; } = Rgb.White;
    public float Radius { get; init; }
    public float Flicker { get; init; }
}
