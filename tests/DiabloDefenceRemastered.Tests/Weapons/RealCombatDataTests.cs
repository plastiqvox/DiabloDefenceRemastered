using System.Text.Json;
using System.Text.Json.Nodes;
using DiabloDefenceRemastered.Core.Sim.Weapons;
using DiabloDefenceRemastered.Tests.Combat;
using DiabloDefenceRemastered.Tests.Data;

namespace DiabloDefenceRemastered.Tests.Weapons;

/// <summary>
/// WP3 이 쓴 실제 <c>Data/</c>(무기 셋·패시브·획득물·드롭 표·문 보상·주인공 규칙·직업)가 형대로 읽히고(모르는 속성 오류), 서로 가리키는 ID 가 있고,
/// 실제 무기 레벨 표가 13.5-2 검사(<see cref="WeaponTableValidator"/>)를 통과하는지. WP7 로더 전이라 참조 연결은 ID 대조로 대신한다.
/// </summary>
public class RealCombatDataTests
{
    private static readonly string[] WeaponFiles = ["weapons/magic_missile.json", "weapons/fireball.json", "weapons/arcane_orbit.json"];

    private static readonly string[] PartKeys = ["targeting", "on_hit", "on_explode", "on_crit"];

    /// <summary>
    /// 실제 무기(부품 칸 <c>targeting</c>·<c>on_*</c> 은 부품 등록부가 읽는 형이라 STJ 로는 못 읽음 — 떼어 내고 읽은 뒤 조준 종류·반경만 따로 본다).
    /// </summary>
    private static List<WeaponDef> Weapons()
    {
        var list = new List<WeaponDef>();
        foreach (var f in WeaponFiles)
            foreach (var n in (JsonArray)RealData.Read(f)["weapons"]!)
            {
                var o = (JsonObject)n!.DeepClone();
                var targeting = o["targeting"]!;
                var type = (string)targeting["type"]!;
                Assert.Contains(type, Targetings.Ids);
                if (type != "none") Assert.True((float)targeting["radius"]! > 0f, $"{o["id"]}: 조준 반경");
                foreach (var k in PartKeys) o.Remove(k);
                list.Add(o.Deserialize<WeaponDef>(TestJson.Options)!);
            }
        return list;
    }

    [Fact]
    public void RealWeapons_PassTableValidator_ProjectilesExist()
    {
        var weapons = Weapons();
        Assert.Equal(["magic_missile", "fireball", "arcane_orbit"], weapons.Select(w => w.Id));
        // 투사체는 hit_effect 가 이펙트 참조 형(로더가 읽음)이라 ID 만
        var projectiles = WeaponFiles.SelectMany(f => (JsonArray)RealData.Read(f)["projectiles"]!).Select(p => (string)p!["id"]!).ToHashSet();
        foreach (var w in weapons)
        {
            Assert.True(FirePatterns.TryGet(w.Pattern, out _), $"{w.Id}: 패턴 '{w.Pattern}'");
            if (w.Projectile is { } pj) Assert.Contains(pj.Id, projectiles);
        }

        var basic = CombatKit.Defs();
        var defs = new GameDefs
        {
            Rules = basic.Rules, Sprites = basic.Sprites, SpriteSets = basic.SpriteSets, Projectiles = basic.Projectiles,
            Weapons = new DefTable<WeaponDef>(weapons), Passives = basic.Passives, HeroClasses = basic.HeroClasses,
            Tilesets = basic.Tilesets, Maps = basic.Maps, Stages = basic.Stages, Enemies = basic.Enemies, Rewards = basic.Rewards, Pickups = basic.Pickups,
        };
        var ctx = new ValidationContext();
        new WeaponTableValidator().Validate(defs, ctx);
        Assert.Empty(ctx.Errors);
    }

    [Fact]
    public void RealPassives_Pickups_DropTables_Rewards_Hero_ReferencesResolve()
    {
        var passives = RealData.Kind<PassiveDef>("passives.json", "passives");
        Assert.Equal(["vitality", "arcane_power", "field_runner", "trait_arcane_affinity"], passives.Select(p => p.Id));
        Assert.DoesNotContain(passives, p => p.Blessing);                                           // v0.8 E — 축복은 MVP 데이터 밖
        var trait = Assert.Single(passives, p => p.Trait);
        Assert.Equal(0.05f, trait.PerLevel[StatKey.CooldownPct], 5);

        var pickups = RealData.Kind<PickupDef>("pickups.json", "pickups").Select(p => p.Id).ToHashSet();
        foreach (var table in RealData.Kind<DropTableDef>("drop_tables.json", "drop_tables"))
            foreach (var e in table.Entries) Assert.Contains(e.Pickup.Id, pickups);

        var rewards = RealData.Kind<RewardDef>("rewards.json", "rewards");
        // 단계 1 가중표 = 마법서·보물 상자. v0.9(WP1g): 회복 문·상인 셋 정의(방 안 물체 없음 — 가중표에 넣는 것은 단계 2)
        Assert.Equal([RewardKind.WeaponCards, RewardKind.WeaponBundle, RewardKind.HealNextRoom, RewardKind.Merchant, RewardKind.Merchant, RewardKind.Merchant], rewards.Select(r => r.Kind));
        foreach (var r in rewards)
            if (r.Object is { } obj) Assert.Contains(obj.Id, pickups);
            else Assert.True(r.Kind is RewardKind.HealNextRoom or RewardKind.Merchant);

        var sorc = Assert.Single(RealData.Kind<HeroClassDef>("heroes/sorcerer.json", "hero_classes"));
        Assert.Equal(["magic_missile"], sorc.StartWeapons.Select(w => w.Id));
        Assert.Contains(sorc.StartWeapons[0].Id, Weapons().Select(w => w.Id));
        Assert.Equal([trait.Id], sorc.Traits.Select(t => t.Id));
        Assert.Equal(["sorcerer"], trait.Classes.Select(c => c.Id));
    }
}
