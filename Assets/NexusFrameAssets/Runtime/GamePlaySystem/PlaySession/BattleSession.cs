using Cysharp.Threading.Tasks;

namespace NexusFrame
{
    public class BattleSession : PlaySessionBase
    {
        private BattleHud _hud;

        public BattleSession() : base(PlaySessionType.Battle) { }

        protected override async UniTask OnEnterSessionInCore()
        {
            _hud = await UiSystem.Instance.MainView.ShowView<BattleHud>();
            if (Stage != null)
                _hud.SetEnemyName(Stage.StageName);
            _hud.OnVictoryClicked += HandleVictory;
            _hud.OnDefeatClicked  += HandleDefeat;
            _hud.OnEscapeClicked  += HandleEscape;
        }

        protected override async UniTask OnEnterSessionOutCore()
        {
            _hud.OnVictoryClicked -= HandleVictory;
            _hud.OnDefeatClicked  -= HandleDefeat;
            _hud.OnEscapeClicked  -= HandleEscape;
            await UiSystem.Instance.MainView.HideView(_hud);
            _hud = null;
        }

        private void HandleVictory() { /* [TODO] */ }
        private void HandleDefeat()  { /* [TODO] */ }
        private void HandleEscape()  { /* [TODO] */ }
    }
}
