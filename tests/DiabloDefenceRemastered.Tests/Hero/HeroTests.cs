using System.Text.Json;
using DiabloDefenceRemastered.Core.Sim.Heroes;
using DiabloDefenceRemastered.Core.Sim.Progression;
using DiabloDefenceRemastered.Tests.Combat;
using DiabloDefenceRemastered.Tests.Data;

namespace DiabloDefenceRemastered.Tests.Heroes;

/// <summary>
/// 주인공(WP3): 실제 <c>heroes/sorcerer.json</c>·<c>passives.json</c>·<c>rules/hero.json</c> 으로 만든 마법사(쿨다운 감소 0.05 — v0.6 완료 기준),
/// 실제 경험치 곡선, 개발 옵션 <c>--level</c>·<c>--weapons</c>·<c>--passives</c>, <c>HeroControl</c>(이동 의도·무리 끈적임·재생·클립), 그래픽 없는 시나리오(해골 50 + 매직 미사일 Lv8 60초).
/// WP7 로더 전이라 실제 파일은 형만 읽고 참조는 손으로 잇는다(<see cref="RealData"/>).
/// </summary>
public class HeroTests
{
    private static T Rule<T>(string key) =>
        RealData.Read("rules/hero.json")["rules"]![key]!.Deserialize<T>(TestJson.Options) ?? throw new JsonException($"rules.{key} null");

    /// <summary>실제 마법사 직업·특성·주인공 규칙·경험치 곡선을 합성 정의(<see cref="CombatKit"/>)에 끼운다. 시작 무기는 같은 ID 의 합성 매직 미사일.</summary>
    private static GameDefs RealSorcererDefs()
    {
        var sorc = Assert.Single(RealData.Kind<HeroClassDef>("heroes/sorcerer.json", "hero_classes"));
        var realPassives = RealData.Kind<PassiveDef>("passives.json", "passives");
        var trait = realPassives.Single(p => p.Trait);
        var mm = CombatKit.MagicMissile() with { Id = "magic_missile" };
        var basic = CombatKit.Defs(startWeapons: [mm]);
        var set = basic.SpriteSets[0] with { Id = sorc.Sprites.Id };
        sorc.Sprites.Bind(set);
        foreach (var r in sorc.StartWeapons) r.Bind(mm);
        foreach (var r in sorc.Traits) r.Bind(realPassives.Single(p => p.Id == r.Id));
        foreach (var r in trait.Classes) r.Bind(sorc);
        var hero = Rule<HeroRulesDef>("hero");
        hero.Class.Bind(sorc);
        return new GameDefs
        {
            Rules = basic.Rules with { Hero = hero, XpCurve = Rule<List<XpCurveSegment>>("xp_curve") },
            Sprites = basic.Sprites, SpriteSets = new DefTable<SpriteSetDef>([.. basic.SpriteSets.All, set]), Projectiles = basic.Projectiles,
            Weapons = new DefTable<WeaponDef>([.. basic.Weapons.All, mm]),
            Passives = new DefTable<PassiveDef>([.. basic.Passives.All.Where(p => !p.Trait), trait]),
            HeroClasses = new DefTable<HeroClassDef>([sorc]),
            Tilesets = basic.Tilesets, Maps = basic.Maps, Stages = basic.Stages, Enemies = basic.Enemies, Rewards = basic.Rewards, Pickups = basic.Pickups,
        };
    }

    [Fact]
    public void RealSorcerer_CooldownPct005_BaseStats_StartWeapon_TraitNoSlot()
    {
        var t = CombatKit.World(RealSorcererDefs());
        var h = t.Hero;
        Assert.Equal("sorcerer", t.World.HeroClass!.Id);
        Assert.Equal(0.05f, h.Stats[StatKey.CooldownPct], 5);
        Assert.Equal(100f, h.MaxHp);
        Assert.Equal(100f, h.Hp);
        Assert.Equal(95f, h.Stats[StatKey.MoveSpeed]);
        Assert.Equal(80f, h.Stats[StatKey.PickupRadius]);   // v0.9 기획 4.1(60 → 80)
        Assert.Equal("magic_missile", h.Weapons[0].Def!.Id);
        Assert.Equal(1, h.Weapons[0].Level);
        for (var i = 0; i < PassiveSlots.SlotCount; i++) Assert.True(h.Passives[i].IsEmpty);   // 특성은 칸 밖
        Assert.Equal(4, h.WeaponSlotsOpen);

        // 실제 경험치 곡선(기획 4.3 v0.8.1)
        var curve = t.World.Defs.Rules.XpCurve;
        Assert.Equal([5, 15, 185, 215, 785, 825], new[] { 1, 2, 19, 20, 39, 40 }.Select(l => XpCurve.Need(curve, l)));
    }

    [Fact]
    public void RunSetup_Level_Weapons_Passives()
    {
        var t = CombatKit.World();
        var w = t.World;
        var setup = new HeroRunSetup();
        setup.Apply(w, new RunOptions
        {
            Level = 7,
            Weapons = [new(CombatKit.Fb, 3), new(CombatKit.Mm, 99)],
            Passives = [new(CombatKit.Vitality, 2), new(CombatKit.Power, 1)],
        });
        Assert.Equal(7, w.Progression!.Level);
        Assert.Equal(0, w.Progression.Xp);
        Assert.Equal((CombatKit.Fb, 3), (w.Hero.Weapons[0].Def!.Id, w.Hero.Weapons[0].Level));
        Assert.Equal((CombatKit.Mm, 8), (w.Hero.Weapons[1].Def!.Id, w.Hero.Weapons[1].Level));   // 최대 레벨로 자름
        Assert.True(w.Hero.Weapons[1].MaxedAtTick >= 0);
        Assert.Equal(140f, w.Hero.MaxHp);                                                       // 100 + 활력 20 × 2
        Assert.Equal(w.Hero.MaxHp, w.Hero.Hp);
        Assert.Equal(0.1f, w.Hero.Stats[StatKey.DamagePct], 5);
        Assert.Equal(0.05f, w.Hero.Stats[StatKey.CooldownPct], 5);                              // 특성은 그대로

        Assert.Throws<ArgumentException>(() => setup.Apply(w, new RunOptions { Passives = [new(CombatKit.Trait, 1)] }));
        Assert.Throws<ArgumentException>(() => setup.Apply(w, new RunOptions
        {
            Weapons = [new(CombatKit.Mm, 1), new(CombatKit.Fb, 1), new(CombatKit.Orbit, 1), new(CombatKit.Mm, 2), new(CombatKit.Fb, 2)],   // 열린 칸 4
        }));
    }

    /// <summary>
    /// 완료 기준 시나리오(11.1 WP3): 해골 50 마리 + 매직 미사일 Lv8 로 60초 → 처치·경험치·레벨업 대기가 생김. 공간 질의는 TestKit 전수(WP2 해시가 들어오면 그것으로도).
    /// 감독·적 이동이 WP4 라 해골은 서 있고, 보석이 줍는 반경 안에 떨어지게 주인공 둘레 30~55px 에 둔다. 선택 창은 건너뛰기로 넘긴다.
    /// </summary>
    [Fact, Trait("Category", TestCategories.Scenario)]
    public void Scenario_50Skeletons_MagicMissileLv8_60s_KillsXpLevelUps()
    {
        TestCategories.RequireEnabled(TestCategories.Scenario);
        var t = CombatKit.World(seed: 3);
        var w = t.World;
        CombatKit.SetWeapon(w, 0, CombatKit.Mm, 8);
        var rng = new Random(5);
        for (var i = 0; i < 50; i++)
        {
            var a = i * MathF.Tau / 50f;
            var r = 30f + (float)rng.NextDouble() * 25f;
            t.AddEnemy(t.Hero.Pos + new Vector2(MathF.Cos(a), MathF.Sin(a)) * r, CombatKit.Skeleton);
        }
        var levelUps = 0;
        for (var tick = 0; tick < 60 * 60; tick++)
        {
            while (w.AwaitingChoice == ChoiceKind.LevelUp)
            {
                levelUps++;
                w.Progression!.Skip();
            }
            t.Step();
        }
        Assert.Equal(50, w.RunStats.Kills);
        Assert.Equal(0, w.Enemies.LiveCount);
        Assert.Equal(50, w.RunStats.XpCollected);
        Assert.Equal(3, levelUps);                                  // 5 + 15 + 25 = 45 ≤ 50 < 80
        Assert.Equal(4, w.Progression!.Level);
        Assert.True(w.RunStats.DamageByWeapon[0] >= 50 * 20f);
    }

    [Fact]
    public void HeroControl_Intent_Facing_CrowdSlow_Regen_Walk()
    {
        var t = CombatKit.World();
        var w = t.World;
        CombatKit.ClearWeapons(w);
        w.Command = new PlayerCommand(new Vector2(0f, 2f));
        t.Step();
        Assert.Equal(new Vector2(0f, 1f), t.Hero.Facing);
        Assert.Equal(95f, t.Hero.MoveSpeed);
        Assert.Equal(ClipKind.Walk, t.Hero.Anim.Clip);

        t.Hero.TouchingCount = 2;
        t.Step();
        Assert.Equal(95f, t.Hero.MoveSpeed);
        t.Hero.TouchingCount = 3;                                   // 닿은 적 3 이상 → ×0.6(기획 4.1)
        t.Step();
        Assert.Equal(95f * 0.6f, t.Hero.MoveSpeed, 3);

        w.Command = PlayerCommand.None;
        t.Hero.TouchingCount = 0;
        t.Step();
        Assert.Equal(ClipKind.Idle, t.Hero.Anim.Clip);

        // 재생: 초당 regen 만큼, 최대 체력을 넘지 않음
        w.Combat.Mods[(int)StatKey.Regen] = 3f;
        w.Combat.StatsDirty = true;
        t.Hero.Hp = 50f;
        t.Step(60);
        Assert.Equal(53f, t.Hero.Hp, 2);
        t.Hero.Hp = t.Hero.MaxHp - 0.01f;
        t.Step(10);
        Assert.Equal(t.Hero.MaxHp, t.Hero.Hp);
    }
}
