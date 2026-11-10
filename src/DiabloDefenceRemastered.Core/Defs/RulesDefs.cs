namespace DiabloDefenceRemastered.Core.Defs;

/// <summary>
/// <c>rules</c>(객체 하나). 여러 파일에서 하위 키 단위로 합친다(같은 하위 키가 두 파일에 있으면 오류, 설계서 6.1).
/// WP3 <c>rules/hero.json</c> = hero·xp_curve·level_up·level_up_push, WP4 <c>rules/enemies.json</c> = ranks·enemies·caps·spawn,
/// WP5 <c>rules/sight.json</c> = sight(v0.5, 첫 판 WP0a — 시뮬레이션 값 줄은 WP4 와 합의, 6.10), WP8 <c>rules/audio.json</c> = audio.
/// </summary>
public sealed record RulesDef
{
    public HeroRulesDef? Hero { get; init; }
    public IReadOnlyList<XpCurveSegment> XpCurve { get; init; } = [];
    public LevelUpRulesDef? LevelUp { get; init; }
    public LevelUpPushDef? LevelUpPush { get; init; }
    public RanksDef? Ranks { get; init; }
    /// <summary>적 정의 검사 기준(예고 필수 판정, 6.5·6.14 의 13.5-13).</summary>
    public EnemyRulesDef? Enemies { get; init; }
    public CapsDef? Caps { get; init; }
    public SpawnRulesDef? Spawn { get; init; }
    public AudioRulesDef? Audio { get; init; }
    /// <summary>
    /// 전장의 안개 값(v0.5, 6.10). 기본값을 코드에 두지 않는다 — 스테이지가 있는 데이터에 없으면 로딩 오류(WP7).
    /// 없으면 <c>World.Visibility.Enabled</c> 가 거짓(맵 없는 시험 월드처럼 안개 없음).
    /// </summary>
    public SightRulesDef? Sight { get; init; }
    /// <summary>
    /// 방 흐름·문·넓이 배율 값(v0.7, 6.10 — WP4 <c>rules/rooms.json</c>). 키는 모두 <c>required</c> — 기본값을 코드에 두지 않는다.
    /// 스테이지가 있는 데이터에 없으면 로딩 오류(WP7). 없으면(시험 월드) 방 흐름을 쓰는 코드(<c>RunPlanner</c>·<c>RoomFlow</c>·<c>RunSession</c>)가 예외.
    /// </summary>
    public RoomRulesDef? Rooms { get; init; }
}

/// <summary>
/// <c>rules.rooms</c> (v0.7 → v0.8 WP1f, 설계서 6.10, 기획 2.5·10.8·11.4·15절 첫 값). 쓰는 곳: <see cref="Grace"/>·<see cref="CleanupLimit"/>·<see cref="EliteSpawnCap"/> = <c>RoomFlow</c>·감독,
/// <see cref="Gates"/>·<see cref="Exits"/> = <c>RunPlanner</c>, <see cref="AreaScale"/>·<see cref="Stairs"/> = <c>RunSession</c>, <see cref="RewardSpot"/>·<see cref="FallbackReward"/> = <c>RoomFlow</c>,
/// <see cref="Transition"/>·<see cref="TransitionFloor"/> = <c>PlayScene</c>(앱 틱), <see cref="AutoplayDoorSkip"/> = <c>AutoplayPolicy</c>.
/// <see cref="Transition"/>·<see cref="TransitionFloor"/> 를 뺀 값을 바꾸면 같은 씨앗의 판이 바뀐다(WP4 가 바꾸고 <c>Scenario</c>·13.5-23 을 다시 돌림).
/// v0.8: <c>remaining_marker</c>(남은 적 점은 정리 시작부터 모두)·<c>reward_anchor</c>·<c>chest_ring</c>(보상 자리 = 주인공 앞, 3.6)은 없앴다 — 남아 있으면 모르는 속성 오류.
/// </summary>
public sealed record RoomRulesDef
{
    /// <summary>들어옴 단계 길이(초, 3). 그동안 생성 없음.</summary>
    public required float Grace { get; init; }
    /// <summary>정리 안전장치(초, 25 — v0.8. v0.7 은 45) — 넘으면 남은 적을 무너뜨린다.</summary>
    public required float CleanupLimit { get; init; }
    /// <summary>정예 방 생성이 멈추는 방 시계(2:30).</summary>
    public required ClockTime EliteSpawnCap { get; init; }
    /// <summary>방 이동 어두워짐 길이(초, 앱 틱 — 그림 값).</summary>
    public required float Transition { get; init; }
    /// <summary>층이 바뀌는 문(<c>RoomExit.NextFloor</c> ≠ 0)의 넘김 어둠(초, 1.0 — v0.8, 앱 틱 그림 값).</summary>
    public required float TransitionFloor { get; init; }
    public required RoomGatesDef Gates { get; init; }
    public required ExitsDef Exits { get; init; }
    public required AreaScaleDef AreaScale { get; init; }
    /// <summary>보상 자리(v0.8, 3.6 "보상 자리" — 정리가 끝난 순간 주인공 앞).</summary>
    public required RewardSpotDef RewardSpot { get; init; }
    /// <summary>문 열기 거르기로 출구 문을 다 못 채울 때 쓰는 보상(보물 상자).</summary>
    public required DefRef<RewardDef> FallbackReward { get; init; }
    /// <summary>계단에서 여는 칸 수(v0.8, 3.6 "계단 보상" — <c>RunSlots.OpenAt</c>).</summary>
    public required StairsRulesDef Stairs { get; init; }
    /// <summary>자동 조작이 문 단계를 이 초 안에 못 끝내면 <c>DOOR_SKIP</c>(보상 단계면 <c>REWARD_STALL</c> 기록).</summary>
    public required float AutoplayDoorSkip { get; init; }
}

/// <summary>
/// 보상 자리(v0.8, 6.10·3.6 — <c>RewardSpot.Find</c>/<c>Row</c>): 주인공 발에서 바라보는 쪽으로 <see cref="Distance"/> px,
/// 여럿이면 수직 방향 <see cref="Spacing"/> px 간격 한 줄, 걸어 간 길의 clearance ≥ <see cref="Clearance"/> px.
/// </summary>
public sealed record RewardSpotDef
{
    /// <summary>주인공 앞 거리(px, 100).</summary>
    public required float Distance { get; init; }
    /// <summary>여럿 놓을 때 간격(px, 40).</summary>
    public required float Spacing { get; init; }
    /// <summary>자리까지 직선으로 걸을 수 있는 clearance 문턱(px, 30).</summary>
    public required float Clearance { get; init; }
}

/// <summary>계단 보상(v0.8, 6.10 — 기획 v0.8.1 2.5): 액트가 바뀔 때마다 여는 칸 수. 열린 칸 = <c>RunSlots.OpenAt</c>(상한 6).</summary>
public sealed record StairsRulesDef
{
    public required int WeaponSlots { get; init; }
    public required int PassiveSlots { get; init; }
}

/// <summary>문 이름(v0.7, 6.10). 모두 맵의 문 넷 안(13.5-23 ④).</summary>
public sealed record RoomGatesDef
{
    /// <summary>일반·정예 방의 들어오는 문 후보(<c>gate_sw</c>·<c>gate_se</c>).</summary>
    public required IReadOnlyList<string> Entry { get; init; }
    /// <summary>보스 방의 들어오는 문(<c>gate_sw</c>).</summary>
    public required string BossEntry { get; init; }
    /// <summary>출구 문 둘(<c>gate_nw</c>·<c>gate_ne</c>) — 순서가 <c>RoomPlan.ExitGates</c> 순서.</summary>
    public required IReadOnlyList<string> Exits { get; init; }
    /// <summary>출구가 하나일 때(액트의 마지막 방 — 계단, v0.8: 다음이 정예·보스 방이어도 출구는 둘)의 문(<c>gate_ne</c>).</summary>
    public required string SingleExit { get; init; }
}

/// <summary>출구 문 수(v0.7, 6.10).</summary>
public sealed record ExitsDef
{
    /// <summary>출구가 둘인 방에서 남은 앞 문을 더해 셋이 될 확률(0.4).</summary>
    public required float ThreeChance { get; init; }
}

/// <summary>넓이 배율(v0.7, 6.10·3.6): <c>clamp(걸을 칸 수 / 기준 맵 칸 수, min, max)</c>.</summary>
public sealed record AreaScaleDef
{
    /// <summary>기준 맵(<c>cathedral_arena</c>).</summary>
    public required DefRef<MapDef> Reference { get; init; }
    public required float Min { get; init; }
    public required float Max { get; init; }
}

/// <summary>
/// <c>rules.sight</c> (설계서 6.10, 기획 10.7·23절). 시야 반경은 여기 없다 — 스테이지 조명의 <c>hero_light.radius</c> 가 곧 시야 반경이다.
/// 시뮬레이션 값 <see cref="Edge"/>·<see cref="ViewInset"/>·<see cref="RefreshTicks"/>·<see cref="RangedMargin"/> 은 쏘기 거리·해시·13.5-15 를 바꾼다(WP4 와 합의).
/// 그림 값 <see cref="MemoryRatio"/>·<see cref="Fade"/>·<see cref="TouchMargin"/> 은 WP5 가 고른다.
/// </summary>
public sealed record SightRulesDef
{
    /// <summary>경계 흐림 폭 px. 그리기 알파·땅 밝기 띠와 "또렷이 보임" 의 R − edge.</summary>
    public required float Edge { get; init; }
    /// <summary>가 본 곳 밝기 = 빛 없는 바닥 밝기 × 이 값(FogRenderer).</summary>
    public required float MemoryRatio { get; init; }
    /// <summary>칸 보임이 바뀔 때 그림이 바뀌는 시간(s, 그림만). F = round(fade × 60) 틱.</summary>
    public required float Fade { get; init; }
    /// <summary>가 본 칸 표시 주기(틱).</summary>
    public required int RefreshTicks { get; init; }
    /// <summary>"또렷이 보임" 의 화면 사각형 안쪽 여백 px.</summary>
    public required float ViewInset { get; init; }
    /// <summary>주인공에 닿은 적을 시야와 상관없이 그리는 거리의 최솟값(몸 반경 합 + 이 값, 적마다 <c>Enemy.TouchReach</c>).</summary>
    public required float TouchMargin { get; init; }
    /// <summary>13.5-15 의 여유(빛 반경 ≥ 원거리 최대 + edge + 이 값).</summary>
    public required float RangedMargin { get; init; }
}

public sealed record EnemyRulesDef
{
    public TelegraphRequiredDef TelegraphRequired { get; init; } = new();
}

/// <summary>
/// 예고가 꼭 필요한 "큰 피해" 기준(기획 12.6, 판정 설계서 6.5). 피해는 기본 피해(정의 값, 배율 전).
/// 범위 즉발·돌진이 기본 피해 ≥ <see cref="MinDamage"/> 면 예고 ≥ <see cref="MinTime"/>.
/// 근접 단일 대상(닿는 거리 ≤ 두 반경 + <see cref="MeleeReachOverContact"/>)은 면제, 단 피해 ≥ <see cref="HeavyMeleeDamage"/> 면
/// 타격 프레임까지 ≥ <see cref="HeavyMeleeWindup"/>.
/// </summary>
public sealed record TelegraphRequiredDef
{
    public float MinDamage { get; init; } = 10f;
    public float MinTime { get; init; } = 0.4f;
    public float MeleeReachOverContact { get; init; } = 50f;
    public float HeavyMeleeDamage { get; init; } = 25f;
    public float HeavyMeleeWindup { get; init; } = 0.35f;
}

/// <summary>
/// <c>rules.hero</c>(v0.6, 6.10) = 모든 직업 공통 규칙 + 고른 직업이 없을 때의 직업. v0.5 의 sprites·radius·start_weapons 는 직업 정의(<see cref="HeroClassDef"/>)로 옮겼다.
/// </summary>
public sealed record HeroRulesDef
{
    /// <summary>고른 직업이 없을 때 쓰는 직업(MVP·<c>--hero</c> 없음·저장 없음 → <c>sorcerer</c>). <c>requires</c> 가 빈 기본 직업이어야 한다(13.5-20).</summary>
    public required DefRef<HeroClassDef> Class { get; init; }
    public float InvulnAfterHit { get; init; } = 0.25f;
    /// <summary>모든 직업의 기본 능력치(기획 4.1 표). 직업 <c>stats</c> 가 키별로 덮어쓴다.</summary>
    public IReadOnlyDictionary<StatKey, float> Stats { get; init; } = new Dictionary<StatKey, float>();
    public CrowdSlowDef? CrowdSlow { get; init; }
    /// <summary>허리띠(v0.8, 6.18 — 기획 v0.8.1 11.4). null 이면 허리띠 없음. 구현 WP3, 수는 <c>Hero.Belt</c>·짐 <c>Belt</c>.</summary>
    public BeltRulesDef? Belt { get; init; }
}

/// <summary>
/// 허리띠(v0.8, 6.10): 체력이 최대 × <see cref="Threshold"/> 아래로 내려가는 순간 하나를 저절로 마신다(전부 회복, <see cref="Cooldown"/> 초에 한 번 — 버튼 없음).
/// 셋 다 필수(기본값을 코드에 두지 않음).
/// </summary>
public sealed record BeltRulesDef
{
    /// <summary>허리띠 칸 수(2).</summary>
    public required int Slots { get; init; }
    /// <summary>저절로 마시는 체력 비율(0.3).</summary>
    public required float Threshold { get; init; }
    /// <summary>두 번 마시는 사이 최소 초(1.0).</summary>
    public required float Cooldown { get; init; }
}

public sealed record CrowdSlowDef
{
    public int Touching { get; init; } = 3;
    public float Mul { get; init; } = 0.6f;
}

/// <summary>경험치 곡선 한 구간: 필요 경험치 = base + per × (L − origin), L ∈ [from, to](to null = 끝 없음).</summary>
public sealed record XpCurveSegment
{
    public int From { get; init; }
    public int? To { get; init; }
    public float Base { get; init; }
    public float Per { get; init; }
    public int? Origin { get; init; }
}

public sealed record LevelUpRulesDef
{
    public int Cards { get; init; } = 3;
    public LevelUpWeightsDef Weights { get; init; } = new();
    /// <summary>판 시작(액트 1)의 열린 무기 칸 수. v0.8: 계단마다 <c>rules.rooms.stairs</c> 만큼 +, 상한 6(<c>RunSlots.OpenAt</c>).</summary>
    public int WeaponSlots { get; init; } = 4;
    /// <summary>판 시작(액트 1)의 열린 패시브 칸 수(위와 같은 규칙).</summary>
    public int PassiveSlots { get; init; } = 4;
    public int Rerolls { get; init; }
    public float SkipHeal { get; init; }
    public float InputLock { get; init; }
    public IReadOnlyList<int> GuaranteeNewWeaponAt { get; init; } = [];
    public IReadOnlyList<FillerCardDef> Fillers { get; init; } = [];
    /// <summary>
    /// <c>단련</c> 채움 카드 ID(v0.8, 6.10 — <see cref="Fillers"/> 안에 있어야 함, 13.5-1). 0장 보상·보물 상자에 올릴 무기 없음·정예 상자 빈 강화가 쓴다(WP3).
    /// </summary>
    public string? Temper { get; init; }
    /// <summary>레벨 표 키·<see cref="StatKey"/> 이름 → 카드 문구 라벨.</summary>
    public IReadOnlyDictionary<string, StatLabelDef> StatLabels { get; init; } = new Dictionary<string, StatLabelDef>();
}

public sealed record LevelUpWeightsDef
{
    public float OwnedWeapon { get; init; }
    public float OwnedPassive { get; init; }
    public float NewWeapon { get; init; }
    public float NewPassive { get; init; }
}

/// <summary>
/// 채움 카드(6.10). v0.8: 금화(<c>gold</c> — <c>filler_gold</c> 금화 주머니)는 없애고 <see cref="Stats"/>(판 끝까지 쌓이는 능력치 — 짐 <c>Mods</c>, <c>단련</c>)를 더했다.
/// </summary>
public sealed record FillerCardDef
{
    public required string Id { get; init; }
    public string Name { get; init; } = "";
    public float Heal { get; init; }
    /// <summary>고를 때마다 판 끝까지 더하는 능력치(v0.8 — <c>단련</c> max_hp +5).</summary>
    public IReadOnlyDictionary<StatKey, float> Stats { get; init; } = new Dictionary<StatKey, float>();
    public IconDef? Icon { get; init; }
}

public enum StatFormat : byte { Abs, Pct }

public sealed record StatLabelDef
{
    public string Label { get; init; } = "";
    public string? Unit { get; init; }
    public StatFormat Format { get; init; } = StatFormat.Abs;
}

/// <summary>선택 창이 닫힌 다음 스테이지 틱에 주인공 반경 radius 안 적을 distance 만큼 밀어낸다(피해 0, 기획 12.6).</summary>
public sealed record LevelUpPushDef
{
    public float Radius { get; init; }
    public float Distance { get; init; }
}

public sealed record RanksDef
{
    public required RankDef Elite { get; init; }
    public required RankDef Unique { get; init; }
    public required PackDef Pack { get; init; }
    /// <summary>생성 때 최종 크기(정의 scale × 등급 배율)의 상한(기획 8.1). 넘으면 자르고 몸 반경도 자른 크기로 센다(6.5).</summary>
    public float MaxScale { get; init; } = 1.6f;
}

/// <summary>정예·우두머리 배율(설계서 6.7·6.10).</summary>
public sealed record RankDef
{
    public float Hp { get; init; } = 1f;
    public float Damage { get; init; } = 1f;
    public float Speed { get; init; } = 1f;
    public float Xp { get; init; } = 1f;
    public float Scale { get; init; } = 1f;
    /// <summary>넉백 저항에 더하는 값(정예).</summary>
    public float? KnockbackResistAdd { get; init; }
    /// <summary>넉백 저항을 이 값으로(우두머리).</summary>
    public float? KnockbackResist { get; init; }
    public int Affixes { get; init; }
    public DefRef<DropTableDef>? DropTable { get; init; }
    public Rgb Glow { get; init; } = Rgb.White;
    public RankPulseDef? Pulse { get; init; }
}

public sealed record RankPulseDef
{
    public float Period { get; init; }
    /// <summary>[최소, 최대] 알파.</summary>
    public float[] Alpha { get; init; } = [];
}

public sealed record PackDef
{
    /// <summary>[최소, 최대]</summary>
    public int[] PackSize { get; init; } = [3, 5];
    public float UniqueChance { get; init; }
    public string Banner { get; init; } = "";
    public float BannerTime { get; init; }
}

public sealed record CapsDef
{
    public int Enemies { get; init; } = 300;
    public int EnemiesAbsolute { get; init; } = 360;
    public int HeroProjectiles { get; init; } = 400;
    public int EnemyProjectiles { get; init; } = 200;
    public int Gems { get; init; } = 300;
}

public sealed record SpawnRulesDef
{
    /// <summary>시야 밖 생성 띠 [최소, 최대] px.</summary>
    public float[] Band { get; init; } = [40f, 200f];
    public int Tries { get; init; } = 12;
    public float EdgeMargin { get; init; }
    /// <summary>[최소, 최대] 무리 크기.</summary>
    public int[] Group { get; init; } = [1, 3];
    public float GroupRadius { get; init; }
    public float AccumulatorHold { get; init; }
    /// <summary>자리 고르기 길 거리 ≤ 직선 × 이 값(v0.6, 4.10 <c>PlaceReach.Detour</c>, 기획 10.1).</summary>
    public float PathDetour { get; init; } = 1.5f;
    /// <summary>방 체류 압박(v0.6.3, 6.12). null 이면 끔 — MVP 데이터는 기획 첫 값(8초·셋 중 하나·150px)을 적는다(WP4).</summary>
    public RoomPressureDef? RoomPressure { get; init; }
}

/// <summary>
/// <c>rules.spawn.room_pressure</c>(v0.6.3, 설계서 6.12 "방 체류 압박", 기획 10.1·열린 질문 30). 주인공이 문 하나인 방(<c>RoomFloor.OneDoor</c>) 바닥에
/// <see cref="After"/> 초 넘게 머물면 그동안 일반 생성 무리 <see cref="EveryNth"/> 번째마다 하나를 그 방 바닥 안, 주인공에서 <see cref="MinDistance"/> px 이상인 걷기 칸에 등장 연출로 낸다.
/// 기본값을 코드에 두지 않는다(값은 이 파일 한 곳).
/// </summary>
public sealed record RoomPressureDef
{
    /// <summary>방 바닥에 머문 시간 문턱(초, 첫 값 8).</summary>
    public required float After { get; init; }
    /// <summary>일반 생성 무리 몇 번째마다 하나를 방 안으로(첫 값 3 = 셋 중 하나).</summary>
    public required int EveryNth { get; init; }
    /// <summary>주인공에서 떨어질 최소 거리(px, 첫 값 150).</summary>
    public required float MinDistance { get; init; }
}
