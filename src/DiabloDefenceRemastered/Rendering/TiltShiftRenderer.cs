using System.Globalization;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using Matrix = Microsoft.Xna.Framework.Matrix;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DiabloDefenceRemastered.Rendering;

/// <summary>
/// 틸트 시프트 + 비네트(2026-11-15 사용자 요청 "HD-2D게임들의 기법을 조금" — 설계서 7.7 ⑦c, 블룸 뒤·디버그 겹침·8층 화면 효과 앞).
/// <c>WorldRT</c> → 임시 타깃(<c>TiltShift.fx</c>) → <c>WorldRT</c> 로 되돌려 그린다. 주인공 높이의 띠는 또렷하고 위아래 끝은 흐리며, 모서리는 어둡다.
/// 셰이더를 못 읽으면 null(효과 없음).
/// </summary>
public sealed class TiltShiftRenderer : IDisposable
{
    /// <summary>또렷한 띠 반폭(화면 높이 비율).</summary>
    public const float Band = 0.2f;
    /// <summary>띠 끝에서 최대 흐림까지(화면 높이 비율).</summary>
    public const float Ramp = 0.28f;
    /// <summary>최대 흐림 반경(월드 px — 그림 배율을 곱해 타깃 px).</summary>
    public const float MaxBlurWorld = 2.2f;
    /// <summary>모서리 어둡기.</summary>
    public const float Vignette = 0.45f;

    private readonly GraphicsDevice _device;
    private readonly Effect _effect;
    private RenderTarget2D? _tmp;

    private TiltShiftRenderer(GraphicsDevice device, Effect effect)
    {
        _device = device;
        _effect = effect;
    }

    public static TiltShiftRenderer? TryLoad(GraphicsDevice device, IAppLog log)
    {
        try
        {
            var effect = new Effect(device, File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Shaders", "TiltShift.mgfxo")));
            log.Info("TILTSHIFT shader=on");
            return new TiltShiftRenderer(device, effect);
        }
        catch (Exception e)
        {
            log.Info(string.Create(CultureInfo.InvariantCulture, $"TILTSHIFT shader=off reason=\"{e.GetType().Name}: {e.Message}\""));
            return null;
        }
    }

    /// <summary><paramref name="world"/> 에 적용(끝나면 <paramref name="world"/> 가 걸린 채로 돌아옴). <paramref name="focus"/> = 주인공 화면 높이(0..1).</summary>
    public void Apply(SpriteBatch batch, RenderTarget2D world, float focus, int artScale)
    {
        if (_tmp is null || _tmp.Width != world.Width || _tmp.Height != world.Height)
        {
            _tmp?.Dispose();
            _tmp = new RenderTarget2D(_device, world.Width, world.Height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
        }
        var p = _effect.Parameters;
        p["MatrixTransform"]?.SetValue(Matrix.CreateOrthographicOffCenter(0, world.Width, world.Height, 0, 0, -1));
        p["TexelSize"]?.SetValue(new XnaVector2(1f / world.Width, 1f / world.Height));
        p["Focus"]?.SetValue(Math.Clamp(focus, 0f, 1f));
        p["Band"]?.SetValue(Band);
        p["Ramp"]?.SetValue(Ramp);
        p["MaxBlur"]?.SetValue(MaxBlurWorld * artScale);
        p["Vignette"]?.SetValue(Vignette);
        p["Aspect"]?.SetValue(world.Width / (float)world.Height);

        _device.SetRenderTarget(_tmp);
        batch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, null, null, _effect);
        batch.Draw(world, new Rectangle(0, 0, _tmp.Width, _tmp.Height), Color.White);
        batch.End();

        _device.SetRenderTarget(world);
        batch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp);
        batch.Draw(_tmp, new Rectangle(0, 0, world.Width, world.Height), Color.White);
        batch.End();
    }

    public void Dispose()
    {
        _tmp?.Dispose();
        _effect.Dispose();
    }
}
