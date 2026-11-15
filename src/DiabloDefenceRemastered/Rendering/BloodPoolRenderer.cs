using System.Globalization;
using DiabloDefenceRemastered.Core.Art;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using Matrix = Microsoft.Xna.Framework.Matrix;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DiabloDefenceRemastered.Rendering;

/// <summary>
/// 바닥 핏웅덩이 그리기(2026-11-15 — <c>BloodPool.fx</c>, 0층 바닥 바로 뒤·벽 앞). 자리는 <see cref="BloodPools.Place"/>(월드마다 한 번).
/// 웅덩이마다 가로 2r × 세로 r 사각형 하나, 정점 색 R 에 모양 씨앗. 셰이더를 못 읽으면 null(웅덩이 없음).
/// </summary>
public sealed class BloodPoolRenderer : IDisposable
{
    /// <summary>진하기(정점 알파).</summary>
    public const float Opacity = 0.92f;

    private readonly Effect _effect;

    private BloodPoolRenderer(Effect effect) => _effect = effect;

    public static BloodPoolRenderer? TryLoad(GraphicsDevice device, IAppLog log)
    {
        try
        {
            var effect = new Effect(device, File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Shaders", "BloodPool.mgfxo")));
            log.Info("BLOOD shader=on");
            return new BloodPoolRenderer(effect);
        }
        catch (Exception e)
        {
            log.Info(string.Create(CultureInfo.InvariantCulture, $"BLOOD shader=off reason=\"{e.GetType().Name}: {e.Message}\""));
            return null;
        }
    }

    /// <summary>
    /// 지금 걸린 타깃에 웅덩이를 그린다(배치는 닫혀 있어야 함). 화면 밖 웅덩이는 건너뜀.
    /// 웅덩이마다 물결(<paramref name="ripples"/>)이 달라 웅덩이 하나씩 배치를 연다(방에 2~4개).
    /// </summary>
    public void Draw(SpriteBatch batch, Texture2D pixel, IReadOnlyList<BloodPool> pools, BloodRipples ripples, float time,
                     RectF view, Matrix toTarget, Int2 targetSize)
    {
        if (pools.Count == 0) return;
        var prm = _effect.Parameters;
        _effect.CurrentTechnique = _effect.Techniques["BloodPool"];
        prm["MatrixTransform"]?.SetValue(toTarget * Matrix.CreateOrthographicOffCenter(0, targetSize.X, targetSize.Y, 0, 0, -1));
        prm["Time"]?.SetValue(time);
        for (var i = 0; i < pools.Count; i++)
        {
            var p = pools[i];
            var r = p.Radius;
            if (p.Pos.X + r < view.X0 || p.Pos.X - r > view.X1 || p.Pos.Y + r < view.Y0 || p.Pos.Y - r > view.Y1) continue;
            var list = ripples.Of(i);
            for (var k = 0; k < _ripples.Length; k++)
            {
                var rp = k < list.Length ? list[k] : default;
                // 아직 없는 칸(-∞)은 아주 과거로 — 셰이더에서 수명 밖
                _ripples[k] = new Microsoft.Xna.Framework.Vector4(rp.Local.X, rp.Local.Y, float.IsFinite(rp.Start) && k < list.Length ? rp.Start : -1000f, 0f);
            }
            prm["Ripples"]?.SetValue(_ripples);
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, _effect);
            var dest = new Rectangle((int)MathF.Floor(p.Pos.X - r), (int)MathF.Floor(p.Pos.Y - r * 0.5f), (int)MathF.Ceiling(r * 2f), (int)MathF.Ceiling(r));
            batch.Draw(pixel, dest, new Color(p.Seed, 0f, 0f, Opacity));
            batch.End();
        }
    }

    /// <summary>핏자국 발자국 반 크기(px — 바닥 공간 정사각형 2a, 화면 2a × a).</summary>
    public const float FootprintHalf = 10f;

    /// <summary>핏자국 발자국(기법 <c>Footprint</c>) — 한 배치. 방향은 바닥 공간 각도(세로 ×2)로 정점 색 G 에.</summary>
    public void DrawFootprints(SpriteBatch batch, Texture2D pixel, IReadOnlyList<BloodFootprint> prints, float time,
                               RectF view, Matrix toTarget, Int2 targetSize)
    {
        if (prints.Count == 0) return;
        _effect.CurrentTechnique = _effect.Techniques["Footprint"];
        _effect.Parameters["MatrixTransform"]?.SetValue(toTarget * Matrix.CreateOrthographicOffCenter(0, targetSize.X, targetSize.Y, 0, 0, -1));
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, _effect);
        const float a = FootprintHalf;
        foreach (var p in prints)
        {
            var alpha = BloodFootprints.Alpha(p, time);
            if (alpha <= 0f || p.Pos.X + a < view.X0 || p.Pos.X - a > view.X1 || p.Pos.Y + a < view.Y0 || p.Pos.Y - a > view.Y1) continue;
            var angle = MathF.Atan2(p.Dir.Y * 2f, p.Dir.X);
            if (angle < 0f) angle += MathF.Tau;
            var dest = new Rectangle((int)MathF.Floor(p.Pos.X - a), (int)MathF.Floor(p.Pos.Y - a * 0.5f), (int)(a * 2f), (int)a);
            batch.Draw(pixel, dest, new Color(p.Seed, angle / MathF.Tau, 0f, alpha * Opacity));
        }
        batch.End();
    }

    private readonly Microsoft.Xna.Framework.Vector4[] _ripples = new Microsoft.Xna.Framework.Vector4[BloodRipples.PerPool];

    public void Dispose() => _effect.Dispose();
}
