namespace DiabloDefenceRemastered.Core.Art;

/// <summary>창살 그림을 빌려 올 자리: <paramref name="Map"/> 의 메가타일 <paramref name="Mega"/> 가 그 창살 조각이다(그 맵 그림의 하위 칸 기둥을 쓴다).</summary>
public sealed record GrateSource(MapDef Map, Int2 Mega, int Tile);

/// <summary>
/// 문 봉인 = 원작 L1 창살 (v0.9.4 사용자 결정 V13, 기획 10.8 "봉인 표시"·설계서 7.11 — 소유 WP5, 단계 2 wp5). 그리기는 게임 <c>WorldRenderer</c>, 이 클래스는 그래픽 없는 규칙.
/// <list type="bullet">
/// <item>조각: 문 틈이 놓인 변에 맞춰 가로 <c>grate_h</c>(ne·sw — 변이 x 를 따라 놓임)·세로 <c>grate_v</c>(nw·se). 문 틈 메가 둘에 조각 하나씩, 벽 높이.
/// 거울판 맵은 변이 바뀌므로(nw ↔ ne, se ↔ sw) 조각도 타일셋 짝 표(<c>grate_h ↔ grate_v</c>)대로 바뀐다.</item>
/// <item>그림: 가져오기는 문 틈에 창살을 굽지 않는다(문 틈은 열린 바닥 13). 그래서 <b>같은 액트 맵 풀에서 그 조각을 쓴 맵</b>(대성당 D "창살 방"·그 거울판·큰 방)의
/// 맵 그림에서 그 메가의 하위 칸 기둥을 빌린다(<see cref="FindSource"/> — 가져오기 도구·자원 형식을 바꾸지 않는 길, 13장 WP5 단계 2 기록). 못 찾거나 맵 그림이 없으면
/// 단계 1 의 코드 그림(붉은 바닥 빛·낮은 창살·룬)으로 돌아간다.</item>
/// <item>열림: 출구로 열린 문의 창살은 문 단계에 들어간 틱부터 <see cref="OpenSeconds"/> 동안 바닥선에서 잘려 아래로 내려간다(<see cref="Sink"/> 0 → 1, 스테이지 틱 — 3.4).
/// 들어온 문·고르지 않은 문(<c>Exits</c> 에 없음)은 그대로 남는다. 충돌·시야·방 흐름은 바꾸지 않는다(기획 2.5 "봉인 표시만").</item>
/// <item>적 드나듦: 입장 중인 적이 문 틈 가까이 있으면 그 문 창살은 흐리게(<see cref="EnemyPassing"/>).</item>
/// </list>
/// </summary>
public static class GateGrates
{
    /// <summary>창살이 내려가는 시간(초, 기획 10.8 V13).</summary>
    public const float OpenSeconds = 0.6f;

    public static int OpenTicks => MathX.SecondsToTicks(OpenSeconds, World.Dt);

    /// <summary>변 → 창살 조각 이름(타일셋 <c>pieces</c>).</summary>
    public static string PieceName(GateSide side) => side is GateSide.Ne or GateSide.Sw ? "grate_h" : "grate_v";

    /// <summary>
    /// 창살이 내려간 정도: 0 = 그대로(닫힘·들어온 문·고르지 않은 문), 1 = 다 내려가 없음. 방 없는 월드는 문 봉인이 없어 1.
    /// 출구 문은 문 단계 첫 틱부터 <see cref="OpenTicks"/> 동안 0 → 1, 나감 단계부터는 1.
    /// </summary>
    public static float Sink(RoomState? room, string gateId, int stageTick)
    {
        if (room is null) return 1f;
        if (GateSeals.IsSealed(room, gateId)) return 0f;
        if (room.Phase > RoomPhase.Doors) return 1f;
        var t = (stageTick - room.PhaseTick) / (float)Math.Max(1, OpenTicks);
        return Math.Clamp(t, 0f, 1f);
    }

    /// <summary>적이 드나드는 문 창살을 흐리게 하는 거리(px, 문 틈 칸 중심에서 적 발까지 — 하위 칸 둘쯤). 첫 값.</summary>
    public const float PassRadius = 72f;

    /// <summary>
    /// 입장 중인 적(<see cref="LifeState.Entering"/>)이 이 문 틈 칸 가까이(<see cref="PassRadius"/>) 있나. 기획 2.4 "적은 닫힌 출구 문으로도 들어온다" 는 그대로라,
    /// 벽 높이 창살을 적이 뚫고 지나가는 장면이 되지 않게 그동안 창살을 흐리게 그린다(<c>WallFade.Alpha</c> — 안쪽 벽 가림과 같은 값). 그림만 바뀐다.
    /// 문 밖에서 다가오는 동안은 창살 너머(또는 앞)에 보인다.
    /// </summary>
    public static bool EnemyPassing(GateZone zone, IsoMath iso, ReadOnlySpan<Enemy> enemies)
    {
        ArgumentNullException.ThrowIfNull(zone);
        const float r2 = PassRadius * PassRadius;
        foreach (var e in enemies)
        {
            if (!e.IsAlive || e.Life != LifeState.Entering) continue;
            foreach (var c in zone.Cells)
                if (Vector2.DistanceSquared(iso.CellCenter(c.X, c.Y), e.Pos) <= r2) return true;
        }
        return false;
    }

    /// <summary>문 틈의 메가타일(문 틈 칸을 메가로 묶은 것, 칸 순).</summary>
    public static IReadOnlyList<Int2> Megas(GateZone zone)
    {
        ArgumentNullException.ThrowIfNull(zone);
        var list = new List<Int2>(2);
        foreach (var c in zone.Cells)
        {
            var m = new Int2(c.X / 2, c.Y / 2);
            if (!list.Contains(m)) list.Add(m);
        }
        return list;
    }

    /// <summary>
    /// <paramref name="maps"/>(앞에서부터 — 액트 맵 풀·거울판) 중 타일셋 조각 <paramref name="piece"/> 를 쓴 첫 메가. 타일셋에 그 이름이 없거나 아무 맵도 안 쓰면 null.
    /// 맵 그림은 메가 자리로 하위 칸 기둥을 찾으므로 배치만 풀면 된다(<see cref="ArenaLayout.Expand(MapDef)"/>).
    /// </summary>
    public static GrateSource? FindSource(IEnumerable<MapDef> maps, string piece)
    {
        ArgumentNullException.ThrowIfNull(maps);
        foreach (var map in maps)
        {
            if (!map.Tileset.IsBound || !map.Tileset.Def.Pieces.TryGetValue(piece, out var tile)) continue;
            ArenaGrid grid;
            try { grid = ArenaLayout.Expand(map); }
            catch (InvalidDataException) { continue; }
            for (var my = 0; my < grid.MegaSize; my++)
                for (var mx = 0; mx < grid.MegaSize; mx++)
                    if (grid.MegaTile(mx, my) == tile) return new GrateSource(map, new Int2(mx, my), tile);
        }
        return null;
    }
}
