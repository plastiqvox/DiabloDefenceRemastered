namespace DiabloDefenceRemastered.Core.Map;

/// <summary>입구 하나의 월드 px: 바깥 시작점(맵 밖) → 진입점(맵 안).</summary>
public sealed record ResolvedEntry(string Id, Vector2 Outside, Vector2 Inside);

/// <summary>
/// 맵 정의의 하위 타일 좌표를 로딩 때 한 번 월드 px 로 푼 것 (설계서 4.1, 계약 WP1b) — <c>World.Resolved</c>.
/// 감독·행진·보스 입장(WP4), 점 빛(WP5), 길 검사 13.5-6(WP2)은 이것만 읽고 <see cref="IsoMath"/> 를 따로 부르지 않는다
/// → 타일 좌표가 월드 px 로 쓰여 맵 왼쪽 위 어둠에 조용히 생기는 일이 없다.
/// </summary>
public sealed class ResolvedMap
{
    private readonly Dictionary<string, ResolvedEntry> _entries;
    private readonly Dictionary<string, Vector2> _anchors;

    private ResolvedMap(IsoMath iso, List<ResolvedEntry> entries, Dictionary<string, Vector2> anchors, Vector2 heroStart, Compass heroFacing)
    {
        Iso = iso;
        Entries = entries;
        _entries = new Dictionary<string, ResolvedEntry>(entries.Count, StringComparer.Ordinal);
        foreach (var e in entries)
            if (!_entries.TryAdd(e.Id, e)) throw new ArgumentException($"입구 ID '{e.Id}' 가 두 번 있다");
        _anchors = anchors;
        HeroStart = heroStart;
        HeroFacing = heroFacing;
    }

    public IsoMath Iso { get; }
    /// <summary>입구(정의 순서).</summary>
    public IReadOnlyList<ResolvedEntry> Entries { get; }
    /// <summary>기준점 ID → 월드 px(찾기 전용 — 순회 순서에 기대지 않는다).</summary>
    public IReadOnlyDictionary<string, Vector2> Anchors => _anchors;
    /// <summary>주인공 시작 위치(월드 px).</summary>
    public Vector2 HeroStart { get; }
    /// <summary>주인공 시작 방위(땅 위 방위 — 화면 벡터는 <c>FacingMath.ToVector</c>).</summary>
    public Compass HeroFacing { get; }

    public static ResolvedMap Resolve(MapDef map) => Resolve(map, IsoMath.For(map, map.Tileset.Def));

    public static ResolvedMap Resolve(MapDef map, IsoMath iso)
    {
        var entries = new List<ResolvedEntry>(map.Entries.Count);
        foreach (var e in map.Entries)
            entries.Add(new ResolvedEntry(e.Id, iso.ToWorld(e.OutsideTile), iso.ToWorld(e.InsideTile)));
        var anchors = new Dictionary<string, Vector2>(map.AnchorsTile.Count, StringComparer.Ordinal);
        foreach (var (id, tile) in map.AnchorsTile) anchors[id] = iso.ToWorld(tile);
        return new ResolvedMap(iso, entries, anchors, iso.ToWorld(map.HeroStart.Tile), map.HeroStart.Facing);
    }

    public bool TryGetEntry(string id, out ResolvedEntry entry)
    {
        if (_entries.TryGetValue(id, out var e)) { entry = e; return true; }
        entry = null!;
        return false;
    }

    public ResolvedEntry Entry(string id) =>
        _entries.TryGetValue(id, out var e) ? e : throw new KeyNotFoundException($"입구 '{id}' 가 맵에 없다");

    public bool TryGetAnchor(string id, out Vector2 pos) => _anchors.TryGetValue(id, out pos);

    public Vector2 Anchor(string id) =>
        _anchors.TryGetValue(id, out var p) ? p : throw new KeyNotFoundException($"기준점 '{id}' 가 맵에 없다");

    /// <summary>기준점 ID 또는 하위 타일 좌표 → 월드 px(점 빛 <c>at</c>).</summary>
    public Vector2 ToWorld(TileOrAnchor at) => at.Anchor is { } id ? Anchor(id) : Iso.ToWorld(at.Tile);
}
