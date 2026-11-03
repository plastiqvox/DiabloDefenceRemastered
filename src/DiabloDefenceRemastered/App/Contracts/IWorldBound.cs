namespace DiabloDefenceRemastered.App.Contracts;

/// <summary>
/// 게임 쪽 "월드 바뀜" 계약 (v0.7.1, 설계서 7.11 — 계약 WP1e-형). Core 는 방마다 새 <c>World</c> 라 저절로 새로 시작하지만 게임 쪽 서비스는 월드를 넘어 산다.
/// 그대로 두면 ① 시체·피해 숫자·이펙트가 옛 방의 "생긴 틱" 을 들고 있어 새 <c>World.Tick</c>(0)에서 나이가 음수가 되어 안 사라지고
/// ② <c>EntityId</c> 가 새 저장소에서 다시 쓰여 개체 키 소리·쿨다운·Fx 붙임이 다른 개체에 붙고 ③ 안개·미니맵·맵 그림이 옛 맵을 가리킨다.
/// 묶음마다 따로 고치면 빠뜨리기 쉬우므로 한 곳에 둔다.
/// <para>
/// <c>PlayScene</c> 이 <c>RunSession.Advance()</c> 직전에 옛 월드의 <c>Presentation</c> 을 <c>FxSystem.Consume</c> → <c>Audio.Consume</c> 으로 소비하고 <c>Clear</c> 한 뒤,
/// 직후에 <b>이 순서로</b> <see cref="Attach"/> 를 부른다: <c>WorldRenderer</c>(WP5) → <c>FxSystem</c>(WP6) → <c>Hud</c>·미니맵(WP6) → <see cref="IAudioServices"/>(WP8).
/// 판을 처음 열 때도 같은 함수로 붙인다(첫 월드도 같은 길).
/// </para>
/// </summary>
public interface IWorldBound
{
    /// <summary>새 <c>World</c> 를 받음 = 그 월드에 묶인 상태를 모두 버리고 다시 만듦.</summary>
    void Attach(World world);
}
