using UnityEngine;

/// <summary>
/// Generic animator controller for characters.
/// </summary>
public class CharacterAnimatorController : MonoBehaviour
{
    [Header("References")]
    [SerializeField, Tooltip("")]
    private Animator animator;

    [Header("Animator Parameter Names")]
    [SerializeField, Tooltip("")]
    private string movingParameter = "IsMoving";
    [SerializeField, Tooltip("")]
    private string downParameter = "IsDown";
    [SerializeField, Tooltip("")]
    private string attackParameter = "Attack";

    private int _isMovingHash;
    private int _isDownHash;
    private int _attackHash;

    private void Awake()
    {
        _isMovingHash = Animator.StringToHash(movingParameter);
        _isDownHash = Animator.StringToHash(downParameter);
        _attackHash = Animator.StringToHash(attackParameter);
    }

    public void SetMoving(bool val)
    {
        if (string.IsNullOrEmpty(movingParameter))
        {
            return;
        }

        animator.SetBool(_isMovingHash, val);
    }

    public void SetDown(bool val)
    {
        if (string.IsNullOrEmpty(downParameter))
        {
            return;
        }

        animator.SetBool(_isDownHash, val);
    }

    public void TriggerAttack()
    {
        if (string.IsNullOrEmpty(attackParameter))
        {
            return;
        }

        animator.SetTrigger(_attackHash);
    }
}
