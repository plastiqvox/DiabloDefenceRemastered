using System.Text.Json;
using System.Text.Json.Nodes;
using DiabloDefenceRemastered.Core.Sim.Run;

namespace DiabloDefenceRemastered.Core.Save;

/// <summary>
/// 저장 본문 (설계서 8.3, WP7): <c>save.json</c>(기록·금화·설정·마지막 직업)과 <c>run.json</c>(계단 저장 칸 하나). 파일 위치는 게임 쪽(<c>SaveStore</c>)이 정해 넘긴다.
/// <list type="bullet">
/// <item>폴더가 null(<see cref="InMemory"/>) — 개발 옵션 실행인데 <c>--save-dir</c> 가 없음: 읽지도 쓰지도 않고 기본값으로 메모리에만(9절 "사용자 데이터 보호").</item>
/// <item>읽기 규칙(v0.7.1): 모르는 키 무시, 기록 한 줄(직업 하나)이 깨지면 그 줄만 버림, 금화·설정은 기록이 깨져도 남김,
/// <c>version</c> 이 지금보다 높으면 읽기 전용(쓰기는 메모리에만), JSON 이 깨지면 <c>save.json.bad</c> 로 옮기고 기본값.</item>
/// <item>깊이: 액트가 지금 사슬에 없거나 <c>rooms_hash</c> 가 지금 표와 다르면 깊이만 비운다(나머지 기록은 남김).</item>
/// <item>쓰기: 임시 파일 → 바꿔치기.</item>
/// <item>계단 저장(v0.8): <c>{ "version": 1, "run": &lt;RunSnapshot&gt; }</c>. 꺼내기(<see cref="TakeRun"/>)는 읽는 순간 파일을 지운다. 깨진 JSON·<c>required</c> 빠짐은 <c>run.json.bad</c>.</item>
/// </list>
/// 로그는 <paramref name="log"/> 로 한 줄씩(<c>SAVE …</c>).
/// </summary>
public sealed class SaveBook
{
    public const string SaveFileName = "save.json";
    public const string RunFileName = "run.json";
    public const string BadSuffix = ".bad";
    /// <summary>계단 저장 파일의 감싸개 판.</summary>
    public const int RunFileVersion = 1;

    private readonly string? _dir;
    private readonly GameDefs _defs;
    private readonly Action<string> _log;
    private SaveData _data;

    private SaveBook(string? dir, GameDefs defs, Action<string>? log)
    {
        _dir = dir;
        _defs = defs;
        _log = log ?? (_ => { });
        _data = new SaveData();
        if (dir is not null) _data = Read();
    }

    /// <summary>파일을 읽지도 쓰지도 않는 저장(개발 옵션 실행).</summary>
    public static SaveBook InMemory(GameDefs defs, Action<string>? log = null) => new(null, defs, log);

    /// <summary><paramref name="dir"/> 의 <c>save.json</c> 을 읽는다(없으면 기본값). 폴더는 처음 쓸 때 만든다.</summary>
    public static SaveBook Open(string dir, GameDefs defs, Action<string>? log = null) => new(dir, defs, log);

    public SaveData Data => _data;
    /// <summary>파일에 쓰는가(메모리 전용·읽기 전용이면 거짓).</summary>
    public bool Persistent => _dir is not null && !ReadOnly;
    /// <summary>파일의 판이 지금보다 높아 이번 실행은 덮어쓰지 않는다.</summary>
    public bool ReadOnly { get; private set; }
    public string? Directory => _dir;
    public string? SavePath => _dir is null ? null : Path.Combine(_dir, SaveFileName);
    public string? RunPath => _dir is null ? null : Path.Combine(_dir, RunFileName);

    public SettingsData Settings => _data.Settings;
    public int TotalGold => _data.TotalGold;
    public string? LastHero => _data.LastHero;

    public void SaveSettings(SettingsData settings)
    {
        _data = _data with { Settings = settings };
        Write();
    }

    public void SetLastHero(string heroClassId)
    {
        _data = _data with { LastHero = heroClassId };
        Write();
    }

    /// <summary>
    /// 한 판 결과: 금화를 더하고, (깊이, 승리, −시간) 비교로 나으면 <c>records.&lt;시작 액트&gt;.&lt;직업&gt;</c> 을 바꾼다. 방 종류·<c>rooms_hash</c> 는 지금 정의에서 계산한다.
    /// 직업은 결과에 실려 온다(늘 <c>sorcerer</c> 를 가정하지 않음).
    /// </summary>
    public void RecordResult(RunResultData r)
    {
        var records = Copy(_data.Records);
        if (!records.TryGetValue(r.StartActId, out var byHero)) records[r.StartActId] = byHero = new Dictionary<string, RecordData>(StringComparer.Ordinal);
        var old = byHero.TryGetValue(r.HeroClassId, out var o) ? o : null;
        var oldDepth = old is null ? (RunDepth?)null : DepthOf(old.BestDepth);
        var better = old is null || RecordCompare.IsBetter(r.Reached, r.Victory, r.RunTime, oldDepth, old.Victory, old.BestTime);
        if (better)
            byHero[r.HeroClassId] = new RecordData
            {
                BestDepth = MakeDepth(r.ReachedActId, r.Reached),
                Victory = r.Victory,
                BestTime = r.RunTime,
                Level = r.Level,
                Kills = r.Kills,
            };
        _data = _data with { TotalGold = _data.TotalGold + Math.Max(0, r.Gold), Records = Freeze(records) };
        Write();
    }

    /// <summary>시작 액트 × 직업 최고 기록. 없으면 null.</summary>
    public RecordView? BestRecord(string startActId, string heroClassId)
    {
        if (!_data.Records.TryGetValue(startActId, out var byHero) || !byHero.TryGetValue(heroClassId, out var rec)) return null;
        return new RecordView(DepthOf(rec.BestDepth), rec.Victory, rec.BestTime, rec.Level, rec.Kills);
    }

    private DepthData MakeDepth(string actId, RunDepth reached)
    {
        var act = _defs.Stages.TryGet(actId, out var a) ? a : null;
        var room = act?.Rooms.FirstOrDefault(x => x.Number == reached.Room);
        return new DepthData
        {
            Act = actId,
            ActNumber = reached.Act,
            Room = reached.Room,
            Kind = room is null ? "" : room.Kind.ToString().ToLowerInvariant(),
            RoomsHash = act is null ? "" : RunSnapshot.RoomsHash(act),
        };
    }

    /// <summary>깊이 기록 → 비교 키. 비운 깊이는 null. 액트 번호는 지금 사슬로 다시 센다.</summary>
    private RunDepth? DepthOf(DepthData? d) =>
        d is null || !_defs.Stages.TryGet(d.Act, out var act) ? null : new RunDepth(RunPlanner.ActNumber(_defs, act), d.Room);

    // ── 계단 저장 ─────────────────────────────────────────

    /// <summary>타이틀 이어 하기 한 줄용. 없거나 읽을 수 없으면 null(파일은 건드리지 않음).</summary>
    public RunSnapshot? PeekRun()
    {
        if (RunPath is not { } path || !File.Exists(path)) return null;
        try
        {
            return ParseRun(File.ReadAllText(path));
        }
        catch (Exception ex) when (ex is JsonException or IOException or InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>
    /// 이어 하기를 고른 순간: 읽고 <b>바로 지운다</b>. 없으면 null. JSON 이 깨졌거나 <c>required</c> 가 빠졌으면 <c>run.json.bad</c> 로 옮기고 null.
    /// 정의 불일치(<c>Format</c> 다름 포함)는 <c>RunSession</c> 이어 하기 생성자가 낸다 — 이때도 파일은 이미 지워졌다.
    /// </summary>
    public RunSnapshot? TakeRun()
    {
        if (RunPath is not { } path || !File.Exists(path)) return null;
        string text;
        try
        {
            text = File.ReadAllText(path);
        }
        catch (IOException ex)
        {
            _log($"SAVE run.json 을 읽지 못했다: {ex.Message}");
            return null;
        }
        try
        {
            var snap = ParseRun(text);
            File.Delete(path);
            _log($"SAVE run.json 을 꺼냈다(지움) next_act={snap.NextActId}");
            return snap;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or NotSupportedException)
        {
            MoveBad(path);
            _log($"SAVE run.json 이 깨져 {RunFileName}{BadSuffix} 로 옮겼다: {ex.Message}");
            return null;
        }
    }

    /// <summary>계단 저장 쓰기(있던 저장은 덮음). 메모리 전용이면 쓰지 않는다.</summary>
    public void SaveRun(RunSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (RunPath is not { } path) return;
        var root = new JsonObject
        {
            ["version"] = RunFileVersion,
            ["run"] = JsonNode.Parse(snapshot.ToJson()),
        };
        WriteAtomic(path, root.ToJsonString());
        _log($"SAVE run.json 을 썼다 next_act={snapshot.NextActId}");
    }

    /// <summary>새 게임으로 이어 할 판을 버린다(기록하지 않음).</summary>
    public void DiscardRun()
    {
        if (RunPath is not { } path || !File.Exists(path)) return;
        File.Delete(path);
        _log("SAVE run.json 을 버렸다");
    }

    private static RunSnapshot ParseRun(string text)
    {
        if (JsonNode.Parse(text, documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true }) is not JsonObject root)
            throw new JsonException("run.json 은 객체여야 한다");
        if (root["version"] is not JsonValue v || !v.TryGetValue<int>(out var version)) throw new JsonException("run.json 에 version 이 없다");
        if (version != RunFileVersion) throw new JsonException($"run.json version {version} 을 모른다");
        if (root["run"] is not JsonObject run) throw new JsonException("run.json 에 run 이 없다");
        // 위치 record 의 생성자 인자는 STJ 기본 옵션에서 빠져도 기본값이 된다(RunSnapshot.JsonOptions 는 계약이라 여기서 막는다 — 8.3 "required 빠짐은 .bad")
        foreach (var name in SnapshotKeys)
            if (!run.ContainsKey(name)) throw new JsonException($"run.json 의 run 에 {name} 이 없다");
        return RunSnapshot.FromJson(run.ToJsonString());
    }

    /// <summary>RunSnapshot 생성자 인자의 JSON 이름(snake_case) — 모두 있어야 한다.</summary>
    private static readonly string[] SnapshotKeys =
    [
        .. typeof(RunSnapshot).GetConstructors().OrderByDescending(c => c.GetParameters().Length).First().GetParameters()
            .Select(p => JsonNamingPolicy.SnakeCaseLower.ConvertName(p.Name!)),
    ];

    // ── save.json 읽기·쓰기 ─────────────────────────────────

    private SaveData Read()
    {
        var path = SavePath!;
        if (!File.Exists(path)) return new SaveData();
        JsonObject root;
        try
        {
            var text = File.ReadAllText(path);
            root = JsonNode.Parse(text, documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true }) as JsonObject
                   ?? throw new JsonException("save.json 은 객체여야 한다");
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException)
        {
            MoveBad(path);
            _log($"SAVE save.json 이 깨져 {SaveFileName}{BadSuffix} 로 옮기고 기본값으로 시작한다: {ex.Message}");
            return new SaveData();
        }

        var version = Int(root, "version") ?? 0;
        if (version > SaveData.CurrentVersion)
        {
            ReadOnly = true;
            _log($"SAVE save.json version {version} > {SaveData.CurrentVersion} — 읽기 전용(이번 실행은 덮어쓰지 않는다)");
        }

        var settings = new SettingsData();
        if (root["settings"] is JsonObject so)
        {
            try { settings = so.Deserialize<SettingsData>(SaveData.ReadOptions) ?? settings; }
            catch (JsonException ex) { _log($"SAVE settings 가 깨져 기본값을 쓴다: {ex.Message}"); }
        }

        var records = new Dictionary<string, Dictionary<string, RecordData>>(StringComparer.Ordinal);
        if (root["records"] is JsonObject ro)
            foreach (var (actId, byHeroNode) in ro)
            {
                if (byHeroNode is not JsonObject byHero)
                {
                    _log($"SAVE 기록 records.{actId} 가 객체가 아니라 버렸다");
                    continue;
                }
                var map = new Dictionary<string, RecordData>(StringComparer.Ordinal);
                foreach (var (heroId, recNode) in byHero)
                {
                    RecordData? rec = null;
                    try { rec = recNode?.Deserialize<RecordData>(SaveData.ReadOptions); }
                    catch (JsonException ex) { _log($"SAVE 기록 records.{actId}.{heroId} 가 깨져 그 줄만 버렸다: {ex.Message}"); continue; }
                    if (rec is null) continue;
                    if (rec.BestDepth is { } d && !DepthStillValid(d, out var why))
                    {
                        _log($"SAVE 기록 records.{actId}.{heroId} 의 깊이를 비웠다({why})");
                        rec = rec with { BestDepth = null };
                    }
                    map[heroId] = rec;
                }
                records[actId] = map;
            }

        return new SaveData
        {
            Version = Math.Max(version, SaveData.CurrentVersion),
            TotalGold = Math.Max(0, Int(root, "total_gold") ?? 0),
            LastHero = root["last_hero"] is JsonValue lh && lh.TryGetValue<string>(out var s) ? s : null,
            Settings = settings,
            Records = Freeze(records),
        };
    }

    private bool DepthStillValid(DepthData d, out string why)
    {
        if (!_defs.Stages.TryGet(d.Act, out var act) || !InChain(act))
        {
            why = $"액트 {d.Act} 가 지금 사슬에 없다";
            return false;
        }
        var hash = RunSnapshot.RoomsHash(act);
        if (!string.Equals(hash, d.RoomsHash, StringComparison.Ordinal))
        {
            why = $"rooms_hash {d.RoomsHash} ≠ 지금 {hash}";
            return false;
        }
        why = "";
        return true;
    }

    private bool InChain(StageDef act)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var s = RunPlanner.Root(_defs); s is not null && seen.Add(s.Id); s = RunPlanner.Next(_defs, s))
            if (s.Id == act.Id) return true;
        return false;
    }

    private void Write()
    {
        if (SavePath is not { } path) return;
        if (ReadOnly) return;
        var data = _data with { Version = SaveData.CurrentVersion, Records = Freeze(Copy(_data.Records)) };
        WriteAtomic(path, JsonSerializer.Serialize(data, SaveData.WriteOptions));
    }

    private static void WriteAtomic(string path, string text)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) System.IO.Directory.CreateDirectory(dir);
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, text);
        File.Move(tmp, path, overwrite: true);
    }

    private static void MoveBad(string path)
    {
        try { File.Move(path, path + BadSuffix, overwrite: true); }
        catch (IOException) { }
    }

    private static int? Int(JsonObject o, string key) =>
        o[key] is JsonValue v && v.TryGetValue<int>(out var i) ? i : (int?)null;

    private static Dictionary<string, Dictionary<string, RecordData>> Copy(IReadOnlyDictionary<string, IReadOnlyDictionary<string, RecordData>> src)
    {
        var d = new Dictionary<string, Dictionary<string, RecordData>>(StringComparer.Ordinal);
        foreach (var (k, v) in src) d[k] = new Dictionary<string, RecordData>(v, StringComparer.Ordinal);
        return d;
    }

    /// <summary>키를 서수 정렬한 사전(같은 내용이면 같은 바이트).</summary>
    private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, RecordData>> Freeze(Dictionary<string, Dictionary<string, RecordData>> src)
    {
        var outer = new SortedDictionary<string, IReadOnlyDictionary<string, RecordData>>(StringComparer.Ordinal);
        foreach (var (k, v) in src) outer[k] = new SortedDictionary<string, RecordData>(v, StringComparer.Ordinal);
        return outer;
    }
}
