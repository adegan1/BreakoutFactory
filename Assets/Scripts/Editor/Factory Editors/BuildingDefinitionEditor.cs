using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BuildingDefinition))]
public class BuildingDefinitionEditor : BreakoutDataEditorBase
{
    private SerializedProperty displayNameProperty;
    private SerializedProperty descriptionProperty;
    private SerializedProperty japaneseDisplayNameProperty;
    private SerializedProperty japaneseDescriptionProperty;
    private SerializedProperty buildingSpriteProperty;
    private SerializedProperty buildingColorProperty;
    private SerializedProperty footprintWidthProperty;
    private SerializedProperty footprintHeightProperty;
    private SerializedProperty placementSoundSizeProperty;
    private SerializedProperty behaviorPrefabProperty;
    private SerializedProperty generatorSettingsProperty;
    private SerializedProperty isConveyorProperty;
    private SerializedProperty conveyorStraightSpriteProperty;
    private SerializedProperty conveyorTurnLeftSpriteProperty;
    private SerializedProperty conveyorTurnRightSpriteProperty;
    private SerializedProperty conveyorStraightAnimationSpritesProperty;
    private SerializedProperty conveyorTurnLeftAnimationSpritesProperty;
    private SerializedProperty conveyorTurnRightAnimationSpritesProperty;
    private SerializedProperty conveyorAnimationFrameRateProperty;
    private SerializedProperty scrapDropAmountProperty;
    private SerializedProperty spriteScaleModeProperty;
    private SerializedProperty customSpriteScaleProperty;
    private SerializedProperty maxOwnedQuantityFromBreakoutDropsProperty;
    private SerializedProperty minShopBuyAmountProperty;
    private SerializedProperty maxShopBuyAmountProperty;

    private void OnEnable()
    {
        displayNameProperty = FindProperty("displayName");
        descriptionProperty = FindProperty("description");
        japaneseDisplayNameProperty = FindProperty("japaneseDisplayName");
        japaneseDescriptionProperty = FindProperty("japaneseDescription");
        buildingSpriteProperty = FindProperty("buildingSprite");
        buildingColorProperty = FindProperty("buildingColor");
        footprintWidthProperty = FindProperty("footprintWidth");
        footprintHeightProperty = FindProperty("footprintHeight");
        placementSoundSizeProperty = FindProperty("placementSoundSize");
        behaviorPrefabProperty = FindProperty("behaviorPrefab");
        generatorSettingsProperty = FindProperty("generatorSettings");
        isConveyorProperty = FindProperty("isConveyor");
        conveyorStraightSpriteProperty = FindProperty("conveyorStraightSprite");
        conveyorTurnLeftSpriteProperty = FindProperty("conveyorTurnLeftSprite");
        conveyorTurnRightSpriteProperty = FindProperty("conveyorTurnRightSprite");
        conveyorStraightAnimationSpritesProperty = FindProperty("conveyorStraightAnimationSprites");
        conveyorTurnLeftAnimationSpritesProperty = FindProperty("conveyorTurnLeftAnimationSprites");
        conveyorTurnRightAnimationSpritesProperty = FindProperty("conveyorTurnRightAnimationSprites");
        conveyorAnimationFrameRateProperty = FindProperty("conveyorAnimationFrameRate");
        scrapDropAmountProperty = FindProperty("scrapDropAmount");
        spriteScaleModeProperty = FindProperty("spriteScaleMode");
        customSpriteScaleProperty = FindProperty("customSpriteScale");
        maxOwnedQuantityFromBreakoutDropsProperty = FindProperty("maxOwnedQuantityFromBreakoutDrops");
        minShopBuyAmountProperty = FindProperty("minShopBuyAmount");
        maxShopBuyAmountProperty = FindProperty("maxShopBuyAmount");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawSection(
            "Display",
            displayNameProperty,
            descriptionProperty,
            japaneseDisplayNameProperty,
            japaneseDescriptionProperty,
            buildingSpriteProperty,
            buildingColorProperty);
        DrawSection("Placement", footprintWidthProperty, footprintHeightProperty, placementSoundSizeProperty);
        DrawSection("Behavior", behaviorPrefabProperty);

        if (ShouldDrawGeneratorSettings())
        {
            DrawSection("Generator", generatorSettingsProperty);
        }

        DrawSection("Conveyor", isConveyorProperty);
        if (isConveyorProperty.boolValue)
        {
            DrawSection(
                "Conveyor Visuals",
                conveyorStraightSpriteProperty,
                conveyorTurnLeftSpriteProperty,
                conveyorTurnRightSpriteProperty,
                conveyorStraightAnimationSpritesProperty,
                conveyorTurnLeftAnimationSpritesProperty,
                conveyorTurnRightAnimationSpritesProperty,
                conveyorAnimationFrameRateProperty);
        }

        DrawSection("Visuals", spriteScaleModeProperty);
        if (spriteScaleModeProperty.enumValueIndex == (int)BuildingDefinition.SpriteScaleMode.Custom)
        {
            EditorGUILayout.PropertyField(customSpriteScaleProperty);
        }

        DrawSection("Drops", scrapDropAmountProperty, maxOwnedQuantityFromBreakoutDropsProperty, minShopBuyAmountProperty, maxShopBuyAmountProperty);

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
