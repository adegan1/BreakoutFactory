using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private GameObject menuObject;
    [Header("Breakout Resume Countdown")]
    [SerializeField] private TextMeshProUGUI resumeCountdownText;
    [SerializeField, Min(1)] private int breakoutResumeCountdownSeconds = 3;

    private bool isPaused;
    private bool isResumingWithCountdown;
    private Coroutine resumeCountdownRoutine;
    private bool isBreakoutLevel;

    private void Awake()
    {
        isBreakoutLevel = FindAnyObjectByType<BreakoutGameController>() != null;

        if (resumeCountdownText != null)
        {
            resumeCountdownText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (isResumingWithCountdown)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused)
            {
                CloseMenu();
            }
            else
            {
                OpenMenu();
            }
        }
    }

    private void OpenMenu()
    {
        isPaused = true;
        if (isBreakoutLevel)
        {
            BreakoutSoundController.PlayPauseMenuOpenSfx();
        }
        else
        {
            FactorySoundController.PlayPauseMenuOpenSfx();
        }

        if (menuObject != null)
        {
            menuObject.SetActive(true);
        }

        Time.timeScale = 0f;
    }

    public void CloseMenu()
    {
        if (isResumingWithCountdown)
        {
            return;
        }

        if (isBreakoutLevel)
        {
            resumeCountdownRoutine = StartCoroutine(ResumeWithCountdownCoroutine());
            return;
        }

        ResumeImmediately();
    }

    private IEnumerator ResumeWithCountdownCoroutine()
    {
        isResumingWithCountdown = true;
        if (menuObject != null)
        {
            menuObject.SetActive(false);
        }

        int seconds = Mathf.Max(1, breakoutResumeCountdownSeconds);
        if (resumeCountdownText != null)
        {
            resumeCountdownText.gameObject.SetActive(true);
        }

        for (int i = seconds; i > 0; i--)
        {
            if (resumeCountdownText != null)
            {
                resumeCountdownText.text = i.ToString();
            }

            BreakoutSoundController.PlayPauseResumeCountdownTickSfx();

            yield return new WaitForSecondsRealtime(1f);
        }

        if (resumeCountdownText != null)
        {
            resumeCountdownText.gameObject.SetActive(false);
        }

        ResumeImmediately();
        isResumingWithCountdown = false;
        resumeCountdownRoutine = null;
    }

    private void ResumeImmediately()
    {
        isPaused = false;
        if (menuObject != null)
        {
            menuObject.SetActive(false);
        }

        Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        if (resumeCountdownRoutine != null)
        {
            StopCoroutine(resumeCountdownRoutine);
            resumeCountdownRoutine = null;
        }

        if (resumeCountdownText != null)
        {
            resumeCountdownText.gameObject.SetActive(false);
        }

        Time.timeScale = 1f;
    }
}
