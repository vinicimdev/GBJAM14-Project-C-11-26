using System;
using UnityEngine;

/// <summary>
/// Player health with damage and i-frames.
/// </summary>
public class CharacterHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField, Tooltip("")]
    private int maxHealth = 5;

    [Header("Invincibility")]
    [SerializeField, Tooltip("")]
    private float invincibilityDuration = 0.8f;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsDead => CurrentHealth <= 0;
    public bool IsInvincible => Time.time < _invincibleUntil;

    public event Action<int> OnHealthChanged;
    public event Action OnDeath;

    private float _invincibleUntil;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead) return;

        if (IsInvincible) return;

        if (amount <= 0) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);

        _invincibleUntil = Time.time + invincibilityDuration;

        OnHealthChanged?.Invoke(CurrentHealth);

        if (CurrentHealth <= 0)
        {
            OnDeath?.Invoke();
        }
    }

    // TODO: Heal method?
}
