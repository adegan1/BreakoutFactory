using UnityEngine;

[DisallowMultipleComponent]
public class BuildingInstance : MonoBehaviour
{
    [SerializeField] private BuildingDefinition buildingDefinition;
    [SerializeField] private SpriteRenderer targetSpriteRenderer;
    [SerializeField] private bool applyDefinitionOnAwake = true;

    public BuildingDefinition BuildingDefinition => buildingDefinition;
    public Vector2Int FootprintSize => buildingDefinition != null ? buildingDefinition.FootprintSize : Vector2Int.one;

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

    public void ApplyDefinitionVisuals()
    {
        if (buildingDefinition == null || targetSpriteRenderer == null)
        {
            return;
        }

        targetSpriteRenderer.sprite = buildingDefinition.BuildingSprite;
        targetSpriteRenderer.color = buildingDefinition.BuildingColor;
    }
}
