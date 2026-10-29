namespace DiabloDefenceRemastered.Scenes;

/// <summary>
/// 그림 확인 씬(<c>--preview sprites/enemies/obstacles/effects</c>, 설계서 8.1·9, 소유 WP5). SceneFactory(WP6)가 고른다.
/// WP1 이 시그니처만 만든 빈 연결 파일 — WP6 이 먼저 부르고 컴파일한다. 본문은 WP5 가 채운다.
/// </summary>
public sealed class PreviewScene : IScene
{
    /// <param name="target"><see cref="AppOptions.Preview"/> 값.</param>
    public PreviewScene(AppServices app, string target) => throw new NotImplementedException("WP5 PreviewScene");

    public bool PausesBelow => false;
    public bool DrawsBelow => false;

    public void Enter(SceneStack stack) => throw new NotImplementedException("WP5 PreviewScene.Enter");
    public void Exit() => throw new NotImplementedException("WP5 PreviewScene.Exit");
    public void FixedTick(in AppTick t, InputFrame input) => throw new NotImplementedException("WP5 PreviewScene.FixedTick");
    public void Draw(RenderContext rc, float alpha) => throw new NotImplementedException("WP5 PreviewScene.Draw");
}
