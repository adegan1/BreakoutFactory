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
    [SerializeField, Min(0)] private int baseValue = 1;

    // Fusion
    [SerializeField] private bool isFusion = false;
    [SerializeField] private bool isCompound = false;

    [System.NonSerialized] private BallTypeData runtimeBallType;

    public string ItemId => itemId;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public Color Tint => tint;
    public int BaseValue => baseValue;
    public bool IsFusion => isFusion;
    public bool IsCompound => isCompound;
    public BallTypeData RuntimeBallType => runtimeBallType;

    public void InitializeAsRuntimeCompound(BallTypeData compoundBallType, string compoundItemId, int compoundBaseValue)
    {
        if (compoundBallType == null)
        {
            return;
        }

        string fallbackId = BuildRuntimeCompoundItemId(compoundBallType.DisplayName);
        itemId = string.IsNullOrWhiteSpace(compoundItemId) ? fallbackId : compoundItemId.Trim();
        displayName = compoundBallType.DisplayName;
        description = compoundBallType.Description;
        icon = compoundBallType.BallSprite;
        tint = compoundBallType.TrailColor;
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
}
