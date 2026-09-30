using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class CannonInteract : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField, Tooltip("")]
    private float holdDurationForUpgrade = 1f;

    [Header("References")]
    [SerializeField, Tooltip("")]
    private CannonController cannon;
    [SerializeField, Tooltip("")]
    private CharacterHealth chestHealth;

    [Header("Upgrade Cost")]
    [SerializeField, Tooltip("")]
    private int upgradeHpCost = 5;

    [Header("Input")]
    [SerializeField, Tooltip("")]
    private InputActionReference interactAction;

    private bool _playerInside;
    private PlayerAmmoCarry _playerAmmo;
    private float _holdTimer;
    private bool _waitingForRelease;
    private bool _upgradeTriggeredThisHold;

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
            _holdTimer = 0f;
            _upgradeTriggeredThisHold = false;
        }
    }

    private void Update()
    {
        if (_playerInside == false)
        {
            return;
        }

        bool isHolding = interactAction.action.IsPressed();

        if (_waitingForRelease == true)
        {
            if (isHolding == false)
            {
                _waitingForRelease = false;
            }

            return;
        }

        if (isHolding == true)
        {
            _holdTimer += Time.deltaTime;

            if (_upgradeTriggeredThisHold == false && _holdTimer >= holdDurationForUpgrade)
            {
                cannon.TryUpgrade(chestHealth, upgradeHpCost);
                _upgradeTriggeredThisHold = true;
                _waitingForRelease = true;
            }

            return;
        }

        // The player unpressed the interact button
        if (_holdTimer > 0f)
        {
            if (_upgradeTriggeredThisHold == false)
            {
                bool reloaded = false;

                if (_playerAmmo != null && _playerAmmo.CarryingAmmo == true && cannon.NeedsReload == true)
                {
                    cannon.Reload();
                    _playerAmmo.ConsumeAll();
                    reloaded = true;
                }

                if (reloaded == false)
                {
                    cannon.TryShoot();
                }
            }

            _holdTimer = 0f;
            _upgradeTriggeredThisHold = false;
        }
    }
}