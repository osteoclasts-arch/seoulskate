# SKATER SEOUL — Legacy Migration Audit

감사일: 2026-10-23  
기준 제품 명세: docs/SKATER_SEOUL_PRD_v1.0.md  
결과: 소스와 규칙을 확인했고, 이번 감사에서는 코드를 이식하지 않았다.

## 범위와 확인한 저장소

- 대상: 현재 작업 폴더의 SKATER SEOUL source pack. 이 폴더에는 ProjectSettings, Packages, Unity 씬이 없고 Git 메타데이터도 없다. 기존 작업 백업은 .backups/skater-seoul-prechange-20261023.tar.gz에 있다.
- 레거시: /Users/osteoclasts_/Documents/deepseek-harness/스케이터키우기. Git HEAD는 1e168ba이고 감사 시점에 tracked 변경은 없었다. 이 저장소는 읽기와 테스트만 했다.
- 레거시 Unity 버전: 6000.3.10f1. Tools/CoreTests/CoreTests.csproj는 Assets/Scripts/Core/**/*.cs를 직접 포함하며 .NET 10.0 콘솔 프로젝트다.
- 이 폴더의 README, CODEX_NEXT_STEPS.md, PRD, 구현 현황과 테스트 보고서를 읽었다. README는 source pack을 별도 Unity 프로젝트로 옮기지 말고 수신 프로젝트가 정해질 때까지 보존하라고 한다.

## 요약 판단

1. 보존 가치가 높은 순수 도메인 코드가 있다. Trick/TrickCatalog, Judge/Tuning/TrickSuccessModel, SkateTouchSession, SkateMatch, Career/Missions가 Core 테스트 프로젝트에서 UnityEngine 없이 컴파일된다.
2. 순수 도메인 후보는 있지만 지금 통째로 연결할 낮은 위험의 완성 모듈은 없다. 새 스타터는 SkateTrick 3개와 자체 판정·숙련도 저장을 이미 사용한다. 레거시를 함께 연결하면 기술 ID, 확률 판정, 저장, S.K.A.T.E. 상태의 중복 소유자가 생긴다.
3. 현재 스타터의 MVP가 PRD상 P0 게이트를 완전히 통과하기 전에는 이식하지 않는다. 구현 현황 문서는 P0를 PARTIAL로 기록한다.
4. 레거시 CoreTests는 실행했다: 385 passed, 0 failed. 붙여넣은 내용의 346개 기록보다 현재 소스가 39개 더 많다. 이 결과는 레거시 순수 Core와 기존 시뮬레이션에만 해당하며 Unity import, 새 프로토타입, Progress 저장 마이그레이션, 입력 장치, 모바일 실행을 검증하지 않는다.
5. 이식이 시작되면 기존 39개 ID를 유지하고 MVP에는 Ollie, Kickflip, Tre Flip만 노출한다. 정수 인덱스를 새 3개 배열에 직접 복사하거나 캐스팅하지 않는다.

## 파일별 분류

분류 의미: KEEP은 도메인 구현을 보존해 단계적으로 사용, ADAPT는 규칙을 유지하되 새 경계에 맞춤, REBUILD는 동작·계약만 참고해 새 구현, DISCARD는 MVP에 가져오지 않음.

| 레거시 파일 | 분류 | 재사용 가능한 내용과 연결 위치 | 주요 위험 |
|---|---|---|---|
| Assets/Scripts/Core/Types.cs | KEEP | Trick enum, 6개 RiderStats, Stance, 실패·시도·결과 타입. 도메인 어셈블리의 안정 ID 계약으로 사용 | Trick enum 순서가 저장 인덱스다. 새 SkateTrick 순서는 Ollie=0, Kickflip=1, TreFlip=2지만 레거시는 Ollie=0, Kickflip=3, TreFlip=24다 |
| Assets/Scripts/Core/TrickCatalog.cs | KEEP | 39개 ID, 한국어/영어명, 카테고리, 티어, 점수, 입력 조합 문법과 기본 관찰 규칙. 도감·판정·진행도에서 단일 catalog로 사용 | 이름을 잘못 연결하거나 ID 순서를 바꾸면 저장 숙련도와 잠금이 다른 기술에 붙는다 |
| Assets/Scripts/Core/Judge.cs | ADAPT | 물리 샘플에 대한 결정론적 실패 사유와 점수·품질 계산. 3D 착지 이벤트가 만든 LandingSample의 유일한 판정 진입점 | 새 3D 충돌 정보를 옛 샘플로 잘못 번역하면 시각 동작과 판정이 어긋난다. 물리 실패를 확률로 뒤집어서는 안 된다 |
| Assets/Scripts/Core/Tuning.cs | ADAPT | 팝·회전·착지·위험도 조정값과 확률 모델의 초기 가설 | 수치는 기존 밸런스 제안이다. PRD의 새 3D 튜닝으로 검증 없이 확정하지 않는다 |
| Assets/Scripts/Core/TrickSuccessModel.cs | ADAPT | 기계적 후보 판정이 성공인 경우에 한 번만 확률을 적용하고, 실패를 UnstableLanding으로 기록하는 분리 구조 | 확률 입력에 사용되는 능력치가 물리에도 쓰인다. 적용 계약을 정하지 않으면 효과가 이중 반영된다 |
| Assets/Scripts/Core/SkateTouchInput.cs | ADAPT | 8방향 선택, 0.10초 방향 안정화, 모션 임계치, 손가락 소유권, 시작/이동/취소/놓기와 상태별 역할 | 단일 연락처 세션이다. 현재 source pack의 별도 트릭 패드와 PUSH 동시 터치 동작을 그대로 대체하지 못한다 |
| Assets/Scripts/Game/TouchInput.cs | DISCARD | Unity 입력 계층 참고만 가능. EventSystem UI 차단 및 입력 시작/이동/종료 전달 구조 | 레거시 UnityEngine.Input 기반이다. 새 Input System과 동시에 폴링하면 중복 입력 가능 |
| Assets/Scripts/Core/SkateSim.cs | REBUILD | 팝·공중·착지·베일의 순서, 결과 한 번 발행, seeded random, 재시도 흐름을 동작 기준으로 참고 | 이전 2D 자체 시뮬레이션이다. Rigidbody 기반 3D 물리와 함께 실행하면 서로 다른 위치·판정·상태가 된다 |
| Assets/Scripts/Core/FlowSim.cs | REBUILD | 고정 틱 입력 큐, 명령 순서, 이벤트·attempt ID 중복 방지, 일시정지 처리 | 전체 이동·표면·충돌 구현이 구형 사용자 정의 물리다. 새 3D 이동 엔진으로 가져오지 않는다 |
| Assets/Scripts/Core/RunSession.cs | ADAPT | 고정 틱과 정렬된 입력 명령, sequence 중복 거부, pause 시 입력 취소 | 현재 Rigidbody 실행과 이중 시계가 되지 않도록 새 물리 틱과 합치거나 도메인 이벤트 큐만 취한다 |
| Assets/Scripts/Core/TrickMotion.cs, TrickPreviewSimulation.cs | REBUILD | 트릭별 회전 목표, 페이즈 분리와 프리뷰 개념 | 이전 2D 렌더링에 가까운 샘플링·좌표 가정이다. 3D 리그와 이동 물리에 직접 연결하지 않는다 |
| Assets/Scripts/Core/Spot.cs, SurfaceQuery.cs | REBUILD | 지형 규칙과 표면 접촉 개념 참고 | 새 MVP는 평지 레인부터 시작한다. 이전 스팟 좌표, 레일, 램프 판정을 옮기지 않는다 |
| Assets/Scripts/Core/SkateMatch.cs | KEEP | 로컬 봇의 setter/matcher, 성공·실패 글자, 불일치, 턴 제한과 종료 규칙 | 현재 prototype의 즉시 처리 방식 및 UI 표현과 연결 시 턴이 두 번 전진할 수 있다 |
| Assets/Scripts/Core/FriendSkateMatch.cs | KEEP, 후순위 | UI/네트워크와 분리된 친구 대전 규칙, 상태 스냅샷 검증, attempt/turn/rule version 확인 | 주석에서 peer-host가 client sample/profile을 신뢰한다고 명시한다. 랭크 보상에는 서버 재시뮬레이션이 필요하다 |
| Assets/Scripts/Game/FriendMatchTransport.cs | DISCARD, 후순위 검토 | 향후 연결형 친구 대전에서 프로토콜 요구 사항만 참고 | Unity.Netcode 및 Unity Services Authentication/Relay 의존성이 있으며 오프라인 MVP 범위 밖이다 |
| Assets/Scripts/Core/Career.cs | KEEP, 후순위 | 순수 C# 12개 챌린지 정의, 목표·별·완료 판정과 보상/해금 규칙 | 새 MVP에는 코스, 그라인드, 39개 트릭이 없다. 현재 3종으로 그대로 표시하면 완료할 수 없는 챌린지가 생긴다 |
| Assets/Scripts/Core/Missions.cs | KEEP, 후순위 | 결정론적 날짜별 미션 선택, 아홉 후보, 진행·완료 판정 | 날짜와 보상 정책은 현지 날짜 변경의 영향을 받는다. PlayerPrefs 저장과 함께 복사하지 않는다 |
| Assets/Scripts/Game/Progress.cs | ADAPT, 후순위 | 성장 규칙, 스탯 상승, 관찰/학습/숙련, 커리어·미션 보상 중복 방지 | PlayerPrefs, JsonUtility, Mathf 사용. version 13 로드가 데이터를 정규화한 뒤 자동 Save한다. 새 앱에서 무심코 Load하면 기존 저장을 다시 쓸 수 있다 |
| Assets/Scripts/Game/GameRoot.cs | DISCARD | 현재 게임의 호출 순서와 레거시 도메인 접합부를 참고 | 2,079줄 단일 조정자이며 상태·저장·입력·모드가 새 prototype과 중복된다 |
| Assets/Scripts/Game/SkaterView.cs, WorldView.cs, FxView.cs, HudView.cs, MenuUI.cs, GameBootstrap.cs | DISCARD 또는 화면별 재작성 | 게임 규칙과 무관한 시각·메뉴 요구만 참고 | 픽셀 렌더링·카메라·기존 씬 참조를 3D URP에 이식하지 않는다 |
| Tools/CoreTests/Program.cs | ADAPT | 검증 기준과 회귀 시나리오. 현재 .NET 테스트 결과는 옛 동작의 기준선 | Unity Test Framework가 아니며 레거시 시뮬레이터를 많이 검증한다. 테스트 통과를 새 3D 검증으로 표시할 수 없다 |

## 확인한 규칙과 실제 구현 차이

### 트릭 ID, 난이도, 해금

Types.Trick의 정수값이 저장 인덱스이며 TrickCatalog는 같은 순서로 정확히 39개 사양을 가진다. catalog 자체에는 난이도 필드가 없다. 개별 티어 0–3에서 TrickSuccessModel이 difficulty = tier × 100/3으로 환산한다. points는 점수 기준이다.

| ID | Trick enum | 종류 | 티어 / 점수 |
|---:|---|---|---:|
| 0 | Ollie | Air | 0 / 100 |
| 1 | Frontside180 | Air | 1 / 220 |
| 2 | Backside180 | Air | 1 / 220 |
| 3 | Kickflip | Flip | 1 / 300 |
| 4 | Heelflip | Flip | 1 / 300 |
| 5 | Shuvit | Flip | 1 / 260 |
| 6 | Impossible | Flip | 2 / 420 |
| 7 | FiftyFifty | Grind | 1 / 280 |
| 8 | BacksideFlip | Flip | 2 / 460 |
| 9 | Hardflip | Flip | 2 / 460 |
| 10 | Frontside360 | Air | 2 / 520 |
| 11 | Backside360 | Air | 2 / 520 |
| 12 | FrontsideFlip | Flip | 2 / 480 |
| 13 | Boardslide | Grind | 2 / 380 |
| 14 | Nosegrind | Grind | 2 / 380 |
| 15 | FiveO | Grind | 2 / 360 |
| 16 | Crooked | Grind | 3 / 420 |
| 17 | InwardHeelflip | Flip | 3 / 520 |
| 18 | Frontside360Shuvit | Flip | 3 / 560 |
| 19 | Backside360Shuvit | Flip | 3 / 560 |
| 20 | ImpossibleFrontside180 | Flip | 3 / 600 |
| 21 | ImpossibleBackside180 | Flip | 3 / 600 |
| 22 | VarialKickflip | Flip | 2 / 480 |
| 23 | VarialHeelflip | Flip | 2 / 480 |
| 24 | TreFlip | Flip | 3 / 650 |
| 25 | LaserFlip | Flip | 3 / 650 |
| 26 | Lipslide | Grind | 2 / 400 |
| 27 | Noseslide | Grind | 2 / 380 |
| 28 | Tailslide | Grind | 2 / 380 |
| 29 | Bluntslide | Grind | 3 / 440 |
| 30 | Nosebluntslide | Grind | 3 / 460 |
| 31 | Smith | Grind | 3 / 440 |
| 32 | Feeble | Grind | 3 / 440 |
| 33 | Overcrook | Grind | 3 / 460 |
| 34 | Darkslide | Grind | 3 / 500 |
| 35 | Hurricane | Grind | 3 / 480 |
| 36 | Salad | Grind | 3 / 440 |
| 37 | Suski | Grind | 3 / 440 |
| 38 | Willy | Grind | 3 / 460 |

새 저장을 정규화하면 기본 관찰 트릭은 Ollie, Frontside180, Backside180, Kickflip이고, 학습 완료는 Ollie다. 관찰 후 LearnTrick으로 학습한다. 커리어 완료는 다음 트릭을 관찰 대상으로 해금한다.

| 챌린지 | 장소 / 목표 | XP | 관찰 해금 |
|---|---|---:|---|
| g1 | gap / Frontside180 1회 | 40 | Heelflip |
| g2 | gap / Kickflip 1회 | 50 | Shuvit |
| g3 | gap / Shuvit 1회 | 60 | Impossible, FiftyFifty, VarialKickflip |
| d1 | drop / Heelflip 1회 | 70 | BacksideFlip |
| d2 | drop / 베일 없이 3연속 | 80 | Hardflip, Frontside360, Backside360, VarialHeelflip |
| d3 | drop / 단일 점수 500 | 90 | FrontsideFlip |
| l1 | ledge / FiftyFifty 1회 | 100 | Boardslide, Lipslide, Noseslide, Tailslide, Darkslide, Hurricane |
| l2 | ledge / Boardslide 1회 | 110 | Nosegrind, Bluntslide, Nosebluntslide, Salad, Suski |
| l3 | ledge / 누적 그라인드 3초 | 120 | FiveO, Crooked, Smith, Feeble, Overcrook, Willy |
| q1 | quarter / 단일 점수 700 | 130 | InwardHeelflip, TreFlip |
| q2 | quarter / FS 또는 BS 360 1회 | 150 | Frontside360Shuvit, Backside360Shuvit |
| q3 | quarter / 서로 다른 트릭 5개 | 200 | ImpossibleFrontside180, ImpossibleBackside180, LaserFlip |

Missions.cs는 후보 9개 중 날짜 문자열로 3개를 결정론적으로 선택한다. mission record의 claimed로 XP 보상 중복 수령을 막는다. pasted text의 “일일 미션 3개”는 표시 개수로는 맞지만, 구현은 9개 후보 pool이다.

### 입력, 스탠스, 능력치

- SkateTouchSession은 Begin에서 입력을 소유하고 초기 선택을 만들며, Move에서 방향 안정화 뒤 미리보기 선택을 바꾸고, End에서 취소되지 않았을 때만 커밋한다. 다른 fingerId는 무시하며 UI 시작 입력과 Cancel은 커밋되지 않는다.
- 기본 방향 문법은 ↑ Ollie, ↗ Kickflip, → Shuvit, ↘ TreFlip, ↓ Hardflip, ↙ VarialHeelflip, ← 미지원, ↖ Heelflip다. 실제 배열에서 6번(←)의 Trick은 null이라 실행할 수 없다. 사용자 제공 8방향 표의 FS Shove-it은 이 저장소에서 구현된 기술 매핑이 아니다.
- Special 입력은 별도 배열이며 일부 이름은 catalog Trick이 아니라 null이다. 화면에 표시되는 설명만으로 실행 가능하다고 간주하면 안 된다.
- Stance는 Regular, Switch, Fakie, Nollie이고 자연 발 자세 NaturalStance는 Regular/Goofy로 분리된다. TrickMotion은 이 값에 따라 부호와 회전 목표를 바꾼다.
- RiderStats 순서는 Pop, Balance, Speed, Landing, Courage, Agility이며 0–100이다. Judge는 Pop/Speed/Agility로 점프·속도·회전 지속시간을 바꾸고 Balance/Landing으로 허용 오차를 계산한다. TrickSuccessModel의 확률 보정도 기술 분류에 따라 이들 값을 다시 포함한다. 예: Flip 확률은 Agility와 Landing 평균을 쓴다. 새 계약에서 물리 효과와 확률 효과를 분리해야 한다.
- Growth stage는 코드에서 미관찰 → 관찰 → 학습 → 숙련으로 계산된다. 별도 Practiced 저장 상태는 없다. 숙련 판정은 학습 상태이며 mastery가 60 이상이다. 제공된 문서의 “80 이상, 두 상황에서 고품질”은 이 소스의 규칙이 아니다.

### 물리 판정과 확률

Judge는 입력된 LandingSample에서 팝 시점, 회전 완료, 착지 피치, 그라인드 접촉 등 기계적 조건을 결정론적으로 확인한다. TrickSuccessModel.Apply는 기계적 Verdict가 성공일 때만 외부에서 받은 단일 roll과 success probability를 비교하고 확률 실패는 UnstableLanding으로 기록한다. 옛 FlowSim은 attempt ID, seed, roll을 결과에 남기며 테스트는 기계적 실패가 확률 판정보다 먼저 처리되는 경우와 반복 입력/결과 방지를 포함한다.

Tuning의 54% 기준 및 99.5% 상한은 주석상 초기 성공 모델 가설이다. 테스트 실행 중 기준 사례는 54.01%, 숙련 사례는 99.59%로 출력됐다. 밸런싱 목표를 확정한 결과로 취급하지 않는다.

초기 감사 당시 SkaterSeoulPrototype.BeginTrick은 이륙 순간 확률을 한 번 굴리고, 실패 사유를 정할 때 추가 난수를 최대 두 번 더 굴렸다. 후속 이식에서 판정을 착지 시점으로 옮겼다. 입력 하한 및 방어 중 오답은 난수를 쓰지 않고 실패 처리하며, 나머지 착지는 난수 1개로 성공 여부와 실패 유형을 함께 정한다. 현재 3D MVP에는 회전·캐치에 대한 독립된 물리 샘플이 없으므로, 레거시 Judge 전체를 옮긴 것으로 간주하지 않는다.

### 성장, 커리어, 대전, 경제

- Progress는 기술별 mastery/observed/learned/attempts 39칸, 여섯 능력치, 12 커리어 기록, 일일 3 미션, 레벨/XP, 설정 및 누계를 저장한다.
- 성공은 기술 티어에 따라 mastery 3+tier, 실패는 1 상승한다. 성공 XP는 점수와 티어에 따라, 실패도 낮은 XP를 준다. 스탯 증가는 기술 종류에 따라 다르며 성공에 연결된다.
- career의 completed와 별 수는 최대값으로 기록한다. XP 지급은 새 완료에만 붙는다. missions는 claimed를 먼저 기록해 반복 청구를 방지한다.
- SkateMatch는 3글자/5글자 패배 규칙, 15초 턴, player/bot set/match 상태, 잘못된 기술, timeout, bot persona를 가진 순수 규칙 엔진이다. current prototype의 즉시 처리보다 명시적인 상태 전이다.
- FriendSkateMatch는 두 사용자 규칙과 버전 2 보고서 검증이 있지만 FriendMatchTransport는 Netcode/Relay/Authentication 의존성이다. peer-host가 클라이언트 샘플을 신뢰한다는 코드 주석이 있으므로 랭크 보상은 서버 재시뮬레이션 전까지 금지한다.
- AddPractice는 시도별로 daily boardingUsed를 올리지 않는다. AddMatch/AddFriendMatch 경로는 사용량 2와 condition 3을 소비한다. 신규 MVP의 자유 연습에서 일일 에너지를 진입 제한으로 사용하지 않는다.
- 현재 소스에서 2초 콤보 연결과 반복 트릭 점수 감소 규칙은 찾지 못했다. pasted text에서 인용한 V2 제안으로만 남기며 확정된 기존 코드 규칙으로 이식하지 않는다.

### 저장 버전과 데이터 안전

- ProgressData.version은 13이다. PlayerPrefs 키는 skater_progress_v3, 이전 JSON 키는 v2/v1이다. v1의 옛 6-trick 순서는 별도 매핑으로 v2 형식에 옮기고, 이후 버전별 배열/튜토리얼/마스크/스탠스 마이그레이션을 거쳐 13으로 정규화한다.
- 저장 배열 길이는 TrickCatalog.Count(39)에 묶여 있다. Enum의 숫자 순서를 바꾸면 숙련도, 관찰/학습, 시도 수, career/mission trickMask의 의미가 바뀐다.
- Progress.Load는 최종적으로 Save를 호출한다. 별도 백업 없이 새 앱에서 로드하는 방식으로 레거시 값을 검사하지 않는다.
- 현재 스타터는 별도 PlayerPrefs 키 SS_Mastery_0..2에 3개 float를 쓴다. 키가 현재는 다르지만, 같은 앱의 저장 공간에서 레거시 Progress를 불러오면 서로 다른 모델이 한 번에 살아난다. 0..2 인덱스도 레거시의 Ollie/Kickflip/TreFlip ID가 아니다.

## PRD와 레거시 충돌·결정

| 주제 | 레거시 관측 | 새 PRD/스타터 | 결정 |
|---|---|---|---|
| 트릭 범위와 ID | 39개, 고정 인덱스 | MVP는 Ollie, Kickflip, Tre Flip | MVP enum ID를 레거시와 같은 0/3/24로 두고, 현재 저장 슬롯 0/1/2는 명시적으로 매핑한다. 나머지 트릭은 아직 노출하지 않는다 |
| 입력 범위 | 상태별 8방향/특수 입력, ↖까지; ← 기본 슬롯은 미지원 | PRD는 초기 세 기술만, 임시 위/좌상/우상 제스처 | 먼저 현 MVP 입력을 P0에서 고정하고 이후 3개를 정확히 연결한다. 8방향 확장은 별도 입력 검증 후다 |
| 터치 영역 | 옛 Unity Input adapter | PRD 5.2는 화면 오른쪽 48% 이후라고 하고, 현재 소스/README는 왼쪽 원형 패드와 오른쪽 PUSH라고 한다 | 문서와 동작이 충돌한다. 현재 검증된 source pack의 왼쪽 패드 동작을 유지하고 PRD 문구를 정리하기 전 입력 위치를 이식하지 않는다 |
| 화면 방향 | 레거시 README는 세로형 iPhone과 양 엄지 조작 | 새 PRD는 가로형 2.5D | 입력 규칙만 보존한다. 레거시 UI 위치, 레이아웃, 카메라 구도는 이식하지 않는다 |
| 성공/실패 판정 | 기계적 실패 선판정, 성공 후보에 단일 확률 draw | 현재 prototype은 이륙 시 성공을 예측하고 추가 난수로 실패 사유 선택 | PRD의 물리 우선, 시도당 단일 최종 결과를 따른다. 판정은 착지 샘플이 준비된 뒤 한 번 발행한다 |
| 스탯 효과 | 물리와 확률 양쪽에서 일부 스탯 재사용 | PRD는 중복 반영 방지를 요구 | 물리 입력과 확률 입력을 각각 표로 확정하고 같은 스탯의 이중 기여를 검증한다 |
| 학습 상태 | 미관찰/관찰/학습/숙련; mastery 60 | 예시에는 Practiced와 80/두 상황 기준이 있음 | 기존 소스에 없는 Practiced 규칙과 80 임계값은 보류한다. 3개 MVP에는 학습 UI를 강제로 노출하지 않는다 |
| 보상과 에너지 | XP, career, missions, 경기 사용량/condition | 자유 연습 무제한, 성장 보상은 선택적으로만 에너지 적용 | 연습 입력을 제한하지 않는다. 일일 에너지와 보상은 커리어 단계에서 제품 결정 후 적용한다 |
| 콤보 | 현재 저장소에서 2초/반복 감점 구현 확인 안 됨 | pasted V2 설계 언급 | 제품 규칙으로 승인되기 전에는 적용하지 않는다 |
| 경기 | 로컬 bot 및 친구 규칙 별도 존재 | 로컬 bot은 MVP, 친구 대전은 후순위 | SkateMatch 규칙만 먼저 후보로 삼고 UI와 접합한다. 친구 규칙/전송 코드는 네트워크 단계까지 보류한다 |
| 이동/그래픽 | 옛 자체 2D 물리, 픽셀 view | 새 3D Rigidbody/URP | 기존 시뮬레이션과 뷰를 이식하지 않는다. 새 3D가 이동 및 표시의 단일 소유자다 |
| 저장 | schema v13, 39칸 배열 | 스타터의 3칸 float와 다른 키 | 변환 명세와 백업을 만든 뒤에만 일회성 import를 고려한다. 수치 배열을 잘라 붙이지 않는다 |

## 테스트 결과와 미검증 영역

실행 명령: dotnet run --project /Users/osteoclasts_/Documents/deepseek-harness/스케이터키우기/Tools/CoreTests/CoreTests.csproj --configuration Release  
결과: exit code 0, passed 385, failed 0.

테스트는 39 ID와 입력 조합, SkateTouchSession의 방향·소유권·취소, deterministic Judge/확률, SkateSim/FlowSim 이동, 12 커리어·미션, SkateMatch/FriendSkateMatch 규칙을 포함한다. 다만 프로젝트가 Game/Progress.cs, Game/TouchInput.cs, FriendMatchTransport.cs, 새 Assets/SkaterSeoul 코드를 컴파일하지 않는다. Unity 6000.3.10f1 URP import, 새 씬, 물리, 실제 iOS 터치, 레거시 save migration도 이 실행으로 확인되지 않는다.

초기 감사에서는 C# 파일, Unity 설정, 씬을 수정하지 않았다. 사용자의 후속 이식 요청에 따라 현재 MVP C#의 세 트릭 ID와 착지 판정 일부를 반영했다. 새 저장소에 레거시 JSON을 가져오거나 레거시 프로젝트·저장값을 수정하지 않았다. 후속 반영 내용은 docs/LEGACY_MIGRATION_PLAN.md의 마지막 절에 기록했다.

## Visual overhaul implementation update (2026-10-23)

The later v2.0 task supplied a real Unity target and explicitly requested migration. The earlier no-import decision above was superseded. The external legacy game repository remains read-only; the existing SKATER SEOUL source-pack files were retained and compatibility edits were made in place.

- Imported the pure legacy `Core` catalog, types, tuning, Judge, motion targets, success model, match, career and missions, plus `Game/Progress.cs`, into `UnityProject/Assets/SkaterSeoul/LegacyDomain/`.
- Adapted `SkateMatch` only to accept an explicit three-trick local bot pool; the legacy default pool and 39 stable trick IDs remain intact.
- The live 3D prototype uses legacy stable IDs (Ollie 0, Kickflip 3, Tre Flip 24), the v13 progress/save structure, Judge for mechanical landing eligibility, the legacy success model for its one probability draw, and `SkateMatch` for player-set to bot-match flow.
- Current scene controls expose three tricks. Full 8-way input, remote/friend play, career/challenge UI, a full match end state, and the rest of the 39 tricks are not connected to the visual slice. Existing modules are retained as data/domain modules, not all claimed as playable features.
- CoreTests from the source project were not rerun against the adapted Unity copies. Unity static rule checks and the scene Play Mode probe are reported separately in `docs/VISUAL_OVERHAUL_QA.md`.
- Save risk: the iOS bundle identifier was kept as `com.osteoclasts.skaterkiugi` and the three old `SS_Mastery_*` keys are mapped into the v13 slots. A copied-PlayerPrefs validation harness restores local keys after the probe; migration of an actual installed user's device save remains **NOT RUN**.
