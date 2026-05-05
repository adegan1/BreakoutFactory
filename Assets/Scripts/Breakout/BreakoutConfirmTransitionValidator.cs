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
            breakoutGameController.ClearCollectedMachinesThisLevel();
        }

        if (sceneLoader == null)
        {
            Debug.LogWarning("BreakoutConfirmTransitionValidator has no SceneLoader assigned.", this);
            return;
        }

        sceneLoader.LoadTargetScene();
    }
}
