using Cysharp.Threading.Tasks;

namespace NexusFrame
{
    public class NarrativeSession : PlaySessionBase
    {
        public NarrativeSession() : base(PlaySessionType.Narrative)
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
