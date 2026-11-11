using System.Text.Json;
using DiabloDefenceRemastered.Core.Sim.Progression;
using DiabloDefenceRemastered.Core.Sim.Weapons;
using DiabloDefenceRemastered.Tests.Combat;
using DiabloDefenceRemastered.Tests.Shops;

namespace DiabloDefenceRemastered.Tests.Progression;

/// <summary>
/// 10.1 <c>Progression</c> 의 짐(v0.9 WP3 단계 2): 마시던 회복을 시간 효과(<c>potion_heal</c>)로 넘기기, 만렙 순번이 방을 넘어도 섞이지 않음(<c>MaxedBase</c>),
/// 그리고 <b>실제 <see cref="CombatModule"/></b> 으로 돈 판 — 상인 방에서 판 명령으로 산 장비·지갑·물약 칸·만난 상인·문을 넘는 회복을 든 짐으로 계단에서 끊은 판 = 끊지 않은 판
/// (<c>RoomRecord</c> 목록·<c>RunStats</c>·짐 JSON 바이트). 적 생성·DeathHooks 는 WP4 자리라 시험 시스템이 흉내 낸다(<see cref="CarryTests"/> 와 같은 모양).
/// </summary>
public class ShopRunCarryTests
{
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, RunSnapshot.JsonOptions);

    // ── 짐 단위 ─────────────────────────────────────────────────

    [Fact]
    public void PotionHeal_CarriedAsEffect_RestoredAndValidated()
    {
        var t = CombatKit.World(ShopKit.Defs());
        var w = t.World;
        w.Hero.Hp = 20f;
        w.Hero.Heal = new HealOverTime(100, 25f);
        w.Combat.SurgeTicks = 30;
        w.Combat.SurgePct = 0.5f;
        var carry = new HeroCarrier().Capture(w);
        Assert.Equal([new CarriedEffect(CombatState.SurgeEffectId, 30, 0.5f), new CarriedEffect(CombatState.PotionHealEffectId, 100, 25f)], carry.Effects);   // Id 서수 순
        Assert.Null(new HeroCarrier().Validate(carry, w.Defs));

        var back = JsonSerializer.Deserialize<HeroCarry>(Json(carry), RunSnapshot.JsonOptions)!;
        var w2 = CombatKit.World(ShopKit.Defs(), seed: 4).World;
        new HeroCarrier().Restore(w2, back);
        Assert.Equal(new HealOverTime(100, 25f), w2.Hero.Heal);
        Assert.Equal(Json(carry), Json(new HeroCarrier().Capture(w2)));

        var c = new HeroCarrier();
        Assert.Contains("남은 양", c.Validate(carry with { Effects = [new CarriedEffect(CombatState.PotionHealEffectId, 10, -1f)] }, w.Defs));
        Assert.Contains("지갑", c.Validate(carry with { Gold = -1 }, w.Defs));
        Assert.Contains("물약 칸", c.Validate(carry with { Potions = -1 }, w.Defs));
        var item = ShopKit.Item("t_cap");
        Assert.Contains("장비 칸", c.Validate(carry with { Equipment = [new CarriedEquip(EquipSlot.Head, item), new CarriedEquip(EquipSlot.Head, item)] }, w.Defs));

        // 회복 중이 아니면 효과가 없고, 풀면 회복도 없다(앞 월드의 회복이 남지 않음)
        w.Hero.Heal = default;
        var none = new HeroCarrier().Capture(w);
        Assert.DoesNotContain(none.Effects, e => e.Id == CombatState.PotionHealEffectId);
        w2.Hero.Heal = new HealOverTime(5, 5f);
        new HeroCarrier().Restore(w2, none);
        Assert.False(w2.Hero.Heal.Active);
    }

    [Fact]
    public void MaxedOrder_NewRoomFirstTickMax_StaysAfterCarriedOnes()
    {
        var t = CombatKit.World();
        var w = t.World;
        CombatKit.ClearWeapons(w);
        CombatKit.SetWeapon(w, 0, CombatKit.Mm, 8);
        CombatKit.SetWeapon(w, 1, CombatKit.Fb, 8);
        CombatKit.SetWeapon(w, 2, CombatKit.Orbit, 7);
        w.Hero.Weapons[0].MaxedAtTick = 50;                                             // 칸 0 이 나중, 칸 1 이 먼저
        w.Hero.Weapons[1].MaxedAtTick = 10;
        var carry = new HeroCarrier().Capture(w);
        Assert.Equal([1, 0, -1, -1, -1, -1], carry.MaxedAtTick);

        var w2 = CombatKit.World(seed: 2).World;                                         // 새 방 — Tick 0
        new HeroCarrier().Restore(w2, carry);
        Assert.Equal(2, w2.Combat.MaxedBase);
        Build.AddWeaponLevels(w2, 2, 1);                                                // 새 방 첫 틱에 만렙
        Assert.Equal(w2.Tick + 2, w2.Hero.Weapons[2].MaxedAtTick);
        Assert.Equal([1, 0, 2, -1, -1, -1], new HeroCarrier().Capture(w2).MaxedAtTick); // 순서가 칸 순서로 섞이지 않음(v0.9 WP1g 는 0·0 으로 칸 2 가 칸 0 앞)
    }

    // ── 실제 모듈 + RunSession ───────────────────────────────────

    /// <summary>
    /// <see cref="RoomActs"/>(방·액트) + <see cref="CombatKit"/>(무기·주인공·보석·금화·물약) + 장비·상인. 액트 1 의 문 보상 가중표를 그리스월드 하나로 —
    /// 3번 방으로 가는 문부터 상인이 나오고(그 앞은 <c>fallback_reward</c> 보물 상자), 액트 2 는 RoomActs 그대로.
    /// </summary>
    private static GameDefs RunDefs()
    {
        var r = RoomActs.Defs();
        var c = CombatKit.Defs();
        var act1 = r.Stages.Get(RoomActs.Act1Id);
        var smith = r.Rewards.Get(TestDefs.RewardSmithId);
        var shopAct1 = act1 with { Rewards = act1.Rewards with { Weights = [new WeightEntry<RewardDef>(DefRef<RewardDef>.To(smith), 1f)] } };
        return new GameDefs
        {
            Rules = c.Rules with { Rooms = r.Rules.Rooms },
            Sprites = r.Sprites, SpriteSets = r.SpriteSets, Projectiles = c.Projectiles,
            Weapons = c.Weapons, Passives = c.Passives, HeroClasses = c.HeroClasses,
            Tilesets = r.Tilesets, Maps = r.Maps, Enemies = r.Enemies, Rewards = r.Rewards,
            Stages = new DefTable<StageDef>([shopAct1, r.Stages.Get(RoomActs.Act2Id)]),
            ItemBases = r.ItemBases, ItemAffixes = r.ItemAffixes, Merchants = r.Merchants,
            Pickups = new DefTable<PickupDef>([.. r.Pickups.All, .. c.Pickups.All.Where(p => p.Kind is not PickupKind.Reward and not PickupKind.Chest)]),
        };
    }

    /// <summary>방마다 생존 1초째 주인공 오른쪽 120px 에 서 있는 적 하나(감독 자리).</summary>
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

    /// <summary>DeathHooks 자리: 죽은 적 → <c>EnemyKilled</c>, 정예·보스 방은 생존 4초째에 처치.</summary>
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

    /// <summary>판 기록(끊고 이은 판과 견줌): 방 2 문 단계에서 마신 회복이 방 3 으로 넘어갔는지, 상인 창에서 산 수.</summary>
    private sealed class Script
    {
        public bool HealCarried;
        public int Bought;
    }

    /// <summary>
    /// 지금 방을 <paramref name="until"/> 단계까지 — 사람·자동 조작과 같은 판 명령 길로만 민다. 선택 창은 첫 카드·상자 열기, 보상 물체는 주인공 발밑으로 옮겨 실제로 줍고,
    /// 상인 방은 상인 자리로 옮겨 열고 살 수 있는 첫 카드를 <c>Choice</c> 로(없으면 <c>Skip</c> = 떠나기), 문 단계는 <c>Exit</c> 0번.
    /// 액트 1 의 1번 방 첫 틱에 금화 1000·물약 둘을 발밑에, 2번 방 문 단계 첫 틱에 체력을 반으로 하고 <c>Drink</c> → 다음 틱에 나감(회복이 문을 넘음).
    /// </summary>
    private static void PlayTo(RunSession s, Script script, RoomPhase until = RoomPhase.Exit)
    {
        var w = s.World;
        var room = w.Room!;
        if (room.Plan.Act == 1 && room.Plan.Number == 1 && w.Tick == 0)
        {
            CombatKit.Drop(w, "t_gold", w.Hero.Pos, 1000);
            CombatKit.Drop(w, "t_potion", w.Hero.Pos);
            CombatKit.Drop(w, "t_potion", w.Hero.Pos);
        }
        if (room.Plan.Act == 1 && room.Plan.Number == 3 && w.Tick == 0) script.HealCarried = w.Hero.Heal.Active;
        var drank = false;
        for (var i = 0; i < 60 * 600 && room.Phase != until && room.Phase != RoomPhase.Exit && w.Outcome == RunOutcome.Running; i++)
        {
            switch (w.AwaitingChoice)
            {
                case ChoiceKind.LevelUp: w.Progression!.Choose(0); continue;
                case ChoiceKind.Chest: w.Progression!.OpenChest(); continue;
                case ChoiceKind.Reward: w.Progression!.ChooseReward(0); continue;
            }
            var cmd = PlayerCommand.None;
            if (room.Shop is { Open: true } shop)
            {
                var pick = shop.Cards.FindIndex(c => !c.Bought && c.Item is not null && c.Price <= w.Hero.Gold);
                cmd = new PlayerCommand(Vector2.Zero, pick switch { 0 => CommandButtons.Choice1, 1 => CommandButtons.Choice2, 2 => CommandButtons.Choice3, _ => CommandButtons.Skip });
                if (pick >= 0 && w.Combat.ShopLockTicks == 0) script.Bought++;
            }
            else if (room.Shop is { Visited: false } && room.Phase is RoomPhase.Reward or RoomPhase.Doors) w.Hero.Pos = w.Hero.PrevPos = room.RewardSpot;
            else if (room.Phase == RoomPhase.Reward)
            {
                foreach (var p in w.Pickups.Items)
                    if (!p.Removed && p.GatesExit) p.Pos = p.PrevPos = w.Hero.Pos;
            }
            else if (room.Phase == RoomPhase.Doors)
            {
                if (room.Plan.Act == 1 && room.Plan.Number == 2 && !drank)
                {
                    drank = true;
                    w.Hero.Hp = w.Hero.MaxHp * 0.5f;
                    cmd = new PlayerCommand(Vector2.Zero, CommandButtons.Drink);
                }
                else cmd = new PlayerCommand(Vector2.Zero, CommandButtons.Exit);
            }
            StageStepper.AppTick(w, cmd);
        }
    }

    private static void ToBossRoom(RunSession s, Script script)
    {
        for (var room = 0; room < 5; room++)
        {
            PlayTo(s, script);
            s.Advance();
        }
        Assert.Equal(6, s.World.Room!.Plan.Number);
        PlayTo(s, script);
        Assert.Equal(RoomPhase.Exit, s.World.Room.Phase);
    }

    private static void PlayToEnd(RunSession s, Script script)
    {
        for (var room = 0; room < 20; room++)
        {
            PlayTo(s, script);
            if (s.World.Outcome != RunOutcome.Running) break;
            s.Advance();
        }
        while (s.World.Outcome is RunOutcome.VictoryPending or RunOutcome.DefeatPending) StageStepper.AppTick(s.World, PlayerCommand.None);
    }

    [Fact]
    public void RealModules_ShopEquipWalletPotions_SuspendAtStairs_EqualsUncutRun()
    {
        var defs = RunDefs();

        var uncutScript = new Script();
        var uncut = Session(defs, 41);
        ToBossRoom(uncut, uncutScript);
        uncut.Advance();
        PlayToEnd(uncut, uncutScript);

        var cutScript = new Script();
        var cut = Session(defs, 41);
        ToBossRoom(cut, cutScript);
        Assert.True(cut.CanSuspend);
        var json = cut.Suspend().ToJson();
        var loaded = RunSnapshot.FromJson(json);
        Assert.Equal(json, loaded.ToJson());

        // 짐에 상인 방에서 산 장비·남은 지갑·물약 칸·만난 상인이 들었다(이 판이 실제로 그 길을 지났음)
        var carry = loaded.Carry!;
        Assert.True(cutScript.HealCarried);                                             // 방 2 → 3 문을 넘은 회복
        Assert.True(cutScript.Bought > 0);
        Assert.NotEmpty(carry.Equipment);
        Assert.Contains(TestDefs.SmithId, loaded.MerchantsMet);
        Assert.True(loaded.Stats.GoldSpent > 0);
        Assert.Equal(loaded.Stats.Gold - loaded.Stats.GoldSpent, carry.Gold);           // 지갑 = 얻은 − 쓴
        Assert.Equal(1, loaded.Stats.PotionsDrunk);
        Assert.InRange(carry.Potions, 1, 2);                                             // 마신 하나 뒤 보물 상자 물약으로 다시 참
        Assert.DoesNotContain(carry.Effects, e => e.Id == CombatState.PotionHealEffectId);   // 계단은 회복을 바로 채움
        Assert.Contains(loaded.Rooms, r => r.Shop == TestDefs.SmithId && r.GoldSpent > 0);

        var resumed = new RunSession(defs, Modules(), loaded, RunOptions.Default);
        Assert.Equal(Json(carry.Equipment), Json(new HeroCarrier().Capture(resumed.World).Equipment));
        PlayToEnd(resumed, cutScript);

        Assert.Equal(RunOutcome.Victory, uncut.World.Outcome);
        Assert.Equal(RunOutcome.Victory, resumed.World.Outcome);
        var ra = uncut.Conclude();
        var rb = resumed.Conclude();
        Assert.Equal(ra.Rooms.Count, rb.Rooms.Count);
        for (var i = 0; i < ra.Rooms.Count; i++) Assert.Equal(ra.Rooms[i], rb.Rooms[i]);
        Assert.Equal(ra.ElapsedTicks, rb.ElapsedTicks);
        Assert.Equal(Json(ra.Stats), Json(rb.Stats));
        Assert.Equal(Json(ra.Carry), Json(rb.Carry));                                   // 다시 쓴 짐 JSON 바이트
        Assert.Equal(ra.MerchantsMet, rb.MerchantsMet);
        Assert.Equal(uncutScript.Bought, cutScript.Bought);
    }
}
