using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NexusFrame
{
public class MainMenuController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Text titleText;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    private void Start()
    {
        Camera.main.backgroundColor = new Color(0.08f, 0.08f, 0.12f);
        Camera.main.clearFlags = CameraClearFlags.SolidColor;

        newGameButton.onClick.AddListener(OnNewGame);
        continueButton.onClick.AddListener(OnContinue);
        settingsButton.onClick.AddListener(OnSettings);
        quitButton.onClick.AddListener(OnQuit);

        continueButton.interactable = false;
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

        // 타이틀
        titleText = new GameObject("Title").AddComponent<Text>();
        titleText.transform.SetParent(canvas.transform, false);
        titleText.text = "NexusFrame";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 64;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;
        titleText.rectTransform.anchorMin = new Vector2(0.25f, 0.72f);
        titleText.rectTransform.anchorMax = new Vector2(0.75f, 0.88f);
        titleText.rectTransform.sizeDelta = Vector2.zero;

        // 버튼 컨테이너
        var container = new GameObject("ButtonContainer").AddComponent<RectTransform>();
        container.SetParent(canvas.transform, false);
        container.anchorMin = new Vector2(0.35f, 0.2f);
        container.anchorMax = new Vector2(0.65f, 0.68f);
        container.sizeDelta = Vector2.zero;

        var layout = container.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 20f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        newGameButton  = CreateButton(container, "New Game",  Color.white,                  null);
        continueButton = CreateButton(container, "Continue",  new Color(0.7f, 0.7f, 0.7f), null);
        settingsButton = CreateButton(container, "Settings",  Color.white,                  null);
        quitButton     = CreateButton(container, "Quit",      new Color(1f, 0.5f, 0.5f),   null);

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    private Button CreateButton(RectTransform parent, string label, Color textColor, UnityEngine.Events.UnityAction onClick)
    {
        var btnObj = new GameObject(label);
        btnObj.transform.SetParent(parent, false);

        var rect = btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 70);

        var image = btnObj.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.08f);

        var btn = btnObj.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.25f);
        colors.pressedColor = new Color(1f, 1f, 1f, 0.4f);
        btn.colors = colors;

        if (onClick != null)
            btn.onClick.AddListener(onClick);

        var textObj = new GameObject("Text").AddComponent<Text>();
        textObj.transform.SetParent(btnObj.transform, false);
        textObj.text = label;
        textObj.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textObj.fontSize = 36;
        textObj.alignment = TextAnchor.MiddleCenter;
        textObj.color = textColor;
        textObj.rectTransform.anchorMin = Vector2.zero;
        textObj.rectTransform.anchorMax = Vector2.one;
        textObj.rectTransform.sizeDelta = Vector2.zero;

        return btn;
    }

    private void OnNewGame()
    {
        // TODO: 게임 데이터 초기화 후 게임 씬으로 전환
        Debug.Log("New Game");
        // SceneManager.LoadScene("GameScene");
        SceneDirector.LoadScene("World0").Forget();
    }

    private void OnContinue()
    {
        // TODO: 저장 데이터 로드 후 게임 씬으로 전환
        Debug.Log("Continue");
    }

    private void OnSettings()
    {
        // TODO: 설정 씬 또는 패널 열기
        Debug.Log("Settings");
    }

    private void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
}
