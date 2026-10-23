# SKATER SEOUL — Legacy Migration Plan

기준: docs/SKATER_SEOUL_PRD_v1.0.md, docs/LEGACY_MIGRATION_AUDIT.md  
순서: 3D MVP 실행 검증 → 도메인 규칙 → 커리어/성장 → 친구 대전

## 실행 원칙

- 현재 source pack을 그대로 보존한다. 이 폴더는 완전한 Unity 프로젝트가 아니며 README는 수신 Unity 프로젝트가 정해지기 전 외부 프로젝트로 옮기지 않도록 한다.
- PRD의 P0가 PARTIAL인 동안 도메인/저장 대규모 병합을 시작하지 않는다. 기존 구현 현황에 이미 기록된 임시 Unity 테스트와 CoreTests 결과를 실제 URP import나 실기기 합격으로 바꾸어 기록하지 않는다.
- 매 단계에서 판정, 입력, 성장, 경기의 단일 소유자를 유지한다. 레거시와 prototype이 동시에 같은 실행을 판단하거나 XP를 지급하지 않게 한다.
- 원본 레거시 저장 키는 읽기 전용으로 취급한다. import가 필요할 경우 새 저장 키에 복사하고 검증하기 전에는 원본 키를 쓰거나 삭제하지 않는다.

## 단계 및 의존성

| 단계 | 시점 / 대상 | 작업 | 통과 조건 |
|---|---|---|---|
| 0. MVP 게이트 | 지금 / Assets/SkaterSeoul | PRD P0를 먼저 끝낸다. Ollie, Kickflip, Tre Flip, 배틀 한 판, 실패 후 복귀, 재시작 경로를 현재 소유 구조에서 확인한다. 생성 씬은 기존 파일을 덮지 않는다 | PRD P0 항목을 실제 통과/NOT RUN으로 남긴다. 현 상태는 implementation status 기준 PARTIAL |
| 1. 도메인 기준선 | P0 이후 / Types.cs, TrickCatalog.cs | 순수 Core를 새 3D 프로젝트의 코드 구조에 복사/참조하기 전에 stable enum 및 39개 catalog 사양을 고정한다. MVP의 세 트릭은 이름 기준으로 Ollie=0, Kickflip=3, TreFlip=24에 연결한다 | 저장 인덱스 회귀 검사와 Unity compile/import 통과. 잘못된 enum/배열 캐스팅 없음 |
| 2. 입력 어댑터 | P1 / SkateTouchInput.cs, 현재 Input System 입력부 | Begin/Move/End, preview 안정화, pointer ownership, UI 제외, cancel 의미를 유지한다. 기존 동시 PUSH를 잃지 않도록 트릭 세션과 push control ownership을 분리한다. MVP는 현 3기술만 활성화한다 | 3개 방향 및 경계 입력, HUD 시작, cancel, 잘못된 pointer 종료, 별도 PUSH 손가락을 회귀한다. 8방향 슬롯 6의 미지원 상태를 숨기지 않는다 |
| 3. 단일 판정 파이프라인 | P1–P2 / Judge.cs, TrickSuccessModel.cs, Tuning.cs | 새 3D 물리에서 착지 샘플을 만들고, 기계적 조건을 먼저 확인한다. 유효 후보에 확률을 한 번 적용하고 결과/attempt ID를 한 번 발행한다. 실패 사유에 맞는 표시를 결과 이벤트에 연결한다 | 기계적 실패에서는 RNG를 소비하지 않음, 유효 시도당 1 draw 및 1 결과, 같은 seed 재현, 회전/캐치/착지 수치와 연출 일치 |
| 4. 성장과 저장 호환 | P3 전 / Progress.cs와 새 저장소 | 새 저장 schema/key를 결정한다. 가져올 경우 legacy v13을 원본 키에서 읽기 전용으로 복사, stable enum 값 기준으로 변환, 새 키에 기록한다. mastery뿐 아니라 stats, observed/learned, career, missions, attempts, settings를 필드별로 승인한다 | v1/v2/v13 fixture, 짧거나 긴 배열, 음수/범위 초과, 마스크, 중복 보상, 변환 재실행을 확인한다. 이전 데이터와 새 데이터를 비교하고 원본 키 보존 |
| 5. 성장 상태와 보상 | MVP 후 / Career.cs, Missions.cs | 학습 상태와 XP, 12개 챌린지, 일일 3개 선택 및 9개 후보, 완료/수령 중복 방지를 연결한다. 실제 구현과 다른 Practiced/80 기준 및 콤보는 제품 결정 전에 제외한다 | 12 challenge ID/목표/보상/해금 표 일치, 일자 seed 재현, 완료·별 상승·미션 claimed 재호출에서 보상 중복 없음, 무제한 자유 연습 |
| 6. 로컬 S.K.A.T.E. | MVP 배틀 교체 시 / SkateMatch.cs | 현재 bot UI를 상태 엔진으로 이동한다. setter success, matcher 동일 기술, 불일치, timeout, 3/5 글자, 승패를 이벤트로 표현한다. bot RNG와 매치 RNG는 시도 판정 RNG와 분리한다 | 모든 phase transition, 역할 유지/교체, 15초 타임아웃, 글자 3/5, reset 후 새 매치, 동일 attempt 중복 처리 검사 |
| 7. 친구 대전 | 친구 대전 마일스톤 / FriendSkateMatch.cs 우선 | 도메인 규칙만 먼저 검토한다. 전송은 별도 프로토콜·서버 권위·버전 호환 결정을 마친 뒤 연결한다 | 서버에서 시도 샘플을 재생하고 규칙 버전·순서·타임아웃·중복 요청을 검증하기 전에는 랭크 보상 없음 |

SkateSim.cs와 FlowSim.cs는 단계별로 통합하지 않는다. 필요하면 상태 이름과 attempt/result 계약만 비교 자료로 쓰고, 3D 이동과 충돌은 새 Physics 소유 구조로 유지한다. TrickMotion, Spot, SurfaceQuery, SkaterView, WorldView도 새 3D 동작과 씬을 직접 참조하지 않는다.

## 저장 마이그레이션 계약

레거시 형식은 ProgressData version 13, PlayerPrefs 키 skater_progress_v3이며 구 키 v2/v1도 있다. 데이터 필드는 39칸 정수 배열이다. 새 prototype의 SS_Mastery_0..2는 현재 float 3칸이며 별도 규칙/키다.

이식 시 지킬 매핑 규칙:

1. legacy enum의 이름과 정수값으로만 기술을 식별한다. 현재 인덱스 0/1/2와 legacy ID가 같은 뜻이라고 가정하지 않는다.
2. MVP 기술만 가져오더라도 legacy mastery의 0, 3, 24번을 의미 기반으로 선택한다. 다른 36개 값은 임시 저장공간에서 유지하거나, 수신 schema가 전부를 보존하지 못하면 import를 하지 않는다.
3. int 0–100에서 float 0–100으로 바꾸는 과정에서 범위와 정밀도를 명시한다. observed, learned, trickAttempts는 mastery와 별개 필드로 매핑한다.
4. career/missions/totals/settings를 버리는 MVP 단순화는 별도의 승인된 저장 변환 정책 없이는 하지 않는다. 미지원 필드가 있으면 새 버전 저장에 보존할지, 원본만 보관할지 결정한다.
5. 신규 schema에 sourceVersion, schemaVersion, import 완료 표시를 둔다. 변환 결과가 검증되기 전에 legacy 키를 삭제하지 않는다.
6. Progress.Load의 자동 Save 때문에 테스트는 원본 PlayerPrefs를 쓰지 않는다. 복제 fixture나 임시 identifier에서만 migration을 실행한다.

## 회귀 검증 계획

기존 385 CoreTests는 호환되는 순수 규칙의 회귀 기준으로 활용한다. 현재 suite가 UnityEngine, PlayerPrefs, 새 prototype, Game/Progress, Netcode 연결을 확인하지 않는다는 제한을 매 보고서에 유지한다. 이식 후에는 기존 .NET 기준선만으로 Unity 실행 검증을 대체하지 않는다.

| 영역 | 최소 회귀 사례 |
|---|---|
| 카탈로그 | 39 ID, 순서, 이름, 카테고리, 티어, 보상 포인트, MVP ID 매핑, 12 챌린지 해금 |
| 터치 | 8방향 입력 재생, 지원되지 않는 슬롯 거부, 미리보기 안정화, UI 터치 제외, 손가락 ID 소유권, 취소/화면 밖 종료, 입력 품질 |
| 판정 | under-rotation/누락 캐치/불안정 착지를 각각 한 번만 결과화, 기계적 실패와 확률 실패 구분, 유효 시도만 단일 난수, seed 재현 |
| 능력치 | 6개 능력치 범위 및 개별 영향. Agility/Balance/Landing 등이 물리와 성공 확률에 중복 반영되지 않는 계약 확인 |
| 성장/저장 | version 1, 2, 13 fixture, 39칸의 ID 보존, 3기술 매핑, observed/learned/mastery, reward claimed, 손상 JSON, 자동 Save 전에 원본 보존 |
| 미션/커리어 | 12 challenge의 완료·별·XP·unlock, 9개 mission pool에서 날짜별 3개 재현, 완료·수령 중복 지급 방지, 자유 연습 무제한 |
| S.K.A.T.E. | 3/5자, 선공/매칭 역할, 맞는 기술/다른 기술, setter 실패, timeout, 5글자 패배, 경기 종료 이후 입력 무시, reset |
| Unity 연결 | 수신 Unity 6 URP 프로젝트 import/compile, Play Mode 실제 착지와 터치, 생성 씬 보존, iOS safe-area/기기 터치. 수행 전까지 NOT RUN |

## 이번 감사에서 수행한 변경

- 작성: docs/LEGACY_MIGRATION_AUDIT.md와 본 문서.
- 실행: 레거시 CoreTests 385 passed, 0 failed.
- 이식하지 않음: Core C# 파일, Unity 입력 adapter, Progress 저장, 커리어/미션, 레거시 씬과 그래픽. 현재 MVP와 결합 시 중복 시스템이 되고, 이 작업 공간에 실제 URP 프로젝트도 없으므로 이식 조건이 충족되지 않는다.
- 수정하지 않음: 레거시 저장 데이터, 레거시 Unity 프로젝트의 소스/설정, 현재 source pack의 기존 C# 및 씬.

## 후속 요청으로 반영한 최소 이식 (2026-10-23)

- `SkateTrick` MVP 값은 레거시 고정 ID `Ollie=0`, `Kickflip=3`, `TreFlip=24`를 사용한다. 현재 `SS_Mastery_0..2` 저장 데이터는 `MasterySlot` 매핑을 통해 기존 슬롯 순서로 계속 읽고 쓴다.
- 정상 착지 후보의 확률 판정은 접지 시점에 한 번 수행한다. 입력 품질 하한과 방어 중 잘못된 기술은 난수 없이 실패한다. 유효 시도에서 얻은 하나의 roll로 성공 여부와 실패 시 MissedCatch/LostBalance 표시를 결정한다.
- 기존 Editor 규칙 점검에 stable ID·슬롯 round-trip·단일 roll 경계 검사를 추가한다.
- 레거시 전체 TrickCatalog, 8방향·Charge/Preview 입력, `Progress` 저장 import, 스탯/커리어/미션, 15초/3글자 선택 규칙, 친구 대전은 현재 PRD의 3트릭·5글자 MVP 범위 및 데이터 안전 조건 때문에 보류한다. 특히 현재 3D 판정에는 레거시 Judge가 요구하는 완전한 착지 샘플이 없어 Judge 파일을 그대로 가져오지 않았다.
- 레거시 프로젝트 소스, 설정 및 저장 데이터는 변경하지 않았다. Unity 입력 실기기 확인과 완전한 물리 판정은 아직 이식 범위 밖이다.
- 검증: 수정된 파일을 disposable Unity 6000.3.10f1 프로젝트에 반영해 Editor static rule validation을 실행했고 PASS했다. 프로젝트에 URP가 없어 URP import, Play Mode, 실기기 입력 확인으로 확대 해석하지 않는다.

## Visual overhaul implementation update (2026-10-23)

The implementation target is now the independent `UnityProject/` Unity 6000.3.10f1 URP project. This supersedes the earlier “wait for a Unity project” gate; it does not modify the legacy source project.

- Imported stable catalog/types, Judge, tuning, success model, motion target data, v13 Progress, Career, Missions and SkateMatch. The live slice routes physical landing candidates through legacy Judge and probability, records practice growth, and uses the legacy local match for player-set/bot-match state.
- Unity scene and Play Mode evidence, completion boundaries and QA results are recorded in `docs/VISUAL_OVERHAUL_QA.md`.
- The current input/UI and bot pool cover Ollie, Kickflip and Tre Flip. Full 39-trick play, 8-way input, Career/Missions UI, friend/online match, complete five-letter game, and end-to-end save migration from a real installed app are deferred/not verified. `SkateSim` and `FlowSim` remain excluded because their 2D simulation would compete with Unity Rigidbody ownership.
- Do not delete legacy progress keys until a real old-save fixture/device migration has been verified. The local validation probe snapshots and restores the user's existing PlayerPrefs keys.
