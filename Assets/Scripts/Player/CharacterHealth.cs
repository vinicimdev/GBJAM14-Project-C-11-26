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

    [Header("Audio References")]
    [SerializeField, Tooltip("")]
    private AudioSource onDamageAudioSource;
    [SerializeField, Tooltip("")]
    private AudioClip onDamageAudioClip;

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
        Debug.Log($"{name} got hit, damage: {amount}! Health left: {CurrentHealth}.");

        if (IsDead) return;

        if (IsInvincible) return;

        if (amount <= 0) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);

        _invincibleUntil = Time.time + invincibilityDuration;

        OnHealthChanged?.Invoke(CurrentHealth);

        onDamageAudioSource.PlayOneShot(onDamageAudioClip);

        if (CurrentHealth <= 0)
        {
            OnDeath?.Invoke();
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        OnHealthChanged?.Invoke(CurrentHealth);
    }
}
