namespace DiabloDefenceRemastered.Core.Common;

/// <summary>한 프레임 처리 결과.</summary>
/// <param name="Ticks">이번 프레임에 실제로 돈 앱 틱 수.</param>
/// <param name="Stopped">캡처 멈춤 지점에 닿아 남은 틱을 미뤘다. 이번 프레임에 캡처한다(alpha = 1).</param>
public readonly record struct FrameTicks(int Ticks, bool Stopped);

/// <summary>
/// 고정 60Hz 틱 누적기 (설계서 3.2). 그래픽과 무관해 Core 에서 시험한다. 게임 FrameLoop 은 이것을 부르기만 한다.
/// - real = min(경과, 0.25s), acc += real × timeScale, 한 프레임 최대 8 × timeScale 틱. 넘는 만큼은 버리고 <see cref="DroppedTicks"/> 에 센다.
/// - 캡처 멈춤 지점(<see cref="AddStop"/>)에 닿으면 그 앱 틱을 돌기 전에 멈추고 남은 틱을 다음 프레임으로 미룬다.
/// 규칙·캡처·종료가 모두 앱 틱 기준이라 timeScale 이나 프레임 묶음이 달라도 도는 앱 틱 열은 같다.
/// </summary>
public sealed class TickAccumulator(double maxElapsedSeconds = 0.25, int maxTicksPerFrame = 8)
{
    public const double TickSeconds = 1.0 / 60.0;
    private const double BoundaryEpsilon = 1e-3;

    private double _accTicks;               // 쌓인 틱(소수 포함)
    private int _deferred;                  // 멈춤 때문에 미룬 틱
    private readonly List<long> _stops = []; // 오름차순, 아직 안 쓴 멈춤 지점

    /// <summary>다음에 돌 앱 틱 번호 = 지금까지 돈 앱 틱 수.</summary>
    public long AppTick { get; private set; }

    /// <summary>상한 때문에 버린 틱 수(로그 dropped_ticks).</summary>
    public long DroppedTicks { get; private set; }

    /// <summary>보간 값 = 남은 누적 / 틱 길이 [0, 1).</summary>
    public float Alpha => (float)Math.Clamp(_accTicks - Math.Floor(_accTicks), 0.0, 1.0);

    /// <summary>캡처 멈춤 지점. 그 앱 틱을 돌기 직전 상태에서 멈춘다(예: --capture-at 1 → 앱 틱 60).</summary>
    public void AddStop(long appTick)
    {
        if (appTick < AppTick) return;
        var i = _stops.BinarySearch(appTick);
        if (i < 0) _stops.Insert(~i, appTick);
    }

    /// <summary>앱 틱이 멈춤 지점이면 그 지점을 쓰고 true. 한 지점은 한 번만 멈춘다.</summary>
    public bool StopAt(long appTick)
    {
        if (_stops.Count == 0 || _stops[0] != appTick) return false;
        _stops.RemoveAt(0);
        return true;
    }

    /// <summary>경과 시간을 쌓고 이번 프레임에 돌 틱 수를 정한다(미뤘던 틱 포함).</summary>
    public int Advance(TimeSpan elapsed, double timeScale)
    {
        if (timeScale <= 0) timeScale = 1;
        var real = Math.Clamp(elapsed.TotalSeconds, 0.0, maxElapsedSeconds);
        _accTicks += real * timeScale / TickSeconds;

        // TimeSpan 은 100ns 단위라 1/60s 가 0.999996틱이 된다 → 틱 경계 바로 아래는 경계로 친다(남는 음수는 버림)
        var available = (long)Math.Floor(_accTicks + BoundaryEpsilon);
        _accTicks -= available;
        if (_accTicks < 0) _accTicks = 0;

        var limit = (long)Math.Ceiling(maxTicksPerFrame * timeScale);
        var total = available + _deferred;
        _deferred = 0;
        if (total > limit)
        {
            DroppedTicks += total - limit;
            total = limit;
        }
        return (int)total;
    }

    /// <summary>남은 틱을 다음 프레임으로 미룬다(캡처 멈춤).</summary>
    public void Defer(int ticks)
    {
        if (ticks > 0) _deferred += ticks;
    }

    /// <summary>
    /// 한 프레임: <see cref="Advance"/> 후 틱마다 멈춤 지점을 보고 <paramref name="tick"/>(앱 틱 번호)를 부른다.
    /// 마지막 틱 뒤에 멈춤 지점에 닿아도 이번 프레임에 멈춘다.
    /// </summary>
    public FrameTicks RunFrame(TimeSpan elapsed, double timeScale, Action<long> tick)
    {
        var n = Advance(elapsed, timeScale);
        for (var i = 0; i < n; i++)
        {
            if (StopAt(AppTick))
            {
                Defer(n - i);
                return new FrameTicks(i, true);
            }
            tick(AppTick);
            AppTick++;
        }
        return new FrameTicks(n, StopAt(AppTick));
    }
}
