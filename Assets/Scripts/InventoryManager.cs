using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class InventoryManager : MonoBehaviour
{
    [Serializable]
    public class InventoryEntry
    {
        [SerializeField] private BuildingDefinition buildingDefinition;
        [Min(0)]
        [SerializeField] private int quantity = 1;

        public InventoryEntry(BuildingDefinition buildingDefinition, int quantity)
        {
            this.buildingDefinition = buildingDefinition;
            this.quantity = Mathf.Max(0, quantity);
        }

        public BuildingDefinition BuildingDefinition => buildingDefinition;
        public int Quantity => quantity;

        public void SetQuantity(int newQuantity)
        {
            quantity = Mathf.Max(0, newQuantity);
        }
    }

    private static InventoryManager instance;

    [Header("Starting Building Inventory")]
    [SerializeField] private List<InventoryEntry> startingBuildings = new();

    [Header("Runtime Building Inventory")]
    [SerializeField] private List<InventoryEntry> buildingInventory = new();

    [Header("Progress")]
    [SerializeField, Min(0)] private int startingScrap;
    [SerializeField, Min(0)] private int startingScore;
    [SerializeField, Min(0)] private int scrap;
    [SerializeField, Min(0)] private int score;

    private readonly Dictionary<BuildingDefinition, InventoryEntry> buildingsByDefinition = new();
    private bool isInitialized;

    public static InventoryManager Instance => EnsureInstance();
    public static bool HasInstance => instance != null;

    public event Action InventoryChanged;

    public IReadOnlyList<InventoryEntry> BuildingItems
    {
        get
        {
            EnsureInitialized();
            return buildingInventory;
        }
    }

    public int Scrap
    {
        get
        {
            EnsureInitialized();
            return scrap;
        }
    }

    public int Score
    {
        get
        {
            EnsureInitialized();
            return score;
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

    public void AddScrap(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        EnsureInitialized();
        scrap += amount;
        InventoryChanged?.Invoke();
    }

    public bool RemoveScrap(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        EnsureInitialized();
        if (scrap < amount)
        {
            return false;
        }

        scrap -= amount;
        InventoryChanged?.Invoke();
        return true;
    }

    public void SetScrap(int amount)
    {
        EnsureInitialized();
        scrap = Mathf.Max(0, amount);
        InventoryChanged?.Invoke();
    }

    public void AddScore(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        EnsureInitialized();
        score += amount;
        InventoryChanged?.Invoke();
    }

    public void SetScore(int amount)
    {
        EnsureInitialized();
        score = Mathf.Max(0, amount);
        InventoryChanged?.Invoke();
    }

    public void ClearInventory()
    {
        EnsureInitialized();

        bool hasAnyBuildings = buildingInventory.Count > 0;
        bool hasProgress = scrap > 0 || score > 0;
        if (!hasAnyBuildings && !hasProgress)
        {
            return;
        }

        buildingInventory.Clear();
        buildingsByDefinition.Clear();
        scrap = 0;
        score = 0;
        InventoryChanged?.Invoke();
    }

    public int GetBuildingQuantity(BuildingDefinition buildingDefinition)
    {
        EnsureInitialized();
        if (buildingDefinition == null)
        {
            return 0;
        }

        return TryGetBuildingQuantityInternal(buildingDefinition, out int quantity) ? quantity : 0;
    }

    public bool HasBuilding(BuildingDefinition buildingDefinition, int quantity = 1)
    {
        if (quantity <= 0)
        {
            return true;
        }

        return GetBuildingQuantity(buildingDefinition) >= quantity;
    }

    public void AddBuilding(BuildingDefinition buildingDefinition, int quantity = 1)
    {
        if (quantity <= 0 || buildingDefinition == null)
        {
            return;
        }

        EnsureInitialized();
        TryGetBuildingQuantityInternal(buildingDefinition, out int currentQuantity);
        SetBuildingQuantityInternal(buildingDefinition, currentQuantity + quantity, true);
    }

    public bool RemoveBuilding(BuildingDefinition buildingDefinition, int quantity = 1)
    {
        if (quantity <= 0)
        {
            return true;
        }

        EnsureInitialized();
        if (buildingDefinition == null)
        {
            return false;
        }

        TryGetBuildingQuantityInternal(buildingDefinition, out int currentQuantity);
        if (currentQuantity < quantity)
        {
            return false;
        }

        SetBuildingQuantityInternal(buildingDefinition, currentQuantity - quantity, true);
        return true;
    }

    public void SetBuildingQuantity(BuildingDefinition buildingDefinition, int quantity)
    {
        EnsureInitialized();
        if (buildingDefinition == null)
        {
            Debug.LogWarning("Cannot set building inventory with a null definition.", this);
            return;
        }

        SetBuildingQuantityInternal(buildingDefinition, quantity, true);
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

        List<InventoryEntry> sourceBuildings = buildingInventory.Count > 0
            ? new List<InventoryEntry>(buildingInventory)
            : new List<InventoryEntry>(startingBuildings);

        buildingInventory.Clear();
        buildingsByDefinition.Clear();

        foreach (InventoryEntry entry in sourceBuildings)
        {
            if (entry == null || entry.BuildingDefinition == null || entry.Quantity <= 0)
            {
                continue;
            }

            TryGetBuildingQuantityInternal(entry.BuildingDefinition, out int currentQuantity);
            int combinedQuantity = currentQuantity + entry.Quantity;
            SetBuildingQuantityInternal(entry.BuildingDefinition, combinedQuantity, false);
        }

        scrap = Mathf.Max(0, scrap > 0 ? scrap : startingScrap);
        score = Mathf.Max(0, score > 0 ? score : startingScore);

        isInitialized = true;
    }

    private bool TryGetBuildingQuantityInternal(BuildingDefinition buildingDefinition, out int quantity)
    {
        if (buildingDefinition != null && buildingsByDefinition.TryGetValue(buildingDefinition, out InventoryEntry entry))
        {
            quantity = entry.Quantity;
            return true;
        }

        quantity = 0;
        return false;
    }

    private void SetBuildingQuantityInternal(BuildingDefinition buildingDefinition, int quantity, bool notifyListeners)
    {
        if (buildingDefinition == null)
        {
            return;
        }

        int clampedQuantity = Mathf.Max(0, quantity);

        if (clampedQuantity == 0)
        {
            if (buildingsByDefinition.TryGetValue(buildingDefinition, out InventoryEntry existingEntry))
            {
                buildingsByDefinition.Remove(buildingDefinition);
                buildingInventory.Remove(existingEntry);

                if (notifyListeners)
                {
                    InventoryChanged?.Invoke();
                }
            }

            return;
        }

        if (buildingsByDefinition.TryGetValue(buildingDefinition, out InventoryEntry entry))
        {
            if (entry.Quantity == clampedQuantity)
            {
                return;
            }

            entry.SetQuantity(clampedQuantity);
        }
        else
        {
            entry = new InventoryEntry(buildingDefinition, clampedQuantity);
            buildingsByDefinition.Add(buildingDefinition, entry);
            buildingInventory.Add(entry);
        }

        if (notifyListeners)
        {
            InventoryChanged?.Invoke();
        }
    }

}
