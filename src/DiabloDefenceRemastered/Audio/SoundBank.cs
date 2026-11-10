using DiabloDefenceRemastered.Core.Audio;
using Microsoft.Xna.Framework.Audio;

namespace DiabloDefenceRemastered.Audio;

/// <summary>
/// 효과음 재생 (소유 WP8, 설계서 7.9 "재생"). <see cref="SoundPipeline"/> 이 고른 소리(<see cref="SoundPlay"/>)만 튼다 — 고르기·제한은 여기서 하지 않는다.
/// <list type="bullet">
/// <item>WAV 는 <see cref="SoundEffect.FromFile"/>(파이프라인 없음, 가져오기가 16비트 PCM 으로 바꿔 둠). 액트 시작 때 <see cref="Preload"/>, 빠진 것은 처음 쓸 때 읽는다.</item>
/// <item>목소리 칸: 효과음 인스턴스 <c>voices</c> + 8 칸(고르기 수와 장치 상태가 어긋나도 새 소리를 받을 여유 — 고르기 결과와 무관) + 반복 소리 칸(소리 ID 마다 하나).
/// 칸이 실제로 아직 소리를 내고 있으면 그 칸만 건너뛴다(<see cref="SoundState"/> 는 재생에만 쓰고 고르기에 쓰지 않는다).</item>
/// <item>음량 = 설정(전체 × 효과음) × 분류 × 정의, 높낮이 배율 → MonoGame 옥타브(log2), 좌우 −1..1.</item>
/// </list>
/// </summary>
public sealed class SoundBank : ISoundSink, IDisposable
{
    private const int SpareVoices = 8;

    private readonly string _root;
    private readonly AudioRulesDef _rules;
    private readonly IAppLog _log;
    private readonly Dictionary<string, SoundEffect?> _effects = new(StringComparer.Ordinal);
    private readonly Voice[] _voices;
    private readonly SortedDictionary<string, Loop> _loops = new(StringComparer.Ordinal);
    private float _master = 1f, _sfx = 1f;
    private bool _paused;
    private long _deviceBusy;

    public SoundBank(string assetRoot, AudioRulesDef rules, IAppLog log)
    {
        _root = assetRoot;
        _rules = rules;
        _log = log;
        _voices = new Voice[Math.Max(1, rules.Voices) + SpareVoices];
    }

    /// <summary>모든 칸이 실제로 아직 소리를 내고 있어 틀지 못한 수(고르기 수와 별개 — 로그용).</summary>
    public long DeviceBusy => _deviceBusy;

    /// <summary>파일들을 미리 읽는다(이미 읽은 것은 건너뜀).</summary>
    public void Preload(IEnumerable<string> files)
    {
        foreach (var f in files) Effect(f);
    }

    public void Play(in SoundPlay play)
    {
        if (Effect(play.File) is not { } effect) return;
        var slot = FreeSlot();
        if (slot < 0)
        {
            _deviceBusy++;
            return;
        }
        try
        {
            var instance = effect.CreateInstance();
            instance.Volume = Math.Clamp(play.Volume * _master * _sfx, 0f, 1f);
            instance.Pan = Math.Clamp(play.Pan, -1f, 1f);
            instance.Pitch = PitchOctaves(play.Pitch);
            instance.Play();   // 일시정지 중에 고른 소리(메뉴 이동 등 UI)는 멈추지 않는다
            _voices[slot] = new Voice(instance, play.Volume);
        }
        catch (Exception ex) when (ex is InstancePlayLimitException or NoAudioHardwareException)
        {
            _deviceBusy++;
        }
    }

    public void StartLoop(SoundDef sound)
    {
        if (_loops.ContainsKey(sound.Id) || sound.Files.Count == 0 || Effect(sound.Files[0]) is not { } effect) return;
        var volume = CategoryVolume(sound.Category) * sound.Volume;
        try
        {
            var instance = effect.CreateInstance();
            instance.IsLooped = true;
            instance.Volume = Math.Clamp(volume * _master * _sfx, 0f, 1f);
            instance.Play();
            if (_paused) instance.Pause();
            _loops[sound.Id] = new Loop(instance, volume);
        }
        catch (Exception ex) when (ex is InstancePlayLimitException or NoAudioHardwareException)
        {
            _deviceBusy++;
        }
    }

    public void StopLoop(SoundDef sound)
    {
        if (!_loops.Remove(sound.Id, out var loop)) return;
        loop.Instance.Stop();
        loop.Instance.Dispose();
    }

    /// <summary>일시정지 오버레이(기획 12.3): 효과음 인스턴스를 멈췄다가 되돌린다.</summary>
    public void SetPaused(bool paused)
    {
        if (_paused == paused) return;
        _paused = paused;
        foreach (var v in _voices)
            if (v.Instance is { IsDisposed: false } i) Toggle(i, paused);
        foreach (var l in _loops.Values) Toggle(l.Instance, paused);
    }

    public void SetVolumes(float master, float sfx)
    {
        _master = master;
        _sfx = sfx;
        foreach (var v in _voices)
            if (v.Instance is { IsDisposed: false } i) i.Volume = Math.Clamp(v.Volume * _master * _sfx, 0f, 1f);
        foreach (var l in _loops.Values) l.Instance.Volume = Math.Clamp(l.Volume * _master * _sfx, 0f, 1f);
    }

    /// <summary>앱 틱마다: 다 난 인스턴스를 놓아 칸을 비운다.</summary>
    public void Update()
    {
        for (var i = 0; i < _voices.Length; i++)
        {
            if (_voices[i].Instance is not { } inst || inst.State != SoundState.Stopped) continue;
            inst.Dispose();
            _voices[i] = default;
        }
    }

    public void Dispose()
    {
        for (var i = 0; i < _voices.Length; i++)
        {
            _voices[i].Instance?.Dispose();
            _voices[i] = default;
        }
        foreach (var l in _loops.Values) l.Instance.Dispose();
        _loops.Clear();
        foreach (var e in _effects.Values) e?.Dispose();
        _effects.Clear();
        if (_deviceBusy > 0) _log.Info($"AUDIO device_busy={_deviceBusy}");
    }

    /// <summary>재생 배율(1 = 그대로) → MonoGame 높낮이(옥타브, −1..1).</summary>
    public static float PitchOctaves(float ratio) => ratio > 0f ? Math.Clamp(MathF.Log2(ratio), -1f, 1f) : 0f;

    private int FreeSlot()
    {
        for (var i = 0; i < _voices.Length; i++)
        {
            var inst = _voices[i].Instance;
            if (inst is null) return i;
            if (inst.State == SoundState.Stopped)
            {
                inst.Dispose();
                _voices[i] = default;
                return i;
            }
        }
        return -1;
    }

    private SoundEffect? Effect(string file)
    {
        if (_effects.TryGetValue(file, out var e)) return e;
        try
        {
            e = SoundEffect.FromFile(Path.Combine(_root, file));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or NotSupportedException or NoAudioHardwareException)
        {
            _log.Warn($"AUDIO 소리 파일을 읽지 못했다: {file} ({ex.Message})");
            e = null;
        }
        _effects[file] = e;
        return e;
    }

    private float CategoryVolume(string category)
    {
        foreach (var c in _rules.Categories)
            if (string.Equals(c.Id, category, StringComparison.Ordinal)) return c.Volume;
        return 1f;
    }

    private static void Toggle(SoundEffectInstance i, bool pause)
    {
        if (pause && i.State == SoundState.Playing) i.Pause();
        else if (!pause && i.State == SoundState.Paused) i.Resume();
    }

    private readonly record struct Voice(SoundEffectInstance? Instance, float Volume);

    private readonly record struct Loop(SoundEffectInstance Instance, float Volume);
}
