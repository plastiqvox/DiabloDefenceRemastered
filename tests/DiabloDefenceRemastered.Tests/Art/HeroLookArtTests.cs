using DiabloDefenceRemastered.Core.Art;

namespace DiabloDefenceRemastered.Tests.Art;

/// <summary>
/// 주인공 겉모습 아홉 벌(v0.9.1 V10, 설계서 6.19 "주인공 겉모습", 단계 2 wp5): 실제 <c>heroes/sorcerer.json</c> 의 <c>looks</c>·<c>look</c> 과 <c>sprites/hero.json</c> 의 묶음,
/// 그리기의 묶음 고르기(<see cref="HeroLookArt"/> — 없는 키 → <c>look</c> → 직업 묶음).
/// </summary>
public class HeroLookArtTests
{
    private static readonly string[] Keys = ["ln", "lu", "lt", "mn", "mu", "mt", "hn", "hu", "ht"];

    [Fact]
    public void RealSorcerer_NineKeys_EachSetSameTimingAsClassSet()
    {
        var defs = RealArtDefs.Defs;
        var sorc = defs.HeroClasses.Get("sorcerer");
        Assert.Equal(Keys.Order(StringComparer.Ordinal), sorc.Looks.Keys.Order(StringComparer.Ordinal));
        Assert.Equal("lt", sorc.Look);
        var main = sorc.Sprites.Def;
        foreach (var key in Keys)
        {
            var set = sorc.Looks[key].Def;
            Assert.Equal($"set_sorcerer_{key}", set.Id);
            Assert.Equal(main.FrameTime, set.FrameTime);
            Assert.Equal(main.BaseSpeed, set.BaseSpeed);
            Assert.Equal(main.BodyHeight, set.BodyHeight);
            // 동작 시간이 묶음마다 같다(시뮬레이션은 직업 묶음 하나만 본다 — 6.19): 클립마다 프레임 수·프레임 시간·이벤트 프레임
            foreach (var kind in Enum.GetValues<ClipKind>())
            {
                var a = main.Clips.Get(kind);
                var b = set.Clips.Get(kind);
                Assert.Equal(a is null, b is null);
                if (a is null || b is null) continue;
                Assert.Equal(a.Sprite.Def.Frames, b.Sprite.Def.Frames);
                Assert.Equal(a.Sprite.Def.Directions, b.Sprite.Def.Directions);
                Assert.Equal(a.FrameTime, b.FrameTime);
                Assert.Equal(a.EventFrame, b.EventFrame);
            }
            // 그림 파일 = 그 벌의 페이지(lt 는 slt), 죽기는 그 갑옷의 맨손 벌(ln 은 slt 페이지의 sln_die — 같은 원본)
            var file = $"d1/hero/s{key}.png";
            Assert.Equal(file, set.Clips.Walk!.Sprite.Def.File);
            Assert.Equal(file, set.Clips.CastFire!.Sprite.Def.File);
        }
    }

    [Fact]
    public void Pick_KeyThenLookThenClassSet()
    {
        var defs = RealArtDefs.Defs;
        var sorc = defs.HeroClasses.Get("sorcerer");
        Assert.Equal("set_sorcerer_mu", HeroLookArt.Pick(sorc, "mu", _ => true).Id);
        Assert.Equal("set_sorcerer_lt", HeroLookArt.Pick(sorc, "zz", _ => true).Id);                       // 없는 키 → look
        Assert.Equal("set_sorcerer_lt", HeroLookArt.Pick(sorc, "hn", s => s.Id != "set_sorcerer_hn").Id);   // 못 그리는 벌 → look
        Assert.Equal("set_sorcerer", HeroLookArt.Pick(sorc, "hn", _ => false).Id);                          // look 도 못 그리면 직업 묶음

        // 시작 겉모습(장비 없음) = ln(기획 34절 — 맨손 벌)
        var hero = new Hero();
        hero.Reset();
        Assert.Equal(HeroLook.Start, HeroLook.Key(hero.Equipment, defs));
        Assert.Equal("set_sorcerer_ln", HeroLookArt.Pick(sorc, hero, defs, _ => true).Id);

        // looks 가 빈 직업(대체 데이터)은 늘 직업 묶음
        var bare = sorc with { Looks = new Dictionary<string, DefRef<SpriteSetDef>>() };
        Assert.Equal("set_sorcerer", HeroLookArt.Pick(bare, "mu", _ => true).Id);
    }
}
