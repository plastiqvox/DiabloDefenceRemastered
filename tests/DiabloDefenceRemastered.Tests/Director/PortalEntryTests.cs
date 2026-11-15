using DiabloDefenceRemastered.Tests.Data;
using DiabloDefenceRemastered.Core.Sim.Director;

namespace DiabloDefenceRemastered.Tests.Director;

/// <summary>
/// 포탈 문(2026-11-15 사용자 결정 "둘레 문을 없애고 출구는 포탈, 적은 붉은 포탈이 열리며 나오게"). 실제 대성당 맵은 테두리에 문 조각이 없어 모든 문이 포탈 문이다.
/// </summary>
public class PortalEntryTests
{
    [Fact]
    public void RealMap_AllGatesArePortals_EnemiesStartAtPortalAndWalkIntoRoom()
    {
        var t = RealData.CathedralWorld();
        var w = t.World;
        Assert.All(w.Gates.Keys, id => Assert.True(DirectorSpawns.IsPortalEntry(w, id), id));

        var entry = w.Resolved!.Entry("gate_nw");
        var list = DirectorSpawns.AtEntry(w, w.Defs.Enemies.All.First(), EnemyRank.Normal, [], entry, 6, SpawnSource.March);
        Assert.Equal(6, list.Count);
        var into = Vector2.Normalize(entry.Inside - entry.Outside);
        foreach (var e in list)
        {
            Assert.True(Vector2.Distance(e.Pos, entry.Inside) <= 10f, $"{e.Pos} — 포탈 자리에서 생김(맵 밖 바깥 시작점이 아님)");
            Assert.True(w.Arena!.Iso.IsInsideMap(e.Pos), $"{e.Pos}");
            Assert.True(Vector2.Dot(e.EntryTarget - entry.Inside, into) > 20f, "방 안쪽으로 걸어 나옴");
        }
    }
}
