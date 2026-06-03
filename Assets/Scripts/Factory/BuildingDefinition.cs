using UnityEngine;

[CreateAssetMenu(fileName = "New Building Definition", menuName = "Factory/Building Definition")]
public class BuildingDefinition : ScriptableObject
{
    public enum PlacementSoundSize
    {
        Small,
        Medium,
        Large
    }

    public enum SpriteScaleMode
    {
        Footprint,
        Native,
        Custom
    }

    // Display
    [SerializeField] private string displayName;
    [SerializeField, TextArea(2, 5)] private string description;
    [SerializeField] private Sprite buildingSprite;
    [SerializeField] private Color buildingColor = Color.white;

    // Placement
    [SerializeField, Min(1)] private int footprintWidth = 1;
    [SerializeField, Min(1)] private int footprintHeight = 1;
    [SerializeField] private PlacementSoundSize placementSoundSize = PlacementSoundSize.Small;

    // Behavior
    [SerializeField] private GameObject behaviorPrefab;
    [SerializeField] private GeneratorBuildingSettings generatorSettings;

    [SerializeField] private bool isConveyor;

    // Visuals
    [SerializeField] private SpriteScaleMode spriteScaleMode = SpriteScaleMode.Footprint;
    [SerializeField] private Vector2 customSpriteScale = Vector2.one;

    // Conveyor visuals
    [SerializeField] private Sprite conveyorStraightSprite;
    [SerializeField] private Sprite conveyorTurnLeftSprite;
    [SerializeField] private Sprite conveyorTurnRightSprite;
    [SerializeField] private Sprite[] conveyorStraightAnimationSprites;
    [SerializeField] private Sprite[] conveyorTurnLeftAnimationSprites;
    [SerializeField] private Sprite[] conveyorTurnRightAnimationSprites;
    [SerializeField, Min(0.1f)] private float conveyorAnimationFrameRate = 8f;

    // Drops
    [SerializeField, Min(0)] private float scrapDropAmount = 0f;
    [SerializeField, Min(0)] private int maxOwnedQuantityFromBreakoutDrops;
    [SerializeField, Min(1)] private int minShopBuyAmount = 1;
    [SerializeField, Min(1)] private int maxShopBuyAmount = 1;

    public string DisplayName => displayName;
    public string Description => description;
    public Sprite BuildingSprite => buildingSprite;
    public Color BuildingColor => buildingColor;
    public int FootprintWidth => footprintWidth;
    public int FootprintHeight => footprintHeight;
    public PlacementSoundSize SoundSize => placementSoundSize;
    public GameObject BehaviorPrefab => behaviorPrefab;
    public GeneratorBuildingSettings GeneratorSettings => generatorSettings;
    public bool IsConveyor => isConveyor;
    public Sprite ConveyorStraightSprite => conveyorStraightSprite;
    public Sprite ConveyorTurnLeftSprite => conveyorTurnLeftSprite;
    public Sprite ConveyorTurnRightSprite => conveyorTurnRightSprite;
    public Sprite[] ConveyorStraightAnimationSprites => conveyorStraightAnimationSprites;
    public Sprite[] ConveyorTurnLeftAnimationSprites => conveyorTurnLeftAnimationSprites;
    public Sprite[] ConveyorTurnRightAnimationSprites => conveyorTurnRightAnimationSprites;
    public float ConveyorAnimationFrameRate => conveyorAnimationFrameRate;
    public float ScrapDropAmount => scrapDropAmount;
    public int MaxOwnedQuantityFromBreakoutDrops => maxOwnedQuantityFromBreakoutDrops;
    public int MinShopBuyAmount => minShopBuyAmount;
    public int MaxShopBuyAmount => Mathf.Max(maxShopBuyAmount, minShopBuyAmount);
    public SpriteScaleMode VisualScaleMode => spriteScaleMode;
    public Vector2 CustomSpriteScale => customSpriteScale;

    public Vector2Int FootprintSize => new Vector2Int(footprintWidth, footprintHeight);

    public int GetConveyorAnimationFrameIndex(float animationTime)
    {
        float clampedRate = Mathf.Max(0.1f, conveyorAnimationFrameRate);
        return Mathf.FloorToInt(Mathf.Max(0f, animationTime) * clampedRate);
    }

    public Vector2 GetVisualScale(int rotationQuarterTurns = 0)
    {
        return GetVisualScale(FootprintSize, rotationQuarterTurns);
    }

    public Vector2 GetVisualScale(Vector2Int footprintSize, int rotationQuarterTurns = 0)
    {
        switch (spriteScaleMode)
        {
            case SpriteScaleMode.Native:
                if (buildingSprite != null)
                {
                    Vector2 spriteSize = buildingSprite.bounds.size;
                    if (spriteSize.x > 0f && spriteSize.y > 0f)
                    {
                        float uniformScale = Mathf.Min(
                            (float)footprintSize.x / spriteSize.x,
                            (float)footprintSize.y / spriteSize.y);
                        return Vector2.one * uniformScale;
                    }
                }
                return Vector2.one;
            case SpriteScaleMode.Custom:
                if ((rotationQuarterTurns & 1) == 0)
                {
                    return customSpriteScale;
                }

                return new Vector2(customSpriteScale.y, customSpriteScale.x);
            case SpriteScaleMode.Footprint:
            default:
                if ((rotationQuarterTurns & 1) == 0)
                {
                    return new Vector2(footprintSize.x, footprintSize.y);
                }

                return new Vector2(footprintSize.y, footprintSize.x);
        }
    }
}
