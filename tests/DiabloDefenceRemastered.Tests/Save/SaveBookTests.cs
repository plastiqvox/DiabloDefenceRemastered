using DiabloDefenceRemastered.Core.Save;

namespace DiabloDefenceRemastered.Tests.Save;

/// <summary>저장(설계서 8.3, 10.1 <c>Save</c> — WP7): 왕복·비교 키·읽기 규칙·계단 저장.</summary>
public sealed class SaveBookTests
{
    private static readonly GameDefs Acts = RoomActs.Defs();

    private static RunResultData Outcome(string hero, int act, int room, bool victory, float time, int gold = 10, int kills = 5) =>
        new(RoomActs.Act1Id, hero, victory, new RunDepth(act, room), act == 1 ? RoomActs.Act1Id : RoomActs.Act2Id, time, 7, kills, gold);

    [Fact]
    public void RoundTrip_TwoHeroes_KeptApart_AndLastHero()
    {
        using var dir = new TempDir();
        var a = SaveBook.Open(dir.Path, Acts);
        a.RecordResult(Outcome("a", 1, 3, false, 200, gold: 15));
        a.RecordResult(Outcome("b", 2, 1, false, 500, gold: 5));
        a.SetLastHero("b");
        a.SaveSettings(new SettingsData { Fullscreen = true, MasterVolume = 0.5f, SfxVolume = 0.7f, MusicVolume = 0.3f });

        var b = SaveBook.Open(dir.Path, Acts);
        Assert.Equal(20, b.TotalGold);
        Assert.Equal("b", b.LastHero);
        Assert.Equal(new RunDepth(1, 3), b.BestRecord(RoomActs.Act1Id, "a")!.BestDepth);
        Assert.Equal(new RunDepth(2, 1), b.BestRecord(RoomActs.Act1Id, "b")!.BestDepth);
        Assert.Null(b.BestRecord(RoomActs.Act1Id, "c"));
        Assert.True(b.Settings.Fullscreen);
        Assert.Equal(0.5f, b.Settings.MasterVolume);
        Assert.Equal(0.3f, b.Settings.MusicVolume);
        var text = File.ReadAllText(Path.Combine(dir.Path, "save.json"));
        Assert.Contains("\"rooms_hash\"", text, StringComparison.Ordinal);
        Assert.Contains("\"kind\": \"normal\"", text, StringComparison.Ordinal);
    }

    [Theory]
    // (깊이, 승리, 시간) 먼저 기록 → 다음 판 → 바뀌나
    [InlineData(1, 3, false, 100f, 1, 4, false, 300f, true)]    // 더 깊음 → 새 기록
    [InlineData(1, 6, true, 300f, 1, 6, true, 200f, true)]     // 같은 깊이 승리 둘 → 짧은 시간
    [InlineData(1, 6, true, 200f, 1, 6, true, 300f, false)]
    [InlineData(1, 6, false, 300f, 1, 6, true, 400f, true)]    // 같은 깊이 패배 → 승리 = 새 기록
    [InlineData(1, 6, true, 300f, 1, 6, false, 100f, false)]   // 같은 깊이 승리 → 패배 = 그대로
    [InlineData(1, 5, false, 300f, 1, 5, false, 100f, false)]  // 같은 깊이 패배 둘 = 그대로
    [InlineData(2, 1, false, 500f, 1, 6, true, 100f, false)]   // 얕은 승리 = 그대로
    public void Compare_DepthVictoryTime(int a1, int r1, bool v1, float t1, int a2, int r2, bool v2, float t2, bool replaced)
    {
        var book = SaveBook.InMemory(Acts);
        book.RecordResult(Outcome("a", a1, r1, v1, t1, kills: 1));
        book.RecordResult(Outcome("a", a2, r2, v2, t2, kills: 2));
        Assert.Equal(replaced ? 2 : 1, book.BestRecord(RoomActs.Act1Id, "a")!.Kills);
    }

    [Fact]
    public void RunDepth_IsLexicographic()
    {
        Assert.True(new RunDepth(2, 1) > new RunDepth(1, 9));
        Assert.True(new RunDepth(1, 4) > new RunDepth(1, 3));
    }

    [Fact]
    public void InMemory_NeverTouchesDisk()
    {
        var book = SaveBook.InMemory(Acts);
        book.RecordResult(Outcome("a", 1, 2, false, 10));
        book.SaveRun(Snapshot());
        Assert.False(book.Persistent);
        Assert.Null(book.SavePath);
        Assert.Null(book.PeekRun());
        Assert.Equal(10, book.TotalGold);
    }

    [Fact]
    public void ReadsV06Shape_WithoutException()
    {
        using var dir = new TempDir();
        dir.Write("save.json", """
            { "version": 0, "total_gold": 321, "unlocked": ["stage_catacombs"],
              "settings": { "fullscreen": true, "screen_shake": false, "unknown_key": 1 },
              "records": { "stage_cathedral": { "sorcerer": { "best_time": 600, "level": 12, "kills": 900 } } } }
            """);
        var book = SaveBook.Open(dir.Path, Acts);
        Assert.Equal(321, book.TotalGold);
        Assert.True(book.Settings.Fullscreen);
        Assert.False(book.Settings.ScreenShake);
        Assert.Null(book.BestRecord("stage_cathedral", "sorcerer")!.BestDepth);   // 깊이 없음 → 비운 깊이
        book.RecordResult(Outcome("sorcerer", 1, 1, false, 30) with { StartActId = "stage_cathedral" });
        Assert.Equal(new RunDepth(1, 1), SaveBook.Open(dir.Path, Acts).BestRecord("stage_cathedral", "sorcerer")!.BestDepth);
    }

    [Fact]
    public void BrokenRecordLine_DropsOnlyThatLine()
    {
        using var dir = new TempDir();
        dir.Write("save.json", """
            { "version": 1, "total_gold": 50,
              "records": { "test_act1": { "a": { "victory": "yes" }, "b": { "victory": true, "best_time": 99, "level": 3, "kills": 4 } } } }
            """);
        var lines = new List<string>();
        var book = SaveBook.Open(dir.Path, Acts, lines.Add);
        Assert.Null(book.BestRecord(RoomActs.Act1Id, "a"));
        Assert.Equal(4, book.BestRecord(RoomActs.Act1Id, "b")!.Kills);
        Assert.Equal(50, book.TotalGold);
        Assert.Contains(lines, l => l.Contains("records.test_act1.a", StringComparison.Ordinal));
    }

    [Fact]
    public void HigherVersion_IsReadOnly_FileBytesUnchanged()
    {
        using var dir = new TempDir();
        var path = dir.Write("save.json", """{ "version": 99, "total_gold": 7, "future": { "x": 1 } }""");
        var before = File.ReadAllBytes(path);
        var book = SaveBook.Open(dir.Path, Acts);
        Assert.True(book.ReadOnly);
        book.RecordResult(Outcome("a", 1, 1, false, 1));
        book.SetLastHero("a");
        Assert.Equal(before, File.ReadAllBytes(path));
        Assert.Equal(17, book.TotalGold);   // 메모리에는 남음
    }

    [Fact]
    public void BrokenJson_MovesToBad_AndDefaults()
    {
        using var dir = new TempDir();
        dir.Write("save.json", "{ \"version\": 1, ");
        var book = SaveBook.Open(dir.Path, Acts);
        Assert.Equal(0, book.TotalGold);
        Assert.True(File.Exists(Path.Combine(dir.Path, "save.json.bad")));
        Assert.False(File.Exists(Path.Combine(dir.Path, "save.json")));
    }

    [Fact]
    public void RoomsTableChanged_ClearsOnlyDepth_AndNewWinIsRecorded()
    {
        using var dir = new TempDir();
        SaveBook.Open(dir.Path, Acts).RecordResult(Outcome("a", 1, 6, true, 400, kills: 9));
        // 방 수를 줄인 데이터(일반 방 5 를 뺌 — rooms_hash 다름)
        var act1 = Acts.Stages[0];
        var shorter = act1 with { Rooms = [.. act1.Rooms.Where(r => r.Number != 5)] };
        var changed = new GameDefs { Rules = Acts.Rules, Stages = new DefTable<StageDef>([shorter, Acts.Stages[1]]), Maps = Acts.Maps };
        var lines = new List<string>();
        var book = SaveBook.Open(dir.Path, changed, lines.Add);
        var rec = book.BestRecord(RoomActs.Act1Id, "a")!;
        Assert.Null(rec.BestDepth);
        Assert.True(rec.Victory);
        Assert.Equal(9, rec.Kills);
        Assert.Contains(lines, l => l.Contains("깊이를 비웠다", StringComparison.Ordinal));
        book.RecordResult(Outcome("a", 1, 2, false, 50, kills: 1));
        Assert.Equal(new RunDepth(1, 2), book.BestRecord(RoomActs.Act1Id, "a")!.BestDepth);
    }

    // ── 계단 저장 ──

    private static RunSnapshot Snapshot() =>
        new(RunSnapshot.CurrentFormat, 7ul, RoomActs.Act1Id, null, RoomActs.Act2Id, 2, RunSnapshot.RoomsHash(Acts.Stages[1]),
            null, new RunStats(), new FairnessCounters(), 1234, new RunDepth(1, 6), []);

    [Fact]
    public void RunFile_RoundTrip_TakeDeletes()
    {
        using var dir = new TempDir();
        var book = SaveBook.Open(dir.Path, Acts);
        Assert.Null(book.PeekRun());
        book.SaveRun(Snapshot());
        var peek = book.PeekRun();
        Assert.Equal(RoomActs.Act2Id, peek!.NextActId);
        Assert.True(File.Exists(book.RunPath));   // 엿보기는 지우지 않음
        var taken = book.TakeRun();
        Assert.Equal(1234, taken!.ElapsedTicks);
        Assert.False(File.Exists(book.RunPath));  // 읽는 순간 지움
        Assert.Null(book.TakeRun());
        book.SaveRun(Snapshot());
        book.DiscardRun();
        Assert.Null(book.PeekRun());
    }

    [Fact]
    public void RunFile_Broken_MovesToBad()
    {
        using var dir = new TempDir();
        dir.Write("run.json", """{ "version": 1, "run": { "format": 1 } }""");   // required 빠짐
        var book = SaveBook.Open(dir.Path, Acts);
        Assert.Null(book.PeekRun());
        Assert.Null(book.TakeRun());
        Assert.True(File.Exists(Path.Combine(dir.Path, "run.json.bad")));
        Assert.False(File.Exists(Path.Combine(dir.Path, "run.json")));
    }

    [Fact]
    public void RunFile_OtherFormat_IsReturned_ForSessionToReject()
    {
        using var dir = new TempDir();
        var book = SaveBook.Open(dir.Path, Acts);
        book.SaveRun(Snapshot() with { Format = RunSnapshot.CurrentFormat + 1 });
        var taken = book.TakeRun();
        Assert.Equal(RunSnapshot.CurrentFormat + 1, taken!.Format);   // 버림은 RunSession 생성자(불일치)
        Assert.False(File.Exists(book.RunPath));
    }
}
