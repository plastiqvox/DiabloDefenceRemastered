namespace DiabloDefenceRemastered.Core.Sim.Contracts;

/// <summary>상인 카드 종류(v0.9, 6.19): 장비(그리스월드·아드리아) / 물약 칸 강화(페핀).</summary>
public enum ShopCardKind : byte { Equip, Potion }

/// <summary>
/// 상인 카드 한 장(v0.9, 6.19). <paramref name="Item"/> = 장비 카드의 굴린 장비(물약 카드는 null), <paramref name="Upgrade"/> = 물약 카드의 종류(장비 카드는 뜻 없음),
/// <paramref name="Price"/> = 지금 값, <paramref name="Bought"/> = 장비 카드는 "산 것"(자리를 지켜 어둡게), 물약 카드는 "살 수 없음"(다 올림·칸 가득 — 기획 12.8).
/// 칸끼리 막지 않으므로(v0.9.1 — 지팡이 + 방패 함께) "빠지는 칸" 자리를 두지 않는다.
/// </summary>
public sealed record ShopCard(ShopCardKind Kind, EquipItem? Item, PotionUpgrade Upgrade, int Price, bool Bought);

/// <summary>
/// 상인 방 상태(v0.9, 설계서 6.19 — 계약 WP1g, 방 수명 → <c>RoomState.Shop</c>). 물건은 상인 방에 <b>들어오는 틱</b>에 <c>RunSession</c> 이 <c>ShopStock.Create</c>(상인 전용 난수 흐름 —
/// 방 난수를 쓰지 않음)로 정한다. 쓰기: <c>RunSession</c>(만들기), WP3 <see cref="IShop"/>(사기·다시 굴리기·떠나기·열기). 읽기: <c>RoomFlow</c>(<see cref="Visited"/> 전에는 문이 안 열림,
/// <see cref="Open"/> 이면 나가지 못함), WP5(상인 그리기), WP6(상인 창), WP4(자동 조작), WP7(해시).
/// </summary>
public sealed class ShopState
{
    public required string MerchantId { get; init; }
    /// <summary>카드(장비 상인은 <c>kinds</c> 순, 페핀은 <c>upgrades</c> 순 — 최대 셋, <c>Choice1~3</c>).</summary>
    public List<ShopCard> Cards { get; } = [];
    /// <summary>이 방에서 누른 다시 굴리기 수(다음 값 = <c>reroll_price + reroll_step × Rerolls</c>, 흐름 = <c>RunSeeds.Shop(…, Rerolls, 카드)</c>).</summary>
    public int Rerolls { get; set; }
    /// <summary>창이 열려 있음(시간 멈춤 — 레벨업 창과 같은 멈춤).</summary>
    public bool Open { get; set; }
    /// <summary>한 번이라도 닫았음 = 보상 받음(문이 열림).</summary>
    public bool Visited { get; set; }
    /// <summary>반지 둘이 찬 채 반지를 고른 카드 번호(−1 = 없음) — 창이 "바꿀 반지" 둘로 바뀐다.</summary>
    public int RingPending { get; set; } = -1;
}
