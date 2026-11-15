using System.Globalization;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using Matrix = Microsoft.Xna.Framework.Matrix;
using XnaVector3 = Microsoft.Xna.Framework.Vector3;

namespace DiabloDefenceRemastered.Rendering;

/// <summary>
/// 바닥 예고 불빛(2026-11-15 "붉은 궤적이 여전히 그림판으로 그린 느낌", "바닥에서 붉은 불빛이 나는 느낌으로" — <c>GroundGlow.fx</c>).
/// 예고(<see cref="GroundLight"/>) 하나에 둘레까지 넉넉한 사각형 하나 — 원은 반경 × <see cref="CircleSpill"/>, 막대는 폭 × <see cref="BarSpill"/>·끝으로 폭만큼 더.
/// 셰이더가 부드러운 모양·밝은 테·바깥 번짐·일렁임을 계산한다. 쓰는 곳: 빛 지도(필터 → 가산 — 둘레가 붉게 물듦), 7층 발광(바닥에서 올라오는 불빛).
/// 셰이더를 못 읽으면 null(빛 지도는 예전 모양 텍스처로).
/// </summary>
public sealed class GroundGlowRenderer : IDisposable
{
    public const float CircleSpill = 1.45f, BarSpill = 3f;

    private readonly Effect _effect;

    private GroundGlowRenderer(Effect effect) => _effect = effect;

    public static GroundGlowRenderer? TryLoad(GraphicsDevice device, IAppLog log)
    {
        try
        {
            var effect = new Effect(device, File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Shaders", "GroundGlow.mgfxo")));
            log.Info("GROUNDGLOW shader=on");
            return new GroundGlowRenderer(effect);
        }
        catch (Exception e)
        {
            log.Info(string.Create(CultureInfo.InvariantCulture, $"GROUNDGLOW shader=off reason=\"{e.GetType().Name}: {e.Message}\""));
            return null;
        }
    }

    /// <summary>
    /// 지금 걸린 타깃에 예고 불빛을 그린다. <paramref name="toTarget"/> = 월드 px → 타깃 px, <paramref name="filter"/> 면 (1 − 색) × 값(블렌드가 곱하기 필터),
    /// 아니면 색 × 값(가산). <paramref name="strength"/> 는 예고 세기에 곱함.
    /// </summary>
    public void Draw(SpriteBatch batch, Texture2D pixel, IReadOnlyList<GroundLight> lights, Matrix toTarget, Int2 targetSize, BlendState blend,
                     bool filter, float strength, float time)
    {
        if (lights.Count == 0) return;
        var p = _effect.Parameters;
        p["MatrixTransform"]?.SetValue(toTarget * Matrix.CreateOrthographicOffCenter(0, targetSize.X, targetSize.Y, 0, 0, -1));
        p["Time"]?.SetValue(time);
        p["Mode"]?.SetValue(filter ? 1f : 0f);
        // 색이 다른 예고는 배치를 나눈다(색은 효과 값) — 보통 모두 붉은색이라 한 번
        Rgb? current = null;
        var open = false;
        foreach (var g in lights)
        {
            if (current != g.Color)
            {
                if (open) batch.End();
                current = g.Color;
                p["Tint"]?.SetValue(new XnaVector3(g.Color.R / 255f, g.Color.G / 255f, g.Color.B / 255f));
                batch.Begin(SpriteSortMode.Deferred, blend, SamplerState.LinearClamp, null, null, _effect);
                open = true;
            }
            var k = Math.Clamp(g.Intensity * strength, 0f, 4f);
            if (g.Line)
            {
                var d = g.B - g.A;
                var len = d.Length();
                if (len < 0.5f) continue;
                var dir = d / len;
                var width = g.Size * 2f * BarSpill;
                var pad = g.Size * 2f;
                var start = g.A - dir * pad;
                var total = len + pad * 2f;
                batch.Draw(pixel, new XnaVector2(start.X, start.Y), null, new Color(g.Progress, 1f, len / total, k / 4f),
                           MathF.Atan2(dir.Y, dir.X), new XnaVector2(0f, 0.5f), new XnaVector2(total, width), SpriteEffects.None, 0f);
            }
            else
            {
                var r = g.Size * CircleSpill;
                batch.Draw(pixel, new XnaVector2(g.A.X - r, g.A.Y - r), null, new Color(g.Progress, 0f, 0f, k / 4f),
                           0f, XnaVector2.Zero, new XnaVector2(r * 2f, r * 2f), SpriteEffects.None, 0f);
            }
        }
        if (open) batch.End();
    }

    public void Dispose() => _effect.Dispose();
}
