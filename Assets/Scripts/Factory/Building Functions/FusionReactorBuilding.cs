using System.Collections.Generic;
using UnityEngine;

// Fusion Reactor machine.
//
// Layout (at 0 (default) rotation):
//   - Two input slots on the LEFT side  (top-left and bottom-left tiles of the footprint)
//   - One output slot on the RIGHT side (centre of the right edge)
//
// When both inputs have received sufficient items matching a recipe in the database the
// machine produces the output item and ejects it from the output side.
[DisallowMultipleComponent]
public class FusionReactorBuilding : MonoBehaviour, IItemInputReceiver, IBuildingInputPreview, IBuildingOutputPreview, IMachineResourceProgressProvider, IMachineProgressDisplayInfo, IMachinePendingItemDropper
{
    public enum InputSide
    {
        Right,
        Up,
        Left,
        Down
    }

    public sealed class MoveState
    {
        public ItemDefinition SlotADefinition;
        public ItemDefinition SlotBDefinition;
        public int SlotAAmount;
        public int SlotBAmount;
        public bool HasItem;
        public ItemDefinition PendingOutputDefinition;
        public int PendingOutputQuantity;
        public Color FirstInputTint;
        public bool HasFirstInputTint;
    }

    [Header("References")]
    [SerializeField] private BuildingInstance buildingInstance;
    [SerializeField] private TileManager tileManager;
    [SerializeField] private ItemEntity itemEntityPrefab;
    [SerializeField] private Transform spawnedItemParent;

    [Header("Runtime Parent")]
    [SerializeField] private bool autoAssignSpawnedItemParent = true;
    [SerializeField] private string runtimeItemsParentName = "Runtime Items";

    [Header("Recipes")]
    [SerializeField] private FusionReactorRecipeDatabase recipeDatabase;

    [Header("Layout")]
    [Tooltip("Which side of the building the two inputs are on (at 0 rotation).")]
    [SerializeField] private InputSide inputSide = InputSide.Left;
    [Tooltip("How many of each input item can be stored per slot.")]
    [SerializeField, Min(1)] private int maxPerSlot = 10;

    [Header("Output")]
    [SerializeField, Min(0.01f)] private float outputTravelDurationSeconds = 0.5f;
    [SerializeField] private Vector3 itemSpawnOffset = Vector3.zero;

    // ── Runtime state ─────────────────────────────────────────────────────────

    // Two input slots; slot 0 = "top" (or first), slot 1 = "bottom" (or second)
    private ItemDefinition slotADefinition;
    private ItemDefinition slotBDefinition;
    private int slotAAmount;
    private int slotBAmount;
    private bool hasItem;
    private ItemDefinition pendingOutputDefinition;
    private int pendingOutputQuantity;
    private Color firstInputTint = Color.white;
    private bool hasFirstInputTint;

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

        if (launchingItem != null)
        {
            return;
        }

        if (hasItem)
        {
            TryReleasePendingOutput();
            return;
        }

        TryFuse();
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

        GetInputTilesWorld(out Vector2Int tileA, out Vector2Int tileB);
        ItemDefinition reservedForTileA = GetReservedIncomingDefinitionForTile(tileA, item);
        ItemDefinition reservedForTileB = GetReservedIncomingDefinitionForTile(tileB, item);

        if (tile == tileA)
        {
            ItemDefinition otherDefinition = slotBDefinition ?? reservedForTileB;
            if (!CanParticipateInRecipe(item.ItemDefinition, otherDefinition))
            {
                return false;
            }

            return FactoryMachineUtility.CanAcceptIntoSlot(item.ItemDefinition, slotADefinition, slotAAmount, item.Quantity, maxPerSlot);
        }

        if (tile == tileB)
        {
            ItemDefinition otherDefinition = slotADefinition ?? reservedForTileA;
            if (!CanParticipateInRecipe(item.ItemDefinition, otherDefinition))
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

        GetInputTilesWorld(out Vector2Int tileA, out Vector2Int tileB);

        int amount = Mathf.Max(1, item.Quantity);
        bool acceptedIntoA = false;

        if (tile == tileA)
        {
            if (!CanParticipateInRecipe(item.ItemDefinition, slotBDefinition) ||
                !FactoryMachineUtility.CanAcceptIntoSlot(item.ItemDefinition, slotADefinition, slotAAmount, amount, maxPerSlot))
            {
                return false;
            }

            FactoryMachineUtility.AcceptIntoSlot(item.ItemDefinition, amount, ref slotADefinition, ref slotAAmount);
            acceptedIntoA = true;
        }
        else if (tile == tileB)
        {
            if (!CanParticipateInRecipe(item.ItemDefinition, slotADefinition) ||
                !FactoryMachineUtility.CanAcceptIntoSlot(item.ItemDefinition, slotBDefinition, slotBAmount, amount, maxPerSlot))
            {
                return false;
            }

            FactoryMachineUtility.AcceptIntoSlot(item.ItemDefinition, amount, ref slotBDefinition, ref slotBAmount);
        }
        else
        {
            return false;
        }

        // Defensive rollback: keep pair-valid state even when multiple insertions resolve in one tick.
        if (!IsCurrentPairRecipeValid())
        {
            FactoryMachineUtility.RollbackAcceptedInput(
                item.ItemDefinition,
                amount,
                acceptedIntoA,
                ref slotADefinition,
                ref slotAAmount,
                ref slotBDefinition,
                ref slotBAmount);

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

    // ── Fusion logic ──────────────────────────────────────────────────────────

    private void TryFuse()
    {
        if (hasItem)
        {
            TryReleasePendingOutput();
            return;
        }

        if (recipeDatabase == null || slotADefinition == null || slotBDefinition == null)
        {
            return;
        }

        FusionReactorRecipe recipe = recipeDatabase.FindRecipe(slotADefinition, slotBDefinition);
        if (recipe == null || recipe.Output == null || recipe.OutputQuantity <= 0)
        {
            return;
        }

        // Determine which slot maps to which recipe input
        bool aIsInputA = slotADefinition == recipe.InputA;
        int costForSlotA = aIsInputA ? recipe.CostA : recipe.CostB;
        int costForSlotB = aIsInputA ? recipe.CostB : recipe.CostA;

        if (slotAAmount < costForSlotA || slotBAmount < costForSlotB)
        {
            return;
        }

        // Consume inputs with consolidated clearing logic
        slotAAmount -= costForSlotA;
        if (slotAAmount <= 0)
        {
            slotADefinition = null;
        }

        slotBAmount -= costForSlotB;
        if (slotBAmount <= 0)
        {
            slotBDefinition = null;
        }

        pendingOutputDefinition = recipe.Output;
        pendingOutputQuantity = recipe.OutputQuantity;
        hasItem = true;
        TryReleasePendingOutput();
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
        ResetInputTintState();
        return true;
    }

    // ── Output position ───────────────────────────────────────────────────────

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
        // The output is on the opposite side from the inputs.
        Vector2Int baseInputDirection = GetBaseDirection(inputSide);
        Vector2Int baseOutputDirection = -baseInputDirection;
        Vector2Int worldOutputDirection = FactoryGridDirectionUtility.RotateDirection(baseOutputDirection, rotationQuarterTurns);
        Vector2Int outputOffset = FactoryGridDirectionUtility.GetSideOffset(worldOutputDirection, footprintSize);

        return topLeft + outputOffset;
    }

    // ── Input tile helpers ────────────────────────────────────────────────────

    // Returns the two input grid positions using the building's live state.
    private void GetInputTilesWorld(out Vector2Int tileA, out Vector2Int tileB)
    {
        ResolveDependenciesIfNeeded();
        Vector2Int topLeft = buildingInstance != null ? buildingInstance.GridPosition : Vector2Int.zero;
        Vector2Int footprintSize = buildingInstance != null ? buildingInstance.FootprintSize : Vector2Int.one;
        int rotation = buildingInstance != null ? buildingInstance.RotationQuarterTurns : 0;
        BuildBothInputTiles(topLeft, footprintSize, rotation, out tileA, out tileB);
    }

    // Calculates both input tile positions given a top-left, footprint size, and rotation.
    // Slot A = "top" position on the input side; Slot B = "bottom" position.
    private void BuildBothInputTiles(
        Vector2Int topLeftGridPosition,
        Vector2Int footprintSize,
        int rotationQuarterTurns,
        out Vector2Int tileA,
        out Vector2Int tileB)
    {
        Vector2Int baseInputDirection = GetBaseDirection(inputSide);
        Vector2Int worldInputDirection = FactoryGridDirectionUtility.RotateDirection(baseInputDirection, rotationQuarterTurns);

        // "Top" and "bottom" offsets along the input edge (both on the inner edge of the footprint)
        Vector2Int offsetA = GetInputEdgeTileOffset(worldInputDirection, footprintSize, topSlot: true);
        Vector2Int offsetB = GetInputEdgeTileOffset(worldInputDirection, footprintSize, topSlot: false);

        tileA = topLeftGridPosition + offsetA;
        tileB = topLeftGridPosition + offsetB;
    }

    // Returns an offset (relative to top-left) pointing to either the "top" or "bottom" tile
    // on the input edge of the building's footprint.
    // For a left-side input on a 2×3 footprint, top slot = (0,2) and bottom slot = (0,0).
    private static Vector2Int GetInputEdgeTileOffset(Vector2Int direction, Vector2Int footprintSize, bool topSlot)
    {
        // The input edge interior tiles lie at x=0 (left) or x=w-1 (right) or y=0 (down) or y=h-1 (up).
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

    private bool IsCurrentPairRecipeValid()
    {
        if (slotADefinition == null || slotBDefinition == null)
        {
            return true;
        }

        if (recipeDatabase == null)
        {
            return false;
        }

        FusionReactorRecipe recipe = recipeDatabase.FindRecipe(slotADefinition, slotBDefinition);
        return recipe != null && recipe.Output != null && recipe.OutputQuantity > 0;
    }

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

    private bool CanParticipateInRecipe(ItemDefinition incomingDefinition, ItemDefinition otherSlotDefinition)
    {
        if (incomingDefinition == null || recipeDatabase == null)
        {
            return false;
        }

        IReadOnlyList<FusionReactorRecipe> recipes = recipeDatabase.Recipes;
        if (recipes == null)
        {
            return false;
        }

        for (int i = 0; i < recipes.Count; i++)
        {
            FusionReactorRecipe recipe = recipes[i];
            if (recipe == null || recipe.Output == null || recipe.OutputQuantity <= 0)
            {
                continue;
            }

            if (otherSlotDefinition == null)
            {
                if (recipe.InputA == incomingDefinition || recipe.InputB == incomingDefinition)
                {
                    return true;
                }

                continue;
            }

            if (recipe.Matches(incomingDefinition, otherSlotDefinition))
            {
                return true;
            }
        }

        return false;
    }

    // ── Launch helpers ────────────────────────────────────────────────────────

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

    public bool TryDropPendingItemToGround()
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

        if (!TryGetOutputGridPosition(out Vector2Int outputTile) ||
            !TryGetLaunchStartTile(outputTile, out Vector2Int launchStartTile))
        {
            return false;
        }

        Vector3 dropWorldPosition = tileManager.GridToWorld(launchStartTile) + itemSpawnOffset;
        ItemEntity droppedItem = Instantiate(itemEntityPrefab, dropWorldPosition, Quaternion.identity, spawnedItemParent);
        droppedItem.Initialize(pendingOutputDefinition, pendingOutputQuantity);

        FactoryMachineUtility.ClearPendingOutput(ref hasItem, ref pendingOutputDefinition, ref pendingOutputQuantity);
        ResetInputTintState();
        return true;
    }

    private bool TryGetLaunchStartTile(Vector2Int outputTile, out Vector2Int launchStartTile)
    {
        Vector2Int outputDirection = FactoryGridDirectionUtility.RotateDirection(
            -GetBaseDirection(inputSide),
            buildingInstance != null ? buildingInstance.RotationQuarterTurns : 0);
        launchStartTile = outputTile - outputDirection;
        return tileManager != null && tileManager.IsInBounds(launchStartTile);
    }

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

    public int CurrentResourceAmount => slotAAmount + slotBAmount;
    public int MaxResourceAmount => maxPerSlot * 2;
    public float NormalizedResourceAmount => MaxResourceAmount > 0
        ? Mathf.Clamp01((float)CurrentResourceAmount / MaxResourceAmount)
        : 0f;
    public Color ResourceTint => hasFirstInputTint ? firstInputTint : Color.white;

    private bool HasPendingOrLaunchingOutput => hasItem || launchingItem != null;

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

    public MoveState CaptureMoveState()
    {
        // Consolidate an in-flight launched item back into pending output before relocation.
        if (launchingItem != null)
        {
            pendingOutputDefinition = launchingItem.ItemDefinition;
            pendingOutputQuantity = Mathf.Max(1, launchingItem.Quantity);
            hasItem = pendingOutputDefinition != null && pendingOutputQuantity > 0;

            launchingItem.ClearReservedDestination(this);
            launchingItem.ReleaseClaim(this);
            Destroy(launchingItem.gameObject);
            launchingItem = null;
            launchMoveTimer = 0f;
        }

        return new MoveState
        {
            SlotADefinition = slotADefinition,
            SlotBDefinition = slotBDefinition,
            SlotAAmount = slotAAmount,
            SlotBAmount = slotBAmount,
            HasItem = hasItem,
            PendingOutputDefinition = pendingOutputDefinition,
            PendingOutputQuantity = pendingOutputQuantity,
            FirstInputTint = firstInputTint,
            HasFirstInputTint = hasFirstInputTint
        };
    }

    public void ApplyMoveState(MoveState state)
    {
        if (state == null)
        {
            return;
        }

        slotADefinition = state.SlotADefinition;
        slotBDefinition = state.SlotBDefinition;
        slotAAmount = Mathf.Max(0, state.SlotAAmount);
        slotBAmount = Mathf.Max(0, state.SlotBAmount);
        hasItem = state.HasItem;
        pendingOutputDefinition = state.PendingOutputDefinition;
        pendingOutputQuantity = Mathf.Max(0, state.PendingOutputQuantity);
        firstInputTint = state.FirstInputTint;
        hasFirstInputTint = state.HasFirstInputTint;

        if (launchingItem != null)
        {
            launchingItem.ClearReservedDestination(this);
            launchingItem.ReleaseClaim(this);
            Destroy(launchingItem.gameObject);
            launchingItem = null;
        }

        launchMoveTimer = 0f;
    }
}
