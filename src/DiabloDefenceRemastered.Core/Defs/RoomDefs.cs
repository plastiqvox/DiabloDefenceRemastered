namespace DiabloDefenceRemastered.Core.Defs;

/// <summary>방 종류(v0.7, 사용자 결정 R — 설계서 3.6·6.12). JSON <c>normal</c> / <c>elite</c> / <c>boss</c>.</summary>
public enum RoomKind : byte { Normal, Elite, Boss }

/// <summary>
/// 방 순번 표 <c>stages[].rooms[]</c> 의 원소 하나(v0.7, 설계서 6.12). 데이터 모양이고, 방 하나의 실행 상태는 <c>World.Room</c>(<c>RoomState</c>, 3.6)이다.
/// <list type="bullet">
/// <item>일반 방: 생존 시간 = <see cref="To"/> − <see cref="From"/>(따로 적지 않음 — 기획 2.5).</item>
/// <item>정예 방: 처치까지. t 는 <see cref="To"/> − 1틱에서 멈춘다(v0.7.1 — 구간 [from, to) 를 끝까지 씀).</item>
/// <item>보스 방: <see cref="To"/> 없음(t 안 멈춤).</item>
/// </list>
/// </summary>
public sealed record RoomDef
{
    public required RoomKind Kind { get; init; }
    /// <summary>t 범위 시작.</summary>
    public required ClockTime From { get; init; }
    /// <summary>t 범위 끝(끝 미포함). 보스 방은 null.</summary>
    public ClockTime? To { get; init; }
    /// <summary>정예 방의 중간 보스(처치가 생존 끝, 문 위 표시). 그 방 범위의 <c>miniboss</c> 이벤트 적과 같아야 한다(13.5-23 ⑤).</summary>
    public DefRef<EnemyDef>? Miniboss { get; init; }
    /// <summary>보스 방의 보스(13.5-23 대조·문 위 이름).</summary>
    public DefRef<EnemyDef>? Boss { get; init; }
    /// <summary>
    /// 보이는 방 번호(v0.7.1, JSON <c>number</c> 선택). 0 = 적지 않음 — 로더(WP7)가 배열 자리 + 1 로 채운다.
    /// 화면·로그·<c>--room</c>·<c>RunSeeds</c>·<c>RunDepth</c> 가 모두 이것을 쓴다. 1부터 커지기만 하고, <c>slice</c> 가 아니면 배열 자리 + 1 과 같다(13.5-23 ①).
    /// </summary>
    public int Number { get; init; }
    /// <summary>
    /// 층(v0.8, JSON <c>floor</c> 선택, 6.12 — 기획 v0.8.1 2.5 "층"). 0 = 안 적음. 방 문구(<c>대성당 2층 — 3</c>, 감독 <c>Banner</c>)와
    /// 층이 바뀌는 출구의 <c>RoomExit.NextFloor</c>(문 위 <c>▼ 2층</c>·넘김 어둠 <c>transition_floor</c>)에만 쓴다 — 난이도·씨앗과 무관.
    /// </summary>
    public int Floor { get; init; }
}
