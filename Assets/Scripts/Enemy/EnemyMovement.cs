using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Enemy chase script, using NavMesh.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovement : MonoBehaviour
{
    public enum EnemyAction
    {
        AttackPlayer,
        Steal,
        Escape,
    }

    [Header("Movement")]
    [SerializeField, Tooltip("")]
    private float stopDistance = 1.5f;
    [SerializeField, Tooltip("")]
    private float updateInterval = 0.2f;

    [Header("AI Behaviour")]
    [SerializeField, Tooltip("")]
    private float chanceToStealOnSpawn = 0.5f;

    [Header("Score")]
    [SerializeField, Tooltip("")]
    private int scoreLostOnEscape = 5;

    public EnemyAction CurrentAction { get; private set; }
    public bool HasStolenGold { get; private set; }
    public int StolenAmount { get; private set; }
    public CharacterHealth CurrentTarget { get; private set; }
    public CharacterHealth Chest => _chest;

    private NavMeshAgent _agent;
    private CharacterHealth _player;
    private CharacterHealth _chest;
    private Vector3 _escapePosition;
    private float _nextUpdateTime;
    private bool _hasSetEscapeDestination;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.stoppingDistance = stopDistance;
    }

    private void Start()
    {
        CharacterHealth[] all = FindObjectsByType<CharacterHealth>(FindObjectsSortMode.None);

        foreach (CharacterHealth h in all)
        {
            if (h.CompareTag("Player") == true)
            {
                _player = h;
            }
            else if (h.CompareTag("Chest") == true)
            {
                _chest = h;
            }
        }

        _escapePosition = transform.position;

        if (Random.value < chanceToStealOnSpawn)
        {
            CurrentAction = EnemyAction.Steal;
        }
        else
        {
            CurrentAction = EnemyAction.AttackPlayer;
        }
    }

    private void Update()
    {
        if (_agent.isOnNavMesh == false)
        {
            return;
        }

        if (Time.time < _nextUpdateTime)
        {
            return;
        }

        _nextUpdateTime = Time.time + updateInterval;

        UpdateAction();
    }

    private void UpdateAction()
    {
        switch (CurrentAction)
        {
            case EnemyAction.AttackPlayer:
                if (_player.IsDead == true)
                {
                    CurrentAction = EnemyAction.Steal;
                    UpdateAction();
                    return;
                }

                CurrentTarget = _player;
                _agent.SetDestination(_player.transform.position);
                break;

            case EnemyAction.Steal:
                if (_chest.IsDead == true)
                {
                    CurrentAction = EnemyAction.Escape;
                    UpdateAction();
                    return;
                }

                CurrentTarget = _chest;
                _agent.SetDestination(_chest.transform.position);
                break;

            case EnemyAction.Escape:
                CurrentTarget = null;

                if (_hasSetEscapeDestination == false)
                {
                    _agent.SetDestination(_escapePosition);
                    _hasSetEscapeDestination = true;
                }

                if (_agent.pathPending == false && _agent.remainingDistance <= _agent.stoppingDistance)
                {
                    if (HasStolenGold == true)
                    {
                        ScoreManager.Instance.RemoveScore(scoreLostOnEscape);
                    }
                    
                    Destroy(gameObject);
                }

                break;
        }
    }

    public void OnSteal(int amount)
    {
        HasStolenGold = true;
        StolenAmount = amount;
        CurrentAction = EnemyAction.Escape;
        CurrentTarget = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
    }
}
