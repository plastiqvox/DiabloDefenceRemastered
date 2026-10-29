# Diablo Defence Remastered — 설계서 (Architecture)

- 상태: v0.2 (설계 담당). v0.1 에 세 리뷰(`docs/reviews/architecture-planner.md`, `architecture-implementer.md`, `architecture-verifier.md`)를 반영했다 — 지적별 처리는 13절.
  기준 기획서: `docs/gdd.md` v0.3(이번 반영으로 고친 곳은 기획서 18절). 출발점: `docs/brief.md`.
- 이 문서는 **코드 구조·형식·알고리즘·작업 나누기**를 정한다. 수치·규칙은 기획서가 정하고, 여기서는 그것을 어디에 어떻게 담는지만 정한다.
  기획서와 다르게 해석한 곳은 그 자리에 `[해석]` 으로 표시하고 12.1 에 모았다.
- 코드 조각은 인터페이스 시그니처 수준이다. 이름은 구현 때 바뀔 수 있지만 **11절 작업 묶음 사이의 계약**(굵게 표시한 형)은 WP1 이 정한 뒤 고치려면 관련 묶음 담당과 합의한다.
  공용 형의 필드를 누가 쓰고 누가 읽는지는 11.2 표가 정본이다.
- 사용자 요구 8 "몬스터 종류를 늘릴 것"(이번 요청 "몬스터 종류도 늘려주고")은 6.13 에 답한다: 새 적 = JSON 정의 1개(`extends` 로 색·크기·행동 변형),
  모든 적 정의를 자동으로 돌려 보는 연기 시험, 그리고 MVP 몬스터를 14종(+분열체)으로 늘림(`bone_brute`·`hell_goat`, 기획서 8.2).

## 0. 핵심 결정 한눈에

| 주제 | 결정 |
|---|---|
| 프로젝트 | `Core`(그래픽 없는 순수 로직 전부: 데이터·길찾기·시뮬레이션·틱 누적기·장애물 굽기) / 게임(MonoGame: 렌더·입력·화면·앱) / 테스트(Core 만 참조) |
| 개체 모델 | 범용 ECS 를 쓰지 않는다. **종류별 풀(EntityStore) + 고정 순서 시스템**. 개체 클래스는 `partial` 이고 묶음마다 자기 필드 조각을 가진다(11.2). 적의 다양성은 상속이 아니라 **행동 부품(전략) 조합** |
| 데이터 | `Data/**/*.json`(경로 서수 정렬로 읽음), 한 파일에 여러 종류를 담을 수 있음(적 + 그 적의 투사체). `extends` 로 변형 정의. 로딩 때 오류를 모두 모아 보여 줌 |
| 루프 | 고정 60Hz 틱 + 보간 렌더. 모든 시간(메뉴·연출·표시 이펙트·흔들림 포함)은 틱으로 센다 → `--seed` 재현, 캡처 시각 정확, 같은 명령이면 같은 PNG |
| 길찾기 | 16px 격자 + 장애물까지 거리(clearance) 장. 무리 = 몸 크기 2등급 플로우 필드, 보스·중간 보스 = A*(이진 힙, 8방향, 코너 자르기 금지, 끈 당기기). 적 부품은 **이동 의도**만 내고 길은 내비가 정한다 |
| 충돌 | 정확한 도형(원·사각형·다각형) 밀어내기 + 원-원 해소(겹친 점은 결정적 방향), 이웃 질의는 32px 공간 해시 |
| 렌더 | 960×540 월드 렌더 타깃 → 정수배 점 확대 후 선형 축소(레터박스). 빛은 곱하기 빛 지도. 장애물은 CPU 로 구운 스프라이트(질감 샘플·돌출·그림자) |
| 검증 | `--autoplay --seed --capture --capture-at --quit-after --stage --debug-nav --preview --log-ticks --log-hash --check --stress --exit-on-result`. 창을 앞으로 끌어오지 않고, 개발 옵션 실행은 사용자 저장 파일을 **읽지도 쓰지도 않는다** |

---

## 1. 솔루션·폴더 구조

### 1.1 트리

```
DiabloDefenceRemastered/
├─ DiabloDefenceRemastered.sln
├─ global.json                      SDK 9.0.306, rollForward latestFeature
├─ Directory.Build.props            net9.0, Nullable enable, ImplicitUsings, LangVersion latest, 경고 규칙 공통
├─ docs/  tools/  README.md  .gitignore  .gitattributes
├─ src/
│  ├─ DiabloDefenceRemastered.Core/            클래스 라이브러리. MonoGame 참조 없음
│  │  ├─ Common/        EntityId, EntityStore<T>, Pool<T>, SimRandom, StableHash, Facing(방향→열), Rgb, Int2, RectI, MathX,
│  │  │                 AnimClock(클립·프레임·이벤트 프레임), CameraMath(ViewRect), TickAccumulator(누적기·틱 상한·캡처 멈춤)
│  │  ├─ Defs/          정의 record (SpriteDef, EnemyDef, WeaponDef, BuffDef …), DefRef<T>, StatKey, EnemyStatKey
│  │  ├─ Data/
│  │  │  ├─ Registry/   PartRegistry, PartKind, IPartFactory, IDataValidator, ValidationContext, DataError, GameDefs   (계약, WP1)
│  │  │  └─ Loading/    DataLoader, JsonMerge(extends·*_add·*_patch), 변환기, 공통 검증 규칙                       (WP7)
│  │  ├─ Geometry/      Shape(원·사각형·다각형), 부호 거리, 밀어내기, 선분 판정
│  │  ├─ Nav/           NavGrid, ClearanceField, FlowField, AStar, BinaryHeap, PathSmoother, Navigator, PlacementResolver, NavConst,
│  │  │                 Validation/(길 검사)
│  │  ├─ Spatial/       SpatialHash(ISpatialQuery 구현), ObstacleIndex
│  │  ├─ Sim/
│  │  │  ├─ World.cs  Simulation.cs  StageStepper.cs  GameModules.cs
│  │  │  ├─ Contracts/  시스템 계약·틱 이벤트·표시 이벤트·명령·피해 요청·이동 의도·질의 인터페이스
│  │  │  ├─ Entities/   Enemy, Hero, Projectile, Hazard, Telegraph, Pickup 의 공통 조각(partial, WP1)
│  │  │  ├─ Movement/   이동 의도 → 조향·적분·충돌 해소, Enemy.Nav.cs(조각), Invariants/(불변식 계측)
│  │  │  ├─ Hero/  Combat/  Weapons/  Projectiles/  Hazards/  Pickups/  Progression/
│  │  │  ├─ Enemies/    EnemySystem, 생애 상태, Parts/(행동 부품·적 동작), Ranks/(정예·우두머리·접두사), Buffs/
│  │  │  ├─ Director/   생성 누적기, 시간표 이벤트, 상한 정리, 생성 출처·우선순위
│  │  │  └─ Autoplay/   자동 조작 정책(이동·카드 고르기)
│  │  ├─ Art/           ObstacleBaker, PixelBuffer, PngHeader (장애물 그림을 픽셀 배열로 굽기, PNG 크기 읽기)
│  │  ├─ Dev/           DevOptions 파서, TickProfiler, StateHasher(--log-hash)
│  │  └─ Save/          SaveData 모델·직렬화(파일 위치는 게임 쪽)
│  └─ DiabloDefenceRemastered/                 실행 파일. MonoGame.Framework.DesktopGL 3.8.6, FontStashSharp.MonoGame 1.6.2
│     ├─ Program.cs  GameApp.cs
│     ├─ App/
│     │  ├─ Contracts/  IScene, SceneStack, RenderContext, InputFrame                         (계약, WP1)
│     │  └─ FrameLoop(TickAccumulator 사용), DevRunner(캡처·종료·감시), SaveStore, Log, WindowControl   (WP7)
│     ├─ Assets/        지금 그대로 (PNG·cur, git 제외)
│     ├─ Data/          정의 JSON (git 포함)
│     ├─ Loading/       TextureStore(색 키), CursorLoader(.cur), FontProvider
│     ├─ Rendering/     RenderServices, VirtualScreen, Camera, WorldRenderer, Layers, SpriteDraw, Lighting, ObstacleSprites,
│     │                 FxSystem(표시 이펙트), CorpseLayer, DamageNumbers, DebugNavOverlay
│     ├─ Input/         KeyboardMouseSource, InputMap
│     ├─ Scenes/        SceneFactory, Title, StageSelect, Play, Result, DataError, Preview, Overlays/(LevelUp, Chest, Pause, Settings), AutoplayDriver
│     └─ UI/            UiCanvas, Widgets, Hud, Minimap, Theme
└─ tests/
   └─ DiabloDefenceRemastered.Tests/           xunit.v3. Core 만 참조
      ├─ TestKit/       TestWorldBuilder, BruteForceSpatial, StraightNav (WP1)
      ├─ Fixtures/      data/(로더 시험용 작은·고장 난 JSON), variants/(코드 없이 만든 변형 적), perf_scenario.json
      └─ Common/ Data/ Geometry/ Nav/ Spatial/ Movement/ Invariants/ Combat/ Weapons/ Hero/ Progression/
         Enemies/ Director/ Art/ Dev/ Save/ Sim/
```

- 저장소 뿌리에 남아 있는 `DiabloDefenceRemastered/obj/` (net9.0-windows 로 한 번 만든 흔적, git 제외 대상)는 WP1 이 지운다. 실제 프로젝트는 `src/` 아래에만 둔다.

### 1.2 왜 Core 를 별도 프로젝트로 나누나

- 길찾기·전투 계산·데이터 검증뿐 아니라 **시뮬레이션 전체**를 Core 에 둔다. 그래서 테스트가 그래픽 장치 없이 한 판을 통째로 돌릴 수 있다
  (결정성 검사, 성능 측정, 행동 부품 시험). 같은 프로젝트 안 폴더로만 나누면 실수로 `Microsoft.Xna.Framework` 를 쓰는 것을 막을 수 없다.
- Core 는 벡터로 `System.Numerics.Vector2` 를 쓴다. 게임 쪽은 그리기 직전에만 MonoGame `Vector2` 로 바꾼다(확장 메서드 하나 `ToXna()`).
- 애니메이션 **시계**(어느 클립의 몇 번째 프레임인지)는 시뮬레이션 상태다. 타격 프레임·발사 프레임이 게임 규칙이기 때문이다(원작의 "그리기 안에서 상태가 바뀜" 문제를 끊는다).
  텍스처·그리기는 게임 쪽만 안다. Core 는 시트의 **크기·프레임 수·프레임 시간**만 정의로 안다.
- 장애물 그림 굽기도 Core(`Art/`)에 둔다: 맵 픽셀 배열 → 장애물 픽셀 배열. 순수 함수라 테스트할 수 있고, 게임은 결과를 `Texture2D` 로 올리기만 한다.

### 1.3 프로젝트 설정

| 프로젝트 | 종류 | 참조 | 비고 |
|---|---|---|---|
| `DiabloDefenceRemastered.Core` | `net9.0` 라이브러리 | (없음, BCL 의 System.Text.Json 만) | `InternalsVisibleTo` 테스트 |
| `DiabloDefenceRemastered` | `net9.0` WinExe | Core, MonoGame.Framework.DesktopGL 3.8.6, FontStashSharp.MonoGame 1.6.2 | MGCB 없음. 두 `Vector2` 가 함께 보이므로 `global using XnaVector2 = Microsoft.Xna.Framework.Vector2;` 를 두고 XNA 쪽은 늘 이 별칭으로 쓴다 |
| `DiabloDefenceRemastered.Tests` | `net9.0` **xunit.v3**, `OutputType Exe` | Core, `xunit.v3`, `xunit.runner.visualstudio` 3.x, `Microsoft.NET.Test.Sdk` | 게임 프로젝트는 참조하지 않는다. 동적 건너뛰기는 `Assert.Skip` |

- **Assets**: 게임 csproj 가 `Assets\*.png;Assets\*.cur` 를 출력 폴더 `Assets\` 로 복사한다(`PreserveNewest`).
  빌드 전 대상(`CheckAssets`)이 `Assets\Dungeon1.png` 가 없으면
  `error DDR0001: 그림 자원이 없다. tools\import-assets.ps1 로 채운다 (README 참고)` 로 멈춘다. `-p:SkipAssetCheck=true` 로 건너뛸 수 있다(CI).
  실행 중에도 같은 문장을 띄운다(파일 하나라도 없으면 그 이름을 적어 DataError 화면).
- **Data**: 게임 csproj 가 `Data\**\*.json` 을 출력 `Data\` 로 복사한다. 테스트 csproj 는 같은 파일을 링크로 포함해 출력 `Data\` 로 복사한다
  (`<None Include="..\..\src\DiabloDefenceRemastered\Data\**\*.json" LinkBase="Data" CopyToOutputDirectory="PreserveNewest" />`).
  → 실제 데이터 검증 테스트가 그림 없이 돈다.
- 테스트는 그림이 있으면 실제 PNG 크기도 검사한다. PNG 는 해독하지 않고 IHDR 머리글(앞 24바이트)의 폭·높이만 읽는다(`Core/Art/PngHeader`).
  그림 폴더는 환경 변수 `DDR_ASSETS` 또는 저장소 상대 경로 `src/DiabloDefenceRemastered/Assets` 로 찾고, 없으면 그 검사만 `Assert.Skip` 으로 건너뜀 표시한다(13.5-7).
- xunit.v3 는 테스트 프로젝트가 실행 파일(`OutputType Exe`)이어야 하고 `xunit.runner.visualstudio` 3.x 가 있어야 `dotnet test` 가 찾는다.
  빠뜨리면 "테스트 0개" 로 조용히 통과하므로 WP1 완료 기준에 "발견된 테스트 수 > 0" 을 넣는다.
- 테스트 분류(`[Trait("Category", …)]`): 기본(표시 없음, 전체 30초 안 목표), `Scenario`(긴 한 판), `Perf`(Release 측정), `Integration`(모든 묶음 등록이 필요한 것, 통합 때 기본으로 옮김).
  느린 분류는 환경 변수 `DDR_RUN`(예: `Scenario,Perf`)에 그 이름이 없으면 `Assert.Skip` 으로 건너뜀 표시한다 → 옵션 없는 `dotnet test` 가 늘 빠르다.
  골라 돌릴 때는 `DDR_RUN=Perf` 와 `--filter Category=Perf` 를 같이 쓴다.
- CI(나중): `dotnet test tests/DiabloDefenceRemastered.Tests` 만 돌리면 그림 없이 끝난다. sln 전체 빌드는 `-p:SkipAssetCheck=true`.

---

## 2. 디자인 패턴

원칙: **규칙이 늘어나는 곳(적 행동·무기·이벤트·데이터)에는 확장 지점을 두고, 늘지 않는 곳(루프·시스템 순서·렌더 단계)은 단순한 코드로 둔다.**

### 2.1 채택

| 패턴 | 어디에 | 왜 |
|---|---|---|
| **타입 오브젝트** | 모든 정의(`EnemyDef`, `WeaponDef`, `ProjectileDef`, `EffectDef`, `StageDef` …). 인스턴스(`Enemy`)는 `Def` 를 가리키고 바뀌는 값만 가진다 | 원작의 "유닛 종류마다 if/else" 를 없애는 핵심. 새 적 = 새 정의 객체, 코드 분기 없음 |
| **프로토타입(정의 상속)** | JSON `extends` — 부모 정의를 깊은 병합한 뒤 덮어씀. `abstract: true` 는 생성 안 되는 틀 | 몬스터 다양화가 대부분 "같은 그림 + 색·크기·수치·부품 하나 다름" 이라 복사 없이 차이만 적게 한다(6.13) |
| **전략(부품)** | 적 행동 부품(`IEnemyBehavior` 계열)·적 동작(`IEnemyAction`), 무기 발사 패턴(`IFirePattern`), 조준(`ITargeting`), 이벤트 종류(`PartKind.StageEvent`), 이동 의도를 길로 바꾸는 `Navigator`(flow/astar/direct, 4.4) | 기획 13.2·13.3 이 이미 "부품 목록 + 매개변수" 로 짜여 있다. 정의는 부품 ID 와 매개변수만 고른다 |
| **레지스트리(팩토리)** | `PartRegistry`: (`PartKind`, JSON `"type"`) → (매개변수 형, 생성 함수). 부품·동작·발사 패턴·이벤트가 각자 등록. 데이터 검사기도 `IDataValidator` 로 등록 | 로더(WP7)가 구체 부품(WP3·WP4)과 기하 검사(WP2)를 몰라도 된다. 모르는 `type` 은 로딩 오류로 모인다 |
| **상태 패턴** | ① 적 생애 상태(`Entering`→`Emerging`→`Active`, `Stunned`, 죽으면 제거) — 고정된 몇 개라 `enum` + `switch`. 공포·행진·호위는 생애 상태가 아니라 이동 지시(`MoveOrder`, 6.6). ② 적의 독점 행동(공격·점화·돌진·시전)은 `ActionState` 를 소유 부품이 진행. ③ 씬·오버레이(`IScene`). ④ 주인공(`Alive`/`Dying`) | 원작은 상태가 그리기 함수 안 플래그로 흩어져 있었다. 상태마다 들어감·진행·나감이 한 곳에 있어야 "점화 중 죽으면 폭발 안 함" 같은 규칙을 지킬 수 있다 |
| **데코레이터** | 보스 패턴 = `PatternAbility(조건, 애니메이션, 예고, 안쪽 동작 목록)`. 훅 = `TriggeredAction(trigger, 동작)`. 접두사 = 생성 때 정의 위에 덧씌우는 수정자 | 동작(`melee_slam`·`ground_hazard` 등)을 보스·일반 적·접두사가 그대로 같이 쓴다(6.13 `bone_brute`) |
| **이벤트 큐(관찰자, 끌어오기형)** | 시뮬레이션 안: 형별 구조체 큐(`EnemyKilled`, `HitLanded`, `HeroDamaged`, `LevelUpReady` …, 두 칸)를 뒤 단계와 다음 틱이 읽는다. 시뮬레이션 → 화면: `PresentationBuffer`(생긴 틱이 붙은 이펙트·피해 숫자·흔들림·문구·시체) | 콜백 구독은 반복 중 재진입(분열이 죽음 처리 중 적을 추가)과 순서 비결정을 만든다. 큐는 순서가 고정되고 할당이 없다 |
| **오브젝트 풀** | 적·투사체·장판·예고·획득물(`EntityStore<T>`), 게임 쪽 표시 이펙트·시체·피해 숫자·빛 | 초당 수백 개가 생기고 사라진다. 틱 중 GC 할당 0 을 목표로 한다 |
| **명령** | 입력 → 틱마다 `PlayerCommand`(이동 벡터 + 버튼 비트). 출처는 `IInputSource`: 키보드·마우스 / 자동 조작 봇 | 봇과 사람이 같은 경로를 탄다. 시뮬레이션은 입력 장치를 모른다. 나중에 리플레이(명령 기록)도 이 위에 얹을 수 있다 |
| **월드 컨텍스트** | 시스템·부품은 `World` 하나를 인자로 받는다(정의, 난수, 저장소, 해시, 내비, 이벤트, 시계) | 한 판의 모든 상태가 한 객체에 있어 테스트가 월드를 여러 개 만들 수 있다. 정적 싱글턴·서비스 로케이터를 쓰지 않는다 |
| **합성 뿌리(수동 DI)** | 게임 쪽 `GameApp` 생성자에서 한 번 조립(로더, 텍스처 저장소, 씬 스택, 저장소) | 객체 그래프가 작다. DI 컨테이너는 이득보다 간접층이 크다 |

### 2.2 쓰지 않는 것과 이유

| 후보 | 안 쓰는 이유 |
|---|---|
| 범용 ECS(아키타입, Arch·DefaultEcs 등) | 개체 종류가 처음부터 정해져 있다(주인공·적·투사체·장판·획득물). 적 수백 마리는 종류별 배열로 충분히 빠르다. 아키타입 질의·구성요소 조합의 배우는 비용과 디버깅 어려움이 이득보다 크다. 대신 **종류별 저장소 + 시스템** 이라는 ECS 의 장점(데이터를 한데 모아 한 번에 돈다, 순서가 명시적)만 가져온다 |
| 클래스 상속 계층(`Enemy` → `Skeleton` → `BurningSkeleton`) | 원작 문제의 재현. 행동은 섞여야 하는데(원거리 + 장판, 근접 + 강타) 상속은 하나만 고른다 |
| 행동 트리 / GOAP | 적 행동이 "이동 방식 1개 + 조건 붙은 능력 몇 개" 로 끝난다. 부품 + 상태로 다 적힌다. 행동 트리는 데이터 형식·편집기까지 커진다 |
| 스크립트 언어(Lua 등) | 정의 + 부품으로 기획 13절 전부가 표현된다. 언어 경계가 디버깅·결정성을 어렵게 한다 |
| 콜백형 이벤트 버스(`event Action<T>`) | 2.1 이벤트 큐 이유. 게임 쪽 UI 처럼 틱 밖에서 한 번 읽는 곳도 버퍼를 읽는다 |
| 커스텀 셰이더(.fx) | MGCB 를 안 쓰므로 효과 파일 컴파일 도구(mgfxc)가 따로 필요하다. 번쩍임·실루엣·빛은 혼합 상태만으로 만든다(7절). 필요해지면 위험 R6 대응으로 다시 본다 |
| 멀티스레드 시뮬레이션 | 목표 규모(적 500)에서 한 스레드로 틱 예산 안에 든다(4.8). 결정성을 잃을 위험만 커진다. 그림 PNG 읽기도 주 스레드에서 차례로 한다(7.5) |

---

## 3. 게임 루프

### 3.1 시간 모델

- **틱 = 1/60초 고정**(`World.Dt`). 시뮬레이션의 모든 속도·시간은 "초" 단위 실수이고 틱마다 `Dt` 를 곱한다.
- 시계는 셋이다.

| 시계 | 무엇 | 멈추는 때 |
|---|---|---|
| 앱 틱 `AppTick` | 프로그램 시작부터 센 틱. 메뉴·오버레이·연출도 이것으로 진행. `--capture-at`, `--quit-after` 의 기준 | 안 멈춤 |
| 스테이지 틱 `World.Tick` / `StageTime` | 한 판의 게임 시간. 생성 구간·이벤트·시간 배율의 기준 | 일시정지, 레벨업·상자 창, 히트 스톱 |
| 실제 시간 | 프레임 사이 벽시계. 누적기에만 쓰고 게임 규칙에는 안 쓴다 | — |

- MonoGame 설정: `IsFixedTimeStep = false`, `SynchronizeWithVerticalRetrace = true`(`--vsync off` 면 false). 고정 틱은 직접 누적기로 돈다(MonoGame 고정 시간 모드는 보간 값을 주지 않는다).

### 3.2 프레임 하나

틱 계산(누적기·틱 상한·캡처 멈춤)은 그래픽과 무관하므로 Core `Common/TickAccumulator` 에 두고 테스트한다. 게임 `FrameLoop` 은 그것을 부르기만 한다.

```
Update(gameTime):
  ticks = accumulator.Advance(gameTime.ElapsedGameTime, timeScale)
          // real = min(경과, 0.25s), acc += real × timeScale, 최대 8 × timeScale 틱. 넘는 만큼은 버리고 "버린 틱" 을 센다
  input.Sample()                                       // 사람 입력: 프레임마다 한 번. --autoplay 면 실제 장치를 만들지 않는다
  repeat ticks:
     if accumulator.StopAt(AppTick) → 남은 틱을 미루고 이번 프레임에 캡처(9절)
     scenes.FixedTick(AppTick, inputFrame)             // 위 씬부터: 오버레이가 PausesBelow 면 아래 씬은 진행 안 함
     AppTick++
Draw:
  scenes.Draw(alpha)                                   // alpha 는 씬이 정한다(아래). 위치 = lerp(PrevPos, Pos, alpha)
```

- **입력**: 사람 입력은 프레임마다 한 번 읽어 그 프레임의 모든 틱에 같은 이동 벡터를 준다. 한 번만 일어나야 하는 버튼(1/2/3, Esc)은 첫 틱에만 넣는다.
  자동 조작 봇은 **틱마다** 월드를 보고 명령을 만든다 → 프레임에 틱이 몇 번 들어가든 같은 결과(결정성, 3.4).
  `--autoplay` 면 `KeyboardMouseSource` 를 만들지 않는다 — 창이 우연히 포커스를 받아도 사용자의 키(Esc·F11·Tab)가 판을 바꾸지 않는다.
- **버린 틱**: 틱을 버려도 결정성은 깨지지 않는다. 규칙·캡처·종료가 모두 앱 틱 기준이라 벽시계와 어긋날 뿐이다(`dropped_ticks` 로 기록).
- **보간**: 개체는 `PrevPos`(틱 시작 때 저장)와 `Pos` 를 가진다. 새로 생긴 개체·순간 이동(입장 시작, 분열체 튕김 시작)은 `PrevPos = Pos` 로 맞춰 미끄러짐을 막는다.
  `alpha` 는 보통 `acc / Dt`. 일시정지 중에는 마지막 값으로 고정하고, 보스 처치 슬로모션 중에는 `StageStepper` 의 늘임 누적값(0..1)을 쓴다
  (앱 누적기 값을 쓰면 위치가 3~4프레임 멈췄다 한 번에 뛴다). 캡처 프레임은 `alpha = 1`.

### 3.3 멈춤·느려짐

스테이지 틱을 돌릴지 정하는 규칙은 Core `Sim/StageStepper`(WP1) 한 곳에 둔다. `PlayScene.FixedTick` 은 앱 틱마다 `StageStepper.AppTick(world, command)` 를 부르기만 한다 → 테스트할 수 있다.

| 상황 | 방법 |
|---|---|
| 일시정지(Esc/P), 레벨업 창, 정예 상자 연출 1.2s | 오버레이가 `PausesBelow = true` → `PlayScene.FixedTick` 이 불리지 않는다. 오버레이 자신의 애니메이션은 앱 틱으로 진행 |
| 레벨업 발생 | 시뮬레이션이 틱 끝(`EndTick`)에 `World.AwaitingChoice` 를 켠다. `PlayScene.FixedTick` 은 `Step` 직후 이것을 보면 **같은 앱 틱 안에서** 레벨업 오버레이를 올리고 돌아간다. `FrameLoop` 는 틱을 끊지 않는다 — 같은 프레임의 남은 틱은 오버레이가 받는다. 레벨업 틱과 오버레이가 올라가는 앱 틱의 간격이 프레임 묶음과 상관없이 늘 0 이다 |
| 선택 뒤 | 오버레이가 `IProgression.Choose/Reroll/Skip` 을 부르고 닫힌다. 다음 스테이지 틱의 `HeroControl` 이 주인공 반경 150 안 적을 80px 밀어낸다(피해 0 넉백 요청, 기획 12.6) |
| 창이 비활성 | 사람 조작 중이면 일시정지 오버레이를 연다. `--autoplay`·`--background` 면 열지 않고 계속 돈다(`InactiveSleepTime = 0`) |
| 히트 스톱 0.08s | 스테이지 시계가 5틱 동안 진행하지 않는다(`World.Freeze(ticks)`, `StageStepper` 가 센다) |
| 보스 처치 슬로모션 ×0.3 | 틱 길이는 그대로 두고, 앱 틱마다 0.3 을 쌓아 1 이 될 때만 스테이지 틱을 한 번 돈다(`StageStepper` 의 늘임 누적기). 고정 `Dt` 가 유지되어 물리·결정성이 그대로다 |
| 승리·패배 뒤 결과 화면까지(3s / 2s) | 스테이지 틱으로 센다. 승리 처리(남은 적 무너짐 등)는 3.5 끝 |

### 3.4 결정성 규칙

- 난수는 `SimRandom`(xoshiro128\*\*, 씨앗은 SplitMix64) 하나에서 용도별 흐름을 나눈다: `Spawn`, `Drop`, `LevelUp`, `Ai`, `Weapon`, `Rank`.
  한 용도에서 난수를 한 번 더 뽑아도 다른 용도의 결과가 바뀌지 않아 버그를 좁히기 쉽다.
- **표시용 시간·난수도 틱으로 센다.** 게임 쪽에는 프레임마다 뽑는 상태 난수가 없다. 표시 값은 모두 "틱과 ID 로 계산하는 함수"다:
  흔들림 오프셋 = `StableHash(seed, stageTick)`, 화로 깜빡임 = `StableHash(seed, lightId, stageTick)`, 장식 = `StableHash(seed, 개체 ID)`.
  표시 이벤트(이펙트·시체·피해 숫자)는 **생긴 스테이지 틱**을 싣고, 그릴 때 "나이 = 현재 틱 − 생긴 틱" 으로 계산한다(진행량을 누적하지 않는다).

| 표시 요소 | 시계 |
|---|---|
| 월드 안 이펙트·시체·피해 숫자·흔들림·화로 깜빡임·기둥 맥동·정예 맥동·발광 번쩍임·광폭화 맥동 | 스테이지 틱(멈추면 같이 멈춘다) |
| 타이틀 로고·메뉴·오버레이 애니메이션(레벨업 카드 올라옴, 빛 기둥) | 앱 틱 |
| 마우스 커서 위치 | 실제 입력(캡처에는 그리지 않는다) |

- **데이터 순서**: `Data/**/*.json` 은 경로를 서수(ordinal) 정렬해 읽는다. JSON 객체로 적은 가중치·패치는 로딩 때 **JSON 에 적힌 순서의 배열**로 바꾼다(`weights` → `(id, weight)[]`).
  정의 목록 순서는 (파일 순서, 파일 안 순서)로 고정된다.
- 시뮬레이션에서 `Dictionary`/`HashSet` 순회 결과에 의존하지 않는다(저장소는 배열 순서, 사전은 찾기에만). `Parallel`·스레드 없음. 벽시계 읽지 않음.
- 같은 PC·같은 빌드 구성에서 같은 씨앗이면 같은 결과를 보장한다(다른 CPU 간, Debug↔Release 간 부동소수 일치는 보장하지 않는다 — 해시 비교는 같은 구성끼리만).
- 재현 정보는 로그 첫 줄에 모은다: `seed=… config=Release|Debug commit=<있으면> data=<Data 내용 해시> args="<전체 명령줄>"`. `--seed` 가 없으면 시각에서 씨앗을 만들어 여기 적는다.
- 확인 수단: Core 의 상태 해시(`StateHasher`: 위치·HP·처치·레벨·난수 흐름 상태·개체 수)를 시험과 게임(`--log-hash`)이 같이 쓴다. 자동 조작의 레벨업 선택은 Core `AutoplayPolicy` 하나라
  Core 시나리오 시험의 해시와 게임 실행의 해시를 같은 스테이지 틱에서 비교할 수 있다.

### 3.5 시스템 실행 순서 (스테이지 틱 하나)

`Simulation` 이 `SimPhase` 순서대로 `ISimSystem.Tick(World)` 를 부른다. **한 단계 = 시스템 하나**(두 번째 등록은 `SimulationBuilder` 가 예외). 순서는 이 표가 정본이다.

| # | 단계 `SimPhase` | 하는 일 | 담당 |
|---|---|---|---|
| 1 | `BeginTick` | `PrevPos ← Pos`, 틱 이벤트 큐 바꿔 끼우기(아래), 시계 진행, 버프·무적·번쩍임 타이머 감소 | WP1 |
| 2 | `Director` | 시간표 이벤트, 일반 생성 누적기, 정예 판정(60s), 상한 정리(먼 적 `Culled`). `World.Outcome` 이 `Running` 이 아니면 아무것도 안 함 | WP4 |
| 3 | `HeroControl` | 명령 → 주인공 이동 의도·바라보는 방향, 체력 재생, 애니메이션, 레벨업 밀어내기 요청 | WP3 |
| 4 | `Navigation` | 플로우 필드 갱신(분산), A* 요청 처리(틱당 최대 2건) | WP2 |
| 5 | `EnemyThink` | 생애 상태, 이동 지시, 부품(오라·능력·위치 잡기) → **이동 의도**·독점 행동, 애니메이션, 타격·발사 프레임 → 피해 요청·투사체 생성, 표시 상태(`Visual`) | WP4 |
| 6 | `Locomotion` | 공간 해시 짓기 → 이동 의도를 희망 속도로(`Navigator`) → 분리 조향 → 속도 적분(넉백 포함) | WP2 |
| 7 | `Collision` | 적-적 원 해소(2회), 주인공-적 규칙(닿은 수), 장애물·맵 경계 밀어내기 | WP2 |
| 8 | `SpatialRebuild` | 이동이 끝난 위치로 해시 다시 짓기(무기 조준·맞음 판정용) | WP2 |
| 9 | `Weapons` | 쿨다운, 조준, 발사(투사체·장판 생성), 회전 구체 위치 | WP3 |
| 10 | `Projectiles` | 이동·유도, 장애물 막힘, 맞음 판정 → 피해 요청, 폭발 | WP3 |
| 11 | `Hazards` | 장판·지연 폭발·예고 진행 → 피해 요청 | WP3 |
| 12 | `DamageResolve` | 피해 요청 적용(방어·치명·무적·배율), 넉백, 번쩍임, `HitLanded`·피해 숫자 표시 이벤트, HP ≤ 0 → `Dead`(`KillCause.Damage`) | WP3 |
| 13 | `DeathHooks` | 죽은 적: 진행 중 행동 끊기, 죽음 훅(분열·장판·폭발), 호위 공포, 보스 처치 → 승리 처리(아래), `EnemyKilled` 틱 이벤트·`CorpseSpawned` 표시 이벤트 | WP4 |
| 14 | `DeathRewards` | `EnemyKilled` 중 `Cause == Damage` 만: 보석·드롭·처치 수·`RunStats` | WP3 |
| 15 | `Pickups` | 끌어당김·줍기, 경험치 → 레벨업 대기, 물약·룬·상자 | WP3 |
| 16 | `EndTick` | 승패 판정 타이머, 승리 대기면 적 투사체·장판·예고 지우기, 레벨업·상자 대기 → `AwaitingChoice`, 저장소 압축(지운 칸), 디버그 빌드 불변식(`float.IsFinite(Pos)`) | WP1 |

- 틱 중에 생긴 개체는 저장소 끝에 붙고 **다음 틱부터** 행동한다(`SpawnTick` 비교). 지우기는 표시만 하고 16 에서 한꺼번에 압축한다. 밀집 인덱스는 압축 전까지만 유효하다.
- **죽음 이유 `KillCause`**: `Damage`(보통 처치 — 훅·보석·드롭·처치 수·시체), `Collapse`(승리 때 무너짐 — 시체만, 훅·보상·처치 수 없음), `Culled`(상한 정리 — 아무것도 없음, 시체도 없음).
- **틱 이벤트 큐는 두 칸**이다: 이번 틱에 쓴 이벤트는 같은 틱의 뒤 단계와 **다음 틱 전체**에서 읽을 수 있다(`Events.Current` / `Events.Previous`). `BeginTick` 이 바꿔 끼우고 오래된 칸을 비운다.
  예: `DamageResolve`(12) 가 낸 `HitLanded` 를 다음 틱 `EnemyThink`(5) 가 읽어 흡혈을 처리한다(1틱 늦음 허용).
- **표시 이벤트(`PresentationBuffer`)는 틱마다 비우지 않는다.** 각 항목에 생긴 스테이지 틱을 싣고, 프레임 끝에 게임이 소비(`FxSystem.Consume` 등)한 뒤 비운다.
  한 프레임에 여러 틱이 돌아도 앞 틱의 이벤트가 사라지지 않는다.
- 해시를 6 과 8 에서 두 번 짓는다. 300~500 개 재배치는 수십 µs 라 한 번 짓고 낡은 값을 쓰는 것보다 단순하고 정확하다.
- **승리 처리**(기획 2.2): `DeathHooks` 가 `rank: boss` 인 적이 `Damage` 로 죽은 것을 보고 스테이지 `victory.type == boss` 면 그 틱에
  `World.Outcome = VictoryPending` → 살아 있는 적 전부 `Dead`(`Collapse`) → `EndTick` 이 적 투사체·장판·예고를 지운다. 그 뒤 `Director` 는 아무것도 하지 않고
  `DamageResolve` 는 주인공 피해를 무시한다. 같은 틱에 주인공 HP 도 0 이 되었으면 승리가 먼저다. 3초(스테이지 틱) 뒤 결과.
  `victory.type == survive` 면 `EndTick` 이 그 시각에 같은 처리를 한다(단계 1 세로 조각, 6.12).

---

## 4. 이동·충돌·길찾기

### 4.1 좌표와 단위

- 월드 좌표 = 맵 픽셀(1536×1024), `float`. 유닛 위치 = **발 위치**(기획 8.1). 속도 px/s, 각도는 데이터에서 도(°), 로딩 때 라디안.
- 방향 → 시트 열: `angle = atan2(-dx, dy)` 를 0..360 으로(남 0°, 서 90°, 북 180°, 동 270°), `열 = round(angle / (360/방향 수)) mod 방향 수`. `Facing.ToColumn(Vector2, int dirs)` 한 곳에만 둔다.
  brief 가 "그림으로 확인할 것" 이라 했으므로 `--preview` 화면(9절)에서 12방향을 돌려 맞는지 본다. 틀리면 이 함수의 오프셋 하나만 고친다.

### 4.2 장애물 도형과 내비 격자

- **도형 하나가 정본**: 맵 `collision` + 스테이지 `obstacles`(부품 `parts` 중 충돌하는 것)를 `Shape`(원 / 사각형 / 다각형) 목록으로 만든다.
  같은 목록에서 ① 충돌 해소(정확한 도형), ② 내비 격자, ③ 투사체 막음, ④ 장애물 그림(7.6)을 만든다 → 그림과 충돌이 어긋날 수 없다.
- **격자**: 칸 16px → 96×64 = 6,144칸. 이유: 가장 작은 유닛 반경 9, 통로 80px = 5칸이라 충분히 곱다. 8px(24,576칸)는 필드 비용이 4배인데 얻는 것이 적다.
- **clearance 장(여유 거리)**: 칸 중심에서 가장 가까운 장애물 도형·맵 경계까지의 **부호 있는 거리**(px, 도형 안이면 음수)를 정확히 계산해 `float[]` 로 둔다.
  도형 25개 × 6,144칸 ≈ 15만 번 거리 계산 → 스테이지 시작 때 1ms 안팎. 장애물이 움직이지 않으므로 한 번만 만든다.
  - "반경 r 유닛이 이 칸에 설 수 있다" = `clearance ≥ r`. 장애물을 반경만큼 부풀려 칸에 찍는 방식과 같은 뜻이지만, 칸 반올림 없이 **실수 거리**라 80px 틈이 칸 경계 때문에 막히는 문제(리뷰 M1)가 줄어든다.
  - 맵 경계도 벽으로 친다. 입장 중인 유닛은 격자를 쓰지 않는다(직선).
  - 기울기(이웃 칸 clearance 차)는 "벽 안으로 밀려 들어간 유닛이 나갈 방향" 으로 쓴다.

### 4.3 무리용 플로우 필드

- **누가**: `movement: flow` 인 적(일반·정예·우두머리)이 "주인공 쪽으로"(`SeekHero`) 이동 의도를 낼 때. 몸 크기 2등급으로 필드를 둘 둔다.

| 필드 | 통과 조건 | 쓰는 유닛 |
|---|---|---|
| `S` | clearance ≥ `NavConst.FieldS` (16) | 몸 반경 ≤ 16 (해골·좀비·궁수·지옥 염소·대부분의 정예) |
| `M` | clearance ≥ `NavConst.FieldM` (26) | 몸 반경 16 초과 (시체 덩어리 20, 뼈 거인 20, 정예·우두머리 덩어리 23·26, 해골 대장 우두머리 19.5 등). 26 을 넘는 flow 유닛은 데이터 검증에서 경고 |

- 문턱 값은 `NavConst` 한 곳에 두고 필드·길 검사(13.5-5)·생성 위치 검사가 같이 읽는다.
- **계산**: 목표 = 주인공. 주인공 둘레 (주인공 반경 + 16)px 안의 그 필드 통과 칸을 모두 시작점(거리 0)으로 하는 다중 시작 다익스트라.
  **그 반경 안에 통과 칸이 없으면**(주인공이 모서리·벽에 바짝 붙어 `M` 문턱을 넘는 칸이 근처에 없음) 주인공 칸에서 BFS 로 찾은 가장 가까운 그 필드 통과 칸들(같은 거리 전부)을 시작점으로 한다.
  8방향, 비용 10/14(정수), **대각선은 두 직교 이웃이 모두 통과 가능할 때만**(코너 자르기 금지). 결과: `ushort dist[]`(65535 = 못 감), `byte dir[]`(가장 작은 이웃 방향).
  우선순위 큐는 A* 와 같은 이진 힙. 6,144칸 기준 0.1~0.2ms.
- **갱신 주기·분산**: 주인공이 다른 칸으로 옮겼을 때만 다시 계산하되, `S` 는 `Tick % 6 == 0`, `M` 은 `Tick % 6 == 3` 에만 한다(각 0.1s 주기, 한 틱에 둘이 겹치지 않음).
  계산은 한 틱 안에서 동기로 끝나므로 **필드 한 벌을 제자리에서** 다시 쓴다(읽는 쪽이 반쯤 된 필드를 볼 일이 없다). **범위는 맵 전체**: 맵이 작아(6,144칸) 범위를 자를 이유가 없다.
- **방향 읽기**: 유닛 위치를 둘러싼 4칸의 `dist` 를 이중선형 보간해 기울기를 구한다(격자 계단 모양 없이 부드러운 흐름). 4칸 중 못 가는 칸이 섞이면 자기 칸의 `dir` 을 쓴다.
  못 가는 칸(dist 65535)에 있으면 clearance 기울기로 빠져나온다.
- **가까우면 직진**: 주인공까지 200px 안이고 시야가 트이면(4.5 시야 검사, 자기 반경) 필드 대신 주인공 쪽으로 곧장 간다. 시야 검사는 비싸므로 적마다 6틱에 한 번(`id % 6` 으로 분산), 결과를 `NavAgentState` 에 저장해 둔다.
- **끼임 감지**: 쫓는 중인데 1초 동안 4px 도 못 움직였고 공격 사거리 밖이면, 0.5초 동안 진행 방향의 옆(왼쪽/오른쪽은 `Ai` 난수)으로 비켜 간다.
- 위 규칙은 전부 WP2 의 `Navigator`(4.4 끝) 안에 있다. 적 부품은 "주인공 쪽으로" 라는 의도만 낸다.

### 4.4 개별 유닛용 A*

- **누가**: `movement: astar` (중간 보스·보스, E 단계 아군). 동시에 많아야 4~6.
- **격자**: 같은 16px 격자, 통과 조건 `clearance ≥ 자기 반경`.
- **알고리즘**: 8방향, 비용 1 / √2(실수), 휴리스틱 옥타일 거리(허용적), 같은 f 면 h 가 작은 쪽. 대각선은 두 직교 이웃이 통과 가능할 때만(코너 자르기 금지).
- **자료구조**: 직접 만든 **이진 힙**(배열, 칸 번호 → 힙 위치 표로 decrease-key 지원). 칸별 `g`, `parent`, `stamp`(이번 탐색 번호) 배열을 미리 잡아 두고
  `stamp` 가 다르면 "아직 안 봄" 으로 친다 → 탐색마다 배열을 지우지 않는다. 할당 0.
- **시작·목표가 막혔을 때**: 시작 칸이 막혀 있으면(밀려 들어감) 반경 3칸 안 가장 가까운 통과 칸에서 시작한다. 3칸 안에도 없으면 경로 없이 clearance 기울기로 0.25s 움직인 뒤 다시 요청한다.
  목표 칸에 못 들어가면(주인공이 큰 몸이 못 가는 틈에 있음) 탐색 중 h 가 가장 작았던 칸까지의 경로를 돌려준다(갈 수 있는 가장 가까운 곳).
- **경로 다듬기**: 끈 당기기(string pulling). 현재 점에서 시야 검사(4.5, 자기 반경)가 통과하는 가장 먼 경유점으로 건너뛴다. 결과는 꼭짓점 몇 개짜리 꺾은선.
- **따라가기·재계획**: 경유점 8px 안이면 다음으로. 재계획 조건: 0.5s 지남, 목표가 마지막 목표에서 32px 넘게 움직임, 끼임 감지. 목표가 시야에 있으면 A* 없이 곧장 간다.
  요청은 `Navigator` 가 내비 안 줄에 세우고 `Navigation` 단계가 틱당 최대 2건 처리한다. 결과는 그 적의 `NavAgentState` 안 **고정 길이 경유점 배열**(`InlineArray(16)`, 넘치면 앞 16개 + 끝에서 재계획)에 바로 쓴다.
  결과가 오기 전에는 직전 경로나 직진을 쓴다.

**이동 의도와 `Navigator` — 적 부품과 내비의 경계**

적 부품(WP4)은 길을 직접 다루지 않고 틱마다 **이동 의도**만 쓴다. 의도를 실제 희망 속도로 바꾸는 일(플로우·A*·직진·끼임·미끄러짐)은 WP2 의 `Navigator` 가 `Locomotion` 단계에서 한다.
행진·호위 따라가기·도망·거리 유지가 모두 같은 길 처리를 타므로 장애물 미끄러짐이 한 곳에 모인다. 호출은 WP2 → (의도 읽기) 한 방향뿐이다.

```csharp
public enum MoveKind : byte { Hold, SeekHero, SeekPoint, Away, Heading, Straight }   // 계약 (WP1)
public struct MoveIntent
{
    public MoveKind Kind;
    public Vector2 Point;       // SeekPoint 의 목표, Away 의 도망칠 기준점
    public Vector2 Dir;         // Heading(행진)·Straight(입장·돌진) 방향
    public float SpeedMul;      // 1 = Enemy.MoveSpeed 그대로. 돌진처럼 자기 속도가 있으면 그 비율
}
// Hold: 멈춤. SeekHero: 정의 movement(flow/astar/direct)대로 주인공에게. SeekPoint: astar 면 A*, 아니면 직진 + 미끄러짐.
// Away: 기준점 반대로 + 미끄러짐. Heading: 방향 고정 + 미끄러짐. Straight: 미끄러짐 없는 직선(입장·돌진. 막히면 충돌 단계가 멈춘다)
```

- `Navigator` 가 적마다 쓰는 상태(`NavAgentState`: 시야 캐시, 끼임 타이머·비켜 가기 방향, 경로 경유점·번호·재계획 시각, 막힘 타이머)는 WP2 조각 파일 `Enemy.Nav.cs` 에 둔다.
- `Navigator` 는 "희망 방향으로 실제 이동이 0.5s 넘게 막힘" 을 공통 필드 `Enemy.MoveBlockedTime` 에 적는다. `kite` 가 물러날 수 없을 때 제자리에서 쏘는 판정(기획 8.3), 돌진이 장애물에 닿아 끝나는 판정에 쓴다.

### 4.5 시야·선분 검사

- `INavWorld.HasClearLine(a, b, radius)`: 선분을 4px 간격으로 걸으며 그 점의 clearance(이중선형) ≥ radius 인지 본다. 플로우 직진 판단·A* 다듬기·돌진 시작 조건·분열체 튕김에 쓴다.
- 투사체 막음은 격자가 아니라 **정확한 도형**으로 한다: `ObstacleIndex`(64px 칸마다 걸치는 도형 번호 목록)로 후보를 고르고 선분-도형 교차 검사. 대상은 `blocks_projectiles` 도형만.
  원거리 적의 "쏠 수 있나" 도 같은 함수(`INavWorld.ProjectileLineClear(a, b)`)로 본다.

### 4.6 조향·적분

1. **희망 속도**: `Navigator` 가 이동 의도 → 방향을 정하고 × `Enemy.MoveSpeed`(WP4 가 버프·둔화·광폭화·접두사를 반영해 매 틱 쓴 값) × `SpeedMul`.
2. **분리(separation)**: 공간 해시로 이웃을 찾아, 거리 d < (rᵢ + rⱼ + 8) 인 이웃마다 `방향(나 − 이웃) × (1 − d/(rᵢ+rⱼ+8))` 를 더한다. 희망 방향에 가중 1.2 로 더한 뒤 정규화 × 속도.
   멈춰 있는(`Hold`) 이웃은 가중 2 로 피해 돌아가게 한다. 이웃은 가까운 순 최대 8개만 본다.
3. **가속 제한**: 속도 변화량을 초당 (속도 × 8) 로 제한해 방향 떨림을 줄인다.
4. **넉백**: `KnockbackVel = 방향 × 거리 / 0.12`, 0.12s 동안 희망 속도 대신 더한다. 넉백 저항·주인공 `knockback_taken_pct` 는 `DamageResolve` 가 거리에 이미 곱해 둔다.
5. `Pos += Vel × Dt`.

- **겹친 점(d < 1e-4)**: "방향(나 − 이웃)" 이 정의되지 않는다. 이때 방향 = `MathX.TieAngle(idA, idB)`(두 개체 ID 의 해시로 정한 각도, 상대 쪽은 반대 방향).
  같은 입구에서 같은 직선으로 들어온 행진 20마리, 분열 3마리, 겹친 소환이 이 규칙으로 결정적으로 벌어진다. 4.7 의 원-원 해소도 같은 함수를 쓴다.

### 4.7 충돌 해소

순서가 중요하다: **부드러운 것 → 단단한 것**.

1. **적-적**: 해시에서 쌍(i<j)을 찾아 겹침 = rᵢ + rⱼ − d > 0 이면 질량(r²) 비로 나눠 민다. 2회 반복. 장애물 통과(`phase_through`) 유닛도 적끼리는 민다.
   입장 중·등장 중(`Entering`·`Emerging`) 적은 해시에 없으므로(4.8) 밀지도 밀리지도 않는다. 진입점에 닿아 `Active` 가 되는 틱부터 4.6 의 겹친 점 규칙으로 벌어진다.
2. **주인공-적**(기획 4.1): 일반 적·정예는 주인공을 질량 무한 원으로 보고 적만 밀린다. 주인공에 닿은 적 수를 `Hero.TouchingCount` 에 쓴다(3 이상이면 다음 틱 주인공 이동 ×0.6, WP3 가 읽음).
   중간 보스·보스·반경 20 이상 적은 주인공에게 단단하다 → 주인공이 밀린다.
3. **장애물·경계**: 원-도형 최근접점으로 밖으로 민다(미끄러짐이 저절로 생긴다). 다각형은 변마다 최근접점. 오목 다각형(덤불 띠)도 변 단위라 문제없다.
   원 도형의 **중심에 정확히 놓인** 유닛은 최근접점 방향이 없으므로 clearance 기울기, 그것도 0 이면 `MathX.TieAngle(id, 도형 번호)` 방향으로 민다.
   `Enemy.PassThroughObstacles`(`phase_through` 가 켬) 유닛은 장애물을 건너뛰고 경계만. 입장 중 유닛은 둘 다 건너뛴다.
4. 마지막에 아직 도형 안이면(좁은 틈에 여럿이 몰린 경우) clearance 기울기로 한 번 더 민다.

### 4.8 공간 해시

- 칸 32px(가장 흔한 상호작용 거리 rᵢ+rⱼ+8 ≈ 32), 맵 48×32 = 1,536칸.
- **넣는 것**: `Life == Active`(또는 `Stunned`) 인 적만. 입장 중·등장 중(`Entering`·`Emerging`) 적은 **위치와 상관없이** 넣지 않는다 → 조준·맞음 판정·장판·분리에서 모두 빠진다.
  기획 2.4 "입장·등장 중에는 피해를 주지도 받지도 않음" 을 규칙 하나로 지킨다(맵 안 진입 구간에서도 유도 미사일이 무적 유닛을 고르지 않는다).
- **계수 정렬 방식**: 칸별 개수 세기 → 누적합 → `entries[]` 에 채우기. 배열 3개를 재사용하므로 할당 0, O(n).
- **질의 계약 `ISpatialQuery`**(WP1 계약, WP2 구현, 시험용 전수 구현은 `TestKit/BruteForceSpatial`):

```csharp
public interface ISpatialQuery
{
    int Query(Vector2 center, float radius, Span<int> denseIdx);              // 원에 걸치는 칸의 적
    int QueryCapsule(Vector2 a, Vector2 b, float radius, Span<int> denseIdx); // 투사체 이동 선분 둘레
    int CellCount(int cx, int cy);                                             // 조준 densest
}
```

- 결과는 적 저장소의 **밀집 인덱스**이고 그 틱 `EndTick` 압축 전까지만 유효하다. 결과 순서 = 칸 순서 + 칸 안 삽입 순서 → 결정적.
- **넘침**: 결과 칸이 모자라면 칸 순서대로 앞에서부터 채우고 **실제 개수(채운 수보다 클 수 있음)** 를 돌려준다. 가까운 순이 정확해야 하는 호출자(분리)는 128칸으로 질의하고,
  돌려받은 수가 128 을 넘으면 반경을 절반으로 줄여 다시 질의한다. 넘침 처리도 결정적이다.
- 주인공 투사체가 적을 찾을 때 반경 = 투사체 반경 + 최대 적 반경(정의 로딩 때 구함).

### 4.9 규모 목표와 60fps 예산

| 항목 | 기획 상한 | 성능 목표(시험 규모) |
|---|---|---|
| 살아 있는 적 | 300 (절대 360) | **500** |
| 주인공 투사체 / 적 투사체 | 400 / 200 | 600 / 300 |
| 장판·예고 | 독 웅덩이 20 + 무기 장판 수십 | 80 |
| 보석 | 300 + 더미 | 300 |
| 시체 / 빛 / 피해 숫자 | 150 / 64 / 120 | 같음 |

**합격 기준은 아래 표 하나다**(다른 절·완료 기준의 성능 숫자는 이 표를 가리킨다).

| 무엇 | 어떻게 재나 | 완료 기준 | 회귀 감지(느린 PC·CI) |
|---|---|---|---|
| 스테이지 틱 1회 (적 500·투사체 600) | `Perf` 시험, `Fixtures/perf_scenario.json`, Release, 이 개발 PC, 워밍업 600틱 뒤 3,600틱 | 평균 ≤ 2.0ms, p99 ≤ 4.0ms | 평균 < 4ms |
| 그중 Navigation + Locomotion + Collision | 같은 시험의 단계별 시간 | 평균 ≤ 1.0ms (필드 갱신이 있는 틱은 +0.4ms 까지) | — |
| 틱 중 GC 할당 | 같은 시험, `GC.GetAllocatedBytesForCurrentThread` 차이 | 0 B | 0 B |
| 렌더 CPU (그리기 준비 + SpriteBatch) | 게임 `--stress 300 --freeze-ai --vsync off --log-ticks`, 첫 5초 뺀 값 | p99 ≤ 6ms | — |
| 그리기 호출(draw call) | 같은 실행의 `draw_calls` | ≤ 600 (7.4) | — |

- `perf_scenario.json` 은 씨앗·적 구성(스테이지 2 의 10:00 구간 가중치)·무기(4종 Lv8)·주인공 정지·틱 수를 고정한다. 구성을 바꾸면 숫자를 비교할 수 없으므로 바꿀 때는 기준값도 다시 잰다.
- 대략 계산: 분리·충돌은 적 1마리당 이웃 ~8개 → 500 × 8 × 2회 = 8,000 쌍 계산, 1ms 미만. 투사체 600 × 해시 질의 ~4칸 → 0.3ms.
- 틀린 예상이면 10.3 측정으로 찾아 4.3 의 시야 검사 주기·이웃 수 상한부터 줄인다.

### 4.10 놓을 자리 찾기 `PlacementResolver`

소환·호위·분열 튕김·정예 무리·포위 고리가 같은 규칙으로 자리를 고른다(WP2, Core `Nav/PlacementResolver`). 부품마다 따로 처리하지 않는다.

```csharp
public static bool TryPlace(INavWorld nav, Vector2 center, Vector2 wish, float bodyRadius, out Vector2 placed);
// wish 가 맵 안이고 clearance ≥ bodyRadius + 4 면 그대로. 아니면 center 쪽으로 20px 씩 당기며 다시 보고,
// center 까지 거리의 절반까지 당겨도 없으면 false(그 개체는 생략. 생략 수는 결정적)
public static Vector2 Throw(INavWorld nav, Vector2 from, Vector2 dir, float dist, float bodyRadius);
// 분열체 튕김: HasClearLine 으로 닿는 곳까지만(기획 8.3 "닿으면 거기서 멈춤")
```

- 포위 고리(기획 10.3 "반경 20px 씩 줄여 R/2 까지")가 이 규칙의 원형이다. 보스가 기둥 옆·맵 모서리에서 소환해도 밀어내기(원 중심 문제·오목 다각형)에 기대지 않는다.

---

## 5. 전투

### 5.1 피해 흐름

모든 피해는 **요청 → 한 곳에서 적용** 으로 흐른다. 근접 타격 프레임, 투사체 맞음, 폭발, 장판 틱, 레벨업 밀어내기가 모두 `DamageRequest` 를 큐에 넣고 `DamageResolve` 단계가 처리한다.
다른 길은 없다(`World.Damage.ToHero(...)` 같은 도우미도 요청을 큐에 넣기만 한다).

```csharp
public readonly record struct DamageRequest(                        // 계약 (WP1)
    EntityId Target,          // 적 또는 주인공(EntityId.Hero). E: 아군
    EntityId Attacker,        // 때린 적(적중 수정·흡혈용). 주인공 무기·환경이면 None
    Faction From,             // Hero / Enemy / Ally / Neutral — 같은 진영 대상은 버림
    float Amount,             // 주인공 쪽: 무기 원값(레벨 표 값, damage_pct 없음). 적 쪽: 그 적의 '최종 피해'(5.2, 이미 정수)
    float DamagePct,          // 주인공 쪽: 발사 순간 복사한 damage_pct. 적 쪽 0
    float CritChance,         // 주인공 쪽: 발사 순간 복사. 적 쪽 0
    sbyte WeaponSlot,         // 주인공 무기 칸(결과 화면 합계·적별 타격 간격). 없으면 −1
    Vector2 KnockbackDir, float KnockbackDist,
    DamageFlags Flags);       // IgnoreInvuln, FromHazard, NoDamage(밀어내기만) …
```

- 적용 단계가 하는 일: 대상이 없거나 `Dead` 면 버림 → 치명 판정(`Weapon` 난수, `CritChance`) → `DamageMath` 로 최종 정수 피해(방어 빼기·최소 1) → HP 감소 →
  번쩍임 타이머 → 넉백(거리 × (1 − 넉백 저항) 또는 × `knockback_taken_pct`, 넉백 면역이면 0) → `HitLanded(Attacker, Target, Applied)` 틱 이벤트 →
  피해 숫자 표시 이벤트(같은 대상 0.1s 합치기는 게임 쪽) → HP ≤ 0 이면 `Dead`, `KillCause.Damage`(죽음 처리는 13·14 단계).
- 주인공은 피격 무적 0.25s 동안 무시, `--god` 이면 피해만 0(넉백은 받음). `World.Outcome` 이 `Running` 이 아니면 주인공 피해를 모두 무시한다(3.5 승리 처리).
- 무기별 준 피해 합계(결과 화면)는 `RunStats.DamageByWeapon[칸]` 에 이 단계에서 더한다.
- **적중 수정 부품**(`heavy_strike`, E `lifesteal`·접두사 `vampiric`): 넉백처럼 정의 값으로 정해지는 것은 요청을 만들 때 채운다(`IHitModifierPart.ModifyOutgoing`, 6.6).
  실제로 들어간 피해가 필요한 것(흡혈)은 다음 틱 `EnemyThink` 가 `HitLanded` 를 읽어 처리한다(1틱 늦음 허용).
- **적대 대상**(기획 E 아군 대비): 적 부품·적 투사체는 대상을 "주인공" 으로 박아 두지 않고 `IHostileQuery`(WP1 계약, WP3 구현)에 묻는다:
  `TryNearestHostile(Faction self, Vector2 from, float range, out EntityId target, out Vector2 pos)`. MVP 구현은 주인공 하나만 돌려준다.
  아군을 넣을 때 바뀌는 것은 12절 R14 에 적었다.

### 5.2 식은 한 곳에

`Combat/DamageMath.cs` 의 순수 함수로 기획 4.2 를 그대로 옮긴다. 테스트가 이 함수들을 직접 부른다.
**`damage_pct` 는 `HeroToEnemy` 에서만 곱한다.** `WeaponStats` 의 피해 값은 레벨 표 원값이다(5.3) — 두 곳에서 곱하는 길을 막는다.

```csharp
public static class DamageMath
{
    public static int HeroToEnemy(float weaponDamage, float damagePct, bool crit, int enemyArmor);       // round(d×(1+pct))×(crit?2:1) − armor, ≥1
    public static int EnemyFinalDamage(float baseDamage, float damageMul, float buffPctSum, float enrage); // 반올림 마지막 한 번
    public static int EnemyFinalHp(float baseHp, float timeMul, float rankMul, float hpPctSum);
    public static int ToHero(int enemyDamage, int heroArmor);                                             // ≥1
    public static float FinalCooldown(float cooldown, float cooldownPct, float surgePct);                 // 주인공 무기: 상한 0.6 / 마력 폭주 중 0.8
    public static float EnemyCooldown(float cooldown, float attackCooldownPctSum);                         // 적 능력·패턴: × (1 + 합), 최소 × 0.3
    public static float TimeHpMul(StageScaling s, float minutes);  public static float TimeDmgMul(StageScaling s, float minutes);
}
```

- 적의 **최종 HP 와 기본 피해 배율**(`DamageMul` = 시간 × 등급 × (1 + 접두사 `damage_pct`))은 생성 순간 `EnemyFactory` 가 계산해 인스턴스에 굳힌다. 버프%·광폭화는 피해를 낼 때마다 더한다.
  그 적이 만든 투사체·장판·폭발은 생성 순간 그 적의 `DamageMul`·버프% 로 계산한 최종 피해를 가진다(기획 2.3 "그 적의 피해 배율을 따른다").
- **발사 시점 고정**: 주인공 투사체·장판은 생성 순간의 `damage_pct`·치명 확률을 복사해 가진다. 날아가는 동안 레벨업으로 패시브가 바뀌어도 그 투사체는 옛 값이다.
- 버프는 데이터 종류 `buffs` 와 적마다 고정 슬롯으로 둔다(6.7).

### 5.3 무기 발동 (WP3 소유)

- 주인공은 무기 칸 4개(`WeaponSlot`: 정의, 레벨, 쿨다운 남은 시간, 패턴 상태, 진화 여부, `AcquiredTick`, `MaxedAtTick`). 매 틱 `Weapons` 단계가 칸마다 쿨다운을 줄이고 0 이 되면 `IFirePattern.Fire` 를 부른다.
  `AcquiredTick` 은 결과 화면 초당 피해의 분모, `MaxedAtTick` 은 "먼저 만렙이 된 무기를 진화" 규칙에 쓴다.
- **수치 해석**: `WeaponStats.Resolve(def, level, hero.Stats)` 가 레벨 표 + `base` + 주인공 능력치를 한 번에 합친 **읽기 전용 구조체**를 만든다(레벨·능력치가 바뀔 때만 다시 계산).
  기획 13.2 표(패턴마다 `projectile_count` 가 더해지는 키, `area_pct`·`duration_pct` 가 곱해지는 키, `cooldown_pct`)는 **패턴이 키 목록으로 선언**하고 `Resolve` 가 그 선언대로 적용한다
  → 표가 코드 한 곳에만 있다. **`damage_pct` 는 `Resolve` 가 적용하지 않는다**(5.2). `Weapons` 시험에 "`Resolve` 의 damage 는 `damage_pct` 와 무관" 을 넣는다.
- 레벨 표 값이 `null` 이면(6.8): `duration` = 상시(무한), `cooldown` = 발동 없음. `orbit` 은 `duration` 이 무한이면 구체를 끄지 않고 다시 쏘지 않는다. `duration_pct` 는 무한에 곱하지 않는다(기획 5.5).

```csharp
public interface IFirePattern
{
    string Id { get; }                                       // "homing", "straight_explode", ...
    IReadOnlyList<string> LevelKeys { get; }                 // 검증 13.5-2: 레벨 표 열 이름 = 이것뿐
    IReadOnlyList<string> NullableKeys { get; }              // null(상시/없음)을 허용하는 열 (orbit: duration, cooldown)
    string? CountKey { get; }                                // projectile_count 가 더해지는 키
    IReadOnlyList<string> AreaKeys { get; }                  // area_pct 가 곱해지는 키
    IReadOnlyList<string> DurationKeys { get; }
    void Fire(World world, ref WeaponSlot slot, in WeaponStats stats);   // 쿨다운이 끝났을 때
    void Tick(World world, ref WeaponSlot slot, in WeaponStats stats) { } // orbit 처럼 계속 관리하는 패턴
}

public interface ITargeting   // nearest / random_in_radius / facing / densest / none
{
    bool TryAim(World world, Vector2 origin, float radius, int shotIndex, Span<EntityId> taken, out Vector2 dir, out EntityId target);
}
```

- 여러 발: `count` 발을 한 번에 또는 `base.burst_interval`(매직 미사일 0.08s) 간격으로. 간격 발사는 칸 상태의 "남은 발 수·다음 발 시각" 으로 처리한다.
- 조준 `densest`: `ISpatialQuery.CellCount` 의 32px 칸 개수를 2×2 로 합쳐 64px 칸 기준 최댓값을 찾는다(해시를 다시 훑지 않는다).
- 무기 발동 시 주인공이 서 있으면 `HeroCasting` 재생(재생 중이면 그대로), 표시 이벤트 없음 — 애니메이션은 주인공 상태라 렌더가 그냥 읽는다.
- 진화 무기의 추가 효과(`on_hit`/`on_explode`/`on_crit`)는 `PartKind.WeaponEffect` 부품 목록이다(6.8). 투사체·장판이 그 순간에 목록 순서대로 부른다.

### 5.4 투사체

- `Projectile`(풀): 정의, 진영(`Faction`), 위치·이전 위치, 속도 벡터, 반경, 남은 수명/거리, 관통 남은 수, 피해(주인공 쪽: 원값·`DamagePct`·치명 확률 / 적 쪽: 최종 피해), 넉백,
  출처(주인공 무기 칸 / 쏜 적 ID), 대상(유도·튕김), **맞은 적 목록**.
- **궤적**은 전략이지만 뜨거운 반복이라 가상 호출 대신 `MotionKind` 열거형 + `switch` 로 둔다: `Straight`, `Homing`(회전율 제한), `Orbit`(주인공 기준 각도), `Ricochet`(맞으면 다음 대상).
  새 궤적 = 열거형 값 하나 + 함수 하나. 투사체 정의의 `turn_rate`·패턴이 이것을 고른다.
- **맞은 적 목록**: 고정 크기 16칸 인라인 배열(`InlineArray`)의 `EntityId`. 관통·튕김이 같은 적을 두 번 때리지 않게 한다(영혼 사냥꾼 최대 12 포함). 넘치면 가장 오래된 것부터 덮는다.
- **맞음 판정**: `ISpatialQuery.QueryCapsule(이동 전, 이동 후, 투사체 반경 + 최대 적 반경)` → 선분-원 최소 거리 ≤ 투사체 반경 + 적 반경. 빠른 투사체(520px/s = 틱당 8.7px)도 건너뛰지 않는다.
  같은 틱에 여러 적이 걸리면 선분 시작에서 가까운 순으로 관통 수만큼.
- **장애물**: `blocked_by_obstacles` 이면 같은 선분으로 `ObstacleIndex` 검사 → 닿으면 폭발/사라짐.
- **적 투사체**: 적대 진영 대상(MVP 는 주인공 하나)과 원-선분 검사. 렌더는 진영을 보고 적 투사체에 붉은 테 빛을 더한다(기획 5.1, 7.5).
- **적별 타격 간격**(회전 구체·장판): 적 인스턴스에 `float NextHitBySlot[4]`(주인공 무기 칸별 다음 허용 시각, WP3 조각). 칸 번호로 바로 찾으므로 사전이 없다.
- 안전 상한(400/200)을 넘으면 가장 오래된 것부터 지운다(저장소가 생성 순서를 기억).

### 5.5 장판·예고·지연 폭발

- `Hazard`(풀): 이펙트 ID, 중심, 반경, 남은 시간, 틱 간격·다음 틱, 피해, 대상 진영, 붙이는 버프(지옥불 둔화 = `buffs` 의 `slow_hellfire`), 출처.
  장판 틱마다 해시 질의(적 대상) 또는 적대 대상 질의(주인공 쪽).
- `Telegraph`(풀): 예고 이펙트 ID, 모양(원/선), 남은 시간, 끝나면 실행할 것(폭발 매개변수). 보스 `telegraphed_blast`, 불타는 접두사 죽음 폭발, `charge`·`dash`·패턴 예고가 쓴다.
  예고가 시뮬레이션 개체인 이유: 피해 시점이 예고 시간에 묶여 있어서다. 그림은 렌더가 이 개체를 읽어 그린다.
- 동시 상한(독 웅덩이 20 등)은 `Hazard` 에 `CapGroup` 을 두고 그룹별로 오래된 것부터 지운다.
- 적 쪽 투사체·장판·예고는 WP4 가 `ICombatSpawner`(WP1 계약, WP3 구현)로 만든다. 저장소·진행·맞음 판정은 WP3 몫이다.

### 5.6 표시용 이펙트 수명

- 폭발 그림·맞음 불꽃·레벨업 빛 기둥·처치 연출처럼 **규칙에 영향이 없는 것**은 시뮬레이션에 두지 않는다. 시뮬레이션은 `PresentationBuffer` 에 `Effect(tick, fxId, pos, scale, tint)` 를 넣고,
  게임 쪽 `FxSystem` 이 풀에서 꺼내 재생·빛·흔들림을 처리한다(나이 = 현재 스테이지 틱 − `tick`, 3.4). 반복 이펙트(장판 불꽃)는 `Hazard` 가 살아 있는 동안 렌더가 직접 그린다.
- 적의 **죽음 애니메이션과 시체**도 표시 쪽이다: 시뮬레이션은 죽은 적을 바로 지우고 표시 이벤트 `CorpseSpawned(tick, 그림 묶음, 위치, 방향, 색조, 크기, 알파, 발광, 죽음 방식)` 을 낸다.
  `Culled`(상한 정리)는 시체를 내지 않는다. 렌더의 `CorpseLayer` 가 `…Die` 재생 → 2초 → 0.5초 투명, 상한 150. → 시뮬레이션 개체 수가 줄고, 시체 상한이 규칙과 섞이지 않는다.
- 이름: 틱 이벤트(시뮬레이션 안) = `EnemyKilled(Id, DefIndex, Pos, Rank, Cause, Attacker)`, 표시 이벤트(화면) = `CorpseSpawned`. 한 사건에 이름 하나씩이다.
- 주인공 죽음(`HeroDie` 23프레임)은 결과 화면 시점이 걸려 있어 시뮬레이션 상태로 둔다.

---

## 6. 데이터 스키마

### 6.1 형식과 파일 배치 (기획 열린 질문 4 의 답: JSON)

- `src/DiabloDefenceRemastered/Data/` 아래 `**/*.json` 을 모두 읽는다(경로 서수 정렬, 3.4). 파일 하나는 **종류 이름을 키로 하는 객체**이고, 한 파일에 여러 종류를 담을 수 있다.

```json
{ "enemies": [ … ], "projectiles": [ … ], "effects": [ … ] }
```

  종류 키: `rules`(객체, 아래), `sprites`, `sprite_sets`, `effects`, `projectiles`, `maps`, `obstacle_kinds`, `enemies`, `affixes`, `buffs`,
  `weapons`, `passives`, `evolutions`, `pickups`, `drop_tables`, `stages`.
- `rules` 는 **여러 파일에서 하위 키 단위로 합친다**: `rules/hero.json` = `{ "rules": { "hero": …, "xp_curve": …, "level_up": … } }`,
  `rules/enemies.json` = `{ "rules": { "ranks": …, "caps": …, "spawn": … } }`. 같은 하위 키가 두 파일에 있으면 오류. 그래서 두 묶음이 같은 파일을 고치지 않는다.
- 권장 배치(소유 묶음은 11절):

```
Data/
  rules/hero.json  rules/enemies.json        주인공 능력치·경험치 곡선·레벨업 규칙 / 등급 배율·상한·생성
  sprites/units.json fx.json ui.json         sprites + sprite_sets
  effects/common.json           fx_warning_*, fx_levelup_pillar …
  maps/dungeon1.json            map
  obstacle_kinds.json
  weapons/<무기>.json           무기 1개 + 그 투사체·이펙트 (+ 진화 무기)
  passives.json  evolutions.json  pickups.json  drop_tables.json
  enemies/<적>.json             적 1개 + 그 적만 쓰는 투사체·이펙트
  enemies/bosses/<보스>.json
  affixes.json  buffs.json
  stages/<스테이지>.json
```

- 이름 규칙: 키는 `snake_case`(기획서 ID 그대로). 색은 `[r,g,b]`, 점은 `[x,y]`, 사각형은 `[x0,y0,x1,y1]`(최소·최대, 기획 "x 1325~1450, y 418~478" 과 같은 순서),
  원은 `[cx,cy,r]`, 시각은 `"mm:ss"`, 프레임 번호는 **1부터**(기획서 "7번째 프레임" 과 같게, 코드가 0부터로 바꾼다).
- 같은 뜻은 어디서나 같은 이름: 예고는 `"telegraph": { "effect": …, "time": … }`, 거리 범위는 `"range_band": [최소, 최대]`, 한 값 사거리는 `"range"`.
- System.Text.Json: `JsonNamingPolicy.SnakeCaseLower`, 주석·끝 쉼표 허용, **모르는 속성은 오류**(`JsonUnmappedMemberHandling.Disallow` — 오타를 잡는다).

### 6.2 로딩 순서

```
1. 파일 목록(서수 정렬)·JSON 파싱(JsonNode)   문법 오류 → DataError, 그 파일만 빠짐
2. 종류별로 모으기, ID 중복 검사, rules 하위 키 합치기
3. extends 병합 (JsonNode 단계)      부모 먼저, 순환 검사. 객체는 깊은 병합, 배열·값은 통째 교체.
                                     이어서 *_add(배열 뒤에 붙이기), *_patch(원소의 id 로 찾아 깊은 병합). 메타 키(extends, abstract, *_add, *_patch) 지우기
4. 형 변환 — 정의 항목마다 따로       JsonNode.Deserialize<T>. 부품·이벤트 원소는 PartRegistry 가 (종류, "type") 으로 매개변수 형을 골라
                                     "type"·"id"·"trigger" 를 뺀 나머지를 역직렬화. JsonException.Path → DataError 의 JSON 경로
5. 연결 (DefRef<T> 채우기, 한 번만)  없는 ID → "비슷한 ID" 제안(편집 거리 ≤ 2)과 함께 오류. 객체 가중치 → JSON 순서 배열
6. 의미 검사                          13.5 규칙 1~3, 7(정의끼리), 8, 9 + 부품 자체 검사 + 6.14 추가 규칙
7. 등록된 IDataValidator 들           기하 검사 13.5-4~6(WP2), 실제 PNG 크기 13.5-7(WP5, 그림이 있을 때만) — 로더는 구체 검사기를 모른다
→ 결과: GameDefs + IReadOnlyList<DataError>
```

- 항목 단위로 역직렬화하는 이유: STJ 는 한 객체의 첫 오류에서 멈추므로, 항목마다 따로 해야 "모두 모은다" 가 된다.
- 변환기(WP7): `Vector2`(`[x,y]`), `Rgb`, 사각형 `RectI`, 도형(`polygon`/`rect`/`circle`/`circle_at`+`r`), 시각 `"mm:ss"`, 레벨 표 값(`float?`), `DefRef<T>`(문자열 ↔ 참조, `JsonConverterFactory`).
- 오류가 있어도 끝까지 돌아 **모두 모은다**. `DataError(File, JsonPath, Message, Severity)` — 예:
  `enemies/skeleton_mage.json $.enemies[0].behaviors[1].projectile: 'pj_mage_boltt' 가 없다 (혹시 'pj_mage_bolt'?)`
- 게임은 오류(Severity=Error)가 하나라도 있으면 타이틀 대신 **DataError 화면**(목록, 파일별 묶음, 스크롤)을 띄우고 로그에 전부 적는다.
  개발 옵션 실행(`--autoplay` 등)이면 화면 없이 로그에 적고 종료 코드 2 로 끝난다. `--validate-data` 는 창 없이 검사만 하고 종료 코드(0 = 통과, 2 = 오류)를 낸다. 경고(Warning)는 로그에만.
- 부품 `(종류, type)` 이 등록되지 않았으면 오류다. 게임과 실제 데이터 시험은 늘 엄격하다. `DataLoadOptions.AllowUnknownParts` 는 **로더 단위 시험 전용**이고 게임 코드에서 켜지 않는다.

```csharp
public sealed class DefRef<T> where T : class, IDef      // 계약 (WP1)
{
    public string Id { get; }
    public T Def { get; }          // 연결 단계(5)가 한 번만 채운다. 두 번째 채우기·채우기 전 읽기는 예외
}
public interface IDef { string Id { get; } }

public enum PartKind { EnemyBehavior, EnemyAction, FirePattern, Targeting, StageEvent, WeaponEffect }
public sealed class PartRegistry                         // 계약 (WP1). 키 = (PartKind, type)
{
    public void Register<TParams>(PartKind kind, string type, Func<TParams, object> create,
                                  Action<TParams, ValidationContext>? validate = null);
}
public interface IDataValidator { string Name { get; } void Validate(GameDefs defs, ValidationContext ctx); }   // 등록식, 등록 순서대로 부름
```

- 정의 record 는 `init` 속성이고 5단계 뒤 바뀌지 않는다. `DefRef<T>` 만 클래스라 연결 단계가 채울 수 있다(정적 사전 없음 — 월드 여러 개·시험과 부딪히지 않는다).
- 정의 안의 부품 원소는 `PartDef(Kind, Type, Id?, Trigger?, Params, Instance)` 로 담긴다. 로더는 `Instance` 를 `object` 로만 알고, 쓰는 묶음이 자기 인터페이스로 바꿔 쓴다.
- 같은 `type` 이 종류마다 따로 있을 수 있다: `escort`(적 부품 / 스테이지 이벤트), `summon`(적 동작 / E 무기 패턴), `ground_pool`(발사 패턴 / 진화 효과). 키가 `(종류, type)` 이라 충돌하지 않는다.

### 6.3 그림 `sprites`, 그림 묶음 `sprite_sets`

```json
{
  "sprites": [
    { "id": "Skeleton",       "file": "Skeleton.png",       "sheet": [1152, 768],  "frame": [96, 96],
      "directions": 12, "frames": 8,  "color_key": [0, 0, 0], "origin": [36, 84] },
    { "id": "SkeletonAttack", "file": "SkeletonAttack.png", "sheet": [1152, 1152], "frame": [96, 96],
      "directions": 12, "frames": 12, "color_key": [0, 0, 0], "origin": [36, 84] },
    { "id": "FireBall",       "file": "FireBall.png",       "sheet": [1540, 1344], "frame": [96, 96],
      "directions": 16, "frames": 14, "color_key": [0, 0, 0], "origin": [48, 48], "slack": [4, 0] },
    { "id": "LOGO",           "file": "LOGO.png",           "sheet": [550, 3240],  "frame": [550, 216],
      "directions": 1,  "frames": 15, "color_key": [0, 255, 0], "origin": [275, 108] }
  ],
  "sprite_sets": [
    { "id": "set_skeleton", "base_speed": 67, "base_radius": 12, "frame_time": 0.06,
      "clips": {
        "idle":   { "sprite": "Skeleton" },
        "walk":   { "sprite": "SkeletonWalk" },
        "attack": { "sprite": "SkeletonAttack", "event_frame": 7 },
        "die":    { "sprite": "SkeletonDie" }
      } }
  ]
}
```

- `sheet` = 정의끼리 검사용 크기(13.5-7). `slack` = 남는 픽셀 허용(가로, 세로). 열 = 방향, 행 = 프레임(brief 4절).
- `origin` = 프레임 안 발 픽셀. 위 값은 **추정**(첫 프레임 그림 범위: 해골 x 1~59, y 30~93 — 그림이 프레임 가운데에 있지 않다). `--preview sprites` 로 맞춘다(기획 열린 질문 7).
  방향마다 발 위치가 다르면 `origin_by_dir: [[x,y] × 12]` 로 덮어쓸 수 있게 자리를 둔다.
- `sprite_sets` 는 적·주인공이 쓰는 클립 묶음. `event_frame` = 타격·발사 프레임 기본값(적 정의가 덮어씀). `base_speed` = 걷기 재생 속도 기준(재생 배율 = 실제 속도 / base_speed).
  `base_radius` = 크기 배율 1 일 때 몸 반경(해골 12, 좀비 14, 염소인간 12, 검사 14, 해골 왕 30) — 적 정의가 `radius` 를 생략하면 `round(base_radius × scale)`(6.5).
- 클립 진행(프레임 시간·재생 배율·이벤트 프레임을 이번 틱에 넘었나)은 Core `AnimClock` 한 곳이 계산한다. 주인공·적·미리보기·시체가 같은 함수를 써서 타격 프레임과 그림이 어긋나지 않는다.

### 6.4 이펙트 `effects`, 투사체 `projectiles`

```json
{
  "effects": [
    { "id": "fx_fire_explosion", "sprite": "FireBallExplosion", "frames": [1, 16], "loop": false, "frame_time": 0.06,
      "tint": [255, 255, 255], "fit_radius": 40, "alpha": 1.0, "blend": "additive",
      "light": { "color": [255, 160, 80], "radius_mul": 1.5, "duration": 0.3 },
      "shake": { "px": 1, "duration": 0.08 } },
    { "id": "fx_poison_pool", "sprite": "FireBallExplosion", "frames": [5, 12], "loop": true, "frame_time": 0.06,
      "tint": [110, 220, 90], "fit_radius": 40, "alpha": 0.55, "blend": "additive" },
    { "id": "fx_warning_circle", "procedural": "warning_circle", "tint": [255, 40, 40], "alpha": 0.35 }
  ],
  "projectiles": [
    { "id": "pj_fireball", "sprite": "FireBall", "tint": [255, 255, 255], "scale": 0.6, "blend": "alpha",
      "radius": 8, "speed": 380, "turn_rate": 0, "range": 450, "blocked_by_obstacles": true, "pierce": 1,
      "explosion": { "damage_mul": 1.0, "effect": "fx_fire_explosion" } },
    { "id": "pj_magic_missile", "sprite": "MagicMissile", "scale": 0.5, "blend": "additive",
      "radius": 6, "speed": 300, "turn_rate": 360, "lifetime": 3.0, "blocked_by_obstacles": true,
      "hit_effect": { "effect": "fx_mm_hit" } }
  ]
}
```

- 이펙트 크기: `scale`(고정 배율) 또는 `fit_radius`(배율 1 일 때 덮는 반경 px → 실제 배율 = 원하는 반경 / fit_radius). 둘 중 하나.
- `procedural`: 코드로 그리는 예고(원·선). 같은 ID 체계라 패턴이 똑같이 참조한다.
- 폭발 반경: 쏘는 쪽(무기 레벨 표 `radius`, 보스 패턴 매개변수)이 주면 그 값, 아니면 정의의 `explosion.radius`. 둘 다 없으면 검증 오류.
- 참조하는 쪽은 `{ "effect": "fx_mm_hit", "tint": [255,70,70], "scale": 0.25 }` 처럼 **색조·크기만** 덮어쓸 수 있다(기획 13.7). 다른 키는 오류.

### 6.5 몬스터 `enemies`

**일반 적의 틀과 변형**

```json
{
  "enemies": [
    { "id": "skeleton", "name": "해골 병사", "tags": ["skeleton", "undead"],
      "sprites": "set_skeleton", "tint": [255, 255, 255], "scale": 1.0, "alpha": 1.0,
      "hp": 10, "speed": 70, "damage": 5, "armor": 0, "knockback_resist": 0, "xp": 1,
      "movement": "flow",
      "behaviors": [ { "id": "melee", "type": "chase_melee", "cooldown": 1.0 } ],
      "death": "anim", "drop_table": "dt_common" },

    { "id": "burning_dead", "extends": "skeleton", "name": "불타는 해골", "tags": ["skeleton", "undead", "fire"],
      "tint": [255, 150, 80], "glow": { "color": [255, 120, 40], "alpha": 0.35 }, "scale": 0.95,
      "hp": 8, "speed": 100, "damage": 15, "xp": 1,
      "behaviors": [
        { "type": "self_destruct", "trigger_range": 10, "fuse": 0.6, "radius": 60,
          "effect": { "effect": "fx_fire_explosion", "scale": 0.8 }, "fizzle_effect": "fx_spark" } ] },

    { "id": "bone_brute", "extends": "skeleton", "name": "뼈 거인", "tint": [230, 220, 200], "scale": 1.7,
      "hp": 110, "speed": 45, "damage": 18, "armor": 2, "knockback_resist": 0.7, "xp": 8,
      "behaviors": [],
      "patterns": [ { "id": "slam", "range_band": [0, 70], "cooldown": 2.4, "clip": "attack", "event_frame": 7,
                      "telegraph": { "effect": "fx_warning_circle", "time": 0.5 },
                      "actions": [ { "type": "melee_slam", "radius": 60, "knockback": 50, "shake": { "px": 2, "duration": 0.15 } } ] } ] }
  ]
}
```

- **몸 반경**: 그림 묶음에 `base_radius`(해골 12, 좀비 14, 염소인간 12, 검사 14, 해골 왕 30)를 두고, 적 정의의 `radius` 를 생략하면 `round(base_radius × scale)` 로 계산한다(기획 8.1).
  위 `bone_brute` 는 `round(12 × 1.7) = 20`. 값을 직접 적을 수도 있고(중간 보스·보스처럼 기획 표 값을 그대로 쓸 때), 계산값과 2 넘게 다르면 경고다.
  → "`extends` + `scale` 만 바꾸기" 변형이 부모 반경을 물려받아 충돌·필드 등급이 조용히 틀어지는 일이 없다. 정예·우두머리 크기 배율은 생성 때 여기에 다시 곱한다.
- `melee_slam` 의 피해를 생략하면 그 적의 `damage` 를 쓴다(일반 적 패턴은 시간 배율을 받는 적 피해, 보스 패턴은 표 값).
- `behaviors` 는 `extends` 에서 **통째로 교체**된다(배열 규칙). 부모 부품에 하나만 더하려면 `"behaviors_add": [ … ]`, 부모 부품의 매개변수 몇 개만 바꾸려면
  `"behaviors_patch": { "<부품 id>": { "cooldown": 0.7 } }`(부품의 선택 `id` 로 찾아 깊은 병합, 없는 id 는 오류). 패턴·페이즈도 `patterns_add`/`patterns_patch`/`phases_patch` 로 같다.
- `movement`: `flow` / `astar` / `direct`. 위치 잡기 부품(`kite`, `phase_through`)이나 이동 지시(6.6)가 없으면 기본 "주인공 쪽으로"(`SeekHero`).
- `rank`: 정의 자체의 등급 `normal`(기본) / `miniboss` / `boss`. 정예·우두머리는 정의가 아니라 생성 때 붙는 등급이다(6.7).
  `miniboss`·`boss` 는 상한 정리에서 지우지 않고, 보스 막대·처치 연출·드롭 규칙을 고른다.
- `elite_eligible`: 기본 `true`. 확률 정예 무리의 종류 뽑기에서 뺄 적은 `false`(`skeleton_captain`, `corpse_spawn`, 기획 9.1). `miniboss`·`boss` 는 늘 빠진다.
- `spawn_group: [최소, 최대]`(선택): 일반 생성에서 이 종류가 뽑히면 무리 크기를 `rules.spawn.group`(1~3) 대신 이 값에서 뽑는다(`hell_goat` 5~7). 감독은 종류를 먼저 뽑고 크기를 뽑는다.
- `death`: `anim`(…Die 재생) / `knight_fade`(Swordman 계열: 마지막 공격 프레임 + `fx_knight_death`).
- `anim_speed`(선택, 기본 1): 이 적의 모든 클립 재생 배율. 공격 쿨이 공격 클립 길이보다 짧으면 그 공격만 클립을 쿨에 맞춰 빠르게 재생한다(쿨은 **공격 시작부터** 센다, 기획 8.1).
- 보스·중간 보스용: `enrage: { "after": "03:00", "damage_mul": 2, "speed_mul": 1.3 }`(등장 시각부터 셈), `arrival: { "banner": "해골 왕이 깨어났다", "time": 2.5, "shake": { "px": 4, "duration": 0.5 } }`.
  스테이지마다 다르게 하려면 `boss`/`miniboss` 이벤트가 같은 이름의 매개변수로 덮어쓴다.

**한 파일에 적 + 투사체 (해골 마법사)**

```json
{
  "enemies": [
    { "id": "skeleton_mage", "extends": "skeleton", "name": "해골 마법사", "tags": ["skeleton", "undead", "ranged"],
      "tint": [120, 230, 220], "glow": { "color": [100, 220, 210], "alpha": 0.2 },
      "hp": 18, "speed": 60, "damage": 9, "xp": 3,
      "behaviors": [
        { "type": "kite", "range_band": [240, 340] },
        { "type": "shoot_projectile", "projectile": "pj_mage_bolt", "clip": "attack", "event_frame": 7,
          "cooldown": 3.0, "cooldown_jitter": 0.4, "range": 340 } ] }
  ],
  "projectiles": [
    { "id": "pj_mage_bolt", "sprite": "FireBall", "tint": [120, 230, 220], "scale": 0.45,
      "radius": 7, "speed": 220, "range": 400, "blocked_by_obstacles": true,
      "hit_effect": { "effect": "fx_fire_explosion", "tint": [120, 230, 220], "scale": 0.35 } }
  ]
}
```

**지휘 + 호위, 분열, 죽은 자리 장판, 돌진** (부품만 발췌)

```json
{ "type": "command_aura", "radius": 200, "target_tag": "skeleton", "buff": "command" }
{ "type": "escort", "default_count": 8, "escort_enemy": "skeleton", "formation_radius": 120, "break_range": 250, "fear_on_leader_death": 1.5 }
{ "type": "split_on_death", "spawn": "corpse_spawn", "count": 3, "throw": 80, "delay": 0.3 }
{ "type": "ground_hazard", "trigger": "on_death", "effect": "fx_poison_pool", "radius": 50, "duration": 3.0,
  "tick": 0.5, "damage": 4, "cap_group": "poison", "cap": 20 }
{ "type": "charge", "range_band": [0, 260], "telegraph": { "effect": "fx_warning_line", "time": 0.5 },
  "speed": 280, "time": 1.0, "stun": 0.8, "cooldown": 4.0, "damage": 10 }
```

- 공포는 `escort` 부품의 규칙이다: 대장이 죽으면 **그 대장을 `Leader` 로 가진 적만** `fear_on_leader_death` 초 동안 도망간다(기획 8.3). 오라 반경 안의 다른 해골이나, 호위 없이 나온 대장 주변의 적은 영향이 없다.
- `ground_hazard` 는 `EnemyAction` 으로 한 번만 등록된다. `behaviors` 안에서 `trigger` 가 붙으면 훅 어댑터가 감싼다(6.6) — 보스 패턴의 동작과 같은 코드다.

**보스 (패턴·페이즈)**

```json
{
  "enemies": [
    { "id": "skeleton_king", "name": "해골 왕", "tags": ["skeleton", "undead", "boss"],
      "sprites": "set_skeleton_king", "hp": 8000, "speed": 55, "damage": 30, "armor": 0,
      "knockback_resist": 1.0, "xp": 0, "movement": "astar",
      "immunities": ["knockback", "slow"], "show_boss_bar": true, "time_scaling": false, "rank": "boss",
      "enrage": { "after": "03:00", "damage_mul": 2, "speed_mul": 1.3 },
      "arrival": { "banner": "해골 왕이 깨어났다", "time": 2.5, "shake": { "px": 4, "duration": 0.5 } },
      "patterns": [
        { "id": "slam", "range_band": [0, 90], "cooldown": 2.0, "clip": "attack", "event_frame": 9,
          "actions": [ { "type": "melee_slam", "damage": 30, "knockback": 60, "shake": { "px": 3, "duration": 0.2 } } ] },
        { "id": "fire_fan", "cooldown": 5.0, "clip": "cast", "event_frame": 8,
          "actions": [ { "id": "spread", "type": "projectile_spread", "projectile": "pj_boss_fireball", "count": 3, "arc": 40,
                         "aim": "hero", "damage": 20 } ] },
        { "id": "raise", "cooldown": 15.0, "clip": "cast", "event_frame": 8,
          "actions": [ { "id": "summon", "type": "summon", "spawns": [ { "enemy": "skeleton", "count": 8 } ], "radius": 120, "alive_cap": 30 } ] }
      ],
      "phases": [
        { "id": "p2", "hp_below": 0.5, "invulnerable": 1.0, "shake": { "px": 5, "duration": 0.3 },
          "glow": { "color": [255, 60, 40], "alpha": 0.3 }, "speed": 70,
          "patch": {
            "fire_fan": { "cooldown": 3.5, "actions_patch": { "spread": { "count": 5, "arc": 80 } } },
            "raise":    { "cooldown": 10.0, "actions_patch": { "summon": { "spawns": [ { "enemy": "skeleton", "count": 6 }, { "enemy": "burning_dead", "count": 2 } ] } } } },
          "add": [], "remove": [] } ]
    }
  ]
}
```

타락한 기사 대장의 "전투 함성"(기획 9.4)은 한 패턴에 동작 둘이다.

```json
{ "id": "battle_cry", "cooldown": 20.0, "clip": "cast", "event_frame": 10,
  "actions": [ { "type": "summon", "spawns": [ { "enemy": "fallen_knight", "count": 4 } ], "radius": 120, "alive_cap": 30 },
               { "type": "buff_area", "radius": 300, "buff": "war_cry", "duration": 6.0 } ] }
```

- 패턴 `range_band` 는 [최소, 최대] 거리, `hp_once` 는 HP 문턱 1회. `telegraph` 는 선택(큰 피해 패턴은 검증이 필수로 본다 — 기획 12.6).
- `actions` 는 **같은 이벤트 프레임에 순서대로** 실행한다(시전 애니메이션 한 번, 소환과 버프가 같은 틱). 여러 틱 걸리는 동작(`dash`)은 목록의 마지막에만 올 수 있다(검증).
- `arc` = 전체 퍼짐각(±20° = 40). 360 이면 사방.
- 페이즈 `patch` 는 패턴 ID 별 깊은 병합(패턴 안 동작은 `actions_patch` 로 동작 `id` 별), `add`/`remove` 는 패턴 목록 조작. **로딩 때** 페이즈마다 완성된 패턴 목록을 미리 풀어 둔다(6.6).
  피의 왕은 `"extends": "skeleton_king"` + `patterns_add` + `phases_patch: { "p2": { "add": [ … ] } }` 로 적어 해골 왕 페이즈를 다시 적지 않는다.
- 일반 적도 `patterns` 를 가질 수 있다(`bone_brute`). 패턴과 `behaviors` 의 능력 부품은 같은 실행 경로를 탄다(6.6).

### 6.6 행동 부품 실행 모델 (WP4 소유, 이 절이 설계)

적 하나는 **이동 방식 1개 + 부품 여러 개 + 인스턴스 상태**다. 부품 객체는 정의당 하나(매개변수만 가진 불변 객체)이고, 적마다 다른 값은 슬롯·행동 상태·이동 지시에 둔다.

**부품 역할과 시그니처** (`Sim/Enemies/Parts/Contracts.cs`, WP4 소유. 레지스트리 종류는 `EnemyBehavior` / `EnemyAction`)

| 역할 | 인터페이스 | 예 |
|---|---|---|
| 위치 잡기 | `IPositioningPart` | `kite`, `phase_through` |
| 능력(독점 행동) | `IAbilityPart` | `chase_melee`, `shoot_projectile`, `self_destruct`, `charge`, `war_cry`, 모든 패턴(`PatternAbility`) |
| 지속 효과 | `IAuraPart` | `command_aura`, `PhaseController`(페이즈가 있으면 저절로 붙음) |
| 훅 | `ISpawnHookPart`, `IDeathHookPart`, `IHitModifierPart` | `escort`(데려오기·공포), `split_on_death`, `heavy_strike`, (E) `lifesteal` |
| 동작(패턴·훅 안쪽) | `IEnemyAction` | `melee_slam`, `projectile_spread`, `telegraphed_blast`, `dash`, `summon`, `ground_hazard`, `buff_area` |

```csharp
public interface IEnemyBehavior { }
public interface IAbilityPart : IEnemyBehavior
{
    bool TryBegin(in EnemyCtx ctx, ref PartSlot slot, ref ActionState action);     // 조건(거리·쿨·시야·HP 문턱) 확인 후 시작
    ActionStatus Tick(in EnemyCtx ctx, ref PartSlot slot, ref ActionState action);  // Running / Done
    void Interrupt(in EnemyCtx ctx, ref PartSlot slot, ref ActionState action, InterruptReason why) { }
}
public interface IPositioningPart : IEnemyBehavior
{
    bool TryPosition(in EnemyCtx ctx, ref PartSlot slot, out MoveIntent intent);  // false 면 다음 부품·기본 이동
}
public interface IAuraPart : IEnemyBehavior { void Tick(in EnemyCtx ctx, ref PartSlot slot); }
public interface ISpawnHookPart : IEnemyBehavior { void OnSpawn(in EnemyCtx ctx, in SpawnContext spawn); }
public interface IDeathHookPart : IEnemyBehavior { void OnDeath(in EnemyCtx ctx); }                    // KillCause.Damage 일 때만 불린다
public interface IHitModifierPart : IEnemyBehavior
{
    void ModifyOutgoing(in EnemyCtx ctx, ref DamageRequest request);                // 요청을 만들 때 (heavy_strike 넉백)
    void OnHitLanded(in EnemyCtx ctx, in HitLanded hit) { }                         // 다음 틱 (흡혈)
}
public interface IEnemyAction
{
    void Begin(in EnemyCtx ctx, ref ActionState action);                             // 이벤트 프레임(또는 훅 순간)에 한 번
    ActionStatus Tick(in EnemyCtx ctx, ref ActionState action) => ActionStatus.Done;  // 여러 틱 동작(dash)만 구현
}

[InlineArray(12)] public struct PartSlots { private PartSlot _e0; }
public struct PartSlot    { public float Cooldown, Timer; public int Counter; public bool Fired; public EntityId Ref; }
public struct ActionState { public sbyte PartIndex, ActionIndex; public byte Stage; public float T; public Vector2 Dir, Point; public EntityId Target; }
public readonly struct EnemyCtx(World world, Enemy self) { /* 거리·시야·난수(Ai)·적대 대상 질의·ICombatSpawner·피해 요청·버프 붙이기·PlacementResolver 도우미 */ }
```

- **훅 어댑터**: `behaviors` 원소에 `trigger` 가 있으면 그 `type` 을 `EnemyAction` 에서 찾아 `TriggeredAction` 으로 감싼다.
  `on_death` → `IDeathHookPart`, `on_spawn` → `ISpawnHookPart`, `cooldown` → 클립 없는 `IAbilityPart`. 접두사 `fire_enchanted` 의 `telegraphed_blast`, `plague_zombie` 의 `ground_hazard` 가 보스와 같은 동작 코드를 탄다.
- **`PatternAbility`**(데코레이터): 조건(`range_band`·`cooldown`·`hp_once`) → 클립 재생 → 예고가 있으면 `Telegraph` 를 만들고 예고 시간 동안 클립을 이벤트 프레임 앞에서 멈춤 →
  이벤트 프레임에서 `actions` 를 순서대로 `Begin` → 마지막 동작이 `Running` 이면 `Done` 까지 `Tick`.
- **슬롯 수**: 부품 슬롯은 적마다 `PartSlots`(인라인 12칸) — 정의 부품 + 모든 페이즈 패턴의 합집합 + 접두사 부품. 12 를 넘는 정의·조합은 로딩 오류(접두사 조합은 최대 2개 기준으로 검사). 풀에서 꺼낸 적에 배열을 새로 만들지 않는다.
- **페이즈**: 로딩 때 페이즈마다 "그 페이즈에 쓰는 패턴 목록(합집합 안 슬롯 번호 배열)" 과, 덮어쓴 매개변수를 가진 패턴 객체를 미리 만든다. 전환은 "지금 쓰는 목록 번호" 하나만 바꾼다
  → 쌓인 쿨다운이 엉뚱한 패턴에 붙지 않고, 실행 중 깊은 병합·할당이 없다. 전환 연출(무적·흔들림·발광·속도)은 `PhaseController` 가 한다.

**인스턴스 상태 — 생성 맥락·이동 지시·관계** (기획 8.3 행진·호위·공포, 9.5 소환 상한, 2.3 상한 정리 순서)

```csharp
public enum SpawnSource : byte { Interval, ElitePack, Ring, March, Escort, EscortMember, Miniboss, Boss, EventOther, Split, Summon, Stress }
public readonly record struct SpawnContext(SpawnSource Source, EntityId Owner, int EscortCount = -1);   // 계약 (WP1). -1 = 정의 기본값

public enum OrderKind : byte { None, March, Follow, Flee }
public struct MoveOrder { public OrderKind Kind; public Vector2 Dir; public EntityId Leader; public Vector2 Offset; public int UntilTick; }
```

- `Enemy` 에 `Source`(생성 출처), `Leader`(호위가 따르는 대장), `Summoner`(소환자), `Order`(이동 지시)를 둔다(WP4 조각, 11.2).
- **생성 훅은 출처를 본다**: `escort` 의 `OnSpawn` 은 `Source == Escort`(이벤트) 일 때만 호위를 데려온다. 수 = `EscortCount ≥ 0` 이면 그 값(이벤트 `escorts`, 0 포함), 아니면 `default_count`.
  구간 생성·정예 판정·`elite_pack` 으로 나온 대장은 호위가 없다(기획 8.3). 호위 하나하나는 `Source = EscortMember`, `Leader = 대장`, `Order = Follow(대장, 대형 자리)`, 자리는 `PlacementResolver`.
- **이동 지시**: `march` 이벤트는 `Order = March(방향, 지금 + 직진 시간)`. `Follow` 는 주인공이 그 호위의 `break_range` 안에 들거나 대장이 죽으면 풀린다. 공포는 `Order = Flee(UntilTick)`.
  지시는 위치 잡기만 대신한다 — 행진 중에도 사거리 안이면 공격한다(공포만 능력을 막는다).
- **소환 상한**: `summon` 은 `Summoner == 자신` 인 살아 있는 적 수를 세서(저장소를 한 번 훑음, 소환은 드물다) `alive_cap` 까지만 부른다.
- **상한 정리 순서**: 감독의 `SpawnGate.Reserve(source, n)` 가 출처를 셋으로 나눈다 — 일반(`Interval`) → 이벤트(`ElitePack`·`Ring`·`March`·`Escort`·`EscortMember`·`Miniboss`·`Boss`·`EventOther`) → 분열·소환(`Split`·`Summon`).
  일반은 300 에서 막힘, 이벤트는 화면 밖 먼 일반 적을 `Culled` 로 지워 자리를 냄, 분열·소환은 360 까지 같은 방법으로(기획 2.3). `Stress` 는 상한 검사를 건너뛴다(개발 옵션).

**매 틱 `EnemyThink` 순서** (적 하나)

1. 생애 상태: `Entering`(진입점까지 `Straight` 의도), `Emerging`(0.4s `Hold`)면 그 처리만. `Stunned` 면 `Hold`.
2. 지난 틱 `HitLanded` 중 자기가 때린 것 → `IHitModifierPart.OnHitLanded`.
3. 버프 만료 정리, 지속 효과 부품 `Tick`(오라는 대상에게 2틱짜리 버프를 다시 붙인다 — 범위를 벗어나면 저절로 사라짐).
4. 이동 지시가 `Flee` 면 `Away(주인공)` 의도만 내고 8번으로(능력 안 씀).
5. 독점 행동이 진행 중이면 그 부품의 `Tick` 만(끝나면 `Active` 로).
6. 아니면 능력 부품을 **슬롯 순서대로** `TryBegin` — 처음 시작한 것이 독점(기획 13.3 "표의 위쪽이 먼저"). 쿨다운은 `DamageMath.EnemyCooldown` 으로 `attack_cooldown_pct` 를 반영한다.
7. 독점 행동이 없으면 이동 의도: 이동 지시(`March` → `Heading`, `Follow` → `SeekPoint(대장 + 자리)`)가 있으면 그것, 없으면 위치 잡기 부품, 없으면 `SeekHero`.
8. 이동 속도(`MoveSpeed` = 정의 × 등급 × (1 + 접두사·버프 `speed_pct` 합) × 광폭화)와 표시 상태(`Visual`, 아래)를 쓴다. 애니메이션 시계(`AnimClock`) 진행.

- 죽음(`DeathHooks`): `Cause == Damage` 면 진행 중 행동에 `Interrupt(Death)` → `self_destruct` 는 여기서 폭발하지 않고 `fx_spark` 만(기획 8.3) → 죽음 훅들 → 호위 공포.
  `Collapse`·`Culled` 는 행동만 끊고 훅을 부르지 않는다(분열·폭발·웅덩이 없음).

**표시 상태 `EnemyVisual`** (기획 8.3 번쩍임·알파, 9.1~9.3 발광)

```csharp
public struct EnemyVisual { public Rgb GlowColor; public float GlowAlpha; public Rgb FlashColor; public float FlashAlpha; public float AlphaMul; public Rgb FootRingColor; public float FootRingAlpha; }
```

- `EnemyThink` 8번에서 정의·등급·접두사·버프·부품이 여기에 쓴다. 렌더(WP5)는 이것만 읽고 부품 종류나 `ActionState` 를 해석하지 않는다 → 새 부품이 렌더 코드를 바꾸지 않는다.
  - 기본: 정의 `glow`·`alpha`. 등급: 정예 파랑 맥동(1.2s, 알파 0.25~0.45 — 스테이지 틱으로 계산), 우두머리 금색.
  - 겹칠 때: **접두사 발광 색 > 등급 발광 색, 맥동 주기는 등급 것**.
  - 부품: `self_destruct` 점화 중 발광 알파 0.35↔0.9, `charge` 예고 중 붉은 `Flash`, `phase_through` 가 장애물 위(`Clearance < 0`)면 `AlphaMul` 0.3, 오라를 받는 적은 발밑 원(버프 정의의 `foot_ring`).
- 맞을 때 흰 번쩍임은 WP3 가 쓰는 공통 필드 `HitFlashTime` 이고, 렌더가 `Visual` 위에 더한다. 시체에는 `CorpseSpawned` 가 그 순간의 색조·알파·발광을 복사해 간다.

### 6.7 등급·접두사 `affixes`, 버프 `buffs`

```json
{
  "affixes": [
    { "id": "berserk", "name": "광폭한", "stats": { "speed_pct": 0.35, "attack_cooldown_pct": -0.30 }, "glow": [255, 60, 60] },
    { "id": "ironskin", "name": "강철 피부의", "stats": { "armor": 3, "hp_pct": 0.5 }, "immunities": ["knockback"], "glow": [170, 170, 180] },
    { "id": "fire_enchanted", "name": "불타는", "glow": [255, 140, 40], "exclude_tags": ["fire"],
      "behaviors_add": [ { "type": "telegraphed_blast", "trigger": "on_death", "points": "self",
                           "telegraph": { "effect": "fx_warning_circle", "time": 0.5 },
                           "radius": 70, "damage": 12, "effect": "fx_fire_explosion" } ] }
  ],
  "buffs": [
    { "id": "command",       "stats": { "speed_pct": 0.25, "damage_pct": 0.25 }, "foot_ring": [255, 210, 90] },
    { "id": "war_cry",       "stats": { "damage_pct": 0.30 }, "foot_ring": [255, 80, 60] },
    { "id": "slow_hellfire", "stats": { "speed_pct": -0.25 }, "debuff": "slow" }
  ]
}
```

- **적 능력치 키 `EnemyStatKey`**: `hp_pct`, `armor`, `speed_pct`, `damage_pct`, `attack_cooldown_pct`, `knockback_resist`. 접두사 `stats` 와 버프 `stats` 가 같은 키를 쓰고,
  목록에 없는 키는 로딩 오류다(주인공 쪽 `StatKey`·13.5-9 와 같은 장치).
  - 적용: `hp_pct`·`armor`·`knockback_resist` 는 생성 때 한 번(접두사만). `speed_pct`·`damage_pct` 는 매 틱(버프 포함) 더해서 곱한다(기획 4.2 "버프%는 더함").
    `attack_cooldown_pct` 는 **모든 능력 부품과 패턴의 `cooldown` 매개변수**에 × (1 + 합)(최소 ×0.3). 보스·중간 보스는 접두사를 받지 않는다.
- 접두사 `exclude_tags`·`exclude_parts`: 이 태그나 부품을 가진 적에게는 붙지 않는다(예: `fire_enchanted` 는 `burning_dead` 에 안 붙음, E `phasing` 은 `phase_through` 를 가진 망령에 안 붙음).
- **버프**: 부품은 버프 ID 와 시간만 넘긴다(`"buff": "command"`). 적마다 `BuffSlots`(인라인 6칸: 버프 정의 번호, 끝 틱, 값 배율). **같은 버프는 큰 값 하나만 남고**(기획 4.2), 다른 버프는 더한다.
  오라처럼 범위 안에 있는 동안만 붙는 효과는 매 틱 2틱짜리로 다시 붙이고, 시간제(함성 6s)는 그 시간으로 붙인다. 칸이 모자라면 가장 먼저 끝나는 것을 바꾼다.
  `debuff` 가 있는 버프는 정의 `immunities` 에 같은 이름이 있으면 붙지 않는다(`slow` 면역 = 보스·중간 보스). 버프 붙이기 도우미는 WP1 공통 코드라 WP3(장판 둔화)·WP4(오라·함성)가 같이 쓴다.
  → "새 오라를 가진 적" 이 정의(버프 1개 + 오라 부품 매개변수)만으로 늘어난다.
- 정예·우두머리 배율(HP ×3.5/×8, 피해 ×1.4/×1.6, 이동 ×1.1, 경험치 ×4/×10, 크기 ×1.15/×1.3, 넉백 저항, 발광, 드롭)과
  정예 무리 기본값(`pack_size: [3,5]`, `unique_chance: 0.3`, `banner: "정예 무리가 나타났다"`, `banner_time: 1.5`)은 `rules.ranks` 의 `elite`·`unique`·`pack` 에 둔다(`rules/enemies.json`, 6.10).
- `EnemyFactory.Create(def, rank, affixes, minutes, in SpawnContext)` 가 정의 + 등급 + 접두사를 합쳐 인스턴스를 만든다(데코레이터 방식: 정의는 그대로, 인스턴스가 합친 값을 가짐).
  접두사 부품은 정의 부품 뒤 슬롯에 붙는다. 분열체는 `rank` 를 물려받지 않는다(부품 매개변수 `inherit_rank: false` 기본). 생성 직후 `ISpawnHookPart` 들을 부른다.

### 6.8 무기 `weapons`, 진화 무기

```json
{
  "weapons": [
    { "id": "magic_missile", "name": "매직 미사일", "icon": { "sprite": "MagicMissileICon" },
      "pattern": "homing", "targeting": { "type": "nearest", "radius": 450 }, "projectile": "pj_magic_missile",
      "max_level": 8,
      "base": { "turn_rate": 360, "lifetime": 3.0, "burst_interval": 0.08, "knockback": 20 },
      "levels": {
        "damage":   [10, 10, 14, 14, 14, 18, 18, 22],
        "cooldown": [1.2, 1.2, 1.2, 1.0, 1.0, 1.0, 0.85, 0.85],
        "count":    [1, 2, 2, 2, 3, 3, 3, 4],
        "pierce":   [1, 1, 1, 1, 1, 2, 2, 2],
        "speed":    [300, 300, 300, 330, 330, 330, 360, 360] } },

    { "id": "arcane_orbit", "name": "비전 구체", "icon": { "sprite": "MagicMissileICon", "tint": [180, 120, 255] },
      "pattern": "orbit", "targeting": { "type": "none" }, "projectile": "pj_arcane_orb", "max_level": 8,
      "base": { "rotation": 180, "hit_interval": 0.5, "knockback": 30 },
      "levels": {
        "damage":       [8, 8, 11, 11, 14, 14, 17, 20],
        "count":        [2, 3, 3, 3, 4, 4, 5, 5],
        "orbit_radius": [70, 70, 80, 80, 90, 90, 100, 100],
        "duration":     [3.0, 3.0, 3.5, 4.0, 4.0, 5.0, 5.0, null],
        "cooldown":     [4.0, 4.0, 4.0, 3.5, 3.5, 3.0, 3.0, null] } },

    { "id": "inferno", "name": "연옥불", "base_weapon": "fireball", "icon": { "sprite": "FireBallICon", "frame_border": "gold" },
      "override": { "explosion_effect": { "effect": "fx_fire_explosion", "tint": [255, 90, 40] } },
      "on_explode": [ { "type": "ground_pool", "effect": "fx_inferno_pool", "tick_damage": 10, "radius": 60, "duration": 2.0,
                        "area_scaled": true, "duration_scaled": true } ] }
  ]
}
```

- 레벨 표는 **열 방식**(키마다 배열, 값은 `float?`). 검증: 배열 길이 = `max_level`, 키 = 패턴의 `LevelKeys` 뿐, 쿨다운은 늘지 않고 나머지는 줄지 않는다(13.5-2).
  `null` 은 패턴의 `NullableKeys` 에서만 허용: `duration` 의 null = 상시(단조성에서 가장 큰 값), `cooldown` 의 null = 발동 없음(같은 레벨 `duration` 이 null 일 때만, 단조성 검사에서 뺌)(5.3, 기획 5.5).
- 진화 무기: `base_weapon` 의 최고 레벨 값 위에 `override` 와 추가 효과를 얹는다. 레벨 표 없음.
  `override` 가 받는 것: 레벨 표 키(예: 비전 폭풍 `damage` 26·`cooldown` 0.3·`count` 1·`pierce` 3), `base` 키(회전 540°/s), `projectile`·이펙트 ID(`pj_arcane_storm`).
  추가 효과는 `on_hit` / `on_explode` / `on_crit` **목록**이고 원소는 `PartKind.WeaponEffect` 부품이다(E 단계 서리 화살 둔화·치명 튕김도 같은 자리).
- "먼저 만렙이 된 무기를 진화" 는 `WeaponSlot.MaxedAtTick` 으로 고른다(5.3).
- 아이콘은 `{ sprite, frame?, dir?, tint?, scale?, layers?, frame_border? }` — 기획 5.9 의 "화살 3장 겹침" 은 `layers`, 탐욕 아이콘(28px `GoldIcon` 을 48 칸에)은 `scale`.

### 6.9 패시브·진화·획득물·드롭

```json
{
  "passives": [
    { "id": "vitality", "name": "활력", "icon": { "sprite": "IncHealth" }, "max_level": 5,
      "per_level": { "max_hp": 20 }, "on_acquire": { "heal": 20 } },
    { "id": "field_runner", "name": "질주", "icon": { "sprite": "FieldRunner" }, "max_level": 5,
      "per_level": { "move_pct": 0.08, "pickup_pct": 0.20 } }
  ],
  "evolutions": [ { "weapon": "fireball", "passive": "arcane_power", "result": "inferno" } ],
  "pickups": [
    { "id": "gem_blue", "kind": "xp", "draw": { "procedural": "gem", "size": [10, 14], "color": [90, 150, 255] }, "value_range": [1, 4], "magnet": true },
    { "id": "healing_potion", "kind": "heal", "amount": 30, "draw": { "sprite": "HealingPotion", "scale": 0.5 }, "magnet": false, "skip_when_full": true }
  ],
  "drop_tables": [
    { "id": "dt_common", "entries": [
        { "pickup": "gold", "chance": 0.03, "value": [1, 5] },
        { "pickup": "healing_potion", "chance": 0.003 },
        { "pickup": "mana_surge", "chance": 0.002 },
        { "pickup": "magnet_rune", "chance": 0.001 } ] },
    { "id": "dt_miniboss", "entries": [
        { "pickup": "elite_chest", "chance": 1.0, "value": [3, 3] },
        { "pickup": "gold", "chance": 1.0, "value": [50, 50] },
        { "pickup": "magnet_rune", "chance": 1.0 } ] }
  ]
}
```

- `elite_chest` 항목의 `value` 는 **강화 횟수**다(중간 보스 3, 우두머리 1 — 기획 9.2·9.4·11.1).
- 능력치 키는 `StatKey` 열거형(기획 4.1 표 그대로, JSON 이름 = 키). 패시브 `per_level` 의 키가 열거형에 없으면 오류(13.5-9).
- 경험치 보석은 드롭 표가 아니라 규칙이다(죽으면 항상 1개, 값 = 적 경험치). 보석 색은 값 범위로 `pickups` 에서 고른다.

### 6.10 레벨업 선택지·규칙 — `rules/hero.json`, `rules/enemies.json`

레벨업 카드는 따로 정의하지 않는다. 무기·패시브 정의에서 **만든다**. 규칙과 채움 카드만 `rules` 에 둔다. `rules` 는 두 파일에서 하위 키 단위로 합친다(6.1).

```json
// rules/hero.json (WP3)
{
  "rules": {
    "hero": { "sprites": "set_hero", "radius": 14, "invuln_after_hit": 0.25,
              "stats": { "max_hp": 100, "move_speed": 95, "armor": 0, "regen": 0, "pickup_radius": 60 },
              "start_weapons": ["magic_missile"], "crowd_slow": { "touching": 3, "mul": 0.6 } },
    "xp_curve": [ { "from": 1, "to": 1, "base": 5, "per": 0 },
                  { "from": 2, "to": 19, "base": 5, "per": 10, "origin": 1 },
                  { "from": 20, "to": 39, "base": 185, "per": 13, "origin": 19 },
                  { "from": 40, "to": null, "base": 445, "per": 16, "origin": 39 } ],
    "level_up": { "cards": 3, "weights": { "owned_weapon": 100, "owned_passive": 100, "new_weapon": 70, "new_passive": 70 },
                  "weapon_slots": 4, "passive_slots": 4, "rerolls": 3, "skip_heal": 10, "input_lock": 0.4,
                  "guarantee_new_weapon_at": [2, 3],
                  "fillers": [ { "id": "filler_potion", "name": "체력 물약", "heal": 30, "icon": { "sprite": "HealingPotionIcon" } },
                               { "id": "filler_gold", "name": "금화 주머니", "gold": 25, "icon": { "sprite": "GoldIcon" } } ],
                  "stat_labels": {
                    "damage": { "label": "피해" }, "cooldown": { "label": "쿨", "unit": "s" }, "count": { "label": "발 수" }, "radius": { "label": "반경" },
                    "max_hp": { "label": "최대 체력" }, "damage_pct": { "label": "피해", "format": "pct" }, "move_pct": { "label": "이동", "format": "pct" } } },
    "level_up_push": { "radius": 150, "distance": 80 }
  }
}
```

```json
// rules/enemies.json (WP4)
{
  "rules": {
    "ranks": { "elite":  { "hp": 3.5, "damage": 1.4, "speed": 1.1, "xp": 4,  "scale": 1.15, "knockback_resist_add": 0.3, "affixes": 1, "drop_table": "dt_elite",
                           "glow": [90, 140, 255], "pulse": { "period": 1.2, "alpha": [0.25, 0.45] } },
               "unique": { "hp": 8,   "damage": 1.6, "speed": 1.1, "xp": 10, "scale": 1.3,  "knockback_resist": 0.8,   "affixes": 2, "drop_table": "dt_unique",
                           "glow": [255, 200, 80] },
               "pack":   { "pack_size": [3, 5], "unique_chance": 0.3, "banner": "정예 무리가 나타났다", "banner_time": 1.5 } },
    "caps": { "enemies": 300, "enemies_absolute": 360, "hero_projectiles": 400, "enemy_projectiles": 200, "gems": 300 },
    "spawn": { "band": [40, 200], "tries": 12, "edge_margin": 4, "group": [1, 3], "group_radius": 40, "accumulator_hold": 3 }
  }
}
```

- 카드 문구 `피해 14 → 18` 은 레벨 표 두 행을 비교해 `stat_labels` 로 만든다 → 새 무기도 문구가 저절로 생긴다.
  패시브 카드(`최대 체력 +20`, `피해 +10%`)도 같은 표에서 `StatKey` 라벨과 `format`(`abs` 기본 / `pct`)으로 만든다. 표에 없는 키를 쓰는 무기·패시브는 검증 경고.
- `level_up_push`: 선택 창이 닫힌 다음 스테이지 틱에 주인공 반경 150 안 적을 80px 밀어낸다(피해 0, 기획 12.6).

### 6.11 맵 `maps`, 장애물 종류 `obstacle_kinds`

```json
{
  "maps": [
    { "id": "dungeon1", "image": "Dungeon1.png", "minimap": "MinimapDungeon1.png", "size": [1536, 1024],
      "hero_start": { "pos": [775, 495], "facing": "south" },
      "collision": [
        { "id": "bush_nw", "shape": { "polygon": [[190,0],[570,0],[570,95],[400,105],[260,75]] }, "blocks_projectiles": false },
        { "id": "sarcophagus_e", "shape": { "rect": [1325, 418, 1450, 478] }, "blocks_projectiles": false },
        { "id": "plinth_sw", "shape": { "circle": [245, 700, 38] }, "blocks_projectiles": true }
      ],
      "entries": [ { "id": "stairs_nw", "outside": [-80, -60], "inside": [140, 140] },
                   { "id": "south_path", "outside": [765, 1104], "inside": [765, 950] } ],
      "anchors": { "center": [775, 495], "glow_nw": [575, 300], "glow_ne": [965, 305] },
      "texture_source": [620, 780, 900, 1000] }
  ],
  "obstacle_kinds": [
    { "id": "pillar",        "height": 72, "blocks_projectiles": true,  "decor": "ring" },
    { "id": "broken_pillar", "height": 30, "blocks_projectiles": true,  "decor": "jagged" },
    { "id": "wall_chunk",    "height": 36, "blocks_projectiles": true,  "decor": "mortar" },
    { "id": "brazier",       "height": 28, "blocks_projectiles": true,  "decor": "bowl",
      "top_effect": { "effect": "fx_brazier_fire" }, "light": { "color": [255, 170, 80], "radius": 140, "flicker": 0.08 } },
    { "id": "arch",          "height": 80, "blocks_projectiles": true,  "decor": "arch" },
    { "id": "collision_only","draw": false }
  ]
}
```

### 6.12 스테이지 `stages`

```json
{
  "stages": [
    { "id": "stage_courtyard", "name": "버려진 안뜰", "map": "dungeon1", "unlock": null,
      "obstacles": [
        { "id": "s1_brazier_1", "kind": "brazier", "shape": { "circle_at": "glow_nw", "r": 16 } },
        { "id": "s1_wall_w", "kind": "wall_chunk", "shape": { "rect": [0, 440, 130, 468] }, "height": 30 },
        { "id": "s1_broken_1", "kind": "broken_pillar", "shape": { "circle": [300, 520, 20] } }
      ],
      "lighting": { "ambient": [215, 225, 200], "darkness": 0.35,
                    "hero_light": { "color": [255, 240, 220], "radius": 280 },
                    "kind_lights": { "brazier": { "flicker": 0.08 } } },
      "victory": { "type": "boss" },
      "scaling": { "hp": [1.0, 0.08], "damage": [1.0, 0.03] },
      "elite_chance": { "every": 60, "from": "02:00", "chance": 0.25 },
      "spawn_intervals": [
        { "from": "00:00", "to": "01:00", "max_alive": 25, "per_second": 0.8, "weights": { "skeleton": 100 } },
        { "from": "01:00", "to": "02:00", "max_alive": 45, "per_second": 1.3, "weights": { "skeleton": 70, "zombie": 30 } },
        { "during": "boss", "max_alive": 120, "per_second": 3.0, "weights": { "skeleton": 60, "zombie": 40 } }
      ],
      "events": [
        { "at": "00:00", "type": "banner", "text": "버려진 안뜰", "duration": 2 },
        { "at": "02:00", "type": "march", "enemy": "goatman_archer", "count": 5, "at_entry": "north_path", "heading": "south", "straight": 15 },
        { "at": "03:00", "type": "ring", "enemy": "burning_dead", "count": 12, "radius": 400 },
        { "at": "04:30", "type": "elite_pack", "enemy": "skeleton", "count": 4, "affixes": 1, "unique_chance": 0.3 },
        { "at": "05:00", "type": "miniboss", "enemy": "rotting_hulk", "at_entry": "south_path" },
        { "at": "05:00", "type": "escort", "enemy": "skeleton_captain", "escorts": 8, "at_entry": "west" },
        { "at": "10:00", "type": "boss", "enemy": "skeleton_king", "at_entry": "stairs_nw" }
      ] }
  ]
}
```

- 배율 식 `[a, b]` = a + b × 분. 구간은 `from`/`to`, 보스 중 구간은 `during: "boss"`. 이벤트 종류 = 기획 13.4, 각 종류가 `PartKind.StageEvent` 부품(레지스트리 등록, WP4).
- `weights` 는 JSON 객체로 적지만 로딩 때 **적힌 순서의 배열**로 바뀐다(3.4). 감독은 종류를 먼저 뽑고, 그 종류의 `spawn_group` 이 있으면 무리 크기를 거기서 뽑는다(6.5).
- `victory`: `{ "type": "boss" }`(최종 보스 처치) 또는 `{ "type": "survive", "at": "10:00" }`(그 시각까지 생존). 단계 1 세로 조각의 `stage_courtyard` 는 `survive`, 단계 2 에서 `boss` 로 바꾼다. 처리는 3.5 끝.
- `kind_lights` 는 그 종류의 빛을 덮어쓰거나 **빛이 없는 종류에 새로 붙인다**. 받는 키: `color`, `radius`, `flicker`, `pulse: { "period", "amount" }`.
  스테이지 2: `"kind_lights": { "pillar": { "color": [255, 60, 40], "radius": 120, "pulse": { "period": 2.0, "amount": 0.15 } } }`.
- `light_change` 이벤트는 바꿀 곳을 경로로 고른다: `{ "at": "11:30", "type": "light_change", "set": { "kind_lights.pillar.pulse.period": 1.0 } }`. 받는 경로: `ambient`, `darkness`, `hero_light.*`, `kind_lights.<종류>.*`.
  감독(WP4)이 `World.Lighting` 을 바꾸고 렌더(WP5)가 읽는다.
- 이벤트가 적 정의를 덮어쓰는 매개변수: `escort` 의 `escorts`(호위 수, 0 포함), `march` 의 `heading`·`straight`, `boss`/`miniboss` 의 `enrage`·`arrival`. 호위 수는 `SpawnContext.EscortCount` 로 넘기고(생성 훅이 읽음), 행진 지시·광폭화·등장 연출은 이벤트가 생성 직후 그 적에 직접 건다(6.6).
- 장애물 모양은 좌표 또는 기준점 참조(`circle_at`). 아치는 `"parts": [{ "circle": [690,240,18] }, { "circle": [840,240,18] }], "lintel": { "x": [672, 858], "thickness": 18 }`.

### 6.13 몬스터 늘리기 — 구조·시험·이번에 늘린 것 (사용자 요구 8)

새 그림 없이 종류를 늘리는 길을 데이터 쪽에 세 겹으로 둔다.

1. **변형 정의(`extends`)**: 같은 그림 묶음 + 색조·발광·알파·크기·수치·부품 일부만 다르게. `behaviors_add`·`behaviors_patch` 로 부모 부품을 다시 적지 않는다.
   몸 반경은 `base_radius × scale` 로 저절로 맞는다(6.5).
2. **부품 조합**: 일반 적 부품 12종 + 적 동작 7종(패턴·훅 안쪽)을 **어떤 적이든** 쓸 수 있다. 원거리 부품은 투사체 ID 를, 오라·함성은 버프 ID 를 받는다.
3. **등급 × 접두사**: 14종 × (일반/정예/우두머리) × 접두사 조합이 실행 중에 만들어진다. 접두사 제외 조건(6.7)이 어색한 조합을 막는다.

**이번에 MVP 로 올린 2종**(기획서 8.2 v0.3, 단계 3 · 스테이지 2 의 7:00 부터) — 둘 다 C# 추가가 거의 없다.

| 적 | 정의 | 새 코드 |
|---|---|---|
| `bone_brute` 뼈 거인 | `extends: skeleton`, (230,220,200) ×1.7(반경 20, `M` 필드), 느림, 패턴 `slam`(`melee_slam` 반경 60 + 예고 0.5s) — 6.5 예 | 없음(보스 동작 재사용) |
| `hell_goat` 지옥 염소 | `extends: goatman_archer`, (150,40,30) + 붉은 발광 ×1.15, `behaviors: [charge]`(굶주린 시체와 같은 부품), `spawn_group: [5,7]` | 감독의 `spawn_group` 처리(무리 크기 뽑기 한 줄) |

**나머지 확장 후보(E)의 비용**

| 확장 적 | 정의 | 새 코드 |
|---|---|---|
| `blood_knight` 피의 기사 | `extends: fallen_knight`, 색조, `lifesteal` | `lifesteal` 적중 수정 부품 1개(접두사 `vampiric` 과 공유, `HitLanded` 사용) |
| `shadow_wraith` 그림자 망령 | `extends: wraith`, 색조·알파, `blink` | `blink` 능력 부품 1개(자리는 `PlacementResolver`) |

**"새 적 = JSON 1개" 를 자동으로 확인하는 시험** (WP4, 10.1 `Enemies`)

- **적 정의 전수 연기 시험**(`Theory`, 실제 `Data/` 의 생성 가능한(`abstract` 아님) 적 전부 + `Fixtures/variants/`): 빈 맵에 무적 주인공과 150px 떨어뜨려 하나 생성 →
  10초(600틱) 동안 예외·NaN 없음 → 근접형은 주인공 쪽으로 움직였거나 피해 요청을 냈고, 원거리형은 거리 띠 안에서 투사체를 냈음 → HP 0 으로 만들면 죽음 훅이 돌고 저장소에서 빠지고 보석 1개.
  보스·중간 보스는 패턴 하나 이상이 실행됐는지를 본다. **새 적을 데이터에 넣으면 이 시험이 저절로 늘어난다.**
- **코드 없는 변형 증거**: `tests/…/Fixtures/variants/ash_skeleton.json`(`extends: skeleton` + 색조 + `behaviors_add: [ground_hazard on_death]` + `behaviors_patch`)을 위 시험에 넣는다.
  이 파일을 바꾸면서 C# 을 고쳐야 했다면 구조가 깨진 것이다.
- `--preview enemies` 는 배치를 고정한다(9절): 적 정의마다 한 행(ID, 방향 0·고정 프레임으로 걷기/공격/죽음 칸) → 같은 시각 캡처가 늘 같은 그림이라 정의를 고친 전후를 나란히 비교할 수 있다.
- 검증 규칙이 "그 적이 쓰는 클립(`attack`·`cast`)이 그림 묶음에 있는가", "`event_frame` ≤ 그 클립 프레임 수" 를 본다 → 해골에 시전 그림이 없는 문제(기획 8.2)가 로딩 때 잡힌다.
- 데이터 경고 두 개(6.14)가 "늘렸는데 화면에서 안 보이는" 경우를 잡는다: 어느 스테이지에서도 나오지 않는 적, 같은 스테이지에서 색이 구별되지 않는 같은 그림 묶음의 두 적.

### 6.14 정의 간 참조 검증 목록

| 규칙 | 내용 | 위치 |
|---|---|---|
| 13.5-1 | 종류 안 ID 유일, 모든 `DefRef` 존재(그림·이펙트·투사체·버프·맵·입구 `entry:`·기준점·몬스터·드롭 표·진화 재료·보스) | Data (WP7) |
| 13.5-2 | 레벨 표 길이 = 최대 레벨, 키 = 패턴 `LevelKeys`, 단조성. `null` 은 `NullableKeys` 에서만: `duration` 의 null = 가장 큰 값, `cooldown` 의 null 은 같은 레벨 `duration` 이 null 일 때만 허용하고 단조성에서 뺌 | Data + 패턴 |
| 13.5-3 | 생성 구간이 0:00 부터 빈틈·겹침 없이, 가중치 합 > 0, `during: boss` 는 보스 있을 때만 | Data |
| 13.5-4 | 장애물이 맵 안, 장애물끼리·경계와 80px 이상 또는 붙음 | Nav/Validation (WP2) |
| 13.5-5 | 길 검사: 반경 14(주인공 시작 → 모든 입구 진입점·도달 칸), `NavConst.FieldS`·`FieldM`(16·26)과 34(입구 진입점 → 원형 바닥), 주인공이 설 수 있는 모든 칸에서 `S` 필드 시작점 ≥ 1 | Nav/Validation (WP2) |
| 13.5-6 | 입구 진입점·시작 위치의 clearance ≥ 38, 바깥 시작점은 맵 밖 | Nav/Validation (WP2) |
| 13.5-7 | 시트 크기 = 프레임 × 개수(+slack), 그림 묶음 클립 존재 / 실제 PNG 크기(IHDR, 있을 때만) | Data / Art (WP5) |
| 13.5-8 | 진화 재료 무기의 최대 레벨, 재료 패시브 존재 | Data |
| 13.5-9 | 패시브 키 ∈ `StatKey`, 접두사·버프 키 ∈ `EnemyStatKey` | Data |
| 추가(오류) | `extends` 순환·없는 부모, `abstract` 정의를 생성에 씀, 부품 `(종류, type)` 미등록, 부품 매개변수 검사, `*_patch` 의 없는 id, 클립·이벤트 프레임 범위, 큰 피해 패턴의 예고 필수, 참조 쪽 덮어쓰기는 색조·크기만, 부품 슬롯 > 12, 여러 틱 동작이 `actions` 마지막이 아님, `rules` 하위 키 중복 | Data + 부품 |
| 추가(경고) | flow 유닛 반경 > 26, 명시한 `radius` 가 `round(base_radius × scale)` 와 2 넘게 다름, **생성 가능한 적인데 어느 스테이지에서도 나오지 않음**(가중치·이벤트·소환·분열·호위 어디에도 없음), **같은 스테이지에 나오는 두 적이 같은 그림 묶음인데 색조 거리(RGB 유클리드) < 60, 크기 차 < 0.15, 알파·발광도 같음** | Data |

- 색조 구별 경고의 기준값(60·0.15)은 설계 제안이고 기획과 함께 정한다. 최종 판정은 `--preview enemies` 캡처를 사람이 본다.
  예: 스테이지 2 의 `ghoul`(150,200,120) 과 `plague_zombie`(120,170,60) 은 거리 약 73 이라 통과.

---

## 7. 렌더

### 7.1 가상 해상도와 창 (기획 열린 질문 1: 960×540 으로 간다)

- 월드는 **960×540 렌더 타깃**(`WorldRT`)에 1:1 월드 픽셀로 그린다. 창(또는 전체 화면)에 맞출 때:
  1. 정수 배율 k = ceil(min(창W/960, 창H/540)) 로 `WorldRT` 를 점 샘플링(PointClamp)해 960k×540k 중간 타깃에 키운다.
  2. 그것을 선형 샘플링으로 창 안 레터박스 사각형(비율 16:9 유지, 남는 곳 검정)에 줄여 그린다.
  → 정수 배율에서는 그대로 날카롭고, 비정수 배율에서도 번짐이 1px 테두리 정도로 줄어든다(sharp bilinear).
- 최종 결과는 `PresentRT` 에 모은다(월드 + UI). 백버퍼에는 그것을 한 번 늘여 복사한다. 캡처는 `PresentRT` 를 저장한다.
  `PresentRT` 크기는 백버퍼와 따로 정한다: `--window` 값이 있으면 그 크기로 고정(창이 화면 배율·작은 모니터 때문에 줄어도 캡처 크기·배율 k 가 바뀌지 않음), 없으면 창 크기.
- 전체 화면은 테두리 없는 창(`HardwareModeSwitch = false`). F11·Alt+Enter. 개발 옵션 실행에서는 F11 을 받지 않는다(실제 입력을 읽지 않음, 3.2).
- `GraphicsDeviceManager.GraphicsProfile = GraphicsProfile.HiDef` 를 명시한다. `LOGO`(550×3240), `SkeletoneKingAttack/Die`(1920×2560), 물약(96×2560) 등이 Reach 한도 2048 을 넘는다.

### 7.2 카메라

- 카메라 중심 = 보간된 주인공 위치, 맵 경계에 맞춰 자름(시야 960×540, 맵 1536×1024). **따라가기 지연 없음**: 시뮬레이션의 생성 띠(카메라 시야 바깥 40~200px)와
  보이는 화면이 정확히 같아야 하고, 지연이 있으면 화면 안에서 적이 생기는 일이 생긴다. 시뮬레이션(`World.ViewRect`)과 렌더 카메라가 **같은 함수** Core `CameraMath.ViewRect(heroPos, mapSize, viewSize)` 를 쓴다.
- 그릴 때 카메라 위치를 정수 픽셀로 내린다(픽셀 그림 떨림 방지). 개체 위치도 그리기 직전에 반올림.
- **흔들림**: 표시 이벤트 `Shake(tick, px, 시간)` 중 지금 남은 것 가운데 가장 센 것(기획 12.6) → ±px 정수 오프셋 = `StableHash(seed, stageTick)`(3.4, 프레임마다 뽑는 난수 아님). 설정에서 끄면 0.
- 화면 → 월드 변환(창 좌표 → 레터박스 → 가상 960×540 → 월드)은 `RenderContext.ScreenToWorld(Point)` 하나다(WP5 구현). 마우스 이동(WP6)이 이것을 쓴다.

### 7.3 그리기 층

| # | 층 | 내용 | 정렬·혼합 |
|---|---|---|---|
| 0 | 바닥 | 맵 그림 | 불투명 |
| 1 | 바닥 장식 | 장애물 그림자, 장판, 예고(원·선), 발밑 원(오라·정예·아군), 등장 연출 어두운 원 | 생성 순, 알파/가산 |
| 2 | 시체 | `CorpseLayer` | 오래된 순 |
| 3 | 획득물 | 보석·금화·물약·상자(빛 기둥은 9층) | 알파 |
| 4 | 유닛·장애물 | 주인공·적·장애물 몸체·화로 불 | **발 y 정렬**, 알파(+발광은 가산 두 번째 그리기) |
| 5 | 위 장애물 | 아치 상인방 | 알파 |
| 6 | 빛 | 빛 지도 곱하기(7.7) | 곱하기 |
| 7 | 발광 | 투사체, 폭발·맞음 이펙트, 화로 불꽃의 가산 부분, 보석 빛, 가려진 주인공 실루엣 | 가산(투사체는 몸체 알파 + 빛 가산) |
| 8 | 화면 효과 | 낮은 체력 붉은 비네트, 광폭화 맥동 | 알파 |
| 9 | 월드 UI | 이름(정예·우두머리), 주인공 발밑 체력 막대, 보스 아래 표시, 피해 숫자 | `PresentRT` 해상도로(글자 선명) |
| 10 | HUD·오버레이 | 8절 | `PresentRT` 해상도 |

- 투사체·폭발을 빛 **뒤에** 그리는 이유: 어둠에 묻히지 않게(빛을 내는 것은 빛 영향을 받지 않음).
- 9~10층은 가상 좌표(960×540)로 배치하고 `UiCanvas` 가 창 배율로 바꿔 그린다. 글꼴은 크기 × 배율로 요청해 늘 선명하다(FontStashSharp 는 크기별로 그림을 굽는다).

### 7.4 y 정렬과 그리기 호출

- 4층 항목을 `DrawItem { float SortY; int Tiebreak; Kind; Index }` 배열에 모아 `Array.Sort`(키 = SortY, 같으면 Tiebreak = 개체 ID) — 같은 y 에서 깜빡임 없음.
  유닛 SortY = 발 y, 장애물 SortY = 바닥 모양의 가장 아래 y(기획 10.2).
- SpriteBatch `Deferred` 로 정렬된 순서대로 낸다. 텍스처가 바뀔 때마다 그리기 호출이 끊기므로 y 로 섞인 400 개체는 최악 400번 호출이다.
  데스크톱 GL 에서 감당할 수준으로 보지만 10.3 으로 잰다(기준은 4.9 표). 넘치면 R5 대응.
- 정예 발광·번쩍임처럼 가산이 필요한 그리기는 같은 항목 안에서 혼합 상태를 바꾸지 않도록, **4층을 끝낸 뒤 7층에서 같은 정렬 순서로 가산만 다시 그린다**(혼합 상태 전환 2번).
  가산 덧그림은 어둠의 영향을 받지 않는데, 발광은 원래 빛나는 것이라 맞다.
  대가: 기둥 뒤에 가려진 유닛의 발광·번쩍임이 기둥 위로 비친다. 작고 짧아 허용한다(눈에 띄면 가려진 유닛만 발광을 건너뛴다 — 7.5 의 주인공 가림 판정 재사용).

### 7.5 색 키·색 변형·번쩍임

- **색 키**: 시트 PNG 는 24비트(알파 없음, 확인함). 읽을 때 `color_key` 와 같은 픽셀 → (0,0,0,0), 나머지 → 알파 255 로 올린다(곱할 알파가 1 이라 premultiply 는 그대로).
  `Texture2D.FromStream(device, stream, colorProcessor)`(3.8.6 에 있음, 확인함)의 색 처리 콜백으로 한 번에 한다.
  이 호출은 해독과 GPU 올리기를 같이 하므로 **주 스레드에서 차례로** 부른다(작업 스레드에서 부르면 DesktopGL 이 GL 호출을 주 스레드로 넘겨 기다려 교착될 수 있다).
  시작이 느리다고 재 보면 그때 StbImageSharp(FontStashSharp 가 끌어오는 2.30.16 — 쓰려면 csproj 에 명시 참조)로 작업 스레드에서 `ImageResult.FromMemory` → 색 키 →
  주 스레드에서 `new Texture2D` + `SetData` 로 바꾼다(R7).
- **색조**: SpriteBatch 색 = 색조 × 알파(premultiplied 이므로 `new Color(r*a, g*a, b*a, a)`). 곱하기라 어둡게만 된다(기획 8.1 그대로).
- **발광**: 같은 프레임을 `BlendState.Additive` 로 발광 색 × 알파로 한 번 더(7층). 발광 색·알파·맥동·번쩍임·발밑 원·알파 배율은 모두 `Enemy.Visual`(6.6)에서 읽는다 — 렌더는 부품 종류를 모른다.
- **적 투사체**: 투사체 진영이 `Enemy` 면 몸체 위에 붉은 테 빛(가산, (255,60,40) 약하게)을 더해 주인공 투사체(가산 발광)와 구별한다(기획 5.1).
- **맞을 때 번쩍임**: 0.08s 동안 같은 프레임을 가산으로 흰색 × 0.8 한 번 더(기획이 허용한 "밝아짐"). 흰 실루엣 텍스처를 따로 만들지 않는다 — 유닛 시트만 약 200MB(VRAM)라 두 벌은 무겁다.
- **알파**(망령 0.55, `phase_through` 장애물 위 0.3, 등장 연출, 시체 사라짐): 색 전체에 곱한다.
- **가려진 주인공**: 주인공 그림 사각형과 겹치는 장애물 중 SortY 가 주인공보다 큰 것이 있으면, 7층에서 주인공을 (180,200,255) × 0.35 로 한 번 더 그린다(장애물 위로 비치는 윤곽).

### 7.6 장애물을 배경과 어울리게 — 굽기

스테이지 시작 때 장애물 배치마다 **CPU 로 픽셀을 계산해** 스프라이트 두 장(몸체, 그림자)을 만든다. `Core/Art/ObstacleBaker` (순수 함수, 테스트 가능).

```csharp
public sealed record BakedObstacle(PixelBuffer Body, Int2 BodyOrigin, PixelBuffer Shadow, Int2 ShadowOrigin,
                                   PixelBuffer? Over, Int2 OverOrigin, float FootY);          // Int2·RectI 는 Core Common (XNA Point 없음)
public static class ObstacleBaker
{
    public static BakedObstacle Bake(PixelBuffer map, RectI textureSource, ObstacleBakeInput input, uint seed);
}
```

1. **윗면**: 바닥 모양(충돌 도형)을 위로 `height` px 올린 모양. 픽셀마다 맵의 질감 원본 영역(남쪽 돌길 (620,780)~(900,1000))에서 색을 가져온다.
   배치마다 다른 오프셋(배치 ID 해시)으로 샘플링하고 원본 영역을 넘으면 거울 반복 → 장애물마다 다른 돌 무늬. 밝기 ×1.1.
2. **옆면(높이 돌출)**: 각 열에서 "바닥 모양 아래 가장자리" 와 "윗면 아래 가장자리" 사이를 채운다(남쪽을 향한 면만 보인다). 같은 질감을 세로로 늘려 ×0.55,
   아래로 갈수록 ×0.45 까지 어둡게(땅과 닿는 곳 그늘).
3. **테두리**: 윗면 가장자리 1px 을 밝게(×1.4), 전체 실루엣 바깥 1px 을 검정 α0.5 로 — 배경과 분리되면서 튀지 않는다.
4. **꾸밈**(`decor`): `ring`(윗면 원 안쪽 2px 어두운 고리), `jagged`(윗면 다각형 꼭짓점 ±4px 흔들기, 씨앗 고정), `mortar`(윗면 가로 줄눈 2개),
   `bowl`(화로: 윗면 가운데 어두운 원 + 불 이펙트 자리), `groove`(석관 세로 홈), `round_ends`(쓰러진 기둥 양 끝 둥글게), `arch`(두 기둥 + `Over` 에 상인방).
5. **그림자**: 바닥 모양을 (높이×0.3, 높이×0.2) 밀어 검정 α0.45, 상자 흐림(3px, 2회)으로 가장자리를 부드럽게. 1층에 그린다.
6. **조명**: 따로 하지 않는다 — 빛 지도(7.7)가 맵·유닛과 똑같이 곱해지므로 어울린다.

- 몸체 스프라이트의 기준점 = 바닥 모양의 가장 아래 y, SortY = 그 y. 기둥 뒤로 지나가는 유닛은 SortY 가 작아 먼저 그려져 가려진다.
- `collision_only` 는 굽지 않는다(배경 그림에 이미 있음).
- 굽기 비용: 장애물 15개 × 평균 100×120px → 수 ms. 스테이지 로딩 때 한 번.

### 7.7 조명

- `LightRT`(480×270, 월드의 절반 해상도 — 부드러운 빛, 채움 비용 ¼)를 `주변광 × (1 − 어둠)` 으로 지운다.
- 빛마다 미리 만든 방사형 그라데이션 텍스처(코드로 생성, 256px)를 색 × 세기로 **가산**해 그린다(주인공·화로·기둥·폭발·지옥불). 깜빡임·맥동은 세기에.
  깜빡임 = `StableHash(seed, lightId, stageTick)`, 맥동 = 스테이지 틱의 사인(3.4). 현재 조명 값(주변광·어둠·종류별 빛)은 `World.Lighting` 에서 읽는다(`light_change` 가 바꿈).
- `WorldRT` 위에 `LightRT` 를 **곱하기 혼합**(`ColorSourceBlend = DestinationColor, ColorDestinationBlend = Zero`)으로 늘려 그린다.
  → 주변광 곱하기와 어둠 뚫기가 한 번에 된다(기획 10.5 의 두 단계와 같은 결과, 단 빛이 겹쳐도 1 을 넘지 않음).
- 빛 상한 64: 주인공·장애물 빛을 먼저, 남는 자리를 짧은 빛(폭발) 중 주인공에게 가까운 순으로.

### 7.8 손맛

| 요소 | 구현 |
|---|---|
| 피해 숫자 | 게임 쪽 풀 120개. 시뮬레이션의 `Damage(tick, 대상, 값, 치명, 주인공이 받음)` 표시 이벤트를 받아 같은 대상 0.1s 안이면 합침. 0.6s 동안 24px 위로 + 알파 감소. 9층 |
| 번쩍임·발광 | 7.5 |
| 넉백·히트 스톱·슬로모션 | 시뮬레이션(4.6, 3.3) |
| 흔들림 | 7.2 |
| 처치 연출 | 시체 층 + 작은 보석 튀어 오름(표시만, 0.25s, 실제 보석은 시뮬레이션 위치에 바로 있음) |
| 위험 예고 | `Telegraph` 개체를 1층에 원/선으로, 남은 시간 비율만큼 안쪽이 차오름 |
| 레벨업 빛 기둥·정예 상자 | 표시 이벤트 → `FxSystem`. 선택 뒤 주변 적 밀어내기는 시뮬레이션(3.3, `level_up_push`) |
| 커서 | `DIInop.cur` 를 `CursorLoader` 가 읽음(ICO 머리글 → 첫 항목, PNG 면 그대로 / BMP 면 32비트·AND 마스크 해석) → `MouseCursor.FromTexture2D(tex, 0, 0)` |

---

## 8. UI

### 8.1 씬과 오버레이

```csharp
public interface IScene
{
    bool PausesBelow { get; }    // 오버레이가 아래 씬의 FixedTick 을 막나
    bool DrawsBelow  { get; }    // 아래 씬을 그 뒤에 그리나
    void Enter(SceneStack stack); void Exit();
    void FixedTick(in AppTick t, InputFrame input);   // 60Hz, 위에서부터
    void Draw(RenderContext rc, float alpha);
    void AutoStep(AutoplayDriver bot) { }              // --autoplay 가 메뉴를 넘길 때
}
```

| 씬 | 종류 | 들어가는 곳 → 나가는 곳 |
|---|---|---|
| `DataErrorScene` | 씬 | 시작 때 데이터 오류 → 종료만 |
| `TitleScene` | 씬 | 시작 → 새 게임(StageSelect) / 설정(오버레이) / 종료 |
| `StageSelectScene` | 씬 | 카드 2장(잠김 표시) → Play |
| `PlayScene` | 씬 | 월드 + HUD. 시뮬레이션 소유 → Result |
| `LevelUpOverlay` | 오버레이(멈춤) | `World.AwaitingChoice == LevelUp` 이면 Play 가 **같은 앱 틱 안에서** 올림(3.3) |
| `ChestOverlay` | 오버레이(멈춤) | 상자 열기 1.2s 연출 |
| `PauseOverlay` → `SettingsOverlay` | 오버레이(멈춤) | Esc/P, 포기 → 패배 처리 |
| `ResultScene` | 씬 | 승리 3s / 패배 2s 뒤 → 다시 하기(같은 스테이지) / 타이틀 |
| `PreviewScene` | 씬(개발) | `--preview` |

- 씬 스택은 상태 패턴이다: 위 씬이 입력을 먼저 받고, `PausesBelow` 가 참이면 아래로 내려가지 않는다.
- `PlayScene` 은 시뮬레이션과 그림을 잇는 곳이다: 앱 틱마다 `StageStepper.AppTick(world, command)` 를 부르고(3.3), `World` 를 읽어 `WorldRenderer`·`Hud` 에 넘긴다.
  레벨업 선택은 `world.Progression`(`IProgression`)의 `Choose(index)` / `Reroll()` / `Skip()` 를 오버레이가 직접 부른다(시뮬레이션이 멈춘 사이라 결정성에 영향 없음).
  자동 조작이면 오버레이가 Core `AutoplayPolicy.PickCard` 의 답을 정해진 앱 틱 수 뒤에 고른다.
- 씬이 바뀔 때마다 로그에 `SCENE <이름> app_tick=…`, 결과 화면이 열릴 때 `RESULT …` 를 쓴다(9절).

### 8.2 HUD (기획 12.1)

- `Hud` 하나가 `World` 를 읽어 그린다: 경험치 막대, 시간, 보스 막대(최종 위·중간 아래), 무기·패시브 칸, 처치·금화, 미니맵, 체력 구슬, 지속 효과, 알림 문구.
- 체력 구슬은 코드로: 원 마스크 텍스처(생성) + 붉은 채움 사각형을 비율만큼(원 안만 보이게 그리기 순서로) + 돌 테(맵 질감 원본에서 고리 모양으로 구운 것 — 7.6 의 굽기 재사용).
- 미니맵: `MinimapDungeon1` ×0.4, 점은 1px 텍스처 늘이기, 장애물은 굽기 때 만든 1/10 축소 실루엣.
- 위젯: `Label`, `Button`, `Card`, `Bar`, `IconSlot` — 즉시 모드에 가까운 작은 클래스. 마우스 올림·방향키 선택·Enter 를 같은 "포커스" 개념으로.
- 글꼴: `C:\Windows\Fonts\malgun.ttf` → `malgunbd.ttf` 순으로 찾고, 모두 없으면 `arial.ttf` + 로그 경고(한글이 네모로 보임).
  `gulim.ttc` 같은 글꼴 묶음(.ttc)은 `FontSystem.AddFont(byte[])` 에 몇 번째 글꼴인지 고를 인자가 없어 목록에서 뺐다. 실제로 넣어 보고 되면 다시 넣는다.

### 8.3 저장 (기획 열린 질문 3)

- 위치: `%LOCALAPPDATA%\DiabloDefenceRemastered\save.json`(기록·해금·누적 금화·설정 한 파일, `version` 필드). 쓰기는 임시 파일 → 바꿔치기.
- `--save-dir <폴더>` 로 위치를 바꾼다. 개발 옵션 실행(9절 목록)인데 `--save-dir` 가 없으면 사용자 저장 파일을 **읽지도 쓰지도 않는다** — 기본값(창 모드, 스테이지 1 만 열림, 기록 없음)으로 시작하고 메모리에만 저장한다.
  읽기까지 막는 이유: 사용자가 켜 둔 전체 화면이 검증 실행에서 화면을 덮고, 사용자의 해금 상태에 따라 자동 조작이 고르는 스테이지가 달라지기 때문이다(brief 7절).

---

## 9. 개발용 실행 옵션

`Core/Dev/DevOptions.Parse(string[]) → (DevOptions, IReadOnlyList<string> errors)`. 모르는 옵션·잘못된 값은 오류 목록 → 로그 + 종료 코드 64.

| 옵션 | 뜻 |
|---|---|
| `--seed <n>` | 시뮬레이션 씨앗. 없으면 시각에서 만들고 로그 첫 줄에 적음 |
| `--autoplay` | 자동 조작(기획 15절). 메뉴도 넘김(각 화면 0.5s 뒤). `--background` 를 함께 켬. 실제 키보드·마우스는 읽지 않음 |
| `--stage <id>` | 타이틀·선택을 건너뛰고 바로 시작(잠금 무시) |
| `--start-at <mm:ss>` | 그 시각부터. 지난 이벤트는 건너뜀, 구간·배율은 그 시각 값. **보스 시각 이후면 보스를 바로 내보내고 보스 시계(광폭화)를 (시작 시각 − 보스 시각)으로 맞춘다**. 중간 보스는 건너뜀 |
| `--time-scale <n>` | 실제 시간 n 배속. 결과는 n 과 상관없이 같다(3.4) `[해석]` |
| `--god`, `--level <n>`, `--weapons <id:lv,…>`, `--passives <id:lv,…>` | 기획 15절 |
| `--stress <적 수>[,<투사체 수>]` | 상한을 그 값으로 올리고 스테이지 구성 가중치대로 맵 전체·시야 띠에 즉시 채운다(`--god` 함께 켬, 생성 출처 `Stress`). 투사체 수를 주면 그 수를 유지하는 시험용 발사를 더함 |
| `--freeze-ai` | 적 생각·이동을 멈춘다(렌더 정적 장면, `--stress` 와 함께) |
| `--capture <png>` | 캡처 파일 경로. 상대 경로는 현재 폴더 기준 |
| `--capture-at <초[,초…]>` | 앱 시간(초) 기준. 여러 개면 파일명 뒤에 `_<초>` 를 붙인다. 그 틱에 정확히 멈춰 보간 없이(alpha = 1) 그린 화면 |
| `--capture-on <levelup\|chest\|boss\|result>` | 그 일이 처음 일어나 화면이 다 열린 프레임을 캡처(시각을 미리 모를 때) |
| `--quit-after <초>` | 앱 시간 기준. 종료 코드 0. 캡처가 남아 있으면 먼저 한다 |
| `--exit-on-result` | 결과 화면이 열리면 `RESULT` 로그를 쓰고 종료 코드 0 |
| `--timeout <초>` | **벽시계** 감시. 넘으면 로그에 마지막 앱 틱·씬·스택 요약을 쓰고 종료 코드 4(틱이 안 도는 교착·무한 루프 대비). 개발 옵션 실행 기본 600 |
| `--window <W>x<H>` | 창 클라이언트 크기이자 **캡처 크기**(7.1). 개발 옵션 실행 기본 1280x720 |
| `--background` | 창을 앞으로 끌어오지 않음: `new GameApp()` 전에 환경 변수 `SDL_WINDOW_NO_ACTIVATION_WHEN_SHOWN=1`(SDL2 는 힌트를 환경 변수에서도 읽는다 — P/Invoke 없음), 비활성일 때 멈추지 않음(`InactiveSleepTime = 0`), 마우스 가두지 않음 |
| `--vsync <on\|off>` | 수직 동기(기본 on). 부하 측정은 `off` |
| `--debug-nav [grid,flow,astar,shapes,entries]` | 격자·막힌 칸(S/M 등급 색)·clearance 열지도·플로우 필드 화살표·A* 원 경로와 다듬은 경로·충돌 원·장애물 도형·입구(시작점→진입점)·기준점 ID 를 덮어 그림. 층은 옵션 값으로 고른다(기본 전부). 키 입력으로 바꾸지 않는다 |
| `--preview <sprites\|enemies\|obstacles\|effects>` | 개발 화면. 그림 + 발 기준점 십자 + ID. 배치는 고정(6.13): 정의마다 한 행, 방향·프레임 고정 칸 + 12방향을 도는 칸 하나 |
| `--check` | 불변식 계측(10.1)을 켠다. `--log-ticks` 에 열로, 종료 요약에 최댓값 |
| `--validate-data` | 창 없이 데이터 검사만, 종료 코드 0/2 |
| `--log <파일>` | 로그 파일. 기본: 개발 옵션 실행이면 **현재 폴더** `logs\`, 아니면 저장 폴더 `logs\` |
| `--log-ticks <csv>` | 틱 시간 기록(10.3) |
| `--log-hash <csv>` | 스테이지 틱 60마다 `app_tick, stage_tick, hash`(3.4 `StateHasher`) |
| `--save-dir <폴더>` | 8.3 |

- **사용자 데이터 보호**(brief 7절): 개발 옵션(`--autoplay`·`--stage`·`--capture*`·`--preview`·`--validate-data`·`--stress` 중 하나라도)이 있고 `--save-dir` 가 없으면
  저장 파일을 **읽지도 쓰지도 않고** 기본값으로 시작한다(창 모드, 해금 = 스테이지 1 만, 기록 없음). `--window` 는 저장된 전체 화면 설정보다 늘 우선한다.
- 자동 실행에서 데이터 오류가 나면 DataError 화면을 띄우지 않고 종료 코드 2 로 끝난다(입력을 기다리며 멈추지 않게).
- 종료 코드: 0 정상, 1 처리 안 된 예외(로그에 스택), 2 데이터 오류, 3 요청한 캡처가 하나라도 안 생김, 4 벽시계 시간 초과, 64 옵션 오류.
- 고정 로그 문장(판정용): 첫 줄 재현 정보(3.4), `SCENE <이름> app_tick=…`, `RESULT stage=… outcome=victory|defeat stage_time=… level=… kills=… seed=…`, 끝 줄 `EXIT code=…`.
- 실행 파일은 WinExe 다. **Windows GUI 실행 파일은 셸이 끝나기를 기다리지 않으므로** 종료 코드는 아래처럼 받고, 판정 근거는 로그 파일로 한다.
  데이터 검사의 1차 판정은 창이 필요 없는 `dotnet test` 의 실제 데이터 시험이다.
- 검증 담당의 실행 예(PowerShell, 사용자 화면·입력·저장을 건드리지 않음):

```powershell
$p = Start-Process -FilePath .\DiabloDefenceRemastered.exe -Wait -PassThru -NoNewWindow -ArgumentList @(
  '--autoplay','--seed','7','--stage','stage_courtyard','--window','1280x720',
  '--capture','out\s1.png','--capture-at','30,120','--quit-after','125',
  '--save-dir','out\save','--log','out\run.log','--log-hash','out\h1.csv','--check')
$p.ExitCode
```

---

## 10. 테스트 전략

### 10.1 단위 테스트 (xunit.v3, Core 만, 그래픽 없음)

| 폴더 | 무엇을 | 담당 |
|---|---|---|
| `Common` | `Facing.ToColumn`(남 0, 서 3, 북 6, 동 9 / 16방향), `SimRandom` 재현, `StableHash`, `EntityStore` 핸들(지운 뒤 낡은 핸들은 무효)·압축, 풀 재사용, `AnimClock`(이벤트 프레임 넘김, 재생 배율), `CameraMath`, `TickAccumulator`(틱 상한·버린 틱·캡처 멈춤, `--time-scale` 1 과 8 이 같은 앱 틱 열), `DefRef` 한 번만 채움, `StageStepper`(히트 스톱·늘임·`AwaitingChoice`) | WP1 |
| `Data` | 규칙별 고장 난 작은 JSON(`Fixtures/`)으로 각 오류가 정확한 파일·경로로 나오는지. `extends` 병합·순환, `*_add`·`*_patch`, 모르는 속성, 비슷한 ID 제안, 항목마다 오류 모음, `rules` 하위 키 합치기·중복, **파일 목록을 뒤섞어 읽어도 `GameDefs` 해시가 같다**, 가중치가 JSON 순서 배열 | WP7 |
| `Data`(실제, `Integration`) | 실제 `Data/` 를 엄격 모드로 읽어 오류 0, 그림이 있으면 PNG 크기(없으면 건너뜀). 모든 모듈 등록이 필요하므로 통합 때 기본으로 옮김 | WP7 작성, WP6 통합 |
| `Geometry` | 원·사각형·다각형(오목 포함) 부호 거리, 밀어내기 방향, 선분 교차, 원 중심에 놓인 점 | WP2 |
| `Nav` | clearance 알려진 점 값, 작은 손 격자에서 플로우 방향, A* 최적성(무작위 격자 200개에서 다익스트라와 비용 비교 — **끈 당기기 전** 경로), 코너 자르기 없음, 다듬은 경로가 막힌 곳을 지나지 않음, 목표 못 갈 때 가장 가까운 곳, 시작이 3칸 밖까지 막힘. **두 스테이지 실제 데이터 전수**: (a) 모든 통과 칸에서 `dir` 을 따라가면 칸 수 이하 걸음에 시작점(고리 없음) (b) 모든 통과 칸 중심의 점 유닛이 이중선형 방향으로 10초 안에 주인공 120px 안 (c) 맵 네 모서리·`s1_wall_w` 아래에 주인공을 둬도 `S`·`M` 필드가 모든 통과 칸에서 dist < 65535 (d) 13.5-4~6 | WP2 |
| `Spatial` | 무작위 점 집합에서 해시 질의 = 전수 비교 결과, 결과 순서 결정적, **한 칸에 200개 몰아 넣었을 때 넘침 규칙**(돌려준 개수·반경 줄여 다시) = 전수 | WP2 |
| `Movement` | 두 적 같은 좌표 → 1틱 뒤 둘 다 유한·떨어짐, 20마리 같은 점 행진 → 진입 2초 뒤 모두 유한·겹침 해소, 기둥 중심 소환 → 밖으로 나옴, 입장 중 적은 해시·분리에서 빠짐, 이동 의도 6종, `PlacementResolver`(맵 모서리·기둥 옆 소환 → 모두 통과 칸, 생략 수 결정적) | WP2 |
| `Invariants` | 계측기가 일부러 만든 NaN·맵 밖·장애물 안·멈춘 적을 센다 | WP2 |
| `Combat` | `DamageMath` — 기획 4.2 예(오라+함성 = ×1.55, 반올림 마지막, 방어 후 최소 1, 치명), `damage_pct` 한 번만, 발사 시점 고정, 시간 배율 제외 대상, 등급 배율, 피격 무적, `--god`, 승리 대기 중 주인공 피해 무시, 입장 중 적만 있는 월드에서 매직 미사일이 쏘지 않음·장판 위를 지나도 HP 그대로 | WP3 |
| `Weapons` | `WeaponStats.Resolve`(count+projectile_count, area_pct 는 선언된 키에만, 쿨 상한 0.6/마력 폭주 0.8, **damage 는 damage_pct 와 무관**, `null` = 상시), 조준 5종, 패턴별 발사 결과(발 수·퍼짐각·궤도 위치), 관통·튕김이 같은 적을 두 번 안 때림, 적별 타격 간격 | WP3 |
| `Progression` | 경험치 곡선(1→5, 2→15, 19→185, 20→198, 40→461), 레벨업 후보(가중치·칸 참·초반 보장·채움·3장 미만), 재추첨 수, 상자(진화 우선·중간 보스 3회·강화 없으면 금화), 레벨업 밀어내기(반경 150, 80px) | WP3 |
| `Enemies` | **전수 연기 시험**(6.13), 점화 중 죽으면 폭발 없음, 분열 3마리 일반 등급·튕김 80px(장애물에서 멈춤)·0.3s 대기, 대장 죽음 → **그 대장의 호위만** 공포 1.5s, 호위 없는 대장 죽음 → 주변 해골 공포 없음, `escort` 대형 120·풀림 250·출처별 호위 수(이벤트 값 / 기본 8 / 구간 0), 행진 직진 시간 뒤 보통 행동, `kite` 거리 띠·물러날 수 없으면 제자리 사격, `shoot_projectile` 가리면 안 쏨, 돌진 예고·시간, `ground_hazard` 동시 20 상한, `war_cry` 겹치지 않음(큰 값 하나), 버프 면역(`slow`), `phase_through` 경계만 막힘, `heavy_strike` 넉백, `summon` 상한 30·절대 상한 360, 보스 페이즈 50% 1회·패턴 덮어쓰기·쿨다운이 같은 패턴에 남음, 패턴 `actions` 두 개가 같은 프레임, 접두사 합치기·제외 조건, `attack_cooldown_pct` 가 모든 능력 쿨에, 승리 때 `Collapse`(분열·폭발·드롭 없음) | WP4 |
| `Director` | 누적기(무리 1~3, `spawn_group`, 최대 수에서 3 고정), 생성 위치가 시야 띠 안·걸을 수 있음·여유 거리, 12번 실패 → 먼 입구, 상한 300/360 과 출처별 막는 순서·지우는 순서(`Culled` 는 보상·시체 없음), 포위 고리 반경 줄이기·생략, 정예 종류 뽑기에서 `elite_eligible: false` 제외, `--start-at` 이 지난 이벤트 건너뜀·보스 시각 이후면 보스 바로, **생성 수**: 상한 무한·처치 없음 월드에서 구간마다 `floor(초당 × 구간 길이)` ± 3 | WP4 |
| `Art` | 장애물 굽기: 크기·기준점·발 y, 바닥 모양 밖 투명, 그림자 오프셋, 같은 씨앗 같은 결과, PNG IHDR 읽기 | WP5 |
| `Dev`·`Save` | `DevOptions` 파싱(값 형식, 여러 캡처 시각, 오류 메시지, 개발 옵션이면 저장 안 읽음 판정), `StateHasher` 가 같은 상태에 같은 값, 저장 직렬화·버전 | WP7 |
| `Sim` | **결정성**: 같은 씨앗·자동 조작으로 30초(기본) / 3분(`Scenario`) 두 번의 상태 해시 열이 같다. **묶음 독립성**: `TickAccumulator` 로 프레임당 1틱 / 4틱으로 묶어 `StageStepper` 를 돌려도 해시 열이 같다(레벨업 오버레이 흉내 포함). **할당**: 정상 상태 틱 1,000번 동안 증가 0. `Scenario`: 단계 1 10분(자동 조작 + `--god`, 예외 없음, 불변식 기준 통과). `Perf`: 4.9 표 | WP4 |

- 부품·시스템 테스트는 `TestWorldBuilder`(WP1 제공: 코드로 만든 작은 맵·정의, 전수 공간 질의, 직선 내비)로 만든다. 실제 데이터에 기대는 테스트와 분리한다.
- **불변식 계측 `InvariantChecker`**(Core, WP2): 매 초 센다 — `nan`(유한하지 않은 위치), `out_of_map`(입장 중이 아닌데 맵 밖), `in_obstacle`(통과·입장 중이 아닌데 도형 부호 거리 < −2px),
  `stuck`(입장·공포·기절·`Hold`·독점 행동이 아니고 주인공까지 거리 > 공격 사거리 + 32 인데 3초 동안 8px 미만 이동), `over_cap`(상한 초과).
  기준: `nan = out_of_map = over_cap = 0`, `in_obstacle` 이 같은 적에서 1초 넘게 이어지지 않음, `stuck` 최댓값 ≤ 살아 있는 적의 2%. 시나리오 시험은 늘 켜고, 게임은 `--check` 로 켠다.

### 10.2 실행 캡처로 확인하는 것 (게임 실행, 검증 담당)

결과는 행마다 **확인 / 문제 / 판정 못 함** 셋 중 하나로 적는다. "빌드 통과" 와 "화면 확인" 을 따로 적는다.

| 확인 | 명령 핵심 | 통과 = |
|---|---|---|
| 사용자 화면·데이터 보호 | 아래 모든 실행 | 실행 전후 사용자 `save.json` 수정 시각 같음, 포그라운드 창(`GetForegroundWindow`, 검증 쪽 PowerShell 에서 읽기만) 같음 |
| 타이틀 | `--autoplay --capture-at 1` | 로고 애니메이션·메뉴·한글이 보임(네모 없음) |
| 그림 규칙 | `--preview enemies`, `--preview sprites` | 걷는 방향이 칸의 방향과 맞음, 발 기준점 십자가 발바닥 ±4px, 같은 스테이지 적끼리 구별됨 |
| 장애물 어울림 | `--stage stage_sanctum --capture-at 3` + `--preview obstacles` | 윗면·옆면·그림자 세 부분이 보이고 바닥 모양 테두리 밖 2px 넘게 번지지 않음, 기둥 뒤를 지나는 주인공 윤곽이 보임 |
| 길찾기 | `--stage stage_sanctum --start-at 07:00 --stress 180 --debug-nav flow,shapes --god --check --capture-at 5,20` | 협로로 몰리는 흐름, 큰 유닛 우회, 로그의 `stuck`·`in_obstacle` 기준(10.1) 통과 |
| 전투·손맛 | `--start-at 04:00 --level 15 --weapons …` | 투사체·폭발·피해 숫자·번쩍임이 보임 |
| 레벨업 창 | `--capture-on levelup` | 카드 3장 문구가 `피해 14 → 18` 꼴, 기획 12.2 배치 |
| 보스 | 기획 15절 예 | 패턴 예고·보스 막대·페이즈 |
| 결과 | `--capture-on result --exit-on-result` | 무기별 피해 표, `RESULT` 로그 줄 |
| 재현 | 같은 명령 두 번(한 번은 `--time-scale 8`) | 두 `--log-hash` CSV 같음, 같은 `--capture-at` PNG 바이트 같음 |
| 단계 끝 | `--autoplay --god --seed 1..5 --stage stage_courtyard --exit-on-result` | 단계 1: 다섯 번 모두 `RESULT outcome=victory` 가 10:00 에(생존 승리). 단계 2: `outcome=victory`, `stage_time ≤ 16:00` |

### 10.3 성능 측정

- `TickProfiler`(Core): 시스템 단계별 `Stopwatch` 누적. `--log-ticks perf.csv` 는 1초마다 한 줄:
  `app_s, stage_s, enemies, hero_proj, enemy_proj, hazards, tick_avg_ms, tick_max_ms, <단계별 평균 ms…>, draw_ms, frame_ms, slow_frames, draw_calls, alloc_bytes, dropped_ticks, <--check 열…>`.
  `draw_ms` = `Draw` 시작부터 `Present` 직전까지 CPU 시간, `slow_frames` = 16.7ms 넘은 프레임 수. 종료 때 첫 5초를 뺀 p50/p95/p99/max 를 로그에 요약한다. `draw_calls` 는 `GraphicsDevice.Metrics.DrawCount`.
- 렌더 측정: `--stage stage_sanctum --stress 300 --freeze-ai --vsync off --window 1280x720 --quit-after 30 --log-ticks perf.csv`. 결과에 "백그라운드 창에서 잼" 을 적는다.
- 그래픽 없는 기준 측정과 합격 기준은 4.9 표 하나다.

---

## 11. 구현 작업 묶음 (Work Package)

- **WP1 이 먼저 혼자** 계약과 뼈대를 끝낸다. 그 뒤 **WP2~WP7 은 서로 파일이 겹치지 않고 동시에** 간다. 통합(실제 데이터 엄격 시험, `PlayScene` 완성, 단계 끝 실행)은 WP6 이 맡는다.
- 각 묶음은 기획 14.1 의 **단계 1(세로 조각)** 범위를 먼저 끝내고, 단계 2·3 내용을 같은 묶음에서 이어 간다. 완료 기준은 단계 1 기준이며, 단계 2·3 은 같은 기준에 내용만 늘린다.
- 맡는 파일은 "맡는 것" 이 전부다. 다른 묶음의 파일을 고쳐야 하면 그 묶음 담당에게 요청한다. 계약(WP1 파일) 변경은 11.3.

```
WP1 (뼈대·계약) ──┬── WP2 내비·충돌·공간 ────────────┐
                  ├── WP3 주인공·전투·무기·성장 ──────┤
                  ├── WP4 적·감독·자동 조작 ──────────┼── 통합(WP6) ── 검증
                  ├── WP5 렌더·그림 굽기 ─────────────┤
                  ├── WP6 화면·UI·입력 ───────────────┤
                  └── WP7 앱 셸·데이터 로더·개발 도구 ┘
```

### 11.1 묶음별 맡는 것·계약·완료 기준

#### WP1 — 솔루션 뼈대·공통 계약 (먼저, 혼자)

- **맡는 것**: `DiabloDefenceRemastered.sln`, `global.json`, `Directory.Build.props`, 세 csproj(자원 검사 DDR0001·복사 대상·xunit.v3 설정 포함),
  Core `Common/`, `Defs/`(모든 정의 record — 6절 스키마 그대로. 부품 매개변수 record 는 제외), `Data/Registry/`, `Sim/Contracts/`, `Sim/Entities/`(공통 조각), `Sim/World.cs`, `Sim/Simulation.cs`, `Sim/StageStepper.cs`,
  `Sim/GameModules.cs`(모듈 목록 한 곳), `Sim/Core/BeginTickSystem.cs`·`EndTickSystem.cs`, 각 묶음의 **빈 모듈 파일**(`Nav/NavModule.cs`, `Sim/Weapons/CombatModule.cs`, `Sim/Enemies/EnemyModule.cs`,
  `Art/ArtModule.cs`, `Data/Loading/LoadingModule.cs` — `Register(ModuleContext)` 빈 본문. 만든 뒤 소유는 해당 묶음),
  게임 `App/Contracts/`(+ `AppServices`: 정의·로그·저장·개발 옵션·그림 서비스 묶음), 게임 쪽 **빈 연결 파일** `Scenes/SceneFactory.cs`(시작 씬·DataError 씬 만들기 → WP6 소유로 넘김)·`Rendering/RenderServices.cs`(텍스처·글꼴·렌더러 만들기 → WP5 소유로 넘김),
  테스트 `TestKit/`·`Common/`. 저장소 뿌리의 `DiabloDefenceRemastered/obj/` 정리.
  → `GameApp`(WP7)은 이 두 연결 파일과 `GameModules` 만 부르므로 WP5·WP6·WP7 이 서로의 파일을 고치지 않고 붙는다.
- **계약(다른 묶음이 기대는 것)** — 필드 단위 정본은 11.2 표:
  - **`World`**(공통 조각), **`ISimSystem`/`SimPhase`(16단계)/`SimulationBuilder`**(한 단계 = 시스템 하나), **`StageStepper`**, **`EntityId`/`EntityStore<T>`**(밀집 인덱스·압축 규칙),
    **`Enemy`/`Hero`/`Projectile`/`Hazard`/`Telegraph`/`Pickup` 공통 조각**(partial class. 풀 재사용 `Reset()` 은 공통 필드를 지우고 각 조각의 `partial void Reset…()` 을 부른다)
  - **`DamageRequest`**, **`HitLanded`**, **`KillCause`**, **`Faction`**, **`MoveIntent`/`MoveKind`**, **`EnemyVisual`**, **`SpawnContext`/`SpawnSource`**, **`BuffSlots`·버프 붙이기 도우미**
  - **틱 이벤트 형**(`EnemyKilled`, `HitLanded`, `HeroDamaged`, `XpGained`, `LevelUpReady`, `BossDefeated` …, 두 칸 큐), **`PresentationBuffer`/표시 이벤트 형**(모두 `Tick` 포함: `Effect`, `Damage`, `CorpseSpawned`, `Shake`, `Banner` …)
  - **질의·생성 인터페이스**: `INavWorld`(clearance·`HasClearLine`·`ProjectileLineClear`·맵 안), `ISpatialQuery`, `IHostileQuery`, `ICombatSpawner`(적 투사체·장판·예고), `IProgression`(+ 카드 형), `RunStats`, `IRunSetup`
  - **`PlayerCommand`/`IInputSource`**, **모든 정의 record + `DefRef<T>` + `StatKey`/`EnemyStatKey`**, **`PartRegistry`/`PartKind`/`PartDef`/`IPartFactory`/`IDataValidator`/`ValidationContext`/`DataError`/`GameDefs`**
  - **공통 도우미**: `AnimClock`, `CameraMath.ViewRect(heroPos, mapSize, viewSize)`, `TickAccumulator`, `StableHash`, `MathX.TieAngle`, `Facing`, `Int2`, `RectI`
  - **게임 쪽**: `IScene`/`SceneStack`/`RenderContext`(`ScreenToWorld(Point)` 포함)/`InputFrame`
  - **`TestKit`**: `TestWorldBuilder`(코드로 맵·정의·개체), `BruteForceSpatial : ISpatialQuery`, `StraightNav : INavWorld`(장애물 없는 직선)
- **완료 기준**: `dotnet build`(그림 있음) 통과, 그림 없을 때 DDR0001 오류 문장. `dotnet test` 통과·**발견된 테스트 수 > 0**
  (`Common`, `TestKit` 으로 빈 월드 1,000틱, 16단계 순서·한 단계에 시스템 둘을 등록하면 예외). 게임 프로젝트는 빈 `GameApp` 으로 빌드만(창·옵션은 WP7).
  계약 파일에 이 문서(3.5, 4.4, 4.8, 5.1, 6.2, 6.6, 11.2)의 시그니처가 빠짐없이 있다.

#### WP2 — 길찾기·충돌·공간

- **맡는 것**: Core `Geometry/`, `Nav/`(`Navigator`, `PlacementResolver`, `NavConst`, `Validation/` = 13.5-4,5,6 검사기, `NavModule.cs`), `Spatial/`, `Sim/Movement/`(+ `Enemy.Nav.cs` 조각, `Invariants/`).
  테스트 `Geometry/ Nav/ Spatial/ Movement/ Invariants/`.
- **계약**: `NavWorld.Build(MapDef, StageDef) : INavWorld`, `SpatialHash : ISpatialQuery`, `ObstacleIndex`, `Navigation / Locomotion / Collision / SpatialRebuild` 시스템,
  이동 의도 → 희망 속도(4.4 끝), `Enemy.MoveBlockedTime`·`Hero.TouchingCount` 쓰기, `ResolvedObstacles`(맵 + 스테이지, 기준점 풀기 — WP5 굽기·`--debug-nav` 가 읽음. 필드·경로·clearance 읽기 API 포함), `InvariantChecker`.
- **완료 기준**: 테스트 통과(무작위 A* 최적성, 해시 = 전수·넘침, 두 스테이지 길 검사·전수 성질, 겹친 점). 그래픽 없는 측정: 적 500 을 맵에 흩어 플로우로 주인공에게 모으는 3,600틱이
  Navigation+Locomotion+Collision 평균 ≤ 1.0ms(4.9 표), 불변식 기준 통과.

#### WP3 — 주인공·전투·무기·성장

- **맡는 것**: Core `Sim/Hero/`, `Sim/Combat/`, `Sim/Weapons/`(`IFirePattern`·`ITargeting`·`WeaponStats`·`CombatModule.cs`), `Sim/Projectiles/`, `Sim/Hazards/`, `Sim/Pickups/`, `Sim/Progression/`,
  이 폴더들의 조각 파일(`Enemy.Combat.cs`, `Hero.Combat.cs`, `World.Combat.cs` …).
  Data `weapons/`, `passives.json`, `evolutions.json`, `pickups.json`, `drop_tables.json`, `rules/hero.json`. 테스트 `Combat/ Weapons/ Progression/ Hero/`.
- **계약**: 발사 패턴 6종·조준 5종·무기 효과 부품 등록, `DamageMath`, `HeroControl`·`Weapons`·`Projectiles`·`Hazards`·`DamageResolve`·`DeathRewards`·`Pickups` 시스템,
  `ICombatSpawner`·`IProgression`·`IHostileQuery`(MVP: 주인공만) 구현, `RunStats` 채우기, `IRunSetup`(`--god`·`--level`·`--weapons`·`--passives`).
- **완료 기준**: 테스트 통과. 그래픽 없는 시나리오(`Scenario`): 해골 50 마리 + 매직 미사일 Lv8 로 60초 → 처치·경험치·레벨업 대기가 생김(`TestKit` 공간 질의로 먼저, WP2 가 들어오면 실제 해시로도).

#### WP4 — 적·감독·자동 조작

- **맡는 것**: Core `Sim/Enemies/`(생애 상태·부품 계약과 구현·보스 패턴·페이즈·등급·접두사·버프 적용·`EnemyFactory`·`EnemyModule.cs`·`Enemy.Behavior.cs` 조각), `Sim/Director/`(+ `World.Director.cs` 조각), `Sim/Autoplay/`.
  Data `enemies/`, `affixes.json`, `buffs.json`, `stages/`, `rules/enemies.json`. 테스트 `Enemies/ Director/ Sim/`(결정성·시나리오·Perf), `Fixtures/variants/`, `Fixtures/perf_scenario.json`.
- **계약**: 부품 등록(일반 12 + 동작 7 + `TriggeredAction`·`PatternAbility`·`PhaseController`), 이벤트 종류 9개(`PartKind.StageEvent`), `Director`·`EnemyThink`·`DeathHooks` 시스템, 승리 처리(3.5),
  `World.ViewRect` 를 쓰는 생성 위치, `AutoplayPolicy`(`Move(World) → Vector2`, `PickCard(offer) → int` — 메뉴 넘김은 WP6), `IRunSetup`(`--start-at`·`--stress`·`--freeze-ai`), `Visual`·`CorpseSpawned` 값 채우기,
  `World.Lighting`·`BossBars`·`Banner` 쓰기.
- **완료 기준**: 테스트 통과(부품·생성기·전수 연기 시험·변형 픽스처·결정성 30초). 단계 1: 적 5종과 stage_courtyard 시간표가 10분 돈다
  (`Scenario`: 자동 조작 + `--god`, 예외 없음, 불변식 기준, 상한 무한 월드의 생성 수 기준 — 10.1 `Director`). 단계 3 에 `bone_brute`·`hell_goat` 포함.

#### WP5 — 렌더·그림 굽기

- **맡는 것**: Core `Art/`(`ObstacleBaker`, `PixelBuffer`, `PngHeader`, `ArtModule.cs` = PNG 크기 검사기 등록), 게임 `Loading/`(텍스처·색 키·커서·글꼴), `Rendering/`(가상 화면·카메라·층·y 정렬·빛·장애물 스프라이트·표시 이펙트·시체·피해 숫자·`DebugNavOverlay`),
  `Scenes/PreviewScene.cs`, `Rendering/RenderServices.cs`. Data `sprites/`, `effects/common.json`, `maps/`, `obstacle_kinds.json`(좌표 맞추기 포함). 테스트 `Art/`.
- **계약**: `WorldRenderer.Draw(World, RenderContext, float alpha)`, `FxSystem.Consume(PresentationBuffer, int stageTick)`, `TextureStore.Get(spriteId)`, `UiCanvas`(가상 좌표 → 창 좌표, 글꼴 배율),
  `RenderContext.ScreenToWorld` 구현 — WP6 이 HUD·마우스 이동에 쓴다. 기획 열린 질문 7 의 좌표·기준점은 이 묶음이 `--preview`·`--debug-nav` 로 맞춰 `maps/dungeon1.json`·`sprites/` 에 반영.
- **완료 기준**: `Art` 테스트 통과. `--preview sprites/enemies/obstacles` 캡처를 10.2 의 "통과 =" 문장으로 검증 담당이 판정(못 한 것은 "판정 못 함"). `--stress 300 --freeze-ai` 장면에서 4.9 표의 렌더 기준.

#### WP6 — 화면·UI·입력, 통합

- **맡는 것**: 게임 `Scenes/`(Preview 제외, `SceneFactory.cs`·`AutoplayDriver` 포함), `UI/`, `Input/`. 통합 때 `PlayScene` 완성과 `Integration` 시험을 기본 분류로 옮기는 일(시험 파일의 분류 표시만 고침 — 내용은 WP7 과 합의).
- **계약**: `KeyboardMouseSource : IInputSource`(WASD·방향키·왼쪽 버튼 이동, 기획 3절 키), 씬 9개, HUD, 레벨업·상자·일시정지·설정·결과, 저장 연동(WP7 `SaveStore`), 자동 조작 메뉴 넘김(`AutoStep`),
  `SCENE`·`RESULT` 로그 문장, 레벨업 오버레이를 같은 앱 틱에 올림(3.3).
- **완료 기준**: `--autoplay --seed 1 --quit-after 30` 이 타이틀 → 선택 → 플레이까지 사람 입력 없이 감(`SCENE` 로그 세 줄). 캡처 4장(타이틀, 플레이 HUD, 레벨업 창, 결과)을 10.2 문장으로 판정.
  통합 뒤: 실제 데이터 엄격 시험 통과, 10.2 "단계 끝" 행 통과. 키보드로 한 판(사람)은 "동작 확인 못 함" 으로 따로 적는다.

#### WP7 — 앱 셸·데이터 로더·개발 도구

- **맡는 것**: Core `Data/Loading/`(로더·병합·변환기·공통 검증 규칙 13.5-1,2,3,7(정의끼리),8,9 와 6.14 추가 규칙, `LoadingModule.cs`), `Dev/`(`DevOptions`, `TickProfiler`, `StateHasher`), `Save/`,
  게임 `Program.cs`, `GameApp.cs`(합성 뿌리), `App/`(Contracts 제외: `FrameLoop`, `DevRunner`, 캡처, 종료·감시, `Log`, `WindowControl`, `SaveStore`, 빈 시험 씬 `BlankScene`).
  테스트 `Data/ Dev/ Save/`, `Fixtures/data/`(고장 난 JSON).
- **계약**: `DataLoader.Load(dir, registry, validators, options) → (GameDefs, errors)`, `--validate-data`, `FrameLoop`(3.2, `TickAccumulator` 사용), `DevRunner`(캡처·`--quit-after`·`--timeout`·`--exit-on-result`·종료 코드),
  9절 옵션 파싱, 저장 보호 규칙(9절), 로그 첫 줄·`EXIT` 줄, `GraphicsProfile.HiDef`, SDL 환경 변수.
- **완료 기준**: 테스트 통과(로더 규칙·뒤섞은 파일 순서·옵션 파싱). 게임 실행: 빈 씬으로 창이 뜨고 `--quit-after 2 --capture x.png --capture-at 1` 이 PNG 를 쓰고 종료 코드 0(`Start-Process -Wait -PassThru`),
  가장 큰 시트 크기(1920×2560, 550×3240)의 텍스처 만들기 성공(`HiDef` 확인), 개발 옵션 실행 전후 사용자 `save.json` 시각 같음, `--background` 실행 전후 포그라운드 창 같음.
  최소화·숨긴 창에서도 `--capture-at` PNG 가 생기는지 확인해 결과를 R8 에 적는다.

### 11.2 공용 형의 필드와 소유 (partial 조각)

`World`·개체 클래스는 `partial class` 다. 공통 조각(WP1)은 여러 묶음이 **읽거나 쓰는** 필드만 가지고, 한 묶음만 쓰는 상태는 그 묶음 폴더의 조각 파일에 둔다
→ 묶음이 자기 상태를 늘릴 때 WP1 을 거치지 않는다. 옆 사전(`Dictionary<EntityId, …>`)으로 상태를 붙이지 않는다(결정성·할당 0).
`Enemy` 는 **클래스**(풀 재사용)다. 같은 어셈블리라 조각은 `partial` 로 합쳐진다.

| 형 | 필드 | 쓰는 묶음 | 읽는 묶음 | 파일 |
|---|---|---|---|---|
| `Enemy` | `Id`, `Def`, `Faction`, `Rank`, `AffixIdx[2]`, `SpawnTick`, `Life`(`Entering`/`Emerging`/`Active`/`Stunned`) | WP4 | 모두 | `Sim/Entities/Enemy.cs` (WP1) |
| | `Pos`, `PrevPos`, `Vel`, `Radius` | WP2(이동), WP4(생성·순간 이동) | 모두 | 〃 |
| | `Hp`, `MaxHp`, `Dead`, `KillCause` | WP3(피해), WP4(생성·`Collapse`·`Culled`) | 모두 | 〃 |
| | `Armor`, `KnockbackResist`, `Immunities`, `DamageMul` | WP4(생성·버프) | WP3 | 〃 |
| | `Intent`(`MoveIntent`), `MoveSpeed`, `PassThroughObstacles` | WP4 | WP2 | 〃 |
| | `MoveBlockedTime` | WP2 | WP4 | 〃 |
| | `KnockbackVel`, `KnockbackTime`, `HitFlashTime` | WP3 | WP2, WP5 | 〃 |
| | `Facing`, `Anim`(`AnimClock` 상태), `Visual`(`EnemyVisual`) | WP4 | WP5 | 〃 |
| | `Buffs`(`BuffSlots`) | WP3(장판 둔화), WP4(오라·함성) — 공통 도우미로만 | WP4 | 〃 |
| | `Nav`(`NavAgentState`) | WP2 | — | `Sim/Movement/Enemy.Nav.cs` |
| | `NextHitBySlot[4]` | WP3 | — | `Sim/Combat/Enemy.Combat.cs` |
| | `Parts`(`PartSlots`), `Action`(`ActionState`), `PhaseIdx`, `Order`(`MoveOrder`), `Leader`, `Summoner`, `Source`, `EnrageAt` | WP4 | — | `Sim/Enemies/Enemy.Behavior.cs` |
| `Hero` | `Pos`, `PrevPos`, `Vel`, `Radius` | WP2(이동), WP3(시작 위치) | 모두 | `Sim/Entities/Hero.cs` (WP1) |
| | `Facing`, `Anim`, `Hp`, `MaxHp`, `Dead`, `InvulnTime`, `HitFlashTime`, `Intent`, `MoveSpeed` | WP3 | 모두 | 〃 |
| | `Stats`(능력치 합), `Weapons`(칸 4), `Passives` | WP3 | WP4(자동 조작), WP6(HUD) | 〃 |
| | `TouchingCount` | WP2 | WP3 | 〃 |
| | 재생·무기 내부 상태 | WP3 | — | `Sim/Hero/Hero.Combat.cs` |
| `Projectile`·`Hazard`·`Telegraph`·`Pickup` | 정의·진영·위치·이전 위치·반경·나이·남은 시간·진행 비율·출처 적 ID | WP3(WP4 는 `ICombatSpawner` 로 생성만) | WP4(자기가 만든 것 세기), WP5(그리기) | `Sim/Entities/*.cs` (WP1) |
| | 맞은 목록·궤적 상태·피해 값 | WP3 | — | `Sim/Projectiles/…` 등 조각 |
| `World` | `Defs`, `Stage`, `Map`, `Rng`, `Tick`, `Dt`, 저장소 5개, `Hero`, `Events`, `Presentation`, `Damage`(요청 큐), `Outcome`, `AwaitingChoice`, `ViewRect`, `FreezeTicks`, `Seed` | WP1(틀), 각 단계 | 모두 | `Sim/World.cs` (WP1) |
| | `Nav : INavWorld`, `Spatial : ISpatialQuery` | WP2(모듈이 넣음) | 모두 | 〃 |
| | `Hostiles : IHostileQuery`, `Spawner : ICombatSpawner`, `Progression : IProgression`, `RunStats` | WP3(모듈이 넣음) | WP4, WP6 | 〃 |
| | `Lighting`(현재 조명: 주변광·어둠·종류별 빛 덮어쓰기), `BossBars`, `Banner` | WP4 | WP5, WP6 | 〃 |
| | 감독 상태(누적기·다음 무리·정예 판정·이벤트 위치) | WP4 | — | `Sim/Director/World.Director.cs` |
| | 무기·성장 내부 상태 | WP3 | — | `Sim/Weapons/World.Combat.cs` |

### 11.3 묶음 사이 순서·충돌 방지

- WP1 이 끝나기 전에는 다른 묶음이 코드를 쓰지 않는다. WP1 은 계약을 이 문서 그대로 적는다. 이 문서에 없는 한 묶음 상태는 그 묶음이 자기 조각에 둔다.
- 계약(WP1 파일) 형을 바꿔야 하면 메인 세션이 바꾸고 알린다. 다른 묶음은 계약 파일을 직접 고치지 않는다. 자기 조각 파일에 필드를 더하는 것은 자유다.
- 한 묶음만 구현·사용하는 형은 그 묶음 소유다(`IFirePattern`·`ITargeting`·`WeaponStats` → WP3, 부품 계약·`PartSlot`·`ActionState`·`MoveOrder` → WP4, `Navigator`·`NavAgentState` → WP2). WP1 은 레지스트리 등록 지점만 가진다.
- 부품 매개변수 record 와 그 JSON 이름은 부품을 만드는 묶음이 정하고, 같은 묶음이 그 데이터 파일을 쓴다. WP1 은 실제 `Data/` 를 쓰지 않는다.
  그래서 다른 묶음이 정한 매개변수 이름과 데이터가 어긋나 `Disallow` 오류가 한꺼번에 터지는 일이 없다.
- `rules` 는 파일을 나눠 쓴다(6.1): WP3 `rules/hero.json`, WP4 `rules/enemies.json`. 데이터 파일 하나를 두 묶음이 고치는 일이 없다.
- 스테이지 파일(`stages/`, WP4)의 장애물 좌표를 맞춰야 하면 WP5 가 값을 알려 주고 WP4 가 고친다. 맵(`maps/`)은 WP5 가 직접 고친다.
- `SimPhase` 순서표(3.5)는 WP1 소유. 단계를 더하려면 요청.
- 개발 옵션은 WP7 이 파싱만 하고, 적용은 `IRunSetup` 을 등록한 묶음이 한다(WP3: 주인공 쪽, WP4: 시각·부하·AI 멈춤).

---

## 12. 위험과 대응

| # | 위험 | 영향 | 대응 |
|---|---|---|---|
| R1 | 방향 열 규칙·발 기준점·맵 좌표가 추정(기획 열린 질문 7) | 유닛이 엉뚱한 방향·위치로 그려짐, 충돌과 그림이 어긋남 | `Facing` 한 함수·`origin` 데이터·`maps/dungeon1.json` 한 곳에 모음. `--preview`·`--debug-nav` 캡처로 WP5 가 단계 1 안에 맞춤 |
| R2 | 플로우 필드 무리가 협로·장애물 모서리에서 뭉쳐 멈춤 | 스테이지 2 길목이 막혀 재미·난이도가 틀어짐 | 분리 가중·가속 제한·끼임 감지(4.3), 이중선형 방향. `--debug-nav` 로 7:00 협로 캡처를 단계 3 완료 조건에 넣음 |
| R3 | 큰 유닛(반경 26~34)이 좁은 틈에서 비빔 | 보스가 안 옴 | A* 는 자기 반경 clearance, 목표 못 가면 가장 가까운 곳, 검증 13.5-5 를 반경 등급별로 |
| R4 | 결정성 깨짐(사전 순회, 벽시계, 프레임 묶음에 의존한 입력·표시 난수, 데이터 파일 순서) | 재현 안 되는 버그, 캡처 비교 실패 | 3.4 규칙 + `Sim` 결정성(30초 판)·묶음 독립성·파일 순서 뒤섞기 시험을 기본 시험으로, `--log-hash` 로 게임 실행도 비교 |
| R5 | y 정렬 때문에 그리기 호출 수백 번 | 렌더 예산 초과 | 10.3 로 측정. 넘치면: 색 키 그림은 알파가 0/1 뿐이라 **알파 테스트(AlphaTestEffect) + 깊이 버퍼**로 불투명 유닛을 텍스처별로 묶어 그리고, 반투명(망령·사라짐)만 정렬 |
| R6 | 셰이더 없이 번쩍임·실루엣이 약해 보임 | 손맛 부족 | 가산 덧그리기 2회까지 허용. 그래도 부족하면 mgfxc 로 미리 컴파일한 효과 파일(`.mgfx`)을 저장소에 넣는 길을 따로 검토(빌드에 도구 추가 필요) |
| R7 | 텍스처 VRAM ~200MB, 시작 때 PNG 해독 시간(주 스레드에서 차례로) | 느린 시작, 낮은 사양 PC | WP7 이 시작 시간을 잰다. 느리면 StbImageSharp 작업 스레드 해독 + 주 스레드 `SetData`(7.5). 그다음은 시트 빈 여백을 잘라 내는 전처리(정의 `trim`) — 단계 2 이후 측정 보고 |
| R8 | SDL 힌트(환경 변수)가 이 MonoGame 의 SDL 판에서 안 먹어 창이 앞으로 옴, 최소화·가린 창에서 `Draw` 가 안 불림 | 검증 실행이 사용자 화면을 뺏음, 캡처가 안 생김 | WP7 이 첫 실행에서 포그라운드 창 비교·최소화 캡처로 확인. 안 되면 숨긴 창(`SDL_WINDOW_HIDDEN`)으로 시작 + 렌더 타깃 캡처로 바꾸고 결과를 이 칸에 적음. 캡처 크기는 `--window` 로 고정이라 창 상태와 무관 |
| R9 | 묶음 병렬 작업 중 계약 변경 | 빌드 깨짐·되돌리기 | WP1 계약을 먼저 고정, 변경은 메인 세션 경유(11.3). 한 묶음 상태는 자기 partial 조각에 두므로 계약 변경이 드물다(11.2). 각 묶음은 `TestKit` 으로 다른 묶음 없이 시험 |
| R10 | 기획 수치·정의가 데이터와 어긋남 | 조용히 다른 밸런스 | 데이터가 정본, 기획서 표와 맞추는 테스트는 핵심 몇 개만(경험치 곡선, 매직 미사일 표). 수치 변경은 데이터 + 기획서 함께 |
| R11 | 표시 쪽 시체·이펙트를 시뮬레이션 밖에 둔 결정 | 나중에 "시체로 소환" 같은 규칙이 생기면 옮겨야 함 | 그때 `Corpse` 저장소를 Core 로 옮긴다(표시 이벤트 형은 그대로). 지금 규칙에는 없음 |
| R12 | 한글 글꼴이 없는 PC | 글자 네모 | 8.2 대체 순서 + 로그 경고 |
| R13 | `Assets` 없는 클론 | 빌드 실패 | DDR0001 문장 + `SkipAssetCheck`, 테스트는 그림 없이 통과 |
| R14 | E 단계 아군(수호 검사·구출 이벤트)을 넣을 때 계약 변경 | WP2·WP3·WP4 계약을 함께 고쳐야 함 | 지금 `Faction`·`IHostileQuery` 자리만 둠(5.1). 넣을 때 바뀌는 것: `Ally` 저장소·공통 조각, `IHostileQuery` 가 아군 해시도 봄, 적 투사체 맞음 판정 대상 목록, `DamageResolve` 의 아군 체력·죽음, 아군 `astar` 이동 의도, 상호작용("곁에 3초") 이벤트 종류 1개 |
| R15 | partial 조각에 필드를 아무 데나 둠 | 소유가 흐려져 같은 필드를 두 묶음이 씀 | 11.2 표에 없는 공용 필드는 계약 변경 절차(11.3). 각 묶음 조각에는 그 묶음만 쓰는 필드만 |
| R16 | 몬스터를 늘렸는데 색이 비슷해 구별이 안 되거나 어느 스테이지에도 안 나옴 | 사용자 요구 8 이 화면에서 안 보임 | 6.14 경고 두 개 + `--preview enemies` 고정 배치 캡처를 검증 담당이 판정(10.2) |

### 12.1 기획서와 다르게 해석·보탠 것

| 자리 | 기획서 | 설계 | 이유 |
|---|---|---|---|
| 15절 `--time-scale` | (v0.2) "화면 한 장마다 고정 틱을 n 번" | 실제 시간 n 배속(60Hz 화면에서는 같은 뜻). 기획 v0.3 에서 문구를 맞춤 | 화면 주사율이 달라도 같은 속도. 결과는 어느 쪽이든 같다(결정성) |
| 12.6 피해 숫자·처치 연출, 8.1 시체 | 규칙처럼 적힘 | 표시 쪽(게임) 처리 | 규칙 영향 없음, 시뮬레이션 단순화. 시간은 스테이지 틱으로 계산해 재현됨 |
| 10.5 조명 두 단계 | 주변광 곱하기 + 어둠 덮고 뚫기 | 빛 지도 한 장 곱하기 | 같은 결과를 한 번에. 빛이 겹쳐도 1 을 넘지 않는 차이 |
| 1절 카메라 | 따라가다 경계에서 멈춤 | 지연 없이 따라감 | 생성 띠와 화면을 정확히 맞추기 위해 |
| 8.1 정예 길찾기 | 플로우 필드 | 플로우 필드 2등급(S/M) | 몸 반경 20~26 정예가 좁은 칸에 끼지 않게 |
| 13.5-5 길 검사 반경 | 14 / 26 / 34 | 14 / 16·26(필드 문턱 `NavConst`) / 34 | 무리 필드 문턱(16)과 검사 반경이 어긋나 "검사 통과·실제로 못 감" 띠가 생기지 않게 |
| 9.4 전투 함성 버프 | "버프 이름 `war_cry`" | `buffs` 정의 `war_cry` 하나를 타락한 검사·기사 대장이 같이 씀 | 같은 버프는 큰 값 하나만 남는 규칙으로 "겹치지 않음" 이 저절로 됨 |
| 2.4 무리 크기 | 1~3 | 종류를 먼저 뽑고, 그 종류에 `spawn_group` 이 있으면 그 범위 | `hell_goat` 무리(5~7) |
| 데이터 | — | `extends`, `*_add`/`*_patch`, `spawn_group`, `base_radius`, `elite_eligible`, `buffs` | 몬스터 변형을 정의 몇 줄로(6.13) |
| 열린 질문 1·3·4 | 설계에서 정할 것 | 960×540 유지 / `%LOCALAPPDATA%` 의 save.json / JSON | 7.1, 8.3, 6.1 |

---

## 13. 리뷰 반영 (v0.2)

세 리뷰의 지적마다 처리를 한 줄로 적는다. 높음·중간은 모두 반영했고, 일부만 반영하거나 보류한 것은 이유를 적었다.

### 13.1 기획 담당 (`docs/reviews/architecture-planner.md`)

| 지적 | 처리 | 내용 / 이유 |
|---|---|---|
| 높음 H1 인스턴스 행동 지시·생성 맥락이 계약에 없음 | 반영 | `SpawnContext`(출처·주인·호위 수), `Enemy` 의 `Order`(March/Follow/Flee)·`Leader`·`Summoner`(6.6). 공포는 `escort` 규칙으로 옮겨 `Leader == 죽은 대장` 만. 상한 정리 순서는 출처 우선순위(6.6) |
| 중간 M1 패턴 하나에 동작 여러 개 | 반영 | `actions: [ … ]` 를 같은 이벤트 프레임에 순서대로, 여러 틱 동작은 마지막만(6.5·6.6). 단수 `action` 줄임은 두지 않음 — 모양이 하나여야 `actions_patch` 가 단순 |
| 중간 M2 승리 때 무너짐·처치 이유 | 반영 | `KillCause { Damage, Collapse, Culled }`, 승리 처리 순서(3.5), 처치 수에 안 셈(기획 2.2 v0.3) |
| 중간 M3 버프가 값 하나 | 반영 | 데이터 종류 `buffs`, `BuffSlots` 6칸(정의 번호·끝 틱·배율), 오라는 2틱마다 다시 붙임, `debuff` 면역(6.7) |
| 중간 M4 적 능력치 키 목록 | 반영 | `EnemyStatKey` 6개, 없는 키는 오류, `attack_cooldown_pct` 는 모든 능력·패턴 쿨에(6.7, `DamageMath.EnemyCooldown`) |
| 중간 M5 `radius` 최종값만 받음 | 반영 | `sprite_sets.base_radius`, `radius` 생략 시 `round(base × scale)`, 차이 > 2 경고(6.5). 기획 8.1 v0.3 문구 맞춤 |
| 중간 M6 겉모습 상태 자리 없음 | 반영 | `EnemyVisual`(6.6) — 렌더는 이것만 읽음. 접두사 발광 > 등급 발광·주기는 등급. `CorpseSpawned` 에 알파 |
| 중간 M7 아군 자리 없음 | 일부 반영 | `Faction`·`IHostileQuery`(MVP 는 주인공만)를 지금 계약에 둠(5.1). 아군 저장소·체력·구출 상호작용은 E 범위라 바뀌는 계약 목록만 R14 에 |
| 중간 M8 "상시"·"—" 표현 | 반영 | 구현 리뷰 M10 과 합쳐 `float?` 열 + 패턴의 `NullableKeys`, null 의 뜻·단조성 규칙(5.3·6.14). 문자열 `"always"` 대신 `null` — 숫자 열에 문자열이 섞이지 않게 |
| 낮음 L1 광폭화·등장 연출 자리 | 반영 | `enrage`·`arrival`, 이벤트가 덮어씀(6.5) |
| 낮음 L2 레벨업 밀어내기 | 반영 | 반경 150·80px, 피해 0 넉백 요청(3.3). 기획 12.6 v0.3 |
| 낮음 L3 정예 기본값·제외·접두사 조건 | 반영 | `rules.ranks` 의 `pack_size`·`unique_chance`·`banner`, `elite_eligible`, 접두사 `exclude_tags`·`exclude_parts`(6.5·6.7) |
| 낮음 L4 상자 강화 횟수 | 반영 | `elite_chest` 의 `value` = 강화 횟수, 중간 보스 드롭 표 예(6.9) |
| 낮음 L5 진화 `override` 범위 | 반영 | 레벨 표 키·`base` 키·`projectile`·이펙트 ID, `MaxedAtTick`, `on_hit`/`on_explode`/`on_crit` 목록(5.3·6.8) |
| 낮음 L6 카드 문구·결과 값 | 반영 | `stat_labels` 에 `StatKey` 라벨과 `pct`/`abs`, 아이콘 `scale`, `AcquiredTick`(5.3·6.8·6.10) |
| 낮음 L7 기둥 빛·빛 바꾸기 | 반영 | `kind_lights` 가 빛을 새로 붙이고 `pulse` 를 받음, `light_change` 는 경로로 대상 선택(6.12) |
| 낮음 L8 배열 통째 교체 | 반영 | 부품·패턴·페이즈·동작에 선택 `id`, `*_patch`(6.5) |
| 낮음 L9 부품 이름 중복 | 반영 | `ground_hazard` 는 `EnemyAction` 으로 한 번 등록 + 훅 어댑터(6.6), `phase` → `phase_through`(기획 8.3·13.3 v0.3) |
| 낮음 L10 적 투사체 붉은 테 | 반영 | 진영을 보고 렌더가 고름(5.4·7.5) |
| 낮음 L11 승리 조건 데이터 | 반영 | `victory: boss / survive`(6.12), 단계 1 은 `survive 10:00` |
| 낮음 L12 공격 애니 속도와 쿨 | 반영 | 쿨은 공격 시작부터, 짧으면 클립을 빠르게, `anim_speed`(6.5). 기획 8.1 v0.3 |

### 13.2 구현 담당 (`docs/reviews/architecture-implementer.md`)

| 지적 | 처리 | 내용 / 이유 |
|---|---|---|
| 높음 H1 공용 형 필드 미정·WP1 병목 | 반영 | partial 조각 + 필드 소유 표(11.2), `Enemy` 는 클래스·`Reset` 규칙 |
| 높음 H2 적 이동에 주인 없음 | 반영 | 이동 의도 `MoveIntent` → WP2 `Navigator`(4.4 끝). 제안의 `Steer` 를 WP4 가 부르는 대신 WP4 는 의도만 쓰고 WP2 가 `Locomotion` 에서 실행 — 부르는 방향이 한쪽뿐이라 두 묶음을 따로 시험하기 쉬움 |
| 높음 H3 `damage_pct` 이중 적용 | 반영 | `HeroToEnemy` 한 곳, `Resolve` 는 원값, 발사 시점 복사, 시험 한 줄(5.2·5.3) |
| 높음 H4 표시 이벤트·난수가 프레임 단위 | 반영 | 표시 이벤트에 `Tick`, 나이 = 현재 − 생긴 틱, 프레임 끝 소비, 해시 함수(3.4·3.5·5.6) |
| 중간 M1 `DeathResolve` 두 묶음·이름 셋 | 반영 | `DeathHooks`(WP4) → `DeathRewards`(WP3), 한 단계 = 시스템 하나, `EnemyKilled`/`CorpseSpawned`(3.5·5.6) |
| 중간 M2 레지스트리 이름 공간·시그니처 | 반영 | 키 `(PartKind, type)`(6.2), 역할 인터페이스 7개 시그니처·훅 어댑터(6.6) |
| 중간 M3 `DefRef` 구조체로는 못 채움 | 반영 | `sealed class`, 연결 단계 한 번만, 변환기 팩토리(6.2) |
| 중간 M4 인스턴스별 이동 지시 | 반영 | 기획 리뷰 H1 과 합쳐 `MoveOrder`(6.6). `Feared` 생애 상태는 `Flee` 지시로 바꿈 |
| 중간 M5 `PartSlot[]` 크기 | 반영 | `InlineArray(12)`, 페이즈는 로딩 때 풀고 합집합 슬롯 번호(6.6) |
| 중간 M6 공간 질의가 구체 클래스 | 반영 | `ISpatialQuery`(밀집 인덱스·틱 안 유효·넘침 규칙), `TestKit/BruteForceSpatial`(4.8) |
| 중간 M7 WP1 이 너무 큼 | 반영 | WP1 은 계약·뼈대만, 로더·앱·개발 도구는 WP7 로 나눠 병렬. Data 는 처음부터 각 묶음, `rules` 파일 나눔, 한 묶음 형은 그 묶음 소유(11절) |
| 중간 M8 PNG 병렬 해독 불가 | 반영 | 주 스레드에서 `FromStream` + 색 처리 콜백으로 차례로. 느리면 StbImageSharp 경로(7.5) |
| 중간 M9 적 → 주인공 피해 길 두 개 | 반영 | 요청 하나, `Attacker`, `HitLanded` 다음 틱, `ModifyOutgoing`(5.1) |
| 중간 M10 레벨 표 null | 반영 | 기획 리뷰 M8 과 같이 처리 |
| 중간 M11 WinExe 종료 코드 | 반영 | `Start-Process -Wait -PassThru`, 판정은 로그, 데이터 1차 판정은 `dotnet test`(9절) |
| 중간 M12 여러 묶음이 쓰는 도우미 | 반영 | `AnimClock`·`CameraMath` 는 WP1 `Common`, `RenderContext.ScreenToWorld`(WP5 구현, WP6 사용) |
| 낮음 L1 플로우 필드 두 벌 | 반영 | 한 벌 제자리(4.3) |
| 낮음 L2 슬로모션 보간 | 반영 | 늘임 누적값을 alpha 로(3.2) |
| 낮음 L3 JSON 로딩 손일 | 반영 | 메타 키 지우기, 항목 단위 역직렬화, 변환기 목록(6.2) |
| 낮음 L4 매개변수 이름 제각각 | 반영 | `telegraph {effect,time}`, `range_band`, `phase_through`(6.1) |
| 낮음 L5 `HiDef` | 반영 | `GraphicsProfile.HiDef`, WP7 완료 기준에 가장 큰 시트(7.1) |
| 낮음 L6 `gulim.ttc` | 반영 | 대체 목록에서 뺌. 실제로 넣어 보고 되면 다시 넣음(8.2) |
| 낮음 L7 SDL 힌트 | 반영 | 환경 변수(9절) |
| 낮음 L8 xunit.v3 설정 | 반영 | `OutputType Exe`, runner 3.x, 테스트 수 > 0(1.3) |
| 낮음 L9 길 검사 14 와 필드 16 | 반영 | `NavConst` 한 곳, 13.5-5 에 16·26 검사(4.3·6.14·12.1) |
| 낮음 L10 로더가 WP2 코드를 부름 | 반영 | `IDataValidator` 등록(6.2) |
| 낮음 L11 `Int2`·`Vector2` 충돌 | 반영 | Core `Int2`·`RectI`, 게임 `XnaVector2` 별칭(1.3) |
| 낮음 L12 개발 옵션 적용 담당 | 반영 | `IRunSetup`(11.3) |
| 낮음 L13 `FrameLoop` 테스트 불가 | 반영 | `TickAccumulator`·`StageStepper` 를 Core 로(3.2·3.3) |
| 낮음 L14 정의 1개 추가 시험 | 반영 | 전수 연기 시험 + 변형 픽스처(6.13). 비용이 거의 없는 `bone_brute`·`hell_goat` 를 MVP 로 올림(기획 8.2 v0.3) |

### 13.3 검증 담당 (`docs/reviews/architecture-verifier.md`)

| 지적 | 처리 | 내용 / 이유 |
|---|---|---|
| 높음 H1 자동 실행이 저장 파일을 읽음 | 반영 | 개발 옵션 + `--save-dir` 없음 → 읽지도 쓰지도 않음, `--window` 우선, 로그는 현재 폴더(9절·8.3). WP7 완료 기준에 save.json 시각 |
| 높음 H2 겹친 점 0 으로 나누기 | 반영 | `MathX.TieAngle`, 원 중심 규칙, `EndTick` 디버그 불변식, 시험 3개(4.6·4.7·10.1) |
| 중간 M1 흔들림·깜빡임 프레임 단위 | 반영 | 표시 시계 표, 해시 함수(3.4) |
| 중간 M2 캡처 크기·포그라운드 판정 | 반영 | 캡처 크기 = `--window`(7.1). 포그라운드·최소화 확인을 WP7 완료 기준과 10.2 에. 안 되면 숨긴 창(R8) |
| 중간 M3 게임 쪽 결정성 기록 | 반영 | `TickAccumulator`·`StageStepper`, 레벨업 오버레이 같은 앱 틱(3.3), `--log-hash` |
| 중간 M4 데이터 순서 | 반영 | 서수 정렬, 객체 → 순서 배열, 뒤섞기 시험(3.4·10.1) |
| 중간 M5 갇힘 계측 | 반영 | `InvariantChecker`·`--check`, 숫자 기준(10.1) |
| 중간 M6 필드 시작점 없음 | 반영 | BFS 대체 시작점, 모서리 시험, 13.5-5 추가(4.3·6.14) |
| 중간 M7 입장 중 적 조준 | 반영 | 입장·등장 중은 위치와 무관하게 해시에서 뺌(4.8), 시험 2개 |
| 중간 M8 성능 규모·기준 | 반영 | `--stress`·`--freeze-ai`, 기준 표 하나, `perf_scenario.json`(4.9) |
| 중간 M9 판정할 수 없는 완료 기준 | 반영 | 생성 수 ± 3, `SCENE`/`RESULT` 로그·`--exit-on-result`, 10.2 "통과 =" 열 |
| 중간 M10 몬스터 늘리기 시험 | 반영 | 전수 연기 시험, 변형 픽스처, 안 나오는 적 경고, 색조 구별 경고(기준값은 기획과 정함), 고정 미리보기, 부품 시험 목록(6.13·6.14·10.1) |
| 중간 M11 소환·호위·분열 자리 | 반영 | `PlacementResolver`(4.10) |
| 낮음 L1 VSync·워밍업 | 반영 | `--vsync off`, `draw_ms` 정의, 첫 5초 뺌(10.3) |
| 낮음 L2 긴 시험이 기본에 | 반영 | 분류 + 환경 변수 건너뛰기, 결정성 기본은 30초 판(1.3·10.1) |
| 낮음 L3 `--start-at` 이 보스 뒤 | 반영 | 보스 바로 + 보스 시계 맞춤(9절). 기획 15절 v0.3 |
| 낮음 L4 길찾기 캡처 시각 | 반영 | `--stress 180 --capture-at 5,20`(10.2) |
| 낮음 L5 끝나지 않는 경로 | 반영 | `--timeout`·종료 코드 4, 자동 실행 데이터 오류는 화면 없이 2, 캡처가 안 생기면 3(9절) |
| 낮음 L6 로그 첫 줄 정보 | 반영 | seed·구성·커밋·데이터 해시·명령줄(3.4) |
| 낮음 L7 자동 조작 중 실제 입력 | 반영 | 입력 장치를 만들지 않음, `--debug-nav` 층은 옵션 값(3.2·9절) |
| 낮음 L8 퇴로 없는 kite·A* 시작 | 반영 | `MoveBlockedTime` 0.5s → 제자리 사격(기획 8.3 v0.3), A* 3칸 밖이면 기울기 뒤 재요청(4.4) |
| 낮음 L9 PNG 읽기 방법 | 일부 반영 | 크기는 IHDR 머리글만(1.3). 기준점 자동 검사는 보류 — 시험에 PNG 해독 패키지가 필요하고, 단계 1 은 `--preview` 사람 판정으로 충분. 단계 2 뒤 어긋남이 잦으면 넣음 |
| 낮음 L10 해시 결과 넘침 | 반영 | 구현 리뷰 M6 과 같이(4.8) |
| 낮음 L11 길찾기 성질 전수 | 반영 | 두 스테이지 전수 (a)(b), A* 비교는 끈 당기기 전(10.1) |
| 낮음 L12 `AllowUnknownParts` | 반영 | 로더 단위 시험 전용, 게임·실제 데이터는 늘 엄격(6.2) |
| 낮음 L13 판정 결과 자리 | 반영 | 10.2 "통과 =" 열, 확인 / 문제 / 판정 못 함 |
