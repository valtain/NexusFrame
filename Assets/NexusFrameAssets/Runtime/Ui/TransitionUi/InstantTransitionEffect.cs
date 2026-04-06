using Cysharp.Threading.Tasks;
using UnityEngine.UI;

namespace NexusFrame
{
public class InstantTransitionEffect : ITransitionEffect
{
    public TransitionEffectType Type => TransitionEffectType.Instant;

    private Graphic _backgroundOverlay = default;
    public async UniTask Begin(Graphic backgroundOverlay)
    {
        _backgroundOverlay = backgroundOverlay;
        _backgroundOverlay.gameObject.SetActive(true);
        var color = _backgroundOverlay.color;
        color.a = 1.0f;
        _backgroundOverlay.color = color;
    }

    public async UniTask End()
    {
        var color = _backgroundOverlay.color;
        color.a = 0.0f;
        _backgroundOverlay.color = color;
        _backgroundOverlay.gameObject.SetActive(false);
    }
}
}