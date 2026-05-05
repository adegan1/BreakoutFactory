using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BreakoutHudController : MonoBehaviour
{
    private struct CollectedMachineDisplayEntry
    {
        public BuildingDefinition Definition;
        public int Quantity;
    }

    [Header("References")]
    [SerializeField] private BreakoutGameController gameController;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private TextMeshProUGUI livesText;
    [SerializeField] private GameObject levelCompletePopup;
    [SerializeField] private TextMeshProUGUI levelEndTitleText;
    [SerializeField] private Transform levelCompleteMachineIconsRoot;
    [SerializeField] private Image levelCompleteMachineIconPrefab;
    [SerializeField] private Transform upcomingBallIconsRoot;
    [SerializeField] private Image upcomingBallIconPrefab;
    [SerializeField] private Transform collectedMachineIconsRoot;
    [SerializeField] private Image collectedMachineIconPrefab;
    [SerializeField] private Sprite fallbackBallSprite;
    [SerializeField] private Sprite fallbackMachineSprite;

    [Header("Labels")]
    [SerializeField] private string livesLabel = "Lives";
    [SerializeField] private string scoreLabel = "Score";
    [SerializeField] private string levelCompleteTitle = "Level Complete!";
    [SerializeField] private string outOfBallsTitle = "Out of Balls!";
    [SerializeField] private string outOfHealthTitle = "Out of Health!";
    [SerializeField, Min(1)] private int previewLimit = 12;
    [SerializeField, Min(1)] private int collectedMachinePreviewLimit = 24;
    [SerializeField] private bool autoConfigureCollectedMachineGrid = true;
    [SerializeField, Min(1)] private int collectedMachineColumns = 6;
    [SerializeField] private bool hideLevelCompletePopupOnEnable = true;
    [SerializeField] private bool autoConfigureLevelCompleteMachineGrid = true;
    [SerializeField, Min(1)] private int levelCompleteMachineColumns = 6;
    [SerializeField] private string quantityPrefix = "x";

    private readonly List<Image> iconPool = new List<Image>();
    private readonly List<Image> collectedMachineIconPool = new List<Image>();
    private readonly List<TextMeshProUGUI> collectedMachineCountLabelPool = new List<TextMeshProUGUI>();
    private readonly List<Image> levelCompleteMachineIconPool = new List<Image>();
    private readonly List<TextMeshProUGUI> levelCompleteMachineCountLabelPool = new List<TextMeshProUGUI>();
    private readonly List<CollectedMachineDisplayEntry> collectedMachineDisplayEntries = new List<CollectedMachineDisplayEntry>();

    private void Awake()
    {
        if (gameController == null)
        {
            gameController = FindAnyObjectByType<BreakoutGameController>();
        }

        ConfigureCollectedMachineLayout();
        ConfigureLevelCompleteMachineLayout();
    }

    private void OnEnable()
    {
        if (hideLevelCompletePopupOnEnable && levelCompletePopup != null)
        {
            levelCompletePopup.SetActive(false);
        }

        if (gameController != null)
        {
            gameController.ScoreChanged += HandleScoreChanged;
            gameController.BallsQueueChanged += HandleQueueChanged;
            gameController.MachinesCollectedChanged += HandleMachinesCollectedChanged;
            gameController.AllBricksCleared += HandleAllBricksCleared;
            gameController.LevelEnded += HandleLevelEnded;
        }

        if (PlayerStats.HasInstance)
        {
            PlayerStats.Instance.HealthChanged += HandleHealthChanged;
            PlayerStats.Instance.LivesChanged += HandleLivesChanged;
        }

        RefreshAll();
    }

    private void OnDisable()
    {
        if (gameController != null)
        {
            gameController.ScoreChanged -= HandleScoreChanged;
            gameController.BallsQueueChanged -= HandleQueueChanged;
            gameController.MachinesCollectedChanged -= HandleMachinesCollectedChanged;
            gameController.AllBricksCleared -= HandleAllBricksCleared;
            gameController.LevelEnded -= HandleLevelEnded;
        }

        if (PlayerStats.HasInstance)
        {
            PlayerStats.Instance.HealthChanged -= HandleHealthChanged;
            PlayerStats.Instance.LivesChanged -= HandleLivesChanged;
        }
    }

    private void HandleScoreChanged(int currentScore)
    {
        UpdateScoreText(currentScore);
    }

    private void HandleQueueChanged()
    {
        UpdateQueueIcons();
    }

    private void HandleMachinesCollectedChanged()
    {
        UpdateCollectedMachineIcons();

        if (levelCompletePopup != null && levelCompletePopup.activeSelf)
        {
            UpdateLevelCompleteMachineIcons();
        }
    }

    private void HandleAllBricksCleared()
    {
        HandleLevelEnded(BreakoutGameController.LevelEndReason.LevelComplete);
    }

    private void HandleLevelEnded(BreakoutGameController.LevelEndReason reason)
    {
        if (levelCompletePopup != null)
        {
            levelCompletePopup.SetActive(true);
        }

        UpdateLevelEndTitle(reason);
        UpdateLevelCompleteMachineIcons();
    }

    private void HandleHealthChanged(int current, int max)
    {
        UpdateHealthSlider(current, max);
    }

    private void HandleLivesChanged(int current)
    {
        UpdateLivesText(current);
    }

    private void RefreshAll()
    {
        UpdateScoreText(gameController != null ? gameController.Score : 0);
        UpdateQueueIcons();
        UpdateCollectedMachineIcons();

        if (PlayerStats.HasInstance)
        {
            UpdateHealthSlider(PlayerStats.Instance.Health, PlayerStats.Instance.MaxHealth);
            UpdateLivesText(PlayerStats.Instance.Lives);
        }
    }

    private void UpdateScoreText(int currentScore)
    {
        if (scoreText == null)
        {
            return;
        }

        scoreText.text = scoreLabel + ": " + currentScore;
    }

    private void UpdateHealthSlider(int current, int max)
    {
        if (healthFillImage == null)
        {
            return;
        }

        healthFillImage.fillAmount = max > 0 ? (float)current / max : 0f;
    }

    private void UpdateLivesText(int current)
    {
        if (livesText == null)
        {
            return;
        }

        livesText.text = livesLabel + ": " + current;
    }

    private void UpdateLevelEndTitle(BreakoutGameController.LevelEndReason reason)
    {
        if (levelEndTitleText == null)
        {
            return;
        }

        string title = levelCompleteTitle;
        if (reason == BreakoutGameController.LevelEndReason.OutOfBalls)
        {
            title = outOfBallsTitle;
        }
        else if (reason == BreakoutGameController.LevelEndReason.OutOfHealth)
        {
            title = outOfHealthTitle;
        }

        levelEndTitleText.text = title;
    }

    private void UpdateQueueIcons()
    {
        if (upcomingBallIconsRoot == null || upcomingBallIconPrefab == null)
        {
            return;
        }

        int maxIcons = Mathf.Max(1, previewLimit);
        EnsureIconPool(maxIcons);

        for (int i = 0; i < iconPool.Count; i++)
        {
            iconPool[i].gameObject.SetActive(false);
        }

        if (gameController == null)
        {
            return;
        }

        List<BallTypeData> upcomingBalls = gameController.GetUpcomingBallsSnapshot();
        int shown = Mathf.Min(maxIcons, upcomingBalls.Count);
        for (int i = 0; i < shown; i++)
        {
            BallTypeData ballType = upcomingBalls[i];
            Image iconImage = iconPool[i];
            iconImage.sprite = ResolveBallSprite(ballType);
            iconImage.color = ResolveBallTint(ballType);
            iconImage.gameObject.SetActive(iconImage.sprite != null);
        }
    }

    private void EnsureIconPool(int count)
    {
        for (int i = iconPool.Count; i < count; i++)
        {
            Image spawnedIcon = Instantiate(upcomingBallIconPrefab, upcomingBallIconsRoot);
            spawnedIcon.gameObject.name = "UpcomingBallIcon_" + i;
            spawnedIcon.gameObject.SetActive(false);
            iconPool.Add(spawnedIcon);
        }
    }

    private Sprite ResolveBallSprite(BallTypeData ballType)
    {
        if (ballType != null && ballType.BallSprite != null)
        {
            return ballType.BallSprite;
        }

        return fallbackBallSprite;
    }

    private static Color ResolveBallTint(BallTypeData ballType)
    {
        if (ballType == null)
        {
            return Color.white;
        }

        return ballType.DisplayColor;
    }

    private void UpdateCollectedMachineIcons()
    {
        if (collectedMachineIconsRoot == null || collectedMachineIconPrefab == null)
        {
            return;
        }

        int maxIcons = Mathf.Max(1, collectedMachinePreviewLimit);
        EnsureCollectedMachineIconPool(maxIcons);

        for (int i = 0; i < collectedMachineIconPool.Count; i++)
        {
            collectedMachineIconPool[i].gameObject.SetActive(false);
        }

        if (gameController == null)
        {
            return;
        }

        BuildCollectedMachineDisplayEntries(gameController.GetCollectedMachinesSnapshot());

        int shown = Mathf.Min(maxIcons, collectedMachineDisplayEntries.Count);
        for (int i = 0; i < shown; i++)
        {
            CollectedMachineDisplayEntry entry = collectedMachineDisplayEntries[i];
            Image iconImage = collectedMachineIconPool[i];
            iconImage.sprite = ResolveMachineSprite(entry.Definition);
            iconImage.color = ResolveMachineTint(entry.Definition);
            iconImage.gameObject.SetActive(iconImage.sprite != null);

            TextMeshProUGUI countLabel = collectedMachineCountLabelPool[i];
            if (countLabel != null)
            {
                bool showLabel = entry.Quantity > 1;
                countLabel.gameObject.SetActive(showLabel);
                if (showLabel)
                {
                    countLabel.text = quantityPrefix + entry.Quantity;
                }
            }
        }
    }

    private void UpdateLevelCompleteMachineIcons()
    {
        if (levelCompleteMachineIconsRoot == null || levelCompleteMachineIconPrefab == null)
        {
            return;
        }

        int maxIcons = Mathf.Max(1, collectedMachinePreviewLimit);
        EnsureLevelCompleteMachineIconPool(maxIcons);

        for (int i = 0; i < levelCompleteMachineIconPool.Count; i++)
        {
            levelCompleteMachineIconPool[i].gameObject.SetActive(false);
        }

        if (gameController == null)
        {
            return;
        }

        BuildCollectedMachineDisplayEntries(gameController.GetCollectedMachinesSnapshot());

        int shown = Mathf.Min(maxIcons, collectedMachineDisplayEntries.Count);
        for (int i = 0; i < shown; i++)
        {
            CollectedMachineDisplayEntry entry = collectedMachineDisplayEntries[i];
            Image iconImage = levelCompleteMachineIconPool[i];
            iconImage.sprite = ResolveMachineSprite(entry.Definition);
            iconImage.color = ResolveMachineTint(entry.Definition);
            iconImage.gameObject.SetActive(iconImage.sprite != null);

            TextMeshProUGUI countLabel = levelCompleteMachineCountLabelPool[i];
            if (countLabel != null)
            {
                bool showLabel = entry.Quantity > 1;
                countLabel.gameObject.SetActive(showLabel);
                if (showLabel)
                {
                    countLabel.text = quantityPrefix + entry.Quantity;
                }
            }
        }
    }

    private void EnsureCollectedMachineIconPool(int count)
    {
        for (int i = collectedMachineIconPool.Count; i < count; i++)
        {
            Image spawnedIcon = Instantiate(collectedMachineIconPrefab, collectedMachineIconsRoot);
            spawnedIcon.gameObject.name = "CollectedMachineIcon_" + i;
            spawnedIcon.gameObject.SetActive(false);
            collectedMachineIconPool.Add(spawnedIcon);

            TextMeshProUGUI countLabel = EnsureCountLabel(spawnedIcon.transform, i);
            if (countLabel != null)
            {
                countLabel.gameObject.SetActive(false);
            }

            collectedMachineCountLabelPool.Add(countLabel);
        }
    }

    private void EnsureLevelCompleteMachineIconPool(int count)
    {
        for (int i = levelCompleteMachineIconPool.Count; i < count; i++)
        {
            Image spawnedIcon = Instantiate(levelCompleteMachineIconPrefab, levelCompleteMachineIconsRoot);
            spawnedIcon.gameObject.name = "LevelCompleteMachineIcon_" + i;
            spawnedIcon.gameObject.SetActive(false);
            levelCompleteMachineIconPool.Add(spawnedIcon);

            TextMeshProUGUI countLabel = EnsureCountLabel(spawnedIcon.transform, i);
            if (countLabel != null)
            {
                countLabel.gameObject.SetActive(false);
            }

            levelCompleteMachineCountLabelPool.Add(countLabel);
        }
    }

    private void BuildCollectedMachineDisplayEntries(List<BuildingDefinition> collectedMachines)
    {
        collectedMachineDisplayEntries.Clear();
        if (collectedMachines == null || collectedMachines.Count == 0)
        {
            return;
        }

        for (int i = 0; i < collectedMachines.Count; i++)
        {
            BuildingDefinition machineDefinition = collectedMachines[i];
            if (machineDefinition == null)
            {
                continue;
            }

            bool merged = false;
            for (int entryIndex = 0; entryIndex < collectedMachineDisplayEntries.Count; entryIndex++)
            {
                CollectedMachineDisplayEntry existing = collectedMachineDisplayEntries[entryIndex];
                if (existing.Definition != machineDefinition)
                {
                    continue;
                }

                existing.Quantity += 1;
                collectedMachineDisplayEntries[entryIndex] = existing;
                merged = true;
                break;
            }

            if (merged)
            {
                continue;
            }

            collectedMachineDisplayEntries.Add(new CollectedMachineDisplayEntry
            {
                Definition = machineDefinition,
                Quantity = 1
            });
        }
    }

    private TextMeshProUGUI EnsureCountLabel(Transform iconTransform, int iconIndex)
    {
        if (iconTransform == null)
        {
            return null;
        }

        TextMeshProUGUI existingLabel = iconTransform.GetComponentInChildren<TextMeshProUGUI>(true);
        if (existingLabel != null)
        {
            return existingLabel;
        }

        GameObject labelObject = new GameObject("CountLabel_" + iconIndex, typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(iconTransform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(1f, 0f);
        labelRect.anchorMax = new Vector2(1f, 0f);
        labelRect.pivot = new Vector2(1f, 0f);
        labelRect.anchoredPosition = new Vector2(-2f, 2f);
        labelRect.sizeDelta = new Vector2(42f, 20f);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.BottomRight;
        label.fontSize = 16f;
        label.color = Color.white;
        label.text = string.Empty;
        return label;
    }

    private void ConfigureCollectedMachineLayout()
    {
        if (!autoConfigureCollectedMachineGrid || collectedMachineIconsRoot == null)
        {
            return;
        }

        GridLayoutGroup gridLayout = collectedMachineIconsRoot.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            gridLayout = collectedMachineIconsRoot.gameObject.AddComponent<GridLayoutGroup>();
        }

        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = Mathf.Max(1, collectedMachineColumns);
    }

    private void ConfigureLevelCompleteMachineLayout()
    {
        if (!autoConfigureLevelCompleteMachineGrid || levelCompleteMachineIconsRoot == null)
        {
            return;
        }

        GridLayoutGroup gridLayout = levelCompleteMachineIconsRoot.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            gridLayout = levelCompleteMachineIconsRoot.gameObject.AddComponent<GridLayoutGroup>();
        }

        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = Mathf.Max(1, levelCompleteMachineColumns);
    }

    private Sprite ResolveMachineSprite(BuildingDefinition machineDefinition)
    {
        if (machineDefinition != null && machineDefinition.BuildingSprite != null)
        {
            return machineDefinition.BuildingSprite;
        }

        return fallbackMachineSprite;
    }

    private static Color ResolveMachineTint(BuildingDefinition machineDefinition)
    {
        if (machineDefinition == null)
        {
            return Color.white;
        }

        return machineDefinition.BuildingColor;
    }
}
