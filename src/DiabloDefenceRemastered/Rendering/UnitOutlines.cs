using DiabloDefenceRemastered.Core.Art;
using DiabloDefenceRemastered.Core.Assets;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using Matrix = Microsoft.Xna.Framework.Matrix;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DiabloDefenceRemastered.Rendering;

/// <summary>
/// 벽 뒤 유닛 윤곽선 (설계서 7.6 "가림" — 2026-11-13 사용자 결정: 앞 벽 반투명 대신 벽은 늘 불투명, 가려진 캐릭터는 경계선만).
/// 빛·안개를 곱한 뒤(6층 뒤, 7층 발광 앞) <c>WorldRT</c> 에 셰이더 없이 깊이·스텐실로 그린다:
/// <list type="number">
/// <item>깊이: 4층 항목(안쪽 벽·바닥 위 조각·아치 덧그림)은 정렬 키 y 로, 5층 앞둘레 턱은 가장 앞으로 — 알파 테스트로 그림의 불투명 픽셀만, 색은 쓰지 않음. 0b 뒷둘레는 늘 뒤라 뺀다.</item>
/// <item>스텐실 몸(0x80): 모든 대상의 그림 자리 — 윤곽이 몸 안쪽에 그려지지 않게.</item>
/// <item>스텐실 테(종류 값 1·2·3): 그림을 상하좌우로 1..두께 px 밀어, 몸이 아니고 그 대상보다 앞(깊이 작음)인 벽이 덮은 픽셀만. 나중 종류가 덮어씀(적 → 정예 → 주인공).</item>
/// <item>색: 종류마다 스텐실이 그 값인 픽셀을 한 색으로 칠함.</item>
/// </list>
/// 그래서 가려지지 않은 몸 둘레(바닥 위)에는 윤곽이 없고, 벽 뒤로 들어간 부분의 경계만 보인다.
/// </summary>
public sealed class UnitOutlines : IDisposable
{
    public enum Kind : byte { Enemy = 1, Elite = 2, Hero = 3 }

    private readonly record struct Target(Texture2D Texture, Rectangle Source, RectF Dest, float Depth, Kind Kind);

    /// <summary>주인공 윤곽(밝은 금색, 2px)·적(붉은색, 1px)·중간 보스·보스(주황, 1px) — 월드 px.</summary>
    public static readonly Rgb HeroColor = new(255, 214, 110), EnemyColor = new(225, 45, 35), EliteColor = new(255, 150, 40);
    public const int HeroWidth = 2, EnemyWidth = 1;
    public const float Alpha = 0.9f;
    /// <summary>앞둘레 턱 깊이(가장 앞).</summary>
    public const float FrontDepth = 0.001f;

    private const int BodyStencil = 0x80;
    /// <summary>알파 테스트 기준(0..255, 이 값보다 커야 그림으로 침): 벽은 반쯤, 유닛은 거의 불투명만(원작 그림자 픽셀 제외).</summary>
    private const int WallAlphaRef = 96, UnitAlphaRef = 200;

    private static readonly BlendState NoColor = new() { Name = "NoColor", ColorWriteChannels = ColorWriteChannels.None };

    private static readonly DepthStencilState WriteDepth = new()
    {
        Name = "OutlineWriteDepth", DepthBufferEnable = true, DepthBufferWriteEnable = true, DepthBufferFunction = CompareFunction.LessEqual, StencilEnable = false,
    };

    private static readonly DepthStencilState WriteBody = new()
    {
        Name = "OutlineWriteBody", DepthBufferEnable = false, DepthBufferWriteEnable = false,
        StencilEnable = true, StencilFunction = CompareFunction.Always, StencilPass = StencilOperation.Replace, ReferenceStencil = BodyStencil,
    };

    private static readonly DepthStencilState[] WriteRing = [Ring(1), Ring(2), Ring(3)];
    private static readonly DepthStencilState[] PaintRing = [Paint(1), Paint(2), Paint(3)];

    /// <summary>빛처럼 얹을 때 선 자체·번진 테 세기(가산).</summary>
    public const float LineAlpha = 0.55f, GlowAlpha = 0.9f;

    private readonly List<Target> _targets = new(256);
    private RenderTarget2D? _line, _half, _quarter;
    private AlphaTestEffect? _effect;

    public int Count => _targets.Count;

    public void Clear() => _targets.Clear();

    /// <summary>정렬 키 y → 깊이(0 = 앞). 화면 위쪽(작은 y)일수록 뒤. 같은 프레임 안에서만 비교하므로 시야 위 끝 기준.</summary>
    public static float Depth(float sortY, RectF view) => Math.Clamp(0.5f - (sortY - view.Y0) / 8192f, 0.002f, 0.999f);

    public void Add(Texture2D texture, Rectangle source, RectF dest, float sortY, RectF view, Kind kind) =>
        _targets.Add(new Target(texture, source, dest, Depth(sortY, view), kind));

    /// <summary>
    /// 윤곽을 자기 타깃(깊이·스텐실 있음)에 그린 뒤 <paramref name="world"/> 에 빛처럼 얹는다(2026-11-15 사용자 요청 "붉은 궤적도 붉은 조명처럼 — 지금은 픽셀로 그린 느낌"):
    /// 선 자체는 <see cref="LineAlpha"/> 로 약하게, ½·¼ 로 줄여 번진 테를 <see cref="GlowAlpha"/> 로 두 번 가산. 끝나면 <paramref name="world"/> 가 걸린 채로 돌아옴.
    /// </summary>
    public void Draw(GraphicsDevice device, SpriteBatch batch, Primitives prim, ArenaRenderer arena, RectF view, Matrix toTarget, RenderTarget2D world)
    {
        if (_targets.Count == 0) return;
        var targetW = world.Width;
        var targetH = world.Height;
        EnsureTargets(device, targetW, targetH);
        device.SetRenderTarget(_line);
        device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.Transparent, 1f, 0);
        var fx = _effect ??= new AlphaTestEffect(device) { VertexColorEnabled = true, AlphaFunction = CompareFunction.Greater };
        fx.World = Matrix.Identity;
        fx.View = toTarget;
        fx.Projection = Matrix.CreateOrthographicOffCenter(0, targetW, targetH, 0, 0, -1);

        // 1. 벽 깊이| ClearOptions.Stencil, Color.Transparent, 1f, 0);

        // 1. 벽 깊이(효과 값은 End 에서 적용되므로 패스마다 Begin 앞에 정한다)
        fx.ReferenceAlpha = WallAlphaRef;
        batch.Begin(SpriteSortMode.Deferred, NoColor, SamplerState.PointClamp, WriteDepth, null, fx);
        var items = arena.Items;
        for (var i = 0; i < items.Count; i++)
        {
            var it = items[i];
            if (it.Kind == ArenaCellKind.Wall && it.Group == WallGroup.Back) continue;
            if (!WallFade.Overlaps(arena.ItemRect(it), view)) continue;
            var front = it.Kind == ArenaCellKind.Wall && it.Group == WallGroup.Front;
            arena.DrawItemDepth(batch, i, prim, front ? FrontDepth : Depth(arena.SortY(it), view));
        }
        batch.End();

        // 2. 몸 — 반투명 그림자 픽셀은 몸으로 치지 않는다(발밑 그림자까지 윤곽이 퍼짐)
        fx.ReferenceAlpha = UnitAlphaRef;
        batch.Begin(SpriteSortMode.Deferred, NoColor, SamplerState.PointClamp, WriteBody, null, fx);
        foreach (var t in _targets) BatchX.Blit(batch, t.Texture, t.Dest, t.Source, Color.White);
        batch.End();

        // 3·4. 종류마다 테 → 칠
        for (var k = Kind.Enemy; k <= Kind.Hero; k++)
        {
            var any = false;
            foreach (var t in _targets) if (t.Kind == k) { any = true; break; }
            if (!any) continue;
            var width = k == Kind.Hero ? HeroWidth : EnemyWidth;
            batch.Begin(SpriteSortMode.Deferred, NoColor, SamplerState.PointClamp, WriteRing[(int)k - 1], null, fx);
            foreach (var t in _targets)
            {
                if (t.Kind != k) continue;
                for (var d = 1; d <= width; d++)
                {
                    Blit(batch, t, d, 0);
                    Blit(batch, t, -d, 0);
                    Blit(batch, t, 0, d);
                    Blit(batch, t, 0, -d);
                }
            }
            batch.End();

            var color = k switch { Kind.Hero => HeroColor, Kind.Elite => EliteColor, _ => EnemyColor };
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, PaintRing[(int)k - 1], null);
            batch.Draw(prim.Pixel, new Rectangle(0, 0, targetW, targetH), color.ToXna(Alpha));
            batch.End();
        }

        // 5. 번짐: ½ → ¼ 선형 축소(테가 번짐) → world 에 가산(번진 테 두 겹 + 약한 선)
        Downsample(device, batch, _line!, _half!);
        Downsample(device, batch, _half!, _quarter!);
        device.SetRenderTarget(world);
        batch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp);
        var full = new Rectangle(0, 0, targetW, targetH);
        batch.Draw(_quarter, full, Color.White * GlowAlpha);
        batch.Draw(_half, full, Color.White * GlowAlpha);
        batch.Draw(_line, full, Color.White * LineAlpha);
        batch.End();
    }

    private static void Blit(SpriteBatch batch, in Target t, float dx, float dy) =>
        BatchX.Blit(batch, t.Texture, new RectF(t.Dest.X0 + dx, t.Dest.Y0 + dy, t.Dest.X1 + dx, t.Dest.Y1 + dy), t.Source, Color.White, t.Depth);

    /// <summary>테: 몸이 아니고(스텐실 &lt; 값 — 몸 0x80 은 큼) 이 대상보다 앞 벽이 덮은 픽셀(정점 깊이 &gt; 버퍼)에 값을 씀.</summary>
    private static DepthStencilState Ring(int value) => new()
    {
        Name = $"OutlineRing{value}", DepthBufferEnable = true, DepthBufferWriteEnable = false, DepthBufferFunction = CompareFunction.Greater,
        StencilEnable = true, StencilFunction = CompareFunction.Greater, StencilPass = StencilOperation.Replace, ReferenceStencil = value,
    };

    private static DepthStencilState Paint(int value) => new()
    {
        Name = $"OutlinePaint{value}", DepthBufferEnable = false, DepthBufferWriteEnable = false,
        StencilEnable = true, StencilFunction = CompareFunction.Equal, StencilPass = StencilOperation.Keep, ReferenceStencil = value,
    };

    private void EnsureTargets(GraphicsDevice device, int w, int h)
    {
        if (_line is not null && _line.Width == w && _line.Height == h) return;
        _line?.Dispose();
        _half?.Dispose();
        _quarter?.Dispose();
        _line = new RenderTarget2D(device, w, h, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.DiscardContents);
        _half = new RenderTarget2D(device, Math.Max(1, w / 2), Math.Max(1, h / 2), false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
        _quarter = new RenderTarget2D(device, Math.Max(1, w / 4), Math.Max(1, h / 4), false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
    }

    private static void Downsample(GraphicsDevice device, SpriteBatch batch, RenderTarget2D src, RenderTarget2D dst)
    {
        device.SetRenderTarget(dst);
        device.Clear(Color.Transparent);
        batch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearClamp);
        batch.Draw(src, new Rectangle(0, 0, dst.Width, dst.Height), Color.White);
        batch.End();
    }

    public void Dispose()
    {
        _effect?.Dispose();
        _line?.Dispose();
        _half?.Dispose();
        _quarter?.Dispose();
    }
}
