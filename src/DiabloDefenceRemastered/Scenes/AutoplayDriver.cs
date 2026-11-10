using DiabloDefenceRemastered.Core.Sim.Autoplay;

namespace DiabloDefenceRemastered.Scenes;

/// <summary>
/// 자동 조작 메뉴 넘김 (설계서 8.1·9절 <c>--autoplay</c>, 계약 <see cref="IAutoplayDriver"/>). 판마다 Core <see cref="AutoplayPolicy"/> 하나를 만들어 가진다(v0.6.1 —
/// 월드가 방마다 바뀌어도 그대로, v0.7). 이동 명령은 <c>PlayScene</c> 이 <see cref="Policy"/>.<c>Next(world)</c> 로, 카드는 오버레이가 <see cref="PickCard"/> 로 받는다.
/// <para>
/// 앱(WP7 <c>FrameLoop</c>)은 앱 틱마다 씬 스택의 <c>FixedTick</c> 뒤에 <see cref="Step"/> 하나를 부른다 — <see cref="Now"/> 를 맞추고 맨 위 씬의 <c>AutoStep</c> 을 부른다.
/// 메뉴 대기 시간(<see cref="MenuDelayTicks"/> = 0.5s)은 그 씬이 처음 물어 온 앱 틱부터 센다.
/// </para>
/// </summary>
public sealed class AutoplayDriver(AutoplayVisit visit) : IAutoplayDriver
{
    /// <summary>각 화면 0.5s 뒤(9절 <c>--autoplay</c>).</summary>
    public const int MenuDelayTicks = World.TicksPerSecond / 2;

    private IScene? _waiting;
    private long _waitingSince;

    public AutoplayVisit Visit { get; } = visit;
    public AppTick Now { get; private set; }

    /// <summary>지금 판의 정책(판을 시작하기 전엔 null).</summary>
    public AutoplayPolicy? Policy { get; private set; }

    /// <summary>판 시작(<c>PlayScene</c> 이 판을 처음 열 때 — 이어 하기 포함). 판마다 정책 하나.</summary>
    public AutoplayPolicy BeginRun() => Policy = new AutoplayPolicy(Visit);

    /// <summary>앱 틱 하나: 시각을 맞추고 맨 위 씬에 메뉴 넘김을 묻는다.</summary>
    public void Step(SceneStack stack, AppTick now)
    {
        Now = now;
        stack.AutoStep(this);
    }

    /// <summary>시각만 맞춘다(<see cref="Step"/> 를 쓰지 않는 시험용).</summary>
    public void SetNow(AppTick now) => Now = now;

    public bool Ready(IScene scene)
    {
        if (!ReferenceEquals(scene, _waiting))
        {
            _waiting = scene;
            _waitingSince = Now.Value;
        }
        return Now.Value - _waitingSince >= MenuDelayTicks;
    }

    public int PickCard(LevelUpOffer offer)
    {
        if (offer.Cards.Count == 0) return -1;
        var pick = (Policy ?? BeginRun()).PickCard(offer);
        return Math.Clamp(pick, 0, offer.Cards.Count - 1);
    }
}
