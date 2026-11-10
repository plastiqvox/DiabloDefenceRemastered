using Microsoft.Xna.Framework;

namespace DiabloDefenceRemastered.App;

/// <summary>
/// 빈 시험 씬 (WP7 — 단계 1 완료 기준 "빈 씬으로 창이 뜨고 캡처"). 한 색으로 지우기만 한다.
/// 시작 씬을 만드는 <c>SceneFactory</c>(WP6) 본문이 아직 없을 때 GameApp 이 이것으로 뜬다 — 앱 셸(프레임 루프·캡처·종료·감시)을 다른 묶음 없이 확인하는 길.
/// </summary>
public sealed class BlankScene : IScene
{
    /// <summary>지우는 색(어두운 갈색 — 화면이 떴는지 캡처로 알아보기 쉽게, 검정이 아님).</summary>
    public static readonly Color Background = new(24, 18, 14);

    public bool PausesBelow => true;
    public bool DrawsBelow => false;
    public long Ticks { get; private set; }

    public void Enter(SceneStack stack) { }
    public void Exit() { }
    public void FixedTick(in AppTick t, InputFrame input) => Ticks++;
    public void Draw(RenderContext rc, float alpha) => rc.Device.Clear(Background);
}
