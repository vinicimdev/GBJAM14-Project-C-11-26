using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] string cutsceneScene = "Cutscene";
    [SerializeField] GameObject creditsWindow;

    void Awake() => creditsWindow.SetActive(false);

    public void Play() => SceneManager.LoadScene(cutsceneScene);
    public void OpenCredits() => creditsWindow.SetActive(true);
    public void CloseCredits() => creditsWindow.SetActive(false);
}
