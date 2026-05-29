using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class FactoryBallConfirmationLayout : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform iconLayoutRoot;
    [SerializeField] private Image iconPrefab;
    [SerializeField] private GameObject unplacedMoldsText;

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
            UpdateUnplacedMoldsTextVisibility();
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

        UpdateUnplacedMoldsTextVisibility();
    }

    private void UpdateUnplacedMoldsTextVisibility()
    {
        if (unplacedMoldsText == null)
        {
            return;
        }

        bool hasUnplacedMolds = false;
        if (InventoryManager.HasInstance)
        {
            IReadOnlyList<InventoryManager.InventoryEntry> buildingItems = InventoryManager.Instance.BuildingItems;
            if (buildingItems != null)
            {
                for (int i = 0; i < buildingItems.Count; i++)
                {
                    InventoryManager.InventoryEntry entry = buildingItems[i];
                    BuildingDefinition definition = entry != null ? entry.BuildingDefinition : null;
                    if (entry != null && entry.Quantity > 0 && IsBallMoldDefinition(definition))
                    {
                        hasUnplacedMolds = true;
                        break;
                    }
                }
            }
        }

        if (unplacedMoldsText.activeSelf != hasUnplacedMolds)
        {
            unplacedMoldsText.SetActive(hasUnplacedMolds);
        }
    }

    private static bool IsBallMoldDefinition(BuildingDefinition definition)
    {
        if (definition == null || definition.BehaviorPrefab == null)
        {
            return false;
        }

        return definition.BehaviorPrefab.GetComponent<BallMoldBuilding>() != null
            || definition.BehaviorPrefab.GetComponentInChildren<BallMoldBuilding>(true) != null;
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

        TooltipTrigger tooltip = iconImage.GetComponent<TooltipTrigger>();
        if (tooltip != null)
        {
            tooltip.SetContent(
                ballType != null ? ballType.DisplayName : string.Empty,
                ballType != null ? ballType.Description : string.Empty);
        }
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
            return ballType.TrailColor;
        }

        if (defaultBallType != null)
        {
            return defaultBallType.TrailColor;
        }

        return Color.white;
    }
}
