namespace DiabloDefenceRemastered.Core.Sim.Contracts;

/// <summary>--weapons / --passives 한 칸(<c>id:lv</c>).</summary>
public readonly record struct LoadoutEntry(string Id, int Level);

/// <summary>
/// 시뮬레이션에 걸리는 개발 옵션 (설계서 9절 중 월드에 적용하는 것). WP7 DevOptions 가 파싱해 이것으로 넘기고,
/// 적용은 <see cref="IRunSetup"/> 을 등록한 묶음이 한다(WP3: 주인공 쪽, WP4: 시각·부하·AI 멈춤, 11.3).
/// </summary>
public sealed record RunOptions
{
    public static RunOptions Default { get; } = new();

    public bool God { get; init; }
    public int? Level { get; init; }
    public IReadOnlyList<LoadoutEntry> Weapons { get; init; } = [];
    public IReadOnlyList<LoadoutEntry> Passives { get; init; } = [];
    public ClockTime? StartAt { get; init; }
    public int? StressEnemies { get; init; }
    public int? StressProjectiles { get; init; }
    public bool FreezeAi { get; init; }
    /// <summary>불변식 계측(--check).</summary>
    public bool Check { get; init; }
}

/// <summary>개발 옵션 적용. 월드를 만들고 첫 틱 전에 등록 순서대로 부른다.</summary>
public interface IRunSetup
{
    void Apply(World world, RunOptions options);
}

[Flags]
public enum CommandButtons : ushort
{
    None = 0,
    Pause = 1 << 0,
    Confirm = 1 << 1,
    Cancel = 1 << 2,
    Choice1 = 1 << 3,
    Choice2 = 1 << 4,
    Choice3 = 1 << 5,
    Reroll = 1 << 6,
    Skip = 1 << 7,
    Minimap = 1 << 8,
}

/// <summary>
/// 틱 하나의 명령 (설계서 2.1 명령 패턴): 이동 벡터(길이 ≤ 1) + 버튼 비트. 시뮬레이션은 입력 장치를 모른다.
/// 한 번만 일어나야 하는 버튼은 프레임의 첫 틱에만 넣는다(3.2).
/// </summary>
public readonly record struct PlayerCommand(Vector2 Move, CommandButtons Buttons = CommandButtons.None)
{
    public static PlayerCommand None => default;
}

/// <summary>명령 출처: 키보드·마우스(WP6) / 자동 조작 봇. 봇은 틱마다 월드를 보고 만든다.</summary>
public interface IInputSource
{
    PlayerCommand Next(World world);
}
