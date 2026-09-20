using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] string cutsceneScene = "Cutscene";
    [SerializeField] GameObject creditsWindow;
    [SerializeField] GameObject playButton;
    [SerializeField] GameObject closeCreditsButton;
    [SerializeField] GameObject howToWindow;
    [SerializeField] GameObject closeHowToButton;

    [SerializeField] AudioSource sfx;
    [SerializeField] AudioClip moveClip;
    [SerializeField] AudioClip clickClip;


    GameObject selected;

    void Awake() => creditsWindow.SetActive(false);

    void Start() => Select(playButton);

    void Update()
    {
        // Navigate goes nowhere while nothing is selected, and the pointer empties it.
        GameObject current = EventSystem.current.currentSelectedGameObject;
        if (current != null)
        {
            if (current != selected) Blip(moveClip);
            selected = current;
        }
        else EventSystem.current.SetSelectedGameObject(selected);
    }

    public void Play() => StartCoroutine(PlayRoutine());

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
        Select(playButton);
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
        Select(playButton);
    }

    IEnumerator PlayRoutine()
    {
        Blip(clickClip);
        if (clickClip != null) yield return new WaitForSecondsRealtime(clickClip.length);

        SceneManager.LoadScene(cutsceneScene);
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
