namespace DiabloDefenceRemastered.Core.Sim.Enemies;

/// <summary>
/// 부품이 받는 맥락 (설계서 6.6). 거리·길 트임·또렷이 보임·난수(Ai)·피해 계산·주인공 때리기·클립 도우미. 할당 없는 값 형식.
/// </summary>
public readonly struct EnemyCtx(World world, Enemy self)
{
    public World World => world;
    public Enemy Self => self;
    public EnemyKit Kit => self.Kit;
    public float Dt => World.Dt;
    public int Tick => world.Tick;
    public Vector2 HeroPos => world.Hero.Pos;
    public float DistToHero => Vector2.Distance(self.Pos, world.Hero.Pos);
    public Vector2 DirToHero => MathX.NormalizeOr(world.Hero.Pos - self.Pos, self.Facing);
    /// <summary>두 몸 반경 합.</summary>
    public float Contact => self.Radius + world.Hero.Radius;
    /// <summary>주인공에게 또렷이 보임(6.6 — 몸 위 끝까지, 시야가 꺼진 월드면 늘 참).</summary>
    public bool SeenClearly => world.Visibility.IsClearlyVisible(self.Pos, self.BodyTop);
    public RandomStream Rng => world.Rng.Ai;
    public SpriteSetDef Sprites => self.Def.Sprites.Def;

    /// <summary>붙은 버프의 그 키 합.</summary>
    public float BuffPct(EnemyStatKey key) => world.Defs.Buffs.Count == 0 ? 0f : self.Buffs.Sum(key, world.Defs.Buffs);

    /// <summary>이 적이 지금 내는 최종 피해(기본 null = 정의 damage).</summary>
    public int FinalDamage(float? baseDamage) =>
        EnemyMath.FinalDamage(baseDamage ?? self.Def.Damage, self.DamageMul, BuffPct(EnemyStatKey.DamagePct), self.EnrageDamageMul);

    /// <summary>능력·패턴 쿨에 <c>attack_cooldown_pct</c>(접두사 + 버프) 반영.</summary>
    public float Cooldown(float cooldown) => EnemyMath.Cooldown(cooldown, self.AffixCooldownPct + BuffPct(EnemyStatKey.AttackCooldownPct));

    /// <summary>주인공에게 피해 요청(나가는 피해 고치기 부품을 거친다).</summary>
    public void HitHero(int amount, Vector2 knockDir = default, float knockDist = 0f)
    {
        var request = new DamageRequest(EntityId.Hero, self.Id, Faction.Enemy, amount, 0f, 0f, -1, knockDir, knockDist, DamageFlags.None);
        var kit = self.Kit;
        foreach (var s in kit.HitModifiers)
            if (kit.Part(self.PhaseIdx, s) is IHitModifierPart m) m.ModifyOutgoing(this, ref request);
        world.Damage.Add(request);
    }

    /// <summary>
    /// 클립 시작. 쿨이 클립 길이보다 짧으면 그 클립만 쿨에 맞춰 빠르게 재생한다(기획 8.1). 신호 <c>EnemyAttack</c>/<c>EnemySpecial</c>.
    /// </summary>
    public void PlayClip(ClipKind clip, float cooldown = 0f)
    {
        var rate = self.Def.AnimSpeed > 0f ? self.Def.AnimSpeed : 1f;
        var len = ClipLength(clip) / rate;
        if (cooldown > 0f && len > cooldown) rate *= len / cooldown;
        self.Anim.Play(clip, loop: false, rate);
        if (clip == ClipKind.Attack) world.Presentation.Cue(new CueEvent(world.Tick, CueKind.EnemyAttack, self.Pos, self.Def, self.Id));
        else if (clip == ClipKind.Special) world.Presentation.Cue(new CueEvent(world.Tick, CueKind.EnemySpecial, self.Pos, self.Def, self.Id));
    }

    /// <summary>지금 클립을 한 틱 진행(이벤트 프레임 1부터, 0 = 그림 묶음 값).</summary>
    public AnimStep AdvanceClip(int eventFrame = 0)
    {
        var (frameTime, frames, ev) = ClipInfo(self.Anim.Clip);
        if (frames <= 0 || frameTime <= 0f)
        {
            // 클립이 없는 그림 묶음(합성 정의): 첫 진행에서 이벤트와 끝을 함께 알린다 — 행동이 멈추지 않게
            if (self.Anim.Done) return AnimStep.Finished;
            self.Anim.Done = true;
            return AnimStep.EventFrame | AnimStep.Finished;
        }
        return self.Anim.Advance(World.Dt, frameTime, frames, eventFrame > 0 ? eventFrame : ev);
    }

    /// <summary>클립 길이(초, 배율 전). 클립이 없으면 0.</summary>
    public float ClipLength(ClipKind clip)
    {
        var (frameTime, frames, _) = ClipInfo(clip);
        return frameTime * frames;
    }

    /// <summary>클립의 이벤트 프레임 시각(초, 배율 전 — (event_frame − 1) × 프레임 시간). 없으면 0.</summary>
    public float EventTime(ClipKind clip, int eventFrame = 0)
    {
        var (frameTime, _, ev) = ClipInfo(clip);
        var f = eventFrame > 0 ? eventFrame : ev;
        return f >= 1 ? (f - 1) * frameTime : 0f;
    }

    /// <summary>(프레임 시간, 프레임 수, 이벤트 프레임). 클립이 없으면 (0, 0, 0).</summary>
    public (float FrameTime, int Frames, int EventFrame) ClipInfo(ClipKind clip)
    {
        var set = Sprites;
        var def = set.Clips.Get(clip);
        if (def is null || !def.Sprite.IsBound) return (0f, 0, 0);
        return (def.FrameTime ?? set.FrameTime, def.Sprite.Def.Frames, def.EventFrame ?? 0);
    }
}
