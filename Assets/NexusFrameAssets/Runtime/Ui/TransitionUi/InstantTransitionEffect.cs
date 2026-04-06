using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;

namespace NexusFrame
{
public class InstantTransitionEffect : ITransitionEffect
{
    public TransitionEffectType Type => TransitionEffectType.Instant;

    private Graphic _backgroundOverlay = default;

    public async UniTask Begin(Graphic backgroundOverlay, CancellationToken ct = default)
    {
        if (ct.IsCancellationRequested)
        {
            return;
        }
        _backgroundOverlay = backgroundOverlay;
        _backgroundOverlay.gameObject.SetActive(true);
        var color = _backgroundOverlay.color;
        color.a = 1.0f;
        _backgroundOverlay.color = color;
        await UniTask.NextFrame();
    }

    public async UniTask End(CancellationToken ct = default)
    {
        if (ct.IsCancellationRequested)
        {
            return;
        }
        var color = _backgroundOverlay.color;
        color.a = 0.0f;
        _backgroundOverlay.color = color;
        _backgroundOverlay.gameObject.SetActive(false);
        await UniTask.NextFrame();
    }
}
}