using DiabloDefenceRemastered.Core.Nav;

namespace DiabloDefenceRemastered.Core.Sim.Movement;

/// <summary>
/// 이동 의도 → 희망 속도 (설계서 4.3 끝·4.4 끝·4.6-1, 소유 WP2). 플로우·A*·직진·끼임·미끄러짐이 모두 여기 있다 — 적 부품(WP4)은 의도만 쓴다.
/// 호출은 <c>Locomotion</c> 단계에서 적마다 한 번. 상태는 <see cref="Enemy.Nav"/>(<see cref="NavAgentState"/>)에만 둔다.
/// </summary>
public sealed class Navigator(World world, NavWorld nav)
{
    /// <summary>끼임 판정: 쫓는 중 1초 동안 4px 도 못 움직이면 0.5초 비켜 간다(4.3).</summary>
    public const float StuckWindow = 1f, StuckDistance = 4f, SidestepSeconds = 0.5f;
    /// <summary>A* 시작 3칸 밖까지 막혔을 때 기울기로 움직이는 시간(4.4).</summary>
    public const float EscapeSeconds = 0.25f;
    /// <summary>
    /// "공격 사거리 밖" 근사(px, 두 몸 반경에 더함). 부품의 사거리는 WP4 가 알므로 여기서는 닿기 직전 거리로 본다 — 사거리 안에서 멈춘 근접 적이 비켜 가지 않게. <c>[해석]</c>
    /// </summary>
    public const float ReachMargin = 40f;

    /// <summary>적 하나의 이번 틱 희망 속도(px/s). 분리·가속 제한은 <c>LocomotionSystem</c> 이 더한다.</summary>
    public Vector2 Desired(Enemy e)
    {
        ref var s = ref e.Nav;
        var intent = e.Intent;
        var speed = e.MoveSpeed * intent.SpeedMul;
        if (e.Life == LifeState.Stunned || speed <= 0f || intent.Kind == MoveKind.Hold)
        {
            s.StuckTime = 0f;
            return Vector2.Zero;
        }
        // 입장 중·등장 중은 격자를 쓰지 않는다(직선, 4.2 끝)
        if (e.Life is LifeState.Entering or LifeState.Emerging)
            return intent.Kind switch
            {
                MoveKind.Straight or MoveKind.Heading => MathX.NormalizeOr(intent.Dir, Vector2.Zero) * speed,
                MoveKind.SeekPoint => MathX.NormalizeOr(intent.Point - e.Pos, Vector2.Zero) * speed,
                MoveKind.SeekHero => MathX.NormalizeOr(world.Hero.Pos - e.Pos, Vector2.Zero) * speed,
                MoveKind.Away => MathX.NormalizeOr(e.Pos - intent.Point, Vector2.Zero) * speed,
                _ => Vector2.Zero,
            };

        Vector2 dir;
        switch (intent.Kind)
        {
            case MoveKind.Straight:
                return MathX.NormalizeOr(intent.Dir, Vector2.Zero) * speed;
            case MoveKind.Heading:
                dir = Slide(e, MathX.NormalizeOr(intent.Dir, Vector2.Zero));
                break;
            case MoveKind.Away:
                dir = Slide(e, MathX.NormalizeOr(e.Pos - intent.Point, Vector2.Zero));
                break;
            case MoveKind.SeekPoint:
                dir = e.Def.Movement == MovementKind.Astar && !e.PassThroughObstacles
                    ? FollowAStar(e, intent.Point)
                    : Slide(e, MathX.NormalizeOr(intent.Point - e.Pos, Vector2.Zero));
                break;
            case MoveKind.SeekHero:
                dir = SeekHero(e);
                break;
            default:
                return Vector2.Zero;
        }
        return dir * speed;
    }

    private Vector2 SeekHero(Enemy e)
    {
        var hero = world.Hero.Pos;
        var toHero = hero - e.Pos;
        Vector2 dir;
        if (e.PassThroughObstacles)
            dir = MathX.NormalizeOr(toHero, Vector2.Zero);   // 안쪽 장애물은 건너뛴다 — 둘레·경계는 충돌 단계가 막는다(4.7-3)
        else if (e.Def.Movement == MovementKind.Astar)
            dir = FollowAStar(e, hero);
        else if (e.Def.Movement == MovementKind.Direct)
            dir = Slide(e, MathX.NormalizeOr(toHero, Vector2.Zero));
        else
            dir = FollowField(e, toHero);

        dir = ApplyStuck(e, dir, toHero);
        return dir;
    }

    private Vector2 FollowField(Enemy e, Vector2 toHero)
    {
        ref var s = ref e.Nav;
        // 가까우면 직진: 200px 안이고 길이 트였으면(6틱에 한 번, Slot % 6 분산)
        if (toHero.LengthSquared() <= NavConst.DirectRange * NavConst.DirectRange)
        {
            if (!s.ClearLineValid || (world.Tick + e.Id.Slot) % NavConst.FieldPeriod == 0)
            {
                s.ClearLine = nav.HasClearLine(e.Pos, world.Hero.Pos, e.Radius);
                s.ClearLineValid = true;
            }
            if (s.ClearLine) return MathX.NormalizeOr(toHero, Vector2.Zero);
        }
        else s.ClearLineValid = false;

        var field = nav.Fields[NavWorld.FieldClass(e.Radius)];
        var dir = field.Direction(e.Pos, toHero);
        if (dir == Vector2.Zero)
        {
            // 못 가는 칸 → clearance 기울기로 빠져나온다(4.3)
            dir = MathX.NormalizeOr(nav.Grid.Gradient(e.Pos), MathX.NormalizeOr(toHero, Vector2.Zero));
        }
        return dir;
    }

    private Vector2 FollowAStar(Enemy e, Vector2 goal)
    {
        ref var s = ref e.Nav;
        var r = e.Radius;
        var toGoal = goal - e.Pos;
        if (toGoal.LengthSquared() < 1e-6f) return Vector2.Zero;

        // 목표까지 길이 트였으면 A* 없이 곧장(4.4). 6틱에 한 번 다시 본다
        if (!s.ClearLineValid || (world.Tick + e.Id.Slot) % NavConst.FieldPeriod == 0)
        {
            s.ClearLine = nav.HasClearLine(e.Pos, goal, r);
            s.ClearLineValid = true;
        }
        if (s.ClearLine)
        {
            s.PointCount = 0;
            return Vector2.Normalize(toGoal);
        }

        if (s.EscapeTime > 0f)
        {
            s.EscapeTime -= World.Dt;
            return MathX.NormalizeOr(nav.Grid.Gradient(e.Pos), Vector2.Normalize(toGoal));
        }

        s.PathAge += World.Dt;
        var replan = s.PointCount == 0
                     || s.PathAge >= NavConst.ReplanSeconds
                     || Vector2.DistanceSquared(goal, s.PathGoal) > NavConst.ReplanGoalMove * NavConst.ReplanGoalMove
                     || (s.PathTruncated && s.PointIndex >= s.PointCount);
        if (replan && !s.PathRequested)
        {
            s.PathRequested = true;
            s.PathGoal = goal;
            nav.RequestPath(e.Id);
        }

        // 경유점 따라가기(8px 안이면 다음). 결과가 오기 전에는 직전 경로나 직진 + 미끄러짐
        while (s.PointIndex < s.PointCount && Vector2.DistanceSquared(e.Pos, s.Points[s.PointIndex]) <= NavConst.WaypointReach * NavConst.WaypointReach)
            s.PointIndex++;
        if (s.PointIndex < s.PointCount) return MathX.NormalizeOr(s.Points[s.PointIndex] - e.Pos, Vector2.Zero);
        return Slide(e, Vector2.Normalize(toGoal));
    }

    /// <summary>
    /// <c>Navigation</c> 단계가 부른다(틱당 최대 2건, 4.4): 줄의 적 하나의 경로를 계산해 그 적의 경유점 배열에 바로 쓴다.
    /// 시작 3칸 안에 통과 칸이 없으면 경로 없이 기울기로 0.25초.
    /// </summary>
    public void Solve(Enemy e)
    {
        ref var s = ref e.Nav;
        s.PathRequested = false;
        s.PathAge = 0f;
        s.PointIndex = 0;
        s.PointCount = 0;
        s.PathTruncated = false;
        var points = _points;
        if (!nav.FindPath(e.Pos, s.PathGoal, e.Radius, points))
        {
            s.EscapeTime = EscapeSeconds;
            return;
        }
        // 첫 점은 지금 자리 — 빼고 담는다
        var n = Math.Min(points.Count - 1, NavPathPoints.Capacity);
        for (var i = 0; i < n; i++) s.Points[i] = points[i + 1];
        s.PointCount = (byte)n;
        s.PathTruncated = points.Count - 1 > NavPathPoints.Capacity;
    }

    private readonly List<Vector2> _points = new(64);

    /// <summary>
    /// 미끄러짐: 희망 방향 앞(몸 반경 + 4)의 clearance 가 몸 반경보다 작으면, 벽 쪽(기울기 반대) 성분을 빼고 벽을 따라 간다.
    /// </summary>
    public Vector2 Slide(Enemy e, Vector2 dir)
    {
        if (dir == Vector2.Zero) return dir;
        var r = e.Radius;
        var probe = e.Pos + dir * (r + 4f);
        if (nav.Clearance(probe) >= r) return dir;
        var n = MathX.NormalizeOr(nav.Grid.Gradient(probe), Vector2.Zero);
        if (n == Vector2.Zero) return dir;
        var into = Vector2.Dot(dir, n);
        if (into >= 0f) return dir;
        var slid = dir - into * n;
        return MathX.NormalizeOr(slid, new Vector2(-n.Y, n.X));
    }

    private Vector2 ApplyStuck(Enemy e, Vector2 dir, Vector2 toHero)
    {
        ref var s = ref e.Nav;
        if (s.SidestepTime > 0f)
        {
            s.SidestepTime -= World.Dt;
            var side = new Vector2(-dir.Y, dir.X) * s.SidestepSign;
            return MathX.NormalizeOr(side, dir);
        }
        var reach = e.Radius + world.Hero.Radius + ReachMargin;
        if (toHero.LengthSquared() <= reach * reach)
        {
            s.StuckTime = 0f;
            s.StuckAnchor = e.Pos;
            return dir;
        }
        if (s.StuckTime == 0f) s.StuckAnchor = e.Pos;
        s.StuckTime += World.Dt;
        if (s.StuckTime >= StuckWindow)
        {
            var moved = Vector2.DistanceSquared(e.Pos, s.StuckAnchor) >= StuckDistance * StuckDistance;
            s.StuckTime = 0f;
            s.StuckAnchor = e.Pos;
            if (!moved)
            {
                s.SidestepTime = SidestepSeconds;
                s.SidestepSign = world.Rng.Ai.Chance(0.5f) ? (sbyte)1 : (sbyte)-1;
                s.PathAge = NavConst.ReplanSeconds;   // A* 는 끼임이면 재계획
            }
        }
        return dir;
    }
}
