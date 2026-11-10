using DiabloDefenceRemastered.Core.Assets;
using DiabloDefenceRemastered.Core.Audio;
using Microsoft.Xna.Framework.Audio;

namespace DiabloDefenceRemastered.Audio;

/// <summary>
/// 음악 (소유 WP8, 설계서 7.9 "재생", 기획 12.7 "음악").
/// <list type="bullet">
/// <item>곡 고르기: 연결 표 <c>music.&lt;키&gt;</c>(<c>title</c> 또는 스테이지 ID) → 그 곡의 <c>requires</c> 묶음이 없으면 <c>fallback</c>(6.15·6.17).
/// <b>지금 곡과 같은 곡이면 아무것도 안 한다</b>(v0.7 — 같은 액트의 방 이동에서 이어 틂, <c>PlayScene</c> 이 안 부르는 것과 이중 안전).</item>
/// <item>바뀔 때: 파일 읽기는 작업 스레드(<see cref="File.ReadAllBytes"/>), 옛 곡은 <c>music_fade</c> 에 걸쳐 줄인다. 읽기가 끝난 뒤 첫 <see cref="Update"/> 에
/// 주 스레드가 <see cref="SoundEffect.FromStream"/> 으로 만들어 튼다(OpenAL 버퍼 올리기는 주 스레드). 메모리에는 최대 두 곡(옛 곡·새 곡).</item>
/// <item>보스 대사 → <c>voice_duck</c>(50%, 2s), 일시정지 → <c>pause_music</c>(50%). 페이드·덕킹은 앱 틱으로 센다.</item>
/// <item>무음: 파일을 읽지 않고 곡 이름만 따라간다 — <c>AUDIO music=</c> 줄은 소리 켬과 같다(액트마다 한 줄 확인, 11.1 WP8).</item>
/// </list>
/// 로그: <c>AUDIO music=&lt;튼 곡&gt;</c>, 묶음이 없어 바꿨으면 <c>AUDIO music=&lt;튼 곡&gt; fallback=&lt;원래 곡&gt;</c>.
/// </summary>
public sealed class MusicPlayer : IDisposable
{
    private readonly MusicChoice _choice;
    private readonly AudioRulesDef _rules;
    private readonly string _root;
    private readonly bool _muted;
    private readonly IAppLog _log;
    private readonly Dictionary<string, Task<byte[]?>> _reads = new(StringComparer.Ordinal);

    private Track? _current;
    private Track? _fading;
    private MusicDef? _pending;
    private long _fadeStart;
    private long _duckUntil = long.MinValue;
    private bool _paused;
    private float _volume = 1f;

    public MusicPlayer(SoundRouter router, AudioRulesDef rules, string assetRoot, ContentAvailability availability, bool muted, IAppLog log)
    {
        _choice = new MusicChoice(router, availability.Has);
        _rules = rules;
        _root = assetRoot;
        _muted = muted;
        _log = log;
    }

    /// <summary>지금 틀고 있거나 틀려고 읽는 곡 ID(없으면 null).</summary>
    public string? Current => _choice.Current;

    /// <summary>설정 음량(전체 × 음악).</summary>
    public void SetVolume(float volume) => _volume = Math.Clamp(volume, 0f, 1f);

    public void SetPaused(bool paused) => _paused = paused;

    /// <summary>보스 대사가 난 프레임: <c>voice_duck.time</c> 동안 <c>voice_duck.to</c> 로 줄인다(다시 오면 늘린다).</summary>
    public void Duck(AppTick now) => _duckUntil = now.Value + Ticks(_rules.VoiceDuck.Time);

    /// <summary>액트 시작 전 그 스테이지 곡을 미리 읽는다(작업 스레드). 무음이면 아무것도 안 한다.</summary>
    public void Preload(string key)
    {
        if (_muted || _choice.Peek(key) is not { } music) return;
        StartRead(music.File);
    }

    public void Play(string key, AppTick now)
    {
        if (!_choice.TryChange(key, out var music, out var line, out var warning))
        {
            if (warning is not null) _log.Warn(warning);
            return;   // 같은 곡 — 이어 틂
        }
        _log.Info(line!);
        if (_muted) return;

        // 옛 곡을 줄이기 시작하고(이미 줄이는 곡은 버림) 새 곡을 읽는다
        if (_fading is { } old) old.Dispose();
        _fading = _current;
        _current = null;
        _fadeStart = now.Value;
        _pending = music!;
        StartRead(music!.File);
        TrimReads(music.File);
    }

    public void Update(AppTick now)
    {
        if (_muted) return;
        var fadeTicks = Ticks(_rules.MusicFade);
        var fadeLeft = fadeTicks <= 0 ? 0f : 1f - Math.Clamp((now.Value - _fadeStart) / (float)fadeTicks, 0f, 1f);

        if (_fading is { } fading)
        {
            if (fadeLeft <= 0f)
            {
                fading.Dispose();
                _fading = null;
            }
            else fading.Instance.Volume = Mixed(fadeLeft, now);
        }

        // 읽기가 끝났고 옛 곡이 다 줄었으면 새 곡을 만든다(주 스레드)
        if (_pending is { } next && _fading is null && _reads.TryGetValue(next.File, out var read) && read.IsCompleted)
        {
            _pending = null;
            var bytes = read.Status == TaskStatus.RanToCompletion ? read.Result : null;
            if (bytes is null) _log.Warn($"AUDIO 곡 파일을 읽지 못했다: {next.File}");
            else _current = Create(next, bytes);
        }

        if (_current is { } cur) cur.Instance.Volume = Mixed(1f, now);
    }

    public void Dispose()
    {
        _fading?.Dispose();
        _current?.Dispose();
        _fading = _current = null;
    }

    private Track? Create(MusicDef music, byte[] bytes)
    {
        try
        {
            using var stream = new MemoryStream(bytes, writable: false);
            var effect = SoundEffect.FromStream(stream);
            var instance = effect.CreateInstance();
            instance.IsLooped = music.Loop;
            instance.Volume = 0f;
            instance.Play();
            return new Track(effect, instance);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or NotSupportedException or InstancePlayLimitException)
        {
            _log.Warn($"AUDIO 곡을 틀지 못했다: {music.File} ({ex.Message})");
            return null;
        }
    }

    private float Mixed(float fade, AppTick now)
    {
        var v = _volume * fade;
        if (now.Value < _duckUntil) v *= _rules.VoiceDuck.To;
        if (_paused) v *= _rules.PauseMusic;
        return Math.Clamp(v, 0f, 1f);
    }

    private void StartRead(string file)
    {
        if (_reads.ContainsKey(file)) return;
        var path = Path.Combine(_root, file);
        _reads[file] = Task.Run(() =>
        {
            try { return File.ReadAllBytes(path); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return (byte[]?)null; }
        });
    }

    /// <summary>읽어 둔 곡 바이트는 최대 둘(지금 곡 + 미리 읽은 다음 곡) — 가장 긴 곡이 약 15MB(7.9).</summary>
    private void TrimReads(string keep)
    {
        if (_reads.Count <= 2) return;
        foreach (var file in _reads.Keys.Where(f => f != keep && _reads[f].IsCompleted).ToList())
        {
            _reads.Remove(file);
            if (_reads.Count <= 2) break;
        }
    }

    private static long Ticks(float seconds) => (long)MathF.Round(seconds * SoundMixer.TicksPerSecond);

    private sealed class Track(SoundEffect effect, SoundEffectInstance instance) : IDisposable
    {
        public SoundEffectInstance Instance { get; } = instance;

        public void Dispose()
        {
            Instance.Stop();
            Instance.Dispose();
            effect.Dispose();
        }
    }
}
