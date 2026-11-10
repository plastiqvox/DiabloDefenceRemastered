namespace DiabloDefenceRemastered.Core.Sim.Combat;

/// <summary>표시 이벤트 내기 도우미(5.6 — 규칙에 영향 없음).</summary>
public static class CombatFx
{
    public static void Effect(World world, EffectRef fx, Vector2 pos, float radius, Vector2 dir)
    {
        if (!world.Presentation.Enabled) return;
        var def = fx.Effect.Def;
        world.Presentation.Effect(new FxEvent(world.Tick, def, pos, fx.Scale ?? 1f, fx.Tint ?? def.Tint, radius, dir));
    }
}
