namespace DiabloDefenceRemastered.Core.Defs;

/// <summary>
/// <c>light_change</c> 스테이지 이벤트의 매개변수(v0.7.1, 계약 WP1e-형 — 설계서 3.6 결정 5·6.12). 부품 등록·방 안 적용은 WP4 가 이 형으로 하고,
/// 방을 만들 때 지난 것을 다시 적용하는 <c>LightingState.Apply</c>(감독 없이 <c>World</c> 생성자가 부름)도 같은 형을 읽는다.
/// JSON <c>{ "at": "11:30", "type": "light_change", "set": { "darkness": 0.7, "hero_light.radius": 400 } }</c> — 로더(WP7)가 <c>set</c> 객체를 적힌 순서의 <see cref="Set"/> 로 바꾼다.
/// </summary>
/// <param name="Set">바꿀 경로와 값(적힌 순서대로 적용).</param>
public sealed record LightChangeParams(IReadOnlyList<LightSet> Set);

/// <summary>
/// 바꿀 경로 하나(6.12). 받는 경로: <c>ambient</c>(값 셋 = 색), <c>darkness</c>, <c>unit_min_light</c>, <c>hero_light.color</c>(셋)·<c>hero_light.radius</c>,
/// <c>lights.&lt;id&gt;.color</c>(셋)·<c>.radius</c>·<c>.flicker</c>·<c>.pulse.period</c>·<c>.pulse.amount</c>. 숫자 하나는 길이 1 배열.
/// 없는 <c>lights.&lt;id&gt;</c> 는 로딩 검사가 막는다(13.5-1).
/// </summary>
public readonly record struct LightSet(string Path, float[] Value);
