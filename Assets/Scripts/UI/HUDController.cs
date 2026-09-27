using TMPro;
using UnityEngine;

/// <summary>
/// Updates the top and bottom bar texts: points, player health, total kills, round, round timer and enemies on deck.
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("References")]
    [SerializeField, Tooltip("")]
    private ScoreManager scoreManager;
    [SerializeField, Tooltip("")]
    private RoundManager roundManager;
    [SerializeField, Tooltip("")]
    private EnemySpawner spawner;
    [SerializeField, Tooltip("")]
    private CharacterHealth playerHealth;

    [Header("UI")]
    [SerializeField, Tooltip("")]
    private TMP_Text pointsNumberText;
    [SerializeField, Tooltip("")]
    private TMP_Text playerHealthText;
    [SerializeField, Tooltip("")]
    private TMP_Text totalKillsText;
    [SerializeField, Tooltip("")]
    private TMP_Text roundNumberText;
    [SerializeField, Tooltip("")]
    private TMP_Text timerText;
    [SerializeField, Tooltip("")]
    private TMP_Text timerNumberText;
    [SerializeField, Tooltip("")]
    private TMP_Text enemiesOnDeckText;

    [Header("Timer Labels")]
    [SerializeField, Tooltip("")]
    private string playingLabel = "LFT";
    [SerializeField, Tooltip("")]
    private string clearingLabel = "CLR";
    [SerializeField, Tooltip("")]
    private string cooldownLabel = "CLD";

    private int _shownSeconds = -1;
    private int _shownOnDeck = -1;
    private RoundManager.RoundState? _shownState;

    private void Start()
    {
        UpdateScore(scoreManager.CurrentScore);
        UpdatePlayerHealth(playerHealth.CurrentHealth);
        UpdateKills(scoreManager.TotalKills);
        UpdateRound(roundManager.CurrentRound);
    }

    private void OnEnable()
    {
        scoreManager.OnScoreChanged += UpdateScore;
        scoreManager.OnKillsChanged += UpdateKills;
        playerHealth.OnHealthChanged += UpdatePlayerHealth;
        roundManager.OnRoundChanged += UpdateRound;
    }

    private void OnDisable()
    {
        scoreManager.OnScoreChanged -= UpdateScore;
        scoreManager.OnKillsChanged -= UpdateKills;
        playerHealth.OnHealthChanged -= UpdatePlayerHealth;
        roundManager.OnRoundChanged -= UpdateRound;
    }

    private void Update()
    {
        int seconds = Mathf.CeilToInt(roundManager.TimeRemaining);

        if (seconds != _shownSeconds)
        {
            _shownSeconds = seconds;
            timerNumberText.text = $"{seconds / 60}:{seconds % 60:00}";
        }

        if (roundManager.CurrentState != _shownState)
        {
            _shownState = roundManager.CurrentState;
            timerText.text = roundManager.CurrentState switch
            {
                RoundManager.RoundState.Clearing => clearingLabel,
                RoundManager.RoundState.Cooldown => cooldownLabel,
                _ => playingLabel,
            };
        }

        if (spawner.AliveCount != _shownOnDeck)
        {
            _shownOnDeck = spawner.AliveCount;
            enemiesOnDeckText.text = _shownOnDeck.ToString();
        }
    }

    private void UpdateScore(int score)
    {
        pointsNumberText.text = score.ToString();
    }

    private void UpdatePlayerHealth(int currentHealth)
    {
        playerHealthText.text = currentHealth.ToString();
    }

    private void UpdateKills(int kills)
    {
        totalKillsText.text = kills.ToString("000");
    }

    private void UpdateRound(int round)
    {
        roundNumberText.text = round.ToString("000");
    }
}
