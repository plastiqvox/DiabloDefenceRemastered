using System.Diagnostics.CodeAnalysis;

namespace DiabloDefenceRemastered.Core.Data.Registry;

/// <summary>
/// 한 종류의 정의 목록. 순서 = (파일 서수 순서, 파일 안 순서)로 고정된다(설계서 3.4). ID 사전은 찾기에만 쓴다.
/// </summary>
public sealed class DefTable<T> where T : class, IDef
{
    private readonly T[] _items;
    private readonly Dictionary<string, int> _index;

    public DefTable(IEnumerable<T> items)
    {
        _items = [.. items];
        _index = new Dictionary<string, int>(_items.Length, StringComparer.Ordinal);
        for (var i = 0; i < _items.Length; i++)
            if (!_index.TryAdd(_items[i].Id, i))
                throw new ArgumentException($"{typeof(T).Name} ID '{_items[i].Id}' 가 두 번 있다");
    }

    public static DefTable<T> Empty { get; } = new([]);

    public int Count => _items.Length;
    public T this[int index] => _items[index];
    public IReadOnlyList<T> All => _items;
    public ReadOnlySpan<T> AsSpan() => _items;

    public bool TryGet(string id, [NotNullWhen(true)] out T? def)
    {
        if (_index.TryGetValue(id, out var i)) { def = _items[i]; return true; }
        def = null;
        return false;
    }

    public T Get(string id) =>
        TryGet(id, out var def) ? def : throw new KeyNotFoundException($"{typeof(T).Name} '{id}' 가 없다");

    /// <summary>목록 안 번호(틱 이벤트의 DefIndex). 없으면 -1.</summary>
    public int IndexOf(string id) => _index.TryGetValue(id, out var i) ? i : -1;

    public int IndexOf(T def) => IndexOf(def.Id);
}

/// <summary>
/// 로딩 결과 = 모든 정의 (설계서 6.2). 로더(WP7)가 만든 뒤 바뀌지 않는다. 시험은 코드로 만든다(TestKit).
/// 종류 키는 <see cref="Kinds"/>. <c>tilesets</c>·<c>maps</c> 는 Core <c>Map/MapDataReader</c> 결과를 그대로 받는다(타일셋 연결까지 됨).
/// </summary>
public sealed class GameDefs
{
    /// <summary>JSON 최상위 종류 키(설계서 6.1). <c>rules</c>·<c>sound_bindings</c> 는 객체 하나, 나머지는 배열.</summary>
    public static readonly IReadOnlyList<string> Kinds =
    [
        "rules", "sprites", "sprite_sets", "effects", "projectiles", "tilesets", "maps", "enemies", "affixes", "buffs",
        "weapons", "passives", "evolutions", "pickups", "drop_tables", "stages", "sounds", "music", "sound_bindings",
        "hero_classes", "rewards", "items", "item_affixes", "merchants",
    ];

    public static GameDefs Empty { get; } = new();

    public RulesDef Rules { get; init; } = new();
    public DefTable<SpriteDef> Sprites { get; init; } = DefTable<SpriteDef>.Empty;
    public DefTable<SpriteSetDef> SpriteSets { get; init; } = DefTable<SpriteSetDef>.Empty;
    public DefTable<EffectDef> Effects { get; init; } = DefTable<EffectDef>.Empty;
    public DefTable<ProjectileDef> Projectiles { get; init; } = DefTable<ProjectileDef>.Empty;
    public DefTable<TilesetDef> Tilesets { get; init; } = DefTable<TilesetDef>.Empty;
    public DefTable<MapDef> Maps { get; init; } = DefTable<MapDef>.Empty;
    public DefTable<EnemyDef> Enemies { get; init; } = DefTable<EnemyDef>.Empty;
    public DefTable<AffixDef> Affixes { get; init; } = DefTable<AffixDef>.Empty;
    public DefTable<BuffDef> Buffs { get; init; } = DefTable<BuffDef>.Empty;
    public DefTable<WeaponDef> Weapons { get; init; } = DefTable<WeaponDef>.Empty;
    public DefTable<PassiveDef> Passives { get; init; } = DefTable<PassiveDef>.Empty;
    public DefTable<EvolutionDef> Evolutions { get; init; } = DefTable<EvolutionDef>.Empty;
    public DefTable<PickupDef> Pickups { get; init; } = DefTable<PickupDef>.Empty;
    public DefTable<DropTableDef> DropTables { get; init; } = DefTable<DropTableDef>.Empty;
    public DefTable<StageDef> Stages { get; init; } = DefTable<StageDef>.Empty;
    public DefTable<SoundDef> Sounds { get; init; } = DefTable<SoundDef>.Empty;
    public DefTable<MusicDef> Music { get; init; } = DefTable<MusicDef>.Empty;
    public SoundBindingsDef SoundBindings { get; init; } = new();
    /// <summary>직업(v0.6, 6.16). 주인공 코드는 이 표가 아니라 <c>World.HeroClass</c>(고른 직업 하나)를 읽는다.</summary>
    public DefTable<HeroClassDef> HeroClasses { get; init; } = DefTable<HeroClassDef>.Empty;
    /// <summary>문 보상(v0.7, 6.18). 액트의 <c>rewards.weights</c>·<c>first_room</c>·<c>rules.rooms.fallback_reward</c> 가 가리킨다.</summary>
    public DefTable<RewardDef> Rewards { get; init; } = DefTable<RewardDef>.Empty;
    /// <summary>
    /// 장비 기본 아이템(v0.9, 6.19 — 종류 키 <c>items</c>, <c>Data/items/bases.json</c>). 상인 물건(<c>ShopStock</c>)이 그 액트·칸 종류 중 균등으로 고른다.
    /// </summary>
    public DefTable<ItemBaseDef> ItemBases { get; init; } = DefTable<ItemBaseDef>.Empty;
    /// <summary>
    /// 장비 접사(v0.9, 6.19 — 종류 키 <c>item_affixes</c>, <c>Data/items/affixes.json</c>). 적 접두사 <see cref="Affixes"/>(<c>affixes</c>, 6.7)와 다른 표다.
    /// 표 순서가 접사 고르기 후보 순서다(<c>ItemRoller</c>).
    /// </summary>
    public DefTable<ItemAffixDef> ItemAffixes { get; init; } = DefTable<ItemAffixDef>.Empty;
    /// <summary>상인(v0.9, 6.19 — 종류 키 <c>merchants</c>, <c>Data/merchants/&lt;id&gt;.json</c>). 문 보상 <c>kind: merchant</c> 가 가리킨다.</summary>
    public DefTable<MerchantDef> Merchants { get; init; } = DefTable<MerchantDef>.Empty;
}
