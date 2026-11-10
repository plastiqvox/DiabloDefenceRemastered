namespace DiabloDefenceRemastered.Core.Art;

/// <summary>
/// 그림 배율 s (설계서 7.1, v0.8 사용자 결정 U, 소유 WP5). 시뮬레이션은 s 를 모른다 — 카메라·<c>World.ViewRect</c>·생성 띠는 늘 960×540 월드 px 이고,
/// 렌더만 <c>WorldRT</c> 를 (960·s)×(540·s) 로 잡아 월드 → 타깃 변환에 s 를 곱한다. 그림마다 그리는 배율 = s ÷ 메타 <c>scale</c>(정수 — 모두 <c>PointClamp</c>).
/// 그래픽 없는 순수 계산이라 Core 에 두고 시험한다(게임 <c>Rendering/</c> 이 부른다).
/// </summary>
public static class ArtScale
{
    /// <summary>가상(월드) 화면 크기 960×540.</summary>
    public static readonly Int2 Virtual = new(960, 540);

    /// <summary><c>WorldRT</c> 크기 = (960·s)×(540·s).</summary>
    public static Int2 WorldTarget(int s)
    {
        if (s < 1) throw new ArgumentOutOfRangeException(nameof(s), s, "그림 배율 s 는 1 이상");
        return new Int2(Virtual.X * s, Virtual.Y * s);
    }

    /// <summary>
    /// 그리는 배율 = s ÷ 그림 <paramref name="metaScale"/>. {1, s} 밖이거나 s 를 나누지 않으면 예외 — 시작 검사(WP7, <c>AssetScale.FitsArtScale</c>)가 이미 막았어야 하는 버그다.
    /// </summary>
    public static int DrawScale(int s, int metaScale)
    {
        if (!Assets.AssetScale.FitsArtScale(metaScale, s))
            throw new InvalidOperationException($"그림 scale {metaScale} 는 그림 배율 s = {s} 로 그릴 수 없다(7.1 — 시작 검사가 막았어야 함)");
        return s / metaScale;
    }

    /// <summary>카메라 왼쪽 위를 타깃 픽셀(1/s 월드 px)로 내린다(7.2 "정수 픽셀로 내림" 을 s 배 화면 기준으로).</summary>
    public static Vector2 FloorToTarget(Vector2 world, int s) => new(MathF.Floor(world.X * s) / s, MathF.Floor(world.Y * s) / s);

    /// <summary>개체 위치를 타깃 픽셀로 반올림(7.2 — 그리기 직전, 0.5 는 큰 쪽).</summary>
    public static Vector2 RoundToTarget(Vector2 world, int s) => new(MathF.Floor(world.X * s + 0.5f) / s, MathF.Floor(world.Y * s + 0.5f) / s);
}

/// <summary>
/// 창에 맞추기 (설계서 7.1·7.2 — <c>RenderContext.ScreenToWorld</c> 의 계산, 소유 WP5).
/// <c>WorldRT</c>((960·s)×(540·s))를 정수 배율 k 로 점 확대한 뒤 창 안 16:9 레터박스 사각형에 선형으로 줄인다(sharp bilinear).
/// 9~10층(UI)은 <c>PresentRT</c> 해상도에 레터박스 사각형 기준으로 그린다(<c>UiCanvas</c>).
/// </summary>
public static class PresentMath
{
    /// <summary>정수 배율 k = ceil(min(W/(960·s), H/(540·s))), 최소 1.</summary>
    public static int IntegerScale(Int2 present, int s)
    {
        var target = ArtScale.WorldTarget(s);
        var fit = MathF.Min(present.X / (float)target.X, present.Y / (float)target.Y);
        return Math.Max(1, (int)MathF.Ceiling(fit - 1e-5f));
    }

    /// <summary>창(<c>PresentRT</c>) 안 16:9 레터박스 사각형 — 가운데, 남는 곳은 검정. 정수 픽셀.</summary>
    public static RectI Letterbox(Int2 present)
    {
        if (present.X <= 0 || present.Y <= 0) return new RectI(0, 0, 0, 0);
        var w = present.X;
        var h = (int)MathF.Round(w * 9f / 16f);
        if (h > present.Y)
        {
            h = present.Y;
            w = (int)MathF.Round(h * 16f / 9f);
        }
        var x = (present.X - w) / 2;
        var y = (present.Y - h) / 2;
        return new RectI(x, y, x + w, y + h);
    }

    /// <summary>가상 좌표 1 = 창 픽셀 몇 개(<c>UiCanvas.Scale</c>).</summary>
    public static float UiScale(Int2 present)
    {
        var box = Letterbox(present);
        return box.Width / (float)ArtScale.Virtual.X;
    }

    /// <summary>창 좌표 → 가상 960×540 좌표(레터박스 밖이면 960×540 밖 값).</summary>
    public static Vector2 ScreenToVirtual(Vector2 screen, Int2 present)
    {
        var box = Letterbox(present);
        if (box.IsEmpty) return Vector2.Zero;
        return new Vector2((screen.X - box.X0) * ArtScale.Virtual.X / box.Width, (screen.Y - box.Y0) * ArtScale.Virtual.Y / box.Height);
    }

    /// <summary>가상 좌표 → 창 좌표.</summary>
    public static Vector2 VirtualToScreen(Vector2 v, Int2 present)
    {
        var box = Letterbox(present);
        return new Vector2(box.X0 + v.X * box.Width / ArtScale.Virtual.X, box.Y0 + v.Y * box.Height / ArtScale.Virtual.Y);
    }

    /// <summary>창 좌표 → 월드(마지막으로 그린 카메라 왼쪽 위 <paramref name="cameraTopLeft"/> 기준, 흔들림 제외). 가상 1 = 월드 1 px.</summary>
    public static Vector2 ScreenToWorld(Vector2 screen, Int2 present, Vector2 cameraTopLeft) => cameraTopLeft + ScreenToVirtual(screen, present);
}
