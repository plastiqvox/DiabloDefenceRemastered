namespace DiabloDefenceRemastered.Core.Audio;

/// <summary>
/// 소리 모듈 (소유 WP8, 설계서 6.14 "소리" 행·7.9): 소리 데이터 검사기(<see cref="AudioDataValidator"/>)를 등록한다.
/// 고르기(<see cref="SoundMixer"/>)·신호 → 소리(<see cref="SoundRouter"/>)·장판 반복(<see cref="HazardLoops"/>)·미리 읽기 목록(<see cref="AudioReach"/>)은
/// 같은 폴더의 순수 로직이고 게임 <c>Audio/</c> 가 쓴다. 월드 설치·개발 옵션 적용은 없다 — 소리는 시뮬레이션에 들어가지 않는다.
/// 시뮬레이션(<c>Core/Sim</c>)은 이 폴더를 참조하지 않는다 — 소리를 켜든 끄든 같은 씨앗이면 같은 결과(7.9).
/// </summary>
public sealed class AudioModule : IGameModule
{
    public void Register(ModuleContext ctx)
    {
        ctx.AddValidator(new AudioDataValidator());
    }
}
