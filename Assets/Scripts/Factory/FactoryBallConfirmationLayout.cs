using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class FactoryBallConfirmationLayout : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform iconLayoutRoot;
    [SerializeField] private Image iconPrefab;

    [Header("Fallback")]
    [SerializeField] private BallTypeData defaultBallType;
    [SerializeField] private Sprite fallbackSprite;

    [Header("Behavior")]
    [SerializeField] private bool refreshOnEnable = true;

    private readonly List<Image> iconPool = new List<Image>();

    private void OnEnable()
    {
        if (InventoryManager.HasInstance)
        {
            InventoryManager.Instance.InventoryChanged += HandleInventoryChanged;
        }

        if (refreshOnEnable)
        {
            RefreshIcons();
        }
    }

    private void OnDisable()
    {
        if (InventoryManager.HasInstance)
        {
            InventoryManager.Instance.InventoryChanged -= HandleInventoryChanged;
        }
    }

    private void HandleInventoryChanged()
    {
        RefreshIcons();
    }

    public void RefreshIcons()
    {
        if (iconLayoutRoot == null || iconPrefab == null)
        {
            return;
        }

        int moldCount = FindObjectsByType<BallMoldBuilding>(FindObjectsSortMode.None).Length;

        EnsureIconPool(moldCount);
        for (int i = 0; i < iconPool.Count; i++)
        {
            iconPool[i].gameObject.SetActive(false);
        }

        if (moldCount <= 0)
        {
            return;
        }

        IReadOnlyList<BallTypeData> craftedBalls = InventoryManager.HasInstance
            ? InventoryManager.Instance.CraftedBalls
            : null;
        int craftedShown = craftedBalls != null ? Mathf.Min(craftedBalls.Count, moldCount) : 0;

        for (int i = 0; i < craftedShown; i++)
        {
            ApplyIcon(iconPool[i], craftedBalls[i]);
        }

        for (int i = craftedShown; i < moldCount; i++)
        {
            ApplyIcon(iconPool[i], defaultBallType);
        }
    }

    private void EnsureIconPool(int count)
    {
        for (int i = iconPool.Count; i < count; i++)
        {
            Image spawnedIcon = Instantiate(iconPrefab, iconLayoutRoot);
            spawnedIcon.gameObject.name = "FactoryBallConfirmIcon_" + i;
            spawnedIcon.gameObject.SetActive(false);
            iconPool.Add(spawnedIcon);
        }
    }

    private void ApplyIcon(Image iconImage, BallTypeData ballType)
    {
        if (iconImage == null)
        {
            return;
        }

        iconImage.sprite = ResolveSprite(ballType);
        iconImage.color = ResolveTint(ballType);
        iconImage.gameObject.SetActive(iconImage.sprite != null);
    }

    private Sprite ResolveSprite(BallTypeData ballType)
    {
        if (ballType != null && ballType.BallSprite != null)
        {
            return ballType.BallSprite;
        }

        if (defaultBallType != null && defaultBallType.BallSprite != null)
        {
            return defaultBallType.BallSprite;
        }

        return fallbackSprite;
    }

    private Color ResolveTint(BallTypeData ballType)
    {
        if (ballType != null)
        {
            return ballType.DisplayColor;
        }

        if (defaultBallType != null)
        {
            return defaultBallType.DisplayColor;
        }

        return Color.white;
    }
}
