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

    [Header("Input")]
    [SerializeField, Tooltip("")]
    private InputActionReference interactAction;

    public float HoldProgress => holdDuration > 0f ? Mathf.Clamp01(_holdTimer / holdDuration) : 0f;

    public bool HasTakenControl { get; private set; }

    private bool _playerInside;
    private float _holdTimer;
    private bool _waitingForRelease;
    private Transform _playerOriginalParent;

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

    private void TakeControl()
    {
        playerMovement.enabled = false;

        _playerOriginalParent = playerMovement.transform.parent;
        playerMovement.transform.SetParent(shipMovement.transform, worldPositionStays: true);

        if (helmStandPoint != null)
        {
            playerMovement.transform.SetPositionAndRotation(helmStandPoint.position, helmStandPoint.rotation);
        }

        playerRigidbody.linearVelocity = Vector3.zero;
        playerRigidbody.angularVelocity = Vector3.zero;
        playerRigidbody.isKinematic = true;

        shipRigidbody.constraints = RigidbodyConstraints.None;
        shipRigidbody.WakeUp();

        shipMovement.enabled = true;

        cameraFollow.SetTarget(shipMovement.transform, snapCameraOnSwitch);

        HasTakenControl = true;
    }

    private void ReleaseControl()
    {
        playerMovement.transform.SetParent(_playerOriginalParent, worldPositionStays: true);
        playerRigidbody.isKinematic = false;

        shipRigidbody.linearVelocity = Vector3.zero;
        shipRigidbody.angularVelocity = Vector3.zero;
        shipRigidbody.constraints = RigidbodyConstraints.FreezeAll;

        shipMovement.enabled = false;
        playerMovement.enabled = true;

        cameraFollow.SetTarget(playerMovement.transform, snapCameraOnSwitch);

        HasTakenControl = false;
    }
}
