using UnityEngine;

[DisallowMultipleComponent]
public class LanguageObjectToggle : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private GameObject englishObject;
    [SerializeField] private GameObject japaneseObject;

    [Header("Behavior")]
    [SerializeField] private bool updateOnLanguageChanged = false;

    private void Start()
    {
        ApplyForCurrentLanguage();
    }

    private void OnEnable()
    {
        if (updateOnLanguageChanged)
        {
            GameSettings.LanguageChanged += HandleLanguageChanged;
        }
    }

    private void OnDisable()
    {
        GameSettings.LanguageChanged -= HandleLanguageChanged;
    }

    private void HandleLanguageChanged(GameSettings.Language _)
    {
        ApplyForCurrentLanguage();
    }

    private void ApplyForCurrentLanguage()
    {
        GameSettings.Language currentLanguage = GameSettings.Instance.CurrentLanguage;
        bool isEnglish = currentLanguage == GameSettings.Language.English;

        if (englishObject != null)
        {
            englishObject.SetActive(isEnglish);
        }

        if (japaneseObject != null)
        {
            japaneseObject.SetActive(!isEnglish);
        }
    }
}
