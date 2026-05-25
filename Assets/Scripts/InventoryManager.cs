using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class InventoryManager : MonoBehaviour
{
    public const int BuildingHotbarSlotCount = 10;

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

    [Serializable]
    public class ItemInventoryEntry
    {
        [SerializeField] private ItemDefinition itemDefinition;
        [Min(0)]
        [SerializeField] private int quantity = 1;

        public ItemInventoryEntry(ItemDefinition itemDefinition, int quantity)
        {
            this.itemDefinition = itemDefinition;
            this.quantity = Mathf.Max(0, quantity);
        }

        public ItemDefinition ItemDefinition => itemDefinition;
        public int Quantity => quantity;

        public void SetQuantity(int newQuantity)
        {
            quantity = Mathf.Max(0, newQuantity);
        }
    }

    [Serializable]
    private class StoredMachineResourceState
    {
        [SerializeField] private string machineStateId;
        [SerializeField] private int storedAmount;

        public string MachineStateId => machineStateId;
        public int StoredAmount => storedAmount;

        public StoredMachineResourceState(string machineStateId, int storedAmount)
        {
            this.machineStateId = machineStateId;
            this.storedAmount = Mathf.Max(0, storedAmount);
        }

        public void SetStoredAmount(int amount)
        {
            storedAmount = Mathf.Max(0, amount);
        }
    }

    [Serializable]
    private class BuildingStoredResourceStackEntry
    {
        [SerializeField] private BuildingDefinition buildingDefinition;
        [SerializeField] private List<StoredMachineResourceState> storedStates = new();

        public BuildingDefinition BuildingDefinition => buildingDefinition;
        public List<StoredMachineResourceState> StoredStates => storedStates;

        public BuildingStoredResourceStackEntry(BuildingDefinition buildingDefinition)
        {
            this.buildingDefinition = buildingDefinition;
        }
    }

    private static InventoryManager instance;

    [Header("Starting Building Inventory")]
    [SerializeField] private List<InventoryEntry> startingBuildings = new();

    [Header("Runtime Building Inventory")]
    [SerializeField] private List<InventoryEntry> buildingInventory = new();
    [SerializeField] private List<BuildingDefinition> buildingHotbarSlots = new();
    [SerializeField] private List<BuildingStoredResourceStackEntry> buildingStoredResourceStacks = new();

    [Header("Starting Item Inventory")]
    [SerializeField] private List<ItemInventoryEntry> startingItems = new();

    [Header("Runtime Item Inventory")]
    [SerializeField] private List<ItemInventoryEntry> itemInventory = new();

    [Header("Crafted Balls")]
    [SerializeField] private List<BallTypeData> craftedBalls = new();

    [Header("Progress")]
    [SerializeField, Min(0)] private int startingScrap;
    [SerializeField, Min(0)] private int startingScore;
    [SerializeField, Min(0)] private int scrap;
    [SerializeField, Min(0)] private int score;

    private readonly Dictionary<BuildingDefinition, InventoryEntry> buildingsByDefinition = new();
    private readonly Dictionary<ItemDefinition, ItemInventoryEntry> itemsByDefinition = new();
    private readonly Dictionary<BuildingDefinition, BuildingStoredResourceStackEntry> storedResourceStacksByDefinition = new();
    private bool isInitialized;
    private bool hasImportedSceneStartingData;

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

    public IReadOnlyList<BuildingDefinition> BuildingHotbarSlots
    {
        get
        {
            EnsureInitialized();
            return buildingHotbarSlots;
        }
    }

    public IReadOnlyList<ItemInventoryEntry> ItemItems
    {
        get
        {
            EnsureInitialized();
            return itemInventory;
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

    public IReadOnlyList<BallTypeData> CraftedBalls
    {
        get
        {
            EnsureInitialized();
            return craftedBalls;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            instance.ImportStartingDataFrom(this);
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureInitialized();
    }

    private void ImportStartingDataFrom(InventoryManager source)
    {
        if (source == null || source == this)
        {
            return;
        }

        EnsureInitialized();

        if (!SourceHasStartingData(source))
        {
            return;
        }

        CaptureStartingDataFromSource(source);

        if (hasImportedSceneStartingData)
        {
            return;
        }

        // Apply scene-configured starting values one time to the persistent runtime inventory.
        MergeStartingBuildings(source.startingBuildings);
        MergeStartingItems(source.startingItems);

        if (scrap <= 0 && source.startingScrap > 0)
        {
            scrap = source.startingScrap;
        }

        if (score <= 0 && source.startingScore > 0)
        {
            score = source.startingScore;
        }

        hasImportedSceneStartingData = true;
        InventoryChanged?.Invoke();
    }

    private static bool SourceHasStartingData(InventoryManager source)
    {
        if (source == null)
        {
            return false;
        }

        bool hasStartingBuildings = source.startingBuildings != null && source.startingBuildings.Count > 0;
        bool hasStartingItems = source.startingItems != null && source.startingItems.Count > 0;
        bool hasStartingScrap = source.startingScrap > 0;
        bool hasStartingScore = source.startingScore > 0;
        return hasStartingBuildings || hasStartingItems || hasStartingScrap || hasStartingScore;
    }

    private void CaptureStartingDataFromSource(InventoryManager source)
    {
        startingBuildings.Clear();
        if (source.startingBuildings != null)
        {
            for (int i = 0; i < source.startingBuildings.Count; i++)
            {
                InventoryEntry entry = source.startingBuildings[i];
                if (entry == null || entry.BuildingDefinition == null || entry.Quantity <= 0)
                {
                    continue;
                }

                startingBuildings.Add(new InventoryEntry(entry.BuildingDefinition, entry.Quantity));
            }
        }

        startingItems.Clear();
        if (source.startingItems != null)
        {
            for (int i = 0; i < source.startingItems.Count; i++)
            {
                ItemInventoryEntry entry = source.startingItems[i];
                if (entry == null || entry.ItemDefinition == null || entry.Quantity <= 0)
                {
                    continue;
                }

                startingItems.Add(new ItemInventoryEntry(entry.ItemDefinition, entry.Quantity));
            }
        }

        startingScrap = Mathf.Max(0, source.startingScrap);
        startingScore = Mathf.Max(0, source.startingScore);
    }

    private void MergeStartingBuildings(List<InventoryEntry> sourceEntries)
    {
        if (sourceEntries == null)
        {
            return;
        }

        // Insert in reverse so final runtime order matches source order at the front.
        for (int i = sourceEntries.Count - 1; i >= 0; i--)
        {
            InventoryEntry entry = sourceEntries[i];
            if (entry == null || entry.BuildingDefinition == null || entry.Quantity <= 0)
            {
                continue;
            }

            BuildingDefinition definition = entry.BuildingDefinition;
            int quantityToAdd = entry.Quantity;

            if (buildingsByDefinition.TryGetValue(definition, out InventoryEntry existingEntry))
            {
                int updatedQuantity = Mathf.Max(0, existingEntry.Quantity + quantityToAdd);
                existingEntry.SetQuantity(updatedQuantity);

                // Move existing entry to the front so starting buildings come first.
                buildingInventory.Remove(existingEntry);
                buildingInventory.Insert(0, existingEntry);
                continue;
            }

            InventoryEntry newEntry = new InventoryEntry(definition, quantityToAdd);
            buildingsByDefinition.Add(definition, newEntry);
            buildingInventory.Insert(0, newEntry);
            AssignBuildingToFirstAvailableHotbarSlot(definition);
        }

        PrioritizeHotbarDefinitionsAtFront(sourceEntries);
    }

    public bool TryGetBuildingAtHotbarSlot(int slotIndex, out BuildingDefinition buildingDefinition, out int quantity)
    {
        buildingDefinition = null;
        quantity = 0;

        EnsureInitialized();
        EnsureHotbarSlotList();

        if (!IsValidHotbarSlotIndex(slotIndex))
        {
            return false;
        }

        buildingDefinition = buildingHotbarSlots[slotIndex];
        if (buildingDefinition == null)
        {
            return false;
        }

        TryGetBuildingQuantityInternal(buildingDefinition, out quantity);
        return true;
    }

    private void MergeStartingItems(List<ItemInventoryEntry> sourceEntries)
    {
        if (sourceEntries == null)
        {
            return;
        }

        for (int i = 0; i < sourceEntries.Count; i++)
        {
            ItemInventoryEntry entry = sourceEntries[i];
            if (entry == null || entry.ItemDefinition == null || entry.Quantity <= 0)
            {
                continue;
            }

            TryGetItemQuantityInternal(entry.ItemDefinition, out int currentQuantity);
            SetItemQuantityInternal(entry.ItemDefinition, currentQuantity + entry.Quantity, false);
        }
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
        bool hasAnyItems = itemInventory.Count > 0;
        bool hasAnyBalls = craftedBalls.Count > 0;
        bool hasProgress = scrap > 0 || score > 0;
        if (!hasAnyBuildings && !hasAnyItems && !hasAnyBalls && !hasProgress)
        {
            return;
        }

        buildingInventory.Clear();
        buildingsByDefinition.Clear();
        buildingHotbarSlots.Clear();
        buildingStoredResourceStacks.Clear();
        storedResourceStacksByDefinition.Clear();
        EnsureHotbarSlotList();
        itemInventory.Clear();
        itemsByDefinition.Clear();
        craftedBalls.Clear();
        scrap = 0;
        score = 0;
        InventoryChanged?.Invoke();
    }

    public int GetItemQuantity(ItemDefinition itemDefinition)
    {
        EnsureInitialized();
        if (itemDefinition == null)
        {
            return 0;
        }

        return TryGetItemQuantityInternal(itemDefinition, out int quantity) ? quantity : 0;
    }

    public bool HasItem(ItemDefinition itemDefinition, int quantity = 1)
    {
        if (quantity <= 0)
        {
            return true;
        }

        return GetItemQuantity(itemDefinition) >= quantity;
    }

    public void AddItem(ItemDefinition itemDefinition, int quantity = 1)
    {
        if (quantity <= 0 || itemDefinition == null)
        {
            return;
        }

        EnsureInitialized();
        TryGetItemQuantityInternal(itemDefinition, out int currentQuantity);
        SetItemQuantityInternal(itemDefinition, currentQuantity + quantity, true);
    }

    public bool RemoveItem(ItemDefinition itemDefinition, int quantity = 1)
    {
        if (quantity <= 0)
        {
            return true;
        }

        EnsureInitialized();
        if (itemDefinition == null)
        {
            return false;
        }

        TryGetItemQuantityInternal(itemDefinition, out int currentQuantity);
        if (currentQuantity < quantity)
        {
            return false;
        }

        SetItemQuantityInternal(itemDefinition, currentQuantity - quantity, true);
        return true;
    }

    public void SetItemQuantity(ItemDefinition itemDefinition, int quantity)
    {
        EnsureInitialized();
        if (itemDefinition == null)
        {
            Debug.LogWarning("Cannot set item inventory with a null definition.", this);
            return;
        }

        SetItemQuantityInternal(itemDefinition, quantity, true);
    }

    public void AddCraftedBall(BallTypeData ballType)
    {
        if (ballType == null)
        {
            return;
        }

        EnsureInitialized();
        craftedBalls.Add(ballType);
        InventoryChanged?.Invoke();
    }

    public bool TryRemoveCraftedBall(BallTypeData ballType)
    {
        if (ballType == null)
        {
            return false;
        }

        EnsureInitialized();
        int idx = craftedBalls.IndexOf(ballType);
        if (idx < 0)
        {
            return false;
        }

        craftedBalls.RemoveAt(idx);
        InventoryChanged?.Invoke();
        return true;
    }

    public void AddCraftedBalls(IEnumerable<BallTypeData> ballTypes)
    {
        if (ballTypes == null)
        {
            return;
        }

        EnsureInitialized();

        bool changed = false;
        foreach (BallTypeData ballType in ballTypes)
        {
            if (ballType == null)
            {
                continue;
            }

            craftedBalls.Add(ballType);
            changed = true;
        }

        if (changed)
        {
            InventoryChanged?.Invoke();
        }
    }

    public void SetCraftedBalls(IEnumerable<BallTypeData> ballTypes)
    {
        EnsureInitialized();

        craftedBalls.Clear();
        if (ballTypes != null)
        {
            foreach (BallTypeData ballType in ballTypes)
            {
                if (ballType != null)
                {
                    craftedBalls.Add(ballType);
                }
            }
        }

        InventoryChanged?.Invoke();
    }

    public List<BallTypeData> ConsumeCraftedBalls()
    {
        EnsureInitialized();

        if (craftedBalls.Count == 0)
        {
            return new List<BallTypeData>();
        }

        List<BallTypeData> consumed = new List<BallTypeData>(craftedBalls);
        craftedBalls.Clear();
        InventoryChanged?.Invoke();
        return consumed;
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

    public void PushStoredMachineResource(BuildingDefinition buildingDefinition, string machineStateId, int resourceAmount)
    {
        EnsureInitialized();
        if (buildingDefinition == null || string.IsNullOrEmpty(machineStateId))
        {
            return;
        }

        BuildingStoredResourceStackEntry entry = GetOrCreateStoredResourceStackEntry(buildingDefinition);
        entry.StoredStates.Add(new StoredMachineResourceState(machineStateId, resourceAmount));
    }

    public bool TryPopStoredMachineResource(BuildingDefinition buildingDefinition, out string machineStateId, out int resourceAmount)
    {
        machineStateId = null;
        resourceAmount = 0;

        EnsureInitialized();
        if (buildingDefinition == null
            || !storedResourceStacksByDefinition.TryGetValue(buildingDefinition, out BuildingStoredResourceStackEntry entry)
            || entry == null
            || entry.StoredStates == null
            || entry.StoredStates.Count == 0)
        {
            return false;
        }

        int lastIndex = entry.StoredStates.Count - 1;
        StoredMachineResourceState state = entry.StoredStates[lastIndex];
        if (state == null || string.IsNullOrEmpty(state.MachineStateId))
        {
            entry.StoredStates.RemoveAt(lastIndex);
            return false;
        }

        machineStateId = state.MachineStateId;
        resourceAmount = Mathf.Max(0, state.StoredAmount);
        entry.StoredStates.RemoveAt(lastIndex);

        if (entry.StoredStates.Count == 0)
        {
            storedResourceStacksByDefinition.Remove(buildingDefinition);
            buildingStoredResourceStacks.Remove(entry);
        }

        return true;
    }

    public bool TryRefundStoredMachineResource(BuildingDefinition buildingDefinition, string machineStateId, int amount, int maxResourceAmount)
    {
        EnsureInitialized();
        if (buildingDefinition == null || string.IsNullOrEmpty(machineStateId) || amount <= 0)
        {
            return false;
        }

        if (!storedResourceStacksByDefinition.TryGetValue(buildingDefinition, out BuildingStoredResourceStackEntry entry)
            || entry == null
            || entry.StoredStates == null
            || entry.StoredStates.Count == 0)
        {
            return false;
        }

        StoredMachineResourceState targetState = null;
        for (int i = entry.StoredStates.Count - 1; i >= 0; i--)
        {
            StoredMachineResourceState state = entry.StoredStates[i];
            if (state != null && state.MachineStateId == machineStateId)
            {
                targetState = state;
                break;
            }
        }

        if (targetState == null)
        {
            return false;
        }

        int current = Mathf.Max(0, targetState.StoredAmount);
        int maxAllowed = maxResourceAmount > 0 ? maxResourceAmount : int.MaxValue;
        targetState.SetStoredAmount(Mathf.Clamp(current + amount, 0, maxAllowed));
        return true;
    }

    // Searches all stored stacks for a matching machineStateId without needing to know the
    // building definition. Used when only the machine state id is available (e.g. items
    // dropped from machine slots whose source generators have since been removed).
    public bool TryRefundStoredMachineResourceById(string machineStateId, int amount)
    {
        EnsureInitialized();
        if (string.IsNullOrEmpty(machineStateId) || amount <= 0)
        {
            return false;
        }

        for (int i = 0; i < buildingStoredResourceStacks.Count; i++)
        {
            BuildingStoredResourceStackEntry entry = buildingStoredResourceStacks[i];
            if (entry?.StoredStates == null)
            {
                continue;
            }

            for (int j = entry.StoredStates.Count - 1; j >= 0; j--)
            {
                StoredMachineResourceState state = entry.StoredStates[j];
                if (state != null && state.MachineStateId == machineStateId)
                {
                    state.SetStoredAmount(Mathf.Max(0, state.StoredAmount) + amount);
                    return true;
                }
            }
        }

        return false;
    }

    public void ClearStoredMachineResources()
    {
        EnsureInitialized();

        if (buildingStoredResourceStacks.Count == 0 && storedResourceStacksByDefinition.Count == 0)
        {
            return;
        }

        buildingStoredResourceStacks.Clear();
        storedResourceStacksByDefinition.Clear();
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

    public void ResetToStartingData()
    {
        EnsureInitialized();

        buildingInventory.Clear();
        buildingsByDefinition.Clear();
        buildingStoredResourceStacks.Clear();
        storedResourceStacksByDefinition.Clear();
        buildingHotbarSlots.Clear();
        EnsureHotbarSlotList();

        itemInventory.Clear();
        itemsByDefinition.Clear();
        craftedBalls.Clear();

        MergeStartingBuildings(startingBuildings);
        MergeStartingItems(startingItems);

        scrap = Mathf.Max(0, startingScrap);
        score = Mathf.Max(0, startingScore);

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

        List<InventoryEntry> sourceBuildings = buildingInventory.Count > 0
            ? new List<InventoryEntry>(buildingInventory)
            : new List<InventoryEntry>(startingBuildings);

        List<ItemInventoryEntry> sourceItems = itemInventory.Count > 0
            ? new List<ItemInventoryEntry>(itemInventory)
            : new List<ItemInventoryEntry>(startingItems);

        bool hasInitialRuntimeOrStartingData = sourceBuildings.Count > 0
            || sourceItems.Count > 0
            || scrap > 0
            || score > 0
            || startingScrap > 0
            || startingScore > 0;

        buildingInventory.Clear();
        buildingsByDefinition.Clear();
        EnsureHotbarSlotList();
        itemInventory.Clear();
        itemsByDefinition.Clear();

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

        RebuildStoredResourceStackLookup();

        foreach (ItemInventoryEntry entry in sourceItems)
        {
            if (entry == null || entry.ItemDefinition == null || entry.Quantity <= 0)
            {
                continue;
            }

            TryGetItemQuantityInternal(entry.ItemDefinition, out int currentQuantity);
            int combinedQuantity = currentQuantity + entry.Quantity;
            SetItemQuantityInternal(entry.ItemDefinition, combinedQuantity, false);
        }

        scrap = Mathf.Max(0, scrap > 0 ? scrap : startingScrap);
        score = Mathf.Max(0, score > 0 ? score : startingScore);

        // If this persistent instance already initialized from runtime or scene-configured
        // starting data, later scene InventoryManager copies should not merge a second set.
        if (hasInitialRuntimeOrStartingData)
        {
            hasImportedSceneStartingData = true;
        }

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
                ClearBuildingFromHotbarSlots(buildingDefinition);
                RemoveStoredResourceStack(buildingDefinition);

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
            AssignBuildingToFirstAvailableHotbarSlot(buildingDefinition);
        }

        if (notifyListeners)
        {
            InventoryChanged?.Invoke();
        }
    }

    private bool TryGetItemQuantityInternal(ItemDefinition itemDefinition, out int quantity)
    {
        if (itemDefinition != null && itemsByDefinition.TryGetValue(itemDefinition, out ItemInventoryEntry entry))
        {
            quantity = entry.Quantity;
            return true;
        }

        quantity = 0;
        return false;
    }

    private void SetItemQuantityInternal(ItemDefinition itemDefinition, int quantity, bool notifyListeners)
    {
        if (itemDefinition == null)
        {
            return;
        }

        int clampedQuantity = Mathf.Max(0, quantity);

        if (clampedQuantity == 0)
        {
            if (itemsByDefinition.TryGetValue(itemDefinition, out ItemInventoryEntry existingEntry))
            {
                itemsByDefinition.Remove(itemDefinition);
                itemInventory.Remove(existingEntry);

                if (notifyListeners)
                {
                    InventoryChanged?.Invoke();
                }
            }

            return;
        }

        if (itemsByDefinition.TryGetValue(itemDefinition, out ItemInventoryEntry entry))
        {
            if (entry.Quantity == clampedQuantity)
            {
                return;
            }

            entry.SetQuantity(clampedQuantity);
        }
        else
        {
            entry = new ItemInventoryEntry(itemDefinition, clampedQuantity);
            itemsByDefinition.Add(itemDefinition, entry);
            itemInventory.Add(entry);
        }

        if (notifyListeners)
        {
            InventoryChanged?.Invoke();
        }
    }

    private void EnsureHotbarSlotList()
    {
        if (buildingHotbarSlots == null)
        {
            buildingHotbarSlots = new List<BuildingDefinition>(BuildingHotbarSlotCount);
        }

        if (buildingHotbarSlots.Count > BuildingHotbarSlotCount)
        {
            buildingHotbarSlots.RemoveRange(BuildingHotbarSlotCount, buildingHotbarSlots.Count - BuildingHotbarSlotCount);
        }

        while (buildingHotbarSlots.Count < BuildingHotbarSlotCount)
        {
            buildingHotbarSlots.Add(null);
        }
    }

    private static bool IsValidHotbarSlotIndex(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < BuildingHotbarSlotCount;
    }

    private void AssignBuildingToFirstAvailableHotbarSlot(BuildingDefinition buildingDefinition)
    {
        if (buildingDefinition == null || buildingHotbarSlots == null)
        {
            return;
        }

        // Pad to full size without calling EnsureHotbarSlotList (avoids recursion).
        while (buildingHotbarSlots.Count < BuildingHotbarSlotCount)
        {
            buildingHotbarSlots.Add(null);
        }

        for (int i = 0; i < buildingHotbarSlots.Count; i++)
        {
            if (buildingHotbarSlots[i] == buildingDefinition)
            {
                return;
            }
        }

        for (int i = 0; i < buildingHotbarSlots.Count; i++)
        {
            if (buildingHotbarSlots[i] == null)
            {
                buildingHotbarSlots[i] = buildingDefinition;
                return;
            }
        }
    }

    private void ClearBuildingFromHotbarSlots(BuildingDefinition buildingDefinition)
    {
        if (buildingDefinition == null || buildingHotbarSlots == null)
        {
            return;
        }

        for (int i = 0; i < buildingHotbarSlots.Count; i++)
        {
            if (buildingHotbarSlots[i] == buildingDefinition)
            {
                buildingHotbarSlots[i] = null;
            }
        }
    }

    private void PrioritizeHotbarDefinitionsAtFront(List<InventoryEntry> sourceEntries)
    {
        if (sourceEntries == null)
        {
            return;
        }

        EnsureHotbarSlotList();

        List<BuildingDefinition> prioritized = new List<BuildingDefinition>();
        for (int i = 0; i < sourceEntries.Count; i++)
        {
            InventoryEntry entry = sourceEntries[i];
            if (entry == null || entry.BuildingDefinition == null)
            {
                continue;
            }

            BuildingDefinition definition = entry.BuildingDefinition;
            if (!prioritized.Contains(definition))
            {
                prioritized.Add(definition);
            }
        }

        if (prioritized.Count == 0)
        {
            return;
        }

        List<BuildingDefinition> reordered = new List<BuildingDefinition>(BuildingHotbarSlotCount);
        for (int i = 0; i < prioritized.Count && reordered.Count < BuildingHotbarSlotCount; i++)
        {
            reordered.Add(prioritized[i]);
        }

        for (int i = 0; i < buildingHotbarSlots.Count && reordered.Count < BuildingHotbarSlotCount; i++)
        {
            BuildingDefinition definition = buildingHotbarSlots[i];
            if (definition == null || reordered.Contains(definition))
            {
                continue;
            }

            reordered.Add(definition);
        }

        while (reordered.Count < BuildingHotbarSlotCount)
        {
            reordered.Add(null);
        }

        for (int i = 0; i < BuildingHotbarSlotCount; i++)
        {
            buildingHotbarSlots[i] = reordered[i];
        }
    }

    private void RebuildStoredResourceStackLookup()
    {
        storedResourceStacksByDefinition.Clear();
        if (buildingStoredResourceStacks == null)
        {
            buildingStoredResourceStacks = new List<BuildingStoredResourceStackEntry>();
            return;
        }

        for (int i = buildingStoredResourceStacks.Count - 1; i >= 0; i--)
        {
            BuildingStoredResourceStackEntry entry = buildingStoredResourceStacks[i];
            if (entry == null || entry.BuildingDefinition == null)
            {
                buildingStoredResourceStacks.RemoveAt(i);
                continue;
            }

            List<StoredMachineResourceState> states = entry.StoredStates;
            if (states == null)
            {
                buildingStoredResourceStacks.RemoveAt(i);
                continue;
            }

            for (int stateIndex = states.Count - 1; stateIndex >= 0; stateIndex--)
            {
                StoredMachineResourceState state = states[stateIndex];
                if (state == null || string.IsNullOrEmpty(state.MachineStateId))
                {
                    states.RemoveAt(stateIndex);
                    continue;
                }

                state.SetStoredAmount(state.StoredAmount);
            }

            if (states.Count == 0)
            {
                buildingStoredResourceStacks.RemoveAt(i);
                continue;
            }

            storedResourceStacksByDefinition[entry.BuildingDefinition] = entry;
        }
    }

    private BuildingStoredResourceStackEntry GetOrCreateStoredResourceStackEntry(BuildingDefinition buildingDefinition)
    {
        if (storedResourceStacksByDefinition.TryGetValue(buildingDefinition, out BuildingStoredResourceStackEntry existing)
            && existing != null)
        {
            return existing;
        }

        BuildingStoredResourceStackEntry created = new BuildingStoredResourceStackEntry(buildingDefinition);
        buildingStoredResourceStacks.Add(created);
        storedResourceStacksByDefinition[buildingDefinition] = created;
        return created;
    }

    private void RemoveStoredResourceStack(BuildingDefinition buildingDefinition)
    {
        if (buildingDefinition == null)
        {
            return;
        }

        if (!storedResourceStacksByDefinition.TryGetValue(buildingDefinition, out BuildingStoredResourceStackEntry entry)
            || entry == null)
        {
            return;
        }

        storedResourceStacksByDefinition.Remove(buildingDefinition);
        buildingStoredResourceStacks.Remove(entry);
    }

}
