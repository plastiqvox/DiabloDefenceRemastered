using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using DiabloDefenceRemastered.Core.Data.Json;

namespace DiabloDefenceRemastered.Core.Assets;

/// <summary>
/// 가져오기 표 <c>tools/DiabloAssetImporter/Table/*.json</c> 8개의 모양과 "만들 이름 목록" (설계서 1.4, 계약 WP1b, 표 파일은 WP0 소유).
/// Core 가 표 전체를 <b>엄격하게</b>(모르는 속성 오류, <see cref="DefJson"/>) 읽고 도구는 이 record 를 그대로 쓴다(도구가 따로 파싱하지 않는다).
/// 13.5-10 ① 시험은 같은 record 로 이름 목록을 만든다. 표에는 원작 바이트·프레임 수·높이가 없다(도구가 파일에서 읽는다).
/// <list type="bullet">
/// <item>공통: <c>out</c>(출력 이름, <c>d1/</c> 로 시작, 소문자·숫자·밑줄), 원작 경로 틀(<c>{a}</c> = 동작 글자, <c>{d}</c> = 방향 파일 1..dirs),
/// <c>width</c>(대조용 칸 폭), <c>frames</c>([처음, 끝], 1부터), <c>shadow_alpha</c>(표 머리 → 항목 → 동작 순으로 덮어씀, 없으면 색인 0 을 검정 그대로),
/// <c>variants</c>(이름 → TRN 경로, <c>""</c> = 원색), <c>optional</c>(<c>--hellfire</c> 로만 찾는 항목 — 없으면 건너뜀).</item>
/// <item>몬스터·주인공 동작 값 = 글자 하나 또는 객체 <c>{ file, cl2, width, frames, shadow_alpha }</c>(덮어쓰기).</item>
/// </list>
/// </summary>
public sealed record ImportTable
{
    /// <summary>표 파일 8개(이름 순은 <see cref="ComputeHash(string)"/> 가 정한다).</summary>
    public static readonly IReadOnlyList<string> FileNames =
        ["monsters.json", "hero.json", "missiles.json", "icons.json", "sounds.json", "music.json", "exe.json", "tilesets.json"];

    public required MonstersTable Monsters { get; init; }
    public required HeroTable Hero { get; init; }
    public required MissilesTable Missiles { get; init; }
    public required IconsTable Icons { get; init; }
    public required SoundsTable Sounds { get; init; }
    public required MusicTable Music { get; init; }
    public required ExeTable Exe { get; init; }
    public required TilesetsTable Tilesets { get; init; }

    // ── 읽기 ──────────────────────────────────────────────────────────

    /// <summary>폴더의 표 8개를 읽고 검사한다. 오류의 <c>File</c> 은 표 파일 이름.</summary>
    public static ImportTableResult Load(string tableDir)
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var name in FileNames)
        {
            var path = Path.Combine(tableDir, name);
            if (File.Exists(path)) files[name] = File.ReadAllText(path);
        }
        return Read(files);
    }

    /// <summary>표 파일 이름 → 내용. 없는 파일·문법·모양 오류를 모두 모은다. 하나라도 형으로 못 바꾸면 <c>Table</c> 은 null.</summary>
    public static ImportTableResult Read(IReadOnlyDictionary<string, string> files)
    {
        var errors = new List<DataError>();
        T? Parse<T>(string name) where T : class
        {
            if (!files.TryGetValue(name, out var text)) { errors.Add(new DataError(name, "$", "표 파일이 없다")); return null; }
            try
            {
                // 형으로 바로 읽으면 중복 키는 마지막 값이 조용히 이긴다 → 먼저 오류로 낸다(정의 쪽 MapDataReader 와 같은 규칙)
                if (DefJson.FindDuplicateKey(text) is { } dup) { errors.Add(new DataError(name, dup, $"키가 두 번 있다: {DefJson.LastKey(dup)}")); return null; }
                return JsonSerializer.Deserialize<T>(text, DefJson.Options) ?? throw new JsonException("null");
            }
            catch (JsonException ex) { errors.Add(new DataError(name, ex.Path ?? "$", ex.Message)); return null; }
        }

        var monsters = Parse<MonstersTable>("monsters.json");
        var hero = Parse<HeroTable>("hero.json");
        var missiles = Parse<MissilesTable>("missiles.json");
        var icons = Parse<IconsTable>("icons.json");
        var sounds = Parse<SoundsTable>("sounds.json");
        var music = Parse<MusicTable>("music.json");
        var exe = Parse<ExeTable>("exe.json");
        var tilesets = Parse<TilesetsTable>("tilesets.json");
        if (monsters is null || hero is null || missiles is null || icons is null || sounds is null || music is null || exe is null || tilesets is null)
            return new ImportTableResult(null, errors);

        var table = new ImportTable
        {
            Monsters = monsters, Hero = hero, Missiles = missiles, Icons = icons,
            Sounds = sounds, Music = music, Exe = exe, Tilesets = tilesets,
        };
        errors.AddRange(table.Validate());
        return new ImportTableResult(table, errors);
    }

    /// <summary>
    /// 표 해시: <see cref="FileNames"/> 8개를 파일 이름 서수 순으로 이은 바이트의 SHA-256(소문자 16진). 도구는 <c>Table/</c>, 게임은 출력 <c>ImportTable\</c> 에 같은 함수를 쓴다.
    /// 폴더의 다른 <c>*.json</c> 은 보지 않는다 — 게임 출력은 PreserveNewest 복사라 지우거나 이름을 바꾼 옛 파일이 남는다. 없는 파일은 빈 바이트.
    /// </summary>
    public static string ComputeHash(string tableDir)
    {
        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var name in FileNames.Order(StringComparer.Ordinal))
        {
            var path = Path.Combine(tableDir, name);
            if (File.Exists(path)) sha.AppendData(File.ReadAllBytes(path));
        }
        return Convert.ToHexStringLower(sha.GetHashAndReset());
    }

    // ── 검사 ──────────────────────────────────────────────────────────

    /// <summary>모양 검사(이름 규칙·틀 자리표시·범위·겹치는 출력 이름). 문법·속성 오류는 읽을 때 이미 잡힌다.</summary>
    public IReadOnlyList<DataError> Validate()
    {
        var v = new Checker();
        CheckUnits(v, "monsters.json", "monsters", Monsters.Palette, Monsters.ShadowAlpha, Monsters.Monsters);
        CheckUnits(v, "hero.json", "hero", Hero.Palette, Hero.ShadowAlpha, Hero.Hero);

        const string mf = "missiles.json";
        v.Palette(mf, Missiles.Palette);
        v.Alpha(mf, "$.shadow_alpha", Missiles.ShadowAlpha);
        for (var i = 0; i < Missiles.Missiles.Count; i++)
        {
            var e = Missiles.Missiles[i];
            var p = $"$.missiles[{i}]";
            v.Out(mf, p, e.Out);
            v.Width(mf, $"{p}.width", e.Width);
            v.Frames(mf, $"{p}.frames", e.Frames);
            v.Alpha(mf, $"{p}.shadow_alpha", e.ShadowAlpha);
            v.Variants(mf, $"{p}.variants", e.Variants);
            v.Dirs(mf, p, e.Cl2, e.Dirs, e.FramesAreDirs);
        }

        const string icf = "icons.json";
        if (Icons.Size <= 0) v.Add(icf, "$.size", $"아이콘 크기 {Icons.Size} 는 1 이상");
        if (Icons.Palette is not null) v.Palette(icf, Icons.Palette);
        for (var i = 0; i < Icons.Icons.Count; i++)
        {
            var e = Icons.Icons[i];
            var p = $"$.icons[{i}]";
            v.Out(icf, p, e.Out);
            v.Width(icf, $"{p}.width", e.Width);
            v.Alpha(icf, $"{p}.shadow_alpha", e.ShadowAlpha);
            v.Dirs(icf, p, e.Cl2, e.Dirs, e.FramesAreDirs);
            if (e.Layers.Count == 0) v.Add(icf, $"{p}.layers", "겹칠 그림(layers)이 하나도 없다");
            for (var k = 0; k < e.Layers.Count; k++)
            {
                var l = e.Layers[k];
                if ((uint)l.Dir >= (uint)e.Directions) v.Add(icf, $"{p}.layers[{k}].dir", $"방향 {l.Dir} 는 0..{e.Directions - 1}(0부터)");
                if (l.Frame < 1) v.Add(icf, $"{p}.layers[{k}].frame", $"프레임 {l.Frame} — 프레임 번호는 1부터");
                if (e.FramesAreDirs is not null && l.Frame != 1) v.Add(icf, $"{p}.layers[{k}].frame", "frames_are_dirs 그림은 방향마다 프레임이 1개다");
            }
        }

        const string sf = "sounds.json";
        for (var i = 0; i < Sounds.Sounds.Count; i++)
        {
            var e = Sounds.Sounds[i];
            var p = $"$.sounds[{i}]";
            var holes = Placeholders(e.Path);
            var keys = e.Vars?.Keys.ToHashSet(StringComparer.Ordinal) ?? new HashSet<string>(StringComparer.Ordinal);
            if (!holes.SetEquals(keys))
                v.Add(sf, $"{p}.path", $"틀의 자리표시({string.Join(",", holes.Order())})와 vars 키({string.Join(",", keys.Order())})가 다르다");
            if (e.Vars is not null)
                foreach (var (k, values) in e.Vars)
                    if (values.Count == 0 || values.Any(s => s.Length == 0 || s.Contains('\\') || s.Contains('/')))
                        v.Add(sf, $"{p}.vars.{k}", "vars 값은 비지 않은 목록이고 경로 구분자를 갖지 않는다");
            v.SourcePath(sf, $"{p}.path", e.Path);
        }
        const string muf = "music.json";
        for (var i = 0; i < Music.Music.Count; i++)
        {
            v.SourcePath(muf, $"$.music[{i}].path", Music.Music[i].Path);
            if (Placeholders(Music.Music[i].Path).Count > 0) v.Add(muf, $"$.music[{i}].path", "음악 경로에는 자리표시를 쓰지 않는다");
        }

        const string ef = "exe.json";
        var exeSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < Exe.Files.Count; i++)
        {
            var f = Exe.Files[i];
            if (f.Length == 0 || f.Contains('/') || f.Contains('\\') ||
                !(f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".cur", StringComparison.OrdinalIgnoreCase)))
                v.Add(ef, $"$.files[{i}]", $"'{f}' — 원작 exe 파일 이름은 폴더 없는 .png·.cur");
            if (!exeSeen.Add(f)) v.Add(ef, $"$.files[{i}]", $"'{f}' 가 두 번 있다");
        }

        const string tf = "tilesets.json";
        var tsSeen = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < Tilesets.Tilesets.Count; i++)
        {
            var t = Tilesets.Tilesets[i];
            var p = $"$.tilesets[{i}]";
            if (!IsName(t.Id)) v.Add(tf, $"{p}.id", $"타일셋 ID '{t.Id}' 는 소문자·숫자·밑줄");
            if (!tsSeen.Add(t.Id)) v.Add(tf, $"{p}.id", $"타일셋 '{t.Id}' 가 두 번 있다");
            foreach (var (prop, value) in new[] { ("dir", t.Dir), ("cel", t.Cel), ("min", t.Min), ("til", t.Til), ("sol", t.Sol) })
                if (string.IsNullOrWhiteSpace(value)) v.Add(tf, $"{p}.{prop}", "비어 있다");
            if (t.PiecesPerSubtile <= 0 || t.PiecesPerSubtile % 2 != 0) v.Add(tf, $"{p}.pieces_per_subtile", $"조각 수 {t.PiecesPerSubtile} 는 양의 짝수(L1 10, L4 16)");
            if (t.Palettes.Count == 0) v.Add(tf, $"{p}.palettes", "팔레트가 하나도 없다");
            if (t.Overlay is { } o)
            {
                if (string.IsNullOrWhiteSpace(o.Cel)) v.Add(tf, $"{p}.overlay.cel", "비어 있다");
                foreach (var (key, list) in o.Frames)
                    if (list.Count == 0) v.Add(tf, $"{p}.overlay.frames.{key}", "번호 목록이 비었다");
            }
        }

        // 출력 이름이 겹치면 두 항목이 같은 파일을 덮는다
        var owners = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (name, owner) in NamedOutputs())
            if (!owners.TryAdd(name, owner))
                v.Add(owner.Split(' ')[0], owner.Split(' ')[1], $"출력 이름 '{name}' 를 {owners[name]} 도 만든다");

        return v.Errors;
    }

    private static void CheckUnits(Checker v, string file, string list, string palette, float? headAlpha, IReadOnlyList<ImportUnitEntry> entries)
    {
        v.Palette(file, palette);
        v.Alpha(file, "$.shadow_alpha", headAlpha);
        for (var i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            var p = $"$.{list}[{i}]";
            v.Out(file, p, e.Out);
            v.Width(file, $"{p}.width", e.Width);
            v.Frames(file, $"{p}.frames", e.Frames);
            v.Alpha(file, $"{p}.shadow_alpha", e.ShadowAlpha);
            v.Variants(file, $"{p}.variants", e.Variants);
            var holes = Placeholders(e.Cl2);
            if (holes.Any(h => h != "a")) v.Add(file, $"{p}.cl2", $"몬스터·주인공 틀의 자리표시는 {{a}} 뿐이다('{e.Cl2}')");
            if (e.Actions.Count == 0) v.Add(file, $"{p}.actions", "동작이 하나도 없다");
            foreach (var (name, a) in e.Actions)
            {
                var ap = $"{p}.actions.{name}";
                if (!IsName(name)) v.Add(file, ap, $"동작 이름 '{name}' 는 소문자·숫자·밑줄");
                if (a.Cl2 is not null)
                {
                    if (a.File is not null) v.Add(file, ap, "file 과 cl2 중 하나만 쓴다");
                    if (Placeholders(a.Cl2).Count > 0) v.Add(file, $"{ap}.cl2", "동작의 cl2 는 자리표시 없는 완전한 경로다");
                    v.SourcePath(file, $"{ap}.cl2", a.Cl2);
                }
                else if (a.File is null || !IsName(a.File)) v.Add(file, ap, "동작 글자(file) 또는 cl2 가 있어야 한다");
                else if (!holes.Contains("a")) v.Add(file, $"{p}.cl2", $"동작 '{name}' 가 글자를 쓰는데 틀에 {{a}} 가 없다");
                if (a.Width is { } w) v.Width(file, $"{ap}.width", w);
                v.Frames(file, $"{ap}.frames", a.Frames);
                v.Alpha(file, $"{ap}.shadow_alpha", a.ShadowAlpha);
            }
        }
    }

    // ── 만들 이름 목록 (13.5-10 ①) ────────────────────────────────────

    /// <summary>
    /// 표가 만드는 <c>d1/</c> 아래 파일 이름(Assets 뿌리 기준, 서수 순): 그림 묶음·마법 = 변종마다 첫 페이지 + 메타 하나, 아이콘 = PNG,
    /// 소리·음악 = <c>d1/sound/</c> + 원작 경로 소문자(<c>vars</c> 펼침) + 소리 색인. 맵 그림은 <see cref="AllNames"/> 가 배치로 계산한다.
    /// 둘째 페이지부터(<c>_p2</c> …)는 가져오기 뒤에야 알 수 있어 목록에 없다 — 정의는 첫 페이지와 메타만 가리키고, 나머지는 메타의 <c>pages</c> 가 안다.
    /// <c>optional</c> 항목도 넣는다(만들 수 있는 이름의 목록).
    /// </summary>
    public IReadOnlyList<string> OutputNames()
    {
        var set = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var (name, _) in NamedOutputs()) set.Add(name);
        return [.. set];
    }

    /// <summary>원작 exe 그림 이름(<c>Table/exe.json</c>, Assets 뿌리). 13.5-10 은 "표가 만드는 이름 + 이것".</summary>
    public IReadOnlyList<string> ExeNames => Exe.Files;

    /// <summary>표 이름 + exe 이름 + 맵 그림 이름(바닥 조각 이름 규칙) — 정의가 가리킬 수 있는 파일 전부(13.5-10).</summary>
    public IReadOnlyList<string> AllNames(IEnumerable<ArenaGrid> arenas)
    {
        var set = new SortedSet<string>(OutputNames(), StringComparer.Ordinal);
        foreach (var f in Exe.Files) set.Add(f);
        foreach (var grid in arenas)
            foreach (var n in ArenaArt.OutputNames(grid)) set.Add(n);
        return [.. set];
    }

    /// <summary>맵 정의가 쓰는 타일셋·팔레트가 <c>tilesets.json</c> 에 있는지(도구가 그림을 구울 수 있나).</summary>
    public IReadOnlyList<string> CheckMaps(IEnumerable<MapDef> maps)
    {
        var problems = new List<string>();
        foreach (var map in maps)
        {
            var t = Tilesets.Tilesets.FirstOrDefault(x => x.Id == map.Tileset.Id);
            if (t is null) problems.Add($"맵 '{map.Id}' 의 타일셋 '{map.Tileset.Id}' 가 tilesets.json 에 없다");
            else if (!t.Palettes.ContainsKey(map.Palette)) problems.Add($"맵 '{map.Id}' 의 팔레트 '{map.Palette}' 가 tilesets.json '{t.Id}' palettes 에 없다");
        }
        return problems;
    }

    private IEnumerable<(string Name, string Owner)> NamedOutputs()
    {
        IEnumerable<(string, string)> Sheet(string file, string path, string outName, IReadOnlyDictionary<string, string?> variants)
        {
            var page = outName + ".png";
            foreach (var variant in variants.Keys.Order(StringComparer.Ordinal))
                yield return (SpriteMeta.PageFile(page, 0, variant), $"{file} {path}");
            yield return (SpriteMeta.MetaFile(page), $"{file} {path}");
        }

        for (var i = 0; i < Monsters.Monsters.Count; i++)
            foreach (var x in Sheet("monsters.json", $"$.monsters[{i}]", Monsters.Monsters[i].Out, Monsters.Monsters[i].Variants)) yield return x;
        for (var i = 0; i < Hero.Hero.Count; i++)
            foreach (var x in Sheet("hero.json", $"$.hero[{i}]", Hero.Hero[i].Out, Hero.Hero[i].Variants)) yield return x;
        for (var i = 0; i < Missiles.Missiles.Count; i++)
            foreach (var x in Sheet("missiles.json", $"$.missiles[{i}]", Missiles.Missiles[i].Out, Missiles.Missiles[i].Variants)) yield return x;
        for (var i = 0; i < Icons.Icons.Count; i++)
            yield return (Icons.Icons[i].Out + ".png", $"icons.json $.icons[{i}]");

        var anySound = false;
        for (var i = 0; i < Sounds.Sounds.Count; i++)
            foreach (var src in Sounds.Sounds[i].SourcePaths())
            {
                anySound = true;
                yield return (AssetFormat.SoundOutput(src), $"sounds.json $.sounds[{i}]");
            }
        for (var i = 0; i < Music.Music.Count; i++)
        {
            anySound = true;
            yield return (AssetFormat.SoundOutput(Music.Music[i].Path), $"music.json $.music[{i}]");
        }
        if (anySound) yield return (AssetFormat.SoundIndexPath, "sounds.json $");
    }

    // ── 도우미 ────────────────────────────────────────────────────────

    /// <summary>틀 안의 자리표시 이름(<c>{a}</c> → <c>a</c>).</summary>
    public static HashSet<string> Placeholders(string template)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        var i = 0;
        while ((i = template.IndexOf('{', i)) >= 0)
        {
            var j = template.IndexOf('}', i + 1);
            if (j < 0) { set.Add(""); break; }
            set.Add(template[(i + 1)..j]);
            i = j + 1;
        }
        return set;
    }

    internal static bool IsName(string s)
    {
        if (s.Length == 0) return false;
        foreach (var c in s)
            if (!(c is >= 'a' and <= 'z' or >= '0' and <= '9' or '_')) return false;
        return true;
    }

    private sealed class Checker
    {
        public List<DataError> Errors { get; } = [];

        public void Add(string file, string path, string message) => Errors.Add(new DataError(file, path, message));

        public void Out(string file, string path, string outName)
        {
            var ok = outName.StartsWith("d1/", StringComparison.Ordinal) && outName.Length > 3;
            if (ok)
                foreach (var part in outName[3..].Split('/'))
                    if (!IsName(part)) { ok = false; break; }
            if (!ok) Add(file, $"{path}.out", $"출력 이름 '{outName}' 는 d1/ 로 시작하고 조각마다 소문자·숫자·밑줄이다");
        }

        public void Palette(string file, string palette)
        {
            if (string.IsNullOrWhiteSpace(palette)) Add(file, "$.palette", "팔레트 경로가 비었다");
        }

        public void Width(string file, string path, int width)
        {
            if (width <= 0) Add(file, path, $"칸 폭 {width} 는 1 이상");
        }

        public void Frames(string file, string path, int[]? frames)
        {
            if (frames is null) return;
            if (frames.Length != 2 || frames[0] < 1 || frames[1] < frames[0])
                Add(file, path, "frames 는 [처음, 끝](1부터, 처음 ≤ 끝)");
        }

        public void Alpha(string file, string path, float? alpha)
        {
            if (alpha is { } a && (a < 0f || a > 1f)) Add(file, path, $"그림자 알파 {a} 는 0..1");
        }

        public void Variants(string file, string path, IReadOnlyDictionary<string, string?> variants)
        {
            if (variants.Count == 0) Add(file, path, "변종이 하나도 없다(원색은 \"\": null)");
            foreach (var (name, trn) in variants)
            {
                if (name.Length == 0)
                {
                    if (trn is not null) Add(file, $"{path}[\"\"]", "원색 \"\" 의 TRN 은 null 이다");
                }
                else if (!IsName(name)) Add(file, $"{path}.{name}", $"변종 이름 '{name}' 는 소문자·숫자·밑줄");
                else if (string.IsNullOrWhiteSpace(trn)) Add(file, $"{path}.{name}", "TRN 경로가 비었다");
            }
        }

        public void Dirs(string file, string path, string cl2, int? dirs, int? framesAreDirs)
        {
            var holes = Placeholders(cl2);
            if (dirs is not null && framesAreDirs is not null) Add(file, path, "dirs 와 frames_are_dirs 중 하나만 쓴다");
            if (dirs is { } d)
            {
                if (d <= 0) Add(file, $"{path}.dirs", $"방향 수 {d} 는 1 이상");
                if (!holes.SetEquals(["d"])) Add(file, $"{path}.cl2", $"방향 파일 틀에는 {{d}} 하나만 쓴다('{cl2}')");
            }
            else if (holes.Count > 0) Add(file, $"{path}.cl2", $"dirs 가 없으면 자리표시를 쓰지 않는다('{cl2}')");
            if (framesAreDirs is <= 0) Add(file, $"{path}.frames_are_dirs", "방향 수는 1 이상");
            SourcePath(file, $"{path}.cl2", cl2);
        }

        public void SourcePath(string file, string path, string source)
        {
            if (string.IsNullOrWhiteSpace(source) || source.Contains('/') || source.StartsWith('\\'))
                Add(file, path, $"원작 경로 '{source}' 는 역슬래시 구분 상대 경로다(MPQ 안)");
        }
    }
}

/// <summary>표 읽기 결과. 형으로 못 바꾼 파일이 있으면 <see cref="Table"/> 이 null.</summary>
public sealed record ImportTableResult(ImportTable? Table, IReadOnlyList<DataError> Errors)
{
    public bool HasErrors => Errors.Count > 0;
}

// ── 표 파일 8개 ───────────────────────────────────────────────────────

/// <summary><c>monsters.json</c> — 표 머리 값은 항목 기본값, 항목·동작이 덮어쓴다.</summary>
public sealed record MonstersTable
{
    public required string Palette { get; init; }
    public float? ShadowAlpha { get; init; }
    public IReadOnlyList<ImportUnitEntry> Monsters { get; init; } = [];
}

/// <summary><c>hero.json</c> — 마법사 <c>slt</c> 처럼 동작마다 폭·파일이 다르면 동작 객체에 적는다.</summary>
public sealed record HeroTable
{
    public required string Palette { get; init; }
    public float? ShadowAlpha { get; init; }
    public IReadOnlyList<ImportUnitEntry> Hero { get; init; } = [];
}

/// <summary><c>missiles.json</c> — shadow_alpha 를 적은 항목만 색인 0 을 그림자로 바꾼다.</summary>
public sealed record MissilesTable
{
    public required string Palette { get; init; }
    public float? ShadowAlpha { get; init; }
    public IReadOnlyList<ImportMissileEntry> Missiles { get; init; } = [];
}

/// <summary><c>icons.json</c> — 아이콘 = 마법 프레임 한 장 이상을 겹친 size×size, 어두운 바탕.</summary>
public sealed record IconsTable
{
    public int Size { get; init; } = 48;
    public Rgb Background { get; init; }
    /// <summary>선택 키. 없으면 <c>missiles.json</c> 의 palette(설계서 1.4).</summary>
    public string? Palette { get; init; }
    public IReadOnlyList<ImportIconEntry> Icons { get; init; } = [];
}

/// <summary><c>sounds.json</c> — <c>{키}</c> 는 vars 의 값마다 펼친다. 출력 = <c>d1/sound/</c> + 원작 경로 소문자·'/'.</summary>
public sealed record SoundsTable
{
    public IReadOnlyList<ImportSoundEntry> Sounds { get; init; } = [];
}

public sealed record MusicTable
{
    public IReadOnlyList<ImportMusicEntry> Music { get; init; } = [];
}

/// <summary><c>exe.json</c> — 원작 exe PNG 49장·커서의 이름(이름 대조용, 그림 없음).</summary>
public sealed record ExeTable
{
    public IReadOnlyList<string> Files { get; init; } = [];
}

/// <summary><c>tilesets.json</c> — 타일셋 원본 정보(도구만 씀). 게임 정의 <c>Data/tilesets</c> 의 id 와 같은 키.</summary>
public sealed record TilesetsTable
{
    public IReadOnlyList<ImportTilesetEntry> Tilesets { get; init; } = [];
}

// ── 항목 ──────────────────────────────────────────────────────────────

/// <summary>몬스터·주인공 그림 묶음 하나 → 페이지(변종마다) + 메타 하나.</summary>
public sealed record ImportUnitEntry
{
    /// <summary>출력 이름(<c>d1/monsters/zombie</c>) — <c>.png</c>·<c>@변종</c>·<c>.json</c> 은 규칙이 붙인다.</summary>
    public required string Out { get; init; }
    /// <summary>원작 경로 틀(<c>monsters\zombie\zombie{a}.cl2</c>).</summary>
    public required string Cl2 { get; init; }
    /// <summary>대조용 칸 폭(도구는 파일에서 추정한 폭을 쓰고, 다르면 종료 코드 4).</summary>
    public required int Width { get; init; }
    public int[]? Frames { get; init; }
    public float? ShadowAlpha { get; init; }
    public IReadOnlyDictionary<string, string?> Variants { get; init; } = new Dictionary<string, string?> { [""] = null };
    public bool Optional { get; init; }
    /// <summary>동작 이름(그림 정의의 <c>action</c>) → 원작 글자 또는 덮어쓰기 객체.</summary>
    public required IReadOnlyDictionary<string, ImportAction> Actions { get; init; }

    /// <summary>동작 하나를 푼다: 경로(<c>{a}</c> 채움 또는 동작의 cl2), 폭 = 동작 → 항목, 프레임 범위 = 동작 → 항목, 그림자 = 동작 → 항목 → 표 머리.</summary>
    public ResolvedImportAction Resolve(string action, float? headShadowAlpha)
    {
        var a = Actions[action];
        var path = a.Cl2 ?? Cl2.Replace("{a}", a.File, StringComparison.Ordinal);
        return new ResolvedImportAction(action, path, a.Width ?? Width, a.Frames ?? Frames, a.ShadowAlpha ?? ShadowAlpha ?? headShadowAlpha);
    }

    /// <summary>모든 동작(이름 서수 순 — 묶기 순서와 같음).</summary>
    public IReadOnlyList<ResolvedImportAction> ResolveAll(float? headShadowAlpha) =>
        [.. Actions.Keys.Order(StringComparer.Ordinal).Select(k => Resolve(k, headShadowAlpha))];
}

/// <summary>동작 값. JSON 은 글자 하나(<c>"w"</c>) 또는 객체 <c>{ "file", "cl2", "width", "frames", "shadow_alpha" }</c>.</summary>
[JsonConverter(typeof(ImportActionJsonConverter))]
public sealed record ImportAction
{
    /// <summary>원작 동작 글자(틀의 <c>{a}</c> 자리).</summary>
    public string? File { get; init; }
    /// <summary>틀 대신 쓸 완전한 경로(주인공 죽기 <c>plrgfx\sorceror\sln\slndt.cl2</c>).</summary>
    public string? Cl2 { get; init; }
    public int? Width { get; init; }
    public int[]? Frames { get; init; }
    public float? ShadowAlpha { get; init; }
}

public sealed class ImportActionJsonConverter : JsonConverter<ImportAction>
{
    public override ImportAction Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String) return new ImportAction { File = reader.GetString() };
        if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("동작 값은 글자 하나 또는 객체다");
        var shape = JsonSerializer.Deserialize<ActionShape>(ref reader, options) ?? throw new JsonException("동작 값이 null 이다");
        return new ImportAction { File = shape.File, Cl2 = shape.Cl2, Width = shape.Width, Frames = shape.Frames, ShadowAlpha = shape.ShadowAlpha };
    }

    public override void Write(Utf8JsonWriter writer, ImportAction value, JsonSerializerOptions options)
    {
        if (value is { Cl2: null, Width: null, Frames: null, ShadowAlpha: null, File: { } f }) { writer.WriteStringValue(f); return; }
        JsonSerializer.Serialize(writer, new ActionShape
        {
            File = value.File, Cl2 = value.Cl2, Width = value.Width, Frames = value.Frames, ShadowAlpha = value.ShadowAlpha,
        }, options);
    }

    private sealed record ActionShape
    {
        public string? File { get; init; }
        public string? Cl2 { get; init; }
        public int? Width { get; init; }
        public int[]? Frames { get; init; }
        public float? ShadowAlpha { get; init; }
    }
}

/// <summary>푼 동작: 원작 경로·대조 폭·쓸 프레임 범위·그림자 알파(null = 색인 0 을 검정 그대로).</summary>
public sealed record ResolvedImportAction(string Name, string SourcePath, int Width, int[]? Frames, float? ShadowAlpha);

/// <summary>마법 하나 → 페이지 + 메타. 방향 파일 16개(<c>{d}</c>)·프레임이 방향(<c>frames_are_dirs</c>)·방향 없음(1).</summary>
public sealed record ImportMissileEntry
{
    public required string Out { get; init; }
    public required string Cl2 { get; init; }
    public required int Width { get; init; }
    /// <summary>방향 파일 수(<c>{d}</c> = 1..dirs, 1번 파일 = 아래 = 방향 0, 시계 방향).</summary>
    public int? Dirs { get; init; }
    /// <summary>파일 하나의 프레임 n → 방향 n−1(<c>arrows</c>).</summary>
    public int? FramesAreDirs { get; init; }
    public int[]? Frames { get; init; }
    public float? ShadowAlpha { get; init; }
    public IReadOnlyDictionary<string, string?> Variants { get; init; } = new Dictionary<string, string?> { [""] = null };
    public bool Optional { get; init; }

    public int Directions => Dirs ?? FramesAreDirs ?? 1;

    /// <summary>읽을 원작 파일(방향 파일이면 1..dirs 순 — 묶어 한 페이지가 된다).</summary>
    public IReadOnlyList<string> SourcePaths() => ExpandDirs(Cl2, Dirs);

    internal static IReadOnlyList<string> ExpandDirs(string cl2, int? dirs) =>
        dirs is { } d
            ? [.. Enumerable.Range(1, d).Select(i => cl2.Replace("{d}", i.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal))]
            : [cl2];
}

/// <summary>아이콘 하나 → <c>out.png</c>. <c>layers</c> 는 그리는 순서.</summary>
public sealed record ImportIconEntry
{
    public required string Out { get; init; }
    public required string Cl2 { get; init; }
    public required int Width { get; init; }
    public int? Dirs { get; init; }
    public int? FramesAreDirs { get; init; }
    public float? ShadowAlpha { get; init; }
    public required IReadOnlyList<ImportIconLayer> Layers { get; init; }
    public bool Optional { get; init; }

    public int Directions => Dirs ?? FramesAreDirs ?? 1;

    public IReadOnlyList<string> SourcePaths() => ImportMissileEntry.ExpandDirs(Cl2, Dirs);
}

/// <summary>아이콘 겹침 한 장: <c>dir</c> = 방향(<b>0부터</b>, 1.4 마법 규칙의 방향 번호), <c>frame</c> = 프레임(<b>1부터</b>), <c>offset</c> 정수 이동, <c>rotate</c> 도.</summary>
public sealed record ImportIconLayer
{
    public int Dir { get; init; }
    public required int Frame { get; init; }
    public Int2? Offset { get; init; }
    public float? Rotate { get; init; }
}

/// <summary>소리 하나 또는 vars 로 펼치는 여럿.</summary>
public sealed record ImportSoundEntry
{
    /// <summary>원작 경로 틀(<c>monsters\zombie\zombie{k}{i}.wav</c>).</summary>
    public required string Path { get; init; }
    /// <summary>자리표시 → 값 목록. 펼침 = 키 서수 순 곱(앞 키가 바깥 고리).</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>>? Vars { get; init; }
    public bool Optional { get; init; }

    public IReadOnlyList<string> SourcePaths()
    {
        var results = new List<string> { Path };
        if (Vars is null) return results;
        foreach (var key in Vars.Keys.Order(StringComparer.Ordinal))
        {
            var next = new List<string>(results.Count * Vars[key].Count);
            foreach (var partial in results)
                foreach (var value in Vars[key])
                    next.Add(partial.Replace("{" + key + "}", value, StringComparison.Ordinal));
            results = next;
        }
        return results;
    }
}

public sealed record ImportMusicEntry
{
    public required string Path { get; init; }
    public bool Optional { get; init; }
}

/// <summary>타일셋 원본: 폴더·CEL·MIN·TIL·SOL·하위 타일 조각 수·팔레트 이름 → 파일, 아치 덧그림(L1S) 번호표.</summary>
public sealed record ImportTilesetEntry
{
    public required string Id { get; init; }
    /// <summary>MPQ 안 폴더(<c>levels\l1data</c>).</summary>
    public required string Dir { get; init; }
    public required string Cel { get; init; }
    public required string Min { get; init; }
    public required string Til { get; init; }
    public required string Sol { get; init; }
    /// <summary>하위 타일 하나의 MIN 조각 수(L1·L2·L3 10, 마을·L4 16).</summary>
    public int PiecesPerSubtile { get; init; } = 10;
    /// <summary>팔레트 이름(맵 정의 <c>palette</c>) → 폴더 안 파일.</summary>
    public IReadOnlyDictionary<string, string> Palettes { get; init; } = new Dictionary<string, string>();
    public ImportOverlayDef? Overlay { get; init; }

    /// <summary>폴더 안 파일의 MPQ 경로.</summary>
    public string SourcePath(string file) => Dir.TrimEnd('\\') + "\\" + file;
}

/// <summary>아치 덧그림: 덧그림 CEL 과 (덧그림 프레임 → 그 프레임을 덮을 조각 번호 목록). 출처: DevilutionX 참고(부록 A.5).</summary>
public sealed record ImportOverlayDef
{
    public required string Cel { get; init; }
    public IReadOnlyDictionary<int, IReadOnlyList<int>> Frames { get; init; } = new Dictionary<int, IReadOnlyList<int>>();
}
