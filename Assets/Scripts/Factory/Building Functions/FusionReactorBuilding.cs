using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fusion Reactor machine.
///
/// Layout (at 0 rotation):
///   - Two input slots on the LEFT side  (top-left and bottom-left tiles of the footprint)
///   - One output slot on the RIGHT side (centre of the right edge)
///
/// When both inputs have received sufficient items matching a recipe in the database the
/// machine produces the output item and ejects it from the output side.
/// </summary>
[DisallowMultipleComponent]
public class FusionReactorBuilding : MonoBehaviour, IItemInputReceiver, IBuildingInputPreview, IBuildingOutputPreview
{
    public enum InputSide
    {
        Right,
        Up,
        Left,
        Down
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

        TryFuse();
    }

    // ── IItemInputReceiver ────────────────────────────────────────────────────

    public bool CanAcceptItemAtTile(Vector2Int tile, ItemEntity item)
    {
        if (item == null || item.ItemDefinition == null)
        {
            return false;
        }

        GetInputTilesWorld(out Vector2Int tileA, out Vector2Int tileB);

        if (tile == tileA)
        {
            return CanAcceptIntoSlot(item.ItemDefinition, ref slotADefinition, slotAAmount, item.Quantity);
        }

        if (tile == tileB)
        {
            return CanAcceptIntoSlot(item.ItemDefinition, ref slotBDefinition, slotBAmount, item.Quantity);
        }

        return false;
    }

    public bool TryAcceptItem(ItemEntity item, Vector2Int tile)
    {
        if (!CanAcceptItemAtTile(tile, item))
        {
            return false;
        }

        GetInputTilesWorld(out Vector2Int tileA, out Vector2Int tileB);

        int amount = Mathf.Max(1, item.Quantity);

        if (tile == tileA)
        {
            AcceptIntoSlot(item.ItemDefinition, amount, ref slotADefinition, ref slotAAmount);
        }
        else if (tile == tileB)
        {
            AcceptIntoSlot(item.ItemDefinition, amount, ref slotBDefinition, ref slotBAmount);
        }
        else
        {
            return false;
        }

        Destroy(item.gameObject);
        return true;
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
        if (recipeDatabase == null || slotADefinition == null || slotBDefinition == null)
        {
            return;
        }

        FusionReactorRecipe recipe = recipeDatabase.FindRecipe(slotADefinition, slotBDefinition);
        if (recipe == null)
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

        // Consume inputs
        slotAAmount -= costForSlotA;
        if (slotAAmount <= 0)
        {
            slotADefinition = null;
            slotAAmount = 0;
        }

        slotBAmount -= costForSlotB;
        if (slotBAmount <= 0)
        {
            slotBDefinition = null;
            slotBAmount = 0;
        }

        TryEjectOutput(recipe);
    }

    private void TryEjectOutput(FusionReactorRecipe recipe)
    {
        if (recipe == null || recipe.Output == null || itemEntityPrefab == null)
        {
            return;
        }

        ResolveDependenciesIfNeeded();
        if (buildingInstance == null || tileManager == null)
        {
            return;
        }

        if (!TryGetOutputGridPosition(out Vector2Int outputTile))
        {
            return;
        }

        Vector2Int outputDirection = FactoryGridDirectionUtility.RotateDirection(
            -GetBaseDirection(inputSide),
            buildingInstance.RotationQuarterTurns);
        Vector2Int launchStartTile = outputTile - outputDirection;

        if (!tileManager.IsInBounds(launchStartTile))
        {
            return;
        }

        if (ItemEntitySceneQuery.HasItemAtOrReservedTile(tileManager, launchStartTile))
        {
            return;
        }

        if (ItemEntitySceneQuery.HasItemAtOrReservedTile(tileManager, outputTile))
        {
            return;
        }

        Vector3 spawnWorldPos = tileManager.GridToWorld(launchStartTile) + itemSpawnOffset;
        Vector3 targetWorldPos = tileManager.GridToWorld(outputTile) + itemSpawnOffset;

        ItemEntity spawnedItem = Instantiate(itemEntityPrefab, spawnWorldPos, Quaternion.identity, spawnedItemParent);
        spawnedItem.Initialize(recipe.Output, recipe.OutputQuantity);

        if (!spawnedItem.TryClaim(this))
        {
            Destroy(spawnedItem.gameObject);
            return;
        }

        BeginLaunch(spawnedItem, spawnWorldPos, targetWorldPos);
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

    /// <summary>Returns the two input grid positions using the building's live state.</summary>
    private void GetInputTilesWorld(out Vector2Int tileA, out Vector2Int tileB)
    {
        ResolveDependenciesIfNeeded();
        Vector2Int topLeft = buildingInstance != null ? buildingInstance.GridPosition : Vector2Int.zero;
        Vector2Int footprintSize = buildingInstance != null ? buildingInstance.FootprintSize : Vector2Int.one;
        int rotation = buildingInstance != null ? buildingInstance.RotationQuarterTurns : 0;
        BuildBothInputTiles(topLeft, footprintSize, rotation, out tileA, out tileB);
    }

    /// <summary>
    /// Calculates both input tile positions given a top-left, footprint size, and rotation.
    /// Slot A = "top" position on the input side; Slot B = "bottom" position.
    /// </summary>
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

    /// <summary>
    /// Returns an offset (relative to top-left) pointing to either the "top" or "bottom" tile
    /// on the input edge of the building's footprint.
    /// For a left-side input on a 2×3 footprint, top slot = (0,2) and bottom slot = (0,0).
    /// </summary>
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

    private bool CanAcceptIntoSlot(
        ItemDefinition incoming,
        ref ItemDefinition slotDefinition,
        int currentAmount,
        int incomingAmount)
    {
        if (incoming == null || incomingAmount <= 0)
        {
            return false;
        }

        if (slotDefinition != null && slotDefinition != incoming)
        {
            return false;
        }

        return currentAmount + incomingAmount <= maxPerSlot;
    }

    private static void AcceptIntoSlot(
        ItemDefinition incoming,
        int amount,
        ref ItemDefinition slotDefinition,
        ref int slotAmount)
    {
        slotDefinition = incoming;
        slotAmount += amount;
    }

    // ── Launch helpers ────────────────────────────────────────────────────────

    private void BeginLaunch(ItemEntity item, Vector3 startWorldPosition, Vector3 targetWorldPosition)
    {
        if (!TryGetOutputGridPosition(out Vector2Int outputTile))
        {
            Destroy(item.gameObject);
            return;
        }

        Vector3 outputWorldPos = tileManager.GridToWorld(outputTile) + itemSpawnOffset;

        if (!item.TryReserveDestination(this, outputTile))
        {
            Destroy(item.gameObject);
            launchingItem = null;
            launchMoveTimer = 0f;
            return;
        }

        launchingItem = item;
        launchStartWorldPosition = startWorldPosition;
        launchTargetWorldPosition = outputWorldPos;
        launchMoveTimer = 0f;
    }

    private void TickLaunchMovement()
    {
        if (launchingItem == null)
        {
            return;
        }

        launchMoveTimer += Time.deltaTime;
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

    // ── Direction helpers ─────────────────────────────────────────────────────

    private static Vector2Int GetBaseDirection(InputSide side)
    {
        return FactoryGridDirectionUtility.DirectionFromQuarterTurns((int)side);
    }
}
