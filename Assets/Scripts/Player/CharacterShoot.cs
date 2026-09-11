using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterShoot : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField, Tooltip("")]
    private Bullet bulletPrefab;
    [SerializeField, Tooltip("")]
    private Transform firePoint;
    [SerializeField, Tooltip("")]
    private float bulletSpeed;

    [Header("Input")]
    [SerializeField, Tooltip("")]
    private InputActionReference fireAction;

    [Header("References")]
    [SerializeField, Tooltip("")]
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
        Vector3 aimPoint = GetMouseWorldPointOnPlane(firePoint.position.y);
        Vector3 dir = aimPoint - firePoint.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f)
        {
            return;
        }

        dir.Normalize();

        Bullet bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(dir));
        bullet.Shoot(dir, bulletSpeed);
    }

    private Vector3 GetMouseWorldPointOnPlane(float planeY)
    {
        Vector2 mousePos;

        if (Mouse.current != null)
        {
            mousePos = Mouse.current.position.ReadValue();
        }
        else
        {
            mousePos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }

        Ray ray = aimCamera.ScreenPointToRay(mousePos);
        Plane ground = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));

        if (ground.Raycast(ray, out float distance) == true)
        {
            return ray.GetPoint(distance);
        }
        else
        {
            return firePoint.position + firePoint.forward;
        }
    }
}
