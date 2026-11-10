using System.Reflection;
using System.Text.Json;

namespace DiabloDefenceRemastered.Tests.Run;

/// <summary>
/// 10.1 <c>Run</c> 행(v0.8 → v0.8.1, WP1f #10·#11·#14) — 계단 보상(<c>RunSlots.OpenAt</c>)·<c>--start-at</c> 상한·계단 저장(<c>CanSuspend</c>·<c>Suspend</c>·이어 하기)·
/// 이어 하기 불일치·<c>HeroCarry</c>/<c>RunSnapshot</c> JSON 왕복·<c>RunStats</c>/<c>FairnessCounters</c> 저장 멤버 규칙·모양 지문.
/// </summary>
public class RunSuspendTests
{
    /// <summary>정예 방 중간 보스·보스 방 보스를 생존 단계 4초째에 "처치"(DeathHooks 자리 — EnemyKilled·BossDefeated 만).</summary>
    private sealed class AutoKill : ISimSystem
    {
        public void Tick(World world)
        {
            if (world.Room is not { Phase: RoomPhase.Survive } room || room.ClockTicks != 4 * 60) return;
            if (room.Plan.Kind == RoomKind.Elite)
                world.Events.Current.EnemyKilled.Add(new EnemyKilled(default, world.Defs.Enemies.IndexOf(RoomActs.MinibossId), Vector2.Zero, EnemyRank.Unique, KillCause.Damage, default));
            else if (room.Plan.Kind == RoomKind.Boss)
                world.Events.Current.BossDefeated.Add(new BossDefeated(default, world.Defs.Enemies.IndexOf(RoomActs.BossId), Vector2.Zero));
        }
    }

    /// <summary>
    /// 시험 모듈: 직선 내비·<see cref="FakeSpawner"/>·<see cref="AutoKill"/>, 그리고 <b>주인공 만들기 흉내</b>(WP3 자리 — 열린 칸 = <c>RunSlots.OpenAt(World.Room?.Plan.Act ?? 1)</c>).
    /// 판 수명 상태를 두지 않는다(11.2) — 이어 하기는 새 모듈이다.
    /// </summary>
    private static ModuleContext Modules(FakeHeroCarry? carry = null)
    {
        var m = new ModuleContext();
        m.AddWorldInstaller((w, b) =>
        {
            w.Presentation.Enabled = false;
            w.Nav = new StraightNav(w.Arena!.Iso);
            w.Spatial = new BruteForceSpatial(w);
            w.Spawner = new FakeSpawner(w);
            var (weapons, passives) = RunSlots.OpenAt(w.Room?.Plan.Act ?? 1, w.Defs.Rules);
            w.Hero.WeaponSlotsOpen = weapons;
            w.Hero.PassiveSlotsOpen = passives;
            b.Add(SimPhase.DeathHooks, new AutoKill());
        });
        if (carry is not null) m.SetHeroCarry(carry);
        return m;
    }

    private static RunSession Session(GameDefs defs, ulong seed = 11, int startIndex = 0, FakeHeroCarry? carry = null, RunOptions? options = null,
                                      string act = RoomActs.Act1Id) =>
        new(defs, Modules(carry), new RunStart(seed, defs.Stages.Get(act), null, options ?? RunOptions.Default, startIndex));

    /// <summary>지금 방을 <paramref name="until"/> 단계까지(보상 물체는 집은 것으로 지움, 문 단계는 Exit 명령 0번). 판이 끝나면 멈춘다.</summary>
    private static void PlayTo(RunSession s, RoomPhase until = RoomPhase.Exit)
    {
        var w = s.World;
        for (var i = 0; i < 60 * 600 && w.Room!.Phase != until && w.Room.Phase != RoomPhase.Exit && w.Outcome == RunOutcome.Running; i++)
        {
            if (w.Room.Phase == RoomPhase.Reward)
                for (var k = 0; k < w.Pickups.Count; k++)
                    if (w.Pickups[k].GatesExit) w.Pickups.Remove(w.Pickups[k].Id);
            var cmd = w.Room.Phase == RoomPhase.Doors ? new PlayerCommand(Vector2.Zero, CommandButtons.Exit) : PlayerCommand.None;
            StageStepper.AppTick(w, cmd);
        }
    }

    /// <summary>액트 1 보스 방(방 6)까지 — 방 1~5 를 지나 보스 방의 <paramref name="until"/> 단계에서 멈춘다.</summary>
    private static void ToBossRoom(RunSession s, RoomPhase until, Action<RunSession>? eachRoom = null)
    {
        for (var room = 0; room < 5; room++)
        {
            eachRoom?.Invoke(s);
            PlayTo(s);
            s.Advance();
        }
        Assert.Equal(6, s.World.Room!.Plan.Number);
        PlayTo(s, until);
        Assert.Equal(until, s.World.Room.Phase);
    }

    private static void PlayToEnd(RunSession s, List<string>? columns = null)
    {
        for (var room = 0; room < 20; room++)
        {
            var w = s.World;
            if (columns is not null)
                for (var i = 0; i < 240 && w.Room!.Phase is RoomPhase.Enter or RoomPhase.Survive; i++)
                {
                    StageStepper.AppTick(w, PlayerCommand.None);
                    if (i % 60 == 59) columns.Add(RoomColumn(w));
                }
            PlayTo(s);
            if (s.World.Outcome != RunOutcome.Running) break;
            s.Advance();
        }
        while (s.World.Outcome is RunOutcome.VictoryPending or RunOutcome.DefeatPending) StageStepper.AppTick(s.World, PlayerCommand.None);
    }

    private static string RoomColumn(World w) =>
        $"{w.Room!.Plan.Act}:{w.Room.Plan.Number}|{w.Tick}|{w.Hero.Pos}|{w.Hero.Hp}|{w.Hero.WeaponSlotsOpen}|{w.Room.Phase}|{w.Room.ClockTicks}|{w.DifficultyMinutes}|" +
        $"{w.Lighting.Darkness}|{w.Lighting.HeroLightRadius}|{w.Rng.Spawn.GetState()}|{w.Rng.Drop.GetState()}|{w.Enemies.Count}|{w.Pickups.Count}";

    /// <summary>방 1 에서 판 누적 값을 0 아닌 값으로(끊고 잇기 비교가 빈 값끼리 같아지지 않게).</summary>
    private static void Seed(RunSession s)
    {
        if (s.World.Room!.Plan.Number != 1 || s.World.Room.Plan.Act != 1) return;
        var w = s.World;
        w.RunStats.Kills += 7;
        w.RunStats.Gold += 33;
        w.RunStats.DamageByWeapon[5] += 12.5f;                       // 6번째 칸(v0.8.1 — v0.8 은 범위 밖)
        w.Fairness.KiteSwitches += 3;
        w.Hero.Hp = 41f;
        w.Hero.MaxHp = 50f;
        w.Hero.Belt = 1;
        w.Hero.Weapons[4] = new WeaponSlot { Def = w.Defs.Weapons[0], Level = 2, MaxedAtTick = -1 };
    }

    // ── 계단 보상·--start-at ────────────────────────────────────────────

    [Fact]
    public void RunSlots_OpenAt_ActNumberOnly_Cap6()
    {
        var rules = RoomActs.Defs().Rules;
        Assert.Equal((4, 4), RunSlots.OpenAt(1, rules));
        Assert.Equal((5, 5), RunSlots.OpenAt(2, rules));
        Assert.Equal((6, 6), RunSlots.OpenAt(3, rules));
        Assert.Equal((6, 6), RunSlots.OpenAt(4, rules));             // 상한 6 = 그릇
        Assert.Equal((4, 4), RunSlots.OpenAt(0, rules));
        Assert.Equal(6, WeaponSlots.SlotCount);
        Assert.Equal(6, PassiveSlots.SlotCount);
    }

    [Fact]
    public void Stairs_OpensOneMoreSlot_SameAsStartingAtAct2()
    {
        var defs = RoomActs.Defs();
        var carry = new FakeHeroCarry();
        var s = Session(defs, carry: carry);
        Assert.Equal(4, s.World.Hero.WeaponSlotsOpen);
        ToBossRoom(s, RoomPhase.Exit);
        s.Advance();                                                  // 계단
        Assert.Equal(2, s.Run.Act.ActNumber);
        Assert.Equal((5, 5), (s.World.Hero.WeaponSlotsOpen, s.World.Hero.PassiveSlotsOpen));
        Assert.Equal((5, 5), (s.Run.Carry!.WeaponSlotsOpen, s.Run.Carry.PassiveSlotsOpen));

        // RunStart 를 액트 2 1번 방으로 준 판 = 보통 판의 액트 2 (v0.8.1 — v0.8 은 계단을 안 지나 4칸)
        var direct = Session(defs, carry: new FakeHeroCarry(), act: RoomActs.Act2Id);
        Assert.Equal(2, direct.Run.Act.ActNumber);
        Assert.Equal((5, 5), (direct.World.Hero.WeaponSlotsOpen, direct.World.Hero.PassiveSlotsOpen));
    }

    [Fact]
    public void StartAt_UpperLimit_PerRoomKind()
    {
        var defs = RoomActs.Defs();
        var rules = defs.Rules.Rooms!;
        var act = defs.Stages.Get(RoomActs.Act1Id);
        Assert.Equal(3600, RoomStartLimits.MaxStartClockTicks(act.Rooms[0], rules));   // 일반 = 생존 길이
        Assert.Equal(9000, RoomStartLimits.MaxStartClockTicks(act.Rooms[3], rules));   // 정예 = elite_spawn_cap 2:30
        Assert.Null(RoomStartLimits.MaxStartClockTicks(act.Rooms[5], rules));          // 보스 = 없음

        RunOptions At(float s) => RunOptions.Default with { StartAt = new ClockTime(s) };
        Assert.Throws<ArgumentOutOfRangeException>(() => Session(defs, options: At(60f)));                 // 같음도 넘음
        Assert.Equal(59 * 60, Session(defs, options: At(59f)).World.Room!.ClockTicks);
        Assert.Throws<ArgumentOutOfRangeException>(() => Session(defs, startIndex: 3, options: At(150f)));
        Assert.Equal(149 * 60, Session(defs, startIndex: 3, options: At(149f)).World.Room!.ClockTicks);
        Assert.Equal(600 * 60, Session(defs, startIndex: 5, options: At(600f)).World.Room!.ClockTicks);
    }

    [Fact]
    public void RoomStart_EliteBonus_IsPreviousOrderFirst()
    {
        var defs = RoomActs.Defs();
        var boss = Session(defs, startIndex: 5);
        Assert.Same(boss.Run.Act.Rooms[4].RewardOrder[0], boss.World.Room!.Reward);
    }

    // ── 계단 저장 ──────────────────────────────────────────────────────

    [Fact]
    public void CanSuspend_OnlyAtActBossDoors_OrStairsExit()
    {
        var defs = RoomActs.Defs();
        var s = Session(defs);
        Assert.False(s.CanSuspend);
        PlayTo(s, RoomPhase.Doors);
        Assert.Equal(RoomPhase.Doors, s.World.Room!.Phase);
        Assert.False(s.CanSuspend);                                   // 일반 방 문 단계
        Assert.Throws<InvalidOperationException>(() => s.Suspend());

        var b = Session(defs, startIndex: 5);
        PlayTo(b, RoomPhase.Survive);
        Assert.False(b.CanSuspend);                                   // 보스 방 생존 단계
        PlayTo(b, RoomPhase.Doors);
        Assert.True(b.CanSuspend);
        b.World.AwaitingChoice = ChoiceKind.LevelUp;
        Assert.False(b.CanSuspend);                                   // 선택 창
        b.World.AwaitingChoice = ChoiceKind.None;
        b.World.Hero.Dead = true;
        Assert.False(b.CanSuspend);

        // 판의 마지막 보스 방(다음 액트 없음)은 계단이 아니다
        var last = Session(defs, startIndex: 2, act: RoomActs.Act2Id);
        PlayTo(last, RoomPhase.Doors);
        Assert.NotEqual(RoomPhase.Doors, last.World.Room!.Phase);    // 승리로 끝남
        Assert.False(last.CanSuspend);
    }

    [Fact]
    public void SuspendAtDoors_ThenResume_EqualsUncutRun_V081()
    {
        var defs = RoomActs.Defs();

        // (가) 끊지 않은 판: 보스 방 문 단계 첫 틱 다음 틱에 계단 문 Exit 명령으로 나감
        var uncut = Session(defs, seed: 31, carry: new FakeHeroCarry());
        ToBossRoom(uncut, RoomPhase.Doors, Seed);
        StageStepper.AppTick(uncut.World, new PlayerCommand(Vector2.Zero, CommandButtons.Exit, 0));
        Assert.Equal(RoomPhase.Exit, uncut.World.Room!.Phase);
        uncut.Advance();
        var uncutColumns = new List<string>();
        PlayToEnd(uncut, uncutColumns);

        // (나) 같은 틱에 Suspend(ⓐ — Core 가 계단 문 Exit 한 틱) → JSON → 새 모듈로 이어 하기
        var cut = Session(defs, seed: 31, carry: new FakeHeroCarry());
        ToBossRoom(cut, RoomPhase.Doors, Seed);
        Assert.True(cut.CanSuspend);
        var snapshot = cut.Suspend();
        Assert.True(cut.Suspended);
        Assert.Throws<InvalidOperationException>(cut.Advance);
        Assert.Throws<InvalidOperationException>(() => cut.Conclude());
        var json = snapshot.ToJson();
        var loaded = RunSnapshot.FromJson(json);
        Assert.Equal(json, loaded.ToJson());                          // 다시 쓴 바이트 같음
        var resumedCarry = new FakeHeroCarry();
        var resumed = new RunSession(defs, Modules(resumedCarry), loaded, RunOptions.Default);
        Assert.Equal(1, resumedCarry.Restores);
        Assert.Equal((5, 5), (resumed.World.Hero.WeaponSlotsOpen, resumed.World.Hero.PassiveSlotsOpen));   // 계단 보상이 짐에
        var resumedColumns = new List<string>();
        PlayToEnd(resumed, resumedColumns);
        Assert.NotEmpty(uncutColumns);

        AssertSameRun(uncut, resumed);
        Assert.Equal(uncutColumns, resumedColumns);                   // 방 상태 열(StateHasher 전의 대리, 13.19)
    }

    [Fact]
    public void SuspendAtStairsExit_InsteadOfAdvance_EqualsUncutRun()
    {
        // (다) 계단 문 틈을 밟아 Phase == Exit 가 된 틱(자동 조작 판) — Advance 대신 Suspend(ⓑ)
        var defs = RoomActs.Defs();
        var uncut = Session(defs, seed: 44, carry: new FakeHeroCarry());
        ToBossRoom(uncut, RoomPhase.Exit, Seed);
        uncut.Advance();
        PlayToEnd(uncut);

        var cut = Session(defs, seed: 44, carry: new FakeHeroCarry());
        ToBossRoom(cut, RoomPhase.Exit, Seed);
        var tick = cut.World.Tick;
        Assert.True(cut.CanSuspend);
        var resumed = new RunSession(defs, Modules(new FakeHeroCarry()), RunSnapshot.FromJson(cut.Suspend().ToJson()), RunOptions.Default);
        Assert.Equal(tick, cut.Run.Rooms[^1].Ticks);                  // 틱을 더 돌리지 않음
        PlayToEnd(resumed);
        AssertSameRun(uncut, resumed);
    }

    [Fact]
    public void SuspendWithoutHeroCarry_CarryNull_Resumes()
    {
        var defs = RoomActs.Defs();
        var s = Session(defs, seed: 5);
        ToBossRoom(s, RoomPhase.Doors);
        var snap = s.Suspend();
        Assert.Null(snap.Carry);
        Assert.Equal(RoomActs.Act2Id, snap.NextActId);
        Assert.Equal(2, snap.NextActNumber);
        Assert.Equal(RunSnapshot.RoomsHash(defs.Stages.Get(RoomActs.Act2Id)), snap.NextActRoomsHash);
        Assert.Equal(6, snap.Rooms.Count);
        Assert.Equal("gate_ne", snap.Rooms[^1].Door);
        var resumed = new RunSession(defs, Modules(), RunSnapshot.FromJson(snap.ToJson()), RunOptions.Default);
        Assert.Equal(new RunDepth(2, 1), resumed.Run.Depth);
        Assert.Equal(TestDefs.RewardTomeId, resumed.World.Room!.Reward!.Id);   // 다음 액트 1번 방 = first_room
    }

    [Fact]
    public void SuspendOnSideChain_ActNumberCountedFromStartAct_Resumes()
    {
        // 뿌리 사슬 밖 액트끼리 next_act 로 이어진 판(헬파이어 곁가지 H1 → H2): RunPlanner.ActNumber 는 둘 다 1 이지만 판 안 번호는 1 → 2(v0.8.2 검증 반영)
        var basic = RoomActs.Defs();
        var h2 = basic.Stages.Get(RoomActs.Act2Id) with { Id = "test_h2", Requires = [ContentBundle.Hellfire] };
        var h1 = basic.Stages.Get(RoomActs.Act1Id) with { Id = "test_h1", Requires = [ContentBundle.Hellfire], NextAct = DefRef<StageDef>.To(h2) };
        var defs = new GameDefs
        {
            Rules = basic.Rules, Sprites = basic.Sprites, SpriteSets = basic.SpriteSets, Weapons = basic.Weapons, Passives = basic.Passives,
            HeroClasses = basic.HeroClasses, Tilesets = basic.Tilesets, Maps = basic.Maps, Enemies = basic.Enemies, Rewards = basic.Rewards, Pickups = basic.Pickups,
            Stages = new DefTable<StageDef>([.. basic.Stages.All, h1, h2]),
        };
        Assert.Equal(1, RunPlanner.ActNumber(defs, h2));              // 사슬 밖

        var s = Session(defs, seed: 6, carry: new FakeHeroCarry(), act: h1.Id);
        ToBossRoom(s, RoomPhase.Doors);
        var snap = RunSnapshot.FromJson(s.Suspend().ToJson());
        Assert.Equal(h2.Id, snap.NextActId);
        Assert.Equal(2, snap.NextActNumber);
        var resumed = new RunSession(defs, Modules(new FakeHeroCarry()), snap, RunOptions.Default);
        Assert.Equal(2, resumed.Run.Act.ActNumber);
        Assert.Equal(new RunDepth(2, 1), resumed.Run.Depth);

        Assert.Contains("번호가 바뀌었다", Assert.Throws<RunSnapshotMismatchException>(
            () => new RunSession(defs, Modules(new FakeHeroCarry()), snap with { NextActNumber = 3 }, RunOptions.Default)).Message);
        Assert.Contains("사슬에 없다", Assert.Throws<RunSnapshotMismatchException>(                 // 뿌리 액트 1 뒤에는 H2 가 없다
            () => new RunSession(defs, Modules(new FakeHeroCarry()), snap with { StartActId = RoomActs.Act1Id }, RunOptions.Default)).Message);
    }

    private static void AssertSameRun(RunSession a, RunSession b)
    {
        Assert.Equal(RunOutcome.Victory, a.World.Outcome);
        Assert.Equal(RunOutcome.Victory, b.World.Outcome);
        var ra = a.Conclude();
        var rb = b.Conclude();
        Assert.Equal(ra.Rooms.Count, rb.Rooms.Count);
        for (var i = 0; i < ra.Rooms.Count; i++) Assert.Equal(ra.Rooms[i], rb.Rooms[i]);   // 보스 방 Ticks 포함
        Assert.Equal(ra.ElapsedTicks, rb.ElapsedTicks);
        Assert.Equal(ra.Depth, rb.Depth);
        Assert.Equal(Json(ra.Stats), Json(rb.Stats));
        Assert.Equal(Json(ra.Fairness), Json(rb.Fairness));
        Assert.Equal(12.5f, rb.Stats.DamageByWeapon[5]);
        Assert.Equal(Json(ra.Carry), Json(rb.Carry));
    }

    private static string Json<T>(T value) => JsonSerializer.Serialize(value, RunSnapshot.JsonOptions);

    // ── 이어 하기 불일치 ───────────────────────────────────────────────

    private static RunSnapshot SnapshotWithCarry(GameDefs defs, FakeHeroCarry carry)
    {
        var s = Session(defs, seed: 9, carry: carry);
        ToBossRoom(s, RoomPhase.Doors);
        return RunSnapshot.FromJson(s.Suspend().ToJson());
    }

    [Fact]
    public void Mismatch_AllCases_RestoreNeverCalled()
    {
        var defs = RoomActs.Defs();
        var snap = SnapshotWithCarry(defs, new FakeHeroCarry());

        void Fails(GameDefs d, RunSnapshot s, FakeHeroCarry? carry, string why)
        {
            var c = carry ?? new FakeHeroCarry();
            var ex = Assert.Throws<RunSnapshotMismatchException>(() => new RunSession(d, Modules(c), s, RunOptions.Default));
            Assert.Contains(why, ex.Message);
            Assert.Equal(0, c.Restores);
        }

        // rooms_hash 를 바꾼 정의(다음 액트 방 하나의 끝 시각)
        var act1 = defs.Stages.Get(RoomActs.Act1Id);
        var act2 = defs.Stages.Get(RoomActs.Act2Id);
        var act2b = act2 with { Rooms = [act2.Rooms[0] with { To = new ClockTime(50f) }, act2.Rooms[1] with { From = new ClockTime(50f) }, act2.Rooms[2]] };
        var changed = new GameDefs
        {
            Rules = defs.Rules, Sprites = defs.Sprites, SpriteSets = defs.SpriteSets, Weapons = defs.Weapons, Passives = defs.Passives,
            HeroClasses = defs.HeroClasses, Tilesets = defs.Tilesets, Maps = defs.Maps, Enemies = defs.Enemies, Rewards = defs.Rewards, Pickups = defs.Pickups,
            Stages = new DefTable<StageDef>([act1 with { NextAct = DefRef<StageDef>.To(act2b) }, act2b]),
        };
        Fails(changed, snap, null, "방 표가 바뀌었다");

        Fails(defs, snap with { Carry = snap.Carry! with { Weapons = [new CarriedSlot("no_such_weapon", 1, false)] } }, null, "no_such_weapon");
        Fails(defs, snap with { Carry = snap.Carry! with { Shrines = ["no_such_shrine"] } }, null, "no_such_shrine");
        Fails(defs, snap with { Format = RunSnapshot.CurrentFormat + 1 }, null, "모양 판");
        Fails(defs, snap with { Carry = null }, null, "짐(carry)이 없다");
        Fails(defs, snap with { Stats = new RunStats { DamageByWeapon = new float[4] } }, null, "무기별 피해 칸");
        Fails(defs, snap with { NextActId = "no_such_act" }, null, "no_such_act");
        Fails(defs, snap, new FakeHeroCarry { ValidateReason = "시간 효과 mana_surge 를 모른다" }, "mana_surge");
        Assert.Throws<RunSnapshotMismatchException>(() => new RunSession(defs, Modules(), snap, RunOptions.Default));   // 짐이 있는데 IHeroCarry 없음

        // 짐의 성소 ID 가 정의에 있으면 통과
        var ok = new RunSession(defs, Modules(new FakeHeroCarry()), snap with { Carry = snap.Carry! with { Shrines = [TestDefs.RewardShrineId] } }, RunOptions.Default);
        Assert.Equal([TestDefs.RewardShrineId], ok.Run.Carry!.Shrines);
    }

    // ── JSON 왕복·저장 멤버 규칙·모양 지문 ─────────────────────────────

    [Fact]
    public void HeroCarry_AllFields_JsonRoundTrip_SameBytes()
    {
        var carry = new HeroCarry
        {
            Level = 12, Xp = 345, RerollsLeft = 2, Hp = 77.5f,
            Weapons = [new CarriedSlot("w1", 3, false), new CarriedSlot("", 0, false), new CarriedSlot("w2", 8, true)],
            Passives = [new CarriedSlot("p1", 2, false)],
            Blessings = [new CarriedSlot("b1", 1, false)],
            WeaponSlotsOpen = 5, PassiveSlotsOpen = 5, Belt = 2,
            Mods = [new CarriedStat(StatKey.MaxHp, 10f), new CarriedStat(StatKey.MaxHpPct, -0.1f)],
            Shrines = ["shrine_fascinating", "shrine_gloomy"],
            Effects = [new CarriedEffect("buff_mana_surge", 90, 0.5f)],
        };
        var json = Json(carry);
        Assert.Contains("\"max_hp_pct\"", json);
        var back = JsonSerializer.Deserialize<HeroCarry>(json, RunSnapshot.JsonOptions)!;
        Assert.Equal(json, Json(back));
        Assert.Equal(carry.Weapons, back.Weapons);                   // 목록은 원소별(record == 는 참조 비교라 늘 거짓)
        Assert.Equal(carry.Mods, back.Mods);
        Assert.Equal(carry.Shrines, back.Shrines);
        Assert.Equal(carry.Effects, back.Effects);
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<HeroCarry>("""{ "level": 1, "xp": 0, "rerolls_left": 0, "hp": 1 }""", RunSnapshot.JsonOptions));   // required 빠짐
    }

    [Theory]
    [InlineData(typeof(RunStats))]
    [InlineData(typeof(FairnessCounters))]
    public void SaveMemberRule_AllPublicMembersWritableValueTypes_RoundTripNonZero(Type type)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        var obj = Activator.CreateInstance(type)!;
        var n = 1;
        foreach (var p in type.GetProperties(flags))
        {
            Assert.True(p.CanWrite && p.SetMethod!.IsPublic, $"{type.Name}.{p.Name} 는 get 만 있다 — STJ 가 읽을 때 건너뛰어 조용히 0 이 된다(3.6 저장 멤버 규칙)");
            Assert.True(Allowed(p.PropertyType), $"{type.Name}.{p.Name} 의 형 {p.PropertyType.Name} 은 값 형식·그 배열이 아니다");
            p.SetValue(obj, NonZero(p.PropertyType, p.GetValue(obj), n++));
        }
        foreach (var f in type.GetFields(flags))
        {
            Assert.False(f.IsInitOnly, $"{type.Name}.{f.Name} 는 readonly 필드다");
            Assert.True(Allowed(f.FieldType), $"{type.Name}.{f.Name} 의 형 {f.FieldType.Name} 은 값 형식·그 배열이 아니다");
            f.SetValue(obj, NonZero(f.FieldType, f.GetValue(obj), n++));
        }
        Assert.True(n > 1);
        var json = JsonSerializer.Serialize(obj, type, RunSnapshot.JsonOptions);
        var back = JsonSerializer.Deserialize(json, type, RunSnapshot.JsonOptions)!;
        Assert.Equal(json, JsonSerializer.Serialize(back, type, RunSnapshot.JsonOptions));
        Assert.DoesNotContain(":0,", json.Replace(":0.", ":x."));   // 0 이 된 멤버 없음

        static bool Allowed(Type t) => t.IsValueType || (t.IsArray && t.GetElementType()!.IsValueType);

        static object NonZero(Type t, object? current, int n)
        {
            if (t.IsArray)
            {
                var length = current is Array a ? a.Length : 3;
                var arr = Array.CreateInstance(t.GetElementType()!, length);
                for (var i = 0; i < length; i++) arr.SetValue(Convert.ChangeType(n + i + 1, t.GetElementType()!), i);
                return arr;
            }
            if (t == typeof(bool)) return true;
            return Convert.ChangeType(n, t);
        }
    }

    [Fact]
    public void ShapeFingerprint_MatchesCurrentFormat_DamageByWeaponIsSix()
    {
        // 짐·RunStats(조각 포함)·FairnessCounters·RoomRecord·RunDepth·RunSnapshot 의 공개 멤버를 바꾸면 깨진다 → RunSnapshot.CurrentFormat 과 ShapeFingerprint 를 함께 올린다
        Assert.Equal(1, RunSnapshot.CurrentFormat);
        Assert.Equal(RunSnapshot.ShapeFingerprint, RunSnapshot.ComputeShapeFingerprint());
        Assert.Equal(6, new RunStats().DamageByWeapon.Length);
        Assert.Null(typeof(RunStats).GetField("WeaponSlotCount"));    // v0.8.1: 상수 없앰
    }

    [Fact]
    public void RoomsHash_KindNumberFromToOnly()
    {
        var act = RoomActs.Defs().Stages.Get(RoomActs.Act1Id);
        var h = RunSnapshot.RoomsHash(act);
        Assert.Matches("^[0-9a-f]{8}$", h);
        Assert.Equal(h, RunSnapshot.RoomsHash(act with { RoomMaps = new RoomMapPoolDef(), Rewards = new StageRewardsDef() }));   // 맵·보상을 바꿔도 같음
        Assert.NotEqual(h, RunSnapshot.RoomsHash(act with { Rooms = [.. act.Rooms.Take(5)] }));
        Assert.NotEqual(h, RunSnapshot.RoomsHash(act with { Rooms = [act.Rooms[0] with { Kind = RoomKind.Elite }, .. act.Rooms.Skip(1)] }));
    }
}
