namespace DiabloDefenceRemastered.Core.Sim.Core;

/// <summary>
/// 단계 1 BeginTick (WP1, 설계서 3.5): PrevPos ← Pos, 틱 이벤트 큐 바꿔 끼우기, 버프·무적·번쩍임 타이머 감소, 화면 사각형,
/// 시야 갱신(<c>World.Visibility.Update</c>, v0.5 WP1c).
/// 스테이지 시계(<see cref="World.Tick"/>)는 EndTick 끝에서 늘어난다 — 단계들이 도는 동안 Tick 은 이번 틱 번호다.
/// </summary>
public sealed class BeginTickSystem : ISimSystem
{
    public void Tick(World world)
    {
        world.Events.Swap();

        var hero = world.Hero;
        hero.PrevPos = hero.Pos;
        hero.InvulnTime = MathF.Max(0f, hero.InvulnTime - World.Dt);
        hero.HitFlashTime = MathF.Max(0f, hero.HitFlashTime - World.Dt);

        var tick = world.Tick;
        var enemies = world.Enemies.Items;
        for (var i = 0; i < enemies.Length; i++)
        {
            var e = enemies[i];
            e.PrevPos = e.Pos;
            e.HitFlashTime = MathF.Max(0f, e.HitFlashTime - World.Dt);
            e.Buffs.RemoveExpired(tick);
        }

        foreach (var p in world.Projectiles.Items) p.PrevPos = p.Pos;
        foreach (var h in world.Hazards.Items) h.PrevPos = h.Pos;
        foreach (var t in world.Telegraphs.Items) t.PrevPos = t.Pos;
        foreach (var k in world.Pickups.Items) k.PrevPos = k.Pos;

        world.ViewRect = CameraMath.ViewRect(hero.Pos, world.MapSize, world.ViewSize);

        // 시야 갱신(4.11): 화면 사각형 다음. 첫 틱이 첫 계산이다. 생성(2)·적 생각(5)이 같은 틱에 같은 시야를 본다
        world.Visibility.Update(world);
    }
}
