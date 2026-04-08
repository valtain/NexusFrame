using Cysharp.Threading.Tasks;
using UnityEngine;

namespace NexusFrame
{
    public enum PlaySessionType
    {
        None = -1,
        Exploration,
        Battle,
        Narrative
    }

    public enum PlaySessionSwitch
    {
        None = -1,

        Replace = 0,
        Push,
        Pop,

        PushOrReplace = 100 // Stack 1 이면 Push, 1 이상이면 Replace
    }

    public enum PlaySessionStatus
    {
        None = -1,
        Created,
        Played,
        SessionIn,
        SessionOut,
        Stopped,
        Paused,
        Slept,
        Resumed,
        Destroyed,
    }

    public abstract class PlaySessionBase
    {
        public PlaySessionType SessionType {get; protected set;} = PlaySessionType.None;
        public PlaySessionStatus Status {get; protected set;} = PlaySessionStatus.None;
        public GameStageBase Stage {get; protected set;} = default;
        public GameStageBase BackgroundStage {get; protected set;} = default;
        public PlaySessionBase(PlaySessionType type) => SessionType = type;

        public event System.Action<PlaySessionBase, PlaySessionStatus> OnStatusChanged
        {
            add { _onStatusChanged -= value; _onStatusChanged += value; } // 이중 추가 방지
            remove { _onStatusChanged -= value; }
        }

        public void SetStage(GameStageBase stage, GameStageBase backgroundStage)
        {
            Stage = stage;
            BackgroundStage = backgroundStage;
        }

        private System.Action<PlaySessionBase, PlaySessionStatus> _onStatusChanged;

        protected virtual UniTask OnEnterCreateCore() => UniTask.CompletedTask;
        protected virtual UniTask OnEnterPlayCore() => UniTask.CompletedTask;
        protected virtual UniTask OnEnterSessionInCore() => UniTask.CompletedTask;
        protected virtual UniTask OnEnterSessionOutCore() => UniTask.CompletedTask;
        protected virtual UniTask OnEnterStopCore() => UniTask.CompletedTask;
        protected virtual UniTask OnEnterPauseCore() => UniTask.CompletedTask;
        protected virtual UniTask OnEnterSleepCore() => UniTask.CompletedTask;
        protected virtual UniTask OnEnterResumeCore() => UniTask.CompletedTask;
        protected virtual UniTask OnEnterDestroyCore() => UniTask.CompletedTask;

        public async UniTask EnterCreated()   { SetStatus(PlaySessionStatus.Created);    await OnEnterCreateCore();     }
        public async UniTask EnterPlayed()    { SetStatus(PlaySessionStatus.Played);     await OnEnterPlayCore();       }
        public async UniTask EnterSessionIn() { SetStatus(PlaySessionStatus.SessionIn);  await OnEnterSessionInCore();  }
        public async UniTask EnterSessionOut(){ SetStatus(PlaySessionStatus.SessionOut); await OnEnterSessionOutCore(); }
        public async UniTask EnterStopped()   { SetStatus(PlaySessionStatus.Stopped);    await OnEnterStopCore();       }
        public async UniTask EnterPaused()    { SetStatus(PlaySessionStatus.Paused);     await OnEnterPauseCore();      }
        public async UniTask EnterSlept()     { SetStatus(PlaySessionStatus.Slept);      await OnEnterSleepCore();   Stage.OnSessionSlept();   }
        public async UniTask EnterResumed()   { SetStatus(PlaySessionStatus.Resumed);    await OnEnterResumeCore();  Stage.OnSessionResumed(); }
        public async UniTask EnterDestroyed() { SetStatus(PlaySessionStatus.Destroyed);  await OnEnterDestroyCore(); }

        private void SetStatus(PlaySessionStatus statusToBe)
        {
            Status = statusToBe;
            _onStatusChanged?.Invoke(this, Status);
        }
    }
}
