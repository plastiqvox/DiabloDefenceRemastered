using DiabloDefenceRemastered.Core.Art;
using DiabloDefenceRemastered.Core.Data.Loading;
using DiabloDefenceRemastered.Core.Nav;
using DiabloDefenceRemastered.Core.Sim.Enemies;
using DiabloDefenceRemastered.Core.Sim.Weapons;

namespace DiabloDefenceRemastered.Core.Sim;

/// <summary>
/// 모듈 목록 한 곳 (설계서 11.1). 로더·게임·시험이 같은 목록을 쓴다 → 묶음이 서로의 파일을 고치지 않고 붙는다.
/// 순서 = 등록 순서(검사기·월드 설치·개발 옵션 적용 순서).
/// </summary>
public static class GameModules
{
    public static IReadOnlyList<IGameModule> Create() =>
    [
        new LoadingModule(),
        new NavModule(),
        new CombatModule(),
        new EnemyModule(),
        new ArtModule(),
    ];

    /// <summary>모든 모듈을 등록한 컨텍스트.</summary>
    public static ModuleContext CreateContext()
    {
        var ctx = new ModuleContext();
        foreach (var module in Create()) module.Register(ctx);
        return ctx;
    }
}
