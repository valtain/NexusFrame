using Cysharp.Threading.Tasks;

namespace NexusFrame
{
    public class ExplorationSession : PlaySessionBase
    {
        private ExplorationHud _hud;

        public ExplorationSession() : base(PlaySessionType.Exploration) { }

        protected override async UniTask OnEnterSessionInCore()
        {
            _hud = await UiSystem.Instance.MainView.ShowView<ExplorationHud>();
            if (Stage != null)
                _hud.SetStageName(Stage.StageName);
        }

        protected override async UniTask OnEnterSessionOutCore()
        {
            await UiSystem.Instance.MainView.HideView(_hud);
            _hud = null;
        }
    }
}
