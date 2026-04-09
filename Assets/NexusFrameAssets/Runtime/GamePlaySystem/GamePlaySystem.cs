using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace NexusFrame
{
    public class GamePlaySystem: MonoPreload<GamePlaySystem>
    {
        public PlaySessionBase CurrentSession => _sessionStack.Count > 0 ? _sessionStack.Peek() : null;
        public IReadOnlyCollection<PlaySessionBase> SessionStack => _sessionStack;

        [field: SerializeField]
        public Transform PrefabInstanceZoneRoot {get; private set;}

        public long SessionStackUpdateTick {get; private set;}

        public Action OnSessionStackUpdatedForEditor { get; private set; } = null; // Editor 에서만 사용
        public void SetSessionStackUpdateCallback(Action callback) => OnSessionStackUpdatedForEditor = callback;

        private Stack<PlaySessionBase> _sessionStack = new();

        public void LaunchSession(PlaySessionType sessionType, PlaySessionSwitch switchType, GameStageDesc stageDesc, TransitionEffectType transitionEffectType)
            => LaunchSessionCore(sessionType, switchType, GetStage(stageDesc), transitionEffectType).Forget();

        public UniTask LaunchSessionAtColdStartup(string sceneName)
        {
            var stageDesc = new GameStageDesc(GameStageType.Level, sceneName, false);
            return LaunchSessionCore(
                PlaySessionType.Exploration,
                PlaySessionSwitch.Replace,
                GetStage(stageDesc),
                TransitionEffectType.Fade);
        }

        private async UniTask LaunchSessionCore(PlaySessionType sessionType, PlaySessionSwitch switchType, GameStageBase stage, TransitionEffectType transitionEffectType)
        {
            Debug.Assert(switchType != PlaySessionSwitch.None);

            if (switchType == PlaySessionSwitch.Pop)
            {
                Debug.Assert(2 <= _sessionStack.Count);
                await PopSessionCore(transitionEffectType);
                return;
            }

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
            {
                await TransitionUi.Instance.Begin(transitionEffectType);
                await RemovePrevSessions(removeAll: false);
                await Resources.UnloadUnusedAssets();
                nextSession = _sessionStack.Peek();

                await nextSession.EnterResumed();
                await TransitionUi.Instance.End();
            }
            await nextSession.EnterSessionIn();
        }

        private async UniTask ReplaceSessionCore(PlaySessionBase nextSession, TransitionEffectType transitionEffectType)
        {
            if (_sessionStack.TryPeek(out var prevSession))
            {
                await prevSession.EnterSessionOut();
                await TransitionUi.Instance.Begin(transitionEffectType);

                var removeAllPrevSessions = nextSession.Stage.StageType == GameStageType.Level; // 기본 Level 은 하나만...
                await RemovePrevSessions(removeAllPrevSessions);
                await Resources.UnloadUnusedAssets();
            }
            else
            {
                await TransitionUi.Instance.Begin(transitionEffectType);
            }

            await PlayNewSession(nextSession);
            await TransitionUi.Instance.End();
            await nextSession.EnterSessionIn();
        }

        private async UniTask StackSessionCore(PlaySessionBase nextSession, TransitionEffectType transitionEffectType)
        {
            Debug.Assert(0 < _sessionStack.Count); // stack count == 0 이면 ReplaceSessionCore 에서 처리

            var prevSession = _sessionStack.Peek();

            await prevSession.EnterSessionOut();

            await TransitionUi.Instance.Begin(transitionEffectType);
            if (nextSession.Stage.DoOverrideStage)
            {
                await prevSession.EnterPaused();
            }
            else
            {
                await prevSession.EnterSlept();
            }
            await PlayNewSession(nextSession);
            await TransitionUi.Instance.End();

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
                if (!(session.Stage?.SubStages is { } subStages))
                {
                    continue;
                }
                foreach (var subStage in subStages)
                {
                    if (subStage is SubLevelStage subLevelStage &&
                        subLevelStage.Id == stageUri)
                    {
                        return subLevelStage;
                    }
                }
            }
            return null;
        }

        internal void RegisterAnchorOnce(GameStageAnchorBase anchor) => CurrentSession.Stage.RegisterAnchor(anchor);
    }
}
