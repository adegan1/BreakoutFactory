using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private GameObject menuObject;

    private bool isPaused;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            isPaused = !isPaused;
            menuObject.SetActive(isPaused);
            Time.timeScale = isPaused ? 0f : 1f;
        }
    }

    public void CloseMenu()
    {
        isPaused = false;
        menuObject.SetActive(false);
        Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
