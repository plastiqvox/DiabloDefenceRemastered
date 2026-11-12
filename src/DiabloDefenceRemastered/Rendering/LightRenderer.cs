using DiabloDefenceRemastered.Core.Art;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using Matrix = Microsoft.Xna.Framework.Matrix;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DiabloDefenceRemastered.Rendering;

/// <summary>
/// 대상 모양 하나(7.7 ④ 최소 밝기·7.10 ④ 안개 덧그림이 같은 목록을 쓴다). <see cref="Dest"/> 는 월드 px, <see cref="Alpha"/> = 안개 알파 × 그림 알파.
/// <see cref="Forced"/> = 늘 그리는 대상(중간 보스·보스·닿은 적·점화 중 자폭 적 — 안개 4단계 ③), <see cref="MinLight"/> = 최소 밝기 대상(주인공·적·시체·획득물, 앞 벽이 겹친 유닛은 뺌).
/// </summary>
public readonly record struct ShapeDraw(Texture2D Texture, Rectangle Source, RectF Dest, Vector2 Foot, float Alpha, bool Forced, bool MinLight, bool FogShape);

/// <summary>프레임 하나의 월드 그리기 값(<c>WorldRenderer</c> 가 채움).</summary>
public sealed class WorldFrame
{
    public World World = null!;
    /// <summary>카메라 왼쪽 위(월드 px, 흔들림 포함 — 그리는 값).</summary>
    public Vector2 Camera;
    /// <summary>보간된 주인공 발.</summary>
    public Vector2 HeroDraw;
    public int StageTick;
    public bool Fog;
    public RectF View;
    public readonly List<ShapeDraw> Shapes = [];
    /// <summary>짧은 빛(폭발·장판) — 자리·반경·색·세기.</summary>
    public readonly List<(Vector2 Pos, float Radius, Rgb Color, float Intensity)> ShortLights = [];
}

/// <summary>
/// 빛 지도 <c>LightRT</c> (설계서 7.7, 소유 WP5): 셰이더 빛이면 <c>WorldRT</c> 크기, 아니면 480×270(월드의 절반). 순서 ① 바탕으로 지움 → ② 빛 가산(<see cref="ShaderLight"/> 또는 빛 텍스처)
/// → ③ 안개 합치기(<see cref="FogRenderer"/> — 안개 RT 는 늘 480×270, 늘려 합침) → ④ 유닛 최소 밝기 → <c>WorldRT</c> 에 곱하기. <c>--reveal</c> 이면 ③ 을 건너뛴다. 빛 상한 64(<see cref="LightMath.Pick"/>).
/// </summary>
public sealed class LightRenderer : IDisposable
{
    public const int Width = 480, Height = 270;

    /// <summary>빛 가산(One/One — premultiplied 빛 텍스처 × 색). 알파는 그대로.</summary>
    private static readonly BlendState LightAdd = new()
    {
        Name = "LightAdd",
        ColorBlendFunction = BlendFunction.Add, ColorSourceBlend = Blend.One, ColorDestinationBlend = Blend.One,
        AlphaBlendFunction = BlendFunction.Add, AlphaSourceBlend = Blend.Zero, AlphaDestinationBlend = Blend.One,
    };

    private readonly GraphicsDevice _device;
    private readonly Primitives _prim;
    private readonly LightCandidate[] _candidates = new LightCandidate[256];
    private readonly (Vector2 Pos, float Radius, Rgb Color, float Intensity)[] _lights = new (Vector2, float, Rgb, float)[256];
    private readonly int[] _picked = new int[LightMath.MaxLights];
    private readonly int[] _scratch = new int[256];
    private readonly (Vector2 Pos, float Radius, Rgb Color, float Intensity)[] _pickedLights = new (Vector2, float, Rgb, float)[LightMath.MaxLights];
    private readonly ShaderLight? _shader;
    /// <summary>④ 최소 밝기의 보조 RT(<see cref="LightRT"/> 크기).</summary>
    private readonly RenderTarget2D _minRT;

    /// <param name="lightSize"><see cref="LightRT"/> 크기 — 셰이더 빛이면 <c>WorldRT</c> 크기(전체 해상도), 아니면 480×270. 안개 RT 는 늘 480×270.</param>
    public LightRenderer(GraphicsDevice device, Primitives prim, Int2 lightSize, ShaderLight? shader)
    {
        _device = device;
        _prim = prim;
        _shader = shader;
        LightSize = lightSize;
        LightRT = Target(device, lightSize);
        _minRT = Target(device, lightSize);
        MemRT = Target(device, new Int2(Width, Height));
        Fog = new FogRenderer(device, prim, Target(device, new Int2(Width, Height)), MemRT);
    }

    public RenderTarget2D LightRT { get; }
    public Int2 LightSize { get; }
    /// <summary>안개 기억 정도(7.10).</summary>
    public RenderTarget2D MemRT { get; }
    public FogRenderer Fog { get; }
    /// <summary>이번 프레임에 그린 빛 수(로그·측정).</summary>
    public int LightCount { get; private set; }

    private static RenderTarget2D Target(GraphicsDevice device, Int2 size) =>
        new(device, size.X, size.Y, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

    /// <summary>월드 px → <see cref="LightRT"/> px(카메라 빼고 <c>LightRT 폭 / 960</c> 배).</summary>
    private Matrix ToLight(Vector2 camera) => Matrix.CreateTranslation(-camera.X, -camera.Y, 0f) * Matrix.CreateScale(LightSize.X / (float)ArtScale.Virtual.X);

    /// <summary>월드 px → <c>LightRT</c> px(카메라 빼고 반으로).</summary>
    public static Matrix WorldToLight(Vector2 camera) => Matrix.CreateTranslation(-camera.X, -camera.Y, 0f) * Matrix.CreateScale(0.5f);

    /// <summary>
    /// 모양 찍기(<c>PointClamp</c> — 안개 벽 기둥·대상 모양·가 본 벽)용 <see cref="WorldToLight"/>: 그림을 월드 <see cref="StampNudge"/> px 민다.
    /// 빛 픽셀 중심은 월드 정수 좌표(카메라 + 2i + 1)라 그대로면 그림 텍셀 경계에 떨어져 어느 텍셀을 집을지 GPU 반올림에 달린다 —
    /// 벽 반쪽 기둥 이음매에서 투명한 쪽을 집으면 벽 값이 안 찍혀 32px 마다 어두운 세로 줄이 생기고 카메라 x 가 1px 움직일 때마다 깜빡였다.
    /// 0.25 는 그림 배율 1(텍셀 1px)·2(텍셀 0.5px, 카메라 0.5px 단위) 모두에서 샘플을 텍셀 안쪽에 둔다(0.5 는 배율 2 에서 다시 경계).
    /// </summary>
    public static Matrix StampToLight(Vector2 camera) => Matrix.CreateTranslation(StampNudge, StampNudge, 0f) * WorldToLight(camera);

    public const float StampNudge = 0.25f;

    public void Build(SpriteBatch batch, WorldFrame f, ArenaRenderer? arena, bool reveal)
    {
        var w = f.World;
        var lighting = w.Lighting;
        var toLight = ToLight(f.Camera);
        var full = new Rectangle(0, 0, LightSize.X, LightSize.Y);

        // ① 바탕
        _device.SetRenderTarget(LightRT);
        var b = LightMath.Base(lighting.Ambient, lighting.Darkness);
        _device.Clear(new Color(b.X, b.Y, b.Z, 1f));

        // ② 빛 가산(주인공·점 빛 먼저, 짧은 빛은 주인공에게 가까운 순)
        var n = 0;
        if (lighting.HeroLightRadius > 0f) Put(ref n, LightKind.Hero, f.HeroDraw, lighting.HeroLightRadius, lighting.HeroLightColor, 1f);
        foreach (var p in lighting.Lights)
            Put(ref n, LightKind.Point, p.Pos, p.Radius, p.Color, LightMath.Intensity(w.Seed, p.Id, f.StageTick, p.Flicker, p.PulsePeriod, p.PulseAmount));
        foreach (var s in f.ShortLights) Put(ref n, LightKind.Short, s.Pos, s.Radius, s.Color, s.Intensity);
        LightCount = LightMath.Pick(_candidates.AsSpan(0, n), f.HeroDraw, _picked, _scratch);
        if (_shader is not null && arena is not null)
        {
            // 셰이더 빛: 바닥 자리 감쇠 + 은은한 벽 그림자(ShaderLight)
            for (var i = 0; i < LightCount; i++) _pickedLights[i] = _lights[_picked[i]];
            _shader.AddLights(batch, _pickedLights.AsSpan(0, LightCount), LightSize, toLight, f.Camera, w.ViewSize, arena.Iso, _prim.Pixel);
        }
        else
        {
            batch.Begin(SpriteSortMode.Deferred, LightAdd, SamplerState.LinearClamp, null, null, null, toLight);
            for (var i = 0; i < LightCount; i++)
            {
                var (pos, radius, color, k) = _lights[_picked[i]];
                BatchX.Centered(batch, _prim.Light, pos, new Vector2(radius * 2f), new Color(color.R / 255f * k, color.G / 255f * k, color.B / 255f * k, 1f));
            }
            batch.End();
        }

        // ③ 안개 합치기
        if (f.Fog && !reveal && arena is not null)
        {
            Fog.Build(batch, f, arena);
            var m = LightMath.Memory(lighting.Ambient, lighting.Darkness, w.Defs.Rules.Sight?.MemoryRatio ?? 0f);
            _device.SetRenderTarget(LightRT);
            batch.Begin(SpriteSortMode.Deferred, FogBlend.MulRgbBySrcA, SamplerState.LinearClamp);
            batch.Draw(Fog.FogRT, full, Color.White);
            batch.End();
            batch.Begin(SpriteSortMode.Deferred, FogBlend.AddRgbBySrcA, SamplerState.LinearClamp);
            batch.Draw(MemRT, full, new Color(m.X, m.Y, m.Z, 1f));
            batch.End();
        }

        // ④ 유닛 최소 밝기
        var min = lighting.UnitMinLight;
        if (min > 0f) MinLight(batch, f, toLight, min);
    }

    private static readonly XnaVector2 Vector2Zero = XnaVector2.Zero;

    private void Put(ref int n, LightKind kind, Vector2 pos, float radius, Rgb color, float intensity)
    {
        if (n >= _candidates.Length || radius <= 0f || intensity <= 0f) return;
        _candidates[n] = new LightCandidate(kind, pos);
        _lights[n] = (pos, radius, color, intensity);
        n++;
    }

    private void MinLight(SpriteBatch batch, WorldFrame f, Matrix toLight, float m)
    {
        var any = false;
        foreach (var s in f.Shapes) if (s.MinLight && s.Alpha > 0f) { any = true; break; }
        if (!any) return;

        _device.SetRenderTarget(_minRT);
        _device.Clear(Color.Transparent);
        batch.Begin(SpriteSortMode.Deferred, FogBlend.CopyRgb, SamplerState.PointClamp);
        batch.Draw(LightRT, Vector2Zero, Color.White);
        batch.End();
        batch.Begin(SpriteSortMode.Deferred, FogBlend.MaxRgb, SamplerState.PointClamp);
        batch.Draw(_prim.Pixel, new Rectangle(0, 0, LightSize.X, LightSize.Y), new Color(m, m, m, 1f));
        batch.End();
        batch.Begin(SpriteSortMode.Deferred, FogBlend.MaxA, SamplerState.LinearClamp, null, null, null, toLight);
        foreach (var s in f.Shapes)
            if (s.MinLight && s.Alpha > 0f)
                BatchX.Blit(batch, s.Texture, s.Dest, s.Source, new Color(255, 255, 255, (int)(Math.Clamp(s.Alpha, 0f, 1f) * 255f)));
        batch.End();
        _device.SetRenderTarget(LightRT);
        batch.Begin(SpriteSortMode.Deferred, FogBlend.LerpRgbBySrcA, SamplerState.PointClamp);
        batch.Draw(_minRT, Vector2Zero, Color.White);
        batch.End();
    }

    public void Dispose()
    {
        LightRT.Dispose();
        _minRT.Dispose();
        MemRT.Dispose();
        Fog.Dispose();
    }
}

/// <summary>
/// 안개 그리기 (설계서 7.10, 소유 WP5). 칸 값 텍스처(<c>SightTex</c> 확장 격자·<c>ExploredTex</c> N×N, CPU 로 채워 <c>SetData</c>)를 칸 좌표 → <c>LightRT</c> 화면 행렬로 투영하고
/// <c>FogRT</c>(보이는 정도 V = 알파)·<c>MemRT</c>(기억 (1 − V)·E = 알파)를 만든다. 합치기는 <see cref="LightRenderer"/> ③.
/// 값은 늘 알파 채널(7.7 "혼합 상태 규칙"). <see cref="Attach"/> = 새 맵(텍스처 크기·칸 바뀜 값) — <c>WorldRenderer.Attach</c> 가 부른다.
/// </summary>
public sealed class FogRenderer(GraphicsDevice device, Primitives prim, RenderTarget2D fogRT, RenderTarget2D memRT) : IDisposable
{
    private Texture2D? _sight;
    private Texture2D? _explored;
    private Color[] _sightData = [];
    private Color[] _exploredData = [];
    private int _exploredVersion = -1;
    private VisibilityField? _field;
    /// <summary>이번 프레임 보이는 벽 칸의 안개 값(<c>ty·N + tx</c>, 그 밖은 NaN) — ③ 이웃 잇기.</summary>
    private float[] _wallLevels = [];

    public RenderTarget2D FogRT { get; } = fogRT;

    /// <summary>
    /// 화면 x 로 한쪽 이웃 벽 기둥 값: 칸 (ax, ay)·(bx, by)(같은 화면 x 쪽 두 이웃 — 벽 줄 방향이 둘) 중 이번 프레임 보이는 벽의 값 평균, 없으면 <paramref name="self"/>.
    /// </summary>
    private float Neighbor(int size, int ax, int ay, int bx, int by, float self)
    {
        var a = At(ax, ay);
        var b = At(bx, by);
        return float.IsNaN(a) ? (float.IsNaN(b) ? self : b) : float.IsNaN(b) ? a : (a + b) * 0.5f;

        float At(int x, int y) => x < 0 || y < 0 || x >= size || y >= size ? float.NaN : _wallLevels[y * size + x];
    }

    /// <summary>새 월드(맵): 텍스처를 맵 크기로 다시 만든다.</summary>
    public void Attach(World world)
    {
        _field = world.Visibility;
        _sight?.Dispose();
        _explored?.Dispose();
        _sight = _explored = null;
        _exploredVersion = -1;
        var n = world.Visibility.Size;
        if (n <= 0) return;
        var e = n + 2 * VisibilityField.Pad;
        _sight = new Texture2D(device, e, e);
        _explored = new Texture2D(device, n, n);
        _sightData = new Color[e * e];
        _exploredData = new Color[n * n];
    }

    /// <summary>칸 좌표(텍셀 중심 = 칸 중심, <paramref name="pad"/> = 텍스처 0 번 텍셀의 칸 번호 − ) → <c>LightRT</c> px 행렬(4.1 등각 변환이 아핀이라 행렬 하나).</summary>
    public static Matrix CellToLight(IsoMath iso, int pad, Vector2 camera)
    {
        var shear = new Matrix(32f, 16f, 0f, 0f, -32f, 16f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 1f);
        return Matrix.CreateTranslation(-0.5f - pad, -0.5f - pad, 0f) * shear
               * Matrix.CreateTranslation(32f * iso.N, iso.H + 16f, 0f) * LightRenderer.WorldToLight(camera);
    }

    public void Build(SpriteBatch batch, WorldFrame f, ArenaRenderer arena)
    {
        var w = f.World;
        var field = w.Visibility;
        if (_field != field) Attach(w);
        if (_sight is null || _explored is null || !field.Enabled) return;

        var n = field.Size;
        var pad = VisibilityField.Pad;
        var e = n + 2 * pad;
        for (var ey = 0; ey < e; ey++)
            for (var ex = 0; ex < e; ex++)
            {
                var a = (byte)Math.Clamp((int)MathF.Round(FogMath.CellLevel(field, ex - pad, ey - pad, f.StageTick) * 255f), 0, 255);
                _sightData[ey * e + ex] = new Color(a, a, a, a);
            }
        _sight.SetData(_sightData);
        if (_exploredVersion != field.ExploredVersion)
        {
            _exploredVersion = field.ExploredVersion;
            for (var ty = 0; ty < n; ty++)
                for (var tx = 0; tx < n; tx++)
                    _exploredData[ty * n + tx] = field.IsExplored(tx, ty) ? Color.White : Color.Transparent;
            _explored.SetData(_exploredData);
        }

        var iso = arena.Iso;
        var toLight = LightRenderer.WorldToLight(f.Camera);

        // FogRT ① 칸 값 투영
        device.SetRenderTarget(FogRT);
        device.Clear(Color.Transparent);
        batch.Begin(SpriteSortMode.Deferred, FogBlend.WriteA, SamplerState.LinearClamp, null, null, null, CellToLight(iso, pad, f.Camera));
        batch.Draw(_sight, XnaVector2.Zero, Color.White);
        batch.End();

        // ② 경계 흐림(반경 R + 64 까지 덮는 띠)
        var r = field.Radius;
        var edge = field.Rules?.Edge ?? 0f;
        var cover = r + VisibilityField.RangeSlack;
        var band = prim.BandFor(device, Math.Max(0f, r - edge) / cover, r / cover);
        batch.Begin(SpriteSortMode.Deferred, FogBlend.MulA, SamplerState.LinearClamp, null, null, null, toLight);
        BatchX.Centered(batch, band, f.HeroDraw, new Vector2(cover * 2f), Color.White);
        batch.End();

        // ③ 벽 기둥은 자기 칸 값 × 칸 중심 흐림, ④ 필요한 대상 모양
        var stamp = LightRenderer.StampToLight(f.Camera);
        batch.Begin(SpriteSortMode.Deferred, FogBlend.MaxA, SamplerState.PointClamp, null, null, null, stamp);
        // 기둥 값 하나로 찍으면 이웃 기둥과 값이 다를 때 64px 칸 단위 계단이 보인다(2026-11-13 사용자 지적) → 먼저 보이는 벽마다 값을 구하고,
        // 기둥을 세로 조각으로 나눠 화면 x 로 ±32 떨어진 이웃 벽 기둥(가운데) 값 쪽으로 선형으로 잇는다(ArenaRenderer.DrawItemShaded).
        var size = arena.Grid.Size;
        if (_wallLevels.Length != size * size)
        {
            _wallLevels = new float[size * size];
            Array.Fill(_wallLevels, float.NaN);
        }
        foreach (var i in arena.VisibleWalls)
        {
            var it = arena.Items[i];
            _wallLevels[it.Ty * size + it.Tx] = FogShade.WallLevel(field, iso, it.Tx, it.Ty, f.HeroDraw, f.StageTick);
        }
        foreach (var i in arena.VisibleWalls)
        {
            var it = arena.Items[i];
            var level = _wallLevels[it.Ty * size + it.Tx];
            var left = Neighbor(size, it.Tx - 1, it.Ty, it.Tx, it.Ty + 1, level);
            var right = Neighbor(size, it.Tx + 1, it.Ty, it.Tx, it.Ty - 1, level);
            if (MathF.Max(level, MathF.Max(left, right)) > 0f) arena.DrawItemShaded(batch, i, prim, level, left, right);
        }
        foreach (var i in arena.VisibleWalls)
        {
            var it = arena.Items[i];
            _wallLevels[it.Ty * size + it.Tx] = float.NaN;
        }
        foreach (var s in f.Shapes)
            if (s.FogShape && s.Alpha > 0f && (s.Forced || FogShade.NeedsShape(field, s.Dest, s.Foot, f.HeroDraw, f.StageTick)))
                BatchX.Blit(batch, s.Texture, s.Dest, s.Source, new Color(255, 255, 255, (int)(Math.Clamp(s.Alpha, 0f, 1f) * 255f)));
        batch.End();

        // MemRT: 가 본 칸 → 가 본 벽 → × (1 − V)
        device.SetRenderTarget(memRT);
        device.Clear(new Color(255, 255, 255, 0));
        batch.Begin(SpriteSortMode.Deferred, FogBlend.WriteA, SamplerState.LinearClamp, null, null, null, CellToLight(iso, 0, f.Camera));
        batch.Draw(_explored, XnaVector2.Zero, Color.White);
        batch.End();
        batch.Begin(SpriteSortMode.Deferred, FogBlend.MaxA, SamplerState.PointClamp, null, null, null, stamp);
        foreach (var i in arena.VisibleWalls)
        {
            var it = arena.Items[i];
            if (field.IsExplored(it.Tx, it.Ty)) arena.DrawItem(batch, i, prim, 1f, Color.White);
        }
        batch.End();
        batch.Begin(SpriteSortMode.Deferred, FogBlend.MulInvA, SamplerState.PointClamp);
        batch.Draw(FogRT, XnaVector2.Zero, Color.White);
        batch.End();
    }

    public void Dispose()
    {
        _sight?.Dispose();
        _explored?.Dispose();
        FogRT.Dispose();
    }
}
