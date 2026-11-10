using DiabloDefenceRemastered.Core.Assets;

namespace DiabloDefenceRemastered.Core.Audio;

/// <summary>고르기에서 따로 세는 제한(기획 12.7 표). 어느 쪽인지는 신호를 소리로 바꾸는 쪽(<see cref="SoundRouter"/>)이 정한다.</summary>
public enum SoundLimit : byte
{
    None,
    /// <summary>몬스터 맞음 — 전체에서 <c>monster_hit_interval</c>(0.08s)에 한 번.</summary>
    MonsterHit,
    /// <summary>몬스터 죽음 — 동시 <c>monster_death.voices</c>(4), 같은 종(<see cref="SoundRequest.Group"/>) <c>same_kind_interval</c>(0.1s)에 한 번.</summary>
    MonsterDeath,
}

/// <summary>
/// 소리 요청 하나(7.9). <paramref name="Tick"/> = 생긴 틱(정렬 둘째 키 — 스테이지 틱 또는 UI 면 0), <paramref name="Pos"/> = 월드 px(null = 위치 없음: UI·좌우 0·화면 밖 검사 안 함),
/// <paramref name="Category"/> = 분류 덮어쓰기(보스·중간 보스가 낸 몬스터 소리 → <c>boss</c>, null = 정의 분류).
/// </summary>
public readonly record struct SoundRequest(SoundDef Def, int Tick, Vector2? Pos = null, string? Category = null,
                                           SoundLimit Limit = SoundLimit.None, string? Group = null);

/// <summary>
/// 고른 소리 하나. 재생 쪽(게임 <c>AudioSystem</c>)은 이것만 보고 튼다. <paramref name="Volume"/> = 분류 × 정의(설정 음량은 재생 쪽이 곱함),
/// <paramref name="Pan"/> −pan..+pan, <paramref name="Pitch"/> 재생 배율(1 = 그대로), <paramref name="EndTick"/> = 자리가 비는 앱 틱(소리 길이 색인).
/// </summary>
public readonly record struct SoundPlay(SoundDef Def, string File, string Category, float Volume, float Pan, float Pitch, long StartTick, long EndTick);

/// <summary>
/// 고르기 누적 수(종료 로그 <c>AUDIO muted=… requested=… played=… dropped_cap=… dropped_interval=… dropped_offscreen=…</c>, 7.9).
/// <see cref="MissingLength"/> = 길이 색인에 없는 파일을 고른 수(1틱으로 셈 — 시작 검사가 따로 잡는다).
/// </summary>
public readonly record struct AudioCounts(long Requested, long Played, long DroppedCap, long DroppedInterval, long DroppedOffscreen, long MissingLength)
{
    public long Dropped => DroppedCap + DroppedInterval + DroppedOffscreen;
}

/// <summary>소리 파일 길이(앱 틱, 높낮이 배율 반영). 0 = 모름.</summary>
public interface ISoundDurations
{
    int Ticks(string file, float pitch);
}

/// <summary>가져오기 출력 소리 색인(<c>d1/sound/index.json</c>)으로 길이를 센다 — 무음·소리 켬이 같은 색인을 써서 <c>AUDIO</c> 수가 같다(7.9).</summary>
public sealed class SoundIndexDurations(SoundIndex index) : ISoundDurations
{
    public int Ticks(string file, float pitch) =>
        index.TryGet(file, out var info) ? info.DurationTicks(pitch, SoundMixer.TicksPerSecond) : 0;
}

/// <summary>
/// 소리 고르기 (설계서 7.9, 기획 12.7 — 순수 로직, 소유 WP8). 한 프레임에 들어온 요청을 (분류 우선순위, 생긴 틱, 들어온 순서)로 정렬해 차례로 본다 → 결정적.
/// <para>
/// 버리는 규칙(값은 <c>rules.audio</c>): ① 화면 밖 — 분류가 <c>offscreen: false</c> 면 시야를 <c>offscreen_margin</c> 만큼 넓힌 사각형 밖(안개와 무관 — 시야 계산을 읽지 않는다)
/// ② 간격 — 같은 소리 ID <c>min_interval</c>(정의가 덮음), 몬스터 맞음 전체 <c>monster_hit_interval</c>, 몬스터 죽음 같은 종 <c>same_kind_interval</c>
/// ③ 자리 — 같은 소리 동시 <c>same_sound</c>(정의 <c>max_instances</c>), 몬스터 죽음 동시 <c>monster_death.voices</c>, 전체 동시 <c>voices</c>.
/// 자리가 없으면 새 소리를 버린다(이미 나는 소리는 끊지 않음). 우선순위가 높은 요청이 먼저 자리를 얻는다.
/// </para>
/// <para>
/// 시간 기준은 앱 틱(벽시계가 아님). 자리가 비는 시각은 소리 길이 색인의 끝 틱이다 — 재생 장치 상태(<c>SoundEffectInstance.State</c>)를 보지 않아 무음·소리 켬·장치 지연과 무관하게 같은 수를 센다.
/// 파일·높낮이 무작위는 게임 쪽 난수(시뮬레이션 <c>SimRandom</c> 과 따로 — 같은 씨앗이면 같은 고르기). 반복 소리는 따로(<see cref="HazardLoops"/>) — 이 동시 수에 들지 않는다.
/// </para>
/// </summary>
public sealed class SoundMixer
{
    public const int TicksPerSecond = 60;

    // 간격 비교의 부동소수 여유(0.05s = 3틱이 정확히 3틱 뒤에 통과하게)
    private const double IntervalEpsilon = 1e-6;

    private readonly AudioRulesDef _rules;
    private readonly ISoundDurations _durations;
    private readonly RandomStream _random;
    private readonly Dictionary<string, int> _priority = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SoundCategoryDef> _categories = new(StringComparer.Ordinal);

    private readonly List<Pending> _pending = [];
    private readonly List<SoundPlay> _played = [];
    private readonly List<Voice> _voices = [];
    private readonly Dictionary<string, long> _lastById = new(StringComparer.Ordinal);
    private readonly Dictionary<string, long> _lastDeathByGroup = new(StringComparer.Ordinal);
    private long _lastMonsterHit = long.MinValue;
    private int _sequence;

    private long _requested, _playedCount, _droppedCap, _droppedInterval, _droppedOffscreen, _missingLength;

    public SoundMixer(AudioRulesDef rules, ISoundDurations durations, ulong seed = 0)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(durations);
        _rules = rules;
        _durations = durations;
        _random = new RandomStream(seed ^ 0x5EED_A0D1_0000_0008UL);
        for (var i = 0; i < rules.Categories.Count; i++)
        {
            var c = rules.Categories[i];
            if (_priority.TryAdd(c.Id, i)) _categories[c.Id] = c;
        }
    }

    public AudioRulesDef Rules => _rules;

    public AudioCounts Counts => new(_requested, _playedCount, _droppedCap, _droppedInterval, _droppedOffscreen, _missingLength);

    /// <summary>아직 고르지 않은 요청 수(이번 프레임).</summary>
    public int PendingCount => _pending.Count;

    /// <summary>이번 프레임에 요청을 쌓는다. 고르기는 <see cref="Mix"/>.</summary>
    public void Request(in SoundRequest request)
    {
        ArgumentNullException.ThrowIfNull(request.Def);
        _requested++;
        var category = request.Category ?? request.Def.Category;
        _pending.Add(new Pending(request, Priority(category), _sequence++));
    }

    /// <summary>
    /// 쌓인 요청을 고른다. <paramref name="now"/> = 지금 앱 틱, <paramref name="view"/> = 카메라 시야(월드 px — 화면 밖 판정·좌우 기준).
    /// 돌려주는 목록은 다음 <see cref="Mix"/> 까지 유효하다.
    /// </summary>
    public IReadOnlyList<SoundPlay> Mix(long now, RectF view)
    {
        _played.Clear();
        FreeVoices(now);
        if (_pending.Count == 0) return _played;

        _pending.Sort(static (a, b) =>
        {
            var c = a.Priority.CompareTo(b.Priority);
            if (c != 0) return c;
            c = a.Request.Tick.CompareTo(b.Request.Tick);
            return c != 0 ? c : a.Sequence.CompareTo(b.Sequence);
        });

        var wide = view.Inflate(_rules.OffscreenMargin);
        foreach (var p in _pending) Consider(p.Request, now, view, wide);
        _pending.Clear();
        _sequence = 0;
        return _played;
    }

    /// <summary>지금 나는(자리를 차지한) 소리 수.</summary>
    public int ActiveVoices(long now)
    {
        FreeVoices(now);
        return _voices.Count;
    }

    /// <summary>
    /// 새 월드를 받음(v0.7.1, <c>IWorldBound</c>): 간격 기록(쿨다운)을 비운다. 나는 소리의 자리는 그대로 둔다 —
    /// 방이 바뀌어도 재생 중인 소리를 끊지 않으므로 그 소리가 끝날 때까지 자리를 차지한다(7.9).
    /// </summary>
    public void ResetCooldowns()
    {
        _lastById.Clear();
        _lastDeathByGroup.Clear();
        _lastMonsterHit = long.MinValue;
    }

    private void Consider(in SoundRequest r, long now, RectF view, RectF wide)
    {
        var def = r.Def;
        var categoryId = r.Category ?? def.Category;
        _categories.TryGetValue(categoryId, out var category);

        // ① 화면 밖. 분류를 덮어쓴 요청(보스 몬스터 소리)은 그 분류 값을 따른다 — 정의 덮어쓰기는 정의 분류일 때만
        var offscreenOk = r.Category is null ? def.Offscreen ?? category?.Offscreen ?? true : category?.Offscreen ?? true;
        if (!offscreenOk && r.Pos is { } pos && !wide.Contains(pos))
        {
            _droppedOffscreen++;
            return;
        }

        // ② 간격
        if (Within(_lastById, def.Id, now, def.MinInterval ?? _rules.MinInterval)
            || (r.Limit == SoundLimit.MonsterHit && Elapsed(_lastMonsterHit, now) < _rules.MonsterHitInterval - IntervalEpsilon)
            || (r.Limit == SoundLimit.MonsterDeath && Within(_lastDeathByGroup, r.Group ?? def.Id, now, _rules.MonsterDeath.SameKindInterval)))
        {
            _droppedInterval++;
            return;
        }

        // ③ 자리
        var maxSame = def.MaxInstances ?? _rules.SameSound;
        int same = 0, deaths = 0;
        foreach (var v in _voices)
        {
            if (string.Equals(v.SoundId, def.Id, StringComparison.Ordinal)) same++;
            if (v.Death) deaths++;
        }
        if (_voices.Count >= _rules.Voices || same >= maxSame
            || (r.Limit == SoundLimit.MonsterDeath && deaths >= _rules.MonsterDeath.Voices))
        {
            _droppedCap++;
            return;
        }

        // 고름: 파일·높낮이 무작위(게임 쪽 난수), 길이 색인으로 끝 틱
        var file = def.Files.Count switch
        {
            0 => "",
            1 => def.Files[0],
            _ => def.Files[_random.Index(def.Files.Count)],
        };
        var pitch = def.PitchJitter > 0f ? 1f + _random.Range(-def.PitchJitter, def.PitchJitter) : 1f;
        var ticks = file.Length > 0 ? _durations.Ticks(file, pitch) : 0;
        if (ticks <= 0)
        {
            _missingLength++;
            ticks = 1;
        }

        var volume = (category?.Volume ?? 1f) * def.Volume;
        var end = now + ticks;
        _voices.Add(new Voice(def.Id, end, r.Limit == SoundLimit.MonsterDeath));
        _lastById[def.Id] = now;
        if (r.Limit == SoundLimit.MonsterHit) _lastMonsterHit = now;
        if (r.Limit == SoundLimit.MonsterDeath) _lastDeathByGroup[r.Group ?? def.Id] = now;
        _played.Add(new SoundPlay(def, file, categoryId, volume, Pan(r.Pos, view), pitch, now, end));
        _playedCount++;
    }

    /// <summary>좌우: (x − 화면 가운데 x) / (화면 폭 / 2) × pan, ±pan 으로 자름. 위치 없음이면 0.</summary>
    public float Pan(Vector2? pos, RectF view)
    {
        if (pos is not { } p || view.Width <= 0f) return 0f;
        var pan = _rules.Pan;
        var v = (p.X - view.Center.X) / (view.Width * 0.5f) * pan;
        return Math.Clamp(v, -pan, pan);
    }

    private int Priority(string category) => _priority.TryGetValue(category, out var i) ? i : _priority.Count;

    private void FreeVoices(long now)
    {
        var w = 0;
        for (var i = 0; i < _voices.Count; i++)
            if (_voices[i].EndTick > now) _voices[w++] = _voices[i];
        _voices.RemoveRange(w, _voices.Count - w);
    }

    private static bool Within(Dictionary<string, long> last, string key, long now, float interval) =>
        last.TryGetValue(key, out var t) && Elapsed(t, now) < interval - IntervalEpsilon;

    private static double Elapsed(long since, long now) => since == long.MinValue ? double.MaxValue : (now - since) / (double)TicksPerSecond;

    private readonly record struct Pending(SoundRequest Request, int Priority, int Sequence);

    private readonly record struct Voice(string SoundId, long EndTick, bool Death);
}
