using System;
using System.Collections;
using UnityEngine;

public class EnemyDeath : MonoBehaviour
{
    [Header("Death Settings")]
    [SerializeField, Tooltip("")]
    private float destroyDelay = 2f;

    [Header("References")]
    [SerializeField, Tooltip("")]
    private CharacterAnimatorController characterAnimatorController;

    [Header("Behaviours")]
    [SerializeField, Tooltip("")]
    private Behaviour[] behavioursToDisable;

    [Header("Score")]
    [SerializeField, Tooltip("")]
    private int scoreOnKill = 10;

    [Header("Audio References")]
    [SerializeField, Tooltip("")]
    private AudioSource onDeathAudioSource;
    [SerializeField, Tooltip("")]
    private AudioClip onDeathAudioClip;

    private CharacterHealth _health;
    private EnemyMovement _movement;
    private CharacterAnimatorController _animatorController;

    private void Awake()
    {
        _health = GetComponent<CharacterHealth>();
        _movement = GetComponent<EnemyMovement>();
        _animatorController = GetComponent<CharacterAnimatorController>();
    }

    private void OnEnable()
    {
        _health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        _health.OnDeath -= HandleDeath;
    }

    private void HandleDeath()
    {
        ScoreManager.Instance.AddScore(scoreOnKill);

        if (_movement.HasStolenGold == true)
        {
            _movement.Chest.Heal(_movement.StolenAmount);
        }

        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        SetBehavioursEnabled(false);

        _animatorController.SetDown(true);

        characterAnimatorController.SetDown(true);

        onDeathAudioSource.PlayOneShot(onDeathAudioClip);

        yield return new WaitForSeconds(destroyDelay);

        Destroy(gameObject);
    }

    private void SetBehavioursEnabled(bool enabled)
    {
        if (behavioursToDisable == null)
        {
            return;
        }

        foreach (Behaviour b in behavioursToDisable)
        {
            if (b != null)
            {
                b.enabled = enabled;
            }
        }
    }
}
