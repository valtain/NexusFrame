using Cysharp.Threading.Tasks;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NexusFrame
{
    /// <summary>
    /// 전투 모드 HUD 뷰.
    /// <para>
    /// <c>Resources/Ui/BattleHud</c> 프리팹에서 동적으로 생성된다.
    /// Show/Hide 시 PrimeTween Sequence 로 연출을 재생한다.
    /// </para>
    /// <remarks>
    /// Show (0.50s): SlidePanel 오른쪽에서 슬라이드 인 + AccentLine ScaleX → SessionLabel 페이드 → EnemyName 페이드 → ButtonGroup 페이드<br/>
    /// Hide (0.47s): 텍스트·버튼 페이드 아웃 → AccentLine 수축 → SlidePanel 슬라이드 아웃
    /// </remarks>
    /// </summary>
    [UiViewPath("Ui/BattleHud")]
    public class BattleHud : UiViewBase
    {
        private const float SlideDuration      = 0.30f;
        private const float AccentLineDuration = 0.15f;
        private const float TextFadeDuration   = 0.18f;
        private const float SessionLabelDelay  = 0.18f;
        private const float EnemyNameDelay     = 0.24f;
        private const float ButtonGroupDelay   = 0.32f;

        private const float HideTextDuration   = 0.12f;
        private const float HideAccentDuration = 0.10f;
        private const float HideSlideDuration  = 0.25f;

        /// <summary>슬라이드 애니메이션 대상 패널.</summary>
        [field: SerializeField] public RectTransform SlidePanel        { get; private set; }

        /// <summary>하단 가로 강조 선. Show 시 왼쪽에서 오른쪽으로 그려진다.</summary>
        [field: SerializeField] public RectTransform AccentLine        { get; private set; }

        /// <summary>SessionLabel 의 알파 애니메이션용 CanvasGroup.</summary>
        [field: SerializeField] public CanvasGroup   SessionLabelGroup { get; private set; }

        /// <summary>EnemyName 의 알파 애니메이션용 CanvasGroup.</summary>
        [field: SerializeField] public CanvasGroup   EnemyNameGroup    { get; private set; }

        /// <summary>전투 결과 버튼 그룹의 알파 애니메이션용 CanvasGroup.</summary>
        [field: SerializeField] public CanvasGroup   ButtonGroup       { get; private set; }

        /// <summary>"BATTLE" 모드 레이블 텍스트.</summary>
        [field: SerializeField] public TMP_Text      SessionLabelText  { get; private set; }

        /// <summary>적/스테이지 이름을 표시하는 텍스트.</summary>
        [field: SerializeField] public TMP_Text      EnemyNameText     { get; private set; }

        [field: SerializeField] public Button VictoryButton { get; private set; }
        [field: SerializeField] public Button DefeatButton  { get; private set; }
        [field: SerializeField] public Button EscapeButton  { get; private set; }

        private Vector2 _onScreenPos;

        private void Awake()
        {
            Debug.Assert(SlidePanel        != null, "[BattleHud] SlidePanel이 할당되지 않았습니다.");
            Debug.Assert(AccentLine        != null, "[BattleHud] AccentLine이 할당되지 않았습니다.");
            Debug.Assert(SessionLabelGroup != null, "[BattleHud] SessionLabelGroup이 할당되지 않았습니다.");
            Debug.Assert(EnemyNameGroup    != null, "[BattleHud] EnemyNameGroup이 할당되지 않았습니다.");
            Debug.Assert(ButtonGroup       != null, "[BattleHud] ButtonGroup이 할당되지 않았습니다.");
            Debug.Assert(VictoryButton     != null, "[BattleHud] VictoryButton이 할당되지 않았습니다.");
            Debug.Assert(DefeatButton      != null, "[BattleHud] DefeatButton이 할당되지 않았습니다.");
            Debug.Assert(EscapeButton      != null, "[BattleHud] EscapeButton이 할당되지 않았습니다.");

            _onScreenPos = SlidePanel.anchoredPosition;
        }

        // ── Data Binding ──────────────────────────────────────────────────────

        /// <summary>HUD 에 표시할 적/스테이지 이름을 설정한다.</summary>
        public void SetEnemyName(string enemyName)
        {
            if (EnemyNameText != null)
                EnemyNameText.text = enemyName;
        }

        // ── UiViewBase ────────────────────────────────────────────────────────

        protected override async UniTask OnShow()
        {
            var off = OffScreenRight();
            SlidePanel.anchoredPosition = off;
            AccentLine.localScale       = new Vector3(0f, 1f, 1f);
            SessionLabelGroup.alpha     = 0f;
            EnemyNameGroup.alpha        = 0f;
            ButtonGroup.alpha           = 0f;

            await Sequence.Create(useUnscaledTime: true)
                .Group(Tween.UIAnchoredPosition(SlidePanel, off, _onScreenPos,
                       SlideDuration, Ease.OutExpo, useUnscaledTime: true))
                .Group(Tween.ScaleX(AccentLine, 0f, 1f,
                       AccentLineDuration, Ease.OutCubic, useUnscaledTime: true))
                .Group(Tween.Alpha(SessionLabelGroup, 0f, 1f,
                       TextFadeDuration, Ease.OutQuad,
                       startDelay: SessionLabelDelay, useUnscaledTime: true))
                .Group(Tween.Alpha(EnemyNameGroup, 0f, 1f,
                       TextFadeDuration, Ease.OutQuad,
                       startDelay: EnemyNameDelay, useUnscaledTime: true))
                .Group(Tween.Alpha(ButtonGroup, 0f, 1f,
                       TextFadeDuration, Ease.OutQuad,
                       startDelay: ButtonGroupDelay, useUnscaledTime: true));
        }

        protected override async UniTask OnHide()
        {
            await Sequence.Create(useUnscaledTime: true)
                .Group(Tween.Alpha(SessionLabelGroup, 1f, 0f,
                       HideTextDuration, Ease.InQuad, useUnscaledTime: true))
                .Group(Tween.Alpha(EnemyNameGroup, 1f, 0f,
                       HideTextDuration, Ease.InQuad, useUnscaledTime: true))
                .Group(Tween.Alpha(ButtonGroup, 1f, 0f,
                       HideTextDuration, Ease.InQuad, useUnscaledTime: true))
                .Chain(Tween.ScaleX(AccentLine, 1f, 0f,
                       HideAccentDuration, Ease.InCubic, useUnscaledTime: true))
                .Chain(Tween.UIAnchoredPosition(SlidePanel, _onScreenPos, OffScreenRight(),
                       HideSlideDuration, Ease.InCubic, useUnscaledTime: true));
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private Vector2 OffScreenRight()
            => new Vector2(_onScreenPos.x + Screen.width, _onScreenPos.y);
    }
}
