namespace DiabloDefenceRemastered.Core.Defs;

/// <summary>스테이지 <c>stages[]</c> (설계서 6.12).</summary>
public sealed record StageDef : IDef
{
    public required string Id { get; init; }
    public string Name { get; init; } = "";
    public required DefRef<MapDef> Map { get; init; }
    /// <summary>해금 조건(먼저 깨야 하는 스테이지 ID). null = 처음부터 열림.</summary>
    public string? Unlock { get; init; }
    public IReadOnlyList<ObstaclePlacementDef> Obstacles { get; init; } = [];
    public LightingDef Lighting { get; init; } = new();
    public VictoryDef Victory { get; init; } = new();
    public StageScaling Scaling { get; init; } = new();
    public EliteChanceDef? EliteChance { get; init; }
    public IReadOnlyList<SpawnIntervalDef> SpawnIntervals { get; init; } = [];

    /// <summary>시간표 이벤트. 원소마다 <c>at</c>·<c>type</c> + 매개변수. <c>at</c> 은 이벤트 매개변수 형이 가진다(WP4).</summary>
    [PartKind(PartKind.StageEvent)]
    public IReadOnlyList<PartDef> Events { get; init; } = [];
}

/// <summary>장애물 배치. 모양은 shape 하나 또는 parts 여러 개(아치), 아치는 lintel.</summary>
public sealed record ObstaclePlacementDef
{
    public required string Id { get; init; }
    public required DefRef<ObstacleKindDef> Kind { get; init; }
    public ShapeDef? Shape { get; init; }
    public IReadOnlyList<ShapeDef> Parts { get; init; } = [];
    public LintelDef? Lintel { get; init; }
    /// <summary>종류 높이 덮어쓰기.</summary>
    public float? Height { get; init; }
}

public sealed record LintelDef
{
    /// <summary>[x0, x1]</summary>
    public float[] X { get; init; } = [];
    public float Thickness { get; init; }
}

/// <summary>조명. <c>light_change</c> 이벤트가 바꾸는 경로: ambient, darkness, hero_light.*, kind_lights.&lt;종류&gt;.*</summary>
public sealed record LightingDef
{
    public Rgb Ambient { get; init; } = Rgb.White;
    public float Darkness { get; init; }
    public HeroLightDef HeroLight { get; init; } = new();
    /// <summary>장애물 종류 ID → 빛 덮어쓰기(빛이 없는 종류에 새로 붙일 수도 있다).</summary>
    public IReadOnlyDictionary<string, KindLightDef> KindLights { get; init; } = new Dictionary<string, KindLightDef>();
}

public sealed record HeroLightDef
{
    public Rgb Color { get; init; } = Rgb.White;
    public float Radius { get; init; }
}

public sealed record KindLightDef
{
    public Rgb? Color { get; init; }
    public float? Radius { get; init; }
    public float? Flicker { get; init; }
    public PulseDef? Pulse { get; init; }
}

public sealed record PulseDef
{
    public float Period { get; init; }
    public float Amount { get; init; }
}

public enum VictoryKind : byte { Boss, Survive }

public sealed record VictoryDef
{
    public VictoryKind Type { get; init; } = VictoryKind.Boss;
    /// <summary>survive 일 때 그 시각.</summary>
    public ClockTime? At { get; init; }
}

/// <summary>배율 식 [a, b] = a + b × 분 (기획 2.3).</summary>
public sealed record StageScaling
{
    public float[] Hp { get; init; } = [1f, 0f];
    public float[] Damage { get; init; } = [1f, 0f];
}

public sealed record EliteChanceDef
{
    /// <summary>판정 간격(초).</summary>
    public float Every { get; init; }
    public ClockTime From { get; init; }
    public float Chance { get; init; }
}

/// <summary>생성 구간. from/to 또는 <c>during: "boss"</c>.</summary>
public sealed record SpawnIntervalDef
{
    public ClockTime? From { get; init; }
    public ClockTime? To { get; init; }
    public string? During { get; init; }
    public int MaxAlive { get; init; }
    public float PerSecond { get; init; }
    public IReadOnlyList<WeightEntry<EnemyDef>> Weights { get; init; } = [];
}
