namespace DiabloDefenceRemastered.Core.Sim;

/// <summary>
/// 한 판의 모든 상태 — 공통 조각 (WP1, 설계서 2.1 월드 컨텍스트·11.2). 시스템·부품은 이것 하나를 인자로 받는다.
/// 정적 싱글턴이 없으므로 시험이 월드를 여러 개 만들 수 있다.
/// 한 묶음만 쓰는 상태는 조각 파일에: <c>Sim/Director/World.Director.cs</c>(WP4), <c>Sim/Weapons/World.Combat.cs</c>(WP3).
/// </summary>
public sealed partial class World
{
    public const int TicksPerSecond = 60;
    /// <summary>틱 길이(초). 모든 속도·시간은 초 단위이고 틱마다 Dt 를 곱한다.</summary>
    public const float Dt = 1f / TicksPerSecond;

    public World(GameDefs defs, ulong seed, StageDef? stage = null)
    {
        Defs = defs;
        Seed = seed;
        Rng = new SimRandom(seed);
        Stage = stage;
        Map = stage?.Map.Def;
        if (Map is { } map)
        {
            // 배치 풀기·좌표 풀기는 여기서 한 번(4.1). 다른 묶음은 Arena·Resolved 만 읽고 IsoMath 를 따로 부르지 않는다
            Arena = ArenaLayout.Expand(map);
            Resolved = ResolvedMap.Resolve(map, Arena.Iso);
            MapSize = Arena.Iso.BoxSize;
        }
        else MapSize = CameraMath.DefaultViewSize;

        if (stage is not null && Resolved is not null) Lighting.Load(stage.Lighting, Resolved);

        Hero.Reset();
        if (defs.Rules.Hero is { } rules) Hero.Radius = rules.Radius;
        if (Resolved is { } r)
        {
            Hero.Pos = Hero.PrevPos = r.HeroStart;
            Hero.Facing = FacingMath.ToVector(r.HeroFacing);
        }
        else Hero.Pos = Hero.PrevPos = MapSize * 0.5f;
        ViewRect = CameraMath.ViewRect(Hero.Pos, MapSize, ViewSize);

        // 시야는 늘 있다(꺼지면 Enabled = false). 계산은 첫 BeginTick 이 한다 — 생성 뒤 주인공을 옮기는 설정이 있어서(4.11 "시작")
        Visibility = new VisibilityField(this);
    }

    // ── 정의·시계·난수 ──────────────────────────────────────────────

    public GameDefs Defs { get; }
    public StageDef? Stage { get; }
    public MapDef? Map { get; }
    /// <summary>맵 배치를 푼 격자(하위 칸 성질·둘레 표시·<c>LayoutHash</c>). 맵 없는 시험 월드는 null.</summary>
    public ArenaGrid? Arena { get; }
    /// <summary>입구·기준점·주인공 시작을 월드 px 로 푼 것. 맵 위치가 필요한 묶음은 이것만 읽는다(4.1). 맵 없는 시험 월드는 null.</summary>
    public ResolvedMap? Resolved { get; }
    /// <summary>외곽 상자 64N × (32N + H)(카메라·생성 띠 기준, 4.1). 맵 없는 시험 월드는 960×540.</summary>
    public Vector2 MapSize { get; set; }
    public ulong Seed { get; }
    public SimRandom Rng { get; }

    /// <summary>
    /// 스테이지 틱 = 끝난 스테이지 틱 수. 단계들이 도는 동안은 "이번 틱 번호"(첫 틱 0)이고 EndTick 끝에서 1 늘어난다.
    /// 일시정지·선택 창·히트 스톱 동안은 늘지 않는다.
    /// </summary>
    public int Tick { get; set; }
    public float StageTime => Tick * Dt;
    public float StageMinutes => Tick * Dt / 60f;

    /// <summary>이 월드를 돌리는 시뮬레이션(<see cref="SimulationBuilder.Build"/> 가 넣는다). <see cref="StageStepper"/> 가 쓴다.</summary>
    public Simulation? Simulation { get; internal set; }

    // ── 저장소 5개·주인공 ─────────────────────────────────────────────

    public EntityStore<Enemy> Enemies { get; } = new(() => new Enemy(), 512);
    public EntityStore<Projectile> Projectiles { get; } = new(() => new Projectile(), 1024);
    public EntityStore<Hazard> Hazards { get; } = new(() => new Hazard(), 128);
    public EntityStore<Telegraph> Telegraphs { get; } = new(() => new Telegraph(), 64);
    public EntityStore<Pickup> Pickups { get; } = new(() => new Pickup(), 512);
    public Hero Hero { get; } = new();

    // ── 이벤트·요청·명령 ───────────────────────────────────────────────

    public TickEvents Events { get; } = new();
    public PresentationBuffer Presentation { get; } = new();
    /// <summary>피해 요청 큐. DamageResolve 가 비운다.</summary>
    public DamageQueue Damage { get; } = new();
    /// <summary>이번 스테이지 틱의 명령(<see cref="StageStepper"/> 가 넣고 HeroControl 이 읽는다).</summary>
    public PlayerCommand Command { get; set; }
    /// <summary>시뮬레이션에 걸린 개발 옵션(--god 등). <see cref="ModuleContext.ApplyRunSetups"/> 가 각 IRunSetup 을 부르기 전에 넣는다.</summary>
    public RunOptions Options { get; set; } = RunOptions.Default;

    // ── 판 상태 ───────────────────────────────────────────────────────

    public RunOutcome Outcome { get; set; }
    /// <summary>Outcome 이 대기(…Pending)로 바뀐 스테이지 틱.</summary>
    public int OutcomeTick { get; set; }
    public ChoiceKind AwaitingChoice { get; set; }

    public Vector2 ViewSize { get; set; } = CameraMath.DefaultViewSize;
    /// <summary>카메라 시야(BeginTick 이 <see cref="CameraMath.ViewRect"/> 로 쓴다). 생성 띠의 기준.</summary>
    public RectF ViewRect { get; set; }

    /// <summary>히트 스톱 남은 앱 틱. 그동안 스테이지 시계가 멈춘다.</summary>
    public int FreezeTicks { get; set; }
    /// <summary>슬로모션 배율·남은 앱 틱·늘임 누적값(0..1, 보간 alpha). <see cref="StageStepper"/> 가 센다.</summary>
    public float SlowMoScale { get; set; } = 1f;
    public int SlowMoAppTicks { get; set; }
    public float SlowMoStretch { get; set; }

    // ── 모듈이 넣는 서비스 ─────────────────────────────────────────────

    /// <summary>WP2 NavModule 이 넣는다.</summary>
    public INavWorld Nav { get; set; } = null!;
    /// <summary>WP2 NavModule 이 넣는다.</summary>
    public ISpatialQuery Spatial { get; set; } = null!;
    /// <summary>WP3 CombatModule 이 넣는다.</summary>
    public IHostileQuery Hostiles { get; set; } = null!;
    /// <summary>WP3 CombatModule 이 넣는다.</summary>
    public ICombatSpawner Spawner { get; set; } = null!;
    /// <summary>WP3 CombatModule 이 넣는다. 없는 시험 월드면 null.</summary>
    public IProgression? Progression { get; set; }
    public RunStats RunStats { get; } = new();

    // ── WP4 가 쓰고 WP5·WP6 이 읽는 것 ─────────────────────────────────

    public LightingState Lighting { get; } = new();
    public List<BossBar> BossBars { get; } = [];
    public BannerState Banner;

    // ── 시야 (v0.5, 4.11) ─────────────────────────────────────────────

    /// <summary>
    /// 전장의 안개 시야. 쓰기 BeginTick 만(<see cref="VisibilityField"/> 구현 WP1c, 그 뒤 WP2 소유).
    /// 읽기 WP4(사격·돌진), WP3(공정성 수 세기만), WP5(안개), WP6(미니맵), WP7(해시). WP3 조준·WP8 소리·자동 조작은 읽지 않는다.
    /// </summary>
    public VisibilityField Visibility { get; }

    /// <summary>공정성 계측 누적 수(v0.5.1, 10.1). 쓰기 WP3·WP4, 읽기 WP2 <c>InvariantChecker</c>·WP7 로그.</summary>
    public FairnessCounters Fairness { get; } = new();

    // ── 도우미 ────────────────────────────────────────────────────────

    /// <summary>히트 스톱(앱 틱). 겹치면 긴 쪽.</summary>
    public void Freeze(int appTicks) => FreezeTicks = Math.Max(FreezeTicks, appTicks);

    /// <summary>슬로모션(보스 처치 ×0.3). 틱 길이는 그대로 두고 앱 틱마다 scale 을 쌓아 1 이 될 때만 스테이지 틱을 돈다.</summary>
    public void SlowMotion(float scale, int appTicks)
    {
        SlowMoScale = Math.Clamp(scale, 0.01f, 1f);
        SlowMoAppTicks = Math.Max(SlowMoAppTicks, appTicks);
    }

    /// <summary>
    /// 승리 처리 시작 (설계서 3.5 끝): VictoryPending, 살아 있는 적 전부 Dead(Collapse).
    /// 적 투사체·장판·예고는 EndTick 이 지운다. boss 승리는 DeathHooks(WP4), survive 승리는 EndTick 이 부른다.
    /// </summary>
    public void BeginVictory()
    {
        if (Outcome != RunOutcome.Running) return;
        Outcome = RunOutcome.VictoryPending;
        OutcomeTick = Tick;
        var enemies = Enemies.Items;
        for (var i = 0; i < enemies.Length; i++)
        {
            var e = enemies[i];
            if (!e.IsAlive) continue;
            e.Dead = true;
            e.KillCause = KillCause.Collapse;
        }
    }
}
