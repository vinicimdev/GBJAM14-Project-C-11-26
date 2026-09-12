using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Allows the player to switch between controlling the character and the ship.
/// </summary>
public class ShipControlInteract : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField, Tooltip("")]
    private float holdDuration = 1f;

    [Header("Player")]
    [SerializeField, Tooltip("")]
    private CharacterMovement playerMovement;
    [SerializeField, Tooltip("")]
    private Rigidbody playerRigidbody;
    [SerializeField, Tooltip("")]
    private Collider playerCollider;
    [SerializeField, Tooltip("")]
    private Transform helmStandPoint;

    [Header("Ship")]
    [SerializeField, Tooltip("")]
    private CharacterMovement shipMovement;
    [SerializeField, Tooltip("")]
    private Rigidbody shipRigidbody;

    [Header("Camera")]
    [SerializeField, Tooltip("")]
    private CameraFollow cameraFollow;
    [SerializeField, Tooltip("")]
    private bool snapCameraOnSwitch = false;
    [SerializeField, Tooltip("")]
    private float shipCameraZoomMultiplier = 1.5f;

    [Header("Input")]
    [SerializeField, Tooltip("")]
    private InputActionReference interactAction;

    public float HoldProgress => holdDuration > 0f ? Mathf.Clamp01(_holdTimer / holdDuration) : 0f;

    public bool HasTakenControl { get; private set; }

    private bool _playerInside;
    private float _holdTimer;
    private bool _waitingForRelease;
    private Vector3 _playerLocalPos;
    private Quaternion _playerLocalRot;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnEnable()
    {
        if (interactAction != null)
        {
            interactAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (interactAction != null)
        {
            interactAction.action.Disable();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInside = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInside = false;
            _holdTimer = 0f;
        }
    }

    private void Update()
    {
        if (_playerInside == false && HasTakenControl == false)
        {
            return;
        }

        bool isHolding = interactAction.action.IsPressed();

        if (isHolding == false)
        {
            _waitingForRelease = false;

            if (_holdTimer > 0f)
            {
                _holdTimer = 0f;
            }

            return;
        }

        if (_waitingForRelease == true)
        {
            return;
        }

        _holdTimer += Time.deltaTime;

        if (_holdTimer > holdDuration)
        {
            if (HasTakenControl == true)
            {
                ReleaseControl();
            }
            else
            {
                TakeControl();
            }

            _holdTimer = 0f;
            _waitingForRelease = true;
        }
    }

    private void FixedUpdate()
    {
        if (HasTakenControl == false)
        {
            return;
        }

        Vector3 worldPos = shipMovement.transform.TransformPoint(_playerLocalPos);
        Quaternion worldRot = shipMovement.transform.rotation * _playerLocalRot;

        playerRigidbody.MovePosition(worldPos);
        playerRigidbody.MoveRotation(worldRot);
    }

    private void TakeControl()
    {
        playerMovement.enabled = false;

        Vector3 targetPos;
        Quaternion targetRot;

        if (helmStandPoint != null)
        {
            targetPos = helmStandPoint.position;
        }
        else
        {
            targetPos = playerMovement.transform.position;
        }

        if (helmStandPoint != null)
        {
            targetRot = helmStandPoint.rotation;
        }
        else
        {
            targetRot = playerMovement.transform.rotation;
        }

        _playerLocalPos = shipMovement.transform.InverseTransformPoint(targetPos);
        _playerLocalRot = Quaternion.Inverse(shipMovement.transform.rotation) * targetRot;

        playerRigidbody.linearVelocity = Vector3.zero;
        playerRigidbody.angularVelocity = Vector3.zero;
        playerRigidbody.isKinematic = true;

        playerRigidbody.position = targetPos;
        playerRigidbody.rotation = targetRot;

        playerCollider.enabled = false;

        shipRigidbody.constraints = RigidbodyConstraints.None;
        shipRigidbody.WakeUp();

        shipMovement.enabled = true;

        cameraFollow.SetTarget(shipMovement.transform, snapCameraOnSwitch);
        cameraFollow.SetOffsetMultiplier(shipCameraZoomMultiplier);

        HasTakenControl = true;
    }

    private void ReleaseControl()
    {
        playerCollider.enabled = true;

        playerRigidbody.isKinematic = false;

        shipRigidbody.linearVelocity = Vector3.zero;
        shipRigidbody.angularVelocity = Vector3.zero;
        shipRigidbody.constraints = RigidbodyConstraints.FreezeAll;

        shipMovement.enabled = false;
        playerMovement.enabled = true;

        cameraFollow.SetTarget(playerMovement.transform, snapCameraOnSwitch);
        cameraFollow.SetOffsetMultiplier(1f);

        HasTakenControl = false;
    }
}
