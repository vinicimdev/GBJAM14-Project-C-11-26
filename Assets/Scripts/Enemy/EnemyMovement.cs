using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Enemy chase script, using NavMesh.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField, Tooltip("")]
    private float detectionRange = 20f;

    [Header("Movement")]
    [SerializeField, Tooltip("")]
    private float stopDistance = 1.5f;
    [SerializeField, Tooltip("")]
    private float updateInterval = 0.2f;

    public CharacterHealth CurrentTarget { get; private set; }

    private NavMeshAgent _agent;
    private CharacterHealth[] _potentialTargets;
    private float _nextUpdateTime;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.stoppingDistance = stopDistance;
    }

    private void Start()
    {
        _potentialTargets = FindObjectsByType<CharacterHealth>(FindObjectsSortMode.None);

        if (_agent.isOnNavMesh == false)
        {
            Debug.LogWarning($"[{nameof(EnemyMovement)}] '{name}' spawned outside of the NavMesh and won't walk.");
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

        CurrentTarget = FindClosestTarget();

        if (CurrentTarget == null)
        {
            if (_agent.hasPath == true)
            {
                _agent.ResetPath();
            }

            return;
        }

        float dist = Vector3.Distance(transform.position, CurrentTarget.transform.position);

        if (dist <= detectionRange)
        {
            _agent.SetDestination(CurrentTarget.transform.position);
        }
        else if (_agent.hasPath == true)
        {
            _agent.ResetPath();
        }
    }

    private CharacterHealth FindClosestTarget()
    {
        CharacterHealth closest = null;
        float closestDist = float.MaxValue;

        foreach (CharacterHealth target in _potentialTargets)
        {

            if (target == null)
            {
                continue;
            }

            if (target.IsDead == true)
            {
                continue;
            }

            float dist = Vector3.Distance(transform.position, target.transform.position);

            if (dist < closestDist)
            {
                closestDist = dist;
                closest = target;
            }
        }

        return closest;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
    }
}
