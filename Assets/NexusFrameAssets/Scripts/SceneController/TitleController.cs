using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TitleController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Text titleText;
    [SerializeField] private Text subtitleText;
    [SerializeField] private Text promptText;

    private bool isTransitioning = false;

    private void Start()
    {
        Camera.main.backgroundColor = new Color(0.05f, 0.05f, 0.15f);
        Camera.main.clearFlags = CameraClearFlags.SolidColor;

        StartCoroutine(BlinkPrompt());
    }

    private void Update()
    {
        if (!isTransitioning && Input.anyKeyDown)
            StartCoroutine(GoToMainMenu());
    }

    /// <summary>
    /// Inspector 우클릭 > "Create UI" 로 에디터에서 UI 계층을 생성합니다.
    /// </summary>
    [ContextMenu("Create UI")]
    private void CreateUI()
    {
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

        // 게임 타이틀
        titleText = new GameObject("Title").AddComponent<Text>();
        titleText.transform.SetParent(canvas.transform, false);
        titleText.text = "NexusFrame";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 96;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;
        titleText.rectTransform.anchorMin = new Vector2(0.1f, 0.5f);
        titleText.rectTransform.anchorMax = new Vector2(0.9f, 0.85f);
        titleText.rectTransform.sizeDelta = Vector2.zero;

        // 서브타이틀
        subtitleText = new GameObject("Subtitle").AddComponent<Text>();
        subtitleText.transform.SetParent(canvas.transform, false);
        subtitleText.text = "An Epic Adventure";
        subtitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        subtitleText.fontSize = 32;
        subtitleText.alignment = TextAnchor.MiddleCenter;
        subtitleText.color = new Color(0.8f, 0.8f, 0.8f);
        subtitleText.rectTransform.anchorMin = new Vector2(0.1f, 0.42f);
        subtitleText.rectTransform.anchorMax = new Vector2(0.9f, 0.52f);
        subtitleText.rectTransform.sizeDelta = Vector2.zero;

        // 프레스 애니 키 프롬프트
        promptText = new GameObject("Prompt").AddComponent<Text>();
        promptText.transform.SetParent(canvas.transform, false);
        promptText.text = "Press Any Key to Start";
        promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        promptText.fontSize = 28;
        promptText.alignment = TextAnchor.MiddleCenter;
        promptText.color = new Color(1f, 1f, 0.5f);
        promptText.rectTransform.anchorMin = new Vector2(0.1f, 0.15f);
        promptText.rectTransform.anchorMax = new Vector2(0.9f, 0.25f);
        promptText.rectTransform.sizeDelta = Vector2.zero;

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    private IEnumerator BlinkPrompt()
    {
        while (true)
        {
            promptText.enabled = !promptText.enabled;
            yield return new WaitForSeconds(0.6f);
        }
    }

    private IEnumerator GoToMainMenu()
    {
        isTransitioning = true;
        yield return new WaitForSeconds(0.2f);
        SceneManager.LoadScene("MainMenu");
    }
}
