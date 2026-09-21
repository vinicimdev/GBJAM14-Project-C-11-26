using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent (typeof(CharacterHealth))]
public class CharacterStunRecovery : MonoBehaviour
{
    [Header("Stun")]
    [SerializeField, Tooltip("")]
    private float stunDuration = 5f;

    [Header("Behaviours")]
    [SerializeField, Tooltip("")]
    private Behaviour[] behavioursToDisable;

    [Header("Physics")]
    [SerializeField, Tooltip("")]
    private CharacterAnimatorController characterAnimatorController;

    [Header("Audio References")]
    [SerializeField, Tooltip("")]
    private AudioSource onStunAudioSource;
    [SerializeField, Tooltip("")]
    private AudioClip onStunAudioClip;

    private CharacterHealth _health;
    private CharacterMovement _movement;
    private Coroutine _stunRoutine;

    private void Awake()
    {
        _health = GetComponent<CharacterHealth>();
        _movement = GetComponent<CharacterMovement>();
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
        if (_stunRoutine != null)
        {
            StopCoroutine(_stunRoutine);
        }

        _stunRoutine = StartCoroutine(StunRoutine());
    }

    private IEnumerator StunRoutine()
    {
        SetBehavioursEnabled(false);

        _movement.SetPlayerAgentStopped(true);

        characterAnimatorController.SetDown(true);

        onStunAudioSource.PlayOneShot(onStunAudioClip);

        yield return new WaitForSeconds(stunDuration);

        _health.Heal(_health.MaxHealth);

        SetBehavioursEnabled(true);

        _movement.SetPlayerAgentStopped(false);

        characterAnimatorController.SetDown(false);

        _stunRoutine = null;
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
