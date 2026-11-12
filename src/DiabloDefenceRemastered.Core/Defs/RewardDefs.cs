namespace DiabloDefenceRemastered.Core.Defs;

/// <summary>
/// 문 보상 종류(v0.8 → v0.9 WP1g, 설계서 6.18·6.19). JSON <c>weapon_cards</c> / <c>weapon_bundle</c> / <c>heal_next_room</c> / <c>shrine</c> / <c>merchant</c>.
/// v0.7 의 <c>blessing_cards</c>·<c>level_up</c>·<c>full_heal</c> 은 없앴다(기획 v0.8.1 11.4 — 레벨업 창과 같거나 위험이 지난 뒤에 오던 것).
/// v0.9: v0.8 의 <c>belt_potions</c>(허리띠 물약)를 없애고 <see cref="HealNextRoom"/>(회복 문 — 그 문을 지나 다음 방에 들어오는 첫 틱에 회복, 사용자 결정 V1 — 양은 2026-11-12 사용자 결정으로 체력 전부에서 최대 체력 × <c>rules.rooms.rewards.heal_amount</c>(0.5))·
/// <see cref="Merchant"/>(상인 방 — 정리 뒤 보상 자리에 상인이 선다, V3·V11)를 더했다.
/// </summary>
public enum RewardKind : byte { WeaponCards, WeaponBundle, HealNextRoom, Shrine, Merchant }

/// <summary>
/// 문 보상 <c>rewards[]</c> (v0.7 → v0.8 WP1f → v0.9 WP1g, 설계서 6.18·6.19, 기획 11.4·13.1). 파일 <c>Data/rewards.json</c> 은 WP3 소유.
/// 정예 상자 보상(기획 <c>reward_elite</c>)은 정의를 두지 않는다 — 중간 보스·보스가 떨군 정예 상자를 <c>RoomFlow</c> 가 <c>GatesExit</c> 로 묶는다.
/// v0.8: 정예·보스 방에도 덤 보상(그 방으로 들어온 문의 보상 — <c>RoomState.Reward</c>)이 있다.
/// 받는 길(WP3): 물체를 주우면 <see cref="Kind"/> 마다 — 마법서 = 보상 대기 +1, 무기 강화 꾸러미 = 무작위 무기 +<c>weapon_levels</c>·물약·금화,
/// 성소 = 무기 레벨·짐 <c>Mods</c>·<c>Shrines</c>(<c>pick_weapon</c> 이면 창). 줄 카드가 0장이면 <c>단련</c>(<c>rules.level_up.temper</c>)으로 바로 처리한다.
/// v0.9: 회복 문(<see cref="RewardKind.HealNextRoom"/>)은 방 안 물체가 없고(<see cref="Object"/> null 허용 — 들어올 때 회복), 상인(<see cref="RewardKind.Merchant"/>)은
/// <see cref="Merchant"/> 가 가리키는 상인이 보상 자리에 선다(물체 대신 <c>RoomState.Shop</c> — 6.19).
/// 종류마다 필요한 칸이 없거나 다른 종류의 칸이 있으면 로딩 오류(13.5-23 ④, WP7 — <c>DataRules</c>).
/// </summary>
public sealed record RewardDef : IDef
{
    public required string Id { get; init; }
    public string Name { get; init; } = "";
    public required RewardKind Kind { get; init; }
    /// <summary>방 안 물체(<c>kind: reward</c> 획득물). v0.9: <c>heal_next_room</c>·<c>merchant</c> 만 null(그 밖은 로딩 오류).</summary>
    public DefRef<PickupDef>? Object { get; init; }
    /// <summary>문 위 아이콘(48px)·HUD 방 줄.</summary>
    public IconDef? Icon { get; init; }
    /// <summary><c>weapon_cards</c>: 후보 가중(<c>owned_weapon</c>·<c>new_weapon</c>).</summary>
    public RewardCardsDef? Cards { get; init; }
    /// <summary>창의 건너뛰기 = 회복(<c>heal</c>, 체력 값). v0.8: <c>gold</c> 없앰.</summary>
    public RewardSkipDef? Skip { get; init; }
    /// <summary><c>weapon_bundle</c>: 무작위 무기 레벨·체력 물약·금화(고르지 않음).</summary>
    public RewardBundleDef? Bundle { get; init; }
    /// <summary><c>shrine</c>: 원작 성소 효과(대가 있음, 한 판에 한 번 — 짐 <c>Shrines</c>).</summary>
    public ShrineDef? Shrine { get; init; }
    /// <summary><c>merchant</c>(v0.9): 보상 자리에 서는 상인(그리스월드·아드리아·페핀 — 6.19). 다른 종류에는 적으면 로딩 오류.</summary>
    public DefRef<MerchantDef>? Merchant { get; init; }
    /// <summary>
    /// 물체에 닿으면 열리고 이 물체가 떨어지는 보상(v0.9.1 V8 — 보물 상자 → 책 <c>items\fbook</c>). 두 단계 상태는 <c>Pickup.RewardStage</c>(<c>Closed</c> → <c>Dropped</c>),
    /// 올릴 무기는 여는 틱에 정해 <c>Pickup.RewardChosen</c>(6.19 "보상 물체 둘레 아이콘"). null 이면 한 번 닿기로 받는다.
    /// </summary>
    public DefRef<PickupDef>? Drop { get; init; }
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
/// 보물 상자 <c>bundle</c>(v0.8 → v0.9, 6.18): 진화 아니고 만렙 아닌 가진 무기 중 무작위 하나 +<see cref="WeaponLevels"/>(없으면 <c>단련</c>)
/// + 체력 물약 <see cref="Potions"/>(v0.9.1 — 물약 칸에 쌓임, 가득이면 빠짐. v0.8 의 <c>belt_potions</c>) + 금화 <see cref="Gold"/> [최소, 최대] × 액트 <c>gold_scale</c>.
/// </summary>
public sealed record RewardBundleDef
{
    public int WeaponLevels { get; init; } = 1;
    /// <summary>체력 물약 수(v0.9.1 — 물약 칸, 6.19). 칸이 가득이면 넘는 만큼은 빠진다(마시지 않는다).</summary>
    public int Potions { get; init; }
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
