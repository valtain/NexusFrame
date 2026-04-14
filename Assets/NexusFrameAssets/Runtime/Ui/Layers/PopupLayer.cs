using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace NexusFrame
{
    /// <summary>
    /// LIFO 스택 기반 팝업 레이어.
    /// <para>
    /// - _stackCanvas(sort:20): 일시정지된 하위 팝업들
    /// - _frontCanvas(sort:30): 최상단 활성 팝업 하나
    /// - BgCover는 sort=25로 두 Canvas 사이에 끼어 최상단 팝업만 밝게 보이게 한다.
    /// </para>
    /// Push/Pop 시 내부에서 reparenting을 처리하므로 호출자는 Canvas를 신경 쓰지 않아도 된다.
    /// </summary>
    public class PopupLayer : MonoBehaviour
    {
        [SerializeField] private Canvas _stackCanvas;  // sort: 20, paused 팝업
        [SerializeField] private Canvas _frontCanvas;  // sort: 30, active 팝업

        private readonly Stack<IUiView> _stack = new();

        public int Count => _stack.Count;

        public async UniTask Push(IUiView popup)
        {
            if (_stack.TryPeek(out var top))
            {
                top.AttachTo(_stackCanvas.transform);
                await top.Pause();
            }

            if (_stack.Count == 0)
                UiSystem.Instance.BgCover.Request(BgCoverPriority.Popup);

            _stack.Push(popup);
            popup.AttachTo(_frontCanvas.transform);
            await popup.Show();
        }

        public async UniTask Pop()
        {
            if (_stack.Count == 0)
            {
                return;
            }

            var top = _stack.Pop();
            await top.Hide();

            if (_stack.Count == 0)
            {
                UiSystem.Instance.BgCover.Release(BgCoverPriority.Popup);
            }
            else
            {
                var prev = _stack.Peek();
                prev.AttachTo(_frontCanvas.transform);
                await prev.Resume();
            }
        }

        public async UniTask PopAll()
        {
            while (_stack.Count > 0)
                await Pop();
        }
    }
}
