using UnityEngine;

[DisallowMultipleComponent]
public class GeneratorBuilding : MonoBehaviour
{
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
    [SerializeField] private ItemEntity itemEntityPrefab;
    [SerializeField] private Transform spawnedItemParent;

    [Header("Runtime Parent")]
    [SerializeField] private bool autoAssignSpawnedItemParent = true;
    [SerializeField] private string runtimeItemsParentName = "Runtime Items";

    [Header("Output")]
    [SerializeField] private Vector3 itemSpawnOffset = Vector3.zero;
    [SerializeField, Min(0.01f)] private float outputTravelDurationSeconds = 0.5f;

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

        Vector2Int worldDirection = FactoryGridDirectionUtility.RotateDirection(
            FactoryGridDirectionUtility.GetBaseDirection(settings.OutputSide),
            buildingInstance.RotationQuarterTurns);
        Vector2Int anchor = buildingInstance.GridPosition;
        Vector2Int sideOffset = FactoryGridDirectionUtility.GetSideOffset(worldDirection, footprintSize);
        outputGridPosition = anchor + sideOffset;
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

        if (!TryGetOutputData(out Vector2Int outputGridPosition, out Vector2Int outputDirection))
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

        Vector3 spawnPosition = tileManager.GridToWorld(launchStartTile) + itemSpawnOffset;
        Vector3 targetPosition = tileManager.GridToWorld(outputGridPosition) + itemSpawnOffset;

        ItemEntity spawnedItem = Instantiate(itemEntityPrefab, spawnPosition, Quaternion.identity, spawnedItemParent);
        spawnedItem.Initialize(settings.ItemDefinition, settings.QuantityPerSpawn);
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

    private bool TryGetOutputData(out Vector2Int outputGridPosition, out Vector2Int outputDirection)
    {
        outputGridPosition = default;
        outputDirection = default;

        if (!TryGetOutputGridPosition(out outputGridPosition))
        {
            return false;
        }

        GeneratorBuildingSettings settings = GetGeneratorSettings();
        if (settings == null)
        {
            return false;
        }

        outputDirection = FactoryGridDirectionUtility.RotateDirection(
            FactoryGridDirectionUtility.GetBaseDirection(settings.OutputSide),
            buildingInstance.RotationQuarterTurns);
        return true;
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
        if (launchingItem != null)
        {
            launchingItem.ClearReservedDestination(this);
            launchingItem.ReleaseClaim(this);
            launchingItem = null;
        }

        launchMoveTimer = 0f;
    }
}