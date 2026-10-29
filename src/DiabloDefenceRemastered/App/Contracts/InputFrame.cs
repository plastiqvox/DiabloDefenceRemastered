using Point = Microsoft.Xna.Framework.Point;

namespace DiabloDefenceRemastered.App.Contracts;

/// <summary>화면 조작 버튼(기획 3). 시뮬레이션 명령은 <see cref="PlayerCommand"/> 로 따로 간다.</summary>
[Flags]
public enum UiButtons : uint
{
    None = 0,
    Up = 1 << 0,
    Down = 1 << 1,
    Left = 1 << 2,
    Right = 1 << 3,
    Confirm = 1 << 4,     // Enter / Space
    Cancel = 1 << 5,
    Pause = 1 << 6,       // Esc / P
    Choice1 = 1 << 7,
    Choice2 = 1 << 8,
    Choice3 = 1 << 9,
    Reroll = 1 << 10,     // R
    Skip = 1 << 11,       // X
    Minimap = 1 << 12,    // Tab
    Fullscreen = 1 << 13, // F11 / Alt+Enter
    Click = 1 << 14,      // 왼쪽 버튼을 이번에 눌렀다
}

/// <summary>
/// 프레임 하나의 입력 (설계서 3.2). 사람 입력은 프레임마다 한 번 읽어 그 프레임의 모든 앱 틱에 같은 이동을 주고,
/// 한 번만 일어나야 하는 버튼(<see cref="Pressed"/>)은 첫 틱에만 넣는다(FrameLoop 이 첫 틱 뒤 <see cref="ClearPressed"/>).
/// --autoplay 면 실제 장치를 읽지 않으므로 늘 비어 있다.
/// </summary>
public sealed class InputFrame
{
    /// <summary>키보드 이동(길이 ≤ 1, 대각선 정규화).</summary>
    public Vector2 Move { get; set; }

    public Point MouseScreen { get; set; }
    public bool MouseInWindow { get; set; }
    /// <summary>왼쪽 버튼을 누르고 있다(커서 쪽으로 걷기).</summary>
    public bool MouseDown { get; set; }

    /// <summary>이번 틱에 새로 눌린 버튼.</summary>
    public UiButtons Pressed { get; set; }

    public bool IsPressed(UiButtons button) => (Pressed & button) != 0;

    public void ClearPressed() => Pressed = UiButtons.None;

    public void Clear()
    {
        Move = Vector2.Zero;
        MouseScreen = Point.Zero;
        MouseInWindow = false;
        MouseDown = false;
        Pressed = UiButtons.None;
    }
}
