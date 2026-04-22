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
    private SerializedProperty isConveyorProperty;
    private SerializedProperty conveyorStraightSpriteProperty;
    private SerializedProperty conveyorTurnLeftSpriteProperty;
    private SerializedProperty conveyorTurnRightSpriteProperty;
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
        isConveyorProperty = FindProperty("isConveyor");
        conveyorStraightSpriteProperty = FindProperty("conveyorStraightSprite");
        conveyorTurnLeftSpriteProperty = FindProperty("conveyorTurnLeftSprite");
        conveyorTurnRightSpriteProperty = FindProperty("conveyorTurnRightSprite");
        scrapDropAmountProperty = FindProperty("scrapDropAmount");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawSection("Display", displayNameProperty, descriptionProperty, buildingSpriteProperty, buildingColorProperty);
        DrawSection("Placement", footprintWidthProperty, footprintHeightProperty);
        DrawSection("Behavior", behaviorPrefabProperty);
        DrawSection("Conveyor", isConveyorProperty);
        if (isConveyorProperty.boolValue)
        {
            DrawSection("Conveyor Visuals", conveyorStraightSpriteProperty, conveyorTurnLeftSpriteProperty, conveyorTurnRightSpriteProperty);
        }
        DrawSection("Drops", scrapDropAmountProperty);

        serializedObject.ApplyModifiedProperties();
    }
}
