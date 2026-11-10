using DiabloDefenceRemastered.Core.Geometry;
using DiabloDefenceRemastered.Core.Nav;
using DiabloDefenceRemastered.Core.Spatial;
using DiabloDefenceRemastered.Core.Sim.Movement.Invariants;

namespace DiabloDefenceRemastered.Core.Sim.Movement;

/// <summary>
/// 단계 4 <c>Navigation</c> (설계서 3.5·4.3·4.4, 소유 WP2): 플로우 필드 갱신(분산 — S 는 Tick % 6 == 0, M 은 == 3, 주인공 칸이 바뀌었을 때만),
/// A* 요청 처리(틱당 최대 2건, 결과는 그 적의 경유점 배열에 바로).
/// </summary>
public sealed class NavigationSystem(NavWorld nav, Navigator navigator) : ISimSystem
{
    public void Tick(World world)
    {
        nav.UpdateFields(force: false, world.Tick);
        var solved = 0;
        while (solved < NavConst.PathRequestsPerTick && nav.TryDequeue(out var id))
        {
            if (!world.Enemies.TryGet(id, out var e) || !e.IsAlive || !e.Nav.PathRequested) continue;
            navigator.Solve(e);
            solved++;
        }
    }
}

/// <summary>
/// 단계 6 <c>Locomotion</c> (설계서 3.5·4.6, 소유 WP2): 공간 해시 짓기 → 이동 의도를 희망 속도로(<see cref="Navigator"/>) → 분리 조향 → 가속 제한 → 적분(넉백 포함).
/// 주인공도 여기서 적분한다(의도 = WP3 <c>HeroControl</c>, 위치·속도 쓰기 = WP2, 11.2).
/// </summary>
public sealed class LocomotionSystem(SpatialHash hash, Navigator navigator) : ISimSystem
{
    /// <summary>분리: 이웃 반경 여유·가중·이웃 수 상한(4.6-2).</summary>
    public const float SeparationPad = 8f, SeparationWeight = 1.2f, HoldWeight = 2f;
    public const int MaxNeighbors = 8;
    /// <summary>가속 제한: 속도 변화량 ≤ 속도 × 8 / s(4.6-3).</summary>
    public const float AccelFactor = 8f;
    /// <summary>질의 칸(넘치면 반경을 절반으로 — 4.8).</summary>
    public const int QueryCapacity = 128;

    private readonly int[] _buf = new int[QueryCapacity];
    private readonly int[] _near = new int[MaxNeighbors];
    private readonly float[] _nearD = new float[MaxNeighbors];

    public void Tick(World world)
    {
        hash.Rebuild();
        var enemies = world.Enemies.Items;
        for (var i = 0; i < enemies.Length; i++)
        {
            var e = enemies[i];
            if (!e.IsAlive || e.SpawnTick >= world.Tick) continue;   // 이번 틱에 생긴 개체는 다음 틱부터(3.5)
            var desired = navigator.Desired(e);
            e.Nav.Desired = desired;
            var v = desired;
            if (e.IsTargetable && e.Intent.Kind != MoveKind.Straight && desired != Vector2.Zero)
                v = Separate(world, e, i, desired);
            // 가속 제한(입장·돌진 직선은 그대로)
            if (e.Life != LifeState.Entering && e.Intent.Kind != MoveKind.Straight)
            {
                var dv = v - e.Vel;
                var max = MathF.Max(v.Length(), e.Vel.Length()) * AccelFactor * World.Dt;
                var len = dv.Length();
                if (len > max && len > 1e-6f) v = e.Vel + dv * (max / len);
            }
            e.Vel = v;
            var move = v;
            if (e.KnockbackTime > 0f)
            {
                move = e.KnockbackVel;   // 0.12s 동안 희망 속도 대신(4.6-4)
                e.KnockbackTime = MathF.Max(0f, e.KnockbackTime - World.Dt);
                if (e.KnockbackTime == 0f) e.KnockbackVel = Vector2.Zero;
            }
            e.Pos += move * World.Dt;
        }

        MoveHero(world.Hero);
    }

    private static void MoveHero(Hero hero)
    {
        var v = Vector2.Zero;
        if (!hero.Dead)
        {
            var intent = hero.Intent;
            var speed = hero.MoveSpeed * intent.SpeedMul;
            v = intent.Kind switch
            {
                MoveKind.Heading or MoveKind.Straight => MathX.NormalizeOr(intent.Dir, Vector2.Zero) * speed,
                MoveKind.SeekPoint => MathX.NormalizeOr(intent.Point - hero.Pos, Vector2.Zero) * speed,
                MoveKind.Away => MathX.NormalizeOr(hero.Pos - intent.Point, Vector2.Zero) * speed,
                _ => Vector2.Zero,
            };
        }
        hero.Vel = v;
        var move = v;
        if (hero.KnockbackTime > 0f)
        {
            move = hero.KnockbackVel;
            hero.KnockbackTime = MathF.Max(0f, hero.KnockbackTime - World.Dt);
            if (hero.KnockbackTime == 0f) hero.KnockbackVel = Vector2.Zero;
        }
        hero.Pos += move * World.Dt;
    }

    /// <summary>
    /// 분리(4.6-2): 거리 d &lt; rᵢ + rⱼ + 8 인 이웃(가까운 순 최대 8)마다 방향(나 − 이웃) × (1 − d / (rᵢ + rⱼ + 8)), 멈춘 이웃은 가중 2.
    /// 희망 방향에 가중 1.2 로 더해 정규화 × 속도. 겹친 점은 <see cref="MathX.TieDir"/>.
    /// </summary>
    private Vector2 Separate(World world, Enemy e, int self, Vector2 desired)
    {
        var enemies = world.Enemies.Items;
        var radius = e.Radius + hash.MaxRadius + SeparationPad;
        var count = hash.Query(e.Pos, radius, _buf);
        while (count > QueryCapacity && radius > 1f)
        {
            radius *= 0.5f;
            count = hash.Query(e.Pos, radius, _buf);
        }
        count = Math.Min(count, QueryCapacity);

        // 가까운 순 8개(삽입 정렬, 같은 거리면 결과 순서 — 결정적)
        var n = 0;
        for (var k = 0; k < count; k++)
        {
            var j = _buf[k];
            if (j == self) continue;
            var d2 = Vector2.DistanceSquared(e.Pos, enemies[j].Pos);
            if (n == MaxNeighbors && d2 >= _nearD[n - 1]) continue;
            var at = n < MaxNeighbors ? n++ : n - 1;
            while (at > 0 && _nearD[at - 1] > d2)
            {
                _nearD[at] = _nearD[at - 1];
                _near[at] = _near[at - 1];
                at--;
            }
            _nearD[at] = d2;
            _near[at] = j;
        }

        var sep = Vector2.Zero;
        for (var k = 0; k < n; k++)
        {
            var o = enemies[_near[k]];
            var rr = e.Radius + o.Radius + SeparationPad;
            var d = MathF.Sqrt(_nearD[k]);
            if (d >= rr) continue;
            var away = d < 1e-4f ? MathX.TieDir(e.Id, o.Id) : (e.Pos - o.Pos) / d;
            var w = o.Intent.Kind == MoveKind.Hold ? HoldWeight : 1f;
            sep += away * (1f - d / rr) * w;
        }
        if (sep == Vector2.Zero) return desired;
        var speed = desired.Length();
        var dir = desired / speed + sep * SeparationWeight;
        return MathX.NormalizeOr(dir, desired / speed) * speed;
    }
}

/// <summary>
/// 단계 7 <c>Collision</c> (설계서 3.5·4.7, 소유 WP2). 부드러운 것 → 단단한 것: 적-적 원 해소(질량 r² 비, 2회) → 주인공-적(닿은 수 <c>Hero.TouchingCount</c>,
/// 큰 적은 주인공을 민다) → 장애물·경계(원-도형 최근접점, <c>phase_through</c> 는 둘레 도형·경계만, 입장 중은 건너뜀) → 아직 도형 안이면 clearance 기울기.
/// 끝에 <c>Enemy.MoveBlockedTime</c>(희망 방향으로 실제 이동이 막힌 시간)을 쓴다.
/// </summary>
public sealed class CollisionSystem(NavWorld nav, SpatialHash hash) : ISimSystem
{
    public const int Iterations = 2;
    /// <summary>큰 적: 이 반경 이상이면 주인공에게 단단하다(4.7-2).</summary>
    public const float HardRadius = 20f;
    /// <summary>막힘 판정: 희망 방향 진행이 기대의 이 비율 미만이면 막힌 틱.</summary>
    public const float BlockedProgress = 0.25f;

    private readonly int[] _buf = new int[LocomotionSystem.QueryCapacity];

    public void Tick(World world)
    {
        var enemies = world.Enemies.Items;
        for (var it = 0; it < Iterations; it++) ResolvePairs(enemies);
        ResolveHero(world, enemies);
        for (var i = 0; i < enemies.Length; i++)
        {
            var e = enemies[i];
            if (!e.IsAlive || e.Life == LifeState.Entering) continue;
            PushOut(ref e.Pos, e.Radius, e.PassThroughObstacles, e.Id);
        }
        PushOut(ref world.Hero.Pos, world.Hero.Radius, false, EntityId.Hero);
        UpdateBlocked(enemies);
    }

    private void ResolvePairs(ReadOnlySpan<Enemy> enemies)
    {
        for (var i = 0; i < enemies.Length; i++)
        {
            var a = enemies[i];
            if (!a.IsTargetable) continue;
            var count = Math.Min(hash.Query(a.Pos, a.Radius + hash.MaxRadius, _buf), _buf.Length);
            for (var k = 0; k < count; k++)
            {
                var j = _buf[k];
                if (j <= i) continue;
                var b = enemies[j];
                var delta = a.Pos - b.Pos;
                var d = delta.Length();
                var overlap = a.Radius + b.Radius - d;
                if (overlap <= 0f) continue;
                var n = d < 1e-4f ? MathX.TieDir(a.Id, b.Id) : delta / d;
                var ma = a.Radius * a.Radius;
                var mb = b.Radius * b.Radius;
                var total = ma + mb;
                var wa = total > 0f ? mb / total : 0.5f;
                a.Pos += n * (overlap * wa);
                b.Pos -= n * (overlap * (1f - wa));
            }
        }
    }

    private void ResolveHero(World world, ReadOnlySpan<Enemy> enemies)
    {
        var hero = world.Hero;
        var touching = 0;
        var count = Math.Min(hash.Query(hero.Pos, hero.Radius + hash.MaxRadius, _buf), _buf.Length);
        for (var k = 0; k < count; k++)
        {
            var e = enemies[_buf[k]];
            var delta = e.Pos - hero.Pos;
            var d = delta.Length();
            var overlap = hero.Radius + e.Radius - d;
            if (overlap <= 0f) continue;
            touching++;
            var n = d < 1e-4f ? MathX.TieDir(e.Id, EntityId.Hero) : delta / d;
            var hard = e.Def.Rank != DefRank.Normal || e.Radius >= HardRadius;
            if (hard) hero.Pos -= n * overlap;
            else e.Pos += n * overlap;
        }
        hero.TouchingCount = touching;
    }

    /// <summary>원을 걷기 막힘 도형·경계 밖으로(4.7-3·4).</summary>
    internal void PushOut(ref Vector2 pos, float r, bool boundaryOnly, EntityId id)
    {
        var obstacles = nav.Obstacles;
        var q = obstacles.BeginQuery();
        var x0 = (int)MathF.Floor((pos.X - r) / obstacles.CellSize);
        var y0 = (int)MathF.Floor((pos.Y - r) / obstacles.CellSize);
        var x1 = (int)MathF.Floor((pos.X + r) / obstacles.CellSize);
        var y1 = (int)MathF.Floor((pos.Y + r) / obstacles.CellSize);
        for (var cy = y0; cy <= y1; cy++)
            for (var cx = x0; cx <= x1; cx++)
                foreach (var s in obstacles.At(cx, cy))
                {
                    if (!obstacles.FirstVisit(s, q)) continue;
                    var shape = obstacles.Shapes[s];
                    if (!shape.BlocksWalk || (boundaryOnly && !shape.IsBoundary)) continue;
                    var min = obstacles.Min[s];
                    var max = obstacles.Max[s];
                    if (pos.X + r < min.X || pos.X - r > max.X || pos.Y + r < min.Y || pos.Y - r > max.Y) continue;
                    var fallback = MathX.NormalizeOr(nav.Grid.Gradient(pos), MathX.FromAngle(MathX.TieAngle(id, s)));
                    if (Geo.PushOut(obstacles.Polys[s], pos, r, fallback, out var push)) pos += push;
                }

        // 경계(바깥 마름모·상자): 안쪽으로 r 만큼
        var b = nav.BoundaryDistance(pos);
        if (b < r) pos += nav.BoundaryInward(pos) * (r - b);

        // 아직 도형 안이면 clearance 기울기로 한 번 더(좁은 틈에 여럿이 몰린 경우)
        if (!boundaryOnly)
        {
            var c = nav.Clearance(pos);
            if (c < 0f)
            {
                var g = MathX.NormalizeOr(nav.Grid.Gradient(pos), Vector2.Zero);
                pos += g * MathF.Min(-c, NavConst.CellSize * 0.5f);
            }
        }
    }

    private static void UpdateBlocked(ReadOnlySpan<Enemy> enemies)
    {
        for (var i = 0; i < enemies.Length; i++)
        {
            var e = enemies[i];
            if (!e.IsAlive) continue;
            var desired = e.Nav.Desired;
            var speed = desired.Length();
            if (speed < 1e-3f || e.KnockbackTime > 0f)
            {
                e.MoveBlockedTime = 0f;
                continue;
            }
            var progress = Vector2.Dot(e.Pos - e.PrevPos, desired / speed);
            if (progress < BlockedProgress * speed * World.Dt) e.MoveBlockedTime += World.Dt;
            else e.MoveBlockedTime = 0f;
        }
    }
}

/// <summary>
/// 단계 8 <c>SpatialRebuild</c> (설계서 3.5·4.8·10.1, 소유 WP2): 이동이 끝난 위치로 해시 다시 짓기(무기 조준·맞음 판정용),
/// 60틱마다 불변식 계측(<c>World.Options.Check</c>·시나리오가 켬 — <see cref="InvariantChecker"/>).
/// </summary>
public sealed class SpatialRebuildSystem(SpatialHash hash, InvariantChecker checker) : ISimSystem
{
    public void Tick(World world)
    {
        hash.Rebuild();
        if ((world.Options.Check || checker.Enabled) && world.Tick % InvariantChecker.Period == 0) checker.Sample();
    }
}
