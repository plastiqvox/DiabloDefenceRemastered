using DiabloDefenceRemastered.Core.Assets;

namespace DiabloDefenceRemastered.Core.Art;

/// <summary>벽 무리 (설계서 7.4). 칸마다 하나.</summary>
public enum WallGroup : byte
{
    /// <summary>걸을 수 있는 칸(벽 아님).</summary>
    None = 0,
    /// <summary>뒷둘레 벽: 뒤쪽 이웃 (tx−1,ty)·(tx,ty−1)·(tx−1,ty−1) 에 걸을 수 있는 칸이 하나도 없는 막힌 칸 — 0b 층.</summary>
    Back = 1,
    /// <summary>앞둘레 턱: 앞쪽 이웃 (tx+1,ty)·(tx,ty+1)·(tx+1,ty+1) 에 걸을 수 있는 칸이 하나도 없는 막힌 칸 — 5층.</summary>
    Front = 2,
    /// <summary>그 밖의 막힌 칸(안쪽 기둥·벽·창살·문 끝 조각) — 4층에서 유닛과 y 정렬.</summary>
    Inner = 3,
}

/// <summary>
/// 칸별 그리기 표 (설계서 7.4·7.6, 소유 WP5) — 맵을 열 때 한 번(칸마다 바이트 하나, 할당은 생성 때만).
/// 무리는 배치 출처(둘레·방·짧은 벽)를 보지 않고 이웃 판정만 쓴다(v0.6). 막힌 칸 = 걷기 막힘(<c>w</c> 창살 칸도 기둥 그림 항목 — 7.4).
/// 뒤·앞 이웃 모두 걸을 수 없는 칸(앞 턱·모서리처럼 두 줄로 막힌 벽의 바깥 줄)은 맵의 앞 반(tx + ty ≥ N − 1)이면 앞 턱, 뒤 반이면 뒷벽이다 —
/// 글자 그대로 "뒤 먼저" 로 두면 앞 턱 바깥 줄이 0b 층으로 가서 그 앞(화면 아래) 입장 중 유닛·맞닿은 안쪽 줄 그림과 순서가 뒤집힌다.
/// </summary>
public sealed class ArenaDrawTable
{
    private readonly WallGroup[] _groups;
    private readonly bool[] _overlay;

    private ArenaDrawTable(int size, WallGroup[] groups, bool[] overlay)
    {
        Size = size;
        _groups = groups;
        _overlay = overlay;
    }

    /// <summary>하위 칸 한 변 N.</summary>
    public int Size { get; }

    /// <summary>무리 나누기(격자 밖 이웃은 걸을 수 없는 칸). <paramref name="art"/> 가 있으면 아치 덧그림(kind 2) 칸도 표시한다.</summary>
    public static ArenaDrawTable Build(ArenaGrid grid, ArenaArt? art = null)
    {
        var n = grid.Size;
        var groups = new WallGroup[n * n];
        for (var ty = 0; ty < n; ty++)
            for (var tx = 0; tx < n; tx++)
            {
                if (grid.IsWalkable(tx, ty)) continue;
                // 둘레(테두리·문 끝 조각)가 아닌 막힌 칸은 늘 안쪽 — 2×2 로 다 막힌 안쪽 조각(제단 wwww, 2026-11-13)의 뒤 칸이 "앞 이웃 없음" 으로 앞 턱(5층),
                // 앞 칸이 "뒤 이웃 없음" 으로 뒷벽(0b 층)에 가 그리는 차례가 뒤집혔다(석관·촛대가 엉켜 보임 — 사용자 지적)
                if (!grid.IsBoundary(tx, ty)) { groups[ty * n + tx] = WallGroup.Inner; continue; }
                var back = grid.IsWalkable(tx - 1, ty) || grid.IsWalkable(tx, ty - 1) || grid.IsWalkable(tx - 1, ty - 1);
                var front = grid.IsWalkable(tx + 1, ty) || grid.IsWalkable(tx, ty + 1) || grid.IsWalkable(tx + 1, ty + 1);
                groups[ty * n + tx] = (back, front) switch
                {
                    (false, false) => tx + ty >= n - 1 ? WallGroup.Front : WallGroup.Back,   // 두 줄 벽의 바깥 줄 — 맵 앞 반이면 앞 턱
                    (false, true) => WallGroup.Back,
                    (true, false) => WallGroup.Front,
                    _ => WallGroup.Inner,
                };
            }
        var overlay = new bool[n * n];
        if (art is not null)
            foreach (var c in art.Cells)
                if (c.Kind == ArenaCellKind.Overlay && grid.IsInside(c.Tx, c.Ty)) overlay[c.Ty * n + c.Tx] = true;
        return new ArenaDrawTable(n, groups, overlay);
    }

    public WallGroup Group(int tx, int ty) => (uint)tx < (uint)Size && (uint)ty < (uint)Size ? _groups[ty * Size + tx] : WallGroup.None;

    /// <summary>그 칸에 아치 덧그림이 있다(7.4 — 칸 안 유닛의 정렬 키를 당긴다).</summary>
    public bool HasOverlay(int tx, int ty) => (uint)tx < (uint)Size && (uint)ty < (uint)Size && _overlay[ty * Size + tx];

    /// <summary>무리별 칸 수(로그·시험).</summary>
    public int Count(WallGroup group)
    {
        var n = 0;
        foreach (var g in _groups) if (g == group) n++;
        return n;
    }
}
