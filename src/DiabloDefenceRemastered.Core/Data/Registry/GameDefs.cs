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
}
