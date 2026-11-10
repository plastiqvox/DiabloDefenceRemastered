namespace DiabloDefenceRemastered.Core.Sim.Heroes;

/// <summary>
/// 단계 3 HeroControl (WP3, 설계서 3.5): 능력치 다시 계산(바뀌었으면) → 레벨업 밀어내기(창이 닫힌 다음 틱) → 재생·시간 효과·허리띠 →
/// 명령의 이동 벡터 → <c>Hero.Intent</c>·<c>MoveSpeed</c>(무리 끈적임)·바라보는 방향 → 그림 클립(걷기·서기, 시전은 무기가 시작). 위치 적분은 WP2 <c>Locomotion</c>.
/// </summary>
public sealed class HeroControlSystem : ISimSystem
{
    public void Tick(World world)
    {
        var hero = world.Hero;
        var state = world.Combat;
        if (state.StatsDirty) HeroStats.Recompute(world);

        if (state.PushPending)
        {
            state.PushPending = false;
            Push(world);
        }

        if (world.TimedEffectsRun && state.SurgeTicks > 0 && --state.SurgeTicks == 0) state.SurgePct = 0f;

        if (hero.Dead)
        {
            hero.Intent = MoveIntent.Hold;
            Animate(world, hero, ClipKind.Die, loop: false);
            return;
        }

        var regen = hero.Stats[StatKey.Regen];
        if (regen > 0f && hero.Hp < hero.MaxHp) hero.Hp = MathF.Min(hero.MaxHp, hero.Hp + regen * World.Dt);
        HeroBelt.Check(world);

        var move = world.Outcome == RunOutcome.Running ? world.Command.Move : Vector2.Zero;
        var len = move.Length();
        var crowd = world.Defs.Rules.Hero?.CrowdSlow;
        var slow = crowd is not null && hero.TouchingCount >= crowd.Touching ? crowd.Mul : 1f;
        hero.MoveSpeed = HeroStats.MoveSpeed(hero) * slow;
        if (len > 1e-3f)
        {
            var dir = move / len;
            hero.Facing = dir;
            hero.Intent = MoveIntent.Heading(dir, MathF.Min(1f, len));
            Animate(world, hero, ClipKind.Walk, loop: true);
        }
        else
        {
            hero.Intent = MoveIntent.Hold;
            var casting = hero.Anim.Clip is ClipKind.CastFire or ClipKind.CastLightning or ClipKind.CastMagic or ClipKind.Attack && !hero.Anim.Done;
            Animate(world, hero, casting ? hero.Anim.Clip : ClipKind.Idle, loop: !casting);
        }
    }

    /// <summary>클립 고르기·진행. 고른 직업의 그림 묶음이 없으면(시험 월드) 클립 이름만 바꾼다.</summary>
    private static void Animate(World world, Hero hero, ClipKind clip, bool loop)
    {
        if (hero.Anim.Clip != clip || (hero.Anim.Done && loop)) hero.Anim.Play(clip, loop);
        if (world.HeroClass?.Sprites is not { IsBound: true } setRef) return;
        var set = setRef.Def;
        if (set.Clips.Get(clip) is not { } def || !def.Sprite.IsBound) return;
        var rate = clip == ClipKind.Walk ? AnimClock.WalkRate(hero.Facing * hero.MoveSpeed, set.BaseSpeed) : 1f;
        hero.Anim.Rate = rate;
        hero.Anim.Advance(World.Dt, def.FrameTime ?? set.FrameTime, def.Sprite.Def.Frames);
    }

    /// <summary>레벨업 밀어내기(<c>level_up_push</c> — 선택 창이 닫힌 다음 틱, 반경 안 적을 거리만큼, 피해 0 — 기획 12.6).</summary>
    private static void Push(World world)
    {
        if (world.Defs.Rules.LevelUpPush is not { Radius: > 0f, Distance: > 0f } push || world.Spatial is null) return;
        var hero = world.Hero;
        var buf = world.Combat.Query;
        var n = Math.Min(world.Spatial.Query(hero.Pos, push.Radius, buf), buf.Length);
        for (var k = 0; k < n; k++)
        {
            var e = world.Enemies[buf[k]];
            var d = e.Pos - hero.Pos;
            world.Damage.Push(e.Id, Faction.Hero, d.LengthSquared() > 1e-6f ? Vector2.Normalize(d) : new Vector2(0, 1), push.Distance);
        }
    }
}
