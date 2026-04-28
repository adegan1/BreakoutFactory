using UnityEngine;

[CreateAssetMenu(fileName = "New Item Definition", menuName = "Factory/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    private const string UnknownItemId = "item.unknown";

    // Identity
    [SerializeField] private string itemId = UnknownItemId;
    [SerializeField] private string displayName;
    [SerializeField, TextArea(2, 5)] private string description;

    // Visual
    [SerializeField] private Sprite icon;
    [SerializeField] private Color tint = Color.white;

    // Balance
    [SerializeField, Min(1)] private int maxStackSize = 100;
    [SerializeField, Min(0)] private int baseValue;

    public string ItemId => itemId;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public Color Tint => tint;
    public int MaxStackSize => maxStackSize;
    public int BaseValue => baseValue;

    private string BuildDefaultItemId()
    {
        string sourceName = string.IsNullOrWhiteSpace(name) ? UnknownItemId : name;
        return sourceName.ToLowerInvariant().Replace(" ", ".");
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            itemId = BuildDefaultItemId();
        }

        itemId = itemId.Trim();
        maxStackSize = Mathf.Max(1, maxStackSize);
        baseValue = Mathf.Max(0, baseValue);
    }
}
