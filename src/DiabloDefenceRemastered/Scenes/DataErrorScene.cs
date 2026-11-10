using DiabloDefenceRemastered.UI;

namespace DiabloDefenceRemastered.Scenes;

/// <summary>
/// 데이터 오류·자원 없음 안내 (설계서 8.1, 1.4 끝 — 기획 19.3). 무엇이 잘못됐는지 줄 목록 + (자원 안내면) 가져오기 명령 한 줄. 나가는 곳은 종료뿐(Enter/Esc → <see cref="GameFlow.Quit"/>).
/// 자동 실행에서는 앱(WP7)이 이 화면을 띄우지 않고 종료 코드로 끝낸다(9절) — 이 씬은 메뉴 넘김을 하지 않는다.
/// </summary>
public sealed class DataErrorScene(GameFlow flow, string title, IReadOnlyList<string> lines, string? command) : IScene
{
    /// <summary>화면에 보일 최대 줄(넘으면 <c>… 외 n개</c>).</summary>
    public const int MaxLines = 16;

    public bool PausesBelow => false;
    public bool DrawsBelow => false;
    public string Title => title;
    public IReadOnlyList<string> Lines => lines;
    public string? Command => command;

    /// <summary>데이터 오류 화면: 오류(경고 제외) 줄.</summary>
    public static DataErrorScene ForDataErrors(GameFlow flow)
    {
        var lines = new List<string>();
        foreach (var e in flow.App.DataErrors)
            if (e.Severity == DataSeverity.Error) lines.Add(e.ToString());
        return new DataErrorScene(flow, "데이터 오류", lines, null);
    }

    public void Enter(SceneStack stack) => flow.LogScene("DataError");

    public void Exit() { }

    public void FixedTick(in AppTick t, InputFrame input)
    {
        flow.Now = t;
        if (input.IsPressed(UiButtons.Confirm) || input.IsPressed(UiButtons.Cancel)) flow.Quit();
    }

    /// <summary>보일 줄(최대 <see cref="MaxLines"/>, 넘으면 마지막 줄이 <c>… 외 n개</c>).</summary>
    public static IReadOnlyList<string> Visible(IReadOnlyList<string> lines)
    {
        if (lines.Count <= MaxLines) return lines;
        var shown = lines.Take(MaxLines - 1).ToList();
        shown.Add($"… 외 {lines.Count - (MaxLines - 1)}개");
        return shown;
    }

    public void Draw(RenderContext rc, float alpha)
    {
        if (flow.Canvas is not { } c) return;
        c.Begin(rc);
        c.Fill(Ui.Screen, Theme.Black, 1f);
        c.Text(title, new Vector2(40, 30), Theme.TitleText, Theme.Red);
        var y = 90f;
        foreach (var line in Visible(lines))
        {
            c.Text(line, new Vector2(40, y), Theme.SmallText, Theme.Text);
            y += 20;
        }
        if (command is { } cmd)
        {
            y += 16;
            c.Text("가져오기를 돌린다:", new Vector2(40, y), Theme.BodyText, Theme.Gold);
            c.Text(cmd, new Vector2(40, y + 24), Theme.BodyText, Theme.Text);
        }
        Ui.Centered(c, "Enter — 끝내기", Theme.Width / 2, Theme.Height - 40, Theme.SmallText, Theme.TextDim);
        c.End();
    }
}
