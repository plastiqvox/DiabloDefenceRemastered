namespace DiabloDefenceRemastered.Core.Audio;

/// <summary>
/// 소리 모듈: 소리 연결 표 검사기(6.14 "소리" 행), 고르기 규칙 <c>SoundMixer</c> 는 같은 폴더 (소유 WP8). WP1b 가 만든 빈 연결 파일.
/// 시뮬레이션(<c>Core/Sim</c>)은 이 폴더를 참조하지 않는다 — 소리를 켜든 끄든 같은 씨앗이면 같은 결과(7.9).
/// </summary>
public sealed class AudioModule : IGameModule
{
    public void Register(ModuleContext ctx)
    {
    }
}
