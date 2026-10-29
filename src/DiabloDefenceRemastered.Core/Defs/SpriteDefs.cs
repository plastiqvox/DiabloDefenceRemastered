namespace DiabloDefenceRemastered.Core.Defs;

/// <summary>그림 시트 <c>sprites[]</c> (설계서 6.3). 열 = 방향, 행 = 프레임.</summary>
public sealed record SpriteDef : IDef
{
    public required string Id { get; init; }
    /// <summary>Assets 안 파일 이름.</summary>
    public required string File { get; init; }
    /// <summary>시트 크기 [w,h](정의끼리 검사 13.5-7).</summary>
    public Int2 Sheet { get; init; }
    public Int2 Frame { get; init; }
    public int Directions { get; init; } = 1;
    public int Frames { get; init; } = 1;
    public Rgb ColorKey { get; init; } = Rgb.Black;
    /// <summary>프레임 안 발 픽셀.</summary>
    public Vector2 Origin { get; init; }
    /// <summary>방향마다 발 위치가 다를 때 덮어씀(방향 수만큼).</summary>
    public IReadOnlyList<Vector2>? OriginByDir { get; init; }
    /// <summary>남는 픽셀 허용(가로, 세로).</summary>
    public Int2? Slack { get; init; }
}

/// <summary>적·주인공이 쓰는 클립 묶음 <c>sprite_sets[]</c>.</summary>
public sealed record SpriteSetDef : IDef
{
    public required string Id { get; init; }
    /// <summary>걷기 재생 속도 기준(재생 배율 = 실제 속도 / base_speed).</summary>
    public float BaseSpeed { get; init; }
    /// <summary>크기 배율 1 일 때 몸 반경. 적 정의가 radius 를 생략하면 round(base_radius × scale).</summary>
    public float BaseRadius { get; init; }
    public float FrameTime { get; init; } = 0.06f;
    public required ClipSetDef Clips { get; init; }
}

/// <summary>클립 이름별 시트. 없는 클립을 쓰는 적은 검증 오류(6.13).</summary>
public sealed record ClipSetDef
{
    public ClipDef? Idle { get; init; }
    public ClipDef? Walk { get; init; }
    public ClipDef? Attack { get; init; }
    public ClipDef? Cast { get; init; }
    public ClipDef? Die { get; init; }

    public ClipDef? Get(ClipKind kind) => kind switch
    {
        ClipKind.Idle => Idle,
        ClipKind.Walk => Walk,
        ClipKind.Attack => Attack,
        ClipKind.Cast => Cast,
        ClipKind.Die => Die,
        _ => null,
    };
}

public sealed record ClipDef
{
    public required DefRef<SpriteDef> Sprite { get; init; }
    /// <summary>타격·발사 프레임 기본값(1부터, 적 정의가 덮어씀).</summary>
    public int? EventFrame { get; init; }
    /// <summary>없으면 묶음의 frame_time.</summary>
    public float? FrameTime { get; init; }
}
