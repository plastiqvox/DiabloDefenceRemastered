namespace DiabloDefenceRemastered.Core.Sim.Contracts;

public enum CardKind : byte { NewWeapon, WeaponUpgrade, NewPassive, PassiveUpgrade, Filler }

/// <summary>
/// 레벨업 카드 한 장 (기획 4.4·12.2). Lines = 이번 레벨에서 바뀌는 수치 문구(<c>"피해 14 → 18"</c>),
/// <c>stat_labels</c> 로 만든다(설계서 6.10).
/// </summary>
public sealed record LevelUpCard(CardKind Kind, string Id, string Name, IconDef? Icon, int FromLevel, int ToLevel, IReadOnlyList<string> Lines);

/// <summary>선택 창의 출처(v0.7, 6.18). 보상 창은 레벨업 오버레이를 재사용한다 — 마법서 = 무기 카드만, 성소 = 축복 카드만.</summary>
public enum OfferSource : byte { LevelUp, Tome, Shrine }

/// <summary>지금 보여 줄 선택지. 3장 미만일 수 있다. <paramref name="Source"/>(v0.7, 끝 인자 — 기존 호출은 그대로 레벨업)로 자동 조작 <c>PickCard</c> 가 규칙을 고른다.</summary>
public sealed record LevelUpOffer(int Level, IReadOnlyList<LevelUpCard> Cards, int RerollsLeft, OfferSource Source = OfferSource.LevelUp);

public enum ChestItemKind : byte { Evolution, WeaponUpgrade, PassiveUpgrade, Gold }

public sealed record ChestItem(ChestItemKind Kind, string Id, string Name, IconDef? Icon, int Level, int Gold);

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

    /// <summary>대기 중인 보상 창 수(마법서·성소를 주우면 +1).</summary>
    int PendingRewards { get; }

    /// <summary>대기 중인 마법서·성소 창(<see cref="LevelUpOffer.Source"/> = Tome·Shrine). 카드 = 무기만 / 축복만, 늘 1장 이상(0장이면 줍는 순간 <c>skip</c> 으로 처리).</summary>
    LevelUpOffer RewardOffer();
    void ChooseReward(int index);
    /// <summary>재추첨 수는 레벨업과 나눠 쓴다(기획 4.4).</summary>
    void RerollReward();
    /// <summary>보상의 <c>skip</c>(금화·회복).</summary>
    void SkipReward();

    /// <summary>
    /// 문 열기 거르기(3.6): 마법서 = 줄 무기 카드가 있음, 성소 = 줄 축복이 있음, 물약 = 체력 &lt; 최대 × <c>skip_when_hp_at_least</c>, 보물 상자 = 늘 참.
    /// 문을 여는 순간 한 번만 부른다.
    /// </summary>
    bool RewardAvailable(RewardDef reward);
}

/// <summary>
/// 한 판 기록(결과 화면·저장). DamageResolve·DeathRewards·Pickups 가 채운다. v0.7: 판 수명 — <c>RunState.Stats</c> 하나를 방 월드마다 <c>World.RunStats</c> 가 가리킨다.
/// <b>지우는 곳이 없다</b>(v0.7.1): <c>RunSession</c> 이 판 시작 때 새로 만들고, 월드 설치·주인공 만들기·WP3 조각은 지우지 않는다(공개 <c>Reset()</c> 을 없앴다 — 방마다 조용히 0 이 되는 길).
/// partial 이다: WP3 은 자기 폴더 조각(namespace <c>…Core.Sim.Contracts</c>)에 집계 필드를 더한다. <see cref="DamageByWeapon"/> 은 칸 번호로 센다 — <c>IHeroCarry.Restore</c> 는 같은 칸에 놓는다.
/// </summary>
public sealed partial class RunStats
{
    public const int WeaponSlotCount = 4;

    public int Kills { get; set; }
    public int EliteKills { get; set; }
    public int Gold { get; set; }
    public int XpCollected { get; set; }
    public float DamageTaken { get; set; }
    /// <summary>무기 칸별 준 피해 합계(결과 화면).</summary>
    public float[] DamageByWeapon { get; } = new float[WeaponSlotCount];
}
