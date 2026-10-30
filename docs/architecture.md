# Diablo Defence Remastered — 설계서 (Architecture)

- 상태: v0.4.2 (설계 담당).
  - v0.1: 첫 판.
  - v0.2: 세 리뷰(`docs/reviews/architecture-planner.md`, `architecture-implementer.md`, `architecture-verifier.md`)를 반영했다 — 지적별 처리는 13.1~13.3.
  - v0.3: 기획서 v0.4 의 **디아블로 1 원본 자원 사용**(사용자 요구 10)을 받았다 — 자원 가져오기 도구(1.4), 등각 아레나 좌표·타일 충돌(4.1·4.2),
    원작 시트·TRN 변종(6.3·7.5), 타일셋·맵 정의(6.11), 소리(6.15·7.9), 원작 타일 그리기(7.6, 장애물 굽기 대체), 작업 묶음 다시 짜기(11절: WP1b·WP0·WP8).
    WP1 구현(커밋 `ec851d6`)이 문서와 다르게 정한 것을 문서에 옮겼다(11.4). 처리 목록은 13.4.
  - v0.4: v0.3 리뷰(`docs/reviews/architecture-v03-review.md`, 구현·검증·기획 담당 지적 41개)를 반영했다 — 맵 데이터 읽기 공용화(`MapDataReader`), 가져오기 표 7개와 `arena.json` 모양,
    최소 실제 데이터 선행 단계(WP0a), 벽 정렬 단계 나누기, 원본 SOL 사실 맞추기(앞둘레 턱 = `X`), 개발 실행의 null 오디오 장치, 저작물 유출 막기, 원작 형식 규칙 부록(부록 A). 처리 목록은 13.5.
  - v0.4.1: 기획서 v0.5 와 맞췄다 — 13.5 의 열린 질문 중 기획서가 답한 것(큰 피해 기준 `telegraph_required`, 조명 판독성 값과 `unit_min_light` 대상, 최종 크기 자르기 `max_scale` 1.6,
    벽 가림 `wall_fade` 기본, 앞둘레 턱 문구)을 설계 값·규칙에 옮기고, 그 밖에 두 문서가 다르게 적은 곳(진입점 `N−6`, 목표 문 행진 끝, 주인공 맞음 소리, 검증 13.5-12~14)을 맞췄다. 처리 목록은 13.6.
  - v0.4.2: WP1b 구현 리뷰를 반영했다 — `GameModules` 위치, 무음 `AudioServices`, 걷기 재생 배율 함수, 시그니처를 코드에 맞춤. 처리 목록은 13.7.
  기준 기획서: `docs/gdd.md` v0.5.1(설계에 넘긴 목록은 기획서 20절 끝, v0.5 처리는 21절). 출발점: `docs/brief.md`.
- 이 문서는 **코드 구조·형식·알고리즘·작업 나누기**를 정한다. 수치·규칙은 기획서가 정하고, 여기서는 그것을 어디에 어떻게 담는지만 정한다.
  기획서와 다르게 해석한 곳은 그 자리에 `[해석]` 으로 표시하고 12.1 에 모았다.
- 코드 조각은 인터페이스 시그니처 수준이다. 이름은 구현 때 바뀔 수 있지만 **11절 작업 묶음 사이의 계약**(굵게 표시한 형)은 WP1·WP1b 가 정한 뒤 고치려면 관련 묶음 담당과 합의한다.
  공용 형의 필드를 누가 쓰고 누가 읽는지는 11.2 표가 정본이다. 이미 커밋된 코드와 이 문서가 다르면 코드가 정본이고, 문서를 고친다(11.4).
- 사용자 요구 8 "몬스터 종류를 늘릴 것" 은 6.13 에 답한다: 새 적 = JSON 정의 1개(원작 그림 묶음 + 원작 색 변종 TRN, 그 위에 `extends` 로 색·크기·행동 변형),
  모든 적 정의를 자동으로 돌려 보는 연기 시험, MVP 몬스터 17종(+분열체, 기획 8.2).
- 사용자 요구 10 "디아블로 1 원본 자원을 쓸 것" 은 1.4·4.2·6.3·6.11·6.15·7.5·7.6·7.9 에 답한다: 원본(`diabdat.mpq`)에서 뽑은 그림·소리·타일 그림은
  저장소에 넣지 않고 가져오기 도구가 사용자 PC 의 `Assets/d1/` 에 쓴다. 저장소에는 도구·가져오기 표·타일 배치·정의 JSON 만 있고, **자원 파일이 없어도 Core 시험(실제 데이터 검사·길 검사·시나리오)은 모두 돈다.**
  단, 원본으로 확인해야 하는 숫자(타일 번호·성질 표, 단계 1 그림 묶음의 프레임 수)는 WP0a 가 이 PC 에서 먼저 읽어 저장소 데이터로 넣는다(11절).

## 0. 핵심 결정 한눈에

| 주제 | 결정 |
|---|---|
| 프로젝트 | `Core`(그래픽·소리 재생 없는 순수 로직: 데이터·타일 배치 풀기·길찾기·시뮬레이션·틱 누적기·소리 고르기 규칙) / 게임(MonoGame: 렌더·소리 재생·입력·화면·앱) / 테스트(Core 만 참조) / 가져오기 도구(`tools/DiabloAssetImporter`: Core + MPQ 읽기)와 그 시험 |
| 자원 | 원작 디아블로 1(`diabdat.mpq`)의 몬스터·주인공·마법·타일·소리·음악은 가져오기 도구가 `Assets/d1/` 에 뽑는다(가져오기 표에 적은 것만, 결정적, 캐시). 원작 exe PNG 는 아이콘·획득물·타이틀·로고·커서에만. 자원이 없으면 빌드는 DDR0001, 실행은 안내 화면, 시험은 자원 대조만 건너뜀 |
| 개체 모델 | 범용 ECS 를 쓰지 않는다. **종류별 풀(EntityStore) + 고정 순서 시스템**. 개체 클래스는 `partial` 이고 묶음마다 자기 필드 조각을 가진다(11.2). 적의 다양성은 상속이 아니라 **행동 부품(전략) 조합** |
| 데이터 | `Data/**/*.json`(경로 서수 정렬로 읽음), 한 파일에 여러 종류를 담을 수 있음(적 + 그 적의 투사체). `extends` 로 변형 정의. 로딩 때 오류를 모두 모아 보여 줌. 게임 정의는 가져오기가 만든 파일 이름만 알고 원작 경로는 모른다 |
| 좌표 | 월드 = 등각 아레나 그림의 **화면 픽셀**(실수). 하위 타일 ↔ 월드는 `IsoMath` 한 곳. 화면 어느 방향이든 같은 속도(4.1) |
| 루프 | 고정 60Hz 틱 + 보간 렌더. 모든 시간(메뉴·연출·표시 이펙트·흔들림 포함)은 틱으로 센다 → `--seed` 재현, 캡처 시각 정확, 같은 명령이면 같은 PNG |
| 길찾기 | 16px 격자(외곽 상자 전체, 마름모 밖은 맵 밖) + 장애물까지 거리(clearance) 장. 무리 = 몸 크기 2등급 플로우 필드, 보스·중간 보스 = A*(이진 힙, 8방향, 코너 자르기 금지, 끈 당기기). 적 부품은 **이동 의도**만 내고 길은 내비가 정한다 |
| 충돌 | **저장소의 타일 배치 + 타일 성질 표**에서 만든 평행사변형이 정본(가져오기 도구가 원작 SOL 과 대조). 정확한 도형 밀어내기 + 원-원 해소(겹친 점은 결정적 방향), 이웃 질의는 32px 공간 해시 |
| 렌더 | 960×540 월드 렌더 타깃 → 정수배 점 확대 후 선형 축소(레터박스). 바닥은 구운 조각 텍스처, 뒷둘레 벽은 바닥 바로 뒤·앞둘레 턱은 유닛 뒤 단계, 안쪽 벽 기둥만 유닛과 y 정렬, 주인공을 가리는 벽은 반투명. 원작 시트는 (그림 묶음 × 변종)마다 모든 동작 프레임을 잘라 한 페이지에 묶은 아틀라스, TRN 변종은 파일. 빛은 곱하기 빛 지도 |
| 소리 | 시뮬레이션은 소리를 모르고 신호(`CueEvent`)·표시 이벤트만 낸다. 게임 `AudioSystem` 이 연결 표(`sound_bindings`)로 소리를 고르고 동시 재생 제한·우선순위를 적용한다(7.9). 소리 길이는 자원 색인에서 읽어 무음·소리 켬이 같은 수를 센다. 개발 옵션 실행은 OpenAL 을 출력 없는 장치(`ALSOFT_DRIVERS=null`)로 열고 기본 무음 |
| 검증 | `--autoplay --seed --capture --capture-at --quit-after --stage --debug-nav --preview --log-ticks --log-hash --check --stress --exit-on-result --mute --sound --sound-device`. 창을 앞으로 끌어오지 않고, 개발 옵션 실행은 사용자 저장 파일을 **읽지도 쓰지도 않으며** 스피커로 소리를 내지 않는다(`--sound-device default` 를 줄 때만, 사용자 동의) |

---

## 1. 솔루션·폴더 구조

### 1.1 트리

```
DiabloDefenceRemastered/
├─ DiabloDefenceRemastered.sln      Core·게임·시험·가져오기 도구·도구 시험
├─ global.json                      SDK 9.0.306, rollForward latestFeature
├─ Directory.Build.props            net9.0, Nullable enable(nullable 경고 = 오류), ImplicitUsings, LangVersion latest, Deterministic
├─ docs/  README.md  .gitignore  .gitattributes
├─ tools/
│  ├─ import-all.ps1                원작 exe PNG + diabdat 가져오기를 한 명령으로 (1.4)
│  ├─ import-assets.ps1             원작 exe PNG 49장(Windows PowerShell 5.1, 그대로 둔다)
│  └─ DiabloAssetImporter/          가져오기 도구(.NET 9 콘솔, War3Net.IO.Mpq + Core)                                  (WP0)
│     ├─ Program.cs  Mpq/  Formats/(CL2·CEL·PAL·TRN·MIN·TIL·SOL·WAV 해석)  Bake/(시트·아틀라스·아레나·미니맵·아이콘·소리)
│     ├─ Output/(PNG 쓰기·manifest·캐시·소리 색인)  Audit/(저장소에 원작 자원이 섞였는지 대조)
│     └─ Table/                      가져오기 표 JSON 8개(원작 경로·TRN·칸 폭·쓸 프레임·그림자 알파·타일셋 원본 경로 → 만들 파일 이름. 원작 바이트 없음, 1.4)
├─ src/
│  ├─ DiabloDefenceRemastered.Core/            클래스 라이브러리. MonoGame 참조 없음
│  │  ├─ Common/        EntityId, EntityStore<T>, Pool<T>, SimRandom, StableHash, FacingMath(방향→열)·Compass, Rgb, Int2, RectI, MathX,
│  │  │                 AnimClock(클립·프레임·이벤트 프레임), CameraMath(ViewRect), TickAccumulator(누적기·틱 상한·캡처 멈춤)
│  │  ├─ Defs/          정의 record (SpriteDef, TilesetDef, MapDef, EnemyDef, WeaponDef, SoundDef …), DefRef<T>, StatKey, EnemyStatKey
│  │  ├─ Data/
│  │  │  ├─ Registry/   PartRegistry, PartKind, IPartFactory, IDataValidator, ValidationContext, DataError, GameDefs   (계약, WP1)
│  │  │  ├─ Json/       DefJson(공용 STJ 옵션: snake_case·주석·Disallow), 공용 변환기(Vector2·TileCoord·Rgb·RectI)    (계약, WP1b)
│  │  │  └─ Loading/    DataLoader, JsonMerge(extends·*_add·*_patch·페이즈 풀기), 나머지 변환기, 공통 검증 규칙     (WP7)
│  │  ├─ Map/           IsoMath(하위 타일 ↔ 월드), TileCoord, MapDataReader(tilesets·maps 만 읽기), ArenaLayout(배치 → 메가타일 격자),
│  │  │                 ArenaGrid(하위 타일 막힘·둘레 표시), ResolvedMap(입구·기준점·시작 = 월드 px)                  (계약, WP1b)
│  │  ├─ Assets/        AssetFormat(판 번호), AssetManifest, ImportTable(가져오기 표 8개 모양·만들 이름 목록), SpriteMeta,
│  │  │                 ArenaArt(arena.json·바닥 조각 이름 규칙), SoundIndex(소리 길이)                                (계약, WP1b)
│  │  ├─ Geometry/      Shape(원·사각형·다각형), 부호 거리, 밀어내기, 선분 판정
│  │  ├─ Nav/           ArenaCollision(막힘 → 평행사변형), NavGrid, ClearanceField, FlowField, AStar, BinaryHeap, PathSmoother,
│  │  │                 Navigator, PlacementResolver, NavConst, Validation/(길 검사)
│  │  ├─ Spatial/       SpatialHash(ISpatialQuery 구현), ObstacleIndex
│  │  ├─ Modules/       GameModules(모듈 목록 — 조립 뿌리, Sim/ 밖이라 Core.Audio 를 알아도 됨)              (계약, WP1·WP1b)
│  │  ├─ Sim/
│  │  │  ├─ World.cs  Simulation.cs  StageStepper.cs
│  │  │  ├─ Contracts/  시스템 계약·틱 이벤트·표시 이벤트(신호 포함)·명령·피해 요청·이동 의도·질의 인터페이스·모듈
│  │  │  ├─ Core/       BeginTickSystem, EndTickSystem
│  │  │  ├─ Entities/   Enemy, Hero, Projectile, Hazard, Telegraph, Pickup 의 공통 조각(partial, WP1)
│  │  │  ├─ Movement/   이동 의도 → 조향·적분·충돌 해소, Enemy.Nav.cs(조각), Invariants/(불변식 계측)
│  │  │  ├─ Hero/  Combat/  Weapons/  Projectiles/  Hazards/  Pickups/  Progression/
│  │  │  ├─ Enemies/    EnemySystem, 생애 상태, Parts/(행동 부품·적 동작), Ranks/(정예·우두머리·접두사), Buffs/
│  │  │  ├─ Director/   생성 누적기, 시간표 이벤트, 상한 정리, 생성 출처·우선순위
│  │  │  └─ Autoplay/   자동 조작 정책(이동·카드 고르기)
│  │  ├─ Audio/         AudioModule(소리 연결 표 검사기), SoundMixer(동시 재생·우선순위 고르기, 순수 로직) — Sim 은 이 폴더를 쓰지 않는다  (WP8)
│  │  ├─ Art/           ArtModule(시트 메타·PNG 크기 검사기), PngHeader
│  │  ├─ Dev/           DevOptions 파서, TickProfiler, StateHasher(--log-hash)
│  │  └─ Save/          SaveData 모델·직렬화(파일 위치는 게임 쪽)
│  └─ DiabloDefenceRemastered/                 실행 파일. MonoGame.Framework.DesktopGL 3.8.6, FontStashSharp.MonoGame 1.6.2
│     ├─ Program.cs  GameApp.cs
│     ├─ App/
│     │  ├─ Contracts/  IScene, IAutoplayDriver, SceneStack, RenderContext, InputFrame, AppServices(+ IArtServices, IAudioServices)   (계약, WP1·WP1b)
│     │  └─ FrameLoop(TickAccumulator 사용), DevRunner(캡처·종료·감시), AssetCheck, SaveStore, Log, WindowControl   (WP7)
│     ├─ Assets/        git 제외. 뿌리 = 원작 exe PNG 49장·커서, d1/ = 가져오기 도구 출력(1.4)
│     ├─ Data/          정의 JSON (git 포함)
│     ├─ Loading/       TextureStore(아틀라스·변종·색 키, 스테이지별 미리 읽기), CursorLoader(.cur), FontProvider
│     ├─ Rendering/     RenderServices, VirtualScreen, Camera, WorldRenderer, ArenaRenderer(바닥 조각·벽 기둥·가림), Layers, SpriteDraw,
│     │                 Lighting, FxSystem(표시 이펙트), CorpseLayer, DamageNumbers, DebugNavOverlay
│     ├─ Audio/         AudioServices(연결 파일), AudioSystem(신호 → 소리), SoundBank(WAV 읽기), MusicPlayer(페이드·덕킹)
│     ├─ Input/         KeyboardMouseSource, InputMap
│     ├─ Scenes/        SceneFactory, Title, StageSelect, Play, Result, DataError, Preview, Overlays/(LevelUp, Chest, Pause, Settings), AutoplayDriver
│     └─ UI/            UiCanvas, Widgets, Hud, Minimap, Theme
└─ tests/
   ├─ DiabloDefenceRemastered.Tests/           xunit.v3. Core 만 참조
   │  ├─ TestKit/       TestWorldBuilder, BruteForceSpatial, StraightNav, TestDefs(작은 타일셋·맵 포함), TestCategories
   │  ├─ Fixtures/      data/(로더 시험용 작은·고장 난 JSON), variants/(코드 없이 만든 변형 적), perf_scenario.json
   │  └─ Common/ Data/ Map/ Assets/ Geometry/ Nav/ Spatial/ Movement/ Invariants/ Combat/ Weapons/ Hero/ Progression/
   │     Enemies/ Director/ Audio/ Art/ Dev/ Save/ Sim/
   └─ DiabloAssetImporter.Tests/               xunit.v3. 가져오기 도구만 참조. 작은 바이트(CL2·TIL·SOL·WAV)와 시험이 만든 작은 MPQ 로 — 원작 자원 없이 돈다.
                                               바이트는 시험 코드의 인코더가 만든다(원본에서 잘라 낸 바이트를 픽스처로 넣지 않는다, R24)
```

- 저장소 뿌리에 남아 있던 `DiabloDefenceRemastered/obj/` 는 WP1 이 정리했다. 실제 프로젝트는 `src/`·`tools/`·`tests/` 아래에만 둔다.

### 1.2 왜 Core 를 별도 프로젝트로 나누나

- 길찾기·전투 계산·데이터 검증뿐 아니라 **시뮬레이션 전체**를 Core 에 둔다. 그래서 테스트가 그래픽 장치 없이 한 판을 통째로 돌릴 수 있다
  (결정성 검사, 성능 측정, 행동 부품 시험). 같은 프로젝트 안 폴더로만 나누면 실수로 `Microsoft.Xna.Framework` 를 쓰는 것을 막을 수 없다.
- Core 는 벡터로 `System.Numerics.Vector2` 를 쓴다. 게임 쪽은 그리기 직전에만 MonoGame `Vector2` 로 바꾼다(확장 메서드 하나 `ToXna()`).
- 애니메이션 **시계**(어느 클립의 몇 번째 프레임인지)는 시뮬레이션 상태다. 타격 프레임·발사 프레임이 게임 규칙이기 때문이다(원작의 "그리기 안에서 상태가 바뀜" 문제를 끊는다).
  텍스처·그리기는 게임 쪽만 안다. Core 는 시트의 **칸 크기·방향 수·프레임 수·프레임 시간**만 정의로 안다.
- **타일 배치 풀기와 충돌도 Core 다**(`Map/`, `Nav/ArenaCollision`): 저장소의 배치 데이터와 타일 성질 표만으로 충돌 도형을 만들므로 길 검사·내비 시험·시나리오가 자원 없이 돈다.
  가져오기 도구도 같은 `ArenaLayout.Expand` 를 불러 그림을 굽는다 → 그림을 구운 배치와 충돌을 만든 배치가 같은 코드에서 나온다.
  정의 JSON 을 읽는 길도 하나다: 도구와 게임 로더(WP7)가 모두 Core `Map/MapDataReader` 로 `tilesets`·`maps` 를 읽는다(1.4) → 같은 저장소면 `LayoutHash` 가 같다.
- **소리 고르기 규칙(동시 재생 상한·우선순위)도 Core `Audio/` 의 순수 로직**이라 시험이 돈다. 시뮬레이션(`Sim/`)은 `Audio/` 를 쓰지 않는다 — 소리를 켜든 끄든 같은 씨앗이면 같은 결과다(7.9).
- 가져오기 표의 모양과 "표가 만드는 파일 이름 목록"(`Assets/ImportTable`)도 Core 에 둔다. 도구와 시험(13.5-10, 정의가 가리키는 이름 ⊂ 표가 만드는 이름)이 같은 코드를 쓴다.

### 1.3 프로젝트 설정

| 프로젝트 | 종류 | 참조 | 비고 |
|---|---|---|---|
| `DiabloDefenceRemastered.Core` | `net9.0` 라이브러리 | (없음, BCL 의 System.Text.Json 만) | `InternalsVisibleTo` 테스트 |
| `DiabloDefenceRemastered` | `net9.0` WinExe | Core, MonoGame.Framework.DesktopGL 3.8.6, FontStashSharp.MonoGame 1.6.2 | MGCB 없음. 두 `Vector2` 가 함께 보이므로 `XnaVector2` 별칭(csproj `Using`)으로 XNA 쪽을 쓴다 |
| `DiabloDefenceRemastered.Tests` | `net9.0` **xunit.v3** 3.1.0, `OutputType Exe` | Core, `xunit.v3`, `xunit.runner.visualstudio` 3.x, `Microsoft.NET.Test.Sdk` | 게임 프로젝트·도구는 참조하지 않는다. 동적 건너뛰기는 `Assert.Skip` |
| `DiabloAssetImporter` (`tools/`) | `net9.0` Exe | Core, `War3Net.IO.Mpq` 6.0.3(MIT) — 전이 의존: `War3Net.Common`(MIT), `System.Drawing.Common` 6.0.0(MIT, Windows 전용), `SharpZipLib` 1.4.2(MIT), `System.IO.Hashing`(MIT) | MonoGame 없음. 그림은 직접 쓰는 PNG 쓰기(ImageSharp 안 씀, 시제품과 같음). sln 에 넣어 같이 빌드. 전이 의존과 라이선스는 README 제3자 고지에도 적는다(WP0) |
| `DiabloAssetImporter.Tests` | `net9.0` xunit.v3, `OutputType Exe` | 도구 | 합성 바이트만. 원작 자원 없이 돈다 |

- **Assets**: 게임 csproj 가 `$(DdrAssetDir)\**\*`(README.md·`d1\.cache\**`·`d1\import.log` 제외)를 출력 폴더 `Assets\` 로 폴더 그대로 복사한다(`PreserveNewest`).
  `DdrAssetDir` 기본 = 프로젝트의 `Assets`(WP1 이 둔 속성), `SkipAssetCheck` 기본 `false`.
  - 빌드 전 대상 `CheckAssets` 가 `$(DdrAssetDir)\d1\manifest.json` 또는 `$(DdrAssetDir)\TITLE.png` 가 없으면
    `error DDR0001: 자원이 없다. tools\import-all.ps1 -Diabdat <diabdat.mpq> -Exe <DiabloDefence.exe> 로 채운다 (README 참고)` 로 멈춘다.
    v0.2 의 확인 파일 `Dungeon1.png` 는 더 쓰지 않는다(계약 변경, WP1b). `-p:SkipAssetCheck=true` 로 건너뛸 수 있다(CI).
  - 빌드는 manifest 가 **있는지만** 본다. 판 번호·파일 목록 대조는 실행 시작 때(1.4 끝, WP7 `AssetCheck`) — MSBuild 안에서 JSON 을 읽지 않으려고.
  - 원본 `$(DdrAssetDir)\d1\manifest.json` 이 없으면 빌드는 **출력 폴더의 `Assets\d1\manifest.json` 을 지운다**(`SkipAssetCheck` 여도). 가져오기가 중간에 실패해 원본 manifest 가 없어진 뒤
    `PreserveNewest` 복사가 새 파일만 덮고 옛 manifest 를 남겨, 섞인 자원으로 뜨는 일을 막는다. 남은 섞임은 시작 검사의 파일 크기 대조(1.4 끝)가 잡는다.
  - 게임 빌드는 가져오기 표(`tools\DiabloAssetImporter\Table\*.json`)도 출력 `ImportTable\` 로 복사한다. 시작 검사가 그 해시를 manifest 의 표 해시와 비교한다(1.4 끝).
  - 복사 양은 수백 MB 가 아니라 백 MB 안팎으로 본다(아틀라스로 잘라 묶은 시트, 1.4·7.5). 출력 폴더를 여러 벌 만들 때 느리면 하드 링크 복사
    (`CreateHardLinksForCopyFilesToOutputDirectoryIfPossible`)를 켠다(R25).
- **Data**: 게임 csproj 가 `Data\**\*.json` 을 출력 `Data\` 로 복사한다. 테스트 csproj 는 같은 파일을 링크로 포함해 출력 `Data\` 로 복사한다
  (`<None Include="..\..\src\DiabloDefenceRemastered\Data\**\*.json" LinkBase="Data" CopyToOutputDirectory="PreserveNewest" />`).
  가져오기 표도 같은 방법으로 출력 `ImportTable\` 에 링크 복사한다(`..\..\tools\DiabloAssetImporter\Table\*.json`, 13.5-10 시험용).
  → 실제 데이터 검증·길 검사·이름 대조 시험이 자원 없이 돈다.
- 테스트는 자원이 있으면 실제 파일도 대조한다(manifest 판, 아틀라스 메타 ↔ 정의, PNG 크기 — 13.5-7). PNG 는 해독하지 않고 IHDR 머리글(앞 24바이트)의 폭·높이만 읽는다(`Core/Art/PngHeader`).
  자원 폴더는 환경 변수 `DDR_ASSETS`(**시험만** 읽는다 — 게임은 읽지 않는다) 또는 저장소 상대 경로 `src/DiabloDefenceRemastered/Assets` 로 찾고, `d1/manifest.json` 이 없으면 그 검사만 `Assert.Skip` 으로 건너뜀 표시한다.
  manifest 가 있어도 형식 판(`AssetFormat.Version`) 또는 표 해시가 저장소 기준과 다르면 `Assert.Skip("자원이 낡았다 — 가져오기를 다시 돌린다")` 로 건너뛴다.
  판이 같을 때만 대조 실패를 실패로 친다 → 코드 회귀와 "다시 가져오기 필요" 가 섞이지 않는다.
- xunit.v3 는 테스트 프로젝트가 실행 파일(`OutputType Exe`)이어야 하고 `xunit.runner.visualstudio` 3.x 가 있어야 `dotnet test` 가 찾는다.
  빠뜨리면 "테스트 0개" 로 조용히 통과하므로 완료 기준에 "발견된 테스트 수 > 0" 을 넣는다.
- 테스트 분류(`[Trait("Category", …)]`): 기본(표시 없음, 전체 30초 안 목표), `Scenario`(긴 한 판), `Perf`(Release 측정), `Integration`(모든 묶음 등록이 필요한 것, 통합 때 기본으로 옮김).
  느린 분류는 환경 변수 `DDR_RUN`(예: `Scenario,Perf`)에 그 이름이 없으면 `Assert.Skip` 으로 건너뜀 표시한다 → 옵션 없는 `dotnet test` 가 늘 빠르다.
  골라 돌릴 때는 `DDR_RUN=Perf` 와 `--filter Category=Perf` 를 같이 쓴다.
- 공통 빌드 설정(WP1 이 정함): `global.json` `rollForward: latestFeature`, `Directory.Build.props` 에서 nullable 경고를 오류로(`WarningsAsErrors` 에 `nullable`), `Deterministic`.
- CI(나중): `dotnet test tests/DiabloDefenceRemastered.Tests` 와 `dotnet test tests/DiabloAssetImporter.Tests` 는 자원 없이 끝난다. sln 전체 빌드는 `-p:SkipAssetCheck=true`.

### 1.4 자원 가져오기 도구 `tools/DiabloAssetImporter` (WP0, 사용자 요구 10, 기획 19절)

**실행** — 사용자가 클론 뒤 한 번. 창을 띄우지 않고, 원본 폴더는 읽기만 한다.

```powershell
# 두 원본을 한 명령으로: 원작 exe PNG(import-assets.ps1) → diabdat(도구)
powershell -ExecutionPolicy Bypass -File tools\import-all.ps1 -Diabdat "<…>\diabdat.mpq" -Exe "<…>\DiabloDefence.exe"

# 도구만
dotnet run --project tools/DiabloAssetImporter -c Release -- import --diabdat <diabdat.mpq> [--out <Assets 폴더>] [--force] [--jobs <n>] [--hellfire <폴더>]
dotnet run --project tools/DiabloAssetImporter -- verify [--out <Assets 폴더>]           # manifest 와 파일 해시만 대조
dotnet run --project tools/DiabloAssetImporter -- probe tiles --diabdat <…> --tileset l1 --tiles 1,2,15   # 성질 표(6.11)를 쓸 때 SOL 보기
dotnet run --project tools/DiabloAssetImporter -- contact --diabdat <…> --out <저장소 밖 폴더>           # 사람이 눈으로 볼 견본 PNG
dotnet run --project tools/DiabloAssetImporter -- audit [--out <Assets 폴더>]   # git 이 추적하는 파일에 원작 자원이 섞였는지 대조(R24)
```

- `--diabdat` 가 없으면 흔한 설치 위치 몇 곳(목록은 도구 코드)을 찾아보고, 못 찾으면 "경로를 `--diabdat` 로 준다" 는 문장과 종료 코드 2 로 끝난다(입력을 기다리지 않는다).
- `--out` 기본값은 현재 폴더가 아니라 **저장소 뿌리 기준**이다: 도구가 현재 폴더에서 위로 올라가며 `DiabloDefenceRemastered.sln` 을 찾아 `<뿌리>\src\DiabloDefenceRemastered\Assets` 로 정한다.
  못 찾으면 "`--out` 을 준다" 와 종료 코드 2(다른 폴더에서 `dotnet run --project …` 로 돌려 엉뚱한 곳에 쓰고 성공처럼 끝나는 일을 막는다).
- **출력 경로 검사**(R24): `import`·`contact`·`probe` 의 출력 폴더가 git 작업 트리 안이면 `git check-ignore -q <폴더>/x.png` 로 무시되는 곳인지 보고, 무시되지 않으면 종료 코드 2 로 거절한다.
  git 을 찾을 수 없으면 작업 트리 안은 `src\DiabloDefenceRemastered\Assets\` 아래만 허용한다. 견본·캡처는 저장소 밖(스크래치·임시 폴더)에 두는 것이 원칙이다.
- `audit`: `git ls-files` 의 파일마다 SHA-256 을 manifest 의 출력 해시, 그리고 표가 가리키는 원본 항목 바이트의 해시와 대조하고, 확장자 목록(`*.mpq *.cl2 *.cel *.pal *.trn *.min *.til *.sol *.dun *.amp *.wav`)과
  `Assets/d1/**` 도 본다. 하나라도 걸리면 그 목록과 종료 코드 4. 원본이 없으면 출력 해시와 확장자만 본다. 원작 그림이 든 캡처 PNG(바이트가 출력과 다름)는 이 검사로 못 잡으므로 규칙으로 막는다(R24).
- 원본은 `FileAccess.Read`·`FileShare.Read` 로만 연다. 원본 폴더에 캐시·로그를 만들지 않는다.
- **`diabdat.mpq` 에만 기댄다**(기획 19.2). `devilutionx.mpq` 는 열지 않는다. `hellfire*.mpq` 는 `--hellfire` 를 줄 때만 열고, 표에서 `"optional": true` 인 항목만 거기서 찾는다(MVP 는 그런 항목이 없다, 기획 19.4).

**구성**: `Mpq/`(War3Net 감싸기, 여러 MPQ 우선순위 — diabdat 먼저), `Formats/`(순수 해석기: 스프라이트 CL2·CEL, 레벨 CEL 조각, MIN, TIL, SOL, PAL, TRN, WAV),
`Bake/`(시트 잘라 묶기, 아레나 바닥·기둥, 미니맵, 아이콘, 소리 변환), `Output/`(PNG 쓰기, 임시 파일 → 바꿔치기, manifest, 캐시, 소리 색인), `Audit/`.
형식 규칙은 **부록 A**(조사 문서·시제품 `d1conv`·`d1map` 이 확인한 것을 저장소로 옮긴 것)가 정본이다. 조사 문서와 시제품은 세션 임시 폴더에 있어 사라질 수 있으므로 WP0 은 부록 A 와 이 절만 보고 만들 수 있어야 한다.
시제품 코드는 참고만 하고 WP0 이 새로 쓴다(시제품은 우리가 쓴 코드지만 DevilutionX 소스에서 읽은 식이 섞여 있다 — 식은 부록 A 의 사실 요약으로 옮기고 코드는 옮기지 않는다, R24).

**MPQ 읽기와 스레드** — War3Net 의 `MpqArchive`·`MpqStream` 은 아카이브 하나의 기반 스트림을 `Seek`·`Read` 로 같이 쓰므로 여러 스레드가 동시에 읽으면 바이트가 섞인다.
원본 항목 읽기(복호·압축 풀기)는 **주 스레드에서 차례로** 해서 `byte[]` 로 넘기고, `--jobs` 병렬은 해독·굽기·PNG 압축에만 쓴다. 캐시 키의 "입력 원본 항목 바이트 해시" 도 주 스레드가 읽은 바이트로 만든다.

**가져오기 표** `tools/DiabloAssetImporter/Table/*.json` 8개 — 모양은 Core `Assets/ImportTable` 의 record 다. Core 가 표 전체를 **엄격하게**(모르는 속성 오류, 6.1 과 같은 STJ 옵션) 읽고,
도구는 그 record 를 그대로 쓴다(도구가 따로 파싱하지 않는다). 13.5-10 시험은 같은 record 로 "만들 이름 목록" 을 만든다. 표는 WP0 소유다.

```json
// Table/monsters.json — 표 머리 값은 항목 기본값, 항목·동작이 덮어쓴다
{ "palette": "levels\\l1data\\l1.pal", "shadow_alpha": 0.5,
  "monsters": [
    { "out": "d1/monsters/zombie", "cl2": "monsters\\zombie\\zombie{a}.cl2", "width": 128,
      "actions": { "idle": "n", "walk": "w", "attack": "a", "special": "s", "die": "d" },
      "variants": { "": null, "bluered": "monsters\\zombie\\bluered.trn", "grey": "monsters\\zombie\\grey.trn",
                    "yellow": "monsters\\zombie\\yellow.trn", "eth": "monsters\\monsters\\eth.trn" } } ] }

// Table/hero.json — 동작 값은 글자 하나 또는 객체 { "file", "cl2", "width", "frames", "shadow_alpha" } (덮어쓰기)
{ "palette": "levels\\l1data\\l1.pal", "shadow_alpha": 0.5,
  "hero": [
    { "out": "d1/hero/slt", "cl2": "plrgfx\\sorceror\\slt\\slt{a}.cl2", "width": 96,
      "actions": { "idle": "as", "walk": "aw", "attack": { "file": "at", "width": 128 },
                   "cast_fire": { "file": "fm", "width": 128 }, "cast_lightning": { "file": "lm", "width": 128 },
                   "cast_magic": { "file": "qm", "width": 128 },
                   "die": { "cl2": "plrgfx\\sorceror\\sln\\slndt.cl2", "width": 128 } },
      "variants": { "": null } } ] }

// Table/missiles.json — shadow_alpha 를 적은 항목만 색인 0 을 그림자로 바꾼다(firerun·guard 처럼 그림자가 있는 것). 없으면 색인 0 도 그냥 색
{ "palette": "levels\\l1data\\l1.pal",
  "missiles": [
    { "out": "d1/missiles/fireba",   "cl2": "missiles\\fireba{d}.cl2", "dirs": 16, "width": 96 },
    { "out": "d1/missiles/arrows",   "cl2": "missiles\\arrows.cl2", "frames_are_dirs": 16, "width": 96 },
    { "out": "d1/missiles/firewal2", "cl2": "missiles\\firewal2.cl2", "width": 128, "frames": [1, 11] },
    { "out": "d1/missiles/firerun",  "cl2": "missiles\\firerun{d}.cl2", "dirs": 8, "width": 128, "shadow_alpha": 0.5 } ] }

// Table/icons.json — 아이콘 = 마법 프레임 한 장 이상을 겹친 48×48. layers 는 그리는 순서
{ "size": 48, "background": [16, 12, 10],          // "palette"(선택) — 없으면 missiles.json 의 palette
  "icons": [
    { "out": "d1/icons/fireball", "cl2": "missiles\\fireba{d}.cl2", "dirs": 16, "width": 96,
      "layers": [ { "dir": 4, "frame": 7 } ] },
    { "out": "d1/icons/multishot", "cl2": "missiles\\arrows.cl2", "frames_are_dirs": 16, "width": 96,
      "layers": [ { "dir": 3, "frame": 1, "offset": [-6, 4] }, { "dir": 4, "frame": 1 }, { "dir": 5, "frame": 1, "offset": [6, 4] } ] } ] }

// Table/sounds.json — {키} 는 vars 의 값마다 펼친다. 출력 = "d1/sound/" + 원작 경로 소문자·'/'
{ "sounds": [
    { "path": "sfx\\misc\\cbolt.wav" },
    { "path": "monsters\\zombie\\zombie{k}{i}.wav", "vars": { "k": ["a", "h", "d", "s"], "i": ["1", "2"] } } ] }

// Table/music.json
{ "music": [ { "path": "music\\dintro.wav" }, { "path": "music\\dlvla.wav" }, { "path": "music\\dlvlb.wav" } ] }

// Table/exe.json — 원작 exe PNG 49장·커서의 이름(이름 대조용, 그림 없음)
{ "files": [ "TITLE.png", "LOGO.png", "Gold.png", "DIInop.cur" ] }

// Table/tilesets.json — 타일셋 원본 정보(도구만 씀). 게임 정의 Data/tilesets(6.11)의 id 와 같은 키
{ "tilesets": [
    { "id": "l1", "dir": "levels\\l1data", "cel": "l1.cel", "min": "l1.min", "til": "l1.til", "sol": "l1.sol",
      "pieces_per_subtile": 10, "palettes": { "l1_1": "l1_1.pal", "l1_2": "l1_2.pal" },
      "overlay": { "cel": "l1s.cel",                       // 출처: DevilutionX 참고(아치 덧그림 번호표, 부록 A.5)
                   "frames": { "1": [11, 70, 320, 210, 340, 417], "2": [10, 248, 324, 343, 330, 420],
                               "3": [252], "4": [254], "5": [258], "6": [266] } } } ] }
```

- 공통 필드: `out`(출력 이름, `d1/` 로 시작), 원작 경로 틀(`{a}` = 동작 글자, `{d}` = 방향 파일 1..`dirs`), `width`, `frames`([처음, 끝], 1부터 — 파일이 더 많을 때 쓸 범위),
  `shadow_alpha`(표 머리 → 항목 → 동작 순으로 덮어씀, 없으면 색인 0 을 그대로 검정으로), `variants`(이름 → TRN 경로, `""` = 원색), `"optional": true`(헬파이어처럼 `--hellfire` 로만 찾는 항목.
  없으면 건너뛰고 manifest 에도 없다 — 정의가 가리키면 시작 검사가 잡는다. MVP 에는 없음).
  아이콘 `layers` 원소 = `{ "dir", "frame", "offset"?: [x,y], "rotate"?: 도 }`(겹침·회전은 1.4 변환 규칙 "아이콘").
- **칸 폭**: 도구가 파일에서 추정한 폭(32줄 건너뛰기 표, 부록 A.2)을 쓴다. 표의 `width` 는 **대조용**이고, 32줄보다 낮은 프레임처럼 추정이 안 되는 때만 대신 쓴다.
  둘이 다르면 종료 코드 4(`hero/slt cast_fire: 파일 폭 128, 표 96`). 마법사 `slt` 처럼 동작마다 폭이 다르면 동작 객체에 `width` 를 적는다.
- 표에 적지 않는 것: 프레임 수·높이(도구가 파일에서 읽는다. 정의의 값과는 게임 시작·시험이 대조 — 6.3), 원작 그림·소리 바이트, DevilutionX `*.tsv`(읽지도 복사하지도 않는다, 기획 19.1).
- **원본에서 온 수치**(R24): 저장소에 들어가는 원본 유래 숫자는 **사실 수치만 손으로** 적는다 — 경로·칸 폭·프레임 범위(이 표), 타일 번호·성질·SOL 보정 칸(6.11),
  아치 덧그림 번호표(위 `overlay`), 동작별 프레임 시간·타격 프레임(그림 묶음 클립, 6.3). DevilutionX 의 파일·표 구조·주석·코드는 옮기지 않는다.
  값마다 출처를 JSON 주석으로 남긴다: `// 출처: 원본 확인`(이 PC 의 diabdat 를 읽어 확인) 또는 `// 출처: DevilutionX 참고`(원본 파일에 없는 게임 동작 값). 둘 곳은 위에 적은 한 곳뿐이다.
- 맵 그림은 표에 없다: 도구가 게임 정의 `Data/tilesets/*.json`·`Data/maps/*.json`(6.11)을 **Core `Map/MapDataReader.Read(dataDir)`** 로 읽어 `ArenaLayout.Expand` 로 펼친다.
  `MapDataReader` 는 `tilesets`·`maps` 두 종류만 읽고(다른 종류·부품 등록·다른 묶음의 데이터 오류와 무관), 공용 STJ 옵션·변환기(Core `Data/Json`)를 쓰고, `MapDef.Tileset` 을 ID 로 연결한다.
  결과는 `MapDataResult(Maps, Tilesets, Errors)`, 로더용 오버로드 `Read(IEnumerable<MapDataElement>)`(파일·JSON 경로를 그대로 넘김)가 있다.
  예외: JSON 문법이 깨진 파일과 중복 키가 있는 파일은 두 종류가 있는지 알 수 없어 다른 묶음 파일이어도 오류로 낸다(중복 키는 정의·표 모두 오류, `DefJson.FindDuplicateKey`).
  두 종류는 `extends`·`*_add`·`*_patch` 를 쓰지 않는다(쓰면 오류) → 병합 규칙이 두 벌이 될 일이 없다. 게임 로더(WP7 `DataLoader`)도 이 두 종류는 `MapDataReader` 결과를 그대로 받는다(6.2).

**출력** — `--out` 기본 = 저장소 뿌리의 `src/DiabloDefenceRemastered/Assets`(위 "실행" 의 찾기 규칙). 도구는 그 아래 `d1/` 만 쓴다(원작 exe PNG 는 뿌리, `import-assets.ps1` 이 그대로 쓴다).

```
Assets/
  *.png  DIInop.cur                   원작 exe (import-assets.ps1)
  d1/
    manifest.json                     마지막에 쓴다: 형식 판(AssetFormat.Version), 도구 판(도구·Core 어셈블리 SHA-256), .NET 런타임 판, 원본 크기·SHA-256,
                                      표 해시(Table/*.json 8개를 이름 순으로 이은 바이트의 SHA-256), 파일마다 경로·크기·SHA-256
    monsters/zombie.png  zombie@bluered.png  zombie@grey.png …  zombie.json     그림 묶음 한 페이지(모든 동작, 변종마다) + 메타(변종 공통, 6.3)
    hero/slt.png  slt.json            missiles/fireba.png  fireba.json …      icons/fireball.png …
    maps/cathedral_arena/arena.json  floor_<cx>_<cy>.png  columns.png  columns.json  minimap.png       (7.6, 이름 규칙은 아래 "아레나")
    sound/sfx/misc/cbolt.wav  sound/monsters/zombie/zombiea1.wav …  sound/music/dlvla.wav               원작 경로를 소문자·'/' 로 그대로
    sound/index.json                  소리 색인: 파일마다 표본 수·표본율·채널(7.9 고르기가 길이를 안다)
    import.log                        마지막 실행 보고
    .cache/index.json                 건너뛰기 표(빌드 복사·manifest 에서 뺌)
```

- 한 묶음이 4096×4096 한 장에 안 들어가면 동작 단위로 다음 페이지(`zombie_p2.png`, `zombie_p2@bluered.png`)로 넘긴다. 메타의 사각형이 페이지 번호를 가진다(6.3).
  실패하지 않고 넘기지만, 페이지가 늘면 그리기 호출도 는다 — 끝 보고에 2페이지 이상인 묶음을 적는다.

**변환 규칙**

| 대상 | 규칙 |
|---|---|
| 몬스터·주인공 CL2 | 8그룹(방향) 묶음으로 읽는다(마법은 평면 — 그룹 머리가 평면 표로도 읽히는 함정 때문에 종류별로 정해 둔다, 부록 A.2). RLE 투명 → 알파 0, **색인 0(그림자) → 검정 × `shadow_alpha`**(그 항목·동작에 값이 있을 때), 나머지 = TRN(있으면 먼저) → PAL. 프레임은 칸(폭 × 그 동작 최대 높이)에 바닥 맞춤으로 놓는다 |
| 잘라 묶기 | 프레임마다 **알파 > 0 인 픽셀**의 사각형으로 잘라(반투명 그림자도 사각형 안에 남는다 — "불투명 픽셀" 로 자르면 그림자만 조용히 잘린다), **(그림 묶음 × 변종) 하나의 모든 동작**을 한 페이지(가로·세로 ≤ 4096)에 줄 쌓기로 묶는다. 순서 = (동작 이름 서수, 방향, 프레임) 고정. 넘치면 동작 단위로 다음 페이지(위 출력). 변종은 같은 자리에 색만 다르게(TRN 은 투명을 바꾸지 않으므로 사각형이 같다) → 메타 하나를 같이 쓴다. 좀비 묶음 전체 약 630프레임 × 평균 50×70 이면 2048² 안에 든다(어림, WP0 이 잰다) |
| 마법 | 방향 있는 것은 파일 16개(뼈 영혼 8개)를 방향 0..15 로(1번 파일 = 아래 = 방향 0, 시계 방향 — brief 4절 각도 순서와 같다). `arrows` 는 프레임 n → 방향 n−1, 프레임 1개. 방향 없는 것은 방향 1 |
| 주인공 | 마법사 `slt` 의 `as`·`aw`·`fm`·`lm`·`qm`, 죽기 `sln\slndt`(기획 4.0) |
| 아이콘 | 표 `layers` 의 (방향, 프레임)을 차례로 겹쳐(`rotate` 는 가장 가까운 픽셀 회전, `offset` 은 정수 이동) 48×48 어두운 칸 가운데에 맞춰 줄인다(정수 상자 평균 — 결정적, 기획 5.9) |
| 아레나 | `MapDataReader` → `ArenaLayout.Expand`(6.11) → 메가타일 격자 → TIL → MIN(위 단부터 저장 — 뒤집어 읽음) → 레벨 CEL 조각(종류 = MIN 값 비트 12~14, 삼각형 패딩) + 맵 팔레트(`Table/tilesets.json` 의 `palettes`). 바닥 칸(성질 `.`) → 512×512 바닥 조각, 그 밖의 칸 → 하위 타일 기둥 그림(64×160) 아틀라스(같은 그림은 한 프레임), L1 아치 덧그림(`overlay`), 칸 목록 `arena.json`(Core `Assets/ArenaArt`), 미니맵(외곽 상자 1/16). **바닥 조각 이름 규칙**(자원 없이 계산, Core `ArenaArt.FloorChunks(ArenaGrid, IsoMath)`): 조각 (cx, cy) 는 성질 `.` 인 칸의 마름모 상자 `[C.x−32, C.x+32) × [C.y−16, C.y+16)` 가 하나라도 `[512cx, 512cx+512) × [512cy, 512cy+512)` 와 겹치면 있다 → `floor_<cx>_<cy>.png`. 픽셀이 모두 투명이어도 규칙이 있다고 하면 쓴다. 자세한 것은 7.6 |
| 성질 대조 | 타일셋의 성질 표(6.11)를 쓰는 메가타일에 대해 원본 SOL 과 비교한다. 비교는 **두 비트만**: `W` = bit0(Solid)만, `M` = bit2(BlockMissile)만, `X` = 둘 다, `.` = 둘 다 아님. 다른 비트(BlockLight·Transparent·TransLeft/Right·Trap 128)는 무시한다(바이트를 그대로 비교하면 SOL 159/175/191 같은 벽 조각에서 거짓 실패). 다르면 그 칸이 표의 `sol_known_diffs` 에 있어야 하고, 없으면 실패: `타일셋 l1 메가타일 15 하위 칸 3: 표 'X', SOL '.'` |
| 소리 | RIFF 해석. PCM 8비트(부호 없음) → 16비트 `(b − 128) << 8`, 16비트는 그대로. 채널·표본율 그대로, 표준 44바이트 머리로 다시 쓴다(다른 덩어리는 버림). 형식 ≠ PCM, 길이 0 이면 실패. 음악도 같은 함수(이미 16비트) |

**결정론** — 같은 원본 + 같은 표 + 같은 맵·타일셋 정의 + 같은 도구 판 → 같은 바이트.
- PNG 는 도구가 직접 쓴다: RGBA 8비트, 줄 필터 0 고정, `ZLibStream` 압축 수준 고정, `tIME`·글 덩어리 없음. JSON 은 정해진 키 순서·불변 문화·`\n`.
- 작업 목록은 출력 경로 서수 정렬, 묶기 순서 고정, 사전 순회 결과에 기대지 않는다. 원본 읽기는 주 스레드에서 차례로, `--jobs` 병렬은 해독·굽기·PNG 압축에만(작업 하나는 한 스레드) → 결과가 같다.
- 확인: 도구 시험(시험이 만든 작은 MPQ 로 두 번 → manifest 같음, `--jobs 1` 과 `--jobs 8` → manifest 같음), WP0 완료 기준(실제 원본으로 두 번 → manifest 같음).
  압축 구현이 .NET 판마다 다를 수 있어 **같은 런타임 판 안에서만** 보장한다(manifest 에 런타임 판을 적는다).

**캐시** — 첫 실행은 PNG 압축이 대부분이라 수십 초 안팎으로 본다(WP0 이 재서 이 줄을 고친다).
- `d1/.cache/index.json`: 작업마다 키 = SHA-256(도구 판 + 작업 명세 JSON + 입력 원본 항목 바이트의 해시 + 맵이면 펼친 배치 해시).
  **도구 판 = 도구·Core 어셈블리 파일 바이트의 SHA-256**(사람이 올리는 번호가 아니다. `Deterministic` 빌드라 같은 소스면 같은 값) → `Bake`·`Formats` 를 고치고 번호를 안 올려도 캐시가 옛 출력을 쓰지 않는다.
  `AssetFormat.Version` 은 게임과의 **호환 판정**에만 쓴다(출력 모양·뜻이 바뀌면 올림, 아래).
  키가 같고 출력 파일이 있고 그 해시가 기록과 같으면 다시 만들지 않는다. `--force` 는 캐시를 무시한다. 캐시 적중·미적중 결과 바이트는 같다(시험).
- **굽기 결과가 바뀌는 도구 변경은 `AssetFormat.Version` 을 올린다**(WP0 규칙 — 메타·`arena.json` 모양, 바닥/벽 나눔, 그림자·잘라 내기 규칙). 게임은 도구 바이트를 모르므로 이 번호로만 "도구가 바뀌어 다시 가져와야 함" 을 안다.
  번호를 안 올리는 실수는 맵 쪽이면 `layout_hash`(타일셋 정의 해시 포함, 6.11)가, 표 쪽이면 표 해시가 대부분 잡는다.

**실패와 보고**
- 시작할 때 `d1/manifest.json` 을 먼저 지우고, 표의 모든 항목을 만든 뒤에만 쓴다 → 중간에 실패하면 빌드·실행이 "자원 없음" 으로 본다(반쯤 채운 Assets 로 뜨지 않음, 기획 19.2).
- 파일은 임시 이름으로 쓰고 바꿔치기한다. 새 manifest 에 없는 `d1/` 아래 파일은 지운다(`d1/` 밖은 건드리지 않음).
- 종료 코드: 0 성공, 1 처리 안 된 예외, 2 원본을 못 찾음·못 엶, 3 표에 있는데 원본에 없는 파일(목록 전부), 4 대조 실패(성질 표·프레임 범위·형식).
- 끝 보고(표준 출력 + `d1/import.log`): 종류별 만든 수·크기, 캐시 적중 수, 걸린 시간, 없는 파일 목록, 원본 SHA-256.

**원작 exe 가져오기와의 관계** — `tools/import-assets.ps1`(원작 exe PNG 49장)은 그대로 둔다. 리소스 Bitmap 을 되살리려면 Windows PowerShell 5.1(.NET Framework)이 필요해서
.NET 9 도구로 옮기지 않는다. `tools/import-all.ps1` 이 exe → diabdat 순서로 둘을 부른다(사용자가 두 번 실행하지 않게, 기획 19.2). exe PNG 는 정의에서 `source: "grid"`(6.3)이고
패시브 아이콘·획득물·타이틀·로고·커서에만 쓴다(기획 19.3).

**빌드 검사·실행 검사와의 관계**
- 빌드: DDR0001(1.3) — manifest 와 `TITLE.png` 가 있는지만.
- 실행 시작(WP7 `AssetCheck`): manifest 를 읽어 ① `format` ≠ Core `AssetFormat.Version` 이면 "자원 형식이 낡았다(판 a → b). 가져오기를 다시 돌린다",
  ② manifest 의 표 해시 ≠ 출력 `ImportTable\` 로 복사된 표의 해시면 "가져오기 표가 바뀌었다 — 가져오기를 다시 돌린다"(git pull 로 칸 폭·그림자 값이 바뀐 경우 — 이름은 같아 ③ 이 못 잡는다),
  ③ 정의가 가리키는 파일(시트·변종·메타·맵 그림·소리·음악·소리 색인)이 manifest 에 없으면 그 목록, ④ manifest 의 파일마다 **크기**가 디스크와 다르면 그 목록(해시보다 싸다 — 섞인 출력 폴더, 1.3),
  ⑤ 맵 `arena.json` 의 `layout_hash` 가 저장소 배치·타일셋으로 계산한 값과 다르면 "맵 배치가 바뀌었다 — 가져오기를 다시 돌린다".
  하나라도 있으면 타이틀 대신 **안내 화면**(DataError 화면, 무엇이 없는지 + 명령 한 줄, 기획 19.3)을 띄운다.
  개발 옵션 실행이면 화면 없이 로그 + 종료 코드 5. 단 개발 옵션이 `--capture*`·`--quit-after`·`--window`·`--log` 뿐이면(`--autoplay` 없음) 안내 화면을 띄운 채 캡처하고 종료 코드 5 — 안내 화면을 캡처로 확인하는 길(10.2).
- 게임의 자원 폴더를 바꾸는 방법은 **빌드 하나**다: `dotnet build -p:DdrAssetDir=<빈 폴더> -p:SkipAssetCheck=true -o <임시 폴더>`(복사가 비어 출력에 `Assets\d1` 이 없다).
  게임은 `DDR_ASSETS` 를 읽지 않는다(그 변수는 시험 전용, 1.3). 실행 옵션으로 자원 폴더를 바꾸는 길은 두지 않는다 — 검사·읽기 경로가 두 벌이 된다.
- 원작 exe PNG 만으로 도는 대체 실행은 만들지 않는다(기획 19.3).

---

## 2. 디자인 패턴

원칙: **규칙이 늘어나는 곳(적 행동·무기·이벤트·데이터)에는 확장 지점을 두고, 늘지 않는 곳(루프·시스템 순서·렌더 단계)은 단순한 코드로 둔다.**

### 2.1 채택

| 패턴 | 어디에 | 왜 |
|---|---|---|
| **타입 오브젝트** | 모든 정의(`EnemyDef`, `WeaponDef`, `ProjectileDef`, `EffectDef`, `StageDef` …). 인스턴스(`Enemy`)는 `Def` 를 가리키고 바뀌는 값만 가진다 | 원작의 "유닛 종류마다 if/else" 를 없애는 핵심. 새 적 = 새 정의 객체, 코드 분기 없음 |
| **프로토타입(정의 상속)** | JSON `extends` — 부모 정의를 깊은 병합한 뒤 덮어씀. `abstract: true` 는 생성 안 되는 틀 | 몬스터 다양화가 대부분 "같은 그림 묶음 + 원작 TRN 변종 + 크기·수치·부품 하나 다름" 이라 복사 없이 차이만 적게 한다(6.13) |
| **전략(부품)** | 적 행동 부품(`IEnemyBehavior` 계열)·적 동작(`IEnemyAction`), 무기 발사 패턴(`IFirePattern`), 조준(`ITargeting`), 이벤트 종류(`PartKind.StageEvent`), 이동 의도를 길로 바꾸는 `Navigator`(flow/astar/direct, 4.4) | 기획 13.2·13.3 이 이미 "부품 목록 + 매개변수" 로 짜여 있다. 정의는 부품 ID 와 매개변수만 고른다 |
| **레지스트리(팩토리)** | `PartRegistry`: (`PartKind`, JSON `"type"`) → (매개변수 형, 생성 함수). 부품·동작·발사 패턴·이벤트가 각자 등록. 데이터 검사기도 `IDataValidator` 로 등록 | 로더(WP7)가 구체 부품(WP3·WP4)과 기하 검사(WP2)를 몰라도 된다. 모르는 `type` 은 로딩 오류로 모인다 |
| **상태 패턴** | ① 적 생애 상태(`Entering`→`Emerging`→`Active`, `Stunned`, 죽으면 제거) — 고정된 몇 개라 `enum` + `switch`. 공포·행진·호위는 생애 상태가 아니라 이동 지시(`MoveOrder`, 6.6). ② 적의 독점 행동(공격·점화·돌진·시전)은 `ActionState` 를 소유 부품이 진행. ③ 씬·오버레이(`IScene`). ④ 주인공(`Alive`/`Dying`) | 원작은 상태가 그리기 함수 안 플래그로 흩어져 있었다. 상태마다 들어감·진행·나감이 한 곳에 있어야 "점화 중 죽으면 폭발 안 함" 같은 규칙을 지킬 수 있다 |
| **데코레이터** | 보스 패턴 = `PatternAbility(조건, 애니메이션, 예고, 안쪽 동작 목록)`. 훅 = `TriggeredAction(trigger, 동작)`. 접두사 = 생성 때 정의 위에 덧씌우는 수정자 | 동작(`melee_slam`·`ground_hazard` 등)을 보스·일반 적·접두사가 그대로 같이 쓴다(6.13 `mud_man`) |
| **이벤트 큐(관찰자, 끌어오기형)** | 시뮬레이션 안: 형별 구조체 큐(`EnemyKilled`, `HitLanded`, `HeroDamaged`, `LevelUpReady` …, 두 칸)를 뒤 단계와 다음 틱이 읽는다. 시뮬레이션 → 화면·소리: `PresentationBuffer`(생긴 틱이 붙은 `FxEvent`·`DamageNumber`·`ShakeEvent`·`CorpseSpawned`·신호 `CueEvent`). 알림 문구는 버퍼가 아니라 `World.Banner` 상태 | 콜백 구독은 반복 중 재진입(분열이 죽음 처리 중 적을 추가)과 순서 비결정을 만든다. 큐는 순서가 고정되고 할당이 없다 |
| **오브젝트 풀** | 적·투사체·장판·예고·획득물(`EntityStore<T>`), 게임 쪽 표시 이펙트·시체·피해 숫자·빛 | 초당 수백 개가 생기고 사라진다. 틱 중 GC 할당 0 을 목표로 한다 |
| **명령** | 입력 → 틱마다 `PlayerCommand`(이동 벡터 + 버튼 비트). 출처는 `IInputSource`: 키보드·마우스 / 자동 조작 봇 | 봇과 사람이 같은 경로를 탄다. 시뮬레이션은 입력 장치를 모른다. 나중에 리플레이(명령 기록)도 이 위에 얹을 수 있다 |
| **월드 컨텍스트** | 시스템·부품은 `World` 하나를 인자로 받는다(정의, 난수, 저장소, 해시, 내비, 이벤트, 시계) | 한 판의 모든 상태가 한 객체에 있어 테스트가 월드를 여러 개 만들 수 있다. 정적 싱글턴·서비스 로케이터를 쓰지 않는다 |
| **합성 뿌리(수동 DI)** | 게임 쪽 `GameApp` 생성자에서 한 번 조립(로더, 자원 검사, 텍스처 저장소, 소리, 씬 스택, 저장소). 묶음별 연결 파일(`RenderServices`·`SceneFactory`·`AudioServices`)만 부른다 | 객체 그래프가 작다. DI 컨테이너는 이득보다 간접층이 크다 |

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
| 실행 중 TRN 색 바꾸기(색인 텍스처 + 팔레트) | 셰이더 없이는 CPU 로 텍스처를 한 벌 더 만드는 것과 같아 그림 메모리 이득이 없고, 색인 형식 해독기·팔레트를 게임에 따로 둬야 한다. 가져올 때 변종 파일로 굽는다(7.5) |
| 원작 무작위 층 생성기·DUN 방 그대로 쓰기 | 생존 액션에는 넓은 방 하나가 맞고(기획 10.1), DevilutionX 의 생성 결과 DUN 은 사용 조건 확인 전이라 쓰지 않는다. 우리가 쓴 배치 데이터로 만든다(6.11) |
| 원작 SOL·마름모 격자로 직접 길찾기 | 칸 하나가 화면 64×32 라 반경 9~34 몸 크기를 나누기에 거칠고, 마름모 격자용 플로우·A*·clearance 를 새로 짜야 한다. 16px 격자 + 평행사변형 도형이면 v0.2 의 4.2~4.4 가 그대로다(4.2) |

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
  흔들림 오프셋 = `StableHash(seed, stageTick)`, 점 빛 깜빡임 = `StableHash(seed, lightId, stageTick)`, 장식 = `StableHash(seed, 개체 ID)`,
  장판 조각 자리 = `StableHash(장판 ID, i)`(6.4). 바닥 타일 섞기는 로딩 때 `StableHash(맵 ID, mx, my)`(6.11).
  표시 이벤트(이펙트·시체·피해 숫자·신호)는 **생긴 스테이지 틱**을 싣고, 그릴 때 "나이 = 현재 틱 − 생긴 틱" 으로 계산한다(진행량을 누적하지 않는다).
- 소리의 무작위(파일 고르기·높낮이)는 게임 쪽 난수이고 시뮬레이션 난수와 따로다. 소리는 결정성 대상이 아니지만 시뮬레이션을 읽기만 하므로 결과를 바꾸지 않는다(7.9).

| 표시 요소 | 시계 |
|---|---|
| 월드 안 이펙트·시체·피해 숫자·흔들림·점 빛 깜빡임·맥동·정예 맥동·발광 번쩍임·광폭화 맥동·장판 조각 | 스테이지 틱(멈추면 같이 멈춘다) |
| 주인공을 가리는 벽 반투명(7.6) | 그 프레임의 보간된 주인공 위치 |
| 타이틀 로고·메뉴·오버레이 애니메이션(레벨업 카드 올라옴, 빛 기둥), 음악 페이드·덕킹 | 앱 틱 |
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
| 8 | `SpatialRebuild` | 이동이 끝난 위치로 해시 다시 짓기(무기 조준·맞음 판정용), 60틱마다 불변식 계측(`--check`·시나리오, 10.1) | WP2 |
| 9 | `Weapons` | 쿨다운, 조준, 발사(투사체·장판 생성), 회전 구체 위치 | WP3 |
| 10 | `Projectiles` | 이동·유도, 장애물 막힘, 맞음 판정 → 피해 요청, 폭발 | WP3 |
| 11 | `Hazards` | 장판·지연 폭발·예고 진행 → 피해 요청 | WP3 |
| 12 | `DamageResolve` | 피해 요청 적용(방어·치명·무적·배율), 넉백, 번쩍임, `HitLanded`·피해 숫자 표시 이벤트, HP ≤ 0 → `Dead`(`KillCause.Damage`) | WP3 |
| 13 | `DeathHooks` | 죽은 적: 진행 중 행동 끊기, 죽음 훅(분열·장판·폭발), 호위 공포, 보스 처치 → 승리 처리(아래), `EnemyKilled` 틱 이벤트·`CorpseSpawned` 표시 이벤트 | WP4 |
| 14 | `DeathRewards` | `EnemyKilled` 중 `Cause == Damage` 만: 보석·드롭·처치 수·`RunStats` | WP3 |
| 15 | `Pickups` | 끌어당김·줍기, 경험치 → 레벨업 대기, 물약·룬·상자 | WP3 |
| 16 | `EndTick` | 승패 판정 타이머, 승리 대기면 적 투사체·장판·예고 지우기, 레벨업·상자 대기 → `AwaitingChoice`, 저장소 압축(지운 칸), 디버그 빌드 불변식(`float.IsFinite(Pos)`) | WP1 |

- **틱 번호**(WP1 코드): `World.Tick` 은 단계들이 도는 동안 "이번 틱 번호"(첫 틱 0)이고 `EndTick` 끝에서 1 늘어난다. 틱 중에 생긴 개체는 `SpawnTick = Tick` 을 받아 저장소 끝에 붙고
  **`SpawnTick < Tick` 인 틱부터** 행동한다(= 다음 틱부터). 지우기는 표시만 하고 16 에서 한꺼번에 압축한다. 밀집 인덱스는 압축 전까지만 유효하다.
- **신호 `CueEvent`**(7.9)는 그 일이 일어난 단계가 표시 이벤트로 낸다(무기 발사 9, 적 맞음 12, 적 공격 시작 5 …). 시뮬레이션 상태를 바꾸지 않고 다른 단계가 읽지 않는다.
- **죽음 이유 `KillCause`**: `Damage`(보통 처치 — 훅·보석·드롭·처치 수·시체), `Collapse`(승리 때 무너짐 — 시체만, 훅·보상·처치 수 없음), `Culled`(상한 정리 — 아무것도 없음, 시체도 없음).
- **틱 이벤트 큐는 두 칸**이다: 이번 틱에 쓴 이벤트는 같은 틱의 뒤 단계와 **다음 틱 전체**에서 읽을 수 있다(`Events.Current` / `Events.Previous`). `BeginTick` 이 바꿔 끼우고 오래된 칸을 비운다.
  예: `DamageResolve`(12) 가 낸 `HitLanded` 를 다음 틱 `EnemyThink`(5) 가 읽어 흡혈을 처리한다(1틱 늦음 허용).
- **표시 이벤트(`PresentationBuffer`)는 틱마다 비우지 않는다.** 각 항목에 생긴 스테이지 틱을 싣고, 프레임 끝에 `PlayScene`(WP6)이 `FxSystem.Consume` → `Audio.Consume` 순서로 소비한 뒤 `Clear` 한다.
  한 프레임에 여러 틱이 돌아도 앞 틱의 이벤트가 사라지지 않는다. 그래픽 없는 실행(시험·시나리오)은 `Presentation.Enabled = false` 로 쌓지 않는다.
- 해시를 6 과 8 에서 두 번 짓는다. 300~500 개 재배치는 수십 µs 라 한 번 짓고 낡은 값을 쓰는 것보다 단순하고 정확하다.
- **승리 처리**(기획 2.2): `DeathHooks` 가 `rank: boss` 인 적이 `Damage` 로 죽은 것을 보고 스테이지 `victory.type == boss` 면 그 틱에
  `World.Outcome = VictoryPending` → 살아 있는 적 전부 `Dead`(`Collapse`) → `EndTick` 이 적 투사체·장판·예고를 지운다. 그 뒤 `Director` 는 아무것도 하지 않고
  `DamageResolve` 는 주인공 피해를 무시한다. 같은 틱에 주인공 HP 도 0 이 되었으면 승리가 먼저다. 3초(스테이지 틱) 뒤 결과.
  `victory.type == survive` 면 `EndTick` 이 그 시각에 같은 처리를 한다(단계 1 세로 조각, 6.12).

---

## 4. 이동·충돌·길찾기

### 4.1 좌표와 단위 (v0.3: 등각 아레나의 화면 픽셀)

- 월드 좌표 = **아레나 그림의 화면 픽셀**(`float`, 기획 10.1 — 조사 문서의 안 A). 유닛 위치 = **발 위치**(기획 8.1). 속도 px/s 는 화면 어느 방향이든 같고,
  몸 충돌 원은 화면에서 동그라미다. 각도는 데이터에서 도(°), 로딩 때 라디안.
- 이유: v0.2 설계(AABB 카메라·생성 띠·원 충돌·16px 격자·"도형이 정본")가 그대로 서고, 바뀌는 것은 맵을 만드는 곳뿐이다.
  직교 타일 좌표(안 B)는 카메라 시야가 회전한 사각형이 되어 `CameraMath`·생성 띠·화면→월드 변환을 다시 짜야 하고, 얻는 것은 "세로가 느린 원작 느낌" 뿐이다.
- **`IsoMath`**(Core `Map/`, WP1b 계약) 한 곳에서만 바꾼다. N = 하위 타일 수(메가타일 수 × 2), Hc = 그 층의 벽 기둥 높이(L1·L2 160), H = Hc − 32.

| 무엇 | 식 |
|---|---|
| 하위 타일 (tx, ty) 바닥 마름모 중심 C | `(32N + 32(tx − ty), H + 16(tx + ty) + 16)`. 꼭짓점: 위 `C − (0,16)`, 아래 `C + (0,16)`, 왼 `C − (32,0)`, 오른 `C + (32,0)` |
| 역변환(실수) | `u = (x − 32N)/32`, `v = (y − H − 16)/16` → `tx = (v + u)/2`, `ty = (v − u)/2`. 칸 번호 = 반올림 |
| 축 | tx 가 늘면 화면 오른쪽 아래, ty 가 늘면 왼쪽 아래. 변: `tx = 0` 왼쪽 위(nw), `ty = 0` 오른쪽 위(ne), `tx = N−1` 오른쪽 아래(se), `ty = N−1` 왼쪽 아래(sw) |
| 외곽 상자 | `64N × (32N + H)`. 스테이지 1(N 40) 2560×1408, 스테이지 2(N 44) 2816×1536. 카메라·`World.MapSize` 는 이 상자 |
| 맵 경계 | 하위 타일 N×N 전체를 덮는 큰 마름모: 위 `(32N, H)`, 오른 `(64N, H + 16N)`, 아래 `(32N, H + 32N)`, 왼 `(0, H + 16N)`. 밖은 맵 밖(어둠) |

- 기획 10.1 의 `Y0 = 160`(외곽 높이 32N + 160)은 **`H = Hc − 32 = 128`** 로 정했다. 맨 위 칸(0,0)의 벽 꼭대기가 정확히 y = 0 에 닿는 값이고, 시제품 `arena40`(2560×1408)과 같다. `[해석]`
- 데이터의 위치(입구 바깥 시작점·진입점, 기준점, 주인공 시작, 점 빛 `at`)는 **하위 타일 좌표(실수)** 로 적고(기획 10.1 과 같은 단위), 로딩 뒤 `IsoMath` 로 월드로 바꾼다.
  **칸 중심 = 정수 좌표**다: 칸 (tx, ty) 는 `[tx−0.5, tx+0.5) × [ty−0.5, ty+0.5)`. 그래서 N×N 맵의 기하 중심은 `((N−1)/2, (N−1)/2)`(N 40 → 19.5),
  변 가운데 메가타일 2개로 연 문의 틈 중심도 19.5 다(6.11 예). v0.2 의 손 좌표(`[775, 495]` 꼴)는 더 쓰지 않는다.
- **타일 좌표와 월드 좌표를 형으로 나눈다**(계약 WP1b): 정의 안의 타일 좌표는 `TileCoord`(`readonly record struct TileCoord(float Tx, float Ty)`, JSON `[tx,ty]`)이고 이름에도 `Tile` 을 붙인다
  (`EntryDef.OutsideTile`·`InsideTile`, `HeroStartDef.Tile`, `MapDef.AnchorsTile`, `PointLightDef.AtTile`). `Vector2` 와 섞이면 컴파일러가 잡는다.
  월드 좌표가 필요한 묶음은 **`World.Resolved`(`ResolvedMap`)** 만 읽는다: 로딩 때 한 번 `IsoMath` 로 바꾼 `Entries`(ID → 바깥·진입점 월드 px), `Anchors`(ID → 월드 px), `HeroStart`(월드 px·방위).
  감독·행진·보스 입장(WP4), 점 빛(WP5), 길 검사 13.5-6(WP2)이 각자 `IsoMath` 를 부르지 않는다 → 타일 좌표 `(4,20)` 이 월드 px 로 쓰여 맵 왼쪽 위 어둠에 조용히 생기는 일이 없다.
- **방향 → 시트 열**: `FacingMath.ToColumn(dir, dirs)` 한 곳(WP1 이 `Facing` 대신 `FacingMath` 로 이름을 정함 — 개체 `Facing` 필드와 겹치지 않게).
  원작 시트의 8·16방향은 **땅 위 방향을 등각으로 그린 것**이라(남서 그림 = 화면 `(−32, +16)` 쪽), 열을 고르기 전에 화면 방향의 y 를 2배 한다
  (`FacingMath.IsoYScale = 2`, 계약 변경 WP1b): `angle = atan2(−dx, 2·dy)` 를 0..360 으로, `열 = round(angle / (360/방향 수)) mod 방향 수`.
  남 0°·서 90°·북 180°·동 270° 기준과 열 순서(S, SW, W, NW, N, NE, E, SE — 16방향은 그 사이를 나눔)는 brief 4절 그대로다.
  - **역과 방위도 같은 보정을 쓴다**(WP1 코드의 `FromColumn`·`ToVector` 를 WP1b 가 고침): `FromColumn(열, dirs)` = 각도 θ 에서 `normalize(−sin θ, cos θ / IsoYScale)` → `ToColumn` 의 역이 되어
    8·12·16방향 왕복이 선다(보정 없이 두면 16방향 열 3 → 열 2 로 밀려 `--preview` 가 방향을 하나씩 건너뛴다).
    `ToVector(Compass)` 의 대각선은 **등각 대각선** `normalize(±2, ±1)`(남서 = `(−2, 1)`) — 데이터의 방위(`hero_start.facing`, 행진 `heading`)는 땅 위 방위이고 원작 그림 방향과 같다.
    화면 45° `(−1, 1)` 로 두면 16방향 열로는 남남서가 된다.
  `--preview sprites` 에서 8·16방향을 돌려 맞는지 보고, 틀리면 `IsoYScale`(1 = 보정 없음)과 `ColumnOffsetDegrees` 두 상수만 고친다.
- **걷기 재생 배율**도 땅 기준 속도로 센다: `AnimClock` 재생 배율 = `|(vx, IsoYScale·vy)| / base_speed`(`base_speed` = 화면 가로로 걸을 때의 px/s, 6.3).
  계산은 **`AnimClock.WalkRate(vel, baseSpeed)`(= `FacingMath.GroundSpeed(vel) / baseSpeed`) 한 곳** — 주인공(WP3)·적(WP4)·미리보기·시체(WP5)가 식을 따로 쓰지 않는다.
  원작 걷기 그림은 땅 위 걸음에 맞춰 그려져, 화면 세로로 같은 화면 속도로 걸으면 땅 거리는 두 배다 — 화면 속도로 재면 세로 걷기에서 발이 미끄러진다(기획 4.1·8.1, 확인은 `--preview enemies` 의 방향별 걷기). `[해석]`

### 4.2 충돌 도형과 내비 격자 (v0.3: 타일 배치에서 만든다)

- **정본 = 저장소의 타일 배치 + 타일 성질 표**: 맵 `layout`(6.11) → Core `ArenaLayout.Expand` → `ArenaGrid`(하위 타일마다 걷기 막힘·투사체 막힘)
  → WP2 `ArenaCollision.Build(ArenaGrid)` → `IReadOnlyList<CollisionShape>`(타일셋·`IsoMath` 는 `ArenaGrid` 가 가진다). 같은 메가타일 번호가 그림(가져오기 도구가 원작 CEL·MIN 으로 굽는다)과 충돌(성질 표)을 함께 정하고,
  성질 표는 가져오기 도구가 원작 SOL 과 대조한다(1.4) → "그림과 충돌이 어긋날 수 없다"(요구 3)가 원작 타일에서도 선다. Core 는 SOL·CEL 을 모른다.
- 기획 10.1 은 "SOL 이 정본, 가져오기 도구가 충돌 다각형을 `Assets/` 에 쓴다" 였다. 그러면 충돌이 자원에 묶여 클론 직후·CI 에서 길 검사·내비 시험·시나리오가 모두 못 돈다
  (기획 20절이 넘긴 질문). 성질 표는 우리가 쓰는 메가타일 십여 개의 "어느 하위 칸이 막혔나" 라는 사실만 담고(그림·소리 바이트 없음), 원본과 다르면 가져오기가 실패하므로
  두 길의 결과는 같다. `[해석]`
- **도형 만들기**(`Nav/ArenaCollision`, WP2):
  1. 막힘 종류마다(걷기: `W`·`X`, 투사체: `M`·`X`, 6.11) 하위 타일 칸을 타일 공간에서 **탐욕 직사각형**으로 묶는다(행 우선, 결정적).
  2. 타일 공간 직사각형은 등각 변환(아핀)으로 화면의 **평행사변형** = 볼록 다각형 하나가 된다. 둘레 벽은 문 틈을 포함해 십여 개, 기둥은 하나씩.
  3. 도형마다 `BlocksWalk`·`BlocksProjectiles`·`IsBoundary`. 걷기만 막는 칸(`W`)은 `BlocksWalk` 만, 투사체만 막는 칸(`M`)은 `BlocksProjectiles` 만.
     **원본 사실**(v0.4, diabdat 직접 확인): L1 앞둘레 턱 메가타일 18·19·20·24 는 하위 칸 4개가 모두 SOL 15(Solid·BlockLight·BlockMissile·Transparent) = `X` 다 — 걷기도 투사체도 막는
     하위 칸 두 줄 두께의 벽이다. 뒷벽(메가타일 1·2)은 한 줄만 막히고 나머지 반은 바닥이다. 그래서 MVP 맵에는 `W` 칸이 없다. "낮은 턱은 투사체 통과" 는 원본과 다르므로 쓰지 않는다
     (기획 v0.5 10.1·10.2 도 같은 사실로 고쳤다 — 앞 턱은 보기에만 낮은 두 줄 두께 벽, 기획 의도 차이는 MVP 에 없음). 투사체가 앞둘레를 넘어야 할 기획 이유가 생기면 그때 성질 표에 `design_diffs`(이유 필수, `sol_known_diffs` 와 따로)를 더한다(6.11).
  4. 맵 경계 = 바깥 마름모(4.1). 밖은 맵 밖이고 입장 중인 유닛만 지나간다(기획 2.4).
     **둘레 도형**: 타일셋 `borders` 와 문 끝 조각(`end_a`/`end_b`)에서 온 막힌 칸의 도형은 `IsBoundary = true` 다. 장애물 통과 유닛(`phase_through`)도 이 도형에는 막힌다(4.7-3).
     → 핀드가 기획 8.3 "안쪽 장애물만 무시, 둘레 벽(타일 테두리 칸 전부)과 맵 바깥은 막힘" 대로 둘레 벽 칸 안으로 들어가지 않는다(벽 기둥에 가려 안 보이고 투사체로 못 맞히는 자리에서 주인공을 때리는 일을 막는다).
  - 같은 목록에서 ① 충돌 해소(정확한 도형) ② 내비 격자 clearance ③ 투사체 막음 ④ `--debug-nav shapes` 를 만든다. 그림은 이 목록으로 만들지 않는다(가져오기 도구가 같은 배치로 굽는다).
- 흙처럼 그림이 32px 떠 보이는 막힌 칸도 충돌은 그 칸 자리다(기획 10.1, 보이는 것과 32px 어긋나는 것이 정상).
- 원작의 "대각선 걸음은 두 옆 칸이 비어야 함" 은 두지 않는다 — 다각형 충돌과 격자의 코너 자르기 금지가 대신한다(기획 10.1).
- **격자**: 칸 16px, **외곽 상자 전체**. 스테이지 1 160×88 = 14,080칸(통과 약 5,400), 스테이지 2 176×96 = 16,896칸(통과 약 6,500).
  마름모 밖 칸은 clearance 가 음수(맵 밖)라 처음부터 막힘이다. 통과 칸 수가 v0.2(6,144)와 비슷해 필드·A* 비용도 비슷하고, 배열만 상자 크기로 잡는다.
  가장 작은 유닛 반경 9, 통로 80px = 5칸이라 충분히 곱다. 8px 는 필드 비용이 4배인데 얻는 것이 적다.
- **clearance 장(여유 거리)**: 칸 중심에서 가장 가까운 걷기 막힘 도형·맵 경계(바깥 마름모)까지의 **부호 있는 거리**(px, 도형 안·맵 밖이면 음수)를 정확히 계산해 `float[]` 로 둔다.
  도형 25개 안팎 × 칸 1.7만 ≈ 40만 번 거리 계산 → `ObstacleIndex`(64px 칸) 로 후보를 줄여 스테이지 시작 때 수 ms. 장애물이 움직이지 않으므로 한 번만 만든다.
  - "반경 r 유닛이 이 칸에 설 수 있다" = `clearance ≥ r`. 장애물을 반경만큼 부풀려 칸에 찍는 방식과 같은 뜻이지만, 칸 반올림 없이 **실수 거리**라 80px 틈이 칸 경계 때문에 막히는 문제(리뷰 M1)가 줄어든다.
  - 입장 중인 유닛은 격자를 쓰지 않는다(직선).
  - 기울기(이웃 칸 clearance 차)는 "벽 안으로 밀려 들어간 유닛이 나갈 방향" 으로 쓴다.

### 4.3 무리용 플로우 필드

- **누가**: `movement: flow` 인 적(일반·정예·우두머리)이 "주인공 쪽으로"(`SeekHero`) 이동 의도를 낼 때. 몸 크기 2등급으로 필드를 둘 둔다.

| 필드 | 통과 조건 | 쓰는 유닛 |
|---|---|---|
| `S` | clearance ≥ `NavConst.FieldS` (16) | 몸 반경 ≤ 16 (해골·좀비·궁수·폴른 원·핀드·염소인간·산성 짐승·블랙 데스·오버로드 16, 대부분의 정예) |
| `M` | clearance ≥ `NavConst.FieldM` (26) | 몸 반경 16 초과 (썩은 시체 19, 머드맨 18, 정예·우두머리 썩은 시체 22·22(우두머리는 최종 크기 1.6 으로 잘림, 6.5), 정예·우두머리 오버로드 18·21, 우두머리 머드맨 24 등). 26 을 넘는 flow 유닛은 데이터 검증에서 경고 |

- 문턱 값은 `NavConst` 한 곳에 두고 필드·길 검사(13.5-5)·생성 위치 검사가 같이 읽는다.
- **계산**: 목표 = 주인공. 주인공 둘레 (주인공 반경 + 16)px 안의 그 필드 통과 칸을 모두 시작점(거리 0)으로 하는 다중 시작 다익스트라.
  **그 반경 안에 통과 칸이 없으면**(주인공이 모서리·벽에 바짝 붙어 `M` 문턱을 넘는 칸이 근처에 없음) 주인공 칸에서 BFS 로 찾은 가장 가까운 그 필드 통과 칸들(같은 거리 전부)을 시작점으로 한다.
  8방향, 비용 10/14(정수), **대각선은 두 직교 이웃이 모두 통과 가능할 때만**(코너 자르기 금지). 결과: `ushort dist[]`(65535 = 못 감), `byte dir[]`(가장 작은 이웃 방향).
  우선순위 큐는 A* 와 같은 이진 힙. 통과 칸 5~7천(4.2) 기준 0.1~0.2ms(마름모 밖 칸은 처음부터 막힘이라 돌지 않는다).
- **갱신 주기·분산**: 주인공이 다른 칸으로 옮겼을 때만 다시 계산하되, `S` 는 `Tick % 6 == 0`, `M` 은 `Tick % 6 == 3` 에만 한다(각 0.1s 주기, 한 틱에 둘이 겹치지 않음).
  계산은 한 틱 안에서 동기로 끝나므로 **필드 한 벌을 제자리에서** 다시 쓴다(읽는 쪽이 반쯤 된 필드를 볼 일이 없다). **범위는 맵 전체**: 통과 칸이 5~7천이라 범위를 자를 이유가 없다.
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
   `Enemy.PassThroughObstacles`(`phase_through` 가 켬) 유닛은 `IsBoundary` 가 아닌 장애물을 건너뛰고, 둘레 도형(`IsBoundary`, 4.2-4)과 바깥 마름모에는 막힌다. 입장 중 유닛은 둘 다 건너뛴다.
4. 마지막에 아직 도형 안이면(좁은 틈에 여럿이 몰린 경우) clearance 기울기로 한 번 더 민다.

### 4.8 공간 해시

- 칸 32px(가장 흔한 상호작용 거리 rᵢ+rⱼ+8 ≈ 32), 외곽 상자 기준 80×44 = 3,520칸(스테이지 1)·88×48 = 4,224칸(스테이지 2). 마름모 밖 칸은 늘 비어 있다.
- **넣는 것**: `Life == Active`(또는 `Stunned`) 인 적만. 입장 중·등장 중(`Entering`·`Emerging`) 적은 **위치와 상관없이** 넣지 않는다 → 조준·맞음 판정·장판·분리에서 모두 빠진다.
  기획 2.4 "입장·등장 중에는 피해를 주지도 받지도 않음" 을 규칙 하나로 지킨다(맵 안 진입 구간에서도 유도 미사일이 무적 유닛을 고르지 않는다).
- **계수 정렬 방식**: 칸별 개수 세기 → 누적합 → `entries[]` 에 채우기. 배열 3개를 재사용하므로 할당 0, O(n).
- **질의 계약 `ISpatialQuery`**(WP1 계약, WP2 구현, 시험용 전수 구현은 `TestKit/BruteForceSpatial`):

```csharp
public interface ISpatialQuery
{
    int Query(Vector2 center, float radius, Span<int> denseIdx);              // 적 중심과 center 의 거리 ≤ radius 인 적
    int QueryCapsule(Vector2 a, Vector2 b, float radius, Span<int> denseIdx); // 적 중심에서 선분 a→b 까지 거리 ≤ radius 인 적(투사체 이동 선분 둘레)
    int CellCount(int cx, int cy);                                             // 칸(32px) 하나에 든 적 수(조준 densest). 이것만 칸 단위
}
```

- **결과는 거리로 거른 정확한 집합이다**(WP1 코드): 구현은 칸으로 고른 후보를 다시 거리로 걸러 위 조건을 만족하는 적만 돌려준다 → 맞음 판정·장판·폭발이 반경 밖 적을 맞히지 않는다.
  적 반경은 부르는 쪽이 `radius` 에 더한다.
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
- "맵 안" 은 바깥 마름모 안이다(4.1). 마름모 아레나에서는 주인공이 모서리 쪽에 있으면 고리의 상당 부분이 맵 밖이라 반경 줄이기가 자주 일어난다 — 그래도 규칙은 같다(기획 2.4).

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
- 무기 발동 시 주인공이 서 있으면 그 무기 정의의 `cast_clip`(`cast_fire`·`cast_lightning`·`cast_magic` — 원작 마법사 `fm`·`lm`·`qm`, 기획 4.0·5.1) 클립 재생(재생 중이면 그대로, 움직이면 바로 걷기).
  애니메이션은 주인공 상태라 렌더가 그냥 읽는다. 발동 한 번에 신호 `WeaponFired` 하나(7.9, 여러 발이어도 하나).
- 진화 무기의 추가 효과(`on_hit`/`on_explode`/`on_crit`)는 `PartKind.WeaponEffect` 부품 목록이다(6.8). 투사체·장판이 그 순간에 목록 순서대로 부른다.

### 5.4 투사체

- `Projectile`(풀): 정의, 진영(`Faction`), 위치·이전 위치, 속도 벡터, 반경, 남은 수명/거리, 관통 남은 수, 피해(주인공 쪽: 원값·`DamagePct`·치명 확률 / 적 쪽: 최종 피해), 넉백,
  출처(주인공 무기 칸 / 쏜 적 ID), 대상(유도·튕김), **맞은 적 목록**.
- **궤적**은 전략이지만 뜨거운 반복이라 가상 호출 대신 `MotionKind` 열거형 + `switch` 로 둔다: `Straight`, `Homing`(회전율 제한), `Orbit`(주인공 기준 각도), `Ricochet`(맞으면 다음 대상).
  새 궤적 = 열거형 값 하나 + 함수 하나. 투사체 정의의 `turn_rate`·패턴이 이것을 고른다.
- **맞은 적 목록**: 고정 크기 16칸 인라인 배열(`InlineArray`)의 `EntityId`. 관통·튕김이 같은 적을 두 번 때리지 않게 한다(영혼 사냥꾼 최대 12 포함). 넘치면 가장 오래된 것부터 덮는다.
- **맞음 판정**: `ISpatialQuery.QueryCapsule(이동 전, 이동 후, 투사체 반경 + 최대 적 반경)` → 선분-원 최소 거리 ≤ 투사체 반경 + 적 반경. 빠른 투사체(520px/s = 틱당 8.7px)도 건너뛰지 않는다.
  같은 틱에 여러 적이 걸리면 선분 시작에서 가까운 순으로 관통 수만큼.
- **장애물**: `blocked_by_obstacles` 이면 같은 선분으로 `ObstacleIndex` 검사(투사체 막힘 도형만 — `W` 칸은 넘어감. MVP 맵의 둘레 턱은 `X` 라 막힌다, 4.2) → 닿으면 폭발/사라짐.
- **끝날 때 장판**(`on_end`, 6.4): 맞음·사거리 끝·장애물 어느 쪽으로 끝나도 그 자리에 장판 하나(산성 침 웅덩이). 피해는 발사 순간에 굳힌 값.
- **그림 방향**: `Vel` 의 방향으로 시트 열을 고른다(4.1, 16방향). 도는 구체는 접선 방향을 `Vel` 에 쓴다(기획 5.2).
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

- 폭발 그림·맞음 불꽃·레벨업 빛 기둥·처치 연출처럼 **규칙에 영향이 없는 것**은 시뮬레이션에 두지 않는다. 시뮬레이션은 `PresentationBuffer` 에 `FxEvent(Tick, Def, Pos, Scale, Tint, Radius, Dir)` 를 넣고,
  게임 쪽 `FxSystem` 이 풀에서 꺼내 재생·빛·흔들림을 처리한다(나이 = 현재 스테이지 틱 − `Tick`, 3.4). 크기는 `Radius > 0` 이고 정의가 `fit_radius` 면 `Radius / fit_radius`, 아니면 정의 `scale`, 거기에 `Scale` 을 곱한다.
  반복 이펙트(장판 불꽃·산 웅덩이)는 `Hazard` 가 살아 있는 동안 렌더가 직접 그린다(`intro` → 반복, `scatter`, 6.4).
- 적의 **죽음 애니메이션과 시체**도 표시 쪽이다: 시뮬레이션은 죽은 적을 바로 지우고 표시 이벤트 `CorpseSpawned` 를 낸다
  (WP1 코드: `Tick, Sprites, Pos, Facing, Tint, Scale, Alpha, GlowColor, GlowAlpha, Death` — v0.3 에서 `Enemy`(`EnemyDef`, 죽음 소리)·`Variant`(변종 시트)를 더한다, 계약 변경 WP1b).
  `Culled`(상한 정리)는 시체를 내지 않는다. 렌더의 `CorpseLayer` 가 죽기 클립 재생 → 2초 → 0.5초 투명, 상한 150. → 시뮬레이션 개체 수가 줄고, 시체 상한이 규칙과 섞이지 않는다.
- 이름: 틱 이벤트(시뮬레이션 안) = `EnemyKilled(Id, DefIndex, Pos, Rank, Cause, Attacker)`, 표시 이벤트(화면) = `CorpseSpawned`. 한 사건에 이름 하나씩이다.
  표시 이벤트 전체: `FxEvent`·`DamageNumber`·`CorpseSpawned`·`ShakeEvent`·`CueEvent`(v0.3, 7.9). 알림 문구는 표시 이벤트가 아니라 `World.Banner`(`BannerState`) 하나다 — WP4 가 쓰고 WP6 HUD 가 읽는다.
- 주인공 죽음(원작 마법사 죽기 20프레임, 기획 4.0)은 결과 화면 시점이 걸려 있어 시뮬레이션 상태로 둔다.

---

## 6. 데이터 스키마

### 6.1 형식과 파일 배치 (기획 열린 질문 4 의 답: JSON)

- `src/DiabloDefenceRemastered/Data/` 아래 `**/*.json` 을 모두 읽는다(경로 서수 정렬, 3.4). 파일 하나는 **종류 이름을 키로 하는 객체**이고, 한 파일에 여러 종류를 담을 수 있다.

```json
{ "enemies": [ … ], "projectiles": [ … ], "effects": [ … ] }
```

  종류 키: `rules`(객체, 아래), `sprites`, `sprite_sets`, `effects`, `projectiles`, `tilesets`, `maps`, `enemies`, `affixes`, `buffs`,
  `weapons`, `passives`, `evolutions`, `pickups`, `drop_tables`, `stages`, `sounds`, `music`, `sound_bindings`(v0.3: `tilesets`·소리 셋 추가, `obstacle_kinds` 삭제).
- `rules` 는 **여러 파일에서 하위 키 단위로 합친다**: `rules/hero.json` = `{ "rules": { "hero": …, "xp_curve": …, "level_up": … } }`,
  `rules/enemies.json` = `{ "rules": { "ranks": …, "enemies": …, "caps": …, "spawn": … } }`, `rules/audio.json` = `{ "rules": { "audio": … } }`. 같은 하위 키가 두 파일에 있으면 오류. 그래서 두 묶음이 같은 파일을 고치지 않는다.
- 권장 배치(소유 묶음은 11절):

```
Data/
  rules/hero.json  rules/enemies.json  rules/audio.json     주인공·레벨업 / 등급·상한·생성 / 소리 규칙
  sprites/monsters.json hero.json missiles.json ui.json     sprites + sprite_sets
  effects/common.json           fx_warning_*, fx_levelup_pillar …
  tilesets/l1.json  l2.json     타일 번호·성질 표
  maps/cathedral_arena.json  catacombs_arena.json           배치·입구·기준점
  audio/sounds.json  audio/bindings.json                    소리·음악 / 연결 표
  weapons/<무기>.json           무기 1개 + 그 투사체·이펙트 (+ 진화 무기)
  passives.json  evolutions.json  pickups.json  drop_tables.json
  enemies/<적>.json             적 1개 + 그 적만 쓰는 투사체·이펙트
  enemies/bosses/<보스>.json
  affixes.json  buffs.json
  stages/<스테이지>.json
```

- 이름 규칙: 키는 `snake_case`(기획서 ID 그대로). 색은 `[r,g,b]`, 점은 `[x,y]`, 사각형은 `[x0,y0,x1,y1]`(최소·최대, 기획 "x 1325~1450, y 418~478" 과 같은 순서),
  원은 `[cx,cy,r]`, 시각은 `"mm:ss"`, 맵 위치는 하위 타일 좌표 `[tx,ty]`(실수, 칸 중심 = 정수, 형은 `TileCoord` — 4.1), 프레임 번호는 **1부터**(기획서 "7번째 프레임" 과 같게).
  가져오기 출력의 프레임 번호(`arena.json` 의 `cells` 등)도 1부터다.
  정의·부품 매개변수·`AnimClock.Advance(…, eventFrame1)` 모두 1부터 그대로 넘긴다 — 로더도 부르는 쪽도 1을 빼지 않는다. 0부터인 것은 `AnimClock.Frame()` 의 결과(시트 안 칸 번호)뿐이다(WP1 코드).
- 같은 뜻은 어디서나 같은 이름: 예고는 `"telegraph": { "effect": …, "time": … }`, 거리 범위는 `"range_band": [최소, 최대]`, 한 값 사거리는 `"range"`.
- System.Text.Json: `JsonNamingPolicy.SnakeCaseLower`, 주석·끝 쉼표 허용, **모르는 속성은 오류**(`JsonUnmappedMemberHandling.Disallow` — 오타를 잡는다).
  이 옵션과 `Vector2`·`TileCoord`·`Rgb`·`RectI` 변환기는 Core `Data/Json/DefJson`(WP1b) 한 곳에 두고 로더(WP7)·`MapDataReader`·`ImportTable`(도구)이 같이 쓴다.

### 6.2 로딩 순서

```
1. 파일 목록(서수 정렬)·JSON 파싱(JsonNode)   문법 오류 → DataError, 그 파일만 빠짐
2. 종류별로 모으기, ID 중복 검사, rules 하위 키 합치기
3. extends 병합 (JsonNode 단계)      부모 먼저, 순환 검사. 객체는 깊은 병합, 배열·값은 통째 교체.
                                     이어서 *_add(배열 뒤에 붙이기), *_patch(원소의 id 로 찾아 깊은 병합). 메타 키(extends, abstract, *_add, *_patch) 지우기.
                                     페이즈: 정의 patterns 에서 시작해 첫 페이즈부터 그 페이즈까지 patch → add → remove 를 누적 적용한 완성 패턴 JSON 을 페이즈마다 만든다
4. 형 변환 — 정의 항목마다 따로       JsonNode.Deserialize<T>. 부품·이벤트 원소는 PartRegistry 가 (종류, "type") 으로 매개변수 형을 골라
                                     "type"·"id"·"trigger" 를 뺀 나머지를 역직렬화. JsonException.Path → DataError 의 JSON 경로.
                                     3단계의 페이즈별 완성 패턴도 같이 바꿔 PhaseDef.Patterns 에 넣는다(WP4 는 이것만 읽는다)
5. 연결 (DefRef<T> 채우기, 한 번만)  없는 ID → "비슷한 ID" 제안(편집 거리 ≤ 2)과 함께 오류. 객체 가중치 → JSON 순서 배열
6. 의미 검사                          13.5 규칙 1~3, 7(정의끼리), 8, 9 + 부품 자체 검사 + 6.14 추가 규칙(변종·클립·배치 이름)
7. 등록된 IDataValidator 들           기하·길 검사 13.5-4~6(WP2, 늘), 레벨 표 13.5-2(WP3), 소리 연결 표(WP8),
                                     자원 대조 13.5-7(WP5, 자원이 있을 때만) — 로더는 구체 검사기를 모른다
→ 결과: GameDefs + IReadOnlyList<DataError>
```

- 항목 단위로 역직렬화하는 이유: STJ 는 한 객체의 첫 오류에서 멈추므로, 항목마다 따로 해야 "모두 모은다" 가 된다.
- 변환기: 공용(`Vector2`, `TileCoord`, `Rgb`, 사각형 `RectI`)은 Core `Data/Json`(WP1b). 나머지(도형 `polygon`/`rect`/`circle`/`circle_at`+`r`, 시각 `"mm:ss"`, 레벨 표 값 `float?`,
  `DefRef<T>` 문자열 ↔ 참조 `JsonConverterFactory`)는 WP7.
- **`tilesets`·`maps` 는 `MapDataReader`(Core `Map/`, WP1b)가 읽는다**: 로더는 1~2단계에서 이 두 종류의 원소를 모아 `MapDataReader` 에 넘기고(파일·JSON 경로는 그대로 → 오류 위치 같음),
  결과 `TilesetDef`·`MapDef`(타일셋 연결까지)를 그대로 `GameDefs` 에 넣는다. 두 종류에는 `extends`·`*_add`·`*_patch` 가 없다(쓰면 오류). 가져오기 도구는 `MapDataReader.Read(dataDir)` 로
  `Data/**/*.json` 에서 이 두 종류만 읽는다 → 도구와 게임이 같은 함수로 같은 record 를 만든다(1.4).
- 오류가 있어도 끝까지 돌아 **모두 모은다**. `DataError(File, JsonPath, Message, Severity)` — 예:
  `enemies/burning_archer.json $.enemies[0].behaviors[1].projectile: 'pj_fire_arow' 가 없다 (혹시 'pj_fire_arrow'?)`
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

### 6.3 그림 `sprites`, 그림 묶음 `sprite_sets` (v0.3: 원작 시트·TRN 변종)

그림은 두 가지 출처다. **`atlas`** = 가져오기 도구가 원작 디아블로 1 에서 만든 시트(PNG 알파, 프레임마다 잘라 묶음, 변종 파일), **`grid`** = 원작 exe PNG(v0.2 그대로, 색 키).

```json
{
  "sprites": [
    { "id": "zombie_walk",   "source": "atlas", "file": "d1/monsters/zombie.png", "action": "walk",
      "frame": [128, 96], "directions": 8, "frames": 24, "origin": [64, 80] },
    { "id": "zombie_attack", "source": "atlas", "file": "d1/monsters/zombie.png", "action": "attack",
      "frame": [128, 96], "directions": 8, "frames": 12, "origin": [64, 80] },
    { "id": "fireba",        "source": "atlas", "file": "d1/missiles/fireba.png",
      "frame": [96, 96], "directions": 16, "frames": 14, "origin": [48, 48] },
    { "id": "icon_magic_missile", "source": "grid", "file": "d1/icons/magic_missile.png", "sheet": [48, 48], "frame": [48, 48], "color_key": null },
    { "id": "Gold",          "source": "grid", "file": "Gold.png", "sheet": [96, 1600], "frame": [96, 160],
      "directions": 1, "frames": 10, "color_key": [0, 0, 0], "origin": [48, 150] },
    { "id": "LOGO",          "source": "grid", "file": "LOGO.png", "sheet": [550, 3240], "frame": [550, 216],
      "directions": 1, "frames": 15, "color_key": [0, 255, 0], "origin": [275, 108] }
  ],
  "sprite_sets": [
    { "id": "set_zombie", "base_speed": 40, "base_radius": 14, "frame_time": 0.05,
      "variants": ["", "bluered", "grey", "yellow", "eth"],
      "clips": {
        "idle":    { "sprite": "zombie_idle", "frame_time": 0.2 },
        "walk":    { "sprite": "zombie_walk" },
        "attack":  { "sprite": "zombie_attack", "event_frame": 8 },
        "special": { "sprite": "zombie_special" },
        "die":     { "sprite": "zombie_die" } } },
    { "id": "set_sorcerer", "base_speed": 95, "base_radius": 14, "frame_time": 0.05,
      "variants": [""],
      "clips": { "idle": { "sprite": "slt_idle" }, "walk": { "sprite": "slt_walk" },
                 "cast_fire": { "sprite": "slt_cast_fire", "event_frame": 8 }, "cast_lightning": { "sprite": "slt_cast_lightning", "event_frame": 8 },
                 "cast_magic": { "sprite": "slt_cast_magic", "event_frame": 8 }, "die": { "sprite": "sln_die" } } }
  ]
}
```

(숫자는 모양을 보이기 위한 예다. 프레임 수·칸은 원작 파일 값, 프레임 시간·이벤트 프레임은 원작 게임 동작 값을 WP5 가 손으로 옮겨 적는다 — 기획 8.1.
원본 유래 수치를 두는 곳은 이 그림 묶음 클립 한 곳이고, 값마다 `// 출처: 원본 확인` / `// 출처: DevilutionX 참고` 주석을 단다(1.4 "원본에서 온 수치", R24).)

- **`source: "atlas"`**
  - `file` = 그림 묶음의 원색(변종 `""`) **페이지**(모든 동작이 한 장, 1.4). `action` = 그 페이지 메타 안의 동작 이름(메타에 동작이 하나뿐이면 생략 — 마법).
    변종 페이지는 `<이름>@<변종>.png`(`zombie@bluered.png`). 메타는 같은 이름의 `.json`(`zombie.json`) 하나를 모든 변종이 같이 쓴다
    (TRN 은 색만 바꾸고 투명은 RLE 가 정하므로 잘라 낸 사각형이 같다).
  - `frame` = **칸**(원작 프레임 폭 × 그 동작의 최대 높이). `directions`·`frames` = 원작 파일 값(정의에 적고, 자원이 있으면 메타와 대조 — 13.5-7).
    `sheet`·`color_key`·`slack` 를 쓰면 오류(시트 크기는 메타의 페이지가 정한다). 이 검사는 **JsonNode 단계에서 속성이 있는지**로 한다(값이 `null` 이어도 오류).
  - **메타 `SpriteMeta`**(Core `Assets/`, 가져오기 도구가 쓰고 렌더·시험이 읽음):
    `{ "format": 2, "pages": [[W,H], …], "actions": { "walk": { "cell": [w,h], "directions": d, "frames": f, "rects": [[p, x, y, w, h, ox, oy], …] }, … } }` —
    `rects` 는 (방향 0 의 프레임 1..f, 방향 1 …) 순서, `p` = 페이지 번호(0 = `<이름>.png`, 1 = `<이름>_p2.png` …), `x,y,w,h` = 페이지 안 사각형, `ox,oy` = 칸 왼쪽 위에서 그 사각형까지(잘라 낸 위치).
    빈 프레임은 `w = h = 0`.
  - 그리기: 화면 왼쪽 위 = `위치 − origin × scale + (ox, oy) × scale`, 원본 사각형 = `(x, y, w, h)`. 프레임마다 크기가 달라도 칸 기준 `origin` 하나로 발이 맞는다.
  - 한 묶음의 모든 동작이 한 텍스처라, 같은 종·같은 변종이면 걷기·공격·죽기 중인 개체가 섞여 정렬돼도 텍스처가 바뀌지 않는다(7.4 그리기 호출).
- **`source: "grid"`**(원작 exe PNG, 그리고 가져오기 도구가 만든 아이콘처럼 한 장짜리): v0.2 그대로 — `sheet` = 정의끼리 검사용 크기, `slack` = 남는 픽셀 허용, 열 = 방향, 행 = 프레임(brief 4절).
  `color_key` 속성이 **없으면** 로더가 검정 `[0,0,0]` 을 채운다(v0.2 기본 그대로), `"color_key": null` 을 **적으면** PNG 알파를 그대로 쓴다(가져오기가 만든 아이콘).
  `SpriteDef.ColorKey` 는 nullable(null = PNG 알파)이고, "없음 → 검정" 은 JsonNode 단계에서 로더가 정한다.
- `origin` = **칸 안** 발 픽셀(잘라 내기 전 칸 기준이라 메타와 상관없다). 첫 값은 (칸 폭/2, 칸 높이 − 16)(기획 8.1), `--preview sprites` 로 맞춘다(기획 열린 질문 7).
  방향마다 다르면 `origin_by_dir: [[x,y] × 방향 수]`.
- 방향 수: 몬스터·주인공 8, 방향 있는 마법 16(뼈 영혼 8), 방향 없는 마법·아이콘·획득물 1. 열 고르기는 4.1(등각 보정).
- **그림 묶음 `variants`**: 이 묶음에 쓰는 원작 TRN 변종 이름 목록(`""` = 원색). 적 정의의 `variant` 는 이 안에 있어야 한다(오류). 이름은 가져오기 표의 이름(원작 TRN 파일 이름 —
  `white`, `bluered`, 고유 몬스터 `eth`·`bhsm`·`genrl`)이고, 경로는 표만 안다.
- **클립**: `idle`(n) `walk`(w) `attack`(a) `special`(s) `die`(d), 주인공은 `cast_fire`(fm) `cast_lightning`(lm) `cast_magic`(qm). v0.2 의 `cast` 는 `special` 로 바꿨다(몬스터의 시전·함성·소환은 원작 특수 그림 — 계약 변경 WP1b).
  맞기(h)는 쓰지 않는다(기획 8.1). 클립 `frame_time` = 원작 동작별 재생 간격 × 50ms, 없으면 묶음 `frame_time`. `event_frame` = 타격·발사 프레임 기본값(1부터, 적 정의·부품이 덮어씀).
- `base_speed` = 걷기 재생 속도 기준(재생 배율 = 땅 기준 속도 `|(vx, 2·vy)|` / base_speed — 4.1. 화면 가로로 걸을 때 실제 속도 / base_speed 와 같다). `base_radius` = 크기 배율 1 일 때 몸 반경(기획 8.1 표: 해골 12, 좀비 14, 폴른 원 10, 핀드 10, 염소인간 14,
  염소인간 활 13, 산성 짐승 14, 오버로드 16, 도살자 20, 해골왕 30) — 적 정의가 `radius` 를 생략하면 `round(base_radius × scale)`(6.5).
- 클립 진행(프레임 시간·재생 배율·이벤트 프레임을 이번 틱에 넘었나)은 Core `AnimClock` 한 곳이 계산한다. 주인공·적·미리보기·시체가 같은 함수를 써서 타격 프레임과 그림이 어긋나지 않는다.
- 소리는 그림 묶음에 두지 않는다. 소리 연결 표(6.15)가 그림 묶음 ID 로 가리킨다.

### 6.4 이펙트 `effects`, 투사체 `projectiles`

```json
{
  "effects": [
    { "id": "fx_fire_explosion", "sprite": "bigexp", "frames": [1, 16], "loop": false, "frame_time": 0.05,
      "fit_radius": 60, "blend": "alpha",
      "light": { "color": [255, 160, 80], "radius_mul": 1.5, "duration": 0.3 },
      "shake": { "px": 1, "duration": 0.08 } },
    { "id": "fx_hellfire_pool", "intro": { "sprite": "firewal1", "frames": [1, 13] },
      "sprite": "firewal2", "frames": [1, 11], "loop": true, "frame_time": 0.05, "scale": 0.5,
      "scatter": { "per_radius": 20, "min": 3, "center": true },
      "light": { "color": [255, 110, 60], "radius_mul": 1.2 } },
    { "id": "fx_warning_circle", "procedural": "warning_circle", "tint": [255, 40, 40], "alpha": 0.35 }
  ],
  "projectiles": [
    { "id": "pj_fireball", "sprite": "fireba", "scale": 0.7, "blend": "alpha",
      "radius": 8, "speed": 380, "turn_rate": 0, "range": 450, "blocked_by_obstacles": true, "pierce": 1,
      "explosion": { "damage_mul": 1.0, "effect": "fx_fire_explosion" } },
    { "id": "pj_acid", "sprite": "acidbf", "scale": 0.7, "radius": 7, "speed": 200, "range": 340,
      "hit_effect": "fx_acid_hit",
      "on_end": { "effect": "fx_acid_pool", "radius": 35, "duration": 2.5, "tick": 0.5, "damage": 3, "cap_group": "acid", "cap": 20 } }
  ]
}
```

- 이펙트 크기: `scale`(고정 배율) 또는 `fit_radius`(배율 1 일 때 덮는 반경 px → 실제 배율 = 원하는 반경 / fit_radius). 둘 중 하나.
- **`intro`**(v0.3, 계약 변경): 반복 이펙트의 "솟음" 구간 — `intro` 프레임을 한 번 재생한 뒤 본 `sprite`·`frames` 를 반복한다(화염벽 `firewal1` → `firewal2`, 산 웅덩이 `acidpud1` → `acidpud2`).
- **`scatter`**(v0.3, 계약 변경): 장판 안에 조각을 여러 개 찍는다(기획 13.1·13.7). 개수 = `max(min, round(반경 / per_radius))` + (`center` 면 가운데 1). 자리 = `StableHash(장판 ID, i)` 로 반경 안에
  고른다(표시만, 시뮬레이션과 무관). 장판이 하나여도 조각마다 시작 프레임을 해시로 어긋나게 해 같은 순간에 같은 그림이 겹치지 않게 한다.
- `blend` 기본은 `alpha`(원작처럼 보통 혼합, 기획 5.1). 가산 후보는 `--preview blend` 로 무기마다 나란히 보고 정의에서 고른다(기획 열린 질문 10).
- `procedural`: 코드로 그리는 예고(원·선). 같은 ID 체계라 패턴이 똑같이 참조한다.
- 폭발 반경: 쏘는 쪽(무기 레벨 표 `radius`, 보스 패턴 매개변수)이 주면 그 값, 아니면 정의의 `explosion.radius`. 둘 다 없으면 검증 오류.
- 참조하는 쪽은 `{ "effect": "fx_bolt_hit", "tint": [255,70,70], "scale": 0.25 }` 처럼 **색조·크기만** 덮어쓸 수 있다(기획 13.7). 다른 키는 오류.
- 투사체 그림 방향 = **속도 방향**(`Projectile.Vel`, 16방향 시트면 4.1 의 열). 도는 구체·유도 투사체도 WP3 이 매 틱 움직이는 방향을 `Vel` 에 쓴다(기획 5.1, 도는 구체는 접선).
- **`on_end`**(v0.3, 계약 변경, 기획 8.1 산성 침): 투사체가 끝날 때(맞음·사거리 끝·장애물) 그 자리에 장판을 남긴다. 매개변수는 `ground_hazard` 와 같은 모양이고,
  피해는 쏜 적의 배율로 발사 순간에 굳힌다(5.2). 처리는 WP3 `Projectiles` 단계.
- v0.2 의 원작 exe 그림 이펙트(`fx_mm_hit`, `fx_knight_death` 등)는 없앴다(기획 13.7).

### 6.5 몬스터 `enemies`

**일반 적의 틀과 변형** (그림 = 원작 그림 묶음 + 원작 TRN 변종, 그 위에 덧입힘 — 기획 8.1)

```json
{
  "enemies": [
    { "id": "skeleton", "name": "해골", "tags": ["skeleton", "undead"],
      "sprites": "set_skelaxe", "variant": "white", "tint": [255, 255, 255], "scale": 1.0, "alpha": 1.0,
      "hp": 10, "speed": 70, "damage": 5, "armor": 0, "knockback_resist": 0, "xp": 1,
      "movement": "flow",
      "behaviors": [ { "id": "melee", "type": "chase_melee", "cooldown": 1.0 } ],
      "death": "anim", "drop_table": "dt_common" },

    { "id": "burning_dead", "extends": "skeleton", "name": "불타는 해골", "tags": ["skeleton", "undead", "fire"],
      "variant": "", "glow": { "color": [255, 120, 40], "alpha": 0.2 },
      "hp": 8, "speed": 100, "damage": 15, "xp": 1,
      "behaviors": [
        { "type": "self_destruct", "trigger_range": 10, "fuse": 0.6, "radius": 60,
          "effect": { "effect": "fx_fire_explosion", "scale": 0.8 }, "fizzle_effect": "fx_spark" } ] },

    { "id": "mud_man", "name": "머드맨", "tags": ["demon"], "sprites": "set_fat", "variant": "blue", "scale": 1.15,
      "hp": 110, "speed": 45, "damage": 18, "armor": 2, "knockback_resist": 0.7, "xp": 8,
      "behaviors": [],
      "patterns": [ { "id": "slam", "range_band": [0, 70], "cooldown": 2.4, "clip": "attack",
                      "telegraph": { "effect": "fx_warning_circle", "time": 0.5 },
                      "actions": [ { "type": "melee_slam", "radius": 60, "knockback": 50, "shake": { "px": 2, "duration": 0.15 } } ] } ] }
  ]
}
```

- **그림**: `sprites` = 그림 묶음 ID, `variant` = 그 묶음의 원작 TRN 변종 이름(`""` = 원색, 기본). 같은 묶음 + 다른 변종이면 다른 시트 파일이다(7.5).
  `tint`·`glow`·`alpha`·`scale` 은 **덧입힘**이다 — 일반 몬스터는 될 수 있으면 원작 TRN 만 쓰고(기획 8.1), 정예·우두머리·접두사 표시와 원작에 없는 변형(분열체 ×0.65)에 쓴다.
  **최종 크기**(생성 때) = `min(정의 scale × 등급 배율, rules.ranks.max_scale)` — 기획 8.1(v0.5): 크기 범위 0.65~1.6 은 정의 크기 × 등급 배율(정예 ×1.15, 우두머리 ×1.3)에 걸고,
  1.6 을 넘으면 1.6 으로 자른다(썩은 시체 우두머리 1.35 × 1.3 = 1.755 → 1.6). 검사는 경고(기획 13.5-14): 정의 `scale` 이 0.65~1.6 밖이거나,
  **정의 `scale` × 그 적이 받을 수 있는 가장 큰 등급 배율**(정예 대상이면 `rules.ranks` 의 우두머리 배율)이 `max_scale` 을 넘으면 — 넘는 쪽은 실제로 잘린다는 알림이다.
- **몸 반경**: 그림 묶음의 `base_radius` × `scale` 을 반올림(`radius` 생략 시). 위 `mud_man` 은 `round(16 × 1.15) = 18`. 값을 직접 적을 수도 있고(중간 보스·보스처럼 기획 표 값을 그대로 쓸 때),
  계산값과 2 넘게 다르면 경고다. → "`extends` + `scale` 만 바꾸기" 변형이 부모 반경을 물려받아 충돌·필드 등급이 조용히 틀어지는 일이 없다.
  생성 때 몸 반경은 **자른 최종 크기**로 다시 센다: `round(base_radius × 최종 크기)`(정의가 `radius` 를 직접 적었으면 `round(radius × 최종 크기 / scale)`). 썩은 시체 우두머리 = `round(14 × 1.6) = 22`(기획 8.1).
- `melee_slam` 의 피해를 생략하면 그 적의 `damage` 를 쓴다(일반 적 패턴은 시간 배율을 받는 적 피해, 보스 패턴은 표 값). 이벤트 프레임을 생략하면 그림 묶음 클립의 `event_frame`(원작 표 값).
- `behaviors` 는 `extends` 에서 **통째로 교체**된다(배열 규칙). 부모 부품에 하나만 더하려면 `"behaviors_add": [ … ]`, 부모 부품의 매개변수 몇 개만 바꾸려면
  `"behaviors_patch": { "<부품 id>": { "cooldown": 0.7 } }`(부품의 선택 `id` 로 찾아 깊은 병합, 없는 id 는 오류). 패턴·페이즈도 `patterns_add`/`patterns_patch`/`phases_patch` 로 같다.
- `movement`: `flow` / `astar` / `direct`. 위치 잡기 부품(`kite`, `phase_through`)이나 이동 지시(6.6)가 없으면 기본 "주인공 쪽으로"(`SeekHero`).
- `rank`: 정의 자체의 등급 `normal`(기본) / `miniboss` / `boss`. 정예·우두머리는 정의가 아니라 생성 때 붙는 등급이다(6.7).
  `miniboss`·`boss` 는 상한 정리에서 지우지 않고, 보스 막대·처치 연출·드롭 규칙을 고른다.
- `elite_eligible`: 기본 `true`. 확률 정예 무리의 종류 뽑기에서 뺄 적은 `false`(`skeleton_captain`, `corpse_spawn`, 기획 9.1). `miniboss`·`boss` 는 늘 빠진다.
- `spawn_group: [최소, 최대]`(선택): 일반 생성에서 이 종류가 뽑히면 무리 크기를 `rules.spawn.group`(1~3) 대신 이 값에서 뽑는다(폴른 원 4~6, 핀드 3~5, 불꽃 부족 5~7). 감독은 종류를 먼저 뽑고 크기를 뽑는다.
- `death`: `anim`(죽기 클립 재생)만 남았다. v0.2 의 `knight_fade`(원작 exe 검사 그림)는 없앴다(계약 변경 WP1b).
- `anim_speed`(선택, 기본 1): 이 적의 모든 클립 재생 배율. 공격 쿨이 공격 클립 길이보다 짧으면 그 공격만 클립을 쿨에 맞춰 빠르게 재생한다(쿨은 **공격 시작부터** 센다, 기획 8.1).
- 보스·중간 보스용: `enrage: { "after": "03:00", "damage_mul": 2, "speed_mul": 1.3 }`(등장 시각부터 셈), `arrival: { "banner": "해골왕이 깨어났다", "time": 2.5, "shake": { "px": 4, "duration": 0.5 } }`.
  스테이지마다 다르게 하려면 `boss`/`miniboss` 이벤트가 같은 이름의 매개변수로 덮어쓴다. 등장 대사 소리는 신호 `BossArrived`(7.9) → 소리 연결 표의 `enemies.<id>.voice`.

**한 파일에 적 + 투사체 (불타는 해골 궁수)**

```json
{
  "enemies": [
    { "id": "burning_archer", "extends": "skeleton_archer", "name": "불타는 해골 궁수", "tags": ["skeleton", "undead", "ranged", "fire"],
      "variant": "", "glow": { "color": [255, 120, 40], "alpha": 0.2 },
      "hp": 18, "speed": 60, "damage": 9, "xp": 3,
      "behaviors": [
        { "type": "kite", "range_band": [240, 340] },
        { "type": "shoot_projectile", "projectile": "pj_fire_arrow", "clip": "attack",
          "cooldown": 3.0, "cooldown_jitter": 0.4, "range": 340 } ] }
  ],
  "projectiles": [
    { "id": "pj_fire_arrow", "sprite": "farrow", "scale": 0.9, "radius": 5, "speed": 240, "range": 380,
      "blocked_by_obstacles": true, "hit_effect": "fx_spark" }
  ]
}
```

(`skeleton_archer` 는 `set_skelbow` · `white`. 불타는 해골 궁수는 같은 묶음의 원색이라 무기 그림(활)과 색으로 해골 궁수와 구별된다 — 기획 8.2.)

**지휘 + 호위, 분열, 죽은 자리 장판, 돌진, 동료 죽음 공포, 날기** (부품만 발췌)

```json
{ "type": "command_aura", "radius": 200, "target_tag": "skeleton", "buff": "command" }
{ "type": "escort", "default_count": 8, "escort_enemy": "skeleton", "formation_radius": 120, "break_range": 250, "fear_on_leader_death": 1.5 }
{ "type": "split_on_death", "spawn": "corpse_spawn", "count": 3, "throw": 80, "delay": 0.3 }
{ "type": "ground_hazard", "trigger": "on_death", "effect": "fx_poison_pool", "radius": 50, "duration": 3.0,
  "tick": 0.5, "damage": 4, "cap_group": "poison", "cap": 20 }
{ "type": "charge", "range_band": [0, 260], "telegraph": { "effect": "fx_warning_line", "time": 0.5 },
  "speed": 280, "time": 1.0, "stun": 0.8, "cooldown": 4.0, "damage": 10 }
{ "type": "panic_on_ally_death", "radius": 150, "fear": 0.8, "rearm": 1.5, "same_def": true }
{ "type": "phase_through", "alpha_over": 1.0 }
{ "type": "war_cry", "cooldown": 15.0, "clip": "special", "radius": 250, "buff": "war_cry", "duration": 6.0 }
```

- **오라·함성·범위 버프는 보스를 거르지 않으면 보스에게도 걸린다**: `command_aura`·`war_cry`·`buff_area` 는 매개변수 `include_bosses`(기본 `false`)를 가진다.
  `false` 면 정의 `rank` 가 `miniboss`·`boss` 인 적에게는 걸지 않는다(대장 오라가 해골왕의 내려치기를 30 → 37.5 로 바꾸는 일을 막는다). 정예·우두머리(생성 등급)는 받는다.
  지휘 오라는 기획 8.2 가 대상 종(해골·해골 궁수·불타는 해골·불타는 해골 궁수·다른 해골 대장)을 정해 보스·중간 보스 본체에 걸리지 않는 것이 확정이다.
  전투 함성(`war_cry`, 오버로드 반경 250)·스틸메이스 함성(`buff_area`, 반경 300)이 중간 보스·보스 본체에 걸리는지는 기획서에 아직 없다 — 그때까지 `false` 가 설계 기본이다(기획 열린 질문 21, 13.6). `[해석]`
- 공포는 `escort` 부품의 규칙이다: 대장이 죽으면 **그 대장을 `Leader` 로 가진 적만** `fear_on_leader_death` 초 동안 도망간다(기획 8.3). 오라 반경 안의 다른 해골이나, 호위 없이 나온 대장 주변의 적은 영향이 없다.
- `panic_on_ally_death`(기획 v0.4 새 부품, 폴른 원, WP4): 지난 틱 `EnemyKilled`(`Cause == Damage`) 중 같은 정의·반경 안이 있으면 `Order = Flee(fear)`, 다시 겁먹기까지 `rearm`. 정예·우두머리는 겁먹지 않는다.
  공포 동작은 호위 공포와 같은 `Flee` 지시다(6.6).
- `phase_through.alpha_over`(기획 v0.4): 장애물 위에서의 알파(기본 0.3, 핀드 1.0 — 박쥐는 날므로 흐려지지 않음).
- `ground_hazard` 는 `EnemyAction` 으로 한 번만 등록된다. `behaviors` 안에서 `trigger` 가 붙으면 훅 어댑터가 감싼다(6.6) — 보스 패턴의 동작과 같은 코드다.

**보스 (패턴·페이즈)**

```json
{
  "enemies": [
    { "id": "skeleton_king", "name": "해골왕", "tags": ["undead", "boss"],
      "sprites": "set_sking", "variant": "genrl", "hp": 8000, "speed": 55, "damage": 30, "armor": 0,
      "knockback_resist": 1.0, "xp": 0, "movement": "astar",
      "immunities": ["knockback", "slow"], "show_boss_bar": true, "time_scaling": false, "rank": "boss",
      "enrage": { "after": "03:00", "damage_mul": 2, "speed_mul": 1.3 },
      "arrival": { "banner": "해골왕이 깨어났다", "time": 2.5, "shake": { "px": 4, "duration": 0.5 } },
      "patterns": [
        { "id": "slam", "range_band": [0, 90], "cooldown": 2.0, "clip": "attack",
          "actions": [ { "type": "melee_slam", "damage": 30, "knockback": 60, "shake": { "px": 3, "duration": 0.2 } } ] },
        { "id": "fire_fan", "cooldown": 5.0, "clip": "special", "event_frame": 8,
          "actions": [ { "id": "spread", "type": "projectile_spread", "projectile": "pj_boss_fireball", "count": 3, "arc": 40,
                         "aim": "hero", "damage": 20 } ] },
        { "id": "raise", "cooldown": 15.0, "clip": "special", "event_frame": 8,
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

스틸메이스의 "전투 함성"(기획 9.4)은 한 패턴에 동작 둘이다.

```json
{ "id": "battle_cry", "cooldown": 20.0, "clip": "special",
  "actions": [ { "type": "summon", "spawns": [ { "enemy": "goatman", "count": 4 } ], "radius": 120, "alive_cap": 30 },
               { "type": "buff_area", "radius": 300, "buff": "war_cry", "duration": 6.0 } ] }
```

도살자(기획 9.5)는 특수 그림이 없어 시체 부르기를 `"clip": "idle"` + 소환 자리 `fx_blood_burst` 로 보이고, 피의 땅은 `telegraphed_blast`(`"points": { "hero": true, "around": 2, "radius": 120 }`)다.
2페이즈에서 대사를 한 번 더 내는 것은 페이즈의 `"voice": true`(v0.3, 계약 변경) → 신호 `BossPhase`(7.9).

- (해골왕에 `skeleton` 태그를 달지 않는다 — `target_tag: skeleton` 오라·호위 규칙의 대상이 아니다. 태그는 "같은 종 규칙의 대상" 이라는 뜻으로만 단다.)
- 패턴 `range_band` 는 [최소, 최대] 거리, `hp_once` 는 HP 문턱 1회. `telegraph` 는 선택이지만 **예고 필수 조건**에 걸리는 패턴·능력은 없으면 로딩 오류다(기획 12.6·13.5-13).
  기준값은 `rules.enemies.telegraph_required` 에 둔다(6.10): `{ "min_damage": 10, "min_time": 0.4, "melee_reach_over_contact": 50, "heavy_melee_damage": 25, "heavy_melee_windup": 0.35 }`.
  "피해" 는 **기본 피해**(정의에 적은 값 — 동작의 `damage`, 생략하면 그 적의 `damage`. 시간·등급·버프·광폭화 배율 전)다. 부품 종류로 나눈다(정의끼리만 보므로 자원 없이 돈다):
  - **예고 필수** — 기본 피해 ≥ `min_damage` 인 ① 범위 즉발 피해(`radius` 가 있는 `melee_slam`, `telegraphed_blast` — 불타는 접두사의 죽음 폭발 포함, `self_destruct`) ② 돌진(`charge`·`dash`).
    `telegraph.time` ≥ `min_time` 이어야 한다. `self_destruct` 는 `fuse`(번쩍임)가 예고다(`fuse` ≥ `min_time`).
  - **예고 없어도 됨** — 근접 단일 대상(`radius` 없는 `melee_slam`, `chase_melee`)으로 닿는 거리 ≤ 자기 반경 + 주인공 반경(`rules.hero.radius`) + `melee_reach_over_contact`:
    공격 그림이 예고다. 단 기본 피해 ≥ `heavy_melee_damage` 면 공격 시작부터 이벤트 프레임까지 `(event_frame − 1) × 프레임 시간 / anim_speed` ≥ `heavy_melee_windup` 이어야 한다(해골왕 내려치기 30·도살자 식칼 35·스틸메이스 강타 25).
    투사체(`projectile_spread`·`shoot_projectile` — 폭발이 있어도 날아오는 그림이 예고)와 장판 틱(`ground_hazard`, 투사체 `on_end`)도 예고 없어도 된다.
  - 단일 대상인데 닿는 거리가 위 값을 넘는 근접은 기획 12.6 의 두 갈래에 없다 — 오류 대신 경고로 알린다(기준을 기획에 물을 자리).
- `clip`: `attack`·`special`·`idle`(기획 13.3). 쓰는 클립이 그림 묶음에 없으면 로딩 오류(도살자 `fatc` 에는 `special` 이 없다).
- `actions` 는 **같은 이벤트 프레임에 순서대로** 실행한다(시전 애니메이션 한 번, 소환과 버프가 같은 틱). 여러 틱 걸리는 동작(`dash`)은 목록의 마지막에만 올 수 있다(검증).
- `arc` = 전체 퍼짐각(±20° = 40). 360 이면 사방.
- 페이즈 `patch` 는 패턴 ID 별 깊은 병합(패턴 안 동작은 `actions_patch` 로 동작 `id` 별), `add`/`remove` 는 패턴 목록 조작.
  **로더(WP7)가** 정의 `patterns` 에서 시작해 첫 페이즈부터 그 페이즈까지 `patch → add → remove` 를 **누적** 적용하고, 부품 매개변수 역직렬화·연결까지 마친 완성 목록을
  `PhaseDef.Patterns` 에 채운다(WP1 코드). 같은 ID 패턴은 원래 자리를 지킨다. WP4 는 `PhaseDef.Patterns` 만 읽고 JSON 을 다시 풀지 않는다(6.6).
- 일반 적도 `patterns` 를 가질 수 있다(`mud_man`). 패턴과 `behaviors` 의 능력 부품은 같은 실행 경로를 탄다(6.6).

### 6.6 행동 부품 실행 모델 (WP4 소유, 이 절이 설계)

적 하나는 **이동 방식 1개 + 부품 여러 개 + 인스턴스 상태**다. 부품 객체는 정의당 하나(매개변수만 가진 불변 객체)이고, 적마다 다른 값은 슬롯·행동 상태·이동 지시에 둔다.

**부품 역할과 시그니처** (`Sim/Enemies/Parts/Contracts.cs`, WP4 소유. 레지스트리 종류는 `EnemyBehavior` / `EnemyAction`)

| 역할 | 인터페이스 | 예 |
|---|---|---|
| 위치 잡기 | `IPositioningPart` | `kite`, `phase_through` |
| 능력(독점 행동) | `IAbilityPart` | `chase_melee`, `shoot_projectile`, `self_destruct`, `charge`, `war_cry`, 모든 패턴(`PatternAbility`). 클립을 시작할 때 신호 `EnemyAttack`/`EnemySpecial`(7.9) |
| 지속 효과 | `IAuraPart` | `command_aura`, `panic_on_ally_death`(지난 틱 `EnemyKilled` 를 보고 `Flee` 지시), `PhaseController`(페이즈가 있으면 저절로 붙음) |
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
  `on_death` → `IDeathHookPart`, `on_spawn` → `ISpawnHookPart`, `cooldown` → 클립 없는 `IAbilityPart`. 접두사 `fire_enchanted` 의 `telegraphed_blast`, `black_death` 의 `ground_hazard` 가 보스와 같은 동작 코드를 탄다.
- **`PatternAbility`**(데코레이터): 조건(`range_band`·`cooldown`·`hp_once`) → 클립 재생 → 예고가 있으면 `Telegraph` 를 만들고 예고 시간 동안 클립을 이벤트 프레임 앞에서 멈춤 →
  이벤트 프레임에서 `actions` 를 순서대로 `Begin` → 마지막 동작이 `Running` 이면 `Done` 까지 `Tick`.
- **슬롯 수**: 부품 슬롯은 적마다 `PartSlots`(인라인 12칸) — 정의 부품 + 모든 페이즈 패턴의 합집합 + 접두사 부품. 12 를 넘는 정의·조합은 로딩 오류(접두사 조합은 최대 2개 기준으로 검사). 풀에서 꺼낸 적에 배열을 새로 만들지 않는다.
- **페이즈**: 덮어쓴 매개변수를 가진 완성 패턴 목록은 **로더(WP7)가** `PhaseDef.Patterns` 에 채워 둔다(6.2 3·4단계). WP4 는 그 목록과 정의 `patterns` 로
  적이 처음 쓰일 때 "페이즈마다 쓰는 패턴 = 합집합 안 슬롯 번호 배열" 표를 한 번 만들 뿐 JSON 을 다시 풀지 않는다. 전환은 "지금 쓰는 목록 번호" 하나만 바꾼다
  → 쌓인 쿨다운이 엉뚱한 패턴에 붙지 않고, 실행 중 깊은 병합·할당이 없다. 전환 연출(무적·흔들림·발광·속도, `voice` 면 신호 `BossPhase`)은 `PhaseController` 가 한다.

**인스턴스 상태 — 생성 맥락·이동 지시·관계** (기획 8.3 행진·호위·공포, 9.5 소환 상한, 2.3 상한 정리 순서)

```csharp
public enum SpawnSource : byte { Interval, ElitePack, Ring, March, Escort, EscortMember, Miniboss, Boss, EventOther, Split, Summon, Stress }
public readonly record struct SpawnContext(SpawnSource Source, EntityId Owner, int EscortCount = -1);   // 계약 (WP1). -1 = 정의 기본값

public enum OrderKind : byte { None, March, Follow, Flee }
public struct MoveOrder { public OrderKind Kind; public Vector2 Dir; public EntityId Leader; public Vector2 Offset; public int UntilTick; public Vector2? Target; }
```

- `Enemy` 에 `Source`(생성 출처), `Leader`(호위가 따르는 대장), `Summoner`(소환자), `Order`(이동 지시)를 둔다(WP4 조각, 11.2).
- **생성 훅은 출처를 본다**: `escort` 의 `OnSpawn` 은 `Source == Escort`(이벤트) 일 때만 호위를 데려온다. 수 = `EscortCount ≥ 0` 이면 그 값(이벤트 `escorts`, 0 포함), 아니면 `default_count`.
  구간 생성·정예 판정·`elite_pack` 으로 나온 대장은 호위가 없다(기획 8.3). 호위 하나하나는 `Source = EscortMember`, `Leader = 대장`, `Order = Follow(대장, 대형 자리)`, 자리는 `PlacementResolver`.
- **이동 지시**: `march` 이벤트는 `Order = March(방향, 지금 + 직진 시간)`. `toward_entry` 면 `Target` = 그 진입점(월드 px, `World.Resolved`)이고, 발이 `Target` 에서 48px 안에 들거나 지나치면
  (`Dir · (Target − 발) ≤ 0` — 분리에 밀려 그 점을 못 밟는 무리 가장자리도 풀리게) `UntilTick` 전이라도 지시를 푼다(기획 10.3 v0.5). `Follow` 는 주인공이 그 호위의 `break_range` 안에 들거나 대장이 죽으면 풀린다. 공포는 `Order = Flee(UntilTick)`.
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
  - 부품: `self_destruct` 점화 중 발광 알파 0.35↔0.9, `charge` 예고 중 붉은 `Flash`, `phase_through` 가 장애물 위(`Clearance < 0`)면 `AlphaMul` = `alpha_over`(기본 0.3, 핀드 1.0), 오라를 받는 적은 발밑 원(버프 정의의 `foot_ring`).
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
- 접두사 `exclude_tags`·`exclude_parts`: 이 태그나 부품을 가진 적에게는 붙지 않는다(예: `fire_enchanted` 는 `burning_dead` 에 안 붙음, E `phasing` 은 `phase_through` 를 가진 핀드에 안 붙음).
- **버프**: 부품은 버프 ID 와 시간만 넘긴다(`"buff": "command"`). 적마다 `BuffSlots`(인라인 6칸: 버프 정의 번호, 끝 틱, 값 배율). **같은 버프는 큰 값 하나만 남고**(기획 4.2), 다른 버프는 더한다.
  오라처럼 범위 안에 있는 동안만 붙는 효과는 매 틱 2틱짜리로 다시 붙이고, 시간제(함성 6s)는 그 시간으로 붙인다. 칸이 모자라면 가장 먼저 끝나는 것을 바꾼다.
  `debuff` 가 있는 버프는 정의 `immunities` 에 같은 이름이 있으면 붙지 않는다(`slow` 면역 = 보스·중간 보스). 버프 붙이기 도우미는 WP1 공통 코드라 WP3(장판 둔화)·WP4(오라·함성)가 같이 쓴다.
  → "새 오라를 가진 적" 이 정의(버프 1개 + 오라 부품 매개변수)만으로 늘어난다.
- 정예·우두머리 배율(HP ×3.5/×8, 피해 ×1.4/×1.6, 이동 ×1.1, 경험치 ×4/×10, 크기 ×1.15/×1.3 — 최종 크기 상한 `max_scale` 1.6(6.5), 넉백 저항, 발광, 드롭)과
  정예 무리 기본값(`pack_size: [3,5]`, `unique_chance: 0.3`, `banner: "정예 무리가 나타났다"`, `banner_time: 1.5`)은 `rules.ranks` 의 `elite`·`unique`·`pack` 에 둔다(`rules/enemies.json`, 6.10).
- `EnemyFactory.Create(def, rank, affixes, minutes, in SpawnContext)` 가 정의 + 등급 + 접두사를 합쳐 인스턴스를 만든다(데코레이터 방식: 정의는 그대로, 인스턴스가 합친 값을 가짐).
  접두사 부품은 정의 부품 뒤 슬롯에 붙는다. 분열체는 `rank` 를 물려받지 않는다(부품 매개변수 `inherit_rank: false` 기본). 생성 직후 `ISpawnHookPart` 들을 부른다.

### 6.8 무기 `weapons`, 진화 무기

```json
{
  "weapons": [
    { "id": "magic_missile", "name": "매직 미사일", "icon": { "sprite": "icon_magic_missile" }, "cast_clip": "cast_lightning",
      "pattern": "homing", "targeting": { "type": "nearest", "radius": 450 }, "projectile": "pj_magic_missile",
      "max_level": 8,
      "base": { "turn_rate": 360, "lifetime": 3.0, "burst_interval": 0.08, "knockback": 20 },
      "levels": {
        "damage":   [10, 10, 14, 14, 14, 18, 18, 22],
        "cooldown": [1.2, 1.2, 1.2, 1.0, 1.0, 1.0, 0.85, 0.85],
        "count":    [1, 2, 2, 2, 3, 3, 3, 4],
        "pierce":   [1, 1, 1, 1, 1, 2, 2, 2],
        "speed":    [300, 300, 300, 330, 330, 330, 360, 360] } },

    { "id": "arcane_orbit", "name": "비전 구체", "icon": { "sprite": "icon_arcane_orbit" }, "cast_clip": "cast_magic",
      "pattern": "orbit", "targeting": { "type": "none" }, "projectile": "pj_arcane_orb", "max_level": 8,
      "base": { "rotation": 180, "hit_interval": 0.5, "knockback": 30 },
      "levels": {
        "damage":       [8, 8, 11, 11, 14, 14, 17, 20],
        "count":        [2, 3, 3, 3, 4, 4, 5, 5],
        "orbit_radius": [70, 70, 80, 80, 90, 90, 100, 100],
        "duration":     [3.0, 3.0, 3.5, 4.0, 4.0, 5.0, 5.0, null],
        "cooldown":     [4.0, 4.0, 4.0, 3.5, 3.5, 3.0, 3.0, null] } },

    { "id": "inferno", "name": "연옥불", "base_weapon": "fireball", "icon": { "sprite": "icon_inferno", "frame_border": "gold" },
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
- 아이콘은 `{ sprite, frame?, dir?, tint?, scale?, layers?, frame_border? }`. 무기 아이콘은 가져오기 도구가 원작 마법 프레임으로 구운 48×48 한 장(`d1/icons/<무기>.png`, `source: "grid"`, 기획 5.9)이고,
  "화살 3장 겹침" 도 도구가 굽는다(1.4). `layers` 는 남겨 두되 MVP 에서 쓰지 않는다. 패시브 아이콘은 원작 exe PNG, 탐욕 아이콘(28px `GoldIcon` 을 48 칸에)은 `scale`.
- `cast_clip`(v0.3, 계약 변경): 서 있을 때 발동하면 재생할 주인공 시전 클립(`cast_fire`·`cast_lightning`·`cast_magic`, 기본 `cast_magic`, 기획 5.1·5.2 표의 시전 그림 열).
- 무기 발사 소리는 무기 정의에 두지 않는다 — 소리 연결 표 `weapons.<id>.fire`(6.15).

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
    "hero": { "sprites": "set_sorcerer", "radius": 14, "invuln_after_hit": 0.25,
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
               "pack":   { "pack_size": [3, 5], "unique_chance": 0.3, "banner": "정예 무리가 나타났다", "banner_time": 1.5 },
               "max_scale": 1.6 },
    "enemies": { "telegraph_required": { "min_damage": 10, "min_time": 0.4, "melee_reach_over_contact": 50,
                                         "heavy_melee_damage": 25, "heavy_melee_windup": 0.35 } },
    "caps": { "enemies": 300, "enemies_absolute": 360, "hero_projectiles": 400, "enemy_projectiles": 200, "gems": 300 },
    "spawn": { "band": [40, 200], "tries": 12, "edge_margin": 4, "group": [1, 3], "group_radius": 40, "accumulator_hold": 3 }
  }
}
```

- 카드 문구 `피해 14 → 18` 은 레벨 표 두 행을 비교해 `stat_labels` 로 만든다 → 새 무기도 문구가 저절로 생긴다.
  패시브 카드(`최대 체력 +20`, `피해 +10%`)도 같은 표에서 `StatKey` 라벨과 `format`(`abs` 기본 / `pct`)으로 만든다. 표에 없는 키를 쓰는 무기·패시브는 검증 경고.
- `level_up_push`: 선택 창이 닫힌 다음 스테이지 틱에 주인공 반경 150 안 적을 80px 밀어낸다(피해 0, 기획 12.6).
- `ranks.max_scale`: 생성 때 최종 크기(정의 크기 × 등급 배율) 상한(기획 8.1, 6.5). `enemies.telegraph_required`: 예고가 꼭 필요한 "큰 피해" 기준(기획 12.6, 판정은 6.5, 검사는 6.14 의 13.5-13).

### 6.11 타일셋 `tilesets`, 맵 `maps` (v0.3: 원작 타일 아레나, 기획 10.1)

v0.2 의 `maps`(배경 그림 `Dungeon1` + 손 좌표 충돌)와 `obstacle_kinds`·스테이지 `obstacles`(코드로 굽는 장애물)는 없앴다(기획 10.2 "MVP 에서 쓰지 않는다").
안쪽 장애물은 맵의 타일 배치에 들어간다. 코드 장애물이 다시 필요해지면 그때 종류를 되살린다(정의 틀을 남겨 두지 않는다 — 쓰지 않는 계약은 검사도 시험도 없이 낡는다).

```json
// Data/tilesets/l1.json (WP0 — 번호와 성질을 원본으로 확인하는 묶음)
{
  "tilesets": [
    { "id": "l1", "column_height": 160, "floor": [13, 162, 163],
      "borders": { "arch_hall": { "corner_n": 4, "edge_ne": 2, "corner_e": 23, "edge_se": 18,
                                  "corner_s": 20, "edge_sw": 19, "corner_w": 24, "edge_nw": 1 } },
      "gates":   { "arch_hall": { "nw": { "open": 13, "end_a": 0, "end_b": 0 } } },     // 문 틈·벽 끝 조각: WP0 이 견본 그림으로 골라 채운다
      "pieces":  { "pillar": 15 },
      "props": {                                     // 메가타일 → 하위 칸 4개 (위-왼, 위-오른, 아래-왼, 아래-오른). 출처: 원본 확인(probe tiles)
        "13": "....", "162": "....", "163": "....",
        "15": "X...",                                // 기둥: 막힌 칸은 위-왼 하나(TIL p24, SOL 13). 나머지 셋은 걸을 수 있는 칸의 위 조각
        "1": "X.X.", "2": "XX..",                    // 뒷벽: 칸의 반만 벽
        "18": "XXXX", "19": "XXXX", "20": "XXXX", "24": "XXXX"   // 앞둘레 턱: SOL 15 = 걷기·투사체 둘 다 막힘(4.2)
      },
      "sol_known_diffs": [                           // 출처: DevilutionX 참고(원본 SOL 오류 보정 칸, 부록 A.4). 번호와 이유만
        { "tile": 4, "sub": 1, "sol": ".", "use": "M", "why": "아치 조각 투사체 막힘 보정(원본 SOL 오류)" } ] }
  ]
}
```

(`props` 의 13·15·1·2·18·19·20·24 는 v0.3 리뷰가 diabdat 를 직접 읽어 확인한 값이다. 모서리(4·23 등)·문 조각의 성질과 `sol_known_diffs` 의 칸은 WP0a 가 `probe tiles` 로 확인해 적는다 —
위 `sol_known_diffs` 원소는 모양을 보이는 예다. DevilutionX 의 보정 목록은 하위 타일(조각) 번호라, 쓰는 메가타일의 (번호, 하위 칸)으로 바꿔 그 칸만 적는다.)

```json
// Data/maps/cathedral_arena.json (WP5)
{
  "maps": [
    { "id": "cathedral_arena", "tileset": "l1", "palette": "l1_1", "size": 20,
      "layout": {
        "border": "arch_hall",
        "gates": [ { "id": "gate_nw", "side": "nw" }, { "id": "gate_ne", "side": "ne" },
                   { "id": "gate_se", "side": "se" }, { "id": "gate_sw", "side": "sw" } ],
        "place": [ { "piece": "pillar", "at": [[7,7], [13,7], [7,13], [13,13], [10,4], [16,10], [10,16], [4,10]] } ] },
      "entries": [ { "id": "gate_nw", "outside": [-4, 19.5], "inside": [4, 19.5] },
                   { "id": "gate_ne", "outside": [19.5, -4], "inside": [19.5, 4] },
                   { "id": "gate_se", "outside": [43, 19.5], "inside": [34, 19.5] },
                   { "id": "gate_sw", "outside": [19.5, 43], "inside": [19.5, 34] } ],
      "hero_start": { "tile": [19.5, 19.5], "facing": "south" },
      "anchors": { "center": [19.5, 19.5] },
      "art": "d1/maps/cathedral_arena" }
  ]
}
```

- **타일셋**(`TilesetDef`, 계약 WP1b): `column_height` = 벽 기둥 높이(4.1 의 Hc), `floor` = 바닥 메가타일(섞어 깖), `borders`/`gates`/`pieces` = 이름 → 메가타일 번호.
  맵은 번호를 직접 적지 않고 이 이름을 쓴다 → 번호를 아는 묶음(WP0)과 배치를 맞추는 묶음(WP5)의 파일이 나뉜다.
  - 테두리 이름은 메가타일 격자 기준이다: `corner_n` = (0,0)(화면 맨 위), `edge_ne` = 윗줄(y = 0), `corner_e`, `edge_se` = 오른쪽 열, `corner_s`, `edge_sw` = 아랫줄, `corner_w`, `edge_nw` = 왼쪽 열.
    L1 번호는 조사 문서의 해골왕 방(`SklKng2.DUN`) 짜임이다.
  - **`props`**(성질 표): 쓰는 메가타일마다 하위 칸 4개(TIL 순서: 위-왼, 위-오른, 아래-왼, 아래-오른 = 하위 타일 (2mx,2my) (2mx+1,2my) (2mx,2my+1) (2mx+1,2my+1)).
    글자: `.` 바닥, `W` 걷기만 막힘, `M` 투사체만 막힘, `X` 둘 다(원본 SOL 의 bit0·bit2 와 대응, 1.4 성질 대조). 지금 MVP 맵에 `W` 칸은 없다 — 앞둘레 턱도 `X` 다(4.2).
    쓰는 메가타일인데 표에 없으면 로딩 오류. **표에 이름 붙이지 않은 메가타일이 있어도 오류**: `props` 의 키 = 그 타일셋이 이름 붙인 번호(`floor`·`borders`·`gates`·`pieces`)와 같은 집합
    (맵과 무관하게 타일셋만으로 판정, 번호 ≥ 0) → 성질 표가 SOL 전체를 옮긴 덤프로 커지지 않는다(R24). 이름만 붙이고 어느 맵도 안 쓰는 조각은 남을 수 있다.
  - `sol_known_diffs`: 원본 SOL 과 일부러 다르게 둔 칸 중 **원작 수정판이 고치는 SOL 오류**만(고치지 않으면 아치 밑으로 투사체가 지나감, 기획 10.1). 원소 = 메가타일 번호·하위 칸·원본 글자·쓰는 글자·이유.
    가져오기 도구가 대조할 때만 쓴다(1.4). 기획 이유로 원본과 다르게 둘 칸이 생기면 따로 `design_diffs`(이유 필수)에 적는다 — 두 목록을 섞지 않는다. 지금은 `design_diffs` 가 없다.
- **맵**(`MapDef`, 계약 변경 WP1b): `size` = 메가타일 수 M(정사각, 하위 타일 N = 2M), `tileset`, `palette`(층 팔레트 — 가져오기·그림만 씀, 기획 10.1 "맵마다 고정"), `art` = 가져오기 출력 폴더(7.6).
  v0.2 의 `image`·`minimap`·`size`(px)·`collision`·`texture_source` 는 없앴다. 월드 크기는 `IsoMath` 가 N 과 기둥 높이로 계산한다(4.1).
- **배치 풀기** `ArenaLayout.Expand(MapDef, TilesetDef) → ArenaGrid`(Core `Map/`, WP1b, 결정적):
  1. 모든 칸 = 바닥: `floor[StableHash(맵 ID, mx, my) % 개수]`(기획 10.1 "좌표 해시로 섞는다").
  2. 테두리: 네 모서리·네 변을 `borders[border]` 의 번호로.
  3. 문: `gates[i].side` 의 가운데(`at` 으로 변 위 메가타일 번호를 줄 수 있음) 메가타일 2개(= 하위 타일 4칸, 폭 약 140px)를 `open` 으로, 양옆 한 칸씩을 `end_a`/`end_b` 로.
  4. `place`: 조각 이름 → 번호를 `at` 목록의 [mx, my] 에, 또는 `line: [[mx0,my0],[mx1,my1]]`(가로·세로 줄 — 스테이지 2 의 짧은 벽).
  5. 결과: 메가타일 번호 M×M + 하위 타일 성질 N×N(`props` 로) + 하위 칸 **둘레 표시**(2·3단계 테두리·문 끝 조각에서 온 칸 — 4.2-4 `IsBoundary`)
     + `LayoutHash`(번호 격자·타일셋 ID·팔레트 + **타일셋 정의 전체**(`props`·`sol_known_diffs`·`design_diffs`·이름 → 번호)를 정해진 순서로 이은 `StableHash` — 그림이 낡았는지 보는 값, 1.4).
     성질 표나 보정 목록을 고치면 바닥/벽 나눔이 바뀔 수 있으므로 해시도 바뀐다.
- 위치(`entries` 의 `outside`/`inside`, `hero_start.tile`, `anchors`)는 **하위 타일 좌표(실수, 칸 중심 = 정수)**, 형은 `TileCoord`(4.1). 입구 ID 는 문 ID 와 같게 둔다(`entry:gate_ne`, 기획 10.1).
  기획 10.1 의 진입점 값은 첫 값이고 `--debug-nav entries,tiles` 로 맞춘다(기획 열린 질문 7). 위 예는 문 틈 중심(19.5)에 맞췄다 — 20 이면 문 틈 중심에서 0.5칸(약 14px) 비켜 행진이 한쪽 문설주로 몰린다.
  진입점은 기획 10.1 표 그대로 뒤 문 `4`, 앞 문 `N − 6`(N 40 → 34), 바깥 시작점 `−4`·`N + 3` 이다 — 뒤 벽은 한 줄, 앞 턱은 두 줄 두께라 둘 다 벽 안쪽 면에서 3.5칸(약 100px) 떨어진다.
  다른 묶음은 이 값을 직접 쓰지 않고 `World.Resolved`(월드 px, 4.1)를 읽는다.
- `wall_fade`(선택, 기본 `{ "mode": "light" }`): 반투명으로 그릴 벽(7.6). `light` = 기획 10.2(v0.5)의 두 규칙 — 주인공보다 앞이고 ① 주인공 그림 사각형과 겹치는 벽, 또는
  ② 주인공 빛 반경(`World.Lighting` 의 현재 `hero_light.radius`) 안의 벽. 모든 맵의 기본이고 MVP 두 맵은 적지 않는다. `hero` = ① 만(미리보기 비교용).
  `radius`(선택, px)를 적으면 ② 에 빛 반경 대신 그 값을 쓴다. v0.4 의 `radius` 모드("그 원 안 유닛을 가리는 벽만")는 기획이 유닛 조건 없이 정해 없앴다.
- 스테이지 2(`catacombs_arena`, L2, M = 22): L2 테두리·문·벽 조각 번호가 아직 없다(기획 열린 질문 15). WP0 이 단계 3 전에 `tilesets/l2.json` 을 채운다.
  앞쪽 둘레·협로 벽은 보기에 낮은 조각을 먼저 찾고(기획 10.4), L2 에 없으면 높은 벽을 쓰고 가림 규칙(7.6, `wall_fade` 기본 `light`)에 맡긴다. 협로 벽은 `place` 의 `line`. L2 조각의 성질도 원본 SOL 로 확인한다 —
  "낮은 턱이라 투사체 통과" 를 미리 가정하지 않는다(L1 앞둘레 턱은 `X` 였다).

### 6.12 스테이지 `stages`

```json
{
  "stages": [
    { "id": "stage_cathedral", "name": "대성당", "map": "cathedral_arena", "unlock": null,
      "lighting": { "ambient": [200, 200, 215], "darkness": 0.5,
                    "hero_light": { "color": [255, 240, 220], "radius": 420 },
                    "unit_min_light": 0.6,
                    "lights": [] },
      "victory": { "type": "boss" },
      "scaling": { "hp": [1.0, 0.08], "damage": [1.0, 0.03] },
      "elite_chance": { "every": 60, "from": "02:00", "chance": 0.25 },
      "spawn_intervals": [
        { "from": "00:00", "to": "01:00", "max_alive": 25, "per_second": 0.8, "weights": { "skeleton": 100 } },
        { "from": "01:00", "to": "02:00", "max_alive": 45, "per_second": 1.3, "weights": { "skeleton": 70, "zombie": 30 } },
        { "during": "boss", "max_alive": 120, "per_second": 3.0, "weights": { "skeleton": 60, "zombie": 40 } }
      ],
      "events": [
        { "at": "00:00", "type": "banner", "text": "대성당", "duration": 2 },
        { "at": "02:00", "type": "march", "enemy": "skeleton_archer", "count": 5, "at_entry": "gate_ne", "toward": "center", "straight": 15 },
        { "at": "03:00", "type": "ring", "enemy": "burning_dead", "count": 12, "radius": 400 },
        { "at": "04:30", "type": "elite_pack", "enemy": "skeleton", "count": 4, "affixes": 1, "unique_chance": 0.3 },
        { "at": "05:00", "type": "miniboss", "enemy": "rotfeast", "at_entry": "gate_sw" },
        { "at": "05:00", "type": "escort", "enemy": "skeleton_captain", "escorts": 8, "at_entry": "gate_nw" },
        { "at": "06:30", "type": "march", "enemy": "fallen", "count": 20, "at_entry": "gate_se", "toward_entry": "gate_nw", "straight": 15 },
        { "at": "10:00", "type": "boss", "enemy": "skeleton_king", "at_entry": "gate_ne" }
      ] }
  ]
}
```

- 배율 식 `[a, b]` = a + b × 분. 구간은 `from`/`to`, 보스 중 구간은 `during: "boss"`. 이벤트 종류 = 기획 13.4, 각 종류가 `PartKind.StageEvent` 부품(레지스트리 등록, WP4).
- `weights` 는 JSON 객체로 적지만 로딩 때 **적힌 순서의 배열**로 바뀐다(3.4). 감독은 종류를 먼저 뽑고, 그 종류의 `spawn_group` 이 있으면 무리 크기를 거기서 뽑는다(6.5).
- `victory`: `{ "type": "boss" }`(최종 보스 처치) 또는 `{ "type": "survive", "at": "10:00" }`(그 시각까지 생존). 처리는 3.5 끝.
- **단계 1 세로 조각**(기획 14.1·10.3): 그때는 폴른 원·썩은 시체·핀드·불타는 해골 궁수 정의가 아직 없어 전체 구간표가 "없는 적" 오류가 된다.
  WP4 는 단계 1 에 `stages/stage_cathedral.json` 을 **단계 1 판**(5종 가중치, `survive 10:00`, 보스·중간 보스 이벤트 없음)으로 쓰고, 단계 2 에서 전체 판으로 바꾼다. 두 판을 같이 두지 않는다.
- `march` 방향(v0.3, 등각 맵이라 방위보다 목표가 자연스럽다): `toward`(기준점 ID) / `toward_entry`(입구 ID — 그 진입점 쪽) / `heading`(방위, v0.2 그대로) 중 하나, `straight` = 직진 시간.
  `toward_entry` 행진은 그 진입점에 닿으면 직진 시간이 남아 있어도 보통 행동으로 바뀐다(기획 10.3 v0.5 — 빠른 핀드가 맞은편 벽까지 가서 멈춰 있지 않게, 6.6 `MoveOrder.Target`).
  매개변수 이름은 이벤트 부품을 만드는 WP4 가 정한다(11.3).
- **조명**(계약 변경 WP1b): v0.2 의 `kind_lights`(장애물 종류별 빛)는 장애물 종류와 함께 없앴다. 대신 `lights: [{ "id", "at": 기준점 ID 또는 [tx,ty], "color", "radius", "flicker", "pulse": { "period", "amount" } }]`
  (점 빛, MVP 두 스테이지는 비어 있음 — 기획 10.3·10.4 "화로·기둥 빛 없앰"). `[tx,ty]` 는 `TileCoord` 이고 `World.Lighting` 은 로딩 때 월드 px 로 바꾼 값을 가진다(4.1 `ResolvedMap`).
- `unit_min_light`(선택, 기본 0 = 끔): 주인공·적·시체·획득물이 받는 빛의 하한(0~1, 기획 10.5 의 "유닛 밝기 하한"). 바닥·벽은 원작처럼 어둡게 두고 이것들만 어둠에서 읽히게 한다(7.7).
  값은 기획 10.3~10.5(v0.5): 두 스테이지 모두 0.6, 어둠 스테이지 1 0.5·스테이지 2 0.6·스테이지 2 11:30 부터 0.7(상한 0.7), 주인공 빛 반경 420·380(380~480 안).
  `light_change` 의 경로에도 들어간다.
- `light_change` 이벤트는 바꿀 곳을 경로로 고른다: `{ "at": "11:30", "type": "light_change", "set": { "darkness": 0.7 } }`(스테이지 2). 받는 경로: `ambient`, `darkness`, `unit_min_light`, `hero_light.*`, `lights.<id>.*`.
  감독(WP4)이 `World.Lighting` 을 바꾸고 렌더(WP5)가 읽는다.
- 이벤트가 적 정의를 덮어쓰는 매개변수: `escort` 의 `escorts`(호위 수, 0 포함), `march` 의 방향·`straight`, `boss`/`miniboss` 의 `enrage`·`arrival`. 호위 수는 `SpawnContext.EscortCount` 로 넘기고(생성 훅이 읽음), 행진 지시·광폭화·등장 연출은 이벤트가 생성 직후 그 적에 직접 건다(6.6).
- 스테이지 음악은 스테이지 정의에 두지 않고 소리 연결 표의 `music.<스테이지 ID>`(6.15).

### 6.13 몬스터 늘리기 — 구조·시험·이번에 늘린 것 (사용자 요구 8)

새 그림을 그리지 않고 종류를 늘리는 길을 데이터 쪽에 네 겹으로 둔다(기획 8.1·19.5 "원작에 없는 것은 원작 재료로").

1. **원작 그림 묶음 + 원작 TRN 변종**: `sprites` + `variant`. 원작도 묶음 하나를 TRN 으로 네 종에 나눠 썼다(좀비 묶음 = 좀비·구울·썩은 시체·블랙 데스). 새 변종 = 가져오기 표 한 줄 + 그림 묶음 `variants` 한 칸.
2. **덧입힘 변형(`extends`)**: 색조·발광·알파·크기·수치·부품 일부. `behaviors_add`·`behaviors_patch` 로 부모 부품을 다시 적지 않는다. 몸 반경은 `base_radius × scale` 로 저절로 맞는다(6.5).
3. **부품 조합**: 일반 적 부품 13종 + 적 동작 7종(패턴·훅 안쪽)을 **어떤 적이든** 쓸 수 있다. 원거리 부품은 투사체 ID 를, 오라·함성은 버프 ID 를 받는다.
4. **등급 × 접두사**: 17종 × (일반/정예/우두머리) × 접두사 조합이 실행 중에 만들어진다. 접두사 제외 조건(6.7)이 어색한 조합을 막는다.

**MVP 17종(+분열체)과 보스 4** (기획 8.2·9.4·9.5) — 새 C# 은 두 곳뿐이다.

| 적 | 그림 묶음 · 변종 / 덧입힘 | 정의 | 새 코드 |
|---|---|---|---|
| `skeleton` | `set_skelaxe` · `white` | 틀(`chase_melee`) | — |
| `zombie` | `set_zombie` · 원색 | 틀 | — |
| `skeleton_archer` | `set_skelbow` · `white` | `kite` + `shoot_projectile(pj_enemy_arrow)` | — |
| `fallen` | `set_falspear` · `fallent` | `chase_melee` + `panic_on_ally_death`, `spawn_group [4,6]` | **`panic_on_ally_death` 부품**(WP4) |
| `burning_dead` | `set_skelaxe` · 원색 + 발광 | `extends: skeleton` + `self_destruct` | — |
| `ghoul` | `set_zombie` · `bluered` | `charge` | — |
| `skeleton_captain` | `set_skelsd` · `white` + 발광, ×1.1 | `command_aura` + `escort` | — |
| `bloated_corpse` | `set_zombie` · `grey`, ×1.35 | `split_on_death(corpse_spawn)` | — |
| `fiend` | `set_bat` · `red` | `phase_through(alpha_over 1.0)` + `chase_melee`, `spawn_group [3,5]` | (`alpha_over` 매개변수 하나) |
| `burning_archer` | `set_skelbow` · 원색 + 발광 | `extends: skeleton_archer`, `pj_fire_arrow` | — |
| `goatman` | `set_goatmace` · 원색 | `chase_melee`, `spawn_group [2,3]` | — |
| `goatman_archer` | `set_goatbow` · 원색 | `kite` + `shoot_projectile` | — |
| `acid_beast` | `set_acid` · 원색 | `kite` + `shoot_projectile(pj_acid)` | **투사체 `on_end`**(WP3, 6.4) |
| `black_death` | `set_zombie` · `yellow` | `chase_melee` + `ground_hazard(on_death)` | — |
| `overlord` | `set_fat` · 원색 | `chase_melee` + `heavy_strike` + `war_cry(special)` | — |
| `mud_man` | `set_fat` · `blue`, ×1.15 | 패턴 `melee_slam` + 예고(6.5) | — |
| `fire_clan` | `set_goatmace` · `red` + 발광 | `charge`, `spawn_group [5,7]` | — |
| `corpse_spawn` | `set_zombie` · `grey`, ×0.65 | `chase_melee`(분열로만) | — |
| `rotfeast` / `steelmace` | `set_zombie` · `eth` ×1.6 / `set_goatmace` · `bhsm` ×1.4 + 붉은 발광 | 중간 보스 패턴(기획 v0.3 의 썩은 거인·타락한 기사 대장 그대로) | — |
| `skeleton_king` / `butcher` | `set_sking` · `genrl` / `set_fatc` · `genrl` ×1.3 | 보스 패턴·페이즈(6.5) | — |

- 기획 v0.3(설계 v0.2)의 `skeleton_mage`·`wraith`·`goatman_shaman`·`plague_zombie`·`fallen_knight`·`bone_brute`·`hell_goat`·`rotting_hulk`·`blood_king` 과 그 투사체는 없앤다(기획 8.2 "v0.3 에서 바뀐 것").
- **확장 후보(E, 기획 8.4)의 비용**: 대부분 "가져오기 표 한 줄 + 정의 1개". 새 부품이 필요한 것만: `scavenger`(`eat_corpse`), `hidden`·`counselor`(`blink`, 자리는 `PlacementResolver`),
  `gargoyle`(`stone_form`), `blood_knight`(`lifesteal` — 접두사 `vampiric` 과 공유, `HitLanded` 사용).

**"새 적 = JSON 1개" 를 자동으로 확인하는 시험** (WP4, 10.1 `Enemies`)

- **적 정의 전수 연기 시험**(`Theory`, 실제 `Data/` 의 생성 가능한(`abstract` 아님) 적 전부 + `Fixtures/variants/`): 빈 맵에 무적 주인공과 150px 떨어뜨려 하나 생성 →
  10초(600틱) 동안 예외·NaN 없음 → 근접형은 주인공 쪽으로 움직였거나 피해 요청을 냈고, 원거리형은 거리 띠 안에서 투사체를 냈음 → HP 0 으로 만들면 죽음 훅이 돌고 저장소에서 빠지고 보석 1개.
  보스·중간 보스는 패턴 하나 이상이 실행됐는지를 본다. **새 적을 데이터에 넣으면 이 시험이 저절로 늘어난다.** 그림이 없어도 돈다(그림은 정의 값만 쓴다).
- **코드 없는 변형 증거**: `tests/…/Fixtures/variants/ash_skeleton.json`(`extends: skeleton` + 색조 + `behaviors_add: [ground_hazard on_death]` + `behaviors_patch`)을 위 시험에 넣는다.
  이 파일을 바꾸면서 C# 을 고쳐야 했다면 구조가 깨진 것이다.
- `--preview enemies` 는 배치를 고정한다(9절): 적 정의마다 한 행(ID·변종, 방향 0·고정 프레임으로 걷기/공격/특수/죽음 칸 + 8방향을 도는 칸) → 같은 시각 캡처가 늘 같은 그림이라 정의를 고친 전후를 나란히 비교할 수 있다.
- 검증 규칙이 "그 적이 쓰는 클립(`attack`·`special`)이 그림 묶음에 있는가", "`event_frame` ≤ 그 클립 프레임 수", "`variant` 가 그림 묶음 `variants` 안에 있는가" 를 본다.
- 데이터 경고 두 개(6.14)가 "늘렸는데 화면에서 안 보이는" 경우를 잡는다: 어느 스테이지에서도 나오지 않는 적, 같은 스테이지에서 구별되지 않는 두 적(같은 묶음 + 같은 변종 + 비슷한 덧입힘).

### 6.14 정의 간 참조 검증 목록

| 규칙 | 내용 | 위치 |
|---|---|---|
| 13.5-1 | 종류 안 ID 유일, 모든 `DefRef` 존재(그림·그림 묶음·이펙트·투사체·버프·타일셋·맵·입구 `entry:`·기준점·몬스터·드롭 표·진화 재료·보스), 맵의 테두리·문·조각 이름이 타일셋에 있음, 적 `variant` ∈ 그림 묶음 `variants` | Data (WP7) |
| 13.5-2 | 레벨 표 길이 = 최대 레벨, 키 = 패턴 `LevelKeys`, 단조성. `null` 은 `NullableKeys` 에서만 | **Data(발사 패턴 등록 여부 — `WeaponDef.Pattern` 의 `[PartType(FirePattern)]`) + WP3 검사기(레벨 키·단조성·null)** (WP1 코드) |
| 13.5-3 | 생성 구간이 0:00 부터 빈틈·겹침 없이, 가중치 합 > 0, `during: boss` 는 보스 있을 때만 | Data |
| 13.5-4 | 배치가 M×M 안, 쓰는 메가타일이 모두 성질 표에 있음, **성질 표의 키 = 타일셋이 이름 붙인 번호**(13.5-12), 문 칸이 비어 있음, 놓은 조각이 테두리와 겹치지 않음. 충돌 도형 묶음끼리·경계와 80px 이상 또는 붙음 | Map(WP1b 배치) + Nav/Validation (WP2) — **자원 없이 늘 돈다** |
| 13.5-5 | 길 검사: 반경 14(주인공 시작 → 모든 입구 진입점·도달 칸), `NavConst.FieldS`·`FieldM`(16·26)과 34(입구 진입점 → `center`), 주인공이 설 수 있는 모든 칸에서 `S` 필드 시작점 ≥ 1 | Nav/Validation (WP2) — 늘 돈다 |
| 13.5-6 | 입구 진입점·시작 위치의 clearance ≥ 38, 바깥 시작점은 맵 밖(바깥 마름모 밖) | Nav/Validation (WP2) — 늘 돈다 |
| 13.5-7 | **정의끼리**(늘): grid 시트 = 프레임 × 개수(+slack), atlas 는 방향·프레임 > 0·`origin` 이 칸 안, 클립 존재, `event_frame` ≤ 프레임 수. **실제 자원**(있을 때만): manifest 판, atlas 메타의 칸·방향·프레임 = 정의, 사각형이 페이지 안, PNG IHDR = 메타 페이지(변종마다), grid PNG = `sheet`, 맵 `arena.json` 의 `layout_hash` = 배치 | Data / Art (WP5) |
| 13.5-8 | 진화 재료 무기의 최대 레벨, 재료 패시브 존재 | Data |
| 13.5-9 | 패시브 키 ∈ `StatKey`, 접두사·버프 키 ∈ `EnemyStatKey` | Data |
| 13.5-10 | 정의가 가리키는 파일 이름(atlas 페이지 × 변종, 메타, grid PNG, 맵 `art` 폴더의 파일 — `arena.json`·`columns.*`·`minimap.png`·바닥 조각 이름 규칙(1.4)으로 계산한 `floor_*`, 소리·음악 파일, 소리 색인) ⊂ **가져오기 표가 만드는 이름 + `Table/exe.json`** — 자원 없이 돈다(기획 13.5-10). 두 쪽으로 나눈다: ① 표 → 이름 목록(`ImportTable`, 기본 분류) ② 정의 → 이름 추출과 대조(실제 `Data/` 를 WP7 로더로 읽으므로 `Integration`) | 시험 `tests/…/Assets/` (WP0 작성, ② 는 통합 때 기본으로) |
| 13.5-11 | 아래 "추가(경고)" 의 구별 경고(기획 13.5-11) | Data |
| 13.5-12 | 성질 표의 키 = 타일셋이 이름 붙인 번호(`floor`·`borders`·`gates`·`pieces`, 번호 ≥ 0 — `ArenaLayout.ValidateTileset`), `sol_known_diffs`·`design_diffs` 원소의 메가타일이 성질 표에 있음, `why` 가 비면 오류 | Map(WP1b `MapDataReader`) — 늘 돈다 |
| 13.5-13 | 큰 피해 패턴·부품의 예고(6.5 판정, `rules.enemies.telegraph_required`): 예고 필수인데 없거나 `min_time` 보다 짧으면 **오류**, 무거운 근접 단일 대상의 타격 프레임까지 시간이 `heavy_melee_windup` 미만이면 **오류** | Data + 부품(WP4 매개변수 검사) |
| 13.5-14 | 정의 `scale` 이 0.65~1.6 밖, 또는 정의 `scale` × 받을 수 있는 가장 큰 등급 배율 > `rules.ranks.max_scale` 이면 경고(넘는 쪽은 생성 때 잘림, 6.5) | Data |
| 소리 | 소리 연결 표의 대상(무기·투사체·이펙트·그림 묶음·적·획득물·스테이지)과 소리 ID 가 있음, 분류가 `rules.audio` 에 있음, 소리 파일 ≥ 1 | Audio (WP8) |
| 추가(오류) | `extends` 순환·없는 부모, `abstract` 정의를 생성에 씀, 부품 `(종류, type)` 미등록, 부품 매개변수 검사, `*_patch` 의 없는 id, 클립·이벤트 프레임 범위, 큰 피해 패턴의 예고 필수(13.5-13), 참조 쪽 덮어쓰기는 색조·크기만, 부품 슬롯 > 12, 여러 틱 동작이 `actions` 마지막이 아님, `rules` 하위 키 중복, atlas 그림에 `sheet`·`color_key`·`slack` 속성, 쓰는 클립(`special`·`cast_*`)이 그림 묶음에 없음, `tilesets`·`maps` 에 `extends`·`*_add`·`*_patch` | Data + 부품 |
| 추가(경고) | flow 유닛 반경 > 26, 명시한 `radius` 가 `round(base_radius × scale)` 와 2 넘게 다름, 크기 범위(13.5-14), 사거리가 몸 반경 합 + `melee_reach_over_contact` 를 넘는 단일 대상 근접(6.5), **생성 가능한 적인데 어느 스테이지에서도 나오지 않음**, **같은 스테이지에 나오는 두 적이 같은 그림 묶음 + 같은 변종이고 색조 거리(RGB 유클리드) < 60, 크기 차 < 0.15, 알파·발광도 같음**, 소리 연결이 없는 그림 묶음을 쓰는 생성 가능한 적(소리 없는 적, WP8) | Data |

- 구별 경고는 변종이 다르면 구별되는 것으로 본다(원작 TRN 은 색이 크게 다르다 — 기획 8.2). 기준값(60·0.15)은 설계 제안이고 최종 판정은 `--preview enemies` 캡처를 사람이 본다.
- v0.2 에서 "그림이 있을 때만" 이던 기하 검사(13.5-4~6)는 v0.3 에서 **늘 돈다**(충돌을 저장소 데이터로 만들므로, 4.2). 기획 13.5-4 의 "다각형이 없으면 건너뜀" 은 필요 없어졌다. `[해석]`

### 6.15 소리 데이터 `sounds`, `music`, `sound_bindings`, `rules.audio` (v0.3, WP8)

```json
// Data/audio/sounds.json
{
  "sounds": [
    { "id": "snd_cbolt",       "files": ["d1/sound/sfx/misc/cbolt.wav"], "category": "weapon_fire" },
    { "id": "snd_zombie_hit",  "files": ["d1/sound/monsters/zombie/zombieh1.wav", "d1/sound/monsters/zombie/zombieh2.wav"],
      "category": "monster_hit", "pitch_jitter": 0.05 },
    { "id": "snd_hero_hurt",   "files": ["d1/sound/sfx/sorceror/mage69.wav", "d1/sound/sfx/sorceror/mage69b.wav"],
      "category": "hero" },
    { "id": "snd_hero_death",  "files": ["d1/sound/sfx/sorceror/mage71.wav"], "category": "hero" },   // 기획 4.0: mage71 은 죽음, sfx/misc/dead.wav(전사)는 안 씀
    { "id": "snd_gem",         "files": ["d1/sound/sfx/items/invgrab.wav"], "category": "pickup", "volume": 0.4, "min_interval": 0.06 },
    { "id": "snd_wallloop",    "files": ["d1/sound/sfx/misc/wallloop.wav"], "category": "ambient", "loop": true }
  ],
  "music": [
    { "id": "mus_title",     "file": "d1/sound/music/dintro.wav" },
    { "id": "mus_cathedral", "file": "d1/sound/music/dlvla.wav" },
    { "id": "mus_catacombs", "file": "d1/sound/music/dlvlb.wav" }
  ]
}

// Data/audio/bindings.json — 무엇이 일어나면 어느 소리를 내나
{
  "sound_bindings": {
    "weapons":      { "magic_missile": { "fire": "snd_cbolt" }, "fireball": { "fire": "snd_fbolt" } },
    "projectiles":  { "pj_enemy_arrow": { "fire": "snd_bfire" }, "pj_acid": { "fire": "snd_acids" } },
    "effects":      { "fx_fire_explosion": "snd_firimp", "fx_bolt_hit": "snd_elecimp", "fx_acid_pool": "snd_puddle" },
    "hazard_loops": { "fx_hellfire_pool": "snd_wallloop" },
    "sprite_sets":  { "set_zombie": { "attack": "snd_zombie_attack", "hit": "snd_zombie_hit", "death": "snd_zombie_death", "special": "snd_zombie_special" } },
    "enemies":      { "skeleton_king": { "voice": "snd_sking_voice" }, "butcher": { "voice": "snd_butcher_voice" } },
    "pickups":      { "gold": { "collect": "snd_gold" }, "healing_potion": { "collect": "snd_invpot", "drop": "snd_flippot" } },
    "hero":         { "hurt": "snd_hero_hurt", "death": "snd_hero_death" },
    "ui":           { "menu_move": "snd_titlemov", "menu_select": "snd_titlslct", "level_up": "snd_magic", "chest_open": "snd_chest", "elite_pack": "snd_magic1" },
    "music":        { "title": "mus_title", "stage_cathedral": "mus_cathedral", "stage_catacombs": "mus_catacombs" }
  }
}

// Data/rules/audio.json — rules 하위 키 audio (WP8 파일, 6.1 규칙대로 다른 묶음과 겹치지 않음)
{
  "rules": { "audio": {
    "voices": 24, "same_sound": 3, "min_interval": 0.05, "monster_hit_interval": 0.08,
    "monster_death": { "voices": 4, "same_kind_interval": 0.1 },
    "offscreen_margin": 100, "pan": 0.5,
    "defaults": { "master": 0.8, "sfx": 0.8, "music": 0.6 },
    "music_fade": 0.5, "voice_duck": { "to": 0.5, "time": 2.0 }, "pause_music": 0.5,
    "categories": [
      { "id": "boss_voice",  "volume": 1.0, "offscreen": true },
      { "id": "hero",        "volume": 1.0, "offscreen": true },
      { "id": "ui",          "volume": 0.7, "offscreen": true },
      { "id": "boss",        "volume": 0.8, "offscreen": true },
      { "id": "weapon_fire", "volume": 0.6, "offscreen": true },
      { "id": "weapon_hit",  "volume": 0.5, "offscreen": false },
      { "id": "monster",     "volume": 0.5, "offscreen": false },
      { "id": "monster_hit", "volume": 0.5, "offscreen": false },
      { "id": "pickup",      "volume": 0.4, "offscreen": true },
      { "id": "ambient",     "volume": 0.6, "offscreen": true } ] } }
}
```

- **소리 ID 를 각 정의에 두지 않고 연결 표 한 곳에 둔다**(기획 12.7·13.1 은 "소리 ID 는 정의에 두고 가리킨다" 였다). `[해석]` 이유:
  ① 시뮬레이션(Core `Sim/`)이 소리를 전혀 모르게 된다 — 신호(`CueEvent`, 7.9)는 "무엇이 일어났나" 만 싣는다.
  ② 소리 데이터와 연결을 WP8 하나가 맡아 무기(WP3)·적(WP4)·이펙트(WP5) 데이터 파일을 고치지 않는다(파일 소유, 11.3). 소리 단계(기획 14.1 의 2단계)가 다른 묶음을 기다리지 않는다.
  ③ 새 적은 그림 묶음이 같으면 소리도 저절로 따라온다(연결 키 = 그림 묶음). 새 그림 묶음에 소리 연결이 없으면 경고(6.14)가 잡는다.
- `sounds[]`: `files`(여럿이면 무작위 — 게임 쪽 난수, 시뮬레이션 난수와 따로), `category`(`rules.audio.categories` 의 ID — 우선순위는 목록 순서, 앞이 높음 = 기획 12.7 순서),
  `volume`(분류 배율에 곱함), `max_instances`(기본 `same_sound`), `min_interval`(기본 `rules.audio.min_interval`), `pitch_jitter`(± 비율), `offscreen`(분류 값 덮어쓰기), `loop`.
- `music[]`: `file`, `loop`(기본 true). 기획 12.7 의 곡 목록.
- 연결 키의 대상은 그 종류의 정의 ID 다. `sprite_sets` 의 소리를 적 정의가 덮어쓰려면 `enemies.<id>.{attack,hit,death,special}`. 보스·중간 보스(정의 `rank`)가 낸 몬스터 소리는 분류를 `boss` 로 올린다(기획 12.7 "보스·중간 보스 소리").
- 파일 이름은 가져오기 출력(1.4: 원작 경로를 소문자로 `d1/sound/` 아래에 그대로)이다. 13.5-10 시험이 표와 맞춘다.
- 소리 길이는 정의에 적지 않고 가져오기 출력 `d1/sound/index.json`(Core `Assets/SoundIndex`: 파일마다 표본 수·표본율·채널)에서 읽는다. 무음에서도 읽는다(7.9 고르기).
- 정의 형(`SoundDef`, `MusicDef`, `SoundBindingsDef`, `AudioRulesDef`)은 Core `Defs/`(데이터 모양, 계약 WP1b)이고, 검사기·고르기 규칙은 Core `Audio/`(WP8), 재생은 게임 `Audio/`(WP8). `Sim/` 은 어느 것도 쓰지 않는다.

---

## 7. 렌더·소리

### 7.1 가상 해상도와 창 (기획 열린 질문 1: 960×540 으로 간다)

- 월드는 **960×540 렌더 타깃**(`WorldRT`)에 1:1 월드 픽셀로 그린다. 창(또는 전체 화면)에 맞출 때:
  1. 정수 배율 k = ceil(min(창W/960, 창H/540)) 로 `WorldRT` 를 점 샘플링(PointClamp)해 960k×540k 중간 타깃에 키운다.
  2. 그것을 선형 샘플링으로 창 안 레터박스 사각형(비율 16:9 유지, 남는 곳 검정)에 줄여 그린다.
  → 정수 배율에서는 그대로 날카롭고, 비정수 배율에서도 번짐이 1px 테두리 정도로 줄어든다(sharp bilinear).
- 최종 결과는 `PresentRT` 에 모은다(월드 + UI). 백버퍼에는 그것을 한 번 늘여 복사한다. 캡처는 `PresentRT` 를 저장한다.
  `PresentRT` 크기는 백버퍼와 따로 정한다: `--window` 값이 있으면 그 크기로 고정(창이 화면 배율·작은 모니터 때문에 줄어도 캡처 크기·배율 k 가 바뀌지 않음), 없으면 창 크기.
- 전체 화면은 테두리 없는 창(`HardwareModeSwitch = false`). F11·Alt+Enter. 개발 옵션 실행에서는 F11 을 받지 않는다(실제 입력을 읽지 않음, 3.2).
- `GraphicsDeviceManager.GraphicsProfile = GraphicsProfile.HiDef` 를 명시한다. `LOGO`(550×3240), 원작 시트 아틀라스(가로 ≤ 4096, 1.4)가 Reach 한도 2048 을 넘는다.
  바닥은 512 조각이라 한도와 상관없다.

### 7.2 카메라

- 카메라 중심 = 보간된 주인공 위치, **외곽 상자**(4.1, 스테이지 1 2560×1408)에 맞춰 자름. **따라가기 지연 없음**: 시뮬레이션의 생성 띠(카메라 시야 바깥 40~200px)와
  보이는 화면이 정확히 같아야 하고, 지연이 있으면 화면 안에서 적이 생기는 일이 생긴다. 시뮬레이션(`World.ViewRect`)과 렌더 카메라가 **같은 함수** Core `CameraMath.ViewRect(heroPos, mapSize, viewSize)` 를 쓴다.
  맵이 마름모라 화면 모서리 쪽은 맵 밖(어둠)이 보인다 — 원작과 같다(기획 1절).
- 그릴 때 카메라 위치를 정수 픽셀로 내린다(픽셀 그림 떨림 방지). 개체 위치도 그리기 직전에 반올림.
- **흔들림**: 표시 이벤트 `ShakeEvent(Tick, Px, Duration)` 중 지금 남은 것 가운데 가장 센 것(기획 12.6) → ±px 정수 오프셋 = `StableHash(seed, stageTick)`(3.4, 프레임마다 뽑는 난수 아님). 설정에서 끄면 0.
- 화면 → 월드 변환(창 좌표 → 레터박스 → 가상 960×540 → 월드)은 `RenderContext.ScreenToWorld(Point)` 하나다(WP5 구현). 마우스 이동(WP6)이 `AppServices.Render` 로 이것을 쓴다.

### 7.3 그리기 층

| # | 층 | 내용 | 정렬·혼합 |
|---|---|---|---|
| 0 | 바닥 | 아레나 바닥 조각(7.6). 맵 밖은 비운다(검정) | 불투명 |
| 0b | 뒷둘레 벽 | **뒤에 걸을 수 있는 칸이 하나도 없는 막힌 칸**(뒷둘레 벽 줄, 7.4)의 기둥과 그 칸들의 덧그림. 그보다 먼저, 맵 밖 뒤쪽(4.1 의 nw·ne 변 바깥)에 있는 입장 중 유닛 | 칸 줄(tx+ty) 순, 같은 줄은 tx 순 |
| 1 | 바닥 장식 | 장판(조각 흩뿌리기 포함), 예고(원·선), 발밑 원(오라·정예·아군), 등장 연출 어두운 원 | 생성 순, 알파/가산 |
| 2 | 시체 | `CorpseLayer` | 오래된 순 |
| 3 | 획득물 | 보석·금화·물약·상자(빛 기둥은 7층) | 알파 |
| 4 | 유닛·벽 | 주인공·적·**안쪽 벽 기둥·바닥 위 조각·아치 덧그림**(7.6) | **y 정렬**(7.4), 주인공 앞 벽 반투명, 알파(+발광은 7층에서 가산 두 번째 그리기) |
| 5 | 앞둘레 턱 | **앞에 걸을 수 있는 칸이 하나도 없는 막힌 칸**(앞둘레 턱 줄)의 기둥·덧그림. 그다음 맵 밖 앞쪽(se·sw 변 바깥)의 입장 중 유닛 | 칸 줄 순. 주인공 가림 반투명 판정 대상(7.6) |
| 6 | 빛 | 빛 지도 곱하기(7.7) | 곱하기 |
| 7 | 발광 | 투사체, 폭발·맞음 이펙트, 장판 이펙트의 빛 부분, 보석 빛, 정예 발광·번쩍임 덧그림, **예고(원·선)의 테두리를 한 번 더** | 이펙트 정의의 `blend`(기본 보통), 주인공 투사체는 가산 한 겹 더, 적 투사체는 붉은 테(7.5), 예고 테두리는 알파 0.6 의 2px 선 |
| 8 | 화면 효과 | 낮은 체력 붉은 비네트, 광폭화 맥동 | 알파 |
| 9 | 월드 UI | 이름(정예·우두머리), 주인공 발밑 체력 막대, 보스 아래 표시, 피해 숫자 | `PresentRT` 해상도로(글자 선명) |
| 10 | HUD·오버레이 | 8절 | `PresentRT` 해상도 |

- 투사체·폭발을 빛 **뒤에** 그리는 이유: 어둠에 묻히지 않게(빛을 내는 것은 빛 영향을 받지 않음). 원작 마법 그림은 보통 혼합으로도 빛 뒤라 밝게 보인다.
- 9~10층은 가상 좌표(960×540)로 배치하고 `UiCanvas` 가 창 배율로 바꿔 그린다. 글꼴은 크기 × 배율로 요청해 늘 선명하다(FontStashSharp 는 크기별로 그림을 굽는다).
- v0.2 의 "장애물 그림자"(1층)와 "가려진 주인공 실루엣"(7층)은 없앴다 — 원작 타일 그림에 그림자가 있고, 가리는 벽은 반투명으로 그린다(7.6).
- **예고를 두 번 그리는 이유**(기획 12.6 "예고 없이 큰 피해 없음"): 1층 예고(채움)는 4층의 160px 벽 기둥 아래에 깔려, 벽 뒤(화면 위쪽)에 생긴 피의 땅·폭발·강타 예고의 바깥 부분이 가려진다.
  가림 반투명은 주인공보다 앞(화면 아래)의 벽에만 걸리므로(7.6) 주인공 뒤쪽 벽 너머의 예고는 못 살린다. 7층에 테두리만 다시 그려 벽 위에서도 범위가 보이게 한다(빛 뒤라 어둠에도 묻히지 않는다).
  뒷둘레 벽은 0b 층이라 1층 예고가 그 밑동 위에 그려진다.

### 7.4 y 정렬과 그리기 호출

- **벽을 세 무리로 나눈다**(v0.4). y 하나로 모든 벽을 정렬하면 뒤쪽 둘레 벽(ty = 0·tx = 0 줄)에 붙어 선 유닛이 다음 벽 칸에 가려진다
  (반경 14 유닛이 ty ≈ 0.99, tx = k 에 서면 발 y 가 벽 칸 (k+1, 0) 의 중심 y 보다 작아, 그 벽 기둥이 유닛 오른쪽 32px 바깥의 무기·팔을 덮는다).
  - **뒷둘레 벽**: 막힌 칸 중 뒤쪽 이웃((tx−1, ty)·(tx, ty−1)·(tx−1, ty−1))에 걸을 수 있는 칸이 하나도 없는 칸(뒷벽 줄과 그 모서리). 어떤 유닛보다도 뒤라 **0b 층**(바닥 바로 뒤)에 그린다.
  - **앞둘레 턱**: 막힌 칸 중 앞쪽 이웃((tx+1, ty)·(tx, ty+1)·(tx+1, ty+1))에 걸을 수 있는 칸이 하나도 없는 칸. 모든 유닛보다 앞이라 **5층**(4층 뒤)에 그린다. 주인공 가림 판정 대상이다.
  - **안쪽 기둥·벽**(그 밖의 막힌 칸, 문 끝 조각 포함): 4층에서 유닛과 y 정렬.
  - 무리는 `ArenaGrid` 에서 한 번 계산한다(칸마다 바이트 하나, `ArenaRenderer` 가 맵을 열 때). 맵 밖 입장 중 유닛은 뒤쪽 바깥이면 0b 층(뒷벽보다 먼저), 앞쪽 바깥이면 5층(앞 턱보다 뒤)에 그린다(7.3).
- 4층 항목을 `DrawItem { float SortY; int Tiebreak; Kind; Index }` 배열에 모아 `Array.Sort`(키 = SortY, 같으면 Tiebreak) — 같은 y 에서 깜빡임 없음.
  - 유닛 SortY = 발 y. 단 **덧그림이 있는 칸 안에 발이 있는 유닛**은 `min(발 y, 그 칸 중심 y − 0.5)`(아래 덧그림). Tiebreak = 개체 ID.
  - 안쪽 벽 기둥(7.6) SortY = **그 칸 바닥 마름모 중심 y**. 걸을 수 있는 칸의 바닥 위 조각(덤불·아치 윗부분) = 위 꼭짓점 y. Tiebreak = 칸 번호.
  - 걸을 수 있는 칸의 **아치 덧그림**(L1S) SortY = 그 칸 중심 y. 원작은 칸 줄 순서로 그려 "그 칸 안 유닛 → 덧그림 → 앞 줄 유닛" 이다.
    y 하나로 두면 앞 이웃 칸 위쪽 절반에 선 유닛(발 y 가 C.y ~ C.y+16)과 그 칸 아래쪽 절반에 선 유닛(같은 y 범위)을 가를 수 없다 — 그래서 그 칸 안 유닛만 키를 C.y − 0.5 로 당겨
    덧그림이 덮게 하고, 앞 칸 유닛(발 y ≥ C.y)은 덧그림 뒤에 그린다. 칸 안 여부는 발 위치의 칸 번호(4.1 역변환)와 칸별 "덧그림 있음" 바이트로 바로 찾는다(할당 0).
  - 막힌 칸을 중심 y 로 두는 이유: 유닛은 막힌 칸 안에 설 수 없으므로 그 칸 둘레에 선다. 뒤쪽 두 이웃 칸(왼쪽 위·오른쪽 위)과 맞닿은 변 위의 점은 y ≤ 중심, 앞쪽 두 이웃(왼쪽 아래·오른쪽 아래)은 y ≥ 중심이라
    "앞 칸 유닛은 벽 앞, 뒤 칸 유닛은 벽 뒤" 가 칸 경계에서도 맞는다. 기획 10.2 의 "아래 꼭짓점 y" 로 두면 오른쪽 아래 이웃 칸에 붙어 선 유닛이 벽 뒤로 들어가 보인다. `[해석]`
    이 규칙은 **한 칸짜리 안쪽 기둥**에서 맞다. 여러 칸으로 길게 이어진 벽(스테이지 2 협로 `line`)은 한쪽 축으로만 앞인 유닛이 다음 칸에 가릴 수 있다 — 그 벽은 칸마다 나눠 정렬하고, 캡처(10.2)에서 보이면 협로 벽도 줄 단위 무리로 뺀다(R20).
  - 원작은 칸 단위로 그려 "벽 뒤 물체가 벽을 뚫고 보이는" 땜질이 필요했다(조사 문서). 화면 위치로 정렬하면 그 문제가 생기지 않는다.
- SpriteBatch `Deferred` 로 정렬된 순서대로 낸다. 텍스처가 바뀔 때마다 그리기 호출이 끊긴다. 그래서 그림 페이지를 **(그림 묶음 × 변종) 하나에 모든 동작**으로 묶는다(1.4·6.3) —
  같은 종·같은 변종이면 걷기·공격·죽기 중인 개체가 섞여도 한 텍스처라, 전환은 "종·변종이 바뀔 때" 와 "벽 기둥(맵마다 아틀라스 한 장)과 유닛이 번갈아 나올 때" 로 준다.
  그래도 y 로 섞인 400 개체 + 화면 안 벽 기둥 수십 개는 수백 번 호출일 수 있다. 10.3 으로 재고(기준 ≤ 600, 4.9 표), **단계 1 완료 조건**에 넣는다(WP5). 넘치면 R5 대응.
- 정예 발광·번쩍임처럼 가산이 필요한 그리기는 같은 항목 안에서 혼합 상태를 바꾸지 않도록, **4층을 끝낸 뒤 7층에서 같은 정렬 순서로 가산만 다시 그린다**(혼합 상태 전환 2번).
  두 번째 그리기는 발광·번쩍임이 켜진 항목만 모은 짧은 목록으로 한다(모든 유닛을 다시 돌지 않는다).
  가산 덧그림은 어둠의 영향을 받지 않는데, 발광은 원래 빛나는 것이라 맞다.
  대가: 벽 뒤에 가려진 유닛의 발광·번쩍임이 벽 위로 비친다. 작고 짧아 허용한다(눈에 띄면 앞에 불투명 벽 기둥이 겹친 유닛만 발광을 건너뛴다).

### 7.5 투명·그림자·변종·색조·번쩍임

- **투명**: atlas(원작 디아블로 1) 시트는 PNG 알파를 그대로 쓴다(가져오기 때 RLE 투명 → 알파 0). grid(원작 exe) 시트는 v0.2 그대로 색 키(`color_key` 와 같은 픽셀 → (0,0,0,0)).
  둘 다 `Texture2D.FromStream(device, stream, colorProcessor)`(3.8.6, 확인함)의 색 처리 콜백에서 **premultiplied 로 바꾼다**(rgb × a) — 그림자가 알파 0.5 라 이제 곱해야 맞다.
  이 호출은 해독과 GPU 올리기를 같이 하므로 **주 스레드에서 차례로** 부른다(작업 스레드에서 부르면 DesktopGL 이 GL 호출을 주 스레드로 넘겨 기다려 교착될 수 있다).
  시작이 느리다고 재 보면 그때 StbImageSharp(FontStashSharp 가 끌어오는 2.30.16 — 쓰려면 csproj 에 명시 참조)로 작업 스레드에서 해독 → 주 스레드에서 `new Texture2D` + `SetData` 로 바꾼다(R7).
- **그림자**: 원작 그림자(색인 0, 불투명 검정)는 가져올 때 검정 × `shadow_alpha`(0.5 첫 값, 기획 8.1·열린 질문 11)로 바뀐다. 값을 바꾸려면 표를 고치고 다시 가져온다(캐시가 그 작업만 다시 만든다).
  그림자는 rgb 가 0 이라 색조·발광·가산 번쩍임이 더해도 변하지 않고, 알파 배율만 같이 걸린다(기획 8.1 "버그 아님").
- **변종(TRN)**: 가져올 때 구운 **파일**이다(`zombie@bluered.png` — 묶음 한 페이지의 변종). 실행 중에 색을 바꾸지 않는다. 이유: 커스텀 셰이더를 쓰지 않으므로(2.2) 실행 중 TRN 은 CPU 로 텍스처를 한 벌 더 만드는 것과 같아
  그림 메모리 이득이 없고, 색인 PNG 해독기·팔레트 데이터를 게임에 따로 둬야 한다. 디스크만 변종 수만큼 늘어난다.
- **그림 메모리**: 칸 그대로의 시트면 변종 하나(좀비 묶음 서기·걷기·공격·죽기)가 25MB 안팎, 스테이지 1 의 변종 12개 + 보스면 300MB 를 넘는다. 프레임을 잘라 묶으면 약 1/3 로 본다(추정, WP0 가 잰다 —
  좀비 묶음 한 페이지가 2048² 안이면 16MB).
  게다가 `TextureStore` 는 **스테이지 시작 때 그 스테이지에서 나올 수 있는 것만** 읽는다: 나오는 적(구간 가중치·이벤트·소환·분열·호위 — 6.14 "안 나오는 적" 과 같은 계산)의 (그림 묶음 × 변종),
  주인공, 모든 무기·이펙트·투사체·획득물·아이콘, 그 맵의 그림. 스테이지를 바꾸면 쓰지 않는 것은 내린다. 로그 `ASSETS stage=… textures=… vram_mb=…`(R7).
- **색조**: SpriteBatch 색 = 색조 × 알파(premultiplied 이므로 `new Color(r*a, g*a, b*a, a)`). 곱하기라 어둡게만 된다(기획 8.1 그대로).
- **발광**: 같은 프레임을 `BlendState.Additive` 로 발광 색 × 알파로 한 번 더(7층). 발광 색·알파·맥동·번쩍임·발밑 원·알파 배율은 모두 `Enemy.Visual`(6.6)에서 읽는다 — 렌더는 부품 종류를 모른다.
- **적 투사체**: 투사체 진영이 `Enemy` 면 몸체 위에 붉은 테 빛(가산, (255,60,40) 약하게)을 더해 주인공 투사체(가산 한 겹 더)와 구별한다(기획 5.1 — 원작 화살을 양쪽이 같이 써서 더 중요해짐).
- **맞을 때 번쩍임**: 0.08s 동안 같은 프레임을 가산으로 흰색 × 0.8 한 번 더(기획이 허용한 "밝아짐"). 흰 실루엣 텍스처를 따로 만들지 않는다 — 그림 메모리가 두 배가 된다.
- **알파**(`phase_through` 장애물 위 `alpha_over`, 등장 연출, 시체 사라짐): 색 전체에 곱한다.

### 7.6 원작 타일 아레나 그리기 (v0.3, v0.2 "장애물 굽기" 대체 — 기획 10.2)

v0.2 의 `ObstacleBaker`(맵 질감으로 장애물 몸체·옆면·그림자를 CPU 로 굽기)는 쓰지 않는다. 바닥·벽·기둥은 가져오기 도구가 원작 타일로 구운 그림이고,
게임은 그것을 원작 순서대로 늘어놓기만 한다. `Core/Art/ObstacleBaker`·`PixelBuffer` 는 만들지 않는다(WP5 범위에서 뺌).

**가져오기 출력**(`Assets/<맵 art>/`, 1.4)

```json
// arena.json — Core Assets/ArenaArt record(계약 WP1b). 도구(WP0)가 쓰고 렌더(WP5)·시작 검사(WP7)·13.5-10 이 읽는다
{ "format": 1, "map": "cathedral_arena", "layout_hash": "…", "box": [2560, 1408],
  "floor": [ { "file": "floor_0_0.png", "pos": [0, 0] }, { "file": "floor_1_0.png", "pos": [512, 0] } ],
  "columns": "columns",                      // columns.png + columns.json(SpriteMeta 모양, 동작 하나·방향 1, 프레임 = 기둥 그림 수)
  "cells": [ [tx, ty, kind, frame], … ] }    // kind: 0 막힌 칸의 벽, 1 걸을 수 있는 칸의 바닥 위 조각, 2 아치 덧그림. frame 은 1부터(6.1)
```

- `floor` 의 파일 이름·`pos` 는 1.4 의 **바닥 조각 이름 규칙**(`ArenaArt.FloorChunks`) 그대로다 — `pos = (512cx, 512cy)` 월드 px. 시작 검사와 13.5-10 이 자원 없이 같은 목록을 계산한다.
  `cells` 는 (ty, tx, kind) 순 정렬. 같은 칸에 kind 1 과 2 가 같이 있을 수 있다. 칸의 무리(뒷둘레·앞둘레·안쪽, 7.4)는 파일에 적지 않고 게임이 `ArenaGrid` 로 계산한다.
- **바닥(0층)**: 바닥 칸(성질 `.`)의 바닥 삼각형 두 조각을 모두 구운 512×512 조각들. 화면에 걸친 조각만 그린다. 한 장으로 구우면 텍스처 한도를 넘는다(기획 10.2).
  원작 "바닥 먼저 전부, 그다음 벽" 순서가 이것으로 지켜진다(한 번에 그리면 앞 칸 바닥이 뒤 칸 벽 밑동을 덮는다).
- **벽 기둥(0b·4·5층)**: 하위 타일마다 64×160 기둥 그림 한 장(같은 그림은 아틀라스에서 한 프레임). 시제품 기준 마름모는 `C.y − 15 .. C.y + 15` 의 31줄이고(맨 위 1줄 빔, 부록 A.3),
  기둥 그림의 마지막 픽셀 줄은 `C.y + 15` 에 온다. 식으로: **기둥 그림 왼쪽 위 = `(C.x − 32, C.y + 16 − Hc)`**(Hc = 160 → 그림 줄 0..159 가 `C.y − 144 .. C.y + 15`).
  바닥 조각도 같은 월드 px 로 굽는다(조각 안 칸의 마름모 줄 = `C.y − 15 .. C.y + 15`). 두 쪽이 같은 `IsoMath.CellCenter` 를 쓰므로 1px 틈·겹침이 생기지 않는다(도구 시험이 맞닿은 줄을 본다, 10.1).
  정렬 키는 7.4. 흙 칸처럼 바닥 단이 비고 위 단만 있는 칸은 그림이 32px 떠 보이지만 그대로 둔다(충돌은 칸 자리, 기획 10.1).
- **가림(반투명)**: 4층·5층을 그릴 때 벽 항목(아치 덧그림 포함)이 주인공보다 앞(4층은 SortY > 주인공 발 y, 5층 앞둘레는 늘 앞)이고 아래 둘 중 하나면 **알파 0.5** 로 그린다(기획 10.2 v0.5).
  ① 그 그림 사각형이 주인공 그림 사각형과 겹친다. ② 그 칸 마름모 중심이 주인공 발에서 빛 반경(`World.Lighting` 의 현재 `hero_light.radius`, 맵 `wall_fade.radius` 가 있으면 그 값) 안이다
  — 협로 벽·앞쪽 높은 벽 너머로 다가오는 무리가 통째로 가려지지 않게. 맵 `wall_fade.mode = "hero"` 면 ① 만(비교용, 6.11). 주인공보다 뒤의 벽이 그 뒤 적을 가리는 것은 그대로다.
  원작의 구역 표(TransList)는 옮기지 않는다. 판정은 4·5층 벽 항목을 모을 때 칸마다 한 번(주인공 발과의 y·거리 비교, 할당 0). 0b 층 뒷둘레 벽은 늘 주인공 뒤라 판정하지 않는다.
- **빛**: 바닥·벽도 유닛과 같이 빛 지도(7.7)가 곱해진다. 원작의 거리별 어두움(16단계 빛 표)은 쓰지 않는다(기획 10.2).
- **맵 밖**: 마름모 바깥은 그리지 않는다(검정). 빛 지도의 어둠이 원작처럼 바깥을 덮는다.
- **미니맵**: 가져오기가 구운 `minimap.png`(외곽 상자 1/16 — 스테이지 1 은 160×88, 바닥 밝게·막힌 칸 어둡게)를 HUD 가 그린다(기획 12.1).
- **낡은 그림**: 그릴 때 `arena.json` 의 `layout_hash` 를 Core 가 같은 배치로 계산한 값과 비교한다. 다르면 시작 검사(1.4 끝)가 이미 막았어야 하므로, 여기서 다르면 예외(버그).
- **`--preview tiles`**: 맵마다 외곽 상자를 줄여 그린 화면 + 하위 칸 성질 겹침(`W`·`M`·`X` 색) + 입구·기준점·충돌 도형. 가져온 그림과 충돌(성질 표)이 맞는지 사람이 본다.
- **하지 않는 것(MVP, 기획 10.2)**: 맵 장식 물체(`objects\`), 문 열림·닫힘, 색 순환(L3·L4), 원작 자동 지도, 원작 빛 표.

### 7.7 조명

- `LightRT`(480×270, 월드의 절반 해상도 — 부드러운 빛, 채움 비용 ¼)를 `주변광 × (1 − 어둠)` 으로 지운다.
- 빛마다 미리 만든 방사형 그라데이션 텍스처(코드로 생성, 256px)를 색 × 세기로 **가산**해 그린다(주인공·점 빛·폭발·지옥불). 깜빡임·맥동은 세기에.
  깜빡임 = `StableHash(seed, lightId, stageTick)`, 맥동 = 스테이지 틱의 사인(3.4). 현재 조명 값(주변광·어둠·점 빛)은 `World.Lighting` 에서 읽는다(`light_change` 가 바꿈).
- `WorldRT` 위에 `LightRT` 를 **곱하기 혼합**(`ColorSourceBlend = DestinationColor, ColorDestinationBlend = Zero`)으로 늘려 그린다.
  → 주변광 곱하기와 어둠 뚫기가 한 번에 된다(기획 10.5 의 두 단계와 같은 결과, 단 빛이 겹쳐도 1 을 넘지 않음). 마름모 바깥도 같이 어두워진다.
- 빛 상한 64: 주인공·점 빛을 먼저, 남는 자리를 짧은 빛(폭발) 중 주인공에게 가까운 순으로.
- **유닛 최소 밝기**(`lighting.unit_min_light` > 0, 6.12): 빛을 다 그린 `LightRT` 에, 화면 안 주인공·적·시체(2층)·획득물(3층)(기획 10.5 "유닛 밝기 하한" 의 대상)의 현재 프레임을 절반 크기로 `(m, m, m)` 색, **최댓값 혼합**
  (`BlendState { ColorBlendFunction = BlendFunction.Max, ColorSourceBlend = One, ColorDestinationBlend = One }`, 알파 0 픽셀은 그리지 않게 알파 기준으로 자름)으로 한 번 더 그린다.
  → 곱하기 뒤 유닛 몸통은 밝기 ≥ m, 바닥·벽은 원작처럼 어둡다(기획 10.5 의 판독성). 적 투사체·발광은 이미 7층(빛 뒤)이라 영향 없다.
  시체(최대 150)·획득물(보석 최대 300)도 같은 목록에 넣는다 — 절반 해상도에 작은 그림이라 채움 비용이 작고, 같은 텍스처끼리 모으면 호출도 몇 번이다.
  대가: 유닛 앞에 안쪽 벽 기둥·앞둘레 턱이 겹치면 그 벽 위에 유닛 모양으로 밝은 자리가 생긴다 — 가림 판정과 같은 사각형 검사로 **앞 벽이 겹친 유닛은 건너뛴다**(7.4 발광과 같은 처리).
  `LightRT` 가 절반 해상도라 테두리 1px 정도가 바닥에 번진다(부드러운 테로 허용). 값이 0 이면 이 단계 전체를 건너뛴다.

### 7.8 손맛

| 요소 | 구현 |
|---|---|
| 피해 숫자 | 게임 쪽 풀 120개. 시뮬레이션의 `DamageNumber(Tick, Target, Pos, Value, Crit, ToHero)` 표시 이벤트를 받아 같은 대상 0.1s 안이면 합침. 0.6s 동안 24px 위로 + 알파 감소. 9층. 설정에서 끄면 그리지 않음 |
| 번쩍임·발광 | 7.5 |
| 넉백·히트 스톱·슬로모션 | 시뮬레이션(4.6, 3.3) |
| 흔들림 | 7.2 |
| 처치 연출 | 시체 층 + 작은 보석 튀어 오름(표시만, 0.25s, 실제 보석은 시뮬레이션 위치에 바로 있음) |
| 위험 예고 | `Telegraph` 개체를 1층에 원/선으로, 남은 시간 비율만큼 안쪽이 차오름 |
| 레벨업 빛 기둥·정예 상자 | 표시 이벤트 → `FxSystem`(`fx_levelup_pillar` = 원작 `ressur1`). 선택 뒤 주변 적 밀어내기는 시뮬레이션(3.3, `level_up_push`) |
| 커서 | `DIInop.cur`(원작 exe) 를 `CursorLoader` 가 읽음(ICO 머리글 → 첫 항목, PNG 면 그대로 / BMP 면 32비트·AND 마스크 해석) → `MouseCursor.FromTexture2D(tex, 0, 0)` |

### 7.9 소리 (v0.3, WP8, 기획 12.7)

**흐름** — Core 의 시뮬레이션은 소리를 모른다.

```
시뮬레이션 단계들 ── 표시 이벤트 ──▶ PresentationBuffer { Effects(FxEvent), Corpses, DamageNumbers, Shakes, Cues(CueEvent) }
                                                      │ 프레임 끝, PlayScene: FxSystem.Consume → Audio.Consume → Clear
                                                      ▼
            AudioSystem(게임) ── 연결 표(6.15)로 소리 ID ──▶ SoundMixer(Core, 고르기) ──▶ SoundBank·MusicPlayer(재생)
씬(타이틀·오버레이) ── PlayUi("menu_move") / PlayMusic("title") ─┘
```

**신호 `CueEvent`**(Core `Sim/Contracts/Presentation.cs`, 계약 WP1b) — "무엇이 일어났나" 만 싣는다.

```csharp
public enum CueKind : byte { WeaponFired, EnemyAttack, EnemySpecial, EnemyHit, EnemyShot, HeroHurt, HeroDied,
                             PickupCollected, PickupDropped, BossArrived, BossPhase, ElitePack }
public readonly record struct CueEvent(int Tick, CueKind Kind, Vector2 Pos, IDef? Subject, EntityId Source = default);
// PresentationBuffer.Cues + Cue(in CueEvent) — Enabled 가 false(그래픽 없는 시험)면 쌓지 않는다
```

| 신호 | 내는 곳(단계) | `Subject` | 연결 키(6.15) | 분류 |
|---|---|---|---|---|
| `WeaponFired` | WP3 `Weapons`(발동 한 번에 하나 — 여러 발이어도) | `WeaponDef` | `weapons.<id>.fire` | weapon_fire |
| `EnemyAttack` / `EnemySpecial` | WP4 `EnemyThink`(능력·패턴이 `attack`·`special` 클립을 시작할 때) | `EnemyDef` | `enemies.<id>` → 없으면 `sprite_sets.<묶음>` 의 `attack`/`special` | monster(보스면 boss) |
| `EnemyHit` | WP3 `DamageResolve`(적에게 실제로 들어간 피해) | `EnemyDef` | 같음, `hit` | monster_hit |
| `EnemyShot` | WP4 `shoot_projectile`·`projectile_spread` | `ProjectileDef` | `projectiles.<id>.fire` | monster(보스면 boss) |
| `HeroHurt` / `HeroDied` | WP3 `DamageResolve`(피격 무적이 아닐 때 한 번) / 죽음 처리 | — | `hero.hurt` / `hero.death` | hero |
| `PickupCollected` / `PickupDropped` | WP3 `Pickups` / 드롭 생성 | `PickupDef` | `pickups.<id>.collect` / `.drop` | pickup |
| `BossArrived` / `BossPhase` | WP4 `boss`·`miniboss` 이벤트 / `PhaseController`(페이즈 `voice: true`) | `EnemyDef` | `enemies.<id>.voice` | boss_voice |
| `ElitePack` | WP4 정예 무리 등장 | — | `ui.elite_pack` | ui |

- 이미 있는 표시 이벤트에서 나오는 소리: `FxEvent` → `effects.<이펙트 ID>`(무기 맞음·폭발·산 웅덩이·피 분수, 분류 weapon_hit), `CorpseSpawned` → 그 적의 `death`(분류 monster, 같은 종 0.1s 에 한 번).
  화살처럼 맞음 이펙트가 없는 것은 소리도 없다(기획 12.7 "화살은 없음").
- 장판 반복 소리: `AudioSystem` 이 프레임마다 `World.Hazards` 에서 `hazard_loops` 에 연결된 이펙트가 하나라도 살아 있는지 보고, 있으면 반복 소리 하나를 켜 두고 없으면 끈다(기획 12.7 "장판 수와 상관없이 하나").
- 씬 소리: 레벨업 창(`ui.level_up`)·상자 열기(`ui.chest_open`)·메뉴 이동/선택은 오버레이·씬(WP6)이 `IAudioServices.PlayUi` 로 직접 부른다. 음악은 씬이 `PlayMusic("title")`, `PlayScene` 이 `PlayMusic(<스테이지 ID>)`.

**고르기 `SoundMixer`**(Core `Audio/`, 순수 로직 — 시험 대상)
- 한 프레임에 들어온 요청을 (분류 우선순위, 생긴 틱, 들어온 순서)로 정렬해 차례로 본다 → 결정적.
- 버리는 규칙(기획 12.7 표, 값은 `rules.audio`): 같은 소리 ID 최소 간격 0.05s, 같은 소리 동시 3(정의 `max_instances`), 몬스터 맞음 전체 0.08s 에 한 번, 몬스터 죽음 동시 4·같은 종 0.1s,
  보석 0.06s, **전체 동시 24 — 자리가 없으면 새 소리를 버린다**(이미 나는 소리는 끊지 않음, 기획 12.7).
- 화면 밖: 그 분류가 `offscreen: false` 이면 `ViewRect` 를 `offscreen_margin`(100px)만큼 넓힌 사각형 밖에서 난 소리를 버린다(보스·중간 보스·이벤트는 늘 냄).
- 좌우: `(x − 시야 가운데 x) / (시야 폭 / 2) × pan(0.5)`, 높낮이: ± `pitch_jitter`. 무작위는 게임 쪽 난수(시뮬레이션 `SimRandom` 과 따로 — 기획 12.7, 15절).
- **자리가 비는 시각은 소리 길이로 센다**: 동시 재생 수(전체 24·같은 소리 3·몬스터 죽음 4)를 세려면 이미 나는 소리가 언제 끝나는지 알아야 한다.
  길이 = `d1/sound/index.json`(1.4, `SoundIndex`)의 표본 수 / 표본율 ÷ 높낮이 배율을 앱 틱으로 올림한 값. 고른 소리는 (끝 앱 틱)을 기록하고, 그 틱이 지나면 자리를 비운다. 반복 소리는 끌 때까지.
  `SoundEffectInstance.State` 는 고르기에 **쓰지 않는다**(장치 지연·벽시계에 따라 달라 로그 수 재현이 깨진다). 재생 칸이 실제로 아직 소리를 내고 있으면 그 칸만 다음 프레임에 다시 본다(고르기 결과와 무관).
- 시간 기준은 앱 틱(벽시계가 아님) — 같은 실행이면 같은 고르기 결과(로그 수까지 재현). 무음과 소리 켬도 같은 길이 색인을 쓰므로 `AUDIO` 수가 같다(10.1 `Audio` 시험).

**재생**(게임 `Audio/`)
- `SoundBank`: `SoundEffect.FromFile(path)`(파이프라인 없이, 16비트 PCM — 가져오기가 바꿔 둠). 스테이지 시작 때 연결 표에서 그 스테이지에 쓸 소리만 읽는다(`TextureStore` 와 같은 "나올 수 있는 것" 계산). 효과음 전부 수 MB.
- 목소리 칸: `SoundEffectInstance` 24개 풀 + 반복 소리 칸. 음량 = 설정(전체 × 효과음) × 분류 × 정의.
- `MusicPlayer`: 곡은 최대 두 곡만 메모리에(바뀌는 동안 옛 곡·새 곡, 가장 긴 `dlvlb` 약 15MB), `SoundEffectInstance.IsLooped`. 보스 대사가 나면 `voice_duck`(50%, 2s).
  장면이 바뀌면 **파일 읽기는 작업 스레드**에서(`File.ReadAllBytes` → `byte[]`) 시작하고 옛 곡을 `music_fade`(0.5s)에 걸쳐 줄인다. 읽기가 끝난 뒤 첫 프레임에 주 스레드가 `SoundEffect` 를 만들어 튼다
  (`SoundEffect.FromStream` — OpenAL 버퍼 올리기는 주 스레드). 플레이 씬의 곡은 `PreloadStage` 가 미리 읽는다. 페이드가 끝나는 프레임에 15MB 읽기·버퍼 올리기가 겹쳐 멈추지 않게 한다.
  `slow_frames`(10.3) 로그로 장면 바뀜 프레임을 확인한다.
  일시정지 오버레이가 열리면 효과음 인스턴스를 `Pause()`, 음악을 `pause_music`(50%)로 — 닫히면 되돌린다(기획 12.3). 페이드·덕킹은 앱 틱으로 센다.
- **장치**: MonoGame DesktopGL 3.8 은 `Game` 을 만들 때 OpenAL 로 **기본 오디오 장치를 연다**(게임 코드로 막을 수 없다 — `MonoGame.Framework.dll` 3.8.6 에서 `Failed to init OpenALSoundController` 문자열 확인).
  그래서 개발 옵션 실행은 `new GameApp()` 전에 환경 변수 **`ALSOFT_DRIVERS=null`** 을 둔다(MonoGame 이 싣는 OpenAL Soft 의 출력 없는 드라이버 — `openal.dll` 에 `ALSOFT_DRIVERS`·`No Output` 문자열 확인, SDL 힌트와 같은 방식).
  장치는 "No Output" 으로 열리고 스피커로는 아무 소리도 나지 않는다. `--sound-device default`(9절)를 줄 때만 이 변수를 두지 않는다(사용자 동의를 받고).
  보통 실행(개발 옵션 없음)은 기본 장치. 장치 초기화가 예외로 끝나면 `GameApp` 생성을 감싼 `Program` 이 로그를 쓰고 `ALSOFT_DRIVERS=null` 로 한 번 다시 만든다. 그것도 실패하면 종료 코드 1(R21, WP7 이 첫 실행에서 확인).
- **무음**: `AppOptions.Muted`(9절 `--mute`, 개발 옵션 실행의 기본값)이면 재생하지 않고 효과음 파일도 읽지 않는다. 길이 색인(`d1/sound/index.json`)은 읽고 `SoundMixer` 를 그대로 돌려 수를 센다(재생만 안 함).
  무음이어도 MonoGame 이 장치를 여는 것은 막을 수 없다 — 개발 옵션 실행은 위 null 드라이버라 상관없다. 오디오 장치를 못 열면(`NoAudioHardwareException` 등) 로그 경고 후 무음으로 계속한다.
- **소리 켬 확인 경로**: 개발 옵션 실행의 `--sound` 는 null 장치로 **WAV 읽기·`SoundEffect` 만들기·재생·제한을 끝까지** 돈다 → 스피커 없이도 실제 재생 경로가 확인된다(10.2).
- 로그: 시작 때 `AUDIO device="<장치 이름>"`(null 드라이버면 `No Output`), 종료 때 `AUDIO muted=… requested=… played=… dropped_cap=… dropped_interval=… dropped_offscreen=…`.
- **결정성**: 소리는 시뮬레이션을 읽기만 한다. 같은 씨앗이면 소리를 켜든 끄든 상태 해시가 같다 — Core 쪽은 `Presentation.Enabled` 켬/끔 시험(10.1 `Sim`),
  게임 `AudioSystem` 이 `World` 를 받는 경로는 `--mute` 실행과 null 장치 `--sound` 실행의 `--log-hash` CSV 비교(10.2 "재현"). `Core/Sim` 소스가 `Core.Audio` 를 참조하지 않는지 시험이 본다(10.1 `Audio`).

```csharp
public interface IAudioServices            // 게임 App/Contracts (계약 WP1b, 구현 WP8 Audio/AudioServices.cs 가 만든다)
{
    bool Muted { get; }
    void PreloadStage(StageDef stage);                     // PlayScene 시작
    void Consume(PresentationBuffer buffer, World world);  // PlayScene 프레임 끝: FxSystem.Consume 다음, buffer.Clear 전
    void PlayUi(string key);                               // "menu_move", "menu_select", "level_up", "chest_open"
    void PlayMusic(string key);                            // "title" 또는 스테이지 ID
    void SetPaused(bool paused);
    void SetVolumes(float master, float sfx, float music);  // 설정 화면·시작 때
    void Update(AppTick now);                              // 앱 틱마다: 페이드·덕킹
}
```

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
    void AutoStep(IAutoplayDriver bot) { }             // --autoplay 가 메뉴를 넘길 때 (WP1 코드: 인터페이스, 구현은 WP6 AutoplayDriver)
}
public interface IAutoplayDriver { AppTick Now { get; } bool Ready(IScene scene); int PickCard(LevelUpOffer offer); }
```

| 씬 | 종류 | 들어가는 곳 → 나가는 곳 |
|---|---|---|
| `DataErrorScene` | 씬 | 시작 때 데이터 오류 또는 **자원 없음·낡음**(1.4 끝: 무엇이 없는지 + 가져오기 명령 한 줄) → 종료만 |
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
  프레임 끝에 `FxSystem.Consume` → `AppServices.Audio.Consume` → `world.Presentation.Clear()` 순서로 표시 이벤트를 소비한다(3.5·7.9). 마우스 이동은 `AppServices.Render.ScreenToWorld` 로.
- 소리(v0.3): 타이틀·스테이지 선택·결과는 `PlayMusic("title")`, `PlayScene` 은 `PreloadStage` 뒤 `PlayMusic(<스테이지 ID>)`, 일시정지 오버레이는 `SetPaused`, 레벨업·상자 오버레이와 메뉴는 `PlayUi`(7.9).
- 씬이 바뀔 때마다 로그에 `SCENE <이름> app_tick=…`, 결과 화면이 열릴 때 `RESULT …` 를 쓴다(9절).

### 8.2 HUD (기획 12.1)

- `Hud` 하나가 `World` 를 읽어 그린다: 경험치 막대, 시간, 보스 막대(최종 위·중간 아래), 무기·패시브 칸, 처치·금화, 미니맵, 체력 구슬, 지속 효과, 알림 문구.
- 체력 구슬은 코드로: 원 마스크 텍스처(생성) + 붉은 채움 사각형을 비율만큼(원 안만 보이게 그리기 순서로) + 돌 테(코드로 만든 회색 고리 그라데이션).
  v0.2 의 "맵 질감으로 구운 테" 는 장애물 굽기와 함께 없앴다. 원작 UI 그림(`ctrlpan\*.cel`)으로 바꿀지는 기획 열린 질문 17.
- 미니맵: 가져오기 도구가 구운 맵의 `minimap.png`(외곽 상자 1/16, 마름모, 막힌 곳 어둡게 — 7.6)를 알파 0.75 로. 점은 1px 텍스처 늘이기, 좌표는 월드 ÷ 16.
- 위젯: `Label`, `Button`, `Card`, `Bar`, `IconSlot` — 즉시 모드에 가까운 작은 클래스. 마우스 올림·방향키 선택·Enter 를 같은 "포커스" 개념으로.
- 글꼴: `C:\Windows\Fonts\malgun.ttf` → `malgunbd.ttf` 순으로 찾고, 모두 없으면 `arial.ttf` + 로그 경고(한글이 네모로 보임).
  `gulim.ttc` 같은 글꼴 묶음(.ttc)은 `FontSystem.AddFont(byte[])` 에 몇 번째 글꼴인지 고를 인자가 없어 목록에서 뺐다. 실제로 넣어 보고 되면 다시 넣는다.

### 8.3 저장 (기획 열린 질문 3)

- 위치: `%LOCALAPPDATA%\DiabloDefenceRemastered\save.json`(기록·해금·누적 금화·설정 한 파일, `version` 필드). 쓰기는 임시 파일 → 바꿔치기.
- 설정(`GameSettings`, 계약 변경 WP1b): 전체 화면, 화면 흔들림, 미니맵, 피해 숫자, **음량 셋**(전체·효과음·음악, 0~100% 10% 단위 — 기획 12.3). 저장 파일에 없으면 음량은 `rules.audio.defaults`(80·80·60%) — `GameSettings` 의 음량은 `float?`(null = 없음)이고 기본값은 rules 한 곳에만 둔다.
- `--save-dir <폴더>` 로 위치를 바꾼다. 개발 옵션 실행(9절 목록)인데 `--save-dir` 가 없으면 사용자 저장 파일을 **읽지도 쓰지도 않는다** — 기본값(창 모드, 스테이지 1 만 열림, 기록 없음)으로 시작하고 메모리에만 저장한다.
  읽기까지 막는 이유: 사용자가 켜 둔 전체 화면이 검증 실행에서 화면을 덮고, 사용자의 해금 상태에 따라 자동 조작이 고르는 스테이지가 달라지기 때문이다(brief 7절).

---

## 9. 개발용 실행 옵션

`Core/Dev/DevOptions.Parse(string[]) → (DevOptions, IReadOnlyList<string> errors)`. 모르는 옵션·잘못된 값은 오류 목록 → 로그 + 종료 코드 64.
시뮬레이션에 걸리는 것은 `RunOptions`(11.4), 앱·화면·소리에 걸리는 것은 `AppOptions`(게임 `App/Contracts`)로 넘긴다.

| 옵션 | 뜻 |
|---|---|
| `--seed <n>` | 시뮬레이션 씨앗. 없으면 시각에서 만들고 로그 첫 줄에 적음 |
| `--autoplay` | 자동 조작(기획 15절). 메뉴도 넘김(각 화면 0.5s 뒤). `--background` 를 함께 켬. 실제 키보드·마우스는 읽지 않음 |
| `--stage <id>` | 타이틀·선택을 건너뛰고 바로 시작(잠금 무시). `stage_cathedral`, `stage_catacombs` |
| `--start-at <mm:ss>` | 그 시각부터. 지난 이벤트는 건너뜀, 구간·배율은 그 시각 값. **보스 시각 이후면 보스를 바로 내보내고 보스 시계(광폭화)를 (시작 시각 − 보스 시각)으로 맞춘다**. 중간 보스는 건너뜀 |
| `--time-scale <n>` | 실제 시간 n 배속. 결과는 n 과 상관없이 같다(3.4) `[해석]` |
| `--god`, `--level <n>`, `--weapons <id:lv,…>`, `--passives <id:lv,…>` | 기획 15절 |
| `--stress <적 수>[,<투사체 수>]` | 상한을 그 값으로 올리고 스테이지 구성 가중치대로 맵 전체·시야 띠에 즉시 채운다(`--god` 함께 켬, 생성 출처 `Stress`). 투사체 수를 주면 그 수를 유지하는 시험용 발사를 더함 |
| `--freeze-ai` | 적 생각·이동을 멈춘다(렌더 정적 장면, `--stress` 와 함께) |
| `--capture <png>` | 캡처 파일 경로. 상대 경로는 현재 폴더 기준. 원작 그림이 찍히므로 **저장소 밖**(스크래치·임시 폴더)에 둔다(R24 — 저장소의 `out/`·`logs/` 는 `.gitignore` 로 막지만 다른 곳은 못 막는다) |
| `--capture-at <초[,초…]>` | 앱 시간(초) 기준. 여러 개면 파일명 뒤에 `_<초>` 를 붙인다. 그 틱에 정확히 멈춰 보간 없이(alpha = 1) 그린 화면 |
| `--capture-on <levelup\|chest\|boss\|result>` | 그 일이 처음 일어나 화면이 다 열린 프레임을 캡처(시각을 미리 모를 때) |
| `--quit-after <초>` | 앱 시간 기준. 종료 코드 0. 캡처가 남아 있으면 먼저 한다 |
| `--exit-on-result` | 결과 화면이 열리면 `RESULT` 로그를 쓰고 종료 코드 0 |
| `--timeout <초>` | **벽시계** 감시. 넘으면 로그에 마지막 앱 틱·씬·스택 요약을 쓰고 종료 코드 4(틱이 안 도는 교착·무한 루프 대비). 개발 옵션 실행 기본 600 |
| `--window <W>x<H>` | 창 클라이언트 크기이자 **캡처 크기**(7.1). 개발 옵션 실행 기본 1280x720 |
| `--background` | 창을 앞으로 끌어오지 않음: `new GameApp()` 전에 환경 변수 `SDL_WINDOW_NO_ACTIVATION_WHEN_SHOWN=1`(SDL2 는 힌트를 환경 변수에서도 읽는다 — P/Invoke 없음), 비활성일 때 멈추지 않음(`InactiveSleepTime = 0`), 마우스 가두지 않음 |
| `--vsync <on\|off>` | 수직 동기(기본 on). 부하 측정은 `off` |
| `--mute` (v0.3) | 소리를 재생하지 않고 효과음 파일도 읽지 않는다(길이 색인만 읽어 고르기 수는 그대로 센다, 7.9). **개발 옵션 실행의 기본값**이다. 장치 자체는 MonoGame 이 열므로, 개발 옵션 실행은 늘 `ALSOFT_DRIVERS=null`(출력 없는 장치)로 연다(7.9) `[해석]` |
| `--sound` (v0.3) | 개발 옵션 실행에서 재생 경로를 켠다 — 장치는 여전히 null("No Output")이라 스피커로는 나지 않는다. WAV 읽기·재생·제한 확인용. `--mute` 와 같이 주면 옵션 오류 |
| `--sound-device default` (v0.4) | 개발 옵션 실행에서도 실제 기본 장치를 연다(`ALSOFT_DRIVERS` 를 두지 않음, `--sound` 를 함께 켬). 사용자 스피커로 소리가 나므로 **사용자 동의를 받고만** 쓴다 |
| `--debug-nav [grid,flow,astar,shapes,entries,tiles]` | 격자·막힌 칸(S/M 등급 색)·clearance 열지도·플로우 필드 화살표·A* 원 경로와 다듬은 경로·충돌 원·충돌 도형(평행사변형)·입구(시작점→진입점)·기준점 ID·**하위 칸 성질(`W`/`M`/`X` 색)** 을 덮어 그림. 층은 옵션 값으로 고른다(기본 전부). 키 입력으로 바꾸지 않는다 |
| `--preview <sprites\|enemies\|effects\|tiles\|blend>` | 개발 화면. 그림 + 발 기준점 십자 + ID. 배치는 고정(6.13): 정의마다 한 행, 방향·프레임 고정 칸 + 방향을 도는 칸 하나. `tiles` = 맵 전체 축소 + 성질 겹침(7.6), `blend` = 무기 투사체·이펙트를 보통/가산 두 칸으로(기획 열린 질문 10). v0.2 의 `obstacles` 는 없앴다 |
| `--check` | 불변식 계측(10.1)을 켠다(`RunOptions.Check`). `--log-ticks` 에 열로, 종료 요약에 최댓값 |
| `--validate-data` | 창 없이 데이터 검사만, 종료 코드 0/2(자원은 보지 않는다) |
| `--log <파일>` | 로그 파일. 기본: 개발 옵션 실행이면 **현재 폴더** `logs\`, 아니면 저장 폴더 `logs\`. 저장소 뿌리에서 돌리면 `logs\` 가 생기므로 `.gitignore` 에 `logs/` 를 둔다(WP1b) |
| `--log-ticks <csv>` | 틱 시간 기록(10.3) |
| `--log-hash <csv>` | 스테이지 틱 60마다 `app_tick, stage_tick, hash`(3.4 `StateHasher`) |
| `--save-dir <폴더>` | 8.3 |

- **사용자 데이터 보호**(brief 7절): 개발 옵션(`--autoplay`·`--stage`·`--capture*`·`--preview`·`--validate-data`·`--stress` 중 하나라도)이 있고 `--save-dir` 가 없으면
  저장 파일을 **읽지도 쓰지도 않고** 기본값으로 시작한다(창 모드, 해금 = 스테이지 1 만, 기록 없음). `--window` 는 저장된 전체 화면 설정보다 늘 우선한다. 같은 조건이면 소리도 기본 무음이다.
- 자동 실행에서 데이터 오류가 나면 DataError 화면을 띄우지 않고 종료 코드 2 로 끝난다(입력을 기다리며 멈추지 않게). 자원이 없거나 낡았으면 종료 코드 5(1.4 끝 — `--autoplay` 없이 캡처만 주면 안내 화면을 캡처한 뒤 5).
- 개발 옵션 실행은 `new GameApp()` 전에 환경 변수 `SDL_WINDOW_NO_ACTIVATION_WHEN_SHOWN=1`(`--background`)과 `ALSOFT_DRIVERS=null`(`--sound-device default` 가 없을 때, 7.9)을 둔다.
- 종료 코드: 0 정상, 1 처리 안 된 예외(로그에 스택), 2 데이터 오류, 3 요청한 캡처가 하나라도 안 생김, 4 벽시계 시간 초과, 5 자원 없음·낡음, 64 옵션 오류.
- 고정 로그 문장(판정용): 첫 줄 재현 정보(3.4), `ASSETS manifest=… format=… stage=… textures=… vram_mb=…`, `SCENE <이름> app_tick=…`, `RESULT stage=… outcome=victory|defeat stage_time=… level=… kills=… seed=…`,
  `AUDIO device="…"`, `AUDIO muted=… requested=… played=… dropped_cap=… dropped_interval=… dropped_offscreen=…`, 끝 줄 `EXIT code=…`.
- 실행 파일은 WinExe 다. **Windows GUI 실행 파일은 셸이 끝나기를 기다리지 않으므로** 종료 코드는 아래처럼 받고, 판정 근거는 로그 파일로 한다.
  데이터 검사의 1차 판정은 창이 필요 없는 `dotnet test` 의 실제 데이터 시험이다.
- 검증 담당의 실행 예(PowerShell, 사용자 화면·입력·저장·스피커를 건드리지 않음). 결과 폴더 `$o` 는 **저장소 밖**(스크래치)이다 — 캡처에 원작 그림이 찍힌다(R24):

```powershell
$o = "$env:TEMP\ddr-run"   # 저장소 밖
$p = Start-Process -FilePath .\DiabloDefenceRemastered.exe -Wait -PassThru -NoNewWindow -ArgumentList @(
  '--autoplay','--seed','7','--stage','stage_cathedral','--window','1280x720',
  '--capture',"$o\s1.png",'--capture-at','30,120','--quit-after','125',
  '--save-dir',"$o\save",'--log',"$o\run.log",'--log-hash',"$o\h1.csv",'--check')
$p.ExitCode
```

---

## 10. 테스트 전략

### 10.1 단위 테스트 (xunit.v3, Core 만, 그래픽 없음, 자원 없음)

| 폴더 | 무엇을 | 담당 |
|---|---|---|
| `Common` | `FacingMath.ToColumn`(8방향: 남 0, 서 2, 북 4, 동 6 / 16방향 / **등각 보정: 화면 (−32, 16) → 8방향 열 1**, 보정 끔이면 45° 열), **`FromColumn` 왕복 8·12·16방향(등각 보정 포함)**, **`ToVector(SouthWest)` → 8방향 열 1·16방향 열 2**, `SimRandom` 재현, `StableHash`, `EntityStore` 핸들(지운 뒤 낡은 핸들은 무효)·압축, 풀 재사용, `AnimClock`(이벤트 프레임 1부터·넘김, 재생 배율), `CameraMath`, `TickAccumulator`(틱 상한·버린 틱·캡처 멈춤, `--time-scale` 1 과 8 이 같은 앱 틱 열), `DefRef` 한 번만 채움, `StageStepper`(히트 스톱·늘임·`AwaitingChoice`) | WP1, WP1b |
| `Map` | `IsoMath` 칸 중심 ↔ 칸 번호 왕복, 외곽 상자(N 40 → 2560×1408, N 44 → 2816×1536), 바깥 마름모 안/밖, `ArenaLayout.Expand`(합성 타일셋: 테두리 여덟 자리·문 2칸 + 끝 조각·`place`·`line`·바닥 섞기 결정적·둘레 표시), 성질 → 하위 칸, `LayoutHash` 가 배치가 같으면 같고 기둥 하나 옮기면·성질 표 한 칸 바꾸면 다름, 성질 표에 없는 메가타일 오류·쓰지 않는 메가타일이 표에 있으면 오류, `MapDataReader`(작은 JSON: 두 종류만 읽음·다른 종류의 오류와 무관·타일셋 연결·`extends` 오류), `ResolvedMap`(타일 → 월드 px, 입구·기준점·시작) | WP1b |
| `Assets` | `SpriteMeta` 읽기·검사(사각형이 페이지 밖·개수 ≠ 방향 × 프레임·없는 페이지 번호면 오류), `ImportTable`(표 8개 엄격 읽기·모르는 속성 오류, 이름 목록: 묶음 × 변종 페이지, 마법 방향 묶기, 아이콘, 소리 `vars` 펼치기 → `d1/sound/…`, 동작별 `width`·`shadow_alpha` 덮어쓰기 순서), `ArenaArt`(바닥 조각 이름 규칙 — 합성 배치에서 손으로 센 조각 목록과 같음, `cells` frame 1부터), manifest 판 대조. **13.5-10**: ① 표 → 이름 목록(기본) ② 실제 정의가 가리키는 이름 ⊂ 표가 만드는 이름 + `exe.json`(`Integration`, WP7 로더 필요). **도구 경로와 게임 경로의 `LayoutHash` 가 같음**(실제 `Data/`, `Integration`). 자원이 있으면(`DDR_ASSETS`) manifest·메타·PNG IHDR 를 정의와 대조, 없으면 건너뜀 표시, 판·표 해시가 다르면 "낡음" 건너뜀 표시(1.3) | WP1b(형), WP0(이름 대조) |
| `Data` | 규칙별 고장 난 작은 JSON(`Fixtures/`)으로 각 오류가 정확한 파일·경로로 나오는지. `extends` 병합·순환, `*_add`·`*_patch`, **페이즈 누적 풀기(patch → add → remove, 같은 ID 자리 지킴)**, 모르는 속성, 비슷한 ID 제안, 항목마다 오류 모음, `rules` 하위 키 합치기·중복, **파일 목록을 뒤섞어 읽어도 `GameDefs` 해시가 같다**, 가중치가 JSON 순서 배열, 변종이 그림 묶음에 없으면 오류 | WP7 |
| `Data`(실제, `Integration`) | 실제 `Data/` 를 엄격 모드로 읽어 오류 0(모든 모듈 검사기 포함 — 길 검사·소리 연결 표). 모든 모듈 등록이 필요하므로 통합 때 기본으로 옮김 | WP7 작성, WP6 통합 |
| `Geometry` | 원·사각형·다각형(오목 포함) 부호 거리, 밀어내기 방향, 선분 교차, 원 중심에 놓인 점 | WP2 |
| `Nav` | `ArenaCollision`: 평행사변형 합집합 = 막힌 칸 합집합(점 표본 1만 개), `W` 칸은 걷기만·투사체 통과(**합성 타일셋 시험** — 실제 MVP 맵에는 `W` 칸이 없다, 4.2), 둘레 도형 `IsBoundary` 는 `phase_through` 도 막음. clearance 알려진 점 값(바깥 마름모 경계 포함), 작은 손 격자에서 플로우 방향, A* 최적성(무작위 격자 200개에서 다익스트라와 비교 — **끈 당기기 전** 경로), 코너 자르기 없음, 다듬은 경로가 막힌 곳을 지나지 않음, 목표 못 갈 때 가장 가까운 곳, 시작이 3칸 밖까지 막힘. **두 스테이지 실제 맵 전수(자원 없이)**: (a) 모든 통과 칸에서 `dir` 을 따라가면 칸 수 이하 걸음에 시작점(고리 없음) (b) 모든 통과 칸 중심의 점 유닛이 이중선형 방향으로 10초 안에 주인공 120px 안 (c) 마름모 네 꼭짓점 쪽·기둥 옆·(스테이지 2) 협로 안에 주인공을 둬도 `S`·`M` 필드가 모든 통과 칸에서 dist < 65535 (d) 13.5-4~6 | WP2 |
| `Spatial` | 무작위 점 집합에서 해시 질의 = 전수 비교 결과(거리로 거른 정확한 집합), 결과 순서 결정적, **한 칸에 200개 몰아 넣었을 때 넘침 규칙**(돌려준 개수·반경 줄여 다시) = 전수 | WP2 |
| `Movement` | 두 적 같은 좌표 → 1틱 뒤 둘 다 유한·떨어짐, 20마리 같은 점 행진 → 진입 2초 뒤 모두 유한·겹침 해소, 기둥(평행사변형) 중심 소환 → 밖으로 나옴, 입장 중 적은 해시·분리에서 빠짐, 이동 의도 6종, `PlacementResolver`(마름모 모서리·기둥 옆 소환 → 모두 통과 칸, 생략 수 결정적) | WP2 |
| `Invariants` | 계측기가 일부러 만든 NaN·맵 밖(바깥 마름모)·장애물 안·멈춘 적을 센다 | WP2 |
| `Combat` | `DamageMath` — 기획 4.2 예(오라+함성 = ×1.55, 반올림 마지막, 방어 후 최소 1, 치명), `damage_pct` 한 번만, 발사 시점 고정, 시간 배율 제외 대상, 등급 배율, 피격 무적, `--god`, 승리 대기 중 주인공 피해 무시, 입장 중 적만 있는 월드에서 매직 미사일이 쏘지 않음·장판 위를 지나도 HP 그대로, **투사체 `on_end` 장판(맞음·사거리 끝·장애물)**, 신호(`WeaponFired` 발동당 하나, 피격 무적 중 `HeroHurt` 없음) | WP3 |
| `Weapons` | `WeaponStats.Resolve`(count+projectile_count, area_pct 는 선언된 키에만, 쿨 상한 0.6/마력 폭주 0.8, **damage 는 damage_pct 와 무관**, `null` = 상시), 조준 5종, 패턴별 발사 결과(발 수·퍼짐각·궤도 위치·접선 `Vel`), 관통·튕김이 같은 적을 두 번 안 때림, 적별 타격 간격 | WP3 |
| `Progression` | 경험치 곡선(1→5, 2→15, 19→185, 20→198, 40→461), 레벨업 후보(가중치·칸 참·초반 보장·채움·3장 미만), 재추첨 수, 상자(진화 우선·중간 보스 3회·강화 없으면 금화), 레벨업 밀어내기(반경 150, 80px) | WP3 |
| `Enemies` | **전수 연기 시험**(6.13), 점화 중 죽으면 폭발 없음, 분열 3마리 일반 등급·튕김 80px(장애물에서 멈춤)·0.3s 대기, 대장 죽음 → **그 대장의 호위만** 공포 1.5s, 호위 없는 대장 죽음 → 주변 해골 공포 없음, **`panic_on_ally_death`(반경 150 안 같은 종 죽음 → 0.8s 도망, 1.5s 안에 다시 안 겁먹음, 정예는 안 겁먹음, `Collapse`·`Culled` 로는 안 겁먹음)**, `escort` 대형 120·풀림 250·출처별 호위 수(이벤트 값 / 기본 8 / 구간 0), 행진 직진 시간 뒤 보통 행동(`toward`·`toward_entry`), `kite` 거리 띠·물러날 수 없으면 제자리 사격, `shoot_projectile` 가리면 안 쏨, 돌진 예고·시간, `ground_hazard` 동시 20 상한, `war_cry` 겹치지 않음(큰 값 하나), 버프 면역(`slow`), `phase_through` 경계만 막힘·`alpha_over`, `heavy_strike` 넉백, `summon` 상한 30·절대 상한 360, 보스 페이즈 50% 1회·패턴 덮어쓰기·쿨다운이 같은 패턴에 남음·`voice` 신호, 패턴 `actions` 두 개가 같은 프레임, 접두사 합치기·제외 조건, `attack_cooldown_pct` 가 모든 능력 쿨에, 승리 때 `Collapse`(분열·폭발·드롭 없음) | WP4 |
| `Director` | 누적기(무리 1~3, `spawn_group`, 최대 수에서 3 고정), 생성 위치가 시야 띠 안·걸을 수 있음·여유 거리, 12번 실패 → 먼 입구, **마름모 모서리 주인공에서 입구 대체 비율 기록**(R23), 상한 300/360 과 출처별 막는 순서·지우는 순서(`Culled` 는 보상·시체 없음), 포위 고리 반경 줄이기·생략(마름모 밖 자리), 정예 종류 뽑기에서 `elite_eligible: false` 제외, `--start-at` 이 지난 이벤트 건너뜀·보스 시각 이후면 보스 바로, **생성 수**: 상한 무한·처치 없음 월드에서 구간마다 `floor(초당 × 구간 길이)` ± 3 | WP4 |
| `Audio` | `SoundMixer`: 전체 24·같은 소리 3·최소 간격 0.05·몬스터 맞음 0.08s 한 번·죽음 4·같은 종 0.1s·보석 0.06s, 자리가 없으면 새 소리를 버리고 높은 우선순위가 먼저 자리를 얻음, **자리는 길이 색인의 끝 틱에 빔(높낮이 배율 반영)**, 화면 밖 버림(분류별), 좌우 값, 같은 입력 같은 결과, **같은 신호 열 → 무음과 소리 켬의 `AUDIO` 수가 같음**. 연결 표 검사기(없는 대상·없는 소리·없는 분류 → 오류, 소리 없는 그림 묶음 → 경고). **`Sim/` 소스 파일이 `Core.Audio` 를 참조하지 않음**(저장소 소스 문자열 검사) | WP8 |
| `Art` | 아틀라스 메타·PNG 대조 검사기(자원 있을 때만), PNG IHDR 읽기 | WP5 |
| `Dev`·`Save` | `DevOptions` 파싱(값 형식, 여러 캡처 시각, 오류 메시지, 개발 옵션이면 저장 안 읽음·기본 무음 판정, `--sound`/`--mute`), `StateHasher` 가 같은 상태에 같은 값, 저장 직렬화·버전(음량 셋 포함) | WP7 |
| `Sim` | **결정성**: 같은 씨앗·자동 조작으로 30초(기본) / 3분(`Scenario`) 두 번의 상태 해시 열이 같다. **묶음 독립성**: `TickAccumulator` 로 프레임당 1틱 / 4틱으로 묶어 `StageStepper` 를 돌려도 해시 열이 같다(레벨업 오버레이 흉내 포함). **표시 이벤트 켬/끔**(`Presentation.Enabled`)과 상관없이 해시 같음. **할당**: 정상 상태 틱 1,000번 동안 증가 0. `Scenario`: `stage_cathedral` 10분(자동 조작 + `--god`, 예외 없음, 불변식 기준 통과). `Perf`: 4.9 표 | WP4 |

| 프로젝트 `DiabloAssetImporter.Tests` | 무엇을 | 담당 |
|---|---|---|
| `Formats` | 시험 인코더로 만든 CL2: 그룹/평면 판별(그룹 머리가 평면 표로도 읽히는 경우), RLE 세 종류, 32줄 건너뛰기 표로 폭 추정(표 `width` 와 다르면 종료 코드 4 경로), 줄이 넘어가는 런. 레벨 CEL 조각 여섯 종류(삼각형 패딩 앞/뒤), MIN 뒤집기, TIL 펴기(0 기반), SOL 두 비트 대조(다른 비트·함정 128 무시), TRN → PAL 순서, 색인 0 → 그림자 알파(값이 없는 표는 검정 그대로), WAV 8비트 → 16비트 값·머리·소리 색인 값 | WP0 |
| `Bake` | 잘라 묶기: 겹침 없음·페이지 안·순서 고정·빈 프레임·**그림자만 있는 줄이 사각형 안에 남음(알파 > 0 기준)**·페이지 넘침 → 다음 페이지, 변종끼리 메타 같음, 마법 16파일 → 방향, `arrows` 프레임 → 방향, 아이콘 겹침, 아레나 바닥 조각 경계·이름 = `ArenaArt.FloorChunks`·기둥 중복 제거, **`IsoMath` 로 놓은 기둥 아래 줄과 구운 바닥 마름모가 같은 줄에서 맞닿음(1px 틈·겹침 없음)**, 성질 대조(표 ≠ SOL → 실패, `sol_known_diffs` 면 통과) | WP0 |
| `Pipeline` | 시험이 만든 작은 MPQ(War3Net 으로 생성)로 전체 실행: 두 번 = manifest 같음, **`--jobs 1` = `--jobs 8`**, 캐시 적중 결과 = 새로 만든 결과, `--force`, **표에 있는데 원본에 없는 항목 → 종료 코드 3·manifest 없음·없는 목록 전부 보고**, 원본에 표 밖 파일이 많아도 성공, `d1/` 밖 파일 안 건드림, 원본 MPQ 파일 바이트·시각 그대로, **다른 현재 폴더에서 실행해도 저장소 뿌리 기준 `--out`**, 저장소 안 무시되지 않는 `--out` 거절. **실제 표 전체 경우**: 실제 `Table/*.json` 이 펼치는 모든 원본 경로에 합성 파일(프레임 수는 정의 JSON 값, TRN 256바이트, TIL 은 쓰는 메가타일 번호보다 크게)을 넣은 MPQ + 실제 `Data/maps`·`tilesets` 로 종료 코드 0, 만든 이름 = 13.5-10 이름 목록. 합성 MPQ 는 War3Net 이 쓸 수 있는 범위에서 원본과 같은 플래그(암호화·파일 이름 키 fix key·PKWARE implode)로 만든다 — 못 쓰는 플래그(파형 압축 등)는 "실제 원본 실행에서만 확인"(WP0 완료 기준) | WP0 |
| `RealMpq`(선택) | 환경 변수 `DDR_DIABDAT` 가 있을 때만 돈다(없으면 건너뜀 표시): 표의 원본 항목이 모두 열리고 머리(CL2 그룹 수·WAV RIFF·SOL 크기)가 맞음, 출력은 임시 폴더. 원본 바이트는 시험 출력에 남기지 않는다 | WP0 |

- 도구 시험의 바이트 픽스처는 **시험 코드의 인코더**(CL2·CEL·MIN·TIL·SOL·WAV 를 쓰는 작은 함수)가 만든다. 원본에서 잘라 낸 바이트를 저장소에 넣지 않는다(R24).

- 부품·시스템 테스트는 `TestWorldBuilder`(WP1 제공, WP1b 갱신: 코드로 만든 작은 타일셋·맵·정의, 전수 공간 질의, 직선 내비)로 만든다. 실제 데이터에 기대는 테스트와 분리한다.
- **불변식 계측 `InvariantChecker`**(Core, WP2): WP2 가 `SpatialRebuild` 안에서 60틱(1초)마다 부른다 — `nan`(유한하지 않은 위치), `out_of_map`(입장 중이 아닌데 바깥 마름모 밖), `in_obstacle`(통과·입장 중이 아닌데 걷기 막힘 도형 부호 거리 < −2px),
  `stuck`(입장·공포·기절·`Hold`·독점 행동이 아니고 주인공까지 거리 > 공격 사거리 + 32 인데 3초 동안 8px 미만 이동), `over_cap`(상한 초과).
  기준: `nan = out_of_map = over_cap = 0`, `in_obstacle` 이 같은 적에서 1초 넘게 이어지지 않음, `stuck` 최댓값 ≤ 살아 있는 적의 2%. 시나리오 시험은 늘 켜고, 게임은 `--check`(`RunOptions.Check`)로 켠다.

### 10.2 실행 캡처로 확인하는 것 (게임 실행, 검증 담당 — 자원이 있어야 한다)

결과는 행마다 **확인 / 문제 / 판정 못 함** 셋 중 하나로 적는다. "빌드 통과" 와 "화면 확인" 을 따로 적는다. 소리는 캡처로 확인할 수 없으므로 `AUDIO` 로그 수로만 보고, 귀로 들은 확인은 "판정 못 함" 으로 둔다.
캡처·로그·견본은 **저장소 밖**(스크래치)에만 둔다 — 원작 그림이 찍힌다(R24). `docs/media` 에도 원작 그림이 든 캡처를 넣지 않는다.

| 확인 | 명령 핵심 | 통과 = |
|---|---|---|
| 사용자 화면·데이터·스피커 보호 | 아래 모든 실행 | 실행 전후 사용자 `save.json` 수정 시각 같음, 포그라운드 창(`GetForegroundWindow`, 검증 쪽 PowerShell 에서 읽기만) 같음, 로그 `AUDIO device="No Output"` 과 `AUDIO muted=true`(`--sound` 행은 `muted=false`) |
| 자원 없음 안내 | `dotnet build -p:DdrAssetDir=<빈 폴더> -p:SkipAssetCheck=true -o <임시>` 로 만든 exe 로 `--autoplay --quit-after 5` | 종료 코드 5, 로그에 없는 파일 목록과 가져오기 명령 |
| 자원 없음 안내 화면 | 같은 exe 로 `--capture-at 1 --quit-after 2`(`--autoplay` 없음) | 종료 코드 5, 캡처에 한글 문장(무엇이 없는지)과 가져오기 명령 한 줄이 보임(네모 없음) |
| 타이틀 | `--autoplay --capture-at 1` | 로고 애니메이션·메뉴·한글이 보임(네모 없음) |
| 그림 규칙 | `--preview enemies`, `--preview sprites` | 걷는 방향이 칸의 방향과 맞음(8방향, 등각 대각선), 발 기준점 십자가 발바닥 ±4px, 그림자가 반투명, 같은 스테이지 적끼리 구별됨(변종 색) |
| 타일 아레나 | `--preview tiles` + `--stage stage_cathedral --capture-at 3` | 벽·아치·기둥이 원작처럼 맞물림, 성질 겹침이 벽 줄·기둥 칸과 맞음, 바닥 조각 경계에 틈 없음, 마름모 밖은 어둠 |
| 벽 가림·정렬 | `--stage stage_cathedral --start-at 00:00 --autoplay --god --capture-at 5,10,15,20` (자동 조작이 기둥 둘레를 지남) | 기둥 뒤 주인공 위 벽이 반투명, 주인공보다 앞이고 빛 반경 안인 벽·앞 턱도 반투명(뒤쪽 벽은 불투명), 기둥 앞 유닛이 기둥 위에 그려짐, 기둥 오른쪽 아래에 붙은 유닛이 벽 뒤로 들어가지 않음 |
| 뒷벽·앞 턱·아치 | `--stage stage_cathedral --start-at 04:00 --autoplay --god --capture-at 10,30,60`(무리가 뒷벽을 타고 돎) + `--debug-nav entries` | **뒷벽에 붙어 선 적의 무기·팔이 다음 벽 칸에 잘리지 않음**, 앞둘레 턱이 그 앞 유닛을 덮음(주인공이면 반투명), 아치 문 아래 유닛은 아치 앞면에 덮이고 문 앞 칸 유닛은 아치 위에 그려짐, 뒤쪽 문으로 들어오는 적이 뒷벽 위에 떠 보이지 않음 |
| 예고·빛 | `--stage stage_cathedral --start-at 07:00 --autoplay --god --capture-at 5,15` | 벽 뒤쪽에 생긴 예고의 테두리가 벽 위로 보임(7층), `unit_min_light` 0.6(두 스테이지 모두)으로 화면 가장자리 적의 몸통·시체·보석이 보이고 바닥은 어두움 |
| 혼합 비교 | `--preview blend` | 무기마다 보통/가산 두 칸이 나란히 보임 — 판정은 기획 담당(열린 질문 10) |
| 길찾기 | `--stage stage_catacombs --start-at 07:00 --stress 180 --debug-nav flow,shapes --god --check --capture-at 5,20` | 앞쪽 협로로 몰리는 흐름, 큰 유닛 우회, 로그의 `stuck`·`in_obstacle` 기준(10.1) 통과 |
| 전투·손맛 | `--start-at 04:00 --level 15 --weapons …` | 투사체(원작 마법 그림·16방향)·폭발·피해 숫자·번쩍임이 보임 |
| 레벨업 창 | `--capture-on levelup` | 카드 3장 문구가 `피해 14 → 18` 꼴, 기획 12.2 배치, 무기 아이콘이 원작 마법 프레임 |
| 보스 | 기획 15절 예 | 패턴 예고·보스 막대·페이즈 |
| 결과 | `--capture-on result --exit-on-result` | 무기별 피해 표, `RESULT` 로그 줄 |
| 소리 고르기 | `--sound --autoplay --god --stage stage_cathedral --start-at 07:00 --quit-after 60 --seed 3` (null 장치 — 스피커로 나지 않음, 동의 필요 없음) | 오류 없이 끝남, `AUDIO device="No Output"`, `AUDIO` 줄의 `played` 가 초당 기준 안(전체 24 동시), 같은 씨앗의 `--mute` 실행과 `requested`·`played`·`dropped_*` 가 같음 |
| 재현 | 같은 명령 두 번(한 번은 `--time-scale 8`). 그리고 같은 명령을 `--mute` 와 null 장치 `--sound` 로 한 번씩 | 두 `--log-hash` CSV 같음, 같은 `--capture-at` PNG 바이트 같음, `--mute` 와 `--sound` 의 `--log-hash` CSV 같음 |
| 실제 스피커 | `--sound-device default …`(**사용자 동의를 받고만**) | 판정 못 함(귀로 들은 확인은 사용자 몫) |
| 단계 끝 | `--autoplay --god --seed 1..5 --stage stage_cathedral --exit-on-result` | 단계 1: 다섯 번 모두 `RESULT outcome=victory` 가 10:00 에(생존 승리). 단계 2: `outcome=victory`, `stage_time ≤ 16:00` |

### 10.3 성능 측정

- `TickProfiler`(Core): 시스템 단계별 `Stopwatch` 누적. `--log-ticks perf.csv` 는 1초마다 한 줄:
  `app_s, stage_s, enemies, hero_proj, enemy_proj, hazards, tick_avg_ms, tick_max_ms, <단계별 평균 ms…>, draw_ms, frame_ms, slow_frames, draw_calls, alloc_bytes, dropped_ticks, <--check 열…>`.
  `draw_ms` = `Draw` 시작부터 `Present` 직전까지 CPU 시간, `slow_frames` = 16.7ms 넘은 프레임 수. 종료 때 첫 5초를 뺀 p50/p95/p99/max 를 로그에 요약한다. `draw_calls` 는 `GraphicsDevice.Metrics.DrawCount`.
- 렌더 측정: `--stage stage_catacombs --stress 300 --freeze-ai --vsync off --window 1280x720 --quit-after 30 --log-ticks perf.csv`. 결과에 "백그라운드 창에서 잼" 을 적는다.
  단계 1 에는 스테이지 2 맵이 없으므로 `--stage stage_cathedral` 로 같은 측정을 한다. `draw_calls` p99 ≤ 600(4.9 표)은 **단계 1 완료 조건**이다(WP5).
- 그래픽 없는 기준 측정과 합격 기준은 4.9 표 하나다.

---

## 11. 구현 작업 묶음 (Work Package)

- 순서: **WP1(끝남, 커밋 `ec851d6`) → WP1b(계약 갱신, 혼자) → WP0a(최소 실제 데이터, 짧게) → WP0·WP2~WP8 이 서로 파일이 겹치지 않고 동시에**. 통합(실제 데이터 엄격 시험, `PlayScene` 완성, 단계 끝 실행)은 WP6 이 맡는다.
- **자원 파일이 없어도 Core 시험은 모두 돈다**(실제 데이터 검사·길 검사·시나리오·성능 포함, 4.2·6.14). 자원이 필요한 것은 게임 실행 확인(캡처)과 자원 대조 검사(13.5-7 실제 쪽)뿐이다.
  다만 실제 맵 길 검사·단계 1 시나리오가 쓰는 **데이터**는 세 묶음에 나뉘어 있고(타일 번호·성질 = 원본 SOL 을 봐야 채움, 맵 배치, 그림 묶음의 프레임 수), 그중 원본으로 확인해야 하는 값이 있다.
  그래서 그 최소분을 **WP0a** 가 먼저 저장소에 넣는다. WP0a 뒤에는 WP0(가져오기 도구 본체)이 늦어도 WP2·WP3·WP4·WP7·WP8 의 Core 쪽은 막히지 않는다. WP5·WP6 의 화면 확인은 WP0 의 단계별 출력을 기다린다.
- WP0 의 맵 굽기는 WP7 로더를 기다리지 않는다 — 도구는 WP1b 의 `MapDataReader` 로 맵 정의를 읽는다(1.4). 13.5-10 의 "정의 → 이름" 쪽만 WP7 로더 뒤(`Integration`)다.
- 각 묶음은 기획 14.1 의 **단계 1(세로 조각)** 범위를 먼저 끝내고, 단계 2·3 내용을 같은 묶음에서 이어 간다. 완료 기준은 단계 1 기준이며, 단계 2·3 은 같은 기준에 내용만 늘린다.
  소리(WP8)는 기획 14.1 대로 단계 2 부터다.
- 맡는 파일은 "맡는 것" 이 전부다. 다른 묶음의 파일을 고쳐야 하면 그 묶음 담당에게 요청한다. 계약(WP1·WP1b 파일) 변경은 11.3.

```
WP1 (끝남) ── WP1b 계약 갱신 ── WP0a 최소 실제 데이터 ──┬── WP0 가져오기 도구·가져오기 표·타일셋 ────────┐   (자원: 게임 화면 확인에 필요)
                                                        ├── WP2 내비·충돌(타일 충돌 포함)·공간 ──────────┤
                                                        ├── WP3 주인공·전투·무기·성장 ───────────────────┤
                                                        ├── WP4 적·감독·자동 조작 ────────────────────────┼── 통합(WP6) ── 검증
                                                        ├── WP5 렌더(아틀라스·타일 아레나)·미리보기 ──────┤
                                                        ├── WP6 화면·UI·입력 ─────────────────────────────┤
                                                        ├── WP7 앱 셸·데이터 로더·개발 도구·자원 검사 ────┤
                                                        └── WP8 소리 ─────────────────────────────────────┘
```

### 11.1 묶음별 맡는 것·계약·완료 기준

#### WP1 — 솔루션 뼈대·공통 계약 (끝남, 커밋 `ec851d6`)

- 한 것: sln·`global.json`·`Directory.Build.props`·세 csproj(DDR0001·복사·xunit.v3), Core `Common/`·`Defs/`·`Data/Registry/`·`Sim/Contracts/`·`Sim/Entities/`·`World`·`Simulation`·`StageStepper`·`GameModules`·`BeginTick`/`EndTick`,
  게임 `App/Contracts/`, 테스트 `TestKit/`·`Common/`, 그리고 각 묶음의 **빈 모듈·연결 파일**:
  `Nav/NavModule.cs`, `Nav/PlacementResolver.cs`, `Nav/ResolvedObstacles.cs`(`INavDebugView`) (→ WP2), `Sim/Weapons/CombatModule.cs`, `Sim/Combat/DamageMath.cs` (→ WP3),
  `Sim/Enemies/EnemyModule.cs`, `Sim/Autoplay/AutoplayPolicy.cs` (→ WP4), `Art/ArtModule.cs`, 게임 `Rendering/RenderServices.cs`·`WorldRenderer.cs`·`FxSystem.cs`·`UiCanvas.cs`·`Scenes/PreviewScene.cs` (→ WP5),
  `Scenes/SceneFactory.cs` (→ WP6), `Data/Loading/LoadingModule.cs` (→ WP7). 만든 뒤 소유는 해당 묶음이다.
- 문서와 다르게 정한 것은 11.4 에 옮겼다(코드가 정본).

#### WP1b — 계약 갱신 (WP0a·WP0·WP2~WP8 앞, 혼자)

v0.3 결정 때문에 바뀌는 **계약 변경** 전부를 한 묶음에 모은다. 실제 `Data/` 는 쓰지 않는다(11.3).

| # | 파일·형 | 바꾸는 것 | 이유 | 영향 |
|---|---|---|---|---|
| 1 | `Defs/SpriteDefs.cs` | `SpriteDef.Source`(`grid`/`atlas`), atlas 는 `Sheet`·`ColorKey`·`Slack` 대신 메타 + `Action`(페이지 안 동작 이름), `Frame` = 칸, `ColorKey` nullable(null = PNG 알파 — grid 에서 속성이 없으면 로더가 검정을 채움, 6.3). `SpriteSetDef.Variants`. `ClipKind`·`ClipSetDef`: `Cast` → `Special`, `CastFire`·`CastLightning`·`CastMagic` 추가 | 원작 시트·TRN 변종·원작 클립 이름(6.3) | WP3·WP4·WP5·WP7 |
| 2 | `Defs/EnemyDefs.cs` | `EnemyDef.Variant`(기본 `""`), `DeathStyle.KnightFade` 삭제, `PhaseDef.Voice`(bool) | 변종, 원작 exe 검사 그림 안 씀, 도살자 2페이즈 대사 | WP4·WP5 |
| 3 | `Defs/EffectDefs.cs` | `EffectDef.Intro`(솟음 구간), `EffectDef.Scatter`, `ProjectileDef.OnEnd`(장판 명세) | 화염벽·산 웅덩이, 산성 침 웅덩이(6.4) | WP3·WP5 |
| 4 | `Defs/MapDefs.cs` | `MapDef` 모양 바꿈(`Tileset`·`Palette`·`Size`(메가타일)·`Layout`·`Entries`(`OutsideTile`·`InsideTile`)/`HeroStart.Tile`/`AnchorsTile` — 형은 `TileCoord`·`Art`·`WallFade`(`Mode` = `Light` 기본/`Hero`, `Radius?`, 6.11)), 새 `TilesetDef`(`Props`·`SolKnownDiffs`·`DesignDiffs`)·`ArenaLayoutDef`. `MapCollisionDef`·`ObstacleKindDef`·`DecorKind`·`ObstacleLightDef` 삭제 | 원작 타일 아레나(6.11), 타일·월드 좌표 섞임 막기(4.1) | WP2·WP5·WP0 |
| 5 | `Defs/StageDefs.cs` | `Obstacles`·`ObstaclePlacementDef`·`LintelDef` 삭제, `LightingDef.KindLights`·`KindLightDef` → `Lights`(점 빛, `AtTile` 또는 기준점 ID), `LightingDef.UnitMinLight` | 코드 장애물 없음(6.12), 유닛 최소 밝기(7.7) | WP4·WP5 |
| 6 | `Defs/WeaponDefs.cs` | `WeaponDef.CastClip`(`ClipKind`, 기본 `CastMagic`) | 무기마다 시전 그림(기획 5.1) | WP3 |
| 7 | 새 `Defs/AudioDefs.cs`, `RulesDefs.cs` | `SoundDef`·`MusicDef`·`SoundBindingsDef`, `RulesDef.Audio`(`AudioRulesDef`). `RanksDef.MaxScale`(기본 1.6), `RulesDef.Enemies`(`EnemyRulesDef.TelegraphRequired` — 6.10 의 다섯 값) | 소리 데이터(6.15). 최종 크기 자르기·큰 피해 기준(기획 v0.5 8.1·12.6, 6.5) | WP4·WP7·WP8 |
| 8 | `Data/Registry/GameDefs.cs` | `Kinds` 에 `tilesets`·`sounds`·`music`·`sound_bindings` 추가, `obstacle_kinds` 삭제, 테이블 추가·삭제 | 위 정의 | WP7 |
| 9 | 새 `Map/IsoMath.cs`·`TileCoord.cs`·`ArenaLayout.cs`·`ArenaGrid.cs`·`ResolvedMap.cs`·`MapDataReader.cs` | 4.1 식, 배치 풀기(6.11 1~5단계), 하위 칸 성질·둘레 표시·`LayoutHash`(타일셋 정의 포함), 타일 → 월드 풀기, `MapDataReader.Read(dataDir) → MapDataResult(Maps, Tilesets, Errors)`와 로더용 `Read(IEnumerable<MapDataElement>)`(두 종류만, 타일셋 ID 연결, `extends` 금지, 중복 키 오류) — **구현과 시험까지** | WP0(그림)과 WP2(충돌)가 같은 풀기, 도구와 WP7 로더가 같은 읽기를 써야 함(1.4·6.2) | WP0·WP2·WP5·WP7 |
| 10 | 새 `Assets/AssetFormat.cs`·`AssetManifest.cs`·`ImportTable.cs`·`SpriteMeta.cs`·`ArenaArt.cs`·`SoundIndex.cs` | 판 번호, manifest 모양(표 해시·파일 크기), **가져오기 표 8개의 모양(1.4 예 전부 — 동작별 `width`·`shadow_alpha` 덮어쓰기, 아이콘 `layers`, 소리 `vars`, `optional`, `tilesets.json`)과 "만들 이름 목록"**, 아틀라스 메타(페이지 여럿·동작별) 읽기·검사, `arena.json` record(`cells` frame 1부터)·바닥 조각 이름 규칙 `FloorChunks`, 소리 색인 — 구현과 시험까지 | 도구(쓰기)와 게임·시험(읽기)의 약속. 표 모양을 WP1b 가 다 정해야 WP0 이 곧바로 형을 바꾸러 오지 않는다(WP1c) | WP0·WP5·WP7·WP8 |
| 11 | `Common/FacingMath.cs` | `IsoYScale = 2`(열 고르기 전 y 2배), `FromColumn` = `normalize(−sin θ, cos θ / IsoYScale)`(ToColumn 의 역), `ToVector(Compass)` 대각선 = `normalize(±2, ±1)`(4.1), 걷기 재생 배율 `FacingMath.GroundSpeed(vel)`·`AnimClock.WalkRate(vel, baseSpeed)`(4.1) | 원작 등각 방향(4.1), `--preview` 방향 돌리기·데이터 방위 | WP3·WP4·WP5 |
| 12 | `Sim/World.cs` | `Arena`(`ArenaGrid?`), `MapSize` = 외곽 상자(`IsoMath`), **`Resolved`(`ResolvedMap`: 입구·기준점·주인공 시작을 월드 px 로 한 번 풀어 둠)**, 주인공 시작 = `Resolved.HeroStart`, `Lighting.Load(stage.Lighting, Resolved)` | 맵 모양 바뀜, 다른 묶음이 `IsoMath` 를 따로 부르지 않게(4.1) | 모두 |
| 13 | `Sim/Contracts/WorldState.cs` | `LightingState` 종류별 빛 → 점 빛 목록(ID 로 찾기) | 5번 | WP4·WP5 |
| 14 | `Sim/Contracts/Presentation.cs` | `CueEvent`·`CueKind`·`PresentationBuffer.Cues`, `CorpseSpawned` 에 `Enemy`(`EnemyDef`)·`Variant` | 소리 신호(7.9), 시체 변종·죽음 소리 | WP3·WP4·WP5·WP8 |
| 15 | `Nav/ResolvedObstacles.cs` → `Nav/ArenaCollision.cs` | 빈 연결 파일: `ArenaCollision.Build(ArenaGrid) → IReadOnlyList<CollisionShape>`(도형 형 `CollisionShape` 는 WP1b 계약), `INavDebugView.Collision`·`Arena`. 굽기 관련(`Kind`·`Height`·`Lintel`) 삭제 | 4.2 | WP2·WP5 |
| 16 | `Sim/GameModules.cs` → `Modules/GameModules.cs`(namespace `Core.Modules`) | `AudioModule`(Core `Audio/`, 빈 본문 → WP8) 추가. 조립 뿌리라 `Sim/` 밖으로 옮김(`Sim/` 소스는 `Core.Audio` 를 참조하지 않는다, 10.1 `Audio`) | 소리 연결 표 검사기 등록 | WP8 |
| 17 | 게임 `App/Contracts/AppServices.cs` | `IArtServices.Sprite(SpriteDef, string variant) → SpriteHandle`(텍스처 + `SpriteMeta?` + 동작 — atlas 는 사각형·`ox,oy` 없이는 못 그린다)와 `IArtServices.DrawFrame(SpriteBatch, SpriteDef, variant, dir, frame, pos, scale, color)`(WP6 처럼 WP5 밖에서 그리는 쪽용), 새 `IAudioServices`(7.9) + `AppServices.Audio`, `GameSettings` 에 `ShowDamageNumbers`·`MasterVolume`·`SfxVolume`·`MusicVolume`, `AppOptions.Muted`·`SoundDevice`, `AppOptions.Preview` 값에 `tiles`·`blend`(`obstacles` 삭제) | 7.5·7.9·8.3·9절 | WP5·WP6·WP7·WP8 |
| 18 | 게임 빈 연결 파일 `Audio/AudioServices.cs` | `Create(AppServices) → IAudioServices`(→ WP8). WP8(단계 2)이 들어오기 전까지 던지지 않고 무음 무동작 구현을 돌려준다(단계 1 의 WP7·WP6 이 그대로 부름) | GameApp(WP7)이 이 파일만 부르게 | WP7·WP8 |
| 19 | csproj·sln·`.gitignore` | 게임: DDR0001 확인 파일·문장(1.3), Assets 재귀 복사(`.cache`·`import.log` 제외), 원본 manifest 가 없으면 출력 manifest 지우기, 가져오기 표를 출력 `ImportTable\` 로 복사. 테스트: 가져오기 표 링크(`ImportTable\`). sln: `tools/DiabloAssetImporter`·`tests/DiabloAssetImporter.Tests` 빈 프로젝트(→ WP0). `.gitignore`: `out/`, `logs/`, `*.mpq *.cl2 *.cel *.pal *.trn *.min *.til *.sol *.dun *.amp`(R24, 대소문자 무관 패턴 `*.[mM][pP][qQ]` 꼴 — 원작 이름은 `DIABDAT.MPQ` 처럼 대문자) | 1.3·1.4 | WP0·WP7 |
| 20 | `TestKit/TestDefs.cs`·`TestWorldBuilder.cs` | 작은 합성 타일셋·맵(번호는 가짜, 성질만 — `W` 칸·둘레 칸 포함), `ArenaGrid` 로 월드 만들기, 1~19 에 맞춰 고침 | 묶음이 실제 데이터 없이 시험 | 모두 |
| 21 | 새 `Data/Json/DefJson.cs` | 공용 STJ 옵션(snake_case·주석·끝 쉼표·`Disallow`)과 변환기 `Vector2`·`TileCoord`·`Rgb`·`RectI`. WP7 로더·`MapDataReader`·`ImportTable` 이 같이 씀 | 병합·변환 규칙이 두 벌이 되어 도구와 게임이 다른 배치를 만드는 일 막기(1.4) | WP0·WP7 |

- 기존 시험(`Common/`)은 1~21 에 맞춰 고친다(`FacingMath` 열 기대값, `FromColumn_RoundTrips` 에 등각 보정, `Compass_ToVector_MatchesColumns` 의 대각선 등).
- **완료 기준**: `dotnet build -p:SkipAssetCheck=true` 통과, `dotnet test` 통과·발견된 시험 수 > 0. 새 시험: `IsoMath` 왕복(칸 중심 → 칸 번호)·외곽 상자 크기(N 40 → 2560×1408),
  합성 타일셋으로 `Expand`(테두리·문·`place`·`line`·같은 입력 같은 `LayoutHash`·둘레 표시), `MapDataReader`(10.1 `Map`), `ResolvedMap`, `SpriteMeta` 읽기·검사(사각형이 페이지 밖이면 오류),
  `ImportTable` 표 8개 읽기·이름 목록(묶음 × 변종 페이지), `ArenaArt.FloorChunks`, `FacingMath` 등각 열(화면 (−32, 16) → 8방향 열 1)·16방향 왕복·`ToVector(SW)`.
  이 문서(1.4, 4.1, 6.3, 6.11, 6.15, 7.6, 7.9, 11.2)의 시그니처·JSON 모양이 계약 파일에 빠짐없이 있다.

#### WP0a — 최소 실제 데이터 (WP1b 뒤, WP2~WP8 앞, 짧게)

WP2 의 실제 맵 길 검사와 WP4 의 단계 1 시나리오가 처음부터 실제 데이터로 돌게, 원본을 봐야 정할 수 있는 숫자만 먼저 저장소에 넣는다. 이 PC 에서 한다(원본 읽기만).

- **하는 것**: 시제품(`d1map probe`, `d1conv scan` — 스크래치)으로 diabdat 를 읽어 값을 적는다. 도구 본체(WP0)는 아직 없어도 된다 — 나중에 WP0 도구의 성질 대조·메타 대조가 같은 값을 다시 확인한다.
  - `Data/tilesets/l1.json`: 단계 1 맵이 쓰는 메가타일의 번호(`floor`·`borders`·`gates`·`pieces`)와 `props`·`sol_known_diffs`(6.11). 문 끝 조각은 견본 그림으로 고른다.
  - `Data/maps/cathedral_arena.json`: 배치·입구·기준점·주인공 시작(6.11 예 값, 칸 중심 규칙).
  - `Data/sprites/monsters.json`·`hero.json` 의 **단계 1 그림 묶음**(기획 14.1 의 단계 1 적 5종 + 주인공): 프레임 수·방향·칸은 `scan` 값, 이벤트 프레임·프레임 시간은 원작 동작 값(출처 주석, 1.4).
- **소유**: `tilesets/` 는 WP0a 뒤에도 WP0. `maps/`·`sprites/` 는 WP0a 가 첫 판을 쓰고 **WP5 로 넘긴다**(WP5 가 미리보기로 기준점·입구·`origin` 을 맞춘다, 11.3). WP0a 는 한 담당이 혼자 한다(WP0 담당 권장).
- **완료 기준**: `dotnet test` 통과(`MapDataReader` 로 실제 두 파일을 읽어 오류 0, `Expand` 성공, 13.5-4 배치 검사 통과), 세 파일의 원본 유래 값마다 출처 주석, `git diff` 에 원작 바이트·덤프 없음(성질 표 키 = 타일셋이 이름 붙인 번호, 13.5-12).
  단계 3 의 L2·`catacombs_arena` 는 WP0a 범위가 아니다(그때까지 WP2 는 합성 맵).

#### WP0 — 가져오기 도구 (WP0a 뒤, 다른 묶음과 동시)

- **맡는 것**: `tools/DiabloAssetImporter/**`(`Table/` 포함), `tools/import-all.ps1`, `tests/DiabloAssetImporter.Tests/**`, 테스트 `Assets/ImportNamesTests.cs`(13.5-10 — ① 기본, ② `Integration`),
  `Data/tilesets/*.json`(번호·성질 표 — 원본으로 확인하는 묶음이라), `src/DiabloDefenceRemastered/Assets/README.md`, `README.md` 의 자원 절·제3자 고지. `tools/import-assets.ps1` 은 고치지 않는다.
- **계약**: 1.4 의 명령·종료 코드·출력 폴더·manifest·메타·소리 색인(Core `Assets/` 형), 13.5-10 을 만족하는 표, 타일셋 성질 표 = 원본 SOL(+ 알려진 차이). 맵 정의는 Core `MapDataReader` 로 읽고
  `ArenaLayout.Expand` 로 펼친다(직접 파싱·펼치기를 하지 않는다). 굽기 결과가 바뀌는 변경은 `AssetFormat.Version` 을 올린다(1.4 캐시). 부록 A 의 형식 규칙을 따른다.
- **단계**: 1 = 몬스터 5종 묶음·변종, 주인공 한 벌, 무기 3종 마법·아이콘, L1 타일셋·아레나. 2 = 나머지 스테이지 1 몬스터·보스, 무기 3종, 이펙트 전부, **소리·음악**. 3 = L2 타일셋(번호 조사, 기획 열린 질문 15)·지하묘지 몬스터.
- **완료 기준**: 도구 시험 통과(10.1 `DiabloAssetImporter.Tests` — 합성 바이트·시험이 만든 MPQ, 결정론·캐시·실패 시 manifest 없음).
  이 PC 실제 실행(창 없음): 단계 목록 전부 생성·종료 코드 0, 두 번 돌려 manifest 같음, 두 번째가 캐시로 빠름(두 시간을 1.4 에 적음), 원본 폴더 파일들의 수정 시각 그대로,
  일부러 없는 경로 하나를 표에 넣으면 종료 코드 3·manifest 없음. `contact` 견본(좀비 원색·`bluered`, 파이어볼 16방향, L1 아레나 바닥 + 벽, 마법사 시전)을 스크래치에 만들어 **눈으로 봄** 을 기록.
  실제 원본 실행이 PKWARE implode·암호화·파형 압축 경로를 덮는다(합성 MPQ 시험이 못 덮는 것, 10.1 `Pipeline`) — 두 번 같음이 그 확인이다. 첫 실행·캐시 실행 시간과 묶음별 페이지 수·크기를 1.4 에 적는다.
  `audit` 가 종료 코드 0(추적 파일에 manifest 출력·원본 항목과 같은 해시 없음, 확장자 목록 없음, `Assets/d1/**` 없음 — R24).

#### WP2 — 길찾기·충돌·공간

- **맡는 것**: Core `Geometry/`, `Nav/`(`ArenaCollision`, `Navigator`, `PlacementResolver`, `NavConst`, `Validation/` = 13.5-4(도형 거리 쪽),5,6 검사기, `NavModule.cs`), `Spatial/`, `Sim/Movement/`(+ `Enemy.Nav.cs` 조각, `Invariants/`).
  테스트 `Geometry/ Nav/ Spatial/ Movement/ Invariants/`.
- **계약**: `ArenaCollision.Build(ArenaGrid) → IReadOnlyList<CollisionShape>`(4.2 — `CollisionShape` 는 WP1b 계약 형, `INavDebugView` 로 WP5 가 읽는다. `Geometry/` 다각형은 이것에서 변환), `NavWorld.Build(World) : INavWorld`(외곽 상자 격자·바깥 마름모 경계), `SpatialHash : ISpatialQuery`, `ObstacleIndex`, `Navigation / Locomotion / Collision / SpatialRebuild` 시스템,
  이동 의도 → 희망 속도(4.4 끝), `Enemy.MoveBlockedTime`·`Hero.TouchingCount` 쓰기, `INavDebugView`(충돌 도형·격자·필드·경로·하위 칸 성질), `InvariantChecker`(`SpatialRebuild` 안에서 60틱마다, 10.1).
- **완료 기준**: 테스트 통과(무작위 A* 최적성, 해시 = 전수·넘침, **실제 맵의 길 검사·전수 성질 — 자원 없이**(단계 1: WP0a 가 넣은 `cathedral_arena`), 겹친 점, 둘레 도형이 `phase_through` 를 막음, `ArenaCollision` 의 평행사변형 합집합 = 막힌 칸 합집합(점 표본)). 그래픽 없는 측정: 적 500 을 맵에 흩어 플로우로 주인공에게 모으는 3,600틱이
  Navigation+Locomotion+Collision 평균 ≤ 1.0ms(4.9 표), 불변식 기준 통과. 단계 3 전까지는 스테이지 2 맵 대신 시험용 합성 맵으로.

#### WP3 — 주인공·전투·무기·성장

- **맡는 것**: Core `Sim/Hero/`, `Sim/Combat/`, `Sim/Weapons/`(`IFirePattern`·`ITargeting`·`WeaponStats`·`CombatModule.cs`), `Sim/Projectiles/`, `Sim/Hazards/`, `Sim/Pickups/`, `Sim/Progression/`,
  이 폴더들의 조각 파일(`Enemy.Combat.cs`, `Hero.Combat.cs`, `World.Combat.cs`, `WeaponPatternState.Combat.cs`, `RunStats` 조각 …).
  Data `weapons/`, `passives.json`, `evolutions.json`, `pickups.json`, `drop_tables.json`, `rules/hero.json`. 테스트 `Combat/ Weapons/ Progression/ Hero/`.
- **계약**: 발사 패턴 6종·조준 5종·무기 효과 부품 등록, `DamageMath`, `HeroControl`·`Weapons`·`Projectiles`·`Hazards`·`DamageResolve`·`DeathRewards`·`Pickups` 시스템,
  `ICombatSpawner`·`IProgression`·`IHostileQuery`(MVP: 주인공만) 구현, `RunStats` 채우기, `IRunSetup`(`--god`·`--level`·`--weapons`·`--passives`).
  v0.3: 무기 `cast_clip` 재생, 투사체 `Vel` 에 그림 방향(도는 구체는 접선), **`ProjectileDef.OnEnd` 장판**, 신호 `WeaponFired`·`EnemyHit`·`HeroHurt`·`HeroDied`·`PickupCollected`·`PickupDropped`(7.9).
- **완료 기준**: 테스트 통과. 그래픽 없는 시나리오(`Scenario`): 해골 50 마리 + 매직 미사일 Lv8 로 60초 → 처치·경험치·레벨업 대기가 생김(`TestKit` 공간 질의로 먼저, WP2 가 들어오면 실제 해시로도).
  `on_end` 시험(맞음·사거리 끝·장애물 세 경우 모두 장판 하나), 신호 시험(무기 발동 한 번에 `WeaponFired` 하나, 피격 무적 중 `HeroHurt` 없음).

#### WP4 — 적·감독·자동 조작

- **맡는 것**: Core `Sim/Enemies/`(생애 상태·부품 계약과 구현·보스 패턴·페이즈·등급·접두사·버프 적용·`EnemyFactory`·`EnemyModule.cs`·`Enemy.Behavior.cs` 조각), `Sim/Director/`(+ `World.Director.cs` 조각), `Sim/Autoplay/`.
  Data `enemies/`, `affixes.json`, `buffs.json`, `stages/`, `rules/enemies.json`. 테스트 `Enemies/ Director/ Sim/`(결정성·시나리오·Perf), `Fixtures/variants/`, `Fixtures/perf_scenario.json`.
- **계약**: 부품 등록(일반 13 + 동작 7 + `TriggeredAction`·`PatternAbility`·`PhaseController`), 이벤트 종류 9개(`PartKind.StageEvent`), `Director`·`EnemyThink`·`DeathHooks` 시스템, 승리 처리(3.5),
  `World.ViewRect` 를 쓰는 생성 위치, `AutoplayPolicy`(`Move(World) → Vector2`, `PickCard(offer) → int` — 메뉴 넘김은 WP6), `IRunSetup`(`--start-at`·`--stress`·`--freeze-ai`), `Visual`·`CorpseSpawned` 값 채우기,
  `World.Lighting`·`BossBars`·`Banner` 쓰기.
  v0.3: 적 17종 + 보스 4 데이터(그림 묶음·변종, 6.13), 새 부품 `panic_on_ally_death`, `phase_through.alpha_over`, 신호 `EnemyAttack`·`EnemySpecial`·`EnemyShot`·`BossArrived`·`BossPhase`·`ElitePack`, 단계 1 판 스테이지(6.12).
- 실제 맵·그림 묶음 정의는 WP0a(타일셋·맵·단계 1 그림 묶음)와 WP5 가 낸다. WP4 는 그 파일을 고치지 않고, 단계 1 판 적 정의는 WP0a 의 그림 묶음 ID 를 가리킨다.
  부품 시험은 처음부터 `TestKit` 합성 맵·합성 그림 묶음으로 한다(실제 데이터를 기다리지 않는다).
- **완료 기준**: 테스트 통과(부품·생성기·전수 연기 시험·변형 픽스처·결정성 30초, 오라·함성이 `include_bosses: false` 면 보스에 안 걸림,
  우두머리 썩은 시체 최종 크기 1.6·반경 22(6.5), 실제 적·보스 정의가 13.5-13 예고 기준을 통과 — 기획 12.6 "지금 표의 패턴은 모두 이 기준을 지킨다", 목표 문 행진이 진입점에서 풀림). 단계 1: 적 5종과 `stage_cathedral`(단계 1 판) 시간표가 10분 돈다
  (`Scenario`: 자동 조작 + `--god`, 예외 없음, 불변식 기준, 상한 무한 월드의 생성 수 기준 — 10.1 `Director`). 마름모 모서리에 주인공을 둔 생성 시험에서 입구 대체 비율을 로그로 낸다(R23).

#### WP5 — 렌더·그림·미리보기

- **맡는 것**: Core `Art/`(`PngHeader`, `ArtModule.cs` = 메타·PNG 대조 검사기 등록), 게임 `Loading/`(텍스처·아틀라스·변종·색 키·스테이지별 미리 읽기·커서·글꼴),
  `Rendering/`(가상 화면·카메라·층·y 정렬·`ArenaRenderer`·빛·표시 이펙트(`intro`·`scatter`)·시체·피해 숫자·`DebugNavOverlay`), `Scenes/PreviewScene.cs`, `Rendering/RenderServices.cs`.
  Data `sprites/`, `effects/`, `maps/`(좌표·배치 맞추기 포함 — `maps/`·`sprites/` 의 첫 판은 WP0a 가 쓰고 넘긴다). 테스트 `Art/`.
- **계약**: `WorldRenderer.Draw(World, RenderContext, float alpha)`, `FxSystem.Consume(PresentationBuffer, int stageTick)`, `IArtServices.Sprite`·`DrawFrame`(WP1b #17), `UiCanvas`(가상 좌표 → 창 좌표, 글꼴 배율),
  4층·0b·5층 벽 무리 나누기와 정렬 키(7.4), 예고 테두리 두 번 그리기(7.3), 유닛 최소 밝기(7.7), `wall_fade`(7.6),
  `RenderContext.ScreenToWorld` 구현 — WP6 이 HUD·마우스 이동에 쓴다. 기획 열린 질문 7 의 기준점·입구 칸·크기 배율, 10(혼합), 11(그림자 값 — 표는 WP0 에 요청), 18(늘린 그림)을
  `--preview`·`--debug-nav` 로 맞춰 데이터에 반영.
- **완료 기준**: `Art` 테스트 통과. `--preview sprites/enemies/effects/tiles/blend` 캡처를 10.2 의 "통과 =" 문장으로 검증 담당이 판정(못 한 것은 "판정 못 함").
  `--stress 300 --freeze-ai` 장면에서 4.9 표의 렌더 기준 — **렌더 CPU p99 와 `draw_calls` ≤ 600 둘 다 단계 1 완료 조건**(단계 1 은 `stage_cathedral`, 10.3). 스테이지 시작 로그의 `vram_mb` 를 R7 에 적는다.
  10.2 의 "뒷벽·앞 턱·아치"·"예고·빛" 행.

#### WP6 — 화면·UI·입력, 통합

- **맡는 것**: 게임 `Scenes/`(Preview 제외, `SceneFactory.cs`·`AutoplayDriver` 포함), `UI/`, `Input/`. 통합 때 `PlayScene` 완성과 `Integration` 시험을 기본 분류로 옮기는 일(시험 파일의 분류 표시만 고침 — 내용은 WP7 과 합의).
- **계약**: `KeyboardMouseSource : IInputSource`(WASD·방향키·왼쪽 버튼 이동, 기획 3절 키), 씬 9개, HUD(미니맵은 가져온 그림), 레벨업·상자·일시정지·설정(**음량 셋**)·결과, 저장 연동(WP7 `SaveStore`),
  자동 조작 메뉴 넘김(`AutoStep(IAutoplayDriver)`), `SCENE`·`RESULT` 로그 문장, 레벨업 오버레이를 같은 앱 틱에 올림(3.3).
  v0.3: 씬별 음악·UI 소리(`IAudioServices`), 일시정지에서 `SetPaused`, **`PlayScene` 프레임 끝 소비 순서 `FxSystem.Consume → Audio.Consume → Presentation.Clear`**, 자원 없음 안내 화면(DataError 화면 재사용, 1.4 끝).
- **완료 기준**: `--autoplay --seed 1 --quit-after 30` 이 타이틀 → 선택 → 플레이까지 사람 입력 없이 감(`SCENE` 로그 세 줄). 캡처 4장(타이틀, 플레이 HUD, 레벨업 창, 결과)을 10.2 문장으로 판정.
  통합 뒤: 실제 데이터 엄격 시험 통과, 10.2 "단계 끝" 행 통과. 키보드로 한 판(사람)·소리를 귀로 듣기는 "동작 확인 못 함" 으로 따로 적는다.

#### WP7 — 앱 셸·데이터 로더·개발 도구·자원 검사

- **맡는 것**: Core `Data/Loading/`(로더·병합·페이즈 풀기·변환기·공통 검증 규칙 13.5-1,2(등록 여부),3,7(정의끼리),8,9 와 6.14 추가 규칙, `LoadingModule.cs`), `Dev/`(`DevOptions`, `TickProfiler`, `StateHasher`), `Save/`,
  게임 `Program.cs`, `GameApp.cs`(합성 뿌리), `App/`(Contracts 제외: `FrameLoop`, `DevRunner`, `AssetCheck`, 캡처, 종료·감시, `Log`, `WindowControl`, `SaveStore`, 빈 시험 씬 `BlankScene`).
  테스트 `Data/ Dev/ Save/`, `Fixtures/data/`(고장 난 JSON).
- **계약**: `DataLoader.Load(dir, registry, validators, options) → (GameDefs, errors)`(새 종류 `tilesets`·`sounds`·`music`·`sound_bindings`, `PhaseDef.Patterns` 채우기), `--validate-data`, `FrameLoop`(3.2), `DevRunner`(캡처·`--quit-after`·`--timeout`·`--exit-on-result`·종료 코드),
  9절 옵션 파싱(`--mute`·`--sound`·`--sound-device` 포함), 저장 보호 규칙(9절), 로그 첫 줄·`EXIT` 줄, `GraphicsProfile.HiDef`, SDL·`ALSOFT_DRIVERS` 환경 변수와 장치 초기화 실패 시 다시 만들기(7.9),
  **시작 자원 검사**(1.4 끝 ①~⑤, 종료 코드 5, 캡처만 있으면 안내 화면 캡처), `DataLoader` 가 `tilesets`·`maps` 를 `MapDataReader` 로 받음(6.2), GameApp 이 `AudioServices.Create` 를 부름.
- **완료 기준**: 테스트 통과(로더 규칙·뒤섞은 파일 순서·옵션 파싱·페이즈 누적 풀기). 게임 실행: 빈 씬으로 창이 뜨고 `--quit-after 2 --capture x.png --capture-at 1` 이 PNG 를 쓰고 종료 코드 0(`Start-Process -Wait -PassThru`),
  가장 큰 텍스처(4096 폭 아틀라스, 550×3240)의 만들기 성공(`HiDef` 확인), 개발 옵션 실행 전후 사용자 `save.json` 시각 같음, `--background` 실행 전후 포그라운드 창 같음,
  `-p:DdrAssetDir=<빈 폴더> -p:SkipAssetCheck=true -o <임시>` 로 만든 exe 의 자동 실행이 종료 코드 5 와 없는 파일 목록 로그, 같은 exe 의 캡처만 준 실행이 안내 화면 PNG + 종료 코드 5(10.2),
  가져오기 표 한 줄을 바꾼 뒤 빌드한 exe 가 "가져오기 표가 바뀌었다" 로 종료 코드 5. 개발 옵션 실행 로그에 `AUDIO device="No Output"`.
  최소화·숨긴 창에서도 `--capture-at` PNG 가 생기는지 확인해 결과를 R8 에 적는다.

#### WP8 — 소리 (단계 2)

- **맡는 것**: Core `Audio/`(`AudioModule`·연결 표 검사기, `SoundMixer`), 게임 `Audio/`(`AudioServices.cs`, `AudioSystem`, `SoundBank`, `MusicPlayer`), Data `audio/`(sounds·music·bindings), `rules/audio.json`.
  테스트 `Audio/`.
- **계약**: 6.15 데이터 모양과 검사, 7.9 의 신호 → 소리 규칙·고르기·재생·무음·로그, `IAudioServices` 구현.
- **완료 기준**: `Audio` 시험 통과(10.1). 게임 실행(개발 옵션, null 장치): `--sound` 를 준 자동 조작 실행이 오류 없이 끝나고 `AUDIO` 로그 줄의 수가 기준 안(맞음 소리 0.08s 제한 → 초당 ≤ 12.5 등) —
  **기준은 `--mute` 와 `--sound` 어느 쪽이든 같은 수**(같은 씨앗이면 두 실행의 `AUDIO` 수가 같다, 7.9), 로그에 `AUDIO device="No Output"`, 두 실행의 `--log-hash` CSV 같음(10.2 "재현").
  장면 바뀜 프레임에 `slow_frames` 가 늘지 않음(음악 읽기, 7.9). 소리를 귀로 확인하는 것은 사용자 몫(`--sound-device default`, 동의를 받고 — "동작 확인 못 함" 으로 적음).

### 11.2 공용 형의 필드와 소유 (partial 조각)

`World`·개체 클래스는 `partial class` 다. 공통 조각(WP1)은 여러 묶음이 **읽거나 쓰는** 필드만 가지고, 한 묶음만 쓰는 상태는 그 묶음 폴더의 조각 파일에 둔다
→ 묶음이 자기 상태를 늘릴 때 WP1 을 거치지 않는다. 옆 사전(`Dictionary<EntityId, …>`)으로 상태를 붙이지 않는다(결정성·할당 0).
`Enemy` 는 **클래스**(풀 재사용)다. 같은 어셈블리라 조각은 `partial` 로 합쳐진다. 구조체·기록 형도 같은 방법을 쓴다: `WeaponPatternState`(partial struct), `RunStats`(sealed partial class, `ResetCombat`).

| 형 | 필드 | 쓰는 묶음 | 읽는 묶음 | 파일 |
|---|---|---|---|---|
| `Enemy` | `Id`, `Def`, `DefIndex`, `Faction`, `Rank`, `AffixIdx[2]`, `SpawnTick`, `Life`(`Entering`/`Emerging`/`Active`/`Stunned`) | WP4 | 모두 | `Sim/Entities/Enemy.cs` (WP1) |
| | `Pos`, `PrevPos`, `Vel`, `Radius` | WP2(이동), WP4(생성·순간 이동) | 모두 | 〃 |
| | `Hp`, `MaxHp`, `Dead`, `KillCause`, `KilledBy` | WP3(피해), WP4(생성·`Collapse`·`Culled`) | 모두(`KilledBy` 는 WP4 `EnemyKilled.Attacker`) | 〃 |
| | `Armor`, `KnockbackResist`, `Immunities`, `DamageMul` | WP4(생성·버프) | WP3 | 〃 |
| | `Intent`(`MoveIntent`), `MoveSpeed`, `PassThroughObstacles` | WP4 | WP2 | 〃 |
| | `MoveBlockedTime` | WP2 | WP4 | 〃 |
| | `KnockbackVel`, `KnockbackTime`, `HitFlashTime` | WP3 | WP2(적분), WP5 | 〃 |
| | `Facing`, `Anim`(`AnimClock` 상태), `Visual`(`EnemyVisual`) | WP4 | WP5 | 〃 |
| | `Buffs`(`BuffSlots`) | WP3(장판 둔화), WP4(오라·함성) — 공통 도우미로만 | WP4 | 〃 |
| | `Nav`(`NavAgentState`) | WP2 | — | `Sim/Movement/Enemy.Nav.cs` |
| | `NextHitBySlot[4]` | WP3 | — | `Sim/Combat/Enemy.Combat.cs` |
| | `Parts`(`PartSlots`), `Action`(`ActionState`), `PhaseIdx`, `Order`(`MoveOrder`), `Leader`, `Summoner`, `Source`, `EnrageAt`, 공포 재무장 시각 | WP4 | — | `Sim/Enemies/Enemy.Behavior.cs` |
| `Hero` | `Pos`, `PrevPos`, `Vel`, `Radius` | WP2(이동), WP3(시작 위치) | 모두 | `Sim/Entities/Hero.cs` (WP1) |
| | `Facing`, `Anim`, `Hp`, `MaxHp`, `Dead`, `InvulnTime`, `HitFlashTime`, `Intent`, `MoveSpeed` | WP3 | 모두 | 〃 |
| | `KnockbackVel`, `KnockbackTime` | WP3(`DamageResolve`) | WP2(`Locomotion` 적분) | 〃 |
| | `Stats`(`StatBlock`), `Weapons`(`WeaponSlots` 4), `Passives`(`PassiveSlots` 4) | WP3 | WP4(자동 조작), WP6(HUD) | 〃 |
| | `TouchingCount` | WP2 | WP3 | 〃 |
| | 재생·무기 내부 상태 | WP3 | — | `Sim/Hero/Hero.Combat.cs` |
| `Projectile` | `Def`, `Faction`, `Pos`, `PrevPos`, `Vel`(그림 방향), `Radius`, `Tint`, `Scale`, `Age`, `TimeLeft`, `Duration`, `Owner`, `WeaponSlot`, `SpawnTick` | WP3(WP4 는 `ICombatSpawner` 로 생성만) | WP4(자기가 만든 것 세기), WP5(그리기) | `Sim/Entities/Projectile.cs` (WP1) |
| `Hazard` | `Effect`, `Tint`, `EffectScale`, `Faction`, `Pos`, `PrevPos`, `Radius`, `Age`, `TimeLeft`, `Duration`, `Owner`, `SpawnTick` | WP3 | WP4, WP5(그리기·조각), WP8(반복 소리) | `Sim/Entities/Hazard.cs` (WP1) |
| `Telegraph`·`Pickup` | 정의·진영·위치·이전 위치·반경·나이·남은 시간·진행 비율·출처 적 ID(1.1 파일 그대로) | WP3 | WP4, WP5 | `Sim/Entities/*.cs` (WP1) |
| | 맞은 목록·궤적 상태·피해 값·`on_end` 명세 | WP3 | — | `Sim/Projectiles/…` 등 조각 |
| `World` | `Defs`, `Stage`, `Map`, `Arena`(`ArenaGrid`, v0.3), `Resolved`(`ResolvedMap` — 입구·기준점·시작 월드 px, v0.4), `MapSize`(외곽 상자), `ViewSize`, `Rng`, `Tick`, `Dt`, 저장소 5개, `Hero`, `Events`, `Presentation`, `Damage`(요청 큐), `Outcome`, `OutcomeTick`, `AwaitingChoice`, `ViewRect`, `FreezeTicks`, `SlowMoScale`·`SlowMoAppTicks`·`SlowMoStretch`, `Seed`, `Simulation` | WP1(틀), 각 단계 | 모두 | `Sim/World.cs` (WP1) |
| | `Command`(`PlayerCommand`) | `StageStepper`(앱 쪽이 넘김) | WP3 `HeroControl` | 〃 |
| | `Options`(`RunOptions`) | `ModuleContext.ApplyRunSetups` | 모두(`--god` 등) | 〃 |
| | `Nav : INavWorld`, `Spatial : ISpatialQuery` | WP2(모듈이 넣음) | 모두 | 〃 |
| | `Hostiles : IHostileQuery`, `Spawner : ICombatSpawner`, `Progression : IProgression?`, `RunStats` | WP3(모듈이 넣음) | WP4, WP6 | 〃 |
| | `Lighting`(`LightingState`: 주변광·어둠·주인공 빛·점 빛), `BossBars`, `Banner`(`BannerState`) | WP4 | WP5, WP6(`Banner` 는 HUD 문구의 유일한 출처) | 〃 |
| | `Presentation.Cues`(`CueEvent`, 7.9) | WP3, WP4(신호를 내는 단계) | WP8 | `Sim/Contracts/Presentation.cs` (WP1b) |
| | 감독 상태(누적기·다음 무리·정예 판정·이벤트 위치) | WP4 | — | `Sim/Director/World.Director.cs` |
| | 무기·성장 내부 상태 | WP3 | — | `Sim/Weapons/World.Combat.cs` |

### 11.3 묶음 사이 순서·충돌 방지

- WP1b 가 끝나기 전에는 다른 묶음이 코드를 쓰지 않는다. WP1b 는 계약을 이 문서 그대로 적는다. 이 문서에 없는 한 묶음 상태는 그 묶음이 자기 조각에 둔다.
- 계약(WP1·WP1b 파일) 형을 바꿔야 하면 메인 세션이 바꾸고 알린다. 다른 묶음은 계약 파일을 직접 고치지 않는다. 자기 조각 파일에 필드를 더하는 것은 자유다.
  WP2~WP8 진행 중에 생기는 계약 변경은 모아서 다음 갱신 묶음(WP1c)으로 낸다 — 한 번에 하나씩 흩어 고치지 않는다.
- 한 묶음만 구현·사용하는 형은 그 묶음 소유다(`IFirePattern`·`ITargeting`·`WeaponStats` → WP3, 부품 계약·`PartSlot`·`ActionState`·`MoveOrder` → WP4, `Navigator`·`NavAgentState` → WP2,
  `SoundMixer` → WP8). WP1 은 레지스트리 등록 지점만 가진다.
- 부품 매개변수 record 와 그 JSON 이름은 부품을 만드는 묶음이 정하고, 같은 묶음이 그 데이터 파일을 쓴다. WP1·WP1b 는 실제 `Data/` 를 쓰지 않는다.
  예외 하나: `ground_hazard` 부품 매개변수는 Defs 의 `GroundHazardDef`(투사체 `on_end` 와 같은 모양, WP1b)를 그대로 쓰거나 같은 키 이름을 쓴다(WP4) — 같은 장판이 두 모양이 되지 않게.
  그래서 다른 묶음이 정한 매개변수 이름과 데이터가 어긋나 `Disallow` 오류가 한꺼번에 터지는 일이 없다.
- `rules` 는 파일을 나눠 쓴다(6.1): WP3 `rules/hero.json`, WP4 `rules/enemies.json`, WP8 `rules/audio.json`. 데이터 파일 하나를 두 묶음이 고치는 일이 없다.
- **최소 실제 데이터 선행**(v0.4): `tilesets/l1.json`·`maps/cathedral_arena.json`·단계 1 그림 묶음은 WP0a 가 WP2~WP8 시작 전에 낸다(11.1). 그 뒤 소유는 아래 그대로다.
  다른 묶음이 기다리지 않으려고 남의 소유 파일에 임시 맵·그림 정의를 만들지 않는다 — 실제 데이터가 아직 없는 시험은 `TestKit` 합성 정의로 한다.
- 데이터 파일 소유(v0.3): 타일셋 `tilesets/` = WP0(번호·성질은 원본으로 확인), 맵 `maps/` = WP5(배치·입구·기준점은 미리보기로 맞춤), 그림·이펙트 `sprites/`·`effects/` = WP5,
  소리 `audio/`·`rules/audio.json` = WP8(연결 표가 다른 종류의 ID 를 가리키지만 그 파일들은 고치지 않는다), 가져오기 표 `Table/` = WP0.
  WP5 가 새 그림 묶음·변종이 필요하면 WP0 에 표 한 줄을 요청한다. 13.5-10 시험이 어긋남을 잡는다.
- 맵 배치를 바꾸면(WP5) 사용자 PC 의 맵 그림이 낡는다. 시작 검사가 `layout_hash` 로 잡고 가져오기를 다시 돌리라고 알린다(1.4). 검증 담당에게 "다시 가져오기 필요" 를 적어 넘긴다.
- `SimPhase` 순서표(3.5)는 WP1 소유. 단계를 더하려면 요청.
- 개발 옵션은 WP7 이 파싱만 하고, 적용은 `IRunSetup` 을 등록한 묶음이 한다(WP3: 주인공 쪽, WP4: 시각·부하·AI 멈춤). 소리 옵션(`--mute`·`--sound`)은 `AppOptions.Muted` 로 WP8 이 읽는다.

### 11.4 WP1 코드가 정한 시그니처 (v0.2 문서에 없거나 다르던 것)

WP1(커밋 `ec851d6`)이 구현하며 정한 것을 옮긴다. **코드가 정본**이고, 이 절은 묶음 담당이 코드를 열기 전에 모양을 알게 하려는 요약이다.

```csharp
// 모듈 (Sim/Contracts/ModuleContext.cs)
public interface IGameModule { void Register(ModuleContext ctx); }
public sealed class ModuleContext
{
    public PartRegistry Parts { get; }
    public IReadOnlyList<IDataValidator> Validators { get; }   public void AddValidator(IDataValidator v);
    public void AddWorldInstaller(Action<World, SimulationBuilder> installer);   // 월드 서비스 넣기 + 자기 단계 시스템 등록, 순서 = 모듈 순서
    public void AddRunSetup(IRunSetup setup);
    public void Install(World world, SimulationBuilder builder);
    public Simulation CreateSimulation(World world);
    public void ApplyRunSetups(World world, RunOptions options);   // world.Options = options 를 먼저 넣고 등록 순서대로 Apply
}
// GameModules.Create() = [LoadingModule, NavModule, CombatModule, EnemyModule, ArtModule] (+ AudioModule, WP1b) — WP1b 리뷰 뒤 Core `Modules/GameModules.cs`(namespace Core.Modules)

// 적 쪽 생성 인자 (Sim/Contracts/Queries.cs)
public readonly record struct EnemyProjectileSpawn(ProjectileDef Def, Vector2 Pos, Vector2 Dir, int Damage, EntityId Owner, EntityId Target = default, float Speed = 0f);
public readonly record struct HazardSpawn(EffectRef? Effect, Vector2 Pos, float Radius, float Duration, float TickInterval, int TickDamage,
                                          Faction From, EntityId Owner, BuffDef? Buff = null, float BuffDuration = 0f, string? CapGroup = null, int Cap = 0);
public readonly record struct BlastSpec(float Radius, int Damage, EffectRef? Effect = null, float Knockback = 0f, ShakeDef? Shake = null);   // Radius 0 = 폭발 없음
public readonly record struct TelegraphSpawn(EffectRef? Effect, TelegraphShape Shape, Vector2 Pos, Vector2 End, float Size, float Time,
                                             Faction From, EntityId Owner, BlastSpec Blast = default);
public interface ICombatSpawner { EntityId SpawnProjectile(in EnemyProjectileSpawn s); EntityId SpawnHazard(in HazardSpawn s); EntityId SpawnTelegraph(in TelegraphSpawn s); }

// 성장 (Sim/Contracts/Progression.cs)
public interface IProgression
{
    int Level { get; }  int Xp { get; }  int XpToNext { get; }  int PendingLevelUps { get; }  int PendingChests { get; }
    LevelUpOffer Offer(); void Choose(int index); void Reroll(); void Skip(); ChestReward OpenChest();
}
// LevelUpCard(CardKind, Id, Name, IconDef?, FromLevel, ToLevel, Lines), LevelUpOffer(Level, Cards, RerollsLeft), ChestItem/ChestReward
public sealed partial class RunStats { Kills, EliteKills, Gold, XpCollected, DamageTaken, float[4] DamageByWeapon; Reset(); partial void ResetCombat(); }

// 개발 옵션 (Sim/Contracts/RunSetup.cs)
public sealed record RunOptions { God, int? Level, Weapons/Passives(LoadoutEntry(Id, Level) 목록), ClockTime? StartAt, int? StressEnemies, int? StressProjectiles, FreezeAi, Check }
public interface IRunSetup { void Apply(World world, RunOptions options); }
public readonly record struct PlayerCommand(Vector2 Move, CommandButtons Buttons = None);   public interface IInputSource { PlayerCommand Next(World world); }

// 주인공 칸 (Sim/Entities/Hero.cs)
public sealed class StatBlock { float this[StatKey]; Clear(); CopyFrom(StatBlock); }
public partial struct WeaponPatternState { BurstLeft, BurstTimer, Timer, Angle, Counter, Active }   // WP3 이 조각으로 더함, default = 상태 없음
public struct WeaponSlot { WeaponDef? Def; Level; Cooldown; WeaponPatternState Pattern; Evolved; AcquiredTick; MaxedAtTick(-1 = 아직); IsEmpty }
public struct PassiveSlot { PassiveDef? Def; Level; AcquiredTick; IsEmpty }   // WeaponSlots·PassiveSlots = InlineArray(4)

// 표시 이벤트 (Sim/Contracts/Presentation.cs) — 이름이 v0.2 문서와 다르다
FxEvent(Tick, EffectDef Def, Pos, Scale, Tint, Radius = 0, Dir = default)   DamageNumber(Tick, Target, Pos, Value, Crit, ToHero)
CorpseSpawned(Tick, EnemyDef Enemy, SpriteSetDef Sprites, string Variant, Pos, Facing, Tint, Scale, Alpha, GlowColor, GlowAlpha, DeathStyle Death)   ShakeEvent(Tick, Px, Duration)
// 알림 문구는 표시 이벤트가 아니다: World.Banner(BannerState { Text, StartTick, EndTick; Show(text, tick, seconds) }) 하나 — WP4 씀, WP6 읽음

// 게임 계약 (App/Contracts)
IScene.AutoStep(IAutoplayDriver bot)   // IAutoplayDriver { AppTick Now; bool Ready(IScene); int PickCard(LevelUpOffer) } — WP6 AutoplayDriver 가 구현
AppServices.Render (RenderContext?)    // GameApp 이 그래픽 장치 뒤에 넣고, WP6 입력이 ScreenToWorld 에 쓴다
```

- `WeaponDef.Pattern` 에 `[PartType(PartKind.FirePattern)]` — 로더는 등록 여부만, 레벨 키·단조성은 WP3 검사기(6.14 의 13.5-2).
- 프레임 번호는 **정의·부품 매개변수·`AnimClock.Advance(dt, frameTime, frameCount, eventFrame1)` 모두 1부터**다. 0부터인 것은 `AnimClock.Frame()` 의 결과(시트 열 안 칸 번호)뿐이다(6.1).
- `PhaseDef.Patch`(원본 JSON)는 로더가 누적 적용해 `PhaseDef.Patterns`(완성 목록)로 푼다. WP4 는 `Patterns` 만 읽는다(6.5·6.6).
- `World.Tick` 은 단계가 도는 동안 "이번 틱 번호"(첫 틱 0)이고 `EndTick` 끝에서 1 늘어난다. 생긴 개체는 `SpawnTick < Tick` 인 틱부터 행동한다(3.5).
- `InvariantChecker` 는 WP2 가 `SpatialRebuild` 안에서 60틱마다 부른다(10.1).
- `ISpatialQuery` 는 칸 후보를 거리로 다시 걸러 **정확한 집합**을 돌려준다(원: 중심 거리 ≤ r, 캡슐: 선분 거리 ≤ r). `CellCount` 만 칸 단위다(4.8).
- csproj: 자원 폴더 속성 `DdrAssetDir`(기본 `Assets`), `SkipAssetCheck`. `global.json` `latestFeature`, nullable 경고 = 오류, 테스트 패키지 `xunit.v3` 3.1.0(1.3).

---

## 12. 위험과 대응

| # | 위험 | 영향 | 대응 |
|---|---|---|---|
| R1 | 방향 열 규칙(등각 보정)·발 기준점·입구 칸·크기 배율이 추정(기획 열린 질문 7) | 유닛이 엉뚱한 방향·위치로 그려짐, 입구가 벽에 걸림 | `FacingMath` 상수 둘·`origin` 데이터·맵 `entries` 한 곳에 모음. `--preview sprites/enemies/tiles`·`--debug-nav entries,tiles` 캡처로 WP5 가 단계 1 안에 맞춤 |
| R2 | 플로우 필드 무리가 협로·장애물 모서리에서 뭉쳐 멈춤 | 스테이지 2 앞쪽 협로가 막혀 재미·난이도가 틀어짐 | 분리 가중·가속 제한·끼임 감지(4.3), 이중선형 방향. `--debug-nav` 로 협로 캡처를 단계 3 완료 조건에 넣음 |
| R3 | 큰 유닛(반경 26~34)이 좁은 틈에서 비빔 | 보스가 안 옴 | A* 는 자기 반경 clearance, 목표 못 가면 가장 가까운 곳, 검증 13.5-5 를 반경 등급별로 |
| R4 | 결정성 깨짐(사전 순회, 벽시계, 프레임 묶음에 의존한 입력·표시 난수, 데이터 파일 순서, 소리가 시뮬레이션을 건드림) | 재현 안 되는 버그, 캡처 비교 실패 | 3.4 규칙 + `Sim` 결정성(30초 판)·묶음 독립성·파일 순서 뒤섞기 시험을 기본 시험으로, `--log-hash` 로 게임 실행도 비교. 소리 켬/끔 해시 같음 시험, `Sim` 소스가 `Core.Audio` 를 참조하지 않음(10.1) |
| R5 | y 정렬 때문에 그리기 호출 수백 번(벽 기둥까지 섞임) | 렌더 예산 초과 | 처음부터 (그림 묶음 × 변종) 한 페이지에 모든 동작(1.4·7.4), 둘레 벽은 정렬 밖 단계(0b·5층), 발광 두 번째 그리기는 발광 항목만. 10.3 로 측정하고 단계 1 완료 조건(WP5). 그래도 넘치면: 한 스테이지에 나오는 묶음 여러 개를 한 장으로 합치기, 그다음 불투명 유닛·벽을 **알파 테스트(AlphaTestEffect) + 깊이 버퍼**로 텍스처별로 묶고 반투명(그림자·가림·사라짐)만 정렬(그림자가 반투명이라 이득이 작다) |
| R6 | 셰이더 없이 번쩍임·실루엣이 약해 보임 | 손맛 부족 | 가산 덧그리기 2회까지 허용. 그래도 부족하면 mgfxc 로 미리 컴파일한 효과 파일(`.mgfx`)을 저장소에 넣는 길을 따로 검토(빌드에 도구 추가 필요) |
| R7 | 그림 메모리(TRN 변종마다 시트 한 벌)와 시작 때 PNG 해독 시간 | 낮은 사양 PC 에서 실패·느린 시작 | 프레임을 잘라 묶은 아틀라스(약 1/3, 7.5), 스테이지별 미리 읽기·내리기, 로그 `vram_mb`. 느리면 StbImageSharp 작업 스레드 해독 + 주 스레드 `SetData`(7.5). WP5 가 스테이지 1 의 실제 값을 이 칸에 적는다 |
| R8 | SDL 힌트(환경 변수)가 이 MonoGame 의 SDL 판에서 안 먹어 창이 앞으로 옴, 최소화·가린 창에서 `Draw` 가 안 불림 | 검증 실행이 사용자 화면을 뺏음, 캡처가 안 생김 | WP7 이 첫 실행에서 포그라운드 창 비교·최소화 캡처로 확인. 안 되면 숨긴 창(`SDL_WINDOW_HIDDEN`)으로 시작 + 렌더 타깃 캡처로 바꾸고 결과를 이 칸에 적음. 캡처 크기는 `--window` 로 고정이라 창 상태와 무관 |
| R9 | 묶음 병렬 작업 중 계약 변경 | 빌드 깨짐·되돌리기 | WP1b 로 계약을 먼저 한 번에 고정, 진행 중 변경은 메인 세션 경유·모아서(11.3). 한 묶음 상태는 자기 partial 조각에 두므로 계약 변경이 드물다(11.2). 각 묶음은 `TestKit` 으로 다른 묶음 없이 시험 |
| R10 | 기획 수치·정의가 데이터와 어긋남 | 조용히 다른 밸런스 | 데이터가 정본, 기획서 표와 맞추는 테스트는 핵심 몇 개만(경험치 곡선, 매직 미사일 표). 수치 변경은 데이터 + 기획서 함께 |
| R11 | 표시 쪽 시체·이펙트를 시뮬레이션 밖에 둔 결정 | 나중에 "시체로 소환"·"시체 먹기"(E `scavenger`) 같은 규칙이 생기면 옮겨야 함 | 그때 `Corpse` 저장소를 Core 로 옮긴다(표시 이벤트 형은 그대로). 지금 MVP 규칙에는 없음 |
| R12 | 한글 글꼴이 없는 PC | 글자 네모 | 8.2 대체 순서 + 로그 경고 |
| R13 | 자원 없는 클론 | 빌드 실패·실행 안 됨 | DDR0001 문장 + `SkipAssetCheck`, 실행은 안내 화면(명령 한 줄), Core 시험은 자원 없이 전부 통과(길 검사 포함, 4.2) |
| R14 | E 단계 아군(가디언 포탑, 기획 11.3)을 넣을 때 계약 변경 | WP2·WP3·WP4 계약을 함께 고쳐야 함 | 지금 `Faction`·`IHostileQuery` 자리만 둠(5.1). 넣을 때 바뀌는 것: `Ally` 저장소·공통 조각, `IHostileQuery` 가 아군 해시도 봄, 적 투사체 맞음 판정 대상 목록, 포탑 자리 `PlacementResolver`, 무기 패턴 `summon`. 포탑은 움직이지 않고 피해를 받지 않아 v0.2 의 수호 검사보다 작다 |
| R15 | partial 조각에 필드를 아무 데나 둠 | 소유가 흐려져 같은 필드를 두 묶음이 씀 | 11.2 표에 없는 공용 필드는 계약 변경 절차(11.3). 각 묶음 조각에는 그 묶음만 쓰는 필드만 |
| R16 | 몬스터를 늘렸는데 구별이 안 되거나 어느 스테이지에도 안 나옴 | 사용자 요구 8 이 화면에서 안 보임 | 6.14 경고 두 개(같은 묶음·변종·덧입힘, 안 나오는 적) + `--preview enemies` 고정 배치 캡처를 검증 담당이 판정(10.2) |
| R17 | 원작 형식 해석 오류(CL2 그룹·평면 겹침, 프레임 폭 추정, MIN 뒤집기, 삼각형 패딩, 0/1 기반 섞임) | 그림이 깨지거나 밀림 | 시제품이 확인한 규칙을 저장소의 부록 A 로 옮겨 정본으로 둠(조사 문서·시제품은 임시 폴더라 사라질 수 있음), 시험 인코더로 만든 바이트 시험(10.1), 폭은 파일 추정 + 표 대조(1.4), WP0 `contact` 견본을 눈으로 확인 |
| R18 | 사용자 원본 판이 다름(다른 설치본, 파일 이름·크기 차이) | 가져오기 실패 또는 다른 그림 | `diabdat.mpq` 에만 기댐, 없는 파일은 목록과 종료 코드 3, manifest 에 원본 크기·SHA-256 을 적어 문제 보고 때 비교 |
| R19 | 타일 성질 표가 원본 SOL 과 어긋남 | 충돌과 그림이 어긋남(요구 3) | 가져오기가 대조해 실패(1.4), 의도한 차이는 `sol_known_diffs` 에 이유와 함께. `--preview tiles` 겹침 캡처 |
| R20 | 등각 y 정렬 규칙 오류(벽이 유닛을 잘못 가림, 아치 아래 유닛, 길게 이어진 벽) | 화면이 이상해 보임 | 둘레 벽은 정렬 밖 단계(0b·5층), 안쪽 기둥만 y 정렬, 아치 덧그림 칸 안 유닛 키 당김(7.4), `--preview tiles` 와 기둥·뒷벽 둘레를 도는 자동 캡처(10.2). 협로 벽이 틀리면 줄 단위 무리로 뺌 |
| R21 | 소리 장치 없음·8비트 WAV·긴 음악 메모리·MonoGame 이 늘 장치를 엶 | 시작 실패·소리 없음·검증 실행이 스피커를 씀 | 가져오기에서 16비트로 변환(7.9), 개발 옵션 실행은 `ALSOFT_DRIVERS=null`, 장치 초기화 예외는 null 드라이버로 다시 만들기, 못 열면 무음으로 계속 + 로그, 음악은 두 곡까지·읽기는 작업 스레드 |
| R22 | 자원이 낡음(형식 판 올림, 가져오기 표 바뀜, 굽기 코드 바뀜, 가져오기 중간 실패 뒤 섞인 출력) | 낡은 `Assets` 로 조용히 이상하게 그려짐 | 캐시 키 = 도구·Core 어셈블리 해시, 굽기 결과가 바뀌면 `AssetFormat.Version` 올림(WP0 규칙), 시작 검사 ①~⑤(판·표 해시·이름·파일 크기·`layout_hash`(타일셋 포함)) → 안내 화면, 원본 manifest 가 없으면 빌드가 출력 manifest 를 지움(1.3·1.4) |
| R23 | 마름모 맵에서 생성 띠의 큰 부분이 맵 밖 | 주인공이 모서리에 있으면 12번 시도가 자주 실패 → 입구로만 들어와 흐름이 바뀜 | WP4 가 모서리 주인공 시험에서 입구 대체 비율을 잼. 높으면 띠 후보를 "띠 ∩ 맵 경계 상자" 에서만 뽑거나 시도 수를 늘린다(`rules.spawn.tries`) |
| R24 | 저작물·사용 조건(Blizzard 자원, DevilutionX "Sustainable Use License") | 공개 저장소에 올라가면 문제 | ① 자원: `.gitignore`(`Assets/*`, `out/`, `logs/`, 원작 형식 확장자), 도구 출력 경로가 저장소 안 무시되지 않는 곳이면 거절(1.4), 캡처·견본·로그는 저장소 밖에(9절·10.2, `docs/media` 포함), 도구 시험 픽스처는 시험 인코더가 만듦(원본에서 자른 바이트 금지), `audit` 명령(추적 파일 해시를 출력·원본 항목 해시와 대조 + 확장자 목록)을 WP0 완료 기준에. ② DevilutionX: 도구·게임이 `*.tsv`·DUN 을 읽거나 저장소에 넣지 않는다. **원작 게임 동작을 재현하는 사실 수치만**(칸 폭·프레임 간격·타격 프레임·SOL 보정 칸·아치 덧그림 번호표) 우리가 확인해 손으로 적고, 파일·코드·표 구조·주석은 옮기지 않으며, 값마다 출처 주석(`원본 확인`/`DevilutionX 참고`)을 남긴다(1.4). 성질 표 키 ⊆ 쓰는 메가타일 검사가 SOL 덤프를 막는다(6.11). 수치 사실을 옮기는 범위의 최종 판단은 사용자(기획 열린 질문 19). 배포할 일이 생기면 다시 정함 |
| R25 | `Assets` 복사 양(출력 폴더마다) | 빌드가 느림·디스크 | `PreserveNewest`. 느리면 하드 링크 복사를 켬(1.3). 다른 출력 폴더(`-o`) 빌드가 잦으면 잰 뒤 판단 |
| R26 | MPQ 를 여러 스레드가 같이 읽음 | 바이트가 섞여 그림·소리가 깨지거나 실행마다 다름(캐시 키도 깨진 바이트로) | 원본 읽기는 주 스레드에서 차례로 `byte[]`, 병렬은 해독·굽기·압축만(1.4), 시험 `--jobs 1` = `--jobs 8` |
| R27 | 핀드(장애물 통과)가 둘레 벽 칸 안으로 들어감 | 벽에 가려 안 보이고 투사체로 못 맞히는 자리에서 공격 | 둘레 도형 `IsBoundary` 는 통과 유닛도 막음(4.2·4.7) |

### 12.1 기획서와 다르게 해석·보탠 것

기획 v0.5·v0.5.1 이 이 표의 몇 줄을 기획서에 옮겼다(10.1 `H`·충돌 정본, 13.5-4, 10.2 정렬 키, 소리 ID 자리, `--mute`·`--sound`, 낮은 턱, 걷기 재생 배율, 큰 피해 기준, 조명 판독성).
그 줄은 "기획서" 열에 옛 문구를 남긴 기록이고, 지금 두 문서는 같다(v0.4.1, 13.6).

| 자리 | 기획서 | 설계 | 이유 |
|---|---|---|---|
| 15절 `--time-scale` | (v0.2) "화면 한 장마다 고정 틱을 n 번" | 실제 시간 n 배속(60Hz 화면에서는 같은 뜻). 기획 v0.3 에서 문구를 맞춤 | 화면 주사율이 달라도 같은 속도. 결과는 어느 쪽이든 같다(결정성) |
| 12.6 피해 숫자·처치 연출, 8.1 시체 | 규칙처럼 적힘 | 표시 쪽(게임) 처리 | 규칙 영향 없음, 시뮬레이션 단순화. 시간은 스테이지 틱으로 계산해 재현됨 |
| 10.5 조명 두 단계 | 주변광 곱하기 + 어둠 덮고 뚫기 | 빛 지도 한 장 곱하기 | 같은 결과를 한 번에. 빛이 겹쳐도 1 을 넘지 않는 차이 |
| 1절 카메라 | 따라가다 경계에서 멈춤 | 지연 없이 따라감 | 생성 띠와 화면을 정확히 맞추기 위해 |
| 8.1 정예 길찾기 | 플로우 필드 | 플로우 필드 2등급(S/M) | 몸 반경 20~26 정예가 좁은 칸에 끼지 않게 |
| 13.5-5 길 검사 반경 | 14 / 26 / 34 | 14 / 16·26(필드 문턱 `NavConst`) / 34 | 무리 필드 문턱(16)과 검사 반경이 어긋나 "검사 통과·실제로 못 감" 띠가 생기지 않게 |
| 9.4 전투 함성 버프 | "버프 이름 `war_cry`" | `buffs` 정의 `war_cry` 하나를 오버로드·스틸메이스가 같이 씀 | 같은 버프는 큰 값 하나만 남는 규칙으로 "겹치지 않음" 이 저절로 됨 |
| 2.4 무리 크기 | 1~3 | 종류를 먼저 뽑고, 그 종류에 `spawn_group` 이 있으면 그 범위 | 폴른 원·핀드·불꽃 부족 무리 |
| 데이터 | — | `extends`, `*_add`/`*_patch`, `spawn_group`, `base_radius`, `elite_eligible`, `buffs` | 몬스터 변형을 정의 몇 줄로(6.13) |
| 열린 질문 1·3·4 | 설계에서 정할 것 | 960×540 유지 / `%LOCALAPPDATA%` 의 save.json / JSON | 7.1, 8.3, 6.1 |
| 10.1 좌표 기준 `Y0 = 160` | 외곽 높이 32N + 160 | `H = 기둥 높이 − 32 = 128`, 외곽 높이 32N + 128(스테이지 1 1408) | 맨 위 칸 벽 꼭대기가 y = 0. 시제품 `arena40` 과 같음(4.1) |
| 10.1 충돌 정본 | 원작 SOL, 가져오기가 다각형을 `Assets/` 에 씀 | 저장소의 배치 + 타일 성질 표가 정본, 가져오기는 SOL 과 대조만 | 충돌이 자원에 묶이면 길 검사·내비 시험이 자원 없이 못 돈다. 대조 실패 시 가져오기가 멈추므로 결과는 같다(4.2) |
| 13.5-4 자원 없을 때 | 기하 검사 4~6 은 건너뜀 표시 | 늘 돈다 | 위와 같은 이유(6.14) |
| 10.2 벽 정렬 키 | 그 칸 바닥 마름모 아래 꼭짓점 y | 뒷둘레 벽 = 바닥 바로 뒤 단계, 앞둘레 턱 = 유닛 뒤 단계, 안쪽 막힌 칸 = 마름모 중심 y, 걸을 수 있는 칸의 위 조각 = 위 꼭짓점, 아치 덧그림 = 중심 y + 그 칸 안 유닛 키 당김 | 오른쪽 아래 이웃 칸에 붙어 선 유닛이 벽 뒤로 들어가 보이는 것, 뒷벽에 붙은 유닛의 무기가 잘리는 것, 아치 앞 칸 유닛이 덧그림에 덮이는 것을 막음(7.4) |
| 8.1·4.0 방향 | brief 4절 45° 단위 열 | 같은 열 순서, 열을 고르기 전에 화면 y 를 2배(등각 보정) | 원작 그림의 대각선은 화면 (−32, 16) 쪽이다(4.1). 미리보기로 확인, 상수 하나로 끌 수 있음 |
| 12.7·13.1 소리 ID 자리 | 정의(무기·투사체·이펙트·그림 묶음·적·획득물)에 소리 ID | 소리 연결 표 한 곳(`sound_bindings`) + 시뮬레이션은 신호만 | 시뮬레이션이 소리를 모르게, WP8 이 다른 묶음 파일을 고치지 않게(6.15) |
| 12.7·15 `--mute` | 개발 실행에서도 소리를 냄 / `--mute` 를 기본으로 켜는 것을 권함 | 개발 옵션 실행은 기본 무음 + 늘 출력 없는 장치(`ALSOFT_DRIVERS=null`), `--sound` 는 그 장치로 재생 경로만 켬, 실제 스피커는 `--sound-device default`(동의). 보통 실행은 소리 냄, `--mute` 로 끔 | 검증 실행이 사용자 스피커로 소리를 내지 않게(brief 7절 취지). MonoGame 이 장치를 늘 열어 "장치를 열지 않음" 은 지킬 수 없다. 무음에서도 길이 색인으로 고르기 수는 같게 센다(7.9) |
| 10.1·10.2 낮은 턱 | 앞쪽 둘레는 낮은 턱 — 걷기만 막고 투사체는 넘어감(`blocks_projectiles: false`) | 원본 SOL 대로 앞둘레 턱도 `X`(걷기·투사체 둘 다 막힘). `W` 칸은 MVP 맵에 없음. 의도한 차이가 필요하면 `design_diffs` | diabdat 직접 확인: 메가타일 18·19·20·24 = SOL 15(4.2). 문서대로 두면 가져오기 성질 대조가 실패한다 — 기획 v0.5 10.1·10.2 가 같은 사실로 고쳤다(차이 없음) |
| 8.2·9.5 오라·함성 대상 | 대장 오라는 해골 4종과 다른 대장에만, "다른 종에는 걸리지 않음". 보스 본체 언급 없음 | `command_aura`·`war_cry`·`buff_area` 의 `include_bosses` 기본 `false`(정의 `rank` 가 miniboss·boss 면 안 걸림), 해골왕에 `skeleton` 태그를 달지 않음 | 9:00 호위 대장 오라가 10:00 해골왕의 이동·피해를 경고 없이 올리는 일을 막음(6.5). 지휘 오라는 기획 8.2 대로 확정, 함성(`war_cry`·`buff_area`)이 보스 본체에 걸리는지는 기획 열린 질문 21 |
| 4.1·8.1 걷기 재생 배율 | 실제 속도 / 기준 속도 | 땅 기준 속도(`vx`, `2·vy` 벡터의 길이) / 기준 속도 | 원작 걷기 그림은 땅 위 걸음 기준이라 화면 세로 이동에서 발이 미끄러짐(4.1). `--preview` 로 확인 |
| 12.6 큰 피해 | (v0.4) "예고 없이 큰 피해 없음"(기준값 없음). v0.5: 범위 즉발·돌진 + 기본 피해 ≥ 10 → 예고 ≥ 0.4s, 근접 단일(사거리 ≤ 몸 반경 합 + 50)은 면제·피해 ≥ 25 면 타격까지 ≥ 0.35s, 투사체·장판 틱 면제 | 기준값을 `rules.enemies.telegraph_required` 한 곳에 두고 **부품 종류**로 범위·돌진·근접·투사체·장판을 가른다(6.5). 오류. 단일 대상인데 사거리가 기준을 넘는 근접은 경고 | 기획 기준을 그대로 담음. 두 갈래에 들지 않는 경우만 설계가 경고로 보탬 |
| 10.5 조명 판독성 | (v0.4) 빛 밖은 거의 보이지 않음. v0.5: 유닛 밝기 하한 0.6(주인공·적·시체·획득물), 어둠 ≤ 0.7, 빛 반경 380~480, 그리는 방법은 설계 | `unit_min_light` = 빛 지도에 대상 모양을 하한값으로 최댓값 혼합, 앞 벽이 겹친 대상은 건너뜀(7.7). 값은 기획서 그대로(6.12) | 값은 기획, 그리는 방법은 설계(기획 10.5) |
| 10.2 가림 ② | 주인공보다 앞이고 "주인공 빛 반경 안" 의 벽 | 벽 칸 마름모 중심과 주인공 발의 거리 ≤ 현재 주인공 빛 반경. `wall_fade.mode = "hero"` 로 ① 만 쓸 수 있음(비교용) | 판정을 칸마다 한 번의 거리 비교로(7.6) |
| 10.3 목표 문 행진 | 목표 문의 진입점에 "닿으면" 보통 행동 | 진입점에서 48px 안 또는 진입점을 지나치면(`MoveOrder.Target`, 6.6) | 20마리 무리는 분리에 밀려 모두 그 점을 밟지 못한다 |
| 19.2 프레임 수 | 도구가 원본 파일에서 읽음 | 도구가 읽고, 정의에도 적어 자원이 있을 때 대조 | 자원 없이 이벤트 프레임 범위를 검사하려면 정의에 수가 있어야 함(6.3) |
| 19.2 PNG 묶는 방식 | 첫 단계는 고정 칸 시트(단순) | 처음부터 프레임을 잘라 묶은 아틀라스 | TRN 변종마다 시트가 한 벌씩이라 고정 칸이면 스테이지 1 만 300MB 를 넘음(7.5). 렌더 쪽 복잡도는 사각형 하나 더 읽는 정도 |
| 10.4 단계 1 구간표 | "1단계용 구간표를 따로 둔다 — 설계" | 단계 1 에는 단계 1 판 스테이지 파일만 두고 단계 2 에서 바꿈(두 판을 같이 두지 않음) | 같은 ID 두 판은 검증·자동 조작 명령을 헷갈리게 함(6.12) |

---

## 13. 리뷰·변경 반영

13.1~13.3 은 v0.2 의 세 리뷰 지적마다 처리를 한 줄로 적은 것이다(높음·중간은 모두 반영, 일부만 반영하거나 보류한 것은 이유). 당시 이름(`bone_brute`, `kind_lights` 등)은 기록으로 그대로 둔다.
13.4 는 v0.3 변경(기획서 v0.4·WP1 구현)의 처리다. 13.5 는 v0.3 리뷰(구현·검증·기획 담당 합본)의 처리다.

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

### 13.4 v0.3 — 기획서 v0.4(디아블로 1 원본 자원)와 WP1 구현 반영

**기획서 v0.4 20절 "설계에 넘기는 것"**

| 넘긴 것 | 처리 | 자리 |
|---|---|---|
| 맵: 월드 1536×1024 와 `maps`(`image`·손 좌표 `collision`)를 등각 아레나로 | 반영 | 4.1(`IsoMath`, 외곽 상자, `H = 128`), 6.11(타일셋·맵·배치 풀기). `CameraMath`·생성 띠·원 충돌은 그대로 |
| 16px 격자를 외곽 상자 전체에, 마름모 밖은 막힘 | 반영 | 4.2(스테이지 1 160×88, 통과 약 5,400) |
| 그리기: y 정렬 층에 벽 기둥, `ObstacleBaker` 를 MVP 에서 안 씀, 벽 가림 반투명 | 반영 | 7.3·7.4(칸 종류별 정렬 키), 7.6(대체) |
| 그림: 12방향 exe 시트 → 8·16방향 원작 시트, 그림자 반투명, PNG 알파, `Facing.ToColumn` 그대로 | 반영 + 보탬 | 6.3(atlas·메타·변종), 7.5. 열 순서는 그대로지만 등각 보정(4.1, `[해석]`) |
| 소리: 새 시스템(재생·제한·우선순위·음악), 8비트 WAV | 반영 | 6.15·7.9, 가져오기에서 16비트로(1.4) |
| 가져오기 도구: 새 프로젝트(War3Net.IO.Mpq), 표 형식, `CheckAssets` 확인 파일 | 반영 | 1.3·1.4, WP0 |
| 검증: 충돌이 Assets 쪽이면 길 검사가 자원 없이 못 돎 | 반영(방향을 바꿈) | 충돌 정본을 저장소 배치 + 성질 표로(4.2, `[해석]`) → 13.5-4~6 이 늘 돈다(6.14) |
| WP1 csproj `DDR0001` 이 `Dungeon1.png` 를 봄 | 반영(계약 변경) | 1.3, WP1b #19 |
| 몬스터 17종·보스 4·새 부품 2(`panic_on_ally_death`, 투사체 `on_end`) | 반영 | 6.4·6.5·6.13, WP3·WP4 |
| 1단계용 구간표를 따로 | 반영 | 6.12(단계 1 판 → 단계 2 에서 바꿈) |
| `--mute` | 반영(기본값 정함) | 9절, 7.9(개발 옵션 실행은 기본 무음, `[해석]`) |
| 기획 13.5-10(표 ↔ 정의 이름)·13.5-11(구별 경고) | 반영 | 6.14 |

**WP1 구현(커밋 `ec851d6`)이 문서와 다르게 정한 것** — 코드가 정본, 문서를 고쳤다.

| 차이 | 처리 | 자리 |
|---|---|---|
| `ISpatialQuery` 결과가 거리로 거른 정확한 집합(`CellCount` 만 칸 단위) | 반영 | 4.8 |
| 빈 연결 파일 추가(`PlacementResolver`·`ResolvedObstacles`/`INavDebugView`·`DamageMath`·`AutoplayPolicy`·`WorldRenderer`·`FxSystem`·`UiCanvas`·`PreviewScene`) | 반영 | 11.1 WP1 |
| 프레임 번호는 정의·부품·`AnimClock` 모두 1부터("코드가 0부터로 바꾼다" 삭제) | 반영 | 6.1, 11.4 |
| `PhaseDef.Patch` 는 로더가 누적 적용해 `PhaseDef.Patterns` 로, WP4 는 `Patterns` 만 | 반영 | 6.2·6.5·6.6 |
| 표시 이벤트 이름 `FxEvent`·`DamageNumber`·`CorpseSpawned`·`ShakeEvent`, 알림 문구는 `World.Banner` 하나 | 반영 | 3.5·5.6·7.2·7.8·11.4 |
| `World.Tick` 은 `EndTick` 끝에서 +1, 생긴 개체는 `SpawnTick < Tick` 부터 행동 | 반영 | 3.5 |
| 11.2 필드 추가(`Enemy.KilledBy`·`DefIndex`, `Hero.KnockbackVel`·`KnockbackTime`, `Projectile.Vel`·`Tint`·`Scale`·`WeaponSlot`, `Hazard.Effect`·`Tint`·`EffectScale`, `World.Command`·`Options`·`OutcomeTick`·슬로모션·`MapSize`·`ViewSize`) | 반영 | 11.2 |
| `IScene.AutoStep(IAutoplayDriver)`, `AppServices.Render` | 반영 | 8.1, 11.4 |
| `WeaponDef.Pattern` 의 `[PartType(FirePattern)]`, 13.5-2 = 등록 여부(Data) + 레벨 키·단조성(WP3) | 반영 | 6.14 |
| `Facing` → `FacingMath` | 반영 | 1.1·4.1·10.1 |
| `ApplyRunSetups` 가 `world.Options` 를 넣음, `InvariantChecker` 는 `SpatialRebuild` 안 60틱마다 | 반영 | 10.1·11.4 |
| csproj 속성 `DdrAssetDir`·`SkipAssetCheck`, `latestFeature`, nullable 경고 = 오류, xunit.v3 3.1.0 | 반영 | 1.3 |
| `WeaponPatternState` partial struct, `RunStats` sealed partial class | 반영 | 11.2·11.4 |
| `IGameModule`·`ModuleContext`, `ICombatSpawner` 인자 구조체, `IProgression` 멤버, `RunOptions`, `WeaponSlot`·`PassiveSlot`·`StatBlock` | 반영(시그니처를 문서로) | 11.4 |

### 13.5 v0.4 — v0.3 리뷰 반영 (`docs/reviews/architecture-v03-review.md`)

세 담당(구현·검증·기획)의 지적을 한 파일로 받았다. 번호는 리뷰 파일의 번호다(구현 I·검증 V·기획 P). 높음·중간은 모두 반영했다. "둘 다" 지적은 이 문서 쪽만 고쳤고, 기획서 쪽은 기획 담당이 고친다.

| 지적 | 처리 | 내용 / 자리 |
|---|---|---|
| 높음 I1 도구가 맵 정의를 읽을 길이 WP7 로더에 묶임 | 반영 | Core `Map/MapDataReader`(WP1b, `tilesets`·`maps` 만, 타일셋 연결, `extends` 금지) + 공용 STJ 옵션·변환기 `Data/Json`(WP1b #21). WP7 로더도 두 종류는 이 결과를 받음. 13.5-10 은 ① 표 → 이름(기본) ② 정의 → 이름(`Integration`)으로 나눔(1.4·6.2·6.14·11절) |
| 높음 I2 가져오기 표 7개·`arena.json` 모양 미정 | 반영 | 표 8개 모양 전부(동작별 `width`·`shadow_alpha` 덮어쓰기, 아이콘 `layers`, 소리 `vars`, `optional`, 새 `Table/tilesets.json` = 타일셋 원본 경로·조각 수·팔레트·덧그림 번호표), Core `ArenaArt`(`cells` frame 1부터), 바닥 조각 이름 규칙 `FloorChunks`(1.4·7.6, WP1b #10) |
| 중간 I3 y 하나로 벽 정렬하면 뒷벽·아치에서 틀림 | 반영 | 벽 세 무리: 뒷둘레 = 0b 층(바닥 바로 뒤), 앞둘레 = 5층(가림 대상), 안쪽만 y 정렬. 아치 덧그림 = 중심 y + 그 칸 안 유닛 키를 C.y − 0.5 로 당김(제안의 "(칸 줄, 종류)" 키 대신 — 유닛끼리의 y 정렬을 깨지 않는 한 키로). 10.2 "뒷벽·앞 턱·아치" 행(7.3·7.4) |
| 중간 I4 앞둘레 턱은 원본에서 `X` | 반영 | 4.2·6.11·10.1 을 원본 사실로. `W` 는 합성 타일셋 시험만, 의도한 차이는 `design_diffs`(지금 없음). 기획 문구는 기획 담당 |
| 중간 I5 `FromColumn`·`ToVector` 가 등각 보정과 안 맞음 | 반영 | `FromColumn` = `normalize(−sin, cos/IsoYScale)`, `ToVector` 대각선 = `normalize(±2, ±1)`, 시험 추가(4.1·10.1, WP1b #11) |
| 중간 I6 타일 좌표가 월드 px 로 조용히 쓰임 | 반영 | `TileCoord` 형·`…Tile` 이름, `World.Resolved`(`ResolvedMap`) 만 읽게 계약(4.1·6.11·11.2, WP1b #4·#9·#12) |
| 중간 I7 `--jobs` 가 MPQ 를 같이 읽음 | 반영 | 원본 읽기는 주 스레드에서 차례로 `byte[]`, 병렬은 해독·굽기·압축만, 시험 `--jobs 1` = `--jobs 8`(1.4·10.1·R26) |
| 중간 I8 "불투명 사각형" 으로 자르면 그림자가 잘림 | 반영 | 알파 > 0 사각형, 시험 "그림자만 있는 줄이 남음"(1.4·10.1) |
| 중간 I9 동작·변종마다 텍스처라 그리기 호출 초과 | 반영 | (그림 묶음 × 변종) 한 페이지에 모든 동작, 메타에 동작별 `rects`·페이지 번호, 넘치면 다음 페이지. 발광 두 번째 그리기는 발광 항목만. `draw_calls` 를 단계 1 완료 조건으로(1.4·6.3·7.4·10.3·WP5·R5) |
| 중간 I10 실제 데이터가 세 묶음에 나뉘어 WP2·WP4 가 기다림 | 반영 | 검증 V1 과 합쳐 WP0a(11절) |
| 낮음 I11 성질 예 값 `"15": "...X"` 가 틀림 | 반영 | 실제 값 `X...`, 1·2·18·19·20·24 추가. 대조 규칙 = bit0·bit2 만(1.4·6.11) |
| 낮음 I12 기둥 그림 1px 어긋날 수 있음 | 반영 | 기둥 왼쪽 위 = `(C.x − 32, C.y + 16 − Hc)` 식, 도구 시험 "맞닿은 줄"(7.6·10.1) |
| 낮음 I13 진입점 20 이 문 틈 중심(19.5)에서 비킴 | 반영 | 칸 중심 = 정수 규칙(4.1·6.1), 예 값 19.5·진입점 35(6.11). 기본값 자동 계산은 두지 않음 — WP5 가 `--debug-nav entries` 로 맞추는 값이라 규칙 하나로 충분 |
| 낮음 I14 동작마다 칸 폭이 다름 | 반영 | 폭은 파일 추정, 표 값은 대조·대체용(다르면 종료 코드 4), 동작별 `width`(1.4) |
| 낮음 I15 War3Net 전이 의존·implode 경로 | 반영 | 1.3 에 전이 의존·라이선스, README 고지(WP0), 합성 MPQ 플래그와 "실제 원본 실행에서만 확인"(10.1·WP0) |
| 낮음 I16 DevilutionX 유래 수치의 근거 | 반영 | 검증 V9·기획 P9 와 합쳐 1.4 "원본에서 온 수치"·R24(사실 수치만, 출처 주석, 구조·코드 복사 안 함). 최종 판단은 기획 열린 질문 19 |
| 낮음 I17 `layout_hash` 가 굽기 입력을 다 안 담음 | 반영 | `LayoutHash` 에 타일셋 정의 전체, 굽기 결과가 바뀌면 `AssetFormat.Version` 올림(WP0 규칙), 캐시 키 = 어셈블리 해시(1.4·6.11) |
| 낮음 I18 `IArtServices.Texture` 로는 atlas 를 못 그림, `ColorKey` 기본값 모호 | 반영 | `Sprite → SpriteHandle`·`DrawFrame`(WP1b #17), `color_key` 금지는 속성 유무로, grid 기본 검정은 로더가 채움(6.3) |
| 낮음 I19 음악 읽기가 프레임을 멈춤 | 반영 | 파일 읽기는 작업 스레드, `SoundEffect` 만들기는 주 스레드, 두 곡까지, 플레이 곡은 `PreloadStage`, `slow_frames` 확인(7.9·WP8) |
| 높음 V1 실제 맵·시나리오 데이터 선행 | 반영 | WP0a(WP1b 뒤 짧게): `tilesets/l1.json`·`maps/cathedral_arena.json`·단계 1 그림 묶음을 시제품으로 읽어 넣음, `maps/`·`sprites/` 는 WP5 로 넘김. 11절 머리 "막히지 않는다" 문장 고침, 남의 파일에 임시 데이터 금지(11.1·11.3) |
| 중간 V2 `--mute` 로 장치를 안 열 수 없음 | 반영 | 개발 옵션 실행은 `ALSOFT_DRIVERS=null`, `--sound` = null 장치로 재생 경로 확인, 실제 스피커는 `--sound-device default`(동의), 로그 `AUDIO device=`, 장치 예외는 null 로 다시 만들기, 10.2 `--mute`/`--sound` 해시 비교(7.9·9절·10.2·WP7·WP8). 이 PC 에서 실제로 되는지는 WP7 첫 실행이 확인(R21) |
| 중간 V3 무음에서 소리 길이를 모름 | 반영 | 도구가 `d1/sound/index.json`(표본 수·표본율·채널), `SoundMixer` 는 길이로 자리 비움, `State` 는 고르기에 안 씀, 시험 "무음 = 소리 켬 수"(1.4·6.15·7.9·10.1) |
| 중간 V4 CI 가 실제 표를 끝까지 안 돌림 | 반영 | Pipeline "실제 표 전체" 경우(합성 원본 파일), 합성 MPQ 플래그, 선택 `RealMpq` 분류(`DDR_DIABDAT`)(10.1) |
| 중간 V5 캐시·낡은 자원 판정이 사람이 올리는 값에 기댐 | 반영 | 도구 판 = 어셈블리 해시, 게임이 표를 출력에 복사해 표 해시 대조, 파일 크기 대조, 원본 manifest 가 없으면 빌드가 출력 manifest 지움(1.3·1.4·R22) |
| 중간 V6 도구가 정의 JSON 을 어떤 길로 읽나 | 반영 | 구현 I1 과 같이 `MapDataReader`(제안의 `DataLoader.LoadKinds` 대신 — WP1b 가 만들어 WP7 을 기다리지 않음). 시험 "도구 경로 `LayoutHash` = 게임 경로"(`Integration`, 10.1) |
| 중간 V7 `DDR_ASSETS` 는 게임이 안 읽음, 안내 화면 확인 길 없음 | 반영 | 자원 폴더 바꾸기 = `-p:DdrAssetDir=<빈 폴더> -p:SkipAssetCheck=true -o <임시>` 빌드 하나, `--autoplay` 없이 캡처만 주면 안내 화면 캡처 + 종료 코드 5, 10.2 행 추가(1.4·10.2·WP7) |
| 중간 V8 저작물이 저장소로 새는 길 | 반영 | 기획 P6 과 합쳐: `.gitignore` 에 `out/`·`logs/`·원작 확장자(WP1b #19), 도구 출력 경로 검사, `audit` 명령, 캡처·견본은 저장소 밖(9절·10.2), 픽스처는 시험 인코더(1.1·10.1·R24). 기획 19.1 문구는 기획 담당 |
| 중간 V9 저장소에 넣는 원본 유래 수치의 범위 | 반영 | 1.4 "원본에서 온 수치": 둘 곳(표·성질 표·그림 묶음 클립)과 출처 주석, 성질 표 키 ⊆ 쓰는 메가타일 검사(6.11·6.14). 프레임 시간·타격 프레임은 게임 정의(그림 묶음)에 둔다 — 게임 규칙(이벤트 프레임)이라. 기획 19.1 이 가져오기 표라고 적은 곳은 기획 담당이 맞춘다 |
| 중간 V10 형식 규칙의 근거가 임시 폴더에만 | 반영 | 부록 A(원작 바이트 없음, 비트 뜻·순서·함정만). 시제품 코드는 참고만 하고 새로 씀(1.4) |
| 낮음 V11 종료 코드 3 문구가 거꾸로 | 반영 | "표에 있는데 원본에 없는 항목", 원본에 표 밖 파일이 많아도 성공 시험(10.1) |
| 낮음 V12 낡은 자원이면 시험이 실패로 보임 | 반영 | 판·표 해시가 다르면 `Assert.Skip("자원이 낡았다 …")`(1.3) |
| 낮음 V13 `--out` 기본이 현재 폴더 상대 | 반영 | sln 을 위로 찾아 뿌리 기준, 못 찾으면 종료 코드 2, 시험 "다른 현재 폴더"(1.4·10.1) |
| 높음 P1 해골왕에 `skeleton` 태그 → 대장 오라 받음 | 반영(설계 쪽) | 보스 예에서 태그 뺌, `include_bosses` 기본 `false`(6.5·12.1). 보스가 버프를 받는지는 기획 4.2·9.5 가 확정(아래 열린 질문) |
| 중간 P2 핀드가 둘레 벽 칸 안으로 | 반영 | 둘레 도형 `IsBoundary`, 장애물 통과 유닛도 막힘(4.2·4.7·10.1·R27) |
| 중간 P3 빛 반경 밖 적이 안 보임 | 반영(설계 쪽) | `unit_min_light`(유닛 몸통 밝기 하한, 빛 지도에 최댓값 혼합) 장치를 둠(6.12·7.7). 어둠·반경·하한 값은 기획 담당 |
| 중간 P4 예고가 벽 밑에 깔림, 협로 벽이 적을 가림 | 반영(설계 쪽) | 예고 테두리를 7층에 한 번 더(7.3), 뒷벽은 0b 층이라 예고가 그 위, 맵 `wall_fade: radius` 선택지(6.11·7.6). 스테이지 2 에 무엇을 쓸지는 기획 10.4·열린 질문 15 |
| 중간 P5 "큰 피해" 기준값 없음 | 반영(설계 쪽) | `rules.enemies.telegraph_required` 로 판정, 기획이 값을 정하기 전까지 경고(6.5·6.14). 값은 기획 12.6 |
| 중간 P6 출력 경로가 저장소 안이어도 막지 않음 | 반영 | 검증 V8 과 같이(1.4 출력 경로 검사, 9절 `--capture`) |
| 낮음 P7 우두머리 썩은 시체가 크기 상한을 넘음 | 반영(설계 쪽) | 경고는 `scale × 가장 큰 등급 배율`, 선택 `rules.ranks.max_scale` 로 자름(6.5·6.14). 상한을 둘지는 기획 9.1·9.2 |
| 낮음 P8 화면 세로 걷기에서 발이 미끄러짐 | 반영 | 재생 배율 = (`vx`, `2·vy` 벡터의 길이) / `base_speed`(4.1·6.3·12.1), `--preview` 로 확인 |
| 낮음 P9 R24 "읽지 않음" 과 6.3 "옮겨 적음" 이 어긋남 | 반영 | 구현 I16·검증 V9 와 같이(R24·1.4·6.3) |

**남은 열린 질문**(기획 담당에게 넘김 — 설계는 장치만 두고 값·방침은 기획서가 정한다). v0.4.1 에서 기획서 v0.5 의 답을 붙였다(처리는 13.6).
1. 보스·중간 보스 본체가 다른 적의 오라·함성 버프를 받는가(설계 기본: 받지 않음, `include_bosses: false`). → **일부 답**: 지휘 오라는 기획 8.2 가 대상 종을 정해 보스에 안 걸림. 함성은 아직 — 기획 열린 질문 21.
2. 판독성 하한: 어둠 값·주인공 빛 반경·`unit_min_light` 값(리뷰 예: 어둠 S1 0.5·S2 0.6·보스 0.7, 하한 0.6), 원거리 적 kite 띠를 빛 반경 안으로. → **답**: 기획 10.3~10.5(하한 0.6, 어둠 0.5/0.6/0.7, 빛 420/380). 미리보기로 고르는 일은 기획 열린 질문 20.
3. "큰 피해" 기준 숫자(`hp_pct`·`reach_over_contact`·`area`) — 정해질 때까지 검사는 경고. → **답**: 기획 12.6·13.5-13(기준이 바뀌어 `telegraph_required` 값을 다시 짰고 검사는 오류).
4. 스테이지 2 협로·앞둘레가 높은 벽일 때 `wall_fade` 를 `hero` 로 둘지 `radius` 로 둘지(열린 질문 15 와 함께). → **답**: 기획 10.2·10.4 — 모든 맵에서 "주인공 빛 반경 안의 앞 벽" 도 반투명(`wall_fade` 기본 `light`). L2 낮은 조각 조사는 기획 열린 질문 15.
5. 등급 배율을 곱한 최종 크기를 자를지(`rules.ranks.max_scale`), 또는 썩은 시체류를 우두머리 크기 배율에서 뺄지. → **답**: 기획 8.1·9.1·9.2 — 최종 크기를 1.6 으로 자름(`max_scale` 1.6, 반경도 자른 크기로).
6. 원본 유래 수치를 저장소에 손으로 적는 범위(1.4 "원본에서 온 수치")를 사용자가 승인하는지(기획 열린 질문 19). → **열림**: 기획 19.1 이 원칙을 같은 뜻으로 적었고 사용자 확인을 기다린다.
7. 기획 10.1·10.2 의 "낮은 턱은 투사체 통과" 문구 — 원본은 `X`. 앞둘레를 투사체가 넘어야 할 기획 이유가 있으면 `design_diffs` 로 적는다. → **답**: 기획 10.1 — 앞 턱은 걷기·투사체 모두 막음, 기획 의도 차이는 MVP 에 없음.

### 13.6 v0.4.1 — 기획서 v0.5 와 맞춤

기획서 v0.5(21절)와 설계서 v0.4 를 다른 담당이 같은 때 고쳐 어긋난 곳을 맞췄다. 값·규칙은 기획서, 구조·시그니처는 이 문서가 정본이다. 기획서 쪽 표현을 고친 곳은 기획 v0.5.1(기획 22절).

| 어긋난 곳 | 기획서 v0.5 | 설계서 v0.4 | 맞춘 것 | 자리 |
|---|---|---|---|---|
| 큰 피해 기준 | 12.6: 범위 즉발·돌진 + 기본 피해 ≥ 10 → 예고 ≥ 0.4s, 근접 단일 면제(사거리 ≤ 반경 합 + 50, 피해 ≥ 25 면 타격까지 ≥ 0.35s), 13.5-13 은 오류 | `telegraph_required = { hp_pct 0.25, reach_over_contact 30, area }`, 경고 | `{ min_damage 10, min_time 0.4, melee_reach_over_contact 50, heavy_melee_damage 25, heavy_melee_windup 0.35 }`, 부품 종류로 판정, 오류 | 6.5·6.10·6.14·12.1·WP1b #7 |
| 최종 크기 | 8.1·9.1·9.2: 정의 × 등급을 1.6 으로 자름, 반경도 자른 크기로. 13.5-14 경고 | `max_scale` 선택(값 없음), 반경은 정의 반경 × 등급 | `rules.ranks.max_scale: 1.6`, 반경 = `round(base_radius × 최종 크기)`(우두머리 썩은 시체 22), 경고 기준을 13.5-14 대로 | 4.3·6.5·6.7·6.10·6.14·WP1b #7 |
| 조명 값 | 10.3~10.5: S1 어둠 0.5·빛 420, S2 0.6·380·11:30 0.7, 하한 0.6 | 예: 어둠 0.6·빛 300, `light_change` 0.8 | 예를 기획 값으로 | 6.12 |
| 밝기 하한 대상 | 10.5: 주인공·적·시체·획득물 | 주인공·적 | 시체·획득물 더함 | 6.12·7.7·10.2 |
| 벽 가림 | 10.2: 앞 벽이 주인공 그림과 겹치거나 **주인공 빛 반경 안**이면 반투명(모든 맵) | `wall_fade` 기본 `hero`, `radius` 는 "그 원 안 유닛을 가리는 벽" 만 | 기본 `light` = 두 규칙, `hero` 는 비교용, `radius` 는 반경 덮어쓰기 | 6.11·7.3·7.6·10.2·12.1·WP1b #4 |
| 앞 문 진입점 | 10.1: `N − 6`(34) — 벽 안쪽 면에서 3.5칸 | 예 값 35(2.5칸) | 34 | 6.11 |
| 목표 문 행진 | 10.3: 목표 문 진입점에 닿으면 직진 시간이 남아도 보통 행동 | 직진 시간까지 | `MoveOrder.Target`, 48px 안 또는 지나치면 풀림 | 6.6·6.12·12.1·WP4 |
| 주인공 맞음 소리 | 4.0·12.7: 맞음 `mage69`·`mage69b`, 죽음 `mage71` | 맞음 목록에 `mage71` | 맞음에서 빼고 죽음 소리로 | 6.15 |
| 핀드·둘레 벽 문구 | 8.3: 안쪽 장애물만 무시, 둘레 벽과 맵 바깥은 막힘 | "맵 경계(둘레 벽)만 막힘" 인용 | 인용을 v0.5 문구로 | 4.2 |
| 오라 대상 | 8.2: 지휘 오라는 해골 4종과 다른 대장만 | 기본값을 "기획이 확정" | 지휘 오라는 확정으로, 함성만 열린 질문 | 6.5·12.1 |

기획서 쪽(v0.5.1)에서 고친 것: 10.2 벽 정렬 문구(v0.3 키 → 설계 7.4 의 세 무리), 10.5 `unit_light_floor` → `unit_min_light`(설계 필드 이름), 15절 `--sound`(null 장치)와 `--sound-device default`,
19.1 저장소에 들어가는 것 표에 아치 덧그림 번호표, 21절 "설계서 다음 판" 이 정할 것 → 설계 v0.4 가 정한 자리, 열린 질문 21(함성 대상) 추가.
13.5 표의 I13 행 "진입점 35" 는 v0.4 기록이라 그대로 두었다.

**남은 열린 질문**
1. 전투 함성(`war_cry`·`buff_area`)이 중간 보스·보스 본체에 걸리는가(기획 열린 질문 21, 설계 기본 `include_bosses: false`).
2. 원본 유래 수치(DevilutionX 를 보고 안 SOL 보정 칸·아치 덧그림 번호표·재생 간격·타격 프레임)를 공개 저장소에 손으로 적어도 되는가 — 사용자 확인(기획 열린 질문 19). 안 되면 그 값은 가져오기 도구가 사용자 PC 에서 원본으로 확인·추정한다.

### 13.7 v0.4.2 — WP1b 구현 리뷰 반영

WP1b 코드 리뷰(검증·설계 담당) 중 문서를 고친 것만 적는다. 코드만 고친 것(중복 키 오류, 해시 문화권, 표 해시 대상, 사전 키 순서, `art` 검사 등)은 코드·시험이 정본이다.

| 지적 | 처리 | 내용 / 자리 |
|---|---|---|
| 중간 `Sim/GameModules.cs` 가 `Core.Audio` 를 참조 → WP8 의 "`Sim/` 소스가 `Core.Audio` 를 참조하지 않음" 시험과 충돌 | 반영(코드 옮김) | Core `Modules/GameModules.cs`(namespace `Core.Modules`)로 옮김. 1.1 트리·WP1b #16·11.4 |
| 중간 `AudioServices.Create` 가 던져 단계 1 의 WP7·WP6 이 부를 수 없음 | 반영 | WP8 전까지 무음 무동작 구현을 돌려줌(WP1b #18) |
| 중간 걷기 재생 배율을 묶음마다 따로 계산 | 반영 | `FacingMath.GroundSpeed`·`AnimClock.WalkRate` 한 곳(4.1·WP1b #11) |
| 낮음 `ArenaCollision.Build` 시그니처·`CollisionShape` 소유 | 반영 | `Build(ArenaGrid) → IReadOnlyList<CollisionShape>`, `CollisionShape` = WP1b 계약 형(4.2·WP1b #15·WP2 계약) |
| 낮음 성질 표 키 검사 기준이 문서("쓰는 메가타일")와 코드("이름 붙인 번호")가 다름 | 반영(코드 쪽으로) | 키 = 타일셋이 이름 붙인 번호, 번호 ≥ 0(6.11·6.14 13.5-4·13.5-12·WP0a) |
| 낮음 `MapDataReader` 시그니처·문법 오류 범위 | 반영 | `MapDataResult`·요소 오버로드, 문법 오류·중복 키 파일은 다른 묶음 파일이어도 오류(1.4·WP1b #9) |
| 낮음 `GameSettings` 음량 기본값이 두 곳 | 반영(코드) | 음량 `float?`(null = `rules.audio.defaults`)(8.3) |
| 낮음 `ground_hazard` 매개변수 모양이 두 벌이 될 수 있음 | 반영 | 11.3 예외: `GroundHazardDef` 를 쓰거나 같은 키 이름 |
| 낮음 11.4 `CorpseSpawned`·1.4 `icons.json` `palette` 가 낡음 | 반영 | 11.4·1.4 |
| 낮음 `.gitignore` 원작 확장자가 소문자뿐 | 반영 | 대소문자 무관 패턴(WP1b #19). `*.wav` 는 넣지 않음 — 자체 소리 계획과 함께 정할 일(R24, 설계 담당) |

---

## 부록 A. 원작 형식 규칙 (조사 문서·시제품에서 옮김)

세션 임시 폴더의 조사 문서(`diablo-inv/inventory-levels.md`·`inventory-sprites-sounds.md`)와 시제품(`d1conv`·`d1map`)이 확인한 규칙을 저장소로 옮긴 것이다(v0.3 리뷰 V10).
원작 바이트는 없다 — 비트 뜻·저장 순서·함정만 적는다. DevilutionX 소스에서 읽은 내용은 사실 요약이고 코드는 옮기지 않았다. WP0 은 이 부록과 1.4 만으로 해석기를 만들 수 있어야 하고,
도구 시험(10.1 `Formats`)의 기대값도 여기서 정한다. 모두 리틀 엔디언.

### A.1 MPQ

- `diabdat.mpq`(이 PC 판 517MB, MPQ 형식 0, `(listfile)` 없음). 경로 구분자는 `\`, 대소문자 무관(War3Net `MpqArchive.Open` + `OpenFile`).
- 파일 목록이 없으므로 이름은 표가 안다(1.4). 저장 단계의 압축(PKWARE implode·파형 압축)과 파일 이름 키 암호화는 War3Net 이 푼다 — 소리 1435개의 RIFF 길이가 실제 길이와 모두 맞았다.
- 같은 이름이 `diabdat.mpq` 와 `hellfire.mpq` 양쪽에 있는 것이 있다 — diabdat 를 먼저 본다.

### A.2 스프라이트 CL2·CEL

- **묶음 머리**: 8방향 묶음은 앞에 그룹 오프셋 8개(`u32`), 그 뒤 그룹마다 `u32 프레임 수, u32 오프셋[프레임 수 + 1]`(그룹 머리 기준). 평면 파일은 바로 `u32 프레임 수, u32 오프셋[n + 1]`.
  **함정**: 그룹 머리 `[0x20, 0x88, …]` 는 "프레임 32개, 첫 프레임 0x88" 인 평면 파일로도 읽힌다. 몬스터·주인공은 8그룹, 마법은 평면으로 **종류별로 정해 둔다**(판별이 필요하면 마지막 프레임 끝 = 파일 길이인 쪽).
- **프레임 머리**(CL2): 10바이트 = `0x000A` + 32줄 단위 건너뛰기 위치 4개. 첫 위치까지가 32줄이라 그 구간 픽셀 수 ÷ 32 = **폭**, 높이 = 전체 픽셀 수 ÷ 폭.
  32줄보다 낮은 프레임은 이 방법이 안 되므로 표 `width` 를 쓴다(1.4).
- **CL2 RLE**: 바이트 v 가 `0x00~0x7F` 면 투명 v칸, `0x80~0xBE` 면 다음 한 바이트 색을 (−v−65)칸 반복(부호 있는 바이트로 보아), `0xBF~0xFF` 면 뒤따르는 (−v)바이트를 그대로.
  **CEL RLE**: `0x80` 이상이면 투명 (−v)칸, `0x01~0x7F` 면 v바이트 그대로. 둘 다 **아래 줄부터 위로**, 칸 수가 줄 끝을 넘으면 다음 줄로 이어진다.
- **투명은 RLE 로만** 정해진다. 색인 0 은 불투명 검정이고 몬스터·주인공의 **그림자**가 이 픽셀이다(1.4 `shadow_alpha`).
- **팔레트**: 스프라이트는 색인 0 과 128~255 만 쓰고, 128~255 는 층 팔레트끼리 같아 `levels\l1data\l1.pal` 하나로 다 풀린다. 팔레트는 768바이트(256 × RGB 8비트, 6비트 아님).
- **TRN**: 256바이트 색인 → 색인 표, **팔레트 전에** 적용. 고유 몬스터 TRN 은 `monsters\monsters\` 아래(해골왕 `genrl`).
- **경로 규칙**: 몬스터 `monsters\{묶음}\{묶음}{동작}.cl2`(n 서기 w 걷기 a 공격 h 맞기 d 죽기 s 특수), 몬스터 소리 `monsters\{소리묶음}{a/h/d/s}{1,2}.wav`,
  마법은 방향이 있으면 `missiles\{이름}{1..N}.cl2`(파일 하나 = 방향 하나, 1번 = 아래, 시계 방향), 없으면 `missiles\{이름}.cl2`. `arrows.cl2` 는 파일 하나에 프레임 번호가 방향.
  주인공 `plrgfx\{직업}\{c}{갑옷}{무기}\{c}{갑옷}{무기}{동작}.cl2`(마법사 `sorceror` 철자 그대로, 동작 as·aw·at·ht·lm·fm·qm·dt, 죽기는 맨손 벌 `{c}{갑옷}n` 에만).
- **표와 실제 파일이 다를 수 있다**: 표에 0프레임인데 파일이 있음(좀비 `s`), 13프레임 중 11만 씀(`firewal2`), 빈 파일(`dmagew.cl2`). 프레임 수는 파일에서 읽고, 쓸 범위만 표로 정한다.

### A.3 레벨 타일 TIL·MIN·레벨 CEL

- **TIL**: 메가타일 하나 = `u16 × 4` = 하위 타일 번호(**0 기반**) 위-왼, 위-오른, 아래-왼, 아래-오른. 메가타일 (x,y) → 하위 타일 (2x,2y),(2x+1,2y),(2x,2y+1),(2x+1,2y+1).
- **MIN**: 하위 타일 하나 = `u16 × 조각 수`(L1·L2·L3 10, 마을·L4 16 — L4 그리기는 12까지). 값 = `종류(비트 12~14) | CEL 프레임 번호(비트 0~11, 1 기반)`, 0 = 빈 칸.
  **파일은 위 단부터 저장**된다 — 바닥 단(맨 아래)은 파일의 마지막 두 칸이다. 짝수 칸 = 왼쪽 32px, 홀수 칸 = 오른쪽 32px, 단마다 32px 위로. 조각 수는 MIN 크기 / SOL 크기로도 확인된다.
- **레벨 CEL**: 머리 = `u32 프레임 수, u32 오프셋[n + 1]`, 프레임 n(1 기반) = `오프셋[n]` 부터. **조각 종류는 CEL 이 아니라 MIN 값 비트 12~14** 에 있다. 모두 아래 행부터 위로.

| 종류 | 이름 | 크기 | 저장 |
|---|---|---|---|
| 0 | 평면 사각형 | 32×32 | 1,024바이트 그대로 |
| 1 | 투명 사각형 | 32×32 | 행마다 RLE: `i8 v`, 양수면 v 개 색, 음수면 −v 개 투명. 행을 넘는 런 없음 |
| 2 | 왼쪽 삼각형 | 32×31 | 31행, 폭 2,4,…,32,30,…,2 를 **오른쪽에 붙여**. 짝수 행(0,2,…) **앞에** 0 두 바이트. 544바이트 |
| 3 | 오른쪽 삼각형 | 32×31 | 같은 폭을 **왼쪽에 붙여**. 짝수 행 **뒤에** 0 두 바이트. 544바이트 |
| 4 | 왼쪽 사다리꼴 | 32×32 | 왼쪽 삼각형의 아래 16행(패딩 포함) + 위 16행 32px 평면. 800바이트 |
| 5 | 오른쪽 사다리꼴 | 32×32 | 오른쪽 삼각형 아래 16행 + 위 16행 평면. 800바이트 |

- 삼각형 두 장(왼·오른)이 바닥 마름모 64×31 을 만든다(맨 위 1행은 비어 있음) → 칸 중심 C 기준 마름모 줄은 `C.y − 15 .. C.y + 15`(7.6).
- 덧그림 CEL(L1S): 폭 64, 아래 행부터, 바이트 `b < 0x80` 이면 b 개 색 복사, 아니면 `256 − b` 개 투명. 프레임 머리말 없이 바로 데이터.
- 투영(원작 기준점): 하위 타일 (x, y) 의 바닥 마름모 왼쪽 아래 = 화면 `((x − y)·32, (x + y)·16)`. 벽 기둥은 기준점에서 위로 `(조각 수 / 2)·32`px(L1 160px).
  이 게임의 월드 좌표는 4.1 `IsoMath` 가 정본이고, 이 식은 해석 확인용이다.

### A.4 SOL

하위 타일마다 1바이트. 비트: 0 Solid(걷기 막힘), 1 BlockLight, 2 BlockMissile(투사체 막힘), 3 Transparent, 4 TransparentLeft, 5 TransparentRight, 7 Trap(128).

- 원작의 "바닥" 판정 = Solid 도 BlockMissile 도 아님. 바닥 칸은 바닥 단을 먼저 그리고, 아니면 벽 단계에서 그린다 → 이 게임의 성질 `.` = 바닥 조각, 그 밖 = 기둥 그림(1.4 아레나).
- 성질 대조는 bit0·bit2 만 본다(1.4). 다른 비트가 섞인 값(159·175·191 등)이 흔하다.
- **원본 SOL 오류**(DevilutionX 참고): L1 아치 조각 일부에 시야·투사체 막힘이 빠져 있다(고치지 않으면 아치 밑으로 투사체가 지나감). 쓰는 메가타일의 칸만 `sol_known_diffs` 에 번호와 이유로 적는다(6.11).
  L3·L4 에도 같은 종류의 오류가 있다 — 그 층을 쓸 때 확인한다.
- 원작에는 SOL 말고도 "대각선 걸음은 두 옆 칸이 비어야 함"(코너 자르기 금지)이 있다 — 이 게임은 격자의 코너 자르기 금지가 대신한다(4.2).

### A.5 그리는 순서와 덧그림

- 원작 순서: ① 화면에 걸친 모든 바닥 칸의 바닥 삼각형 ② 화면 위 행부터(x + y 작은 순), 같은 행은 왼쪽부터, 칸마다 벽 조각(바닥 단 → 위 단) → 그 칸의 물체·유닛 → 아치 덧그림.
  이 게임은 ① 을 바닥 조각, ② 를 0b·4·5층(7.3·7.4)으로 옮긴다.
- L1 아치 덧그림: 하위 타일의 조각 번호가 정해진 목록에 있으면 L1S 의 해당 프레임을 그 칸 위에 덮는다(번호표는 `Table/tilesets.json` 의 `overlay`, 출처: DevilutionX 참고). L2 는 번호표가 다르다(단계 3 에 확인).
- 흙·막힌 칸은 바닥 단이 비고 위 단만 있어 그림이 32px 위로 떠 보인다 — 막힘은 원래 칸에 있는 것이 정상(4.2).
- 원작의 투명 구역(`TransList`)·거리별 빛 표(16단계)·색 순환(L3 용암·L4)·자동 지도(AMP)는 쓰지 않는다(7.6).

### A.6 소리

- 모두 RIFF PCM(형식 1), RIFF 안에 ADPCM 없음. diabdat: 모노 8비트 22050Hz 1004개, 모노 16비트 22050Hz 91개, 모노 8비트 11025Hz 4개, 모노 8비트 44100Hz 1개, 스테레오 16비트 3개(음악 등).
- 변환: 8비트(부호 없음) → 16비트 `(b − 128) << 8`, 표준 44바이트 머리로 다시 씀, 다른 덩어리 버림(1.4). 소리 색인의 표본 수 = 데이터 바이트 ÷ (채널 × 2).
- 음악 `music\dlvl{a..f}.wav`·`dtowne`·`dintro`(이미 16비트 스테레오), `dlvlb` 가 가장 길다(약 15MB PCM).
