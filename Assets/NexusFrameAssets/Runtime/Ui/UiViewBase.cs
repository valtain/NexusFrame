using Cysharp.Threading.Tasks;
using NexusFrame.Extensions;
using UnityEngine;

namespace NexusFrame
{
    /// <summary>
    /// IUiView의 MonoBehaviour 기본 구현.
    /// 서브클래스는 OnShow/OnHide를 오버라이드하여 애니메이션을 추가한다.
    /// </summary>
    public abstract class UiViewBase : MonoBehaviour, IUiView
    {
        public async UniTask Show()
        {
            gameObject.SetActiveSafe(true);
            await OnShow();
        }

        public async UniTask Hide()
        {
            await OnHide();
            gameObject.SetActiveSafe(false);
        }

        public virtual UniTask Pause() => UniTask.CompletedTask;
        public virtual UniTask Resume() => UniTask.CompletedTask;

        public virtual void AttachTo(Transform parent)
            => transform.SetParent(parent, worldPositionStays: false);

        protected virtual UniTask OnShow() => UniTask.CompletedTask;
        protected virtual UniTask OnHide() => UniTask.CompletedTask;
    }
}
