using DiabloDefenceRemastered.Core.Data.Loading;
using DiabloDefenceRemastered.Core.Modules;
using DiabloDefenceRemastered.Tests.Data;

namespace DiabloDefenceRemastered.Tests.Art;

/// <summary>실제 <c>Data/</c> 를 엄격 모드로 한 번 읽어 둔 정의(단계 2 wp5 Art 시험 공용 — <c>TestKit</c> 은 고치지 않음). 오류가 있으면 첫 사용에서 실패.</summary>
internal static class RealArtDefs
{
    private static readonly Lazy<GameDefs> Lazy = new(() =>
    {
        var ctx = GameModules.CreateContext();
        var r = DataLoader.Load(RealData.Dir, ctx.Parts, ctx.Validators);
        Assert.False(r.HasErrors, string.Join("\n", r.Errors));
        return r.Defs;
    });

    public static GameDefs Defs => Lazy.Value;
}
