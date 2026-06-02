using UnityEngine;

[DisallowMultipleComponent]
public class ConveyorBuilding : MonoBehaviour
{
    private static float sharedAnimationClockSeconds;
    private static int sharedAnimationClockFrame = -1;

    [Header("References")]
    [SerializeField] private BuildingInstance buildingInstance;
    [SerializeField] private TileManager tileManager;

    [Header("Movement")]
    [SerializeField, Min(0.01f)] private float moveDurationSeconds = 0.2f;
    [SerializeField] private Vector3 itemWorldOffset = Vector3.zero;

    [Header("Debug")]
    [SerializeField] private bool drawDirectionGizmo = true;
    [SerializeField] private Color gizmoColor = new Color(0.3f, 1f, 0.8f, 0.9f);

    private ItemEntity carriedItem;
    private bool isMoving;
    private float moveTimer;
    private Vector3 moveStartWorldPosition;
    private Vector3 moveTargetWorldPosition;
    private bool suppressItemSnapOnDisable;

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
        ResolveDependenciesIfNeeded();
        if (buildingInstance == null || tileManager == null)
        {
            return;
        }

        TickSharedAnimationClock();
        ApplyAnimatedConveyorVisual();

        if (carriedItem != null && !carriedItem.IsClaimedBy(this))
        {
            carriedItem.ClearReservedDestination(this);
            carriedItem = null;
            isMoving = false;
            moveTimer = 0f;
        }

        if (carriedItem == null)
        {
            isMoving = false;
            moveTimer = 0f;
            TryAcquireItemOnConveyorTile();
            return;
        }

        if (!isMoving)
        {
            if (!TryGetOutputTile(out Vector2Int outputTile))
            {
                return;
            }

            if (!CanOutputToTile(outputTile, carriedItem))
            {
                return;
            }

            if (HasBlockingItemAt(outputTile, carriedItem))
            {
                return;
            }

            BeginMoveToTile(outputTile);
        }

        TickMove();
    }

    private void TickSharedAnimationClock()
    {
        if (sharedAnimationClockFrame == Time.frameCount)
        {
            return;
        }

        sharedAnimationClockFrame = Time.frameCount;
        sharedAnimationClockSeconds += FactoryBuildingPlacer.FactoryDeltaTime;
    }

    private void ApplyAnimatedConveyorVisual()
    {
        if (buildingInstance == null)
        {
            return;
        }

        BuildingDefinition definition = buildingInstance.BuildingDefinition;
        if (definition == null || !definition.IsConveyor)
        {
            return;
        }

        SpriteRenderer targetRenderer = buildingInstance.TargetSpriteRenderer;
        if (targetRenderer == null)
        {
            return;
        }

        int quarterTurns = Mathf.Abs(buildingInstance.RotationQuarterTurns) % 4;
        int animationFrameIndex = definition.GetConveyorAnimationFrameIndex(sharedAnimationClockSeconds);
        ConveyorVisualResolver.Result conveyorVisual = ConveyorVisualResolver.Resolve(
            definition,
            GetIncomingDirectionForThisConveyor(),
            quarterTurns,
            animationFrameIndex);

        if (conveyorVisual.Sprite != null)
        {
            targetRenderer.sprite = conveyorVisual.Sprite;
        }
    }

    private Vector2Int? GetIncomingDirectionForThisConveyor()
    {
        if (!TryGetConveyorTile(out Vector2Int conveyorTile))
        {
            return null;
        }

        Vector2Int[] candidateIncomingDirections =
        {
            Vector2Int.right,
            Vector2Int.up,
            Vector2Int.left,
            Vector2Int.down
        };

        for (int i = 0; i < candidateIncomingDirections.Length; i++)
        {
            Vector2Int incomingDirection = candidateIncomingDirections[i];
            Vector2Int sourceTile = conveyorTile - incomingDirection;
            if (!tileManager.IsInBounds(sourceTile))
            {
                continue;
            }

            if (!BuildingInstanceSceneQuery.TryGetBuildingAtTile(sourceTile, out BuildingInstance upstream)
                || upstream == null
                || upstream == buildingInstance)
            {
                continue;
            }

            BuildingDefinition upstreamDefinition = upstream.BuildingDefinition;
            if (upstreamDefinition == null || !upstreamDefinition.IsConveyor)
            {
                continue;
            }

            int upstreamQuarterTurns = Mathf.Abs(upstream.RotationQuarterTurns) % 4;
            Vector2Int upstreamOutputDirection = ConveyorVisualResolver.DirectionFromQuarterTurns(upstreamQuarterTurns);
            if (upstreamOutputDirection == incomingDirection)
            {
                return incomingDirection;
            }
        }

        return null;
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

    private void TryAcquireItemOnConveyorTile()
    {
        if (!TryGetConveyorTile(out Vector2Int conveyorTile))
        {
            return;
        }

        ItemEntity[] itemsInScene = ItemEntitySceneQuery.GetItems();
        for (int i = 0; i < itemsInScene.Length; i++)
        {
            ItemEntity item = itemsInScene[i];
            if (item == null)
            {
                continue;
            }

            if (item.IsClaimed && !item.IsClaimedBy(this))
            {
                continue;
            }

            Vector2Int itemTile = tileManager.WorldToGrid(item.transform.position);
            if (itemTile != conveyorTile)
            {
                continue;
            }

            if (!item.TryClaim(this))
            {
                continue;
            }

            carriedItem = item;
            isMoving = false;
            moveTimer = 0f;
            return;
        }
    }

    private bool TryGetConveyorTile(out Vector2Int conveyorTile)
    {
        conveyorTile = default;

        if (buildingInstance == null || tileManager == null)
        {
            return false;
        }

        conveyorTile = buildingInstance.GridPosition;
        return tileManager.IsInBounds(conveyorTile);
    }

    private bool TryGetOutputTile(out Vector2Int outputTile)
    {
        outputTile = default;

        if (!TryGetConveyorTile(out Vector2Int conveyorTile))
        {
            return false;
        }

        Vector2Int outputDirection = GetFacingDirection();
        outputTile = conveyorTile + outputDirection;
        return tileManager.IsInBounds(outputTile);
    }

    private Vector2Int GetFacingDirection()
    {
        if (buildingInstance != null)
        {
            int quarterTurns = Mathf.Abs(buildingInstance.RotationQuarterTurns) % 4;
            return ConveyorVisualResolver.DirectionFromQuarterTurns(quarterTurns);
        }

        Vector2 worldRight = transform.right;
        if (Mathf.Abs(worldRight.x) >= Mathf.Abs(worldRight.y))
        {
            return worldRight.x >= 0f ? Vector2Int.right : Vector2Int.left;
        }

        return worldRight.y >= 0f ? Vector2Int.up : Vector2Int.down;
    }

    private bool HasBlockingItemAt(Vector2Int tile, ItemEntity ignoreItem)
    {
        return ItemEntitySceneQuery.HasItemAtOrReservedTile(tileManager, tile, ignoreItem);
    }

    private bool CanOutputToTile(Vector2Int tile, ItemEntity item)
    {
        if (!BuildingInstanceSceneQuery.TryGetBuildingAtTile(tile, out BuildingInstance destinationBuilding)
            || destinationBuilding == null)
        {
            return true;
        }

        BuildingDefinition destinationDefinition = destinationBuilding.BuildingDefinition;
        if (destinationDefinition != null && destinationDefinition.IsConveyor)
        {
            return true;
        }

        IItemInputReceiver inputReceiver = destinationBuilding.GetComponent<IItemInputReceiver>();
        if (inputReceiver == null)
        {
            return false;
        }

        // Validate the incoming direction matches the building's input side
        Vector2Int conveyorFacingDirection = GetFacingDirection();
        int requiredInputQuarterTurns = inputReceiver.GetRequiredInputDirectionQuarterTurns();
        
        if (requiredInputQuarterTurns >= 0)
        {
            Vector2Int requiredInputDirection = FactoryGridDirectionUtility.DirectionFromQuarterTurns(requiredInputQuarterTurns);
            // The item comes from the opposite direction of the conveyor's movement
            Vector2Int incomingDirection = -conveyorFacingDirection;
            
            if (incomingDirection != requiredInputDirection)
            {
                return false;
            }
        }

        return inputReceiver.CanAcceptItemAtTile(tile, item);
    }

    private bool TryDeliverToInputReceiver(Vector2Int tile)
    {
        if (carriedItem == null)
        {
            return false;
        }

        if (!BuildingInstanceSceneQuery.TryGetBuildingAtTile(tile, out BuildingInstance destinationBuilding)
            || destinationBuilding == null)
        {
            return false;
        }

        BuildingDefinition destinationDefinition = destinationBuilding.BuildingDefinition;
        if (destinationDefinition != null && destinationDefinition.IsConveyor)
        {
            return false;
        }

        IItemInputReceiver inputReceiver = destinationBuilding.GetComponent<IItemInputReceiver>();
        if (inputReceiver == null)
        {
            return false;
        }

        // Validate the incoming direction matches the building's input side
        Vector2Int conveyorFacingDirection = GetFacingDirection();
        int requiredInputQuarterTurns = inputReceiver.GetRequiredInputDirectionQuarterTurns();
        
        if (requiredInputQuarterTurns >= 0)
        {
            Vector2Int requiredInputDirection = FactoryGridDirectionUtility.DirectionFromQuarterTurns(requiredInputQuarterTurns);
            // The item comes from the opposite direction of the conveyor's movement
            Vector2Int incomingDirection = -conveyorFacingDirection;
            
            if (incomingDirection != requiredInputDirection)
            {
                return false;
            }
        }

        return inputReceiver.TryAcceptItem(carriedItem, tile);
    }

    private void BeginMoveToTile(Vector2Int outputTile)
    {
        if (carriedItem == null)
        {
            return;
        }

        if (!carriedItem.TryReserveDestination(this, outputTile))
        {
            return;
        }

        moveStartWorldPosition = carriedItem.transform.position;
        moveTargetWorldPosition = tileManager.GridToWorld(outputTile) + itemWorldOffset;
        moveTimer = 0f;
        isMoving = true;
    }

    private void TickMove()
    {
        if (!isMoving || carriedItem == null)
        {
            return;
        }

        moveTimer += FactoryBuildingPlacer.FactoryDeltaTime;
        float t = Mathf.Clamp01(moveTimer / moveDurationSeconds);
        carriedItem.transform.position = Vector3.Lerp(moveStartWorldPosition, moveTargetWorldPosition, t);

        if (t < 1f)
        {
            return;
        }

        carriedItem.transform.position = moveTargetWorldPosition;

        Vector2Int destinationTile = tileManager.WorldToGrid(moveTargetWorldPosition);
        if (TryDeliverToInputReceiver(destinationTile))
        {
            carriedItem = null;
            isMoving = false;
            moveTimer = 0f;
            return;
        }

        carriedItem.ClearReservedDestination(this);
        carriedItem.ReleaseClaim(this);
        carriedItem = null;
        isMoving = false;
        moveTimer = 0f;
    }

    /// <summary>
    /// Call before destroying this conveyor when it is being replaced by a newly placed belt.
    /// Prevents the in-transit item from snapping to the nearest tile center, since a new
    /// conveyor will take over on the same tile immediately.
    /// </summary>
    public void SuppressItemSnapOnDisable()
    {
        suppressItemSnapOnDisable = true;
    }

    private void OnDisable()
    {
        if (carriedItem != null)
        {
            if (!suppressItemSnapOnDisable && tileManager != null)
            {
                Vector2Int nearestTile = tileManager.WorldToGrid(carriedItem.transform.position);
                carriedItem.transform.position = tileManager.GridToWorld(nearestTile);
            }

            carriedItem.ClearReservedDestination(this);
            carriedItem.ReleaseClaim(this);
            carriedItem = null;
        }

        isMoving = false;
        moveTimer = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDirectionGizmo)
        {
            return;
        }

        ResolveDependenciesIfNeeded();
        if (buildingInstance == null || tileManager == null)
        {
            return;
        }

        Vector2Int startTile = buildingInstance.GridPosition;
        if (!tileManager.IsInBounds(startTile))
        {
            return;
        }

        Vector2Int direction = GetFacingDirection();
        Vector2Int endTile = startTile + direction;

        Vector3 start = tileManager.GridToWorld(startTile);
        Vector3 end = tileManager.IsInBounds(endTile)
            ? tileManager.GridToWorld(endTile)
            : start + new Vector3(direction.x, direction.y, 0f) * tileManager.TileSize;

        float z = tileManager.GridPlaneZ + 0.05f;
        start.z = z;
        end.z = z;

        Gizmos.color = gizmoColor;
        Gizmos.DrawLine(start, end);

        Vector3 arrowDirection = (end - start).normalized;
        float headLength = tileManager.TileSize * 0.25f;
        Vector3 headBase = end - arrowDirection * headLength;
        Vector3 side = Vector3.Cross(arrowDirection, Vector3.forward) * headLength * 0.5f;

        Gizmos.DrawLine(end, headBase + side);
        Gizmos.DrawLine(end, headBase - side);
    }
}
