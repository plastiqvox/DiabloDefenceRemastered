using DiabloDefenceRemastered.Core.Sim.Enemies;

namespace DiabloDefenceRemastered.Core.Sim;

/// <summary>
/// 적 — WP4 조각 (설계서 11.2·6.6). 부품 슬롯·독점 행동·페이즈·이동 지시·관계·광폭화·공포 재무장 시각.
/// 풀에서 다시 쓰므로 <see cref="ResetBehavior"/> 가 모두 지운다.
/// </summary>
public sealed partial class Enemy
{
    /// <summary>정의 + 접두사 조합으로 만든 부품 표(월드가 한 번 만들어 같은 조합끼리 같이 씀).</summary>
    public EnemyKit Kit = null!;
    public PartSlots Parts;
    public ActionState Action;
    /// <summary>지금 페이즈(0 = 정의 <c>patterns</c>, k = <c>phases[k-1]</c>).</summary>
    public int PhaseIdx;
    public MoveOrder Order;
    /// <summary>호위가 따르는 대장.</summary>
    public EntityId Leader;
    /// <summary>불러낸 적(소환 상한 세기).</summary>
    public EntityId Summoner;
    public SpawnSource Source;
    /// <summary>광폭화가 걸리는 스테이지 틱(−1 = 없음).</summary>
    public int EnrageAt;
    /// <summary>다시 겁먹을 수 있는 틱(panic_on_ally_death).</summary>
    public int PanicRearmTick;
    /// <summary>이 틱까지 멈춰 있다(분열체 delay·등장 연출).</summary>
    public int WakeTick;
    /// <summary>입장(<see cref="LifeState.Entering"/>)의 목표 진입점(월드 px).</summary>
    public Vector2 EntryTarget;
    /// <summary>호위 대형을 푸는 주인공 거리(escort <c>break_range</c>).</summary>
    public float FollowBreak;
    /// <summary>생성 때 굳힌 이동 속도(정의 × 등급 × 시간 없음 — 버프·광폭화 전).</summary>
    public float BaseMoveSpeed;
    /// <summary>접두사 <c>speed_pct</c>·<c>attack_cooldown_pct</c> 합(생성 때 굳힘, 버프는 매 틱 더함).</summary>
    public float AffixSpeedPct;
    public float AffixCooldownPct;
    /// <summary>최종 크기(정의 scale × 등급 배율, max_scale 로 자름).</summary>
    public float FinalScale;
    /// <summary>광폭화 배율(걸리기 전 1).</summary>
    public float EnrageDamageMul;
    public float EnrageSpeedMul;
    /// <summary>페이즈가 덮어쓴 속도(0 = 없음)·발광.</summary>
    public float PhaseSpeed;
    public GlowDef? PhaseGlow;
    /// <summary>점화 중인 자폭(표시 — 발광 번쩍임·ForceShown).</summary>
    public bool Igniting;
    /// <summary>돌진 예고 중(붉은 번쩍임).</summary>
    public bool ChargeWarn;
    /// <summary>보스 순간이동 사라짐 시작 틱·길이(틱, 0 = 사라지는 중 아님 — <c>Visual.AlphaMul</c> 1 → 0, 6.20 ⑧).</summary>
    public int FadeOutTick;
    public int FadeOutTicks;
    /// <summary>순간이동 뒤 이 틱까지 그리지 않는다(<c>AlphaMul</c> 0). 그 뒤 깰 때(<see cref="WakeTick"/>)까지 0 → 1(6.20 ⑧).</summary>
    public int HiddenUntilTick;
    /// <summary>입장·등장 끝에 생성 훅을 부를 생성 맥락(호위 데려오기는 들어온 뒤).</summary>
    public SpawnContext PendingSpawn;
    public bool SpawnHooksPending;
    /// <summary>
    /// 우두머리 고유 이름 굴림(2026-11-14, 기획 9.2 — <c>rules.ranks.unique_names</c> 의 first·second·title 번호를 10 비트씩, −1 = 없음).
    /// 그리기만 읽는다(<see cref="UniqueNames"/>).
    /// </summary>
    public int NameRoll;
    /// <summary>접두사가 이 적의 주인공 피해 요청에 얹는 표시(<c>hit_flags</c> — 냉기 강화 <see cref="DamageFlags.Chill"/>). 근접·이 적이 쏜 탄 모두.</summary>
    public DamageFlags HitFlags;
    /// <summary>접두사 <c>extra_shots</c> 합(다중 사격 — <c>shoot_projectile</c> 한 번에 더 쏘는 탄 수).</summary>
    public int ExtraShots;

    partial void ResetBehavior()
    {
        Kit = null!;
        Parts.Clear();
        Action = ActionState.None;
        PhaseIdx = 0;
        Order = default;
        Leader = EntityId.None;
        Summoner = EntityId.None;
        Source = SpawnSource.Interval;
        EnrageAt = -1;
        PanicRearmTick = 0;
        WakeTick = 0;
        EntryTarget = Vector2.Zero;
        FollowBreak = 0f;
        BaseMoveSpeed = 0f;
        AffixSpeedPct = AffixCooldownPct = 0f;
        FinalScale = 1f;
        EnrageDamageMul = EnrageSpeedMul = 1f;
        PhaseSpeed = 0f;
        PhaseGlow = null;
        Igniting = ChargeWarn = false;
        PendingSpawn = default;
        SpawnHooksPending = false;
        FadeOutTick = FadeOutTicks = HiddenUntilTick = 0;
        NameRoll = -1;
        HitFlags = DamageFlags.None;
        ExtraShots = 0;
    }
}
