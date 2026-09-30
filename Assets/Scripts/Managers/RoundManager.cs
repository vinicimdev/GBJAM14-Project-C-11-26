using System;
using UnityEngine;

/// <summary>
/// Runs the round loop: a timed round, then waits for the last enemy on deck,
/// then a short cooldown before the next, longer round.
/// </summary>
public class RoundManager : MonoBehaviour
{
    public enum RoundState
    {
        Playing,
        Clearing,
        Cooldown,
    }

    [Header("Round Settings")]
    [SerializeField, Tooltip("")]
    private float roundDuration = 15f;
    [SerializeField, Tooltip("")]
    private float durationAddedPerRound = 5f;
    [SerializeField, Tooltip("")]
    private float cooldownDuration = 3f;
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
    public RoundState CurrentState { get; private set; }
    public float TimeRemaining { get; private set; }

    public event Action<int> OnRoundChanged;

    private void Start()
    {
        StartRound();
    }

    private void Update()
    {
        switch (CurrentState)
        {
            case RoundState.Playing:
                TimeRemaining = Mathf.Max(0f, TimeRemaining - Time.deltaTime);

                if (TimeRemaining <= 0f)
                {
                    spawner.SetSpawning(false);
                    CurrentState = RoundState.Clearing;
                }

                break;

            case RoundState.Clearing:
                if (spawner.AliveCount == 0)
                {
                    TimeRemaining = cooldownDuration;
                    CurrentState = RoundState.Cooldown;
                }

                break;

            case RoundState.Cooldown:
                TimeRemaining = Mathf.Max(0f, TimeRemaining - Time.deltaTime);

                if (TimeRemaining <= 0f)
                {
                    CurrentRound++;
                    StartRound();
                }

                break;
        }
    }

    private void StartRound()
    {
        ApplyRoundSettings();

        TimeRemaining = roundDuration + (CurrentRound - 1) * durationAddedPerRound;
        CurrentState = RoundState.Playing;

        spawner.SetSpawning(true);

        OnRoundChanged?.Invoke(CurrentRound);
    }

    private void ApplyRoundSettings()
    {
        int maxAlive = startingMaxAlive + (CurrentRound - 1) * enemiesAddedPerRound;
        float spawnInterval = Mathf.Max(minSpawnInterval, startingSpawnInterval - (CurrentRound - 1) * spawnIntervalMultiplier);

        spawner.SetMaxAlive(maxAlive);
        spawner.SetSpawnInterval(spawnInterval);
    }
}