using System.Collections;
using UnityEngine;

public class ExpandingSphereFX : MonoBehaviour
{
    // Quick entry: spawn centered on a world transform, sized to its render bounds,
    // with a tiny camera-facing offset so it doesn't get occluded by the object itself.
    public static void SpawnAtObject(
        Transform target,
        Color color,
        float endRadius = 4f,
        float duration = 0.35f,
        float padMeters = 0.05f,   // how much bigger than the object to start
        float camOffset = 0.06f)   // small nudge toward camera to ensure visibility
    {
        if (target == null)
            return;

        // Compute a decent start radius from renderers
        var b = GetCompositeBounds(target);
        float startR = Mathf.Max(0.05f, b.extents.magnitude + padMeters);

        // Position: center of the object, nudged a bit toward the camera
        Vector3 pos = target.position;
        var cam = Camera.main;
        if (cam) pos += cam.transform.forward * camOffset;

        Spawn(pos, color, startR, Mathf.Max(startR, endRadius), duration, renderOnTop: true);
    }

    // Original API (kept, with an extra renderOnTop flag)
    public static void Spawn(
        Vector3 position,
        Color color,
        float startRadius = 0.1f,
        float endRadius = 4f,
        float duration = 0.35f,
        bool renderOnTop = false)
    {
        var go = new GameObject("ExpandingSphereFX");
        go.transform.position = position;

        var fx = go.AddComponent<ExpandingSphereFX>();
        fx._color = color;
        fx._startR = Mathf.Max(0f, startRadius);
        fx._endR = Mathf.Max(fx._startR, endRadius);
        fx._dur = Mathf.Max(0.01f, duration);
        fx._renderOnTop = renderOnTop;

        fx.BuildSphere();
        fx.StartCoroutine(fx.Run());
    }

    MeshRenderer _renderer;
    Material _mat;
    float _startR, _endR, _dur;
    Color _color;
    bool _renderOnTop;

    void BuildSphere()
    {
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.SetParent(transform, worldPositionStays: false);
        sphere.transform.localPosition = Vector3.zero;
        sphere.transform.localScale = Vector3.one * (_startR * 2f); // diameter
        var col = sphere.GetComponent<Collider>(); if (col) Destroy(col);

        _renderer = sphere.GetComponent<MeshRenderer>();
        _mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (_mat == null) _mat = new Material(Shader.Find("Standard")); // fallback
        EnsureTransparent(_mat, _renderOnTop);

        SetMatColor(_mat, _color);
        _renderer.sharedMaterial = _mat;
    }

    IEnumerator Run()
    {
        float t = 0f;
        while (t < _dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / _dur);

            float r = Mathf.Lerp(_startR, _endR, Mathf.SmoothStep(0f, 1f, k));
            transform.GetChild(0).localScale = Vector3.one * (r * 2f);

            // fade alpha
            float alpha = Mathf.Lerp(_color.a, 0f, k);
            var c = GetMatColor(_mat);
            c.r = _color.r; c.g = _color.g; c.b = _color.b; c.a = alpha;
            SetMatColor(_mat, c);

            yield return null;
        }
        Destroy(gameObject);
    }

    static Bounds GetCompositeBounds(Transform root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(root.position, Vector3.zero);

        var b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);
        return b;
    }

    static void EnsureTransparent(Material m, bool renderOnTop)
    {
        if (m == null) return;

        // URP / Built-in common settings
        if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f); // Transparent
        if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 0f);
        m.DisableKeyword("_ALPHATEST_ON");
        m.EnableKeyword("_ALPHABLEND_ON");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");

        if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);

        // draw very late
        m.renderQueue = 5000;

        // Try to force ZTest Always so it doesn't get occluded by the object
        if (renderOnTop && m.HasProperty("_ZTest"))
        {
            // 8 == CompareFunction.Always
            m.SetInt("_ZTest", 8);
        }

        // Initialize alpha to 1
        var c = GetMatColor(m);
        c.a = 1f;
        SetMatColor(m, c);
    }

    static Color GetMatColor(Material m)
    {
        if (m.HasProperty("_BaseColor")) return m.GetColor("_BaseColor");
        if (m.HasProperty("_Color")) return m.GetColor("_Color");
        return Color.white;
    }

    static void SetMatColor(Material m, Color c)
    {
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
    }
}
