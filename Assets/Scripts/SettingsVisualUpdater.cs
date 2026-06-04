using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

[DisallowMultipleComponent]
public class SettingsVisualUpdater : MonoBehaviour
{
    [System.Serializable]
    private struct LanguageButtonVisual
    {
        [SerializeField] private GameSettings.Language language;
        [SerializeField] private Graphic targetGraphic;

        public GameSettings.Language Language => language;
        public Graphic TargetGraphic => targetGraphic;
    }

    [Header("Audio Sliders")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Slider ambienceVolumeSlider;

    [Header("Language Buttons")]
    [SerializeField] private LanguageButtonVisual[] languageButtonVisuals;
    [SerializeField] private Color selectedLanguageColor = Color.white;
    [SerializeField] private Color unselectedLanguageColor = Color.gray;

    [Header("Localization Fonts")]
    [SerializeField] private TMP_FontAsset englishFont;
    [SerializeField] private TMP_FontAsset japaneseFont;
    [SerializeField] private bool keepOriginalEnglishFont = true;

    private readonly List<(Button button, UnityAction action)> wiredButtonListeners = new List<(Button, UnityAction)>();
    private bool listenersWired;

    private void Start()
    {
        ApplyLocalizationFontSettings();
        WireListeners();
        SyncUiFromSettings();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        GameSettings.LanguageChanged += HandleLanguageChanged;
        ApplyLocalizationFontSettings();
        SyncUiFromSettings();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        GameSettings.LanguageChanged -= HandleLanguageChanged;
    }

    private void OnDestroy()
    {
        if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.RemoveListener(SetMasterVolume);
        if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.RemoveListener(SetMusicVolume);
        if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.RemoveListener(SetSfxVolume);
        if (ambienceVolumeSlider != null) ambienceVolumeSlider.onValueChanged.RemoveListener(SetAmbienceVolume);

        for (int i = 0; i < wiredButtonListeners.Count; i++)
        {
            if (wiredButtonListeners[i].button != null)
            {
                wiredButtonListeners[i].button.onClick.RemoveListener(wiredButtonListeners[i].action);
            }
        }

        wiredButtonListeners.Clear();
    }

    private void WireListeners()
    {
        if (listenersWired)
        {
            return;
        }

        listenersWired = true;

        if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.AddListener(SetSfxVolume);
        if (ambienceVolumeSlider != null) ambienceVolumeSlider.onValueChanged.AddListener(SetAmbienceVolume);

        if (languageButtonVisuals == null)
        {
            return;
        }

        for (int i = 0; i < languageButtonVisuals.Length; i++)
        {
            Graphic graphic = languageButtonVisuals[i].TargetGraphic;
            if (graphic == null)
            {
                continue;
            }

            Button button = graphic.GetComponentInParent<Button>(true);
            if (button == null)
            {
                continue;
            }

            GameSettings.Language lang = languageButtonVisuals[i].Language;
            UnityAction action = () => SetLanguage(lang);
            button.onClick.AddListener(action);
            wiredButtonListeners.Add((button, action));
        }
    }

    private void HandleSceneLoaded(Scene _, LoadSceneMode __)
    {
        SyncUiFromSettings();
    }

    private void HandleLanguageChanged(GameSettings.Language _)
    {
        SyncLanguageVisualsFromSettings();
    }

    public void SyncUiFromSettings()
    {
        SyncAudioSlidersFromSettings();
        SyncLanguageVisualsFromSettings();
    }

    private void SyncAudioSlidersFromSettings()
    {
        GameSettings settings = GameSettings.Instance;
        if (settings == null)
        {
            return;
        }

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.SetValueWithoutNotify(settings.MasterVolume);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.SetValueWithoutNotify(settings.MusicVolume);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.SetValueWithoutNotify(settings.SfxVolume);
        }

        if (ambienceVolumeSlider != null)
        {
            ambienceVolumeSlider.SetValueWithoutNotify(settings.AmbienceVolume);
        }
    }

    public void SetMasterVolume(float value)
    {
        GameSettings.Instance.SetMasterVolume(value);
    }

    public void SetMusicVolume(float value)
    {
        GameSettings.Instance.SetMusicVolume(value);
    }

    public void SetSfxVolume(float value)
    {
        GameSettings.Instance.SetSfxVolume(value);
    }

    public void SetAmbienceVolume(float value)
    {
        GameSettings.Instance.SetAmbienceVolume(value);
    }

    public void SetLanguageEnglish()
    {
        SetLanguage(GameSettings.Language.English);
    }

    public void SetLanguageJapanese()
    {
        SetLanguage(GameSettings.Language.Japanese);
    }

    public void SetLanguage(int languageIndex)
    {
        GameSettings.Language selectedLanguage = languageIndex == (int)GameSettings.Language.Japanese
            ? GameSettings.Language.Japanese
            : GameSettings.Language.English;

        SetLanguage(selectedLanguage);
    }

    private void SetLanguage(GameSettings.Language language)
    {
        GameSettings.Instance.SetLanguage(language);
        SyncLanguageVisualsFromSettings();
    }

    private void SyncLanguageVisualsFromSettings()
    {
        SyncLanguageButtonColorsFromSettings();
    }

    private void ApplyLocalizationFontSettings()
    {
        if (englishFont == null && japaneseFont == null)
        {
            return;
        }

        LocalizationManager.ConfigureFonts(englishFont, japaneseFont, keepOriginalEnglishFont);
    }

    private void SyncLanguageButtonColorsFromSettings()
    {
        if (languageButtonVisuals == null || languageButtonVisuals.Length == 0)
        {
            return;
        }

        GameSettings settings = GameSettings.Instance;
        if (settings == null)
        {
            return;
        }

        GameSettings.Language currentLanguage = settings.CurrentLanguage;
        for (int i = 0; i < languageButtonVisuals.Length; i++)
        {
            Graphic targetGraphic = languageButtonVisuals[i].TargetGraphic;
            if (targetGraphic == null)
            {
                continue;
            }

            targetGraphic.color = languageButtonVisuals[i].Language == currentLanguage
                ? selectedLanguageColor
                : unselectedLanguageColor;
        }
    }
}
