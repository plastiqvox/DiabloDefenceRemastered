using System.Text.Json;
using System.Text.Json.Serialization;
using DiabloDefenceRemastered.Core.Data.Json;

namespace DiabloDefenceRemastered.Core.Assets;

/// <summary>
/// 아틀라스 메타 <c>&lt;이름&gt;.json</c> (설계서 6.3, 계약 WP1b) — 가져오기 도구가 쓰고 렌더·시험이 읽는다. 변종 페이지 모두가 같은 메타를 쓴다
/// (TRN 은 색만 바꾸고 투명은 RLE 가 정하므로 잘라 낸 사각형이 같다).
/// <code>
/// { "format": 2, "pages": [[W,H], …],
///   "actions": { "walk": { "cell": [w,h], "directions": d, "frames": f, "rects": [[p, x, y, w, h, ox, oy], …] }, … } }
/// </code>
/// <c>rects</c> 순서 = (방향 0 의 프레임 1..f, 방향 1 …). <c>p</c> = 페이지 번호(0 = <c>&lt;이름&gt;.png</c>, 1 = <c>&lt;이름&gt;_p2.png</c> …),
/// <c>x,y,w,h</c> = 페이지 안 사각형, <c>ox,oy</c> = 칸 왼쪽 위에서 그 사각형까지. 빈 프레임은 w = h = 0.
/// 그리기: 화면 왼쪽 위 = 위치 − origin × scale + (ox, oy) × scale, 원본 사각형 = (x, y, w, h).
/// </summary>
public sealed record SpriteMeta
{
    /// <summary>메타 형식 번호. 다르면 읽는 쪽이 오류로 본다.</summary>
    public const int CurrentFormat = 2;

    public int Format { get; init; }
    /// <summary>페이지 크기(가로·세로 ≤ 4096).</summary>
    public IReadOnlyList<Int2> Pages { get; init; } = [];
    /// <summary>동작 이름 → 칸·방향·프레임·사각형.</summary>
    public IReadOnlyDictionary<string, SpriteMetaAction> Actions { get; init; } = new Dictionary<string, SpriteMetaAction>();

    public static SpriteMeta Read(string path) => AssetJson.Read<SpriteMeta>(path);
    public static SpriteMeta Parse(string json) => AssetJson.Deserialize<SpriteMeta>(json);

    /// <summary>정의의 <c>action</c> 으로 동작을 찾는다. 이름이 없으면(마법) 동작이 하나일 때만 그것.</summary>
    public bool TryGetAction(string? name, out SpriteMetaAction action)
    {
        if (name is not null) return TryGet(name, out action);
        if (Actions.Count == 1) { action = Actions.Values.First(); return true; }
        action = null!;
        return false;
    }

    private bool TryGet(string name, out SpriteMetaAction action)
    {
        if (Actions.TryGetValue(name, out var a)) { action = a; return true; }
        action = null!;
        return false;
    }

    /// <summary>
    /// 검사(10.1 Assets): 형식 판, 페이지 크기(1..4096), 동작마다 칸·방향·프레임 &gt; 0, 사각형 수 = 방향 × 프레임,
    /// 없는 페이지 번호, 사각형이 페이지 밖, 잘라 낸 사각형이 칸 밖(ox,oy 포함). 문제가 없으면 빈 목록.
    /// </summary>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (Format != CurrentFormat) errors.Add($"메타 형식 {Format} ≠ {CurrentFormat}");
        if (Pages.Count == 0) errors.Add("페이지가 없다");
        for (var p = 0; p < Pages.Count; p++)
            if (Pages[p].X is <= 0 or > 4096 || Pages[p].Y is <= 0 or > 4096)
                errors.Add($"페이지 {p} 크기 {Pages[p]} 가 1..4096 밖이다");
        if (Actions.Count == 0) errors.Add("동작이 없다");

        foreach (var (name, a) in Actions.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            if (a.Cell.X <= 0 || a.Cell.Y <= 0) errors.Add($"{name}: 칸 {a.Cell} 이 비었다");
            if (a.Directions <= 0 || a.Frames <= 0) { errors.Add($"{name}: 방향 {a.Directions}·프레임 {a.Frames} 는 1 이상"); continue; }
            if (a.Rects.Count != a.Directions * a.Frames)
                errors.Add($"{name}: 사각형 {a.Rects.Count}개 ≠ 방향 {a.Directions} × 프레임 {a.Frames}");
            for (var i = 0; i < a.Rects.Count; i++)
            {
                var r = a.Rects[i];
                var at = $"{name} rects[{i}]";
                if (r.W < 0 || r.H < 0) { errors.Add($"{at}: 크기가 음수다"); continue; }
                if (r.W == 0 && r.H == 0) continue;   // 빈 프레임
                if (r.W == 0 || r.H == 0) { errors.Add($"{at}: 폭·높이 중 하나만 0 이다"); continue; }
                if ((uint)r.Page >= (uint)Pages.Count) { errors.Add($"{at}: 없는 페이지 {r.Page}"); continue; }
                var page = Pages[r.Page];
                if (r.X < 0 || r.Y < 0 || r.X + r.W > page.X || r.Y + r.H > page.Y)
                    errors.Add($"{at}: 사각형 ({r.X},{r.Y},{r.W},{r.H}) 이 페이지 {r.Page} {page} 밖이다");
                if (r.Ox < 0 || r.Oy < 0 || r.Ox + r.W > a.Cell.X || r.Oy + r.H > a.Cell.Y)
                    errors.Add($"{at}: 잘라 낸 자리 ({r.Ox},{r.Oy},{r.W},{r.H}) 가 칸 {a.Cell} 밖이다");
            }
        }
        return errors;
    }

    // ── 파일 이름 규칙(1.4 출력) ──────────────────────────────────────

    /// <summary>
    /// 페이지 파일: <c>d1/monsters/zombie.png</c> 를 기준으로 페이지 0 = <c>zombie.png</c>, 페이지 1 = <c>zombie_p2.png</c>,
    /// 변종이면 <c>zombie@bluered.png</c>, <c>zombie_p2@bluered.png</c>.
    /// </summary>
    /// <param name="baseFile">정의의 <c>file</c>(원색 첫 페이지, <c>.png</c>).</param>
    public static string PageFile(string baseFile, int page, string variant = "")
    {
        var stem = Stem(baseFile);
        var pagePart = page == 0 ? "" : $"_p{page + 1}";
        var variantPart = variant.Length == 0 ? "" : "@" + variant;
        return $"{stem}{pagePart}{variantPart}.png";
    }

    /// <summary>메타 파일: <c>d1/monsters/zombie.png</c> → <c>d1/monsters/zombie.json</c>.</summary>
    public static string MetaFile(string baseFile) => Stem(baseFile) + ".json";

    private static string Stem(string baseFile) =>
        baseFile.EndsWith(".png", StringComparison.Ordinal) ? baseFile[..^4] : baseFile;
}

public sealed record SpriteMetaAction
{
    /// <summary>칸 = 원작 프레임 폭 × 그 동작의 최대 높이. 정의의 <c>frame</c> 과 같아야 한다(13.5-7).</summary>
    public Int2 Cell { get; init; }
    public int Directions { get; init; }
    public int Frames { get; init; }
    public IReadOnlyList<SpriteRect> Rects { get; init; } = [];

    /// <summary>방향 <paramref name="dir"/>(0부터)의 프레임 <paramref name="frame1"/>(<b>1부터</b>, 6.1) 사각형.</summary>
    public SpriteRect Rect(int dir, int frame1)
    {
        if ((uint)dir >= (uint)Directions) throw new ArgumentOutOfRangeException(nameof(dir), dir, $"방향은 0..{Directions - 1}");
        if (frame1 < 1 || frame1 > Frames) throw new ArgumentOutOfRangeException(nameof(frame1), frame1, $"프레임은 1..{Frames}");
        return Rects[dir * Frames + frame1 - 1];
    }
}

/// <summary>
/// 잘라 낸 프레임 하나. JSON <c>[p, x, y, w, h, ox, oy]</c>. 빈 프레임은 w = h = 0.
/// </summary>
[JsonConverter(typeof(SpriteRectJsonConverter))]
public readonly record struct SpriteRect(int Page, int X, int Y, int W, int H, int Ox, int Oy)
{
    public bool IsEmpty => W == 0 || H == 0;
}

public sealed class SpriteRectJsonConverter : JsonConverter<SpriteRect>
{
    private const string What = "p,x,y,w,h,ox,oy";

    public override SpriteRect Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        DefJson.BeginArray(ref reader, What);
        var r = new SpriteRect(
            DefJson.ReadInt(ref reader, What), DefJson.ReadInt(ref reader, What), DefJson.ReadInt(ref reader, What),
            DefJson.ReadInt(ref reader, What), DefJson.ReadInt(ref reader, What), DefJson.ReadInt(ref reader, What),
            DefJson.ReadInt(ref reader, What));
        DefJson.EndArray(ref reader, What);
        return r;
    }

    public override void Write(Utf8JsonWriter writer, SpriteRect v, JsonSerializerOptions options)
    {
        // 한 줄에(들여쓰기 옵션이어도 7개 숫자가 줄마다 흩어지지 않게) — 결정적 바이트
        writer.WriteRawValue(string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"[{v.Page},{v.X},{v.Y},{v.W},{v.H},{v.Ox},{v.Oy}]"), skipInputValidation: true);
    }
}
