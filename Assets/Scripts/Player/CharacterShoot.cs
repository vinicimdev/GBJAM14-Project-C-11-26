using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterShoot : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField, Tooltip("Reference to the bullet prefab.")]
    private Bullet bulletPrefab;
    [SerializeField, Tooltip("Reference to the FirePoint object, children of the Player object.")]
    private Transform firePoint;
    [SerializeField, Tooltip("Base speed of the projectile.")]
    private float bulletSpeed;

    [Header("Input")]
    [SerializeField, Tooltip("Reference to the Input Action, not the whole Input Action Map.")]
    private InputActionReference fireAction;

    [Header("References")]
    [SerializeField, Tooltip("Reference to the Camera.")]
    private Camera aimCamera;

    private void Awake()
    {
        if (aimCamera == null && Camera.main != null)
        {
            aimCamera = Camera.main;
        }
    }

    private void OnEnable()
    {
        fireAction.action.Enable();
        fireAction.action.performed += OnFirePerformed;
    }

    private void OnDisable()
    {
        fireAction.action.performed -= OnFirePerformed;
        fireAction.action.Disable();
    }

    private void OnFirePerformed(InputAction.CallbackContext ctx)
    {
        Fire();
    }

    private void Fire()
    {
        Vector3 dir = firePoint.forward;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
        {
            return;
        }

        dir.Normalize();

        Bullet bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(dir));
        bullet.Shoot(dir, bulletSpeed);
    }
}
