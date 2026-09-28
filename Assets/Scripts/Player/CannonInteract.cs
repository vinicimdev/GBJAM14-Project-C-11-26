using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class CannonInteract : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField, Tooltip("Tempo (segundos) que precisa segurar pra confirmar o upgrade")]
    private float holdDurationForUpgrade = 1f;

    [Header("References")]
    [SerializeField, Tooltip("")]
    private CannonController cannon;
    [SerializeField, Tooltip("O CharacterHealth do baú — de onde o HP do upgrade sai")]
    private CharacterHealth chestHealth;

    [Header("Upgrade Cost")]
    [SerializeField, Tooltip("Quantos HP do baú custa cada upgrade")]
    private int upgradeHpCost = 5;

    [Header("Input")]
    [SerializeField, Tooltip("")]
    private InputActionReference interactAction;

    private bool _playerInside;
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
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") == true)
        {
            _playerInside = false;
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
                cannon.TryShoot();
            }

            _holdTimer = 0f;
            _upgradeTriggeredThisHold = false;
        }
    }
}