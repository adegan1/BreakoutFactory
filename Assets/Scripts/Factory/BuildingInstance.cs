using UnityEngine;

[DisallowMultipleComponent]
public class BuildingInstance : MonoBehaviour
{
    [SerializeField] private BuildingDefinition buildingDefinition;
    [SerializeField] private SpriteRenderer targetSpriteRenderer;
    [SerializeField] private bool applyDefinitionOnAwake = true;

    private Vector2Int gridPosition;
    private Vector2Int footprintSize;
    private int rotationQuarterTurns;
    private float tileSize = 1f;

    public BuildingDefinition BuildingDefinition => buildingDefinition;
    public SpriteRenderer TargetSpriteRenderer => targetSpriteRenderer;
    public Vector2Int GridPosition => gridPosition;
    public Vector2Int FootprintSize => footprintSize;
    public int RotationQuarterTurns => rotationQuarterTurns;

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

    public void Initialize(BuildingDefinition definition, float tileSizeValue = 1f)
    {
        buildingDefinition = definition;
        tileSize = tileSizeValue;
        ApplyDefinitionVisuals();
    }

    public void SetGridPosition(Vector2Int newGridPosition, Vector2Int newFootprintSize)
    {
        gridPosition = newGridPosition;
        footprintSize = newFootprintSize;
        rotationQuarterTurns = 0;
    }

    public void SetGridPosition(Vector2Int newGridPosition, Vector2Int newFootprintSize, int newRotationQuarterTurns)
    {
        gridPosition = newGridPosition;
        footprintSize = newFootprintSize;
        rotationQuarterTurns = Mathf.Abs(newRotationQuarterTurns) % 4;
    }

    public void ApplyDefinitionVisuals()
    {
        if (buildingDefinition == null || targetSpriteRenderer == null)
        {
            return;
        }

        targetSpriteRenderer.sprite = buildingDefinition.BuildingSprite;
        targetSpriteRenderer.color = buildingDefinition.BuildingColor;

        Vector2 visualScale = buildingDefinition.GetVisualScale(buildingDefinition.FootprintSize, rotationQuarterTurns);
        targetSpriteRenderer.transform.localScale = new Vector3(visualScale.x * tileSize, visualScale.y * tileSize, 1f);
    }
}
