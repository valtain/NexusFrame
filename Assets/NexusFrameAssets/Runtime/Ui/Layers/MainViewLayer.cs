using Cysharp.Threading.Tasks;
using UnityEngine;

namespace NexusFrame
{
    /// <summary>
    /// 세션당 하나의 활성 뷰(HUD 등)를 표시하는 레이어.
    /// SetView 호출 시 기존 뷰를 Hide한 뒤 신규 뷰를 Show한다.
    /// </summary>
    public class MainViewLayer : MonoBehaviour
    {
        [field: SerializeField] public Canvas Canvas { get; private set; }

        private IUiView _currentView;

        public async UniTask SetView(IUiView view)
        {
            if (_currentView != null)
            {
                await _currentView.Hide();
            }
            _currentView = view;
            if (_currentView != null)
            {
                _currentView.AttachTo(Canvas.transform);
                await _currentView.Show();
            }
        }

        public async UniTask ClearView()
        {
            if (_currentView == null) return;
            await _currentView.Hide();
            _currentView = null;
        }
    }
}
