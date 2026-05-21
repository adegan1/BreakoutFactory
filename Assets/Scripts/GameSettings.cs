using UnityEngine;

[DisallowMultipleComponent]
public class GameSettings : MonoBehaviour
{
    private const string ShowInfoKey = "GameSettings.ShowInfo";
    private const string ShowControlsKey = "GameSettings.ShowControls";
    private const string FactorySpeedIsDoubleKey = "GameSettings.FactorySpeedIsDouble";
    private const string FactoryAutoPauseKey = "GameSettings.FactoryAutoPause";

    private static GameSettings instance;

    [Header("Defaults")]
    [SerializeField] private bool defaultShowInfo = true;
    [SerializeField] private bool defaultShowControls = true;
    [SerializeField] private bool defaultFactorySpeedIsDouble;
    [SerializeField] private bool defaultFactoryAutoPause = true;

    [Header("Runtime")]
    [SerializeField] private bool showInfo = true;
    [SerializeField] private bool showControls = true;
    [SerializeField] private bool factorySpeedIsDouble;
    [SerializeField] private bool factoryAutoPause = true;

    public static bool HasInstance => instance != null;
    public static GameSettings Instance => EnsureInstance();

    public bool ShowInfo => showInfo;
    public bool ShowControls => showControls;
    public bool FactorySpeedIsDouble => factorySpeedIsDouble;
    public bool FactoryAutoPause => factoryAutoPause;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        LoadFromPrefs();
    }

    public void SetShowInfo(bool value)
    {
        if (showInfo == value)
        {
            return;
        }

        showInfo = value;
        PlayerPrefs.SetInt(ShowInfoKey, showInfo ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetShowControls(bool value)
    {
        if (showControls == value)
        {
            return;
        }

        showControls = value;
        PlayerPrefs.SetInt(ShowControlsKey, showControls ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetFactorySpeedIsDouble(bool value)
    {
        if (factorySpeedIsDouble == value)
        {
            return;
        }

        factorySpeedIsDouble = value;
        PlayerPrefs.SetInt(FactorySpeedIsDoubleKey, factorySpeedIsDouble ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetFactoryAutoPause(bool value)
    {
        if (factoryAutoPause == value)
        {
            return;
        }

        factoryAutoPause = value;
        PlayerPrefs.SetInt(FactoryAutoPauseKey, factoryAutoPause ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void LoadFromPrefs()
    {
        showInfo = PlayerPrefs.GetInt(ShowInfoKey, defaultShowInfo ? 1 : 0) != 0;
        showControls = PlayerPrefs.GetInt(ShowControlsKey, defaultShowControls ? 1 : 0) != 0;
        factorySpeedIsDouble = PlayerPrefs.GetInt(FactorySpeedIsDoubleKey, defaultFactorySpeedIsDouble ? 1 : 0) != 0;
        factoryAutoPause = PlayerPrefs.GetInt(FactoryAutoPauseKey, defaultFactoryAutoPause ? 1 : 0) != 0;
    }

    private static GameSettings EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        instance = FindFirstObjectByType<GameSettings>();
        if (instance == null)
        {
            GameObject settingsObject = new GameObject("GameSettings");
            instance = settingsObject.AddComponent<GameSettings>();
        }

        return instance;
    }
}
