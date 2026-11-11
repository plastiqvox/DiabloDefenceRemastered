using DiabloDefenceRemastered.Core.Sim.Progression;
using DiabloDefenceRemastered.Tests.Combat;
using DiabloDefenceRemastered.Tests.Shops;

namespace DiabloDefenceRemastered.Tests.Heroes;

/// <summary>
/// 10.1 <c>Hero</c> 행(v0.9.1 V9 WP3 단계 2 — 설계서 6.19 "물약 칸과 서서히 회복", 기획 11.5): <c>CommandButtons.Drink</c> 한 병 = 최대 체력 40% 를 3초(빠른 물약 2초·1.2초)에 고르게,
/// 거절 넷(상인 창·회복 중·물약 0·체력 가득 → <c>PotionDenied</c>, 물약 그대로), 판 시간이 멈추면 회복도 멈춤.
/// </summary>
public class PotionDrinkTests
{
    private static readonly PlayerCommand Drink = new(Vector2.Zero, CommandButtons.Drink);

    private static TestWorld World(float hp, int potions)
    {
        var t = CombatKit.World(ShopKit.Defs(), configure: b => b.WithPresentation());
        CombatKit.ClearWeapons(t.World);                                                 // 쏘지 않게(적 없음 — 피해·경험치 없음)
        t.Hero.Hp = hp;
        t.Hero.Potions = potions;
        return t;
    }

    [Fact]
    public void Drink_OnePotion_HealsExactly40PctOver180Ticks_CappedAtMax()
    {
        var t = World(10f, 2);
        var w = t.World;
        Assert.Equal(100f, w.Hero.MaxHp);
        Assert.Equal(StageStep.Stepped, t.AppTick(Drink));
        Assert.Equal(1, w.Hero.Potions);
        Assert.Equal(1, w.RunStats.PotionsDrunk);
        var drink = Assert.Single(ShopKit.Cues(w, CueKind.PotionDrink));
        Assert.Null(drink.Subject);
        Assert.Equal(179, w.Hero.Heal.TicksLeft);                                       // 마신 틱부터 회복(3초 = 180틱)
        for (var i = 1; i < 179; i++) t.AppTick();
        Assert.True(w.Hero.Heal.Active);                                                // 179틱째 — 아직 하나 남음
        t.AppTick();
        Assert.False(w.Hero.Heal.Active);
        Assert.Equal(50f, w.Hero.Hp, 3);                                                 // 합 = 정확히 40(마지막 틱에 나머지)
        for (var i = 0; i < 30; i++) t.AppTick();
        Assert.Equal(50f, w.Hero.Hp, 3);

        var full = World(90f, 1);
        full.AppTick(Drink);
        for (var i = 0; i < 200; i++) full.AppTick();
        Assert.Equal(100f, full.Hero.Hp);                                               // 최대에서 자름
        Assert.Equal(0, full.Hero.Potions);
    }

    [Fact]
    public void Denied_Four_ShopWindow_Healing_NoPotion_FullHp_PotionsKept()
    {
        // 회복 중
        var a = World(10f, 2);
        a.AppTick(Drink);
        a.AppTick(Drink);
        Assert.Equal(1, a.Hero.Potions);
        Assert.Single(ShopKit.Cues(a.World, CueKind.PotionDenied));
        Assert.Equal(1, a.World.RunStats.PotionsDrunk);

        // 물약 0
        var b = World(10f, 0);
        b.AppTick(Drink);
        Assert.Single(ShopKit.Cues(b.World, CueKind.PotionDenied));
        Assert.False(b.Hero.Heal.Active);

        // 체력 가득
        var c = World(100f, 2);
        c.AppTick(Drink);
        Assert.Single(ShopKit.Cues(c.World, CueKind.PotionDenied));
        Assert.Equal(2, c.Hero.Potions);
        Assert.Null(Assert.Single(ShopKit.Cues(c.World, CueKind.PotionDenied)).Subject);

        // 상인 창
        var d = ShopKit.Room(merchantId: TestDefs.HealerId);
        d.Hero.Potions = 2;
        d.Hero.Hp = 30f;
        ShopKit.Touch(d);
        d.AppTick(Drink);
        Assert.Single(ShopKit.Cues(d.World, CueKind.PotionDenied));
        Assert.Equal((2, 30f, false), (d.Hero.Potions, d.Hero.Hp, d.Hero.Heal.Active));
        Assert.Empty(ShopKit.Cues(d.World, CueKind.PotionDrink));
    }

    [Fact]
    public void ChoiceWait_HealDoesNotTick_ResumesAfter()
    {
        var t = World(10f, 1);
        var w = t.World;
        t.AppTick(Drink);
        ((ProgressionService)w.Progression!).AddXp(5);                                  // 레벨업 대기 → 이번 틱 끝에 창
        t.AppTick();
        Assert.Equal(ChoiceKind.LevelUp, w.AwaitingChoice);
        var (ticks, hp, tick) = (w.Hero.Heal.TicksLeft, w.Hero.Hp, w.Tick);
        for (var i = 0; i < 120; i++) Assert.Equal(StageStep.AwaitingChoice, t.AppTick(Drink));
        Assert.Equal((ticks, hp, tick), (w.Hero.Heal.TicksLeft, w.Hero.Hp, w.Tick));   // 판 시간이 멈추면 같이 멈춤
        Assert.Equal(0, w.Hero.Potions);
        w.Progression.Skip();
        t.AppTick();
        Assert.Equal(ticks - 1, w.Hero.Heal.TicksLeft);
    }

    [Fact]
    public void SpeedLevels_Durations_180_120_72Ticks()
    {
        foreach (var (level, ticks) in new[] { (0, 180), (1, 120), (2, 72) })
        {
            var t = World(10f, 1);
            t.Hero.PotionSpeedLevel = level;
            t.AppTick(Drink);
            Assert.Equal(ticks - 1, t.Hero.Heal.TicksLeft);
            for (var i = 1; i < ticks; i++) t.AppTick();
            Assert.False(t.Hero.Heal.Active);
            Assert.Equal(50f, t.Hero.Hp, 3);
        }
    }

    [Fact]
    public void Heal_NotInterruptedByDamage()
    {
        var t = World(60f, 1);
        var w = t.World;
        t.AppTick(Drink);
        w.Damage.ToHero(default, 5f);
        t.AppTick();
        Assert.True(w.Hero.Heal.Active);                                                // 맞아도 끊기지 않음
        for (var i = 0; i < 200; i++) t.AppTick();
        Assert.False(w.Hero.Heal.Active);
        Assert.True(w.Hero.Hp > 90f);
    }
}
