using UnityEditor;

[CustomEditor(typeof(BrickTypeData))]
public class BrickTypeDataEditor : Editor
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
        hitPointsProperty = serializedObject.FindProperty("hitPoints");
        displayColorProperty = serializedObject.FindProperty("displayColor");
        scoreValueProperty = serializedObject.FindProperty("scoreValue");
        flammableProperty = serializedObject.FindProperty("flammable");
        fireResistantProperty = serializedObject.FindProperty("fireResistant");
        amplifiesLightningProperty = serializedObject.FindProperty("amplifiesLightning");
        lightningTargetBonusProperty = serializedObject.FindProperty("lightningTargetBonus");
        typeProperty = serializedObject.FindProperty("type");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Core Properties", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(hitPointsProperty);
        EditorGUILayout.PropertyField(displayColorProperty);
        EditorGUILayout.PropertyField(scoreValueProperty);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Fire Interaction", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(flammableProperty);
        EditorGUILayout.PropertyField(fireResistantProperty);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Lightning Interaction", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(amplifiesLightningProperty);
        if (amplifiesLightningProperty.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(lightningTargetBonusProperty);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Type", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(typeProperty);

        serializedObject.ApplyModifiedProperties();
    }
}