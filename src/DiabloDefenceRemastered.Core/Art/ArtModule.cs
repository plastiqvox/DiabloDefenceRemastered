namespace DiabloDefenceRemastered.Core.Art;

/// <summary>
/// 그림 모듈 (소유 WP5): 실제 자원 대조 검사기 13.5-7(<see cref="ArtAssetValidator"/>)을 등록한다. 자원이 없으면 그 검사기는 아무것도 하지 않는다.
/// 자원 뿌리 기본값 = 실행 폴더의 <c>Assets</c>(게임 출력 — csproj 가 복사한다, 1.3). 시험 출력에는 <c>Assets</c> 가 없어 늘 건너뛴다 —
/// 시험은 <c>DDR_ASSETS</c> 로 찾은 뿌리를 직접 넘겨 <see cref="ArtAssetValidator"/> 를 만든다(게임은 <c>DDR_ASSETS</c> 를 읽지 않는다, 1.3).
/// </summary>
public sealed class ArtModule(string? assetRoot = null) : IGameModule
{
    public static string DefaultAssetRoot => Path.Combine(AppContext.BaseDirectory, "Assets");

    public void Register(ModuleContext ctx) => ctx.AddValidator(new ArtAssetValidator(assetRoot ?? DefaultAssetRoot));
}
