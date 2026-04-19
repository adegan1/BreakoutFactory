using UnityEditor;

[CustomEditor(typeof(BallTypeData))]
public class BallTypeDataEditor : Editor
{
    private SerializedProperty displayColorProperty;
    private SerializedProperty movementSpeedProperty;
    private SerializedProperty collideWithBricksProperty;
    private SerializedProperty appliesBurnProperty;
    private SerializedProperty burnDamageProperty;
    private SerializedProperty burnTickIntervalProperty;
    private SerializedProperty burnHitCountProperty;
    private SerializedProperty lightningBurstProperty;
    private SerializedProperty lightningBurstTargetCountProperty;
    private SerializedProperty lightningBurstDamageProperty;
    private SerializedProperty lightningBurstRadiusProperty;
    private SerializedProperty earthCrackProperty;
    private SerializedProperty shatterDamageProperty;
    private SerializedProperty shatterRadiusProperty;
    private SerializedProperty appliesRootProperty;
    private SerializedProperty rootDurationProperty;
    private SerializedProperty rootSpeedMultiplierProperty;
    private SerializedProperty elementsProperty;
    private SerializedProperty strongAgainstProperty;

    private void OnEnable()
    {
        displayColorProperty = serializedObject.FindProperty("displayColor");
        movementSpeedProperty = serializedObject.FindProperty("movementSpeed");
        collideWithBricksProperty = serializedObject.FindProperty("collideWithBricks");
        appliesBurnProperty = serializedObject.FindProperty("appliesBurn");
        burnDamageProperty = serializedObject.FindProperty("burnDamage");
        burnTickIntervalProperty = serializedObject.FindProperty("burnTickInterval");
        burnHitCountProperty = serializedObject.FindProperty("burnHitCount");
        lightningBurstProperty = serializedObject.FindProperty("lightningBurst");
        lightningBurstTargetCountProperty = serializedObject.FindProperty("lightningBurstTargetCount");
        lightningBurstDamageProperty = serializedObject.FindProperty("lightningBurstDamage");
        lightningBurstRadiusProperty = serializedObject.FindProperty("lightningBurstRadius");
        earthCrackProperty = serializedObject.FindProperty("earthCrack");
        shatterDamageProperty = serializedObject.FindProperty("shatterDamage");
        shatterRadiusProperty = serializedObject.FindProperty("shatterRadius");
        appliesRootProperty = serializedObject.FindProperty("appliesRoot");
        rootDurationProperty = serializedObject.FindProperty("rootDuration");
        rootSpeedMultiplierProperty = serializedObject.FindProperty("rootSpeedMultiplier");
        elementsProperty = serializedObject.FindProperty("elements");
        strongAgainstProperty = serializedObject.FindProperty("strongAgainst");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Visual", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(displayColorProperty);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Movement", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(movementSpeedProperty);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Brick Interaction", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(collideWithBricksProperty);
        EditorGUILayout.PropertyField(appliesBurnProperty);

        if (appliesBurnProperty.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(burnDamageProperty);
            EditorGUILayout.PropertyField(burnTickIntervalProperty);
            EditorGUILayout.PropertyField(burnHitCountProperty);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.PropertyField(lightningBurstProperty);

        if (lightningBurstProperty.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(lightningBurstTargetCountProperty);
            EditorGUILayout.PropertyField(lightningBurstDamageProperty);
            EditorGUILayout.PropertyField(lightningBurstRadiusProperty);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.PropertyField(earthCrackProperty);

        if (earthCrackProperty.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(shatterDamageProperty);
            EditorGUILayout.PropertyField(shatterRadiusProperty);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.PropertyField(appliesRootProperty);

        if (appliesRootProperty.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(rootDurationProperty);
            EditorGUILayout.PropertyField(rootSpeedMultiplierProperty);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Elements", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(elementsProperty, includeChildren: true);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Strong Against", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(strongAgainstProperty, includeChildren: true);

        serializedObject.ApplyModifiedProperties();
    }
}