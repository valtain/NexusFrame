using Cysharp.Threading.Tasks;

namespace NexusFrame
{
    public class ExplorationSession : PlaySessionBase
    {
        public ExplorationSession() : base(PlaySessionType.Exploration)
        {
        }

        protected override UniTask OnEnterSessionInCore()
        {
            // [TODO]
            return UniTask.CompletedTask;
        }
        protected override UniTask OnEnterSessionOutCore()
        {
            // [TODO]
            return UniTask.CompletedTask;
        }
    }
}
