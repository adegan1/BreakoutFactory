using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[DefaultExecutionOrder(-10000)]
[DisallowMultipleComponent]
public class LocalizationManager : MonoBehaviour
{
    private static LocalizationManager instance;

    private readonly Dictionary<int, string> englishSourceByTextId = new Dictionary<int, string>();
    private readonly Dictionary<int, TMP_FontAsset> originalFontByTextId = new Dictionary<int, TMP_FontAsset>();
    private bool isApplyingText;

    [Header("Font Swap")]
    [SerializeField] private TMP_FontAsset englishFont;
    [SerializeField] private TMP_FontAsset japaneseFont;
    [SerializeField] private bool keepOriginalEnglishFont = true;

    [Header("Translation Tables")]
    [SerializeField] private List<LocalizationTable> localizationTables = new List<LocalizationTable>();

    [Header("Refresh Timing")]
    [SerializeField, Min(0)] private int delayedRefreshFrames = 2;

    private static readonly Dictionary<string, string> JapaneseTranslations = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        { "Play", "プレイ" },
        { "Settings", "設定" },
        { "Language", "言語" },
        { "English", "English" },
        { "日本語", "日本語" },
        { "Audio", "オーディオ" },
        { "Controls", "操作" },
        { "Master Volume", "マスター音量" },
        { "Music Volume", "音楽音量" },
        { "SFX Volume", "効果音音量" },
        { "Ambience Volume", "環境音音量" },
        { "Quit", "終了" },
        { "Team Sankumi", "チームSankumi" },
        { "Reset Player Data", "プレイヤーデータをリセット" },
        { "Game Over!", "ゲームオーバー！" },
        { "Menu", "メニュー" },
        { "Back", "戻る" },
        { "Continue", "続ける" },
        { "Continue >", "続ける >" },
        { "Game Paused", "一時停止中" },
        { "Are You Ready to Continue?", "続行の準備はできましたか？" },
        { "Are you sure you want to clear your factory?", "工場を本当にクリアしますか？" },
        { "(All machines will be returned to your inventory)", "（すべての機械はインベントリに返却されます）" },
        { "Confirm Factory", "工場を確定" },
        { "Clear", "クリア" },
        { "Reset Factory", "工場をリセット" },
        { "You have unused ball molds!", "未使用のボールモールドがあります！" },
        { "Must Keep at Least One Ball Mold", "少なくとも1つのボールモールドを残してください" },
        { "Insufficient Scrap", "スクラップ不足" },
        { "Level Complete!", "レベルクリア！" },
        { "Level Complete", "レベルクリア" },
        { "Out of Balls!", "ボールがありません！" },
        { "Out of Balls", "ボールがありません" },
        { "Out of Health!", "体力がありません！" },
        { "Out of Health", "体力がありません" },
        { "Open Shop", "ショップを開く" },
        { "Open Inventory", "インベントリを開く" },
        { "Shop", "ショップ" },
        { "Inventory", "インベントリ" },
        { "Reroll", "再抽選" },
        { "Sell Selected", "選択を売却" },
        { "Buy", "購入" },
        { "Select Items to Sell!", "売却するアイテムを選択してください" },
        { "Balls Created:", "作成ボール:" },
        { "Machines:", "機械:" },
        { "Machines Collected:", "回収した機械:" },
        { "Lives:", "ライフ:" },
        { "Lives", "ライフ" },
        { "Health:", "体力:" },
        { "Score:", "スコア:" },
        { "Score", "スコア" },
        { "x", "x" },
        { "-1 Life", "ライフ -1" },
        { "- Place", "- 設置" },
        { "- Remove", "- 撤去" },
        { "- Rotate", "- 回転" },
        { "- Pan/Zoom", "- パン/ズーム" },
        { "- Show Info", "- 情報表示" },
        { "- Speed Up", "- 速度アップ" },
        { "- Play/Pause", "- 再生/一時停止" },
        { "Error Text", "エラーテキスト" },
        { "STRONG!", "強い！" },
        { "Scrap", "スクラップ" },
    };

    private static readonly Dictionary<string, string> EnglishByJapaneseTranslations = BuildEnglishByJapaneseMap();
    private static readonly Dictionary<string, string> JapaneseOverridesByEnglish = new Dictionary<string, string>(StringComparer.Ordinal);
    private static readonly Dictionary<string, string> EnglishByJapaneseOverrides = new Dictionary<string, string>(StringComparer.Ordinal);
    private static readonly Dictionary<string, string> JapaneseTableTranslations = new Dictionary<string, string>(StringComparer.Ordinal);
    private static readonly Dictionary<string, string> EnglishByJapaneseTableTranslations = new Dictionary<string, string>(StringComparer.Ordinal);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        RebuildTableTranslations();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (instance == this)
        {
            RebuildTableTranslations();
        }
    }

    public static void ConfigureFonts(TMP_FontAsset english, TMP_FontAsset japanese, bool preserveOriginalEnglishFont = true)
    {
        LocalizationManager localizationManager = EnsureInstance();
        localizationManager.englishFont = english;
        localizationManager.japaneseFont = japanese;
        localizationManager.keepOriginalEnglishFont = preserveOriginalEnglishFont;
        localizationManager.ApplyLocalizationToAllTexts();
    }

    public static string Localize(string englishText)
    {
        if (!Application.isPlaying)
        {
            return englishText;
        }

        GameSettings.Language currentLanguage = GameSettings.Instance.CurrentLanguage;
        return LocalizeEnglishString(englishText, currentLanguage);
    }

    public static string Localize(string englishText, string japaneseOverride)
    {
        if (!Application.isPlaying)
        {
            return englishText;
        }

        RegisterTranslationOverride(englishText, japaneseOverride);

        GameSettings.Language currentLanguage = GameSettings.Instance.CurrentLanguage;
        if (currentLanguage == GameSettings.Language.Japanese && !string.IsNullOrWhiteSpace(japaneseOverride))
        {
            return japaneseOverride.Trim();
        }

        return LocalizeEnglishString(englishText, currentLanguage);
    }

    public static string LocalizeToJapanese(string englishText, string japaneseOverride = null)
    {
        if (!string.IsNullOrWhiteSpace(japaneseOverride))
        {
            if (Application.isPlaying)
            {
                RegisterTranslationOverride(englishText, japaneseOverride);
            }

            return japaneseOverride.Trim();
        }

        if (!Application.isPlaying)
        {
            return englishText;
        }

        return LocalizeEnglishString(englishText, GameSettings.Language.Japanese);
    }

    public static void RegisterTranslationOverride(string englishText, string japaneseText)
    {
        string normalizedEnglish = NormalizeForLookup(englishText);
        string normalizedJapanese = NormalizeForLookup(japaneseText);

        if (string.IsNullOrEmpty(normalizedEnglish) || string.IsNullOrEmpty(normalizedJapanese))
        {
            return;
        }

        JapaneseOverridesByEnglish[normalizedEnglish] = japaneseText.Trim();
        EnglishByJapaneseOverrides[normalizedJapanese] = englishText.Trim();
    }

    private void OnEnable()
    {
        GameSettings.LanguageChanged += HandleLanguageChanged;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(HandleTextChanged);
    }

    private void OnDisable()
    {
        GameSettings.LanguageChanged -= HandleLanguageChanged;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(HandleTextChanged);
    }

    private void Start()
    {
        ApplyLocalizationToAllTexts();
        StartCoroutine(ApplyLocalizationDelayed());
    }

    private void HandleLanguageChanged(GameSettings.Language _)
    {
        ApplyLocalizationToAllTexts();
    }

    private void HandleSceneLoaded(Scene _, LoadSceneMode __)
    {
        ApplyLocalizationToAllTexts();
        StartCoroutine(ApplyLocalizationDelayed());
    }

    private IEnumerator ApplyLocalizationDelayed()
    {
        // Some UI text is initialized one or more frames after scene load.
        int refreshCount = Mathf.Max(0, delayedRefreshFrames);
        for (int i = 0; i < refreshCount; i++)
        {
            yield return null;
            ApplyLocalizationToAllTexts();
        }
    }

    private void HandleTextChanged(Object changedObject)
    {
        if (isApplyingText)
        {
            return;
        }

        TMP_Text changedText = changedObject as TMP_Text;
        if (changedText == null)
        {
            return;
        }

        TrackAndApplyText(changedText);
    }

    private void ApplyLocalizationToAllTexts()
    {
        TMP_Text[] allTexts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < allTexts.Length; i++)
        {
            TrackAndApplyText(allTexts[i]);
        }
    }

    private void TrackAndApplyText(TMP_Text text)
    {
        if (text == null)
        {
            return;
        }

        LocalizationTextExclusion exclusion = text.GetComponentInParent<LocalizationTextExclusion>(true);
        bool skipTranslation = exclusion != null && exclusion.ExcludeTranslation;
        bool skipFontSwap = exclusion != null && exclusion.ExcludeFontSwap;

        ApplyFontForLanguage(text, GameSettings.Instance.CurrentLanguage, skipFontSwap);

        if (skipTranslation)
        {
            return;
        }

        int textId = text.GetInstanceID();
        GameSettings.Language language = GameSettings.Instance.CurrentLanguage;

        if (!englishSourceByTextId.TryGetValue(textId, out string englishSource))
        {
            englishSource = ResolveEnglishSourceFromLiveText(text.text, language);
            englishSourceByTextId[textId] = englishSource;
        }

        if (language == GameSettings.Language.English)
        {
            string normalizedCurrent = NormalizeForLookup(text.text);
            string localizedJapaneseFromSource = NormalizeForLookup(LocalizeEnglishString(englishSource, GameSettings.Language.Japanese));

            // If this label is still showing Japanese while English mode is active,
            // recover the English source instead of re-caching Japanese as source text.
            if (string.Equals(normalizedCurrent, localizedJapaneseFromSource, StringComparison.Ordinal))
            {
                text.text = englishSource;
                return;
            }
        }

        // Only refresh source text from live UI while English is active.
        // Otherwise, switching from Japanese back to English can incorrectly
        // cache Japanese as the new source and keep JP text forever.
        if (language == GameSettings.Language.English && !string.Equals(text.text, englishSource, StringComparison.Ordinal))
        {
            string normalizedCurrent = NormalizeForLookup(text.text);
            string localizedJapaneseFromSource = NormalizeForLookup(LocalizeEnglishString(englishSource, GameSettings.Language.Japanese));

            if (!string.Equals(normalizedCurrent, localizedJapaneseFromSource, StringComparison.Ordinal))
            {
                englishSource = ResolveEnglishSourceFromLiveText(text.text, language);
            }

            englishSourceByTextId[textId] = englishSource;
        }

        string localizedText = LocalizeEnglishString(englishSource, language);
        if (string.Equals(text.text, localizedText, StringComparison.Ordinal))
        {
            return;
        }

        isApplyingText = true;
        text.text = localizedText;
        isApplyingText = false;
    }

    private void ApplyFontForLanguage(TMP_Text text, GameSettings.Language language, bool skipFontSwap)
    {
        if (skipFontSwap || text == null)
        {
            return;
        }

        int textId = text.GetInstanceID();
        if (!originalFontByTextId.ContainsKey(textId))
        {
            originalFontByTextId[textId] = text.font;
        }

        TMP_FontAsset targetFont = null;
        if (language == GameSettings.Language.Japanese)
        {
            targetFont = japaneseFont;
        }
        else if (keepOriginalEnglishFont)
        {
            targetFont = originalFontByTextId[textId];
        }
        else
        {
            targetFont = englishFont != null ? englishFont : originalFontByTextId[textId];
        }

        if (targetFont == null || text.font == targetFont)
        {
            return;
        }

        text.font = targetFont;
    }

    private static LocalizationManager EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        instance = FindFirstObjectByType<LocalizationManager>();
        if (instance != null)
        {
            return instance;
        }

        GameObject managerObject = new GameObject("LocalizationManager");
        instance = managerObject.AddComponent<LocalizationManager>();
        DontDestroyOnLoad(managerObject);
        return instance;
    }

    private static string LocalizeEnglishString(string english, GameSettings.Language language)
    {
        if (string.IsNullOrEmpty(english) || language != GameSettings.Language.Japanese)
        {
            return english;
        }

        if (ContainsRichTextTags(english))
        {
            return LocalizeRichTextString(english);
        }

        return LocalizePlainJapaneseString(english, logMissing: true);
    }

    private static string LocalizePlainJapaneseString(string english, bool logMissing)
    {
        if (string.IsNullOrEmpty(english))
        {
            return english;
        }

        string lookupText = NormalizeForLookup(english);

        if (TryGetTranslation(lookupText, out string directTranslation))
        {
            return ReapplyOuterWhitespace(english, directTranslation);
        }

        int colonIndex = lookupText.IndexOf(':');
        if (colonIndex > 0)
        {
            string prefix = lookupText.Substring(0, colonIndex + 1);
            if (TryGetTranslation(prefix, out string localizedPrefix))
            {
                return ReapplyOuterWhitespace(english, localizedPrefix + lookupText.Substring(colonIndex + 1));
            }
        }

        if (TryLocalizeTrailingQuantity(lookupText, out string localizedWithQuantity))
        {
            return ReapplyOuterWhitespace(english, localizedWithQuantity);
        }

        return english;
    }

    private static bool ContainsRichTextTags(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        int openIndex = text.IndexOf('<');
        if (openIndex < 0)
        {
            return false;
        }

        return text.IndexOf('>', openIndex + 1) > openIndex;
    }

    private static string LocalizeRichTextString(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        System.Text.StringBuilder output = new System.Text.StringBuilder(text.Length + 16);
        int index = 0;
        while (index < text.Length)
        {
            int tagStart = text.IndexOf('<', index);
            if (tagStart < 0)
            {
                string trailingPlain = text.Substring(index);
                output.Append(LocalizePlainJapaneseString(trailingPlain, logMissing: false));
                break;
            }

            if (tagStart > index)
            {
                string plainSegment = text.Substring(index, tagStart - index);
                output.Append(LocalizePlainJapaneseString(plainSegment, logMissing: false));
            }

            int tagEnd = text.IndexOf('>', tagStart + 1);
            if (tagEnd < 0)
            {
                // Malformed tag: localize the remainder as plain text.
                string remainingPlain = text.Substring(tagStart);
                output.Append(LocalizePlainJapaneseString(remainingPlain, logMissing: false));
                break;
            }

            output.Append(text.Substring(tagStart, tagEnd - tagStart + 1));
            index = tagEnd + 1;
        }

        return output.ToString();
    }

    private static string ReapplyOuterWhitespace(string originalText, string translatedCore)
    {
        if (string.IsNullOrEmpty(originalText))
        {
            return translatedCore;
        }

        int leadingCount = 0;
        while (leadingCount < originalText.Length && char.IsWhiteSpace(originalText[leadingCount]))
        {
            leadingCount++;
        }

        int trailingCount = 0;
        int trailingIndex = originalText.Length - 1;
        while (trailingIndex >= 0 && char.IsWhiteSpace(originalText[trailingIndex]))
        {
            trailingCount++;
            trailingIndex--;
        }

        string leading = leadingCount > 0 ? originalText.Substring(0, leadingCount) : string.Empty;
        string trailing = trailingCount > 0 ? originalText.Substring(originalText.Length - trailingCount, trailingCount) : string.Empty;
        return leading + translatedCore + trailing;
    }

    private static string ResolveEnglishSourceFromLiveText(string liveText, GameSettings.Language language)
    {
        string normalized = NormalizeForLookup(liveText);
        if (language == GameSettings.Language.Japanese && TryResolveEnglishTrailingQuantity(normalized, out string englishWithQuantity))
        {
            return englishWithQuantity;
        }

        if (language == GameSettings.Language.Japanese && EnglishByJapaneseOverrides.TryGetValue(normalized, out string englishFromOverride))
        {
            return englishFromOverride;
        }

        if (language == GameSettings.Language.Japanese && EnglishByJapaneseTableTranslations.TryGetValue(normalized, out string englishFromTable))
        {
            return englishFromTable;
        }

        if (language == GameSettings.Language.Japanese && EnglishByJapaneseTranslations.TryGetValue(normalized, out string englishFromJapanese))
        {
            return englishFromJapanese;
        }

        return liveText;
    }

    private static Dictionary<string, string> BuildEnglishByJapaneseMap()
    {
        Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, string> entry in JapaneseTranslations)
        {
            string normalizedJapanese = NormalizeForLookup(entry.Value);
            if (!map.ContainsKey(normalizedJapanese))
            {
                map[normalizedJapanese] = entry.Key;
            }
        }

        return map;
    }

    private static bool TryGetTranslation(string key, out string translation)
    {
        if (JapaneseOverridesByEnglish.TryGetValue(key, out translation))
        {
            return true;
        }

        if (JapaneseTableTranslations.TryGetValue(key, out translation))
        {
            return true;
        }

        if (JapaneseTranslations.TryGetValue(key, out translation))
        {
            return true;
        }

        // Fallback for trailing punctuation variants commonly used in UI labels.
        if (key.EndsWith("!", StringComparison.Ordinal) && JapaneseTranslations.TryGetValue(key.Substring(0, key.Length - 1), out translation))
        {
            return true;
        }

        if (key.EndsWith("!", StringComparison.Ordinal) && JapaneseTableTranslations.TryGetValue(key.Substring(0, key.Length - 1), out translation))
        {
            return true;
        }

        if (key.EndsWith("?", StringComparison.Ordinal) && JapaneseTranslations.TryGetValue(key.Substring(0, key.Length - 1), out translation))
        {
            return true;
        }

        if (key.EndsWith("?", StringComparison.Ordinal) && JapaneseTableTranslations.TryGetValue(key.Substring(0, key.Length - 1), out translation))
        {
            return true;
        }

        return false;
    }

    private static bool TryLocalizeTrailingQuantity(string key, out string localizedText)
    {
        localizedText = null;
        if (!TrySplitTrailingQuantity(key, out string baseText, out string quantitySuffix))
        {
            return false;
        }

        if (!TryGetTranslation(baseText, out string localizedBase))
        {
            return false;
        }

        localizedText = localizedBase + " " + quantitySuffix;
        return true;
    }

    private static bool TryResolveEnglishTrailingQuantity(string localizedText, out string englishText)
    {
        englishText = null;
        if (!TrySplitTrailingQuantity(localizedText, out string localizedBase, out string quantitySuffix))
        {
            return false;
        }

        if (EnglishByJapaneseOverrides.TryGetValue(localizedBase, out string englishBase)
            || EnglishByJapaneseTableTranslations.TryGetValue(localizedBase, out englishBase)
            || EnglishByJapaneseTranslations.TryGetValue(localizedBase, out englishBase))
        {
            englishText = englishBase + " " + quantitySuffix;
            return true;
        }

        return false;
    }

    private static bool TrySplitTrailingQuantity(string value, out string baseText, out string quantitySuffix)
    {
        baseText = null;
        quantitySuffix = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        int end = value.Length - 1;
        while (end >= 0 && char.IsWhiteSpace(value[end]))
        {
            end--;
        }

        int digitStart = end;
        while (digitStart >= 0 && char.IsDigit(value[digitStart]))
        {
            digitStart--;
        }

        if (digitStart == end)
        {
            return false;
        }

        int xIndex = digitStart;
        while (xIndex >= 0 && char.IsWhiteSpace(value[xIndex]))
        {
            xIndex--;
        }

        if (xIndex < 0)
        {
            return false;
        }

        char xChar = value[xIndex];
        if (xChar != 'x' && xChar != 'X')
        {
            return false;
        }

        string parsedBase = value.Substring(0, xIndex).TrimEnd();
        if (string.IsNullOrEmpty(parsedBase))
        {
            return false;
        }

        string parsedQuantity = value.Substring(xIndex, end - xIndex + 1).Trim();
        if (string.IsNullOrEmpty(parsedQuantity))
        {
            return false;
        }

        baseText = parsedBase;
        quantitySuffix = parsedQuantity;
        return true;
    }

    private static string NormalizeForLookup(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return text
            .Replace("\r\n", "\n")
            .Replace('\u2019', '\'')
            .Trim();
    }

    private void RebuildTableTranslations()
    {
        JapaneseTableTranslations.Clear();
        EnglishByJapaneseTableTranslations.Clear();

        for (int tableIndex = 0; tableIndex < localizationTables.Count; tableIndex++)
        {
            LocalizationTable table = localizationTables[tableIndex];
            if (table == null || table.Entries == null)
            {
                continue;
            }

            IReadOnlyList<LocalizationTable.Entry> entries = table.Entries;
            for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
            {
                LocalizationTable.Entry entry = entries[entryIndex];
                if (entry == null)
                {
                    continue;
                }

                string english = entry.english != null ? entry.english.Trim() : string.Empty;
                string japanese = entry.japanese != null ? entry.japanese.Trim() : string.Empty;

                string normalizedEnglish = NormalizeForLookup(english);
                string normalizedJapanese = NormalizeForLookup(japanese);
                if (string.IsNullOrEmpty(normalizedEnglish) || string.IsNullOrEmpty(normalizedJapanese))
                {
                    continue;
                }

                JapaneseTableTranslations[normalizedEnglish] = japanese;
                if (!EnglishByJapaneseTableTranslations.ContainsKey(normalizedJapanese))
                {
                    EnglishByJapaneseTableTranslations[normalizedJapanese] = english;
                }
            }
        }
    }

}
