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

    /// <summary>
    /// 첫 플레이(2026-11-11)에서 처치 141 에 금화 0 — 적 정의·등급 표에 drop_table 이 비어 드롭 표가 굴려지지 않았다.
    /// 기획 11.1: 일반 = dt_common(금화 3%, 1~5), 정예 = dt_elite(금화 100%, 10~30), 우두머리 = dt_unique.
    /// v0.9.2 값(WP3 단계 2 — 기획 11.1·11.5·11.7): 금화 일반 10%·3~7, 체력 물약 일반 0.3%·정예 10%·우두머리 30%·중간 보스 1개 확정.
    /// </summary>
    [Fact]
    public void RealData_EnemiesAndRanksHaveDropTables_WithGold()
    {
        var ctx = GameModules.CreateContext();
        var r = DataLoader.Load(RealData.Dir, ctx.Parts, ctx.Validators);
        Assert.False(r.HasErrors, string.Join("\n", r.Errors));
        var normal = r.Defs.Enemies.All.Where(e => e.Rank == DiabloDefenceRemastered.Core.Defs.DefRank.Normal).ToList();
        Assert.NotEmpty(normal);
        foreach (var e in normal) Assert.Equal("dt_common", e.DropTable?.Def.Id);
        var common = r.Defs.DropTables.Get("dt_common").Entries.Single(x => x.Pickup.Def.Id == "gold");
        Assert.Equal(0.10f, common.Chance);
        Assert.Equal([3, 7], common.Value!);
        var ranks = r.Defs.Rules.Ranks!;
        Assert.Equal("dt_elite", ranks.Elite!.DropTable?.Def.Id);
        Assert.Equal("dt_unique", ranks.Unique!.DropTable?.Def.Id);
        var elite = ranks.Elite.DropTable!.Def.Entries.Single(x => x.Pickup.Def.Id == "gold");
        Assert.Equal(1f, elite.Chance);
        Assert.Equal([10, 30], elite.Value!);

        float Potion(string table) => r.Defs.DropTables.Get(table).Entries.Single(x => x.Pickup.Def.Id == "healing_potion").Chance;
        Assert.Equal(0.003f, Potion("dt_common"));
        Assert.Equal(0.10f, Potion("dt_elite"));
        Assert.Equal(0.30f, Potion("dt_unique"));
        Assert.Equal(1f, Potion("dt_miniboss"));
        Assert.Equal(PickupKind.PotionStack, r.Defs.Pickups.Get("healing_potion").Kind);
    }
}
