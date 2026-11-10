namespace DiabloDefenceRemastered.Core.Defs;

/// <summary>상인이 파는 것(v0.9 → v0.9.1 V11, 설계서 6.19). 그리스월드·아드리아 = <see cref="Equipment"/>, 페핀 = <see cref="Potions"/>.</summary>
public enum MerchantStock : byte { Equipment, Potions }

/// <summary>
/// 상인 <c>merchants[]</c>(v0.9, 설계서 6.19, 기획 11.7 — 파일 <c>Data/merchants/&lt;id&gt;.json</c>, 소유 WP3). 문 보상 <c>kind: merchant</c> 가 <c>merchant</c> 로 가리킨다.
/// 장비 상인은 <see cref="Kinds"/> 마다 카드 하나(그 액트 기본 아이템 중 균등 + <c>ItemRoller</c>), 물약 상인은 <see cref="Upgrades"/> 마다 카드 하나(굴리지 않음).
/// </summary>
public sealed record MerchantDef : IDef
{
    public required string Id { get; init; }
    /// <summary>그리스월드·아드리아·페핀(문 위·창 머리 이름).</summary>
    public required string Name { get; init; }
    public required MerchantStock Stock { get; init; }
    /// <summary>
    /// <see cref="MerchantStock.Equipment"/>: 카드 칸 종류 목록(그리스월드 head·body·shield, 아드리아 staff·ring·ring). 반지 둘은 서로 다른 접사.
    /// <see cref="MerchantStock.Potions"/> 에 적으면 로딩 오류.
    /// </summary>
    public IReadOnlyList<EquipKind> Kinds { get; init; } = [];
    /// <summary><see cref="MerchantStock.Potions"/>: 물약 주머니·빠른 물약·물약 채우기(v0.9.1 V11). 장비 상인에 적으면 로딩 오류.</summary>
    public IReadOnlyList<PotionUpgradeDef> Upgrades { get; init; } = [];
    /// <summary>다시 굴리기를 받는가(페핀 거짓 — 고정 물건).</summary>
    public bool Rerollable { get; init; } = true;
    /// <summary>
    /// 원작 <c>towners</c> 그림 묶음(서기 반복) — 문 위 아이콘·창 머리·방 안 상인. 가져오기 전·실패 때는 null 이고 코드 그림 대체(6.3 대체 규칙 —
    /// 6.19 는 필수로 적었지만 단계가 그림 때문에 막히지 않게 null 허용, 13장 WP1g 구현 기록).
    /// </summary>
    public DefRef<SpriteSetDef>? Sprite { get; init; }
    /// <summary>인사 대사 소리 연결 키(6.15).</summary>
    public string? GreetSound { get; init; }
    public IReadOnlyList<ContentBundle> Requires { get; init; } = [];
}

/// <summary>페핀의 물건 종류(v0.9.1 V11): 물약 주머니(칸 +1), 빠른 물약(회복 시간 단계 +1), 물약 채우기(빈 칸 × <c>per_slot</c>).</summary>
public enum PotionUpgrade : byte { Slots, Speed, Refill }

/// <summary>
/// 페핀 카드 하나(v0.9.1, 6.19). <see cref="Prices"/> = 단계마다 값(<c>Slots</c> 80·150·220, <c>Speed</c> 100·200) — 다 사면 <c>다 올림</c>.
/// <see cref="PerSlot"/> = <c>Refill</c> 의 빈 칸 하나 값(25). 같은 카드를 여러 번 살 수 있다(값은 단계마다).
/// </summary>
public sealed record PotionUpgradeDef
{
    public required PotionUpgrade Kind { get; init; }
    public IReadOnlyList<int> Prices { get; init; } = [];
    public int PerSlot { get; init; }
}
