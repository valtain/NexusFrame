# Coding Conventions

Follows [Unity C# coding standards](https://unity.com/how-to/naming-and-code-style-tips-c-scripting-unity). Rules below take precedence where they differ.

## Naming

- Private fields: `_camelCase` (underscore prefix, no `m_`)
- Public inspector-exposed property: `[field: SerializeField]` with `public T Foo { get; private set; } = default;`
- Interfaces: `I` prefix (`ITransitionEffect`, `IInputAxisOwner`)
- Async methods that return `UniTask`: descriptive name without mandatory `Async` suffix (e.g., `LoadSceneInternal`, `EnsurePrerequisitesLoaded`)

## Async

- Use **UniTask** exclusively — no coroutines (`IEnumerator` / `StartCoroutine`).
- Fire-and-forget: call `.Forget()` on the returned `UniTask`.
- Cancellation: `this.GetCancellationTokenOnDestroy()` tied to the MonoBehaviour lifetime.
- Timing delays: `UniTask.Delay()` instead of `WaitForSeconds`.

## Unity-Specific

- **No public fields** on MonoBehaviours — use `[SerializeField]` private fields or `[field: SerializeField]` properties.
- Cache `this.transform` into a local field (`_cachedTransform`) to avoid repeated property access.
- Use `Debug.Assert()` for defensive invariant checks; do not use `throw` for game-logic assertions.
- Editor-only code must be guarded with `#if UNITY_EDITOR`.
- Scripts that must initialize early use `[DefaultExecutionOrder(-1000)]`.

## Documentation

- XML doc comments (`/// <summary>`) in **Korean** on all public and protected members.
- Use `<see cref=""/>` for cross-references to related types/members.
