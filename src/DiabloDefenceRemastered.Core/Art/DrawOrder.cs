using DiabloDefenceRemastered.Core.Assets;

namespace DiabloDefenceRemastered.Core.Art;

/// <summary>4층 항목 종류(설계서 7.4). 렌더가 <see cref="DrawItem.Index"/> 를 어느 목록에서 찾을지.</summary>
public enum DrawKind : byte { Hero, Enemy, Wall, FloorPiece, Overlay, Merchant, Grate }

/// <summary>
/// 4층 정렬 항목 (설계서 7.4): 키 = <see cref="SortY"/>, 같으면 <see cref="Tiebreak"/>(개체 ID·칸 번호) — 같은 y 에서 깜빡임 없음.
/// 배열에 모아 <c>Array.Sort(items, DrawItem.Comparer)</c>(할당 0).
/// </summary>
public readonly record struct DrawItem(float SortY, int Tiebreak, DrawKind Kind, int Index)
{
    public static readonly IComparer<DrawItem> Comparer = Comparer<DrawItem>.Create(static (a, b) =>
    {
        var c = a.SortY.CompareTo(b.SortY);
        if (c != 0) return c;
        c = a.Tiebreak.CompareTo(b.Tiebreak);
        return c != 0 ? c : ((int)a.Kind).CompareTo((int)b.Kind);
    });
}

/// <summary>
/// 정렬 키·기둥 자리 (설계서 7.4·7.6, 소유 WP5). 모두 월드 px — 그림 배율 s 와 무관(자리·정렬·가림 판정은 월드 px 그대로, 7.6 v0.8).
/// </summary>
public static class DrawOrder
{
    /// <summary>
    /// 유닛 정렬 키 = 발 y. 단 <b>덧그림이 있는 칸 안에 발이 있으면</b> min(발 y, 그 칸 중심 y − 0.5) — 그 칸의 아치 덧그림이 덮게(7.4).
    /// </summary>
    public static float UnitSortY(ArenaDrawTable table, IsoMath iso, Vector2 foot)
    {
        var cell = iso.CellOf(foot);
        if (!table.HasOverlay(cell.X, cell.Y)) return foot.Y;
        return MathF.Min(foot.Y, iso.CellCenter(cell.X, cell.Y).Y - 0.5f);
    }

    /// <summary>막힌 칸의 기둥 정렬 키 = 그 칸 바닥 마름모 중심 y.</summary>
    public static float WallSortY(IsoMath iso, int tx, int ty) => iso.CellCenter(tx, ty).Y;

    /// <summary>걸을 수 있는 칸의 바닥 위 조각(덤불·아치 윗부분) = 위 꼭짓점 y(중심 − 16).</summary>
    public static float FloorPieceSortY(IsoMath iso, int tx, int ty) => iso.CellCenter(tx, ty).Y - IsoMath.HalfHeight;

    /// <summary>아치 덧그림 = 그 칸 중심 y.</summary>
    public static float OverlaySortY(IsoMath iso, int tx, int ty) => iso.CellCenter(tx, ty).Y;

    /// <summary>칸 그림 하나의 정렬 키(<c>arena.json</c> 의 kind 그대로).</summary>
    public static float CellSortY(IsoMath iso, ArenaCell cell) => cell.Kind switch
    {
        ArenaCellKind.Wall => WallSortY(iso, cell.Tx, cell.Ty),
        ArenaCellKind.FloorPiece => FloorPieceSortY(iso, cell.Tx, cell.Ty),
        _ => OverlaySortY(iso, cell.Tx, cell.Ty),
    };

    /// <summary>칸 번호(정렬 Tiebreak) = ty·N + tx.</summary>
    public static int CellTiebreak(IsoMath iso, int tx, int ty) => ty * iso.N + tx;

    /// <summary>
    /// 기둥 그림 왼쪽 위(월드 px) = (C.x − 32, C.y + 16 − Hc) — 그림 줄 0..Hc−1 이 C.y − (Hc − 16) .. C.y + 15(7.6). 바닥 조각과 같은 <c>CellCenter</c> 라 1px 틈·겹침이 없다.
    /// </summary>
    public static Vector2 ColumnTopLeft(IsoMath iso, int tx, int ty)
    {
        var c = iso.CellCenter(tx, ty);
        return new Vector2(c.X - IsoMath.HalfWidth, c.Y + IsoMath.HalfHeight - iso.ColumnHeight);
    }

    /// <summary>기둥 그림 사각형(월드 px, 64 × Hc).</summary>
    public static RectF ColumnRect(IsoMath iso, int tx, int ty)
    {
        var p = ColumnTopLeft(iso, tx, ty);
        return new RectF(p.X, p.Y, p.X + 2 * IsoMath.HalfWidth, p.Y + iso.ColumnHeight);
    }
}

/// <summary>
/// 주인공 앞 벽 반투명 (설계서 7.6 "가림", 기획 10.2 v0.5, 소유 WP5). 4·5층 벽 항목(아치 덧그림 포함)을 모을 때 칸마다 한 번(할당 0).
/// 0b 층 뒷둘레 벽은 늘 주인공 뒤라 부르지 않는다.
/// </summary>
public static class WallFade
{
    /// <summary>가림 알파.</summary>
    public const float Alpha = 0.5f;

    /// <summary>
    /// 반투명인가: 벽이 주인공보다 앞(4층은 정렬 키 &gt; 주인공 발 y, 5층 앞둘레는 늘 앞)이고 ① 그림 사각형이 주인공 그림 사각형과 겹치거나
    /// ② 그 칸 마름모 중심이 주인공 발에서 <paramref name="radius"/> 안(<c>wall_fade.mode = hero</c> 면 ① 만).
    /// </summary>
    /// <param name="radius">맵 <c>wall_fade.radius</c> 가 있으면 그 값, 없으면 지금 <c>hero_light.radius</c>.</param>
    public static bool Fades(bool frontLedge, float itemSortY, RectF itemRect, Vector2 cellCenter,
                             Vector2 heroFoot, RectF heroRect, float radius, WallFadeMode mode)
    {
        if (!frontLedge && itemSortY <= heroFoot.Y) return false;
        if (Overlaps(itemRect, heroRect)) return true;
        if (mode == WallFadeMode.Hero) return false;
        return Vector2.DistanceSquared(cellCenter, heroFoot) <= radius * radius;
    }

    /// <summary>맵 값이 있으면 그것, 없으면 빛 반경.</summary>
    public static float Radius(WallFadeDef def, float heroLightRadius) => def.Radius ?? heroLightRadius;

    public static bool Overlaps(RectF a, RectF b) => a.X0 < b.X1 && b.X0 < a.X1 && a.Y0 < b.Y1 && b.Y0 < a.Y1;
}

/// <summary>
/// 그림 한 프레임의 화면 자리 (설계서 6.3 "그리기", 소유 WP5) — 월드 px. 그 뒤 타깃 변환에서 그림 배율 s 를 곱한다(7.1).
/// </summary>
public static class SpriteLayout
{
    /// <summary>
    /// atlas: 왼쪽 위 = 위치 − origin × k + (ox, oy) × k / m, 크기 = (w, h) × k / m(k = 정의의 크기 배율, m = 메타 <c>scale</c>). 빈 프레임은 크기 0.
    /// <c>origin</c> 은 칸 기준 월드 px(정의 값)이라 m 으로 나누지 않는다.
    /// </summary>
    public static RectF Atlas(Vector2 pos, Vector2 origin, SpriteRect rect, float scale, int metaScale)
    {
        var k = scale / metaScale;
        var x = pos.X - origin.X * scale + rect.Ox * k;
        var y = pos.Y - origin.Y * scale + rect.Oy * k;
        return new RectF(x, y, x + rect.W * k, y + rect.H * k);
    }

    /// <summary>grid: 왼쪽 위 = 위치 − origin × k, 크기 = frame × k(월드 px — 그림 픽셀 배율은 원본 사각형 쪽, <see cref="GridSource"/>).</summary>
    public static RectF Grid(Vector2 pos, Vector2 origin, Int2 frame, float scale)
    {
        var x = pos.X - origin.X * scale;
        var y = pos.Y - origin.Y * scale;
        return new RectF(x, y, x + frame.X * scale, y + frame.Y * scale);
    }

    /// <summary>grid 의 원본 사각형(그림 픽셀): 열 = 방향, 행 = 프레임(<paramref name="frame1"/> 1부터).</summary>
    public static RectI GridSource(Int2 frame, int dir, int frame1, int fileScale = 1)
    {
        var w = frame.X * fileScale;
        var h = frame.Y * fileScale;
        var x = dir * w;
        var y = (frame1 - 1) * h;
        return new RectI(x, y, x + w, y + h);
    }

    /// <summary>방향마다 발이 다르면 그 값(<c>origin_by_dir</c>), 아니면 <c>origin</c>.</summary>
    public static Vector2 Origin(SpriteDef sprite, int dir) =>
        sprite.OriginByDir is { } byDir && (uint)dir < (uint)byDir.Count ? byDir[dir] : sprite.Origin;
}
