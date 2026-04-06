using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
public enum TransitionEffectType
{
    Instant = 0,
    Fade = 1
}
public interface ITransitionEffect
{
    public TransitionEffectType Type {get;}
    public UniTask Begin(Graphic backgroundOverlay);
    public UniTask End();
}