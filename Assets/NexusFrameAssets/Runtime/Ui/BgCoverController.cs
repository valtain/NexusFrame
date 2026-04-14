using System.Collections.Generic;
using NexusFrame.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace NexusFrame
{
    /// <summary>
    /// Popup / Loading / Transition 레이어가 공유하는 배경 커버 Canvas를 관리한다.
    /// <para>
    /// - enum 값이 곧 Canvas sortingOrder이므로 별도 매핑 없이 우선순위를 표현한다.
    /// - <see cref="SortedSet{T}"/>의 Max를 적용해 Release 순서와 무관하게 이전 상태로 자동 복원된다.
    /// </para>
    /// </summary>
    public class BgCoverController : MonoBehaviour
    {
        [SerializeField] private Canvas  _canvas = default;
        [SerializeField] private Graphic _image  = default;

        /// <summary>ITransitionEffect.Begin 에 전달하는 배경 커버 Graphic.</summary>
        public Graphic Image => _image;

        private readonly SortedSet<int> _requests = new();

        public void Request(BgCoverPriority priority)
        {
            _requests.Add((int)priority);
            Apply();
        }

        public void Release(BgCoverPriority priority)
        {
            _requests.Remove((int)priority);
            Apply();
        }

        private void Apply()
        {
            if (_requests.Count == 0)
            {
                _canvas.gameObject.SetActiveSafe(false);
                return;
            }
            _canvas.gameObject.SetActiveSafe(true);
            _canvas.sortingOrder = _requests.Max;
        }
    }

    public enum BgCoverPriority
    {
        Popup      = 25,  // PopupStack(20)과 PopupFront(30) 사이
        Loading    = 45,  // Floating(40)과 Loading(50) 사이
        Transition = 55,  // Loading(50)과 Transition(60) 사이
    }
}
