using DiabloDefenceRemastered.Core.Sim.Enemies.Parts;

namespace DiabloDefenceRemastered.Core.Sim.Enemies;

/// <summary>
/// 적 만들기 (설계서 6.7 <c>EnemyFactory.Create</c>): 정의 + 등급 + 접두사를 합쳐 인스턴스에 굳힌다(데코레이터 방식 — 정의는 그대로).
/// 최종 크기 = min(정의 scale × 등급 배율, <c>max_scale</c>), 몸 반경은 자른 크기로(6.5), HP·피해 배율은 그 순간 t(난이도 분)의 시간 배율.
/// <c>BodyTop</c>·<c>TouchReach</c>(6.6)도 여기서. 생성 직후 생성 훅(입장·등장 중이면 들어온 뒤).
/// </summary>
public static class EnemyFactory
{
    public static Enemy Create(World world, EnemyDef def, EnemyRank rank, ReadOnlySpan<int> affixes, Vector2 pos, in SpawnContext spawn,
                               LifeState life = LifeState.Active)
    {
        var e = world.Enemies.Add();
        var defs = world.Defs;
        var rankDef = RankOf(world, rank);
        int a0 = affixes.Length > 0 ? affixes[0] : -1, a1 = affixes.Length > 1 ? affixes[1] : -1;

        e.Def = def;
        e.DefIndex = defs.Enemies.IndexOf(def.Id);
        e.Faction = Faction.Enemy;
        e.Rank = rank;
        e.AffixIdx[0] = (short)a0;
        e.AffixIdx[1] = (short)a1;
        e.SpawnTick = world.Tick;
        e.Life = life;
        e.Pos = e.PrevPos = pos;
        e.Source = spawn.Source;
        e.Kit = world.KitFor(def, a0, a1);

        var scale = FinalScale(world, def, rank);
        e.FinalScale = scale;
        e.Radius = BodyRadius(world, def, rank);

        float hpPct = 0f, dmgPct = 0f, armor = 0f, kb = 0f, speedPct = 0f, cdPct = 0f;
        var immunities = Immunities.FromNames(def.Immunities);
        foreach (var ai in affixes)
        {
            if (ai < 0) continue;
            var a = defs.Affixes[ai];
            hpPct += Stat(a.Stats, EnemyStatKey.HpPct);
            dmgPct += Stat(a.Stats, EnemyStatKey.DamagePct);
            armor += Stat(a.Stats, EnemyStatKey.Armor);
            kb += Stat(a.Stats, EnemyStatKey.KnockbackResist);
            speedPct += Stat(a.Stats, EnemyStatKey.SpeedPct);
            cdPct += Stat(a.Stats, EnemyStatKey.AttackCooldownPct);
            immunities |= Immunities.FromNames(a.Immunities);
        }

        var minutes = world.DifficultyMinutes;
        var scaling = world.Stage?.Scaling;
        var timeHp = def.TimeScaling && scaling is not null ? EnemyMath.TimeHpMul(scaling, minutes) : 1f;
        var timeDmg = def.TimeScaling && scaling is not null ? EnemyMath.TimeDmgMul(scaling, minutes) : 1f;
        e.Hp = e.MaxHp = EnemyMath.FinalHp(def.Hp, timeHp, rankDef?.Hp ?? 1f, hpPct);
        // --boss-hp(6.20 ⑩): 중간 보스·보스는 최대 × 값으로 시작 — 첫 틱의 페이즈 처리가 문턱대로 넘긴다
        if (world.Options.BossHp is { } bossHp && def.Rank is DefRank.Miniboss or DefRank.Boss) e.Hp = e.MaxHp * Math.Clamp(bossHp, 1e-4f, 1f);
        e.DamageMul = timeDmg * (rankDef?.Damage ?? 1f) * (1f + dmgPct);
        e.Armor = def.Armor + armor;
        var resist = def.KnockbackResist;
        if (rankDef?.KnockbackResist is { } fixedResist) resist = fixedResist;
        else if (rankDef?.KnockbackResistAdd is { } add) resist += add;
        e.KnockbackResist = Math.Clamp(resist + kb, 0f, 1f);
        e.Immunities = immunities;
        e.BaseMoveSpeed = def.Speed * (rankDef?.Speed ?? 1f);
        e.AffixSpeedPct = speedPct;
        e.AffixCooldownPct = cdPct;
        e.MoveSpeed = e.BaseMoveSpeed * (1f + speedPct);
        e.PassThroughObstacles = HasPart<PhaseThrough>(e.Kit);
        e.EnrageAt = def.Enrage is { } enrage ? world.Tick + MathX.SecondsToTicks(enrage.After.Seconds, World.Dt) : -1;
        // 정해진 순서(6.20 ③): 첫 칸까지 쉼 = 정의 sequence.start(등장 — 들어온 뒤부터 준다)
        if (e.Kit.SequencerSlot >= 0) e.Parts[e.Kit.SequencerSlot].Cooldown = def.Sequence?.Start ?? 0f;

        var set = def.Sprites.IsBound ? def.Sprites.Def : null;
        e.BodyTop = (set?.BodyHeight ?? 0f) * scale;
        var margin = defs.Rules.Sight?.TouchMargin ?? 0f;
        e.TouchReach = e.Kit.MeleeReachOver > 0f ? MathF.Max(margin, e.Kit.MeleeReachOver + 10f) : margin;

        var toHero = world.Hero.Pos - pos;
        e.Facing = MathX.NormalizeOr(toHero, new Vector2(0, 1));
        e.Anim.Play(ClipKind.Idle, loop: true);
        e.Intent = life == LifeState.Active ? MoveIntent.SeekHero() : MoveIntent.Hold;

        if (life == LifeState.Active) RunSpawnHooks(world, e, spawn);
        else
        {
            e.PendingSpawn = spawn;
            e.SpawnHooksPending = true;
        }
        return e;
    }

    /// <summary>생성 훅(입장·등장이 끝난 뒤에도 같은 길).</summary>
    public static void RunSpawnHooks(World world, Enemy e, in SpawnContext spawn)
    {
        e.SpawnHooksPending = false;
        var kit = e.Kit;
        if (kit.SpawnHooks.Length == 0) return;
        var ctx = new EnemyCtx(world, e);
        foreach (var s in kit.SpawnHooks)
            if (kit.Part(e.PhaseIdx, s) is ISpawnHookPart hook) hook.OnSpawn(ctx, spawn);
    }

    public static RankDef? RankOf(World world, EnemyRank rank) => rank switch
    {
        EnemyRank.Elite => world.Defs.Rules.Ranks?.Elite,
        EnemyRank.Unique => world.Defs.Rules.Ranks?.Unique,
        _ => null,
    };

    /// <summary>최종 크기 = min(정의 scale × 등급 배율, max_scale).</summary>
    public static float FinalScale(World world, EnemyDef def, EnemyRank rank)
    {
        var ranks = world.Defs.Rules.Ranks;
        var mul = RankOf(world, rank)?.Scale ?? 1f;
        return MathF.Min(def.Scale * mul, ranks?.MaxScale ?? 1.6f);
    }

    /// <summary>몸 반경 = round(base_radius × 최종 크기), 정의가 radius 를 적었으면 round(radius × 최종 크기 / scale)(6.5).</summary>
    public static float BodyRadius(World world, EnemyDef def, EnemyRank rank)
    {
        var scale = FinalScale(world, def, rank);
        if (def.Radius is { } r) return MathF.Round(r * scale / (def.Scale > 0f ? def.Scale : 1f));
        var baseRadius = def.Sprites.IsBound ? def.Sprites.Def.BaseRadius : 12f;
        return MathF.Round(baseRadius * scale);
    }

    private static float Stat(IReadOnlyDictionary<EnemyStatKey, float> stats, EnemyStatKey key) =>
        stats.TryGetValue(key, out var v) ? v : 0f;

    private static bool HasPart<T>(EnemyKit kit)
    {
        for (var s = 0; s < kit.Count; s++)
            if (kit.Part(0, s) is T) return true;
        return false;
    }
}
