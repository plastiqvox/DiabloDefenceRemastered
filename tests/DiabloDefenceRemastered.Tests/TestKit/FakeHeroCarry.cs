namespace DiabloDefenceRemastered.Tests.TestKit;

/// <summary>
/// 레벨·체력·칸만 왕복하는 가짜 <see cref="IHeroCarry"/>(v0.7.1 #23-형). 레벨은 <c>World.Progression</c> 이 있으면 그것, 없으면 <see cref="Level"/>(시험이 정함).
/// 칸은 칸 번호 순서 그대로(빈 칸 = 빈 ID)이고 <c>Restore</c> 는 같은 번호 칸에 놓는다(3.6).
/// </summary>
public sealed class FakeHeroCarry : IHeroCarry
{
    public int Level { get; set; } = 1;
    public int Captures { get; private set; }
    public int Restores { get; private set; }

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
        return new HeroCarry(world.Progression?.Level ?? Level, 0, 0, hero.Hp, weapons, passives, blessings);
    }

    public void Restore(World world, HeroCarry carry)
    {
        Restores++;
        Level = carry.Level;
        var hero = world.Hero;
        hero.Hp = carry.Hp;
        hero.MaxHp = MathF.Max(hero.MaxHp, carry.Hp);
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
}
