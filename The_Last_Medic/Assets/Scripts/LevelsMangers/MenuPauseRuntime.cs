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
    [Header("Optional: assign if you have them in-scene")]
    public Camera MenuCamera;       // Enabled at start; nice menu shot
    public Camera PlayerCamera;     // Disabled at start; on your player rig
    public GameObject PlayerRoot;   // Optional; will be auto-filled if null
    public GameObject Plr;          // Optional shortcut; if set, we use this as PlayerRoot

    [Header("Timings")]
    public float ButtonsFadeTime = 0.25f;
    public float BgFadeTime = 0.25f;
    public float CameraTweenTime = 0.45f;
    public AnimationCurve TweenCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Sensitivity Ranges")]
    public float HipMin = 0.1f;
    public float HipMax = 5f;
    public float ADSMin = 0.1f;
    public float ADSMax = 1.5f;

    const string PP_HIP = "sens_hip";
    const string PP_ADS = "sens_ads_scale";

    // UI refs created at runtime
    Canvas _canvas;
    CanvasGroup _mainButtonsGroup;
    CanvasGroup _blackBG;
    GameObject _rulesPanel;
    GameObject _backStoryPanel;
    CanvasGroup _pauseGroup;

    // Buttons
    Button _btnPlay, _btnRules, _btnBackStory;
    Button _btnPauseResume, _btnPauseRules, _btnPauseSkip, _btnPauseMain;

    // Sensitivity UI
    Slider _hipSlider, _adsSlider;
    Text _hipLabel, _adsLabel;

    // Camera/audio
    AudioListener _menuAL, _playerAL;

    // Sensitivity binding
    Component _lookComponent;
    FieldInfo _sensField;
    PropertyInfo _sensProp;
    float _hipDefault = 1f;
    ADSController _ads;

    bool _playing;
    bool _paused;

    void Awake()
    {
        EnsureEventSystem();
        EnsureCameras();
        BuildCanvasAndUI();
        BindSensitivityTargets();

        // Initialize UI state
        SetGroup(_mainButtonsGroup, 1f, true);
        SetGroup(_blackBG, 0.6f, false); // don't block clicks
        var bgImg = _blackBG ? _blackBG.GetComponent<Image>() : null;
        if (bgImg) bgImg.raycastTarget = false;

        _rulesPanel.SetActive(false);
        _backStoryPanel.SetActive(false);
        SetPauseVisible(false);

        // Load saved sens
        var hip = PlayerPrefs.GetFloat(PP_HIP, Mathf.Clamp(_hipDefault, HipMin, HipMax));
        var ads = PlayerPrefs.GetFloat(PP_ADS, 0.5f);
        ApplyHip(hip, updateSlider: false);
        ApplyADS(ads, updateSlider: false);

        // Find the player root once, cleanly
        ResolvePlayerRoot();

        // Freeze gameplay until Play (disable all scripts on player hierarchy)
        SetPlayerChildrenEnabled(PlayerRoot, false);
        SetCursorForUI(true);
    }

    void Update()
    {
        bool escPressed = false;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        escPressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        escPressed = Input.GetKeyDown(KeyCode.Escape);
#endif
        if (escPressed && _playing) TogglePause();
    }

    // ---------- Player resolve & enable/disable ----------

    void ResolvePlayerRoot()
    {
        // Priority: Plr field → PlayerRoot field → Player tag → PlayerCamera root → MenuCamera root
        if (Plr) { PlayerRoot = Plr; return; }
        if (PlayerRoot) return;

        var tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged) { PlayerRoot = tagged; return; }

        if (PlayerCamera) { PlayerRoot = PlayerCamera.transform.root.gameObject; return; }
        if (MenuCamera) { PlayerRoot = MenuCamera.transform.root.gameObject; }
    }

    void SetPlayerChildrenEnabled(GameObject root, bool enabled)
    {
        if (!root) return;

        var behaviours = root.GetComponentsInChildren<Behaviour>(true);
        foreach (var b in behaviours)
        {
            if (!b) continue;

            // keep visuals/audio/animators on so menus/cameras still work
            if (b is Camera || b is AudioListener || b is Animator)
                continue;

            b.enabled = enabled;
        }
    }

    // ---------- Bootstrapping ----------

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
        {
            var cams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var c in cams) { if (!c.enabled) { PlayerCamera = c; break; } }
        }
        if (!MenuCamera)
        {
            var cams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var c in cams) { if (c.enabled) { MenuCamera = c; break; } }
        }

        if (PlayerCamera) PlayerCamera.enabled = false;
        if (MenuCamera) MenuCamera.enabled = true;

        _menuAL = MenuCamera ? MenuCamera.GetComponent<AudioListener>() : null;
        _playerAL = PlayerCamera ? PlayerCamera.GetComponent<AudioListener>() : null;
        if (_menuAL) _menuAL.enabled = true;
        if (_playerAL) _playerAL.enabled = false;
    }

    void BuildCanvasAndUI()
    {
        _canvas = CreateCanvas("RuntimeCanvas", out var scaler);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Black BG (semi-transparent)
        _blackBG = CreatePanel("BlackBG", new Color(0, 0, 0, 0.6f), stretch: true);
        // Main buttons group
        _mainButtonsGroup = CreatePanel("MainButtons", new Color(0, 0, 0, 0f), stretch: false,
                                        size: new Vector2(400, 300), anchor: new Vector2(0.5f, 0.6f));
        // Buttons
        _btnPlay = CreateButton(_mainButtonsGroup.transform, "Play", new Vector2(0, 80), OnClick_Play);
        _btnRules = CreateButton(_mainButtonsGroup.transform, "Rules", new Vector2(0, 0), OnClick_Rules);
        _btnBackStory = CreateButton(_mainButtonsGroup.transform, "Back Story", new Vector2(0, -80), OnClick_BackStory);

        // Rules panel with close & sliders
        _rulesPanel = CreatePanelGO("RulesPanel", new Color(0, 0, 0, 0.35f), size: new Vector2(800, 520),
                                    anchor: new Vector2(0.5f, 0.5f), pivot: new Vector2(0.5f, 0.5f));
        _rulesPanel.transform.SetParent(_canvas.transform, false);
        _rulesPanel.SetActive(false);

        CreateHeaderText(_rulesPanel.transform, "How the Game Works", new Vector2(0, 200));
        CreateBodyText(_rulesPanel.transform,
            "Survive, complete objectives, and avoid getting overwhelmed.\n" +
            "Enemies react to sound (bell), ranged mages fire orbs, allies can fight.", new Vector2(0, 140));

        CreateHeaderText(_rulesPanel.transform, "Controls", new Vector2(0, 70));
        CreateBodyText(_rulesPanel.transform,
            "WASD = Move | Space = Jump | Shift = Sprint\n" +
            "Mouse1 = Fire | Mouse2 = ADS | R = Reload\n" +
            "E = Interact | Esc = Pause", new Vector2(0, 30));

        // Sliders
        _hipSlider = CreateSlider(_rulesPanel.transform, "Hip Sensitivity", HipMin, HipMax, new Vector2(0, -60),
                                  OnHipSliderChanged, out _hipLabel);
        _adsSlider = CreateSlider(_rulesPanel.transform, "ADS Scale", ADSMin, ADSMax, new Vector2(0, -140),
                                  OnADSSliderChanged, out _adsLabel);

        CreateButton(_rulesPanel.transform, "Close", new Vector2(0, -210), () => _rulesPanel.SetActive(false));

        // Back Story panel
        _backStoryPanel = CreatePanelGO("BackStoryPanel", new Color(0, 0, 0, 0.35f), size: new Vector2(800, 520),
                                        anchor: new Vector2(0.5f, 0.5f), pivot: new Vector2(0.5f, 0.5f));
        _backStoryPanel.transform.SetParent(_canvas.transform, false);
        _backStoryPanel.SetActive(false);
        CreateHeaderText(_backStoryPanel.transform, "Back Story", new Vector2(0, 200));
        CreateBodyText(_backStoryPanel.transform,
            "A sudden outbreak turned the world into chaos. Bells lure the horde; " +
            "barrels can burst with force. Your goal: regroup, survive, uncover the truth.", new Vector2(0, 130));
        CreateButton(_backStoryPanel.transform, "Close", new Vector2(0, -210), () => _backStoryPanel.SetActive(false));

        // Pause group (hidden)
        _pauseGroup = CreatePanel("PauseMenu", new Color(0, 0, 0, 0.35f), stretch: false,
                                  size: new Vector2(420, 360), anchor: new Vector2(0.5f, 0.5f));
        SetPauseVisible(false);
        _btnPauseResume = CreateButton(_pauseGroup.transform, "Resume", new Vector2(0, 90), Resume);
        _btnPauseRules = CreateButton(_pauseGroup.transform, "Rules", new Vector2(0, 30), () => _rulesPanel.SetActive(true));
        _btnPauseSkip = CreateButton(_pauseGroup.transform, "Skip Level", new Vector2(0, -30), SkipLevel);
        _btnPauseMain = CreateButton(_pauseGroup.transform, "Back to Menu", new Vector2(0, -90), BackToMainMenu);
    }

    void BindSensitivityTargets()
    {
        // Bind look sensitivity (by common names)
        var all = FindObjectsByType<Component>(FindObjectsSortMode.None);
        foreach (var c in all)
        {
            if (!c) continue;
            var t = c.GetType();
            _sensField = t.GetField("LookSensitivity") ?? t.GetField("lookSensitivity") ??
                         t.GetField("MouseSensitivity") ?? t.GetField("mouseSensitivity");
            _sensProp = t.GetProperty("LookSensitivity") ?? t.GetProperty("MouseSensitivity");
            if (_sensField != null || _sensProp != null)
            {
                _lookComponent = c;
                _hipDefault = ReadSensitivity();
                break;
            }
        }
        _ads = FindAnyObjectByType<ADSController>(FindObjectsInactive.Include);
        UpdateSensLabels();
    }

    // ---------- Button handlers ----------

    public void OnClick_Play()
    {
        if (_playing) return;
        StartCoroutine(PlaySequence());
    }

    public void OnClick_Rules()
    {
        _rulesPanel.SetActive(true);
        _backStoryPanel.SetActive(false);
    }

    public void OnClick_BackStory()
    {
        _backStoryPanel.SetActive(true);
        _rulesPanel.SetActive(false);
    }

    // ---------- Play flow ----------

    IEnumerator PlaySequence()
    {
        _playing = true;

        yield return FadeCanvas(_mainButtonsGroup, _mainButtonsGroup.alpha, 0f, ButtonsFadeTime);
        yield return FadeCanvas(_blackBG, _blackBG.alpha, 0f, BgFadeTime);

        if (MenuCamera && PlayerCamera)
        {
            PlayerCamera.enabled = false;
            if (_playerAL) _playerAL.enabled = false;
            SetCursorForUI(false);
            yield return TweenCamera(MenuCamera.transform, PlayerCamera.transform, CameraTweenTime, TweenCurve);

            MenuCamera.enabled = false;
            if (_menuAL) _menuAL.enabled = false;

            PlayerCamera.enabled = true;
            if (_playerAL) _playerAL.enabled = true;

        }

        // Enable all scripts under the player
        SetPlayerChildrenEnabled(PlayerRoot, true);

        _rulesPanel.SetActive(false);
        _backStoryPanel.SetActive(false);
        _mainButtonsGroup.gameObject.SetActive(false);
        _blackBG.gameObject.SetActive(false);
    }

    // ---------- Pause ----------

    void TogglePause()
    {
        if (_paused) Resume();
        else Pause();
    }

    public void Pause()
    {
        _paused = true;
        Time.timeScale = 0f;
        SetPauseVisible(true);
        SetCursorForUI(true);


        // Disable all scripts under the player while paused
        SetPlayerChildrenEnabled(PlayerRoot, false);
    }

    public void Resume()
    {
        _rulesPanel.SetActive(false);
        _paused = false;
        Time.timeScale = 1f;
        SetPauseVisible(false);

        // Re-enable all scripts under the player
        SetPlayerChildrenEnabled(PlayerRoot, true);
        SetCursorForUI(false);

    }

    void SetPauseVisible(bool vis)
    {
        SetGroup(_pauseGroup, vis ? 1f : 0f, vis);
    }

    public void SkipLevel()
    {
        int i = SceneManager.GetActiveScene().buildIndex;
        Time.timeScale = 1f;
        SceneManager.LoadScene(i + 1);
    }

    public void BackToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    // ---------- Sensitivity ----------

    void OnHipSliderChanged(float v)
    {
        ApplyHip(v, updateSlider: false);
        PlayerPrefs.SetFloat(PP_HIP, v);
        UpdateSensLabels();
    }

    void OnADSSliderChanged(float v)
    {
        ApplyADS(v, updateSlider: false);
        PlayerPrefs.SetFloat(PP_ADS, v);
        UpdateSensLabels();
    }

    void ApplyHip(float v, bool updateSlider)
    {
        v = Mathf.Clamp(v, HipMin, HipMax);
        if (_lookComponent != null)
        {
            try
            {
                if (_sensField != null) _sensField.SetValue(_lookComponent, v);
                if (_sensProp != null) _sensProp.SetValue(_lookComponent, v);
            }
            catch { }
        }
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

    // ---------- UI Builders ----------

    Canvas CreateCanvas(string name, out CanvasScaler scaler)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var c = go.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        scaler = go.GetComponent<CanvasScaler>();
        return c;
    }

    CanvasGroup CreatePanel(string name, Color bg, bool stretch, Vector2? size = null, Vector2? anchor = null)
    {
        var go = CreatePanelGO(name, bg, size, anchor ?? new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        go.transform.SetParent(_canvas.transform, false);

        var rt = go.GetComponent<RectTransform>();
        if (stretch)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
        var cg = go.AddComponent<CanvasGroup>();
        return cg;
    }

    GameObject CreatePanelGO(string name, Color bg, Vector2? size, Vector2 anchor, Vector2 pivot)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size ?? new Vector2(600, 400);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        var img = go.GetComponent<Image>();
        img.color = bg;
        return go;
    }

    Button CreateButton(Transform parent, string label, Vector2 anchoredPos, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(260, 48);
        rt.anchoredPosition = anchoredPos;

        var img = go.GetComponent<Image>();
        img.color = new Color(1, 1, 1, 0.9f);

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        var txtGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
        txtGO.transform.SetParent(go.transform, false);
        var trt = txtGO.GetComponent<RectTransform>();
        trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
        trt.sizeDelta = new Vector2(240, 40);
        var t = txtGO.GetComponent<Text>();
        t.text = label;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.color = Color.black;
        t.resizeTextForBestFit = true;

        return btn;
    }

    void CreateHeaderText(Transform parent, string text, Vector2 anchoredPos)
    {
        var go = new GameObject("Header", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(720, 50);
        rt.anchoredPosition = anchoredPos;
        var tx = go.GetComponent<Text>();
        tx.text = text;
        tx.alignment = TextAnchor.MiddleCenter;
        tx.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tx.fontSize = 28;
        tx.color = Color.white;
    }

    void CreateBodyText(Transform parent, string text, Vector2 anchoredPos)
    {
        var go = new GameObject("Body", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(720, 100);
        rt.anchoredPosition = anchoredPos;
        var tx = go.GetComponent<Text>();
        tx.text = text;
        tx.alignment = TextAnchor.UpperCenter;
        tx.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tx.fontSize = 20;
        tx.color = Color.white;
    }

    Slider CreateSlider(Transform parent, string label, float min, float max, Vector2 anchoredPos,
                        UnityEngine.Events.UnityAction<float> onChanged, out Text valueLabel)
    {
        // Label
        var labelGO = new GameObject(label + "Label", typeof(RectTransform), typeof(Text));
        labelGO.transform.SetParent(parent, false);
        var lrt = labelGO.GetComponent<RectTransform>();
        lrt.sizeDelta = new Vector2(720, 24);
        lrt.anchoredPosition = anchoredPos + new Vector2(0, 30);
        var lt = labelGO.GetComponent<Text>();
        lt.text = label;
        lt.alignment = TextAnchor.MiddleCenter;
        lt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        lt.color = Color.white;

        // Slider background
        var go = new GameObject(label + "Slider", typeof(RectTransform), typeof(Slider));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(520, 24);
        rt.anchoredPosition = anchoredPos;
        var slider = go.GetComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = false;

        // Background
        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(go.transform, false);
        var bgrt = bg.GetComponent<RectTransform>();
        bgrt.anchorMin = new Vector2(0, 0.25f);
        bgrt.anchorMax = new Vector2(1, 0.75f);
        bgrt.offsetMin = bgrt.offsetMax = Vector2.zero;
        var bgImg = bg.GetComponent<Image>();
        bgImg.color = new Color(1, 1, 1, 0.25f);

        // Fill area
        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(go.transform, false);
        var fart = fillArea.GetComponent<RectTransform>();
        fart.anchorMin = new Vector2(0.05f, 0.25f);
        fart.anchorMax = new Vector2(0.95f, 0.75f);
        fart.offsetMin = fart.offsetMax = Vector2.zero;

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        var frt = fill.GetComponent<RectTransform>();
        frt.anchorMin = new Vector2(0, 0);
        frt.anchorMax = new Vector2(1, 1);
        frt.offsetMin = frt.offsetMax = Vector2.zero;
        var fillImg = fill.GetComponent<Image>();
        fillImg.color = new Color(1, 1, 1, 0.8f);

        // Handle
        var handleSlideArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleSlideArea.transform.SetParent(go.transform, false);
        var hsrt = handleSlideArea.GetComponent<RectTransform>();
        hsrt.anchorMin = new Vector2(0, 0);
        hsrt.anchorMax = new Vector2(1, 1);
        hsrt.offsetMin = hsrt.offsetMax = Vector2.zero;

        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(handleSlideArea.transform, false);
        var hrt = handle.GetComponent<RectTransform>();
        hrt.sizeDelta = new Vector2(20, 20);
        var handleImg = handle.GetComponent<Image>();
        handleImg.color = Color.white;

        // Wire slider
        slider.fillRect = frt;
        slider.handleRect = hrt;
        slider.targetGraphic = handleImg;
        slider.onValueChanged.AddListener(onChanged);

        // Value readout
        var valGO = new GameObject(label + "Value", typeof(RectTransform), typeof(Text));
        valGO.transform.SetParent(parent, false);
        var vrt = valGO.GetComponent<RectTransform>();
        vrt.sizeDelta = new Vector2(720, 20);
        vrt.anchoredPosition = anchoredPos + new Vector2(0, -24);
        var vt = valGO.GetComponent<Text>();
        vt.text = "";
        vt.alignment = TextAnchor.MiddleCenter;
        vt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        vt.color = Color.white;

        valueLabel = vt;

        // Set defaults from prefs
        if (label.Contains("Hip"))
        {
            float v = PlayerPrefs.GetFloat(PP_HIP, Mathf.Clamp(_hipDefault, HipMin, HipMax));
            slider.SetValueWithoutNotify(v);
        }
        else
        {
            float v = PlayerPrefs.GetFloat(PP_ADS, 0.5f);
            slider.SetValueWithoutNotify(v);
        }

        return slider;
    }

    // ---------- Helpers ----------

    static void SetGroup(CanvasGroup cg, float alpha, bool interactable)
    {
        if (!cg) return;
        cg.alpha = alpha;
        cg.interactable = interactable;
        cg.blocksRaycasts = interactable;
    }

    void SetCursorForUI(bool uiMode)
    {
        Cursor.lockState = uiMode ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = uiMode;
    }


    IEnumerator FadeCanvas(CanvasGroup cg, float from, float to, float time)
    {
        if (!cg) yield break;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        float t = 0f;
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
        from.position = p1;
        from.rotation = r1;
    }
}
