using DiabloDefenceRemastered.Core.Assets;
using DiabloDefenceRemastered.Tests.Data;

namespace DiabloDefenceRemastered.Tests.Assets;

/// <summary>
/// 13.5-10 (설계서 6.14, 소유 WP0): 정의가 가리키는 파일 이름 ⊂ 가져오기 표가 만드는 이름 + <c>Table/exe.json</c>. 자원 없이 돈다.
/// ① 기본 — 실제 표(<c>tools/DiabloAssetImporter/Table</c>)를 Core <see cref="ImportTable"/> 로 엄격하게 읽어 이름 목록을 만들고,
///   WP7 로더 전이라 실제 <c>Data/sprites</c> 는 형으로만 읽어(<see cref="RealData"/>) 페이지 × 변종·메타, 맵 그림 이름을 대조한다.
/// ② <c>Integration</c> — 정의 → 이름 추출을 Core <see cref="DefAssetNames.For"/> 하나로(시작 검사 ③ 과 같은 함수, 본문 WP7). 통합 때 기본으로 옮긴다.
/// </summary>
public class ImportNamesTests
{
    private static readonly string[] SpriteFiles = ["sprites/monsters.json", "sprites/hero.json"];

    private static string TableDir => Path.Combine(RealData.RepoRoot(), "tools", "DiabloAssetImporter", "Table");

    private static ImportTable Table()
    {
        var r = ImportTable.Load(TableDir);
        Assert.True(!r.HasErrors, string.Join("\n", r.Errors));
        return r.Table!;
    }

    private static List<ArenaGrid> Arenas()
    {
        Assert.Empty(RealData.Maps.Errors);
        var arenas = new List<ArenaGrid>();
        foreach (var m in RealData.Maps.Maps)
        {
            arenas.Add(ArenaLayout.Expand(m));
            if (m.Mirrored is { } mm) arenas.Add(ArenaLayout.Expand(mm, mm.Tileset.Def));
        }
        return arenas;
    }

    [Fact]
    public void RealTable_ReadsStrictly_ValidatesClean_MapsHaveTilesets()
    {
        var t = Table();
        Assert.Empty(t.Validate());
        Assert.Empty(t.CheckMaps(RealData.Maps.Maps));
        var names = t.OutputNames();
        Assert.All(names, n => Assert.StartsWith("d1/", n, StringComparison.Ordinal));
        Assert.Equal(names.Order(StringComparer.Ordinal), names);
        Assert.Equal(names.Count, names.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void RealSprites_PagesTimesVariants_AndMeta_AreTableNames()
    {
        var all = Table().AllNames(Arenas()).ToHashSet(StringComparer.Ordinal);
        var sprites = SpriteFiles.SelectMany(f => RealData.Kind<SpriteDef>(f, "sprites")).ToDictionary(s => s.Id);
        var sets = SpriteFiles.SelectMany(f => RealData.Kind<SpriteSetDef>(f, "sprite_sets")).ToList();
        var missing = new SortedSet<string>(StringComparer.Ordinal);
        void Need(string name) { if (!all.Contains(name)) missing.Add(name); }

        foreach (var s in sprites.Values)
        {
            Need(s.File);
            if (s.Source == SpriteSource.Atlas) Need(Path.ChangeExtension(s.File, ".json"));
        }
        foreach (var set in sets)
            foreach (var kind in Enum.GetValues<ClipKind>())
                if (set.Clips.Get(kind) is { } clip && sprites.TryGetValue(clip.Sprite.Id, out var sp) && sp.Source == SpriteSource.Atlas)
                    foreach (var v in set.Variants.Where(v => v != ""))
                        Need($"{sp.File[..^4]}@{v}.png");
        Assert.True(missing.Count == 0, "표가 만들지 않는 이름: " + string.Join(", ", missing));
    }

    [Fact]
    public void RealMaps_ArtNames_AreTableNames_AndBundleIsTilesetEntry()
    {
        var t = Table();
        var arenas = Arenas();
        var bundles = t.OutputBundles(arenas);
        foreach (var grid in arenas)
            foreach (var n in ArenaArt.OutputNames(grid))
            {
                Assert.True(bundles.TryGetValue(n, out var b), n);
                Assert.Equal(t.Tilesets.Tilesets.Single(x => x.Id == grid.Tileset.Id).Bundle, b);
            }
    }

    /// <summary>② 정의 → 이름(주인 포함) ⊂ 표 이름 + exe 이름, 주인 D 의 <c>requires</c> 가 묶음을 덮음. WP7 로더·<see cref="DefAssetNames"/> 본문 뒤에 돈다.</summary>
    [Fact, Trait("Category", TestCategories.Integration)]
    public void DefAssetNames_SubsetOfTableNames_BundlesCovered()
    {
        TestCategories.RequireEnabled(TestCategories.Integration);
        // 통합 때(WP7 DataLoader 가 생긴 뒤) 실제 Data 전체를 GameDefs 로 읽어 DefAssetNames.For 에 넘긴다.
        // 그 전에는 tilesets·maps 만 든 정의로 맵 그림 쪽만 본다.
        var defs = new GameDefs
        {
            Tilesets = new DefTable<TilesetDef>(RealData.Maps.Tilesets),
            Maps = new DefTable<MapDef>(RealData.Maps.Maps),
        };
        var arenas = Arenas();
        var t = Table();
        var all = t.AllNames(arenas).ToHashSet(StringComparer.Ordinal);
        var bundles = t.OutputBundles(arenas);
        foreach (var a in DefAssetNames.For(defs, arenas))
        {
            Assert.True(all.Contains(a.File), $"표가 만들지 않는 이름: {a.File}");
            if (bundles.TryGetValue(a.File, out var b) && b is { } bundle)
                Assert.True(a.Owner is not null, $"기본 내용이 묶음 {bundle} 의 파일 {a.File} 을 씀");
        }
    }
}
