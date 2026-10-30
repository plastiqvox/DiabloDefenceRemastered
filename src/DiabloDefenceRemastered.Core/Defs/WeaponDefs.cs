using System.Text.Json;
using System.Text.Json.Serialization;

namespace DiabloDefenceRemastered.Core.Defs;

/// <summary>
/// 무기 <c>weapons[]</c> (설계서 6.8). 레벨 표는 열 방식(키마다 길이 max_level 배열, 값 float?).
/// null 은 패턴의 NullableKeys 에서만: duration null = 상시, cooldown null = 발동 없음.
/// 진화 무기는 <see cref="BaseWeapon"/> 의 최고 레벨 값 위에 <see cref="Override"/> 와 추가 효과를 얹고 레벨 표가 없다.
/// </summary>
public sealed record WeaponDef : IDef
{
    public required string Id { get; init; }
    public string Name { get; init; } = "";
    public IconDef? Icon { get; init; }
    /// <summary>발동할 때 주인공이 재생할 시전 그림(기획 5.1). cast_fire / cast_lightning / cast_magic.</summary>
    public ClipKind CastClip { get; init; } = ClipKind.CastMagic;
    /// <summary>
    /// 발사 패턴 type(<see cref="PartKind.FirePattern"/> 등록 이름). 진화 무기는 비워 재료 무기 것을 쓴다.
    /// 로더는 등록 여부만 검사하고, 레벨 표 키·단조성(13.5-2)은 WP3 검사기가 본다.
    /// </summary>
    [PartType(PartKind.FirePattern)]
    public string? Pattern { get; init; }

    [PartKind(PartKind.Targeting)]
    public PartDef? Targeting { get; init; }
    public DefRef<ProjectileDef>? Projectile { get; init; }
    /// <summary>장판 패턴처럼 투사체 대신 이펙트를 쓰는 무기.</summary>
    public EffectRef? Effect { get; init; }
    public int MaxLevel { get; init; }
    /// <summary>레벨 무관 고정값(turn_rate, lifetime, burst_interval, knockback, hit_interval, rotation …).</summary>
    public IReadOnlyDictionary<string, float> Base { get; init; } = new Dictionary<string, float>();
    public IReadOnlyDictionary<string, float?[]> Levels { get; init; } = new Dictionary<string, float?[]>();

    public DefRef<WeaponDef>? BaseWeapon { get; init; }
    public WeaponOverrideDef? Override { get; init; }

    [PartKind(PartKind.WeaponEffect)]
    public IReadOnlyList<PartDef> OnHit { get; init; } = [];
    [PartKind(PartKind.WeaponEffect)]
    public IReadOnlyList<PartDef> OnExplode { get; init; } = [];
    [PartKind(PartKind.WeaponEffect)]
    public IReadOnlyList<PartDef> OnCrit { get; init; } = [];
}

/// <summary>
/// 진화 무기의 덮어쓰기. 투사체·이펙트 참조는 이름 있는 속성, 나머지 수치 키(레벨 표 키·base 키)는 <see cref="Values"/> 에 모인다.
/// 받을 수 있는 수치 키인지는 WP3 이 검사한다.
/// </summary>
public sealed record WeaponOverrideDef
{
    public DefRef<ProjectileDef>? Projectile { get; init; }
    public EffectRef? ExplosionEffect { get; init; }
    public EffectRef? HitEffect { get; init; }
    public EffectRef? Effect { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Values { get; init; }
}

/// <summary>패시브 <c>passives[]</c>. per_level 키 ∈ <see cref="StatKey"/>(13.5-9).</summary>
public sealed record PassiveDef : IDef
{
    public required string Id { get; init; }
    public string Name { get; init; } = "";
    public IconDef? Icon { get; init; }
    public int MaxLevel { get; init; }
    public IReadOnlyDictionary<StatKey, float> PerLevel { get; init; } = new Dictionary<StatKey, float>();
    public OnAcquireDef? OnAcquire { get; init; }
}

public sealed record OnAcquireDef
{
    public float Heal { get; init; }
    public int Gold { get; init; }
}

/// <summary>진화 <c>evolutions[]</c>. JSON 에 id 가 없어 결과 무기 ID 를 ID 로 쓴다.</summary>
public sealed record EvolutionDef : IDef
{
    public required DefRef<WeaponDef> Weapon { get; init; }
    public required DefRef<PassiveDef> Passive { get; init; }
    public required DefRef<WeaponDef> Result { get; init; }

    [JsonIgnore]
    public string Id => Result.Id;
}

public enum PickupKind : byte { Xp, Gold, Heal, Surge, Magnet, Chest }

/// <summary>획득물 <c>pickups[]</c> (기획 11.1). 경험치 보석은 드롭 표가 아니라 규칙(죽으면 항상 1개).</summary>
public sealed record PickupDef : IDef
{
    public required string Id { get; init; }
    public PickupKind Kind { get; init; }
    public required PickupDrawDef Draw { get; init; }
    /// <summary>[최소, 최대] — 경험치 보석 색 고르기(값 범위).</summary>
    public int[]? ValueRange { get; init; }
    /// <summary>획득 반경·끌어당김 룬에 반응하는지. false 면 몸에 닿아야 먹힌다.</summary>
    public bool Magnet { get; init; }
    public bool SkipWhenFull { get; init; }
    /// <summary>효과량(회복량, 쿨 감소 비율 …).</summary>
    public float? Amount { get; init; }
    public float? Duration { get; init; }
}

public sealed record PickupDrawDef
{
    /// <summary>코드 그림(<c>"gem"</c>). 있으면 sprite 없음.</summary>
    public string? Procedural { get; init; }
    public Int2? Size { get; init; }
    public Rgb? Color { get; init; }
    public DefRef<SpriteDef>? Sprite { get; init; }
    public float Scale { get; init; } = 1f;
    public Rgb? Tint { get; init; }
    /// <summary>멈춰 둘 프레임(1부터). 없으면 재생 후 마지막 프레임.</summary>
    public int? Frame { get; init; }
}

/// <summary>드롭 표 <c>drop_tables[]</c>. elite_chest 의 value 는 강화 횟수(설계서 6.9).</summary>
public sealed record DropTableDef : IDef
{
    public required string Id { get; init; }
    public IReadOnlyList<DropEntryDef> Entries { get; init; } = [];
}

public sealed record DropEntryDef
{
    public required DefRef<PickupDef> Pickup { get; init; }
    public float Chance { get; init; }
    /// <summary>[최소, 최대].</summary>
    public int[]? Value { get; init; }
}
