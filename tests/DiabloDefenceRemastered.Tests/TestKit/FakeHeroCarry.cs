namespace DiabloDefenceRemastered.Tests.TestKit;

/// <summary>
/// 레벨·체력·칸·열린 칸 수·물약 칸·지갑·장비(v0.9)만 왕복하는 가짜 <see cref="IHeroCarry"/>(v0.7.1 #23-형 → v0.8 WP1f). 레벨은 <c>World.Progression</c> 이 있으면 그것, 없으면 <see cref="Level"/>(시험이 정함).
/// 칸은 칸 번호 순서 그대로(빈 칸 = 빈 ID)이고 <c>Restore</c> 는 같은 번호 칸에 놓는다(3.6). <c>Mods</c>·<c>Shrines</c>·<c>Effects</c>·<c>MaxedAtTick</c>(v0.9) 는 시험이 <see cref="Extra"/> 로 붙인 값을 그대로 넘긴다
/// (판 수명 상태를 이 인스턴스에 두지 않는다 — 11.2: <see cref="Extra"/> 는 <c>Capture</c> 마다 짐에 실려 가고 <c>Restore</c> 가 짐에서 다시 읽는다).
/// <see cref="Validate"/> 는 <see cref="ValidateReason"/>(기본 null = 맞음).
/// </summary>
public sealed class FakeHeroCarry : IHeroCarry
{
    public int Level { get; set; } = 1;
    public int Captures { get; private set; }
    public int Restores { get; private set; }
    /// <summary>다음 <c>Capture</c> 에 실을 짐 덧값(Mods·Shrines·Effects). <c>Restore</c> 가 받은 짐의 값으로 바꾼다.</summary>
    public HeroCarry? Extra { get; set; }
    /// <summary><see cref="Validate"/> 가 돌려줄 이유(이어 하기 불일치 시험).</summary>
    public string? ValidateReason { get; set; }
    /// <summary>마지막 <c>Restore</c> 가 받은 짐.</summary>
    public HeroCarry? LastRestored { get; private set; }

    public HeroCarry Capture(World world)
    {
        Captures++;
        var hero = world.Hero;
        var weapons = new List<CarriedSlot>(WeaponSlots.SlotCount);
        for (var i = 0; i < WeaponSlots.SlotCount; i++)
            weapons.Add(new CarriedSlot(hero.Weapons[i].Def?.Id ?? "", hero.Weapons[i].Level, hero.Weapons[i].Evolved));
        var passives = new List<CarriedSlot>(PassiveSlots.SlotCount);
        for (var i = 0; i < PassiveSlots.SlotCount; i++)
            passives.Add(new CarriedSlot(hero.Passives[i].Def?.Id ?? "", hero.Passives[i].Level, false));
        var blessings = new List<CarriedSlot>(BlessingSlots.SlotCount);
        for (var i = 0; i < BlessingSlots.SlotCount; i++)
            blessings.Add(new CarriedSlot(hero.Blessings[i].Def?.Id ?? "", hero.Blessings[i].Level, false));
        return new HeroCarry
        {
            Level = world.Progression?.Level ?? Level, Xp = 0, RerollsLeft = 0, Hp = hero.Hp,
            Weapons = weapons, Passives = passives, Blessings = blessings,
            WeaponSlotsOpen = hero.WeaponSlotsOpen, PassiveSlotsOpen = hero.PassiveSlotsOpen,
            Potions = hero.Potions, PotionSlotsLevel = hero.PotionSlotsLevel, PotionSpeedLevel = hero.PotionSpeedLevel, Gold = hero.Gold,
            Equipment = [.. Enumerable.Range(0, EquipSlots.Count).Where(i => hero.Equipment[i] is not null).Select(i => new CarriedEquip((EquipSlot)i, hero.Equipment[i]!))],
            MaxedAtTick = Extra?.MaxedAtTick ?? [],
            Mods = Extra?.Mods ?? [], Shrines = Extra?.Shrines ?? [], Effects = Extra?.Effects ?? [],
        };
    }

    public void Restore(World world, HeroCarry carry)
    {
        Restores++;
        LastRestored = carry;
        Level = carry.Level;
        Extra = carry;
        var hero = world.Hero;
        hero.Hp = carry.Hp;
        hero.MaxHp = MathF.Max(hero.MaxHp, carry.Hp);
        hero.WeaponSlotsOpen = carry.WeaponSlotsOpen;
        hero.PassiveSlotsOpen = carry.PassiveSlotsOpen;
        hero.Potions = carry.Potions;
        hero.PotionSlotsLevel = carry.PotionSlotsLevel;
        hero.PotionSpeedLevel = carry.PotionSpeedLevel;
        hero.Gold = carry.Gold;
        Array.Clear(hero.Equipment);
        foreach (var e in carry.Equipment) hero.Equipment[(int)e.Slot] = e.Item;
        for (var i = 0; i < carry.Weapons.Count && i < WeaponSlots.SlotCount; i++)
        {
            var s = carry.Weapons[i];
            hero.Weapons[i] = s.Id.Length == 0
                ? new WeaponSlot { MaxedAtTick = -1 }
                : new WeaponSlot { Def = world.Defs.Weapons.Get(s.Id), Level = s.Level, Evolved = s.Evolved, MaxedAtTick = -1 };
        }
        for (var i = 0; i < carry.Passives.Count && i < PassiveSlots.SlotCount; i++)
        {
            var s = carry.Passives[i];
            hero.Passives[i] = s.Id.Length == 0 ? default : new PassiveSlot { Def = world.Defs.Passives.Get(s.Id), Level = s.Level };
        }
        for (var i = 0; i < carry.Blessings.Count && i < BlessingSlots.SlotCount; i++)
        {
            var s = carry.Blessings[i];
            hero.Blessings[i] = s.Id.Length == 0 ? default : new PassiveSlot { Def = world.Defs.Passives.Get(s.Id), Level = s.Level };
        }
    }

    public string? Validate(HeroCarry carry, GameDefs defs) => ValidateReason;
}
