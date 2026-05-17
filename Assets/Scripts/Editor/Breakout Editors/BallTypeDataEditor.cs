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
    private SerializedProperty createsLightningSnakeProperty;
    private SerializedProperty lightningSnakeBounceCountProperty;
    private SerializedProperty lightningSnakeDamageProperty;
    private SerializedProperty lightningSnakeRadiusProperty;
    private SerializedProperty lightningSnakeWaterSplitCountProperty;
    private SerializedProperty lightningSnakeBounceDelayProperty;
    private SerializedProperty earthCrackProperty;
    private SerializedProperty shatterDamageProperty;
    private SerializedProperty shatterRadiusProperty;
    private SerializedProperty appliesRootProperty;
    private SerializedProperty rootDurationProperty;
    private SerializedProperty rootSpeedMultiplierProperty;
    private SerializedProperty createsCombustionProperty;
    private SerializedProperty combustionBurnDamageProperty;
    private SerializedProperty combustionBurnTickIntervalProperty;
    private SerializedProperty combustionBurnHitCountProperty;
    private SerializedProperty combustionExplosionDamageProperty;
    private SerializedProperty combustionExplosionRadiusProperty;
    private SerializedProperty createsFireSpreadProperty;
    private SerializedProperty fireSpreadRadiusProperty;
    private SerializedProperty fireSpreadCooldownProperty;
    private SerializedProperty fireSpreadBonusBurnDamageProperty;
    private SerializedProperty fireSpreadBurnSpeedMultiplierProperty;
    private SerializedProperty fireSpreadBurnHitCountBonusProperty;
    private SerializedProperty createsForestFireProperty;
    private SerializedProperty forestFireBurnDamageProperty;
    private SerializedProperty forestFireBurnTickIntervalProperty;
    private SerializedProperty forestFireBurnHitCountProperty;
    private SerializedProperty forestFireSpreadGenerationsProperty;
    private SerializedProperty createsWaterDropsProperty;
    private SerializedProperty waterDropletTypeProperty;
    private SerializedProperty waterDropCooldownProperty;
    private SerializedProperty createsFlameTrailProperty;
    private SerializedProperty flameTrailSpriteProperty;
    private SerializedProperty flameTrailColorProperty;
    private SerializedProperty flameTrailSizeMultiplierProperty;
    private SerializedProperty flameTrailSpawnIntervalProperty;
    private SerializedProperty flameTrailRiseSpeedProperty;
    private SerializedProperty flameTrailLifetimeProperty;
    private SerializedProperty flameTrailImpactDamageProperty;
    private SerializedProperty flameTrailBurnDamageProperty;
    private SerializedProperty flameTrailBurnTickIntervalProperty;
    private SerializedProperty flameTrailBurnHitCountProperty;
    private SerializedProperty timedEffectInitialDelayProperty;
    private SerializedProperty createsSteamBurstProperty;
    private SerializedProperty steamBurstBallTypeProperty;
    private SerializedProperty steamBurstBallCountProperty;
    private SerializedProperty steamBurstMinIntervalProperty;
    private SerializedProperty steamBurstMaxIntervalProperty;
    private SerializedProperty steamBurstSpawnRadiusProperty;
    private SerializedProperty steamBurstSpeedMultiplierProperty;
    private SerializedProperty steamBurstSpeedLerpDurationProperty;
    private SerializedProperty impactBurstProperty;
    private SerializedProperty impactBurstDamageProperty;
    private SerializedProperty impactBurstRadiusProperty;
    private SerializedProperty isCompoundProperty;
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
        createsLightningSnakeProperty = FindProperty("createsLightningSnake");
        lightningSnakeBounceCountProperty = FindProperty("lightningSnakeBounceCount");
        lightningSnakeDamageProperty = FindProperty("lightningSnakeDamage");
        lightningSnakeRadiusProperty = FindProperty("lightningSnakeRadius");
        lightningSnakeWaterSplitCountProperty = FindProperty("lightningSnakeWaterSplitCount");
        lightningSnakeBounceDelayProperty = FindProperty("lightningSnakeBounceDelay");
        earthCrackProperty = FindProperty("earthCrack");
        shatterDamageProperty = FindProperty("shatterDamage");
        shatterRadiusProperty = FindProperty("shatterRadius");
        appliesRootProperty = FindProperty("appliesRoot");
        rootDurationProperty = FindProperty("rootDuration");
        rootSpeedMultiplierProperty = FindProperty("rootSpeedMultiplier");
        createsCombustionProperty = FindProperty("createsCombustion");
        combustionBurnDamageProperty = FindProperty("combustionBurnDamage");
        combustionBurnTickIntervalProperty = FindProperty("combustionBurnTickInterval");
        combustionBurnHitCountProperty = FindProperty("combustionBurnHitCount");
        combustionExplosionDamageProperty = FindProperty("combustionExplosionDamage");
        combustionExplosionRadiusProperty = FindProperty("combustionExplosionRadius");
        createsFireSpreadProperty = FindProperty("createsFireSpread");
        fireSpreadRadiusProperty = FindProperty("fireSpreadRadius");
        fireSpreadCooldownProperty = FindProperty("fireSpreadCooldown");
        fireSpreadBonusBurnDamageProperty = FindProperty("fireSpreadBonusBurnDamage");
        fireSpreadBurnSpeedMultiplierProperty = FindProperty("fireSpreadBurnSpeedMultiplier");
        fireSpreadBurnHitCountBonusProperty = FindProperty("fireSpreadBurnHitCountBonus");
        createsForestFireProperty = FindProperty("createsForestFire");
        forestFireBurnDamageProperty = FindProperty("forestFireBurnDamage");
        forestFireBurnTickIntervalProperty = FindProperty("forestFireBurnTickInterval");
        forestFireBurnHitCountProperty = FindProperty("forestFireBurnHitCount");
        forestFireSpreadGenerationsProperty = FindProperty("forestFireSpreadGenerations");
        createsWaterDropsProperty = FindProperty("createsWaterDrops");
        waterDropletTypeProperty = FindProperty("waterDropletType");
        waterDropCooldownProperty = FindProperty("waterDropCooldown");
        createsFlameTrailProperty = FindProperty("createsFlameTrail");
        flameTrailSpriteProperty = FindProperty("flameTrailSprite");
        flameTrailColorProperty = FindProperty("flameTrailColor");
        flameTrailSizeMultiplierProperty = FindProperty("flameTrailSizeMultiplier");
        flameTrailSpawnIntervalProperty = FindProperty("flameTrailSpawnInterval");
        flameTrailRiseSpeedProperty = FindProperty("flameTrailRiseSpeed");
        flameTrailLifetimeProperty = FindProperty("flameTrailLifetime");
        flameTrailImpactDamageProperty = FindProperty("flameTrailImpactDamage");
        flameTrailBurnDamageProperty = FindProperty("flameTrailBurnDamage");
        flameTrailBurnTickIntervalProperty = FindProperty("flameTrailBurnTickInterval");
        flameTrailBurnHitCountProperty = FindProperty("flameTrailBurnHitCount");
        timedEffectInitialDelayProperty = FindProperty("timedEffectInitialDelay");
        createsSteamBurstProperty = FindProperty("createsSteamBurst");
        steamBurstBallTypeProperty = FindProperty("steamBurstBallType");
        steamBurstBallCountProperty = FindProperty("steamBurstBallCount");
        steamBurstMinIntervalProperty = FindProperty("steamBurstMinInterval");
        steamBurstMaxIntervalProperty = FindProperty("steamBurstMaxInterval");
        steamBurstSpawnRadiusProperty = FindProperty("steamBurstSpawnRadius");
        steamBurstSpeedMultiplierProperty = FindProperty("steamBurstSpeedMultiplier");
        steamBurstSpeedLerpDurationProperty = FindProperty("steamBurstSpeedLerpDuration");
        impactBurstProperty = FindProperty("impactBurst");
        impactBurstDamageProperty = FindProperty("impactBurstDamage");
        impactBurstRadiusProperty = FindProperty("impactBurstRadius");
        isCompoundProperty = FindProperty("isCompound");
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
        EditorGUILayout.PropertyField(createsLightningSnakeProperty);
        DrawConditionalGroup(
            createsLightningSnakeProperty,
            lightningSnakeBounceCountProperty,
            lightningSnakeDamageProperty,
            lightningSnakeRadiusProperty,
            lightningSnakeWaterSplitCountProperty,
            lightningSnakeBounceDelayProperty);
        EditorGUILayout.PropertyField(earthCrackProperty);
        DrawConditionalGroup(earthCrackProperty, shatterDamageProperty, shatterRadiusProperty);
        EditorGUILayout.PropertyField(appliesRootProperty);
        DrawConditionalGroup(appliesRootProperty, rootDurationProperty, rootSpeedMultiplierProperty);
        EditorGUILayout.PropertyField(createsCombustionProperty);
        DrawConditionalGroup(
            createsCombustionProperty,
            combustionBurnDamageProperty,
            combustionBurnTickIntervalProperty,
            combustionBurnHitCountProperty,
            combustionExplosionDamageProperty,
            combustionExplosionRadiusProperty);
        EditorGUILayout.PropertyField(createsFireSpreadProperty);
        DrawConditionalGroup(
            createsFireSpreadProperty,
            fireSpreadRadiusProperty,
            fireSpreadCooldownProperty,
            fireSpreadBonusBurnDamageProperty,
            fireSpreadBurnSpeedMultiplierProperty,
            fireSpreadBurnHitCountBonusProperty);
        EditorGUILayout.PropertyField(createsForestFireProperty);
        DrawConditionalGroup(
            createsForestFireProperty,
            forestFireBurnDamageProperty,
            forestFireBurnTickIntervalProperty,
            forestFireBurnHitCountProperty,
            forestFireSpreadGenerationsProperty);
        EditorGUILayout.PropertyField(createsWaterDropsProperty);
        DrawConditionalGroup(createsWaterDropsProperty, waterDropletTypeProperty, waterDropCooldownProperty);
        EditorGUILayout.PropertyField(createsFlameTrailProperty);
        DrawConditionalGroup(
            createsFlameTrailProperty,
            flameTrailSpriteProperty,
            flameTrailColorProperty,
            flameTrailSizeMultiplierProperty,
            flameTrailSpawnIntervalProperty,
            flameTrailRiseSpeedProperty,
            flameTrailLifetimeProperty,
            flameTrailImpactDamageProperty,
            flameTrailBurnDamageProperty,
            flameTrailBurnTickIntervalProperty,
            flameTrailBurnHitCountProperty);
        EditorGUILayout.PropertyField(timedEffectInitialDelayProperty);
        EditorGUILayout.PropertyField(createsSteamBurstProperty);
        DrawConditionalGroup(
            createsSteamBurstProperty,
            steamBurstBallTypeProperty,
            steamBurstBallCountProperty,
            steamBurstMinIntervalProperty,
            steamBurstMaxIntervalProperty,
            steamBurstSpawnRadiusProperty,
            steamBurstSpeedMultiplierProperty,
            steamBurstSpeedLerpDurationProperty);
        EditorGUILayout.PropertyField(impactBurstProperty);
        DrawConditionalGroup(impactBurstProperty, impactBurstDamageProperty, impactBurstRadiusProperty);

        DrawSection("Compound", isCompoundProperty);

        DrawSection("Elements");
        EditorGUILayout.PropertyField(elementsProperty, includeChildren: true);

        DrawSection("Strong Against");
        EditorGUILayout.PropertyField(strongAgainstProperty, includeChildren: true);

        serializedObject.ApplyModifiedProperties();
    }
}