using DiabloDefenceRemastered.Core.Data.Loading;

namespace DiabloDefenceRemastered.Core.Assets;

/// <summary>정의가 가리키는 파일 하나와 그 주인 정의 D(적·스테이지·직업·무기·음악, 또는 기본 내용이면 null — 6.14 13.5-10 귀속 규칙).</summary>
/// <param name="Owner">
/// 그 파일에 닿는 <c>requires</c> 를 가진 정의. 그림 묶음·소리·이펙트처럼 <c>requires</c> 가 없는 정의의 파일은 그것을 가리키는 D 들마다 한 줄씩,
/// 맵 그림(<c>art</c> 폴더)은 그 맵을 쓰는 스테이지(v0.7 — 그 맵을 <c>room_maps</c> 에 둔 스테이지들, 본문 WP7), 소리 연결 표 줄은 귀속 규칙대로(<c>hero.&lt;id&gt;</c> → 직업, <c>ui</c>·<c>pickups</c>·<c>music.title</c> → null).
/// </param>
/// <param name="File">Assets 뿌리 기준 '/' 상대 경로(<c>d1/monsters/zombie.png</c>).</param>
public readonly record struct DefAsset(IDef? Owner, string File);

/// <summary>
/// 정의 → 파일 이름(주인 포함) 추출 (설계서 6.14 13.5-10 ②·1.4 끝 시작 검사 ③ 교차 확인, 계약 WP1d v0.6.1, 본문 WP7).
/// 13.5-10 ② 시험(WP0)과 시작 검사(WP7)가 같은 추출을 쓴다 — 두 벌이면 "꺼짐" 판정과 이름 대조가 어긋난다.
/// <list type="bullet">
/// <item>주인 D = 적·스테이지·직업·무기·음악(<c>requires</c> 를 가진 정의). D 에서 참조를 따라 닿는 <c>requires</c> 없는 정의(그림 묶음·그림·이펙트·투사체·버프·획득물·드롭 표·소리 …)의
/// 파일은 그 D 의 것으로 센다 — 다른 D(소환한 적·직업의 시작 무기 등)에서는 멈춘다(그 D 가 자기 줄을 낸다).</item>
/// <item>적의 그림 묶음으로 닿은 atlas 그림은 적의 <c>variant</c> 페이지, 그 밖의 atlas 는 원색 페이지 + 메타(<see cref="SpriteMeta.PageFile"/>·<see cref="SpriteMeta.MetaFile"/>), grid 는 <c>file</c>.</item>
/// <item>스테이지의 맵(<c>room_maps</c> — v0.7)은 <paramref name="arenas"/> 의 같은 맵 격자로 <see cref="ArenaArt.OutputNames"/>(v0.8: 거울판 <c>art</c> 도 — 거울판 격자를 넘기면).</item>
/// <item>소리 연결 표: <c>hero.&lt;id&gt;</c> → 직업, <c>enemies.&lt;id&gt;</c> → 적, <c>sprite_sets.&lt;s&gt;</c> → 그 묶음을 쓰는 적·직업들, <c>weapons.&lt;w&gt;</c> → 무기,
/// <c>music.&lt;스테이지&gt;</c> → 그 음악 정의, <c>projectiles</c>·<c>effects</c>·<c>hazard_loops</c> → 그것을 가리키는 D 들(없으면 기본), <c>ui</c>·<c>pickups</c>·<c>rewards</c>·<c>music.title</c> → 기본.</item>
/// <item>어느 D 에서도 닿지 않는 정의(획득물 그림·보상 아이콘·패시브 아이콘 등)의 파일은 주인 null(기본). 소리가 하나라도 있으면 소리 색인(null).</item>
/// </list>
/// </summary>
public static class DefAssetNames
{
    /// <summary>
    /// 정의가 가리키는 모든 파일(atlas 페이지 × 변종·메타·grid PNG·맵 그림 — <see cref="ArenaArt.OutputNames"/>·소리·음악·소리 색인)과 주인.
    /// 순서는 (파일 서수, 주인 ID 서수) — 결정적. 같은 (주인, 파일)은 한 줄.
    /// </summary>
    /// <param name="arenas">맵 그림 이름 계산용 펼친 격자(<c>ArenaLayout.Expand</c>).</param>
    public static IReadOnlyList<DefAsset> For(GameDefs defs, IEnumerable<ArenaGrid> arenas)
    {
        ArgumentNullException.ThrowIfNull(defs);
        var grids = new Dictionary<string, List<ArenaGrid>>(StringComparer.Ordinal);
        foreach (var g in arenas)
        {
            var key = g.Map.MirrorOf ?? g.Map.Id;
            if (!grids.TryGetValue(key, out var list)) grids[key] = list = [];
            list.Add(g);
        }

        var result = new HashSet<(string? Owner, string File)>();
        var ownerOf = new Dictionary<string, IDef>(StringComparer.Ordinal);
        var reachedByAny = new HashSet<object>(ReferenceEqualityComparer.Instance);
        // 기본 내용이 아닌(어느 D 에서 닿은) 비 D 정의 → 그 D 들
        var reachers = new Dictionary<object, List<IDef>>(ReferenceEqualityComparer.Instance);

        void Add(IDef? owner, string file)
        {
            if (file.Length == 0) return;
            if (owner is not null) ownerOf[Key(owner)] = owner;
            result.Add((owner is null ? null : Key(owner), file));
        }

        foreach (var d in Owners(defs))
        {
            var variant = d is EnemyDef e ? e.Variant : "";
            var reach = Reach(defs, d);
            foreach (var x in reach)
            {
                reachedByAny.Add(x);
                if (!reachers.TryGetValue(x, out var who)) reachers[x] = who = [];
                who.Add(d);
            }
            var enemySet = d is EnemyDef en && en.Sprites.IsBound ? en.Sprites.Def : null;
            var enemySprites = enemySet is null ? [] : ClipSprites(enemySet);
            foreach (var x in reach)
                foreach (var f in FilesOf(x, enemySprites.Contains(x) ? variant : "")) Add(d, f);
            foreach (var f in DirectFiles(d)) Add(d, f);
            if (d is StageDef s)
                foreach (var m in StageMaps(s))
                    if (grids.TryGetValue(m.Id, out var gs))
                        foreach (var g in gs)
                            foreach (var n in ArenaArt.OutputNames(g)) Add(d, n);
        }

        // 어느 D 에서도 닿지 않는 정의 → 기본(소리 연결 표가 가리키는 소리는 아래 귀속 규칙으로)
        var bound = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var bc = new Collector(defs);
        DefWalker.Walk(defs.SoundBindings, "$", bc);
        foreach (var x in bc.Found) if (x is SoundDef) bound.Add(x);
        foreach (var x in AllNonOwnerDefs(defs))
            if (!reachedByAny.Contains(x) && !bound.Contains(x))
                foreach (var f in FilesOf(x, "")) Add(null, f);

        // 소리 연결 표 귀속
        var b = defs.SoundBindings;
        void Bind(IEnumerable<IDef?> owners, IEnumerable<DefRef<SoundDef>?> sounds)
        {
            var list = owners.ToList();
            if (list.Count == 0) list.Add(null);
            foreach (var sr in sounds)
                if (sr is { IsBound: true })
                    foreach (var o in list)
                        foreach (var f in sr.Def.Files) Add(o, f);
        }
        foreach (var (id, s) in b.Hero) Bind([defs.HeroClasses.TryGet(id, out var h) ? h : null], [s.Hurt, s.Death]);
        foreach (var (id, s) in b.Enemies) Bind([defs.Enemies.TryGet(id, out var e) ? e : null], [s.Attack, s.Hit, s.Death, s.Special, s.Voice]);
        foreach (var (id, s) in b.SpriteSets)
            Bind(defs.Enemies.All.Where(e => e.Sprites.Id == id).Cast<IDef>().Concat(defs.HeroClasses.All.Where(h => h.Sprites.Id == id)), [s.Attack, s.Hit, s.Death, s.Special]);
        foreach (var (id, s) in b.Weapons) Bind([defs.Weapons.TryGet(id, out var w) ? w : null], [s.Fire]);
        foreach (var (id, s) in b.Projectiles)
            Bind(defs.Projectiles.TryGet(id, out var p) && reachers.TryGetValue(p, out var who) ? who : [], [s.Fire]);
        foreach (var (id, s) in b.Effects)
            Bind(defs.Effects.TryGet(id, out var fx) && reachers.TryGetValue(fx, out var who) ? who : [], [s]);
        foreach (var (id, s) in b.HazardLoops)
            Bind(defs.Effects.TryGet(id, out var fx) && reachers.TryGetValue(fx, out var who) ? who : [], [s]);
        foreach (var (_, s) in b.Ui) Bind([], [s]);
        foreach (var (_, s) in b.Pickups) Bind([], [s.Collect, s.Drop]);
        foreach (var (_, s) in b.Rewards) Bind([], [s.Collect, s.Open]);
        foreach (var (key, m) in b.Music)
        {
            if (!m.IsBound) continue;
            // music.<스테이지> → 그 음악 정의(음악 D), music.title → 기본
            IDef? owner = key == "title" ? null : m.Def;
            Add(owner, m.Def.File);
            if (m.Def.Fallback is { IsBound: true } fb) Add(null, fb.Def.File);
        }
        foreach (var m in defs.Music.All) Add(m, m.File);
        if (defs.Sounds.Count > 0) Add(null, AssetFormat.SoundIndexPath);

        return
        [
            .. result.Select(r => new DefAsset(r.Owner is null ? null : ownerOf[r.Owner], r.File))
                .OrderBy(a => a.File, StringComparer.Ordinal)
                .ThenBy(a => a.Owner?.Id ?? "", StringComparer.Ordinal),
        ];
    }

    private static string Key(IDef d) => $"{DefKinds.KindOf(d.GetType())}:{d.Id}";

    private static bool IsOwnerType(object x) => x is EnemyDef or StageDef or HeroClassDef or WeaponDef or MusicDef;

    private static IEnumerable<IDef> Owners(GameDefs defs) =>
        defs.Enemies.All.Cast<IDef>().Concat(defs.Stages.All).Concat(defs.HeroClasses.All).Concat(defs.Weapons.All).Concat(defs.Music.All);

    private static IEnumerable<object> AllNonOwnerDefs(GameDefs defs) =>
        defs.Sprites.All.Cast<object>().Concat(defs.SpriteSets.All).Concat(defs.Effects.All).Concat(defs.Projectiles.All)
            .Concat(defs.Pickups.All).Concat(defs.Passives.All).Concat(defs.Rewards.All).Concat(defs.Buffs.All).Concat(defs.Affixes.All)
            .Concat(defs.DropTables.All).Concat(defs.Sounds.All).Concat(defs.Evolutions.All).Concat(OwnerlessRules(defs));

    /// <summary>규칙이 가리키는 정의(필러 카드 아이콘 등 — 기본 내용).</summary>
    private static IEnumerable<object> OwnerlessRules(GameDefs defs)
    {
        var c = new Collector(defs);
        DefWalker.Walk(defs.Rules, "$", c);
        return c.Found.Where(x => !IsOwnerType(x));
    }

    private static IEnumerable<MapDef> StageMaps(StageDef s)
    {
        foreach (var m in s.RoomMaps.Normal) if (m.IsBound) yield return m.Def;
        foreach (var m in s.RoomMaps.Elite) if (m.IsBound) yield return m.Def;
        if (s.RoomMaps.Boss is { IsBound: true } boss) yield return boss.Def;
    }

    /// <summary>D 에서 닿는 비 D 정의(D 자신 제외, 다른 D 에서 멈춤). 맵·타일셋은 스테이지가 따로 다룬다.</summary>
    private static HashSet<object> Reach(GameDefs defs, IDef root)
    {
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var queue = new Queue<object>();
        void Visit(object o)
        {
            var c = new Collector(defs);
            DefWalker.Walk(o, "$", c);
            foreach (var x in c.Found)
            {
                if (IsOwnerType(x) || x is MapDef or TilesetDef) continue;
                if (seen.Add(x)) queue.Enqueue(x);
            }
        }
        Visit(root);
        while (queue.Count > 0) Visit(queue.Dequeue());
        return seen;
    }

    private static HashSet<object> ClipSprites(SpriteSetDef set)
    {
        var s = new HashSet<object>(ReferenceEqualityComparer.Instance);
        foreach (ClipKind k in Enum.GetValues<ClipKind>())
            if (set.Clips.Get(k) is { Sprite.IsBound: true } clip) s.Add(clip.Sprite.Def);
        return s;
    }

    /// <summary>정의 하나가 직접 가리키는 파일(그림·소리). <paramref name="variant"/> 는 atlas 페이지 변종.</summary>
    private static IEnumerable<string> FilesOf(object def, string variant)
    {
        switch (def)
        {
            case SpriteDef { Source: SpriteSource.Atlas } atlas:
                yield return SpriteMeta.PageFile(atlas.File, 0, variant);
                yield return SpriteMeta.MetaFile(atlas.File);
                break;
            case SpriteDef grid:
                yield return grid.File;
                break;
            case SoundDef sound:
                foreach (var f in sound.Files) yield return f;
                break;
        }
    }

    /// <summary>D 자신이 파일을 가리키는 경우(음악).</summary>
    private static IEnumerable<string> DirectFiles(IDef d)
    {
        if (d is MusicDef m) yield return m.File;
    }

    /// <summary>객체 안의 DefRef 가 가리키는 정의(연결 안 됐으면 ID 로 찾음, 없으면 건너뜀).</summary>
    private sealed class Collector(GameDefs defs) : DefWalker.IVisitor
    {
        private Dictionary<Type, IReadOnlyDictionary<string, IDef>>? _index;
        public List<object> Found { get; } = [];

        public void DefRef(object defRef, Type target, string path)
        {
            var t = defRef.GetType();
            if ((bool)t.GetProperty("IsBound")!.GetValue(defRef)!)
            {
                Found.Add(t.GetProperty("Def")!.GetValue(defRef)!);
                return;
            }
            _index ??= DefKinds.Index(defs);
            var id = (string)t.GetProperty("Id")!.GetValue(defRef)!;
            if (_index.TryGetValue(target, out var table) && table.TryGetValue(id, out var d)) Found.Add(d);
        }

        public void Part(PartDef part, string path) { }
        public void PartTypeName(PartKind kind, string type, string path) { }
    }
}
