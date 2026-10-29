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
}

/// <summary>폭발. 반경은 쏘는 쪽이 주면 그 값, 아니면 여기 radius. 둘 다 없으면 검증 오류.</summary>
public sealed record ExplosionDef
{
    public float DamageMul { get; init; } = 1f;
    public float? Radius { get; init; }
    public EffectRef? Effect { get; init; }
}
