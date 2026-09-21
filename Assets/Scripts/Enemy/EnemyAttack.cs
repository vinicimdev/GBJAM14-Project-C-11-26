using UnityEngine;

/// <summary>
/// Enemy melee attack, deals damage to CharacterHealth when close.
/// </summary>
public class EnemyAttack : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField, Tooltip("")]
    private float attackRange = 1.8f;
    [SerializeField, Tooltip("")]
    private int attackDamage = 1;
    [SerializeField, Tooltip("")]
    private float attackCooldown = 1f;

    private EnemyMovement _movement;
    private CharacterAnimatorController _animatorController;
    private float _nextAttackTime;

    private void Awake()
    {
        _movement = GetComponent<EnemyMovement>();
        _animatorController = GetComponent<CharacterAnimatorController>();
    }

    private void Update()
    {
        CharacterHealth target = _movement.CurrentTarget;

        if (target == null)
        {
            _movement.SetAgentStopped(false);
            return;
        }

        if (target.IsDead == true)
        {
            _movement.SetAgentStopped(false);
            return;
        }

        Vector3 toTarg = target.transform.position - transform.position;
        toTarg.y = 0f;
        float dist = toTarg.magnitude;
        bool inRange = dist <= attackRange;

        _movement.SetAgentStopped(inRange);

        if (Time.time < _nextAttackTime)
        {
            return;
        }

        if (inRange == true)
        {
            target.TakeDamage(attackDamage);
            _nextAttackTime = Time.time + attackCooldown;

            _animatorController.TriggerAttack();

            if (target.CompareTag("Chest") == true)
            {
                _movement.OnSteal(attackDamage);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
