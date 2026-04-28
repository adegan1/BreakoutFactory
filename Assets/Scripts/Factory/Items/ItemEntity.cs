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

    public ItemDefinition ItemDefinition => itemDefinition;
    public int Quantity => quantity;
    public bool IsClaimed => movementOwner != null;

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