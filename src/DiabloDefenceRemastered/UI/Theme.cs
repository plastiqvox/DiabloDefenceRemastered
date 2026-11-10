namespace DiabloDefenceRemastered.UI;

/// <summary>
/// 화면 색·크기 한 곳 (설계서 8.2, 기획 12). 좌표·크기는 가상 960×540 기준(<c>UiCanvas</c> 가 창 배율로 바꿔 그린다).
/// 그림 ID 는 정의(<c>sprites</c>)에 있을 때만 그린다 — 없으면 그 그림만 건너뛴다(시험·자원 없는 실행이 멈추지 않게).
/// </summary>
public static class Theme
{
    public const float Width = 960f;
    public const float Height = 540f;

    // ── 색 ─────────────────────────────────────────────
    public static readonly Rgb Text = new(230, 220, 200);
    public static readonly Rgb TextDim = new(150, 140, 125);
    public static readonly Rgb Gold = new(232, 190, 80);
    public static readonly Rgb Red = new(200, 40, 30);
    public static readonly Rgb DarkRed = new(110, 15, 10);
    public static readonly Rgb Blue = new(60, 110, 230);
    public static readonly Rgb WeaponBand = new(225, 125, 40);
    public static readonly Rgb PassiveBand = new(70, 120, 220);
    public static readonly Rgb Panel = new(20, 16, 14);
    public static readonly Rgb PanelEdge = new(95, 80, 60);
    public static readonly Rgb Focus = new(255, 235, 170);
    public static readonly Rgb Grey = new(120, 120, 120);
    public static readonly Rgb White = Rgb.White;
    public static readonly Rgb Black = Rgb.Black;

    // ── 글자 크기 ─────────────────────────────────────
    public const float SmallText = 12f;
    public const float BodyText = 16f;
    public const float MenuText = 22f;
    public const float TitleText = 36f;

    // ── 오버레이 ──────────────────────────────────────
    /// <summary>레벨업·일시정지 배경 어둡게(기획 12.2 α0.6).</summary>
    public const float DimAlpha = 0.6f;

    // ── 원작 exe 그림(설계서 6.3 grid 정의 ID) ────────
    public const string TitleSprite = "TITLE";
    public const string LogoSprite = "LOGO";
    public const string GoldSprite = "Gold";
}
