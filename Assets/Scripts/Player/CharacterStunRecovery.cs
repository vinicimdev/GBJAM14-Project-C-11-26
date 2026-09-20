using System.Collections;
using UnityEngine;

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
    private Rigidbody characterRigidbody;
    [SerializeField, Tooltip("")]
    private CharacterAnimatorController characterAnimatorController;

    private CharacterHealth _health;
    private Coroutine _stunRoutine;
    private RigidbodyConstraints _originalConstraints;
    private void Awake()
    {
        _health = GetComponent<CharacterHealth>();
        characterRigidbody = GetComponent<Rigidbody>();
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

    private void StopMovement()
    {
        if (characterRigidbody == null)
        {
            return;
        }

        characterRigidbody.linearVelocity = Vector3.zero;
        characterRigidbody.angularVelocity = Vector3.zero;
        _originalConstraints = characterRigidbody.constraints;
        characterRigidbody.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private IEnumerator StunRoutine()
    {
        SetBehavioursEnabled(false);

        StopMovement();

        characterAnimatorController.SetDown(true);

        yield return new WaitForSeconds(stunDuration);

        _health.Heal(_health.MaxHealth);

        characterRigidbody.constraints = _originalConstraints;

        SetBehavioursEnabled(true);

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
