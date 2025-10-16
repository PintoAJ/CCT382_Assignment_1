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
        stats = GetComponent<CharacterStats>();
    }

    // Update is called once per frame

    private void Update()
    {
        UpdateHealthBar();
        
    }
    private void UpdateHealthBar()
    {
        
        if (stats.HealthLeft() > 0)
        {
            targetFillAmount = (float) stats.HealthLeft() / (float) stats.MaxHealth;
            _healthBarFill.fillAmount = targetFillAmount;
            _healthBarFill.color = _colorGradient.Evaluate(targetFillAmount);
            Debug.Log(targetFillAmount);
        }
        else {
            _healthBarFill.enabled = false;
        }
        
    }
}
