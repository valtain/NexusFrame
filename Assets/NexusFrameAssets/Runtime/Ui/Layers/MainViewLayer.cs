using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace NexusFrame
{
    /// <summary>
    /// 세션당 하나의 활성 뷰(HUD 등)를 표시하는 레이어.
    /// <para>
    /// <see cref="ShowView{T}"/> 로 뷰를 생성·표시하고,
    /// <see cref="HideView"/> 로 숨김·소멸시킨다.
    /// 동시에 active 한 뷰는 하나로 제한된다.
    /// </para>
    /// </summary>
    public class MainViewLayer : MonoBehaviour
    {
        [field: SerializeField] public Canvas Canvas { get; private set; }

        /// <summary>현재 표시 중인 뷰. null 이면 아무것도 표시되지 않는다.</summary>
        public IUiView CurrentView { get; private set; }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// <typeparamref name="T"/> 타입 뷰를 Resources 에서 로드하여 생성·표시한다.
        /// 기존 뷰가 있으면 먼저 Hide 한다.
        /// <typeparamref name="T"/> 에 <c>public const string ResourcesPath</c> 가 선언되어 있어야 한다.
        /// </summary>
        public async UniTask<T> ShowView<T>() where T : MonoBehaviour, IUiView
        {
            var path = ResolveResourcesPath<T>();

            if (CurrentView != null)
                await CurrentView.Hide();

            var prefab = Resources.Load<GameObject>(path);
            Debug.Assert(prefab != null,
                $"[MainViewLayer] Resources/{path} 를 찾을 수 없습니다.");

            var go = Instantiate(prefab);
            var view = go.GetComponent<T>();
            Debug.Assert(view != null,
                $"[MainViewLayer] 프리팹에 {typeof(T).Name} 컴포넌트가 없습니다.");

            CurrentView = view;
            CurrentView.AttachTo(Canvas.transform);
            await CurrentView.Show();

            return view;
        }

        /// <summary>
        /// 지정한 뷰를 Hide 하고 GameObject 를 소멸시킨다.
        /// <paramref name="view"/> 가 <see cref="CurrentView"/> 와 다르면 무시한다.
        /// </summary>
        public async UniTask HideView(IUiView view)
        {
            if (view == null || view != CurrentView)
                return;

            await CurrentView.Hide();
            var viewMb = (MonoBehaviour)CurrentView;
            var viewGo = viewMb.gameObject;
            CurrentView = null;
            Destroy(viewGo);
        }

        /// <summary>
        /// 현재 뷰를 Hide 만 하고 GameObject 는 소멸시키지 않는다.
        /// Session 이 <see cref="HideView"/> 를 누락했을 때의 Safety fallback 용도.
        /// </summary>
        public async UniTask ClearView()
        {
            if (CurrentView == null) return;
            await CurrentView.Hide();
            CurrentView = null;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static string ResolveResourcesPath<T>() where T : MonoBehaviour, IUiView
        {
            var attr = typeof(T).GetCustomAttribute<UiViewPathAttribute>();
            Debug.Assert(attr != null,
                $"[MainViewLayer] {typeof(T).Name} 에 [UiViewPath] 어트리뷰트가 선언되어야 합니다.");
            return attr.Path;
        }
    }
}
