namespace DiabloDefenceRemastered.Core.Art;

/// <summary>
/// 주인공 겉모습 묶음 고르기 (v0.9.1 V10, 설계서 6.19 "주인공 겉모습" — 그리기 WP5, 단계 2 wp5). 시뮬레이션은 키(<c>HeroLook.Key</c>)만 알고,
/// 그리기가 프레임마다 키 → <see cref="HeroClassDef.Looks"/> 묶음을 고른다. 그 묶음을 못 쓰면(가져오기에서 빠짐·자원 없음) <see cref="HeroClassDef.Look"/> 의 묶음,
/// 그것도 못 쓰면 직업 묶음 <see cref="HeroClassDef.Sprites"/>(시뮬레이션이 보는 묶음 — 동작 시간이 같다).
/// </summary>
public static class HeroLookArt
{
    /// <param name="available">그 묶음을 그릴 수 있나(게임: 서기 클립 그림을 미리 읽었고 자원 파일이 있음).</param>
    public static SpriteSetDef Pick(HeroClassDef hero, string key, Func<SpriteSetDef, bool> available)
    {
        ArgumentNullException.ThrowIfNull(hero);
        ArgumentNullException.ThrowIfNull(available);
        if (hero.Looks.TryGetValue(key, out var set) && set.IsBound && available(set.Def)) return set.Def;
        if (hero.Looks.TryGetValue(hero.Look, out var fallback) && fallback.IsBound && available(fallback.Def)) return fallback.Def;
        return hero.Sprites.Def;
    }

    /// <summary>지금 장비의 겉모습 묶음(<see cref="HeroLook.Key(IReadOnlyList{EquipItem?}, GameDefs)"/> → <see cref="Pick"/>).</summary>
    public static SpriteSetDef Pick(HeroClassDef hero, Hero h, GameDefs defs, Func<SpriteSetDef, bool> available)
    {
        ArgumentNullException.ThrowIfNull(h);
        return Pick(hero, HeroLook.Key(h.Equipment, defs), available);
    }
}
