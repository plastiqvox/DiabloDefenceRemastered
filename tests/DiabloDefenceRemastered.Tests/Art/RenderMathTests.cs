using DiabloDefenceRemastered.Core.Art;
using DiabloDefenceRemastered.Core.Assets;
using DiabloDefenceRemastered.Tests.Data;

namespace DiabloDefenceRemastered.Tests.Art;

/// <summary>그리기 규칙의 그래픽 없는 계산(WP5 — 7.1 배율·창, 7.4 벽 무리·정렬 키, 7.6 기둥 자리·가림, 6.3 그림 자리).</summary>
public class RenderMathTests
{
    // ── 7.1 그림 배율 s·창 ─────────────────────────────────────────

    [Fact]
    public void ArtScale_WorldTargetAndDrawScale()
    {
        Assert.Equal(new Int2(960, 540), ArtScale.WorldTarget(1));
        Assert.Equal(new Int2(1920, 1080), ArtScale.WorldTarget(2));
        Assert.Equal(1, ArtScale.DrawScale(1, 1));
        Assert.Equal(2, ArtScale.DrawScale(2, 1));     // 원본 그림은 2배 점 확대
        Assert.Equal(1, ArtScale.DrawScale(2, 2));     // 확대한 그림은 1:1
        Assert.Throws<InvalidOperationException>(() => ArtScale.DrawScale(1, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => ArtScale.WorldTarget(0));

        // 타깃 픽셀 = 1/s 월드 px
        Assert.Equal(new Vector2(10f, 3f), ArtScale.FloorToTarget(new Vector2(10.7f, 3.2f), 1));
        Assert.Equal(new Vector2(10.5f, 3f), ArtScale.FloorToTarget(new Vector2(10.7f, 3.2f), 2));
        Assert.Equal(new Vector2(11f, 3f), ArtScale.RoundToTarget(new Vector2(10.7f, 3.2f), 1));
        Assert.Equal(new Vector2(10.5f, 3.0f), ArtScale.RoundToTarget(new Vector2(10.6f, 3.2f), 2));
    }

    [Fact]
    public void PresentMath_IntegerScale_Letterbox_ScreenToWorld()
    {
        Assert.Equal(1, PresentMath.IntegerScale(new Int2(960, 540), 1));
        Assert.Equal(2, PresentMath.IntegerScale(new Int2(1920, 1080), 1));
        Assert.Equal(2, PresentMath.IntegerScale(new Int2(1280, 720), 1));     // ceil(1.33)
        Assert.Equal(1, PresentMath.IntegerScale(new Int2(1920, 1080), 2));    // s = 2 면 1920×1080 창은 k = 1(7.1)
        Assert.Equal(1, PresentMath.IntegerScale(new Int2(300, 200), 1));

        Assert.Equal(new RectI(0, 0, 1920, 1080), PresentMath.Letterbox(new Int2(1920, 1080)));
        Assert.Equal(new RectI(0, 60, 1280, 780), PresentMath.Letterbox(new Int2(1280, 840)));    // 위아래 띠
        Assert.Equal(new RectI(160, 0, 1120, 540), PresentMath.Letterbox(new Int2(1280, 540)));   // 좌우 띠
        Assert.Equal(2f, PresentMath.UiScale(new Int2(1920, 1080)));

        var present = new Int2(1280, 840);
        var v = PresentMath.ScreenToVirtual(new Vector2(640, 420), present);
        Assert.Equal(480f, v.X, 3);
        Assert.Equal(270f, v.Y, 3);
        var back = PresentMath.VirtualToScreen(v, present);
        Assert.Equal(640f, back.X, 3);
        Assert.Equal(420f, back.Y, 3);
        Assert.Equal(new Vector2(100f + 480f, 50f + 270f), PresentMath.ScreenToWorld(new Vector2(640, 420), present, new Vector2(100, 50)));
    }

    // ── 7.4 벽 무리·정렬 ───────────────────────────────────────────

    [Fact]
    public void WallGroups_RealCathedral_BackFrontInner()
    {
        var grid = ArenaLayout.Expand(RealData.Cathedral, RealData.L1);
        var t = ArenaDrawTable.Build(grid);
        var n = grid.Size;

        // 뒷벽(ne 변 메가 [7,0]·nw 변 메가 [0,7] — 반만 막는 벽)은 뒷둘레, 앞 턱(se 변 [19,7]·sw 변 [7,19] — 두 줄 XXXX)은 두 줄 다 앞둘레,
        // 가운데 기둥(메가 [7,9] 의 위-왼 칸)은 안쪽. 문 틈(변 가운데)·안쪽 방이 덮은 자리는 피했다
        Assert.Equal(WallGroup.Back, t.Group(14, 0));
        Assert.Equal(WallGroup.Back, t.Group(0, 14));
        Assert.Equal(WallGroup.Front, t.Group(39, 14));             // 바깥 줄(뒤·앞 이웃 모두 막힘 — 맵 앞 반)
        Assert.Equal(WallGroup.Front, t.Group(38, 14));             // 안쪽 줄
        Assert.Equal(WallGroup.Front, t.Group(14, 39));
        Assert.Equal(WallGroup.Front, t.Group(14, 38));
        Assert.Equal(WallGroup.Inner, t.Group(14, 18));             // 기둥 [7,9] → 하위 (14,18)
        Assert.Equal(WallGroup.None, t.Group(n / 2, n / 2));        // 가운데 바닥
        Assert.Equal(WallGroup.None, t.Group(-1, 0));               // 격자 밖

        // 모든 막힌 칸은 무리가 있고 걸을 수 있는 칸은 없다
        for (var ty = 0; ty < n; ty++)
            for (var tx = 0; tx < n; tx++)
                Assert.Equal(!grid.IsWalkable(tx, ty), t.Group(tx, ty) != WallGroup.None);
        Assert.True(t.Count(WallGroup.Inner) > 0);
    }

    [Fact]
    public void DrawOrder_Keys_AndOverlayPull()
    {
        var iso = new IsoMath(40, 160);
        var c = iso.CellCenter(10, 12);
        Assert.Equal(c.Y, DrawOrder.WallSortY(iso, 10, 12));
        Assert.Equal(c.Y - 16, DrawOrder.FloorPieceSortY(iso, 10, 12));
        Assert.Equal(c.Y, DrawOrder.CellSortY(iso, new ArenaCell(10, 12, ArenaCellKind.Overlay, 1)));

        // 기둥 그림 = (C.x − 32, C.y + 16 − 160), 64 × 160
        Assert.Equal(new Vector2(c.X - 32, c.Y - 144), DrawOrder.ColumnTopLeft(iso, 10, 12));
        var rect = DrawOrder.ColumnRect(iso, 10, 12);
        Assert.Equal(64f, rect.Width);
        Assert.Equal(160f, rect.Height);
        Assert.Equal(c.Y + 16, rect.Y1);   // 마지막 픽셀 줄 = C.y + 15

        // 덧그림 칸 안의 발은 칸 중심 − 0.5 로 당긴다(앞 칸 유닛은 그대로)
        var grid = ArenaLayout.Expand(RealData.Cathedral, RealData.L1);
        var art = new ArenaArt { Map = "cathedral_arena", LayoutHash = grid.LayoutHash, Cells = [new ArenaCell(19, 19, ArenaCellKind.Overlay, 1)] };
        var table = ArenaDrawTable.Build(grid, art);
        var center = grid.Iso.CellCenter(19, 19);
        Assert.True(table.HasOverlay(19, 19));
        Assert.Equal(center.Y - 0.5f, DrawOrder.UnitSortY(table, grid.Iso, center + new Vector2(0, 8)));
        Assert.Equal(center.Y - 4f, DrawOrder.UnitSortY(table, grid.Iso, center + new Vector2(0, -4)));
        var front = grid.Iso.CellCenter(20, 20);
        Assert.Equal(front.Y, DrawOrder.UnitSortY(table, grid.Iso, front));

        // 정렬: y → Tiebreak → 종류
        DrawItem[] items = [new(10, 5, DrawKind.Enemy, 0), new(10, 2, DrawKind.Wall, 1), new(9.5f, 9, DrawKind.Hero, 0)];
        Array.Sort(items, DrawItem.Comparer);
        Assert.Equal([DrawKind.Hero, DrawKind.Wall, DrawKind.Enemy], items.Select(i => i.Kind));
    }

    [Fact]
    public void WallFade_FrontOnly_OverlapOrRadius()
    {
        var hero = new Vector2(500, 500);
        var heroRect = new RectF(470, 420, 530, 500);
        var near = new RectF(480, 380, 544, 540);
        var far = new RectF(900, 380, 964, 540);
        // 주인공 뒤(정렬 키 ≤ 발 y)는 반투명이 아니다
        Assert.False(WallFade.Fades(false, 490, near, new Vector2(512, 495), hero, heroRect, 420, WallFadeMode.Light));
        // 앞이고 그림이 겹침
        Assert.True(WallFade.Fades(false, 520, near, new Vector2(512, 520), hero, heroRect, 420, WallFadeMode.Light));
        // 앞이고 안 겹치지만 빛 반경 안 → light 모드만
        Assert.True(WallFade.Fades(false, 520, far, new Vector2(800, 520), hero, heroRect, 420, WallFadeMode.Light));
        Assert.False(WallFade.Fades(false, 520, far, new Vector2(800, 520), hero, heroRect, 420, WallFadeMode.Hero));
        // 반경 밖
        Assert.False(WallFade.Fades(false, 520, far, new Vector2(1000, 520), hero, heroRect, 420, WallFadeMode.Light));
        // 5층 앞 턱은 늘 앞
        Assert.True(WallFade.Fades(true, 0, near, new Vector2(512, 520), hero, heroRect, 0, WallFadeMode.Hero));
        Assert.Equal(300f, WallFade.Radius(new WallFadeDef { Radius = 300 }, 420));
        Assert.Equal(420f, WallFade.Radius(new WallFadeDef(), 420));
    }

    [Fact]
    public void RealCathedral_IsNormalAndBossRoomMap_PassesRoomChecks()
    {
        // 11.1 WP5 맵 데이터: 단계 1 의 모든 일반 방 + 액트 1 보스 방(13.5-23 ② 를 slice 스테이지가 통과하게)
        var map = RealData.Cathedral;
        Assert.Equal([RoomKind.Normal, RoomKind.Boss], map.RoomKinds);
        var errors = new List<LayoutError>();
        ArenaLayout.ValidateRooms(map, ArenaLayout.Expand(map, RealData.L1), errors);
        Assert.Empty(errors.Where(e => !e.Warning).Select(e => $"{e.Path}: {e.Message}"));
    }

    // ── 6.3 그림 자리 ──────────────────────────────────────────────

    [Fact]
    public void SpriteLayout_AtlasAndGrid_WithMetaScale()
    {
        var pos = new Vector2(1000, 600);
        var rect = new SpriteRect(0, 10, 20, 40, 70, 28, 10);
        // 크기 1, 메타 scale 1: 왼쪽 위 = 위치 − origin + (ox, oy)
        var r = SpriteLayout.Atlas(pos, new Vector2(48, 80), rect, 1f, 1);
        Assert.Equal(new RectF(1000 - 48 + 28, 600 - 80 + 10, 1000 - 48 + 28 + 40, 600 - 80 + 10 + 70), r);
        // 메타 scale 2(그림 픽셀 2배): ox·크기는 ÷ 2, origin 은 월드 px 그대로 → 같은 자리
        var r2 = SpriteLayout.Atlas(pos, new Vector2(48, 80), rect with { W = 80, H = 140, Ox = 56, Oy = 20 }, 1f, 2);
        Assert.Equal(r, r2);
        // 정의 크기 1.5
        var r3 = SpriteLayout.Atlas(pos, new Vector2(48, 80), rect, 1.5f, 1);
        Assert.Equal(1000 - 72 + 42f, r3.X0);
        Assert.Equal(60f, r3.Width);

        Assert.Equal(new RectF(952, 450, 1048, 610), SpriteLayout.Grid(pos, new Vector2(48, 150), new Int2(96, 160), 1f));
        Assert.Equal(new RectI(192, 320, 288, 480), SpriteLayout.GridSource(new Int2(96, 160), 2, 3));
        Assert.Equal(new RectI(384, 640, 576, 960), SpriteLayout.GridSource(new Int2(96, 160), 2, 3, 2));

        var sprite = TestDefs.Sprite() with { OriginByDir = [new Vector2(1, 2), new Vector2(3, 4)] };
        Assert.Equal(new Vector2(3, 4), SpriteLayout.Origin(sprite, 1));
        Assert.Equal(sprite.Origin, SpriteLayout.Origin(sprite, 5));
    }
}
