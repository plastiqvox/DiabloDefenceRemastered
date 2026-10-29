namespace DiabloDefenceRemastered.Core.Sim.Contracts;

public enum CardKind : byte { NewWeapon, WeaponUpgrade, NewPassive, PassiveUpgrade, Filler }

/// <summary>
/// 레벨업 카드 한 장 (기획 4.4·12.2). Lines = 이번 레벨에서 바뀌는 수치 문구(<c>"피해 14 → 18"</c>),
/// <c>stat_labels</c> 로 만든다(설계서 6.10).
/// </summary>
public sealed record LevelUpCard(CardKind Kind, string Id, string Name, IconDef? Icon, int FromLevel, int ToLevel, IReadOnlyList<string> Lines);

/// <summary>지금 보여 줄 선택지. 3장 미만일 수 있다.</summary>
public sealed record LevelUpOffer(int Level, IReadOnlyList<LevelUpCard> Cards, int RerollsLeft);

public enum ChestItemKind : byte { Evolution, WeaponUpgrade, PassiveUpgrade, Gold }

public sealed record ChestItem(ChestItemKind Kind, string Id, string Name, IconDef? Icon, int Level, int Gold);

/// <summary>상자 하나를 연 결과(중간 보스 상자는 3번).</summary>
public sealed record ChestReward(IReadOnlyList<ChestItem> Items);

/// <summary>
/// 성장 (WP1 계약, WP3 구현). 선택 창은 시뮬레이션이 멈춘 사이에 오버레이(WP6)가 직접 부른다(결정성 영향 없음).
/// EndTick 은 <see cref="PendingLevelUps"/>·<see cref="PendingChests"/> 를 보고 <c>World.AwaitingChoice</c> 를 켠다.
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
}

/// <summary>
/// 한 판 기록(결과 화면·저장). DamageResolve·DeathRewards·Pickups 가 채운다.
/// partial 이다: WP3 은 자기 폴더 조각(namespace <c>…Core.Sim.Contracts</c>)에 집계 필드를 더하고 <see cref="ResetCombat"/> 에서 지운다.
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

    public void Reset()
    {
        Kills = EliteKills = Gold = XpCollected = 0;
        DamageTaken = 0;
        Array.Clear(DamageByWeapon);
        ResetCombat();
    }

    /// <summary>WP3 조각의 집계 지우기.</summary>
    partial void ResetCombat();
}
