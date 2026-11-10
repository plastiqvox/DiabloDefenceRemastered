namespace DiabloDefenceRemastered.Core.Audio;

/// <summary>재생 쪽(게임 <c>SoundBank</c>·목소리 칸). 무음이면 <see cref="SoundPipeline"/> 이 부르지 않는다.</summary>
public interface ISoundSink
{
    void Play(in SoundPlay play);
    void StartLoop(SoundDef sound);
    void StopLoop(SoundDef sound);
}

/// <summary>
/// 신호 → 고르기 → 재생 한 줄 (설계서 7.9 — 순수 로직, 소유 WP8). 게임 <c>AudioSystem</c> 은 이것에 실제 재생(<see cref="ISoundSink"/>)만 붙인다.
/// <b>무음이어도 고르기를 그대로 돌린다</b>(<see cref="Muted"/> 는 재생만 끔) → 같은 신호 열이면 무음과 소리 켬의 <c>AUDIO</c> 수가 같다(10.1 <c>Audio</c>).
/// <para>
/// 시간은 앱 틱(<c>now</c>). 월드가 바뀌면(<see cref="Attach"/>, v0.7.1 <c>IWorldBound</c>) 장판 반복을 끄고 간격 기록을 비운다 — 나는 소리는 끊지 않는다.
/// </para>
/// </summary>
public sealed class SoundPipeline
{
    private readonly SoundRouter _router;
    private readonly SoundMixer _mixer;
    private readonly HazardLoops _loops;
    private readonly ISoundSink? _sink;
    private readonly List<SoundDef> _started = [];
    private readonly List<SoundDef> _stopped = [];

    public SoundPipeline(GameDefs defs, AudioRulesDef rules, ISoundDurations durations, ulong seed, bool muted, ISoundSink? sink)
    {
        _router = new SoundRouter(defs);
        _mixer = new SoundMixer(rules, durations, seed);
        _loops = new HazardLoops(_router);
        Muted = muted || sink is null;
        _sink = sink;
    }

    public bool Muted { get; }
    public SoundRouter Router => _router;
    public SoundMixer Mixer => _mixer;
    public AudioCounts Counts => _mixer.Counts;
    public int ActiveLoops => _loops.Count;

    /// <summary>마지막 고르기에서 보스 대사(<c>boss_voice</c>)를 골랐나 — 음악 줄이기(<c>voice_duck</c>) 신호.</summary>
    public bool BossVoicePlayed { get; private set; }

    /// <summary>새 월드(방 이동·판 시작): 옛 월드의 장판 반복을 끄고 간격 기록을 비운다.</summary>
    public void Attach()
    {
        _stopped.Clear();
        _loops.Clear(_stopped);
        if (!Muted)
            foreach (var s in _stopped) _sink!.StopLoop(s);
        _mixer.ResetCooldowns();
    }

    /// <summary>
    /// 프레임 끝: 버퍼의 신호·이펙트·시체를 고르고(재생), 장판 반복을 맞춘다. 버퍼는 비우지 않는다.
    /// <paramref name="world"/> = 이 버퍼의 월드(직업·쏜 적·장판·카메라 시야).
    /// </summary>
    public void Consume(PresentationBuffer buffer, World world, long now)
    {
        _router.Route(buffer, world, _mixer);
        Flush(now, world.ViewRect);

        _started.Clear();
        _stopped.Clear();
        _loops.Update(world, _started, _stopped);
        if (Muted) return;
        foreach (var s in _stopped) _sink!.StopLoop(s);
        foreach (var s in _started) _sink!.StartLoop(s);
    }

    /// <summary>씬 UI 소리(<see cref="SoundRouter.TryUi"/>). 연결이 없으면 아무것도 안 함(거짓).</summary>
    public bool PlayUi(string key, long now, RectF view)
    {
        if (!_router.TryUi(key, out var r)) return false;
        _mixer.Request(r);
        Flush(now, view);
        return true;
    }

    private void Flush(long now, RectF view)
    {
        BossVoicePlayed = false;
        foreach (var p in _mixer.Mix(now, view))
        {
            if (string.Equals(p.Category, SoundRouter.BossVoiceCategory, StringComparison.Ordinal)) BossVoicePlayed = true;
            if (!Muted) _sink!.Play(p);
        }
    }

    /// <summary>종료 로그 한 줄(7.9): <c>AUDIO muted=… requested=… played=… dropped_cap=… dropped_interval=… dropped_offscreen=…</c>.</summary>
    public static string LogLine(bool muted, AudioCounts c) =>
        string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"AUDIO muted={(muted ? "true" : "false")} requested={c.Requested} played={c.Played} dropped_cap={c.DroppedCap} dropped_interval={c.DroppedInterval} dropped_offscreen={c.DroppedOffscreen}");
}
