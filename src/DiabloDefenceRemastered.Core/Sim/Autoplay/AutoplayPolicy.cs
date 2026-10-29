namespace DiabloDefenceRemastered.Core.Sim.Autoplay;

/// <summary>
/// 자동 조작 정책 (설계서 3.4·8.1, 소유 WP4). 시험과 게임(--autoplay)이 같은 정책을 써서 같은 씨앗이면 같은 판이 된다.
/// 메뉴 넘김은 WP6 AutoplayDriver. WP1 이 시그니처만 만든 빈 연결 파일 — WP6 이 먼저 부르고 컴파일한다. 본문은 WP4 가 채운다.
/// </summary>
public static class AutoplayPolicy
{
    /// <summary>이번 스테이지 틱의 이동(길이 ≤ 1). <see cref="PlayerCommand.Move"/> 로 넘긴다.</summary>
    public static Vector2 Move(World world)
        => throw new NotImplementedException("WP4 AutoplayPolicy.Move");

    /// <summary>고를 카드 번호(0부터, <see cref="IProgression.Choose"/> 에 넘김).</summary>
    public static int PickCard(LevelUpOffer offer)
        => throw new NotImplementedException("WP4 AutoplayPolicy.PickCard");
}
