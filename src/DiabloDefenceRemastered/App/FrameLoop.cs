namespace DiabloDefenceRemastered.App;

/// <summary>
/// 프레임 하나 (설계서 3.2, WP7). 경과 시간을 <see cref="TickAccumulator"/> 에 쌓아 앱 틱을 돌린다:
/// 사람 입력은 프레임마다 한 번 읽어 그 프레임의 모든 앱 틱에 같은 이동을 주고, 한 번만 일어나야 하는 버튼은 첫 틱에만(그 뒤 <see cref="InputFrame.ClearPressed"/>).
/// <c>--autoplay</c> 면 입력은 늘 비고, 틱마다 <see cref="SceneStack.AutoStep"/>(메뉴 넘김)을 먼저 부른다.
/// 캡처 멈춤 지점(<see cref="TickAccumulator.AddStop"/>)에 닿으면 남은 틱을 미루고 <see cref="FrameTicks.Stopped"/> — 그 프레임을 alpha = 1 로 그려 캡처한다.
/// </summary>
public sealed class FrameLoop(SceneStack stack, double timeScale, IAutoplayDriver? bot = null)
{
    public TickAccumulator Accumulator { get; } = new();
    public SceneStack Stack { get; } = stack;
    public InputFrame Input { get; } = new();
    public double TimeScale { get; } = timeScale;

    /// <summary>다음에 돌 앱 틱.</summary>
    public AppTick Now => new(Accumulator.AppTick);

    /// <summary>앱 틱마다(씬이 돈 뒤) — DevRunner 의 종료 판정·소리 갱신이 건다.</summary>
    public event Action<AppTick>? Ticked;

    /// <summary>
    /// 한 프레임의 틱을 돈다. <paramref name="readInput"/> 은 프레임마다 한 번(사람 입력 — 자동 조작이면 null 을 넘겨 빈 입력).
    /// </summary>
    public FrameTicks RunFrame(TimeSpan elapsed, Action<InputFrame>? readInput)
    {
        Input.Clear();
        readInput?.Invoke(Input);
        var first = true;
        return Accumulator.RunFrame(elapsed, TimeScale, tick =>
        {
            var t = new AppTick(tick);
            if (bot is not null) Stack.AutoStep(bot);
            Stack.FixedTick(t, Input);
            if (first)
            {
                Input.ClearPressed();
                first = false;
            }
            Ticked?.Invoke(t);
        });
    }
}
