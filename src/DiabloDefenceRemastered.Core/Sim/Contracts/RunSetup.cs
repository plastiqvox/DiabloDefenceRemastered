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
    /// <summary>--start-at(v0.7): 판의 <b>첫 방</b> 방 시계 시작(<c>RoomStart.StartClockTicks</c>, 3.6). 둘째 방부터는 쓰지 않는다. 방 없는 월드는 v0.6 대로 스테이지 시각.</summary>
    public ClockTime? StartAt { get; init; }
    public int? StressEnemies { get; init; }
    public int? StressProjectiles { get; init; }
    public bool FreezeAi { get; init; }
    /// <summary>불변식 계측(--check).</summary>
    public bool Check { get; init; }
    /// <summary>
    /// --boss-hp(v0.9.4, 6.20 ⑩ — 계약): 정의 rank 가 miniboss·boss 인 적이 생길 때 체력 = 최대 × 값((0, 1]). 첫 틱의 페이즈 처리가 문턱대로 넘긴다(탄막 확인용).
    /// </summary>
    public float? BossHp { get; init; }
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
    /// <summary>
    /// 문 단계에서 <see cref="PlayerCommand.ExitIndex"/> 번째 출구로 나간다(v0.7.1 — 자동 조작 <c>DOOR_SKIP</c> 만, 3.6·9절). 깃발이 없으면 "안 나감" 이라
    /// 기본값(<c>default</c>·<see cref="PlayerCommand.None"/>)이 조용히 0번 출구로 나가는 일이 없다.
    /// </summary>
    Exit = 1 << 9,
    /// <summary>
    /// 물약 하나를 마신다(v0.9.1 V9, 6.19 "물약 칸" — 사람 <c>Q</c>, 자동 조작은 체력 &lt; <c>bot_threshold</c>). 판 명령이라 재생·자동 조작·사람이 같은 길이다.
    /// 못 마시면(창·회복 중·물약 0·체력 가득) <c>CueKind.PotionDenied</c>. 한 번만 일어나야 하는 버튼이라 프레임의 첫 틱에만 넣는다(3.2).
    /// 상인 창(6.19)은 새 버튼 없이 <see cref="Choice1"/>~<see cref="Choice3"/>·<see cref="Reroll"/>·<see cref="Skip"/> 을 쓴다.
    /// </summary>
    Drink = 1 << 10,
    /// <summary>
    /// 순간이동(v0.9.4 V21, 6.20 ⑫ — 사람 <c>Space</c>, 자동 조작 <c>ShouldBlink</c>). 방향 = 같은 명령의 <see cref="PlayerCommand.Move"/>(길이 &gt; 1e-3 이면 정규화) 아니면 <c>Hero.Facing</c>.
    /// 못 하면(칸 0·시전 중·그 방향 24px 미만) <c>CueKind.BlinkDenied</c>. 한 번짜리 버튼이라 프레임의 첫 틱에만 넣는다(3.2). 사람 입력·자동 조작·재생이 같은 길이다.
    /// </summary>
    Blink = 1 << 11,
}

/// <summary>
/// 틱 하나의 명령 (설계서 2.1 명령 패턴): 이동 벡터(길이 ≤ 1) + 버튼 비트. 시뮬레이션은 입력 장치를 모른다.
/// 한 번만 일어나야 하는 버튼은 프레임의 첫 틱에만 넣는다(3.2).
/// <paramref name="ExitIndex"/>(v0.7.1)는 <see cref="CommandButtons.Exit"/> 가 있을 때만 뜻이 있다 — 문 단계에서 <c>Room.Exits[ExitIndex]</c> 로 나감.
/// </summary>
public readonly record struct PlayerCommand(Vector2 Move, CommandButtons Buttons = CommandButtons.None, byte ExitIndex = 0)
{
    public static PlayerCommand None => default;
}

/// <summary>명령 출처: 키보드·마우스(WP6) / 자동 조작 봇. 봇은 틱마다 월드를 보고 만든다.</summary>
public interface IInputSource
{
    PlayerCommand Next(World world);
}
