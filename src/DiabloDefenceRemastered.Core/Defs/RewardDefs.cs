namespace DiabloDefenceRemastered.Core.Defs;

/// <summary>문 보상 종류(v0.7, 설계서 6.18). JSON <c>weapon_cards</c> / <c>blessing_cards</c> / <c>level_up</c> / <c>full_heal</c>.</summary>
public enum RewardKind : byte { WeaponCards, BlessingCards, LevelUp, FullHeal }

/// <summary>
/// 문 보상 <c>rewards[]</c> (v0.7, 사용자 결정 R — 설계서 6.18, 기획 11.4·13.1). 파일 <c>Data/rewards.json</c> 은 WP3 소유.
/// 정예 상자 보상(기획 <c>reward_elite</c>)은 정의를 두지 않는다 — 정예·보스 방은 <c>RoomState.Reward = null</c> 이고 드롭 상자를 <c>RoomFlow</c> 가 <c>GatesExit</c> 로 묶는다.
/// 받는 길(WP3): 물체를 주우면 <see cref="Kind"/> 마다 — 카드 둘은 보상 대기 +1, <see cref="RewardKind.LevelUp"/> 은 경험치 <c>XpToNext</c> + 금화 <see cref="Gold"/> × <c>gold_scale</c>,
/// <see cref="RewardKind.FullHeal"/> 은 체력 전부. 줍는 순간 줄 카드가 0장이면 <see cref="Skip"/> 으로 바로 처리한다(v0.7.1).
/// </summary>
public sealed record RewardDef : IDef
{
    public required string Id { get; init; }
    public string Name { get; init; } = "";
    public required RewardKind Kind { get; init; }
    /// <summary>방 안 물체(<c>kind: reward</c> 획득물).</summary>
    public required DefRef<PickupDef> Object { get; init; }
    /// <summary>문 위 아이콘(48px)·HUD 방 줄.</summary>
    public IconDef? Icon { get; init; }
    /// <summary><c>weapon_cards</c>: 후보 가중(<c>owned_weapon</c>·<c>new_weapon</c>).</summary>
    public RewardCardsDef? Cards { get; init; }
    /// <summary>건너뛰기 = 금화(<c>gold</c>) 또는 회복(<c>heal</c>).</summary>
    public RewardSkipDef? Skip { get; init; }
    /// <summary><c>level_up</c>: [최소, 최대] × 액트 <c>gold_scale</c>.</summary>
    public int[]? Gold { get; init; }
    /// <summary><c>full_heal</c>: 체력이 최대 × 이 비율 이상이면 문 열기 후보에서 빠진다(<c>IProgression.RewardAvailable</c>).</summary>
    public float? SkipWhenHpAtLeast { get; init; }
    public IReadOnlyList<ContentBundle> Requires { get; init; } = [];
}

/// <summary>마법서 카드 후보 가중(6.18).</summary>
public sealed record RewardCardsDef
{
    public float OwnedWeapon { get; init; }
    public float NewWeapon { get; init; }
}

/// <summary>보상 창 건너뛰기(6.18). 금화 또는 회복 비율.</summary>
public sealed record RewardSkipDef
{
    public int Gold { get; init; }
    public float Heal { get; init; }
}
