using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.FPS.Gameplay; // ADSController
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

public class MenuPauseRuntime : MonoBehaviour
{
    [Header("Optional")]
    public Camera MenuCamera;
    public Camera PlayerCamera;
    public GameObject PlayerRoot;
    public GameObject Plr;

    [Header("Timings")]
    public float ButtonsFadeTime = 0.25f;
    public float BgFadeTime = 0.25f;
    public float CameraTweenTime = 0.45f;
    public AnimationCurve TweenCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Sensitivity Ranges")]
    public float HipMin = 0.1f, HipMax = 5f, ADSMin = 0.1f, ADSMax = 1.5f;

    const string PP_HIP = "sens_hip";
    const string PP_ADS = "sens_ads_scale";

    // UI refs
    Canvas _canvas;
    CanvasGroup _mainButtonsGroup;
    CanvasGroup _blackBG;
    CanvasGroup _pauseGroup;
    GameObject _rulesPanel;
    GameObject _backStoryPanel;

    // Buttons
    Button _btnPlay, _btnRules, _btnBackStory;
    Button _btnPauseResume, _btnPauseRules, _btnPauseSkip, _btnPauseMain;

    // Sensitivity UI
    Slider _hipSlider, _adsSlider;
    Text _hipLabel, _adsLabel;

    // Audio
    AudioListener _menuAL, _playerAL;

    // Sensitivity binding
    Component _lookComponent;
    FieldInfo _sensField;
    PropertyInfo _sensProp;
    float _hipDefault = 1f;
    ADSController _ads;

    bool _playing, _paused, _subpanelOpen;
    enum HostMenu { None, Main, Pause }
    HostMenu _subpanelHost = HostMenu.None;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        EnsureEventSystem();
        EnsureCameras();
        BuildCanvasAndUI();
        BindSensitivityTargets();

        // Initial state
        ShowMainButtons(true);
        SetGroup(_blackBG, 0.6f, false);
        _rulesPanel.SetActive(false);
        _backStoryPanel.SetActive(false);
        SetPauseVisible(false);

        ResolvePlayerRoot();
        SetPlayerChildrenEnabled(PlayerRoot, false);
        SetCursorForUI(true);

        // Load saved sens defaults
        var hip = PlayerPrefs.GetFloat(PP_HIP, Mathf.Clamp(_hipDefault, HipMin, HipMax));
        var ads = PlayerPrefs.GetFloat(PP_ADS, 0.5f);
        ApplyHip(hip, false);
        ApplyADS(ads, false);
    }

    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        EnsureEventSystem();
        EnsureCameras();
        ResolvePlayerRoot();

        if (_playing && !_paused)
        {
            SetPlayerChildrenEnabled(PlayerRoot, true);
            SetCursorForUI(false);
        }
        else
        {
            SetPlayerChildrenEnabled(PlayerRoot, false);
            SetCursorForUI(true);
        }
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && _playing) TogglePause();
#else
        if (Input.GetKeyDown(KeyCode.Escape) && _playing) TogglePause();
#endif
    }

    // ---------- Setup helpers ----------

    void EnsureEventSystem()
    {
        if (!FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include))
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(es);
        }
    }

    void EnsureCameras()
    {
        if (!PlayerCamera)
            foreach (var c in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (!c.enabled) { PlayerCamera = c; break; }
        if (!MenuCamera)
            foreach (var c in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (c.enabled) { MenuCamera = c; break; }

        if (PlayerCamera) PlayerCamera.enabled = false;
        if (MenuCamera) MenuCamera.enabled = true;

        _menuAL = MenuCamera ? MenuCamera.GetComponent<AudioListener>() : null;
        _playerAL = PlayerCamera ? PlayerCamera.GetComponent<AudioListener>() : null;
        if (_menuAL) _menuAL.enabled = true;
        if (_playerAL) _playerAL.enabled = false;
    }

    void ResolvePlayerRoot()
    {
        if (Plr) { PlayerRoot = Plr; return; }
        if (PlayerRoot) return;

        var tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged) { PlayerRoot = tagged; return; }

        if (PlayerCamera) PlayerRoot = PlayerCamera.transform.root.gameObject;
        else if (MenuCamera) PlayerRoot = MenuCamera.transform.root.gameObject;
    }

    void SetPlayerChildrenEnabled(GameObject root, bool enabled)
    {
        if (!root) return;
        foreach (var b in root.GetComponentsInChildren<Behaviour>(true))
        {
            if (!b || b is Camera || b is AudioListener || b is Animator) continue;
            b.enabled = enabled;
        }
    }

    // ---------- Build UI ----------

    void BuildCanvasAndUI()
    {
        _canvas = CreateCanvas("RuntimeCanvas", out var scaler);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        DontDestroyOnLoad(_canvas.gameObject);

        // BG
        _blackBG = CreatePanel("BlackBG", new Color(0, 0, 0, 0.6f), true);

        // Main buttons group — CENTERED
        _mainButtonsGroup = CreatePanel("MainButtons", new Color(0, 0, 0, 0f), false,
                                        new Vector2(520, 360), new Vector2(0.5f, 0.5f)); // center
        _btnPlay = CreateButton(_mainButtonsGroup.transform, "Play", new Vector2(0, 80), OnClick_Play);
        _btnRules = CreateButton(_mainButtonsGroup.transform, "Rules", new Vector2(0, 0), OnClick_Rules);
        _btnBackStory = CreateButton(_mainButtonsGroup.transform, "Back Story", new Vector2(0, -80), OnClick_BackStory);

        // Rules panel — TALLER
        _rulesPanel = CreatePanelGO("RulesPanel", new Color(0, 0, 0, 0.35f),
                                    new Vector2(1100, 820),
                                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        _rulesPanel.transform.SetParent(_canvas.transform, false);
        _rulesPanel.SetActive(false);

        float y = 320f;
        CreateHeaderText(_rulesPanel.transform, "Rules", new Vector2(0, y), 30);
        y -= 60f;
        CreateBodyText(_rulesPanel.transform,
            "You play as Medic Sniper capable of reviving fallen allies when shooting them. Living allies can be ordered to Attack or Stand Down. " +
            "Allies can die again, but can also be revived twice more. Zombies will patrol around unless they spot an Ally, in which case they will chase and attack. " +
            "Be careful: some zombies are stronger than usual and should be dealt with carefully.\n\n" +

            "Around the town there are bells, bombs, and barrels which can be used to even the odds. Bells can distract nearby zombies but have a cooldown when used. " +
            "Bombs and exploding barrels deal significant damage to anything nearby, including Allies!\n\n" +

            "You will have a time limit of 10 mins to complete each level. You will also gain score for killing zombies and lose score for every Ally's death you're " +
            "responsible for. Your immediate win condition is to eliminate all zombies, but to achieve mastery you're aiming to complete levels with maximum score and time left over.",
            new Vector2(0, y), 20, 980, 380);

        y -= 400f;
        CreateHeaderText(_rulesPanel.transform, "Controls", new Vector2(0, y), 30);
        y -= 54f;
        CreateBodyText(_rulesPanel.transform,
            "WASD - Move\n" +
            "Left Click - Shoot\n" +
            "Right Click - Aiming (ADS)\n" +
            "E - order living Allies to Attack or Stand Down\n" +
            "ESC - Pause menu",
            new Vector2(0, y), 20, 980, 140);

        y -= 160f;
        _hipSlider = CreateSlider(_rulesPanel.transform, "Hip Sensitivity", HipMin, HipMax, new Vector2(0, y), OnHipSliderChanged, out _hipLabel);
        y -= 80f;
        _adsSlider = CreateSlider(_rulesPanel.transform, "ADS Scale", ADSMin, ADSMax, new Vector2(0, y), OnADSSliderChanged, out _adsLabel);

        y -= 90f;
        CreateButton(_rulesPanel.transform, "Close", new Vector2(0, y), CloseActiveSubpanel);

        // Back Story panel — TALLER
        _backStoryPanel = CreatePanelGO("BackStoryPanel", new Color(0, 0, 0, 0.35f),
                                        new Vector2(1100, 820),
                                        new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        _backStoryPanel.transform.SetParent(_canvas.transform, false);
        _backStoryPanel.SetActive(false);

        float y2 = 320f;
        CreateHeaderText(_backStoryPanel.transform, "Backstory", new Vector2(0, y2), 30);
        y2 -= 60f;
        CreateBodyText(_backStoryPanel.transform,
            "When negotiations fail, they send the troops. When the troops fail, they send you. You are a sniper medic, a marksman who's taken the Hippocratic Oath. " +
            "You shoot to heal, not kill. Your allies have lost the battlefield, but they've not lost hope. Not as long as you still stand; The Last Medic.\n\n" +

            "Zombies have overrun a nearby village and boots on the ground have failed to recover it. The presence of newer, stranger strains of zombies made standard rescue " +
            "operations too risky. Your mission is to resurrect your fallen Allies in order to strategically ambush and eradicate any zombie presence in the area.",
            new Vector2(0, y2), 20, 980, 520);

        y2 -= 540f;
        CreateButton(_backStoryPanel.transform, "Close", new Vector2(0, y2), CloseActiveSubpanel);

        // Pause menu — a bit taller
        _pauseGroup = CreatePanel("PauseMenu", new Color(0, 0, 0, 0.35f), false,
                                  new Vector2(620, 460), new Vector2(0.5f, 0.5f));
        SetPauseVisible(false);
        _btnPauseResume = CreateButton(_pauseGroup.transform, "Resume", new Vector2(0, 120), Resume);
        _btnPauseRules = CreateButton(_pauseGroup.transform, "Rules", new Vector2(0, 60), () => OpenSubpanel(_rulesPanel, HostMenu.Pause));
        _btnPauseSkip = CreateButton(_pauseGroup.transform, "Skip Level", new Vector2(0, 0), SkipLevel);
        _btnPauseMain = CreateButton(_pauseGroup.transform, "Back to Menu", new Vector2(0, -60), BackToMainMenu);
    }

    // ---------- Button logic ----------

    void OnClick_Play()
    {
        if (_playing) return;
        StartCoroutine(PlaySequence());

    }
    void OnClick_Rules() => OpenSubpanel(_rulesPanel, HostMenu.Main);
    void OnClick_BackStory() => OpenSubpanel(_backStoryPanel, HostMenu.Main);

    void OpenSubpanel(GameObject panel, HostMenu host)
    {
        if (!panel) return;
        _subpanelHost = host; _subpanelOpen = true;

        if (host == HostMenu.Main)
            ShowMainButtons(false);
        if (host == HostMenu.Pause)
            SetGroup(_pauseGroup, 0, false);

        panel.SetActive(true);
    }

    void CloseActiveSubpanel()
    {
        if (_rulesPanel) _rulesPanel.SetActive(false);
        if (_backStoryPanel) _backStoryPanel.SetActive(false);
        if (_subpanelHost == HostMenu.Main) ShowMainButtons(true);
        if (_subpanelHost == HostMenu.Pause) SetGroup(_pauseGroup, 1, true);
        _subpanelOpen = false;
        _subpanelHost = HostMenu.None;
    }

    // ---------- Play/Pause flow ----------

    IEnumerator PlaySequence()
    {
        _playing = true;
        yield return FadeCanvas(_mainButtonsGroup, 1, 0, ButtonsFadeTime);
        yield return FadeCanvas(_blackBG, _blackBG.alpha, 0, BgFadeTime);

        if (MenuCamera && PlayerCamera)
        {
            PlayerCamera.enabled = false; if (_playerAL) _playerAL.enabled = false;
            SetCursorForUI(false);
            yield return TweenCamera(MenuCamera.transform, PlayerCamera.transform, CameraTweenTime, TweenCurve);
            MenuCamera.enabled = false; if (_menuAL) _menuAL.enabled = false;
            PlayerCamera.enabled = true; if (_playerAL) _playerAL.enabled = true;
        }

        SetPlayerChildrenEnabled(PlayerRoot, true);
        _rulesPanel.SetActive(false);
        _backStoryPanel.SetActive(false);
        ShowMainButtons(false);
        _blackBG.gameObject.SetActive(false);
    }

    void TogglePause() { if (_paused) Resume(); else Pause(); }

    void Pause()
    {
        _paused = true;
        Time.timeScale = 0f;
        _blackBG.gameObject.SetActive(true);
        SetGroup(_blackBG, 0.6f, false); // ★ CHANGED: don't block raycasts so pause/main buttons remain clickable
        ShowMainButtons(false);
        SetPauseVisible(true);
        SetPlayerChildrenEnabled(PlayerRoot, false);
        SetCursorForUI(true);
    }

    void Resume()
    {
        if (_subpanelOpen) CloseActiveSubpanel();
        _paused = false;
        Time.timeScale = 1f;
        SetPauseVisible(false);
        SetPlayerChildrenEnabled(PlayerRoot, true);
        SetCursorForUI(false);
        _blackBG.gameObject.SetActive(false);
    }

    void SetPauseVisible(bool vis)
    {
        SetGroup(_pauseGroup, vis ? 1f : 0f, vis);
        _pauseGroup.gameObject.SetActive(vis);
    }

    void SkipLevel()
    {
        int i = SceneManager.GetActiveScene().buildIndex;
        Time.timeScale = 1f;
        SceneManager.LoadScene(i + 1);
    }

    void BackToMainMenu()
    {
        if (_subpanelOpen) CloseActiveSubpanel();
        SetPauseVisible(false);
        SetGroup(_pauseGroup, 0f, false);

        Time.timeScale = 1f;
        SceneManager.LoadScene(0);

        _playing = false;
        _paused = false;

        _blackBG.gameObject.SetActive(true);
        SetGroup(_blackBG, 0.6f, false);

        ShowMainButtons(true);
        SetCursorForUI(true);

        SetPlayerChildrenEnabled(PlayerRoot, false);
    }

    // ---------- Sensitivity ----------

    void BindSensitivityTargets()
    {
        foreach (var c in FindObjectsByType<Component>(FindObjectsSortMode.None))
        {
            if (!c) continue;
            var t = c.GetType();
            _sensField = t.GetField("LookSensitivity") ?? t.GetField("lookSensitivity") ??
                         t.GetField("MouseSensitivity") ?? t.GetField("mouseSensitivity");
            _sensProp = t.GetProperty("LookSensitivity") ?? t.GetProperty("MouseSensitivity");
            if (_sensField != null || _sensProp != null) { _lookComponent = c; _hipDefault = ReadSensitivity(); break; }
        }
        _ads = FindAnyObjectByType<ADSController>(FindObjectsInactive.Include);
        UpdateSensLabels();
    }

    void OnHipSliderChanged(float v) { ApplyHip(v, false); PlayerPrefs.SetFloat(PP_HIP, v); UpdateSensLabels(); }
    void OnADSSliderChanged(float v) { ApplyADS(v, false); PlayerPrefs.SetFloat(PP_ADS, v); UpdateSensLabels(); }

    void ApplyHip(float v, bool updateSlider)
    {
        v = Mathf.Clamp(v, HipMin, HipMax);
        if (_lookComponent != null) { try { _sensField?.SetValue(_lookComponent, v); _sensProp?.SetValue(_lookComponent, v); } catch { } }
        if (updateSlider && _hipSlider) _hipSlider.SetValueWithoutNotify(v);
    }

    void ApplyADS(float v, bool updateSlider)
    {
        v = Mathf.Clamp(v, ADSMin, ADSMax);
        if (_ads != null) _ads.AimSensitivityScale = v;
        if (updateSlider && _adsSlider) _adsSlider.SetValueWithoutNotify(v);
    }

    float ReadSensitivity()
    {
        try
        {
            if (_sensField != null) return ToFloat(_sensField.GetValue(_lookComponent));
            if (_sensProp != null) return ToFloat(_sensProp.GetValue(_lookComponent));
        }
        catch { }
        return 1f;
    }

    static float ToFloat(object v)
    {
        if (v == null) return 1f;
        if (v is float f) return f;
        if (v is double d) return (float)d;
        if (v is int i) return i;
        float.TryParse(v.ToString(), out var p);
        return p <= 0 ? 1f : p;
    }

    void UpdateSensLabels()
    {
        if (_hipLabel && _hipSlider) _hipLabel.text = $"Hip Sensitivity: {_hipSlider.value:0.00}";
        if (_adsLabel && _adsSlider) _adsLabel.text = $"ADS Scale: {_adsSlider.value:0.00}×";
    }

    // ---------- UI builders ----------

    Canvas CreateCanvas(string name, out CanvasScaler scaler)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var c = go.GetComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay;
        scaler = go.GetComponent<CanvasScaler>();
        return c;
    }

    CanvasGroup CreatePanel(string name, Color bg, bool stretch, Vector2? size = null, Vector2? anchor = null)
    {
        var go = CreatePanelGO(name, bg, size, anchor ?? new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        go.transform.SetParent(_canvas.transform, false);
        var rt = go.GetComponent<RectTransform>();
        if (stretch) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }
        var cg = go.AddComponent<CanvasGroup>();
        return cg;
    }

    GameObject CreatePanelGO(string name, Color bg, Vector2? size, Vector2 anchor, Vector2 pivot)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size ?? new Vector2(600, 400);
        rt.anchorMin = rt.anchorMax = anchor; rt.pivot = pivot;
        go.GetComponent<Image>().color = bg;
        return go;
    }

    Button CreateButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(320, 56); rt.anchoredPosition = pos;
        var img = go.GetComponent<Image>(); img.color = new Color(1, 1, 1, 0.92f);
        var btn = go.GetComponent<Button>(); btn.targetGraphic = img; btn.onClick.AddListener(onClick);

        var txtGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
        txtGO.transform.SetParent(go.transform, false);
        var trt = txtGO.GetComponent<RectTransform>(); trt.sizeDelta = new Vector2(300, 40);
        var t = txtGO.GetComponent<Text>();
        t.text = label; t.alignment = TextAnchor.MiddleCenter;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 22; t.color = Color.black; t.resizeTextForBestFit = true;

        return btn;
    }

    void CreateHeaderText(Transform parent, string text, Vector2 pos, int size = 28)
    {
        var go = new GameObject("Header", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(980, 50); rt.anchoredPosition = pos;
        var tx = go.GetComponent<Text>();
        tx.text = text; tx.alignment = TextAnchor.MiddleCenter;
        tx.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tx.fontSize = size; tx.color = Color.white;
    }

    void CreateBodyText(Transform parent, string text, Vector2 pos, int size = 20, float w = 980, float h = 120)
    {
        var go = new GameObject("Body", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(w, h); rt.anchoredPosition = pos;
        var tx = go.GetComponent<Text>();
        tx.text = text; tx.alignment = TextAnchor.UpperCenter;
        tx.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tx.fontSize = size; tx.color = Color.white;
        tx.horizontalOverflow = HorizontalWrapMode.Wrap;
        tx.verticalOverflow = VerticalWrapMode.Overflow;
    }

    Slider CreateSlider(Transform parent, string label, float min, float max, Vector2 pos,
                        UnityEngine.Events.UnityAction<float> onChanged, out Text valueLabel)
    {
        // Title
        var labelGO = new GameObject(label + "Label", typeof(RectTransform), typeof(Text));
        labelGO.transform.SetParent(parent, false);
        var lrt = labelGO.GetComponent<RectTransform>();
        lrt.sizeDelta = new Vector2(980, 24); lrt.anchoredPosition = pos + new Vector2(0, 28);
        var lt = labelGO.GetComponent<Text>();
        lt.text = label; lt.alignment = TextAnchor.MiddleCenter;
        lt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        lt.fontSize = 18; lt.color = Color.white;

        // Slider root
        var sGO = new GameObject(label + "Slider", typeof(RectTransform), typeof(Slider));
        sGO.transform.SetParent(parent, false);
        var srt = sGO.GetComponent<RectTransform>();
        srt.sizeDelta = new Vector2(720, 24); srt.anchoredPosition = pos;
        var slider = sGO.GetComponent<Slider>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = min; slider.maxValue = max; slider.wholeNumbers = false;

        // Background (thin)
        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(sGO.transform, false);
        var bgrt = bg.GetComponent<RectTransform>();
        bgrt.anchorMin = new Vector2(0, 0.4f); bgrt.anchorMax = new Vector2(1, 0.6f);
        bgrt.offsetMin = bgrt.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(1, 1, 1, 0.25f);

        // Fill (same rect)
        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(sGO.transform, false);
        var frt = fill.GetComponent<RectTransform>();
        frt.anchorMin = new Vector2(0, 0.4f); frt.anchorMax = new Vector2(1, 0.6f);
        frt.offsetMin = frt.offsetMax = Vector2.zero;
        fill.GetComponent<Image>().color = new Color(1, 1, 1, 0.8f);

        // Handle (smaller)
        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(sGO.transform, false);
        var hrt = handle.GetComponent<RectTransform>();
        hrt.sizeDelta = new Vector2(18, 18); hrt.anchorMin = hrt.anchorMax = new Vector2(0, 0.5f);
        handle.GetComponent<Image>().color = Color.white;

        slider.fillRect = frt;
        slider.handleRect = hrt;
        slider.targetGraphic = handle.GetComponent<Image>();

        // Value text
        var val = new GameObject(label + "Value", typeof(RectTransform), typeof(Text));
        val.transform.SetParent(parent, false);
        var vrt = val.GetComponent<RectTransform>();
        vrt.sizeDelta = new Vector2(980, 18); vrt.anchoredPosition = pos + new Vector2(0, -24);
        var vt = val.GetComponent<Text>();
        vt.text = ""; vt.alignment = TextAnchor.MiddleCenter;
        vt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        vt.fontSize = 16; vt.color = Color.white;
        valueLabel = vt;

        float start = label.Contains("Hip")
            ? PlayerPrefs.GetFloat(PP_HIP, Mathf.Clamp(_hipDefault, HipMin, HipMax))
            : PlayerPrefs.GetFloat(PP_ADS, 0.5f);
        slider.SetValueWithoutNotify(start);
        slider.onValueChanged.AddListener(onChanged);
        onChanged?.Invoke(start);

        return slider;
    }

    // ---------- misc helpers ----------

    static void SetGroup(CanvasGroup cg, float alpha, bool interactable)
    {
        if (!cg) return;
        cg.alpha = alpha; cg.interactable = interactable; cg.blocksRaycasts = interactable;
    }

    void ShowMainButtons(bool show)
    {
        if (!_mainButtonsGroup) return;
        _mainButtonsGroup.gameObject.SetActive(show);
        SetGroup(_mainButtonsGroup, show ? 1f : 0f, show);
    }

    void SetCursorForUI(bool uiMode)
    {
        Cursor.lockState = uiMode ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = uiMode;
    }

    IEnumerator FadeCanvas(CanvasGroup cg, float from, float to, float time)
    {
        if (!cg) yield break;
        float t = 0f; cg.interactable = false; cg.blocksRaycasts = false;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / time);
            yield return null;
        }
        cg.alpha = to;
    }

    IEnumerator TweenCamera(Transform from, Transform to, float time, AnimationCurve curve)
    {
        if (!from || !to) yield break;
        Vector3 p0 = from.position; Quaternion r0 = from.rotation;
        Vector3 p1 = to.position; Quaternion r1 = to.rotation;
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / time);
            float s = curve != null ? curve.Evaluate(k) : k;
            from.position = Vector3.Lerp(p0, p1, s);
            from.rotation = Quaternion.Slerp(r0, r1, s);
            yield return null;
        }
        from.position = p1; from.rotation = r1;
    }
}
