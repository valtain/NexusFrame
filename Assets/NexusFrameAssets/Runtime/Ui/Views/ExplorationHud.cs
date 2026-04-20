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
    /// Show/Hide 시 PrimeTween Sequence 로 P5 스타일 3패널 연출을 재생한다.
    /// </para>
    /// <remarks>
    /// Show: 좌상단(SlidePanel) + 우상단(DateTimePanel) + 하단(SpBarPanel) 동시 슬라이드 인 → 텍스트 페이드 스태거<br/>
    /// Hide: 텍스트 페이드 아웃 → 강조선 수축 → 전체 패널 슬라이드 아웃
    /// </remarks>
    /// </summary>
    [UiViewPath("Ui/ExplorationHud")]
    public class ExplorationHud : UiViewBase
    {
        // ── Show timings ──────────────────────────────────────────────────────
        private const float SlideDuration      = 0.35f;
        private const float AccentLineDuration = 0.20f;
        private const float TextFadeDuration   = 0.18f;
        private const float ModeLabelDelay     = 0.22f;
        private const float StageNameDelay     = 0.28f;
        private const float TimeDelay          = 0.28f;
        private const float DateDelay          = 0.34f;
        private const float SpBarFillDuration  = 0.28f;
        private const float SpFillDelay        = 0.20f;

        // ── Hide timings ──────────────────────────────────────────────────────
        private const float HideTextDuration   = 0.15f;
        private const float HideAccentDuration = 0.10f;
        private const float HideSlideDuration  = 0.30f;

        // ── Top-left panel ────────────────────────────────────────────────────
        /// <summary>슬라이드 애니메이션 대상 패널 (좌상단).</summary>
        [field: SerializeField] public RectTransform SlidePanel        { get; private set; }

        /// <summary>좌측 세로 강조 선. Show 시 위에서 아래로 그려진다.</summary>
        [field: SerializeField] public RectTransform AccentLine        { get; private set; }

        /// <summary>SessionLabel 의 알파 애니메이션용 CanvasGroup.</summary>
        [field: SerializeField] public CanvasGroup   SessionLabelGroup { get; private set; }

        /// <summary>"EXPLORATION" 등 모드 레이블 텍스트.</summary>
        [field: SerializeField] public TMP_Text      SessionLabelText  { get; private set; }

        /// <summary>StageName 의 알파 애니메이션용 CanvasGroup.</summary>
        [field: SerializeField] public CanvasGroup   StageNameGroup    { get; private set; }

        /// <summary>스테이지 이름을 표시하는 텍스트.</summary>
        [field: SerializeField] public TMP_Text      StageNameText     { get; private set; }

        // ── Top-right panel (date/time) ───────────────────────────────────────
        /// <summary>슬라이드 애니메이션 대상 패널 (우상단, 날짜/시간).</summary>
        [field: SerializeField] public RectTransform DateTimePanel      { get; private set; }

        /// <summary>우측 가로 강조 선. Show 시 우에서 좌로 그려진다.</summary>
        [field: SerializeField] public RectTransform DateTimeAccentLine { get; private set; }

        /// <summary>시간 텍스트의 알파 애니메이션용 CanvasGroup.</summary>
        [field: SerializeField] public CanvasGroup   TimeGroup          { get; private set; }

        /// <summary>인게임 시간을 표시하는 텍스트 (예: "12:30").</summary>
        [field: SerializeField] public TMP_Text      TimeText           { get; private set; }

        /// <summary>날짜 텍스트의 알파 애니메이션용 CanvasGroup.</summary>
        [field: SerializeField] public CanvasGroup   DateGroup          { get; private set; }

        /// <summary>인게임 날짜를 표시하는 텍스트 (예: "MON 4/9").</summary>
        [field: SerializeField] public TMP_Text      DateText           { get; private set; }

        // ── Bottom panel (SP bar) ─────────────────────────────────────────────
        /// <summary>슬라이드 애니메이션 대상 패널 (하단, SP 바).</summary>
        [field: SerializeField] public RectTransform SpBarPanel  { get; private set; }

        /// <summary>SP 패널 전체의 알파 애니메이션용 CanvasGroup.</summary>
        [field: SerializeField] public CanvasGroup   SpBarGroup  { get; private set; }

        /// <summary>SP 게이지 채움 이미지. ScaleX 로 비율을 표현한다.</summary>
        [field: SerializeField] public RectTransform SpBarFill   { get; private set; }

        /// <summary>현재 SP 수치를 표시하는 텍스트.</summary>
        [field: SerializeField] public TMP_Text      SpValueText { get; private set; }

        private Vector2 _onScreenPos;
        private Vector2 _dateTimeOnScreenPos;
        private Vector2 _spBarOnScreenPos;
        private float   _spRatio = 1f;

        private void Awake()
        {
            Debug.Assert(SlidePanel        != null, "[ExplorationHud] SlidePanel이 할당되지 않았습니다.");
            Debug.Assert(AccentLine        != null, "[ExplorationHud] AccentLine이 할당되지 않았습니다.");
            Debug.Assert(SessionLabelGroup != null, "[ExplorationHud] SessionLabelGroup이 할당되지 않았습니다.");
            Debug.Assert(StageNameGroup    != null, "[ExplorationHud] StageNameGroup이 할당되지 않았습니다.");
            Debug.Assert(DateTimePanel     != null, "[ExplorationHud] DateTimePanel이 할당되지 않았습니다.");
            Debug.Assert(SpBarPanel        != null, "[ExplorationHud] SpBarPanel이 할당되지 않았습니다.");

            _onScreenPos         = SlidePanel.anchoredPosition;
            _dateTimeOnScreenPos = DateTimePanel.anchoredPosition;
            _spBarOnScreenPos    = SpBarPanel.anchoredPosition;
        }

        // ── Data Binding ──────────────────────────────────────────────────────

        /// <summary>HUD 에 표시할 스테이지 이름을 설정한다.</summary>
        public void SetStageName(string stageName)
        {
            if (StageNameText != null)
                StageNameText.text = stageName;
        }

        /// <summary>HUD 에 표시할 인게임 시간·날짜를 설정한다.</summary>
        public void SetDateTime(string time, string date)
        {
            if (TimeText != null) TimeText.text = time;
            if (DateText != null) DateText.text = date;
        }

        /// <summary>HUD SP 바의 수치와 비율을 설정한다.</summary>
        public void SetSp(int current, int max)
        {
            _spRatio = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
            if (SpValueText != null) SpValueText.text = $"{current}";
        }

        // ── UiViewBase ────────────────────────────────────────────────────────

        /// <summary>
        /// 좌상단·우상단·하단 3개 패널이 동시에 슬라이드 인되고,
        /// 이어서 각 텍스트 요소가 시차를 두고 페이드 인한다.
        /// </summary>
        protected override async UniTask OnShow()
        {
            var offLeft   = OffScreenLeft();
            var offRight  = OffScreenRight();
            var offBottom = OffScreenBottom();

            SlidePanel.anchoredPosition    = offLeft;
            AccentLine.localScale          = new Vector3(1f, 0f, 1f);
            SessionLabelGroup.alpha        = 0f;
            StageNameGroup.alpha           = 0f;

            DateTimePanel.anchoredPosition = offRight;
            DateTimeAccentLine.localScale  = new Vector3(0f, 1f, 1f);
            if (TimeGroup != null) TimeGroup.alpha = 0f;
            if (DateGroup != null) DateGroup.alpha = 0f;

            SpBarPanel.anchoredPosition = offBottom;
            if (SpBarGroup != null) SpBarGroup.alpha             = 0f;
            if (SpBarFill  != null) SpBarFill.localScale         = new Vector3(0f, 1f, 1f);

            await Sequence.Create(useUnscaledTime: true)
                .Group(Tween.UIAnchoredPosition(SlidePanel, offLeft, _onScreenPos,
                       SlideDuration, Ease.OutBack, useUnscaledTime: true))
                .Group(Tween.ScaleY(AccentLine, 0f, 1f,
                       AccentLineDuration, Ease.OutCubic, useUnscaledTime: true))
                .Group(Tween.UIAnchoredPosition(DateTimePanel, offRight, _dateTimeOnScreenPos,
                       SlideDuration, Ease.OutBack, useUnscaledTime: true))
                .Group(Tween.ScaleX(DateTimeAccentLine, 0f, 1f,
                       AccentLineDuration, Ease.OutCubic, useUnscaledTime: true))
                .Group(Tween.UIAnchoredPosition(SpBarPanel, offBottom, _spBarOnScreenPos,
                       SlideDuration, Ease.OutCubic, useUnscaledTime: true))
                .Group(Tween.Alpha(SessionLabelGroup, 0f, 1f,
                       TextFadeDuration, Ease.OutQuad,
                       startDelay: ModeLabelDelay, useUnscaledTime: true))
                .Group(Tween.Alpha(StageNameGroup, 0f, 1f,
                       TextFadeDuration, Ease.OutQuad,
                       startDelay: StageNameDelay, useUnscaledTime: true))
                .Group(Tween.Alpha(TimeGroup, 0f, 1f,
                       TextFadeDuration, Ease.OutQuad,
                       startDelay: TimeDelay, useUnscaledTime: true))
                .Group(Tween.Alpha(DateGroup, 0f, 1f,
                       TextFadeDuration, Ease.OutQuad,
                       startDelay: DateDelay, useUnscaledTime: true))
                .Group(Tween.Alpha(SpBarGroup, 0f, 1f,
                       TextFadeDuration, Ease.OutQuad,
                       startDelay: 0.15f, useUnscaledTime: true))
                .Group(Tween.ScaleX(SpBarFill, 0f, _spRatio,
                       SpBarFillDuration, Ease.OutCubic,
                       startDelay: SpFillDelay, useUnscaledTime: true));
        }

        /// <summary>
        /// 전체 텍스트 동시 페이드 아웃 → 강조선 수축 → 3개 패널 동시 슬라이드 아웃 순으로 재생된다.
        /// </summary>
        protected override async UniTask OnHide()
        {
            await Sequence.Create(useUnscaledTime: true)
                .Group(Tween.Alpha(SessionLabelGroup, 1f, 0f,
                       HideTextDuration, Ease.InQuad, useUnscaledTime: true))
                .Group(Tween.Alpha(StageNameGroup, 1f, 0f,
                       HideTextDuration, Ease.InQuad, useUnscaledTime: true))
                .Group(Tween.Alpha(TimeGroup, 1f, 0f,
                       HideTextDuration, Ease.InQuad, useUnscaledTime: true))
                .Group(Tween.Alpha(DateGroup, 1f, 0f,
                       HideTextDuration, Ease.InQuad, useUnscaledTime: true))
                .Group(Tween.Alpha(SpBarGroup, 1f, 0f,
                       HideTextDuration, Ease.InQuad, useUnscaledTime: true))
                .Group(Tween.ScaleX(SpBarFill, _spRatio, 0f,
                       HideAccentDuration, Ease.InCubic, useUnscaledTime: true))
                .Chain(Tween.ScaleY(AccentLine, 1f, 0f,
                       HideAccentDuration, Ease.InCubic, useUnscaledTime: true))
                .Group(Tween.ScaleX(DateTimeAccentLine, 1f, 0f,
                       HideAccentDuration, Ease.InCubic, useUnscaledTime: true))
                .Chain(Tween.UIAnchoredPosition(SlidePanel, _onScreenPos, OffScreenLeft(),
                       HideSlideDuration, Ease.InCubic, useUnscaledTime: true))
                .Group(Tween.UIAnchoredPosition(DateTimePanel, _dateTimeOnScreenPos, OffScreenRight(),
                       HideSlideDuration, Ease.InCubic, useUnscaledTime: true))
                .Group(Tween.UIAnchoredPosition(SpBarPanel, _spBarOnScreenPos, OffScreenBottom(),
                       HideSlideDuration, Ease.InCubic, useUnscaledTime: true));
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private Vector2 OffScreenLeft()
            => new Vector2(_onScreenPos.x - Screen.width, _onScreenPos.y);

        private Vector2 OffScreenRight()
            => new Vector2(_dateTimeOnScreenPos.x + Screen.width, _dateTimeOnScreenPos.y);

        private Vector2 OffScreenBottom()
            => new Vector2(_spBarOnScreenPos.x, _spBarOnScreenPos.y - Screen.height);
    }
}
