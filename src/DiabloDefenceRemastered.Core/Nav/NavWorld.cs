using DiabloDefenceRemastered.Core.Geometry;

namespace DiabloDefenceRemastered.Core.Nav;

/// <summary>
/// 내비 질의 구현 (설계서 4.2~4.5·4.10, 소유 WP2) — <c>NavWorld.Build(World)</c>: 충돌 도형(<see cref="ArenaCollision"/>)·<see cref="ObstacleIndex"/>·16px clearance 장·
/// 플로우 필드 둘(S 16·M 26)·A*. 외곽 상자 격자, 경계 = 바깥 마름모(맵 없는 시험 월드는 상자 직사각형).
/// 월드(방)마다 새로 만든다(3.6 결정 1 — 되돌리기 경로 없음). <see cref="INavDebugView"/> 도 같이 구현한다(<c>--debug-nav</c>).
/// </summary>
public sealed class NavWorld : INavWorld, INavDebugView
{
    private readonly IsoMath? _iso;
    private readonly Vector2 _box;
    private readonly List<int> _cells = new(256);
    private readonly List<Vector2> _points = new(256);
    private readonly List<Vector2> _raw = new(256);
    private readonly Queue<EntityId> _requests = new(64);
    private World? _world;

    /// <param name="arena">맵 격자. null 이면 장애물 없는 상자(<paramref name="boxSize"/>) — 맵 없는 시험 월드.</param>
    public NavWorld(ArenaGrid? arena, Vector2 boxSize)
    {
        Arena = arena!;
        _iso = arena?.Iso;
        _box = arena?.Iso.BoxSize ?? boxSize;
        Collision = arena is null ? [] : ArenaCollision.Build(arena);
        Obstacles = new ObstacleIndex(Collision, _box);
        var w = Math.Max(1, (int)MathF.Ceiling(_box.X / NavConst.CellSize));
        var h = Math.Max(1, (int)MathF.Ceiling(_box.Y / NavConst.CellSize));
        var clear = new float[w * h];
        for (var cy = 0; cy < h; cy++)
            for (var cx = 0; cx < w; cx++)
                clear[cy * w + cx] = ExactClearance(new Vector2((cx + 0.5f) * NavConst.CellSize, (cy + 0.5f) * NavConst.CellSize));
        Grid = new NavGrid(w, h, clear);
        Fields = [new FlowField(Grid, NavConst.FieldS), new FlowField(Grid, NavConst.FieldM)];
        AStar = new GridAStar(Grid);
    }

    /// <summary>월드의 맵으로 만들고 주인공 위치에 첫 필드를 계산한다(설치 — 방 이동 예산 안, 10.3).</summary>
    public static NavWorld Build(World world)
    {
        var nav = new NavWorld(world.Arena, world.MapSize) { _world = world };
        nav.UpdateFields(force: true, tick: 0);
        return nav;
    }

    // ── 내부 ───────────────────────────────────────────────────────

    public NavGrid Grid { get; }
    public ObstacleIndex Obstacles { get; }
    /// <summary>필드 둘: 0 = S(16), 1 = M(26).</summary>
    public FlowField[] Fields { get; }
    public GridAStar AStar { get; }
    /// <summary>A* 요청 처리 수(시험 계측).</summary>
    public int PathsSolved { get; private set; }

    /// <summary>몸 반경 → 필드 등급(≤ 16 → S, 그 위 → M). 26 을 넘는 flow 유닛도 M(데이터 검증 경고 몫).</summary>
    public static int FieldClass(float radius) => radius <= NavConst.FieldS ? 0 : 1;

    /// <summary>경계(바깥 마름모·상자)까지 부호 있는 거리.</summary>
    public float BoundaryDistance(Vector2 p) => _iso is { } iso
        ? iso.SignedDistanceToBoundary(p)
        : MathF.Min(MathF.Min(p.X, _box.X - p.X), MathF.Min(p.Y, _box.Y - p.Y));

    /// <summary>경계 안쪽 방향(가장 가까운 변의 안쪽 법선). 마름모 변 기울기 ±1/2 → 법선 (±1, ±2)/√5.</summary>
    public Vector2 BoundaryInward(Vector2 p)
    {
        if (_iso is { } iso)
        {
            var c = new Vector2(32f * iso.N, iso.H + 16f * iso.N);
            var sx = p.X >= c.X ? 1f : -1f;
            var sy = p.Y >= c.Y ? 1f : -1f;
            return new Vector2(-sx, -2f * sy) / MathF.Sqrt(5f);
        }
        var dl = p.X; var dr = _box.X - p.X; var dt = p.Y; var db = _box.Y - p.Y;
        var m = MathF.Min(MathF.Min(dl, dr), MathF.Min(dt, db));
        return m == dl ? Vector2.UnitX : m == dr ? -Vector2.UnitX : m == dt ? Vector2.UnitY : -Vector2.UnitY;
    }

    /// <summary>정확한 clearance(도형·경계 실제 거리, 이중선형 아님). 검사·장 만들기용.</summary>
    public float ExactClearance(Vector2 p)
    {
        var b = BoundaryDistance(p);
        if (b <= 0f) return b;
        return MathF.Min(b, Obstacles.NearestWalkDistance(p, boundaryOnly: false, out _));
    }

    /// <summary>
    /// 필드 갱신(4.3): 주인공이 다른 칸으로 옮겼을 때만, S 는 <c>Tick % 6 == 0</c>, M 은 <c>== 3</c>. <paramref name="force"/> 면 둘 다 지금.
    /// </summary>
    public void UpdateFields(bool force, int tick)
    {
        if (_world is not { } w) return;
        var hero = w.Hero;
        var cell = Grid.ClampedIndex(hero.Pos);
        for (var k = 0; k < Fields.Length; k++)
        {
            var f = Fields[k];
            var due = force || (tick % NavConst.FieldPeriod == (k == 0 ? 0 : 3) && f.GoalCell != cell);
            if (due) f.Compute(hero.Pos, hero.Radius);
        }
    }

    // ── INavWorld ───────────────────────────────────────────────────

    public float Clearance(Vector2 p)
    {
        if (p.X < 0 || p.Y < 0 || p.X >= _box.X || p.Y >= _box.Y) return BoundaryDistance(p);
        return Grid.Sample(p);
    }

    public bool HasClearLine(Vector2 a, Vector2 b, float radius)
    {
        var d = b - a;
        var len = d.Length();
        var steps = Math.Max(1, (int)MathF.Ceiling(len / NavConst.LineStep));
        for (var i = 0; i <= steps; i++)
            if (Clearance(a + d * (i / (float)steps)) < radius) return false;
        return true;
    }

    public bool ProjectileLineClear(Vector2 a, Vector2 b) => Obstacles.ProjectileLineClear(a, b);

    public bool IsInsideMap(Vector2 p) => _iso is { } iso
        ? iso.IsInsideMap(p)
        : p.X >= 0 && p.Y >= 0 && p.X < _box.X && p.Y < _box.Y;

    public float PathLength(Vector2 from, Vector2 to, float radius, float maxLength)
    {
        var straight = Vector2.Distance(from, to);
        if (straight > maxLength) return float.PositiveInfinity;
        if (HasClearLine(from, to, radius)) return straight;
        if (!SearchCells(from, to, radius, maxLength / Grid.CellSize, out var cost, out var reached) || !reached) return float.PositiveInfinity;
        var px = cost * Grid.CellSize;
        return px <= maxLength ? px : float.PositiveInfinity;
    }

    public float PathLengthToHero(Vector2 from, float radius, float maxLength)
    {
        if (_world is not { } w) return float.PositiveInfinity;
        if (radius <= NavConst.FieldM)
        {
            var field = Fields[FieldClass(radius)];
            var cell = Grid.ClampedIndex(from);
            if (field.Passable(cell) && field.GoalCell == Grid.ClampedIndex(w.Hero.Pos))
            {
                var d = field.Dist[cell];
                if (d == NavConst.Unreachable) return float.PositiveInfinity;
                // 격자 비용(10/14) → px. 시작점(거리 0)은 직선 거리
                var px = d == 0 ? Vector2.Distance(from, w.Hero.Pos) : d * (Grid.CellSize / (float)NavConst.StraightCost);
                return px <= maxLength ? px : float.PositiveInfinity;
            }
        }
        return PathLength(from, w.Hero.Pos, radius, maxLength);
    }

    public bool TryPathStep(Vector2 from, Vector2 to, float radius, out Vector2 next)
    {
        if (HasClearLine(from, to, radius))
        {
            next = to;
            return true;
        }
        if (!FindPath(from, to, radius, _points, requireGoal: true) || _points.Count < 2)
        {
            next = from;
            return false;
        }
        next = _points[1];
        return true;
    }

    // ── A* ─────────────────────────────────────────────────────────

    /// <summary>
    /// 칸 A*(4.4): 시작이 막혔으면 3칸 안 가장 가까운 통과 칸, 목표 칸이 막혔으면 3칸 안 통과 칸(없으면 목표 칸 그대로 — h 가 가장 작은 곳까지).
    /// 경로 칸은 <see cref="LastCells"/>. 시작에서 3칸 안에 통과 칸이 없으면 false.
    /// </summary>
    internal bool SearchCells(Vector2 from, Vector2 to, float radius, float maxCost, out float cost, out bool reached)
    {
        cost = float.PositiveInfinity;
        reached = false;
        _cells.Clear();
        var start = AStar.NearestPassable(Grid.ClampedIndex(from), radius, NavConst.StartSearchCells);
        if (start < 0) return false;
        var goalRaw = Grid.ClampedIndex(to);
        var goal = AStar.NearestPassable(goalRaw, radius, NavConst.StartSearchCells);
        if (goal < 0) goal = goalRaw;
        reached = AStar.Search(start, goal, radius, maxCost, NavConst.PathExpandLimit, out cost, out var closest);
        AStar.PathTo(reached ? goal : closest, _cells);
        return true;
    }

    /// <summary>마지막 <see cref="SearchCells"/> 의 칸 경로.</summary>
    internal IReadOnlyList<int> LastCells => _cells;

    /// <summary>
    /// from → to 다듬은 경로(끈 당기기, 4.4): 첫 점 = from, 끝 = to(닿으면) 또는 갈 수 있는 가장 가까운 칸 중심. <paramref name="requireGoal"/> 면 못 닿을 때 false.
    /// </summary>
    public bool FindPath(Vector2 from, Vector2 to, float radius, List<Vector2> smoothed, bool requireGoal = false)
    {
        smoothed.Clear();
        if (!SearchCells(from, to, radius, float.PositiveInfinity, out _, out var reached)) return false;
        if (requireGoal && !reached) return false;
        PathsSolved++;
        // 원 경로 점: from, 칸 중심들(첫 칸 제외), 끝
        var raw = _raw;
        raw.Clear();
        raw.Add(from);
        for (var i = 1; i < _cells.Count; i++) raw.Add(Grid.Center(_cells[i]));
        if (reached) raw.Add(to);
        else if (_cells.Count == 1) raw.Add(Grid.Center(_cells[0]));
        Pull(raw, radius, smoothed);
        return true;
    }

    /// <summary>끈 당기기: 현재 점에서 길 트임이 통과하는 가장 먼 점으로 건너뛴다.</summary>
    private void Pull(List<Vector2> raw, float radius, List<Vector2> smoothed)
    {
        smoothed.Add(raw[0]);
        var i = 0;
        while (i < raw.Count - 1)
        {
            var j = i + 1;
            while (j + 1 < raw.Count && HasClearLine(raw[i], raw[j + 1], radius)) j++;
            smoothed.Add(raw[j]);
            i = j;
        }
    }

    // ── 요청 줄(4.4) ─────────────────────────────────────────────────

    /// <summary>A* 요청을 줄에 세운다(같은 적이 이미 줄에 있으면 부르는 쪽이 <c>Nav.PathRequested</c> 로 막는다).</summary>
    internal void RequestPath(EntityId enemy) => _requests.Enqueue(enemy);

    internal int PendingRequests => _requests.Count;

    internal bool TryDequeue(out EntityId enemy) => _requests.TryDequeue(out enemy);

    // ── INavDebugView ───────────────────────────────────────────────

    public IReadOnlyList<CollisionShape> Collision { get; }
    public ArenaGrid Arena { get; }
    public int CellSize => Grid.CellSize;
    public Int2 GridSize => new(Grid.Width, Grid.Height);
    public float CellClearance(int cx, int cy) => Grid.At(cx, cy);
    public bool IsBlocked(int sizeClass, int cx, int cy) =>
        !Grid.InGrid(cx, cy) || !Fields[Math.Clamp(sizeClass, 0, Fields.Length - 1)].Passable(Grid.Index(cx, cy));
    public Vector2 FlowDirection(int sizeClass, int cx, int cy)
    {
        if (!Grid.InGrid(cx, cy)) return Vector2.Zero;
        var f = Fields[Math.Clamp(sizeClass, 0, Fields.Length - 1)];
        return FlowField.DirVector(f.Dir[Grid.Index(cx, cy)]);
    }

    public bool TryGetPath(EntityId enemy, List<Vector2> raw, List<Vector2> smoothed)
    {
        raw.Clear();
        smoothed.Clear();
        if (_world is not { } w || !w.Enemies.TryGet(enemy, out var e) || e.Nav.PointCount == 0) return false;
        for (var i = 0; i < e.Nav.PointCount; i++) smoothed.Add(e.Nav.Points[i]);
        if (SearchCells(e.Pos, e.Nav.PathGoal, e.Radius, float.PositiveInfinity, out _, out _))
            foreach (var c in _cells) raw.Add(Grid.Center(c));
        return true;
    }

    /// <summary>원(중심 c, 반경 r)이 걷기 막힘 도형을 침범한 깊이 중 가장 큰 값(0 = 안 겹침). 불변식 <c>in_obstacle</c>(10.1).</summary>
    public float WalkPenetration(Vector2 c, bool boundaryOnly)
    {
        var worst = 0f;
        var q = Obstacles.BeginQuery();
        var cx = (int)MathF.Floor(c.X / Obstacles.CellSize);
        var cy = (int)MathF.Floor(c.Y / Obstacles.CellSize);
        foreach (var s in Obstacles.At(cx, cy))
        {
            if (!Obstacles.FirstVisit(s, q)) continue;
            var sh = Obstacles.Shapes[s];
            if (!sh.BlocksWalk || (boundaryOnly && !sh.IsBoundary)) continue;
            var d = Geo.SignedDistance(Obstacles.Polys[s], c);
            if (d < 0) worst = MathF.Max(worst, -d);
        }
        return worst;
    }
}
