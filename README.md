# NexusFrame

라이브 서비스 개발에서 반복적으로 마주친 구조적 문제들을 직접 풀어보려고 만든 Unity 개인 프레임워크.

> 진행 중인 프로젝트입니다. 씬 관리·Session Stack·플레이어 시스템이 구현되어 있고, 계속 확장 중입니다.

---

## 왜 만들었나

오랫동안 라이브 서비스를 운영하면서 비슷한 문제들이 반복됐다.

- Singleton 생명주기와 초기화 순서를 명확하게 제어할 방법이 없어서, `DontDestroyOnLoad`를 여기저기 쓰다 보면 언제부터인가 흐름이 불투명해지는 문제
- 씬 전환 순서를 코드로 강제하지 않으면 타이밍이 어긋나고, 유저 입장에서는 화면이 버벅이거나 전환이 어색하게 느껴지는 문제
- Cinemachine 등 외부 카메라 시스템과 플레이어 입력을 함께 다루다 보면 컴포넌트 하나에 책임이 몰리면서 코드가 빠르게 복잡해지는 문제

이걸 한번은 제대로 구조로 풀어보고 싶었다.

---

## Preload 씬 기반 Singleton 관리

Unity에서 Singleton을 유지하는 흔한 방법은 `DontDestroyOnLoad`다. 하지만 이걸 여기저기 쓰다 보면 어떤 오브젝트가 언제 생성되고 얼마나 사는지 파악하기 어려워진다.

NexusFrame은 씬 자체를 수명의 단위로 쓴다. 역할에 따라 두 씬으로 나눠 필요한 시점에만 로드한다.

**Preload 씬** — 앱 전체에서 항상 필요한 요소. 시작부터 종료까지 상주한다.

| 오브젝트 | 역할 |
|----------|------|
| `SceneDirector` | 씬 로딩 전담 |
| `Transition` | 전환 연출 |
| `Camera` | 메인 카메라 |
| `EventSystem` | UGUI 이벤트 처리 |

**GamePlay 씬** — 게임플레이 구간에서만 필요한 요소. Level 씬 로드 시 함께 올라오고, 메뉴 구간에서는 로드하지 않는다.

| 오브젝트 | 역할 |
|----------|------|
| `GamePlaySystem` | Session Stack 관리 |

`MonoPreload<T>`는 두 씬 모두에서 쓰는 Singleton 베이스 클래스다. 차이는 어느 씬에 배치하느냐, 즉 **수명의 범위**다. `DontDestroyOnLoad` 호출 없이 씬이 영속성을 보장하고, 초기화 순서는 씬 로드 순서로 명시적으로 제어된다.

---

## GamePlaySystem — Session Stack

이 프레임워크의 핵심. 게임 상태(Session)를 Stack으로 관리하며, 씬 전환 타이밍과 생명주기 전체를 제어한다.

`SceneDirector`는 씬 로딩·언로딩만 전담하는 하위 수단이다. GamePlaySystem이 언제 로드하고 언로드할지 결정하고, SceneDirector는 그 명령을 실행한다.

### 설계 철학

게임 진행은 **어디서(Stage)** 와 **무엇을 하는가(Session)** 로 분리해서 관리한다.

- `Stage` — 현재 로드된 씬이나 오브젝트. 물리적인 공간
- `Session` — 그 위에서 실행 중인 게임 상태. 탐색인지, 전투인지, 대화인지

둘을 분리하면 같은 공간을 여러 게임 흐름에서 공유해서 쓸 수 있다. 예를 들어 필드에서 몬스터를 만나 Battle Session으로 전환해도 필드 Stage는 그대로 유지된다. 전투가 끝나면 Pop으로 돌아오고, 필드를 다시 로드할 필요가 없다.

### Session 타입

게임 상태를 나타내는 단위. 각 Session은 하나의 Stage(씬 또는 오브젝트)를 갖는다.

`Exploration` / `Battle` / `Narrative`

### Session 생명주기

단일 Session의 상태 흐름.

```
Created → Played → SessionIn → ... → SessionOut → (Paused | Slept | Stopped) → (Resumed | Destroyed)
```

- `Created` / `Destroyed` — 생성·소멸
- `Played` — 씬 로드 완료, 활성 상태 진입
- `SessionIn` — 화면이 보이기 시작하는 시점의 콜백
- `SessionOut` — 화면에서 사라지기 직전의 콜백
- `Paused` — Push로 덮인 상태. 오브젝트 유지
- `Slept` — Push로 덮인 상태. 오브젝트 비활성화
- `Resumed` — Paused 또는 Slept에서 복귀
- `Stopped` — 제거 직전 상태

### 전환 순서

전환 시 이전 Session과 이후 Session의 흐름이 맞물린다.

```
[이전] SessionOut → Transition 시작 → Paused / Slept / Stopped+Destroyed
[이후]                                 Created+Played / Resumed → Transition 종료 → SessionIn
```

이 순서를 코드 레벨에서 강제한다. 흐트러지면 빈 화면 노출이나 전환 UI 깜빡임 같은 문제로 이어진다.

### Stage 타입

Stage는 게임 상태(Session)와 분리되어 로딩 방식에만 집중한다. 같은 Session 타입이라도 어떤 Stage를 쓰느냐에 따라 씬을 새로 로드할 수도, 기존 씬을 그대로 쓸 수도, Prefab으로 구성할 수도 있다.

| 타입 | 설명 |
|------|------|
| `Level` | 일반 게임 레벨 씬 |
| `AdditiveLevel` | 추가 로드 레벨 씬 |
| `SubLevel` | 기존 Stack 내 씬 재활용 (Anchor 기반) |
| `PrefabInstance` | 씬 없이 Prefab 인스턴스로 구성 |

### 전환 방식

| 전환 | 동작 |
|------|------|
| `Replace` | 현재 Session 제거 후 교체. Level 스테이지면 스택 전체 제거 |
| `Push` | 현재 Session 유지한 채 새 Session 추가 |
| `Pop` | 현재 Session 제거, 이전 Session으로 복귀 |
| `PushOrReplace` | 전투처럼 반복 진입하는 Session이 스택에 중복으로 쌓이는 걸 방지하기 위해 사용. Level만 있으면 그 위에 Push, 이미 다른 Session이 있으면 Replace |

Push 시 이전 Session 처리는 `DoOverrideStage` 플래그로 결정된다. 스테이지를 덮어쓰면 `Paused`(오브젝트 유지), 아니면 `Slept`(오브젝트 비활성화).



---

## SceneDirector — 씬 로딩 전담

씬 로딩·언로딩만을 전담하는 단일 진입점. GamePlaySystem의 지시에 따라 동작한다.

모든 씬 전환이 `SceneDirector.LoadScene()`을 경유하며, Level 씬 로드 시 필수 씬(Preload, GamePlay)을 자동으로 선로드한다.

| SceneType | 역할 | 동작 |
|-----------|------|------|
| Preload (0) | Singleton 생성 | 항상 상주 |
| Splash / Title / MainMenu | 화면 전환 | 전환 시 언로드 |
| GamePlay (99) | 게임플레이 환경 | 상주, 한 번만 로드 |
| Level (100+) | 게임 레벨 | 전환 시 언로드 |
| Test (1000+) | 에디터 전용 | — |

에디터에서 중간 씬을 바로 실행할 경우, `ColdStartup.cs`(`#if UNITY_EDITOR`)가 누락된 필수 씬을 자동으로 로드한다.

---

## Transition — 전환 연출

Strategy 패턴으로 전환 효과를 교체할 수 있게 설계했다.

- `FadeTransitionEffect` — Alpha 페이드로 전환
- `InstantTransitionEffect` — 즉시 전환

`Begin() / End()`는 Reference Count 기반으로 중첩 호출에 안전하다. `GamePlaySystem`은 `Scope()` 패턴(`await using`)으로 Begin/End 쌍을 자동 보장한다.

---

## 플레이어 시스템

Cinemachine과 Input System을 함께 쓰면 입력·카메라·이동·애니메이션이 얽히면서 컴포넌트 하나에 책임이 빠르게 몰린다. 이걸 세 컴포넌트 파이프라인으로 분리해 각자의 책임을 명확히 했다.

Cinemachine과 동일한 `IInputAxisOwner` 인터페이스를 채택해 카메라-플레이어 입력 구조를 일관되게 맞췄다. 입력 소스를 교체해도 이동·애니메이션 코드는 건드릴 필요가 없다.

- `PlayerControllerBase` — `IInputAxisOwner` 기반 입력 계약 정의. 입력 소스 교체 가능
- `PlayerController` — `CharacterController` 기반 이동. 카메라 공간 입력을 월드 공간으로 변환
- `PlayerAnimator` — 트랜스폼 델타를 읽어 애니메이터 파라미터 구동. 컨트롤러와 완전히 독립

`CameraRelativeInputFrameResolver`는 카메라가 플레이어 머리 위를 넘을 때 입력 방향이 반전되는 문제를 블렌딩으로 처리한다.

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
                                └─ SceneDirector가 GamePlay.unity 자동 로드
                                     └─ GamePlaySystem.LaunchSession() → Exploration/World0
```

---

## 기술 스택

- **Unity** URP 17.3.0
- **UniTask** — async/await 기반 씬 로딩·전환 시퀀스
- **Cinemachine** 3.1.6
- **Input System** 1.18.0
- **C#**

---

## 빌드 & 테스트

빌드: Unity Editor → File > Build Settings

테스트: Unity Editor → Window > General > Test Runner
- Runtime 테스트: `Assets/NexusFrameAssets/Tests/Runtime`
  - `SceneFlowTests` — Splash→World0 전체 플로우 및 ColdStartup 시나리오 검증

---

## 진행 상황

- [x] Preload 씬 기반 Singleton 관리
- [x] SceneDirector 씬 전환 시스템
- [x] Transition (Fade / Instant)
- [x] GamePlaySystem — Session Stack (Replace / Push / Pop / PushOrReplace)
- [x] Session 생명주기
- [x] Stage 타입 (Level / AdditiveLevel / SubLevel / PrefabInstance)
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
