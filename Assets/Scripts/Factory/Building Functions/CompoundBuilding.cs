using System;
using System.Collections.Generic;
using UnityEngine;

// Compounder machine.
//
// Layout (at 0 / default rotation):
//   - Two input slots on the LEFT side (top-left and bottom-left tiles of the footprint)
//   - No output tile — the compound ball is sent directly to the crafted-ball queue
//
// When both input slots each hold at least one item the machine consumes one of each,
// looks up their corresponding BallTypeData via the configured mappings, creates a
// runtime compound BallTypeData (inheriting abilities from both sources), and calls
// InventoryManager.AddCraftedBall with the result.
[DisallowMultipleComponent]
public class CompoundBuilding : MonoBehaviour,
    IItemInputReceiver,
    IBuildingInputPreview,
    IBuildingOutputPreview,
    IMachineResourceProgressProvider,
    IMachineProgressDisplayInfo,
    IMachinePendingItemDropper
{
    public enum InputSide
    {
        Right,
        Up,
        Left,
        Down
    }

    [Serializable]
    private class ItemToBallMapping
    {
        public ItemDefinition SourceItem;
        public BallTypeData SourceBallType;
    }

    private sealed class CachedCompoundOutput
    {
        public string Key;
        public BallTypeData BallType;
        public ItemDefinition ItemDefinition;
    }

    private static readonly Dictionary<string, CachedCompoundOutput> CachedOutputsByKey = new();

    [Header("References")]
    [SerializeField] private BuildingInstance buildingInstance;
    [SerializeField] private TileManager tileManager;
    [SerializeField] private ItemEntity itemEntityPrefab;
    [SerializeField] private Transform spawnedItemParent;

    [Header("Runtime Parent")]
    [SerializeField] private bool autoAssignSpawnedItemParent = true;
    [SerializeField] private string runtimeItemsParentName = "Runtime Items";

    [Header("Layout")]
    [Tooltip("Which side of the building the two inputs are on (at 0 rotation).")]
    [SerializeField] private InputSide inputSide = InputSide.Left;
    [Tooltip("How many of each input item can be stored per slot.")]
    [SerializeField, Min(1)] private int maxPerSlot = 10;

    [Header("Ball Mappings")]
    [Tooltip("Maps each accepted ItemDefinition to its source BallTypeData for compounding.")]
    [SerializeField] private List<ItemToBallMapping> itemMappings = new();

    [Header("Output")]
    [SerializeField, Min(0.01f)] private float outputTravelDurationSeconds = 0.5f;
    [SerializeField] private Vector3 itemSpawnOffset = Vector3.zero;

    // ── Runtime state ─────────────────────────────────────────────────────────

    private ItemDefinition slotADefinition;
    private ItemDefinition slotBDefinition;
    private int slotAAmount;
    private int slotBAmount;
    private readonly List<List<string>> slotASourceIds = new();
    private readonly List<List<string>> slotBSourceIds = new();
    private bool hasItem;
    private ItemDefinition pendingOutputDefinition;
    private int pendingOutputQuantity;
    private readonly List<string> pendingOutputOriginIds = new();
    private Color firstInputTint = Color.white;
    private bool hasFirstInputTint;
    private BallTypeData lastCompoundBall;
    private ItemEntity launchingItem;
    private float launchMoveTimer;
    private Vector3 launchStartWorldPosition;
    private Vector3 launchTargetWorldPosition;

    // ─────────────────────────────────────────────────────────────────────────

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
    }

    private void Update()
    {
        TickLaunchMovement();
        ScanInputTilesForItems();

        if (launchingItem != null)
        {
            return;
        }

        TryCompound();
    }

    private void ScanInputTilesForItems()
    {
        GetInputTilesWorld(out Vector2Int tileA, out Vector2Int tileB);
        FactoryMachineUtility.TryScanAndAbsorbItemAtTile(this, tileA, tileManager);
        FactoryMachineUtility.TryScanAndAbsorbItemAtTile(this, tileB, tileManager);
    }

    // ── IItemInputReceiver ────────────────────────────────────────────────────

    public bool CanAcceptItemAtTile(Vector2Int tile, ItemEntity item)
    {
        if (hasItem || item == null || item.ItemDefinition == null)
        {
            return false;
        }

        if (item.ItemDefinition.IsCompound)
        {
            return false;
        }

        // Only accept items that have a ball mapping
        if (FindBallType(item.ItemDefinition) == null)
        {
            return false;
        }

        GetInputTilesWorld(out Vector2Int tileA, out Vector2Int tileB);
        ItemDefinition reservedForTileA = GetReservedIncomingDefinitionForTile(tileA, item);
        ItemDefinition reservedForTileB = GetReservedIncomingDefinitionForTile(tileB, item);

        if (tile == tileA)
        {
            ItemDefinition otherDefinition = slotBDefinition ?? reservedForTileB;
            if (otherDefinition == item.ItemDefinition)
            {
                return false;
            }

            return FactoryMachineUtility.CanAcceptIntoSlot(item.ItemDefinition, slotADefinition, slotAAmount, item.Quantity, maxPerSlot);
        }

        if (tile == tileB)
        {
            ItemDefinition otherDefinition = slotADefinition ?? reservedForTileA;
            if (otherDefinition == item.ItemDefinition)
            {
                return false;
            }

            return FactoryMachineUtility.CanAcceptIntoSlot(item.ItemDefinition, slotBDefinition, slotBAmount, item.Quantity, maxPerSlot);
        }

        return false;
    }

    public bool TryAcceptItem(ItemEntity item, Vector2Int tile)
    {
        if (hasItem || item == null || item.ItemDefinition == null)
        {
            return false;
        }

        if (item.ItemDefinition.IsCompound)
        {
            return false;
        }

        if (FindBallType(item.ItemDefinition) == null)
        {
            return false;
        }

        GetInputTilesWorld(out Vector2Int tileA, out Vector2Int tileB);

        int amount = Mathf.Max(1, item.Quantity);
        bool acceptedIntoA = false;

        if (tile == tileA)
        {
            if (slotBDefinition == item.ItemDefinition ||
                !FactoryMachineUtility.CanAcceptIntoSlot(item.ItemDefinition, slotADefinition, slotAAmount, amount, maxPerSlot))
            {
                return false;
            }

            FactoryMachineUtility.AcceptIntoSlot(item.ItemDefinition, amount, ref slotADefinition, ref slotAAmount);
            MachineSlotSourceTracker.Append(slotASourceIds, item, amount);
            acceptedIntoA = true;
        }
        else if (tile == tileB)
        {
            if (slotADefinition == item.ItemDefinition ||
                !FactoryMachineUtility.CanAcceptIntoSlot(item.ItemDefinition, slotBDefinition, slotBAmount, amount, maxPerSlot))
            {
                return false;
            }

            FactoryMachineUtility.AcceptIntoSlot(item.ItemDefinition, amount, ref slotBDefinition, ref slotBAmount);
            MachineSlotSourceTracker.Append(slotBSourceIds, item, amount);
        }
        else
        {
            return false;
        }

        // Defensive rollback: avoid illegal duplicate pairs under same-tick multi-item insertion.
        if (slotADefinition != null && slotADefinition == slotBDefinition)
        {
            FactoryMachineUtility.RollbackAcceptedInput(
                item.ItemDefinition,
                amount,
                acceptedIntoA,
                ref slotADefinition,
                ref slotAAmount,
                ref slotBDefinition,
                ref slotBAmount);

            MachineSlotSourceTracker.TrimFromEnd(acceptedIntoA ? slotASourceIds : slotBSourceIds, amount);
            return false;
        }

        RegisterFirstInputTint(item.ItemDefinition);
        Destroy(item.gameObject);
        return true;
    }

    public int GetRequiredInputDirectionQuarterTurns()
    {
        ResolveDependenciesIfNeeded();
        int baseInputQuarterTurns = (int)inputSide;
        int rotationQuarterTurns = buildingInstance != null ? buildingInstance.RotationQuarterTurns : 0;
        return (baseInputQuarterTurns + rotationQuarterTurns) % 4;
    }

    // ── IBuildingInputPreview ─────────────────────────────────────────────────

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

        BuildBothInputTiles(topLeftGridPosition, footprintSize, rotationQuarterTurns, out Vector2Int tileA, out Vector2Int tileB);
        inputTiles.Add(tileA);
        inputTiles.Add(tileB);
    }

    public bool TryGetOutputTile(
        Vector2Int topLeftGridPosition,
        Vector2Int footprintSize,
        int rotationQuarterTurns,
        out Vector2Int outputTile,
        out Vector2Int outputDirection)
    {
        Vector2Int baseInputDirection = GetBaseDirection(inputSide);
        Vector2Int baseOutputDirection = -baseInputDirection;

        outputDirection = FactoryGridDirectionUtility.RotateDirection(baseOutputDirection, rotationQuarterTurns);
        outputTile = BuildOutputGridPosition(topLeftGridPosition, footprintSize, rotationQuarterTurns);
        return true;
    }

    // ── Compounding logic ─────────────────────────────────────────────────────

    private void TryCompound()
    {
        if (hasItem)
        {
            TryReleasePendingOutput();
            return;
        }

        if (slotADefinition == null || slotBDefinition == null)
        {
            return;
        }

        if (slotAAmount < 1 || slotBAmount < 1)
        {
            return;
        }

        BallTypeData ballA = FindBallType(slotADefinition);
        BallTypeData ballB = FindBallType(slotBDefinition);

        if (ballA == null || ballB == null)
        {
            return;
        }

        CachedCompoundOutput compoundOutput = ResolveOrCreateCompoundOutput(slotADefinition, slotBDefinition, ballA, ballB);
        if (compoundOutput == null || compoundOutput.ItemDefinition == null)
        {
            return;
        }

        slotAAmount -= 1;
        pendingOutputOriginIds.Clear();
        MachineSlotSourceTracker.TakeFromFront(slotASourceIds, 1, pendingOutputOriginIds);
        if (slotAAmount <= 0)
        {
            slotADefinition = null;
        }

        slotBAmount -= 1;
        MachineSlotSourceTracker.TakeFromFront(slotBSourceIds, 1, pendingOutputOriginIds);
        if (slotBAmount <= 0)
        {
            slotBDefinition = null;
        }

        pendingOutputDefinition = compoundOutput.ItemDefinition;
        pendingOutputQuantity = 1;
        hasItem = true;
        lastCompoundBall = compoundOutput.BallType;

        TryReleasePendingOutput();

        if (slotADefinition == null && slotBDefinition == null)
        {
            ResetInputTintState();
        }
    }

    // ── IMachineResourceProgressProvider ─────────────────────────────────────

    public int CurrentResourceAmount => slotAAmount + slotBAmount;
    public int MaxResourceAmount => maxPerSlot * 2;
    public float NormalizedResourceAmount => MaxResourceAmount > 0
        ? Mathf.Clamp01((float)CurrentResourceAmount / MaxResourceAmount)
        : 0f;
    public Color ResourceTint => hasFirstInputTint ? firstInputTint : Color.white;

    // ── IMachineProgressDisplayInfo ───────────────────────────────────────────

    public bool HasProgressDisplay => HasPendingOrLaunchingOutput || slotAAmount > 0 || slotBAmount > 0;
    public bool UseQuestionMarkSprite => !HasPendingOrLaunchingOutput;
    public Sprite ProgressDisplaySprite => HasPendingOrLaunchingOutput
        ? (pendingOutputDefinition != null ? pendingOutputDefinition.Icon : launchingItem != null ? launchingItem.ItemDefinition?.Icon : null)
        : null;
    public Color ProgressDisplayTint => HasPendingOrLaunchingOutput
        ? (pendingOutputDefinition != null
            ? pendingOutputDefinition.Tint
            : launchingItem != null && launchingItem.ItemDefinition != null
                ? launchingItem.ItemDefinition.Tint
                : ResourceTint)
        : ResourceTint;

    // ── Ball mapping lookup ───────────────────────────────────────────────────

    private BallTypeData FindBallType(ItemDefinition definition)
    {
        if (definition == null)
        {
            return null;
        }

        for (int i = 0; i < itemMappings.Count; i++)
        {
            if (itemMappings[i].SourceItem == definition)
            {
                return itemMappings[i].SourceBallType;
            }
        }

        return null;
    }

    private CachedCompoundOutput ResolveOrCreateCompoundOutput(
        ItemDefinition sourceItemA,
        ItemDefinition sourceItemB,
        BallTypeData ballA,
        BallTypeData ballB)
    {
        if (sourceItemA == null || sourceItemB == null || ballA == null || ballB == null)
        {
            return null;
        }

        string key = BuildCompoundKey(sourceItemA, sourceItemB);
        if (CachedOutputsByKey.TryGetValue(key, out CachedCompoundOutput cachedOutput) &&
            cachedOutput != null &&
            cachedOutput.ItemDefinition != null &&
            cachedOutput.BallType != null)
        {
            return cachedOutput;
        }

        BallTypeData compoundBall = ScriptableObject.CreateInstance<BallTypeData>();
        compoundBall.InitializeAsCompound(ballA, ballB);
        compoundBall.name = compoundBall.DisplayName;

        ItemDefinition compoundItem = ScriptableObject.CreateInstance<ItemDefinition>();
        Color compoundItemTint = Color.Lerp(sourceItemA.Tint, sourceItemB.Tint, 0.5f);
        compoundItem.InitializeAsRuntimeCompound(
            compoundBall,
            $"item.compound.{key}",
            sourceItemA.BaseValue + sourceItemB.BaseValue,
            sourceItemA.Icon,
            compoundItemTint);

        CachedCompoundOutput newOutput = new CachedCompoundOutput
        {
            Key = key,
            BallType = compoundBall,
            ItemDefinition = compoundItem
        };

        CachedOutputsByKey[key] = newOutput;
        return newOutput;
    }

    private static string BuildCompoundKey(ItemDefinition inputItemA, ItemDefinition inputItemB)
    {
        string firstId = string.IsNullOrWhiteSpace(inputItemA.ItemId) ? inputItemA.name : inputItemA.ItemId;
        string secondId = string.IsNullOrWhiteSpace(inputItemB.ItemId) ? inputItemB.name : inputItemB.ItemId;
        return $"{firstId}__{secondId}".ToLowerInvariant().Replace(" ", ".");
    }

    private bool TryReleasePendingOutput()
    {
        if (!hasItem || pendingOutputDefinition == null || pendingOutputQuantity <= 0 || itemEntityPrefab == null)
        {
            return false;
        }

        ResolveDependenciesIfNeeded();
        if (buildingInstance == null || tileManager == null)
        {
            return false;
        }

        if (!TryGetOutputGridPosition(out Vector2Int outputTile))
        {
            return false;
        }

        if (!TryGetLaunchStartTile(outputTile, out Vector2Int launchStartTile))
        {
            return false;
        }

        if (ItemEntitySceneQuery.HasItemAtOrReservedTile(tileManager, launchStartTile) ||
            ItemEntitySceneQuery.HasItemAtOrReservedTile(tileManager, outputTile))
        {
            return false;
        }

        Vector3 spawnWorldPos = tileManager.GridToWorld(launchStartTile) + itemSpawnOffset;
        Vector3 targetWorldPos = tileManager.GridToWorld(outputTile) + itemSpawnOffset;

        ItemEntity spawnedItem = Instantiate(itemEntityPrefab, spawnWorldPos, Quaternion.identity, spawnedItemParent);
        spawnedItem.Initialize(pendingOutputDefinition, pendingOutputQuantity);
        spawnedItem.SetOriginSourceIds(pendingOutputOriginIds);

        if (!spawnedItem.TryClaim(this))
        {
            Destroy(spawnedItem.gameObject);
            return false;
        }

        if (!BeginLaunch(spawnedItem, spawnWorldPos, targetWorldPos))
        {
            return false;
        }

        FactoryMachineUtility.ClearPendingOutput(ref hasItem, ref pendingOutputDefinition, ref pendingOutputQuantity);
        pendingOutputOriginIds.Clear();
        return true;
    }

    public bool TryDropPendingItemToGround()
    {
        ResolveDependenciesIfNeeded();
        if (buildingInstance == null || tileManager == null)
        {
            return false;
        }

        bool droppedSomething = false;

        // Refund the pending output's constituent inputs directly to their generators
        // instead of dropping the compound item, so the player can rebuild the inputs.
        if (hasItem && pendingOutputDefinition != null && pendingOutputQuantity > 0)
        {
            droppedSomething |= RefundOrDropCompositeOutput(
                pendingOutputDefinition,
                pendingOutputQuantity,
                pendingOutputOriginIds);
            FactoryMachineUtility.ClearPendingOutput(ref hasItem, ref pendingOutputDefinition, ref pendingOutputQuantity);
            pendingOutputOriginIds.Clear();
        }

        droppedSomething |= DropStoredInputsToGround();
        return droppedSomething;
    }

    private bool DropStoredInputsToGround()
    {
        GetInputTilesWorld(out Vector2Int tileA, out Vector2Int tileB);
        bool refundedAny = false;

        if (slotADefinition != null && slotAAmount > 0)
        {
            Vector3 dropPos = tileManager.GridToWorld(tileA) + itemSpawnOffset;
            refundedAny |= MachineSlotSourceTracker.RefundOrDropAll(
                slotASourceIds, slotADefinition, dropPos, itemEntityPrefab, spawnedItemParent);
            slotADefinition = null;
            slotAAmount = 0;
        }

        if (slotBDefinition != null && slotBAmount > 0)
        {
            Vector3 dropPos = tileManager.GridToWorld(tileB) + itemSpawnOffset;
            refundedAny |= MachineSlotSourceTracker.RefundOrDropAll(
                slotBSourceIds, slotBDefinition, dropPos, itemEntityPrefab, spawnedItemParent);
            slotBDefinition = null;
            slotBAmount = 0;
        }

        return refundedAny;
    }

    // Refund every constituent input of the pending output (one refund per source id)
    // back to its originating generator. Falls back to dropping the compound item if
    // any constituent generator no longer exists.
    private bool RefundOrDropCompositeOutput(ItemDefinition definition, int quantity, List<string> originIds)
    {
        if (definition == null || quantity <= 0)
        {
            return false;
        }

        bool fullyRefunded = originIds != null && originIds.Count > 0;
        if (fullyRefunded)
        {
            for (int i = 0; i < originIds.Count; i++)
            {
                string id = originIds[i];
                if (string.IsNullOrEmpty(id) || !GeneratorBuilding.TryRefundByMachineStateId(id, 1))
                {
                    fullyRefunded = false;
                }
            }
        }

        if (fullyRefunded)
        {
            return true;
        }

        if (itemEntityPrefab == null || !TryGetOutputGridPosition(out Vector2Int outputTile))
        {
            return false;
        }

        if (!TryGetLaunchStartTile(outputTile, out Vector2Int launchStartTile))
        {
            return false;
        }

        Vector3 dropWorldPosition = tileManager.GridToWorld(launchStartTile) + itemSpawnOffset;
        ItemEntity droppedItem = Instantiate(itemEntityPrefab, dropWorldPosition, Quaternion.identity, spawnedItemParent);
        droppedItem.Initialize(definition, quantity);
        if (originIds != null && originIds.Count > 0)
        {
            droppedItem.SetOriginSourceIds(originIds);
        }

        return true;
    }

    private bool TryGetOutputGridPosition(out Vector2Int outputGridPosition)
    {
        outputGridPosition = default;

        if (buildingInstance == null || tileManager == null)
        {
            return false;
        }

        Vector2Int footprintSize = buildingInstance.FootprintSize;
        if (footprintSize.x <= 0 || footprintSize.y <= 0)
        {
            return false;
        }

        outputGridPosition = BuildOutputGridPosition(
            buildingInstance.GridPosition,
            footprintSize,
            buildingInstance.RotationQuarterTurns);

        return tileManager.IsInBounds(outputGridPosition);
    }

    private Vector2Int BuildOutputGridPosition(Vector2Int topLeft, Vector2Int footprintSize, int rotationQuarterTurns)
    {
        Vector2Int baseInputDirection = GetBaseDirection(inputSide);
        Vector2Int baseOutputDirection = -baseInputDirection;
        Vector2Int worldOutputDirection = FactoryGridDirectionUtility.RotateDirection(baseOutputDirection, rotationQuarterTurns);
        Vector2Int outputOffset = FactoryGridDirectionUtility.GetSideOffset(worldOutputDirection, footprintSize);

        return topLeft + outputOffset;
    }

    private bool BeginLaunch(ItemEntity item, Vector3 startWorldPosition, Vector3 targetWorldPosition)
    {
        if (!TryGetOutputGridPosition(out Vector2Int outputTile))
        {
            Destroy(item.gameObject);
            return false;
        }

        if (!item.TryReserveDestination(this, outputTile))
        {
            Destroy(item.gameObject);
            return false;
        }

        launchingItem = item;
        launchStartWorldPosition = startWorldPosition;
        launchTargetWorldPosition = tileManager.GridToWorld(outputTile) + itemSpawnOffset;
        launchMoveTimer = 0f;
        return true;
    }

    private void TickLaunchMovement()
    {
        if (launchingItem == null)
        {
            return;
        }

        launchMoveTimer += FactoryBuildingPlacer.FactoryDeltaTime;
        float t = Mathf.Clamp01(launchMoveTimer / outputTravelDurationSeconds);
        launchingItem.transform.position = Vector3.Lerp(launchStartWorldPosition, launchTargetWorldPosition, t);

        if (t < 1f)
        {
            return;
        }

        launchingItem.transform.position = launchTargetWorldPosition;
        launchingItem.ClearReservedDestination(this);
        launchingItem.ReleaseClaim(this);
        launchingItem = null;
        launchMoveTimer = 0f;
    }

    private bool TryGetLaunchStartTile(Vector2Int outputTile, out Vector2Int launchStartTile)
    {
        Vector2Int outputDirection = FactoryGridDirectionUtility.RotateDirection(
            -GetBaseDirection(inputSide),
            buildingInstance != null ? buildingInstance.RotationQuarterTurns : 0);
        launchStartTile = outputTile - outputDirection;
        return tileManager != null && tileManager.IsInBounds(launchStartTile);
    }

    // ── Input tile helpers ────────────────────────────────────────────────────

    private void GetInputTilesWorld(out Vector2Int tileA, out Vector2Int tileB)
    {
        ResolveDependenciesIfNeeded();
        Vector2Int topLeft = buildingInstance != null ? buildingInstance.GridPosition : Vector2Int.zero;
        Vector2Int footprintSize = buildingInstance != null ? buildingInstance.FootprintSize : Vector2Int.one;
        int rotation = buildingInstance != null ? buildingInstance.RotationQuarterTurns : 0;
        BuildBothInputTiles(topLeft, footprintSize, rotation, out tileA, out tileB);
    }

    private void BuildBothInputTiles(
        Vector2Int topLeftGridPosition,
        Vector2Int footprintSize,
        int rotationQuarterTurns,
        out Vector2Int tileA,
        out Vector2Int tileB)
    {
        Vector2Int baseInputDirection = GetBaseDirection(inputSide);
        Vector2Int worldInputDirection = FactoryGridDirectionUtility.RotateDirection(baseInputDirection, rotationQuarterTurns);

        Vector2Int offsetA = GetInputEdgeTileOffset(worldInputDirection, footprintSize, topSlot: true);
        Vector2Int offsetB = GetInputEdgeTileOffset(worldInputDirection, footprintSize, topSlot: false);

        tileA = topLeftGridPosition + offsetA;
        tileB = topLeftGridPosition + offsetB;
    }

    private static Vector2Int GetInputEdgeTileOffset(Vector2Int direction, Vector2Int footprintSize, bool topSlot)
    {
        if (direction == Vector2Int.left)
        {
            return topSlot
                ? new Vector2Int(0, footprintSize.y - 1)
                : new Vector2Int(0, 0);
        }

        if (direction == Vector2Int.right)
        {
            return topSlot
                ? new Vector2Int(footprintSize.x - 1, footprintSize.y - 1)
                : new Vector2Int(footprintSize.x - 1, 0);
        }

        if (direction == Vector2Int.up)
        {
            return topSlot
                ? new Vector2Int(footprintSize.x - 1, footprintSize.y - 1)
                : new Vector2Int(0, footprintSize.y - 1);
        }

        // Down
        return topSlot
            ? new Vector2Int(footprintSize.x - 1, 0)
            : new Vector2Int(0, 0);
    }

    // ── Slot helpers ──────────────────────────────────────────────────────────

    private ItemDefinition GetReservedIncomingDefinitionForTile(Vector2Int tile, ItemEntity ignoredItem)
    {
        ItemEntity[] items = ItemEntitySceneQuery.GetItems();
        for (int i = 0; i < items.Length; i++)
        {
            ItemEntity candidate = items[i];
            if (candidate == null || candidate == ignoredItem || candidate.ItemDefinition == null)
            {
                continue;
            }

            if (candidate.TryGetReservedDestination(out Vector2Int reservedTile) && reservedTile == tile)
            {
                return candidate.ItemDefinition;
            }
        }

        return null;
    }

    // ── Tint helpers ──────────────────────────────────────────────────────────

    private void RegisterFirstInputTint(ItemDefinition inputDefinition)
    {
        if (hasFirstInputTint || inputDefinition == null)
        {
            return;
        }

        firstInputTint = inputDefinition.Tint;
        hasFirstInputTint = true;
    }

    private void ResetInputTintState()
    {
        firstInputTint = Color.white;
        hasFirstInputTint = false;
    }

    // ── Direction helpers ─────────────────────────────────────────────────────

    private static Vector2Int GetBaseDirection(InputSide side)
    {
        return FactoryGridDirectionUtility.DirectionFromQuarterTurns((int)side);
    }

    private bool HasPendingOrLaunchingOutput => hasItem || launchingItem != null;

    // ── Dependency resolution ─────────────────────────────────────────────────

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

        ResolveSpawnedItemParentIfNeeded();
    }

    private void ResolveSpawnedItemParentIfNeeded()
    {
        if (spawnedItemParent != null || !autoAssignSpawnedItemParent)
        {
            return;
        }

        string parentName = string.IsNullOrWhiteSpace(runtimeItemsParentName)
            ? "Runtime Items"
            : runtimeItemsParentName.Trim();

        GameObject existingParent = GameObject.Find(parentName);
        if (existingParent != null)
        {
            spawnedItemParent = existingParent.transform;
            return;
        }

        GameObject newParent = new GameObject(parentName);
        spawnedItemParent = newParent.transform;
    }
}
