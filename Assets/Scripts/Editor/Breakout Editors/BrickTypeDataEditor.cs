using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BrickTypeData))]
public class BrickTypeDataEditor : BreakoutDataEditorBase
{
    private SerializedProperty hitPointsProperty;
    private SerializedProperty brickSpriteProperty;
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

    // Lightning brick interactions
    private SerializedProperty earthLightningStrikeRadiusProperty;
    private SerializedProperty earthLightningStrikeDamageProperty;
    private SerializedProperty earthLightningStrikeBurnDamageProperty;
    private SerializedProperty earthLightningStrikeBurnIntervalProperty;
    private SerializedProperty earthLightningStrikeBurnHitCountProperty;
    private SerializedProperty earthLightningBoltColorProperty;
    private SerializedProperty earthLightningBoltWidthProperty;
    private SerializedProperty earthLightningBoltLifetimeProperty;
    private SerializedProperty earthLightningBoltSegmentsProperty;
    private SerializedProperty earthLightningBoltNoiseProperty;
    private SerializedProperty windChainLightningDurationProperty;
    private SerializedProperty windChainLightningDamageProperty;
    private SerializedProperty windChainLightningRadiusProperty;
    private SerializedProperty windChainLightningBoltColorProperty;
    private SerializedProperty windChainLightningBoltWidthProperty;
    private SerializedProperty windChainLightningBoltLifetimeProperty;
    private SerializedProperty windChainLightningBoltSegmentsProperty;
    private SerializedProperty windChainLightningBoltNoiseProperty;

    // Earth brick interactions
    private SerializedProperty lightningCrackFieldDurationProperty;
    private SerializedProperty lightningCrackFieldTickIntervalProperty;
    private SerializedProperty lightningCrackFieldCrackDamageProperty;
    private SerializedProperty lightningCrackFieldCrackRadiusProperty;
    private SerializedProperty lightningCrackFieldSearchRadiusProperty;
    private SerializedProperty lightningCrackFieldTargetCountProperty;
    private SerializedProperty lightningCrackFieldBoltColorProperty;
    private SerializedProperty lightningCrackFieldBoltWidthProperty;
    private SerializedProperty lightningCrackFieldBoltLifetimeProperty;
    private SerializedProperty lightningCrackFieldBoltSegmentsProperty;
    private SerializedProperty lightningCrackFieldBoltNoiseProperty;
    private SerializedProperty earthLifeRootSearchRadiusProperty;
    private SerializedProperty earthLifeRootDurationProperty;
    private SerializedProperty earthLifeRootSpeedMultiplierProperty;

    private void OnEnable()
    {
        hitPointsProperty = FindProperty("hitPoints");
        brickSpriteProperty = FindProperty("brickSprite");
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

        earthLightningStrikeRadiusProperty = FindProperty("earthLightningStrikeRadius");
        earthLightningStrikeDamageProperty = FindProperty("earthLightningStrikeDamage");
        earthLightningStrikeBurnDamageProperty = FindProperty("earthLightningStrikeBurnDamage");
        earthLightningStrikeBurnIntervalProperty = FindProperty("earthLightningStrikeBurnInterval");
        earthLightningStrikeBurnHitCountProperty = FindProperty("earthLightningStrikeBurnHitCount");
        earthLightningBoltColorProperty = FindProperty("earthLightningBoltColor");
        earthLightningBoltWidthProperty = FindProperty("earthLightningBoltWidth");
        earthLightningBoltLifetimeProperty = FindProperty("earthLightningBoltLifetime");
        earthLightningBoltSegmentsProperty = FindProperty("earthLightningBoltSegments");
        earthLightningBoltNoiseProperty = FindProperty("earthLightningBoltNoise");
        windChainLightningDurationProperty = FindProperty("windChainLightningDuration");
        windChainLightningDamageProperty = FindProperty("windChainLightningDamage");
        windChainLightningRadiusProperty = FindProperty("windChainLightningRadius");
        windChainLightningBoltColorProperty = FindProperty("windChainLightningBoltColor");
        windChainLightningBoltWidthProperty = FindProperty("windChainLightningBoltWidth");
        windChainLightningBoltLifetimeProperty = FindProperty("windChainLightningBoltLifetime");
        windChainLightningBoltSegmentsProperty = FindProperty("windChainLightningBoltSegments");
        windChainLightningBoltNoiseProperty = FindProperty("windChainLightningBoltNoise");

        lightningCrackFieldDurationProperty = FindProperty("lightningCrackFieldDuration");
        lightningCrackFieldTickIntervalProperty = FindProperty("lightningCrackFieldTickInterval");
        lightningCrackFieldCrackDamageProperty = FindProperty("lightningCrackFieldCrackDamage");
        lightningCrackFieldCrackRadiusProperty = FindProperty("lightningCrackFieldCrackRadius");
        lightningCrackFieldSearchRadiusProperty = FindProperty("lightningCrackFieldSearchRadius");
        lightningCrackFieldTargetCountProperty = FindProperty("lightningCrackFieldTargetCount");
        lightningCrackFieldBoltColorProperty = FindProperty("lightningCrackFieldBoltColor");
        lightningCrackFieldBoltWidthProperty = FindProperty("lightningCrackFieldBoltWidth");
        lightningCrackFieldBoltLifetimeProperty = FindProperty("lightningCrackFieldBoltLifetime");
        lightningCrackFieldBoltSegmentsProperty = FindProperty("lightningCrackFieldBoltSegments");
        lightningCrackFieldBoltNoiseProperty = FindProperty("lightningCrackFieldBoltNoise");
        earthLifeRootSearchRadiusProperty = FindProperty("earthLifeRootSearchRadius");
        earthLifeRootDurationProperty = FindProperty("earthLifeRootDuration");
        earthLifeRootSpeedMultiplierProperty = FindProperty("earthLifeRootSpeedMultiplier");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawSection("Core Properties", hitPointsProperty, brickSpriteProperty, displayColorProperty, scoreValueProperty, damageToPlayerProperty);
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

        // BallElement.Lightning is index 3
        if (typeProperty.enumValueIndex == 3)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Lightning Interaction", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("Earth Strike (triggered when hit by an Earth ball)", EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(earthLightningStrikeDamageProperty);
            EditorGUILayout.PropertyField(earthLightningStrikeRadiusProperty);
            EditorGUILayout.PropertyField(earthLightningStrikeBurnDamageProperty);
            EditorGUILayout.PropertyField(earthLightningStrikeBurnIntervalProperty);
            EditorGUILayout.PropertyField(earthLightningStrikeBurnHitCountProperty);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Strike Bolt Visuals", EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(earthLightningBoltColorProperty);
            EditorGUILayout.PropertyField(earthLightningBoltWidthProperty);
            EditorGUILayout.PropertyField(earthLightningBoltLifetimeProperty);
            EditorGUILayout.PropertyField(earthLightningBoltSegmentsProperty);
            EditorGUILayout.PropertyField(earthLightningBoltNoiseProperty);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Chain Lightning (triggered when hit by a Wind ball)", EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(windChainLightningDurationProperty);
            EditorGUILayout.PropertyField(windChainLightningDamageProperty);
            EditorGUILayout.PropertyField(windChainLightningRadiusProperty);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Chain Bolt Visuals", EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(windChainLightningBoltColorProperty);
            EditorGUILayout.PropertyField(windChainLightningBoltWidthProperty);
            EditorGUILayout.PropertyField(windChainLightningBoltLifetimeProperty);
            EditorGUILayout.PropertyField(windChainLightningBoltSegmentsProperty);
            EditorGUILayout.PropertyField(windChainLightningBoltNoiseProperty);

            EditorGUI.indentLevel--;
        }

        // BallElement.Earth is index 5
        if (typeProperty.enumValueIndex == 5)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Earth Interaction", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Crack Field (triggered when hit by a Lightning ball)", EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(lightningCrackFieldDurationProperty);
            EditorGUILayout.PropertyField(lightningCrackFieldTickIntervalProperty);
            EditorGUILayout.PropertyField(lightningCrackFieldTargetCountProperty);
            EditorGUILayout.PropertyField(lightningCrackFieldCrackDamageProperty);
            EditorGUILayout.PropertyField(lightningCrackFieldCrackRadiusProperty);
            EditorGUILayout.PropertyField(lightningCrackFieldSearchRadiusProperty);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Crack Field Bolt Visuals", EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(lightningCrackFieldBoltColorProperty);
            EditorGUILayout.PropertyField(lightningCrackFieldBoltWidthProperty);
            EditorGUILayout.PropertyField(lightningCrackFieldBoltLifetimeProperty);
            EditorGUILayout.PropertyField(lightningCrackFieldBoltSegmentsProperty);
            EditorGUILayout.PropertyField(lightningCrackFieldBoltNoiseProperty);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Vine Root (triggered when hit by a Life ball)", EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(earthLifeRootDurationProperty);
            EditorGUILayout.PropertyField(earthLifeRootSpeedMultiplierProperty);
            EditorGUILayout.PropertyField(earthLifeRootSearchRadiusProperty);
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