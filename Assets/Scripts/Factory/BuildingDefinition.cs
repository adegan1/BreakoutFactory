using UnityEngine;

[CreateAssetMenu(fileName = "New Building Definition", menuName = "Factory/Building Definition")]
public class BuildingDefinition : ScriptableObject
{
    [Header("Display")]
    [SerializeField] private string displayName;
    [SerializeField, TextArea(2, 5)] private string description;
    [SerializeField] private Sprite buildingSprite;
    [SerializeField] private Color buildingColor = Color.white;

    [Header("Placement")]
    [SerializeField, Min(1)] private int footprintWidth = 1;
    [SerializeField, Min(1)] private int footprintHeight = 1;

    [Header("Drops")]
    [SerializeField, Min(0)] private int scrapDropAmount = 0;

    public string DisplayName => displayName;
    public string Description => description;
    public Sprite BuildingSprite => buildingSprite;
    public Color BuildingColor => buildingColor;
    public int FootprintWidth => footprintWidth;
    public int FootprintHeight => footprintHeight;
    public int ScrapDropAmount => scrapDropAmount;

    public Vector2Int FootprintSize => new Vector2Int(footprintWidth, footprintHeight);
}
