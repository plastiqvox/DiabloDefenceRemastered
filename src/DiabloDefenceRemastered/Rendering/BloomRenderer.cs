using System.Globalization;
using DiabloDefenceRemastered.Core.Art;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DiabloDefenceRemastered.Rendering;

/// <summary>
/// 블룸(2026-11-14 사용자 요청, 설계서 7.7 ⑦b — 7층 발광 뒤·8층 화면 효과 앞): 빛을 곱한 <c>WorldRT</c> 에서 밝은 부분만(<c>BloomExtract.fx</c> — 문턱·부드러운 무릎)
/// ½ 크기로 뽑고 → ¼ 로 줄여 가로·세로 가우스(<c>BloomBlur.fx</c>) → ⅛ 로 한 번 더 줄여 흐림(넓은 번짐) → 두 층을 <c>WorldRT</c> 에 가산한다.
/// 어두운 던전이라 주인공 빛 안 바닥은 문턱 아래고, 불덩이·폭발·번개·발광처럼 밝은 것만 번진다. 셰이더를 못 읽으면 null(블룸 없음).
/// </summary>
public sealed class BloomRenderer : IDisposable
{
    /// <summary>밝기(RGB 최댓값) 문턱 — 이보다 밝은 만큼만 번짐.</summary>
    public const float Threshold = 0.62f;
    /// <summary>문턱 둘레 부드러운 무릎 폭.</summary>
    public const float Knee = 0.2f;
    /// <summary>¼·⅛ 층을 더하는 세기.</summary>
    public const float NearIntensity = 0.55f, WideIntensity = 0.45f;

    private readonly GraphicsDevice _device;
    private readonly Effect _extract;
    private readonly Effect _blur;
    private Int2 _size;
    private RenderTarget2D? _half, _qa, _qb, _ea, _eb;

    private BloomRenderer(GraphicsDevice device, Effect extract, Effect blur)
    {
        _device = device;
        _extract = extract;
        _blur = blur;
    }

    /// <summary>출력 <c>Shaders\</c> 의 블룸 셰이더 둘을 읽는다. 없거나 실패하면 null(로그 한 줄).</summary>
    public static BloomRenderer? TryLoad(GraphicsDevice device, IAppLog log)
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Shaders");
        try
        {
            var extract = new Effect(device, File.ReadAllBytes(Path.Combine(dir, "BloomExtract.mgfxo")));
            var blur = new Effect(device, File.ReadAllBytes(Path.Combine(dir, "BloomBlur.mgfxo")));
            log.Info("BLOOM shader=on");
            return new BloomRenderer(device, extract, blur);
        }
        catch (Exception e)
        {
            log.Info(string.Create(CultureInfo.InvariantCulture, $"BLOOM shader=off reason=\"{e.GetType().Name}: {e.Message}\""));
            return null;
        }
    }

    /// <summary><paramref name="world"/> 에 블룸을 더한다(끝나면 <paramref name="world"/> 가 걸린 채로 돌아옴).</summary>
    public void Apply(SpriteBatch batch, RenderTarget2D world)
    {
        Ensure(new Int2(world.Width, world.Height));
        // ① 밝은 부분 ½
        Pass(batch, world, _half!, _extract, e =>
        {
            e.Parameters["Threshold"]?.SetValue(Threshold);
            e.Parameters["Knee"]?.SetValue(Knee);
        });
        // ② ¼ 로 줄이고 가로·세로 흐림
        Pass(batch, _half!, _qa!, null, null);
        Blur(batch, _qa!, _qb!);
        // ③ ⅛ 로 줄이고 흐림(넓은 번짐)
        Pass(batch, _qa!, _ea!, null, null);
        Blur(batch, _ea!, _eb!);
        // ④ 가산
        _device.SetRenderTarget(world);
        var full = new Rectangle(0, 0, world.Width, world.Height);
        batch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp);
        batch.Draw(_qa!, full, new Color(NearIntensity, NearIntensity, NearIntensity, 1f));
        batch.Draw(_ea!, full, new Color(WideIntensity, WideIntensity, WideIntensity, 1f));
        batch.End();
    }

    /// <summary>가로(a → b) → 세로(b → a). 결과는 <paramref name="a"/>.</summary>
    private void Blur(SpriteBatch batch, RenderTarget2D a, RenderTarget2D b)
    {
        Pass(batch, a, b, _blur, e => e.Parameters["Step"]?.SetValue(new Microsoft.Xna.Framework.Vector2(1f / a.Width, 0f)));
        Pass(batch, b, a, _blur, e => e.Parameters["Step"]?.SetValue(new Microsoft.Xna.Framework.Vector2(0f, 1f / b.Height)));
    }

    private void Pass(SpriteBatch batch, Texture2D src, RenderTarget2D dst, Effect? effect, Action<Effect>? set)
    {
        _device.SetRenderTarget(dst);
        _device.Clear(Color.Black);
        if (effect is not null)
        {
            effect.Parameters["MatrixTransform"]?.SetValue(Matrix.CreateOrthographicOffCenter(0, dst.Width, dst.Height, 0, 0, -1));
            set?.Invoke(effect);
        }
        batch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, null, null, effect);
        batch.Draw(src, new Rectangle(0, 0, dst.Width, dst.Height), Color.White);
        batch.End();
    }

    private void Ensure(Int2 size)
    {
        if (_half is not null && _size == size) return;
        DisposeTargets();
        _size = size;
        _half = Target(size.X / 2, size.Y / 2);
        _qa = Target(size.X / 4, size.Y / 4);
        _qb = Target(size.X / 4, size.Y / 4);
        _ea = Target(size.X / 8, size.Y / 8);
        _eb = Target(size.X / 8, size.Y / 8);
    }

    private RenderTarget2D Target(int w, int h) =>
        new(_device, Math.Max(1, w), Math.Max(1, h), false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);

    private void DisposeTargets()
    {
        _half?.Dispose(); _qa?.Dispose(); _qb?.Dispose(); _ea?.Dispose(); _eb?.Dispose();
        _half = _qa = _qb = _ea = _eb = null;
    }

    public void Dispose()
    {
        DisposeTargets();
        _extract.Dispose();
        _blur.Dispose();
    }
}
