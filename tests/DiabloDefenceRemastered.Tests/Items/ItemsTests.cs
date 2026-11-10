namespace DiabloDefenceRemastered.Tests.Items;

/// <summary>
/// 10.1 <c>Items</c> 행(v0.9 WP1g #4·#5 — 설계서 6.19): <c>ItemRoller</c>(마법 비율·접사 둘·같은 쪽 둘 없음·반지 늘 마법·5 단위 값·범위 끝 값 배율·같은 씨앗 같은 결과),
/// <c>EquipStats</c>(방어 상한 15·지팡이 + 방패 합산·무게 이동 하한·합친 뒤 자름·최대 체력 바뀜), <c>HeroLook.Key</c>(아홉 조합 + 지팡이·방패 둘 다).
/// </summary>
public class ItemsTests
{
    private static readonly GameDefs Defs = TestDefs.Basic();
    private static EquipmentRulesDef Rules => Defs.Rules.Equipment!;
    private static ItemBaseDef Base(string id) => Defs.ItemBases.Get(id);
    private static RandomStream Rng(int seed) => new(RunSeeds.Shop(0xC0FFEEul, 1, 4, 0, seed));

    // ── ItemRoller ──────────────────────────────────────────────────

    [Theory]
    [InlineData(1, 0.35)]
    [InlineData(2, 0.60)]
    public void Roll_MagicRatioPerAct_TwoAffix25_NoSameSideTwo(int act, double magic)
    {
        var cap = Base("t_cap");   // 투구: 접두사(t_af_fine)·접미사(t_af_fox, 액트 2 는 t_af_jaguar 도) 둘 다 있다
        int magicCount = 0, two = 0;
        for (var s = 0; s < 1000; s++)
        {
            var item = ItemRoller.Roll(cap, act, Rules, Defs, Rng(s));
            Assert.Equal(0, item.Price % 5);
            if (!item.Magic) continue;
            magicCount++;
            if (item.Prefix is { } p) Assert.True(Defs.ItemAffixes.Get(p.Id).Prefix);      // 같은 쪽 둘은 구조로 없다
            if (item.Suffix is { } x) Assert.False(Defs.ItemAffixes.Get(x.Id).Prefix);
            if (item.Prefix is not null && item.Suffix is not null) two++;
        }
        Assert.InRange(magicCount / 1000.0, magic - 0.03, magic + 0.03);
        Assert.InRange(two / (double)magicCount, 0.25 - 0.03 * 1000 / magicCount, 0.25 + 0.03 * 1000 / magicCount);
    }

    [Fact]
    public void Roll_TwoAffixRatio_OnManyMagicRolls()
    {
        var cap = Base("t_cap");
        var two = 0;
        for (var s = 0; s < 1000; s++)
        {
            var item = ItemRoller.RollMagic(cap, 1, Rules, Defs, Rng(s), new HashSet<string>());
            Assert.True(item.Magic);
            if (item.Prefix is not null && item.Suffix is not null) two++;
        }
        Assert.InRange(two / 1000.0, 0.22, 0.28);
    }

    [Fact]
    public void Roll_RingAlwaysMagic_SameSeedSameResult_StageTwoOnlyInAct2()
    {
        var ring = Base("t_ring");
        for (var s = 0; s < 300; s++)
        {
            var a = ItemRoller.Roll(ring, 1, Rules, Defs, Rng(s));
            Assert.True(a.Magic);
            Assert.Equal(a, ItemRoller.Roll(ring, 1, Rules, Defs, Rng(s)));         // 같은 씨앗 두 번 같은 결과
            Assert.NotEqual("t_af_jaguar", a.Suffix?.Id);                            // 단계 2 접사는 액트 1 에 없다
        }
        Assert.Contains(Enumerable.Range(0, 300), s => ItemRoller.Roll(ring, 2, Rules, Defs, Rng(s)).Suffix?.Id == "t_af_jaguar");
    }

    [Fact]
    public void Roll_DrawCountFixed_SevenPerRoll()
    {
        // 갈래(보통·마법·접사 하나·둘)와 상관없이 늘 일곱 — 같은 흐름의 다음 굴림이 밀리지 않는다
        for (var s = 0; s < 200; s++)
        {
            var rng = Rng(s);
            ItemRoller.Roll(Base("t_cap"), 1, Rules, Defs, rng);
            var reference = Rng(s);
            for (var k = 0; k < ItemRoller.DrawsPerRoll; k++) reference.NextUInt();
            Assert.Equal(reference.GetState(), rng.GetState());
        }
    }

    [Fact]
    public void Price_RangeEnds_Are075And125_OfAffixPrice_Rounded5()
    {
        var fine = Defs.ItemAffixes.Get("t_af_fine");   // 방어 1~2, 값 더함 100
        var free = Base("t_cap") with { Price = 0 };
        Assert.Equal(75, ItemRoller.PriceOf(free, new RolledAffix(fine.Id, 1f), null, Rules, Defs));
        Assert.Equal(125, ItemRoller.PriceOf(free, new RolledAffix(fine.Id, 2f), null, Rules, Defs));
        Assert.Equal(100, ItemRoller.PriceOf(free, null, new RolledAffix("t_af_fox", 10f), Rules, Defs));   // 고정 접사 = 가운데(× 1.0)
        Assert.Equal(40 + 75 + 100, ItemRoller.PriceOf(Base("t_cap"), new RolledAffix(fine.Id, 1f), new RolledAffix("t_af_fox", 10f), Rules, Defs));
        var jagged = Defs.ItemAffixes.Get("t_af_jagged");   // 0.03~0.05 간격 0.01 → 칸 셋
        Assert.Equal(3, ItemRoller.Steps(jagged));
        Assert.Equal(0.05f, ItemRoller.ValueAt(jagged, 2));
        Assert.Equal(1f, ItemRoller.Position(jagged, 0.05f));
        // 굴린 값은 늘 범위 안·간격 위
        for (var s = 0; s < 500; s++)
        {
            var item = ItemRoller.RollMagic(Base("t_staff"), 1, Rules, Defs, Rng(s), new HashSet<string>());
            if (item.Prefix is { Id: "t_af_jagged" } p) Assert.Contains(p.Value, new[] { 0.03f, 0.04f, 0.05f });
        }
    }

    [Fact]
    public void RollMagic_AvoidsGivenAffixes()
    {
        var ring = Base("t_ring");
        var avoid = new HashSet<string> { "t_af_jagged", "t_af_fox" };
        for (var s = 0; s < 300; s++)
        {
            var item = ItemRoller.RollMagic(ring, 1, Rules, Defs, Rng(s), avoid);
            Assert.True(item.Magic);
            Assert.False(avoid.Contains(item.Prefix?.Id ?? ""));
            Assert.False(avoid.Contains(item.Suffix?.Id ?? ""));
        }
    }

    // ── EquipStats ─────────────────────────────────────────────────

    private static EquipItem?[] Equip(params (EquipSlot Slot, EquipItem Item)[] items)
    {
        var e = new EquipItem?[EquipSlots.Count];
        foreach (var (slot, item) in items) e[(int)slot] = item;
        return e;
    }

    private static EquipItem Plain(string id) => new(id, null, null, Base(id).Price);
    private static EquipItem Fine(string id, float v) => new(id, new RolledAffix("t_af_fine", v), null, 0);

    [Fact]
    public void ArmorCap15_PlateThreePlusFine_Clamps_PlateAloneDoesNot()
    {
        var plate = Equip((EquipSlot.Head, Plain("t_full_helm")), (EquipSlot.Body, Plain("t_breast_plate")), (EquipSlot.Shield, Plain("t_kite_shield")));
        var none = new StatBlock();
        Assert.Equal(13f, EquipStats.Sum(plate, Defs, Rules)[StatKey.Armor]);
        Assert.Equal(13f, EquipStats.Total(none, plate, Defs, Rules)[StatKey.Armor]);          // 판금 셋만은 상한 안(v0.9.1 — v0.9 의 10 이면 잘렸다)
        foreach (var v in new[] { 1f, 2f })
        {
            var fine = Equip((EquipSlot.Head, Fine("t_full_helm", v)), (EquipSlot.Body, Fine("t_breast_plate", v)), (EquipSlot.Shield, Fine("t_kite_shield", v)));
            Assert.Equal(13f + 3 * v, EquipStats.Sum(fine, Defs, Rules)[StatKey.Armor]);       // 16 · 19
            Assert.Equal(15f, EquipStats.Total(none, fine, Defs, Rules)[StatKey.Armor]);
        }
        var stoneSkin = new StatBlock { [StatKey.Armor] = 5f };                                    // 다른 출처와 합친 뒤 자름
        Assert.Equal(15f, EquipStats.Total(stoneSkin, plate, Defs, Rules)[StatKey.Armor]);
        Assert.Equal(5f, stoneSkin[StatKey.Armor]);                                                // 다른 출처 블록은 바꾸지 않음
    }

    [Fact]
    public void StaffAndShield_BothCounted_NoTwoHandRule()
    {
        var e = Equip((EquipSlot.Staff, Plain("t_staff")), (EquipSlot.Shield, Plain("t_buckler")));
        var s = EquipStats.Sum(e, Defs, Rules);
        Assert.Equal(0.03f, s[StatKey.CooldownPct], 5);
        Assert.Equal(1f, s[StatKey.Armor]);
    }

    [Fact]
    public void WeightMove_FloorMinus15_AndSwiftnessAddedBeforeClamp()
    {
        var plate = Equip((EquipSlot.Head, Plain("t_full_helm")), (EquipSlot.Body, Plain("t_breast_plate")), (EquipSlot.Shield, Plain("t_kite_shield")));
        Assert.Equal(-0.18f, EquipStats.Sum(plate, Defs, Rules)[StatKey.MovePct], 5);            // −10 −4 −4
        Assert.Equal(-0.15f, EquipStats.Total(new StatBlock(), plate, Defs, Rules)[StatKey.MovePct], 5);

        // of Swiftness + 판금: 합친 뒤 자름(−18 + 5 = −13) — 하한 뒤에 더하기(−15 + 5 = −10)가 아니다
        var swift = Equip((EquipSlot.Head, Plain("t_full_helm")), (EquipSlot.Body, Plain("t_breast_plate")),
                          (EquipSlot.Shield, new EquipItem("t_kite_shield", null, new RolledAffix("t_af_swift", 0.05f), 0)));
        Assert.Equal(-0.13f, EquipStats.Total(new StatBlock(), swift, Defs, Rules)[StatKey.MovePct], 5);
        Assert.Equal(-0.02f, EquipStats.Sum(Equip((EquipSlot.Head, Plain("t_helm"))), Defs, Rules)[StatKey.MovePct], 5);
    }

    [Fact]
    public void RebaseHp_UpAddsDelta_DownClampsToNewMax()
    {
        Assert.Equal(80f, EquipStats.RebaseHp(50f, 100f, 130f));    // 최대 +30 → 지금 +30
        Assert.Equal(110f, EquipStats.RebaseHp(125f, 130f, 110f));  // 바꿔서 −20 → 새 최대에서 자름
        Assert.Equal(60f, EquipStats.RebaseHp(60f, 130f, 110f));    // 내려가도 지금 체력은 빼지 않음
        Assert.Equal(1f, EquipStats.RebaseHp(0.5f, 100f, 100f));    // 살아 있으면 1 아래로 안 내림
        Assert.Equal(0f, EquipStats.RebaseHp(0f, 100f, 130f));      // 죽음은 그대로
    }

    // ── HeroLook ───────────────────────────────────────────────────

    [Theory]
    [InlineData(null, null, null, "ln")]
    [InlineData("t_quilted", null, null, "ln")]
    [InlineData("t_ring_mail", null, null, "mn")]
    [InlineData("t_breast_plate", null, null, "hn")]
    [InlineData("t_quilted", null, "t_buckler", "lu")]
    [InlineData("t_ring_mail", null, "t_buckler", "mu")]
    [InlineData("t_breast_plate", null, "t_kite_shield", "hu")]
    [InlineData(null, "t_staff", null, "lt")]
    [InlineData("t_ring_mail", "t_staff", null, "mt")]
    [InlineData("t_breast_plate", "t_staff", null, "ht")]
    [InlineData("t_breast_plate", "t_staff", "t_kite_shield", "ht")]   // 지팡이 + 방패 = 지팡이 벌(v0.9.1 열린 질문 53)
    [InlineData(null, "t_staff", "t_buckler", "lt")]
    public void HeroLook_Key_WeightAndHeld(string? body, string? staff, string? shield, string key)
    {
        var e = new EquipItem?[EquipSlots.Count];
        if (body is not null) e[(int)EquipSlot.Body] = Plain(body);
        if (staff is not null) e[(int)EquipSlot.Staff] = Plain(staff);
        if (shield is not null) e[(int)EquipSlot.Shield] = Plain(shield);
        e[(int)EquipSlot.Head] = Plain("t_full_helm");                // 투구 무게는 겉모습에 들지 않는다
        Assert.Equal(key, HeroLook.Key(e, Defs));
    }

    [Fact]
    public void HeroLook_NineKeys_AllDistinct_StartIsLn()
    {
        var keys = new HashSet<string>();
        foreach (var w in new[] { ArmorWeight.Light, ArmorWeight.Medium, ArmorWeight.Heavy })
            foreach (var (staff, shield) in new[] { (false, false), (false, true), (true, false) })
                keys.Add(HeroLook.Key(w, staff, shield));
        Assert.Equal(9, keys.Count);
        Assert.Equal("ln", HeroLook.Start);
        Assert.Equal("ln", HeroLook.Key(new EquipItem?[EquipSlots.Count], Defs));
        Assert.Equal("ln", HeroLook.Key([], Defs));
    }
}
