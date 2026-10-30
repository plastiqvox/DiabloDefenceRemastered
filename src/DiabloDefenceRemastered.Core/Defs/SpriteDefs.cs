namespace DiabloDefenceRemastered.Core.Defs;

/// <summary>
/// 그림 출처 (설계서 6.3). atlas = 가져오기 도구가 원작 디아블로 1 에서 만든 페이지(PNG 알파, 프레임마다 잘라 묶음, 변종 파일),
/// grid = 원작 exe PNG·가져오기가 만든 한 장짜리(열 = 방향, 행 = 프레임, 색 키).
/// </summary>
public enum SpriteSource : byte { Grid, Atlas }

/// <summary>
/// 그림 <c>sprites[]</c> (설계서 6.3).
/// <list type="bullet">
/// <item>atlas: <see cref="File"/> = 그림 묶음의 원색 페이지, <see cref="Action"/> = 페이지 메타(<c>Assets/SpriteMeta</c>) 안 동작 이름(동작이 하나면 생략).
/// 시트 크기는 메타의 페이지가 정한다 — <c>sheet</c>·<c>color_key</c>·<c>slack</c> 속성을 적으면 로딩 오류(JsonNode 단계에서 속성 유무로).</item>
/// <item>grid: <see cref="Sheet"/> = 정의끼리 검사용 크기, <see cref="Slack"/> = 남는 픽셀 허용. <c>color_key</c> 속성이 없으면 로더가 검정을 채우고,
/// <c>"color_key": null</c> 을 적으면 PNG 알파를 그대로 쓴다.</item>
/// </list>
/// </summary>
public sealed record SpriteDef : IDef
{
    public required string Id { get; init; }
    public required SpriteSource Source { get; init; }
    /// <summary>Assets 안 파일 이름(<c>d1/monsters/zombie.png</c>, <c>Gold.png</c>).</summary>
    public required string File { get; init; }
    /// <summary>atlas 페이지 메타 안 동작 이름. grid 는 없음.</summary>
    public string? Action { get; init; }
    /// <summary>grid 시트 크기 [w,h](정의끼리 검사 13.5-7). atlas 는 쓰지 않는다.</summary>
    public Int2 Sheet { get; init; }
    /// <summary>칸 = 원작 프레임 폭 × 그 동작의 최대 높이(atlas), 프레임 크기(grid).</summary>
    public Int2 Frame { get; init; }
    public int Directions { get; init; } = 1;
    public int Frames { get; init; } = 1;
    /// <summary>grid 색 키. null = PNG 알파 그대로(atlas 는 늘 null). "속성 없음 → 검정" 은 로더(WP7)가 JsonNode 단계에서 정한다.</summary>
    public Rgb? ColorKey { get; init; }
    /// <summary>칸 안 발 픽셀(잘라 내기 전 칸 기준).</summary>
    public Vector2 Origin { get; init; }
    /// <summary>방향마다 발 위치가 다를 때 덮어씀(방향 수만큼).</summary>
    public IReadOnlyList<Vector2>? OriginByDir { get; init; }
    /// <summary>grid 의 남는 픽셀 허용(가로, 세로).</summary>
    public Int2? Slack { get; init; }
}

/// <summary>적·주인공이 쓰는 클립 묶음 <c>sprite_sets[]</c>. 소리는 두지 않는다(소리 연결 표가 이 ID 로 가리킨다, 6.15).</summary>
public sealed record SpriteSetDef : IDef
{
    public required string Id { get; init; }
    /// <summary>걷기 재생 속도 기준. 재생 배율 = 땅 기준 속도 |(vx, 2·vy)| / base_speed(4.1).</summary>
    public float BaseSpeed { get; init; }
    /// <summary>크기 배율 1 일 때 몸 반경. 적 정의가 radius 를 생략하면 round(base_radius × scale).</summary>
    public float BaseRadius { get; init; }
    public float FrameTime { get; init; } = 0.06f;
    /// <summary>이 묶음에 쓰는 원작 TRN 변종 이름(<c>""</c> = 원색). 적 정의의 <c>variant</c> 는 이 안에 있어야 한다.</summary>
    public IReadOnlyList<string> Variants { get; init; } = [""];
    public required ClipSetDef Clips { get; init; }
}

/// <summary>
/// 클립 이름별 그림. 원작 클립 이름: idle(n) walk(w) attack(a) special(s) die(d), 주인공 cast_fire(fm) cast_lightning(lm) cast_magic(qm).
/// 쓰는 클립이 없는 적·무기는 검증 오류(6.13·6.14). 맞기(h)는 쓰지 않는다.
/// </summary>
public sealed record ClipSetDef
{
    public ClipDef? Idle { get; init; }
    public ClipDef? Walk { get; init; }
    public ClipDef? Attack { get; init; }
    /// <summary>몬스터의 시전·함성·소환(원작 특수 그림). v0.2 의 cast.</summary>
    public ClipDef? Special { get; init; }
    public ClipDef? Die { get; init; }
    public ClipDef? CastFire { get; init; }
    public ClipDef? CastLightning { get; init; }
    public ClipDef? CastMagic { get; init; }

    public ClipDef? Get(ClipKind kind) => kind switch
    {
        ClipKind.Idle => Idle,
        ClipKind.Walk => Walk,
        ClipKind.Attack => Attack,
        ClipKind.Special => Special,
        ClipKind.Die => Die,
        ClipKind.CastFire => CastFire,
        ClipKind.CastLightning => CastLightning,
        ClipKind.CastMagic => CastMagic,
        _ => null,
    };
}

public sealed record ClipDef
{
    public required DefRef<SpriteDef> Sprite { get; init; }
    /// <summary>타격·발사 프레임 기본값(1부터, 적 정의가 덮어씀).</summary>
    public int? EventFrame { get; init; }
    /// <summary>원작 동작별 재생 간격 × 50ms. 없으면 묶음의 frame_time.</summary>
    public float? FrameTime { get; init; }
}
