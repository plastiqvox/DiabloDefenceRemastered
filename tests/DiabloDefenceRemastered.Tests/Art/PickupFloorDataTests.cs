using DiabloDefenceRemastered.Core.Art;
using DiabloDefenceRemastered.Tests.Data;

namespace DiabloDefenceRemastered.Tests.Art;

/// <summary>
/// 획득물·보상의 원작 그림 잇기(단계 2 wp5 — 설계서 6.9 "보석 그림"·6.19, 기획 11.1·11.4 V8): 실제 <c>pickups.json</c>·<c>rewards.json</c>·<c>merchants/</c> 와
/// 바닥 그림 프레임 고르기(<see cref="PickupArt"/>), 가져오기 표의 <c>items\rock</c> 줄.
/// </summary>
public class PickupFloorDataTests
{
    private static readonly Rgb Green = new(110, 230, 120);
    private static readonly Rgb Red = new(255, 80, 80);

    [Fact]
    public void Gems_GreenRedRockLastFrameTinted_DiamondKept_BlueNoFloor()
    {
        var defs = RealArtDefs.Defs;
        var rock = defs.Sprites.Get("item_rock");
        Assert.Equal(20, rock.Frames);                                                    // 원본 확인(probe cel — 20프레임, 칸 96×96)
        Assert.Equal(new Int2(96, 96), rock.Frame);
        foreach (var (id, tint, size) in new[] { ("gem_green", Green, new Int2(12, 16)), ("gem_red", Red, new Int2(14, 20)) })
        {
            var p = defs.Pickups.Get(id);
            Assert.Equal(PickupKind.Xp, p.Kind);
            var floor = Assert.IsType<IconDef>(p.Floor);
            Assert.Same(rock, floor.Sprite.Def);
            Assert.Equal(rock.Frames, floor.Frame);                                       // 마지막 프레임 하나만(떨어짐 애니 없음)
            Assert.Equal(tint, floor.Tint);
            Assert.Equal("gem", p.Draw.Procedural);                                       // 마름모는 대체·빛 색으로 그대로
            Assert.Equal(size, p.Draw.Size);
            Assert.Equal(tint, p.Draw.Color);
        }
        var blue = defs.Pickups.Get("gem_blue");
        Assert.Null(blue.Floor);
        Assert.Equal("gem", blue.Draw.Procedural);
    }

    [Fact]
    public void GoldPotionSurge_FloorItems_FullDropAnim()
    {
        var defs = RealArtDefs.Defs;
        foreach (var (id, sprite, frames) in new[] { ("gold", "item_goldflip", 10), ("healing_potion", "item_fbttlebr", 16), ("mana_surge", "item_fbttlebb", 16) })
        {
            var floor = defs.Pickups.Get(id).Floor!;
            Assert.Equal(sprite, floor.Sprite.Id);
            Assert.Null(floor.Frame);                                                     // 떨어짐 애니 전부 → 마지막 프레임
            Assert.Equal(frames, floor.Sprite.Def.Frames);
            Assert.NotNull(defs.Pickups.Get(id).Draw.Sprite);                             // exe PNG 대체가 남아 있다
        }
        Assert.Null(defs.Pickups.Get("magnet_rune").Floor);                               // 원작 아이템 없음
        Assert.Null(defs.Pickups.Get("elite_chest").Floor);
    }

    [Fact]
    public void RewardObjects_BookAndChest_PkBook_ChestDropsBookWithPotion()
    {
        var defs = RealArtDefs.Defs;
        Assert.Equal(("obj_book1", 2), (defs.Pickups.Get("obj_tome").Floor!.Sprite.Id, defs.Pickups.Get("obj_tome").Floor!.Frame!.Value));
        Assert.Equal(("obj_chest1", 1), (defs.Pickups.Get("obj_chest").Floor!.Sprite.Id, defs.Pickups.Get("obj_chest").Floor!.Frame!.Value));

        var book = defs.Pickups.Get("pk_book");
        Assert.Equal(PickupKind.Reward, book.Kind);
        Assert.False(book.Magnet);
        Assert.Equal("item_fbook", book.Floor!.Sprite.Id);
        Assert.Null(book.Floor.Frame);
        Assert.NotNull(book.Draw.Sprite);                                                 // 대체 그림

        var chest = defs.Rewards.Get("reward_chest");
        Assert.Same(book, chest.Drop!.Def);
        Assert.Equal(1, chest.Bundle!.Potions);
        Assert.Equal(1, chest.Bundle.WeaponLevels);
        Assert.Null(defs.Rewards.Get("reward_tome").Drop);

        // 문 위 아이콘(7.11): 마법서·보물 상자 = 원작 물체, 회복 = 붉은 물약 마지막 프레임, 상인 = 상인 그림(icon 없음)
        Assert.Equal("obj_book1", defs.Rewards.Get("reward_tome").Icon!.Sprite.Id);
        Assert.Equal("obj_chest1", chest.Icon!.Sprite.Id);
        var heal = defs.Rewards.Get("reward_potion").Icon!;
        Assert.Equal("item_fbttlebr", heal.Sprite.Id);
        Assert.Equal(heal.Sprite.Def.Frames, heal.Frame);
        foreach (var (merchant, set) in new[] { ("smith", "set_towner_smith"), ("witch", "set_towner_witch"), ("healer", "set_towner_healer") })
        {
            var m = defs.Merchants.Get(merchant);
            Assert.Equal(set, m.Sprite!.Id);
            Assert.NotNull(m.Sprite.Def.Clips.Idle);
        }
        Assert.Equal([16, 19, 20], new[] { "towner_smith", "towner_witch", "towner_healer" }.Select(id => defs.Sprites.Get(id).Frames));
    }

    [Fact]
    public void FloorFrame_DropAnimThenLast_FixedFrame_OpenedChest()
    {
        var anim = new IconDef { Sprite = new DefRef<SpriteDef>("item_goldflip") };
        Assert.Equal(1, PickupArt.FloorFrame(anim, 10, 0f, false));
        Assert.Equal(3, PickupArt.FloorFrame(anim, 10, 0.1f + 1e-4f, false));            // 0.05s 마다 한 프레임
        Assert.Equal(10, PickupArt.FloorFrame(anim, 10, 0.45f + 1e-4f, false));
        Assert.Equal(10, PickupArt.FloorFrame(anim, 10, 30f, false));                     // 마지막에서 멈춤
        var rock = anim with { Frame = 20 };
        Assert.Equal(20, PickupArt.FloorFrame(rock, 20, 0f, false));                      // 보석: 처음부터 그 프레임
        var chest = anim with { Frame = 1 };
        Assert.Equal(1, PickupArt.FloorFrame(chest, 6, 5f, false));
        Assert.Equal(3, PickupArt.FloorFrame(chest, 6, 5f, true));                        // 열린 상자(빔)

        var defs = RealArtDefs.Defs;
        Assert.Equal(1f, PickupArt.GlowScale(defs.Pickups.Get("gem_blue"), false));
        Assert.Equal(0.6f, PickupArt.GlowScale(defs.Pickups.Get("gem_green"), true));
        Assert.Equal(1f, PickupArt.GlowScale(defs.Pickups.Get("gem_red"), false));       // 돌을 못 그리면 마름모 빛 그대로
        Assert.Equal(0f, PickupArt.GlowScale(defs.Pickups.Get("gold"), true));
    }

    [Fact]
    public void ImportTable_RockLineAfterFloorItems_DoubleBackslash()
    {
        var text = File.ReadAllText(Path.Combine(RealData.RepoRoot(), "tools", "DiabloAssetImporter", "Table", "missiles.json"));
        var bb = text.IndexOf("\"d1/items/fbttlebb\"", StringComparison.Ordinal);
        var rock = text.IndexOf("{ \"out\": \"d1/items/rock\",     \"cl2\": \"items\\\\rock.cel\",     \"width\": 96, \"shadow_alpha\": 0.5 }", StringComparison.Ordinal);
        Assert.True(bb > 0 && rock > bb, "rock 줄은 바닥 아이템 묶음 끝(fbttlebb 뒤)");
        Assert.True(rock < text.IndexOf("\"d1/objects/chest1\"", StringComparison.Ordinal));
    }
}
