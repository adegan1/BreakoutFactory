using UnityEngine;

[CreateAssetMenu(fileName = "New Item Definition", menuName = "Factory/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    private const string UnknownItemId = "item.unknown";

    // Identity
    [SerializeField] private string itemId = UnknownItemId;
    [SerializeField] private string displayName;
    [SerializeField, TextArea(2, 5)] private string description;

    [Header("Localization Overrides")]
    [SerializeField] private string japaneseDisplayName;
    [SerializeField, TextArea(2, 5)] private string japaneseDescription;

    // Visual
    [SerializeField] private Sprite icon;
    [SerializeField] private Color tint = Color.white;

    // Balance
    [SerializeField, Min(0)] private int baseValue = 1;

    // Fusion
    [SerializeField] private bool isFusion = false;
    [SerializeField] private bool isCompound = false;

    [System.NonSerialized] private BallTypeData runtimeBallType;

    public string ItemId => itemId;
    public string DisplayName => ResolveDisplayName(displayName, name);
    public string Description => ResolveDescription(description);
    public string LocalizedDisplayName => LocalizationManager.Localize(DisplayName, ResolveOverride(japaneseDisplayName));
    public string LocalizedDescription => LocalizationManager.Localize(Description, ResolveOverride(japaneseDescription));
    public Sprite Icon => icon;
    public Color Tint => tint;
    public int BaseValue => baseValue;
    public bool IsFusion => isFusion;
    public bool IsCompound => isCompound;
    public BallTypeData RuntimeBallType => runtimeBallType;

    public void InitializeAsRuntimeCompound(
        BallTypeData compoundBallType,
        string compoundItemId,
        int compoundBaseValue,
        Sprite iconOverride = null,
        Color? tintOverride = null)
    {
        if (compoundBallType == null)
        {
            return;
        }

        string fallbackId = BuildRuntimeCompoundItemId(compoundBallType.DisplayName);
        itemId = string.IsNullOrWhiteSpace(compoundItemId) ? fallbackId : compoundItemId.Trim();
        displayName = compoundBallType.DisplayName;
        description = compoundBallType.Description;
        icon = iconOverride != null ? iconOverride : compoundBallType.BallSprite;
        tint = tintOverride ?? compoundBallType.TrailColor;
        baseValue = Mathf.Max(0, compoundBaseValue);
        isFusion = false;
        isCompound = true;
        runtimeBallType = compoundBallType;
        name = displayName;
    }

    private static string BuildRuntimeCompoundItemId(string sourceName)
    {
        string safeName = string.IsNullOrWhiteSpace(sourceName)
            ? UnknownItemId
            : sourceName.Trim().ToLowerInvariant().Replace(" ", ".").Replace("+", "plus");

        return $"item.compound.{safeName}";
    }

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
        baseValue = Mathf.Max(0, baseValue);
    }

    private static string ResolveDisplayName(string value, string fallback)
    {
        string trimmed = value != null ? value.Trim() : string.Empty;
        if (!string.IsNullOrEmpty(trimmed) && !IsPlaceholderText(trimmed))
        {
            return trimmed;
        }

        return string.IsNullOrWhiteSpace(fallback) ? string.Empty : fallback.Trim();
    }

    private static string ResolveDescription(string value)
    {
        string trimmed = value != null ? value.Trim() : string.Empty;
        if (string.IsNullOrEmpty(trimmed) || IsPlaceholderText(trimmed))
        {
            return string.Empty;
        }

        return trimmed;
    }

    private static string ResolveOverride(string value)
    {
        string trimmed = value != null ? value.Trim() : string.Empty;
        return IsPlaceholderText(trimmed) ? string.Empty : trimmed;
    }

    private static bool IsPlaceholderText(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        return string.Equals(value, "Item Name", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "Item Description", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "Building Name", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "Building Description", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "Ball Name", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "Ball Description", System.StringComparison.OrdinalIgnoreCase);
    }
}
