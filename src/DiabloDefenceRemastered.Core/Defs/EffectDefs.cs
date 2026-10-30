namespace DiabloDefenceRemastered.Core.Defs;

public enum BlendKind : byte { Alpha, Additive }

/// <summary>표시 이펙트 <c>effects[]</c> (설계서 6.4). 크기는 scale(고정 배율) 또는 fit_radius(배율 1 일 때 덮는 반경) 중 하나.</summary>
public sealed record EffectDef : IDef
{
    public required string Id { get; init; }
    public DefRef<SpriteDef>? Sprite { get; init; }
    /// <summary>[처음, 끝] 프레임 번호(1부터, 끝 포함).</summary>
    public int[]? Frames { get; init; }
    public bool Loop { get; init; }
    public float FrameTime { get; init; } = 0.06f;
    public Rgb Tint { get; init; } = Rgb.White;
    public float? Scale { get; init; }
    public float? FitRadius { get; init; }
    public float Alpha { get; init; } = 1f;
    public BlendKind Blend { get; init; } = BlendKind.Alpha;
    public EffectLightDef? Light { get; init; }
    public ShakeDef? Shake { get; init; }
    /// <summary>코드로 그리는 예고(<c>"warning_circle"</c>, <c>"warning_line"</c>). 있으면 sprite 없음.</summary>
    public string? Procedural { get; init; }
    /// <summary>반복 이펙트의 "솟음" 구간: 이 프레임을 한 번 재생한 뒤 본 sprite·frames 를 반복한다(화염벽 firewal1 → firewal2).</summary>
    public EffectIntroDef? Intro { get; init; }
    /// <summary>장판 안에 조각 여러 개 찍기(표시만, 시뮬레이션과 무관).</summary>
    public ScatterDef? Scatter { get; init; }
}

public sealed record EffectIntroDef
{
    public required DefRef<SpriteDef> Sprite { get; init; }
    /// <summary>[처음, 끝] 프레임 번호(1부터, 끝 포함). 없으면 그림 전체.</summary>
    public int[]? Frames { get; init; }
}

/// <summary>조각 수 = max(min, round(반경 / per_radius)) + (center 면 가운데 1). 자리·시작 프레임 = StableHash(장판 ID, i).</summary>
public sealed record ScatterDef
{
    public float PerRadius { get; init; }
    public int Min { get; init; }
    public bool Center { get; init; }
}

public sealed record EffectLightDef
{
    public Rgb Color { get; init; } = Rgb.White;
    public float RadiusMul { get; init; } = 1f;
    /// <summary>없으면 이펙트가 살아 있는 동안.</summary>
    public float? Duration { get; init; }
}

/// <summary>투사체 <c>projectiles[]</c> (설계서 6.4, 기획 13.7). 반경 = 맞음 판정 원.</summary>
public sealed record ProjectileDef : IDef
{
    public required string Id { get; init; }
    public required DefRef<SpriteDef> Sprite { get; init; }
    public Rgb Tint { get; init; } = Rgb.White;
    public float Scale { get; init; } = 1f;
    public BlendKind Blend { get; init; } = BlendKind.Alpha;
    public float Radius { get; init; }
    /// <summary>없으면 쏘는 쪽(무기 레벨 표)이 준다.</summary>
    public float? Speed { get; init; }
    /// <summary>유도 회전율(°/s, 0 = 직선).</summary>
    public float TurnRate { get; init; }
    /// <summary>사거리(px). lifetime 과 둘 중 하나 이상.</summary>
    public float? Range { get; init; }
    public float? Lifetime { get; init; }
    public bool BlockedByObstacles { get; init; } = true;
    public int Pierce { get; init; } = 1;
    public ExplosionDef? Explosion { get; init; }
    public EffectRef? HitEffect { get; init; }
    /// <summary>투사체가 끝날 때(맞음·사거리 끝·장애물) 그 자리에 남기는 장판(산성 침). 피해는 쏜 적의 배율로 발사 순간에 굳힌다(5.2). 처리 WP3.</summary>
    public GroundHazardDef? OnEnd { get; init; }
}

/// <summary>
/// 장판 명세(<c>ground_hazard</c> 부품과 같은 모양): 이펙트·반경·지속·틱 간격·틱 피해·동시 상한 묶음.
/// WP4 의 <c>ground_hazard</c> 부품 매개변수는 이 record 를 그대로 쓰거나 같은 키 이름을 쓴다(설계서 11.3).
/// </summary>
public sealed record GroundHazardDef
{
    public EffectRef? Effect { get; init; }
    public float Radius { get; init; }
    public float Duration { get; init; }
    /// <summary>틱 간격(초).</summary>
    public float Tick { get; init; }
    /// <summary>틱마다 피해(기본 피해, 배율 전).</summary>
    public float Damage { get; init; }
    public string? CapGroup { get; init; }
    public int Cap { get; init; }
}

/// <summary>폭발. 반경은 쏘는 쪽이 주면 그 값, 아니면 여기 radius. 둘 다 없으면 검증 오류.</summary>
public sealed record ExplosionDef
{
    public float DamageMul { get; init; } = 1f;
    public float? Radius { get; init; }
    public EffectRef? Effect { get; init; }
}
