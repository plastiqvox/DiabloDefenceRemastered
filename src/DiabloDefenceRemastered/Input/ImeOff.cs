using System.Runtime.InteropServices;

namespace DiabloDefenceRemastered.Input;

/// <summary>
/// 게임 창의 입력기(IME) 끄기(2026-11-15 사용자 보고 "한글 할때는 입력안됨" — 치트 콘솔). 한글 입력기가 켜져 있으면 글자 키가 입력기로 가서
/// 키 상태(<see cref="InputMap"/>)에 영문 자리가 잡히지 않았다. MonoGame DesktopGL 은 SDL2 위에 있으므로 <c>SDL_StopTextInput</c> 으로 이 창의 입력기를 끈다
/// (SDL 이 창의 입력기 문맥을 떼어 낸다 — 다른 프로그램의 한/영 상태는 그대로). 게임에는 한글을 쳐야 하는 곳이 없다.
/// 창이 다시 앞으로 올 때·콘솔을 열 때 한 번 더 부른다. SDL2 를 못 찾으면 조용히 넘어간다(로그 한 줄).
/// </summary>
public static class ImeOff
{
    [DllImport("SDL2", EntryPoint = "SDL_StopTextInput", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SdlStopTextInput();

    private static bool _failed;

    public static void Apply(IAppLog? log = null)
    {
        if (_failed) return;
        try { SdlStopTextInput(); }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            _failed = true;
            log?.Warn($"IME 끄기 못 함({e.GetType().Name}) — 한글 입력 상태면 콘솔 글자가 안 들어갈 수 있다");
        }
    }
}
