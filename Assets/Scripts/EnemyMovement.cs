using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using static UnityEditor.Experimental.GraphView.GraphView;

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

    private NavMeshAgent _agent;
    private Transform _playerTransform;
    private float _nextUpdateTime;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.stoppingDistance = stopDistance;
    }

    private void Start()
    {
        GameObject player = FindAnyObjectByType<CharacterMovement>().gameObject;

        if (player != null)
        {
            _playerTransform = player.transform;
        }
    }

    private void Update()
    {
        if (_playerTransform == null || _agent.isOnNavMesh == false)
        {
            return;
        }

        if (Time.time < _nextUpdateTime)
        {
            return;
        }

        _nextUpdateTime = Time.time + updateInterval;

        float dist = Vector3.Distance(transform.position, _playerTransform.position);

        if (dist <= detectionRange)
        {
            _agent.SetDestination(_playerTransform.position);
        }
        else if (_agent.hasPath)
        {
            _agent.ResetPath();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
    }
}
