namespace DiabloDefenceRemastered.Scenes;

/// <summary>
/// 시작 씬 고르기(타이틀 / --stage·--room 바로 시작 / --preview / DataError) (소유 WP6, PreviewScene 은 WP5).
/// GameApp(WP7)은 이 파일만 부른다. WP1 이 만든 빈 연결 파일.
/// v0.7(8.1): StageSelect 없음 — 새 게임 = <c>PlayScene(RunStart)</c>(직업 단계면 HeroSelect → Play). <c>PlayScene</c> 은 <c>RunSession</c> 을 가진다.
/// v0.7.1: <c>PlayScene</c> 은 <c>Advance</c> 앞뒤의 표시 이벤트 소비·<see cref="IWorldBound.Attach"/> 순서(7.11)와, <c>StageStep.Finished</c> 를 처음 받은 앱 틱에 한 번
/// <c>RunSession.Conclude()</c>(판 끝 정산 — 결과·<c>RESULT</c>·<c>RecordResult</c> 는 그 뒤의 <c>RunState</c> 만 읽음)를 지킨다.
/// </summary>
public static class SceneFactory
{
    public static IScene CreateStart(AppServices app) => throw new NotImplementedException("WP6 SceneFactory.CreateStart");

    public static IScene CreateDataError(AppServices app) => throw new NotImplementedException("WP6 SceneFactory.CreateDataError");
}
