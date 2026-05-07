using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class GeneratorBuilding : MonoBehaviour, IMachineResourceProgressProvider, IMachineStoredResourceReceiver
{
    private static readonly Dictionary<string, GeneratorBuilding> activeByMachineStateId = new();

    public enum OutputSide
    {
        Right,
        Up,
        Left,
        Down
    }

    [Header("References")]
    [SerializeField] private BuildingInstance buildingInstance;
    [SerializeField] private TileManager tileManager;
    [SerializeField] private FactoryBuildingPlacer factoryBuildingPlacer;
    [SerializeField] private ItemEntity itemEntityPrefab;
    [SerializeField] private Transform spawnedItemParent;

    [Header("Runtime Parent")]
    [SerializeField] private bool autoAssignSpawnedItemParent = true;
    [SerializeField] private string runtimeItemsParentName = "Runtime Items";

    [Header("Output")]
    [SerializeField] private Vector3 itemSpawnOffset = Vector3.zero;
    [SerializeField, Min(0.01f)] private float outputTravelDurationSeconds = 0.5f;

    [Header("State")]
    [SerializeField] private string machineStateId;

    private float spawnTimer;
    private int spawnedItemCount;
    private ItemEntity launchingItem;
    private float launchMoveTimer;
    private Vector3 launchStartWorldPosition;
    private Vector3 launchTargetWorldPosition;

    public ItemDefinition ItemDefinition => GetGeneratorSettings()?.ItemDefinition;
    public int MaxItemsToSpawn => GetGeneratorSettings()?.MaxItemsToSpawn ?? 0;
    public int SpawnedItemCount => spawnedItemCount;
    public int RemainingItemCount => Mathf.Max(0, MaxItemsToSpawn - spawnedItemCount);
    public OutputSide CurrentOutputSide => GetGeneratorSettings()?.OutputSide ?? OutputSide.Right;
    public int CurrentResourceAmount => RemainingItemCount;
    public int MaxResourceAmount => MaxItemsToSpawn;
    public float NormalizedResourceAmount => MaxItemsToSpawn > 0
        ? Mathf.Clamp01((float)RemainingItemCount / MaxItemsToSpawn)
        : 0f;
    public Color ResourceTint => ItemDefinition != null ? ItemDefinition.Tint : Color.white;
    public string MachineStateId => machineStateId;

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
        RegisterMachineStateIdIfValid();
    }

    private void Update()
    {
        TickLaunchMovement();

        if (launchingItem != null)
        {
            return;
        }

        GeneratorBuildingSettings settings = GetGeneratorSettings();
        if (!CanGenerateItems(settings))
        {
            return;
        }

        spawnTimer += Time.deltaTime;
        float spawnInterval = settings.SpawnIntervalSeconds;
        if (spawnTimer < spawnInterval)
        {
            return;
        }

        if (TrySpawnItem())
        {
            spawnTimer = 0f;
        }
        else
        {
            spawnTimer = spawnInterval;
        }
    }

    public bool TryGetOutputGridPosition(out Vector2Int outputGridPosition)
    {
        outputGridPosition = default;

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

        GeneratorBuildingSettings settings = GetGeneratorSettings();
        if (settings == null)
        {
            return false;
        }

        outputGridPosition = BuildOutputGridPosition(settings, buildingInstance.GridPosition, footprintSize, buildingInstance.RotationQuarterTurns);
        return tileManager.IsInBounds(outputGridPosition);
    }

    public bool TrySpawnItem()
    {
        ResolveDependenciesIfNeeded();

        if (launchingItem != null)
        {
            return false;
        }

        GeneratorBuildingSettings settings = GetGeneratorSettings();
        if (!CanGenerateItems(settings))
        {
            return false;
        }

        Vector2Int outputDirection = GetOutputDirection(settings, buildingInstance.RotationQuarterTurns);
        Vector2Int outputGridPosition = BuildOutputGridPosition(
            settings,
            buildingInstance.GridPosition,
            buildingInstance.FootprintSize,
            buildingInstance.RotationQuarterTurns);

        if (!tileManager.IsInBounds(outputGridPosition))
        {
            return false;
        }

        Vector2Int launchStartTile = outputGridPosition - outputDirection;
        if (!tileManager.IsInBounds(launchStartTile))
        {
            return false;
        }

        if (HasItemOnTile(outputGridPosition) || HasItemOnTile(launchStartTile))
        {
            return false;
        }

        // Don't output if a non-conveyor building occupies the output space
        if (IsOutputBlockedByNonConveyorBuilding(outputGridPosition))
        {
            return false;
        }

        Vector3 spawnPosition = tileManager.GridToWorld(launchStartTile) + itemSpawnOffset;
        Vector3 targetPosition = tileManager.GridToWorld(outputGridPosition) + itemSpawnOffset;

        ItemEntity spawnedItem = Instantiate(itemEntityPrefab, spawnPosition, Quaternion.identity, spawnedItemParent);
        spawnedItem.Initialize(settings.ItemDefinition, settings.QuantityPerSpawn);
        spawnedItem.SetSourceContext(this, buildingInstance != null ? buildingInstance.BuildingDefinition : null, MaxResourceAmount, machineStateId);
        spawnedItem.SetSourceGenerator(this);
        if (!spawnedItem.TryClaim(this))
        {
            Destroy(spawnedItem.gameObject);
            return false;
        }

        BeginLaunch(spawnedItem, spawnPosition, targetPosition);
        spawnedItemCount++;
        return true;
    }

    private bool CanGenerateItems(GeneratorBuildingSettings settings)
    {
        ResolveDependenciesIfNeeded();

        return settings != null
            && settings.ItemDefinition != null
            && itemEntityPrefab != null
            && tileManager != null
            && buildingInstance != null
            && launchingItem == null
            && spawnedItemCount < settings.MaxItemsToSpawn;
    }

    private static Vector2Int GetOutputDirection(GeneratorBuildingSettings settings, int rotationQuarterTurns)
    {
        return FactoryGridDirectionUtility.RotateDirection(
            FactoryGridDirectionUtility.GetBaseDirection(settings.OutputSide),
            rotationQuarterTurns);
    }

    private static Vector2Int BuildOutputGridPosition(
        GeneratorBuildingSettings settings,
        Vector2Int anchor,
        Vector2Int footprintSize,
        int rotationQuarterTurns)
    {
        Vector2Int baseSideOffset = FactoryGridDirectionUtility.GetSideOffset(
            FactoryGridDirectionUtility.GetBaseDirection(settings.OutputSide),
            footprintSize);

        Vector2Int sideOffset = FactoryGridDirectionUtility.RotateOffsetAroundFootprintCenter(
            baseSideOffset,
            footprintSize,
            rotationQuarterTurns);

        return anchor + sideOffset;
    }

    private void BeginLaunch(ItemEntity item, Vector3 startWorldPosition, Vector3 targetWorldPosition)
    {
        Vector2Int reservedOutputTile = tileManager != null
            ? tileManager.WorldToGrid(targetWorldPosition)
            : default;

        if (!item.TryReserveDestination(this, reservedOutputTile))
        {
            Destroy(item.gameObject);
            launchingItem = null;
            launchMoveTimer = 0f;
            return;
        }

        launchingItem = item;
        launchStartWorldPosition = startWorldPosition;
        launchTargetWorldPosition = targetWorldPosition;
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

    private GeneratorBuildingSettings GetGeneratorSettings()
    {
        if (buildingInstance == null)
        {
            return null;
        }

        BuildingDefinition definition = buildingInstance.BuildingDefinition;
        return definition != null ? definition.GeneratorSettings : null;
    }

    private bool HasItemOnTile(Vector2Int gridPosition)
    {
        return ItemEntitySceneQuery.HasItemAtOrReservedTile(tileManager, gridPosition);
    }

    private bool IsOutputBlockedByNonConveyorBuilding(Vector2Int outputGridPosition)
    {
        if (factoryBuildingPlacer == null)
        {
            factoryBuildingPlacer = FindFirstObjectByType<FactoryBuildingPlacer>();
        }

        if (factoryBuildingPlacer == null)
        {
            return false;
        }

        return factoryBuildingPlacer.IsPositionBlockedByNonConveyorBuilding(outputGridPosition);
    }

    public bool TryRefundGeneratedItem(ItemEntity item, int amount = 1)
    {
        if (item == null || item.SourceGenerator != this)
        {
            return false;
        }

        int refundAmount = Mathf.Max(0, amount);
        if (refundAmount <= 0)
        {
            return false;
        }

        spawnedItemCount = Mathf.Max(0, spawnedItemCount - refundAmount);
        return true;
    }

    public static bool TryRefundByMachineStateId(string sourceMachineStateId, int amount)
    {
        if (string.IsNullOrEmpty(sourceMachineStateId)
            || amount <= 0
            || !activeByMachineStateId.TryGetValue(sourceMachineStateId, out GeneratorBuilding generator)
            || generator == null)
        {
            return false;
        }

        generator.spawnedItemCount = Mathf.Max(0, generator.spawnedItemCount - amount);
        return true;
    }

    public void SetMachineStateId(string newMachineStateId)
    {
        if (machineStateId == newMachineStateId)
        {
            return;
        }

        UnregisterMachineStateIdIfValid();
        machineStateId = newMachineStateId;
        RegisterMachineStateIdIfValid();
    }

    public void SetStoredResourceAmount(int resourceAmount)
    {
        GeneratorBuildingSettings settings = GetGeneratorSettings();
        if (settings == null)
        {
            return;
        }

        int clampedRemaining = Mathf.Clamp(resourceAmount, 0, settings.MaxItemsToSpawn);
        spawnedItemCount = Mathf.Clamp(settings.MaxItemsToSpawn - clampedRemaining, 0, settings.MaxItemsToSpawn);
    }

    private void RegisterMachineStateIdIfValid()
    {
        if (string.IsNullOrEmpty(machineStateId))
        {
            return;
        }

        activeByMachineStateId[machineStateId] = this;
    }

    private void UnregisterMachineStateIdIfValid()
    {
        if (string.IsNullOrEmpty(machineStateId))
        {
            return;
        }

        if (activeByMachineStateId.TryGetValue(machineStateId, out GeneratorBuilding existing) && existing == this)
        {
            activeByMachineStateId.Remove(machineStateId);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!TryGetOutputGridPosition(out Vector2Int outputGridPosition) || tileManager == null)
        {
            return;
        }

        Vector3 buildingCenter = transform.position;
        Vector3 outputCenter = tileManager.GridToWorld(outputGridPosition) + itemSpawnOffset;

        Gizmos.color = new Color(0.3f, 1f, 0.8f, 0.9f);
        Gizmos.DrawLine(buildingCenter, outputCenter);
        Gizmos.DrawWireSphere(outputCenter, tileManager.TileSize * 0.2f);
    }

    private void OnDisable()
    {
        UnregisterMachineStateIdIfValid();

        if (launchingItem != null)
        {
            launchingItem.ClearReservedDestination(this);
            launchingItem.ReleaseClaim(this);
            launchingItem = null;
        }

        launchMoveTimer = 0f;
    }
}