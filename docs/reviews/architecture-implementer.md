# 설계서 리뷰 (구현 담당)

- 대상: `docs/architecture.md` 초안 v0.1 (1250줄), 기준 기획 `docs/gdd.md` v0.2, `docs/brief.md`
- 초점: 이대로 코드를 쓸 수 있는가 — 인터페이스 계약의 구체성, 작업 묶음(WP) 파일 소유 겹침, 병렬 구현 때 서로 막히는 곳,
  MonoGame 3.8.6 / FontStashSharp 1.6.2 / .NET 9 에서 실제로 되는 API 인지, 과설계.
- 확인한 것: 이 PC NuGet 캐시의 `monogame.framework.desktopgl/3.8.6`(`MonoGame.Framework.xml`), `fontstashsharp.monogame/1.6.2`(xml·nuspec) API 이름과 의존성.
  빌드·실행은 하지 않았다(코드가 아직 없다).
- 등급: **높음** = 그대로 구현하면 틀린 결과·데이터 유실·막힘, **중간** = 좁은 경로로 틀리거나 재발 위험 구조, **낮음** = 표현·배치·작은 빈칸

## 총평

알고리즘(고정 틱·플로우 필드·A*·clearance·공간 해시·피해 큐)과 렌더 방식은 그대로 구현할 수 있고, 쓰려는 MonoGame·FontStashSharp API 는
3.8.6 / 1.6.2 에 모두 있다(아래 API 확인 표). 몬스터 늘리기(사용자 요구 8)도 `extends` + 부품 + 등급·접두사 구조로 코드 분기 없이 된다.

막히는 곳은 알고리즘이 아니라 **계약과 소유**다.
`World`·`Enemy` 같은 공용 형의 필드가 비어 있는데 WP1 혼자 소유해서, WP2~WP4 가 매일 WP1 을 거쳐야 한다(H1).
적 이동(플로우 따라가기·끼임·A* 경로 따라가기)은 WP2 절에 적혀 있지만 실행 자리는 WP4 의 `EnemyThink` 라 주인이 없다(H2).
수치 쪽에서는 `damage_pct` 가 두 곳에서 곱해지는 길이 열려 있고(H3), 캡처 PNG 바이트 비교를 약속했지만 표시 이벤트가 프레임 단위로 흘러 그대로는 안 맞는다(H4).
넷 다 WP1 이 계약을 고정하기 **전에** 정하면 싸고, 병렬 작업이 시작된 뒤에는 비싸다.

| 등급 | 개수 |
|---|---|
| 높음 | 4 |
| 중간 | 12 |
| 낮음 | 14 |

---

## 높음

### H1. `World`·개체 데이터 클래스의 필드가 정해지지 않았고 WP1 단독 소유라 병렬 작업이 WP1 에 줄 선다

- **위치**: 11 WP1 계약("`World`(필드 목록 3.5·5절)", "`Enemy`/`Hero`/`Projectile`/… 데이터 클래스(필드만)"), 11.1 "다른 묶음은 계약 파일을 직접 고치지 않는다"
- **문제**: 3.5·5절에는 `World` 필드 목록이 없다. 각 묶음이 실제로 필요한 상태를 모아 보면 이렇다.
  - WP2: `DesiredVel`, `KnockbackVel`/남은 시간, 끼임 타이머·비켜 가기 방향, 시야 캐시(6틱), A* 경로 버퍼·경유점 번호·재계획 시각, 주인공 "닿은 적 수".
  - WP3: `NextHitBySlot[4]`, 번쩍임 타이머, 둔화(장판 −25%), 무적 타이머, 무기 칸·`RunStats`·`Progression` 상태.
  - WP4: 생애 상태, `PartSlot[]`, `ActionState`, `BuffSlot[4]`, 등급·접두사, 리더/대형 자리(H2·M5), 행진 방향·남은 시간, 공포 남은 시간, 애니메이션 시계, `SpawnTick`, `DamageMul`.
  - 감독: 누적기·다음 무리 크기·정예 판정 시각·이벤트 진행 위치. 조명 바꾸기(`light_change`)의 현재 조명.
  이것이 모두 WP1 파일이라 WP2~WP4 는 필드 하나 늘릴 때마다 WP1 에 요청해야 한다. 단계 1 동안 하루에도 여러 번 생기는 일이라 WP1 이 병목이 되고,
  기다리기 싫은 묶음이 자기 폴더에 "옆 사전(`Dictionary<EntityId, …>`)" 을 만들면 결정성(3.4)·할당 0(4.9) 규칙이 깨진다.
- **제안**:
  1. `World`·`Enemy`·`Hero`·`Projectile`·`Hazard` 를 `partial class` 로 두고, 묶음마다 자기 폴더에 조각 파일을 소유한다
     (예: `Sim/Movement/Enemy.Movement.cs`(WP2), `Sim/Enemies/Enemy.Behavior.cs`(WP4), `Sim/Combat/Enemy.Combat.cs`(WP3), `Sim/Director/World.Director.cs`(WP4)).
     WP1 은 공통 필드(`Id`, `Def`, `Pos`, `PrevPos`, `Vel`, `Radius`, `Hp`, `Dead`, `SpawnTick`)만 가진다. 같은 어셈블리라 `partial` 이 된다.
  2. 위 목록 수준의 **필드 표를 설계서에 한 번 적는다**(어느 묶음이 쓰고 어느 묶음이 읽는지). 읽기만 하는 쪽이 있는 필드(둔화·넉백·`DesiredVel`)는 쓰는 묶음이 소유한다.
  3. `Enemy` 가 클래스(풀 재사용)인지 구조체인지 적는다. `EnemyCtx(World, Enemy self)` 를 보면 클래스다 — 그러면 풀에서 꺼낼 때 초기화(`Reset`) 규칙도 계약에 넣는다.

### H2. 적 이동(플로우 따라가기·직진·끼임·A* 경로 따라가기)에 주인이 없다

- **위치**: 4.3 "방향 읽기 / 가까우면 직진 / 끼임 감지", 4.4 "따라가기·재계획", 6.6 매 틱 순서 5 "위치 잡기 부품 → 이동 방식(flow/astar/direct)으로 희망 속도", 2.1 전략 표의 `INavigator`, 11 WP2 계약(`INavWorld`: `FlowDir`, `RequestPath`, `HasClearLine`, `Clearance`)
- **문제**: 4.3·4.4 의 규칙은 WP2 절에 있지만, 실행되는 곳은 3.5 의 5단계 `EnemyThink`(WP4)다. WP2 계약의 `INavWorld` 는 질의만 내고,
  "시야 검사를 6틱마다 하고 결과를 저장", "1초 4px 끼임 → 0.5초 옆으로", "경유점 8px 안이면 다음", "0.5s/32px/끼임이면 재계획",
  "결과가 오기 전에는 직전 경로" 를 하는 코드는 어느 묶음에도 적혀 있지 않다. `RequestPath` 의 결과를 **어떻게 받는지**(반환값? 적의 경로 버퍼? 핸들?)도 없다.
  2.1 에 `INavigator` 가 있지만 11절 계약 목록에는 없다. 결과: 두 묶음이 서로 상대가 한다고 생각하고 통합 때 빈칸이 나오거나, 둘 다 만들어 다르게 동작한다.
  사용자 요구 1(제대로 된 길찾기)의 핵심 부분이다.
- **제안**: `INavigator` 를 WP2 계약으로 올리고 WP2 가 flow/astar/direct 세 구현과 4.3·4.4 의 따라가기 규칙 전부를 소유한다.
  ```csharp
  public interface INavigator   // WP2 구현. EnemyThink(WP4)가 독점 행동·위치 잡기 부품이 없을 때 부른다
  {
      Vector2 Steer(World world, Enemy self, Vector2 goal, ref NavAgentState nav);   // 희망 방향(정규화), 0 이면 멈춤
  }
  public struct NavAgentState { /* 시야 캐시, 끼임 타이머, 경로 핸들, 경유점 번호, 재계획 시각 … — WP2 소유 */ }
  ```
  위치 잡기 부품(`kite` 의 다가가기·물러나기, 행진 직진, 호위 따라가기)도 "목표 점" 만 정하고 실제 길은 `Steer` 로 가게 하면 장애물 미끄러짐이 한 곳에 모인다.
  A* 경로는 `NavAgentState` 안 고정 길이 경유점 배열(예: 16개, `InlineArray`)에 받아 할당 없이 둔다.

### H3. `damage_pct` 를 곱하는 자리가 두 곳이다 — 그대로 짜면 이중 적용

- **위치**: 5.2 `DamageMath.HeroToEnemy(weaponDamage, damagePct, crit, armor)`, 5.3 "`WeaponStats.Resolve(def, level, hero.Stats)` 가 레벨 표 + base + 주인공 능력치를 한 번에 합친다", 기획 13.2 "모든 패턴에서 `damage`·`tick_damage` 에는 `damage_pct` … 가 붙는다"
- **문제**: 13.2 를 따라 `Resolve` 가 `damage × (1 + damage_pct)` 를 넣고, `DamageResolve` 가 기획 4.2 식 그대로 `HeroToEnemy(d, damagePct, …)` 를 부르면
  마력 증폭 Lv5(+50%)에서 피해가 ×2.25 가 된다. 조용히 틀리는 밸런스 오류이고 테스트(`Combat`, `Weapons`)도 각자 맞게 통과한다.
  더해서 정하지 않은 것: 투사체가 날아가는 동안 패시브를 얻으면(레벨업 창은 투사체를 멈춘 채 열림) 그 투사체는 옛 값인가 새 값인가, 반올림은 어디서 한 번인가(4.2 "반올림은 마지막에 한 번").
- **제안**: 한 줄로 정한다. 권장: `WeaponStats` 의 `Damage`/`TickDamage` 는 **레벨 표 원값**(배율 없음)을 두고, 투사체·장판은 생성 순간 `damage_pct`·치명 확률을 복사해 가진다(발사 시점 고정).
  `DamageRequest` 에 `DamagePct` 를 싣고 `HeroToEnemy` 한 곳에서만 곱하고 반올림한다. `WeaponStats.Resolve` 의 키 표(13.2)에서는 `damage_pct` 줄을 빼고 "적용은 `DamageMath`" 로 적는다.
  `Weapons` 테스트에 "Resolve 결과의 damage 는 damage_pct 와 무관" 을 넣어 다시 섞이지 않게 한다.

### H4. 표시 이벤트·표시 난수가 프레임 단위라 "같은 씨앗 → 같은 PNG" 가 성립하지 않는다

- **위치**: 3.4 마지막 줄 "표시 이펙트·시체·피해 숫자도 … 그 프레임에 돈 스테이지 틱 수 × Dt 만큼 진행 → 같은 씨앗·같은 캡처 시각이면 같은 그림", 7.2 흔들림 "프레임마다 표시 난수로", 10.2 "재현: 같은 명령 두 번 → 두 PNG 바이트 비교", 5.6 `FxSystem.Consume(PresentationBuffer, float simDt)`
- **문제**: 한 프레임에 몇 틱이 도는지는 벽시계(누적기)가 정한다 — 같은 명령이라도 실행마다 다르다.
  1. 4틱 묶음 프레임의 첫 틱에 생긴 폭발과 마지막 틱에 생긴 폭발을 똑같이 "4틱 × Dt" 만큼 진행시키면, 묶음이 다르게 잘린 두 실행의 캡처에서 이펙트 프레임이 다르다.
  2. 흔들림 난수를 "프레임마다" 뽑으면 그때까지 지나간 프레임 수가 실행마다 달라 같은 시각의 오프셋이 다르다.
  3. 3.5 의 1단계 "틱 이벤트 큐 비우기" 와 같은 방식으로 `PresentationBuffer` 를 틱마다 비우면 앞 틱의 표시 이벤트가 사라진다(프레임에 여러 틱이 돌 때). 15단계 "표시 이벤트 확정" 의 뜻이 적혀 있지 않다.
  그대로 구현하면 10.2 "재현" 확인과 3.4 의 약속이 실패하고, 원인을 시뮬레이션 쪽 결정성 버그로 오해하기 쉽다.
- **제안**: WP1 계약 단계에서 고친다.
  - `PresentationBuffer` 의 모든 항목에 **생긴 스테이지 틱**(`Tick`)을 싣는다. 버퍼는 틱마다 비우지 않고 **프레임 끝에 게임이 소비한 뒤** 비운다(소비 = `FxSystem.Consume`).
  - `FxSystem`·`CorpseLayer`·`DamageNumbers` 는 "나이 = 현재 스테이지 틱 − 생긴 틱" 으로 그린다(진행량을 누적하지 않는다). 그러면 `simDt` 인자가 필요 없다.
  - 흔들림·장식 난수는 프레임 수가 아니라 **스테이지 틱(또는 이벤트 번호)을 씨앗으로 한 해시**로 뽑는다(`Hash(seed, tick)`).
  - 일시정지 중 표시(오버레이 애니메이션)는 앱 틱 기준으로 같은 방식.

---

## 중간

### M1. `DeathResolve` 단계를 두 묶음이 같이 쓰고, 죽음 이벤트 이름이 셋이다

- **위치**: 3.5 13단계 "WP4(훅) + WP3(드롭)", 11 WP1 틱 이벤트 `EnemyDied`, WP3 계약 "`DamageResolve` 가 내는 `EnemyKilled`", 5.6·WP4 계약 표시 이벤트 `EnemyDied(그림 묶음, …)`
- **문제**: 한 단계에 두 묶음의 코드가 들어가는데 시스템 파일·순서가 정해지지 않았다(훅 먼저? 드롭 먼저? — 분열체 생성과 드롭이 같은 자리에서 순서에 따라 결과 목록 순서가 바뀐다).
  `SimPhase` 하나에 시스템 여러 개를 허용하는지도 적혀 있지 않다. 또 `EnemyDied` 가 틱 이벤트(WP1)와 표시 이벤트(WP4) 두 형의 이름이고, WP3 는 `EnemyKilled` 를 낸다고 한다.
  상한 정리로 조용히 지운 적(경험치 없음, 시체 없음)과 보스 처치 연출로 무너진 적(경험치 없음, 시체 있음)도 구별해야 한다.
- **제안**: 단계를 둘로 나눈다 — `DeathHooks`(WP4: 행동 끊기·분열·장판·공포·보스 처치) → `DeathRewards`(WP3: 보석·드롭·처치 수·`RunStats`). 압축은 15 그대로.
  이름: 틱 이벤트 `EnemyKilled(Id, Source, Pos, Cause)`(`Cause` = 피해 / 정리 / 보스 처치 연출) 하나, 표시 이벤트는 `CorpseSpawned(…)` 로 따로.
  `SimulationBuilder` 는 "한 단계 = 시스템 하나" 로 못 박거나, 여러 개면 등록 순서가 아니라 명시한 `order` 값으로 정렬한다.

### M2. `PartRegistry` 이름 공간이 하나라 `type` 이 겹치고, 부품 역할 인터페이스 대부분의 시그니처가 없다

- **위치**: 2.1 레지스트리, 6.2 `PartRegistry.Register<TParams>(string type, …)`, 6.6 역할 표, 6.13 "같은 레지스트리", 13.2·13.4
- **문제**:
  - 같은 `type` 문자열이 다른 종류로 쓰인다: `escort`(적 부품 / 이벤트), `ground_pool`(발사 패턴 / 진화 `on_explode`), `summon`(보스 동작 / E 무기 패턴), `ground_hazard`(일반 부품 / 보스 동작),
    `telegraphed_blast`(보스 동작 / 접두사 죽음 훅). 이름 하나에 매개변수 형 하나인 레지스트리라 두 번째 등록이 충돌하거나 덮어써서 엉뚱한 형으로 역직렬화된다.
  - 시그니처가 있는 것은 `IAbilityPart` 뿐이다. `IPositioningPart`, `IAuraPart`, `ISpawnHookPart`, `IDeathHookPart`, `IHitModifierPart`, 보스 **동작** 부품(`PatternAbility` 가 감싸는 안쪽, 예: `melee_slam` 의 실행 함수), `IStageEvent` 의 메서드가 없다.
    WP1 이 이것을 정해야 WP4 가 시작할 수 있다.
  - `ground_hazard` 는 `behaviors` 안에서 죽음 훅(`trigger: on_death`)이면서 패턴 안에서 동작(`trigger: cooldown`)이다 — 한 부품이 두 역할을 어떻게 등록하는지 정해야 한다.
- **제안**: 레지스트리 키를 `(PartKind, type)` 로 한다(`EnemyPart`, `BossAction`, `FirePattern`, `StageEvent`, `OnExplode`). 위 여섯 인터페이스의 시그니처를 6.6 에 `IAbilityPart` 처럼 적는다.
  보스 동작은 `IEnemyAction { void Execute(in EnemyCtx ctx, ref ActionState a); }` 정도로 두고, `ground_hazard`·`telegraphed_blast` 처럼 훅으로도 쓰는 것은
  "훅 어댑터 하나가 `trigger` 를 보고 동작을 부른다" 로 정하면 접두사(6.7)와 보스가 같은 코드를 쓴다.

### M3. `DefRef<T>` 를 `readonly record struct` 로는 5단계(연결)에서 채울 수 없다

- **위치**: 6.2 `public readonly record struct DefRef<T>(string Id) … { public T Def => …; }`, "5단계에서 `Def` 를 채운다. 그 뒤 정의는 바뀌지 않는다(모두 `init`)"
- **문제**: 정의 record 는 `init` 이라 4단계 뒤 고칠 수 없고, 그 안에 값으로 박힌 `readonly struct` 도 고칠 수 없다. `Def` 를 채우려면
  (a) 그래프 전체를 `with` 로 다시 만들거나 (b) 정적 사전에서 찾는 수밖에 없는데, (b) 는 2.1 "정적 싱글턴 없음" 과 테스트의 월드 여러 개와 부딪힌다.
- **제안**: `DefRef<T>` 를 `sealed class` 로 하고 `Def` 는 `internal set`(연결 단계만 한 번 씀, 두 번째 쓰기는 예외) — 또는 연결 단계가 ID 를 정수 인덱스로 바꾸고 `GameDefs` 배열에서 찾는다.
  어느 쪽이든 STJ 에는 `JsonConverterFactory`(문자열 ↔ `DefRef<T>`)가 하나 필요하다는 것도 적는다.

### M4. 정의 밖의 "인스턴스별 이동 지시"(호위 따라가기·행진·공포·입장)를 둘 자리가 없다

- **위치**: 6.6 위치 잡기 부품 예 "`escort`(호위로 따라가기), 이벤트 `march`", 6.5 `escort` 부품 매개변수, 기획 8.3 호위·행진
- **문제**: 호위로 따라오는 것은 평범한 `skeleton` 정의다. `skeleton` 정의에 `escort` 따라가기 부품이 없으므로 "대장 120 안 대형 유지, 250 안이면 추적" 은 정의의 부품으로 표현되지 않는다.
  행진(입구에서 방향으로 15s 직진)도 이벤트가 만든 아무 적에게 붙는다. 둘 다 **그 인스턴스에만** 붙는 일시적 이동 지시인데, 6.6 매 틱 순서에는 그것을 볼 단계가 없다.
- **제안**: `Enemy` 에 `MoveOrder { Kind(None/Enter/March/Follow/Flee), Leader, Offset, Dir, UntilTick }` 를 두고 6.6 순서 1(생애 상태) 다음에
  "이동 지시가 있으면 그것이 위치 잡기를 대신한다" 를 넣는다. `escort` 부품은 대장 쪽 생성 훅으로 호위를 만들며 각 호위에 `Follow` 지시를 건다.
  `Feared` 도 같은 틀(`Flee`)로 묶으면 생애 상태 열거형이 줄어든다.

### M5. 접두사 부품·페이즈 패턴이 붙으면 `PartSlot[]` 크기가 인스턴스마다 달라진다

- **위치**: 6.6 "적마다 다른 값은 `PartSlot[]`(부품 순서대로) … 적 생성 때 할당 없음", 6.7 "접두사 부품은 정의 부품 뒤에 붙는다", 6.5 페이즈 `patch`/`add`/`remove`, 6.6 `PhaseController`
- **문제**: 같은 `skeleton` 정의라도 접두사 0~2개에 따라 부품 수가 다르고, 페이즈가 패턴을 더하고 빼면 순서 인덱스가 바뀌어 쌓인 쿨다운(`PartSlot`)이 엉뚱한 패턴에 붙는다.
  풀에서 꺼낸 `Enemy` 의 배열을 정의마다 다시 만들면 "할당 0" 이 깨진다. 페이즈 `patch` 를 실행 중에 깊은 병합하면 부품 객체를 새로 만든다.
- **제안**: (1) 부품 상한을 정해(예: 정의 8 + 접두사 4) `PartSlot` 을 `InlineArray(12)` 로 둔다. (2) 페이즈는 **로딩 때** 페이즈별 패턴 목록을 미리 풀어 두고,
  보스의 슬롯은 "모든 페이즈 패턴의 합집합 순서" 로 고정 인덱스를 준다 → 전환은 "지금 쓰는 목록 번호" 하나만 바꾼다.

### M6. WP3 의 "해시 없이 시험" 이 계약상 불가능하다 — 공간 질의가 구체 클래스다

- **위치**: 11 WP3 선행 "(WP2 해시가 없을 때는 `TestWorldBuilder` 의 단순 전수 해시로 시험)", WP2 계약 `SpatialHash`(`Rebuild`, `Query(center, radius, Span<int>)`), 5.3 `ITargeting.TryAim(…, Span<EntityId> taken, …)`
- **문제**: `SpatialHash` 는 WP2 소유 구체 클래스라 WP1 의 `TestWorldBuilder` 가 갈아 끼울 수 없다. 또 `Query` 가 돌려주는 `int` 가 저장소 **밀집 인덱스**인지 `EntityId` 인지,
  결과 칸이 모자라면(무리 한가운데 300마리) 잘리는지 개수만 돌려주는지가 없다. 인덱스라면 15단계 압축 전까지만 유효하다는 규칙도 필요하다.
- **제안**: WP1 계약에 `ISpatialQuery { int Query(Vector2 c, float r, Span<int> denseIdx); int CellCount(int cx, int cy); }` 를 두고 WP2 가 구현,
  `TestWorldBuilder` 는 전수 구현을 넣는다. 결과는 밀집 인덱스, 틱 안에서만 유효, 넘치면 앞에서부터 채우고 실제 개수를 돌려준다고 적는다.

### M7. WP1 이 너무 커서 임계 경로가 되고, 남의 부품 매개변수 이름까지 정한다

- **위치**: 11 WP1 맡는 것(솔루션·계약 전부·로더·검증·FrameLoop·DevRunner·캡처·저장·로그·창 제어 + `Data/` 첫 판 전부), 완료 기준 `AllowUnknownParts`, 11.1 `rules.json` 키 단위 분담, 6.1 "`rules` 는 전체에서 한 파일만"
- **문제**:
  - WP2~WP6 는 WP1 이 끝나야 시작한다. 그런데 WP1 완료 기준에 캡처·`--validate-data`·DDR0001·저장까지 들어 있어 계약만 기다리는 묶음이 오래 논다.
  - WP1 이 적·무기 JSON 첫 판을 쓰면 `chase_melee`·`homing` 의 매개변수 이름을 WP1 이 정하게 된다. `AllowUnknownParts` 로 검사도 꺼져 있으니,
    WP4·WP3 가 매개변수 record 를 만들 때 이름이 어긋나 `Disallow` 오류가 한꺼번에 터진다.
  - `rules.json` 을 두 묶음이 키 블록으로 나눠 고치는 것은 같은 파일 동시 수정이라 병렬 작업에서 충돌한다.
- **제안**:
  - WP1 을 둘로 쪼갠다. **WP1a 계약**(sln·csproj·`Common/`·`Defs/`·`Sim/Contracts/`·`World` 뼈대·`TestWorldBuilder`·빈 모듈 파일)만 먼저 끝내고 바로 WP2~WP6 를 연다.
    **WP1b 앱·데이터 도구**(로더 본체·검증 규칙·FrameLoop·DevRunner·캡처·저장)는 다른 묶음과 동시에 한다.
  - `Data/` 는 처음부터 각 묶음이 자기 파일을 쓴다(WP1 은 형식 예시 파일 하나만).
  - `rules` 를 여러 파일에서 하위 키로 합치게 허용한다(`rules/hero.json` = WP3, `rules/enemies.json` = WP4, 같은 하위 키가 두 파일에 있으면 오류).
  - 계약 중 한 묶음만 구현·사용하는 형(`IFirePattern`·`ITargeting`·`WeaponStats` → WP3, `IEnemyPart` 계열·`PartSlot`·`ActionState` → WP4)은 그 묶음 소유로 옮기고,
    WP1 은 레지스트리 등록 지점만 가진다. 그래야 그 형을 바꿀 때 WP1 을 거치지 않는다.

### M8. "PNG 해독은 작업 스레드에서 병렬로, 올리기는 주 스레드에서" 는 `Texture2D.FromStream` 으로 안 된다

- **위치**: 7.5 색 키 "`Texture2D.FromStream(device, stream, colorProcessor)` … PNG 해독은 시작 때 작업 스레드에서 병렬로", 2.2 "그림 PNG 해독만 시작 때 병렬로", R7
- **문제**: 3.8.6 의 `FromStream(GraphicsDevice, Stream, Action<byte[]>)` 은 있다(확인). 하지만 해독과 GPU 올리기를 한 호출에서 하므로 둘을 나눌 수 없다.
  작업 스레드에서 부르면 DesktopGL 은 GL 호출을 주 스레드로 넘겨 기다리는데, 주 스레드가 그 작업들을 `Wait` 하고 있으면 교착된다.
- **제안**: 단계 1 은 주 스레드에서 `FromStream` + `colorProcessor` 로 차례로 읽는다(병렬 없음). 시작 시간을 재 보고 느리면
  `StbImageSharp`(FontStashSharp.MonoGame 1.6.2 가 2.30.16 을 이미 끌어온다 — 쓰려면 csproj 에 명시 참조)로 작업 스레드에서 `ImageResult.FromMemory(…, RedGreenBlueAlpha)` →
  색 키 처리 → 주 스레드에서 `new Texture2D` + `SetData`. 설계서 7.5·2.2 문장을 이 순서로 고친다.

### M9. 적이 주인공에게 피해를 주는 길이 두 개다

- **위치**: 5.1 `DamageRequest(Target = EntityId.Hero, BaseAmount = 적의 최종 피해 …)`, 11 WP3 계약 "적이 피해를 줄 때 쓰는 `World.Damage.ToHero(...)`", 6.6 `IHitModifierPart`(`heavy_strike`, E `lifesteal`)
- **문제**: 근접 타격 프레임(WP4)이 `DamageRequest` 를 큐에 넣는지 `World.Damage.ToHero` 를 바로 부르는지 정하지 않았다. 강타 넉백(`heavy_strike`)·흡혈(`lifesteal`, 접두사 `vampiric`)은
  "피해가 실제로 들어간 뒤(무적·방어 후 값)" 를 알아야 하는데, 그 값은 `DamageResolve`(WP3)에서야 나온다. 누가 `IHitModifierPart` 를 언제 부르는지 없다.
- **제안**: 길은 하나 — 모든 피해는 `DamageRequest`. `World.Damage.ToHero` 는 큐에 넣는 도우미로만 둔다. `DamageRequest` 에 `EntityId Attacker` 를 넣고,
  `DamageResolve` 가 적용 뒤 `HitLanded(Attacker, Target, Applied)` 틱 이벤트를 내면 WP4 가 다음 틱 `EnemyThink` 에서 적중 수정 부품을 돈다(흡혈 회복 1틱 늦음 허용).
  넉백은 정의 값이라 요청에 미리 실으면 된다(`heavy_strike` 는 요청을 만들 때 `KnockbackDist` 만 채우는 부품).

### M10. 레벨 표의 "상시"·"—" 를 JSON 으로 적는 방법과 단조성 검사가 없다

- **위치**: 6.8 레벨 표 열 방식·검증 "쿨다운은 늘지 않고 나머지는 줄지 않는다", 기획 5.5 비전 구체 Lv8 `duration` = 상시, `cooldown` = —
- **문제**: `float[]` 열에 "무한"·"없음" 을 넣을 방법이 없다(JSON 에 Infinity 없음). `null` 을 쓰면 `float[]` 역직렬화가 실패하고, `-1` 을 쓰면 단조성 검사(지속은 줄지 않음)가 Lv8 에서 오류를 낸다.
  WP1(검증)과 WP3(데이터·패턴)이 따로 정하면 둘이 어긋난다.
- **제안**: 열 값을 `float?[]` 로 받고 `null` = "상시/해당 없음" 으로 정한다. 단조성 검사는 `null` 을 건너뛰고, 패턴이 "`duration` 이 null 이면 쿨 무시" 를 선언(`IFirePattern` 에 허용 키)한다.

### M11. GUI 실행 파일(WinExe)의 종료 코드를 PowerShell 에서 못 받는다

- **위치**: 9절 "종료 코드: 0/1/2/3/64", "셸에서 출력을 넘겨받으면 콘솔 출력이 그대로 나온다", `--validate-data`, 9절 검증 실행 예(`^` 줄 잇기 — cmd 문법)
- **문제**: Windows 서브시스템 실행 파일은 PowerShell·cmd 대화형에서 **끝나기를 기다리지 않고** 바로 돌아온다. `$LASTEXITCODE` 가 비고 출력도 사라진다.
  검증 담당이 `--validate-data` 결과(0/2)나 캡처 실패(3)를 이 코드로 판정하면 늘 "성공" 처럼 보인다.
- **제안**: 9절에 실행 방법을 못 박는다 — PowerShell 은 `Start-Process -Wait -PassThru -NoNewWindow` 로 `ExitCode` 를 읽고, 로그 파일(`--log`)을 판정 근거로 쓴다.
  데이터 검사는 창 없이도 되므로 `dotnet test` 의 실제 데이터 테스트를 1차 판정으로 삼는다. 예시 명령을 PowerShell 문법으로 하나 더 적는다.

### M12. 여러 묶음이 같이 쓰는 도우미가 계약에 빠져 있다 — 애니메이션 시계, 카메라 식, 화면→월드 좌표

- **위치**: 1.2 "애니메이션 시계는 시뮬레이션 상태", 7.2 "시뮬레이션은 같은 식으로 `World.ViewRect` 를 계산한다", 3절 마우스 이동(커서 쪽으로 걷기), 9절 `--preview enemies`
- **문제**:
  - 클립·프레임·방향 열·`base_speed` 재생 배율·`event_frame` 넘김 판정을 주인공(WP3)·적(WP4)·시체·이펙트·미리보기(WP5)가 각자 만들게 된다. 셋이 다르게 반올림하면 타격 프레임과 그림이 어긋난다.
  - 카메라 식을 WP4(생성 띠)와 WP5(카메라)가 따로 쓰면 "정확히 같아야 한다"(7.2) 가 지켜진다는 보장이 없다.
  - 마우스 이동은 창 좌표 → 레터박스 → 가상 960×540 → 월드 변환이 필요한데 그 변환은 WP5(`VirtualScreen`·`Camera`)에 있고 입력은 WP6 이다.
- **제안**: WP1 `Common/` 에 `AnimClock`(클립·프레임 시간·재생 배율·이벤트 프레임 지남 판정), `CameraMath.ViewRect(heroPos, mapSize, viewSize)` 를 둔다.
  화면→월드 변환은 WP5 가 `RenderContext` 에 `ScreenToWorld(Point)` 로 내고 WP6 가 쓴다고 계약에 적는다.

---

## 낮음

### L1. 플로우 필드 "뒤 버퍼에 계산하고 바꿔 끼운다" 는 필요 없다 (과설계)

- **위치**: 4.3 갱신 주기·분산
- **문제**: 계산이 한 틱 안에서 동기로 끝나므로(0.1~0.2ms) 읽는 쪽이 반쯤 계산된 필드를 볼 일이 없다. 버퍼 두 벌은 메모리·코드만 늘린다.
- **제안**: 한 벌로 제자리 계산. 나중에 여러 틱에 나눠 계산할 때 다시 넣는다.

### L2. 보스 처치 슬로모션 중 보간 값이 틀려 끊겨 보인다

- **위치**: 3.3 `TimeDilation`, 3.2 `alpha = acc / Dt`
- **문제**: 스테이지 틱이 앱 틱 3.3번에 한 번 도는데 `alpha` 는 앱 누적기에서 나오므로, 위치가 3~4프레임 멈췄다 한 번에 뛴다(0.6초 동안).
- **제안**: 슬로모션 중에는 `alpha` 를 `TimeDilation` 누적값(0..1)으로 준다. `PlayScene` 한 곳에서 처리.

### L3. JSON 로딩의 실제 손일이 적혀 있지 않다

- **위치**: 6.1 `Disallow`, 6.2 로딩 순서 4단계, 6.11·6.12 도형 표기
- **문제**: (1) `Disallow` 이면 병합 뒤 남는 메타 키(`extends`, `abstract`, `behaviors_add`, `patterns_add`)와 부품의 `"type"` 이 모르는 속성으로 오류가 난다.
  (2) STJ 는 한 객체에서 첫 오류에서 멈추므로 "모두 모은다" 를 하려면 정의 항목마다 따로 역직렬화해야 한다.
  (3) 필요한 변환기: `Vector2`(`[x,y]`), `Rgb`, 사각형, 도형(`polygon`/`rect`/`circle`/`circle_at`+`r`), `"mm:ss"`, `DefRef<T>`.
- **제안**: 6.2 에 "4단계 전에 메타 키를 지운다, 항목 단위로 `JsonNode.Deserialize` 하고 `JsonException.Path` 를 `DataError` 로 옮긴다, 변환기 목록" 을 적는다.

### L4. 같은 뜻의 매개변수 이름이 자리마다 다르다

- **위치**: 6.5 패턴 `telegraph: { effect, time }` 와 6.7 접두사 `telegraph: "fx_warning_circle"` + `delay`, 패턴 `range: [최소, 최대]` 와 부품·투사체 `range: 340`, 부품 이름 `phase`(장애물 통과)와 `phases`·`PhaseController`
- **문제**: 같은 `telegraphed_blast` 를 두 모양으로 적으면 매개변수 record 가 둘이 되거나 한쪽이 오류다. `phase` 는 검색·대화에서 보스 페이즈와 헷갈린다.
- **제안**: 예고는 어디서나 `{ "effect", "time" }`. 거리 범위는 `range_band: [min,max]`. 장애물 통과 부품은 `phase_through`.

### L5. 큰 시트 때문에 `GraphicsProfile.HiDef` 를 명시한다

- **위치**: 7절 전반, brief 4절 시트 크기
- **문제**: `LOGO` 550×3240, `SkeletoneKingAttack/Die` 1920×2560, `HealingPotion`·`ManaPotion` 96×2560, `ZombieWalk` 1152×2304, `HeroDie` 96×2208 은 Reach 프로필 한도(2048)를 넘는다.
  `GraphicsDevice.GraphicsProfile` 문서(3.8.6 xml)에 기본값이 Reach 로 적혀 있다. 장치 관리자가 실제로 무엇을 고르는지는 실행해 봐야 한다.
- **제안**: `GameApp` 생성자에서 `graphics.GraphicsProfile = GraphicsProfile.HiDef` 를 명시하고, WP1 첫 실행 확인 항목에 "가장 큰 시트 올리기" 를 넣는다.

### L6. `gulim.ttc` 대체 글꼴은 FontStashSharp 에서 확인이 필요하다

- **위치**: 8.2 글꼴 대체 순서 `malgun.ttf` → `malgunbd.ttf` → `gulim.ttc` → `arial.ttf`
- **문제**: `FontSystem.AddFont(byte[])` 는 글꼴 묶음(.ttc)의 몇 번째 글꼴을 쓰는지 고르는 인자가 없다. .ttc 를 넣으면 실패하거나 첫 글꼴만 쓸 수 있다.
- **제안**: 단계 1 에서 .ttc 를 실제로 넣어 보고, 안 되면 목록에서 뺀다. `malgun.ttf` 가 있는 이 PC 에서는 영향 없음.

### L7. SDL 힌트는 환경 변수로 넣으면 P/Invoke 가 필요 없다

- **위치**: 9절 `--background` "창 만들기 전에 SDL 힌트", R8
- **문제**: MonoGame 은 `SDL_SetHint` 를 공개하지 않는다. 직접 P/Invoke 하면 MonoGame 이 실은 SDL2 네이티브 파일 이름·경로에 기대게 된다.
- **제안**: `new GameApp()` 전에 `Environment.SetEnvironmentVariable("SDL_WINDOW_NO_ACTIVATION_WHEN_SHOWN", "1")` — SDL2 는 힌트를 환경 변수에서도 읽는다. 효과는 R8 대로 첫 실행에서 확인.

### L8. xunit.v3 프로젝트 설정을 적어 둔다

- **위치**: 1.3 "동적 건너뛰기를 위해 xunit.v3 를 쓴다(`Assert.Skip`)"
- **문제**: xunit.v3 테스트 프로젝트는 `OutputType Exe` 가 필요하고 `xunit.runner.visualstudio` 도 3.x 여야 `dotnet test` 가 찾는다. 빠뜨리면 "테스트 0개" 로 조용히 통과한다.
- **제안**: 1.3 표 비고에 두 줄 추가. WP1 완료 기준에 "테스트 수 > 0 확인".

### L9. 길 검사 반경 14 와 S 필드 통과 조건 16 이 다르다

- **위치**: 4.3 `S` = clearance ≥ 16, 6.14 13.5-5 "반경 14 / 26 / 34"
- **문제**: 검사는 반경 14 로 "주인공이 가는 곳에 일반 적도 간다" 를 통과시키지만, 실제 무리 필드는 16 을 요구한다. clearance 14~16 인 띠는 검사 통과·실제 못 감.
  80px 규칙 때문에 지금 맵에서는 드물지만, 새 스테이지에서 조용히 갇힌 주머니가 생길 수 있다.
- **제안**: 13.5-5 의 무리 등급을 S·M 필드 문턱(16·26)과 같은 값으로 돌리거나, 필드 문턱을 상수 하나에서 같이 읽는다.

### L10. 로더 7단계(기하 검사)가 WP2 코드를 부른다 — 등록 지점을 둔다

- **위치**: 6.2 로딩 순서 7 "Nav 의 검사기", 11 WP1(로더)·WP2(`Nav/Validation`)
- **문제**: WP1 로더가 WP2 클래스를 직접 부르면 WP2 가 생기기 전에는 WP1 이 빌드되지 않거나, WP1 이 빈 클래스를 만들어 WP2 파일을 건드리게 된다.
- **제안**: `PartRegistry` 처럼 `IDataValidator` 등록 지점을 두고 `NavModule.Register` 에서 넣는다.

### L11. Core 에서 쓸 정수 점·사각형 형과 `Vector2` 이름 충돌

- **위치**: 7.6 `BakedObstacle(… Point BodyOrigin …)`, `RectI`, 1.2 "Core 는 `System.Numerics.Vector2`"
- **문제**: Core 에는 XNA `Point` 가 없다. 게임 프로젝트는 `ImplicitUsings` 로 두 `Vector2` 가 함께 보이는 파일이 많아 모호성 오류가 잦다.
- **제안**: Core `Common/` 에 `Int2`, `RectI` 를 정의(WP1). 게임 프로젝트에 `global using XnaVector2 = Microsoft.Xna.Framework.Vector2;` 같은 별칭 규칙을 정한다.

### L12. 개발 옵션의 "적용" 담당이 없다

- **위치**: 9절 `--start-at`, `--level`, `--weapons`, `--passives`, `--god` / 11 WP1 `Dev/`(파서)
- **문제**: 파서는 WP1 이지만 "지난 이벤트 건너뛰기·그 시각 구간"(WP4 감독), "레벨·무기·패시브 주기"(WP3 성장)를 실제로 하는 코드가 어느 묶음 일인지 없다.
- **제안**: `World` 생성 직후 `IRunSetup.Apply(World, DevOptions)` 를 묶음별로 등록하게 하거나, 담당을 11절에 한 줄씩 적는다.

### L13. `FrameLoop` 의 틱 계산이 게임 프로젝트에 있어 테스트할 수 없다

- **위치**: 1.1 `App/FrameLoop`, 1.3 "테스트는 Core 만 참조", 10.1 `Sim` "묶음 독립성: 프레임당 1틱 / 4틱"
- **문제**: 누적기·틱 상한·캡처 시각에서 멈추기·"한 번만 일어나는 버튼은 첫 틱만" 은 결정성에 직접 걸리는데 테스트가 닿지 않는다. 10.1 의 묶음 독립성 테스트는 Core 에서는 의미가 약하다(Core 는 틱 하나씩만 돈다).
- **제안**: 그래픽과 무관한 틱 계산을 `Core/Common/TickAccumulator` 로 내리고 테스트한다. 게임 `FrameLoop` 은 그것을 부르기만 한다.

### L14. 몬스터 늘리기를 "정의 1개 추가" 로 실제 시험하는 완료 기준이 없다

- **위치**: 6.13, 11 WP4 완료 기준
- **문제**: 사용자가 다시 "몬스터 종류도 늘려주고" 라고 했다. 6.13 이 구조로 답했지만, 완료 기준에는 "새 적 = JSON 1개" 를 확인하는 항목이 없어 나중에 부품이 정의 밖 상태(H1·M4)에 기대게 되면 조용히 깨진다.
  또 확장 후보 중 `bone_brute` 는 새 코드 0(보스 부품 재사용)이라 MVP 범위 안에서 거의 공짜로 늘릴 수 있다.
- **제안**: WP4 완료 기준에 "`Fixtures/` 에 변형 적 정의 1개를 더하면 로딩·생성·`--preview enemies` 가 코드 변경 없이 통과" 테스트를 넣는다.
  `bone_brute` 같은 새 코드 0 변형을 단계 2·3 에 넣을지는 기획 담당이 정한다(구현 쪽은 비용 거의 없음).

---

## 지적 없음

- 0절 핵심 결정, 2.2 쓰지 않는 것: 범용 ECS·행동 트리·스크립트 언어·셰이더를 빼는 판단에 동의. 규모(적 500)에 맞다.
- 3.1 시계 셋, 3.4 결정성 규칙(H4 의 표시 쪽 제외), 3.5 단계 순서(M1 제외): 그대로 구현 가능.
- 4.1 좌표·방향 열 식(남 0, 서 90, 북 180, 동 270 — `atan2(-dx, dy)` 로 네 방향 대입해 맞음), 4.2 clearance 장, 4.5 시야·선분, 4.6 조향·적분, 4.7 충돌 해소 순서, 4.9 예산.
- 5.4 투사체(선분 질의·`InlineArray` 맞은 목록·`MotionKind` switch), 5.5 장판·예고·`CapGroup`.
- 7.1 가상 해상도·`PresentRT`·캡처, 7.3 층, 7.4 y 정렬, 7.6 장애물 굽기(순수 함수), 7.7 빛 지도 곱하기 혼합, 7.8 커서.
- 8.3 저장(`--autoplay` 면 메모리만 — brief 7절 맞음).
- 12 위험 표(R1~R13).

## API 확인 (이 PC NuGet 캐시의 xml 문서·nuspec)

| 설계서에서 쓰는 것 | 3.8.6 / 1.6.2 에 있나 | 비고 |
|---|---|---|
| `Texture2D.FromStream(GraphicsDevice, Stream, Action<byte[]>)` | 있음 | RGBA 바이트 처리 콜백. 해독+올리기 한 번에(M8) |
| `Texture2D.FromFile(…, Action<byte[]>)` | 있음 | |
| `DefaultColorProcessors.PremultiplyAlpha` / `ZeroTransparentPixels` | 있음 | 색 키 뒤 알파가 0/1 이라 따로 premultiply 불필요(7.5 맞음) |
| `MouseCursor.FromTexture2D(Texture2D, int, int)` | 있음 | |
| `GraphicsDevice.Metrics.DrawCount` (`GraphicsMetrics`) | 있음 | |
| `Game.InactiveSleepTime`, `GraphicsDeviceManager.HardwareModeSwitch`, `Game.IsActive` | 있음 | |
| `Texture2D.SaveAsPng(Stream, int, int)` | 있음 | `RenderTarget2D` 는 `Texture2D` 를 상속 |
| `AlphaTestEffect` | 있음 | R5 대응에 쓸 수 있음 |
| `GraphicsDevice.GraphicsProfile` | 있음 | 문서상 기본 Reach(L5) |
| FontStashSharp `FontSystem.AddFont(byte[])`, `GetFont(float)`, `SpriteBatch.DrawString(SpriteFontBase, …)` 확장 | 있음 | .ttc 색인 인자 없음(L6) |
| StbImageSharp | FontStashSharp.MonoGame 1.6.2 의존성(2.30.16) | 직접 쓰려면 명시 참조(M8) |
| .NET 9: `JsonNamingPolicy.SnakeCaseLower`, `JsonUnmappedMemberHandling.Disallow`, `[InlineArray]`, `GC.GetAllocatedBytesForCurrentThread`, struct 기본 생성자 | 있음 | .NET 8 부터 |
