using UnityEngine;

[CreateAssetMenu(fileName = "New Item Definition", menuName = "Factory/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    // Identity
    [SerializeField] private string itemId = "item.unknown";
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

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            itemId = name.ToLowerInvariant().Replace(" ", ".");
        }

        maxStackSize = Mathf.Max(1, maxStackSize);
        baseValue = Mathf.Max(0, baseValue);
    }
}
