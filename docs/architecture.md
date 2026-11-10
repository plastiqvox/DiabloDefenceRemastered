# Diablo Defence Remastered — 설계서 (Architecture)

- 상태: v0.9.1 (설계 담당 — v0.7.2 줄은 WP1e 구현 담당, v0.8.2 줄은 WP1f 구현 담당).
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
  - v0.5: 기획서 v0.6 의 **전장의 안개**(사용자 요구 11, 기획 10.7)를 받았다 — Core 시야 `VisibilityField`(4.11), 성질 표의 시야 막힘 글자(6.11), 원거리·돌진·생성의 시야 조건(6.5·6.6),
    안개 그리기(7.10)·미니맵 기억(8.2), `rules.sight`(6.10), 검증 13.5-15, `--reveal`·`--debug-nav sight`(9절), 계약 갱신 묶음 **WP1c**(11절).
    사용자 결정으로 원본 유래 수치 범위(기획 열린 질문 19)와 함성 대상(21, `include_bosses: false`)을 닫았다. 처리 목록은 13.8.
  - v0.5.1: 안개 설계 리뷰(`docs/reviews/architecture-v05-fog-review.md`, 구현·검증·기획 담당 지적 30개)를 반영했다 — 안개 값을 **알파 채널**로 옮기는 혼합 방식(7.7·7.10),
    두 줄 벽 함께 보임·칸 바뀜 값 잇기(4.11), 몸 높이까지 보는 "또렷이 보임"과 `kite` 띠 당기기(4.11·6.3·6.6 — 기획 v0.6.1 값), 공정성 계측(10.1), 고정 안개 장면 `--preview fog`(9절·10.2),
    보이는 생성 후보 버림 규칙 뺌(6.12), `FogMath` 소유 WP5. 처리 목록은 13.9.
  - v0.5.2: WP1c 구현(시야 계약·`VisibilityField`·`FogMath`)이 문서와 다르게·더 정한 것을 옮겼다. 처리 목록은 13.10.
  - v0.5.3: 기획서 v0.6.2 의 **중간 보스 늘리기**(사용자 요구 "중간 보스 늘리자" — 스테이지 1 은 2명, 스테이지 2 는 3명)를 받았다 — 패턴 조건 `clear_sight`·발사 프레임 시야 재확인(6.5·6.6),
    중간 보스의 `kite`·`split_on_death`, 보스 막대 여러 줄(8.2), 검증 13.5-18(6.14), 계약 보탬 **WP1d**(11.1). 처리 목록은 13.11.
  - v0.6: 기획서 v0.7 의 사용자 결정 셋(brief 요구 12~14)을 받았다 — **H 헬파이어 자원 정식**: 콘텐츠 묶음 `ContentBundle`(MPQ 하나 = 묶음 하나)과 정의의 `requires`(6.17),
    가져오기 표 `bundle`·`prefer`·없는 MPQ 건너뛰기·manifest 의 만든 묶음/건너뛴 묶음(1.4), 시작 검사의 "꺼짐"(1.4 끝), 지하실 L5·둥지 L6 타일셋(6.11). **K 직업 4개**: 직업 정의 `HeroClassDef`(6.16 — `rules.hero` 는 공통 규칙과 고른 직업만),
    무기 `classes`·근접 시전 클립 `attack`(6.8), 직업별 주인공 소리(6.15), 기록 = 스테이지 × 직업(8.3), `--hero`(9절). **M 넓은 방 + 안쪽 방**: 배치 `grid`·`over_border`·`open_center`·방 기준점·타일 표시 `marks`(6.11),
    갇힌 바닥 칸 오류·큰 적 방 길(13.5-5), 자리 고르기의 길 거리 조건(4.10), `--autoplay-visit rooms`(9절), 검증 13.5-19~21(6.14). 계약은 아직 구현 전인 **WP1d 에 합쳤다**(11.1). 처리 목록은 13.12.
  - v0.6.1: v0.6 리뷰(`docs/reviews/architecture-v06-review.md`, 구현·검증·기획 담당 지적 29개)를 반영하고 기획서 v0.7.1 에 맞췄다 — 맵 그림의 묶음 = 타일셋 표 항목의 묶음(`OutputBundles(arenas)`, 1.4), 불완전·깨진 묶음 MPQ 는 그 묶음만 건너뜀(1.4),
    저장 계약에 직업(`RunResult.HeroClassId`·`LastHero`·`BestRecord`, 8.3), `AutoplayPolicy` 인스턴스와 방 들르기 기록(9절), "스테이지에서 나올 수 있는 적" 함수 `StageReach`(6.14), 특성 = 패시브(6.16),
    스테이지 1 예시 배치를 자기 검사에 맞게 고침(모서리 조각·Γ 한 칸 밈·기둥·창살 두 칸, 6.11)과 하위 칸 채우기·1칸 틈·`over_border` 막힘 검사(13.5-19), "원작 방을 본뜬다" 의 뜻과 `audit` 의 DUN 배열 대조(R24),
    WP1d 를 형·구현 둘로 나눔(11.1). 처리 목록은 13.13.
  - v0.6.2: WP1d 구현(형·순수 로직·합성 방 맵)이 문서와 다르게·더 정한 것을 옮겼다. 처리 목록은 13.14.
  - v0.6.3: WP1d 검증 지적을 반영했다 — 13.13 열린 질문 3(기획 v0.7.1 의 방 보급 `room_supply`·방 체류 압박)을 WP1d 커밋 전에 정했다: 맵 `room_areas` 와 방 바닥 `RoomFloors`(6.11),
    `World.Rooms`·`World.RoomPresence`, 획득물 놓기 `ICombatSpawner.SpawnPickup`·`Pickup.SupplyRoom`·틱 이벤트 `SupplyCollected`, `rules.spawn.room_pressure`(6.10·6.12), 13.5-19 방 영역 검사, 빈 `grid` 오류.
    WP1d 에 형과 순수 로직(방 바닥)을 더했다(11.1 #21~#24). 처리 목록은 13.15.
  - v0.6.4: WP0a(최소 실제 데이터 — `tilesets/l1.json`·`maps/cathedral_arena.json`·`rules/sight.json`·단계 1 그림 묶음)가 원본·견본으로 정한 번호와 6.11 예와 다르게 둔 것을 옮겼다. 처리 목록은 13.16.
  - v0.7: 기획서 v0.8 의 **사용자 결정 R "하데스처럼 방 구조로"**(brief 요구 15 — 고른 안 "섞기: 방 = 짧은 생존")을 받았다 — 판 → 액트 → 방 구조와 **방마다 새 `World`**(3.6), 판 흐름 결정(`RunPlanner`·`RunSeeds`)·방 흐름 상태 기계(`RoomFlow`, 새 단계 `SimPhase.RoomFlow`)·판 진행(`RunSession`)·빌드 넘기기(`IHeroCarry`),
    스테이지 = 액트(6.12 — 방 순번 표·방 맵 풀·문 보상·다음 액트, `map`·`unlock`·`victory` 없앰), 문 보상 `rewards`(6.18)·축복 패시브, `rules.rooms`(6.10), 방 맵 규칙(6.11 — `room_kinds`·문 넷·나가는 자리·넓이 배율), 검증 13.5-23(6.14),
    방 구조 그리기(7.11)·화면(8.1·8.2 — 스테이지 선택 없앰)·저장(8.3 — 깊이 기록), `--room`·`--map`(9절), 계약 갱신 묶음 **WP1e**(11.1). 처리 목록은 13.17.
  - v0.7.1: v0.7 리뷰(`docs/reviews/architecture-v07-review.md`, 지적 35개 — 근거는 같은 사용자 결정 R)를 반영했다 — 문 명령의 기본값(`CommandButtons.Exit` 깃발), 판 끝 정산 `RunSession.Conclude`, 들어온 문 바꾸기 = 서로 바꾸기,
    `StageStepper` 판정 순서·나가기 조건(대기·끌려오는 획득물·문 틈 "들어섬"), 보상 물체 `GatesExit`·0장 보상, 정예 방 t 를 `to` 직전에서 멈춤, 보이는 방 번호 `RoomDef.Number`, 방 해시 / 판 해시, 첫 틱 전 조명 다시 적용(`LightingState.Apply`),
    기록 비교 키(깊이 → 승리 → 시간)·깊이 기록 모양·저장 읽기 규칙, 게임 쪽 "월드 바뀜" 계약 `IWorldBound`, 맵 풀 전체의 기준점 검사·문 틈 닿음 검사, 자동 조작이 멈추는 단계 덮기, WP1e 를 형 / 구현-1 / 구현-2 로 나눔(11.1). 처리 목록은 13.18.
  - v0.7.2: WP1e 구현(형·구현-1·구현-2 를 한 번에)이 문서와 다르게·더 정한 것을 옮겼다 — `RoomDef.Number` 0 = 로더가 채움, 뽑기 단계마다 난수 개수 고정, `RoomState.ClearTick`, 정예 상자 자리를 `TryPlace` 규칙으로 직접, `RunStats.ResetCombat` 없앰, 시험 도우미 `FakeHeroCarry.cs`·합성 보상 셋. 기획 v0.8.1 의 넘긴 목록(29절)은 아직 받지 않았다. 처리 목록은 13.19.
    같은 판의 검증 반영: 일반 방 t 도 `to` − 1틱에서 멈춤(기획 v0.8.1 2.5), 죽은 틱에 `Exit` 로 가지 않음(`RoomFlow` 는 나감만 막고 같은 틱 승리는 먼저·`StageStepper`·`Advance`), 정예·보스 방에 주인 없으면 `PlanAct` 예외, WP1f 에서 바뀌는 멤버 목록(11.3).
  - v0.8: 기획서 v0.8.1 29절(v0.8 리뷰 반영에서 설계로 넘긴 것 1~19)과 **사용자 결정 U "AI 확대"**(2026-11-03 — Real-ESRGAN 일반 모델 `realesrgan-x4plus`)를 받았다.
    방 구조 쪽: 문 보상 효과 다시 짬(`RewardKind` 넷 = 마법서·무기 강화 꾸러미·허리띠 물약·성소, 0장·빈 강화 = `단련` — 6.18), 정예·보스 앞 문도 둘 + 덤 보상(3.6 ③), 보상 자리 = 주인공 앞(`RewardSpot`), `cleanup_limit` 25·`remaining_marker`·`reward_anchor`·`chest_ring` 없앰(6.10),
    시간 효과·허리띠·`단련`·성소 대가·칸 수가 짐(`HeroCarry`)으로 이어짐(3.6 결정 1 — 12.1 `[해석]` 닫음), 계단 보상(칸 +1)·**계단 저장**(`RunSnapshot`·`RunSession.Suspend`/이어 하기 생성자, 8.3), `--start-at` 상한 함수·입장 이벤트 바로 내보냄(3.6 결정 5),
    **거울판**(타일셋 짝 표·`MapDef.Mirror`·`MapMirror`·(맵, 방향) 뽑기·문 이름 짝 바꾸기 — 6.11), 층(`RoomDef.Floor`·계단 문 `▼ n층`), `ROOM` 로그 `mirror`·`boss_s`·`picks_left`, 13.5-23 ⑥ 방 이벤트 자리·거울판 검사(6.14).
    AI 확대 쪽: 가져오기 선택 기능 `--upscale realesrgan-x4plus`(기본 끔, 실행 파일·모델은 사용자 PC 에 따로 받고 해시 확인, 실패하면 원본 픽셀 — 1.4), 그림 배율 `scale`(메타·`arena.json`·manifest — 형식 판 3), 월드 렌더 타깃 2배(7.1), 확대한 묶음의 그림자는 AI 없이 도구가 매끈하게 늘려 시트에 굽는다(1.4).
    11.3 의 "WP1f 에서 바뀌는 멤버" ①~⑧ 을 닫고 계약 갱신 묶음 **WP1f**(11.1)로 모았다 — WP0·WP2~WP8 은 WP1f 뒤에 기댄다. 처리 목록은 13.20.
  - v0.8.1: v0.8 리뷰(`docs/reviews/architecture-v08-review.md`, 지적 25개 — 근거는 같은 사용자 결정 R·U)를 반영했다 — 들어온 문 바꾸기를 **시작 문 기준 바꾸기**로(시작 문이 들어온 문일 때만 — 서로 바꾸기는 맞은편 이벤트를 주인공 문으로 옮겼다 — 3.6 결정 5),
    거울판 `entries` 의 ID 도 짝으로(6.11), 칸 그릇 6 에 `RunStats.DamageByWeapon` 을 맞추고 저장 멤버 규칙을 "공개 읽기·쓰기 멤버 + 모양 지문"으로(3.6·11.2), `Suspend` = 계단 문 `Exit` 틱과 같은 길, 이어 하기의 짐 ID 대조·`Carry` null 규칙,
    시작 액트에 따른 열린 칸 수(`RunSlots.OpenAt`), 판 수명 상태는 짐·`RunState` 에만(11.2), AI 확대의 세 단계 처리·묶음 실행과 진행 감시·중간 캐시·변종 공통 알파·맵 그림 알파는 원본 점 확대·Vulkan 런타임 확인·가짜 확대 실행 파일(1.4),
    `output_scale` 첫 판 2 고정, 짝 검사에 둘레·문 자리 짝(13.5-12), WP1f 표 세부(`Hero.Reset`·`DoorsOpened` 자리·`RewardSpot` 두 번째 문턱·`Applies` 둘·직렬화 바이트 비교). 처리 목록은 13.21.
  - v0.8.2: WP1f 구현(형과 순수 로직을 한 번에 — 11.1 WP1f 표 #1~#20)이 문서와 다르게·더 정한 것을 옮겼다 — `RunSlots.OpenAt` 이 `rules.level_up` 을 받음, `RewardSpot.Find`/`Row` 의 내비 인자 판, 이어 하기 대조에 액트 번호를 더함,
    `RunSnapshot` 의 JSON 옵션·`RoomsHash`·모양 지문 상수, `MapMirror.TryOf`/`WithMirror`, `UpscaleTable` 검사 세부·`AssetScale`, 시험 도우미(합성 성소·짝 표 타일셋·거울판·층). 처리 목록은 13.22.
    같은 판의 검증 반영: 이어 하기의 액트 번호를 시작 액트에서 셈(헬파이어 곁가지 액트끼리 이어진 판), 좁은 통로의 보상 줄이 `Find` 의 문턱으로 당김, 모양 지문에 열거형 값 이름(지문만 다시 — `CurrentFormat` 1 그대로),
    `rules.level_up` 키 경로 본문 일곱 곳, WP6 의 `Suspend` 예외 처리, WP0 시작 전 조건(기획서가 13.21 넘긴 것 2~6 을 받음). 13.22 끝 "검증 반영".
  - v0.9: 기획서 v0.9(33절 — 사용자 결정 V1~V5: 회복 문·방 맵 풀·상인 방·장비)와 v0.9.1(34절 — V6~V12: 방어구 무게·무작위 옵션·보상 물체 그림과 둘레 아이콘·물약 칸과 서서히 회복·주인공 겉모습·페핀·원작 체력 구슬)을 받았다.
    새 계약 모양은 **6.19**(장비 정의·인스턴스·굴림 결정론·능력치 합산 뒤 자르기, 상인 정의·상인 방 상태·구매 명령, 물약 칸·`Drink` 명령·서서히 회복, 회복 문 적용 시점, 둘레 아이콘, 겉모습 고르기, 가져오기 표),
    계약 변경과 단계 1 구현·통합이 쌓은 WP1g 요청을 11.1 **WP1g** 한 묶음(22줄)에 모았고 순서·소유는 11.3, 처리 표는 13.23. 레벨업 아이콘이 검던 관찰은 원작 `ctrlpan\spelicon.cel` 로(가져오기 표).
  - v0.9.1: 기획서 v0.9.2(35절 — 열린 질문 50~53 사용자 답)를 받았다 — **방어 상한 10 → 15**(6.19 `armor_cap`·WP1g #5 시험), **지팡이와 방패를 함께**(양손 규칙 없음 — 6.19 끼우기·겉모습 `t`), 무기 아이콘 = `spelicon` 56×56 칸 그대로(코드 `9128cc9` — 1.4·6.8·6.19),
    13장 오타 둘(가져오기 스크립트 경로, WP5 기록 표의 남는 칸). 처리는 13.23 표 끝 줄.
  기준 기획서: `docs/gdd.md` v0.9.2(v0.9.1 — 35절 "설계에 넘기는 것" 16~18 을 13.23 끝 줄이 받음. v0.9 — 33절·34절 "설계에 넘기는 것" 1~15 를 13.23 이 받음. 그 앞: v0.8.4 — 설계에 넘긴 목록은 기획서 28절·29절 끝 — v0.8 에서 모두 받음, 13.20. 기획서 31절 끝 넘긴 것은 v0.8.1 이 받음, 13.21).
  13.21 끝 "기획서에 넘기는 것" 2~6 과 13.22 의 기획 몫은 기획서 v0.8.4 가 받았다(기획서 32절 — 19.6·2.5·11.4·15절, 설계서 문장 그대로. 11.1 WP0 "시작 전").
  두 문서에 걸친 지적은 설계서 쪽만 고치고 기획서 문장은 13.21 끝 "기획서에 넘기는 것" 으로 넘겼다(13.20 끝 목록도 그대로 유효). 출발점: `docs/brief.md`.
- 이 문서는 **코드 구조·형식·알고리즘·작업 나누기**를 정한다. 수치·규칙은 기획서가 정하고, 여기서는 그것을 어디에 어떻게 담는지만 정한다.
  기획서와 다르게 해석한 곳은 그 자리에 `[해석]` 으로 표시하고 12.1 에 모았다.
- 코드 조각은 인터페이스 시그니처 수준이다. 이름은 구현 때 바뀔 수 있지만 **11절 작업 묶음 사이의 계약**(굵게 표시한 형)은 WP1·WP1b 가 정한 뒤 고치려면 관련 묶음 담당과 합의한다.
  공용 형의 필드를 누가 쓰고 누가 읽는지는 11.2 표가 정본이다. 이미 커밋된 코드와 이 문서가 다르면 코드가 정본이고, 문서를 고친다(11.4).
- 사용자 요구 8 "몬스터 종류를 늘릴 것" 은 6.13 에 답한다: 새 적 = JSON 정의 1개(원작 그림 묶음 + 원작 색 변종 TRN, 그 위에 `extends` 로 색·크기·행동 변형),
  모든 적 정의를 자동으로 돌려 보는 연기 시험, MVP 몬스터 17종(+분열체, 기획 8.2).
- 사용자 요구 10 "디아블로 1 원본 자원을 쓸 것" 은 1.4·4.2·6.3·6.11·6.15·7.5·7.6·7.9 에 답한다: 원본(`diabdat.mpq`)에서 뽑은 그림·소리·타일 그림은
  저장소에 넣지 않고 가져오기 도구가 사용자 PC 의 `Assets/d1/` 에 쓴다. 저장소에는 도구·가져오기 표·타일 배치·정의 JSON 만 있고, **자원 파일이 없어도 Core 시험(실제 데이터 검사·길 검사·시나리오)은 모두 돈다.**
  단, 원본으로 확인해야 하는 숫자(타일 번호·성질 표, 단계 1 그림 묶음의 프레임 수)는 WP0a 가 이 PC 에서 먼저 읽어 저장소 데이터로 넣는다(11절).
- 사용자 요구 11 "전장의 안개(원작식 시야)" 는 4.11·6.11·6.5·7.10·8.2 에 답한다: 시야는 **시뮬레이션 상태**(Core `VisibilityField`, 하위 타일 칸 단위, 성질 표의 시야 막힘 칸)라
  원거리 적의 사격·돌진이 같은 씨앗이면 같고, 그리기(`--reveal` 포함)는 그것을 읽기만 한다.
- 사용자 결정 H "헬파이어 자원도 쓴다"(요구 12)는 1.4·6.11·6.17 에 답한다: 묶음은 `hellfire`·`hfmonk`·`hfmusic`·`hfvoice` 넷이고, MPQ 가 없는 PC 는 가져오기가 그 묶음을 건너뛰고(실패 아님)
  그 묶음을 `requires` 로 단 스테이지·직업만 꺼진다. 기본 내용(대성당·지하묘지·마법사)은 헬파이어 파일을 하나도 쓰지 않으므로(13.5-20) 같은 씨앗·스테이지·직업이면 묶음 유무와 상관없이 결과가 같다.
- 사용자 결정 K "직업 4개"(요구 13)는 6.16 에 답한다: 직업 = 정의 파일 하나(`Data/heroes/<id>.json`, 그림 묶음·시작 무기·능력치 덮어쓰기·특성·`requires`). 주인공을 만드는 코드는 `World.HeroClass` 만 읽는다.
  MVP 데이터는 `sorcerer` 하나지만 계약(형·소리 연결 키·저장 모양)은 처음부터 직업 단위라 직업을 더할 때 계약을 다시 고치지 않는다.
- 사용자 결정 M "넓은 방 + 안쪽 방"(요구 14)은 4.10·6.11·6.14 에 답한다: 원작 작은 방·짧은 벽은 배치 데이터(`grid` 놓기, 둘레에 겹쳐 붙이기)로 적고, 갇힌 바닥 칸은 길 검사 오류, 큰 적은 모든 방 기준점까지 길이 있어야 한다.
- 사용자 결정 R "하데스처럼 방 구조로"(요구 15, v0.7)는 3.6·6.12·6.18·6.11·8·11.1 WP1e 에 답한다: 한 판 = 액트(지금의 스테이지 정의) 여럿, 액트 = 방 순번 표대로 방 여럿, **방 하나 = `World` 하나**(맵·적·안개·투사체는 방마다 새로, 주인공 빌드·누적 기록은 `RunState` 로 넘김).
  방 순서는 데이터로 고정이고 씨앗은 방 맵·들어오는 문·출구 문 수·문 보상 후보만 고른다(`RunPlanner`, `(씨앗, 액트, 방 번호)` 마다 따로 — 맵만 바로 앞 방 맵에 이어짐). 이미 있는 것(타일·맵 형식, 안개, 가져오기, 무기·레벨업, 몬스터, 직업, 헬파이어 묶음, 공정성, 결정성)은 그대로 두고, 이미 커밋된 `cathedral_arena` 는 액트 1 보스 방이자 단계 1 의 모든 방이다.
- 사용자 결정 U "원작 그림을 AI 로 확대"(2026-11-03, v0.8 — 비교 시험 뒤 "AI 일반이 나은듯" → `realesrgan-x4plus`)는 1.4·7.1·7.5·11.1 WP0·WP1f·WP5 에 답한다: 가져오기 도구의 **선택 기능**이고 기본은 원본 픽셀이다.
  확대 결과도 저장소에 넣지 않고(사용자 PC 에서 가져올 때 만들고 캐시), 확대 도구는 사용자 PC 에 따로 받는다. 시뮬레이션은 그림 배율을 모른다 — 같은 씨앗이면 확대 유무와 상관없이 결과가 같다.
- 이 문서에서 **"시야"** 는 주인공 시야(안개)만 뜻한다. 카메라가 보는 사각형은 **화면 사각형**(`World.ViewRect`), 길찾기의 선분 검사(`HasClearLine`)는 **길 트임**이라 부른다(v0.5, 4.5).

## 0. 핵심 결정 한눈에

| 주제 | 결정 |
|---|---|
| 프로젝트 | `Core`(그래픽·소리 재생 없는 순수 로직: 데이터·타일 배치 풀기·길찾기·시뮬레이션·틱 누적기·소리 고르기 규칙) / 게임(MonoGame: 렌더·소리 재생·입력·화면·앱) / 테스트(Core 만 참조) / 가져오기 도구(`tools/DiabloAssetImporter`: Core + MPQ 읽기)와 그 시험 |
| 자원 | 원작 디아블로 1(`diabdat.mpq`)의 몬스터·주인공·마법·타일·소리·음악은 가져오기 도구가 `Assets/d1/` 에 뽑는다(가져오기 표에 적은 것만, 결정적, 캐시). 원작 exe PNG 는 아이콘·획득물·타이틀·로고·커서에만. 자원이 없으면 빌드는 DDR0001, 실행은 안내 화면, 시험은 자원 대조만 건너뜀 |
| 헬파이어 묶음 (v0.6) | 같은 폴더의 `hellfire`·`hfmonk`·`hfmusic`·`hfvoice` MPQ = 콘텐츠 묶음 넷(`ContentBundle`). 가져오기 표 항목에 `bundle`, 정의(적·스테이지·직업·무기·음악)에 `requires`. MPQ 가 없으면 그 묶음만 건너뛰고(종료 코드 0) manifest 에 적음 → 시작 검사가 그 묶음 정의를 "꺼짐" 으로 세고 그 스테이지·직업 카드를 어둡게. 시뮬레이션은 묶음 유무를 모른다(6.17) |
| 직업 (v0.6) | `HeroClassDef`(6.16): 그림 묶음·몸 반경·능력치 덮어쓰기·시작 무기·특성·`requires`. `rules.hero` = 공통 규칙 + 기본 능력치 + 고른 직업 ID. 월드는 `World.HeroClass` 하나를 받는다. MVP = `sorcerer` 하나 |
| 맵 배치 (v0.6) | 넓은 방 + 안쪽 방: 둘레 테두리·문 + 조각 놓기(`at`·`line`·**`grid`** 블록, 둘레에 겹치는 **`over_border`**) + `open_center`·방 기준점 `room_*`. 갇힌 바닥 칸은 길 검사 오류, 큰 적(26·34)은 모든 방 기준점까지 길. 포위·소환·지점 예고 자리는 길 거리 ≤ 직선 × 1.5(4.10) |
| 판 구조 (v0.7) | 판(`RunState`) → 액트(= `StageDef`, 방 순번 표·방 맵 풀·문 보상·`next_act`) → 방(`RoomPlan` → **방마다 새 `World`**). 방 흐름 들어옴 → 생존 → 정리 → 보상 → 문 은 새 단계 `SimPhase.RoomFlow` 의 상태 기계(`World.Room`). 판 흐름은 `RunPlanner`(순수, `(씨앗, 액트, 순번)` 마다 따로), 방 사이 이동은 Core `RunSession` 이 빌드(`IHeroCarry`)를 넘겨 다음 `World` 를 만든다. 난이도는 방 시계로 정한 **난이도 분 t**(3.6). v0.8: 방 맵 = (맵, 원판/거울판) 쌍, 시간 효과·허리띠·`단련`·성소 대가도 짐, 액트 보스 방 문 단계에서 **계단 저장**(`RunSnapshot`, 불러오면 지움) |
| AI 확대 (v0.8) | 가져오기 `--upscale realesrgan-x4plus`(기본 끔): 사용자 PC 의 Real-ESRGAN ncnn-vulkan(해시 확인)으로 타일·벽·칸 폭 160 이상 몬스터를 4배 → 2배로 줄여 씀. 없거나 실패하면 원본 픽셀(종료 코드 6·manifest). 그림 배율은 메타 `scale`, 월드 렌더 타깃은 배율만큼(1920×1080). 확대 결과는 GPU 마다 바이트가 다를 수 있어 manifest 에 도구·모델 해시만 적는다 |
| 개체 모델 | 범용 ECS 를 쓰지 않는다. **종류별 풀(EntityStore) + 고정 순서 시스템**. 개체 클래스는 `partial` 이고 묶음마다 자기 필드 조각을 가진다(11.2). 적의 다양성은 상속이 아니라 **행동 부품(전략) 조합** |
| 데이터 | `Data/**/*.json`(경로 서수 정렬로 읽음), 한 파일에 여러 종류를 담을 수 있음(적 + 그 적의 투사체). `extends` 로 변형 정의. 로딩 때 오류를 모두 모아 보여 줌. 게임 정의는 가져오기가 만든 파일 이름만 알고 원작 경로는 모른다 |
| 좌표 | 월드 = 등각 아레나 그림의 **화면 픽셀**(실수). 하위 타일 ↔ 월드는 `IsoMath` 한 곳. 화면 어느 방향이든 같은 속도(4.1) |
| 루프 | 고정 60Hz 틱 + 보간 렌더. 모든 시간(메뉴·연출·표시 이펙트·흔들림 포함)은 틱으로 센다 → `--seed` 재현, 캡처 시각 정확, 같은 명령이면 같은 PNG |
| 길찾기 | 16px 격자(외곽 상자 전체, 마름모 밖은 맵 밖) + 장애물까지 거리(clearance) 장. 무리 = 몸 크기 2등급 플로우 필드, 보스·중간 보스 = A*(이진 힙, 8방향, 코너 자르기 금지, 끈 당기기). 적 부품은 **이동 의도**만 내고 길은 내비가 정한다 |
| 충돌 | **저장소의 타일 배치 + 타일 성질 표**에서 만든 평행사변형이 정본(가져오기 도구가 원작 SOL 과 대조). 정확한 도형 밀어내기 + 원-원 해소(겹친 점은 결정적 방향), 이웃 질의는 32px 공간 해시 |
| 시야 (v0.5) | Core `VisibilityField`: **하위 타일 칸** 단위, 주인공 칸 중심 → 칸 중심 정수 선분이 시야 막힘 칸(성질 표 대문자, 원작 SOL bit1)을 지나지 않으면 보임, 반경 = 주인공 빛 반경, 두 줄 벽은 같은 메가타일의 벽 칸이 함께 보임. 주인공 칸·반경이 바뀐 틱에 다시 계산(`BeginTick`), 가 본 칸 비트맵. 질의 `IsClearlyVisible`(몸 위 끝까지 화면 안)을 원거리·돌진 규칙이 쓴다. 그리기는 이것을 읽어 안개(보임·기억·검정)를 빛 지도에 합친다(값은 알파 채널, 7.10) |
| 렌더 | 960×540 월드 렌더 타깃 → 정수배 점 확대 후 선형 축소(레터박스). 바닥은 구운 조각 텍스처, 뒷둘레 벽은 바닥 바로 뒤·앞둘레 턱은 유닛 뒤 단계, 안쪽 벽 기둥만 유닛과 y 정렬, 주인공을 가리는 벽은 반투명. 원작 시트는 (그림 묶음 × 변종)마다 모든 동작 프레임을 잘라 한 페이지에 묶은 아틀라스, TRN 변종은 파일. 빛은 곱하기 빛 지도 |
| 소리 | 시뮬레이션은 소리를 모르고 신호(`CueEvent`)·표시 이벤트만 낸다. 게임 `AudioSystem` 이 연결 표(`sound_bindings`)로 소리를 고르고 동시 재생 제한·우선순위를 적용한다(7.9). 소리 길이는 자원 색인에서 읽어 무음·소리 켬이 같은 수를 센다. 개발 옵션 실행은 OpenAL 을 출력 없는 장치(`ALSOFT_DRIVERS=null`)로 열고 기본 무음 |
| 검증 | `--autoplay --seed --capture --capture-at --quit-after --stage --room --map --hero --autoplay-visit --debug-nav --preview --log-ticks --log-hash --check --stress --exit-on-result --mute --sound --sound-device --reveal`. 창을 앞으로 끌어오지 않고, 개발 옵션 실행은 사용자 저장 파일을 **읽지도 쓰지도 않으며** 스피커로 소리를 내지 않는다(`--sound-device default` 를 줄 때만, 사용자 동의) |

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
│     └─ Table/                      가져오기 표 JSON 9개(v0.8: + upscale.json — AI 확대 모델·허용 해시·범위. 원작 경로·TRN·칸 폭·쓸 프레임·그림자 알파·타일셋 원본 경로 → 만들 파일 이름. 원작 바이트 없음, 1.4)
├─ src/
│  ├─ DiabloDefenceRemastered.Core/            클래스 라이브러리. MonoGame 참조 없음
│  │  ├─ Common/        EntityId, EntityStore<T>, Pool<T>, SimRandom, StableHash, FacingMath(방향→열)·Compass, Rgb, Int2, RectI, MathX,
│  │  │                 AnimClock(클립·프레임·이벤트 프레임), CameraMath(ViewRect), TickAccumulator(누적기·틱 상한·캡처 멈춤)
│  │  ├─ Defs/          정의 record (SpriteDef, TilesetDef, MapDef, EnemyDef, WeaponDef, SoundDef, HeroClassDef(v0.6) …), DefRef<T>, StatKey, EnemyStatKey,
│  │  │                 ContentBundle(v0.6 — 헬파이어 묶음 이름)
│  │  ├─ Data/
│  │  │  ├─ Registry/   PartRegistry, PartKind, IPartFactory, IDataValidator, ValidationContext, DataError, GameDefs   (계약, WP1)
│  │  │  │              StageReach(스테이지에서 나올 수 있는 적, 6.14)                                       (v0.6.1 계약 WP1d, 소유 WP4)
│  │  │  ├─ Json/       DefJson(공용 STJ 옵션: snake_case·주석·Disallow), 공용 변환기(Vector2·TileCoord·Rgb·RectI)    (계약, WP1b)
│  │  │  └─ Loading/    DataLoader, JsonMerge(extends·*_add·*_patch·페이즈 풀기), 나머지 변환기, 공통 검증 규칙     (WP7)
│  │  ├─ Map/           IsoMath(하위 타일 ↔ 월드), TileCoord, MapDataReader(tilesets·maps 만 읽기), ArenaLayout(배치 → 메가타일 격자),
│  │  │                 ArenaGrid(하위 타일 막힘·둘레 표시), ResolvedMap(입구·기준점·시작 = 월드 px)                  (계약, WP1b)
│  │  │                 GateZones(문 틈 칸·맞은편 문), MapArea(걸을 넓이), RoomMapRules(작은 방 검사 상수)              (v0.7 계약·구현 WP1e)
│  │  │                 MapMirror(거울판 MapDef — 짝 표로 다시 깖)                                                   (v0.8 계약·구현 WP1f)
│  │  ├─ Assets/        AssetFormat(판 번호), AssetManifest, ImportTable(가져오기 표 9개 모양·만들 이름 목록 — v0.8 UpscaleTable), SpriteMeta,
│  │  │                 ArenaArt(arena.json·바닥 조각 이름 규칙), SoundIndex(소리 길이)                                (계약, WP1b)
│  │  │                 ContentAvailability(있는 묶음 → 꺼진 스테이지·직업, 6.17)                                       (계약·구현 WP1d)
│  │  │                 DefAssetNames(정의 → 파일·주인 D, 13.5-10·시작 검사)                                   (v0.6.1 계약 WP1d, 본문 WP7)
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
│  │  │  ├─ Visibility/ VisibilityField(주인공 시야·가 본 칸, 4.11), FogMath(안개 알파·칸 바뀜, 7.10)        (계약·구현 WP1c, 그 뒤 VisibilityField 는 WP2·FogMath 는 WP5)
│  │  │  ├─ Enemies/    EnemySystem, 생애 상태, Parts/(행동 부품·적 동작), Ranks/(정예·우두머리·접두사), Buffs/
│  │  │  ├─ Director/   생성 누적기, 방 이벤트(t → 방 시계), 상한 정리, 생성 출처·우선순위
│  │  │  ├─ Run/        (v0.7) RunSeeds, RunPlanner(판 흐름 결정), RoomEvents(이벤트 → 방 시계), RoomFlow·RoomFlowSystem(방 흐름 상태 기계),
│  │  │  │              RunState·RunSession(방마다 World 만들기·빌드 넘기기), v0.8: RewardSpot·RunSnapshot(계단 저장)·RoomStartLimits   (계약·구현 WP1e·WP1f, 그 뒤 WP4)
│  │  │  └─ Autoplay/   자동 조작 정책(이동·카드 고르기·v0.7: 정리·보상 집기·문 고르기)
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
│     ├─ Loading/       TextureStore(아틀라스·변종·색 키, 액트별 미리 읽기 — v0.7: 방 맵 풀 전부), CursorLoader(.cur), FontProvider
│     ├─ Rendering/     RenderServices, VirtualScreen, Camera, WorldRenderer, ArenaRenderer(바닥 조각·벽 기둥·가림), Layers, SpriteDraw,
│     │                 Lighting, FogRenderer(안개, 7.10), FxSystem(표시 이펙트), CorpseLayer, DamageNumbers, DebugNavOverlay
│     ├─ Audio/         AudioServices(연결 파일), AudioSystem(신호 → 소리), SoundBank(WAV 읽기), MusicPlayer(페이드·덕킹)
│     ├─ Input/         KeyboardMouseSource, InputMap
│     ├─ Scenes/        SceneFactory, Title, Play(방 이동 어두워짐 포함), Result, DataError, Preview, Overlays/(LevelUp — 마법서·성소 보상 창 겸함, Chest, Pause, Settings), AutoplayDriver
│     │                 (v0.7: StageSelect 없앰 — 액트는 판 안에서 이어진다, 8.1)
│     │                 (직업 단계: HeroSelect — 8.1)
│     └─ UI/            UiCanvas, Widgets, Hud, Minimap, Theme
└─ tests/
   ├─ DiabloDefenceRemastered.Tests/           xunit.v3. Core 만 참조
   │  ├─ TestKit/       TestWorldBuilder, BruteForceSpatial, StraightNav, TestDefs(작은 타일셋·맵 포함), TestCategories
   │  ├─ Fixtures/      data/(로더 시험용 작은·고장 난 JSON), variants/(코드 없이 만든 변형 적), perf_scenario.json
   │  └─ Common/ Data/ Map/ Assets/ Geometry/ Nav/ Visibility/ Spatial/ Movement/ Invariants/ Combat/ Weapons/ Hero/ Progression/
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
powershell -ExecutionPolicy Bypass -File tools\import-all.ps1 -Diabdat "<…>\diabdat.mpq" -Exe "<…>\DiabloDefence.exe" [-Hellfire "<폴더>"] [-SkipBundles "<묶음,…|all>"] [-Upscale realesrgan-x4plus] [-UpscaleBin "<폴더>"] [-UpscaleGpu <n>]   # v0.6.1·v0.8: 도구 옵션을 그대로 넘김

# 도구만
dotnet run --project tools/DiabloAssetImporter -c Release -- import --diabdat <diabdat.mpq> [--out <Assets 폴더>] [--force] [--jobs <n>] [--hellfire <폴더>] [--skip-bundles <묶음,…|all>]
                                                                [--upscale <모델>] [--upscale-bin <폴더>] [--upscale-gpu <n>]   # v0.8 AI 확대(아래 "AI 확대"), 기본 끔
dotnet run --project tools/DiabloAssetImporter -- upscaler-check [--upscale-bin <폴더>] [--upscale-gpu <n>]   # v0.8: 확대 도구 해시·Vulkan 런타임 확인 + 임시 폴더에서 작은 그림 한 장을 변환해 장치 줄을 보임(Assets 는 안 만듦, v0.8.1)
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
  **v0.6.1 방 배열 대조(13.5-22)**: 원본이 있으면 알려진 L1·L2 방 DUN(경로 목록은 가져오기 표 `Table/tilesets.json` 의 타일셋 항목 `audit_dun` — 타일셋 `dir` 기준 파일 이름만, 예 l1 의 `rnd6.dun`·`banner1.dun`·`banner2.dun`·`skngdo.dun`, 단계 3 의 l2 에 `blind1.dun`·`blind2.dun`·`bonecha1.dun`·`bonecha2.dun` 등 — 목록은 WP0 이 원본 목록으로 확인.
  v0.7.1: 작은 방 맵이 생기며 대조할 맵이 늘었다 — **맵이 본뜬 원작 방은 반드시 목록에 넣는다**: 맵 JSON 첫머리 주석 `// 본뜬 원작 방: <dun>, …` 과 `audit_dun` 이 맞는지 자원 없이 시험한다, 6.14 13.5-22)을 **메모리에서만** 읽어(내용을 출력·저장하지 않음),
  저장소 `Data/maps/*.json` 을 펼친 메가타일 격자(`MapDataReader` → `ArenaLayout.Expand`)와 4×4 창마다 비교한다. 두 쪽 모두 바닥 번호(그 타일셋 `floor`)와 DUN 의 0 을 "바닥" 하나로 본 뒤, 16칸이 모두 같고 그 창에 바닥 아닌 칸이 **8개 이상**이면
  `maps/cathedral_arena.json (mx,my): rnd6.dun (x,y) 와 4×4 배열이 같다` 와 종료 코드 4. 바닥 아닌 칸 8개 기준은 "벽 모서리 하나(Γ + 두 벽 = 7칸)" 같은 어느 방에나 생기는 모양을 거르려는 시험 상수다 — WP0 이 실제 배치에서 거짓 실패가 없는지 보고 고친다.
  DUN 형식은 부록 A.3 끝. "원작 방을 본뜬다" 의 뜻은 6.11 "원작 방 본뜨기"·R24.
- 원본은 `FileAccess.Read`·`FileShare.Read` 로만 연다. 원본 폴더에 캐시·로그를 만들지 않는다.
- **기본 내용은 `diabdat.mpq` 에만 기댄다**(기획 19.2). `devilutionx.mpq` 는 열지 않는다. 헬파이어 MPQ 는 v0.6 에 정식 자원이 됐지만(기획 19.4) **표에서 `bundle` 을 단 항목만** 거기서 찾는다 — 아래 "헬파이어 묶음".

**헬파이어 묶음** (v0.6, 사용자 결정 H, 기획 19.4)

- 묶음 넷 = MPQ 넷: `hellfire`(`hellfire.mpq`), `hfmonk`(`hfmonk.mpq`), `hfmusic`(`hfmusic.mpq`), `hfvoice`(`hfvoice.mpq`). 이름은 Core `Defs/ContentBundle`(열거형, JSON 은 소문자 이름) 하나다.
- **찾기**: `--hellfire <폴더>` 의 기본값은 `--diabdat` 파일이 있는 폴더다(v0.5 까지는 "줄 때만"). 그 폴더에서 네 이름을 대소문자 무관으로 찾는다(`HELLFIRE.MPQ`).
  `--skip-bundles <묶음,…>`(또는 `all`)은 있어도 열지 않는다 — 이 PC(네 MPQ 가 다 있음)에서 "헬파이어 없는 PC" 를 재현하는 검증용이다.
- **없는 MPQ = 그 묶음 건너뛰기**(실패 아님, 종료 코드 0): 그 묶음 항목을 만들지 않고 manifest 의 `skipped_bundles` 에 `{ "bundle", "mpq", "reason" }` 로 적고 끝 보고에 한 줄.
  `reason` = `"없음"` | `"--skip-bundles"` | (v0.6.1) `"열 수 없음"` | `"판 다름: <빠진 원본 경로 목록>"`.
- **쓸 수 없는 묶음 MPQ도 그 묶음만 건너뛴다**(v0.6.1 — v0.6 은 종료 코드 2·3 이었다): `--hellfire` 기본값이 diabdat 폴더라 사용자가 고르지 않은 묶음 MPQ 도 저절로 열린다.
  그 MPQ 가 깨져 열리지 않거나(`열 수 없음`), 판이 달라 그 묶음 표 항목의 원본 파일이 하나라도 없으면(`판 다름`) 그 묶음 **전체**를 건너뛰고 기본 항목은 그대로 만든다 — 종료 코드 0, 끝 보고 맨 끝에
  `경고: 헬파이어 묶음 hfmusic 을 건너뜀 — hfmusic.mpq 를 열 수 없음` 꼴로 묶음마다 한 줄(사용자가 "기본 내용까지 자원 없음" 이 되지 않게 — 기획 19.4 "없으면 그 내용만 꺼진다").
  판이 다른 MPQ 를 "없음" 으로 덮지 않는다는 v0.6 의 뜻은 `reason` 에 빠진 파일 목록을 남기는 것으로 지킨다. 원본 찾는 순서(아래)에서 `prefer` 로만 쓰는 묶음(`hfvoice`)이 쓸 수 없으면 그 묶음만 건너뛰고 다음 순서로 찾는다.
  **실패로 남기는 경우**: `diabdat.mpq` 를 못 찾거나 못 엶(2), 기본 항목의 원본 파일이 없음(3), 그리고 사용자가 `--hellfire <폴더>` 를 **직접 준** 실행에서 묶음 MPQ 가 열리지 않거나(2) 파일이 빠짐(3) — 직접 고른 것은 조용히 넘기지 않는다.
  이 두 실패의 문구 끝에 우회 명령 한 줄을 붙인다: `헬파이어 없이 가져오려면: … --skip-bundles hellfire`(`import-all.ps1` 이면 `-SkipBundles hellfire`).
- **한 묶음은 전부 있거나 전부 없다**(기획 19.4): 묶음 항목 일부만 만들어지는 경우는 없다(위 규칙 — 쓸 수 없으면 통째로 건너뜀).
- **타일셋 묶음과 맵**(v0.6.1): 맵 정의에는 `requires` 를 두지 않는다(`MapDataReader` 가 모르는 속성 오류). 맵 그림(`arena.json`·`floor_*`·`columns.*`·`minimap.png`)의 묶음은 **그 맵이 쓰는 타일셋의 표 항목 `bundle`** 이다.
  타일셋 항목의 묶음을 건너뛰면 그 타일셋을 쓰는 맵 굽기도 건너뛴다(manifest 에 그 맵 그림이 없다 — 시작 검사가 `ImportTable.OutputBundles(arenas)` 로 "꺼짐" 으로 센다, 1.4 끝).
- **원본 찾는 순서**(항목 하나의 원본 경로마다): 항목의 `prefer` 묶음 MPQ(있으면, 적힌 순서) → `diabdat.mpq` → 항목 `bundle` 의 MPQ. 처음 찾은 곳의 바이트를 쓴다.
  - 기본 항목(`bundle` 없음)은 diabdat 만 본다. 그림·기본 소리는 diabdat 먼저(기존 규칙, 부록 A.1).
  - 펠트윈·스컬윙·본 데몬은 그림이 diabdat 에 있어도 표 항목에 `"bundle": "hellfire"` 를 단다 — 판단 기준은 내용이다(기획 19.4). 그래서 hellfire.mpq 가 없으면 그림도 만들지 않는다.
    (v0.6.1 사실 바로잡음) 소리는 펠트윈만 hellfire 에 있고(`newsfx\ftwin`), 스컬윙·본 데몬의 원작 소리 묶음(`demskel\demskl`)은 원본 어디에도 없다(조사 0/8) — 두 적은 WP10 에서 헬파이어 소리를 빌려 단다.
  - 수도사 목소리·헬파이어 대사처럼 같은 이름이 16비트(hfvoice)와 8비트(hfmonk·hellfire)에 다 있으면 `"bundle": "hfmonk", "prefer": ["hfvoice"]` — 출력 이름은 하나(`d1/sound/…`)이고 어느 MPQ 에서 왔는지는 manifest 의 파일 줄 `sources` 가 안다.
- `hfvoice` 는 **원본 우선순위에만 쓰는 묶음**이다: 정의의 `requires` 에 쓰지 않는다(13.5-20 오류). 없으면 8비트판으로 같은 이름이 만들어져 꺼지는 내용이 없다.
- 헬파이어 층 경로는 `nlevels\l5data\…`·`nlevels\l6data\…` 다(실행 파일 문자열의 `Levels/L5Data/…` 로는 없다 — 부록 A.1).
- MVP 표에는 묶음 항목이 아직 없다(기획 19.4 — MVP 내용은 헬파이어를 쓰지 않음). 도구 시험이 합성 MPQ 로 "묶음 MPQ 없음 → 건너뜀·manifest 기록", "`--skip-bundles`", "있는 MPQ 에 파일 없음 → 그 묶음만 건너뜀(`판 다름`)·`--hellfire` 직접 지정이면 3",
  "깨진 묶음 MPQ → `열 수 없음`", "`prefer` 순서", "타일셋 묶음 건너뜀 → 그 맵 굽기 건너뜀" 을 본다(10.1 `Pipeline`). 실제 묶음 MPQ 의 항목이 War3Net 으로 읽히는지는 `RealMpq` 가 이 PC 에서 본다(10.1, v0.6.1).

**구성**: `Mpq/`(War3Net 감싸기, 여러 MPQ 우선순위 — diabdat 먼저), `Formats/`(순수 해석기: 스프라이트 CL2·CEL, 레벨 CEL 조각, MIN, TIL, SOL, PAL, TRN, WAV),
`Bake/`(시트 잘라 묶기, 아레나 바닥·기둥, 미니맵, 아이콘, 소리 변환), `Output/`(PNG 쓰기, 임시 파일 → 바꿔치기, manifest, 캐시, 소리 색인), `Audit/`.
형식 규칙은 **부록 A**(조사 문서·시제품 `d1conv`·`d1map` 이 확인한 것을 저장소로 옮긴 것)가 정본이다. 조사 문서와 시제품은 세션 임시 폴더에 있어 사라질 수 있으므로 WP0 은 부록 A 와 이 절만 보고 만들 수 있어야 한다.
시제품 코드는 참고만 하고 WP0 이 새로 쓴다(시제품은 우리가 쓴 코드지만 DevilutionX 소스에서 읽은 식이 섞여 있다 — 식은 부록 A 의 사실 요약으로 옮기고 코드는 옮기지 않는다, R24).

**MPQ 읽기와 스레드** — War3Net 의 `MpqArchive`·`MpqStream` 은 아카이브 하나의 기반 스트림을 `Seek`·`Read` 로 같이 쓰므로 여러 스레드가 동시에 읽으면 바이트가 섞인다.
원본 항목 읽기(복호·압축 풀기)는 **주 스레드에서 차례로** 해서 `byte[]` 로 넘기고, `--jobs` 병렬은 해독·굽기·PNG 압축에만 쓴다. 캐시 키의 "입력 원본 항목 바이트 해시" 도 주 스레드가 읽은 바이트로 만든다.

**가져오기 표** `tools/DiabloAssetImporter/Table/*.json` 9개(v0.8: `upscale.json` 을 더함 — 아래 "AI 확대") — 모양은 Core `Assets/ImportTable` 의 record 다. Core 가 표 전체를 **엄격하게**(모르는 속성 오류, 6.1 과 같은 STJ 옵션) 읽고,
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
      "variants": { "": null } },
    // (직업 단계 예, v0.6) 수도사: 묶음 hfmonk, 칸 폭이 동작마다 다름, 죽기는 자기 벌(맨손 벌에서 빌리지 않음 — 공통 규칙으로 두지 않는다)
    { "out": "d1/hero/mlt", "cl2": "plrgfx\\monk\\mlt\\mlt{a}.cl2", "width": 112, "bundle": "hfmonk",
      "actions": { "idle": "as", "walk": "aw", "attack": { "file": "at", "width": 130 },
                   "cast_fire": { "file": "fm", "width": 114 }, "cast_lightning": { "file": "lm", "width": 114 },
                   "cast_magic": { "file": "qm", "width": 114 }, "die": { "file": "dt", "width": 160 } } } ] }

// Table/missiles.json — shadow_alpha 를 적은 항목만 색인 0 을 그림자로 바꾼다(firerun·guard 처럼 그림자가 있는 것). 없으면 색인 0 도 그냥 색
{ "palette": "levels\\l1data\\l1.pal",
  "missiles": [
    { "out": "d1/missiles/fireba",   "cl2": "missiles\\fireba{d}.cl2", "dirs": 16, "width": 96 },
    { "out": "d1/missiles/arrows",   "cl2": "missiles\\arrows.cl2", "frames_are_dirs": 16, "width": 96 },
    { "out": "d1/missiles/firewal2", "cl2": "missiles\\firewal2.cl2", "width": 128, "frames": [1, 11] },
    { "out": "d1/missiles/firerun",  "cl2": "missiles\\firerun{d}.cl2", "dirs": 8, "width": 128, "shadow_alpha": 0.5 } ] }

// Table/icons.json — 아이콘 = 그림 프레임 한 장 이상을 겹친 size×size(v0.9.1: 56×56 — 원작 마법 아이콘 칸 그대로). layers 는 그리는 순서
// v0.9.1(코드 9128cc9): 무기 아이콘은 cel 항목(ctrlpan\spelicon.cel 의 칸, 1부터 — 파이어볼 12·매직 미사일 39·비전 구체 42). cl2·cel 중 하나만
{ "size": 56, "background": [16, 12, 10],          // "palette"(선택) — 없으면 missiles.json 의 palette
  "icons": [
    { "out": "d1/icons/fireball", "cel": "ctrlpan\\spelicon.cel", "width": 56, "layers": [ { "frame": 12 } ] },
    // 아래는 cl2 항목 모양(v0.9 까지의 무기 아이콘 — 투사체 프레임을 겹쳐 굽기). 지금 표에는 없다
    { "out": "d1/icons/multishot", "cl2": "missiles\\arrows.cl2", "frames_are_dirs": 16, "width": 96,
      "layers": [ { "dir": 3, "frame": 1, "offset": [-6, 4] }, { "dir": 4, "frame": 1 }, { "dir": 5, "frame": 1, "offset": [6, 4] } ] } ] }

// Table/sounds.json — {키} 는 vars 의 값마다 펼친다. 출력 = "d1/sound/" + 원작 경로 소문자·'/'
{ "sounds": [
    { "path": "sfx\\misc\\cbolt.wav" },
    { "path": "monsters\\zombie\\zombie{k}{i}.wav", "vars": { "k": ["a", "h", "d", "s"], "i": ["1", "2"] } },
    // (v0.6 예) 같은 이름이 hfvoice(16비트)·hfmonk(8비트)에 있음 → hfvoice 먼저
    { "path": "sfx\\monk\\monk{i}.wav", "vars": { "i": ["69", "69b", "71"] }, "bundle": "hfmonk", "prefer": ["hfvoice"] } ] }

// Table/music.json
{ "music": [ { "path": "music\\dintro.wav" }, { "path": "music\\dlvla.wav" }, { "path": "music\\dlvlb.wav" } ] }

// Table/exe.json — 원작 exe PNG 49장·커서의 이름(이름 대조용, 그림 없음)
{ "files": [ "TITLE.png", "LOGO.png", "Gold.png", "DIInop.cur" ] }

// Table/tilesets.json — 타일셋 원본 정보(도구만 씀). 게임 정의 Data/tilesets(6.11)의 id 와 같은 키
{ "tilesets": [
    { "id": "l1", "dir": "levels\\l1data", "cel": "l1.cel", "min": "l1.min", "til": "l1.til", "sol": "l1.sol",
      "pieces_per_subtile": 10, "palettes": { "l1_1": "l1_1.pal", "l1_2": "l1_2.pal" },
      "audit_dun": ["rnd6.dun", "banner1.dun", "banner2.dun", "skngdo.dun"],   // v0.6.1: audit 의 방 배열 대조(13.5-22)만 읽음 — 메모리에서만
      "overlay": { "cel": "l1s.cel",                       // 출처: DevilutionX 참고(아치 덧그림 번호표, 부록 A.5)
                   "frames": { "1": [11, 70, 320, 210, 340, 417], "2": [10, 248, 324, 343, 330, 420],
                               "3": [252], "4": [254], "5": [258], "6": [266] } } },
    // (헬파이어 스테이지 단계 예, v0.6) 지하실 L5 — 경로는 nlevels, 팔레트 하나. 덧그림 번호표는 그 단계에 확인
    { "id": "l5", "dir": "nlevels\\l5data", "cel": "l5.cel", "min": "l5.min", "til": "l5.til", "sol": "l5.sol",
      "pieces_per_subtile": 10, "palettes": { "l5base": "l5base.pal" }, "bundle": "hellfire" } ] }

// Table/upscale.json (v0.8, 사용자 결정 U) — AI 확대의 모델·허용 해시·적용 범위. --upscale 을 주지 않으면 읽기만 하고 쓰지 않는다
{ "models": {
    "realesrgan-x4plus": { "scale": 4,                                   // 모델 배율(실행 파일 -s 값)
      "files": { "realesrgan-ncnn-vulkan.exe": "<sha256>", "vcomp140.dll": "<sha256>",      // --upscale-bin 폴더 기준 상대 경로 → SHA-256.
                 "models/realesrgan-x4plus.param": "<sha256>", "models/realesrgan-x4plus.bin": "<sha256>" } } },   // 값은 WP0 이 공식 릴리스 파일로 채움
  "output_scale": 2,                                                     // 모델 결과(4배)를 이 배율로 줄여 씀(첫 값 — 기획 열린 질문 41). v0.8.1: 첫 판의 표 읽기는 2 만 받는다(다른 값은 표 오류 — 4 로 가려면 WP1g 계약 변경, 7.1 s)
  "apply": { "maps": true, "monsters_min_width": 160 },                  // 첫 값: 맵 그림(바닥 조각·벽 기둥·아치 덧그림) + 칸 폭 160 이상 몬스터. 주인공·마법·아이콘·exe PNG 는 안 함(기획 19.6)
  "include": [], "exclude": [] }                                         // 몬스터 표 항목 out 이름 또는 "tileset:<id>" 로 더하고 뺌(exclude 가 이김)
```

- 공통 필드: `out`(출력 이름, `d1/` 로 시작), 원작 경로 틀(`{a}` = 동작 글자, `{d}` = 방향 파일 1..`dirs`), `width`, `frames`([처음, 끝], 1부터 — 파일이 더 많을 때 쓸 범위),
  `shadow_alpha`(표 머리 → 항목 → 동작 순으로 덮어씀, 없으면 색인 0 을 그대로 검정으로), `variants`(이름 → TRN 경로, `""` = 원색),
  **`bundle`**(v0.6 — v0.5 의 `"optional": true` 를 대신함. 묶음 이름, 없으면 기본 = diabdat. 그 MPQ 가 없으면 항목을 건너뛰고 manifest 에 없다 — 정의가 가리키면 시작 검사가 "꺼짐" 으로 센다, 1.4 끝),
  **`prefer`**(v0.6, 소리·음악 항목 — 원본을 먼저 찾을 묶음 목록, 위 "헬파이어 묶음"). 모든 항목 종류(몬스터·주인공·마법·아이콘·소리·음악·타일셋)가 `bundle` 을 가진다. MVP 표에는 둘 다 없다.
  타일셋 항목의 `bundle` 은 그 타일셋을 쓰는 **맵 그림의 묶음**도 정한다(v0.6.1, 위 "타일셋 묶음과 맵").
  아이콘 `layers` 원소 = `{ "dir", "frame", "offset"?: [x,y], "rotate"?: 도 }`(겹침·회전은 1.4 변환 규칙 "아이콘").
- **칸 폭**: 도구가 파일에서 추정한 폭(32줄 건너뛰기 표, 부록 A.2)을 쓴다. 표의 `width` 는 **대조용**이고, 32줄보다 낮은 프레임처럼 추정이 안 되는 때만 대신 쓴다.
  둘이 다르면 종료 코드 4(`hero/slt cast_fire: 파일 폭 128, 표 96`). 마법사 `slt` 처럼 동작마다 폭이 다르면 동작 객체에 `width` 를 적는다.
  칸 폭을 96·128·160 으로 가정하지 않는다 — 헬파이어는 나크룰 226, 폭발 `ex_red3`·`ex_blu3` 292, 수도사 98~160(동작마다)까지 간다(v0.6, 기획 8.5·4.5).
- 표에 적지 않는 것: 프레임 수·높이(도구가 파일에서 읽는다. 정의의 값과는 게임 시작·시험이 대조 — 6.3), 원작 그림·소리 바이트, DevilutionX `*.tsv`(읽지도 복사하지도 않는다, 기획 19.1).
- **원본에서 온 수치**(R24): 저장소에 들어가는 원본 유래 숫자는 **사실 수치만 손으로** 적는다 — 경로·칸 폭·프레임 범위(이 표), 타일 번호·성질·SOL 보정 칸(6.11),
  아치 덧그림 번호표(위 `overlay`), 동작별 프레임 시간·타격 프레임(그림 묶음 클립, 6.3). DevilutionX 의 파일·표 구조·주석·코드는 옮기지 않는다.
  원작 방 DUN 의 **칸 배열**도 옮기지 않는다(v0.6.1) — 맵 배치는 크기·문 방향·벽 재료만 본뜨고 칸은 우리 조각 이름으로 새로 짠다(6.11 "원작 방 본뜨기", `audit` 가 대조).
  값마다 출처를 JSON 주석으로 남긴다: `// 출처: 원본 확인`(이 PC 의 diabdat 를 읽어 확인) 또는 `// 출처: DevilutionX 참고`(원본 파일에 없는 게임 동작 값). 둘 곳은 위에 적은 한 곳뿐이다.
  JSON 주석은 로더가 버리므로 원문 문자열 시험이 주석이 있는지 본다(6.14 13.5-16, v0.5.1).
  이 범위는 **사용자가 확인했다**(v0.5, 기획 열린 질문 19 닫힘·기획 19.1): DevilutionX(Sustainable Use License) 소스를 보고 안 사실 수치는 출처 주석을 달아 공개 저장소에 손으로 적어도 되고,
  `*.tsv`·코드·DUN 파일 자체와 표 구조·주석은 옮기지 않는다. 그래서 "안 되면 도구가 사용자 PC 에서 추정" 하는 대체 길(v0.4.1 13.6)은 만들지 않는다. 배포할 일이 생기면 다시 정한다(R24).
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
                                      표 해시(Table/*.json 9개를 이름 순으로 이은 바이트의 SHA-256 — v0.8: 8 → 9), 파일마다 경로·크기·SHA-256
                                      (v0.8) `upscale`(확대 요청·결과·도구·모델 해시 — 아래 "AI 확대"), 파일 줄마다 `scale`(그림 배율, 1 이면 적지 않음)
                                      (v0.6) 원본 줄마다 묶음(`bundle`, 기본은 없음), `skipped_bundles`(건너뛴 묶음·MPQ·이유),
                                      파일 줄마다 `bundle`(그 파일을 만든 표 항목의 묶음 — 맵 그림은 타일셋 항목의 묶음)·`sources`(v0.6.1: 바이트를 읽은 MPQ 이름 목록,
                                      서수 정렬 — 고유 몬스터 페이지처럼 CL2 는 hellfire, TRN 은 diabdat 인 출력이 있다. v0.6 의 이름 하나 `source` 를 대신함)
    monsters/zombie.png  zombie@bluered.png  zombie@grey.png …  zombie.json     그림 묶음 한 페이지(모든 동작, 변종마다) + 메타(변종 공통, 6.3)
    hero/slt.png  slt.json            missiles/fireba.png  fireba.json …      icons/fireball.png …
    maps/cathedral_arena/arena.json  floor_<cx>_<cy>.png  columns.png  columns.json  minimap.png       (7.6, 이름 규칙은 아래 "아레나")
    maps/cathedral_arena@m/…          (v0.8) 거울판 그림 — 맵 `mirror: true` 일 때만(6.11 "거울판"), 이름 규칙 같음
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
| 주인공 | 직업마다 장비 한 벌(기획 4.5): MVP 는 마법사 `slt` 의 `as`·`aw`·`fm`·`lm`·`qm`, 죽기 `sln\slndt`(기획 4.0). 직업 단계에 전사 `wms`(죽기 `wmn\wmndt`)·도적 `rlb`(`rln\rlndt`)·수도사 `mlt`(죽기 자기 벌 `mltdt`, `hfmonk`), 근접 시작 무기 직업은 공격 `at` 도. 프레임 수는 파일에서 읽는다(전사 `hn`·`mn` 죽기 15처럼 원작 표와 다른 곳이 있다) |
| 아이콘 | 표 `layers` 의 (방향, 프레임)을 차례로 겹쳐(`rotate` 는 가장 가까운 픽셀 회전, `offset` 은 정수 이동) `size`×`size`(v0.9.1: 56×56) 어두운 칸 가운데에 맞춰 줄인다(정수 상자 평균 — 결정적, 기획 5.9). 그림이 칸 안에 들면 줄이지 않고 1:1(원작 마법 아이콘 `spelicon` 56×56). `cel` 항목은 프레임 머리 없는 CEL(부록 A.2)의 프레임 전부를 방향 1개 묶음으로 푼다 |
| 아레나 | `MapDataReader` → `ArenaLayout.Expand`(6.11) → 메가타일 격자 → TIL → MIN(위 단부터 저장 — 뒤집어 읽음) → 레벨 CEL 조각(종류 = MIN 값 비트 12~14, 삼각형 패딩) + 맵 팔레트(`Table/tilesets.json` 의 `palettes`). 바닥 칸(걷기·투사체 막힘이 없는 칸 — 성질 `.`·`L`, 원작 바닥 판정 A.4) → 512×512 바닥 조각, 그 밖의 칸 → 하위 타일 기둥 그림(64×160) 아틀라스(같은 그림은 한 프레임), L1 아치 덧그림(`overlay`), 칸 목록 `arena.json`(Core `Assets/ArenaArt`), 미니맵(외곽 상자 1/16). **바닥 조각 이름 규칙**(자원 없이 계산, Core `ArenaArt.FloorChunks(ArenaGrid, IsoMath)`): 조각 (cx, cy) 는 바닥 칸(`.`·`L`)의 마름모 상자 `[C.x−32, C.x+32) × [C.y−16, C.y+16)` 가 하나라도 `[512cx, 512cx+512) × [512cy, 512cy+512)` 와 겹치면 있다 → `floor_<cx>_<cy>.png`. 픽셀이 모두 투명이어도 규칙이 있다고 하면 쓴다. 자세한 것은 7.6 |
| 성질 대조 | 타일셋의 성질 표(6.11)를 쓰는 메가타일에 대해 원본 SOL 과 비교한다. 비교는 **세 비트만**(v0.5: bit1 추가): bit0(Solid) = 걷기, bit1(BlockLight) = 시야, bit2(BlockMissile) = 투사체 — `sol & 7` 을 6.11 의 글자 하나로 바꿔 표 글자와 같은지 본다(`x` = 걷기·투사체, `X` = 셋 다 …). 다른 비트(Transparent·TransLeft/Right·Trap 128)는 무시한다(바이트를 그대로 비교하면 SOL 159/175/191 같은 벽 조각에서 거짓 실패). 다르면 그 칸이 표의 `sol_known_diffs`(또는 `design_diffs`)에 그 원본 글자로 있어야 하고, 없으면 실패: `타일셋 l1 메가타일 15 하위 칸 0: 표 'X', SOL 'x'(시야 막힘 다름)` |
| 소리 | RIFF 해석. PCM 8비트(부호 없음) → 16비트 `(b − 128) << 8`, 16비트는 그대로. 채널·표본율 그대로, 표준 44바이트 머리로 다시 쓴다(다른 덩어리는 버림). 형식 ≠ PCM, 길이 0 이면 실패. 음악도 같은 함수(이미 16비트) |

**결정론** — 같은 원본 + 같은 표 + 같은 맵·타일셋 정의 + 같은 도구 판 → 같은 바이트.
- PNG 는 도구가 직접 쓴다: RGBA 8비트, 줄 필터 0 고정, `ZLibStream` 압축 수준 고정, `tIME`·글 덩어리 없음. JSON 은 정해진 키 순서·불변 문화·`\n`.
- 작업 목록은 출력 경로 서수 정렬, 묶기 순서 고정, 사전 순회 결과에 기대지 않는다. 원본 읽기는 주 스레드에서 차례로, `--jobs` 병렬은 해독·굽기·PNG 압축에만(작업 하나는 한 스레드) → 결과가 같다.
  (v0.8.1) `--upscale` 을 켜면 확대 대상 작업만 **세 단계**로 나뉜다(아래 "AI 확대" — 병렬 ① 해독·굽기·확대 전 판 → 주 스레드 ② 외부 프로그램(동시에 하나) → 병렬 ③ 줄이기·묶기·PNG). 확대하지 않는 작업은 지금처럼 한 스레드에서 끝까지 간다.
- 확인: 도구 시험(시험이 만든 작은 MPQ 로 두 번 → manifest 같음, `--jobs 1` 과 `--jobs 8` → manifest 같음), WP0 완료 기준(실제 원본으로 두 번 → manifest 같음).
  압축 구현이 .NET 판마다 다를 수 있어 **같은 런타임 판 안에서만** 보장한다(manifest 에 런타임 판을 적는다).
- **예외 — AI 확대 출력**(v0.8): `--upscale` 로 만든 파일(manifest 파일 줄 `scale` ≠ 1)은 같은 바이트를 보장하지 않는다. 외부 실행 파일이 GPU·드라이버마다 다른 부동소수 결과를 낼 수 있어서다.
  그래서 manifest 에는 **도구·모델 파일 해시와 요청·결과**만 적고(아래 "AI 확대"), 위 "두 번 → manifest 같음" 은 확대를 끈 실행에만 요구한다. 확대 출력도 만들어진 뒤에는 캐시가 그대로 다시 쓰므로 같은 PC 의 두 번째 실행은 같다.

**캐시** — 첫 실행은 PNG 압축이 대부분이라 수십 초 안팎으로 본다(WP0 이 재서 이 줄을 고친다).
- `d1/.cache/index.json`: 작업마다 키 = SHA-256(도구 판 + 작업 명세 JSON + 입력 원본 항목 바이트의 해시 + 맵이면 펼친 배치 해시).
  **도구 판 = 도구·Core 어셈블리 파일 바이트의 SHA-256**(사람이 올리는 번호가 아니다. `Deterministic` 빌드라 같은 소스면 같은 값) → `Bake`·`Formats` 를 고치고 번호를 안 올려도 캐시가 옛 출력을 쓰지 않는다.
  `AssetFormat.Version` 은 게임과의 **호환 판정**에만 쓴다(출력 모양·뜻이 바뀌면 올림, 아래).
  키가 같고 출력 파일이 있고 그 해시가 기록과 같으면 다시 만들지 않는다. `--force` 는 캐시를 무시한다. 캐시 적중·미적중 결과 바이트는 같다(시험).
- **굽기 결과가 바뀌는 도구 변경은 `AssetFormat.Version` 을 올린다**(WP0 규칙 — 메타·`arena.json` 모양, 바닥/벽 나눔, 그림자·잘라 내기 규칙). 게임은 도구 바이트를 모르므로 이 번호로만 "도구가 바뀌어 다시 가져와야 함" 을 안다.
  번호를 안 올리는 실수는 맵 쪽이면 `layout_hash`(타일셋 정의 해시 포함, 6.11)가, 표 쪽이면 표 해시가 대부분 잡는다.
- (v0.8) 캐시 키에 **실제로 쓸 확대 방식**을 더한다: 확대하지 않는 작업은 `"원본"`, 확대하는 작업은 (모델 이름, `upscale.json` 의 그 모델 `files` 해시들, `output_scale`, 확대 처리 판). GPU 번호는 넣지 않는다 —
  같은 입력의 확대 결과가 GPU 마다 달라도 한 번 만든 것을 다시 쓴다(다시 만들려면 `--force`, 결과 바이트는 manifest 의 파일 해시가 안다 — 기획 19.6). 되돌아간 실행(아래)의 출력은 `"원본"` 키로만 남아, 다음에 확대가 되면 확대 대상만 다시 만든다.
- (v0.8.1) **확대 중간 캐시** `d1/.cache/upscaled/`: 외부 프로그램이 돌려준 4배 그림을 (확대 전 판 PNG 바이트의 SHA-256 + 위 확대 방식) 키로 남긴다(색 판·알파 판 따로, 만든 때의 `gpu` 를 기록).
  되돌아간 실행에서도 그때까지 **성공한 묶음의 결과는 남긴다** — 그 실행의 출력은 규칙대로 전부 원본이지만, 다음 실행은 남은 판만 돌려 이어서 끝낼 수 있다(진척이 쌓임). 출력 캐시(위)와 따로라 `"원본"` 키 출력과 섞이지 않는다.
  `--force` 는 이 캐시도 무시한다. 새 manifest 에 없는 출력을 지우는 규칙과 무관하게, 키가 더 이상 쓰이지 않는 항목은 실행 끝에 지운다(쓰인 키 목록 밖).

**AI 확대 `--upscale`** (v0.8, 사용자 결정 U — 2026-11-03, 기획 v0.8.2 19.6·brief 요구 16)

사용자가 원작 그림 일부를 Real-ESRGAN 두 모델(일반 `realesrgan-x4plus`, 그림용 `realesrgan-x4plus-anime`)로 4배 확대해 비교했고 일반 모델을 골랐다(타일·큰 몬스터는 좋음, 작은 캐릭터는 얼굴·손이 뭉개짐, 그림자·투명 가장자리는 계단이 남음, 걷기 프레임마다 결과가 조금씩 달라 떨림을 봐야 함).
가져오기의 **선택 기능**이다 — 주지 않으면(기본) 지금까지와 같은 원본 픽셀 출력이고 확대 프로그램을 찾지도 않으며 manifest 의 `upscale` 은 `"status": "off"` 다. 게임에는 켜고 끄는 설정이 없다(가져오기가 만든 그림을 그대로 씀).

- **도구는 받지 않고 찾기만 한다**: 실행 파일·모델은 사용자가 공식 배포처에서 따로 받아 저장소 밖 폴더에 푼다(Real-ESRGAN ncnn-vulkan **v0.2.5.0** Windows 판 — 사용자 시험에 쓴 것, BSD-3-Clause). 도구는 내려받지 않는다(네트워크에 기대지 않음).
  저장소·Assets·도구 출력 어디에도 넣지 않고 배포하지 않으므로 고지 의무는 없다 — README 자원 절에 이름·판·라이선스·받는 곳·풀 폴더 한 단락(WP0).
  폴더는 `--upscale-bin <폴더>`(기획 19.6 의 이름), 기본 `%LOCALAPPDATA%\DiabloDefenceRemastered\realesrgan-ncnn-vulkan\`(저장소 밖).
- **해시 확인 단계**: 시작할 때 `upscale.json` 의 그 모델 `files`(실행 파일·DLL·모델 두 파일) 마다 `--upscale-bin` 기준 파일을 SHA-256 으로 대조한다. 하나라도 없거나 다르면 실행 파일을 **띄우지 않는다**(모르는 실행 파일을 돌리지 않음) — 아래 "되돌아감".
  허용 해시는 WP0 이 공식 배포판 파일에서 받아 표에 적는다(사용자 시험 때 받은 zip 의 SHA-256 `abc02804e17982a3be33675e4d471e91ea374e65b70167abc09e31acb412802d` — 같은 배포판인지 대조용). 새 판으로 바꿀 때는 표만 고친다(표 해시가 바뀌어 게임이 다시 가져오기를 알린다).
  `--upscale <모델>` 의 이름이 표 `models` 에 없으면 입력 오류 **종료 코드 2**(아무것도 만들지 않음 — 되돌아가지 않는다). 지금 표에는 `realesrgan-x4plus` 하나다.
- **Vulkan 런타임 확인**(v0.8.1): 실행 파일(v0.2.5.0)은 `vulkan-1.dll` 을 정적으로 가져온다(사용자 zip 의 PE 가져오기 표로 확인 — `vulkan-1.dll`·`ole32`·`KERNEL32`·`OLEAUT32`·`VCOMP140`). 런타임이 없는 PC 에서 띄우면 프로세스가 `STATUS_DLL_NOT_FOUND`(0xC0000135)로 뜨지 못하고 Windows 오류 창이 사용자 화면을 덮을 수 있다.
  그래서 띄우기 전에 `Environment.SystemDirectory\vulkan-1.dll` 이 있는지 보고, 없으면 띄우지 않고 이유 `Vulkan 런타임 없음` 으로 바로 되돌아간다. 띄울 때는 오류 창이 뜨지 않게 도구 프로세스의 오류 모드(`SetErrorMode(SEM_FAILCRITICALERRORS | SEM_NOOPENFILEERRORBOX)` — 자식이 물려받음)를 먼저 정한다(물려받는지는 WP0 이 이 PC 에서 확인 — 안 되면 다른 길을 1.4 에 적음).
  종료 코드 0xC0000135 는 이유 `Vulkan 런타임 없음(0xC0000135)` 으로 옮겨 적는다.
- **띄우는 법**: `Process` 로 직접(셸 없음, `ArgumentList`, `CreateNoWindow`, 표준 입력 닫음, 작업 폴더 = 임시 폴더). 인자 `-i <입력 폴더> -o <출력 폴더> -n <모델> -m <upscale-bin>\models -s 4 -f png`, `--upscale-gpu <n>` 이면 `-g <n>`(없으면 프로그램 기본값).
  `-m` 을 늘 준다(v0.8.1) — 도움말상 기본값이 상대 경로 `models` 라 작업 폴더(임시 폴더)나 다른 자리의 모델을 읽을 수 있다. 절대 경로로 해시를 확인한 모델 파일만 쓰게 한다.
  **묶음으로 나눠 돌린다**(v0.8.1): 확대할 판을 (작업 이름 서수, 판 이름) 순으로 늘어놓고 입력 픽셀 합이 `UpscaleBatchPixels`(도구 상수, 첫 값 16MP — WP0 실측 뒤 고침)를 넘지 않게 끊어 묶음마다 한 번 띄운다(작업마다 띄우지 않음, 동시에 하나).
  **시간 제한은 진행 기준**이다: 출력 폴더의 결과 파일 수가 `UpscaleNoProgress`(첫 값 2분) 동안 하나도 늘지 않으면 그 묶음을 죽이고 실패로 본다(v0.8 의 "한 번에 10분" 은 큰 몬스터 몇 묶음·맵·거울판만으로도 중급 GPU 에서 넘을 수 있어 첫 실행이 늘 되돌아갈 수 있었다).
  메모리 부족 같은 오류 종료는 그 묶음만 `-t 128`(작은 타일)로 한 번 다시 돌린다. 성공한 묶음의 결과는 바로 확대 중간 캐시(위 "캐시")에 넣는다.
  **실패 판정**(묶음마다): 종료 코드 ≠ 0, 진행 없음, 입력 판마다 결과 PNG 가 **없음**(이 프로그램은 그림 하나를 못 읽어도 0 으로 끝날 수 있다 — 결과 파일로만 판정), 결과 크기 ≠ 입력 × 4. 하나라도 걸리면 아래 "되돌아감".
  임시 폴더는 `d1/.cache/tmp/`(git 무시 안, 끝나면 지움 — 원본 바이트가 든 PNG 를 저장소 밖 `%TEMP%` 에 흘리지 않는다).
  끝 보고에 `확대 진척 n/m`(중간 캐시 적중 + 이번에 만든 판 / 필요한 판)과 묶음별 걸린 시간을 적는다. 첫 실행 시간은 WP0 이 이 PC 에서 재서 이 문단의 상수와 기획 19.6 의 "첫 실행 수 분" 을 고친다.
- **적용 범위**(첫 값, `upscale.json` `apply` — 기획 19.6 표): 맵 그림(바닥 조각·벽 기둥·아치 덧그림 — 미니맵 빼고)과 칸 폭 160 이상 몬스터 항목(그 항목의 모든 변종·동작 — 한 그림 묶음 안에서 배율이 섞이지 않게). 어느 묶음이 드는지 끝 보고에 적는다.
  주인공·칸 160 미만 몬스터(얼굴·손이 뭉개짐 — 기획 열린 질문 40), 마법·이펙트·아이콘·물체·UI·원작 exe PNG 는 하지 않는다. `include`·`exclude` 로 항목마다 켜고 끈다 — 몬스터는 표 항목 `out`(`d1/monsters/diablo`), 맵 그림은 `tileset:<id>`(그 타일셋을 쓰는 모든 맵 그림).
  판정 함수는 둘이다(v0.8.1, Core `UpscaleTable` — 순수): `Applies(ImportUnitEntry entry)` = (`include` 에 `out` 이 있거나 칸 폭 ≥ `monsters_min_width`) 이고 `exclude` 에 없음 — **칸 폭은 그 항목 동작들의 가장 넓은 칸 폭**(동작마다 `width` 가 다르면, 한 묶음 안에서 배율이 섞이지 않게 항목 단위로 정함),
  `Applies(string tilesetId)` = (`apply.maps` 이거나 `include` 에 `tileset:<id>`) 이고 `exclude` 에 없음(맵 그림은 표 항목이 아니라 배치에서 나오므로 그 맵의 타일셋 ID 로 판정 — 거울판 `<art>@m` 도 같은 타일셋).
- **대조는 원본 픽셀에서**: 성질 표 ↔ SOL, 칸 폭·프레임 수, `audit` 방 배열(13.5-22)은 모두 확대 전 원본에서 한다. 확대는 해독·굽기 뒤 묶기 전의 마지막 단계다.
- **처리 순서**(그림 하나 — 결정적인 부분은 도구 코드, 색 바이트만 외부 프로그램에 따라 다름):
  ① 원본 픽셀 해독(지금과 같음). 몬스터는 그림자 칸(색인 0)을 **몸과 떼어** 따로 둔다(그림자 마스크).
  ② **투명 칸 색 채우기**: 알파 0 칸의 색을 가장 가까운 불투명 칸 색으로 채운다(바깥으로 **여백 폭(16px) 이상** — v0.8.1: v0.8 의 8px 은 ③ 의 여백보다 좁아 여백 바깥 8px 에 검정이 남았다. 투명 칸의 검정이 가장자리에 번져 검은 테가 생기지 않게).
  ③ **이음매 막기**: 프레임(벽 기둥 하나)마다 원본 16px 여백(② 의 번진 색)을 두고 펼친 판을 만든다 — 이웃 프레임 색이 섞이지 않게. 바닥 조각은 이웃 바닥을 32px 더 구운 판(굽기가 배치에서 바로 그림)을 확대해 가운데만 자른다 — 조각 경계 질감이 이어진다.
  같은 그림 판(바이트가 같은 판)은 한 번만 넣는다(v0.8.1 — 같은 기둥 그림·같은 프레임이 여러 번 나와도 GPU 는 한 번).
  ④ 외부 프로그램으로 색(불투명 RGB)을 4배. **알파는 그림 종류에 따라 둘로 나눈다**(v0.8.1):
  **맵 그림**(바닥 조각·벽 기둥·아치 덧그림)의 알파는 AI 에 넣지 않고 원본 알파를 **2배 점 확대**(최근접)한다 — 바닥 마름모 가장자리와 기둥 경계는 원작에서 정확히 맞물리는 기하 모양이라 AI 알파를 문턱으로 자르면 경계가 ±1px 흔들려 바닥과 벽이 만나는 곳마다 틈·겹침이 생길 수 있다. 점 확대면 7.6 의 "1px 틈·겹침 없음" 이 그대로 선다.
  **몬스터**는 알파를 흑백 그림으로 같은 모델에 따로 4배(기획 19.6 첫 값) 하되, **프레임마다 한 번만**(변종 공통 — TRN 은 투명을 바꾸지 않으므로 알파가 같다) 확대해 모든 변종이 같이 쓴다. 그래서 변종마다 잘린 사각형이 같고 메타 하나를 같이 쓰는 규칙(1.4 변환 규칙 "잘라 묶기")이 선다.
  ⑤ `output_scale` 2 로 줄임: 2×2 상자 평균(정수). 몬스터 알파는 줄인 뒤 **0.5 에서 잘라** 있음·없음으로(계단도 후광도 없는 가장자리 — 기획 열린 질문 42).
  ⑥ **그림자**는 AI 에 넣지 않는다: ① 의 마스크를 도구 코드로 매끈하게 2배(4배 쌍삼차 → 2×2 상자 평균 → 1px 흐림, 고정 소수점 — GPU 와 무관하게 늘 같음), 검정 × 표 `shadow_alpha`(0.5)로 **몸 아래에 깔아 같은 시트에 굽는다**.
  그래서 게임은 그림자를 따로 그리지 않고 지금처럼 시트를 그린다(기획 19.6 이 설계에 맡긴 "판을 도구가 미리 굽을지" — 도구가 굽는다. 게임 쪽 그리기 경로·그리기 호출이 늘지 않음). 확대하지 않는 그림은 지금 그대로다.
  ⑦ 잘라 묶기(지금 규칙 그대로, 사각형·페이지 크기만 2배). 페이지가 4096 을 넘으면 지금처럼 다음 페이지로(끝 보고에 2페이지 이상 묶음).
- **작업 나누기 — 세 단계**(v0.8.1, `--jobs` 와의 관계 — 기획 30절이 넘긴 것): 확대 대상 작업(위 "적용 범위")만 아래처럼 나눈다. 확대하지 않는 작업은 지금처럼 한 스레드에서 끝까지 간다.
  (1) **병렬**(`--jobs`): 작업마다 해독·굽기 → ①~③ 의 확대 전 판(색 판·몬스터 알파 판) → 판마다 확대 중간 캐시 조회. 출력 캐시(위 "캐시")가 적중한 작업은 여기서 끝.
  (2) **주 스레드**: 미적중 판만 모아 같은 판을 한 번으로 줄이고 위 "띄우는 법" 의 묶음 순서로 외부 프로그램을 돌린다(GPU 는 동시에 하나 — `--jobs` 와 무관). 결과는 중간 캐시에.
  (3) **병렬**(`--jobs`): 작업마다 ⑤ 줄이기·알파 자르기·⑥ 그림자 굽기·⑦ 묶기·PNG·출력 캐시 기록.
  (2) 에서 실패하면 (3) 은 확대 대상 작업 **전부**를 확대 없이(맵 그림·몬스터 모두 원본 경로, 출력 캐시 키 `"원본"`) 돈다 — (1) 에서 이미 확대 출력 캐시가 적중한 작업도 원본으로 다시 만든다(일부만 확대된 채로 두지 않음, 아래).
  순서가 (작업 이름 서수, 판 이름)로 고정이라 `--jobs 1` 과 `--jobs 8` 의 외부 프로그램 입력·묶음이 같다.
- **되돌아감**(기획 19.6 — 확대를 못 해도 가져오기는 끝난다): 실행 파일·모델이 없음, 해시 다름, Vulkan 런타임 없음(v0.8.1), 띄울 수 없음·오류 종료(Vulkan 장치 없음 포함)·진행 없음, 결과 파일 없음·크기 틀림 — 어느 것이든 **확대를 모두 버리고** 그 실행의 모든 그림을 원본 픽셀로 만든다.
  일부만 확대된 채로 두지 않는다(바닥 조각 반만 확대된 화면이 되지 않게 — 헬파이어 묶음의 "전부 있거나 전부 없다" 와 같은 뜻). 그래서 manifest 의 `status` 는 `off`·`done`·`fallback` 셋뿐이다.
  끝 보고 맨 끝에 `경고: AI 확대를 하지 못해 원본 픽셀로 만듦 — <이유>` 한 줄, manifest `reason`. **종료 코드 6**(경고와 함께 성공 — Assets 는 온전하고 게임이 돈다. 확대를 원한 사용자·스크립트가 0 과 가른다). `import-all.ps1` 은 0 과 6 을 성공으로, 6 이면 그 경고 줄을 다시 보인다.
- **manifest `upscale`**(Core `AssetManifest.Upscale` = `ManifestUpscale`, v0.8): `{ "status": "off|done|fallback", "model": "realesrgan-x4plus", "tool": "realesrgan-ncnn-vulkan v0.2.5.0", "files": { "<상대 경로>": "<sha256>", … }, "output_scale": 2, "gpu": "default|<n>", "reason": "…" }`.
  확대한 파일의 줄에 `"scale": 2`. 픽셀이 같다는 것은 요구하지 않는다(위 "결정론" 예외 — 파일 크기·해시는 그 PC 의 섞임·깨짐을 보는 시작 검사·`verify` 용이고 PC 사이에 견주는 값이 아니다).
  `gpu` 는 이번 옵션 값이 아니라 **만든 때의 값**이다(v0.8.1 — 중간 캐시가 판마다 기록한 `gpu`. 캐시 적중이면 이번 실행은 GPU 를 돌리지 않았으므로). 판마다 다르면 `"mixed"`. 첫 판은 `default` 또는 번호 문자열.
- **출력 모양**(형식 판 `AssetFormat.Version` 2 → 3, v0.8): 그림 메타(6.3 `SpriteMeta`, 형식 3)에 `scale`(그림 픽셀 / 월드 px — 1 또는 2, 기본 1). 메타의 `cell`·`rects`·`ox,oy` 는 모두 **그림 픽셀**이고 그리는 쪽이 `scale` 로 나눈다.
  프레임 수·방향·동작은 배율과 무관 — 정의와의 대조(13.5-7)는 칸 크기를 `scale` 로 나눠 본다. 게임 정의(`sprite_set`·`map`·`tileset`)는 확대를 모른다.
  `arena.json` 에 `scale`(형식 2 — 바닥 조각 `512 × scale` 그림 픽셀, `pos` 는 월드 px 그대로), 기둥 아틀라스 메타도 `scale`. 형식 판이 올라가므로 옛 자원은 시작 검사 ① 이 "자원 형식이 낡았다" 로 막는다.
- **시험**(10.1 `Pipeline`, 자원 없이 — 기획 19.6 "가짜 확대 프로그램"). v0.8.1 에 두 겹으로 나눴다:
  **가짜 실행 파일**: 시험이 빌드하는 작은 콘솔 프로그램(`tests/FakeUpscaler` — 최근접 4배, 실제 프로그램과 같은 인자 `-i -o -n -m -s -f -g -t` 를 받음, 환경 변수로 "오류 종료·잠자기(진행 없음)·결과 하나 빼기·크기 틀림·0xC0000135 흉내" 를 고름)로 **`Process` 경로 전체**를 본다 —
  인자 목록·셸 없이 띄우기·`-m` 경로·종료 코드 판정·진행 없음 → 죽이고 실패·`-t 128` 로 한 번 다시·결과 파일 없음 → 실패·묶음 나누기·해시 확인 단계. 시험은 확대 표(허용 해시)를 **주입**한다(도구의 표 읽기가 시험용 `UpscaleTable` 을 받는 내부 진입점 — 가짜 실행 파일의 해시로 바꿔 낌. 실제 `Table/upscale.json` 은 바꾸지 않는다).
  **`IUpscaler` 가짜**(픽셀을 4배로 늘리기만): 처리 ①~⑦ 단위 시험 — 크기·여백·채우기 반경·같은 판 한 번·맵 알파 점 확대(바닥·기둥 경계 줄이 원본 2배와 같음)·몬스터 알파 프레임마다 한 번(변종 사각형 같음)·알파 0.5 자르기·그림자 굽기·메타 `scale`·캐시 키(GPU 를 바꿔도 적중)·**`--jobs 1` = `--jobs 8`**(확대 경로 포함)·**일부 중간 캐시 적중 + 나머지 실패 → 전부 원본**(적중한 판은 중간 캐시에 남음)·다음 실행이 남은 판만 돌림.
  되돌아감(없음·해시 다름·Vulkan 런타임 없음·결과 파일 없음·결과 크기 틀림·진행 없음 → 모두 원본 + 종료 코드 6 + `status: fallback` + 출력 해시가 확대를 끈 실행과 같음), 모르는 모델 → 2, `--upscale` 없으면 확대 프로그램 폴더를 읽지 않음을 본다.
  실제 프로그램은 이 PC 에서 WP0 완료 기준으로 한 번(11.1 WP0 v0.8 — 기획 19.6 확인 항목 ①~⑧ 의 견본을 만듦).
- **`upscaler-check`**(v0.8.1): 해시 확인 + Vulkan 런타임 확인 뒤, 임시 폴더(`d1/.cache/tmp/`)에서 도구가 만든 작은 무늬 그림(원작 바이트 아님, 16×16) 한 장을 변환해 종료 코드·결과 크기와 표준 오류의 장치 줄(GPU 이름)을 보인다. 이 프로그램에는 GPU 목록만 내는 옵션이 없어서다. `--upscale-gpu` 를 주면 그 장치로. Assets 는 만들지 않는다.

**실패와 보고**
- 시작할 때 `d1/manifest.json` 을 먼저 지우고, 표의 모든 항목을 만든 뒤에만 쓴다 → 중간에 실패하면 빌드·실행이 "자원 없음" 으로 본다(반쯤 채운 Assets 로 뜨지 않음, 기획 19.2).
- 파일은 임시 이름으로 쓰고 바꿔치기한다. 새 manifest 에 없는 `d1/` 아래 파일은 지운다(`d1/` 밖은 건드리지 않음).
- 종료 코드: 0 성공(묶음 MPQ 가 없거나 쓸 수 없어 그 묶음을 건너뛴 것 포함 — v0.6.1), 1 처리 안 된 예외, 2 원본을 못 찾음·못 엶(`diabdat.mpq`, 또는 `--hellfire` 로 직접 준 폴더의 열리지 않는 묶음 MPQ),
  3 표에 있는데 원본에 없는 파일(목록 전부 — 기본 항목, 또는 `--hellfire` 로 직접 준 묶음 MPQ), 4 대조 실패(성질 표·프레임 범위·형식, v0.6.1: `audit` 의 DUN 배열 같음 포함),
  (v0.8) 6 성공이지만 요청한 AI 확대를 못 해 원본 픽셀로 만듦(위 "AI 확대 — 되돌아감"). 모르는 확대 모델 이름은 2.
- 끝 보고(표준 출력 + `d1/import.log`): 종류별 만든 수·크기, 캐시 적중 수, 걸린 시간, 없는 파일 목록, 원본 SHA-256, (v0.6) 묶음마다 `만듦`/`건너뜀(이유)` 한 줄,
  (v0.6.1) 쓸 수 없어 건너뛴 묶음은 맨 끝에 `경고:` 줄(위 "헬파이어 묶음") — 종료 코드만 보는 `import-all.ps1` 도 이 줄을 그대로 보인다.
- 캐시 키(v0.6): 원본 항목 바이트 해시가 이미 키에 있으므로 같은 이름을 hfvoice → hfmonk 로 바꿔 읽으면 키가 달라져 다시 만든다. 건너뛴 묶음의 옛 출력은 "새 manifest 에 없는 `d1/` 아래 파일은 지운다" 규칙으로 지워진다.

**원작 exe 가져오기와의 관계** — `tools/import-assets.ps1`(원작 exe PNG 49장)은 그대로 둔다. 리소스 Bitmap 을 되살리려면 Windows PowerShell 5.1(.NET Framework)이 필요해서
.NET 9 도구로 옮기지 않는다. `tools/import-all.ps1` 이 exe → diabdat 순서로 둘을 부른다(사용자가 두 번 실행하지 않게, 기획 19.2). exe PNG 는 정의에서 `source: "grid"`(6.3)이고
패시브 아이콘·획득물·타이틀·로고·커서에만 쓴다(기획 19.3).

**빌드 검사·실행 검사와의 관계**
- 빌드: DDR0001(1.3) — manifest 와 `TITLE.png` 가 있는지만.
- 실행 시작(WP7 `AssetCheck`): manifest 를 읽어 ① `format` ≠ Core `AssetFormat.Version` 이면 "자원 형식이 낡았다(판 a → b). 가져오기를 다시 돌린다",
  ② manifest 의 표 해시 ≠ 출력 `ImportTable\` 로 복사된 표의 해시면 "가져오기 표가 바뀌었다 — 가져오기를 다시 돌린다"(git pull 로 칸 폭·그림자 값이 바뀐 경우 — 이름은 같아 ③ 이 못 잡는다),
  ③ 정의가 가리키는 파일(시트·변종·메타·맵 그림·소리·음악·소리 색인)이 manifest 에 없으면 그 목록 — **단 v0.6: 그 이름을 만드는 표 항목의 묶음이 manifest `skipped_bundles` 에 있으면 오류가 아니라 "꺼짐"**
  으로 센다(기획 19.4. 이름 → 묶음은 출력 `ImportTable\` 의 **`ImportTable.OutputBundles(arenas)`** — v0.6.1: 맵 그림 이름은 표 항목이 아니라 배치에서 계산되므로 펼친 `ArenaGrid` 를 받아 그 타일셋 항목의 묶음에 잇는다).
  꺼짐은 안내 화면을 띄우지 않고, `ContentAvailability`(6.17)가 그 묶음을 `requires` 로 단 스테이지·직업을 끈다.
  **교차 확인**(v0.6.1): "꺼짐" 으로 센 파일마다 그 파일에 닿는 정의(`DefAssetNames.For` 의 주인 D, 6.14 13.5-10)를 구하고, 그중 하나라도 **켜진** 스테이지·직업에서 닿으면(켜진 스테이지의 적 = `StageReach.Enemies`,
  켜진 직업·스테이지의 무기·음악 — `fallback` 이 있는 음악은 대체되므로 빼고) 꺼짐이 아니라 오류다(`기본 스테이지 stage_cathedral 이 꺼진 묶음 hellfire 의 파일 d1/… 을 씀`). 13.5-10·13.5-20 이 빈틈없으면 이 경우는 없다 — 그 둘이 놓친 것을 시작 때 잡는 그물이다.
  꺼진 묶음이 아닌데 없는 파일(기본 내용, 또는 만든 묶음의 파일)은 지금처럼 오류다. ④ manifest 의 파일마다 **크기**가 디스크와 다르면 그 목록(해시보다 싸다 — 섞인 출력 폴더, 1.3),
  ⑤ 맵 `arena.json` 의 `layout_hash` 가 저장소 배치·타일셋으로 계산한 값과 다르면 "맵 배치가 바뀌었다 — 가져오기를 다시 돌린다".
  하나라도 있으면 타이틀 대신 **안내 화면**(DataError 화면, 무엇이 없는지 + 명령 한 줄, 기획 19.3)을 띄운다.
  "꺼짐" 만 있으면 타이틀로 가고, **꺼진 스테이지·직업이 하나라도 있으면** 타이틀 아래 한 줄(기획 12.5 — `헬파이어 자원 없음 — 수도사·지하실 꺼짐`, 8.1. v0.6.1: 문구의 앞말은 `skipped_bundles` 의 이유로 고른다 —
  `없음`·`--skip-bundles` 는 "없음", `열 수 없음`·`판 다름` 은 "읽지 못함(<MPQ 이름>)", 문구 자체는 기획 12.5). 음악만 대체되는 경우(`hfmusic` 만 없음)는 꺼지는 것이 없으므로 한 줄을 띄우지 않고 로그 `AUDIO music=… fallback=…` 만 남긴다.
  로그 `ASSETS` 줄에 `bundles=… skipped=… disabled=…`(9절 — `disabled` 는 꺼진 스테이지·직업 ID). v0.8: `upscale=<off|done|fallback> art_scale=<1|2>`(7.1 — `done` 이면 2).
  확대 결과(`fallback` 포함)는 오류도 꺼짐도 아니다 — 안내 화면·타이틀 한 줄을 띄우지 않는다.
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
| 원작 무작위 층 생성기·DUN 방 그대로 쓰기 | 방 맵은 손으로 짠 고정 배치이고 씨앗은 어느 맵을 쓸지만 고른다(v0.7, 기획 10.8 — v0.6 까지의 "넓은 방 하나" 는 v0.7 에서 방 여럿으로 바뀜). DUN 파일 자체는 옮기지 않는다(사용자 결정 — 기획 23절 B: 사실 수치만 손으로 적고 표·코드·DUN 은 옮기지 않음, R24). 우리가 쓴 배치 데이터로 만든다(6.11) |
| 원작 SOL·마름모 격자로 직접 길찾기 | 칸 하나가 화면 64×32 라 반경 9~34 몸 크기를 나누기에 거칠고, 마름모 격자용 플로우·A*·clearance 를 새로 짜야 한다. 16px 격자 + 평행사변형 도형이면 v0.2 의 4.2~4.4 가 그대로다(4.2) |

---

## 3. 게임 루프

### 3.1 시간 모델

- **틱 = 1/60초 고정**(`World.Dt`). 시뮬레이션의 모든 속도·시간은 "초" 단위 실수이고 틱마다 `Dt` 를 곱한다.
- 시계는 셋이다.

| 시계 | 무엇 | 멈추는 때 |
|---|---|---|
| 앱 틱 `AppTick` | 프로그램 시작부터 센 틱. 메뉴·오버레이·연출도 이것으로 진행. `--capture-at`, `--quit-after` 의 기준 | 안 멈춤 |
| 스테이지 틱 `World.Tick` / `StageTime` | **방 하나**의 게임 시간(v0.7 — `World` 가 방마다 새로라 방마다 0 부터, 3.6). 표시 이벤트·개체 `SpawnTick`·시야 바뀜의 기준. 이름은 그대로 둔다(코드 전체가 쓰는 이름) | 일시정지, 레벨업·상자·보상 창, 히트 스톱 |
| 방 시계 `World.Room.ClockTicks` (v0.7) | 그 방의 들어옴·생존·정리 단계 동안 흐른 스테이지 틱. 방 이벤트 시각·시작 유예·정리 안전장치·HUD 남은 시간·`--start-at` 의 기준 | 위 + 보상·문 단계 |
| 난이도 분 t `World.DifficultyMinutes` (v0.7) | 방 t 시작 + 방 시계(분) — **일반·정예 방은 `to` − 1틱(1/60초)에서 멈추고** 정리 중에는 오르지 않음(정예 방 v0.7.1, 일반 방 v0.7.2 — 구간 `[from, to)` 조회가 다음 방 구간으로 넘어가지 않게, 기획 v0.8.1 2.5), 보스 방은 안 멈춤(기획 2.5). 생성 구간·배율·정예 판정의 기준. 방 없는 시험 월드는 `StageMinutes` | 방 시계와 같음 |
| 판 걸린 시간 `RunState.ElapsedTicks` (v0.7) | 방마다 끝난 `World.Tick` 의 합(멈춘 시간 빼고) — 다음 방으로 갈 때(`RunSession.Advance`)와 판이 끝날 때(`RunSession.Conclude`, v0.7.1 — 마지막 방·진 방 포함) 더함. 결과 화면·기록 | 위 |
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
| (v0.7) 마법서·성소 보상 창 | 레벨업 창과 같다 — `EndTick` 이 `AwaitingChoice = Reward` 를 켜고 같은 앱 틱에 보상 창(레벨업 오버레이)을 올린다. 대기 순서는 레벨업 → 상자 → 보상(3.6) |
| (v0.7) 방 나감 → 다음 방 0.5s | `StageStepper.AppTick` 이 `World.Room.Phase == Exit` 이고 `Outcome == Running` 이면(v0.7.2 — 끝나 가는 판은 돌아서 끝남) 돌지 않고 `StageStep.RoomExit` 를 돌려준다. `PlayScene` 이 앱 틱으로 0.5s 어두워짐을 세고, 가운데서 `RunSession.Advance()` 로 새 방 `World` 를 받는다. 시뮬레이션은 그동안 돌지 않는다. **판정 순서**(v0.7.1, 계약): `Finished`(`Outcome` 이 `Victory`·`Defeat`) → `AwaitingChoice` → `RoomExit` → 히트 스톱·늘임 → 한 틱. 같은 틱에 나감과 선택 대기가 함께 생겨도 창이 먼저 닫힌다(`RoomFlow` 는 대기가 남아 있으면 `Exit` 로 가지 않지만 — 3.6 — 순서로도 한 번 더 막는다). 새 `World` 를 받기 전에 `PlayScene` 이 옛 월드의 표시 이벤트를 소비·`Clear` 하고, 받은 뒤 게임 쪽 "월드 바뀜" 을 정해진 순서로 부른다(7.11 `IWorldBound`) |
| (v0.7.1) 판 끝 | `StageStep.Finished` 를 처음 받은 앱 틱에 `PlayScene` 이 `RunSession.Conclude()` 를 한 번 부른다(포기 = 패배 처리 뒤 같은 길). 마지막 방의 `RoomRecord`·`ElapsedTicks`·공정성·`INNER_ROOMS` 줄이 여기서 더해진 뒤 결과 화면·`RESULT`·저장이 `RunState` 를 읽는다(3.6 결정 4) |

### 3.4 결정성 규칙

- 난수는 `SimRandom`(xoshiro128\*\*, 씨앗은 SplitMix64) 하나에서 용도별 흐름을 나눈다: `Spawn`, `Drop`, `LevelUp`, `Ai`, `Weapon`, `Rank`.
  한 용도에서 난수를 한 번 더 뽑아도 다른 용도의 결과가 바뀌지 않아 버그를 좁히기 쉽다.
- **(v0.7) 판 씨앗과 방 씨앗**: `--seed` 는 **판 씨앗**이다. 방 `World` 의 `SimRandom` 은 **방 씨앗** `RunSeeds.Room(판 씨앗, 액트, 방 번호)` 으로 만들고(생성·드롭·카드·AI 의 난수 흐름이 앞 방의 난수 소비와 무관하게 그 방에서 새로 시작),
  판 흐름(방 맵·들어오는 문·출구 문 수·문 보상 후보 순서)은 **판 흐름 씨앗** `RunSeeds.Flow(판 씨앗, 액트, 방 번호)` 으로 `RunPlanner` 가 정한다(3.6). 두 줄기는 서로를 건드리지 않는다. 방 번호는 보이는 번호 `RoomDef.Number`(v0.7.1 — `slice` 판도 기획 번호, 3.6).
  같은 판 씨앗·같은 플레이(고른 문·카드·움직임)면 판 전체가 같고, `--room` 으로 그 방부터 돌려도 방 맵·들어오는 문·방 씨앗이 같다(기획 2.5 "결정성").
  **같은 짐·같은 입력**이면 같은 번호의 방은 처음부터 돈 판이든 `--room` 이든 같다 — 생성 자리·수는 주인공 위치·살아 있는 적 수에 달려 있으므로 "앞 방에서 무엇을 했든 같은 생성" 은 아니다(v0.7.1, 기획 2.5 문장도 같은 뜻으로 고침).
- **표시용 시간·난수도 틱으로 센다.** 게임 쪽에는 프레임마다 뽑는 상태 난수가 없다. 표시 값은 모두 "틱과 ID 로 계산하는 함수"다:
  흔들림 오프셋 = `StableHash(seed, stageTick)`, 점 빛 깜빡임 = `StableHash(seed, lightId, stageTick)`, 장식 = `StableHash(seed, 개체 ID)`,
  장판 조각 자리 = `StableHash(장판 ID, i)`(6.4). 바닥 타일 섞기는 로딩 때 `StableHash(맵 ID, mx, my)`(6.11).
  표시 이벤트(이펙트·시체·피해 숫자·신호)는 **생긴 스테이지 틱**을 싣고, 그릴 때 "나이 = 현재 틱 − 생긴 틱" 으로 계산한다(진행량을 누적하지 않는다).
- 소리의 무작위(파일 고르기·높낮이)는 게임 쪽 난수이고 시뮬레이션 난수와 따로다. 소리는 결정성 대상이 아니지만 시뮬레이션을 읽기만 하므로 결과를 바꾸지 않는다(7.9).

| 표시 요소 | 시계 |
|---|---|
| 월드 안 이펙트·시체·피해 숫자·흔들림·점 빛 깜빡임·맥동·정예 맥동·발광 번쩍임·광폭화 맥동·장판 조각, 안개 칸 바뀜 0.15s(7.10) | 스테이지 틱(멈추면 같이 멈춘다) |
| 주인공을 가리는 벽 반투명(7.6) | 그 프레임의 보간된 주인공 위치 |
| 타이틀 로고·메뉴·오버레이 애니메이션(레벨업 카드 올라옴, 빛 기둥), 음악 페이드·덕킹 | 앱 틱 |
| 마우스 커서 위치 | 실제 입력(캡처에는 그리지 않는다) |

- **데이터 순서**: `Data/**/*.json` 은 경로를 서수(ordinal) 정렬해 읽는다. JSON 객체로 적은 가중치·패치는 로딩 때 **JSON 에 적힌 순서의 배열**로 바꾼다(`weights` → `(id, weight)[]`).
  정의 목록 순서는 (파일 순서, 파일 안 순서)로 고정된다.
- 시뮬레이션에서 `Dictionary`/`HashSet` 순회 결과에 의존하지 않는다(저장소는 배열 순서, 사전은 찾기에만). `Parallel`·스레드 없음. 벽시계 읽지 않음.
- 같은 PC·같은 빌드 구성에서 같은 씨앗이면 같은 결과를 보장한다(다른 CPU 간, Debug↔Release 간 부동소수 일치는 보장하지 않는다 — 해시 비교는 같은 구성끼리만).
- 재현 정보는 로그 첫 줄에 모은다: `seed=… config=Release|Debug commit=<있으면> data=<Data 내용 해시> args="<전체 명령줄>"`. `--seed` 가 없으면 시각에서 씨앗을 만들어 여기 적는다.
- 확인 수단: Core 의 상태 해시(`StateHasher`: 위치·HP·처치·레벨·난수 흐름 상태·개체 수·**가 본 칸**(v0.5.1 — `VisibilityField.ExploredHash`, 비트맵 1,600~1,936비트의 `StableHash`))를 시험과 게임(`--log-hash`)이 같이 쓴다.
  가 본 칸은 개수가 아니라 집합을 넣는다(개수가 같고 집합이 다른 경우를 잡게). 시야 안 칸(`InSight`)은 넣지 않는다 — 해시된 주인공 위치·빛 반경과 맵에서 다시 나오는 값이다(4.11). 자동 조작의 레벨업 선택은 Core `AutoplayPolicy` 하나라
  Core 시나리오 시험의 해시와 게임 실행의 해시를 같은 스테이지 틱에서 비교할 수 있다.
  v0.7: 해시에 **판 흐름 상태**(액트·순번·들어온 문·지금까지 고른 문·다음 방 보상 ID)와 **방 상태**(`Room.Phase`·`ClockTicks`·출구 문)를 넣는다(WP7).
  **v0.7.1 — 해시 둘**: `StateHasher.Room(World)` = **방 해시**(그 `World` 의 상태만 — 위 목록에서 `RunStats` 를 빼고 방 상태를 넣음. `Room.Reward` 는 앞 방에서 고른 결과라 넣지 않는다),
  `StateHasher.Run(RunState, World)` = **판 해시**(방 해시 + `RunStats` + 판 흐름 상태). `--room` 으로 시작한 방과 처음부터 돈 판의 그 방은 판 누적이 다르므로 **방 해시로** 비교한다(10.1 `Run`). 판 두 번 비교(`Scenario`·10.2 "재현")는 판 해시.
  `--log-hash` 의 행은 `act, room, stage_tick, room_hash, run_hash` 로 방마다 틱이 0 부터다(9절 — 앱 틱은 넣지 않는다, `--time-scale` 비교가 흐려지지 않게).

### 3.5 시스템 실행 순서 (스테이지 틱 하나)

`Simulation` 이 `SimPhase` 순서대로 `ISimSystem.Tick(World)` 를 부른다. **한 단계 = 시스템 하나**(두 번째 등록은 `SimulationBuilder` 가 예외). 순서는 이 표가 정본이다.

| # | 단계 `SimPhase` | 하는 일 | 담당 |
|---|---|---|---|
| 1 | `BeginTick` | `PrevPos ← Pos`, 틱 이벤트 큐 바꿔 끼우기(아래), 시계 진행, 버프·무적·번쩍임 타이머 감소, 화면 사각형(`ViewRect`), **시야 갱신**(`VisibilityField.Update`, 4.11 — v0.5) | WP1(시야는 WP1c) |
| 2 | `Director` | 방 이벤트(v0.7 — t → 방 시계, `RoomEvents`), 일반 생성 누적기, 정예 판정(v0.7: 일반 방마다 한 번), 상한 정리(먼 적 `Culled`). `World.Outcome` 이 `Running` 이 아니거나 방이 생성 단계가 아니면(`Room.SpawningOpen` 거짓 — 유예·정리·보상·문, 정예 방 2:30 뒤) 생성·이벤트를 안 함 | WP4 |
| 3 | `HeroControl` | 명령 → 주인공 이동 의도·바라보는 방향, 체력 재생, 애니메이션, 레벨업 밀어내기 요청 | WP3 |
| 4 | `Navigation` | 플로우 필드 갱신(분산), A* 요청 처리(틱당 최대 2건) | WP2 |
| 5 | `EnemyThink` | 생애 상태, 이동 지시, 부품(오라·능력·위치 잡기 — 원거리·돌진은 **또렷이 보일 때만**, 6.6) → **이동 의도**·독점 행동, 애니메이션, 타격·발사 프레임 → 피해 요청·투사체 생성, 표시 상태(`Visual`) | WP4 |
| 6 | `Locomotion` | 공간 해시 짓기 → 이동 의도를 희망 속도로(`Navigator`) → 분리 조향 → 속도 적분(넉백 포함) | WP2 |
| 7 | `Collision` | 적-적 원 해소(2회), 주인공-적 규칙(닿은 수), 장애물·맵 경계 밀어내기 | WP2 |
| 8 | `SpatialRebuild` | 이동이 끝난 위치로 해시 다시 짓기(무기 조준·맞음 판정용), 60틱마다 불변식 계측(`--check`·시나리오, 10.1) | WP2 |
| 9 | `Weapons` | 쿨다운, 조준, 발사(투사체·장판 생성), 회전 구체 위치 | WP3 |
| 10 | `Projectiles` | 이동·유도, 장애물 막힘, 맞음 판정 → 피해 요청, 폭발 | WP3 |
| 11 | `Hazards` | 장판·지연 폭발·예고 진행 → 피해 요청 | WP3 |
| 12 | `DamageResolve` | 피해 요청 적용(방어·치명·무적·배율), 넉백, 번쩍임, `HitLanded`·피해 숫자 표시 이벤트, HP ≤ 0 → `Dead`(`KillCause.Damage`) | WP3 |
| 13 | `DeathHooks` | 죽은 적: 진행 중 행동 끊기, 죽음 훅(분열·장판·폭발), 호위 공포, 보스 처치 → 승리 처리(아래), `EnemyKilled` 틱 이벤트·`CorpseSpawned` 표시 이벤트 | WP4 |
| 14 | `DeathRewards` | `EnemyKilled` 중 `Cause == Damage` 만: 보석·드롭·처치 수·`RunStats` | WP3 |
| 15 | `Pickups` | 끌어당김·줍기, 경험치 → 레벨업 대기, 물약·룬·상자, (v0.7) 문 보상 물체 → 보상 대기·효과, 방 정리 때 보석·금화 끌어당김(`RoomCleared`) | WP3 |
| 16 | `RoomFlow` (v0.7) | 방 흐름 상태 기계(3.6): 방 시계 진행, 들어옴 → 생존 → 정리 → 보상 → 문 → 나감 전이, 정리 안전장치(무너짐), 보상 물체 놓기, 출구 문 열기(보상 거르기), 나가는 자리 판정. 방 없는 월드(`World.Room == null`)는 아무것도 안 함 | WP1e(그 뒤 WP4) |
| 17 | `EndTick` | 승패 판정 타이머, 승리 대기·(v0.7) 방 보상 단계 이후면 적 투사체·장판·예고 지우기, 레벨업·상자·(v0.7) 보상 대기 → `AwaitingChoice`, 저장소 압축(지운 칸), 디버그 빌드 불변식(`float.IsFinite(Pos)`) | WP1 |

- **틱 번호**(WP1 코드): `World.Tick` 은 단계들이 도는 동안 "이번 틱 번호"(첫 틱 0)이고 `EndTick` 끝에서 1 늘어난다. 틱 중에 생긴 개체는 `SpawnTick = Tick` 을 받아 저장소 끝에 붙고
  **`SpawnTick < Tick` 인 틱부터** 행동한다(= 다음 틱부터). 지우기는 표시만 하고 17(v0.6 까지 16)에서 한꺼번에 압축한다. 밀집 인덱스는 압축 전까지만 유효하다.
  v0.7 에 `RoomFlow` 를 `EndTick` 바로 앞에 끼웠다 — 그 틱에 생긴 죽음·줍기·분열을 다 본 뒤 단계를 바꾸고, 같은 틱의 `EndTick` 이 바뀐 단계로 적 공격 지우기·선택 대기를 정한다.
  `EndTick` 이 맡던 승패 판정에 넣지 않은 이유: 방 흐름은 감독(WP4)·성장(WP3)·앱(WP6)이 모두 읽는 상태라 따로 시험할 순수 상태 기계로 두고(3.6), WP1 의 틀 단계는 작게 둔다.
- **신호 `CueEvent`**(7.9)는 그 일이 일어난 단계가 표시 이벤트로 낸다(무기 발사 9, 적 맞음 12, 적 공격 시작 5 …). 시뮬레이션 상태를 바꾸지 않고 다른 단계가 읽지 않는다.
- **죽음 이유 `KillCause`**: `Damage`(보통 처치 — 훅·보석·드롭·처치 수·시체), `Collapse`(승리 때 무너짐 — 시체만, 훅·보상·처치 수 없음), `Culled`(상한 정리 — 아무것도 없음, 시체도 없음).
- **틱 이벤트 큐는 두 칸**이다: 이번 틱에 쓴 이벤트는 같은 틱의 뒤 단계와 **다음 틱 전체**에서 읽을 수 있다(`Events.Current` / `Events.Previous`). `BeginTick` 이 바꿔 끼우고 오래된 칸을 비운다.
  예: `DamageResolve`(12) 가 낸 `HitLanded` 를 다음 틱 `EnemyThink`(5) 가 읽어 흡혈을 처리한다(1틱 늦음 허용).
- **표시 이벤트(`PresentationBuffer`)는 틱마다 비우지 않는다.** 각 항목에 생긴 스테이지 틱을 싣고, 프레임 끝에 `PlayScene`(WP6)이 `FxSystem.Consume` → `Audio.Consume` 순서로 소비한 뒤 `Clear` 한다.
  한 프레임에 여러 틱이 돌아도 앞 틱의 이벤트가 사라지지 않는다. 그래픽 없는 실행(시험·시나리오)은 `Presentation.Enabled = false` 로 쌓지 않는다.
- 해시를 6 과 8 에서 두 번 짓는다. 300~500 개 재배치는 수십 µs 라 한 번 짓고 낡은 값을 쓰는 것보다 단순하고 정확하다.
- **승리 처리**(기획 2.2): `World.BeginVictory()` = `World.Outcome = VictoryPending` → 살아 있는 적 전부 `Dead`(`Collapse`, `World.CollapseEnemies()`) → `EndTick` 이 적 투사체·장판·예고를 지운다. 그 뒤 `Director` 는 아무것도 하지 않고
  `DamageResolve` 는 주인공 피해를 무시한다. 같은 틱에 주인공 HP 도 0 이 되었으면 승리가 먼저다. 3초(스테이지 틱) 뒤 결과.
  **누가 부르나**(v0.7): 방 월드에서는 `RoomFlow` 하나다 — **판의 마지막 방**(`RoomState.IsLastInRun` — `RunSession` 이 `RoomStart` 로 넣음)이 끝나는 순간: 보스 방이면 보스 처치(`BossDefeated` 틱 이벤트), 일반 방이면(단계 1 세로 조각처럼 다음 방이 없음) 정리 끝.
  마지막이 아닌 액트 보스는 승리가 아니라 무너짐 + 보상 단계다(3.6). `DeathHooks`(WP4)는 방 월드에서 `BossDefeated` 만 내고 `BeginVictory` 를 부르지 않는다. 방 없는 시험 월드(`World.Room == null`)에서만 v0.6 처럼 `DeathHooks` 가 보스 처치 때 부른다.
  v0.6 의 `victory.type == survive`(그 시각 생존 승리)는 없앴다 — 단계 1 의 끝은 "마지막 일반 방 정리" 다(기획 2.2·14.1).

### 3.6 판·액트·방 — `RunPlanner`·`RoomFlow`·`RunSession` (v0.7, 사용자 결정 R, 기획 2.5·10.8·11.4)

```
판  RunState      씨앗·직업·시작 액트·지금 액트/순번·빌드 짐(HeroCarry)·누적 RunStats·공정성 합·방 기록·다음 방 보상
 └ 액트 StageDef  방 순번 표 rooms·방 맵 풀 room_maps·문 보상 rewards·next_act·난이도 구간표·방 이벤트(6.12)
    └ 방 RoomPlan  맵·들어온 문·출구 문·보상 후보 순서·방 씨앗  →  World 하나 + Simulation 하나(World.Room = RoomState)
```

**결정 1 — 방마다 `World` 를 새로 만든다**(한 `World` 안에서 맵을 갈아끼우지 않는다).

- `World` 생성자가 이미 맵에 묶인 것(`Arena`·`Resolved`·`Rooms`·`MapSize`·`Lighting`·`Visibility`)을 한 번 만들고 모두 get-only 다. WP2 의 내비 격자·clearance·플로우 필드·공간 해시·`ObstacleIndex`·충돌 도형, `VisibilityField` 의 가 본 칸,
  WP4 감독 상태가 "월드 수명 = 맵 수명" 을 가정한다. 맵만 갈면 이 모두에 되돌리기 경로가 생기고, 하나라도 빠뜨리면 앞 방의 필드·가 본 칸·밀집 인덱스가 새 방으로 새는 조용한 버그가 된다.
- 방을 나가면 적·투사체·장판·예고·획득물(남은 물약·보급 포함)이 모두 사라져야 한다(기획 11.1) — 새 `World` 면 저절로다. 가 본 곳 기억 지움(기획 10.7·10.8)도 저절로다.
- `World.Tick` 이 0 부터 = 방 틱이라 `SpawnTick` 규칙(3.5)·표시 이벤트의 "생긴 틱"·시야 첫 계산(Tick 0, 4.11)이 고칠 것 없이 선다.
- 방 씨앗으로 `World` 를 만들면 "앞 방 플레이와 상관없이 같은 순번 방의 생성이 같다"(기획 2.5)가 구조로 선다.
- 값: 방마다 저장소 풀·격자 할당과 길 계산(4.2 clearance 수 ms, 필드). 방 이동은 0.5s 어두워짐(앱 틱, 시뮬레이션 밖) 안이고 한 판에 20번이라 틱 중 할당 0(4.9)과 무관하다. 시간은 WP2 가 잰다(R33).
- 잃는 것은 방 사이에 이어져야 하는 상태뿐이고, 그것을 아래 표로 **명시적으로** 넘긴다.
- **시간 효과 시계**(v0.8, 기획 v0.8.1 2.5): 주인공의 시간 효과(버프·마력 폭주 — WP3)는 **생존·정리 단계에서만 흐른다**. 보상·문 단계, 방 넘김(시뮬레이션 밖), 다음 방의 들어옴(유예) 동안은 멈춘다 — 정리 끝에 먹은 마력 폭주가 다음 방 첫머리에 쓰인다.
  판정은 한 곳 `World.TimedEffectsRun`(= `Room is null || Room.Phase is Survive or Cleanup`, 계약 WP1f)이고 WP3 가 시간 효과를 줄이는 자리에서 읽는다. 그래서 효과의 끝은 절대 틱(`EndTick`)이 아니라 **남은 틱**으로 센다(멈춘 동안 줄지 않게 — WP3 조각). 짐에는 남은 틱을 그대로 넣는다.

| 넘긴다 (`RunState` — 판 수명) | 방마다 새로 (`World` — 방 수명) |
|---|---|
| 주인공 빌드 `HeroCarry`: 레벨·경험치·재추첨 남은 수·체력·무기 칸(정의·레벨·진화)·패시브 칸·축복 칸 — WP3 `IHeroCarry` 가 `Capture`/`Restore`. 칸 목록은 **칸 번호 순서 그대로**이고 `Restore` 는 같은 번호 칸에 놓는다(v0.7.1 — `RunStats.DamageByWeapon[칸]` 이 무기를 칸 번호로 세므로). 대기(레벨업·상자·보상)는 넘기지 않는다 — 나갈 때 늘 0 이다(3.6 `Doors` 줄, `Capture` 는 0 이 아니면 예외). **v0.8**(기획 v0.8.1 2.5 "방 사이에 이어지는 것"): + 열린 칸 수(무기·패시브 — 계단에서 +1), 허리띠 물약 수, 판 끝까지 가는 능력치(`단련` 쌓임·성소 대가 — `Mods`), 받은 성소(`Shrines` — 한 판에 한 번), **시간 효과의 남은 틱**(`Effects` — 마력 폭주 등) | 맵·`Arena`·`Resolved`·`InnerRooms`(v0.7.1 이름 — 맵 안 안쪽 방 바닥)·`Gates`, 충돌·내비·플로우 필드·공간 해시, `Visibility`(가 본 곳 지움) |
| `RunStats`(처치·금화·피해·무기별 피해 — 판 누적. `World.RunStats` 가 `RunState.Stats` 를 가리킴). **지우는 곳이 없다**(v0.7.1): `RunSession` 이 판 시작 때 새로 만들고, 월드 설치·주인공 만들기·WP3 `ResetCombat` 은 부르지 않는다 — 공개 `RunStats.Reset()` 은 WP1e-형이 없앤다(지금 부르는 코드 없음) | 적·투사체·장판·예고·획득물, 피격 무적(새 방은 시작 유예가 대신함), 무기 쿨다운·무기 내부 시간 상태(새 방에 들어오면 모두 준비 — 기획 v0.8.1 2.5). v0.7 의 `[해석]` "버프·마력 폭주는 방이 바뀌면 끝남" 은 v0.8 에서 닫았다 — 시간 효과는 넘긴다(위) |
| 판 흐름: 지금 액트·순번·지금 액트의 `ActPlan`·고른 문·다음 방 보상·방 기록(`RoomRecord`)·걸린 시간 | `Rng`(방 씨앗)·`Tick`(0 부터)·감독 상태·`InnerRoomPresence`·`BossBars`·`Banner`·`Fairness`(방이 끝날 때 `RunState.Fairness` 에 더함) |
| — 조명은 넘기지 않는다: 방을 만들 때 그 액트의 `light_change` 중 t < 방 t 시작인 것을 시간 순서로 다시 적용한다(`RoomEvents.Schedule` 의 `Replay`, v0.5.1 `--start-at` 규칙과 같은 함수). 그래서 `--room` 시작도 같은 조명이다. **v0.7.1**: 적용은 Core 순수 함수 `LightingState.Apply(LightChangeParams)` 이고 `World` 생성자가 **첫 틱 전에** 한다 — 감독(WP4) 없이도 서고, 방 첫 틱의 시야(1단계)가 바뀐 반경으로 계산된다 | `Lighting`(스테이지 조명 + 위 다시 적용) |

**결정 2 — 판 흐름은 순수 함수 `RunPlanner` 가 액트 단위로 미리 정한다**(Core `Sim/Run/`, 계약·구현 WP1e).

```csharp
public static class RunSeeds   // SplitMix64 섞기 한 곳 — 씨앗 이름·순서를 바꾸면 모든 판이 바뀐다
{
    public static ulong Flow(ulong runSeed, int act, int number);  // 판 흐름(방 맵·들어오는 문·출구 문 수·보상 후보). number = RoomDef.Number(보이는 번호, v0.7.1)
    public static ulong Room(ulong runSeed, int act, int number);  // 방 World 의 SimRandom
}
public static class RunPlanner
{
    public static ActPlan PlanAct(StageDef act, int actNumber, ulong runSeed, RoomRulesDef rules);
    public static int ActNumber(GameDefs defs, StageDef act);   // 뿌리 액트부터 next_act 사슬의 번호(1부터). 사슬에 없으면(헬파이어 곁가지 등) 1
    public static StageDef? ActAt(GameDefs defs, int number);   // --room 의 액트. 뿌리 = 다른 액트의 next_act 가 아니고 requires 가 빈 액트(13.5-20 이 하나만 허용)
}
public sealed record ActPlan(StageDef Act, int ActNumber, IReadOnlyList<RoomPlan> Rooms);
public sealed record RoomPlan(int Act, int Index, RoomDef Def, MapDef Map, string GateIn,
                              IReadOnlyList<string> ExitGates, IReadOnlyList<RewardDef> RewardOrder, ulong RoomSeed)
{
    public int Number => Def.Number;         // v0.7.1: 보이는 방 번호(RoomDef.Number) — 화면·로그·--room·RunSeeds·RunDepth 가 모두 이것. Index 는 rooms 배열 자리(0부터, 코드 안)
    public RoomKind Kind => Def.Kind;
    public bool Mirrored => Map.IsMirror;    // v0.8: 거울판(Map 은 MapMirror 가 만든 거울판 MapDef — 6.11 "거울판"). 로그 map= 은 Map.MirrorOf ?? Map.Id
}
```

- **v0.8 거울판과 문 이름**: `GateIn`·`ExitGates` 는 **그 방 맵(거울판이면 거울판)의 실제 문 이름**이다. 뽑기 ②③ 는 `rules.rooms.gates` 의 이름으로 하고, 거울판이면 그 결과를 짝 이름(`gate_nw` ↔ `gate_ne`, `gate_sw` ↔ `gate_se` — `GateZones.MirrorGate`)으로 바꿔 담는다.
  이벤트의 `entry:` 이름도 같은 짝 바꾸기를 거친다(`RoomState.RemapEntry`, 결정 5) — 그래서 거울판 방의 계획 전체(들어온 문·출구 순서·보스 입장 문)가 원판 계획의 거울 모양이다(보스 방: 원판 들어옴 `gate_sw`·보스 `gate_ne` → 거울판 `gate_se`·`gate_nw`).
  보스가 주인공 맞은편 문에서 나오는 것은 데이터(보스 이벤트 `entry:gate_ne`, 보스 방 들어온 문 `gate_sw`)와 v0.8.1 의 **시작 문 기준 바꾸기**(결정 5 끝 — 이벤트의 시작 문이 들어온 문일 때만 바꿈)가 함께 만든다. v0.8 문서의 "늘 맞은편" 은 v0.7.1 의 서로 바꾸기 아래에서는 틀렸다(보스가 들어온 문으로 옮겨졌다 — 13.21).
  들어오는 문 후보 {sw, se}·출구 {nw, ne} 는 짝 바꾸기에 닫혀 있어 뽑기의 분포는 그대로다.

- **방 번호 `RoomDef.Number`**(v0.7.1, JSON `number`, 선택): 없으면 배열 자리 + 1. `slice: true` 판(단계 1)은 기획 번호를 그대로 적는다(1·2·3·4·6·8) — 그래서 `--room 1:6` 은 단계 1 과 단계 2 에서 같은 t 의 방이고, 씨앗(`RunSeeds`)도 단계 2 에서 밀리지 않는다.
  13.5-23 ①: 번호는 1부터 커지기만 하고, `slice` 가 아니면 배열 자리 + 1 과 같아야 한다. HUD 분모는 그 액트 마지막 방 번호(`대성당 6 / 8`), `--room a:n` 의 `n` 이 표에 없는 번호면 옵션 오류(64).

- `actNumber` = 시작 액트(기본 액트 사슬의 뿌리 = 앞 액트가 없고 `requires` 가 빈 액트)부터 `next_act` 를 따라 센 번호(1부터). `--stage stage_catacombs` 로 시작해도 액트 2 다 — 깊이 기록·`--room 2:n` 이 같은 번호를 쓴다.
- 방마다 판 흐름 씨앗 `Flow(씨앗, 액트, 방 번호)` 으로 `SimRandom` 하나를 만들어 **이 순서로** 뽑는다(순서를 바꾸면 계약 변경):
  ① **방 맵** — 후보 = (맵, 방향) 쌍(v0.8, 기획 v0.8.1 10.8 "거울판"): 보스 방은 `room_maps.boss`, 아니면 그 종류의 풀. 맵마다 원판, 그 맵이 `mirror: true` 면 바로 뒤에 거울판(풀 순서 → 원판 → 거울판).
  후보가 둘 이상이면 **바로 앞 방(배열 바로 앞 원소)과 같은 쌍(같은 맵·같은 방향)을 빼고** 균등(앞 방 맵도 씨앗만으로 정해지므로 플레이와 무관). v0.7 의 "앞 방과 같은 맵을 뺀다" 는 풀 2장이면 A·B·A·B 로 굳어 씨앗이 첫 맵만 골랐다 —
  v0.8 은 같은 맵의 다른 방향은 바로 이어 나올 수 있고, 풀이 1장(단계 1·2 의 대성당)이어도 거울판이 있으면 원판·거울판이 번갈아 나온다. 보스 방도 같은 규칙(보스 맵 원판·거울판)이다.
  그래서 방 n 의 **맵은 앞 방들의 계획에 이어지고**, ②~④ 는 `Flow(n)` 만의 함수다(v0.7.1 — 시험 문구도 이대로, 10.1 `Run`). 난수는 v0.7.2 대로 단계마다 하나(①)다.
  ② **들어온 문** — 보스 방은 `rules.rooms.gates.boss_entry`(`gate_sw`), 아니면 `gates.entry`(`gate_sw`·`gate_se`)에서 균등.
  ③ **출구 문** — 판의 마지막 방이면 없음. 이 방이 액트의 마지막 방(보스 방 — 계단)이면 `gates.single_exit`(`gate_ne`) 하나.
  아니면 `gates.exits`(`gate_nw`·`gate_ne`) 둘, `exits.three_chance`(0.4) 면 남은 앞 문(`entry` 중 들어온 문이 아닌 것)을 더해 셋. 순서 = `exits` 순서 뒤에 남은 앞 문.
  **v0.8**(기획 v0.8.1 2.5 "문 갈래"): 다음 방이 정예·보스 방이어도 출구는 둘(셋) — v0.7 의 "다음이 정예·보스면 하나" 를 지웠다. 어느 문이든 그 방으로 가고 문마다 덤 보상이 다르다(아래 "문 열기").
  ②③ 의 이름은 `rules.rooms.gates` 이름으로 뽑고, ① 이 거울판이면 짝 이름으로 바꿔 담는다(위 "v0.8 거울판과 문 이름").
  ④ **보상 후보 순서** — 그 액트 `rewards.weights` 전체를 가중 비복원 추출한 순열(JSON 순서 배열, 3.4). 상태 거르기는 하지 않는다(아래 "문 열기").
- 순번은 데이터(`rooms`) 순서 그대로다 — 씨앗으로 섞지 않는다(기획 2.5 "방 순서 고정"). 그래서 `PlanAct` 결과는 (액트 정의, 액트 번호, 판 씨앗, `rules.rooms`)만의 함수이고(고른 문·카드와 무관 — `RunPlanner` 에 `World` 입력이 없다), 액트 시작 때 한 번 만들어 `RunState` 에 둔다.
- **방의 보상**(`RoomState.Reward` — 그 방을 정리하면 받는 것): 액트의 첫 방(배열 자리 0)은 `rewards.first_room`(액트 1 마법서·액트 2 보물 상자), 그 밖의 방은 **종류와 상관없이** 앞 방에서 고른 문의 보상이다.
  v0.8(기획 v0.8.1 11.4 `reward_elite`): 정예·보스 방의 보상 = 그 방 중간 보스·보스가 떨군 정예 상자(정의 없음, `GatesExit` 로 묶음) + **덤 보상**(`RoomState.Reward` — 그 방으로 들어온 문의 보상). v0.7 의 "정예·보스 방은 `Reward = null`" 을 바꿨다.
  `--room` 으로 그 방부터 시작하면 **앞 방(배열 바로 앞 원소) `RewardOrder` 의 첫 칸**(상태 거르기 없이 — 앞 방이 없으므로)이다 — v0.8 부터 정예·보스 방도 같다. `[해석]`

**결정 3 — 방 흐름은 상태 기계 하나 `RoomFlow`, 새 단계 `SimPhase.RoomFlow`(3.5 16번)에서 돈다.**

```csharp
// Core Defs/RoomDefs.cs (계약 WP1e) — JSON normal / elite / boss
public enum RoomKind : byte { Normal, Elite, Boss }
// Core Sim/Contracts/RoomState.cs (계약 WP1e)
public enum RoomPhase : byte { Enter, Survive, Cleanup, Reward, Doors, Exit }
public readonly record struct RoomExit(string GateId, RewardDef? Reward, RoomKind NextKind, EnemyDef? NextBoss, bool Stairs, string? NextActName,
                                       int NextFloor = 0);   // v0.8: 다음 방 층이 이 방과 다르면 그 층(문 위 `▼ 2층`, 넘김 어둠 transition_floor), 같거나 층을 안 적은 액트면 0
public sealed record RoomStart(RunState Run, RoomPlan Plan, RewardDef? Reward, float AreaScale, bool IsLastInRun, int StartClockTicks = 0);
public sealed class RoomState                          // World.Room — 쓰기는 생성자와 RoomFlowSystem 뿐
{
    public RoomPlan Plan { get; }   public RewardDef? Reward { get; }   public bool IsLastInRun { get; }   public float AreaScale { get; }
    public RoomPhase Phase { get; }                    // 지금 단계
    public int PhaseTick { get; }                      // 이 단계에 들어온 World.Tick
    public int ClockTicks { get; }                     // 방 시계(Enter·Survive·Cleanup 동안)
    public int SurviveTicks { get; }                   // 일반 방 생존 길이(t 끝 − t 시작), 정예·보스 방은 0(처치까지)
    public float T { get; }                            // 난이도 분(3.1) — 일반·정예 방은 to − 1틱에서 멈춤(정예 v0.7.1, 일반 v0.7.2 — 정리 단계는 그 값), 보스 방은 안 멈춤
    public bool SpawningOpen { get; }                  // Survive && ClockTicks ≥ 유예 && (정예 방이면 ClockTicks < elite_spawn_cap)
    public int CleanupTicks { get; }   public bool Collapsed { get; }   // 정리 안전장치가 돌았음
    public IReadOnlyList<RoomExit> Exits { get; }      // Doors 단계부터(그 전엔 빈 목록 — 문은 모두 봉인)
    public string? ChosenExit { get; }
    public bool ExitArmed { get; }                     // v0.7.1: Doors 에서 주인공이 열린 출구 문 틈 밖에 한 번 섰음(아래 Doors 줄)
    public string RemapEntry(string entryId);          // v0.8.1 시작 문 기준 바꾸기(결정 5 끝): 거울판 방이면 먼저 짝 이름(GateZones.MirrorGate), 그다음 그 문이 들어온 문이면 맞은편(GateZones.Opposite), 아니면 그대로
    public (string At, string Toward) RemapMarch(string atEntry, string towardEntry);   // v0.8.1: 행진 — 짝 이름 뒤 시작 문이 들어온 문이면 시작 → 맞은편, 목표는 들어온 문 ↔ 맞은편만 맞바꿈(나머지 두 문 그대로). 시작 문이 다르면 둘 다 그대로
                                                       // (v0.7.1 의 "서로 바꾸기"(맞은편 → 들어온 문)는 없앴다 — 맞은편 문의 보스·대장·행진이 주인공이 막 들어온 문으로 옮겨졌다)
    public int BossClockTicks { get; }                 // v0.8: 정예·보스 방에서 중간 보스·보스를 잡은 방 시계(로그 boss_s), 못 잡았거나 일반 방이면 −1
    public bool TimedEffectsRun { get; }               // v0.8: Phase is Survive or Cleanup — 주인공 시간 효과가 흐름(결정 1 "시간 효과 시계", World.TimedEffectsRun 이 감쌈)
    public Vector2 RewardSpot { get; }                 // v0.8: 보상 단계에 들어간 순간 정한 보상 자리(월드 px, RewardSpot.Find — 아래 "보상 자리"). 그 전엔 주인공 시작 자리
}
```

| 단계 | 다음으로 가는 조건 (RoomFlow, 16번 단계) | 그때 하는 일 |
|---|---|---|
| `Enter` | 방 시계 ≥ `rules.rooms.grace`(3s) | — (주인공은 들어온 문 진입점에서 `center` 를 보고 섬 — `World` 생성자) |
| `Survive` 일반 방 | 방 시계 ≥ `SurviveTicks` | 틱 이벤트 `RoomSurviveEnded`. 감독은 다음 틱부터 생성·이벤트를 안 함(남은 방 이벤트는 버림) |
| `Survive` 정예 방 | `EnemyKilled`(`DefIndex` == `Plan.Def.Miniboss` 정의의 인덱스 — 틱 이벤트에는 `DefIndex` 만 있다, `Cause == Damage`) | 위와 같음. 그 전이라도 방 시계 `elite_spawn_cap`(2:30) 부터 `SpawningOpen` 거짓. 이벤트 적과 `miniboss` 가 같은지는 13.5-23 ⑤ 가 막는다(다르면 이 줄이 영영 안 옴) |
| `Survive` 보스 방 | `BossDefeated` | 판의 마지막 방이면 `BeginVictory()`(3.5). 아니면 `CollapseEnemies()` → 바로 `Reward`(정리 단계 없음, 기획 2.5) + `RoomCleared(Boss)` |
| `Cleanup` | 살아 있는 적(입장·등장 중 포함) 0 | 판의 마지막 방이면 `BeginVictory()`(단계 1). 아니면 `Reward` + 틱 이벤트·신호 `RoomCleared` |
| `Cleanup` | 정리 단계 ≥ `cleanup_limit`(25s — v0.8, 기획 v0.8.1 2.5. v0.7 은 45s) | `CollapseEnemies()`, `Collapsed = true`, 그다음 위 줄과 같음 |
| `Reward` 시작 | — | (v0.8) **보상 자리** `RewardSpot` = `RewardSpot.Find(world, rules.rooms.reward_spot)`(아래 "보상 자리" — 정리가 끝난 순간 주인공 앞). 놓을 것 = `Reward`(덤 보상 포함) 하나 + 살아 있는 정예 상자 전부 — **이번 틱에 생긴 것(`SpawnTick == Tick`)도 포함**(v0.7.1: 마지막이 아닌 보스 처치 틱에 `DeathRewards` 가 떨군 상자). 순서 = 보상 → 상자(저장소 순서)이고 자리는 `RewardSpot.Row(...)`(보상 자리에서 주인공 바라보는 방향의 수직으로 `spacing` 40px 간격, 가운데 맞춤). `Reward` 는 `Spawner.SpawnPickup(new PickupSpawn(reward.Object, 자리, Reward: reward))` — **`PickupSpawn.Reward != null` 이면 `SpawnPickup` 이 `GatesExit = true` 로 만든다**(v0.7.1, WP3). 상자는 `RoomFlow` 가 `GatesExit = true` 로 표시하고 그 자리로 옮긴다. v0.7 의 `reward_anchor`(`center`)·`chest_ring` 은 없앴다 |
| `Reward` | `GatesExit` 인 살아 있는 획득물 0 이고 **끌려오는 획득물 0**(v0.7.1 — WP3 의 끌어당김 상태 `Pickup.Magnetized`, 정리 끝 끌어당긴 보석·금화가 다 닿을 때까지) 이고 `PendingLevelUps`·`PendingChests`·`PendingRewards` 0 이고 `AwaitingChoice == None` | `Exits` 를 만든다(아래 "문 열기") → `Doors`, 신호 `DoorsOpened`. `ExitArmed` = 주인공 발 칸이 열린 출구 문의 나가는 자리 **밖** |
| `Doors` | (주인공 발 칸 ∈ 열린 출구 문의 나가는 자리(`World.Gates[id].Cells`) 이고 `ExitArmed`), 또는 `Command.Buttons` 에 `CommandButtons.Exit`(그 문 = `Command.ExitIndex` 번째 `Exits`, 자동 조작 `DOOR_SKIP`, 9절) — 둘 다 **대기 0·`AwaitingChoice == None`·끌려오는 획득물 0** 일 때만 | `ChosenExit`, `Exit`. `ExitArmed` 는 발 칸이 나가는 자리 밖인 틱마다 참이 된다 — 보상을 집다 문 틈에 서 있던 주인공이 문 위 보상을 보기도 전에 나가지 않게(기획 "들어서면") |
| `Exit` | — | 시뮬레이션은 더 돌지 않는다(`StageStep.RoomExit`, 3.3). 앱이 `RunSession.Advance()` |

- 주인공이 죽으면 어느 단계든 v0.6 대로 패배(`EndTick`)다. `Outcome != Running` 이면 `RoomFlow` 는 아무것도 안 한다. `Hero.Dead` 는 **나감만 막는다**(v0.7.2 — `Reward` → `Doors`·`Doors` → `Exit` 를 하지 않는다. 죽은 틱에 출구 틈에 들어서거나 `Exit` 명령을 내도 `Exit` 로 가지 않고, `EndTick`(17)이 같은 틱 뒤에 `DefeatPending` 으로 바꾼다).
  나머지 전이는 죽은 틱에도 돈다 — 판의 마지막 방에서 같은 틱에 보스 처치·정리 끝이 오면 `BeginVictory` 가 먼저 불려 승리다(3.5 "같은 틱이면 승리가 먼저").
  막는 곳 둘을 더 둔다: `StageStepper` 는 `Outcome == Running` 일 때만 `RoomExit` 를 돌려주고(끝나 가는 판은 돌아서 `Defeat` 로 끝남), `RunSession.Advance()` 는 `Outcome != Running` 이면 예외(죽은 주인공의 짐을 넘기지 않음).
- **무너짐** `World.CollapseEnemies()`: 살아 있는 적 전부 `Dead`(`KillCause.Collapse` — 훅·보상·처치 수 없음, 3.5). 적 투사체·장판·예고는 `EndTick` 이 `Phase ≥ Reward` 인 동안 지운다(승리 대기와 같은 함수).
  정리 단계의 분열·소환은 그대로 일어나고(기획 2.4) 그 적도 정리 대상이다.
- **문 열기**(기획 11.4): 다음 방이 액트 안에 있으면(종류와 상관없이 — v0.8) `Plan.RewardOrder` 를 앞에서부터 보며 `IProgression.RewardAvailable(r)`(WP3 — v0.8 거르기: 마법서 = 줄 무기 카드가 있음, 물약 = 허리띠가 차지 않음, 마법에 걸린 성소 = 진화 아닌 무기 ≥ 2, 매혹의 성소 = `pick_max_level` 이하 무기 있음, 성소 = 이 판에 안 받음, 보물 상자 = 늘 참)인 것을 출구 문 수만큼 고른다.
  모자라면 `rules.rooms.fallback_reward`(보물 상자)로 채운다(같은 종류가 두 문에 올 수 있음). 출구 문 순서 = `ExitGates` 순서. 상태가 같으면 결과가 같다(결정적).
  거르기는 문을 여는 이 순간 한 번뿐이다 — 실제로 집는 것은 다음 방 정리 뒤라 그 사이 레벨업으로 줄 카드가 0장이 될 수 있다. 그때는 집는 순간 창 없이 **`단련`**(`rules.level_up.temper`)으로 바로 처리한다(6.18 "받는 길" — v0.8, 기획 v0.8.1 11.4. v0.7.1 은 그 보상의 `skip` 금화).
  다음 방이 정예·보스 방이면 보상(= 덤) + `NextKind`·`NextBoss`(문 위 `정예`·보스 이름과 덤 아이콘), 액트 보스 방이면 보상 없음 + `Stairs`·`NextActName`(`지하묘지로`). 다음 방 `RoomDef.Floor` 가 이 방과 다르면 `NextFloor`(문 위 `▼ 2층` — 계단 문도).
- **보상 자리** `RewardSpot.Find(World, RewardSpotDef)`(v0.8, Core `Sim/Run/RewardSpot.cs` — 계약·구현 WP1f, 기획 v0.8.1 11.4): 정리가 끝난 순간(보스 방은 보스가 무너진 순간) 주인공 발에서 `Hero.Facing` 방향으로 `distance`(100px) 떨어진 자리를 원한다.
  방향 후보 = 바라보는 쪽, ±45°, ±90°, ±135°, 180°(이 순서, + 쪽 먼저). 후보마다 주인공 발에서 그 방향으로 8px 씩 나아가며 `INavWorld.IsInsideMap` 이고 `Clearance ≥ clearance`(30px)인 동안만 걷는다 — 직선이 끊기지 않으므로 그 자리는 주인공과 길로 이어진다.
  `distance` 까지 간 첫 방향의 끝을 쓰고, 없으면 가장 멀리 간 방향의 끝(같으면 앞 후보).
  어느 방향도 8px 을 못 가면(v0.8.1 — 문 틈·기둥 사이처럼 좁은 곳에서는 발 앞 8px 점의 clearance 가 30 미만이라 모두 실패한다) **같은 차례를 clearance 문턱 = 주인공 반경(`Hero.Radius`)으로 한 번 더** 돌고, 그래도 못 가면 주인공 발 자리.
  (두 번째 차례의 자리는 주인공이 선 곳과 같은 좁은 길이라 집을 수 있다 — 기획 "가장 가까운 걸을 수 있는 칸" 에 더 가깝다.) 여럿 놓을 때 `RewardSpot.Row(spot, facing, count, spacing)` 는 수직 방향 줄이고, 줄의 각 자리가 위 조건(맵 안·clearance — v0.8.2: 자리를 찾은 차례의 문턱, 두 번째 차례면 주인공 반경. 13.22)을 못 지키면 그 자리 대신 `spot` 쪽으로 8px 씩 당긴다.
  난수를 쓰지 않는다(같은 자리·같은 방향이면 같은 자리). `TryPlace`(WP2)를 기다리지 않으려고 `INavWorld` 질의 둘만 쓴다(v0.7.2 의 상자 자리와 같은 까닭 — 13.19). 자원 없는 시험은 TestKit 의 직선 내비로 돈다.
  `center` 기준점은 남는다 — 큰 적 길 검사(13.5-5)의 목적지이고 13.5-23 ③ 이 여전히 요구한다. 보상 자리로는 쓰지 않는다.
- **사냥 모드**(v0.8, 기획 v0.8.1 2.5): `Phase == Cleanup` 인 동안 적의 거리 유지(`kite`)·도망 부품을 끄고 길을 따라 주인공에게 다가가게 한다(원거리 공격 규칙·시야 조건은 그대로). 판정은 감독·적 부품(WP4)이 `World.Room.Phase` 를 읽어 한다 — 계약 변경 없음.
  남은 적 미니맵 점은 정리 단계에 들어가면 수와 상관없이 모두(WP6) — v0.7 의 `remaining_marker`(10 이하부터) 키는 없앴다.
- **나가는 자리**(기획 10.8 "문 틈 4칸 × 안쪽 2칸"): 그 문의 열린 메가타일 둘의 하위 칸 중 걷기 막힘 없는 칸(`GateZones`, 6.11 — 문 틈 그 자체, 앞 턱 문은 두 줄, 뒷벽 문도 메가 두께라 두 줄). 충돌은 바뀌지 않는다. `[해석]`
  주인공이 문마다 그 칸 하나 이상에 닿는지는 13.5-23 ③ 의 길 검사(v0.7.1, WP2 — 13.5-5 의 "몸이 걸칠 수 있다" 판정)가 본다. 13.5-5 는 진입점 `inside` 까지만 보므로 이것만으로는 문 틈을 보장하지 않는다.
- 보스 방이 판의 마지막이 아니면 `RoomCleared(Boss)` 를 받은 WP3 가 체력을 전부 채운다(기획 11.4). 보석·금화 끌어당김은 `RoomCleared` 를 받은 WP3 `Pickups`(다음 틱, 끌어당김 룬과 같은 동작).

**결정 4 — 판 진행은 Core `RunSession` 한 곳**(앱은 부르기만, 시험도 같은 것을 돌린다).

```csharp
// Core Sim/Run/ (계약·구현 WP1e, 그 뒤 WP4)
public readonly record struct RoomRef(int Act, int Room);                   // --room 2:6 (둘 다 1부터, Room = RoomDef.Number)
public readonly record struct RunDepth(int Act, int Room) : IComparable<RunDepth>;   // 가장 깊이 간 곳(액트 → 방 번호 사전순). 기록 비교 키는 8.3(깊이 → 승리 → 시간)
public sealed record RunStart(ulong Seed, StageDef StartAct, HeroClassDef? HeroClass, RunOptions Options, int StartRoomIndex = 0);
//   --room a:n → 앱(WP7)이 RunPlanner.ActAt(defs, a) 로 액트를 찾고 StartRoomIndex = Number == n 인 rooms 원소의 배열 자리. 없는 번호는 옵션 오류(64)
// v0.8(WP1f): 위치 인자 record → init 속성 record(필드를 더할 때 기존 생성 호출·FakeHeroCarry 가 깨지지 않게). JSON 왕복(계단 저장 RunSnapshot)도 이 모양 그대로
// v0.8.1: 짐에 필드가 필요하면 WP1g 로 모은다(sealed 계약 record — WP3 가 조각으로 더할 길이 없다). 판 수명 상태를 모듈 인스턴스·정적 필드에 숨기지 않는다(11.2)
public sealed record HeroCarry
{
    public required int Level { get; init; }   public required int Xp { get; init; }   public required int RerollsLeft { get; init; }   public required float Hp { get; init; }
    public IReadOnlyList<CarriedSlot> Weapons { get; init; } = [];   public IReadOnlyList<CarriedSlot> Passives { get; init; } = [];   public IReadOnlyList<CarriedSlot> Blessings { get; init; } = [];
    public required int WeaponSlotsOpen { get; init; }   public required int PassiveSlotsOpen { get; init; }   // v0.8: 열린 칸 수(rules.level_up.*_slots 로 시작, 계단마다 rules.rooms.stairs 만큼 +)
    public int Belt { get; init; }                                        // v0.8: 허리띠 물약 수(0..rules.hero.belt.slots) — v0.9 WP1g 에 없앰 → Potions·PotionSlotsLevel·PotionSpeedLevel, 더함 Equipment·Gold·MaxedAtTick(6.19·11.1 WP1g #6)
    public IReadOnlyList<CarriedStat> Mods { get; init; } = [];           // v0.8: 판 끝까지 가는 능력치 더하기(단련 max_hp +5 쌓임·성소 대가 — 같은 키는 하나로 합침, StatKey 순)
    public IReadOnlyList<string> Shrines { get; init; } = [];             // v0.8: 이 판에 받은 성소 보상 ID(서수 순 — 한 판에 한 번)
    public IReadOnlyList<CarriedEffect> Effects { get; init; } = [];      // v0.8: 시간 효과의 남은 틱(Id 서수 순 — 이름은 WP3 가 정함, 예 buff ID)
}
public readonly record struct CarriedSlot(string Id, int Level, bool Evolved);
public readonly record struct CarriedStat(StatKey Stat, float Add);
public readonly record struct CarriedEffect(string Id, int TicksLeft, float Value);
public interface IHeroCarry
{
    HeroCarry Capture(World world); void Restore(World world, HeroCarry carry);   // WP3 이 ModuleContext.SetHeroCarry 로 등록
    string? Validate(HeroCarry carry, GameDefs defs);   // v0.8.1: 이어 하기 전 WP3 만 아는 뜻(시간 효과 ID·진화 짝·칸 수 범위) 확인 — null = 맞음, 아니면 이유(Restore 전에 RunSnapshotMismatchException)
}
public readonly record struct RoomRecord(int Act, int Number, string MapId, string GateIn, string? RewardId, string? Door,
                                         int ClearTick, int CleanupTicks, bool Collapsed, int Ticks, int Level,
                                         bool Mirrored = false, int BossClockTicks = -1, int PicksLeft = -1);   // v0.8: ROOM 로그 mirror·boss_s·picks_left(IProgression.PicksLeft, 없으면 −1). MapId = 원판 ID
public sealed class RunState
{
    public ulong Seed { get; }   public StageDef StartAct { get; }   public HeroClassDef? HeroClass { get; }
    public ActPlan Act { get; }  public int RoomIndex { get; }       public RunDepth Depth { get; }
    public RunStats Stats { get; }   public FairnessCounters Fairness { get; }   public int ElapsedTicks { get; }
    public IReadOnlyList<RoomRecord> Rooms { get; }                 // 끝난 방(HUD 방 줄·결과·ROOM 로그)
    public HeroCarry? Carry { get; }   public RewardDef? NextReward { get; }
}
public sealed class RunSession
{
    public RunSession(GameDefs defs, ModuleContext modules, RunStart start);
    public RunState Run { get; }   public World World { get; }       // 지금 방(World.Simulation 이 들어 있음)
    public void Advance();                                           // World.Room.Phase == Exit 이고 World.Outcome == Running 일 때만(아니면 예외, v0.7.2)
    public RunState Conclude();                                      // v0.7.1: Outcome 이 Victory·Defeat 일 때만(아니면 예외). 두 번째부터는 아무것도 안 하고 같은 RunState. 그 뒤 Advance 는 예외
    // ── v0.8 계단 저장(기획 v0.8.1 2.5, 8.3 — 계약·구현 WP1f) ──
    public RunSession(GameDefs defs, ModuleContext modules, RunSnapshot snapshot, RunOptions options);   // 이어 하기: 다음 액트 1번 방부터. 정의가 안 맞으면 RunSnapshotMismatchException
    public bool CanSuspend { get; }                                  // 메뉴용: 액트 보스 방 Doors(계단 출구가 열림)·Outcome == Running·대기 0·AwaitingChoice None·주인공 살아 있음
                                                                     //   또는(v0.8.1) Phase == Exit 이고 ChosenExit == 계단 문(자동 조작 판이 계단으로 나간 틱 — Advance 자리)
    public RunSnapshot Suspend();                                    // v0.8.1: 계단 문 Exit 틱과 같은 길(아래 "계단 저장") → Advance ①~③ 정산까지만 하고 판 상태를 돌려줌. 그 뒤 Advance·Conclude 는 예외
}
// Core Sim/Run/RunSnapshot.cs (v0.8) — 계단 저장 한 칸. STJ 로 그대로 쓰고 읽는다(저장 파일 모양 = 이 record, 8.3)
public sealed record RunSnapshot(int Format, ulong Seed, string StartActId, string? HeroClassId, string NextActId, int NextActNumber, string NextActRoomsHash,
                                 HeroCarry? Carry, RunStats Stats, FairnessCounters Fairness, int ElapsedTicks, RunDepth Depth, IReadOnlyList<RoomRecord> Rooms)
{
    public const int CurrentFormat = 1;          // v0.8.1: 짐·RunStats·FairnessCounters·이 record 의 모양이 바뀌면 올린다(모양 지문 시험이 지킴 — 아래 "계단 저장")
}
public static class RoomStartLimits { public static int? MaxStartClockTicks(RoomDef room, RoomRulesDef rules); }   // v0.8 --start-at 상한(결정 5 끝)
public static class RunSlots { public static (int Weapons, int Passives) OpenAt(int actNumber, LevelUpRulesDef? levelUp, RoomRulesDef rooms); public static (int Weapons, int Passives) OpenAt(int actNumber, RulesDef rules); }   // v0.8.1 열린 칸 수(아래 "계단 보상" — v0.8.2 13.22: 시작 칸 수는 rules.level_up)
```

- 생성자: 시작 액트의 `ActPlan`(액트 번호 = `ActNumber`) → 시작 방(`StartRoomIndex`, 기본 순번 0)의 `World` 를 만들고 `ModuleContext.CreateSimulation` → `ApplyRunSetups(options)`(`--level`·`--weapons`·`--passives`·`--god` …). `RunOptions.StartAt` 은 이 첫 방의 방 시계 시작(`RoomStart.StartClockTicks`)이다.
- `Advance()`: ① `IHeroCarry.Capture(World)` ② **방 닫기**(`CloseRoom` — 내부 함수 하나): `RoomRecord`·`ElapsedTicks += World.Tick`·`Fairness` 더하기·그 방의 `INNER_ROOMS` 줄 자료 ③ 고른 문의 보상을 `NextReward` 로 ④ 다음 방 — 액트의 마지막 방(계단)이면 `next_act` 의 `ActPlan` 을 만들고 배열 자리 0
  ⑤ 새 `World(defs, RoomStart)`(방 씨앗) + 시뮬레이션 → `ApplyRunSetups(options with { Level = null, Weapons = [], Passives = [], StartAt = null })`(무적·계측만 다시) → `IHeroCarry.Restore(world, carry)`.
  등록된 `IHeroCarry` 가 없으면(WP3 없는 시험) ①⑤ 의 짐은 건너뛴다.
  **계단 보상**(v0.8, 기획 v0.8.1 2.5): ④ 에서 액트가 바뀌면 짐을 `carry with { WeaponSlotsOpen = 열린 칸(다음 액트 번호).Weapons, PassiveSlotsOpen = ….Passives }` 로 바꿔 넘긴다.
  **열린 칸 수는 액트 번호만의 함수**(v0.8.1, Core `RunSlots.OpenAt(actNumber, levelUp, rooms)`·`OpenAt(actNumber, rules)` = `min(6, rules.level_up.*_slots + (actNumber − 1) × rules.rooms.stairs.*_slots)`, 6 = `WeaponSlots.SlotCount`·`PassiveSlots.SlotCount`) —
  판 시작(주인공 만들기 WP3 가 `World.Room?.Plan.Act ?? 1` 로 부름)과 계단(위)이 같은 함수를 쓴다. 그래서 `--room 2:n`·`--stage stage_catacombs` 로 액트 2 에서 시작해도 보통 판의 액트 2 와 같은 5칸이다(v0.8 은 계단을 안 지나 4칸이었다).
  `--weapons`·`--passives` 의 수가 시작 액트의 열린 칸보다 많으면 옵션 오류(64, 데이터를 읽은 뒤 — WP7 이 같은 함수로 확인. 조용히 잘리지 않게).
- **계단 저장**(v0.8, 기획 v0.8.1 2.5 — 계약·구현 WP1f, 저장 파일은 WP7·화면은 WP6, 8.3): `Suspend()` 는 **계단 문으로 나가는 틱과 같은 길**을 탄다(v0.8.1 — v0.8 은 문 단계에서 틱 없이 정산해, 끊지 않은 판보다 보스 방 `Ticks`·`ElapsedTicks`·그 틱의 재생이 달랐다):
  ⓐ `Phase == Doors`(메뉴에서 부름)면 시뮬레이션을 **한 틱** `PlayerCommand(Vector2.Zero, CommandButtons.Exit, ExitIndex = 계단 문의 Exits 자리)` 로 돌린다 — 끊지 않은 판이 같은 틱에 계단 문 `Exit` 명령을 낸 것과 같은 틱이다(Doors 에는 적·대기가 없어 그 틱에 `Exit` 로 간다. 못 가면 `InvalidOperationException` — `CanSuspend` 조건이면 일어나지 않음).
  ⓑ `Phase == Exit` 이고 `ChosenExit` 가 계단 문이면(자동 조작 판이 계단으로 나간 틱, 사람이 문 틈을 밟은 틱) 틱을 더 돌리지 않는다 — `Advance` 를 부를 자리에서 대신 부른다.
  그다음 계단 문으로 `Advance` 하는 것과 **같은 정산**(① 짐 담기 + 계단 보상 ② `CloseRoom(계단 문)` ③ `NextReward = null`)까지만 하고
  새 `World` 를 만들지 않고 `RunSnapshot`(씨앗·시작 액트·직업·다음 액트 ID·번호·그 액트 `rooms_hash`(8.3 깊이와 같은 식)·짐·`RunStats`·공정성·걸린 시간·깊이·방 기록)을 돌려준다.
  이어 하기 생성자는 그 값으로 `RunState` 를 되살리고 다음 액트의 `PlanAct` → 1번 방(`first_room` 보상) → `Restore(carry)` — `Advance` ④⑤ 와 같은 함수(`Enter`)를 탄다. 그래서 **이어 한 판의 판 해시 = 끊지 않은 판**(기획 15절 — 시험 10.1 `Run`): 방 씨앗이 앞 방과 무관하고(3.4), 넘기는 것이 짐·`RunState` 둘뿐이다.
  **비교 기준**(v0.8.1): "끊지 않은 판" 은 끊은 판이 `Suspend` 한 틱에 계단 문 `Exit` 명령(ⓐ) 또는 같은 문 틈 밟기(ⓑ)로 나간 판이다 — 시험·자동 조작 판 전체 실행 모두 이 정의로 견준다.
  **판 수명 상태는 짐·`RunState` 에만 둔다**(v0.8.1, 11.2 규칙): 모듈 인스턴스(`ModuleContext` 에 등록한 객체의 필드)·정적 필드에 판 수명 상태를 두면 이어 하기(새 `ModuleContext`)에서 사라져 같음이 깨진다. 짐에 새 필드가 필요하면 WP1g 로 모은다.
  **저장 멤버 규칙**(v0.8.1 — v0.8 의 "공개 필드만" 을 바꿈): `RunStats`·`FairnessCounters` 의 상태는 **공개 읽기·쓰기 멤버**(공개 필드, 또는 공개 get·set 속성)이고 형은 값 형식·그 배열뿐이다. get 만 있는 속성(특히 배열·컬렉션)은 STJ 가 역직렬화 때 건너뛰어 조용히 0 이 되므로 금지 — `RunStats.DamageByWeapon` 은 `{ get; set; } = new float[WeaponSlots.SlotCount]`(6)이고 `WeaponSlotCount = 4` 상수는 없앤다.
  STJ 는 필드 포함 옵션으로 왕복한다. 시험(WP1f): 반사로 **공개 필드 + 공개 속성 전부**를 0 아닌 값으로 채워 왕복 → 다시 쓴 JSON 바이트 같음, get 만 있는 공개 속성·허용 밖 형이 하나라도 있으면 실패(규칙 강제).
  **모양 지문**(v0.8.1 — R41): `RunSnapshot` 에서 닿는 형 전부(짐·`CarriedSlot`·`RunStats`·`FairnessCounters`·`RoomRecord`·`RunDepth`)의 공개 멤버 (이름, 형) 목록과 거기서 닿는 열거형의 값 JSON 이름 목록(v0.8.2 — `StatKey` 값을 없애거나 바꿔도 깨지게)을 서수로 이은 해시를 시험이 계산해 `RunSnapshot.CurrentFormat` 옆 기대 값과 견준다 — WP3·WP4 가 `RunStats` 조각에 멤버를 더하면 시험이 깨지고, 고치는 사람이 `CurrentFormat` 과 기대 값을 함께 올린다. 다른 `Format` 의 저장은 버린다(아래).
  **정의가 안 맞으면**(이어 하기 생성자, `Restore` 전에 모두 확인 → `RunSnapshotMismatchException`): `Format` ≠ `CurrentFormat`, 시작·다음 액트·직업이 없음, `rooms_hash` 다름, 다음 액트 번호 다름(v0.8.2 — 시작 액트의 번호 + 시작 액트에서 `next_act` 를 따라간 걸음 수, 사슬에 없으면 불일치. 13.22), 짐의 정의 ID 가 없음(v0.8.1 — `Weapons`·`Passives`·`Blessings` 의 `Id` = 무기·패시브 정의, `Shrines` = 성소 보상 정의),
  `DamageByWeapon` 길이 ≠ 6, `IHeroCarry` 등록 여부와 `Carry` null 여부가 다름(`Carry` 는 `IHeroCarry` 가 없는 판 — WP3 없는 시험 — 에서만 null), `IHeroCarry.Validate` 가 이유를 돌려줌(시간 효과 ID·진화 짝 등 WP3 만 아는 것).
  앱은 저장을 버리고 로그 한 줄(8.3) — `TakeRun` 이 이미 지웠으므로 예외가 `Restore` 안에서 터져 앱이 죽거나 짐 일부가 조용히 빠진 판으로 이어지는 길을 막는다. `Suspend` 는 `CanSuspend` 일 때만(아니면 예외).
  자동 조작 정책(WP4)은 방마다의 판단 상태를 월드가 바뀔 때 비운다 — 판단이 (`World`, 그 방 상태)의 함수여야 끊었다 이은 판이 같다. 로그용 목록(`DoorSkips` 등)은 판단에 쓰지 않는다.
- **`Conclude()`**(v0.7.1): 판이 끝난 방(마지막 방의 승리, 어느 방이든 패배·포기)은 `Exit` 에 닿지 않아 `Advance` 가 불리지 않는다 — 그 방을 같은 `CloseRoom` 으로 닫는다(`RoomRecord.Door = null`).
  `PlayScene` 이 `StageStep.Finished` 를 처음 받은 앱 틱에 한 번 부르고(3.3), 결과 화면·`RESULT`·`RecordResult`(`RunTime` = `ElapsedTicks`)·시나리오 시험은 그 뒤의 `RunState` 만 읽는다. 빠뜨리면 걸린 시간이 마지막 방만큼 짧아 기록 비교(8.3)가 틀린 값으로 고른다.
- **넓이 배율**(기획 10.8): `RunSession` 이 판 시작 때 쓰일 수 있는 맵(지금 액트 사슬의 모든 풀)을 한 번씩 펼쳐 `MapArea.WalkableCells`(6.11)를 세고 `AreaScale = clamp(칸 수 / 기준 맵 칸 수, min, max)`(`rules.rooms.area_scale`, 기준 `cathedral_arena`)를 `RoomStart` 에 넣는다. 감독이 동시 최대에 곱한다(반올림).
- **판의 끝**: 마지막 액트(다음 액트 없음)의 마지막 방이 끝나면 승리(위 표), 패배·포기는 v0.6 그대로. `RunState` 가 결과 화면·저장·`RESULT` 로그의 출처다(8.3·9절). 판 중간 저장은 액트 사이의 **계단 저장** 하나다(v0.8 — 위. 방 안·액트 1 에서 끄면 그 판은 끝, 기획 v0.8.1 2.5).
- **방 없는 월드**: `World(defs, seed, stage)` 처럼 `RoomStart` 없이 만들면 `World.Room == null` 이고 v0.6 처럼 그 액트의 보스 맵(없으면 첫 일반 맵) 하나에서 돈다(`RoomFlow` 꺼짐, `DifficultyMinutes = StageMinutes`). 시험·`--preview`·`--map` 만 쓴다. 게임 판은 늘 `RunSession` 이다.
- **생성자 둘**(v0.7.1): 방 월드는 `World(GameDefs defs, RoomStart room)` 하나로만 만든다 — 씨앗 = `room.Plan.RoomSeed`, 액트 = `room.Run.Act.Act`, 직업 = `room.Run.HeroClass` 라 서로 다른 값이 들어올 길이 없다. 방 없는 월드는 `World(defs, seed, stage?, heroClass?)` 그대로.

**결정 5 — 방 이벤트는 순수 함수 `RoomEvents.Schedule` 이 방 시계로 옮긴다**(WP1e 구현·시험, 감독 WP4 가 부름).

```csharp
public static class RoomEvents
{
    public static RoomSchedule Schedule(StageDef act, RoomPlan room, RoomRulesDef rules, int startClockTicks);
}
public sealed record RoomSchedule(IReadOnlyList<ScheduledEvent> Events, IReadOnlyList<PartDef> Replay);   // Replay = 지금 바로 적용할 상태형 이벤트(light_change), 시간 순
public readonly record struct ScheduledEvent(int ClockTick, PartDef Event);                           // 방 시계 순, 같은 틱이면 정의 순서
// v0.7.1 — light_change 매개변수 형은 계약(Core Defs/LightChangeParams.cs, WP1e-형): 6.12 의 받는 경로·값 목록. 부품 등록·방 안 적용은 WP4
public sealed record LightChangeParams(IReadOnlyList<LightSet> Set);
public readonly record struct LightSet(string Path, float[] Value);          // ambient·darkness·unit_min_light·hero_light.*·lights.<id>.*
// LightingState 에 더함: public void Apply(LightChangeParams change);       // 순수, 없는 lights.<id> 는 로딩 검사가 막음(13.5-1)
```

- 이벤트 시각은 `PartDef.At`(v0.7 — 로더가 스테이지 이벤트의 `at` 을 매개변수에서 꺼내 둔다, WP7)이고 **t** 다. 방 [t 시작, t 끝)(보스 방은 t 시작 이상)에 드는 이벤트만 그 방 것이고, 방 시계 = max(t − t 시작, `grace`)(기획 13.4).
- t < 방 t 시작인 `light_change` 는 `Replay`(시간 순). 그 밖의 지난 이벤트는 버린다. `startClockTicks`(`--start-at`) 보다 이른 방 이벤트는 `light_change` 면 `Replay` 뒤에 붙이고,
  **`miniboss`·`boss`(입장 이벤트)는 버리지 않고 방 시계 `startClockTicks` 로 옮겨 바로 내보낸다**(v0.8, 기획 v0.8.1 15절 — 정예·보스 방을 `--start-at` 으로 시작해도 생존이 끝날 수 있게), 나머지는 버린다. 보스 광폭화 시계는 감독(WP4)이 그만큼 흐른 것으로 친다.
- **`--start-at` 상한**(v0.8, 계약·구현 WP1f — v0.7.1 9절 규칙을 Core 한 곳으로): `RoomStartLimits.MaxStartClockTicks(room, rules)` = 일반 방 `SurviveTicks`(그 미만만), 정예 방 `elite_spawn_cap`(그 미만만), 보스 방 `null`(상한 없음).
  옵션 파싱(WP7)이 이것으로 64 를 내고, `RunSession` 생성자도 같은 함수로 `ArgumentOutOfRangeException`(앱을 거치지 않는 시험·시나리오의 잘못을 조용히 넘기지 않게).
- **방 이벤트 자리 검사**(v0.8, 13.5-23 ⑥ — 계약·구현 WP1f, 검사기 등록은 WP4): `RoomEvents.CheckPlacement(StageDef act, RoomRulesDef rules) → IReadOnlyList<DataError>` — 이벤트를 위와 같은 규칙으로 방 시계에 놓은 뒤
  ① `RoomEvents.StrongKinds`(`elite_pack`·`escort`·`ring` — 기획 "강적(정예 무리·해골 대장)·포위 고리")가 방 시계 `StrongEarliest`(0:20) 전이면 **오류**(`miniboss`·`boss` 입장은 예외),
  ② 2번 방부터의 일반 방에 생존 끝 `ClimaxWindow`(0:45) 안의 `RoomEvents.ClimaxKinds`(`march`·`ring`·`elite_pack`·`escort`) 이벤트가 없으면 **경고**. 상수·종류 목록은 Core 상수다(기획 13.5-23 의 수 — 데이터 아님, `RoomMapRules` 와 같은 까닭).
  이벤트 종류 이름은 `PartDef` 의 종류 문자열로만 본다(매개변수 형은 WP4 — 몰라도 됨).
  `Replay` 는 `World(defs, RoomStart)` 생성자가 첫 틱 전에 `LightingState.Apply` 로 적용한다(v0.7.1 — 결정 1 표). 감독은 방 안에서 시각이 된 `light_change` 만 같은 함수로 적용한다.
- 생존 단계가 끝나면 감독이 남은 `Events` 를 버린다(이 함수 밖 — 감독 상태). `entry:` 바꾸기는 이벤트 부품이 한다 — 문 하나(`miniboss`·`boss`·`at_entry` 만 있는 생성)는 `Room.RemapEntry`, 목표 문이 있는 행진은 `Room.RemapMarch(at_entry, toward_entry)`(v0.8: 거울판 방이면 둘 다 안에서 짝 이름을 먼저 — 부품은 따로 할 일이 없다).
  **v0.8.1 시작 문 기준 바꾸기**(기획 v0.8.3 2.4 "바꾸기는 한쪽만이다"): 이벤트의 **시작 문**(`entry:`)이 **들어온 문일 때만** 그 이벤트를 바꾼다 — 시작 문은 맞은편으로, 행진이면 목표 문에서 들어온 문 ↔ 맞은편만 맞바꾼다(목표가 맞은편이었으면 들어온 문이 됨 — 시작한 문으로 다시 가지 않게, 나머지 두 문은 그대로). 시작 문이 들어온 문이 아니면 시작·목표 모두 그대로다(목표 문이 들어온 문인 행진도 그대로 — 주인공이 들어온 쪽으로 가로지를 뿐 등 뒤에서 쏟아지지 않는다).
  예: 6:30 행진(`at_entry gate_se`·`toward_entry gate_nw`)은 `gate_sw` 로 들어온 방에서 그대로, `gate_se` 로 들어온 방에서 `gate_nw` → `gate_se`. 보스(`entry:gate_ne`)는 보스 방이 늘 `gate_sw` 로 들어오므로 그대로 `gate_ne` — 맞은편이다.
  v0.7.1 의 **서로 바꾸기**(들어온 문 ↔ 맞은편)는 없앴다: 맞은편 문에 적힌 이벤트(보스 `gate_ne`, 2번 방 행진·7·8번 방 대장의 `gate_ne`, 5·7·8번 방의 `gate_nw`)를 주인공이 **막 들어온 문**으로 옮겨, 해골왕이 모든 판에서 방 시계 0:03 에 주인공 등 뒤 문으로 나왔다(시험 `fromSw.RemapEntry("gate_ne") == "gate_sw"` 가 그 동작을 굳히고 있었다 — WP1f #8 에서 고침).
  13.5-23 ⑤ 는 들어올 수 있는 문마다(거울판 포함) 바꾼 뒤 **시작 문 = 들어온 문이면 오류**(맞은편 문이 없는 맵 등), 행진의 시작 문 = 목표 문이면 오류를 본다.

---

## 4. 이동·충돌·길찾기

### 4.1 좌표와 단위 (v0.3: 등각 아레나의 화면 픽셀)

- 월드 좌표 = **아레나 그림의 화면 픽셀**(`float`, 기획 10.1 — 조사 문서의 안 A). 유닛 위치 = **발 위치**(기획 8.1). 속도 px/s 는 화면 어느 방향이든 같고,
  몸 충돌 원은 화면에서 동그라미다. 각도는 데이터에서 도(°), 로딩 때 라디안.
- 이유: v0.2 설계(AABB 카메라·생성 띠·원 충돌·16px 격자·"도형이 정본")가 그대로 서고, 바뀌는 것은 맵을 만드는 곳뿐이다.
  직교 타일 좌표(안 B)는 화면 사각형이 회전한 사각형이 되어 `CameraMath`·생성 띠·화면→월드 변환을 다시 짜야 하고, 얻는 것은 "세로가 느린 원작 느낌" 뿐이다.
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
  1. 막힘 종류마다(걷기: `w`·`W`·`x`·`X`, 투사체: `m`·`M`·`x`·`X`, 6.11 — v0.5 글자) 하위 타일 칸을 타일 공간에서 **탐욕 직사각형**으로 묶는다(행 우선, 결정적).
  2. 타일 공간 직사각형은 등각 변환(아핀)으로 화면의 **평행사변형** = 볼록 다각형 하나가 된다. 둘레 벽은 문 틈을 포함해 십여 개, 기둥은 하나씩.
  3. 도형마다 `BlocksWalk`·`BlocksProjectiles`·`IsBoundary`. 걷기만 막는 칸(`w`·`W`)은 `BlocksWalk` 만, 투사체만 막는 칸(`m`·`M`)은 `BlocksProjectiles` 만.
     **원본 사실**(v0.4, diabdat 직접 확인): L1 앞둘레 턱 메가타일 18·19·20·24 는 하위 칸 4개가 모두 SOL 15(Solid·BlockLight·BlockMissile·Transparent) = `X` 다 — 걷기도 투사체도(시야도) 막는
     하위 칸 두 줄 두께의 벽이다. 뒷벽(메가타일 1·2)은 한 줄만 막히고 나머지 반은 바닥이다. 그래서 둘레에는 걷기만 막는 칸(`w`·`W`)이 없다(v0.6: 안쪽 방의 창살이 `w` 를 처음 쓴다, 6.11). "낮은 턱은 투사체 통과" 는 원본과 다르므로 쓰지 않는다
     (기획 v0.5 10.1·10.2 도 같은 사실로 고쳤다 — 앞 턱은 보기에만 낮은 두 줄 두께 벽, 기획 의도 차이는 MVP 에 없음). 투사체가 앞둘레를 넘어야 할 기획 이유가 생기면 그때 성질 표에 `design_diffs`(이유 필수, `sol_known_diffs` 와 따로)를 더한다(6.11).
  4. 맵 경계 = 바깥 마름모(4.1). 밖은 맵 밖이고 입장 중인 유닛만 지나간다(기획 2.4).
     **둘레 도형**: 타일셋 `borders` 와 문 끝 조각(`end_a`/`end_b`)에서 온 막힌 칸의 도형은 `IsBoundary = true` 다. 장애물 통과 유닛(`phase_through`)도 이 도형에는 막힌다(4.7-3).
     → 핀드가 기획 8.3 "안쪽 장애물만 무시, 둘레 벽(타일 테두리 칸 전부)과 맵 바깥은 막힘" 대로 둘레 벽 칸 안으로 들어가지 않는다(벽 기둥에 가려 안 보이고 투사체로 못 맞히는 자리에서 주인공을 때리는 일을 막는다).
     (v0.6) 둘레 표시는 **메가타일 자리**(테두리 줄·문 끝)로 정한다 — 안쪽 방의 바깥 벽을 둘레에 겹쳐 놓아(`over_border`, 6.11) 번호가 바뀌어도 그 칸은 둘레다(그 너머는 맵 밖이라 핀드가 들어갈 곳이 아님).
     안쪽 방의 나머지 벽·짧은 벽·창살은 안쪽 장애물이라 핀드가 넘는다(기획 8.3·열린 질문 30).
  - 같은 목록에서 ① 충돌 해소(정확한 도형) ② 내비 격자 clearance ③ 투사체 막음 ④ `--debug-nav shapes` 를 만든다. 그림은 이 목록으로 만들지 않는다(가져오기 도구가 같은 배치로 굽는다).
  - 시야 막힘(성질 대문자, v0.5)은 도형을 만들지 않는다. 시야는 하위 칸 단위로 보므로 `VisibilityField` 가 `ArenaGrid.BlocksSight` 를 칸 그대로 읽는다(4.11). `ArenaCollision` 은 바뀌지 않는다.
- 흙처럼 그림이 32px 떠 보이는 막힌 칸도 충돌은 그 칸 자리다(기획 10.1, 보이는 것과 32px 어긋나는 것이 정상).
- 원작의 "대각선 걸음은 두 옆 칸이 비어야 함" 은 두지 않는다 — 다각형 충돌과 격자의 코너 자르기 금지가 대신한다(기획 10.1).
- **격자**: 칸 16px, **외곽 상자 전체**. 스테이지 1 160×88 = 14,080칸(통과 약 5,400), 스테이지 2 176×96 = 16,896칸(통과 약 6,500).
  마름모 밖 칸은 clearance 가 음수(맵 밖)라 처음부터 막힘이다. 통과 칸 수가 v0.2(6,144)와 비슷해 필드·A* 비용도 비슷하고, 배열만 상자 크기로 잡는다.
  가장 작은 유닛 반경 9, 통로 80px = 5칸이라 충분히 곱다. 8px 는 필드 비용이 4배인데 얻는 것이 적다.
- **clearance 장(여유 거리)**: 칸 중심에서 가장 가까운 걷기 막힘 도형·맵 경계(바깥 마름모)까지의 **부호 있는 거리**(px, 도형 안·맵 밖이면 음수)를 정확히 계산해 `float[]` 로 둔다.
  도형 25개 안팎(v0.6 안쪽 방 배치로 40~60개 — 짧은 벽 줄·방 벽이 직사각형 몇 개씩, 여전히 같은 자릿수) × 칸 1.7만 ≈ 40만~100만 번 거리 계산 → `ObstacleIndex`(64px 칸) 로 후보를 줄여 스테이지 시작 때 수 ms. 장애물이 움직이지 않으므로 한 번만 만든다.
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
- **가까우면 직진**: 주인공까지 200px 안이고 길이 트이면(4.5 길 트임 검사, 자기 반경) 필드 대신 주인공 쪽으로 곧장 간다. 길 트임 검사는 비싸므로 적마다 6틱에 한 번(`id % 6` 으로 분산), 결과를 `NavAgentState` 에 저장해 둔다.
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
- **경로 다듬기**: 끈 당기기(string pulling). 현재 점에서 길 트임 검사(4.5, 자기 반경)가 통과하는 가장 먼 경유점으로 건너뛴다. 결과는 꼭짓점 몇 개짜리 꺾은선.
- **따라가기·재계획**: 경유점 8px 안이면 다음으로. 재계획 조건: 0.5s 지남, 목표가 마지막 목표에서 32px 넘게 움직임, 끼임 감지. 목표까지 길이 트였으면 A* 없이 곧장 간다.
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

- `Navigator` 가 적마다 쓰는 상태(`NavAgentState`: 길 트임 캐시, 끼임 타이머·비켜 가기 방향, 경로 경유점·번호·재계획 시각, 막힘 타이머)는 WP2 조각 파일 `Enemy.Nav.cs` 에 둔다.
- `Navigator` 는 "희망 방향으로 실제 이동이 0.5s 넘게 막힘" 을 공통 필드 `Enemy.MoveBlockedTime` 에 적는다. `kite` 가 물러날 수 없을 때 제자리에서 쏘는 판정(기획 8.3), 돌진이 장애물에 닿아 끝나는 판정에 쓴다.

### 4.5 선분 검사 — 길 트임·투사체 길

v0.4 까지 "시야 검사" 라 부르던 것이다. 주인공 시야(안개, 4.11)와 헷갈리지 않게 v0.5 에서 **길 트임**으로 이름을 바꿨다(기획 10.7 "용어" — 시야는 시야 막힘 칸 기준, 길 트임은 걷기 clearance 기준). 함수 이름은 그대로다.

- `INavWorld.HasClearLine(a, b, radius)`(길 트임): 선분을 4px 간격으로 걸으며 그 점의 clearance(이중선형) ≥ radius 인지 본다. 플로우 직진 판단·A* 다듬기·돌진 시작 조건·분열체 튕김에 쓴다.
- 투사체 막음은 격자가 아니라 **정확한 도형**으로 한다: `ObstacleIndex`(64px 칸마다 걸치는 도형 번호 목록)로 후보를 고르고 선분-도형 교차 검사. 대상은 `blocks_projectiles` 도형만.
  원거리 적의 "쏠 수 있나" 도 같은 함수(`INavWorld.ProjectileLineClear(a, b)`)로 본다. v0.5: 쏘려면 이것에 더해 주인공에게 **또렷이 보여야** 한다(4.11·6.6).
- 셋(길 트임·투사체 길·시야)은 서로 다른 성질을 본다: 길 트임 = 걷기 막힘 도형의 clearance, 투사체 길 = 투사체 막힘 도형, 시야 = 시야 막힘 칸(성질 표 대문자, 4.11).
  L1 기둥은 앞 둘을 막고 시야는 막지 않는다(원본 SOL 13).

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
- 틀린 예상이면 10.3 측정으로 찾아 4.3 의 길 트임 검사 주기·이웃 수 상한부터 줄인다.
- 시야 다시 계산(4.11)은 주인공이 다른 칸에 들어선 틱에만 돌아 1초에 서너 번이고, 한 번에 칸 700개 안팎 × 정수 선분 — 수십 µs 로 본다. 위 틱 예산 안에 든다(WP1c 가 `Perf` 로 잰다).

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

**자리 고르기의 길 거리 조건** (v0.6, 기획 10.1 "자리 고르기" — 안쪽 방이 생겨 "가깝지만 벽 너머" 자리가 생긴다)

```csharp
public readonly record struct PlaceReach(Vector2 From, float Detour, bool FromHero = false);
// From = 기준(주인공 또는 부른 적), Detour = rules.spawn.path_detour(1.5), FromHero = 기준이 주인공(v0.6.1 — 어느 길 함수를 쓸지 부르는 쪽이 정함)
public static bool TryPlace(INavWorld nav, Vector2 center, Vector2 wish, float bodyRadius, PlaceReach reach, out Vector2 placed);
// 위 TryPlace 의 각 후보에 조건 하나를 더한다: 길 길이(아래) 가 유한해야 함. maxLength = |후보 − From| × Detour.
//   FromHero 면 nav.PathLengthToHero(후보, bodyRadius, maxLength), 아니면 nav.PathLength(후보, reach.From, bodyRadius, maxLength).
// 아니면 다음 후보(20px 당김), 끝까지 없으면 false(생략).
// INavWorld (v0.6, 계약 WP1d — v0.6.1 에 둘을 나눔)
float PathLength(Vector2 from, Vector2 to, float radius, float maxLength);        // 자른 A* 길 길이(px). 없거나 maxLength 를 넘으면 +∞
float PathLengthToHero(Vector2 from, float radius, float maxLength);             // 주인공까지. 필드를 쓸 수 있으면 필드 거리(아래), 아니면 PathLength(from, Hero.Pos, …)
bool TryPathStep(Vector2 from, Vector2 to, float radius, out Vector2 next);     // (v0.6.1) 반경 radius 의 A* 길에서 from 다음 꺾임점. 길이 없으면 false — 자동 조작 방 들르기(9절)
```

- 쓰는 곳: 포위 고리(`ring` 이벤트 — 기준 주인공), `summon`(기준 부른 적), `telegraphed_blast` 지점(기준 주인공, 몸 반경 0 — 막힌 칸이면 가장 가까운 걸을 수 있는 칸으로), 정예 무리·호위 대형(기준 대장).
  분열 튕김(`Throw`)은 쓰지 않는다(죽은 자리 바로 옆, 기획 10.1). 일반 생성(생성 띠)도 쓰지 않는다 — 띠는 화면 밖이고 방 안 생성은 허용이다(기획 2.4).
- 구현(WP2): `PathLength` 는 `maxLength` 로 자른 A*(4.4 와 같은 격자·코너 자르기 금지, 펼침 수 상한). `PathLengthToHero` 는 **`radius` ≤ 등급 반경인 가장 작은 플로우 필드 등급(S 16·M 26)이 있고 후보 칸이 그 필드의 통과 칸일 때만**
  그 필드 거리(4.3, 이미 있음 — 비용 0)를 쓰고, 아니면(반경 34, 필드가 못 닿는 칸) 자른 A* 로 넘긴다. 반경 0(예고 지점)은 S 필드다. 기준이 주인공인지를 좌표 비교로 짐작하지 않는다(v0.6.1).
  거리는 격자 비용(10/14)을 px 로 바꾼 값이다. 조건이 "1.5 배" 로 넉넉해 격자 근사로 충분하다. 길 트임(`HasClearLine`)이 참이면 직선 거리 그대로(검색 없음).
  `TryPathStep` 은 4.4 의 A*·끈 당기기를 그대로 쓰고 결과를 맡지 않는다(부르는 쪽이 0.5초마다 다시 부름).
- 결정성: 후보 순서·당김 간격이 고정이고 `PathLength` 가 결정적이라 생략 수도 결정적이다. 값 1.5 는 `rules.spawn.path_detour`(WP4 파일) 한 곳.
- "맵 안" 은 바깥 마름모 안이다(4.1). 마름모 아레나에서는 주인공이 모서리 쪽에 있으면 고리의 상당 부분이 맵 밖이라 반경 줄이기가 자주 일어난다 — 그래도 규칙은 같다(기획 2.4).

### 4.11 시야(전장의 안개) `VisibilityField` (v0.5, 기획 10.7, 사용자 요구 11)

시야는 원거리 적의 사격·돌진 시작을 정하므로 **시뮬레이션 상태**다(Core `Sim/Visibility/VisibilityField`, `World.Visibility`). 그리기(7.10)·미니맵(8.2)은 읽기만 하고,
자동 조작은 읽지 않는다(봇은 안개를 무시, 기획 15절). 그래서 같은 씨앗이면 `--reveal`·`--time-scale`·소리와 상관없이 결과가 같다.

**격자 — 하위 타일 칸** (16px 내비 격자가 아니다)

1. 시야 막힘은 원본 SOL 의 하위 타일 성질(bit1 BlockLight)이다. 칸이 같으면 옮김 오차가 없다 — 16px 사각 격자는 마름모 칸을 근사해 벽 모서리에서 칸 조각만큼 새거나 막힌다.
2. 칸 중심이 정수 타일 좌표(4.1)라 "주인공 칸 중심 → 칸 중심" 선분의 끝점이 정수다 → 지나는 칸을 **정수만으로** 센다. 부동소수가 없어 구성(Debug/Release)에 따라 달라질 일도 없다.
3. 칸 수가 1,600(스테이지 1)·1,936(스테이지 2)으로 16px 격자(1.4만~1.7만)의 약 1/9 이고, 가 본 칸 비트맵이 구운 미니맵(칸 하나 = 4×2px)과 같은 단위로 맞물린다.
4. 칸 경계의 계단은 그리기의 이중선형·경계 흐림(7.10)이 지우고, 반경은 칸이 아니라 실제 px 거리로 잰다 — 16px 정밀도는 화면에서 이득이 없다.

- 범위: 맵 N×N 둘레에 **P = 20칸 띠를 더한 확장 격자**(스테이지 1 80×80, 스테이지 2 84×84). 맵 밖 칸은 시야를 막지 않으므로(기획 10.7) 문 틈 밖에서 걸어 들어오는 적도 같은 규칙으로 보인다.
  칸 (dx, dy) 만큼 떨어진 칸의 화면 거리 `|(32(dx − dy), 16(dx + dy))|` 는 max(|dx|, |dy|) = k 일 때 최소 약 28.6k px 다. 주인공은 늘 둘레 벽 안쪽(맵 가장자리에서 2칸 이상)이라
  확장 격자 밖 칸은 k ≥ 22, 화면 거리 ≥ 630px 로 반경 상한 480(13.5-15) + 64 보다 멀다. 확장 격자 밖 자리는 "안 보임".
- 가 본 칸 비트맵은 N×N 만이다(맵 밖은 가 본 곳이 되지 않는다 — 기획 10.7).
- 시야 막힘 칸 = `ArenaGrid.BlocksSight(tx, ty)`(성질 표 대문자 글자, 6.11). 격자 밖(`Outside`)은 걷기·투사체는 막아도 시야는 막지 않는다.

**선 — 주인공 칸 중심에서 칸 중심까지 정수 선분**

- 칸 c 가 시야 안(`InSight`) = 주인공 발이 있는 칸 h 의 중심에서 c 의 중심까지 선분이 지나는 칸 가운데 시야 막힘 칸이 없다. h 와 c 자신은 세지 않는다(막힘 칸도 선이 닿으면 보인다 — 벽 면, 기획 10.7).
- 지나는 칸 = 선분 내부가 지나는 칸 전부(supercover). 선분이 **칸 꼭짓점을 정확히 지나면** 그 꼭짓점 양옆 두 칸이 **둘 다** 막힘일 때만 막힌다 —
  꼭짓점끼리 붙어 대각선으로 이어진 벽은 새지 않고, 기둥 하나의 모서리를 스치는 선은 막히지 않는다. `[해석]`(기획은 꼭짓점 규칙을 정하지 않았다)
- **두 줄 벽은 함께 보인다**(v0.5.1): 걷기·시야를 둘 다 막는 칸(`W`·`X`)은, **같은 메가타일**(4.1 — 하위 칸 (tx, ty) 의 메가타일 = (tx >> 1, ty >> 1))의 `W`·`X` 칸 하나가
  위 선 규칙으로 `InSight` 이면 함께 `InSight` 다. 한 번만 적용하고 옆 메가타일로 퍼지지 않는다. 가 본 칸 표시(아래)도 같은 규칙을 따른다. `[해석]`
  - 이유: 앞 턱 18·19·20·24 와 모서리·문 끝 조각(`XXXX`·`XXX.`), 스테이지 2 의 두 줄 협로 벽은 바깥 줄로 가는 선이 반드시 안쪽 줄(시야 막힘, 끝점이 아님)을 지나고
    꼭짓점을 지나도 양옆이 둘 다 막힘이라, 선 규칙만으로는 바깥 줄이 영원히 안 보인다 — 그림에서 벽 하나가 밝은 반·검은 반으로 갈리고 미니맵에서 그 줄이 빠진다(7.10).
  - 막힌 칸에는 적이 서지 않으므로(`phase_through` 의 핀드도 근접이라 시야 질의를 쓰지 않는다, 6.6) 이 규칙이 바꾸는 것은 그리기·기억·미니맵뿐이다.
    시야만 막는 `L`·`M`(걸을 수 있음 — 아치 밑 보정 칸)은 넣지 않는다 — 그 칸에 선 원거리 적이 옆 칸 덕에 "보임" 이 되면 안 된다.
- 그림자 던지기(recursive shadowcasting)를 쓰지 않는다. 기획의 정의("칸의 중심까지 가는 선")를 그대로 옮기면 선분 검사이고, 그림자 던지기는 경계 칸 판정이 이 정의와 달라진다. 비용은 아래처럼 작다.
- 계산하는 칸: 주인공 칸 중심에서 그 칸 중심까지 거리 ≤ R + 64 인 칸(발과 발의 칸 중심, 대상 자리와 그 칸 중심이 각각 최대 32px 어긋나므로 R 안의 모든 자리를 덮는다). R 420 이면 700개 안팎, 선 하나는 십여 칸.
  칸마다 `InSight` 와 그 값이 바뀐 스테이지 틱 `ChangedTick` 을 둔다(그리기의 0.15s 바뀜 — 표시 시간은 틱으로 센다, 3.4). 배열은 갱신 번호(stamp)로 지우지 않는다. 할당 0.
  계산 범위 밖으로 나간 칸은 그 틱에 `InSight` 가 거짓으로 바뀐 것으로 친다(범위 안이던 칸 목록을 한 벌 들고 있다가 비교).
- **바뀜 값 잇기**(v0.5.1): F = `round(rules.sight.fade × 60)`(9틱). 칸 값이 틱 t 에 바뀔 때 지난 바뀜에서 지난 틱 수 a = t − `ChangedTick` 이 F 보다 작으면(아직 바뀌는 중)
  `ChangedTick ← t − (F − a)`, 아니면 `t`. → `CellLevel`(아래)이 지금 보이는 값에서 반대로 움직이고 0 이나 1 로 튀지 않는다(int 하나로 됨).
  주인공이 하위 칸 경계에서 적에게 밀려 1px 씩 오가며 벽 그림자 경계 칸의 `InSight` 가 매 틱 뒤집혀도, 그림은 깜빡이지 않고 중간 값 근처에 머문다.
- 선 걷기: 끝점이 정수인 선분이라 두 배 좌표의 정수 걸음(Bresenham 꼴 supercover)으로 칸을 차례로 낸다. 꼭짓점 통과는 걸음의 오차 항이 0 인 자리라 정수로 바로 안다.
  시험은 이 구현을 "칸 사각형 ∩ 선분" 을 유리수로 푼 느린 기준 구현과 무작위 격자에서 비교한다(10.1 `Visibility`).

**갱신 — `BeginTick` 안, 화면 사각형 다음** (3.5 1단계)

- 매 틱: 주인공 위치(`HeroPos`), 반경(`Radius` = `World.Lighting.HeroLightRadius` — `light_change` 의 `hero_light.radius` 가 바꾼다, 기획 10.7), 화면 사각형을 받아 둔다. 질의의 거리는 이 값으로 잰다.
  `light_change` 는 2단계(`Director`)에서 반경을 바꾸므로 시야는 **다음 틱부터** 새 반경이다(그 틱의 질의는 옛 반경 — v0.5.1). 그리기(7.10)의 경계 흐림도 `Visibility.Radius` 를 쓴다.
- 선 다시 계산: 첫 틱, 주인공 칸이 바뀐 틱, 반경이 바뀐 틱. 선 결과는 주인공 칸과 반경에만 기대므로 이것으로 정확하다 — 기획의 "늦어도 6틱" 보다 늘 최신이다.
- 가 본 칸 표시: 선을 다시 계산한 틱과 `Tick % rules.sight.refresh_ticks == 0`(6틱)에, N×N 안이고 `InSight` 이고 칸 중심이 `HeroPos` 에서 R 안인 칸을 가 본 칸으로 한다
  (두 줄 벽 규칙: 같은 메가타일의 `W`·`X` 칸 하나가 가 본 칸이 되면 그 메가타일의 `W`·`X` 칸 모두). 새 칸이 생기면 `ExploredVersion` 을 올린다(미니맵이 가림을 다시 굽는 신호, 8.2).
- 시작(v0.5.1): `World` 생성자는 계산하지 않는다 — `IRunSetup`·`TestWorldBuilder.Setup` 이 생성 뒤에 주인공을 옮기므로, 생성자에서 표시하면 원래 시작 자리 둘레가 가 본 칸으로 남는다.
  **첫 `BeginTick`(Tick 0)** 이 첫 계산과 가 본 칸 표시를 한다(첫 틱의 시야만 열림. `--start-at` 도 그 자리부터). 첫 틱 전에 그리는 프레임이 있으면 안개는 전부 검정이다.
  이 첫 계산의 `ChangedTick` 은 `−F` 로 둬 첫 화면이 0.15s 에 걸쳐 밝아지지 않게 한다(7.10).
  판이 끝나면 `World` 를 버리므로 기억도 없어진다(결과 화면·다시 하기·스테이지 바꾸기, 저장 안 함 — 기획 10.7).
  v0.7: `World` 가 방마다 새로라(3.6) **방에 들어갈 때마다 가 본 곳이 비고**(기획 10.7·10.8) 시작 자리는 들어온 문의 진입점이다. 따로 지우는 코드는 없다.
- 1단계에 두는 이유: 생성(2)·적 생각(5)이 같은 틱에 같은 시야를 본다. 시야는 주인공 위치와 맵에만 기대고 적 위치와 무관해 틱 시작 값 하나로 그 틱 규칙이 모두 맞는다.
  새 `SimPhase` 를 더하지 않는다(WP1 순서표 그대로, `BeginTickSystem` 이 부른다).

**질의** — 시뮬레이션 규칙과 그리기가 같은 뜻으로 쓴다(기획 10.7 "용어").

```csharp
public sealed class VisibilityField              // Core Sim/Visibility (계약·구현 WP1c, 그 뒤 WP2 소유) — World.Visibility
{
    public bool Enabled { get; }                  // 맵(World.Arena)·주인공 빛(반경 > 0)·rules.sight 가 다 있을 때. 거짓은 맵 없는 시험 월드뿐이다
                                                  //   (스테이지가 있는 데이터는 빛 없음·반경 < 380 을 13.5-15 가, rules.sight 를 6.14 가 막는다). 꺼지면 질의는 늘 true, ClearReach 는 +∞
    public float Radius { get; }   public Vector2 HeroPos { get; }   public int Margin { get; }   // Margin = P(20)
    public SightRulesDef? Rules { get; }   public int FadeTicks { get; }  // rules.sight(6.10, 없으면 null — Enabled 거짓), F = round(fade × 60)
    public RectF ViewRect { get; }   public Int2 CellOf(Vector2 pos);       // 틱 시작 화면 사각형, 자리 → 칸(v0.5.2, WP1c 코드)
    public bool IsVisible(Vector2 pos);           // 보임: |pos − HeroPos| ≤ R 이고 pos 의 칸이 InSight
    public bool IsClearlyVisible(Vector2 pos, float top);   // 또렷이 보임: 보임 + |pos − HeroPos| ≤ R − rules.sight.edge(40)
                                                  //   + 발 pos 와 몸 위 끝 (pos.x, pos.y − top) 이 둘 다 ViewRect 를 rules.sight.view_inset(16)만큼 줄인 사각형 안. top = 몸 높이(BodyTop, 아래)
    public float ClearReach(Vector2 dir, float top);        // L: HeroPos 에서 방향 dir 로 "또렷이 보임" 의 거리·화면 조건(벽 가림 제외)을 지키는 가장 먼 거리
                                                  //   = min(R − edge, 그 방향 반직선이 위 안쪽 사각형(위 변은 top 만큼 아래로)을 나가는 거리). kite 띠 당기기(6.6)
    public bool InSight(int tx, int ty);   public int ChangedTick(int tx, int ty);              // 확장 격자(맵 밖 포함) — 그리기·디버그
    public bool IsExplored(int tx, int ty);   public int ExploredCount { get; }   public ulong ExploredHash { get; }   public int ExploredVersion { get; }   // N×N 만
    internal void Update(World world);            // BeginTick
}

public static class FogMath                       // Core Sim/Visibility (구현 WP1c, 그 뒤 WP5 소유 — 그리기 규칙) — 그리기·HUD·공정성 계측이 같이 쓰는 안개 알파(7.10)
{
    public static float CellLevel(VisibilityField f, int tx, int ty, int stageTick);   // 0..1, 칸 보임이 바뀐 뒤 F 틱에 걸쳐 바뀜(바뀜 값 잇기는 ChangedTick 이 함)
    public static float PosAlpha(VisibilityField f, Vector2 pos, int stageTick);       // CellLevel(pos 의 칸) × clamp((R − |pos − HeroPos|) / edge, 0, 1)
    public static float PosAlpha(VisibilityField f, Vector2 pos, int stageTick, Vector2 heroPos);   // 그리기용: 보간된 주인공·대상 위치(아래)
    public static float EnemyAlpha(World w, Enemy e, int stageTick);                   // 1 = 중간 보스·보스, 닿은 적(두 반경 + e.TouchReach), e.Visual.ForceShown. 그 밖 PosAlpha(e.Pos)
    public static float EnemyAlpha(World w, Enemy e, int stageTick, Vector2 heroPos, Vector2 drawPos);   // 그리기용
}
```

- 자리 → 칸은 `IsoMath` 역변환의 반올림(4.1). 질의는 할당 없이 O(1).
- **또렷이 보임의 화면 조건은 몸 위 끝까지 본다**(v0.5.1, 기획 v0.6.1 10.7): 발만 화면 안쪽 16px 이면 화면 위쪽에 선 궁수는 몸(60~90px)이 거의 화면 밖인 채 쏜다.
  그래서 발과 몸 위 끝(발 + 몸 높이)이 둘 다 안쪽 사각형 안이어야 한다. `top` 은 적마다 `Enemy.BodyTop` = 그림 묶음 `body_height`(6.3, 발에서 머리 위 끝까지 px, 미리보기로 맞춤) × 최종 크기(생성 때 WP4 가 씀).
  화면 위아래 한계는 위쪽 270 − 16 − `top`(약 164~194), 아래쪽 254 다. 위쪽 한계가 거리 유지 최소(200~240)보다 작아지므로 `kite` 는 띠를 당긴다(6.6 — `ClearReach`).
- **그리기용 오버로드**(v0.5.1): 빛 원과 카메라는 보간된 주인공 위치를 쓰는데 `HeroPos` 는 틱 시작 값이라 이동 속도 × 1틱(2~3px)만큼 어긋난다.
  그리기는 보간된 주인공 위치와 대상의 그리는 위치를 받는 오버로드를 쓴다(칸 값 `CellLevel` 은 그대로). 시뮬레이션·계측은 틱 값 오버로드를 쓴다.
- `FogMath` 를 Core 에 두는 이유: 그리기(WP5)·미니맵 점(WP6)·공정성 계측(10.1 `InvariantChecker`)이 같은 "보이는가" 를 쓰고, 그래픽 없이 시험할 수 있다. `Enabled` 가 거짓이면 모든 알파가 1 이다.
  소유는 WP5 다(그리기 규칙 — 보스는 늘 보임·닿은 거리·흐림 띠). 시그니처를 바꾸면 계약 변경(11.3).
- **쓰는 곳**: 원거리 사격·`kite`·돌진 시작(또렷이 보임, 6.6), 그리기(7.10)·미니맵 점(8.2), 공정성 계측(10.1), `--debug-nav sight`(9절).
  **쓰지 않는 곳**: 일반 생성 자리(v0.5.1 에서 뺌 — 6.12), 주인공 무기 조준(기획 5.1 — 시야와 상관없음, 열린 질문 23), 상한 정리(화면 밖 기준 그대로, 기획 2.3), 소리(7.9), 자동 조작 `AutoplayPolicy`(봇은 안개 무시), `--stress` 채우기(개발 옵션).
  조준을 "보임" 인 적으로 줄이기로 하면(기획 열린 질문 23) WP3 의 조준 후보 거르기에 `IsVisible` 하나를 더한다.
- 중간 보스·보스, 주인공에 닿은 적, 자폭 점화 중인 적이 "늘 보임" 인 것은 **그리기 규칙**이다(7.10). 시야 질의는 등급을 모른다 — 보스 패턴에 시야 조건이 없는 것은 부품 쪽 규칙이다(6.6).
  "닿은 거리" 는 적마다 `Enemy.TouchReach`(두 반경에 더하는 값 = max(`rules.sight.touch_margin`, 그 적의 단일 대상 근접 사거리 여분 + 타격 여유 10), 생성 때 WP4)다 —
  근접 사거리가 몸 반경 합 + 6 보다 긴 적(13.5-13 은 + 50 까지 허용)이 1칸 벽(두께 약 28.6px) 너머에서 숨은 채 때리지 않게(v0.5.1).
- 결정성: 정수 선분·고정 순회 순서, 사전 없음. `StateHasher`(3.4)에 `ExploredHash`(가 본 칸 비트맵의 `StableHash`)를 넣는다(WP7) — 시야가 실행마다 달라지면 해시가 갈린다.
  `InSight` 는 넣지 않는다(해시된 주인공 위치·반경과 맵의 순수 함수). 그리기 옵션(`--reveal`)은 이 객체를 바꾸지 않는다.

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
- 무기 발동 시 주인공이 서 있으면 그 무기 정의의 `cast_clip`(`cast_fire`·`cast_lightning`·`cast_magic` — 원작 `fm`·`lm`·`qm`, 근접 무기는 `attack` — `at`, v0.6, 기획 4.0·4.5·5.1) 클립을
  **고른 직업의 그림 묶음**(`World.HeroClass.Sprites`)에서 재생(재생 중이면 그대로, 움직이면 바로 걷기). 그림은 보여 주기만 한다 — 발동 시각·피해는 쿨다운이 정하고 클립 길이·이벤트 프레임은 규칙에 쓰지 않는다(직업마다 시전 길이가 달라도 결과가 같다, 기획 4.5).
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
- **장애물**: `blocked_by_obstacles` 이면 같은 선분으로 `ObstacleIndex` 검사(투사체 막힘 도형만 — 걷기만 막는 `w`·`W` 칸은 넘어감, 시야 막힘은 상관없음. MVP 맵의 둘레 턱은 `X` 라 막힌다, 4.2) → 닿으면 폭발/사라짐.
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
- 주인공 죽음(그 직업의 죽기 클립 — 마법사 20프레임, 기획 4.0·4.5)은 결과 화면 시점이 걸려 있어 시뮬레이션 상태로 둔다. 결과 화면 시점은 기획 2.2 의 초 단위 값으로 세고 클립 길이에 묶지 않는다 — 직업마다 죽기 프레임 수가 달라도(수도사 칸 160) 결과 시각이 같다.

---

## 6. 데이터 스키마

### 6.1 형식과 파일 배치 (기획 열린 질문 4 의 답: JSON)

- `src/DiabloDefenceRemastered/Data/` 아래 `**/*.json` 을 모두 읽는다(경로 서수 정렬, 3.4). 파일 하나는 **종류 이름을 키로 하는 객체**이고, 한 파일에 여러 종류를 담을 수 있다.

```json
{ "enemies": [ … ], "projectiles": [ … ], "effects": [ … ] }
```

  종류 키: `rules`(객체, 아래), `sprites`, `sprite_sets`, `effects`, `projectiles`, `tilesets`, `maps`, `enemies`, `affixes`, `buffs`,
  `weapons`, `passives`, `evolutions`, `pickups`, `drop_tables`, `stages`, `sounds`, `music`, `sound_bindings`(v0.3: `tilesets`·소리 셋 추가, `obstacle_kinds` 삭제), `hero_classes`(v0.6, 6.16), `rewards`(v0.7, 6.18).
- `rules` 는 **여러 파일에서 하위 키 단위로 합친다**: `rules/hero.json` = `{ "rules": { "hero": …, "xp_curve": …, "level_up": … } }`,
  `rules/enemies.json` = `{ "rules": { "ranks": …, "enemies": …, "caps": …, "spawn": … } }`, `rules/audio.json` = `{ "rules": { "audio": … } }`, `rules/rooms.json` = `{ "rules": { "rooms": … } }`(v0.7, 6.10). 같은 하위 키가 두 파일에 있으면 오류. 그래서 두 묶음이 같은 파일을 고치지 않는다.
- 권장 배치(소유 묶음은 11절):

```
Data/
  rules/hero.json  rules/enemies.json  rules/audio.json     주인공·레벨업 / 등급·상한·생성 / 소리 규칙
  rules/rooms.json              방 흐름·문·넓이 배율 규칙(v0.7, 6.10)
  rewards.json                  문 보상(v0.7, 6.18)
  heroes/<직업>.json            직업 정의 1개(v0.6, 6.16 — MVP 는 sorcerer.json 하나)
  sprites/monsters.json hero.json missiles.json ui.json     sprites + sprite_sets
  effects/common.json           fx_warning_*, fx_levelup_pillar …
  tilesets/l1.json  l2.json     타일 번호·성질 표 (헬파이어 스테이지 단계에 l5.json·l6.json — 층마다 따로, 6.11)
  maps/cathedral_arena.json  catacombs_arena.json           배치·입구·기준점 — 큰 방(액트 보스 방)
  maps/cathedral_room_a.json …  catacombs_room_c.json …     작은 방 32×32(v0.7, 액트마다 MVP 2장 — 기획 10.8, 단계 2·3)
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
                                     (v0.7) 스테이지 이벤트(PartKind.StageEvent)는 "at" 도 꺼내 PartDef.At(ClockTime, t)에 둔다 —
                                     방 이벤트 옮기기(3.6)·13.5-18·23 이 매개변수 형을 몰라도 시각을 읽게
                                     3단계의 페이즈별 완성 패턴도 같이 바꿔 PhaseDef.Patterns 에 넣는다(WP4 는 이것만 읽는다)
5. 연결 (DefRef<T> 채우기, 한 번만)  없는 ID → "비슷한 ID" 제안(편집 거리 ≤ 2)과 함께 오류. 객체 가중치 → JSON 순서 배열
6. 의미 검사                          13.5 규칙 1~3, 7(정의끼리), 8, 9, 20(정의 쪽)·21(v0.6) + 부품 자체 검사 + 6.14 추가 규칙(변종·클립·배치 이름)
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
    { "id": "set_zombie", "base_speed": 40, "base_radius": 14, "body_height": 72, "frame_time": 0.05,
      "variants": ["", "bluered", "grey", "yellow", "eth"],
      "clips": {
        "idle":    { "sprite": "zombie_idle", "frame_time": 0.2 },
        "walk":    { "sprite": "zombie_walk" },
        "attack":  { "sprite": "zombie_attack", "event_frame": 8 },
        "special": { "sprite": "zombie_special" },
        "die":     { "sprite": "zombie_die" } } },
    { "id": "set_sorcerer", "base_speed": 95, "base_radius": 14, "body_height": 76, "frame_time": 0.05,
      "variants": [""],
      "clips": { "idle": { "sprite": "slt_idle" }, "walk": { "sprite": "slt_walk" },
                 "cast_fire": { "sprite": "slt_cast_fire", "event_frame": 8 }, "cast_lightning": { "sprite": "slt_cast_lightning", "event_frame": 8 },
                 "cast_magic": { "sprite": "slt_cast_magic", "event_frame": 8 }, "die": { "sprite": "sln_die" } } }
  ]
}
```

- **주인공 그림 묶음은 직업마다 하나**(v0.6, 6.16): `set_sorcerer`(MVP), 직업 단계에 `set_warrior`·`set_rogue`·`set_monk`. 같은 클립 이름(`idle`·`walk`·`cast_*`·`die`, 근접 시작 무기 직업은 `attack`)을 쓴다.
  시전 클립의 `event_frame` 이 기획 4.5 의 "발동 프레임"(마법사 8·전사 14·도적 12·수도사 13, 출처: DevilutionX 참고)이고 `frames` 가 시전 길이(파일 값 12·20·16·18)다 — 그림 맞춤 값이라 직업 정의에 따로 두지 않는다(12.1 `[해석]`).
  수도사는 칸 폭이 동작마다 달라 그림(`sprites`)마다 `frame` 이 다르다(112·130·98·114·160). `origin` 도 그림마다 맞춘다(기획 열린 질문 7).

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
    v0.8(형식 3, 사용자 결정 U — 1.4 "AI 확대"): `"scale": 1|2`(그림 픽셀 / 월드 px, 기본 1). 확대한 묶음도 그림자는 시트에 구워져 있다(AI 없이 늘림 — 1.4 ⑥). `cell`·`rects`·`ox,oy` 는 그림 픽셀이고, 정의의 `frame`·`origin` 과 견줄 때 `scale` 로 나눈다(13.5-7).
  - 그리기: 화면 왼쪽 위 = `위치 − origin × scale + (ox, oy) × scale`, 원본 사각형 = `(x, y, w, h)`. 프레임마다 크기가 달라도 칸 기준 `origin` 하나로 발이 맞는다.
    (여기 `scale` 은 정의의 크기 배율이다. v0.8 메타 `scale` m 이 있으면 `(ox, oy)` 와 그린 크기에 `scale ÷ m` 을 쓴다 — 7.1 그림 배율 s 는 그다음에 타깃 변환으로 곱한다.)
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
- `body_height`(v0.5.1, 필수 — 기획 v0.6.1 13.1) = 크기 배율 1 일 때 발에서 머리 위 끝까지 px(대략 60~90). "또렷이 보임" 의 화면 위쪽 한계(4.11)와 `kite` 띠 당기기(6.6)가 쓴다. 첫 값은 WP0a 가 단계 1 묶음에 어림으로 적고 WP5 가 `--preview enemies` 로 맞춘다(기획 열린 질문 7). 계약 `SpriteSetDef.BodyHeight`(WP1c #11).
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
- `requires`(v0.6, 6.17): 헬파이어 몬스터는 `["hellfire"]`(펠트윈·스컬윙·본 데몬도 — 그림이 diabdat 여도). 이 적을 내는 스테이지의 `requires` 안이어야 한다(13.5-20). MVP 적은 모두 빈 목록.
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
(v0.5) `shoot_projectile` 은 거리·쿨·투사체 길에 더해 **주인공에게 또렷이 보일 때만** 쏘고, `kite` 는 띠 안이어도 또렷이 보이지 않으면 다가간다(6.6, 기획 8.1·8.3).
매개변수로 두지 않고 부품 규칙으로 고정한다 — 데이터로 끌 수 있으면 "보이지 않는 곳에서 맞지 않음"(기획 10.7 공정성)이 조용히 깨진다. `range_band` 최대 + 60 ≤ 빛 반경은 13.5-15 가 본다.

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
  전투 함성(`war_cry`, 오버로드 반경 250)·스틸메이스 함성(`buff_area`, 반경 300)도 중간 보스·보스 본체에 걸리지 않는다 — 사용자 결정으로 확정(v0.5, 기획 4.2·8.3·9.4, 열린 질문 21 닫힘).
  스틸메이스는 `rank: miniboss` 라 자기 함성도 자신에게 걸리지 않는다(같은 규칙). MVP 데이터에는 `include_bosses: true` 가 없다. 매개변수는 E 단계용으로 남긴다.
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
      "sprites": "set_sking", "variant": "genrl", "hp": 9000, "speed": 55, "damage": 30, "armor": 0,
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
(v0.5.3) 해골왕 `hp` 는 기획 v0.6.2 의 9000 이다(도살자 15000 → 18000 — 중간 보스가 늘어 보스 전 상자 강화가 늘었다, 기획 9.4·9.5).

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

**중간 보스 — 패턴 틀 + 일반 부품** (v0.5.3, 기획 v0.6.2 9.4. 부품만 발췌)

```json
{ "id": "blackash", "name": "불타는 블랙애시", "tags": ["undead", "fire"], "rank": "miniboss",
  "sprites": "set_skelbow", "variant": "bashtb", "glow": { "color": [255, 120, 40], "alpha": 0.2 }, "scale": 1.5, "radius": 18,
  "hp": 2100, "speed": 62, "damage": 0, "armor": 1, "knockback_resist": 0.9, "xp": 150, "movement": "astar",
  "immunities": ["slow"], "show_boss_bar": true, "time_scaling": false, "drop_table": "dt_miniboss",
  "behaviors": [ { "type": "kite", "range_band": [220, 320] } ],
  "patterns": [
    { "id": "raise_60", "hp_once": 0.6, "clip": "special",
      "actions": [ { "type": "summon", "spawns": [ { "enemy": "burning_dead", "count": 4 } ], "radius": 140, "alive_cap": 12 } ] },
    { "id": "raise_30", "hp_once": 0.3, "clip": "special",
      "actions": [ { "type": "summon", "spawns": [ { "enemy": "burning_dead", "count": 4 } ], "radius": 140, "alive_cap": 12 } ] },
    { "id": "fire_rain", "cooldown": 8.0, "clip": "attack", "clear_sight": true,
      "actions": [ { "type": "telegraphed_blast", "points": { "hero": true, "around": 3, "radius": 110 },
                     "telegraph": { "effect": "fx_warning_circle", "time": 0.8 }, "radius": 55, "damage": 18, "effect": "fx_fire_explosion" } ] },
    { "id": "fire_fan", "range_band": [0, 340], "cooldown": 2.8, "clip": "attack", "clear_sight": true,
      "actions": [ { "type": "projectile_spread", "projectile": "pj_fire_arrow", "count": 5, "arc": 60, "aim": "hero", "damage": 10 } ] } ] }

// 데스스핏 산성 자국 — 클립 없는 쿨 동작은 behaviors 의 trigger: cooldown(로트피스트 독 웅덩이와 같은 꼴, 기획 8.3)
{ "type": "ground_hazard", "trigger": "cooldown", "cooldown": 2.0, "effect": "fx_acid_pool", "radius": 45, "duration": 4.0,
  "tick": 0.5, "damage": 5, "cap_group": "acid", "cap": 20 }
// 슬러지 남작 죽으면 분열 — 썩은 시체와 같은 부품, 나올 적만 다름
{ "type": "split_on_death", "spawn": "mud_man", "count": 3, "throw": 80, "delay": 0.3 }
```

- **`clear_sight`**(패턴 조건, v0.5.3 — 기획 13.3 "또렷이 보임"): `true` 면 `PatternAbility.TryBegin` 이 다른 조건에 더해 `EnemyCtx.SeenClearly` 를 보고, 이벤트 프레임에 한 번 더 본다(6.6).
  겨누어 멀리 쏘는 중간 보스 패턴에 단다(블랙애시 `fire_rain`·`fire_fan`, 데스스핏 산성 침 부채). 사방(`arc: 360`)·근접·장판 패턴에는 달지 않는다(기획 9.4). 계약 `PatternDef.ClearSight`(WP1d).
- 중간 보스도 `behaviors` 와 `patterns` 를 함께 가진다(정의 형이 이미 둘을 따로 가진다). 슬롯 = 정의 부품 + 패턴 합집합: 블랙애시 5(`kite` + 4), 데스스핏 3, 슬러지 남작 3(패턴 2 + 죽음 훅) — 12 안.
  `kite` 의 "다가감"(`SeekHero`)은 정의 `movement` 대로 A* 를 탄다(4.4 `MoveIntent`), 물러남(`Away`)·멈춤(`Hold`)은 일반 궁수와 같다.
- `rank: miniboss` 의 `damage` 는 쓰는 근접 부품이 없으면 0 이다(블랙애시·데스스핏 — 몸이 닿아도 피해 없음, 기획 9.4). 정예 상자·금화·룬은 드롭 표 `dt_miniboss`(WP3)가 낸다.
- 해골 일으키기 두 번은 `hp_once` 패턴 둘이다(문턱 하나에 한 번씩). 불러낸 불타는 해골은 `summon` 의 등장 연출(`Emerging` 0.4s, 6.6)을 탄다.
  블랙애시에도 `skeleton` 태그를 달지 않는다(위 해골왕과 같은 이유 — 대장 오라·호위 규칙의 대상이 아니다).

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
    bool TryBegin(in EnemyCtx ctx, ref PartSlot slot, ref ActionState action);     // 조건(거리·쿨·길 트임·또렷이 보임·HP 문턱) 확인 후 시작
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
public readonly struct EnemyCtx(World world, Enemy self) { /* 거리·길 트임·SeenClearly·난수(Ai)·적대 대상 질의·ICombatSpawner·피해 요청·버프 붙이기·PlacementResolver 도우미 */ }
```

- **시야 조건**(v0.5, 기획 10.7 공정성): `EnemyCtx.SeenClearly` = `World.Visibility.IsClearlyVisible(self.Pos, self.BodyTop)`(4.11 — v0.5.1: 몸 위 끝까지).
  v0.5.3: v0.5 의 "정의 `rank` 가 `miniboss`·`boss` 면 늘 참" 을 없앴다 — 등급과 상관없이 실제 시야다(기획 v0.6.2 25절 넘긴 것 1). 보스가 시야를 안 보는 것은 **패턴에 조건이 없어서**이고,
  보스 패턴은 `clear_sight` 를 단 것만 본다(6.5). MVP 의 다른 중간 보스·보스는 `shoot_projectile`·`kite`·`charge` 를 쓰지 않아 바뀌는 것이 없다. `[해석]` 이것을 쓰는 곳은 넷이다.
  - `shoot_projectile.TryBegin`: 거리·쿨·`ProjectileLineClear` 에 더해 `SeenClearly`. v0.5.3: **이벤트(발사) 프레임에 `SeenClearly` 를 한 번 더 보고, 거짓이면 그 발사만 버린다**(클립은 끝까지, 쿨은 씀).
    시작과 발사 사이(활 클립 약 0.5s)에 주인공이 움직여 안 보이게 될 수 있는데, 공정성 `unseen_shots`(10.1)는 투사체가 생기는 순간을 센다 — 기획 8.1 "또렷이 보일 때만 쏜다" 를 쏘는 순간에 맞춘다. `[해석]`
  - `PatternAbility`(v0.5.3): 패턴에 `clear_sight` 가 있으면 `TryBegin` 조건에 `SeenClearly`, 이벤트 프레임에 다시 보고 거짓이면 그 패턴의 `actions` 를 하나도 `Begin` 하지 않는다(위와 같은 규칙).
    `clear_sight` 패턴이 만드는 투사체는 `EnemyProjectileSpawn.SightGated = true`(WP1d) — 공정성 계측이 이 발사를 보스 제외에서 빼고 센다(10.1).
  - `kite.TryPosition`(v0.5.1 — 기획 v0.6.1 8.3 "띠 당기기", 떨림 막기 R29): L = `World.Visibility.ClearReach(normalize(self.Pos − HeroPos), self.BodyTop)`(그 방향으로 또렷이 보이는 가장 먼 거리, 벽 제외).
    쓰는 띠 = 최대' = min(최대, L − 10), 최소' = min(최소, 최대' − 40). ① `SeenClearly` 가 거짓이거나 거리 > 최대' → `SeekHero`(다가감). ② 거리 < 최소' → 물러남. ③ 그 밖 → 멈추고 쏨.
    띠 폭이 늘 40 이상이라 주인공이 조금 움직여도 쏘기·이동을 오가지 않는다. 양옆은 L = R − 40 ≥ 최대 + 20 이라 띠가 그대로이고, 화면 위아래 쪽에서만 당겨진다(위쪽 한계 약 164~194 가 띠 최소보다 작아도 쏠 자리가 있다).
    L 은 틱마다 그 적 방향으로 한 번(사각형 네 변과 원 하나의 교점 — 할당 없음). 물러날 수 없을 때(0.5s 못 움직임) 제자리 사격은 그대로이고 `shoot_projectile` 쪽 조건을 탄다.
    `kite` 상태가 바뀔 때마다 `World.Fairness.KiteSwitches` 를 1 늘린다(계측, 10.1).
  - `charge.TryBegin`: 길 트임(`HasClearLine`)·거리 ≤ 260 에 더해 `SeenClearly`. 돌진 중에 안 보이게 되어도 끊지 않는다(시작 조건일 뿐).
  `kite` 는 등급과 상관없이 위 규칙 그대로다 — 중간 보스 블랙애시(기획 9.4)도 안 보이면 다가가고 띠를 당긴다(`BodyTop` = `body_height` × 최종 크기 1.5).
  `clear_sight` 가 없는 보스 패턴(`PatternAbility`)·근접(`chase_melee`·`melee_slam`)·`self_destruct`·오라·함성에는 시야 조건이 없다(기획 9.5 — 보스는 늘 보이고, 근접·자폭은 닿아야 맞는다).
  대신 그리기가 닿는 적을 늘 보인다: 근접은 `Enemy.TouchReach`(4.11), 자폭은 **점화 중 `Visual.ForceShown = true`**(v0.5.1 — 점화 뒤 주인공이 벽 모서리를 돌아도 반경 60 폭발 전에 번쩍임 예고가 보인다, 기획 12.6).
  E 단계에 멀리서 때리는 새 부품을 만들면 같은 `SeenClearly` 를 쓴다(6.13 "새 부품" 규칙). 시험은 10.1 `Enemies`, 전체 계측은 10.1 `InvariantChecker` 의 공정성 열.
- **`Enemy.BodyTop`·`Enemy.TouchReach`**(v0.5.1): 생성 때 한 번 쓴다(WP4). `BodyTop` = 그림 묶음 `body_height`(6.3) × 최종 크기(px, Core 정의만으로 계산 — 그림 파일 없이 됨).
  `TouchReach` = max(`rules.sight.touch_margin`, 능력 부품 중 단일 대상 근접(`chase_melee`, `radius` 없는 `melee_slam`)의 사거리 − 두 반경 + 10)(기획 v0.6.1 "닿는 거리"). 보스·중간 보스는 늘 보이므로 계산만 하고 쓰지 않는다.

- **훅 어댑터**: `behaviors` 원소에 `trigger` 가 있으면 그 `type` 을 `EnemyAction` 에서 찾아 `TriggeredAction` 으로 감싼다.
  `on_death` → `IDeathHookPart`, `on_spawn` → `ISpawnHookPart`, `cooldown` → 클립 없는 `IAbilityPart`. 접두사 `fire_enchanted` 의 `telegraphed_blast`, `black_death` 의 `ground_hazard` 가 보스와 같은 동작 코드를 탄다.
  (v0.5.3) `cooldown` 훅은 **그 틱에 끝나는 즉시 동작**이다: `TryBegin` 이 `Begin` 까지 하고 독점 행동을 남기지 않는다 → 같은 틱 7번(이동 의도)이 그대로 돈다.
  로트피스트 독 웅덩이·데스스핏 산성 자국(기획 9.4 — 표에는 패턴으로 적혔지만 클립이 없다)이 이것이다. 데스스핏은 걸으면서 2s 마다 웅덩이를 남긴다. 다른 독점 행동(시전 클립)이 진행 중인 틱에는 미뤄진다(5번).
- **`PatternAbility`**(데코레이터): 조건(`range_band`·`cooldown`·`hp_once`, v0.5.3 `clear_sight`) → 클립 재생 → 예고가 있으면 `Telegraph` 를 만들고 예고 시간 동안 클립을 이벤트 프레임 앞에서 멈춤 →
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
  (v0.5.3, 기획 9.4 슬러지 남작) 중간 보스의 죽음 훅도 같은 순서다: 13 `DeathHooks` 가 분열(`split_on_death` — 머드맨 3, `Source = Split`, 일반 등급, 그 틱의 시간 배율, 분열·소환 상한 360 의 `SpawnGate`)과 `EnemyKilled` →
  14 `DeathRewards`(WP3)가 드롭 표 `dt_miniboss`(정예 상자·금화·끌어당김 룬) → 히트 스톱 0.08s(`World.Freeze`, 기획 12.6 — 지금까지 거는 곳이 정해지지 않았다. `DeathHooks`(WP4)가 `rank: miniboss`·우두머리 처치에서 그 틱에 건다).
  분열체는 생긴 다음 틱부터 세고(3.5) `delay` 0.3s 를 기다리므로 히트 스톱 동안 움직이지 않는다. 최종 보스 처치로 무너지는(`Collapse`) 슬러지 남작은 분열하지 않는다(기획 2.2·9.4).

**표시 상태 `EnemyVisual`** (기획 8.3 번쩍임·알파, 9.1~9.3 발광)

```csharp
public struct EnemyVisual { public Rgb GlowColor; public float GlowAlpha; public Rgb FlashColor; public float FlashAlpha; public float AlphaMul; public Rgb FootRingColor; public float FootRingAlpha;
                            public bool ForceShown; }   // v0.5.1: 안개와 상관없이 그림(자폭 점화 중, 7.10)
```

- `EnemyThink` 8번에서 정의·등급·접두사·버프·부품이 여기에 쓴다. 렌더(WP5)는 이것만 읽고 부품 종류나 `ActionState` 를 해석하지 않는다 → 새 부품이 렌더 코드를 바꾸지 않는다.
  - 기본: 정의 `glow`·`alpha`. 등급: 정예 파랑 맥동(1.2s, 알파 0.25~0.45 — 스테이지 틱으로 계산), 우두머리 금색.
  - 겹칠 때: **접두사 발광 색 > 등급 발광 색, 맥동 주기는 등급 것**.
  - 부품: `self_destruct` 점화 중 발광 알파 0.35↔0.9·`ForceShown`, `charge` 예고 중 붉은 `Flash`, `phase_through` 가 장애물 위(`Clearance < 0`)면 `AlphaMul` = `alpha_over`(기본 0.3, 핀드 1.0), 오라를 받는 적은 발밑 원(버프 정의의 `foot_ring`).
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
- 아이콘은 `{ sprite, frame?, dir?, tint?, scale?, layers?, frame_border? }`. 무기 아이콘은 가져오기 도구가 원작 마법 아이콘 `ctrlpan\spelicon.cel` 의 칸을 줄이지 않고 옮긴 56×56 한 장(`d1/icons/<무기>.png`, `source: "grid"`, 기획 5.9 — v0.9.1, v0.9 까지는 투사체 프레임으로 구운 48×48)이고,
  화면이 자리 크기(레벨업 카드 48·HUD 무기 칸 32)에 맞춰 그린다. "화살 3장 겹침" 같은 겹침도 도구가 굽는다(1.4). `layers` 는 남겨 두되 MVP 에서 쓰지 않는다. 패시브 아이콘은 원작 exe PNG, 탐욕 아이콘(28px `GoldIcon` 을 48 칸에)은 `scale`.
- `cast_clip`(v0.3, 계약 변경): 서 있을 때 발동하면 재생할 주인공 시전 클립(`cast_fire`·`cast_lightning`·`cast_magic`, 기본 `cast_magic`, 기획 5.1·5.2 표의 시전 그림 열).
  v0.6: 근접 무기(`cleave`·`staff_sweep`, 직업 단계)는 `"cast_clip": "attack"`(기획의 `at` — 클립 이름은 `ClipKind` 그대로 쓰고 원작 글자는 가져오기 표만 안다, 12.1). 클립은 고른 직업의 그림 묶음에서 찾는다(5.3).
- **`classes`**(v0.6, 기획 5.2): 이 무기를 고를 수 있는 직업 ID 목록(`DefRef<HeroClassDef>`), 비면 모든 직업. 레벨업 후보(WP3)·`--weapons`(9절)가 `World.HeroClass` 로 거른다.
  직업 전용 시작 무기만 쓴다(`"classes": ["warrior"]`). 무기 목록 자체는 직업 공통이다(기획 4.5).
- **`requires`**(v0.6, 6.17): 헬파이어 마법 그림(`ms_*`·`ex_*`)을 쓰는 무기의 묶음. 레벨업 후보는 `requires ⊆ (스테이지 requires ∪ 직업 requires)` 인 무기만 낸다 — 그래서 후보가 PC 의 묶음 유무로 바뀌지 않는다(결정성, 6.17). MVP 무기는 모두 비어 있다.
- `melee_arc`(E, 직업 단계 — 기획 13.2): 바라보는 방향 부채꼴(`arc`)·둘레 원(`arc` 360) 근접 피해. 레벨 표 키 `damage`·`cooldown`·`arc`·`reach`, `area_pct` 는 `reach` 에, `projectile_count` 는 휘두르는 횟수(0.15s 간격)에.
  새 발사 패턴 하나(WP3 등록)와 무기 정의 둘이다. 맞음 판정은 `ISpatialQuery.Query(주인공, 몸 반경 + reach + 적 반경)` 후 각도로 거른다. 레벨 표는 직업 단계에 정한다.
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
- (v0.7) **문 보상 물체**: 획득물 `kind: "reward"` — 방 안에 놓이는 마법서 받침·상자·성소·물약(그림 `objects\`·`items\`, 단계 1 은 대체 그림 — 기획 11.4·열린 질문 32). 끌어당김에 안 날아오고 몸에 닿아야 먹히며 수명·상한 정리에서 빠진다(WP3).
  무엇을 주는지는 획득물이 아니라 그것을 가리키는 `RewardDef`(6.18)가 정하고, 놓을 때 `Pickup.Reward` 에 실린다. 정리 끝에 남은 정예 상자와 보상 물체는 `Pickup.GatesExit` 가 참 — 다 집어야 출구 문이 열린다(3.6).
- (v0.7) **축복 패시브**(기획 6절): `"blessing": true` 인 패시브(`bless_divine` 등 여섯). 패시브 칸을 쓰지 않고(`Hero.Blessings` 8칸), 레벨업·채움·마법서·상자 후보에 안 나오고 성소 창에만, 레벨 있음, 진화의 짝이 아님, `trait` 와 함께 참이면 오류(13.5-23).
  능력치는 패시브와 같은 길(4.1)로 더한다(WP3). HUD 패시브 칸에 그리지 않고 일시정지 빌드 목록에 "축복" 으로(WP6).
  **v0.8**(기획 v0.8.1 6절·11.4): 성소가 축복 창을 주지 않게 바뀌어 축복 패시브는 **E**(MVP 데이터에 없음). 형(`PassiveDef.Blessing`·`Hero.Blessings`·`HeroCarry.Blessings`)과 `--passives` 로 주는 길·검사는 남긴다 — E 에서 바로 쓰고, 지금도 시험이 있다.
  축복을 주던 보상 종류 `RewardKind.BlessingCards` 는 없앴다(6.18 — 데이터·길이 없는 계약은 남기지 않는다).
- 능력치 키는 `StatKey` 열거형(기획 4.1 표 그대로, JSON 이름 = 키). 패시브 `per_level` 의 키가 열거형에 없으면 오류(13.5-9).
- 경험치 보석은 드롭 표가 아니라 규칙이다(죽으면 항상 1개, 값 = 적 경험치). 보석 색은 값 범위로 `pickups` 에서 고른다.

### 6.10 레벨업 선택지·규칙 — `rules/hero.json`, `rules/enemies.json`, `rules/sight.json`

레벨업 카드는 따로 정의하지 않는다. 무기·패시브 정의에서 **만든다**. 규칙과 채움 카드만 `rules` 에 둔다. `rules` 는 두 파일에서 하위 키 단위로 합친다(6.1).

```json
// rules/hero.json (WP3)
{
  "rules": {
    "hero": { "class": "sorcerer", "invuln_after_hit": 0.25,                        // v0.6: 그림·반경·시작 무기는 직업 정의로(6.16)
              "stats": { "max_hp": 100, "move_speed": 95, "armor": 0, "regen": 0, "pickup_radius": 60 },   // 모든 직업의 기본값(기획 4.1 = 마법사 값)
              "crowd_slow": { "touching": 3, "mul": 0.6 },
              "belt": { "slots": 2, "threshold": 0.3, "cooldown": 1.0 } },                  // v0.8 허리띠(기획 v0.8.1 11.4): 체력 < 최대 × threshold 인 순간 하나를 저절로(전부 회복), cooldown 초에 한 번
    "xp_curve": [ { "from": 1, "to": 1, "base": 5, "per": 0 },
                  { "from": 2, "to": 19, "base": 5, "per": 10, "origin": 1 },
                  { "from": 20, "to": 39, "base": 185, "per": 30, "origin": 19 },       // v0.8: 기획 v0.8.1 4.3(13 → 30)
                  { "from": 40, "to": null, "base": 785, "per": 40, "origin": 39 } ],   // v0.8: 445 + 16 → 785 + 40
    "level_up": { "cards": 3, "weights": { "owned_weapon": 100, "owned_passive": 100, "new_weapon": 70, "new_passive": 70 },
                  "weapon_slots": 4, "passive_slots": 4, "rerolls": 3, "skip_heal": 10, "input_lock": 0.4,   // 칸 수 = 시작 값(v0.8: 계단마다 rules.rooms.stairs 만큼 +, 상한 6)
                  "guarantee_new_weapon_at": [2, 3],
                  "fillers": [ { "id": "filler_potion", "name": "체력 물약", "heal": 30, "icon": { "sprite": "HealingPotionIcon" } },
                               { "id": "filler_temper", "name": "단련", "heal": 5, "stats": { "max_hp": 5 }, "icon": { "sprite": "…" } } ],   // v0.8: 금화 주머니 → 단련(쌓임, 짐 Mods)
                  "temper": "filler_temper",                                                // v0.8: 0장 보상·보물 상자에 올릴 무기 없음·정예 상자 빈 강화가 쓰는 채움 카드(fillers 안 ID)
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
    "spawn": { "band": [40, 200], "tries": 12, "edge_margin": 4, "group": [1, 3], "group_radius": 40, "accumulator_hold": 3,
               "path_detour": 1.5,                                                     // v0.6: 자리 고르기 길 거리 ≤ 직선 × 이 값(4.10, 기획 10.1)
               "room_pressure": { "after": 8, "every_nth": 3, "min_distance": 150 } } // v0.6.3: 방 체류 압박(6.12, 기획 10.1 첫 값 — 열린 질문 30). 없으면 끔, 있으면 셋 다 필수
  }
}
```

- **`rules.hero`**(v0.6, 계약 변경 WP1d): `class` = 고른 직업이 없을 때 쓰는 직업(MVP·`--hero` 없음·저장 없음 → `sorcerer`, 기본 직업이어야 함 — 13.5-20), `stats` = 모든 직업의 기본 능력치(기획 4.1 표),
  `invuln_after_hit`·`crowd_slow` = 모든 직업 공통. v0.5 의 `sprites`·`radius`·`start_weapons` 는 직업 정의로 옮겼다(6.16). 능력치 = `rules.hero.stats` 를 직업 `stats` 가 키별로 **덮어쓰고**, 그 위에 직업 특성·패시브를 **더한다**(기획 4.1 규칙).
  v0.8: 그 위에 짐의 `Mods`(판 끝까지 — `단련`·성소 대가)를 더한다. 새 능력치 키 `max_hp_pct`(`StatKey.MaxHpPct` — 매혹의 성소 −10%, 최대 체력 = (기본 + 더하기) × (1 + 합), 지금 체력도 같은 비율로).
- **v0.8 계약**(WP1f, 기획 v0.8.1 4.4·11.4): `HeroRulesDef.Belt`(`BeltRulesDef` — `slots`·`threshold`·`cooldown`, 셋 다 `required`, 없으면 허리띠 없음), `LevelUpRulesDef.Temper`(채움 카드 ID — `fillers` 안에 있어야 함, 13.5-1),
  `FillerCardDef.Stats`(`StatKey` → 더함, 짐 `Mods` 에 쌓임). `filler_gold`(금화 주머니)는 데이터에서 빠진다 — MVP 에 금화를 쓸 곳이 없다(기획 v0.8.1 26). 무기·패시브 칸의 그릇은 `WeaponSlots`·`PassiveSlots.SlotCount` 4 → **6**(열린 칸은 `Hero.WeaponSlotsOpen`·`PassiveSlotsOpen`, 짐이 넘김).
  `ChestItemKind.Gold` → **`Temper`**(정예 상자의 빈 강화 = `단련`, 기획 v0.8.1 11.1).
- 카드 문구 `피해 14 → 18` 은 레벨 표 두 행을 비교해 `stat_labels` 로 만든다 → 새 무기도 문구가 저절로 생긴다.
  패시브 카드(`최대 체력 +20`, `피해 +10%`)도 같은 표에서 `StatKey` 라벨과 `format`(`abs` 기본 / `pct`)으로 만든다. 표에 없는 키를 쓰는 무기·패시브는 검증 경고.
- `level_up_push`: 선택 창이 닫힌 다음 스테이지 틱에 주인공 반경 150 안 적을 80px 밀어낸다(피해 0, 기획 12.6).
- `ranks.max_scale`: 생성 때 최종 크기(정의 크기 × 등급 배율) 상한(기획 8.1, 6.5). `enemies.telegraph_required`: 예고가 꼭 필요한 "큰 피해" 기준(기획 12.6, 판정은 6.5, 검사는 6.14 의 13.5-13).

```json
// rules/sight.json (v0.5 — 첫 판 WP0a, 그 뒤 키별 소유는 아래. 기획 10.7·23절 "정한 수치")
{
  "rules": {
    "sight": { "edge": 40, "memory_ratio": 0.5, "fade": 0.15, "refresh_ticks": 6,
               "view_inset": 16, "touch_margin": 16, "ranged_margin": 20 }
  }
}
```

- 시야 반경은 여기 두지 않는다 — 스테이지 조명의 `hero_light.radius` 가 곧 시야 반경이다(기획 10.7, 6.12). 값이 한 곳이라 "밝은 곳 = 보이는 곳" 이 어긋날 수 없다.
- 안개 값은 **모든 스테이지 공통**으로 한 번만 둔다(기획 13.1 "모든 스테이지 같은 값이면 공통 규칙에 한 번"). 스테이지마다 달라야 할 이유가 생기면 그때 스테이지 `lighting.fog` 덮어쓰기를 더한다.
- `edge`: 경계 흐림 폭 px(그리기의 알파·땅 밝기 띠, "또렷이 보임" 의 R − edge). `memory_ratio`: 가 본 곳 밝기 = 빛 없는 바닥 밝기(`ambient × (1 − darkness)`) × 이 값.
  `fade`: 칸 보임이 바뀔 때 그림이 바뀌는 시간(s, 그림만 — 규칙은 바로 바뀐 값). `refresh_ticks`: 가 본 칸 표시 주기(4.11). `view_inset`: "또렷이 보임" 의 화면 사각형 안쪽 여백 px.
  `touch_margin`: 주인공에 닿은 적을 시야와 상관없이 그리는 거리의 최솟값(몸 반경 합 + 이 값 — 적마다 `TouchReach` 로 늘 수 있음, 4.11·7.10). `ranged_margin`: 13.5-15 의 여유(빛 반경 ≥ 원거리 최대 + `edge` + 이 값 = + 60).
- 기본값을 코드에 두지 않는다(`RulesDef.Sight` 는 `SightRulesDef?`, 그 안의 일곱 필드는 `required`) — 스테이지가 있는 데이터에 `rules.sight` 가 없으면 로딩 오류(WP7). 값은 이 파일 한 곳이다(기획 열린 질문 20 으로 고를 때도 여기만 고친다).
  시험 월드는 `TestDefs.Sight`(같은 값)를 쓴다(WP1c). 쓰는 곳: `refresh_ticks`·`edge`·`view_inset`·`fade`(바뀜 값 잇기) = `VisibilityField`(시뮬레이션), `edge`·`fade`·`touch_margin` = `FogMath`(그리기·HUD·계측),
  `touch_margin` = `TouchReach`(WP4, 생성 때), `memory_ratio` = `FogRenderer`(7.10), `edge`·`ranged_margin` = 검사 13.5-15.
- **키별 소유**(v0.5.1): 한 파일 안에 시뮬레이션 값과 그림 값이 섞여 있다.
  - **시뮬레이션 값** `edge`·`view_inset`·`refresh_ticks`·`ranged_margin` — 쏘기·돌진 거리(R − `edge`), 화면 위아래 한계, 가 본 칸(해시), 13.5-15 가 읽는다.
    바꾸면 같은 씨앗의 해시와 궁수 쏘기 거리가 바뀌고 13.5-15 가 WP4 쪽에서 실패할 수 있으므로 **메인 세션이 WP4 와 합의해 바꾼다**(11.3). 바꾼 뒤 WP4 `Scenario`·13.5-15 를 다시 돌린다.
  - **그림 값** `memory_ratio`·`fade`·`touch_margin` — WP5 가 미리보기 캡처로 고른다(기획 열린 질문 20). `fade` 는 바뀜 값 잇기로 시뮬레이션에도 들어가지만 `InSight`·질의를 바꾸지 않아 해시에 영향이 없다.
    `touch_margin` 은 그리기·계측만 바꾼다.
  - 기획이 흐림 띠와 또렷이 보임 경계를 같은 40 으로 묶었으므로 키를 둘로 나누지는 않는다. 파일 자체는 WP5 가 쓰고, 시뮬레이션 값 줄만 위 합의를 거친다. `rules` 하위 키 `sight` 는 이 파일에만 있다(6.1, 11.3).

```json
// rules/rooms.json (v0.7 — WP4. 기획 2.5·10.8·11.4·15절 첫 값)
{
  "rules": {
    "rooms": { "grace": 3, "cleanup_limit": 25, "elite_spawn_cap": "02:30", "transition": 0.5, "transition_floor": 1.0,
               "gates": { "entry": ["gate_sw", "gate_se"], "boss_entry": "gate_sw", "exits": ["gate_nw", "gate_ne"], "single_exit": "gate_ne" },
               "exits": { "three_chance": 0.4 },
               "area_scale": { "reference": "cathedral_arena", "min": 0.6, "max": 1.0 },
               "reward_spot": { "distance": 100, "spacing": 40, "clearance": 30 }, "fallback_reward": "reward_chest",
               "stairs": { "weapon_slots": 1, "passive_slots": 1 },
               "autoplay_door_skip": 20 }
  }
}
```

- (v0.7, 계약 `RulesDef.Rooms` = `RoomRulesDef?`, 필드는 모두 `required` — `rules.sight` 처럼 기본값을 코드에 두지 않는다) 스테이지가 있는 데이터에 없으면 로딩 오류(WP7). 쓰는 곳: `grace`·`cleanup_limit`·`elite_spawn_cap` = `RoomFlow`·감독,
  `gates`·`exits` = `RunPlanner`, `area_scale`·`stairs` = `RunSession`, `reward_spot`·`fallback_reward` = `RoomFlow`, `transition`·`transition_floor` = `PlayScene`(앱 틱), `autoplay_door_skip` = `AutoplayPolicy`(보상 단계 `REWARD_STALL` 도 같은 초).
- **v0.8**(기획 v0.8.1 2.5·11.4 — WP1f 계약 변경): `cleanup_limit` 45 → 25. **없앤 키** `remaining_marker`(남은 적 점은 정리 시작부터 모두 — 키가 남으면 모르는 속성 오류), `reward_anchor`·`chest_ring`(보상 자리 = 주인공 앞, 3.6 "보상 자리").
  **새 키** `reward_spot`(`RewardSpotDef` — `distance` 100·`spacing` 40·`clearance` 30, px), `stairs`(`StairsRulesDef` — 계단에서 여는 칸 수, 3.6 "계단 보상"), `transition_floor`(층이 바뀌는 문의 넘김 어둠, 1.0s — 기획 v0.8.1 2.5 "층").
  13.5-23 ④ 의 "`reward_anchor` 가 `center`" 는 없어졌다(`center` 는 맵 쪽 ③ 이 그대로 요구). 시작 칸 수 + `stairs` × (사슬의 액트 수 − 1) ≤ 6(`WeaponSlots.SlotCount`) 을 13.5-23 ④ 에 더한다.
- 시뮬레이션 값(`transition`·`transition_floor` 를 뺀 나머지)을 바꾸면 같은 씨앗의 판이 바뀐다 — WP4 가 바꾸고 `Scenario`·13.5-23 을 다시 돌린다.
- 맵 검사 상수 둘(작은 방 N ≤ 36, 문 진입점 150px)은 여기 두지 않고 Core `Map/RoomMapRules` 상수다 — 맵 검사(`ArenaLayout.ValidateRooms`)는 `MapDataReader` 안에서 돌아 `rules` 를 모르고, 가져오기 도구도 같은 검사를 돈다(6.2). `[해석]`

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
      "gates":   { "arch_hall": { "nw": { "open": 13, "end_b": 6 } } },                  // 문 틈·벽 끝 조각(v0.6.4: 13.16 — 끝 조각은 틈 뒤 end_b 에만, nw 6·ne 7·se 23·sw 24. end_a 는 둘레 조각 그대로)
      "pieces":  { "pillar": 15,                     // v0.6: 안쪽 방·짧은 벽 조각(기획 10.3). 이름 → 번호는 이 층의 번호다(L5·L6 은 자기 파일)
                   "wall_v": 1, "wall_h": 2, "wall_corner": 4, "wall_end_v": 6, "wall_end_h": 7, "grate_h": 36,
                   "blood_wall_v": 166, "blood_wall_corner": 168, "blood_wall_h": 169, "blood_floor": 172, "door_closed": 26,   // 피 방 번호(v0.6.4: 13.16 — 원본 SOL 에서 167 은 세로 7070, 모서리는 168)
                   "wall_post": 3 },                 // v0.6.4: 닫힌 문은 26(98 은 피 튄 아치 벽 — 13.16), 작은 모서리 기둥 3(┘ 자리, 아래 "벽 맞물림")
      "marks":   { "pillar": [15], "grate": [36], "closed_door": [26] },   // v0.6: 검증용 표시(13.5-19). 번호는 이름 붙인 것만(25·11·12 는 이름을 붙일 때 같이 표시). 키는 TileMark 의 JSON 이름
      "mirror":  { "pairs": [ ["wall_v", "wall_h"], ["wall_end_v", "wall_end_h"], ["blood_wall_v", "blood_wall_h"],   // v0.8 거울판 짝 표(6.11 "거울판", 기획 v0.8.1 10.8)
                              ["grate_h", "grate_v"], ["door_closed", "door_closed_v"] ],    //   grate_v·door_closed_v 의 번호(35·25 후보)·성질은 WP0 이 원본으로 확인해 pieces 에 더함
                   "self":  [ "pillar", "wall_corner", "wall_post", "blood_floor", "blood_wall_corner" ] },   // 뒤집어도 같은 조각
      "props": {                                     // 메가타일 → 하위 칸 4개 (위-왼, 위-오른, 아래-왼, 아래-오른). 출처: 원본 확인(probe tiles)
        "13": "....", "162": "....", "163": "....",
        "15": "x...",                                // 기둥: 막힌 칸은 위-왼 하나(TIL p24, SOL 13 = 걷기·투사체, 시야 통과). 나머지 셋은 걸을 수 있는 칸의 위 조각
        "1": "X.X.", "2": "XX..",                    // 뒷벽: 칸의 반만 벽. 시야 비트는 WP0a 가 원본으로 확인(v0.6.4: 이 값 그대로)
        "36": "Xw..",                                // v0.6 창살: 기둥 칸만 셋 다, 나머지 한 칸은 걷기만(SOL 1 — 시야·투사체 통과). v0.6.4: 원본은 `ww..`, 기둥 칸 `X` 는 `sol_known_diffs` 보정(13.16)
        "18": "XXXX", "19": "XXXX", "20": "XXXX", "24": "XXXX"   // 앞둘레 턱: SOL 15 = 걷기·시야·투사체 모두 막힘(4.2)
      },
      "sol_known_diffs": [                           // 출처: DevilutionX 참고(원본 SOL 오류 보정 칸, 부록 A.4). 번호와 이유만
        { "tile": 36, "sub": 0, "sol": "w", "use": "X", "why": "창살 기둥 칸 시야·투사체 막힘 보정(원본 SOL 오류, 원작 수정판이 고침)" } ] }   // v0.6.4: 실제 값(13.16)
  ]
}
```

(`props` 의 13·15·1·2·18·19·20·24 는 v0.3 리뷰가 diabdat 를 직접 읽어 확인한 값이다(v0.5: 15 의 시야 통과·18~24 의 시야 막힘도 같은 확인 — SOL 13·15). 1·2 의 시야 비트, 모서리(4·23 등)·문 조각의 성질과
`sol_known_diffs` 의 칸은 WP0a 가 `probe tiles` 로 확인해 적는다 — 위 `sol_known_diffs` 원소는 WP0a 가 확인해 `Data/tilesets/l1.json` 에 적은 실제 값이다(v0.6.4 — 예였던 아치 `{ tile 4, sub 1, ".", "M" }` 는 L1 실제 데이터에 없다, 13.16). DevilutionX 의 보정 목록은 하위 타일(조각) 번호라, 쓰는 메가타일의 (번호, 하위 칸)으로 바꿔 그 칸만 적는다.)

**성질 글자** (v0.5: 시야 막힘을 더해 8글자. 원본 SOL 의 `sol & 7` 과 1:1)

| 글자 | 걷기 막힘 (bit0) | 시야 막힘 (bit1) | 투사체 막힘 (bit2) | `sol & 7` | 쓰는 곳 |
|---|---|---|---|---|---|
| `.` | | | | 0 | 바닥 |
| `w` | 막힘 | | | 1 | v0.6: L1 창살(35·36 계열의 기둥 아닌 칸, 기획 10.1), L2 교수대·말뚝(단계 3), 합성 시험 |
| `L` | | 막힘 | | 2 | (MVP 맵에 없음, 합성 시험) |
| `W` | 막힘 | 막힘 | | 3 | (MVP 맵에 없음) |
| `m` | | | 막힘 | 4 | (MVP 맵에 없음, 합성 시험) |
| `x` | 막힘 | | 막힘 | 5 | L1 기둥(SOL 13) |
| `M` | | 막힘 | 막힘 | 6 | L1 아치 보정 칸(`use`) (MVP L1 실제 데이터에 없음) |
| `X` | 막힘 | 막힘 | 막힘 | 7 | 둘레 벽·앞 턱(SOL 15·159·175·191) |

- 규칙 하나로 읽는다: **대문자 = 시야 막힘**, 소문자 = 시야 통과, 글자(w·m·x) = 걷기·투사체 조합(`L` 은 시야만, `.` 은 아무것도 아님).
- 글자 하나에 세 비트를 다 담은 이유: 표 한 칸 = 원본 SOL 한 칸이라 대조가 `sol & 7` 하나로 끝나고(1.4), 시야만 적는 두 번째 표를 두면 두 표의 키·길이가 어긋날 수 있다.
  대소문자로 시야를 가른 것은 v0.4 의 `X`(걷기·투사체)가 둘레 벽에서 원본대로 시야도 막아 그대로 맞기 때문이다 — 바뀌는 글자는 기둥 `X` → `x` 와 v0.4 의 `W`·`M`(걷기만·투사체만) → `w`·`m` 뿐이다(WP1c).
- **주의**(v0.5.1): `W`·`M` 은 글자는 그대로인데 뜻이 바뀐다(걷기만 → 걷기 + 시야, 투사체만 → 시야 + 투사체). 고치지 않고 남은 옛 글자는 해석 오류 없이 **조용히 시야를 막는다**.
  그래서 WP1c 는 저장소의 `W`·`M` 성질 글자를 모두 찾아 뜻을 확인하고, 기존 시험을 글자가 아니라 `CellFlags` 값으로 확인하게 바꾼다(11.1 WP1c #8·#9).
  시제품·도구 출력도 v0.4 의 4글자를 옮겨 적지 않는다 — `sol` 값(또는 `sol & 7`)을 받아 위 표로 바꿔 적는다(WP0a).
- `ArenaGrid` 는 칸마다 `CellFlags`(`BlocksWalk`·`BlocksProjectiles`·`BlocksSight`·`Boundary`·`Outside`)를 가진다. 격자 밖은 걷기·투사체 막힘이고 시야는 막지 않는다(4.11).
  `ArenaGrid.Prop(tx, ty)` 는 위 글자(격자 밖 `#`)를 돌려준다(미리보기·로그).

```json
// Data/maps/cathedral_arena.json (WP5)
{
  "maps": [
    { "id": "cathedral_arena", "tileset": "l1", "palette": "l1_1", "size": 20,
      "layout": {
        "border": "arch_hall",
        "gates": [ { "id": "gate_nw", "side": "nw" }, { "id": "gate_ne", "side": "ne" },
                   { "id": "gate_se", "side": "se" }, { "id": "gate_sw", "side": "sw" } ],
        "place": [                                   // v0.6: 넓은 방 + 안쪽 방(기획 10.3). 자리는 첫 값, 이름·번호는 WP0a 가 견본으로 맞춘다
                                                     // v0.6.1: 기획 v0.7.1 10.3 배치. 위 줄·왼쪽 열을 같이 막아야 하는 자리는 모서리 조각(아래 "벽 맞물림")
                                                     //   설계 담당이 하위 칸으로 풀어 13.5-19 ①②·막힘 줄어듦·open_center·80px(근사)를 확인했다(13.13)
                                                     // v0.6.4: 실제 maps/cathedral_arena.json 은 벽 끝·피 방 바깥 벽의 그림 번호만 이 예와 다르다(13.16 — 하위 칸 성질은 피 방 [5,5] 하나 빼고 같음).
                                                     //   이 예는 합성 ④ TestDefs.RoomMapExample 와 같게 두고, 다른 자리는 줄마다 "v0.6.4" 로 적었다. 실제 파일을 이 예로 되돌리지 않는다
          { "id": "s1_room_blood", "origin": [0, 0], "over_border": true,      // 피 방 6×6: 바깥 두 벽은 둘레 자리에 겹침(갇힌 띠 없음, 기획 10.1)
            "decor_why": "닫힌 문은 아래 벽 장식(기획 10.3)",   // v0.6.4: 닫힌 문은 26(98 은 문이 아님, 13.16)
            "grid": [ "blood_wall_corner blood_wall_h blood_wall_h blood_wall_h blood_wall_h blood_wall_corner",   // [5,0](T): 둘레 위 줄 + 오른쪽 벽 왼쪽 열 = 모서리(v0.6.4: 실제는 wall_corner — 피 묻은 벽은 방 밖에 피 바닥이 번짐)
                      "blood_wall_v      blood_floor  blood_floor  blood_floor  blood_floor  wall_v",
                      "blood_wall_v      blood_floor  blood_floor  blood_floor  blood_floor  wall_end_v",   // v0.6.4: 실제 [5,2] 는 wall_v(벽 끝 조각은 위 끝이 마감 — 아래 끝이 빈 자리에서는 안 보임)
                      "blood_wall_v      blood_floor  blood_floor  blood_floor  blood_floor  .",
                      "blood_wall_v      blood_floor  blood_floor  blood_floor  blood_floor  .",
                      "blood_wall_corner blood_wall_h door_closed  blood_wall_h blood_wall_h blood_wall_h" ] },   // [0,5](├): 둘레 왼쪽 열 + 아래 벽 위 줄 = 모서리(v0.6.4: 실제 아래 줄은 wall_corner·wall_h, door_closed 그대로, [5,5](┘) 는 wall_post — 하위 (11,10) 이 바닥)
          { "id": "s1_room_grate", "origin": [13, 0], "over_border": true,     // 창살 방: 위는 둘레, 오른쪽은 앞 턱. 짧은 줄의 나머지는 건드리지 않음
            "grid": [ "wall_corner",                                           // [13,0](T): 둘레 위 줄 + 왼쪽 벽 = 모서리
                      "wall_v",
                      "wall_end_v",                                            // 문 틈 [13,3..4](하위 4칸). v0.6.4: 실제는 [13,2] wall_v·틈 아래 [13,5] wall_end_v(조사 함정 7 의 "틈 앞 끝만" 은 버림)
                      ".",
                      ".",
                      "wall_v",
                      "wall_h wall_h grate_h grate_h wall_h wall_h" ] },       // [13,6](ㄴ): 위에서 온 왼쪽 벽이 가로로 꺾임 = 가로 조각. 창살은 두 칸 잇기(기획 10.1 "창은 2메가 이상")
          { "id": "s1_wall_west", "piece": "wall_end_h", "at": [[3, 13]] },    // ㄱ 벽: 기획 v0.7.1 에서 둘레와 떼어 출입구 둘(둘레 쪽 하위 5칸 틈 + [6,17..18])
          { "piece": "wall_h", "line": [[4, 13], [5, 13]] },
          { "piece": "wall_v", "line": [[6, 13], [6, 15]] }, { "piece": "wall_end_v", "at": [[6, 16]] },     // 가로 끝 다음 칸의 세로 조각은 그대로 이어진다(ㄱ). v0.6.4: 실제 [6,16] 은 wall_v
          { "id": "s1_wall_south", "piece": "wall_corner", "at": [[13, 13]] },                              // Γ: v0.6 의 [12,12] 에서 한 칸 밈 — open_center 밖
          { "piece": "wall_h", "line": [[14, 13], [15, 13]] }, { "piece": "wall_end_h", "at": [[16, 13]] },   // v0.6.4: 실제 [16,13]·[13,16] 은 wall_h·wall_v(아래·오른쪽 끝)
          { "piece": "wall_v", "line": [[13, 14], [13, 15]] }, { "piece": "wall_end_v", "at": [[13, 16]] },
          { "piece": "pillar", "at": [[15, 16], [17, 15], [17, 17], [7, 9], [12, 9]] } ] },               // Γ 안 기둥 셋은 벽·서로·앞 턱과 하위 3칸 이상(13.5-4 80px)
      "open_center": [6, 6, 13, 13],                 // v0.6: 메가 [6..12,6..12](RectI 끝 미포함, 기획 "7×7"). 안에는 기둥 2개까지(13.5-19)
      "entries": [ { "id": "gate_nw", "outside": [-4, 19.5], "inside": [4, 19.5] },
                   { "id": "gate_ne", "outside": [19.5, -4], "inside": [19.5, 4] },
                   { "id": "gate_se", "outside": [43, 19.5], "inside": [34, 19.5] },
                   { "id": "gate_sw", "outside": [19.5, 43], "inside": [19.5, 34] } ],
      "hero_start": { "tile": [19.5, 19.5], "facing": "south" },
      "anchors": { "center": [19.5, 19.5], "room_blood": [5, 5], "room_grate": [32, 7],      // v0.6: 방 기준점 = 방 바닥 가운데(하위 타일)
                   "room_west": [6, 32], "room_south": [31, 31] },                              // v0.6.1: 반쯤 막힌 구역(ㄱ 안·Γ 안)에도 기준점(13.5-5 큰 적 길)
      "room_areas": { "room_blood": [0, 0, 5, 5], "room_grate": [13, 0, 19, 6],       // v0.6.3: 방 영역(메가, 끝 미포함) → 방 바닥·문 수(아래 "방 영역과 방 바닥")
                      "room_west": [0, 13, 6, 19], "room_south": [13, 13, 19, 19] },  //   문 수: 피 방·창살 방 1(방 체류 압박 대상), ㄱ 안·Γ 안 2
      "room_kinds": ["normal", "boss"],               // v0.7: 이 맵을 쓸 수 있는 방 종류(기획 10.8 — 액트 1 보스 방 + 단계 1 의 모든 방. 정예 방에는 안 씀). 실제 파일에는 WP5 가 더한다
      "mirror": true,                                  // v0.8: 거울판도 쓴다(타일셋 짝 표가 이 맵의 조각을 모두 덮어야 함 — 아니면 로딩 오류). 기본 거짓
      "art": "d1/maps/cathedral_arena" }
  ]
}
```

- **타일셋**(`TilesetDef`, 계약 WP1b): `column_height` = 벽 기둥 높이(4.1 의 Hc), `floor` = 바닥 메가타일(섞어 깖), `borders`/`gates`/`pieces` = 이름 → 메가타일 번호.
  맵은 번호를 직접 적지 않고 이 이름을 쓴다 → 번호를 아는 묶음(WP0)과 배치를 맞추는 묶음(WP5)의 파일이 나뉜다.
  - **층마다 타일셋 하나**(v0.6, 기획 13.1): 메가타일 번호 44 이상은 층마다 뜻이 다르다(L1 162 바닥 / L5 162 기둥). 이름 → 번호는 타일셋 파일 안에서만 뜻이 있으므로 L5·L6 은 자기 파일(`l5.json`·`l6.json`)을 갖는다.
    L5 는 번호 1~43 과 바닥 13 이 L1 과 같은 구실이라(조사, 원본 그림으로 확인) `pieces` 이름을 L1 과 같게 붙이면 스테이지 1 의 배치를 거의 그대로 쓸 수 있다 — 바닥 변종·피 방처럼 44 이상인 이름만 L5 번호로 다시 고른다.
    성질 표는 이름이 같아도 그 층의 SOL 로 따로 확인한다(L5 기둥 15·아치는 걷기·투사체만 막고 시야는 안 막음 — 기획 10.6). L6 은 L3 번호와 맞지 않아 견본으로 새로 고른다.
  - **`marks`**(v0.6, 계약 `TilesetDef.Marks`, 키 = `TileMark`: `pillar`·`grate`·`closed_door`·`arch` — v0.6.1: 사전 키로 읽으므로 `StatKey` 처럼 값마다 `JsonStringEnumMemberName` 을 단다. `closed_door` 처럼 두 낱말인 이름의 읽기 시험을 WP1d 에 둔다): 검증이 뜻을 알아야 하는 번호의 표시. 번호는 이름 붙인 번호여야 한다(13.5-12 — 성질 표 키와 같은 집합 안).
    `pillar` = `open_center` 안에 허용되는 조각(13.5-19), `closed_door`·`arch` = 문으로 쓰면 안 되는 조각(원작 닫힌 문은 SOL 로 벽, 아치는 틈이 하위 1칸 — 기획 10.1, 경고), `grate` = 미리보기·로그 이름(규칙은 성질 글자가 정함).
    `marks` 는 `LayoutHash` 에 넣지 않는다 — 그림을 바꾸지 않는다.
  - 테두리 이름은 메가타일 격자 기준이다: `corner_n` = (0,0)(화면 맨 위), `edge_ne` = 윗줄(y = 0), `corner_e`, `edge_se` = 오른쪽 열, `corner_s`, `edge_sw` = 아랫줄, `corner_w`, `edge_nw` = 왼쪽 열.
    L1 번호는 조사 문서의 해골왕 방(`SklKng2.DUN`) 짜임이다.
  - **`props`**(성질 표): 쓰는 메가타일마다 하위 칸 4개(TIL 순서: 위-왼, 위-오른, 아래-왼, 아래-오른 = 하위 타일 (2mx,2my) (2mx+1,2my) (2mx,2my+1) (2mx+1,2my+1)).
    글자는 위 표 8개(원본 SOL 의 bit0·bit1·bit2 와 대응, 1.4 성질 대조 — v0.4 까지는 bit0·bit2 의 4글자). 앞둘레 턱은 `X` 다(4.2). 걷기만 막는 칸(`w`)은 v0.6 의 창살에서 처음 쓴다 —
    걷기와 시야가 다른 첫 칸이라 길 검사·시야·투사체가 같은 칸을 다르게 보는지가 실제 맵에서 시험된다(10.1 `Nav`·`Visibility`).
    쓰는 메가타일인데 표에 없으면 로딩 오류. **표에 이름 붙이지 않은 메가타일이 있어도 오류**: `props` 의 키 = 그 타일셋이 이름 붙인 번호(`floor`·`borders`·`gates`·`pieces`)와 같은 집합
    (맵과 무관하게 타일셋만으로 판정, 번호 ≥ 0) → 성질 표가 SOL 전체를 옮긴 덤프로 커지지 않는다(R24). 이름만 붙이고 어느 맵도 안 쓰는 조각은 남을 수 있다.
  - `sol_known_diffs`: 원본 SOL 과 일부러 다르게 둔 칸 중 **원작 수정판이 고치는 SOL 오류**만(고치지 않으면 아치 밑으로 투사체가 지나가고 시야가 샌다, 기획 10.1·10.7). 원소 = 메가타일 번호·하위 칸·원본 글자·쓰는 글자·이유.
    글자는 위 8글자라 시야 보정도 같은 원소 하나로 적는다(아치: 원본 `.` → `M`). 가져오기 도구가 대조할 때만 쓴다(1.4).
    기획 이유로 원본과 다르게 둘 칸이 생기면 따로 `design_diffs`(이유 필수)에 적는다 — 두 목록을 섞지 않는다. 지금은 `design_diffs` 가 없다
    (v0.6: 기둥이 시야를 막게 하기로 하면 `{ "tile": 15, "sub": 0, "sol": "x", "use": "X", "why": "…" }` 는 `design_diffs` 가 아니라 `sol_known_diffs` 원소다 — DevilutionX 가 "잘못 표시된 기둥" 으로 보정하는 칸이라 SOL 오류 보정으로 적는다, 출처: DevilutionX 참고. 기획 열린 질문 22 v0.7, 지금 기본은 원본대로 안 막음).
- **맵**(`MapDef`, 계약 변경 WP1b): `size` = 메가타일 수 M(정사각, 하위 타일 N = 2M), `tileset`, `palette`(층 팔레트 — 가져오기·그림만 씀, 기획 10.1 "맵마다 고정"), `art` = 가져오기 출력 폴더(7.6).
  v0.2 의 `image`·`minimap`·`size`(px)·`collision`·`texture_source` 는 없앴다. 월드 크기는 `IsoMath` 가 N 과 기둥 높이로 계산한다(4.1).
- **배치 풀기** `ArenaLayout.Expand(MapDef, TilesetDef) → ArenaGrid`(Core `Map/`, WP1b, 결정적):
  1. 모든 칸 = 바닥: `floor[StableHash(맵 ID, mx, my) % 개수]`(기획 10.1 "좌표 해시로 섞는다").
  2. 테두리: 네 모서리·네 변을 `borders[border]` 의 번호로.
  3. 문: `gates[i].side` 의 가운데(`at` 으로 변 위 메가타일 번호를 줄 수 있음) 메가타일 2개(= 하위 타일 4칸, 폭 약 140px)를 `open` 으로, 양옆 한 칸씩을 `end_a`/`end_b` 로.
  4. `place`(파일 순서대로): 조각 이름 → 번호를 `at` 목록의 [mx, my] 에, 또는 `line: [[mx0,my0],[mx1,my1]]`(가로·세로 줄 — 짧은 벽), 또는 (v0.6) **`grid`** 블록 —
     `origin` [mx, my] 에서 시작하는 줄 목록(줄 = y, 칸 = 공백으로 나눈 조각 이름 — `Split(' ', RemoveEmptyEntries)` 라 칸 사이 공백 수는 자유, `.` = 건드리지 않음).
     줄마다 칸 수가 달라도 된다 — 짧은 줄의 나머지는 건드리지 않는다(v0.6.1). 우리 조각 이름으로 방을 짜는 자리다(아래 "원작 방 본뜨기").
     놓기 하나는 `at`·`line`·`grid` 중 하나만, `grid` 면 `piece` 없음. 한 칸에 두 번 놓으면 오류(지금 규칙 그대로).
     **`over_border`**(v0.6, 기본 거짓): 참이면 그 놓기가 테두리 칸(문 칸 제외 — 문 칸은 여전히 오류)을 덮을 수 있다. 덮인 칸의 **둘레 표시는 남는다**(4.2). 거짓이면 테두리 칸에 놓기는 지금처럼 오류.
     → 안쪽 방의 바깥 벽을 "둘레 벽 바로 옆 메가" 가 아니라 둘레 자리에 겹쳐 놓아, 벽 두 겹 사이 하위 1칸 바닥 띠(갇힌 띠)가 생기지 않게 한다(기획 10.1, 조사 함정 7).
     **덮어도 막힘은 줄지 않는다**(v0.6.1, 오류): 덮은 칸의 하위 칸 넷마다 원래 테두리 조각이 막던 비트(걷기·시야·투사체 셋 각각)를 덮는 조각도 막아야 한다.
     `Expand` 가 덮을 때 성질 글자 둘을 비교해 `place[3]: 테두리 (0,13) 하위 칸 아래-왼 의 걷기·시야 막힘을 wall_h 가 풀었다` 로 낸다 — 둘레 왼쪽 열을 가로 조각으로 덮어 바깥 벽에 홈이 나는 일(v0.6 예의 ㄱ 벽)을 막는다.
     **벽 맞물림**(v0.6.1, 원본 SOL — 벽 메가는 반만 막는다: 세로 1 = 왼쪽 열, 가로 2 = 위 줄, 모서리 4 = 위 줄 + 왼쪽 열): 위 줄과 왼쪽 열을 **같이** 막아야 하는 메가(Γ 꺾임, 세로 벽이 가로 벽·둘레 위 줄 밑에 T 자로 붙는 자리,
     가로 벽이 둘레 왼쪽 열에 붙는 자리)에는 모서리 조각을 둔다. 한쪽 조각만 두면 하위 1칸 구멍이 남는다(약 28.6px — 시야가 새고 작은 적만 지나감). 세로 벽이 위에서 내려와 가로로 꺾이는 └ 자리는 가로 조각,
     가로 벽 끝 다음 칸에서 세로로 내려가는 ㄱ 자리는 세로 조각이면 이어진다. 가로 벽이 왼쪽에서, 세로 벽(또는 그 줄의 문 틈)이 위에서 와서 그 메가의 위-왼 꼭짓점에서 끝나는 ┘ 자리는
     작은 모서리 기둥(L1 3 `wall_post`, `X...`) — 가로 조각을 두면 벽이 세로 벽 선 너머로 한 메가 더 나간다(v0.6.4, 피 방 [5,5], 13.16). 남은 구멍은 13.5-19 의 "하위 1칸 틈" 과 13.5-4 의 80px 규칙이 잡는다. 그림 맞물림은 WP0a 가 견본으로 본다.
     `id`(선택)는 방·벽 이름 — 오류 문구와 `--debug-nav tiles` 에만 쓴다. `decor_why`(선택)는 `marks` 의 `closed_door`·`arch` 번호를 일부러 장식으로 쓸 때의 이유 — 있으면 13.5-19 경고를 끈다.
  5. 결과: 메가타일 번호 M×M + 하위 타일 성질 N×N(`props` 로) + 하위 칸 **둘레 표시**(2·3단계 테두리·문 끝 조각에서 온 칸 — 4.2-4 `IsBoundary`)
     + `LayoutHash`(번호 격자·타일셋 ID·팔레트 + **타일셋 정의 전체**(`props`·`sol_known_diffs`·`design_diffs`·이름 → 번호)를 정해진 순서로 이은 `StableHash` — 그림이 낡았는지 보는 값, 1.4).
     성질 표나 보정 목록을 고치면 바닥/벽 나눔이 바뀔 수 있으므로 해시도 바뀐다. 시야 글자만 바꿔도(`x` ↔ `X`) 해시가 바뀐다 — 성질 글자를 그대로 잇기 때문이다(v0.5, 식은 그대로).
     그래서 WP1c 가 합성·실제 표의 글자를 바꾸면 그 전에 구운 맵 그림은 낡음이 된다(1.4 시작 검사 ⑤, 다시 가져오기).
- **`open_center`**(v0.6, 선택, 계약 `MapDef.OpenCenter` = `RectI?`, 메가타일 좌표, 끝 미포함 — 기획 "메가 [6..12,6..12]" = `[6, 6, 13, 13]`): 가운데 열린 공간.
  그 안의 메가타일은 바닥(`floor`) 또는 `marks.pillar` 번호뿐이고 기둥은 2개 이하(13.5-19, 오류). 무리를 끌고 도는 가운데가 넓게 남는지를 데이터로 지킨다(기획 10.1).
- **방 기준점**(v0.6): `anchors` 의 키가 `room_` 으로 시작하면 안쪽 방 기준점(방 바닥 가운데)이다. 따로 칸을 두지 않는다 — 기준점은 이미 `anchors` 이고 이름 규칙 하나로 충분하다.
  v0.6.1: 방이 아닌 **반쯤 막힌 구역**(출입구가 한두 곳인 짧은 벽 안쪽 — 스테이지 1 의 ㄱ 안 `room_west`·Γ 안 `room_south`)에도 같은 이름으로 둔다. 주인공이 숨어 큰 적이 못 들어오는 곳이 생기지 않는지를 데이터가 지키게 한다.
  쓰는 곳: 큰 적 길 검사(13.5-5 — 26·34 등급이 모든 입구 진입점에서 모든 방 기준점까지), 13.5-19(기준점 칸이 맵 안·걷기 막힘 없음), `--autoplay-visit rooms`(9절), `--debug-nav entries`.
- **방 영역과 방 바닥**(v0.6.3, 계약 `MapDef.RoomAreas` = `room_areas`, 기획 10.1 "방 바닥 = 기준점에서 반경 14 로 문을 지나지 않고 닿는 칸"): `room_*` 기준점마다 메가타일 사각형 하나(끝 미포함, `open_center` 와 같은 표기).
  **방 바닥** = 그 사각형을 하위 칸으로 넓힌 범위 안에서 기준점 칸부터 걷기 막힘 없는 칸을 4이웃으로 채운 칸(13.5-19 ① 과 같은 걷기 기준 — 하위 1칸 틈이 ② 로 막히므로 반경 14 의 대리로 충분하다).
  **문 수** = 사각형 밖이면서 바닥 칸에 4이웃으로 붙은 걷기 칸들의 4이웃 무리 수. 문 칸을 데이터에 따로 적지 않아도 "문을 지나지 않고" 가 사각형 경계로 정해진다.
  벽 메가는 위 줄·왼쪽 열만 막으므로 방의 **오른쪽·아래 벽 메가는 사각형에 넣지 않는다**(피 방 블록 메가 [0..5] → `[0, 0, 5, 5]`) — 넣으면 벽 바깥 반 칸이 문을 지나 바닥에 섞인다. 왼쪽·위 벽 메가는 넣어도 된다(그 줄의 문 틈 칸이 바닥이 된다).
  계산은 Core `Map/RoomFloors.Compute(MapDef, ArenaGrid) → RoomFloors`(맵을 읽을 때 한 번 — `World.InnerRooms`): 방 순서 = 기준점 ID 서수, 칸 = 행 우선, `IndexAt(tx, ty)`·`IndexAt(월드 px)` → 방 번호 또는 −1, `RoomFloor.OneDoor`(문 수 1).
  잘못된 입력은 빈 바닥으로 두고 던지지 않는다 — 오류 문구는 13.5-19 가 낸다. `ResolvedMap` 에 두지 않은 이유: 바닥 계산에 펼친 격자(걷기 막힘)가 필요한데 `ResolvedMap.Resolve(MapDef, IsoMath)` 는 격자를 받지 않는다(시그니처를 바꾸지 않고 `World` 가 `Arena` 옆에 둔다).
  쓰는 곳: 방 체류 압박(6.12 — 문 하나인 방만)·방 보급 검사(13.5-19)·방 들르기의 "방 안"(9절)·판 끝 `ROOMS` 로그(9절).
- **원작 방 본뜨기**(v0.6.1, 사용자 결정 B·M, R24): 원작 작은 방(도살자 방 `rnd6` 6×6, `Banner` 8×8 등)을 "본뜬다" 는 **크기·문 방향·벽 재료(어느 조각 이름을 쓰나)만 따른다**는 뜻이다.
  칸 배열은 우리 조각 이름(벽·모서리·벽 끝·바닥·장식)으로 새로 짠다 — DUN 을 읽어 칸마다 옮겨 적지 않는다. 장식 번호(피 묻은 벽 166~170·피 바닥 171~174·닫힌 문 25·26 — v0.6.4: 98 은 문이 아니라 피 튄 아치 벽, 13.16)가 원본에 있다는 것은 사실로 적고 타일셋 `pieces` 에 이름을 붙이지만,
  그 번호를 어디에 어떤 순서로 놓을지는 우리가 정한다. `audit` 가 원본이 있을 때 맵 배치와 알려진 방 DUN 의 4×4 배열을 대조해 같으면 실패한다(1.4). 이 경계는 사용자 확인을 받는다(13.13 열린 질문).
  v0.7.1: 원작 방을 본뜬 맵은 JSON 첫머리에 `// 본뜬 원작 방: bonecha1.dun, …`(없으면 줄을 두지 않음)을 적고, 그 DUN 이 그 타일셋 항목의 `audit_dun` 에 있어야 한다(13.5-22 — 기획 10.4 의 L2 뼈 방처럼 "딴다" 고 적은 곳도 같은 뜻: 크기·문 방향·벽 재료만).
- 위치(`entries` 의 `outside`/`inside`, `hero_start.tile`, `anchors`)는 **하위 타일 좌표(실수, 칸 중심 = 정수)**, 형은 `TileCoord`(4.1). 입구 ID 는 문 ID 와 같게 둔다(`entry:gate_ne`, 기획 10.1).
  기획 10.1 의 진입점 값은 첫 값이고 `--debug-nav entries,tiles` 로 맞춘다(기획 열린 질문 7). 위 예는 문 틈 중심(19.5)에 맞췄다 — 20 이면 문 틈 중심에서 0.5칸(약 14px) 비켜 행진이 한쪽 문설주로 몰린다.
  진입점은 기획 10.1 표 그대로 뒤 문 `4`, 앞 문 `N − 6`(N 40 → 34), 바깥 시작점 `−4`·`N + 3` 이다 — 뒤 벽은 한 줄, 앞 턱은 두 줄 두께라 둘 다 벽 안쪽 면에서 3.5칸(약 100px) 떨어진다.
  다른 묶음은 이 값을 직접 쓰지 않고 `World.Resolved`(월드 px, 4.1)를 읽는다.
- `wall_fade`(선택, 기본 `{ "mode": "light" }`): 반투명으로 그릴 벽(7.6). `light` = 기획 10.2(v0.5)의 두 규칙 — 주인공보다 앞이고 ① 주인공 그림 사각형과 겹치는 벽, 또는
  ② 주인공 빛 반경(`World.Lighting` 의 현재 `hero_light.radius`) 안의 벽. 모든 맵의 기본이고 MVP 두 맵은 적지 않는다. `hero` = ① 만(미리보기 비교용).
  `radius`(선택, px)를 적으면 ② 에 빛 반경 대신 그 값을 쓴다. v0.4 의 `radius` 모드("그 원 안 유닛을 가리는 벽만")는 기획이 유닛 조건 없이 정해 없앴다.
- 스테이지 2(`catacombs_arena`, L2, M = 22 — v0.7: 액트 2 **보스 방**, `room_kinds: ["boss"]`. 일반·정예 방은 지하묘지 작은 방 2장): L2 테두리·문·벽 조각 번호가 아직 없다(기획 열린 질문 15). WP0 이 단계 3 전에 `tilesets/l2.json` 을 채운다.
  앞쪽 둘레·협로 벽은 보기에 낮은 조각을 먼저 찾고(기획 10.4), L2 에 없으면 높은 벽을 쓰고 가림 규칙(7.6, `wall_fade` 기본 `light`)에 맡긴다. 협로 벽은 `place` 의 `line`. L2 조각의 성질도 원본 SOL 로 확인한다 —
  "낮은 턱이라 투사체 통과" 를 미리 가정하지 않는다(L1 앞둘레 턱은 `X` 였다). v0.5: 협로 벽의 **시야 막힘**도 원본대로 적는다 — 그 값에 따라 앞쪽 길목에서 안개가 하는 일이 달라진다(기획 10.7·열린 질문 15).
  v0.6: 뒤쪽 안쪽 방 둘(`s2_room_bone` 문 둘, `s2_room_gallows` 문 하나 + 교수대·말뚝 `w` 칸 — 기획 10.4)도 같은 `grid`·`over_border` 로 적는다. 조사 견본의 L2 번호(벽 1·2, 모서리 6~9, 걷기만 95·101·104,
  닫힌 문 150·151 → `marks.closed_door`, 아치 4·5 → `marks.arch`, 낮은 판 10~12 = 앞 턱 후보)는 추정이라 단계 3 에 깔아 보고 성질을 원본 SOL 로 확인한다.
- **헬파이어 층**(v0.6, 헬파이어 스테이지 단계 — 기획 10.6): 지하실 `l5`(`nlevels\l5data`, 팔레트 `l5base` 하나)·둥지 `l6`(`nlevels\l6data`, `l6base1~5` 중 하나 고정). 가져오기 표 `tilesets.json` 의 항목에 `"bundle": "hellfire"`(1.4).
  스테이지는 `requires: ["hellfire"]`(6.17). **맵에는 `requires` 를 두지 않는다**(v0.6.1 — `MapDef` 에 칸이 없어 엄격 읽기가 오류를 낸다): 맵 그림의 묶음은 타일셋 표 항목의 `bundle` 이고(1.4 "타일셋 묶음과 맵"),
  그 맵을 쓰는 스테이지의 `requires` 에 그 묶음이 있는지는 13.5-10 묶음판이 본다. 색 순환(용암)은 멈춘 그림(기획 10.2). 나크룰 방은 MPQ 에 없어 우리가 L5 이름으로 `grid` 를 깐다(DevilutionX 의 DUN 은 보지 않는다, 기획 19.4).

**방 맵** (v0.7, 사용자 결정 R, 기획 10.8) — 방 맵도 지금의 `maps` 정의·배치·성질 표 그대로이고 위 규칙을 모두 따른다. 더하는 것만 적는다.

- **`room_kinds`**(계약 `MapDef.RoomKinds` = `IReadOnlyList<RoomKind>`, 기본 빈 목록): 그 맵을 쓸 수 있는 방 종류(`normal`·`elite`·`boss`). 비어 있으면 방 맵이 아니다(방 없는 월드·`--map` 미리보기에만). 액트의 방 맵 풀(6.12)에 든 맵은 그 종류를 담아야 한다(13.5-23).
- **크기 둘**: 작은 방 = 메가 16(N 32, 외곽 상자 2048×1152 — 일반·정예 방, 액트마다 MVP 2장), 큰 방 = 지금 아레나(`cathedral_arena` 40·`catacombs_arena` 44 — 보스 방). 크기는 `size` 그대로이고 따로 적지 않는다.
  격자·해시·시야 확장 격자 배열은 맵 크기로 잡으므로(4.2·4.8·4.11) 작은 방은 그만큼 작다.
- **`cathedral_arena` 의 새 역할**(사용자 결정 R — 버리지 않음): 액트 1 **보스 방**(해골왕)이고, 작은 방 맵이 생기기 전인 **단계 1 의 모든 일반 방**이다(들어오는 문만 바뀜). `room_kinds: ["normal", "boss"]`.
  정예 방에는 쓰지 않는다(기획 10.8 — 같은 큰 방을 한 액트에서 두 번 이상 보지 않게). 단계 2 에 대성당 작은 방 2장이 생기면 그 스테이지의 일반 풀에서 빠진다(맵의 `room_kinds` 는 그대로 둬도 됨 — 풀이 정한다).
  피 방·창살 방(문 하나)·방 보급·방 체류 압박은 이 큰 방에서만 일한다(작은 방에는 문 하나인 방이 없다, 아래).
- **문 넷과 `center` 필수**: `room_kinds` 가 빈 목록이 아니면 `gates` 가 정확히 `gate_nw`(nw)·`gate_ne`(ne)·`gate_se`(se)·`gate_sw`(sw) 넷이고 `entries` 도 그 넷, 기준점 `center` 가 있다(v0.7 은 보상 자리 — v0.8 부터는 큰 적 길 검사 13.5-5 의 목적지로만, 보상 자리는 주인공 앞 3.6). 이벤트의 `entry:` 이름이 맵이 바뀌어도 같게 하려는 것이다(13.5-23).
  **입구와 문이 같은 자리**(v0.8.1, 13.5-23 ③ — `ValidateRooms`, 원판·거울판 모두): 입구마다 `inside` 에서 가장 가까운(칸 마름모 중심 거리) 문 틈 칸(`GateZones.Compute` 의 `Cells`)이 **같은 ID 문**의 칸이어야 한다. 아니면 `입구 gate_nw 가 문 gate_ne 앞에 있음` 오류 — ID 넷이 있는지만 보던 검사는 입구 좌표가 다른 문 앞에 있어도 통과했다.
- **문 틈 칸·맞은편 문** `Map/GateZones`(계약·구현 WP1e, 결정적): `GateZones.Compute(MapDef, ArenaGrid) → IReadOnlyDictionary<string, GateZone>`,
  `GateZone(string GateId, GateSide Side, IReadOnlyList<Int2> Cells)` — 그 문의 열린 메가타일 둘의 하위 칸 중 걷기 막힘 없는 칸(행 우선). `World.Gates` 가 생성자에서 한 번 계산한다.
  쓰는 곳: 나가는 자리 판정(`RoomFlow`), 봉인 그리기·문 위 아이콘 자리(WP5, 7.11), 미니맵 문 점(WP6). `GateZones.Opposite(MapDef, gateId)`: 변이 맞은편(nw ↔ se, ne ↔ sw)인 문 — 들어온 문 바꾸기(`RoomState.RemapEntry`).
- **걸을 넓이** `Map/MapArea.WalkableCells(ArenaGrid) → int`(계약·구현 WP1e): 주인공 시작 칸에서 걷기 막힘 없는 하위 칸을 4이웃으로 채운 칸 수(13.5-19 ① 과 같은 채우기). 기획의 "반경 14 걸을 넓이" 의 대리다 —
  13.5-19 ② 가 하위 1칸 틈을 막으므로 반경 14 가 못 지나는 걷기 칸 띠가 남지 않는다(방 바닥과 같은 근거, 13.15). 넓이 배율(3.6)의 분자·분모가 같은 함수다.
- **작은 방 검사**(`room_kinds` 가 있고 N ≤ `RoomMapRules.SmallRoomMaxN`(36) — 기획 13.5-23, `ArenaLayout.ValidateRooms` 에 더함, WP1e): 문 하나인 방 바닥(`RoomFloor.OneDoor`)이 없음(막다른 방 없음, 기획 10.8),
  문 넷 진입점(`entries` 의 `inside`)에서 `RoomMapRules.DoorClearance`(150px) 안에 **둘레가 아닌** 걷기 막힘 칸이 없음(칸 마름모 중심 거리 — 13.5-6 의 60px 와 같은 대상. 둘레 벽은 진입점에서 약 100px 라 빼야 규칙이 선다). 모두 오류.
- 맵 하나의 `open_center` 크기(작은 방 6×6)는 맵 데이터가 정한다 — 검사는 v0.6 그대로(안 기둥 ≤ 2)다.

**거울판** (v0.8, 기획 v0.8.1 10.8 — 계약·구현 WP1f) — 맵 하나로 방이 둘이 되는 싼 변형. 대각선 뒤집기(하위 (tx,ty) ↔ (ty,tx), 메가 [mx,my] ↔ [my,mx] — 화면 좌우 뒤집기)다.
그림을 뒤집지 않는다(원작 타일 그림은 빛 방향이 있어 뒤집으면 틀림) — **조각 이름을 짝으로 바꿔 다시 깐다**.

- **타일셋 짝 표** `mirror`(계약 `TilesetDef.Mirror` = `TilesetMirrorDef(Pairs, Self)`): `pairs` = 서로 바뀌는 조각 이름 둘(세로 벽 ↔ 가로 벽, 세로 벽 끝 ↔ 가로 벽 끝, 가로 창살 ↔ 세로 창살, 닫힌 문 가로 ↔ 세로 — L1 1 ↔ 2, 6 ↔ 7), `self` = 뒤집어도 그대로인 조각(모서리·작은 모서리 기둥·기둥·바닥).
  **짝 검사**(13.5-12 에 더함, 오류): 이름이 `pieces` 에 있음, 한 이름은 한 번만, 짝 (a, b) 마다 `props[b]` = `T(props[a])`, `self` 마다 `props[a]` = `T(props[a])` — `T` = 하위 칸 넷(위-왼, 위-오른, 아래-왼, 아래-오른)의 위-오른 ↔ 아래-왼 바꾸기.
  성질이 맞지 않는 짝은 거울판의 충돌·시야를 틀리게 하므로 표에 넣을 수 없다. 비교하는 것은 **최종 성질**(`props` — 보정을 반영한 값)뿐이다(v0.8.1 — v0.8 의 "보정이 있는 조각은 짝도 같은 칸에 보정" 규칙은 없앴다:
  짝 조각의 원본 SOL 이 이미 그 값이면 원작 수정판에 없는 "보정" 을 지어내게 만들었다. `sol_known_diffs` 는 원작 수정판이 고친 SOL 오류만 담는 자리다 — R24). 짝 조각의 원본 SOL 이 전치와 다르면 그 조각은 자기 성질 대조(1.4)를 따로 통과해야 하고,
  원작 수정판의 보정이 있으면 `sol_known_diffs`, 없으면 `design_diffs`(이유 "거울판 짝 맞춤" — 기획 v0.8.3 10.1 의 예외)에 둔다. 그것도 안 되면 그 조각은 짝 표에 못 들고, 쓰는 맵은 거울판을 못 단다(WP0 v0.8 ③).
  둘레(`borders`)·문(`gates`)·바닥(`floor`)은 짝 표에 넣지 않는다 — 자리로 고르므로(뒤집은 배치의 윗줄은 늘 `edge_ne` 번호) 거울판이 바뀐 자리의 조각을 다시 고른다.
  대신 **자리 짝 검사**(v0.8.1, 13.5-12 — `mirror` 표가 있는 타일셋만)를 둔다: 같은 재료(`borders`·`gates` 의 키)마다 둘레 자리 짝 `edge_nw` ↔ `edge_ne`·`edge_sw` ↔ `edge_se`·`corner_w` ↔ `corner_e` 는 `props[b]` = `T(props[a])`, `corner_n`·`corner_s` 는 자기 전치와 같음,
  문은 변 짝 nw ↔ ne·sw ↔ se 의 `open`·`end_a`·`end_b` 끼리 같은 비교(변을 따라 가는 순서가 전치로 보존되므로 `end_b` ↔ `end_b`). L1 은 지금 통과한다(1 ↔ 2·6 ↔ 7·18 ↔ 19·23 ↔ 24, 4·20 자기 전치) — 다음 타일셋(L2 등)에서 거울판 충돌이 원판의 전치와 조용히 달라지는 것을 막는다.
- **맵** `mirror`(계약 `MapDef.Mirror`, bool, 기본 거짓): 참이면 `MapDataReader` 가 읽을 때 거울판 `MapDef` 를 하나 더 만든다 — `MapMirror.Of(MapDef, TilesetDef) → MapDef`(Core `Map/MapMirror.cs`, 순수·결정적).
  거울판: `Id` = `<원판 ID>@m`(정의 표에는 없음 — ID 로 찾는 코드가 원판을 조용히 받지 않게), `MirrorOf` = 원판 ID, `IsMirror` = 참, `Art` = `<원판 art>@m`(가져오기가 따로 굽는다, 1.4 출력), 원판의 `Mirrored` 속성이 거울판을 가리킨다(`JsonIgnore` 셋).
  바꾸는 것: `place` 의 `at`·`line` 좌표와 `origin` 을 뒤집고 `grid` 블록을 전치(줄 ↔ 칸)하고 조각 이름을 짝으로, 문의 변(nw ↔ ne, sw ↔ se)과 ID(`gate_nw` ↔ `gate_ne`, `gate_sw` ↔ `gate_se` — `GateZones.MirrorGate`), `entries` 의 좌표 뒤집기와 **ID 도 같은 짝으로**(v0.8.1 — 입구 ID 는 문 ID 와 같다. v0.8 의 "ID 그대로" 면 거울판에서 입구 `gate_nw` 가 ne 변, 곧 문 `gate_ne` 앞에 놓여 주인공이 `GateIn` 과 다른 문에서 들어오고 `GateZones`(문 기준)와 이벤트 진입점(입구 기준)이 어긋났다),
  `hero_start`·`anchors` 좌표 뒤집기(**기준점 ID 는 그대로** — `center`·`room_*` 는 문 이름이 아니다),
  `hero_start.facing` 좌우(서 ↔ 동, 남서 ↔ 남동, 북서 ↔ 북동 — 남·북 그대로), `open_center`·`room_areas` 사각형 뒤집기([x0,y0,x1,y1] → [y0,x0,y1,x1] — 벽 메가가 위 줄·왼쪽 열을 막는 규칙이 뒤집어도 맞다). `border`·`palette`·`size`·`room_kinds`·`wall_fade` 는 그대로.
  짝 표에 없는 조각을 쓴 맵에 `mirror: true` 면 로딩 오류(`maps/x.json: 거울판 — 조각 grate_h 의 짝이 타일셋 l1 짝 표에 없다`). 바닥 섞기 해시는 거울판 ID 로 하므로 바닥 무늬는 원판과 다르다(그림만, 성질은 같음).
- **같은 검사**: 거울판도 원판과 똑같이 `ArenaLayout.Expand`·13.5-4~6·19·23(③ 문 넷·작은 방, 문 틈 닿음)·13.5-22(`audit` 의 DUN 배열 대조 — 거울판 배치도 펼쳐 대조)를 돈다(그림·시야·충돌 모두 다시 깐 배치에서 나온다).
  `RoomFloors`·`GateZones`·`MapArea` 도 거울판 `MapDef` 를 그대로 받는다 — 거울판을 아는 코드는 `MapMirror`·`RunPlanner` ①·`RoomState.RemapEntry`/`RemapMarch` 셋뿐이다.
- **쓰는 곳**: `RunPlanner` ① 의 (맵, 방향) 뽑기(3.6), `ROOM` 로그 `map=<원판 ID> mirror=1`, `--map <id>@m`(미리보기 — 9절), 가져오기 굽기 목록(원판 + `mirror: true` 인 맵의 거울판), 시작 검사 ③·13.5-10 의 맵 그림 이름(거울판 `art` 포함), 넓이 배율(`AreaScaleOf` — 거울판은 원판 값).
  점 빛 `[tx,ty]` 는 거울판에서 엉뚱한 자리가 되므로 거울판을 쓰는 액트도 점 빛은 기준점 ID 만 쓴다(13.5-23 ⑤ — "방 맵이 둘 이상" 조건에 "거울판이 있는 맵이 하나라도" 를 더함).
- `cathedral_arena` 는 L1 짝 표가 그 맵의 조각(창살 `grate_h`·닫힌 문 `door_closed` 포함)을 다 덮은 뒤에 `mirror: true` 를 단다(WP0 이 짝 조각 번호·성질을 확인 → WP5). 그 전의 단계 1 은 들어오는 문만 바뀐다.

### 6.12 스테이지 `stages` — v0.7: 스테이지 = 액트

v0.7(사용자 결정 R, 기획 2.5·13.1): 스테이지 정의 하나가 **액트** 하나다. 정의 종류·파일·ID 는 그대로(`stages/stage_cathedral.json`)이고, 맵 하나(`map`)를 **방 순번 표**(`rooms`)·**방 맵 풀**(`room_maps`)·**문 보상**(`rewards`)으로,
해금(`unlock`)을 **다음 액트**(`next_act`)로 바꿨다. `victory` 는 없앴다(판의 끝 = 마지막 액트의 마지막 방, 3.5·3.6). 생성 구간·이벤트의 시각은 **난이도 분 t** 다.

```json
{
  "stages": [
    { "id": "stage_cathedral", "name": "대성당", "next_act": "stage_catacombs",
      "room_maps": { "normal": ["cathedral_room_a", "cathedral_room_b"], "elite": ["cathedral_room_a", "cathedral_room_b"], "boss": "cathedral_arena" },
      "rooms": [                                                       // 순번 = 배열 순서(1부터 보임). t 범위는 0:00 부터 빈틈 없이(13.5-23)
        { "kind": "normal", "from": "00:00", "to": "01:00", "floor": 1 },          // 일반 방 생존 시간 = to − from. floor(v0.8) = 원작 층(방 문구·계단 문)
        { "kind": "normal", "from": "01:00", "to": "02:15", "floor": 1 },
        { "kind": "normal", "from": "02:15", "to": "03:30", "floor": 2 },
        { "kind": "normal", "from": "03:30", "to": "05:00", "floor": 2 },
        { "kind": "elite",  "from": "05:00", "to": "06:00", "miniboss": "rotfeast", "floor": 2 },   // 정예 방: 처치까지(t 는 to − 1틱에서 멈춤 — 구간 [5:00, 6:00) 을 끝까지 씀), 생성은 elite_spawn_cap 까지
        { "kind": "normal", "from": "06:00", "to": "07:30", "floor": 3 },
        { "kind": "elite",  "from": "07:30", "to": "08:30", "miniboss": "blackash", "floor": 3 },
        { "kind": "normal", "from": "08:30", "to": "10:00", "floor": 4 },
        { "kind": "boss",   "from": "10:00", "boss": "skeleton_king", "floor": 4 } ],                  // 보스 방: to 없음(t 안 멈춤)
      "rewards": { "weights": { "reward_tome": 30, "reward_chest": 30, "reward_potion": 15,             // v0.8(기획 v0.8.1 11.4): 성소 셋, 축복 성소 reward_shrine 없앰
                                "shrine_enchanted": 9, "shrine_fascinating": 8, "shrine_gloomy": 8 },   //   단계마다 그 단계에 있는 ID 만 적는다(단계 1 = 마법서·보물 상자)
                   "first_room": "reward_tome", "gold_scale": 1.0 },                     // 액트 2: first_room reward_chest, gold_scale 1.5(기획 11.4)
      "lighting": { "ambient": [200, 200, 215], "darkness": 0.5,
                    "hero_light": { "color": [255, 240, 220], "radius": 420 },
                    "unit_min_light": 0.6,
                    "lights": [] },
      "scaling": { "hp": [1.0, 0.08], "damage": [1.0, 0.03] },                         // a + b × t
      "elite_chance": { "from": "02:00", "at": "00:30", "chance": 0.25 },             // v0.7: t 시작 ≥ from 인 일반 방마다 방 시계 at 에 한 번
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
        { "at": "06:00", "type": "elite_pack", "enemy": "ghoul", "count": 4, "affixes": 1, "unique_chance": 1.0 },
        { "at": "06:30", "type": "march", "enemy": "fallen", "count": 20, "at_entry": "gate_se", "toward_entry": "gate_nw", "straight": 15 },
        { "at": "07:30", "type": "miniboss", "enemy": "blackash", "at_entry": "gate_nw" },
        { "at": "10:00", "type": "boss", "enemy": "skeleton_king", "at_entry": "gate_ne" }
      ] }
  ]
}
```

- 배율 식 `[a, b]` = a + b × t(v0.7 — v0.6 은 "경과 분"). 구간은 `from`/`to`(t), 보스 중 구간은 `during: "boss"`(보스 방에서 보스가 나온 뒤). 이벤트 종류 = 기획 13.4, 각 종류가 `PartKind.StageEvent` 부품(레지스트리 등록, WP4).
- **방 순번 표 `rooms`**(v0.7, 계약 `StageDef.Rooms` = `IReadOnlyList<RoomDef>`): 원소 = `kind`(`normal`·`elite`·`boss`), `from`·`to`(t 범위 — `ClockTime`, 보스 방은 `to` 없음), `miniboss`(정예 방의 중간 보스 `DefRef<EnemyDef>` — 처치가 생존 끝, 문 위 표시),
  `boss`(보스 방 — 13.5-23 대조·문 위 이름), `number`(v0.7.1, 선택 — 보이는 방 번호, 없으면 배열 자리 + 1, 3.6). 일반 방 생존 시간 = `to − from`(따로 적지 않음 — 기획 2.5 "생존 시간 = t 범위 길이"). 정예 방의 `to` 는 t 가 멈추는 곳일 뿐이다.
  `floor`(v0.8, 계약 `RoomDef.Floor` int, 선택 — 0 = 안 적음, 기획 v0.8.1 2.5 "층"): 방 문구 `대성당 2층 — 3`(감독 `Banner`)과 층이 바뀌는 출구의 `RoomExit.NextFloor`(문 위 `▼ 2층`·넘김 어둠 `transition_floor`)에만 쓴다 — 난이도·씨앗과 무관.
  한 액트의 방이 모두 적거나 모두 안 적고, 적었으면 1 이상이고 줄지 않는다(13.5-23 ①). 액트를 넘는 계단(대성당 4층 → 지하묘지 5층)도 `NextFloor` 를 단다.
  v0.7.1: 정예 방의 t 는 `to` 가 아니라 **`to` − 1틱**에서 멈춘다 — 구간은 `[from, to)` 로 찾으므로 `to` 에서 멈추면 중간 보스가 1:00 넘게 살아 있을 때 다음 방의 센 구간·새 종(6:00 구간의 썩은 시체 등)이 정예 방에 나온다.
  `RoomDef` 는 데이터 모양이고, 방 하나의 실행 상태는 `World.Room`(3.6)이다.
- **방 맵 풀 `room_maps`**(계약 `RoomMapPoolDef`: `Normal`·`Elite` = `IReadOnlyList<DefRef<MapDef>>`, `Boss` = `DefRef<MapDef>?`): 순번 표에 있는 종류마다 맵이 하나 이상(13.5-23). 고르기는 `RunPlanner`(3.6).
- **문 보상 `rewards`**(계약 `StageRewardsDef`): `weights`(보상 ID → 가중, JSON 순서 배열 — 3.4), `first_room`(1번 방 고정 보상), `gold_scale`(보물 상자 금화 배율). 보상 정의는 6.18.
- **`next_act`**(계약 `StageDef.NextAct` = `DefRef<StageDef>?`, v0.6 의 `unlock` 을 대신함): 보스 방 뒤 계단이 가는 액트. 마지막 액트는 비운다. 판은 늘 사슬의 뿌리 액트에서 시작한다(`--stage`·`--room` 만 다른 액트에서).
- **`slice`**(계약 `StageDef.Slice`, 기본 거짓 — 단계 1 세로 조각 판): 참이면 정예·보스 방이 없어도 되고 t 범위 빈틈을 허용한다(13.5-23 두 규칙만 끔). 단계 1 의 `stage_cathedral` 은 순번 1·2·3·4·6·8(기획 10.3) 여섯 줄을 원래 t 범위 그대로 적고 `slice: true`, `next_act` 없음 —
  마지막 방(8번) 정리가 판의 끝이다(3.5). 단계 2 에서 전체 판으로 바꾸며 지운다. v0.7.1: 여섯 줄에 `number` 를 기획 번호(1·2·3·4·6·8)로 적는다 — 화면·로그·`--room`·씨앗·깊이가 모두 기획 번호라
  단계 1 은 `대성당 6 / 8` 처럼 보이고(빠진 번호는 건너뜀), 단계 1 의 `--room 1:6` 과 단계 2 의 `--room 1:6` 이 같은 t 의 방, 단계 1 끝은 `depth=1:8` 이다(v0.7 의 "배열 순서" `[해석]` 을 없앰).
- `elite_chance`(v0.7 계약 변경 — `every` 를 없애고 `at` 을 더함, `at` 기본 0:30): 정예 무리 판정은 **t 시작 ≥ `from` 인 일반 방마다 방 시계 `at` 에 한 번**(기획 2.3). 정예·보스 방에서는 하지 않는다.
- (v0.5.3, 기획 v0.6.2 9.4·10.3·10.4 — v0.7: 방으로) 중간 보스는 액트 1 이 정예 방 5 로트피스트·7 블랙애시, 액트 2 가 3 데스스핏·6 스틸메이스·9 슬러지 남작이다. `miniboss` 이벤트는 그 정예 방 t 시작(5:00 등)에 두고 방 시계 0:03 에 입장한다.
  정예 방이 따로라 v0.6 의 "앞 중간 보스가 살아 있어도 다음이 제 시각에 나옴"(겹침)은 생기지 않는다(기획 9.4). 정예 방이 쓰는 구간(t 시작 ~ `to` 직전 — v0.7.1)은 앞 구간보다 줄인 값이고, 이 리듬은 검증 13.5-18(6.14)이 경고로 지킨다.
  위 예는 이벤트 일부만 보인다 — 전체 값은 기획 10.3·10.4 표가 정본이다.
- **이벤트는 방으로 옮겨진다**(v0.7, 3.6 `RoomEvents`): 시각 `at` 은 t 이고, 그 t 가 든 방의 방 시계 `t − 방 t 시작`(유예 0:03 전이면 0:03)에 온다. 생존이 끝나면 남은 것은 버린다. `entry:` 는 시작 문이 들어온 문일 때만 바꾼다(`Room.RemapEntry`·`RemapMarch`, v0.8.1 — 시작 문은 맞은편, 행진 목표 문은 들어온 문 ↔ 맞은편만 맞바꿈. v0.7.1 의 서로 바꾸기는 없앰, 3.6 결정 5).
  **기준점은 맵마다 다르다**(v0.7.1): 이벤트가 가리키는 기준점(`toward`·`anchor` 등)은 그 t 가 든 방 종류의 풀에 있는 **모든 맵**에 있어야 한다(13.5-23 ⑤). 맵 풀이 여럿인데 한 맵에만 있는 기준점을 쓰면 판 중간 `RunSession.Advance` 에서 `World` 생성이 예외가 된다.
  `light_change` 는 그 액트 끝까지 남는다 — 방을 만들 때 지난 것을 다시 적용한다(3.6).
- **넓이 배율**(v0.7, 기획 10.8): 감독은 구간의 `max_alive` 에 `World.Room.AreaScale` 을 곱해(반올림) 쓴다. `per_second` 는 곱하지 않는다.
- `weights` 는 JSON 객체로 적지만 로딩 때 **적힌 순서의 배열**로 바뀐다(3.4). 감독은 종류를 먼저 뽑고, 그 종류의 `spawn_group` 이 있으면 무리 크기를 거기서 뽑는다(6.5).
- v0.6 의 `victory`(`boss` / `survive` + 시각)는 v0.7 에서 없앴다 — 판의 끝은 `RunSession` 이 안다(마지막 액트의 마지막 방 — `RoomState.IsLastInRun`, 3.5·3.6).
- **단계 1 세로 조각**(기획 14.1·10.3): 그때는 폴른 원·썩은 시체·핀드·불타는 해골 궁수 정의가 아직 없어 전체 구간표가 "없는 적" 오류가 된다.
  WP4 는 단계 1 에 `stages/stage_cathedral.json` 을 **단계 1 판**(5종 가중치, `slice: true` 로 일반 방 여섯, 보스·중간 보스 이벤트 없음, `room_maps.normal` = `cathedral_arena` 하나, 보상 가중은 마법서·보물 상자 둘)으로 쓰고, 단계 2 에서 전체 판으로 바꾼다. 두 판을 같이 두지 않는다.
  풀이 맵 하나라 "연달아 쓰지 않음" 은 꺼지고 들어오는 문만 바뀐다(기획 10.8). v0.8: `cathedral_arena` 가 `mirror: true` 를 달면 원판·거울판이 번갈아 나온다(3.6 ①).
- `march` 방향(v0.3, 등각 맵이라 방위보다 목표가 자연스럽다): `toward`(기준점 ID) / `toward_entry`(입구 ID — 그 진입점 쪽) / `heading`(방위, v0.2 그대로) 중 하나, `straight` = 직진 시간.
  `toward_entry` 행진은 그 진입점에 닿으면 직진 시간이 남아 있어도 보통 행동으로 바뀐다(기획 10.3 v0.5 — 빠른 핀드가 맞은편 벽까지 가서 멈춰 있지 않게, 6.6 `MoveOrder.Target`).
  매개변수 이름은 이벤트 부품을 만드는 WP4 가 정한다(11.3).
- **조명**(계약 변경 WP1b): v0.2 의 `kind_lights`(장애물 종류별 빛)는 장애물 종류와 함께 없앴다. 대신 `lights: [{ "id", "at": 기준점 ID 또는 [tx,ty], "color", "radius", "flicker", "pulse": { "period", "amount" } }]`
  (점 빛, MVP 두 스테이지는 비어 있음 — 기획 10.3·10.4 "화로·기둥 빛 없앰"). `[tx,ty]` 는 `TileCoord` 이고 `World.Lighting` 은 로딩 때 월드 px 로 바꾼 값을 가진다(4.1 `ResolvedMap`).
  v0.7.1: 점 빛은 액트(모든 방)의 것이라, 방 맵이 둘 이상인 액트(`room_maps` 의 서로 다른 맵 수 ≥ 2 — v0.8: 또는 `mirror: true` 인 맵이 하나라도 있음)는 `at` 에 **기준점 ID 만** 쓰고 그 기준점이 액트의 모든 방 맵에 있어야 한다(13.5-23 ⑤ — 크기가 다른 맵에서 같은 `[tx,ty]` 는 엉뚱한 자리). 맵마다 다른 빛이 필요해지면 점 빛을 `MapDef` 로 옮긴다(계약 변경).
- `unit_min_light`(선택, 기본 0 = 끔): 주인공·적·시체·획득물이 받는 빛의 하한(0~1, 기획 10.5 의 "유닛 밝기 하한"). 바닥·벽은 원작처럼 어둡게 두고 이것들만 어둠에서 읽히게 한다(7.7).
  값은 기획 10.3~10.5(v0.5): 두 스테이지 모두 0.6, 어둠 스테이지 1 0.5·스테이지 2 0.6·스테이지 2 11:30 부터 0.7(상한 0.7), 주인공 빛 반경 420·400(380~480 안 — v0.5.1: 기획 v0.6.1 이 스테이지 2 를 380 → 400).
  `light_change` 의 경로에도 들어간다. v0.5: 하한은 **그리는 대상**(안개 알파 > 0 — 시야 안, 또는 늘 보이는 보스·닿은 적)에만 쓴다(7.7·7.10, 기획 10.5).
- **`hero_light.radius` = 시야 반경**(v0.5, 기획 10.7): `VisibilityField.Radius` 는 틱마다 `World.Lighting.HeroLightRadius` 를 읽는다(4.11). 시야 반경을 따로 적는 칸은 없다.
  `light_change` 로 `hero_light.radius` 를 바꾸면 **다음 틱부터** 시야도 바뀐다(감독이 2단계에서 바꾸고 시야는 1단계에서 읽는다, 4.11 — MVP 두 스테이지에는 없음).
  범위는 13.5-15(380 ≤ R ≤ 480, 원거리 띠 최대 + 60 ≤ R).
- **일반 생성 자리**(v0.5.1): v0.5 의 "보이는 생성 후보 버림"(기획 2.4·10.7 v0.6)은 **뺐다**(기획 리뷰 (가) — 기획 v0.6.1 도 뺐다). 생성 띠는 늘 화면 밖이라 거기 선 적은 안개와 무관하게 안 보이고,
  위아래에서 오는 적은 화면 가장자리(270 < R − 40)에서 이미 시야 안이라 어디서 생기든 빛 속에서 들어온다 — 규칙의 실제 효과가 생성 분포를 양옆으로 미는 것뿐이었다.
  게다가 시야가 꺼진 월드에서 "보이면 버림" 이 후보 12개를 모두 버리는 뒤집힘(구현·검증 리뷰)과 R23 비율을 부풀리는 값이 따라왔다. 생성은 v0.4 그대로(띠·걸을 수 있음·여유 거리, 12번 → 먼 입구)다.
  나중에 비슷한 규칙(생성 후보를 시야로 거르기)을 다시 넣으면 질의는 `HiddenForSpawn(pos)` = `!Enabled || !IsVisible(pos)` 꼴로 둔다 — 꺼진 월드에서 아무것도 버리지 않는 쪽이 기본값이어야 한다(구현·검증 리뷰).
- `light_change` 이벤트는 바꿀 곳을 경로로 고른다: `{ "at": "11:30", "type": "light_change", "set": { "darkness": 0.7 } }`(스테이지 2). 받는 경로: `ambient`, `darkness`, `unit_min_light`, `hero_light.*`, `lights.<id>.*`.
  감독(WP4)이 `World.Lighting` 을 바꾸고 렌더(WP5)가 읽는다.
- 이벤트가 적 정의를 덮어쓰는 매개변수: `escort` 의 `escorts`(호위 수, 0 포함), `march` 의 방향·`straight`, `boss`/`miniboss` 의 `enrage`·`arrival`. 호위 수는 `SpawnContext.EscortCount` 로 넘기고(생성 훅이 읽음), 행진 지시·광폭화·등장 연출은 이벤트가 생성 직후 그 적에 직접 건다(6.6).
- 스테이지 음악은 스테이지 정의에 두지 않고 소리 연결 표의 `music.<스테이지 ID>`(6.15). v0.7: 같은 액트 안에서 방이 바뀌어도 곡을 이어 튼다(7.9).
- **`requires`**(v0.6, 6.17): 그 스테이지가 쓰는 헬파이어 묶음(지하실·둥지 `["hellfire"]`, MVP 두 스테이지는 비어 있음). 구간표·이벤트·보스·소환·분열·호위로 나오는 적의 `requires` 는 이 목록 안이어야 한다(13.5-20) —
  그래서 묶음이 없을 때 "구간표에서 몇 종만 빠지는" 일은 없고 스테이지가 통째로 꺼진다(기획 19.4). v0.7: 스테이지 선택 화면이 없어(8.1) 꺼진 액트는 `--stage`·`--room` 이 종료 코드 5 로 막고 타이틀 한 줄로만 알린다.
  **다음 액트 사슬**(v0.7 — v0.6.1 의 해금 사슬을 옮김): `requires` 가 빈 액트(기본)의 `next_act` 는 `requires` 가 빈 액트만 가리킨다(13.5-20 오류) — 헬파이어 없는 PC 에서 판이 계단에서 끊기지 않게.
  기본 액트의 방 맵 풀에 묶음 타일셋 맵이 있어도 오류(13.5-10 묶음판 귀속 — 맵 그림 → 그 맵을 풀에 둔 스테이지). 헬파이어 액트를 어디에 잇는지(계단 둘 / 따로 판)는 기획 열린 질문 38 — 계단 둘이면 `next_act` 를 목록으로 바꾼다(계약 변경).
- **안쪽 방과 생성**(v0.6, 기획 2.4·10.1): 일반 생성 자리 규칙은 그대로다(생성 띠·걸을 수 있음·여유 거리) — 방 안도 후보다. 갇힌 바닥 칸이 없으므로(13.5-5) 방 안에서 생긴 적은 문으로 나와 플로우 필드를 따라온다.
  포위 고리·`summon`·`telegraphed_blast` 지점은 4.10 의 길 거리 조건을 쓴다(벽 너머 방 안 자리 버림).
- **방 보급 `room_supply`**(v0.6.3, 기획 10.1·10.3·10.4·13.4 — v0.7: 시각은 보스 방의 t, 액트 1 10:40·11:40·12:40·13:40, 기준점은 그 보스 방 맵의 `room_*`. 작은 방에는 문 하나인 방이 없어 보급도 없다): `{ "at": "10:40", "type": "room_supply", "anchor": "room_blood", "pickup": "potion_health" }` — 이벤트 부품(WP4). 그 시각에
  `World.Spawner.SpawnPickup(new PickupSpawn(정의, World.Resolved.Anchor(anchor), SupplyRoom: anchor))` 로 기준점 자리에 획득물 하나를 놓는다.
  같은 기준점에 **살아 있는 보급**(`World.Pickups` 중 `SupplyRoom == anchor`)이 있으면 생략한다(쌓이지 않음) — "안 주운 보급" 상태는 이 필드 하나이고 따로 표를 두지 않는다.
  씨앗을 쓰지 않는다(같은 시각·같은 자리). 보급은 수명·상한 정리에서 빠지고(WP3) 미니맵에 흰 점으로 늘 보인다(WP6 — 가 본 곳이 아니어도, 8.2). 주우면 WP3 가 틱 이벤트 `SupplyCollected(방 ID, 정의, 자리)` 를 낸다.
  매개변수 이름은 WP4 가 정하는 자리지만(위 "매개변수 이름") 13.5-19 의 방 보급 검사(WP7)가 읽으므로 `anchor`·`pickup` 으로 고정한다. 보급은 위협이 아니라 13.5-18 리듬 검사에 세지 않는다(기획 10.3).
- **방 체류 압박**(v0.6.3, 기획 10.1·열린 질문 30, 값 `rules.spawn.room_pressure` — 6.10): 감독(WP4, 2단계)이 틱마다 주인공 자리의 방 번호(`World.InnerRooms.IndexAt(Hero.Pos)`)로 `World.InnerRoomPresence` 의 `Current`·`EnteredTick` 을 고친다(바뀌면 `EnteredTick` = 지금 틱).
  `Current` 방이 `OneDoor` 이고 머문 시간이 `after` 초를 넘으면 그동안 일반 생성 무리 `every_nth` 번째마다 하나(감독의 무리 순번으로 셈 — 결정적)를 그 방 바닥 칸 중 주인공에서 `min_distance` px 이상인 걷기 칸에 등장 연출(2.4)로 낸다.
  후보 칸은 바닥 칸 목록에서 `World.Rng` 로 뽑고(`spawn.tries` 번), 다 실패하면 그 무리는 보통 자리에 낸다(조용히 버리지 않음). 방을 나오면 바로 멈춘다. `room_pressure` 가 없으면 끈다.
  누적(방마다 체류 틱·받은 피해·주운 보급)도 감독이 같은 자리에서 앞 틱의 `HeroDamaged`·`SupplyCollected` 로 센다 — 사람이 하는 판도 세어 판 끝 로그 `INNER_ROOMS`(9절)가 쓴다.
  v0.7: `RoomPresence` 는 `World` 수명이라 **방 하나(그 맵)** 의 누적이다. `ROOMS` 는 판 끝이 아니라 방이 끝날 때마다(`RunSession.Advance` 직전, 판의 마지막 방은 결과 때) 쓴다. 줄에 `act=`·`room=`(순번)·`map=` 을 더하고 기준점은 `anchor=` 로 옮긴다(9절).
  v0.7.1: 속성 이름을 `World.InnerRoomPresence`(`World.Rooms` → `World.InnerRooms`)로, 로그 이름을 `INNER_ROOMS` 로 바꿨다 — 판 단위 방(`World.Room`·`ROOM`)과 한 글자 차이였다(11.2). "판의 마지막 방은 결과 때" = `RunSession.Conclude`(3.6).

### 6.13 몬스터 늘리기 — 구조·시험·이번에 늘린 것 (사용자 요구 8)

새 그림을 그리지 않고 종류를 늘리는 길을 데이터 쪽에 네 겹으로 둔다(기획 8.1·19.5 "원작에 없는 것은 원작 재료로").

1. **원작 그림 묶음 + 원작 TRN 변종**: `sprites` + `variant`. 원작도 묶음 하나를 TRN 으로 네 종에 나눠 썼다(좀비 묶음 = 좀비·구울·썩은 시체·블랙 데스). 새 변종 = 가져오기 표 한 줄 + 그림 묶음 `variants` 한 칸.
2. **덧입힘 변형(`extends`)**: 색조·발광·알파·크기·수치·부품 일부. `behaviors_add`·`behaviors_patch` 로 부모 부품을 다시 적지 않는다. 몸 반경은 `base_radius × scale` 로 저절로 맞는다(6.5).
3. **부품 조합**: 일반 적 부품 13종 + 적 동작 7종(패턴·훅 안쪽)을 **어떤 적이든** 쓸 수 있다. 원거리 부품은 투사체 ID 를, 오라·함성은 버프 ID 를 받는다.
4. **등급 × 접두사**: 17종 × (일반/정예/우두머리) × 접두사 조합이 실행 중에 만들어진다. 접두사 제외 조건(6.7)이 어색한 조합을 막는다.

**MVP 17종(+분열체)과 중간 보스 5·보스 2** (기획 8.2·9.4·9.5 — v0.5.3: 중간 보스 2 → 5) — 새 부품은 두 곳뿐이다. v0.5.3 의 새 중간 보스 셋은 새 부품 없이 되고 패턴 조건 `clear_sight` 하나만 더한다(6.5).

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
| `rotfeast` / `steelmace` | `set_zombie` · `eth` ×1.6 / `set_goatmace` · `bhsm` ×1.4 + 붉은 발광 | 중간 보스 패턴(기획 v0.3 의 썩은 거인·타락한 기사 대장 그대로). 로트피스트 독 웅덩이는 `ground_hazard(trigger: cooldown)` | — |
| `blackash` (v0.5.3) | `set_skelbow` · `bashtb` ×1.5 + 발광 | `kite` + 패턴 4(`summon` ×2·`telegraphed_blast`·`projectile_spread(pj_fire_arrow)`, 뒤 둘 `clear_sight`) — 6.5 | (`clear_sight` 조건, WP4) |
| `deathspit` (v0.5.3) | `set_acid` · `bfds` ×1.5 | `ground_hazard(trigger: cooldown)` + 패턴 `projectile_spread(pj_acid)` 둘(사방 / 부채 `clear_sight`) | — |
| `baron_sludge` (v0.5.3) | `set_fat` · `bsm` ×1.5 | 패턴 `summon(mud_man)`·`melee_slam` + 예고, `split_on_death(mud_man)` | — |
| `skeleton_king` / `butcher` | `set_sking` · `genrl` / `set_fatc` · `genrl` ×1.3 | 보스 패턴·페이즈(6.5) | — |

- 기획 v0.3(설계 v0.2)의 `skeleton_mage`·`wraith`·`goatman_shaman`·`plague_zombie`·`fallen_knight`·`bone_brute`·`hell_goat`·`rotting_hulk`·`blood_king` 과 그 투사체는 없앤다(기획 8.2 "v0.3 에서 바뀐 것").
- **확장 후보(E, 기획 8.4)의 비용**: 대부분 "가져오기 표 한 줄 + 정의 1개". 새 부품이 필요한 것만: `scavenger`(`eat_corpse`), `hidden`·`counselor`(`blink`, 자리는 `PlacementResolver`),
  `gargoyle`(`stone_form`), `blood_knight`(`lifesteal` — 접두사 `vampiric` 과 공유, `HitLanded` 사용).
  멀리서 피해를 주는 새 부품(투사체·원거리 즉발·돌진류)은 시작 조건에 `EnemyCtx.SeenClearly` 를 넣는다(v0.5, 6.6 — 보이지 않는 곳에서 맞지 않음). 보스 패턴으로 겨누어 쏘면 `clear_sight`(v0.5.3).
- **헬파이어 몬스터의 비용**(v0.6, 기획 8.5·13.6): "가져오기 표 한 줄(`bundle: hellfire`) + 그림 묶음 정의 + 적 정의 1개(`requires: [hellfire]`) + 그 스테이지 가중치". hellfire.mpq 의 그림 묶음 24개에는 원작 TRN 이 없어 `variants` 는 `[""]` 뿐이고,
  예외로 diabdat 그림(`demskel\demskl`)을 빌리는 스컬윙은 diabdat `monsters\skelaxe\skelt.trn` 을 변종 `skelt` 로 단다(v0.6.1 — 같은 그림의 본 데몬은 원색. 조사 문서 확인),
  고유 셋(호크 데몬·디파일러·나크룰)은 diabdat 의 `monsters\monsters\genrl.trn` 을 변종으로 단다(표 항목은 `bundle: hellfire` 인데 TRN 경로만 diabdat — 찾는 순서 1.4 가 그대로 찾는다).
  기본 스테이지에 넣으면 13.5-20 오류다. 헬파이어 몬스터와 원작 투사체(`ms_*`·`ex_*`)의 연결은 아직 확인하지 않았다 — 그 스테이지를 만들 때 원본으로 본다.
- **중간 보스 늘리기의 비용**(v0.5.3, 기획 9.4 "고르는 기준" ②): 그림 묶음이 그 스테이지 목록에 이미 있는 원작 고유 몬스터면 "가져오기 표 `variants` 한 칸(고유 TRN `monsters\monsters\<이름>.trn`) + 그림 묶음 `variants` 한 칸 + 정의 1개 + `miniboss` 이벤트 한 줄 + 그 구간 줄임" 이다.
  쓰는 클립이 그 묶음 정의에 아직 없으면(블랙애시·데스스핏의 `special`) 클립 한 줄과 소리 연결 `special` 한 줄이 더 든다(WP5·WP8).

**"새 적 = JSON 1개" 를 자동으로 확인하는 시험** (WP4, 10.1 `Enemies`)

- **적 정의 전수 연기 시험**(`Theory`, 실제 `Data/` 의 생성 가능한(`abstract` 아님) 적 전부 + `Fixtures/variants/`): 빈 맵에 무적 주인공과 150px 떨어뜨려 하나 생성 →
  10초(600틱) 동안 예외·NaN 없음 → 근접형은 주인공 쪽으로 움직였거나 피해 요청을 냈고, 원거리형은 거리 띠 안에서 투사체를 냈음 → HP 0 으로 만들면 죽음 훅이 돌고 저장소에서 빠지고 보석 1개.
  보스·중간 보스는 패턴 하나 이상이 실행됐는지를 본다. **새 적을 데이터에 넣으면 이 시험이 저절로 늘어난다.** 그림이 없어도 돈다(그림은 정의 값만 쓴다).
  v0.5.3: 빈 맵은 시야가 꺼진 시험 월드(`Enabled = false` — 질의 늘 참, 4.11)라 `clear_sight` 패턴도 돈다(블랙애시는 150px 에서 띠 220 까지 물러난 뒤 부채를 쏜다).
- **코드 없는 변형 증거**: `tests/…/Fixtures/variants/ash_skeleton.json`(`extends: skeleton` + 색조 + `behaviors_add: [ground_hazard on_death]` + `behaviors_patch`)을 위 시험에 넣는다.
  이 파일을 바꾸면서 C# 을 고쳐야 했다면 구조가 깨진 것이다.
- `--preview enemies` 는 배치를 고정한다(9절): 적 정의마다 한 행(ID·변종, 방향 0·고정 프레임으로 걷기/공격/특수/죽음 칸 + 8방향을 도는 칸) → 같은 시각 캡처가 늘 같은 그림이라 정의를 고친 전후를 나란히 비교할 수 있다.
- 검증 규칙이 "그 적이 쓰는 클립(`attack`·`special`)이 그림 묶음에 있는가", "`event_frame` ≤ 그 클립 프레임 수", "`variant` 가 그림 묶음 `variants` 안에 있는가" 를 본다.
- 데이터 경고 두 개(6.14)가 "늘렸는데 화면에서 안 보이는" 경우를 잡는다: 어느 스테이지에서도 나오지 않는 적, 같은 스테이지에서 구별되지 않는 두 적(같은 묶음 + 같은 변종 + 비슷한 덧입힘).

### 6.14 정의 간 참조 검증 목록

| 규칙 | 내용 | 위치 |
|---|---|---|
| 13.5-1 | 종류 안 ID 유일, 모든 `DefRef` 존재(그림·그림 묶음·이펙트·투사체·버프·타일셋·맵·입구 `entry:`·기준점·몬스터·드롭 표·진화 재료·보스), 맵의 테두리·문·조각 이름이 타일셋에 있음, 적 `variant` ∈ 그림 묶음 `variants`. **v0.7.1**: 스테이지의 이벤트·점 빛이 가리키는 기준점·입구는 "그 스테이지 맵 하나" 가 아니라 13.5-23 ⑤ 의 **풀 전체** 규칙으로 본다 | Data (WP7) |
| 13.5-2 | 레벨 표 길이 = 최대 레벨, 키 = 패턴 `LevelKeys`, 단조성. `null` 은 `NullableKeys` 에서만 | **Data(발사 패턴 등록 여부 — `WeaponDef.Pattern` 의 `[PartType(FirePattern)]`) + WP3 검사기(레벨 키·단조성·null)** (WP1 코드) |
| 13.5-3 | 생성 구간이 0:00 부터 빈틈·겹침 없이, 가중치 합 > 0, `during: boss` 는 보스 있을 때만. **v0.7**: 구간은 t 이고 0:00 부터 **보스 방 t 시작**(보스 방이 없는 `slice` 판은 마지막 방 t 끝)까지 빈틈 없이 덮는다, `during: boss` 는 보스 방이 있을 때만 | Data |
| 13.5-4 | 배치가 M×M 안, 쓰는 메가타일이 모두 성질 표에 있음, **성질 표의 키 = 타일셋이 이름 붙인 번호**(13.5-12), 문 칸이 비어 있음, 놓은 조각이 테두리와 겹치지 않음(v0.6: `over_border` 놓기는 문 칸만 아니면 됨), 놓기마다 `at`·`line`·`grid` 중 하나, `grid` 칸 이름이 타일셋에 있음. 충돌 도형 묶음끼리·경계와 80px 이상 또는 붙음 | Map(WP1b 배치) + Nav/Validation (WP2) — **자원 없이 늘 돈다** |
| 13.5-5 | 길 검사: 반경 14(주인공 시작 → 모든 입구 진입점·도달 칸 — v0.6.1: 모든 직업의 `radius` ≤ 14 는 13.5-21 이 지킨다), `NavConst.FieldS`·`FieldM`(16·26)과 34(입구 진입점 → `center`, **v0.6: 그리고 모든 방 기준점 `room_*`** — v0.6.1: 반쯤 막힌 구역 기준점 포함, 6.11), 주인공이 설 수 있는 모든 칸에서 `S` 필드 시작점 ≥ 1. **v0.6 갇힌 바닥 칸(오류)**: 주인공 시작에서 반경 14 로 닿는 내비 칸(clearance ≥ 14, 8방향·코너 자르기 금지 채우기)을 구하고, 걷기 막힘이 없는 맵 안 하위 칸마다 "닿는 내비 칸 중심이 그 칸 마름모에서 14px 안에 있다"(주인공 몸이 그 칸에 걸칠 수 있다)가 아니면 `갇힌 바닥 칸 (tx,ty)` — 칸 목록을 메가타일로 묶어 한 줄씩. 자동으로 막힘 처리하지 않는다(그림과 충돌이 어긋남, 기획 10.1) | Nav/Validation (WP2) — 늘 돈다 |
| 13.5-6 | 입구 진입점·시작 위치의 clearance ≥ 38, 바깥 시작점은 맵 밖(바깥 마름모 밖), **v0.6: 입구 진입점에서 둘레가 아닌 걷기 막힘 도형(안쪽 방·짧은 벽·기둥·창살)까지 > 60px**(기획 10.1 "방은 진입점에서 60px 넘게") | Nav/Validation (WP2) — 늘 돈다 |
| 13.5-7 | **정의끼리**(늘): grid 시트 = 프레임 × 개수(+slack), atlas 는 방향·프레임 > 0·`origin` 이 칸 안, 클립 존재, `event_frame` ≤ 프레임 수. **실제 자원**(있을 때만): manifest 판, atlas 메타의 칸·방향·프레임 = 정의, 사각형이 페이지 안, PNG IHDR = 메타 페이지(변종마다), grid PNG = `sheet`, 맵 `arena.json` 의 `layout_hash` = 배치 | Data / Art (WP5) |
| 13.5-8 | 진화 재료 무기의 최대 레벨, 재료 패시브 존재 | Data |
| 13.5-9 | 패시브 키 ∈ `StatKey`, 접두사·버프 키 ∈ `EnemyStatKey` | Data |
| 13.5-10 | 정의가 가리키는 파일 이름(atlas 페이지 × 변종, 메타, grid PNG, 맵 `art` 폴더의 파일 — `arena.json`·`columns.*`·`minimap.png`·바닥 조각 이름 규칙(1.4)으로 계산한 `floor_*`, 소리·음악 파일, 소리 색인) ⊂ **가져오기 표가 만드는 이름 + `Table/exe.json`** — 자원 없이 돈다(기획 13.5-10). 두 쪽으로 나눈다: ① 표 → 이름 목록(`ImportTable`, 기본 분류) ② 정의 → 이름 추출과 대조(실제 `Data/` 를 WP7 로더로 읽으므로 `Integration`). **v0.6 묶음판**: ② 에서 정의 D(적·스테이지·직업·무기·음악 — 그림 묶음·소리·이펙트처럼 `requires` 가 없는 정의는 그것을 가리키는 D 들의 것으로)가 닿는 파일 이름의 묶음(`ImportTable.OutputBundles(arenas)`)이 기본이거나 D 의 `requires` 안. **v0.6.1 귀속 규칙**: 맵 그림(`art` 폴더의 파일) → 그 맵을 쓰는 스테이지 D(그래서 "스테이지 `requires` ⊇ 맵 타일셋 항목의 `bundle`" 이 여기서 서다). 소리 연결 표 줄 → `hero.<id>` 는 그 직업, `enemies.<id>` 는 그 적, `sprite_sets.<s>` 는 `s` 를 쓰는 적·직업들, `weapons.<w>` 는 그 무기, `music.<스테이지>` 는 그 줄이 가리키는 음악 정의(음악 D), `projectiles`·`effects`·`hazard_loops` 는 그것을 가리키는 D 들(없으면 기본), `ui`·`pickups`·`music.title` 은 기본. ② 의 "정의 → 이름(주인 D 포함)" 추출은 Core 함수 하나 `DefAssetNames.For(GameDefs, arenas) → (D, 파일)` 목록(WP7, D 가 없으면 기본 — v0.6.2)이고 시작 검사 ③ 의 교차 확인(1.4 끝)이 같은 함수를 쓴다 | 시험 `tests/…/Assets/` (WP0 작성, ② 는 통합 때 기본으로) |
| 13.5-11 | 아래 "추가(경고)" 의 구별 경고(기획 13.5-11) | Data |
| 13.5-12 | 성질 표의 키 = 타일셋이 이름 붙인 번호(`floor`·`borders`·`gates`·`pieces`, 번호 ≥ 0 — `ArenaLayout.ValidateTileset`), 성질·`sol`·`use` 글자가 8글자(`.wLWmxMX`, 6.11 — v0.5) 안, `sol_known_diffs`·`design_diffs` 원소의 메가타일이 성질 표에 있음, `why` 가 비면 오류, **v0.6: `marks` 의 번호가 이름 붙인 번호 안**, **v0.8 짝 표**(`mirror` — 6.11 "거울판"): 이름이 `pieces` 안·한 번만, 짝은 **최종** 성질이 전치(위-오른 ↔ 아래-왼)로 같고 `self` 는 자기 전치와 같음(v0.8.1: v0.8 의 "보정 칸도 짝에서 전치 자리" 는 없앰), **v0.8.1 자리 짝**: 둘레 `edge_nw` ↔ `edge_ne`·`edge_sw` ↔ `edge_se`·`corner_w` ↔ `corner_e`(`corner_n`·`corner_s` 는 자기 전치), 문 변 nw ↔ ne·sw ↔ se 의 `open`·`end_a`·`end_b` 가 전치로 같음, 맵 `mirror: true` 인데 짝 표에 없는 조각을 씀 | Map(WP1b `MapDataReader`, 글자 WP1c, `marks` WP1d, 짝 표 WP1f) — 늘 돈다 |
| 13.5-13 | 큰 피해 패턴·부품의 예고(6.5 판정, `rules.enemies.telegraph_required`): 예고 필수인데 없거나 `min_time` 보다 짧으면 **오류**, 무거운 근접 단일 대상의 타격 프레임까지 시간이 `heavy_melee_windup` 미만이면 **오류** | Data + 부품(WP4 매개변수 검사) |
| 13.5-14 | 정의 `scale` 이 0.65~1.6 밖, 또는 정의 `scale` × 받을 수 있는 가장 큰 등급 배율 > `rules.ranks.max_scale` 이면 경고(넘는 쪽은 생성 때 잘림, 6.5) | Data |
| 13.5-15 (v0.5) | 스테이지마다 주인공 빛 반경(= 시야 반경 — 정의 값과 그 스테이지 `light_change` 가 넣는 `hero_light.radius` 값 모두)이 **380 이상(기획 10.5-3 하한 — v0.5.1, 원거리 적이 없는 스테이지에도) 화면 가로 반폭(`ViewSize.X / 2` = 480) 이하**이고, **그 스테이지에 나오는 적**(구간 가중치·이벤트·소환·분열·호위 — v0.6.1: `StageReach.Enemies`, 아래 "나오는 적 계산") 중 `kite` 를 가진 적의 `range_band` 최대 + `rules.sight.edge` + `rules.sight.ranged_margin`(40 + 20 = 60) **이상**. 어기면 오류: `stage_catacombs: 빛 반경 360 < burning_archer 거리 유지 최대 340 + 60`. 주인공 빛(`hero_light`)이 없거나 반경 0 이하면 같은 오류다 — 그래서 `VisibilityField.Enabled` 가 거짓인 것은 맵 없는 시험 월드뿐이다(4.11) | Data(380·반폭) + WP4 검사기(`kite` 매개변수를 읽음) |
| 13.5-16 (v0.5.1) | **원본 유래 값의 출처 주석**(오류)(사용자 결정 B 의 조건, 1.4): JSON 주석은 로더가 버리므로 원문 문자열로 본다. `Data/tilesets/*.json` 의 `props`·`sol_known_diffs`, `Data/sprites/*.json` 의 `frame_time`·`event_frame`, 가져오기 표 `Table/tilesets.json` 의 `overlay` 가 있는 줄마다, 그 줄 끝 또는 그 줄을 담은 객체·배열을 여는 줄 끝에 `// 출처: 원본 확인` 또는 `// 출처: DevilutionX 참고` 가 있다(줄 단위 + 괄호 깊이 추적) | 시험 `Data/SourceCommentTests`(기본 분류, WP0a 작성 — 실제 `Data/`·가져오기 표 원문만 읽음) |
| 13.5-18 (v0.5.3, v0.7 고침) | **중간 보스 리듬**(경고, 기획 13.5-18 v0.8): 같은 액트의 정예 방끼리, 마지막 정예 방과 보스 방 사이에 일반 방이 1개 미만이면 경고, 정예 방들의 t 시작끼리 2:00 미만이면 경고(v0.6 의 "이벤트 시각끼리 2:00" 을 방으로). 정예 방이 쓰는 생성 구간 — v0.7.1: t 시작 하나가 아니라 **정예 방 전체 `[t 시작, to)` 에 걸치는 모든 구간** — 의 `max_alive`·`per_second` 중 하나라도 정예 방 바로 앞 구간보다 크면 경고(같으면 통과 — 액트 2 의 3:00 구간). 같은 액트의 중간 보스 둘(`rooms[].miniboss`)이 같은 그림 묶음(`sprites`)을 쓰면 경고. 자원 없이 늘 돈다 | WP4 검사기(`rooms`·`PartDef.At` 를 읽음 — 13.5-15 와 같은 자리) |
| 13.5-19 (v0.6) | **안쪽 방**(기획 13.5-19): 맵의 `open_center` 안 메가타일은 바닥 또는 `marks.pillar` 이고 기둥 ≤ 2(오류), `open_center` 가 맵 안·테두리 밖(오류). `marks.closed_door`·`arch` 번호를 놓은 놓기에 `decor_why` 가 없으면 경고. 방 기준점(`room_*`)마다 맵 안·그 칸 걷기 막힘 없음(오류). **v0.6.1 하위 칸 검사**(오류, 반경·격자 없이 성질만): ① **갇힌 바닥** — 걷기 막힘 없는 맵 안 하위 칸을 주인공 시작 칸에서 4이웃으로 채워 못 닿는 칸(띠·막힌 방), ② **하위 1칸 틈** — 걷기 막힘 없는 칸인데 좌우 둘 다 또는 위아래 둘 다 걷기 막힘(맵 밖 포함): 벽 맞물림 구멍·벽 두 겹 사이 띠·아치 틈(6.11 "벽 맞물림"). 칸 목록을 메가타일로 묶어 한 줄씩. `over_border` 의 막힘 줄어듦은 `Expand` 가 낸다(6.11). WP2 의 13.5-4(80px)·13.5-5(반경 14 격자)는 그 위의 정밀 검사로 남는다. **v0.6.3 방 영역**(오류): `room_*` 기준점마다 `room_areas` 하나, 키는 `room_*` 기준점, 맵 안·서로 겹치지 않음, 기준점이 영역 안, 방 바닥에서 영역 밖으로 이어지는 걷기 칸이 있음(문 0 = 오류, 6.11). **방 보급**(v0.6.3, 정의 검사 — 스테이지 + 맵): `room_supply` 의 `anchor` 가 그 스테이지 맵의 `room_*` 이고 `RoomFloor.OneDoor`, `pickup` 이 있는 획득물(오류). 자원 없이 늘 돈다 | Map(`ArenaLayout.ValidateRooms`, WP1d-구현 — 부르는 곳 `MapDataReader.Read`), 방 보급 검사 WP7 |
| 13.5-20 (v0.6) | **자원 묶음**(오류, 기획 13.5-20): `requires` 에 `hfvoice` 없음(원본 우선순위 전용, 1.4). 참조 덮기 — 스테이지가 내는 적(**`StageReach.Enemies`** — 구간·이벤트·보스·`summon`·분열·호위, v0.6.1), 직업의 시작 무기·특성 패시브가 가리키는 정의의 `requires` ⊆ 가리키는 쪽의 `requires`. 스테이지 음악(`music.<스테이지>`)은 `requires` 가 스테이지 것 밖이면 `fallback` 이 있어야 함. `rules.hero.class` 의 직업은 `requires` 가 빔(기본 직업). 음악 `fallback` 은 `requires` 가 비고 다시 `fallback` 이 없음. **v0.6.1 해금 사슬 → v0.7 다음 액트 사슬**: `requires` 가 빈 액트의 `next_act` 가 `requires` 가 비지 않은 액트를 가리키면 오류(6.12), `rooms[].miniboss`·`boss` 도 참조 덮기에 든다(`StageReach` 가 셈). 맵에는 `requires` 가 없다 — 맵 그림 묶음과 스테이지의 대조는 13.5-10 묶음판(v0.7: 맵 그림 → 그 맵을 `room_maps` 에 둔 스테이지들) | Data (WP7) + 13.5-10 시험(WP0) |
| 13.5-21 (v0.6) | **직업**(오류, 기획 13.5-21): 시작 무기 ≥ 1 이고 모두 그 직업이 고를 수 있음(`classes` 가 비었거나 그 직업 포함), 특성(v0.6.1: `traits` 의 패시브)은 `trait: true`·`max_level` 1·진화의 짝 패시브가 아니고 그 패시브의 `classes` 가 비었거나 그 직업 포함, **직업 `radius` ≤ 14**(v0.6.1 — 13.5-5 길 검사의 주인공 등급. 미리보기로 키우려면 길 검사 등급을 같이 올린다, 기획 13.5-21), `stats` 키 ∈ `StatKey`(형이 보장), 그림 묶음에 `idle`·`walk`·`cast_fire`·`cast_lightning`·`cast_magic`·`die` 클립, 시작 무기 중 `cast_clip: attack` 이 있으면 `attack` 클립, 시전 클립 `event_frame` ≤ 프레임 수(13.5-7 과 같은 자리), 직업 ID 마다 소리 연결 `hero.<id>` 가 있음(경고 — WP8 검사기) | Data (WP7), 소리 WP8 |
| 13.5-22 (v0.6.1) | **원작 방 DUN 칸 배열을 옮기지 않음**(오류, 기획 13.5-22, 사용자 결정 B): 원본이 있을 때 표 `audit_dun` 의 DUN 과 `Data/maps/*.json` 을 펼친 메가 격자에 4×4 창이 같은 곳(바닥·0 을 하나로 보고, 그 창의 바닥 아닌 칸 ≥ 8)이 있으면 실패. DUN 은 메모리에서만 읽음. **v0.7.1**: 맵이 본뜬 원작 방(맵 JSON 첫머리 주석 `// 본뜬 원작 방: bonecha1.dun, …`)은 모두 그 타일셋 항목의 `audit_dun` 에 있어야 한다 — 시험 `Data/SourceCommentTests` 가 원문으로 대조(자원 없이 늘 돎, WP0) | 도구 `audit`(WP0) — 원본이 없으면 건너뜀 표시 |
| 13.5-23 (v0.7) | **방 구조**(오류, 기획 13.5-23 — 자원 없이 늘 돈다). ① **순번 표**(Data, WP7): 일반 방 6~8개, 보스 방은 마지막 하나뿐, 1번 방은 일반 방, 일반 방 생존(`to − from`) 1:00~2:00, t 범위가 0:00 부터 빈틈·겹침 없이(보스 방은 `from` 만), 정예 방마다 `miniboss` 가 있고 그 적이 `rank: miniboss`, 보스 방 `boss` 가 `rank: boss` 이고 `boss` 이벤트의 적과 같음. `slice: true` 면 "일반 방 6~8"·"보스 방"·"빈틈" 셋을 끈다. v0.7.1: `number` 가 1부터 커지기만 함, `slice` 가 아니면 배열 자리 + 1 과 같음. ② **방 맵 풀**(Data, WP7): 순번 표에 있는 종류마다 맵 ≥ 1, 풀의 맵마다 `room_kinds` 가 그 종류를 담음, 보스 맵 하나. ③ **맵 쪽**(`ArenaLayout.ValidateRooms`, WP1e — `room_kinds` 가 빈 목록이 아닌 맵): 문 넷 이름·변·입구 넷·`center`·**입구가 같은 ID 문 앞**(v0.8.1 — `inside` 에서 가장 가까운 문 틈 칸이 같은 ID 문, 6.11 "방 맵"), 작은 방(N ≤ 36) 문 하나인 방 바닥 없음·진입점 150px 안 둘레 아닌 막힌 칸 없음(6.11 "방 맵"). 풀의 맵마다 13.5-4~6·19 는 그대로. **v0.7.1 문 틈 닿음**(Nav/Validation, WP2 — 반경 격자가 필요해 `ValidateRooms` 가 아님): 문 넷마다 나가는 자리 칸(`GateZones.Cells`) 중 하나 이상에 주인공 시작에서 반경 14 내비로 닿음(13.5-5 와 같은 "닿는 내비 칸 중심이 그 칸 마름모에서 14px 안"). 아니면 `문 틈에 못 닿음 gate_ne` — 문 틈 옆 끝 조각·`over_border` 벽이 틈을 좁히면 사람 판이 문 단계에서 멈춘다(봇은 `DOOR_SKIP` 으로 덮여 드러나지 않음). ④ **문 보상**(Data, WP7): 가중 > 0 인 보상 ≥ 2(출구 문 둘을 채움), `first_room` 있음, `rules.rooms.fallback_reward` 있음, 참조(물체 획득물 `kind: reward`·아이콘·축복 패시브) 존재, 축복 패시브는 `blessing: true` 이고 `trait` 아님. `rules.rooms` 의 `gates` 이름이 문 넷 안(v0.7 의 "`reward_anchor` 가 `center`" 는 v0.8 에 키와 함께 없앰). ⑤ **이벤트**(WP4 검사기, 13.5-18 자리): 모든 이벤트의 `At` 이 어느 방 범위 안이고 그 방 생존 안(정예 방은 `elite_spawn_cap` 안, 보스 방 제한 없음), `room_supply` 의 기준점이 그 방(보스 방) 맵의 `room_*`·`OneDoor`, `miniboss`·`boss` 이벤트가 그 정예·보스 방 t 시작에 있음. **v0.7.1**: 정예 방 범위의 `miniboss` 이벤트가 **정확히 하나**이고 그 `enemy` == `rooms[].miniboss`(다르면 정예 방 생존이 끝나지 않음 — 3.6), 보스 방 범위의 `boss` 이벤트도 정확히 하나. 이벤트가 가리키는 기준점(`toward`·`anchor`)이 그 t 가 든 방 종류 풀의 **모든 맵**에 있음, 점 빛 `at` 은 방 맵이 둘 이상인 액트에서 기준점 ID 만이고 그 기준점이 액트의 모든 방 맵에 있음(6.12). 들어온 문 바꾸기(v0.8.1 시작 문 기준 바꾸기 — v0.7.1 은 서로 바꾸기, 들어올 수 있는 문마다·v0.8.1: 거울판을 쓰는 맵이 그 방 풀에 있으면 거울판 이름으로도)를 했을 때 **바꾼 뒤 이벤트의 시작 문(`miniboss`·`boss`·`at_entry`) = 들어온 문이면 오류**(v0.8.1 — 바꾸는 규칙의 구현도 이 검사가 잡는다), 행진의 시작 문 = 목표 문이면 **오류**, 같은 방 시계·같은 문에 두 이벤트가 겹치면 **경고**. **v0.8**(기획 v0.8.1 13.5-23): ① 에 `floor` 규칙(모두 적거나 모두 안 적음, ≥ 1, 줄지 않음), ③ 을 **거울판에도**(`mirror: true` 맵은 `MapMirror.Of` 결과로 ③·13.5-4~6·19 를 한 번 더 — 문 틈 닿음 포함), ④ 에서 `reward_anchor` 조건을 빼고 "시작 칸 수 + `stairs` × (액트 수 − 1) ≤ 6"·`rules.level_up.temper` 가 `fillers` 안·성소 보상의 `pick_max_level` ≥ 1 을 더함, ⑤ 의 점 빛 조건에 "거울판이 있는 액트", **⑥ 방 이벤트 자리**(`RoomEvents.CheckPlacement`, WP1f 구현·WP4 등록 — 3.6 결정 5): 강적·포위가 방 시계 0:20 전 **오류**, 2번 방부터의 일반 방에 끝 0:45 안 절정 없음 **경고** | Data (WP7) + Map(WP1e·WP1f 거울판) + Nav(WP2, 문 틈 닿음) + WP4 검사기 |
| 소리 | 소리 연결 표의 대상(무기·투사체·이펙트·그림 묶음·적·획득물·스테이지·**직업**(v0.6)·**보상**(v0.7))과 소리 ID 가 있음, 분류가 `rules.audio` 에 있음, 소리 파일 ≥ 1 | Audio (WP8) |
| 추가(오류, v0.5) | 스테이지가 있는데 `rules.sight` 가 없음(6.10) | Data (WP7) |
| 추가(오류) | `extends` 순환·없는 부모, `abstract` 정의를 생성에 씀, 부품 `(종류, type)` 미등록, 부품 매개변수 검사, `*_patch` 의 없는 id, 클립·이벤트 프레임 범위, 큰 피해 패턴의 예고 필수(13.5-13), 참조 쪽 덮어쓰기는 색조·크기만, 부품 슬롯 > 12, 여러 틱 동작이 `actions` 마지막이 아님, `rules` 하위 키 중복, atlas 그림에 `sheet`·`color_key`·`slack` 속성, 쓰는 클립(`special`·`cast_*`)이 그림 묶음에 없음, `tilesets`·`maps` 에 `extends`·`*_add`·`*_patch` | Data + 부품 |
| 추가(경고) | flow 유닛 반경 > 26, 명시한 `radius` 가 `round(base_radius × scale)` 와 2 넘게 다름, 크기 범위(13.5-14), 사거리가 몸 반경 합 + `melee_reach_over_contact` 를 넘는 단일 대상 근접(6.5), **생성 가능한 적인데 어느 스테이지에서도 나오지 않음**, **같은 스테이지에 나오는 두 적이 같은 그림 묶음 + 같은 변종이고 색조 거리(RGB 유클리드) < 60, 크기 차 < 0.15, 알파·발광도 같음**, 소리 연결이 없는 그림 묶음을 쓰는 생성 가능한 적(소리 없는 적, WP8), **13.5-17 `include_bosses: true` 인 오라·함성·범위 버프**(v0.5.1 — MVP 데이터에는 없어야 함, 결정 C. WP4 매개변수 검사) | Data + 부품 |

- 13.5-15 의 "`kite` 를 가진 적" 은 등급과 상관없다 — v0.5.3 에 스테이지 1 블랙애시(`kite` 최대 320 + 60 = 380 ≤ 420)가 든다. 13.5-13 예고 검사는 새 중간 보스의 불비(18, 예고 0.8s)·진흙 강타(반경 75 피해 26, 0.6s)도 본다.
- 구별 경고는 변종이 다르면 구별되는 것으로 본다(원작 TRN 은 색이 크게 다르다 — 기획 8.2). 기준값(60·0.15)은 설계 제안이고 최종 판정은 `--preview enemies` 캡처를 사람이 본다.
- v0.2 에서 "그림이 있을 때만" 이던 기하 검사(13.5-4~6)는 v0.3 에서 **늘 돈다**(충돌을 저장소 데이터로 만들므로, 4.2). 기획 13.5-4 의 "다각형이 없으면 건너뜀" 은 필요 없어졌다. `[해석]`
- (v0.6) **갇힌 바닥 칸**의 기준을 "반경 14 로 설 수 있는 칸" 이 아니라 "**주인공 몸이 걸칠 수 있는 바닥 칸**" 으로 둔 이유: 벽 두 겹 사이 하위 1칸 띠는 폭이 약 28.6px 라 띠 가운데 clearance 가 14 를 겨우 넘거나 못 넘는다 —
  "clearance ≥ 14 인데 못 닿는 칸" 만 보면 격자 표본 위치에 따라 띠를 놓칠 수 있다. 바닥 칸(그림이 바닥인 칸)을 기준으로 보면 띠는 늘 걸리고, 벽에 붙은 보통 바닥 칸은 몸이 걸쳐 통과한다(벽 옆 칸 중심의 벽까지 거리 ≈ 14.3px).
  시험(WP2, 합성 맵): 둘레 벽 바로 옆 메가에 안쪽 벽 줄을 둔 배치 → 갇힌 칸 오류, 같은 벽을 `over_border` 로 둘레에 겹친 배치와 하위 3칸 띄운 배치 → 통과(10.1 `Nav`).
- (v0.6) 13.5-20 의 참조 덮기는 "기본 내용이 묶음 내용을 가리키지 못한다" 보다 넓다 — 묶음 정의끼리도 `hellfire` 스테이지가 `hfmonk` 적을 내면 오류다. 그래서 `ContentAvailability` 는 스테이지·직업 자신의 `requires` 만 보면 된다(6.17).
- (v0.6.1) **나오는 적 계산 `StageReach`** — "스테이지에서 나올 수 있는 적" 을 함수 하나로 둔다. 13.5-15·13.5-20·"안 나오는 적" 경고·`TextureStore`·`SoundBank`·시작 검사 ③ 교차 확인이 모두 이것을 부른다(같은 닫힘 계산을 여러 벌 짜다 엇갈리는 일 막기 — 소환으로만 나오는 헬파이어 적을 놓치면 R30 이 다시 생긴다).

```csharp
// Core Data/Registry/StageReach.cs (계약 WP1d-형, 구현·시험 WP1d-구현, 그 뒤 소유 WP4)
public static class StageReach
{
    public static IReadOnlyList<EnemyDef> Enemies(StageDef stage, GameDefs defs);   // ID 서수 순, 결정적
}
// Defs/PartDef.cs: PartDef 에 `IReadOnlyList<DefRef<EnemyDef>> ReferencedEnemies { get; init; } = []` — 그 부품 매개변수 안의 모든 DefRef<EnemyDef>.
//   로더(WP7)가 매개변수의 DefRef 를 채우는 연결 단계에서 같이 모은다(매개변수 형을 몰라도 됨 — 부품을 만드는 묶음은 코드를 더하지 않는다).
```

  계산: 시작 집합 = 구간 `weights` 의 적 + 이벤트 부품(`PartKind.StageEvent`)의 `ReferencedEnemies`(보스·중간 보스·행진·고리·호위 대장 등) + (v0.7) 순번 표의 `miniboss`·`boss`. 그 뒤 고정점까지 — 집합의 각 적이 가진 모든 부품(행동·패턴·페이즈·훅·죽음 훅)의 `ReferencedEnemies`(`summon`·`split_on_death`·호위 등)를 더한다.
  시험(WP1d-구현, 합성 정의): 소환으로만 나오는 적·분열체의 분열체·페이즈 패턴의 소환이 들어감, 고리가 있어도 끝남, 같은 입력 같은 순서.

- (v0.6.1) **하위 칸 검사(13.5-19 ①②)와 13.5-5 의 관계**: ①② 는 반경·격자 없이 성질 글자만 보므로 WP1d-구현이 `ArenaLayout` 에 넣고 WP0a 의 실제 맵이 WP2 를 기다리지 않고 통과를 확인한다(v0.6 의 "시제품 길 검사를 다시 돌림" 대신).
  ① 은 4이웃이라 하위 1칸 띠도 끝이 막혔으면 잡고, 띠 끝이 열린 경우는 ② 가 잡는다. 13.5-5 의 반경 14 갇힌 칸·13.5-4 의 80px 는 WP2 가 실제 충돌 도형으로 다시 본다(정밀 검사).

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
    { "id": "mus_catacombs", "file": "d1/sound/music/dlvlb.wav" },
    // (헬파이어 스테이지 단계 예, v0.6) 묶음이 없으면 fallback 곡
    { "id": "mus_crypt", "file": "d1/sound/music/dlvlf.wav", "requires": ["hfmusic"], "fallback": "mus_cathedral" }
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
    "hero":         { "sorcerer": { "hurt": "snd_hero_hurt", "death": "snd_hero_death" } },     // v0.6: 키 = 직업 ID(6.16)
    "ui":           { "menu_move": "snd_titlemov", "menu_select": "snd_titlslct", "level_up": "snd_magic", "chest_open": "snd_chest", "elite_pack": "snd_magic1",
                      "door_open": "snd_dooropen", "potion_drink": "snd_invpot" },   // v0.7: 출구 문 열림(sfx\items\dooropen.wav — 원작 문 소리, 기획 12.7). v0.9(WP1g): room_clear 는 걸지 않는다(기획 12.7 v0.8 — 방 정리는 소리 없이), belt_potion → potion_drink
    "rewards":      { "reward_tome": { "collect": "snd_invbook", "open": "snd_readbook" }, "reward_chest": { "collect": "snd_chest" },
                      "reward_potion": { "collect": "snd_invpot" }, "shrine_enchanted": { "collect": "snd_magic" }, "shrine_fascinating": { "collect": "snd_magic", "open": "snd_magic" },
                      "shrine_gloomy": { "collect": "snd_magic" } },   // v0.7: 키 = 보상 ID(6.18). open = 보상 창이 열릴 때. v0.8: 성소 셋(reward_shrine 없앰), ui.belt_potion = 허리띠 저절로 마심(CueKind.BeltPotion)
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
- `music[]`: `file`, `loop`(기본 true). 기획 12.7 의 곡 목록. v0.6: `requires`(6.17)·`fallback`(음악 ID — `requires` 묶음이 없으면 대신 트는 곡, 기획 19.4: 지하실 → `dlvla`, 둥지 → `dlvlc`).
  음악은 그림과 달리 묶음이 없어도 그 스테이지를 끄지 않는다 — 스테이지의 `requires` 에 `hfmusic` 을 넣지 않고 음악 정의에만 둔다. 고르기는 게임 쪽(WP8 `MusicPlayer`)이라 시뮬레이션과 무관하다.
- **주인공 소리**(v0.6, 기획 4.5·12.7): `hero` 의 키는 **직업 ID** 다(v0.5 는 객체 하나). `AudioSystem` 이 `World.HeroClass.Id` 로 고른다. 소리 정의는 직업 단계에 `snd_<직업>_hurt`·`_death`
  (전사 파일 철자 `warior69`·`wario69b`·`warior71` 섞임, 수도사 `monk69`·`69b`·`71` — `72` 없음, 기획 4.5. 번호는 들어 보고 확정 — 기획 열린 질문 29).
- 연결 키의 대상은 그 종류의 정의 ID 다. `sprite_sets` 의 소리를 적 정의가 덮어쓰려면 `enemies.<id>.{attack,hit,death,special}`. 보스·중간 보스(정의 `rank`)가 낸 몬스터 소리는 분류를 `boss` 로 올린다(기획 12.7 "보스·중간 보스 소리").
  v0.5.3: 새 중간 보스는 그림 묶음이 같아 소리가 저절로 따라온다. 다만 `special` 을 처음 쓰는 묶음(`set_skelbow` — 블랙애시 해골 일으키기, `set_acid` — 데스스핏 산성 분출)은 `sprite_sets.<묶음>.special` 한 줄과 그 WAV(`monsters\skelbow\sklbws*.wav`·`monsters\acid\acids*.wav`)를 가져오기 소리 표에 더한다. 중간 보스에는 등장 대사(`voice`)가 없다(기획 12.7).
- 파일 이름은 가져오기 출력(1.4: 원작 경로를 소문자로 `d1/sound/` 아래에 그대로)이다. 13.5-10 시험이 표와 맞춘다.
- (v0.7, 기획 11.4·12.7) 문 보상 소리: 계약 `SoundBindingsDef.Rewards`(보상 ID → `RewardSoundsDef(Collect, Open)`), 신호 `CueKind.RewardCollected`(Subject = `RewardDef`)·`DoorsOpened`·`RoomCleared`(3.6). 13.5-10 귀속: `rewards.<id>` 줄 → 그 보상(묶음이 없으면 기본).
  새 WAV 는 `sfx\items\dooropen.wav`·`invbook.wav`·`readbook.wav` 셋(가져오기 소리 표, WP0). `chest`·`magic`·`invpot` 은 이미 있다.
- 소리 길이는 정의에 적지 않고 가져오기 출력 `d1/sound/index.json`(Core `Assets/SoundIndex`: 파일마다 표본 수·표본율·채널)에서 읽는다. 무음에서도 읽는다(7.9 고르기).
- 정의 형(`SoundDef`, `MusicDef`, `SoundBindingsDef`, `AudioRulesDef`)은 Core `Defs/`(데이터 모양, 계약 WP1b)이고, 검사기·고르기 규칙은 Core `Audio/`(WP8), 재생은 게임 `Audio/`(WP8). `Sim/` 은 어느 것도 쓰지 않는다.

### 6.16 직업 `hero_classes` (v0.6, 사용자 결정 K, 기획 4.5·13.1)

MVP 는 마법사 하나지만 주인공을 **처음부터 직업 정의로** 만든다. 직업을 더할 때 늘어나는 것은 정의 파일·그림 묶음·가져오기 표 줄·소리 연결 줄뿐이고, 계약(형·월드·소리 키·저장 모양)은 그대로다.

```json
// Data/heroes/sorcerer.json (WP3) — 직업마다 파일 하나
{ "hero_classes": [
    { "id": "sorcerer", "name": "마법사", "description": "마법 무기의 쿨다운이 조금 짧다",
      "sprites": "set_sorcerer", "radius": 14,
      "stats": {},                                         // rules.hero.stats 를 키별로 덮어씀(기획 4.1 기본값 = 마법사 값이라 비어 있음)
      "start_weapons": ["magic_missile"],
      "traits": ["trait_arcane_affinity"] } ] }                // v0.6.1: 특성 = 패시브 정의 ID(아래 passives.json)

// Data/passives.json 에 더함 (WP3) — 특성 패시브. 아이콘은 직업 단계(WP9)에 WP5 가 단다(MVP 는 HUD 에 특성 아이콘을 그리지 않음)
{ "passives": [
    { "id": "trait_arcane_affinity", "name": "마력 친화",          // 설명 문구는 패시브 카드처럼 per_level 에서 만든다(쿨다운 감소 +5%)
      "trait": true, "max_level": 1, "per_level": { "cooldown_pct": 0.05 }, "classes": ["sorcerer"] } ] }

// (직업 단계 예) Data/heroes/monk.json
{ "hero_classes": [
    { "id": "monk", "name": "수도사", "description": "…", "sprites": "set_monk", "radius": 14,
      "stats": { "max_hp": 115, "move_speed": 100 }, "start_weapons": ["staff_sweep"],
      "traits": ["trait_discipline"],                     // 패시브 trait_discipline: per_level { "regen": 0.2 }, trait, classes ["monk"]
      "requires": ["hfmonk"] } ] }
```

```csharp
// Core Defs/HeroDefs.cs (계약, WP1d)
public sealed record HeroClassDef : IDef
{
    public required string Id { get; init; }
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public IconDef? Icon { get; init; }                                   // 직업 고르기 카드(직업 단계). 없으면 서기 그림 첫 프레임
    public required DefRef<SpriteSetDef> Sprites { get; init; }           // idle·walk·cast_*·die(+ attack) — 13.5-21
    public float Radius { get; init; } = 14f;
    public IReadOnlyDictionary<StatKey, float> Stats { get; init; } = new Dictionary<StatKey, float>();   // 덮어쓰기
    public required IReadOnlyList<DefRef<WeaponDef>> StartWeapons { get; init; }
    public IReadOnlyList<DefRef<PassiveDef>> Traits { get; init; } = [];   // v0.6.1: 특성 = 레벨 없고 슬롯을 안 쓰는 패시브(v0.6 의 HeroTraitDef 를 대신함)
    public IReadOnlyList<ContentBundle> Requires { get; init; } = [];
}
// WeaponDefs.cs (v0.6.1, 기본값 있는 칸 더하기)
//   PassiveDef:   bool Trait(`trait`, 기본 false — 참이면 레벨업·상자 후보에 안 나오고 직업 특성으로만 붙음),
//                 IReadOnlyList<DefRef<HeroClassDef>> Classes(`classes`, 기본 빈 = 모든 직업 — 직업 전용 패시브)
//   EvolutionDef: IReadOnlyList<DefRef<HeroClassDef>> Classes(기본 빈 = 모두 — 직업 전용 진화)
// RulesDefs.cs: HeroRulesDef 에서 Sprites·Radius·StartWeapons 를 빼고 `required DefRef<HeroClassDef> Class` 를 더함. Stats(기본값)·InvulnAfterHit·CrowdSlow 는 그대로
// World.cs:     public World(GameDefs defs, ulong seed, StageDef? stage = null, HeroClassDef? heroClass = null)
//               public HeroClassDef? HeroClass { get; }   // heroClass ?? defs.Rules.Hero?.Class.Def. 맵·규칙 없는 시험 월드는 null
//               Hero.Radius = HeroClass?.Radius ?? 14
```

- **고른 직업이 들어오는 길**: 앱(WP6·WP7)이 `--hero` → 저장의 마지막 직업 → `rules.hero.class` 순으로 정하고, `ContentAvailability`(6.17)로 꺼진 직업이면 기본 직업으로 바꾼다(`--hero` 로 꺼진 직업을 고르면 시작하지 않음 — 9절).
  그 정의를 `World` 생성자에 넘긴다. 시뮬레이션은 묶음을 모른다. 같은 씨앗·스테이지·직업이면 같은 결과다(3.4 — 첫 로그 줄과 `RESULT` 에 `hero=<id>`).
- **주인공 만들기**(WP3 `HeroSetup`): 능력치 = `rules.hero.stats` → `HeroClass.Stats` 로 키별 덮어쓰기 → 특성 패시브(`Traits`, 1레벨 `per_level`·`on_acquire`)를 더함 → 고른 패시브(4.1 적용 규칙). 시작 무기 = `HeroClass.StartWeapons`(Lv1).
  그림 = `HeroClass.Sprites`(시전 클립은 `cast_clip` 으로, 5.3). 특성은 슬롯을 쓰지 않고 레벨이 없다 — HUD 지속 효과 줄에 아이콘 하나(직업 단계, WP6. 아이콘 그림은 WP5 가 그때 단다 — 원작 exe PNG 나 주문 아이콘).
- **특성을 패시브로 둔 이유**(v0.6.1, 기획 리뷰): 직업 단계에서 특성 둘 이상·`on_acquire`·직업 전용 패시브·진화를 원해도 직업 형을 고치지 않는다. 능력치 키로 안 되는 특성(피격 반격·이동 중 막기 같은 효과)이 필요해지면
  그것은 **패시브 일반의 확장**(패시브 효과 부품)이라 직업 계약 밖이다(R31 남는 위험). `trait` 패시브는 레벨업·상자·`--passives` 후보에서 빠진다(WP3, 6.9).
- **시전 길이·발동 프레임**은 직업 정의가 아니라 그 직업 그림 묶음의 시전 클립(`frames`·`event_frame`, 출처 주석)이다(6.3) — 그림 맞춤 값이고 규칙이 쓰지 않는다(기획 4.5 "시전 그림은 보여 주기만"). `[해석]`
- **무기는 직업 공통 목록**이다. 직업 전용 무기만 무기 정의의 `classes` 로 묶는다(6.8 — v0.6.1: 패시브·진화도 같은 `classes`). 레벨업 후보·진화·상자는 `classes` 와 6.17 의 묶음 조건으로 거른다(WP3).
- **소리**는 직업 정의에 두지 않는다 — `sound_bindings.hero.<직업 ID>`(6.15).
- **직업 고르기 화면·근접 무기 `melee_arc`·전사·도적·수도사 데이터**는 직업 단계(WP9, 11.1)다. MVP 의 WP1d·WP3 은 형·로딩·주인공 만들기·`sorcerer` 한 파일까지.

### 6.17 자원 묶음 `requires`·`ContentAvailability` (v0.6, 사용자 결정 H, 기획 19.4)

```csharp
// Core Defs/ContentBundle.cs (계약, WP1d)
public enum ContentBundle : byte { Hellfire, Hfmonk, Hfmusic, Hfvoice }     // JSON: "hellfire" "hfmonk" "hfmusic" "hfvoice"
public static class ContentBundles { public static string MpqName(ContentBundle b); }   // "hellfire.mpq" …
// 정의에 더하는 칸(모두 기본 빈 목록): EnemyDef.Requires, StageDef.Requires, HeroClassDef.Requires, WeaponDef.Requires, MusicDef.Requires (+ MusicDef.Fallback: DefRef<MusicDef>?)

// Core Assets/ContentAvailability.cs (계약·구현 WP1d)
public sealed class ContentAvailability
{
    public static ContentAvailability All { get; }                         // 시험·자원 없이 도는 경로
    public static ContentAvailability From(AssetManifest manifest);        // skipped_bundles 에 없는 묶음 = 있음
    public bool Has(ContentBundle b);
    public IReadOnlyList<ContentBundle> Missing(IReadOnlyList<ContentBundle> requires);   // 빈 목록 = 켜짐
    public IReadOnlyList<ContentBundle> MissingFor(StageDef s);            // = Missing(s.Requires)  (13.5-20 이 적·무기를 덮어 줌)
    public IReadOnlyList<ContentBundle> MissingFor(HeroClassDef c);
    public DisabledContent Disabled(GameDefs defs);                        // v0.6.1(v0.6 의 DisabledInDefs 를 대신함): 꺼진 스테이지·직업과 그 묶음
    public ManifestSkippedBundle? Skipped(ContentBundle b);                // v0.6.1: 건너뛴 이유(없음·--skip-bundles·열 수 없음·판 다름) — 타이틀 한 줄의 앞말. All 이면 늘 null
}
public sealed record DisabledContent(IReadOnlyList<StageDef> Stages, IReadOnlyList<HeroClassDef> Heroes, IReadOnlyList<ContentBundle> Bundles)
{
    public bool Any => Stages.Count > 0 || Heroes.Count > 0;              // 타이틀 한 줄·ASSETS disabled= 는 이것이 참일 때만(음악만 대체되면 거짓)
}
```

- **꺼짐 단위는 스테이지·직업**이다(기획 19.4 "그 정의를 쓰는 스테이지·직업이 통째로 꺼진다"). 적·무기·음악은 스테이지·직업을 거쳐서만 쓰이고 13.5-20 이 `requires` 를 위로 덮게 하므로, 스테이지·직업 자신의 `requires` 만 보면 된다.
  음악은 꺼지지 않고 `fallback` 으로 바뀐다(6.15). 그래서 `hfmusic` 만 없으면 `Disabled(defs).Any` 가 거짓이고 타이틀 한 줄도 없다 — 로그 `AUDIO music=… fallback=…` 만 남는다(v0.6.1).
- **시뮬레이션은 묶음을 모른다**: `Sim/` 은 `ContentAvailability` 를 쓰지 않는다(소스 문자열 시험, 10.1 `Sim`). 레벨업 후보도 PC 의 묶음이 아니라 **정의**로 거른다 — 무기 `requires ⊆ 스테이지 requires ∪ 직업 requires`(6.8).
  그래서 같은 씨앗·스테이지·직업이면 헬파이어 MPQ 유무와 상관없이 결과가 같고(기획 19.4 "결정성"), 꺼진 스테이지·직업은 애초에 시작되지 않는다.
- **쓰는 곳**: 시작 검사 ③ "꺼짐"(WP7, 1.4 끝), 타이틀 한 줄(WP6, 8.1 — v0.7: 스테이지 카드는 스테이지 선택 화면과 함께 없앰), 직업 고르기 카드(WP9), 음악 대체(WP8), `--hero`·`--stage`·`--room` 이 꺼진 것을 고르면 종료 코드 5 와 이유(WP7, 9절).
  (v0.7) 판 중간에 꺼진 액트로 넘어가는 일은 13.5-20 다음 액트 사슬 규칙이 막는다(기본 액트 → 기본 액트만).
  시험·`--validate-data` 는 `All` 을 쓴다(자원 없이 모든 정의를 검사).
- 카드 문구의 MPQ 이름은 `ContentBundles.MpqName` 이 만든다: `헬파이어 자원 필요(hellfire.mpq)`(기획 12.5). 타이틀 한 줄의 앞말은 `Skipped(b).Reason` 으로 "없음" / "읽지 못함" 을 가른다(1.4 끝, v0.6.1).

### 6.18 문 보상 `rewards` (v0.7, 사용자 결정 R, 기획 11.4·13.1)

```json
// Data/rewards.json (WP3) — v0.8(기획 v0.8.1 11.4 다시 짬): 문 보상은 레벨업 창에 없는 것을 준다
{ "rewards": [
    { "id": "reward_tome",   "name": "마법서",         "kind": "weapon_cards",  "object": "obj_tome",   "icon": { "sprite": "obj_book_icon" },
      "cards": { "owned_weapon": 100, "new_weapon": 100 }, "skip": { "heal": 10 } },                       // 건너뛰기 = 체력 10(레벨업 건너뛰기와 같음)
    { "id": "reward_chest",  "name": "보물 상자",      "kind": "weapon_bundle", "object": "obj_chest",  "icon": { "sprite": "obj_chest_icon" },
      "bundle": { "weapon_levels": 1, "belt_potions": 1, "gold": [30, 60] } },                              // 고르지 않음: 무작위 무기 +1(없으면 단련) + 허리띠 1 + 금화 × gold_scale
    { "id": "reward_potion", "name": "완전 회복 물약", "kind": "belt_potions",  "object": "obj_potion", "icon": { "sprite": "obj_potion_icon" }, "belt_potions": 2 },
    { "id": "shrine_enchanted",   "name": "마법에 걸린 성소", "kind": "shrine", "object": "obj_shrine",       "icon": { "sprite": "obj_shrine_icon" },
      "shrine": { "all_weapons": 1, "random_weapon": -1 } },                                                // 모두 +1, 그중 무작위 하나는 대신 −1(Lv1 은 안 맞음, 진화 빼고)
    { "id": "shrine_fascinating", "name": "매혹의 성소",     "kind": "shrine", "object": "obj_shrine_red",   "icon": { "sprite": "obj_shrine_red_icon" },
      "shrine": { "pick_weapon": 2, "pick_max_level": 7, "stats": { "max_hp_pct": -0.10 } } },              // 고른 무기 +2(창 — OfferSource.Shrine), 대가 최대 체력 −10%
    { "id": "shrine_gloomy",      "name": "음산한 성소",     "kind": "shrine", "object": "obj_shrine_dark",  "icon": { "sprite": "obj_shrine_dark_icon" },
      "shrine": { "stats": { "armor": 2, "damage_pct": -0.05 } } } ] }                                      // 방어 +2, 대가 피해 −5%. 어두운 그림(색조 0.6)은 그림 정의 쪽(WP5)
// pickups.json 에 더함: obj_tome·obj_chest·obj_potion·obj_shrine·obj_shrine_red·obj_shrine_dark = { "kind": "reward", "draw": { "sprite": … }, "magnet": false }
//   (원작 물체 그림 검증 전까지 exe PNG·코드 그림이 정식 대체 — 기획 v0.8.1 11.4·열린 질문 32)
```

```csharp
// Core Defs/RewardDefs.cs (계약 WP1e → v0.8 WP1f)
public enum RewardKind : byte { WeaponCards, WeaponBundle, BeltPotions, Shrine }   // JSON weapon_cards / weapon_bundle / belt_potions / shrine (v0.7 의 blessing_cards·level_up·full_heal 없앰)
// v0.9(WP1g — 6.19·11.1 #1): BeltPotions 없앰, HealNextRoom(heal_next_room — 회복 문)·Merchant(merchant) 더함, Object null 허용(그 둘만)·Merchant·Drop 칸, BeltPotions 칸 → Bundle.Potions
public sealed record RewardDef : IDef
{
    public required string Id { get; init; }   public string Name { get; init; } = "";
    public required RewardKind Kind { get; init; }
    public required DefRef<PickupDef> Object { get; init; }      // 방 안 물체(kind: reward 획득물)
    public IconDef? Icon { get; init; }                           // 문 위 아이콘(48px)·HUD 방 줄
    public RewardCardsDef? Cards { get; init; }                   // weapon_cards: 후보 가중(owned_weapon·new_weapon)
    public RewardSkipDef? Skip { get; init; }                     // 창의 건너뛰기 = 회복(heal, 체력 값). v0.8: gold 없앰
    public RewardBundleDef? Bundle { get; init; }                 // weapon_bundle
    public int BeltPotions { get; init; }                         // belt_potions: 허리띠에 넣는 수(차 있으면 그 자리에서 마심)
    public ShrineDef? Shrine { get; init; }                       // shrine
    public IReadOnlyList<ContentBundle> Requires { get; init; } = [];
}
public sealed record RewardBundleDef { public int WeaponLevels { get; init; } = 1; public int BeltPotions { get; init; } public int[]? Gold { get; init; } }   // Gold [최소, 최대] × 액트 gold_scale
public sealed record ShrineDef
{
    public int AllWeapons { get; init; }        // 진화 아닌 가진 무기 모두 +n(만렙은 안 오름)
    public int RandomWeapon { get; init; }      // 0 이 아니면 그중 무작위 하나는 AllWeapons 대신 이 값(Lv1 무기는 고르지 않음, 방 난수 Drop)
    public int PickWeapon { get; init; }        // 0 이 아니면 고른 무기 하나 +n — 창(LevelUpOffer.Source = Shrine), 후보 = PickMaxLevel 이하 진화 아닌 무기
    public int PickMaxLevel { get; init; }
    public IReadOnlyDictionary<StatKey, float> Stats { get; init; } = new Dictionary<StatKey, float>();   // 판 끝까지(짐 Mods)
}
// GameDefs.Rewards(DefTable<RewardDef>), Kinds 에 "rewards". 종류마다 필요한 칸이 없거나 다른 종류의 칸이 있으면 로딩 오류(13.5-23 ④)
```

- **정예 상자 보상**(기획 `reward_elite`)은 정의를 두지 않는다 — 중간 보스·보스가 떨군 정예 상자(드롭 표 `dt_miniboss`, 6.9)를 `RoomFlow` 가 `GatesExit` 로 묶는다(3.6). 같은 일을 두 곳에서 정하지 않게. `[해석]`
  v0.8: 정예·보스 방에는 여기에 **덤 보상**(그 방으로 들어온 문의 보상 — `RoomState.Reward`)이 더해진다. 둘 다 보상 자리 줄에 놓인다(3.6 "보상 자리").
- **성소는 종류마다 한 판에 한 번**: 받은 성소 ID 는 짐 `Shrines` 에 쌓이고 `RewardAvailable` 이 거른다(같은 판에 두 번 오면 음산한 성소 방어가 돌 피부와 합쳐 7 을 넘는다 — 기획 6절).
- **`GatesExit` 를 다는 곳 둘**(v0.7.1): 문 보상 물체는 `SpawnPickup` 이 `PickupSpawn.Reward != null` 이면 만들 때 단다(WP3), 정예 상자는 `Reward` 시작 때 `RoomFlow` 가 단다(이번 틱에 생긴 상자 포함). 그 밖의 획득물은 달지 않는다.
- **받는 길**(WP3, `Pickups` 단계 — v0.8): 보상 물체를 주우면 `Kind` 마다 — `WeaponCards` → 보상 대기 +1(`IProgression.PendingRewards`, 창은 레벨업 오버레이 재사용 — 8.1, `Source = Tome`),
  `WeaponBundle` → 진화 아니고 만렙 아닌 가진 무기 중 무작위 하나 +`WeaponLevels`(방 난수 `Drop`, 없으면 `단련`) + 허리띠 `BeltPotions`(단계 2 부터 데이터) + 금화 `Gold × gold_scale`,
  `BeltPotions` → 허리띠에 넣고 넘치는 만큼은 그 자리에서 마심(체력 전부), `Shrine` → `AllWeapons`·`RandomWeapon` 바로 적용 + `Stats` 를 짐 `Mods` 에 + ID 를 `Shrines` 에, `PickWeapon` 이 있으면 보상 대기 +1(`Source = Shrine`, 고른 무기 +`PickWeapon`). 신호 `RewardCollected`.
  v0.7 의 `LevelUp`("큰 영혼석" — 레벨업 1회)·`FullHeal`(줍는 순간 전부 회복)·`BlessingCards` 는 없앴다(기획 v0.8.1 11.4 — 레벨업 창과 같거나, 위험이 지난 뒤에 오던 것).
- **보상 물체는 상태와 상관없이 늘 집힌다**(v0.7.1, 물약 포함 — 획득물 체력 물약의 "체력이 가득이면 안 집힘"(기획 11.1)은 `kind: reward` 에 쓰지 않는다. 쓰면 체력이 가득일 때 문이 영영 안 열린다).
  **0장 보상**(v0.8): 줍는 순간 그 보상이 줄 카드가 0장이면(문을 연 뒤 레벨업으로 무기 칸이 차고 모두 만렙, 매혹의 성소에 Lv7 이하 무기가 없음) 대기를 만들지 않고 **`단련`**(`rules.level_up.temper`)으로 바로 처리한다 — 거르기(`RewardAvailable`)는 문을 열 때 한 번뿐이라(3.6) 그 사이 바뀐 상태를 여기서 받는다.
  (v0.7.1 은 그 보상의 `skip` 금화 25 — 금화는 MVP 에 쓸 곳이 없다, 기획 v0.8.1 26.) 그래서 보상 창은 늘 카드 ≥ 1장으로 열리고 자동 조작이 0장 창에서 멈추지 않는다(9절).
- **거르기** `RewardAvailable`(WP3, 문을 열 때 한 번 — 3.6): 마법서 = 줄 무기 카드 ≥ 1, 물약 = 허리띠가 차지 않음, 성소 = 짐 `Shrines` 에 없음 + (`RandomWeapon` 이면 진화 아닌 무기 ≥ 2, `PickWeapon` 이면 `PickMaxLevel` 이하 진화 아닌 무기 ≥ 1), 보물 상자 = 늘 참. v0.7 의 `skip_when_hp_at_least` 는 없앴다.
- **보상 창 계약**(WP1e 형, WP3 구현 — `Sim/Contracts/Progression.cs`):

```csharp
public enum OfferSource : byte { LevelUp, Tome, Shrine }      // v0.8: Shrine = 매혹의 성소의 무기 고르기 창(축복 창 아님)
public sealed record LevelUpOffer(int Level, IReadOnlyList<LevelUpCard> Cards, int RerollsLeft, OfferSource Source = OfferSource.LevelUp);
// IProgression 에 더함
int PendingRewards { get; }
LevelUpOffer RewardOffer();            // 대기 중인 마법서·매혹의 성소 창(Source = Tome·Shrine). 카드 = 무기만
void ChooseReward(int index); void RerollReward(); void SkipReward();   // 재추첨은 레벨업과 같은 수를 나눠 씀(기획 4.4). 건너뛰기 = RewardDef.Skip(체력)
bool RewardAvailable(RewardDef reward); // 문 열기 거르기(3.6, 위 "거르기")
int PicksLeft { get; }                 // v0.8: 빌드에 남은 칸 레벨(열린 칸마다 (최대 − 지금), 빈 칸은 최대) — 0 이면 만렙. ROOM 로그 picks_left
// ChoiceKind 에 Reward. EndTick 의 대기 순서 = LevelUp → Chest → Reward
```

- 대기 순서를 레벨업 → 상자 → 보상으로 둔 이유: 끌어당긴 보석이 만든 레벨업과 정예 상자 진화를 먼저 끝내야 마법서 카드가 지금 칸 상태로 만들어진다. 출구 문은 셋이 다 비어야 열린다(3.6 `Reward` 줄).
- **허리띠**(v0.8, 기획 v0.8.1 11.4 — 계약 `Hero.Belt`(int, 공통 조각 — HUD 가 읽음)·`rules.hero.belt`, 구현 WP3): 체력이 최대 × `threshold` 아래로 내려가는 순간 하나를 저절로 마신다(전부 회복, `cooldown` 초에 한 번 — 버튼 없음). 짐 `Belt` 로 방·액트 사이에 이어진다.
  (v0.9) 허리띠는 기획 v0.9.1(사용자 결정 V9)에서 **물약 칸**으로 바뀌었다 — 저절로 마심·`Hero.Belt`·`rules.hero.belt` 를 없애고 `Drink` 명령·서서히 회복으로(6.19 "물약 칸", WP1g). 이 줄은 v0.8 기록이다.
- 소리는 정의에 두지 않고 `sound_bindings.rewards.<id>`(6.15). 문 위 표시·방 안 물체 그리기는 7.11.

### 6.19 장비·상인·물약 칸 `items`·`affixes`·`merchants`·`rules.equipment`·`rules.hero.potions` (v0.9, 기획 v0.9.1 11.4~11.7·12.1·12.8·33·34)

기획서 v0.9(33절, 사용자 결정 V1~V5)와 v0.9.1(34절, V6~V12)이 판 안의 **장비·금화·상인·물약 칸**을 더했다. 모두 판 수명 상태라 짐(`HeroCarry`)·계단 저장(`RunSnapshot`)에 들어가고(3.6 "판 수명 상태는 짐·`RunState` 에만"),
고르기·굴림은 판 씨앗에서 나온 난수 흐름으로 정해 같은 씨앗·같은 명령이면 같은 물건·같은 값이다. 계약은 WP1g(11.1)가 형과 순수 로직까지 한 번에 낸다.

**정의 — 기본 아이템·접사**(`Data/items/bases.json`, `Data/items/affixes.json` — 기획 11.6 표 그대로)

```csharp
// Core Defs/ItemDefs.cs (v0.9 — 계약 WP1g)
public enum EquipKind : byte { Head, Body, Shield, Staff, Ring }                 // JSON head / body / shield / staff / ring
public enum EquipSlot : byte { Head, Body, Shield, Staff, Ring1, Ring2 }          // 칸 여섯(기획 11.6 — 목걸이 없음). HUD 장비 줄 순서와 같다
public enum ArmorWeight : byte { None, Light, Medium, Heavy }                     // JSON - / light / medium / heavy (v0.9.1 V6 — 지팡이·반지는 None)
public sealed record ItemBaseDef : IDef
{
    public required string Id { get; init; }   public string Name { get; init; } = "";   // 원작 영어 이름 그대로(기획 11.6 "이름")
    public required EquipKind Kind { get; init; }
    public ArmorWeight Weight { get; init; }                                      // 방어구만. 이동 감소는 rules.equipment.weight_move 에서(정의에 이동을 따로 적지 않음)
    public IReadOnlyDictionary<StatKey, float> Stats { get; init; } = new Dictionary<StatKey, float>();   // 기본 능력치 — 굴리지 않음
    public required int Price { get; init; }
    public IReadOnlyList<int> Acts { get; init; } = [1, 2];                        // 나오는 액트 번호
    public bool AlwaysMagic { get; init; }                                        // 반지(기획: 늘 마법)
    public SpriteRef? Icon { get; init; }                                         // 인벤토리 그림 objcurs 프레임(가져오기 전 코드 그림 — 6.3 대체 규칙)
    public SpriteRef? Floor { get; init; }                                        // 바닥 그림 items\*(장비 드롭은 E — 지금은 읽기만)
    public IReadOnlyList<ContentBundle> Requires { get; init; } = [];
}
public sealed record AffixDef : IDef
{
    public required string Id { get; init; }   public required string Name { get; init; }
    public required bool Prefix { get; init; }                                    // 거짓 = 접미사
    public required IReadOnlyList<EquipKind> Kinds { get; init; }                 // 붙는 칸 종류
    public required StatKey Stat { get; init; }
    public required float Min { get; init; }   public required float Max { get; init; }   public float Step { get; init; } = 1;   // v0.9.1 V7: 범위 굴림 — Min == Max 면 고정
    public required int Price { get; init; }                                      // 범위 가운데의 값
    public int Stage { get; init; } = 1;                                          // 1 = 두 액트, 2 = 액트 2 만
}
// GameDefs.ItemBases·Affixes(DefTable), Kinds 에 "items"·"affixes"
```

**장비 인스턴스**(굴린 결과 — 정의 ID + 굴린 값. 짐·저장·상인 카드가 같은 형을 쓴다)

```csharp
// Core Sim/Items/EquipItem.cs (v0.9 — 계약 WP1g)
public readonly record struct RolledAffix(string Id, float Value);
public sealed record EquipItem(string BaseId, RolledAffix? Prefix, RolledAffix? Suffix, int Price)   // Price = 굴림 반영 값(사는 순간 그대로)
{
    public bool Magic => Prefix is not null || Suffix is not null;
}
public static class ItemRoller   // 순수 함수 — WP1g 가 본문과 시험까지
{
    // 마법 확률(액트별) → 접사 수(하나 1-two_affix / 둘) → 접사(그 칸·그 액트 단계 중 균등, 하나면 접두사·접미사 반반) → 값(단계 균등) → 값 = Price + Σ affix.Price × (0.75 + 0.5 × 위치), 5 단위 반올림
    public static EquipItem Roll(ItemBaseDef b, int act, EquipmentRulesDef rules, GameDefs defs, ref DetRng rng);
    public static EquipItem RollMagic(ItemBaseDef b, int act, EquipmentRulesDef rules, GameDefs defs, ref DetRng rng, IReadOnlySet<string> avoidAffixes);  // 반지 둘 = 서로 다른 접사
}
```

- **난수 수 고정**: `Roll` 은 마법 여부·접사 수·접사 고르기·값 굴림마다 **늘 같은 수**의 난수를 뽑는다(쓰지 않는 갈래도 뽑아 버림 — 3.6 "뽑기 단계마다 난수 개수 고정" 과 같은 규칙). 정의 표에 접사를 더해도 다른 카드의 굴림이 밀리지 않게, 카드마다 흐름을 따로 낸다(아래 "상인 물건").
- **능력치 합산**: 장비 능력치는 4.1 능력치 키 합산의 한 출처(`StatSources.Equipment`)다 — 절댓값 키 더하기, `_pct` 같은 키끼리 더해 한 번 곱하기. **방어 상한 15**(v0.9.1 — 기획 v0.9.2 열린 질문 52 답, v0.9 는 10)·**무게 이동 하한 −15%**·쿨다운 상한 60% 는 모든 출처를 합친 **뒤** 자른다(`rules.equipment.armor_cap`·`weight_move_floor`).
  무게 이동은 끼운 방어구마다 `rules.equipment.weight_move[칸 종류][무게]` 를 `move_pct` 에 더한다. 최대 체력이 바뀌는 순간 지금 체력 = `max(1, 지금 + 오른 만큼)` 을 새 최대에서 자른다(기획 11.6).
- **칸끼리 막지 않는다**(v0.9.1 — 기획 v0.9.2 열린 질문 53 답): 끼우기는 그 칸만 바꾸고 다른 칸을 비우지 않는다 — 지팡이(`Staff`)와 방패(`Shield`)를 함께 든다(원작 양손 규칙 없음). 그래서 상인 카드에 "빠지는 것" 줄이 없고, `ShopCard` 에 빠지는 칸 자리를 두지 않는다.

**규칙 데이터**(`Data/rules/equipment.json` → `rules.equipment`, `rules.hero.potions`, `rules.rooms.rewards`)

```jsonc
// rules.equipment (v0.9 — 기획 11.6·11.7 첫 값)
{ "magic_chance": [0.35, 0.60],                // 액트 1·2
  "two_affix_chance": 0.25,                    // v0.9.1 V7
  "price_roll": [0.75, 0.5], "price_round": 5,
  "armor_cap": 15,                             // v0.9.1 — 기획 v0.9.2(10 에서 올림)
  "weight_move": { "body": { "light": 0, "medium": -0.05, "heavy": -0.10 }, "head": { "light": 0, "medium": -0.02, "heavy": -0.04 }, "shield": { "light": 0, "medium": -0.02, "heavy": -0.04 } },
  "weight_move_floor": -0.15,
  "reroll_price": 30, "reroll_step": 30 }
// rules.hero.potions (v0.9.1 V9 — v0.8 의 rules.hero.belt 를 바꿈: slots·threshold·cooldown 없앰)
{ "slots": 2, "slots_max": 5, "heal_pct": 0.40, "durations": [3.0, 2.0, 1.2], "bot_threshold": 0.35 }
// rules.rooms.rewards (v0.9 — 문 보상 뽑기의 값. 6.18)
{ "heal_skip_at_least": 0.75, "heal_guarantee_below": 0.40, "heal_low_hp": 0.50, "heal_low_hp_weight": 30,
  "merchant_first_room": 3, "merchant_per_act": 2, "merchant_guarantee_room": [4, 5], "merchant_met_other": 1.5,
  "healer_boost": 1.5, "healer_boost_below": 0.50 }
```

**상인 정의**(`Data/merchants/<id>.json` — 그리스월드·아드리아·페핀)

```csharp
// Core Defs/MerchantDefs.cs (v0.9 — 계약 WP1g)
public enum MerchantStock : byte { Equipment, Potions }                      // 그리스월드·아드리아 = Equipment, 페핀 = Potions(v0.9.1 V11)
public sealed record MerchantDef : IDef
{
    public required string Id { get; init; }   public required string Name { get; init; }   // 그리스월드·아드리아·페핀
    public required MerchantStock Stock { get; init; }
    public IReadOnlyList<EquipKind> Kinds { get; init; } = [];                  // Equipment: 카드 칸 종류 목록(그리스월드 head·body·shield, 아드리아 staff·ring·ring)
    public IReadOnlyList<PotionUpgradeDef> Upgrades { get; init; } = [];        // Potions: 물약 주머니·빠른 물약·물약 채우기
    public bool Rerollable { get; init; } = true;                               // 페핀 거짓
    public required DefRef<SpriteSetDef> Sprite { get; init; }                  // 원작 towners 그림(서기 반복) — 문 위 아이콘·창 머리·방 안 상인
    public string? GreetSound { get; init; }                                    // 소리 연결 키(6.15)
}
public enum PotionUpgrade : byte { Slots, Speed, Refill }
public sealed record PotionUpgradeDef { public required PotionUpgrade Kind { get; init; } public IReadOnlyList<int> Prices { get; init; } = []; public int PerSlot { get; init; } }  // Refill = 빈 칸 × PerSlot
```

**상인 방 상태와 구매 명령**

```csharp
// Core Sim/Contracts/ShopState.cs (v0.9 — 계약 WP1g, 방 수명 → RoomState.Shop)
public enum ShopCardKind : byte { Equip, Potion }
public sealed record ShopCard(ShopCardKind Kind, EquipItem? Item, PotionUpgrade Upgrade, int Price, bool Bought);
public sealed class ShopState
{
    public required string MerchantId { get; init; }
    public List<ShopCard> Cards { get; } = [];
    public int Rerolls { get; set; }                       // 이 방에서 누른 다시 굴리기 수(값 = reroll_price + reroll_step × Rerolls)
    public bool Open { get; set; }                         // 창이 열려 있음(시간 멈춤 — 레벨업 창과 같은 멈춤)
    public bool Visited { get; set; }                      // 한 번이라도 닫았음 = 보상 받음(문이 열림)
    public int RingPending { get; set; } = -1;             // 반지 둘이 찬 채 반지를 고른 카드 번호 — 창이 "바꿀 반지" 둘로 바뀜
}
// IProgression 옆에 IShop(WP3): ShopState? Shop { get; }  void Buy(int card); void Reroll(); void Leave(); void ReplaceRing(int ring);
// OfferSource 에 Merchant 를 더하지 않는다 — 상인 창은 LevelUpOffer 가 아니라 ShopState 를 그린다(화면 모양만 같다, 기획 12.8)
```

- **물건을 정하는 때**: 상인 방(다음 방 보상이 `merchant`)에 **들어오는 틱**(`RunSession.Advance` ⑤ 새 `World` 직후, 정리 전)에 `ShopState` 를 만든다. 난수는 **상인 전용 흐름** `DetRng.For(seed, act, room, "shop", rerolls, card)` — 방 난수(`Drop`·`Spawn`)를 하나도 소비하지 않아 상인 방이든 아니든 적·드롭이 같다.
  다시 굴리기는 같은 식에 `rerolls` 만 바꿔 **사지 않은 카드만** 새로 굴린다(산 카드는 자리를 지킴). 같은 씨앗·같은 누른 수면 같은 결과.
- **명령**: `CommandButtons` 에 **`Drink = 1 << 10`**(아래 물약) 하나만 더한다. 상인 카드는 셋(기획 v0.9.1 — v0.9 의 아드리아 넷째 칸 허리띠 물약이 페핀으로 감)이라 `Choice1~3` 으로 된다.
  창이 열려 있는 동안 `Choice1~3` = `Buy`(또는 `RingPending` 이면 `ReplaceRing`), `Reroll` = 다시 굴리기, `Skip` = 떠나기. 선택 입력 잠금 0.4s 는 레벨업 창과 같은 자리(`rules.level_up.input_lock`)에서. 모두 판 명령이라 재생·자동 조작·사람이 같은 길이다.
- **열고 닫기**: 상인(정리 뒤 보상 자리에 놓인 물체 — `RewardKind.Merchant`)에 닿으면 `Open = true`. `Leave` = `Open = false; Visited = true`(→ 보상 받음 신호 = 문이 열림, 3.6 "집어야 문이 열린다"). 문 단계에 다시 닿으면 다시 연다.
- **금화**: 지갑은 짐 `HeroCarry.Gold`(판 안 쓸 금화)이고, `RunStats.Gold`(얻은 금화 — 결과 화면·누적)는 그대로 늘기만 한다. 쓴 금화 `RunStats.GoldSpent`(새 멤버). `Buy` 는 `Gold ≥ Price` 일 때만, 모자라면 아무 일 없음 + `CueKind.ShopDenied`.

**물약 칸과 서서히 회복**(v0.9.1 V9 — v0.8 의 허리띠 저절로 마심을 바꿈)

- 상태: 짐 `HeroCarry.Potions`(가진 수)·`PotionSlotsLevel`(0 → 칸 = `slots` + 단계, 상한 `slots_max`)·`PotionSpeedLevel`(`durations` 의 번호). 마시는 중 회복은 `Hero.Heal`(`HealOverTime` — 남은 틱·틱당 양) 하나.
- **마시기**: `CommandButtons.Drink` 가 들어온 틱에 (창 없음 ∧ 회복 중 아님 ∧ `Potions > 0` ∧ 체력 < 최대)이면 `Potions -= 1`, `Heal = (ticks = round(durations[lv] × 60), total = max_hp × heal_pct)`. 아니면 `CueKind.PotionDenied`.
  회복은 `HeroSystem` 차례에서 틱마다 `total / ticks`(마지막 틱에 나머지 — 합이 정확히 `total`), 최대 체력에서 자른다. 피해로 끊기지 않는다. 판 시간이 멈추면(창·넘김) 같이 멈춘다(틱이 돌지 않으므로 따로 할 일 없음).
- **줍기**: `healing_potion`(`PickupDef.Effect = potion_stack`)은 `Potions < 칸` 일 때만 집힌다(가득이면 그대로 남음 — 6.9 집기 조건). 마력 폭주는 지금처럼 닿는 순간.
- **계단 저장**: 계단 문을 나가는 틱(`Suspend` 의 ①)에 남은 회복을 바로 채우고 `Heal` 을 비운다 — 짐에 회복 중 상태를 넣지 않는다(기획 11.5).
- **없앰**: `rules.hero.belt`·`HeroCarry.Belt`·`RewardDef.BeltPotions`·`RewardBundleDef.BeltPotions`·`CueKind.BeltPotion`, WP3 의 30% 저절로 마심 경로. 새 `CueKind.PotionDrink`·`PotionDenied`·`ShopOpen`·`ShopBuy`·`ShopDenied`.
- **자동 조작**(WP4): 체력 < `bot_threshold` ∧ 회복 중 아님 ∧ `Potions > 0` → 그 틱 명령에 `Drink`. 상인 창 규칙은 기획 12.8(살 수 있는 카드 중 최대 체력·방어 차이 최대 하나, 페핀은 채우기 → 주머니 → 빠른 물약).

**회복 문**(v0.9 V1 — `RewardKind.HealNextRoom`)

- 방 안 물체가 없다(`RewardDef.Object` = null 허용, 이 종류만). 그 문으로 나가는 틱에 `RunState.HealOnEnter = true`, `RunSession.Advance` 가 새 방을 만든 뒤 **들어옴 단계 첫 틱**(넘김 어둠이 걷힌 틱 — `RoomFlow` 가 `Phase == Enter` 로 바꾼 첫 틱)에 체력 = 최대, `HealOnEnter = false`, `CueKind.HealOnEnter`.
- 정예·보스 방 앞 문의 덤이면 그 방에 들어올 때 회복이고, 그 방 정리 뒤 보상 자리에는 정예 상자만 놓인다. 계단 문은 보상이 없으므로 `Suspend` 때 `HealOnEnter` 는 늘 거짓이다(스냅숏에 넣지 않고, `Suspend` 가 거짓임을 단언).
- 거르기·보장(기획 11.4 "뽑기")은 **문이 열리는 틱의 상태**의 함수다(체력 비율·이 액트 상인 방 수·바로 앞 방이 상인 방·만난 상인) — 그 틱은 결정적이라 같은 씨앗·같은 명령이면 같은 문이다.
  보장 순서: 회복 문 보장(체력 < 40% → 첫 문 `gate_nw`, 거울판이면 짝) → 상인 보장(그 액트에 상인 문이 안 보였고 액트 1 4번·액트 2 5번 방으로 가는 문이면 둘째 문) → 나머지 가중 뽑기. 보장으로 정한 문도 뽑기 난수 수는 그대로 소비한다(난수 수 고정).

**보상 물체 둘레 아이콘**(v0.9.1 V8)

- `RewardDef.Drop`(`DefRef<PickupDef>?`): 물체에 닿으면 열리고 이 물체가 떨어지는 보상(보물 상자 → `pk_book` = `items\fbook`). 두 단계 상태 `RewardPickup.Stage`(`Closed` → `Dropped`) — 상자에 닿으면 `Dropped`, 책에 닿으면 받음. 책 자리 = 상자 앞 30px(`RewardSpot.Row` 의 당김 규칙).
- 둘레 아이콘 목록은 **표시 전용 순수 함수** `RewardOsd.Icons(RewardDef, World) → IReadOnlyList<OsdIcon>`(WP3 본문, 그리기 WP5). 보물 상자의 올릴 무기는 **상자를 여는 틱**에 방 난수 `Drop` 으로 정해 `RewardPickup.Chosen` 에 둔다 — 책을 집는 틱에 다시 뽑지 않는다(보이는 것과 받는 것이 같게). 마법서 카드는 집는 순간(지금과 같음).

**주인공 겉모습**(v0.9.1 V10)

- `HeroClassDef.Looks`(`IReadOnlyDictionary<string, DefRef<SpriteSetDef>>` — 키 `lt`·`mn` 처럼 무게 글자 + 든 것 글자, 아홉)와 `HeroClassDef.Look`(기본 키 `lt` — 못 가져온 벌의 대신).
- 고르기는 순수 함수 `HeroLook.Key(equip) = 무게(갑옷 칸: 없음·경 → l, 중 → m, 판금 → h) + 든 것(지팡이 → t, 방패만 → u, 없음 → n)`. 지팡이와 방패를 함께 들면 `t`(v0.9.1 — 기획 v0.9.2 열린 질문 53: 함께 든 그림이 원작에 없어 지팡이 벌을 쓰고 방패는 능력치만). 그리기(WP5)가 프레임마다 키로 묶음을 고른다 — 시뮬레이션은 키만 안다(그림 크기·프레임은 그리기 몫, 동작 시간은 묶음마다 같음을 13.5 검사가 확인).
  묶음이 `ContentAvailability` 에 없거나 가져오기에서 빠졌으면 `Look` 으로.

**가져오기 표**(1.4 — WP0. 이름은 모두 기획서가 기억으로 적은 것이라 원본 목록으로 확인)

| 쓰임 | 원본 | 출력 | 비고 |
|---|---|---|---|
| 무기 아이콘 (5.9 v0.9.1) | `ctrlpan\spelicon.cel` | `d1/icons/<무기>.png` 56×56(줄이지 않음) | 프레임 머리 없는 CEL(부록 A.2) — 칸 크기를 표에 적어 읽음. `Table/icons.json` 의 무기 항목 = `cel` + 칸 번호(1부터 — 파이어볼 12·매직 미사일 39·비전 구체 42, DevilutionX 마법 아이콘 표에서 손으로 옮김). **됨**(코드 `9128cc9`) — 옛 "투사체 한 프레임" 항목은 표에서 뺐고 가산 굽기는 하지 않음(아래) |
| 체력 구슬 (12.1 V12) | `ctrlpan\panel8.cel`(구슬 자리·허리띠 칸 자르기), `ctrlpan\p8bulbs.cel`(붉은 액체) | `d1/ui/orb_frame.png`·`orb_fill.png`·`belt_slot.png` | 자를 사각형은 표에 적음 |
| 바닥 아이템 (V8) | `items\fbook`·`fbttle*`·`goldflip` | `d1/items/<이름>.png` + 메타(떨어짐 프레임) | 몬스터 CEL 길과 같은지 확인(열린 질문 32) |
| 인벤토리 그림 (11.6) | `data\inv\objcurs.cel` | 아이템 프레임 표 | 장비 18 + 물약 |
| 상인 (11.7 V5·V11) | `towners\smith\smithn`·`towners\townwmn1\witch`·`towners\healer\healer` | `d1/towners/<id>` | 서기 첫 프레임 = 문 아이콘. 인사 대사 `sfx\towners\bsmith*`·`witch*`·`healer*` |
| 주인공 아홉 벌 (4.0 V10) | `plrgfx\sorceror\s{l,m,h}{n,u,t}\*` 의 `as`·`aw`·`fm`·`lm`·`qm` | 지금 `slt` 와 같은 모양 | 죽기는 `sln` 하나 |

- 확인 전·실패 때는 코드 그림 대체(6.3 대체 규칙 — 단계가 그림 때문에 막히지 않는다). 아이콘이 어둡던 문제(통합 뒤 플레이)의 원인은 `IconBaker` 가 미리 곱한 색을 255 로 두 번 나눈 굽기 버그였다(`9128cc9` 에서 고침 — 불투명 칸도 바탕색 가까이 검게 구워짐). 원작 아이콘으로 바꾸면서 그 버그도 고쳤으므로 대체 아이콘을 가산으로 굽는 일은 두지 않는다(v0.9.1).

---

## 7. 렌더·소리

### 7.1 가상 해상도와 창 (기획 열린 질문 1: 960×540 으로 간다)

- 월드는 **960×540 렌더 타깃**(`WorldRT`)에 1:1 월드 픽셀로 그린다. 창(또는 전체 화면)에 맞출 때:
  1. 정수 배율 k = ceil(min(창W/960, 창H/540)) 로 `WorldRT` 를 점 샘플링(PointClamp)해 960k×540k 중간 타깃에 키운다.
  2. 그것을 선형 샘플링으로 창 안 레터박스 사각형(비율 16:9 유지, 남는 곳 검정)에 줄여 그린다.
  → 정수 배율에서는 그대로 날카롭고, 비정수 배율에서도 번짐이 1px 테두리 정도로 줄어든다(sharp bilinear).
- **그림 배율 s**(v0.8, 사용자 결정 U — 1.4 "AI 확대"): 시작 검사가 manifest `upscale.status` 가 `done` 이면 s = manifest `upscale.output_scale`, 아니면 1 로 정한다(로그 `ASSETS … art_scale=`).
  v0.8.1: 첫 판은 `output_scale` 이 늘 2 다(표 읽기가 2 만 받음 — 1.4). 시작 검사는 메타·`arena.json` 의 `scale` 이 **{1, s} 안이고 s 를 나누는지**(그리는 배율 s ÷ `scale` 이 정수인지) 본다 — 어기면 시작 검사 ① 과 같은 "자원 형식이 낡았다" 안내. 4배 판(s = 4)은 계약 변경(WP1g — 13.20 열린 질문 2). `WorldRT` 는 **(960·s)×(540·s)**(s = 2 면 1920×1080)이고
  월드 → 타깃 변환에 배율 s 를 곱한다. 카메라·`World.ViewRect`·생성 띠는 그대로 960×540 월드 px 이다 — **시뮬레이션은 s 를 모른다**(같은 씨앗이면 s 와 상관없이 `--log-hash` 가 같다, 10.2).
  그림마다 그리는 배율 = s ÷ 메타 `scale`: 확대한 그림(scale 2)은 1:1, 원본 그림(scale 1)은 2배 점 확대. 둘 다 정수라 월드 그리기는 모두 `PointClamp` 하나로 된다. 카메라·개체 위치는 **타깃 픽셀**(= 1/s 월드 px — s = 2 면 월드 반 픽셀)로 내린다(그림 떨림 방지 규칙을 2배 화면 기준으로, 기획 19.6).
  빛 지도·안개 타깃(7.7·7.10)은 월드 px 해상도 그대로 두고 곱하기 그리기에서 선형으로 s 배 늘린다(부드러운 값이라 해상도를 올릴 까닭이 없다). 위 1·2 단계의 960·540 을 960·s·540·s 로 읽는다(창 1920×1080 이면 k = 1).
  9~10층(월드 UI·HUD)은 지금처럼 `PresentRT` 해상도다. 첫 값 "4배 결과를 2배로 줄여 1920×1080" 은 기획 열린 질문 41 이다(화면 배율·성능 — R38). 텍스처는 확대한 그림만큼 4배(큰 방 바닥 약 14MB → 약 58MB, 기획 19.6) — WP5 가 `vram_mb` 로 다시 잰다(R7·R38).
  v0.8.1: 이 어림은 맵 하나 값이다. 액트 단위로 읽는 맵 그림(7.5)에는 `mirror: true` 인 맵의 **거울판 그림 `<art>@m` 이 원판만큼 더해지므로**(거울판 맵마다 ×2) 액트의 맵 그림 합은 (풀의 맵 수 + 거울판 수) × 맵 하나 값으로 본다.
- 최종 결과는 `PresentRT` 에 모은다(월드 + UI). 백버퍼에는 그것을 한 번 늘여 복사한다. 캡처는 `PresentRT` 를 저장한다.
  `PresentRT` 크기는 백버퍼와 따로 정한다: `--window` 값이 있으면 그 크기로 고정(창이 화면 배율·작은 모니터 때문에 줄어도 캡처 크기·배율 k 가 바뀌지 않음), 없으면 창 크기.
- 전체 화면은 테두리 없는 창(`HardwareModeSwitch = false`). F11·Alt+Enter. 개발 옵션 실행에서는 F11 을 받지 않는다(실제 입력을 읽지 않음, 3.2).
- `GraphicsDeviceManager.GraphicsProfile = GraphicsProfile.HiDef` 를 명시한다. `LOGO`(550×3240), 원작 시트 아틀라스(가로 ≤ 4096, 1.4)가 Reach 한도 2048 을 넘는다.
  바닥은 512 조각이라 한도와 상관없다.

### 7.2 카메라

- 카메라 중심 = 보간된 주인공 위치, **외곽 상자**(4.1, 스테이지 1 2560×1408)에 맞춰 자름. **따라가기 지연 없음**: 시뮬레이션의 생성 띠(화면 사각형 바깥 40~200px)와
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
| 6 | 빛·안개 | 빛 지도에 안개(보임·기억·검정)를 합친 뒤 곱하기(7.7·7.10, v0.5) | 곱하기 |
| 7 | 발광 | 투사체, 폭발·맞음 이펙트, 장판 이펙트의 빛 부분, 보석 빛, 정예 발광·번쩍임 덧그림, **예고(원·선)의 테두리를 한 번 더** | 이펙트 정의의 `blend`(기본 보통), 주인공 투사체는 가산 한 겹 더, 적 투사체는 붉은 테(7.5), 예고 테두리는 알파 0.6 의 2px 선 |
| 8 | 화면 효과 | 낮은 체력 붉은 비네트, 광폭화 맥동 | 알파 |
| 9 | 월드 UI | 이름(정예·우두머리), 주인공 발밑 체력 막대, 보스 아래 표시, 피해 숫자 | `PresentRT` 해상도로(글자 선명) |
| 10 | HUD·오버레이 | 8절 | `PresentRT` 해상도 |

- 투사체·폭발을 빛 **뒤에** 그리는 이유: 어둠에 묻히지 않게(빛을 내는 것은 빛 영향을 받지 않음). 원작 마법 그림은 보통 혼합으로도 빛 뒤라 밝게 보인다.
  7층 중 **적 투사체·예고 테두리·주인공 마법·폭발 이펙트는 안개에 가리지 않는다** — 시야 밖에서도 보인다(기획 10.7 "늘 그린다, 안개 위에").
  **적 발광·번쩍임·보석 빛은 그 대상의 안개 알파를 따른다**(시야 밖 적·보석이면 그리지 않음 — 아래, 7.10 표). v0.5.1 에서 문구를 나눴다.
- **안개 숨김**(v0.5, 7.10): 층을 따로 두지 않는다. 1~5·7·9층의 각 대상이 그릴 때 안개 알파(`FogMath`)를 곱하고, 0 이면 그 대상의 모든 그리기를 건너뛴다(그림·그림자·발광·번쩍임·이름·발밑 원).
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
  - (v0.6) 안쪽 방·짧은 벽·창살도 같은 이웃 판정으로 나뉜다(배치 출처를 보지 않음). 둘레에 겹쳐 놓은 방 바깥 벽(`over_border`)은 뒤가 맵 밖이라 뒷둘레 무리, 방 안쪽 벽·창살 기둥은 4층(기획 10.2).
    창살의 걷기만 막는 칸(`w`)도 Solid 라 원작 바닥 판정(A.4)으로는 바닥이 아니다 — 기둥 그림 항목(4층)이고 가림·안개도 벽 칸 규칙을 따른다(시야는 막지 않으므로 그 너머 칸이 보인다).
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
  **v0.8 확대한 묶음의 그림자**(사용자 결정 U — AI 로 확대하면 그림자 가장자리에 계단·지저분한 테가 생김, 기획 19.6): 그림자는 AI 에 넣지 않고 가져오기 도구가 원본 그림자 모양을 **매끈하게 2배**(도구 코드 — 결정적) 해
  몸 아래에 같은 시트로 굽는다(1.4 "AI 확대" ⑥). 그래서 게임은 확대 여부와 상관없이 그림자를 따로 그리지 않는다 — 그리기 경로·그리기 호출이 그대로다. 진하기는 같은 `shadow_alpha`(0.5)라 확대 안 한 그림의 그림자와 같다(기획 19.6 확인 항목 ④).
- **변종(TRN)**: 가져올 때 구운 **파일**이다(`zombie@bluered.png` — 묶음 한 페이지의 변종). 실행 중에 색을 바꾸지 않는다. 이유: 커스텀 셰이더를 쓰지 않으므로(2.2) 실행 중 TRN 은 CPU 로 텍스처를 한 벌 더 만드는 것과 같아
  그림 메모리 이득이 없고, 색인 PNG 해독기·팔레트 데이터를 게임에 따로 둬야 한다. 디스크만 변종 수만큼 늘어난다.
- **그림 메모리**: 칸 그대로의 시트면 변종 하나(좀비 묶음 서기·걷기·공격·죽기)가 25MB 안팎, 스테이지 1 의 변종 12개 + 보스면 300MB 를 넘는다. 프레임을 잘라 묶으면 약 1/3 로 본다(추정, WP0 가 잰다 —
  좀비 묶음 한 페이지가 2048² 안이면 16MB).
  게다가 `TextureStore` 는 **스테이지 시작 때 그 스테이지에서 나올 수 있는 것만** 읽는다: 나오는 적(구간 가중치·이벤트·소환·분열·호위 — v0.6.1: `StageReach.Enemies`, 6.14)의 (그림 묶음 × 변종),
  주인공, 모든 무기·이펙트·투사체·획득물·아이콘, 그 맵의 그림. 스테이지를 바꾸면 쓰지 않는 것은 내린다. 로그 `ASSETS stage=… textures=… vram_mb=…`(R7).
  v0.7: 단위는 **액트**다 — 액트 시작 때(판 시작·계단) 그 액트 **방 맵 풀의 모든 맵 그림**·보상 물체·아이콘까지 읽고, 방 이동 때는 읽지 않는다(0.5s 어두워짐 안에 디스크 읽기가 끼지 않게, 기획 28절 넘긴 것 2). 계단에서 앞 액트 것을 내린다.
  v0.8.1: 풀의 맵이 `mirror: true` 면 거울판 그림 `<art>@m` 도 같이 읽는다(방 이동 때 읽지 않는 규칙 그대로 — 7.1 의 메모리 어림에 들어감).
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
  v0.8: `arena.json` 에 `scale`(1·2, 형식 판 2). 확대한 맵은 바닥 조각이 `512 × scale` 그림 픽셀, 기둥 그림이 `64·scale × 160·scale` 이고 그리는 배율은 7.1 의 s ÷ `scale` — 자리·정렬 키·가림 판정은 모두 월드 px 그대로다.
  벽 기둥은 하나씩 확대하므로 이웃 기둥과 맞닿는 세로 가장자리에 **색** 이음매가 보일 수 있다(1.4 ③ 여백으로 줄임) — `--preview tiles` 로 본다(10.2, R38).
  v0.8.1: 맵 그림의 알파는 원본을 2배 점 확대하므로(1.4 ④) 바닥 조각·기둥의 경계 모양은 원본 2배 그대로다 — 아래 "1px 틈·겹침 없음" 은 확대해도 선다. 이음매는 색만의 문제이고, 번진 색 여백은 이웃 기둥의 질감을 보지 못하므로 여백을 늘려서는 이어지지 않는다.
  보이면 대처는 **벽 줄을 배치째 확대한 뒤 기둥으로 자르기**(같은 그림 한 프레임 규칙을 그 맵 그림에서는 포기 — 아틀라스가 커짐) 또는 **맵 범위에서 기둥 빼기**(바닥 조각만 확대 — 지금 표에는 기둥만 끄는 칸이 없어 `UpscaleTable` 계약 변경, WP1g. 13.20 열린 질문 4).
  `cells` 는 (ty, tx, kind) 순 정렬. 같은 칸에 kind 1 과 2 가 같이 있을 수 있다. 칸의 무리(뒷둘레·앞둘레·안쪽, 7.4)는 파일에 적지 않고 게임이 `ArenaGrid` 로 계산한다.
- **바닥(0층)**: 바닥 칸(성질 `.`·`L`)의 바닥 삼각형 두 조각을 모두 구운 512×512 조각들. 화면에 걸친 조각만 그린다. 한 장으로 구우면 텍스처 한도를 넘는다(기획 10.2).
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
- **맵 밖**: 마름모 바깥은 그리지 않는다(검정). 빛 지도의 어둠이 원작처럼 바깥을 덮는다. v0.5: 안 가 본 칸도 안개로 검정이 된다(7.10).
- **미니맵**: 가져오기가 구운 `minimap.png`(외곽 상자 1/16 — 스테이지 1 은 160×88, 바닥 밝게·막힌 칸 어둡게)를 HUD 가 가 본 칸만 보이게 가려 그린다(8.2, 기획 12.1). 구운 그림은 바뀌지 않는다.
- **낡은 그림**: 그릴 때 `arena.json` 의 `layout_hash` 를 Core 가 같은 배치로 계산한 값과 비교한다. 다르면 시작 검사(1.4 끝)가 이미 막았어야 하므로, 여기서 다르면 예외(버그).
- **`--preview tiles`**: 맵마다 외곽 상자를 줄여 그린 화면 + 하위 칸 성질 겹침(걷기·투사체·시야 막힘을 세 색으로 겹침 — 8글자, 6.11) + 입구·기준점·충돌 도형. 안개는 그리지 않는다. 가져온 그림과 충돌(성질 표)이 맞는지 사람이 본다.
- **하지 않는 것(MVP, 기획 10.2)**: 맵 장식 물체(`objects\`), 문 열림·닫힘, 색 순환(L3·L4), 원작 자동 지도, 원작 빛 표.

### 7.7 조명

- `LightRT`(480×270, 월드의 절반 해상도 — 부드러운 빛, 채움 비용 ¼)를 `주변광 × (1 − 어둠)` 으로 지운다.
- 빛마다 미리 만든 방사형 그라데이션 텍스처(코드로 생성, 256px)를 색 × 세기로 **가산**해 그린다(주인공·점 빛·폭발·지옥불). 깜빡임·맥동은 세기에.
  깜빡임 = `StableHash(seed, lightId, stageTick)`, 맥동 = 스테이지 틱의 사인(3.4). 현재 조명 값(주변광·어둠·점 빛)은 `World.Lighting` 에서 읽는다(`light_change` 가 바꿈).
- `WorldRT` 위에 `LightRT` 를 **곱하기 혼합**(`ColorSourceBlend = DestinationColor, ColorDestinationBlend = Zero`)으로 늘려 그린다.
  → 주변광 곱하기와 어둠 뚫기가 한 번에 된다(기획 10.5 의 두 단계와 같은 결과, 단 빛이 겹쳐도 1 을 넘지 않음). 마름모 바깥도 같이 어두워진다.
- 빛 상한 64: 주인공·점 빛을 먼저, 남는 자리를 짧은 빛(폭발) 중 주인공에게 가까운 순으로.
- **`LightRT` 를 만드는 순서**(v0.5): ① 바탕(`주변광 × (1 − 어둠)`)으로 지움 → ② 빛 가산 → ③ **안개 합치기**(7.10: 보이는 정도로 곱하고 기억 밝기를 더함) → ④ 유닛 최소 밝기(아래) → `WorldRT` 에 곱하기.
  ③ 이 ② 뒤라 폭발·장판 빛도 보이는 정도로 잘린다 — 시야 밖 땅을 밝히지 않는다(기획 10.7). ④ 가 ③ 뒤라 시야 경계·기억 칸에 선 대상도 하한을 받는다. `--reveal` 이면 ③ 을 건너뛴다.
- **혼합 상태 규칙**(v0.5.1): 셰이더가 없으므로 SpriteBatch 기본 효과의 결과는 **텍스처 색 × 틴트**이고, 최댓값 혼합(`BlendFunction.Max`)은 혼합 계수를 무시한다.
  그래서 "그림 모양을 값 g 로 찍기" 를 색 채널로 하면 들어가는 값이 g 가 아니라 **그림 색 × g** 다(어두운 벽·적 픽셀일수록 값이 작아짐). 값은 늘 **알파 채널**에 담는다 —
  틴트 (255,255,255,g) 의 결과 알파는 그림 알파 × g 로 그림 색과 무관하다. 쓰는 혼합 상태는 7.10 표(`FogBlend`)에 모았다.
- **유닛 최소 밝기**(`lighting.unit_min_light` = m > 0, 6.12, v0.5.1 다시 씀): 안개까지 합친 `LightRT`(L) 에서, **그리는**(안개 알파 > 0) 주인공·적·시체(2층)·획득물(3층)(기획 10.5 "유닛 밝기 하한" 의 대상)
  모양 안만 max(L, m) 으로 올린다. 보조 렌더 타깃(`MemRT` 를 다시 씀 — 7.10 ③ 뒤라 비어 있음)으로 네 번 그린다.
  1. 보조 RT 를 (0,0,0,0) 으로 지우고 `LightRT` 를 색만 복사(`CopyRgb`).
  2. 1px 흰 텍스처를 화면 전체에 틴트 (m,m,m,1) 로 `MaxRgb` → 보조 RT 의 색 = max(L, m).
  3. 대상들의 현재 프레임을 절반 크기로, 틴트 (255,255,255, a) 로 `MaxA` → 보조 RT 의 알파 = 그림 알파 × a. a = **안개 알파 × 그림 알파**(`AlphaMul`·등장·사라짐).
  4. 보조 RT 를 `LightRT` 에 `LerpRgbBySrcA`(SourceAlpha / InverseSourceAlpha) → `LightRT` = max(L, m)·a + L·(1 − a).
  → 불투명한 대상 픽셀은 그림 색과 상관없이 빛 ≥ m 이다(색 채널에 찍던 v0.5 방식은 어두운 픽셀에서 하한이 m × 색으로 떨어졌다). 바닥·벽은 원작처럼 어둡다(기획 10.5 의 판독성).
  흐림 띠·0.15s 바뀜 중인 적(a 작음)은 둘레 땅보다 a 만큼만 밝아진다 — v0.5 처럼 m 을 그대로 쓰면 반투명한 적의 실루엣 안 **바닥까지** m 을 받아, 거의 투명한 적 자리에
  둘레보다 2~3배 밝은 바닥 모양이 먼저 드러났다(기획 리뷰). 그림 알파도 같은 이유로 곱한다(`phase_through` 0.3, 등장 연출). 그림자 픽셀(알파 0.5)은 반만 오르지만 색이 검정이라 보이지 않는다.
  적 투사체·발광은 이미 7층(빛 뒤)이라 영향 없다. 시체(최대 150)·획득물(보석 최대 300)도 같은 목록에 넣는다 — 절반 해상도에 작은 그림이라 채움 비용이 작고, 같은 텍스처끼리 모으면 호출도 몇 번이다.
  대가: 유닛 앞에 안쪽 벽 기둥·앞둘레 턱이 겹치면 그 벽 위에 유닛 모양으로 밝은 자리가 생긴다 — 가림 판정과 같은 사각형 검사로 **앞 벽이 겹친 유닛은 건너뛴다**(7.4 발광과 같은 처리).
  `LightRT` 가 절반 해상도라 테두리 1px 정도가 바닥에 번진다(부드러운 테로 허용). 값이 0 이면 이 단계 전체를 건너뛴다.
  시야 밖이라 그리지 않는 대상은 이 목록에도 넣지 않는다(기획 10.5 "하한은 시야 안에서만"). 늘 보이는 중간 보스·보스·닿은 적·점화 중 자폭 적은 a = 1 로 늘 넣는다.
  확인은 10.2 "안개 고정 장면" 의 빛 지도 PNG(대상 모양 안 값 ≥ m)다.

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
| `HeroHurt` / `HeroDied` | WP3 `DamageResolve`(피격 무적이 아닐 때 한 번) / 죽음 처리 | — | `hero.<World.HeroClass.Id>.hurt` / `.death`(v0.6) | hero |
| `PickupCollected` / `PickupDropped` | WP3 `Pickups` / 드롭 생성 | `PickupDef` | `pickups.<id>.collect` / `.drop` | pickup |
| `BossArrived` / `BossPhase` | WP4 `boss`·`miniboss` 이벤트 / `PhaseController`(페이즈 `voice: true`) | `EnemyDef` | `enemies.<id>.voice` | boss_voice |
| `ElitePack` | WP4 정예 무리 등장 | — | `ui.elite_pack` | ui |

- 이미 있는 표시 이벤트에서 나오는 소리: `FxEvent` → `effects.<이펙트 ID>`(무기 맞음·폭발·산 웅덩이·피 분수, 분류 weapon_hit), `CorpseSpawned` → 그 적의 `death`(분류 monster, 같은 종 0.1s 에 한 번).
  화살처럼 맞음 이펙트가 없는 것은 소리도 없다(기획 12.7 "화살은 없음").
- 장판 반복 소리: `AudioSystem` 이 프레임마다 `World.Hazards` 에서 `hazard_loops` 에 연결된 이펙트가 하나라도 살아 있는지 보고, 있으면 반복 소리 하나를 켜 두고 없으면 끈다(기획 12.7 "장판 수와 상관없이 하나").
- 씬 소리: 레벨업 창(`ui.level_up`)·상자 열기(`ui.chest_open`)·메뉴 이동/선택은 오버레이·씬(WP6)이 `IAudioServices.PlayUi` 로 직접 부른다. 음악은 씬이 `PlayMusic("title")`, `PlayScene` 이 `PlayMusic(<스테이지 ID>)`.
  v0.7: `PlayScene` 은 **액트가 바뀔 때만**(판 시작·계단) `PreloadStage(액트)` → `PlayMusic(<액트 ID>)` 를 부른다. 같은 액트의 방 이동에서는 부르지 않아 곡이 이어진다(기획 12.7). `MusicPlayer` 도 지금 곡과 같은 키면 아무것도 안 한다(이중 안전).
  문 열림은 신호 `DoorsOpened` → `ui.door_open`, 보상 줍기·창 열림은 `RewardCollected` → `rewards.<id>.collect`, 보상 창은 오버레이가 `rewards.<id>.open`(6.15).

**고르기 `SoundMixer`**(Core `Audio/`, 순수 로직 — 시험 대상)
- 한 프레임에 들어온 요청을 (분류 우선순위, 생긴 틱, 들어온 순서)로 정렬해 차례로 본다 → 결정적.
- 버리는 규칙(기획 12.7 표, 값은 `rules.audio`): 같은 소리 ID 최소 간격 0.05s, 같은 소리 동시 3(정의 `max_instances`), 몬스터 맞음 전체 0.08s 에 한 번, 몬스터 죽음 동시 4·같은 종 0.1s,
  보석 0.06s, **전체 동시 24 — 자리가 없으면 새 소리를 버린다**(이미 나는 소리는 끊지 않음, 기획 12.7).
- 화면 밖: 그 분류가 `offscreen: false` 이면 `ViewRect` 를 `offscreen_margin`(100px)만큼 넓힌 사각형 밖에서 난 소리를 버린다(보스·중간 보스·이벤트는 늘 냄).
  **안개와 무관**(v0.5, 기획 10.7·12.7): 기준은 화면 사각형뿐이라 화면 안이면 시야 밖(안개 속) 적의 소리도 낸다 — 어둠 속 적의 단서다. `AudioSystem`·`SoundMixer` 는 `World.Visibility` 를 읽지 않는다.
  그래서 "그리지 않는 적의 소리" 가 정상이다. `--reveal` 은 소리 수를 바꾸지 않는다(`AUDIO` 수 같음).
- 좌우: `(x − 화면 가운데 x) / (화면 폭 / 2) × pan(0.5)`, 높낮이: ± `pitch_jitter`. 무작위는 게임 쪽 난수(시뮬레이션 `SimRandom` 과 따로 — 기획 12.7, 15절).
- **자리가 비는 시각은 소리 길이로 센다**: 동시 재생 수(전체 24·같은 소리 3·몬스터 죽음 4)를 세려면 이미 나는 소리가 언제 끝나는지 알아야 한다.
  길이 = `d1/sound/index.json`(1.4, `SoundIndex`)의 표본 수 / 표본율 ÷ 높낮이 배율을 앱 틱으로 올림한 값. 고른 소리는 (끝 앱 틱)을 기록하고, 그 틱이 지나면 자리를 비운다. 반복 소리는 끌 때까지.
  `SoundEffectInstance.State` 는 고르기에 **쓰지 않는다**(장치 지연·벽시계에 따라 달라 로그 수 재현이 깨진다). 재생 칸이 실제로 아직 소리를 내고 있으면 그 칸만 다음 프레임에 다시 본다(고르기 결과와 무관).
- 시간 기준은 앱 틱(벽시계가 아님) — 같은 실행이면 같은 고르기 결과(로그 수까지 재현). 무음과 소리 켬도 같은 길이 색인을 쓰므로 `AUDIO` 수가 같다(10.1 `Audio` 시험).

**재생**(게임 `Audio/`)
- `SoundBank`: `SoundEffect.FromFile(path)`(파이프라인 없이, 16비트 PCM — 가져오기가 바꿔 둠). 스테이지 시작 때 연결 표에서 그 스테이지에 쓸 소리만 읽는다(`TextureStore` 와 같은 "나올 수 있는 것" 계산 = `StageReach.Enemies` — v0.6: 고른 직업의 `hero.<id>` 포함). 효과음 전부 수 MB.
  v0.6: 꺼진 스테이지·직업은 시작되지 않으므로 건너뛴 묶음의 소리를 읽을 일이 없다. 음악은 `MusicPlayer` 가 그 곡의 `requires` 를 `ContentAvailability` 로 보고 없으면 `fallback` 을 튼다(6.15, 로그 `AUDIO music=… fallback=…`).
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

### 7.10 안개 그리기 `FogRenderer` (v0.5, WP5, 기획 10.7)

시야(4.11)를 읽어 땅을 **보이는 곳 / 가 본 곳(기억) / 안 가 본 곳(검정)** 셋으로 그리고, 시야 밖 대상을 숨긴다. 셰이더 없이(2.2) SpriteBatch 혼합 상태와 반해상도 렌더 타깃만 쓴다.
`--reveal`(`AppOptions.Reveal`, 9절)이거나 `World.Visibility.Enabled` 가 거짓이면 이 절 전체를 건너뛴다(땅은 7.7 빛 지도만, 모든 대상 알파 1, 미니맵 전체).

**값은 알파 채널에 담는다**(v0.5.1, 7.7 "혼합 상태 규칙"): SpriteBatch 기본 효과의 결과는 텍스처 색 × 틴트이고 최댓값 혼합은 혼합 계수를 무시하므로,
v0.5 처럼 "벽·대상 모양을 값 g 의 회색으로 최댓값 혼합" 하면 들어가는 값이 g 가 아니라 **벽·적 그림의 색 × g** 다. 그러면 보이는 뒷벽 윗부분이 검정 대신 색² 밝기로 어둡게 남고(R28 이 막으려던 결과),
시야 밖에서 늘 그리는 보스·닿은 적도 색² 로 어두워진다. 그래서 `FogRT`·`MemRT` 의 값은 알파에 두고, 색은 합치기에서만 쓴다.

**혼합 상태 `FogBlend`**(정적 객체로 한 번 만든다. "A 만" = `ColorWriteChannels = Alpha`, "RGB 만" = `Red | Green | Blue`)

| 이름 | 설정 | 결과 |
|---|---|---|
| `WriteA` | A 만, Alpha Add, One / Zero | dst.a = src.a |
| `MaxA` | A 만, `AlphaBlendFunction = Max` | dst.a = max(dst.a, 그림 알파 × 틴트 알파) |
| `MulA` | A 만, Alpha Add, `AlphaSourceBlend = Zero`, `AlphaDestinationBlend = SourceAlpha` | dst.a ×= src.a |
| `MulInvA` | A 만, Alpha Add, Zero / `InverseSourceAlpha` | dst.a ×= 1 − src.a |
| `MulRgbBySrcA` | RGB 만, Color Add, `ColorSourceBlend = Zero`, `ColorDestinationBlend = SourceAlpha` | dst.rgb ×= src.a |
| `AddRgbBySrcA` | RGB 만, Color Add, SourceAlpha / One | dst.rgb += src.rgb × src.a |
| `LerpRgbBySrcA` | RGB 만, Color Add, SourceAlpha / InverseSourceAlpha | dst.rgb = src.rgb·src.a + dst.rgb·(1 − src.a) (7.7 ④) |
| `MaxRgb` | RGB 만, `ColorBlendFunction = Max` | dst.rgb = max(dst.rgb, src.rgb) (7.7 ④) |
| `CopyRgb` | RGB 만, Color Add, One / Zero | dst.rgb = src.rgb (7.7 ④) |

**칸 값 텍스처** (CPU, 프레임마다)

- `SightTex`(확장 격자 크기, 스테이지 1 80×80): 칸마다 색 흰색, **알파** = `FogMath.CellLevel`(4.11 — 보임 0/1 을 칸이 바뀐 뒤 F = 9틱에 걸쳐 잇는 값, 스테이지 틱 시계 3.4).
  `ExploredTex`(N×N): 알파 = 가 본 칸 1. 둘 다 배열을 다시 쓰지 않고 채워 `SetData`(합쳐 30KB 안팎). `ExploredTex` 는 `ExploredVersion` 이 바뀐 프레임에만.
- **땅 위 투영**: 두 텍스처를 `SpriteBatch.Begin(transformMatrix: 칸 좌표 → LightRT 화면 좌표, blend: WriteA)` 로 한 장씩 그린다. 등각 변환(4.1 `IsoMath`)이 아핀이라 행렬 하나로 텍셀 = 칸 마름모가 되고(텍셀 중심 = 칸 중심),
  `LinearClamp` 이중선형이 칸 중심 사이 알파를 이어 벽 그림자 경계가 칸 반 폭쯤(기획 10.7 "약 32px") 부드러워진다. 칸 모양 계단이 드러나지 않는다.

**보이는 정도 `FogRT`**(480×270, `LightRT` 와 같은 크기, `SurfaceFormat.Color`) — 값 V ∈ [0, 1] = 알파

1. (0,0,0,0) 으로 지우고 `SightTex` 를 투영한다(`WriteA`).
2. **경계 흐림**: 보간된 주인공 발(그리기 위치 — `FogMath` 그리기용 오버로드와 같은 값, 4.11)에 둥근 띠 텍스처(코드로 생성 — 알파가 반경 `R − edge` 안 1, `R` 에서 0, R 밖 0. R = `Visibility.Radius`)를
   `MulA` 로 그린다. 띠 사각형은 반경 R + 64(시야 계산 범위)까지 덮는다 — 그보다 먼 칸은 `SightTex` 가 이미 0 이다. → 거리 R − 40 … R 에서 땅이 보이는 밝기에서 기억 밝기로 바뀐다(기획 10.7).
3. **벽 기둥은 자기 칸 값**: 화면 안 막힌 칸(0b·4·5층 벽 항목, 7.4)의 기둥 그림을 틴트 (255,255,255, `그 칸 CellLevel × 칸 중심의 경계 흐림 값`) 로 `MaxA` 덧그린다.
   벽 그림은 칸 위로 144px 솟아 화면에서 뒤 칸(맵 밖·벽 너머 안 보이는 칸) 자리에 걸치므로, 땅 투영만 쓰면 보이는 뒷벽의 윗부분이 검게 잘린다. 원작도 벽 조각은 그 칸의 빛을 받았다.
   두 줄 벽(앞 턱·모서리·스테이지 2 협로)의 바깥 줄은 4.11 의 "두 줄 벽은 함께 보인다" 로 안쪽 줄과 같은 값을 받는다 — 벽 하나가 밝은 반·검은 반으로 갈리지 않는다.
4. **그리는 대상의 모양**(필요한 것만, v0.5.1): 그리는 유닛(안개 알파 > 0)·장판·예고 채움 중 ① 그림 사각형이 시야 원 `R − edge` 밖에 걸치거나 ② 사각형 네 귀퉁이·발의 칸 중 `CellLevel < 1` 인 칸이 있는 것,
   그리고 ③ 늘 그리는 중간 보스·보스·닿은 적·점화 중 자폭 적을, 틴트 (255,255,255, 안개 알파 × 그림 알파 — 장판·예고는 1) 로 `MaxA`.
   그 밖의 대상은 그 자리 `FogRT` 가 이미 1 이라 빼도 같다(--stress 300 에서 유닛을 한 번 더 300개 넘게 내지 않게). → 머리가 뒤 칸 화면 자리에 걸친 적, 시야 밖에 선 보스, 안개 속 적 장판도 빛 지도를 그대로 받는다(밝기 하한이 꺼진 맵에서도).

**기억 정도 `MemRT`**(480×270) — 값 (1 − V)·E = 알파

1. **(255,255,255,0)** 으로 지우고(색 흰색) `ExploredTex` 를 투영(`WriteA`) → 2. 가 본 벽 칸의 기둥 그림을 틴트 알파 1 로 `MaxA`(3번과 같은 이유) →
3. `FogRT` 를 `MulInvA` 로 그려 알파에 (1 − V) 를 곱한다.

**`LightRT` 에 합치기** (7.7 순서 ③ — 빛 가산 뒤, 유닛 최소 밝기 앞)

- `FogRT` 를 흰 틴트로 `MulRgbBySrcA` → `LightRT` = L × V. 이어서 `MemRT` 를 틴트 (M, 1) 로 `AddRgbBySrcA` → `LightRT` += M × (1 − V)·E(`MemRT` 색이 흰색이라 src.rgb = M).
  M = `주변광 × (1 − 어둠) × rules.sight.memory_ratio`(스테이지 1 약 0.2, 스테이지 2 약 0.14, 기획 10.7).
- 결과 = **V·L + (1 − V)·E·M** (L = 빛 지도). 보이는 곳은 지금까지와 같고, 가 본 곳은 빛 없는 바닥보다 한 단 어두운 고정 밝기, 안 가 본 곳은 0(검정)이다.
  L 은 V 로 잘리므로 폭발·장판의 빛이 벽 너머·시야 밖 땅을 밝히지 않는다. 어둠이 `light_change` 로 바뀌면 M 도 같은 프레임에 따른다. 맵 밖은 바닥을 그리지 않아 원래 검정이다.
  벽·적 그림의 색은 이 값에 들어가지 않는다 — 색은 `WorldRT` 에 곱할 때 한 번만 곱해진다.
- 기억 칸에서는 적·시체·획득물을 그리지 않으므로(아래) 지형만 남는다.
- **남는 한계**: 최댓값은 "자기 칸 값 > 땅 값" 인 경우만 고친다. 다른 시야 막힘 벽 뒤에 숨은 벽이 **보이는 벽·땅의 화면 자리 앞으로** 솟는 배치(앞쪽 벽 두 개가 겹친 협로 등 — 두 줄 벽 규칙이 덮지 않는 것)는
  그 숨은 벽 그림이 밝게 남을 수 있다. MVP 스테이지 1 에는 없다(시야를 막는 것이 둘레 벽뿐). 스테이지 2 캡처에서 보이면 벽 항목만 그리는 순서대로 "지우기(`MulInvA`, 알파 1) + 더하기(Alpha One/One, 자기 값)" 두 번 그리기로
  자기 칸 값으로 덮어쓴다(벽 항목만 묶음이 끊김, R28).

**대상 숨기기 — 안개 알파** (`FogMath`, 4.11)

| 대상 | 안개 알파 |
|---|---|
| 일반·정예·우두머리 적(그림·그림자·발광·번쩍임·발밑 원·이름·등장 연출) | `EnemyAlpha` = `PosAlpha(발)` = 칸 값 × `clamp((R − d) / edge, 0, 1)`(d = 보간된 주인공과 그리는 발 위치의 거리) |
| 중간 보스·보스, 주인공 몸에 닿은 적(거리 ≤ 두 반경 + `TouchReach` — 최소 `touch_margin` 16, 4.11), 점화 중 자폭 적(`Visual.ForceShown`, v0.5.1) | 1(늘 그림, 밝기 하한도 늘 — 기획 10.7) |
| 시체(`CorpseLayer`), 획득물(보석·금화·물약·룬·상자 그림, 보석 빛) | `PosAlpha(자리)` |
| 피해 숫자 | 대상이 주인공이면 1, 적이면 그 적의 `EnemyAlpha`(대상이 없어졌으면 생긴 자리의 `PosAlpha`) |
| 주인공과 그 무기의 투사체·이펙트·장판, 적 투사체, 위험 예고(1층 채움·7층 테두리), 적 장판, 정예 상자 빛 기둥·레벨업 빛 기둥, 표시 이펙트(`FxSystem`) | 1(늘 그림 — 위험은 안개에 가리지 않는다, 기획 10.7·12.6). 장판·예고는 `FogRT` 4단계로 빛도 받는다 |

- 알파 0 이면 그 대상의 그리기 항목을 아예 모으지 않는다(4층 정렬 배열·7층 발광 목록·최소 밝기 목록에서 빠짐 — 그리기 호출도 준다).
- 표시 이펙트를 늘 그리는 이유: 적 쪽 이펙트는 위험(폭발·강타)이고 주인공 쪽은 기획 10.7 이 늘 그리라고 했다. 예외는 적 등장 연출(어두운 원·솟음)과 죽는 그림·시체 — 위 표대로 숨긴다.
- 0.15s 바뀜은 칸 값(`CellLevel`)에만 있다. 벽 모서리를 돌 때 땅과 그 칸의 적이 같이 서서히 나타난다. 바뀌는 중에 다시 바뀌면 지금 값에서 되돌아간다(4.11 "바뀜 값 잇기").
  규칙(사격·돌진)은 바뀐 틱의 값을 바로 쓴다(4.11).

**비용과 확인**

- 반해상도 렌더 타깃 두 장(각 0.5MB — `MemRT` 는 7.7 ④ 의 보조 RT 로 다시 씀), 그리기 호출 + 14 안팎(투영 3·띠 1·`MulInvA` 1·합치기 2·최소 밝기 4·나머지 상태 전환) + **벽·대상 모양 묶음**
  (텍스처마다 끊김 — 대상은 4단계 조건으로 줄인 목록, 벽은 화면 안 벽 항목). 4.9 렌더 예산 안에서 잰다(WP5, 10.3 — 단계별 시간 열 `fog_ms`).
- 확인: `--debug-nav sight`(칸 보임·가 본 칸·시야 막힘 칸, 9절), 10.2 "안개 고정 장면"(`--preview fog` — 자리 고정 캡처 + 빛 지도 PNG 의 값 대조)·"안개 흐름" 행. `--reveal` 캡처와 나란히 놓아 숨김·기억 밝기를 본다.
- 하지 않는 것(MVP): 원작 자동 지도(AMP) 선 그리기, 기억에 적의 마지막 자리 남기기, 화면 가장자리 화살표(기획 10.7), 셰이더 안개.

### 7.11 방 구조 그리기 (v0.7, WP5, 기획 10.8·11.4)

- **문 봉인**: `World.Room.Phase < Doors` 이거나 `Exits` 에 없는 문(들어온 문 포함)은 문 틈 칸(`World.Gates[id].Cells`)의 마름모 위에 붉은 빛 막(코드 그림, 알파 0.5, 스테이지 틱으로 느리게 일렁임 — 3.4 표시 시간)을 4층 앞 벽 단계와 같은 자리에 그린다.
  열린 출구 문은 막을 걷고(0.3s 앱 틱 사라짐) 문 위(벽 높이 위)에 보상 아이콘 48px(`RoomExit.Reward.Icon`), 주인공이 300px 안이면 그 아래 이름. 정예 방 문은 아이콘 테를 붉게 + `정예`, 보스 방 문은 보스 이름 글자, 계단은 `지하묘지로`(`NextActName`).
  v0.8: 정예·보스 방으로 가는 문에도 덤 보상 아이콘이 있다(이름 `정예 상자 + 마법서` 꼴 — 기획 v0.8.1 11.4), `NextFloor` 가 0 이 아니면 아이콘 아래 `▼ 2층`(계단 문 포함). 성소는 성소 이름.
  막은 그림뿐이다 — 충돌·시야를 바꾸지 않는다(기획 2.5 "봉인 표시만"). 원작 그림으로 바꿀지는 기획 열린 질문 32.
- **보상 물체**: `kind: reward` 획득물이라 보통 획득물 그리기(시체·보석과 같은 층, 안개 알파 — 7.10)를 그대로 탄다. 정리 끝에 보상 자리(v0.8 — 주인공 앞, 3.6 "보상 자리". v0.7 은 `center`)에 놓이고 가 본 곳이 아니어도 미니맵에 금색 점(8.2).
- **방 이동 어두워짐**: `PlayScene` 이 `rules.rooms.transition`(0.5s — v0.8: 고른 출구의 `NextFloor` 가 0 이 아니면 `transition_floor` 1.0s) 동안 앱 틱으로 검정 덮개를 0 → 1 → 0(가운데에서 `RunSession.Advance`, 새 `World` 의 첫 프레임은 덮개 1). 시뮬레이션은 돌지 않는다. 캡처 시각은 앱 틱이라 그대로 재현된다.
- 카메라는 새 `World` 의 주인공(들어온 문 진입점)에서 바로 시작한다(보간 없음 — `PrevPos = Pos`). 작은 방(2048×1152)도 화면(960×540)보다 커서 카메라 규칙(7.2)은 그대로다.
- **월드 바뀜**(v0.7.1, 계약 WP1e-형 — 게임 `App/Contracts/IWorldBound.cs`): Core 는 새 `World` 라 저절로 새로 시작하지만 게임 쪽 서비스는 월드를 넘어 산다. 그대로 두면 ① `FxSystem` 의 시체·피해 숫자·이펙트가 옛 방의 "생긴 틱" 을 들고 있어 새 `World.Tick`(0)에서 나이가 음수가 되어 안 사라지고
  ② `EntityId` 가 새 저장소에서 다시 쓰여 개체 ID 를 키로 쓰는 소리 반복·쿨다운·Fx 붙임이 다른 개체에 붙고 ③ 안개의 칸 바뀜 값·맵 크기 텍스처, 미니맵 픽셀·가 본 곳 배열, 맵 그림 묶음이 옛 맵을 가리킨다. 묶음마다 따로 고치면 빠뜨리기 쉬우므로 한 곳에 둔다.

```csharp
public interface IWorldBound { void Attach(World world); }   // 새 World 를 받음 = 그 월드에 묶인 상태를 모두 버리고 다시 만듦
```

  `PlayScene` 이 `RunSession.Advance()` 직전에 옛 월드의 `Presentation` 을 `FxSystem.Consume` → `Audio.Consume` 으로 소비하고 `Clear` 한 뒤, 직후에 **이 순서로** `Attach(newWorld)` 를 부른다:
  `WorldRenderer`(맵 그림 묶음·`FogRenderer` 텍스처·칸 바뀜 값 — WP5) → `FxSystem`(남은 항목 버림 — WP6) → `Hud`·미니맵(가 본 곳 배열·문 점 — WP6) → `IAudioServices`(개체 키 반복 소리 멈춤·쿨다운 비움, 곡은 액트가 같으면 이어 틂 — WP8).
  판을 처음 열 때도 같은 함수로 붙인다(첫 월드도 같은 길). 시험: WP6 의 `PlayScene` 통합 완료 기준에 "방 이동 뒤 첫 프레임에 옛 방 Fx 0·개체 키 소리 0" (10.2 "방 흐름").

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
| `TitleScene` | 씬 | 시작 → 새 게임(v0.7: 바로 Play — 액트 1 의 1번 방, 직업 단계부터 HeroSelect) / 설정(오버레이) / 종료. 메뉴 아래 고른 직업의 최고 기록 한 줄(`최고: 지하묘지 6 / 11`, v0.7 — 8.3). v0.6: 꺼진 스테이지·직업이 있으면 메뉴 아래 한 줄(`ContentAvailability.Disabled(defs).Any` — v0.6.1: 음악만 대체되면 없음, 기획 12.5) |
| `HeroSelectScene` (직업 단계, WP9) | 씬 | 카드 4장(서기 그림·이름·설명·특성, 꺼진 직업은 어둡게 + MPQ 이름) → Play. 고른 직업은 저장의 `last_hero` |
| ~~`StageSelectScene`~~ | — | **v0.7 에서 없앰**(기획 12.5 — 액트는 판 안에서 이어지고 해금이 없다). 헬파이어 액트의 갈래(기획 열린 질문 38)가 생기면 다시 본다 |
| `PlayScene` | 씬 | 월드 + HUD. v0.7: **`RunSession` 소유**(3.6) — 지금 방 `World` 를 돌리고 `StageStep.RoomExit` 면 0.5s 어두워짐 가운데 `Advance()`(앞뒤로 표시 이벤트 소비·`IWorldBound.Attach` — 7.11, v0.7.1), 액트가 바뀌면 미리 읽기·음악(7.9). 판 끝(`StageStep.Finished`) → `RunSession.Conclude()` 한 번(v0.7.1) → Result |
| `LevelUpOverlay` | 오버레이(멈춤) | `World.AwaitingChoice == LevelUp` 이면 Play 가 **같은 앱 틱 안에서** 올림(3.3). v0.7: `== Reward` 면 같은 오버레이를 `IProgression.RewardOffer()` 로 연다 — 제목 `마법서`·`매혹의 성소`(`OfferSource` — v0.8: 성소 창은 무기 고르기, 축복 띠 색은 E), 띠 색(무기 주황), 건너뛰기 문구는 그 보상의 `skip`(체력 — v0.8), 빛 기둥 없음(기획 12.2). 고르기는 `ChooseReward`/`RerollReward`/`SkipReward` |
| `ChestOverlay` | 오버레이(멈춤) | 상자 열기 1.2s 연출 |
| `PauseOverlay` → `SettingsOverlay` | 오버레이(멈춤) | Esc/P, 포기 → 패배 처리 |
| `ResultScene` | 씬 | 승리 3s / 패배 2s 뒤 → 다시 하기(같은 시작 액트·직업, 새 씨앗) / 타이틀. v0.7: 도달한 곳(`RunState.Depth` — `지하묘지 6 / 11`, 승리면 `도살자 처치`)·걸린 시간(`ElapsedTicks`)·방 줄(방마다 받은 보상 아이콘)·무기별 피해(기획 12.4) |
| `PreviewScene` | 씬(개발) | `--preview` |

- 씬 스택은 상태 패턴이다: 위 씬이 입력을 먼저 받고, `PausesBelow` 가 참이면 아래로 내려가지 않는다.
- `PlayScene` 은 시뮬레이션과 그림을 잇는 곳이다: 앱 틱마다 `StageStepper.AppTick(world, command)` 를 부르고(3.3), `World` 를 읽어 `WorldRenderer`·`Hud` 에 넘긴다.
  레벨업 선택은 `world.Progression`(`IProgression`)의 `Choose(index)` / `Reroll()` / `Skip()` 를 오버레이가 직접 부른다(시뮬레이션이 멈춘 사이라 결정성에 영향 없음).
  자동 조작이면 오버레이가 Core `AutoplayPolicy` 인스턴스(v0.6.1 — `AutoplayDriver` 가 판마다 하나 만듦, 9절)의 `PickCard` 답을 정해진 앱 틱 수 뒤에 고른다.
  프레임 끝에 `FxSystem.Consume` → `AppServices.Audio.Consume` → `world.Presentation.Clear()` 순서로 표시 이벤트를 소비한다(3.5·7.9). 마우스 이동은 `AppServices.Render.ScreenToWorld` 로.
- 소리(v0.3): 타이틀·결과는 `PlayMusic("title")`, `PlayScene` 은 `PreloadStage` 뒤 `PlayMusic(<스테이지 ID>)`(v0.7: 액트가 바뀔 때만 — 7.9), 일시정지 오버레이는 `SetPaused`, 레벨업·상자 오버레이와 메뉴는 `PlayUi`(7.9).
- 씬이 바뀔 때마다 로그에 `SCENE <이름> app_tick=…`, 결과 화면이 열릴 때 `RESULT …` 를 쓴다(9절).

### 8.2 HUD (기획 12.1)

- `Hud` 하나가 `World` 를 읽어 그린다: 경험치 막대, 시간, 보스 막대(최종 위·중간 아래), 무기·패시브 칸(v0.8: 열린 칸 수 `Hero.WeaponSlotsOpen`·`PassiveSlotsOpen` 만큼 — 계단 뒤 5), 처치·금화, 미니맵, 체력 구슬과 허리띠 칸 둘(v0.8 `Hero.Belt`, `CueKind.BeltPotion` 에 깜빡임), 지속 효과, 알림 문구.
- **방 줄·방 상태**(v0.7, 기획 12.1 — v0.6 의 판 시간 `07:42` 대신): `Hud` 는 `World` 와 함께 `RunState` 를 받는다. 위 가운데 `대성당 4 / 9`(액트 이름·`RoomPlan.Number` / 그 액트 마지막 방 번호 — v0.7.1, `slice` 판은 `6 / 8`) + `rooms` 원소마다 작은 칸 —
  지난 방 = `RunState.Rooms[i].RewardId` 의 아이콘, 지금 방 = 밝은 테, 앞 방 = `RunState.Act.Rooms[i].Kind`(일반 회색 점·정예 붉은 마름모·보스 붉은 네모). 그 아래 방 상태: `Survive` = 남은 시간(`SurviveTicks − ClockTicks`, 정예·보스 방은 없음),
  `Cleanup` = `남은 적 n`(살아 있는 적 수), `Doors` = `문을 고르라`. 방 문구 `대성당 — 3`(1.5s)은 `World.Banner` 에 감독이 방 시작 때 쓴다(WP4).
- **미니맵**(v0.7): 그 방 맵의 `minimap.png`. `Cleanup` 이면(v0.8 — 수와 상관없이 정리 시작부터. v0.7 은 `remaining_marker` 10 이하부터) **모든 남은 적**을 붉은 작은 점으로(가 본 곳·시야와 상관없이 — 기획 2.5), 보상 물체·옮겨진 정예 상자는 금색, 열린 출구 문은 보상 색 점(`World.Gates[id]` 가운데).
- **보스 막대 여러 줄**(v0.5.3, 기획 12.1 v0.6.2): `World.BossBars`(`List<BossBar(Id, Name, Final)>`, WP4 씀)는 **등장 순서**로 쌓고 죽거나 무너진 적의 줄을 뺀다. 계약 형은 그대로다.
  HUD 는 `Final` 인 줄을 맨 위 큰 막대(480×10, 2페이즈 선)로, 나머지를 목록 순서대로 그 아래 작은 막대(320×6)로 그린다. `Final` 이 없으면 목록 첫 줄(가장 먼저 나온 중간 보스)이 큰 막대다.
  최대 4줄(중간 보스 3 + 최종 보스 — 기획 12.1). 넘으면 `Final` 과 먼저 나온 중간 보스 3줄만 그리고 로그에 경고 한 번(MVP 데이터로는 생기지 않는다). 체력은 그 적에서 읽는다.
  v0.7: 정예 방·보스 방이 따로라 한 방에 막대는 보통 하나다(소환으로 생긴 중간 보스가 없으면). 여러 줄 규칙은 그대로 둔다(같은 방에 둘이 생기는 데이터를 막지 않으므로).
- 체력 구슬은 코드로: 원 마스크 텍스처(생성) + 붉은 채움 사각형을 비율만큼(원 안만 보이게 그리기 순서로) + 돌 테(코드로 만든 회색 고리 그라데이션).
  v0.2 의 "맵 질감으로 구운 테" 는 장애물 굽기와 함께 없앴다. 원작 UI 그림(`ctrlpan\*.cel`)으로 바꿀지는 기획 열린 질문 17.
- 미니맵: 가져오기 도구가 구운 맵의 `minimap.png`(외곽 상자 1/16, 마름모, 막힌 곳 어둡게 — 7.6)를 알파 0.75 로. 점은 1px 텍스처 늘이기, 좌표는 월드 ÷ 16.
  - **가 본 곳만**(v0.5, 기획 12.1·10.7): 읽을 때 픽셀을 CPU 배열로 한 벌 둔다(160×88 안팎). `World.Visibility.ExploredVersion` 이 바뀐 프레임에 픽셀 (x, y) 마다
    월드 점 `(16x + 8, 16y + 8)` 의 칸(`IsoMath` 역변환)이 가 본 칸이면 원래 색, 아니면 투명(0)으로 표시용 배열을 채워 `SetData`(1.4만 픽셀, 1초에 몇 번).
    셰이더·렌더 타깃 가림을 쓰지 않는 이유: 그림이 작고 바뀌는 때가 드물어 CPU 가 가장 단순하고, 같은 가 본 칸이면 같은 픽셀이다. 칸 하나가 4×2px 라 경계 계단은 미니맵 크기에서 문제가 없다.
    `--reveal` 이면 원래 그림 그대로. 원작 자동 지도처럼 벽 선을 따로 그리지 않는다(기획 10.2).
  - **점 규칙**: 주인공 흰색, 중간 보스·보스 큰 빨강, 정예 상자 금색은 늘. 정예·우두머리 파랑은 `FogMath.EnemyAlpha > 0` 일 때만(그 적이 화면에 그려질 때와 같은 판정). 일반 적은 그리지 않는다.
    v0.6.3: 방 보급(`Pickup.SupplyRoom` 이 있는 획득물)은 흰 점으로 늘 — 가 본 곳이 아니어도, 주울 때까지(기획 10.1, 6.12).
- 위젯: `Label`, `Button`, `Card`, `Bar`, `IconSlot` — 즉시 모드에 가까운 작은 클래스. 마우스 올림·방향키 선택·Enter 를 같은 "포커스" 개념으로.
- 글꼴: `C:\Windows\Fonts\malgun.ttf` → `malgunbd.ttf` 순으로 찾고, 모두 없으면 `arial.ttf` + 로그 경고(한글이 네모로 보임).
  `gulim.ttc` 같은 글꼴 묶음(.ttc)은 `FontSystem.AddFont(byte[])` 에 몇 번째 글꼴인지 고를 인자가 없어 목록에서 뺐다. 실제로 넣어 보고 되면 다시 넣는다.

### 8.3 저장 (기획 열린 질문 3)

- 위치: `%LOCALAPPDATA%\DiabloDefenceRemastered\save.json`(기록·누적 금화·설정 한 파일, `version` 필드 — v0.7: 해금 없음). 쓰기는 임시 파일 → 바꿔치기.
- **기록은 시작 액트 × 직업**(v0.6 의 스테이지 × 직업 모양 그대로, 기획 2.5·4.5): `"records": { "<시작 액트 ID>": { "<직업 ID>": { "best_depth": { … }, "victory", "best_time", "level", "kills" } } }`, `"last_hero": "<직업 ID>"`.
  **깊이 모양**(v0.7.1): `"best_depth": { "act": "<액트 ID>", "act_number": 2, "room": 11, "kind": "boss", "rooms_hash": "<그 액트 rooms 표의 StableHash>" }` — 숫자 둘(`[액트, 순번]`)만 두면 방 수 바꾸기(기획 2.5 — 액트 2 일반 방 7 → 6)·순번 흔들기(열린 질문 37)·헬파이어 액트 잇기(38) 뒤에
  옛 `[2,11]` 이 없는 방·다른 방을 뜻해 새 판이 그보다 깊어질 수 없거나 엉뚱하게 비교된다. 읽을 때 `act` 가 지금 사슬에 없거나 `rooms_hash` 가 지금 표와 다르면 **깊이만 비우고**(그 기록의 `victory`·`best_time`·`level`·`kills` 는 남김, 로그 한 줄) 다음 판이 새 깊이를 쓴다.
  비율로 바꿔 잇는 안은 쓰지 않는다 — 방 표가 바뀐 판끼리 깊이를 견주는 뜻이 정해져 있지 않다. `rooms_hash` 는 각 원소의 (`kind`, `number`, `from`, `to`)만 해시한다(맵·보상 가중을 바꿔도 기록은 남게).
  보통 판은 늘 뿌리 액트(`stage_cathedral`)에서 시작하므로 키는 사실상 하나다 — 개발 옵션 판(`--stage`·`--room`)은 저장을 안 쓴다(9절). 키 모양을 남긴 것은 헬파이어 액트 갈래(기획 열린 질문 38)가 "따로 판" 이 되면 그 시작 액트가 키가 되게 하려는 것이다.
  **비교**(v0.7.1 — 기획 2.5 "기록" 도 같은 순서로 고침): 키 = (깊이 `RunDepth`, 승리 여부, −걸린 시간)의 **사전순**. 더 깊으면 새 기록, 같은 깊이면 승리가 패배를 이기고, 둘 다 승리면 짧은 `best_time`, 그 밖은 그대로.
  v0.7 의 "같은 깊이에서 둘 다 승리면 짧은 시간, 그 밖은 그대로" 는 보스 방에서 진 판과 이긴 판이 깊이가 같아(2:11, 단계 1 `1:8`) 옛 기록이 패배면 나중에 이겨도 기록되지 않았다. 깊이가 비워진 기록(위)은 어떤 판보다도 얕다.
  저장에 있는 직업이 데이터에 없거나 꺼져 있으면 `rules.hero.class` 로 시작한다(기록은 지우지 않음).
  v0.6 의 `best_time`(생존 시간)·해금 값은 쓰지 않는다. 저장 구현(WP7) 전이라 옮길 옛 판이 없다(v0.7.1 확인: `%LOCALAPPDATA%\DiabloDefenceRemastered` 가 없고 게임의 저장 계약은 아직 v0.6 모양) — `version` 은 처음부터 v0.7 모양이다.
- **읽기 규칙**(v0.7.1, WP7 — 손으로 고친 파일·다음 판 변경에서 예외나 금화·설정 유실이 없게): 모르는 키는 무시(옛 `unlocked`·`best_depth` 없는 `best_time` 포함), 기록 한 줄(직업 하나)이 깨지면 그 줄만 버리고 로그 한 줄,
  누적 금화·설정은 기록이 깨져도 남김, 파일의 `version` 이 지금보다 높으면 **읽기 전용**(그 판에서는 저장을 덮어쓰지 않고 메모리에만 — 로그 한 줄), JSON 자체가 깨지면 `save.json.bad` 로 옮기고 기본값(지우지 않음).
- **저장 계약**(v0.6.1 → v0.7, 게임 `App/Contracts/AppServices.cs` — WP1e): 직업은 결과에 실려 오고, 저장 구현이 "늘 `sorcerer`" 를 가정하지 않는다(가정하면 WP9 에서 다른 직업의 판이 마법사 기록으로 조용히 저장된다).

```csharp
public sealed record RunResult(string StartActId, string HeroClassId, bool Victory, RunDepth Reached, string ReachedActId, float RunTime, int Level, int Kills, int Gold);
//   HeroClassId = World.HeroClass.Id, Reached·ReachedActId·RunTime = RunSession.Conclude() 뒤의 RunState(v0.7.1). 방 종류·rooms_hash 는 저장 구현(WP7)이 GameDefs 에서 계산해 적는다
public sealed record RunRecord(RunDepth? BestDepth, bool Victory, float BestTime, int Level, int Kills);   // v0.6.1 의 StageRecord 를 대신함. BestDepth null = 방 표가 바뀌어 비운 깊이(v0.7.1)
public interface ISaveService
{
    // … 기존(Settings·TotalGold·SaveSettings·RecordResult) 그대로. v0.7: IsUnlocked 없앰
    string? LastHero { get; }                                   // 저장의 last_hero(없으면 null)
    void SetLastHero(string heroClassId);                       // 직업 고르기(WP9)·--hero 없는 판 시작 때. RecordResult 는 기록만 쓴다
    RunRecord? BestRecord(string startActId, string heroClassId); // 타이틀 한 줄(WP6)
}
```

  쓰는 곳: WP6(판 시작 때 고른 직업으로 `SetLastHero`, 결과에 `HeroClassId`·`Reached`·`RunTime` 을 채워 `RecordResult`, 타이틀이 `BestRecord`), WP7(`SaveStore` 구현).
- **계단 저장**(v0.8, 기획 v0.8.1 2.5 — MVP 단계 3. 계약 WP1f, 파일 WP7, 화면 WP6): 저장 칸 **하나**, 파일 `%LOCALAPPDATA%\DiabloDefenceRemastered\run.json`(`save.json` 과 따로 — 깨져도 기록·설정과 무관), 내용 = `RunSnapshot`(3.6 결정 4) 그대로 + `version`.
  - 쓰기: 액트 보스 방의 문 단계(`RunSession.CanSuspend`)에서 일시정지 메뉴 `저장하고 끝내기` → `Suspend()` → `SaveRun` → 타이틀. 임시 파일 → 바꿔치기(save.json 과 같음).
  - 읽기: 타이틀이 `PeekRun()` 으로 `이어 하기 (지하묘지)`(다음 액트 이름) 한 줄을 보이고, 고르면 `TakeRun()` — **읽는 순간 파일을 지운다**(다시 끄면 그 판은 끝, 판 중간 저장으로 죽음을 되돌리지 못하게). 그 값으로 `RunSession` 이어 하기 생성자.
    생성자가 `RunSnapshotMismatchException`(정의가 바뀜 — 3.6)이면 저장은 이미 지워졌고 로그 한 줄 + 타이틀 한 줄 `이어 할 판을 읽지 못했다`. JSON 이 깨졌으면 `run.json.bad` 로 옮긴다(save.json 과 같은 규칙).
    v0.8.1: 불일치에는 짐의 정의 ID(무기·패시브·축복·성소)가 지금 데이터에 없는 것, `Format` 이 지금 `RunSnapshot.CurrentFormat` 과 다른 것(짐·`RunStats` 모양이 바뀜 — 3.6 "모양 지문")이 든다 — `Restore` 안에서 예외가 터지거나 짐 일부가 조용히 빠진 판으로 잇지 않는다.
    `required` 멤버가 빠진 JSON 은 STJ 예외라 깨진 파일과 같이 `.bad` 다. 저장 파일 모양에는 "없는 값 = 기본" 규칙을 두지 않는다(R41 — 모양이 바뀌면 `Format` 을 올려 버림).
  - 쓰는 틱(v0.8.1): 메뉴의 `저장하고 끝내기` 는 `Suspend()` 하나다 — `Suspend` 가 시뮬레이션을 한 틱 계단 문 `Exit` 명령으로 돌린 뒤 정산한다(3.6 "계단 저장" ⓐ). 앱은 그 틱을 따로 돌리지 않는다(멈춤 중이어도 이 한 틱은 Core 가 돈다 — 렌더·소리 신호는 버림).
  - 새 게임: 저장이 있으면 "이어 할 판이 지워진다" 를 한 번 묻고 `DiscardRun()`. 버린 판은 기록하지 않는다(첫 값 — 기획 열린 질문, 13.20).
  - 기록: 이어 한 판도 한 판이다 — `RunState` 를 되살리므로 결과의 깊이·시간·처치가 판 전체 값이고 `RecordResult` 는 지금처럼 판 끝에 한 번.
  - 개발 옵션 실행은 `run.json` 도 **읽지도 쓰지도 않는다**(`--save-dir` 이면 그 폴더만 — 아래). `ISaveService` 에 더함: `RunSnapshotInfo? PeekRun()`(`NextActId`·`HeroClassId`·`NextActNumber`), `RunSnapshot? TakeRun()`, `void SaveRun(RunSnapshot)`, `void DiscardRun()`.
- 설정(`GameSettings`, 계약 변경 WP1b): 전체 화면, 화면 흔들림, 미니맵, 피해 숫자, **음량 셋**(전체·효과음·음악, 0~100% 10% 단위 — 기획 12.3). 저장 파일에 없으면 음량은 `rules.audio.defaults`(80·80·60%) — `GameSettings` 의 음량은 `float?`(null = 없음)이고 기본값은 rules 한 곳에만 둔다.
- `--save-dir <폴더>` 로 위치를 바꾼다. 개발 옵션 실행(9절 목록)인데 `--save-dir` 가 없으면 사용자 저장 파일을 **읽지도 쓰지도 않는다** — 기본값(창 모드, 기록 없음)으로 시작하고 메모리에만 저장한다.
  읽기까지 막는 이유: 사용자가 켜 둔 전체 화면이 검증 실행에서 화면을 덮고, 사용자의 마지막 직업에 따라 자동 조작의 판이 달라지기 때문이다(brief 7절 — v0.6 까지는 해금 상태도 이유였다).

---

## 9. 개발용 실행 옵션

`Core/Dev/DevOptions.Parse(string[]) → (DevOptions, IReadOnlyList<string> errors)`. 모르는 옵션·잘못된 값은 오류 목록 → 로그 + 종료 코드 64.
시뮬레이션에 걸리는 것은 `RunOptions`(11.4), 앱·화면·소리에 걸리는 것은 `AppOptions`(게임 `App/Contracts`)로 넘긴다.

| 옵션 | 뜻 |
|---|---|
| `--seed <n>` | 시뮬레이션 씨앗. 없으면 시각에서 만들고 로그 첫 줄에 적음 |
| `--autoplay` | 자동 조작(기획 15절). 메뉴도 넘김(각 화면 0.5s 뒤). `--background` 를 함께 켬. 실제 키보드·마우스는 읽지 않음. **안개를 무시한다**(v0.5): `AutoplayPolicy` 는 `World.Visibility` 를 읽지 않고 월드 전체로 판단한다(기획 10.7·15절) — 봇 판단이 그리기 상태에 묶이지 않고, 적 규칙의 시야 조건은 봇이든 사람이든 같다 |
| `--stage <id>` | 타이틀을 건너뛰고 그 **액트의 1번 방**부터 판을 시작(v0.7 — 판이 그 액트에서 시작하는 개발 판. 액트 2 부터면 Lv1 시작 무기라 `--level`·`--weapons` 를 같이 준다, 기획 15절). `stage_cathedral`, `stage_catacombs`. v0.6: 꺼진 스테이지(헬파이어 묶음 없음, 6.17)면 시작하지 않고 로그에 이유 + 종료 코드 5 |
| `--room <액트>:<순번>` (v0.7) | 그 방부터 시작(예: `--room 1:9` = 액트 1 보스 방, 둘 다 1부터 — 액트 번호는 뿌리 액트부터 `next_act` 사슬, 3.6. 순번 = 보이는 방 번호 `RoomDef.Number`(v0.7.1) — `slice` 판의 `--room 1:5` 는 없는 번호라 옵션 오류). `--stage` 를 겸한다(같이 주면 옵션 오류). 앞 방은 건너뛰고(보상 없음) 빌드는 `--level`·`--weapons`·`--passives` 로 준다. 방 맵·들어오는 문·방 씨앗은 처음부터 돈 판의 그 방과 같다(`RunPlanner`·`RunSeeds`, 3.6). 그 방의 보상 = 앞 순번 `RewardOrder` 첫 칸. 범위 밖이면 옵션 오류(64, 데이터를 읽은 뒤). `AppOptions.StartRoom`(`RoomRef?`) → `RunPlanner.ActAt` 로 `RunStart.StartAct`·`StartRoomIndex` |
| `--map <id>` (v0.7) | `--preview tiles`·`--preview fog` 의 맵을 고른다(방 맵이 여럿이 되어 `--stage` 하나로 못 고름). 없으면 `--stage`(없으면 뿌리 액트)의 보스 맵. 그 밖의 실행에서는 옵션 오류. v0.8: `<id>@m` 은 그 맵의 거울판(6.11 "거울판" — `mirror: true` 가 아니면 옵션 오류) |
| `--hero <id>` (v0.6) | 그 직업으로 시작(`AppOptions.HeroClass`, 직업 고르기 화면을 건너뜀 — 기획 15절). 없으면 저장의 `last_hero`(개발 옵션 실행은 저장을 안 읽으므로 `rules.hero.class` = `sorcerer`). 없는 ID 는 옵션 오류(64, 데이터를 읽은 뒤 판정), 꺼진 직업은 종료 코드 5 와 이유. 개발 옵션 판정(저장 보호)에 든다 |
| `--autoplay-visit rooms` (v0.6) | `--autoplay` 와 함께만(아니면 옵션 오류). 자동 조작이 이동 규칙에 더해 맵의 방 기준점(`room_*`)을 이름 순으로 차례로 들른다(기준점 쪽 가중 0.8, 반경 60 안에 닿으면 다음 방, 끝나면 처음부터 — 기획 15절). **v0.6.1**: 기준점 쪽 방향은 직선이 아니라 **길 방향**(`INavWorld.TryPathStep(주인공, 기준점, 14)` 의 다음 꺾임점, 0.5초마다 다시 구함 — 문이 반대쪽 벽에 있는 방에서 벽에 붙어 미끄러지지 않게), 40초 안에 못 닿으면 다음 방으로 넘어간다. 정책 인스턴스가 기록(`Events` — 닿음·넘김 각각 방 ID·스테이지 틱, `RoomStats` — 방마다 들른 수·넘긴 수·방 안에 있던 틱·그동안 받은 피해)을 가지고, WP6 이 새 기록마다 로그 `VISIT room=<id> stage_tick=…` 또는 `VISIT_SKIP room=<id> stage_tick=…` 를, 판 끝에 방마다 `VISITS room=<id> visits=… skips=… inside_s=… damage=…` 를 쓴다(기획 15절). "방 안" 은 방 바닥 칸(v0.6.3: `World.Rooms.IndexAt(주인공 자리)` 가 그 방 — 방 체류 압박과 같은 계산, 6.11). `AppOptions.AutoplayVisit`(Core `AutoplayVisit` 열거형) → WP6 이 `new AutoplayPolicy(visit)` 로 판마다 하나 만듦. 기준점이 없는 맵이면 경고 한 줄 뒤 보통 자동 조작. 결정적이다(봇 입력이 시뮬레이션에 들어가므로 같은 씨앗·같은 옵션이면 같은 해시 — 방 번호 상태는 정책 인스턴스에, 시뮬레이션 상태가 아님). v0.6.3: 이 옵션과 상관없이 판 끝에 방마다 `ROOMS room=<id> doors=… inside_s=… damage=… supplies=…`(WP7, `World.RoomPresence` — 기획 10.1 첫 플레이 로그). **v0.7**: 방 하나가 1~2분이라 들르기는 **판 전체에서 그 판이 쓴 맵마다** 센다 — 정책은 `World` 가 바뀌어도 판마다 하나(`RoomStats` 키 = `(맵 ID, 기준점 ID)` — `RoomVisit`·`RoomVisitStats` 에 `MapId` 를 더함, `VISIT`·`VISIT_SKIP`·`VISITS` 줄에 `map=`), 기준점이 없는 방 맵에서는 이동 규칙만, 방 단계가 `Cleanup` 뒤(보상·문)면 들르기를 쉰다(기획 15절). `ROOMS` 줄은 방이 끝날 때마다 `act=`·`room=`(순번)·`map=` 을 붙이고 기준점은 `anchor=` 로 쓴다(6.12). **v0.7.1**: 줄 이름 `ROOMS` → `INNER_ROOMS`(판 단위 `ROOM` 줄과 한 글자 차이였다), 읽는 속성 `World.InnerRooms`·`World.InnerRoomPresence` |
| 자동 조작의 방 규칙 (v0.7, 기획 15절) | `--autoplay` 의 정책(WP4 `AutoplayPolicy`)이 `World.Room.Phase` 를 본다: `Cleanup` 에서 남은 적 ≤ 10(v0.8: 정책 상수 — `remaining_marker` 키는 없앰, 6.10)이면 가장 가까운 남은 적 쪽 길 방향(`TryPathStep`)을 가중 0.8 로 더함, `Reward` 에서 보상 물체·`GatesExit` 상자로 길 방향, 보상 창은 `PickCard` 규칙(마법서·매혹의 성소 = 무기 순위 — v0.8: 축복 창 규칙 없앰), `Doors` 에서 **문 우선순위**(v0.8, 기획 v0.8.1 15절: 체력 < 50% 이고 허리띠가 비었으면 완전 회복 물약 > 마법서(무기 칸이 남았거나 진화 재료 무기가 Lv8 아님) > 보물 상자 > 성소(마법에 걸린 > 매혹의 > 음산한) > 나머지 — 정예·보스 방 앞 문은 덤 보상으로 같은 순위, 같으면 화면 왼쪽 문 — `Exits` 순서가 아니라 문 틈 가운데의 화면 x, **그것도 같으면 화면 y 가 작은(위) 문, 그다음 `Exits` 순서**(v0.7.1 — 출구 셋일 때 `gate_nw`·`gate_sw` 는 마름모 대칭이라 화면 x 가 같다))로 고른 문의 나가는 자리로 길 방향. `Doors` 가 `rules.rooms.autoplay_door_skip`(20s) 안에 안 끝나면 `PlayerCommand` 에 `CommandButtons.Exit` 깃발 + `ExitIndex = 그 문 번호`(3.6 — 시뮬레이션이 받는 명령이라 결정적, 정책이 상태를 직접 바꾸지 않는다. v0.7.1: 기본값 `PlayerCommand.None`(= `default`)은 깃발이 없어 "안 나감" 이다)와 정책 기록 `DoorSkips`. WP6 이 새 기록마다 로그 `DOOR_SKIP act=<n> room=<n> stage_tick=<틱>`. **v0.7.1 멈추는 단계 덮기**: ① **정예 방**에서 `SpawningOpen` 이 거짓이 되면(2:30 뒤 — 남은 것은 중간 보스와 남은 적) 중간 보스 쪽 길 방향을 가중 0.8 로 더한다(`Cleanup` 규칙과 같게 — `kite` 중간 보스와 서로 멀어지기만 하지 않게). ② **보상 창**의 카드가 0장일 수는 없다(6.18 — 줍는 순간 `단련` 으로 처리, v0.8. v0.7.1 은 `skip`). ③ `Reward` 단계가 `autoplay_door_skip`(20s) 안에 안 끝나면 정책 기록 `RewardStalls` → 로그 `REWARD_STALL act=<n> room=<n> phase=<단계> stage_tick=<틱>`(한 번, 상태는 바꾸지 않음 — 원인을 드러내기만). 시험 기준은 `DOOR_SKIP`·`REWARD_STALL` 둘 다 0 |
| `--start-at <mm:ss>` | (v0.7) 시작 방의 **방 시계** 그 시각부터(`RunOptions.StartAt` → 첫 방 `RoomStart.StartClockTicks` — 3.6, 둘째 방부터는 쓰지 않음). 그 전 방 이벤트는 건너뜀 — 단 **상태형 이벤트(`light_change`)는 앞 방들 것과 그 시각 이전 것을 시간 순서로 차례로 적용한 뒤 시작**(v0.5.1 규칙 그대로, `RoomEvents.Schedule` 의 `Replay`). t·구간·배율은 그 시각 값. 보스 방이면 보스 등장(방 시계 0:03)보다 늦은 시각이면 보스를 바로 내보내고 광폭화 시계를 그만큼 흐른 것으로 친다. 시각 상한(v0.7.1 — 방 종류마다, `SurviveTicks` 는 정예·보스 방에서 0 이라 하나로 쓰면 그 방들이 모두 막힘): 일반 방 = 생존 길이, 정예 방 = `elite_spawn_cap`(2:30), 보스 방 = 상한 없음. 넘으면(같음 포함) 옵션 오류(64). v0.6 의 "판 시각 — 보스 시각 이후면 바로 보스" 는 `--room 1:9` 로 바뀜 |
| `--time-scale <n>` | 실제 시간 n 배속. 결과는 n 과 상관없이 같다(3.4) `[해석]` |
| `--god`, `--level <n>`, `--weapons <id:lv,…>`, `--passives <id:lv,…>` | 기획 15절. v0.6: 고른 직업이 고를 수 없는 무기(`classes`)는 옵션 오류(64, 데이터를 읽은 뒤). v0.8.1: 무기·패시브 수가 시작 액트의 열린 칸(`RunSlots.OpenAt` — 액트 1 은 4, 액트 2 는 5, 3.6 "계단 보상")보다 많으면 옵션 오류(64 — 조용히 자르지 않음) |
| `--stress <적 수>[,<투사체 수>]` | 상한을 그 값으로 올리고 스테이지 구성 가중치대로 맵 전체·생성 띠에 즉시 채운다(`--god` 함께 켬, 생성 출처 `Stress`). 투사체 수를 주면 그 수를 유지하는 시험용 발사를 더함. v0.7: `--room` 이 없으면 그 액트의 **보스 방**(큰 방 — v0.6 측정과 같은 맵)에서 돈다. 방 흐름은 생존 단계에 묶어 둔다(정리·문으로 넘어가지 않음) |
| `--freeze-ai` | 적 생각·이동을 멈춘다(렌더 정적 장면, `--stress` 와 함께) |
| `--capture <png>` | 캡처 파일 경로. 상대 경로는 현재 폴더 기준. 원작 그림이 찍히므로 **저장소 밖**(스크래치·임시 폴더)에 둔다(R24 — 저장소의 `out/`·`logs/` 는 `.gitignore` 로 막지만 다른 곳은 못 막는다) |
| `--capture-at <초[,초…]>` | 앱 시간(초) 기준. 여러 개면 파일명 뒤에 `_<초>` 를 붙인다. 그 틱에 정확히 멈춰 보간 없이(alpha = 1) 그린 화면 |
| `--capture-on <levelup\|chest\|boss\|result\|doors\|reward>` | 그 일이 처음 일어나 화면이 다 열린 프레임을 캡처(시각을 미리 모를 때). v0.7: `doors` = 첫 `Doors` 단계(봉인 걷힘·문 위 아이콘), `reward` = 첫 마법서·성소 창 |
| `--quit-after <초>` | 앱 시간 기준. 종료 코드 0. 캡처가 남아 있으면 먼저 한다 |
| `--exit-on-result` | 결과 화면이 열리면 `RESULT` 로그를 쓰고 종료 코드 0 |
| `--timeout <초>` | **벽시계** 감시. 넘으면 로그에 마지막 앱 틱·씬·스택 요약을 쓰고 종료 코드 4(틱이 안 도는 교착·무한 루프 대비). 개발 옵션 실행 기본 600 |
| `--window <W>x<H>` | 창 클라이언트 크기이자 **캡처 크기**(7.1). 개발 옵션 실행 기본 1280x720 |
| `--background` | 창을 앞으로 끌어오지 않음: `new GameApp()` 전에 환경 변수 `SDL_WINDOW_NO_ACTIVATION_WHEN_SHOWN=1`(SDL2 는 힌트를 환경 변수에서도 읽는다 — P/Invoke 없음), 비활성일 때 멈추지 않음(`InactiveSleepTime = 0`), 마우스 가두지 않음 |
| `--vsync <on\|off>` | 수직 동기(기본 on). 부하 측정은 `off` |
| `--mute` (v0.3) | 소리를 재생하지 않고 효과음 파일도 읽지 않는다(길이 색인만 읽어 고르기 수는 그대로 센다, 7.9). **개발 옵션 실행의 기본값**이다. 장치 자체는 MonoGame 이 열므로, 개발 옵션 실행은 늘 `ALSOFT_DRIVERS=null`(출력 없는 장치)로 연다(7.9) `[해석]` |
| `--sound` (v0.3) | 개발 옵션 실행에서 재생 경로를 켠다 — 장치는 여전히 null("No Output")이라 스피커로는 나지 않는다. WAV 읽기·재생·제한 확인용. `--mute` 와 같이 주면 옵션 오류 |
| `--sound-device default` (v0.4) | 개발 옵션 실행에서도 실제 기본 장치를 연다(`ALSOFT_DRIVERS` 를 두지 않음, `--sound` 를 함께 켬). 사용자 스피커로 소리가 나므로 **사용자 동의를 받고만** 쓴다 |
| `--debug-nav [grid,flow,astar,shapes,entries,tiles,sight]` | 격자·막힌 칸(S/M 등급 색)·clearance 열지도·플로우 필드 화살표·A* 원 경로와 다듬은 경로·충돌 원·충돌 도형(평행사변형)·입구(시작점→진입점)·기준점 ID·**하위 칸 성질(걷기·투사체·시야 막힘 세 색, 8글자 6.11)** 을 덮어 그림. **`sight`**(v0.5): 시야 안 칸(초록 마름모, `ChangedTick` 이 9틱 안이면 밝게)·가 본 칸(회색)·시야 막힘 칸(테두리)·주인공 칸, 반경 R 과 R − edge 원, 화면 사각형을 `view_inset` 만큼 줄인 사각형, 또렷이 보이는 원거리 적에 점. 층은 옵션 값으로 고른다(기본 전부). 키 입력으로 바꾸지 않는다 |
| `--reveal` (v0.5) | 안개를 **그리기에서만** 끈다(`AppOptions.Reveal` — `RunOptions` 가 아니라 Core 는 이 값을 모른다): 땅은 빛 지도만, 시야 밖 대상도 그림, 미니맵 전체(7.10·8.2). `World.Visibility` 와 그것을 쓰는 규칙(원거리·돌진)은 그대로라 같은 씨앗이면 `--log-hash` 가 같다(10.2 "재현"). 같은 뜻의 다른 이름(`--no-fog`)은 두지 않는다(기획 15절 이름 그대로). **개발 옵션 판정(저장 보호)에 든다**(v0.5.1 — 안개 없는 판의 기록이 사용자 저장에 남지 않게, 기획 15절 — v0.7: 해금은 없어짐) |
| `--preview <sprites\|enemies\|effects\|tiles\|blend\|fog>` | 개발 화면. 그림 + 발 기준점 십자 + ID. 배치는 고정(6.13): 정의마다 한 행, 방향·프레임 고정 칸 + 방향을 도는 칸 하나. `tiles` = 맵 전체 축소 + 성질 겹침(7.6), `blend` = 무기 투사체·이펙트를 보통/가산 두 칸으로(기획 열린 질문 10). v0.2 의 `obstacles` 는 없앴다. **`fog`**(v0.5.1, WP5): `--stage` 의 실제 맵(v0.7: `--map` — 없으면 그 액트 보스 맵)에 **장면 여섯 개를 1초(60틱)씩 차례로** 놓는다 — 주인공 자리와 적 배치·동작 프레임을 코드에 고정하고 적 생각·이동은 멈춤(시야는 매 틱 계산, 바뀜 9틱이 장면 안에 끝남). ① 뒷벽 앞(벽 윗부분이 뒤 칸 자리에 걸침) ② 문 앞(문 틈 밖 입장 중 궁수 하나, 둘레 벽 뒤 맵 밖 적 하나) ③ 기둥 옆(기둥 뒤 적) ④ 시야 경계(머리가 R 에 걸친 적, 흐림 띠 안 적, 닿은 거리 안 적) ⑤ 앞 턱 앞(두 줄 턱 바깥 줄·모서리) ⑥ 어두운 바닥의 적·시체·보석(최소 밝기). 장면 k 는 앱 시간 k 초에 캡처하면 늘 같은 그림이다. 캡처마다 `FOG` 로그 줄을 쓰고, `--capture` 파일 옆에 그 프레임의 `LightRT`(7.7 ④ 뒤)를 `<이름>_light.png`(480×270)로 쓴다 — 빛 값을 그림 색 없이 대조한다(10.2) |
| `--check` | 불변식 계측(10.1)을 켠다(`RunOptions.Check`). `--log-ticks` 에 열로, 종료 요약에 최댓값 |
| `--validate-data` | 창 없이 데이터 검사만, 종료 코드 0/2(자원은 보지 않는다) |
| `--log <파일>` | 로그 파일. 기본: 개발 옵션 실행이면 **현재 폴더** `logs\`, 아니면 저장 폴더 `logs\`. 저장소 뿌리에서 돌리면 `logs\` 가 생기므로 `.gitignore` 에 `logs/` 를 둔다(WP1b) |
| `--log-ticks <csv>` | 틱 시간 기록(10.3) |
| `--log-hash <csv>` | 스테이지 틱 60마다 `act, room, stage_tick, room_hash, run_hash`(3.4 `StateHasher.Room`·`Run` — v0.7.1: 방마다 스테이지 틱이 0 부터라 `act`·`room` 이 있어야 행이 겹치지 않고, 앱 틱은 `--time-scale` 비교를 흐리므로 넣지 않음) |
| `--save-dir <폴더>` | 8.3 |

- **사용자 데이터 보호**(brief 7절): 개발 옵션(`--autoplay`·`--stage`·`--room`·`--map`·`--capture*`·`--preview`·`--validate-data`·`--stress`·`--reveal`·`--god`·`--level`·`--weapons`·`--passives`·`--hero` 중 하나라도 — v0.5.1 에 다섯, v0.6 에 `--hero`, v0.7 에 `--room`·`--map` 을 더함)이 있고 `--save-dir` 가 없으면
  저장 파일을 **읽지도 쓰지도 않고** 기본값으로 시작한다(창 모드, 기록 없음). `--window` 는 저장된 전체 화면 설정보다 늘 우선한다. 같은 조건이면 소리도 기본 무음이다.
- 자동 실행에서 데이터 오류가 나면 DataError 화면을 띄우지 않고 종료 코드 2 로 끝난다(입력을 기다리며 멈추지 않게). 자원이 없거나 낡았으면 종료 코드 5(1.4 끝 — `--autoplay` 없이 캡처만 주면 안내 화면을 캡처한 뒤 5).
- 개발 옵션 실행은 `new GameApp()` 전에 환경 변수 `SDL_WINDOW_NO_ACTIVATION_WHEN_SHOWN=1`(`--background`)과 `ALSOFT_DRIVERS=null`(`--sound-device default` 가 없을 때, 7.9)을 둔다.
- 종료 코드: 0 정상, 1 처리 안 된 예외(로그에 스택), 2 데이터 오류, 3 요청한 캡처가 하나라도 안 생김, 4 벽시계 시간 초과, 5 자원 없음·낡음, 64 옵션 오류.
- 고정 로그 문장(판정용): 첫 줄 재현 정보(3.4 — v0.6: `hero=<직업 ID>` 포함), `ASSETS manifest=… format=… stage=… textures=… vram_mb=… bundles=… skipped=… disabled=…`(v0.6 — 만든 묶음, 건너뛴 묶음, 꺼진 스테이지·직업 ID), `SCENE <이름> app_tick=…`, `RESULT start=<시작 액트 ID> hero=… outcome=victory|defeat depth=<액트>:<순번> run_time=… level=… kills=… seed=…`(v0.7 — v0.6 의 `stage=`·`stage_time=` 대신),
  `ROOM act=<n> room=<n> map=<원판 id> mirror=<0|1> gate_in=<id> reward=<id|-> door=<id|-> clear_tick=<틱> cleanup_s=<초> collapsed=<0|1> boss_s=<초|-> picks_left=<n|-> level=<방을 나갈 때 레벨>`(v0.7 — 방이 끝날 때마다 `RunState.Rooms` 의 새 줄, 사람 판도, 기획 15절. `level=` 은 설계가 더함. v0.8: `mirror`·`boss_s`(`RoomRecord.BossClockTicks`, 일반 방 `-`)·`picks_left`(`RoomRecord.PicksLeft`, WP3 없으면 `-`) — 기획 v0.8.1 15절)·`DOOR_SKIP act=<n> room=<n> stage_tick=<틱>`(v0.7, 자동 조작)·`REWARD_STALL act=<n> room=<n> phase=<단계> stage_tick=<틱>`(v0.7.1, 자동 조작),
  `INNER_ROOMS act=<n> room=<n> map=<id> anchor=<기준점 ID> doors=… inside_s=… damage=… supplies=…`(v0.7 — 방마다, 안쪽 방 기준점마다 한 줄. v0.6.3 의 `room=<기준점>` 은 `anchor=` 로 옮김. v0.7.1 에 `ROOMS` 에서 이름을 바꿈. 판의 마지막 방 것은 `Conclude` 때), `VISIT room=… stage_tick=…`·`VISIT_SKIP room=… stage_tick=…`·`VISITS room=… visits=… skips=… inside_s=… damage=…`(v0.6.1, `--autoplay-visit rooms`),
  `AUDIO device="…"`, `AUDIO muted=… requested=… played=… dropped_cap=… dropped_interval=… dropped_offscreen=…`,
  `FOG stage_tick=… in_sight=… explored=… enemies_drawn=… enemies_hidden=… reveal=0|1`(v0.5.1 — 캡처마다, 그 프레임의 시야 안 칸·가 본 칸·그린 적·숨긴 적 수. `--reveal` 이면 `enemies_hidden=0`), 끝 줄 `EXIT code=…`.
- 실행 파일은 WinExe 다. **Windows GUI 실행 파일은 셸이 끝나기를 기다리지 않으므로** 종료 코드는 아래처럼 받고, 판정 근거는 로그 파일로 한다.
  데이터 검사의 1차 판정은 창이 필요 없는 `dotnet test` 의 실제 데이터 시험이다.
- 검증 담당의 실행 예(PowerShell, 사용자 화면·입력·저장·스피커를 건드리지 않음). 결과 폴더 `$o` 는 **저장소 밖**(스크래치)이다 — 캡처에 원작 그림이 찍힌다(R24):

```powershell
$o = "$env:TEMP\ddr-run"   # 저장소 밖
$p = Start-Process -FilePath .\DiabloDefenceRemastered.exe -Wait -PassThru -NoNewWindow -ArgumentList @(
  '--autoplay','--seed','7','--room','1:2','--window','1280x720',
  '--capture',"$o\s1.png",'--capture-at','30,120','--quit-after','125',
  '--save-dir',"$o\save",'--log',"$o\run.log",'--log-hash',"$o\h1.csv",'--check')
$p.ExitCode
```

---

## 10. 테스트 전략

### 10.1 단위 테스트 (xunit.v3, Core 만, 그래픽 없음, 자원 없음)

| 폴더 | 무엇을 | 담당 |
|---|---|---|
| `Common` | `FacingMath.ToColumn`(8방향: 남 0, 서 2, 북 4, 동 6 / 16방향 / **등각 보정: 화면 (−32, 16) → 8방향 열 1**, 보정 끔이면 45° 열), **`FromColumn` 왕복 8·12·16방향(등각 보정 포함)**, **`ToVector(SouthWest)` → 8방향 열 1·16방향 열 2**, `SimRandom` 재현, `StableHash`, `EntityStore` 핸들(지운 뒤 낡은 핸들은 무효)·압축, 풀 재사용, `AnimClock`(이벤트 프레임 1부터·넘김, 재생 배율), `CameraMath`, `TickAccumulator`(틱 상한·버린 틱·캡처 멈춤, `--time-scale` 1 과 8 이 같은 앱 틱 열), `DefRef` 한 번만 채움, `StageStepper`(히트 스톱·늘임·`AwaitingChoice`, **v0.7.1 판정 순서**: `Phase == Exit` 이고 `AwaitingChoice` 가 켜진 월드 → `AwaitingChoice`, 끝난 판 → `Finished`), `PlayerCommand.None`·`default`·`new PlayerCommand()` 에 `CommandButtons.Exit` 가 없음 | WP1, WP1b, WP1e-형 |
| `Map` | `IsoMath` 칸 중심 ↔ 칸 번호 왕복, 외곽 상자(N 40 → 2560×1408, N 44 → 2816×1536), 바깥 마름모 안/밖, `ArenaLayout.Expand`(합성 타일셋: 테두리 여덟 자리·문 2칸 + 끝 조각·`place`·`line`·바닥 섞기 결정적·둘레 표시), 성질 → 하위 칸(**v0.5: 8글자 → 걷기·시야·투사체 비트, 격자 밖은 시야를 막지 않음, `Prop` 왕복**), `LayoutHash` 가 배치가 같으면 같고 기둥 하나 옮기면·성질 표 한 칸 바꾸면(**시야 글자만 `x` → `X` 여도**) 다름, 성질 표에 없는 메가타일 오류·쓰지 않는 메가타일이 표에 있으면 오류, `MapDataReader`(작은 JSON: 두 종류만 읽음·다른 종류의 오류와 무관·타일셋 연결·`extends` 오류), `ResolvedMap`(타일 → 월드 px, 입구·기준점·시작). **v0.6**: `grid` 놓기(`.` 건너뜀·없는 이름 오류·`piece` 와 같이 쓰면 오류), `over_border`(테두리 칸 덮음·둘레 표시 남음·문 칸이면 오류, 없으면 테두리 겹침 오류 그대로), `marks` 번호가 이름 붙인 번호 밖이면 오류·`marks` 를 바꿔도 `LayoutHash` 같음, 13.5-19(`open_center` 안 기둥 셋 → 오류·둘 → 통과·벽 → 오류, `closed_door` 놓기 경고·`decor_why` 면 없음, 방 기준점이 막힌 칸이면 오류). **v0.6.1**: `grid` 줄의 공백 여러 개·줄마다 다른 칸 수, `marks` 의 `closed_door` 키 읽기, `over_border` 막힘 줄어듦(둘레 왼쪽 열에 `wall_h` → 오류, 모서리 → 통과), 하위 칸 검사(모서리 자리에 가로·세로 조각만 둔 방 → 하위 1칸 틈 오류·모서리 조각 → 통과, 둘레 바로 옆 메가 벽 줄의 끝을 막은 띠 → 갇힌 바닥 오류, 아치 문 → 1칸 틈 오류), **6.11 예 배치 좌표 그대로 만든 합성 맵(합성 타일셋 이름)이 13.5-19 전체 통과**(예와 검사가 다시 어긋나지 않게), `ValidateRooms` 의 경고가 `MapDataReader` 결과에 `DataSeverity.Warning` 으로. **v0.6.3**: `room_areas` 읽기, `RoomFloors`(6.11 예: 문 수 피 방·창살 방 1·ㄱ 안·Γ 안 2, 피 방 바닥 81칸 — 오른쪽 벽 바깥 반 칸·문 틈 빠짐, 칸·월드 px 의 방 번호, 같은 입력 같은 결과, 영역 없으면 `Empty`), 방 영역 오류(없음·키·맵 밖·겹침·기준점 밖·문 0), 빈 `grid`·`.` 만 있는 `grid` 오류. **v0.7**: `room_kinds` 읽기, `GateZones`(합성 맵 네 문: 칸 = 열린 메가 둘의 걷기 칸, 앞 턱 문 두 줄, `Opposite` nw↔se·ne↔sw), `MapArea.WalkableCells`(빈 방 = N² − 막힌 칸, 갇힌 칸은 안 셈, 같은 입력 같은 값), 13.5-23 맵 쪽(문 이름·변이 다르면·`center` 없으면 오류, 작은 합성 방에 문 하나 방 → 오류, 진입점 140px 에 짧은 벽 → 오류·둘레 벽만 → 통과, `room_kinds` 빈 맵은 검사 안 함, 실제 `cathedral_arena` 에 `room_kinds` 를 단 사본이 통과) | WP1b, v0.6 WP1d, v0.7 WP1e |
| `Assets` | `SpriteMeta` 읽기·검사(사각형이 페이지 밖·개수 ≠ 방향 × 프레임·없는 페이지 번호면 오류), `ImportTable`(표 8개 엄격 읽기·모르는 속성 오류, 이름 목록: 묶음 × 변종 페이지, 마법 방향 묶기, 아이콘, 소리 `vars` 펼치기 → `d1/sound/…`, 동작별 `width`·`shadow_alpha` 덮어쓰기 순서), `ArenaArt`(바닥 조각 이름 규칙 — 합성 배치에서 손으로 센 조각 목록과 같음, `cells` frame 1부터), manifest 판 대조. **13.5-10**: ① 표 → 이름 목록(기본) ② 실제 정의가 가리키는 이름 ⊂ 표가 만드는 이름 + `exe.json`(`Integration`, WP7 로더 필요). **도구 경로와 게임 경로의 `LayoutHash` 가 같음**(실제 `Data/`, `Integration`). 자원이 있으면(`DDR_ASSETS`) manifest·메타·PNG IHDR 를 정의와 대조, 없으면 건너뜀 표시, 판·표 해시가 다르면 "낡음" 건너뜀 표시(1.3). **v0.6**: 표 `bundle`·`prefer` 엄격 읽기(`optional` 은 모르는 속성 오류), `OutputBundles(arenas)`(묶음 항목 이름 → 묶음, 기본 → null, **v0.6.1: 합성 `l5` 타일셋 항목 `bundle: hellfire` 를 쓰는 맵의 `arena.json`·`floor_*`·`columns.*`·`minimap.png` → hellfire**), manifest `skipped_bundles`(이유 넷)·파일 `bundle`·`sources` 읽기, `ContentAvailability`(빈 `skipped_bundles` → 모두 있음, `hellfire` 건너뜀 → `requires: [hellfire]` 스테이지만 `MissingFor` 비지 않음, `Disabled(defs)` 가 꺼진 스테이지·직업만·`hfmusic` 만 없으면 `Any` 거짓, `Skipped` 가 이유를 돌려줌), 13.5-10 묶음판(`Integration` — 맵 그림·소리 연결 줄 귀속 규칙 포함) | WP1b(형), WP0(이름 대조), v0.6 WP1d(형·`ContentAvailability`) |
| `Data` | 규칙별 고장 난 작은 JSON(`Fixtures/`)으로 각 오류가 정확한 파일·경로로 나오는지. `extends` 병합·순환, `*_add`·`*_patch`, **페이즈 누적 풀기(patch → add → remove, 같은 ID 자리 지킴)**, 모르는 속성, 비슷한 ID 제안, 항목마다 오류 모음, `rules` 하위 키 합치기·중복, **파일 목록을 뒤섞어 읽어도 `GameDefs` 해시가 같다**, 가중치가 JSON 순서 배열, 변종이 그림 묶음에 없으면 오류. **v0.6**: `hero_classes` 종류 읽기·연결, 13.5-20(기본 스테이지 구간표에 `requires: [hellfire]` 적 → 오류, `requires: [hfvoice]` → 오류, `rules.hero.class` 가 묶음 직업 → 오류, 음악 `fallback` 이 다시 묶음 → 오류), 13.5-21(시작 무기 `classes` 밖·클립 없음·`attack` 클립 없음 → 오류), 소리 연결 `hero` 가 직업 ID 키. **v0.6.1**: 기본 스테이지의 `unlock`(v0.7: `next_act`) 이 묶음 스테이지 → 오류, 특성 패시브가 `trait` 아님·`classes` 밖 → 오류, 맵 정의에 `requires` → 모르는 속성 오류, `StageReach.Enemies`(합성: 소환으로만 나오는 적·분열체의 분열체·페이즈 패턴 소환 포함, 고리가 있어도 끝남, 결정적 순서)와 그것을 쓴 13.5-20(기본 스테이지의 적이 `requires: [hellfire]` 적을 소환 → 오류) | WP7 |
| `Data`(실제, `Integration`) | 실제 `Data/` 를 엄격 모드로 읽어 오류 0(모든 모듈 검사기 포함 — 길 검사·소리 연결 표). 모든 모듈 등록이 필요하므로 통합 때 기본으로 옮김 | WP7 작성, WP6 통합 |
| `Run` (v0.7) | (v0.7.1: 시험마다 **[구현-1]**(단계 1 — 일반 방·한 액트) / **[구현-2]**(단계 2 전 — 정예·보스 분기·계단·넓이 배율·작은 방) 표시는 11.1 WP1e 의 경계를 따른다. 표시가 없는 것은 구현-1.) **`RunPlanner`**(합성 액트 — 일반·정예·보스 방과 작은 맵 둘·큰 맵 하나): 같은 입력 같은 `ActPlan`, 씨앗을 바꾸면 달라짐, **`ActPlan` 전체는 (액트 정의, 액트 번호, 씨앗, `rules.rooms`)의 함수이고 맵은 앞 방 계획에 이어짐, 들어온 문·출구 수·보상 순서는 `Flow(씨앗, 액트, 방 번호)` 만의 함수**(v0.7.1 — 다른 방 번호의 결과를 바꿔 끼워도 그 셋은 그대로), `RunPlanner` 의 공개 함수에 `World` 인자가 없음(플레이와 무관), 풀이 둘이면 이웃 순번 맵이 같지 않음(씨앗 1,000개)·하나면 늘 그 맵, 보스 방 들어온 문 `gate_sw`, 액트 보스 방 출구 하나(`gate_ne` — v0.8: 정예·보스 앞 방은 둘·셋, WP1f)·판의 마지막 방 출구 없음, 셋 비율 0.4 ± 0.03(씨앗 1만), 보상 순서 = 가중 비복원 순열(모든 종류 한 번씩, 첫 칸 분포가 가중 비율 ± 0.02). **`RunSeeds`** 두 줄기가 다름·`RunPlanner` 의 뽑기 순서 고정(맵 → 문 → 출구 → 보상 — 바꾸면 고정 값 시험이 깨짐). **`RoomEvents.Schedule`**: t → 방 시계, 유예 전 이벤트는 0:03, 방 밖 t 버림, 보스 방은 끝 없음, 지난 `light_change` 가 `Replay` 에 시간 순, `startClockTicks` 앞 이벤트 버림(상태형은 `Replay`), 같은 틱 정의 순서. **`RemapEntry`**(v0.7.1 서로 바꾸기): 6:30 행진(`at_entry gate_se`·`toward_entry gate_nw`)이 들어온 문 `gate_sw` 면 그대로, `gate_se` 면 `gate_nw` → `gate_se` 로 시작과 목표가 함께 바뀜(두 경우 모두 시작 ≠ 목표), 들어온 문도 맞은편도 아닌 문은 그대로. **v0.8.1(WP1f)**: 서로 바꾸기를 없앰 — `gate_sw` 로 들어온 방에서 `RemapEntry("gate_ne")` = `gate_ne`(보스·대장이 맞은편 그대로, v0.7.1 시험 `== "gate_sw"` 를 뒤집음), 거울판 보스 방(`gate_se` 로 들어옴)의 보스 `gate_ne` → `gate_nw`, 시작 문이 들어온 문이 아닌 행진은 목표가 들어온 문이어도 그대로. **`LightingState.Apply`**: 경로 다섯 종류, `World(defs, RoomStart)` 가 첫 틱 전에 `Replay` 를 적용(감독 없는 합성 월드에서 앞 방에서 바뀐 어둠·주인공 빛 반경이 첫 틱 시야 계산에 들어감). **`RoomFlow`**(표 3.6 의 줄마다 한 시험, 합성 월드): 유예 3s 동안 `SpawningOpen` 거짓, 일반 방 생존 끝 → 정리, 정예 방 중간 보스 `Damage` 처치 → 정리(`Collapse` 로는 안 넘어감)·2:30 에 `SpawningOpen` 거짓, 보스 방 마지막 → `VictoryPending`·아니면 무너짐 + 보상(정리 없음), 정리 중 분열체도 기다림, `cleanup_limit`(v0.8 25s — 시험은 합성 값) → 무너짐·`Collapsed`, 마지막 일반 방 정리 → 승리, 보상 물체가 보상 자리(v0.8 주인공 앞 — v0.7 은 `center`)에 하나·남은 정예 상자 `GatesExit`, 대기(레벨업·상자·보상) 남으면 문 안 열림, 문 열기 거르기(`RewardAvailable` 거짓 칸 건너뜀·모자라면 `fallback_reward`), 나가는 자리 밖·닫힌 문 틈·들어온 문 틈에서는 안 나감, `CommandButtons.Exit` 로 나감, 주인공 죽음은 어느 단계든 패배, 정리 중 t 멈춤·보스 방 t 안 멈춤. **v0.7.1**: `PlayerCommand.None` 으로 `Doors` 단계를 600틱 돌려도 `Exit` 로 안 감, **보상 물체를 줍기 전에는 문이 안 열림(물약·마법서 각각 — 물약은 체력 가득이어도 집히고 열림)**, 끌려오는 보석이 남아 있으면 보상을 집어도 `Doors` 가 아님, 문 틈에 서서 `Reward` → `Doors` 가 되면 `Exit` 아님(밖으로 한 걸음 → 다시 들어서면 `Exit`), `Doors` 에서 나가는 자리에 선 틱에 레벨업 대기가 생기면 그 틱은 `Exit` 아님, [구현-2] 마지막이 아닌 액트 보스 처치 틱에 떨어진 정예 상자가 `GatesExit`·열기 전에는 `Doors` 아님, [구현-2] 정예 방 t 가 `to` − 1틱에서 멈춤(방 시계 2:00 의 `DifficultyMinutes` < `to`), [구현-2] 정예 방 생존 끝 = `DefIndex` 가 `miniboss` 인 `EnemyKilled`. **`RunSession`**(합성 액트 + 시험용 `FakeHeroCarry`): 방 이동마다 새 `World`(이전 저장소·`Visibility` 를 같이 쓰지 않음), 짐 왕복(레벨·체력·칸이 그대로), `RunStats`·`ElapsedTicks`·`Fairness` 누적(**v0.7.1: 두 방 뒤 `Kills` 가 두 방의 합 — 월드 설치·주인공 만들기가 `RunStats` 를 지우지 않음**), [구현-2] 계단 → 다음 액트 1번 방(`ActPlan` 새로)·`light_change` 다시 적용(앞 방에서 바뀐 어둠이 다음 방에 있음), **`--room` 같음**: 처음부터 돈 판이 방 번호 n 에 들어온 순간과 `RunStart(StartRoomIndex: n 의 배열 자리)` 의 첫 방이 맵·들어온 문·방 씨앗·같은 짐을 넣었을 때 첫 600틱 **방 해시**(3.4 — 판 누적·고른 문 빼고) 열이 같음(주인공 정지 명령), 마지막 방 뒤 `Advance` 는 예외. **`Conclude`**(v0.7.1): 진 방(생존 중 주인공 HP 0)·마지막 방 승리 각각에서 `Conclude` 뒤 `ElapsedTicks` = 모든 방 `World.Tick` 의 합(마지막 방 포함)·`Rooms` 수 = 들어간 방 수(마지막 줄 `Door = null`)·`Fairness` 합, 두 번 불러도 같은 값, `Conclude` 뒤 `Advance` 예외, 판이 안 끝났을 때 `Conclude` 예외 | WP1e(그 뒤 WP4) |
| `Geometry` | 원·사각형·다각형(오목 포함) 부호 거리, 밀어내기 방향, 선분 교차, 원 중심에 놓인 점 | WP2 |
| `Visibility` (v0.5) | `VisibilityField`(합성 맵): 빈 방에서 반경 안 칸 모두 보임·`R + 64` 밖 안 보임, 시야 막힘 벽 뒤 칸 가림·벽 칸 자신은 보임, **꼭짓점 규칙**(대각선으로 이은 벽 새지 않음·기둥 하나 모서리 스침은 안 막음), `x` 기둥 뒤 보임·`X` 기둥 뒤 가림, 맵 밖 칸은 문 틈으로만 보임(둘레 벽 뒤 맵 밖 안 보임), **선 걷기 = 느린 기준 구현**(무작위 격자 200개 × 모든 칸), 같은 칸 안 이동은 다시 계산 안 함(횟수 계측)·칸·반경 바뀜은 다시, 계산 범위 밖으로 나간 칸 `InSight` 거짓, 가 본 칸 N×N 만·단조 증가·6틱 주기·`ExploredVersion`·`ExploredHash`(같은 집합 같은 값, 개수가 같고 집합이 다르면 다른 값), **두 줄 벽(v0.5.1)**: 합성 `XXXX` 메가타일 두 줄 벽에서 안쪽 줄이 보이면 바깥 줄도 보임·가 본 칸, 옆 메가타일로 퍼지지 않음, `L`·`M` 칸은 함께 보임에 들지 않음, `IsClearlyVisible` 경계(R − 40, 화면 안쪽 16px — 아래 254, **위는 몸 위 끝** `pos.y − top`, top 0 이면 254)·`ClearReach`(양옆 = R − 40, 정위 = 254 − top, 대각선은 사각형·원 교점), `Enabled` 거짓이면 질의 늘 참·`ClearReach` +∞, **반경 바뀜은 다음 틱부터**(`light_change` 가 바꾼 틱의 질의는 옛 반경, 다음 틱 새 반경), **첫 계산은 첫 `BeginTick`**(생성자 뒤 `Setup` 이 주인공을 옮기면 옮긴 자리 둘레만 가 본 칸), 첫 계산 `ChangedTick = −9`, **바뀜 값 잇기**(9틱 안에 왕복하면 `ChangedTick = t − (9 − a)`), 할당 0(`Update` 1,000번), `Perf`: 스테이지 1 크기 다시 계산 1회 ≤ 0.1ms. `FogMath`: 칸 바뀜 9틱 경사, **9틱 안 왕복 시 `CellLevel` 연속**(바뀐 틱 앞뒤 차이 ≤ 1/9, 매 틱 뒤집혀도 0·1 로 튀지 않음), 경계 흐림 알파, 그리기용 오버로드(주인공 위치를 바꾸면 알파가 그 위치 기준), 보스·닿은 적(`TouchReach` 가 `touch_margin` 보다 큰 적은 그 거리까지)·`ForceShown` 1, 꺼짐 1 | WP1c |
| `Nav` | `ArenaCollision`: 평행사변형 합집합 = 막힌 칸 합집합(점 표본 1만 개), `w` 칸은 걷기만·투사체 통과, 시야 글자는 도형에 영향 없음(**합성 타일셋 시험** — v0.6 부터 실제 스테이지 1 의 창살 칸으로도), 둘레 도형 `IsBoundary` 는 `phase_through` 도 막음. clearance 알려진 점 값(바깥 마름모 경계 포함), 작은 손 격자에서 플로우 방향, A* 최적성(무작위 격자 200개에서 다익스트라와 비교 — **끈 당기기 전** 경로), 코너 자르기 없음, 다듬은 경로가 막힌 곳을 지나지 않음, 목표 못 갈 때 가장 가까운 곳, 시작이 3칸 밖까지 막힘. **두 스테이지 실제 맵 전수(자원 없이)**: (a) 모든 통과 칸에서 `dir` 을 따라가면 칸 수 이하 걸음에 시작점(고리 없음) (b) 모든 통과 칸 중심의 점 유닛이 이중선형 방향으로 10초 안에 주인공 120px 안 (c) 마름모 네 꼭짓점 쪽·기둥 옆·(스테이지 2) 협로 안·(v0.6) 각 안쪽 방 기준점에 주인공을 둬도 `S`·`M` 필드가 모든 통과 칸에서 dist < 65535 (d) 13.5-4~6. **v0.6**: 갇힌 바닥 칸(합성: 둘레 바로 옆 메가 벽 줄 → 오류, `over_border` 겹침·하위 3칸 띄움 → 통과, 사방이 막힌 방 → 오류), 26·34 등급 진입점 → 모든 `room_*`(문을 2칸 → 1칸으로 좁힌 합성 방 → 34 오류), (v0.6.1) 합성 T 자 접합(세로 벽이 가로 벽 밑에 붙는 메가에 세로 조각만) → 13.5-4 80px 오류·모서리 조각 → 통과, 진입점 60px 규칙, `w` 창살 칸: 걷기 도형만·투사체 선 통과·길 검사에서 막힘, `PathLength`(트인 선 = 직선 거리, 벽 너머 방 안 = 문 돌아가는 길이, `maxLength` 넘으면 +∞, 결정적). **v0.7.1 문 틈 닿음**(13.5-23 ③): 합성 방 맵에서 문 틈 한쪽을 벽 끝 조각으로 막아 반경 14 가 못 들어가게 한 배치 → `문 틈에 못 닿음` 오류, 막지 않은 배치·실제 `cathedral_arena` → 통과 | WP2 |
| `Spatial` | 무작위 점 집합에서 해시 질의 = 전수 비교 결과(거리로 거른 정확한 집합), 결과 순서 결정적, **한 칸에 200개 몰아 넣었을 때 넘침 규칙**(돌려준 개수·반경 줄여 다시) = 전수 | WP2 |
| `Movement` | 두 적 같은 좌표 → 1틱 뒤 둘 다 유한·떨어짐, 20마리 같은 점 행진 → 진입 2초 뒤 모두 유한·겹침 해소, 기둥(평행사변형) 중심 소환 → 밖으로 나옴, 입장 중 적은 해시·분리에서 빠짐, 이동 의도 6종, `PlacementResolver`(마름모 모서리·기둥 옆 소환 → 모두 통과 칸, 생략 수 결정적. **v0.6** `PlaceReach`: 문이 반대쪽인 방 벽 바로 너머 후보는 버리고 다음 후보, 문이 기준 쪽인 방 안은 허용, `Detour` 1.5) | WP2 |
| `Invariants` | 계측기가 일부러 만든 NaN·맵 밖(바깥 마름모)·장애물 안·멈춘 적을 센다 | WP2 |
| `Combat` | `DamageMath` — 기획 4.2 예(오라+함성 = ×1.55, 반올림 마지막, 방어 후 최소 1, 치명), `damage_pct` 한 번만, 발사 시점 고정, 시간 배율 제외 대상, 등급 배율, 피격 무적, `--god`, 승리 대기 중 주인공 피해 무시, 입장 중 적만 있는 월드에서 매직 미사일이 쏘지 않음·장판 위를 지나도 HP 그대로, **투사체 `on_end` 장판(맞음·사거리 끝·장애물)**, 신호(`WeaponFired` 발동당 하나, 피격 무적 중 `HeroHurt` 없음) | WP3 |
| `Weapons` | `WeaponStats.Resolve`(count+projectile_count, area_pct 는 선언된 키에만, 쿨 상한 0.6/마력 폭주 0.8, **damage 는 damage_pct 와 무관**, `null` = 상시), 조준 5종, 패턴별 발사 결과(발 수·퍼짐각·궤도 위치·접선 `Vel`), 관통·튕김이 같은 적을 두 번 안 때림, 적별 타격 간격 | WP3 |
| `Progression` | 경험치 곡선(1→5, 2→15, 19→185, 20→215, 40→825 — v0.8 곡선, 6.10), 레벨업 후보(가중치·칸 참·초반 보장·채움·3장 미만), 재추첨 수, 상자(진화 우선·중간 보스 3회·강화 없으면 금화), 레벨업 밀어내기(반경 150, 80px). **v0.6 직업**: 주인공 만들기(기본 능력치 → 직업 덮어쓰기 → 특성 더하기 — 합성 직업 둘로 `max_hp`·`cooldown_pct` 값), 시작 무기 = 직업 것, `classes` 가 다른 직업의 무기는 후보에 안 나옴, `requires` 가 스테이지·직업 밖인 무기는 후보에 안 나옴, 특성은 슬롯을 쓰지 않음. **v0.6.1**: 특성 = `trait` 패시브(1레벨 `per_level` 이 능력치에 더해짐, 레벨업·상자 후보에 안 나옴), 특성 둘을 단 합성 직업, 패시브·진화 `classes` 거르기. **v0.7 문 보상·축복·짐**: 보상 물체 줍기 → 종류별(마법서 = 무기 카드만·성소 = 축복 카드만으로 `RewardOffer` 의 `Source`, 보물 상자 = 정확히 레벨업 1회 + 금화 × `gold_scale`, 물약 = 체력 전부), `RewardAvailable`(무기 칸이 차고 모두 만렙이면 마법서 거짓, 축복 모두 최대면 성소 거짓, 체력 70% 이상이면 물약 거짓), 재추첨 수를 레벨업과 나눠 씀, 건너뛰기 금화, 대기 순서 레벨업 → 상자 → 보상, 축복은 패시브 칸을 안 쓰고 능력치에 더해짐·레벨업·상자 후보에 안 나옴, `RoomCleared` 다음 틱 보석·금화가 끌어당겨짐·보스 `RoomCleared` 면 체력 전부, `IHeroCarry` 왕복(Capture → 새 월드 Restore → 능력치·칸·레벨·체력·재추첨 수가 같음, 진화 무기 포함). **v0.7.1**: `IHeroCarry` 가 칸 번호를 지킴(무기 둘을 칸 0·2 에 둔 빌드 → 새 월드에서도 0·2, 두 방 뒤 `RunStats.DamageByWeapon[칸]` 이 그 무기 피해의 합), 대기가 남은 월드에서 `Capture` 는 예외, `SpawnPickup(Reward: r)` 의 획득물이 `GatesExit`, **0장 보상**(문 연 뒤 무기 칸이 차고 모두 Lv8 → 마법서 줍기 = 금화 25·`PendingRewards` 0, 축복 모두 최대 → 성소 줍기 = 금화 25), 체력 가득 + 완전 회복 물약 → 집힘, `ResetCombat`·주인공 만들기가 `RunStats` 를 안 건드림 | WP3 |
| `Enemies` | **전수 연기 시험**(6.13), 점화 중 죽으면 폭발 없음, 분열 3마리 일반 등급·튕김 80px(장애물에서 멈춤)·0.3s 대기, 대장 죽음 → **그 대장의 호위만** 공포 1.5s, 호위 없는 대장 죽음 → 주변 해골 공포 없음, **`panic_on_ally_death`(반경 150 안 같은 종 죽음 → 0.8s 도망, 1.5s 안에 다시 안 겁먹음, 정예는 안 겁먹음, `Collapse`·`Culled` 로는 안 겁먹음)**, `escort` 대형 120·풀림 250·출처별 호위 수(이벤트 값 / 기본 8 / 구간 0), 행진 직진 시간 뒤 보통 행동(`toward`·`toward_entry`), `kite` 거리 띠·물러날 수 없으면 제자리 사격, `shoot_projectile` 가리면 안 쏨, **시야 조건(v0.5): 또렷이 보이지 않는 궁수는 쏘지 않고 다가옴(시야 막힘 벽 뒤·R − 40 밖·화면 위아래 밖 세 경우), 보이게 되면 그 틱부터 쏨, 돌진은 또렷이 보일 때만 시작·돌진 중 안 보여도 안 끊김, 보스 패턴·근접·자폭은 시야 무관**. v0.5.1 — 시야 조건이 판정을 바꾸는지 드러나게 짠다: 벽 뒤 경우는 **시야만 막는 `L` 줄**(걷기·투사체는 트임) 뒤에 궁수·돌진 적을 둔다(`X` 벽이면 `ProjectileLineClear`·`HasClearLine` 이 먼저 막아 `SeenClearly` 를 빼먹어도 통과한다). R − 40 밖 경우는 `TestWorldBuilder` 로 빛 반경을 띠 최대 + 20 쯤으로 낮춘다(13.5-15 를 거치지 않음). 화면 위 경우는 몸 위 끝이 화면 안쪽 16px 밖이고 발은 안인 자리. **각 시험은 같은 배치에서 시야를 끈 월드(`Enabled = false`)로 쏘거나 돌진하는지 대조군을 같이 본다**. **떨림(R29)**: 주인공 바로 위에 궁수를 두고 ① 주인공이 위로 다가갈 때 ② 멈춰 있을 때 10초 동안 `kite` 상태(다가감·물러남·쏨) 전환 수 ≤ 6, ③ 주인공이 아래로 물러날 때 궁수가 따라옴, 위쪽 궁수가 그 자리에서 쏨(위쪽 한계가 띠 최소보다 작아도). **자폭 점화 중 `ForceShown`**, 생성 때 `BodyTop`·`TouchReach`(사거리 여분 + 10 이 16 을 넘는 합성 근접 적). **중간 보스(v0.5.3)**: `clear_sight` 패턴은 `L` 줄 뒤(대조군: 시야 끈 월드에서는 시작)·R − 40 밖에서 시작하지 않고 조건 없는 패턴(사방 산성 분출)은 시작함, 시작 뒤 이벤트 프레임 전에 주인공을 `L` 줄 뒤로 옮기면 투사체 0·쿨은 씀(`shoot_projectile` 도 같은 시험), `clear_sight` 패턴 투사체의 `SightGated` 가 참, `rank: miniboss` 의 `kite` 가 `L` 줄 뒤에서 다가옴(시야 끈 대조군은 멈추고 쏨), 클립 없는 `trigger: cooldown` 동작이 그 틱의 이동 의도를 막지 않음(걷는 중 2s 마다 장판), 중간 보스 `split_on_death` → 머드맨 3 일반 등급·`Source = Split`·같은 틱 `EnemyKilled` 하나·히트 스톱 5틱, `Collapse` 면 분열 없음, `BossBars` 가 등장 순서·죽으면 빠짐(최종 보스는 `Final`). **함성 대상(결정 C)**: `buff_area` 를 쓰는 `rank: miniboss` 적이 자기 자신과 반경 안 다른 중간 보스·보스에게 버프를 걸지 않음, 오버로드 정예 함성 반경 안 도살자의 식칼 피해가 35 그대로(`DamageMath` 까지 통과한 값), 돌진 예고·시간, `ground_hazard` 동시 20 상한, `war_cry` 겹치지 않음(큰 값 하나), 버프 면역(`slow`), `phase_through` 경계만 막힘·`alpha_over`, `heavy_strike` 넉백, `summon` 상한 30·절대 상한 360, 보스 페이즈 50% 1회·패턴 덮어쓰기·쿨다운이 같은 패턴에 남음·`voice` 신호, 패턴 `actions` 두 개가 같은 프레임, 접두사 합치기·제외 조건, `attack_cooldown_pct` 가 모든 능력 쿨에, 승리 때 `Collapse`(분열·폭발·드롭 없음) | WP4 |
| `Director` | 누적기(무리 1~3, `spawn_group`, 최대 수에서 3 고정), 생성 위치가 생성 띠 안·걸을 수 있음·여유 거리(v0.5 의 "보이지 않음" 조건은 v0.5.1 에서 뺌 — 6.12), 12번 실패 → 먼 입구, **마름모 모서리 주인공에서 입구 대체 비율 기록**(R23), 상한 300/360 과 출처별 막는 순서·지우는 순서(`Culled` 는 보상·시체 없음), 포위 고리 반경 줄이기·생략(마름모 밖 자리, v0.6: 벽 너머 방 안 자리 — 길 거리 > 직선 × 1.5), 정예 종류 뽑기에서 `elite_eligible: false` 제외, `--start-at` 이 지난 이벤트 건너뜀·**지난 `light_change` 는 차례로 적용**(12:00 시작 스테이지 2 의 `World.Lighting` = 처음부터 돈 판의 12:00 값 — v0.7: `--room 2:11` 의 보스 방 = 처음부터 돈 판의 그 방)·보스 시각 이후면 보스 바로(v0.7: 방 시계 판, 9절), **생성 수**: 상한 무한·처치 없음 월드에서 구간마다 `floor(초당 × 구간 길이)` ± 3, **중간 보스 이벤트(v0.5.3)**: 앞 중간 보스가 살아 있어도 다음 `miniboss` 가 제 시각에 나옴, 13.5-18 검사기가 고장 난 작은 스테이지 셋(간격 1:30, 중간 보스 구간이 앞보다 큼, 같은 묶음 중간 보스 둘)에서 경고 하나씩·실제 두 스테이지에서 경고 0. **v0.7 방**: 감독이 `World.DifficultyMinutes` 로 구간·배율을 고름(방 시계 0:30·t 시작 3:30 → 4:00 구간), `SpawningOpen` 이 거짓이면 누적기·이벤트가 멈춤(유예·정리·정예 방 2:30), 생존 끝에 남은 방 이벤트 버림, 정리 중 분열은 나옴, `max_alive × AreaScale`(0.65 → 200 이 130), 정예 판정 = t 시작 ≥ 2:00 인 일반 방에서 방 시계 0:30 한 번(정예·보스 방에서는 없음), 이벤트 `entry:` 가 들어온 문이면 맞은편 문(행진 목표 문도), 위치 후보가 없는 일반 생성은 들어온 문을 빼고 가장 먼 문, 방 시작 때 지난 `light_change` 다시 적용(액트 2 10번 방 1:30 의 어둠 0.7 이 11번 보스 방에 있음), 방 문구 `대성당 — n` 을 `Banner` 에, 방 없는 월드(`Room == null`)는 v0.6 그대로. 13.5-18 방 판(정예 방 사이 일반 방 0 → 경고)·13.5-23 ⑤(방 밖 이벤트·정예 방 2:30 밖 이벤트 → 오류, 바꾼 문 겹침 → 경고). **v0.7.1**: 정예 방 방 시계 2:00(중간 보스가 1:00 넘게 삶)에도 감독이 그 정예 방 구간(5:00–6:00 줄)을 쓰고 다음 구간의 새 종이 안 나옴, 13.5-18 이 정예 방 전체 `[t 시작, to)` 에 걸친 구간을 봄(정예 방 가운데서 구간이 커지는 고장 스테이지 → 경고), 13.5-23 ⑤ 고장 픽스처(정예 방 `miniboss` 이벤트 적 ≠ `rooms[].miniboss` → 오류, `miniboss` 이벤트 둘 → 오류, 서로 바꾼 뒤 행진 시작 문 = 목표 문 → 오류, 풀의 맵 하나에만 있는 기준점을 `toward` 로 → 오류, 방 맵 둘인 액트의 점 빛 `[tx,ty]` → 오류), 감독이 `toward_entry` 를 `RemapEntry` 로 바꿈(들어온 문 `gate_se` 인 방의 6:30 행진이 `gate_nw` → `gate_se` 로 가로지름). **v0.8.1**: 행진은 `RemapMarch`, 13.5-23 ⑤ 고장 픽스처에 "바꾼 뒤 시작 문 = 들어온 문"(맞은편 문이 없는 합성 맵) → 오류, 실제 두 액트(원판·거울판, 들어올 수 있는 문마다) 오류 0 | WP4 |
| `Audio` | `SoundMixer`: 전체 24·같은 소리 3·최소 간격 0.05·몬스터 맞음 0.08s 한 번·죽음 4·같은 종 0.1s·보석 0.06s, 자리가 없으면 새 소리를 버리고 높은 우선순위가 먼저 자리를 얻음, **자리는 길이 색인의 끝 틱에 빔(높낮이 배율 반영)**, 화면 밖 버림(분류별), 좌우 값, 같은 입력 같은 결과, **같은 신호 열 → 무음과 소리 켬의 `AUDIO` 수가 같음**. 연결 표 검사기(없는 대상·없는 소리·없는 분류 → 오류, 소리 없는 그림 묶음 → 경고). **`Sim/` 소스 파일이 `Core.Audio` 를 참조하지 않음**(저장소 소스 문자열 검사) | WP8 |
| `Art` | 아틀라스 메타·PNG 대조 검사기(자원 있을 때만), PNG IHDR 읽기 | WP5 |
| `Dev`·`Save` | `DevOptions` 파싱(값 형식, 여러 캡처 시각, 오류 메시지, 개발 옵션이면 저장 안 읽음·기본 무음 판정 — **`--reveal`·`--god`·`--level`·`--weapons`·`--passives` 하나만 줘도 개발 옵션**(v0.5.1), `--sound`/`--mute`, `--preview fog`), `StateHasher` 가 같은 상태에 같은 값·가 본 칸 집합만 다르면 다른 값, 저장 직렬화·버전(음량 셋 포함). **v0.6**: `--hero`(개발 옵션 판정, 없는 ID·꺼진 직업), `--autoplay-visit rooms`(`--autoplay` 없으면 오류), 저장 기록 스테이지 × 직업·`last_hero`. **v0.6.1 저장 왕복**: 합성 직업 둘(`a`·`b`)로 같은 스테이지 `RecordResult` → 다시 읽어 `BestRecord(stage, a)`·`(stage, b)` 가 따로 남음, `SetLastHero` → `LastHero` 왕복. **v0.7**: `--room 1:9`·`--room` 범위 밖(64)·`--room` 과 `--stage` 같이(64)·`--map` 이 미리보기 밖(64)·`--start-at` 이 방 생존 이상(64), `--room`·`--map` 이 개발 옵션, 기록 비교(더 깊음 → 새 기록, 같은 깊이 승리 둘 → 짧은 시간, 얕은 승리 없음·같은 깊이 패배 → 그대로), `RunDepth` 사전순, `StateHasher` 가 판 흐름·방 상태만 다르면 다른 값. **v0.7.1**(합성 액트): `--room 1:9 --start-at 03:10`(보스 방) 통과·`--room 1:5 --start-at 02:40`(정예 방 2:30 넘음) 오류(64)·일반 방 생존 길이와 같은 시각 오류, `slice` 판의 없는 번호 `--room 1:5` 오류, 기록 비교 키 **(깊이, 승리, −시간)**(같은 깊이 패배 → 승리 = 새 기록, 같은 깊이 승리 → 패배 = 그대로, 둘 다 승리 → 짧은 시간), **저장 읽기**(v0.6 모양 픽스처 — `unlocked`·`best_depth` 없음 → 예외 없음·금화·설정 유지·기록 없음, 기록 한 줄 깨짐 → 그 줄만 버림, 더 높은 `version` → 읽기 전용(저장해도 파일 바이트 그대로), 방 수가 줄어든 데이터(`rooms_hash` 다름)로 옛 기록 읽기 → 예외 없음·깊이만 비움·새 승리가 기록됨), `StateHasher.Room` 은 `RunStats`·판 흐름만 다르면 같은 값·`Run` 은 다른 값 | WP7 |
| `Sim` | **결정성**: 같은 씨앗·자동 조작으로 30초(기본) / 3분(`Scenario`) 두 번의 상태 해시 열이 같다. **묶음 독립성**: `TickAccumulator` 로 프레임당 1틱 / 4틱으로 묶어 `StageStepper` 를 돌려도 해시 열이 같다(레벨업 오버레이 흉내 포함). **표시 이벤트 켬/끔**(`Presentation.Enabled`)과 상관없이 해시 같음. **시야(v0.5)**: 같은 씨앗 두 번의 가 본 칸 수 열이 같음, `Sim/Autoplay/` 소스가 `Visibility` 를 참조하지 않음(소스 문자열 검사 — 봇은 안개 무시). **묶음(v0.6)**: `Sim/` 소스가 `ContentAvailability`·`AssetManifest` 를 참조하지 않음(소스 문자열 검사 — 시뮬레이션은 묶음을 모름), 같은 씨앗을 `World(…, heroClass)` 의 합성 직업 둘로 돌리면 해시가 다르고 같은 직업이면 같음. **할당**: 정상 상태 틱 1,000번 동안 증가 0(방 이동은 틱 밖이라 세지 않음). `Scenario`: **액트 1 실행**(v0.7 — `RunSession` 으로 1번 방부터 액트 끝까지, 자동 조작 + `--god`. 단계 1 = 일반 방 여섯, 예외 없음, 불변식 기준 통과 — 공정성 열 포함, `DOOR_SKIP` 0, 같은 씨앗 두 번의 해시 열·`RoomRecord` 목록이 같음. v0.6 의 "`stage_cathedral` 10분"). **v0.7.1 방 틱 상한**: 시나리오는 방마다 방 시계 상한(일반 방 = 생존 길이 + `cleanup_limit` + 60s, 정예 방 = `elite_spawn_cap` + `cleanup_limit` + 60s, 보스 방 = 10:00 — 시나리오 상수)과 `Reward`·`Doors` 단계 상한(각 `autoplay_door_skip` + 10s)을 두고, 넘으면 그 액트·방 번호·단계를 적고 실패한다(시간 초과만으로 끝나 원인을 모르는 일이 없게). `REWARD_STALL` 0. **판 전체**(v0.7.1, 단계 3 완료 기준 — `[Trait("Long")]` 로 따로 돈다): `RunSession` + `AutoplayPolicy` + `--god`, 실제 `Data/`, 씨앗 2개, 액트 1 첫 방부터 판 끝까지(계단 → 액트 2). 기준: 예외 없음, `DOOR_SKIP`·`REWARD_STALL` 0, 공정성 세 열 0, 계단 앞뒤 `RoomRecord.Level` 이 이어짐, 액트 2 첫 방 보상이 `first_room`(보물 상자), 같은 씨앗 두 번의 `RoomRecord` 목록·판 해시 열이 같음. **공정성 합성 맵**(v0.5.1, `Scenario`): 안쪽 `L`·`X` 벽과 문 틈이 있는 `TestDefs` 합성 맵 + 궁수·돌진·자폭·근접 섞은 구간표로 자동 조작 + `--god` 3분, 공정성 세 열 0·`kite_switches` 기준(아래). `Perf`: 4.9 표 | WP4 |

| 프로젝트 `DiabloAssetImporter.Tests` | 무엇을 | 담당 |
|---|---|---|
| `Formats` | 시험 인코더로 만든 CL2: 그룹/평면 판별(그룹 머리가 평면 표로도 읽히는 경우), RLE 세 종류, 32줄 건너뛰기 표로 폭 추정(표 `width` 와 다르면 종료 코드 4 경로), 줄이 넘어가는 런. 레벨 CEL 조각 여섯 종류(삼각형 패딩 앞/뒤), MIN 뒤집기, TIL 펴기(0 기반), SOL 세 비트 대조(`sol & 7` → 8글자, v0.5 — 다른 비트·함정 128 무시, SOL 13 → `x`·15 → `X`·159/175/191 → `X`), TRN → PAL 순서, 색인 0 → 그림자 알파(값이 없는 표는 검정 그대로), WAV 8비트 → 16비트 값·머리·소리 색인 값 | WP0 |
| `Bake` | 잘라 묶기: 겹침 없음·페이지 안·순서 고정·빈 프레임·**그림자만 있는 줄이 사각형 안에 남음(알파 > 0 기준)**·페이지 넘침 → 다음 페이지, 변종끼리 메타 같음, 마법 16파일 → 방향, `arrows` 프레임 → 방향, 아이콘 겹침, 아레나 바닥 조각 경계·이름 = `ArenaArt.FloorChunks`·기둥 중복 제거, **`IsoMath` 로 놓은 기둥 아래 줄과 구운 바닥 마름모가 같은 줄에서 맞닿음(1px 틈·겹침 없음)**, 성질 대조(표 ≠ SOL → 실패, `sol_known_diffs` 면 통과, **시야 비트만 다른 칸도 실패**) | WP0 |
| `Pipeline` | 시험이 만든 작은 MPQ(War3Net 으로 생성)로 전체 실행: 두 번 = manifest 같음, **`--jobs 1` = `--jobs 8`**, 캐시 적중 결과 = 새로 만든 결과, `--force`, **표에 있는데 원본에 없는 항목 → 종료 코드 3·manifest 없음·없는 목록 전부 보고**, 원본에 표 밖 파일이 많아도 성공, `d1/` 밖 파일 안 건드림, 원본 MPQ 파일 바이트·시각 그대로, **다른 현재 폴더에서 실행해도 저장소 뿌리 기준 `--out`**, 저장소 안 무시되지 않는 `--out` 거절.
  **묶음(v0.6)**: 시험 표에 `bundle` 항목을 넣고 ① 묶음 MPQ 없음 → 그 항목 없음·`skipped_bundles` 기록·종료 코드 0 ② `--skip-bundles hellfire` → 같은 결과 ③ (v0.6.1) 묶음 MPQ 는 있는데 파일 없음 → 그 묶음 전체 건너뜀(`판 다름: <목록>`)·기본 항목 그대로·종료 코드 0·끝 보고 `경고:` 줄, 같은 MPQ 를 `--hellfire` 로 직접 주면 3 ④ 같은 이름이 diabdat·묶음 MPQ 에 있으면 diabdat 바이트 ⑤ `prefer` 묶음이 있으면 그 바이트, 없으면 다음 순서 ⑥ 묶음을 켰다 끈 두 실행에서 기본 항목 출력 바이트 같음 ⑦ `--hellfire` 기본 = `--diabdat` 폴더(대문자 파일 이름도 찾음)
  ⑧ (v0.6.1) 깨진 묶음 MPQ(머리를 망친 파일) → `열 수 없음` 건너뜀·종료 코드 0, `--hellfire` 직접 지정이면 2 ⑨ (v0.6.1) 합성 `l5` 타일셋 항목 `bundle: hellfire` + 그것을 쓰는 합성 맵 → 묶음 건너뜀이면 그 맵 그림 없음·기본 맵 그림 그대로, 그 manifest 로 시작 검사 시험(WP7)이 꺼짐 1·오류 0
  ⑩ (v0.6.1) 파일 줄 `sources`: diabdat TRN + 묶음 CL2 출력 → 두 이름 정렬. **`audit` DUN 대조**: 시험 인코더로 만든 합성 DUN(우리 배열을 4×4 그대로 담은 것 → 4, 바닥만·벽 모서리 하나만 같은 것 → 0). **실제 표 전체 경우**: 실제 `Table/*.json` 이 펼치는 모든 원본 경로에 합성 파일(프레임 수는 정의 JSON 값, TRN 256바이트, TIL 은 쓰는 메가타일 번호보다 크게)을 넣은 MPQ + 실제 `Data/maps`·`tilesets` 로 종료 코드 0, 만든 이름 = 13.5-10 이름 목록. 합성 MPQ 는 War3Net 이 쓸 수 있는 범위에서 원본과 같은 플래그(암호화·파일 이름 키 fix key·PKWARE implode)로 만든다 — 못 쓰는 플래그(파형 압축 등)는 "실제 원본 실행에서만 확인"(WP0 완료 기준).<br>**AI 확대(v0.8 — v0.8.1 에 나눔, 1.4 "AI 확대 — 시험")**: 가짜 실행 파일 `tests/FakeUpscaler`(시험이 빌드)로 `Process` 경로 전체(인자·`-m`·종료 코드·진행 없음·`-t 128` 다시·결과 파일 없음·크기 틀림·0xC0000135 → `Vulkan 런타임 없음`·묶음 나누기, 시험이 허용 해시를 주입), `IUpscaler` 가짜로 처리 ①~⑦·세 단계·`--jobs 1` = `--jobs 8`·일부 중간 캐시 적중 + 실패 → 전부 원본·다음 실행이 남은 판만·맵 알파 점 확대·몬스터 알파 프레임마다 한 번(변종 사각형 같음), Vulkan 런타임이 없다고 꾸민 실행 → 띄우지 않고 6 | WP0 |
| `RealMpq`(선택) | 환경 변수 `DDR_DIABDAT` 가 있을 때만 돈다(없으면 건너뜀 표시): 표의 원본 항목이 모두 열리고 머리(CL2 그룹 수·WAV RIFF·SOL 크기)가 맞음, 출력은 임시 폴더. 원본 바이트는 시험 출력에 남기지 않는다. **v0.6.1**: `DDR_HELLFIRE_DIR`(묶음 MPQ 폴더)가 있으면 묶음마다 대표 항목(`plrgfx\monk\mlt\mltas.cl2`, `sfx\monk\monk69.wav` — hfmonk·hfvoice 둘 다, `nlevels\l5data\l5.sol`, `music\dlvle.wav`, `monsters\nkr\nkrn.cl2` 등)이 War3Net 으로 열리고 머리가 맞음 — MVP 표에 묶음 항목이 없어 WP9·WP10 전까지 아무도 실제 묶음 항목을 읽지 않는 빈틈을 메움 | WP0 |

- **v0.8 시험**(WP1f — 거울판·(맵, 방향) 뽑기·출구 둘·덤 보상·보상 자리·층·짐 왕복·계단 저장 왕복과 판 해시·`--start-at` 상한·입장 이벤트·방 이벤트 자리·짝 표·새 보상 정의·AI 확대 메타 모양)는 11.1 WP1f 완료 기준에 적었다. 각 폴더(`Run`·`Map`·`Data`·`Assets`·`Save`)에 들어간다.
- 도구 시험의 바이트 픽스처는 **시험 코드의 인코더**(CL2·CEL·MIN·TIL·SOL·WAV 를 쓰는 작은 함수)가 만든다. 원본에서 잘라 낸 바이트를 저장소에 넣지 않는다(R24).

- 부품·시스템 테스트는 `TestWorldBuilder`(WP1 제공, WP1b 갱신: 코드로 만든 작은 타일셋·맵·정의, 전수 공간 질의, 직선 내비)로 만든다. 실제 데이터에 기대는 테스트와 분리한다.
- **불변식 계측 `InvariantChecker`**(Core, WP2): WP2 가 `SpatialRebuild` 안에서 60틱(1초)마다 부른다 — `nan`(유한하지 않은 위치), `out_of_map`(입장 중이 아닌데 바깥 마름모 밖), `in_obstacle`(통과·입장 중이 아닌데 걷기 막힘 도형 부호 거리 < −2px),
  `stuck`(입장·공포·기절·`Hold`·독점 행동이 아니고 주인공까지 거리 > 공격 사거리 + 32 인데 3초 동안 8px 미만 이동), `over_cap`(상한 초과).
  기준: `nan = out_of_map = over_cap = 0`, `in_obstacle` 이 같은 적에서 1초 넘게 이어지지 않음, `stuck` 최댓값 ≤ 살아 있는 적의 2%. 시나리오 시험은 늘 켜고, 게임은 `--check`(`RunOptions.Check`)로 켠다.
- **공정성 열**(v0.5.1, 기획 10.7 공정성 1~5 — 부품마다 단위 시험만으로는 새 부품·접두사·소환 경로에서 깨져도 못 잡는다). `World.Fairness`(계약 WP1c, 누적 수)를 쓰는 곳이 세고 `InvariantChecker` 가 열로 낸다. 기준은 셋 다 **0**.
  - `unseen_shots`: 적 진영 투사체를 만드는 순간(`ICombatSpawner` 한 곳, WP3) 쏜 적이 보스·중간 보스가 아니거나 **`SightGated` 인 발사**이고(v0.5.3 — `clear_sight` 패턴, 기획 10.7 "세지 않는 것" 의 예외), `IsClearlyVisible(발, BodyTop)` 이 거짓.
    발사 프레임에 다시 보는 규칙(6.6)이 있어 기준 0 이 지켜진다.
  - `unseen_charges`: 돌진(`charge`)을 시작하는 순간(WP4) 같은 조건.
  - `hidden_hits`: `DamageResolve`(WP3)가 주인공 피해를 적용할 때(`--god`·피격 무적으로 피해를 버리기 전에 센다 — `--god` 시나리오에서도 셈) 원인이 적 본체(근접·자폭·돌진 — 적 투사체·장판·예고 패턴은 뺌)이고 보스·중간 보스가 아니며 그 순간 `FogMath.EnemyAlpha`(틱 값) = 0.
    자폭은 점화 중 `ForceShown` 이라 폭발 순간 1 이다 — 점화를 시야 밖에서 시작해도 번쩍임이 보이므로 맞다.
  - `kite_switches`: `kite` 상태 전환 수(6.6). 기준은 살아 있는 `kite` 적 하나당 초당 ≤ 1(떨림이면 수십). 10.2 "안개 흐름" 실행의 `--log-ticks` 에서도 숫자로 본다.
  - v0.6: 스테이지 1 에 안쪽 방이 생겨 실제 맵에서도 벽 가림이 생긴다(기획 10.7). `stage_cathedral` 10분(v0.7: 액트 1 실행) `Scenario` 를 **방 들르기 자동 조작**(`AutoplayPolicy` 의 rooms 모드 = `--autoplay-visit rooms`)으로 한 번 더 돌려 세 열 0 을 본다. 창살 너머 원거리 적 시나리오 하나(궁수를 창살 방 안에 두고 주인공이 창살 앞 — 쏘고 `unseen_shots` 0, 주인공 투사체도 창살을 지나 맞음).
    **v0.6.1 — 빈 통과 막기**: 세 열 0 이 "벽 가림 경우가 생기지 않아서" 나온 것이 아님을 같이 본다. ① 그 실행에서 정책의 `RoomStats` 가 **모든 `room_*` 를 2번 이상**(넘김 수·방 안 시간·받은 피해는 따로 보고), ② `InvariantChecker` 의 새 열 `occluded`(60틱마다 표본: 입장 중이 아닌 일반 적(보스·중간 보스 아님) 중 주인공에서 시야 반경 R 안인데 `Visibility.IsVisible` 이 거짓인 적 수, 합계) **> 0** — 실제로 벽 너머 적이 있었다는 증거. 둘 중 하나라도 못 미치면 통과가 아니다.
    **v0.7**: "`stage_cathedral` 10분" 은 **액트 1 실행**(`RunSession`, 기획 10.7)이다. ① 의 "2번 이상" 은 판 전체에서 **그 판이 쓴 맵마다** 센다(같은 맵을 여러 방이 쓰면 합침 — 단계 1 은 `cathedral_arena` 하나라 여섯 방의 합), 공정성 세 열·`occluded` 는 `RunState.Fairness`·방마다 열의 합이다.
    방 하나가 1~2분이라 한 방 안에서 모든 기준점을 두 번 못 돌 수 있다 — 그래서 합친다. 단계 2 이후 작은 방 맵은 문 하나인 방이 없어 기준점이 구역뿐이다.
  - 이 열은 액트 1 실행 `Scenario`(v0.7 — v0.6 의 `stage_cathedral` 10분), 공정성 합성 맵 3분 `Scenario`(`Sim` 행), 10.2 "길찾기" 실행에 들어간다. 합성 맵에는 `L` 벽 모서리 + 자폭 적 배치를 넣어 `hidden_hits` 가 그 경우를 실제로 잡는지 확인한다(점화 중 `ForceShown` 을 쓰지 않는 시험 전용 자폭 부품으로 바꿔 등록하면 > 0).

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
| 벽 가림·정렬 | `--room 1:1 --autoplay --god --capture-at 5,10,15,20` (v0.7 — 단계 1 의 방은 모두 `cathedral_arena`, 단계 2 부터는 `--room 1:9`. 자동 조작이 기둥 둘레를 지남) | 기둥 뒤 주인공 위 벽이 반투명, 주인공보다 앞이고 빛 반경 안인 벽·앞 턱도 반투명(뒤쪽 벽은 불투명), 기둥 앞 유닛이 기둥 위에 그려짐, 기둥 오른쪽 아래에 붙은 유닛이 벽 뒤로 들어가지 않음 |
| 뒷벽·앞 턱·아치 | `--room 1:4 --start-at 00:30 --autoplay --god --capture-at 10,30,60`(v0.7 — 옛 4:00 = 4번 방 t 3:30 + 0:30. 무리가 뒷벽을 타고 돎) + `--debug-nav entries` | **뒷벽에 붙어 선 적의 무기·팔이 다음 벽 칸에 잘리지 않음**, 앞둘레 턱이 그 앞 유닛을 덮음(주인공이면 반투명), 아치 문 아래 유닛은 아치 앞면에 덮이고 문 앞 칸 유닛은 아치 위에 그려짐, 뒤쪽 문으로 들어오는 적이 뒷벽 위에 떠 보이지 않음 |
| 예고·빛 | `--room 1:6 --start-at 01:00 --autoplay --god --capture-at 5,15`(v0.7 — 옛 7:00) | 벽 뒤쪽에 생긴 예고의 테두리가 벽 위로 보임(7층), `unit_min_light` 0.6(두 스테이지 모두)으로 시야 안 가장자리 적의 몸통·시체·보석이 보이고 바닥은 어두움 |
| 안개 고정 장면 (v0.5.1) | `--preview fog --map cathedral_arena --capture <png> --capture-at 1,2,3,4,5,6`(v0.7: `--stage` 대신 `--map`) 과 같은 명령에 `--reveal` 을 더한 실행(장면 ①~⑥, 9절) | 그림과 `FOG` 줄·`_light.png` 를 같이 본다. ① **보이는 뒷벽 윗부분이 검게도 어둡게도 잘리지 않음**: `_light.png` 에서 그 벽 그림 자리의 빛 값 = `--reveal` 실행의 같은 자리 값(±2/255 — 벽이 시야 원 R − 40 안이면 V = 1 이라 같아야 한다. 벽 그림 색과 무관) ② 문 틈 밖 입장 중 궁수는 보이고 둘레 벽 뒤 맵 밖 적은 안 보임(`enemies_hidden` = 1) ③ 기둥(`x`) 뒤 적이 보임 ④ 머리가 R 에 걸친 적이 검게 잘리지 않음, 흐림 띠 적 둘레에 **밝은 바닥 실루엣이 없음**(`_light.png` 에서 그 적 모양 안 값 ≤ 둘레 땅 + 안개 알파 × (m − 둘레) + 2/255), 닿은 거리 안 적은 시야와 상관없이 보임 ⑤ **앞 턱 바깥 줄·모서리가 검게 잘리지 않음**(바깥 줄 그림 자리 빛 값 = `--reveal` 실행의 같은 자리 값, ±2/255) ⑥ **어두운 그림 픽셀도 하한**: `_light.png` 에서 적·시체·보석 모양 안 값 ≥ m(0.6, −2/255) — 그림의 어두운 픽셀 자리 포함. `--reveal` 캡처는 안개 없음·`enemies_hidden=0`, 두 실행의 `FOG` 줄 `in_sight`·`explored` 같음 |
| 안개 흐름 (v0.5.1) | `--room 1:1 --autoplay --god --seed 7 --check --log-ticks <csv> --capture-at 1,20,60`(v0.7 — 1번 방 1:00 안) 과 같은 명령에 `--reveal` 을 더한 실행 + `--debug-nav sight` 한 장 | 1초: 방 가운데 원만 보이고 둘레가 검정. 20·60초: 지나온 자리가 흐린 기억(바닥·벽만, 적·시체·보석 없음), 보이는 곳 경계가 칸 계단 없이 부드러움, 예고·적 투사체·적 장판이 안개 위에 보임. 숨김이 맞는지는 그림 대신 `FOG` 줄로 대조(그린 적 + 숨긴 적 = `--reveal` 실행의 그린 적). `--log-ticks` 의 공정성 열 0·`kite_switches` 기준(10.1). `--reveal` 캡처는 안개 없음 |
| 안개 재현 | 위 두 실행(`--reveal` 유무)의 `--log-hash`·`AUDIO` 줄 | CSV 같음, `AUDIO` 수 같음(안개는 그리기만 바꾼다) |
| 미니맵 기억 (v0.5.1, WP6·단계 2) | "안개 흐름" 과 같은 명령, `--capture-at 20,60` | 미니맵이 가 본 곳만(앞 턱 바깥 줄 포함 — 두 줄 벽 규칙), `--reveal` 이면 전체. 점 규칙(8.2) |
| 안쪽 방 (v0.6) | `--preview tiles --map cathedral_arena` + `--autoplay --autoplay-visit rooms --god --seed 7 --check --log-ticks <csv> --capture-at 20,60,120`(v0.7: 판 처음부터 — 단계 1 의 방은 모두 이 맵. 단계 2 부터는 `--room 1:9`) + `--debug-nav entries,tiles` 한 장 | 피 방·창살 방·짧은 벽이 원작처럼 맞물리고 둘레에 겹친 바깥 벽에 틈·겹침 그림 없음, 방 문 틈 끝이 벽 끝 기둥으로 마무리, 성질 겹침의 창살 칸이 걷기만 색, 방 안·벽 뒤가 안개로 가려지고 창살 너머는 보임, 자동 조작이 방 안까지 들어갔다 나옴(v0.6.1: 캡처가 아니라 로그 `VISIT` 줄로 판정 — 120초 안에 모든 `room_*` 가 한 번 이상, `VISIT_SKIP` 이 있으면 그 방 이름을 적음), `--log-ticks` 의 공정성 세 열 0·`occluded` > 0·`stuck`·`in_obstacle` 기준 통과 |
| 헬파이어 없음 (v0.6) | 도구 `import --skip-bundles all --out <저장소 밖 임시 폴더>`(원작 exe PNG 는 지금 `Assets` 뿌리에서 그 폴더로 복사) → 그 폴더로 빌드한 exe(`-p:DdrAssetDir=<그 폴더> -o <임시>`)로 `--autoplay --stage stage_cathedral --quit-after 30` 과 `--capture-at 1 --quit-after 2` | 가져오기 종료 코드 0·`import.log` 에 묶음 넷 "건너뜀", 게임 종료 코드 0(꺼짐만 있으면 안내 화면 아님), `ASSETS … skipped=hellfire,hfmonk,hfmusic,hfvoice disabled=`(MVP 는 꺼질 정의 없음), 같은 씨앗의 묶음 있는 실행과 `--log-hash` CSV 같음. 헬파이어 정의가 생긴 뒤에는 타이틀 한 줄·꺼진 카드 캡처를 더함 |
| 혼합 비교 | `--preview blend` | 무기마다 보통/가산 두 칸이 나란히 보임 — 판정은 기획 담당(열린 질문 10) |
| 길찾기 | `--room 2:11 --stress 180 --debug-nav flow,shapes --god --check --capture-at 5,20`(v0.7 — 협로는 보스 방 `catacombs_arena`, 단계 3. 작은 방은 `--room 2:2 --stress 120` 한 번 더 — 문 넷·짧은 벽 둘레) | 앞쪽 협로로 몰리는 흐름, 큰 유닛 우회, 로그의 `stuck`·`in_obstacle` 기준(10.1) 통과 |
| 전투·손맛 | `--room 1:4 --start-at 00:30 --level 15 --weapons …` | 투사체(원작 마법 그림·16방향)·폭발·피해 숫자·번쩍임이 보임 |
| 레벨업 창 | `--capture-on levelup` | 카드 3장 문구가 `피해 14 → 18` 꼴, 기획 12.2 배치, 무기 아이콘이 원작 마법 프레임 |
| 보스 | 기획 15절 예 | 패턴 예고·보스 막대·페이즈 |
| 정예 방 (v0.7 — v0.5.3 의 "중간 보스 겹침" 을 바꿈: 정예 방이 따로라 겹침이 없다, WP6·단계 2·3) | `--room 1:5 --autoplay --god --level 12 --capture-at 20,170`(170 ≈ 방 시계 2:30 뒤 — 생성 멈춤) + 단계 3 `--room 2:9`. `--preview enemies` | 중간 보스 막대 한 줄, 2:30 뒤 일반 생성 없음(`--log-ticks` 의 적 수가 줄기만 함), 처치 뒤 정리 → 보상 = 정예 상자 + 덤 보상(v0.8 — v0.7 은 상자만) → 출구 문 둘(셋)에 다음 방 표시와 덤 아이콘(v0.8 — v0.7 은 하나). 슬러지 남작 `bsm`·데스스핏 `bfds`·블랙애시 `bashtb` 가 같은 묶음 일반 적과 색으로 구별됨(기획 열린 질문 16) |
| 방 흐름 (v0.7, WP6) | `--autoplay --god --seed 3 --capture-on doors --capture-on reward --log <로그>` 와 `--room 1:2 --capture-at 3,70` | 들어올 때 주인공이 앞 문 진입점·방 문구, 생존 중 HUD 남은 시간·방 줄, 정리 중 `남은 적 n`·미니맵 붉은 점(v0.8: 정리 시작부터 모두), 보상 물체가 주인공 앞에(v0.8 — v0.7 은 `center`), 층이 바뀌는 문에 `▼ n층`(v0.8), 마법서 창 제목·카드가 무기만, 문 봉인이 걷히고 문 위 아이콘·이름, 다른 문 틈은 붉은 막. 로그 `ROOM` 줄이 방마다 하나·`DOOR_SKIP` 0, 방 이동 프레임에 `slow_frames` 가 기준 안(R33). v0.7.1: 방 이동 뒤 첫 캡처에 옛 방의 시체·피해 숫자·이펙트가 없음(`IWorldBound`, 7.11), 결과 화면 걸린 시간 = `ROOM` 줄 방 시간 합(마지막 방 포함 — `Conclude`) |
| 결과 | `--capture-on result --exit-on-result` | 무기별 피해 표, v0.7: 도달한 곳·걸린 시간·방 줄, `RESULT` 로그 줄(`depth=`·`run_time=`) |
| 소리 고르기 | `--sound --autoplay --god --room 1:6 --start-at 01:00 --quit-after 60 --seed 3` (null 장치 — 스피커로 나지 않음, 동의 필요 없음) | 오류 없이 끝남, `AUDIO device="No Output"`, `AUDIO` 줄의 `played` 가 초당 기준 안(전체 24 동시), 같은 씨앗의 `--mute` 실행과 `requested`·`played`·`dropped_*` 가 같음 |
| 재현 | 같은 명령 두 번(한 번은 `--time-scale 8`). 그리고 같은 명령을 `--mute` 와 null 장치 `--sound` 로 한 번씩 | 두 `--log-hash` CSV 같음, 같은 `--capture-at` PNG 바이트 같음(안개 0.15s 바뀜도 스테이지 틱이라 같음), `--mute` 와 `--sound` 의 `--log-hash` CSV 같음 |
| 실제 스피커 | `--sound-device default …`(**사용자 동의를 받고만**) | 판정 못 함(귀로 들은 확인은 사용자 몫) |
| 거울판 (v0.8, WP5·WP6) | `--preview tiles --map cathedral_room_a` 와 `--map cathedral_room_a@m`(단계 2), `--autoplay --god --seed 3 --log <로그>` | 거울판 그림이 원판의 좌우 뒤집힌 배치로 맞물림(벽·벽 끝·창살·닫힌 문 짝 조각), 성질 겹침이 그림과 맞음. `ROOM` 줄에 `mirror=1` 이 나오고 바로 앞 방과 같은 (맵, 방향) 이 없음 |
| AI 확대 (v0.8, WP0·WP5) | 도구 `import --upscale realesrgan-x4plus --out <저장소 밖 임시 폴더>` 로 만든 자원으로(위 "헬파이어 없음" 과 같은 방법), `--preview tiles --map cathedral_arena`·`--preview enemies`·`--room 1:9 --autoplay --god --capture-at 5,20` + 같은 씨앗 원본 자원 실행 | 종료 코드 0(또는 되돌아가면 6 과 경고 줄), `ASSETS … upscale=done art_scale=2`, 기획 19.6 확인 항목 ①~⑦ 판정(못 하면 "판정 못 함"): ① 바닥·벽 질감·색, ② 바닥 조각·벽 기둥 이음매, ③ 투명 가장자리(계단·검은 테·후광), ④ 그림자가 몸과 붙고 확대 안 한 그림과 진하기가 같음, **⑤ 걷기 프레임 떨림**(`--preview enemies` 의 반복 재생 캡처 여러 장 — 정지 칸에서 몸 윤곽이 프레임마다 흔들리는지), ⑥ 확대 배경 위 원본 캐릭터가 떠 보이는지, ⑦ 빛·안개·벽 가림과 겹친 모습. 두 실행의 `--log-hash` CSV 같음(시뮬레이션은 그림 배율을 모름). ⑧ 되돌아감은 WP0 완료 기준 |
| 계단 저장 (v0.8, WP6·WP7·단계 3) | `--save-dir <임시 폴더> --room 1:9 --autoplay --god --seed 3 --capture-on doors`, 캡처 뒤 일시정지 화면 캡처 | 보스 방 문 단계의 일시정지 메뉴에만 `저장하고 끝내기` 가 있음(다른 방 문 단계·생존 단계에는 없음). 저장·이어 하기 왕복과 "끊지 않은 판과 판 해시 같음" 은 자원 없는 시험(10.1 `Run`·`Save`)이 보고, 사람이 메뉴로 끊고 잇는 확인은 "동작 확인 못 함" 으로 따로 적는다 |
| 단계 끝 | `--autoplay --god --seed 1..5 --exit-on-result`(v0.7 — `--stage` 없이 판 처음부터) | 단계 1: 다섯 번 모두 `RESULT outcome=victory depth=1:8`(v0.7.1 — 방 번호가 기획 번호라 기획 14.1 "8번 방 정리 = 결과" 와 같은 값. 일반 방 여섯 — `slice` 판의 마지막 방 정리), `ROOM` 여섯 줄(`room=1,2,3,4,6,8`)·`DOOR_SKIP`·`REWARD_STALL` 0·`collapsed=1` 은 적어 둠. 단계 2: `outcome=victory depth=1:9`, `run_time ≤ 20:00`(기획 2.5 약 15분). 단계 3: `depth=2:11`, `run_time ≤ 45:00`(약 35분), 계단 앞뒤 `ROOM` 줄의 `level=` 이 이어짐(빌드 넘기기) |

### 10.3 성능 측정

- `TickProfiler`(Core): 시스템 단계별 `Stopwatch` 누적. `--log-ticks perf.csv` 는 1초마다 한 줄:
  `app_s, stage_s, enemies, hero_proj, enemy_proj, hazards, tick_avg_ms, tick_max_ms, <단계별 평균 ms…>, draw_ms, fog_ms, frame_ms, slow_frames, draw_calls, alloc_bytes, dropped_ticks, <--check 열…>`
  (`fog_ms` = `FogRenderer` 와 7.7 ④ 의 CPU 시간, v0.5.1).
  `draw_ms` = `Draw` 시작부터 `Present` 직전까지 CPU 시간, `slow_frames` = 16.7ms 넘은 프레임 수. 종료 때 첫 5초를 뺀 p50/p95/p99/max 를 로그에 요약한다. `draw_calls` 는 `GraphicsDevice.Metrics.DrawCount`.
- 렌더 측정: `--stage stage_catacombs --stress 300 --freeze-ai --vsync off --window 1280x720 --quit-after 30 --log-ticks perf.csv`. 결과에 "백그라운드 창에서 잼" 을 적는다.
  단계 1 에는 스테이지 2 맵이 없으므로 `--stage stage_cathedral` 로 같은 측정을 한다. `draw_calls` p99 ≤ 600(4.9 표)은 **단계 1 완료 조건**이다(WP5).
  v0.7: `--stress` 는 `--room` 이 없으면 그 액트 보스 방(큰 방)에서 돌므로(9절) 위 명령은 v0.6 과 같은 맵을 잰다. 작은 방은 상자가 작아 같은 수의 적이 더 빽빽하다 — 단계 2 에 `--room 1:2 --stress 300` 을 한 번 더 재 4.9 와 견준다.
- **방 이동 시간**(v0.7, R33): `RunSession.Advance` 한 번(짐 챙기기 + `World` 생성 + 시뮬레이션 설치 = 배치 풀기·충돌 도형·clearance·첫 필드)을 `Perf` 시험으로 잰다 — 큰 방·작은 방 각각 ≤ 150ms(0.5s 어두워짐의 가운데에서 한 프레임이 길어져도 덮개 1 이라 보이지 않는 범위). 게임에서는 그 프레임이 `slow_frames` 에 하나로 잡히는 것만 허용한다.
- 안개(v0.5.1): 안개를 켠 `--stress` 장면은 시야 밖 적을 그리지 않아 오히려 가벼워 최악이 재지지 않는다. 그래서 같은 명령을 `--reveal`(모든 적을 그림, 안개 단계 없음)과 안개 켬으로 두 번 재고,
  **최악 추정 = `--reveal` 실행의 p99 + 안개 켬 실행의 `fog_ms` p99**, 호출 수도 `--reveal` 의 `draw_calls` + 안개 켬과 `--reveal` 의 차이로 4.9 기준과 비교한다.
- 그래픽 없는 기준 측정과 합격 기준은 4.9 표 하나다.

---

## 11. 구현 작업 묶음 (Work Package)

- 순서: **WP1(끝남, 커밋 `ec851d6`) → WP1b(계약 갱신, 끝남, 커밋 `02c3b8b`) → WP1c(시야 계약 갱신, 끝남, 커밋 `ceddc36` — v0.5) → WP1d-형(계약 보탬의 모양·빈 본문 — v0.5.3 중간 보스 + v0.6 헬파이어 묶음·직업·안쪽 방 + v0.6.3 방 바닥·방 보급, 짧게 — **형·구현 끝남**, 커밋은 `git log --grep WP1d`) → WP0a(최소 실제 데이터, 짧게 — **끝남**, 커밋 `d2b15c1`) → **WP1e-형**(v0.7 방 구조 계약, 혼자, 짧게 — v0.7.2: 형·구현-1·구현-2 **끝남**, 커밋은 `git log --grep WP1e`) → **WP1f**(v0.8 — 기획 v0.8.1 29절과 사용자 결정 U 의 계약, 혼자, 형과 순수 로직 구현까지 한 번에 — v0.8.2: **끝남**, 커밋은 `git log --grep WP1f`, 다르게 정한 것 13.22) → WP0·WP2~WP8 이 서로 파일이 겹치지 않고 동시에**. v0.6.1: **WP1d-구현**(순수 로직)은 WP1d-형 뒤 WP0a·WP2~WP8 과 동시에 간다 — 그것을 기다리는 완료 기준만 11.1 에 적었다. 통합(실제 데이터 엄격 시험, `PlayScene` 완성, 단계 끝 실행)은 WP6 이 맡는다.
  v0.7: 사용자 결정 R 이 이미 커밋된 형(`StageDef.Map`·`Unlock`·`Victory`, `RunResult`·`StageRecord`·`IsUnlocked`, `SimPhase` 순서)을 바꾸므로 WP2~WP8 이 시작하기 **전에** WP1e-형을 낸다(기획 28절 권고 — 11.3 의 "모아서 다음 갱신" 을 기다리지 않음, WP1c·WP1d 와 같은 이유).
  **WP1e-구현**(판 흐름·방 흐름·방 이벤트·문 틈·넓이·`RunSession` 의 순수 로직)은 WP1e-형 뒤 WP0·WP2~WP8 과 동시에 간다(WP1d 와 같은 나눔).
  MVP 다음에 **WP9(직업 고르기)**·**WP10(헬파이어 스테이지)** 이 온다(v0.6 — 기획 14.2 이후 확장 1·2). 둘 다 계약을 고치지 않고 데이터·씬·패턴만 더하는 것이 WP1d 의 목표다.
- **자원 파일이 없어도 Core 시험은 모두 돈다**(실제 데이터 검사·길 검사·시나리오·성능 포함, 4.2·6.14). 자원이 필요한 것은 게임 실행 확인(캡처)과 자원 대조 검사(13.5-7 실제 쪽)뿐이다.
  다만 실제 맵 길 검사·단계 1 시나리오가 쓰는 **데이터**는 세 묶음에 나뉘어 있고(타일 번호·성질 = 원본 SOL 을 봐야 채움, 맵 배치, 그림 묶음의 프레임 수), 그중 원본으로 확인해야 하는 값이 있다.
  그래서 그 최소분을 **WP0a** 가 먼저 저장소에 넣는다. WP0a 뒤에는 WP0(가져오기 도구 본체)이 늦어도 WP2·WP3·WP4·WP7·WP8 의 Core 쪽은 막히지 않는다. WP5·WP6 의 화면 확인은 WP0 의 단계별 출력을 기다린다.
- WP0 의 맵 굽기는 WP7 로더를 기다리지 않는다 — 도구는 WP1b 의 `MapDataReader` 로 맵 정의를 읽는다(1.4). 13.5-10 의 "정의 → 이름" 쪽만 WP7 로더 뒤(`Integration`)다.
- 각 묶음은 기획 14.1 의 **단계 1(세로 조각)** 범위를 먼저 끝내고, 단계 2·3 내용을 같은 묶음에서 이어 간다. 완료 기준은 단계 1 기준이며, 단계 2·3 은 같은 기준에 내용만 늘린다.
  소리(WP8)는 기획 14.1 대로 단계 2 부터다.
- 맡는 파일은 "맡는 것" 이 전부다. 다른 묶음의 파일을 고쳐야 하면 그 묶음 담당에게 요청한다. 계약(WP1·WP1b 파일) 변경은 11.3.

```
WP1 (끝남) ── WP1b (끝남) ── WP1c (끝남) ── WP1d (끝남) ── WP0a (끝남) ── WP1e (끝남)
  ── WP1f(v0.8 계약, 끝남) ──────────────┬── WP0 가져오기 도구·가져오기 표·타일셋(+ v0.8 AI 확대) ┐   (자원: 게임 화면 확인에 필요)
                                 ├── WP2 내비·충돌(타일 충돌 포함)·공간·시야 ─────┤
                                 ├── WP3 주인공·전투·무기·성장 ───────────────────┤
                                 ├── WP4 적·감독·자동 조작 ────────────────────────┼── 통합(WP6) ── 검증
                                 ├── WP5 렌더(아틀라스·타일 아레나·안개)·미리보기 ┤
                                 ├── WP6 화면·UI·입력 ─────────────────────────────┤
                                 ├── WP7 앱 셸·데이터 로더·개발 도구·자원 검사 ────┤
                                 └── WP8 소리 ─────────────────────────────────────┘
                                                                MVP 뒤 ── WP9 직업 고르기 ── WP10 헬파이어 스테이지
(v0.9) 단계 1 통합 뒤: 버그 수정·방 맵 합침 ── WP1g(v0.9 계약 — 장비·상인·물약 칸·회복 문 + 단계 1 요청) ── 단계 2(WP0·WP3~WP8 의 v0.9 줄 동시) ── 통합 ── 검증
```

### 11.1 묶음별 맡는 것·계약·완료 기준

#### WP1 — 솔루션 뼈대·공통 계약 (끝남, 커밋 `ec851d6`)

- 한 것: sln·`global.json`·`Directory.Build.props`·세 csproj(DDR0001·복사·xunit.v3), Core `Common/`·`Defs/`·`Data/Registry/`·`Sim/Contracts/`·`Sim/Entities/`·`World`·`Simulation`·`StageStepper`·`GameModules`·`BeginTick`/`EndTick`,
  게임 `App/Contracts/`, 테스트 `TestKit/`·`Common/`, 그리고 각 묶음의 **빈 모듈·연결 파일**:
  `Nav/NavModule.cs`, `Nav/PlacementResolver.cs`, `Nav/ResolvedObstacles.cs`(`INavDebugView`) (→ WP2), `Sim/Weapons/CombatModule.cs`, `Sim/Combat/DamageMath.cs` (→ WP3),
  `Sim/Enemies/EnemyModule.cs`, `Sim/Autoplay/AutoplayPolicy.cs` (→ WP4), `Art/ArtModule.cs`, 게임 `Rendering/RenderServices.cs`·`WorldRenderer.cs`·`FxSystem.cs`·`UiCanvas.cs`·`Scenes/PreviewScene.cs` (→ WP5),
  `Scenes/SceneFactory.cs` (→ WP6), `Data/Loading/LoadingModule.cs` (→ WP7). 만든 뒤 소유는 해당 묶음이다.
- 문서와 다르게 정한 것은 11.4 에 옮겼다(코드가 정본).

#### WP1b — 계약 갱신 (끝남, 커밋 `02c3b8b`)

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
| 10 | 새 `Assets/AssetFormat.cs`·`AssetManifest.cs`·`ImportTable.cs`·`SpriteMeta.cs`·`ArenaArt.cs`·`SoundIndex.cs` | 판 번호, manifest 모양(표 해시·파일 크기), **가져오기 표 8개의 모양(1.4 예 전부 — 동작별 `width`·`shadow_alpha` 덮어쓰기, 아이콘 `layers`, 소리 `vars`, `optional`, `tilesets.json`)과 "만들 이름 목록"**, 아틀라스 메타(페이지 여럿·동작별) 읽기·검사, `arena.json` record(`cells` frame 1부터)·바닥 조각 이름 규칙 `FloorChunks`, 소리 색인 — 구현과 시험까지 | 도구(쓰기)와 게임·시험(읽기)의 약속. 표 모양을 WP1b 가 다 정해야 WP0 이 곧바로 형을 바꾸러 오지 않는다(다음 갱신 묶음) | WP0·WP5·WP7·WP8 |
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

#### WP1c — 시야 계약 갱신 (v0.5, 끝남, 커밋 `ceddc36`)

기획 v0.6 의 전장의 안개(사용자 요구 11)가 바꾸는 **계약**과, 여러 묶음이 같이 쓰는 **순수 로직**(`VisibilityField`·`FogMath`)을 한 묶음에 모은다.
시야는 원거리 적(WP4)·생성(WP4)·그리기(WP5)·미니맵(WP6)·해시(WP7)가 다 읽으므로 WP2~WP8 이 시작하기 전에 형과 동작이 서 있어야 한다.
그래픽·자원·실제 `Data/` 와 무관한 순수 로직이라 **구현과 시험까지** WP1c 에서 끝낸다(WP1b #9·#10 과 같은 방식). WP2 에 미루면 WP4 의 시야 조건 시험이 WP2 를 기다린다. 실제 `Data/` 는 쓰지 않는다(11.3).

| # | 파일·형 | 바꾸는 것 | 이유 | 영향 |
|---|---|---|---|---|
| 1 | `Map/ArenaGrid.cs` | `CellFlags.BlocksSight`(새 비트 16 — 1·2·4·8 은 그대로), `BlocksSight(tx, ty)`, 격자 밖(`Outside`)은 시야를 막지 않음(`OutsideFlags` 그대로), `PropChar` 8글자 | 시야 막힘 칸(4.11·6.11) | WP2·WP5 |
| 2 | `Map/ArenaLayout.cs` | `PropChars = ".wLWmxMX"`, `TryParseProps` 8글자, `sol_known_diffs`·`design_diffs` 의 `sol`·`use` 8글자 검사, 오류 문구. `LayoutHash` 식은 그대로(성질 글자를 이어 시야 글자도 들어감) | 성질 글자(6.11) | WP0·WP5 |
| 3 | `Defs/RulesDefs.cs` | `RulesDef.Sight`(`SightRulesDef?` — `Edge`·`MemoryRatio`·`Fade`·`RefreshTicks`·`ViewInset`·`TouchMargin`·`RangedMargin`, 모두 `required`), 머리 주석의 파일 소유에 `rules/sight.json` | 안개 값 한 곳(6.10) | WP4·WP5·WP7 |
| 4 | 새 `Sim/Visibility/VisibilityField.cs`·`FogMath.cs` | 4.11 의 격자·선 걷기·두 줄 벽·바뀜 값 잇기·갱신·질의(`ClearReach` 포함)·가 본 칸(`ExploredHash`), `FogMath` 함수와 그리기용 오버로드 — **구현과 시험까지** | 시야는 시뮬레이션 상태(4.11) | WP2(`VisibilityField` 소유)·WP5(`FogMath` 소유)·WP3·WP4·WP6·WP7 |
| 5 | `Sim/World.cs` | `World.Visibility`(`VisibilityField`, 늘 있음 — 꺼지면 `Enabled = false`, 생성자는 계산하지 않음), `World.Fairness`(`FairnessCounters`: `UnseenShots`·`UnseenCharges`·`HiddenHits`·`KiteSwitches`, v0.5.1) | 4.11 "시작", 10.1 공정성 열 | 모두 |
| 6 | `Sim/Core/BeginTickSystem.cs` | `ViewRect` 다음에 `world.Visibility.Update(world)`(첫 틱이 첫 계산). 머리 주석의 "시야 갱신" 이 이것이 됨 | 3.5 1단계 | — |
| 7 | 게임 `App/Contracts/AppServices.cs` | `AppOptions.Reveal`(bool), `DebugNav` 주석에 `sight`, `AppOptions.Preview` 값에 `fog` | `--reveal`·`--debug-nav sight`·`--preview fog`(9절) — 파싱은 WP7 | WP5·WP6·WP7 |
| 8 | `TestKit/TestDefs.cs`·`TestWorldBuilder.cs` | 합성 타일셋 글자 바꿈(기둥 `x...`, 걷기만 `LowWall = "ww.."`, 투사체만 `Glass = "mm.."`) + **걷기·시야 `W` 조각과 시야·투사체 `M` 조각을 따로**(두 뜻을 시험이 구별) + 시야만 막는 조각(`L`)·시야 막는 안쪽 벽(`X` 줄)·두 줄 벽(`XXXX`) 하나씩, `TestDefs.Sight`(6.10 값), 시야 켬/끔 월드·빛 반경 바꾸기 | 묶음이 시야를 실제 데이터 없이 시험 | 모두 |
| 9 | 기존 시험 `Map/`·`Assets/` | 글자 기대값(`MapDataReaderTests` 기둥 `x...`, `ArenaLayoutTests` 의 고장 글자·`LayoutHash` 시야 글자 경우), 주석의 글자. **`Prop` 글자가 아니라 `CellFlags` 값으로 확인하게 바꿈**(`ArenaLayoutTests` 131·134줄: `LowWall` = `BlocksWalk` 만, `Glass` = `BlocksProjectiles` 만 — v0.4 글자가 남으면 실패하게). 8글자 → 플래그 표 전체 왕복(`TryParseProps` ↔ `PropChar`) 시험 | #1·#2, 6.11 "주의" | — |
| 10 | `Sim/Entities/Enemy.cs`·`EnemyVisual` | `Enemy.TouchReach`(float)·`Enemy.BodyTop`(float) — 쓰기 WP4(생성 때), 읽기 `FogMath`·`SeenClearly`. `EnemyVisual.ForceShown`(bool) | 4.11·6.6·7.10 (v0.5.1) | WP4·WP5 |
| 11 | `Defs/SpriteDefs.cs` | `SpriteSetDef.BodyHeight`(`body_height`, 필수 — 기존 시험 픽스처의 그림 묶음에 값 채움) | 또렷이 보임의 몸 높이(6.3, 기획 v0.6.1) | WP4·WP5·WP7 |

- 하지 않는 것: `EnemyCtx.SeenClearly`·부품 시야 조건·`kite` 띠 당기기(WP4 소유, 6.6), 공정성 수 세기(WP3·WP4), `FogRenderer`·`--preview fog`(WP5), 미니맵 가림(WP6), `--reveal` 파싱·`StateHasher` 의 `ExploredHash`·`rules.sight` 없음 오류(WP7), 가져오기 도구의 세 비트 대조(WP0). 각 묶음 완료 기준에 넣었다.
- **완료 기준**: `dotnet build -p:SkipAssetCheck=true` 통과, `dotnet test` 통과. 새 시험: 10.1 `Visibility` 행 전부(선 걷기 = 기준 구현, 꼭짓점 규칙, 두 줄 벽, 맵 밖 문 틈, 다시 계산 시점·반경은 다음 틱, 첫 계산은 첫 `BeginTick`, 가 본 칸·`ExploredHash`, 또렷이 보임 경계(몸 위 끝)·`ClearReach`, `Enabled`, 바뀜 값 잇기, 할당 0)와
  10.1 `Map` 의 8글자·`BlocksSight`·시야 글자만 바뀐 `LayoutHash`·8글자 왕복. **`Perf` 는 따로 돌려 값을 적는다**: `DDR_RUN=Perf dotnet test --filter Category=Perf`(분류 규칙상 옵션 없는 `dotnet test` 는 건너뜀, 1.3) → 다시 계산 1회 ≤ 0.1ms 와 잰 값을 완료 보고에 적는다.
  **옛 글자 점검**: `rg -n "\"[.WMX]{4}\"|'W'|'M'" src tests tools Data` 로 저장소의 `W`·`M` 성질 글자를 모두 찾아 줄마다 새 뜻(걷기 + 시야 / 시야 + 투사체)이 맞는지 확인한 결과를 완료 보고에 적는다(6.11 "주의").
  이 문서(4.11 코드 조각, 6.6 `Enemy` 새 필드, 6.10 `rules.sight`, 6.11 글자 표, 9절 `--reveal`·`--preview fog`)의 시그니처·모양이 계약 파일에 빠짐없이 있다. `git diff` 에 실제 `Data/` 변경 없음.

#### WP1d — 계약 보탬: 중간 보스(v0.5.3) + 헬파이어 묶음·직업·안쪽 방(v0.6) — v0.6.1: **WP1d-형**(WP1c 뒤·WP2~WP8 앞, 혼자, 짧게) + **WP1d-구현**(WP1d-형 뒤, WP0a·WP2~WP8 과 동시) — v0.6.3: 형·구현 끝남

기획 v0.6.2 의 중간 보스 늘리기(9.4, #1~#3)와 기획 v0.7 의 사용자 결정 H·K·M(#4~#20)이 바꾸는 **계약**을 한 묶음에 모은다. 아직 구현 전이라 v0.6 에 합쳤다(기획 26절 권고) —
WP2~WP8 이 시작하기 전에 들어가야 "직업을 더해도 계약을 다시 고치지 않는다"(결정 K)가 선다. WP2~WP8 이 시작하기 전이라 11.3 의 "모아서 다음 갱신" 을 기다리지 않고 메인 세션이 바로 낸다.
실제 `Data/` 는 쓰지 않는다. #1~#3 은 기본값이 있는 필드 더하기다.

**둘로 나눈다**(v0.6.1 — v0.6 의 "혼자, WP2~WP8 앞" 에 17항목과 순수 로직 구현까지 얹으면 주 경로가 길어진다):
- **WP1d-형**(짧게, WP2~WP8 앞): 아래 표의 형·시그니처·JSON 모양 전부와 컴파일. 순수 로직 본문은 비워 둔다(`throw new NotImplementedException("WP1d-구현 …")`, 배치 풀기의 `grid` 놓기는 `LayoutError` "WP1d-구현 전").
  형을 읽는 지금 코드는 `World.cs`(33줄 `HeroRulesDef.Radius` — #10 이 지우고 #12 가 같이 고침)와 `ArenaLayout.cs`(155줄 `place.Piece` — #14 가 nullable 로 바꾸고 #15 가 같이 고침)뿐이다(v0.6.1 저장소 확인 — v0.6 의 "그 형을 쓰는 구현이 없다" 는 틀린 문장이었다).
  그 밖에 #5(`SoundBindingsDef.Hero`)·#6(`optional` → `bundle`)은 시험 픽스처만 고친다.
- **WP1d-구현**(WP1d-형 뒤, WP0a·WP2~WP8 과 동시): 여러 묶음이 읽고 자원·그래픽과 무관한 순수 로직을 WP1b·WP1c 처럼 **구현과 시험까지** — #6 `OutputBundles(arenas)`·표 검사, #8 `ContentAvailability`, #15 `grid`·`over_border`(막힘 줄어듦 포함)·`ValidateRooms`(13.5-19 + 하위 칸 검사), #19 `StageReach`, #17 합성 맵·정의.
  맡는 파일(`Map/ArenaLayout.cs`, `Assets/ImportTable.cs`·`ContentAvailability.cs`, `Data/Registry/StageReach.cs`, `TestKit/`)은 WP2~WP8 이 쓰지 않는다. **기다리는 것**: WP0a 완료 기준(13.5-19), WP2 의 방 맵 시험(#17 합성 방 맵), WP4 의 13.5-15 검사기(`StageReach`), WP7 의 시작 검사·13.5-20(`ContentAvailability`·`OutputBundles`·`StageReach`) — 각 묶음은 그 시험을 WP1d-구현 뒤에 켠다.
  v0.6.3: WP1d-형·WP1d-구현은 끝났다(#21 의 방 바닥 `RoomFloors` 포함). 위 기다리는 시험을 모두 켜도 된다 — 따로 알림을 기다리지 않는다.

| # | 파일·형 | 바꾸는 것 | 이유 | 영향 |
|---|---|---|---|---|
| 1 | `Defs/EnemyDefs.cs` `PatternDef` | `bool ClearSight`(`clear_sight`, 기본 `false`), 주석 "참이면 시작·이벤트 프레임에 `SeenClearly`(6.6)" | 패턴 조건(6.5). 로더가 모르는 속성을 오류로 내므로 형에 있어야 데이터에 적을 수 있다 | WP4·WP7 |
| 2 | `Sim/Contracts/Queries.cs` `EnemyProjectileSpawn` | 끝에 `bool SightGated = false` — "시야 조건을 지켜야 하는 발사(`shoot_projectile`, `clear_sight` 패턴)" | 공정성 `unseen_shots` 가 `clear_sight` 패턴 투사체를 보스 제외에서 빼고 셈(10.1) | WP3(셈)·WP4(채움) |
| 3 | `Sim/Contracts/WorldState.cs` `FairnessCounters.UnseenShots` 주석 | "(보스·중간 보스 제외)" → "(보스·중간 보스 제외, 단 `SightGated` 발사는 셈)" | 2 와 맞춤 | — |
| 4 | 새 `Defs/ContentBundle.cs` | `enum ContentBundle { Hellfire, Hfmonk, Hfmusic, Hfvoice }`(JSON 소문자), `ContentBundles.MpqName` | 묶음 이름 한 곳(H, 1.4·6.17) | WP0·WP6·WP7·WP8 |
| 5 | `Defs/EnemyDefs.cs`·`StageDefs.cs`·`WeaponDefs.cs`·`AudioDefs.cs` | `EnemyDef.Requires`·`StageDef.Requires`·`WeaponDef.Requires`·`MusicDef.Requires`(`IReadOnlyList<ContentBundle>`, 기본 빈), `MusicDef.Fallback`(`DefRef<MusicDef>?`), `WeaponDef.Classes`(`IReadOnlyList<DefRef<HeroClassDef>>`, 기본 빈 = 모두), `WeaponDef.CastClip` 주석에 `attack`(근접), **`SoundBindingsDef.Hero` → `IReadOnlyDictionary<string, HeroSoundsDef>`(키 = 직업 ID)** | 정의의 묶음 조건(6.17), 직업 전용 무기·근접 시전 클립(6.8), 직업 소리(6.15) | WP3·WP4·WP7·WP8 |
| 6 | `Assets/ImportTable.cs` | 모든 항목 종류의 `Optional` → **`Bundle`**(`ContentBundle?`, 타일셋 항목에도), 소리·음악 항목 `Prefer`(`IReadOnlyList<ContentBundle>`), 표 검사(`bundle: hfvoice` 금지, `prefer` 는 소리·음악만), **`OutputBundles(IEnumerable<ArenaGrid> arenas)`**(이름 → 묶음, 기본 null — v0.6.1: 맵 그림 이름(`ArenaArt.OutputNames(grid)`)은 `grid.Tileset.Id` 의 타일셋 항목 `bundle` 로. 인자 없는 v0.6 판은 두지 않음), 타일셋 항목 `AuditDun`(`audit_dun`, 원작 경로 목록 — v0.6.1, 13.5-22) — 구현과 시험 WP1d-구현 | 가져오기 표 묶음(1.4), 시작 검사 "꺼짐"(1.4 끝), 13.5-10 묶음판 | WP0·WP7 |
| 7 | `Assets/AssetManifest.cs`·`AssetFormat.cs` | `ManifestSource.Bundle`(`ContentBundle?`), `AssetManifest.SkippedBundles`(`ManifestSkippedBundle(Bundle, Mpq, Reason)` 목록 — `Reason` 은 1.4 의 넷 중 하나, `판 다름` 이면 빠진 경로 목록 포함), `ManifestFile.Bundle`·**`Sources`**(v0.6.1: MPQ 이름 목록, 서수 정렬 — v0.6 의 `Source` 하나를 대신함). `AssetFormat.Version` +1(manifest 모양 바뀜) | 도구(쓰기)와 시작 검사(읽기)의 약속(1.4) | WP0·WP7 |
| 8 | 새 `Assets/ContentAvailability.cs` | 6.17 의 형 전부(v0.6.1: `Disabled(defs) → DisabledContent`·`Skipped(b)`, v0.6 의 `DisabledInDefs` 없앰) — 구현과 시험 WP1d-구현 | 꺼진 스테이지·직업 판정을 WP6·WP7·WP8·WP9 가 같은 함수로(6.17) | WP6·WP7·WP8 |
| 9 | 새 `Defs/HeroDefs.cs`, `Defs/WeaponDefs.cs` | `HeroClassDef`(6.16 — v0.6.1: 특성은 `Traits`(`IReadOnlyList<DefRef<PassiveDef>>`), `HeroTraitDef` 는 만들지 않음). `PassiveDef.Trait`(bool)·`PassiveDef.Classes`·`EvolutionDef.Classes`(`IReadOnlyList<DefRef<HeroClassDef>>`, 기본 빈 = 모두) | 직업 정의(K), 특성 = 패시브(기획 리뷰 — 특성 둘·`on_acquire`·직업 전용 패시브·진화를 형 변경 없이) | WP3·WP5·WP6·WP7·WP8 |
| 10 | `Defs/RulesDefs.cs` | **`HeroRulesDef`**: `Sprites`·`Radius`·`StartWeapons` 삭제, `required DefRef<HeroClassDef> Class` 추가(`Stats` = 모든 직업 기본값·`InvulnAfterHit`·`CrowdSlow` 그대로). `SpawnRulesDef.PathDetour`(기본 1.5) | `rules.hero` = 공통 규칙 + 고른 직업(6.10), 자리 고르기(4.10) | WP3·WP4·WP7 |
| 11 | `Data/Registry/GameDefs.cs` | `Kinds` 에 `hero_classes`, `HeroClasses`(`DefTable<HeroClassDef>`) | 새 종류(6.1) | WP7 |
| 12 | `Sim/World.cs` | 생성자 끝 인자 `HeroClassDef? heroClass = null`, `World.HeroClass`(= 인자 ?? `Rules.Hero?.Class.Def`), `Hero.Radius = HeroClass?.Radius ?? 14` | 월드가 고른 직업 하나를 가짐(6.16) — 주인공 코드는 이것만 읽음 | WP3·WP5·WP6·WP7·WP8 |
| 13 | 게임 `App/Contracts/AppServices.cs` | `AppOptions.HeroClass`(`string?`, `--hero`), `AppOptions.AutoplayVisit`(Core `Sim/Autoplay/AutoplayVisit` 열거형 `None`/`Rooms` — v0.6.1: Core 로 옮겨 `Scenario` 시험도 씀). **v0.6.1 저장**: `RunResult` 에 `HeroClassId`(StageId 다음), 새 `StageRecord`, `ISaveService.LastHero`·`SetLastHero(string)`·`BestRecord(stageId, heroClassId)`(8.3) | 9절 옵션 — 파싱 WP7, 쓰기 WP6. 저장 = 스테이지 × 직업을 형이 강제(WP9 에서 AppServices.cs 를 다시 고치지 않게 — R31) | WP6·WP7 |
| 14 | `Defs/MapDefs.cs` | `PiecePlacementDef`: `Piece` → `string?`, `Grid`(`IReadOnlyList<string>?`)·`Origin`(`Int2?`)·`Id`(`string?`)·`OverBorder`(bool)·`DecorWhy`(`string?`). `TilesetDef.Marks`(`IReadOnlyDictionary<TileMark, IReadOnlyList<int>>`, 새 `enum TileMark { Pillar, Grate, ClosedDoor, Arch }` — v0.6.1: 값마다 `JsonStringEnumMemberName`(`pillar`·`grate`·`closed_door`·`arch`), `StatKey` 와 같은 방식). `MapDef.OpenCenter`(`RectI?`). `MapDef` 에 `Requires` 는 **두지 않음**(1.4 "타일셋 묶음과 맵"). `TilesetDef.Props` 주석의 v0.4 4글자 설명을 8글자(6.11)로 고침 | 안쪽 방 배치(M, 6.11) | WP0·WP2·WP5 |
| 15 | `Map/ArenaLayout.cs`·`MapDataReader.cs` | 배치 풀기 4단계에 `grid`(줄 `Split(' ', RemoveEmptyEntries)`, 줄 길이 자유)·`over_border`(둘레 표시 유지, 문 칸 오류, **막힘 줄어듦 오류** — v0.6.1), `ValidateTileset` 에 `marks` 번호 검사, 새 `ValidateRooms(MapDef, ArenaGrid, ICollection<LayoutError>)`(13.5-19 + v0.6.1 하위 칸 검사 ①②). `LayoutError` 에 `bool Warning = false`(v0.6.1 — `TryExpand`·`ValidateTileset`·`ValidateRooms` 가 같은 형), 부르는 곳 = `MapDataReader.Read`(펼친 뒤 `ValidateRooms` 를 부르고 `Warning` 을 `DataSeverity.Warning` 으로 바꿈). `LayoutHash` 에 `marks` 를 넣지 않음 — 구현과 시험 WP1d-구현 | 6.11 배치 풀기·13.5-19 | WP0·WP2·WP5·WP7 |
| 16 | `Sim/Contracts/Queries.cs`·`Nav/PlacementResolver.cs` | `INavWorld.PathLength(from, to, radius, maxLength)`·**`PathLengthToHero(from, radius, maxLength)`**·**`TryPathStep(from, to, radius, out next)`**(v0.6.1), `PlaceReach(From, Detour, FromHero)` record·`TryPlace(…, PlaceReach, out)` 겹지정(빈 본문 → WP2) | 자리 고르기 길 거리(4.10), 방 들르기 길 방향(9절) | WP2(구현)·WP4(부름) |
| 17 | `TestKit/TestDefs.cs`·`TestWorldBuilder.cs`·`StraightNav.cs` | 합성 직업 `test_hero`(그림 묶음·시작 무기·특성 패시브)와 `rules.hero.class`, `TestWorldBuilder.WithHeroClass`, `StraightNav.PathLength`·`PathLengthToHero`(트인 선이면 직선 거리, 아니면 +∞)·`TryPathStep`(트인 선이면 목표), 합성 타일셋 `marks`·창살 조각(`Xw..`)·모서리 조각, **합성 방 맵**(① 둘레 바로 옆 메가 벽 줄 — 갇힌 띠 ② 같은 벽 `over_border` ③ 문 하나 방 + `room_a` 기준점 ④ (v0.6.1) **6.11 예 배치 좌표 그대로**(합성 이름) ⑤ (v0.6.1) 모서리 자리에 가로·세로 조각만 둔 방 ⑥ (v0.6.1) 둘레 왼쪽 열을 가로 조각으로 덮은 `over_border`) — WP1d-구현의 13.5-19 시험과 WP2 의 13.5-4·5 시험이 쓴다 | 묶음이 실제 데이터 없이 시험 | 모두 |
| 18 | `Sim/Autoplay/AutoplayPolicy.cs` (v0.6.1) | 정적 클래스 → **`sealed class AutoplayPolicy(AutoplayVisit visit)`**: `Vector2 Move(World)`, `int PickCard(LevelUpOffer)`, `IReadOnlyList<RoomVisit> Events`, `IReadOnlyList<RoomVisitStats> RoomStats`. 새 `enum AutoplayVisit { None, Rooms }`, `readonly record struct RoomVisit(string RoomId, int StageTick, bool Skipped)`, `readonly record struct RoomVisitStats(string RoomId, int Visits, int Skips, int TicksInside, float DamageInside)`. 방 번호·다시 구할 시각 상태는 인스턴스에 둔다(시뮬레이션 상태 아님 — 같은 씨앗·같은 옵션이면 같은 입력). 본문 → WP4 | 방 들르기 모드·기록(9절)을 WP4(본문)와 WP6(호출)이 각자 시그니처를 바꾸며 부딪히지 않게 | WP4·WP6 |
| 19 | `Defs/PartDef.cs`, 새 `Data/Registry/StageReach.cs` (v0.6.1) | `PartDef.ReferencedEnemies`(`IReadOnlyList<DefRef<EnemyDef>>`, 기본 빈 — 로더 WP7 이 연결 단계에서 채움), `StageReach.Enemies(StageDef, GameDefs)`(6.14 "나오는 적 계산") — 구현과 시험 WP1d-구현, 그 뒤 소유 WP4 | 13.5-15·13.5-20·안 나오는 적·`TextureStore`·`SoundBank`·시작 검사가 같은 닫힘 계산을 씀 | WP4·WP5·WP7·WP8 |
| 21 | `Defs/MapDefs.cs`, 새 `Map/RoomFloors.cs`, `Map/ArenaLayout.cs` (v0.6.3) | `MapDef.RoomAreas`(`room_areas`, 기준점 ID → `RectI` 메가 사각형), `RoomFloors.Compute(MapDef, ArenaGrid)`·`RoomFloors.Empty`·`IndexAt(tx, ty)`·`IndexAt(Vector2)`·`TryGet`, `RoomFloor(Id, Area, Cells, Doors)`·`OneDoor`, `ValidateRooms` 에 방 영역 검사(13.5-19) — 구현과 시험 WP1d-구현 | 방 바닥(6.11) — 방 체류 압박·방 보급 검사·방 들르기 "방 안" 이 같은 계산 | WP0a·WP4·WP7 |
| 22 | `Sim/World.cs`, `Sim/Contracts/WorldState.cs` (v0.6.3) | `World.Rooms`(`RoomFloors`, 생성자에서 `Arena` 로 계산 — 맵 없으면 `Empty`), `World.RoomPresence`(새 `RoomPresence`: `Current`·`EnteredTick`·방마다 `TicksInside`·`DamageInside`·`SuppliesTaken`, `Reset(roomCount)`) | 방 체류 압박 상태와 판 끝 `ROOMS` 로그를 WP4(씀)·WP7(읽음)이 같은 형으로 | WP4·WP7 |
| 23 | `Sim/Entities/Pickup.cs`, `Sim/Contracts/Queries.cs`·`TickEvents.cs` (v0.6.3) | `Pickup.SupplyRoom`(`string?`, `Reset` 에서 null), `readonly record struct PickupSpawn(PickupDef Def, Vector2 Pos, int Value = 0, string? SupplyRoom = null)`, `ICombatSpawner.SpawnPickup(in PickupSpawn)`, 틱 이벤트 `SupplyCollected(string RoomId, PickupDef Def, Vector2 Pos)`(`TickEventSet.SupplyCollected`) | `room_supply`(WP4)가 WP3 저장소에 획득물을 놓고 "안 주운 보급" 을 판정. 지금 `ICombatSpawner` 구현이 없어 메서드를 더해도 깨지는 코드가 없다 | WP3(구현)·WP4(부름)·WP6·WP7 |
| 24 | `Defs/RulesDefs.cs` (v0.6.3) | `SpawnRulesDef.RoomPressure`(`RoomPressureDef?` — `required` `After`·`EveryNth`·`MinDistance`, null = 끔) | 방 체류 압박 값(6.10·6.12) | WP4 |
| 20 | 새 `Assets/DefAssetNames.cs` (v0.6.1) | `DefAssetNames.For(GameDefs, IEnumerable<ArenaGrid>) → IReadOnlyList<DefAsset>`, `readonly record struct DefAsset(IDef? Owner, string File)`(v0.6.2: null = 기본 내용) — 정의가 가리키는 파일과 주인 D(6.14 13.5-10 귀속 규칙). 빈 본문 → WP7 | 13.5-10 ②(WP0 시험)와 시작 검사 ③ 교차 확인(WP7)이 같은 추출을 씀 | WP0·WP7 |

- 하지 않는 것: `SeenClearly`·`PatternAbility` 조건·발사 프레임 재확인(WP4, 6.6), `unseen_shots` 세기(WP3), 보스 막대 줄 순서(WP4 씀·WP6 그림 — `BossBar` 형은 그대로, 8.2).
  v0.6: 가져오기 도구의 묶음 찾기·건너뛰기(WP0), 시작 검사 "꺼짐"·`--hero`·`--autoplay-visit` 파싱·13.5-20·21 정의 검사·저장 모양(WP7), 주인공 만들기·레벨업 후보 거르기(WP3), `PathLength`·`TryPlace(PlaceReach)`·갇힌 칸·방 기준점 길 검사(WP2),
  자리 고르기를 부르는 쪽·방 들르기 자동 조작(WP4), 꺼짐 카드·타이틀 한 줄(WP6), 직업 소리·음악 대체(WP8), `melee_arc`·직업 고르기 씬(WP9), L5·L6 타일셋(WP10). 각 묶음 v0.6 줄에 넣었다.
  v0.6.1: `DefAssetNames` 본문·`PartDef.ReferencedEnemies` 채우기·저장 구현(WP7), `AutoplayPolicy` 본문·방 들르기 기록(WP4)과 `VISIT` 로그 줄(WP6), `PathLengthToHero`·`TryPathStep` 본문·`occluded` 열(WP2).
  v0.6.3: `room_supply` 이벤트 부품·방 체류 압박·`RoomPresence` 채우기·`AutoplayPolicy` 의 "방 안"(WP4), `SpawnPickup` 구현·보급 수명 빼기·`SupplyCollected` 내기(WP3), 미니맵 흰 점(WP6), 방 보급 검사·`ROOMS` 로그·`StateHasher` 의 `SupplyRoom`·`RoomPresence`(WP7), 실제 `cathedral_arena` 의 `room_areas`(WP0a).
- **완료 기준 — WP1d-형**: `dotnet build -p:SkipAssetCheck=true` 통과, `dotnet test` 통과(빈 본문을 부르는 기존 시험이 없음). 새 시험: (v0.5.3) 공용 STJ 옵션(`DefJson`, WP1b #21)으로 `{ "clear_sight": true }` 를 `PatternDef` 로 읽으면 `ClearSight` 참·생략하면 거짓, `EnemyProjectileSpawn` 의 `SightGated` 기본 거짓.
  (v0.6) `DefJson` 으로 `HeroClassDef`(6.16 예 두 개 — `requires: ["hfmonk"]` → `ContentBundle.Hfmonk`, 모르는 묶음 이름 → 오류, v0.6.1: `traits` 목록)·`PassiveDef`(`trait`·`classes`)·`rules.hero`(`class` 없으면 오류)·`MusicDef`(`fallback`)·`WeaponDef`(`classes`)·`MapDef`(`open_center`·`grid` 놓기, `requires` 는 모르는 속성 오류)·**`TilesetDef.Marks`(`closed_door` 키 → `TileMark.ClosedDoor`, 모르는 키 오류)** 읽기,
  `SoundBindingsDef.Hero` 가 직업 ID 키, `World(…, heroClass)` 의 `HeroClass`·`Hero.Radius`, 10.1 `Assets` 의 표 `bundle`·`prefer`·`optional` 오류·manifest(`sources`·`skipped_bundles`) 읽기.
  (v0.6.3) `MapDef.RoomAreas`(`room_areas`) 읽기, `rules.spawn.room_pressure`(없으면 null, 있으면 세 값 필수) 읽기.
  이 문서(4.10 `PlaceReach`·`PathLength`·`PathLengthToHero`·`TryPathStep`, 6.11 배치·`marks`·`open_center`, 6.14 `StageReach`, 6.16·6.17 코드 조각, 6.15 `hero`, 1.4 manifest 칸, 8.3 저장 계약, 9절 `AutoplayPolicy`)의 시그니처·모양이 계약 파일에 빠짐없이 있다. `git diff` 에 실제 `Data/` 변경 없음.
  **옛 모양 점검**: `rg -n "Optional|optional|StartWeapons|HeroRulesDef|HeroSoundsDef|HeroTraitDef|AutoplayPolicy\.|RunResult\(" src tests tools` 결과를 줄마다 새 모양으로 확인해 완료 보고에 적는다.
- **완료 기준 — WP1d-구현**: `dotnet test` 통과. 새 시험: 10.1 `Map` 의 v0.6·v0.6.1 시험 전부(합성 방 맵 ④ 가 13.5-19 통과, ⑤ 가 하위 1칸 틈 오류, ⑥ 이 막힘 줄어듦 오류), 10.1 `Assets` 의 `OutputBundles(arenas)`(합성 `l5` 맵 그림 → hellfire)·`ContentAvailability`(`Disabled`·`Skipped`), 10.1 `Data` 의 `StageReach` 합성 시험.
  (v0.6.3) 10.1 `Map` 의 방 영역·방 바닥 시험과 빈 `grid` 오류.

#### WP0a — 최소 실제 데이터 (WP1c 뒤, WP2~WP8 앞, 짧게) — v0.6.4: 끝남(커밋은 `git log --grep WP0a`)

WP2 의 실제 맵 길 검사와 WP4 의 단계 1 시나리오가 처음부터 실제 데이터로 돌게, 원본을 봐야 정할 수 있는 숫자만 먼저 저장소에 넣는다. 이 PC 에서 한다(원본 읽기만).

- **하는 것**: 시제품(`d1map probe`, `d1conv scan` — 스크래치)으로 diabdat 를 읽어 값을 적는다. 도구 본체(WP0)는 아직 없어도 된다 — 나중에 WP0 도구의 성질 대조·메타 대조가 같은 값을 다시 확인한다.
  - `Data/tilesets/l1.json`: 단계 1 맵이 쓰는 메가타일의 번호(`floor`·`borders`·`gates`·`pieces`)와 `props`·`sol_known_diffs`(6.11). 문 끝 조각은 견본 그림으로 고른다.
    v0.5: 성질은 8글자로 **시야 비트까지** 원본에서 읽어 적는다 — 뒷벽 1·2·모서리·문 끝 조각의 시야 막힘, 아치 보정 칸의 시야(기획 열린 질문 22).
    v0.5.1: 시제품 `d1map probe` 의 출력은 **`sol` 값(또는 `sol & 7`) 그대로** 받아 6.11 표로 바꿔 적는다 — 시제품이 v0.4 의 4글자(`.WMX`)를 내면 그 글자를 옮기지 않는다(`W`·`M` 뜻이 바뀌어 잘못된 시야 비트가 된다).
  - `Data/rules/sight.json`(v0.5): 6.10 의 값 그대로(기획 23절 "정한 수치"). 원본 유래 값이 아니고 첫 판일 뿐이다 — 그 뒤 소유는 WP5.
  - `Data/maps/cathedral_arena.json`: 배치·입구·기준점·주인공 시작(6.11 예 값, 칸 중심 규칙).
    v0.6: **안쪽 방 배치**(기획 10.3 — 피 방·창살 방·ㄱ·Γ 짧은 벽·기둥 다섯, `open_center` [6,6,13,13], `room_blood`·`room_grate` 기준점 — v0.6.1: + 구역 기준점 `room_west`·`room_south`). 피 방·창살 방의 둘레 쪽 벽은 `over_border` 로 둘레 자리에 겹친다(갇힌 띠 없음).
    v0.6.1: 6.11 예(= 기획 v0.7.1 10.3)가 첫 값이다 — 벽이 꺾이거나 둘레에 겹쳐 꺾이는 자리는 모서리 조각(6.11 "벽 맞물림"), ㄱ 벽은 둘레와 떼어 [3..5,13], Γ 는 [13,13] 부터(`open_center` 밖), Γ 안 기둥 셋은 [15,16]·[17,15]·[17,17], 창살은 두 칸.
    시제품의 ㄱ [1..5,13]·Γ [12,12]·기둥 [16,13] 은 13.5-19·13.5-4 에 걸리므로 옮기지 않는다.
    v0.6.3: `room_areas` 도 6.11 예 그대로 적는다(13.5-19 방 영역 검사 — 피 방·창살 방 문 하나, ㄱ 안·Γ 안 문 둘).
    그래서 `tilesets/l1.json` 에 방 조각 번호·이름(벽 1·2, Γ 모서리 4, 벽 끝 6·7, 창살 36(또는 35), 피 방 166~174 — v0.6.4: 세로 166·모서리 168·가로 169·바닥 172(13.16), 장식 닫힌 문 98 — v0.6.4: 26(13.16), 작은 모서리 기둥 3)과 그 성질(세 비트, `sol` 값 그대로 받아 바꿈)·`marks`(`pillar` 15, `grate`, `closed_door` 26(v0.6.4) — 이름 붙인 번호만, 13.5-12)를 더 적는다.
    번호는 견본(`out-sheets`)으로 고르고, 꺾임·모서리의 그림 맞물림은 견본 그림으로 본다(조사 함정 8). **원작 DUN 은 칸마다 옮기지 않는다**(v0.6.1, 6.11 "원작 방 본뜨기" — 크기·문 방향·벽 재료만 따르고 칸 배열은 이름으로 새로 짬, 기획 19.1). 시제품의 `rnd6` 찍기도 옮기지 않는다.
  - `Data/sprites/monsters.json`·`hero.json` 의 **단계 1 그림 묶음**(기획 14.1 의 단계 1 적 5종 + 주인공): 프레임 수·방향·칸은 `scan` 값, 이벤트 프레임·프레임 시간은 원작 동작 값(출처 주석, 1.4).
    (v0.5.3) `set_skelbow` 는 단계 1 판이라 `variants` 에 `bashtb`·클립에 `special` 을 아직 넣지 않는다 — 블랙애시(단계 2)를 넣을 때 WP5 가 더한다(값은 `scan`: `sklbws` 16프레임).
- **소유**: `tilesets/` 는 WP0a 뒤에도 WP0. `maps/`·`sprites/` 는 WP0a 가 첫 판을 쓰고 **WP5 로 넘긴다**(WP5 가 미리보기로 기준점·입구·`origin` 을 맞춘다, 11.3). WP0a 는 한 담당이 혼자 한다(WP0 담당 권장).
- **완료 기준**: `dotnet test` 통과(`MapDataReader` 로 실제 두 파일을 읽어 오류 0, `Expand` 성공, 13.5-4 배치 검사 통과), 세 파일의 원본 유래 값마다 출처 주석 — **원문 문자열 시험 `Data/SourceCommentTests`(6.14 13.5-16, 기본 분류)를 WP0a 가 써서 통과**,
  `git diff` 에 원작 바이트·덤프 없음(성질 표 키 = 타일셋이 이름 붙인 번호, 13.5-12).
  v0.5: 실제 `cathedral_arena` 로 만든 `VisibilityField` 에서 주인공 시작 자리의 시야가 방 안을 덮고 둘레 벽 뒤 맵 밖은 문 틈 쪽만 보임(시험 하나, 기둥이 `x` 면 기둥 뒤도 보임).
  v0.6: 실제 `cathedral_arena` 가 13.5-19(`ArenaLayout.ValidateRooms`, WP1d-구현 — v0.6.1: 하위 칸 검사 ①② 와 `over_border` 막힘 검사 포함)를 통과, 같은 시야 시험에서 가운데 자리에서 피 방 안·창살 방 안(창살 칸 너머 빼고)이 가려짐(조사 시제품 시야와 같은 모양).
  v0.6.1: v0.6 의 "시제품 길 검사를 다시 돌림" 은 뺐다 — 시제품은 저장소 배치(`cathedral_arena.json`)를 읽지 않고 자기 코드의 옛 배치를 찍으므로 확인이 되지 않는다. 자원 없이 도는 13.5-19 하위 칸 검사가 그 자리를 맡고(WP1d-구현 뒤 켬), 반경 14 격자 검사(13.5-5)는 WP2 가 그 위에 더한다. 시제품 그림은 근거로만 언급한다.
  `audit` 의 DUN 배열 대조(1.4)는 WP0 의 도구가 생긴 뒤 이 맵에 다시 돈다(WP0 완료 기준).
  단계 3 의 L2·`catacombs_arena` 는 WP0a 범위가 아니다(그때까지 WP2 는 합성 맵).

#### WP1e — 계약 갱신: 방 구조 (v0.7, 사용자 결정 R) — **WP1e-형**(WP0a 뒤·WP0·WP2~WP8 앞, 혼자, 짧게) + **WP1e-구현-1**(WP1e-형 뒤, WP0·WP2~WP8 과 동시) + **WP1e-구현-2**(구현-1 뒤, 단계 2 전) — v0.7.1: 구현을 둘로 나눔 — v0.7.2: 형·구현-1·구현-2 **끝남**(커밋은 `git log --grep WP1e`, 다르게 정한 것 13.19)

기획 v0.8 의 방 구조(28절 넘긴 것 1~13)가 바꾸는 **계약**을 한 묶음에 모은다. 이미 커밋된 형(`StageDef`·`RunResult`·`ISaveService`·`SimPhase`)을 바꾸므로 WP2~WP8 이 그 형 위에 코드를 쓰기 전에 낸다.
지금 그 형을 읽는 코드는 `World.cs`(`stage?.Map.Def`)·`EndTickSystem.cs`(`Victory`)와 시험 — `TestDefs.Stage`·`RealData`·`StageReachTests`·`BundleTests`·`HeroAndBundleDefsTests`, 그리고 v0.7.1 에 빠진 것을 더함: `StageStepperTests`(`WithVictory`·`VictoryDef Survive` 로 승리 대기·3초 결과), `SimulationTests`(`(SimPhase)15`·`PhaseCount`·`CreateBuilder` 가 `[BeginTick, EndTick]` 만 넣음·`Director..EndTick` 사이 모든 단계에 시험 시스템을 넣는 반복), `FakeProgression`(`IProgression` 에 보상 멤버가 더해지면 컴파일이 깨짐) — 뿐이고 실제 `Data/stages/` 는 아직 없다 — 지금이 가장 싸다.

- **WP1e-형**(짧게): 아래 표의 형·시그니처·JSON 모양 전부와 컴파일, 그리고 **지금 코드가 새 형으로 계속 도는 데 필요한 본문**(#11 `World` 생성자·#12 `StageStepper`·#13 `EndTick`·#8 단계 끼우기 — 작고 다른 묶음이 바로 기대는 것)과 **#23-형 시험 도우미**.
  v0.7.1 **형 단계 규칙**: `Simulation` 이 늘 부르는 것(`RoomFlowSystem`)과 `World` 생성자가 부르는 것(`GateZones.Compute`)은 **예외를 쓰지 않는 무동작**이다 — `RoomFlowSystem.Tick` 은 `Room == null` 이면 `return`, 방이 있어도 단계를 바꾸지 않는다(형 시험은 `WithRoom` 으로 단계를 고정해 돈다). `GateZones.Compute` 는 빈 사전을 돌려주고 **`World` 생성자는 형에서 이미 `Gates = GateZones.Compute(...)` 를 부른다**(그래서 구현이 `World.cs` 를 고치지 않는다).
  `Simulation` 에 들어가지 않는 순수 로직(`RunPlanner`·`RoomEvents`·`RunSession`·`MapArea`·`ValidateRooms` 13.5-23 부분)만 `throw new NotImplementedException("WP1e-구현 …")` 로 둔다. WP1e-형은 실제 `Data/` 를 쓰지 않는다.
- **WP1e-구현-1**(WP1e-형 뒤, 나란히 — **단계 1 에 필요한 것**): 여러 묶음이 읽고 자원·그래픽과 무관한 순수 로직을 구현과 시험까지 — #10 `RunSeeds`·`RunPlanner`(맵 하나 풀이면 사실상 문만 고름)·`RoomEvents.Schedule`·`LightingState.Apply`·`RoomFlow`(+ `RoomFlowSystem`)의 **일반 방**(들어옴·생존·정리·보상·문·나감, 판의 마지막 일반 방 = 승리)·`RunSession` **한 액트**(`Advance`·`Conclude`·`--room`), #18 `GateZones`, #23-구현 합성 정의.
- **WP1e-구현-2**(구현-1 뒤, **단계 2 전**): 정예·보스 방 분기(`RoomFlow` 정예 처치·2:30·보스 무너짐 + 보상, 정예 방 t 멈춤), 계단(`ActNumber`·`ActAt`·`next_act` 의 `ActPlan`), 넓이 배율(`MapArea`·`AreaScale` — 맵 하나면 늘 1.0), 작은 방 검사(`RoomMapRules`·`ValidateRooms` 13.5-23 맵 쪽 — N ≤ 36 맵이 단계 2 전에는 없음). 10.1 `Run` 행의 `[구현-2]` 시험.
  두 구현이 맡는 파일(`Sim/Run/**`, `Map/GateZones.cs`·`MapArea.cs`·`RoomMapRules.cs`, `Map/ArenaLayout.cs` 의 13.5-23 부분, `TestKit/` 의 **#23-구현** 부분 — 합성 액트·작은 합성 맵)은 그동안 WP2~WP8 이 쓰지 않는다. 끝나면 `Sim/Run/` 은 WP4, `Map/` 쪽은 계약(메인 세션)으로 넘긴다(11.3).
  **기다리는 것**: WP4 의 방 감독·액트 `Scenario`(`RoomEvents`·`RoomFlow`·`RunSession`), WP3 의 보상 시험 중 `RoomCleared` 를 내는 경우(`RoomFlow`), WP6 의 `PlayScene`(`RunSession`), WP2 의 방 맵 실제 시험(`GateZones` 는 안 씀 — 기다리지 않음).
  그 전에는 각 묶음이 **#23-형**의 도우미(`TestDefs.Stage`·`WithRoom`·`FakeHeroCarry`·합성 `rules.rooms`·가짜 `Spawner`)로 시험한다 — 형과 같이 나오므로 기다리지 않는다.

| # | 파일·형 | 바꾸는 것 | 이유 | 영향 |
|---|---|---|---|---|
| 1 | `Defs/StageDefs.cs` | `StageDef`: **`Map`·`Unlock`·`Victory` 삭제**(`VictoryDef`·`VictoryKind` 삭제), `RoomMaps`(`RoomMapPoolDef` — `Normal`·`Elite` = `IReadOnlyList<DefRef<MapDef>>`, `Boss` = `DefRef<MapDef>?`), `Rooms`(`IReadOnlyList<RoomDef>`), `Rewards`(`StageRewardsDef` — `Weights` = `IReadOnlyList<WeightEntry<RewardDef>>`, `FirstRoom` = `DefRef<RewardDef>?`, `GoldScale` 1), `NextAct`(`DefRef<StageDef>?`), `Slice`(bool). 새 `RoomDef`(`Kind`, `From`·`To`(`ClockTime`/`ClockTime?`), `Miniboss`·`Boss` = `DefRef<EnemyDef>?`, `Number`(int — v0.7.1, JSON `number` 선택, 없으면 로더가 배열 자리 + 1)). `EliteChanceDef`: `Every` 삭제, `At`(`ClockTime`, 기본 0:30). `SpawnIntervalDef`·`StageScaling` 주석의 "분" → t | 스테이지 = 액트(6.12) | WP4·WP5·WP7·WP8 |
| 2 | 새 `Defs/RewardDefs.cs`, `Data/Registry/GameDefs.cs` | `RewardDef`·`RewardKind`·`RewardCardsDef`·`RewardSkipDef`(6.18), `GameDefs.Rewards`·`Kinds` 에 `rewards` | 문 보상 정의(6.18) | WP3·WP5·WP7·WP8 |
| 3 | `Defs/MapDefs.cs`, 새 `Defs/RoomDefs.cs` | `MapDef.RoomKinds`(`IReadOnlyList<RoomKind>`, 기본 빈), `enum RoomKind { Normal, Elite, Boss }`(JSON 소문자) | 방 맵 표시(6.11) | WP0·WP5·WP7 |
| 4 | `Defs/RulesDefs.cs` | `RulesDef.Rooms`(`RoomRulesDef?` — 6.10 의 키 전부 `required`, `RoomGatesDef`·`AreaScaleDef`·`ExitsDef`) | 방 흐름 값(6.10) | WP4·WP6·WP7 |
| 5 | `Defs/WeaponDefs.cs`·`AudioDefs.cs` | `PassiveDef.Blessing`(bool), `PickupKind.Reward`(JSON `reward`), `SoundBindingsDef.Rewards`(`IReadOnlyDictionary<string, RewardSoundsDef(Collect, Open)>`) | 축복·보상 물체·보상 소리(6.9·6.15) | WP3·WP8 |
| 6 | `Defs/PartDef.cs` | `PartDef.At`(`ClockTime?` — 스테이지 이벤트의 `at`, 로더 WP7 이 채움) | 방 이벤트 옮기기·13.5-18·23 이 매개변수 형을 모르고 시각을 읽음(6.2) | WP4·WP7 |
| 7 | 새 `Sim/Contracts/RoomState.cs`, `TickEvents.cs`·`Presentation.cs` | `RoomPhase`·`RoomExit`·`RoomStart`·`RoomState`(3.6), 틱 이벤트 `RoomCleared(RoomKind Kind, bool Collapsed)`·`RoomSurviveEnded`, `CueKind` 에 `RoomCleared`·`DoorsOpened`·`RewardCollected` | 방 상태를 감독·성장·그림·화면·소리가 같은 형으로 | 모두 |
| 8 | `Sim/Contracts/SimSystems.cs`, `Simulation.CreateBuilder` | `SimPhase.RoomFlow`(16)를 `EndTick`(17) 앞에, `CreateBuilder` 가 `BeginTick`·`EndTick` 과 함께 `RoomFlowSystem` 을 넣음(형: 무동작 본문 — 예외 아님, v0.7.1. 구현 #10), `PhaseCount` 17 | 방 흐름 단계(3.5) | 모두(순서표) |
| 9 | `Sim/Contracts/Progression.cs`·`WorldState.cs` | `ChoiceKind.Reward`, `OfferSource`, `LevelUpOffer.Source`(끝 인자, 기본 `LevelUp`), `IProgression` 에 `PendingRewards`·`RewardOffer`·`ChooseReward`·`RerollReward`·`SkipReward`·`RewardAvailable`(6.18) | 보상 창 = 레벨업 오버레이 재사용 | WP3(구현)·WP6 |
| 10 | 새 `Sim/Run/` | `RunSeeds`, `RunPlanner`·`ActPlan`·`RoomPlan`, `RoomEvents`·`RoomSchedule`·`ScheduledEvent`, `RoomFlow`·`RoomFlowSystem`, `RunState`·`RoomRecord`·`RunDepth`·`RoomRef`·`RunStart`·`HeroCarry`·`CarriedSlot`·`IHeroCarry`·`RunSession`(3.6) — **본문은 WP1e-구현** | 판·방 구조의 한 곳 | 모두 |
| 11 | `Sim/World.cs` | 생성자 둘(v0.7.1 — 3.6 결정 4): 방 없는 월드 `World(GameDefs defs, ulong seed, StageDef? stage = null, HeroClassDef? heroClass = null)`(맵 = `stage?.RoomMaps.Boss?.Def ?? 첫 일반 맵` — `slice` 판은 보스 맵이 없다), 방 월드 `World(GameDefs defs, RoomStart room)`(씨앗·액트·직업을 `room` 에서 — 맵 = `room.Plan.Map`, 주인공 = 들어온 문 `inside`·`center` 쪽 방위, 첫 틱 전 `RoomEvents.Schedule(...).Replay` 를 `LightingState.Apply` 로). `Room`(`RoomState?`)·`Run`(`RunState?`)·`Gates`(= `GateZones.Compute(...)` 를 **형에서 이미 부름** — 형 본문은 빈 사전, 구현 #18)·`DifficultyMinutes`, `RunStats` = `room?.Run.Stats ?? new()`, `CollapseEnemies()`(`BeginVictory` 가 부름). `Rooms`·`RoomPresence` 속성 이름 → `InnerRooms`·`InnerRoomPresence`(#24) | 방마다 새 월드(3.6 결정 1) | 모두 |
| 12 | `Sim/StageStepper.cs` | `StageStep.RoomExit` — `Room?.Phase == Exit`(v0.7.2: 이고 `Outcome == Running`) 면 돌지 않고 돌려줌. 판정 순서 `Finished` → `AwaitingChoice` → `RoomExit` → 히트 스톱(v0.7.1, 3.3) | 방 이동(3.3) | WP6 |
| 13 | `Sim/Core/EndTickSystem.cs` | `Survive` 승리 삭제, 적 공격 지우기를 `Room?.Phase ≥ Reward` 에도, 선택 대기 순서 레벨업 → 상자 → 보상 | 3.5·6.18 | WP3·WP6 |
| 14 | `Sim/Entities/Pickup.cs`·`Hero.cs`, `Sim/Contracts/Queries.cs` | `Pickup.Reward`(`RewardDef?`)·`Pickup.GatesExit`(bool, `Reset` 에서 지움 — `PickupSpawn.Reward != null` 이면 `SpawnPickup` 이 참으로, v0.7.1)·`Pickup.Magnetized`(bool, v0.7.1 — 끌려오는 중. 쓰기 WP3, 읽기 `RoomFlow` 의 나가기 조건. WP3 조각에 있던 끌어당김 표시를 공통 조각으로 올림), `PickupSpawn` 끝에 `RewardDef? Reward = null`, `Hero.Blessings`(`BlessingSlots` — `InlineArray(8)` 의 `PassiveSlot`) | 보상 물체·축복(6.9) | WP3·WP5·WP6·WP7 |
| 15 | `Sim/Contracts/RunSetup.cs`·`ModuleContext.cs` | `CommandButtons.Exit` 깃발 + `PlayerCommand` 끝에 `byte ExitIndex = 0`(깃발이 있을 때만 뜻이 있음 — 문 단계에서 `Exits[ExitIndex]` 로 나감, 자동 조작 `DOOR_SKIP` 만). v0.7.1: v0.7 의 `sbyte Exit = -1` 은 `default`·`PlayerCommand.None`·`new PlayerCommand()` 에서 0 이 되어 정지 명령이 0번 출구로 나가는 조용한 오동작이었다 — 기본값 0 = "안 나감" 이 되게 깃발로 바꿈, `ModuleContext.SetHeroCarry(IHeroCarry)`·`HeroCarry` 속성. `RunOptions.StartAt` 주석 = 첫 방의 방 시계 | 3.6·9절 | WP3·WP4·WP6 |
| 16 | `Sim/Autoplay/AutoplayPolicy.cs` | `PlayerCommand Next(World)`(`Move` + 문 고르기·`DOOR_SKIP` 의 `Exit`), `int PickCard(LevelUpOffer)` 가 `Source` 로 마법서·성소 규칙, `IReadOnlyList<DoorSkip> DoorSkips`(`DoorSkip(int Act, int Room, int StageTick)`), v0.7.1 `IReadOnlyList<RewardStall> RewardStalls`(`RewardStall(int Act, int Room, RoomPhase Phase, int StageTick)`), `RoomVisit`·`RoomVisitStats` 에 `MapId`. 정책 인스턴스는 판마다 하나(월드가 바뀌어도 그대로). 본문 → WP4 | 자동 조작의 방 규칙(9절) | WP4·WP6 |
| 17 | 게임 `App/Contracts/AppServices.cs`·새 `IWorldBound.cs` | `RunResult(StartActId, HeroClassId, Victory, Reached, ReachedActId, RunTime, Level, Kills, Gold)`, `StageRecord` → `RunRecord`(`BestDepth` 는 `RunDepth?` — v0.7.1, 8.3), `IWorldBound`(7.11, v0.7.1)·`IAudioServices : IWorldBound`, `ISaveService.IsUnlocked` 삭제·`BestRecord(startActId, heroClassId) → RunRecord?`, `AppOptions.StartRoom`(`RoomRef?`)·`Map`(`string?`), `StageId` 주석 = 시작 액트, `IAudioServices.PreloadStage` 주석 = 액트 시작 때만 | 저장·옵션(8.3·9절) | WP6·WP7·WP8 |
| 18 | 새 `Map/GateZones.cs`·`MapArea.cs`·`RoomMapRules.cs`, `Map/ArenaLayout.cs` | `GateZones.Compute`·`Opposite`·`GateZone`, `MapArea.WalkableCells`, `RoomMapRules.SmallRoomMaxN`(36)·`DoorClearance`(150), `ValidateRooms` 에 13.5-23 맵 쪽(6.11 "방 맵") — **본문은 WP1e-구현** | 방 맵 규칙(6.11) | WP0·WP2·WP4·WP5·WP6 |
| 19 | `Data/Registry/StageReach.cs` | 시작 집합에 `rooms[].miniboss`·`boss`(6.14) — 한 줄, WP1e-형 | 정예 방 중간 보스가 이벤트 없이도 "나오는 적" 에 듦 | WP4·WP5·WP7 |
| 20 | `Assets/ContentAvailability.cs` 주석·`DefAssetNames` 주석 | 맵 그림 귀속 = 그 맵을 `room_maps` 에 둔 스테이지들(본문 WP7) | 13.5-10 묶음판(6.14) | WP0·WP7 |
| 21 | 게임 `Scenes/SceneFactory.cs` 주석 | StageSelect 없음 — 새 게임 = `PlayScene(RunStart)`. v0.7.1: `PlayScene` 은 `Advance` 앞뒤의 표시 이벤트 소비·`IWorldBound.Attach` 순서(7.11)와 판 끝 `Conclude` 를 지킨다(주석) | 8.1 | WP6 |
| 22 | `Sim/Contracts/WorldState.cs` | `RoomPresence` 주석 = 방(월드) 하나의 누적, `FairnessCounters.AddTo(FairnessCounters)` | 3.6 넘기는 것 | WP4·WP7 |
| 23-형 | `TestKit/TestDefs.cs`·`TestWorldBuilder.cs`·새 `FakeSpawner.cs` | **형과 같이 냄**(v0.7.1 — 다른 묶음이 바로 씀): `TestDefs.Stage(map)` 를 새 모양으로(보스 맵 = `map`, `rooms` 는 일반 방 하나 — 기존 시험이 컴파일되려면 형에서 바뀌어야 함), 합성 보상 둘·보상 물체·`rules.rooms`, `TestWorldBuilder.WithRoom(phase, clockTicks, …)`(합성 `RoomState` — 단계를 손으로 정함), `FakeHeroCarry`(레벨·체력·칸 왕복만), `FakeSpawner`(`SpawnPickup` 이 획득물을 만들고 `Reward` 면 `GatesExit` — WP3 없이 `RoomFlow` 시험), `TestWorldBuilder` 의 직선 내비가 `TryPlace` 를 줌(WP2 없이) | 묶음이 실제 데이터·WP3 없이 방을 시험 | 모두 |
| 23-구현 | `TestKit/RoomActs.cs`(새) | **WP1e-구현** 맡음: **합성 액트 둘**(작은 합성 맵 둘 + 큰 합성 맵 하나, 일반·정예·보스 방 — `RunPlanner`·`RunSession` 시험), 작은 합성 방 맵(13.5-23 맵 쪽 시험). 구현-1 은 일반 방 액트 하나부터, 구현-2 가 정예·보스·계단을 더함 | `Run` 행 시험 | WP1e-구현 |
| 24 | `Sim/World.cs`·`Sim/Contracts/WorldState.cs`·`Progression.cs` (v0.7.1) | 이름 바꾸기(IDE 바꾸기 — 본문 그대로): `World.Rooms` → `World.InnerRooms`, `World.RoomPresence` → `World.InnerRoomPresence`(형 `RoomFloors`·`RoomPresence` 는 그대로 — 11.2 의 "방" 두 뜻). `RunStats.Reset()` 삭제(부르는 곳 없음 — 판마다 `RunSession` 이 새로 만듦, 3.6) | `World.Room`(판 단위 방)과 한 글자 차이, 방마다 `RunStats` 가 조용히 0 이 되는 길 막기 | WP3·WP4·WP6·WP7 |
| 25 | 새 `Defs/LightChangeParams.cs`, `Sim/Contracts/WorldState.cs` (v0.7.1) | `LightChangeParams`·`LightSet`(3.6 결정 5), `LightingState.Apply(LightChangeParams)` — 본문은 WP1e-구현-1. 부품 `(StageEvent, light_change)` 등록은 WP4 가 이 매개변수 형으로 | 첫 틱 전 조명 다시 적용(감독 없이) | WP4·WP5 |
| 26 | `Sim/Contracts/RoomState.cs`·`Sim/Run/RunSession.cs` (v0.7.1) | `RoomState.ExitArmed`, `RemapEntry` 주석 = 서로 바꾸기, `RunSession.Conclude()`, `RoomDef.Number`(#1 에 더함 — JSON `number`), `StateHasher` 자리 `Room`·`Run`(본문 WP7) | 3.6·3.4 | WP4·WP6·WP7 |

- 하지 않는 것: 감독의 방 모드·`room_supply` 시각·정예 판정·넓이 배율 곱하기·`--start-at` 적용(WP4), 보상 줍기·창·축복·`IHeroCarry` 구현·끌어당김(WP3), 봉인·아이콘·어두워짐 그리기(WP5), `PlayScene`·HUD 방 줄·결과·타이틀 한 줄·로그 줄(WP6),
  로더의 `rewards`·`rules.rooms`·`PartDef.At`·`NextAct` 연결·13.5-3·20·23 데이터 쪽·옵션 파싱·저장·해시(WP7), 소리 연결·문 소리(WP8), 작은 방 맵 데이터(WP5)·굽기(WP0). 각 묶음 v0.7 줄에 넣었다.
- **완료 기준 — WP1e-형**: `dotnet build -p:SkipAssetCheck=true` 통과, `dotnet test` 통과(빈 본문을 부르는 기존 시험이 없음 — #11 의 방 없는 월드 경로로 기존 시험이 돎). v0.7.1 — **"그대로" 가 아니라 다시 쓰는 기존 시험**: `StageStepperTests` 의 승리 지연은 `survive` 승리가 없어져 `World.BeginVictory()` 를 직접 불러 시험, `SimulationTests` 는 단계 번호 `RoomFlow` 16·`EndTick` 17·`PhaseCount` 17, `CreateBuilder` 기본 등록 셋(`BeginTick`·`RoomFlow`·`EndTick`), 시험 시스템을 넣는 반복 범위에서 `RoomFlow` 를 뺌(두 번째 등록 예외), `FakeProgression` 에 보상 멤버. 새 시험:
  `DefJson`(+ `TestJson`)으로 6.12 예의 `StageDef`(`rooms`·`room_maps`·`rewards`·`next_act`·`slice`·`elite_chance.at`, 옛 `map`·`unlock`·`victory`·`elite_chance.every` → 모르는 속성 오류)·6.18 `RewardDef`(`kind` 넷, 모르는 종류 오류)·`MapDef.room_kinds`·`rules.rooms`(키 하나 빠지면 오류)·`PassiveDef.blessing`·`SoundBindingsDef.rewards` 읽기,
  `World(defs, RoomStart)`(v0.7.1 — 방 월드 생성자) 가 주인공을 `gate_se` 진입점에 `center` 쪽 방위로 두고 `Room.Phase == Enter`·`Run`·`RunStats` 가 `RunState.Stats` 와 같은 객체, 방 없는 월드의 `DifficultyMinutes == StageMinutes`, `StageStepper` 가 `Phase == Exit` 에서 `RoomExit` 를 돌려주고 틱을 돌지 않음,
  `EndTick` 대기 순서(레벨업·보상 둘 다 있으면 레벨업 먼저), `Phase = Reward` 인 월드에서 적 투사체가 그 틱에 지워짐(`WithRoom` 으로 단계를 고정 — 형의 `RoomFlowSystem` 은 무동작이라 한 틱을 돌려도 예외·단계 바뀜 없음), `SimPhase.RoomFlow` 가 `EndTick` 바로 앞, `LevelUpOffer` 기존 생성 호출이 그대로 컴파일(`Source` 기본값).
  v0.7.1: `PlayerCommand.None` 에 `CommandButtons.Exit` 없음, `StageStepper` 판정 순서(`Exit` + 선택 대기 → `AwaitingChoice`), `World(defs, RoomStart)` 의 씨앗·액트·직업이 `RoomStart` 것, `World.Gates` 가 형에서 빈 사전(예외 없음), `RunStats.Reset` 없음.
  이 문서(3.6 코드 조각, 6.10 `rules.rooms`, 6.12 예, 6.18, 8.3 저장 계약, 9절 `--room`·`--map`)의 시그니처·모양이 계약 파일에 빠짐없이 있다. `git diff` 에 실제 `Data/` 변경 없음.
  **옛 모양 점검**: `rg -n "Unlock|VictoryDef|VictoryKind|\.Victory\b|IsUnlocked|StageRecord|\.Map\.Def|EliteChance|StageSelect|StageMinutes|WithVictory|\(SimPhase\)15|PhaseCount|\.Rooms\b|RoomPresence\b|RunStats\.Reset|sbyte Exit" src tests tools`(v0.7.1 에 뒤 여덟을 더함. `StageMinutes` 는 남지만 규칙 쪽 쓰임은 `DifficultyMinutes` 로, `PhaseCount` 는 17 인지) 결과를 줄마다 새 모양으로 확인해 완료 보고에 적는다.
- **완료 기준 — WP1e-구현-1**: `dotnet test` 통과. 새 시험: 10.1 `Run` 행 중 `[구현-2]` 표시가 없는 것 전부, 10.1 `Map` 의 `GateZones` 시험. `RunSession` 은 등록된 `IHeroCarry` 가 없을 때도 돈다(짐 건너뜀).
- **완료 기준 — WP1e-구현-2**(단계 2 시작 전): 10.1 `Run` 행의 `[구현-2]` 전부, 10.1 `Map` 의 나머지 v0.7 시험(`MapArea`·13.5-23 맵 쪽 — 실제 `cathedral_arena` 를 `room_kinds` 를 단 사본으로 읽어 통과).

#### WP1f — 계약 갱신: 기획 v0.8.1 29절 + 사용자 결정 U (v0.8) — WP1e 뒤·WP0·WP2~WP8 앞, 혼자, **형과 순수 로직 구현까지 한 번에** — v0.8.2: **끝남**(커밋은 `git log --grep WP1f`, 다르게 정한 것 13.22)

기획 v0.8.1 29절 "설계에 넘기는 것" 1~19 와 사용자 결정 U(AI 확대)가 바꾸는 **계약**을 한 묶음에 모은다. 11.3 의 "WP1f 에서 바뀌는 멤버" ①~⑧(+ ⑨ 층, ⑩ AI 확대 자원 모양)이 이 표로 닫힌다 — WP1f 뒤에는 WP0·WP2~WP8 이 이 멤버의 모양·값에 기대도 된다.
WP2~WP8 이 아직 시작 전이라 지금이 가장 싸다(WP1e 와 같은 까닭). WP1e 처럼 형과 구현을 한 작업에서 낸다 — 바뀌는 순수 로직(`Sim/Run/**`·`Map/`)이 모두 WP1e 가 쓴 파일이고 다른 묶음이 아직 그 위에 코드를 쓰지 않았다.
나누는 기준은 **"형만"**(모양·시그니처·JSON 읽기 — 본문은 그 묶음)과 **"구현과 시험까지"**(자원·그래픽과 무관하고 여러 묶음이 같이 읽는 순수 로직 — WP1f 가 본문과 시험을 낸다)이고, 표의 "구현" 열에 적었다.

| # | 파일·형 | 바꾸는 것 | 구현 | 기획 29 # · 11.3 | 영향 |
|---|---|---|---|---|---|
| 1 | `Defs/RewardDefs.cs` | `RewardKind` = `WeaponCards`·`WeaponBundle`·`BeltPotions`·`Shrine`(`BlessingCards`·`LevelUp`·`FullHeal` 삭제), `RewardDef`: `Gold`·`SkipWhenHpAtLeast` 삭제, `Bundle`(`RewardBundleDef`)·`BeltPotions`·`Shrine`(`ShrineDef`) 더함, `RewardSkipDef.Gold` 삭제(6.18) | 형 + 읽기 시험(본문 WP3) | 4 · ③ | WP3·WP5·WP7·WP8 |
| 2 | `Defs/StatKeys.cs`, `Defs/RulesDefs.cs`(hero) | `StatKey.MaxHpPct`(`max_hp_pct`), `HeroRulesDef.Belt`(`BeltRulesDef` — `slots`·`threshold`·`cooldown`), `LevelUpRulesDef.Temper`(string), `FillerCardDef.Stats` 더함·`FillerCardDef.Gold` 삭제(6.10) | 형 + 읽기 시험 | 4·9 · ③④ | WP3·WP6·WP7 |
| 3 | `Defs/RulesDefs.cs`(rooms) | `RoomRulesDef`: `RemainingMarker`·`RewardAnchor`·`ChestRing` 삭제, `RewardSpot`(`RewardSpotDef` — `Distance`·`Spacing`·`Clearance`)·`Stairs`(`StairsRulesDef` — `WeaponSlots`·`PassiveSlots`)·`TransitionFloor` 더함, `CleanupLimit` 주석 25(6.10) | 형 + 읽기 시험 | 6·8 · ⑤ | WP4·WP6·WP7 |
| 4 | `Defs/RoomDefs.cs` | `RoomDef.Floor`(int, JSON `floor`, 0 = 안 적음 — 6.12) | 형 | 19(층) · ⑨ | WP4·WP6·WP7 |
| 5 | `Defs/MapDefs.cs` | `TilesetDef.Mirror`(`TilesetMirrorDef(Pairs, Self)`), `MapDef.Mirror`(bool, JSON `mirror`), `MapDef.Mirrored`(`MapDef?`)·`IsMirror`·`MirrorOf`(`string?`) — 셋은 `JsonIgnore`, 읽기가 채움(6.11 "거울판") | 형 | 13 · ⑦ | WP0·WP5·WP7 |
| 6 | 새 `Map/MapMirror.cs`, `Map/MapDataReader.cs`, `Map/ArenaLayout.cs`(`ValidateTileset`·`ValidateRooms`), `Map/GateZones.cs` | `MapMirror.Of(MapDef, TilesetDef)`(배치 뒤집기·짝 이름·문 변/ID·**`entries` 의 ID 도 짝으로**(v0.8.1)·좌표·사각형·방위 — 기준점 ID 는 그대로), 읽기가 `mirror: true` 맵의 거울판을 만들어 원판 `Mirrored` 에, 짝 표 검사(13.5-12 — 최종 성질 전치, v0.8.1 둘레·문 **자리 짝** 검사), 13.5-19·23 ③ 을 거울판에도, v0.8.1 ③ 에 "입구가 같은 ID 문 앞"(`ValidateRooms`), `GateZones.MirrorGate(id)`·`MirrorSide` | **구현과 시험까지** | 11·13 · ⑦⑧ | WP0·WP2·WP5 |
| 7 | `Sim/Run/RunPlanner.cs` | ① (맵, 방향) 쌍 뽑기·바로 앞 쌍 빼기, ③ 출구는 계단만 하나(정예·보스 앞도 둘·셋), 거울판이면 ②③ 결과를 짝 이름으로(3.6 결정 2). 단계마다 난수 수는 그대로 | **구현과 시험까지** | 5·13 · ① | WP4·WP6 |
| 8 | `Sim/Contracts/RoomState.cs`, `Sim/World.cs` | `RoomExit.NextFloor`(끝 인자, 기본 0), `RoomState.BossClockTicks`·`TimedEffectsRun`·`RewardSpot`, `RemapEntry` 가 거울판 짝 이름 먼저, **v0.8.1: `RemapEntry` 를 시작 문 기준 바꾸기로(서로 바꾸기 없앰)·`RemapMarch` 더함**(3.6 결정 5 — `RoomEventsTests` 의 `fromSw.RemapEntry("gate_ne") == "gate_sw"` 를 `"gate_ne"` 로), `World.TimedEffectsRun`(3.6), `World` 생성자의 주인공 시작 방향이 `rules.RewardAnchor` 대신 기준점 `center`(v0.8.1 — 키를 없애므로) | **구현과 시험까지** | 6·7·13 · ②⑦ | WP3·WP4·WP5·WP6 |
| 9 | `Sim/Run/RoomFlow.cs`, 새 `Sim/Run/RewardSpot.cs` | 보상 자리 `RewardSpot.Find`·`Row`(주인공 앞 100px, 줄 40px — v0.8.1: 어느 방향도 못 가면 clearance 문턱을 주인공 반경으로 한 번 더) — `reward_anchor`·`chest_ring` 자리 대신, `DoorsOpened` 신호 자리도 `rules.RewardAnchor` 대신 `RoomState.RewardSpot`(v0.8.1), 정예·보스 방 앞 문도 보상(= 덤) 채우기, `NextFloor`, `BossClockTicks` 기록(3.6 결정 3) | **구현과 시험까지** | 5·6 · ② | WP3·WP4·WP5 |
| 10 | `Sim/Run/RunState.cs` | `HeroCarry` → init 속성 record + `WeaponSlotsOpen`·`PassiveSlotsOpen`·`Belt`·`Mods`·`Shrines`·`Effects`, `CarriedStat`·`CarriedEffect`, `RoomRecord` 끝에 `Mirrored`·`BossClockTicks`·`PicksLeft`(기본값 있음), `IHeroCarry` 주석(넘기는 것 v0.8)·**v0.8.1 `IHeroCarry.Validate`**(이어 하기 전 WP3 뜻 확인 — `FakeHeroCarry` 는 null) | 형(+ JSON 왕복 시험). `Capture`·`Restore` 본문은 WP3 | 4·7·8·14 · ④⑥ | WP3·WP6·WP7 |
| 11 | `Sim/Run/RunSession.cs`, 새 `Sim/Run/RunSnapshot.cs`·`RoomStartLimits.cs` | 계단 보상(칸 +1, 상한 6 — v0.8.1: 새 `Sim/Run/RunSlots.cs` `OpenAt(액트 번호)` 하나로), `--room` 시작 방의 보상 = 앞 방 `RewardOrder[0]`(종류 무관), `RoomStartLimits.MaxStartClockTicks` + 생성자 검사, `CanSuspend`·`Suspend()`(v0.8.1: Doors 면 계단 문 `Exit` 한 틱, Exit 면 그 자리 — 3.6 ⓐⓑ)·이어 하기 생성자(v0.8.1: 짐 ID·`Format`·`Carry` null·`Validate` 대조를 `Restore` 전에), `RunSnapshot`(v0.8.1: `Carry` nullable, `CurrentFormat`)·`RunSnapshotMismatchException`, `AreaScaleOf` 가 거울판을 원판 값으로, `CloseRoom` 이 `Mirrored`·`BossClockTicks`·`PicksLeft` 를 채움(3.6 결정 4) | **구현과 시험까지** | 8·14·15 · ⑥ | WP4·WP6·WP7 |
| 12 | `Sim/Run/RoomEvents.cs` | `startClockTicks` 전의 `miniboss`·`boss` 는 그 시각으로 옮김, `CheckPlacement(act, rules)`·`StrongKinds`·`ClimaxKinds`·`StrongEarliest`(0:20)·`ClimaxWindow`(0:45) 상수(3.6 결정 5) | **구현과 시험까지**(검사기 등록은 WP4) | 11·15 · ⑥⑧ | WP4·WP7 |
| 13 | `Sim/Entities/Hero.cs` | `WeaponSlots`·`PassiveSlots.SlotCount` 4 → 6, `Hero.WeaponSlotsOpen`·`PassiveSlotsOpen`(int — v0.8.1: `Reset()` 은 규칙을 받지 않으므로 0 으로 두고, 주인공 만들기(WP3)가 `RunSlots.OpenAt(World.Room?.Plan.Act ?? 1, …)` 로 정함, 짐 `Restore` 가 덮음)·`Hero.Belt`(int) — 공통 조각(11.2) | 형 | 4·8 · ④ | WP3·WP5·WP6·WP7 |
| 14 | `Sim/Contracts/Progression.cs`·`RunStats`·`FairnessCounters` | `IProgression.PicksLeft`, `ChestItemKind.Gold` → `Temper`, `OfferSource.Shrine` 주석(매혹의 성소 창), `RewardAvailable` 주석(v0.8 거르기). `RunStats`·`FairnessCounters` 는 **모든 공개 멤버가 읽기·쓰기 가능·값 형식(배열)**(규칙 — 3.6 "계단 저장"). **v0.8.1**: `RunStats.WeaponSlotCount = 4` 없앰, `DamageByWeapon` = `{ get; set; } = new float[WeaponSlots.SlotCount]`(6 — v0.8 표에 빠져 5번째 칸 무기 피해가 범위 밖 예외, get 만 있어 저장 왕복에서 0) | 형 + 반사 왕복 시험·모양 지문 시험 | 4·14·16 · ③⑥ | WP3·WP4·WP6 |
| 15 | 게임 `App/Contracts/AppServices.cs` | `ISaveService` 에 `PeekRun()`·`TakeRun()`·`SaveRun(RunSnapshot)`·`DiscardRun()`, `RunSnapshotInfo`(8.3) | 형(본문 WP7) | 8 · ⑥ | WP6·WP7 |
| 16 | `Assets/ImportTable.cs` | `upscale.json` → `UpscaleTable`(`Models` — 이름 → `scale`·`files`, `OutputScale`, `Apply`(`Maps`·`MonstersMinWidth`), `Include`·`Exclude`), `FileNames` 9개, 표 해시가 9개를 이음(1.4). v0.8.1: `output_scale` 은 2 만 받음(다른 값 표 오류) | 형 + 읽기 시험(적용 범위 판정 함수 `UpscaleTable.Applies(ImportUnitEntry)`(칸 폭 = 동작 최대 폭)·`Applies(string tilesetId)` 까지 — 순수, v0.8.1 에 둘로 나눔) | U · ⑩ | WP0·WP7 |
| 17 | `Assets/AssetManifest.cs` | `AssetManifest.Upscale`(`ManifestUpscale` — `Status`(off·done·fallback)·`Model`·`Tool`·`Files`·`OutputScale`·`Gpu`·`Reason`), `ManifestFile.Scale`(기본 1, 1 이면 안 씀) | 형 + 읽기 시험 | U · ⑩ | WP0·WP7 |
| 18 | `Assets/SpriteMeta.cs`·`ArenaArt.cs`·`AssetFormat.cs` | `SpriteMeta.Scale`(1·2), 메타 형식 2 → 3, `ArenaArt.Scale`·형식 1 → 2, `AssetFormat.Version` 2 → 3, `Validate` 가 칸·사각형을 그림 픽셀로 보고 `scale` ∈ {1, 2} 확인(7.1·7.5·7.6) | 형 + 검사 시험 | U · ⑩ | WP0·WP5·WP7 |
| 19 | `Sim/Contracts/Presentation.cs` | `CueKind.BeltPotion`(허리띠 저절로 마심 — 소리·HUD 깜빡임) | 형 | 4 | WP3·WP6·WP8 |
| 20 | `TestKit/`(#23-형·#23-구현 소유는 11.3) | `FakeHeroCarry` 새 짐 필드(+ `Validate` 는 null), `TestDefs` 합성 보상 = 마법서·보물 상자·물약 + 성소 하나(새 모양), `RoomRules` 새 키, `RoomActs` 에 짝 표가 있는 합성 타일셋·`mirror: true` 작은 맵·층을 단 합성 액트(v0.8.1: `RoomActs` 는 WP4 로 넘어갔지만 WP1f 가 끝날 때까지 WP1f 가 맡는다 — 11.3) | 구현 | 모두 | 모두 |

- **하지 않는 것**(본문은 그 묶음 — 각 묶음 v0.8 줄): 보상 효과·허리띠·`단련`·성소·시간 효과 멈춤·짐 담기/풀기·`PicksLeft`(WP3), 사냥 모드·`CheckPlacement` 등록·포위 고리·광폭화 시계·정책 상태 비우기·문 우선순위(WP4), 그림 배율·거울판 미리보기·문 위 `▼`(WP5),
  일시정지 `저장하고 끝내기`·타이틀 `이어 하기`·`transition_floor`·HUD 허리띠·칸 5(WP6), 로더 새 키·`run.json`·`--map …@m`·`StateHasher`(WP7), AI 확대 도구·거울판 굽기·`audit` 거울판·L1 짝 조각 번호(WP0), 성소·물약 소리(WP8). 실제 `Data/` 는 고치지 않는다(WP0·WP3·WP4·WP5 소유).
- **완료 기준 — 형**: `dotnet build -p:SkipAssetCheck=true`·`dotnet test` 통과. 읽기 시험(`DefJson`/`TestJson`): 6.18 새 보상 넷(옛 `level_up`·`full_heal`·`blessing_cards` → 모르는 값 오류, `skip.gold`·`gold`·`skip_when_hp_at_least` → 모르는 속성 오류), 6.10 `rules.rooms` 새 모양(옛 `remaining_marker`·`reward_anchor`·`chest_ring` → 오류, `reward_spot`·`stairs`·`transition_floor` 하나 빠지면 오류),
  `rules.hero.belt`·`level_up.temper`·필러 `stats`·`max_hp_pct`, `RoomDef.floor`, 타일셋 `mirror`·맵 `mirror`, `Table/upscale.json`(9개 표 해시, v0.8.1: `output_scale` 4 → 표 오류, `Applies(ImportUnitEntry)` 가 동작마다 폭이 다른 항목을 최대 폭으로 판정, `Applies("l1")`·`exclude: ["tileset:l1"]`), manifest `upscale`·파일 `scale`, `SpriteMeta` 형식 3(`scale`, 형식 2 → 오류)·`ArenaArt` 형식 2.
  `HeroCarry`(모든 필드)·`RunSnapshot` JSON 왕복 같음 — v0.8.1: 비교는 **다시 직렬화한 JSON 바이트가 같음** + 목록 원소별 비교(record 의 `==` 는 목록·클래스 멤버를 참조로 견줘 늘 거짓이다),
  `RunStats`·`FairnessCounters` 의 **공개 필드 + 공개 속성 전부**를 반사로 0 아닌 값으로 채워 왕복 → 바이트 같음, get 만 있는 공개 속성·허용 밖 형(값 형식·그 배열 밖)이 있으면 실패(조각 멤버가 늘어도 이 시험이 지킴 — v0.8 은 "필드" 만 봐 get 만 있는 `DamageByWeapon` 의 손실을 놓쳤다),
  **모양 지문**(v0.8.1): `RunSnapshot` 에서 닿는 형의 공개 멤버 (이름, 형) 해시 = `CurrentFormat` 옆 기대 값(3.6 "계단 저장"), `RunStats.DamageByWeapon.Length` = 6. 이 문서(3.6·6.10·6.11·6.12·6.18·8.3·1.4)의 시그니처·모양이 계약 파일에 빠짐없이 있다. `git diff` 에 실제 `Data/` 변경 없음.
- **완료 기준 — 구현과 시험까지**(10.1 `Map`·`Run`):
  거울판 — 합성 짝 표 타일셋으로 `MapMirror.Of` 의 하위 칸 성질이 원판의 전치와 칸마다 같음, `Of(Of(m))` 의 배치 = `m`(조각·좌표·문), 문 ID·변·`entries`·`anchors`·`open_center`·`room_areas` 뒤집힘, `GateZones`(거울판) = 원판 칸의 전치, `RoomFloors` 바닥 수·문 수 같음, 거울판이 13.5-19·23 ③ 통과,
  **v0.8.1**: 거울판 `entries[id].inside` 에서 가장 가까운 문 틈 칸이 `GateZones[id]` 의 칸(원판·거울판 모두 — 입구 ID 를 그대로 둔 고장 `MapMirror` 흉내 → ③ 오류), 기준점 ID 는 그대로,
  짝 표 오류 넷(성질이 전치와 다름·없는 이름·이름 두 번·`mirror: true` 인데 짝 없는 조각 — 실제 `cathedral_arena` 를 `mirror: true` 로 바꾼 사본의 오류 목록에 `grate_h`·`door_closed` 짝 없음이 **들어 있음**(포함 — 실제 `l1.json` 에는 아직 짝 표가 없어 다른 조각 오류도 함께 나오고 순서는 보지 않는다)),
  자리 짝 오류(합성 타일셋의 `edge_ne` 성질을 `edge_nw` 의 전치와 다르게 → 오류, 문 `end_b` 도 같음), 짝 조각에 원본과 다른 "보정" 을 요구하지 않음(최종 성질만 봄).
  `RunPlanner` — 풀 1장 + 거울판이면 원판·거울판 번갈이, 풀 2장 + 거울판이면 씨앗 1000개에서 바로 앞과 같은 쌍 0, 정예·보스 앞 방 출구 둘(40% 셋), 출구 하나는 계단뿐, 판의 마지막 방 출구 없음, 거울판 방의 `GateIn`·`ExitGates` 가 짝 이름, ②~④ 가 `Flow(n)` 만의 함수(v0.7.2 시험 그대로), 고정 값 시험 셋을 새 값으로.
  `RoomFlow`·`RewardSpot` — 정예·보스 방 덤 보상 + 정예 상자가 한 줄로(40px)·모두 `GatesExit`, 보상 자리: 앞이 트이면 바라보는 쪽 100px, 앞이 벽이면 다음 후보 방향, 사방이 막히면 주인공 발, 자리가 주인공에서 직선으로 이어짐(clearance ≥ 30), v0.8.1: 좁은 통로(clearance 20 인 합성 복도)에서는 두 번째 차례(문턱 = 주인공 반경)로 발 밖 자리, `DoorsOpened` 신호 자리 = `RewardSpot`, `NextFloor`(층 바뀜·계단), `BossClockTicks`, `TimedEffectsRun` 이 단계마다 맞음, `cleanup_limit` 을 rules 에서 읽음.
  `RunSession` — 계단에서 칸 +1(상한 6), v0.8.1 `RunSlots.OpenAt`(액트 1 = 4, 액트 2 = 5, 상한 6)·`RunStart` 를 액트 2 1번 방으로 준 판의 짐 칸 수 = 보통 판의 액트 2(합성 `FakeHeroCarry`), `--room` 정예 방 시작의 덤, `--start-at` 상한(일반 = 생존 길이, 정예 = 2:30, 보스 = 없음 — 넘으면 예외), `CanSuspend` 가 보스 방 문 단계에서만 참,
  **끊고 잇기**(v0.8.1 — 비교 기준을 정함): 합성 두 액트 판을 (가) 끊지 않고 **(나) 가 `Suspend` 한 틱에 `CommandButtons.Exit`(계단 문)로 나가게** 하고, (나) 보스 방 문 단계 첫 틱에 `Suspend`(ⓐ — 한 틱 돌림) → JSON → 이어 하기 생성자로 돌려
  `RoomRecord` 목록(보스 방 `Ticks` 포함)·`ElapsedTicks`·`RunStats`·방 상태 열(주인공·방 시계·단계·난수 상태 — WP7 `StateHasher` 전의 대리, 13.19)이 같음. (다) 계단 문 틈을 밟아 `Phase == Exit` 가 된 틱에 `Advance` 대신 `Suspend`(ⓑ)도 같은 비교.
  불일치 — `rooms_hash` 를 바꾼 정의, 짐에 없는 무기 ID, `Format` 다름, `IHeroCarry` 를 등록한 판에 `Carry: null` 스냅숏, `Validate` 가 이유를 돌려주는 가짜 → 모두 `RunSnapshotMismatchException` 이고 `Restore` 는 불리지 않음.
  `RoomEvents` — `--start-at` 이 입장 이벤트 시각을 넘으면 그 이벤트가 시작 시각에 있음(정예 방 생존이 끝남), `CheckPlacement` 오류(0:20 전 `ring`)·경고(2번 방 끝 0:45 안 절정 없음)·예외(`miniboss` 0:03 은 통과).
  `RemapEntry`·`RemapMarch`(v0.8.1 — 3.6 결정 5): `gate_sw` 로 들어온 방에서 `gate_ne` 는 그대로(옛 시험 `== "gate_sw"` 를 뒤집음), 들어온 문 `gate_sw` 는 `gate_ne`, 6:30 행진은 `gate_se` 로 들어오면 `gate_nw` → `gate_se`·`gate_sw` 로 들어오면 그대로, 시작 문이 다른 행진의 목표가 들어온 문이어도 그대로,
  거울판 보스 방(`gate_se` 로 들어옴)의 보스 `gate_ne` → `gate_nw`, 맞은편 문이 없는 합성 맵에서 들어온 문인 시작 문은 그대로(13.5-23 ⑤ 가 오류로 잡음).
- **옛 모양 점검**: `rg -n "RemainingMarker|remaining_marker|RewardAnchor|reward_anchor|ChestRing|chest_ring|BlessingCards|blessing_cards|RewardKind\.LevelUp|level_up\"|FullHeal|full_heal|SkipWhenHpAtLeast|skip_when_hp|filler_gold|ChestItemKind\.Gold|SlotCount = 4|WeaponSlotCount|HeroCarry\(" src tests tools` 결과를 줄마다 새 모양으로 확인해 완료 보고에 적는다
  (`level_up` 은 `rules.level_up` 키라 남는다 — 보상 종류 쓰임만 없어야 함).

#### WP1g — 계약 갱신: 기획 v0.9·v0.9.1(사용자 결정 V1~V12) + 단계 1 구현·통합이 쌓은 요청 (v0.9) — 버그 수정·방 맵 마무리를 main 에 합친 뒤, 단계 2 묶음 앞, 혼자, **형과 순수 로직 구현까지 한 번에**

기획서 33절 "설계에 넘기는 것" 1~8 과 34절 9~15, 그리고 단계 1 구현 기록(13장 끝 WP7·WP2·WP3·WP4·WP0·WP8·WP6 소절과 "통합 기록")의 **WP1g 요청**을 한 묶음에 모은다. 새 계약 모양은 6.19(장비·상인·물약)·6.18(문 보상) 이 정본이다.
WP1f 와 같은 까닭으로 형과 구현을 한 작업에서 낸다 — 바뀌는 순수 로직(`Sim/Run/**`·새 `Sim/Items/**`)은 WP1e·WP1f 가 쓴 파일이고, 단계 2 묶음은 아직 그 위에 코드를 쓰지 않았다.
단계 1 코드가 이미 있으므로 **"형만" 줄도 그 형을 쓰는 단계 1 코드의 컴파일을 고치는 데까지**는 WP1g 몫이다(옛 이름을 지우면 쓰던 자리를 새 이름으로 — 동작 본문은 그 묶음).

| # | 파일·형 | 바꾸는 것 | 구현 | 기획 # | 영향 |
|---|---|---|---|---|---|
| 1 | `Defs/RewardDefs.cs` | `RewardKind` = `WeaponCards`·`WeaponBundle`·**`HealNextRoom`**·`Shrine`·**`Merchant`**(JSON `heal_next_room`·`merchant` — `BeltPotions` 없앰), `RewardDef.Object` null 허용(`HealNextRoom`·`Merchant` 만), `RewardDef.Merchant`(`DefRef<MerchantDef>?`)·`Drop`(`DefRef<PickupDef>?` — V8 상자 → 책), `RewardDef.BeltPotions` 없앰, `RewardBundleDef.BeltPotions` → `Potions`, 로딩 검사(종류마다 칸 — 13.5-23 ④) | 형 + 읽기 시험 | 33-2·34-12·34-13 | WP3·WP4·WP6·WP7·WP8 |
| 2 | 새 `Defs/ItemDefs.cs`·`Defs/MerchantDefs.cs` | `EquipKind`·`EquipSlot`·`ArmorWeight`·`ItemBaseDef`·`AffixDef`(범위·단계 — V7)·`MerchantDef`·`MerchantStock`·`PotionUpgradeDef`(6.19), `GameDefs.ItemBases`·`Affixes`·`Merchants`, `Kinds` 에 `items`·`affixes`·`merchants` | 형 + 읽기 시험 | 33-1·34-10·34-13 | WP3·WP6·WP7 |
| 3 | `Defs/RulesDefs.cs` | `rules.equipment`(`EquipmentRulesDef` — 마법 확률·접사 둘·값 굴림·방어 상한·무게 이동 표·하한·다시 굴리기), `rules.hero.potions`(`PotionRulesDef`), `rules.rooms.rewards`(`RewardRulesDef` — 회복 문 빠짐·보장·저체력 가중, 상인 첫 방·액트 상한·보장 방·만난 뒤 배율·페핀 배율), `rules.hero.belt` 없앰, `pickup_radius` 첫 값 80 은 데이터 | 형 + 읽기 시험 | 33-1·33-2·34-9 | WP3·WP4·WP7 |
| 4 | 새 `Sim/Items/EquipItem.cs`·`ItemRoller.cs`·`HeroLook.cs` | `EquipItem`·`RolledAffix`, `ItemRoller.Roll`/`RollMagic`(난수 수 고정·5 단위 값), `HeroLook.Key`(V10 — 무게 글자 + 든 것 글자) | **구현과 시험까지** | 34-10·34-11 | WP3·WP5·WP6 |
| 5 | `Defs/StatKeys.cs`, 새 `Sim/Items/EquipStats.cs` | 장비 능력치 출처 `StatSources.Equipment`, 합산 뒤 자르기(방어 상한·무게 이동 하한·쿨 상한) 순수 함수 `EquipStats.Sum(equip, defs, rules) → StatBlock`, 최대 체력 바뀜 규칙 `EquipStats.RebaseHp` | **구현과 시험까지** | 33-1·34-10 | WP3 |
| 6 | `Sim/Run/RunState.cs` | `HeroCarry` 에 `Equipment`(`IReadOnlyList<CarriedEquip>` — `EquipSlot` + `EquipItem`, 칸 순)·`Gold`(지갑)·`Potions`·`PotionSlotsLevel`·`PotionSpeedLevel`·**`MaxedAtTick`**(무기 칸별 — WP3 요청: 방을 넘으면 만렙 순서가 칸 순서로 바뀌던 것), `Belt` 없앰. `RunState.HealOnEnter`·`MerchantsMet`(상인 ID 서수 순)·`MerchantRoomsThisAct`·`MerchantShownThisAct`·`LastRoomWasMerchant`, `RoomRecord` 끝에 `HpIn`·`GoldSpent`·`Shop`(상인 ID 또는 null)·`Potions`·`Drunk`(기본값 있음 — 기획 15절 `ROOM` 새 열) | 형 + 왕복 시험 | 33-1·33-5·34-9 | WP3·WP6·WP7 |
| 7 | `Sim/Run/RunSnapshot.cs` | `MerchantsMet` 더함, `CurrentFormat` 1 → **2**(짐·`RunStats` 모양이 바뀜 — 옛 `run.json` 은 불일치로 지움, 8.3), 모양 지문 기대 값 갱신, `Suspend` 가 `HealOnEnter == false` 단언·회복 중 물약을 바로 채움 | 형 + **구현과 시험까지**(단언·채움) | 33-3·34-9 | WP6·WP7 |
| 8 | `Sim/Contracts/Progression.cs` | `RunStats.GoldSpent`(int), `IShop`(6.19 — `Shop`·`Buy`·`Reroll`·`Leave`·`ReplaceRing`), `ChestItem` 의 `Gold` 인자 없앰(13.22 "계약 줄이기는 WP1g"), `LevelUpOffer.Reward`(`RewardDef?` — WP6 요청 3: 건너뛰기·성소 카드 문구) | 형 | 33-4·33-5 | WP3·WP6 |
| 9 | 새 `Sim/Contracts/ShopState.cs`, `Sim/Contracts/RoomState.cs` | `ShopState`·`ShopCard`·`ShopCardKind`, `RoomState.Shop`(방 수명), `RewardPickup.Stage`(`Closed`·`Dropped`)·`Chosen`(V8 상자를 연 틱에 정한 무기) | 형 | 33-4·34-12 | WP3·WP5·WP6 |
| 10 | 새 `Sim/Run/ShopStock.cs` | 상인 물건 정하기 `ShopStock.Create(merchant, act, rules, defs, seed, act, room, rerolls)`(상인 전용 흐름 — 방 난수를 쓰지 않음)·`Reroll`(산 카드 자리 지킴)·페핀 카드(단계 값·다 올림·채우기 값) | **구현과 시험까지** | 33-4·34-13 | WP3·WP6 |
| 11 | `Sim/Run/RunPlanner.cs`(보상 뽑기)·`RoomFlow.cs` | 문 보상 거르기(회복 문 75%·상인 다섯 조건·성소 한 번), **보장**(회복 문 40% → 첫 문, 상인 액트 보장 → 둘째 문, 겹치면 회복이 첫 문), 가중 바꿈(회복 문 저체력 30, 만난 뒤 다른 상인 ×1.5, 페핀 ×1.5) — 문이 열리는 틱 상태의 함수, 난수 수 고정. 회복 문 적용 = 들어옴 첫 틱 | **구현과 시험까지** | 33-2·33-3·34-13 | WP4·WP6 |
| 12 | `Sim/Contracts/RunSetup.cs` | `CommandButtons.Drink = 1 << 10` | 형 | 34-9 | WP3·WP4·WP6 |
| 13 | `Sim/Contracts/Presentation.cs` | `CueKind.BeltPotion` 없앰, `PotionDrink`·`PotionDenied`·`HealOnEnter`·`ShopOpen`·`ShopBuy`·`ShopDenied`·`ChestDrop`(상자 → 책) 더함 | 형 | 34-9·34-12 | WP3·WP6·WP8 |
| 14 | `Defs/HeroClassDefs.cs` | `HeroClassDef.Looks`(키 아홉 → 그림 묶음)·`Look`(대신 쓸 키, 기본 `lt`) | 형 + 읽기 시험 | 34-11 | WP3·WP5·WP7 |
| 15 | `Defs/PickupDefs.cs` | `PickupEffect.PotionStack`(JSON `potion_stack` — `healing_potion` 의 `heal` 을 바꿈), 집기 조건 "물약 칸이 가득이면 안 집힘", 바닥 그림 `Floor`(V8 — 떨어짐 프레임) | 형 + 읽기 시험 | 34-9 | WP3·WP5·WP7 |
| 16 | `Assets/ImportTable.cs` | 표 새 묶음: `ctrlpan`(spelicon 칸 크기·자르기 사각형), `items`(바닥), `inv`(objcurs 프레임 표), `towners`, 주인공 아홉 벌 — 6.19 가져오기 표. 표 해시가 새 표를 이음 | 형 + 읽기 시험 | 33-7·34-14 | WP0·WP5·WP7 |
| 17 | `Sim/Entities/Enemy.cs`(공통 조각) | **`InvulnUntilTick`** — WP4 조각에서 옮김(WP4 요청 1·통합 기록 중간), WP3 `DamageResolve` 가 읽는 자리를 공통으로 | 형(옮기기 — 본문 그대로) | — | WP3·WP4 |
| 18 | `Sim/Contracts/Damage.cs` | `DamageRequest.Cause`(`DamageCause` — 투사체·적 몸·장판 — WP3 요청: 공정성 `hidden_hits` 를 "투사체 단계 전 요청" 으로 어림하던 것) | 형 | — | WP3·WP4 |
| 19 | 게임 `App/Contracts/AppServices.cs`·`IAudioServices` | `AppServices.Content`(`ContentAvailability`)·`AssetProblems`·`AssetRoot`(WP7·WP8·WP6 요청), `IDevHooks`(`CaptureEvent`·`ResultShown`·`AttachProfiler`·`HashRow` — WP7 요청), 화면 신호·종료 요청·창 비활성 멈춤 자리(WP6 요청 2 — `GameFlow` 우회를 계약으로), `IAudioServices : IDisposable`(WP8 요청 1), `PlayReward(RewardDef, bool open)`(WP8 요청 2, 선택) | 형(본문 WP6·WP7·WP8) | — | WP6·WP7·WP8 |
| 20 | `Sim/Navigation/INavDebugView.cs`, `Directory.Build.props` | `INavDebugView.Arena` 를 `ArenaGrid?`(WP2 요청 — 상자 내비 시험 월드), `IncludeSourceRevisionInInformationalVersion` 거짓(WP0 요청 — 커밋마다 가져오기 캐시가 깨짐) | 형 | — | WP0·WP2 |
| 21 | `TestKit/` | `FakeHeroCarry` 새 짐 필드, `TestDefs` 합성 기본 아이템·접사(범위 하나·고정 하나)·상인 셋·`rules.equipment`·`rules.hero.potions`·`rules.rooms.rewards`, 회복 문·상인 보상 | 구현 | — | 모두 |
| 22 | 문서·데이터 | 6.15 예의 `ui.room_clear: snd_magic` 지움(WP8 요청 4), 실제 `Data/` 의 `reward_potion` → `heal_next_room`·`healing_potion` → `potion_stack`(WP1g 가 계약과 함께 — 읽기가 깨지지 않게), 장비·상인·규칙 첫 파일(기획 11.6 표 그대로) | 구현 | 33 전체 | WP7 |

- **하지 않는 것**(본문은 그 묶음): 상인 창·물약 칸·장비 줄·체력 구슬 화면과 `Q` 키 연결(WP6), 상인·장비·물약 효과 본문과 능력치 반영·서서히 회복·저절로 마심 삭제·둘레 아이콘 목록(WP3), 자동 조작의 물약·상인 창·회복 문 우선순위(WP4),
  그림 묶음 아홉 벌 고르기·둘레 아이콘·구슬 액체 그리기·원작 아이콘(WP5), 가져오기 표 실제 줄과 원본 이름 확인(WP0), 상인 대사·물약 소리(WP8), `run.json` Format 2 와 로그 `ROOM` 새 열(WP7). 게임 쪽 시험 프로젝트(WP6 요청 4)는 메인 세션이 정한다 — WP1g 에 넣지 않는다.
- **완료 기준 — 형**: `dotnet build -p:SkipAssetCheck=true`·`dotnet test` 통과(단계 1 시험 모두 — 옛 이름을 지운 자리를 고친 뒤). 읽기 시험: 6.18 다섯 종류(옛 `belt_potions` → 모르는 값 오류, `heal_next_room` 에 `object` 가 있어도 되고 없어도 됨, `merchant` 에 `merchant` 칸 없음 → 오류),
  6.19 기본 아이템 18·접사 11(범위 `min > max`·`step ≤ 0`·`stage` 1·2 밖 → 오류), 상인 셋(`Potions` 상인에 `kinds` → 오류), `rules.equipment`·`rules.hero.potions`(`durations` 길이 ≥ 1, `slots ≤ slots_max`)·`rules.rooms.rewards`, 옛 `rules.hero.belt` → 모르는 속성 오류,
  `HeroCarry`(장비·지갑·물약·`MaxedAtTick` 포함)·`RunSnapshot`(Format 2) **다시 직렬화한 바이트가 같음**, 모양 지문 새 기대 값, `RunStats` 반사 왕복(새 `GoldSpent`).
- **완료 기준 — 구현과 시험까지**(10.1 `Items`·`Run`):
  `ItemRoller` — 씨앗 1,000개에서 마법 비율 액트 1 35%±3·액트 2 60%±3, 접사 둘 25%±3, 같은 쪽 둘 0, 반지 늘 마법, 값이 5 의 배수·범위 끝 굴림의 값 = 0.75×·1.25×, 같은 씨앗 두 번 같은 결과, **정의 표에 접사 하나를 더해도 다른 칸 카드 결과가 그대로**(카드마다 흐름).
  `EquipStats` — 방어 합이 상한 15 에서 잘림(판금 셋 13 + Fine 셋 3~6 = 16~19 → 15, v0.9.1 — v0.9 는 10), 판금 셋만(13)은 잘리지 않음, 지팡이 + 방패를 함께 끼우면 둘 다 합산(양손 규칙 없음), 무게 이동 −10 −4 −4 → −15% 하한, `of Swiftness` + 판금은 하한 뒤에 더하지 않음(합친 뒤 자름 — 순서 시험), 최대 체력 +30 → 지금 체력 +30, 바꿔서 −20 → 새 최대에서 자름.
  `HeroLook.Key` — 아홉 조합 + 지팡이·방패 둘 다 → `t`, 갑옷 없음 → `l`.
  `ShopStock` — 같은 (씨앗, 액트, 방) 같은 물건, 다시 굴리기가 산 카드를 지킴, `rerolls` 같으면 같음, **상인 방이든 아니든 그 방의 적 생성·드롭이 같음**(방 난수를 쓰지 않음), 페핀 카드 단계 값·다 올림·채우기 = 빈 칸 × 25.
  보상 뽑기 — 체력 39% → 첫 문 회복 문(거울판이면 짝), 76% → 회복 문 빠짐, 액트 1 2번 방 문엔 상인 0, 이 액트 상인 둘 뒤 상인 0, 한 문 단계 상인 둘 0, 액트 1 4번 방 문 상인 보장(겹치면 회복 첫·상인 둘째), 보장이 있어도 다음 방들의 뽑기 결과가 보장 없는 판과 같은 난수 위치(난수 수 고정).
  회복 문 — 넘김 어둠 동안 체력 그대로, 들어옴 첫 틱에 최대, 정예 방 덤이면 그 방 들어올 때. `Suspend` — 회복 중 물약이 있으면 채운 체력이 스냅숏에 들어감, 이어 하기 뒤 같은 판과 `RoomRecord`·`RunStats` 같음(WP1f 끊고 잇기 시험에 장비·지갑·물약·만난 상인을 더해).
- **옛 모양 점검**: `rg -n "BeltPotions|belt_potions|rules\.hero\.belt|HeroCarry\.Belt|CueKind\.BeltPotion|\"heal\".*healing_potion|ChestItem\(.*Gold" src tests Data docs/architecture.md` 결과 0(이 문서에서는 "없앰" 을 적은 문장·13장 기록만 남음).
- **WP1g 뒤**: 단계 2 묶음(WP3·WP4·WP5·WP6·WP0·WP8 의 v0.9 줄)이 이 계약에 기대어 동시에 간다. 단계 2 데이터(액트 1 아홉 방·작은 방 풀 5장)는 WP0a 방식으로 버그 수정·방 맵 브랜치가 넣은 것 위에 더한다.

**앞서 쌓인 WP1g 요청 모음**(13장 끝 구현 기록·통합 기록 — 위 표의 어느 줄이 받는지)

| 낸 곳 | 요청 | 받는 줄 |
|---|---|---|
| WP7 구현 기록 | `AppServices` 에 `ContentAvailability`·`DisabledContent`, 개발 실행 훅 `IDevHooks` | #19 |
| WP2 구현 기록 | `INavDebugView.Arena` null 표시 | #20 |
| WP3 구현 기록 | `HeroCarry.MaxedAtTick`(방을 넘으면 만렙 순서가 칸 순서로), 피해 요청 원인 칸(`hidden_hits`) | #6·#18 |
| WP4 구현 기록 1·통합 기록(중간) | `Enemy.InvulnUntilTick` 을 공통 조각으로 | #17 |
| WP0 구현 기록 | `Directory.Build.props` 소스 리비전 끔(가져오기 캐시) | #20 |
| WP8 구현 기록 1~4 | `IAudioServices` 끝내기, `PlayReward`(선택), `AppServices` 자원 폴더, 6.15 예 고침 | #19·#22 |
| WP6 구현 기록 1~3 | 시작 검사 결과 자리, 화면 신호·종료·비활성 멈춤 계약, `LevelUpOffer.Reward` | #19·#8 |
| WP6 구현 기록 4 | 게임 쪽 시험 프로젝트 | 메인 세션이 정함(WP1g 밖) |
| 13.21·13.22 | `ChestItem.Gold` 계약 줄이기, 칸 그릇 6 넘김(액트 셋 이상)·`output_scale` 4 | #8 / 그대로 열린 질문(액트 셋·4배를 넣을 때) |

#### WP0 — 가져오기 도구 (WP0a 뒤, 다른 묶음과 동시)

- **시작 전**(v0.8.2, WP1f 검증 반영): 기획 담당이 13.21 끝 "기획서에 넘기는 것" 2~6(`upscaler-check` 는 작은 무늬 그림 한 장 변환, 되돌아감 이유 `Vulkan 런타임 없음`·"진행 없음 2분", 맵 그림 알파 = 원본 2배 점 확대·이음매 대처 = 벽 줄째 확대·기둥 빼기, 액트 2 시작 칸 5·`--weapons` 초과 옵션 오류, 끊는 틱 두 길)을 기획서 19.6·2.5·15절에 넣은 뒤 시작한다.
  — **받음**: 기획서 v0.8.4(32절)가 넣었고 머리말의 "기준 기획서" 를 v0.8.4 로 올렸다. 두 문서가 같으므로 조건은 풀렸다. 앞으로도 두 문서가 다르면 **설계서(1.4·3.6·9절) 문장대로** 구현한다(WP7 의 `--weapons` 검사도 같음).
- **맡는 것**: `tools/DiabloAssetImporter/**`(`Table/` 포함), `tools/import-all.ps1`, `tests/DiabloAssetImporter.Tests/**`, 테스트 `Assets/ImportNamesTests.cs`(13.5-10 — ① 기본, ② `Integration`),
  `Data/tilesets/*.json`(번호·성질 표 — 원본으로 확인하는 묶음이라), `src/DiabloDefenceRemastered/Assets/README.md`, `README.md` 의 자원 절·제3자 고지. `tools/import-assets.ps1` 은 고치지 않는다.
- **계약**: 1.4 의 명령·종료 코드·출력 폴더·manifest·메타·소리 색인(Core `Assets/` 형), 13.5-10 을 만족하는 표, 타일셋 성질 표 = 원본 SOL(+ 알려진 차이) — **v0.5: bit0·bit1·bit2 세 비트**(1.4 성질 대조), 단계 3 의 L2 협로 벽 시야 비트 포함. 맵 정의는 Core `MapDataReader` 로 읽고
  `ArenaLayout.Expand` 로 펼친다(직접 파싱·펼치기를 하지 않는다). 굽기 결과가 바뀌는 변경은 `AssetFormat.Version` 을 올린다(1.4 캐시). 부록 A 의 형식 규칙을 따른다.
- **단계**: 1 = 몬스터 5종 묶음·변종, 주인공 한 벌, 무기 3종 마법·아이콘, L1 타일셋·아레나. 2 = 나머지 스테이지 1 몬스터·보스, 무기 3종, 이펙트 전부, **소리·음악**. 3 = L2 타일셋(번호 조사, 기획 열린 질문 15)·지하묘지 몬스터.
  v0.5.3 중간 보스(기획 9.4): 단계 2 에 `skelbow` 변종 `bashtb` + 동작 `special`(`s`) + 소리 `sklbws*`, 단계 3 에 `acid` 변종 `bfds` + 동작 `special` + 소리 `acids*`(`monsters\acid\`), `fat` 변종 `bsm`.
  v0.6 헬파이어 묶음(1.4 "헬파이어 묶음"): 단계 1 에 **묶음 구조**(`--hellfire` 기본 = diabdat 폴더, 묶음 MPQ 찾기·`prefer` 순서·`--skip-bundles`, 없는 MPQ 건너뛰기, manifest `skipped_bundles`·파일 `bundle`·`sources`(v0.6.1), 끝 보고) — MVP 표에는 묶음 항목이 없으므로 시험(합성 MPQ)과 이 PC 의 "네 MPQ 다 있음 / `--skip-bundles all`" 두 실행으로 확인한다.
  묶음 내용(수도사 한 벌·헬파이어 몬스터·L5·L6·헬파이어 음악·대사)은 그것을 쓰는 정의가 생기는 WP9·WP10 에 표 줄을 더한다. 단계 1 의 L1 타일셋에 안쪽 방 조각(WP0a 가 넣은 것)의 성질 대조가 든다.
  고유 TRN 경로는 `monsters\monsters\<이름>.trn`(세 파일 모두 diabdat 원본 목록에서 확인, 256바이트). 새 그림 묶음은 없고 변종 페이지만 셋 는다(R7 의 `vram_mb` 에 조금 더해짐).
- **완료 기준**: 도구 시험 통과(10.1 `DiabloAssetImporter.Tests` — 합성 바이트·시험이 만든 MPQ, 결정론·캐시·실패 시 manifest 없음).
  이 PC 실제 실행(창 없음): 단계 목록 전부 생성·종료 코드 0, 두 번 돌려 manifest 같음, 두 번째가 캐시로 빠름(두 시간을 1.4 에 적음), 원본 폴더 파일들의 수정 시각 그대로,
  일부러 없는 경로 하나를 표에 넣으면 종료 코드 3·manifest 없음. `contact` 견본(좀비 원색·`bluered`, 파이어볼 16방향, L1 아레나 바닥 + 벽, 마법사 시전)을 스크래치에 만들어 **눈으로 봄** 을 기록.
  실제 원본 실행이 PKWARE implode·암호화·파형 압축 경로를 덮는다(합성 MPQ 시험이 못 덮는 것, 10.1 `Pipeline`) — 두 번 같음이 그 확인이다. 첫 실행·캐시 실행 시간과 묶음별 페이지 수·크기를 1.4 에 적는다.
  `audit` 가 종료 코드 0(추적 파일에 manifest 출력·원본 항목과 같은 해시 없음, 확장자 목록 없음, `Assets/d1/**` 없음 — R24).
  v0.6: 이 PC 실행 끝 보고에 묶음 넷 "만듦", `--skip-bundles all` 실행(다른 `--out`, 저장소 밖)이 종료 코드 0·넷 "건너뜀"·기본 항목 출력 해시가 같음. 묶음 MPQ 의 원본 줄(`sources`)이 manifest 에 있음(크기·SHA-256 — R18).
  v0.6.1: 쓸 수 없는 묶음 MPQ 건너뛰기(1.4 — `판 다름`·`열 수 없음`, `--hellfire` 직접 지정일 때만 2·3)와 실패 문구의 우회 명령, `import-all.ps1` 의 `-Hellfire`·`-SkipBundles`, 타일셋 묶음을 건너뛰면 그 맵 굽기도 건너뜀, 파일 줄 `sources`.
  `RealMpq` 를 `DDR_HELLFIRE_DIR` 로 이 PC 에서 돌려 결과(묶음마다 대표 항목이 열림)를 완료 보고에 적는다. `audit` 의 DUN 배열 대조가 실제 `cathedral_arena` 에 종료 코드 0(거짓 실패가 나면 기준 상수를 고치고 그 이유를 1.4 에 적음).
- **v0.7(방 구조, 기획 19.2·28절 넘긴 것 11)**: 맵 굽기는 맵 정의마다라 바뀌는 코드는 없다 — 굽는 목록이 `Data/maps/*.json` 전부이므로 작은 방 맵(단계 2 대성당 2장, 단계 3 지하묘지 2장 + `catacombs_arena`)이 생기면 저절로 굽는다. `--jobs` 와 캐시로 시간을 1.4 에 적는다.
  **원작 물체·아이템 그림**(단계 2, 기획 11.4·열린 질문 32): `objects\chest1~3`·`lshrineg`·`book1`(방 안 물체)·`items\fbook`·`fbttle*`(아이콘) — 먼저 `probe` 로 형식을 본다(물체는 프레임 CEL 로 알려져 있다 — 부록 A.2 의 CEL 길로 풀리는지, 칸 폭·프레임 수는 파일에서). 풀리면 가져오기 표 `Table/missiles.json`(마법 그림과 같은 "프레임 시트" 항목 모양)에 `objects\…`·`items\…` 줄을 더하는 것이 첫 안이다. 표 모양에 없는 형식(CEL 프레임 머리 등)이 필요하면 `ImportTable` 형을 바꾸는 계약 변경이므로 11.3 대로 메인 세션에 올린다(WP1e 이후의 다음 갱신 묶음).
  안 풀리면 원작 exe PNG·코드 그림 대체(단계 1 그대로)를 유지하고 열린 질문 32 에 적는다. 소리 `sfx\items\dooropen.wav`·`invbook.wav`·`readbook.wav` 를 소리 표에(6.15).
  13.5-10 귀속(v0.7): 맵 그림 → 그 맵을 `room_maps` 에 둔 스테이지들(맵 하나를 두 액트가 쓰면 두 스테이지 모두 — 묶음 조건은 둘 다 만족해야 함).
  완료 기준 더함: 단계 2·3 의 작은 방 맵이 `layout_hash` 대조를 통과하고 `audit` 의 DUN 배열 대조 0, 물체 그림을 풀었다면 `contact` 견본에 상자·성소·책·물약을 넣어 눈으로 봄.
- **v0.7.1**(13.18): 13.5-22 의 맵 주석 `// 본뜬 원작 방:` ↔ 타일셋 항목 `audit_dun` 대조를 `SourceCommentTests` 에 더함(자원 없이 늘 돎 — 6.11 "원작 방 본뜨기").
- **v0.8**(13.20, WP1f 뒤): ① **AI 확대**(사용자 결정 U, 1.4 "AI 확대"): `--upscale`·`--upscale-bin`·`--upscale-gpu`·`upscaler-check`, `Table/upscale.json`(허용 해시는 공식 릴리스 파일에서), 처리 ①~⑦·되돌아감·종료 코드 6·manifest `upscale`·파일 `scale`·메타 `scale`(형식 판 3)·그림자 굽기(⑥), v0.8.1: 세 단계 처리·묶음 실행·진행 감시·확대 중간 캐시·맵 알파 점 확대·몬스터 알파 변종 공통·Vulkan 런타임 확인·오류 창 막기·`-m`·`확대 진척 n/m`, 도구 시험(10.1 `Pipeline` — 가짜 실행 파일 `tests/FakeUpscaler` + `IUpscaler` 가짜), `import-all.ps1` 의 `-Upscale`·`-UpscaleBin`·`-UpscaleGpu`(0·6 = 성공), README 자원 절(받는 곳·풀 곳·제3자 고지).
  ② **거울판 굽기**: `mirror: true` 맵의 거울판(`MapDataReader` 결과의 `Mirrored`)도 굽는 목록에 — 출력 `<art>@m`, `layout_hash` 따로. `audit` 의 DUN 배열 대조도 거울판 배치로 한 번 더(기획 v0.8.1 13.5-22).
  ③ **L1 짝 조각**(`Data/tilesets/l1.json`): 세로 창살(`grate_v` — 35 후보)·세로 닫힌 문(`door_closed_v` — 25 후보)의 번호·성질을 `probe tiles` 로 원본에서 확인해 `pieces`·`props` 에 더하고 `mirror` 짝 표를 적는다(6.11 예). 짝 검사(13.5-12 — 최종 성질 전치·자리 짝)가 통과해야 한다.
  v0.8.1(기획 v0.8.3 10.1 "세로 창살"): 35 의 기둥 칸 성질은 원본 SOL 과 DevilutionX 보정 여부로 셋 중 하나 — 보정이 있으면 `sol_known_diffs`(출처: DevilutionX 참고), 보정이 없으면 `design_diffs`(이유 "창살 기둥 칸은 방향과 상관없이 막음, 거울판 짝"),
  35 의 원본 모양이 36 의 전치가 아니면(기둥 칸 자리·걷기 칸이 다름) 짝 표에 넣지 않는다 — 그러면 창살을 쓴 `cathedral_arena` 는 거울판을 달지 않는다. 짝 조각에 원작 수정판에 없는 "보정" 을 지어내지 않는다(R24). 그 뒤 WP5 가 `cathedral_arena` 에 `mirror: true`.
  완료 기준 더함: 이 PC 에서 `--upscale realesrgan-x4plus` 실제 실행 한 번(저장소 밖 `--out`) — 종료 코드 0, manifest `status: done`, 확대 작업 수·걸린 시간·첫 실행과 캐시 실행 시간을 1.4 에 적음, `contact` 견본(L1 바닥·벽, 칸 160 이상 몬스터 하나, 같은 몬스터 걷기 8프레임 줄)을 스크래치에 만들어 **눈으로 봄**(이음매·가장자리·떨림)을 기록.
  `--upscale-bin` 을 빈 폴더로 준 실행이 종료 코드 6·`status: fallback`·출력이 원본 픽셀 실행과 같은 해시. 확대를 끈 실행은 지금 기준(두 번 → manifest 같음) 그대로.
  v0.8.1: 실제 실행의 묶음 수·묶음별 시간·전체 시간으로 1.4 의 `UpscaleBatchPixels`·`UpscaleNoProgress` 첫 값과 기획 19.6 의 "첫 실행 수 분" 을 고친다(값이 바뀌면 기획 담당에 넘김). 오류 모드를 물려받아 `vulkan-1.dll` 없는 실행에서 창이 뜨지 않는지(이 PC 에서 꾸밀 수 없으면 "확인 못 함"), `upscaler-check` 의 장치 줄을 완료 보고에 적는다.
- **v0.9**(WP1g 의 표 모양 뒤 — 원본 이름 확인이 먼저라 WP1g 와 나란히 시작해도 됨): 6.19 가져오기 표 — `ctrlpan\spelicon.cel`(56×56, 프레임 머리 없는 CEL — **됨**, 코드 `9128cc9`: 무기 셋의 칸 12·39·42, 굽기 버그 고침, 가산 굽기는 하지 않음 — 6.19. 남은 무기 셋의 칸은 그 무기 데이터를 낼 때), `panel8`·`p8bulbs` 자르기, `items\fbook`·`fbttle*`·`goldflip`, `objcurs` 아이템 프레임 표, `towners` 셋, 주인공 아홉 벌. 원본 목록으로 이름을 확인해 기획 34절의 기억한 이름을 고쳐 적는다.

#### WP2 — 길찾기·충돌·공간

- **맡는 것**: Core `Geometry/`, `Nav/`(`ArenaCollision`, `Navigator`, `PlacementResolver`, `NavConst`, `Validation/` = 13.5-4(도형 거리 쪽),5,6 검사기, `NavModule.cs`), `Spatial/`, `Sim/Movement/`(+ `Enemy.Nav.cs` 조각, `Invariants/`),
  `Sim/Visibility/VisibilityField.cs`(v0.5 — WP1c 가 구현·시험까지 끝낸 뒤 소유만 넘겨받는다. 고칠 일은 성능·버그뿐이고 질의 뜻을 바꾸면 계약 변경, 11.3. 같은 폴더의 `FogMath.cs` 는 WP5 — 그리기 규칙).
  테스트 `Geometry/ Nav/ Spatial/ Movement/ Invariants/ Visibility/`.
- **계약**: `ArenaCollision.Build(ArenaGrid) → IReadOnlyList<CollisionShape>`(4.2 — `CollisionShape` 는 WP1b 계약 형, `INavDebugView` 로 WP5 가 읽는다. `Geometry/` 다각형은 이것에서 변환), `NavWorld.Build(World) : INavWorld`(외곽 상자 격자·바깥 마름모 경계), `SpatialHash : ISpatialQuery`, `ObstacleIndex`, `Navigation / Locomotion / Collision / SpatialRebuild` 시스템,
  이동 의도 → 희망 속도(4.4 끝), `Enemy.MoveBlockedTime`·`Hero.TouchingCount` 쓰기, `INavDebugView`(충돌 도형·격자·필드·경로·하위 칸 성질), `InvariantChecker`(`SpatialRebuild` 안에서 60틱마다, 10.1).
  v0.6(안쪽 방): `INavWorld.PathLength`(플로우 필드 거리 / 자른 A*, 4.10)·`PlacementResolver.TryPlace(…, PlaceReach)`, 13.5-5 **갇힌 바닥 칸**(6.14)·26·34 등급 → 모든 `room_*`, 13.5-6 진입점 60px, 창살 `w` 칸(걷기 도형만).
- **완료 기준**: 테스트 통과(무작위 A* 최적성, 해시 = 전수·넘침, **실제 맵의 길 검사·전수 성질 — 자원 없이**(단계 1: WP0a 가 넣은 `cathedral_arena`), 겹친 점, 둘레 도형이 `phase_through` 를 막음, `ArenaCollision` 의 평행사변형 합집합 = 막힌 칸 합집합(점 표본)). 그래픽 없는 측정: 적 500 을 맵에 흩어 플로우로 주인공에게 모으는 3,600틱이
  Navigation+Locomotion+Collision 평균 ≤ 1.0ms(4.9 표), 불변식 기준 통과. 단계 3 전까지는 스테이지 2 맵 대신 시험용 합성 맵으로.
  v0.6: 10.1 `Nav`·`Movement` 의 v0.6 시험(WP1d 합성 방 맵 — 갇힌 띠가 16px 격자에서 잡힘, v0.6.1: 맵 ⑤ 의 모서리 구멍이 13.5-4 80px 로도 잡힘), 실제 `cathedral_arena`(안쪽 방 배치)가 13.5-4~6 통과(갇힌 칸 0, 26·34 가 모든 `room_*` 까지 — v0.6.1: 구역 둘 포함), 위 측정을 안쪽 방 배치로 다시(필드 통과 칸이 조금 준다).
  v0.6.1: `PathLengthToHero`(필드 등급·통과 칸 조건, 아니면 A*)·`TryPathStep` 본문, `InvariantChecker` 의 `occluded` 열(10.1 공정성).
  **v0.7(방 구조)**: 내비·공간 해시·충돌 도형은 `World` 마다 새로 만든다(3.6 결정 1 — 지금 설계 그대로, 되돌리기 경로를 만들지 않는다). 할 일은 **잼과 검사 범위**다:
  `NavModule` 설치(충돌 도형·`ObstacleIndex`·clearance·첫 필드)가 방 이동 예산(10.3 "방 이동 시간" — 큰 방·작은 방 각 ≤ 150ms, 그중 내비 몫을 따로 적음) 안인지 `Perf` 로 잼, 13.5-4~6 을 **모든 액트 풀의 모든 맵**에(작은 방 N 32 합성 맵 포함),
  작은 방 문 넷 진입점 150px 은 WP1e 의 하위 칸 근사(13.5-23 ③) 위에 실제 충돌 도형 거리로 한 번 더(정밀 검사 — 13.5-6 의 60px 와 같은 함수에 거리만 바꿈), `InvariantChecker` 열을 방마다 → `RunState.Fairness` 합(WP1e `AddTo`).
  완료 기준 더함: 작은 합성 방 맵에서 10.1 `Nav` 전수 시험(a)~(d), 방 이동 `Perf` 두 맵 기준 안, R23 입구 대체 비율을 작은 방(32)에서도 잼(기획 열린 질문 35 — 30% 넘으면 적어 넘김).
- **v0.7.1**(13.18): 13.5-23 ③ **문 틈 닿음** 검사(Nav/Validation — 13.5-5 의 닿는 내비 칸 집합으로 문마다 나가는 자리 칸 하나 이상, 10.1 `Nav`). 방 맵 풀의 맵마다 돈다.
- **v0.8**(13.20): 13.5-4~6·문 틈 닿음·작은 방 150px 정밀 검사를 **거울판에도**(`MapDef.Mirrored` 가 있는 맵마다 한 번 더 — 6.11 "거울판"). 내비·충돌은 거울판 `MapDef` 를 그대로 받으므로 코드 변경은 검사 목록뿐이다. `RewardSpot`(WP1f)이 쓰는 `INavWorld.IsInsideMap`·`Clearance` 의 뜻을 바꾸지 않는다.
- **v0.9**(WP1g 뒤): 기획 v0.9·v0.9.1 의 새 일 없음(상인은 충돌 없는 물체, 무게는 이동 배율이라 길찾기와 무관). `INavDebugView.Arena` null 표시(WP1g #20)에 맞춰 디버그 그리기 쪽 null 처리만.

#### WP3 — 주인공·전투·무기·성장

- **맡는 것**: Core `Sim/Hero/`, `Sim/Combat/`, `Sim/Weapons/`(`IFirePattern`·`ITargeting`·`WeaponStats`·`CombatModule.cs`), `Sim/Projectiles/`, `Sim/Hazards/`, `Sim/Pickups/`, `Sim/Progression/`,
  이 폴더들의 조각 파일(`Enemy.Combat.cs`, `Hero.Combat.cs`, `World.Combat.cs`, `WeaponPatternState.Combat.cs`, `RunStats` 조각 …).
  Data `weapons/`, `passives.json`, `evolutions.json`, `pickups.json`, `drop_tables.json`, `rules/hero.json`, `heroes/`(v0.6 — MVP 는 `sorcerer.json` 하나). 테스트 `Combat/ Weapons/ Progression/ Hero/`.
- **계약**: 발사 패턴 6종·조준 5종·무기 효과 부품 등록, `DamageMath`, `HeroControl`·`Weapons`·`Projectiles`·`Hazards`·`DamageResolve`·`DeathRewards`·`Pickups` 시스템,
  `ICombatSpawner`·`IProgression`·`IHostileQuery`(MVP: 주인공만) 구현, `RunStats` 채우기, `IRunSetup`(`--god`·`--level`·`--weapons`·`--passives`).
  v0.3: 무기 `cast_clip` 재생, 투사체 `Vel` 에 그림 방향(도는 구체는 접선), **`ProjectileDef.OnEnd` 장판**, 신호 `WeaponFired`·`EnemyHit`·`HeroHurt`·`HeroDied`·`PickupCollected`·`PickupDropped`(7.9).
  v0.5: 무기 조준은 시야를 보지 않는다(기획 5.1 — 바꾸기로 하면 조준 후보 거르기에 `World.Visibility.IsVisible` 하나, 기획 열린 질문 23).
  v0.5.1: 공정성 수 세기 — `ICombatSpawner` 의 적 진영 투사체 생성에서 `Fairness.UnseenShots`, `DamageResolve` 의 주인공 피해에서 `Fairness.HiddenHits`(10.1 공정성 열). 피해 요청에 원인 종류(적 본체 / 투사체 / 장판 / 예고 패턴)가 있어야 가른다.
  v0.6(직업): 주인공 만들기는 `World.HeroClass` 만 읽는다(능력치 = `rules.hero.stats` → 직업 덮어쓰기 → 특성 더하기, 시작 무기, 그림 묶음 — 6.16), 시전 클립을 직업 그림 묶음에서(`attack` 포함, 5.3),
  레벨업 후보·상자·`--weapons` 가 무기 `classes` 와 `requires ⊆ 스테이지 ∪ 직업`(6.8)으로 거름, `heroes/sorcerer.json`(특성 마력 친화 `cooldown_pct` +0.05 — 기획 4.1). `melee_arc` 는 WP9.
  v0.6.1: 특성 = `trait` 패시브(`passives.json` 의 `trait_arcane_affinity`, 아이콘 없음 — 6.16), 주인공 만들기가 `HeroClass.Traits` 를 1레벨로 붙이고 슬롯을 쓰지 않음, 레벨업·상자·`--passives` 후보에서 `trait` 를 뺌, 패시브·진화 `classes` 거르기.
  v0.6.3(방 보급): `ICombatSpawner.SpawnPickup` 구현(드롭도 같은 길, `PickupDropped` 신호), `Pickup.SupplyRoom` 채우기, 보급은 수명·상한 정리에서 뺌, 주우면 틱 이벤트 `SupplyCollected`(6.12).
  **v0.7(방 구조, 6.9·6.18·3.6)**: `IHeroCarry` 구현과 `ModuleContext.SetHeroCarry` 등록(레벨·경험치·재추첨 남은 수·체력·무기·패시브·축복 칸 — 무기 내부 시간 상태·버프·마력 폭주·피격 무적은 넘기지 않음), 문 보상 — 획득물 `kind: reward`(몸에 닿아야, 끌어당김·수명·상한 정리 밖, `Pickup.Reward`·`GatesExit` 채우기),
  줍기 효과(v0.8 에 바뀜 — 아래 v0.8 줄. v0.7: 마법서·성소 → `PendingRewards`, 보물 상자 → `XpToNext` 만큼 경험치 + 금화 × `gold_scale`, 물약 → 전부 회복), `IProgression` 의 `RewardOffer`(무기 카드만 — `cards` 가중 / 축복 카드만)·`ChooseReward`·`RerollReward`·`SkipReward`(`skip`)·`RewardAvailable`, 재추첨 수 공유,
  **축복**(`blessing` 패시브 — `Hero.Blessings`, 능력치 더하기, 레벨업·상자·`--passives` 후보에서 빠짐. `--passives` 로 축복을 주면 축복 칸에), `RoomCleared` 다음 틱 보석·금화 끌어당김(끌어당김 룬 함수)·보스 `RoomCleared` 면 체력 전부, 신호 `RewardCollected`.
  Data: `rewards.json`(6.18), `passives.json` 에 축복 여섯(기획 6절 값), `pickups.json` 에 보상 물체 넷(단계 1 은 대체 그림 — 기획 11.4: `GoldIcon`·`HealingPotion`·`MagicMissileICon`·코드 그림). 단계 1 = 마법서·보물 상자, 단계 2 = 성소·물약·축복.
- **완료 기준**: 테스트 통과. 그래픽 없는 시나리오(`Scenario`): 해골 50 마리 + 매직 미사일 Lv8 로 60초 → 처치·경험치·레벨업 대기가 생김(`TestKit` 공간 질의로 먼저, WP2 가 들어오면 실제 해시로도).
  `on_end` 시험(맞음·사거리 끝·장애물 세 경우 모두 장판 하나), 신호 시험(무기 발동 한 번에 `WeaponFired` 하나, 피격 무적 중 `HeroHurt` 없음). v0.6: 10.1 `Progression` 의 직업 시험, 실제 `heroes/sorcerer.json` 으로 만든 주인공의 쿨다운 감소 0.05.
  v0.7: 10.1 `Progression` 의 "문 보상·축복·짐" 시험, 합성 `RoomState`(TestKit #23)로 `RoomCleared` 끌어당김, `RunSession`(WP1e-구현 뒤)으로 방 둘을 지나 빌드가 같은지.
- **v0.7.1**(13.18): `IHeroCarry` 는 칸 번호 그대로(`Restore` 가 같은 칸), 대기가 남은 월드의 `Capture` 는 예외, `SpawnPickup` 이 `PickupSpawn.Reward != null` 이면 `GatesExit = true`, 끌어당김 표시는 공통 조각 `Pickup.Magnetized` 에, 보상 물체는 상태와 상관없이 늘 집힘(물약 포함)·줍는 순간 줄 카드가 0장이면 그 보상의 `skip`, `ResetCombat`·주인공 만들기는 `RunStats` 를 건드리지 않음(10.1 `Progression`).
- **v0.8**(13.20, WP1f 뒤 — 기획 v0.8.1 4.3·4.4·11.1·11.4·2.5): 문 보상 효과 넷(6.18 "받는 길" — 무기 강화 꾸러미·허리띠 물약·성소 셋, 0장·올릴 무기 없음·정예 상자 빈 강화(`ChestItemKind.Temper`) = `단련`), `RewardAvailable` 의 v0.8 거르기, 매혹의 성소 창(`OfferSource.Shrine`),
  **허리띠**(`Hero.Belt`, 30% 아래 저절로·1초 간격), **열린 칸 수**(`Hero.WeaponSlotsOpen`·`PassiveSlotsOpen` — 레벨업·상자·`--weapons` 가 이 수만큼만 채움), `Mods`(`단련`·성소 대가 — 능력치 더하기, `max_hp_pct`), **시간 효과**를 남은 틱으로 세고 `World.TimedEffectsRun` 일 때만 줄임,
  `IHeroCarry` 가 새 짐 필드 전부(`Effects` 포함 — 무기 쿨다운·피격 무적은 넣지 않음), `IProgression.PicksLeft`. Data: `rewards.json` 새 모양(6.18), `rules/hero.json` 의 `belt`·`temper`·`filler_temper`·경험치 곡선(6.10), 축복 패시브 여섯은 MVP 데이터에서 뺌(E).
  완료 기준 더함(10.1 `Progression`): 보상 넷의 받는 길, 0장 → `단련`, 허리띠 저절로(문턱·간격·찼을 때 물약 = 바로 마심), 성소 한 판에 한 번, 시간 효과가 보상·문·들어옴 단계에서 안 줄고 짐으로 다음 방에 이어짐, 칸 5 에서 무기 다섯.
  **v0.8.1**: 주인공 만들기가 열린 칸을 `RunSlots.OpenAt(World.Room?.Plan.Act ?? 1, …)` 로 정함(`Hero.Reset` 은 0), 5번째 칸 무기의 피해가 `RunStats.DamageByWeapon[4]` 에 쌓임, `IHeroCarry.Validate`(모르는 시간 효과 ID·진화 짝 어긋남 → 이유), **판 수명 상태를 모듈 인스턴스·정적 필드에 두지 않음**(11.2 — 짐에 필드가 더 필요하면 WP1g 요청).
  완료 기준 더함: **실제 모듈로** 합성 두 액트 판을 계단에서 끊고(`Suspend` → JSON → 새 `ModuleContext` 로 이어 하기) 끊지 않은 판(같은 틱 계단 문 `Exit`)과 `RoomRecord` 목록·`RunStats`·짐(다시 쓴 JSON 바이트)이 같음 — WP1f 의 `FakeHeroCarry` 시험이 못 덮는 "짐 밖에 숨은 판 상태" 를 잡는다(WP7 `StateHasher` 가 생기면 판 해시로도).
- **v0.9**(WP1g 뒤, 6.19·기획 11.4~11.7·34절): 장비 능력치 반영(`EquipStats` 를 주인공 능력치 출처로)·최대 체력 바뀜, 지갑 금화 줍기, 물약 칸(`potion_stack` 줍기 조건·`Drink` 명령·서서히 회복·저절로 마심 경로 삭제), 회복 문 효과(들어옴 첫 틱), 상인 창 `IShop`(사기·다시 굴리기·떠나기·반지 바꾸기), 보물 상자 → 책 두 단계와 여는 틱의 무기 고르기, 둘레 아이콘 목록 `RewardOsd.Icons`, `MaxedAtTick` 짐. 완료 기준: 6.19 규칙마다 시험 하나 + 실제 모듈 끊고 잇기에 장비·지갑·물약.

#### WP4 — 적·감독·자동 조작

- **맡는 것**: Core `Sim/Enemies/`(생애 상태·부품 계약과 구현·보스 패턴·페이즈·등급·접두사·버프 적용·`EnemyFactory`·`EnemyModule.cs`·`Enemy.Behavior.cs` 조각), `Sim/Director/`(+ `World.Director.cs` 조각), `Sim/Autoplay/`.
  Data `enemies/`, `affixes.json`, `buffs.json`, `stages/`, `rules/enemies.json`. 테스트 `Enemies/ Director/ Sim/`(결정성·시나리오·Perf), `Fixtures/variants/`, `Fixtures/perf_scenario.json`.
- **계약**: 부품 등록(일반 13 + 동작 7 + `TriggeredAction`·`PatternAbility`·`PhaseController`), 이벤트 종류 9개(`PartKind.StageEvent`), `Director`·`EnemyThink`·`DeathHooks` 시스템, 승리 처리(3.5),
  `World.ViewRect` 를 쓰는 생성 위치, `AutoplayPolicy`(v0.6.1: 인스턴스 `new AutoplayPolicy(visit)` 의 `Move(World) → Vector2`, `PickCard(offer) → int`, `Events`·`RoomStats` — 메뉴 넘김은 WP6), `IRunSetup`(`--start-at`·`--stress`·`--freeze-ai`), `Visual`·`CorpseSpawned` 값 채우기,
  `World.Lighting`·`BossBars`·`Banner` 쓰기.
  v0.3: 적 17종 + 보스 4 데이터(그림 묶음·변종, 6.13), 새 부품 `panic_on_ally_death`, `phase_through.alpha_over`, 신호 `EnemyAttack`·`EnemySpecial`·`EnemyShot`·`BossArrived`·`BossPhase`·`ElitePack`, 단계 1 판 스테이지(6.12).
  v0.5: `EnemyCtx.SeenClearly` 와 `shoot_projectile`·`kite`·`charge` 의 시야 조건(6.6), 13.5-15 검사기(`kite` 띠 최대, 6.14), `include_bosses: false` 확정(데이터에 `true` 없음 — v0.5.1 경고 검사).
  v0.5.1: `SeenClearly` 의 몸 위 끝(`Enemy.BodyTop` = `body_height` × 크기), `kite` 띠 당기기(`ClearReach`, 6.6), `Enemy.TouchReach`·자폭 점화 중 `Visual.ForceShown`, `Fairness.UnseenCharges`·`KiteSwitches` 세기,
  `--start-at` 이 지난 `light_change` 를 차례로 적용. 일반 생성 자리의 "보이는 후보 버림" 은 하지 않는다(6.12).
  `AutoplayPolicy` 는 `World.Visibility` 를 읽지 않는다(9절).
  v0.5.3(기획 v0.6.2 중간 보스): 단계 2 에 `blackash`, 단계 3 에 `deathspit`·`baron_sludge` 정의(6.5·6.13 — 중간 보스 5·보스 2), 두 스테이지 구간표·이벤트(기획 10.3·10.4), 해골왕 `hp` 9000·도살자 18000,
  `SeenClearly` 등급 예외 없앰·`PatternAbility` 의 `clear_sight`·`shoot_projectile`·`clear_sight` 패턴의 발사 프레임 재확인과 `SightGated` 채우기(6.6), 즉시 끝나는 `trigger: cooldown` 동작,
  중간 보스 죽음 순서·히트 스톱(`DeathHooks`), `BossBars` 등장 순서(8.2), 13.5-18 검사기(6.14).
  v0.6(안쪽 방): 포위 고리·`summon`·`telegraphed_blast` 지점·정예 무리·호위 대형 자리를 `TryPlace(…, PlaceReach(기준, rules.spawn.path_detour))` 로(4.10), `rules/enemies.json` 의 `spawn.path_detour` 1.5,
  `AutoplayPolicy` 의 방 들르기 모드(`--autoplay-visit rooms` — 기준점 이름 순, 가중 0.8, 반경 60, 9절). 스테이지 정의 `requires`(MVP 빈 목록).
  v0.6.1: 방 들르기의 길 방향(`TryPathStep`)·40초 넘김·`Events`/`RoomStats` 기록, 자리 고르기에서 기준이 주인공이면 `PlaceReach(…, FromHero: true)`, `StageReach.Enemies` 소유(WP1d-구현 뒤 — 13.5-15·13.5-18 검사기가 이것을 씀).
  v0.6.3: `room_supply` 이벤트 부품(`anchor`·`pickup`, 살아 있는 보급이 있으면 생략)과 두 스테이지 보급 시각표(기획 10.3·10.4), 방 체류 압박(`rules.spawn.room_pressure` 8·3·150)·`World.InnerRoomPresence` 갱신과 누적(6.12), `AutoplayPolicy` 의 "방 안" = `World.InnerRooms`(9절).
  시험: 합성 방 맵(③ 문 하나 방)에서 보급 생략·방 안 등장 칸이 바닥 안이고 주인공에서 150px 이상·방을 나오면 멈춤(10.1 `Director`).
  **v0.7(방 구조, 3.6·6.12)**: 감독의 **방 모드** — `World.DifficultyMinutes` 로 구간·배율, `Room.SpawningOpen` 일 때만 누적기·이벤트, 방 시작 때 `RoomEvents.Schedule`(WP1e)로 이벤트를 방 시계에 놓음(`Replay` 의 `light_change` 적용은 v0.7.1 부터 `World` 생성자 — 3.6), 생존 끝에 남은 이벤트 버림,
  `max_alive × Room.AreaScale`(반올림), 정예 판정 = 일반 방마다 방 시계 `elite_chance.at` 한 번, 이벤트 부품의 `entry:`·행진 목표 문을 `Room.RemapEntry` 로, 위치 후보가 없는 일반 생성은 들어온 문을 뺀 가장 먼 문, 방 문구 `대성당 — n` 을 `Banner` 에(1.5s).
  `DeathHooks` 는 방 월드면 `BeginVictory` 를 부르지 않고 `BossDefeated` 만(3.5). `--start-at` 의 v0.5.1 `IRunSetup` 은 없앤다 — 시작 방 시계는 `RunSession` 이 `RoomStart` 로 넣는다(9절). `--stress` 는 생존 단계에 묶음.
  `AutoplayPolicy` 의 방 규칙(9절 — 정리 추적·보상 집기·문 우선순위·`DOOR_SKIP` 의 `CommandButtons.Exit`, 판마다 하나), 13.5-18 방 판·13.5-23 ⑤ 검사기(6.14), `rules/rooms.json`(6.10). `Sim/Run/` 소유(WP1e-구현 뒤).
  **스테이지 데이터**: 단계 1 = `stage_cathedral` 의 `slice` 판(6.12 — 일반 방 여섯·풀 `cathedral_arena`·보상 마법서·보물 상자), 단계 2 = 액트 1 전체(방 아홉·정예 둘·보스·작은 방 풀 — WP5 맵 뒤), 단계 3 = 액트 2(`next_act` 사슬, 기획 10.4 방 순번 표·올린 구간표·중간 보스·도살자 HP)와 액트 1 의 `next_act`.
  보급 시각은 보스 방 t(기획 10.3·10.4).
- 실제 맵·그림 묶음 정의는 WP0a(타일셋·맵·단계 1 그림 묶음)와 WP5 가 낸다. WP4 는 그 파일을 고치지 않고, 단계 1 판 적 정의는 WP0a 의 그림 묶음 ID 를 가리킨다.
  부품 시험은 처음부터 `TestKit` 합성 맵·합성 그림 묶음으로 한다(실제 데이터를 기다리지 않는다).
- **완료 기준**: 테스트 통과(부품·생성기·전수 연기 시험·변형 픽스처·결정성 30초, 오라·함성이 `include_bosses: false` 면 보스에 안 걸림,
  우두머리 썩은 시체 최종 크기 1.6·반경 22(6.5), 실제 적·보스 정의가 13.5-13 예고 기준을 통과 — 기획 12.6 "지금 표의 패턴은 모두 이 기준을 지킨다", 목표 문 행진이 진입점에서 풀림,
  v0.5: 또렷이 보이지 않는 궁수는 쏘지 않고 다가옴·돌진 시작 조건(10.1 `Enemies` — v0.5.1: `L` 줄·낮춘 반경·대조군으로), 정위 궁수 떨림 시험, 함성 대상 시험, 실제 스테이지가 13.5-15 를 통과). 단계 1: 적 5종과 `stage_cathedral`(단계 1 판) 시간표가 10분 돈다(v0.7: `slice` 판 일반 방 여섯을 `RunSession` 으로 — 아래 v0.7 줄)
  (`Scenario`: 자동 조작 + `--god`, 예외 없음, 불변식 기준·공정성 열 0, 상한 무한 월드의 생성 수 기준 — 10.1 `Director`), 공정성 합성 맵 3분 `Scenario`. 마름모 모서리에 주인공을 둔 생성 시험에서 입구 대체 비율을 로그로 낸다(R23).
  `rules/sight.json` 의 시뮬레이션 값(`edge`·`view_inset`·`refresh_ticks`·`ranged_margin`)이 바뀌면 `Scenario`·13.5-15 를 다시 돌린다(6.10).
  v0.5.3: 10.1 `Enemies` 의 "중간 보스" 시험과 `Director` 의 중간 보스 이벤트·13.5-18 시험 통과. 단계 2: `stage_cathedral` 전체 판 10분 `Scenario`(블랙애시 포함)에서 공정성 세 열 0·`kite_switches` 기준, 실제 두 스테이지 13.5-18 경고 0.
  단계 3: 같은 `Scenario` 를 `stage_catacombs` 12분으로(중간 보스 셋이 겹쳐도 예외 없음·상한 360 안).
  v0.6: 단계 1 의 `stage_cathedral` 10분 `Scenario` 를 방 들르기 모드로도 한 번(공정성 세 열 0·`stuck` 기준 — 방 문·벽 모서리, v0.6.1: 모든 `room_*` 2번 이상·`occluded` > 0 — 빈 통과 막기, 10.1), 창살 너머 궁수 시나리오(10.1 공정성 열), 10.1 `Director` 의 포위 고리 길 거리 시험.
  **v0.7**: 위의 "`stage_cathedral` 10분"·"12분" `Scenario` 는 모두 **`RunSession` 액트 실행**으로 바꾼다(단계 1 = 일반 방 여섯, 단계 2 = 액트 1 아홉 방, 단계 3 = 액트 2 열한 방 — `--room 2:1` 시작 + `--level`·`--weapons` 로 액트 1 끝 빌드 흉내). 기준은 같고 더해서 `DOOR_SKIP` 0·`collapsed` 수를 보고(0 이 아니면 그 방 이름을 적음).
  10.1 `Director` 의 v0.7 "방" 시험, 13.5-18·23 ⑤ 시험, 실제 액트들이 13.5-3·18·23 을 통과. 방 들르기 `Scenario` 는 판 전체·맵마다 셈(10.1 공정성 v0.7).
- **v0.7.1**(13.18): `RemapEntry` 를 시작 문·목표 문 둘 다에(서로 바꾸기 — **v0.8.1 에 바뀜**: 시작 문이 들어온 문일 때만, 행진은 `RemapMarch`, 3.6 결정 5), 정예 방의 구간·배율은 `DifficultyMinutes`(`to` − 1틱에서 멈춤) 그대로 쓰면 됨, 방 안에서 시각이 된 `light_change` 는 `LightingState.Apply`(방 시작 때 지난 것의 `Replay` 는 `World` 생성자가 함 — 감독은 하지 않음), `light_change` 부품은 매개변수 형 `LightChangeParams` 로 등록, 13.5-18 정예 방 전체 구간·13.5-23 ⑤ 새 규칙(중간 보스·보스 이벤트 정확히 하나·같은 적, 기준점은 풀의 모든 맵, 점 빛, 서로 바꾼 뒤 시작 = 목표 오류). 자동 조작: `CommandButtons.Exit`·`ExitIndex`, 문 동률 둘째 기준(화면 y → `Exits` 순서), 정예 방 `SpawningOpen` 거짓 뒤 중간 보스 쪽 길 방향 0.8, `RewardStalls`. 시나리오 방 틱 상한과 단계 3 의 "판 전체"(10.1 `Sim`).
- **v0.8**(13.20, WP1f 뒤 — 기획 v0.8.1 2.5·10.3·15): **사냥 모드**(`Room.Phase == Cleanup` 이면 `kite`·도망 부품 끔, 길 따라 주인공에게 — 중간 보스·보스 본체는 해당 없음), `RoomEvents.CheckPlacement` 를 13.5-23 ⑥ 검사기로 등록,
  포위 고리 반경 하한 300·못 놓은 마리 남은 호에 다시 나눔(기획 10.3 — 감독 시험: 작은 방 진입점의 주인공에게 좀비 24·420 고리, 최소 거리 ≥ 300·생략 ≤ 25% 를 로그로), `--start-at` 보스 방의 광폭화 시계를 그만큼 흐른 것으로,
  방 문구에 층(`대성당 2층 — 3` — `RoomDef.Floor`), 자동 조작 문 우선순위 v0.8(9절 — 허리띠·성소 셋·덤), 정책의 방마다 판단 상태를 월드가 바뀔 때 비움(3.6 "계단 저장" — 끊고 이은 판이 같게).
  Data: `rules/rooms.json` 새 모양(6.10), 두 액트의 `floor`·이벤트 시각(기획 10.3·10.4 v0.8.1 표)·블랙애시 호위 이벤트·로트피스트 1800·블랙애시 2500·해골왕 10000, 보상 가중(6.12 예 — 단계마다 있는 ID 만).
  완료 기준 더함: 단계 3 "판 전체" 에 **계단 저장으로 끊었다 이은 판의 판 해시 = 끊지 않은 판**(씨앗 2개 — `RunSession.Suspend`/이어 하기 생성자를 Core 로 바로), 실제 액트들이 13.5-23 ⑥ 오류 0.
  v0.8.1 **끊는 틱**: 자동 조작에는 메뉴가 없으므로 봇이 계단 문 틈을 밟아 `Phase == Exit`(계단 문)가 된 틱에 `Advance` 대신 `Suspend`(3.6 ⓑ) → JSON → 이어 하기. 끊지 않은 판은 같은 틱에 그대로 `Advance` 한다 — 두 판이 같은 틱에 보스 방을 닫으므로 `ROOM` 줄 목록(보스 방 줄 포함)·판 해시가 같아야 한다(기획 v0.8.3 15절).
  13.5-23 ⑤ 의 v0.8.1 검사(바꾼 뒤 시작 문 = 들어온 문 → 오류, 원판·거울판·들어올 수 있는 문마다)가 실제 두 액트에서 오류 0, 감독의 행진은 `RemapMarch`.
- **v0.9**(WP1g 뒤): 자동 조작 — 물약 `bot_threshold` 35% 에 `Drink`, 상인 창 규칙(기획 12.8 — 최대 체력·방어 차이 최대 하나, 페핀은 채우기 → 주머니 → 빠른 물약), 문 우선순위의 회복 문·상인(기획 15절), 맵 풀 덱 순서(기획 10.8 v0.9 — `RunPlanner` ①, 방 맵 브랜치가 합쳐진 뒤). `Scenario` 에 상인 방이 든 액트 1 판 하나.

#### WP5 — 렌더·그림·미리보기

- **맡는 것**: Core `Art/`(`PngHeader`, `ArtModule.cs` = 메타·PNG 대조 검사기 등록), 게임 `Loading/`(텍스처·아틀라스·변종·색 키·스테이지별 미리 읽기·커서·글꼴),
  `Rendering/`(가상 화면·카메라·층·y 정렬·`ArenaRenderer`·빛·**안개 `FogRenderer`(v0.5, 7.10)**·표시 이펙트(`intro`·`scatter`)·시체·피해 숫자·`DebugNavOverlay`), `Scenes/PreviewScene.cs`, `Rendering/RenderServices.cs`.
  Core `Sim/Visibility/FogMath.cs`(v0.5.1 — WP1c 가 구현한 뒤 넘겨받는다. 그리기 규칙이라 WP5·WP6 이 바꿀 일이 생기면 여기서. 시그니처는 계약, 11.3), `Scenes/PreviewScene.cs` 의 `fog` 장면.
  Data `sprites/`, `effects/`, `maps/`(좌표·배치 맞추기 포함 — `maps/`·`sprites/` 의 첫 판은 WP0a 가 쓰고 넘긴다), `rules/sight.json`(v0.5, 첫 판 WP0a — 시뮬레이션 값 줄은 메인 세션 합의, 6.10). 테스트 `Art/`.
- **계약**: `WorldRenderer.Draw(World, RenderContext, float alpha)`, `FxSystem.Consume(PresentationBuffer, int stageTick)`, `IArtServices.Sprite`·`DrawFrame`(WP1b #17), `UiCanvas`(가상 좌표 → 창 좌표, 글꼴 배율),
  4층·0b·5층 벽 무리 나누기와 정렬 키(7.4), 예고 테두리 두 번 그리기(7.3), 유닛 최소 밝기(7.7), `wall_fade`(7.6),
  **안개(v0.5)**: `LightRT` 순서 ①~④(7.7 — v0.5.1: 값은 알파 채널, `FogBlend` 표), 칸 값 텍스처·`FogRT`·`MemRT`·합치기, `FogMath` 알파로 대상 숨기기(그리기용 오버로드, 7.10), `--reveal`, `--debug-nav sight`, `--preview fog`·`FOG` 로그 줄·`_light.png`,
  `RenderContext.ScreenToWorld` 구현 — WP6 이 HUD·마우스 이동에 쓴다. 기획 열린 질문 7 의 기준점·입구 칸·크기 배율, 10(혼합), 11(그림자 값 — 표는 WP0 에 요청), 18(늘린 그림)을
  `--preview`·`--debug-nav` 로 맞춰 데이터에 반영.
  v0.5.3: 그림 묶음 `variants`·클립 보탬 — `set_skelbow` 에 `bashtb`·`special`(단계 2), `set_acid` 에 `bfds`·`special`, `set_fat` 에 `bsm`(단계 3). 표 줄은 WP0 에 요청(11.3). 새 고유 TRN 색과 ×1.5 크기는 `--preview enemies` 로 본다(기획 열린 질문 16·18).
  v0.6: `maps/cathedral_arena.json` 의 안쪽 방 배치(첫 판 WP0a)를 `--preview tiles`·`--debug-nav tiles,entries` 로 맞춤(±1 메가 — 기획 10.3. v0.6.4: 문 틈 앞 벽·방 벽의 아래·오른쪽 끝 잘린 면도 본다 — 모서리 기둥 16·17 을 쓰면 틈이 하위 3칸으로 줄어 배치를 같이 옮김. 앞 흙 턱 그림이 막힘 칸보다 64px 위에 보이는 것(충돌 칸을 옮길지)과 긴 벽 칸이 같은 줄 이웃 벽에 가려 가운데가 끊겨 보이는 것(벽 무리 단위 밝기 또는 시야 규칙 보정)도 정함, 13.16), 주인공 그림은 `World.HeroClass.Sprites`(마법사 그대로),
  창살·방 벽의 4층 정렬·가림(7.4 — 이웃 판정 그대로), `--preview sprites` 에 직업 그림 묶음 행(MVP 는 마법사 하나). 단계 3 의 `catacombs_arena` 뒤쪽 방 둘(기획 10.4).
  **v0.7(방 구조, 6.11·7.11)**: 방 구조 그리기(문 봉인·문 위 아이콘·이름·0.5s 어두워짐 덮개 — 7.11), 보상 물체 그림 정의(`sprites/objects.json` — 단계 1 은 대체 그림, WP0 이 원작 물체를 풀면 바꿈), 액트별 미리 읽기(풀의 모든 맵·보상 물체, 7.5), `--preview tiles|fog` 의 `--map`.
  **맵 데이터**: 실제 `cathedral_arena.json` 에 `room_kinds: ["normal", "boss"]`(6.11 — 단계 1 전에, WP4 의 `slice` 스테이지가 13.5-23 ② 를 통과하게), 단계 2 에 대성당 작은 방 2장(기획 10.8 성격 A "기둥 홀"·B "창살 복도" — 배치 표는 기획이 단계 2 에 냄), 단계 3 에 지하묘지 작은 방 2장(C "뼈 방"·D "협로")과 `catacombs_arena`(`room_kinds: ["boss"]`).
  작은 방마다 13.5-4~6·19·23 통과, `room_*` 구역 기준점·`room_areas`, 문 넷 진입점(앞 문 `N − 6` = 26, 뒤 문 4).
- **완료 기준**: `Art` 테스트 통과. `--preview sprites/enemies/effects/tiles/blend` 캡처를 10.2 의 "통과 =" 문장으로 검증 담당이 판정(못 한 것은 "판정 못 함").
  `--stress 300 --freeze-ai` 장면에서 4.9 표의 렌더 기준 — **렌더 CPU p99 와 `draw_calls` ≤ 600 둘 다 단계 1 완료 조건**(단계 1 은 `stage_cathedral`, 10.3). 스테이지 시작 로그의 `vram_mb` 를 R7 에 적는다.
  10.2 의 "뒷벽·앞 턱·아치"·"예고·빛" 행. v0.5: 10.2 "안개 고정 장면"·"안개 흐름"·"안개 재현" 행(안개는 기획 14.1 단계 1 범위 — 고정 장면의 빛 값 대조 ①·⑤·⑥ 포함),
  렌더 기준은 `--stress 300 --freeze-ai` 를 `--reveal` 과 안개 켬으로 두 번 재 최악 추정으로(10.3). `rules/sight.json` 의 시뮬레이션 값 줄을 바꾸면 WP4 `Scenario`·13.5-15 를 다시 돌린다(6.10).
  v0.7: 10.2 "방 흐름" 행의 봉인·아이콘 부분, 액트 시작 로그 `ASSETS … vram_mb=` 가 풀의 맵을 다 읽은 값(R7 에 적음), 단계 2 의 `--room 1:2 --stress 300` 렌더 측정(10.3).
- **v0.7.1**(13.18): `IWorldBound` 구현 — `WorldRenderer`(맵 그림 묶음·`FogRenderer` 텍스처·칸 바뀜 값)가 새 월드를 받으면 다시 만듦(7.11). 원작 방을 본뜬 작은 방 맵은 첫머리 주석 `// 본뜬 원작 방:` 과 WP0 에 `audit_dun` 줄 요청(13.5-22), 점 빛은 기준점 ID 로(6.12).
- **v0.8**(13.20, WP1f 뒤): ① **그림 배율 s**(7.1 — `WorldRT` (960·s)×(540·s), 그리는 배율 = s ÷ 메타 `scale`, `PointClamp`, 빛·안개 타깃은 월드 해상도 그대로), 메타 형식 3·`arena.json` 형식 2 읽기, 렌더 측정을 s = 2 로도 한 번(10.3 — `draw_ms`·`vram_mb` 를 R7·R38 에).
  ② **거울판**: 작은 방 맵에 `mirror: true`(짝 표가 덮으면), `cathedral_arena` 는 WP0 의 L1 짝 조각 뒤에, `--preview tiles --map <id>@m` 으로 맞물림 확인. ③ 문 위 덤 아이콘·`▼ n층`(7.11), 보상 물체 그림 정의(성소 셋 — 음산한 성소는 색조 0.6).
  완료 기준 더함: 10.2 "거울판"·"AI 확대" 행(확대 자원은 WP0 의 실제 실행 출력으로 — 없으면 "판정 못 함").
  v0.8.1: s = 확대가 `done` 이면 manifest `output_scale`(첫 판 2), 메타 `scale` 이 {1, s} 밖이거나 s 를 나누지 않으면 시작 검사가 막음(7.1 — 검사는 WP7). `TextureStore` 는 액트 풀의 `mirror: true` 맵의 거울판 그림 `<art>@m` 도 액트 시작 때 읽고, `vram_mb` 측정에 거울판을 넣는다(7.1·7.5).
- **v0.9**(WP1g 뒤): 주인공 아홉 벌을 `HeroLook.Key` 로 고르기(못 가져온 벌은 `slt`), 바닥 아이템 떨어짐 애니(`items\*`)와 둘레 아이콘 그리기(위쪽 호·흔들림·300px 글자·시야 밖 숨김), 상인 그림(서기 반복·충돌 없음), 체력 구슬(`panel8` 바탕 + `p8bulbs` 액체를 아래에서 비율만큼·마시는 중 옅은 띠) — `UiCanvas` 쪽은 WP6 와 나눔. 미리보기에 아홉 벌·둘레 아이콘 한 화면.

#### WP6 — 화면·UI·입력, 통합

- **맡는 것**: 게임 `Scenes/`(Preview 제외, `SceneFactory.cs`·`AutoplayDriver` 포함), `UI/`, `Input/`. 통합 때 `PlayScene` 완성과 `Integration` 시험을 기본 분류로 옮기는 일(시험 파일의 분류 표시만 고침 — 내용은 WP7 과 합의).
- **계약**: `KeyboardMouseSource : IInputSource`(WASD·방향키·왼쪽 버튼 이동, 기획 3절 키), 씬 9개, HUD(미니맵은 가져온 그림 — v0.5: 가 본 칸만 보이게 가림·점 규칙, 8.2. 기획 14.1 단계 2), 레벨업·상자·일시정지·설정(**음량 셋**)·결과, 저장 연동(WP7 `SaveStore`),
  자동 조작 메뉴 넘김(`AutoStep(IAutoplayDriver)`), `SCENE`·`RESULT` 로그 문장, 레벨업 오버레이를 같은 앱 틱에 올림(3.3).
  v0.3: 씬별 음악·UI 소리(`IAudioServices`), 일시정지에서 `SetPaused`, **`PlayScene` 프레임 끝 소비 순서 `FxSystem.Consume → Audio.Consume → Presentation.Clear`**, 자원 없음 안내 화면(DataError 화면 재사용, 1.4 끝).
  v0.5.3: 보스 막대 여러 줄(8.2 — `Final` 위 큰 막대, 나머지 등장 순서로 작은 막대, 최대 4줄). 단계 3 에 10.2 "중간 보스 겹침" 행.
  v0.6: 고른 직업 정하기(`AppOptions.HeroClass` → 저장 `last_hero` → `rules.hero.class`, 꺼졌으면 기본 직업 — 6.16)와 `World` 생성에 넘기기, 타이틀 한 줄·스테이지 카드 꺼짐 표시(`ContentAvailability`, 8.1 — v0.7: 스테이지 카드는 StageSelect 와 함께 없앰, 타이틀 한 줄만),
  최고 기록 표시는 고른 직업 것, `AutoplayDriver` 가 `AppOptions.AutoplayVisit` 으로 `AutoplayPolicy` 를 만듦. 10.2 "안쪽 방" 행(통합 뒤).
  v0.6.1: 판 시작 때 `Save.SetLastHero`, 결과에 `HeroClassId = World.HeroClass.Id` 를 채워 `RecordResult`, StageSelect 가 `Save.BestRecord(stage, hero)`(8.3 — v0.7: 타이틀 한 줄이 `BestRecord(시작 액트, hero)`). `AutoplayDriver` 가 판마다 `new AutoplayPolicy(visit)` 하나를 만들고 `Events` 가 늘 때마다 `VISIT`·`VISIT_SKIP`, 판 끝에 `VISITS` 로그 줄(9절).
  타이틀 한 줄은 `ContentAvailability.Disabled(defs).Any` 일 때만, 앞말은 `Skipped(b).Reason` 으로(1.4 끝).
  v0.6.3: 미니맵에 방 보급(`Pickup.SupplyRoom` 이 있는 획득물)을 흰 점으로 — 가 본 곳이 아니어도(6.12, 8.2).
  **v0.7(방 구조, 3.6·8)**: `StageSelectScene` 없앰(새 게임 → Play, 직업 단계면 HeroSelect → Play), `PlayScene` 이 `RunSession` 을 가짐 — `RunStart`(`AppOptions` 의 씨앗·`StageId`·`StartRoom`·직업·`Run`)로 만들고, `StageStep.RoomExit` 면 `rules.rooms.transition` 어두워짐 가운데 `Advance()`·액트가 바뀌면 `PreloadStage`·`PlayMusic`(7.9),
  보상 창(`AwaitingChoice == Reward` → `LevelUpOverlay` 를 `RewardOffer` 로), HUD 방 줄·방 상태·미니맵 남은 적·문 점(8.2), 결과 화면(도달한 곳·걸린 시간·방 줄 — `RunState`), 타이틀 최고 기록 한 줄(`BestRecord`), `RecordResult` 에 `RunResult` 새 모양,
  로그 `ROOM`(방마다 — `RunState.Rooms` 의 새 줄, 사람 판도)·`INNER_ROOMS`(방마다)·`DOOR_SKIP`(정책 `DoorSkips`)·`RESULT` 새 모양, `AutoplayDriver` 가 판마다 정책 하나(월드가 바뀌어도 그대로)·명령은 `policy.Next(world)`, `--capture-on doors|reward`.
- **완료 기준**: `--autoplay --seed 1 --quit-after 30` 이 타이틀 → 플레이까지 사람 입력 없이 감(`SCENE` 로그 두 줄 — v0.7: 선택 화면 없음). 캡처 4장(타이틀, 플레이 HUD, 레벨업 창, 결과)을 10.2 문장으로 판정. v0.7: 10.2 "방 흐름" 행(통합 뒤 — `RunSession` 은 WP1e-구현).
  단계 2: 10.2 "미니맵 기억" 행(v0.5.1 — WP5 의 단계 1 안개 행에서 옮김).
  통합 뒤: 실제 데이터 엄격 시험 통과, 10.2 "단계 끝" 행 통과. 키보드로 한 판(사람)·소리를 귀로 듣기는 "동작 확인 못 함" 으로 따로 적는다.
- **v0.7.1**(13.18): `Advance` 앞뒤의 표시 이벤트 소비·`IWorldBound.Attach` 순서(7.11 — `FxSystem`·`Hud`·미니맵 쪽 구현), `StageStep.Finished` 첫 앱 틱에 `RunSession.Conclude()` 한 번 → 결과 화면·`RESULT`·`RecordResult`(`ReachedActId`), HUD 분모 = 그 액트 마지막 방 번호, 로그 `REWARD_STALL`·`INNER_ROOMS`(9절). 완료 기준에 10.2 "방 흐름" 의 v0.7.1 줄.
- **v0.8**(13.20, WP1f 뒤 — 기획 v0.8.1 2.5·12): 일시정지 메뉴 `저장하고 끝내기`(`RunSession.CanSuspend` 일 때만) → `Suspend` → `Save.SaveRun` → 타이틀, 타이틀 `이어 하기 (<다음 액트 이름>)`(`PeekRun`) → `TakeRun` → 이어 하기 생성자(불일치면 한 줄 안내), 새 게임에서 남은 저장을 지운다고 한 번 묻기(`DiscardRun`),
  넘김 어둠 `transition_floor`(고른 출구 `NextFloor` ≠ 0), HUD 허리띠 칸 둘·무기·패시브 칸 = 열린 칸 수(4 → 5), 남은 적 미니맵 점은 정리 시작부터 모두, 로그 `ROOM` 의 `mirror`·`boss_s`·`picks_left`(9절). 완료 기준 더함: 10.2 "계단 저장" 행.
  v0.8.2(WP1f 검증 반영): `Suspend` 가 `InvalidOperationException` 을 내면(ⓐ 의 한 틱 뒤 `Exit` 가 아님 — `CanSuspend` 조건이면 사실상 없지만, 그 틱 안에서 획득물이 끌려오기 시작하는 경우는 못 막음. 그 틱은 이미 돌았고 되돌리지 않는다) 메뉴는 `저장 못 함` 한 줄을 보이고 판을 그대로 이어 간다(타이틀로 가지 않음, `SaveRun` 안 함).
- **v0.9**(WP1g 뒤): `Q` → `CommandButtons.Drink`(창이 열려 있으면 넣지 않음), 상인 창(레벨업 창 재사용 — 카드 셋·값·차이 줄·무게 줄·`산 것`·`다 올림`·페핀 창·반지 바꾸기), HUD 장비 줄·물약 칸(`2/3`·키 `Q`)·금화 번쩍·원작 체력 구슬, 레벨업 카드 아이콘(원작 `spelicon`), 결과 화면 `얻은 금화 · 쓴 금화`. 플레이 관찰 "키 입력이 이상함" 의 원인이 창 닫힘 앞뒤 입력이면 `InputMap` 잠금(기획 3절 첫 안).

#### WP7 — 앱 셸·데이터 로더·개발 도구·자원 검사

- **맡는 것**: Core `Data/Loading/`(로더·병합·페이즈 풀기·변환기·공통 검증 규칙 13.5-1,2(등록 여부),3,7(정의끼리),8,9 와 6.14 추가 규칙, `LoadingModule.cs`), `Dev/`(`DevOptions`, `TickProfiler`, `StateHasher`), `Save/`,
  게임 `Program.cs`, `GameApp.cs`(합성 뿌리), `App/`(Contracts 제외: `FrameLoop`, `DevRunner`, `AssetCheck`, 캡처, 종료·감시, `Log`, `WindowControl`, `SaveStore`, 빈 시험 씬 `BlankScene`).
  테스트 `Data/ Dev/ Save/`, `Fixtures/data/`(고장 난 JSON).
- **계약**: `DataLoader.Load(dir, registry, validators, options) → (GameDefs, errors)`(새 종류 `tilesets`·`sounds`·`music`·`sound_bindings`, `PhaseDef.Patterns` 채우기), `--validate-data`, `FrameLoop`(3.2), `DevRunner`(캡처·`--quit-after`·`--timeout`·`--exit-on-result`·종료 코드),
  9절 옵션 파싱(`--mute`·`--sound`·`--sound-device` 포함), 저장 보호 규칙(9절), 로그 첫 줄·`EXIT` 줄, `GraphicsProfile.HiDef`, SDL·`ALSOFT_DRIVERS` 환경 변수와 장치 초기화 실패 시 다시 만들기(7.9),
  **시작 자원 검사**(1.4 끝 ①~⑤, 종료 코드 5, 캡처만 있으면 안내 화면 캡처), `DataLoader` 가 `tilesets`·`maps` 를 `MapDataReader` 로 받음(6.2), GameApp 이 `AudioServices.Create` 를 부름.
  v0.5: `--reveal`·`--debug-nav sight` 파싱(`AppOptions.Reveal`), `StateHasher` 에 `Visibility.ExploredHash`(가 본 칸 집합, 3.4·4.11 — v0.5.1), "스테이지가 있는데 `rules.sight` 없음" 오류·13.5-15 의 380·화면 반폭 쪽(6.14).
  v0.5.1: `--preview fog` 파싱, 저장 보호 판정에 `--reveal`·`--god`·`--level`·`--weapons`·`--passives`(9절), 캡처 때 `FOG` 로그 줄(값은 WP5 렌더가 넘김)과 `_light.png` 저장 경로.
  v0.6: 로더의 `hero_classes` 종류·13.5-20(정의 쪽)·13.5-21 검사(6.14), 시작 검사 ③ 의 **"꺼짐"**(꺼진 묶음 파일은 오류 아님, 1.4 끝)과 `ASSETS … bundles= skipped= disabled=` 줄, `--hero`·`--autoplay-visit` 파싱·검사(없는 ID 64, 꺼진 직업·스테이지 5),
  저장 보호에 `--hero`, 저장 모양 스테이지 × 직업·`last_hero`(8.3), 첫 로그 줄·`RESULT` 의 `hero=`, `StateHasher` 첫 값에 직업 ID. `ContentAvailability.cs` 소유(WP1d 뒤 — 11.3).
  v0.6.1: `SaveStore` 가 `LastHero`·`SetLastHero`·`BestRecord` 구현(`RunResult.HeroClassId` 아래에 기록 — "늘 `sorcerer`" 를 가정하지 않음), 로더 연결 단계가 `PartDef.ReferencedEnemies` 를 채움, `DefAssetNames.For` 본문(13.5-10 귀속 규칙),
  시작 검사 ③ 의 `OutputBundles(arenas)`·교차 확인(꺼짐 파일에 켜진 스테이지·직업이 닿으면 오류), 13.5-20 의 해금 사슬 규칙(v0.7: 다음 액트 사슬), `ASSETS disabled=` 는 꺼진 스테이지·직업만.
  v0.6.3: 13.5-19 의 방 보급 검사(`room_supply` → 그 맵의 `OneDoor` 방·있는 획득물), 판 끝 `INNER_ROOMS` 로그(9절), `StateHasher` 에 `Pickup.SupplyRoom`·`RoomPresence.Current`·`EnteredTick`.
  **v0.7(방 구조)**: 로더 — 종류 `rewards`, `rules.rooms`(없으면 오류), 스테이지 이벤트의 `at` → `PartDef.At`, `StageDef.NextAct`(같은 종류 안 참조 — 연결 단계에서 순환 오류)·`RoomDef.Miniboss`·`Boss`·`RoomMapPoolDef` 연결, 13.5-3(t 범위 판)·13.5-20(다음 액트 사슬)·13.5-23 ①②④(6.14),
  `DefAssetNames`·13.5-10 의 맵 그림 귀속(`room_maps` 를 따라), `DevOptions` 의 `--room`·`--map`·`--stage`·`--start-at` 새 뜻·`--capture-on doors|reward`(9절)와 개발 옵션 판정, 액트 번호 = 뿌리 액트부터 `next_act`(3.6), `--stage`·`--room` 의 꺼진 액트 → 종료 코드 5,
  `SaveStore` 새 모양(8.3 — 시작 액트 × 직업, 깊이 비교, 해금 없음), `StateHasher` 에 판 흐름·방 상태(3.4), 첫 로그 줄에 `start=<액트> room=<a:n>`(있을 때).
- **완료 기준**: 테스트 통과(로더 규칙·뒤섞은 파일 순서·옵션 파싱·페이즈 누적 풀기). 게임 실행: 빈 씬으로 창이 뜨고 `--quit-after 2 --capture x.png --capture-at 1` 이 PNG 를 쓰고 종료 코드 0(`Start-Process -Wait -PassThru`),
  가장 큰 텍스처(4096 폭 아틀라스, 550×3240)의 만들기 성공(`HiDef` 확인), 개발 옵션 실행 전후 사용자 `save.json` 시각 같음, `--background` 실행 전후 포그라운드 창 같음,
  `-p:DdrAssetDir=<빈 폴더> -p:SkipAssetCheck=true -o <임시>` 로 만든 exe 의 자동 실행이 종료 코드 5 와 없는 파일 목록 로그, 같은 exe 의 캡처만 준 실행이 안내 화면 PNG + 종료 코드 5(10.2),
  가져오기 표 한 줄을 바꾼 뒤 빌드한 exe 가 "가져오기 표가 바뀌었다" 로 종료 코드 5. 개발 옵션 실행 로그에 `AUDIO device="No Output"`.
  v0.6: 10.2 "헬파이어 없음" 행(`--skip-bundles all` 자원으로 종료 코드 0·안내 화면 없음·`skipped=` 넷·같은 씨앗 해시 같음), 시작 검사 시험(합성 manifest 에 `skipped_bundles: [hellfire]` + `requires: [hellfire]` 합성 스테이지 정의 → 꺼짐 1·오류 0, 같은 파일이 기본 내용이면 오류).
  v0.6.1 시작 검사 시험 더: 합성 `l5` 타일셋(표 항목 `bundle: hellfire`)을 쓰는 맵의 그림이 manifest 에 없음 → 꺼짐·오류 0, **기본 스테이지의 적이 묶음 소리(`sprite_sets.<s>` 줄)를 가리킴 + 그 묶음 건너뜀 → 오류**(교차 확인), `hfmusic` 만 건너뜀 → 타이틀 한 줄 없음·`AUDIO … fallback=`.
  10.1 `Dev`·`Save` 의 저장 왕복(합성 직업 둘의 기록이 따로 남음). v0.7: 10.1 `Dev`·`Save` 의 v0.7 줄, 고장 난 작은 JSON 으로 13.5-23 ①②④ 의 오류가 하나씩(일반 방 다섯·보스 방이 가운데·빈틈·`miniboss` 없음·풀에 종류 없음·`room_kinds` 불일치·가중 보상 하나·`first_room` 없음·`blessing`+`trait`), `slice: true` 면 빈틈·보스 방 없음이 통과.
  최소화·숨긴 창에서도 `--capture-at` PNG 가 생기는지 확인해 결과를 R8 에 적는다.
- **v0.7.1**(13.18): 로더 — `RoomDef.Number`(없으면 배열 자리 + 1)와 13.5-23 ① 번호 규칙, 13.5-1 의 이벤트·점 빛 기준점 검사를 13.5-23 ⑤ 의 풀 전체 규칙으로. 옵션 — `--room` 은 방 번호로, `--start-at` 상한은 방 종류마다(9절). `StateHasher.Room`·`Run` 과 `--log-hash` 열(3.4). 저장 — 깊이 모양(`act`·`act_number`·`room`·`kind`·`rooms_hash`)·비교 키(깊이, 승리, −시간)·읽기 규칙(8.3, 10.1 `Save`).
- **v0.8**(13.20, WP1f 뒤): 로더 — `rewards` 새 모양·`rules.rooms`·`rules.hero` 새 키(6.10·6.18), 13.5-23 ① `floor` 규칙·④ 칸 수 상한·`temper` 참조, 맵 `mirror` 와 거울판(읽기는 `MapDataReader` 결과 그대로 — 거울판은 정의 표에 넣지 않음), 13.5-10·시작 검사 ③ 의 맵 그림 이름에 거울판 `art`.
  저장 — `run.json`(8.3 "계단 저장": `PeekRun`·`TakeRun` 읽는 순간 지움·`SaveRun`·`DiscardRun`, 깨진 파일 `.bad`, 개발 옵션 실행은 읽지도 쓰지도 않음). 옵션 — `--map <id>@m`, `--start-at` 상한은 `RoomStartLimits`(WP1f) 로. v0.8.1: `--weapons`·`--passives` 수 > 시작 액트의 `RunSlots.OpenAt` 이면 64, 시작 검사가 메타 `scale` ∈ {1, s}·s 를 나눔을 봄(7.1), `run.json` 의 `Format` ≠ `RunSnapshot.CurrentFormat` 은 불일치(버림 — `required` 빠짐은 `.bad`, 8.3).
  자원 검사 — 형식 판 3, `ASSETS … upscale= art_scale=`(1.4 끝), 맵 그림 대조에 메타 `scale`. 완료 기준 더함: 10.1 `Save` 의 `run.json` 왕복(읽으면 지워짐·깨진 파일·높은 판), 10.1 `Dev` 의 `--map …@m`·`--start-at` 상한 셋.
- **v0.9**(WP1g 뒤): 로더 새 키(`items/`·`affixes`·`merchants/`·`rules.equipment`·`rules.hero.potions`·`rules.rooms.rewards`, 옛 `rules.hero.belt` 오류), `run.json` Format 2(옛 판은 불일치로 지움), `ROOM` 로그 새 열(`hp_in`·`kills`·`gold`·`gold_spent`·`shop`·`potions`·`drunk`), `AppServices` 새 칸·`IDevHooks` 본문(WP1g #19).

#### WP8 — 소리 (단계 2)

- **맡는 것**: Core `Audio/`(`AudioModule`·연결 표 검사기, `SoundMixer`), 게임 `Audio/`(`AudioServices.cs`, `AudioSystem`, `SoundBank`, `MusicPlayer`), Data `audio/`(sounds·music·bindings), `rules/audio.json`.
  테스트 `Audio/`.
- **계약**: 6.15 데이터 모양과 검사, 7.9 의 신호 → 소리 규칙·고르기·재생·무음·로그, `IAudioServices` 구현.
  v0.5.3: 소리 연결 `sprite_sets.set_skelbow.special`(단계 2)·`set_acid.special`(단계 3)과 그 소리 정의(6.15). 새 중간 보스는 그림 묶음 연결을 그대로 쓰고 분류만 `boss` 로 오른다.
  v0.6: 연결 표 `hero` 를 직업 ID 키로(`hero.sorcerer`), `AudioSystem` 이 `World.HeroClass.Id` 로 고름, 직업마다 `hero.<id>` 가 없으면 경고(13.5-21), `MusicPlayer` 의 `requires`·`fallback`(6.15·7.9 — MVP 곡은 모두 기본이라 대체 없음, 시험은 합성 곡).
  **v0.7**: 연결 표 `rewards.<id>`(`collect`·`open`)·`ui.door_open`·`ui.room_clear`, 신호 `DoorsOpened`·`RoomCleared`·`RewardCollected` → 소리, 소리 정의 `snd_dooropen`·`snd_invbook`·`snd_readbook`(가져오기 줄은 WP0 에 요청), `MusicPlayer` 가 같은 키면 아무것도 안 함(액트 안 곡 이어 틀기 — 7.9),
  `AudioSystem` 이 `World` 가 바뀌어도(방 이동) 재생 중인 소리를 끊지 않음(새 월드의 표시 이벤트만 소비 — 틱이 0 부터라 "생긴 틱" 비교는 월드마다). 완료 기준 더함: 방 이동 앞뒤 `AUDIO` 수가 `--mute`·`--sound` 같음, 음악이 방 이동에서 다시 시작하지 않음(로그 `AUDIO music=` 줄이 액트마다 하나).
- **완료 기준**: `Audio` 시험 통과(10.1). 게임 실행(개발 옵션, null 장치): `--sound` 를 준 자동 조작 실행이 오류 없이 끝나고 `AUDIO` 로그 줄의 수가 기준 안(맞음 소리 0.08s 제한 → 초당 ≤ 12.5 등) —
  **기준은 `--mute` 와 `--sound` 어느 쪽이든 같은 수**(같은 씨앗이면 두 실행의 `AUDIO` 수가 같다, 7.9), 로그에 `AUDIO device="No Output"`, 두 실행의 `--log-hash` CSV 같음(10.2 "재현").
  장면 바뀜 프레임에 `slow_frames` 가 늘지 않음(음악 읽기, 7.9). 소리를 귀로 확인하는 것은 사용자 몫(`--sound-device default`, 동의를 받고 — "동작 확인 못 함" 으로 적음).
- **v0.8**(13.20): 연결 표 `rewards.<id>` 를 새 보상 ID(성소 셋 — 원작 성소 소리 `sfx\items\magic.wav`, 물약 `invpot.wav`, 기획 v0.8.1 11.4)로, 허리띠 저절로 마심은 새 신호 `CueKind.BeltPotion`(WP1f #19, WP3 가 냄) → 연결 `ui.belt_potion`(물약 소리). 소리 표 줄은 WP0 에 요청.
- **v0.7.1**(13.18): `IAudioServices : IWorldBound` — 새 월드를 받으면 개체 ID 를 키로 쓰는 반복 소리를 멈추고 쿨다운을 비운다(곡은 액트가 같으면 이어 틂, 7.11).
- **v0.9**(WP1g 뒤): 상인 인사 대사 셋(`sfx\towners\bsmith*`·`witch*`·`healer*`), 물약 마심·못 함·회복 문 들어옴·사기·상자 → 책 떨어짐·장비 끼우기 소리(새 `CueKind`), `IAudioServices : IDisposable` 로 옮김.

#### WP9 — 직업 고르기 (v0.6, MVP 다음 — 기획 14.2 이후 확장 1, 사용자 결정 K)

계약은 WP1d 가 이미 냈다. 이 묶음은 **데이터·씬·패턴만** 더한다 — 계약 파일을 고쳐야 하면 WP1d 의 목표가 깨진 것이니 메인 세션에 알린다.

- **하는 것**: 가져오기 표에 `warrior\wms`(죽기 `wmn\wmndt`)·`rogue\rlb`(`rln\rlndt`)·`monk\mlt`(죽기 자기 벌, `bundle: hfmonk`) 한 벌씩 + 맞음·죽음 소리(수도사 `prefer: [hfvoice]`)(WP0 에 요청),
  그림 묶음 셋(WP5 — 시전 클립 `event_frame` 14·12·13, 출처 주석), 직업 정의 셋(6.16 — 기획 4.5 표)과 특성 패시브(`trait`, WP3), 특성 아이콘 그림 정의(WP5 — 원작 exe PNG 나 주문 아이콘, v0.6.1), 발사 패턴 `melee_arc`(WP3, 6.8)와 무기 `cleave`·`staff_sweep`(`classes` 로 묶음),
  `HeroSelectScene`(8.1 — 카드 4장, 꺼진 직업 어둡게), 소리 연결 `hero.<id>`(WP8), `--preview sprites` 의 직업 행.
- **완료 기준**: 네 직업 각각 `--hero <id> --autoplay --god --exit-on-result` 가 판 끝까지 감(v0.7 — v0.6 의 "`stage_cathedral` 10분". 시작 무기·그림·소리 신호, 계단에서 직업·빌드가 이어짐), 같은 씨앗·직업이면 해시 같음,
  `--skip-bundles hfmonk` 자원에서 수도사 카드가 어둡고 `--hero monk` 가 종료 코드 5, 기획 열린 질문 27(직업 균형)·29(수도사 색·직업 소리)를 사람이 보는 캡처·로그 목록.

#### WP10 — 헬파이어 스테이지 (v0.6, MVP 다음 — 기획 14.2 이후 확장 2, 사용자 결정 H)

- **하는 것**: 지하실(L5) 먼저(기획 10.6 — L1 이름·배치를 거의 그대로), 그다음 둥지(L6, 번호 조사 새로). `Table/tilesets.json` 의 `l5`·`l6`(`bundle: hellfire`, 경로 `nlevels\…`, 덧그림 번호표는 그때 원본으로 확인),
  `tilesets/l5.json`·`l6.json`(성질은 그 층 SOL), 맵(`requires` 없음 — 맵 그림 묶음은 타일셋 항목 `bundle`, v0.6.1 — v0.7: 액트마다 작은 방 풀 + 보스 방)·스테이지 = 액트(`requires: [hellfire]`, v0.7: 기본 액트 사슬에 어떻게 잇는지는 기획 열린 질문 38 — 계단 둘이면 `next_act` 목록 계약 변경, 따로 판이면 저장 키 = 그 시작 액트, 6.12·8.3), 헬파이어 몬스터 표 줄·그림 묶음·정의(`requires: [hellfire]`, 고유 셋은 `genrl` 변종, 스컬윙은 `skelt`), 음악(`requires: [hfmusic]`·`fallback`), 나크룰 방(우리 `grid`).
  스컬윙·본 데몬은 원작 소리가 없으므로 헬파이어 소리를 빌려 소리 연결을 단다(그렇지 않으면 6.14 "소리 없는 적" 경고, v0.6.1).
  헬파이어 몬스터와 투사체(`ms_*`·`ex_*`)의 연결은 그때 원본으로 확인한다(기획 8.5).
- **완료 기준**: 13.5-20 오류 0, `--skip-bundles hellfire` 자원에서 타이틀 한 줄(v0.7 — 스테이지 카드 없음)이 뜨고 `--stage stage_crypt` 가 종료 코드 5, `--skip-bundles hfmusic` 이면 대체 곡으로 돎(`AUDIO music=… fallback=…`), 지하실 10분 `Scenario`.

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
| | `Facing`, `Anim`(`AnimClock` 상태), `Visual`(`EnemyVisual` — v0.5.1 `ForceShown`) | WP4 | WP5, `FogMath` | 〃 |
| | `BodyTop`, `TouchReach`(v0.5.1, 6.6) | WP4(생성 때) | WP4(`SeenClearly`), `FogMath`(WP5·WP6·계측) | 〃 (WP1c) |
| | `Buffs`(`BuffSlots`) | WP3(장판 둔화), WP4(오라·함성) — 공통 도우미로만 | WP4 | 〃 |
| | `Nav`(`NavAgentState`) | WP2 | — | `Sim/Movement/Enemy.Nav.cs` |
| | `NextHitBySlot[4]` | WP3 | — | `Sim/Combat/Enemy.Combat.cs` |
| | `Parts`(`PartSlots`), `Action`(`ActionState`), `PhaseIdx`, `Order`(`MoveOrder`), `Leader`, `Summoner`, `Source`, `EnrageAt`, 공포 재무장 시각 | WP4 | — | `Sim/Enemies/Enemy.Behavior.cs` |
| `Hero` | `Pos`, `PrevPos`, `Vel`, `Radius` | WP2(이동), WP3(시작 위치) | 모두 | `Sim/Entities/Hero.cs` (WP1) |
| | `Facing`, `Anim`, `Hp`, `MaxHp`, `Dead`, `InvulnTime`, `HitFlashTime`, `Intent`, `MoveSpeed` | WP3 | 모두 | 〃 |
| | `KnockbackVel`, `KnockbackTime` | WP3(`DamageResolve`) | WP2(`Locomotion` 적분) | 〃 |
| | `Stats`(`StatBlock`), `Weapons`(`WeaponSlots` 4 — v0.8: 그릇 6), `Passives`(`PassiveSlots` 4 — v0.8: 그릇 6) | WP3 | WP4(자동 조작), WP6(HUD) | 〃 |
| | `WeaponSlotsOpen`·`PassiveSlotsOpen`(열린 칸 수)·`Belt`(허리띠 물약 수) (v0.8 — 짐으로 넘김, 3.6) | WP3(주인공 만들기·`Restore`·허리띠) | WP4(자동 조작 문 우선순위), WP6(HUD 칸·허리띠), WP7(해시) | 〃 (WP1f) |
| | `TouchingCount` | WP2 | WP3 | 〃 |
| | `Blessings`(`BlessingSlots` 8, v0.7 — 축복 패시브, 슬롯 밖) | WP3 | WP6(일시정지 빌드 목록), WP7(해시) | 〃 (WP1e) |
| | 재생·무기 내부 상태 | WP3 | — | `Sim/Hero/Hero.Combat.cs` |
| `Projectile` | `Def`, `Faction`, `Pos`, `PrevPos`, `Vel`(그림 방향), `Radius`, `Tint`, `Scale`, `Age`, `TimeLeft`, `Duration`, `Owner`, `WeaponSlot`, `SpawnTick` | WP3(WP4 는 `ICombatSpawner` 로 생성만) | WP4(자기가 만든 것 세기), WP5(그리기) | `Sim/Entities/Projectile.cs` (WP1) |
| `Hazard` | `Effect`, `Tint`, `EffectScale`, `Faction`, `Pos`, `PrevPos`, `Radius`, `Age`, `TimeLeft`, `Duration`, `Owner`, `SpawnTick` | WP3 | WP4, WP5(그리기·조각), WP8(반복 소리) | `Sim/Entities/Hazard.cs` (WP1) |
| `Telegraph`·`Pickup` | 정의·진영·위치·이전 위치·반경·나이·남은 시간·진행 비율·출처 적 ID(1.1 파일 그대로) | WP3 | WP4, WP5 | `Sim/Entities/*.cs` (WP1) |
| `Pickup` | `SupplyRoom`(`string?`, v0.6.3 — 방 보급의 방 기준점 ID) | WP3(`SpawnPickup` 이 `PickupSpawn.SupplyRoom` 으로 채움, 넘기는 쪽 WP4) | WP4("안 주운 보급"), WP6(미니맵 흰 점), WP7(해시) | `Sim/Entities/Pickup.cs` (WP1d) |
| | `Reward`(`RewardDef?`)·`GatesExit`(bool) (v0.7 — 문 보상 물체·출구 문을 막는 획득물)·`Magnetized`(bool, v0.7.1 — 끌려오는 중) | WP3(`SpawnPickup` 이 `PickupSpawn.Reward` 로 — **v0.7.1: `Reward != null` 이면 `GatesExit = true`**, `Magnetized` 는 끌어당김), `RoomFlow`(정리 끝에 남은 정예 상자 `GatesExit` — 이번 틱에 생긴 것 포함·자리 옮김) | WP3(줍기 효과), `RoomFlow`(문 열기 조건), WP5(그리기), WP6(미니맵 금색), WP7(해시) | `Sim/Entities/Pickup.cs` (WP1e) |
| | 맞은 목록·궤적 상태·피해 값·`on_end` 명세 | WP3 | — | `Sim/Projectiles/…` 등 조각 |
| `World` | `HeroClass`(`HeroClassDef?`, v0.6 — 생성자 인자, 바뀌지 않음) | 앱(WP6)·시험이 생성 때 | WP3(주인공 만들기·후보 거르기), WP5(그림), WP8(소리), WP7(해시·로그) | `Sim/World.cs` (WP1d) |
| | `Defs`, `Stage`, `Map`, `Arena`(`ArenaGrid`, v0.3), `Resolved`(`ResolvedMap` — 입구·기준점·시작 월드 px, v0.4), `MapSize`(외곽 상자), `ViewSize`, `Rng`, `Tick`, `Dt`, 저장소 5개, `Hero`, `Events`, `Presentation`, `Damage`(요청 큐), `Outcome`, `OutcomeTick`, `AwaitingChoice`, `ViewRect`, `FreezeTicks`, `SlowMoScale`·`SlowMoAppTicks`·`SlowMoStretch`, `Seed`, `Simulation` | WP1(틀), 각 단계 | 모두 | `Sim/World.cs` (WP1) |
| | `Command`(`PlayerCommand`) | `StageStepper`(앱 쪽이 넘김) | WP3 `HeroControl` | 〃 |
| | `Options`(`RunOptions`) | `ModuleContext.ApplyRunSetups` | 모두(`--god` 등) | 〃 |
| | `Nav : INavWorld`, `Spatial : ISpatialQuery` | WP2(모듈이 넣음) | 모두 | 〃 |
| | `Hostiles : IHostileQuery`, `Spawner : ICombatSpawner`, `Progression : IProgression?`, `RunStats` | WP3(모듈이 넣음) | WP4, WP6 | 〃 |
| | `Lighting`(`LightingState`: 주변광·어둠·주인공 빛·점 빛), `BossBars`, `Banner`(`BannerState`) | WP4 | WP5, WP6(`Banner` 는 HUD 문구의 유일한 출처), `Visibility`(반경) | 〃 |
| | `Visibility`(`VisibilityField`, v0.5 — 4.11) | `BeginTick` 만(WP1c 구현, 그 뒤 WP2) | WP4(사격·돌진), WP3(공정성 수 세기만), WP5(안개), WP6(미니맵), WP7(해시). WP3 조준·WP8 소리·`AutoplayPolicy` 는 읽지 않음 | 〃 (형은 `Sim/Visibility/`) |
| | `InnerRooms`(`RoomFloors`, v0.6.3 — **안쪽 방** 바닥, 생성자에서 한 번. v0.7.1 에 `Rooms` 에서 이름 바꿈) | 생성자(WP1d) | WP4(방 체류 압박·방 보급·`AutoplayPolicy` "방 안"), WP7(검사·로그) | `Sim/World.cs` (WP1d) |
| | `InnerRoomPresence`(`RoomPresence`, v0.6.3 — 주인공이 선 안쪽 방·안쪽 방마다 누적. v0.7.1 에 이름 바꿈) | WP4(`Director`) | WP4(`AutoplayPolicy`), WP7(`INNER_ROOMS` 로그·해시) | 〃 (형은 `Sim/Contracts/WorldState.cs`) |
| | `Room`(`RoomState?`, v0.7 — 방 흐름, 3.6. 방 없는 월드는 null) | 생성자, `RoomFlowSystem`(16단계)만 | WP3(`RoomCleared`·보상), WP4(감독 — `T`·`SpawningOpen`·`RemapEntry`·`ClockTicks`, `AutoplayPolicy`), WP5(봉인·아이콘), WP6(HUD·`StageStep`), WP7(해시·`ROOM` 로그) | `Sim/World.cs` (WP1e, 형은 `Sim/Contracts/RoomState.cs`) |
| | `Run`(`RunState?`, v0.7 — 판 수명, `RunSession` 이 넣음), `RunStats` 는 `Run.Stats` 를 가리킴. **v0.7.1: `RunStats` 를 지우는 곳은 없다** — `RunSession` 이 판 시작 때 새로 만들고, 월드 설치·주인공 만들기·`ResetCombat` 은 건드리지 않는다(`Reset()` 없앰). `DamageByWeapon` 은 칸 번호로 세므로 `IHeroCarry.Restore` 는 같은 칸에 놓는다 | `RunSession`(방 사이에서만) | WP3(`RunStats`), WP6(방 줄·결과), WP7(로그·해시) | 〃 |
| | `Gates`(`GateZones`, v0.7 — 문 틈 칸, 생성자에서 한 번), `DifficultyMinutes`(v0.7 — `Room?.T ?? StageMinutes`) | 생성자 / 계산 속성 | `RoomFlow`, WP4(구간·배율), WP5(봉인 자리), WP6(미니맵 문 점) | 〃 |
| | `Fairness`(`FairnessCounters`, v0.5.1 — 10.1 공정성 열) | WP3(`UnseenShots`·`HiddenHits`), WP4(`UnseenCharges`·`KiteSwitches`) | WP2(`InvariantChecker` 가 열로 냄), WP7(로그) | 〃 (WP1c) |
| | `TimedEffectsRun`(v0.8 — `Room is null \|\| Room.TimedEffectsRun`, 계산 속성) | — | WP3(시간 효과 줄이기) | 〃 (WP1f) |
| | `Presentation.Cues`(`CueEvent`, 7.9) | WP3, WP4(신호를 내는 단계) | WP8 | `Sim/Contracts/Presentation.cs` (WP1b) |
| | 감독 상태(누적기·다음 무리·정예 판정·이벤트 위치) | WP4 | — | `Sim/Director/World.Director.cs` |
| | 무기·성장 내부 상태 | WP3 | — | `Sim/Weapons/World.Combat.cs` |

- **"방" 의 두 뜻**(v0.7.1): **안쪽 방** = 맵 안의 벽으로 둘린 구역(피 방·창살 방, 데이터 `room_areas`·`room_*` 기준점·`room_supply`·`room_pressure`, 코드 `World.InnerRooms`·`World.InnerRoomPresence`·형 `RoomFloors`·`RoomPresence`, 로그 `INNER_ROOMS`).
  **방** = 판 단위 방 하나(`World` 하나, 코드 `World.Room`·`RoomState`·`RoomPlan`·`RoomDef`·`RunState.Rooms`, 로그 `ROOM`). 데이터 키는 이미 커밋된 맵·기획 문장이 쓰므로 그대로 두고, 코드에서 같은 함수에 함께 나오는 `World` 속성만 이름을 갈랐다(WP1e #24).
- (v0.8 → v0.8.1) `RunStats`·`FairnessCounters` 의 상태는 **공개 읽기·쓰기 멤버**(공개 필드 또는 공개 get·set 속성)이고 형은 **값 형식·그 배열**뿐이다 — 계단 저장(`RunSnapshot`, 3.6)이 STJ 로 그대로 왕복하고, 반사 왕복 시험(WP1f — 공개 필드 + 공개 속성 전부)이 새 멤버를 저절로 덮는다.
  get 만 있는 속성(배열·컬렉션 포함)은 STJ 가 역직렬화 때 건너뛰어 조용히 0 이 되므로 금지(시험이 실패시킴). 사전·참조 형 멤버를 더하려면 계약 변경(11.3). 멤버를 더하면 모양 지문 시험이 깨진다 — `RunSnapshot.CurrentFormat` 과 기대 값을 함께 올린다(3.6 "계단 저장").
  `FairnessCounters` 는 partial 이 아니다(조각 없음) — 이 규칙은 모든 공개 멤버에 건다. `RunStats.DamageByWeapon` 은 `float[WeaponSlots.SlotCount]`(6, v0.8.1 — v0.8 은 4 로 남아 있었다).
- (v0.8.1) **판 수명 상태는 `HeroCarry`·`RunState`(+ 그 안의 `RunStats`·`FairnessCounters`)에만 둔다.** 모듈 인스턴스(`ModuleContext` 에 등록한 객체의 필드)·정적 필드에 판 수명 상태를 두지 않는다 — 계단 저장의 이어 하기는 새 `ModuleContext` 라 거기 둔 것은 사라지고 "이어 한 판 = 끊지 않은 판" 이 깨진다.
  방 수명 상태는 `World` 조각(위 표), 판 수명 상태가 더 필요하면 `HeroCarry`(sealed 계약 record — WP3 가 조각으로 더할 수 없음)에 넣는 계약 변경으로 **WP1g** 에 모은다. WP3 완료 기준의 "실제 모듈로 끊고 잇기" 시험이 지킨다.

### 11.3 묶음 사이 순서·충돌 방지

- WP1b 가 끝나기 전에는 다른 묶음이 코드를 쓰지 않는다. WP1b 는 계약을 이 문서 그대로 적는다. 이 문서에 없는 한 묶음 상태는 그 묶음이 자기 조각에 둔다.
- 계약(WP1·WP1b 파일) 형을 바꿔야 하면 메인 세션이 바꾸고 알린다. 다른 묶음은 계약 파일을 직접 고치지 않는다. 자기 조각 파일에 필드를 더하는 것은 자유다.
  WP2~WP8 진행 중에 생기는 계약 변경은 모아서 다음 갱신 묶음(**WP1g** — WP1d 는 v0.5.3 중간 보스·v0.6 헬파이어·직업·안쪽 방, WP1e 는 v0.7 방 구조, WP1f 는 v0.8 기획 v0.8.1 29절·AI 확대 계약으로 썼다)으로 낸다 — 한 번에 하나씩 흩어 고치지 않는다.
  (v0.5 의 WP1c 는 기획 v0.6 의 시야 계약을 WP2~WP8 시작 전에 넣는 묶음이다. WP1b 와 같은 규칙으로 혼자 하고, 끝나기 전에는 다른 묶음이 코드를 쓰지 않는다.)
- 한 묶음만 구현·사용하는 형은 그 묶음 소유다(`IFirePattern`·`ITargeting`·`WeaponStats` → WP3, 부품 계약·`PartSlot`·`ActionState`·`MoveOrder` → WP4, `Navigator`·`NavAgentState` → WP2,
  `SoundMixer` → WP8). WP1 은 레지스트리 등록 지점만 가진다.
- 부품 매개변수 record 와 그 JSON 이름은 부품을 만드는 묶음이 정하고, 같은 묶음이 그 데이터 파일을 쓴다. WP1·WP1b 는 실제 `Data/` 를 쓰지 않는다.
  예외 하나: `ground_hazard` 부품 매개변수는 Defs 의 `GroundHazardDef`(투사체 `on_end` 와 같은 모양, WP1b)를 그대로 쓰거나 같은 키 이름을 쓴다(WP4) — 같은 장판이 두 모양이 되지 않게.
  그래서 다른 묶음이 정한 매개변수 이름과 데이터가 어긋나 `Disallow` 오류가 한꺼번에 터지는 일이 없다.
- `rules` 는 파일을 나눠 쓴다(6.1): WP3 `rules/hero.json`, WP4 `rules/enemies.json`, WP5 `rules/sight.json`(v0.5, 첫 판 WP0a), WP8 `rules/audio.json`. 데이터 파일 하나를 두 묶음이 고치는 일이 없다.
  단 `rules/sight.json` 의 **시뮬레이션 값 줄**(`edge`·`view_inset`·`refresh_ticks`·`ranged_margin`)은 WP4 규칙·해시·13.5-15 를 바꾸므로 메인 세션이 WP4 와 합의해 바꾼다(6.10 키별 소유, v0.5.1).
- `FogMath`(v0.5.1): 구현은 WP1c, 소유는 WP5(그리기 규칙). 함수 시그니처는 WP3·WP4(계측)·WP6 이 부르므로 계약으로 보고 바꿀 때 이 절 규칙(메인 세션)을 따른다. 안의 값(보스 늘 보임·닿은 거리·흐림 띠)을 바꾸는 것은 WP5 가 한다.
- **최소 실제 데이터 선행**(v0.4): `tilesets/l1.json`·`maps/cathedral_arena.json`·단계 1 그림 묶음(v0.5: + `rules/sight.json`)은 WP0a 가 WP2~WP8 시작 전에 낸다(11.1). 그 뒤 소유는 아래 그대로다.
  다른 묶음이 기다리지 않으려고 남의 소유 파일에 임시 맵·그림 정의를 만들지 않는다 — 실제 데이터가 아직 없는 시험은 `TestKit` 합성 정의로 한다.
- 데이터 파일 소유(v0.3): 타일셋 `tilesets/` = WP0(번호·성질은 원본으로 확인 — v0.6 의 `marks` 도), 맵 `maps/` = WP5(배치·입구·기준점·`open_center` 은 미리보기로 맞춤), 직업 `heroes/` = WP3(v0.6), 그림·이펙트 `sprites/`·`effects/` = WP5,
  소리 `audio/`·`rules/audio.json` = WP8(연결 표가 다른 종류의 ID 를 가리키지만 그 파일들은 고치지 않는다), 가져오기 표 `Table/` = WP0.
  WP5 가 새 그림 묶음·변종이 필요하면 WP0 에 표 한 줄을 요청한다. 13.5-10 시험이 어긋남을 잡는다.
- 맵 배치를 바꾸면(WP5) 사용자 PC 의 맵 그림이 낡는다. 시작 검사가 `layout_hash` 로 잡고 가져오기를 다시 돌리라고 알린다(1.4). 검증 담당에게 "다시 가져오기 필요" 를 적어 넘긴다.
- `SimPhase` 순서표(3.5)는 WP1 소유. 단계를 더하려면 요청. (v0.7: WP1e 가 `RoomFlow` 를 16번에 더했다 — `EndTick` 은 17번.)
- (v0.6) WP1d 가 구현한 순수 로직의 소유: `Assets/ContentAvailability.cs` → WP7(시작 검사가 주 사용처), `Map/ArenaLayout.cs` 의 `grid`·`over_border`·`ValidateRooms` → 그대로 계약(바꾸면 WP0 굽기·WP2 충돌이 같이 바뀜 — 메인 세션).
  (v0.6.1) `Data/Registry/StageReach.cs` → WP4(부품이 참조하는 적을 아는 쪽), `Assets/DefAssetNames.cs` → WP7(본문). WP1d-구현이 끝나기 전에는 그 파일을 WP1d-구현 담당만 고친다.
  직업·묶음 데이터를 더하는 WP9·WP10 은 계약 파일을 고치지 않는다. 고쳐야 하면 11.3 의 다음 갱신 묶음으로 모은다(v0.7: WP1e 는 방 구조, v0.8: WP1f 는 29절·AI 확대 계약으로 썼다 — 그다음은 WP1g).
- (v0.8) **WP1f 목록은 닫혔다**: 아래 v0.7.2 의 ①~⑧ 은 모두 11.1 WP1f 표에 들어갔다(① → #7, ② → #8·#9, ③ → #1·#2·#14, ④ → #10·#13, ⑤ → #3, ⑥ → #10·#11·#12·#15, ⑦ → #5·#6·#8, ⑧ → #6·#12). 29절에서 v0.7.2 목록에 없던 것도 함께 넣었다 —
  ⑨ 층(`RoomDef.Floor`·`RoomExit.NextFloor`·`transition_floor` — 29 #25·#19), ⑩ AI 확대 자원 모양(`UpscaleTable`·`ManifestUpscale`·메타 `scale`·형식 판 3 — 사용자 결정 U), 허리띠 신호 `CueKind.BeltPotion`.
  **WP1f 가 끝나면** WP0·WP2~WP8 은 이 멤버의 모양·값에 기대도 된다(아래 v0.7.2 의 "기대지 않는다" 는 WP1f 커밋으로 풀린다). 그 뒤 새 계약 변경은 WP1g 로 모은다. 29절 중 계약을 바꾸지 않는 것은 13.20 표에 처리 자리를 적었다.
  (v0.8.1) v0.8 리뷰가 찾은 계약 구멍도 **같은 WP1f 에 넣었다**(새 묶음을 만들지 않음 — 13.21): `RoomState.RemapEntry` 규칙·`RemapMarch`(#8), 거울판 `entries` ID·입구-문 검사·자리 짝 검사(#6), `RunSlots.OpenAt`·`Suspend` 의 틱·이어 하기 대조·`RunSnapshot.Carry` nullable·`CurrentFormat`(#11), `IHeroCarry.Validate`(#10),
  `RunStats.DamageByWeapon` 6칸·`WeaponSlotCount` 없앰·저장 멤버 규칙(#14), `Hero.Reset` 의 칸 수 0(#13), `DoorsOpened` 자리·`RewardSpot` 두 번째 문턱(#9), `UpscaleTable.Applies` 둘·`output_scale` 2 고정(#16). 그래서 WP1f 뒤 WP2~WP8 이 기대는 목록은 위 ①~⑩ 과 이 줄이 전부다.
- (v0.7.2 — v0.8 에 닫음) **WP1f 에서 바뀌는 멤버**(기획 v0.8.1 29절 — 설계 v0.8 이 받은 뒤, 13.19 끝): WP2~WP8 은 아래 멤버의 **지금 모양·값에 기대는 코드·시험을 쓰지 않는다**(읽어 지나가는 것은 되지만 값을 굳히거나 분기를 걸지 않는다).
  ① `RunPlanner.PlanAct` ③ 출구 수(정예·보스 앞 방도 둘·셋 — 고정 값 시험이 바뀜)와 ①의 맵 뽑기(거울판 쌍), ② `RoomFlow.BeginReward`·`OpenDoors` 의 보상 자리(`reward_anchor`/`center` → 주인공 앞 100px)·정예·보스 앞 문의 보상(정예 상자 + 덤),
  ③ `RewardKind`·`RewardDef` 효과 필드(무기 강화 꾸러미·허리띠 물약·성소 효과, 0장 대체 `단련` — 지금 `Skip.Gold`), ④ `HeroCarry` 필드(허리띠·`단련`·성소 대가·시간 효과 남은 틱),
  ⑤ `RoomRulesDef` 키의 뜻(`cleanup_limit` 값, `reward_anchor`, `remaining_marker` 문턱 — 정리 시작부터),
  ⑥ `RunState`·`RunSession`(계단 저장의 판 상태 직렬화·이어 하기 생성 경로, 계단 보상 칸 +1 을 넘기는 자리, `--start-at` 상한 — 생성자 검사·`RoomStart.StartClockTicks`)와 `RoomRecord`(`ROOM` 로그 보탬 `mirror`·`boss_s`·`picks_left`),
  ⑦ `RoomPlan`(거울판 방향)·`MapDef`(거울판 쓰기 여부)·타일셋 짝 표, 거울판의 문 이름 짝 바꾸기 — `RoomState.RemapEntry`·`GateZones`·이벤트 `entry:`,
  ⑧ 13.5-23 검사(`ArenaLayout.ValidateRooms`·`RoomMapRules` — 거울판에도 돌림, 방 이벤트 자리 0:20·끝 0:45 검사).
  기획 29절 번호로: ① = 5·13, ② = 6, ③ = 4, ④ = 4·7·8, ⑤ = 6, ⑥ = 8·14·15, ⑦ = 13, ⑧ = 11·13. 나머지(1 은 받음, 2 는 v0.7.1 에 받은 기록 비교 키(본문 WP7 저장), 3 은 아직 없는 `StateHasher`(WP7), 9·19 는 데이터 값, 10·17 은 끝남, 12·14 의 봇·감독은 WP4 본문, 16 은 시험, 18 은 가져오기 도구)는 계약 멤버를 바꾸지 않는다.
  WP1f 를 WP2~WP8 시작 전에 낼지(설계 v0.8 먼저), 이 목록만 두고 나란히 갈지는 사용자·메인 세션이 정한다. — v0.8: WP2~WP8 시작 전에 낸다(11 머리 순서, 사용자 진행 순서 "WP1f → WP0·WP2~WP8").
- (v0.8) WP1f 가 구현한 순수 로직의 소유: `Sim/Run/**` 에 더한 것(`RewardSpot`·`RunSnapshot`·`RoomStartLimits`·`RoomEvents.CheckPlacement`) → WP4(아래 v0.7 과 같음 — 단 `RewardSpot` 규칙·`RunSnapshot` 모양·`RoomStartLimits` 는 계약: 바꾸면 판 결과·저장 파일·옵션 오류가 바뀐다),
  `Map/MapMirror.cs`·짝 표 검사 → 계약(메인 세션 — 바꾸면 WP0 굽기·WP2 검사·WP5 그림이 같이 바뀜). 데이터: L1 짝 표 = WP0(`tilesets/`), 맵 `mirror` = WP5, `Table/upscale.json` = WP0.
  (v0.8.1) `Sim/Run/RunSlots.cs` → 계약(칸 수 = 판 결과·옵션 오류), `RoomState.RemapEntry`/`RemapMarch` → 계약(이벤트 문 — 판 결과). **TestKit `RoomActs.cs`**(v0.7.1 의 #23-구현 — WP1e 가 끝나 WP4 소유)는 WP1f 가 짝 표 타일셋·거울판 맵·층을 더하므로 **WP1f 가 끝날 때까지 WP1f 가 맡고**, 끝나면 다시 WP4 로 넘긴다(그 사이 WP4 는 시작 전이라 겹치지 않음).
- (v0.7) WP1e 가 구현한 순수 로직의 소유: `Sim/Run/**`(`RunPlanner`·`RoomEvents`·`RoomFlow`·`RunSession`) → **WP4**(감독·`Scenario`·자동 조작이 주 사용처). 단 `RunSeeds` 의 섞기·`RunPlanner` 의 뽑기 순서·`RoomFlow` 의 단계 전이는 판 결과를 바꾸므로 계약이다 — 바꾸려면 메인 세션(같은 씨앗의 기록·`--room` 같음이 깨진다).
  `Map/GateZones.cs`·`MapArea.cs`·`RoomMapRules.cs`·`ArenaLayout.ValidateRooms` 의 13.5-23 부분 → 계약(메인 세션 — 바꾸면 WP0 굽기와 무관하지만 WP2·WP5·WP6 이 같이 바뀜).
  데이터 파일 소유: `rules/rooms.json`·`stages/` = WP4, `rewards.json` = WP3, 작은 방 `maps/` = WP5, `rules.rooms` 의 `transition`·`transition_floor`(그림 값 — v0.8: `remaining_marker` 는 없앰)는 WP6 이 WP4 에 요청해 바꾼다.
  (v0.7.1) `TestKit` 의 경계: **#23-형**(`TestDefs.Stage`·`WithRoom`·`FakeHeroCarry`·`FakeSpawner`·합성 `rules.rooms`·합성 보상)은 형과 같이 나오는 공용 도우미라 형 뒤에는 계약처럼 다룬다 — 다른 묶음이 비슷한 도우미를 따로 만들지 않고, 바꿀 것은 메인 세션에 요청한다.
  **#23-구현**(`TestKit/RoomActs.cs` — 합성 액트·작은 합성 맵)은 WP1e-구현이 끝날 때까지 그 담당만 고치고, 끝나면 WP4 로 넘긴다.
- (v0.9) **WP1g 순서와 소유**(기획 v0.9·v0.9.1 — 11.1 WP1g):
  순서: 지금 돌던 버그 수정(`fix/playtest`)·방 맵(`maps/act1`)을 main 에 합친다 → **WP1g**(혼자 — 계약 파일과 단계 1 코드의 옛 이름 자리를 고침) → 단계 2 묶음 동시. 버그 수정·방 맵 브랜치가 WP1g 보다 늦게 끝나면 WP1g 가 그 위로 다시 맞춘다(그쪽은 계약 파일을 고치지 않으므로 겹침은 옛 이름 쓰는 자리뿐).
  **WP1g 가 끝날 때까지** 다른 묶음은 `RewardKind.BeltPotions`·`HeroCarry.Belt`·`rules.hero.belt`·`CueKind.BeltPotion`·`healing_potion` 의 `heal` 효과에 기대는 새 코드를 쓰지 않는다(WP1g 가 지움). 레벨업 아이콘을 원작 `spelicon` 으로 바꾸는 가져오기 일(WP0)은 계약과 무관해 WP1g 와 나란히 해도 된다 — 다만 `Table/icons.json` 은 WP0 소유 한 곳에서만.
  WP1g 가 구현한 순수 로직의 소유: `Sim/Items/**`(`ItemRoller`·`EquipStats`·`HeroLook`) → **WP3**(주인공·성장이 주 사용처. 단 `ItemRoller` 의 난수 순서·`HeroLook.Key` 글자는 계약 — 바꾸면 저장·그림이 같이 바뀜),
  `Sim/Run/ShopStock.cs`·보상 뽑기의 보장·거르기 → **WP4**(v0.7 의 `Sim/Run/**` 와 같음 — 단 상인 전용 난수 흐름 이름은 계약), `ShopState`·`RoomState.Shop` → 계약.
  데이터 파일 소유: `items/`·`merchants/`·`rules/equipment.json` = WP3, `rules.rooms.rewards` = WP4, `rewards.json` 의 새 종류 = WP3, 가져오기 표 새 묶음 = WP0, 주인공 아홉 벌 그림 묶음(`sprites/hero.json`) = WP5.
  짐(`HeroCarry`)·`RunSnapshot` 필드가 또 필요하면 다음 갱신 묶음(**WP1h**)으로 모은다 — WP1g 뒤에도 sealed 계약 record 규칙(11.2)은 그대로다.

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
//   (v0.7.1: Reset()·ResetCombat 없앰. v0.8.1 WP1f: WeaponSlotCount 상수 없앰, DamageByWeapon { get; set; } = new float[WeaponSlots.SlotCount](6) — 3.6 "계단 저장" 저장 멤버 규칙)

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
| R9 | 묶음 병렬 작업 중 계약 변경 | 빌드 깨짐·되돌리기 | WP1b 로 계약을 먼저 한 번에 고정, 진행 중 변경은 메인 세션 경유·모아서(11.3). 한 묶음 상태는 자기 partial 조각에 두므로 계약 변경이 드물다(11.2). 각 묶음은 `TestKit` 으로 다른 묶음 없이 시험. v0.7: 방 구조가 커밋된 형(`StageDef`·저장·`SimPhase`)을 바꿨지만 WP2~WP8 시작 전이라 WP1e-형 하나로 낸다(11.1) — 시작 뒤였다면 여러 묶음을 되돌려야 했다 |
| R10 | 기획 수치·정의가 데이터와 어긋남 | 조용히 다른 밸런스 | 데이터가 정본, 기획서 표와 맞추는 테스트는 핵심 몇 개만(경험치 곡선, 매직 미사일 표). 수치 변경은 데이터 + 기획서 함께 |
| R11 | 표시 쪽 시체·이펙트를 시뮬레이션 밖에 둔 결정 | 나중에 "시체로 소환"·"시체 먹기"(E `scavenger`) 같은 규칙이 생기면 옮겨야 함 | 그때 `Corpse` 저장소를 Core 로 옮긴다(표시 이벤트 형은 그대로). 지금 MVP 규칙에는 없음 |
| R12 | 한글 글꼴이 없는 PC | 글자 네모 | 8.2 대체 순서 + 로그 경고 |
| R13 | 자원 없는 클론 | 빌드 실패·실행 안 됨 | DDR0001 문장 + `SkipAssetCheck`, 실행은 안내 화면(명령 한 줄), Core 시험은 자원 없이 전부 통과(길 검사 포함, 4.2) |
| R14 | E 단계 아군(가디언 포탑, 기획 11.3)을 넣을 때 계약 변경 | WP2·WP3·WP4 계약을 함께 고쳐야 함 | 지금 `Faction`·`IHostileQuery` 자리만 둠(5.1). 넣을 때 바뀌는 것: `Ally` 저장소·공통 조각, `IHostileQuery` 가 아군 해시도 봄, 적 투사체 맞음 판정 대상 목록, 포탑 자리 `PlacementResolver`, 무기 패턴 `summon`. 포탑은 움직이지 않고 피해를 받지 않아 v0.2 의 수호 검사보다 작다 |
| R15 | partial 조각에 필드를 아무 데나 둠 | 소유가 흐려져 같은 필드를 두 묶음이 씀 | 11.2 표에 없는 공용 필드는 계약 변경 절차(11.3). 각 묶음 조각에는 그 묶음만 쓰는 필드만 |
| R16 | 몬스터를 늘렸는데 구별이 안 되거나 어느 스테이지에도 안 나옴 | 사용자 요구 8 이 화면에서 안 보임 | 6.14 경고 두 개(같은 묶음·변종·덧입힘, 안 나오는 적) + `--preview enemies` 고정 배치 캡처를 검증 담당이 판정(10.2) |
| R17 | 원작 형식 해석 오류(CL2 그룹·평면 겹침, 프레임 폭 추정, MIN 뒤집기, 삼각형 패딩, 0/1 기반 섞임) | 그림이 깨지거나 밀림 | 시제품이 확인한 규칙을 저장소의 부록 A 로 옮겨 정본으로 둠(조사 문서·시제품은 임시 폴더라 사라질 수 있음), 시험 인코더로 만든 바이트 시험(10.1), 폭은 파일 추정 + 표 대조(1.4), WP0 `contact` 견본을 눈으로 확인 |
| R18 | 사용자 원본 판이 다름(다른 설치본, 파일 이름·크기 차이) | 가져오기 실패 또는 다른 그림 | 기본 내용은 `diabdat.mpq` 에만 기댐, 없는 파일은 목록과 종료 코드 3, manifest 에 원본 크기·SHA-256 을 적어 문제 보고 때 비교. v0.6: 헬파이어 MPQ 도 원본 줄에 적고, **있는 묶음 MPQ 의 빠진 파일은 "없음" 으로 덮지 않고 실패**(1.4) — 판이 다른 MPQ 로 반쯤 켜진 묶음이 생기지 않게 |
| R30 | (v0.6) 헬파이어 묶음 유무가 PC 마다 다름 | 같은 씨앗의 결과·최고 기록이 PC 마다 달라짐, 기본 스테이지에 헬파이어 몬스터가 섞이면 없는 PC 에서 소리·그림 없는 적 | 시뮬레이션은 묶음을 모름(6.17 — 소스 문자열 시험), 꺼짐 단위 = 스테이지·직업, 13.5-20 이 `requires` 를 위로 덮게 함(기본 내용이 묶음 내용을 못 가리킴), 레벨업 후보는 정의로 거름(6.8). 10.2 "헬파이어 없음" 행이 같은 씨앗 해시를 비교 |
| R31 | (v0.6) 직업을 더할 때 계약을 다시 고침(결정 K 의 반대) | WP2~WP8 끝난 뒤 계약 변경 묶음 | 형·월드 인자·소리 키·저장 모양을 WP1d 에서 직업 단위로 정함. MVP 도 `sorcerer` 정의 하나로 돌려 경로를 매일 씀. 남는 위험: `melee_arc` 의 맞음 판정·근접 시전 클립 재생 — WP9 에서 처음 쓰임(패턴 등록·무기 정의는 데이터라 계약 밖). v0.6.1: 저장 계약에 직업(`RunResult.HeroClassId`·`LastHero`·`BestRecord`), 특성 = 패시브(`Traits`, 6.16) — 능력치 키로 안 되는 특성 효과는 패시브 일반의 확장(효과 부품)이라 직업 계약 밖이지만 그때 `PassiveDef` 를 고친다 |
| R32 | (v0.6) 안쪽 방 배치의 갇힌 바닥 띠·좁은 틈 | 그림은 바닥인데 아무도 못 감, 큰 적이 방에 못 들어가 주인공이 숨음 | `over_border` 로 둘레에 겹쳐 놓기(6.11), 13.5-5 갇힌 바닥 칸 오류(자동 막힘 처리 안 함)·26·34 → 방 기준점, 13.5-4 의 80px 규칙, 합성 띠 시험(WP1d 맵 → WP2). v0.6.1: 벽 맞물림 규칙(꺾이는 자리 = 모서리 조각, 6.11)·`over_border` 막힘 줄어듦 오류·13.5-19 하위 칸 검사(갇힌 바닥·하위 1칸 틈 — 자원·격자 없이 WP0a 에서 바로)·6.11 예 배치를 그대로 쓴 합성 맵 시험, 그 위에 WP2 가 실제 격자로 확인. 시제품 길 검사는 저장소 배치를 읽지 않으므로 확인 수단에서 뺐다 |
| R33 | (v0.7) 방마다 `World` 를 새로 만드는 비용(배치 풀기·충돌 도형·clearance·첫 필드·저장소 풀) | 방 이동 프레임이 길어 멈칫함 | 0.5s 어두워짐 가운데(덮개 1)에서 한 번만, 텍스처는 액트 시작 때 미리 읽음(7.5). `Perf` 로 ≤ 150ms(10.3), 게임은 `slow_frames` 하나만 허용. 넘으면 ① 맵별 펼친 격자·충돌 도형·clearance 를 판 수명 캐시(`RoomMapCatalog` — 맵은 바뀌지 않으므로 공유해도 상태가 새지 않음) ② 저장소 풀을 `RunSession` 이 다음 월드에 넘겨 재사용(지운 상태 확인 시험 필요) 순으로 줄인다 |
| R34 | (v0.7) 방 흐름이 멈춤(구석에 박힌 적, 안 열리는 문, 집을 수 없는 보상) | 판이 끝나지 않음 | 정리 안전장치 25s(v0.8 — v0.7 45s, 무너짐) + 사냥 모드, 보상 자리 = 주인공 앞(v0.8 `RewardSpot` — 주인공에서 직선으로 이어진 clearance ≥ 30 자리라 늘 닿음. v0.7 의 `center` 는 13.5-5 길 검사가 봤다), 정예 상자·덤도 같은 줄에, 나가는 자리 = 문 틈 칸(v0.7.1: 주인공이 닿는지는 13.5-23 ③ 문 틈 닿음 검사 — 13.5-5 는 진입점까지만 봄), 자동 조작은 20s `DOOR_SKIP`·`REWARD_STALL`(로그로 드러남 — 시험 기준 0), 정예 방 2:30 뒤 중간 보스 쫓기, 0장 보상은 줍는 순간 `단련`(v0.8 — v0.7.1 `skip`), 보상 물체는 늘 집힘(v0.7.1), 시나리오 방 틱 상한(넘으면 방·단계를 적고 실패). `ROOM` 줄의 `collapsed`·`cleanup_s` 로 첫 플레이에서 본다 |
| R35 | (v0.7) 방을 넘기며 결정성이 깨짐(넘기지 않은 상태가 남거나, 넘긴 상태가 빠짐, 뽑기 순서가 바뀜) | 같은 씨앗이 다른 판, `--room` 재현 실패 | 넘기는 것을 `HeroCarry`·`RunState` 두 형으로 한정(3.6 표), 판 흐름은 `RunPlanner` 순수 함수·뽑기 순서 고정 시험, 방 씨앗 분리, 해시에 판 흐름·방 상태, 액트 실행 두 번 같은 해시·`--room` 같음 시험(10.1 `Run`·`Sim`). v0.7.1: 방 해시 / 판 해시를 나눠 `--room` 비교가 판 누적에 걸리지 않게, 게임 쪽 월드 수명 상태는 `IWorldBound` 한 곳(7.11), 판 전체 시나리오(단계 3) |
| R36 | (v0.7) 한 판 약 35분 + 판 중간 저장 없음 | 사람이 판을 끝까지 못 함, 검증 실행이 길어짐 | 기획 열린 질문 33. v0.8: **계단 저장**(3.6·8.3 — `RunSnapshot`, 불러오면 지움)으로 액트 사이에서 끊고 잇는다. 이어 한 판 = 끊지 않은 판(판 해시 시험). 검증은 `--room`·`--start-at` 으로 방 단위(10.2), 전체 판은 단계 끝 실행·Core "판 전체" 에만 |
| R38 | (v0.8, 사용자 결정 U) AI 확대 자원으로 그리기 비용·그림 메모리가 늘고, 확대한 그림과 원본 그림(주인공·마법)이 한 화면에 섞임 | `WorldRT` 4배 픽셀(채움), 확대 묶음 4배 바이트·페이지 수 증가(그리기 호출), 벽 기둥 이음매·걷기 프레임 떨림, 작은 캐릭터만 거친 픽셀로 보임 | 기본 끔(선택 기능). 범위 첫 값을 맵 그림 + 칸 160 이상 몬스터로 좁힘, 렌더 측정을 s = 2 로도(WP5 — `draw_ms`·`vram_mb` 를 R7 에), 이음매·떨림은 10.2 "AI 확대" 행에서 사람이 판정. 나쁘면 범위를 표에서 고쳐 다시 가져온다(코드 변경 없음 — v0.8.1: `output_scale` 은 첫 판 2 고정이라 바꾸려면 WP1g, 기둥 이음매는 여백이 아니라 벽 줄째 확대·기둥 빼기 — 7.6). v0.8.1: 맵 그림 메모리에 거울판 `<art>@m` 이 원판만큼 더해짐(7.1·7.5). 작은 캐릭터 확대·화면 배율은 열린 질문(13.20) |
| R39 | (v0.8) 가져오기가 사용자 PC 의 외부 실행 파일을 띄움 | 모르는 실행 파일이 돌거나, 같은 입력에 GPU 마다 다른 결과 | 표의 허용 SHA-256(실행 파일·DLL·모델 모두)과 다르면 띄우지 않고 원본 픽셀로(종료 코드 6), 셸 없이 인자 목록으로·창 없이·`-m` 으로 확인한 모델만·진행 기준 시간 제한(v0.8.1 — 전체 10분 대신 묶음마다 "출력이 2분 동안 안 늚"), `vulkan-1.dll` 이 없으면 띄우지 않고 오류 창이 뜨지 않게 오류 모드를 정함(v0.8.1). 결과 바이트는 결정론 대상에서 빼고(1.4) manifest 에 도구·모델 해시만 — 시뮬레이션·해시는 그림 배율과 무관(7.1). 가짜 실행 파일로 `Process` 경로를 시험(10.1 `Pipeline`) |
| R40 | (v0.8) 거울판의 짝 조각이 그림·성질에서 원판과 어긋남(원작 타일은 빛 방향이 있어 뒤집지 않고 짝으로 바꿈) | 거울판 방만 충돌·시야가 그림과 다르거나 벽이 맞물리지 않음 | 짝 표 검사(성질 전치 같음 — 13.5-12), 거울판에도 13.5-4~6·19·23 과 `audit`, 맞물림은 `--preview tiles --map <id>@m`(10.2). 짝이 없는 조각을 쓴 맵은 `mirror` 를 못 단다(오류) |
| R41 | (v0.8) 계단 저장이 다른 판의 데이터·다른 판 코드로 읽힘(게임을 고친 뒤 이어 하기) | 이어 한 판이 다른 방 표·없는 직업으로 시작하거나 예외 | `RunSnapshot.Format`·다음 액트 `rooms_hash`·직업 존재, v0.8.1: 짐의 정의 ID(무기·패시브·축복·성소)·`IHeroCarry.Validate`·`Carry` null 여부를 생성자가 `Restore` 전에 확인해 `RunSnapshotMismatchException` → 저장은 이미 지웠고 한 줄 안내(8.3). **필드를 더할 때의 규칙은 하나**(v0.8.1 — v0.8 의 "필드가 늘어도 옛 저장을 읽는다(없는 값 = 기본)" 는 `required` 멤버(없으면 STJ 예외)·`WeaponSlotsOpen` 처럼 기본 0 이 틀린 값인 필드와 맞지 않아 지웠다): 짐·`RunStats`·`FairnessCounters`·`RunSnapshot` 모양이 바뀌면 `CurrentFormat` 을 올리고 옛 저장은 버린다 — 모양 지문 시험이 올리기를 잊지 않게 한다(3.6) |
| R37 | (v0.7) 작은 방(32×32)에서 생성 띠의 큰 부분이 맵 밖 → 입구(문) 대체가 잦음 | 무리가 문에서만 쏟아져 흐름이 바뀜 | R23 측정을 작은 방에서도(WP2·WP4), 30% 넘으면 기획 열린 질문 35 의 다음 수(N 36). 들어온 문은 대체 입구에서 뺌(기획 2.4) |
| R19 | 타일 성질 표가 원본 SOL 과 어긋남 | 충돌과 그림이 어긋남(요구 3) | 가져오기가 대조해 실패(1.4), 의도한 차이는 `sol_known_diffs` 에 이유와 함께. `--preview tiles` 겹침 캡처 |
| R20 | 등각 y 정렬 규칙 오류(벽이 유닛을 잘못 가림, 아치 아래 유닛, 길게 이어진 벽) | 화면이 이상해 보임 | 둘레 벽은 정렬 밖 단계(0b·5층), 안쪽 기둥만 y 정렬, 아치 덧그림 칸 안 유닛 키 당김(7.4), `--preview tiles` 와 기둥·뒷벽 둘레를 도는 자동 캡처(10.2). 협로 벽이 틀리면 줄 단위 무리로 뺌 |
| R21 | 소리 장치 없음·8비트 WAV·긴 음악 메모리·MonoGame 이 늘 장치를 엶 | 시작 실패·소리 없음·검증 실행이 스피커를 씀 | 가져오기에서 16비트로 변환(7.9), 개발 옵션 실행은 `ALSOFT_DRIVERS=null`, 장치 초기화 예외는 null 드라이버로 다시 만들기, 못 열면 무음으로 계속 + 로그, 음악은 두 곡까지·읽기는 작업 스레드 |
| R22 | 자원이 낡음(형식 판 올림, 가져오기 표 바뀜, 굽기 코드 바뀜, 가져오기 중간 실패 뒤 섞인 출력) | 낡은 `Assets` 로 조용히 이상하게 그려짐 | 캐시 키 = 도구·Core 어셈블리 해시, 굽기 결과가 바뀌면 `AssetFormat.Version` 올림(WP0 규칙), 시작 검사 ①~⑤(판·표 해시·이름·파일 크기·`layout_hash`(타일셋 포함)) → 안내 화면, 원본 manifest 가 없으면 빌드가 출력 manifest 를 지움(1.3·1.4) |
| R23 | 마름모 맵에서 생성 띠의 큰 부분이 맵 밖 | 주인공이 모서리에 있으면 12번 시도가 자주 실패 → 입구로만 들어와 흐름이 바뀜 | WP4 가 모서리 주인공 시험에서 입구 대체 비율을 잼. 높으면 띠 후보를 "띠 ∩ 맵 경계 상자" 에서만 뽑거나 시도 수를 늘린다(`rules.spawn.tries`). v0.7: 작은 방은 R37 |
| R24 | 저작물·사용 조건(Blizzard 자원, DevilutionX "Sustainable Use License") | 공개 저장소에 올라가면 문제 | ① 자원: `.gitignore`(`Assets/*`, `out/`, `logs/`, 원작 형식 확장자), 도구 출력 경로가 저장소 안 무시되지 않는 곳이면 거절(1.4), 캡처·견본·로그는 저장소 밖에(9절·10.2, `docs/media` 포함), 도구 시험 픽스처는 시험 인코더가 만듦(원본에서 자른 바이트 금지), `audit` 명령(추적 파일 해시를 출력·원본 항목 해시와 대조 + 확장자 목록)을 WP0 완료 기준에. ② DevilutionX: 도구·게임이 `*.tsv`·DUN 을 읽거나 저장소에 넣지 않는다(v0.6.1 예외 하나: `audit` 가 유출 대조를 위해 원본 DUN 을 메모리에서만 읽음 — 내용을 출력·저장하지 않음). **원작 방을 본뜬 배치는 크기·문 방향·벽 재료만 따르고 칸 배열은 우리 조각 이름으로 새로 짠다 — DUN 을 칸마다 옮겨 적지 않는다**(6.11 "원작 방 본뜨기", 1.4 `audit` 의 4×4 배열 대조. `Data/maps/*.json` 은 13.5-16 출처 주석 검사 대상이 아니므로 이 대조가 유일한 자동 그물이다). **원작 게임 동작을 재현하는 사실 수치만**(칸 폭·프레임 간격·타격 프레임·SOL 보정 칸·아치 덧그림 번호표) 우리가 확인해 손으로 적고, 파일·코드·표 구조·주석은 옮기지 않으며, 값마다 출처 주석(`원본 확인`/`DevilutionX 참고`)을 남긴다(1.4). 성질 표 키 ⊆ 쓰는 메가타일 검사가 SOL 덤프를 막는다(6.11). 수치 사실을 이 범위로 옮기는 것은 **사용자가 확인했다**(v0.5, 기획 열린 질문 19 닫힘·기획 19.1). 배포할 일이 생기면 다시 정함 |
| R25 | `Assets` 복사 양(출력 폴더마다) | 빌드가 느림·디스크 | `PreserveNewest`. 느리면 하드 링크 복사를 켬(1.3). 다른 출력 폴더(`-o`) 빌드가 잦으면 잰 뒤 판단 |
| R26 | MPQ 를 여러 스레드가 같이 읽음 | 바이트가 섞여 그림·소리가 깨지거나 실행마다 다름(캐시 키도 깨진 바이트로) | 원본 읽기는 주 스레드에서 차례로 `byte[]`, 병렬은 해독·굽기·압축만(1.4), 시험 `--jobs 1` = `--jobs 8` |
| R27 | 핀드(장애물 통과)가 둘레 벽 칸 안으로 들어감 | 벽에 가려 안 보이고 투사체로 못 맞히는 자리에서 공격 | 둘레 도형 `IsBoundary` 는 통과 유닛도 막음(4.2·4.7) |
| R28 | 안개를 화면 공간 가림(반해상도 `FogRT`)으로 합침(v0.5) | 벽 윗부분·유닛 머리가 뒤 칸의 화면 자리에 걸쳐 검게 잘림, 경계가 번지거나 칸 계단이 보임 | 벽은 자기 칸 값, 그리는 대상은 자기 알파로 최댓값 덧그림(7.10 3·4단계) — v0.5.1: 값은 **알파 채널**에(색 채널이면 벽·적 그림 색이 곱해져 색² 로 어두워짐), 두 줄 벽은 함께 보임(4.11). 10.2 "안개 고정 장면" 의 빛 값 대조를 단계 1 완료 조건에(WP5). 남는 경우(숨은 벽이 앞으로 솟는 겹벽)는 벽 항목만 "지우기 + 더하기" 로 덮어씀(7.10). 그래도 어긋나면 원작처럼 **그리기 항목마다 자기 칸 값을 틴트로 곱하는** 길로 바꾼다(셰이더 없이 되고 땅만 투영이 남음) |
| R29 | 시야 조건 경계에서 원거리 적이 쏘기·다가가기를 오감(띠 끝·벽 모서리·화면 위아래) | 궁수가 떨며 제자리걸음, 쏘는 수가 기획과 다름 | 빛 반경 ≥ 띠 최대 + 60(13.5-15)으로 띠 끝에서 늘 또렷이 보이게. v0.5.1: 화면 위아래는 v0.5 의 "254 > 띠 최소" 로는 정위에서 14px 창뿐이었고 몸 위 끝까지 보면 창이 없다 → `kite` 띠 당기기(기획 v0.6.1: 최대' = min(최대, L − 10), 최소' = min(최소, 최대' − 40), 띠 폭 ≥ 40 — 6.6). 시험(10.1 `Enemies` 정위 궁수 전환 수)·`kite_switches` 열(10.1)·`--debug-nav sight` 캡처로 확인 |

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
| 10.1·10.2 낮은 턱 | 앞쪽 둘레는 낮은 턱 — 걷기만 막고 투사체는 넘어감(`blocks_projectiles: false`) | 원본 SOL 대로 앞둘레 턱도 `X`(걷기·투사체 둘 다 막힘 — v0.5 글자로는 시야까지 셋 다). 걷기만 막는 칸은 MVP 맵에 없음. 의도한 차이가 필요하면 `design_diffs` | diabdat 직접 확인: 메가타일 18·19·20·24 = SOL 15(4.2). 문서대로 두면 가져오기 성질 대조가 실패한다 — 기획 v0.5 10.1·10.2 가 같은 사실로 고쳤다(차이 없음) |
| 8.2·9.5 오라·함성 대상 | 대장 오라는 해골 4종과 다른 대장에만, "다른 종에는 걸리지 않음". 보스 본체 언급 없음 | `command_aura`·`war_cry`·`buff_area` 의 `include_bosses` 기본 `false`(정의 `rank` 가 miniboss·boss 면 안 걸림), 해골왕에 `skeleton` 태그를 달지 않음 | 9:00 호위 대장 오라가 10:00 해골왕의 이동·피해를 경고 없이 올리는 일을 막음(6.5). 지휘 오라는 기획 8.2 대로 확정. 함성(`war_cry`·`buff_area`)도 보스 본체에 안 걸림 — v0.5 에 사용자 결정으로 확정(기획 4.2·열린 질문 21 닫힘, 기획 v0.6 이 같은 문구라 이제 차이 없음) |
| 4.1·8.1 걷기 재생 배율 | 실제 속도 / 기준 속도 | 땅 기준 속도(`vx`, `2·vy` 벡터의 길이) / 기준 속도 | 원작 걷기 그림은 땅 위 걸음 기준이라 화면 세로 이동에서 발이 미끄러짐(4.1). `--preview` 로 확인 |
| 12.6 큰 피해 | (v0.4) "예고 없이 큰 피해 없음"(기준값 없음). v0.5: 범위 즉발·돌진 + 기본 피해 ≥ 10 → 예고 ≥ 0.4s, 근접 단일(사거리 ≤ 몸 반경 합 + 50)은 면제·피해 ≥ 25 면 타격까지 ≥ 0.35s, 투사체·장판 틱 면제 | 기준값을 `rules.enemies.telegraph_required` 한 곳에 두고 **부품 종류**로 범위·돌진·근접·투사체·장판을 가른다(6.5). 오류. 단일 대상인데 사거리가 기준을 넘는 근접은 경고 | 기획 기준을 그대로 담음. 두 갈래에 들지 않는 경우만 설계가 경고로 보탬 |
| 10.5 조명 판독성 | (v0.4) 빛 밖은 거의 보이지 않음. v0.5: 유닛 밝기 하한 0.6(주인공·적·시체·획득물), 어둠 ≤ 0.7, 빛 반경 380~480, 그리는 방법은 설계 | `unit_min_light` = 빛 지도에서 대상 모양 안만 max(L, m) 으로 올림(모양은 알파 채널, 안개 알파 × 그림 알파만큼 — v0.5.1), 앞 벽이 겹친 대상은 건너뜀(7.7). 값은 기획서 그대로(6.12) | 값은 기획, 그리는 방법은 설계(기획 10.5) |
| 10.2 가림 ② | 주인공보다 앞이고 "주인공 빛 반경 안" 의 벽 | 벽 칸 마름모 중심과 주인공 발의 거리 ≤ 현재 주인공 빛 반경. `wall_fade.mode = "hero"` 로 ① 만 쓸 수 있음(비교용) | 판정을 칸마다 한 번의 거리 비교로(7.6) |
| 10.3 목표 문 행진 | 목표 문의 진입점에 "닿으면" 보통 행동 | 진입점에서 48px 안 또는 진입점을 지나치면(`MoveOrder.Target`, 6.6) | 20마리 무리는 분리에 밀려 모두 그 점을 밟지 못한다 |
| 19.2 프레임 수 | 도구가 원본 파일에서 읽음 | 도구가 읽고, 정의에도 적어 자원이 있을 때 대조 | 자원 없이 이벤트 프레임 범위를 검사하려면 정의에 수가 있어야 함(6.3) |
| 19.2 PNG 묶는 방식 | 첫 단계는 고정 칸 시트(단순) | 처음부터 프레임을 잘라 묶은 아틀라스 | TRN 변종마다 시트가 한 벌씩이라 고정 칸이면 스테이지 1 만 300MB 를 넘음(7.5). 렌더 쪽 복잡도는 사각형 하나 더 읽는 정도 |
| 10.4 단계 1 구간표 | "1단계용 구간표를 따로 둔다 — 설계" | 단계 1 에는 단계 1 판 스테이지 파일만 두고 단계 2 에서 바꿈(두 판을 같이 두지 않음) | 같은 ID 두 판은 검증·자동 조작 명령을 헷갈리게 함(6.12) |
| 10.7 선이 칸 꼭짓점을 지날 때 (v0.5) | "칸의 중심까지 가는 선이 시야 막힘 칸을 지나지 않으면 보임"(꼭짓점은 정하지 않음) | 꼭짓점 양옆 두 칸이 **둘 다** 막힘일 때만 막힘 | 꼭짓점끼리 이은 대각선 벽으로 시야가 새지 않고, 기둥 하나의 모서리를 스치는 선은 막히지 않는다(4.11) `[해석]` |
| 10.7 시야 갱신 주기 | 늦어도 6틱마다 + 주인공이 다른 칸에 들어선 틱 | 선 계산은 주인공 칸·반경이 바뀐 틱에만(그 사이 결과가 같다), 가 본 칸 표시는 6틱마다 + 그 틱 | 같은 결과에 계산이 적다. 규칙은 늘 최신 시야를 본다(4.11) |
| 10.7 맵 밖 칸 범위 | 맵 밖 칸은 시야를 막지 않음 | 맵 둘레 20칸까지만 계산(확장 격자), 그 밖은 "안 보임" | 반경 상한 480 + 64 보다 늘 멀어 결과가 같다(4.11) |
| 10.7 경계 흐림·벽 | 벽 그림자 경계는 칸 반 폭쯤 흐림, 그리는 방법은 설계 | 칸 값 텍스처의 이중선형 투영 + 벽 기둥은 자기 칸 값 + 그리는 대상은 자기 알파(최댓값, 값은 알파 채널 — v0.5.1) | 셰이더 없이 같은 모양. 화면 공간 가림만 쓰면 보이는 벽 윗부분이 검게 잘림(7.10, R28) |
| 10.7 두 줄 벽 (v0.5.1) | 칸의 중심까지 가는 선이 시야 막힘 칸을 지나지 않으면 보임(벽 칸 자신은 선이 닿으면 보임) | 걷기·시야를 둘 다 막는 칸은 같은 메가타일의 그런 칸 하나가 보이면 함께 보임(가 본 칸도) | 두 줄 벽(앞 턱·모서리·협로)의 바깥 줄은 선이 안쪽 줄을 지나 영원히 안 보여, 벽 하나가 밝은 반·검은 반으로 갈리고 미니맵에서 빠진다. 막힌 칸에는 적이 서지 않아 규칙(사격·돌진)은 바뀌지 않는다(4.11) `[해석]` |
| 10.7 바뀜 0.15s (v0.5.1) | 칸이 보이게·안 보이게 될 때 0.15s 에 걸쳐 바뀜 | 바뀌는 중에 다시 바뀌면 지금 값에서 되돌아감(`ChangedTick` 당김) | 경계에서 1px 씩 밀리며 매 틱 뒤집히는 칸이 0·1 로 깜빡이지 않게(4.11) |
| 10.7 또렷이 보임·8.3 `kite` (v0.5.1) | v0.6.1: 발과 몸 위 끝이 안쪽 16px 안, `kite` 띠 당기기(L = 그 방향 또렷이 보이는 가장 먼 거리, 벽 제외) | 몸 높이 = 그림 묶음 `body_height` × 최종 크기(`Enemy.BodyTop`), L = `VisibilityField.ClearReach(방향, BodyTop)` 를 틱마다 적 방향으로 한 번 | 기획 값 그대로 담음 — 차이 없음(이름·자리만 설계, 기획 24절 넘긴 것 1) |
| 13.1 안개 값 자리 | 스테이지마다 또는 모든 스테이지 같은 값이면 공통 규칙에 한 번 | 공통 `rules.sight` 한 곳(+ 시야 쪽 값 `refresh_ticks`·`view_inset`·`touch_margin`·`ranged_margin`), 키별 소유(시뮬레이션 값은 메인 세션 합의, v0.5.1) | 두 스테이지 값이 같다. 달라질 때 스테이지 `lighting.fog` 덮어쓰기를 더한다(6.10) |
| 10.7·12.6 닿은 적·자폭 (v0.5.1) | 주인공에 닿은 적은 늘 보임, 근접·자폭은 닿아야 맞음 | 닿은 거리는 적마다 `TouchReach`(최소 `touch_margin`, 근접 사거리 여분 + 10 까지), 자폭 점화 중 적은 늘 그림 | 근접 사거리가 긴 적이 1칸 벽 너머에서, 점화한 자폭 적이 벽 모서리 너머에서 숨은 채 때리지 않게(공정성 3·5) |
| 10.7 피해 숫자·표시 이펙트 | 피해 숫자는 시야 밖 적이면 숨김, 주인공 무기 이펙트는 늘 | 피해 숫자는 대상의 안개 알파, 표시 이펙트(`FxSystem`)는 적 쪽도 늘 그림(적 등장 연출·죽는 그림·시체만 숨김) | 적 쪽 이펙트는 위험(폭발·강타)이라 안개에 가리지 않는 쪽이 공정성(기획 10.7)에 맞다. `FxSystem` 이 이펙트 주인을 몰라도 된다(7.10) |
| 9.4·10.7·25절 `clear_sight` (v0.5.3) | 이 조건을 단 패턴과 그 적의 `kite` 는 등급과 상관없이 실제 시야 | `SeenClearly` 의 등급 예외를 통째로 없앰 — 시야 조건이 있는 일반 부품(`shoot_projectile`·`kite`·`charge`)은 어느 등급이든 실제 시야, 보스 패턴은 `clear_sight` 만 | 규칙 하나로 같은 결과. MVP 의 다른 중간 보스·보스는 이 부품을 쓰지 않는다. E 단계에 보스가 일반 원거리·돌진 부품을 쓰면 그것도 시야를 본다(기획 10.7 공정성과 같은 방향) `[해석]` |
| 4.5·13.1 시전 맞춤 자리 (v0.6) | 직업 정의에 시전 길이·발동 프레임 | 그 직업 그림 묶음의 시전 클립(`frames`·`event_frame`, 출처 주석 — 6.3) | 원본 유래 수치는 그림 묶음 클립 한 곳에 둔다(1.4·13.5-16). 규칙은 쓰지 않는 그림 맞춤 값이라 직업 정의의 칸이 되면 두 곳이 어긋날 수 있다 `[해석]` |
| 4.1·4.5 기본 능력치 자리 (v0.6) | `rules.hero` 에서 `stats` 를 직업 정의로 옮기고 `rules.hero` 는 공통 규칙과 고른 직업만 | `rules.hero.stats` = 모든 직업의 기본값(기획 4.1 표)으로 남기고, 직업 `stats` 는 덮어쓸 키만 | 기획 4.1 "다른 직업은 몇 키를 덮어쓴다" 와 같은 모양. 직업마다 키 열다섯 개를 다 적지 않아도 된다(마법사는 빈 객체) `[해석]` |
| 5.1·13.1 근접 시전 그림 `at` (v0.6) | `cast_clip: at` | `"cast_clip": "attack"`(`ClipKind.Attack`) | 클립 이름은 정의 전체가 `idle`·`walk`·`attack`… 으로 통일돼 있고 원작 글자(`at`)는 가져오기 표만 안다(6.3) |
| 19.4 `hfvoice` 묶음 (v0.6) | 묶음 넷, 정의의 `requires` 는 묶음 목록 | `hfvoice` 는 원본 우선순위(`prefer`)에만, `requires`·표 `bundle` 에 쓰면 오류 | hfvoice 가 없어도 8비트판이 같은 이름을 만들어 꺼지는 내용이 없다 — `requires` 에 쓰면 내용을 쓸데없이 끈다 `[해석]` |
| 13.5-5 갇힌 칸 (v0.6) | 반경 14 로 걸을 수 있는 칸인데 시작에서 닿지 않는 칸 | 걷기 막힘 없는 하위 칸에 시작에서 닿는 반경 14 몸이 걸칠 수 없으면 오류 | 하위 1칸 띠는 clearance 가 14 언저리라 "걸을 수 있는 칸" 기준이면 격자 표본에 따라 놓친다(6.14) `[해석]` |
| 19.4 음악 대체 (v0.6) | 음악에 `requires`·대체 곡 | `MusicDef.Requires`·`Fallback`, 스테이지 `requires` 에는 `hfmusic` 을 넣지 않음 | 음악은 없어도 스테이지를 끄지 않는다(기획 표 "대체 곡으로 돈다") — 꺼짐 단위(스테이지·직업)와 섞지 않게 |
| 10.1 방 기준점 (v0.6) | 맵에 방 기준점 `room_<이름>` | `anchors` 의 키 이름 규칙(`room_` 접두) — 새 칸 없음 | 기준점은 이미 `anchors` 이고 다른 묶음(행진 목표·빛)도 같은 표를 읽는다 |
| 2.5·28 방을 바꿀 때 월드 (v0.7) | `World` 를 방마다 새로 만들지 맵만 갈지 설계가 정함 | 방마다 새 `World` + `RunState`·`HeroCarry` 로 넘김, 조명은 넘기지 않고 지난 `light_change` 를 다시 적용 | 월드 수명 = 맵 수명 가정을 지킴(3.6 결정 1). `--room` 시작과 같은 함수로 조명이 맞는다 `[해석]` |
| 2.5 시간 효과 (v0.7) | 액트 사이에 "레벨·무기·패시브·축복·금화·재추첨" 이 이어짐(시간 효과는 정하지 않음) | 마력 폭주·버프·피격 무적은 방이 바뀌면 끝남 | 방 이동은 0.5s 어두워짐 뒤 들어옴·유예 3초라 남은 몇 초를 넘길 이득이 작고, 넘기면 짐 형이 버프 표까지 커진다 `[해석]` — v0.7.1: 기획 v0.8.1 이 2.5 "방 사이" 표로 정하는 중(리뷰 지적), 정해지면 이 줄을 닫는다(13.18 열린 질문 1). **v0.8 닫음**: 기획 v0.8.1 2.5 대로 시간 효과의 남은 틱을 짐(`HeroCarry.Effects`)으로 넘기고 생존·정리 단계에서만 흐르게 했다(3.6 결정 1 "시간 효과 시계") — 이제 두 문서가 같다. 피격 무적·무기 쿨다운은 방마다 새로(기획도 같음) |
| 10.8 나가는 자리 (v0.7) | "문 틈 하위 4칸 × 벽 안쪽 면에서 하위 2칸" | 그 문의 열린 메가타일 둘의 걷기 칸(`GateZones`) — 뒷벽 문·앞 턱 문 모두 메가 두께라 4 × 2 | 데이터 없이 배치에서 바로 나오고 문 틈 그 자체다. 주인공이 닿는지는 v0.7.1 의 13.5-23 ③ 문 틈 닿음 검사가 본다(13.5-5 는 진입점까지만) `[해석]` |
| 14.1 단계 1 의 방 순번 (v0.7 → v0.7.1) | 순번 1·2·3·4·6·8 만 돌고 t 범위는 표 그대로 | 스테이지 `slice: true` 로 순번 표 여섯 줄(원래 t 범위), 13.5-23 의 빈틈·방 수·보스 방 규칙만 끔. v0.7.1: 각 줄에 `number`(기획 번호) — HUD `6 / 8`·로그·`--room`·씨앗·깊이가 기획 번호 | v0.7 은 번호 칸을 두지 않고 배열 순서로 보였으나, 같은 `--room 1:6` 이 단계마다 다른 방이 되고 씨앗이 단계 2 에서 밀려 리뷰 지적으로 바꿨다(13.18). 이제 `[해석]` 아님 |
| 2.5 정예 방의 t (v0.7.1) | "정예 방의 t 범위는 이름뿐인 1:00, 오래 못 잡으면 t 는 끝에서 멈춘다" | t 는 `to` − 1틱에서 멈춤 | 구간을 `[from, to)` 로 찾으므로 `to` 에서 멈추면 다음 방 구간(센 생성·새 종)이 정예 방에 나온다. 기획 v0.8.1 이 같은 뜻으로 고침 — v0.7.2: 기획 v0.8.1 2.5 대로 일반 방(정리 단계)도 같게 |
| 15 자동 조작 문 동률 (v0.7.1) | 같으면 화면 왼쪽 문 | 그다음 화면 y 가 작은 문, 그다음 `Exits` 순서 | 출구 셋일 때 `gate_nw`·`gate_sw` 는 마름모 대칭이라 화면 x 가 같다 — 구현마다 봇 판이 달라지지 않게. 기획 v0.8.1 15절도 같게 |
| 11.4 0장 보상 (v0.7.1) | 보상 거르기는 문을 열 때 | 줍는 순간 카드 0장이면 그 보상의 `skip` 으로 처리, 보상 물체는 상태와 상관없이 늘 집힘 | 문을 열고 집기까지 한 방이 지나 상태가 바뀐다. 0장 창·"가득이면 안 집힘" 은 문이 영영 안 열리는 길이다. 기획 v0.8.1 11.4 도 같게. v0.8: 대체는 `skip` 이 아니라 `단련`(기획 v0.8.1 11.4·26) — 두 문서가 같다 |
| 15 `--room` 시작 방의 보상 (v0.7) | 앞 방은 건너뛰고 보상도 없음 | 시작 방의 보상 = 앞 순번 보상 후보 순서의 첫 칸(상태 거르기 없음) | 방 흐름이 늘 보상 단계를 지나야 문이 열려(3.6) "보상 없음" 이 별도 경로가 된다. 정해진 값이라 재현된다 `[해석]` |
| 15 `DOOR_SKIP` (v0.7) | 문 단계가 20초 안에 안 끝나면 "그 문으로 옮긴다" | 정책이 `CommandButtons.Exit` + `ExitIndex` 명령(v0.7.1 — 기본값이 "안 나감" 인 깃발)을 내고 `RoomFlow` 가 받아 나감 | 정책이 시뮬레이션 상태를 직접 바꾸지 않는다(명령 패턴, 2.1) — 사람과 봇이 같은 길, 결정적 |
| 10.8·13.5-23 작은 방 검사 상수 (v0.7) | 작은 방 N ≤ 36, 문 진입점 150px 안 막힌 칸 없음 | Core `RoomMapRules` 상수(데이터 아님), 150px 는 **둘레가 아닌** 막힌 칸(13.5-6 의 60px 와 같은 대상) | 맵 검사는 `rules` 를 모른다(6.2 — 도구도 같은 검사). 둘레 벽은 진입점에서 약 100px 라 넣으면 모든 맵이 실패한다 `[해석]` |
| 13.4 이벤트 시각 자리 (v0.7) | 이벤트는 시각 + 종류 + 매개변수 | 로더가 `at` 을 꺼내 `PartDef.At` 에 | 방 이벤트 옮기기·검사가 매개변수 형(WP4 소유)을 몰라도 시각을 읽는다 |
| 11.4 정예 상자 보상 (v0.7) | `reward_elite` 를 문 보상 표의 한 줄로 | 정의 없음 — 정예·보스 방은 보상 없음 + 떨군 정예 상자를 `GatesExit` 로 묶음 | 상자는 이미 드롭 표가 낸다. 같은 일을 보상 정의와 드롭 두 곳에서 정하지 않게 `[해석]`. v0.8: 정예·보스 방에도 덤 보상(`RoomState.Reward` — 들어온 문의 보상)이 더해진다. 상자는 여전히 정의 없음 |
| 8.1·10.7 "또렷이 보일 때만 쏜다" (v0.5.3) | 쏘는 조건(시점은 적지 않음) | 시작 조건 + **발사 프레임에 다시 봄**, 그때 안 보이면 그 발사만 버림(클립은 끝까지, 쿨은 씀). `shoot_projectile`·`clear_sight` 패턴 둘 다 | 시작에서 발사까지(약 0.5s) 주인공이 움직여 안 보이게 되면 공정성 `unseen_shots`(투사체가 생기는 순간을 셈)가 0 이 안 된다(6.6·10.1) `[해석]` |
| 11.4 보상 자리 (v0.8) | 주인공이 바라보는 쪽 100px 에서 가장 가까운 걸을 수 있는 칸(주인공과 길로 이어진 칸 — 경계·장애물에서 30px 이상), 여럿이면 40px 간격 | 방향 후보 8개(바라보는 쪽 → ±45° … 180°)마다 주인공 발에서 직선으로 8px 씩 걸어 clearance ≥ 30 인 동안만, 100px 까지 간 첫 방향의 끝(3.6 "보상 자리"). 여럿은 바라보는 방향의 수직 줄 | "길로 이어진 가장 가까운 칸" 을 찾으려면 길찾기(WP2)가 필요한데 그것을 기다리지 않고, 직선이 끊기지 않으면 이어짐이 보장된다. 결과는 같은 자리·같은 방향이면 같다(난수 없음) `[해석]` |
| 10.8 거울판의 맵 이름·바닥 (v0.8) | 맵마다 거울판을 쓸 수 있음, 고르기는 (맵, 방향) | 거울판 = 따로 만든 `MapDef`(`<id>@m`, 정의 표 밖), 로그는 `map=<원판> mirror=1`, 바닥 섞기는 거울판 ID 로(무늬가 원판과 다름 — 성질은 같음) | ID 로 정의를 찾는 코드가 원판을 조용히 받지 않게. 바닥 무늬까지 뒤집으려면 배치 풀기에 거울판 분기가 생긴다 `[해석]` |
| 19.6 확대한 묶음의 그림자 (v0.8, 사용자 결정 U) | 그림자는 AI 에 넣지 않고 원본 모양을 매끈하게 늘린 판을 몸 아래에 따로 — 도구가 미리 굽을지 게임이 그릴지는 설계 | 가져오기 도구가 마스크를 매끈하게 2배(결정적) 해 몸 아래에 같은 시트로 굽는다(1.4 ⑥) — 게임은 그림자를 따로 그리지 않음 | 게임 쪽 그리기 경로·그리기 호출·텍스처가 늘지 않고, 확대 안 한 묶음과 같은 길로 그린다. 도구 쪽은 결정적이라 GPU 와 무관 |
| 13.5-23 방 이벤트 자리의 종류 (v0.8) | 강적(정예 무리·해골 대장)·포위 고리, 절정 = 이벤트 하나 | 강적 = `elite_pack`·`escort`·`ring`, 절정 = `march`·`ring`·`elite_pack`·`escort`(`banner`·`light_change`·`room_supply`·입장 이벤트는 아님) | 검사가 이벤트 종류 이름만으로 판정해야 한다(매개변수 형은 WP4) `[해석]` |
| 2.5 버린 계단 저장 (v0.8) | 새 게임을 시작하면 남은 저장을 지운다고 묻는다(기록은 정하지 않음) | 버린 판은 기록하지 않는다 | 판이 끝나지 않았으므로 결과(깊이·승리·시간)가 없다. 기획이 "버린 판도 깊이를 기록" 으로 정하면 `DiscardRun` 이 `RecordResult` 를 부르게 바꾼다(13.20 열린 질문) `[해석]` |

---

## 13. 리뷰·변경 반영

13.1~13.3 은 v0.2 의 세 리뷰 지적마다 처리를 한 줄로 적은 것이다(높음·중간은 모두 반영, 일부만 반영하거나 보류한 것은 이유). 당시 이름(`bone_brute`, `kind_lights` 등)은 기록으로 그대로 둔다.
13.4 는 v0.3 변경(기획서 v0.4·WP1 구현)의 처리다. 13.5 는 v0.3 리뷰(구현·검증·기획 담당 합본)의 처리다.
13.6 은 기획서 v0.5 와 맞춘 것, 13.7 은 WP1b 구현 리뷰, 13.8 은 기획서 v0.6(전장의 안개)과 사용자 결정의 처리다. 13.9 는 그 안개 설계의 리뷰 처리다.
13.10 은 WP1c 구현, 13.11 은 기획서 v0.6.2(중간 보스), 13.12 는 기획서 v0.7(헬파이어·직업·안쪽 방)의 처리다.
13.13~13.16 은 v0.6 리뷰·WP1d 구현·검증·WP0a 구현, 13.17 은 기획서 v0.8(사용자 결정 R — 방 구조)의 처리, 13.18 은 그 방 구조 설계(v0.7)의 리뷰 처리다. 13.19 는 WP1e 구현, 13.20 은 기획서 v0.8.1(29절)과 사용자 결정 U(AI 확대)의 처리, 13.21 은 그 설계(v0.8)의 리뷰 처리다. 13.x 의 옛 판 기록은 당시 이름(`StageSelect`·`unlock`·`victory`·"10분" 등) 그대로 둔다.

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

### 13.8 v0.5 — 기획서 v0.6(전장의 안개)과 사용자 결정 반영

사용자 결정 세 가지(기획 23절 A·B·C)를 받았다. A = 원작식 시야(사용자 요구 11), B = DevilutionX 소스를 보고 안 사실 수치를 출처 주석과 함께 손으로 적어도 됨, C = 함성 버프는 보스 본체에 안 걸림.
기획 23절 "설계에 넘기는 것" 의 처리:

| # | 넘긴 것 | 처리 | 자리 |
|---|---|---|---|
| 1 | Core 시야와 기억, 질의 "보임"·"또렷이 보임", `HasClearLine` 과 이름 나누기 | `VisibilityField`(하위 타일 칸 격자 + 맵 둘레 20칸, 칸 중심 정수 선분 supercover, 꼭짓점 규칙, 칸·반경이 바뀐 틱에 다시 계산, 가 본 칸 6틱), `IsVisible`·`IsClearlyVisible`, `BeginTick` 안. 4.5 의 선분 검사는 **길 트임**으로 부름(함수 이름 그대로), "시야" 는 안개 시야만 | 0·4.5·4.11·3.5 |
| 2 | 성질 표의 시야 막힘 표기·가져오기 대조·보정·`LayoutHash` | 8글자(대문자 = 시야 막힘, `sol & 7` 과 1:1), 대조 세 비트, 보정은 같은 원소(아치 `.` → `M`), 해시는 글자를 이어 저절로 포함. 기둥 `X` → `x` | 1.4·4.2·6.11·6.14·A.4 |
| 3 | 적 규칙(`shoot_projectile`·`kite`·`charge`)과 생성 후보 | `EnemyCtx.SeenClearly`(보스·중간 보스는 참), 세 부품의 시작 조건. 감독은 보이는 후보를 버리고 12번에 같이 셈(이벤트·보스·소환 제외) | 6.5·6.6·6.12·6.13 |
| 4 | 렌더(세 상태·흐림·숨김·늘 그림·폭발 빛·층·`unit_min_light`) | `FogRenderer`: 칸 값 텍스처 투영, `FogRT`·`MemRT`, `LightRT` = V·L + (1 − V)·E·M, 벽·대상 모양 최댓값, `FogMath` 알파로 숨김, 6층 "빛·안개", 하한은 그리는 대상에만 | 7.3·7.7·7.10 |
| 5 | 미니맵 | 구운 그림을 CPU 로 가 본 칸만 남겨 다시 올림(`ExploredVersion`), 점 규칙 | 7.6·8.2 |
| 6 | 안개 값 자리·검증 15 | 공통 `rules/sight.json`(일곱 값, 코드 기본값 없음), 13.5-15 = 빛 반경 ≤ 480 이고 ≥ `kite` 띠 최대 + 60 | 6.10·6.12·6.14 |
| 7 | `--reveal`·디버그 보기·봇·시험 | `--reveal`(`AppOptions` — Core 가 모름), `--debug-nav sight`, `AutoplayPolicy` 는 시야를 읽지 않음(소스 검사), 시험 `Visibility`·`Enemies`·`Director`·`Sim`, 실행 확인 "안개"·"안개 재현" | 9·10.1·10.2 |
| 8 | 결정 B·C | B: R24·1.4 "원본에서 온 수치" 에 사용자 확인을 적고 v0.4.1 의 "안 되면 도구가 추정" 대체 길을 없앰. C: `include_bosses: false` 확정(스틸메이스 자신 포함), 12.1 표 차이 없음 | 1.4·6.5·12·12.1 |
| — | 소리 | 화면 기준 그대로, 안개 속 적 소리도 냄(`AudioSystem` 은 시야를 읽지 않음) | 7.9 |
| — | 작업 묶음 | **WP1c**(시야 계약 + `VisibilityField`·`FogMath` 구현·시험, WP0a 앞). 다음 계약 갱신은 WP1d. 렌더는 WP5, 미니맵은 WP6, 파싱·해시는 WP7, 세 비트 대조는 WP0 | 11·11.1·11.2·11.3 |

13.6 의 남은 열린 질문 1(함성 대상)·2(원본 유래 수치)는 결정 C·B 로 닫혔다. 13.5 의 열린 질문 6 도 같다(기록은 그대로 둔다).

**남은 열린 질문**
1. 안개 값(`edge` 40·`memory_ratio` 0.5·`fade` 0.15)과 빛 반경 R — 미리보기·10.2 "안개" 캡처로 고른다(기획 열린 질문 20). 양옆 압박이 거칠면 R 을 올린다(상한 480, 13.5-15 가 하한을 지킴). 값은 `rules/sight.json`·스테이지 조명 두 곳만 고친다.
2. L2 조각, 특히 스테이지 2 협로 벽의 시야 비트(기획 열린 질문 15) — WP0 단계 3 이 원본으로 확인한다.
3. 기둥이 시야를 막게 할지(기획 열린 질문 22) — 바꾸면 `design_diffs` 에 기둥 칸 `x` → `X` 한 원소(6.11). L1 뒷벽 1·2·모서리·문 끝 조각·아치 보정 칸의 시야 비트는 WP0a 가 원본으로 확인한다.
4. 무기가 시야 밖·벽 너머 적을 조준할지(기획 열린 질문 23) — 바꾸면 WP3 조준 후보에 `IsVisible` 하나(4.11).
5. (설계) 7.10 의 화면 공간 합성(벽·대상 모양 최댓값)이 캡처에서 맞는지 — WP5 첫 캡처로 본다. 어긋나면 R28 의 대체 길.
6. (설계) 꼭짓점 규칙(12.1 `[해석]`)을 기획이 받는지 — 원작의 시야 계산과 경계 칸 몇 개가 다를 수 있다(원작 방식은 확인하지 않았다). 기획 확인을 기다린다.

### 13.9 v0.5.1 — 안개 설계 리뷰 반영 (`docs/reviews/architecture-v05-fog-review.md`)

구현(I)·검증(V)·기획(P) 담당 지적 30개. 높음·중간은 모두 반영했고, 낮음도 모두 반영했다(일부는 다른 지적과 합침). 기획 쪽 고침이 필요한 "둘 다" 지적은 이 문서 쪽만 고쳤다 — 기획서는 기획 담당이 같은 지적으로 고친다.
지적 원문과 줄별 처리는 리뷰 파일에 있다. 여기는 요약이다.

| 지적 | 처리 | 내용 | 자리 |
|---|---|---|---|
| 높음 I1 회색 최댓값 혼합이 SpriteBatch 로는 색 × g | 반영 | 값을 **알파 채널**에(틴트 (255,255,255,g) → 그림 알파 × g), 혼합 상태 표 `FogBlend`, `MemRT` 흰색 + 알파, 합치기 `MulRgbBySrcA`·`AddRgbBySrcA`, 최소 밝기 = 보조 RT 에 max(L, m) → 모양 알파 → `LerpRgbBySrcA`. 10.2 고정 장면의 `_light.png` 값 대조(≥ m, 벽 = `--reveal` 값) | 7.7·7.10·9·10.2·R28 |
| 중간 I2·V1 `Enabled` 거짓이면 생성 후보가 모두 버려짐 | 반영(규칙을 뺌) | 기획 리뷰 P3 의 (가)를 따라(기획 v0.6.1 도 뺌) "보이는 생성 후보 버림" 을 뺐다 — 뒤집힘이 생길 자리가 없어짐. 비슷한 규칙을 다시 넣으면 `HiddenForSpawn(pos) = !Enabled \|\| !IsVisible(pos)` 꼴(꺼진 월드 = 아무것도 안 버림). V1 의 13.5-15 하한(반경 > 0)은 P5 와 합쳐 380 | 6.12·4.11·6.14 |
| 중간 I3 두꺼운 벽 바깥 줄이 영원히 안 보임 | 반영 | `W`·`X` 칸은 같은 메가타일의 `W`·`X` 칸 하나가 보이면 함께 보임·가 본 칸(한 번만, `L`·`M` 제외). 12.1 `[해석]`, 10.1 두 줄 벽 시험, 10.2 장면 ⑤. 남는 겹벽 경우는 "지우기 + 더하기" 덮어쓰기 길(7.10) | 4.11·7.10·10.1·10.2·12.1 |
| 중간 I4 0.15s 바뀜 중 다시 바뀌면 값이 처음부터 | 반영 | `ChangedTick ← t − (F − a)`(a < F 일 때), 10.1 "9틱 안 왕복 시 값 연속" | 4.11·7.10·10.1 |
| 중간 I5·V10 `W`·`M` 뜻이 바뀌어도 오류가 안 남 | 반영 | 6.11 "주의", WP1c #8(`W`·`M` 조각 따로)·#9(`CellFlags` 로 확인, 8글자 왕복), 완료 기준에 `rg` 점검 결과, WP0a 는 `sol` 값으로 받아 바꿔 적음 | 6.11·WP1c·WP0a |
| 중간 I6 `rules/sight.json` 에 소유가 다른 값이 섞임 | 반영 | 키별 소유: 시뮬레이션 값(`edge`·`view_inset`·`refresh_ticks`·`ranged_margin`)은 메인 세션이 WP4 와 합의, 그림 값(`memory_ratio`·`fade`·`touch_margin`)은 WP5. 바꾸면 WP4 `Scenario`·13.5-15 다시. 키는 나누지 않음 | 6.10·11.3·WP4·WP5 |
| 낮음 I7 `FogMath` 소유가 WP2 | 반영 | `FogMath.cs` = WP5 소유(시그니처는 계약), `VisibilityField` 만 WP2 | 1.1·4.11·WP2·WP5·11.3 |
| 낮음 I8 생성자에서 첫 계산 → 옮긴 시작 자리 둘레가 가 본 칸으로 남음 | 반영 | 첫 계산은 첫 `BeginTick`. 그 전 프레임은 안개 검정 | 4.11·WP1c #5·#6·10.1 |
| 낮음 I9 `FogRT` 4단계가 유닛을 다시 다 냄, 안개 켠 스트레스가 최악이 아님 | 반영 | 4단계 대상을 흐림 띠·`CellLevel < 1` 에 걸친 것 + 늘 보이는 것으로 줄임, 렌더 기준은 `--reveal` + 안개 켬 두 번 재 최악 추정(`fog_ms` 열) | 7.10·10.3·WP5 |
| 낮음 I10 빛 원과 안개 원이 1틱 어긋남 | 반영 | `FogMath` 그리기용 오버로드(보간된 주인공·그리는 위치), 띠도 보간 위치 | 4.11·7.10 |
| 중간 V2 시야 조건 시험이 다른 조건에 먼저 막혀 통과 | 반영 | 벽 뒤는 `L` 줄, R − 40 밖은 반경을 낮춘 시험 월드, 화면 위는 몸 위 끝 경우, 각 시험에 시야 끈 대조군 | 10.1 `Enemies` |
| 중간 V3 피해 전체를 보는 공정성 계측이 없음 | 반영 | `World.Fairness` + `InvariantChecker` 열 `unseen_shots`·`unseen_charges`·`hidden_hits`(기준 0), 공정성 합성 맵 3분 `Scenario`, 기존 10분·길찾기 실행에도 | 10.1·11.2·WP1c #5·WP3·WP4 |
| 중간 V4 자폭 예고가 안개에 숨음, 근접 사거리 긴 적 | 반영 | 자폭 점화 중 `Visual.ForceShown`, 닿은 거리 = 적마다 `TouchReach`(근접 여분 + 10 까지). 합성 맵 `L` 모서리 + 자폭으로 `hidden_hits` 확인 | 4.11·6.6·7.10·10.1·12.1 |
| 중간 V5 R29 떨림 시험이 없음 | 반영 | 정위 궁수 전환 수 시험, `kite_switches` 열, `kite` 띠 당기기(띠 폭 ≥ 40 — P1 과 합침) | 6.6·10.1·R29 |
| 중간 V6 10.2 안개 판정 항목이 그 시각에 화면에 있다는 보장이 없음 | 반영 | `--preview fog` 고정 장면 여섯 개(자리·적 고정, 앱 k 초 = 장면 k — 스테이지 시각과 무관), 캡처마다 `FOG` 줄, 미니맵은 WP6 행으로. 150초 행진 캡처는 장면 ② 로 바꿈 | 9·10.2·WP5·WP6·WP7 |
| 낮음 V7 해시에 가 본 칸 개수만 | 반영 | `ExploredHash`(비트맵 `StableHash`), `InSight` 를 넣지 않는 이유 | 3.4·4.11·WP7 |
| 낮음 V8 반경 바뀜이 "그 틱부터" 가 아님 | 반영 | "다음 틱부터", 그리기도 `Visibility.Radius`, 10.1 기대값 | 4.11·6.12·7.10·10.1 |
| 낮음 V9 `Perf` 가 `dotnet test` 로는 안 돎 | 반영 | WP1c 완료 기준에 `DDR_RUN=Perf dotnet test --filter Category=Perf` 와 잰 값 기록 | WP1c |
| 낮음 V11 출처 주석을 시험이 못 봄 | 반영 | 원문 문자열 시험 `Data/SourceCommentTests`(기본 분류, WP0a) | 6.14·WP0a |
| 낮음 V12 결정 C 를 기계적으로 보는 시험이 없음 | 반영 | 중간 보스 `buff_area` 가 자신·다른 보스에 안 걸림, 오버로드 함성 + 도살자 35 그대로, `include_bosses: true` 경고 | 6.14·10.1·WP4 |
| 낮음 V13 `--start-at` 이 지난 `light_change` 를 건너뜀 | 반영 | 상태형 이벤트는 차례로 적용한 뒤 시작, 10.1 `Director` | 9·10.1 |
| 중간 P1 또렷이 보임이 발만 봄·정위 14px 창 | 반영(설계 쪽, 기획 v0.6.1 값) | `IsClearlyVisible(pos, top)`(발과 몸 위 끝), 그림 묶음 `body_height` → `Enemy.BodyTop`, `kite` 띠 당기기(`ClearReach` 로 L, 최대' = min(최대, L − 10), 최소' = min(최소, 최대' − 40)). 13.5-15 의 "띠 최소 ≤ 위아래 한계" 검사는 규칙 변경으로 필요 없음 | 4.11·6.3·6.6·10.1·12.1·WP1c #11 |
| 중간 P2 최소 밝기가 흐림 띠 적 자리 바닥을 m 으로 올림 | 반영 | 모양 알파 = 안개 알파 × 그림 알파(I1 의 새 방식에서 max(L, m)·a + L·(1 − a)), 10.2 "밝은 바닥 실루엣 없음" | 7.7·10.2 |
| 낮음 P3 보이는 생성 후보 버림이 화면에 차이를 만들지 않음 | 반영 (가) | 규칙을 뺌(6.12), 10.1 `Director`·WP4·3.5·0절·4.11 "쓰는 곳"·9절에서 지움. 13.8 표의 기록은 그대로 | 6.12 외 |
| 낮음 P4 `--reveal` 이 저장 보호에 안 듦 | 반영 | 저장 보호 판정에 `--reveal`·`--god`·`--level`·`--weapons`·`--passives` | 9·10.1·WP7 |
| 낮음 P5 13.5-15 에 하한이 없음 | 반영 | 380 ≤ R(원거리 적 없는 스테이지에도), 0 이하도 같은 오류 → `Enabled` 거짓은 맵 없는 시험 월드뿐 | 4.11·6.12·6.14 |
| 낮음 P6 "7층은 안개에 가리지 않는다" 문구 | 반영 | 7층 중 적 투사체·예고 테두리·주인공 마법·폭발은 늘, 적 발광·번쩍임·보석 빛은 대상의 안개 알파 | 7.3 |
| 낮음 P7 DUN 근거가 "사용 조건 확인 전" | 반영 | 사용자 결정(기획 23절 B) — DUN 파일 자체는 옮기지 않음 | 2.2 |

**남은 열린 질문**(13.8 의 1~5 는 그대로 열려 있다. 6 꼭짓점 규칙은 기획 v0.6.1 이 받아 닫혔다 — 기획 24절 "설계 v0.5 의 `[해석]` 받기". 아래는 이번에 생긴 것)
7. 그림 묶음마다 `body_height`(대략 60~90px) 값 — WP0a 가 어림으로 넣고 WP5 가 `--preview enemies`·`--debug-nav sight` 로 맞춘다(기획 열린 질문 7, v0.6.1 추가분).
8. (설계) 겹벽(숨은 벽이 보이는 벽 앞으로 솟음)이 스테이지 2 협로에서 생기는지 — 단계 3 캡처로 보고, 생기면 벽 항목 덮어쓰기(7.10 "남는 한계").

기획서 v0.6.1(같은 리뷰를 기획 쪽에서 반영 — `docs/reviews/gdd-v06-fog-review.md`)과 맞춘 것: 또렷이 보임의 몸 높이(그림 묶음 값)와 `kite` 띠 당기기(기획 값 그대로 — 설계가 처음 적은 "경계 안쪽 10px 에서 물러남 멈춤" 은 기획이 띠 폭 40 을 붙여 정한 규칙으로 바꿈),
닿는 거리 = 두 반경 + max(16, 근접 사거리 여유 + 10), 점화 중 자폭 적 늘 보임, 스테이지 2 빛 반경 400, 13.5-15 하한 380·빛 없음 오류, 13.5-16(출처 주석)·13.5-17(`include_bosses: true` 경고) 번호, 일반 생성의 시야 조건 뺌(기획도 (가)), 저장 보호 다섯 옵션.

### 13.10 v0.5.2 — WP1c 구현이 정한 것

WP1c(커밋 전) 코드가 문서와 다르게 또는 더 정한 것. 코드가 정본이다(11.4 와 같은 규칙).

| 자리 | 문서 | 코드 | 이유 |
|---|---|---|---|
| 4.11 `VisibilityField`·`FogMath` namespace | 폴더 `Sim/Visibility/` | 폴더는 그대로, namespace 는 `Core.Sim`(`Sim/Entities/` 와 같은 방식) | 모든 묶음이 쓰는 공용 형이라 전역 using 하나로 보이게. `Core.Sim.Visibility` 로 두면 `World.Visibility` 속성과 이름이 겹친다 |
| 4.11 `Rules`·더한 멤버 | `SightRulesDef Rules` | `SightRulesDef? Rules`(없으면 `Enabled` 거짓), `ViewRect`(틱 시작 화면 사각형)·`CellOf(pos)`·`Size`, 시험용 `internal` `RecomputeCount`·`LineClear` | 규칙이 없는 시험 월드도 늘 `Visibility` 를 가짐. 그리기·디버그가 같은 칸 변환·사각형을 쓰게 |
| 4.11 경계 처리 | 정하지 않음 | "또렷이 보임" 의 안쪽 사각형은 경계 포함(`ClearReach` 끝 자리도 또렷이 보임), `ClearReach` 는 0 이상·방향 0 이면 R − edge, `EnemyAlpha` 틱 값은 `Visibility.HeroPos`·두 반경은 `Enemy.Radius + Hero.Radius`, `ExploredHash` 는 두 사슬로 64비트 | 시험이 경계 값을 그대로 확인(10.1) |
| 4.11 계산 범위 안 선 걷기 | 칸마다 선 걷기 | 주인공 칸과 그 칸을 잇는 상자에 시야 막힘 칸이 없으면 걷지 않음(누적합, 결과 같음 — 기준 구현 비교 시험) | 다시 계산 1회 Debug 0.09ms → 0.05ms(`Perf`, N 40·R 420·벽 줄) |

### 13.11 v0.5.3 — 기획서 v0.6.2(중간 보스 늘리기) 반영

사용자 요구 "중간 보스 늘리자"(메인 세션 결정: 스테이지 1 은 2명, 스테이지 2 는 3명, 2~3분마다 강적 하나). 기획 25절 "설계에 넘기는 것" 의 처리:

| # | 넘긴 것 | 처리 | 자리 |
|---|---|---|---|
| 1 | `clear_sight` 패턴과 그 적의 `kite` 는 실제 시야, `unseen_shots` 가 이 패턴을 셈 | 계약 `PatternDef.ClearSight`. `SeenClearly` 의 등급 예외를 없앰(12.1 `[해석]`). 발사 프레임 재확인(`shoot_projectile` 포함, 12.1 `[해석]`). 계약 `EnemyProjectileSpawn.SightGated` 로 셈 | 6.5·6.6·10.1·WP1d |
| 2 | 중간 보스에 `behaviors` + `patterns` | 정의 형이 이미 둘을 가짐 — 슬롯 블랙애시 5·데스스핏 3·슬러지 남작 3(≤ 12). `kite` 다가감은 `movement: astar`. 클립 없는 장판은 `trigger: cooldown`(즉시 동작으로 정함) | 6.5·6.6 |
| 3 | 중간 보스 죽음 순서 | `DeathHooks` 분열·`EnemyKilled`·히트 스톱(거는 곳을 이번에 정함) → `DeathRewards` 드롭. `Collapse` 는 분열 없음 | 6.6 |
| 4 | 보스 막대 여러 줄 | `BossBars` 등장 순서 목록(형 그대로), HUD 그리기 규칙, 최대 4줄 | 8.2·WP6 |
| 5 | 데이터 | 적 정의 셋(6.5 예), 이벤트(6.12), 해골왕 `hp` 9000(6.5 예), 가져오기 변종 셋·`special` 동작·소리 | 6.5·6.12·6.13·6.15·WP0·WP5·WP8 |
| 6 | 검증 13.5-18, 13.5-15 에 블랙애시 | 13.5-18 = WP4 검사기(경고 셋). 13.5-15 는 등급과 상관없이 `kite` 를 봄(380 ≤ 420) | 6.14·10.1 |
| 7 | 6.12·6.13 의 중간 보스 언급 | 6.12 예에 6:00 우두머리 무리·7:30 블랙애시, 6.13 표에 세 행 | 6.12·6.13 |
| 8 | WP4 범위 | WP4 v0.5.3 줄·완료 기준, 10.1 `Enemies`·`Director`, 10.2 "중간 보스 겹침" | 10.1·10.2·11.1 |

**원본으로 확인한 것**(조사 자료 `scan.tsv`, 읽기만): 고유 TRN `monsters\monsters\bashtb.trn`·`bfds.trn`·`bsm.trn` 이 diabdat 에 있음(256바이트).
쓰는 클립의 프레임 수가 기획 표와 같음 — `sklbwa`·`sklbws` 16, `acida`·`acids` 12, `fata` 15·`fats` 10. 원작 종(불타는 해골 궁수·산성 짐승·머드맨)과 층(4·6·8, 출처: DevilutionX 참고)도 기획 9.4 와 같다.

**기획서에서 고친 것**(기획 25절 끝에도 적음): 12.6 의 "슬러지 남작 진흙 강타(반경 26)" → "(반경 75, 피해 26)". 그 밖의 패턴은 예고 기준(13.5-13)·상한 360·13.5-15·슬롯 12 를 지키고, 없는 부품이 필요한 패턴은 없다.

**남은 열린 질문**
1. 데스스핏 산성 자국은 `pj_acid` 웅덩이와 같은 상한 무리(`acid`, 20)를 쓴다. 산성 분출 10발 + 산성 짐승이 겹치면 오래된 자국부터 지워진다 — 첫 플레이에서 자국이 일찍 사라져 보이면 자국만 따로 무리를 둔다(데이터 한 줄).
2. 발사 프레임 재확인으로 버려지는 발사 수 — `kite_switches` 처럼 로그 열로 볼지는 WP4 첫 `Scenario` 결과를 보고 정한다(많으면 화면 위아래 궁수가 헛손질처럼 보일 수 있다).
3. 기획 열린 질문 16·18(새 고유 TRN 색, ×1.5 크기)·24(상자 수와 보스 HP)·25(원거리·장판 중간 보스의 손맛)는 첫 플레이·`--preview enemies` 로 본다.

### 13.12 v0.6 — 기획서 v0.7(헬파이어·직업·안쪽 방) 반영

사용자 결정 H(헬파이어 자원 정식)·K(직업 4개)·M(넓은 방 + 안쪽 방), brief 요구 12~14. 기획 26절 "설계에 넘기는 것" 의 처리. 계약 1~3 은 기획 권고대로 아직 구현 전인 **WP1d 에 합쳤다**(11.1 WP1d #4~#17).

| # | 넘긴 것 | 처리 | 자리 |
|---|---|---|---|
| 1 | 직업 정의 `HeroClassDef`, 직업마다 파일, `rules.hero` 는 공통 규칙 + 고른 직업 ID, 주인공 코드는 직업 정의만 읽음 | `HeroClassDef`·`HeroTraitDef`, 종류 `hero_classes`, `Data/heroes/<id>.json`, `HeroRulesDef.Class`, `World.HeroClass`(생성자 인자). 기본 능력치는 `rules.hero.stats` 에 남기고 직업은 덮어쓰기(12.1 `[해석]`), 시전 길이·발동 프레임은 그림 묶음 클립(12.1 `[해석]`) | 6.10·6.16·6.3·WP1d #9~#12 |
| 2 | 무기 `cast_clip` 에 `at`, `classes`, 주인공 소리 키 = 직업 ID | `"cast_clip": "attack"`(12.1), `WeaponDef.Classes`, `SoundBindingsDef.Hero` 를 직업 ID 사전으로. 레벨업 후보·`--weapons` 가 `classes` 로 거름 | 5.3·6.8·6.15·WP1d #5 |
| 3 | 자원 묶음: 정의 `requires`, 음악 대체 곡, 표 `optional` → `bundle`, manifest 의 만든·건너뛴 묶음, 시작 검사 "꺼짐", 저장 키 = 스테이지 × 직업 | `ContentBundle`, 적·스테이지·직업·무기·음악 `Requires`, `MusicDef.Fallback`, 표 `Bundle`·`Prefer`·`OutputBundles()`, manifest `SkippedBundles`·파일 `Bundle`·`Source`, `ContentAvailability`(꺼짐 단위 = 스테이지·직업), 시작 검사 ③ 의 꺼짐, 저장 `records[스테이지][직업]`·`last_hero` | 1.4·6.15·6.17·8.3·WP1d #4~#8 |
| 4 | 가져오기 도구: `--hellfire` 기본 = diabdat 폴더, 없는 MPQ 건너뛰기, 헬파이어 경로 `nlevels\…`, 큰 칸 폭, 프레임 수는 파일에서 | 1.4 "헬파이어 묶음"(찾는 순서 `prefer` → diabdat → 묶음 MPQ, 있는 MPQ 의 빠진 파일은 실패, `--skip-bundles` 검증용), 칸 폭 가정 금지 문장, 종료 코드·끝 보고·캐시 | 1.4·WP0 |
| 5 | 배치 데이터(WP0a): 스테이지 1 안쪽 방, 층별 타일 조각 이름, `open_center`, 방 기준점, 창살 성질 | 배치 놓기 `grid`·`origin`·`over_border`·`id`·`decor_why`, `MapDef.OpenCenter`, 방 기준점 = `anchors` 의 `room_*`(12.1), `TilesetDef.Marks`, 창살 = 기존 `w` 글자(성질 표가 이미 세 비트). 층별 이름 = 층마다 타일셋 파일 | 6.11·WP0a·WP1d #14·#15 |
| 6 | 길 검사: 갇힌 칸 = 오류, 큰 적 → 방 기준점, 갇힌 띠 합성 시험 | 13.5-5 "갇힌 바닥 칸"(주인공 몸이 걸칠 수 있는가 — 12.1), 26·34 → 모든 `room_*`, 13.5-6 진입점 60px, 합성 방 맵 셋(WP1d) → WP2 시험 | 6.14·10.1·WP2 |
| 7 | 자리 고르기(포위·소환·예고 지점) | `INavWorld.PathLength`, `PlaceReach`·`TryPlace` 겹지정, `rules.spawn.path_detour` 1.5. 분열·일반 생성은 쓰지 않음 | 4.10·6.10·6.12·WP2·WP4 |
| 8 | 공정성: `--autoplay-visit rooms` 로 스테이지 1 방 문·벽 모서리 | `AppOptions.AutoplayVisit`, `AutoplayPolicy` 방 들르기 모드, 스테이지 1 10분 `Scenario` 한 번 더, 창살 너머 궁수 시나리오, 10.2 "안쪽 방" 행 | 9절·10.1·10.2·WP4 |
| 9 | 검증 13.5-19~21, 13.5-10 묶음판 | 6.14 표 세 행(19 = `ArenaLayout.ValidateRooms` WP1d, 20 = WP7 + WP0 시험, 21 = WP7 + 소리 WP8), 13.5-10 ② 묶음판 | 6.14 |
| 10 | 마법사 고정·"헬파이어 선택" 문장 | 1.4 "diabdat 에만 기댄다"·`optional`, 5.3 시전 클립, 5.6 주인공 죽음, 6.3 주인공 그림 묶음, 6.10 `rules.hero` 예, 4.2·6.11 "MVP 맵에 `w` 없음", 1.4 변환 규칙 주인공 행을 v0.7 에 맞춤. 역사 기록(13.4~13.11·WP1b 표)은 당시 문장 그대로 | 여러 곳 |

**더 정한 것**: 무기 `requires`(헬파이어 마법 그림 무기 — 레벨업 후보는 `requires ⊆ 스테이지 ∪ 직업` 으로 걸러 PC 와 무관, 6.8), `hfvoice` 는 `prefer` 전용(12.1), 벽 무리 나누기에 안쪽 방·창살(7.4), `--hero` 를 저장 보호 판정에(9절),
작업 묶음 WP9(직업 고르기)·WP10(헬파이어 스테이지)(11.1), 위험 R30~R32.

**원본으로 확인한 것**(조사 담당 2차 조사, 세션 작업 폴더 `diablo-inv/inventory-hellfire.md`·`inventory-rooms.md`, 읽기만 — 이 문서 작성 때 다시 읽지 않고 조사 결과를 옮겼다):
MPQ 항목 hellfire 584(이름 찾음 476)·hfmonk 336·hfvoice 90(88)·hfmusic 2. 헬파이어 일반 몬스터 TRN 없음. L5 = `nlevels\l5data`(CEL 1,836·MIN 650 × 10칸·TIL 217, `l5base.pal`), L6 = `nlevels\l6data`(CEL 1,763·MIN 606·TIL 166, `l6base1~5`).
헬파이어 팔레트 색인 128~255 = `l1.pal`. 수도사 칸 폭 112/130/98/114/160·27벌 모두 자기 죽기. L1 창살 35·36 = 기둥 칸만 셋 다·나머지 걷기만, 닫힌 문 25·26·98 = 벽, 아치 11·12 = 틈 하위 1칸, 벽 메가타일 1·2 는 반만 막힘.

**남은 열린 질문**
1. 기둥 시야(기획 열린 질문 22): v0.7 기본 = 원본대로(`x`, 안 막음). 바꾸면 `design_diffs` 가 아니라 `sol_known_diffs`(출처: DevilutionX 참고)에 `{ tile: 15, sub: 0, sol: "x", use: "X" }` 를 더한다(기획 v0.7 — 6.11 문장도 이에 맞춰 고쳤다).
   L5 기둥은 DevilutionX 도 보정하지 않으므로 바꿔도 L1 만이다.
2. 13.5-5 갇힌 바닥 칸의 "14px 안" 기준이 실제 L1 벽 모서리에서 거짓 오류를 내지 않는지 — WP2 가 실제 `cathedral_arena` 로 처음 돌릴 때 본다. 내면 기준을 "칸 중심에서 반경 14 + 하위 칸 반대각" 으로 넓힌다(데이터 아닌 검사 상수).
3. `PathLength` 의 A* 펼침 수 상한(소환 1회당) — WP2 가 `--stress` 와 보스 소환이 겹친 틱으로 재서 4.9 예산 안인지 적는다.
4. 기획 열린 질문 26~30(헬파이어 보스 거리·직업 균형·해금 순서·직업 그림·소리·안쪽 방 손맛)은 기획·첫 플레이 몫이다. 30 의 대안("핀드는 기둥·창살만 넘음")을 고르면 충돌 도형에 "안쪽 벽" 표시가 하나 더 필요하다
   (지금 `ArenaCollision` 은 둘레/안쪽 둘만 가른다, 4.2) — 계약 변경이라 그때 WP1e 로 모은다.

### 13.13 v0.6.1 — v0.6 리뷰 반영 (`docs/reviews/architecture-v06-review.md`)

구현(I1~I11)·검증(V1~V9)·기획(P1~P9) 담당 지적 29개. 높음·중간은 모두 반영했고, 낮음도 모두 반영했다(받아들이지 않은 낮음 없음). 같은 문제를 여러 담당이 짚은 것은 한 번에 고쳤다.
"둘 다" 지적의 기획서 쪽은 기획 담당이 v0.7.1 로 고쳤고(같은 시점), 이 문서는 그 판(10.1 "벽이 꺾이고 만나는 자리", 10.3 새 배치, 13.5-21·22, 15절 `VISIT`·`VISIT_SKIP`, 특성 패시브 `trait`)에 맞췄다.

| 지적 | 처리 | 자리 |
|---|---|---|
| 높음 I1 맵에 `requires` 칸이 없고 맵 그림 이름의 묶음을 알 수 없음 → 헬파이어 없는 PC 에서 L5·L6 맵 그림이 "꺼짐" 이 아니라 오류 | 반영(지적의 ② 안). 맵에는 `requires` 를 두지 않고, 맵 그림의 묶음 = 그 타일셋 표 항목의 `bundle`. `ImportTable.OutputBundles(arenas)` 가 맵 그림 이름도 묶음에 잇는다. 도구는 타일셋 묶음을 건너뛰면 그 맵 굽기도 건너뜀. 13.5-10 묶음판에 "맵 그림 → 그 맵을 쓰는 스테이지 D" 귀속(= 스테이지 `requires` ⊇ 타일셋 묶음). 합성 `l5` 시험(도구 ⑨·시작 검사 꺼짐 1·오류 0). ① 안(`MapDef.Requires`)을 고르지 않은 이유: 같은 사실(타일셋이 어느 MPQ 에 있나)을 표와 맵 두 곳에 적게 되어 어긋날 수 있다 | 1.4 "타일셋 묶음과 맵"·시작 검사 ③, 6.11 헬파이어 층, 6.14 13.5-10·20, WP1d #6·#14, WP10, 10.1 `Assets`·`Pipeline` |
| 높음 I2 `RunResult`·`ISaveService` 에 직업이 없음 → WP9 에서 다른 직업 판이 마법사 기록으로 조용히 저장되거나 계약 파일을 다시 고침 | 반영. `RunResult.HeroClassId`, `StageRecord`, `ISaveService.LastHero`·`SetLastHero`·`BestRecord(stage, hero)`. 저장 왕복 시험(합성 직업 둘의 기록이 따로) | 8.3, WP1d #13, WP6·WP7, 10.1 `Dev`·`Save`, R31 |
| 중간 I3 `AutoplayPolicy` 가 정적 클래스라 방 들르기 상태를 둘 곳이 없음 | 반영. `sealed class AutoplayPolicy(AutoplayVisit)`, `AutoplayVisit` 열거형을 Core 로, 방 번호 상태는 인스턴스에(시뮬레이션 상태 아님) | 9절, WP1d #18, 8.1, WP4·WP6 |
| 중간 I4·V2·V3·P1·P2 스테이지 1 예 배치가 자기 검사에 걸림(`open_center` 안 Γ 모서리, 꺾이는 자리 1칸 구멍 넷, 둘레 구멍, Γ 끝 ↔ 기둥 28.6px) | 반영. 기획 v0.7.1 10.3 배치로 예를 바꿈: 꺾이는 자리(T·├·Γ)는 모서리 조각, ㄴ 은 가로 조각, Γ 는 [13,13] 부터(`open_center` [6,6,13,13] 7×7 그대로), 기둥 [15,16]·[17,15]·[17,17], ㄱ 벽은 둘레와 뗌, 창살 두 칸. 6.11 에 "벽 맞물림" 규칙. 설계 담당이 v0.6 예와 새 예를 하위 칸으로 풀어 확인했다 — v0.6 예: 1칸 틈 다섯·둘레 막힘 줄어듦 하나·`open_center` 하나·80px 미만 여섯, 새 예: 모두 0(80px 는 마름모 거리 근사, 저장소 밖 스크립트) | 6.11, WP0a, 10.1 `Map`·`Nav` |
| 중간 I5·P4 쓸 수 없는 묶음 MPQ(깨짐·판 다름)가 기본 내용까지 "자원 없음" 으로 만듦, 우회 수단이 `import-all.ps1` 에 없음 | 반영. 그 묶음 전체 건너뜀(`열 수 없음`·`판 다름: <목록>`), 종료 코드 0 + 끝 보고 `경고:` 줄. 2·3 은 diabdat 와 `--hellfire` 직접 지정일 때만, 그 문구에 `--skip-bundles` 우회 명령. `import-all.ps1` 에 `-Hellfire`·`-SkipBundles`. 타이틀 한 줄의 앞말을 이유로 가름. 경고 전용 종료 코드(예 6)는 두지 않음 — 기본 내용이 다 만들어졌으면 성공이고, 종료 코드만 보는 호출자(`import-all.ps1`)가 실패로 오해하지 않게 | 1.4, 6.17, 10.1 `Pipeline` ③⑧, WP0 |
| 중간 I6 "스테이지에서 나올 수 있는 적" 계산이 13.5-15(WP4)·13.5-20(WP7)·`TextureStore` 에 따로 → 엇갈림 | 반영. Core `StageReach.Enemies(StageDef, GameDefs)` 하나, `PartDef.ReferencedEnemies`(로더가 연결 단계에서 매개변수의 `DefRef<EnemyDef>` 를 모음 — `IPartFactory` 에 메서드를 더하는 안보다 부품 묶음 코드가 늘지 않음). 구현·시험 WP1d-구현, 소유 WP4 | 6.14, 7.5·7.9, WP1d #19, 11.3 |
| 낮음 I7 `PathLength` 가 어느 끝이 주인공인지 모름, 필드·A* 고르기가 모호 | 반영. `PathLengthToHero` 를 따로, `PlaceReach.FromHero`, "반경 ≤ 등급 반경인 필드가 있고 후보가 통과 칸일 때만 필드, 아니면 자른 A*" | 4.10, WP1d #16 |
| 낮음 I8 `grid` 공백·줄 길이, `ValidateRooms` 오류 형, `TileMark` 사전 키 | 반영. `Split(' ', RemoveEmptyEntries)`·줄 길이 자유, `LayoutError.Warning` 을 더해 세 검사가 같은 형·부르는 곳 `MapDataReader.Read`, `TileMark` 에 `JsonStringEnumMemberName` 과 읽기 시험 | 6.11, WP1d #14·#15 |
| 낮음 I9·P6 스컬윙·본 데몬 소리 문장이 사실과 다름, 스컬윙 `skelt` 변종 누락 | 반영. 소리는 펠트윈만 hellfire, 두 적은 원작 소리 없음(WP10 에서 빌림). 스컬윙 변종 `skelt` 예외 | 1.4, 6.13, A.2, WP10 |
| 낮음 I10 manifest `source` 하나로는 여러 MPQ 출처를 못 적음, 음악만 대체돼도 타이틀 한 줄이 뜰 수 있음 | 반영. `sources`(MPQ 이름 목록, 정렬). `ContentAvailability.Disabled(defs)`(꺼진 스테이지·직업)로 바꾸고 `Any` 일 때만 한 줄·`ASSETS disabled=`. 음악만이면 `AUDIO … fallback=` | 1.4, 6.17, 8.1, WP1d #7·#8 |
| 낮음 I11 WP1d 가 17항목 + 구현으로 커져 주 경로가 김, "그 형을 쓰는 구현이 없다" 가 사실과 다름 | 반영. WP1d-형(짧게, WP2~WP8 앞) / WP1d-구현(WP0a·WP2~WP8 과 동시, 기다리는 시험을 적음). 문장을 "형을 읽는 코드는 `World.cs`·`ArenaLayout.cs` 뿐, #12·#15 가 같이 고침" 으로 | 11절 머리·그림, WP1d |
| 높음 V1 원작 방 "손으로 옮겨 적기" 가 DUN 칸 배열을 공개 저장소에 옮기는 것과 같고 막는 검사가 없음 | 반영. "본뜬다" = 크기·문 방향·벽 재료만, 칸 배열은 우리 조각 이름으로 새로 짬(6.11·R24·1.4 같은 뜻). `audit` 의 방 배열 대조 13.5-22(표 `audit_dun` 의 DUN 을 메모리에서만 읽어 4×4 창 비교). 지적의 "4×4 이상 같으면 실패" 에 **바닥 아닌 칸 ≥ 8** 조건을 더했다 — 벽 모서리 하나(7칸)처럼 어느 방에나 생기는 모양으로 거짓 실패가 나지 않게(WP0 이 실제 배치로 상수를 확인). 이 경계는 사용자 확인을 기다린다(아래 열린 질문 1) | 1.4, 6.11, 6.14 13.5-22, R24, A.3, WP0a·WP0, 10.1 |
| 중간 V4·P9 방 들르기에 실제로 들어갔는지 보는 수·로그가 없음, 직선 가중이라 문이 반대쪽인 방에 못 들어감 | 반영. 길 방향(`INavWorld.TryPathStep`, 0.5초마다), 40초 넘김, 정책 기록 `Events`·`RoomStats`, 로그 `VISIT`·`VISIT_SKIP`·`VISITS`. `Scenario` 통과 조건에 "모든 `room_*` 2번 이상" 과 `InvariantChecker` 의 `occluded` > 0. 들른 수를 지적의 `RunStats.RoomVisits` 가 아니라 정책에 둔 이유: `RunStats` 는 시뮬레이션 쪽(해시·결과)이고 봇 기록은 시뮬레이션 밖이다. `occluded` 는 매 틱 대신 60틱 표본(기준이 "> 0" 이라 충분) | 4.10, 9절, 10.1 공정성, 10.2 "안쪽 방", WP1d #16·#18, WP2·WP4·WP6 |
| 중간 V5 WP0a 의 "시제품 길 검사 다시 돌림" 은 저장소 배치를 검사하지 않음 | 반영. 자원·격자 없이 도는 하위 칸 검사(13.5-19 ① 4이웃 채우기 갇힌 바닥 ② 하위 1칸 틈)를 `ValidateRooms` 에. WP0a 완료 기준을 이것으로 바꿈, 시제품은 근거로만 | 6.14 13.5-19, WP0a, R32 |
| 중간 V6 시작 검사 ③ "꺼짐" 이 그 파일을 실제로 꺼진 정의만 쓰는지 안 봄, 소리 연결 표 줄의 귀속이 없음 | 반영. 13.5-10 묶음판에 귀속 규칙(`hero`·`enemies`·`sprite_sets`·`weapons`·`music`·`projectiles`·`ui`…), 정의 → 파일 추출을 Core `DefAssetNames.For` 하나로, 시작 검사 ③ 교차 확인(꺼짐 파일에 켜진 스테이지·직업이 닿으면 오류)과 WP7 합성 시험 | 1.4 끝, 6.14 13.5-10, WP1d #20, WP7 |
| 낮음 V7 실제 헬파이어 MPQ 항목을 WP9·WP10 까지 아무도 안 읽음 | 반영. `RealMpq` 에 `DDR_HELLFIRE_DIR` 와 묶음마다 대표 항목 | 10.1 `RealMpq`, WP0 |
| 낮음 V8 MVP `sorcerer.json` 예가 만들어지지 않는 아이콘을 가리킴 | 반영. 예에서 아이콘을 뺌. 특성이 패시브가 되어(P5) 아이콘은 `PassiveDef.Icon`(nullable) — 직업 단계에 WP5 가 | 6.16, WP9 |
| 낮음 V9 직업 반경이 14 를 넘으면 길 검사가 못 잡음 | 반영(기획 v0.7.1 13.5-21 안): 13.5-21 에 "직업 `radius` ≤ 14" 오류. 지적의 다른 안(13.5-5 등급 = 최댓값)은 고르지 않음 — 등급을 데이터로 움직이면 플로우 필드 등급(16)과의 관계까지 같이 봐야 한다 | 6.14 13.5-5·21 |
| 중간 P3 헬파이어 스테이지가 해금 사슬 중간에 끼면 헬파이어 없는 PC 에서 뒤 기본 스테이지가 영원히 잠김 | 반영. 기본 스테이지의 `unlock` 은 기본 스테이지만 가리킴(13.5-20 오류), 헬파이어 스테이지는 곁가지 | 6.12, 6.14 13.5-20, WP10 |
| 중간 P5 특성 모양이 좁음(능력치 키 하나, 특성 하나) | 반영. 특성 = `trait` 패시브(`HeroClassDef.Traits`), `PassiveDef.Trait`·`Classes`, `EvolutionDef.Classes`. `HeroTraitDef` 는 만들지 않음. 능력치 키로 안 되는 특성 효과(반격 등)는 패시브 일반의 확장이라 직업 계약 밖(R31 남는 위험) | 6.16, 6.14 13.5-21, WP1d #9, WP3, 10.1 `Progression` |
| 낮음 P7 창살 한 칸이면 보이는 창이 하위 1칸 | 반영. 예 배치에 창살 두 칸 | 6.11, WP0a |
| 낮음 P8 큰 적 길 검사가 방이 아닌 반쯤 막힌 구역을 안 봄 | 반영. 구역에도 `room_` 기준점(`room_west`·`room_south`) | 6.11, 6.14 13.5-5 |

**더 정한 것**: 1.4 의 DUN 형식 한 줄(부록 A.3 — `audit` 전용), `audit` 실패를 종료 코드 4 에 넣음, 저장소 밖에서 배치를 하위 칸으로 풀어 본 검사 결과(위 I4 줄), `VISITS` 판 끝 줄의 "방 안" 은 방 체류 압박의 방 바닥 계산이 정해지기 전까지 기준점 반경 60(v0.6.3 에서 방 바닥으로 바꿈 — 13.15).

**남은 열린 질문**
1. **원작 방 본뜨기의 경계**(사용자 확인 필요 — 기획 열린 질문 31): "크기·문 방향·벽 재료만 따르고 칸 배열은 새로 짠다, 장식 번호(피 묻은 벽 166~170 등)는 이름을 붙여 쓴다, `audit` 가 4×4 배열 대조" 로 좋은지. 칸 배열도 옮겨도 되면 13.5-22 를 빼고, 더 엄하면 피 방을 보통 벽 1·2·4 로 바꾼다.
2. 13.5-22 의 "바닥 아닌 칸 ≥ 8" 기준 상수 — WP0 이 실제 `cathedral_arena` 와 알려진 DUN 으로 거짓 실패 0 을 확인한다. 실제 원본 DUN 의 칸 값이 우리 `pieces` 번호 체계와 같은지도 WP0 이 `rnd6` 으로 확인(A.3).
3. ~~기획 v0.7.1 이 새로 넘긴 **방 보급 이벤트 `room_supply`**·**방 체류 압박**~~ — v0.6.3 에서 닫았다(13.15). WP1d 를 커밋하기 전에 형(`room_areas`·`RoomFloors`·`World.Rooms`·`RoomPresence`·`SpawnPickup`·`Pickup.SupplyRoom`·`SupplyCollected`·`room_pressure`)과 방 바닥 계산을 WP1d 에 넣었다.
4. ┘ 자리(가로·세로 벽이 꼭짓점으로만 닿음, 기획 10.1 표 마지막 줄)는 13.5-19 ② 가 잡지 않는다 — 걷기는 코너 자르기 금지가, 시야는 4.11 꼭짓점 규칙이 막으므로 남는 것은 그림뿐이다(WP0a 견본). 스테이지 1 에는 없다.
5. 13.12 의 남은 열린 질문 1~4 는 그대로다.

### 13.14 v0.6.2 — WP1d 구현이 정한 것

WP1d(형·구현, 커밋 전) 코드가 문서와 다르게 또는 더 정한 것. 코드가 정본이다(11.4 와 같은 규칙).

| 자리 | 문서 | 코드 | 이유 |
|---|---|---|---|
| 6.14 13.5-10·WP1d #20 `DefAsset` | `DefAsset(IDef Owner, string File)`, 6.14 는 `For(GameDefs)` | `DefAsset(IDef? Owner, string File)`(null = 기본 내용), `For(GameDefs, IEnumerable<ArenaGrid>)` — 6.14 문장을 #20 에 맞춤 | 귀속 규칙에 주인 없는 줄(`ui`·`pickups`·`music.title`)이 있다. 맵 그림 이름은 펼친 격자가 있어야 계산된다 |
| 6.11 4단계 문 칸 | "문 칸은 여전히 오류" | `over_border` 가 아닌 놓기가 테두리(문 칸 포함)에 오면 지금처럼 "테두리와 겹친다", `over_border` 놓기만 "문 칸" 오류 | 기존 오류 문구·시험을 그대로 둔다 |
| 6.11·13.5-19 `ValidateRooms` 더 본 것 | — | 주인공 시작 칸이 걷기 막힘이면 오류(① 채우기를 못 함, 격자 밖은 `MapDataReader` 의 기존 오류), `marks` 의 빈 번호 목록 오류(`ValidateTileset`), `open_center` 안 잘못된 메가는 메가마다 한 줄 | 검사가 조용히 건너뛰지 않게 |
| 1.4 manifest 쓰기 | 예의 diabdat 줄에 `bundle` 없음 | 원본·파일 줄 `bundle` 이 null 이면 쓰지 않음, `ManifestSkippedBundle.Unreadable` 은 계산 속성이라 쓰지 않음 | 예 모양 그대로, 왕복 같은 바이트 |
| WP1d-형 완료 기준 "`DefJson` 으로 읽기" | 공용 옵션 | 공용 옵션 + 시험용 참조 변환기(`TestKit/TestJson` — ID 문자열 → 연결 안 된 `DefRef`). `MapDef`·`TilesetDef` 는 `MapDataReader` 로 | 공용 옵션에는 `DefRef` 변환기가 없다(연결은 로더 WP7 5단계) |
| WP1d #17 합성 타일셋·방 맵 | 합성 타일셋에 `marks`·창살·모서리 | 기존 `TestDefs.Tileset()` 은 `marks.pillar` 만 더함. 방 시험은 새 `TestDefs.RoomTileset()`(6.11 예의 조각 이름, 번호는 가짜, 문 끝 조각 없음, 아치 `X...`)과 방 맵 ①~⑥(`RoomMapStrip`·`RoomMapOverBorder`·`RoomMapOneDoor`·`RoomMapExample`·`RoomMapNoCorners`·`RoomMapBorderNotch`) | 기존 시험의 번호·해시를 건드리지 않는다. ① 띠의 두 끝은 둘레 자리에 겹친 모서리로 막았다 — L1 식 반 칸 둘레는 둘레를 덮지 않고는 띠 끝을 막을 수 없다 |
| WP1d #17 기본 정의 | — | `TestDefs.Basic` 에 합성 직업 `test_hero`(= `rules.hero.class`, 시작 무기·특성 패시브 하나씩), `StraightNav.HeroPos`(빌더가 주인공 위치로 이음) | 기본 시험 월드의 `World.HeroClass` 가 null 이 아니게, `PathLengthToHero` 가 주인공을 알게 |
| 10.1 `Sim` "`Sim/` 이 묶음을 모름" 소스 문자열 시험 | WP4 | WP1d 가 `Assets/BundleTests` 에 먼저 둠 | 형을 낸 쪽이 바로 지킨다. WP4 는 옮기거나 그대로 쓴다 |
| 6.17 `ContentBundle` JSON 이름 | 소문자 | 공용 열거형 변환기가 대소문자를 가리지 않아 `"Hellfire"` 도 읽힌다(모든 열거형 공통, 기존 동작) | 공용 변환기는 바꾸지 않는다. `"diabdat"`·숫자는 오류 |

### 13.15 v0.6.3 — WP1d 검증 지적 반영

WP1d(형·구현, 커밋 전) 검증이 남긴 지적 셋(중간 1, 낮음 2)을 처리했다. 높음은 없었다.

| 지적 | 처리 | 바뀐 곳 |
|---|---|---|
| 중간 — 13.13 열린 질문 3(방 보급 `room_supply`·방 체류 압박)이 "WP1d-형을 내기 전에 정하기를 권한다" 인데 설계되지 않은 채 WP1d 계약이 나옴. 방 바닥·획득물 놓기·"안 주운 보급" 이 계약을 건드려 WP2~WP8 시작 뒤 다시 고칠 수 있음(결정 K·M 의 "계약을 다시 고치지 않는다" 와 어긋남) | 반영 — 계약 변경이 필요하다고 판단했다(방 바닥에는 맵 데이터가, 보급에는 WP4 → WP3 저장소 경로가 필요). WP1d 커밋 전에 형과 방 바닥 계산을 넣었다: 맵 `room_areas`(메가 사각형) + Core `RoomFloors`(사각형 안 4이웃 채우기, 문 수 = 사각형 밖 이웃 무리 수) → `World.Rooms`, 상태 `World.RoomPresence`, `ICombatSpawner.SpawnPickup(PickupSpawn)`·`Pickup.SupplyRoom`("안 주운 보급" 은 이 필드 하나)·틱 이벤트 `SupplyCollected`, `rules.spawn.room_pressure`(`After`·`EveryNth`·`MinDistance`), 13.5-19 방 영역·방 보급 검사. 이벤트 매개변수는 WP4 몫이지만 검사가 읽으므로 `anchor`·`pickup` 으로 고정 | 6.10·6.11·6.12·6.14·9절·10.1 `Map`, 11.1 WP1d #21~#24·WP0a·WP3·WP4·WP6·WP7 |
| 낮음 — 빈 `grid`·`.` 만 있는 `grid` 를 오류 없이 받아 아무것도 놓지 않음 | 반영. 놓은 칸이 0개면 `$.layout.place[i].grid` "grid 에 놓을 조각이 하나도 없다" 오류(다른 모양 오류와 같게) | `ArenaLayout.PlacementCells`, 10.1 `Map` |
| 낮음 — 11절 순서 줄과 WP1d 제목에 진행 상태가 없어 11장만 읽는 묶음 담당이 기다리는 시험을 켜도 되는지 모름 | 반영. 순서 줄·WP1d 제목에 "형·구현 끝남, 커밋 전", "기다리는 것" 끝에 "WP1d 가 커밋되면 모두 켜도 된다". 해시는 커밋할 때 적는다(커밋 전이라 아직 없음) | 11절 머리, 11.1 WP1d |

**더 정한 것**
- 방 바닥의 반경 14 는 하위 칸 4이웃 채우기로 대신한다 — 13.5-19 ② 가 하위 1칸 틈을 막으므로 사각형 안에서 반경 14 가 못 지나는 걷기 칸 띠가 남지 않는다. WP2 의 반경 등급 격자를 기다리지 않아 WP1d-구현에서 끝난다.
- 방 바닥을 `ResolvedMap` 이 아니라 `World.Rooms` 에 둔다 — `ResolvedMap.Resolve` 는 격자를 받지 않는다(시그니처 그대로).
- 방 체류 압박의 "셋 중 하나" 는 확률이 아니라 감독의 무리 순번(`every_nth`)으로 센다 — 결정적이고 시험이 쉽다. 방 안 자리 고르기가 다 실패하면 그 무리는 보통 자리에 낸다.
- 방 누적(`RoomPresence`)은 자동 조작의 `RoomVisitStats` 와 따로 시뮬레이션 쪽에 둔다 — 기획 10.1 의 "첫 플레이 로그" 는 사람이 하는 판이라 정책 인스턴스에 둘 수 없다. `RunStats` 에 넣지 않은 것은 결과 화면·저장에 쓰지 않아서다.
- 9절 `VISITS` 의 "방 안" 은 기준점 반경 60 대신 방 바닥이다(13.13 "더 정한 것" 의 임시 규칙을 바꿈). 방에 "닿음"(다음 방으로 넘어감)은 그대로 반경 60.
- 시험 픽스처: `TestDefs.RoomMap` 에 `roomAreas` 인자, ③·④ 에 영역(④ 는 6.11 예 그대로). 기존 `room_*` 기준점 시험 셋에 영역을 더했다(없으면 이제 오류).

**남은 열린 질문**: 13.13 의 1·2·4·5 그대로. 방 체류 압박의 8초·셋 중 하나·150px 는 기획 열린 질문 30 의 첫 값이다.

### 13.16 v0.6.4 — WP0a 구현이 정한 것

WP0a(커밋 전)가 이 PC 의 diabdat 를 읽어(`L1.TIL`·`L1.SOL` 보정 없이 직접, CL2 는 조사 `scan` 값) 실제 `Data/` 첫 판을 넣으며 6.11 예·기획 10.1·10.3 과 다르게 또는 더 정한 것.
번호는 조사 견본(L1 메가타일 견본 그림)으로 골랐고, 맞물림은 같은 렌더러로 배치를 그려 견본 그림을 눈으로 비교했다(그림은 저장소 밖 스크래치에만 있다).

| 자리 | 문서 | 정한 것 | 이유 |
|---|---|---|---|
| 6.11 `pieces` 피 방 번호, 11.1 WP0a, 부록 A.3 | 모양 예 `blood_wall_v` 166·`blood_wall_corner` 167·`blood_wall_h` 168·`blood_floor` 171(11.1 WP0a·부록 A.3 "피 묻은 모서리 167") | 세로 166(`X.X.`)·모서리 **168**(`XXX.`)·가로 **169**(`XX..`)·바닥 **172** | 원본 SOL: 166·167 = 세로(7070), 168 만 모서리(7770), 169·170 = 가로(7700). 167 을 모서리로 쓰면 `XXX.` 가 아니라 위 줄이 비어 하위 1칸 틈이 생긴다. 바닥은 무늬가 고른 172. 한 이름에 번호 하나라 원작 방 DUN 의 배열(변종이 섞임)과도 겹치지 않는다(아래 `audit` 근사) |
| 6.11 `props` 창살 36 | `"36": "Xw.."`, "기둥 칸만 셋 다 — 원본 확인"(기획 10.1 도 "원본 확인") | `"Xw.."` 그대로 두되 **원본은 `ww..`**(기둥 칸 조각 63 의 SOL 41 = 걷기만). `sol_known_diffs` 에 `{ tile 36, sub 0, sol "w", use "X" }`(출처: DevilutionX 참고 — L1 SOL 보정 목록의 조각 63) | 조사 견본·표(`L1-megatiles.tsv`)는 DevilutionX 보정을 넣은 값이었다. 설계 값(기둥 칸은 시야·투사체도 막음)을 따르고 차이는 보정 목록에 적는다. 기획 10.1·10.3 의 "원본 확인" 문구는 기획 담당이 고친다 |
| 6.11 `props` 기둥 15 | `x...`(원본) | `x...` — 보정(조각 24 시야 막힘)을 넣지 않음 | 기획 열린 질문 22 기본값. 쓰는 메가 중 DevilutionX 보정이 바꾸는 칸은 15 의 조각 24 와 36 의 조각 63 둘뿐이다(그 밖의 쓰는 메가는 보정 목록 조각을 쓰지 않음 — 원본 직접 확인) |
| 6.11 `gates` 끝 조각 | 예 `nw: { open 13, end_a 0, end_b 0 }`, 기획 10.1 "틈 양 끝은 벽 끝 조각", 조사 함정 7 "벽 끝은 틈 앞쪽에만" | 네 문 모두 `open` 13 + **`end_b` 만**: nw 6, ne 7, se 23, sw 24. `end_a` 는 둘레 조각 그대로 | 원작 짜임(DevilutionX `drlg_l1` 의 벽 끝 규칙)과 견본 그림: 벽 끝 6·7·흙 턱 끝 23·24 는 **번호가 작은 쪽 끝**(화면 위·왼쪽 끝)이 마감된 조각이다. 틈 앞쪽 벽(번호가 큰 쪽 끝이 빈 벽)에 6·7 을 두면 마감이 반대 끝(이웃 벽과 붙는 자리)에 가서 안 보이고 틈 쪽은 잘린 면 그대로다 — 시제품 그림이 그랬다. 성질은 6=1·7=2 라 충돌·시야는 같다 |
| 6.11 예 배치 — 벽 끝 자리 | 창살 방 `[13,2]`·피 방 `[5,2]`·ㄱ `[6,16]`·Γ `[13,16]` 에 `wall_end_v`, Γ `[16,13]` 에 `wall_end_h` | 창살 방은 틈 **아래** `[13,5]` 에 `wall_end_v`, 나머지 다섯 자리는 `wall_v`/`wall_h`. ㄱ `[3,13]` 의 `wall_end_h`(왼쪽 끝)는 그대로 | 위 행과 같은 이유 — 마감은 위·왼쪽 끝이 빈 자리에만 보인다. 아래·오른쪽 끝은 원작이 다음 칸에 작은 모서리 기둥(16·17 — 원본 `W...`(sol 11, 걷기·시야만), DevilutionX 보정 후 `X...`(조각 27·28 투사체 막힘). 쓰면 성질 표를 `X...` 로 적고 기둥마다 `sol_known_diffs` 원소를 더함, 출처: DevilutionX 참고)을 두어 마감하는데, 그러면 틈이 하위 3칸으로 줄어(문 = 하위 4칸, 80px) 두지 않았다 — 잘린 면이 남는 것은 WP5 가 미리보기로 본다 |
| 6.11 예 배치 — 피 방 바깥 벽 | `[5,0]`(T)·`[0,5]`(├) `blood_wall_corner`, 아래 줄 `[1..5,5]` `blood_wall_h` | `[5,0]`·`[0,5]` = `wall_corner`, 아래 줄 = `wall_h`(닫힌 문 장식 자리 `[2,5]` 는 그대로 — 번호는 아래 닫힌 문 행) | 피 묻은 벽 메가는 바닥 반 칸도 피 바닥이라, 방 밖을 향하는 아래 벽·오른쪽 T 에 두면 방 밖에 피 바닥 띠가 생긴다(그림으로 확인). 원작 도살자 방도 아래·오른쪽 벽은 보통 벽이다(재료만 따름, 6.11 "원작 방 본뜨기") |
| 위 배치 바꿈의 범위 | — | 하위 칸 성질은 6.11 예(합성 ④ `TestDefs.RoomMapExample`)와 **칸마다 같다**(시험 `RealMapTests.Placement_IsDesignExampleShape_ArtOnlyDifferences`) — 바뀐 것은 그림 번호뿐. 예외는 피 방 `[5,5]` 의 하위 (11,10) 하나(아래 ┘ 행 — 시험의 알려진 차이 목록). `open_center`·기준점·`room_areas`·입구·시작은 예 그대로 | 13.5-19·길 검사 결과가 6.11 예에서 검토한 것과 같다 |
| 닫힌 문 번호(미리보기 지적) | 6.11·11.1 WP0a·기획 10.1·10.3 "닫힌 문 98"(`door_closed` 98, `marks.closed_door` [98]) | `door_closed` **26**(가로 벽 가운데에 닫힌 문, 원본 SOL `XX..`)·`marks.closed_door` [26], 피 방 아래 벽 `[2,5]` 장식 그대로. 98 은 이름·성질 표에서 뺌 | 견본 그림과 원본 타일로 다시 그려 보니 98 은 피 튄 아치 가로 벽이고 문이 아니다. 닫힌 문은 가로 26·31·세로 25·30·103(피 묻은 세로 문). 장식 의도(기획 10.3 "닫힌 문은 아래 벽 장식")를 살려 가로 26 을 같은 자리에 둔다 — 성질이 98 과 같아(`XX..`) 충돌·시야는 그대로. 쓰지 않는 98 은 13.5-12(이름 붙인 번호만 성질 표 키)대로 표에서 뺐다. 기획 10.1·10.3 의 "98" 문구는 기획 담당이 맞춘다 |
| 피 방 오른쪽 아래 `[5,5]`(미리보기 지적) | 6.11 예·기획 10.3 "아래 줄 [1..5,5] 가로" | **`wall_post`(작은 모서리 기둥 3, 원본 SOL `X...`)** — 하위 (11,10) 이 바닥이 된다. 6.11 "벽 맞물림" 에 ┘ 규칙을 적음 | 가로 벽 2 는 위 줄 둘 다 막아 아래 벽이 오른쪽 벽 선(tx 10)보다 한 메가 더 나가 문 밖으로 삐져나와 보였다. ┘(가로 벽 왼쪽에서, 문 틈 선 위에서)은 기획 10.1 표대로 작은 모서리 기둥 — 그림으로 모서리가 닫히는 것을 확인했다. 문 틈 하위 4칸·방 바닥 81칸·문 수·13.5-19 그대로. 합성 ④(TestKit, WP1d 소유)는 고치지 않고 시험에 알려진 차이로 적었다 — 6.11 예를 고칠 때 ④ 도 같이 고치고 목록에서 뺀다 |
| 13.5-22 근사 | `audit`(WP0)가 원본이 있을 때 | 스크래치 도구로 알려진 L1 방 DUN 11개와 펼친 배치를 같은 규칙(바닥 13·162·163·0 하나로, 바닥 아닌 칸 ≥ 8 인 4×4 창)으로 대조 → 같은 창 0 | WP0 도구 전 미리 확인(원본은 메모리에서만 읽음) |
| 6.3 그림 묶음 `file` | 예 `d1/monsters/zombie.png`·`d1/hero/slt.png` | `d1/monsters/skelaxe.png`·`skelbow.png`·`zombie.png`(원작 폴더 이름), 주인공 `d1/hero/slt.png` 한 페이지(죽기 `sln_die` 도 그 페이지의 `die`) | 1.4 표 예의 `out` 규칙. 13.5-10 이 WP0 의 표와 대조한다 |
| 6.3 클립·프레임 시간 | 예 `set_zombie` 에 `special`, 프레임 시간 기획 4.0 "주인공 모두 50ms" | 단계 1 몬스터 묶음은 `idle`·`walk`·`attack`·`die` 만. 원작 동작 값(출처: DevilutionX 참고): 몬스터 간격 × 50ms(서기 좀비·해골 궁수 0.2, 해골 0.25, 나머지 0.05), 타격 프레임 좀비·해골 8·해골 궁수 12. 주인공 서기 0.2·죽기 0.1(플레이어 틱 수 4·2), 걷기·시전 0.05, 시전 프레임 8 | 단계 1 행동(추적·근접·돌진·자폭·`kite`+사격)이 특수 그림을 쓰지 않는다 — 필요해지면 WP5 가 클립과 가져오기 표 동작을 같이 더한다. 해골·불타는 해골은 원작에서 서기 간격이 다르지만(5·2) 묶음 하나라 해골 값. 주인공 서기·죽기는 기획 4.0 의 첫 값(50ms)과 다르다 — 원작 값을 첫 값으로 두고 기획 담당에게 알림 |
| 6.3 `base_speed`·`body_height`·`origin` | WP0a 어림, WP5 가 맞춤 | `base_speed` = 그 묶음 주력 종 속도(해골 70·해골 궁수 62·좀비 40·마법사 95), `body_height` 70·70·72·76, `origin` = (칸 폭/2, 칸 높이 − 16) | 기획 8.1 첫 값 규칙. 미리보기 전 어림 |
| 13.5-16 시험 규칙 | "그 줄 끝 또는 그 줄을 담은 객체·배열을 여는 줄 끝"(줄 단위 + 괄호 깊이) | 값이 여러 줄이면 그 줄마다 본다. 덮는 주석 = 그 줄 끝 주석 또는 **그 줄이 시작될 때 열려 있던 가장 안쪽** 객체·배열을 연 줄의 끝 주석(더 바깥 조상은 세지 않음). 문자열 안의 `//` 는 주석이 아님. 가져오기 표 `Table/tilesets.json` 은 있을 때만 본다(WP0 이 만듦) | 파일 맨 위 주석 하나가 전부를 덮는 일을 막는다 |
| 시험 도우미 | — | `tests/…/Data/RealData.cs`(저장소 `Data/` 원문 읽기·실제 맵으로 시험 월드 만들기 — 합성 직업·그림에 실제 맵·`rules.sight`·빛 반경 420). TestKit 은 고치지 않음 | WP1d 소유 파일을 건드리지 않는다. WP2·WP4 가 실제 맵 시험에 같이 쓸 수 있다 |

**WP0a 완료 기준 대조**: `MapDataReader` 로 실제 두 파일 오류·경고 0, `Expand` 성공, 13.5-4 배치 쪽·13.5-12·13.5-19(`ValidateRooms` — ①② 하위 칸·`over_border` 막힘·방 영역, 문 수 1·1·2·2) 통과(`Map/RealMapTests`),
13.5-16(`Data/SourceCommentTests`), 실제 맵 시야(`Visibility/RealMapVisibilityTests` — 시작 자리에서 열린 가운데 전부·기둥 너머 보임·맵 밖 안 보임, 피 방은 문 쐐기만·창살 방 안은 안 보임, 창살 앞에서는 창살 칸 너머 쐐기만, 뒤 문 진입점에서 맵 밖은 문 틈 쐐기만),
그림 묶음·`rules.sight` 형 읽기(`Data/RealSpriteAndSightTests`). 실제 데이터를 기다리던 다른 묶음의 건너뛴 시험은 없었다(`Assert.Skip` 은 `Perf` 분류뿐).

**남은 것**: 문 틈 앞 벽·방 벽의 아래·오른쪽 끝 잘린 면 — WP5 가 미리보기로 본다(11.1 WP5 v0.6.4). 모서리 기둥 16·17 을 쓰면 그 틈이 하위 3칸으로 줄어든다.
미리보기가 찾았지만 데이터로 고치지 않은 둘(WP5 가 정함, 11.1 WP5 v0.6.4):
① 앞 두 변 흙 턱(18·19·20·23·24)의 그림이 막힘 칸(tx/ty 38–39)보다 64px 위(한 메가 안쪽 화면 자리)에 보인다 — 원작 그리기와 같은 높이 표현인지 미리보기로 확인하고, 충돌 칸을 옮길지 정한다.
② 시야: 빛 반경 안의 긴 벽 칸이 같은 줄 이웃 벽 칸에 가려 안 보인다(피 방 아래 벽 (5..9,10), 남쪽 Γ (28..33,26)·(26,28..33)). 칸별로 벽을 가리면 긴 벽 가운데가 끊겨 보일 수 있다 — 렌더(벽 무리 단위 밝기) 또는 시야 규칙 보정 중 하나로 정한다.
13.5-22 는 WP0 `audit` 가 다시 돈다. 그림 묶음의 칸·프레임 수는 WP0 도구의 메타 대조(13.5-7)가, 성질 표는 도구의 성질 대조(1.4)가 같은 값을 다시 확인한다.
기획서에 남은 다른 문구(기획 4.0 주인공 프레임 시간, 10.1·10.3 의 창살 "원본 확인"·문 끝 조각 자리·피 방 번호)는 기획 담당이 맞춘다.

### 13.17 v0.7 — 기획서 v0.8(사용자 결정 R, 방 구조) 반영

사용자 결정 R "하데스처럼 방 구조로"(brief 요구 15 — 고른 안 **섞기: 방 = 짧은 생존**). 기획 28절 "설계에 넘기는 것" 1~13 의 처리. 계약은 기획 권고대로 WP2~WP8 시작 전에 한 묶음 **WP1e**(형 / 구현 둘로 — 11.1)로 낸다.
유지할 것(사용자 지정 — 타일·맵 형식, 안개, 가져오기, 무기·레벨업, 몬스터, 직업 4개, 헬파이어 묶음, 공정성, 결정성)은 형을 바꾸지 않았고, 이미 커밋된 `cathedral_arena` 는 액트 1 보스 방이자 단계 1 의 모든 방으로 쓴다(6.11).

| # | 넘긴 것 | 처리 | 자리 |
|---|---|---|---|
| 1 | 판 흐름 상태 기계, `World.Outcome`·`Director` 와의 경계, 맡을 묶음 | 방 흐름 = `RoomFlow`(순수 전이 + `RoomFlowSystem`, 새 단계 `SimPhase.RoomFlow` 16번), 상태 = `World.Room`(`RoomState`). 승리(`BeginVictory`)를 부르는 곳을 `RoomFlow` 하나로(방 월드), 감독은 `Room.SpawningOpen`·`T` 만 읽음. 판 수준(액트·순번·빌드)은 `RunState`·`RunSession`. 구현 WP1e-구현, 그 뒤 WP4 | 3.5·3.6, 11.1 WP1e #7·#8·#10 |
| 2 | 방을 바꿀 때 월드 수명, `World` 를 방마다 새로 / 맵만 갈기, 그림 미리 읽기 | **방마다 새 `World`**(결정 1 — 맵에 묶인 get-only 상태·내비·시야·감독이 "월드 = 맵" 을 가정, 방 씨앗과 `Tick` 0 이 결정성·표시 규칙을 그대로 세움). 넘김 = `HeroCarry`(WP3 `IHeroCarry`)·`RunStats`·판 흐름, 조명은 지난 `light_change` 다시 적용. 텍스처·곡은 액트 시작 때 풀 전부 | 3.6, 7.5·7.9, R33 |
| 3 | `StageDef` = 액트(`Map` → 풀, `Unlock` → 다음 액트, 방 순번 표, 보상 가중·1번 방 보상, 시각 = t, t → 방 시계, 생존 끝 버림, 들어온 문 바꾸기, `VictoryDef` 의 뜻) | `RoomMaps`·`Rooms`(`RoomDef` from/to·miniboss·boss)·`Rewards`·`NextAct`·`Slice`, `Map`·`Unlock`·`Victory` 삭제, `EliteChance.At`. 이벤트 시각 `PartDef.At`(로더가 꺼냄), `RoomEvents.Schedule`(t → 방 시계·유예·지난 상태형 다시 적용), 생존 끝 버림 = 감독, `Room.RemapEntry`(행진 목표 문도). `VictoryDef` 는 없앰 — 판의 끝은 `RoomState.IsLastInRun`(`RunSession` 이 정함) | 6.12, 3.6 결정 5, 6.2, WP1e #1·#6 |
| 4 | 맵 정의(`room_kinds`, 문 넷·`center` 필수, 넓이 배율, 나가는 자리, 13.5-23 작은 방 검사), `cathedral_arena` 그대로, `room_supply` 시각만 | `MapDef.RoomKinds`, `GateZones`(문 틈 칸·맞은편), `MapArea.WalkableCells`(넓이 배율 분자·분모 — `RunSession` 이 `RoomStart.AreaScale` 로), `ValidateRooms` 에 13.5-23 맵 쪽(`RoomMapRules` 상수 36·150px). `cathedral_arena.json` 은 `room_kinds` 한 줄만 더함(WP5). `room_supply` 는 이벤트 그대로·시각은 보스 방 t | 6.11 "방 맵", 6.12, 12.1 |
| 5 | 난수 두 줄기, 보상 후보 뽑은 뒤 거르기, `StateHasher` 에 판 흐름, `--room` 같음 | `RunSeeds.Flow`·`Room`, `RunPlanner`(뽑기 순서 고정, 액트 단위 미리), 보상 = 가중 비복원 순열 → 문 열 때 `RewardAvailable` 로 거르고 `fallback_reward` 로 채움, 해시에 판 흐름·방 상태, `--room` 같음 시험 | 3.4·3.6 결정 2, 10.1 `Run`, R35 |
| 6 | 문 보상 정의 `reward`, 보상 창 = 레벨업 오버레이, 큰 영혼석·완전 회복, "집어야 문이 열림", 소리 키, 축복 `blessing` | `RewardDef`(`kind` 넷 — 정예 상자는 정의 없이 드롭 상자를 `GatesExit` 로), `IProgression.RewardOffer`·`ChooseReward`·`RerollReward`·`SkipReward`·`RewardAvailable`·`PendingRewards`, `ChoiceKind.Reward`(대기 순서 레벨업 → 상자 → 보상), 큰 영혼석 = `XpToNext` 만큼, `Pickup.Reward`·`GatesExit`, `sound_bindings.rewards`, `PassiveDef.Blessing`·`Hero.Blessings` | 6.9·6.15·6.18, 8.1, WP1e #2·#5·#9·#14 |
| 7 | 정리 끝 끌어당김, 나갈 때 획득물 지움, 미니맵 남은 적 점, 봉인·문 위 아이콘·문 소리 | 틱 이벤트 `RoomCleared` → WP3 끌어당김·보스면 전부 회복, 나갈 때 지움 = 새 `World`(코드 없음), 미니맵 점 = HUD 가 `Phase`·적 수로(WP6), 봉인·아이콘 = 7.11(WP5), 신호 `DoorsOpened`·`RewardCollected`·`RoomCleared` → `ui.door_open` 등(WP8) | 3.6, 7.11, 8.2, 6.15 |
| 8 | HUD 방 줄·방 상태, 결과 화면, 타이틀 기록 한 줄, StageSelect 없앰, 액트 안 음악 이어 틀기 | 8.2 방 줄·방 상태(`RunState` + `World.Room`), 결과 = `RunState`(깊이·걸린 시간·방 줄), 타이틀 `BestRecord`, `StageSelectScene` 삭제, `PlayScene` 은 액트가 바뀔 때만 미리 읽기·`PlayMusic` | 8.1·8.2·7.9, WP6 |
| 9 | 저장: 기록 키 그대로, 값 = 깊이·승리·시간·처치·레벨, 비교 깊이 → 시간, 해금 안 씀 | `RunResult`(`StartActId`·`Reached`·`RunTime`), `RunRecord`, `IsUnlocked` 삭제, 비교 규칙, 저장 구현 전이라 옛 판 옮김 없음 | 8.3, WP1e #17, WP7 |
| 10 | `--room a:n`, `--stage` = 1번 방, `--start-at` = 방 시계, 자동 조작 정리·보상·문 우선순위·20초 `DOOR_SKIP`, `ROOM` 로그, 방 들르기 통과 조건 | 9절 표(`--room`·`--map` 새로, `--stage`·`--start-at`·`--stress` 새 뜻, `--capture-on doors|reward`), 자동 조작의 방 규칙(`DOOR_SKIP` = `PlayerCommand.Exit` 명령 — 12.1), 로그 `ROOM`(+ `level=`)·`DOOR_SKIP`·`RESULT`·`ROOMS` 새 모양, 방 들르기 = 판 전체·맵마다 | 9절, 10.1 공정성, WP1e #15·#16 |
| 11 | 가져오기: 작은 방 맵 4장·`catacombs_arena` 굽기, 물체·아이템 CEL 넷, 소리 셋 | 맵 굽기는 정의마다라 코드 변경 없음, 물체 CEL 은 `probe` 로 형식 확인부터(표 모양이 안 맞으면 11.3 계약 변경), 소리 표 셋, 13.5-10 맵 그림 귀속 = `room_maps` | 11.1 WP0, 6.15 |
| 12 | 검증: 13.5-3·18·20 고침, 13.5-23 새로, 시험(방 흐름·정리 안전장치·같은 순번 같음·문 바꾸기·빌드 유지·공정성 = 액트 1 실행) | 6.14 표(13.5-3 t 판·18 방 판·20 다음 액트 사슬·23 ①~⑤), 10.1 `Run`(새 행)·`Map`·`Director`·`Progression`·`Dev`·`Save`·`Sim`, 공정성 = 액트 1 실행·맵마다 셈, 10.2 `--room` 으로 바꾼 행과 "정예 방"·"방 흐름"·"단계 끝" | 6.14, 10.1, 10.2 |
| 13 | 만드는 순서: 단계 1 에 방 흐름·`cathedral_arena` 한 장·보상 둘·`--room`, 작은 방 배치는 기획이 단계 2·3 에 | 단계 1 = `slice` 스테이지(WP4) + `cathedral_arena` `room_kinds`(WP5) + 보상 둘(WP3) + `RunSession`·`--room`(WP1e·WP7·WP6). 작은 방 맵은 기획 표가 나오면 WP5(단계 2·3) | 6.12, 11.1 WP3~WP7 |

**더 정한 것**: 단계 `SimPhase.RoomFlow` 를 `EndTick` 앞에(3.5 — 단계 수 16 → 17), 액트 번호 = 뿌리 액트부터 `next_act` 사슬, `RoomRecord.Level`·`ROOM level=`(계단 앞뒤 빌드 이어짐을 로그로 봄),
`--map`(미리보기 맵 고르기), `ROOMS` 의 기준점 칸 이름 `anchor=`, `--stress` 는 그 액트 보스 방, 방 이동 시간 기준 ≤ 150ms(10.3), 위험 R33~R37, 해석 9줄(12.1).
v0.6 문장 중 폐기된 것("스테이지 선택"·"해금"·"`unlock`"·"`victory`"·"`survive`"·"10분"·"12분"·"`--start-at` 판 시각"·"넓은 방 하나")은 고치거나 그 자리에 "v0.7" 을 달았다. 13.1~13.16 의 판 기록은 고치지 않았다.

**남은 열린 질문**
1. **시간 효과를 방 사이에 넘길지**(12.1 `[해석]`): 마력 폭주(8s)·버프·피격 무적은 방이 바뀌면 끝난다. 기획이 넘기기를 원하면 `HeroCarry` 에 남은 틱 목록을 더한다(계약 변경, WP1f).
2. **방 이동 시간**(R33): 150ms 를 넘으면 맵별 펼친 격자·충돌·clearance 를 판 수명 캐시로 — 그때 `World` 생성자에 미리 만든 `RoomMapInfo` 를 받는 겹지정이 필요하다(계약 변경). WP2 가 처음 잰 값으로 정한다.
3. ~~**단계 1 의 순번 표시**(12.1 `[해석]`): `slice` 판은 HUD 가 `4 / 6` 처럼 배열 순서로 보인다. 기획 번호(`6 / 9`)로 보여야 하면 `RoomDef.Number`(보이는 번호) 칸을 더한다.~~ — v0.7.1 에서 닫았다(`RoomDef.Number`, 13.18 #25).
4. **판 중간 저장**(기획 열린 질문 33): 액트 사이 저장을 넣으면 `RunState`(씨앗·액트·순번·`HeroCarry`·`RunStats`·방 기록)를 직렬화한다. 같은 씨앗 재현은 판 흐름이 `(씨앗, 액트, 순번)` 함수라 그대로 선다.
5. **헬파이어 액트 잇기**(기획 열린 질문 38): 계단 둘이면 `next_act` → 목록 + 계단 문 고르기(`RoomExit.Stairs` 가 둘), 따로 판이면 저장 키 = 시작 액트(지금 모양 그대로). 정해지면 WP1f 또는 WP10 앞.
6. **물체·아이템 CEL**(기획 열린 질문 32): 가져오기 표 모양으로 안 풀리면 `ImportTable` 계약 변경(WP0 이 올림). 그 전까지 대체 그림.
7. **작은 방 생성**(기획 열린 질문 35, R37): 작은 방의 입구 대체 비율·넓이 배율 0.65 가 빽빽함에 맞는지는 첫 플레이·`--room 1:2 --stress` 로 본다. 바뀌는 것은 데이터(`size`·`area_scale`)뿐이다.
8. 13.13 의 1·2·4·5 는 그대로다.

### 13.18 v0.7.1 — v0.7 리뷰 반영 (`docs/reviews/architecture-v07-review.md`)

근거는 13.17 과 같은 **사용자 결정 R**(brief 요구 15 — "하데스처럼 방 구조로", 고른 안 **섞기: 방 = 짧은 생존**: 한 액트 = 방 6~8개 + 정예 방 + 보스 방, 방마다 1~2분 무리 생존 → 생성 멈춤 → 남은 적 정리 → 문마다 보이는 보상을 골라 다음 방.
유지할 것 — 타일·맵 형식(`ArenaLayout`·안쪽 방 검사), 안개, 원본 가져오기, 무기·레벨업, 몬스터, 직업 4개(MVP 마법사), 헬파이어 묶음, 공정성(시야 밖에서 쏘지 않음), 결정성(같은 씨앗 = 같은 판), 그리고 이미 커밋된 `cathedral_arena`(액트 1 보스 방·단계 1 의 모든 방)).
이 리뷰는 그 결정을 바꾸지 않고, v0.7 이 그 결정을 담은 방식의 구멍을 메운다. 지적 35개(높음 4·중간 19·낮음 12) — 높음·중간은 모두 반영했고 낮음도 모두 반영했다(일부는 제안과 다른 방법, 아래 "제안과 다르게").
"둘 다" 지적의 기획서 문장 쪽은 기획 담당이 v0.8.1 로 고친다(`docs/reviews/gdd-v08-review.md`) — 이 표의 처리는 설계 쪽이고, 기획서가 고쳐야 하는 문장은 맨 아래 "기획서에 넘기는 것" 에 모았다.

| 지적 | 처리 | 자리 |
|---|---|---|
| 높음 #1 `PlayerCommand` 끝 `sbyte Exit = -1` 은 `default`·`None` 에서 0 → 정지 명령이 0번 출구로 나감 | 반영. `CommandButtons.Exit` 깃발 + `byte ExitIndex`(기본값 0 = 깃발 없음 = 안 나감). 시험: `None` 으로 `Doors` 600틱 → `Exit` 아님, `default`·`new()` 에 깃발 없음. 제안 셋 중 깃발 안을 고른 이유: 버튼 깃발이 이미 있는 형이고 "+1" 같은 뜻 바꾸기가 없다 | 3.6 표 `Doors`, 9절 자동 조작, 11.1 #15, 12.1, 10.1 `Common`·`Run` |
| 높음 #2 판의 마지막 방·진 방은 `Advance` 가 안 불려 걸린 시간·방 줄·공정성에서 빠짐 | 반영. `RunSession.Conclude()` — `Advance` 와 같은 내부 `CloseRoom` 으로 마지막 방을 닫음, `PlayScene` 이 `Finished` 첫 앱 틱에 한 번(포기 포함), 두 번째는 무동작, 그 뒤 `Advance` 예외. `RunResult` 는 `Conclude` 뒤의 `RunState` | 3.1·3.3·3.6 결정 4, 8.1·8.3, 9절, 10.1 `Run`, 10.2 |
| 중간 #3 `RemapEntry` 가 한쪽만 바꿔 6:30 행진이 "nw → nw" 가 됨 | 반영. **서로 바꾸기**(들어온 문 ↔ 맞은편), 시작 문·목표 문 둘 다. 13.5-23 ⑤ 에 "바꾼 뒤 시작 = 목표 → 오류", `RoomEvents`·`Director` 시험에 들어온 문 sw·se 두 경우 | 3.6 결정 3·5, 6.12, 6.14, 10.1 `Run`·`Director` |
| 중간 #4 `Doors` 같은 틱 레벨업 대기가 `Advance` 에서 사라지고, 끌려오던 보석·금화도 사라짐 | 반영(제안의 (a)) + 순서. `StageStepper` 판정 순서 `Finished` → `AwaitingChoice` → `RoomExit`(계약), `Reward` → `Doors` 에 "끌려오는 획득물 0"(`Pickup.Magnetized` 를 공통 조각으로), `Doors` → `Exit` 에 "대기 0·`AwaitingChoice` 없음·끌려오는 것 0", `IHeroCarry.Capture` 는 대기가 남으면 예외. (b)(짐에 대기 수)를 고르지 않은 이유: 나갈 때 늘 0 이면 짐 형이 커지지 않고, 0 이 아닌 길을 예외로 드러낼 수 있다 | 3.3, 3.6 표, 3.6 결정 1 표, 11.1 #12·#14, 11.2, 10.1 |
| 중간 #5 문 보상 물체 자신에 `GatesExit` 를 다는지 없음 → 물약 보상이 집기 전에 문을 엶 | 반영. `PickupSpawn.Reward != null` 이면 `SpawnPickup` 이 `GatesExit = true`(WP3), 정예 상자는 `RoomFlow`. 시험: 물약·마법서 각각 줍기 전 문 안 열림 | 3.6 표, 6.18, 11.1 #14, 11.2, 10.1 `Run`·`Progression` |
| 중간 #6 방 맵 풀이 여럿이 되며 기준점·점 빛 `[tx,ty]` 검사의 기준(스테이지 맵 하나)이 사라짐 | 반영(검사 안). 13.5-23 ⑤ 에 "이벤트 기준점은 그 방 종류 풀의 모든 맵에", 점 빛은 방 맵이 둘 이상인 액트에서 기준점 ID 만·액트의 모든 방 맵에. 13.5-1 은 풀 규칙을 따름. 점 빛을 `MapDef` 로 옮기는 안은 맵마다 다른 빛이 필요해질 때로 미룸(계약 변경) | 6.12, 6.14 13.5-1·23, WP7 v0.7.1 줄, 10.1 `Director` |
| 중간 #7 게임 쪽 서비스(Fx·소리·안개 그리기·미니맵)가 월드를 넘어 살아 나이 음수·`EntityId` 재사용·옛 맵 텍스처 | 반영. 게임 계약 `IWorldBound.Attach(World)` 한 곳, `PlayScene` 이 `Advance` 전 표시 이벤트 소비·`Clear`, 뒤에 `WorldRenderer` → `FxSystem` → `Hud`·미니맵 → `IAudioServices` 순서. 첫 월드도 같은 길 | 7.11, 3.3, 8.1, 11.1 #17·#21, WP5·WP6·WP8 v0.7.1 줄, 10.2 "방 흐름" |
| 중간 #8 `RunStats.Reset()` 공개 + WP3 `ResetCombat` → 방마다 처치·금화 0, 칸 순서가 바뀌면 무기별 피해가 다른 무기에 | 반영. 지우는 곳 없음(`RunSession` 이 판마다 새로), `Reset()` 삭제(부르는 곳 없음 — 코드 확인), `HeroCarry` 칸 번호 순서·`Restore` 같은 칸. 시험: 두 방 뒤 `Kills`·`DamageByWeapon[칸]` 누적 | 3.6 결정 1 표, 11.1 #24, 11.2, 10.1 `Run`·`Progression` |
| 중간 #9 형 단계 "빈 본문 = 예외" 면 방 월드를 한 틱만 돌려도 예외(형 자신의 완료 시험이 실패), `Gates` 연결이 구현의 `World.cs` 수정을 부름 | 반영. 형 규칙: `RoomFlowSystem`·`GateZones.Compute` 는 무동작(예외 없음), `World` 생성자는 형에서 이미 `Compute` 를 부름. 예외는 `Simulation` 밖 순수 로직만. 형 시험은 `WithRoom` 으로 단계 고정 | 11.1 WP1e 머리·#8·#11·완료 기준 |
| 중간 #10 #23 이 형·구현 양쪽에 있어 다른 묶음이 기다리거나 도우미를 따로 만듦 | 반영. #23-형(`TestDefs.Stage`·`WithRoom`·`FakeHeroCarry`·`FakeSpawner`·합성 `rules.rooms`·보상)과 #23-구현(`TestKit/RoomActs.cs` — 합성 액트·작은 맵)으로 나눔, 11.3 에 경계 | 11.1 #23-형·#23-구현, 11.3 |
| 중간 #11 `Run` 시험 셋이 구조상 못 지킴(① `Flow(n)` 만의 함수 ② `--room` 해시 열 같음 ③ 감독 없이 `light_change` 다시 적용) | 반영. ① #19 와 합침 — "`ActPlan` 전체는 (정의, 번호, 씨앗, rules) 의 함수, 맵은 앞 방에 이어짐, 문·출구·보상 순서는 `Flow(n)` 만, `RunPlanner` 에 `World` 입력 없음" ② #18 과 합침 — 방 해시로 비교 ③ `LightChangeParams`·`LightingState.Apply`(Core 순수)를 계약으로, `World(defs, RoomStart)` 생성자가 첫 틱 전에 `Replay` 적용(첫 틱 시야도 바뀐 반경) | 3.4, 3.6 결정 1·2·5, 11.1 #11·#25, 10.1 `Run` |
| 중간 #18 해시에 판 누적이 들어가 `--room` 비교가 처음부터 다름, 기획 "무엇을 했든 같은 생성" 이 지나침 | 반영. `StateHasher.Room`(방 해시 — `World` 상태만)·`Run`(판 해시 = 방 해시 + `RunStats` + 판 흐름). `--room` 은 방 해시, 판 두 번 비교는 판 해시. 3.4 문장을 "같은 짐·같은 입력이면 같은 방" 으로 | 3.4, 9절 `--log-hash`, 10.1 `Run`·`Dev`, R35 |
| 중간 #19 `Flow(n)` 만의 함수가 ① 의 앞 맵 빼기와 모순 | 반영(#11 ① 과 합침) | 3.6 결정 2, 10.1 `Run` |
| 높음 #16 정예 방 t 를 `to` 에서 멈추면 `[from, to)` 조회가 다음 방 구간(센 생성·새 종)을 씀, `RoomState.T` 주석도 엇갈림 | 반영. 정예 방 t 는 **`to` − 1틱**에서 멈춤(제안의 min 안 — 배율도 그 방 끝 값에서 멈춤). `RoomState.T`·3.1 표·6.12 주석을 맞춤. 13.5-18 은 정예 방 전체 `[t 시작, to)` 의 구간을 봄. 시험(Director·Run): 방 시계 2:00 에 5:00–6:00 줄·새 종 없음 | 3.1, 3.6 결정 3, 6.12, 6.14 13.5-18, 12.1, 10.1 `Director`·`Run` |
| 높음 #17 같은 깊이에서 옛 패배 → 새 승리가 기록되지 않음 | 반영. 비교 키 (깊이, 승리, −시간) 사전순. 시험: 같은 깊이 패배 → 승리 = 새 기록, 승리 → 패배 = 그대로 | 8.3, 3.6 `RunDepth` 주석, 10.1 `Save` |
| 중간 #20 보상 거르기와 집기 사이 레벨업으로 카드 0장 창, 체력 가득 물약 | 반영. 줍는 순간 0장이면 그 보상의 `skip`(금화 25), 보상 물체는 늘 집힘(물약 포함 — 11.1 체력 물약 규칙을 `kind: reward` 에 쓰지 않음). 봇은 0장 창을 만나지 않음 | 3.6 "문 열기", 6.18, 9절, 12.1, 10.1 `Progression`·`Run` |
| 중간 #21 정예 방 이벤트 적과 `rooms[].miniboss` 가 같은지 안 봄 → 생존 단계가 안 끝남 | 반영. 13.5-23 ⑤ 에 "정예 방 범위 `miniboss` 이벤트 정확히 하나·같은 적", 보스 방 `boss` 이벤트 정확히 하나. 고장 픽스처 시험 | 3.6 표, 6.14, 10.1 `Director` |
| 중간 #22 봇이 정예 방 2:30 뒤(`kite` 중간 보스)·`Reward` 단계에서 끝없이 멈출 수 있고 원인이 안 드러남 | 반영. 정예 방 `SpawningOpen` 거짓 뒤 중간 보스 쪽 길 방향 0.8, `Reward` 20s → `REWARD_STALL`(상태는 안 바꿈), 시나리오 방마다 틱 상한(넘으면 방·단계를 적고 실패). 보스 방 상한은 시나리오 상수 10:00 로 둠(지적에 값 없음) | 9절, 10.1 `Sim`, R34, WP4 v0.7.1 줄 |
| 중간 #23 문 틈 칸이 진입점에서 닿는다는 근거(13.5-5)는 진입점까지만 봄 | 반영. 13.5-23 ③ 문 틈 닿음(WP2 Nav/Validation — 반경 격자가 필요해 `ValidateRooms` 가 아닌 13.5-5 자리). 합성 맵 시험(문 틈 한쪽을 벽 끝으로 막음 → 오류). R34·12.1 근거를 이 검사로 | 3.6, 6.14, 10.1 `Nav`, R34, 12.1, WP2 v0.7.1 줄 |
| 중간 #24 `--start-at` 상한 = 생존 길이라 정예·보스 방에서 모두 막힘 | 반영. 일반 방 = 생존 길이, 정예 방 = `elite_spawn_cap`, 보스 방 = 상한 없음. 시험 `--room 1:9 --start-at 03:10` 통과·`--room 1:5 --start-at 02:40` 오류 | 9절, 10.1 `Dev` |
| 중간 #25 단계 1 순번이 배열 순서라 `--room`·씨앗·깊이가 단계마다 다른 방 | 반영. `RoomDef.Number`(JSON `number`, 없으면 배열 자리 + 1)를 지금 넣고 화면·로그·`--room`·`RunSeeds`·`RunDepth` 를 모두 번호로. `slice` 판은 기획 번호(1·2·3·4·6·8), 단계 1 통과 `depth=1:8`. 13.17 열린 질문 3 을 닫음 | 3.4, 3.6 결정 2·4, 6.12, 6.14 13.5-23 ①, 8.2, 9절, 10.2, 11.1 #1, 12.1 |
| 중간 #26 `best_depth: [액트, 순번]` 은 방 표가 바뀌면 다른 방을 뜻함 | 반영. 깊이 = `act`(ID)·`act_number`·`room`·`kind`·`rooms_hash`. 안 맞으면 **깊이만 비움**(다른 값은 남김) — 비율로 잇는 안은 고르지 않음(방 표가 바뀐 판끼리 깊이를 견주는 뜻이 정해져 있지 않음). `RunRecord.BestDepth` 는 `RunDepth?`, `RunResult.ReachedActId` | 8.3, 11.1 #17, 10.1 `Save` |
| 중간 #27 두 액트 판 전체가 자원 없는 시험에 없음 | 반영. 단계 3 완료 기준에 Core `Scenario` "판 전체"(실제 Data, 씨앗 2개, `[Trait("Long")]`) — 예외·`DOOR_SKIP`·`REWARD_STALL`·공정성 0, 계단 앞뒤 `Level` 이어짐, 액트 2 첫 방 보물 상자, 같은 씨앗 같은 `RoomRecord`·판 해시 | 10.1 `Sim`, WP4 v0.7.1 줄 |
| 낮음 #12 WP1e-구현이 단계 1 에 필요 없는 것(계단·넓이 배율·작은 방 검사·정예·보스)까지 지금 함 | 반영. 형은 그대로, 구현을 **구현-1**(단계 1 — 일반 방·한 액트·`--room`)·**구현-2**(단계 2 전 — 정예·보스·계단·`MapArea`/`AreaScale`·작은 방 검사)로. 10.1 `Run` 시험에 `[구현-2]` 표시 | 11.1 WP1e, 10.1 `Run` |
| 낮음 #13 "방" 두 뜻, `World.Rooms`/`World.Room` 한 글자 차이 | 반영(일부). 코드의 `World` 속성만 `InnerRooms`·`InnerRoomPresence` 로, 로그 `ROOMS` → `INNER_ROOMS`. 형 이름(`RoomFloors`·`RoomPresence`)과 데이터 키(`room_areas`·`room_*`·`room_supply`·`room_pressure`)는 그대로 — 데이터 키는 커밋된 맵·기획 문장이 쓰고, 같은 함수에 함께 나와 헷갈리는 것은 `World` 속성이다. 11.2 에 두 뜻을 굵게 | 6.12, 9절, 11.1 #11·#24, 11.2 |
| 낮음 #14 형 완료 기준 "기존 시험 그대로" 에 빠진 시험(`StageStepperTests`·`SimulationTests`·`FakeProgression`) | 반영(코드 확인: `WithVictory`·`(SimPhase)15`·`PhaseCount = 16`·반복 범위). 새 모양을 완료 기준에, 옛 모양 점검 `rg` 에 `WithVictory`·`(SimPhase)15`·`PhaseCount` 등을 더함 | 11.1 WP1e 머리·완료 기준 |
| 낮음 #15 ① `EnemyKilled` 에 `Def` 없음 ② `World` 생성자의 seed·stage 와 `room` 이 겹침 ③ 합성 월드에 `Spawner`·`Nav` 없음 | 반영. ① `DefIndex` 로 판정 ② 생성자를 둘로 — 방 월드는 `World(defs, RoomStart)` 하나(씨앗·액트·직업을 `room` 에서) ③ #23-형에 `FakeSpawner`·직선 내비 `TryPlace` | 3.6 표·결정 4, 11.1 #11·#23-형 |
| 낮음 #28 마지막이 아닌 보스 처치 틱의 상자가 `SpawnTick` 규칙 때문에 `GatesExit` 없이 남을 수 있음 | 반영. `RoomFlow` 는 이번 틱에 생긴 획득물도 포함. 시험 [구현-2] | 3.6 표, 6.18, 11.2, 10.1 `Run` |
| 낮음 #29 `--log-hash` 열이 3.4 와 9절에서 다름 | 반영. `act, room, stage_tick, room_hash, run_hash`(앱 틱 뺌) | 3.4, 9절 |
| 낮음 #30 출구 셋일 때 nw·sw 의 화면 x 가 같아 동률 기준이 없음 | 반영. 둘째 기준 화면 y 작은 문, 그다음 `Exits` 순서 | 9절, 12.1 |
| 낮음 #31 문 틈에 서 있던 주인공이 `Doors` 첫 틱에 바로 나감 | 반영. `RoomState.ExitArmed` — 나가는 자리 밖에 한 번 선 뒤에만 들어서면 나감. #4 의 대기 조건과 함께 써도 막히지 않음(대기가 풀리면 이미 무장된 상태로 나감) | 3.6 코드·표, 10.1 `Run` |
| 낮음 #32 저장 읽기 규칙(모르는 키·깨진 줄·높은 판)이 없음 | 반영(사실 확인: 저장 폴더 없음, 게임 저장 계약은 아직 v0.6 모양). 읽기 규칙과 v0.6 모양 픽스처 시험 | 8.3, 10.1 `Save` |
| 낮음 #33 "L2 방에서 딴다" 가 칸 배열 옮기기로 읽힐 수 있고 본뜬 DUN 이 `audit_dun` 에 없음 | 반영. 맵 첫머리 주석 `// 본뜬 원작 방:` ↔ `audit_dun` 대조(자원 없이), 예에 `bonecha1/2`. 기획 10.4 문장은 기획 담당 | 1.4, 6.11, 6.14 13.5-22, WP0 v0.7.1 줄 |
| 낮음 #34 끌어당긴 보석이 날아오는 중에 나가면 경험치 유실 | 반영(#4 와 합침 — `Reward` → `Doors` 에 끌려오는 획득물 0) | 3.6 표 |
| 낮음 #35 방 사이 시간 효과를 기획이 정하지 않음 | 설계 쪽은 `[해석]`(끊김) 유지 — 결정은 기획 몫. 기획 v0.8.1 이 2.5 "방 사이" 표로 정하면 이 줄을 닫는다. 끊김 + "보상 단계에서 시간 효과 시계 멈춤" 으로 정하면 `BeginTick` 의 버프·무적 감소를 `Room.Phase ≥ Reward` 동안 건너뛰는 한 줄(WP3)이 더해진다(아래 열린 질문 1) | 3.6 결정 1 표, 12.1 |

**제안과 다르게**: #1(세 안 중 깃발), #4((a) + 순서, 짐에 대기를 넣지 않음), #6(`MapDef` 로 옮기지 않고 검사), #13(`World` 속성·로그만 이름 바꿈), #22(보스 방 상한 값을 설계가 정함), #26(비율 대신 깊이만 비움). 이유는 표에 적었다.

**더 정한 것**: `RoomState.ExitArmed`, `Pickup.Magnetized` 를 공통 조각으로, `RunResult.ReachedActId`, `TestKit/RoomActs.cs`·`FakeSpawner`, 정예 방 생존 판정을 `DefIndex` 로, 13.5-1 이 기준점·점 빛을 풀 규칙으로 넘김, `REWARD_STALL` 로그, WP1e #24~#26.

**기획서에 넘기는 것**(기획 담당 v0.8.1 — 설계는 이 뜻으로 적었다):
1. 2.5 "난이도 분 t" — 정예 방은 `to` 직전에서 멈춤(그 방 구간을 끝까지 씀), 13.5-18 은 정예 방 전체 구간.
2. 2.5 "기록" — 비교 = 깊이 → 승리 → 짧은 시간. 깊이 기록은 방 표가 바뀌면 깊이만 비움.
3. 2.5 "결정성" — "같은 짐·같은 입력이면 같은 순번 방은 같다(방 씨앗이 앞 방 난수 소비와 무관)".
4. 2.4·10.3 들어온 문 바꾸기 = 서로 바꾸기(시작 문·목표 문 둘 다).
5. 11.4 — 줍는 순간 카드 0장이면 `skip`, 보상 물체는 상태와 상관없이 늘 집힘(물약 포함), 정리 끝 끌어당긴 보석·금화가 다 닿아야 문이 열림, 문 틈에 서 있었으면 한 번 나왔다 들어서야 나감.
6. 13.5-23 — 정예 방 `miniboss`·보스 방 `boss` 이벤트 정확히 하나·같은 적, 이벤트·점 빛 기준점은 풀의 모든 맵, 바꾼 뒤 시작 = 목표 오류, 문 틈 닿음, `number` 규칙.
7. 15절 — 문 동률 둘째 기준(화면 y → 출구 순서), 정예 방 2:30 뒤 봇이 중간 보스 쫓기, `REWARD_STALL` 로그, `--start-at` 상한(일반 = 생존, 정예 = 2:30, 보스 = 없음), 0장 보상은 봇도 `skip`.
8. 2.2·10.3·14.1 — 방 번호 = 기획 번호(`slice` 판도), 단계 1 결과 `depth=1:8`. 14.1 단계 3 에 Core "판 전체" 시나리오.
9. 10.4 "L2 방에서 딴다" → "크기·문 방향·벽 재료만 본뜬다", 본뜬 DUN 은 `audit_dun` 에.
10. 2.5 "방 사이" 표(이어지는 것·끊기는 것, 보상 단계의 시간 효과 시계).

**남은 열린 질문**
1. **방 사이 시간 효과**(#35, 13.17 의 1): 기획 v0.8.1 2.5 "방 사이" 가 정해지면 12.1 `[해석]` 줄을 닫는다. 넘기기로 하면 `HeroCarry` 에 남은 틱 목록(WP1f), 끊되 보상 단계에서 멈추면 `BeginTick` 한 줄(WP3).
2. **보스 방 시나리오 상한**(#22): 10:00 은 설계 상수다 — 단계 2 의 첫 보스 시나리오에서 실제 처치 시각을 보고 고친다(광폭화 + 여유가 기준).
3. 13.17 의 2·4~7 은 그대로다(3 은 #25 로 닫힘, 1 은 위 1).

### 13.19 v0.7.2 — WP1e 구현이 정한 것

WP1e(형·구현-1·구현-2 를 한 번에, 커밋 전) 코드가 문서와 다르게 또는 더 정한 것. 코드가 정본이다(11.4 와 같은 규칙).
형과 구현을 나눠 내지 않았다 — 같은 작업에서 구현까지 끝나 형 단계의 무동작 본문(`RoomFlowSystem`·`GateZones.Compute`)과 `NotImplementedException` 자리는 거치지 않았다. 다른 묶음이 기대는 계약 모양은 11.1 표 그대로다.

| 자리 | 문서 | 코드 | 이유 |
|---|---|---|---|
| 11.1 WP1e 나누기 | 형 → 구현-1(단계 1) → 구현-2(단계 2 전) | 셋을 한 번에. 10.1 `Run` 행의 `[구현-2]` 시험(정예·보스 분기·정예 방 t·계단·넓이 배율·13.5-23 ③ 맵 쪽·실제 `cathedral_arena` 사본)도 들어 있다 | 정예·보스 분기는 `RoomFlow` 한 함수 안이고 계단·넓이 배율은 `RunSession` 몇 줄이라 나누면 같은 파일을 두 번 연다 |
| 3.6 `RoomDef.Number` | "없으면 로더가 배열 자리 + 1" | `int Number`, 0 = 적지 않음(로더 WP7 이 채움). `RunPlanner.PlanAct` 는 번호가 1 미만(0 = 로더 전)이거나 앞 방 번호보다 크지 않으면 `ArgumentException` | 로더 전에 합성 정의가 0 을 넘기면 씨앗이 조용히 겹친다 |
| 3.6 ①~④ 뽑기 | 순서만 | 단계마다 난수를 **늘 정해진 개수** 쓴다: ①② 한 번씩(보스 방도 버리는 한 번), ③ 한 번(출구가 하나·없어도), ④ 가중 > 0 인 보상 수만큼. `RandomStream.Index` 대신 늘 `NextUInt` 하나를 쓰는 `Pick` | `Index(1)` 은 뽑지 않아 앞 방 맵 후보 수에 따라 ②~④ 의 난수 열이 밀린다 — "②~④ 는 `Flow(n)` 만의 함수" 가 깨진다. 가중 0 보상은 순열에 넣지 않음 |
| 3.6 `RunSeeds` | SplitMix64 섞기 | 소금 둘(`Flow`·`Room`) → 액트 → 번호 순으로 `SplitMix64` 세 번. 고정 값 시험 셋(`RunPlannerTests`) | 계약(11.3) — 바꾸면 그 시험이 깨진다 |
| 3.6 `RoomState` | 문서 필드 | 더함: `ClearTick`(보상 단계 시작 틱, 로그 `ROOM clear_tick`·`RoomRecord.ClearTick`, 정리 못 하면 −1), `CleanupLimitTicks`, `ChosenExitInfo`, internal `EnterPhase`. setter 는 모두 internal(시험은 `InternalsVisibleTo`) | `RoomRecord` 의 `ClearTick` 출처가 문서에 없었다 |
| 3.6 방 시계 | "Enter·Survive·Cleanup 동안" | `RoomFlowSystem`(16단계)이 틱 끝에 1 늘린다 — 앞 단계(감독)는 "이번 틱의 방 시계" 를 읽는다. `Enter` → `Survive` 는 늘린 값 ≥ 유예일 때, 일반 방 생존 끝도 늘린 값 ≥ `SurviveTicks` | 유예 180틱이면 감독이 처음 보는 생존 단계의 시계가 180 |
| 3.6 `T` | 상태처럼 적음 | 계산 속성(t 시작 + 방 시계, 일반·정예 방은 `to` − 1틱에서 자름 — 검증 반영: 일반 방을 `to` 에서 자르면 정리 단계 내내 다음 방 구간 줄·첫 등장을 가리킨다, 기획 v0.8.1 2.5) | 정리 단계 멈춤이 따로 기록 없이 선다 |
| 3.6 죽은 틱의 나감 (검증 반영) | "`Outcome != Running` 이면 아무것도 안 함" | `RoomFlow.Step` 은 `Hero.Dead` 면 나감만 막는다(`Reward` → `Doors`·`Doors` → `Exit` — 마지막 방의 보스 처치·정리 끝 → 승리는 죽은 틱에도 돈다, 3.5 "승리가 먼저"). `StageStepper` 는 `Outcome == Running` 일 때만 `RoomExit`, `RunSession.Advance` 는 `Outcome != Running` 이면 예외 | `RoomFlow`(16)가 `EndTick`(17)보다 먼저라 죽은 틱에 출구 틈에 들어서면 `Exit` + `DefeatPending` 이 되어 판이 끝나지 않고, `Advance` 가 죽은 주인공의 짐을 넘겼다. 죽은 틱의 `RoomFlow` 전체를 막으면 마지막 방의 같은 틱 승리가 패배로 뒤집힌다(v0.7 부터 방 월드의 승리를 부르는 곳이 `RoomFlow` 하나) |
| 3.6 `PlanAct` 주인 검사 (검증 반영) | 13.5-23 ①(WP7) | 정예 방에 `miniboss`, 보스 방에 `boss` 가 없으면 `ArgumentException`(번호 검사와 같은 자리) | 정예 방에 중간 보스가 없으면 생존 단계가 끝나지 않고 판이 멈춘다 — 로더 검사(WP7) 전에도 막음 |
| 3.6 `RoomSurviveEnded` | 이름만 | `RoomSurviveEnded(RoomKind Kind)` | 감독이 종류를 바로 안다 |
| 3.6 "`Reward` 시작" 정예 상자 자리 | `PlacementResolver.TryPlace` | `RoomFlow` 안에서 같은 규칙(맵 안·clearance ≥ 반경 + 4, 아니면 기준점 쪽 20px 씩, 절반까지 없으면 기준점)을 `INavWorld.IsInsideMap`·`Clearance` 로 직접. 둘레 각도 = 2π·k/상자 수(0 부터) `[해석]` | `TryPlace` 본문은 WP2 라 지금 예외다 — 11.1 #23-형 "직선 내비가 `TryPlace` 를 줌" 은 정적 함수라 시험 도우미로 줄 수 없다. WP2 가 끝나면 `TryPlace` 로 바꿔도 결과 규칙은 같다 |
| 3.6 보상 단계 → 문 | 같은 표 줄 | 보상 단계에 들어간 틱에는 문 조건을 보지 않고 다음 틱부터 본다 | 같은 틱에 놓인 보상 물체를 `Pickups`(15) 가 아직 못 봤다 |
| 3.6 결정 4 `CloseRoom` | "그 방의 `INNER_ROOMS` 줄 자료" | `RunState` 에 담지 않는다 — 앱(WP6·WP7)이 `Advance` 직전·`Conclude` 뒤에 아직 남은 `World.InnerRoomPresence` 를 읽어 쓴다 | 로그 형식은 WP7 몫이고 `World` 가 그때까지 살아 있다 |
| 3.6 결정 4 `RoomRecord.Level` | 방을 나갈 때 레벨 | `Progression?.Level ?? Run.Carry?.Level ?? 1` | WP3 없는 시험에서도 값이 선다 |
| 3.6 결정 4 `RunSession` | 문서 멤버 | 더함: `Concluded`, `AreaScaleOf(MapDef)`. `RunPlanner` 에 `Root`·`Next`·`IsLastInRun`, `RoomEvents.LightChange` 상수, `GateZones.Empty`·`OppositeSide`·`GateZone.Contains`, `RoomMapRules.Gates`·`CenterAnchor`, `RunSeeds.SplitMix64` | 한 계산을 여러 곳(`RoomFlow` 계단 이름·`RunSession`·검사)이 같이 쓴다 |
| 3.6 `Depth` | 들어간 방 | 들어간 방 중 가장 깊은 곳(줄지 않음) | 뜻 그대로 |
| 3.6 생성자 둘 | — | 방 월드는 `rules.rooms`·맵이 없으면 예외. 주인공 방위 = `center` 쪽을 8방위로 맞춘 벡터(`FacingMath.ToColumn` → `FromColumn`) | "방위" 라는 말대로 |
| 3.6 `ModuleContext.SetHeroCarry` | — | 두 번째 등록은 예외 | 묶음 둘이 짐을 다르게 넘기는 일을 막음 |
| 11.2 `RunStats` | partial + `ResetCombat` | `Reset()` 과 함께 `partial void ResetCombat()` 도 없앴다(지우는 곳이 없으므로). WP3 조각은 필드만 더한다 | 부르는 곳 없는 지우기 훅이 남으면 다시 "방마다 0" 길이 생긴다 |
| 9절 `AutoplayPolicy` | `Next` + `Move` | `Move` 를 없애고 `Next` 하나(본문 WP4) | `Next` 가 이동을 담는다 |
| 11.1 #23-형 시험 도우미 | `TestDefs.cs`·`TestWorldBuilder.cs`·새 `FakeSpawner.cs` | `FakeHeroCarry` 는 새 `TestKit/FakeHeroCarry.cs`. `TestDefs.Basic` 의 합성 보상은 셋(마법서·보물 상자·물약 — 문서 "둘") + 정예 상자 획득물, `victory` 인자를 없앰. `TestDefs.Map(roomKinds:)`·`TestDefs.Room(...)`·`TestDefs.RoomRules(...)`. `TestJson` 에 시험용 `ClockTime`·가중치 객체 변환기 | 물약 보상 시험(체력 가득이어도 문을 막음)과 6.12 예 읽기 시험이 필요했다 |
| 11.1 #26 `StateHasher` 자리 | `Room`·`Run`(본문 WP7) | 하지 않음 — `StateHasher` 형이 아직 없다(WP7). `--room` 같음 시험은 방 해시 대신 주인공·방 시계·단계·조명·난수 상태 열로 견준다 | WP7 이 `StateHasher.Room` 을 만들면 그 시험을 바꾼다 |
| 11.1 형 완료 기준 "옛 모양 점검" | `rg` 결과를 줄마다 | 남은 것은 모두 새 모양: `.Rooms` = `StageDef.Rooms`·`ActPlan.Rooms`·`RunState.Rooms`·`RulesDef.Rooms`·`RoomFloors.Rooms`, `StageMinutes`(그대로, 규칙 쪽은 `DifficultyMinutes`), `EliteChance`(새 `At`), `RoomPresence`(형 이름 그대로, 속성은 `InnerRoomPresence`), `.Victory` = `RunOutcome.Victory`, `(SimPhase)15` = `RoomFlow`(0부터 — 문서 1부터 16), `PhaseCount` = 17, `StageSelect`·`StageRecord` 는 바뀐 것을 설명하는 주석뿐 | — |

**기획 v0.8.1 과 아직 안 맞는 것**(설계가 먼저 받아야 함 — 기획 29절 "설계에 넘기는 것" 1~19): 이 구현은 설계 v0.7.1 대로다. 기획 v0.8.1 이 바꾼 것 — 문 보상 효과 종류(보물 상자 = 무기 +1Lv·허리띠 물약·금화, 성소 = 대가 있는 원작 성소, 물약 = 허리띠 물약 둘), 모든 방 출구 둘(정예·보스 앞도), 보상 자리 = 주인공 앞(`center` 아님), 정리 단계 "사냥 모드", `cleanup_limit` 25, 시간 효과가 방 사이에 이어짐, 짐에 허리띠·`단련`·성소 대가, 계단 보상·계단 저장, 거울판 짝 표 — 은 계약(`RewardKind`·`RoomRulesDef`·`RunPlanner` ③·`RoomFlow` 보상 자리·`HeroCarry`)을 다시 바꾼다. 설계 v0.8 이 받은 뒤 다음 계약 갱신 묶음(WP1f)으로 낸다. 지금 코드의 값(45s 등)은 시험 합성 값뿐이고 실제 `Data/` 는 바꾸지 않았다.
WP1f 에서 바뀌는 멤버 목록과 다른 묶음이 기대지 않을 것은 11.3. (기획 29절 1 "방 안의 t" 는 이 판에서 받았다 — 위 `T` 줄.) — v0.8 에서 받았다(13.20).

### 13.20 v0.8 — 기획서 v0.8.1(29절) 받기·사용자 결정 U(AI 확대)

근거 둘. ① 기획 v0.8.1 29절 "설계에 넘기는 것" 1~19(v0.8 리뷰 반영 — 근거는 여전히 사용자 결정 R "하데스처럼 방 구조로", 섞기: 방 = 짧은 생존). ② **사용자 결정 U**(2026-11-03): 원작 그림 일부를 Real-ESRGAN 두 모델로 4배 확대해 비교했고("AI 확대 비교" — 옛 스크래치 `aiup\out-*`, 저장소 밖), 사용자가 "AI 일반이 나은듯" → `realesrgan-x4plus`.
계약은 한 묶음 **WP1f**(11.1)로 모았고 11.3 의 ①~⑧ 목록을 닫았다. 실제 `Data/` 는 이 판에서 고치지 않는다(소유 묶음 v0.8 줄).

**기획 29절 "설계에 넘기는 것"**

| # | 넘긴 것 | 처리 | 자리 |
|---|---|---|---|
| 1 | 방 안의 t(끝 − 1틱), 13.5-18 정예 방 전체 | v0.7.2 에 받음(`RoomState.T`, 13.19), 13.5-18 은 v0.7.1 | 3.6, 6.12 |
| 2 | 기록 비교 깊이 → 승리 → 시간 | v0.7.1 에 받음 | 8.3 |
| 3 | 방 해시 / 판 해시 | v0.7.1 에 받음(본문 `StateHasher` 는 WP7) | 3.4, 9절 |
| 4 | 문 보상 계약(효과 종류·거르기·0장 `단련`·허리띠·채움 카드·마법서 건너뛰기 체력 10) | `RewardKind` = 마법서·무기 강화 꾸러미·허리띠 물약·성소(`RewardBundleDef`·`ShrineDef`), `BlessingCards`·`LevelUp`·`FullHeal`·`SkipWhenHpAtLeast`·`Skip.Gold` 없앰, 축복 패시브는 E(형은 남김), 거르기 v0.8, 0장·올릴 무기 없음·정예 상자 빈 강화 = `단련`(`level_up.temper`·`FillerCardDef.Stats`·`ChestItemKind.Temper`), 허리띠(`rules.hero.belt`·`Hero.Belt`·짐·`CueKind.BeltPotion`), `StatKey.MaxHpPct` | 6.18, 6.10, 6.9, 3.6, WP1f #1·#2·#13·#14·#19 |
| 5 | 문 갈래 — 정예·보스 앞도 출구 둘, 덤 보상 | `RunPlanner` ③ 의 "다음이 정예·보스면 하나" 를 지움(하나는 계단만), 정예·보스 방 `RoomState.Reward` = 덤, `--room` 시작 방도 같음, 고정 값 시험 갱신 | 3.6 결정 2·3, WP1f #7·#9·#11 |
| 6 | 방 흐름 — 끌려오는 것 조건·사냥 모드·`cleanup_limit` 25·남은 적 점·보상 자리 = 주인공 앞 | 끌려오는 것은 v0.7.1 에 받음. 사냥 모드 = WP4 본문(계약 없음), `cleanup_limit` 25, `remaining_marker`·`reward_anchor`·`chest_ring` 없앰, `reward_spot`·`RewardSpot.Find`/`Row`(직선 걷기 규칙 — 12.1), 13.5-23 ④ 의 `center` 조건 뺌(`center` 는 ③ 이 그대로 요구 — 큰 적 길 목적지) | 3.6, 6.10, 7.11, 12.1, WP1f #3·#8·#9 |
| 7 | 방 사이에 이어지는 것 — 시간 효과 이어짐·멈춤, 쿨다운 새로 | `HeroCarry.Effects`(남은 틱)·`World.TimedEffectsRun`(생존·정리에서만 흐름), 성소 대가·`단련`(`Mods`)·받은 성소·허리띠·칸 수도 짐, 12.1 `[해석]` "방마다 새로" 닫음 | 3.6 결정 1, 12.1, WP1f #8·#10 |
| 8 | 계단 보상·저장 | 계단에서 칸 +1(`rules.rooms.stairs`, 그릇 4 → 6), **계단 저장** = `RunSession.CanSuspend`·`Suspend`·이어 하기 생성자·`RunSnapshot`(STJ 왕복, `RunStats`·`FairnessCounters` 공개 필드 규칙), `run.json` 한 칸·읽으면 지움·불일치 예외, `ISaveService` 넷, 이어 한 판 = 끊지 않은 판(시험) | 3.6 결정 4, 8.3, 11.2, WP1f #10·#11·#13·#14·#15 |
| 9 | 경험치 곡선 L ≥ 20 계수 30, L ≥ 40 은 785 + 40 | 데이터 값(6.10 예 고침 — WP3 데이터). 곡선 형(`XpCurveSegment`)은 그대로 | 6.10 |
| 10 | 순번 = 표 번호 | v0.7.1 에 받음(`RoomDef.Number`) | 3.6, 6.12 |
| 11 | 13.5-23 보탬 — 이벤트 하나·150px 둘레 아님·방 이벤트 자리·거울판도 | 앞 둘은 v0.7·v0.7.1 에 받음. **⑥ 방 이벤트 자리** `RoomEvents.CheckPlacement`(0:20 강적·포위 오류, 끝 0:45 절정 경고 — 종류 목록 12.1), ③ 을 거울판에도 | 6.14, 3.6 결정 5, WP1f #6·#12 |
| 12 | 포위 고리 반경 하한 300·다시 나눔·감독 시험 | WP4 본문(계약 없음) | 11.1 WP4 v0.8 |
| 13 | 거울판 | 타일셋 짝 표(`TilesetDef.Mirror`, 성질 전치 검사)·`MapDef.Mirror`·`MapMirror.Of`(거울판 `MapDef` `<id>@m`)·(맵, 방향) 뽑기·바로 앞 쌍 빼기·문 이름 짝 바꾸기(`RoomPlan` 의 문·`RemapEntry`)·검사·`audit`·굽기 `<art>@m`·`ROOM mirror` | 6.11 "거울판", 3.6, 1.4, WP1f #5~#8 |
| 14 | 자동 조작 보탬, `ROOM` 로그 보탬 | 봇 규칙은 v0.7.1 에 받음 + 문 우선순위 v0.8(허리띠·성소 셋·덤). `ROOM` 의 `mirror`·`boss_s`·`picks_left` = `RoomRecord`·`RoomState.BossClockTicks`·`IProgression.PicksLeft` | 9절, 6.18, WP1f #10·#14 |
| 15 | `--start-at` 상한·입장 이벤트 바로 | 상한은 v0.7.1 9절 규칙을 Core `RoomStartLimits` 로(옵션 파싱·`RunSession` 생성자 둘 다), 입장 이벤트는 `RoomEvents.Schedule` 이 시작 시각으로 옮김, 광폭화 시계는 WP4 | 3.6 결정 5, 9절, WP1f #11·#12 |
| 16 | 판 전체 실행 | v0.7.1 에 받음 + "계단 저장으로 끊고 이은 판 = 끊지 않은 판"(Core 로 바로) | 11.1 WP4 v0.8, 10.1 |
| 17 | WP1e 구현 나누기 | v0.7.1 에 나눴고 v0.7.2 구현이 한 번에 끝냄(13.19) | 11.1 WP1e |
| 18 | 가져오기 — `audit_dun` ⊂, 물체 그림 대체, 계단 소리 후보 | `audit_dun` 은 v0.7.1. v0.8: 거울판 배치도 `audit` 대조, 거울판 굽기. 물체 그림은 검증 전까지 exe PNG·코드 그림(지금 설계 그대로), 계단 소리·조각은 기획 열린 질문 32 | 1.4, 11.1 WP0 v0.8 |
| 19 | 데이터 | `l1.json` 짝 표·세로 창살·세로 닫힌 문 = WP0, 이벤트 시각·HP·호위·`floor`·보상 정의·`rules/rooms.json` = WP3·WP4, 맵 `mirror` = WP5 | 11.1 각 묶음 v0.8 |
| (표 #25) | 층 — 방 문구·계단 문·넘김 1.0s | `RoomDef.Floor`(`floor`)·`RoomExit.NextFloor`·`rules.rooms.transition_floor`, 13.5-23 ① 층 규칙 | 6.12, 3.6, 7.11, WP1f #3·#4·#8 |

**사용자 결정 U — AI 확대**

| 항목 | 정한 것 | 자리 |
|---|---|---|
| 기능의 자리 | 가져오기 도구의 선택 기능 `--upscale realesrgan-x4plus`, 기본 끔(= 원본 픽셀). 게임 실행 옵션은 없다 — 확대는 가져올 때 한 번 | 1.4 "AI 확대" |
| 저장소 | 확대 결과도 넣지 않는다(사용자 PC 의 `Assets/d1/` 에만, 캐시). 실행 파일·모델도 넣지 않고 배포하지 않는다 | 1.4, R24 |
| 도구 받기·확인 | 사용자가 Real-ESRGAN ncnn-vulkan v0.2.5.0 을 따로 받음(`--upscale-bin`, 기본 `%LOCALAPPDATA%` 아래), `Table/upscale.json` 의 허용 SHA-256(실행 파일·DLL·모델)과 맞아야 띄움, `upscaler-check` 명령, 라이선스 BSD-3 확인·README 고지(WP0) | 1.4, R39 |
| 실패 | 없음·해시 다름·띄울 수 없음·시간 초과·결과 크기 틀림 → **확대를 모두 버리고** 원본 픽셀(기획 19.6 — 일부만 확대 안 함), 종료 코드 6, manifest `status` = `fallback`(일부만 확대된 채로 두지 않음), `reason`, 끝 보고 `경고:` 줄. 모르는 모델 이름은 2 | 1.4 |
| 적용 범위 첫 값 | 맵 그림(바닥 조각·벽 기둥·아치 덧그림) + 칸 폭 160 이상 몬스터(그 항목 전체), 표 `apply`·`include`·`exclude`(몬스터 `out`, 맵은 `tileset:<id>`)로 고침. 주인공·작은 몬스터·마법·아이콘·exe PNG 는 안 함 | 1.4, `upscale.json` |
| 그림자 | AI 에 넣지 않는다 — 그림자 마스크를 도구 코드로 매끈하게 2배(쌍삼차 → 상자 평균 → 1px 흐림, 결정적) 해 몸 아래에 같은 시트로 굽는다(기획 19.6 이 맡긴 "도구가 굽기 / 게임이 그리기" 중 도구). 게임 그리기는 그대로 | 1.4 ①⑥, 12.1 |
| 투명 가장자리 | 색은 가장 가까운 불투명 색으로 채우기(검은 테 막기), 알파는 같은 모델에 흑백 그림으로 따로 4배 → 2배 상자 평균 → 0.5 에서 잘라 있음·없음(기획 19.6 첫 값, 열린 질문 42) | 1.4 ②④⑤ |
| 이음매 | 프레임마다 16px 여백, 바닥 조각은 이웃을 32px 더 구워 가운데만 | 1.4 ③, 7.6 |
| 화면 배율 첫 값 | 4배 결과를 2배로 줄여 씀, `WorldRT` (960·s)×(540·s) = 1920×1080, 그리는 배율 = s ÷ 메타 `scale`(정수), 빛·안개는 월드 해상도 | 7.1 |
| 떨림 | 확인 항목: WP0 `contact` 견본(걷기 8프레임 줄)·10.2 "AI 확대" 행 | 11.1 WP0·WP5, 10.2 |
| GPU | `--upscale-gpu <n>` → `-g n`(없으면 실행 파일이 고름), manifest `gpu` | 1.4 |
| 결정론·캐시 | 확대 출력은 같은 바이트를 요구하지 않는다(GPU 마다 다를 수 있음) — manifest 에 도구·모델 해시만. 캐시 키 = 쓸 확대 방식(모델·해시·배율·처리 판), GPU 는 키 밖(바꾸려면 `--force`). 시뮬레이션·해시는 그림 배율과 무관 | 1.4 결정론·캐시, 7.1 |
| 계약 | `UpscaleTable`·`ManifestUpscale`·`ManifestFile.Scale`·`SpriteMeta.Scale`·`ArenaArt.Scale`, `AssetFormat.Version` 3 | WP1f #16~#18 |

**더 정한 것**: 거울판 `MapDef` 의 ID 규칙 `<id>@m`·`MirrorOf`, (맵, 방향) 후보 순서(풀 → 원판 → 거울판), 거울판 방의 들어온 문·출구·이벤트 문이 모두 짝 이름(계획 전체가 거울 모양), `HeroCarry` 를 init 속성 record 로, `RunSnapshot` 의 정의 확인(`Format`·`rooms_hash`·직업),
`RunStats`·`FairnessCounters` 공개 필드 규칙, `run.json` 을 `save.json` 과 따로, `--map <id>@m`, `CueKind.BeltPotion`, `upscaler-check`, 종료 코드 6, 위험 R38~R41, 해석 다섯 줄(12.1).
**없앤 것**(폐기 용어로 찾아 고침 — 13.x 판 기록은 그대로): `remaining_marker`·`reward_anchor`·`chest_ring`, `RewardKind` 의 `blessing_cards`·`level_up`·`full_heal`, `skip_when_hp_at_least`, `skip.gold`, `filler_gold`, `reward_shrine`, "정예·보스 앞 출구 하나", "정예·보스 방 `Reward = null`", "시간 효과는 방마다 끝남", "판 중간 저장 없음", "45s 안전장치".

**기획서에 넘기는 것**(기획 담당 — 이 판에서 설계가 정한 뜻으로 기획서 문장을 맞춘다)
1. 19.6(AI 확대 — 기획 v0.8.2 가 같은 때 쓰는 중, 설계가 고른 것을 맞춤): 폴더 옵션 `--upscale-bin` 의 기본 자리 `%LOCALAPPDATA%\DiabloDefenceRemastered\realesrgan-ncnn-vulkan\`, "경고와 함께 성공" 종료 코드 = **6**, 그림자 판은 **도구가 굽는다**(게임은 따로 그리지 않음),
   이음매는 프레임 16px 여백·바닥 조각 이웃 32px, 항목마다 끄기는 `Table/upscale.json` 의 `exclude`(몬스터 `out`·`tileset:<id>`) — 가져오기 표 항목 줄이 아니라 이 표 한 줄, 확인 명령 `upscaler-check`. 19.1 "저장소에 들어가는 것" 표에 `Table/upscale.json`(허용 해시·범위 — 원작 바이트 없음).
2. 12절(화면) — 확대 자원이면 월드를 2배 해상도(1920×1080)로 그린다는 것, 13.5-7·12.1 의 960×540 문장은 "월드 px" 로 읽는다는 것.
3. 11.4 보상 자리 — "가장 가까운 걸을 수 있는 칸" 을 설계 규칙(바라보는 쪽부터 방향 여덟을 차례로, 주인공에서 직선으로 이어진 clearance 30 자리)으로 맞춤. 6절·13.1 — 성소 정의 칸 이름(`all_weapons`·`random_weapon`·`pick_weapon`·`pick_max_level`·`stats`), 보물 상자 `bundle`, 새 능력치 키 `max_hp_pct`.
4. 10.8 거울판 — 짝 조각에 세로 창살·세로 닫힌 문이 필요(L1 35·25 후보, WP0 확인). 둘레·문 끝 조각(앞 턱 18 ↔ 19, 턱 모서리 23 ↔ 24, 뒷벽 1 ↔ 2 의 둘레 쓰임)은 자리(`borders`·`gates`)로 고르므로 짝 표에 넣지 않는다 — 기획 v0.8.2 10.8 의 짝 후보 목록에서 그 둘을 "자리로 바뀜" 으로. 그 전에는 `cathedral_arena` 거울판 없음(단계 1 은 들어오는 문만 바뀜). `--map <id>@m`, `ROOM map=` 은 원판 ID.
5. 13.5-23 방 이벤트 자리의 종류: 강적 = 정예 무리·해골 대장(호위)·포위 고리, 절정 = 행진·포위·정예 무리·호위.
6. 2.5 계단 저장 — 버린 저장(새 게임으로 지움)의 판은 기록하지 않음(설계 첫 값), 저장은 `save.json` 과 다른 파일.
7. **WP0a 에서 바뀐 값**(13.16 "남은 것" 의 기획 몫 — 기획 v0.8.2 30절이 같은 때 맞추는 중, 아래 목록과 같은지만 확인): 닫힌 문 98 → **26**(98 은 피 튄 아치 가로 벽 — 10.1·10.3 의 "닫힌 문 98"), 창살 36 의 원본 SOL 은 `ww..`(기둥 칸 `X` 는 DevilutionX 참고 보정 — 10.1·10.3 의 "원본 확인" 문구),
   문 끝 조각은 틈 **뒤** `end_b` 하나(nw 6·ne 7·se 23·sw 24, `end_a` 는 둘레 조각 그대로 — 10.1 "틈 양 끝은 벽 끝 조각"), 피 방 번호(세로 166·모서리 **168**·가로 **169**·바닥 **172**), 10.1 표 ┘ 행이 이제 피 방 `[5,5]` 의 작은 모서리 기둥 3(`wall_post`)에 쓰임(10.3 피 방 그림),
   주인공 프레임 시간(4.0 "주인공 모두 50ms" → 원작 값 서기 0.2s·죽기 0.1s, 걷기·시전 0.05s — 출처: DevilutionX 참고).

**남은 열린 질문**
1. **작은 캐릭터 확대**(기획 열린 질문 40): 주인공·작은 몬스터를 확대하지 않으면 한 화면에 확대 그림과 2배 점 확대 그림이 섞인다. 견본에서 얼굴·손이 뭉개졌다 — 보이는 차이가 크면 범위를 넓히거나 다른 모델·배율을 다시 비교한다(표만 고침).
2. **화면 배율**(기획 열린 질문 41): 첫 값 "4배 → 2배, 1920×1080". 창이 1920×1080 보다 크면 2배 그림을 다시 늘린다(k). 4배 그대로(3840×2160 타깃)는 메모리·채움 비용 때문에 첫 값에서 뺐다 — 렌더 측정(R38) 뒤 정한다. (v0.8.1) 4배로 가면 표 읽기의 `output_scale` 2 고정·메타 `scale` ∈ {1, 2} 를 푸는 **계약 변경(WP1g)** 이다(7.1).
3. **투명 가장자리·그림자**(기획 열린 질문 42·11): 알파 0.5 자르기(첫 값)가 계단처럼 보이면 부드러운 알파(도구 코드 쌍삼차)로, 굽은 그림자 판이 몸과 따로 놀면 흐림 폭을 — 둘 다 도구 안(`AssetFormat.Version` 을 올림).
4. **이음매·떨림**이 보이면: 떨림은 동작 단위로 한 판에 확대 → 그 항목을 범위에서 빼기 순(표·도구 안). (v0.8.1) 기둥 이음매는 여백을 늘려도 이어지지 않는다(번진 색은 이웃 기둥 질감을 모름) — **벽 줄을 배치째 확대한 뒤 기둥으로 자르기**(같은 그림 한 프레임 규칙 포기) 또는 **맵 범위에서 기둥 빼기**(표에 칸이 없어 WP1g), 7.6. 바닥·기둥 경계 모양은 알파 점 확대라 흔들리지 않는다.
5. **버린 계단 저장의 기록**(12.1 `[해석]`): 기획이 정하면 `DiscardRun` 이 `RecordResult` 를 부르게.
6. **L1 짝 조각**: 세로 창살(35 후보)·세로 닫힌 문(25 후보)의 원본 성질이 36·26 의 전치와 맞는지(WP0). 안 맞으면 `cathedral_arena` 는 거울판을 못 단다.
7. **칸 그릇 6**: 액트 셋 이상(헬파이어 액트 잇기 — 기획 열린 질문 38)이면 그릇을 늘린다(계약 변경, WP1g).
8. 13.17 의 2·5~7, 13.18 의 2 는 그대로다(13.17 의 1·4 와 13.18 의 1 은 이 판에서 닫힘 — 시간 효과 이어짐, 계단 저장).

### 13.21 v0.8.1 — v0.8 리뷰 반영 (`docs/reviews/architecture-v08-review.md`)

근거는 13.20 과 같다(사용자 결정 R "하데스처럼 방 구조로" — 섞기: 방 = 짧은 생존, 사용자 결정 U "AI 확대" — `realesrgan-x4plus`). 이 판은 결정을 바꾸지 않고 v0.8 이 그것을 계약·처리로 옮긴 방식의 구멍을 메운다.
지적 25개(높음 3·중간 14·낮음 8). 같은 문제를 본 지적은 합쳤다(#2·#17, #3·#16, #4·#18, #5·#20, #10·#22, #12·#24, #11·#23 일부). 반영하지 않은 지적은 없다. 바뀐 계약은 모두 **WP1f 에 넣었다**(11.1 표 #6·#8·#9·#10·#11·#13·#14·#16·#20 — 새 묶음 없음, 11.3).
기획서 쪽 문장은 같은 때 기획 담당이 v0.8.3 으로 고치고 있다(`docs/reviews/gdd-v082-review.md`) — 이 판은 그 v0.8.3 의 들어온 문 바꾸기 규칙(2.4)·끊는 틱(2.5·15절)·세로 창살 셋 중 하나(10.1)·그림자 굽기(19.6)에 맞췄다.

| # | 등급 | 문서 | 지적 | 처리 | 자리 |
|---|---|---|---|---|---|
| 1 | 높음 | 설계 | 거울판 `entries` 가 "ID 그대로" 라 입구 `gate_nw` 가 문 `gate_ne` 앞에 놓이고 검사가 못 잡음 | 반영 — `entries` ID 도 `MirrorGate` 로(기준점 ID 만 그대로), 13.5-23 ③ "입구가 같은 ID 문 앞"(가장 가까운 문 틈 칸), 시험 | 6.11 "방 맵"·"거울판", 6.14, 11.1 #6·완료 기준 |
| 2·17 | 높음 | 설계 | 칸 그릇 6 인데 `RunStats.WeaponSlotCount = 4`·`DamageByWeapon` get-only `float[4]` — 5번째 칸 범위 밖 예외, 저장 왕복에서 0 | 반영 — 상수 없앰, `{ get; set; } = new float[6]`, 저장 멤버 규칙 "공개 읽기·쓰기 멤버", 반사 시험이 필드 + 속성·get-only 면 실패 | 3.6 "계단 저장", 11.2, 11.4, 11.1 #14·완료 기준 |
| 3·16 | 중간 | 둘 다 | `Suspend` 가 틱 없이 정산해 끊지 않은 판과 보스 방 `Ticks`·재생이 다름, 자동 조작 판의 끊는 틱 없음 | 반영 — `Suspend` ⓐ Doors 면 계단 문 `Exit` 한 틱 ⓑ Exit 면 `Advance` 자리, 비교 기준 = 같은 틱 계단 문으로 나간 판, 자동 조작은 ⓑ | 3.6 결정 4, 8.3, 11.1 #11·완료 기준, WP4 v0.8 |
| 4·18 | 중간 | 설계 | 불일치 검사가 짐 ID 를 안 봄, `Carry` null, R41 "없는 값 = 기본" 이 `required`·기본 0 필드와 모순 | 반영 — 짐 정의 ID·`IHeroCarry.Validate`·`Carry` null 여부·`DamageByWeapon` 길이를 `Restore` 전에, `RunSnapshot.Carry` nullable, `CurrentFormat` + 모양 지문 시험, R41 을 "모양이 바뀌면 Format 올려 버림" 하나로 | 3.6 결정 4, 8.3, R41, 11.1 #10·#11 |
| 5·20 | 중간 | 설계 | 전체 10분 제한이면 첫 실행이 늘 되돌아갈 수 있고 진척이 안 쌓임 | 반영 — 묶음 실행(16MP)·진행 감시(2분 무진척)·확대 중간 캐시(되돌아가도 성공 묶음은 남김)·`확대 진척 n/m`·같은 판 한 번·몬스터 알파 프레임마다 한 번, WP0 실측 뒤 값 고침 | 1.4 "AI 확대"·"캐시", 11.1 WP0 v0.8 |
| 6 | 중간 | 설계 | 맵 그림 알파를 AI 로 확대해 0.5 로 자르면 바닥·기둥 경계가 흔들려 틈·겹침, 채우기 8px < 여백 16px, 이음매 대처가 "여백 늘리기" | 반영 — 맵 그림 알파 = 원본 2배 점 확대(AI 는 색만), 몬스터만 AI 알파, 채우기 ≥ 여백, 이음매 대처 = 벽 줄째 확대·기둥 빼기 | 1.4 ②④, 7.6, 13.20 열린 질문 4 |
| 7 | 중간 | 설계 | 작업 하나 = 한 스레드인 파이프라인과 "한 번에 확대" 를 잇는 구조가 없음 | 반영 — 세 단계(병렬 해독·확대 전 판·캐시 조회 → 주 스레드 GPU → 병렬 줄이기·묶기·PNG), 실패면 확대 대상 전부 (3) 을 원본으로, `--jobs 1` = `--jobs 8`·일부 적중 + 실패 시험 | 1.4 "결정론"·"AI 확대", 10.1 `Pipeline` |
| 8 | 중간 | 설계 | `--room 2:n` 시작은 4칸, `--weapons` 다섯이 조용히 넷으로 | 반영 — `RunSlots.OpenAt(액트 번호)` 하나(판 시작·계단 둘 다), 초과 `--weapons` 는 64 | 3.6 결정 4, 9절, 11.1 #11·#13, WP3·WP7 v0.8 |
| 9 | 중간 | 설계 | 판 수명 상태가 모듈 인스턴스에 숨으면 이어 하기 같음이 깨지고 `FakeHeroCarry` 시험이 못 잡음 | 반영 — 규칙(11.2·3.6), WP3 완료 기준 "실제 모듈로 끊고 잇기", 짐 필드는 WP1g | 11.2, 3.6, WP3 v0.8 |
| 10·22 | 중간 | 둘 다 | 그림자를 누가 그리는지 기획·brief 는 "게임이 따로", 설계는 "도구가 굽기" | 설계는 이미 "도구가 굽는다". 기획 v0.8.3 이 19.6·30절·Q42·머리말·brief 요구 16 을 맞춤(아래 "기획서에 넘기는 것" 1 은 확인만) | 1.4 ⑥, 7.5 |
| 11 | 낮음 | 설계 | 둘레·문은 짝 검사 밖이라 다음 타일셋에서 조용히 어긋남, "보정이 있으면 짝도 보정" 이 없는 보정을 지어내게 함 | 반영 — 자리 짝 검사(둘레 여덟 자리·문 변 짝의 `open`·`end_a`·`end_b`), 짝 비교는 최종 성질만 | 6.11 "거울판", 6.14 13.5-12, 11.1 #6 |
| 12·24 | 낮음 | 설계 | `output_scale` 이 바꿀 수 있어 보이나 Core 는 {1,2}·s = 2 고정 | 반영 — s = manifest `output_scale`(done 일 때), 표 읽기는 2 만, 시작 검사는 `scale` ∈ {1, s}·s 를 나눔, 4배는 WP1g | 1.4 표, 7.1, 13.20 열린 질문 2, 11.1 #16 |
| 13 | 낮음 | 설계 | WP1f 표 세부 일곱(`Hero.Reset` 인자, `DoorsOpened` 자리, `FairnessCounters` 조각, `Applies`, record `==`, 좁은 통로 보상 자리, `RoomActs` 소유) | 반영 — ① 0 + WP3 가 `OpenAt` ② `RewardSpot` ③ "모든 공개 멤버" ④ `Applies(ImportUnitEntry)`(최대 폭)·`Applies(tilesetId)` ⑤ 바이트 비교 ⑥ 두 번째 차례 문턱 = 주인공 반경 ⑦ WP1f 끝까지 WP1f | 3.6, 1.4, 11.1 #9·#13·#14·#16·#20, 11.3 |
| 14 | 낮음 | 설계 | `upscaler-check` 가 GPU 목록만 낼 수 없음, manifest `gpu` 가 캐시 적중에도 이번 값, 거울판 그림 메모리 미산입 | 반영 — 작은 무늬 그림 한 장 변환으로 장치 줄, `gpu` = 만든 때 값(`mixed`), 거울판 ×2 어림·`TextureStore` 가 `@m` 도 읽음 | 1.4, 7.1, 7.5, WP5 v0.8 |
| 15 | 높음 | 둘 다 | 서로 바꾸기가 맞은편 문의 보스·대장·행진을 들어온 문으로 옮김(해골왕이 늘 등 뒤 문), "늘 맞은편" 이 문제를 덮음, 검사가 못 잡음 | 반영 — **시작 문 기준 바꾸기**(기획 v0.8.3 2.4 — 시작 문이 들어온 문일 때만, 행진 목표는 들어온 문 ↔ 맞은편만), `RemapMarch`, 13.5-23 ⑤ "바꾼 뒤 시작 문 = 들어온 문 → 오류"(원판·거울판·들어올 수 있는 문마다), 시험 뒤집음 | 3.6 결정 2·3·5, 6.12, 6.14, 10.1, 11.1 #8·완료 기준, WP4 v0.8 |
| 19 | 중간 | 설계 | 실행 파일이 `vulkan-1.dll` 을 정적으로 가져와 런타임 없는 PC 에서 오류 창 + 10분 대기 | 반영 — 띄우기 전 `vulkan-1.dll` 확인(없으면 바로 되돌아감), 오류 모드(`SEM_FAILCRITICALERRORS`) 물려주기(WP0 확인), 0xC0000135 → 이유, 시험 | 1.4, R39, 10.1 `Pipeline`, WP0 v0.8 |
| 21 | 중간 | 둘 다 | `IUpscaler` 가짜만으로는 `Process` 경로가 CI 에서 시험되지 않음, 허용 해시를 못 바꿔 가짜가 늘 "해시 다름" | 반영 — 가짜 실행 파일 `tests/FakeUpscaler`(Process 경로 전체) + `IUpscaler` 가짜(처리 단위), 시험이 표를 주입, "결과 파일 없음 = 실패", `-m` 절대 경로 | 1.4 "시험"·"띄우는 법", 10.1 `Pipeline` |
| 23 | 낮음 | 둘 다 | 세로 창살 35 의 기둥 칸 보정이 원작에 없으면 기획 "의도 차이 MVP 없음" 과 충돌 | 반영(설계 쪽) — WP0 v0.8 ③ 셋 중 하나(보정 있음 = `sol_known_diffs`, 없음 = `design_diffs` "거울판 짝", 전치 아님 = 짝 표 밖·거울판 없음). 기획 v0.8.3 10.1 이 예외를 적음 | 6.11, 11.1 WP0 v0.8 |
| 25 | 낮음 | 설계 | 왕복 "같음" 을 record `==` 로 쓰면 늘 거짓, `grate_h` 오류 기대가 실제 데이터와 안 맞음 | 반영 — 직렬화 바이트 + 원소별 비교, 오류 목록 "포함" | 11.1 WP1f 완료 기준 |

**제안과 다르게 반영한 것**
- **#15**: 리뷰의 안 1·안 2 대신 기획 v0.8.3 2.4 가 같은 때 정한 규칙(시작 문이 들어온 문일 때만, 그 이벤트 안에서 들어온 문 ↔ 맞은편만 맞바꿈)을 따랐다 — 안 1 과 뜻이 같고, 게임 규칙은 기획서가 정한다. 행진에 함수 하나(`RemapMarch`)를 더해 "시작 문을 바꿨는지" 를 목표 문 판단에 넘긴다.
- **#3·#16**: 두 안을 다 썼다 — 메뉴(문 단계)는 ⓐ `Suspend` 가 계단 문 `Exit` 한 틱을 Core 안에서 돌리고, 자동 조작은 ⓑ `Phase == Exit` 자리에서 부른다. 앱이 멈춤 중에 틱을 따로 돌리지 않아도 된다.
- **#4**: `Carry` 는 `CanSuspend` 거짓 대신 nullable 로 — WP3 없는 Core 시험도 끊고 잇기를 돌려야 한다. 대신 `IHeroCarry` 등록 여부와 null 여부가 다르면 불일치다. WP3 만 아는 뜻(시간 효과 ID·진화 짝)은 `IHeroCarry.Validate` 로 넘겼다(Core 가 효과 ID 목록을 모름).
- **#5·#20**: 시간 제한은 "입력 픽셀 비례" 대신 "진행 없음"(출력 파일 수가 2분 동안 안 늚)으로 — GPU 속도를 몰라도 서고, 큰 묶음이 정상적으로 오래 걸리는 것과 멈춘 것을 가른다. 묶음 크기(16MP)·2분은 첫 값이고 WP0 이 이 PC 에서 잰다.
- **#12·#24**: 표에서 `output_scale` 을 빼지 않고 "2 만 받음" 으로 남겼다 — 기획 19.6·열린 질문 41 이 값을 표의 한 줄로 말하고 있어, 4배로 갈 때 바뀌는 곳(표 읽기 허용 값·메타 검사)을 한 곳에 모은다.
- **#2·#17**: "공개 필드만" 규칙을 "공개 읽기·쓰기 멤버" 로 넓혔다 — 지금 `RunStats`·`FairnessCounters` 의 멤버가 모두 get·set 속성이라 필드로 바꾸면 커밋된 코드 전부를 고쳐야 하고, 손실의 원인은 속성이 아니라 get-only 였다.

**확인한 것**(커밋 `ead8e15`, 읽기만)
- `RoomState.RemapEntry`(RoomState.cs 118~124)가 들어온 문 → 맞은편, 맞은편 → 들어온 문을 하고, `RoomEventsTests` 66줄이 `fromSw.RemapEntry("gate_ne") == "gate_sw"` 를 확인한다(#15).
- `RunStats.WeaponSlotCount = 4`·`public float[] DamageByWeapon { get; } = new float[WeaponSlotCount]`(Progression.cs 75·83), `RunStats` 의 다른 멤버는 get·set 속성(#2). `FairnessCounters` 는 `sealed`(partial 아님)이고 멤버가 get·set 속성(WorldState.cs 203~, #13 ③).
- `Hero.Reset()` 은 인자가 없다(Hero.cs 124, #13 ①). `RoomFlow.cs` 114·193 과 `World.cs` 87 이 `rules.RewardAnchor` 를 읽는다 — 114 는 v0.8 이 이미 `RewardSpot` 으로 바꿨고 193(`DoorsOpened` 자리)·87(주인공 시작 방향)은 v0.8.1 에 정함(#13 ②).
- 확대 프로그램(옛 스크래치 `aiup\dl\resr.zip`, SHA-256 `abc02804…` = 1.4 의 값)을 새 스크래치에 풀어 **실행하지 않고** 읽음: PE 가져오기 = `vulkan-1.dll`·`ole32.dll`·`KERNEL32.dll`·`OLEAUT32.dll`·`VCOMP140.DLL`(지연 가져오기 없음 — #19), 도움말 문자열에 GPU 목록 옵션 없음(`-g gpu-id`·`-t tile-size`·`-m model-path`(기본 `models`)·`-j` — #14·#21).
- 빌드·시험은 하지 않았다(문서만). 게임 창·원본 폴더를 쓰지 않았다.

**기획서에 넘기는 것**(기획 담당 — v0.8.3 이 대부분 이미 받음. 아래는 이 판에서 설계가 더 정한 것)
1. (확인만 — 기획 v0.8.3 이 이미 받음) brief 요구 16·기획 19.6·30절·Q42 의 그림자 = "가져오기가 AI 없이 매끈하게 2배로 늘려 시트에 굽는다(게임 그리기는 그대로)"(#10·#22).
2. 19.6 `upscaler-check` 문장(기획 v0.8.3 "그림을 만들지 않고 … GPU 목록만") → "확대 프로그램에 GPU 목록 옵션이 없어 임시 폴더에서 작은 무늬 그림 한 장을 변환해 장치 이름을 보인다(Assets 는 안 만듦)"(#14).
3. 19.6 되돌아감 이유에 `Vulkan 런타임 없음`(`vulkan-1.dll`), 실행 실패의 "시간 초과" → "진행 없음(출력이 2분 동안 안 늚 — 첫 값)", 첫 실행 시간은 WP0 실측 뒤(#5·#19·#20). 확대 결과는 묶음마다 중간 캐시에 남아 되돌아간 다음 실행이 이어서 끝낼 수 있다는 것.
4. 19.6 "투명 가장자리": 맵 그림(바닥·벽)의 알파는 AI 에 넣지 않고 원본을 2배 점 확대(경계 모양이 원본 그대로 — 틈 없음), 0.5 자르기는 몬스터만. 이음매 대처는 "여백 늘리기" 가 아니라 벽 줄째 확대·기둥 빼기(#6).
5. 2.5 계단 보상 — 액트 2 에서 시작한 개발 판(`--room 2:n`·`--stage stage_catacombs`)도 칸 5(열린 칸 = 액트 번호의 함수), `--weapons` 가 칸보다 많으면 옵션 오류(15절 문장, #8).
6. 15절·2.5 끊는 틱 — 설계는 두 길을 다 받았다: 메뉴는 그 틱에 계단 문 `Exit` 명령 한 틱(Core 안), 자동 조작은 문 틈을 밟은 틱(기획 v0.8.3 15절의 "설계가 고른다" 의 답).

**남은 열린 질문**
1. 오류 모드를 자식 프로세스가 물려받아 `vulkan-1.dll` 없는 실행에서 창이 정말 안 뜨는지(WP0 이 이 PC 에서 확인 — 꾸밀 수 없으면 "확인 못 함" 으로 남김).
2. `UpscaleBatchPixels`(16MP)·`UpscaleNoProgress`(2분) 첫 값 — WP0 실측 뒤 1.4·기획 19.6.
3. 13.20 의 남은 열린 질문은 그대로다(2·4 는 이 판에서 WP1g 조건만 더함).

### 13.22 v0.8.2 — WP1f 구현이 정한 것

WP1f(형과 순수 로직을 한 번에 — 11.1 WP1f 표 #1~#20) 코드가 문서와 다르게 또는 더 정한 것. 코드가 정본이다(11.4 와 같은 규칙). 실제 `Data/` 는 고치지 않았다.
"구현과 시험까지" 인 #6~#9·#11·#12·#20 은 본문과 xunit 시험까지, 나머지는 형·읽기 시험(본문은 표의 그 묶음)이다. 13.5-23 ⑤ 의 "바꾼 뒤 시작 문 = 들어온 문" 검사기는 WP4 몫이라 하지 않았다(11.1 WP4 v0.8.1 — `RemapEntry`/`RemapMarch` 만 냈다).

| 자리 | 문서 | 코드 | 이유 |
|---|---|---|---|
| 3.6 `RunSlots.OpenAt` | `(int actNumber, HeroRulesDef hero, RoomRulesDef rooms)` | `(int actNumber, LevelUpRulesDef? levelUp, RoomRulesDef rooms)` + `(int actNumber, RulesDef rules)`. `levelUp` 이 null 이면 `LevelUpRulesDef` 기본값(4·4), 액트 번호 1 미만은 1 | 시작 칸 수 `weapon_slots`·`passive_slots` 는 `rules.hero` 가 아니라 `rules.level_up`(`RulesDef.LevelUp`)에 있다(6.10 JSON) |
| 3.6 `RoomStartLimits` | `MaxStartClockTicks` 하나 | 더함: `Allows(room, rules, startClockTicks)`(0 이상이고 상한 미만). `RunSession` 생성자·WP7 옵션 파싱이 같은 판정을 쓴다 | "그 미만만" 을 한 곳에 |
| 3.6 "보상 자리" `RewardSpot` | `Find(World, RewardSpotDef)`, `Row(spot, facing, count, spacing)` | `Find(World, def)` = `Find(INavWorld?, feet, facing, heroRadius, def)`, `Row(INavWorld?, spot, facing, count, RewardSpotDef, float? threshold = null)`. 검증 반영: `Find(…, out float threshold)` 가 자리를 찾은 문턱(첫 차례 `clearance`, 두 번째 차례·주인공 발은 주인공 반경)을 돌려주고 `RoomFlow` 가 그 문턱으로 `Row` 를 당긴다(없으면 `clearance`). 상수 `Step` 8·`TurnDegrees`. 내비가 없으면(모듈 없는 월드) 벽이 없다고 본다. 바라보는 방향이 0 이면 남(+y). 마지막 걸음은 `distance` 에 맞춤(8 의 배수가 아님 — 100) | 순수 함수를 내비만으로 시험(좁은 복도·막힌 곳)하려고. `Row` 는 당길 때 `clearance` 문턱이 필요해 정의 전체를 받는다. 좁은 통로(clearance 30 미만)에서 줄을 30 으로 당기면 모든 자리가 `spot` 으로 돌아와 덤 보상과 정예 상자가 한 점에 겹쳤다 |
| 3.6 `RoomState.RewardSpot` | "그 전엔 주인공 시작 자리" | `World` 생성자가 주인공을 들어온 문 진입점에 세운 뒤 넣는다(internal setter) | `RoomState` 생성자는 맵을 펼치기 전이라 진입점을 모른다 |
| 3.6 결정 3 보상 단계 | 놓을 것 = 보상 + 상자 | 줄의 수 = 보상(있으면 1) + 살아 있는 정예 상자 수, 보상이 줄의 첫 자리. `RoomCleared` 신호 자리도 `RewardSpot` | 문서 순서 그대로, 신호 자리를 `DoorsOpened` 와 맞춤 |
| 3.6 `BossClockTicks` | 잡은 방 시계 | 그 틱에 1 늘린 뒤의 방 시계(정예 = `miniboss` `Damage` 처치 틱, 보스 = `BossDefeated` 틱) | `RoomFlow` 가 시계를 먼저 늘리고 전이를 본다(13.19) |
| 3.6 결정 2 ③ | "하나는 계단만" | 출구 하나 = 액트의 마지막 방일 때뿐(v0.7 의 `def.Kind == Boss` 조건도 뺌) | 보스 방이 액트 가운데 오는 데이터가 생겨도 "출구 하나 = 계단" 이 깨지지 않게 |
| 3.6 결정 2 ① | 거울판 후보 | `mirror: true` 인데 `MapDef.Mirrored` 가 null 이면 `ArgumentException`(읽기·`MapMirror.WithMirror` 를 거치지 않은 정의). `RoomPlan.MapId`(= `Map.MirrorOf ?? Map.Id`)를 더함 | 조용히 원판만 뽑지 않게. 로그 `map=`·`RoomRecord.MapId`·넓이 배율이 같은 식을 씀 |
| 3.6 결정 4 이어 하기 대조 | 문서 목록 | 더함: 다음 액트의 지금 번호 ≠ `NextActNumber`, `Stats`·`Fairness`·`Rooms` 가 null(STJ 는 null 을 막지 않음). 짐 칸의 빈 ID("")는 정의 대조에서 뺌. 검증 반영: 지금 번호는 뿌리가 아니라 **시작 액트에서 센다** — `RunPlanner.ActNumber(시작 액트)` + 시작 액트에서 `next_act` 를 따라 다음 액트까지 간 걸음 수(생성자·`Advance`·`Suspend` 의 "시작 번호에 계단마다 + 1" 과 같은 식). 다음 액트가 시작 액트 뒤 사슬에 없으면 불일치 | 사슬이 바뀌면 같은 `rooms_hash` 여도 씨앗(`RunSeeds` 의 액트 번호)이 달라 "이어 한 판 = 끊지 않은 판" 이 깨진다. `ActNumber` 는 뿌리 사슬 밖 액트(헬파이어 곁가지)에 1 을 돌려주므로, 뿌리에서 세면 곁가지 액트끼리 이어진 판(H1 → H2, WP10)은 늘 불일치로 저장을 잃는다 |
| 3.6 `CanSuspend` ⓐ | 대기 0·`AwaitingChoice None`·살아 있음 | + 끌려오는 획득물 0(`RoomFlow.ExitBlocked` 를 internal 로 같이 씀). `Suspend` 는 그 한 틱 뒤 `Phase == Exit`·계단 문이 아니면 `InvalidOperationException`. `RunSession.Suspended` 를 더함 | `CanSuspend` 가 참이면 ⓐ 의 한 틱이 늘 `Exit` 로 가게 — 문 단계의 나가는 조건과 한 함수 |
| 3.6 `RunSnapshot` | record·`CurrentFormat` | 더함: `JsonOptions`(공용 `DefJson` 규칙 + 필드 포함), `ToJson`/`FromJson`, `RoomsHash(StageDef)`(8.3 의 식 — 원소마다 (종류, 번호, from, to) 를 `rooms/1\|` 뒤에 이은 `StableHash` 16진 8자리, WP7 깊이 기록도 이 함수), 모양 지문 `ShapeFingerprint` 상수·`ShapeTypes`·`ComputeShapeFingerprint()`(공개 인스턴스 필드·속성 (형, 이름, 멤버 형) 서수 정렬, `JsonIgnore` 속성 뺌 — 검증 반영: 멤버 형에서 닿는 열거형(`StatKey` 등)의 값 JSON 이름 목록도 넣음) | "기대 값" 을 시험 파일이 아니라 `CurrentFormat` 바로 옆 Core 상수로 — 올리는 사람이 두 값을 한 곳에서 본다. 열거형 값을 없애거나 이름을 바꾸면 옛 `run.json` 이 `FromJson` 에서 깨지므로 지문도 바뀌어야 한다. 첫 값 `CurrentFormat` 1 · 지문 `7c149bd4`(검증 반영 전 `3d03c6b8` — 저장된 판이 아직 없어 판은 올리지 않음) |
| 3.6 `RunState` | — | internal 생성자에 `stats`·`fairness` 선택 인자, internal `AddRooms` | 이어 하기가 저장 값을 그대로 쓰게 |
| 3.6 `FirstRoomReward` | 액트 첫 방 = `first_room` | 첫 방 종류를 보지 않는다(v0.7 은 일반 방만) | v0.8 "덤 보상" — 정예·보스 방도 보상을 받는다 |
| 3.6 결정 5 `RoomEvents` | 상수 이름 | `StrongEarliest`·`ClimaxWindow` 는 `static readonly ClockTime`(C# `const` 불가), 입장 이벤트 목록 `EntranceKinds`(`miniboss`·`boss`). `CheckPlacement` 오류의 파일은 비움·경로 `$.events[k]`(강적)·`$.rooms[i]`(절정 경고), 절정 창 = 방 시계 [생존 끝 − 0:45, 생존 끝) | 검사기 등록(WP4)이 액트 파일 이름을 채운다 |
| 6.11 `MapMirror` | `Of(MapDef, TilesetDef)` | 더함: `TryOf`(오류 목록, 쓴 조각마다 "짝 표에 없다" — 짝 표가 없는 타일셋이면 모든 조각), `WithMirror(map)`(= `map with { Mirror = true, Mirrored = Of(…) }` — 읽기·시험 정의가 같은 함수), `TransposeProps`(T), `MirrorFacing`, `PieceMap`, `Suffix` `@m`. `grid` 전치는 줄 끝 `.` 를 줄인다. 거울판의 `Mirror` 는 거짓 | 실제 `cathedral_arena` 사본의 오류 목록에 `grate_h`·`door_closed` 가 따로 들어가야 한다(11.1 완료 기준). 짝 표가 없을 때 "표 없음" 한 줄만 내면 그 둘이 안 보인다 |
| 6.11 `MapDef.IsMirror` | `JsonIgnore` 셋 | `IsMirror` 는 계산 속성(`MirrorOf != null`). `JsonIgnore` 멤버 이름(`mirror_of` 등)이 파일에 있으면 STJ 엄격 읽기가 오류 없이 무시한다(채워지지는 않음 — 시험) | STJ 동작. 데이터 쪽 오탈자를 막으려면 WP7 로더가 따로 본다 |
| 6.11 `MapDataReader` | 거울판을 만들어 `Mirrored` 에 | 원판 줄 오류(짝 없음)는 그대로, 거울판 배치·방 검사 오류는 문구 앞에 `거울판 @m — `. 결과 목록에는 원판만(`def with { Mirrored }` 로 바꿔 넣음) | 오류 위치(파일·경로)는 원판 원소다 |
| 6.11 짝·자리 짝 검사 | 문서 규칙 | 더함: 짝 원소가 이름 둘이 아니면 오류, 변 짝의 한쪽에만 문(또는 `end_a`·`end_b`)이 있으면 오류. 성질 표에 없는 번호는 건너뜀(다른 검사가 냄) | 거울판에서 그 문이 없는 변으로 간다 |
| 6.11 입구-문 검사 | `ValidateRooms` | `ValidateRoomMap`(`room_kinds` 가 있는 맵)에서 — 13.5-23 ③ 의 줄이므로. 같은 거리면 `layout.gates` 순서 앞 문 | 문서 "13.5-23 ③" 그대로 |
| 6.10 `RoomRulesDef` | 새 키 셋 | `RewardSpotDef`·`StairsRulesDef` 의 필드도 모두 `required`. `RoomGatesDef.SingleExit` 주석을 "계단만" 으로 | `rules.rooms` 는 기본값을 코드에 두지 않는다(6.10) |
| 6.18 `RewardSkipDef` | `gold` 없앰 | `Heal` 만 남김. `ChestItem` 의 `Gold` 칸(record 인자)은 그대로 — 표에 없어서 | 계약 줄이기는 WP1g |
| 1.4 `UpscaleTable` | 모양·`Applies` 둘 | 더함: `SupportedOutputScale` 2·`TilesetPrefix`, `UpscaleModelDef`(`Scale`·`Files`)·`UpscaleApplyDef`(`Maps`·`MonstersMinWidth` — null 이면 몬스터 없음), 표 검사(모델 배율 2·4 이고 `output_scale` 로 나뉨, 허용 해시 하나 이상·소문자 16진 64자·경로 규칙, `include`·`exclude` 는 `d1/…` 또는 `tileset:<id>`·중복 없음). `ImportTable.Upscale` 은 `required` | 표 오류를 가져오기 전에 잡는다(해시가 틀린 표로 실행 파일을 띄우지 않음) |
| 1.4 manifest `upscale`·파일 `scale` | 모양 | `UpscaleStatus`(off·done·fallback) 열거형, `ManifestUpscale.ArtScale`(= done 이면 `output_scale`, 아니면 1 — 7.1 s), `ManifestFile.Scale` 은 JSON `scale`(`ScaleJson`, `int?` — 1 이면 안 씀) 뒤의 `int` | "1 이면 안 씀" 을 STJ 조건 무시(null)로 |
| 7.1 시작 검사 | "scale ∈ {1, s}·s 를 나눔" | Core `AssetScale.IsSupported`(1·2)·`FitsArtScale(scale, s)` — 메타·`arena.json` 의 `Validate` 와 WP7 시작 검사가 같은 함수 | 규칙 한 곳 |
| 11.2 `Hero` | 열린 칸·허리띠 | 공개 필드 `WeaponSlotsOpen`·`PassiveSlotsOpen`·`Belt`(같은 클래스의 다른 상태와 같은 꼴), `Reset()` 이 셋 다 0 | — |
| 7.9 `CueKind.BeltPotion` | — | 열거형 끝에 더함(기존 값 번호 그대로) | 소리 연결 표가 이름으로 읽어도 순서를 지킴 |
| 11.1 #20 TestKit | 합성 보상 넷 | `TestDefs.Rewards()` 는 넷(+ 성소 `test_reward_shrine`)이지만 `Basic`·`RoomActs` 의 가중표는 앞의 셋 — 기존 순열·문 열기 시험을 지킴, 성소는 정의 표·짐 `Shrines` 대조에만. `TestWorldBuilder.WithRoom` 의 보상 후보 순서 = 액트 가중표 순서(없으면 정의 표). `FakeHeroCarry` 에 `Extra`(Mods·Shrines·Effects 를 짐에 실어 나름 — 인스턴스에 판 상태를 두지 않음)·`ValidateReason`·`LastRestored`. `RoomActs.Defs(twoNormalMaps, mirror, floors)`·`MirrorTileset()`(`wall_v` ↔ `wall_h`, self `pillar`)·`MirrorWall(M)`(원판과 전치가 다른 가로 벽 한 줄 — 기둥 넷은 대각 대칭) | 거울판·층을 기본으로 켜면 v0.7 의 판 계획 시험이 모두 바뀐다 — 켜는 시험만 켬 |
| 10.1 끊고 잇기 시험 | (가)(나)(다) | 시험 모듈의 월드 설치가 주인공 만들기(WP3)를 흉내 내 열린 칸 = `RunSlots.OpenAt(World.Room?.Plan.Act ?? 1)` 를 넣는다. 비교 = `RoomRecord` 원소별·`ElapsedTicks`·깊이·`RunStats`/`Fairness`/짐의 직렬화 바이트·액트 2 방마다 생존 첫 4초의 방 상태 열 | `FakeHeroCarry` 만으로는 판 시작의 칸 수를 정할 곳이 없다 |

**옛 모양 점검**(11.1 WP1f — `rg` 가 이 PC 에 없어 같은 식을 `grep -rnE` 로 `src tests tools` 의 `.cs`·`.json`·`.csproj`·`.ps1` 에): 남은 줄은 모두 새 모양이다 —
없앤 키를 설명하는 주석(`RewardDefs.cs`·`RulesDefs.cs`·`RoomFlow.cs`·`World.cs`), 옛 키가 오류인지 보는 시험 입력(`RoomDefsTests` 의 `level_up`·`full_heal`·`blessing_cards`·`skip_when_hp_at_least`·`remaining_marker`·`reward_anchor`·`chest_ring`, `ContractV08Tests` 의 `filler_gold`), `rules.level_up` 키(`DefsTests`·`ContractV08Tests`), 소리 연결 키 `ui.level_up`(`AppServices.cs`),
`HeroCarry(` 에 걸린 `SetHeroCarry(`·`FakeHeroCarry(`·`WorksWithoutHeroCarry(`, `WeaponSlotCount` 가 없음을 보는 시험 한 줄. `SlotCount = 4` 는 없다.

**남은 것**
1. 13.5-23 ⑤ v0.8.1 검사기(바꾼 뒤 시작 문 = 들어온 문, 원판·거울판·들어올 수 있는 문마다)·`CheckPlacement` 등록 — WP4. 13.5-23 ④ 칸 수 상한(시작 칸 + `stairs` × (사슬 액트 수 − 1) ≤ 6)·`temper` 참조 — WP7 로더.
2. `RunSnapshot.ShapeFingerprint` 는 WP3·WP4 가 `RunStats` 조각에 멤버를 더하는 순간 시험이 깨진다 — 그때 `CurrentFormat` 을 2 로 올리고 지문을 새 계산 값으로(11.2 저장 멤버 규칙).
3. 실제 `l1.json` 짝 표·`cathedral_arena` 의 `mirror`·`Table/upscale.json`(허용 해시)·`rules/rooms.json`·`rewards.json` 은 소유 묶음(WP0·WP5·WP4·WP3)이 낸다 — 지금 실제 데이터에 거울판이 없다.

**검증 반영**(WP1f 검증 지적 — 위 표의 해당 줄은 이미 고친 뒤의 모양)

| 수준 | 지적 | 처리 |
|---|---|---|
| 중간 | 이어 하기 대조가 다음 액트 번호를 뿌리 사슬 번호(`RunPlanner.ActNumber`)와 견줘, 곁가지 액트끼리 이어진 판(H1 → H2 — WP10)은 늘 불일치로 저장을 잃음 | 시작 액트에서 셈(`RunSession.ActNumberAfter` — 시작 액트 번호 + `next_act` 걸음 수, 사슬에 없으면 불일치). 시험 `SuspendOnSideChain_ActNumberCountedFromStartAct_Resumes`(곁가지 두 액트 판을 끊고 이음·번호 다름·사슬 밖) |
| 중간 | 기획서 v0.8.3 이 13.21 넘긴 것 2~6 을 아직 안 받아 19.6·2.5·15절이 설계와 다름, 머리말의 기준 판 | 기획서는 기획 담당 몫이라 고치지 않음. 11.1 WP0 에 "시작 전" 조건(받은 뒤 시작, 그 전엔 설계서 문장 우선)을 적고 머리말에 아직 안 받았음을 적음. 받으면 기준 판을 올림 |
| 낮음 | 본문 일곱 곳이 `rules.hero.level_up.*`·`OpenAt(actNumber, HeroRulesDef hero, …)` | `rules.level_up.*` 로, 3.6 코드 블록과 "계단 보상" 의 `OpenAt` 을 위 표의 두 시그니처로 |
| 낮음 | 좁은 통로에서 `Row` 가 늘 `clearance` 30 으로 당겨 덤 보상·정예 상자가 한 점에 겹침 | `Find(…, out threshold)`·`Row(…, threshold)`, `RoomFlow.BeginReward` 가 넘김. 시험 `NarrowCorridor_EliteRoom_RowUsesFindThreshold_NoOverlap_V082` |
| 낮음 | 모양 지문이 열거형 값 이름을 안 봐 `StatKey` 값을 없애거나 바꿔도 지문 그대로 | 닿는 열거형의 값 JSON 이름을 지문에 넣음. 지문 `3d03c6b8` → `7c149bd4`, `CurrentFormat` 1 그대로(저장된 판이 아직 없음) |
| 낮음 | `Suspend` ⓐ 의 한 틱 뒤 예외면 월드가 이미 한 틱 진행됨 | 코드는 그대로. 11.1 WP6 v0.8.2 에 메뉴가 예외를 받아 `저장 못 함` 한 줄로 판을 이어 가게 적음 |
| 낮음 | 커밋 준비(`git log --grep WP1f` 참조·설계 v0.8/v0.8.1 본문·리뷰 문서 2개·기획서·brief 변경) | 문서 변경 없음 — 커밋 제목·본문에 `WP1f` 를 넣고, 함께 넣을 파일은 메인 세션·사용자가 정함 |

### 13.23 v0.9 — 기획서 v0.9(33절, 사용자 결정 V1~V5)·v0.9.1(34절, V6~V12) 받기와 WP1g

단계 1 통합 뒤 첫 플레이(2026-11-11)에서 사용자가 정한 열두 가지와 레벨업 아이콘 관찰을 받았다. 새 계약 모양은 6.19(장비·상인·물약 칸·회복 문·둘레 아이콘·겉모습·가져오기 표)이고, 계약 변경과 단계 1 이 쌓은 요청은 11.1 **WP1g** 한 묶음에 모았다(순서·소유 11.3).

**기획서 33절·34절 "설계에 넘기는 것" 처리**

| 기획 # | 내용 | 처리 | 자리 |
|---|---|---|---|
| 33-1 | 장비 계약 — 정의·칸 여섯·짐·계단 저장·합산 순서(방어 상한은 합산 뒤)·최대 체력 바뀜 | 반영 | 6.19 정의·인스턴스·합산, WP1g #2·#4·#5·#6·#7 |
| 33-2 | 문 보상 — 회복 문(방 안 물체 없음)·상인, 거르기·보장의 결정성 | 반영 — `RewardKind.HealNextRoom`·`Merchant`, 문이 열리는 틱 상태의 함수 + 난수 수 고정 | 6.19 "회복 문", WP1g #1·#3·#11 |
| 33-3 | 회복 문 적용 시점 — 들어옴 첫 틱, 계단 다음은 회복 문 아님 | 반영 — `RunState.HealOnEnter`, `Suspend` 가 거짓 단언 | 6.19, WP1g #7·#11 |
| 33-4 | 상인 창 상태·구매 명령의 결정론 | 반영 — 들어오는 틱에 상인 전용 난수 흐름(방 난수 안 씀), 사기·다시 굴리기·떠나기·반지 바꾸기 = `Choice1~3`·`Reroll`·`Skip` 판 명령 | 6.19 "상인 방 상태와 구매 명령", WP1g #8·#9·#10 |
| 33-5 | 금화 경제 상태 — 지갑·쓴 금화·상인 방 횟수·만난 상인 | 반영 — 지갑 `HeroCarry.Gold`, `RunStats.GoldSpent`, 액트 횟수는 `RunState`(액트마다 0), 만난 상인은 스냅숏까지 | WP1g #6·#7·#8 |
| 33-6 | 맵 풀 데이터 — 액트마다 작은 방 5장 이상·덱 순서 | 데이터는 방 맵 브랜치(`maps/act1`)·단계 2 데이터, 덱 순서는 3.6 결정 2(이미 있는 뽑기 — 바로 앞 쌍 빼기)를 "액트 시작 때 섞은 순서" 로 바꾸는 일 | **남김** — 방 맵 브랜치가 합쳐진 뒤 WP4 가 `RunPlanner` ① 을 고침(계약 아님 — 뽑기 순서는 판 결과라 시험 기대 값만 바뀜) |
| 33-7 | 가져오기 표 — 상인 그림·대사·인벤토리 그림·장비 소리 | 반영 | 6.19 가져오기 표, WP1g #16(표 모양), 실제 줄 WP0 |
| 33-8 | `ROOM` 로그 `hp_in`·`kills`·`gold`·`gold_spent`·`shop`, 자동 조작 상인 창·문 우선순위 | 로그 열은 WP7(계약 아님 — `RoomRecord` 에 `HpIn`·`GoldSpent`·`Shop` 을 더하는 것은 WP1g #6 과 같은 커밋), 자동 조작은 WP4 | WP1g #6·하지 않는 것 |
| 34-9 | 물약 칸 상태·마시기 명령·서서히 회복·저절로 마심 삭제 | 반영 — `CommandButtons.Drink`, `Hero.Heal` 틱마다 같은 양(끝 틱 나머지), 판 시간 멈추면 멈춤, 계단 저장 때 바로 채움 | 6.19 "물약 칸", WP1g #3·#6·#12·#13·#15 |
| 34-10 | 장비 정의 보탬 — 무게·접사 범위·접사 둘·인스턴스·무게 이동 표 | 반영 | 6.19, WP1g #2·#3·#4·#5 |
| 34-11 | 겉모습 고르기 — 순수 함수, 못 가져온 벌은 `slt` | 반영 — `HeroLook.Key`, `HeroClassDef.Looks`·`Look` | 6.19, WP1g #4·#14 |
| 34-12 | 둘레 아이콘 — 상자를 여는 순간 정한 무기의 결정성, 상자 → 책 두 단계 | 반영 — 여는 틱에 방 난수 `Drop` 으로 정해 `RewardPickup.Chosen`, 책을 집는 틱에 다시 뽑지 않음 | 6.19 "둘레 아이콘", WP1g #1·#9·#13 |
| 34-13 | 페핀 — `reward_healer`, 고정 물건 셋, 다시 굴리기 없음 | 반영 — `MerchantStock.Potions`·`PotionUpgradeDef`·`Rerollable` 거짓 | 6.19, WP1g #2·#10·#11 |
| 34-14 | 가져오기 표 — spelicon·panel8·p8bulbs·items·towners·아홉 벌 | 반영 | 6.19 가져오기 표, WP1g #16 |
| 34-15 | `ROOM` `potions`·`drunk`, 자동 조작 물약 35%·페핀 창 | 로그 WP7, 자동 조작 WP4(`rules.hero.potions.bot_threshold`) | WP1g 하지 않는 것 |
| 관찰 | 레벨업 아이콘이 거의 검게 보임(`Table/icons.json` 이 투사체 CL2 한 프레임을 48px 로 줄여 만듦) | 원작 `ctrlpan\spelicon.cel`(56×56, 프레임 머리 없는 CEL)로 — WP0 이 표를 바꾸고, 대체 아이콘은 가산으로 구움. 계약 변경 없음(그림 경로·크기만) | 6.19 가져오기 표, 11.3 |
| 관찰 | "키 입력이 이상하다"(무엇인지 미확인) | 버그 수정 담당이 재현. 창 닫힌 뒤 입력 새기가 원인이면 WP6 `InputMap` 의 잠금(기획 3절 첫 안 0.2초) — 계약 아님 | 기획 열린 질문 50 |
| 35-16~18 (v0.9.1 — 기획 v0.9.2, 열린 질문 50~53 답) | 방어 상한 10 → 15, 지팡이 + 방패 함께(양손 규칙 없음 — 둘 다면 겉모습 `t`), 아이콘 56×56 그대로. 물약 키 `Q`·물약 양 첫 값은 확정만(계약 그대로) | 반영 — `armor_cap` 15·`EquipStats` 시험 기대 값, 끼우기가 다른 칸을 비우지 않음(상인 카드에 빠지는 칸 없음), `HeroLook.Key` 는 이미 둘 다 → `t`. 위 "관찰" 줄의 가산 굽기는 하지 않음 — 원인이 굽기 버그였다(`9128cc9`). 같은 판에 13장 오타 둘(WP5 기록의 `tools\import-all.ps1` 경로, 작은 방 A 표 행의 남는 칸) 고침 | 6.19 합산·끼우기·겉모습·가져오기 표, 1.4·6.8 아이콘, WP1g #5 시험, WP0 v0.9 줄 |

**다르게 해석·보탠 것**(설계가 정함 — 기획서에 넘기는 것)

1. 상인 카드 최대 셋 → `CommandButtons` 에 `Choice4` 를 더하지 않았다(기획 v0.9 의 아드리아 넷째 칸이 v0.9.1 에 페핀으로 갔다). 카드가 넷 이상인 상인을 더하면 그때 계약.
2. 상인 물건은 **상인 전용 난수 흐름**으로 정한다(기획 11.7 "방 난수" 를 좁힘) — 상인 방이든 아니든 그 방의 적·드롭이 같아야 시험·재현이 쉽다. 같은 씨앗 = 같은 물건이라는 기획 문장은 그대로 선다.
3. 보장으로 정한 문도 뽑기 난수를 그대로 소비한다(난수 수 고정 — 3.6). 기획에는 없던 규칙이지만 보이는 결과는 같다.
4. 계단 저장 `Format` 1 → 2: 옛 `run.json` 은 불일치로 지워진다(8.3 — 판 하나를 잃음). 단계 1 은 계단 저장을 쓰지 않으므로 사용자 판은 잃지 않는다.
5. 지갑(`HeroCarry.Gold`)과 얻은 금화(`RunStats.Gold`)를 나눴다 — 기획 11.7 "쓴 금화도 얻은 금화에 센다" 와 결과 화면 `얻은 금화 · 쓴 금화` 를 그대로 받는 모양.

**기획서에 넘기는 것**: 위 1~3 — 기획서 v0.9.1 이 같은 날 받았다(11.4 보장의 난수 수, 11.7 상인 전용 흐름, 12.8 카드 셋, 34절 끝). 33-6 의 덱 순서와 3.6 결정 2(바로 앞 쌍 빼기)의 합침은 기획 10.8 v0.9 가 이미 정했다(다 쓰면 다시 섞되 바로 앞 방 맵을 맨 앞에 두지 않음) — WP4 가 그대로 구현.

### WP7 구현 기록

WP7(앱 셸·데이터 로더·개발 도구·자원 검사, 단계 1) 코드가 문서와 다르게 또는 더 정한 것. 코드가 정본이다(11.4 와 같은 규칙).
게임 창을 띄우는 완료 기준(빈 씬 캡처·`HiDef` 큰 텍스처·저장 시각·포그라운드 창·빈 자원 exe·표 바꾼 exe·최소화 창 캡처 R8)은 이 묶음에서 확인하지 않았다 — 합친 뒤 검증 담당이 본다.
창 없이 도는 길(`--validate-data` 0, 모르는 옵션 64, 자원 없는 빌드의 `--autoplay` 5 와 `ASSETS`·`AUDIO device="No Output"` 줄)은 빌드한 exe 로 확인했다.

| 자리 | 문서 | 코드 | 이유 |
|---|---|---|---|
| 9절 시작 판정 | `GameApp` 이 판정 | 창을 만들기 전 `App/Boot.Prepare`: 데이터 → 첫 줄 → 저장(`SAVE` 줄은 첫 줄 뒤로 미룸) → `AUDIO device=` → 자원 검사 → `DevOptions.CheckData`. 개발 옵션 실행의 데이터 오류 2·자원 5·옵션 64/5 는 창 없이 끝남 | 자동 실행이 창을 띄웠다 닫지 않게, 첫 줄이 늘 맨 앞에 |
| 9절 "자동 실행에서 데이터 오류" | 자동 실행 | 개발 옵션 실행이면 모두(캡처만 준 실행 포함) 2 로 끝남 | 캡처로 DataError 화면을 보는 길은 문서에 없음 — 입력을 기다리며 멈추지 않는 쪽으로 |
| 1.4 끝 안내 화면 | DataError 화면 재사용 | 자원 문제 줄과 가져오기 명령 한 줄을 `DataError("Assets", "$", …)` 로 `AppServices.DataErrors` 에 넣어 `SceneFactory.CreateDataError` 를 부름. 캡처만 준 실행은 `--quit-after` 가 없어도 캡처를 다 찍으면 5 로 끝남 | 화면 쪽 계약을 늘리지 않음. 감시(4)까지 기다리지 않게 |
| 3.4 첫 줄 | `seed config commit data args` (+ `hero`, `start`, `room`) | 순서 `seed= config= commit= data= hero= start= room= args="…"`. `data` = `Data/**/*.json` 을 경로 서수 순으로 경로·길이·바이트 SHA-256 앞 8바이트(16진). `commit` 은 `AssemblyInformationalVersion` 의 `+` 뒤 앞 12자(없으면 칸 없음). `--seed` 가 없으면 시각에서 splitmix64 | `Core/Dev/RunHeader` 로 시험 |
| 9절 캡처 | 그 틱에 멈춰 그린 화면 | `Core/Dev/DevSchedule`: 앱 초 → 틱 반올림, `--quit-after` 보다 늦은 캡처가 있으면 끝 틱을 그 캡처까지, 안 생긴 캡처(시각마다·`--capture-on` 하나)면 3. 게임은 백 버퍼(`GetBackBufferData`)를 알파 255 로 PNG. 로그 `CAPTURE path= app_tick= at=` | 그래픽 없는 판정은 Core 에서 시험 |
| 9절 감시 | 마지막 앱 틱·씬·스택 | `TIMEOUT wall_s= limit_s= app_tick= dropped_ticks= scene= stack=[…]` → `EXIT code=4` 후 감시 스레드에서 `Environment.Exit(4)` | — |
| 9절 고정 로그 | — | 더함: `GRAPHICS profile= backbuffer= vsync=`(HiDef 확인용), `DEV captures= on_event= quit_tick=`, 끝 `FRAMES app_tick= dropped_ticks=`, `--log-ticks` 면 `PERF …`, 예외는 `CRASH <스택>`, 자원 없는 다른 묶음 본문은 `WARN SHELL <함수> 본문 없음` | 판정·디버깅용, 고정 문장과 겹치지 않음 |
| 7.9 `AUDIO device=` | 개발 옵션 실행 로그에 `No Output` | `Boot` 가 판정값(`ALSOFT_DRIVERS=null` 을 둘지)으로 쓴다: `AUDIO device="No Output"` 또는 `AUDIO device="default" muted=0\|1`. 장치를 다시 만들면 `fallback=1` 줄 하나 더 | MonoGame 이 연 장치 이름을 묻는 공개 API 가 없음 |
| 7.9 다시 만들기 | 장치 초기화 예외 | `new GameApp()` 예외만 잡아 한 번 다시 만든다. `Run()` 안에서 처음 소리를 열 때의 예외는 `CRASH` → 1 | MonoGame 이 장치를 여는 시점이 플랫폼마다 다름 — R21 첫 실행에서 확인할 것 |
| 단계 1 빈 씬 | 빈 시험 씬으로 창 | `SceneFactory`·`RenderServices` 가 `NotImplementedException` 이면 `BlankScene`(어두운 갈색 한 색)·`ShellRenderContext`(화면 좌표 그대로) 로 뜸. 합친 뒤에는 진짜 씬이 뜨므로 빈 씬을 고르는 옵션은 두지 않음 | 옵션을 늘리지 않음 |
| 10.3 `TickProfiler` | 열 목록 | 단계 열 이름 = `SimPhase` 이름 snake_case + `_ms`(17개). `--check` 열은 아직 없음. 요약은 앞 5초를 뺀 frame·tick·draw_calls 의 nearest-rank p50/p95/p99/max | — |
| 11.1 WP7 시험 `Fixtures/data/` | 고장 난 JSON 파일 | 파일 대신 시험 안의 `DataFile` 목록(`LoaderFixture`)으로 고장 하나씩 | 시험 한 곳에서 입력과 기대를 같이 봄 |
| 6.14 `rules.rooms` 없음 오류 | 늘 | 스테이지가 있을 때만(`rules.sight`·`rules.hero` 도 같음) | 스테이지가 없는 지금 실제 데이터가 막힘 |
| 8.3 `run.json` | `RunSnapshot` | 감싸개 `{ version: 1, run: … }`, 위치 record 인자가 빠지면 `.bad`(STJ 가 기본값으로 채우므로 키를 직접 봄) | `RunSnapshot` 이 모르는 속성을 오류로 봄 |

**남은 것**(계약에 길이 없어 잇지 못한 것 — 아래 WP1g 요청)
1. ~~사람 입력과 자동 조작 메뉴 넘김을 `FrameLoop` 에 넘기는 길~~ — **통합 때 이음**: `GameApp.Update` 가 `InputMap.Sample`(WP6)로 `readInput` 을 채우고(자동 조작이면 null), 앱 틱마다 `FixedTick` 뒤 `GameFlow.Of(app).Autoplay?.Step(stack, t)`(`FrameLoop.Ticked`). `GameFlow.QuitRequested` → 창 닫기, 개발 실행이 아니면 `SettingsChanged`·F11 → 전체 화면, 창 비활성 → `RequestPause`(3.3). `KeyboardMouseSource` 는 `PlayScene` 이 만든다.
2. `--capture-on`·`--exit-on-result`·`RESULT`: 씬이 "그 일이 일어나 화면이 다 열림"·"결과 화면 열림" 을 앱에 알리는 길. 지금은 `--capture-on` 캡처가 늘 "안 생김"(3).
3. `--log-ticks` 의 월드 쪽 열(`stage_s`·적·투사체·장판 수·단계 시간)과 `--log-hash`: 판을 가진 씬이 `Simulation.Profiler`·월드를 앱에 보이는 길. 지금은 0.
4. 꺼진 묶음(`ContentAvailability`)을 씬에 넘기는 길 — **통합 때 이음**: `Program` 이 `BootResult.Assets.Availability` 를 `GameApp` 에 넘기고 `SceneFactory.CreateStart(app, content)`(WP6)로 준다. `AppServices` 칸(WP1g 요청)은 그대로 남음.

**WP1g 요청**: `AppServices` 에 `ContentAvailability Availability`·`DisabledContent Disabled`(자원 검사 결과), 앱 개발 실행 훅 `IDevHooks`(`CaptureEvent(string kind)` — 화면이 다 열린 프레임, `ResultShown(RunResult)` — `--exit-on-result`, `AttachProfiler(Simulation)`·`HashRow(World, RunState)` — `--log-ticks`·`--log-hash`),
`SceneFactory.CreateInput(AppServices) → IInputSource?`(자동 조작이면 null)·`SceneFactory.CreateAutoplayDriver(AppServices) → IAutoplayDriver?` 또는 같은 뜻의 연결 파일 함수. `RunSnapshot` 위치 인자의 `required`(STJ `JsonRequired`) — 지금은 `SaveBook` 이 키를 직접 본다.

### WP2 구현 기록

WP2(길찾기·충돌·공간·시야 소유) 코드가 문서와 다르게 또는 더 정한 것. 코드가 정본이다(11.4 와 같은 규칙). 계약 파일·실제 `Data/` 는 고치지 않았다. 커밋은 `git log --grep WP2`.
파일: Core `Geometry/Geo.cs`, `Nav/`(`ArenaCollision` 본문·`NavConst`·`NavGrid`·`ObstacleIndex`·`FlowField`·`GridAStar`·`IndexedMinHeap`·`NavWorld`·`PlacementResolver` 본문·`NavModule`·`Validation/PathChecks`·`Validation/FlowRadiusValidator`),
`Spatial/SpatialHash.cs`, `Sim/Movement/`(`Navigator`·`MovementSystems`·`Enemy.Nav.cs`·`World.Movement.cs`·`Invariants/InvariantChecker`), 시험 `Geometry/ Nav/ Spatial/ Movement/ Invariants/`.

| 자리 | 문서 | 코드 | 이유 |
|---|---|---|---|
| 4.2 도형 만들기 1 | 막힘 종류마다(걷기 칸 묶음·투사체 칸 묶음) 탐욕 직사각형 | **칸 종류**(걷기·투사체·둘레 세 비트가 같은 칸)마다 탐욕 직사각형. `X` 칸은 두 플래그를 다 가진 도형 하나, `w`·`W` 는 걷기만, `m`·`M` 은 투사체만 | 같은 칸에 도형 둘이 겹치지 않고, 둘레 칸과 안쪽 칸이 한 도형에 섞이지 않아 `IsBoundary` 가 칸 그대로다. 합집합 = 막힌 칸 합집합(점 표본 시험) |
| 4.2 clearance | 칸 중심 부호 거리 | `min(바깥 마름모 부호 거리, 걷기 막힘 도형 부호 거리 최솟값)`. 도형 안의 깊이는 그 도형 하나 기준(합집합 깊이보다 작을 수 있음). 모든 도형을 외접 상자 거리로 거름 — 색인 칸 반경 제한 없음 | 정확한 값이 싸다(설치 N 40 2.7ms). 맵 밖은 경계 거리 그대로 |
| 4.3 방향 읽기 | 4칸 이중선형 기울기, 못 가는 칸이 섞이면 자기 칸 `dir` | 더함: 보간 기울기가 자기 칸 `dir` 과 **60° 넘게 엇갈리면** 자기 칸 `dir`(`FlowField.SaddleCos`) | 벽 모서리의 거리 안장에서 보간 기울기가 제자리를 맴돌게 했다(실제 맵 점 유닛 16칸이 멈춤 — 시험 (b)) |
| 4.3 끼임 | "공격 사거리 밖이면" 비켜 감 | 사거리 = 두 몸 반경 + `Navigator.ReachMargin`(40) 근사. 비켜 가기 방향은 `Rng.Ai` | 부품 사거리는 WP4 가 안다 — 내비는 의도만 받는다(4.4 끝). `[해석]` |
| 4.3·4.4 `phase_through` | 장애물 통과 유닛의 길 | `SeekHero` 면 필드·A* 없이 주인공 쪽 직선(안쪽 장애물은 충돌이 건너뛰고 둘레·경계는 막음) | 필드는 안쪽 장애물을 막힘으로 본다 |
| 4.4 목표 칸 | 목표에 못 들어가면 h 최소 칸까지 | 목표 칸이 막혔으면 먼저 3칸 안 가장 가까운 통과 칸을 목표로(없으면 그 칸 그대로 → h 최소). 시작 칸도 같은 3칸 규칙. 펼침 상한 `NavConst.PathExpandLimit` 2만 | 시작 쪽 규칙(4.4)과 같은 범위. 주인공이 큰 몸이 못 서는 벽 곁에 있어도 경로가 그 앞까지 간다 |
| 4.5 길 트임 | 4px 간격 이중선형 clearance ≥ r | 그대로(양 끝 포함). 상자 밖 점은 경계 거리 | — |
| 4.6 주인공 | (적만 적힘) | `Locomotion` 이 주인공도 적분: `Hero.Intent` 가 `Heading`·`Straight` 면 `Dir` 정규화 × `MoveSpeed` × `SpeedMul`, `SeekPoint`·`Away` 는 점 쪽·반대, 그 밖 0. 가속 제한·미끄러짐 없음(벽은 충돌이 밀어 미끄러짐이 생김). 넉백 중이면 넉백 속도 | `Hero.Pos·Vel` 쓰기가 WP2(11.2). 주인공은 즉답이 맞다 |
| 4.6 분리·가속 | 모든 이동 | `Straight`(입장·돌진) 의도는 분리·가속 제한을 건너뜀, 입장 중은 가속 제한 없음. 분리는 해시에 든(조준 가능) 적만 | "미끄러짐 없는 직선" 을 지킴 |
| 4.6-4 넉백 | `KnockbackVel` 을 0.12s 동안 | `Locomotion` 이 그동안 희망 속도 **대신** 넉백 속도로 움직이고 `KnockbackTime` 을 줄인다(0 이면 `KnockbackVel` 도 0). WP3 은 쓰기만 | 적분하는 곳이 시간도 줄여야 두 번 줄지 않는다 |
| 4.7 막힘 시간 | "희망 방향으로 실제 이동이 막힘" | 충돌 뒤 `(Pos − PrevPos)·희망 방향` 이 기대(희망 속도 × Dt)의 25% 미만이면 `MoveBlockedTime += Dt`, 아니면 0. 희망 속도 0·넉백 중이면 0 | 문턱 한 곳(`CollisionSystem.BlockedProgress`) |
| 4.7-1 적-적 | 해시에서 쌍 | `Locomotion` 이 지은 해시를 다시 쓴다(질의 칸 범위를 `SpatialHash.Slack` 8px 넓힘, 거리는 지금 위치로) | 한 틱 이동(2~6px)은 여유 안. 해시 짓기는 6·8 단계 두 번 그대로(3.5) |
| 4.7-2 단단한 적 | 중간 보스·보스·반경 20 이상 | `Def.Rank != Normal` 또는 반경 ≥ 20(`CollisionSystem.HardRadius`) | 정의 등급 = 중간 보스·보스 |
| 4.7-3 입장·등장 | 입장 중은 장애물·경계를 건너뜀 | 등장 중(`Emerging`)은 장애물·경계에 밀린다(해시·분리에서만 빠짐) | 등장은 맵 안 자리에서 솟는다 |
| 4.8 해시 | 외곽 상자 칸 | 상자 밖 자리는 가장자리 칸에 넣는다(질의 범위도 자름). `MaxRadius`(넣은 적 중 최대 반경)를 같이 냄 | 입장 중이 아니어도 밀려 상자 밖에 잠깐 있는 적을 잃지 않게 |
| 4.10 `Throw` | `HasClearLine` 으로 닿는 곳까지 | 4px 걸음으로 clearance ≥ 몸 반경인 마지막 자리(처음부터 막히면 제자리) | 같은 뜻을 걸음으로 |
| 4.10 `PathLengthToHero` | 필드 등급·통과 칸이면 필드 거리 | + 그 필드가 지금 주인공 칸으로 계산된 것일 때만(아니면 자른 A*). 시작점 칸은 직선 거리 | 주인공이 옮긴 뒤 갱신 전(최대 6틱) 낡은 필드 거리를 쓰지 않게 |
| 6.14 13.5-4 | 도형 묶음끼리·경계와 80px 이상 또는 붙음 | 걷기 막힘 도형을 "붙음"(거리 ≤ 0.5px)으로 묶음. **둘레 묶음**(둘레 도형을 품거나 바깥 마름모에 붙은 묶음)끼리는 안 봄. 둘레가 아닌 묶음 ↔ 바깥 마름모도 봄 | 둘레끼리는 같은 경계다. 실제 맵·6.11 예 합성 맵 통과, T 자 접합(세로 조각만) 오류·모서리 조각 통과 |
| 6.14 13.5-5 기준점 | 입구 진입점 → `center`·`room_*` 길(16·26·34) | **기준점 칸에서 3칸(체비쇼프) 안에 닿는 칸**이 있으면 닿음 | 실제 `room_south`(31,31)는 기둥(30,32)에서 약 32px 라 34 몸이 그 칸에 정확히 못 선다 — A* 가 "갈 수 있는 가장 가까운 곳" 까지 가는 것과 같은 뜻. `[해석]` |
| 6.14 13.5-5 나머지 | "도달 칸", S 필드 시작점 | "주인공 시작 → 모든 입구 진입점" 만(도달 칸은 갇힌 바닥 칸 검사가 덮음). S 필드 시작점은 BFS 대체 없이 "(14 + 16)px 안 통과 칸 ≥ 1" 을 직접 봄 | BFS 대체가 있으면 늘 참이라 검사가 아니다 |
| 6.14 13.5-6·작은 방 | 60px·150px | 같은 함수 `PathChecks.InnerDistance`(둘레 아닌 걷기 도형까지 실제 거리), 작은 방(방 맵·N ≤ 36)이면 150px 문턱 | 11.1 WP2 v0.7 "같은 함수에 거리만" |
| 6.14 오류 위치 | 파일·경로 | 길 검사 = `maps/<원판 ID>.json` `$`, 거울판은 문구 앞 `(거울판 <id>@m)`. flow 몸 경고 = 파일 `enemies` 경로 `$.enemies[<id>].radius`(경고) | 정의에 원본 파일 이름이 없다 — WP7 로더가 고쳐 붙일 수 있다 |
| 13.13 넘긴 것 2 | "14px 안" 이 실제 L1 벽 모서리에서 거짓 오류를 내는지 | 실제 `cathedral_arena`·6.11 예 합성 맵에서 갇힌 바닥 칸 0 — 기준을 넓히지 않았다 | — |
| 10.1 `Nav` (b) | 10초 안에 120px | 한계 = max(10초, 필드 길 길이 × 1.5 / 70px/s + 2초) | 2560px 맵의 먼 칸은 길이 1,300px 를 넘어 해골 걸음으로 10초에 못 온다. 시험의 뜻은 "맴돌지 않음" |
| 10.1 `InvariantChecker` | `SpatialRebuild` 안 60틱마다 | `World.Options.Check` 또는 `InvariantChecker.Enabled` 일 때 `Tick % 60 == 0`. 결과 = `World.Invariants`(`InvariantReport` — World 조각 `Sim/Movement/World.Movement.cs`, 방 수명). 판 합은 부르는 쪽이 방마다 `InvariantReport.AddTo`. `stuck` 은 3표본(3초) 간격으로 기준 자리와 비교, 제외 거리 = 두 반경 + 40 + 32, `Away`·`Hold`·`Straight` 의도는 뺌. `occluded` 는 일반 정의 등급·입장·등장 중 아님·`Visibility.Enabled`. 공정성 네 열은 `World.Fairness` 를 줄(`Format`)에만 붙임 | `RunState` 에 칸이 없다(계약 — WP1g 요청). 판 수명 상태를 모듈 인스턴스에 두지 않는다(11.2) |
| 4.9 측정 | 적 500 플로우 3,600틱 | `Perf` 시험 `MovementPerfTests`: 실제 `cathedral_arena`, 주인공이 가운데 둘레를 천천히 돎(필드 갱신 틱 포함). Release 이 PC: Navigation 0.036 + Locomotion 0.167 + Collision 0.169 = **0.371ms**(SpatialRebuild 0.006), 할당 0B, 불변식(`stuck_max` 4/500). 500 은 절대 상한 360 을 일부러 넘으므로 `over_cap` 은 안 봄. 설치(`NavWorld.Build`) N 40 2.7ms·N 32 1.6ms | 주인공 정지면 500 이 한 점에 굳어 `stuck` 이 기준을 넘는다(뒤 줄이 못 움직임 — 내비 고장이 아님) |
| `INavDebugView.Arena` | 맵 격자 | 맵 없는 시험 월드(상자 내비)에서는 null | 형은 계약(null 불가 표시) — WP1g 요청 |

**남은 것**
1. 스테이지 2 실제 맵(협로 포함)의 (a)~(d) — 단계 3(맵이 생기면 `RealMapNavTests` 를 그 맵으로 한 번 더 — (a)~(c) 본문은 `Nav/NavExhaustive` 한 곳). 지금은 합성 맵·합성 액트 풀로 대신: 작은 방 N 32 세 맵(액트 풀 `test_room_a`·그 거울판·안쪽 방 하나와 기둥을 둔 맵)의 (a)~(d) 는 `SmallRoomNavTests`, 맵 ⑤ 모서리 구멍 → 13.5-4 는 `SyntheticMapNavTests`.
2. R23 입구 대체 비율(작은 방 32) — 감독 생성 규칙(WP4 `Director`)이 있어야 잰다. 못 함.
3. 단계 1 시나리오의 불변식·공정성 열 0(액트 1 실행) — WP4 `Scenario` 가 `RunOptions.Check` 로 켜고 방마다 `World.Invariants` 를 합친다.
4. `--debug-nav`·창으로 보는 확인 — 확인 못 함(합친 뒤, WP5).

### WP3 구현 기록

WP3(주인공·전투·무기·성장) 구현이 문서와 다르게 정했거나 문서에 없던 것을 정한 자리. 계약 형을 바꿔야 하는 것은 "WP1g 요청" 으로 모았다(WP3 은 계약 파일을 고치지 않았다).

| 자리 | 정한 것 | 까닭 |
|---|---|---|
| 공정성 `hidden_hits`(10.1) | 주인공 피해 요청 중 "이번 틱 `Projectiles` 단계 전에 들어온 적 쪽 요청" 을 적 본체(근접·자폭·돌진) 피해로 보고 센다(`DamageResolveSystem.CountHiddenHit`, 중간 보스·보스 제외) | 피해 요청(`DamageRequest`)에 원인 종류 칸이 없다(11.1 WP3 v0.5.1 "원인 종류가 있어야 가른다"). WP4 가 `Projectiles` 단계 뒤에 적 쪽 요청을 넣으면 틀어진다 — **WP1g 요청**: 요청에 원인(본체·투사체·장판·예고) |
| `WeaponPatternState.Combat.cs` 조각 | 두지 않는다. 궤도 구체 수는 살아 있는 투사체를 세어 보고, 나머지는 계약 struct 의 필드(`BurstLeft`·`BurstTimer`·`Timer`·`Angle`·`Counter`·`Active`)로 충분 | `partial struct` 를 다른 파일에 나누면 필드 순서 경고 CS0282 가 난다(경고 0 규칙) |
| 짐의 만렙 시각 | `HeroCarrier.Restore` 가 만렙(또는 진화) 무기의 `MaxedAtTick` 을 0 으로 둔다 — 같은 값끼리는 칸 번호가 먼저 | `HeroCarry` 에 `MaxedAtTick` 칸이 없다. 방을 넘으면 "먼저 만렙이 된 무기" 순서가 칸 순서로 바뀐다(진화 우선순위·상자 고르기에 닿음) — **WP1g 요청**: `CarriedSlot` 에 만렙 순번(또는 틱) |
| 장판 틱 | "간격 확인 뒤 줄이기" — 첫 틱이 한 틱을 더 먹지 않게(장판 틱 간격 29틱 ≠ 적별 간격 30틱이라 한 번씩 빠지던 것), 적별 타격 간격 비교에 반 틱 여유 | 장판 `tick` 과 적별 타격 간격이 같은 수일 때 둘이 맞물려야 한다 |
| 시험의 다른 묶음 자리 | 적 생성(감독)·적 이동·`DeathHooks` 는 WP4 몫이라 시험 시스템으로 흉내(`FakeDeathHooks`, 방마다 서 있는 적 하나, 정예·보스 처치 흉내). 시나리오 "해골 50 + 매직 미사일 Lv8 60초" 는 해골이 걷지 않으므로 줍는 반경 안(주인공 둘레 30~55px)에 세운다 | 11.1 규칙(다른 묶음 본문에 기대는 시험은 TestKit 합성·가짜로). WP4 가 들어오면 실제 감독·`DeathHooks` 로 다시 돈다 |
| 실제 데이터 시험 | WP7 로더 전이라 실제 `Data/` 를 형만 읽고 참조는 손으로 잇는다. 부품 칸(`targeting`·`on_*`)과 투사체 `hit_effect` 는 STJ 로 못 읽어 떼어 내거나 ID 만 본다 | 로더(부품 등록부·이펙트 참조 읽기)가 WP7 몫 |

남은 것(확인 못 함 — 합친 뒤): WP2 실제 공간 해시로 시나리오 다시 돌리기, WP4 감독·`DeathHooks` 와 합친 실제 판의 끊고 잇기 같음, 게임 창에서 무기 `cast_clip`·피해 숫자·보상 창 보기.

### WP4 구현 기록

WP4(적·감독·자동 조작) 단계 1 코드가 문서와 다르게 또는 더 정한 것. 코드가 정본이다(11.4 와 같은 규칙). 계약 파일(WP1~WP1f 형)은 고치지 않았다 — 바꿀 것은 "WP1g 요청" 에 적는다.
이웃 묶음 본문(WP2 이동·충돌·`PlacementResolver`, WP3 `DamageMath`·피해·투사체·장판·드롭)이 아직 없어 시험은 `tests/…/Sim/FakeArena.cs` 가짜(직선 이동·가장 가까운 적 때리기·피해 적용·줍기·생성 세기)로 돈다.

| 자리 | 문서 | 코드 | 이유 |
|---|---|---|---|
| 6.6 `kite` 띠 당기기 | ① 다가감 ② 물러남 ③ 멈춤, 경계는 `최대'`·`최소'` | 되돌이 여유 20px(`Kite.Hysteresis`): 다가가는 중이면 `최대' − 20` 까지, 물러나는 중이면 `최소' + 20` 까지 계속. 물러나다 0.5s 못 움직이면 "물러날 수 없음" 표시(슬롯 `Timer`)를 두고 띠 최소 밖으로 나갈 때까지 제자리 | 주인공이 궁수보다 빨리 멀어질 때 띠 경계에서 매 틱 다가감·멈춤을 오가 `kite_switches` 가 기준을 넘었다(띠 폭 ≥ 40 이라 가운데에 닿는다) |
| 6.6 `self_destruct` | 폭발 뒤 자기는 사라짐 | 터진 적은 `KillCause.Culled` — 보상·시체·죽음 훅 없음 | 자기 폭발로 경험치·드롭을 주지 않게. 점화 중 `Damage` 죽음은 폭발 없이 `fizzle_effect` 만(시험) |
| 5.2 적 피해 식 | `Combat/DamageMath`(WP3) | 같은 식을 `Sim/Enemies/EnemyMath` 에 둠 | WP3 본문이 `NotImplemented`. 부르는 곳이 이 클래스 하나라 합칠 때 본문만 `DamageMath` 호출로 바꾼다 |
| 4.10 놓을 자리 | `Nav/PlacementResolver`(WP2) | 같은 규칙을 `Sim/Enemies/EnemyPlacement`(`TryPlace`·`Throw`)에 둠 | WP2 본문이 `NotImplemented`. 소환·호위·분열·정예 무리·포위 고리가 이것 하나를 부른다 — 합칠 때 호출로 바꾼다 |
| 6.6 페이즈 무적 | — | `Enemy.InvulnUntilTick` 을 WP4 조각(`Enemy.Behavior.cs`)에 둠, 가짜 피해가 지킴 | 공통 조각에 무적 필드가 없다(WP1g 요청 1). WP3 `DamageResolve` 가 읽어야 한다 |
| 9절 `--stress` | 적 수 + 투사체 수 유지 | 적만 채움(출처 `Stress`, 상한 검사 없음) | 투사체 수 유지는 WP3 무기 쪽 일이라 본문이 생기면 거기서 |
| 9절·기획 15절 `PickCard` | 진화 재료 무기 강화 > 가진 무기 강화 > … | 진화 재료 = 주인공이 그 진화의 재료 패시브를 가진 진화 전 무기. 계약 시그니처 `PickCard(offer)` 를 지키려고 정책이 마지막으로 `Next` 를 받은 월드로 판단(아직 없으면 보통 무기 강화와 같은 순위) | 카드에 진화 정보가 없다. 메뉴 오버레이는 늘 판 도중에 열리므로 월드가 이미 있다 |
| 6.12 단계 1 판 이벤트 | 기획 10.3 표 | 단계 1 에 있는 적으로만: 6번 방 행진의 폴른 원 20 → 해골 20, 해골 대장·중간 보스·보스·방 보급은 단계 2. 구간표는 없는 종(폴른 원·썩은 시체·핀드·불타는 해골 궁수) 가중을 빼고 2:00–2:30·2:30–3:00 을 한 줄로 합침, 이 판에 안 쓰이는 5:00–6:00·7:30–8:30 줄도 13.5-3(빈틈 없음) 때문에 남김 | 단계 1 세로 조각(6.12)은 적 5종뿐. 단계 2 에 전체 판으로 바꾸고 이 판은 지운다 |
| 6.7 접두사 | 광폭한·강철 피부의·불타는 | `affixes.json` 은 광폭한·강철 피부의 둘. 불타는 은 죽음 폭발 이펙트와 함께 단계 2 | 이펙트 정의(WP5)가 없다 |
| 10.1 `Enemies` 변형 픽스처 | 로더가 `extends`·`behaviors_add`·`behaviors_patch` 를 풂 | `Fixtures/variants/ash_skeleton.json` 을 시험 안 최소 풀기(`Wp4Data.ResolveVariant` — 부모 한 단계, add 는 뒤에, patch 는 같은 `id` 자리)로 실제 적 표에 넣음 | WP7 로더 전. 로더가 생기면 그것으로 읽고 이 함수는 지운다 |
| 10.1 실제 데이터 시험 | WP7 로더로 `Data/` 읽기 | `tests/…/Sim/Wp4Data.cs` 가 WP4 파일 + 실제 맵·그림 묶음·`rules.sight` 를 읽고, 다른 묶음 정의(투사체·이펙트·문 보상·획득물)는 시험 안 대역으로 연결 | 저장소 데이터에 임시 정의를 넣지 않으려고(11.3) |
| 10.1 공정성 열 | 세 열 0 | WP4 가 세는 `unseen_charges`·`kite_switches` 만 본다. `unseen_shots`·`hidden_hits`(WP3 이 셈)와 불변식 계측기(WP2)는 합친 뒤 | 가짜 이웃에는 그 계측이 없다 |
| 10.1 `Director` 포위 고리 길 거리 | 벽 너머 방 안 자리 = 길 거리 > 직선 × 1.5 → 버림 | 시험은 실제 `cathedral_arena` 진입점 넷에서 최소 거리 ≥ 300·생략 비율 로그만 — 길 거리는 `StraightNav` 가짜(벽 무시) | WP2 `Navigator.PathLength` 본문 뒤에 같은 시험을 실제 내비로 |

**단계 1 완료 기준**(11.1 WP4) — 됨: 부품·생성기·전수 연기 시험(`Enemies/PartsSmokeTests` — 등록 목록 = 시험 목록), 변형 픽스처, 결정성 30초·표시 이벤트 켬/끔(`Sim/Wp4SimTests`), 오라·함성 `include_bosses`·함성 대상(`Enemies/EnemyRulesTests`),
우두머리 썩은 시체 1.6·22(합성 — 정의는 단계 2), 실제 적의 13.5-13·실제 스테이지의 13.5-15·13.5-18 고장 픽스처(`Enemies/EnemyDataValidatorTests`), 목표 문 행진 풀림·돌진 시작 조건(`Enemies/EnemyBehaviorTests`), 궁수 시야 조건 세 경우(`L` 줄 뒤·R − 40 밖·머리가 화면 위 밖 — 대조군 포함, `SightWallTests`·`EnemyRulesTests`),
정위 궁수 떨림 ①②③(③ 은 궁수가 주인공보다 느려 "아래로 300px 넘게 따라옴" 으로 봄), 단계 1 판 `RunSession` 실행·방 들르기(`Sim/Stage1ScenarioTests`), 상한 무한 월드 생성 수·마름모 모서리 입구 대체 로그(`Director/DirectorTests`), 공정성 합성 맵 3분, 자동 조작 카드·문 우선순위(`Sim/AutoplayPolicyTests`).
못 함: 창살 너머 궁수 시나리오(주인공 투사체가 WP3), 실제 내비로 고리 길 거리. 확인 못 함 — 합친 뒤: 불변식 기준·`unseen_shots`·`hidden_hits`, 실제 이동·충돌로 도는 단계 1 판.

**WP1g 요청**
1. `Enemy` 공통 조각에 `InvulnUntilTick`(페이즈 전환 무적 끝 틱) — 지금은 WP4 조각. WP3 `DamageResolve` 가 읽는다.

**다른 묶음에 넘길 것**
- WP2: `PlacementResolver.TryPlace`·`Throw` 본문이 생기면 `EnemyPlacement` 본문을 그 호출로. 실제 `Navigator` 로 `DirectorTests.Ring_keeps_min_radius_300_from_every_real_entry` 에 길 거리 버림을 더함. `PassThroughObstacles`(phase_through) 충돌.
- WP3: `DamageMath` 본문이 생기면 `EnemyMath` 를 그 호출로. `DamageResolve` 가 `InvulnUntilTick` 을 지킴. `ground_hazard` 상한 묶음(오래된 것부터)·`telegraphed_blast` 터짐·`--stress` 투사체 수 유지. `unseen_shots`·`hidden_hits` 셈.
- WP5·WP0a: 단계 2 적(폴른 원·썩은 시체·핀드·불타는 해골 궁수·해골 대장·중간 보스·해골왕)의 그림 묶음과 정예·보스 방 맵·작은 방 풀 — 그 뒤 WP4 가 액트 1 아홉 방 판으로 바꾼다.
- WP6: `AutoplayPolicy.DoorSkips`·`RewardStalls`·`Events` 를 로그 줄로. 메뉴 오버레이가 `PickCard` 를 부르기 전에 그 판의 `Next` 가 한 번 이상 불렸어야 진화 재료 순위가 선다.
- WP7: 로더가 생기면 `Wp4Data`·`ResolveVariant` 를 로더 호출로 바꾸고, `EnemyDataValidator`(이미 `EnemyModule` 이 `AddValidator`)가 실제 읽기에서 돈다.

### WP0 구현 기록 (단계 1 — 커밋은 `git log --grep WP0`)

WP0 이 다른 묶음과 동시에 일해서, 1.4 본문에 적기로 한 실측 값과 문서와 다르게 정한 것을 이 소절 하나에 모았다. 통합 때 1.4 해당 문단으로 옮긴다.

**이 PC 실측**(단계 1 표, `--upscale` 끔, Debug 빌드, 창 없음)
- 작업 11개·파일 35개·3.6MB. 첫 실행(캐시 0) 1.4초(원본 읽기 0.1초·굽기 0.5초, 나머지는 원본 MPQ 다섯의 SHA-256), 캐시 실행 0.9초(적중 11/11). 두 번 → manifest 바이트 같음. 원본 폴더 MPQ 의 수정 시각 그대로.
  1.4 "캐시" 의 "첫 실행 수십 초 안팎" 은 단계 1 에서는 몇 초다 — 단계 2(소리·음악·이펙트 전부) 뒤 다시 잰다.
- 페이지는 모두 한 장(2페이지 이상 묶음 없음): `hero/slt` 2048×1478(759KB), `monsters/skelaxe` 2048×860·`skelbow` 2048×979·`zombie` 2048×863(변종마다 약 290~330KB), `missiles/fireba`·`holy` 512×500, `miniltng` 128×79, 아이콘 48×48 셋,
  `maps/cathedral_arena` 기둥 아틀라스 512×475·바닥 조각 13장·미니맵 160×88.
- 네 MPQ 다 있음 → 끝 보고 묶음 넷 `만듦`. `--skip-bundles all`(저장소 밖 `--out`) → 종료 코드 0·넷 `건너뜀(--skip-bundles)`·기본 항목 출력 해시 차이 0. manifest `sources` 에 묶음 MPQ 넷의 크기·SHA-256.
- 표에 없는 경로 하나(`sfx\misc\…`)를 넣은 실행 → 종료 코드 3·manifest 없음(표는 되돌림). 성질 대조(L1 안쪽 방 조각 포함) 통과.
- `audit`(원본 있음) 종료 코드 0 — `cathedral_arena` 의 DUN 배열 대조에 거짓 실패 없음, 기준 상수(바닥 아닌 칸 8)는 그대로. `verify` 0.
- `RealMpq`(`DDR_DIABDAT`·`DDR_HELLFIRE_DIR`) 8개 통과 — 묶음마다 대표 항목(`mltas.cl2`, `monk69.wav` hfmonk·hfvoice, `l5.sol`, `nkrn.cl2`, `dlvle.wav`)이 War3Net 으로 열리고 머리가 맞음.
- `--upscale realesrgan-x4plus --upscale-bin <빈 폴더>`(저장소 밖 `--out`) → 종료 코드 6·`status: fallback`·이유 `확대 프로그램 파일 없음(…)`·출력 파일 35개 해시가 확대를 끈 실행과 같음(실행 파일을 띄우지 않음).
- **도구 판이 커밋마다 바뀜**: .NET SDK 가 git 커밋 해시를 어셈블리 `InformationalVersion`(`1.0.0+<커밋>`)에 넣어, 소스가 같아도 커밋할 때마다 도구·Core 어셈블리 바이트 → 도구 판(1.4 "캐시") → 캐시 키·manifest `tool` 이 바뀐다.
  같은 빌드 안의 "두 번 → manifest 같음" 은 그대로지만, 1.4 의 "같은 소스면 같은 값" 이 서지 않고 커밋 뒤 첫 가져오기는 캐시를 못 쓴다. 고치는 곳은 공통 `Directory.Build.props`(`IncludeSourceRevisionInInformationalVersion` 거짓) — WP1g 요청.
- `contact` 견본(스크래치)을 눈으로 봄: L1 아레나 바닥·벽·아치 덧그림·창살·닫힌 문·기둥이 이음매·틈 없이 맞물림, 좀비 원색·`bluered` 걷기, 파이어볼 16방향(1번 = 아래, 시계 방향), 마법사 `cast_fire`.
  안쪽 방 하나는 피 바닥·피 벽(172·166·168·169)으로 붉게 보인다 — 맵 데이터 그대로라 WP5 가 `--preview tiles` 로 볼 일.

**다르게 정한 것**
1. 1.4 ② 투명 칸 색 채우기: 몬스터 프레임·기둥의 색 판은 **반경 없이** 판 끝까지 채운다. 반경 32(4방향 거리)로는 불투명 칸에서 먼 여백 귀퉁이에 검정이 남았다 — "여백 폭 이상" 을 가장 단순하게 지킴. 바닥 조각의 넓힌 판은 32px 덧굽기 폭 × 2 그대로.
2. 묶음 MPQ 를 열 때 War3Net 이 깨진 머리에 자기 예외형을 던져서, 메모리 부족 말고는 모두 `열 수 없음` 으로 받는다(1.4 v0.6.1 의 뜻 그대로 — 처리 안 된 예외 1 이 되지 않게).
3. `import-all.ps1`: exe 단계가 실패하면 도구를 부르지 않고 1. 도구가 2·3 으로 끝나고 `-Hellfire` 를 직접 줬으면 `-SkipBundles` 우회 한 줄을 더 보인다. Windows PowerShell 5.1 용이라 BOM 있는 UTF-8.
4. 13.5-10 ②(`ImportNamesTests`, `Integration`)는 WP7 데이터 로더 전이라 tilesets·maps 만 든 `GameDefs` 로 `DefAssetNames.For` 를 부른다 — 통합 때 실제 `Data/` 전체로 바꾸고 기본 분류로 옮긴다. ① 은 로더 없이 실제 그림 정의를 형으로 읽어 페이지 × 변종·메타·맵 그림 이름을 대조한다.

**확인 못 함**
- `--upscale realesrgan-x4plus` 실제 실행·`upscaler-check` 의 실제 장치 줄·확대 견본 눈으로 봄·`UpscaleBatchPixels`(16MP)·`UpscaleNoProgress`(2분) 첫 값 고치기 — 이번 작업 규칙상 내려받은 실행 파일을 띄우지 않았다(이 PC 의 기본 폴더에도 없음). 가짜 실행 파일로 `Process` 경로 전체와 `upscaler-check` 를 시험했다.
- 오류 모드 물려받기(`vulkan-1.dll` 없는 실행에서 창이 안 뜨는지) — 이 PC 에서 꾸밀 수 없음. `SetErrorMode` 는 띄우기 전에 부른다.
- `import-all.ps1` 실제 실행 — 원작 exe 단계가 옛 폴더를 읽어서 돌리지 않았다(구문 검사만). 도구 단계는 같은 옵션의 `import` 직접 실행으로 확인.

**남은 것**: v0.8 ③ L1 짝 조각 — `probe tiles` 로 본 원본은 35 = 하위 62·63(1부터) SOL 25·0·25·0(`w.w.`, 36 `ww..` 의 전치 — MIN 조각 배치도 26↔25 와 같은 꼴이라 기둥 칸 = 위-왼), 25 = SOL 31·0·31·0(`X.X.`, 26 `XX..` 의 전치).
  `pieces` 에 `grate_v` 35·`door_closed_v` 25, `props` 35 `X.w.`·25 `X.X.`, `marks` 에 35·25, 6.11 예와 같은 `mirror` 짝 표, 35 기둥 칸은 `design_diffs`(원작 수정판 보정 여부는 확인 못 함 — 보정이라 하지 않음)로 넣어 보면
  실제 원본 성질 대조 통과·13.5-12 짝 검사 통과(`mirror: true` 인 `cathedral_arena` 가 오류 없이 거울판을 만듦)였다. 다만 지금 `MapMirrorTests.RealCathedral_MirrorTrue_ReportsMissingPairs_GrateAndClosedDoor`(짝 표가 없음을 기대)·
  `RealMapTests.Tileset_PropsKeysAreNamedTiles_AndOriginValues`(`design_diffs` 가 빔을 기대)가 깨져서 — 둘 다 WP0 소유가 아님 — 넣지 않았다. 통합 때 두 시험을 같이 고치며 넣는다.
  — **통합 때 넣음**: 위 값 그대로 `l1.json` 에 넣고 두 시험을 고쳤다(`RealCathedral_MirrorTrue_MakesMirroredMap_NoPairErrors` — 오류 0·거울판 있음, 성질 표 시험에 35·25·`design_diffs` 하나·`marks`). 실제 `cathedral_arena` 의 `mirror` 는 켜지 않았다(맵 소유 WP5 가 정함).

### WP5 구현 기록

WP5(렌더·그림·미리보기) 단계 1 구현이 이 문서와 다르게 정했거나 문서가 비워 둔 자리를 정한 것. 커밋은 `git log --grep WP5`.

| 자리 | 정한 것 | 까닭 |
|---|---|---|
| 그리기 계산의 자리(11.1 WP5 맡는 것) | 그래픽 없는 규칙(`ArtScale`·`PresentMath`·`WallGroups`·`DrawOrder`·`WallFade`·`SpriteLayout`·`FxMath`·`LightFogMath`)을 Core `Art/` 에 두고 게임 `Rendering/` 이 부른다 | 시험 프로젝트가 게임을 참조하지 않아(1.3) 자원·창 없이 규칙을 시험하려면 Core 에 있어야 한다. 소유는 그대로 WP5 |
| 벽 무리 나누기(7.4) | 뒤·앞 이웃이 모두 막힌 칸(두 줄 벽의 바깥 줄)은 맵 앞 반(tx + ty ≥ N − 1)이면 앞 턱(5층), 뒤 반이면 뒷벽(0b 층) | 글자 그대로 "뒤 먼저" 면 앞 턱 바깥 줄이 0b 층으로 가서 그 앞의 입장 중 유닛·안쪽 줄 그림과 순서가 뒤집힌다 |
| 13.5-7 실제 자원 대조 | PNG 는 IHDR 머리글 24바이트만 읽는다. manifest 가 없거나 형식 판이 다르면 대조하지 않는다. `ArtModule` 은 실행 폴더 `Assets` 를 본다(`DDR_ASSETS` 안 읽음) | 시험에 해독 패키지를 넣지 않음. 낡은 자원은 시작 검사(WP7)·시험 건너뜀이 맡아 코드 회귀와 섞지 않음 |
| 그림 서비스(7.5) | `Sprite` 는 미리 읽은 것만(없으면 예외 — 미리 읽기 누락 버그), 자원 파일이 없는 것은 `TryGetSprite` 거짓 | 자원 없는 개발 실행(`SkipAssetCheck`)이 서게 하되, 미리 읽기 목록 누락은 바로 드러나게 |
| 커서·글꼴 | 커서는 ICO/CUR 첫 항목 BMP(1·4·8 팔레트·24·32 비트 + AND 마스크)를 직접 푼다. 글꼴은 malgun → malgunbd → arial(경고), 다 없으면 글자를 건너뜀 | 원작 커서가 8비트 팔레트라 PNG 해독기가 필요 없다 |
| 표시 이펙트 버리기(7.11) | `FxSystem.Attach`(남은 항목 버림)의 본문은 WP5 가 썼다(7.11 은 WP6 이라 적음). `WorldRenderer.Attach` 는 버리지 않고, 부르는 순서는 그대로 `PlayScene`(WP6) | `FxSystem` 이 WP5 파일(`Rendering/`)이라 — WP6 은 순서대로 부르기만 한다 |
| `--debug-nav`(9절) | `grid`·`tiles`·`entries`·`sight` 만 그린다. `flow`·`astar`·`shapes` 는 합친 뒤 WP2 가 `DebugNavOverlay` 에 더하거나 자기 겹침을 둔다 | WP2 의 길찾기·충돌 상태를 읽어야 한다 |
| `--preview fog` 의 `_light.png`(9절) | 씬은 `SaveLightPng` 만 내고, 파일 이름을 아는 캡처 쪽(WP7)이 부른다. 장면은 60틱씩, 시야만 `BeginTick` 으로 계산(적 생각·이동 없음) | 씬은 캡처 파일 이름을 모른다 |
| 음산한 성소 색조 0.6(13.20 ③) | 그림 정의(`sprites/objects.json`)에 색조 칸이 없어 물체·아이콘 쪽 `tint`(WP3 `rewards`·`pickups`)로 | 그림 정의는 원작 exe PNG 대체 그림(기획 열린 질문 32 — 원작 물체 그림 확인 전). 시트 크기는 이 PC 의 실제 크기 |
| 대성당 작은 방 A "기둥 홀"(11.1 맵 데이터 — 단계 2 데이터를 먼저) | `Data/maps/` 가 아니라 시험 고정 파일 `tests/…/Art/Maps/cathedral_room_a.json`(시험 `Art/RoomMapTests`) | `Map/RealMapTests` 가 실제 맵 목록을 `cathedral_arena` 하나로 굳혀 두어 Data 에 넣으면 그 시험이 깨지고, 그 파일은 WP5 몫이 아니다. 목록 줄이 풀리면(WP1g 요청) 파일만 옮긴다. 기획의 배치 표(10.8)가 나오면 그것에 맞춘다. — **v0.9 맵 묶음에서 `Data/maps/` 로 옮김**(아래 "작은 방 다섯") |

**작은 방 A 배치**: 메가 16(하위 32, 외곽 상자 2048×1152), `arch_hall` 둘레·문 넷(하위 14..17), 기둥 넷은 `open_center` [5,5,11,11] 바깥 귀퉁이(안에 두면 기둥 상한 2), 앞쪽 구석 Γ(모서리 + 가로·세로 벽 하나씩)는 둘레와 메가 하나 뗌.
진입점 뒤 문 4·앞 문 26, `room_kinds` normal·elite, `room_*` 기준점 없음, 원작 방을 본뜨지 않음(`// 본뜬 원작 방: 없음` — 13.5-22 대조 줄 없음). 거울판은 L1 짝 표(WP0) 뒤.
`MapDataReader` 로 오류·경고 0(13.5-4 배치 쪽·13.5-19·13.5-23 ③ 작은 방 진입점 150px). WP2 의 13.5-4(80px)·5·6 은 합친 뒤.

**작은 방 다섯**(v0.9 맵 묶음, 브랜치 `maps/act1`): `Data/maps/cathedral_room_a~e`, 모두 메가 16·`arch_hall` 둘레·문 넷·진입점 뒤 4/앞 26·`room_kinds` normal·elite·`mirror: true`·원작 방을 본뜨지 않음.
A "기둥 홀"(기둥 셋 + 앞쪽 Γ), B "긴 회랑"(가로 벽 둘이 nw ↔ se 회랑을 가름 — 거울판은 ne ↔ sw), C "안쪽 방 둘"(뒤·앞 모서리에 출입구 둘짜리 방, `room_north`·`room_south`),
D "창살 방"(좌우 모서리에 창살 낀 방, `room_east`·`room_west` — 배치가 대각선 대칭이라 거울판이 원판과 거의 같다), E "십자"(귀퉁이 ㄱ 넷).
A 의 처음 판은 통합 검증에서 13.5-4 를 어겼다(Γ [12,12] 가 앞 둘레와 하위 2칸 57px·기둥 [11,11] 과 대각 1칸 32px) — Γ 를 [11,11] 로 옮기고 그 기둥을 뺐다.
시험은 `Nav/RealRoomMapTests`(작은 방 전부 원판·거울판 — `ValidateRooms`·`NavPathValidator` 13.5-4~6·13.5-23·10.1 `Nav` 전수 (a)~(c)·문 넷 진입점·맵 풀).
`stage_cathedral` 의 `room_maps.normal` 이 이 다섯이고 `boss` 는 `cathedral_arena` 그대로(이 판엔 보스 방이 없어 방 없는 월드가 씀).
**맵 그림**: 시작 검사가 `Data/maps` 의 모든 맵(거울판 포함)의 `d1/maps/<id>/arena.json` 을 요구하므로, 이 묶음을 받은 PC 는 `tools\import-all.ps1`(또는 도구 `import`)을 다시 돌려야 게임이 뜬다(안 돌리면 데이터 오류 화면, 개발 실행은 종료 코드 2).

**남은 것**(단계 1 완료 기준 중 창이 필요한 것 — 합친 뒤 검증 담당): `--preview sprites/enemies/effects/tiles/blend/fog` 캡처 판정, `--stress 300 --freeze-ai` 렌더 p99·`draw_calls` ≤ 600(`--reveal`·안개 켬 두 번, s = 2 한 번), `vram_mb`(R7·R38),
10.2 "뒷벽·앞 턱·아치"·"예고·빛"·"안개 고정 장면"·"안개 흐름"·"안개 재현"·"방 흐름"(봉인·아이콘) 행. 기획 열린 질문 7·10·11·18 의 값 맞추기도 미리보기를 본 뒤.

### WP8 구현 기록

WP8(소리) 코드가 문서와 다르게 또는 더 정한 것. 코드가 정본이다(11.4 와 같은 규칙). 계약 파일(`Defs/AudioDefs.cs`·`Presentation.cs`·`AppServices.cs`·`IWorldBound.cs`)은 고치지 않았다.
기획상 단계 2 묶음이지만 모듈·시험·단계 1 내용의 데이터를 먼저 냈다. 게임 실행 확인(10.2 — `--sound`·`--mute` 의 `AUDIO` 수, `AUDIO device="No Output"`, 해시 같음, `slow_frames`)은 다른 묶음과 합친 뒤.

| 자리 | 문서 | 코드 | 이유 |
|---|---|---|---|
| 7.9 흐름 | `AudioSystem`(게임)이 연결 표로 소리 ID 를 고르고 `SoundMixer`(Core)가 고른다 | Core `Audio/` 에 더함: `SoundRouter`(신호·`FxEvent`·`CorpseSpawned` → `SoundRequest`), `SoundPipeline`(신호 → 고르기 → `ISoundSink` 재생 한 줄, 무음이면 재생만 끔), `HazardLoops`(장판 반복), `AudioReach`(액트 미리 읽기 목록), `MusicChoice`(곡 결정·`AUDIO music=` 줄). 게임 `AudioSystem` 은 앱 틱·재생(`SoundBank`·`MusicPlayer`)만 붙인다 | 시험 프로젝트는 게임을 참조하지 않는다(1.3) — 신호 소비(`BeltPotion` 포함)·"같은 신호 열 → 무음과 소리 켬의 `AUDIO` 수 같음"·방 이동을 그래픽 없이 시험하려고 |
| 7.9 고르기 제한 | 몬스터 맞음 0.08s·죽음 4/같은 종 0.1s | 요청의 `SoundLimit`(`MonsterHit`·`MonsterDeath`, 같은 종 = 적 ID `Group`)으로 센다(분류 이름에 기대지 않음). **보스·중간 보스**(정의 `rank`)가 낸 몬스터 소리는 분류 `boss` 이고 이 두 제한을 받지 않는다(같은 소리 간격·자리 제한은 그대로) | 보스 죽음이 몬스터 죽음 4자리에 밀려 사라지지 않게. `boss` 로 올린 소리는 분류 `offscreen` 을 따른다(정의 `offscreen` 덮어쓰기는 정의 분류일 때만) |
| 7.9 간격·시간 | 앱 틱 | `(지금 − 마지막) / 60 < 간격 − 1e-6` 이면 버림 — 0.05s = 정확히 3틱 뒤 통과, 맞음 0.08s 는 5틱(초당 최대 12). 한 프레임에 여러 스테이지 틱의 같은 소리가 오면 같은 앱 틱이라 둘째부터 간격으로 버린다 | 무음·소리 켬이 같은 묶음(`--time-scale`)이면 같은 수 |
| 7.9 자리 | 길이 색인의 끝 틱 | 색인에 없는 파일은 1틱으로 세고 `AudioCounts.MissingLength` 에 따로 센다(종료 줄에는 없음). 반복 소리는 동시 수에 들지 않는다(재생 쪽 반복 칸) | 시작 검사가 따로 잡고, 무음·소리 켬이 같은 수를 센다 |
| 7.9 좌우·위치 | `(x − 가운데) / (폭/2) × pan` | ±pan 으로 자른다(화면 밖이어도 내는 분류). 위치 없는 요청(UI·`ElitePack`·`DoorsOpened`·`RoomCleared`·`BeltPotion`)은 좌우 0·화면 밖 검사 없음 | 기획 12.7 "가장자리 ±0.5" |
| 7.9 무작위 | 게임 쪽 난수 | Core `RandomStream`(시뮬레이션과 다른 인스턴스, 씨앗 = `AppOptions.Seed` ^ 상수). 파일·높낮이는 모든 제한을 통과한 뒤에만 뽑는다 | 같은 씨앗이면 같은 고르기 |
| 6.15 `ui.room_clear` | 예에 `snd_magic` | 데이터에 걸지 않는다. `SoundRouter` 는 걸면 낸다 | 기획 12.7 v0.8 "방 정리·방 바뀜은 소리 없이" 가 6.15 예보다 새 결정 |
| 7.9 보상 창 소리 | 오버레이가 `rewards.<id>.open` | `IAudioServices.PlayUi("rewards.<보상 ID>.open")`(`.collect` 도) — 연결 표 경로 그대로 받는다 | 계약에 `PlayUi(string)` 하나뿐. WP1g 요청 ② |
| 7.9 종료 로그 | 종료 때 `AUDIO muted=… requested=…` | `AudioSystem : IDisposable` — `Dispose` 가 쓴다(`SoundPipeline.LogLine`) | 계약에 끝내기 함수가 없다. GameApp(WP7)이 `(app.Audio as IDisposable)?.Dispose()`. WP1g 요청 ① |
| 7.9 자원 위치 | — | `AudioServices.Create(app)` = 실행 폴더 `Assets`(csproj 복사 위치). 소리 색인 `d1/sound/index.json`·manifest(묶음)를 직접 읽고, 없거나 깨졌으면 경고 후 길이 1틱·묶음 모두 있음. `Create(app, assetRoot)` 판도 둠 | `AppServices` 에 자원 경로·`ContentAvailability` 가 없다. WP1g 요청 ③ |
| 7.9 장치 이름 | `AUDIO device="<장치 이름>"` | MonoGame 이 싣는 OpenAL Soft(`openal`)에 `alcGetCurrentContext` → `alcGetContextsDevice` → `alcGetString(0x1005)` 로 묻는다. 못 물으면 `ALSOFT_DRIVERS=null` 이면 `No Output`, 아니면 `unknown` | MonoGame 공개 API 에 장치 이름이 없다. 실제 이름은 실행 확인 못 함 |
| 7.9 목소리 칸 | 24개 풀 | `voices` + 8 칸. 모든 칸이 실제로 아직 소리를 내면 그 소리만 못 틀고 `AUDIO device_busy=n`(종료 때, 0 이면 안 씀) | 고르기는 길이 색인으로 세므로 장치 지연이 있으면 칸이 모자랄 수 있다 — 고르기 결과와 무관 |
| 7.9 일시정지 | 효과음 `Pause()`, 음악 50% | 나던 효과음·반복만 멈춘다. 일시정지 중에 고른 소리(메뉴 이동 등 UI)는 그대로 낸다 | 일시정지 메뉴 소리가 들려야 한다 |
| 7.9 `SoundBank` 미리 읽기 | 그 스테이지에 쓸 소리만(고른 직업 `hero.<id>` 포함) | 적 소리는 `StageReach.Enemies` 의 적만, 나머지 줄(무기·투사체·이펙트·반복·획득물·UI·보상)과 `hero` 는 **줄 전부** | `PreloadStage(StageDef)` 가 직업을 받지 않는다(직업마다 두 소리라 작음). 레벨업 후보는 미리 모른다 |
| 6.14 "소리" 검사 | 대상·소리·분류·파일 ≥ 1 | `AudioDataValidator` 가 더 본다: 파일 이름(`d1/sound/` 아래·소문자·`.wav`·상대 경로 규칙), 값 범위(`volume` ≥ 0·`max_instances` ≥ 1·`min_interval` ≥ 0·`pitch_jitter` 0..0.5), `rules.audio`(분류 중복·음량·기본 음량 0..1), 음악 `fallback` 존재, 경고: 모르는 `ui` 키·`hazard_loops` 소리가 `loop` 아님·한 번 소리 자리에 `loop` 소리. 오류 파일은 고정 이름(`audio/bindings.json`·`audio/sounds.json`·`rules/audio.json`), 비슷한 ID 제안 | 로더가 원소의 실제 파일을 넘기지 않는다(`IDataValidator` 계약). 소리 정의는 이 세 파일에만 둔다 |
| 6.15 데이터 | 예 | 단계 1 내용만(무기 셋·해골·해골 궁수·좀비 묶음·마법사·보석·금화·물약·마법서·보물 상자·UI·문·곡 둘). 주인공 소리 ID 는 `snd_sorcerer_hurt`·`_death`(6.15 "직업 단계에 `snd_<직업>_…`"). 몬스터 소리 이름 규칙(`sklax`·`sklbw`·`zombie` + `a`·`h`·`d` + `1`·`2`)은 DevilutionX 참고(출처 주석) — 원본 목록 대조는 WP0 가져오기 때 | 단계 2·3 정의가 아직 없어 줄을 걸면 실제 데이터 엄격 시험이 깨진다 |

**WP1g 요청**(계약 — 고치지 않고 자기 파일에서 우회함)
1. `IAudioServices` 에 끝내기(`IDisposable` 또는 `Shutdown()`) — 종료 `AUDIO` 줄을 쓰는 자리. 지금은 구현 형이 `IDisposable`.
2. (선택) `IAudioServices.PlayReward(RewardDef reward, bool open)` — 지금은 `PlayUi("rewards.<id>.open")` 문자열 경로.
3. `AppServices` 에 자원 폴더(또는 시작 검사가 만든 `ContentAvailability`·`SoundIndex`) — 지금은 소리 서비스가 manifest·색인을 따로 읽는다.
4. 문서만: 6.15 예의 `ui.room_clear: snd_magic` 을 기획 12.7 v0.8 에 맞춰 지우거나 "걸지 않음" 으로.

**남은 것**
1. 가져오기 소리 표 줄(WP0): `Data/audio/sounds.json` 의 효과음 파일 39개 + 곡 둘(`music\dintro.wav`·`dlvla.wav`)과 소리 색인 출력. 몬스터 소리 이름은 원본 목록으로 확인.
2. 실제 데이터의 연결 대상(다른 묶음): 무기 `magic_missile`·`fireball`·`arcane_orbit`, 획득물 `gem_blue`·`gold`·`healing_potion`, 보상 `reward_tome`·`reward_chest`, 직업 `sorcerer`(WP3), 투사체 `pj_enemy_arrow`·스테이지 `stage_cathedral`(WP4), 이펙트 `fx_fire_explosion`·`fx_bolt_hit`(WP5) — ID 가 다르면 합칠 때 `bindings.json` 을 고친다(시험 `RealAudioDataTests` 가 목록을 굳혀 둠).
3. 단계 2·3 줄: 무기 셋·진화 둘·몬스터 묶음·`special`(`set_skelbow` 블랙애시, `set_acid`)·보스 대사·`hazard_loops`(`fx_hellfire_pool` → `wallloop`)·정예 상자·물약 보상·성소·액트 2 곡.
4. 실행 확인(합친 뒤): `--sound`/`--mute` 수 같음·`device="No Output"`·`--log-hash` 같음·장면 바뀜 `slow_frames`·방 이동 앞뒤 수·`AUDIO music=` 액트마다 한 줄. 귀로 듣기는 사용자 몫.

### WP6 구현 기록

WP6(화면·UI·입력 — 11.1 WP6) 코드가 문서와 다르게 또는 더 정한 것. 코드가 정본이다(11.4 와 같은 규칙). 계약 파일(`App/Contracts/`·Core)은 고치지 않았다 — 필요한 자리는 WP6 파일 안에서 우회하고 아래 "WP1g 요청" 에 모았다.
다른 묶음 본문(WP3 성장·WP4 `AutoplayPolicy`·WP5 그리기·WP7 `GameApp`·`FrameLoop`)이 아직 없어 게임 창을 띄우는 완료 기준은 합친 뒤에 본다.

| 자리 | 문서 | 코드 | 이유 |
|---|---|---|---|
| 8.1 화면 공용 맥락 | 씬이 `AppServices` 만 받음 | `Scenes/GameFlow.cs` — `GameFlow.Of(app)`(앱 하나에 하나): `Autoplay`(`AutoplayDriver?`)·`Now`(앱 틱)·`Content`(`ContentAvailability`)·`NextSeed`·`Moment` 이벤트(`--capture-on` 이름 `levelup`·`chest`·`boss`·`result`·`doors`·`reward`)·`QuitRequested`·`SettingsChanged`·`RequestPause()`·`Canvas`(그림 서비스가 있을 때만 `UiCanvas`) | 계약 `AppServices` 를 고치지 않고 씬끼리·앱(WP7)과 넘길 것을 한 곳에 |
| 8.1 `SCENE` 로그 | "씬이 바뀔 때마다"(`SceneStack.Entered`) | 씬이 `Enter` 에서 `GameFlow.LogScene` 으로 쓴다. 이름: `Title`·`HeroSelect`·`Play`·`Result`·`DataError`·`LevelUp`·`Reward`(보상 창 — 같은 오버레이)·`Chest`·`Pause`·`Settings`. 오버레이도 쓴다. `app_tick` = 그 Push 가 일어난 앱 틱 | `Entered` 는 앱 틱을 모른다. WP7 은 `Entered` 로 `SCENE` 을 또 쓰지 않는다 |
| 9절 `SceneFactory` | `CreateStart(app)`·`CreateDataError(app)` | `CreateStart(app, ContentAvailability? content = null)`(선택 인자 — 기존 호출 그대로), 더함 `CreateAssetError(app, problems)`(자원 없음 안내 = DataError 화면 + 가져오기 명령 한 줄). `CreateStart` 가 시작 음량(`SetVolumes` — 저장에 없으면 `rules.audio.defaults`)을 넣고 `NextSeed = --seed` | 시작 검사 결과를 받을 계약 자리가 없다(WP1g 요청 1) |
| 8.1 `AutoplayDriver` | `IAutoplayDriver` 구현 | `new AutoplayDriver(AutoplayVisit)`, 앱은 앱 틱마다 `FixedTick` 뒤 `Step(stack, now)` 하나(시각 맞춤 + `stack.AutoStep`). `Ready` 의 0.5s 는 그 씬이 **처음 물어 온 앱 틱**부터. `BeginRun()` = 판마다 `AutoplayPolicy` 하나(`PlayScene.Enter` — 이어 하기도 새 판으로 셈), `PickCard` 는 그 정책 답을 카드 범위로 자름 | 씬이 들어온 앱 틱을 운전자가 따로 듣지 않아도 되게 |
| 8.1 자동 조작 명령 | "명령은 `policy.Next(world)`" | `PlayScene` 이 오버레이·어두워짐이 없는 **앱 틱마다** `Next(world)` 한 번 → `StageStepper.AppTick`(히트 스톱·늘임 틱에도 부름 — 그 명령은 버려짐) | WP4 `Scenario` 가 같은 호출 순서여야 게임과 해시가 같다(WP4 에 넘김) |
| 8.1·3.5 표시 이벤트 소비 | 프레임 끝 `FxSystem.Consume → Audio.Consume → Clear` | **스텝 끝**(`StageStep.Stepped` 마다) 같은 순서. `Advance` 앞에서 한 번 더(옛 월드 틱으로). `BossArrived` 신호를 보면 화면 신호 `boss` | 그리지 않는 앱 틱(캡처 멈춤·그래픽 없는 실행)·방 이동 앞에서 빠지거나 쌓이지 않게. `FxSystem.Consume(buffer, world.Tick)` 의 틱은 스텝 뒤 값 |
| 7.11 `IWorldBound` 순서 | `WorldRenderer → FxSystem → Hud·미니맵 → 소리` | 그 순서. 빈 연결 파일의 `WorldRenderer`·`FxSystem` 은 `IWorldBound` 가 없어 `(object)x is IWorldBound` 로 부른다(달면 저절로 불림) | WP5 파일을 고치지 않음 |
| 3.6·8.1 방 이동 어두워짐 | 0.5s 가운데 `Advance` | 길이 = `rules.rooms.transition`(고른 출구 `NextFloor` ≠ 0 이면 `transition_floor`)을 앱 틱으로 반올림(최소 2), `RoomExit` 앱 틱이 1, `총/2` 앱 틱에 `Advance` — 옛 월드 `INNER_ROOMS` → `Advance` → 새 `ROOM` 줄 → `Attach` → 액트가 바뀌면 `PreloadStage`·`PlayMusic` | — |
| 8.1 `포기` | "패배 처리" | `World.Outcome = Defeat`·`OutcomeTick = Tick` 을 앱이 직접 쓴다(앱이 Core 상태를 쓰는 유일한 곳) → 다음 앱 틱 `Finished` → `Conclude`(같은 길) | 판이 끝나는 길을 하나로 |
| 8.1 HeroSelect | "직업 단계면" | 켜진 직업 ≥ 2 이고 `--hero` 가 없고 자동 조작이 아닐 때. 자동 조작은 `ResolveHero`(9절 완료 기준 "SCENE 두 줄"). HeroSelect 의 고르기가 `SetLastHero` 를 바로 씀 | 단계 1 데이터는 직업 하나라 지금은 안 나옴 |
| 8.1 다시 하기 씨앗 | "새 씨앗" | `GameFlow.Reseed(seed)`(64비트 LCG 한 걸음, 결정적). 타이틀 새 게임도 `NextSeed` 를 쓰고 한 걸음 — 첫 판 = `--seed` | 같은 씨앗의 자동 실행이 둘째 판도 같게 |
| 8.1 결과 `RecordResult` | 결과 화면이 열릴 때 | `PlayScene` 이 `Conclude` 직후 `ResultSummary.From`(그때 `BestRecord` 를 먼저 읽음) → `ResultScene.Enter` 가 `RESULT` → `RecordResult`(직업 ID 가 빈 시험 데이터면 건너뜀) → 음악 `title` → 신호 `result`. `새 기록` 은 `RunText.IsNewRecord`(8.3 비교 키 — WP7 저장과 같은 순서를 화면에도 둠) | 저장 뒤에 읽으면 늘 "같음" |
| 12.4 도달한 곳 | 승리면 `도살자 처치` | 승리면 **그 판 마지막 액트의 마지막 방 보스 이름 + 처치**, 보스 방이 없는 판(단계 1 `slice`)은 `대성당 8 / 8 정리`. 결과 `ReachedActId` = 판이 끝난 액트(`Depth` 는 늘 지금 액트 안) | 단계 1 판에는 보스가 없다 |
| 8.1 보상 창 건너뛰기·성소 카드 | 그 보상의 `skip`, 카드에 `+2Lv · 최대 체력 −10%` | 회복량 = `World.Room.Reward.Skip.Heal`(이 방의 보상 = 주운 마법서·성소), 성소 창 카드 둘째 줄 = `RunText.ShrineLine(Room.Reward.Shrine)`(`pick_weapon` + 대가 능력치) | `RewardOffer()` 가 보상 정의를 주지 않는다(WP1g 요청 3) |
| 8.2 미니맵 지형 | `minimap.png` + 가 본 칸 | 경로 = 출력 폴더 `Assets/<map.art>/minimap.png`(없으면 어두운 판만), AI 확대 그림이면 그림 폭 ÷ 맵 폭으로 배율을 셈, 가림 = `MinimapMask`(픽셀 가운데 월드 점). 그리기는 `UiCanvas.Begin` 이 연 `RenderContext.Batch` 에 `ToScreen` 좌표로 직접 | `UiCanvas` 에 텍스처 그리기가 없다(WP5 에 넘김) |
| 8.2 체력 구슬·허리띠 | 원 마스크 + 돌 테, 허리띠 번쩍임 | 사각 채움(비율만큼 아래부터) + 테, 허리띠 칸 = `rules.hero.belt.slots`(없으면 2). `BeltPotion` 신호는 `PlayScene` 이 소비 전에 읽어 `Hud.OnBeltPotion` — 0.3s(스테이지 틱 18) 번쩍임 | `UiCanvas` 에 원·텍스처가 생기면 바꿈(WP5) |
| 8.2 HUD 경고 | 4줄 넘으면 경고 한 번 | 앱 틱에서 봄(`Hud.Tick`) — 그리기와 무관 | 그래픽 없는 실행에서도 로그가 같게 |
| 9절 `INNER_ROOMS` | v0.6.3 문장은 WP7, 11.1 WP6 v0.7 은 WP6 | WP6(`PlayScene` — 방이 끝날 때마다, 판 끝·계단 저장 포함) | 방 끝을 아는 곳이 `PlayScene` |
| 8.3 이어 하기 실패 | 불일치 예외만 | `TakeRun()` 이 null(없음·깨짐 — 파일은 저장 구현이 `.bad` 로)이어도 같은 한 줄 `이어 할 판을 읽지 못했다` + 로그 `이어 하기 못 함: …` | 사람에게는 같은 일 |
| 3절 입력 | 키 표 | `InputMap.Map`(순수 함수): Esc = `Pause \| Cancel`, P = `Pause`, Backspace = `Cancel`, Alt+Enter = `Fullscreen`(그때는 `Confirm` 아님), 숫자 패드 1~3 도. 비활성 창은 키를 읽지 않음. `KeyboardMouseSource` 는 키 이동이 0 이고 왼쪽 버튼이면 커서 쪽, 커서 6px 안이면 서 있음 | 커서 위에서 떨지 않게 |
| 8.1 `PlayScene` 생성자 | — | `PlayScene(flow, session, continued = false, IInputSource? input = null)` — 명령 출처를 바꿔 끼움(그래픽 없는 흐름 시험·다시 보기) | 자동 조작 정책(WP4 본문)이 없어도 판 흐름을 끝까지 돌려 봄 |

**확인한 것**(그래픽 없는 흐름 시험 — 저장소 밖 임시 xunit 프로젝트가 게임 프로젝트·TestKit 을 참조, 시험 프로젝트가 게임을 참조하지 않는 1.1 규칙 때문에 저장소에는 넣지 않음, WP1g 요청 4):
타이틀 Enter → Play(`SCENE` 두 줄, 음악 `title` → 액트 미리 읽기·음악), 자동 조작 타이틀 → Play 가 0.5s 뒤, 레벨업 대기가 켜진 **같은 앱 틱**에 오버레이·입력 잠금 동안 무시·고른 뒤 스테이지 틱이 멈춰 있었음,
합성 두 액트 판을 끝까지(가짜 성장·`FakeHeroCarry`) — `ROOM` 줄 수 = `RunState.Rooms`, `RESULT` 한 줄(`outcome=victory depth=` 판 깊이)·`RecordResult` 한 번·`run_time` = 방 틱 합, 방마다 새 월드에 `Attach`, 계단 뒤 액트 2 미리 읽기 한 번,
어두워짐 가운데 앱 틱에 `Advance`, 포기 → `outcome=defeat depth=1:1`, 일시정지 메뉴의 `저장하고 끝내기` 가 생존 단계에 없고 보스 방 문 단계에만 → 타이틀 `이어 하기 (합성 액트 2)` → 이어 하기(파일 지워짐, 액트 2 의 1번 방),
새 게임 때 한 번 묻고 `DiscardRun`, 불일치 저장 → 안내 한 줄, `--room 1:4` 바로 시작, 설정 음량이 rules 기본 80% 에서 10% 단위, 로그 문장 모양·`MinimapMask`·`InputMap`·`MenuList`·기록 비교 키. 22개 통과.

**확인 못 함 — 합친 뒤**: 11.1 WP6 완료 기준 전부(게임 실행 `--autoplay --seed 1 --quit-after 30`·캡처 4장·10.2 "방 흐름"·"미니맵 기억"·"계단 저장"·"단계 끝") — `GameApp`(WP7)·그리기(WP5)·정책(WP4)·성장(WP3) 본문이 필요하다. 키보드로 한 판·소리는 "동작 확인 못 함" 으로 따로.

**WP1g 요청**
1. `AppServices` 에 시작 검사 결과(`ContentAvailability`·자원 문제 목록)를 둘 자리 — 지금은 `SceneFactory.CreateStart(app, content)`·`CreateAssetError(app, problems)` 인자로 우회.
2. 화면 신호(`--capture-on`)·종료 요청·창 비활성 일시정지를 앱과 씬이 주고받는 계약 자리 — 지금은 WP6 `GameFlow` 의 이벤트로 우회(WP7 이 `GameFlow.Of(app)` 를 부름).
3. `LevelUpOffer` 에 보상 창의 `RewardDef?`(건너뛰기 문구·성소 카드 문구 `+2Lv · 최대 체력 −10%`) — 지금은 `World.Room.Reward` 로 우회.
4. 게임 쪽 시험 프로젝트(게임 프로젝트를 참조하는 xunit — 씬 흐름·`InputMap`·`HudModel`·로그 문장) — 1.1 "시험은 Core 만 참조" 를 바꾸는 일이라 메인 세션이 정함. 위 임시 시험을 그대로 옮길 수 있다.

**다른 묶음에 넘길 것**
- WP7: `FrameLoop` 이 사람 실행에서만 `InputMap.Sample(frame, 창 크기, 활성)`, 앱 틱마다 `FixedTick` 뒤 `GameFlow.Of(app).Autoplay?.Step(stack, now)`, `Moment` 로 `--capture-on`·`--exit-on-result`(이름 그대로, 처음 것만 고름), `QuitRequested` → 종료 0, `SettingsChanged` → 전체 화면, 창 비활성(사람 실행) → `RequestPause()`, `UiButtons.Fullscreen` 처리, `SCENE` 을 `Entered` 로 또 쓰지 않음.
- WP5: `WorldRenderer`·`FxSystem` 에 `IWorldBound`, `UiCanvas` 에 텍스처·원 그리기(미니맵·체력 구슬), 그림 정의 `TITLE`·`LOGO`(6.3 grid), 문 위 이름은 `RunText.DoorLabel` 을 쓸 수 있음, 피해 숫자·화면 흔들림 설정(`Save.Settings`)은 그리는 쪽이 읽음.
- WP4: 위 "자동 조작 명령" 줄의 호출 순서(`Scenario` 도 앱 틱마다 `Next` → `AppTick`), `Events`·`DoorSkips`·`RewardStalls` 는 늘기만 하는 목록이어야 함(WP6 이 센 수 뒤만 로그).
- WP8: `Consume` 은 스텝마다(프레임마다가 아님), `PlayUi` 키 `menu_move`·`menu_select`·`level_up`·`chest_open`, `PlayMusic("title")`·액트 ID(액트가 바뀔 때만), 일시정지 `SetPaused`, 시작 때 `SetVolumes`.
- WP3: 선택 뒤 대기가 남지 않으면 `AwaitingChoice = None`(남으면 `PlayScene` 이 다음 앱 틱에 새 창), `Reroll` 뒤 `Offer()` 가 새 카드.


### 통합 기록 (WP0·WP2~WP8 합침 — 단계 1, 커밋은 `git log --grep 통합`·`git log --grep 합침`)

합친 순서 wp7 → wp2 → wp3 → wp4 → wp0 → wp5 → wp8 → wp6(모두 `--no-ff`). 겹친 파일은 이 문서 하나였고, 13장 끝 구현 기록 소절을 모두 남겼다(합친 순서대로).
브랜치마다 따로는 초록이었지만 합치고 나서 드러난 이음새와 처리:

| 수준 | 이음새 | 처리 |
|---|---|---|
| 높음 | 그림·이펙트 모듈이 함께 등록되자 참조 검사가 켜져 실제 Data 가 29개 오류(무기·적·소리 연결이 가리키는 `sprites/missiles.json`·`ui.json`·`effects/common.json` 이 어느 브랜치에도 없음) — 게임이 데이터 오류 화면으로 뜸 | 세 파일을 첫 판으로 넣음. 마법·아이콘은 가져오기 메타 값, 획득물·아이콘은 exe PNG 크기. 기획 13.7 의 `lghning`·`bigexp`·`firarwex`·`arrows` 는 가져오기 표에 없어 exe PNG 대체 그림 — WP0 이 표에 넣고 WP5 가 바꾼다 |
| 높음 | `GameApp` 이 입력·봇 없이 돔(WP6 본문 전에 쓴 WP7 셸) — 키·마우스가 안 먹고 `--autoplay` 가 메뉴를 넘기지 못함, 타이틀 종료가 아무 일 없음 | `InputMap.Sample`·`FixedTick` 뒤 `AutoplayDriver.Step`·`QuitRequested`·전체 화면·창 비활성 멈춤·`ContentAvailability` 를 이음(13장 WP7 구현 기록 "남은 것" 1·4) |
| 중간 | WP4 `EnemyMath` 사본이 WP3 `DamageMath` 와 반올림이 다름(x.5 에서 1 차이) | 정본 호출로 바꿈 |
| 중간 | WP4 `Enemy.InvulnUntilTick` 을 WP3 `DamageResolve` 가 읽지 않음 | 무적 틱 전 요청을 버림 + 시험. 공통 조각으로 옮기는 일은 WP1g 요청 그대로 |
| 중간 | WP0 이 넘긴 L1 짝 조각(35·25)·거울 짝 표 — 남의 시험 둘이 막고 있었음 | 넣고 두 시험을 고침(13장 WP0 구현 기록) |

합친 뒤 실제 데이터·실제 모듈로 액트 1 판을 머리 없이 돈 결과(`Sim/RealActScenarioTests`, Scenario 분류): `--god` 씨앗 7 이 9.8분 35,197틱에 승리(방 1·2·3·4·6·8),
불변식 통과, 공정성 `unseen_shots`·`unseen_charges`·`hidden_hits` 0, `DOOR_SKIP`·`REWARD_STALL` 0, 같은 씨앗 두 번 같은 `ROOM` 기록. `--god` 없는 자동 조작은 씨앗 8 승리, 7·9 패배(방 2·6).
창을 띄우는 확인(그리기·입력·소리·캡처)은 하지 않았다.

### WP1g 구현 기록

WP1g(11.1 — 계약 갱신 v0.9·v0.9.1 + 단계 1 이 쌓은 요청, 형과 순수 로직 구현까지) 코드가 문서와 다르게 또는 더 정한 것. 코드가 정본이다(11.4 와 같은 규칙). 커밋은 `git log --grep WP1g`.
사용자 결정(2026-11-11, 기획 v0.9.2 열린 질문 50~53 답)을 그대로 받았다: 물약 키 `Q`(계약은 `CommandButtons.Drink` — 키 연결은 WP6), 물약 양 첫 값(`rules.hero.potions`·드롭 표는 손대지 않음), 방어 상한 15, 지팡이 + 방패 함께(`t`).

**이름·모양**

| 설계 | 문서 | 코드 | 이유 |
|---|---|---|---|
| 6.19 장비 접사 | `AffixDef`, 종류 키 `affixes`, `GameDefs.Affixes` | `ItemAffixDef`, 종류 키 `item_affixes`(`Data/items/affixes.json`), `GameDefs.ItemAffixes`. 기본 아이템은 문서대로 `items`·`GameDefs.ItemBases` | `AffixDef`·`affixes` 는 적 접두사(6.7)가 이미 쓴다 |
| 6.19 그림 칸 | `SpriteRef? Icon`·`Floor`, `MerchantDef.Sprite` 필수 | `IconDef?`(있는 형), `MerchantDef.Sprite` 는 null 허용 + `Requires` | `SpriteRef` 형이 없다. 상인 그림은 가져오기 전이라 코드 그림 대체(6.3) — 필수면 상인 데이터를 못 쓴다 |
| 6.19·#15 | `PickupEffect.PotionStack` | `PickupKind.PotionStack`(옛 `Heal` 자리, JSON `potion_stack`), `PickupDef.Floor`(`IconDef?`) | 획득물 효과는 `PickupKind` 하나가 정한다 |
| #4 `ItemRoller` | `DetRng`, `ref` | `RandomStream`(참조 형이라 `ref` 없음), `DrawsPerRoll = 7`(마법·접사 수·쪽·접두사·접미사·값 둘), 값은 소수 넷째 자리 반올림, 고정 접사 위치 0.5, 후보 = 정의 표 순서, 고른 쪽에 후보가 없으면 다른 쪽(둘 다 없으면 보통) | 난수 수 고정. 0.03 + 2 × 0.01 이 0.05 가 되게 |
| #5 `EquipStats.Sum` | 합산 뒤 자르기(방어·이동·쿨)까지 한 함수 | `Sum`(장비만, 자르지 않음)·`Add`·`Clamp`(방어 ≤ `armor_cap`, `move_pct` ≥ `weight_move_floor`)·`Total`(다른 출처 + 장비 → 자름), `RebaseHp(hp, oldMax, newMax)`(살아 있으면 1 아래로 안 내림) | "모든 출처를 합친 뒤" 자르려면 다른 출처를 받아야 한다. **쿨 상한 60% 는 자르지 않는다** — `DamageMath.FinalCooldown` 이 마력 폭주(상한 80%)와 함께 쓰는 곳에서 자르므로 미리 자르면 폭주 때 값이 달라진다 |
| #5 `StatSources.Equipment` | 출처 이름만 | 열거형 `StatSources { Base, Traits, Passives, Mods, Equipment }`(합산 차례의 이름표 — 합산 본문은 WP3 `HeroStats`) | — |
| #4 `HeroLook` | `Key(equip)` | `Key(IReadOnlyList<EquipItem?> 칸 순, GameDefs)`·`Key(무게, 지팡이, 방패)`, `HeroLook.Start = "ln"` | 정의를 찾아야 무게를 안다 |
| #10 `ShopStock` | `Create(merchant, act, rules, defs, seed, act, room, rerolls)`, `DetRng.For(seed, act, room, "shop", rerolls, card)` | `Create(merchant, act, number, runSeed, defs, ShopPotions)`·`Reroll(shop, …)`(`Rerolls` + 1 은 여기서)·`RerollPrice`·`PotionCard`, 흐름 `RunSeeds.Shop(seed, act, number, rerolls, card)`(SplitMix64, 소금 `SHOP_STC`) | 문서 시그니처에 `act` 가 두 번. 페핀 카드 값은 물약 칸 상태가 정한다. 흐름 이름·차례는 계약(11.3) |
| #9 `ShopCard.Bought` | 산 것 | 장비 카드 = 산 것, 물약 카드 = **살 수 없음**(다 올림·칸 가득). 그 액트에 나올 기본 아이템이 없는 칸 = 살 수 없는 빈 카드 | 페핀 카드는 같은 카드를 여러 번 산다(기획 12.8) |
| #9 상자 → 책 | `RewardPickup.Stage`·`Chosen` | `Pickup.RewardStage`(`RewardPickupStage { Closed, Dropped }`)·`Pickup.RewardChosen`(올릴 무기 칸, −1 = 아직·`단련`) — 공통 조각 | `RewardPickup` 형이 없다(보상 물체는 `Pickup`) |
| #8 `IShop` 자리 | — | `World.ShopService`(`IShop?`, WP3 이 넣음) | `IProgression` 과 같은 자리. 상인 상태는 `Room.Shop` |
| #18 `DamageCause` | 투사체·적 몸·장판 | `{ None, Body, Projectile, Hazard, Telegraph }`, `DamageRequest` 끝 인자(기본 `None`), `DamageQueue.ToHero(…, cause)` 선택 인자 | WP3 요청의 "예고" 를 함께. 기존 호출 그대로 |
| #19 | 화면 신호·종료·비활성 멈춤 자리 | `IAppSignals`(`Moment`·`QuitRequested`·`Signal`·`Quit`·`RequestPause`) — WP6 `GameFlow` 가 구현하고 처음 만들 때 `AppServices.Signals` 에 넣음. `AppServices.Content`·`AssetProblems`(문장 목록)·`AssetRoot`·`DevHooks`(`IDevHooks`), `IAudioServices : IDisposable`, `PlayReward` 는 기본 구현(`PlayUi("rewards.<id>.open|collect")`) | 단계 1 코드가 그대로 돈다. 쓰는 쪽을 옮기는 것은 WP6·WP7·WP8 |
| #16 가져오기 표 | 새 묶음 | 표 13개(`ctrlpan.json`·`items.json`·`inv.json`·`towners.json` — 빈 표), 모양 검사. 새 표의 출력은 아직 `OutputNames` 에 넣지 않음 | 도구가 아직 만들지 않는다 — WP0 이 만들기 시작할 때 함께 넣는다(13.5-10) |
| 6.15 | `ui.room_clear: snd_magic`, `ui.belt_potion` | 예에서 `room_clear` 지움(WP8 요청 4), `belt_potion` → `potion_drink`(실제 `bindings.json`·`UiKeys` 도) | — |

**더 정한 것**

1. **공통 조각 `Hero`**(11.1 표에 짐만 있음): `Potions`·`PotionSlotsLevel`·`PotionSpeedLevel`·`Heal`(`HealOverTime(TicksLeft, Remaining)` — 틱마다 남은 양 / 남은 틱, 마지막 틱에 나머지 → 합이 정확히 처음 양)·`Gold`(지갑)·`Equipment`(`EquipItem?[6]`). `Suspend` 의 회복 채우기·`RewardDraw` 의 페핀 가중·HUD 가 읽을 자리. `Hero.Belt` 는 없앰.
2. **`RunStats.PotionsDrunk`**(문서는 `GoldSpent` 만): `ROOM drunk` 는 방 차이로 센다(WP3 `Drink` 가 늘림). `RoomRecord` 의 `HpIn` = 짐을 푼 뒤 체력(회복 문 방이면 최대 체력, 모르면 −1), `Potions` = 들어올 때 물약 수, `GoldSpent`·`Drunk` = 방 차이, `Shop` = 상인 ID.
3. **`HeroCarry.MaxedAtTick`** 은 틱이 아니라 **만렙 순번**(0부터, 같으면 칸 번호, −1 = 아직 — `World.Tick` 은 방마다 0 부터라 틱은 방을 넘어 견줄 수 없다). WP3 `HeroCarrier` 가 순번을 담고 풀 때 `MaxedAtTick = 순번`(새 방 초반 틱과 같으면 칸 번호 — 작은 어긋남, WP3 이 다듬을 수 있음).
4. **마시던 회복은 계단 문에서만 채운다**: `RunSession.Settle` 이 계단(액트의 마지막 방)일 때 짐 담기 전에 남은 회복을 채우고 비운다 — 끊은 판(`Suspend`)·끊지 않은 판이 같은 틱에 같은 값. 보통 문을 넘는 회복은 짐 필드가 아니라 WP3 이 시간 효과(`HeroCarry.Effects`)로 넘긴다(6.19 "짐에 회복 중 상태를 넣지 않는다" 는 계단 저장 이야기 — 기획 11.5 "넘김 동안 멈췄다가 이어진다").
5. **회복 문**: `RoomState.HealOnEnter` 는 그 방 보상(덤 포함)이 `heal_next_room` 이면 참으로 시작하고, `RoomFlow.Step` 맨 앞(어느 단계든 첫 시뮬레이션 틱)에서 체력을 최대로 채우고 거짓 + `CueKind.HealOnEnter`. `--room` 시작 방의 보상이 회복 문이어도 같다. `RunState.HealOnEnter` 는 회복 문으로 나간 정산 때 참, 다음 방을 만들면 거짓(방으로 넘어감) — 그래서 `Suspend` 의 단언은 늘 참.
6. **문 보상 뽑기 `RewardDraw`**(새 파일 `Sim/Run/RewardDraw.cs` — 문서는 `RunPlanner.cs`): `RunPlanner` ④ 가 쓴 난수를 `RoomPlan.RewardDraws`(끝 인자, 가중표 원소 수 + 1 — 마지막 하나는 상인 보장의 누구 고르기)에 남기고, 문이 열리는 틱에 바뀐 가중으로 **같은 난수**를 다시 줄 세운다. 바뀐 가중·보장·거르기가 없으면 v0.8 결과(`RewardOrder` 앞에서부터)와 같다 — 단계 1 판(마법서·상자만)은 그대로다.
   차례: 가중 바꿈 → 회복 보장(첫 문 `ExitGates[0]`, 거울판이면 이미 짝 이름) → 상인 보장(둘째 문) → 줄 순서대로 거르기(같은 ID 두 문 없음, 한 문 단계 상인 하나) → 모자라면 `fallback_reward`. 체력 조건은 최대 체력이 0 인 월드(성장 모듈 없는 시험)에서 보지 않는다.
   `merchant_first_room` 은 **액트 1 에만**(기획 11.7 "액트 1 은 3번 방으로 가는 문부터"). 상인 보장은 다음 방이 일반 방이고 상인 조건을 통과할 때만, 누구인지는 `available` 을 통과한 상인 중 균등.
   "만난 상인" = 상인 방에 **들어감**(`RunSession.Enter` — 창을 열었는지와 무관), "이 액트에 상인 문이 보였음" = 정산 때 그 방 출구 중 상인 문이 있었음. `IProgression.RewardAvailable` 은 회복 문·상인에 늘 참(조건은 `RewardDraw`).
7. **상인 방**: `RunSession.Enter` 가 짐을 푼 **뒤** `ShopStock.Create`(페핀 카드가 물약 칸 상태를 읽음). 보상 단계에서 상인은 물체를 놓지 않고 보상 줄 첫 자리를 비워 둔다(그리기 WP5 는 `RewardSpot`). `Shop.Visited` 전에는 문이 안 열리고, `Shop.Open` 이면 `ExitBlocked`.
   `ShopStock` 의 반지 둘 다른 접사는 **다른 반지 카드의 접사만** 피한다(첫 판은 지팡이 카드 접사까지 피해 두 번째 반지가 보통이 되었다 — 시험이 찾음).
8. **`RunSnapshot`**: `MerchantsMet` 를 끝 인자로, `CurrentFormat` 2, 모양 지문 `4bf0bbef`(장비 형 셋을 `ShapeTypes` 에), 이어 하기 대조에 장비 기본 아이템·접사 ID·만난 상인 ID. WP7 요청대로 저장 옵션에 `RespectRequiredConstructorParameters` — 기본값 없는 위치 인자가 빠지면 `JsonException`(기본값으로 조용히 채우지 않음).
9. **로딩 검사**(`DataRules`, 13.5-23 ④ 보탬): 종류마다 칸(`object` — `heal_next_room`·`merchant` 만 생략, `merchant` 는 `merchant` 필수·그 밖 금지, `bundle`·`shrine`·`cards` 는 제 종류만, `drop` 은 `kind: reward` 이고 물체 있는 보상만),
   6.19(방어구는 무게 필수·지팡이/반지는 무게 없음, 접사 `min ≤ max`·`step > 0`·`stage` 1·2·`kinds` 있음, 장비 상인 `kinds` 필수·`upgrades` 금지, 물약 상인 `kinds` 금지·`upgrades` 필수·`rerollable: false`·`rules.hero.potions` 필요, 장비·장비 상인이 있으면 `rules.equipment` 필요, `durations` ≥ 1·양수, `slots ≤ slots_max`, 겉모습 키 `[lmh][nut]`·`look` 이 `looks` 안).
10. **단계 1 코드의 옛 이름 자리**(본문은 그 묶음): 허리띠 저절로 마심은 `rules.hero.belt` 와 함께 없앴다(`HeroBelt` → `HeroPotions` 의 `Capacity`·`Add`·`Full` — 넣기만, 넘치면 빠짐). 체력 물약(`potion_stack`)은 칸에 쌓이고 가득이면 안 집힘, 보물 상자의 `potions` 도 칸으로. 마시기(`Drink`)·서서히 회복 틱·`PotionDrink`/`PotionDenied` 신호는 **아직 없다**(WP3).
    HUD 는 허리띠 칸 자리에 물약 칸 수만큼 칸(WP6 이 물약 칸 하나 + `2/3` 로), 미니맵 보상 색 회복 문 빨강·상인 금색(임시), 상자 창의 `단련` 금화 문구 지움, 자동 조작 문 우선순위 0 = 저체력일 때 회복 문(WP4 가 6.19 규칙으로), `StateHasher` 의 허리띠 열 → 물약 수(장비·지갑·회복 중 열은 WP7).
11. **실제 데이터**: `rewards.json` 에 `reward_potion`(회복 문)·`reward_smith`·`reward_witch`·`reward_healer` **정의만**(액트 가중표에는 넣지 않음 — `stages/` 는 WP4 의 단계 2 데이터), 보물 상자 `potions` 는 0 그대로(물약 칸 본문이 없어 1 이면 줍기만 하고 마실 수 없음 — WP3 이 본문과 함께 1 로).
    `items/bases.json`(18)·`items/affixes.json`(11)·`merchants/`(셋, 그림 없음)·`rules/equipment.json` 은 기획 11.6·11.7 표 그대로(퍼센트는 비율). 직업 `looks` 는 넣지 않았다(아홉 벌 그림 묶음이 아직 없다 — WP5·WP0).
12. **가져오기 표 해시가 바뀐다**(표 13개): v0.9 표를 받은 PC 는 시작 검사가 "표가 바뀜" 으로 막으므로 가져오기를 다시 돌린다(1.4 — 새 표는 비어 있어 만드는 파일은 같다).

**단계 2 묶음에 넘길 것**
- WP3: `Drink` 명령(창·회복 중·물약 0·체력 가득이면 `PotionDenied`) → `Hero.Heal = HealOverTime.Start(MaxHp × heal_pct, round(Duration(PotionSpeedLevel) × 60))`·`RunStats.PotionsDrunk++`, `HeroSystem` 차례에서 `Heal.Step()`(최대에서 자름), 방 사이 회복은 `Effects` 로 넘기고 `Validate` 에 그 ID.
  장비 능력치를 `HeroStats.Recompute` 에 `EquipStats.Add` → 모든 출처 합 뒤 `EquipStats.Clamp`, 최대 체력 바뀜은 `EquipStats.RebaseHp`. `IShop` 구현(`World.ShopService`)·상인에 닿으면 `Open`·`CueKind.Shop*`, 지갑 `Hero.Gold`(금화 줍기도 지갑에 — 지금은 `RunStats.Gold` 만 늘어남), 보물 상자 두 단계(`RewardStage`·`RewardChosen`·`ChestDrop`)·`RewardOsd`, `rewards.json` 상자 `potions: 1`, `HeroCarrier` 의 만렙 순번 다듬기, `LevelUpOffer.Reward` 채우기.
- WP4: `stages/` 가중표에 회복 문·상인 셋(기획 11.4 가중 15·8·8·6), 자동 조작의 물약(`bot_threshold`)·상인 창(기획 12.8)·회복 문 우선순위, `RunPlanner` ① 덱 순서(33-6), `DamageRequest.Cause` 를 적 쪽 요청에 채움(WP3 의 `hidden_hits` 셈이 원인으로), `InvulnUntilTick` 은 이제 공통 조각.
- WP5: 상인(`Room.Shop` — `RewardSpot` 자리, 코드 그림 대체)·회복 문 아이콘·둘레 아이콘·겉모습 아홉 벌(`HeroLook.Key` → `HeroClassDef.Looks`, 없으면 `Look`)·바닥 그림(`PickupDef.Floor`·`ItemBaseDef.Floor`)·물약 칸·체력 구슬.
- WP6: `Q` → `CommandButtons.Drink`(한 번만 — 첫 틱), 상인 창(`ShopState` — `Choice1~3`·`Reroll`·`Skip`, `RingPending`), HUD 물약 칸·장비 줄·지갑, `IAppSignals`·`AppServices.Content` 로 옮기기(`GameFlow` 우회 정리), 결과 화면 `쓴 금화`.
- WP0: 가져오기 표 넷의 실제 줄(원본 이름·사각형·프레임 확인)과 도구 출력·`OutputNames`, 상인 대사 `sounds.json`, 주인공 아홉 벌 `hero.json` 줄. `IncludeSourceRevisionInInformationalVersion` 거짓이 들어갔다.
- WP7: `run.json` Format 2(옛 판 1 은 불일치로 버림 — 이미 대조가 함), `ROOM` 새 열(`hp_in`·`kills`·`gold`·`gold_spent`·`shop`·`potions`·`drunk`), `StateHasher` 에 장비·지갑·물약 칸·`Heal`, `IDevHooks` 구현·`AppServices.Content`·`AssetProblems`·`AssetRoot` 채우기, `IAudioServices.Dispose` 를 계약으로 부르기.
- WP8: `PotionDrink`(지금 `ui.potion_drink`)·`PotionDenied`·`HealOnEnter`·`ShopOpen`(인사 대사)·`ShopBuy`·`ShopDenied`·`ChestDrop` 연결 키, `PlayReward` 본문(선택), `AppServices.AssetRoot` 로 자원 위치.

---

## 부록 A. 원작 형식 규칙 (조사 문서·시제품에서 옮김)

세션 임시 폴더의 조사 문서(`diablo-inv/inventory-levels.md`·`inventory-sprites-sounds.md`)와 시제품(`d1conv`·`d1map`)이 확인한 규칙을 저장소로 옮긴 것이다(v0.3 리뷰 V10).
원작 바이트는 없다 — 비트 뜻·저장 순서·함정만 적는다. DevilutionX 소스에서 읽은 내용은 사실 요약이고 코드는 옮기지 않았다. WP0 은 이 부록과 1.4 만으로 해석기를 만들 수 있어야 하고,
도구 시험(10.1 `Formats`)의 기대값도 여기서 정한다. 모두 리틀 엔디언.

### A.1 MPQ

- `diabdat.mpq`(이 PC 판 517MB, MPQ 형식 0, `(listfile)` 없음). 경로 구분자는 `\`, 대소문자 무관(War3Net `MpqArchive.Open` + `OpenFile`).
- 파일 목록이 없으므로 이름은 표가 안다(1.4). 저장 단계의 압축(PKWARE implode·파형 압축)과 파일 이름 키 암호화는 War3Net 이 푼다 — 소리 1435개의 RIFF 길이가 실제 길이와 모두 맞았다.
- 같은 이름이 `diabdat.mpq` 와 `hellfire.mpq` 양쪽에 있는 것이 있다 — diabdat 를 먼저 본다.
- (v0.6, 2차 조사) 헬파이어 MPQ 넷: `hellfire.mpq` 항목 584개(이름 찾음 476), `hfmonk.mpq` 336개(전부), `hfvoice.mpq` 90개(88), `hfmusic.mpq` 2개. 역시 `(listfile)` 이 없어 이름은 표가 안다.
  헬파이어 층 폴더는 `nlevels\l5data`·`nlevels\l6data` — 실행 파일에 남은 문자열 `Levels/L5Data/…` 로 찾으면 없다고 나온다.
  수도사 목소리·헬파이어 대사는 같은 이름이 `hfvoice`(16비트)와 `hfmonk`·`hellfire`(8비트)에 둘 다 있다(찾는 순서는 1.4 `prefer`).

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
  (v0.6) 주인공도 그렇다: 전사 `hn`·`mn` 벌 죽기 15프레임, 활 벌 서기 8프레임처럼 DevilutionX 표와 파일이 다른 곳이 있다.
- (v0.6) **주인공 네 직업**: 시전 파일 프레임·칸 = 전사 20·96, 도적 16·96, 마법사 12·128, 수도사 18·114(원본 확인). 세 시전 파일(`fm`·`lm`·`qm`)에는 효과가 그림에 박혀 있다.
  죽기는 전사·도적·마법사가 맨손 벌(`{c}{갑옷}n`)에만 있고 **수도사는 27벌 모두 자기 죽기·막기**가 있다 — "죽기는 맨손 벌에서" 를 공통 규칙으로 두지 않고 표 항목마다 경로를 적는다(1.4).
  수도사 `plrgfx\monk\` 291개(48.2MB)는 칸 폭이 동작마다 다르다(서기·걷기 112, 공격 130, 맞기·막기 98, 시전 114, 죽기 160).
- (v0.6) **헬파이어 몬스터**: 그림 묶음 24개(hellfire.mpq) + 그림은 diabdat 의 것을 쓰는 셋(펠트윈 `tsneak`, 스컬윙·본 데몬 `demskel`). hellfire.mpq 의 그림 묶음에는 TRN 이 없다.
  (v0.6.1 바로잡음) 그 셋의 소리: 펠트윈만 hellfire 에 있고(`newsfx\ftwin`), 스컬윙·본 데몬의 원작 소리 묶음 `demskel\demskl` 은 원본 어디에도 없다(0/8). 스컬윙은 diabdat `monsters\skelaxe\skelt.trn` 변종을 쓴다(본 데몬은 원색).
  칸 폭은 나크룰 226(서기 2프레임), 마법 폭발 `ex_red3`·`ex_blu3` 292 까지 — 원작 최대 160 을 넘는다. 헬파이어 팔레트도 색인 128~255 가 `l1.pal` 과 같아 스프라이트는 `l1.pal` 하나로 풀린다.

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
- (v0.6) **헬파이어 층**: L5(지하실) CEL 1,836장·MIN 650개 × 10칸·TIL 217개·팔레트 `l5base.pal` 하나, L6(둥지) CEL 1,763장·MIN 606개·TIL 166개·팔레트 `l6base1~5`(조각 수는 WP10 이 MIN 크기 / SOL 크기로 확인).
  **번호 함정**: L5 는 L1 메가타일 번호 1~43 과 바닥 13 만 같은 구실이고 44 이상은 다르다(L1 바닥 변종 162·163 이 L5 에서는 기둥). L6 은 L3 번호와 아예 맞지 않는다. 그래서 타일셋은 층마다 따로다(6.11).
  헬파이어 미리 만든 방(모퉁이 돌, 나크룰 방)은 MPQ 에 없다. 색 순환이 있는 층이다(이 게임은 멈춘 그림).
- (v0.6) **벽 메가타일은 반만 막힌다**(L1 1 = 왼쪽 열, 2 = 위 줄). 그래서 벽 메가 두 개를 바로 이웃에 두면 사이에 하위 1칸 바닥 띠가 남는다(6.11 `over_border`, 13.5-5).
  (v0.6.1) 같은 이유로 가로·세로 벽이 만나는 메가에 한쪽 조각만 두면 하위 1칸 구멍이 남는다 — 위 줄과 왼쪽 열을 둘 다 막는 조각은 모서리 4(7770)·피 묻은 모서리 168(7770 — 167 은 세로 7070, 13.16)(6.11 "벽 맞물림").
- (v0.6.1) **DUN**(`audit` 대조에만, 1.4): `u16 폭, u16 높이`(메가타일 수) 뒤에 칸마다 `u16`(줄 = y, 칸 = x 순), 0 = 비움, 그 밖 = 메가타일 번호(우리 타일셋 `pieces`·`floor` 번호와 같은 체계 — WP0 이 `rnd6` 으로 확인).
  그 뒤에 물체·몬스터 층이 더 있을 수 있으나 읽지 않는다. 내용은 출력·저장하지 않는다.
- 덧그림 CEL(L1S): 폭 64, 아래 행부터, 바이트 `b < 0x80` 이면 b 개 색 복사, 아니면 `256 − b` 개 투명. 프레임 머리말 없이 바로 데이터.
- 투영(원작 기준점): 하위 타일 (x, y) 의 바닥 마름모 왼쪽 아래 = 화면 `((x − y)·32, (x + y)·16)`. 벽 기둥은 기준점에서 위로 `(조각 수 / 2)·32`px(L1 160px).
  이 게임의 월드 좌표는 4.1 `IsoMath` 가 정본이고, 이 식은 해석 확인용이다.

### A.4 SOL

하위 타일마다 1바이트. 비트: 0 Solid(걷기 막힘), 1 BlockLight, 2 BlockMissile(투사체 막힘), 3 Transparent, 4 TransparentLeft, 5 TransparentRight, 7 Trap(128).

- 원작의 "바닥" 판정 = Solid 도 BlockMissile 도 아님. 바닥 칸은 바닥 단을 먼저 그리고, 아니면 벽 단계에서 그린다 → 이 게임의 성질 `.` = 바닥 조각, 그 밖 = 기둥 그림(1.4 아레나).
- 성질 대조는 bit0·bit1·bit2 를 본다(v0.5 — v0.4 까지는 bit0·bit2, 1.4). `sol & 7` 이 6.11 의 8글자와 1:1 이다(13 → `x`, 15·159·175·191 → `X`). 다른 비트가 섞인 값이 흔하다.
  원작에서 bit1 은 빛·시야를 막는 비트다(이 게임의 시야 막힘, 4.11).
- **원본 SOL 오류**(DevilutionX 참고): L1 아치 조각 일부에 시야·투사체 막힘이 빠져 있다(고치지 않으면 아치 밑으로 투사체가 지나가고 시야가 샌다). 쓰는 메가타일의 칸만 `sol_known_diffs` 에 번호와 이유로 적는다(6.11).
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
- (v0.6) 헬파이어 음악 `dlvle`(둥지)·`dlvlf`(지하실)는 `hfmusic.mpq`(스테레오 16비트). 헬파이어 몬스터 소리 `monsters\newsfx\*`, 대사 `sfx\hellfire\nakrul1~6`·`defiler6~8`.
  주인공 소리는 직업 폴더마다 철자가 섞여 있다(전사 `warior`·`wario`), 수도사는 `monk72` 가 없다. 소리는 머리만 읽었고 듣지 않았다(기획 열린 질문 29).
