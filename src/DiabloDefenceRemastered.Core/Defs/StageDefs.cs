using System.Text.Json;
using System.Text.Json.Serialization;
using DiabloDefenceRemastered.Core.Data.Json;

namespace DiabloDefenceRemastered.Core.Defs;

/// <summary>스테이지 <c>stages[]</c> (설계서 6.12). 장애물은 맵의 타일 배치에 들어가므로 스테이지에는 없다.</summary>
public sealed record StageDef : IDef
{
    public required string Id { get; init; }
    public string Name { get; init; } = "";
    public required DefRef<MapDef> Map { get; init; }
    /// <summary>해금 조건(먼저 깨야 하는 스테이지 ID). null = 처음부터 열림.</summary>
    public string? Unlock { get; init; }
    public LightingDef Lighting { get; init; } = new();
    public VictoryDef Victory { get; init; } = new();
    public StageScaling Scaling { get; init; } = new();
    public EliteChanceDef? EliteChance { get; init; }
    public IReadOnlyList<SpawnIntervalDef> SpawnIntervals { get; init; } = [];

    /// <summary>시간표 이벤트. 원소마다 <c>at</c>·<c>type</c> + 매개변수. <c>at</c> 은 이벤트 매개변수 형이 가진다(WP4).</summary>
    [PartKind(PartKind.StageEvent)]
    public IReadOnlyList<PartDef> Events { get; init; } = [];
}

/// <summary>
/// 조명 (설계서 6.12·7.7). <c>light_change</c> 이벤트(WP4)가 바꾸는 경로: ambient, darkness, unit_min_light, hero_light.*, lights.&lt;id&gt;.*
/// v0.2 의 장애물 종류별 빛(kind_lights)은 장애물 종류와 함께 없앴다.
/// </summary>
public sealed record LightingDef
{
    public Rgb Ambient { get; init; } = Rgb.White;
    public float Darkness { get; init; }
    public HeroLightDef HeroLight { get; init; } = new();
    /// <summary>주인공·적·시체·획득물이 받는 빛의 하한(0~1, 0 = 끔). 바닥·벽은 원작처럼 어둡게 둔다.</summary>
    public float UnitMinLight { get; init; }
    /// <summary>점 빛(MVP 두 스테이지는 비어 있음). 정의 순서 그대로 <c>World.Lighting</c> 에 실린다.</summary>
    public IReadOnlyList<PointLightDef> Lights { get; init; } = [];
}

public sealed record HeroLightDef
{
    public Rgb Color { get; init; } = Rgb.White;
    public float Radius { get; init; }
}

/// <summary>점 빛 하나. 자리 <c>at</c> = 기준점 ID 또는 하위 타일 좌표 <c>[tx,ty]</c> — <c>World.Lighting</c> 은 로딩 뒤 월드 px 를 가진다.</summary>
public sealed record PointLightDef
{
    public required string Id { get; init; }
    [JsonPropertyName("at")]
    public required TileOrAnchor AtTile { get; init; }
    public Rgb Color { get; init; } = Rgb.White;
    public float Radius { get; init; }
    public float Flicker { get; init; }
    public PulseDef? Pulse { get; init; }
}

/// <summary>JSON 에서 기준점 ID 문자열 또는 <c>[tx,ty]</c>. 월드로 바꾸는 곳은 <c>ResolvedMap</c>.</summary>
[JsonConverter(typeof(TileOrAnchorJsonConverter))]
public readonly record struct TileOrAnchor
{
    public TileOrAnchor(string anchor) { Anchor = anchor; }
    public TileOrAnchor(TileCoord tile) { Tile = tile; }

    /// <summary>기준점 ID. null 이면 <see cref="Tile"/>.</summary>
    public string? Anchor { get; }
    public TileCoord Tile { get; }
    public bool IsAnchor => Anchor is not null;

    public override string ToString() => Anchor ?? Tile.ToString();
}

public sealed class TileOrAnchorJsonConverter : JsonConverter<TileOrAnchor>
{
    private static readonly TileCoordJsonConverter TileConverter = new();

    public override TileOrAnchor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String) return new TileOrAnchor(reader.GetString()!);
        return new TileOrAnchor(TileConverter.Read(ref reader, typeof(TileCoord), options));
    }

    public override void Write(Utf8JsonWriter writer, TileOrAnchor value, JsonSerializerOptions options)
    {
        if (value.Anchor is { } a) writer.WriteStringValue(a);
        else TileConverter.Write(writer, value.Tile, options);
    }
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
