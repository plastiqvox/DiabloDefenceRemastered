using System.Text.Json;
using System.Text.Json.Serialization;
using DiabloDefenceRemastered.Core.Data.Json;

namespace DiabloDefenceRemastered.Core.Defs;

/// <summary>
/// 스테이지 <c>stages[]</c> (설계서 6.12) — v0.7(사용자 결정 R): 스테이지 정의 하나 = <b>액트</b> 하나. 맵 하나(<c>map</c>)를 방 순번 표(<see cref="Rooms"/>)·방 맵 풀(<see cref="RoomMaps"/>)·
/// 문 보상(<see cref="Rewards"/>)으로, 해금(<c>unlock</c>)을 다음 액트(<see cref="NextAct"/>)로 바꿨고 <c>victory</c> 는 없앴다(판의 끝 = 마지막 액트의 마지막 방, 3.6).
/// 생성 구간·이벤트·배율의 시각은 난이도 분 t 다. 장애물은 맵의 타일 배치에 들어가므로 스테이지에는 없다.
/// </summary>
public sealed record StageDef : IDef
{
    public required string Id { get; init; }
    public string Name { get; init; } = "";
    /// <summary>방 순번 표(v0.7). 순번 = 배열 순서(씨앗으로 섞지 않음 — 기획 2.5). 보이는 번호는 <see cref="RoomDef.Number"/>.</summary>
    public IReadOnlyList<RoomDef> Rooms { get; init; } = [];
    /// <summary>방 종류별 맵 풀(v0.7). 고르기는 <c>RunPlanner</c>(3.6).</summary>
    public RoomMapPoolDef RoomMaps { get; init; } = new();
    /// <summary>문 보상 가중·1번 방 보상·금화 배율(v0.7, 6.18).</summary>
    public StageRewardsDef Rewards { get; init; } = new();
    /// <summary>
    /// 보스 방 뒤 계단이 가는 액트(v0.7 — v0.6 의 <c>unlock</c> 을 대신함). 마지막 액트는 null.
    /// <c>requires</c> 가 빈 액트는 <c>requires</c> 가 빈 액트만 가리킨다(13.5-20).
    /// </summary>
    public DefRef<StageDef>? NextAct { get; init; }
    /// <summary>단계 1 세로 조각 판(v0.7, 기본 거짓). 참이면 13.5-23 의 "일반 방 6~8"·"보스 방"·"t 범위 빈틈" 셋을 끈다(6.12).</summary>
    public bool Slice { get; init; }
    /// <summary>
    /// 이 스테이지가 쓰는 자원 묶음(v0.6, 6.17). 묶음 MPQ 가 없는 PC 에서는 스테이지가 통째로 꺼진다(<c>ContentAvailability.MissingFor</c>).
    /// 맵에는 <c>requires</c> 가 없다 — 맵 그림의 묶음은 타일셋 표 항목의 <c>bundle</c> 이고, 이 목록이 그것을 덮는지는 13.5-10 묶음판이 본다.
    /// 음악 묶음(<c>hfmusic</c>)은 넣지 않는다 — 음악은 꺼지지 않고 <c>fallback</c> 으로 바뀐다.
    /// </summary>
    public IReadOnlyList<ContentBundle> Requires { get; init; } = [];
    public LightingDef Lighting { get; init; } = new();
    public StageScaling Scaling { get; init; } = new();
    public EliteChanceDef? EliteChance { get; init; }
    public IReadOnlyList<SpawnIntervalDef> SpawnIntervals { get; init; } = [];

    /// <summary>
    /// 이벤트. 원소마다 <c>at</c>·<c>type</c> + 매개변수. <c>at</c> 은 t(난이도 분)이고 로더(WP7)가 <see cref="PartDef.At"/> 로 꺼내 둔다(v0.7).
    /// 방으로 옮기기는 <c>RoomEvents.Schedule</c>(3.6).
    /// </summary>
    [PartKind(PartKind.StageEvent)]
    public IReadOnlyList<PartDef> Events { get; init; } = [];
}

/// <summary>방 맵 풀(v0.7, 6.12). 순번 표에 있는 종류마다 맵이 하나 이상(13.5-23). 풀의 맵은 <see cref="MapDef.RoomKinds"/> 에 그 종류를 담는다.</summary>
public sealed record RoomMapPoolDef
{
    public IReadOnlyList<DefRef<MapDef>> Normal { get; init; } = [];
    public IReadOnlyList<DefRef<MapDef>> Elite { get; init; } = [];
    /// <summary>보스 방 맵 하나. <c>slice</c> 판(단계 1)은 없다.</summary>
    public DefRef<MapDef>? Boss { get; init; }
}

/// <summary>문 보상(v0.7, 6.12·6.18).</summary>
public sealed record StageRewardsDef
{
    /// <summary>보상 → 가중(JSON 은 객체, 로딩 때 적힌 순서의 배열 — 3.4). 보상 후보 순서는 이것의 가중 비복원 순열(<c>RunPlanner</c>).</summary>
    public IReadOnlyList<WeightEntry<RewardDef>> Weights { get; init; } = [];
    /// <summary>액트 첫 방(배열 자리 0)의 보상(액트 1 마법서·액트 2 보물 상자).</summary>
    public DefRef<RewardDef>? FirstRoom { get; init; }
    /// <summary>보물 상자 금화 배율(액트 2 1.5).</summary>
    public float GoldScale { get; init; } = 1f;
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

/// <summary>배율 식 [a, b] = a + b × t(난이도 분 — v0.7, v0.6 은 경과 분. 기획 2.3).</summary>
public sealed record StageScaling
{
    public float[] Hp { get; init; } = [1f, 0f];
    public float[] Damage { get; init; } = [1f, 0f];
}

/// <summary>
/// 정예 무리 판정(v0.7 — <c>every</c> 를 없애고 <see cref="At"/> 을 더함): t 시작 ≥ <see cref="From"/> 인 일반 방마다 방 시계 <see cref="At"/> 에 한 번(기획 2.3).
/// 정예·보스 방에서는 하지 않는다.
/// </summary>
public sealed record EliteChanceDef
{
    /// <summary>판정을 하는 방의 t 시작 하한.</summary>
    public ClockTime From { get; init; }
    /// <summary>방 시계의 판정 시각(기본 0:30).</summary>
    public ClockTime At { get; init; } = new(30f);
    public float Chance { get; init; }
}

/// <summary>
/// 생성 구간. <c>from</c>/<c>to</c>(t, v0.7) 또는 <c>during: "boss"</c>(보스 방에서 보스가 나온 뒤).
/// 감독은 <see cref="MaxAlive"/> 에 <c>World.Room.AreaScale</c> 을 곱해(반올림) 쓴다 — <see cref="PerSecond"/> 는 곱하지 않는다(6.12).
/// </summary>
public sealed record SpawnIntervalDef
{
    public ClockTime? From { get; init; }
    public ClockTime? To { get; init; }
    public string? During { get; init; }
    public int MaxAlive { get; init; }
    public float PerSecond { get; init; }
    public IReadOnlyList<WeightEntry<EnemyDef>> Weights { get; init; } = [];
}
