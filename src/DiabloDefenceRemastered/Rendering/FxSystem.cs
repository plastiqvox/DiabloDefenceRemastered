namespace DiabloDefenceRemastered.Rendering;

/// <summary>
/// 표시 이펙트 (설계서 5.6, 소유 WP5). 표시 이벤트 버퍼의 이펙트·피해 숫자·시체·흔들림을 가져가 풀에서 재생한다(나이 = stageTick − 항목 Tick).
/// 인스턴스는 <see cref="WorldRenderer.Fx"/>. 버퍼 비우기는 부르는 쪽(PlayScene)이 Consume 뒤에 한다.
/// WP1 이 시그니처만 만든 빈 연결 파일 — 본문은 WP5 가 채운다.
/// </summary>
public sealed class FxSystem
{
    public void Consume(PresentationBuffer buffer, int stageTick) => throw new NotImplementedException("WP5 FxSystem.Consume");
}
