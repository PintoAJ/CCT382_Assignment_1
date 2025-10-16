using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// ADS controller (no scope anchor required).
    /// - Hold Right Mouse to aim.
    /// - Smooth FOV and weapon pose (slides toward centered ADS pose).
    /// - Fades gun (auto-collected under Sniper_1) + anything in FadeTargets + all children in FadeGroups.
    /// - Scales look sensitivity while ADS (reflection) and optionally reduces WeaponSway.
    /// - Uses the new Input System.
    /// </summary>
    public class ADSController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Camera used for aiming. If null, Camera.main is used.")]
        public Camera PlayerCamera;
        [Tooltip("Weapon root whose local pose we animate (e.g., Sniper_Weapon).")]
        public Transform WeaponRoot;
        [Tooltip("Drag Player/PlayerCamera/WeaponSocket here. We'll search under it for 'Sniper_1'.")]
        public Transform WeaponSocket;

        [Header("FOV")]
        public float HipFOV = 70f;
        public float AimFOV = 28f;
        public float FovLerpSpeed = 10f;

        [Header("Weapon Pose (no scope anchor; tweak to taste)")]
        [Tooltip("Local pose when NOT aiming.")]
        public Vector3 HipLocalPos = Vector3.zero;
        public Vector3 HipLocalEuler = Vector3.zero;

        [Tooltip("Local pose when aiming. Defaults slide the gun toward the screen center/top a bit.")]
        public Vector3 AimLocalPos = new Vector3(0.0f, -0.02f, -0.085f);
        public Vector3 AimLocalEuler = new Vector3(0f, 0f, 0f);

        [Tooltip("How fast pose/FOV interpolate.")]
        public float PoseLerpSpeed = 12f;

        [Header("Transparency / Fade")]
        [Tooltip("Extra renderers to fade (add your towers, props, etc.). Gun renderers are auto-added.")]
        public List<Renderer> FadeTargets = new List<Renderer>();

        [Tooltip("Drop parent objects here (e.g., 'TreeStuff'); all child Renderers will fade.")]
        public List<Transform> FadeGroups = new List<Transform>();

        [Range(0f, 1f)] public float AimAlpha = 0.35f;
        [Range(0f, 1f)] public float MoveExtraFade = 0.2f;
        [Tooltip("Mouse delta magnitude above this counts as 'moving'.")]
        public float MoveThreshold = 0.04f;
        public float FadeLerpSpeed = 12f;

        [Header("Sensitivity (auto detect & scale)")]
        [Tooltip("Multiplier applied to look sensitivity while aiming (e.g., 0.5 halves sensitivity).")]
        public float AimSensitivityScale = 0.5f;
        [Tooltip("Try to find and scale a look sensitivity field/property on your look script (by reflection).")]
        public bool AutoScaleLookSensitivity = true;

        [Header("Sway (optional)")]
        [Tooltip("If you use WeaponSway, we’ll try to scale it down while ADS.")]
        public bool AutoScaleWeaponSway = true;
        [Range(0f, 1f)] public float SwayScaleAtADS = 0.2f;

        [Header("Debug")]
        public bool LogVerbose = false;

        // Runtime state
        bool _isAiming;
        float _aimT;                       // 0..1, smoothed ADS factor
        Vector2 _prevMouse;
        bool _materialsInstanced;

        readonly List<Renderer> _autoCollected = new List<Renderer>();
        readonly List<Material[]> _originalMats = new List<Material[]>();
        readonly List<Material[]> _runtimeMats = new List<Material[]>();

        // reflection cache for sensitivity scaling
        Component _lookComponent;
        FieldInfo _sensField;
        PropertyInfo _sensProperty;
        float _hipSensitivityValue = float.NaN;

        // optional sway scaling
        Component _swayComponent;
        FieldInfo _swayAmountField;
        FieldInfo _swayRotField;
        float _hipSwayAmount, _hipSwayRot;

        const string kWeaponMeshName = "Sniper_1";

        void Awake()
        {
            if (PlayerCamera == null) PlayerCamera = Camera.main;

            // Initial pose/FOV
            if (WeaponRoot != null)
            {
                WeaponRoot.localPosition = HipLocalPos;
                WeaponRoot.localRotation = Quaternion.Euler(HipLocalEuler);
            }
            if (PlayerCamera != null) PlayerCamera.fieldOfView = HipFOV;

            _prevMouse = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
        }

        void Start()
        {
            // Collect gun renderers after the weapon prefab spawns, then set up materials.
            StartCoroutine(CollectThenSetup());

            if (AutoScaleLookSensitivity)
                TryBindLookSensitivity();
            if (AutoScaleWeaponSway)
                TryBindWeaponSway();
        }

        IEnumerator CollectThenSetup()
        {
            // Wait up to ~1s for the weapon clone to appear.
            const int maxFrames = 60;
            int tries = 0;
            bool collected = false;

            while (tries++ < maxFrames && !collected)
            {
                collected = TryCollectWeaponRenderers();
                if (!collected) yield return null;
            }

            if (!collected && LogVerbose)
                Debug.LogWarning("[ADSController] Could not find renderers under WeaponSocket/Sniper_1. Check hierarchy names or increase wait window.");

            // Build final list: auto-collected gun + user-added targets + groups
            var finalTargets = new List<Renderer>();
            finalTargets.AddRange(_autoCollected);
            foreach (var r in FadeTargets)
                if (r != null && !finalTargets.Contains(r)) finalTargets.Add(r);
            AddGroupRenderersTo(finalTargets);

            SetupMaterials(finalTargets);
        }

        bool TryCollectWeaponRenderers()
        {
            bool added = false;

            if (WeaponSocket != null)
            {
                Transform sniper1 = FindDeepChildByName(WeaponSocket, kWeaponMeshName);
                if (sniper1 != null)
                {
                    var rs = sniper1.GetComponentsInChildren<Renderer>(true);
                    foreach (var r in rs)
                    {
                        if (r != null && !_autoCollected.Contains(r))
                        {
                            _autoCollected.Add(r);
                            added = true;
                            if (LogVerbose) Debug.Log($"[ADSController] + weapon renderer: {r.name}", r);
                        }
                    }
                }
                else if (LogVerbose) Debug.Log("[ADSController] Waiting for Sniper_1 to spawn under WeaponSocket…");
            }

            return added && _autoCollected.Count > 0;
        }

        void AddGroupRenderersTo(List<Renderer> dest)
        {
            if (FadeGroups == null) return;

            foreach (var group in FadeGroups)
            {
                if (group == null) continue;
                var rs = group.GetComponentsInChildren<Renderer>(true);
                foreach (var r in rs)
                {
                    if (r != null && !dest.Contains(r))
                        dest.Add(r);
                }
            }
        }

        void SetupMaterials(List<Renderer> targets)
        {
            if (_materialsInstanced) return;

            _originalMats.Clear();
            _runtimeMats.Clear();

            foreach (var r in targets)
            {
                if (r == null)
                {
                    _originalMats.Add(null);
                    _runtimeMats.Add(null);
                    continue;
                }

                var shared = r.sharedMaterials;
                _originalMats.Add(shared);

                var instanced = new Material[shared.Length];
                for (int i = 0; i < shared.Length; i++)
                {
                    instanced[i] = new Material(shared[i]);  // clone original
                    EnsureTransparent(instanced[i]);         // force alpha blending
                }
                r.materials = instanced;
                _runtimeMats.Add(instanced);
            }

            _materialsInstanced = true;
        }

        void Update()
        {
            // Input (new Input System)
            if (Mouse.current != null)
                _isAiming = Mouse.current.rightButton.isPressed;

            // Smooth aim factor
            float target = _isAiming ? 1f : 0f;
            _aimT = Mathf.MoveTowards(_aimT, target, Time.deltaTime * PoseLerpSpeed);

            // Pose (no scope anchor: slide toward centered ADS pose)
            if (WeaponRoot != null)
            {
                Vector3 aimPos = Vector3.Lerp(HipLocalPos, AimLocalPos, Smooth01(_aimT));
                Quaternion aimRot = Quaternion.Slerp(Quaternion.Euler(HipLocalEuler),
                                                     Quaternion.Euler(AimLocalEuler),
                                                     Smooth01(_aimT));

                WeaponRoot.localPosition = Vector3.Lerp(WeaponRoot.localPosition, aimPos, Time.deltaTime * PoseLerpSpeed);
                WeaponRoot.localRotation = Quaternion.Slerp(WeaponRoot.localRotation, aimRot, Time.deltaTime * PoseLerpSpeed);
            }

            // FOV
            if (PlayerCamera != null)
            {
                float targetFov = Mathf.Lerp(HipFOV, AimFOV, Smooth01(_aimT));
                PlayerCamera.fieldOfView = Mathf.Lerp(PlayerCamera.fieldOfView, targetFov, Time.deltaTime * FovLerpSpeed);
            }

            // Mouse motion fade
            Vector2 mouseDelta = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
            float mouseMag = (mouseDelta - _prevMouse).magnitude;
            _prevMouse = mouseDelta;

            float alpha = Mathf.Lerp(1f, AimAlpha, Smooth01(_aimT));
            if (mouseMag > MoveThreshold) alpha = Mathf.Max(0f, alpha - MoveExtraFade);
            ApplyAlpha(alpha);

            // Sensitivity & sway scaling
            UpdateLookSensitivity();
            UpdateWeaponSwayScale();
        }

        // ---------------- helpers ----------------

        static float Smooth01(float t) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));

        static Transform FindDeepChildByName(Transform root, string name)
        {
            if (root == null) return null;
            var q = new Queue<Transform>();
            q.Enqueue(root);
            while (q.Count > 0)
            {
                var t = q.Dequeue();
                if (t.name == name) return t;
                for (int i = 0; i < t.childCount; i++) q.Enqueue(t.GetChild(i));
            }
            return null;
        }

        void ApplyAlpha(float targetAlpha)
        {
            if (!_materialsInstanced) return;

            foreach (var mats in _runtimeMats)
            {
                if (mats == null) continue;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null) continue;

                    Color c = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor")
                              : m.HasProperty("_Color") ? m.GetColor("_Color")
                              : Color.white;

                    c.a = Mathf.Lerp(c.a, targetAlpha, Time.deltaTime * FadeLerpSpeed);

                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
                    if (m.HasProperty("_Color")) m.SetColor("_Color", c);
                }
            }
        }

        static void EnsureTransparent(Material m)
        {
            if (m == null) return;

            // URP Lit/Unlit/Graph
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f); // Transparent
            if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 0f);
            if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0f);     // Alpha
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");

            if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            // Built-in Standard compatibility
            if (m.HasProperty("_Mode")) m.SetFloat("_Mode", 3f); // Transparent

            // Ensure starting alpha is 1
            if (m.HasProperty("_BaseColor"))
            {
                var c = m.GetColor("_BaseColor"); c.a = 1f; m.SetColor("_BaseColor", c);
            }
            if (m.HasProperty("_Color"))
            {
                var c = m.GetColor("_Color"); c.a = 1f; m.SetColor("_Color", c);
            }
        }

        // -------- sensitivity scaling (reflection; no code changes needed elsewhere) --------
        void TryBindLookSensitivity()
        {
            var root = transform.root;
            var all = root.GetComponentsInChildren<Component>(true);
            foreach (var comp in all)
            {
                if (comp == null) continue;
                var t = comp.GetType();

                _sensField = t.GetField("LookSensitivity") ?? t.GetField("lookSensitivity") ??
                             t.GetField("MouseSensitivity") ?? t.GetField("mouseSensitivity");
                _sensProperty = t.GetProperty("LookSensitivity") ?? t.GetProperty("MouseSensitivity");

                if (_sensField != null || _sensProperty != null)
                {
                    _lookComponent = comp;
                    float hip = ReadSensitivity();
                    if (!float.IsNaN(hip)) _hipSensitivityValue = hip;
                    if (LogVerbose) Debug.Log($"[ADSController] Sensitivity bound to {t.Name} (hip={_hipSensitivityValue}).");
                    break;
                }
            }

            if (_lookComponent == null && LogVerbose)
                Debug.LogWarning("[ADSController] Could not auto-bind look sensitivity. You can turn this off or expose a public float named LookSensitivity.");
        }

        float ReadSensitivity()
        {
            try
            {
                if (_sensField != null) return ConvertToFloat(_sensField.GetValue(_lookComponent));
                if (_sensProperty != null) return ConvertToFloat(_sensProperty.GetValue(_lookComponent));
            }
            catch { }
            return float.NaN;
        }

        void WriteSensitivity(float value)
        {
            try
            {
                if (_sensField != null) _sensField.SetValue(_lookComponent, value);
                if (_sensProperty != null) _sensProperty.SetValue(_lookComponent, value);
            }
            catch { }
        }

        static float ConvertToFloat(object v)
        {
            if (v == null) return float.NaN;
            if (v is float f) return f;
            if (v is double d) return (float)d;
            if (v is int i) return i;
            if (float.TryParse(v.ToString(), out var p)) return p;
            return float.NaN;
        }

        void UpdateLookSensitivity()
        {
            if (!AutoScaleLookSensitivity || _lookComponent == null || float.IsNaN(_hipSensitivityValue)) return;
            float scaled = Mathf.Lerp(_hipSensitivityValue, _hipSensitivityValue * AimSensitivityScale, Smooth01(_aimT));
            WriteSensitivity(scaled);
        }

        // -------- optional sway scaling (works with the earlier WeaponSway script) --------
        void TryBindWeaponSway()
        {
            if (WeaponRoot == null) return;
            foreach (var comp in WeaponRoot.GetComponentsInChildren<Component>(true))
            {
                if (comp == null) continue;
                if (comp.GetType().Name != "WeaponSway") continue;

                _swayComponent = comp;
                var t = comp.GetType();
                _swayAmountField = t.GetField("swayAmount");
                _swayRotField = t.GetField("swayRotAmount");

                if (_swayAmountField != null) _hipSwayAmount = ConvertToFloat(_swayAmountField.GetValue(comp));
                if (_swayRotField != null) _hipSwayRot = ConvertToFloat(_swayRotField.GetValue(comp));
                if (LogVerbose) Debug.Log("[ADSController] Bound to WeaponSway for ADS scaling.");
                break;
            }
        }

        void UpdateWeaponSwayScale()
        {
            if (!AutoScaleWeaponSway || _swayComponent == null) return;
            float k = Mathf.Lerp(1f, SwayScaleAtADS, Smooth01(_aimT));
            try
            {
                if (_swayAmountField != null) _swayAmountField.SetValue(_swayComponent, _hipSwayAmount * k);
                if (_swayRotField != null) _swayRotField.SetValue(_swayComponent, _hipSwayRot * k);
            }
            catch { }
        }

        // -------- clean-up --------
        void OnDestroy()
        {
            // Restore sensitivity
            if (AutoScaleLookSensitivity && _lookComponent != null && !float.IsNaN(_hipSensitivityValue))
                WriteSensitivity(_hipSensitivityValue);

            // Clean materials (instances) & restore originals
            if (_runtimeMats != null)
            {
                foreach (var mats in _runtimeMats)
                {
                    if (mats == null) continue;
                    for (int i = 0; i < mats.Length; i++)
                        if (mats[i] != null) Destroy(mats[i]);
                }
            }
            // We mirrored add order into _originalMats; restore in that order.
            int idx = 0;
            void Restore(List<Renderer> list)
            {
                foreach (var r in list)
                {
                    if (r != null && _originalMats.Count > idx && _originalMats[idx] != null)
                        r.sharedMaterials = _originalMats[idx];
                    idx++;
                }
            }
            Restore(_autoCollected);

            // Rebuild final list order the same way we did in setup for user-provided targets
            var finalTargets = new List<Renderer>();
            foreach (var r in FadeTargets) if (r != null) finalTargets.Add(r);
            if (FadeGroups != null)
            {
                foreach (var group in FadeGroups)
                {
                    if (group == null) continue;
                    var rs = group.GetComponentsInChildren<Renderer>(true);
                    foreach (var r in rs) if (r != null) finalTargets.Add(r);
                }
            }
            Restore(finalTargets);
        }
    }
}
