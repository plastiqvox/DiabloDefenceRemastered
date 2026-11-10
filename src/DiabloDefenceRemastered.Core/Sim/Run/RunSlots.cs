namespace DiabloDefenceRemastered.Core.Sim.Run;

/// <summary>
/// 열린 칸 수(v0.8.1, 설계서 3.6 "계단 보상" — 계약·구현 WP1f): <b>액트 번호만의 함수</b>
/// = <c>min(6, rules.level_up.*_slots + (액트 번호 − 1) × rules.rooms.stairs.*_slots)</c>, 6 = <see cref="WeaponSlots.SlotCount"/>·<see cref="PassiveSlots.SlotCount"/>.
/// 판 시작(주인공 만들기 WP3 — <c>World.Room?.Plan.Act ?? 1</c>)과 계단(<c>RunSession</c>)이 같은 함수를 쓴다 → <c>--room 2:n</c>·<c>--stage stage_catacombs</c> 로 액트 2 에서 시작해도
/// 보통 판의 액트 2 와 같은 칸 수다. <c>--weapons</c>·<c>--passives</c> 가 시작 액트의 열린 칸보다 많으면 옵션 오류(64 — WP7 이 이 함수로 확인).
/// </summary>
public static class RunSlots
{
    /// <summary>
    /// 액트 <paramref name="actNumber"/>(1부터)의 열린 무기·패시브 칸 수. <paramref name="levelUp"/> 이 null 이면 <see cref="LevelUpRulesDef"/> 기본값(4·4).
    /// 1 미만의 액트 번호는 1 로 본다.
    /// </summary>
    public static (int Weapons, int Passives) OpenAt(int actNumber, LevelUpRulesDef? levelUp, RoomRulesDef rooms)
    {
        ArgumentNullException.ThrowIfNull(rooms);
        levelUp ??= new LevelUpRulesDef();
        var steps = Math.Max(0, actNumber - 1);
        return (Math.Min(WeaponSlots.SlotCount, levelUp.WeaponSlots + steps * rooms.Stairs.WeaponSlots),
                Math.Min(PassiveSlots.SlotCount, levelUp.PassiveSlots + steps * rooms.Stairs.PassiveSlots));
    }

    /// <summary><see cref="OpenAt(int, LevelUpRulesDef?, RoomRulesDef)"/> 를 <c>rules</c> 에서(<c>rules.rooms</c> 가 없으면 예외).</summary>
    public static (int Weapons, int Passives) OpenAt(int actNumber, RulesDef rules) =>
        OpenAt(actNumber, rules.LevelUp, rules.Rooms ?? throw new InvalidOperationException("열린 칸 수에는 rules.rooms 가 있어야 한다(stairs, 6.10)"));
}
