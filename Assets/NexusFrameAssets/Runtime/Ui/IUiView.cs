using Cysharp.Threading.Tasks;
using UnityEngine;

namespace NexusFrame
{
    /// <summary>
    /// UiSystem이 관리하는 모든 UI 패널이 구현해야 하는 인터페이스.
    /// Show/Hide는 입장·퇴장 애니메이션을 포함한 비동기 전환이다.
    /// </summary>
    public interface IUiView
    {
        UniTask Show();
        UniTask Hide();

        /// <summary>Popup 스택에서 위에 새 패널이 쌓일 때 호출된다.</summary>
        UniTask Pause() => UniTask.CompletedTask;

        /// <summary>Popup 스택에서 위의 패널이 제거될 때 호출된다.</summary>
        UniTask Resume() => UniTask.CompletedTask;

        /// <summary>
        /// 지정한 부모 Transform에 이 뷰를 reparent한다.
        /// PopupLayer가 Stack/Front Canvas 간 이동 시 내부적으로 호출한다.
        /// </summary>
        void AttachTo(Transform parent);
    }
}
