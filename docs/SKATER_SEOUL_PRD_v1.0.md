# SKATER SEOUL — Unity 6 모바일 스케이트보드 게임 실행형 PRD

> **문서 버전:** 1.0  
> **기준일:** 2026-10-23  
> **독자:** Codex(구현·검증 담당), 1인 기획/개발자  
> **프로젝트:** 스케이터 키우기 / SKATER SEOUL  
> **문서 성격:** 기존 스타터 코드 감사 → 패치 → 플레이 가능한 Vertical Slice → 모바일 QA를 강제하는 실행 사양서  
> **상태:** 구현 지시용. Unity에서 실제 실행·빌드되었다는 뜻이 아니다.

> **정정(2026-10-23):** 플랫폼 우선순위는 iOS이며 Android는 호환 대상으로 둔다.

---

## 0. Codex 최상위 실행 지시 — 반드시 준수

**당신은 시니어 Unity 6 URP 게임플레이·애니메이션·모바일 QA 엔지니어다.** 이 문서를 읽은 뒤 계획만 작성하고 멈추지 말고, **현재 열려 있는 Unity 프로젝트의 실제 상태를 조사한 다음 코드를 수정하고 검증**하라. 가능한 범위에서 동작하는 결과를 남겨라.

1. `Assets/SkaterSeoul`에 이미 배치된 스타터 코드를 **기존 자산**으로 취급한다. 새 프로젝트를 무작정 생성하거나 기존 `SkateSim`, `FlowSim`, 다른 프로젝트/씬을 지우지 않는다.
2. 작업 전 `README.md`, `CODEX_NEXT_STEPS.md`, `RESEARCH.md`를 찾고 읽는다. 파일이 저장소 루트에 없으면 부모 폴더·하위 폴더를 탐색한다. 문서의 주장보다 **현재 저장소와 실제 실행 결과**를 우선한다.
3. `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json`, 씬, C# 스크립트, Git 상태를 조사한다. 수정 전 변경사항을 확인하고 보존한다. Git이 없으면 파일을 안전하게 백업한다.
4. **Phase 0을 통과하지 못했으면 Phase 1 이후의 대규모 리팩터링과 그래픽 작업을 시작하지 않는다.** 막힌 이유를 재현 가능하게 남기고 허용된 범위에서 계속 해결한다.
5. Unity Editor를 실행할 수 있으면 컴파일·EditMode 테스트·PlayMode·빌드 검증을 실제 수행한다. 실행 환경이 없으면 소스 수준 검증만으로 `PASS`라고 기록하지 말고 `NOT RUN`으로 보고한다. 불가능한 검증을 수행한 척하지 않는다.
6. 자유 사용 권한이 확인되지 않은 외부 코드/애셋은 프로젝트에 복사하지 않는다. EA/Skate City/Roblox의 원본 에셋·코드를 복제하지 않는다. REA는 조사 보조수단이며 필수 실행 의존성이 아니다.
7. **가장 작은 작동 버전부터 완성**한다. Ollie → Kickflip → Tre Flip, 조작 → 물리 → 애니메이션 → 판정 → 성장 → S.K.A.T.E. 순서로 통합한다.
8. 변경할 때마다 컴파일 오류·콘솔 예외·씬 참조 손실을 점검한다. 완료 뒤 마지막의 **산출물 및 보고 형식**을 지킨다.
9. 막히는 세부 결정은 본 PRD의 기본값을 적용한다. 단, Unity 버전 변경, 기존 데이터 삭제, 권리 불명 에셋 설치, 외부 서비스 비용 발생 등 파괴적·비가역적 작업은 임의로 수행하지 않는다.

**절대 금지:** 장문의 계획만 제출, 빈 인터페이스만 생성, 작동하지 않는 애니메이션을 작동한다고 보고, 씬이 파괴되는 자동 생성, 기존 과거 프로젝트 변경, 테스트를 하지 않고 성공으로 표기.

---

## 1. 제품 정의

### 1.1 한 문장 제품

**가로형 2.5D 서울 스케이트 게임.** 플레이어는 쉬운 터치 스와이프로 트릭을 선택하고, 연습하면서 트릭별 숙련도를 키운 뒤, 같은 트릭을 따라 해야 하는 **S.K.A.T.E. 봇 배틀**로 실력을 시험한다.

### 1.2 핵심 플레이 루프

`주행 → 스와이프 → 트릭 인식 → 팝/회전/캐치/착지 → 성공·실패 피드백 → 숙련도 변화 → 다음 시도 또는 S.K.A.T.E. 턴`

- 플레이어가 **무슨 트릭을 시도했는지**는 입력 인식이 결정한다.
- **그 트릭을 성공시켰는지**는 입력 품질, 트릭 숙련도, 트릭 난도 및 규칙으로 결정한다.
- 잘못된 입력 때문에 **다른 기술이 랜덤으로 실행되면 안 된다.** 모호한 입력은 거부하거나 재시도를 요청한다.
- 실패 확률이 있어도 실패 원인과 화면 동작이 일치해야 한다. 숙련자가 쉬운 기술을 계속 실패하는 상태를 방치하지 않는다.

### 1.3 타깃과 제약

| 항목 | 결정 |
|---|---|
| 엔진 | 기존 프로젝트의 Unity **6** + **URP** |
| 플랫폼 | iOS 우선, Android 호환 가능 구조 |
| 방향 | Landscape 가로형, 기본 기준 16:9, 19.5:9·20:9 검증 |
| 시점 | 사이드뷰 기반 **2.5D**, 캐릭터·보드는 3D |
| 디자인 참고 | Skate City의 터치 접근성과 연계감 **참고**. 동작·아트·상표·맵은 오리지널 |
| 핵심 모드 | 자유 연습(Practice), 1P vs Bot S.K.A.T.E. |
| 핵심 기술 | Ollie, Kickflip, Tre Flip (최초 완성 기준) |
| 무대 | 첫 버전은 무장애 플랫그라운드 테스트 레인. 이후 뚝섬에서 영감 받은 **독자 제작** 스팟 |
| 예상 사용자 행동 | 한 번에 수십 회 트릭 시도, 빠른 재도전, 반복 성장 |
| 런타임 오프라인 | 첫 버전에서 로그인·서버·네트워크 없이 작동 |
| 프레임 목표 | 중급 iPhone의 안정적 60FPS **목표**, 실제 측정 전 달성 주장 금지 |

### 1.4 MVP 범위 / 비범위

**이번 필수 범위(P0~P3):** 실제 실행 가능한 씬, 튼튼한 터치 제스처, 트릭 3종, 게임플레이와 일치하는 3D 움직임, 실패 종류, 숙련도 지속 저장, 연습 모드, 완결 가능한 봇 배틀, 최소 모바일 HUD, 테스트.

**범위 제외:** 온라인 PvP, 소셜, 코스튬 상점, 수십 가지 트릭, 대규모 서울 오픈월드, 레일/계단/매뉴얼/복잡한 그라인드, 계정·서버, 과금, 광고, AI 생성 NPC, Skate 3 전체 물리 복제. 추가 기능은 승인된 다음 마일스톤에만 진입한다.

---

## 2. 인수받은 코드 감사 — 이 항목은 추측이 아니라 실제 ZIP 소스 기준

스타터 ZIP에서 확인한 파일:

```text
Assets/SkaterSeoul/
├── Scripts/
│   ├── SkaterSeoulPrototype.cs
│   └── SkaterSeoulRules.cs
├── Editor/
│   ├── SkaterSeoulSceneBuilder.cs
│   └── SkaterSeoulValidation.cs
└── Scenes/                 (생성할 씬의 위치)
README.md
CODEX_NEXT_STEPS.md
RESEARCH.md
```

| 현재 코드에 실제 존재하는 기능 | 현재 제한·감사 포인트 |
|---|---|
| Rigidbody 전진·점프, X 방향 자동 주행 | 지면 감지와 실제 보드 바퀴 접지, 부드러운 재시작 확인 필요 |
| 키보드 `1/2/3` 트릭 시도, 마우스·터치 오른쪽 영역 | **시작점/끝점 두 점만 사용**. 연속 제스처·곡선·다중 손가락 경합 미처리 |
| `Ollie/Kickflip/TreFlip` 3종 | 복잡한 트릭을 포함할 수 있는 데이터 기반 표현 미구축 |
| 보드 로컬 X축 Flip, 로컬 Y축 Shuv 회전 | 팝→플릭→회전→캐치→착지의 자연스러운 발·몸 연결 부재 |
| `Probability()`, `PlayerPrefs` 숙련도 증가 | 입력 하한은 있으나 실패 세부 동작/학습 효과·시험 시드 검증 부족 |
| 베일 후 약 0.85초 복귀 | 충돌/애니메이션/재도전 간 상태 경합·시각적 연속성 검증 필요 |
| 텍스트 기반 봇 S.K.A.T.E. | `AdvanceBattle()`가 한 메서드에서 턴을 즉시 처리, 명시적 규칙 엔진·로그 부족 |
| OnGUI HUD 및 RESET 버튼 | 실제 모바일 안전 영역·UI 터치 충돌·글자 가독성 보장 안 됨 |
| 에디터 씬 생성 메뉴 | 새 씬 생성·저장 전 기존 씬/기존 경로 덮어쓰기 여부 감사 필요 |
| 정적 유효성 검사 메뉴 | 실제 Unity 컴파일/런타임/모바일 검증 대신할 수 없음 |

### 2.1 알려진 구조적 위험

- `SkaterSeoulPrototype.cs` 한 컴포넌트가 입력·이동·애니메이션·확률·XP·배틀·GUI를 모두 소유하는 **God Object**다. 기능을 추가하기 전에 작동 상태를 고정하고, 테스트를 붙이면서 단계적으로 책임을 분리한다.
- `ReceiveGroundContact()`는 충돌 노멀과 시간으로 착지를 결정한다. 현재 물리 Root 위치와 보드/바퀴 시각 모델의 지면 높이가 다를 수 있다. **발·바퀴가 떠 있거나 지면을 관통하는 현상**을 우선 확인한다.
- `BeginTrick()`은 착지 성공을 이륙 시 RNG로 정하지만 `AnimateFlight()`는 성공/실패 연출을 완전히 분기하지 않는다. 결과가 아니라 동작이 판정을 설명하도록 교정한다.
- `AdvanceBattle()`은 플레이어 선공 실패 뒤 봇이 즉시 기술을 제시하거나 실패하면 플레이어 턴을 유지하는 단축 루프다. **상대 턴과 선공권 전환을 명시적으로 모델링**해야 한다.
- `OnGUI`와 터치 입력 영역이 겹칠 수 있다. **버튼 터치가 트릭으로 인식되는 문제**를 조사하고 해결한다.
- 리서치 ZIP에는 외부 코드나 3D 에셋이 포함되지 않는다. 외부 자료 링크를 실제 임포트된 패키지로 착각하지 않는다.

---

## 3. 우선순위와 실행 게이트

| Phase | 필수 결과 | 진행 조건 |
|---|---|---|
| **P0 — Compile & Play** | 스타터 정상 실행, 3종 트릭, 성공/실패, 한 판 완주 | 다른 Phase 착수 전 **필수** |
| **P1 — Controls & State** | 정확한 입력 인식·스탠스 좌표 모델·테스트 가능한 상태 엔진 | P0 통과 |
| **P2 — Motion & Feedback** | 보드·발·몸이 연결되는 3D 동작, 실패 원인 시각화 | P1의 입력/상태 계약 확정 |
| **P3 — Battle & Progression** | 올바른 봇 턴, 숙련도 저장, 휴대폰용 HUD, 터치 QA | P0~P2 주요 검증 통과 |
| **P4 — Art & Device Polish** | CC0 테스트 파크·사운드·성능·iOS 실행 확인 | P3 플레이 루프 완성 |
| **P5 — 확장(이번 범위 외)** | Heelflip, Pop Shove-it, FS180, 뚝섬 맵 확장 | 사용자가 MVP 합격 판정 후 |

**시간 제한/에디터 접근 불가로 P0 이후를 완료하지 못하면**, 완성했다고 주장하지 않는다. 가장 마지막으로 통과한 Gate와 다음에 필요한 조치를 남긴다.

---

## 4. P0 — 현재 ZIP으로 게임을 실제 실행시키기

### 4.1 작업

- [ ] 프로젝트 루트와 Unity 버전 확인. 다른 프로젝트의 변경분과 혼재 여부 조사.
- [ ] `com.unity.inputsystem` 설치 유무와 Player `Active Input Handling` 확인. 필요한 경우 최소 수정만 한다.
- [ ] 기존 C#과 URP에서 실제 컴파일. 특히 `Rigidbody.linearVelocity`, `EnhancedTouchSupport`, `Touch.activeTouches`, 네임스페이스·패키지 확인.
- [ ] 씬 생성 메뉴 `Tools > SKATER SEOUL > 1 - Create Playable MVP Scene`을 안전하게 실행. 기존 `SkaterSeoul_MVP.unity`가 있으면 백업·확인 후 재생성. **무조건 덮어쓰지 말 것.**
- [ ] 에디터 플레이: 자동 주행, 카메라 추적, 점프 포물선, 보드·캐릭터 표시, 세 트릭의 실제 차이 확인.
- [ ] 키보드 1/2/3, 게임 뷰 마우스 오른쪽 영역 스와이프, 터치 환경 가능 시 3종 검증.
- [ ] `Tools > SKATER SEOUL > 2 - Validate Game Rules` 실행, 통과/실패 로그 캡처.
- [ ] 성공과 베일 이후 입력 잠금/복구 검증. 턴이 영구적으로 멈추거나 플레이어가 추락·관통하면 패치.
- [ ] 자동 주행 루프와 리스폰 위치에서 카메라/보드 튐 없음 확인.
- [ ] iOS 빌드 환경이 있다면 **실제** 빌드·시뮬레이터 또는 실기기 설치·가로 화면 확인. 없다면 정확한 제약 보고. Android는 후순위 호환 검증으로 기록한다.

### 4.2 P0 합격 기준

1. 콘솔 컴파일 오류 **0건**. 실행 중 재현 가능한 예외 **0건**.
2. 편집기에서 **3종 트릭을 연속 10회 이상** 시도할 수 있고 상태가 고착되지 않는다.
3. Ollie는 팝만, Kickflip은 데크 장축 기준 플립, Tre Flip은 플립+수평 스핀으로 **서로 눈에 띄게 구분**된다.
4. 착지 후 재시도 가능, 실패 후 정상 회복, 리셋 후 새 배틀이 시작된다.
5. 배틀에서 한쪽이 `SKATE` 5글자를 획득하면 승패가 확정된다.
6. 테스트하지 않은 항목은 `PASS`가 아니라 `NOT RUN`으로 명시한다.

### 4.3 보존 규칙

- `SkaterSeoulPrototype` 기능을 전면 삭제하기 전에 **기존 플레이 가능한 씬과 입력을 고정**하고 새 구조로 천천히 이전한다.
- Git이면 Phase별 작고 검증 가능한 커밋을 남기되, **명시적 요청 없는 push/deploy는 하지 않는다**.
- 기존 씬 참조를 끊는 파일명·클래스명 변경은 레퍼런스 마이그레이션과 함께 수행한다.

---

## 5. P1 — Skate City에서 영감 받은 모바일 터치 조작

### 5.1 설계 원칙

**원작의 내부 코드를 추정·복제하지 말고**, 누구나 습득할 수 있는 스와이프 문법을 독자적으로 구현한다. 처음에는 세 기술과 단일 입력만 확실히 만든다. 전체 조작 문법은 이후 입력 실험 결과로 확장한다.

### 5.2 입력 데이터 계약

```text
GestureSample { fingerId, positionNormalized, timestampUnscaled }
GesturePath { samples[], startRegion, duration, length, displacement,
              normalizedDirection, curvature, peakSpeed, meanSpeed,
              canceled, beganOverUI }
GestureMatch { status: Recognized|Ambiguous|Rejected,
               trickId?, quality01, confidence01, rejectionReason }
TrickRequest { trickId, stance, approachDirection, inputQuality01,
               source: Touch|Mouse|KeyboardDebug, inputTime }
```

- 터치 경로는 여러 샘플로 기록하며 입력 장치마다 스크린 정규화 기준을 동일하게 한다.
- 시작점은 **오른쪽 48% 이후**를 초기값으로 하되, Inspector/ScriptableObject에서 수정 가능하게 한다.
- HUD에서 시작한 터치, 취소된 터치, 다중 손가락 중복 소유권, 화면 밖으로 나간 입력을 안전하게 처리한다.
- 제스처 종료 시각과 수집한 path를 활용한다. 시간 정규화와 거리 정규화를 구분한다.
- **너무 짧거나 느리거나 모호한 입력은 거부**한다. 인식 실패가 트릭 실행이나 RNG 실패로 둔갑하면 안 된다.
- 입력 품질은 `[0,1]`로 표준화하고 HUD와 로그에 남긴다.
- 단일 입력당 최대 하나의 트릭만 실행한다. 공중/베일/리셋 상태에서는 새로운 트릭을 무시하며 이유를 로그로 남긴다.

### 5.3 MVP 임시 제스처 표 — 원작 정식 매핑이 아님

| 오른쪽 화면 제스처 | TrickId | 비고 |
|---|---|---|
| 위 직선 | Ollie | 길이, 속도, 방향으로 판정 |
| 좌상향 직선 | Kickflip | Ollie와 임계각 중복 금지 |
| 우상향 직선 | TreFlip | 향후 곡선/순차 입력으로 교체 가능하도록 데이터화 |

이 표는 **기존 ZIP과의 호환을 위한 임시 조작**이다. Tre Flip이 반드시 우상향 직선이어야 한다는 최종 게임 디자인 결정이 아니다. 추후 Skate City식 제스처 연구 결과에 따라 `InputProfile` 데이터만 교체할 수 있어야 한다.

### 5.4 스탠스와 진행 방향

다음 값을 명시적으로 보유한다.

```text
Stance: Regular | Goofy
TravelDirection: ForwardX | BackwardX
RidingMode (확장 준비): Normal | Switch | Fakie | Nollie
TrickRotation: FlipAxis + signedAngle; ShuvAxis + signedAngle
```

현재 MVP는 한 방향으로만 달려도 좋지만 **스탠스/진행방향을 코드에 하드코딩하지 않는다**. 월드 좌표, 보드 로컬 좌표, 화면 입력 좌표를 혼동하지 않는다. `Regular→Goofy`, `Forward→Backward` 전환 시 스케이터가 갑자기 180도 잘못 회전하거나 동일 트릭이 다른 기술로 인식되지 않도록 좌표계 변환을 테스트한다.

### 5.5 입력 테스트

- [ ] 직선 위/좌상/우상 각 20개 샘플을 입력 리플레이 테스트로 재생, 의도한 기술 인식률 **95% 이상(목표)**.
- [ ] 경계각·짧은 제스처·중단된 드래그·HUD 시작 입력에서 잘못된 기술 실행 **0건**.
- [ ] 동일한 정규화 입력을 다른 화면 비율에 적용했을 때 판정이 바뀌지 않는다.
- [ ] 터치·마우스 Debug 입력이 하나의 `TrickRequest` 파이프라인에 수렴한다.
- [ ] 조작감을 바꾸는 threshold는 코드 숫자 교체 대신 조정 가능한 데이터로 보존한다.

---

## 6. P2 — 3D 트릭·물리·실패 연출

### 6.1 움직임과 보여지는 표현 분리

```text
SkaterPhysicsRoot (Rigidbody, Collider, GroundProbe)
├── BoardVisualRoot (Deck, Trucks/Wheels visual, BoardTrickMotor)
└── SkaterVisualRoot (Humanoid or temporary rig, RiderAnimator, Foot IK targets)
```

- 물리 Root는 **실제 이동·점프 포물선·충돌·접지 상태**만 관리한다.
- Visual Root는 **보드의 플립/수평 스핀과 스케이터 포즈**를 담당한다.
- 보드 전체의 시각적 회전을 실제 물리 충돌체에 강제하지 않는다.
- 낮은 수준의 충돌에는 2.5D 단순화가 허용된다. 단, 공중 회전 종료와 실제 접지 시간을 동기화한다.

### 6.2 공통 상태 머신

```text
Rolling → Crouch → Pop → AirborneRotation → Catch → LandingRecovery → Rolling
                                    │             │          │
                                    └──── Failure(UnderRotation / MissedCatch / LostBalance)
                                                           ↓
                                                          Bail → Recover → Rolling
```

- 시간 또는 애니메이션 이벤트와 실제 Physics 이벤트 사이에 충돌하는 경우 **명시적 우선순위**를 정의한다.
- 같은 프레임에 두 번 착지/두 번 XP/두 번 글자 지급을 금지한다.
- 새 트릭을 요청한 뒤 `ResultResolved`는 **정확히 한 번** 발행한다.
- 시각 상태가 판정 결과와 동기화될 때만 성공 판정을 화면에 보여준다.
- 물리 Root의 재시작/리스폰은 카메라와 모든 시각 Root의 상태를 일관되게 재설정한다.

### 6.3 트릭 모션 명세

| 기술 | 보드 회전 | 캐릭터 동작 | 성공 상태 |
|---|---|---|---|
| Ollie | 플립 없음, 약간의 팝 자세 | 뒷발 팝→앞발 레벨링→양발 캐치 | 평평한 보드로 착지 |
| Kickflip | 보드 **장축(Local X)** 기준 360° 플립 | 앞발 측면 플릭, 두 발 분리 후 캐치 | 플립 1회 후 발이 다시 그립 위 |
| Tre Flip | Kickflip 회전 + **수직축(Local Y)** 360° Shuv | 팝 후 발 벌림, 보드 추적·캐치 | 두 회전이 끝나고 같은 진행 방향으로 착지 |

**주의:** 보드 플립이 360도 완료되었다고 끝이 아니다. 그 시점의 발 높이, 신발 위치, 골반·무릎 각도, 보드 접지 높이가 함께 설득력 있어야 한다.

### 6.4 실패 유형

| FailureType | 눈에 보여야 하는 현상 | 판정 근거 |
|---|---|---|
| `UnderRotation` | 보드가 완전히 회전하지 못한 채 접지/비틀어짐 | 회전 목표의 일정 부분만 완성 |
| `MissedCatch` | 보드는 거의 회전하지만 발이 잡지 못하고 분리됨 | Catch 타이밍 실패 |
| `LostBalance` | 착지는 했지만 무게중심이 무너져 넘어짐 | 착지 후 균형 실패 |
| `WrongTrick` | 배틀 방어 시 요청 트릭 불일치 안내, 성공 판정 불가 | 입력 기술과 요구 기술 불일치 |

실패 원인은 UX로 설명 가능한 제한된 결정에서 선택한다. **모든 실패에 같은 넘어짐 애니메이션을 붙이는 것으로 완료 처리하지 않는다.** 미술 퀄리티 부족 시 우선 Primitive + 회전·위치 차이로라도 결과를 구분한다.

### 6.5 아트·리깅 도입 순서

1. P0 Primitive 상태에서 **회전축·지면·착지 타이밍** 확정.
2. 별도 다운로드한 **권리 확인된 CC0 Kenney Mini Skate** 구조물로 테스트파크를 개선.
3. CC0 또는 사용권이 확인된 휴머노이드 도입. Quaternius 무료 범위 또는 Unity 내 적법 사용이 가능한 Mixamo 참고.
4. `com.unity.animation.rigging`으로 발 목표·무릎 굴곡·보드 따라가기 설정.
5. 발을 붙일 구간(Rolling/Crouch/Catch/Landing)과 발을 뗄 구간(Pop/Flip/Bail)을 분리.
6. 아트가 없어 구현을 중지하지 말 것. 플레이 가능한 Primitive 폴백 유지.

---

## 7. P3 — 성장·확률·실패 공정성

### 7.1 코드 계약

```text
TrickDefinition: id, displayName, difficulty01, flipDegrees, shuvDegrees,
                 motionProfile, gestureProfile, unlockRule
TrickMastery: trickId, mastery0to100, attempts, lands, lastPracticed
AttemptContext: trickId, inputQuality, mastery, mode, fatigue(optional), seed(optional)
AttemptResult: resultId, landed, failureType, probabilityUsed, randomDraw,
               masteryBefore, masteryAfter, phaseTimestamps
```

`TrickDefinition`은 `ScriptableObject` 또는 직렬화 가능한 데이터로 구현한다. 확률 계산·배틀 규칙은 Unity `MonoBehaviour`와 분리해 **순수 로직 테스트**가 가능해야 한다.

### 7.2 확률 모델

초기에는 현 스타터의 시그모이드 모델을 기준점으로 사용한다.

\[
P(land) = \begin{cases}
0 & Q < Q_{min}\\
\mathrm{clamp}(\sigma(-0.55+0.045(M-D)+2.5(Q-0.7)),\,0.01,\,0.985) & Q \ge Q_{min}
\end{cases}
\]

- `M` = 트릭별 숙련도(0~100), `D` = 난도(스타터 내부 지표), `Q` = 입력 품질(0~1), `Q_min = 0.27`는 초기값.
- 이 수식은 **초기 구현 기준값**이지 학습과학/실제 스케이터 성공률의 실증 모델이 아니다.
- 높은 입력 품질과 숙련도에서 Ollie가 과도하게 실패하지 않는지 별도 밸런싱한다.
- 방어 턴에서 요구 트릭과 다르면 성공 확률은 0이며, **WrongTrick은 입력 오인식과 구분**한다.
- RNG는 주입 가능한 인터페이스로 분리해 테스트용 고정 시드를 지원한다. 일반 실행에서는 적절한 의사난수 시드를 사용한다.
- 숨겨진 승부 조작·봇 특혜·비공개 확률 보정은 MVP에 넣지 않는다.

### 7.3 숙련도 성장

- 시도 1회가 **정확히 1번** 기록되어야 한다.
- 착지 성공과 실패의 증가 폭을 별도 설정 가능하게 한다(스타터의 `성공 +2.0 / 실패 +0.65`는 잠정값).
- 연습 모드에서 전용 성장 지표를 표시한다. 배틀 경험치를 이중 지급하지 않는다.
- 로컬 저장은 `PlayerPrefs` 유지 가능하지만 키에 버전/네임스페이스를 부여하고 구버전 마이그레이션 또는 안전한 기본값을 제공한다.
- `Reset Battle`은 글자·턴만 초기화, **숙련도 유지**. 별도 개발용 `Reset Progress`를 제공하되 사용자 데이터 삭제 경고 필요.

### 7.4 요구 테스트

- [ ] 동일 조건 + 동일 seed → 동일 판정 결과.
- [ ] `M↑` 또는 `Q↑`일 때 기대 확률은 감소하지 않는다(하한·상한 제외).
- [ ] `Q < 0.27`이면 RNG와 관계없이 항상 실패.
- [ ] 시뮬레이션 10,000회/조건 등 **통계 테스트**로 표시 확률과 경험적 결과 간 오차를 기록(표본 오차 고려).
- [ ] 배틀 리셋 이후에도 저장된 숙련도 유지.
- [ ] 비정상 저장값(NaN, 음수, 100 초과) 정규화.

---

## 8. P3 — S.K.A.T.E. 봇 배틀 명시적 규칙

현 스타터의 텍스트 구현은 단축 시뮬레이션이다. 이후 아래 **MVP 전용 변형 규칙**으로 명시하고 코드화한다. 실제 로컬 스케이트 문화의 모든 변형 규칙을 대표한다고 주장하지 않는다.

### 8.1 상태

```text
MatchState: Ready | PlayerSetting | BotSetting | PlayerDefending |
            BotDefending | ResolveTurn | Won | Lost
SetTrick: TrickId + modifiers (MVP는 TrickId만 필수)
LetterCount: 0..5 for both players
Setter: Player | Bot
```

### 8.2 선택한 MVP 규칙

1. 선공은 플레이어로 시작한다.
2. 선공자가 트릭을 **성공**하면 방어자는 동일한 `TrickId`를 시도한다.
3. 방어자가 성공하면 글자가 추가되지 않고 **기존 선공권 유지**; 다음 턴으로 간다.
4. 방어자가 실패하거나 틀린 트릭을 수행하면 해당 방어자에게 `S`, `K`, `A`, `T`, `E` 순서로 **글자 하나**가 추가된다. 선공권은 유지한다.
5. 선공자가 제시 트릭에 실패하면 **선공권이 상대에게 넘어간다**. 이 실패 자체로는 글자를 얻지 않는다.
6. MVP에서는 **기술 재사용 허용**, **마지막 E에서 추가 방어 시도 없음**으로 한다. 두 규칙은 추후 룰 옵션으로 확장 가능하게 만든다.
7. 글자 수가 5가 되는 시점에 게임 종료. 승패 화면·다시 하기 기능 표시.
8. 봇이 선공이면 트릭 선택 및 성공/실패를 봇 확률 모델로 계산하고, 결과를 **애니메이션 또는 최소한의 재생 연출+명확한 UI**로 보여준다. 즉시 글자만 변경해 사용자가 무슨 일이 일어났는지 모르게 하지 않는다.
9. 선공권, 제시한 트릭, 방어 기회, 판정 결과, 누적 글자를 UI/로그로 추적할 수 있어야 한다.
10. 방어자가 자신이 시도할 트릭을 자유롭게 선택하는 입력은 가능하되, **요구 기술과 불일치하면 미스매치로 취급**한다. 입력 인식이 틀렸기 때문인지, 다른 기술을 시도한 것인지 로그에 구분한다.

### 8.3 배틀 화면 필수 정보

- 나와 BOT의 `S K A T E` 상태 및 누적 글자.
- `YOUR SET / BOT SET / DEFEND: KICKFLIP` 등 현재 턴 안내.
- 현재 시도한 기술, 성공·실패 원인, 시도 품질 및 성공률.
- `Practice / Battle` 모드 전환(게임 진행 중 의도치 않은 재설정 방지).
- 배틀 종료: 승자/패자, `RESTART BATTLE`, 숙련도 유지 안내.

### 8.4 자동화 테스트 케이스

| TestId | 선공·행동 | 예상 결과 |
|---|---|---|
| B01 | Player setter 실패 | 글자 증가 없음, Bot setter로 전환 |
| B02 | Player setter 성공, Bot defense 성공 | 글자 증가 없음, Player setter 유지 |
| B03 | Player setter 성공, Bot defense 실패 | Bot +1 letter, Player setter 유지 |
| B04 | Bot setter 성공, Player defense 성공 | 글자 증가 없음, Bot setter 유지 |
| B05 | Bot setter 성공, Player defense 실패 | Player +1 letter, Bot setter 유지 |
| B06 | Bot setter 실패 | 글자 증가 없음, Player setter로 전환 |
| B07 | Player defender가 다른 트릭 수행 | Player +1 letter, WrongTrick 로그 |
| B08 | 누군가 5번째 글자 획득 | 즉시 종료, 입력 잠금, 승자 표시 |
| B09 | Battle reset | 글자 0, 턴 초기화, 숙련도 불변 |
| B10 | 방어 결과 중복 이벤트 | 글자 중복 지급 없음 |

---

## 9. P4 — 모바일 HUD, 비주얼, 사운드 및 성능

### 9.1 HUD 원칙

- `OnGUI`는 P0 디버깅 용도로만 유지 가능. 모바일 릴리스 경로는 uGUI 또는 UI Toolkit으로 교체.
- 노치/안전 영역 `Screen.safeArea`, 다양한 가로 비율, 큰 엄지 조작 영역을 고려한다.
- 오른쪽 Flick 구역과 버튼의 터치 소유권을 구분한다. UI 터치가 스케이트 트릭을 발동시키면 **실패**.
- 장시간 플레이 시 중요한 정보는 짧게, 실패 원인은 일관된 용어로 표시한다.
- 색상만으로 성공/실패를 구분하지 않는다. 텍스트·형태·진동 또는 소리도 함께 사용한다.

### 9.2 3D·아트

- 저폴리곤 그 자체를 목표로 하지 않는다. **깔끔한 실루엣, 좋은 라이팅, 일관된 소재, 자연스러운 모션**을 우선한다.
- 첫 파크는 테스트 차선 + 미니 램프/레일 소품(실제 플레이 충돌은 P4 범위 내 플랫만 필수).
- 무료 에셋 출처·URL·라이선스를 `THIRD_PARTY_ASSETS.md`에 기록. 저장소에 포함된 경우 파일별 라이선스를 검토한다.
- 뚝섬이라는 지명은 콘셉트로 쓸 수 있지만 **실재 장소의 정확한 형상 재현**은 후속 현장 자료/허가·저작권 확인 후 진행한다.

### 9.3 오디오·햅틱

- 최소: 구름 소리, Pop, Flip, 성공 착지, Bail, 승패 UI.
- 사운드 볼륨/음소거 지원. 실제 외부 오디오 소스는 개별 페이지에서 재배포 권한 확인.
- 햅틱은 지원 기기에서만 선택적으로 작동하고, 빈번한 중복 호출을 막는다.

### 9.4 성능·빌드

- URP 모바일 적합 설정 사용. Shadow, MSAA, 렌더 스케일, 파티클 비용을 프로파일 기반으로 조정.
- 연속 플레이 중 불필요한 `GC.Alloc` 및 프레임마다 새 배열·리스트 생성을 피한다.
- 목표: **중급 iPhone 60FPS**. 장치·해상도·측정 도구 없이 달성 주장 불가.
- iOS 빌드: 씬 등록·가로 화면·safe area·터치 입력·UI 크기 확인. Android는 호환성 검증으로 별도 기록한다.

---

## 10. 권장 리팩터링 구조 — 한번에 전부 만들 필요 없음

```text
Assets/SkaterSeoul/
├── Core/
│   ├── TrickId.cs
│   ├── TrickDefinition.cs            # ScriptableObject or serializable
│   ├── TrickCatalog.cs
│   └── SkaterFrame.cs                 # stance / travel / local board axes
├── Input/
│   ├── GesturePathRecorder.cs
│   ├── GestureClassifier.cs
│   ├── TrickInputMapper.cs
│   └── GestureReplayFixture.cs
├── Gameplay/
│   ├── SkaterPhysicsMotor.cs
│   ├── TrickExecutionStateMachine.cs
│   ├── TrickOutcomeResolver.cs
│   └── GroundProbe.cs
├── Presentation/
│   ├── BoardTrickVisual.cs
│   ├── SkaterPoseController.cs
│   ├── LandingBailPresenter.cs
│   └── SideCameraController.cs
├── Progression/
│   ├── SuccessProbabilityModel.cs
│   ├── TrickMasteryRepository.cs
│   └── AttemptTelemetry.cs
├── Battle/
│   ├── SkateMatchState.cs
│   ├── SkateMatchRules.cs
│   ├── SkateMatchController.cs
│   └── BotSkaterPolicy.cs
├── UI/
│   ├── PracticeHUD.cs
│   └── BattleHUD.cs
├── Tests/
│   ├── EditMode/
│   └── PlayMode/
├── Editor/
│   ├── SkaterSeoulSceneBuilder.cs
│   └── SkaterSeoulValidation.cs
└── Scenes/
    └── SkaterSeoul_MVP.unity
```

**이것은 목표 모듈 책임 구조이며 파일 수 체크리스트가 아니다.** 필요 없는 추상화/클래스를 억지로 생성하지 않는다. 기존 `SkaterSeoulRules`와 `SkaterSeoulPrototype`는 회귀 테스트를 갖춘 뒤 점진적으로 분리한다.

### 10.1 강제 이벤트 계약

```text
GestureCompleted(GesturePath)
  → TrickRecognized(TrickRequest)
  → TrickStarted(AttemptId)
  → TrickVisualPhaseChanged(phase)
  → TrickResolved(AttemptResult)        # 정확히 한 번
  → MasteryUpdated(AttemptId)          # 정확히 한 번
  → BattleTurnResolved(AttemptId)      # 배틀 모드에서만, 정확히 한 번
```

- Practice와 Battle이 한 번의 `TrickResolved`를 이중 구독해 보상을 중복 지급하면 안 된다.
- `AttemptId` 기반으로 중복 방지한다.
- Unity PlayMode 중 새 씬/리스폰/리셋 시 이벤트 구독 해제를 보장한다.

---

## 11. 소스 및 에셋 리서치 활용 범위

아래 링크는 확인·참고 목적이며 **새로 다운로드해 프로젝트에 이미 포함됐다고 가정하지 않는다**.

| 리소스 | 목적 | 재사용 원칙 |
|---|---|---|
| https://kenney.nl/assets/mini-skate | CC0 테스트 스케이트파크 | 배포 전 원본 라이선스 재확인 |
| https://kenney.nl/assets/city-kit-roads | 도시 환경 | CC0 조건 기록 |
| https://quaternius.itch.io/universal-animation-library | 리깅·휴머노이드 동작 | 무료 패키지 실제 범위·권리 확인 |
| https://www.mixamo.com/ | 휴머노이드 리깅·기본 동작 | 원본 파일 재배포 제한 확인 |
| https://github.com/burning-laboratory/unity-swipe-input | 간단한 스와이프 연구 | MIT 고지 보존, 필요 시 참고 후 자체 구현 |
| https://github.com/SteBeeGizmo/DollarUnity | 곡선 입력 템플릿 매칭 | BSD/MIT 고지 보존, 직선 입력 한계 인지 |
| https://github.com/StarKnightt/skate-park | 브라우저 트릭·모션 구조 관찰 | **라이선스 없음: 코드 직접 복사 금지** |
| https://github.com/SK8-ENGINE/skate-3-rust-engine | 고급 물리/트릭 연구 | GPL 및 EA 원본 애셋 문제: 제품 소스로 이식 금지 |
| https://github.com/morluto/rea | 합법적 역공학 도구 | REA 자체도 독립 도구, 필수 빌드 종속성 아님 |
| https://docs.unity3d.com/Manual/com.unity.animation.rigging.html | 발 IK/리깅 | Unity 공식 패키지 사용 |

**REA 사용 규칙:** 설치되어 있으면 권한 있는 프로그램·오픈소스 대상으로 함수/상태/입력 구조를 조사할 수 있다. 없다고 구현을 중단하거나 권한 불명 실행 파일을 요청하지 않는다. 관찰 근거(파일명·함수명·영상 타임코드)를 보고하고 오리지널 코드로 구현한다.

---

## 12. QA 매트릭스와 완료 정의(Definition of Done)

### 12.1 기본 사용자 시나리오

- [ ] 새 프로젝트에서 씬을 열자마자 카메라·스케이터·바닥이 표시된다.
- [ ] Play 누른 뒤 자동 주행하고, 오른쪽 화면 위 스와이프로 Ollie 시도 가능.
- [ ] Kickflip/Tre Flip이 시각적으로 서로 다르고 입력도 구분된다.
- [ ] 의도한 기술을 인식 못하면 해당 기술을 멋대로 실행하지 않는다.
- [ ] 확률 실패 시 `UnderRotation`, `MissedCatch`, `LostBalance` 연출이 구별된다.
- [ ] 무한정 베일, 무한 점프, 지면 관통, 뒤집힌 자세 고착 없음.
- [ ] 연습하면 숙련도 수치와 예상 성공률 변화가 관찰된다.
- [ ] 재시작 후 숙련도가 지속되며 `Reset Battle`은 성장 데이터를 지우지 않는다.
- [ ] 플레이어가 선공·수비하는 S.K.A.T.E. 한 판을 정상 종료한다.
- [ ] HUD를 터치해도 원치 않는 트릭이 발동하지 않는다.
- [ ] PC에서 테스트 가능한 디버그 키는 모바일 릴리스 UI에 드러나지 않는다.

### 12.2 상태·확률·전환 테스트

- [ ] `Rolling → Airborne → Landing → Rolling`과 `Rolling → Airborne → Bail → Recover → Rolling` 정상 전이.
- [ ] 공중 트릭 중 추가 스와이프·두 손가락 동시 터치로 중복 점프 불가.
- [ ] 씬 리로드/게임 종료/기기 전환 때 진행 중 트릭·구독자가 유실·중복되지 않음.
- [ ] 배틀 B01~B10 통과.
- [ ] 입력 품질 하한, 숙련도 단조성, 시드 재현성 테스트 통과.
- [ ] 제스처 인식 Fixture에서 각 기본 기술 20건 이상 정상 샘플 확보, 오류율 기록.

### 12.3 화면·빌드 테스트

| 대상 | 최소 확인 |
|---|---|
| Unity Editor | Compile + PlayMode + Console 검사 |
| 16:9 Landscape | HUD 가독성·트릭 공간·입력 겹침 |
| 19.5:9 Landscape | 안전 영역·카메라 인물 잘림 |
| 20:9 Landscape | 울트라와이드 배경·UI 여백 |
| iOS 실기기(접근 가능할 때) | 터치, 연속 5분 플레이, 가로 safe area, FPS·메모리 |
| Android 실기기(후순위, 접근 가능할 때) | 터치 호환성과 가로 화면을 확인하고 미실행 시 사유 기록 |

### 12.4 전체 완료 조건

**최소 사용자 제공 빌드(MVP):** P0 통과 + P1 기본 입력 3종 + P2 실패 연출 구분 + P3 S.K.A.T.E. 규칙/성장/모바일 HUD가 동작하고, 모든 핵심 재현 테스트를 통과한 상태. iOS 실기기에서 실행하지 못했다면 **'iOS에서 실행 확인'이라고 표현하지 않는다**.

**아트 완성:** 엄지 조작감, 캐치·착지 모션, 카메라, 사운드가 안정적으로 일관된 후 판단. 진짜 서울 배경 재현이나 추가 트릭이 필수조건은 아니다.

---

## 13. Codex가 생성·갱신할 파일

코드 구현과 함께 저장소에 다음 문서를 남긴다.

```text
/docs/SKATER_SEOUL_IMPLEMENTATION_STATUS.md
/docs/SKATER_SEOUL_TEST_REPORT.md
/docs/SKATER_SEOUL_CONTROLS.md
/docs/SKATER_SEOUL_BALANCE.md
/docs/THIRD_PARTY_ASSETS.md
```

### 13.1 IMPLEMENTATION_STATUS

각 기능 `IMPLEMENTED / PARTIAL / BLOCKED / NOT STARTED` 표기, 실제 코드 경로, 미해결 이슈·위험, 다음 작업 순서.

### 13.2 TEST_REPORT

Unity 버전·실행 환경·플랫폼·날짜, 수행 명령·절차, PASS/FAIL/NOT RUN, 실제 콘솔 오류, 스크린샷/로그 위치(존재할 때만), 재현 단계.

### 13.3 CONTROLS

조작 영역, Ollie/Kickflip/Tre Flip 입력 예시, 터치 소유권·취소 처리, 스탠스 변환, 임계값 및 알려진 한계.

### 13.4 BALANCE

각 트릭 난도·숙련도 초기값·성공 확률·경험치 증가량, RNG Seed 테스트, 확률 모델 한계 및 향후 튜닝 포인트.

### 13.5 THIRD_PARTY_ASSETS

모든 외부 파일의 원래 URL·제작자·라이선스·다운로드 일자·고지 요건·배포 가능 여부. 가져온 외부 파일이 없으면 **`No third-party assets imported`**로 작성한다.

---

## 14. 최종 보고 형식 (Codex의 마지막 응답)

다음 순서로 압축적으로 보고한다.

1. **현재 실행 상태:** Unity Editor 열림 여부, 씬 경로, 실제 Play 확인 여부, 빌드 결과.
2. **완료한 Phase:** P0~P4 각각 `PASS / PARTIAL / BLOCKED / NOT RUN`.
3. **구현된 플레이 루프:** 주행 → 제스처 → 트릭 → 착지·실패 → 성장 → 배틀을 실제로 검증한 범위.
4. **수정·생성 파일 목록:** 기존 파일 변경과 신규 파일 구분.
5. **테스트 증거:** 실행한 테스트, 통과/실패 수, 콘솔·빌드 오류, 실제 디바이스 테스트 여부.
6. **알려진 문제 3~7개:** 중요도, 재현 순서, 권장 해결책.
7. **다음 실행 명령/Unity 메뉴:** 사용자가 Editor에서 무엇을 눌러 게임을 실행하는지.
8. **다음 한 단계:** 가장 가치 높은 작업 하나만 추천.

완료하지 못한 일을 완료라고 포장하지 말고, 실제로 플레이 가능한 결과를 우선한다.

---

## 15. 최종 사용자 수용 기준 — 게임성을 검증하는 질문

기술적으로 돌아가는 것이 끝이 아니다. 첫 10분 체험에서 다음이 성립해야 한다.

1. 튜토리얼을 읽지 않고도 1분 내 Ollie를 시도할 수 있는가?
2. Kickflip을 시도했는데 엉뚱한 기술이 나오는 비율이 충분히 낮은가?
3. Tre Flip이 어려운 이유가 조작 품질과 숙련도에 연결되어 느껴지는가?
4. 실패 모션만 보고도 회전 부족·캐치 실패·착지 균형 실패가 어느 정도 구분되는가?
5. 같은 기술을 연습할수록 실제 성공률과 체감 안정성이 함께 좋아지는가?
6. 플레이어가 S.K.A.T.E. 배틀에서 어떤 기술을 제시할지 **전략적으로 고민하게 되는가**?

**이 여섯 질문 중 전반부 1~4개가 실패한다면**, 추가 맵·새 트릭·NPC·스토리보다 조작감·애니메이션·확률 판정을 먼저 고친다.

---

## 16. Codex에 그대로 전달할 착수 프롬프트

> 현재 저장소에 이미 가져온 `Assets/SkaterSeoul` 스타터 코드를 기반으로 첨부한 `SKATER_SEOUL_PRD_v1.0.md`를 **실제 구현 명세로 실행**하라. 먼저 실제 프로젝트 구조와 ZIP 파일의 코드·문서를 읽고 현재 컴파일 및 Play Mode 상태를 검증하라. P0부터 순차 진행하고 P0이 안정화되기 전에는 그래픽/대규모 리팩터링을 하지 마라. 변경분을 기존 사용자 작업과 분리·보존하고, 실제 Unity Editor를 사용할 수 없다면 그 사실을 명시하라. 구현한 부분은 테스트와 함께 제출하고, 완료하지 않은 범위를 성공했다고 표시하지 마라. 계획만 주지 말고 실제 파일을 수정하라. 마지막에 `docs/SKATER_SEOUL_IMPLEMENTATION_STATUS.md`와 `docs/SKATER_SEOUL_TEST_REPORT.md`를 생성하고 실행 방법을 요약하라.
