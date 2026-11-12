namespace DiabloDefenceRemastered.Core.Sim.Contracts;

public enum CardKind : byte { NewWeapon, WeaponUpgrade, NewPassive, PassiveUpgrade, Filler }

/// <summary>
/// 레벨업 카드 한 장 (기획 4.4·12.2). Lines = 이번 레벨에서 바뀌는 수치 문구(<c>"피해 14 → 18"</c>),
/// <c>stat_labels</c> 로 만든다(설계서 6.10).
/// </summary>
public sealed record LevelUpCard(CardKind Kind, string Id, string Name, IconDef? Icon, int FromLevel, int ToLevel, IReadOnlyList<string> Lines);

/// <summary>
/// 선택 창의 출처(v0.7 → v0.8, 6.18). 보상 창은 레벨업 오버레이를 재사용한다 — 마법서 = 무기 카드만,
/// <see cref="Shrine"/>(v0.8) = 매혹의 성소의 무기 고르기 창(<c>pick_weapon</c> — 축복 창이 아님, 카드 = <c>pick_max_level</c> 이하 진화 아닌 무기).
/// </summary>
public enum OfferSource : byte { LevelUp, Tome, Shrine }

/// <summary>
/// 지금 보여 줄 선택지. 3장 미만일 수 있다. <paramref name="Source"/>(v0.7, 끝 인자 — 기존 호출은 그대로 레벨업)로 자동 조작 <c>PickCard</c> 가 규칙을 고른다.
/// <paramref name="Reward"/>(v0.9 WP1g — WP6 요청 3): 보상 창(<c>Tome</c>·<c>Shrine</c>)이면 그 보상 정의(건너뛰기 회복 문구·성소 카드 둘째 줄), 레벨업 창은 null.
/// </summary>
public sealed record LevelUpOffer(int Level, IReadOnlyList<LevelUpCard> Cards, int RerollsLeft, OfferSource Source = OfferSource.LevelUp, RewardDef? Reward = null);

/// <summary>
/// 상자 한 칸의 종류. v0.8: <see cref="Temper"/> = 정예 상자의 빈 강화(올릴 것이 없을 때 <c>단련</c> — <c>rules.level_up.temper</c>, 기획 v0.8.1 11.1). v0.7 의 <c>Gold</c> 를 바꿨다.
/// </summary>
public enum ChestItemKind : byte { Evolution, WeaponUpgrade, PassiveUpgrade, Temper }

/// <summary>상자 한 칸(v0.9 WP1g: v0.7 의 <c>Gold</c> 인자를 없앴다 — v0.8 에 금화 대체가 <c>단련</c> 으로 바뀌어 늘 0 이었다, 13.22 "계약 줄이기는 WP1g").</summary>
public sealed record ChestItem(ChestItemKind Kind, string Id, string Name, IconDef? Icon, int Level);

/// <summary>상자 하나를 연 결과(중간 보스 상자는 3번).</summary>
public sealed record ChestReward(IReadOnlyList<ChestItem> Items);

/// <summary>
/// 성장 (WP1 계약, WP3 구현). 선택 창은 시뮬레이션이 멈춘 사이에 오버레이(WP6)가 직접 부른다(결정성 영향 없음).
/// EndTick 은 <see cref="PendingLevelUps"/>·<see cref="PendingChests"/>·<see cref="PendingRewards"/>(v0.7) 를 이 순서로 보고 <c>World.AwaitingChoice</c> 를 켠다(레벨업 → 상자 → 보상, 6.18).
/// 고른 뒤 대기가 남지 않으면 구현이 <c>World.AwaitingChoice</c> 를 None 으로 돌린다.
/// </summary>
public interface IProgression
{
    int Level { get; }
    int Xp { get; }
    /// <summary>이번 레벨에서 다음 레벨까지 필요한 경험치.</summary>
    int XpToNext { get; }

    int PendingLevelUps { get; }
    int PendingChests { get; }

    /// <summary>지금 대기 중인 레벨업의 선택지(처음 부르면 만든다).</summary>
    LevelUpOffer Offer();
    void Choose(int index);
    void Reroll();
    void Skip();

    /// <summary>대기 중인 상자 하나를 연다.</summary>
    ChestReward OpenChest();

    // ── 문 보상(v0.7, 6.18 — 계약 WP1e, 구현 WP3) ──────────────────────────────

    /// <summary>대기 중인 보상 창 수(마법서·매혹의 성소를 주우면 +1).</summary>
    int PendingRewards { get; }

    /// <summary>
    /// 대기 중인 마법서·매혹의 성소 창(<see cref="LevelUpOffer.Source"/> = Tome·Shrine). 카드 = 무기만, 늘 1장 이상
    /// (v0.8: 줍는 순간 0장이면 대기를 만들지 않고 <c>단련</c> 으로 바로 처리 — 6.18 "0장 보상").
    /// </summary>
    LevelUpOffer RewardOffer();
    void ChooseReward(int index);
    /// <summary>재추첨 수는 레벨업과 나눠 쓴다(기획 4.4).</summary>
    void RerollReward();
    /// <summary>보상의 <c>skip</c>(v0.8: 회복만 — 금화 없앰).</summary>
    void SkipReward();

    /// <summary>
    /// 문 열기 거르기(3.6, v0.8 — 6.18 "거르기"): 마법서 = 줄 무기 카드 ≥ 1, 물약 = 허리띠가 차지 않음,
    /// 성소 = 짐 <c>Shrines</c> 에 없음 + (<c>random_weapon</c> 이면 진화 아닌 무기 ≥ 2, <c>pick_weapon</c> 이면 <c>pick_max_level</c> 이하 진화 아닌 무기 ≥ 1), 보물 상자 = 늘 참.
    /// 문을 여는 순간 한 번만 부른다.
    /// </summary>
    bool RewardAvailable(RewardDef reward);

    /// <summary>
    /// 빌드에 남은 칸 레벨(v0.8 — 로그 <c>ROOM picks_left</c>, 9절): 열린 칸마다 (최대 − 지금), 빈 칸은 최대. 0 이면 만렙.
    /// </summary>
    int PicksLeft { get; }
}

/// <summary>
/// 상인 창(v0.9, 설계서 6.19 "상인 방 상태와 구매 명령" — 계약 WP1g, 구현 WP3). 상인 창은 <see cref="LevelUpOffer"/> 가 아니라 <see cref="ShopState"/> 를 그린다(화면 모양만 같다 — 기획 12.8).
/// 창이 열려 있는 동안 판 명령 <c>Choice1~3</c> = <see cref="Buy"/>(또는 <see cref="ShopState.RingPending"/> 이면 <see cref="ReplaceRing"/>), <c>Reroll</c> = <see cref="Reroll"/>, <c>Skip</c> = <see cref="Leave"/> —
/// 재생·자동 조작·사람이 같은 길이다. 선택 입력 잠금은 레벨업 창과 같은 자리(<c>rules.level_up.input_lock</c>).
/// </summary>
public interface IShop
{
    /// <summary>이 방의 상인 상태(<c>World.Room.Shop</c>). 상인 방이 아니면 null.</summary>
    ShopState? Shop { get; }
    /// <summary>카드 하나를 산다(<c>지갑 ≥ 값</c> 일 때만 — 모자라면 아무 일 없음 + <c>CueKind.ShopDenied</c>). 장비는 그 칸에 끼운다(반지 둘이 찼으면 <see cref="ShopState.RingPending"/>).</summary>
    void Buy(int card);
    /// <summary>다시 굴리기(<c>reroll_price + reroll_step × Rerolls</c> — 산 카드는 자리를 지킴, <c>ShopStock.Reroll</c>). 페핀(<c>rerollable: false</c>)은 아무 일 없음.</summary>
    void Reroll();
    /// <summary>떠나기 = <c>Open = false; Visited = true</c>(보상 받음 → 문이 열림, 3.6 "집어야 문이 열린다"). 문 단계에 다시 닿으면 다시 연다.</summary>
    void Leave();
    /// <summary>반지 둘이 찬 채 반지를 고른 카드(<see cref="ShopState.RingPending"/>)로 <paramref name="ring"/>(0 = 반지 1, 1 = 반지 2)을 바꾼다.</summary>
    void ReplaceRing(int ring);
}

/// <summary>
/// 한 판 기록(결과 화면·저장). DamageResolve·DeathRewards·Pickups 가 채운다. v0.7: 판 수명 — <c>RunState.Stats</c> 하나를 방 월드마다 <c>World.RunStats</c> 가 가리킨다.
/// <b>지우는 곳이 없다</b>(v0.7.1): <c>RunSession</c> 이 판 시작 때 새로 만들고, 월드 설치·주인공 만들기·WP3 조각은 지우지 않는다(공개 <c>Reset()</c> 을 없앴다 — 방마다 조용히 0 이 되는 길).
/// partial 이다: WP3 은 자기 폴더 조각(namespace <c>…Core.Sim.Contracts</c>)에 집계 멤버를 더한다. <see cref="DamageByWeapon"/> 은 칸 번호로 센다 — <c>IHeroCarry.Restore</c> 는 같은 칸에 놓는다.
/// <b>저장 멤버 규칙</b>(v0.8.1, 3.6 "계단 저장" — 계단 저장 <c>RunSnapshot</c> 이 STJ 로 그대로 왕복한다): 상태는 <b>공개 읽기·쓰기 멤버</b>(공개 필드 또는 공개 get·set 속성)이고
/// 형은 값 형식·그 배열뿐이다. get 만 있는 속성은 STJ 가 읽을 때 건너뛰어 조용히 0 이 되므로 금지 — 반사 왕복 시험·모양 지문 시험이 지킨다(멤버를 더하면 <c>RunSnapshot.CurrentFormat</c> 과 기대 값을 함께 올린다).
/// </summary>
public sealed partial class RunStats
{
    public int Kills { get; set; }
    public int EliteKills { get; set; }
    /// <summary>얻은 금화(결과 화면·누적 — 늘기만 한다. 쓴 금화도 여기 센다, 기획 11.7). 판 안 지갑은 <c>Hero.Gold</c>·짐 <c>HeroCarry.Gold</c>.</summary>
    public int Gold { get; set; }
    /// <summary>상인에게 쓴 금화(v0.9 — 결과 화면 <c>쓴 금화</c>·로그 <c>ROOM gold_spent</c>).</summary>
    public int GoldSpent { get; set; }
    /// <summary>마신 물약 수(v0.9.1 — 로그 <c>ROOM drunk</c> 의 방 차이, WP3 <c>Drink</c> 가 센다).</summary>
    public int PotionsDrunk { get; set; }
    /// <summary>순간이동을 쓴 수(보스 묶음 B — 칸을 쓴 누름, 못 함은 세지 않음. 로그 <c>ROOM blinks</c> 의 방 차이·보스 방 시나리오 기록, WP3 <c>HeroBlink</c> 가 센다).</summary>
    public int Blinks { get; set; }
    public int XpCollected { get; set; }
    public float DamageTaken { get; set; }
    /// <summary>무기 칸별 준 피해 합계(결과 화면). 길이 = <see cref="WeaponSlots.SlotCount"/>(6, v0.8.1 — v0.8 은 4 로 남아 5번째 칸이 범위 밖이었다).</summary>
    public float[] DamageByWeapon { get; set; } = new float[WeaponSlots.SlotCount];
}
