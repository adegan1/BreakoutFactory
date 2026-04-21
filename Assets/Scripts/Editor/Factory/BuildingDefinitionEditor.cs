using UnityEditor;

[CustomEditor(typeof(BuildingDefinition))]
public class BuildingDefinitionEditor : BreakoutDataEditorBase
{
    private SerializedProperty displayNameProperty;
    private SerializedProperty descriptionProperty;
    private SerializedProperty buildingSpriteProperty;
    private SerializedProperty buildingColorProperty;
    private SerializedProperty footprintWidthProperty;
    private SerializedProperty footprintHeightProperty;
    private SerializedProperty behaviorPrefabProperty;
    private SerializedProperty scrapDropAmountProperty;

    private void OnEnable()
    {
        displayNameProperty = FindProperty("displayName");
        descriptionProperty = FindProperty("description");
        buildingSpriteProperty = FindProperty("buildingSprite");
        buildingColorProperty = FindProperty("buildingColor");
        footprintWidthProperty = FindProperty("footprintWidth");
        footprintHeightProperty = FindProperty("footprintHeight");
        behaviorPrefabProperty = FindProperty("behaviorPrefab");
        scrapDropAmountProperty = FindProperty("scrapDropAmount");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawSection("Display", displayNameProperty, descriptionProperty, buildingSpriteProperty, buildingColorProperty);
        DrawSection("Placement", footprintWidthProperty, footprintHeightProperty);
        DrawSection("Behavior", behaviorPrefabProperty);
        DrawSection("Drops", scrapDropAmountProperty);

        serializedObject.ApplyModifiedProperties();
    }
}
