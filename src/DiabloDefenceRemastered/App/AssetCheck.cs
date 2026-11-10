using DiabloDefenceRemastered.Core.Dev;

namespace DiabloDefenceRemastered.App;

/// <summary>
/// 실행 시작 자원 검사 (설계서 1.4 끝, WP7). 검사 본문은 Core <see cref="AssetStartupCheck"/>(시험 가능), 여기서는 실행 파일 옆 폴더(<c>Assets\</c>·<c>ImportTable\</c>)와
/// 맵 격자(원판 + 거울판)를 넘기고 로그를 쓴다. 게임은 자원 폴더를 실행 옵션으로 바꾸지 않는다 — 빌드 속성 <c>DdrAssetDir</c> 하나뿐(1.4).
/// </summary>
public static class AssetCheck
{
    public static string AssetsDir => Path.Combine(AppContext.BaseDirectory, "Assets");
    public static string ImportTableDir => Path.Combine(AppContext.BaseDirectory, "ImportTable");

    /// <summary>검사하고 <c>ASSETS</c> 줄(자원 쪽 필드)과 문제를 로그에 쓴다.</summary>
    public static AssetCheckResult Run(GameDefs defs, IAppLog log)
    {
        var grids = new List<ArenaGrid>();
        foreach (var map in defs.Maps.All)
        {
            grids.Add(ArenaLayout.Expand(map));
            if (map.Mirrored is { } m) grids.Add(ArenaLayout.Expand(m));
        }
        var result = AssetStartupCheck.Run(AssetsDir, ImportTableDir, defs, grids);
        log.Info("ASSETS " + result.LogFields());
        foreach (var p in result.Problems) log.Error("ASSETS " + p);
        if (result.DisabledFiles.Count > 0) log.Info($"ASSETS disabled_files={result.DisabledFiles.Count}");
        return result;
    }
}
