namespace DiabloDefenceRemastered.Core.Defs;

/// <summary>
/// 문 보상 종류(v0.8, 설계서 6.18 — WP1f). JSON <c>weapon_cards</c> / <c>weapon_bundle</c> / <c>belt_potions</c> / <c>shrine</c>.
/// v0.7 의 <c>blessing_cards</c>·<c>level_up</c>·<c>full_heal</c> 은 없앴다(기획 v0.8.1 11.4 — 레벨업 창과 같거나 위험이 지난 뒤에 오던 것).
/// </summary>
public enum RewardKind : byte { WeaponCards, WeaponBundle, BeltPotions, Shrine }

/// <summary>
/// 문 보상 <c>rewards[]</c> (v0.7 → v0.8 WP1f, 설계서 6.18, 기획 11.4·13.1). 파일 <c>Data/rewards.json</c> 은 WP3 소유.
/// 정예 상자 보상(기획 <c>reward_elite</c>)은 정의를 두지 않는다 — 중간 보스·보스가 떨군 정예 상자를 <c>RoomFlow</c> 가 <c>GatesExit</c> 로 묶는다.
/// v0.8: 정예·보스 방에도 덤 보상(그 방으로 들어온 문의 보상 — <c>RoomState.Reward</c>)이 있다.
/// 받는 길(WP3): 물체를 주우면 <see cref="Kind"/> 마다 — 마법서 = 보상 대기 +1, 무기 강화 꾸러미 = 무작위 무기 +<c>weapon_levels</c>·허리띠·금화,
/// 허리띠 물약 = 허리띠에 넣음(넘치면 마심), 성소 = 무기 레벨·짐 <c>Mods</c>·<c>Shrines</c>(<c>pick_weapon</c> 이면 창). 줄 카드가 0장이면 <c>단련</c>(<c>rules.level_up.temper</c>)으로 바로 처리한다.
/// 종류마다 필요한 칸이 없거나 다른 종류의 칸이 있으면 로딩 오류(13.5-23 ④, WP7).
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
    /// <summary>창의 건너뛰기 = 회복(<c>heal</c>, 체력 값). v0.8: <c>gold</c> 없앰.</summary>
    public RewardSkipDef? Skip { get; init; }
    /// <summary><c>weapon_bundle</c>: 무작위 무기 레벨·허리띠 물약·금화(고르지 않음).</summary>
    public RewardBundleDef? Bundle { get; init; }
    /// <summary><c>belt_potions</c>: 허리띠에 넣는 수(차 있으면 그 자리에서 마심).</summary>
    public int BeltPotions { get; init; }
    /// <summary><c>shrine</c>: 원작 성소 효과(대가 있음, 한 판에 한 번 — 짐 <c>Shrines</c>).</summary>
    public ShrineDef? Shrine { get; init; }
    public IReadOnlyList<ContentBundle> Requires { get; init; } = [];
}

/// <summary>마법서 카드 후보 가중(6.18).</summary>
public sealed record RewardCardsDef
{
    public float OwnedWeapon { get; init; }
    public float NewWeapon { get; init; }
}

/// <summary>보상 창 건너뛰기(6.18) — 회복(체력 값). v0.8: 금화(<c>gold</c>)는 없앴다.</summary>
public sealed record RewardSkipDef
{
    public float Heal { get; init; }
}

/// <summary>
/// 보물 상자 <c>bundle</c>(v0.8, 6.18): 진화 아니고 만렙 아닌 가진 무기 중 무작위 하나 +<see cref="WeaponLevels"/>(없으면 <c>단련</c>) + 허리띠 <see cref="BeltPotions"/>
/// + 금화 <see cref="Gold"/> [최소, 최대] × 액트 <c>gold_scale</c>.
/// </summary>
public sealed record RewardBundleDef
{
    public int WeaponLevels { get; init; } = 1;
    public int BeltPotions { get; init; }
    /// <summary>[최소, 최대] × 액트 <c>gold_scale</c>. null 이면 금화 없음.</summary>
    public int[]? Gold { get; init; }
}

/// <summary>
/// 성소 효과(v0.8, 6.18 — 기획 v0.8.1 11.4 원작 성소). 칸 이름은 기획 13.1 과 같다.
/// </summary>
public sealed record ShrineDef
{
    /// <summary>진화 아닌 가진 무기 모두 +n(만렙은 안 오름).</summary>
    public int AllWeapons { get; init; }
    /// <summary>0 이 아니면 그중 무작위 하나는 <see cref="AllWeapons"/> 대신 이 값(Lv1 무기는 고르지 않음, 방 난수 Drop).</summary>
    public int RandomWeapon { get; init; }
    /// <summary>0 이 아니면 고른 무기 하나 +n — 창(<c>LevelUpOffer.Source = Shrine</c>), 후보 = <see cref="PickMaxLevel"/> 이하 진화 아닌 무기.</summary>
    public int PickWeapon { get; init; }
    public int PickMaxLevel { get; init; }
    /// <summary>판 끝까지 가는 능력치 더하기(짐 <c>Mods</c>).</summary>
    public IReadOnlyDictionary<StatKey, float> Stats { get; init; } = new Dictionary<StatKey, float>();
}
