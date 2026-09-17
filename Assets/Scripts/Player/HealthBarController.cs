using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Updates a Unity UI Slider to display the CharacterHealth's current and max health.
/// </summary>
public class HealthBarController : MonoBehaviour
{
    [Header("References")]
    [SerializeField, Tooltip("")]
    private CharacterHealth health;
    [SerializeField, Tooltip("")]
    private Slider slider;

    private void Start()
    {
        slider.minValue = 0f;
        slider.maxValue = health.MaxHealth;
        slider.value = health.CurrentHealth;
    }

    private void OnEnable()
    {
        health.OnHealthChanged += UpdateSlider;
    }

    private void OnDisable()
    {
        health.OnHealthChanged -= UpdateSlider;
    }

    private void UpdateSlider(int currentHealth)
    {
        slider.value = currentHealth;
    }
}
