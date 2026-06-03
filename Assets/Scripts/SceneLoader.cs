using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class SceneLoader : MonoBehaviour
{
    [SerializeField] private string targetSceneName = "Breakout";
    private bool isSceneLoadInProgress;

    public void LoadTargetScene()
    {
        LoadSceneByName(targetSceneName);
    }

    public void LoadSceneByName(string sceneName)
    {
        if (isSceneLoadInProgress)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("SceneLoader cannot load an empty scene name.", this);
            return;
        }

        string trimmedSceneName = sceneName.Trim();
        isSceneLoadInProgress = true;
        MusicController.FadeOutBeforeSceneChange(() => SceneManager.LoadScene(trimmedSceneName));
    }
}
