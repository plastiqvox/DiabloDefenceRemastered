using DiabloDefenceRemastered.Core.Data.Loading;
using DiabloDefenceRemastered.Core.Modules;

namespace DiabloDefenceRemastered.Tests.Data;

/// <summary>
/// 실제 <c>Data/</c> 를 엄격 모드로(WP7). 10.1 의 <c>Data</c>(실제, <c>Integration</c>) 행은 모든 모듈 검사기가 들어온 뒤 WP6 이 기본으로 옮긴다 —
/// 여기서는 지금 등록된 모듈로 읽어 로더 자체가 실제 파일을 오류 없이 받는지만 본다(다른 묶음 본문이 없어도 돈다).
/// </summary>
public sealed class RealDataLoadTests
{
    [Fact]
    public void RealData_LoadsStrict_WithCurrentModules()
    {
        var ctx = GameModules.CreateContext();
        var r = DataLoader.Load(RealData.Dir, ctx.Parts, ctx.Validators);
        Assert.False(r.HasErrors, string.Join("\n", r.Errors));
        Assert.True(r.Defs.Maps.TryGet("cathedral_arena", out var map));
        Assert.Same(r.Defs.Tilesets.Get("l1"), map.Tileset.Def);
        Assert.NotNull(r.Defs.Rules.Sight);
        Assert.True(r.Defs.SpriteSets.TryGet("set_sorcerer", out var set));
        Assert.Same(r.Defs.Sprites.Get("slt_idle"), set.Clips.Idle!.Sprite.Def);
    }

    /// <summary>도구 경로(<c>MapDataReader.Read(dir)</c>)와 로더가 같은 배치를 만든다(같은 LayoutHash — 1.4).</summary>
    [Fact]
    public void RealData_MapsSameAsToolReader()
    {
        var ctx = GameModules.CreateContext();
        var r = DataLoader.Load(RealData.Dir, ctx.Parts, ctx.Validators);
        var tool = RealData.Maps;
        Assert.Equal(tool.Maps.Select(m => m.Id), r.Defs.Maps.All.Select(m => m.Id));
        foreach (var m in tool.Maps)
            Assert.Equal(ArenaLayout.Expand(m).LayoutHash, ArenaLayout.Expand(r.Defs.Maps.Get(m.Id)).LayoutHash);
    }
}
