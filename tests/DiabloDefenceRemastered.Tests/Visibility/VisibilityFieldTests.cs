using System.Diagnostics;

namespace DiabloDefenceRemastered.Tests.Visibility;

/// <summary>
/// 시야 (설계서 4.11, 10.1 <c>Visibility</c> 행, WP1c). 합성 맵(<see cref="TestDefs"/>)으로 돈다.
/// 기본 무대: M 20(N 40, 스테이지 1 크기) 빈 방, 주인공 칸 (20,20) 중심 = 월드 (1280, 784), 화면 사각형 800..1760 × 514..1054(맵 가운데라 카메라가 안 잘림).
/// </summary>
public class VisibilityFieldTests
{
    private const int M = 20;
    private const float R = TestDefs.SightRadius;   // 420

    private static TestWorld Room(float radius = R, int megaTiles = M, int heroTx = 20, int heroTy = 20, PiecePlacementDef[]? place = null)
    {
        var t = new TestWorldBuilder().WithMegaTiles(megaTiles).WithSight(radius).WithPlace(place ?? []).Build();
        t.PlaceHero(t.CellCenter(heroTx, heroTy));
        return t;
    }

    private static PiecePlacementDef Line(string piece, int mx0, int my0, int mx1, int my1) =>
        new() { Piece = piece, Line = [new(mx0, my0), new(mx1, my1)] };

    private static PiecePlacementDef At(string piece, int mx, int my) => new() { Piece = piece, At = [new(mx, my)] };

    /// <summary>칸 (dx, dy) 만큼 떨어진 칸 중심 사이 화면 거리.</summary>
    private static float CellDist(int dx, int dy) => new Vector2(32 * (dx - dy), 16 * (dx + dy)).Length();

    // ── 범위·막힘 ───────────────────────────────────────────────────

    [Fact]
    public void EmptyRoom_AllCellsInRangeVisible_BeyondRangeNot()
    {
        var t = Room();
        t.Step();
        var f = t.World.Visibility;
        Assert.True(f.Enabled);
        Assert.Equal(20, f.Margin);
        // 범위 R + 64 = 484 안에는 시야 막힘 칸이 없다(가장 가까운 둘레 벽까지 515px 이상)
        var inRange = 0;
        for (var ty = -20; ty < 60; ty++)
            for (var tx = -20; tx < 60; tx++)
            {
                var d = CellDist(tx - 20, ty - 20);
                Assert.Equal(d <= R + 64f, f.InSight(tx, ty));
                if (d <= R + 64f) inRange++;
            }
        Assert.InRange(inRange, 600, 800);   // 4.11 "R 420 이면 700개 안팎"
        Assert.False(f.InSight(-21, 20));     // 확장 격자 밖
        var p = t.Hero.Pos;
        Assert.True(f.IsVisible(p + new Vector2(R - 1f, 0)));
        Assert.False(f.IsVisible(p + new Vector2(R + 1f, 0)));   // 칸은 보여도 반경 밖
        Assert.True(f.IsVisible(p + new Vector2(0, -(R - 1f))));
    }

    [Fact]
    public void SightWall_HidesCellsBehind_WallFaceVisible()
    {
        // 안쪽 벽 줄 "XX.." 을 my 8 에 가로로 → 하위 칸 ty 16, tx 12..29 가 X
        var t = Room(place: [Line("inner_wall", 6, 8, 14, 8)]);
        t.Step();
        var f = t.World.Visibility;
        Assert.True(f.InSight(20, 17));
        Assert.True(f.InSight(20, 16));    // 벽 면(막힘 칸도 선이 닿으면 보인다)
        Assert.False(f.InSight(20, 15));
        Assert.False(f.InSight(20, 14));
        Assert.False(f.IsVisible(t.CellCenter(20, 14)));
        Assert.True(f.IsVisible(t.CellCenter(20, 17)));
    }

    [Fact]
    public void Pillar_LowercaseX_DoesNotBlockSight_UppercaseColumnDoes()
    {
        var open = Room(place: [At("pillar", 10, 8)]);    // x... → (20,16) 걷기·투사체만
        open.Step();
        Assert.True(open.World.Visibility.InSight(20, 14));
        Assert.True(open.World.Arena!.BlocksWalk(20, 16));

        var shut = Room(place: [At("column", 10, 8)]);    // X... → (20,16) 시야도
        shut.Step();
        Assert.False(shut.World.Visibility.InSight(20, 14));
        Assert.True(shut.World.Visibility.InSight(20, 16));
    }

    [Fact]
    public void VertexRule_DiagonalWallBlocks_SingleCornerGrazeDoesNot()
    {
        // 정대각선 (18,18) → (23,23) 은 꼭짓점 (20.5, 20.5) 를 지난다. 양옆 칸 = (21,20)·(20,21) = 메가타일 (10,10) 의 위-오른·아래-왼
        var ts = TestDefs.Tileset();
        ts = ts with
        {
            Pieces = new Dictionary<string, int>(ts.Pieces) { ["anti_diag"] = 40, ["one_corner"] = 41 },
            Props = new Dictionary<int, string>(ts.Props) { [40] = ".XX.", [41] = ".X.." },
        };
        TestWorld Make(string piece)
        {
            var defs = TestDefs.Basic(M, sight: TestDefs.Sight, heroLight: R, place: [At(piece, 10, 10)], tileset: ts);
            var t = new TestWorldBuilder().WithDefs(defs).Build();
            t.PlaceHero(t.CellCenter(18, 18));
            t.Step();
            return t;
        }

        var diag = Make("anti_diag").World.Visibility;
        Assert.False(diag.InSight(23, 23));   // 꼭짓점으로 이은 벽은 새지 않는다
        Assert.False(diag.InSight(22, 22));
        Assert.True(diag.InSight(20, 20));
        Assert.True(diag.InSight(21, 20));    // 막힘 칸 자신(벽 면)

        var graze = Make("one_corner").World.Visibility;
        Assert.True(graze.InSight(23, 23));   // 기둥 하나의 모서리를 스치는 선은 막히지 않는다
        Assert.True(graze.InSight(22, 22));
    }

    [Fact]
    public void OutsideMap_VisibleOnlyThroughGateGap()
    {
        // M 12(N 24): 북서 문 틈 = tx 0..1, ty 10..13, 끝 조각 ty 8..9·14..15(XXXX), 나머지 왼쪽 열 tx 0 은 X
        var t = Room(megaTiles: 12, heroTx: 3, heroTy: 11);
        t.Step();
        var f = t.World.Visibility;
        Assert.True(f.InSight(-3, 11));    // 문 틈 밖 맵 밖 칸
        Assert.True(f.InSight(-1, 12));
        Assert.False(f.InSight(-3, 4));    // 둘레 벽 뒤 맵 밖
        Assert.False(f.InSight(-3, 18));   // 문 끝 조각 뒤
        Assert.False(f.IsExplored(-3, 11));   // 가 본 칸은 N×N 만
        Assert.True(f.IsExplored(1, 11));
        Assert.True(t.World.Arena!.Iso.IsInsideGrid(1, 11));
        Assert.True(f.ExploredCount <= 24 * 24);
    }

    // ── 선 걷기 = 느린 기준 구현 ─────────────────────────────────────

    [Fact]
    public void LineWalk_MatchesReference_OnRandomGrids()
    {
        var rng = new RandomStream(1234);
        const int W = 15, H = 13;
        var blocks = new bool[W * H];
        var mismatches = new List<string>();
        for (var g = 0; g < 200; g++)
        {
            var density = 0.1f + 0.4f * rng.NextFloat();
            for (var i = 0; i < blocks.Length; i++) blocks[i] = rng.NextFloat() < density;
            var hx = rng.Index(W);
            var hy = rng.Index(H);
            for (var cy = 0; cy < H; cy++)
                for (var cx = 0; cx < W; cx++)
                {
                    var fast = VisibilityField.LineClear(blocks, W, hx, hy, cx, cy);
                    var slow = ReferenceLine.Clear(blocks, W, hx, hy, cx, cy);
                    if (fast != slow && mismatches.Count < 10) mismatches.Add($"격자 {g}: ({hx},{hy}) → ({cx},{cy}) 빠름 {fast} 기준 {slow}");
                }
        }
        Assert.Empty(mismatches);
    }

    [Fact]
    public void Field_MatchesReference_WithScatteredColumns()
    {
        // 기둥 X... 는 메가타일 안에 막힘 칸이 하나뿐이라 두 줄 벽 규칙이 끼지 않는다 → InSight = 기준 구현(범위 안)
        var rng = new RandomStream(77);
        for (var round = 0; round < 5; round++)
        {
            var at = new List<Int2>();
            for (var my = 2; my < 18; my++)
                for (var mx = 2; mx < 18; mx++)
                    if ((mx, my) != (10, 10) && rng.Chance(0.25f)) at.Add(new(mx, my));
            var t = Room(place: [new PiecePlacementDef { Piece = "column", At = at }]);
            t.Step();
            var f = t.World.Visibility;
            var arena = t.World.Arena!;
            const int E = 80, P = 20;
            var blocks = new bool[E * E];
            for (var y = 0; y < E; y++)
                for (var x = 0; x < E; x++)
                    blocks[y * E + x] = arena.BlocksSight(x - P, y - P);
            for (var ty = -P; ty < 40 + P; ty++)
                for (var tx = -P; tx < 40 + P; tx++)
                {
                    var expected = CellDist(tx - 20, ty - 20) <= R + 64f && ReferenceLine.Clear(blocks, E, 20 + P, 20 + P, tx + P, ty + P);
                    Assert.True(expected == f.InSight(tx, ty), $"회차 {round} 칸 ({tx},{ty}): 기대 {expected}");
                }
        }
    }

    [Fact]
    public void ReferenceLine_SanityCases()
    {
        // 3×3, 가운데 막힘: 왼 → 오른 가로선은 막힘, 대각선 (0,0) → (2,2) 는 가운데를 지나 막힘
        var b = new bool[9];
        b[4] = true;
        Assert.False(ReferenceLine.Clear(b, 3, 0, 1, 2, 1));
        Assert.False(ReferenceLine.Clear(b, 3, 0, 0, 2, 2));
        Assert.True(ReferenceLine.Clear(b, 3, 0, 0, 2, 0));
        // 꼭짓점: (0,0) → (1,1) 양옆 (1,0)·(0,1)
        var c = new bool[4];
        c[1] = true;
        Assert.True(ReferenceLine.Clear(c, 2, 0, 0, 1, 1));
        c[2] = true;
        Assert.False(ReferenceLine.Clear(c, 2, 0, 0, 1, 1));
        Assert.False(VisibilityField.LineClear(c, 2, 0, 0, 1, 1));
    }

    // ── 다시 계산 시점·범위 ──────────────────────────────────────────

    [Fact]
    public void Recompute_OnlyOnCellOrRadiusChange()
    {
        var t = Room();
        var f = t.World.Visibility;
        Assert.Equal(0, f.RecomputeCount);   // 생성자는 계산하지 않는다
        t.Step();
        Assert.Equal(1, f.RecomputeCount);
        t.PlaceHero(t.Hero.Pos + new Vector2(6, 3));   // 같은 칸 안
        t.Step(3);
        Assert.Equal(1, f.RecomputeCount);
        t.PlaceHero(t.CellCenter(21, 20));
        t.Step();
        Assert.Equal(2, f.RecomputeCount);
        t.SetHeroLightRadius(300);
        t.Step();
        Assert.Equal(3, f.RecomputeCount);
        Assert.Equal(300f, f.Radius);
        t.Step(2);
        Assert.Equal(3, f.RecomputeCount);
    }

    [Fact]
    public void CellLeavingRange_TurnsOff_AtThatTick()
    {
        var t = Room();
        t.Step();
        var f = t.World.Visibility;
        Assert.True(CellDist(15, 15) <= R + 64f && CellDist(16, 16) > R + 64f);
        Assert.True(f.InSight(35, 35));
        t.Step(19);                                  // 틱 0..19
        t.PlaceHero(t.CellCenter(19, 19));
        t.Step();                                    // 틱 20: (35,35) 는 범위 밖
        Assert.False(f.InSight(35, 35));
        Assert.Equal(20, f.ChangedTick(35, 35));
    }

    [Fact]
    public void RadiusChange_TakesEffectNextTick()
    {
        var log = new List<(int Tick, float Radius, bool Visible)>();
        var t = new TestWorldBuilder().WithMegaTiles(M).WithSight()
            .WithSystem(SimPhase.Director, new ActionSystem(w => { if (w.Tick == 5) w.Lighting.HeroLightRadius = 200; }))
            .WithSystem(SimPhase.EnemyThink, new ActionSystem(w =>
                log.Add((w.Tick, w.Visibility.Radius, w.Visibility.IsVisible(w.Hero.Pos + new Vector2(300, 0))))))
            .Build();
        t.Step(8);
        Assert.Equal((5, 420f, true), log[5]);    // light_change 가 바꾼 틱의 질의는 옛 반경
        Assert.Equal((6, 200f, false), log[6]);   // 다음 틱부터 새 반경
        Assert.Equal(200f, t.World.Visibility.Radius);
    }

    [Fact]
    public void FirstComputation_IsFirstBeginTick_AfterSetupMovesHero()
    {
        Vector2 moved = default;
        var t = new TestWorldBuilder().WithMegaTiles(M).WithSight()
            .Setup(w => w.Hero.Pos = w.Hero.PrevPos = moved = w.Arena!.Iso.CellCenter(10, 10))
            .Build();
        var f = t.World.Visibility;
        Assert.True(f.Enabled);
        Assert.Equal(0, f.ExploredCount);
        Assert.False(f.InSight(10, 10));      // 첫 틱 전에는 모두 안 보임(그리면 검정)
        Assert.False(f.InSight(20, 20));
        t.Step();
        Assert.Equal(moved, f.HeroPos);
        Assert.True(f.IsExplored(10, 10));
        Assert.False(f.IsExplored(28, 28));   // 원래 시작 자리(19.5, 19.5) 둘레는 가 본 칸이 아니다
        Assert.Equal(-9, f.ChangedTick(10, 10));   // 첫 계산은 −F → 첫 화면이 서서히 밝아지지 않음
        Assert.Equal(1f, FogMath.CellLevel(f, 10, 10, 0));
        Assert.Equal(9, f.FadeTicks);
    }

    // ── 가 본 칸 ────────────────────────────────────────────────────

    [Fact]
    public void Explored_RefreshesEverySixTicks_WithinSameCell()
    {
        // R 410: (33,33) 칸 중심은 416px — 보이지만(범위 안) 반경 밖이라 가 본 칸이 아니다
        var t = Room(radius: 410);
        t.Step();
        var f = t.World.Visibility;
        Assert.True(f.InSight(33, 33));
        Assert.False(f.IsExplored(33, 33));
        Assert.Equal(1, f.ExploredVersion);
        t.PlaceHero(t.Hero.Pos + new Vector2(0, 8));   // 같은 칸 안 → 408px
        for (var tick = 1; tick <= 5; tick++)
        {
            t.Step();
            Assert.False(f.IsExplored(33, 33));
        }
        t.Step();                                      // 틱 6
        Assert.True(f.IsExplored(33, 33));
        Assert.Equal(2, f.ExploredVersion);
        Assert.Equal(1, f.RecomputeCount);
    }

    [Fact]
    public void Explored_IsMonotonic()
    {
        var t = Room();
        t.Step();
        var f = t.World.Visibility;
        var seen = new HashSet<(int, int)>();
        var last = 0;
        foreach (var (tx, ty) in new[] { (20, 20), (14, 20), (8, 14), (20, 30), (30, 10), (20, 20) })
        {
            t.PlaceHero(t.CellCenter(tx, ty));
            t.Step(7);
            Assert.True(f.ExploredCount >= last);
            foreach (var c in seen) Assert.True(f.IsExplored(c.Item1, c.Item2));
            for (var y = 0; y < 40; y++)
                for (var x = 0; x < 40; x++)
                    if (f.IsExplored(x, y)) seen.Add((x, y));
            Assert.Equal(seen.Count, f.ExploredCount);
            last = f.ExploredCount;
        }
    }

    [Fact]
    public void ExploredHash_SameSetSameValue_SameCountDifferentSetDiffers()
    {
        ulong HashAt(int tx, int ty, out int count)
        {
            var t = Room(heroTx: tx, heroTy: ty);
            t.Step();
            count = t.World.Visibility.ExploredCount;
            return t.World.Visibility.ExploredHash;
        }
        var a1 = HashAt(20, 20, out var ca);
        var a2 = HashAt(20, 20, out _);
        var b = HashAt(21, 21, out var cb);
        Assert.Equal(a1, a2);
        Assert.Equal(ca, cb);      // 빈 방에서 한 칸 옮기면 같은 모양
        Assert.NotEqual(a1, b);
    }

    // ── 두 줄 벽 ────────────────────────────────────────────────────

    [Fact]
    public void DoubleWall_OuterRowVisibleWithInner_DoesNotSpread()
    {
        // 두 줄 벽 XXXX: 메가타일 (10,7) = 칸 (20..21, 14..15), 그 뒤 (10,6) = 칸 (20..21, 12..13)
        var t = Room(place: [At("thick_wall", 10, 7), At("thick_wall", 10, 6)]);
        t.Step();
        var f = t.World.Visibility;
        Assert.True(f.InSight(20, 15));    // 안쪽 줄(선이 닿음)
        Assert.True(f.InSight(20, 14));    // 바깥 줄 — 선으로는 안 보이지만 같은 메가타일
        Assert.True(f.InSight(21, 14));
        Assert.True(f.IsExplored(20, 14));
        Assert.False(f.InSight(20, 13));   // 옆 메가타일로 퍼지지 않음
        Assert.False(f.InSight(21, 12));
        Assert.False(f.IsExplored(20, 13));
    }

    [Fact]
    public void DoubleWall_AppliesToWalkSightCells_NotToSightOnlyOrMissileSightCells()
    {
        // 주인공 (16,12) 에서 가로로: (20,12) 은 선이 닿고, (21,12) 는 (20,12) 뒤
        bool Behind(string piece, out bool explored)
        {
            var t = Room(heroTx: 16, heroTy: 12, place: [At(piece, 10, 6)]);
            t.Step();
            Assert.True(t.World.Visibility.InSight(20, 12));
            explored = t.World.Visibility.IsExplored(21, 12);
            return t.World.Visibility.InSight(21, 12);
        }
        Assert.True(Behind("walk_sight_wall", out var e1));       // W: 함께 보임
        Assert.True(e1);
        Assert.True(Behind("inner_wall", out _));                 // X
        Assert.False(Behind("sight_only", out var e2));           // L: 걸을 수 있는 칸 — 함께 보임에 들지 않음
        Assert.False(e2);
        Assert.False(Behind("sight_missile_wall", out _));        // M
    }

    // ── 또렷이 보임·ClearReach ───────────────────────────────────────

    [Fact]
    public void ClearlyVisible_DistanceAndScreenEdges_BodyTop()
    {
        var t = Room();
        t.Step();
        var f = t.World.Visibility;
        var p = t.Hero.Pos;
        Assert.Equal(new RectF(800, 514, 1760, 1054), f.ViewRect);
        // 거리: R − 40 = 380
        Assert.True(f.IsClearlyVisible(p + new Vector2(380, 0), 0));
        Assert.False(f.IsClearlyVisible(p + new Vector2(381, 0), 0));
        Assert.True(f.IsVisible(p + new Vector2(381, 0)));
        // 아래: 화면 안쪽 16px → 254
        Assert.True(f.IsClearlyVisible(p + new Vector2(0, 254), TestDefs.BodyHeight));
        Assert.False(f.IsClearlyVisible(p + new Vector2(0, 255), TestDefs.BodyHeight));
        // 위: 몸 위 끝까지 — top 0 이면 254, top 72 면 182
        Assert.True(f.IsClearlyVisible(p + new Vector2(0, -254), 0));
        Assert.False(f.IsClearlyVisible(p + new Vector2(0, -255), 0));
        Assert.True(f.IsClearlyVisible(p + new Vector2(0, -182), 72));
        Assert.False(f.IsClearlyVisible(p + new Vector2(0, -183), 72));
        Assert.True(f.IsVisible(p + new Vector2(0, -183)));
    }

    [Fact]
    public void ClearlyVisible_FalseBehindWall()
    {
        var t = Room(place: [Line("inner_wall", 6, 8, 14, 8)]);
        t.Step();
        Assert.False(t.World.Visibility.IsClearlyVisible(t.CellCenter(20, 14), 0));
        Assert.True(t.World.Visibility.IsClearlyVisible(t.CellCenter(20, 17), 0));
    }

    [Fact]
    public void ClearReach_SidesUpDownDiagonal()
    {
        var t = Room();
        t.Step();
        var f = t.World.Visibility;
        Assert.Equal(380f, f.ClearReach(new Vector2(1, 0), 72), 3);    // 양옆 = R − 40
        Assert.Equal(380f, f.ClearReach(new Vector2(-1, 0), 72), 3);
        Assert.Equal(182f, f.ClearReach(new Vector2(0, -1), 72), 3);   // 정위 = 254 − top
        Assert.Equal(254f, f.ClearReach(new Vector2(0, -1), 0), 3);
        Assert.Equal(254f, f.ClearReach(new Vector2(0, 5), 72), 3);    // 정아래 = 254(길이 무관)
        Assert.Equal(254f * MathF.Sqrt(2), f.ClearReach(new Vector2(1, -1), 0), 2);   // 사각형이 먼저
        Assert.Equal(380f, f.ClearReach(new Vector2(2, 1), 0), 3);                     // 원이 먼저
        Assert.Equal(0f, f.ClearReach(new Vector2(0, -1), 400));       // 몸이 화면보다 크면 0
        // 그 거리의 자리는 또렷이 보인다(벽 없는 방)
        var dir = Vector2.Normalize(new Vector2(1, -1));
        Assert.True(f.IsClearlyVisible(p(t) + dir * (f.ClearReach(dir, 0) - 0.01f), 0));
        static Vector2 p(TestWorld w) => w.Hero.Pos;
    }

    // ── 꺼짐 ────────────────────────────────────────────────────────

    [Fact]
    public void Disabled_QueriesAlwaysTrue_ClearReachInfinite()
    {
        // 시야 규칙 없음(기본 시험 월드), 반경 0, 맵 없음 — 셋 다 꺼짐
        var noRules = new TestWorldBuilder().Build();
        var zeroRadius = new TestWorldBuilder().WithSight(radius: 0).Build();
        var noMap = new World(TestDefs.Basic(sight: TestDefs.Sight, heroLight: R), 1);
        foreach (var w in new[] { noRules.World, zeroRadius.World, noMap })
        {
            var f = w.Visibility;
            Assert.False(f.Enabled);
            f.Update(w);
            Assert.True(f.IsVisible(new Vector2(-9999, -9999)));
            Assert.True(f.IsClearlyVisible(new Vector2(-9999, -9999), 500));
            Assert.Equal(float.PositiveInfinity, f.ClearReach(new Vector2(1, 0), 0));
            Assert.Equal(0, f.ExploredCount);
        }
        Assert.Equal(0, noMap.Visibility.Size);
    }

    // ── 바뀜 값 잇기 ────────────────────────────────────────────────

    [Fact]
    public void ChangedTick_ContinuesWhenFlippedWithinFade()
    {
        // (35,35): 주인공 (20,20) 에서 480px(범위 안), (19,19) 에서 512px(범위 밖)
        var t = Room();
        var f = t.World.Visibility;
        t.Step(20);                                    // 틱 0..19
        Assert.Equal(-9, f.ChangedTick(35, 35));
        t.PlaceHero(t.CellCenter(19, 19));
        t.Step(3);                                     // 틱 20..22 — 20 에 꺼짐
        Assert.Equal(20, f.ChangedTick(35, 35));
        t.PlaceHero(t.CellCenter(20, 20));
        t.Step();                                      // 틱 23 — a = 3 < 9 → 23 − (9 − 3) = 17
        Assert.True(f.InSight(35, 35));
        Assert.Equal(17, f.ChangedTick(35, 35));
        t.Step(20);                                    // 다 켜진 뒤 다시 끄면 그 틱
        t.PlaceHero(t.CellCenter(19, 19));
        t.Step();
        Assert.Equal(t.World.Tick - 1, f.ChangedTick(35, 35));
    }

    // ── 할당·성능 ───────────────────────────────────────────────────

    [Fact]
    public void Update_AllocatesNothing()
    {
        var t = Room(place: [Line("inner_wall", 6, 8, 14, 8)]);
        var w = t.World;
        var f = w.Visibility;
        var a = t.CellCenter(20, 20);
        var b = t.CellCenter(21, 19);
        void Run(int n)
        {
            for (var i = 0; i < n; i++)
            {
                w.Hero.Pos = (i & 1) == 0 ? a : b;
                f.Update(w);
                _ = f.ExploredHash;
                _ = FogMath.PosAlpha(f, b, w.Tick);
                w.Tick++;
            }
        }
        Run(50);
        var before = GC.GetAllocatedBytesForCurrentThread();
        Run(1000);
        var after = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(0, after - before);
        Assert.True(f.RecomputeCount >= 1000);
    }

    [Fact, Trait("Category", TestCategories.Perf)]
    public void Perf_RecomputeStageOneSize_Under0_1ms()
    {
        TestCategories.RequireEnabled(TestCategories.Perf);
        // 스테이지 1 크기(M 20, N 40), R 420, 기둥 넷 + 주인공 4칸 위 안쪽 벽 줄(시야 막힘이 범위 안 상자 절반쯤에 걸림)
        var t = new TestWorldBuilder().WithMegaTiles(M).WithSight()
            .WithPlace(new PiecePlacementDef { Piece = "pillar", At = [new(7, 7), new(13, 7), new(7, 13), new(13, 13)] }, Line("inner_wall", 6, 8, 14, 8))
            .Build();
        var w = t.World;
        var f = w.Visibility;
        var a = t.CellCenter(20, 20);
        var b = t.CellCenter(20, 21);
        for (var i = 0; i < 200; i++) { w.Hero.Pos = (i & 1) == 0 ? a : b; f.Update(w); w.Tick++; }
        const int n = 2000;
        var start = f.RecomputeCount;
        var sw = Stopwatch.StartNew();
        for (var i = 0; i < n; i++) { w.Hero.Pos = (i & 1) == 0 ? a : b; f.Update(w); w.Tick++; }
        sw.Stop();
        Assert.Equal(n, f.RecomputeCount - start);
        var ms = sw.Elapsed.TotalMilliseconds / n;
        TestContext.Current.SendDiagnosticMessage($"VisibilityField 다시 계산 1회 {ms:F4} ms (N 40, R 420, {n}회 평균)");
        TestContext.Current.TestOutputHelper?.WriteLine($"PERF visibility_recompute_ms={ms:F4}");
        Assert.True(ms <= 0.1, $"다시 계산 1회 {ms:F4} ms > 0.1 ms");
    }

    private sealed class ActionSystem(Action<World> action) : ISimSystem
    {
        public void Tick(World world) => action(world);
    }
}

/// <summary>
/// 선 걷기의 느린 기준 구현: 칸 사각형(열린) ∩ 열린 선분을 유리수로 푼다(4.11 — 시험만 씀).
/// 막힘 = 선분 내부가 지나는 칸(h·c 제외) 중 막힘 칸이 있거나, 선분이 지나는 칸 꼭짓점의 양옆 두 칸(선분이 들어가지 않는 두 칸)이 둘 다 막힘.
/// </summary>
internal static class ReferenceLine
{
    public static bool Clear(bool[] blocks, int stride, int hx, int hy, int cx, int cy)
    {
        int dx = cx - hx, dy = cy - hy;
        if (dx == 0 && dy == 0) return true;
        int x0 = Math.Min(hx, cx), x1 = Math.Max(hx, cx), y0 = Math.Min(hy, cy), y1 = Math.Max(hy, cy);
        for (var y = y0; y <= y1; y++)
            for (var x = x0; x <= x1; x++)
            {
                if ((x == hx && y == hy) || (x == cx && y == cy)) continue;
                if (blocks[y * stride + x] && Hits(hx, hy, dx, dy, x, y)) return false;
            }
        // 꼭짓점 (a + ½, b + ½): 두 배 좌표 (2a+1, 2b+1)
        for (var vy = 2 * y0 - 1; vy <= 2 * y1 + 1; vy += 2)
            for (var vx = 2 * x0 - 1; vx <= 2 * x1 + 1; vx += 2)
            {
                long px = vx - 2 * hx, py = vy - 2 * hy;   // 2(v − h)
                if (px * dy != py * dx) continue;           // 직선 위가 아님
                // t = px / (2dx) 또는 py / (2dy) ∈ (0, 1)
                long tn = dx != 0 ? px : py, td = dx != 0 ? 2L * dx : 2L * dy;
                if (td < 0) { tn = -tn; td = -td; }
                if (tn <= 0 || tn >= td) continue;
                int a = (vx - 1) / 2, b = (vy - 1) / 2;
                if (vx - 1 < 0) a = (vx - 2) / 2;   // 음수 나눗셈 보정(시험 격자는 0 이상이라 실제로는 안 씀)
                if (vy - 1 < 0) b = (vy - 2) / 2;
                var sides = 0;
                var blocked = 0;
                for (var j = 0; j < 2; j++)
                    for (var i = 0; i < 2; i++)
                    {
                        int x = a + i, y = b + j;
                        if (Hits(hx, hy, dx, dy, x, y)) continue;
                        sides++;
                        if (x >= 0 && y >= 0 && x < stride && y * stride + x < blocks.Length && blocks[y * stride + x]) blocked++;
                    }
                if (sides != 2) throw new InvalidOperationException($"꼭짓점 ({vx}/2, {vy}/2) 양옆 칸 수 {sides}");
                if (blocked == 2) return false;
            }
        return true;
    }

    /// <summary>열린 선분 h + t·d (0 &lt; t &lt; 1) 이 칸 (x, y) 의 열린 사각형 (x ± ½, y ± ½) 을 지나나.</summary>
    private static bool Hits(int hx, int hy, int dx, int dy, int x, int y)
    {
        // 아래 끝 최댓값 < 위 끝 최솟값. 분수는 (분자, 양의 분모)
        (long N, long D) lo = (0, 1), hi = (1, 1);
        if (!Axis(hx, dx, x, ref lo, ref hi)) return false;
        if (!Axis(hy, dy, y, ref lo, ref hi)) return false;
        return lo.N * hi.D < hi.N * lo.D;
    }

    private static bool Axis(int h, int d, int cell, ref (long N, long D) lo, ref (long N, long D) hi)
    {
        if (d == 0) return h == cell;   // 정수 좌표라 |h − cell| < ½ ⇔ 같음
        long a = 2L * (cell - h) - 1, b = 2L * (cell - h) + 1, den = 2L * d;
        if (den < 0) { (a, b) = (-b, -a); den = -den; }
        if (a * lo.D > lo.N * den) lo = (a, den);
        if (b * hi.D < hi.N * den) hi = (b, den);
        return true;
    }
}
