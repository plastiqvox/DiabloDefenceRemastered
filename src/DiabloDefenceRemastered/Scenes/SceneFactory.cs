namespace DiabloDefenceRemastered.Scenes;

/// <summary>
/// 시작 씬 고르기(타이틀 / --stage 바로 시작 / --preview / DataError) (소유 WP6, PreviewScene 은 WP5).
/// GameApp(WP7)은 이 파일만 부른다. WP1 이 만든 빈 연결 파일.
/// </summary>
public static class SceneFactory
{
    public static IScene CreateStart(AppServices app) => throw new NotImplementedException("WP6 SceneFactory.CreateStart");

    public static IScene CreateDataError(AppServices app) => throw new NotImplementedException("WP6 SceneFactory.CreateDataError");
}
