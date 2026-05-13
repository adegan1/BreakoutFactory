using UnityEditor;

[CustomEditor(typeof(BallTypeData))]
public class BallTypeDataEditor : BreakoutDataEditorBase
{
    private SerializedProperty primarySourceElementProperty;
    private SerializedProperty secondarySourceElementProperty;
    private SerializedProperty primaryEffectProfileProperty;
    private SerializedProperty secondaryEffectProfileProperty;
    private SerializedProperty displayNameProperty;
    private SerializedProperty descriptionProperty;
    private SerializedProperty displayColorProperty;
    private SerializedProperty ballSpriteProperty;
    private SerializedProperty sizeProperty;
    private SerializedProperty movementSpeedProperty;
    private SerializedProperty damageProperty;
    private SerializedProperty bouncesProperty;
    private SerializedProperty passThroughBricksProperty;
    private SerializedProperty passThroughBallsProperty;
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
    private SerializedProperty createsWaterDropsProperty;
    private SerializedProperty waterDropletTypeProperty;
    private SerializedProperty waterDropCooldownProperty;
    private SerializedProperty impactBurstProperty;
    private SerializedProperty impactBurstDamageProperty;
    private SerializedProperty impactBurstRadiusProperty;
    private SerializedProperty elementsProperty;
    private SerializedProperty strongAgainstProperty;

    private void OnEnable()
    {
        primarySourceElementProperty = FindProperty("primarySourceElement");
        secondarySourceElementProperty = FindProperty("secondarySourceElement");
        primaryEffectProfileProperty = FindProperty("primaryEffectProfile");
        secondaryEffectProfileProperty = FindProperty("secondaryEffectProfile");
        displayNameProperty = FindProperty("displayName");
        descriptionProperty = FindProperty("description");
        displayColorProperty = FindProperty("displayColor");
        ballSpriteProperty = FindProperty("ballSprite");
        sizeProperty = FindProperty("size");
        movementSpeedProperty = FindProperty("movementSpeed");
        damageProperty = FindProperty("damage");
        bouncesProperty = FindProperty("bounces");
        passThroughBricksProperty = FindProperty("passThroughBricks");
        passThroughBallsProperty = FindProperty("passThroughBalls");
        appliesBurnProperty = FindProperty("appliesBurn");
        burnDamageProperty = FindProperty("burnDamage");
        burnTickIntervalProperty = FindProperty("burnTickInterval");
        burnHitCountProperty = FindProperty("burnHitCount");
        lightningBurstProperty = FindProperty("lightningBurst");
        lightningBurstTargetCountProperty = FindProperty("lightningBurstTargetCount");
        lightningBurstDamageProperty = FindProperty("lightningBurstDamage");
        lightningBurstRadiusProperty = FindProperty("lightningBurstRadius");
        earthCrackProperty = FindProperty("earthCrack");
        shatterDamageProperty = FindProperty("shatterDamage");
        shatterRadiusProperty = FindProperty("shatterRadius");
        appliesRootProperty = FindProperty("appliesRoot");
        rootDurationProperty = FindProperty("rootDuration");
        rootSpeedMultiplierProperty = FindProperty("rootSpeedMultiplier");
        createsWaterDropsProperty = FindProperty("createsWaterDrops");
        waterDropletTypeProperty = FindProperty("waterDropletType");
        waterDropCooldownProperty = FindProperty("waterDropCooldown");
        impactBurstProperty = FindProperty("impactBurst");
        impactBurstDamageProperty = FindProperty("impactBurstDamage");
        impactBurstRadiusProperty = FindProperty("impactBurstRadius");
        elementsProperty = FindProperty("elements");
        strongAgainstProperty = FindProperty("strongAgainst");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawSection("Recipe", primarySourceElementProperty, secondarySourceElementProperty, primaryEffectProfileProperty, secondaryEffectProfileProperty);
        DrawSection("Display", displayNameProperty, descriptionProperty, displayColorProperty, ballSpriteProperty, sizeProperty);
        DrawSection("Movement", movementSpeedProperty);
        DrawSection("Core Combat", damageProperty, bouncesProperty);
        DrawSection("Brick Interaction", passThroughBricksProperty, passThroughBallsProperty, appliesBurnProperty);
        DrawConditionalGroup(appliesBurnProperty, burnDamageProperty, burnTickIntervalProperty, burnHitCountProperty);
        EditorGUILayout.PropertyField(lightningBurstProperty);
        DrawConditionalGroup(lightningBurstProperty, lightningBurstTargetCountProperty, lightningBurstDamageProperty, lightningBurstRadiusProperty);
        EditorGUILayout.PropertyField(earthCrackProperty);
        DrawConditionalGroup(earthCrackProperty, shatterDamageProperty, shatterRadiusProperty);
        EditorGUILayout.PropertyField(appliesRootProperty);
        DrawConditionalGroup(appliesRootProperty, rootDurationProperty, rootSpeedMultiplierProperty);
        EditorGUILayout.PropertyField(createsWaterDropsProperty);
        DrawConditionalGroup(createsWaterDropsProperty, waterDropletTypeProperty, waterDropCooldownProperty);
        EditorGUILayout.PropertyField(impactBurstProperty);
        DrawConditionalGroup(impactBurstProperty, impactBurstDamageProperty, impactBurstRadiusProperty);

        DrawSection("Elements");
        EditorGUILayout.PropertyField(elementsProperty, includeChildren: true);

        DrawSection("Strong Against");
        EditorGUILayout.PropertyField(strongAgainstProperty, includeChildren: true);

        serializedObject.ApplyModifiedProperties();
    }
}