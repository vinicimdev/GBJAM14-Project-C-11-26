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

    private CharacterHealth _health;

    private void Awake()
    {
        _health = GetComponent<CharacterHealth>();
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
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        SetBehavioursEnabled(false);

        characterAnimatorController.SetDown(true);

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
