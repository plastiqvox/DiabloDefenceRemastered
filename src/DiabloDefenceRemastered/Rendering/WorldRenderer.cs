namespace DiabloDefenceRemastered.Rendering;

/// <summary>
/// 월드 그리기 (설계서 7, 소유 WP5): 원작 타일 아레나(바닥 조각·벽 기둥·가림, 7.6)·유닛 y 정렬·빛·표시 이펙트·시체·피해 숫자.
/// PlayScene(WP6)이 만들고 프레임마다 <see cref="Fx"/>.Consume → <c>Audio.Consume</c>(7.9) → <c>world.Presentation.Clear()</c> → <see cref="Draw"/> 순으로 부른다.
/// WP1 이 시그니처만 만든 빈 연결 파일 — WP6 이 먼저 부르고 컴파일한다. 본문은 WP5 가 채운다.
/// </summary>
public sealed class WorldRenderer
{
    /// <summary>스테이지(월드) 하나에 하나. 맵 그림(<c>arena.json</c>)은 처음 그리는 월드의 <c>World.Arena</c> 를 보고 연다.</summary>
    public WorldRenderer(AppServices app) => throw new NotImplementedException("WP5 WorldRenderer");

    /// <summary>이 렌더러가 그리는 표시 이펙트.</summary>
    public FxSystem Fx => throw new NotImplementedException("WP5 WorldRenderer.Fx");

    public void Draw(World world, RenderContext rc, float alpha) => throw new NotImplementedException("WP5 WorldRenderer.Draw");
}
