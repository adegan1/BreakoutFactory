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
    private ItemShop breakoutItemShop;

    private void Awake()
    {
        isBreakoutLevel = FindAnyObjectByType<BreakoutGameController>() != null;
        if (isBreakoutLevel)
        {
            breakoutItemShop = FindAnyObjectByType<ItemShop>();
        }

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
        ApplyPauseMuffle(isMuffled: true);
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

        if (isBreakoutLevel && !IsShopOpen())
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
        ApplyPauseMuffle(isMuffled: false);
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

        ApplyPauseMuffle(isMuffled: false);
        Time.timeScale = 1f;
    }

    private static void ApplyPauseMuffle(bool isMuffled)
    {
        MusicController.SetPauseMuffled(isMuffled);
        BreakoutSoundController.SetPauseMuffled(isMuffled);
        FactorySoundController.SetPauseMuffled(isMuffled);
    }

    private bool IsShopOpen()
    {
        if (breakoutItemShop == null)
        {
            breakoutItemShop = FindAnyObjectByType<ItemShop>();
        }

        return breakoutItemShop != null && breakoutItemShop.IsOpen;
    }
}
