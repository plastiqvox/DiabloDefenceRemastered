namespace DiabloDefenceRemastered.Core.Defs;

/// <summary>
/// 정의 안의 부품 원소 하나 (설계서 6.2). JSON 원소 <c>{ "type": …, "id"?: …, "trigger"?: …, 나머지 매개변수 }</c>.
/// 로더가 <see cref="PartRegistry"/> 에서 (Kind, Type) 으로 매개변수 형을 골라 <c>type·id·trigger</c> 를 뺀 나머지를 역직렬화하고(<see cref="Params"/>),
/// 등록된 생성 함수로 부품 객체를 만든다(<see cref="Instance"/>). 로더는 Instance 를 object 로만 알고,
/// 쓰는 묶음이 자기 인터페이스로 바꿔 쓴다.
/// </summary>
/// <param name="Kind">레지스트리 종류. <c>behaviors</c> 원소에 trigger 가 있으면 <see cref="PartKind.EnemyAction"/>(훅 어댑터, 6.6).</param>
/// <param name="Type">JSON "type".</param>
/// <param name="Id">선택 "id" — <c>*_patch</c>·페이즈 <c>patch</c> 가 이것으로 찾는다.</param>
/// <param name="Trigger">선택 "trigger"(<c>on_death</c> / <c>on_spawn</c> / <c>cooldown</c>).</param>
public sealed record PartDef(PartKind Kind, string Type, string? Id, string? Trigger, object Params, object Instance)
{
    /// <summary>
    /// 이 부품 매개변수 안의 모든 <c>DefRef&lt;EnemyDef&gt;</c>(v0.6.1 — summon·split_on_death·호위·보스 이벤트 등, 6.14).
    /// 로더(WP7)가 매개변수의 DefRef 를 채우는 연결 단계에서 같이 모은다 — 매개변수 형을 몰라도 되고 부품을 만드는 묶음은 코드를 더하지 않는다.
    /// "스테이지에서 나올 수 있는 적"(<c>StageReach</c>)이 이것만 읽는다.
    /// </summary>
    public IReadOnlyList<DefRef<EnemyDef>> ReferencedEnemies { get; init; } = [];
}
