using Microsoft.Xna.Framework.Input;
using ButtonState = Microsoft.Xna.Framework.Input.ButtonState;
using Point = Microsoft.Xna.Framework.Point;

namespace DiabloDefenceRemastered.Input;

/// <summary>
/// 키·버튼 → <see cref="InputFrame"/> (기획 3절 키, 설계서 3.2). <c>FrameLoop</c>(WP7)이 프레임마다 한 번 <see cref="Sample"/> 을 부른다 — <c>--autoplay</c> 면 부르지 않는다(장치를 읽지 않음).
/// 한 번만 일어나야 하는 버튼은 "이번에 눌림"(앞 프레임에 안 눌림 → 이번 프레임에 눌림)만 <see cref="InputFrame.Pressed"/> 에 넣는다.
/// 장치 상태 → 프레임 변환은 <see cref="Map"/>(순수 함수)로 나눠 그래픽·창 없이 돈다.
/// </summary>
public sealed class InputMap
{
    private KeyboardState _prevKeys;
    private bool _prevLeft;
    private bool _first = true;

    /// <summary>
    /// 장치를 읽어 <paramref name="frame"/> 을 채운다. <paramref name="windowSize"/> = 창 클라이언트 크기(마우스가 창 안인지), <paramref name="active"/> = 창이 앞에 있음
    /// (비활성 창이면 키·버튼을 읽지 않고 이동을 0 으로 — 다른 창에 치는 키가 게임에 들어가지 않게).
    /// </summary>
    public void Sample(InputFrame frame, Point windowSize, bool active)
    {
        var keys = active ? Keyboard.GetState() : default;
        var mouse = active ? Mouse.GetState() : default;
        Map(frame, keys, _first ? keys : _prevKeys, mouse.LeftButton == ButtonState.Pressed, _prevLeft, mouse.Position, windowSize);
        _prevKeys = keys;
        _prevLeft = mouse.LeftButton == ButtonState.Pressed;
        _first = false;
    }

    /// <summary>
    /// 키 상태 둘(이번·앞)과 마우스로 프레임 하나를 만든다. 이동은 WASD·방향키(8방향, 대각선 정규화), 반대 키를 같이 누르면 그 축은 0.
    /// </summary>
    public static void Map(InputFrame frame, KeyboardState keys, KeyboardState prev, bool leftDown, bool prevLeftDown, Point mouse, Point windowSize)
    {
        var x = 0f;
        var y = 0f;
        if (keys.IsKeyDown(Keys.A) || keys.IsKeyDown(Keys.Left)) x -= 1f;
        if (keys.IsKeyDown(Keys.D) || keys.IsKeyDown(Keys.Right)) x += 1f;
        if (keys.IsKeyDown(Keys.W) || keys.IsKeyDown(Keys.Up)) y -= 1f;
        if (keys.IsKeyDown(Keys.S) || keys.IsKeyDown(Keys.Down)) y += 1f;
        var move = new Vector2(x, y);
        frame.Move = move.LengthSquared() > 1f ? Vector2.Normalize(move) : move;

        frame.MouseScreen = mouse;
        frame.MouseInWindow = mouse.X >= 0 && mouse.Y >= 0 && mouse.X < windowSize.X && mouse.Y < windowSize.Y;
        frame.MouseDown = leftDown && frame.MouseInWindow;

        var pressed = UiButtons.None;
        bool Hit(Keys k) => keys.IsKeyDown(k) && !prev.IsKeyDown(k);
        if (Hit(Keys.W) || Hit(Keys.Up)) pressed |= UiButtons.Up;
        if (Hit(Keys.S) || Hit(Keys.Down)) pressed |= UiButtons.Down;
        if (Hit(Keys.A) || Hit(Keys.Left)) pressed |= UiButtons.Left;
        if (Hit(Keys.D) || Hit(Keys.Right)) pressed |= UiButtons.Right;
        var alt = keys.IsKeyDown(Keys.LeftAlt) || keys.IsKeyDown(Keys.RightAlt);
        if (Hit(Keys.Enter) && alt) pressed |= UiButtons.Fullscreen;
        else if (Hit(Keys.Enter) || Hit(Keys.Space)) pressed |= UiButtons.Confirm;
        if (Hit(Keys.F11)) pressed |= UiButtons.Fullscreen;
        if (Hit(Keys.Escape)) pressed |= UiButtons.Pause | UiButtons.Cancel;
        if (Hit(Keys.P)) pressed |= UiButtons.Pause;
        if (Hit(Keys.Back)) pressed |= UiButtons.Cancel;
        if (Hit(Keys.D1) || Hit(Keys.NumPad1)) pressed |= UiButtons.Choice1;
        if (Hit(Keys.D2) || Hit(Keys.NumPad2)) pressed |= UiButtons.Choice2;
        if (Hit(Keys.D3) || Hit(Keys.NumPad3)) pressed |= UiButtons.Choice3;
        if (Hit(Keys.R)) pressed |= UiButtons.Reroll;
        if (Hit(Keys.X)) pressed |= UiButtons.Skip;
        if (Hit(Keys.Tab)) pressed |= UiButtons.Minimap;
        if (leftDown && !prevLeftDown && frame.MouseInWindow) pressed |= UiButtons.Click;
        frame.Pressed = pressed;
    }
}
