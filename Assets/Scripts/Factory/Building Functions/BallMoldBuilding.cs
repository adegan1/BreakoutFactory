using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BallMoldBuilding : MonoBehaviour, IItemInputReceiver, IBuildingInputPreview, IMachineResourceProgressProvider
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

    [Header("References")]
    [SerializeField] private BuildingInstance buildingInstance;
    [SerializeField] private TileManager tileManager;

    [Header("Input")]
    [SerializeField] private InputSide inputSide = InputSide.Left;
    [SerializeField, Min(1)] private int maxResources = 10;

    [Header("Debug Inventory")]
    [SerializeField] private List<StoredItemEntry> storedItems = new();

    private readonly Dictionary<ItemDefinition, int> inventoryByItem = new();
    private ItemDefinition lastAcceptedItemDefinition;

    public int DistinctItemCount => inventoryByItem.Count;
    public InputSide ConfiguredInputSide => inputSide;
    public int MaxResources => maxResources;
    public int CurrentResourceCount => GetTotalStoredAmount();
    public int CurrentResourceAmount => CurrentResourceCount;
    public int MaxResourceAmount => maxResources;
    public float NormalizedResourceAmount => maxResources > 0
        ? Mathf.Clamp01((float)CurrentResourceCount / maxResources)
        : 0f;
    public Color ResourceTint => lastAcceptedItemDefinition != null ? lastAcceptedItemDefinition.Tint : Color.white;

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
    }

    public bool CanAcceptItemAtTile(Vector2Int tile, ItemEntity item)
    {
        if (item == null || item.ItemDefinition == null)
        {
            return false;
        }

        if (!TryGetInputGridPosition(out Vector2Int inputTile))
        {
            return false;
        }

        int incomingAmount = Mathf.Max(1, item.Quantity);
        return tile == inputTile && HasCapacityFor(incomingAmount);
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
        Vector2Int baseInputOffset = GetInputTileOffset(GetBaseDirection(inputSide), footprintSize);
        Vector2Int rotatedInputOffset = FactoryGridDirectionUtility.RotateOffsetAroundFootprintCenter(
            baseInputOffset,
            footprintSize,
            rotationQuarterTurns);

        return topLeftGridPosition + rotatedInputOffset;
    }

    private void AddToInventory(ItemDefinition itemDefinition, int amount)
    {
        if (itemDefinition == null || amount <= 0)
        {
            return;
        }

        inventoryByItem.TryGetValue(itemDefinition, out int current);
        inventoryByItem[itemDefinition] = current + amount;
        lastAcceptedItemDefinition = itemDefinition;
        SyncSerializedInventory();
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
        // Returns offset to a tile on the INSIDE edge of the building, not outside
        if (direction == Vector2Int.right)
        {
            return new Vector2Int(footprintSize.x - 1, (footprintSize.y - 1) / 2);
        }

        if (direction == Vector2Int.left)
        {
            return new Vector2Int(0, (footprintSize.y - 1) / 2);
        }

        if (direction == Vector2Int.up)
        {
            return new Vector2Int((footprintSize.x - 1) / 2, footprintSize.y - 1);
        }

        // Down
        return new Vector2Int((footprintSize.x - 1) / 2, 0);
    }

    private static Vector2Int GetBaseDirection(InputSide side)
    {
        return FactoryGridDirectionUtility.DirectionFromQuarterTurns((int)side);
    }

    private void RebuildRuntimeInventoryFromSerialized()
    {
        inventoryByItem.Clear();
        lastAcceptedItemDefinition = null;

        for (int i = 0; i < storedItems.Count; i++)
        {
            StoredItemEntry entry = storedItems[i];
            if (entry == null || entry.Item == null || entry.Quantity <= 0)
            {
                continue;
            }

            inventoryByItem.TryGetValue(entry.Item, out int current);
            inventoryByItem[entry.Item] = current + entry.Quantity;

            if (lastAcceptedItemDefinition == null)
            {
                lastAcceptedItemDefinition = entry.Item;
            }
        }
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
