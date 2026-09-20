using UnityEngine;

/// <summary>
/// Projectile script for the Player shoot attack.
/// </summary>
public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField, Tooltip("")]
    private float lifetime = 3f;
    [SerializeField, Tooltip("")]
    private int damage = 1;

    [Header("Aim Assist")]
    [SerializeField, Tooltip("")]
    private float assistRange = 8f;
    [SerializeField, Tooltip("")]
    private float assistConeAngle = 45f;
    [SerializeField, Tooltip("")]
    private float assistCurveSpeed = 60f;

    private Vector3 _direction;
    private float _speed;

    private void Awake()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        GetComponent<Collider>().isTrigger = true;
    }

    public void Shoot(Vector3 dir, float speed)
    {
        _direction = dir.normalized;
        _speed = speed;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        Transform target = FindNearestEnemyInCone();

        if (target != null)
        {
            Vector3 desired = target.position - transform.position;
            desired.y = 0f;

            if (desired.sqrMagnitude > 0.0001f)
            {
                desired.Normalize();

                _direction = Vector3.RotateTowards(_direction, desired, assistCurveSpeed * Mathf.Deg2Rad * Time.deltaTime, 0f);
            }
        }

        transform.position += _direction * _speed * Time.deltaTime;
        transform.rotation = Quaternion.LookRotation(_direction);
    }

    private Transform FindNearestEnemyInCone()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, assistRange);

        Transform closest = null;
        float closestDistSqr = float.MaxValue;

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Enemy") == false)
            {
                continue;
            }

            Vector3 toEnemy = hit.transform.position - transform.position;
            toEnemy.y = 0f;

            if (Vector3.Angle(_direction, toEnemy) > assistConeAngle)
            {
                continue;
            }

            float distSqr = toEnemy.sqrMagnitude;

            if (distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr;
                closest = hit.transform;
            }
        }

        return closest;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Chest"))
        {
            return;
        }

        if (other.TryGetComponent(out CharacterHealth health))
        {
            health.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
