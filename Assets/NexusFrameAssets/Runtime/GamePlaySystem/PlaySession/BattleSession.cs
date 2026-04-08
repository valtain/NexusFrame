using Cysharp.Threading.Tasks;

namespace NexusFrame
{
    public class BattleSession : PlaySessionBase
    {
        public BattleSession() : base(PlaySessionType.Battle)
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
