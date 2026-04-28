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

    private float spawnTimer;
    private int spawnedItemCount;

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

        Vector2Int worldDirection = RotateDirection(GetBaseDirection(settings.OutputSide), buildingInstance.RotationQuarterTurns);
        Vector2Int anchor = buildingInstance.GridPosition;
        Vector2Int sideOffset = GetSideOffset(worldDirection, footprintSize);
        outputGridPosition = anchor + sideOffset;
        return tileManager.IsInBounds(outputGridPosition);
    }

    public bool TrySpawnItem()
    {
        ResolveDependenciesIfNeeded();

        GeneratorBuildingSettings settings = GetGeneratorSettings();
        if (!CanGenerateItems(settings))
        {
            return false;
        }

        if (!TryGetOutputGridPosition(out Vector2Int outputGridPosition))
        {
            return false;
        }

        if (HasItemOnTile(outputGridPosition))
        {
            return false;
        }

        Vector3 spawnPosition = tileManager.GridToWorld(outputGridPosition) + itemSpawnOffset;
        Transform parent = spawnedItemParent != null ? spawnedItemParent : null;
        ItemEntity spawnedItem = Instantiate(itemEntityPrefab, spawnPosition, Quaternion.identity, parent);
        spawnedItem.Initialize(settings.ItemDefinition, settings.QuantityPerSpawn);
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
            && spawnedItemCount < settings.MaxItemsToSpawn;
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
        ItemEntity[] itemsInScene = FindObjectsByType<ItemEntity>(FindObjectsSortMode.None);
        for (int i = 0; i < itemsInScene.Length; i++)
        {
            ItemEntity item = itemsInScene[i];
            if (item == null)
            {
                continue;
            }

            Vector2Int itemGridPosition = tileManager.WorldToGrid(item.transform.position);
            if (itemGridPosition == gridPosition)
            {
                return true;
            }
        }

        return false;
    }

    private static Vector2Int GetBaseDirection(OutputSide side)
    {
        switch (side)
        {
            case OutputSide.Up:
                return Vector2Int.up;
            case OutputSide.Left:
                return Vector2Int.left;
            case OutputSide.Down:
                return Vector2Int.down;
            default:
                return Vector2Int.right;
        }
    }

    private static Vector2Int RotateDirection(Vector2Int direction, int quarterTurns)
    {
        Vector2Int rotated = direction;
        int normalizedQuarterTurns = Mathf.Abs(quarterTurns) % 4;

        for (int i = 0; i < normalizedQuarterTurns; i++)
        {
            rotated = new Vector2Int(-rotated.y, rotated.x);
        }

        return rotated;
    }

    private static Vector2Int GetSideOffset(Vector2Int direction, Vector2Int footprintSize)
    {
        if (direction == Vector2Int.right)
        {
            return new Vector2Int(footprintSize.x, (footprintSize.y - 1) / 2);
        }

        if (direction == Vector2Int.left)
        {
            return new Vector2Int(-1, (footprintSize.y - 1) / 2);
        }

        if (direction == Vector2Int.up)
        {
            return new Vector2Int((footprintSize.x - 1) / 2, footprintSize.y);
        }

        return new Vector2Int((footprintSize.x - 1) / 2, -1);
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
}