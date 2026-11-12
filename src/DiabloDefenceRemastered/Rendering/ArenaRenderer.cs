using DiabloDefenceRemastered.Core.Art;
using DiabloDefenceRemastered.Core.Assets;
using DiabloDefenceRemastered.Loading;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DiabloDefenceRemastered.Rendering;

/// <summary>칸 그림 하나(<c>arena.json</c> 의 <c>cells</c> 한 줄 + 그 칸의 벽 무리). 자원이 없으면 <see cref="Frame"/> 0 = 코드 그림.</summary>
public readonly record struct ArenaItem(int Tx, int Ty, ArenaCellKind Kind, int Frame, WallGroup Group);

/// <summary>
/// 원작 타일 아레나 그리기 (설계서 7.6, 소유 WP5). 맵을 열 때 <c>arena.json</c>·바닥 조각·기둥 아틀라스를 받고 벽 무리를 한 번 계산한다(<see cref="ArenaDrawTable"/>).
/// <list type="bullet">
/// <item>0층 바닥: 화면에 걸친 512 조각만. 0b 층 뒷둘레 벽·5층 앞둘레 턱은 칸 줄(ty, tx) 순 — <c>cells</c> 가 이미 그 순서다. 4층 항목(안쪽 벽·바닥 위 조각·아치 덧그림)은
/// <see cref="CollectLayer4"/> 로 유닛과 같은 배열에 넣어 정렬한다.</item>
/// <item>그리는 자리·정렬 키·가림은 모두 월드 px(그림 배율 s 는 SpriteBatch 변환 행렬이 곱한다 — 7.1).</item>
/// <item>자원이 없으면(개발 실행) 코드 마름모·기둥 상자로 그린다 — 배치·충돌 확인용(<c>--preview tiles</c>).</item>
/// <item><c>layout_hash</c> 가 저장소 배치와 다르면 예외 — 시작 검사(1.4 끝)가 이미 막았어야 하는 버그다(7.6 "낡은 그림").</item>
/// </list>
/// </summary>
public sealed class ArenaRenderer
{
    private readonly List<ArenaItem> _items = [];
    private readonly List<int> _visibleWalls = [];

    public ArenaRenderer(ArenaGrid grid, MapArt? art)
    {
        if (art is not null && !string.Equals(art.Arena.LayoutHash, grid.LayoutHash, StringComparison.Ordinal))
            throw new InvalidOperationException($"맵 '{grid.Map.Id}' 그림이 낡았다(layout_hash {art.Arena.LayoutHash} ≠ {grid.LayoutHash}) — 시작 검사가 막았어야 함");
        Grid = grid;
        Art = art;
        Table = ArenaDrawTable.Build(grid, art?.Arena);
        if (art is not null)
            foreach (var c in art.Arena.Cells) _items.Add(new ArenaItem(c.Tx, c.Ty, c.Kind, c.Frame, Group(c)));
        else
            for (var ty = 0; ty < grid.Size; ty++)
                for (var tx = 0; tx < grid.Size; tx++)
                    if (!grid.IsWalkable(tx, ty)) _items.Add(new ArenaItem(tx, ty, ArenaCellKind.Wall, 0, Table.Group(tx, ty)));
    }

    public ArenaGrid Grid { get; }
    public MapArt? Art { get; }
    public ArenaDrawTable Table { get; }
    public IsoMath Iso => Grid.Iso;
    public IReadOnlyList<ArenaItem> Items => _items;

    /// <summary>이번 프레임 화면에 걸친 벽 항목(<see cref="Items"/> 번호, 0b·4·5층 — 안개 3단계가 다시 쓴다).</summary>
    public IReadOnlyList<int> VisibleWalls => _visibleWalls;

    private WallGroup Group(ArenaCell c)
    {
        if (c.Kind != ArenaCellKind.Wall) return WallGroup.Inner;   // 걸을 수 있는 칸의 조각은 4층
        var g = Table.Group(c.Tx, c.Ty);
        return g == WallGroup.None ? WallGroup.Inner : g;
    }

    /// <summary>화면(월드 px) 안 벽 목록을 새로 모은다(프레임마다 한 번, 할당 0).</summary>
    public void Prepare(RectF view)
    {
        _visibleWalls.Clear();
        for (var i = 0; i < _items.Count; i++)
        {
            var it = _items[i];
            if (it.Kind != ArenaCellKind.Wall) continue;
            if (WallFade.Overlaps(ItemRect(it), view)) _visibleWalls.Add(i);
        }
    }

    /// <summary>항목 그림 사각형(월드 px). 기둥 칸 = 64 × Hc(7.6), 자원이 있으면 잘라 낸 사각형.</summary>
    public RectF ItemRect(in ArenaItem it)
    {
        if (Art is { ColumnMeta: { } meta, Columns: { } action } && it.Frame >= 1 && it.Frame <= action.Frames)
        {
            var r = action.Rect(0, it.Frame);
            var tl = DrawOrder.ColumnTopLeft(Iso, it.Tx, it.Ty);
            var m = meta.Scale;
            return new RectF(tl.X + r.Ox / (float)m, tl.Y + r.Oy / (float)m, tl.X + (r.Ox + r.W) / (float)m, tl.Y + (r.Oy + r.H) / (float)m);
        }
        return DrawOrder.ColumnRect(Iso, it.Tx, it.Ty);
    }

    public float SortY(in ArenaItem it) => DrawOrder.CellSortY(Iso, new ArenaCell(it.Tx, it.Ty, it.Kind, it.Frame));

    /// <summary>0층 바닥(불투명 — 맵 밖은 비움).</summary>
    public void DrawFloor(SpriteBatch batch, RectF view, Primitives prim)
    {
        if (Art is { } art)
        {
            for (var i = 0; i < art.Arena.Floor.Count; i++)
            {
                if (art.Floors[i] is not { } tex) continue;
                var p = art.Arena.Floor[i].Pos;
                var rect = new RectF(p.X, p.Y, p.X + ArenaArt.ChunkSize, p.Y + ArenaArt.ChunkSize);
                if (!WallFade.Overlaps(rect, view)) continue;
                batch.Draw(tex, new Rectangle(p.X, p.Y, ArenaArt.ChunkSize, ArenaArt.ChunkSize), Color.White);
            }
            return;
        }
        // 코드 그림: 걸을 수 있는 칸 마름모(바닥 섞기 없이 두 단 회색)
        for (var ty = 0; ty < Grid.Size; ty++)
            for (var tx = 0; tx < Grid.Size; tx++)
            {
                if (!Grid.IsWalkable(tx, ty)) continue;
                var c = Iso.CellCenter(tx, ty);
                if (c.X + 32 < view.X0 || c.X - 32 > view.X1 || c.Y + 16 < view.Y0 || c.Y - 16 > view.Y1) continue;
                var shade = (byte)(((tx + ty) & 1) == 0 ? 70 : 62);
                batch.Draw(prim.Diamond, new Rectangle((int)c.X - 32, (int)c.Y - 16, 64, 32), new Color(shade, shade, shade, (byte)255));
            }
    }

    /// <summary>무리 하나의 벽(0b 층 = 뒷둘레, 5층 = 앞둘레)을 칸 줄 순으로. <paramref name="alpha"/> 가 항목마다 알파(가림 반투명)를 준다.</summary>
    public void DrawGroup(SpriteBatch batch, WallGroup group, Primitives prim, Func<int, float>? alpha = null)
    {
        foreach (var i in _visibleWalls)
        {
            var it = _items[i];
            if (it.Group != group) continue;
            DrawItem(batch, i, prim, alpha?.Invoke(i) ?? 1f);
        }
    }

    /// <summary>4층 항목(안쪽 벽·바닥 위 조각·아치 덧그림)을 정렬 배열에 넣는다.</summary>
    public void CollectLayer4(RectF view, List<DrawItem> items)
    {
        for (var i = 0; i < _items.Count; i++)
        {
            var it = _items[i];
            if (it.Kind == ArenaCellKind.Wall && it.Group != WallGroup.Inner) continue;
            if (!WallFade.Overlaps(ItemRect(it), view)) continue;
            var kind = it.Kind switch { ArenaCellKind.Wall => DrawKind.Wall, ArenaCellKind.FloorPiece => DrawKind.FloorPiece, _ => DrawKind.Overlay };
            items.Add(new DrawItem(SortY(it), DrawOrder.CellTiebreak(Iso, it.Tx, it.Ty), kind, i));
        }
    }

    /// <summary>안개 벽 값 조각 폭(월드 px) — 빛 RT 텍셀 하나(월드 2px).</summary>
    public const float ShadeSliceWidth = 2f;

    /// <summary>
    /// 안개 벽 찍기(7.10 3단계): 기둥을 <see cref="ShadeSliceWidth"/> 폭 세로 조각으로 나눠 알파 = 칸 가운데 <paramref name="center"/> 에서 화면 x 로 ±32(이웃 기둥 가운데)의
    /// <paramref name="left"/>·<paramref name="right"/> 까지 선형. 이웃 기둥도 같은 식이라 이음매에서 값이 같아 칸 단위 계단이 없다. 자원이 없으면 가운데 값 하나로.
    /// </summary>
    public void DrawItemShaded(SpriteBatch batch, int index, Primitives prim, float center, float left, float right)
    {
        var it = _items[index];
        if (Art is { ColumnMeta: { } meta, Columns: { } action } art && it.Frame >= 1 && it.Frame <= action.Frames)
        {
            var r = action.Rect(0, it.Frame);
            if (r.IsEmpty) return;
            var page = art.ColumnPages[r.Page];
            var dest = ItemRect(it);
            var cx = Iso.CellCenter(it.Tx, it.Ty).X;
            var m = meta.Scale;
            var step = Math.Max(1, (int)(ShadeSliceWidth * m));
            for (var sx = 0; sx < r.W; sx += step)
            {
                var w = Math.Min(step, r.W - sx);
                var x0 = dest.X0 + sx / (float)m;
                var x1 = x0 + w / (float)m;
                var u = ((x0 + x1) * 0.5f - cx) / 32f;
                var level = u < 0f ? center + (left - center) * MathF.Min(1f, -u) : center + (right - center) * MathF.Min(1f, u);
                if (level <= 0f) continue;
                BatchX.Blit(batch, page, new RectF(x0, dest.Y0, x1, dest.Y1), new Rectangle(r.X + sx, r.Y, w, r.H), new Color(255, 255, 255, (int)(MathF.Min(1f, level) * 255f)));
            }
            return;
        }
        if (center > 0f) DrawItem(batch, index, prim, 1f, new Color(255, 255, 255, (int)(MathF.Min(1f, center) * 255f)));
    }

    /// <summary>항목 하나 그리기(틴트 = 흰색 × 알파 — premultiplied). <paramref name="tint"/> 가 있으면 그 색(안개 모양 찍기).</summary>
    public void DrawItem(SpriteBatch batch, int index, Primitives prim, float alpha, Color? tint = null)
    {
        var it = _items[index];
        var color = tint ?? new Color(alpha, alpha, alpha, alpha);
        if (Art is { ColumnMeta: { } meta, Columns: { } action } art && it.Frame >= 1 && it.Frame <= action.Frames)
        {
            var r = action.Rect(0, it.Frame);
            if (r.IsEmpty) return;
            BatchX.Blit(batch, art.ColumnPages[r.Page], ItemRect(it), new Rectangle(r.X, r.Y, r.W, r.H), color);
            return;
        }
        if (it.Kind != ArenaCellKind.Wall) return;
        // 코드 기둥: 칸 마름모를 64px 위로 올린 윗면 + 옆면 상자
        var c = Iso.CellCenter(it.Tx, it.Ty);
        var side = tint ?? new Color((byte)(48 * alpha), (byte)(40 * alpha), (byte)(34 * alpha), (byte)(255 * alpha));
        var top = tint ?? new Color((byte)(110 * alpha), (byte)(96 * alpha), (byte)(80 * alpha), (byte)(255 * alpha));
        batch.Draw(prim.Pixel, new Rectangle((int)c.X - 32, (int)c.Y - 64, 64, 64), side);
        batch.Draw(prim.Diamond, new Rectangle((int)c.X - 32, (int)c.Y - 80, 64, 32), top);
        batch.Draw(prim.Diamond, new Rectangle((int)c.X - 32, (int)c.Y - 16, 64, 32), side);
    }
}
