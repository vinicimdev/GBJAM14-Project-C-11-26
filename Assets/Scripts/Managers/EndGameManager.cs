using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
    [SerializeField, Tooltip("")]
    private GameObject restartButton;
    [SerializeField, Tooltip("")]
    private GameObject menuButton;

    [Header("Audio")]
    [SerializeField, Tooltip("")]
    private AudioSource sfx;
    [SerializeField, Tooltip("")]
    private AudioClip moveClip;
    [SerializeField, Tooltip("")]
    private AudioClip clickClip;

    [Header("Scenes")]
    [SerializeField, Tooltip("")]
    private string menuSceneName = "Menu";

    private bool isOver;
    private GameObject selected;

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

    private void Update()
    {
        if (!isOver) return;

        GameObject current = EventSystem.current.currentSelectedGameObject;
        if (current != null)
        {
            if (current != selected) Blip(moveClip);
            selected = current;
        }
        else EventSystem.current.SetSelectedGameObject(selected);
    }

    private void DisplayScore()
    {
        scoreText.text = $"Final Score: {ScoreManager.Instance.CurrentScore}";
        panel.SetActive(true);

        Time.timeScale = 0f;

        isOver = true;
        Select(restartButton);
    }

    public void OnRestartClicked() => StartCoroutine(LoadRoutine(SceneManager.GetActiveScene().name));

    public void OnMenuClicked() => StartCoroutine(LoadRoutine(menuSceneName));

    private IEnumerator LoadRoutine(string sceneName)
    {
        Blip(clickClip);
        if (clickClip != null) yield return new WaitForSecondsRealtime(clickClip.length);

        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    private void Blip(AudioClip clip)
    {
        if (sfx != null && clip != null) sfx.PlayOneShot(clip);
    }

    private void Select(GameObject button)
    {
        selected = button;
        EventSystem.current.SetSelectedGameObject(button);
    }
}
