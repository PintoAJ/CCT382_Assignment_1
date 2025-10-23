using UnityEngine;
using UnityEngine.UI;

public class HealthBarUpdate : MonoBehaviour
{
    private CharacterStats stats;
    [SerializeField] private Image _healthBarFill;
    [SerializeField] private Gradient _colorGradient;
    public float targetFillAmount;

    void Start()
    {
        // If the stats live on a parent (typical), use GetComponentInParent as fallback
        stats = GetComponent<CharacterStats>() ?? GetComponentInParent<CharacterStats>();

        if (stats == null)
            Debug.LogError("[HealthBarUpdate] CharacterStats not found on this object or its parents.");

        if (_healthBarFill == null)
            Debug.LogError("[HealthBarUpdate] _healthBarFill is not assigned in the inspector.");

        // Reminder: the Image type must be 'Filled' for fillAmount to be visible.
        if (_healthBarFill != null && _healthBarFill.type != Image.Type.Filled)
            Debug.LogWarning("[HealthBarUpdate] _healthBarFill Image.type is not 'Filled' — fillAmount won't be visible.");
    }

    private void Update()
    {
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (stats == null || _healthBarFill == null) return;

        // Protect against zero/invalid MaxHealth
        float max = Mathf.Max(1f, (float)stats.MaxHealth);
        float current = Mathf.Clamp((float)stats.HealthLeft(), 0f, max);

        targetFillAmount = Mathf.Clamp01(current / max);
        _healthBarFill.fillAmount = targetFillAmount;

        if (_colorGradient != null)
            _healthBarFill.color = _colorGradient.Evaluate(targetFillAmount);

        // Debug to verify values
        // Debug.Log($"Health: {current}/{max} => {targetFillAmount:0.00}");
    }
}
