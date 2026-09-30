using UnityEngine;
using UnityEngine.InputSystem;

public class AmmoPileInteract : MonoBehaviour
{
    [Header("Input")]
    [SerializeField, Tooltip("")]
    private InputActionReference interactAction;

    private bool _playerInside;
    private PlayerAmmoCarry _playerAmmo;
    private bool _waitingForRelease;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnEnable()
    {
        interactAction.action.Enable();
    }

    private void OnDisable()
    {
        interactAction.action.Disable();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") == true)
        {
            _playerInside = true;
            _playerAmmo = other.GetComponent<PlayerAmmoCarry>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") == true)
        {
            _playerInside = false;
            _playerAmmo = null;
            _waitingForRelease = false;
        }
    }

    private void Update()
    {
        if (_playerInside == false)
        {
            return;
        }

        bool isPressed = interactAction.action.IsPressed();

        if (_waitingForRelease == true)
        {
            if (isPressed == false)
            {
                _waitingForRelease = false;
            }

            return;
        }

        if (isPressed == true && _playerAmmo != null)
        {
            _playerAmmo.PickUp();
            _waitingForRelease = true;
        }
    }
}