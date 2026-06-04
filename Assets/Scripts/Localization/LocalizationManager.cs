using System;
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
    private bool isApplyingText;

    private static readonly Dictionary<string, string> JapaneseTranslations = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        { "Play", "プレイ" },
        { "Quit", "終了" },
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
        { "Out of Balls!", "ボールがありません！" },
        { "Open Shop", "ショップを開く" },
        { "Open Inventory", "インベントリを開く" },
        { "Shop", "ショップ" },
        { "Inventory", "インベントリ" },
        { "Reroll", "再抽選" },
        { "Sell Selected", "選択を売却" },
        { "Select Items to Sell!", "売却するアイテムを選択してください" },
        { "Item Name", "アイテム名" },
        { "Item Description", "アイテム説明" },
        { "Balls Created:", "作成ボール:" },
        { "Machines:", "機械:" },
        { "Machines Collected:", "回収した機械:" },
        { "Lives:", "ライフ:" },
        { "Health:", "体力:" },
        { "Score:", "スコア:" },
        { "- Place", "- 設置" },
        { "- Remove", "- 撤去" },
        { "- Rotate", "- 回転" },
        { "- Pan/Zoom", "- パン/ズーム" },
        { "- Show Info", "- 情報表示" },
        { "- Speed Up", "- 速度アップ" },
        { "- Play/Pause", "- 再生/一時停止" },
        { "Error Text", "エラーテキスト" }
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        GameObject managerObject = new GameObject("LocalizationManager");
        instance = managerObject.AddComponent<LocalizationManager>();
        DontDestroyOnLoad(managerObject);
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
    }

    private void HandleLanguageChanged(GameSettings.Language _)
    {
        ApplyLocalizationToAllTexts();
    }

    private void HandleSceneLoaded(Scene _, LoadSceneMode __)
    {
        ApplyLocalizationToAllTexts();
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

        int textId = text.GetInstanceID();
        if (!englishSourceByTextId.TryGetValue(textId, out string englishSource))
        {
            englishSource = text.text;
            englishSourceByTextId[textId] = englishSource;
        }

        GameSettings.Language language = GameSettings.Instance.CurrentLanguage;

        // Update the English source when another script writes text at runtime.
        string expectedLocalized = LocalizeEnglishString(englishSource, language);
        if (!string.Equals(text.text, expectedLocalized, StringComparison.Ordinal))
        {
            englishSource = text.text;
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

    private static string LocalizeEnglishString(string english, GameSettings.Language language)
    {
        if (string.IsNullOrEmpty(english) || language != GameSettings.Language.Japanese)
        {
            return english;
        }

        if (JapaneseTranslations.TryGetValue(english, out string directTranslation))
        {
            return directTranslation;
        }

        int colonIndex = english.IndexOf(':');
        if (colonIndex > 0)
        {
            string prefix = english.Substring(0, colonIndex + 1);
            if (JapaneseTranslations.TryGetValue(prefix, out string localizedPrefix))
            {
                return localizedPrefix + english.Substring(colonIndex + 1);
            }
        }

        return english;
    }
}
