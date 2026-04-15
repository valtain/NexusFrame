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
            _hud.VictoryButton.onClick.AddListener(HandleVictory);
            _hud.DefeatButton.onClick.AddListener(HandleDefeat);
            _hud.EscapeButton.onClick.AddListener(HandleEscape);
        }

        protected override async UniTask OnEnterSessionOutCore()
        {
            _hud.VictoryButton.onClick.RemoveListener(HandleVictory);
            _hud.DefeatButton.onClick.RemoveListener(HandleDefeat);
            _hud.EscapeButton.onClick.RemoveListener(HandleEscape);
            await UiSystem.Instance.MainView.HideView(_hud);
            _hud = null;
        }

        private void HandleVictory() =>
            GamePlaySystem.PopSession(TransitionEffectType.Fade); // 테스트 구현
        private void HandleDefeat()  =>
            GamePlaySystem.PopSession(TransitionEffectType.Fade); // 테스트 구현
        private void HandleEscape()  =>
            GamePlaySystem.PopSession(TransitionEffectType.Fade); // 테스트 구현
    }
}
