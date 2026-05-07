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

    public ItemDefinition ItemDefinition => itemDefinition;
    public int Quantity => quantity;
    public bool IsClaimed => movementOwner != null;
    public GeneratorBuilding SourceGenerator => sourceGenerator;

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

    public bool TryRefundToSourceGenerator(int amount = 1)
    {
        return sourceGenerator != null && sourceGenerator.TryRefundGeneratedItem(this, amount);
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