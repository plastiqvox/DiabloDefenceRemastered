using System.Text.Json.Nodes;
using DiabloDefenceRemastered.Core.Assets;
using DiabloDefenceRemastered.Tests.Data;

namespace DiabloDefenceRemastered.Tests.Assets;

/// <summary>정의 → 파일·주인 추출(13.5-10 ②·시작 검사 ③ — 본문 WP7).</summary>
public sealed class DefAssetNamesTests
{
    private static (GameDefs Defs, IReadOnlyList<DefAsset> Names) Load(LoaderFixture f)
    {
        var r = f.Load();
        Assert.False(r.HasErrors, string.Join("\n", r.Errors));
        var grids = r.Defs.Maps.All.Select(m => ArenaLayout.Expand(m)).ToList();
        return (r.Defs, DefAssetNames.For(r.Defs, grids));
    }

    [Fact]
    public void Owners_FollowAttributionRules()
    {
        var f = new LoaderFixture();
        f.Files["audio/sounds.json"] = LoaderFixture.Obj("""
            { "sounds": [ { "id": "snd_hurt", "files": ["d1/sound/sfx/sorceror/mage69.wav"], "category": "hero" },
                          { "id": "snd_ui", "files": ["d1/sound/sfx/items/titlemov.wav"], "category": "ui" } ],
              "music": [ { "id": "mus_cath", "file": "d1/sound/music/dlvla.wav" } ],
              "sound_bindings": { "hero": { "sorcerer": { "hurt": "snd_hurt" } }, "ui": { "menu_move": "snd_ui" }, "music": { "stage_test": "mus_cath" } } }
            """);
        var (defs, names) = Load(f);
        IDef? OwnerOf(string file) => names.Single(n => n.File == file).Owner;

        // 적: 그 변종 페이지 + 메타
        Assert.Same(defs.Enemies.Get("skeleton"), OwnerOf("d1/monsters/skelaxe@white.png"));
        Assert.Contains(names, n => n.File == "d1/monsters/skelaxe.json" && n.Owner == defs.Enemies.Get("skeleton"));
        Assert.DoesNotContain(names, n => n.File == "d1/monsters/skelaxe.png" && n.Owner == defs.Enemies.Get("skeleton"));
        // 직업: 원색 페이지
        Assert.Same(defs.HeroClasses.Get("sorcerer"), OwnerOf("d1/hero/slt.png"));
        // 스테이지: 맵 그림
        var art = defs.Maps.Get("cathedral_arena").Art;
        Assert.Contains(names, n => n.File == $"{art}/arena.json" && n.Owner == defs.Stages.Get("stage_test"));
        // 소리 연결 표: hero.<id> → 직업, ui → 기본, music.<스테이지> → 음악 D
        Assert.Same(defs.HeroClasses.Get("sorcerer"), OwnerOf("d1/sound/sfx/sorceror/mage69.wav"));
        Assert.Null(OwnerOf("d1/sound/sfx/items/titlemov.wav"));
        Assert.Same(defs.Music.Get("mus_cath"), OwnerOf("d1/sound/music/dlvla.wav"));
        Assert.Contains(names, n => n.File == AssetFormat.SoundIndexPath && n.Owner is null);
        // 아무 D 도 안 쓰는 그림 묶음(set_zombie)은 기본
        Assert.Contains(names, n => n.File == "d1/monsters/zombie.png" && n.Owner is null);
        // 결정적 순서
        Assert.Equal(names.Select(n => n.File).OrderBy(x => x, StringComparer.Ordinal), names.Select(n => n.File));
    }

    [Fact]
    public void SummonedEnemy_IsItsOwnOwner()
    {
        var f = new LoaderFixture();
        var enemies = (JsonArray)f.Files["enemies/skeleton.json"]["enemies"]!;
        enemies.Add(LoaderFixture.Obj("""{ "id": "zed", "sprites": "set_zombie", "variant": "bluered" }"""));
        ((JsonArray)f.First("enemies/skeleton.json", "enemies")["behaviors"]!).Add(LoaderFixture.Obj("""{ "type": "summon", "trigger": "on_death", "enemy": "zed" }"""));
        var (defs, names) = Load(f);
        var zombiePage = names.Where(n => n.File == "d1/monsters/zombie@bluered.png").ToList();
        Assert.Same(defs.Enemies.Get("zed"), Assert.Single(zombiePage).Owner);
    }
}
