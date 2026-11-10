namespace DiabloDefenceRemastered.Core.Data.Loading;

/// <summary>정의 형 ↔ JSON 종류 키(6.1)와 <see cref="GameDefs"/> 표 찾기.</summary>
internal static class DefKinds
{
    private static readonly Dictionary<Type, string> Names = new()
    {
        [typeof(SpriteDef)] = "sprites",
        [typeof(SpriteSetDef)] = "sprite_sets",
        [typeof(EffectDef)] = "effects",
        [typeof(ProjectileDef)] = "projectiles",
        [typeof(TilesetDef)] = "tilesets",
        [typeof(MapDef)] = "maps",
        [typeof(EnemyDef)] = "enemies",
        [typeof(AffixDef)] = "affixes",
        [typeof(BuffDef)] = "buffs",
        [typeof(WeaponDef)] = "weapons",
        [typeof(PassiveDef)] = "passives",
        [typeof(EvolutionDef)] = "evolutions",
        [typeof(PickupDef)] = "pickups",
        [typeof(DropTableDef)] = "drop_tables",
        [typeof(StageDef)] = "stages",
        [typeof(SoundDef)] = "sounds",
        [typeof(MusicDef)] = "music",
        [typeof(HeroClassDef)] = "hero_classes",
        [typeof(RewardDef)] = "rewards",
        [typeof(ItemBaseDef)] = "items",
        [typeof(ItemAffixDef)] = "item_affixes",
        [typeof(MerchantDef)] = "merchants",
    };

    /// <summary>배열 종류 키 → 정의 형(<c>rules</c>·<c>sound_bindings</c> 는 객체라 없다).</summary>
    public static readonly IReadOnlyDictionary<string, Type> Types = Names.ToDictionary(kv => kv.Value, kv => kv.Key, StringComparer.Ordinal);

    public static string KindOf(Type defType) => Names.TryGetValue(defType, out var n) ? n : defType.Name;

    /// <summary>형마다 ID → 정의 찾기 표(연결 단계에서 한 번 만든다).</summary>
    public static Dictionary<Type, IReadOnlyDictionary<string, IDef>> Index(GameDefs defs)
    {
        var index = new Dictionary<Type, IReadOnlyDictionary<string, IDef>>();
        foreach (var t in Names.Keys)
            if (Table(defs, t) is { } table) index[t] = table;
        return index;
    }

    private static IReadOnlyDictionary<string, IDef>? Table(GameDefs defs, Type t)
    {
        IEnumerable<IDef>? all =
            t == typeof(SpriteDef) ? defs.Sprites.All :
            t == typeof(SpriteSetDef) ? defs.SpriteSets.All :
            t == typeof(EffectDef) ? defs.Effects.All :
            t == typeof(ProjectileDef) ? defs.Projectiles.All :
            t == typeof(TilesetDef) ? defs.Tilesets.All :
            t == typeof(MapDef) ? defs.Maps.All :
            t == typeof(EnemyDef) ? defs.Enemies.All :
            t == typeof(AffixDef) ? defs.Affixes.All :
            t == typeof(BuffDef) ? defs.Buffs.All :
            t == typeof(WeaponDef) ? defs.Weapons.All :
            t == typeof(PassiveDef) ? defs.Passives.All :
            t == typeof(EvolutionDef) ? defs.Evolutions.All :
            t == typeof(PickupDef) ? defs.Pickups.All :
            t == typeof(DropTableDef) ? defs.DropTables.All :
            t == typeof(StageDef) ? defs.Stages.All :
            t == typeof(SoundDef) ? defs.Sounds.All :
            t == typeof(MusicDef) ? defs.Music.All :
            t == typeof(HeroClassDef) ? defs.HeroClasses.All :
            t == typeof(RewardDef) ? defs.Rewards.All :
            t == typeof(ItemBaseDef) ? defs.ItemBases.All :
            t == typeof(ItemAffixDef) ? defs.ItemAffixes.All :
            t == typeof(MerchantDef) ? defs.Merchants.All :
            null;
        if (all is null) return null;
        var d = new Dictionary<string, IDef>(StringComparer.Ordinal);
        foreach (var x in all) d.TryAdd(x.Id, x);
        return d;
    }
}

/// <summary>"비슷한 ID" 제안(편집 거리 ≤ 2, 6.2).</summary>
internal static class IdSuggest
{
    /// <summary><c> (혹시 'x'?)</c> 또는 빈 문자열. 거리가 가장 작은 것, 같으면 서수 순 첫 것.</summary>
    public static string Hint(string id, IEnumerable<string> candidates)
    {
        string? best = null;
        var bestD = 3;
        foreach (var c in candidates.OrderBy(c => c, StringComparer.Ordinal))
        {
            if (Math.Abs(c.Length - id.Length) > 2) continue;
            var d = Distance(id, c);
            if (d < bestD && d > 0)
            {
                best = c;
                bestD = d;
            }
        }
        return best is null ? "" : $" (혹시 '{best}'?)";
    }

    public static int Distance(string a, string b)
    {
        var prev = new int[b.Length + 1];
        var cur = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++) prev[j] = j;
        for (var i = 1; i <= a.Length; i++)
        {
            cur[0] = i;
            for (var j = 1; j <= b.Length; j++)
                cur[j] = Math.Min(Math.Min(cur[j - 1] + 1, prev[j] + 1), prev[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
            (prev, cur) = (cur, prev);
        }
        return prev[b.Length];
    }
}
