using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using NexusFrame;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SplashController : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float displayDuration = 2.5f;
    [SerializeField] private float fadeDuration = 0.8f;

    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image background;
    [SerializeField] private Text splashText;

    private void Start()
    {

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        SplashSequence().Forget();
    }

    /// <summary>
    /// Inspector 우클릭 > "Create UI" 로 에디터에서 UI 계층을 생성합니다.
    /// </summary>
    [ContextMenu("Create UI")]
    private void CreateUI()
    {
        // 기존 Canvas 제거
        var existing = GetComponentInChildren<Canvas>();
        if (existing != null)
        {
#if UNITY_EDITOR
            DestroyImmediate(existing.gameObject);
#else
            Destroy(existing.gameObject);
#endif
        }

        // Canvas
        var canvas = new GameObject("Canvas").AddComponent<Canvas>();
        canvas.transform.SetParent(transform, false);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvas.gameObject.AddComponent<GraphicRaycaster>();

        // 배경
        background = new GameObject("Background").AddComponent<Image>();
        background.transform.SetParent(canvas.transform, false);
        background.color = Color.black;
        background.rectTransform.anchorMin = Vector2.zero;
        background.rectTransform.anchorMax = Vector2.one;
        background.rectTransform.sizeDelta = Vector2.zero;

        // 스플래시 텍스트
        splashText = new GameObject("SplashText").AddComponent<Text>();
        splashText.transform.SetParent(canvas.transform, false);
        splashText.text = "NexusFrame";
        splashText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        splashText.fontSize = 72;
        splashText.fontStyle = FontStyle.Bold;
        splashText.alignment = TextAnchor.MiddleCenter;
        splashText.color = Color.white;
        splashText.rectTransform.anchorMin = new Vector2(0.1f, 0.3f);
        splashText.rectTransform.anchorMax = new Vector2(0.9f, 0.7f);
        splashText.rectTransform.sizeDelta = Vector2.zero;

        // CanvasGroup (페이드용)
        canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    private async UniTaskVoid SplashSequence()
    {
        await Fade(0f, 1f, fadeDuration);
        await UniTask.Delay(Mathf.RoundToInt(displayDuration * 1000));
        await Fade(1f, 0f, fadeDuration);
        SceneDirector.LoadScene("Title").Forget();
    }

    private async UniTask Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            await UniTask.Yield();
        }
        canvasGroup.alpha = to;
    }
}
