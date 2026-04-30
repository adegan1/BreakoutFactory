using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class SceneLoader : MonoBehaviour
{
    [SerializeField] private string targetSceneName = "Breakout";

    public void LoadTargetScene()
    {
        LoadSceneByName(targetSceneName);
    }

    public void LoadSceneByName(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("SceneLoader cannot load an empty scene name.", this);
            return;
        }

        SceneManager.LoadScene(sceneName.Trim());
    }
}
