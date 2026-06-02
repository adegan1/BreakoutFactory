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
    private SerializedProperty trailColorProperty;
    private SerializedProperty trailColorsProperty;
    private SerializedProperty trailColorCycleRateProperty;
    private SerializedProperty trailColorModeProperty;
    private SerializedProperty ballSpriteProperty;
    private SerializedProperty ballMaterialProperty;
    private SerializedProperty animateTextureProperty;
    private SerializedProperty animFrameColumnsProperty;
    private SerializedProperty animFrameRowsProperty;
    private SerializedProperty animFrameRateProperty;
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
    private SerializedProperty lightningBurstBoltColorProperty;
    private SerializedProperty lightningBurstBoltWidthProperty;
    private SerializedProperty lightningBurstBoltLifetimeProperty;
    private SerializedProperty lightningBurstBoltSegmentsProperty;
    private SerializedProperty lightningBurstBoltNoiseProperty;
    private SerializedProperty lightningBurstBoltMaterialProperty;
    private SerializedProperty lightningSnakeVisualModeProperty;
    private SerializedProperty lightningSnakeBoltColorProperty;
    private SerializedProperty lightningSnakeBoltWidthProperty;
    private SerializedProperty lightningSnakeBoltLifetimeProperty;
    private SerializedProperty lightningSnakeBoltSegmentsProperty;
    private SerializedProperty lightningSnakeBoltNoiseProperty;
    private SerializedProperty lightningSnakeBoltMaterialProperty;
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
    private SerializedProperty flameTrailAnimSpritesProperty;
    private SerializedProperty flameTrailAnimFrameRateProperty;
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
    private SerializedProperty fertilePatchAnimSpritesProperty;
    private SerializedProperty fertilePatchAnimFrameRateProperty;
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
    private SerializedProperty linearProjectileSpriteProperty;
    private SerializedProperty linearProjectileAnimSpritesProperty;
    private SerializedProperty linearProjectileAnimFrameRateProperty;
    private SerializedProperty linearProjectileColorProperty;
    private SerializedProperty linearProjectileSizeProperty;
    private SerializedProperty linearProjectileSpeedProperty;
    private SerializedProperty linearProjectileDamageProperty;
    private SerializedProperty linearProjectileAppliesBurnProperty;
    private SerializedProperty linearProjectileBurnDamageProperty;
    private SerializedProperty linearProjectileBurnTickIntervalProperty;
    private SerializedProperty linearProjectileBurnHitCountProperty;
    private SerializedProperty linearProjectileAppliesCrackProperty;
    private SerializedProperty linearProjectileCrackShatterDamageProperty;
    private SerializedProperty linearProjectileCrackShatterRadiusProperty;
    private SerializedProperty linearProjectileAppliesRootProperty;
    private SerializedProperty linearProjectileRootDurationProperty;
    private SerializedProperty linearProjectileRootSpeedMultiplierProperty;
    private SerializedProperty linearProjectileHitsBeforeDestroyProperty;
    private SerializedProperty linearProjectileIncludesTopWallProperty;
    private SerializedProperty createsBlackoutProperty;
    private SerializedProperty blackoutDamageProperty;
    private SerializedProperty blackoutIntervalProperty;
    private SerializedProperty blackoutBoltColorsProperty;
    private SerializedProperty blackoutBoltWidthProperty;
    private SerializedProperty blackoutBoltLifetimeProperty;
    private SerializedProperty blackoutBoltSegmentsProperty;
    private SerializedProperty blackoutBoltNoiseProperty;
    private SerializedProperty blackoutBoltMaterialProperty;
    private SerializedProperty createsFirstAidProperty;
    private SerializedProperty firstAidHealPerHitProperty;
    private SerializedProperty firstAidHealThresholdProperty;
    private SerializedProperty firstAidExplosionDamageProperty;
    private SerializedProperty firstAidExplosionRadiusProperty;
    private SerializedProperty createsElectricCascadeProperty;
    private SerializedProperty electricCascadeShockDamageProperty;
    private SerializedProperty electricCascadeConductiveDurationProperty;
    private SerializedProperty electricCascadeBeamColorProperty;
    private SerializedProperty electricCascadeBeamWidthProperty;
    private SerializedProperty electricCascadeBeamLifetimeProperty;
    private SerializedProperty electricCascadeBeamSegmentsProperty;
    private SerializedProperty electricCascadeBeamNoiseProperty;
    private SerializedProperty electricCascadeBeamLengthProperty;
    private SerializedProperty electricCascadeBeamMaterialProperty;
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
    private SerializedProperty shockTherapyBoltColorProperty;
    private SerializedProperty shockTherapyBoltWidthProperty;
    private SerializedProperty shockTherapyBoltLifetimeProperty;
    private SerializedProperty shockTherapyBoltSegmentsProperty;
    private SerializedProperty shockTherapyBoltNoiseProperty;
    private SerializedProperty shockTherapyBoltMaterialProperty;
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
        trailColorProperty = FindProperty("trailColor");
        trailColorsProperty = FindProperty("trailColors");
        trailColorCycleRateProperty = FindProperty("trailColorCycleRate");
        trailColorModeProperty = FindProperty("trailColorMode");
        ballSpriteProperty = FindProperty("ballSprite");
        ballMaterialProperty = FindProperty("ballMaterial");
        animateTextureProperty = FindProperty("animateTexture");
        animFrameColumnsProperty = FindProperty("animFrameColumns");
        animFrameRowsProperty = FindProperty("animFrameRows");
        animFrameRateProperty = FindProperty("animFrameRate");
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
        lightningBurstBoltColorProperty = FindProperty("lightningBurstBoltColor");
        lightningBurstBoltWidthProperty = FindProperty("lightningBurstBoltWidth");
        lightningBurstBoltLifetimeProperty = FindProperty("lightningBurstBoltLifetime");
        lightningBurstBoltSegmentsProperty = FindProperty("lightningBurstBoltSegments");
        lightningBurstBoltNoiseProperty = FindProperty("lightningBurstBoltNoise");
        lightningBurstBoltMaterialProperty = FindProperty("lightningBurstBoltMaterial");
        lightningSnakeVisualModeProperty = FindProperty("lightningSnakeVisualMode");
        lightningSnakeBoltColorProperty = FindProperty("lightningSnakeBoltColor");
        lightningSnakeBoltWidthProperty = FindProperty("lightningSnakeBoltWidth");
        lightningSnakeBoltLifetimeProperty = FindProperty("lightningSnakeBoltLifetime");
        lightningSnakeBoltSegmentsProperty = FindProperty("lightningSnakeBoltSegments");
        lightningSnakeBoltNoiseProperty = FindProperty("lightningSnakeBoltNoise");
        lightningSnakeBoltMaterialProperty = FindProperty("lightningSnakeBoltMaterial");
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
        flameTrailAnimSpritesProperty = FindProperty("flameTrailAnimSprites");
        flameTrailAnimFrameRateProperty = FindProperty("flameTrailAnimFrameRate");
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
        fertilePatchAnimSpritesProperty = FindProperty("fertilePatchAnimSprites");
        fertilePatchAnimFrameRateProperty = FindProperty("fertilePatchAnimFrameRate");
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
        linearProjectileSpriteProperty = FindProperty("linearProjectileSprite");
        linearProjectileAnimSpritesProperty = FindProperty("linearProjectileAnimSprites");
        linearProjectileAnimFrameRateProperty = FindProperty("linearProjectileAnimFrameRate");
        linearProjectileColorProperty = FindProperty("linearProjectileColor");
        linearProjectileSizeProperty = FindProperty("linearProjectileSize");
        linearProjectileSpeedProperty = FindProperty("linearProjectileSpeed");
        linearProjectileDamageProperty = FindProperty("linearProjectileDamage");
        linearProjectileAppliesBurnProperty = FindProperty("linearProjectileAppliesBurn");
        linearProjectileBurnDamageProperty = FindProperty("linearProjectileBurnDamage");
        linearProjectileBurnTickIntervalProperty = FindProperty("linearProjectileBurnTickInterval");
        linearProjectileBurnHitCountProperty = FindProperty("linearProjectileBurnHitCount");
        linearProjectileAppliesCrackProperty = FindProperty("linearProjectileAppliesCrack");
        linearProjectileCrackShatterDamageProperty = FindProperty("linearProjectileCrackShatterDamage");
        linearProjectileCrackShatterRadiusProperty = FindProperty("linearProjectileCrackShatterRadius");
        linearProjectileAppliesRootProperty = FindProperty("linearProjectileAppliesRoot");
        linearProjectileRootDurationProperty = FindProperty("linearProjectileRootDuration");
        linearProjectileRootSpeedMultiplierProperty = FindProperty("linearProjectileRootSpeedMultiplier");
        linearProjectileHitsBeforeDestroyProperty = FindProperty("linearProjectileHitsBeforeDestroy");
        linearProjectileIncludesTopWallProperty = FindProperty("linearProjectileIncludesTopWall");
        createsBlackoutProperty = FindProperty("createsBlackout");
        blackoutDamageProperty = FindProperty("blackoutDamage");
        blackoutIntervalProperty = FindProperty("blackoutInterval");
        blackoutBoltColorsProperty = FindProperty("blackoutBoltColors");
        blackoutBoltWidthProperty = FindProperty("blackoutBoltWidth");
        blackoutBoltLifetimeProperty = FindProperty("blackoutBoltLifetime");
        blackoutBoltSegmentsProperty = FindProperty("blackoutBoltSegments");
        blackoutBoltNoiseProperty = FindProperty("blackoutBoltNoise");
        blackoutBoltMaterialProperty = FindProperty("blackoutBoltMaterial");
        createsFirstAidProperty = FindProperty("createsFirstAid");
        firstAidHealPerHitProperty = FindProperty("firstAidHealPerHit");
        firstAidHealThresholdProperty = FindProperty("firstAidHealThreshold");
        firstAidExplosionDamageProperty = FindProperty("firstAidExplosionDamage");
        firstAidExplosionRadiusProperty = FindProperty("firstAidExplosionRadius");
        createsElectricCascadeProperty = FindProperty("createsElectricCascade");
        electricCascadeShockDamageProperty = FindProperty("electricCascadeShockDamage");
        electricCascadeConductiveDurationProperty = FindProperty("electricCascadeConductiveDuration");
        electricCascadeBeamColorProperty = FindProperty("electricCascadeBeamColor");
        electricCascadeBeamWidthProperty = FindProperty("electricCascadeBeamWidth");
        electricCascadeBeamLifetimeProperty = FindProperty("electricCascadeBeamLifetime");
        electricCascadeBeamSegmentsProperty = FindProperty("electricCascadeBeamSegments");
        electricCascadeBeamNoiseProperty = FindProperty("electricCascadeBeamNoise");
        electricCascadeBeamLengthProperty = FindProperty("electricCascadeBeamLength");
        electricCascadeBeamMaterialProperty = FindProperty("electricCascadeBeamMaterial");
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
        shockTherapyBoltColorProperty = FindProperty("shockTherapyBoltColor");
        shockTherapyBoltWidthProperty = FindProperty("shockTherapyBoltWidth");
        shockTherapyBoltLifetimeProperty = FindProperty("shockTherapyBoltLifetime");
        shockTherapyBoltSegmentsProperty = FindProperty("shockTherapyBoltSegments");
        shockTherapyBoltNoiseProperty = FindProperty("shockTherapyBoltNoise");
        shockTherapyBoltMaterialProperty = FindProperty("shockTherapyBoltMaterial");
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
        DrawSection("Display", displayNameProperty, descriptionProperty, trailColorModeProperty, ballSpriteProperty, ballMaterialProperty, sizeProperty, animateTextureProperty);
        DrawConditionalGroup(animateTextureProperty, animFrameColumnsProperty, animFrameRowsProperty, animFrameRateProperty);
        if (trailColorModeProperty.enumValueIndex == 0)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(trailColorProperty);
            EditorGUI.indentLevel--;
        }
        else if (trailColorModeProperty.enumValueIndex == 1)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(trailColorsProperty, new GUIContent("Trail Colors"), true);
            EditorGUILayout.PropertyField(trailColorCycleRateProperty);
            EditorGUI.indentLevel--;
        }
        DrawSection("Movement", movementSpeedProperty, directionRestraintProperty);
        DrawSection("Core Combat", damageProperty, bouncesProperty, timedEffectInitialDelayProperty);
        DrawSection("Brick Interaction", passThroughBricksProperty, passThroughBallsProperty, destroyOnWallProperty, appliesBurnProperty);
        DrawConditionalGroup(appliesBurnProperty, burnDamageProperty, burnTickIntervalProperty, burnHitCountProperty);
        EditorGUILayout.PropertyField(lightningBurstProperty, new GUIContent("Lightning Burst"));
        DrawConditionalGroup(lightningBurstProperty,
            lightningBurstTargetCountProperty,
            lightningBurstDamageProperty,
            lightningBurstRadiusProperty,
            lightningBurstBoltColorProperty,
            lightningBurstBoltWidthProperty,
            lightningBurstBoltLifetimeProperty,
            lightningBurstBoltSegmentsProperty,
            lightningBurstBoltNoiseProperty,
            lightningBurstBoltMaterialProperty);
        EditorGUILayout.PropertyField(createsLightningSnakeProperty, new GUIContent("Lightning Snake"));
        DrawConditionalGroup(
            createsLightningSnakeProperty,
            lightningSnakeBounceCountProperty,
            lightningSnakeDamageProperty,
            lightningSnakeRadiusProperty,
            lightningSnakeWaterSplitCountProperty,
            lightningSnakeBounceDelayProperty,
            lightningSnakeVisualModeProperty,
            lightningSnakeBoltColorProperty,
            lightningSnakeBoltWidthProperty,
            lightningSnakeBoltLifetimeProperty,
            lightningSnakeBoltSegmentsProperty,
            lightningSnakeBoltNoiseProperty,
            lightningSnakeBoltMaterialProperty);
        EditorGUILayout.PropertyField(earthCrackProperty, new GUIContent("Apply Crack"));
        DrawConditionalGroup(earthCrackProperty, shatterDamageProperty, shatterRadiusProperty);
        EditorGUILayout.PropertyField(createsTremorProperty, new GUIContent("Tremor"));
        DrawConditionalGroup(createsTremorProperty, tremorCrackDamageProperty, tremorCrackRadiusProperty);
        if (createsTremorProperty.boolValue && !earthCrackProperty.boolValue)
        {
            EditorGUILayout.HelpBox("Tremor modifies existing crack/shatter behavior. Enable Earth Crack (or another crack source) for consistent results.", MessageType.Info);
        }
        EditorGUILayout.PropertyField(createsAbrasionProperty, new GUIContent("Abrasion"));
        DrawConditionalGroup(createsAbrasionProperty, abrasionWeakenDurationProperty);
        EditorGUILayout.PropertyField(createsCycloneProperty, new GUIContent("Cyclone"));
        DrawConditionalGroup(createsCycloneProperty, cycloneFollowUpHitCountProperty, cycloneHitDelayProperty, cycloneCurveStrengthProperty);
        EditorGUILayout.PropertyField(appliesRootProperty, new GUIContent("Apply Root"));
        DrawConditionalGroup(appliesRootProperty, rootDurationProperty, rootSpeedMultiplierProperty);
        EditorGUILayout.PropertyField(createsSeedProperty, new GUIContent("Seed Spread"));
        DrawConditionalGroup(
            createsSeedProperty,
            seedRootDurationProperty,
            seedRootSpeedMultiplierProperty,
            seedSpreadRadiusProperty,
            seedSpreadCountProperty,
            seedSpreadGenerationsProperty);
        if (createsSeedProperty.boolValue && !appliesRootProperty.boolValue)
        {
            EditorGUILayout.HelpBox("Seed spread now depends on Applies Root. Enable Applies Root to activate Seed effects.", MessageType.Info);
        }
        EditorGUILayout.PropertyField(createsCombustionProperty, new GUIContent("Combustion"));
        DrawConditionalGroup(
            createsCombustionProperty,
            combustionBurnDamageProperty,
            combustionBurnTickIntervalProperty,
            combustionBurnHitCountProperty,
            combustionExplosionDamageProperty,
            combustionExplosionRadiusProperty);
        EditorGUILayout.PropertyField(createsFireSpreadProperty, new GUIContent("Fire Spread"));
        DrawConditionalGroup(
            createsFireSpreadProperty,
            fireSpreadRadiusProperty,
            fireSpreadCooldownProperty,
            fireSpreadBonusBurnDamageProperty,
            fireSpreadBurnSpeedMultiplierProperty,
            fireSpreadBurnHitCountBonusProperty);
        EditorGUILayout.PropertyField(createsForestFireProperty, new GUIContent("Forest Fire"));
        DrawConditionalGroup(
            createsForestFireProperty,
            forestFireBurnDamageProperty,
            forestFireBurnTickIntervalProperty,
            forestFireBurnHitCountProperty,
            forestFireSpreadGenerationsProperty);
        EditorGUILayout.PropertyField(createsWaterDropsProperty, new GUIContent("Water Drops"));
        DrawConditionalGroup(createsWaterDropsProperty, waterDropletTypeProperty, waterDropCooldownProperty);
        EditorGUILayout.PropertyField(createsPressurizedSplashProperty, new GUIContent("Pressurized Splash"));
        DrawConditionalGroup(
            createsPressurizedSplashProperty,
            pressurePerHitProperty,
            maxPressureProperty,
            splashDropletTypeProperty,
            splashDropletCountProperty);

        EditorGUILayout.PropertyField(createsFlameTrailProperty, new GUIContent("Flame Trail"));
        DrawConditionalGroup(
            createsFlameTrailProperty,
            flameTrailSpriteProperty,
            flameTrailAnimSpritesProperty,
            flameTrailAnimFrameRateProperty,
            flameTrailColorProperty,
            flameTrailSizeMultiplierProperty,
            flameTrailSpawnIntervalProperty,
            flameTrailRiseSpeedProperty,
            flameTrailLifetimeProperty,
            flameTrailImpactDamageProperty,
            flameTrailBurnDamageProperty,
            flameTrailBurnTickIntervalProperty,
            flameTrailBurnHitCountProperty);
        EditorGUILayout.PropertyField(createsFertileLandProperty, new GUIContent("Fertile Land"));
        DrawConditionalGroup(
            createsFertileLandProperty,
            fertilePatchSpriteProperty,
            fertilePatchAnimSpritesProperty,
            fertilePatchAnimFrameRateProperty,
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
        if (createsFertileLandProperty.boolValue)
        {
            if (!earthCrackProperty.boolValue)
            {
                EditorGUILayout.HelpBox("Fertile Land crack values apply only when Earth Crack is enabled.", MessageType.Info);
            }

            if (!appliesRootProperty.boolValue)
            {
                EditorGUILayout.HelpBox("Fertile Land root values apply only when Applies Root is enabled.", MessageType.Info);
            }
        }
        EditorGUILayout.PropertyField(createsSteamBurstProperty, new GUIContent("Steam Burst"));
        DrawConditionalGroup(
            createsSteamBurstProperty,
            steamBurstBallTypeProperty,
            steamBurstBallCountProperty,
            steamBurstMinIntervalProperty,
            steamBurstMaxIntervalProperty,
            steamBurstSpawnRadiusProperty,
            steamBurstSpeedMultiplierProperty,
            steamBurstSpeedLerpDurationProperty);
        EditorGUILayout.PropertyField(impactBurstProperty, new GUIContent("Impact Burst"));
        DrawConditionalGroup(impactBurstProperty, impactBurstDamageProperty, impactBurstRadiusProperty);
        EditorGUILayout.PropertyField(createsCollapseProperty, new GUIContent("Collapse"));
        DrawConditionalGroup(createsCollapseProperty, collapseRadiusProperty, collapseDurationProperty);
        EditorGUILayout.PropertyField(createsLinearProjectileProperty, new GUIContent("Linear Projectile"));
        DrawConditionalGroup(
            createsLinearProjectileProperty,
            linearProjectileSpriteProperty,
            linearProjectileAnimSpritesProperty,
            linearProjectileAnimFrameRateProperty,
            linearProjectileColorProperty,
            linearProjectileSizeProperty,
            linearProjectileSpeedProperty,
            linearProjectileDamageProperty,
            linearProjectileAppliesBurnProperty,
            linearProjectileBurnDamageProperty,
            linearProjectileBurnTickIntervalProperty,
            linearProjectileBurnHitCountProperty,
            linearProjectileAppliesCrackProperty,
            linearProjectileCrackShatterDamageProperty,
            linearProjectileCrackShatterRadiusProperty,
            linearProjectileAppliesRootProperty,
            linearProjectileRootDurationProperty,
            linearProjectileRootSpeedMultiplierProperty,
            linearProjectileHitsBeforeDestroyProperty,
            linearProjectileIncludesTopWallProperty);
        EditorGUILayout.PropertyField(createsBlackoutProperty, new GUIContent("Blackout"));
        DrawConditionalGroup(createsBlackoutProperty,
            blackoutDamageProperty,
            blackoutIntervalProperty,
            blackoutBoltColorsProperty,
            blackoutBoltWidthProperty,
            blackoutBoltLifetimeProperty,
            blackoutBoltSegmentsProperty,
            blackoutBoltNoiseProperty,
            blackoutBoltMaterialProperty);
        EditorGUILayout.PropertyField(createsFirstAidProperty, new GUIContent("First Aid"));
        DrawConditionalGroup(createsFirstAidProperty, firstAidHealPerHitProperty, firstAidHealThresholdProperty, firstAidExplosionDamageProperty, firstAidExplosionRadiusProperty);
        EditorGUILayout.PropertyField(createsElectricCascadeProperty, new GUIContent("Electric Cascade"));
        DrawConditionalGroup(
            createsElectricCascadeProperty,
            electricCascadeShockDamageProperty,
            electricCascadeConductiveDurationProperty,
            electricCascadeBeamColorProperty,
            electricCascadeBeamWidthProperty,
            electricCascadeBeamLifetimeProperty,
            electricCascadeBeamSegmentsProperty,
            electricCascadeBeamNoiseProperty,
            electricCascadeBeamLengthProperty,
            electricCascadeBeamMaterialProperty);
        EditorGUILayout.PropertyField(createsRollingThunderProperty, new GUIContent("Rolling Thunder"));
        DrawConditionalGroup(
            createsRollingThunderProperty,
            rollingThunderStartScaleMultiplierProperty,
            rollingThunderMaxScaleMultiplierProperty,
            rollingThunderGrowthAmountProperty,
            rollingThunderSpawnBallTypeProperty,
            rollingThunderMinLaunchAngleProperty,
            rollingThunderMaxLaunchAngleProperty);
        EditorGUILayout.PropertyField(createsShockTherapyProperty, new GUIContent("Shock Therapy"));
        DrawConditionalGroup(
            createsShockTherapyProperty,
            shockTherapyMinTargetsProperty,
            shockTherapyMaxTargetsProperty,
            shockTherapyDamageProperty,
            shockTherapyHealAmountProperty,
            shockTherapyBoltColorProperty,
            shockTherapyBoltWidthProperty,
            shockTherapyBoltLifetimeProperty,
            shockTherapyBoltSegmentsProperty,
            shockTherapyBoltNoiseProperty,
            shockTherapyBoltMaterialProperty);

        DrawSection("Compound", isCompoundProperty);

        DrawSection("Elements");
        EditorGUILayout.PropertyField(elementsProperty, includeChildren: true);

        DrawSection("Strong Against");
        EditorGUILayout.PropertyField(strongAgainstProperty, includeChildren: true);

        serializedObject.ApplyModifiedProperties();
    }
}