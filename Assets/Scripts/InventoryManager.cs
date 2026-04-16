using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class InventoryManager : MonoBehaviour
{
    [Serializable]
    public class InventoryEntry
    {
        [SerializeField] private string itemId;
        [Min(0)]
        [SerializeField] private int quantity = 1;

        public InventoryEntry(string itemId, int quantity)
        {
            this.itemId = itemId;
            this.quantity = Mathf.Max(0, quantity);
        }

        public string ItemId => itemId;
        public int Quantity => quantity;

        public void SetQuantity(int newQuantity)
        {
            quantity = Mathf.Max(0, newQuantity);
        }
    }

    private static InventoryManager instance;

    [Header("Starting Inventory")]
    [SerializeField] private List<InventoryEntry> startingItems = new();

    [Header("Runtime Inventory")]
    [SerializeField] private List<InventoryEntry> inventoryItems = new();

    private readonly Dictionary<string, InventoryEntry> itemsById = new(StringComparer.OrdinalIgnoreCase);
    private bool isInitialized;

    public static InventoryManager Instance => EnsureInstance();
    public static bool HasInstance => instance != null;

    public event Action InventoryChanged;

    public IReadOnlyList<InventoryEntry> Items
    {
        get
        {
            EnsureInitialized();
            return inventoryItems;
        }
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
        EnsureInitialized();
    }

    public int GetQuantity(string itemId)
    {
        EnsureInitialized();
        string normalizedItemId = NormalizeItemId(itemId);
        if (string.IsNullOrEmpty(normalizedItemId))
        {
            return 0;
        }

        return itemsById.TryGetValue(normalizedItemId, out InventoryEntry entry)
            ? entry.Quantity
            : 0;
    }

    public bool HasItem(string itemId, int quantity = 1)
    {
        if (quantity <= 0)
        {
            return true;
        }

        return GetQuantity(itemId) >= quantity;
    }

    public void AddItem(string itemId, int quantity = 1)
    {
        if (quantity <= 0)
        {
            return;
        }

        EnsureInitialized();
        string normalizedItemId = NormalizeItemId(itemId);
        if (string.IsNullOrEmpty(normalizedItemId))
        {
            Debug.LogWarning("Cannot add an inventory item with an empty id.", this);
            return;
        }

        int currentQuantity = GetQuantity(normalizedItemId);
        SetQuantityInternal(normalizedItemId, currentQuantity + quantity, true);
    }

    public bool RemoveItem(string itemId, int quantity = 1)
    {
        if (quantity <= 0)
        {
            return true;
        }

        EnsureInitialized();
        string normalizedItemId = NormalizeItemId(itemId);
        if (string.IsNullOrEmpty(normalizedItemId))
        {
            return false;
        }

        int currentQuantity = GetQuantity(normalizedItemId);
        if (currentQuantity < quantity)
        {
            return false;
        }

        SetQuantityInternal(normalizedItemId, currentQuantity - quantity, true);
        return true;
    }

    public void SetQuantity(string itemId, int quantity)
    {
        EnsureInitialized();
        string normalizedItemId = NormalizeItemId(itemId);
        if (string.IsNullOrEmpty(normalizedItemId))
        {
            Debug.LogWarning("Cannot set an inventory item with an empty id.", this);
            return;
        }

        SetQuantityInternal(normalizedItemId, quantity, true);
    }

    public void ClearInventory()
    {
        EnsureInitialized();

        if (inventoryItems.Count == 0)
        {
            return;
        }

        inventoryItems.Clear();
        itemsById.Clear();
        InventoryChanged?.Invoke();
    }

    private static InventoryManager EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        instance = FindFirstObjectByType<InventoryManager>();
        if (instance == null)
        {
            GameObject inventoryObject = new GameObject("InventoryManager");
            instance = inventoryObject.AddComponent<InventoryManager>();
        }

        EnsureInitialized();
        return instance;
    }

    private static void EnsureInitialized()
    {
        if (instance != null)
        {
            instance.InitializeIfNeeded();
        }
    }

    private void InitializeIfNeeded()
    {
        if (isInitialized)
        {
            return;
        }

        List<InventoryEntry> sourceItems = inventoryItems.Count > 0
            ? new List<InventoryEntry>(inventoryItems)
            : new List<InventoryEntry>(startingItems);

        inventoryItems.Clear();
        itemsById.Clear();

        foreach (InventoryEntry entry in sourceItems)
        {
            if (entry == null)
            {
                continue;
            }

            string normalizedItemId = NormalizeItemId(entry.ItemId);
            if (string.IsNullOrEmpty(normalizedItemId) || entry.Quantity <= 0)
            {
                continue;
            }

            int combinedQuantity = GetQuantity(normalizedItemId) + entry.Quantity;
            SetQuantityInternal(normalizedItemId, combinedQuantity, false);
        }

        isInitialized = true;
    }

    private void SetQuantityInternal(string itemId, int quantity, bool notifyListeners)
    {
        int clampedQuantity = Mathf.Max(0, quantity);

        if (clampedQuantity == 0)
        {
            if (itemsById.TryGetValue(itemId, out InventoryEntry existingEntry))
            {
                itemsById.Remove(itemId);
                inventoryItems.Remove(existingEntry);

                if (notifyListeners)
                {
                    InventoryChanged?.Invoke();
                }
            }

            return;
        }

        if (itemsById.TryGetValue(itemId, out InventoryEntry entry))
        {
            if (entry.Quantity == clampedQuantity)
            {
                return;
            }

            entry.SetQuantity(clampedQuantity);
        }
        else
        {
            entry = new InventoryEntry(itemId, clampedQuantity);
            itemsById.Add(itemId, entry);
            inventoryItems.Add(entry);
        }

        if (notifyListeners)
        {
            InventoryChanged?.Invoke();
        }
    }

    private static string NormalizeItemId(string itemId)
    {
        return string.IsNullOrWhiteSpace(itemId) ? string.Empty : itemId.Trim();
    }
}
