using DiabloDefenceRemastered.Core.Sim.Enemies;

namespace DiabloDefenceRemastered.Core.Sim;

/// <summary>월드 — WP4 적 조각: 정의 + 접두사 조합의 부품 표 캐시(같은 조합의 적이 같이 씀 — 적마다의 상태가 아니라 정의에서 만든 불변 표).</summary>
public sealed partial class World
{
    private readonly Dictionary<(int Def, int A0, int A1, int A2), EnemyKit> _kits = [];

    /// <summary>정의 번호·접두사 번호(−1 = 없음, 2026-11-14 세 칸) 조합의 부품 표.</summary>
    public EnemyKit KitFor(EnemyDef def, int affix0 = -1, int affix1 = -1, int affix2 = -1)
    {
        var index = Defs.Enemies.IndexOf(def.Id);
        var key = (index, affix0, affix1, affix2);
        if (_kits.TryGetValue(key, out var kit)) return kit;
        var affixes = new List<AffixDef>(3);
        if (affix0 >= 0) affixes.Add(Defs.Affixes[affix0]);
        if (affix1 >= 0) affixes.Add(Defs.Affixes[affix1]);
        if (affix2 >= 0) affixes.Add(Defs.Affixes[affix2]);
        kit = EnemyKit.Build(def, index, affixes);
        _kits[key] = kit;
        return kit;
    }
}
