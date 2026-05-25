using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ItemEntity : MonoBehaviour
{
    [SerializeField] private ItemDefinition itemDefinition;
    [SerializeField] private SpriteRenderer targetSpriteRenderer;
    [SerializeField] private bool applyDefinitionOnAwake = true;
    [SerializeField] private bool renameGameObjectToItemName = true;

    private int quantity = 1;
    private bool hasWarnedMissingRenderer;
    private Object movementOwner;
    private Object reservedDestinationOwner;
    private bool hasReservedDestination;
    private Vector2Int reservedDestinationTile;
    private GeneratorBuilding sourceGenerator;
    private BuildingDefinition sourceBuildingDefinition;
    private int sourceMaxResourceAmount;
    private string sourceMachineStateId;
    // Per-unit-of-original-input source generator IDs. For a basic generator-spawned
    // stack of Quantity=N this contains N copies of the generator's stateId. For a
    // compound/fusion output (Quantity=1) it contains every input's contributing id
    // (flattened from all consumed inputs, including nested composites). Used so that
    // refunding the entity returns every original resource to its source generator.
    private readonly List<string> originSourceIds = new();

    public ItemDefinition ItemDefinition => itemDefinition;
    public int Quantity => quantity;
    public bool IsClaimed => movementOwner != null;
    public GeneratorBuilding SourceGenerator => sourceGenerator;
    public string SourceMachineStateId => sourceMachineStateId;
    public IReadOnlyList<string> OriginSourceIds => originSourceIds;

    private void Reset()
    {
        if (targetSpriteRenderer == null)
        {
            targetSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void Awake()
    {
        ResolveSpriteRendererIfNeeded();

        if (applyDefinitionOnAwake)
        {
            ApplyDefinitionVisuals();
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying && applyDefinitionOnAwake)
        {
            ApplyDefinitionVisuals();
        }
    }

    public void Initialize(ItemDefinition definition, int startingQuantity = 1)
    {
        ResolveSpriteRendererIfNeeded();

        itemDefinition = definition;
        quantity = Mathf.Max(0, startingQuantity);
        ApplyDefinitionVisuals();
    }

    public void SetSourceGenerator(GeneratorBuilding generator)
    {
        sourceGenerator = generator;
    }

    public void SetSourceContext(GeneratorBuilding generator, BuildingDefinition sourceBuilding, int maxResourceAmount, string machineStateId)
    {
        sourceGenerator = generator;
        sourceBuildingDefinition = sourceBuilding;
        sourceMaxResourceAmount = Mathf.Max(0, maxResourceAmount);
        sourceMachineStateId = machineStateId;

        // Initialize per-unit origin ids: one id per unit of quantity.
        originSourceIds.Clear();
        if (!string.IsNullOrEmpty(machineStateId))
        {
            for (int i = 0; i < quantity; i++)
            {
                originSourceIds.Add(machineStateId);
            }
        }
    }

    // Replace origin ids with an explicit list (used by compound/fusion outputs whose
    // contributing source ids span multiple original inputs). Caller passes the full
    // flattened list of generator stateIds that produced this item.
    public void SetOriginSourceIds(IReadOnlyList<string> ids)
    {
        originSourceIds.Clear();
        if (ids == null)
        {
            return;
        }

        for (int i = 0; i < ids.Count; i++)
        {
            originSourceIds.Add(ids[i]);
        }
    }

    public bool TryRebindSourceGenerator(GeneratorBuilding generator, string machineStateId)
    {
        if (generator == null || string.IsNullOrEmpty(machineStateId) || sourceMachineStateId != machineStateId)
        {
            return false;
        }

        sourceGenerator = generator;
        return true;
    }

    public bool TryRefundToSourceGenerator(int amount = 1)
    {
        int refundAmount = Mathf.Max(0, amount);
        if (refundAmount <= 0)
        {
            return false;
        }

        // Composite items (compound/fusion output) carry multiple origin ids per unit.
        // Refund the whole list whenever the caller is taking the entire stack.
        if (originSourceIds.Count > 0 && quantity > 0)
        {
            int idsToRefund = refundAmount >= quantity
                ? originSourceIds.Count
                : Mathf.Min(originSourceIds.Count, refundAmount * (originSourceIds.Count / quantity));
            if (idsToRefund > 0)
            {
                bool refundedAny = false;
                for (int i = 0; i < idsToRefund; i++)
                {
                    string id = originSourceIds[i];
                    if (string.IsNullOrEmpty(id))
                    {
                        continue;
                    }

                    if (GeneratorBuilding.TryRefundByMachineStateId(id, 1))
                    {
                        refundedAny = true;
                    }
                }

                if (refundedAny)
                {
                    originSourceIds.RemoveRange(0, idsToRefund);
                    return true;
                }
            }
        }

        if (sourceGenerator != null && sourceGenerator.TryRefundGeneratedItem(this, refundAmount))
        {
            return true;
        }

        if (GeneratorBuilding.TryRefundByMachineStateId(sourceMachineStateId, refundAmount))
        {
            return true;
        }

        if (!InventoryManager.HasInstance || sourceBuildingDefinition == null)
        {
            return false;
        }

        return InventoryManager.Instance.TryRefundStoredMachineResource(
            sourceBuildingDefinition,
            sourceMachineStateId,
            refundAmount,
            sourceMaxResourceAmount);
    }

    public bool ContainsWorldPoint(Vector3 worldPoint)
    {
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            return collider.OverlapPoint(worldPoint);
        }

        ResolveSpriteRendererIfNeeded();
        if (targetSpriteRenderer == null)
        {
            return false;
        }

        Bounds bounds = targetSpriteRenderer.bounds;
        Vector3 testPoint = new Vector3(worldPoint.x, worldPoint.y, bounds.center.z);
        return bounds.Contains(testPoint);
    }

    public void SetQuantity(int newQuantity)
    {
        quantity = Mathf.Max(0, newQuantity);
    }

    public bool TryClaim(Object owner)
    {
        if (owner == null)
        {
            return false;
        }

        if (movementOwner != null && movementOwner != owner)
        {
            return false;
        }

        movementOwner = owner;
        return true;
    }

    public bool IsClaimedBy(Object owner)
    {
        return movementOwner != null && movementOwner == owner;
    }

    public void ReleaseClaim(Object owner)
    {
        if (owner == null)
        {
            return;
        }

        if (movementOwner == owner)
        {
            movementOwner = null;
            ClearReservedDestination(owner);
        }
    }

    public bool TryReserveDestination(Object owner, Vector2Int tile)
    {
        if (owner == null)
        {
            return false;
        }

        if (reservedDestinationOwner != null && reservedDestinationOwner != owner)
        {
            return false;
        }

        reservedDestinationOwner = owner;
        reservedDestinationTile = tile;
        hasReservedDestination = true;

        // Invalidate the scene-query cache so any same-frame output check
        // by another building will see this reservation immediately.
        ItemEntitySceneQuery.InvalidateCache();

        return true;
    }

    public bool TryGetReservedDestination(out Vector2Int tile)
    {
        tile = reservedDestinationTile;
        return hasReservedDestination;
    }

    public void ClearReservedDestination(Object owner)
    {
        if (owner == null)
        {
            return;
        }

        if (reservedDestinationOwner == owner)
        {
            reservedDestinationOwner = null;
            hasReservedDestination = false;
        }
    }

    public void ApplyDefinitionVisuals()
    {
        ResolveSpriteRendererIfNeeded();

        if (itemDefinition == null)
        {
            if (targetSpriteRenderer != null)
            {
                targetSpriteRenderer.sprite = null;
                targetSpriteRenderer.color = Color.white;
            }

            return;
        }

        if (targetSpriteRenderer == null)
        {
            if (!hasWarnedMissingRenderer)
            {
                hasWarnedMissingRenderer = true;
                Debug.LogWarning($"ItemEntity on '{name}' could not find a SpriteRenderer to apply item visuals.", this);
            }

            return;
        }

        targetSpriteRenderer.sprite = itemDefinition.Icon;
        targetSpriteRenderer.color = itemDefinition.Tint;

        if (renameGameObjectToItemName && !string.IsNullOrWhiteSpace(itemDefinition.DisplayName))
        {
            gameObject.name = itemDefinition.DisplayName + " Item";
        }
    }

    private void ResolveSpriteRendererIfNeeded()
    {
        if (targetSpriteRenderer != null)
        {
            return;
        }

        targetSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }
}