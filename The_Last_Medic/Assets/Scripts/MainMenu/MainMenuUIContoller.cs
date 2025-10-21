using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUIController : MonoBehaviour
{
    [System.Serializable]
    public class Panel
    {
        [Tooltip("Drag the EMPTY root object of the panel (e.g., Canvas/Panels/RulesPanel)")]
        public GameObject root;

        [Tooltip("Optional. If omitted, we'll GetComponent<CanvasGroup>() from root at runtime.")]
        public CanvasGroup canvasGroup;

        [Tooltip("Back button inside this panel")]
        public Button backButton;

        public bool IsOpen => root && root.activeSelf;

        public void EnsureCanvasGroup()
        {
            if (!root) return;
            if (!canvasGroup) canvasGroup = root.GetComponent<CanvasGroup>();
        }
    }

    [Header("Scene")]
    [SerializeField] private string level1SceneName = "Level_1";

    [Header("Core UI")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private Image dimmer;             // Canvas/Dimmer (Image)
    [SerializeField] private CanvasGroup dimmerGroup;  // Canvas/Dimmer (CanvasGroup)
    [SerializeField] private GameObject buttonStack;   // Canvas/ButtonStack
    [SerializeField] private CanvasGroup buttonStackGroup; // Canvas/ButtonStack (CanvasGroup)

    [Header("Title Area (fades with the rest of the UI)")]
    [SerializeField] private CanvasGroup titleAreaGroup;   // Canvas/TitleArea (CanvasGroup)

    [Header("Buttons (inside ButtonStack)")]
    [SerializeField] private Button btnPlay;
    [SerializeField] private Button btnRules;
    [SerializeField] private Button btnBackstory;
    [SerializeField] private Button btnSettings;
    [SerializeField] private Button btnQuit;

    [Header("Panels (drag the ROOT empty objects)")]
    [SerializeField] private Panel rulesPanel;       // root = Canvas/Panels/RulesPanel
    [SerializeField] private Panel backstoryPanel;   // root = Canvas/Panels/BackstoryPanel
    [SerializeField] private Panel settingsPanel;    // root = Canvas/Panels/SettingsPanel

    [Header("Vignette (fades out during camera tween)")]
    [SerializeField] private Image vignette;               // Canvas/Vignette (Image)
    [SerializeField] private CanvasGroup vignetteGroup;    // Canvas/Vignette (CanvasGroup)

    [Header("Camera Tween")]
    [SerializeField] private Transform menuCameraRig;      // parent of menu camera
    [SerializeField] private Transform playerCameraPose;   // target pose (PlayerCamera or an empty)
    [SerializeField, Range(0.1f, 5f)] private float cameraTweenTime = 1.25f;
    [SerializeField]
    private AnimationCurve cameraTweenCurve =
        AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Timings")]
    [SerializeField, Range(0.05f, 2f)] private float panelFadeTime = 0.20f;
    [SerializeField, Range(0.05f, 2f)] private float dimmerFadeTime = 0.35f;
    [SerializeField, Range(0.05f, 1f)] private float buttonsFadeTime = 0.20f;

    [Header("Input")]
    [SerializeField] private EventSystem eventSystem;
    [SerializeField] private Selectable firstSelected; // usually Btn_Play

    bool IsAnyPanelOpen =>
        (rulesPanel != null && rulesPanel.IsOpen) ||
        (backstoryPanel != null && backstoryPanel.IsOpen) ||
        (settingsPanel != null && settingsPanel.IsOpen);

    void Awake()
    {
        if (!canvas) canvas = GetComponentInParent<Canvas>();
        if (!dimmerGroup && dimmer) dimmerGroup = dimmer.GetComponent<CanvasGroup>();

        // Try to auto-find TitleArea group if not wired
        if (!titleAreaGroup && canvas)
        {
            var t = canvas.transform.Find("TitleArea");
            if (t) titleAreaGroup = t.GetComponent<CanvasGroup>();
        }

        rulesPanel?.EnsureCanvasGroup();
        backstoryPanel?.EnsureCanvasGroup();
        settingsPanel?.EnsureCanvasGroup();
    }

    void Start()
    {
        // Base state
        SetDimmer(0f, raycast: false);
        if (buttonStackGroup) buttonStackGroup.alpha = 1f;
        if (titleAreaGroup) titleAreaGroup.alpha = 1f;
        ShowButtonStack(true);

        HidePanelImmediate(rulesPanel);
        HidePanelImmediate(backstoryPanel);
        HidePanelImmediate(settingsPanel);

        // Wire main buttons
        btnPlay.onClick.AddListener(OnPlay);
        btnRules.onClick.AddListener(() => OpenPanel(rulesPanel));
        btnBackstory.onClick.AddListener(() => OpenPanel(backstoryPanel));
        btnSettings.onClick.AddListener(() => OpenPanel(settingsPanel));
        btnQuit.onClick.AddListener(OnQuit);

        // Wire back buttons -> always return home
        if (rulesPanel?.backButton) rulesPanel.backButton.onClick.AddListener(GoHome);
        if (backstoryPanel?.backButton) backstoryPanel.backButton.onClick.AddListener(GoHome);
        if (settingsPanel?.backButton) settingsPanel.backButton.onClick.AddListener(GoHome);

        // First-selected
        var starter = firstSelected ? firstSelected : (Selectable)btnPlay;
        if (eventSystem && starter)
        {
            eventSystem.firstSelectedGameObject = starter.gameObject;
            starter.Select();
        }
    }

    void Update()
    {
        // ESC -> always go back to home if any panel is open
        if (Input.GetKeyDown(KeyCode.Escape) && IsAnyPanelOpen)
        {
            GoHome();
        }
    }

    // ---------- Actions ----------

    void OnPlay()
    {
        // Stop opening any panels mid-flight
        StopAllCoroutines();

        // Hide panels if any are open
        HidePanelImmediate(rulesPanel);
        HidePanelImmediate(backstoryPanel);
        HidePanelImmediate(settingsPanel);

        // Start the cinematic: fade buttons + title -> fade vignette -> tween camera -> black -> load
        StartCoroutine(PlaySequence());
    }

    void OnQuit()
    {
#if UNITY_EDITOR
        Debug.Log("[MainMenu] Quit requested (Editor).");
#else
        Application.Quit();
#endif
    }

    void OpenPanel(Panel p)
    {
        if (p == null || p.root == null) return;

        ShowButtonStack(false);

        p.root.SetActive(true);
        if (p.canvasGroup)
        {
            StartCoroutine(FadeCanvasGroup(p.canvasGroup, 0f, 1f, panelFadeTime));
        }

        // Dim background slightly while panel is open
        SetDimmer(0.35f, raycast: true);

        // Focus back button if set
        if (eventSystem && p.backButton)
            eventSystem.SetSelectedGameObject(p.backButton.gameObject);
    }

    public void GoHome()
    {
        StopAllCoroutines();

        HidePanelImmediate(rulesPanel);
        HidePanelImmediate(backstoryPanel);
        HidePanelImmediate(settingsPanel);

        SetDimmer(0f, raycast: false);
        ShowButtonStack(true);

        if (buttonStackGroup) buttonStackGroup.alpha = 1f;
        if (titleAreaGroup) titleAreaGroup.alpha = 1f;

        if (eventSystem && btnPlay)
            eventSystem.SetSelectedGameObject(btnPlay.gameObject);
    }

    // ---------- Play sequence ----------

    IEnumerator PlaySequence()
    {
        // 1) Fade out the whole button stack
        if (buttonStackGroup)
            yield return FadeCanvasGroup(buttonStackGroup, 1f, 0f, buttonsFadeTime);
        ShowButtonStack(false);

        // 1b) Fade out the TitleArea as part of the UI fade
        if (titleAreaGroup)
            yield return FadeCanvasGroup(titleAreaGroup, 1f, 0f, buttonsFadeTime);

        // 2) Fade out the vignette next
        if (vignetteGroup)
            yield return FadeCanvasGroup(vignetteGroup, vignetteGroup.alpha, 0f, 0.20f);

        // 3) Disable parallax during the move (if present)
        var parallax = menuCameraRig ? menuCameraRig.GetComponent<MenuCameraParallax>() : null;
        if (parallax) parallax.SetEnabled(false);

        // 4) Tween the MenuCameraRig to the player camera pose (ease-in/out “velocity” feel)
        if (menuCameraRig && playerCameraPose)
            yield return TweenRig(menuCameraRig, playerCameraPose, cameraTweenTime, cameraTweenCurve);

        // 5) Final quick fade to black, then load scene
        yield return FadeDimmer(1f, dimmerFadeTime, raycast: true);

        var op = SceneManager.LoadSceneAsync(level1SceneName, LoadSceneMode.Single);
        yield return op;
    }

    IEnumerator TweenRig(Transform rig, Transform target, float duration, AnimationCurve curve)
    {
        Vector3 p0 = rig.position;
        Quaternion r0 = rig.rotation;
        Vector3 p1 = target.position;
        Quaternion r1 = target.rotation;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / duration);

            // Smooth, velocity-like ease while respecting your custom curve
            float raw = curve != null ? curve.Evaluate(u) : u;
            float k = Mathf.SmoothStep(0f, 1f, raw);

            rig.position = Vector3.LerpUnclamped(p0, p1, k);
            rig.rotation = Quaternion.SlerpUnclamped(r0, r1, k);
            yield return null;
        }

        rig.position = p1;
        rig.rotation = r1;
    }

    // ---------- Helpers ----------

    void ShowButtonStack(bool show)
    {
        if (buttonStack) buttonStack.SetActive(show);
    }

    void HidePanelImmediate(Panel p)
    {
        if (p == null || p.root == null) return;

        if (p.canvasGroup) p.canvasGroup.alpha = 0f;
        p.root.SetActive(false);
        if (p.canvasGroup)
        {
            p.canvasGroup.interactable = false;
            p.canvasGroup.blocksRaycasts = false;
        }
    }

    void SetDimmer(float targetAlpha, bool raycast)
    {
        if (!dimmerGroup) return;
        dimmerGroup.alpha = targetAlpha;
        if (dimmer) dimmer.raycastTarget = raycast;
    }

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        if (!cg) yield break;

        cg.alpha = from;
        cg.interactable = false;
        cg.blocksRaycasts = true;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        cg.alpha = to;

        bool visible = (to >= 0.99f);
        cg.interactable = visible;
        cg.blocksRaycasts = visible;
    }

    IEnumerator FadeDimmer(float to, float duration, bool raycast)
    {
        if (!dimmerGroup) yield break;
        float from = dimmerGroup.alpha;
        if (dimmer) dimmer.raycastTarget = raycast;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            dimmerGroup.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        dimmerGroup.alpha = to;

        if (to <= 0.001f && dimmer) dimmer.raycastTarget = false;
    }
}
