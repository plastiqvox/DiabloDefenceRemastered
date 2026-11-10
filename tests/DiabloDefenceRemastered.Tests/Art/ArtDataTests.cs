using DiabloDefenceRemastered.Core.Art;
using DiabloDefenceRemastered.Tests.Data;

namespace DiabloDefenceRemastered.Tests.Art;

/// <summary>WP5 데이터(<c>sprites/objects.json</c> — 보상 물체 대체 그림)와 액트 미리 읽기 목록(7.5).</summary>
public class ArtDataTests
{
    [Fact]
    public void ObjectSprites_StrictGridSheets_CoverRewardIcons()
    {
        var sprites = RealData.Kind<SpriteDef>("sprites/objects.json", "sprites");
        Assert.Equal(sprites.Count, sprites.Select(s => s.Id).Distinct().Count());
        foreach (var s in sprites)
        {
            Assert.StartsWith("obj_", s.Id);
            Assert.Equal(SpriteSource.Grid, s.Source);
            Assert.DoesNotContain('/', s.File);                          // 원작 exe PNG = 자원 뿌리 바로 아래
            Assert.EndsWith(".png", s.File);
            Assert.Equal(new Int2(s.Frame.X * s.Directions, s.Frame.Y * s.Frames), s.Sheet);   // 13.5-7 정의끼리: 시트 = 칸 × 개수
            Assert.True(s.Origin.X >= 0 && s.Origin.X <= s.Frame.X && s.Origin.Y >= 0 && s.Origin.Y <= s.Frame.Y, $"{s.Id} origin 이 칸 밖");
        }
        // 설계서 6.18 예의 아이콘·물체 ID(rewards.json·pickups.json 은 WP3 가 이 이름으로 가리킨다)
        string[] expected = ["obj_book_icon", "obj_chest_icon", "obj_potion_icon", "obj_shrine_icon", "obj_shrine_red_icon", "obj_shrine_dark_icon",
                             "obj_tome", "obj_chest", "obj_potion", "obj_shrine", "obj_shrine_red", "obj_shrine_dark"];
        Assert.Empty(expected.Except(sprites.Select(s => s.Id)));
    }

    [Fact]
    public void ActPreload_ReachableSetsWithVariants_LooseSpritesAll_MapsWithMirrors()
    {
        var walk = TestDefs.Sprite("walk_a");
        var other = TestDefs.Sprite("walk_b");
        var loose = TestDefs.Sprite("fireba", 16, 14);
        var setA = TestDefs.SpriteSet(walk, "set_a") with { Variants = ["", "white"] };
        var setB = TestDefs.SpriteSet(other, "set_b");
        var heroSet = TestDefs.SpriteSet(TestDefs.Sprite("hero_walk"), "set_hero");
        var enemyA = TestDefs.Enemy(setA, "skeleton") with { Variant = "white" };
        var weapon = TestDefs.Weapon();
        var hero = TestDefs.HeroClass(heroSet, weapon);
        var map = TestDefs.Map();
        var stage = TestDefs.Stage(map) with
        {
            SpawnIntervals = [new SpawnIntervalDef { Weights = [new WeightEntry<EnemyDef>(DefRef<EnemyDef>.To(enemyA), 1f)] }],
        };
        var defs = new GameDefs
        {
            Sprites = new DefTable<SpriteDef>([walk, other, loose, heroSet.Clips.Walk!.Sprite.Def]),
            SpriteSets = new DefTable<SpriteSetDef>([setA, setB, heroSet]),
            Enemies = new DefTable<EnemyDef>([enemyA]),
            Stages = new DefTable<StageDef>([stage]),
        };

        var uses = ActPreload.Sprites(defs, stage, hero).Select(u => (u.Sprite.Id, u.Variant)).ToHashSet();
        Assert.Contains(("walk_a", "white"), uses);          // 나오는 적의 그 변종
        Assert.DoesNotContain(("walk_a", ""), uses);         // 원색 skeleton 은 이 액트에 없음
        Assert.DoesNotContain(("walk_b", ""), uses);         // 안 나오는 묶음
        Assert.Contains(("hero_walk", ""), uses);            // 주인공
        Assert.Contains(("fireba", ""), uses);               // 묶음 밖 그림은 모두

        var all = ActPreload.Sprites(defs, null, null).Select(u => (u.Sprite.Id, u.Variant)).ToHashSet();
        Assert.Contains(("walk_a", ""), all);
        Assert.Contains(("walk_a", "white"), all);
        Assert.Contains(("walk_b", ""), all);

        Assert.Equal([map.Id], ActPreload.Maps(stage).Select(m => m.Id));
        Assert.Empty(ActPreload.Maps(null));
        Assert.Equal(["d1/monsters/zombie.json", "d1/monsters/zombie@bluered.png"],
                     ActPreload.PrimaryFiles(new SpriteUse(new SpriteDef { Id = "z", Source = SpriteSource.Atlas, File = "d1/monsters/zombie.png" }, "bluered")));
    }
}
