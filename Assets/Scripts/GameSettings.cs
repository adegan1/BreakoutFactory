using UnityEngine;
using System;

[DisallowMultipleComponent]
public class GameSettings : MonoBehaviour
{
    public enum Language
    {
        English = 0,
        Japanese = 1
    }

    public enum AudioBus
    {
        Music,
        Sfx,
        Ambience
    }

    private const string ShowInfoKey = "GameSettings.ShowInfo";
    private const string ShowControlsKey = "GameSettings.ShowControls";
    private const string FactorySpeedIsDoubleKey = "GameSettings.FactorySpeedIsDouble";
    private const string FactoryAutoPauseKey = "GameSettings.FactoryAutoPause";
    private const string MasterVolumeKey = "GameSettings.Audio.MasterVolume";
    private const string MusicVolumeKey = "GameSettings.Audio.MusicVolume";
    private const string SfxVolumeKey = "GameSettings.Audio.SfxVolume";
    private const string AmbienceVolumeKey = "GameSettings.Audio.AmbienceVolume";
    private const string LanguageKey = "GameSettings.Language";

    private static GameSettings instance;

    [Header("Defaults")]
    [SerializeField] private bool defaultShowInfo = true;
    [SerializeField] private bool defaultShowControls = true;
    [SerializeField] private bool defaultFactorySpeedIsDouble;
    [SerializeField] private bool defaultFactoryAutoPause = true;
    [SerializeField, Range(0f, 1f)] private float defaultMasterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float defaultMusicVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float defaultSfxVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float defaultAmbienceVolume = 1f;
    [SerializeField] private Language defaultLanguage = Language.English;

    [Header("Runtime")]
    [SerializeField] private bool showInfo = true;
    [SerializeField] private bool showControls = true;
    [SerializeField] private bool factorySpeedIsDouble;
    [SerializeField] private bool factoryAutoPause = true;
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float ambienceVolume = 1f;
    [SerializeField] private Language language = Language.English;

    public static bool HasInstance => instance != null;
    public static GameSettings Instance => EnsureInstance();
    public static event Action<Language> LanguageChanged;

    public bool ShowInfo => showInfo;
    public bool ShowControls => showControls;
    public bool FactorySpeedIsDouble => factorySpeedIsDouble;
    public bool FactoryAutoPause => factoryAutoPause;
    public float MasterVolume => masterVolume;
    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;
    public float AmbienceVolume => ambienceVolume;
    public Language CurrentLanguage => language;

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

    public void SetMasterVolume(float value)
    {
        float clampedValue = Mathf.Clamp01(value);
        if (Mathf.Approximately(masterVolume, clampedValue))
        {
            return;
        }

        masterVolume = clampedValue;
        PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
        PlayerPrefs.Save();
    }

    public void SetMusicVolume(float value)
    {
        float clampedValue = Mathf.Clamp01(value);
        if (Mathf.Approximately(musicVolume, clampedValue))
        {
            return;
        }

        musicVolume = clampedValue;
        PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
        PlayerPrefs.Save();
    }

    public void SetSfxVolume(float value)
    {
        float clampedValue = Mathf.Clamp01(value);
        if (Mathf.Approximately(sfxVolume, clampedValue))
        {
            return;
        }

        sfxVolume = clampedValue;
        PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
        PlayerPrefs.Save();
    }

    public void SetAmbienceVolume(float value)
    {
        float clampedValue = Mathf.Clamp01(value);
        if (Mathf.Approximately(ambienceVolume, clampedValue))
        {
            return;
        }

        ambienceVolume = clampedValue;
        PlayerPrefs.SetFloat(AmbienceVolumeKey, ambienceVolume);
        PlayerPrefs.Save();
    }

    public void SetLanguage(Language value)
    {
        if (language == value)
        {
            return;
        }

        language = value;
        PlayerPrefs.SetInt(LanguageKey, (int)language);
        PlayerPrefs.Save();
        LanguageChanged?.Invoke(language);
    }

    public void SetLanguageEnglish()
    {
        SetLanguage(Language.English);
    }

    public void SetLanguageJapanese()
    {
        SetLanguage(Language.Japanese);
    }

    public void ToggleLanguage()
    {
        SetLanguage(language == Language.English ? Language.Japanese : Language.English);
    }

    public float GetBusVolume(AudioBus bus)
    {
        switch (bus)
        {
            case AudioBus.Music:
                return Mathf.Clamp01(musicVolume);
            case AudioBus.Ambience:
                return Mathf.Clamp01(ambienceVolume);
            case AudioBus.Sfx:
            default:
                return Mathf.Clamp01(sfxVolume);
        }
    }

    public float GetCombinedVolume(AudioBus bus)
    {
        return Mathf.Clamp01(masterVolume) * GetBusVolume(bus);
    }

    public static float GetCombinedVolumeMultiplier(AudioBus bus)
    {
        return Instance.GetCombinedVolume(bus);
    }

    public void ResetToDefaults()
    {
        showInfo = defaultShowInfo;
        showControls = defaultShowControls;
        factorySpeedIsDouble = defaultFactorySpeedIsDouble;
        factoryAutoPause = defaultFactoryAutoPause;
        masterVolume = Mathf.Clamp01(defaultMasterVolume);
        musicVolume = Mathf.Clamp01(defaultMusicVolume);
        sfxVolume = Mathf.Clamp01(defaultSfxVolume);
        ambienceVolume = Mathf.Clamp01(defaultAmbienceVolume);
        language = defaultLanguage;

        PlayerPrefs.DeleteKey(ShowInfoKey);
        PlayerPrefs.DeleteKey(ShowControlsKey);
        PlayerPrefs.DeleteKey(FactorySpeedIsDoubleKey);
        PlayerPrefs.DeleteKey(FactoryAutoPauseKey);
        PlayerPrefs.DeleteKey(MasterVolumeKey);
        PlayerPrefs.DeleteKey(MusicVolumeKey);
        PlayerPrefs.DeleteKey(SfxVolumeKey);
        PlayerPrefs.DeleteKey(AmbienceVolumeKey);
        PlayerPrefs.DeleteKey(LanguageKey);
        PlayerPrefs.Save();

        LanguageChanged?.Invoke(language);
    }

    public void ResetRetainedDataToDefaultsPreservingAudioAndLanguage()
    {
        showInfo = defaultShowInfo;
        showControls = defaultShowControls;
        factorySpeedIsDouble = defaultFactorySpeedIsDouble;
        factoryAutoPause = defaultFactoryAutoPause;

        PlayerPrefs.DeleteKey(ShowInfoKey);
        PlayerPrefs.DeleteKey(ShowControlsKey);
        PlayerPrefs.DeleteKey(FactorySpeedIsDoubleKey);
        PlayerPrefs.DeleteKey(FactoryAutoPauseKey);
        PlayerPrefs.Save();
    }

    private void LoadFromPrefs()
    {
        showInfo = PlayerPrefs.GetInt(ShowInfoKey, defaultShowInfo ? 1 : 0) != 0;
        showControls = PlayerPrefs.GetInt(ShowControlsKey, defaultShowControls ? 1 : 0) != 0;
        factorySpeedIsDouble = PlayerPrefs.GetInt(FactorySpeedIsDoubleKey, defaultFactorySpeedIsDouble ? 1 : 0) != 0;
        factoryAutoPause = PlayerPrefs.GetInt(FactoryAutoPauseKey, defaultFactoryAutoPause ? 1 : 0) != 0;
        masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, Mathf.Clamp01(defaultMasterVolume)));
        musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, Mathf.Clamp01(defaultMusicVolume)));
        sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, Mathf.Clamp01(defaultSfxVolume)));
        ambienceVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(AmbienceVolumeKey, Mathf.Clamp01(defaultAmbienceVolume)));

        int languageValue = PlayerPrefs.GetInt(LanguageKey, (int)defaultLanguage);
        if (!Enum.IsDefined(typeof(Language), languageValue))
        {
            languageValue = (int)defaultLanguage;
        }

        language = (Language)languageValue;
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
