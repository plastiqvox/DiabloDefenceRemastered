namespace DiabloDefenceRemastered.Core.Sim.Enemies;

/// <summary>
/// 우두머리 고유 이름(2026-11-14, 기획 9.2 디아2식): <c>rules.ranks.unique_names</c> 의 <c>first</c>+<c>second</c> 를 붙여 쓰고 띄어 <c>title</c>.
/// 생성 때 <c>World.Rng.Rank</c> 로 세 번호를 굴려 <c>Enemy.NameRoll</c> 에 10 비트씩 담고, 그리기가 글자로 바꾼다(시뮬레이션은 글자를 안 씀).
/// </summary>
public static class UniqueNames
{
    private const int Bits = 10, Mask = (1 << Bits) - 1;

    /// <summary>세 번호 굴림(낱말 표가 비었으면 −1, 난수도 쓰지 않음).</summary>
    public static int Roll(World world)
    {
        if (world.Defs.Rules.Ranks?.UniqueNames is not { First.Count: > 0, Second.Count: > 0, Title.Count: > 0 } names) return -1;
        var rng = world.Rng.Rank;
        var a = rng.Index(Math.Min(names.First.Count, Mask + 1));
        var b = rng.Index(Math.Min(names.Second.Count, Mask + 1));
        var c = rng.Index(Math.Min(names.Title.Count, Mask + 1));
        return a | b << Bits | c << (2 * Bits);
    }

    /// <summary>
    /// 머리 위 이름판(기획 9.1·9.2): 정예 = ("광폭한 해골", null), 우두머리 = ("썩은이빨 도살자", "해골 · 불꽃 강화 · 힘의 오라"), 일반 = (정의 이름, null).
    /// 우두머리 이름 굴림이 없으면 정의 이름이 첫 줄이고 둘째 줄에 종류를 다시 쓰지 않는다.
    /// </summary>
    public static (string Name, string? Affixes) Plate(World world, Enemy e)
    {
        var defs = world.Defs;
        string? Label(int i) => e.AffixIdx[i] < 0 ? null : defs.Affixes[e.AffixIdx[i]] is var a && a.Label.Length > 0 ? a.Label : a.Name;
        switch (e.Rank)
        {
            case EnemyRank.Elite:
                return e.AffixIdx[0] >= 0 && defs.Affixes[e.AffixIdx[0]].Name is { Length: > 0 } prefix ? (prefix + " " + e.Def.Name, null) : (e.Def.Name, null);
            case EnemyRank.Unique:
            {
                var unique = Format(defs.Rules.Ranks?.UniqueNames, e.NameRoll);
                var parts = new List<string>(3);
                if (unique is not null) parts.Add(e.Def.Name);
                if (Label(0) is { } l0) parts.Add(l0);
                if (Label(1) is { } l1) parts.Add(l1);
                return (unique ?? e.Def.Name, parts.Count > 0 ? string.Join(" · ", parts) : null);
            }
            default:
                return (e.Def.Name, null);
        }
    }

    /// <summary>굴림 → "썩은이빨 도살자". 굴림이 없거나 표가 바뀌어 번호가 넘치면 null.</summary>
    public static string? Format(UniqueNamesDef? names, int roll)
    {
        if (roll < 0 || names is null) return null;
        int a = roll & Mask, b = roll >> Bits & Mask, c = roll >> (2 * Bits) & Mask;
        if (a >= names.First.Count || b >= names.Second.Count || c >= names.Title.Count) return null;
        return names.First[a] + names.Second[b] + " " + names.Title[c];
    }
}
