using System.Text.Json;
using DiabloDefenceRemastered.Core.Sim.Progression;
using DiabloDefenceRemastered.Core.Sim.Weapons;
using DiabloDefenceRemastered.Tests.Combat;

namespace DiabloDefenceRemastered.Tests.Progression;

/// <summary>
/// 10.1 <c>Progression</c> 의 짐(v0.7 왕복·v0.7.1 칸 번호·대기 예외·v0.8 새 필드·v0.8.1 <c>Validate</c>)과
/// <b>실제 <see cref="CombatModule"/></b> 으로 돈 <see cref="RunSession"/>(방 둘을 지난 빌드·칸별 피해 누적·계단에서 끊고 이은 판 = 끊지 않은 판).
/// 적 생성·DeathHooks 는 WP4 자리라 시험 시스템(<see cref="Dummy"/>·<see cref="Deaths"/>)이 흉내 낸다.
/// </summary>
public class CarryTests
{
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, RunSnapshot.JsonOptions);

    private static GameDefs EvoDefs()
    {
        var basic = CombatKit.Defs();
        var mm = basic.Weapons.Get(CombatKit.Mm);
        var power = basic.Passives.Get(CombatKit.Power);
        var storm = new WeaponDef { Id = "t_storm", Name = "비전 폭풍", BaseWeapon = DefRef<WeaponDef>.To(mm) };
        return new GameDefs
        {
            Rules = basic.Rules, Sprites = basic.Sprites, SpriteSets = basic.SpriteSets, Projectiles = basic.Projectiles,
            Weapons = new DefTable<WeaponDef>([.. basic.Weapons.All, storm]), Passives = basic.Passives, HeroClasses = basic.HeroClasses,
            Tilesets = basic.Tilesets, Maps = basic.Maps, Stages = basic.Stages, Enemies = basic.Enemies, Rewards = basic.Rewards, Pickups = basic.Pickups,
            Evolutions = new DefTable<EvolutionDef>([new EvolutionDef { Weapon = DefRef<WeaponDef>.To(mm), Passive = DefRef<PassiveDef>.To(power), Result = DefRef<WeaponDef>.To(storm) }]),
        };
    }

    /// <summary>칸 0·2 에 무기(2 = 진화), 패시브 칸 1, 성소·단련 덧값·시간 효과·허리띠를 가진 빌드.</summary>
    private static World Built(GameDefs defs)
    {
        var t = CombatKit.World(defs);
        var w = t.World;
        CombatKit.ClearWeapons(w);
        CombatKit.SetWeapon(w, 0, CombatKit.Fb, 3);
        w.Hero.Weapons[2] = new WeaponSlot { Def = w.Defs.Weapons.Get("t_storm"), Level = 8, Evolved = true, MaxedAtTick = 0 };
        w.Hero.Passives[1] = new PassiveSlot { Def = w.Defs.Passives.Get(CombatKit.Power), Level = 2 };
        w.Combat.Level = 6;
        w.Combat.Xp = 7;
        w.Combat.RerollsLeft = 1;
        w.Combat.Mods[(int)StatKey.MaxHp] = 10f;
        w.Combat.Mods[(int)StatKey.Armor] = 2f;
        w.Combat.Shrines.Add(TestDefs.RewardShrineId);
        w.Combat.StatsDirty = true;
        w.Hero.Belt = 1;
        w.Hero.WeaponSlotsOpen = 5;
        w.Hero.PassiveSlotsOpen = 5;
        t.Step();
        w.Hero.Hp = 61f;
        w.Combat.SurgeTicks = 90;                                   // 방 없는 월드는 시간 효과가 흐르므로 틱 뒤에
        w.Combat.SurgePct = 0.5f;
        return w;
    }

    [Fact]
    public void Carry_RoundTrip_SameSlotNumbers_Evolved_AllFields()
    {
        var defs = EvoDefs();
        var a = Built(defs);
        var carry = new HeroCarrier().Capture(a);
        Assert.Equal(WeaponSlots.SlotCount, carry.Weapons.Count);
        Assert.Equal(new CarriedSlot(CombatKit.Fb, 3, false), carry.Weapons[0]);
        Assert.Equal(new CarriedSlot("", 0, false), carry.Weapons[1]);
        Assert.Equal(new CarriedSlot("t_storm", 8, true), carry.Weapons[2]);
        Assert.Equal(new CarriedSlot(CombatKit.Power, 2, false), carry.Passives[1]);
        Assert.Equal([new CarriedStat(StatKey.MaxHp, 10f), new CarriedStat(StatKey.Armor, 2f)], carry.Mods.OrderBy(m => m.Stat));
        Assert.Equal([TestDefs.RewardShrineId], carry.Shrines);
        Assert.Equal([new CarriedEffect(CombatState.SurgeEffectId, 90, 0.5f)], carry.Effects);
        Assert.Null(new HeroCarrier().Validate(carry, defs));

        // JSON 왕복 뒤 새 월드(다른 주인공 시작 무기)에 풀기
        var back = JsonSerializer.Deserialize<HeroCarry>(Json(carry), RunSnapshot.JsonOptions)!;
        var t2 = CombatKit.World(defs, seed: 9);
        var b = t2.World;
        new HeroCarrier().Restore(b, back);
        Assert.Equal(CombatKit.Fb, b.Hero.Weapons[0].Def!.Id);
        Assert.True(b.Hero.Weapons[1].IsEmpty);
        Assert.Equal("t_storm", b.Hero.Weapons[2].Def!.Id);
        Assert.True(b.Hero.Weapons[2].Evolved);
        Assert.True(b.Hero.Passives[0].IsEmpty);
        Assert.Equal(2, b.Hero.Passives[1].Level);
        Assert.Equal((6, 7, 1), (b.Combat.Level, b.Combat.Xp, b.Combat.RerollsLeft));
        Assert.Equal((5, 5, 1), (b.Hero.WeaponSlotsOpen, b.Hero.PassiveSlotsOpen, b.Hero.Belt));
        Assert.Equal(61f, b.Hero.Hp);
        Assert.Equal(a.Hero.MaxHp, b.Hero.MaxHp);
        for (var i = 0; i < StatKeys.Count; i++) Assert.Equal(a.Hero.Stats[(StatKey)i], b.Hero.Stats[(StatKey)i]);
        Assert.False(b.Progression!.RewardAvailable(b.Defs.Rewards.Get(TestDefs.RewardShrineId)));   // 성소 한 판에 한 번
        Assert.Equal((90, 0.5f), (b.Combat.SurgeTicks, b.Combat.SurgePct));
        Assert.Equal(Json(carry), Json(new HeroCarrier().Capture(b)));                              // 다시 담은 짐 = 같은 바이트
    }

    [Fact]
    public void Capture_WithPendingChoice_Throws()
    {
        var t = CombatKit.World();
        var w = t.World;
        ((ProgressionService)w.Progression!).AddXp(5);
        Assert.Throws<InvalidOperationException>(() => new HeroCarrier().Capture(w));
        w.Progression.Skip();
        new HeroCarrier().Capture(w);
        w.Combat.Chests.Add(1);
        Assert.Throws<InvalidOperationException>(() => new HeroCarrier().Capture(w));
        w.Combat.Chests.Clear();

        CombatKit.Drop(w, w.Defs.Rewards.Get(TestDefs.RewardTomeId).Object.Id, t.Hero.Pos, reward: w.Defs.Rewards.Get(TestDefs.RewardTomeId));
        t.Step();
        Assert.Equal(1, w.Progression.PendingRewards);
        Assert.Throws<InvalidOperationException>(() => new HeroCarrier().Capture(w));
    }

    [Fact]
    public void Validate_Reasons_AndRestoreRefuses()
    {
        var defs = EvoDefs();
        var good = new HeroCarrier().Capture(Built(defs));
        var c = new HeroCarrier();
        string? Why(HeroCarry x) => c.Validate(x, defs);

        Assert.Contains("mana_surge2", Why(good with { Effects = [new CarriedEffect("mana_surge2", 10, 0.5f)] }));
        Assert.Contains("진화 표시", Why(good with { Weapons = [new CarriedSlot("t_storm", 8, false)] }));       // 진화 결과인데 표시 없음
        Assert.Contains("진화 표시", Why(good with { Weapons = [new CarriedSlot(CombatKit.Mm, 8, true)] }));    // 재료인데 진화 표시
        Assert.Contains("no_such", Why(good with { Weapons = [new CarriedSlot("no_such", 1, false)] }));
        Assert.Contains("레벨 9", Why(good with { Weapons = [new CarriedSlot(CombatKit.Mm, 9, false)] }));
        Assert.Contains("특성", Why(good with { Passives = [new CarriedSlot(CombatKit.Trait, 1, false)] }));
        Assert.Contains("성소", Why(good with { Shrines = ["no_shrine"] }));
        Assert.Contains("열린 무기 칸", Why(good with { WeaponSlotsOpen = 7 }));

        var w = CombatKit.World(defs).World;
        var before = w.Hero.Weapons[0].Def!.Id;
        Assert.Throws<InvalidOperationException>(() => c.Restore(w, good with { Effects = [new CarriedEffect("x", 1, 0f)] }));
        Assert.Equal(before, w.Hero.Weapons[0].Def!.Id);                                            // 확인이 먼저 — 반쯤 풀린 짐 없음
    }

    // ── 실제 모듈 + RunSession ───────────────────────────────────

    /// <summary>
    /// <see cref="RoomActs"/>(방·액트·보상 가중) + <see cref="CombatKit"/>(무기·패시브·직업·주인공 규칙·보석)을 한 벌로.
    /// 보상·보상 물체는 RoomActs 쪽(RoomFlow 규칙이 가리키는 같은 인스턴스), 보석·금화·물약은 CombatKit 쪽.
    /// </summary>
    private static GameDefs RunDefs()
    {
        var r = RoomActs.Defs();
        var c = CombatKit.Defs();
        return new GameDefs
        {
            Rules = c.Rules with { Rooms = r.Rules.Rooms },
            Sprites = r.Sprites, SpriteSets = r.SpriteSets, Projectiles = c.Projectiles,
            Weapons = c.Weapons, Passives = c.Passives, HeroClasses = c.HeroClasses,
            Tilesets = r.Tilesets, Maps = r.Maps, Stages = r.Stages, Enemies = r.Enemies, Rewards = r.Rewards,
            Pickups = new DefTable<PickupDef>([.. r.Pickups.All, .. c.Pickups.All.Where(p => p.Kind is not PickupKind.Reward and not PickupKind.Chest)]),
        };
    }

    /// <summary>방마다 생존 1초째 주인공 오른쪽 120px 에 서 있는 적 하나(감독 자리 — WP4 대신).</summary>
    private sealed class Dummy : ISimSystem
    {
        public void Tick(World world)
        {
            if (world.Room is not { Phase: RoomPhase.Survive } room || world.Tick - room.PhaseTick != 60) return;
            var def = world.Defs.Enemies.Get(TestDefs.EnemyId);
            var e = world.Enemies.Add();
            e.Def = def;
            e.DefIndex = world.Defs.Enemies.IndexOf(def.Id);
            e.Faction = Faction.Enemy;
            e.SpawnTick = world.Tick - 1;
            e.Life = LifeState.Active;
            e.Pos = e.PrevPos = world.Hero.Pos + new Vector2(120f, 0f);
            e.Radius = 12f;
            e.Hp = e.MaxHp = 40f;
            e.Intent = MoveIntent.SeekHero();
        }
    }

    /// <summary>DeathHooks 자리: 죽은 적 → <c>EnemyKilled</c>, 정예·보스 방은 생존 4초째에 처치(RunSuspendTests 의 AutoKill 과 같음).</summary>
    private sealed class Deaths : ISimSystem
    {
        private readonly FakeDeathHooks _hooks = new();

        public void Tick(World world)
        {
            _hooks.Tick(world);
            if (world.Room is not { Phase: RoomPhase.Survive } room || room.ClockTicks != 4 * 60) return;
            if (room.Plan.Kind == RoomKind.Elite)
                world.Events.Current.EnemyKilled.Add(new EnemyKilled(default, world.Defs.Enemies.IndexOf(RoomActs.MinibossId), Vector2.Zero, EnemyRank.Unique, KillCause.Damage, default));
            else if (room.Plan.Kind == RoomKind.Boss)
                world.Events.Current.BossDefeated.Add(new BossDefeated(default, world.Defs.Enemies.IndexOf(RoomActs.BossId), Vector2.Zero));
        }
    }

    /// <summary>실제 <see cref="CombatModule"/>(짐 = 실제 <see cref="HeroCarrier"/>) + 직선 내비·전수 공간 질의·시험 시스템. 판 수명 상태 없음 — 이어 하기는 새 모듈.</summary>
    private static ModuleContext Modules()
    {
        var m = new ModuleContext();
        m.AddWorldInstaller((w, b) =>
        {
            w.Presentation.Enabled = false;
            w.Nav = new StraightNav(w.Arena!.Iso);
            w.Spatial = new BruteForceSpatial(w);
            b.Add(SimPhase.Director, new Dummy());
            b.Add(SimPhase.DeathHooks, new Deaths());
        });
        new CombatModule().Register(m);
        return m;
    }

    private static RunSession Session(GameDefs defs, ulong seed) =>
        new(defs, Modules(), new RunStart(seed, defs.Stages.Get(RoomActs.Act1Id), null, RunOptions.Default, 0));

    /// <summary>
    /// 지금 방을 <paramref name="until"/> 단계까지. 선택 창은 첫 카드·상자 열기로 넘기고, 보상 단계의 보상 물체는 주인공 발밑으로 옮겨
    /// <b>실제로 줍게</b> 한다(보상 효과가 빌드에 들어감). 문 단계는 Exit 명령 0번.
    /// </summary>
    private static void PlayTo(RunSession s, RoomPhase until = RoomPhase.Exit)
    {
        var w = s.World;
        for (var i = 0; i < 60 * 600 && w.Room!.Phase != until && w.Room.Phase != RoomPhase.Exit && w.Outcome == RunOutcome.Running; i++)
        {
            switch (w.AwaitingChoice)
            {
                case ChoiceKind.LevelUp: w.Progression!.Choose(0); continue;
                case ChoiceKind.Chest: w.Progression!.OpenChest(); continue;
                case ChoiceKind.Reward: w.Progression!.ChooseReward(0); continue;
            }
            if (w.Room.Phase == RoomPhase.Reward)
                foreach (var p in w.Pickups.Items)
                    if (!p.Removed && p.GatesExit) p.Pos = p.PrevPos = w.Hero.Pos;
            var cmd = w.Room.Phase == RoomPhase.Doors ? new PlayerCommand(Vector2.Zero, CommandButtons.Exit) : PlayerCommand.None;
            StageStepper.AppTick(w, cmd);
        }
    }

    private static void ToBossRoom(RunSession s, RoomPhase until)
    {
        for (var room = 0; room < 5; room++)
        {
            PlayTo(s);
            s.Advance();
        }
        Assert.Equal(6, s.World.Room!.Plan.Number);
        PlayTo(s, until);
        Assert.Equal(until, s.World.Room.Phase);
    }

    private static void PlayToEnd(RunSession s)
    {
        for (var room = 0; room < 20; room++)
        {
            PlayTo(s);
            if (s.World.Outcome != RunOutcome.Running) break;
            s.Advance();
        }
        while (s.World.Outcome is RunOutcome.VictoryPending or RunOutcome.DefeatPending) StageStepper.AppTick(s.World, PlayerCommand.None);
    }

    [Fact]
    public void RunSession_TwoRooms_BuildCarried_DamagePerSlotAccumulates()
    {
        var defs = RunDefs();
        var s = Session(defs, 21);
        var w = s.World;
        Assert.Equal(4, w.Hero.WeaponSlotsOpen);                    // 액트 1 = RunSlots.OpenAt(1)
        CombatKit.ClearWeapons(w);
        CombatKit.SetWeapon(w, 0, CombatKit.Mm, 2);
        CombatKit.SetWeapon(w, 2, CombatKit.Fb, 1);                 // 칸 1 은 비움

        PlayTo(s);
        var after1 = new HeroCarrier().Capture(s.World);
        var dmg1 = (float[])s.World.RunStats.DamageByWeapon.Clone();
        var kills1 = s.World.RunStats.Kills;
        Assert.True(dmg1[0] + dmg1[2] > 0f);
        Assert.Equal(0f, dmg1[1]);
        Assert.Equal(1, kills1);
        s.Advance();

        // 새 방 월드: 짐이 그대로(칸 번호 포함), 판 누적은 이어짐
        var w2 = s.World;
        Assert.NotSame(w, w2);
        Assert.Equal(Json(after1), Json(new HeroCarrier().Capture(w2)));
        Assert.Equal(CombatKit.Mm, w2.Hero.Weapons[0].Def!.Id);
        Assert.True(w2.Hero.Weapons[1].IsEmpty);
        Assert.Equal(CombatKit.Fb, w2.Hero.Weapons[2].Def!.Id);
        PlayTo(s);
        Assert.Equal(2, s.World.RunStats.Kills);
        Assert.True(s.World.RunStats.DamageByWeapon[0] + s.World.RunStats.DamageByWeapon[2] > dmg1[0] + dmg1[2]);
        Assert.Equal(0f, s.World.RunStats.DamageByWeapon[1]);
        Assert.Equal(s.World.RunStats.DamageByWeapon.Sum(), s.World.RunStats.DamageByWeapon[0] + s.World.RunStats.DamageByWeapon[2]);
    }

    [Fact]
    public void RealModules_SuspendAtStairs_EqualsUncutRun_V081()
    {
        var defs = RunDefs();

        var uncut = Session(defs, 31);
        ToBossRoom(uncut, RoomPhase.Exit);
        uncut.Advance();
        PlayToEnd(uncut);

        var cut = Session(defs, 31);
        ToBossRoom(cut, RoomPhase.Exit);
        Assert.True(cut.CanSuspend);
        var json = cut.Suspend().ToJson();
        var loaded = RunSnapshot.FromJson(json);
        Assert.Equal(json, loaded.ToJson());
        Assert.NotNull(loaded.Carry);
        Assert.Contains(loaded.Carry!.Weapons, x => x.Id.Length > 0);
        var resumed = new RunSession(defs, Modules(), loaded, RunOptions.Default);
        Assert.Equal((5, 5), (resumed.World.Hero.WeaponSlotsOpen, resumed.World.Hero.PassiveSlotsOpen));
        PlayToEnd(resumed);

        Assert.Equal(RunOutcome.Victory, uncut.World.Outcome);
        Assert.Equal(RunOutcome.Victory, resumed.World.Outcome);
        var ra = uncut.Conclude();
        var rb = resumed.Conclude();
        Assert.Equal(ra.Rooms.Count, rb.Rooms.Count);
        for (var i = 0; i < ra.Rooms.Count; i++) Assert.Equal(ra.Rooms[i], rb.Rooms[i]);
        Assert.Equal(ra.ElapsedTicks, rb.ElapsedTicks);
        Assert.Equal(Json(ra.Stats), Json(rb.Stats));
        Assert.True(rb.Stats.Kills > 0);
        Assert.Equal(Json(ra.Carry), Json(rb.Carry));                // 다시 쓴 짐 JSON 바이트
    }
}
