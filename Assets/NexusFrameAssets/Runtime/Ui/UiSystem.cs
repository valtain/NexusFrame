using Cysharp.Threading.Tasks;
using UnityEngine;

namespace NexusFrame
{
    /// <summary>
    /// UI 레이어 전체의 단일 진입점.
    /// <para>
    /// - Scope 패턴(Transition, Loading)은 static 편의 메서드로 노출한다.
    /// - 나머지 레이어(Popup, MainView, Floating)는 Instance 경유로 접근한다.
    /// </para>
    /// </summary>
    public class UiSystem : MonoPreload<UiSystem>
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize() => ResetInstance();

        [field: SerializeField] public BgCoverController BgCover    { get; private set; }
        [field: SerializeField] public NormalLayer       Normal     { get; private set; }
        [field: SerializeField] public MainViewLayer     MainView   { get; private set; }
        [field: SerializeField] public PopupLayer        Popup      { get; private set; }
        [field: SerializeField] public FloatingLayer     Floating   { get; private set; }
        [field: SerializeField] public LoadingLayer      Loading    { get; private set; }
        [field: SerializeField] public TransitionLayer   Transition { get; private set; }

        // ── Static Convenience
        /// <summary>SceneDirector, GamePlaySystem 등 인프라에서 사용하는 화면 전환 Scope.
        /// Scope 패턴(Begin-End 쌍)에 한해 static 편의 메서드로 노출한다.
        /// 그 외 레이어 접근은 Instance 경유를 원칙으로 한다.</summary>

        public static UniTask<TransitionLayer.TransitionScope> ScopeTransition(TransitionEffectType type)
            => Instance.Transition.Scope(type);

        /// <summary>비동기 작업 중 입력 차단 Scope.</summary>
        public static UniTask<LoadingLayer.LoadingScope> ScopeLoading()
            => Instance.Loading.Scope();

        public static bool IsTransitioning
            => HasInstance && Instance.Transition.TransitionCount > 0;
    }
}
