using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Isometric movement script.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CharacterMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField, Tooltip("Base move speed of the character.")] private float moveSpeed = 5f;
    [SerializeField, Tooltip("Base rotation speed of the character.")] private float rotationSpeed = 12f;

    [Header("References")]
    [SerializeField, Tooltip("Reference to the Input Action, not the whole Input Action Map.")] 
    private InputActionReference moveAction;
    [SerializeField, Tooltip("Reference to the Camera.")]
    private Transform cameraTransform;

    private Rigidbody _rb;
    private Vector2 _inputRaw;
    private Vector3 _moveDirection;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        _rb.freezeRotation = true;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void OnEnable()
    {
        if (moveAction != null)
        {
            moveAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.action.Disable();
        }
    }

    private void Update()
    {
        if (moveAction != null)
        {
            _inputRaw = moveAction.action.ReadValue<Vector2>();
        }
        else
        {
            _inputRaw = Vector2.zero;
        }

        _moveDirection = CameraRelativeDirection(_inputRaw);
    }

    private void FixedUpdate()
    {
        Vector3 velocity = _moveDirection * moveSpeed;
        velocity.y = _rb.linearVelocity.y;
        _rb.linearVelocity = velocity;

        if (_moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targRotation = Quaternion.LookRotation(_moveDirection, Vector3.up);

            _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, targRotation, rotationSpeed * Time.fixedDeltaTime));
        }
    }

    private Vector3 CameraRelativeDirection(Vector2 input)
    {
        if (cameraTransform == null)
        {
            return new Vector3(input.x, 0f, input.y);
        }

        Vector3 camFwd = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camFwd.y = 0f;
        camRight.y = 0f;
        camFwd.Normalize();
        camRight.Normalize();

        Vector3 dir = camFwd * input.y + camRight * input.x;

        if (dir.sqrMagnitude > 1f)
        {
            return dir.normalized;
        }
        else
        {
            return dir;
        }
    }
}
