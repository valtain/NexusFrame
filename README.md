# NexusFrame

Unity 게임 클라이언트 개발 경험에서 반복적으로 마주친 구조적 문제들을 직접 해결하기 위해 만든 개인 프레임워크.

> 진행 중인 프로젝트입니다. 현재 씬 관리·플레이어 시스템·전환 구조가 구현되어 있으며, 추가 시스템은 지속 개발 중입니다.

---

## 만들게 된 이유

라이브 서비스를 운영하면서 반복적으로 겪은 문제들이 있었다.

- 씬 전환 시 UI가 사라진 뒤 빈 화면이 노출되는 타이밍 문제
- `DontDestroyOnLoad` 남용으로 Singleton 생명주기와 초기화 순서가 불투명해지는 문제
- 카메라가 플레이어 머리 위를 넘어갈 때 이동 방향이 반전되는 입력 문제

이 문제들을 해결하는 구조를 직접 설계하면서, 라이브 서비스에서 쓸 수 있는 수준의 프레임워크를 목표로 발전시키고 있다.

---

## 핵심 설계

### Preload 씬 기반 Singleton 관리

외부 DI 컨테이너 없이 생명주기와 초기화 순서를 명시적으로 제어하는 구조.

`MonoPreload<T>` 베이스 클래스를 상속한 Singleton들이 Preload 씬에서 생성되어 앱 전체 생존한다. `DontDestroyOnLoad` 직접 호출 없이 Preload 씬 자체가 영속성을 보장한다.

```
App Start → Preload.unity 로드
  └─ SceneDirector 생성 (씬 전환 단일 진입점)
  └─ Transition 생성 (전환 연출 담당)
  └─ GamePlaySystem 생성 (Session Stack 관리)
  └─ 이후 모든 씬 전환은 SceneDirector.LoadScene() 경유
```

### GamePlaySystem - Session Stack 기반 게임 진행 관리

씬 전환 타이밍 문제를 해결하기 위해 설계한 핵심 시스템. `SceneDirector`가 씬 로딩을 담당하고, `GamePlaySystem`이 게임 상태(Session)를 Stack으로 관리한다.

**전환 순서를 코드 레벨에서 강제한다:**

```
EnterSessionOut → Transition.Scope() 진입 → 언로드/로드 → Scope 해제 → EnterSessionIn
```

이 순서가 깨지면 빈 화면 노출이나 전환 UI 불규칙 노출 같은 문제가 생긴다. `GamePlaySystem`은 이 순서를 항상 보장한다.

**4가지 전환 방식:**

| 전환 | 동작 |
|------|------|
| `Replace` | 현재 Session 제거 후 새 Session으로 교체. Level 스테이지 교체 시 스택 전체 제거 |
| `Push` | 현재 Session을 Stack에 유지한 채 새 Session 추가 |
| `Pop` | 현재 Session 제거, 이전 Session 복귀 |
| `PushOrReplace` | Stack이 비어 있으면 Replace, 1개면 Push, 2개 이상이면 Replace |

**Stage 타입** - Session이 로드하는 씬/오브젝트의 종류:

- `Level` - 일반 게임 레벨 씬
- `AdditiveLevel` - 추가 로드 레벨 씬
- `SubLevel` - 기존 Stack 내 씬 재활용 (Anchor 기반)
- `PrefabInstance` - 씬 없이 Prefab 인스턴스로 구성

**Session 타입** - 게임 상태의 종류:

- `Exploration` - 탐색/이동
- `Battle` - 전투
- `Narrative` - 대화/연출

**Push 시 이전 Session 처리 방식:**

`DoOverrideStage` 플래그로 결정된다. 새 Session이 스테이지를 덮어쓰면 이전 Session은 `Paused` (오브젝트 유지), 그렇지 않으면 `Slept` (오브젝트 비활성화) 상태로 전환된다. 두 상태 모두 Pop 시 `Resumed`로 복귀한다.

**Session 생명주기:**

```
Created → Played → SessionOut → (Paused | Slept | Stopped) → (Resumed | Destroyed) → SessionIn
```

- `SessionOut` / `SessionIn` - 전환 연출 바깥(화면이 보이는 구간)의 진입/이탈 콜백
- `Paused` / `Slept` - Push로 덮인 상태 (Paused: 스테이지 덮어씀, Slept: 오브젝트 비활성화)
- `Stopped` / `Destroyed` - Replace 또는 Pop으로 완전히 제거되는 흐름

**인게임 진입점 - `NaivePortal`:**

Collider 기반 트리거 컴포넌트. Inspector에서 Session/Stage 설정을 직접 지정하면 플레이어가 충돌했을 때 `GamePlaySystem.LaunchSession()`을 호출한다. 씬 코드 없이 레벨 전환을 구성할 수 있다.

### SceneDirector - 씬 전환 타이밍 보장

씬 전환 타이밍 문제를 해결하기 위한 단일 진입점.

모든 씬 전환이 `SceneDirector.LoadScene()`을 경유하며, 전환 전후 `Transition.Begin() / End()`가 자동으로 감싸진다. Level 씬은 필수 씬(Preload, GamePlay)을 자동으로 선로드한다.

| SceneType | 역할 | 동작 방식 |
|-----------|------|-----------|
| Preload (0) | Singleton 생성 | 항상 상주 |
| Splash / Title / MainMenu | 화면 전환 | 전환 시 언로드 |
| GamePlay (99) | 게임플레이 환경 | 상주, 한 번만 로드 |
| Level (100+) | 게임 레벨 | 전환 시 언로드 |
| Test (1000+) | 에디터 전용 | - |

에디터에서 중간 씬을 직접 실행할 경우 `ColdStartup.cs`가 누락된 필수 씬을 자동으로 로드한다 (`#if UNITY_EDITOR`).

### Transition - 전환 연출

Strategy 패턴으로 전환 효과를 교체 가능하게 설계.

- `FadeTransitionEffect` - 0.5초 동안 알파 0.2 → 1.0 페이드
- `InstantTransitionEffect` - 즉시 전환

`Begin() / End()`는 Reference Count 기반으로 중첩 호출에 안전하다. `GamePlaySystem`은 `Scope()` 패턴(`await using`)으로 Begin/End 쌍을 자동 보장한다.

### 플레이어 시스템

입력 → 이동 → 애니메이션 파이프라인을 세 컴포넌트로 분리.

- `PlayerControllerBase` - Cinemachine과 동일한 `IInputAxisOwner` 인터페이스로 입력 계약 정의
- `PlayerController` - `CharacterController` 기반 이동. 카메라 공간 입력을 월드 공간으로 변환
- `PlayerAnimator` - 컨트롤러와 독립적으로 트랜스폼 델타를 읽어 애니메이터 파라미터 구동

`CameraRelativeInputFrameResolver`가 카메라가 플레이어 머리 위를 넘을 때 입력 방향이 반전되는 문제를 블렌딩으로 해결한다.

---

## 씬 플로우

```
App Start
  └─ Splash → Startup.cs
       └─ SceneDirector.EnsurePreloadReady() → Preload.unity 로드
            └─ SplashController (페이드 시퀀스)
                 └─ Title → TitleController (아무 키)
                      └─ MainMenu → MainMenuController (New Game)
                           └─ World0 (Level)
                                └─ SceneDirector가 Preload + GamePlay 자동 로드
                                     └─ GamePlaySystem.LaunchSession() → Exploration/World0
```

---

## 기술 스택

- **Unity** URP 17.3.0
- **UniTask** - async/await 기반 씬 로딩, 전환 시퀀스
- **Cinemachine** 3.1.6
- **Input System** 1.18.0
- **C#**

---

## 빌드 & 테스트

빌드: Unity Editor → File > Build Settings

테스트: Unity Editor → Window > General > Test Runner
- Runtime 테스트: `Assets/NexusFrameAssets/Tests/Runtime`
  - `SceneFlowTests` - Splash→World0 전체 플로우 및 ColdStartup 시나리오 검증

---

## 진행 상황

- [x] Preload 씬 기반 Singleton 관리
- [x] SceneDirector 씬 전환 시스템
- [x] Transition (Fade / Instant)
- [x] GamePlaySystem - Session Stack (Replace / Push / Pop / PushOrReplace)
- [x] Session 생명주기 (Created → Played → SessionOut/In → Paused/Slept → Resumed → Stopped → Destroyed)
- [x] Stage 타입 (Level / AdditiveLevel / SubLevel / PrefabInstance)
- [x] NaivePortal - Inspector 설정 기반 인게임 세션 전환 트리거
- [x] 플레이어 시스템 (입력 / 이동 / 애니메이션 분리)
- [x] CameraRelativeInputFrameResolver
- [ ] Session / Stage Pool
- [ ] 추가 예정

---

## 폴더 구조

```
Assets/
  NexusFrameAssets/       # 메인 프레임워크 패키지 (UPM 레이아웃)
    Runtime/
    Editor/
    Tests/
  {PackageName}Assets/    # 독립 패키지는 동일 레벨에 추가
```
