using Cysharp.Threading.Tasks;
using UnityEngine;

namespace NexusFrame
{
    /// <summary>
    /// 독립적으로 등장·소멸하는 알림 스타일 레이어.
    /// 스택 없음 — 각 뷰가 독립 생명주기를 가진다.
    /// </summary>
    public class FloatingLayer : MonoBehaviour
    {
        [field: SerializeField] public Canvas Canvas { get; private set; }

        public UniTask Show(IUiView view)
        {
            view.AttachTo(Canvas.transform);
            return view.Show();
        }

        public UniTask Hide(IUiView view) => view.Hide();
    }
}
