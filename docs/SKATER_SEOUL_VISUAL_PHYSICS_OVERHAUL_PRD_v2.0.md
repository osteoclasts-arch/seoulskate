# SKATER SEOUL — Visual, Physics & UX Overhaul PRD v2.0

> **문서 목적:** `osteoclasts-arch/seoulskate`의 현재 Unity 프로토타입을 **한 번의 Codex 구현 사이클에서** 상업 게임에 가까운 **시각적 Vertical Slice**로 교체한다. 단순 제안이나 리디자인 목업이 아니라 **실제 수정된 Unity 씬·프리팹·코드·에셋·빌드·검증 영상**이 납품 대상이다.
>
> **기준일:** 2026-10-23(KST) / **대상:** Unity 6 URP, 모바일 가로 화면, iOS 우선 / **참고:** Skate City의 *관찰 가능한* 카메라·조작 반응·모션·아트 품질. 그 게임의 내부 물리 공식, 코드, 리그, 원본 에셋을 동일하게 확보했다는 뜻은 아니다.
>
> **저장소:** https://github.com/osteoclasts-arch/seoulskate / **입력 녹화:** `ScreenRecording_10-23-2026 02-19-39_1.MP4`(약 27.2초, 1180×2556 저장, 프레임은 가로 게임 화면) / **우선순위:** 이 PRD의 비주얼·물리·HUD 요구가 이전 PRD의 임시 플레이스홀더 표현보다 우선하며, **성장·경기·입력 데이터 정책은 종전 PRD를 보존**한다.

## 0. 성공의 정의 및 범위 통제

### 이번 사이클에서 반드시 *작동*해야 할 것

1. 임시 큐브 인간이 아니라 **일관된 체형·의상·스킨과 자연스러운 관절을 가진 리깅된 3D 스케이터**가 화면에 나온다. 브랜딩: 청록 캡, 크림색 후디, 남색 배기팬츠, 흰 스케이트화. *원작 Skate City 캐릭터·트레이드드레스는 복제하지 않는다.*
2. **실제 형태를 갖춘 스케이트보드**(concave 데크, 노즈·테일, 트럭, 4개 바퀴)가 접지해 달린다. 바퀴는 `전진 거리/바퀴 반지름`으로 회전하고 시각적 접촉점이 땅에서 부유하거나 관통하지 않는다.
3. PUSH 시 **스케이팅 푸시 모션 → 발이 보드에 복귀 → 양발 라이딩 자세**, 미는 힘과 관성으로 속도를 얻고, 버튼을 떼면 급정거하지 않고 코스팅한다.
4. Ollie / Kickflip / Tre Flip 각각 **Crouch → Pop → Air/Foot Flick → Board Rotation → Catch → Compress → Ride**가 판정 이벤트에 동기화된다. 실패는 Under-rotation / No-catch / Lost-balance의 **시각적으로 다른 원인**으로 연출된다.
5. 하늘색 단색 벽·거대 박스가 아닌 **도시의 원근·공간·질감이 느껴지는 오리지널 서울풍 스팟**: 스케이트 가능한 콘크리트 플랫, 렛지 1개, 은은한 한강/교량 실루엣, 나무·벤치·표지판·멀리 있는 건물, 따뜻한 자연광, 그림자, 재질의 변주. 실제 뚝섬과 동일한 지형이라고 주장하지 않는다.
6. 거대한 상단 디버그 사각형·8방향 휠·과도한 PUSH 버튼을 없애고 **Play HUD / Training HUD / Debug HUD를 명확히 분리**한다. 가로형 가독성·Safe Area·양손 터치가 검증된다.
7. 현재 연습, 숙련도 저장, 로컬 S.K.A.T.E. 대전의 기능은 **회귀 없이 유지**한다. 생성용 에디터 메뉴는 새로운 씬과 프리팹을 만들고 기존 씬을 덮어쓰지 않는다.
8. 코드 컴파일 PASS뿐 아니라 **Unity Play Mode 실제 상태·연속 주행·트릭·착지·복구**, 실기기 가로 화면 캡처, 성능 기록을 근거로 합격/미달을 보고한다. 테스트가 안 되면 `NOT RUN`이다.

### 이번에 하지 않을 것

39개 전 기술 제작, 복수 서울 실경 맵, 온라인 실시간 PvP/랭크, 커리어 전체 이식, 상점·결제, 풀 래그돌, 4바퀴 각각의 풀 멀티바디 물리, 원작 실행 파일 복사·추출. 이런 일을 이번 목적의 필수 요구로 끼워 넣지 않는다. **단, 화면에 드러나는 임시 플레이스홀더로 ‘완료’를 선언하는 것은 금지한다.**

### 품질 수준에 대한 정직한 합의

이번 목표는 *Skate City의 소스/셰이더/물리 엔진 정확한 복제*가 아니라 **동일 장르의 유려한 플레이 감각과 시각적 밀도를 갖춘 독립 작품**이다. 무료 에셋만으로 해당 수준의 리깅/트릭 애니메이션을 확보하지 못하면, 그 항목을 `BLOCKED — asset needed`라고 보고해야 하며 임시 큐브로 대체하고 `PASS`를 기록하면 안 된다. Codex는 가능한 구현을 계속 진행하고 미완료 목록과 다음 액션을 남긴다.

## 1. 현재 저장소 사실 감사 — 문제의 직접 원인

> 정적 코드 분석 및 영상 관찰을 분리한다. 해당 저장소는 `Assets/SkaterSeoul`과 문서 위주의 **소스 팩**으로, `Packages/`, `ProjectSettings/` 및 저장된 씬은 Git에 없다. Codex는 **실제로 iPhone 앱을 빌드한 로컬 Unity 프로젝트**를 먼저 확인해야 한다. 다른 Unity 프로젝트에 코드를 수정하고 성공했다고 주장하지 않는다.

| ID | 근거 파일/메서드 | 현상/원인 | 필수 수정 | 검증 |
|---|---|---|---|---|
| A01 | `Editor/SkaterSeoulSceneBuilder.cs:CreateScene` | 캐릭터는 큐브 팔다리/후디 + 구 머리, 환경은 큐브 빌딩, 단색 배경 | 리깅된 캐릭터/전용 보드 메시, 파크 프리팹/재질/라이팅, 명확한 계층 분리 | 기본 Ride 스크린샷에서 큐브 인간/빈 배경 제거 |
| A02 | `SceneBuilder`의 `Wheel` 4개는 `PrimitiveType.Cylinder`, `CreateVisual`이 Collider 제거 | 바퀴를 물리적으로 지탱하는 물체가 아니다. 충돌은 루트 BoxCollider가 처리 | wheel contact proxy + 4 wheel rolling visuals (필요하면 suspension/ground pose) | 정지/전진/후진/공중/착지 접점 검사 |
| A03 | `SkaterSeoulPrototype.FixedUpdate`의 `body.linearVelocity.x` 직접 보간 | 일정 목표 속도에 즉시 맞추는 운동. 바퀴 구름/경사/푸시 동작과 분리됨 | 가속도·마찰·제동·지면 접촉 기반 2.5D 라이딩 컨트롤러 | 속도-시간 곡선, 경사, 코스팅 |
| A04 | `SkaterSeoulPrototype`에 wheelTransform 회전 업데이트 부재 | **주행 중 바퀴가 안 도는 직접적 코드 원인** | 로컬 축이 올바른 wheel visuals 등록, 거리 기반 spin 적용 | 1m 주행과 역주행 시 회전량 검사 |
| A05 | `AnimateFlight()`은 `(Time.time-trickStarted)/추정비행시간`에 맞춰 보드만 회전 | 발 flick, pop, catch, compression 없음; 실제 접촉 전에 UI 연출이 진행 | 물리 상태/이벤트 기반 모션 타임라인, 두 발 IK, 독립 body/board pose | 3기술 접촉시점 6~8 keyframe 검사 |
| A06 | `BeginTrick()`이 성공 여부를 이륙할 때 draw, `ResolveLanding()`은 `landed=intendedSuccess` | 실제 board orientation/접촉/회전 부족과 결과가 분리됨. 실패 종류 선택에 추가 난수 사용 | Ground → Air → Contact 결과 표본으로 기계적 판정 후 유효한 경우 1회 확률, 결과-연출 단일 이벤트 | 한 attempt 한 verdict, 실패 원인과 모션 일치 |
| A07 | `OnCollisionEnter/Stay` 정상 벡터·0.25초 이후 접촉으로만 착지 확인 | 큰 루트 BoxCollider를 바닥 지지로 사용, 바퀴 접촉·데크 충돌을 구분하지 못함 | Ground probe + board envelope + 충돌 normal, wheel contact, grounded hysteresis | 0~9m/s, 단차, 점프, 지면 경사 |
| A08 | `LateUpdate()` 고정 높이·후행 x+1.5, `Camera.orthographicSize=4.4` | 스케이터 작고 지형 예고·수직 점프 추적·연출 레이어 부재 | 측면 추적+진행 방향 예고+높이 추적+화면 내 인물 크기 목표 | 앞쪽 장애물 최소 예고량·시야 케이스 |
| A09 | `OnGUI()/DrawControlButtons()` | 상단 커다란 텍스트 상자, 좌측 8방향 wheel, 큰 PUSH 원, 메뉴 2개가 플레이 가림 | 런타임 `Canvas`/TextMeshPro와 state-specific HUD, minimal controls | HUD 크기/대비/겹침/손가락 영역 |
| A10 | `SkaterSeoulRules.RecognizeSwipe` | 상향 스와이프 3종만 지원, 방향은 시작-끝 displacement 위주; 트릭 wheel 탭이 주 인터랙션처럼 보임 | 현재 3트릭 의미는 보존하고 제스처 zone/preview 개선. 8방향 완전 확장은 별도 테스트 후 | 제스처 오인식·UI 시작·취소·두 손가락 |
| A11 | README/STATUS/TEST_REPORT | 일회용 프로젝트에서 Unity 6.3 컴파일·10회 smoke, iPhone 앱 설치/프로세스 실행은 기록됨. **타깃 URP와 실제 터치는 미검증** | 지금의 실제 로컬 Unity 프로젝트에서 URP/실기기 새 QA 수행 | 실행된 테스트만 PASS |
| A12 | `SkateTrick` 임시 enum `Ollie=0, Kickflip=1, TreFlip=2` | 레거시 39트릭 catalog 인덱스와 다름(`Ollie=0`, `Kickflip=3`, `TreFlip=24`) | 외관 작업에서 과거 저장 enum을 이식하지 말 것. 명시적 ID 매핑만 허용 | SS_Mastery_* 보존, migration 분리 |

정확한 GitHub 파일: `Assets/SkaterSeoul/Scripts/SkaterSeoulPrototype.cs`, `SkaterSeoulRules.cs`, `Assets/SkaterSeoul/Editor/SkaterSeoulSceneBuilder.cs`, `docs/SKATER_SEOUL_IMPLEMENTATION_STATUS.md`, `docs/SKATER_SEOUL_TEST_REPORT.md`. 모든 코드 현황은 이 소스 팩의 확인 시점 기준이며 사용자 로컬 프로젝트가 더 최신일 수 있다. **작업 전 git status/branch/current local files diff 필수**.

### 영상 증거(사용자가 제공한 녹화)

- 초반 ~0–5초: 평평한 민트 배경, 두꺼운 검정 HUD와 양측 조작 휠, 화면에 비해 작은 정적 스케이터. PUSH 중 다리로 지면을 차는 동작이 안 보임.
- ~6초 전후: 공중 플립 시 몸의 상하 이동은 보이지만 발이 보드를 튕기고 분리·캐치하는 운동학이 읽히지 않음.
- ~15–21초: 파란 직육면체/크림색 직육면체가 장애물·도시를 대체함; 좌측 조작 휠은 장애물을 가림.
- ~22–25초: 실패할 때 전신이 단일 회전·복귀에 가까움; 보드 이탈/발 접촉/착지 물리의 원인은 화면에서 식별하기 어려움.
- 마지막 ~27초: iOS Control Center로 전환. 종료 뒤 시스템 UI는 게임 결함으로 기록하지 않음.

영상으로 *확정되는 것*은 외관과 움직임의 시각적 결과다. 실제 루트 Rigidbody의 접촉 수치·프레임 타이밍 등은 저장소 코드 및 장치 로그가 필요하다. **바퀴 업데이트 부재는 코드 증거로 확정.**

## 2. Visual Art Direction — 'Seoul Sunset Skate' (오리지널)

### 2.1 시각적 북극성

- **키워드:** human-scale skater, warm concrete, soft sunset, layered Korean cityscape, controlled palette, breathable composition, material-rich stylization, fluid motion.
- **목표:** 2.5D 실루엣 가독성 + 실제 3D 깊이(그림자·조명·트럭·슈즈) + 적절한 배경 원근. *저폴리곤/픽셀/플랫 블록*이 아니라 **적당한 폴리곤 수의 매끈한 카툰 리얼리즘**.
- **금지:** 큐브 팔다리, 원통 머리, 균일 무광색의 거대 직육면체 하늘/지형, UI의 플레이스홀더 텍스트, 과도한 Bloom, 검정 테두리 과다, 원작 로고·아이콘·캐릭터·지형 데이터 복제.
- **필수 컬러:** 아스팔트/콘크리트 중성 회베이지와 따뜻한 엣지, 청록 캡, 크림 후디, 네이비 팬츠, 미색 휠. 뒤쪽 도시/한강은 낮은 채도, 전경 skater만 포컬 포인트. 빛의 방향 및 명암을 분리.
- **레퍼런스:** https://skatecitygame.com/skatecity/presskit/ 및 https://skatecitygame.com/newyork/presskit/ — 이미지 복사본을 에셋으로 추출하지 말고 *분위기/광원/컷 구도/모션 관찰 자료*로만 사용.

### 2.2 캐릭터: 최초 화면부터 합격해야 하는 수준

`SkaterCharacterVisual` 프리팹이 Humanoid/일관된 리그·스킨에 바인딩되고, 어깨·팔꿈치·손목·고관절·무릎·발목이 구별되어야 한다. **몸통·두 다리는 다이내믹 포즈를 취하고 발은 각각 독립적으로 보드에 붙거나 떨어질 수 있어야 한다.**

- 인체 비례: 성인 캐주얼 스케이터, 과도한 머리 비율 금지. 선명한 측면 실루엣과 의상 볼륨(후디 자락, 배기팬츠 주름, 신발 윤곽). 모자·얼굴·상체 색역을 분리.
- 생산 가능 경로 A: CC0/상업 이용 허용 휴머노이드 리그를 선택 → 라이선스 원문 확인 → 청록 캡/크림 후디/네이비 팬츠/스케이트화로 수정 → Animator/Humanoid avatar 등록 → Animation Rigging로 foot IK.
- 생산 가능 경로 B: 기존 모델이 없다면 Blender/Unity에서 **스킨드 메시·관절 구조·커브가 있는 의상 실루엣**을 자체 작성. *primitive cube renderer를 묶은 사람은 fallback QA용에만 남겨도 되나 런타임 최종 화면에 보이면 FAIL.*
- 시각 검사: Ride, Push contact, Crouch, Pop, Kickflip flick, Tre rotation, Catch, Landing compress, Bail에서 관절 접힘과 신발/데크 관계를 포즈 Contact Sheet로 납품.

### 2.3 보드: 실제 데크 구조가 읽혀야 한다

- `BoardVisualRoot`: Deck(곡률 있는 노즈/테일과 concave), GripTape, DeckGraphic, TruckFront/Rear(베이스·행어), WheelFL/FR/RL/RR, Bearings 선택.
- **분리된 child hierarchy**: 보드 전체는 flip/yaw/pitch를 수행하고 휠은 그 아래에서 **독립적인 axle 회전**. 트럭과 바퀴가 공중 플립 중 데크와 분리되지 않음.
- 데크 기준축을 한 문서와 코드에서 고정: 로컬 길이 `+X`=nose, `-X`=tail; width `±Z`; vertical `+Y`; Kickflip=longitudinal X roll; Shuv/Tre=yaw around Y; Ollie=pitch around Z. `regular/goofy`, `travel sign`, flip direction 변환은 **물리 좌표계/비주얼 부호**를 별도 정의.
- 바퀴 반경은 모든 계산과 렌더에 하나의 값(m)을 쓰며, 4개가 동등한 위치/반경으로 배치된다. 접촉 프록시와 mesh bottom 높이 차이는 1cm 이하 목표.
- 주행 소리: 속도 연동 pavement roll, pop/clack, wheel landing clack, bail 사운드. 저작권 허용된 소리 또는 자체 합성 사용.

### 2.4 환경·맵 및 조명

**한 개의 작지만 완결된 서울풍 파크**를 제작한다. 정확한 장소 재현을 선행 조건으로 하지 않는다.

- Gameplay plane: 시작 평지 20~30m + 낮은 렛지 1개(측면과 상판 분리 충돌) + 완만한 뱅크(양방향 진입) + 충분한 복구 영역. collision geometry와 visual geometry를 구분하고 `SurfaceProfile`로 묶는다.
- Midground: 콘크리트 계단, 금속 난간, 안전 펜스, 가로수/수목, 가로등, 노면 표시. 실제 서울의 공공시설처럼 보이는 실루엣과 스케일.
- Background: 한강 건너편 낮은 건물, 다리 실루엣, 청명/노을 하늘 색 변화. 깊이 레이어 3개 이상(parallax 약하게), 플레이 충돌에는 관여하지 않는 decor.
- Material: PBR roughness variation, 데크 grip과 wheel urethane, concrete 노멀/roughness, 자연스러운 ambient bounce. 콘크리트 평면에 문양 없는 단색 material 하나만 허용하지 않는다.
- Lighting: URP main directional light 1개, soft-ish shadow(품질에 따라), ambient sky, baked/static props 우선. 과한 SSAO/실시간 light cluster는 모바일 프로파일 결과 후 판단.
- Particle/VFX: landing dust subtle, push gravel micro-puff, 실패 소량. 섬광과 screen-shake 과다 금지.
- 저작권: Kenney Mini Skate (CC0, https://kenney.nl/assets/mini-skate), Quaternius 사용 시 개별 CC0 라이선스, Poly Haven concrete (CC0, https://polyhaven.com/a/concrete) 등을 검토. **Kenney 원본만 설치한 저폴리 파크를 최종 아트로 간주하지 말고** 적합한 소재 선택·형상 변형·색/조명/구도를 통합한다. 다운로드 불가 또는 라이선스 불분명하면 자작 메시/재질 사용. `THIRD_PARTY_ASSETS.md`에 URL, 저자, 라이선스, 다운로드일, 실제 파일을 기록.

### 2.5 카메라와 화면 구성

- Orthographic side-follow 기본 유지, 3D 메시 깊이를 보이기 위한 조명과 소폭의 지면 z 레이어 활용. **측면 입력·트릭 판정의 정밀도에 영향 주는 Perspective 카메라로 임의 전환 금지.**
- 측면 라이더+보드가 일반 주행 시 safe gameplay height의 **22~34%**를 차지하도록 조정. 현재 고정 `orthographicSize=4.4`와 `y=3.25`를 상수로 복제하지 말고 기기 비율·모션 기준 조정.
- 진행 방향 전방이 플레이 가능한 가로폭의 **55~65%**가 되게 look-ahead; 정지·역주행·공중 체류는 damping, 쿼터/높은 장애물은 y tracking.
- 카메라는 Root와 별개의 CameraRig가 보간하고, Rigidbody를 Camera가 직접 이동시키지 않는다. `LateUpdate`에서 interpolate target; input visual latency를 측정.
- 상단 상태와 캐릭터의 bounding rect, 장애물 시작점, SafeArea/노치/홈 인디케이터 겹침 테스트.

## 3. Physics & Skate Feel — 하이브리드, 데이터와 접촉에 근거

### 3.1 모델 선택

**2.5D Kinematic/Hybrid Board Physics:** 하나의 물리 루트 Rigidbody + 단일 surface query + 보드/바퀴 visual rig + event-based trick controller. 진짜 4개의 rigid wheel joint/hinge와 rider ragdoll은 이번에는 도입하지 않는다. 그럼에도 플레이어에게는 *바퀴가 회전하고 땅을 밀고 데크가 팝되는 인과관계*가 보여야 한다. 따라서 `rolling`과 `visual`이 독립인 대신 **동기화되는 불변조건**을 구현한다.

- `FixedUpdate`는 이동/접촉/충돌 계산만. `Update` 터치 샘플과 상태 전이 명령 생성. `LateUpdate` 또는 `OnAnimatorIK/Animation Rigging`은 최신 상태의 프레임 시각화.
- `Rigidbody.linearVelocity.x`를 매 FixedUpdate 목표값으로 무조건 덮어쓰지 않는다. Ground friction, push impulse, air drag, signed travel speed, break/stop 제어를 이동 상태별로 처리.
- 기본 수치는 *튜닝용 최초 가설*이다. 기존 UX 밸런스를 고정 수치로 우기지 않는다.

### 3.2 이동 상태와 제안 수치

| 상태 | 입력/업데이트 | 제안 시작값 | 완료 조건 |
|---|---|---|---|
| Idle/Roll | 보드 바퀴 지지 + push impulse + ground rolling resistance | cruise ~4.5~6.0 m/s, max 8~9 m/s, roll resistance ~0.15~0.4 m/s² | PUSH 중 가속, 뗀 뒤 1초 안 급정거 없음 |
| Pushing | 뒷발을 땅에 내려 실제 뒤로 차는 자세/손 동작 + impulse | 1 push step 0.35~0.65s (튜닝) | 한 번의 push cycle마다 속도/발 움직임 일치 |
| Crouch/Pop | 양발 준비→tail pressure→상향 impulse, 제스처 release 커밋 | 초기 pop speed 4.2~5.2 m/s | jump 시작에 보드 tail/nose 동작이 관찰됨 |
| Air | 지면 지지 0, 중력 포물선; lateral momentum 유지 | gravity 프로젝트 설정 확인 | 보드가 공중에서 정지/순간이동하지 않음 |
| Catch/Landing | 보드 회전/몸/지면 충돌 조건 평가, normal 기반 지면 지지 | normal.y≥0.65 기본, 각도허용도 데이터 | 보드 수평·발 접촉·충격 흡수와 판정 일치 |
| Bail/Recover | 잘못된 충돌·회전 미완료·실패 → 자세·보드 이탈 연출 → 안전 재진입 | 복구 모션 0.7~1.0s | 화면 재시작 없이 재주행 가능 |

**주의:** 자유 주행 시 `pushHeld==false`는 브레이크가 아니다. 기존 prototype은 damping 0.12m/s²이라 수치상 급정지는 아니지만, *발 추진 모션/소리/휠 동작이 없어* 움직임이 지각되지 않는다. UI의 PUSH만 수정해도 해결되지 않는다.

### 3.3 바퀴 회전·접지의 필수 계약

- 각 wheel에 `WheelVisual` 컴포넌트/캐시된 `restLocalRotation`과 axle 방향 벡터를 둔다. **회전 누적각** `theta += (signed_ground_distance / radius)` [radian] 또는 `degrees += (signed_ground_distance / radius)*Rad2Deg`. `signed_ground_distance`는 실제 물리 Root의 고정 스텝 변위를 지면 tangent에 투영하며, 단순 `cruiseSpeed*deltaTime`이 아니다.
- wheel cylinder의 원본 Y축 → 적절한 rest orientation(Z 방향 axle) 변경 후 **rest rotation 위에 local axle spin을 합성**한다. Spin이 board flip의 회전을 취소하지 않게 parent hierarchy와 곱 순서를 테스트한다.
- 정지 0 속도에서 spin=0, 후진 시 부호 반전, 공중에서는 별도 inertia-decay 애니메이션 정책 (ground distance=0을 그대로 쓰면 급정지처럼 보일 수 있음). 바퀴/보드가 UI 터치와 독립으로 돌아가면 안 된다.
- Wheel contact probes(전/후 axle, 가능하면 4개 corner)와 board envelope로 지면 높이/법선을 표준화. support point와 렌더 wheel bottom 간 오차 **≤0.01m** 목표. 바닥 BoxCollider에 접촉하면서 휠이 3cm 떠 있는 현상 허용 금지.
- 지면 접촉 수치 표기: contactNormal, rootVelocity, signedDistance, anglePerWheel, visualBottomGap. `Development HUD`에서만 노출.
- 검증 샘플: radius 0.04m(실제 에셋 반경 기준 재측정)일 때 1m 주행 후 회전량 약 25rad≒3.98회전; radius 값은 실제 모델로 덮어써야 한다. 방향과 관찰 애니메이션이 함께 일치할 것.

### 3.4 Grounding/충돌/경사

- 기존 `BoxCollider`는 메인 캐릭터와 바닥을 지지하지만 wheel physically-accurate collider가 아님. 루트 collider를 skater collision envelope로 제한하고 wheel bottom과 surface plane을 일치시키는 보정/지면 추종자를 새로 둔다. visual만 떠다니는 보정으로 fake하지 않는다.
- 평지/상판/측면/경사는 다른 `SurfaceKind`로 판정한다. 수평 x 이동 중 기물 측면을 지나가지 않고, 위에서 진입하면 실제 상판에 안착해야 한다. 고속 이동에서 sweep/cast 사용해 터널링 회피.
- support hysteresis/coyote window **50~80ms**(튜닝), landing 재인식·충돌 중복 결과 방지. Wall hit/bail과 valid landing 분리. 오버랩되거나 지면 없는 reset은 안전 anchor로.
- 경사 표면에서는 gravity tangent 및 rolling resistance 적용; 완전한 차량 서스펜션은 금지. 디버그 튜닝 장면에서 평지, 10°/20° bank, ledge side/top, 0.15m drop을 모두 검증.

### 3.5 트릭이 '설득력 있게' 보이는 조건

- Ollie: crouch(시각 120~220ms) → 테일이 지면을 치고 노즈가 먼저 들림 → 앞발이 grip을 위로 쓸어 올림 → 보드 수평화 → 발 접촉 유지 → 착지 시 양 무릎 굴곡.
- Kickflip: Ollie 기반 pop + 앞발 외측 flick → 보드가 **로컬 X축 360°** 회전 → 중립 높이에서 발 이탈 → 회전 종료 시 발 catch → 착지. 상하/전후 방향에 따라 발 및 회전 부호 정의.
- Tre Flip: pop + 보드 X축 flip 360°와 Y축 shove 360° **동시에 합성**, 발이 실제로 보드에서 분리되고 재접촉; 몸이 보드와 같이 360° 빙글 돌지 않도록 스케이터 yaw 분리.
- 캐치는 **판정 결과 이벤트**와 일치해야 한다. 미완성 회전 시 `Catch=false`와 기계적 실패. 회전 완료 시 지면과 남은 시간 `catchWindow` 내에서 발 IK weight 0→1. 보드는 착지 충격에서 짧게 압축되되 지면 관통하지 않는다.
- 모든 트릭에 `MotionPhase`/`TrickSpec` 데이터로 타이밍을 넣는다. **전역 `Time.time`만으로 정상화된 고정 공중 지속시간을 추정하지 않는다.** 실제 비행 시간/접촉/신체 포즈는 이벤트 스트림과 연동.
- 성공률 RNG는 우선 실제 입출력 기계적 판정을 통과한 시도에 한해 1회; RNG 실패라면 물리적으로 원인을 느낄 수 있도록 balancing drift/unstable landing 파라미터를 기록·표현한다. 게임 결과와 연출이 다르면 FAIL.

### 3.6 물리 모델과 확률 모델의 책임 경계

1. `GestureIntent(trickId,quality,stance,travel,timestamp)` 생성 — 인식된 기술은 랜덤 변경 불가.
2. `TrickExecution` → pop·board angular action → contact state 기록.
3. `MechanicalJudge` → 회전잔량, 접촉 법선, board pitch/roll, catch 완료, 지면 상태 검사. 필수 물리 조건 실패면 바로 지정된 이유로 FAIL.
4. 유효한 후보에만 `SuccessProbabilityModel` 적용. `attemptId, probability, roll, outcome`이 일관된 단일 레코드로 생성됨.
5. `FeedbackPresenter`는 event record만 표현; 후처리 UI가 결과를 변경하지 않음.

실패 사유를 분리하는 것과 확률을 설득력 있게 만드는 것은 별개다. 특히 '입력 정확도 90%인 올리가 이유 없이 5번 연속 실패'가 플레이 체감을 해친다면 자동 보정/보호 장치를 *수치 실험으로* 검토하고, PvP에서는 RNG를 더 엄격히 통제한다. 기존 승률 숫자는 일단 유지·측정 후 수정.

## 4. UI/UX/HUD 재설계 — 게임 화면을 반드시 되찾기

### 4.1 정보 계층

**Level 1: 세계** — 스케이터·보드·다음 장애물. **Level 2: 행동 피드백** — 인식한 기술, Charge/Pop, 캐치·성공·실패 (짧은 미니 카드/타이포). **Level 3: 경기·진행** — S.K.A.T.E. 현재 글자·상대 제시 트릭·턴. **Level 4: 디버그/상세 수치** — 숨김.

- `GameHud` 기본 상태: 상단 좌측 작은 모드/플레이어, 상단 중앙 S.K.A.T.E. letter strip (대전 때만), 하단 중앙 위 트릭 성공/실패 한 줄 피드백 (최대 1.3초), 상단 우측 Pause 한 아이콘.
- `TrainingHud`: 선택한 트릭과 숙련도 progress micro-bar를 명확하게 표시하되, 모든 기술의 숙련 수치를 상단에 동시에 쏟아내지 않는다. 자세한 수치는 Pause → Skills 화면에.
- `TouchHud`: 큰 8방향 물리 joystick 휠을 제거하고 **왼쪽 하단 넓은 투명 스와이프 영역**으로 변경. 손가락 down 중에만 gesture preview/방향 마커가 뜨고 release에서 사라진다. 오른쪽 하단 PUSH는 엄지로 쉽게 닿는 작은 pill/ring (투명, 면적 충분)로 디자인. 터치 hit target은 시각적 버튼보다 넓게 한다.
- `DebugHud`는 개발용 토글 옵션(에디터 단축키/Development Build 메뉴)로만. `INPUT 77`, `SUCCESS CHANCE 20%`, 실시간 PlayerPrefs, 상태 메시지 전부 기본 HUD 금지. 내부 Logger/Profiler로 이동.
- `Menu/Pause`: Practice/Battle 전환은 Pause 패널에서. Reset은 확인 또는 작은 Retry flow; 게임 중 상시 상단 `RESET`·`BATTLE` 큰 두 버튼 금지.

### 4.2 화면 레이아웃 및 수치 가이드

- 실제 `Canvas` + TextMeshPro 또는 UIToolkit 런타임(선택 후 일관 적용). `OnGUI`로 상업 HUD 구현하지 않는다. 화면 safe area를 감안한 anchors/layout groups. 참조 해상도는 1920×1080 landscape, `MatchWidthOrHeight`/pixel-dp scale을 실기기 조정.
- `iPhone 15 Pro` landscape를 첫 목표로, 16:9/19.5:9/20:9 각 비율에서 텍스트 레이아웃·Touch hit rect·notch overlap 검사. 1180×2556 저장 녹화는 rotation metadata가 있을 수 있으므로 UI 배치는 `Screen.width/height`와 실제 SafeArea로 계산.
- 예시 컴포넌트 크기(1080px 세로 기준 1920×1080 캔버스): top gameplay HUD 총 높이 72~100px 수준, 기본 trick feedback 100px 아래, PUSH 가시 원형 지름 105~140px/터치 영역 최소 150px, 왼쪽 swipe overlay는 손가락 down 때만 표시. **고정값 강제보다 비율·접근성 우선**.
- 텍스트 한글/영어 폰트 fallback 검증, 가장 긴 실패 메시지와 S.K.A.T.E. 상대 기술명을 safe area 내 줄바꿈. 오디오/햅틱 off 시에도 시각적 피드백 충분.
- 색/스타일: off-white 글자, 따뜻한 slate 투명 칩, 청록 하나를 상태 강조에, 붉은색은 실패 메시지 한정. 과도한 neon/gradient UI와 faux-mobile joystick 금지.

### 4.3 상호작용 상태표

| 입력·상태 | 화면 반응 | 핵심 안전 장치 |
|---|---|---|
| Idle/Riding | HUD 최소, swipe zone 거의 투명 | UI 진입 터치는 게임 제스처로 처리하지 않음 |
| Trick touch down | 탭 위치 주변 조그만 charge ring/ghost path | 같은 pointer ID만 gesture 소유 |
| Move/selection | 기술 이름 preview (Ollie/Kickflip/Tre), 방향/품질 | 안정화 전에 잘못된 기술 점멸 금지 |
| Release | 위로 Pop, HUD 0.4~0.8초 선택 기술 표시 | cancel/OS 알림은 release로 간주하지 않음 |
| Air/catch | 별도 큰 HUD보다 보드/발 애니메이션과 clack/haptic | 가짜 성공 토스트 금지 |
| Success | 작고 선명한 'KICKFLIP · LANDED' + 상승한 숙련 표시 | 결과 중복 호출 시 한 번만 표시 |
| Failure | '회전 부족 / 캐치 실패 / 착지 불안정' + 포즈·사운드 | 잠깐 후 자연스럽게 retry 가능 |
| Battle/set/match | 상단 S-K-A-T-E progress, 턴과 필요 트릭 | 실패/성공에 따라 정확한 점수 변동, 경기 끝에서 입력 잠금 |
| Pause/modal | 입력 모두 cancel, push off; resume 시 재획득 | 뒤 배경 터치로 trick 실행 안 됨 |

## 5. 시스템 아키텍처 / 폴더·타입·단일 소유권

현재 1개 `SkaterSeoulPrototype.cs`가 물리·터치·시각·UI·저장·배틀을 모두 처리한다. 이번 사이클에서 **최소한 아래 책임은 분리**한다. 분리했다고 새로운 기능을 추가하는 것은 아니다.

```text
Assets/SkaterSeoul/
  Runtime/
    Domain/SkateState.cs                // Rolling/Push/Crouch/Air/Catch/Land/Bail
    Domain/TrickSpec.cs                 // per-trick durations, target rotations, catch windows
    Input/TouchGestureController.cs     // existing controls semantics, pointer ownership
    Physics/SkateMovementController.cs // push/drag/brake/ground/air, Rigidbody
    Physics/SurfaceContactSolver.cs    // ground, wheel probes, ledge side/top, slopes
    Physics/TrickExecutionController.cs// input intent -> trick state timeline
    Physics/TrickLandingJudge.cs       // mechanical then probability, exactly once
    Presentation/SkateboardVisualRig.cs// deck/truck/wheel refs and rolling spin
    Presentation/SkaterAnimationBridge.cs // Animator + foot IK + push/catch/landing
    Presentation/SkateCameraController.cs // forward lead + look Y + safe framing
    UI/GameHudController.cs
    UI/GesturePreview.cs
    UI/BattleHudController.cs
    Audio/SkateAudioController.cs
  Config/SkateTuning.asset, VisualStyle.asset, TrickSpecs/*.asset
  Prefabs/SkaterRoot.prefab, RiderVisual.prefab, BoardVisual.prefab,
          GameHud.prefab, SkateparkVerticalSlice.prefab
  Scenes/SkaterSeoul_VisualSlice.unity
  Editor/SkaterSeoulSceneBuilder.cs, SkaterSeoulValidation.cs
  Tests/EditMode/, Tests/PlayMode/
  Art/Characters/, Art/Board/, Art/Environment/, Art/Materials/, Art/Lighting/
  docs/VISUAL_OVERHAUL_QA.md, docs/THIRD_PARTY_ASSETS.md
```

폴더명은 기존 프로젝트 구조와 충돌하지 않도록 조정 가능하다. 그러나 *동일 책임을 두 곳에서 수행하거나* `SkaterSeoulPrototype.cs`를 옛 코드 그대로 둔 채 새 컨트롤러도 움직이게 하면 **실패**. 어댑터를 남기더라도 dispatch만 맡긴다.

### 핵심 데이터 계약

```csharp
// 의사 계약: 바로 복사해 컴파일해야 하는 완성 구현 아님
record struct TrickIntent(int AttemptId, SkateTrick Trick,
    float InputQuality, SkateStance Stance, TravelDirection Travel, double Timestamp);
record struct SkateSnapshot(Vector3 Position, Vector3 Velocity,
    bool Supported, Vector3 SurfaceNormal, float GroundDistance,
    MotionPhase Phase, float PhaseTime, float BoardFlipDeg, float BoardYawDeg);
record struct LandingSample(int AttemptId, float RemainingFlipDegrees,
    bool Caught, Vector3 ContactNormal, float PitchErrorDegrees,
    float SpeedAtContact);
record struct AttemptResult(int AttemptId, bool Succeeded,
    FailureType Failure, float Probability, float Roll, LandingSample Sample);
```

- 물리 판정은 `AttemptResult`를 *한 번만* 발행한다.
- 시각과 HUD는 읽기 전용 snapshot / result만 구독한다.
- PlayerPrefs 키 `SS_Mastery_*`는 이 사이클에서 유지한다. 구 프로젝트 39 ID와 3기술 인덱스를 혼동하지 않는다.
- 현 S.K.A.T.E. 로컬 규칙과 Practice/Battle toggle UI 진입은 가능해야 한다. **매치 도메인 로직을 무단 리디자인하지 않는다.**
- `SkaterSeoulSceneBuilder`는 아트 프리팹 참조를 통해 고품질 씬을 생성한다. 에셋 미설치 시 에디터에 명시적인 오류와 설치 가이드를 표시하고, 큐브 씬을 새 정상 결과물로 생성하지 않는다. 레거시 MVP builder는 디버그용으로 이름 변경·격리 가능.

## 6. 실행 순서 — 한 번의 Codex 작업에서 P0~P5 계속 진행

**P0 Baseline & capture:** 현재 로컬 Unity 프로젝트 루트(Assets/Packages/ProjectSettings), git status, Unity version, URP pipeline asset, target scene, 녹화의 재현 동작 확인. baseline gif/png 3컷 저장. 주요 파일 소유권/기존 씬·저장 스냅샷 생성. 레포만 존재한다면 소스 팩과 실제 Unity 프로젝트를 분리해 기록.

**P1 Board actually rolls:** 4 wheel refs / signed-distance rotation / contact probe / push state / coast / braking (있다면) 구현. 간단한 테스트 씬에서 0→5m/s→release→jump→land→reverse 테스트. Physics 회귀 통과 전 아트만 바꾸고 끝내지 않음. `WheelVisual`의 각도 텔레메트리 기록.

**P2 Rider & trick mechanics:** placeholder rider 교체, 리그·IK 설치, push cycle, 3기술 phase animation, 실제 착지 상태/기계적 판정 연결, bail 3종. 3기술 × 성공/실패 contact sheet를 캡처. 기존 배틀/숙련 저장 보존.

**P3 Environment & camera:** 콘크리트 질감, 도로/도시 3-depth, 실제 그림자, 라이팅 톤 매핑, 전경 씬 구성, 카메라 lead & y tracking. *Skater만 예쁘고 배경은 민트색 빈 화면이면 fail.*

**P4 HUD + feel:** Runtime Canvas + gesture preview + PUSH 버튼 축소·레이아웃 개선, 상단 디버그 제거, S.K.A.T.E. 턴 표시, 실감 나는 wheel/pop/land/bail SFX + 선택적 haptics. UI 상태마다 safe area 검사.

**P5 QA & delivery:** Editor EditMode/PlayMode tests, build, 10초 연속 주행 및 3기술/3실패 capture, 2개 이상 종횡비 스크린샷, 1개 실제 iPhone 런타임 영상(기기 접근 가능할 때), draw calls/memory/frame timing, 버그 수정. 결과를 정직하게 PASS/FAIL/NOT RUN으로 문서화.

**실행 원칙:** P0~P5는 한 번의 PRD 작업 단위다. Codex는 단계별 산출과 검증을 마치며 가능한 만큼 계속 구현한다. 허용되지 않는 라이선스, 외부 에셋 자동 다운로드 불가, Unity 설치 부재는 명시적 blocker로 남긴다. *모든 미달을 숨기고 '완료'만 출력하는 것을 금지한다.* 이 문서만 작성하거나 새 PRD를 또 쓰는 것으로 마무리하지 않는다.

## 7. 정량 Acceptance Gates

| Gate | 합격 기준(목표) | 방법 |
|---|---|---|
| G00 실제 타깃 | Codex가 실제 Unity 6 URP 프로젝트 경로와 씬을 확인하고 수정 | `ProjectSettings/ProjectVersion.txt`, URP asset, 작업 로그 |
| G01 그래픽 | 기본 주행 화면에서 보이는 큐브/구 모양 사람·단색 큐브 도시·거대 민트 배경 없음, 허용 에셋 기반 캐릭터/보드/재질/그림자 적용 | 구도 동일 캡처 BEFORE/AFTER 육안 QA |
| G02 휠 회전 | 바닥 1m 주행 시 wheel rotation≈1/r rad, reverse 부호 반전, 0m/s에서 spin0 | telemetry Edit/PlayMode assertions + 60fps 영상 |
| G03 접지 | 지면 위에서 4개 wheel bottom과 support surface 시각 오차≤0.01m, clip/float 0 (허용 오차 내) | contact point debug gizmo + flat/slope footage |
| G04 주행 손맛 | HOLD PUSH 시 다리 푸시 모션과 가속 동시. release 코스팅, 수평속도 연속성. landing 후 갑작스러운 정지/teleport 없음 | speed-time CSV/graph + game recording |
| G05 트릭 동작 | Ollie/Kickflip/Tre 각각 crouch/pop/flick/board spin/catch/compress가 구분되고 발 IK 적용 | 각 6~8 key times의 이미지 contact sheet와 60fps 캡처 |
| G06 판정 일관성 | 착지 전 확률/실패를 UI로 확정 발표하지 않음, mechanical invalid는 랜덤 회복 안 됨, 유효 시도 1 draw/1 result, 3실패 motion 다름 | seeded test + 판정 event log |
| G07 HUD | 기본 Play HUD world-screen 가림을 크게 줄이고 큰 디버그 패널/8방향 휠 제거. 타임라인 feedback, 반응하는 터치 패드, notch safe | UI screenshot at 16:9/19.5:9, 두 손가락 30 trace |
| G08 S.K.A.T.E | Practice/Battle 전환, bot setter/defender, five-letter end, duplicate landing guard, mastery 저장·재실행 후 유지 | 기존 회귀 테스트 + 정상 플레이 |
| G09 성능 | iPhone 15 Pro 60fps 목표(10분 지속, p95 frame≤16.7ms)·중급 기기 30fps fallback. 실제 측정 미수행 시 `NOT RUN` | Unity Profiler + iOS frame pacing·thermals |
| G10 아트·권리 | 재사용 에셋 파일마다 출처·명시 라이선스·사용 여부 기록, 원작 추출 파일 없음 | third-party ledger + 디렉터리 감사 |
| G11 결과 보고 | 실제 빌드 씬·스크린샷 before/after 4쌍·10초+영상·QA표 + 남은 문제 | `docs/VISUAL_OVERHAUL_QA.md` |

**실패 정의:** UI만 변경, 바퀴 spin만 추가, 3D 인형/환경을 그대로 두는 경우; Asset Store 스크린샷만 PRD에 붙이는 경우; Unity 컴파일만으로 물리/접지/실기기 통과 주장; 상업 사용 권리 확인 없이 원작 모형·소리를 포함한 경우.

### Visual Regression Capture Matrix

| 테스트 컷 | 필수 확인점 |
|---|---|
| ① Ride at 5m/s | 바퀴 연속 회전, 접지, 전체 신체 비례, 나무/건물/하늘 깊이, 무해한 HUD |
| ② Push frame | 한 발 바닥 contact, 어깨 기울기, 몸 중심 균형, 순간 가속 |
| ③ Ollie apex | 데크 수평화, 무릎/발 접촉, 그림자 높이 |
| ④ Kickflip 90°/180°/270°/Catch | 데크 앞/뒤/엣지 식별, 보드/발 분리, spin 축 일치 |
| ⑤ Tre Flip yaw+flip | 보드는 복합 회전, 캐릭터는 의도되지 않은 yaw 동반 회전 없음 |
| ⑥ Landing | 두 발 그립에 접촉 + 압축 + wheels ground, 속도 연속 |
| ⑦ Three bail reasons | 회전 부족/캐치 실패/착지 불안정이 포즈만으로 구별 |
| ⑧ Battle | 상대 기술명·S.K.A.T.E. 글자·턴 구분; world-screen 넓게 유지 |
| ⑨ Reverse/Goofy | 보드 시각/스핀/안전 접촉이 mirrored direction에서도 올바름 |

## 8. 반드시 포함할 기술 회귀 테스트

- **Wheel:** signed distance ±1m and 0m, radius 0.035~0.055m parameterized, 30/60/120 rendering fps에서 누적값 동일, 공중 inertial spin policy, 부모 보드 X roll/Y yaw 중 wheel spin axis 일관.
- **Collision:** 평지/경사/ledge side/top, 0/2/6/9m/s, 공중 충돌/중복 contact, 착지 정상/회전 부족/노캐치/넘어짐, multi-collider hierarchy와 root 충돌 ownership.
- **State:** Roll→Push→Roll→Crouch→Pop→Air→Catch→Land→Roll, Roll→Air→Bail→Recovery, pause/cancel/resume, mode switch after bail, gameover input lock.
- **Input:** left swipe zone + right PUSH 같은 순간, finger IDs 교환, UI/일시정지 중 touch, cancellation without commit, 현재 3기술의 기존 인식 의미 보존.
- **Persistence:** SS_Mastery_0..2, practice/battle toggle, reset, duplicate result, 이상 값 복구. 레거시 39트릭 ID/저장 충돌 금지.
- **Render:** URP shader import, mobile Metal/iOS orientation, safe area, 16:9/19.5:9/20:9에서 visual clipping/anchors, dynamic resolution/texture compression.

## 9. Codex 최종 출력 형식(필수)

```markdown
# VISUAL OVERHAUL — Implementation Report
- Repo/local Unity project branch + pre-change commit:
- Modified files / created assets + license references:
- Baseline screenshot/video path:
- Build target, Unity version, URP configuration:
- P0..P5: actual implemented/partial/blocked:
- Gates G00..G11: PASS/FAIL/NOT RUN with evidence:
- Wheel rotation test: wheel radius, 1m expected rad vs measured:
- Ground probe test: height, normal, contact gap:
- Trick visual contact sheets: Ollie/Kickflip/Tre/Bail:
- UI layouts: 16:9 / 19.5:9 / iPhone safe area:
- Runtime profile: device + p95 frame + thermals, or NOT RUN:
- Existing gameplay regression test results:
- Remaining 5 most severe defects:
- Play the new Scene: exact Editor menu/scene path:
```

**최종 우선순위:** 1) 실제 구름과 접지 2) 리깅/3기술 모션 3) 서울풍 환경·빛 4) HUD 5) 객관적 실기기 검증. **임시 박스 월드 + 바퀴가 안 도는 화면을 완성품으로 다시 제출하지 말 것.**

## 10. 문헌·레퍼런스와 확신 수준

- 플레이 감각·아트 참고: Skate City 공식 presskit https://skatecitygame.com/skatecity/presskit/ ; Skate City: New York https://skatecitygame.com/newyork/presskit/ — *시각적 레퍼런스만. 독점 게임 코드는 미확보.*
- Unity Rig/IK 공식: https://docs.unity.cn/Packages/com.unity.animation.rigging@1.3/manual/constraints/TwoBoneIKConstraint.html
- Unity 6 URP mobile 최적화: https://docs.unity.cn/6000.1/Documentation/Manual/urp/configure-for-better-performance.html
- CC0 스케이트 에셋 후보: https://kenney.nl/assets/mini-skate / https://kenney-assets.itch.io/mini-skate
- CC0 콘크리트 표면: https://polyhaven.com/a/concrete
- 현재 코드/검증 사실: https://github.com/osteoclasts-arch/seoulskate (main의 `Assets/SkaterSeoul` 및 `docs/`)

오픈소스/무료 에셋 자료는 후보이며 자동 다운로드 성공, 화질 적합성, Unity URP 전환, 수정 가능한 rig, 라이선스 적합성은 **실제 도입 시 파일 단위로 검증**한다. 모션과 물리 수치 가운데 직접 장치에서 실측한 Skate City 값은 없다. 본 문서의 숫자는 최초 품질 게이트 및 튜닝 가설이다.
