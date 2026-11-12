using DiabloDefenceRemastered.Core.Sim.Progression;
using DiabloDefenceRemastered.Tests.Data;
using DiabloDefenceRemastered.Tests.Heroes;
using static DiabloDefenceRemastered.Tests.Heroes.BlinkKit;

namespace DiabloDefenceRemastered.Tests.Progression;

/// <summary>
/// 패시브 레벨별 효과 <c>levels</c>(보스 묶음 B — 설계서 6.20 ⑯, 기획 v0.9.5 6절 순간이동 숙련): 레벨 L = 앞 L 개의 합, <c>per_level</c> 과 함께면 오류·길이 ≠ <c>max_level</c> 오류,
/// 카드 줄 = 다음 레벨의 항목, Lv2 를 얻는 순간 늘어난 칸이 참, Lv4 충전 5.28s, Lv5 시전 0(누른 틱에 이동).
/// </summary>
public sealed class PassiveLevelsTests
{
    [Fact]
    public void SumsUpToLevel()
    {
        var t = World();
        var s = t.Hero.Stats;
        foreach (var (level, cast, charges, recharge) in new[] { (1, -0.05f, 0f, 0f), (2, -0.05f, 1f, 0f), (3, -0.10f, 1f, 0f), (4, -0.10f, 1f, -0.12f), (5, -0.15f, 1f, -0.12f) })
        {
            SetMastery(t.World, level);
            Assert.Equal(cast, s[StatKey.BlinkCast], 5);
            Assert.Equal(charges, s[StatKey.BlinkCharges], 5);
            Assert.Equal(recharge, s[StatKey.BlinkRechargePct], 5);
        }
        Assert.Equal(3, Core.Sim.Heroes.HeroBlink.MaxCharges(t.World));
    }

    [Fact]
    public void PerLevelAndLevels_Error()
    {
        var f = new LoaderFixture();
        f.Files["passives.json"] = LoaderFixture.Obj("""
            { "passives": [ { "id": "bm", "max_level": 2, "per_level": { "armor": 1 }, "levels": [ { "blink_cast": -0.05 }, { "blink_charges": 1 } ] } ] }
            """);
        var r = f.Load();
        var hit = Assert.Single(LoaderFixture.ErrorsOf(r), e => e.Message.Contains("levels 와 per_level 을 함께", StringComparison.Ordinal));
        Assert.Equal("passives.json", hit.File);
        Assert.EndsWith(".levels", hit.JsonPath);

        var ok = new LoaderFixture();
        ok.Files["passives.json"] = LoaderFixture.Obj("""
            { "passives": [ { "id": "bm", "max_level": 2, "levels": [ { "blink_cast": -0.05 }, { "blink_charges": 1 } ] } ] }
            """);
        var good = ok.Load();
        Assert.False(good.HasErrors, string.Join("\n", good.Errors));
    }

    [Fact]
    public void LengthMismatch_Error()
    {
        var f = new LoaderFixture();
        f.Files["passives.json"] = LoaderFixture.Obj("""
            { "passives": [ { "id": "bm", "max_level": 5, "levels": [ { "blink_cast": -0.05 }, { "blink_charges": 1 } ] } ] }
            """);
        var r = f.Load();
        Assert.Single(LoaderFixture.ErrorsOf(r), e => e.Message.Contains("levels 길이 2 ≠ max_level 5", StringComparison.Ordinal));

        // 모르는 키는 지금처럼 로딩 오류(StatKey 밖)
        var g = new LoaderFixture();
        g.Files["passives.json"] = LoaderFixture.Obj("""{ "passives": [ { "id": "bm", "max_level": 1, "levels": [ { "blink_range": 20 } ] } ] }""");
        Assert.True(g.Load().HasErrors);
    }

    [Fact]
    public void CardLine_IsNextLevel()
    {
        var t = World();
        var w = t.World;
        Assert.Equal(["순간이동 시전 -0.05s"], Build.Card(w, new Candidate(CardKind.NewPassive, Mastery, 1f)).Lines);
        foreach (var (level, line) in new[] { (1, "순간이동 칸 +1"), (2, "순간이동 시전 -0.05s"), (3, "순간이동 충전 -12%"), (4, "순간이동 시전 -0.05s") })
        {
            SetMastery(w, level);
            var card = Build.Card(w, new Candidate(CardKind.PassiveUpgrade, Mastery, 1f));
            Assert.Equal((level, level + 1), (card.FromLevel, card.ToLevel));
            Assert.Equal([line], card.Lines);
        }
        SetMastery(w, 5);
        Assert.Empty(Build.Card(w, new Candidate(CardKind.PassiveUpgrade, Mastery, 1f)).Lines);   // 최대 — 다음 레벨 없음
    }

    [Fact]
    public void Lv2_GrantsChargeImmediately()
    {
        var t = World();
        var w = t.World;
        SetMastery(w, 1);
        t.AppTick(Blink(new Vector2(1f, 0f)));
        for (var i = 0; i < 60; i++) t.AppTick();
        Assert.Equal(1, t.Hero.BlinkCharges);
        var recharge = t.Hero.BlinkRecharge;

        Build.Apply(w, Build.Card(w, new Candidate(CardKind.PassiveUpgrade, Mastery, 1f)));   // 카드로 Lv2 — 능력치는 다음 판 틱 HeroControl 이 다시 계산
        Assert.Equal(2, t.Hero.Passives[0].Level);
        t.AppTick();
        Assert.Equal(3, Core.Sim.Heroes.HeroBlink.MaxCharges(w));
        Assert.Equal(2, t.Hero.BlinkCharges);                                                    // 늘어난 칸 하나가 바로 참(쓴 칸은 그대로 충전 중)
        Assert.True(t.Hero.BlinkRecharge > recharge);
    }

    [Fact]
    public void Lv4_Recharge528()
    {
        var t = World();
        SetMastery(t.World, 4);
        Assert.Equal(5.28f, Core.Sim.Heroes.HeroBlink.RechargeTime(t.Hero, t.World.Defs.Rules.Hero!.Blink!), 4);
        t.AppTick(Blink(new Vector2(1f, 0f)));
        Assert.Equal(2, t.Hero.BlinkCharges);
        for (var i = 0; i < 316; i++) t.AppTick();
        Assert.Equal(2, t.Hero.BlinkCharges);
        t.AppTick();                                                                             // 5.28s = 316.8틱 → 317틱째
        Assert.Equal(3, t.Hero.BlinkCharges);
    }

    [Fact]
    public void Lv5_CastZero_MovesSameTick()
    {
        var t = World();
        SetMastery(t.World, 5);
        Assert.Equal(0f, Core.Sim.Heroes.HeroBlink.CastTime(t.Hero, t.World.Defs.Rules.Hero!.Blink!));
        var start = t.Hero.Pos;
        t.AppTick(Blink(new Vector2(0f, 1f)));
        Assert.Equal(start + new Vector2(0f, 160f), t.Hero.Pos);
        Assert.Equal(0f, t.Hero.BlinkCast);
        Assert.Single(Shops.ShopKit.Cues(t.World, CueKind.Blink));

        // Lv1(시전 0.10 = 6틱)
        var u = World();
        SetMastery(u.World, 1);
        var s = u.Hero.Pos;
        u.AppTick(Blink(new Vector2(0f, 1f)));
        for (var i = 0; i < 5; i++) u.AppTick();
        Assert.Equal(s, u.Hero.Pos);
        u.AppTick();
        Assert.Equal(s + new Vector2(0f, 160f), u.Hero.Pos);
    }
}
