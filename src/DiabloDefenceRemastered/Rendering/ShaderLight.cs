using System.Globalization;
using DiabloDefenceRemastered.Core.Art;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using Matrix = Microsoft.Xna.Framework.Matrix;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DiabloDefenceRemastered.Rendering;

/// <summary>
/// 셰이더 빛 (설계서 7.7 ② — 2026-11-13 사용자 결정: 빛을 GPU 셰이더로, 벽 그림자는 은은하게). 셰이더 파일(<c>Shaders\*.mgfxo</c>)이 없거나 못 읽으면 만들지 않고
/// <see cref="LightRenderer"/> 가 예전 빛 텍스처 더하기(480×270)로 그린다.
/// <list type="number">
/// <item>바닥 y 버퍼(<see cref="GroundRT"/>, <c>WorldRT</c> 크기): 0~5층을 그릴 때 벽·유닛 그림마다 "서 있는 바닥 y" 를 모아(<see cref="AddGround"/>) 같은 차례로 다시 찍는다(<c>GroundWrite.fx</c>).
/// 유닛은 발 y, 벽 기둥은 2px 세로 조각마다 칸 가운데 y 를 화면 x ±32 이웃 막힌 칸 가운데 y 쪽으로 선형으로 이은 값 — 벽 줄을 따라 바닥선이 끊기지 않는다.</item>
/// <item>칸 막힘 지도(N×N, 시야 막힘 칸 = 1): 맵이 바뀔 때 한 번.</item>
/// <item>빛마다 둘레 사각형(위로 벽 높이만큼 더)을 <c>LightAdd.fx</c> 로 더한다: 바닥 자리에서 거리 감쇠(예전 텍스처 곡선) × 벽 그림자(빛까지 칸 막힘 16번 표본의 최댓값 × <see cref="ShadowStrength"/>).</item>
/// </list>
/// </summary>
public sealed class ShaderLight : IDisposable
{
    /// <summary>벽 그림자 진하기(2026-11-13 사용자 결정 "은은하게").</summary>
    public const float ShadowStrength = 0.45f;
    /// <summary>그림자 표본에서 바닥 자리·빛 쪽 끝을 건너뛰는 거리(월드 px) — 벽 기둥 자기 칸·벽에 붙은 횃불.</summary>
    public const float ShadowSkip = 36f;
    /// <summary>빛 사각형을 위로 더 늘리는 높이(월드 px) — 바닥은 반경 안이지만 그림은 위에 있는 벽(기둥 160)·유닛.</summary>
    public const float LiftMargin = 176f;
    /// <summary>바닥 y 부호화: (y − 카메라 y + 512) × 16 을 16비트로.</summary>
    public const float GroundOffset = 512f, GroundScale = 16f;
    /// <summary>벽 바닥선 조각 폭(월드 px).</summary>
    public const float SliceWidth = 2f;

    private readonly record struct GroundDraw(Texture2D Texture, Rectangle Source, RectF Dest, float GroundY);

    private static readonly BlendState Add = new()
    {
        Name = "ShaderLightAdd",
        ColorBlendFunction = BlendFunction.Add, ColorSourceBlend = Blend.One, ColorDestinationBlend = Blend.One,
        AlphaBlendFunction = BlendFunction.Add, AlphaSourceBlend = Blend.Zero, AlphaDestinationBlend = Blend.One,
    };

    private readonly GraphicsDevice _device;
    private readonly Effect _ground;
    private readonly Effect _light;
    private readonly List<GroundDraw> _draws = new(2048);
    private Texture2D? _occ;
    private ArenaGrid? _occGrid;

    private ShaderLight(GraphicsDevice device, Effect ground, Effect light)
    {
        _device = device;
        _ground = ground;
        _light = light;
    }

    public RenderTarget2D? GroundRT { get; private set; }

    /// <summary>출력 <c>Shaders\</c> 의 두 셰이더를 읽는다. 없거나 실패하면 null(로그 한 줄).</summary>
    public static ShaderLight? TryLoad(GraphicsDevice device, IAppLog log)
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Shaders");
        try
        {
            var ground = new Effect(device, File.ReadAllBytes(Path.Combine(dir, "GroundWrite.mgfxo")));
            var light = new Effect(device, File.ReadAllBytes(Path.Combine(dir, "LightAdd.mgfxo")));
            log.Info("LIGHT shader=on");
            return new ShaderLight(device, ground, light);
        }
        catch (Exception e)
        {
            log.Info(string.Create(CultureInfo.InvariantCulture, $"LIGHT shader=off reason=\"{e.GetType().Name}: {e.Message}\""));
            return null;
        }
    }

    public void BeginFrame() => _draws.Clear();

    /// <summary>바닥 y 가 <paramref name="groundY"/> 인 그림 하나(유닛 — 발 y).</summary>
    public void AddGround(Texture2D texture, Rectangle source, RectF dest, float groundY) => _draws.Add(new GroundDraw(texture, source, dest, groundY));

    /// <summary>
    /// 벽 기둥 하나: 칸 가운데 (cx, cy) 에서 화면 x 로 ±32 의 이웃 막힌 칸 가운데 y(<paramref name="leftY"/>·<paramref name="rightY"/> — 없으면 cy)까지 선형인 바닥선을
    /// <see cref="SliceWidth"/> 조각으로. <paramref name="metaScale"/> = 그림 텍셀 / 월드 px.
    /// </summary>
    public void AddGroundColumn(Texture2D texture, Rectangle source, RectF dest, int metaScale, float cx, float cy, float leftY, float rightY)
    {
        var step = Math.Max(1, (int)(SliceWidth * metaScale));
        for (var sx = 0; sx < source.Width; sx += step)
        {
            var w = Math.Min(step, source.Width - sx);
            var x0 = dest.X0 + sx / (float)metaScale;
            var x1 = x0 + w / (float)metaScale;
            var u = ((x0 + x1) * 0.5f - cx) / 32f;
            var y = u < 0f ? cy + (leftY - cy) * MathF.Min(1f, -u) : cy + (rightY - cy) * MathF.Min(1f, u);
            _draws.Add(new GroundDraw(texture, new Rectangle(source.X + sx, source.Y, w, source.Height), new RectF(x0, dest.Y0, x1, dest.Y1), y));
        }
    }

    /// <summary>칸 막힘 지도(맵이 바뀔 때만 다시).</summary>
    public void Attach(ArenaGrid? grid)
    {
        if (ReferenceEquals(grid, _occGrid)) return;
        _occGrid = grid;
        _occ?.Dispose();
        _occ = null;
        if (grid is null) return;
        var n = grid.Size;
        var data = new Color[n * n];
        for (var ty = 0; ty < n; ty++)
            for (var tx = 0; tx < n; tx++)
                data[ty * n + tx] = grid.BlocksSight(tx, ty) ? Color.White : Color.Black;
        _occ = new Texture2D(_device, n, n, false, SurfaceFormat.Color);
        _occ.SetData(data);
    }

    /// <summary>바닥 y 버퍼를 <c>WorldRT</c> 크기로 만들고 모은 그림을 찍는다.</summary>
    public void DrawGround(SpriteBatch batch, Int2 targetSize, Matrix toTarget, Vector2 camera)
    {
        if (GroundRT is null || GroundRT.Width != targetSize.X || GroundRT.Height != targetSize.Y)
        {
            GroundRT?.Dispose();
            GroundRT = new RenderTarget2D(_device, targetSize.X, targetSize.Y, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        }
        _device.SetRenderTarget(GroundRT);
        _device.Clear(Color.Transparent);
        _ground.Parameters["MatrixTransform"]?.SetValue(toTarget * Ortho(targetSize));
        batch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, _ground);
        foreach (var d in _draws) BatchX.Blit(batch, d.Texture, d.Dest, d.Source, Encode(d.GroundY - camera.Y));
        batch.End();
    }

    /// <summary>빛들을 지금 걸린 <c>LightRT</c>(<paramref name="lightSize"/>)에 더한다. <paramref name="toLight"/> = 월드 → LightRT px.</summary>
    public void AddLights(SpriteBatch batch, ReadOnlySpan<PointLight> lights, Int2 lightSize, Matrix toLight,
                          Vector2 camera, Vector2 viewSize, IsoMath iso, Texture2D pixel)
    {
        if (GroundRT is null || _occ is null || lights.Length == 0) return;
        var p = _light.Parameters;
        p["MatrixTransform"]?.SetValue(toLight * Ortho(lightSize));
        p["Camera"]?.SetValue(new XnaVector2(camera.X, camera.Y));
        p["ViewSize"]?.SetValue(new XnaVector2(viewSize.X, viewSize.Y));
        p["Iso"]?.SetValue(new Microsoft.Xna.Framework.Vector4(32f * iso.N, iso.H + 16f, iso.N, 0f));
        p["ShadowStrength"]?.SetValue(ShadowStrength);
        p["ShadowSkip"]?.SetValue(ShadowSkip);
        p["GroundTexture"]?.SetValue(GroundRT);
        p["OccTexture"]?.SetValue(_occ);
        batch.Begin(SpriteSortMode.Immediate, Add, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, _light);
        foreach (var (pos, radius, color, k, height) in lights)
        {
            var rect = new RectF(pos.X - radius, pos.Y - radius - LiftMargin, pos.X + radius, pos.Y + radius + 8f);
            p["RectMin"]?.SetValue(new XnaVector2(rect.X0, rect.Y0));
            p["RectSize"]?.SetValue(new XnaVector2(rect.Width, rect.Height));
            p["LightPos"]?.SetValue(new XnaVector2(pos.X, pos.Y));
            p["LightHeight"]?.SetValue(height);
            p["LightRadius"]?.SetValue(radius);
            p["LightColor"]?.SetValue(new Microsoft.Xna.Framework.Vector3(color.R / 255f * k, color.G / 255f * k, color.B / 255f * k));
            BatchX.Blit(batch, pixel, rect, new Rectangle(0, 0, 1, 1), Color.White);
        }
        batch.End();
    }

    /// <summary>카메라 기준 바닥 y → 정점 색 RG(16비트).</summary>
    public static Color Encode(float relY)
    {
        var u = (int)Math.Clamp(MathF.Round((relY + GroundOffset) * GroundScale), 0f, 65535f);
        return new Color(u >> 8, u & 255, 0, 255);
    }

    private static Matrix Ortho(Int2 size) => Matrix.CreateOrthographicOffCenter(0, size.X, size.Y, 0, 0, -1);

    public void Dispose()
    {
        GroundRT?.Dispose();
        _occ?.Dispose();
        _ground.Dispose();
        _light.Dispose();
    }
}
