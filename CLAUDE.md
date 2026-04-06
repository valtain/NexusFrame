# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Test

- **Build**: Unity Editor → File > Build Settings
  - No CLI build scripts exist
- **Tests**: Unity Editor → Window > General > Test Runner
  - Editor tests: `Assets/NexusFrameAssets/Tests/Editor`
  - Runtime tests: `Assets/NexusFrameAssets/Tests/Runtime`

## Folder Convention

- `Assets/NexusFrameAssets/` — main framework package, follows UPM package layout
  - Internal layout: `Runtime/`, `Editor/`, `Tests/`, `.asmdef`
- Additional independent packages live at the same level as `NexusFrameAssets/`
  - Naming: `{PackageName}Assets/` with its own UPM layout

## Key Dependencies

- **UniTask** — async/await; used for scene loading, transitions, UI sequences
- **Cinemachine 3.1.6** — camera system; player input integrates via `IInputAxisOwner`
- **Input System 1.18.0** — new Unity input system
- **URP 17.3.0** — render pipeline

## Architecture

### Singleton: `MonoPreload<T>`

- Base class for all persistent singletons (`Assets/NexusFrameAssets/Scripts/Utils/MonoPreload.cs`)
- Instantiated by the **Preload** scene; persist for the entire application lifetime
- Core singletons: `SceneDirector`, `TransitionUi`

### Scene Management: `SceneDirector`

`Assets/NexusFrameAssets/Scripts/Scene/SceneDirector.cs`

**Scene types** (by `SceneType` enum value):

| Value | Type | Role |
|-------|------|------|
| 0 | `Preload` | Loads all singletons; always resident |
| 1 | `Splash` | Splash screen |
| 2 | `Title` | Title screen |
| 3 | `MainMenu` | Main menu |
| 99 | `GamePlay` | Gameplay environment; co-loaded with Level scenes |
| 100+ | `Level` | Gameplay levels |
| 1000+ | `Test` | Editor-only test scenes |

**Loading rules:**
- *Content scenes* (Splash, Title, MainMenu, Level) — unloaded before loading the next one
- *Prerequisite scenes* (Preload, GamePlay) — remain loaded; loaded only once

**Key points:**
- Single entry point: `SceneDirector.LoadScene(sceneName)` — all scene transitions go through here
  - Automatically wraps loading with `TransitionUi.Begin()` / `TransitionUi.End()`
  - Level scenes automatically trigger loading of Preload + GamePlay prerequisites
- `ColdStartup.cs` — editor-only; auto-loads missing prerequisites so any scene can run standalone

### UI Transition System: `TransitionUi`

`Assets/NexusFrameAssets/Scripts/Ui/TransitionUi/TransitionUi.cs`

- Strategy pattern via `ITransitionEffect`
  - `FadeTransitionEffect` — animates canvas alpha 0.2 → 1.0 over 0.5s
  - `InstantTransitionEffect` — immediately shows full black overlay
- `Begin()` / `End()` are reference-counted — safe for nested calls

### Player System

`Assets/NexusFrameAssets/Scripts/Player/`

Three decoupled components in a pipeline:

1. **`PlayerControllerBase`** — input contract
   - Implements `IInputAxisOwner` (MoveX, MoveZ, Jump, Sprint axes)
   - Exposes `Landed` UnityEvent
2. **`PlayerController`** — movement
   - `CharacterController`-based movement
   - Transforms camera-space input to world space via `CameraRelativeInputFrameResolver`
   - Slerp/Lerp damping for smooth transitions
3. **`PlayerAnimator`** — animation (decoupled from controller)
   - Reads transform position delta each frame
   - Drives `Speed` and `Direction` Animator parameters

### Camera-Relative Input: `CameraRelativeInputFrameResolver`

`Assets/NexusFrameAssets/Scripts/Player/CameraRelativeInputFrameResolver.cs`

- Detects when camera crosses above/below the player's "up" vector (hemisphere boundary)
- Blends between top/bottom hemisphere input frames over `_blendTime` (default 2s)
- Prevents jittery movement when camera sweeps overhead

## Coding Conventions

See [.claude/coding-conventions.md](.claude/coding-conventions.md).

## Scene Load Flow

```
App Start
  └─ Splash scene entry → Startup.cs
       └─ SceneDirector.EnsurePreloadReady() → loads Preload.unity (singletons)
            └─ SplashController → fade sequence
                 └─ Title → TitleController → any key press
                      └─ MainMenu → MainMenuController → New Game
                           └─ World0 (Level)
                                └─ SceneDirector auto-loads Preload + GamePlay
```
