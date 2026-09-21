using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

/// <summary>
/// Isometric movement script.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class CharacterMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField, Tooltip("Base move speed of the character.")] 
    private float moveSpeed = 5f;
    [SerializeField, Tooltip("Base rotation speed of the character.")]
    private float rotationSpeed = 12f;

    [Header("References")]
    [SerializeField, Tooltip("Reference to the Input Action, not the whole Input Action Map.")] 
    private InputActionReference moveAction;
    [SerializeField, Tooltip("Reference to the Camera.")]
    private Transform cameraTransform;
    [SerializeField, Tooltip("")]
    private CharacterAnimatorController characterAnimatorController;

    private NavMeshAgent _agent;
    private Vector2 _inputRaw;
    private Vector3 _moveDirection;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();

        _agent.updateRotation = false;

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

        if (_agent.isOnNavMesh == true)
        {
            _agent.velocity = Vector3.zero;
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

        if (Mathf.Abs(_inputRaw.x) > 0.01f)
        {
            transform.Rotate(0f, _inputRaw.x * rotationSpeed * Time.deltaTime, 0f);
        }

        _moveDirection = transform.forward * _inputRaw.y;

        characterAnimatorController.SetMoving(Mathf.Abs(_inputRaw.y) > 0.01f);

        _agent.Move(_moveDirection * moveSpeed * Time.deltaTime);
    }

    public void SetPlayerAgentStopped(bool stopped)
    { 
        _agent.isStopped = stopped;
    }
}
