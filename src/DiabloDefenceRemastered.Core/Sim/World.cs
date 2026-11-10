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

    /// <summary>
    /// 방 없는 월드(v0.7.1 — 3.6 결정 4 "생성자 둘"): <see cref="Room"/> == null, 그 액트의 보스 맵(없으면 첫 일반 맵 — <c>slice</c> 판) 하나에서 v0.6 처럼 돈다.
    /// 시험·<c>--preview</c>·<c>--map</c> 만 쓴다. 게임 판은 늘 <see cref="RunSession"/>(방 월드)이다.
    /// </summary>
    /// <param name="heroClass">
    /// 고른 직업(v0.6, 6.16). 앱(WP6·WP7)이 <c>--hero</c> → 저장의 마지막 직업 → <c>rules.hero.class</c> 순으로 정하고 꺼진 직업이면 기본 직업으로 바꿔 넘긴다.
    /// null 이면 <c>rules.hero.class</c>. 시뮬레이션은 묶음을 모른다(같은 씨앗·스테이지·직업이면 같은 결과).
    /// </param>
    public World(GameDefs defs, ulong seed, StageDef? stage = null, HeroClassDef? heroClass = null)
        : this(defs, seed, stage, heroClass, null)
    {
    }

    /// <summary>
    /// 방 월드(v0.7.1 — 3.6 결정 1·4): 방 하나 = <c>World</c> 하나. 씨앗 = <c>room.Plan.RoomSeed</c>, 액트 = <c>room.Run.Act.Act</c>, 직업 = <c>room.Run.HeroClass</c>,
    /// 맵 = <c>room.Plan.Map</c> — 서로 다른 값이 들어올 길이 없게 이 생성자 하나로만 만든다(<see cref="RunSession"/>·시험).
    /// 주인공은 들어온 문 진입점(<c>inside</c>)에서 <c>center</c> 쪽 방위로 서고, 첫 틱 전에 그 액트의 지난 <c>light_change</c>(<c>RoomSchedule.Replay</c>)를 다시 적용한다.
    /// </summary>
    public World(GameDefs defs, RoomStart room)
        : this(defs, room.Plan.RoomSeed, room.Run.Act.Act, room.Run.HeroClass, room)
    {
    }

    private World(GameDefs defs, ulong seed, StageDef? stage, HeroClassDef? heroClass, RoomStart? room)
    {
        Defs = defs;
        Seed = seed;
        Rng = new SimRandom(seed);
        Stage = stage;
        HeroClass = heroClass ?? defs.Rules.Hero?.Class.Def;
        Map = room is not null ? room.Plan.Map : NoRoomMap(stage);
        if (Map is { } map)
        {
            // 배치 풀기·좌표 풀기는 여기서 한 번(4.1). 다른 묶음은 Arena·Resolved 만 읽고 IsoMath 를 따로 부르지 않는다
            Arena = ArenaLayout.Expand(map);
            Resolved = ResolvedMap.Resolve(map, Arena.Iso);
            InnerRooms = RoomFloors.Compute(map, Arena);
            Gates = GateZones.Compute(map, Arena);
            MapSize = Arena.Iso.BoxSize;
        }
        else MapSize = CameraMath.DefaultViewSize;

        if (room is not null)
        {
            var rules = defs.Rules.Rooms ?? throw new InvalidOperationException("방 월드에는 rules.rooms 가 있어야 한다(6.10)");
            if (Resolved is null) throw new InvalidOperationException($"방 {room.Plan.Act}:{room.Plan.Number} 에 맵이 없다");
            Run = room.Run;
            Room = new RoomState(room, rules);
            RunStats = room.Run.Stats;
        }
        else RunStats = new RunStats();

        if (stage is not null && Resolved is not null)
        {
            Lighting.Load(stage.Lighting, Resolved);
            if (room is not null)
            {
                // 첫 틱 전에 지난 조명 바꾸기를 시간 순으로 다시 적용(v0.7.1) — 감독 없이도 서고 첫 틱 시야(1단계)가 바뀐 반경으로 계산된다
                var schedule = RoomEvents.Schedule(stage, room.Plan, defs.Rules.Rooms!, room.StartClockTicks);
                foreach (var e in schedule.Replay)
                    if (e.Params is LightChangeParams change) Lighting.Apply(change);
            }
        }
        InnerRoomPresence.Reset(InnerRooms.Rooms.Count);

        Hero.Reset();
        Hero.Radius = HeroClass?.Radius ?? 14f;
        if (Resolved is { } r)
        {
            if (room is not null)
            {
                var start = r.Entry(room.Plan.GateIn).Inside;
                Hero.Pos = Hero.PrevPos = start;
                Room!.RewardSpot = start;   // 보상 단계 전의 값(3.6) — RoomFlow 가 보상 단계에 들어갈 때 정한다
                // 시작 방향 = 기준점 center 쪽(v0.8.1 — v0.7 의 rules.rooms.reward_anchor 를 없앰. center 는 13.5-23 ③ 이 늘 요구)
                var toCenter = r.Anchor(RoomMapRules.CenterAnchor) - start;
                Hero.Facing = toCenter.LengthSquared() > 1e-6f
                    ? FacingMath.FromColumn(FacingMath.ToColumn(Vector2.Normalize(toCenter), 8), 8)
                    : FacingMath.ToVector(r.HeroFacing);
            }
            else
            {
                Hero.Pos = Hero.PrevPos = r.HeroStart;
                Hero.Facing = FacingMath.ToVector(r.HeroFacing);
            }
        }
        else Hero.Pos = Hero.PrevPos = MapSize * 0.5f;
        ViewRect = CameraMath.ViewRect(Hero.Pos, MapSize, ViewSize);

        // 시야는 늘 있다(꺼지면 Enabled = false). 계산은 첫 BeginTick 이 한다 — 생성 뒤 주인공을 옮기는 설정이 있어서(4.11 "시작")
        Visibility = new VisibilityField(this);
    }

    /// <summary>방 없는 월드의 맵: 보스 맵, 없으면 첫 일반 맵(<c>slice</c> 판은 보스 맵이 없다).</summary>
    private static MapDef? NoRoomMap(StageDef? stage)
    {
        if (stage is null) return null;
        if (stage.RoomMaps.Boss is { } boss) return boss.Def;
        return stage.RoomMaps.Normal.Count > 0 ? stage.RoomMaps.Normal[0].Def : null;
    }

    // ── 정의·시계·난수 ──────────────────────────────────────────────

    public GameDefs Defs { get; }
    public StageDef? Stage { get; }
    /// <summary>
    /// 이 판의 직업(v0.6, 6.16 — 생성자 인자 ?? <c>rules.hero.class</c>, 바뀌지 않음). 주인공 만들기(WP3)·그림(WP5)·소리(WP8)·해시·로그(WP7)가 이것만 읽는다.
    /// 규칙 없는 시험 월드는 null(주인공 반경 14).
    /// </summary>
    public HeroClassDef? HeroClass { get; }
    public MapDef? Map { get; }
    /// <summary>맵 배치를 푼 격자(하위 칸 성질·둘레 표시·<c>LayoutHash</c>). 맵 없는 시험 월드는 null.</summary>
    public ArenaGrid? Arena { get; }
    /// <summary>입구·기준점·주인공 시작을 월드 px 로 푼 것. 맵 위치가 필요한 묶음은 이것만 읽는다(4.1). 맵 없는 시험 월드는 null.</summary>
    public ResolvedMap? Resolved { get; }
    /// <summary>
    /// <b>안쪽 방</b> 바닥(v0.6.3, 6.11 "방 바닥" — 맵을 읽을 때 한 번. v0.7.1 에 <c>Rooms</c> 에서 이름을 바꿈 — 판 단위 방 <see cref="Room"/> 과 한 글자 차이였다, 11.2).
    /// 방 체류 압박·방 보급(WP4)·방 들르기 "방 안"(WP4 <c>AutoplayPolicy</c>)·로그 <c>INNER_ROOMS</c>(WP7)가 이것만 읽는다.
    /// 맵 없는 시험 월드·<c>room_areas</c> 없는 맵은 <see cref="RoomFloors.Empty"/>.
    /// </summary>
    public RoomFloors InnerRooms { get; } = RoomFloors.Empty;
    /// <summary>
    /// 문 틈 칸(v0.7, 6.11 — 생성자에서 한 번 <c>GateZones.Compute</c>). 나가는 자리 판정(<c>RoomFlow</c>), 봉인 그리기·문 위 아이콘(WP5), 미니맵 문 점(WP6). 맵 없는 월드는 빈 사전.
    /// </summary>
    public IReadOnlyDictionary<string, GateZone> Gates { get; } = GateZones.Empty;
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
    /// <summary>
    /// 난이도 분 t(v0.7, 3.1·3.6) = <c>Room?.T ?? StageMinutes</c>. 감독의 구간·배율·이벤트 판정과 규칙 쪽 시각은 이것을 쓴다 — 방 월드는 방마다 <see cref="Tick"/> 이 0 부터라
    /// <see cref="StageMinutes"/> 는 그 방 안의 경과 시간일 뿐이다.
    /// </summary>
    public float DifficultyMinutes => Room?.T ?? StageMinutes;

    /// <summary>
    /// 주인공 시간 효과(버프·마력 폭주 — WP3)가 이번 틱에 흐르는가(v0.8, 3.6 결정 1 "시간 효과 시계") = <c>Room is null || Room.TimedEffectsRun</c>.
    /// WP3 가 시간 효과의 남은 틱을 줄이는 자리에서 읽는다 — 보상·문 단계와 다음 방의 들어옴 동안 멈춘다.
    /// </summary>
    public bool TimedEffectsRun => Room is null || Room.TimedEffectsRun;

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
    /// <summary>상인 창 명령(v0.9, 6.19 — WP3 가 넣는다). 없는 시험 월드면 null. 상인 상태 자체는 <c>Room.Shop</c>(<see cref="ShopState"/>).</summary>
    public IShop? ShopService { get; set; }
    /// <summary>
    /// 판 누적 기록. 방 월드는 <c>Run.Stats</c> 를 가리키고(v0.7 — 방 사이에 이어짐), 방 없는 월드는 새로 만든다. 지우는 곳이 없다(v0.7.1, 3.6).
    /// </summary>
    public RunStats RunStats { get; }

    // ── WP4 가 쓰고 WP5·WP6 이 읽는 것 ─────────────────────────────────

    public LightingState Lighting { get; } = new();
    /// <summary>주인공이 선 안쪽 방과 안쪽 방마다 누적(v0.6.3 — 방 체류 압박·첫 플레이 로그. v0.7.1 에 <c>RoomPresence</c> 에서 이름을 바꿈). 방 번호 = <see cref="InnerRooms"/> 순서.</summary>
    public RoomPresence InnerRoomPresence { get; } = new();
    public List<BossBar> BossBars { get; } = [];
    public BannerState Banner;

    // ── 판·방 (v0.7, 3.6) ─────────────────────────────────────────────

    /// <summary>
    /// 이 방의 흐름 상태(v0.7). 방 없는 월드는 null(<c>RoomFlow</c> 꺼짐). 쓰기는 생성자와 <c>RoomFlowSystem</c>(16단계)뿐.
    /// </summary>
    public RoomState? Room { get; }
    /// <summary>판 상태(v0.7 — 판 수명, <see cref="RunSession"/> 이 넣음). 방 없는 월드는 null.</summary>
    public RunState? Run { get; }

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
    /// 적 투사체·장판·예고는 EndTick 이 지운다. v0.7: 방 월드는 판의 마지막 방에서 <c>RoomFlow</c>(보스 처치·정리 끝)가, 방 없는 월드는 DeathHooks(WP4, 보스 처치)가 부른다.
    /// </summary>
    public void BeginVictory()
    {
        if (Outcome != RunOutcome.Running) return;
        Outcome = RunOutcome.VictoryPending;
        OutcomeTick = Tick;
        CollapseEnemies();
    }

    /// <summary>
    /// 무너짐(v0.7, 3.6): 살아 있는 적 전부 Dead(<see cref="KillCause.Collapse"/> — 훅·보상·처치 수 없음). <see cref="BeginVictory"/>·정리 안전장치·마지막이 아닌 보스 처치가 부른다.
    /// 적 투사체·장판·예고는 <c>EndTick</c> 이 승리 대기·<c>Room.Phase ≥ Reward</c> 동안 지운다.
    /// </summary>
    public void CollapseEnemies()
    {
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
