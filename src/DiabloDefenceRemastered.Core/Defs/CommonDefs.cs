using System.Globalization;

namespace DiabloDefenceRemastered.Core.Defs;

// 여러 정의가 같이 쓰는 작은 조각. JSON 표기는 설계서 6.1(키 snake_case, 색 [r,g,b], 점 [x,y], 사각형 [x0,y0,x1,y1], 시각 "mm:ss").

/// <summary>데이터의 시각 <c>"mm:ss"</c>(초 단위로 담는다). 변환기는 WP7, 읽기 규칙은 <see cref="Parse"/>.</summary>
public readonly record struct ClockTime(float Seconds)
{
    public float Minutes => Seconds / 60f;

    public static bool TryParse(string text, out ClockTime time)
    {
        time = default;
        var colon = text.IndexOf(':');
        if (colon <= 0 || colon == text.Length - 1) return false;
        if (!int.TryParse(text.AsSpan(0, colon), NumberStyles.None, CultureInfo.InvariantCulture, out var mm)) return false;
        if (!float.TryParse(text.AsSpan(colon + 1), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var ss) || ss >= 60f) return false;
        time = new ClockTime(mm * 60f + ss);
        return true;
    }

    public static ClockTime Parse(string text) =>
        TryParse(text, out var t) ? t : throw new FormatException($"시각은 \"mm:ss\" 꼴이어야 한다: '{text}'");

    public override string ToString()
    {
        var total = (int)MathF.Floor(Seconds);
        return $"{total / 60:00}:{total % 60:00}";
    }
}

/// <summary>
/// 이펙트 참조. JSON 은 ID 문자열(<c>"fx_spark"</c>) 또는 <c>{ "effect": …, "tint": …, "scale": … }</c>.
/// 참조하는 쪽은 색조·크기만 덮어쓸 수 있다(기획 13.7, 다른 키는 오류).
/// </summary>
public sealed record EffectRef
{
    public required DefRef<EffectDef> Effect { get; init; }
    public Rgb? Tint { get; init; }
    public float? Scale { get; init; }
}

/// <summary>예고 <c>{ "effect": …, "time": … }</c>.</summary>
public sealed record TelegraphDef
{
    public required DefRef<EffectDef> Effect { get; init; }
    public float Time { get; init; }
}

/// <summary>화면 흔들림 <c>{ "px": …, "duration": … }</c>.</summary>
public sealed record ShakeDef
{
    public float Px { get; init; }
    public float Duration { get; init; }
}

/// <summary>발광 <c>{ "color": [r,g,b], "alpha": … }</c>.</summary>
public sealed record GlowDef
{
    public Rgb Color { get; init; } = Rgb.White;
    public float Alpha { get; init; }
}

/// <summary>아이콘 <c>{ sprite, frame?, dir?, tint?, scale?, layers?, frame_border? }</c> (설계서 6.8).</summary>
public sealed record IconDef
{
    public required DefRef<SpriteDef> Sprite { get; init; }
    /// <summary>프레임 번호(1부터).</summary>
    public int? Frame { get; init; }
    public int? Dir { get; init; }
    public Rgb? Tint { get; init; }
    public float? Scale { get; init; }
    /// <summary>겹쳐 그릴 같은 그림(화살 3장 겹침).</summary>
    public IReadOnlyList<IconLayerDef> Layers { get; init; } = [];
    /// <summary>테두리 종류(<c>"gold"</c> = 진화 무기).</summary>
    public string? FrameBorder { get; init; }
}

public sealed record IconLayerDef
{
    public Vector2 Offset { get; init; }
    public float? Rotation { get; init; }
    public Rgb? Tint { get; init; }
    public float? Scale { get; init; }
}

/// <summary>
/// 도형 하나. 다음 중 하나만 쓴다: <c>{ "polygon": [[x,y]…] }</c>, <c>{ "rect": [x0,y0,x1,y1] }</c>,
/// <c>{ "circle": [cx,cy,r] }</c>, <c>{ "circle_at": "기준점 ID", "r": … }</c>. 기준점 풀기·기하는 WP2.
/// </summary>
public sealed record ShapeDef
{
    public IReadOnlyList<Vector2>? Polygon { get; init; }
    public RectI? Rect { get; init; }
    /// <summary>[cx, cy, r]</summary>
    public float[]? Circle { get; init; }
    public string? CircleAt { get; init; }
    public float? R { get; init; }
}

/// <summary>
/// 가중치 한 칸. JSON 은 객체(<c>{ "skeleton": 100, "zombie": 30 }</c>)지만 로딩 때 적힌 순서의 배열로 바뀐다 (설계서 3.4).
/// </summary>
public readonly record struct WeightEntry<T>(DefRef<T> Ref, float Weight) where T : class, IDef;
