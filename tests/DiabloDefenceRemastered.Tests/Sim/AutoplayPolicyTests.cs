using DiabloDefenceRemastered.Core.Sim.Autoplay;
using static DiabloDefenceRemastered.Tests.Enemies.Wp4Defs;
using SimWorld = DiabloDefenceRemastered.Core.Sim.World;

namespace DiabloDefenceRemastered.Tests.Sim;

/// <summary>
/// 자동 조작 정책 시험 (설계서 9절·기획 15절): 카드 순위(진화 재료 무기 강화 > 가진 무기 강화 > 새 무기 > 가진 패시브 > 새 패시브 > 채움, 같으면 왼쪽),
/// 문 우선순위(체력 &lt; 50%·허리띠 빔이면 물약 > 마법서 > 보물 상자 > 성소)와 <c>DOOR_SKIP</c>(넘으면 <c>Exit</c> + 고른 문 번호, 방마다 한 번 기록),
/// 적 피하기 방향, 월드가 바뀌면 방마다 판단 상태를 비움.
/// </summary>
public sealed class AutoplayPolicyTests
{
    private static LevelUpCard Card(CardKind kind, string id) => new(kind, id, id, null, 0, 1, []);

    [Fact]
    public void Pick_card_follows_rank_then_leftmost()
    {
        var policy = new AutoplayPolicy(AutoplayVisit.None);
        Assert.Equal(3, policy.PickCard(new LevelUpOffer(2, [Card(CardKind.NewPassive, "p"), Card(CardKind.Filler, "f"), Card(CardKind.NewWeapon, "w1"), Card(CardKind.WeaponUpgrade, "w2")], 0)));
        Assert.Equal(1, policy.PickCard(new LevelUpOffer(2, [Card(CardKind.NewPassive, "p"), Card(CardKind.NewWeapon, "a"), Card(CardKind.NewWeapon, "b")], 0)));
        Assert.Equal(2, policy.PickCard(new LevelUpOffer(2, [Card(CardKind.Filler, "f"), Card(CardKind.NewPassive, "p"), Card(CardKind.PassiveUpgrade, "q")], 0)));
        Assert.Equal(0, policy.PickCard(new LevelUpOffer(2, [Card(CardKind.Filler, "f")], 0)));
    }

    [Fact]
    public void Evolution_material_upgrade_beats_other_weapon_upgrade()
    {
        // 합성 직업의 특성 패시브(test_trait)를 재료로 하는 진화: test_weapon + test_trait → test_evolved
        var basic = TestDefs.Basic();
        var weapon = basic.Weapons.Get(TestDefs.WeaponId);
        var trait = basic.Passives.Get(TestDefs.TraitId);
        var evo = new EvolutionDef
        {
            Weapon = DefRef<WeaponDef>.To(weapon), Passive = DefRef<PassiveDef>.To(trait), Result = DefRef<WeaponDef>.To(TestDefs.Weapon("test_evolved")),
        };
        var t = World(Defs([], evolutions: [evo]));
        // 특성 패시브를 주는 것은 WP3 진행 — 여기서는 칸에 바로 넣는다
        t.Hero.Passives[0] = new PassiveSlot { Def = trait, Level = 1 };

        var offer = new LevelUpOffer(3, [Card(CardKind.WeaponUpgrade, "other_weapon"), Card(CardKind.WeaponUpgrade, TestDefs.WeaponId)], 0);
        var policy = new AutoplayPolicy(AutoplayVisit.None);
        Assert.Equal(0, policy.PickCard(offer));   // 월드를 아직 못 봄 → 같은 순위, 왼쪽
        policy.Next(t.World);
        Assert.Equal(1, policy.PickCard(offer));
        t.Hero.Passives[0] = default;
        Assert.Equal(0, policy.PickCard(offer));   // 재료 패시브가 없으면 보통 강화
    }

    private static (int Index, IReadOnlyList<DoorSkip> Skips) DoorAfterSkip(Func<GameDefs, RewardDef> nw, Func<GameDefs, RewardDef> ne, float hp = 100f)
    {
        var defs = Defs([]);
        var t = new TestWorldBuilder().WithDefs(defs).WithRoom(RoomPhase.Doors, exits: ["gate_nw", "gate_ne"]).WithModules(FakeArena.Modules(weapons: false)).Build();
        var room = t.World.Room!;
        room.Exits = [new RoomExit("gate_nw", nw(defs), RoomKind.Normal, null, false, null), new RoomExit("gate_ne", ne(defs), RoomKind.Normal, null, false, null)];
        t.Hero.MaxHp = 100f;
        t.Hero.Hp = hp;
        var policy = new AutoplayPolicy(AutoplayVisit.None);
        var first = policy.Next(t.World);
        Assert.Equal(CommandButtons.None, first.Buttons);   // 넘김 전에는 문 쪽으로 걸어감
        var skip = MathX.SecondsToTicks(defs.Rules.Rooms!.AutoplayDoorSkip, SimWorld.Dt);
        t.Step(skip + 2);
        var cmd = policy.Next(t.World);
        Assert.Equal(CommandButtons.Exit, cmd.Buttons);
        policy.Next(t.World);
        return (cmd.ExitIndex, policy.DoorSkips);
    }

    private static RewardDef R(GameDefs d, string id) => d.Rewards.Get(id);

    [Fact]
    public void Door_priority_prefers_chest_over_shrine_and_logs_one_door_skip()
    {
        var (index, skips) = DoorAfterSkip(d => R(d, TestDefs.RewardShrineId), d => R(d, TestDefs.RewardChestId));
        Assert.Equal(1, index);
        Assert.Single(skips);
    }

    [Fact]
    public void Door_priority_prefers_heal_door_when_hurt()
    {
        Assert.Equal(1, DoorAfterSkip(d => R(d, TestDefs.RewardChestId), d => R(d, TestDefs.RewardPotionId), hp: 40f).Index);
        Assert.Equal(0, DoorAfterSkip(d => R(d, TestDefs.RewardChestId), d => R(d, TestDefs.RewardPotionId), hp: 90f).Index);
    }

    [Fact]
    public void Door_priority_prefers_useful_tome_over_chest()
    {
        Assert.Equal(1, DoorAfterSkip(d => R(d, TestDefs.RewardChestId), d => R(d, TestDefs.RewardTomeId)).Index);
    }

    [Fact]
    public void Avoid_moves_away_from_nearby_enemy()
    {
        var t = World(Defs([]));
        t.AddEnemy(t.Hero.Pos + new Vector2(80f, 0f));
        var cmd = new AutoplayPolicy(AutoplayVisit.None).Next(t.World);
        Assert.True(cmd.Move.X < 0f, $"적 반대쪽으로 가지 않았다 ({cmd.Move})");
        Assert.True(cmd.Move.Length() <= 1f + 1e-4f);
    }

    // ── 보스 묶음 A: 탄 피하기(6.20 ⑪, 기획 15절) ───────────────────────

    /// <summary>날고 있는 적 탄 하나(가짜 이웃은 투사체를 움직이지 않는다 — 정책이 읽기만).</summary>
    private static Projectile Bullet(TestWorld t, Vector2 offset, Vector2 vel)
    {
        var p = t.World.Projectiles.Add();
        p.Def = DiabloDefenceRemastered.Tests.Enemies.BossKit.Orb;
        p.Faction = Faction.Enemy;
        p.Radius = 5f;
        p.Pos = p.PrevPos = t.Hero.Pos + offset;
        p.Vel = vel;
        p.Speed = vel.Length();
        return p;
    }

    [Fact]
    public void HeadOnBullet_SidestepsPerpendicular()
    {
        // 오른쪽 80px 에서 120px/s 로 날아오는 탄(주인공 3px 위를 지남) — 탄 길에 수직으로, 탄 길 반대쪽(아래)으로 비킨다
        var t = World(Defs([]));
        Bullet(t, new Vector2(80f, -3f), new Vector2(-120f, 0f));
        var dodge = AutoplayPolicy.Dodge(t.World);
        Assert.True(dodge.Length() > 0.1f);
        Assert.InRange(Vector2.Dot(Vector2.Normalize(dodge), new Vector2(-1f, 0f)), -0.01f, 0.01f);
        Assert.True(dodge.Y > 0f);
        var cmd = new AutoplayPolicy(AutoplayVisit.None).Next(t.World);
        Assert.True(cmd.Move.Y > 0.1f && MathF.Abs(cmd.Move.X) < 0.05f, $"수직으로 비키지 않았다 ({cmd.Move})");
    }

    [Fact]
    public void NoBullets_SameCommandAsBefore()
    {
        var t = World(Defs([]));
        var policy = new AutoplayPolicy(AutoplayVisit.None);
        var quiet = policy.Next(t.World);
        Assert.Equal(Vector2.Zero, AutoplayPolicy.Dodge(t.World));
        // 멀어지는 탄·200px 밖 탄·멀리 지나가는 탄은 셈하지 않는다 — 명령이 탄 없을 때와 같다
        Bullet(t, new Vector2(80f, 0f), new Vector2(120f, 0f));
        Bullet(t, new Vector2(260f, 0f), new Vector2(-120f, 0f));
        Bullet(t, new Vector2(100f, 60f), new Vector2(-120f, 0f));
        Assert.Equal(Vector2.Zero, AutoplayPolicy.Dodge(t.World));
        Assert.Equal(quiet, policy.Next(t.World));
    }
}
