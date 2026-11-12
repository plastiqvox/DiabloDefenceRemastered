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
    /// <summary>
    /// 장비 규칙(v0.9, 6.19 — WP3 <c>rules/equipment.json</c>). 마법 확률·접사 둘·값 굴림·방어 상한·무게 이동 표·하한·다시 굴리기.
    /// 기본값을 코드에 두지 않는다 — 장비 정의(<c>items</c>)나 장비 상인이 있는데 없으면 로딩 오류(WP7 <c>DataRules</c>). 없으면 장비를 쓰는 코드(<c>ItemRoller</c>·<c>ShopStock</c>)가 예외.
    /// </summary>
    public EquipmentRulesDef? Equipment { get; init; }
}

/// <summary>
/// <c>rules.rooms</c> (v0.7 → v0.8 WP1f, 설계서 6.10, 기획 2.5·10.8·11.4·15절 첫 값). 쓰는 곳: <see cref="Grace"/>·<see cref="CleanupLimit"/>·<see cref="EliteSpawnCap"/> = <c>RoomFlow</c>·감독,
/// <see cref="Gates"/>·<see cref="Exits"/> = <c>RunPlanner</c>, <see cref="AreaScale"/>·<see cref="Stairs"/> = <c>RunSession</c>, <see cref="RewardSpot"/>·<see cref="FallbackReward"/> = <c>RoomFlow</c>,
/// <see cref="Transition"/>·<see cref="TransitionFloor"/> = <c>PlayScene</c>(앱 틱), <see cref="AutoplayDoorSkip"/> = <c>AutoplayPolicy</c>.
/// v0.9: <see cref="Rewards"/>(문 보상 뽑기 — 회복 문 빠짐·보장·저체력 가중, 상인 조건·보장·배율) = <c>RoomFlow</c> 문 열기(<c>RewardDraw</c>).
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
    /// <summary>문 보상 뽑기의 값(v0.9, 6.19 "회복 문"·기획 11.4 "뽑기"·11.7 — <c>RewardDraw</c>).</summary>
    public required RewardRulesDef Rewards { get; init; }
}

/// <summary>
/// <c>rules.rooms.rewards</c>(v0.9 → v0.9.1, 6.19, 기획 11.4·11.5·11.7 첫 값). 체력 비율은 문이 열리는 틱의 <c>Hero.Hp / Hero.MaxHp</c>.
/// 모두 필수(기본값을 코드에 두지 않음). 바꾸면 같은 씨앗의 문 보상이 바뀐다(WP4 — Scenario 를 다시 돌림).
/// </summary>
public sealed record RewardRulesDef
{
    /// <summary>체력 비율이 이 값 이상이면 회복 문이 빠진다(0.75).</summary>
    public required float HealSkipAtLeast { get; init; }
    /// <summary>체력 비율이 이 값 미만이면 첫 출구 문을 회복 문으로 보장(0.40).</summary>
    public required float HealGuaranteeBelow { get; init; }
    /// <summary>체력 비율이 이 값 미만이면 회복 문 가중을 <see cref="HealLowHpWeight"/> 로(0.50).</summary>
    public required float HealLowHp { get; init; }
    /// <summary>저체력 회복 문 가중(30 — 보통은 액트 가중표 값 15).</summary>
    public required float HealLowHpWeight { get; init; }
    /// <summary>회복 문으로 들어온 방의 첫 틱에 채우는 양 = 최대 체력 × 이 값(0.5, 최대를 넘지 않음). 2026-11-12 사용자 결정 — 체력 전부(v0.9)에서 일부로.</summary>
    public required float HealAmount { get; init; }
    /// <summary>액트 1 에서 상인 문이 처음 나오는 다음 방 번호(3 — 1·2번 방을 깨는 동안은 금화가 모자람, 기획 11.7).</summary>
    public required int MerchantFirstRoom { get; init; }
    /// <summary>한 액트에 상인 방 상한(2 — 상인 셋을 합쳐 셈).</summary>
    public required int MerchantPerAct { get; init; }
    /// <summary>액트별 상인 보장 방 번호([4, 5] — 액트 1 은 4번 방, 액트 2 는 5번 방으로 가는 문). 액트 수보다 짧으면 그 뒤 액트는 보장 없음.</summary>
    public required IReadOnlyList<int> MerchantGuaranteeRoom { get; init; }
    /// <summary>이 판에 한 상인을 만났으면 다른 상인 가중 × 이 값(1.5).</summary>
    public required float MerchantMetOther { get; init; }
    /// <summary>물약 칸이 빈 채 체력 비율 &lt; <see cref="HealerBoostBelow"/> 면 물약 상인(페핀) 가중 × 이 값(1.5).</summary>
    public required float HealerBoost { get; init; }
    public required float HealerBoostBelow { get; init; }
}

/// <summary>
/// <c>rules.equipment</c>(v0.9 → v0.9.1, 6.19, 기획 11.6·11.7 첫 값 — WP3 <c>rules/equipment.json</c>). 모두 필수.
/// </summary>
public sealed record EquipmentRulesDef
{
    /// <summary>액트별 마법 등급 확률([0.35, 0.60]). 액트 번호가 길이를 넘으면 마지막 값.</summary>
    public required IReadOnlyList<float> MagicChance { get; init; }
    /// <summary>마법 등급의 접사 둘(접두사 + 접미사) 확률(0.25 — v0.9.1 V7).</summary>
    public required float TwoAffixChance { get; init; }
    /// <summary>접사 값 배율 = [0] + [1] × 굴림 위치([0.75, 0.5] → 0.75~1.25). 길이 2.</summary>
    public required IReadOnlyList<float> PriceRoll { get; init; }
    /// <summary>값 반올림 단위(5).</summary>
    public required int PriceRound { get; init; }
    /// <summary>방어 상한(15 — v0.9.1, 기획 v0.9.2 열린 질문 52. 모든 출처를 합친 뒤 자름).</summary>
    public required float ArmorCap { get; init; }
    /// <summary>끼운 방어구마다 <c>move_pct</c> 에 더하는 값: 칸 종류 → 무게 → 값(갑옷 중 −0.05·판금 −0.10, 투구·방패 중 −0.02·판금 −0.04). 없는 칸·무게는 0.</summary>
    public required IReadOnlyDictionary<EquipKind, IReadOnlyDictionary<ArmorWeight, float>> WeightMove { get; init; }
    /// <summary><c>move_pct</c> 하한(−0.15 — 모든 출처를 합친 뒤 자름).</summary>
    public required float WeightMoveFloor { get; init; }
    /// <summary>상인 다시 굴리기 값 = <see cref="RerollPrice"/>(30) + <see cref="RerollStep"/>(30) × 이 방에서 누른 수.</summary>
    public required int RerollPrice { get; init; }
    public required int RerollStep { get; init; }

    /// <summary>액트 <paramref name="act"/>(1부터)의 마법 확률(길이를 넘으면 마지막 값, 비었으면 0).</summary>
    public float MagicChanceAt(int act) => MagicChance.Count == 0 ? 0f : MagicChance[Math.Clamp(act - 1, 0, MagicChance.Count - 1)];
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
    /// <summary>보스 탄막 공통 값(v0.9.4, 6.20 ①④⑨ — 계약, 기획 38.1).</summary>
    public BarrageRulesDef Barrage { get; init; } = new();
}

/// <summary>
/// <c>rules.enemies.barrage</c>(기획 38.1 공정 장치): <see cref="SpawnClear"/> = 주인공 발에서 이 거리 안에는 탄을 만들지 않음(그 탄만 빠짐),
/// <see cref="TelegraphMin"/> = <c>barrage</c> 가 든 패턴의 예고·<c>teleport.appear</c> 최소(로딩 검사), <see cref="ClearFxMax"/> = 페이즈 탄 지우기의 불꽃 수 상한.
/// </summary>
public sealed record BarrageRulesDef
{
    public float SpawnClear { get; init; } = 48f;
    public float TelegraphMin { get; init; } = 0.4f;
    public int ClearFxMax { get; init; } = 24;
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
    /// <summary>
    /// 물약 칸(v0.9.1 V9, 6.19 — v0.8 의 <c>belt</c>(저절로 마심)를 바꿈, 옛 키는 모르는 속성 오류). null 이면 물약 칸 없음(체력 물약을 줍지 않음).
    /// 구현 WP3, 수는 <c>Hero.Potions</c>·짐 <c>Potions</c>.
    /// </summary>
    public PotionRulesDef? Potions { get; init; }
}

/// <summary>
/// 물약 칸(v0.9.1 V9, 6.19 — 기획 11.5): <c>Q</c>(<c>CommandButtons.Drink</c>)로 하나를 마시면 최대 체력 × <see cref="HealPct"/> 를 <see cref="Durations"/>[빠른 물약 단계] 초에 걸쳐 고르게.
/// 칸 = <see cref="Slots"/> + 물약 주머니 단계(상한 <see cref="SlotsMax"/>). 모두 필수. v0.8 의 <c>slots</c>·<c>threshold</c>·<c>cooldown</c>(저절로 마심)은 없앴다.
/// </summary>
public sealed record PotionRulesDef
{
    /// <summary>기본 칸 수(2).</summary>
    public required int Slots { get; init; }
    /// <summary>칸 상한(5 — 페핀 물약 주머니). <see cref="Slots"/> 이상(로딩 검사).</summary>
    public required int SlotsMax { get; init; }
    /// <summary>한 병 회복량 = 최대 체력 × 이 값(0.40).</summary>
    public required float HealPct { get; init; }
    /// <summary>빠른 물약 단계별 회복 시간(초, [3.0, 2.0, 1.2]). 길이 ≥ 1(로딩 검사).</summary>
    public required IReadOnlyList<float> Durations { get; init; }
    /// <summary>자동 조작이 마시는 체력 비율(0.35 — WP4).</summary>
    public required float BotThreshold { get; init; }

    /// <summary>물약 주머니 단계 <paramref name="slotsLevel"/> 의 칸 수(<see cref="SlotsMax"/> 에서 자름).</summary>
    public int Capacity(int slotsLevel) => Math.Min(SlotsMax, Slots + Math.Max(0, slotsLevel));

    /// <summary>빠른 물약 단계 <paramref name="speedLevel"/> 의 회복 시간(초 — 범위 밖이면 끝 값).</summary>
    public float Duration(int speedLevel) => Durations.Count == 0 ? 0f : Durations[Math.Clamp(speedLevel, 0, Durations.Count - 1)];
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
