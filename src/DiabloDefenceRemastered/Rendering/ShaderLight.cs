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
    private readonly Effect _noShadow;
    private readonly Effect _shadow;
    private readonly List<GroundDraw> _draws = new(2048);
    private Texture2D? _occ;
    private ArenaGrid? _occGrid;

    private ShaderLight(GraphicsDevice device, Effect ground, Effect light, Effect noShadow, Effect shadow)
    {
        _device = device;
        _ground = ground;
        _light = light;
        _noShadow = noShadow;
        _shadow = shadow;
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
            var noShadow = new Effect(device, File.ReadAllBytes(Path.Combine(dir, "SpriteNoShadow.mgfxo")));
            var shadow = new Effect(device, File.ReadAllBytes(Path.Combine(dir, "UnitShadow.mgfxo")));
            return new ShaderLight(device, ground, light, noShadow, shadow);
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

    // ── 그림자(2026-11-13 사용자 요청 "그림자도 셰이더로 3D 효과") ─────────────

    /// <summary>그림자 진하기 상한(빛이 발밑에 가장 셀 때).</summary>
    public const float ShadowMaxAlpha = 0.6f;
    /// <summary>발밑 접지 그림자 알파(빛과 상관없이 늘).</summary>
    public const float ContactAlpha = 0.6f;
    /// <summary>높이 0 인 빛(주인공 빛·맵 점 빛)을 그림자 계산에서 볼 높이(px) — 머리 위 횃불쯤.</summary>
    public const float FlatLightHeight = 110f;
    /// <summary>유닛 하나가 그림자를 받는 빛 수 상한.</summary>
    public const int ShadowLightsPerUnit = 2;

    /// <summary>그림자 혼합: dest × (1 − 알파). 알파 채널은 그대로.</summary>
    private static readonly BlendState Darken = new()
    {
        Name = "ShaderShadow",
        ColorBlendFunction = BlendFunction.Add, ColorSourceBlend = Blend.Zero, ColorDestinationBlend = Blend.InverseSourceAlpha,
        AlphaBlendFunction = BlendFunction.Add, AlphaSourceBlend = Blend.Zero, AlphaDestinationBlend = Blend.One,
    };

    private readonly Dictionary<Texture2D, List<VertexPositionColorTexture>> _shadowQuads = [];
    private readonly List<VertexPositionColorTexture> _contactQuads = [];
    private short[] _indices = [];
    private VertexPositionColorTexture[] _vbuf = new VertexPositionColorTexture[1024];

    /// <summary>0~5층 SpriteBatch 효과: 기본 그리기와 같고 원작 구운 그림자 픽셀만 버린다(<c>SpriteNoShadow.fx</c>).</summary>
    public Effect NoShadowEffect(Matrix toTarget, Int2 targetSize)
    {
        _noShadow.Parameters["MatrixTransform"]?.SetValue(toTarget * Ortho(targetSize));
        return _noShadow;
    }

    /// <summary>
    /// 지금 걸린 <c>LightRT</c> 에 유닛 그림자를 깎는다(<c>UnitShadow.fx</c>): 그림자를 드리우는 모양(<see cref="ShapeDraw.Caster"/>)마다
    /// ① 발밑 접지 그림자(빛 텍스처, 옅게 늘) ② 발밑에 가장 세게 닿는 빛 <see cref="ShadowLightsPerUnit"/> 개의 반대쪽으로 눕힌 실루엣.
    /// 실루엣: 그림 픽셀 (x, y) → 발 F + 가로 축 × (x − F.x) + 빛 반대 방향 × (F.y − y) × 길이 배율 — 바닥 공간(세로 ×2)에서 계산해 화면으로 되돌린다.
    /// 길이 배율 = 바닥 거리 / 빛 높이(0.3~1.5), 진하기 = 발밑 빛 세기(감쇠 × 밝기) × 0.75(상한 <see cref="ShadowMaxAlpha"/>) × 모양 알파.
    /// </summary>
    public void DrawShadows(IReadOnlyList<ShapeDraw> shapes, ReadOnlySpan<PointLight> lights, Int2 lightSize, Matrix toLight,
                            Vector2 camera, Vector2 viewSize, Texture2D soft)
    {
        if (GroundRT is null) return;
        foreach (var list in _shadowQuads.Values) list.Clear();
        _contactQuads.Clear();
        Span<(float C, int I)> best = stackalloc (float, int)[ShadowLightsPerUnit];
        foreach (var s in shapes)
        {
            if (!s.Caster || s.Alpha <= 0f) continue;
            var foot = s.Foot;
            var cw = Math.Clamp(s.Dest.Width * 0.8f, 24f, 80f);
            AddQuad(_contactQuads, soft, new Rectangle(0, 0, soft.Width, soft.Height),
                    new XnaVector2(foot.X - cw * 0.5f, foot.Y - cw * 0.225f), new XnaVector2(cw, 0f), new XnaVector2(0f, cw * 0.45f), ContactAlpha * s.Alpha);

            var n = 0;
            for (var i = 0; i < lights.Length; i++)
            {
                var c = Contribution(lights[i], foot);
                if (c < 0.06f) continue;
                if (n < best.Length) best[n++] = (c, i);
                else
                {
                    var min = 0;
                    for (var k = 1; k < n; k++) if (best[k].C < best[min].C) min = k;
                    if (c > best[min].C) best[min] = (c, i);
                }
            }
            for (var k = 0; k < n; k++)
            {
                var l = lights[best[k].I];
                var v = foot - l.Pos;
                var g = new Vector2(v.X, v.Y * 2f);                               // 바닥 공간(아이소 세로 ×2)
                var dg = g.Length();
                var dir = g / dg;
                var perp = new Vector2(-dir.Y, dir.X);
                if (perp.X < 0f || (perp.X == 0f && perp.Y < 0f)) perp = -perp;
                // 가로 축: 빛이 앞뒤면 화면 가로 그대로, 옆이면 빛과 직각 쪽으로 — 몸 두께가 남게
                var w = Vector2.Normalize(new Vector2(MathF.Abs(dir.Y), 0f) + perp * MathF.Abs(dir.X));
                var len = Math.Clamp(dg / (l.Height > 0f ? l.Height : FlatLightHeight), 0.3f, 1.5f);
                // 그림 사각형 (x, y) → F + S(w·(x − F.x) + dir·(F.y − y)·len), S = (gx, gy / 2)
                XnaVector2 Map(float x, float y)
                {
                    var p = w * (x - foot.X) + dir * ((foot.Y - y) * len);
                    return new XnaVector2(foot.X + p.X, foot.Y + p.Y * 0.5f);
                }
                var o = Map(s.Dest.X0, s.Dest.Y0);
                var ax = Map(s.Dest.X1, s.Dest.Y0) - o;
                var ay = Map(s.Dest.X0, s.Dest.Y1) - o;
                var a = MathF.Min(ShadowMaxAlpha, best[k].C * 0.75f) * s.Alpha;
                if (!_shadowQuads.TryGetValue(s.Texture, out var list)) _shadowQuads[s.Texture] = list = new List<VertexPositionColorTexture>(256);
                AddQuad(list, s.Texture, s.Source, o, ax, ay, a);
            }
        }

        var p = _shadow.Parameters;
        p["MatrixTransform"]?.SetValue(toLight * Ortho(lightSize));
        p["Camera"]?.SetValue(new XnaVector2(camera.X, camera.Y));
        p["ViewSize"]?.SetValue(new XnaVector2(viewSize.X, viewSize.Y));
        p["GroundTexture"]?.SetValue(GroundRT);
        _device.BlendState = Darken;
        _device.DepthStencilState = DepthStencilState.None;
        _device.RasterizerState = RasterizerState.CullNone;
        p["Soft"]?.SetValue(1f);
        Flush(soft, _contactQuads, SamplerState.LinearClamp);
        p["Soft"]?.SetValue(0f);
        foreach (var (tex, list) in _shadowQuads) Flush(tex, list, SamplerState.PointClamp);
    }

    /// <summary>빛 하나가 발 자리에 닿는 세기(감쇠 × 밝기 × 세기) — 발과 거의 같은 자리의 빛(자기 빛)은 0.</summary>
    private static float Contribution(in PointLight l, Vector2 foot)
    {
        var d = Vector2.Distance(foot, l.Pos);
        if (d < 6f || l.Radius <= 0f) return 0f;
        var v = Math.Clamp(1f - d / l.Radius, 0f, 1f);
        return v * v * (3f - 2f * v) * l.Intensity * (l.Color.R + l.Color.G + l.Color.B) / (3f * 255f);
    }

    private static void AddQuad(List<VertexPositionColorTexture> list, Texture2D tex, Rectangle src, XnaVector2 o, XnaVector2 ax, XnaVector2 ay, float alpha)
    {
        var u0 = src.X / (float)tex.Width;
        var v0 = src.Y / (float)tex.Height;
        var u1 = src.Right / (float)tex.Width;
        var v1 = src.Bottom / (float)tex.Height;
        var c = new Color(0f, 0f, 0f, Math.Clamp(alpha, 0f, 1f));
        list.Add(new VertexPositionColorTexture(new Microsoft.Xna.Framework.Vector3(o, 0f), c, new XnaVector2(u0, v0)));
        list.Add(new VertexPositionColorTexture(new Microsoft.Xna.Framework.Vector3(o + ax, 0f), c, new XnaVector2(u1, v0)));
        list.Add(new VertexPositionColorTexture(new Microsoft.Xna.Framework.Vector3(o + ay, 0f), c, new XnaVector2(u0, v1)));
        list.Add(new VertexPositionColorTexture(new Microsoft.Xna.Framework.Vector3(o + ax + ay, 0f), c, new XnaVector2(u1, v1)));
    }

    private void Flush(Texture2D tex, List<VertexPositionColorTexture> verts, SamplerState sampler)
    {
        if (verts.Count == 0) return;
        var quads = verts.Count / 4;
        if (_indices.Length < quads * 6)
        {
            var n = Math.Max(quads, _indices.Length / 6 * 2);
            _indices = new short[n * 6];
            for (var q = 0; q < n; q++)
            {
                var b = (short)(q * 4);
                _indices[q * 6] = b; _indices[q * 6 + 1] = (short)(b + 1); _indices[q * 6 + 2] = (short)(b + 2);
                _indices[q * 6 + 3] = (short)(b + 1); _indices[q * 6 + 4] = (short)(b + 3); _indices[q * 6 + 5] = (short)(b + 2);
            }
        }
        if (_vbuf.Length < verts.Count) _vbuf = new VertexPositionColorTexture[Math.Max(verts.Count, _vbuf.Length * 2)];
        verts.CopyTo(_vbuf);
        _shadow.Parameters["SpriteTexture"]?.SetValue(tex);
        _device.SamplerStates[0] = sampler;
        foreach (var pass in _shadow.CurrentTechnique.Passes)
        {
            pass.Apply();
            _device.Textures[0] = tex;
            _device.Textures[1] = GroundRT;
            _device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _vbuf, 0, verts.Count, _indices, 0, quads * 2);
        }
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
        _noShadow.Dispose();
        _shadow.Dispose();
    }
}
