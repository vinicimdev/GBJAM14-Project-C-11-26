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

    private Transform _playerTransform;
    private CharacterHealth _playerHealth;
    private float _nextAttackTime;

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        _playerTransform = player.transform;
        _playerHealth = player.GetComponent<CharacterHealth>();
    }

    private void Update()
    {
        if (Time.time < _nextAttackTime)
        {
            return;
        }

        float dist = Vector3.Distance(transform.position, _playerTransform.position);

        if (dist <= attackRange)
        {
            _playerHealth.TakeDamage(attackDamage);
            _nextAttackTime = Time.time + attackCooldown;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
