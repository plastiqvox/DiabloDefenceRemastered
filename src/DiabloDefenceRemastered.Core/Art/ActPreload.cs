using DiabloDefenceRemastered.Core.Assets;

namespace DiabloDefenceRemastered.Core.Art;

/// <summary>그림 하나 × 변종 하나(아틀라스는 그 변종 페이지들, grid 는 파일 하나).</summary>
public readonly record struct SpriteUse(SpriteDef Sprite, string Variant);

/// <summary>
/// 액트 시작 때 미리 읽을 것 (설계서 7.5 "그림 메모리", 소유 WP5). 게임 <c>ArtServices</c> 가 이 목록만 읽고 방 이동 때는 읽지 않는다(0.5s 어두워짐 안에 디스크 읽기가 끼지 않게).
/// <list type="bullet">
/// <item>그림 묶음: 그 액트에서 나올 수 있는 적(<c>StageReach.Enemies</c> — 구간 가중치·이벤트·소환·분열·호위)의 (묶음 × 변종)과 주인공 직업 묶음·겉모습 묶음 전부, 상인 묶음 전부(원색, v0.9.1).</item>
/// <item>묶음에 들지 않은 그림(무기·이펙트·투사체·획득물·아이콘·보상 물체)은 모두 원색으로.</item>
/// <item>맵: 그 액트 방 맵 풀의 모든 맵(일반·정예·보스) + <c>mirror: true</c> 인 맵의 거울판(<c>&lt;art&gt;@m</c>, v0.8.1).</item>
/// </list>
/// 액트가 없으면(미리보기·방 없는 시험) 모든 묶음의 모든 변종.
/// </summary>
public static class ActPreload
{
    public static IReadOnlyList<SpriteUse> Sprites(GameDefs defs, StageDef? stage, HeroClassDef? hero)
    {
        var inSets = new HashSet<string>(StringComparer.Ordinal);
        foreach (var set in defs.SpriteSets.All)
            foreach (var clip in Clips(set)) inSets.Add(clip.Id);

        var uses = new List<SpriteUse>();
        var seen = new HashSet<(string, string)>();
        void Add(SpriteDef sprite, string variant)
        {
            if (seen.Add((sprite.Id, variant))) uses.Add(new SpriteUse(sprite, variant));
        }
        void AddSet(SpriteSetDef set, string variant)
        {
            foreach (var clip in Clips(set)) Add(clip, variant);
        }

        if (stage is null)
            foreach (var set in defs.SpriteSets.All)
                foreach (var v in set.Variants) AddSet(set, v);
        else
        {
            if (hero is not null)
            {
                AddSet(hero.Sprites.Def, "");
                // 겉모습 아홉 벌(v0.9.1 V10, 6.19) — 상인에게 산 장비로 방 안에서 바뀌므로 모두 미리 읽는다(방 이동 때 읽지 않는 규칙)
                foreach (var look in hero.Looks.Values) AddSet(look.Def, "");
            }
            foreach (var enemy in StageReach.Enemies(stage, defs)) AddSet(enemy.Sprites.Def, enemy.Variant);
            // 상인 묶음(v0.9 — 방 안 상인·문 위 아이콘·상인 창 머리). 어느 액트에서 나올지는 문 뽑기가 정하므로 모두(셋)
            foreach (var m in defs.Merchants.All)
                if (m.Sprite is { } ms) AddSet(ms.Def, "");
        }
        foreach (var sprite in defs.Sprites.All)
            if (!inSets.Contains(sprite.Id)) Add(sprite, "");
        return uses;
    }

    /// <summary>액트 방 맵 풀(+ 거울판). 액트가 없으면 빈 목록.</summary>
    public static IReadOnlyList<MapDef> Maps(StageDef? stage)
    {
        var maps = new List<MapDef>();
        if (stage is null) return maps;
        void Add(MapDef m)
        {
            if (maps.Exists(x => ReferenceEquals(x, m) || x.Id == m.Id)) return;
            maps.Add(m);
            if (m.Mirrored is { } mirror) maps.Add(mirror);
        }
        foreach (var m in stage.RoomMaps.Normal) Add(m.Def);
        foreach (var m in stage.RoomMaps.Elite) Add(m.Def);
        if (stage.RoomMaps.Boss is { } boss) Add(boss.Def);
        return maps;
    }

    /// <summary>미리 읽을 그림 파일(자원 뿌리 기준 — 아틀라스는 메타의 페이지 수가 있어야 정하므로 원색 첫 페이지와 메타만, 페이지는 읽는 쪽이 메타로 늘린다).</summary>
    public static IEnumerable<string> PrimaryFiles(SpriteUse use) =>
        use.Sprite.Source == SpriteSource.Atlas
            ? [SpriteMeta.MetaFile(use.Sprite.File), SpriteMeta.PageFile(use.Sprite.File, 0, use.Variant)]
            : [use.Sprite.File];

    private static IEnumerable<SpriteDef> Clips(SpriteSetDef set)
    {
        foreach (var kind in Enum.GetValues<ClipKind>())
            if (set.Clips.Get(kind) is { } clip) yield return clip.Sprite.Def;
    }
}
