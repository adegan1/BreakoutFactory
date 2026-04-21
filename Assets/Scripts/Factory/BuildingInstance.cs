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

    public BuildingDefinition BuildingDefinition => buildingDefinition;
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

    public void Initialize(BuildingDefinition definition)
    {
        buildingDefinition = definition;
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

        // Scale sprite to match occupied footprint in world space.
        // For 90/270 degree turns, occupied width/height are swapped relative to local sprite axes.
        if (footprintSize.x > 0 && footprintSize.y > 0)
        {
            Vector2Int scaleFootprint = (rotationQuarterTurns & 1) == 0
                ? footprintSize
                : new Vector2Int(footprintSize.y, footprintSize.x);

            targetSpriteRenderer.transform.localScale = new Vector3(scaleFootprint.x, scaleFootprint.y, 1f);
        }
    }
}
