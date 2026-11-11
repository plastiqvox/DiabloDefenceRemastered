namespace DiabloDefenceRemastered.Tests.Data;

/// <summary>
/// 실제 <c>Data/sprites/*.json</c>(단계 1 그림 묶음)·<c>Data/rules/sight.json</c> (WP0a 첫 판). WP7 로더 전이라 형으로 엄격하게 읽고(모르는 속성 오류)
/// 파일 안 참조·클립 범위(13.5-7 정의끼리 쪽)·직업 그림 클립(13.5-21)만 본다.
/// </summary>
public class RealSpriteAndSightTests
{
    private static readonly string[] SpriteFiles = ["sprites/monsters.json", "sprites/hero.json"];

    private static (List<SpriteDef> Sprites, List<SpriteSetDef> Sets) ReadAll()
    {
        var sprites = new List<SpriteDef>();
        var sets = new List<SpriteSetDef>();
        foreach (var f in SpriteFiles)
        {
            sprites.AddRange(RealData.Kind<SpriteDef>(f, "sprites"));
            sets.AddRange(RealData.Kind<SpriteSetDef>(f, "sprite_sets"));
        }
        return (sprites, sets);
    }

    [Fact]
    public void Sight_IsSection610Values()
    {
        // 6.10 첫 판 = 합성 시험 값과 같다(합성 월드와 실제 데이터의 시야가 같은 규칙)
        Assert.Equal(TestDefs.Sight, RealData.Sight());
        Assert.Equal(["sight"], RealData.Read("rules/sight.json")["rules"]!.AsObject().Select(p => p.Key));
    }

    [Fact]
    public void Sprites_ReadStrictly_ClipsResolve_EventFramesInRange()
    {
        var (sprites, sets) = ReadAll();
        var byId = sprites.ToDictionary(s => s.Id);   // 중복 ID 면 여기서 던진다
        Assert.Equal(sets.Count, sets.Select(s => s.Id).Distinct().Count());

        foreach (var s in sprites)
        {
            Assert.Equal(SpriteSource.Atlas, s.Source);
            Assert.StartsWith("d1/", s.File);
            Assert.Equal(8, s.Directions);
            Assert.True(s.Frames > 0 && s.Frame.X > 0 && s.Frame.Y > 0, s.Id);
            Assert.True(s.Origin.X > 0 && s.Origin.X < s.Frame.X && s.Origin.Y > 0 && s.Origin.Y < s.Frame.Y, $"{s.Id} origin 이 칸 밖");
            Assert.Null(s.ColorKey);
        }
        foreach (var set in sets)
        {
            // 대략 60~90(6.3). 단계 2 의 해골왕(칸 160)만 큰 몸 — 몸 높이 < 그 묶음 서기 칸 높이
            var cell = set.Clips.Idle is { } idle && byId.TryGetValue(idle.Sprite.Id, out var idleSprite) ? idleSprite.Frame.Y : 96;
            Assert.True(set.BodyHeight > 50 && set.BodyHeight < Math.Max(100, cell - 16), set.Id);
            Assert.Contains("", set.Variants);
            foreach (var kind in Enum.GetValues<ClipKind>())
            {
                if (set.Clips.Get(kind) is not { } clip) continue;
                Assert.True(byId.TryGetValue(clip.Sprite.Id, out var sprite), $"{set.Id}.{kind}: 그림 '{clip.Sprite.Id}' 없음");
                if (clip.EventFrame is { } ev) Assert.InRange(ev, 1, sprite.Frames);
                if (clip.FrameTime is { } ft) Assert.InRange(ft, 0.05f, 0.25f);
            }
            Assert.NotNull(set.Clips.Idle); Assert.NotNull(set.Clips.Walk); Assert.NotNull(set.Clips.Die);
        }
    }

    [Fact]
    public void StageOneSets_MatchDesign()
    {
        var (sprites, sets) = ReadAll();
        var set = sets.ToDictionary(s => s.Id);
        var frames = sprites.ToDictionary(s => s.Id, s => s.Frames);

        // 기획 8.2 단계 1: 해골·불타는 해골 = set_skelaxe(white·원색), 해골 궁수 = set_skelbow(white), 좀비·구울 = set_zombie(원색·bluered)
        Assert.Equal(["", "white"], set["set_skelaxe"].Variants);
        // 단계 2(wp4): 블랙애시 bashtb·로트피스트 eth 변종과 소환용 특수 클립(16프레임)
        Assert.Equal(["", "white", "bashtb"], set["set_skelbow"].Variants);
        Assert.Equal(["", "bluered", "eth"], set["set_zombie"].Variants);
        Assert.Equal(16, frames[set["set_skelbow"].Clips.Special!.Sprite.Id]);
        Assert.Equal(16, frames[set["set_zombie"].Clips.Special!.Sprite.Id]);
        // 해골 대장 skelsd(white)·해골왕 sking(genrl, 칸 160 — 기획 9.5 공격 16·특수 12)
        Assert.Contains("white", set["set_skelsd"].Variants);
        Assert.Contains("genrl", set["set_sking"].Variants);
        Assert.Equal(30f, set["set_sking"].BaseRadius);
        Assert.Equal(16, frames["sking_attack"]);
        Assert.Equal(12, frames["sking_special"]);
        Assert.Equal(12, set["set_skelbow"].Clips.Attack!.EventFrame);
        Assert.Equal(16, frames["skelbow_attack"]);   // 기획 8.2 "원작 활 해골 a(16프레임)"
        Assert.Equal(24, frames["zombie_walk"]);

        // 주인공(6.16·13.5-21): idle·walk·cast_fire·cast_lightning·cast_magic·die, 시전 12프레임·발동 8(기획 4.0)
        var hero = set["set_sorcerer"];
        foreach (var c in new[] { hero.Clips.CastFire, hero.Clips.CastLightning, hero.Clips.CastMagic })
        {
            Assert.NotNull(c);
            Assert.Equal(8, c.EventFrame);
            Assert.Equal(12, frames[c.Sprite.Id]);
        }
        Assert.Equal(20, frames[hero.Clips.Die!.Sprite.Id]);
        Assert.Equal(14f, hero.BaseRadius);
    }
}
