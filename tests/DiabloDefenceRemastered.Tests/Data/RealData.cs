using System.Text.Json;
using System.Text.Json.Nodes;
using DiabloDefenceRemastered.Core.Data.Json;

namespace DiabloDefenceRemastered.Tests.Data;

/// <summary>
/// 실제 <c>Data/</c>(저장소 원문, WP0a 첫 판)를 읽는 도우미. 자원 없이 돈다. WP7 로더 전이라 두 종류(tilesets·maps)는 <see cref="MapDataReader"/>,
/// 나머지(그림·규칙)는 공용 STJ 옵션으로 형만 읽는다(참조는 연결하지 않음 — <see cref="TestJson"/>).
/// </summary>
public static class RealData
{
    public static string Dir { get; } = Path.Combine(RepoRoot(), "src", "DiabloDefenceRemastered", "Data");

    private static readonly Lazy<MapDataResult> MapsLazy = new(() => MapDataReader.Read(Dir));

    /// <summary>실제 tilesets·maps(한 번 읽어 둔다).</summary>
    public static MapDataResult Maps => MapsLazy.Value;

    public static MapDef Cathedral => Maps.Maps.Single(m => m.Id == "cathedral_arena");

    public static TilesetDef L1 => Maps.Tilesets.Single(t => t.Id == "l1");

    /// <summary><c>rules/sight.json</c> 의 <c>rules.sight</c>.</summary>
    public static SightRulesDef Sight()
    {
        var node = Read("rules/sight.json")["rules"]!["sight"]!;
        return node.Deserialize<SightRulesDef>(TestJson.Options) ?? throw new JsonException("rules.sight null");
    }

    /// <summary>파일 하나의 한 종류 배열을 형으로(모르는 속성 오류).</summary>
    public static List<T> Kind<T>(string rel, string kind) =>
        [.. ((JsonArray)Read(rel)[kind]!).Select(n => n.Deserialize<T>(TestJson.Options) ?? throw new JsonException($"{rel} {kind}: null"))];

    public static JsonObject Read(string rel)
    {
        var text = File.ReadAllText(Path.Combine(Dir, rel));
        Assert.Null(DefJson.FindDuplicateKey(text));
        return (JsonObject)DefJson.ParseNode(text)!;
    }

    /// <summary>
    /// 실제 맵·타일셋으로 된 시험 월드: 합성 정의(<see cref="TestDefs.Basic"/> — 직업·그림·적)에 실제 <c>cathedral_arena</c> 와 실제 <c>rules.sight</c>,
    /// 스테이지 1 빛 반경 420(기획 10.3)을 끼운다. 주인공은 맵의 <c>hero_start</c>.
    /// </summary>
    public static TestWorld CathedralWorld(float heroLight = 420f)
    {
        var basic = TestDefs.Basic();
        var map = Cathedral;
        var stage = TestDefs.Stage(map, lighting: new LightingDef { HeroLight = new HeroLightDef { Radius = heroLight } });
        var defs = new GameDefs
        {
            Rules = basic.Rules with { Sight = Sight() },
            Sprites = basic.Sprites,
            SpriteSets = basic.SpriteSets,
            Weapons = basic.Weapons,
            Passives = basic.Passives,
            HeroClasses = basic.HeroClasses,
            Enemies = basic.Enemies,
            Tilesets = new DefTable<TilesetDef>([L1]),
            Maps = new DefTable<MapDef>([map]),
            Stages = new DefTable<StageDef>([stage]),
        };
        return new TestWorldBuilder().WithDefs(defs).Build();
    }

    /// <summary>저장소 뿌리(<c>DiabloDefenceRemastered.sln</c> 이 있는 폴더) — 시험 실행 폴더에서 위로 올라가며 찾는다.</summary>
    public static string RepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "DiabloDefenceRemastered.sln"))) return dir.FullName;
        throw new InvalidOperationException("저장소 뿌리(DiabloDefenceRemastered.sln)를 찾지 못했다");
    }
}
