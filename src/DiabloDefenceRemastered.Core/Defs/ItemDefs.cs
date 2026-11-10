namespace DiabloDefenceRemastered.Core.Defs;

/// <summary>장비 종류(v0.9, 설계서 6.19 — 기획 11.6). JSON <c>head</c> / <c>body</c> / <c>shield</c> / <c>staff</c> / <c>ring</c>.</summary>
public enum EquipKind : byte { Head, Body, Shield, Staff, Ring }

/// <summary>
/// 장비 칸 여섯(v0.9, 기획 11.6 — 목걸이 없음). 순서 = HUD 장비 줄 순서 = 짐 <c>HeroCarry.Equipment</c> 순서 = <c>Hero.Equipment</c> 번호.
/// 칸끼리 막지 않는다(v0.9.1 — 기획 v0.9.2 열린 질문 53: 지팡이와 방패를 함께 든다, 원작 양손 규칙 없음).
/// </summary>
public enum EquipSlot : byte { Head, Body, Shield, Staff, Ring1, Ring2 }

/// <summary>
/// 방어구 무게(v0.9.1 V6 — 원작 갑옷 그림 L·M·H). JSON <c>none</c>(생략) / <c>light</c> / <c>medium</c> / <c>heavy</c>. 지팡이·반지는 <see cref="None"/>.
/// 이동 감소는 <c>rules.equipment.weight_move</c> 에서(정의에 이동을 따로 적지 않음), 겉모습 글자는 <c>HeroLook.Key</c>(갑옷만).
/// </summary>
public enum ArmorWeight : byte { None, Light, Medium, Heavy }

/// <summary>장비 칸 도우미(v0.9).</summary>
public static class EquipSlots
{
    /// <summary>칸 수(6).</summary>
    public const int Count = 6;

    /// <summary>칸이 받는 종류(반지 두 칸 = <see cref="EquipKind.Ring"/>).</summary>
    public static EquipKind KindOf(EquipSlot slot) => slot switch
    {
        EquipSlot.Head => EquipKind.Head,
        EquipSlot.Body => EquipKind.Body,
        EquipSlot.Shield => EquipKind.Shield,
        EquipSlot.Staff => EquipKind.Staff,
        _ => EquipKind.Ring,
    };
}

/// <summary>
/// 기본 아이템 <c>items[]</c>(v0.9 → v0.9.1, 설계서 6.19, 기획 11.6 표 — 파일 <c>Data/items/bases.json</c>, 소유 WP3). 기본 능력치는 굴리지 않는다.
/// 이름은 원작 영어 이름 그대로(기획 11.6 "이름").
/// </summary>
public sealed record ItemBaseDef : IDef
{
    public required string Id { get; init; }
    public string Name { get; init; } = "";
    public required EquipKind Kind { get; init; }
    /// <summary>방어구 무게(v0.9.1 V6). 지팡이·반지는 <see cref="ArmorWeight.None"/>(로딩 검사).</summary>
    public ArmorWeight Weight { get; init; }
    /// <summary>기본 능력치(4.1 능력치 키 — 굴리지 않음).</summary>
    public IReadOnlyDictionary<StatKey, float> Stats { get; init; } = new Dictionary<StatKey, float>();
    /// <summary>금화 값(접사 값이 더해짐 — <c>ItemRoller</c>).</summary>
    public required int Price { get; init; }
    /// <summary>나오는 액트 번호(1부터).</summary>
    public IReadOnlyList<int> Acts { get; init; } = [1, 2];
    /// <summary>늘 마법 등급(반지 — 기획 11.6).</summary>
    public bool AlwaysMagic { get; init; }
    /// <summary>인벤토리 그림(원작 <c>objcurs</c> 프레임 — 가져오기 전 코드 그림, 6.3 대체 규칙). 상인 카드·HUD 장비 칸.</summary>
    public IconDef? Icon { get; init; }
    /// <summary>바닥 그림(원작 <c>items\*</c> — 장비 드롭은 E, 지금은 읽기만).</summary>
    public IconDef? Floor { get; init; }
    public IReadOnlyList<ContentBundle> Requires { get; init; } = [];
}

/// <summary>
/// 장비 접사 <c>item_affixes[]</c>(v0.9 → v0.9.1 V7, 설계서 6.19, 기획 11.6 접사 표 — 파일 <c>Data/items/affixes.json</c>, 소유 WP3).
/// 6.19 의 <c>AffixDef</c>·종류 키 <c>affixes</c> 는 적 접두사(6.7)가 이미 쓰므로 형 이름 <c>ItemAffixDef</c>·종류 키 <c>item_affixes</c> 로 둔다(13장 WP1g 구현 기록).
/// 값은 [<see cref="Min"/>, <see cref="Max"/>] 를 <see cref="Step"/> 간격으로 나눈 값 중 균등(<c>Min == Max</c> 면 고정 — 굴림 위치 0.5).
/// 퍼센트 키(<c>_pct</c>)는 비율로 적는다(0.03 = 3%).
/// </summary>
public sealed record ItemAffixDef : IDef
{
    public required string Id { get; init; }
    /// <summary>원작 접사 이름(영어 — <c>Fine</c>·<c>of the Fox</c>).</summary>
    public required string Name { get; init; }
    /// <summary>참 = 접두사, 거짓 = 접미사.</summary>
    public required bool Prefix { get; init; }
    /// <summary>붙는 칸 종류.</summary>
    public required IReadOnlyList<EquipKind> Kinds { get; init; }
    public required StatKey Stat { get; init; }
    public required float Min { get; init; }
    public required float Max { get; init; }
    /// <summary>굴림 간격(&gt; 0). 범위가 간격으로 나누어떨어지지 않으면 끝 값은 <see cref="Max"/> 를 넘지 않는 마지막 칸.</summary>
    public float Step { get; init; } = 1f;
    /// <summary>범위 가운데 값일 때 더하는 금화(굴림 위치로 × 0.75~1.25 — <c>rules.equipment.price_roll</c>).</summary>
    public required int Price { get; init; }
    /// <summary>1 = 두 액트, 2 = 액트 2 만.</summary>
    public int Stage { get; init; } = 1;
}
