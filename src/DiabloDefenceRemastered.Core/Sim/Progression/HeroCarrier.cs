using DiabloDefenceRemastered.Core.Sim.Heroes;

namespace DiabloDefenceRemastered.Core.Sim.Progression;

/// <summary>
/// 방 사이 빌드 넘기기(WP3 <see cref="IHeroCarry"/>, 설계서 3.6 결정 1 표 — v0.8 짐 필드 전부). 칸은 <b>칸 번호 그대로</b>(빈 칸 = 빈 ID, <c>Restore</c> 가 같은 칸 — v0.7.1).
/// 넘기는 것: 레벨·경험치·재추첨 남은 수·체력·무기(ID·레벨·진화)·패시브·축복·열린 칸 수·허리띠·<c>Mods</c>(0 아닌 키, <see cref="StatKey"/> 순)·<c>Shrines</c>(서수 순)·
/// 시간 효과(<see cref="CombatState.SurgeEffectId"/> 남은 틱·값). 넘기지 않는 것: 무기 쿨다운·패턴 상태·피격 무적·허리띠 간격(새 방에 들어오면 모두 준비).
/// 대기(레벨업·상자·보상)가 남은 월드의 <see cref="Capture"/> 는 예외. 이 객체에는 상태가 없다(11.2 — 이어 하기는 새 모듈).
/// </summary>
public sealed class HeroCarrier : IHeroCarry
{
    public HeroCarry Capture(World world)
    {
        var s = world.Combat;
        if (s.PendingLevelUps > 0 || s.Chests.Count > 0 || s.Rewards.Count > 0)
            throw new InvalidOperationException($"대기가 남은 채 방을 나가려 한다(레벨업 {s.PendingLevelUps}, 상자 {s.Chests.Count}, 보상 {s.Rewards.Count})");
        var hero = world.Hero;
        var weapons = new CarriedSlot[WeaponSlots.SlotCount];
        for (var i = 0; i < weapons.Length; i++)
            weapons[i] = new CarriedSlot(hero.Weapons[i].Def?.Id ?? "", hero.Weapons[i].Def is null ? 0 : hero.Weapons[i].Level, hero.Weapons[i].Evolved);
        var passives = new CarriedSlot[PassiveSlots.SlotCount];
        for (var i = 0; i < passives.Length; i++)
            passives[i] = new CarriedSlot(hero.Passives[i].Def?.Id ?? "", hero.Passives[i].Def is null ? 0 : hero.Passives[i].Level, false);
        var blessings = new CarriedSlot[BlessingSlots.SlotCount];
        for (var i = 0; i < blessings.Length; i++)
            blessings[i] = new CarriedSlot(hero.Blessings[i].Def?.Id ?? "", hero.Blessings[i].Def is null ? 0 : hero.Blessings[i].Level, false);
        var mods = new List<CarriedStat>();
        for (var i = 0; i < s.Mods.Length; i++)
            if (s.Mods[i] != 0f) mods.Add(new CarriedStat((StatKey)i, s.Mods[i]));
        var shrines = new List<string>(s.Shrines);
        shrines.Sort(StringComparer.Ordinal);
        var effects = new List<CarriedEffect>();
        if (s.SurgeTicks > 0) effects.Add(new CarriedEffect(CombatState.SurgeEffectId, s.SurgeTicks, s.SurgePct));
        return new HeroCarry
        {
            Level = s.Level,
            Xp = s.Xp,
            RerollsLeft = s.RerollsLeft,
            Hp = hero.Hp,
            Weapons = weapons,
            Passives = passives,
            Blessings = blessings,
            WeaponSlotsOpen = hero.WeaponSlotsOpen,
            PassiveSlotsOpen = hero.PassiveSlotsOpen,
            // v0.9 WP1g: 만렙 순번·장비·지갑·물약 칸(장비·물약 효과 본문은 WP3)
            MaxedAtTick = MaxedOrder(hero),
            Equipment = Equipped(hero),
            Gold = hero.Gold,
            Potions = hero.Potions,
            PotionSlotsLevel = hero.PotionSlotsLevel,
            PotionSpeedLevel = hero.PotionSpeedLevel,
            Mods = mods,
            Shrines = shrines,
            Effects = effects,
        };
    }

    public void Restore(World world, HeroCarry carry)
    {
        var reason = Validate(carry, world.Defs);
        if (reason is not null) throw new InvalidOperationException($"짐을 풀 수 없다: {reason}");
        var hero = world.Hero;
        var s = world.Combat;
        var defs = world.Defs;
        s.Level = carry.Level;
        s.Xp = carry.Xp;
        s.RerollsLeft = carry.RerollsLeft;
        s.PendingLevelUps = 0;
        s.Chests.Clear();
        s.Rewards.Clear();
        s.LevelOffer = s.RewardOffer = null;
        hero.WeaponSlotsOpen = carry.WeaponSlotsOpen;
        hero.PassiveSlotsOpen = carry.PassiveSlotsOpen;
        hero.Potions = carry.Potions;
        hero.PotionSlotsLevel = carry.PotionSlotsLevel;
        hero.PotionSpeedLevel = carry.PotionSpeedLevel;
        hero.Gold = carry.Gold;
        Array.Clear(hero.Equipment);
        foreach (var e in carry.Equipment) hero.Equipment[(int)e.Slot] = e.Item;
        for (var i = 0; i < WeaponSlots.SlotCount; i++)
        {
            var c = i < carry.Weapons.Count ? carry.Weapons[i] : default;
            if (string.IsNullOrEmpty(c.Id)) { hero.Weapons[i] = new WeaponSlot { MaxedAtTick = -1 }; continue; }
            var def = defs.Weapons.Get(c.Id);
            var max = c.Evolved || def.BaseWeapon is not null || c.Level >= def.MaxLevel;
            // v0.9 WP1g: 짐의 만렙 순번(없으면 v0.8 처럼 만렙 무기 0) — 방을 넘어도 "먼저 만렙이 된 무기" 순서가 남는다(같으면 칸 번호)
            var rank = i < carry.MaxedAtTick.Count ? carry.MaxedAtTick[i] : -1;
            hero.Weapons[i] = new WeaponSlot { Def = def, Level = c.Level, Evolved = c.Evolved, AcquiredTick = 0, MaxedAtTick = max ? Math.Max(0, rank) : -1 };
        }
        for (var i = 0; i < PassiveSlots.SlotCount; i++)
        {
            var c = i < carry.Passives.Count ? carry.Passives[i] : default;
            hero.Passives[i] = string.IsNullOrEmpty(c.Id) ? default : new PassiveSlot { Def = defs.Passives.Get(c.Id), Level = c.Level };
        }
        for (var i = 0; i < BlessingSlots.SlotCount; i++)
        {
            var c = i < carry.Blessings.Count ? carry.Blessings[i] : default;
            hero.Blessings[i] = string.IsNullOrEmpty(c.Id) ? default : new PassiveSlot { Def = defs.Passives.Get(c.Id), Level = c.Level };
        }
        Array.Clear(s.Mods);
        foreach (var m in carry.Mods) s.Mods[(int)m.Stat] += m.Add;
        s.Shrines.Clear();
        s.Shrines.AddRange(carry.Shrines);
        s.SurgeTicks = 0;
        s.SurgePct = 0f;
        foreach (var e in carry.Effects)
            if (e.Id == CombatState.SurgeEffectId) { s.SurgeTicks = e.TicksLeft; s.SurgePct = e.Value; }

        // 능력치 → 체력(비율 기억을 짐의 덧값으로 맞춘 뒤 — 지금 체력을 비율로 다시 곱하지 않게)
        hero.MaxHp = 0f;
        HeroStats.Recompute(world);
        hero.Hp = MathF.Min(carry.Hp, hero.MaxHp);
        hero.MoveSpeed = HeroStats.MoveSpeed(hero);
    }

    /// <summary>이어 하기 전 확인(v0.8.1): 모르는 정의 ID·시간 효과 ID, 진화 짝 어긋남, 칸 수·레벨 범위.</summary>
    public string? Validate(HeroCarry carry, GameDefs defs)
    {
        if (carry.Level < 1) return $"레벨 {carry.Level}";
        if (carry.WeaponSlotsOpen is < 0 or > WeaponSlots.SlotCount) return $"열린 무기 칸 {carry.WeaponSlotsOpen}";
        if (carry.PassiveSlotsOpen is < 0 or > PassiveSlots.SlotCount) return $"열린 패시브 칸 {carry.PassiveSlotsOpen}";
        if (carry.Weapons.Count > WeaponSlots.SlotCount || carry.Passives.Count > PassiveSlots.SlotCount || carry.Blessings.Count > BlessingSlots.SlotCount)
            return "칸 수가 그릇보다 많다";
        foreach (var w in carry.Weapons)
        {
            if (string.IsNullOrEmpty(w.Id)) continue;
            if (!defs.Weapons.TryGet(w.Id, out var def)) return $"모르는 무기 '{w.Id}'";
            var isResult = false;
            foreach (var e in defs.Evolutions.All) isResult |= e.Result.Id == w.Id;
            if (w.Evolved != isResult) return $"무기 '{w.Id}' 의 진화 표시({w.Evolved})가 진화 정의와 다르다";
            if (!w.Evolved && (w.Level < 1 || w.Level > def.MaxLevel)) return $"무기 '{w.Id}' 레벨 {w.Level}";
        }
        foreach (var p in carry.Passives)
        {
            if (string.IsNullOrEmpty(p.Id)) continue;
            if (!defs.Passives.TryGet(p.Id, out var def)) return $"모르는 패시브 '{p.Id}'";
            if (def.Trait || def.Blessing) return $"패시브 칸에 특성·축복 '{p.Id}'";
            if (p.Level < 1 || p.Level > def.MaxLevel) return $"패시브 '{p.Id}' 레벨 {p.Level}";
        }
        foreach (var b in carry.Blessings)
        {
            if (string.IsNullOrEmpty(b.Id)) continue;
            if (!defs.Passives.TryGet(b.Id, out var def) || !def.Blessing) return $"축복이 아닌 '{b.Id}'";
        }
        foreach (var s in carry.Shrines)
            if (!defs.Rewards.TryGet(s, out var r) || r.Kind != RewardKind.Shrine) return $"모르는 성소 '{s}'";
        foreach (var e in carry.Effects)
        {
            if (e.Id != CombatState.SurgeEffectId) return $"모르는 시간 효과 '{e.Id}'";
            if (e.TicksLeft < 0) return $"시간 효과 '{e.Id}' 남은 틱 {e.TicksLeft}";
        }
        return null;
    }

    /// <summary>무기 칸별 만렙 순번(v0.9 — 만렙이 된 틱 순, 같으면 칸 번호. 아직이면 −1).</summary>
    private static List<int> MaxedOrder(Hero hero)
    {
        var order = new List<int>(WeaponSlots.SlotCount);
        var maxed = new List<(int Tick, int Slot)>();
        for (var i = 0; i < WeaponSlots.SlotCount; i++)
        {
            order.Add(-1);
            if (!hero.Weapons[i].IsEmpty && hero.Weapons[i].MaxedAtTick >= 0) maxed.Add((hero.Weapons[i].MaxedAtTick, i));
        }
        maxed.Sort();
        for (var r = 0; r < maxed.Count; r++) order[maxed[r].Slot] = r;
        return order;
    }

    /// <summary>끼운 장비(칸 순, 빈 칸 빼고).</summary>
    private static List<CarriedEquip> Equipped(Hero hero)
    {
        var list = new List<CarriedEquip>();
        for (var i = 0; i < EquipSlots.Count; i++)
            if (hero.Equipment[i] is { } item) list.Add(new CarriedEquip((EquipSlot)i, item));
        return list;
    }
}
