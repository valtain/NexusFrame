using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NexusFrame.Extensions;
using UnityEngine;

namespace NexusFrame
{
    /// <summary>
    /// 화면 전환 효과를 관리하는 레이어. 기존 Transition.cs 로직을 이전한 것.
    /// <para>
    /// - Begin/End 호출 쌍을 <see cref="TransitionCount"/>로 추적해 중복 요청을 안전하게 처리한다.
    /// - BgCover는 <see cref="BgCoverController"/>를 통해 요청·해제한다.
    /// - End 처리 중 새 Begin이 들어오면 End 완료 후 자동으로 해당 Begin 효과를 실행한다.
    /// </para>
    /// </summary>
    public class TransitionLayer : MonoBehaviour
    {
        [field: SerializeField] public Canvas Canvas { get; private set; }

        public int TransitionCount { get; private set; }


        private ITransitionEffect _activeEffect;
        private readonly Dictionary<TransitionEffectType, ITransitionEffect> _effectMap = new();
        private bool _isPendingForEnd;
        private System.Threading.CancellationToken _lifetimeToken;

        private void Awake()
        {
            _lifetimeToken = this.GetCancellationTokenOnDestroy();
            Canvas.gameObject.SetActiveSafe(false);
            AddEffect(new InstantTransitionEffect());
            AddEffect(new FadeTransitionEffect());
            void AddEffect(ITransitionEffect e) => _effectMap.Add(e.Type, e);
        }

        public async UniTask Begin(TransitionEffectType type)
        {
            ++TransitionCount;
            if (TransitionCount >= 2) return;

            UiSystem.Instance.BgCover.Request(BgCoverPriority.Transition);
            Canvas.gameObject.SetActiveSafe(true);
            _activeEffect = _effectMap[type];

            if (_isPendingForEnd) return; // End 효과 종료 시 자동 처리됨

            await _activeEffect.Begin(UiSystem.Instance.BgCover.Image, _lifetimeToken);
        }

        public async UniTask End()
        {
            --TransitionCount;
            if (TransitionCount >= 1) return;

            _isPendingForEnd = true;
            var effectToEnd = _activeEffect;
            _activeEffect = null;

            await effectToEnd.End(_lifetimeToken);

            if (!UiSystem.HasInstance) return;

            if (_activeEffect != null)
            {
                // End 처리 중 추가된 Begin 효과 실행
                _activeEffect.Begin(UiSystem.Instance.BgCover.Image, _lifetimeToken).Forget();
            }
            else
            {
                Canvas.gameObject.SetActiveSafe(false);
                UiSystem.Instance.BgCover.Release(BgCoverPriority.Transition);
            }
            _isPendingForEnd = false;
        }

        public async UniTask<TransitionScope> Scope(TransitionEffectType type)
        {
            await Begin(type);
            return new TransitionScope();
        }

        public readonly struct TransitionScope
        {
            public UniTask DisposeAsync() => UiSystem.Instance.Transition.End();
        }
    }
}
