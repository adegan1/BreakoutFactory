using UnityEditor;
using UnityEngine;

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
    private SerializedProperty directionRestraintProperty;
    private SerializedProperty destroyOnWallProperty;
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
    private SerializedProperty createsTremorProperty;
    private SerializedProperty tremorCrackDamageProperty;
    private SerializedProperty tremorCrackRadiusProperty;
    private SerializedProperty createsAbrasionProperty;
    private SerializedProperty abrasionWeakenDurationProperty;
    private SerializedProperty createsCycloneProperty;
    private SerializedProperty cycloneFollowUpHitCountProperty;
    private SerializedProperty cycloneHitDelayProperty;
    private SerializedProperty cycloneCurveStrengthProperty;
    private SerializedProperty appliesRootProperty;
    private SerializedProperty rootDurationProperty;
    private SerializedProperty rootSpeedMultiplierProperty;
    private SerializedProperty createsSeedProperty;
    private SerializedProperty seedRootDurationProperty;
    private SerializedProperty seedRootSpeedMultiplierProperty;
    private SerializedProperty seedSpreadRadiusProperty;
    private SerializedProperty seedSpreadCountProperty;
    private SerializedProperty seedSpreadGenerationsProperty;
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
    private SerializedProperty createsFertileLandProperty;
    private SerializedProperty fertilePatchSpriteProperty;
    private SerializedProperty fertilePatchColorProperty;
    private SerializedProperty fertilePatchSizeMultiplierProperty;
    private SerializedProperty fertilePatchSpawnIntervalProperty;
    private SerializedProperty fertilePatchRiseSpeedProperty;
    private SerializedProperty fertilePatchLifetimeProperty;
    private SerializedProperty fertilePatchCrackShatterDamageProperty;
    private SerializedProperty fertilePatchCrackShatterRadiusProperty;
    private SerializedProperty fertilePatchRootRadiusProperty;
    private SerializedProperty fertilePatchRootDurationProperty;
    private SerializedProperty fertilePatchRootSpeedMultiplierProperty;
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
    private SerializedProperty createsCollapseProperty;
    private SerializedProperty collapseRadiusProperty;
    private SerializedProperty collapseDurationProperty;
    private SerializedProperty createsLinearProjectileProperty;
    private SerializedProperty linearProjectileTypeProperty;
    private SerializedProperty linearProjectileIncludesTopWallProperty;
    private SerializedProperty createsBlackoutProperty;
    private SerializedProperty blackoutDamageProperty;
    private SerializedProperty blackoutIntervalProperty;
    private SerializedProperty createsFirstAidProperty;
    private SerializedProperty firstAidHealPerHitProperty;
    private SerializedProperty firstAidHealThresholdProperty;
    private SerializedProperty firstAidExplosionDamageProperty;
    private SerializedProperty firstAidExplosionRadiusProperty;
    private SerializedProperty createsElectricCascadeProperty;
    private SerializedProperty electricCascadeShockDamageProperty;
    private SerializedProperty electricCascadeConductiveDurationProperty;
    private SerializedProperty createsRollingThunderProperty;
    private SerializedProperty rollingThunderStartScaleMultiplierProperty;
    private SerializedProperty rollingThunderMaxScaleMultiplierProperty;
    private SerializedProperty rollingThunderGrowthAmountProperty;
    private SerializedProperty rollingThunderSpawnBallTypeProperty;
    private SerializedProperty rollingThunderMinLaunchAngleProperty;
    private SerializedProperty rollingThunderMaxLaunchAngleProperty;
    private SerializedProperty createsShockTherapyProperty;
    private SerializedProperty shockTherapyMinTargetsProperty;
    private SerializedProperty shockTherapyMaxTargetsProperty;
    private SerializedProperty shockTherapyDamageProperty;
    private SerializedProperty shockTherapyHealAmountProperty;
    private SerializedProperty createsPressurizedSplashProperty;
    private SerializedProperty pressurePerHitProperty;
    private SerializedProperty maxPressureProperty;
    private SerializedProperty splashDropletTypeProperty;
    private SerializedProperty splashDropletCountProperty;
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
        directionRestraintProperty = FindProperty("directionRestraint");
        destroyOnWallProperty = FindProperty("destroyOnWall");
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
        createsTremorProperty = FindProperty("createsTremor");
        tremorCrackDamageProperty = FindProperty("tremorCrackDamage");
        tremorCrackRadiusProperty = FindProperty("tremorCrackRadius");
        createsAbrasionProperty = FindProperty("createsAbrasion");
        abrasionWeakenDurationProperty = FindProperty("abrasionWeakenDuration");
        createsCycloneProperty = FindProperty("createsCyclone");
        cycloneFollowUpHitCountProperty = FindProperty("cycloneFollowUpHitCount");
        cycloneHitDelayProperty = FindProperty("cycloneHitDelay");
        cycloneCurveStrengthProperty = FindProperty("cycloneCurveStrength");
        appliesRootProperty = FindProperty("appliesRoot");
        rootDurationProperty = FindProperty("rootDuration");
        rootSpeedMultiplierProperty = FindProperty("rootSpeedMultiplier");
        createsSeedProperty = FindProperty("createsSeed");
        seedRootDurationProperty = FindProperty("seedRootDuration");
        seedRootSpeedMultiplierProperty = FindProperty("seedRootSpeedMultiplier");
        seedSpreadRadiusProperty = FindProperty("seedSpreadRadius");
        seedSpreadCountProperty = FindProperty("seedSpreadCount");
        seedSpreadGenerationsProperty = FindProperty("seedSpreadGenerations");
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
        createsFertileLandProperty = FindProperty("createsFertileLand");
        fertilePatchSpriteProperty = FindProperty("fertilePatchSprite");
        fertilePatchColorProperty = FindProperty("fertilePatchColor");
        fertilePatchSizeMultiplierProperty = FindProperty("fertilePatchSizeMultiplier");
        fertilePatchSpawnIntervalProperty = FindProperty("fertilePatchSpawnInterval");
        fertilePatchRiseSpeedProperty = FindProperty("fertilePatchRiseSpeed");
        fertilePatchLifetimeProperty = FindProperty("fertilePatchLifetime");
        fertilePatchCrackShatterDamageProperty = FindProperty("fertilePatchCrackShatterDamage");
        fertilePatchCrackShatterRadiusProperty = FindProperty("fertilePatchCrackShatterRadius");
        fertilePatchRootRadiusProperty = FindProperty("fertilePatchRootRadius");
        fertilePatchRootDurationProperty = FindProperty("fertilePatchRootDuration");
        fertilePatchRootSpeedMultiplierProperty = FindProperty("fertilePatchRootSpeedMultiplier");
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
        createsCollapseProperty = FindProperty("createsCollapse");
        collapseRadiusProperty = FindProperty("collapseRadius");
        collapseDurationProperty = FindProperty("collapseDuration");
        createsLinearProjectileProperty = FindProperty("createsLinearProjectile");
        linearProjectileTypeProperty = FindProperty("linearProjectileType");
        linearProjectileIncludesTopWallProperty = FindProperty("linearProjectileIncludesTopWall");
        createsBlackoutProperty = FindProperty("createsBlackout");
        blackoutDamageProperty = FindProperty("blackoutDamage");
        blackoutIntervalProperty = FindProperty("blackoutInterval");
        createsFirstAidProperty = FindProperty("createsFirstAid");
        firstAidHealPerHitProperty = FindProperty("firstAidHealPerHit");
        firstAidHealThresholdProperty = FindProperty("firstAidHealThreshold");
        firstAidExplosionDamageProperty = FindProperty("firstAidExplosionDamage");
        firstAidExplosionRadiusProperty = FindProperty("firstAidExplosionRadius");
        createsElectricCascadeProperty = FindProperty("createsElectricCascade");
        electricCascadeShockDamageProperty = FindProperty("electricCascadeShockDamage");
        electricCascadeConductiveDurationProperty = FindProperty("electricCascadeConductiveDuration");
        createsRollingThunderProperty = FindProperty("createsRollingThunder");
        rollingThunderStartScaleMultiplierProperty = FindProperty("rollingThunderStartScaleMultiplier");
        rollingThunderMaxScaleMultiplierProperty = FindProperty("rollingThunderMaxScaleMultiplier");
        rollingThunderGrowthAmountProperty = FindProperty("rollingThunderGrowthAmount");
        rollingThunderSpawnBallTypeProperty = FindProperty("rollingThunderSpawnBallType");
        rollingThunderMinLaunchAngleProperty = FindProperty("rollingThunderMinLaunchAngle");
        rollingThunderMaxLaunchAngleProperty = FindProperty("rollingThunderMaxLaunchAngle");
        createsShockTherapyProperty = FindProperty("createsShockTherapy");
        shockTherapyMinTargetsProperty = FindProperty("shockTherapyMinTargets");
        shockTherapyMaxTargetsProperty = FindProperty("shockTherapyMaxTargets");
        shockTherapyDamageProperty = FindProperty("shockTherapyDamage");
        shockTherapyHealAmountProperty = FindProperty("shockTherapyHealAmount");
        createsPressurizedSplashProperty = FindProperty("createsPressurizedSplash");
        pressurePerHitProperty = FindProperty("pressurePerHit");
        maxPressureProperty = FindProperty("maxPressure");
        splashDropletTypeProperty = FindProperty("splashDropletType");
        splashDropletCountProperty = FindProperty("splashDropletCount");
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
        DrawSection("Brick Interaction", passThroughBricksProperty, passThroughBallsProperty, directionRestraintProperty, destroyOnWallProperty, appliesBurnProperty);
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
        EditorGUILayout.PropertyField(createsTremorProperty);
        DrawConditionalGroup(createsTremorProperty, tremorCrackDamageProperty, tremorCrackRadiusProperty);
        EditorGUILayout.PropertyField(createsAbrasionProperty);
        DrawConditionalGroup(createsAbrasionProperty, abrasionWeakenDurationProperty);
        EditorGUILayout.PropertyField(createsCycloneProperty);
        DrawConditionalGroup(createsCycloneProperty, cycloneFollowUpHitCountProperty, cycloneHitDelayProperty, cycloneCurveStrengthProperty);
        EditorGUILayout.PropertyField(appliesRootProperty);
        DrawConditionalGroup(appliesRootProperty, rootDurationProperty, rootSpeedMultiplierProperty);
        EditorGUILayout.PropertyField(createsSeedProperty);
        DrawConditionalGroup(
            createsSeedProperty,
            seedRootDurationProperty,
            seedRootSpeedMultiplierProperty,
            seedSpreadRadiusProperty,
            seedSpreadCountProperty,
            seedSpreadGenerationsProperty);
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
        EditorGUILayout.PropertyField(createsFertileLandProperty);
        DrawConditionalGroup(
            createsFertileLandProperty,
            fertilePatchSpriteProperty,
            fertilePatchColorProperty,
            fertilePatchSizeMultiplierProperty,
            fertilePatchSpawnIntervalProperty,
            fertilePatchRiseSpeedProperty,
            fertilePatchLifetimeProperty,
            fertilePatchCrackShatterDamageProperty,
            fertilePatchCrackShatterRadiusProperty,
            fertilePatchRootRadiusProperty,
            fertilePatchRootDurationProperty,
            fertilePatchRootSpeedMultiplierProperty);
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
        EditorGUILayout.PropertyField(createsCollapseProperty);
        DrawConditionalGroup(createsCollapseProperty, collapseRadiusProperty, collapseDurationProperty);
        EditorGUILayout.PropertyField(createsLinearProjectileProperty, new GUIContent("Create Linear Projectile"));
        DrawConditionalGroup(
            createsLinearProjectileProperty,
            linearProjectileTypeProperty,
            linearProjectileIncludesTopWallProperty);
        EditorGUILayout.PropertyField(createsBlackoutProperty);
        DrawConditionalGroup(createsBlackoutProperty, blackoutDamageProperty, blackoutIntervalProperty);
        EditorGUILayout.PropertyField(createsFirstAidProperty);
        DrawConditionalGroup(createsFirstAidProperty, firstAidHealPerHitProperty, firstAidHealThresholdProperty, firstAidExplosionDamageProperty, firstAidExplosionRadiusProperty);
        EditorGUILayout.PropertyField(createsElectricCascadeProperty);
        DrawConditionalGroup(
            createsElectricCascadeProperty,
            electricCascadeShockDamageProperty,
            electricCascadeConductiveDurationProperty);
        EditorGUILayout.PropertyField(createsRollingThunderProperty);
        DrawConditionalGroup(
            createsRollingThunderProperty,
            rollingThunderStartScaleMultiplierProperty,
            rollingThunderMaxScaleMultiplierProperty,
            rollingThunderGrowthAmountProperty,
            rollingThunderSpawnBallTypeProperty,
            rollingThunderMinLaunchAngleProperty,
            rollingThunderMaxLaunchAngleProperty);
        EditorGUILayout.PropertyField(createsShockTherapyProperty);
        DrawConditionalGroup(
            createsShockTherapyProperty,
            shockTherapyMinTargetsProperty,
            shockTherapyMaxTargetsProperty,
            shockTherapyDamageProperty,
            shockTherapyHealAmountProperty);
        EditorGUILayout.PropertyField(createsPressurizedSplashProperty);
        DrawConditionalGroup(
            createsPressurizedSplashProperty,
            pressurePerHitProperty,
            maxPressureProperty,
            splashDropletTypeProperty,
            splashDropletCountProperty);

        DrawSection("Compound", isCompoundProperty);

        DrawSection("Elements");
        EditorGUILayout.PropertyField(elementsProperty, includeChildren: true);

        DrawSection("Strong Against");
        EditorGUILayout.PropertyField(strongAgainstProperty, includeChildren: true);

        serializedObject.ApplyModifiedProperties();
    }
}