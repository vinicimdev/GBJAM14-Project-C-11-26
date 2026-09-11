using UnityEngine;

/// <summary>
/// Projectile script for the Player shoot attack.
/// </summary>
public class Bullet : MonoBehaviour
{
    [SerializeField, Tooltip("")]
    private float lifetime = 3f;

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
        // Just in case we forget to disable the bullet tag collision with the player tag
        // on the layer collision matrix
        if (other.CompareTag("Player"))
        {
            return;
        }

        if (other.CompareTag("Enemy"))
        {
            Destroy(other.gameObject);
            Destroy(gameObject);
        }
    }
}
