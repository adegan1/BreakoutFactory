using UnityEngine;

[DisallowMultipleComponent]
public class ItemEntity : MonoBehaviour
{
    [SerializeField] private ItemDefinition itemDefinition;
    [SerializeField] private SpriteRenderer targetSpriteRenderer;
    [SerializeField] private bool applyDefinitionOnAwake = true;
    [SerializeField] private bool renameGameObjectToItemName = true;

    private int quantity = 1;

    public ItemDefinition ItemDefinition => itemDefinition;
    public int Quantity => quantity;

    private void Reset()
    {
        if (targetSpriteRenderer == null)
        {
            targetSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void Awake()
    {
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
        itemDefinition = definition;
        quantity = Mathf.Max(0, startingQuantity);
        ApplyDefinitionVisuals();
    }

    public void SetQuantity(int newQuantity)
    {
        quantity = Mathf.Max(0, newQuantity);
    }

    public void ApplyDefinitionVisuals()
    {
        if (targetSpriteRenderer == null)
        {
            return;
        }

        if (itemDefinition == null)
        {
            targetSpriteRenderer.sprite = null;
            targetSpriteRenderer.color = Color.white;
            return;
        }

        targetSpriteRenderer.sprite = itemDefinition.Icon;
        targetSpriteRenderer.color = itemDefinition.Tint;

        if (renameGameObjectToItemName && !string.IsNullOrWhiteSpace(itemDefinition.DisplayName))
        {
            gameObject.name = itemDefinition.DisplayName + " Item";
        }
    }
}