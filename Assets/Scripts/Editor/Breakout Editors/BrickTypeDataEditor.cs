using UnityEditor;

[CustomEditor(typeof(BrickTypeData))]
public class BrickTypeDataEditor : BreakoutDataEditorBase
{
    private SerializedProperty hitPointsProperty;
    private SerializedProperty displayColorProperty;
    private SerializedProperty scoreValueProperty;
    private SerializedProperty flammableProperty;
    private SerializedProperty fireResistantProperty;
    private SerializedProperty amplifiesLightningProperty;
    private SerializedProperty lightningTargetBonusProperty;
    private SerializedProperty typeProperty;

    private void OnEnable()
    {
        hitPointsProperty = FindProperty("hitPoints");
        displayColorProperty = FindProperty("displayColor");
        scoreValueProperty = FindProperty("scoreValue");
        flammableProperty = FindProperty("flammable");
        fireResistantProperty = FindProperty("fireResistant");
        amplifiesLightningProperty = FindProperty("amplifiesLightning");
        lightningTargetBonusProperty = FindProperty("lightningTargetBonus");
        typeProperty = FindProperty("type");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawSection("Core Properties", hitPointsProperty, displayColorProperty, scoreValueProperty);
        DrawSection("Fire Interaction", flammableProperty, fireResistantProperty);
        DrawSection("Lightning Interaction", amplifiesLightningProperty);
        DrawConditionalGroup(amplifiesLightningProperty, lightningTargetBonusProperty);
        DrawSection("Type", typeProperty);

        serializedObject.ApplyModifiedProperties();
    }
}