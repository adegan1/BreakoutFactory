using UnityEditor;
using UnityEngine;

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
    private SerializedProperty damageToPlayerProperty;
    private SerializedProperty typeProperty;

    private SerializedProperty windFireBurstRadiusProperty;
    private SerializedProperty windFireBurstDamageProperty;
    private SerializedProperty windFireBurstColorProperty;
    private SerializedProperty windFireBurstWidthProperty;
    private SerializedProperty windFireBurstRayLengthProperty;
    private SerializedProperty windFireBurstLifetimeProperty;
    private SerializedProperty windFireBurstRayCountProperty;

    private SerializedProperty steamWeakenRadiusProperty;
    private SerializedProperty steamWeakenDurationProperty;
    private SerializedProperty steamColorProperty;
    private SerializedProperty steamWidthProperty;
    private SerializedProperty steamRingRadiusProperty;
    private SerializedProperty steamLifetimeProperty;

    private SerializedProperty lifeRootSearchRadiusProperty;
    private SerializedProperty lifeRootDurationProperty;
    private SerializedProperty lifeRootSpeedMultiplierProperty;
    private SerializedProperty vineColorProperty;
    private SerializedProperty vineWidthProperty;
    private SerializedProperty vineGrowDurationProperty;
    private SerializedProperty vineHoldDurationProperty;
    private SerializedProperty vineFadeDurationProperty;

    private void OnEnable()
    {
        hitPointsProperty = FindProperty("hitPoints");
        displayColorProperty = FindProperty("displayColor");
        scoreValueProperty = FindProperty("scoreValue");
        flammableProperty = FindProperty("flammable");
        fireResistantProperty = FindProperty("fireResistant");
        amplifiesLightningProperty = FindProperty("amplifiesLightning");
        lightningTargetBonusProperty = FindProperty("lightningTargetBonus");
        damageToPlayerProperty = FindProperty("damageToPlayer");
        typeProperty = FindProperty("type");

        windFireBurstRadiusProperty = FindProperty("windFireBurstRadius");
        windFireBurstDamageProperty = FindProperty("windFireBurstDamage");
        windFireBurstColorProperty = FindProperty("windFireBurstColor");
        windFireBurstWidthProperty = FindProperty("windFireBurstWidth");
        windFireBurstRayLengthProperty = FindProperty("windFireBurstRayLength");
        windFireBurstLifetimeProperty = FindProperty("windFireBurstLifetime");
        windFireBurstRayCountProperty = FindProperty("windFireBurstRayCount");

        steamWeakenRadiusProperty = FindProperty("steamWeakenRadius");
        steamWeakenDurationProperty = FindProperty("steamWeakenDuration");
        steamColorProperty = FindProperty("steamColor");
        steamWidthProperty = FindProperty("steamWidth");
        steamRingRadiusProperty = FindProperty("steamRingRadius");
        steamLifetimeProperty = FindProperty("steamLifetime");

        lifeRootSearchRadiusProperty = FindProperty("lifeRootSearchRadius");
        lifeRootDurationProperty = FindProperty("lifeRootDuration");
        lifeRootSpeedMultiplierProperty = FindProperty("lifeRootSpeedMultiplier");
        vineColorProperty = FindProperty("vineColor");
        vineWidthProperty = FindProperty("vineWidth");
        vineGrowDurationProperty = FindProperty("vineGrowDuration");
        vineHoldDurationProperty = FindProperty("vineHoldDuration");
        vineFadeDurationProperty = FindProperty("vineFadeDuration");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawSection("Core Properties", hitPointsProperty, displayColorProperty, scoreValueProperty, damageToPlayerProperty);
        DrawSection("Fire Interaction", flammableProperty, fireResistantProperty);
        DrawSection("Lightning Interaction", amplifiesLightningProperty);
        DrawConditionalGroup(amplifiesLightningProperty, lightningTargetBonusProperty);
        DrawSection("Type", typeProperty);

        // BallElement.Fire is index 1
        if (typeProperty.enumValueIndex == 1)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Fire Interaction", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Steam Weaken (triggered when hit by a Water ball)", EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(steamWeakenDurationProperty);
            EditorGUILayout.PropertyField(steamWeakenRadiusProperty);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Steam Visuals", EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(steamColorProperty);
            EditorGUILayout.PropertyField(steamWidthProperty);
            EditorGUILayout.PropertyField(steamRingRadiusProperty);
            EditorGUILayout.PropertyField(steamLifetimeProperty);
            EditorGUI.indentLevel--;
        }

        // BallElement.Life is index 4
        if (typeProperty.enumValueIndex == 4)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Life Interaction", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Vine Root (triggered when hit by a Water ball)", EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(lifeRootDurationProperty);
            EditorGUILayout.PropertyField(lifeRootSpeedMultiplierProperty);
            EditorGUILayout.PropertyField(lifeRootSearchRadiusProperty);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Vine Visuals", EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(vineColorProperty);
            EditorGUILayout.PropertyField(vineWidthProperty);
            EditorGUILayout.PropertyField(vineGrowDurationProperty);
            EditorGUILayout.PropertyField(vineHoldDurationProperty);
            EditorGUILayout.PropertyField(vineFadeDurationProperty);
            EditorGUI.indentLevel--;
        }

        // BallElement.Wind is index 6: Basic=0,Fire=1,Water=2,Lightning=3,Life=4,Earth=5,Wind=6
        if (typeProperty.enumValueIndex == 6)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Wind Interaction", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Fire Burst (triggered when hit by a Fire ball)", EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(windFireBurstDamageProperty);
            EditorGUILayout.PropertyField(windFireBurstRadiusProperty);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Burst Visuals", EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(windFireBurstColorProperty);
            EditorGUILayout.PropertyField(windFireBurstWidthProperty);
            EditorGUILayout.PropertyField(windFireBurstRayLengthProperty);
            EditorGUILayout.PropertyField(windFireBurstLifetimeProperty);
            EditorGUILayout.PropertyField(windFireBurstRayCountProperty);
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }
}