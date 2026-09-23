using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] InputActionReference pause;
    [SerializeField] string menuScene = "Menu";
    [SerializeField] GameObject pauseWindow;
    [SerializeField] GameObject resumeButton;
    [SerializeField] GameObject creditsWindow;
    [SerializeField] GameObject closeCreditsButton;
    [SerializeField] GameObject howToWindow;
    [SerializeField] GameObject closeHowToButton;
    [SerializeField] GameObject settingsButton;
    [SerializeField] SettingsWindow settings;

    [SerializeField] AudioSource sfx;
    [SerializeField] AudioClip moveClip;
    [SerializeField] AudioClip clickClip;

    bool isPaused;
    GameObject selected;

    void Awake()
    {
        pauseWindow.SetActive(false);
        creditsWindow.SetActive(false);
        howToWindow.SetActive(false);
    }

    void OnEnable()
    {
        pause.action.Enable();
        pause.action.performed += OnPause;
    }

    void OnDisable()
    {
        pause.action.performed -= OnPause;
        pause.action.Disable();
    }

    void OnDestroy() => Time.timeScale = 1f;

    void OnPause(InputAction.CallbackContext ctx) => Toggle();

    void Update()
    {
        if (!isPaused) return;

        GameObject current = EventSystem.current.currentSelectedGameObject;
        if (current != null)
        {
            if (current != selected) Blip(moveClip);
            selected = current;
        }
        else EventSystem.current.SetSelectedGameObject(selected);
    }

    public void Toggle()
    {
        isPaused = !isPaused;
        Blip(clickClip);
        Time.timeScale = isPaused ? 0f : 1f;
        pauseWindow.SetActive(isPaused);
        creditsWindow.SetActive(false);
        howToWindow.SetActive(false);
        if (settings != null) settings.Hide();

        if (isPaused) Select(resumeButton);
        else EventSystem.current.SetSelectedGameObject(null);
    }

    public void ToMenu() => StartCoroutine(ToMenuRoutine());

    public void OpenCredits()
    {
        Blip(clickClip);
        creditsWindow.SetActive(true);
        Select(closeCreditsButton);
    }

    public void CloseCredits()
    {
        Blip(clickClip);
        creditsWindow.SetActive(false);
        Select(resumeButton);
    }

    public void OpenHowTo()
    {
        Blip(clickClip);
        howToWindow.SetActive(true);
        Select(closeHowToButton);
    }

    public void CloseHowTo()
    {
        Blip(clickClip);
        howToWindow.SetActive(false);
        Select(resumeButton);
    }

    public void OpenSettings()
    {
        Blip(clickClip);
        pauseWindow.SetActive(false);
        settings.Show();
        Select(settings.FirstRow);
    }

    public void CloseSettings()
    {
        Blip(clickClip);
        settings.Hide();
        pauseWindow.SetActive(true);
        Select(settingsButton);
    }

    IEnumerator ToMenuRoutine()
    {
        Blip(clickClip);
        if (clickClip != null) yield return new WaitForSecondsRealtime(clickClip.length);

        SceneManager.LoadScene(menuScene);
    }

    void Blip(AudioClip clip)
    {
        if (sfx != null && clip != null) sfx.PlayOneShot(clip);
    }

    void Select(GameObject button)
    {
        selected = button;
        EventSystem.current.SetSelectedGameObject(button);
    }
}
