using DiabloDefenceRemastered.Core.Sim.Progression;

namespace DiabloDefenceRemastered.Core.Sim.Heroes;

/// <summary>
/// 주인공 만들기(WP3 <c>HeroSetup</c>, 설계서 6.16): <c>World.HeroClass</c> 만 읽는다 — 능력치(<see cref="HeroStats"/>), 시작 무기(Lv1, 칸 0 부터), 특성(슬롯 밖, 1레벨),
/// 열린 칸 = <c>RunSlots.OpenAt(World.Room?.Plan.Act ?? 1, …)</c>(v0.8.1 — <c>rules.rooms</c> 가 없는 시험 월드는 <c>rules.level_up</c> 의 시작 칸 수), 체력 가득, 재추첨 = <c>rules.level_up.rerolls</c>.
/// <c>RunStats</c> 는 건드리지 않는다(v0.7.1). 특성의 <c>on_acquire</c> 는 판 시작에 한 번 — 방 월드는 액트 1 의 첫 방에서만(짐이 없는 첫 월드).
/// </summary>
public static class HeroSetup
{
    public static void Create(World world)
    {
        var hero = world.Hero;
        var defs = world.Defs;
        var levelUp = defs.Rules.LevelUp ?? new LevelUpRulesDef();
        var (weapons, passives) = defs.Rules.Rooms is { } rooms
            ? RunSlots.OpenAt(world.Room?.Plan.Act ?? 1, levelUp, rooms)
            : (Math.Min(WeaponSlots.SlotCount, levelUp.WeaponSlots), Math.Min(PassiveSlots.SlotCount, levelUp.PassiveSlots));
        hero.WeaponSlotsOpen = weapons;
        hero.PassiveSlotsOpen = passives;

        var state = world.Combat;
        state.Level = 1;
        state.Xp = 0;
        state.RerollsLeft = levelUp.Rerolls;

        if (world.HeroClass is { } cls)
        {
            var slot = 0;
            foreach (var w in cls.StartWeapons)
            {
                if (slot >= WeaponSlots.SlotCount) break;
                var def = w.Def;
                hero.Weapons[slot++] = new WeaponSlot { Def = def, Level = 1, AcquiredTick = world.Tick, MaxedAtTick = def.MaxLevel <= 1 ? world.Tick : -1 };
            }
        }
        hero.MaxHp = 0f;
        HeroStats.Recompute(world);
        hero.Hp = hero.MaxHp;
        if (world.HeroClass is { } c)
            foreach (var t in c.Traits)
                if (t.Def.OnAcquire is { } on)
                {
                    hero.Hp = MathF.Min(hero.MaxHp, hero.Hp + on.Heal);
                    if (world.Run is null || world.Run.Rooms.Count == 0 && world.Run.Carry is null) world.RunStats.Gold += on.Gold;
                }
        hero.MoveSpeed = HeroStats.MoveSpeed(hero);
    }
}

/// <summary>
/// 개발 옵션의 주인공 쪽(WP3 <see cref="IRunSetup"/>, 9절·기획 15절): <c>--level</c>(대기 없이 그 레벨), <c>--weapons</c>(칸 0 부터 그 무기·레벨로 바꿈 — 고를 수 없는 무기는 예외),
/// <c>--passives</c>(칸 0 부터, 축복이면 축복 칸 — 특성은 예외). <c>--god</c> 은 <c>DamageResolve</c> 가 <c>World.Options.God</c> 로 본다.
/// 열린 칸보다 많으면 예외(옵션 오류는 WP7 이 먼저 같은 함수 <c>RunSlots.OpenAt</c> 로 막는다 — 여기서 조용히 자르지 않음).
/// </summary>
public sealed class HeroRunSetup : IRunSetup
{
    public void Apply(World world, RunOptions options)
    {
        var hero = world.Hero;
        var state = world.Combat;
        if (options.Level is { } level && level > 1)
        {
            state.Level = level;
            state.Xp = 0;
        }
        if (options.Weapons.Count > 0)
        {
            if (options.Weapons.Count > hero.WeaponSlotsOpen)
                throw new ArgumentException($"--weapons {options.Weapons.Count}개 — 열린 무기 칸 {hero.WeaponSlotsOpen}개를 넘는다");
            for (var i = 0; i < WeaponSlots.SlotCount; i++) hero.Weapons[i] = new WeaponSlot { MaxedAtTick = -1 };
            for (var i = 0; i < options.Weapons.Count; i++)
            {
                var (id, lv) = options.Weapons[i];
                var def = world.Defs.Weapons.Get(id);
                if (!Build.ClassOk(world, def.Classes)) throw new ArgumentException($"--weapons '{id}' — 직업 '{world.HeroClass?.Id}' 가 고를 수 없는 무기");
                var level2 = def.BaseWeapon is null ? Math.Clamp(lv, 1, def.MaxLevel) : def.BaseWeapon.Def.MaxLevel;
                hero.Weapons[i] = new WeaponSlot
                {
                    Def = def, Level = level2, Evolved = def.BaseWeapon is not null, AcquiredTick = world.Tick,
                    MaxedAtTick = def.BaseWeapon is not null || level2 >= def.MaxLevel ? world.Tick : -1,
                };
            }
        }
        if (options.Passives.Count > 0)
        {
            var p = 0;
            var b = 0;
            for (var i = 0; i < PassiveSlots.SlotCount; i++) hero.Passives[i] = default;
            foreach (var (id, lv) in options.Passives)
            {
                var def = world.Defs.Passives.Get(id);
                if (def.Trait) throw new ArgumentException($"--passives '{id}' — 특성은 직업으로만 붙는다");
                if (!Build.ClassOk(world, def.Classes)) throw new ArgumentException($"--passives '{id}' — 직업 '{world.HeroClass?.Id}' 가 고를 수 없는 패시브");
                var slot = new PassiveSlot { Def = def, Level = Math.Clamp(lv, 1, Math.Max(1, def.MaxLevel)), AcquiredTick = world.Tick };
                if (def.Blessing)
                {
                    if (b >= BlessingSlots.SlotCount) throw new ArgumentException("--passives — 축복 칸 8개를 넘는다");
                    hero.Blessings[b++] = slot;
                }
                else
                {
                    if (p >= hero.PassiveSlotsOpen) throw new ArgumentException($"--passives — 열린 패시브 칸 {hero.PassiveSlotsOpen}개를 넘는다");
                    hero.Passives[p++] = slot;
                }
            }
        }
        if (options.Level is not null || options.Weapons.Count > 0 || options.Passives.Count > 0)
        {
            HeroStats.Recompute(world);
            hero.Hp = hero.MaxHp;
        }
    }
}
