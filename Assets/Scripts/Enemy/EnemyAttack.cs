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
    private float _nextAttackTime;

    private void Awake()
    {
        _movement = GetComponent<EnemyMovement>();
    }

    private void Update()
    {
        CharacterHealth target = _movement.CurrentTarget;

        if (target == null)
        {
            return;
        }

        if (target.IsDead == true)
        {
            return;
        }

        if (Time.time < _nextAttackTime)
        {
            return;
        }

        float dist = Vector3.Distance(transform.position, target.transform.position);

        if (dist <= attackRange)
        {
            target.TakeDamage(attackDamage);
            _nextAttackTime = Time.time + attackCooldown;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
