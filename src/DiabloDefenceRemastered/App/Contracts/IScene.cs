namespace DiabloDefenceRemastered.App.Contracts;

/// <summary>앱 틱(프로그램 시작부터 센 60Hz 틱, 설계서 3.1). 메뉴·오버레이·연출·--capture-at·--quit-after 의 기준.</summary>
public readonly record struct AppTick(long Value)
{
    public double Seconds => Value / 60.0;

    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}

/// <summary>
/// 씬·오버레이 (설계서 8.1, 상태 패턴). 위 씬이 입력을 먼저 받고, <see cref="PausesBelow"/> 가 참이면 아래 씬은 진행하지 않는다.
/// </summary>
public interface IScene
{
    bool PausesBelow { get; }    // 오버레이가 아래 씬의 FixedTick 을 막나
    bool DrawsBelow  { get; }    // 아래 씬을 그 뒤에 그리나
    void Enter(SceneStack stack); void Exit();
    void FixedTick(in AppTick t, InputFrame input);   // 60Hz, 위에서부터
    void Draw(RenderContext rc, float alpha);
    void AutoStep(IAutoplayDriver bot) { }             // --autoplay 가 메뉴를 넘길 때
}

/// <summary>
/// 자동 조작 메뉴 넘김(WP6 AutoplayDriver 가 구현). 씬은 이것에 물어 정해진 앱 틱 수 뒤에 다음으로 넘긴다.
/// 카드 고르기는 Core <c>AutoplayPolicy.PickCard</c>(WP4) 의 답을 쓴다.
/// </summary>
public interface IAutoplayDriver
{
    AppTick Now { get; }

    /// <summary>그 씬이 떠 있은 지 메뉴 대기 시간(0.5s)이 지났나.</summary>
    bool Ready(IScene scene);

    int PickCard(LevelUpOffer offer);
}
