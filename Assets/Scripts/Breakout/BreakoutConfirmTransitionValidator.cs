using UnityEngine;

[DisallowMultipleComponent]
public class BreakoutConfirmTransitionValidator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SceneLoader sceneLoader;
    [SerializeField] private BreakoutGameController breakoutGameController;

    private void Awake()
    {
        if (sceneLoader == null)
        {
            sceneLoader = GetComponent<SceneLoader>();
        }

        if (breakoutGameController == null)
        {
            breakoutGameController = FindAnyObjectByType<BreakoutGameController>();
        }
    }

    public void OnConfirmPressed()
    {
        if (breakoutGameController == null)
        {
            breakoutGameController = FindAnyObjectByType<BreakoutGameController>();
        }

        if (breakoutGameController != null)
        {
            breakoutGameController.ClearCollectedMachinesThisLevel(notifyListeners: false);
        }

        if (sceneLoader == null)
        {
            Debug.LogWarning("BreakoutConfirmTransitionValidator has no SceneLoader assigned.", this);
            return;
        }

        bool lifeLost = breakoutGameController != null &&
            (breakoutGameController.LastLevelEndReason == BreakoutGameController.LevelEndReason.OutOfBalls ||
             breakoutGameController.LastLevelEndReason == BreakoutGameController.LevelEndReason.OutOfHealth);

        if (lifeLost && PlayerStats.Instance != null)
            PlayerStats.Instance.ResetHealthForNewLife();

        sceneLoader.LoadTargetScene();
    }
}
