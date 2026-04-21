using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BreakoutHudController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BreakoutGameController gameController;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private Transform upcomingBallIconsRoot;
    [SerializeField] private Image upcomingBallIconPrefab;
    [SerializeField] private Sprite fallbackBallSprite;

    [Header("Labels")]
    [SerializeField] private string scoreLabel = "Score";
    [SerializeField, Min(1)] private int previewLimit = 12;

    private readonly List<Image> iconPool = new List<Image>();

    private void Awake()
    {
        if (gameController == null)
        {
            gameController = FindAnyObjectByType<BreakoutGameController>();
        }
    }

    private void OnEnable()
    {
        if (gameController == null)
        {
            return;
        }

        gameController.ScoreChanged += HandleScoreChanged;
        gameController.BallsQueueChanged += HandleQueueChanged;
        RefreshAll();
    }

    private void OnDisable()
    {
        if (gameController == null)
        {
            return;
        }

        gameController.ScoreChanged -= HandleScoreChanged;
        gameController.BallsQueueChanged -= HandleQueueChanged;
    }

    private void HandleScoreChanged(int currentScore)
    {
        UpdateScoreText(currentScore);
    }

    private void HandleQueueChanged()
    {
        UpdateQueueIcons();
    }

    private void RefreshAll()
    {
        UpdateScoreText(gameController != null ? gameController.Score : 0);
        UpdateQueueIcons();
    }

    private void UpdateScoreText(int currentScore)
    {
        if (scoreText == null)
        {
            return;
        }

        scoreText.text = scoreLabel + ": " + currentScore;
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
}
