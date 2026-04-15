using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace NexusFrame
{
    public class GamePlaySystem: MonoPreload<GamePlaySystem>
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize() => ResetInstance();

        public PlaySessionBase CurrentSession => _sessionStack.Count > 0 ? _sessionStack.Peek() : null;
        public IReadOnlyCollection<PlaySessionBase> SessionStack => _sessionStack;

        [field: SerializeField]
        public Transform PrefabInstanceZoneRoot {get; private set;}

        public long SessionStackUpdateTick {get; private set;}

        public Action OnSessionStackUpdatedForEditor { get; private set; } = null; // Editor 에서만 사용

        public void SetSessionStackUpdateCallback(Action callback)
        {
#if UNITY_EDITOR
            OnSessionStackUpdatedForEditor = callback;
#endif
        }

        private Stack<PlaySessionBase> _sessionStack = new();

        // ── Static Entry Points ───────────────────────────────────────────

        public static void LaunchSession(PlaySessionType sessionType, PlaySessionSwitch switchType, GameStageDesc stageDesc, TransitionEffectType transitionEffectType)
            => LaunchSessionStatic(sessionType, switchType, stageDesc, transitionEffectType).Forget();

        public static void PopSession(TransitionEffectType transitionEffectType)
            => PopSessionStatic(transitionEffectType).Forget();

        public static void LaunchSession(PlaySessionSwitch switchType, TransitionEffectType transitionEffectType)
        {
            Debug.Assert(switchType == PlaySessionSwitch.Pop, "This overload is for Pop only.");
            Debug.Assert(HasInstance && 2 <= Instance._sessionStack.Count);
            PopSessionStatic(transitionEffectType).Forget();
        }

        public static UniTask LaunchSessionAtColdStartup(PlaySessionType sessionType, string sceneName)
        {
            var stageDesc = new GameStageDesc(GameStageType.Level, sceneName, false);
            return Instance.LaunchNewSessionCore(
                sessionType,
                PlaySessionSwitch.Replace,
                Instance.GetStage(stageDesc),
                TransitionEffectType.Fade);
        }

        private static async UniTask LaunchSessionStatic(PlaySessionType sessionType, PlaySessionSwitch switchType, GameStageDesc stageDesc, TransitionEffectType transitionEffectType)
        {
            await SceneDirector.EnsureGamePlayReady();
            if (switchType == PlaySessionSwitch.Pop)
                await Instance.PopSessionCore(transitionEffectType);
            else
                await Instance.LaunchNewSessionCore(sessionType, switchType, Instance.GetStage(stageDesc), transitionEffectType);
        }

        private static async UniTask PopSessionStatic(TransitionEffectType transitionEffectType)
        {
            Debug.Assert(HasInstance);
            await Instance.PopSessionCore(transitionEffectType);
        }

        private async UniTask LaunchNewSessionCore(PlaySessionType sessionType, PlaySessionSwitch switchType, GameStageBase stage, TransitionEffectType transitionEffectType)
        {
            Debug.Assert(switchType != PlaySessionSwitch.None);
            Debug.Assert(switchType != PlaySessionSwitch.Pop);

            PlaySessionBase session = GetSession(sessionType);
            GameStageBase backgroundStage = 0 < _sessionStack.Count ? _sessionStack.Peek().Stage : null;
            session.SetStage(stage, backgroundStage);

            switchType = CheckSwitch(switchType);
            switch (switchType)
            {
                case PlaySessionSwitch.Replace:
                    await ReplaceSessionCore(session, transitionEffectType); break;
                case PlaySessionSwitch.Push:
                    await StackSessionCore(session, transitionEffectType); break;
                default:
                    Debug.LogError($"Unexpected switch type: {switchType}");
                    break;
            }
        }

        private async UniTask PopSessionCore(TransitionEffectType transitionEffectType)
        {
            PlaySessionBase prevSession = _sessionStack.Peek();
            PlaySessionBase nextSession = null;

            await prevSession.EnterSessionOut();

            await using (await UiSystem.ScopeTransition(transitionEffectType))
            {
                await RemovePrevSessions(removeAll: false);
                await Resources.UnloadUnusedAssets();
                nextSession = _sessionStack.Peek();
                await nextSession.EnterResumed();
            }

            await nextSession.EnterSessionIn();
        }

        private async UniTask ReplaceSessionCore(PlaySessionBase nextSession, TransitionEffectType transitionEffectType)
        {
            if (_sessionStack.TryPeek(out var prevSession))
            {
                await prevSession.EnterSessionOut();
                await using(await UiSystem.ScopeTransition(transitionEffectType))
                {
                    // Level(Main Level)은 항상 하나만 존재한다.
                    // 다른 세션들이 현재 Main Level에 의존할 가능성이 높으므로, Level 전환 시 스택 전체를 제거한다.
                    var removeAllPrevSessions = nextSession.Stage.StageType == GameStageType.Level;
                    await RemovePrevSessions(removeAllPrevSessions);
                    await Resources.UnloadUnusedAssets();
                    await PlayNewSession(nextSession);
                }
                await nextSession.EnterSessionIn();
            }
            else
            {
                await using(await UiSystem.ScopeTransition(transitionEffectType))
                {
                    await PlayNewSession(nextSession);
                }
                await nextSession.EnterSessionIn();
            }

        }

        private async UniTask StackSessionCore(PlaySessionBase nextSession, TransitionEffectType transitionEffectType)
        {
            Debug.Assert(0 < _sessionStack.Count); // stack count == 0 이면 ReplaceSessionCore 에서 처리

            var prevSession = _sessionStack.Peek();

            await prevSession.EnterSessionOut();

            await using (await UiSystem.ScopeTransition(transitionEffectType))
            {
                if (nextSession.Stage.DoOverrideStage)
                {
                    await prevSession.EnterPaused();
                }
                else
                {
                    await prevSession.EnterSlept();
                }
                await PlayNewSession(nextSession);
            }

            await nextSession.EnterSessionIn();
        }

        private async UniTask PlayNewSession(PlaySessionBase session)
        {
            await session.EnterCreated();
            _sessionStack.Push(session);
            NotifySessionStackUpdate();
            await session.Stage.Load();
            await session.EnterPlayed();
        }

        private async UniTask RemovePrevSessions(bool removeAll)
        {
            if (_sessionStack.Count == 0)
            {
                return;
            }

            var unloadCount = removeAll ? _sessionStack.Count : 1;
            for (var i = 0; i < unloadCount; ++i)
            {
                PlaySessionBase session = _sessionStack.Peek();
                await session.EnterStopped();
                if (session.Stage != null)
                {
                    await session.Stage.Unload();
                }
                _ = _sessionStack.Pop();
                NotifySessionStackUpdate();
                await session.EnterDestroyed();
            }
        }

        private PlaySessionSwitch CheckSwitch(PlaySessionSwitch switchType)
        {
            if (_sessionStack.Count == 0 && switchType != PlaySessionSwitch.Replace)
            {
                return PlaySessionSwitch.Replace;
            }
            if (switchType == PlaySessionSwitch.PushOrReplace)
            {
                return _sessionStack.Count switch
                {
                    0 => PlaySessionSwitch.Replace,
                    1 => PlaySessionSwitch.Push,
                    >= 2 => PlaySessionSwitch.Replace,
                    _ => throw new InvalidOperationException($"Invalid stack count: {_sessionStack.Count}"),
                };
            }
            return switchType;
        }

        private void NotifySessionStackUpdate()
        {
            SessionStackUpdateTick = DateTime.Now.Ticks;
#if UNITY_EDITOR
            OnSessionStackUpdatedForEditor?.Invoke();
#endif
        }

        private GameStageBase GetStage(GameStageDesc desc)
        {
            // 차후에 pool 로 전환 검토
            return desc.StageType switch
            {
                GameStageType.Level => new LevelStage(desc.StageType, desc.AssetPath, desc.DoOverrideStage),
                GameStageType.AdditiveLevel => new LevelStage(desc.StageType, desc.AssetPath, desc.DoOverrideStage),
                GameStageType.SubLevel => FindSubLevelStage(desc.AssetPath),
                GameStageType.PrefabInstance => new PrefabInstanceStage(desc.AssetPath, desc.DoOverrideStage),
                GameStageType.GoPrevLevel => null,
                GameStageType.None => null,
                _ => null
            };
        }

        private PlaySessionBase GetSession(PlaySessionType sessionType)
        {
            // [TODO] Convert to Pool
            return sessionType switch
            {
                PlaySessionType.Exploration => new ExplorationSession(),
                PlaySessionType.Battle => new BattleSession(),
                PlaySessionType.Narrative => new NarrativeSession(),
                _ => null
            };
        }

        private SubLevelStage FindSubLevelStage(string stageUri)
        {
            foreach (var session in _sessionStack)
            {
                var found = session.Stage?.SubStages?
                    .OfType<SubLevelStage>()
                    .FirstOrDefault(s => s.Id == stageUri);
                if (found != null) return found;
            }
            return null;
        }

        internal void RegisterAnchorOnce(GameStageAnchorBase anchor)
        {
            Debug.Assert(CurrentSession != null, "RegisterAnchorOnce called with no active session.");
            CurrentSession.Stage.RegisterAnchor(anchor);
        }
    }
}
