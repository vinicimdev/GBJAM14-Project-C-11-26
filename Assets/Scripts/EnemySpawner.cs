using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns enemies in pre-defined spots, with a maximum limit of enemis alive
/// and cooldown between spawns.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField, Tooltip("")]
    private GameObject enemyPrefab;

    [Header("Spawn Points")]
    [SerializeField, Tooltip("")]
    private Transform[] spawnPoints;
    [SerializeField, Tooltip("")]
    private bool randomSpawnPoint = true;

    [Header("Spawn Timing")]
    [SerializeField, Tooltip("")]
    private float spawnInterval = 3f;

    [SerializeField, Tooltip("")]
    private int maxAlive = 10;
    [SerializeField, Tooltip("")]
    private bool spawnOnStart = true;

    private readonly List<GameObject> _alive = new();
    private float _nextSpawnTime;
    private int _sequentialIndex;

    private void Start()
    {
        if (spawnOnStart == true)
        {
            TrySpawn();
            _nextSpawnTime = Time.time + spawnInterval;
        }
    }

    private void Update()
    {
        _alive.RemoveAll(e => e == null);

        if (Time.time < _nextSpawnTime)
        {
            return;
        }

        if (_alive.Count >= maxAlive)
        {
            return;
        }

        TrySpawn();
        _nextSpawnTime = Time.time + spawnInterval;
    }

    private void TrySpawn()
    {
        Transform point = ChooseSpawnPoint();
        if (point == null)
        {
            return;
        }

        GameObject enemy = Instantiate(enemyPrefab, point.position, point.rotation);
        _alive.Add(enemy);
    }

    private Transform ChooseSpawnPoint()
    {
        if (randomSpawnPoint)
        {
            return spawnPoints[Random.Range(0, spawnPoints.Length)];
        }

        Transform point = spawnPoints[_sequentialIndex];
        _sequentialIndex = (_sequentialIndex + 1) % spawnPoints.Length;
        return point;
    }

    private void OnDrawGizmos()
    {
        if (spawnPoints == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        foreach (Transform p in spawnPoints)
        {
            if (p == null)
            {
                continue;
            }

            Gizmos.DrawWireSphere(p.position, 0.5f);
            Gizmos.DrawLine(p.position, p.position + Vector3.up * 2f);
        }
    }
}
