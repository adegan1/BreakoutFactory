using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class BallMoldBuilding : MonoBehaviour, IItemInputReceiver, IBuildingInputPreview
{
    public enum InputSide
    {
        Right,
        Up,
        Left,
        Down
    }

    [Serializable]
    private class StoredItemEntry
    {
        public ItemDefinition Item;
        public int Quantity;

        public StoredItemEntry(ItemDefinition item, int quantity)
        {
            Item = item;
            Quantity = quantity;
        }
    }

    [Serializable]
    private class BallPreviewBallTypeEntry
    {
        public ItemDefinition Item;
        public BallTypeData BallType;
    }

    [Header("References")]
    [SerializeField] private BuildingInstance buildingInstance;
    [SerializeField] private TileManager tileManager;

    [Header("Input")]
    [SerializeField] private InputSide inputSide = InputSide.Left;
    [SerializeField, Min(1)] private int maxResources = 10;

    [Header("Ball Preview")]
    [SerializeField] private SpriteRenderer ballPreviewRenderer;
    [SerializeField] private Transform ballPreviewMaskTransform;
    [SerializeField] private bool keepPreviewDefaultRotation = true;
    [SerializeField] private List<BallPreviewBallTypeEntry> ballGenerations = new();
    [SerializeField, Min(0f)] private float previewFillLerpSpeed = 6f;
    [SerializeField] private bool hidePreviewWhenEmpty = true;

    [Header("Debug Inventory")]
    [SerializeField] private List<StoredItemEntry> storedItems = new();

    private readonly Dictionary<ItemDefinition, int> inventoryByItem = new();
    private ItemDefinition acceptedResourceDefinition;
    private BallTypeData lastCreatedBallType;
    private bool isMoldCompleted;
    private float previewFillVisual;
    private Vector3 previewBaseLocalPosition;
    private Vector3 previewBaseScale = Vector3.one;
    private Vector3 maskBaseLocalPosition;
    private Vector3 maskBaseScale = Vector3.one;
    private Quaternion previewBaseWorldRotation = Quaternion.identity;
    private Quaternion maskBaseWorldRotation = Quaternion.identity;

    public int DistinctItemCount => inventoryByItem.Count;
    public InputSide ConfiguredInputSide => inputSide;
    public int MaxResources => maxResources;
    public int CurrentResourceCount => GetTotalStoredAmount();

    private void Reset()
    {
        buildingInstance = GetComponent<BuildingInstance>();

        if (tileManager == null)
        {
            tileManager = FindFirstObjectByType<TileManager>();
        }
    }

    private void Awake()
    {
        ResolveDependenciesIfNeeded();
        RebuildRuntimeInventoryFromSerialized();
        ResolveAcceptedResourceDefinition();
        CachePreviewBaseTransform();
        ApplyBallPreviewVisualImmediate();
    }

    private void Update()
    {
        UpdateBallPreviewVisual();
    }

    public bool CanAcceptItemAtTile(Vector2Int tile, ItemEntity item)
    {
        if (isMoldCompleted)
        {
            return false;
        }

        if (item == null || item.ItemDefinition == null)
        {
            return false;
        }

        if (!TryGetInputGridPosition(out Vector2Int inputTile))
        {
            return false;
        }

        int incomingAmount = Mathf.Max(1, item.Quantity);
        return tile == inputTile
            && IsResourceTypeAccepted(item.ItemDefinition)
            && HasCapacityFor(incomingAmount);
    }

    public bool TryAcceptItem(ItemEntity item, Vector2Int tile)
    {
        if (!CanAcceptItemAtTile(tile, item))
        {
            return false;
        }

        int amount = Mathf.Max(1, item.Quantity);
        AddToInventory(item.ItemDefinition, amount);
        Destroy(item.gameObject);
        return true;
    }

    public bool TryGetInputGridPosition(out Vector2Int inputGridPosition)
    {
        inputGridPosition = default;

        ResolveDependenciesIfNeeded();
        if (buildingInstance == null || tileManager == null)
        {
            return false;
        }

        Vector2Int footprintSize = buildingInstance.FootprintSize;
        if (footprintSize.x <= 0 || footprintSize.y <= 0)
        {
            return false;
        }

        inputGridPosition = BuildInputGridPosition(
            buildingInstance.GridPosition,
            footprintSize,
            buildingInstance.RotationQuarterTurns);
        return tileManager.IsInBounds(inputGridPosition);
    }

    public int GetStoredAmount(ItemDefinition itemDefinition)
    {
        if (itemDefinition == null)
        {
            return 0;
        }

        return inventoryByItem.TryGetValue(itemDefinition, out int amount) ? amount : 0;
    }

    public void GetInputTiles(
        Vector2Int topLeftGridPosition,
        Vector2Int footprintSize,
        int rotationQuarterTurns,
        List<Vector2Int> inputTiles)
    {
        if (inputTiles == null)
        {
            return;
        }

        inputTiles.Add(BuildInputGridPosition(topLeftGridPosition, footprintSize, rotationQuarterTurns));
    }

    private Vector2Int BuildInputGridPosition(
        Vector2Int topLeftGridPosition,
        Vector2Int footprintSize,
        int rotationQuarterTurns)
    {
        Vector2Int baseInputDirection = GetBaseDirection(inputSide);
        Vector2Int worldInputDirection = FactoryGridDirectionUtility.RotateDirection(baseInputDirection, rotationQuarterTurns);
        Vector2Int inputOffset = GetInputTileOffset(worldInputDirection, footprintSize);

        return topLeftGridPosition + inputOffset;
    }

    private void AddToInventory(ItemDefinition itemDefinition, int amount)
    {
        if (itemDefinition == null || amount <= 0)
        {
            return;
        }

        inventoryByItem.TryGetValue(itemDefinition, out int current);
        inventoryByItem[itemDefinition] = current + amount;
        if (acceptedResourceDefinition == null)
        {
            acceptedResourceDefinition = itemDefinition;
        }

        TryCreateBallsFromStoredResources();
        SyncSerializedInventory();
    }

    private void TryCreateBallsFromStoredResources()
    {
        if (acceptedResourceDefinition == null || maxResources <= 0)
        {
            return;
        }

        BallTypeData createdBallType = ResolveMappedBallType(acceptedResourceDefinition);
        if (createdBallType == null)
        {
            return;
        }

        int storedAmount = GetStoredAmount(acceptedResourceDefinition);
        if (storedAmount < maxResources)
        {
            return;
        }

        int createdCount = storedAmount / maxResources;
        for (int i = 0; i < createdCount; i++)
        {
            InventoryManager.Instance.AddCraftedBall(createdBallType);
        }

        if (createdCount > 0)
        {
            lastCreatedBallType = createdBallType;
            isMoldCompleted = true;
        }

        int remainingAmount = storedAmount - createdCount * maxResources;
        if (remainingAmount > 0)
        {
            inventoryByItem[acceptedResourceDefinition] = remainingAmount;
        }
        else
        {
            inventoryByItem.Remove(acceptedResourceDefinition);
            acceptedResourceDefinition = null;
        }
    }

    private bool IsResourceTypeAccepted(ItemDefinition incomingItemDefinition)
    {
        if (isMoldCompleted)
        {
            return false;
        }

        if (incomingItemDefinition == null)
        {
            return false;
        }

        if (CurrentResourceCount <= 0)
        {
            return true;
        }

        return acceptedResourceDefinition == null || acceptedResourceDefinition == incomingItemDefinition;
    }

    private bool HasCapacityFor(int incomingAmount)
    {
        if (incomingAmount <= 0)
        {
            return false;
        }

        int currentStored = GetTotalStoredAmount();
        return currentStored + incomingAmount <= maxResources;
    }

    private int GetTotalStoredAmount()
    {
        int total = 0;
        foreach (KeyValuePair<ItemDefinition, int> pair in inventoryByItem)
        {
            total += Mathf.Max(0, pair.Value);
        }

        return total;
    }

    private void ResolveDependenciesIfNeeded()
    {
        if (buildingInstance == null)
        {
            buildingInstance = GetComponent<BuildingInstance>();
        }

        if (tileManager == null)
        {
            tileManager = FindFirstObjectByType<TileManager>();
        }
    }

    private static Vector2Int GetInputTileOffset(Vector2Int direction, Vector2Int footprintSize)
    {
        // Use a single corner input tile that rotates consistently across 0/90/180/270.
        if (direction == Vector2Int.left)
        {
            return new Vector2Int(0, 0);
        }

        if (direction == Vector2Int.up)
        {
            return new Vector2Int(0, footprintSize.y - 1);
        }

        if (direction == Vector2Int.right)
        {
            return new Vector2Int(footprintSize.x - 1, footprintSize.y - 1);
        }

        // Down
        return new Vector2Int(footprintSize.x - 1, 0);
    }

    private static Vector2Int GetBaseDirection(InputSide side)
    {
        return FactoryGridDirectionUtility.DirectionFromQuarterTurns((int)side);
    }

    private void RebuildRuntimeInventoryFromSerialized()
    {
        inventoryByItem.Clear();
        acceptedResourceDefinition = null;

        for (int i = 0; i < storedItems.Count; i++)
        {
            StoredItemEntry entry = storedItems[i];
            if (entry == null || entry.Item == null || entry.Quantity <= 0)
            {
                continue;
            }

            inventoryByItem.TryGetValue(entry.Item, out int current);
            inventoryByItem[entry.Item] = current + entry.Quantity;

            if (acceptedResourceDefinition == null)
            {
                acceptedResourceDefinition = entry.Item;
            }
        }
    }

    private void ResolveAcceptedResourceDefinition()
    {
        if (acceptedResourceDefinition != null || CurrentResourceCount <= 0)
        {
            return;
        }

        foreach (KeyValuePair<ItemDefinition, int> pair in inventoryByItem)
        {
            if (pair.Key != null && pair.Value > 0)
            {
                acceptedResourceDefinition = pair.Key;
                return;
            }
        }
    }

    private void CachePreviewBaseTransform()
    {
        if (ballPreviewRenderer == null)
        {
            return;
        }

        previewBaseLocalPosition = ballPreviewRenderer.transform.localPosition;
        previewBaseScale = ballPreviewRenderer.transform.localScale;
        previewBaseWorldRotation = FactoryGridDirectionUtility.CalculateUnrotatedWorldRotation(ballPreviewRenderer.transform);

        if (ballPreviewMaskTransform != null)
        {
            maskBaseLocalPosition = ballPreviewMaskTransform.localPosition;
            maskBaseScale = ballPreviewMaskTransform.localScale;
            maskBaseWorldRotation = FactoryGridDirectionUtility.CalculateUnrotatedWorldRotation(ballPreviewMaskTransform);
            ballPreviewRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        }
    }

    private void ApplyBallPreviewVisualImmediate()
    {
        float targetFill = GetTargetPreviewFill();
        previewFillVisual = targetFill;
        ApplyPreviewRendererVisual(targetFill);
    }

    private void UpdateBallPreviewVisual()
    {
        if (ballPreviewRenderer == null)
        {
            return;
        }

        float targetFill = GetTargetPreviewFill();
        float speed = Mathf.Max(0f, previewFillLerpSpeed);
        if (speed > 0f)
        {
            float t = 1f - Mathf.Exp(-speed * Time.deltaTime);
            previewFillVisual = Mathf.Lerp(previewFillVisual, targetFill, t);
        }
        else
        {
            previewFillVisual = targetFill;
        }

        ApplyPreviewRendererVisual(previewFillVisual);
    }

    private float GetTargetPreviewFill()
    {
        if (acceptedResourceDefinition == null || maxResources <= 0)
        {
            return lastCreatedBallType != null ? 1f : 0f;
        }

        int storedAmount = GetStoredAmount(acceptedResourceDefinition);
        return Mathf.Clamp01((float)storedAmount / maxResources);
    }

    private void ApplyPreviewRendererVisual(float visualFill)
    {
        if (ballPreviewRenderer == null)
        {
            return;
        }

        ItemDefinition previewItem = acceptedResourceDefinition;
        BallTypeData previewBallType = ResolvePreviewBallType(previewItem);
        bool shouldShow = previewBallType != null && (!hidePreviewWhenEmpty || visualFill > 0.001f);
        ballPreviewRenderer.enabled = shouldShow;
        if (!shouldShow)
        {
            return;
        }

        ballPreviewRenderer.sprite = previewBallType.BallSprite;
        ballPreviewRenderer.color = previewBallType.DisplayColor;

        // Keep the sprite unsquished and reveal fill using a vertically resized mask.
        ballPreviewRenderer.transform.localPosition = previewBaseLocalPosition;
        ballPreviewRenderer.transform.localScale = previewBaseScale;
        ApplyPreviewDefaultRotationIfNeeded();

        if (ballPreviewMaskTransform == null)
        {
            return;
        }

        float clampedFill = Mathf.Clamp01(visualFill);
        Vector3 maskScale = maskBaseScale;
        maskScale.y = maskBaseScale.y * clampedFill;
        ballPreviewMaskTransform.localScale = maskScale;

        Vector3 maskLocalPos = maskBaseLocalPosition;
        maskLocalPos.y = maskBaseLocalPosition.y - maskBaseScale.y * (1f - clampedFill) * 0.5f;
        ballPreviewMaskTransform.localPosition = maskLocalPos;
        ApplyPreviewDefaultRotationIfNeeded();
    }

    private void ApplyPreviewDefaultRotationIfNeeded()
    {
        if (!keepPreviewDefaultRotation)
        {
            return;
        }

        if (ballPreviewRenderer != null)
        {
            ballPreviewRenderer.transform.rotation = previewBaseWorldRotation;
        }

        if (ballPreviewMaskTransform != null)
        {
            ballPreviewMaskTransform.rotation = maskBaseWorldRotation;
        }
    }

    private BallTypeData ResolvePreviewBallType(ItemDefinition itemDefinition)
    {
        if (itemDefinition == null)
        {
            return lastCreatedBallType;
        }

        BallTypeData mappedBallType = ResolveMappedBallType(itemDefinition);
        if (mappedBallType != null)
        {
            return mappedBallType;
        }

        return null;
    }

    private BallTypeData ResolveMappedBallType(ItemDefinition itemDefinition)
    {
        if (itemDefinition == null)
        {
            return null;
        }

        for (int i = 0; i < ballGenerations.Count; i++)
        {
            BallPreviewBallTypeEntry entry = ballGenerations[i];
            if (entry != null && entry.Item == itemDefinition && entry.BallType != null)
            {
                return entry.BallType;
            }
        }

        return null;
    }

    private void SyncSerializedInventory()
    {
        storedItems.Clear();
        foreach (KeyValuePair<ItemDefinition, int> pair in inventoryByItem)
        {
            storedItems.Add(new StoredItemEntry(pair.Key, pair.Value));
        }
    }

    private void OnValidate()
    {
        maxResources = Mathf.Max(1, maxResources);
    }
}
