using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Displays a panel with the final score and two buttons to restart the game or go to the main menu scene.
/// </summary>
public class EndGameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField, Tooltip("")]
    private CharacterHealth chestHealth;

    [Header("UI")]
    [SerializeField, Tooltip("")]
    private GameObject panel;
    [SerializeField, Tooltip("")]
    private TMP_Text scoreText;

    [Header("Scenes")]
    [SerializeField, Tooltip("")]
    private string menuSceneName = "Menu";

    private void Awake()
    {
        panel.SetActive(false);
    }

    private void OnEnable()
    {
        chestHealth.OnDeath += DisplayScore;
    }

    private void OnDisable()
    {
        chestHealth.OnDeath -= DisplayScore;
    }

    private void DisplayScore()
    {
        scoreText.text = $"Final Score: {ScoreManager.Instance.CurrentScore}";
        panel.SetActive(true);

        Time.timeScale = 0f;
    }

    public void OnRestartClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnMenuClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }
}
