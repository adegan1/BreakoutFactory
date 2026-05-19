using UnityEditor;
using UnityEngine;

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
    private SerializedProperty generatorSettingsProperty;
    private SerializedProperty isConveyorProperty;
    private SerializedProperty conveyorStraightSpriteProperty;
    private SerializedProperty conveyorTurnLeftSpriteProperty;
    private SerializedProperty conveyorTurnRightSpriteProperty;
    private SerializedProperty scrapDropAmountProperty;
    private SerializedProperty spriteScaleModeProperty;
    private SerializedProperty customSpriteScaleProperty;
    private SerializedProperty maxOwnedQuantityFromBreakoutDropsProperty;

    private void OnEnable()
    {
        displayNameProperty = FindProperty("displayName");
        descriptionProperty = FindProperty("description");
        buildingSpriteProperty = FindProperty("buildingSprite");
        buildingColorProperty = FindProperty("buildingColor");
        footprintWidthProperty = FindProperty("footprintWidth");
        footprintHeightProperty = FindProperty("footprintHeight");
        behaviorPrefabProperty = FindProperty("behaviorPrefab");
        generatorSettingsProperty = FindProperty("generatorSettings");
        isConveyorProperty = FindProperty("isConveyor");
        conveyorStraightSpriteProperty = FindProperty("conveyorStraightSprite");
        conveyorTurnLeftSpriteProperty = FindProperty("conveyorTurnLeftSprite");
        conveyorTurnRightSpriteProperty = FindProperty("conveyorTurnRightSprite");
        scrapDropAmountProperty = FindProperty("scrapDropAmount");
        spriteScaleModeProperty = FindProperty("spriteScaleMode");
        customSpriteScaleProperty = FindProperty("customSpriteScale");
        maxOwnedQuantityFromBreakoutDropsProperty = FindProperty("maxOwnedQuantityFromBreakoutDrops");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawSection("Display", displayNameProperty, descriptionProperty, buildingSpriteProperty, buildingColorProperty);
        DrawSection("Placement", footprintWidthProperty, footprintHeightProperty);
        DrawSection("Behavior", behaviorPrefabProperty);

        if (ShouldDrawGeneratorSettings())
        {
            DrawSection("Generator", generatorSettingsProperty);
        }

        DrawSection("Conveyor", isConveyorProperty);
        if (isConveyorProperty.boolValue)
        {
            DrawSection("Conveyor Visuals", conveyorStraightSpriteProperty, conveyorTurnLeftSpriteProperty, conveyorTurnRightSpriteProperty);
        }

        DrawSection("Visuals", spriteScaleModeProperty);
        if (spriteScaleModeProperty.enumValueIndex == (int)BuildingDefinition.SpriteScaleMode.Custom)
        {
            EditorGUILayout.PropertyField(customSpriteScaleProperty);
        }

        DrawSection("Drops", scrapDropAmountProperty, maxOwnedQuantityFromBreakoutDropsProperty);

        serializedObject.ApplyModifiedProperties();
    }

    private bool ShouldDrawGeneratorSettings()
    {
        if (generatorSettingsProperty != null && generatorSettingsProperty.objectReferenceValue != null)
        {
            return true;
        }

        GameObject behaviorPrefab = behaviorPrefabProperty != null
            ? behaviorPrefabProperty.objectReferenceValue as GameObject
            : null;

        return behaviorPrefab != null && behaviorPrefab.GetComponent<GeneratorBuilding>() != null;
    }
}
