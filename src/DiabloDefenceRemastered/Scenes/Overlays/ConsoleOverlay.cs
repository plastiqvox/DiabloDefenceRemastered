using DiabloDefenceRemastered.Core.Dev;
using DiabloDefenceRemastered.Rendering;
using DiabloDefenceRemastered.UI;

namespace DiabloDefenceRemastered.Scenes.Overlays;

/// <summary>
/// 치트 콘솔(2026-11-15 사용자 요청 "임시로 패턴보고 싶은데 치트키로 넘어가게", "`로 열려하는건? 퀘이크처럼") — 임시. 판에서 ` 로 열고 ` 나 Esc 로 닫는다.
/// 열린 동안 판은 멈춘다. 글자는 영문·숫자·띄어쓰기(<see cref="InputFrame.Typed"/>), Backspace 지우기, Enter 실행(Space 는 띄어쓰기만).
/// P 는 일시정지 대신 글자로 들어간다(Esc 는 Pause|Cancel 둘 다, P 는 Pause 만, Backspace 는 Cancel 만이라 셋을 가른다).
/// 명령은 <see cref="CheatCommands"/>. 방을 옮기거나 클리어하면 닫는다.
/// </summary>
public sealed class ConsoleOverlay(GameFlow flow, PlayScene play) : IScene
{
    public const int MaxLine = 40;
    public const int HistoryLines = 6;

    private SceneStack? _stack;
    private string _line = "";

    public bool PausesBelow => true;
    public bool DrawsBelow => true;
    public string Line => _line;

    public void Enter(SceneStack stack)
    {
        _stack = stack;
        flow.LogScene("Console");
    }

    public void Exit() { }

    public void FixedTick(in AppTick t, InputFrame input)
    {
        flow.Now = t;
        var pause = input.IsPressed(UiButtons.Pause);
        var cancel = input.IsPressed(UiButtons.Cancel);
        if (input.IsPressed(UiButtons.Console) || (pause && cancel))
        {
            _stack?.Pop();
            return;
        }
        if (cancel && _line.Length > 0) _line = _line[..^1];
        if (input.Typed.Length > 0) _line = (_line + input.Typed)[..Math.Min(MaxLine, _line.Length + input.Typed.Length)];
        if (input.IsPressed(UiButtons.Confirm) && !input.IsPressed(UiButtons.Blink)) Submit();
    }

    /// <summary>지금 줄을 실행(시험용으로 공개).</summary>
    public void Submit(string? line = null)
    {
        line ??= _line;
        _line = "";
        if (line.Trim().Length == 0) return;
        var result = CheatCommands.Run(play.Session, line);
        play.ConsoleLog.Add("> " + line.Trim());
        if (result.Message.Length > 0) play.ConsoleLog.Add(result.Message);
        flow.Log.Info($"CHEAT \"{line.Trim()}\" → {result.Message}");
        if (result.WarpTo is { } index)
        {
            _stack?.Pop();
            play.CheatWarp(index);
        }
        else if (line.Trim().Equals("clear", StringComparison.OrdinalIgnoreCase) && result.Message == "방 클리어") _stack?.Pop();
    }

    public void Draw(RenderContext rc, float alpha)
    {
        if (flow.Canvas is not { } c) return;
        c.Begin(rc);
        const float top = 0f, height = 150f, x = 16f;
        c.Fill(new RectF(0, top, Theme.Width, top + height), Theme.Black, 0.82f);
        c.Fill(new RectF(0, top + height - 2, Theme.Width, top + height), Theme.DarkRed, 1f);
        var log = play.ConsoleLog;
        var y = top + 10f;
        for (var i = Math.Max(0, log.Count - HistoryLines); i < log.Count; i++)
        {
            c.Text(log[i], new Vector2(x, y), Theme.BodyText, log[i].StartsWith('>') ? Theme.TextDim : Theme.Text);
            y += 18f;
        }
        var caret = (flow.Now.Value / 30) % 2 == 0 ? "_" : " ";
        c.Text("> " + _line + caret, new Vector2(x, top + height - 26f), Theme.BodyText, Theme.Gold);
        c.End();
    }
}
