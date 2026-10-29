namespace DiabloDefenceRemastered.Tests.TestKit;

/// <summary>
/// 전수 공간 질의 (WP1 TestKit, <see cref="ISpatialQuery"/> 의 기준 구현). 적 저장소를 매번 다 훑는다.
/// - 대상: 조준 가능한 적(<see cref="Enemy.IsTargetable"/> — Active·Stunned, 입장·등장 중 제외).
/// - 원 질의: 중심이 원 안(거리 ≤ radius). 적 반경은 부르는 쪽이 radius 에 더한다(4.8).
/// - 캡슐: 중심에서 선분까지 거리 ≤ radius.
/// - 결과 순서 = 밀집 인덱스 순서. 칸이 모자라면 앞에서부터 채우고 실제 개수를 돌려준다.
/// WP2 SpatialHash 시험은 결과를 집합으로 비교한다(해시는 칸 순서).
/// </summary>
public sealed class BruteForceSpatial(World world, float cellSize = 32f) : ISpatialQuery
{
    public float CellSize { get; } = cellSize;

    public int Query(Vector2 center, float radius, Span<int> denseIdx)
    {
        var r2 = radius * radius;
        var count = 0;
        var enemies = world.Enemies.Items;
        for (var i = 0; i < enemies.Length; i++)
        {
            var e = enemies[i];
            if (!e.IsTargetable || Vector2.DistanceSquared(e.Pos, center) > r2) continue;
            if (count < denseIdx.Length) denseIdx[count] = i;
            count++;
        }
        return count;
    }

    public int QueryCapsule(Vector2 a, Vector2 b, float radius, Span<int> denseIdx)
    {
        var r2 = radius * radius;
        var count = 0;
        var enemies = world.Enemies.Items;
        for (var i = 0; i < enemies.Length; i++)
        {
            var e = enemies[i];
            if (!e.IsTargetable || MathX.DistanceSqToSegment(e.Pos, a, b) > r2) continue;
            if (count < denseIdx.Length) denseIdx[count] = i;
            count++;
        }
        return count;
    }

    public int CellCount(int cx, int cy)
    {
        var count = 0;
        foreach (var e in world.Enemies.Items)
        {
            if (!e.IsTargetable) continue;
            if ((int)MathF.Floor(e.Pos.X / CellSize) == cx && (int)MathF.Floor(e.Pos.Y / CellSize) == cy) count++;
        }
        return count;
    }
}
