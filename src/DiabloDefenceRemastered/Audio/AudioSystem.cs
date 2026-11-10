using DiabloDefenceRemastered.Core.Assets;
using DiabloDefenceRemastered.Core.Audio;

namespace DiabloDefenceRemastered.Audio;

/// <summary>
/// 소리 서비스 본문 (소유 WP8, 설계서 7.9). 신호 → 소리 고르기는 Core <see cref="SoundPipeline"/>(시험 대상)이 하고, 여기는 시간(앱 틱)·재생·음악만 붙인다.
/// <list type="bullet">
/// <item><see cref="Consume"/>: <c>PlayScene</c> 프레임 끝(<c>FxSystem.Consume</c> 다음, <c>buffer.Clear</c> 전). 시간은 마지막 <see cref="Update"/> 의 앱 틱.</item>
/// <item><see cref="Attach"/>(v0.7.1 <c>IWorldBound</c>): 방 이동 — 장판 반복을 끄고 간격 기록을 비운다. 나는 효과음·곡은 끊지 않는다(곡은 액트가 같으면 이어 틂).</item>
/// <item><see cref="PreloadStage"/>: 액트를 시작할 때만(v0.7) — 그 액트의 소리(<see cref="AudioReach"/>)와 곡을 미리 읽는다. 무음이면 효과음은 읽지 않는다.</item>
/// <item>보스 대사를 고른 프레임에 음악을 <c>voice_duck</c>(50%, 2s)로 줄인다.</item>
/// </list>
/// 끝낼 때 <see cref="Dispose"/> 가 <c>AUDIO muted=…</c> 요약 줄을 쓴다(GameApp 이 <c>IDisposable</c> 로 부른다 — WP8 구현 기록).
/// </summary>
public sealed class AudioSystem : IAudioServices, IDisposable
{
    private readonly GameDefs _defs;
    private readonly SoundPipeline _pipeline;
    private readonly SoundBank? _bank;
    private readonly MusicPlayer _music;
    private readonly IAppLog _log;
    private readonly HashSet<string> _warnedUi = new(StringComparer.Ordinal);
    private RectF _view;
    private AppTick _now;
    private bool _disposed;

    public AudioSystem(GameDefs defs, AudioRulesDef rules, string assetRoot, ISoundDurations durations, ContentAvailability availability,
                       ulong seed, bool muted, IAppLog log)
    {
        _defs = defs;
        _log = log;
        _bank = muted ? null : new SoundBank(assetRoot, rules, log);
        _pipeline = new SoundPipeline(defs, rules, durations, seed, muted, _bank);
        _music = new MusicPlayer(_pipeline.Router, rules, assetRoot, availability, muted, log);
        Muted = muted;
        var d = rules.Defaults;
        SetVolumes(d.Master, d.Sfx, d.Music);
    }

    public bool Muted { get; }

    /// <summary>지금까지 고르기 수(로그·검증용).</summary>
    public AudioCounts Counts => _pipeline.Counts;

    public void PreloadStage(StageDef stage)
    {
        _bank?.Preload(AudioReach.Files(AudioReach.Sounds(_defs, stage)));
        _music.Preload(stage.Id);
    }

    public void Attach(World world)
    {
        _pipeline.Attach();
        _view = world.ViewRect;
    }

    public void Consume(PresentationBuffer buffer, World world)
    {
        _view = world.ViewRect;
        _pipeline.Consume(buffer, world, _now.Value);
        if (_pipeline.BossVoicePlayed) _music.Duck(_now);
    }

    public void PlayUi(string key)
    {
        if (!_pipeline.PlayUi(key, _now.Value, _view) && !_warnedUi.Contains(key))
        {
            _warnedUi.Add(key);
            _log.Warn($"AUDIO ui 소리 '{key}' 연결이 없다");
        }
    }

    public void PlayMusic(string key) => _music.Play(key, _now);

    public void SetPaused(bool paused)
    {
        _bank?.SetPaused(paused);
        _music.SetPaused(paused);
    }

    public void SetVolumes(float master, float sfx, float music)
    {
        master = Math.Clamp(master, 0f, 1f);
        _bank?.SetVolumes(master, Math.Clamp(sfx, 0f, 1f));
        _music.SetVolume(master * Math.Clamp(music, 0f, 1f));
    }

    public void Update(AppTick now)
    {
        _now = now;
        _bank?.Update();
        _music.Update(now);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _log.Info(SoundPipeline.LogLine(Muted, _pipeline.Counts));
        _music.Dispose();
        _bank?.Dispose();
    }
}
