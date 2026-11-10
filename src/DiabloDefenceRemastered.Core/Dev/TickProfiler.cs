using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace DiabloDefenceRemastered.Core.Dev;

/// <summary><c>--log-ticks</c> 한 줄의 월드 쪽 수(그 순간 값).</summary>
public readonly record struct PerfCounts(int Enemies, int HeroProjectiles, int EnemyProjectiles, int Hazards);

/// <summary>한 프레임의 그리기 쪽 측정(게임이 넘김). 시간은 ms.</summary>
public readonly record struct FrameSample(double FrameMs, double DrawMs, double FogMs, int DrawCalls);

/// <summary>
/// 단계별 시간 재기 (설계서 10.3, WP7). <see cref="Simulation.Profiler"/> 로 걸면 단계마다 <see cref="Stopwatch"/> 시각을 누적하고,
/// 게임은 틱마다 <see cref="BeginTick"/>/<see cref="EndTick"/>, 프레임마다 <see cref="Frame"/>, 1초마다 <see cref="Row"/>(CSV 한 줄)를 부른다.
/// 열: <c>app_s, stage_s, enemies, hero_proj, enemy_proj, hazards, tick_avg_ms, tick_max_ms, &lt;단계별 평균 ms…&gt;, draw_ms, fog_ms, frame_ms, slow_frames, draw_calls, alloc_bytes, dropped_ticks</c>.
/// <c>slow_frames</c> = 16.7ms 넘은 프레임 수. 끝에 <see cref="Summary"/> 가 앞 5초를 뺀 p50/p95/p99/max 를 낸다. <c>--check</c> 열은 아직 없다(13장 WP7 구현 기록).
/// 시계는 넘겨받을 수 있다(시험).
/// </summary>
public sealed class TickProfiler(Func<long>? clock = null, long frequency = 0) : ISimProfiler
{
    public const double SlowFrameMs = 1000.0 / 60.0;
    public const double SummarySkipSeconds = 5;

    private readonly Func<long> _clock = clock ?? Stopwatch.GetTimestamp;
    private readonly double _msPerTick = 1000.0 / (frequency > 0 ? frequency : clock is null ? Stopwatch.Frequency : 1000);
    private readonly long[] _phaseStart = new long[SimulationBuilder.PhaseCount];
    private readonly long[] _phaseSum = new long[SimulationBuilder.PhaseCount];
    private long _tickStart;
    private long _tickSum;
    private long _tickMax;
    private int _ticks;
    private int _frames;
    private int _slowFrames;
    private double _frameSum, _drawSum, _fogSum;
    private long _drawCallSum;
    private long _allocMark = GC.GetTotalAllocatedBytes();

    private readonly List<double> _frameMs = [];
    private readonly List<double> _tickMs = [];
    private readonly List<int> _drawCalls = [];

    /// <summary>마지막 프레임의 앱 초 — 이것이 <see cref="SummarySkipSeconds"/> 아래인 동안의 측정은 요약에서 뺀다.</summary>
    private double _appSeconds;

    public static string Header
    {
        get
        {
            var sb = new StringBuilder("app_s,stage_s,enemies,hero_proj,enemy_proj,hazards,tick_avg_ms,tick_max_ms");
            for (var i = 0; i < SimulationBuilder.PhaseCount; i++) sb.Append(',').Append(PhaseColumn((SimPhase)i));
            sb.Append(",draw_ms,fog_ms,frame_ms,slow_frames,draw_calls,alloc_bytes,dropped_ticks");
            return sb.ToString();
        }
    }

    private static string PhaseColumn(SimPhase p)
    {
        var name = p.ToString();
        var sb = new StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c) && i > 0) sb.Append('_');
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.Append("_ms").ToString();
    }

    public void Begin(SimPhase phase) => _phaseStart[(int)phase] = _clock();
    public void End(SimPhase phase) => _phaseSum[(int)phase] += _clock() - _phaseStart[(int)phase];

    /// <summary>스테이지 틱 하나 시작(<c>Simulation.Step</c> 앞).</summary>
    public void BeginTick() => _tickStart = _clock();

    public void EndTick()
    {
        var d = _clock() - _tickStart;
        _tickSum += d;
        if (d > _tickMax) _tickMax = d;
        _ticks++;
        if (_appSeconds >= SummarySkipSeconds) _tickMs.Add(d * _msPerTick);
    }

    /// <summary>프레임 하나(Present 직전). <paramref name="appSeconds"/> 는 요약의 앞 5초 빼기에 쓴다.</summary>
    public void Frame(FrameSample s, double appSeconds)
    {
        _appSeconds = appSeconds;
        _frames++;
        if (s.FrameMs > SlowFrameMs) _slowFrames++;
        _frameSum += s.FrameMs;
        _drawSum += s.DrawMs;
        _fogSum += s.FogMs;
        _drawCallSum += s.DrawCalls;
        if (appSeconds >= SummarySkipSeconds)
        {
            _frameMs.Add(s.FrameMs);
            _drawCalls.Add(s.DrawCalls);
        }
    }

    /// <summary>지난 줄 뒤의 구간을 CSV 한 줄로 내고 구간을 비운다(1초마다).</summary>
    public string Row(double appSeconds, double stageSeconds, PerfCounts counts, long droppedTicks)
    {
        var ic = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.Append(ic, $"{appSeconds:0.###},{stageSeconds:0.###},{counts.Enemies},{counts.HeroProjectiles},{counts.EnemyProjectiles},{counts.Hazards}");
        var t = Math.Max(_ticks, 1);
        sb.Append(ic, $",{_tickSum * _msPerTick / t:0.####},{_tickMax * _msPerTick:0.####}");
        for (var i = 0; i < _phaseSum.Length; i++) sb.Append(ic, $",{_phaseSum[i] * _msPerTick / t:0.####}");
        var f = Math.Max(_frames, 1);
        var alloc = GC.GetTotalAllocatedBytes();
        sb.Append(ic, $",{_drawSum / f:0.###},{_fogSum / f:0.###},{_frameSum / f:0.###},{_slowFrames},{_drawCallSum / f},{alloc - _allocMark},{droppedTicks}");
        _allocMark = alloc;
        Array.Clear(_phaseSum);
        _tickSum = _tickMax = 0;
        _ticks = _frames = _slowFrames = 0;
        _frameSum = _drawSum = _fogSum = 0;
        _drawCallSum = 0;
        return sb.ToString();
    }

    /// <summary>끝 요약(앞 5초를 뺀 것): <c>PERF frame_ms p50=… p95=… p99=… max=… tick_ms … draw_calls …</c>.</summary>
    public string Summary() =>
        $"PERF frames={_frameMs.Count} frame_ms {Percentiles(_frameMs)} tick_ms {Percentiles(_tickMs)} draw_calls {Percentiles(_drawCalls.ConvertAll(x => (double)x))}";

    /// <summary>가장 가까운 순위 방식(nearest-rank). 빈 목록은 <c>p50=- …</c>.</summary>
    public static string Percentiles(IReadOnlyList<double> values)
    {
        if (values.Count == 0) return "p50=- p95=- p99=- max=-";
        var sorted = values.Order().ToArray();
        string P(double q) => sorted[Math.Clamp((int)Math.Ceiling(q * sorted.Length) - 1, 0, sorted.Length - 1)].ToString("0.###", CultureInfo.InvariantCulture);
        return $"p50={P(0.50)} p95={P(0.95)} p99={P(0.99)} max={sorted[^1].ToString("0.###", CultureInfo.InvariantCulture)}";
    }
}
