using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class LevelHUDFactory_NoText : MonoBehaviour
{
    [Tooltip("If left empty, we will find the first LevelManager in the scene.")]
    public LevelManager levelManager;

    [Header("Options")]
    public string canvasName = "LevelHUD_Canvas";

    void Awake()
    {
        if (!levelManager) levelManager = FindOne<LevelManager>();
        if (!levelManager)
        {
            Debug.LogError("[LevelHUDFactory_NoText] No LevelManager found. Aborting UI build.");
            return;
        }

        // Canvas + EventSystem
        var canvasGO = new GameObject(canvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        EnsureEventSystemExists();

        // ===== HUD root (top-left group for your counters/mode) =====
        var hudRoot = CreateRect("HUD", canvasGO.transform,
                                 new Vector2(0, 1), new Vector2(0, 1),
                                 Vector2.zero, Vector2.zero);
        // Placeholders (no text components; just empty RectTransforms positioned for your TMPs)
        var alliesSlot = CreateRect("Allies Text (DROP TMP HERE)", hudRoot,
                                    new Vector2(0, 1), new Vector2(0, 1),
                                    Vector2.zero, Vector2.zero, new Vector2(800, 40));
        PinTopLeft(alliesSlot, 0);

        var zombiesSlot = CreateRect("Zombies Text (DROP TMP HERE)", hudRoot,
                                     new Vector2(0, 1), new Vector2(0, 1),
                                     Vector2.zero, Vector2.zero, new Vector2(800, 40));
        PinTopLeft(zombiesSlot, 34);

        var modeSlot = CreateRect("Mode Text (DROP TMP HERE)", hudRoot,
                                  new Vector2(0, 1), new Vector2(0, 1),
                                  Vector2.zero, Vector2.zero, new Vector2(800, 40));
        PinTopLeft(modeSlot, 68);

        // ===== End Panel (center overlay with buttons) =====
        var endRoot = CreateRect("End Panel", canvasGO.transform,
                                 new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                 Vector2.zero, Vector2.zero, new Vector2(700, 420));
        var endBg = endRoot.gameObject.AddComponent<Image>();
        endBg.color = new Color(0f, 0f, 0f, 0.65f);

        var endGroup = endRoot.gameObject.AddComponent<CanvasGroup>();
        endGroup.alpha = 0f;
        endGroup.interactable = false;
        endGroup.blocksRaycasts = false;

        // Title + Score placeholders (no text components)
        var endTitleSlot = CreateRect("End Title (DROP TMP HERE)", endRoot,
                                      new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                      Vector2.zero, Vector2.zero, new Vector2(640, 60));
        endTitleSlot.anchoredPosition = new Vector2(0, -30);

        var scoreSlot = CreateRect("Score Text (DROP TMP HERE)", endRoot,
                                   new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                   Vector2.zero, Vector2.zero, new Vector2(640, 50));
        scoreSlot.anchoredPosition = new Vector2(0, -90);

        // Buttons row
        var buttonsRow = CreateRect("Buttons", endRoot,
                                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                    Vector2.zero, Vector2.zero, new Vector2(640, 80));
        buttonsRow.anchoredPosition = new Vector2(0, -170);

        // Retry
        var retryButton = CreateButton("Retry Button", buttonsRow);
        PositionButton(retryButton.GetComponent<RectTransform>(), new Vector2(-220, 0));
        CreateLabelSlot(retryButton.transform); // placeholder for TMP label

        // Next Level
        var nextButton = CreateButton("Next Button", buttonsRow);
        PositionButton(nextButton.GetComponent<RectTransform>(), new Vector2(0, 0));
        CreateLabelSlot(nextButton.transform);

        // Main Menu
        var menuButton = CreateButton("Menu Button", buttonsRow);
        PositionButton(menuButton.GetComponent<RectTransform>(), new Vector2(220, 0));
        CreateLabelSlot(menuButton.transform);

        // Wire buttons to LevelManager (safe; no text required)
        retryButton.onClick.AddListener(levelManager.RestartLevel);
        nextButton.onClick.AddListener(levelManager.GoToNextScene);
        menuButton.onClick.AddListener(() => SceneManager.LoadScene(0));

        // Assign non-text refs back to LevelManager (safe)
        levelManager.endPanel = endGroup;
        levelManager.retryButton = retryButton;
        levelManager.nextButton = nextButton;
        levelManager.menuButton = menuButton;

        Debug.Log("[LevelHUDFactory_NoText] Built UI shell (no text). Drop TMP texts into the slots and drag them into LevelManager afterward.");
    }

    // -------- helpers --------
    T FindOne<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<T>();
#else
#pragma warning disable CS0618
        return Object.FindObjectOfType<T>();
#pragma warning restore CS0618
#endif
    }

    void EnsureEventSystemExists()
    {
#if UNITY_2023_1_OR_NEWER
        var es = Object.FindFirstObjectByType<EventSystem>();
#else
#pragma warning disable CS0618
        var es = Object.FindObjectOfType<EventSystem>();
#pragma warning restore CS0618
#endif
        if (!es)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    RectTransform CreateRect(string name, Transform parent,
                             Vector2 anchorMin, Vector2 anchorMax,
                             Vector2 anchoredMin, Vector2 anchoredMax,
                             Vector2? sizeDelta = null)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2((anchorMin.x + anchorMax.x) * 0.5f, (anchorMin.y + anchorMax.y) * 0.5f);
        rt.anchoredPosition = anchoredMin;
        if (sizeDelta.HasValue) rt.sizeDelta = sizeDelta.Value;
        return rt;
    }

    Button CreateButton(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var img = go.GetComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.12f);

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(180, 60);

        var btn = go.GetComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 0.15f);
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.28f);
        colors.pressedColor = new Color(1f, 1f, 1f, 0.35f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(1f, 1f, 1f, 0.08f);
        btn.colors = colors;

        return btn;
    }

    void CreateLabelSlot(Transform buttonRoot)
    {
        // Empty child where you'll add a TMP_Text and stretch to full
        var slot = new GameObject("Label (DROP TMP HERE)", typeof(RectTransform)).GetComponent<RectTransform>();
        slot.SetParent(buttonRoot, false);
        slot.anchorMin = Vector2.zero;
        slot.anchorMax = Vector2.one;
        slot.offsetMin = Vector2.zero;
        slot.offsetMax = Vector2.zero;
    }

    void PositionButton(RectTransform rt, Vector2 centerOffset)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = centerOffset;
    }

    void PinTopLeft(RectTransform rt, float yOffset)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(20f, -20f - yOffset);
    }
}
