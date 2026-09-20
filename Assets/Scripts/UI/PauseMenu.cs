using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] InputActionReference pause;
    [SerializeField] GameObject pauseWindow;
    [SerializeField] GameObject firstSelected;

    bool isPaused;

    void Awake() => pauseWindow.SetActive(false);

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

    public void Toggle()
    {
        isPaused = !isPaused;
        pauseWindow.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;
        EventSystem.current.SetSelectedGameObject(isPaused ? firstSelected : null);
    }
}
