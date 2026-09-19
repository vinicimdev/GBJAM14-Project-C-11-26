using System;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    [Header("Round Settings")]
    [SerializeField, Tooltip("")]
    private float roundDuration = 15f;
    [SerializeField, Tooltip("")]
    private int startingMaxAlive = 2;
    [SerializeField, Tooltip("")]
    private int enemiesAddedPerRound = 1;
    [SerializeField, Tooltip("")]
    private float startingSpawnInterval = 5f;
    [SerializeField, Tooltip("")]
    private float spawnIntervalMultiplier = 0.3f;
    [SerializeField, Tooltip("")]
    private float minSpawnInterval = 0.5f;

    [Header("References")]
    [SerializeField, Tooltip("")]
    private EnemySpawner spawner;

    public int CurrentRound { get; private set; } = 1;

    public event Action<int> OnRoundChanged;

    private float _roundTimer;

    private void Start()
    {
        ApplyRoundSettings();
    }

    private void Update()
    {
        _roundTimer += Time.deltaTime;

        if (_roundTimer >= roundDuration)
        {
            _roundTimer = 0f;
            CurrentRound++;
            ApplyRoundSettings();
            OnRoundChanged?.Invoke(CurrentRound);
        }
    }

    private void ApplyRoundSettings()
    {
        int maxAlive = startingMaxAlive + (CurrentRound - 1) * enemiesAddedPerRound;
        float spawnInterval = Mathf.Max(minSpawnInterval, startingSpawnInterval - (CurrentRound - 1) * spawnIntervalMultiplier);

        spawner.SetMaxAlive(maxAlive);
        spawner.SetSpawnInterval(spawnInterval);
    }
}