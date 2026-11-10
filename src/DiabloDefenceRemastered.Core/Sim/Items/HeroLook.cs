namespace DiabloDefenceRemastered.Core.Sim.Items;

/// <summary>
/// 주인공 겉모습 키(v0.9.1 V10, 설계서 6.19 "주인공 겉모습" — 계약·구현 WP1g, 그 뒤 WP3. <b>글자는 계약</b> — 바꾸면 직업 정의 <c>looks</c>·그림 묶음이 같이 바뀐다).
/// 키 = 무게 글자(갑옷 칸: 없음·경 → <c>l</c>, 중 → <c>m</c>, 판금 → <c>h</c>) + 든 것 글자(지팡이 → <c>t</c>, 방패만 → <c>u</c>, 없음 → <c>n</c>).
/// 지팡이와 방패를 함께 들면 <c>t</c>(v0.9.1 — 기획 v0.9.2 열린 질문 53: 함께 든 그림이 원작에 없어 지팡이 벌을 쓰고 방패는 능력치만).
/// 투구·방패의 무게는 겉모습에 들지 않는다(이동·방어만). 시뮬레이션은 키만 알고 그림 묶음 고르기·대신 쓰기(<c>HeroClassDef.Look</c>)는 그리기(WP5)가 한다.
/// </summary>
public static class HeroLook
{
    /// <summary>시작 겉모습(갑옷·든 것 없음 — 기획 34절 <c>sln</c>).</summary>
    public const string Start = "ln";

    /// <param name="equipment">칸 번호(<see cref="EquipSlot"/>) 순 장비, 빈 칸 null. 길이가 짧으면 없는 칸은 빈 칸.</param>
    public static string Key(IReadOnlyList<EquipItem?> equipment, GameDefs defs)
    {
        ArgumentNullException.ThrowIfNull(equipment);
        ArgumentNullException.ThrowIfNull(defs);
        var body = Base(equipment, EquipSlot.Body, defs);
        var staff = Base(equipment, EquipSlot.Staff, defs) is not null;
        var shield = Base(equipment, EquipSlot.Shield, defs) is not null;
        return Key(body?.Weight ?? ArmorWeight.None, staff, shield);
    }

    /// <summary>글자 조합(무게, 지팡이 있음, 방패 있음) → 키.</summary>
    public static string Key(ArmorWeight bodyWeight, bool staff, bool shield)
    {
        var w = bodyWeight switch
        {
            ArmorWeight.Medium => 'm',
            ArmorWeight.Heavy => 'h',
            _ => 'l',
        };
        var held = staff ? 't' : shield ? 'u' : 'n';
        return string.Create(2, (w, held), static (span, s) => { span[0] = s.w; span[1] = s.held; });
    }

    private static ItemBaseDef? Base(IReadOnlyList<EquipItem?> equipment, EquipSlot slot, GameDefs defs)
    {
        var i = (int)slot;
        if (i >= equipment.Count || equipment[i] is not { } item) return null;
        return defs.ItemBases.TryGet(item.BaseId, out var b) ? b : null;
    }
}
