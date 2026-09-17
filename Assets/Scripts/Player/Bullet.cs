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
        transform.position += _direction * _speed * Time.deltaTime;
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
