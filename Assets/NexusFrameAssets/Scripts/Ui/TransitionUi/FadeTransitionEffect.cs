using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class FadeTransitionEffect : ITransitionEffect
{
    public TransitionEffectType Type => TransitionEffectType.Fade;
    const float fadeDuration = 0.5f;
    const int fadeEffectDelayMs = 100;
    const float fadeBeginAlpha = 0.2f;
    const float fadeEndAlpha = 1.0f;

    private Graphic _backgroundOverlay = default;

    public async UniTask Begin(Graphic backgroundOverlay)
    {
        _backgroundOverlay = backgroundOverlay;
        _backgroundOverlay.gameObject.SetActive(true);
        await FadeEffect(fadeBeginAlpha, fadeEndAlpha, fadeDuration);
    }
    public async UniTask End()
    {
        await FadeEffect(fadeEndAlpha, fadeBeginAlpha, fadeDuration);
        _backgroundOverlay.gameObject.SetActive(false);
    }

    private async UniTask FadeEffect(float fromAlpha, float toAlpha, float duration)
    {
        var beginTime = Time.unscaledTime;
        var endTime = beginTime + fadeDuration;

        SetAlpha(fromAlpha);
        while(true)
        {
            await UniTask.Delay(fadeEffectDelayMs);
            var now = Time.unscaledTime;
            var alpha = Mathf.Lerp(fromAlpha, toAlpha, (now - beginTime)/fadeDuration);
            alpha = Mathf.Clamp01(alpha);
            SetAlpha(alpha);
            if (endTime <= now)
            {
                break;
            }
        }
        SetAlpha(toAlpha);
    }

    private void SetAlpha(float alpha)
    {
        var color = _backgroundOverlay.color;
        color.a = alpha;
        _backgroundOverlay.color = color;
    }

}
