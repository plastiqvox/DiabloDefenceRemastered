using FontStashSharp;

namespace DiabloDefenceRemastered.Loading;

/// <summary>
/// 글꼴 (설계서 8.2 "글꼴", 소유 WP5): <c>C:\Windows\Fonts\malgun.ttf</c> → <c>malgunbd.ttf</c> 순으로 찾고, 모두 없으면 <c>arial.ttf</c> + 로그 경고(한글이 네모로 보임).
/// 글꼴 묶음(.ttc)은 <c>FontSystem.AddFont(byte[])</c> 에 몇 번째 글꼴인지 고를 인자가 없어 쓰지 않는다. 크기는 부르는 쪽이 가상 크기 × 배율로 요청한다(늘 선명 — 7.3).
/// </summary>
public static class FontLoader
{
    public static readonly string[] Preferred = ["malgun.ttf", "malgunbd.ttf"];
    public const string Fallback = "arial.ttf";

    /// <param name="loaded">글꼴이 하나라도 들어갔나(FontStashSharp 는 빈 FontSystem 에서 글자를 그리면 던진다 — 부르는 쪽이 그리기를 건너뛴다).</param>
    public static FontSystem Create(IAppLog? log, out bool loaded)
    {
        var system = new FontSystem();
        loaded = true;
        var dir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        foreach (var name in Preferred)
        {
            var path = Path.Combine(dir, name);
            if (!File.Exists(path)) continue;
            system.AddFont(File.ReadAllBytes(path));
            return system;
        }
        var fallback = Path.Combine(dir, Fallback);
        if (File.Exists(fallback))
        {
            log?.Warn($"FONT {string.Join("·", Preferred)} 가 없어 {Fallback} 를 쓴다 — 한글이 네모로 보인다");
            system.AddFont(File.ReadAllBytes(fallback));
        }
        else
        {
            log?.Warn("FONT 쓸 글꼴이 없다 — 글자를 그리지 않는다");
            loaded = false;
        }
        return system;
    }
}
