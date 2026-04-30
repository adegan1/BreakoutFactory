using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class FactoryConfirmTransitionValidator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SceneLoader sceneLoader;
    [SerializeField] private TMP_Text errorText;

    [Header("Validation")]
    [SerializeField, Min(1)] private int minimumBallsRequired = 1;
    [SerializeField] private string noBallsMessage = "Cannot continue with no balls.";
    [SerializeField] private BallTypeData defaultBallType;
    [SerializeField] private bool includeDefaultBallPerEmptyMold = true;

    [Header("Error Animation")]
    [SerializeField, Min(0f)] private float fadeInSeconds = 0.15f;
    [SerializeField, Min(0f)] private float visibleSeconds = 1.5f;
    [SerializeField, Min(0f)] private float fadeOutSeconds = 0.25f;

    private Coroutine errorRoutine;
    private Color errorBaseColor = Color.white;

    private void Awake()
    {
        if (sceneLoader == null)
        {
            sceneLoader = GetComponent<SceneLoader>();
        }

        if (errorText != null)
        {
            errorBaseColor = errorText.color;
            SetErrorAlpha(0f);
        }
    }

    public void OnConfirmPressed()
    {
        List<BallTypeData> transitionBalls = BuildTransitionBallQueue();
        if (transitionBalls.Count < minimumBallsRequired)
        {
            ShowError(noBallsMessage);
            return;
        }

        InventoryManager.Instance.SetCraftedBalls(transitionBalls);

        if (sceneLoader == null)
        {
            Debug.LogWarning("FactoryConfirmTransitionValidator has no SceneLoader assigned.", this);
            return;
        }

        sceneLoader.LoadTargetScene();
    }

    public bool CanContinueToNextScene()
    {
        return BuildTransitionBallQueue().Count >= minimumBallsRequired;
    }

    private List<BallTypeData> BuildTransitionBallQueue()
    {
        List<BallTypeData> queue = new List<BallTypeData>();
        if (!InventoryManager.HasInstance)
        {
            return queue;
        }

        IReadOnlyList<BallTypeData> craftedBalls = InventoryManager.Instance.CraftedBalls;
        for (int i = 0; i < craftedBalls.Count; i++)
        {
            if (craftedBalls[i] != null)
            {
                queue.Add(craftedBalls[i]);
            }
        }

        if (!includeDefaultBallPerEmptyMold || defaultBallType == null)
        {
            return queue;
        }

        BallMoldBuilding[] molds = FindObjectsByType<BallMoldBuilding>(FindObjectsSortMode.None);
        int moldCount = molds != null ? molds.Length : 0;
        while (queue.Count < moldCount)
        {
            queue.Add(defaultBallType);
        }

        return queue;
    }

    public void ShowError(string message)
    {
        if (errorText == null)
        {
            Debug.LogWarning(message, this);
            return;
        }

        errorText.text = message;

        if (errorRoutine != null)
        {
            StopCoroutine(errorRoutine);
        }

        errorRoutine = StartCoroutine(AnimateErrorText());
    }

    private IEnumerator AnimateErrorText()
    {
        yield return FadeTo(1f, fadeInSeconds);

        if (visibleSeconds > 0f)
        {
            yield return new WaitForSeconds(visibleSeconds);
        }

        yield return FadeTo(0f, fadeOutSeconds);
        errorRoutine = null;
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (errorText == null)
        {
            yield break;
        }

        float startAlpha = errorText.color.a;
        if (duration <= 0f)
        {
            SetErrorAlpha(targetAlpha);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            SetErrorAlpha(alpha);
            yield return null;
        }

        SetErrorAlpha(targetAlpha);
    }

    private void SetErrorAlpha(float alpha)
    {
        if (errorText == null)
        {
            return;
        }

        Color color = errorBaseColor;
        color.a = Mathf.Clamp01(alpha);
        errorText.color = color;
    }
}
