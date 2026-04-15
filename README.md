# NexusFrame

> 다년간 라이브 서비스와 개발 경험에서 마주친 구조적인 문제들을 직접 해결하기 위해 만든 개인 프레임워크.

게임 프로젝트를 진행하다 보면 구조적인 해결이 필요하다고 느끼는 지점들이 생긴다.
매 프로젝트마다 임시방편으로 막는 대신, 한 번 제대로 설계해두기 위해 시작했다.

> **진행 중인 프로젝트.** 씬 관리 · 플레이어 시스템 · 전환 구조 구현 완료. 추가 시스템 개발 중.

<br>

---

## 어떤 문제를 풀었는가

<br>

### 문제 1 — 씬 전환 타이밍

전환 중 빈 화면이 노출되거나, 전환 연출이 불규칙하게 나타났다 사라지는 문제.
전환 순서가 코드 여기저기 흩어져 있고, 호출 시점이 보장되지 않는 게 원인이다.

**해결:** `GamePlaySystem`이 전환 순서를 코드 레벨에서 강제한다.
세션 전환은 항상 아래 순서로 진행되며, `await using` Scope 패턴으로 예외 상황에서도 보장된다.

```
[ 이전 세션 ]  SessionOut → UI 숨김 → Transition 시작
[ 전환 구간 ]  이전 세션 제거 → 새 세션 로드 완료 대기
[ 다음 세션 ]  새 세션 시작 → Transition 종료 → SessionIn
```

<br>

### 문제 2 — Singleton 초기화 순서

`DontDestroyOnLoad` 남용으로 생성 순서와 의존 관계가 불투명해지면, 초기화 타이밍 버그는 재현도 고치기도 어렵다.
VContainer 등 외부 DI를 검토했지만 이 규모에서는 과하다고 판단해, 필요한 범위만 직접 구현했다.

**해결:** Preload 씬이 초기화 순서를 씬 로드 순서로 명시적으로 제어한다.

```
App Start → Preload.unity 로드
  └─ SceneDirector  (씬 전환 단일 진입점)
  └─ Transition     (전환 연출)
  └─ GamePlaySystem (Session Stack 관리)
```

`DontDestroyOnLoad` 직접 호출 없이, Preload 씬 자체가 영속성을 보장한다.

<br>

### 문제 3 — 캐릭터 제어 코드 복잡도

입력 처리, 카메라 공간 변환, 이동 로직이 한 곳에 뭉치면 동작 하나 추가할 때마다 전체를 건드리게 된다.

**해결:** Cinemachine의 `IInputAxisOwner` 인터페이스를 활용해 세 컴포넌트로 분리했다.

- `PlayerControllerBase` — 입력 계약 정의, 입력 소스 교체 가능
- `PlayerController` — 카메라 공간 → 월드 공간 변환 후 이동만 담당
- `CameraRelativeInputFrameResolver` — 카메라 각도 블렌딩, 머리 위 반전 문제 해결

현재 점프 · 공격 · 앉기 등 기본 동작 확장 중.

<br>

---

## 핵심 설계

<br>

### Session Stack

씬 전환(`SceneDirector`)과 게임 상태(`GamePlaySystem`)를 분리한 게 핵심 설계 결정이다.

| 전환 | 동작 |
|------|------|
| `Replace` | 현재 Session 제거 후 교체 |
| `Push` | 현재 Session 유지한 채 새 Session 추가 |
| `Pop` | 현재 Session 제거, 이전 Session 복귀 |
| `PushOrReplace` | Stack 크기에 따라 자동 선택 |

Push로 덮인 Session은 `Paused`(오브젝트 유지) 또는 `Slept`(비활성화) 상태로 대기하다가, Pop 시 `Resumed`로 돌아온다.

<br>

### Transition

`ITransitionEffect` 인터페이스로 전환 효과를 런타임에 교체할 수 있다.
`Begin() / End()`는 Reference Count 기반으로 중첩 호출해도 안전하다.

- `FadeTransitionEffect` — 0.5초 알파 페이드
- `InstantTransitionEffect` — 즉시 전환

<br>

---

## 개발 방식 — Claude Code 활용

설계 의사결정, 리팩토링, 반복 작업 자동화에 Claude Code를 적극 활용하고 있다.
커스텀 슬래시 커맨드(`/check-convention`, `/fix-warnings`, `/git-commit` 등)로 워크플로우를 자동화하고,
CLAUDE.md에 Unity 주의사항을 누적해서 반복 오류를 줄이는 방식으로 운용하고 있다.

<br>

---

## 기술 스택

| | |
|--|--|
| **Unity** URP 17.3.0 · **C#** | 클라이언트 전반 |
| **UniTask** | async/await 기반 씬 로딩 · 전환 시퀀스 |
| **Cinemachine** 3.1.6 · **Input System** 1.18.0 | 카메라 · 입력 |
| **Claude Code** | AI 보조 개발 워크플로우 |

<br>

---

## 씬 플로우

```
App Start → Splash → Title → MainMenu → World0 (Level)
                                              └─ GamePlaySystem.LaunchSession()
                                                    └─ Exploration/World0
```

에디터에서 중간 씬을 직접 실행할 경우 `ColdStartup.cs`가 누락된 필수 씬을 자동으로 로드해준다.

<br>

---

## 진행 상황

**완료**
- Preload 씬 기반 Singleton 관리
- SceneDirector — 씬 전환 단일 진입점
- Transition — Fade / Instant, Reference Count 기반 중첩 안전
- GamePlaySystem — Session Stack (Replace / Push / Pop / PushOrReplace)
- Session 생명주기 (Created → Played → SessionOut/In → Paused/Slept → Resumed → Stopped → Destroyed)
- Stage 타입 (Level / AdditiveLevel / SubLevel / PrefabInstance)
- 플레이어 시스템 — 입력 / 이동 / 애니메이션 분리
- CameraRelativeInputFrameResolver
- Session / Stage Pool
- Runtime 씬 플로우 테스트 (Splash→World0, ColdStartup 시나리오)

**예정**
- 캐릭터 동작 확장 (점프 · 공격 · 앉기 등)
- UI 레이어 시스템 (팝업 스택 관리)
- 인벤토리 / 아이템 기반 시스템
- 세이브 / 로드 구조

<br>

---

## 폴더 구조

```
Assets/
  NexusFrameAssets/       # 메인 프레임워크 (UPM 레이아웃)
    Runtime/
    Editor/
    Tests/
  {PackageName}Assets/    # 독립 패키지는 동일 레벨에 추가
```