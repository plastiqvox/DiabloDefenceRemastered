namespace DiabloDefenceRemastered.Core.Sim.Progression;

/// <summary>대기 중인 보상 창 하나(마법서 = Tome, 매혹의 성소 = Shrine — 6.18).</summary>
public readonly record struct PendingReward(RewardDef Reward, OfferSource Source);

/// <summary>
/// WP3 의 방 수명 상태(<see cref="World.Combat"/>). 판 수명 값(레벨·경험치·재추첨·<c>Mods</c>·<c>Shrines</c>·시간 효과)은 방을 나갈 때
/// <see cref="HeroCarrier.Capture"/> 가 짐에 담고 새 방에서 <see cref="HeroCarrier.Restore"/> 가 되살린다 — 모듈 인스턴스·정적 필드에는 두지 않는다(11.2).
/// </summary>
public sealed class CombatState
{
    /// <summary>시간 효과 ID: 마력 폭주(짐 <c>Effects</c> 의 이름 — WP3 가 정함).</summary>
    public const string SurgeEffectId = "mana_surge";

    public int Level = 1;
    public int Xp;
    public int PendingLevelUps;
    public int RerollsLeft;
    /// <summary>대기 중인 상자들의 강화 횟수(먼저 연 순서).</summary>
    public readonly List<int> Chests = [];
    /// <summary>대기 중인 보상 창(주운 순서).</summary>
    public readonly List<PendingReward> Rewards = [];
    /// <summary>판 끝까지 가는 능력치 더하기(<c>단련</c>·성소 대가 — 짐 <c>Mods</c>). <see cref="StatKey"/> 번호로.</summary>
    public readonly float[] Mods = new float[StatKeys.Count];
    /// <summary>이 판에 받은 성소 ID(짐 <c>Shrines</c>).</summary>
    public readonly List<string> Shrines = [];
    /// <summary>마력 폭주 남은 틱·쿨 감소 비율(시간 효과 — <c>World.TimedEffectsRun</c> 일 때만 줄어든다).</summary>
    public int SurgeTicks;
    public float SurgePct;
    /// <summary>선택 창이 닫혔다 — 다음 스테이지 틱 <c>HeroControl</c> 이 밀어내기(<c>level_up_push</c>)를 한다.</summary>
    public bool PushPending;
    /// <summary>지금 보여 주는 레벨업 창·보상 창(처음 부를 때 만들고 고르기·재추첨·건너뛰기가 지운다).</summary>
    public LevelUpOffer? LevelOffer;
    public LevelUpOffer? RewardOffer;
    /// <summary>이번 틱 <c>DamageQueue</c> 에서 WP3(투사체·장판·예고)가 넣은 적 쪽 요청이 시작하는 자리(공정성 <c>hidden_hits</c> 가르기).</summary>
    public int OwnEnemyRequestsFrom = int.MaxValue;
    /// <summary>능력치를 다시 계산해야 함(칸·레벨·덧값이 바뀜).</summary>
    public bool StatsDirty = true;

    /// <summary>공간 질의 결과 버퍼(할당 0).</summary>
    internal readonly int[] Query = new int[1024];
}
