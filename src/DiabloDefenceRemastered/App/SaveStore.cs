using DiabloDefenceRemastered.Core.Save;

namespace DiabloDefenceRemastered.App;

/// <summary>
/// 저장 서비스 (설계서 8.3, 계약 <see cref="ISaveService"/>, WP7). 본문은 Core <see cref="SaveBook"/> 이고, 여기서는 위치와 계약 형 옮기기만 한다.
/// 위치: <c>--save-dir</c> 또는 <c>%LOCALAPPDATA%\DiabloDefenceRemastered</c>. 개발 옵션 실행인데 <c>--save-dir</c> 가 없으면 메모리에만(읽지도 쓰지도 않음).
/// </summary>
public sealed class SaveStore : ISaveService
{
    private readonly SaveBook _book;

    private SaveStore(SaveBook book) => _book = book;

    /// <summary>사용자 저장 폴더(<c>%LOCALAPPDATA%\DiabloDefenceRemastered</c>).</summary>
    public static string UserDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DiabloDefenceRemastered");

    /// <param name="saveDir"><c>--save-dir</c>(없으면 null).</param>
    /// <param name="protect">개발 옵션 실행이고 <c>--save-dir</c> 가 없음 — 메모리에만.</param>
    public static SaveStore Create(string? saveDir, bool protect, GameDefs defs, IAppLog log)
    {
        if (protect)
        {
            log.Info("SAVE memory_only=1 (개발 옵션 실행 — 사용자 저장을 읽지도 쓰지도 않는다)");
            return new SaveStore(SaveBook.InMemory(defs, log.Info));
        }
        var dir = saveDir ?? UserDir;
        var book = SaveBook.Open(dir, defs, log.Info);
        log.Info($"SAVE dir=\"{dir}\" read_only={(book.ReadOnly ? 1 : 0)}");
        return new SaveStore(book);
    }

    public SaveBook Book => _book;

    public GameSettings Settings
    {
        get
        {
            var s = _book.Settings;
            return new GameSettings
            {
                Fullscreen = s.Fullscreen,
                ScreenShake = s.ScreenShake,
                ShowMinimap = s.ShowMinimap,
                ShowDamageNumbers = s.ShowDamageNumbers,
                MasterVolume = s.MasterVolume,
                SfxVolume = s.SfxVolume,
                MusicVolume = s.MusicVolume,
            };
        }
    }

    public int TotalGold => _book.TotalGold;
    public string? LastHero => _book.LastHero;

    public void SaveSettings(GameSettings settings) => _book.SaveSettings(new SettingsData
    {
        Fullscreen = settings.Fullscreen,
        ScreenShake = settings.ScreenShake,
        ShowMinimap = settings.ShowMinimap,
        ShowDamageNumbers = settings.ShowDamageNumbers,
        MasterVolume = settings.MasterVolume,
        SfxVolume = settings.SfxVolume,
        MusicVolume = settings.MusicVolume,
    });

    public void RecordResult(RunResult result) => _book.RecordResult(new RunResultData(
        result.StartActId, result.HeroClassId, result.Victory, result.Reached, result.ReachedActId, result.RunTime, result.Level, result.Kills, result.Gold));

    public void SetLastHero(string heroClassId) => _book.SetLastHero(heroClassId);

    public RunRecord? BestRecord(string startActId, string heroClassId) =>
        _book.BestRecord(startActId, heroClassId) is { } r ? new RunRecord(r.BestDepth, r.Victory, r.BestTime, r.Level, r.Kills) : null;

    public RunSnapshotInfo? PeekRun() =>
        _book.PeekRun() is { } s ? new RunSnapshotInfo(s.NextActId, s.NextActNumber, s.HeroClassId) : null;

    public RunSnapshot? TakeRun() => _book.TakeRun();
    public void SaveRun(RunSnapshot snapshot) => _book.SaveRun(snapshot);
    public void DiscardRun() => _book.DiscardRun();
}
