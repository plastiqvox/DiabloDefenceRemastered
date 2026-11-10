using DiabloDefenceRemastered.Core.Nav;

namespace DiabloDefenceRemastered.Core.Sim.Movement.Invariants;

/// <summary>
/// 불변식 계측 결과(설계서 10.1) — 방(월드) 하나의 누적. 시나리오는 방마다 <see cref="AddTo"/> 로 판 합을 만든다.
/// 공정성 세 열·<c>kite_switches</c> 는 <c>World.Fairness</c>(판 합은 <c>RunState.Fairness</c>, WP1e <c>AddTo</c>)에 있고 여기서는 읽어 줄로만 낸다.
/// </summary>
public sealed class InvariantReport
{
    /// <summary>표본 수(60틱마다 하나).</summary>
    public int Samples { get; set; }
    /// <summary>유한하지 않은 위치(합).</summary>
    public int Nan { get; set; }
    /// <summary>입장 중이 아닌데 바깥 마름모 밖(합).</summary>
    public int OutOfMap { get; set; }
    /// <summary>통과·입장 중이 아닌데 걷기 막힘 도형 부호 거리 &lt; −2px(합).</summary>
    public int InObstacle { get; set; }
    /// <summary>같은 적에서 <c>in_obstacle</c> 이 이어진 가장 긴 표본 수(기준: 1 이하 = 1초 넘게 이어지지 않음).</summary>
    public int InObstacleLongest { get; set; }
    /// <summary>3초 동안 8px 미만 이동한 쫓는 적(합)·한 표본의 최댓값·그때 살아 있던 적.</summary>
    public int Stuck { get; set; }
    public int StuckMax { get; set; }
    public int StuckMaxAlive { get; set; }
    /// <summary>살아 있는 적이 절대 상한(<c>caps.enemies_absolute</c>)을 넘은 표본 수.</summary>
    public int OverCap { get; set; }
    /// <summary>
    /// (v0.6.1) 입장 중이 아닌 일반 적(보스·중간 보스 아님) 중 주인공에서 시야 반경 R 안인데 <c>Visibility.IsVisible</c> 이 거짓인 적 수(합).
    /// 공정성 세 열 0 이 "벽 가림이 없어서" 가 아님을 보이는 증거 — 액트 실행 기준 &gt; 0.
    /// </summary>
    public int Occluded { get; set; }

    /// <summary>기준(10.1): nan = out_of_map = over_cap = 0, in_obstacle 이 1초 넘게 이어지지 않음, stuck 최댓값 ≤ 살아 있는 적의 2%.</summary>
    public bool Passed => Nan == 0 && OutOfMap == 0 && OverCap == 0 && InObstacleLongest <= 1
                          && StuckMax <= MathF.Max(0f, StuckMaxAlive * 0.02f);

    public void AddTo(InvariantReport total)
    {
        total.Samples += Samples;
        total.Nan += Nan;
        total.OutOfMap += OutOfMap;
        total.InObstacle += InObstacle;
        total.InObstacleLongest = Math.Max(total.InObstacleLongest, InObstacleLongest);
        total.Stuck += Stuck;
        if (StuckMax > total.StuckMax || (StuckMax == total.StuckMax && StuckMaxAlive > total.StuckMaxAlive))
        {
            total.StuckMax = StuckMax;
            total.StuckMaxAlive = StuckMaxAlive;
        }
        total.OverCap += OverCap;
        total.Occluded += Occluded;
    }

    /// <summary>로그 한 줄(<c>INVARIANTS</c>) — 공정성 열을 같이 붙인다.</summary>
    public string Format(FairnessCounters fairness) =>
        $"INVARIANTS samples={Samples} nan={Nan} out_of_map={OutOfMap} in_obstacle={InObstacle} in_obstacle_run={InObstacleLongest} " +
        $"stuck={Stuck} stuck_max={StuckMax}/{StuckMaxAlive} over_cap={OverCap} occluded={Occluded} " +
        $"unseen_shots={fairness.UnseenShots} unseen_charges={fairness.UnseenCharges} hidden_hits={fairness.HiddenHits} kite_switches={fairness.KiteSwitches}";
}

/// <summary>
/// 불변식 계측기 (설계서 10.1, 소유 WP2). <c>SpatialRebuild</c> 가 60틱마다 <see cref="Sample"/> 을 부른다(<c>--check</c>·시나리오·<see cref="Enabled"/>).
/// 적마다 이어짐 상태는 <see cref="NavAgentState"/> 의 계측 칸에 둔다(옆 사전 없음, 할당 0). 결과는 <see cref="World.Invariants"/>.
/// </summary>
public sealed class InvariantChecker(World world, NavWorld nav)
{
    public const int Period = 60;
    /// <summary><c>in_obstacle</c> 문턱(도형 부호 거리 &lt; −2px).</summary>
    public const float ObstacleDepth = 2f;
    /// <summary><c>stuck</c>: 3초(표본 셋) 동안 8px 미만.</summary>
    public const int StuckSamples = 3;
    public const float StuckDistance = 8f;
    /// <summary><c>stuck</c> 에서 빼는 거리(주인공까지 ≤ 몸 반경 합 + 이동 사거리 근사 + 32).</summary>
    public const float StuckReach = Navigator.ReachMargin + 32f;

    /// <summary>시험·시나리오가 <c>--check</c> 없이 켠다.</summary>
    public bool Enabled { get; set; }

    public InvariantReport Report => world.Invariants;

    public void Sample()
    {
        var r = world.Invariants;
        r.Samples++;
        var hero = world.Hero;
        if (!MathX.IsFinite(hero.Pos)) r.Nan++;
        else if (!nav.IsInsideMap(hero.Pos)) r.OutOfMap++;

        var alive = 0;
        var stuck = 0;
        var vis = world.Visibility;
        var enemies = world.Enemies.Items;
        for (var i = 0; i < enemies.Length; i++)
        {
            var e = enemies[i];
            if (!e.IsAlive) continue;
            alive++;
            ref var s = ref e.Nav;
            if (!MathX.IsFinite(e.Pos))
            {
                r.Nan++;
                continue;
            }
            var entering = e.Life == LifeState.Entering;
            if (!entering && !nav.IsInsideMap(e.Pos)) r.OutOfMap++;

            if (!entering && !e.PassThroughObstacles && nav.WalkPenetration(e.Pos, boundaryOnly: false) > ObstacleDepth)
            {
                r.InObstacle++;
                s.ObstacleRun = (byte)Math.Min(s.ObstacleRun + 1, 255);
                r.InObstacleLongest = Math.Max(r.InObstacleLongest, s.ObstacleRun);
            }
            else s.ObstacleRun = 0;

            // 멈춘 적: 입장·기절·멈춤·직선(독점 행동)·도망이 아니고 주인공에게서 멀리 있는데 3초 동안 8px 미만
            var chasing = e.Life == LifeState.Active && (e.Intent.Kind is MoveKind.SeekHero or MoveKind.SeekPoint or MoveKind.Heading)
                          && Vector2.Distance(e.Pos, hero.Pos) > e.Radius + hero.Radius + StuckReach;
            if (!chasing)
            {
                s.CheckSamples = 0;
                s.CheckAnchor = e.Pos;
            }
            else if (s.CheckSamples == 0)
            {
                s.CheckSamples = 1;
                s.CheckAnchor = e.Pos;
            }
            else if (++s.CheckSamples > StuckSamples)
            {
                if (Vector2.Distance(e.Pos, s.CheckAnchor) < StuckDistance) stuck++;
                s.CheckSamples = 1;
                s.CheckAnchor = e.Pos;
            }

            if (vis.Enabled && !entering && e.Life != LifeState.Emerging && e.Def.Rank == DefRank.Normal
                && Vector2.Distance(e.Pos, vis.HeroPos) <= vis.Radius && !vis.IsVisible(e.Pos))
                r.Occluded++;
        }
        r.Stuck += stuck;
        if (stuck > r.StuckMax || (stuck == r.StuckMax && alive > r.StuckMaxAlive))
        {
            r.StuckMax = stuck;
            r.StuckMaxAlive = alive;
        }
        var cap = world.Defs.Rules.Caps?.EnemiesAbsolute ?? 360;
        if (alive > cap) r.OverCap++;
    }
}
