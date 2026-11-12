using DiabloDefenceRemastered.Core.Sim.Combat;

namespace DiabloDefenceRemastered.Core.Sim.Heroes;

/// <summary>
/// 주인공 능력치 합 한 곳(기획 4.1 적용 규칙, 설계서 6.10·6.16·6.19): <c>rules.hero.stats</c> → 직업 <c>stats</c> 가 키별로 덮어씀 → 특성(1레벨)·패시브·축복 <c>per_level</c> × 레벨(또는 <c>levels</c> 앞 L 개의 합 — 6.20 ⑯) → 짐 <c>Mods</c>(<c>단련</c>·성소 대가)를 더함
/// → 장비(v0.9 — 출처 <see cref="StatSources.Equipment"/>, <c>EquipStats.Add</c>) → <b>모든 출처를 합친 뒤</b> <c>EquipStats.Clamp</c>(방어 ≤ <c>armor_cap</c>, <c>move_pct</c> ≥ <c>weight_move_floor</c> — <c>rules.equipment</c> 가 있을 때).
/// 쿨다운 상한은 여기서 자르지 않는다(<c>DamageMath</c> 가 마력 폭주와 함께 자름).
/// 최대 체력 = round(<c>max_hp</c> × (1 + <c>max_hp_pct</c>)). 비율이 바뀌면 지금 체력도 같은 비율로(v0.8 매혹의 성소), 더하기만 바뀌면 지금 체력은 그대로(회복은 <c>on_acquire</c>·채움 카드가 따로,
/// 장비를 끼운 순간의 오른 만큼은 <see cref="HeroEquipment.Equip"/> 가 <c>EquipStats.RebaseHp</c> 로).
/// </summary>
public static class HeroStats
{
    public static void Recompute(World world)
    {
        var hero = world.Hero;
        var s = hero.Stats;
        var blinkMax = HeroBlink.MaxCharges(world);   // 보스 묶음 B: 최대 칸이 늘면 늘어난 만큼 바로 찬다(6.20 ⑯)
        s.Clear();
        if (world.Defs.Rules.Hero is { } rules)
            foreach (var (k, v) in rules.Stats) s[k] = v;
        if (world.HeroClass is { } cls)
        {
            foreach (var (k, v) in cls.Stats) s[k] = v;
            foreach (var t in cls.Traits) Add(s, t.Def, 1);
        }
        for (var i = 0; i < PassiveSlots.SlotCount; i++)
            if (hero.Passives[i].Def is { } p) Add(s, p, hero.Passives[i].Level);
        for (var i = 0; i < BlessingSlots.SlotCount; i++)
            if (hero.Blessings[i].Def is { } b) Add(s, b, hero.Blessings[i].Level);
        var mods = world.Combat.Mods;
        for (var i = 0; i < mods.Length; i++) s[(StatKey)i] += mods[i];
        if (world.Defs.Rules.Equipment is { } equipment)
        {
            EquipStats.Add(s, hero.Equipment, world.Defs, equipment);
            EquipStats.Clamp(s, equipment);
        }

        var factor = 1f + s[StatKey.MaxHpPct];
        var max = Math.Max(1, DamageMath.Round(s[StatKey.MaxHp] * factor));
        if (hero.MaxHp > 0f && hero.MaxHpFactor > 0f && factor != hero.MaxHpFactor) hero.Hp *= factor / hero.MaxHpFactor;
        hero.MaxHpFactor = factor;
        hero.MaxHp = max;
        hero.Hp = MathF.Min(hero.Hp, max);
        HeroBlink.OnMaxChanged(hero, blinkMax, HeroBlink.MaxCharges(world));
        world.Combat.StatsDirty = false;
    }

    private static void Add(StatBlock s, PassiveDef def, int level)
    {
        if (level <= 0) return;
        if (def.Levels is { } levels)
        {
            // 레벨별 효과(6.20 ⑯): 앞 L 개의 합
            for (var i = 0; i < Math.Min(level, levels.Count); i++)
                foreach (var (k, v) in levels[i]) s[k] += v;
            return;
        }
        foreach (var (k, v) in def.PerLevel) s[k] += v * level;
    }

    /// <summary>최종 이동 속도 = move_speed × (1 + move_pct)(기획 4.1).</summary>
    public static float MoveSpeed(Hero hero) => hero.Stats[StatKey.MoveSpeed] * (1f + hero.Stats[StatKey.MovePct]);

    /// <summary>최종 획득 반경 = pickup_radius × (1 + pickup_pct).</summary>
    public static float PickupRadius(Hero hero) => hero.Stats[StatKey.PickupRadius] * (1f + hero.Stats[StatKey.PickupPct]);
}

/// <summary>
/// 물약 칸(v0.9.1 V9, 6.19 — v0.8 의 허리띠를 바꿈): 넣기·가득 판정·마시기(<see cref="Drink"/>)·서서히 회복 한 틱(<see cref="StepHeal"/>).
/// 저절로 마시기는 없앴다(사용자 결정 V9 — <c>Q</c> 로 원할 때). 칸 = <c>rules.hero.potions.Capacity(Hero.PotionSlotsLevel)</c>, 규칙이 없으면 0.
/// </summary>
public static class HeroPotions
{
    /// <summary>지금 칸 수(규칙이 없으면 0).</summary>
    public static int Capacity(World world) => world.Defs.Rules.Hero?.Potions?.Capacity(world.Hero.PotionSlotsLevel) ?? 0;

    /// <summary>물약 칸에 <paramref name="count"/> 개 넣는다. 칸을 넘는 만큼은 빠진다(마시지 않는다 — 기획 v0.9.1 11.4 "집는 순간").</summary>
    public static void Add(World world, int count)
    {
        var hero = world.Hero;
        hero.Potions = Math.Min(Capacity(world), hero.Potions + Math.Max(0, count));
    }

    /// <summary>물약 칸이 찼나(체력 물약 줍기 — 가득이면 안 집힘).</summary>
    public static bool Full(World world) => world.Hero.Potions >= Capacity(world);

    /// <summary>
    /// <c>CommandButtons.Drink</c> 를 받은 틱(<c>HeroControl</c> 차례): 선택 대기 없음 ∧ 상인 창 닫힘 ∧ 회복 중 아님 ∧ 물약 &gt; 0 ∧ 체력 &lt; 최대면
    /// 물약 −1, <c>Hero.Heal</c> = 최대 체력 × <c>heal_pct</c> 를 <c>round(durations[빠른 물약 단계] × 60)</c> 틱에 걸쳐, <c>RunStats.PotionsDrunk</c> +1, <c>CueKind.PotionDrink</c>.
    /// 아니면 <c>CueKind.PotionDenied</c>(물약 그대로). 신호 대상은 둘 다 null(자리 = 주인공). 상인 창이 열린 동안의 <c>Drink</c> 는 판 틱이 돌지 않으므로 <c>ShopInput</c> 이 거절한다.
    /// </summary>
    public static bool Drink(World world)
    {
        var hero = world.Hero;
        var rules = world.Defs.Rules.Hero?.Potions;
        var waiting = world.AwaitingChoice != ChoiceKind.None
            || world.Progression is { } p && (p.PendingLevelUps > 0 || p.PendingChests > 0 || p.PendingRewards > 0);
        if (rules is null || waiting || world.Room?.Shop is { Open: true } || hero.Heal.Active || hero.Potions <= 0 || hero.Hp >= hero.MaxHp)
        {
            Denied(world);
            return false;
        }
        hero.Potions--;
        var ticks = (int)MathF.Round(rules.Duration(hero.PotionSpeedLevel) * World.TicksPerSecond, MidpointRounding.AwayFromZero);
        hero.Heal = HealOverTime.Start(hero.MaxHp * rules.HealPct, ticks);
        world.RunStats.PotionsDrunk++;
        world.Presentation.Cue(new CueEvent(world.Tick, CueKind.PotionDrink, hero.Pos, null));
        return true;
    }

    /// <summary>못 마심 신호(<c>CueKind.PotionDenied</c>, 대상 null).</summary>
    public static void Denied(World world) => world.Presentation.Cue(new CueEvent(world.Tick, CueKind.PotionDenied, world.Hero.Pos, null));

    /// <summary>서서히 회복 한 틱(<c>HeroControl</c> 차례 — 판 틱이 돌 때만이라 창·넘김 동안은 같이 멈춘다). 최대 체력에서 자른다. 피해로 끊기지 않는다.</summary>
    public static void StepHeal(World world)
    {
        var hero = world.Hero;
        if (!hero.Heal.Active) return;
        var (amount, next) = hero.Heal.Step();
        hero.Heal = next;
        hero.Hp = MathF.Min(hero.MaxHp, hero.Hp + amount);
    }
}
