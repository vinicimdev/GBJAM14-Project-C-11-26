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


    GameObject selected;

    void Awake() => creditsWindow.SetActive(false);

    void Start() => Select(playButton);

    void Update()
    {
        // Navigate goes nowhere while nothing is selected, and the pointer empties it.
        GameObject current = EventSystem.current.currentSelectedGameObject;
        if (current != null) selected = current;
        else EventSystem.current.SetSelectedGameObject(selected);
    }

    public void Play() => SceneManager.LoadScene(cutsceneScene);

    public void OpenCredits()
    {
        creditsWindow.SetActive(true);
        Select(closeCreditsButton);
    }

    public void CloseCredits()
    {
        creditsWindow.SetActive(false);
        Select(playButton);
    }

    public void OpenHowTo()
    {
        howToWindow.SetActive(true);
        Select(closeHowToButton);
    }

    public void CloseHowTo()
    {
        howToWindow.SetActive(false);
        Select(playButton);
    }

    void Select(GameObject button)
    {
        selected = button;
        EventSystem.current.SetSelectedGameObject(button);
    }
}
