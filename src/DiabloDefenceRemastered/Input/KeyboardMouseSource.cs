namespace DiabloDefenceRemastered.Input;

/// <summary>
/// 사람 조작 명령 (설계서 11.1 WP6 계약, 기획 3절): 키보드 이동(WASD·방향키) 우선, 키 이동이 없고 왼쪽 버튼을 누르고 있으면 커서 쪽으로 걷기.
/// <c>PlayScene</c> 이 앱 틱마다 <see cref="Frame"/> 에 그 틱의 <see cref="InputFrame"/> 을 넣고 <see cref="Next"/> 를 부른다.
/// 마우스 창 좌표 → 월드는 <see cref="AppServices.Render"/>.<c>ScreenToWorld</c>(WP5) 하나 — 그리기 맥락이 없으면(그래픽 없는 실행) 마우스 이동은 없다.
/// </summary>
public sealed class KeyboardMouseSource(AppServices app) : IInputSource
{
    /// <summary>커서가 이 거리(px) 안이면 서 있는다 — 커서 위에서 떨지 않게.</summary>
    public const float MouseDeadZone = 6f;

    /// <summary>이번 앱 틱의 입력(<c>PlayScene</c> 이 넣음).</summary>
    public InputFrame? Frame { get; set; }

    /// <summary>창 좌표 → 월드(시험이 바꿔 끼움). 기본은 <see cref="AppServices.Render"/>.</summary>
    public Func<Microsoft.Xna.Framework.Point, Vector2?> ScreenToWorld { get; set; } =
        p => app.Render?.ScreenToWorld(p);

    public PlayerCommand Next(World world)
    {
        if (Frame is not { } f) return PlayerCommand.None;
        var move = f.Move;
        if (move == Vector2.Zero && f.MouseDown && f.MouseInWindow && ScreenToWorld(f.MouseScreen) is { } target)
            move = Toward(world.Hero.Pos, target);
        return new PlayerCommand(move);
    }

    /// <summary>주인공 → 커서 단위 벡터(데드존 안이면 0).</summary>
    public static Vector2 Toward(Vector2 from, Vector2 to)
    {
        var d = to - from;
        var len = d.Length();
        return len <= MouseDeadZone || !float.IsFinite(len) ? Vector2.Zero : d / len;
    }
}
