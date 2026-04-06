using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace NexusFrame
{
    public enum TransitionEffectType
    {
        Instant = 0,
        Fade = 1
    }

    public interface ITransitionEffect
    {
        public TransitionEffectType Type {get;}
        public UniTask Begin(Graphic backgroundOverlay, CancellationToken ct = default);
        public UniTask End(CancellationToken ct = default);
    }
}