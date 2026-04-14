using Cysharp.Threading.Tasks;
using PrimeTween;
using TMPro;
using UnityEngine;

namespace NexusFrame
{
    /// <summary>
    /// 탐험 모드 HUD 뷰.
    /// <para>
    /// <c>Resources/Ui/ExplorationHud</c> 프리팹에서 동적으로 생성된다.
    /// Show/Hide 시 PrimeTween Sequence 로 P5 스타일 연출을 재생한다.
    /// </para>
    /// <remarks>
    /// Show (0.46s): SlidePanel 슬라이드 인 + AccentLine 드로우 → ModeLabel 페이드 → StageName 페이드<br/>
    /// Hide (0.55s): 텍스트 페이드 아웃 → AccentLine 수축 → SlidePanel 슬라이드 아웃
    /// </remarks>
    /// </summary>
    [UiViewPath("Ui/ExplorationHud")]
    public class ExplorationHud : UiViewBase
    {

        private const float SlideDuration      = 0.35f;
        private const float AccentLineDuration = 0.20f;
        private const float TextFadeDuration   = 0.18f;
        private const float ModeLabelDelay     = 0.22f;
        private const float StageNameDelay     = 0.28f;

        private const float HideTextDuration    = 0.15f;
        private const float HideAccentDuration  = 0.10f;
        private const float HideSlideDuration   = 0.30f;

        /// <summary>슬라이드 애니메이션 대상 패널.</summary>
        [field: SerializeField] public RectTransform SlidePanel      { get; private set; }

        /// <summary>좌측 세로 강조 선. Show 시 위에서 아래로 그려진다.</summary>
        [field: SerializeField] public RectTransform AccentLine      { get; private set; }

        /// <summary>SessionLabel 의 알파 애니메이션용 CanvasGroup.</summary>
        [field: SerializeField] public CanvasGroup   SessionLabelGroup  { get; private set; }

        /// <summary>StageName 의 알파 애니메이션용 CanvasGroup.</summary>
        [field: SerializeField] public CanvasGroup   StageNameGroup  { get; private set; }

        /// <summary>스테이지 이름을 표시하는 텍스트.</summary>
        [field: SerializeField] public TMP_Text      StageNameText   { get; private set; }

        /// <summary>"EXPLORATION" 등 모드 레이블 텍스트.</summary>
        [field: SerializeField] public TMP_Text      SessionLabelText { get; private set; }

        private Vector2 _onScreenPos;

        private void Awake()
        {
            Debug.Assert(SlidePanel     != null, "[ExplorationHud] SlidePanel이 할당되지 않았습니다.");
            Debug.Assert(AccentLine     != null, "[ExplorationHud] AccentLine이 할당되지 않았습니다.");
            Debug.Assert(SessionLabelGroup != null, "[ExplorationHud] ModeLabelGroup이 할당되지 않았습니다.");
            Debug.Assert(StageNameGroup != null, "[ExplorationHud] StageNameGroup이 할당되지 않았습니다.");
            _onScreenPos = SlidePanel.anchoredPosition;
        }

        // ── Data Binding ──────────────────────────────────────────────────────

        /// <summary>HUD 에 표시할 스테이지 이름을 설정한다.</summary>
        public void SetStageName(string stageName)
        {
            if (StageNameText != null)
                StageNameText.text = stageName;
        }

        // ── UiViewBase ────────────────────────────────────────────────────────

        /// <summary>
        /// SlidePanel 슬라이드 인 + AccentLine 드로우가 동시에 시작되고,
        /// 이어서 ModeLabel → StageName 순으로 페이드 인한다.
        /// </summary>
        protected override async UniTask OnShow()
        {
            var off = OffScreenLeft();
            SlidePanel.anchoredPosition    = off;
            AccentLine.localScale          = new Vector3(1f, 0f, 1f);
            SessionLabelGroup.alpha        = 0f;
            StageNameGroup.alpha           = 0f;

            await Sequence.Create(useUnscaledTime: true)
                .Group(Tween.UIAnchoredPosition(SlidePanel, off, _onScreenPos,
                       SlideDuration, Ease.OutBack, useUnscaledTime: true))
                .Group(Tween.ScaleY(AccentLine, 0f, 1f,
                       AccentLineDuration, Ease.OutCubic, useUnscaledTime: true))
                .Group(Tween.Alpha(SessionLabelGroup, 0f, 1f,
                       TextFadeDuration, Ease.OutQuad,
                       startDelay: ModeLabelDelay, useUnscaledTime: true))
                .Group(Tween.Alpha(StageNameGroup, 0f, 1f,
                       TextFadeDuration, Ease.OutQuad,
                       startDelay: StageNameDelay, useUnscaledTime: true));
        }

        /// <summary>
        /// ModeLabel/StageName 페이드 아웃 → AccentLine 수축 → SlidePanel 슬라이드 아웃 순으로 재생된다.
        /// </summary>
        protected override async UniTask OnHide()
        {
            await Sequence.Create(useUnscaledTime: true)
                .Group(Tween.Alpha(SessionLabelGroup, 1f, 0f,
                       HideTextDuration, Ease.InQuad, useUnscaledTime: true))
                .Group(Tween.Alpha(StageNameGroup, 1f, 0f,
                       HideTextDuration, Ease.InQuad, useUnscaledTime: true))
                .Chain(Tween.ScaleY(AccentLine, 1f, 0f,
                       HideAccentDuration, Ease.InCubic, useUnscaledTime: true))
                .Chain(Tween.UIAnchoredPosition(SlidePanel, _onScreenPos, OffScreenLeft(),
                       HideSlideDuration, Ease.InCubic, useUnscaledTime: true));
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private Vector2 OffScreenLeft()
            => new Vector2(_onScreenPos.x - Screen.width, _onScreenPos.y);
    }
}
