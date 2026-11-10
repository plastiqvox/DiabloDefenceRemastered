using DiabloDefenceRemastered.Core.Sim.Combat;

namespace DiabloDefenceRemastered.Core.Sim.Heroes;

/// <summary>
/// 주인공 능력치 합 한 곳(기획 4.1 적용 규칙, 설계서 6.10·6.16): <c>rules.hero.stats</c> → 직업 <c>stats</c> 가 키별로 덮어씀 → 특성(1레벨)·패시브·축복 <c>per_level</c> × 레벨 → 짐 <c>Mods</c>(<c>단련</c>·성소 대가)를 더함.
/// 최대 체력 = round(<c>max_hp</c> × (1 + <c>max_hp_pct</c>)). 비율이 바뀌면 지금 체력도 같은 비율로(v0.8 매혹의 성소), 더하기만 바뀌면 지금 체력은 그대로(회복은 <c>on_acquire</c>·채움 카드가 따로).
/// </summary>
public static class HeroStats
{
    public static void Recompute(World world)
    {
        var hero = world.Hero;
        var s = hero.Stats;
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

        var factor = 1f + s[StatKey.MaxHpPct];
        var max = Math.Max(1, DamageMath.Round(s[StatKey.MaxHp] * factor));
        if (hero.MaxHp > 0f && hero.MaxHpFactor > 0f && factor != hero.MaxHpFactor) hero.Hp *= factor / hero.MaxHpFactor;
        hero.MaxHpFactor = factor;
        hero.MaxHp = max;
        hero.Hp = MathF.Min(hero.Hp, max);
        world.Combat.StatsDirty = false;
    }

    private static void Add(StatBlock s, PassiveDef def, int level)
    {
        if (level <= 0) return;
        foreach (var (k, v) in def.PerLevel) s[k] += v * level;
    }

    /// <summary>최종 이동 속도 = move_speed × (1 + move_pct)(기획 4.1).</summary>
    public static float MoveSpeed(Hero hero) => hero.Stats[StatKey.MoveSpeed] * (1f + hero.Stats[StatKey.MovePct]);

    /// <summary>최종 획득 반경 = pickup_radius × (1 + pickup_pct).</summary>
    public static float PickupRadius(Hero hero) => hero.Stats[StatKey.PickupRadius] * (1f + hero.Stats[StatKey.PickupPct]);
}

/// <summary>
/// 허리띠(v0.8, 6.18 — 기획 v0.8.1 11.4): 체력이 최대 × <c>threshold</c> 아래면 하나를 저절로 마신다(전부 회복, <c>cooldown</c> 초에 한 번 — 버튼 없음).
/// 피해를 받은 순간(<c>DamageResolve</c>)과 매 틱(<c>HeroControl</c> — 간격 때문에 못 마신 경우)에 본다. 넣을 때 차 있으면 그 자리에서 마신다(<see cref="Add"/>).
/// </summary>
public static class HeroBelt
{
    public static void Check(World world)
    {
        var hero = world.Hero;
        if (world.Defs.Rules.Hero?.Belt is not { } belt || hero.Belt <= 0 || hero.Dead) return;
        if (hero.Hp >= hero.MaxHp * belt.Threshold || world.Tick < hero.BeltReadyTick) return;
        hero.Belt--;
        Drink(world);
        hero.BeltReadyTick = world.Tick + Math.Max(1, MathX.SecondsToTicks(belt.Cooldown, World.Dt));
    }

    /// <summary>허리띠에 <paramref name="count"/> 개 넣는다. 칸을 넘는 만큼은 그 자리에서 마신다.</summary>
    public static void Add(World world, int count)
    {
        var hero = world.Hero;
        var slots = world.Defs.Rules.Hero?.Belt?.Slots ?? 0;
        for (var i = 0; i < count; i++)
        {
            if (hero.Belt < slots) hero.Belt++;
            else Drink(world);
        }
        Check(world);
    }

    /// <summary>허리띠가 찼나(물약 보상 거르기).</summary>
    public static bool Full(World world) => world.Hero.Belt >= (world.Defs.Rules.Hero?.Belt?.Slots ?? 0);

    private static void Drink(World world)
    {
        var hero = world.Hero;
        hero.Hp = hero.MaxHp;
        world.Presentation.Cue(new CueEvent(world.Tick, CueKind.BeltPotion, hero.Pos, null));
    }
}
