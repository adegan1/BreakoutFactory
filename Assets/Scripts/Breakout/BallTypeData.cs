using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "New Ball Type", menuName = "Breakout/Ball Type Data")]
public class BallTypeData : ScriptableObject
{
    public enum BallElement
    {
        Basic,
        Fire,
        Water,
        Lightning,
        Life,
        Earth,
        Wind
    }

    public enum ComboEffectProfile
    {
        None,
        Burn,
        Burst,
        Collapse,
        LinearProjectile,
        Tremor,
        Abrasion,
        Chain,
        LightningSnake,
        Crack,
        FlameTrail,
        FertileLand,
        FireSpread,
        SteamBurst,
        Root,
        WaterDrops,
        ElectricCascade,
        RollingThunder,
        ShockTherapy,
        Pierce,
        Cyclone
    }

    public enum DirectionRestraint
    {
        None,
        HorizontalOnly,
        VerticalOnly
    }

    // Recipe
    [SerializeField] private BallElement primarySourceElement = BallElement.Basic;
    [SerializeField] private BallElement secondarySourceElement = BallElement.Basic;
    [SerializeField] private ComboEffectProfile primaryEffectProfile = ComboEffectProfile.None;
    [SerializeField] private ComboEffectProfile secondaryEffectProfile = ComboEffectProfile.None;

    // Display
    [SerializeField] private string displayName;
    [SerializeField, TextArea(2, 4)] private string description;
    [SerializeField] private Color displayColor = Color.white;
    [SerializeField] private Sprite ballSprite;
    [SerializeField, Range(0.25f, 3f)] private float size = 1f;

    // Movement
    [SerializeField, Min(0f)] private float movementSpeed = 8f;

    // Core Combat
    [SerializeField, Min(1)] private int damage = 1;
    [SerializeField] private int bounces = -1;

    // Brick Interaction
    [SerializeField] private bool passThroughBricks = false;
    [SerializeField] private bool passThroughBalls = false;
    [SerializeField] private DirectionRestraint directionRestraint = DirectionRestraint.None;
    [SerializeField] private bool destroyOnWall = false;
    [SerializeField] private bool appliesBurn = false;
    [SerializeField, Min(1)] private int burnDamage = 1;
    [SerializeField, Min(0.01f)] private float burnTickInterval = 0.5f;
    [SerializeField, Min(1)] private int burnHitCount = 3;
    [SerializeField] private bool lightningBurst = false;
    [SerializeField, Min(1)] private int lightningBurstTargetCount = 2;
    [SerializeField, Min(1)] private int lightningBurstDamage = 1;
    [SerializeField, Min(0.1f)] private float lightningBurstRadius = 2f;
    [SerializeField] private bool createsLightningSnake = false;
    [SerializeField, Min(1)] private int lightningSnakeBounceCount = 4;
    [SerializeField, Min(1)] private int lightningSnakeDamage = 1;
    [SerializeField, Min(0.1f)] private float lightningSnakeRadius = 2f;
    [SerializeField, Min(1)] private int lightningSnakeWaterSplitCount = 2;
    [SerializeField, Min(0f)] private float lightningSnakeBounceDelay = 0.06f;
    [SerializeField] private bool earthCrack = false;
    [SerializeField, Min(1)] private int shatterDamage = 2;
    [SerializeField, Min(0.1f)] private float shatterRadius = 1.4f;
    [SerializeField] private bool createsTremor = false;
    [SerializeField, Min(1)] private int tremorCrackDamage = 2;
    [SerializeField, Min(0.1f)] private float tremorCrackRadius = 1.4f;
    [SerializeField] private bool createsAbrasion = false;
    [SerializeField, Min(0.01f)] private float abrasionWeakenDuration = 2f;
    [SerializeField] private bool createsCyclone = false;
    [SerializeField, Min(1)] private int cycloneFollowUpHitCount = 2;
    [SerializeField, Min(0.01f)] private float cycloneHitDelay = 0.08f;
    [SerializeField, Min(0f)] private float cycloneCurveStrength = 18f;
    [SerializeField] private bool appliesRoot = false;
    [SerializeField, Min(0.1f)] private float rootDuration = 2f;
    [SerializeField, Range(0f, 1f)] private float rootSpeedMultiplier = 0.6f;
    [SerializeField] private bool createsSeed = false;
    [SerializeField, Min(0.1f)] private float seedRootDuration = 2.5f;
    [SerializeField, Range(0f, 1f)] private float seedRootSpeedMultiplier = 0.3f;
    [SerializeField, Min(0.1f)] private float seedSpreadRadius = 2.5f;
    [SerializeField, Min(1)] private int seedSpreadCount = 2;
    [SerializeField, Min(0)] private int seedSpreadGenerations = 3;
    [SerializeField] private bool createsCombustion = false;
    [SerializeField, Min(1)] private int combustionBurnDamage = 1;
    [SerializeField, Min(0.01f)] private float combustionBurnTickInterval = 0.5f;
    [SerializeField, Min(1)] private int combustionBurnHitCount = 3;
    [SerializeField, Min(1)] private int combustionExplosionDamage = 2;
    [SerializeField, Min(0.1f)] private float combustionExplosionRadius = 1.8f;
    [SerializeField] private bool createsFireSpread = false;
    [SerializeField, Min(0.1f)] private float fireSpreadRadius = 1.75f;
    [SerializeField, Min(0f)] private float fireSpreadCooldown = 0.35f;
    [SerializeField, Min(0)] private int fireSpreadBonusBurnDamage = 1;
    [SerializeField, Min(1f)] private float fireSpreadBurnSpeedMultiplier = 1.25f;
    [SerializeField, Min(0)] private int fireSpreadBurnHitCountBonus = 1;
    [SerializeField] private bool createsForestFire = false;
    [SerializeField, Min(1)] private int forestFireBurnDamage = 1;
    [SerializeField, Min(0.01f)] private float forestFireBurnTickInterval = 0.5f;
    [SerializeField, Min(1)] private int forestFireBurnHitCount = 3;
    [SerializeField, Min(0)] private int forestFireSpreadGenerations = 2;
    [SerializeField] private bool createsWaterDrops = false;
    [SerializeField] private BallTypeData waterDropletType;
    [SerializeField, Min(0.01f)] private float waterDropCooldown = 0.08f;
    [SerializeField] private bool createsFlameTrail = false;
    [SerializeField] private Sprite flameTrailSprite;
    [SerializeField] private Color flameTrailColor = new Color(1f, 0.45f, 0.1f, 0.95f);
    [SerializeField, Range(0.1f, 2f)] private float flameTrailSizeMultiplier = 0.55f;
    [SerializeField, Min(0.01f)] private float flameTrailSpawnInterval = 0.2f;
    [SerializeField, Min(0.1f)] private float flameTrailRiseSpeed = 2.5f;
    [SerializeField, Min(0.1f)] private float flameTrailLifetime = 2.25f;
    [SerializeField, Min(0)] private int flameTrailImpactDamage = 1;
    [SerializeField, Min(1)] private int flameTrailBurnDamage = 1;
    [SerializeField, Min(0.01f)] private float flameTrailBurnTickInterval = 0.5f;
    [SerializeField, Min(1)] private int flameTrailBurnHitCount = 2;
    [SerializeField] private bool createsFertileLand = false;
    [SerializeField] private Sprite fertilePatchSprite;
    [SerializeField] private Color fertilePatchColor = new Color(0.35f, 0.85f, 0.35f, 0.95f);
    [SerializeField, Range(0.1f, 2f)] private float fertilePatchSizeMultiplier = 0.55f;
    [SerializeField, Min(0.01f)] private float fertilePatchSpawnInterval = 0.3f;
    [SerializeField, Min(0.1f)] private float fertilePatchRiseSpeed = 2.5f;
    [SerializeField, Min(0.1f)] private float fertilePatchLifetime = 2.25f;
    [SerializeField, Min(1)] private int fertilePatchCrackShatterDamage = 2;
    [SerializeField, Min(0.1f)] private float fertilePatchCrackShatterRadius = 1.4f;
    [SerializeField, Min(0.1f)] private float fertilePatchRootRadius = 2f;
    [SerializeField, Min(0.1f)] private float fertilePatchRootDuration = 2f;
    [SerializeField, Range(0f, 1f)] private float fertilePatchRootSpeedMultiplier = 0.6f;
    [SerializeField, Min(0f)] private float timedEffectInitialDelay = 0.15f;
    [SerializeField] private bool createsSteamBurst = false;
    [SerializeField] private BallTypeData steamBurstBallType;
    [SerializeField, Min(2)] private int steamBurstBallCount = 8;
    [SerializeField, Min(0.1f)] private float steamBurstMinInterval = 3f;
    [SerializeField, Min(0.1f)] private float steamBurstMaxInterval = 6f;
    [SerializeField, Min(0.01f)] private float steamBurstSpawnRadius = 0.18f;
    [SerializeField, Min(1f)] private float steamBurstSpeedMultiplier = 1.35f;
    [SerializeField, Min(0.1f)] private float steamBurstSpeedLerpDuration = 1.5f;
    [SerializeField] private bool impactBurst = false;
    [SerializeField, Min(1)] private int impactBurstDamage = 1;
    [SerializeField, Min(0.1f)] private float impactBurstRadius = 1.6f;
    [SerializeField] private bool createsCollapse = false;
    [SerializeField, Min(0.1f)] private float collapseRadius = 1.8f;
    [SerializeField, Min(0.01f)] private float collapseDuration = 3f;
    [FormerlySerializedAs("createsOceanBreeze")]
    [SerializeField] private bool createsLinearProjectile = false;
    [FormerlySerializedAs("oceanBreezeWaveType")]
    [SerializeField] private BallTypeData linearProjectileType;
    [SerializeField] private bool linearProjectileIncludesTopWall = false;
    [SerializeField] private bool createsBlackout = false;
    [SerializeField, Min(1)] private int blackoutDamage = 1;
    [SerializeField, Min(0.1f)] private float blackoutInterval = 1f;
    [SerializeField] private bool createsFirstAid = false;
    [SerializeField, Min(1)] private int firstAidHealPerHit = 1;
    [SerializeField, Min(1)] private int firstAidHealThreshold = 5;
    [SerializeField, Min(1)] private int firstAidExplosionDamage = 3;
    [SerializeField, Min(0.1f)] private float firstAidExplosionRadius = 2f;
    [SerializeField] private bool createsElectricCascade = false;
    [SerializeField, Min(1)] private int electricCascadeShockDamage = 1;
    [SerializeField, Min(0.01f)] private float electricCascadeConductiveDuration = 3f;
    [SerializeField] private bool createsRollingThunder = false;
    [SerializeField, Min(1f)] private float rollingThunderStartScaleMultiplier = 1f;
    [SerializeField, Min(1f)] private float rollingThunderMaxScaleMultiplier = 2f;
    [SerializeField, Min(0.01f)] private float rollingThunderGrowthAmount = 0.2f;
    [SerializeField] private BallTypeData rollingThunderSpawnBallType;
    [SerializeField] private float rollingThunderMinLaunchAngle = 20f;
    [SerializeField] private float rollingThunderMaxLaunchAngle = 160f;
    [SerializeField] private bool createsShockTherapy = false;
    [SerializeField, Min(1)] private int shockTherapyMinTargets = 1;
    [SerializeField, Min(1)] private int shockTherapyMaxTargets = 3;
    [SerializeField, Min(1)] private int shockTherapyDamage = 1;
    [SerializeField, Min(1)] private int shockTherapyHealAmount = 1;
    [SerializeField] private bool createsPressurizedSplash = false;
    [SerializeField, Min(1)] private int pressurePerHit = 1;
    [SerializeField, Min(1)] private int maxPressure = 4;
    [SerializeField] private BallTypeData splashDropletType;
    [SerializeField, Min(1)] private int splashDropletCount = 4;

    // Compound
    [SerializeField] private bool isCompound = false;

    // Elements
    [SerializeField] private BallElement[] elements = new BallElement[] { BallElement.Basic };

    // Strong Against...
    [SerializeField] private BallElement[] strongAgainst = new BallElement[0];

    public BallElement PrimarySourceElement => primarySourceElement;
    public BallElement SecondarySourceElement => secondarySourceElement;
    public ComboEffectProfile PrimaryEffectProfile => primaryEffectProfile;
    public ComboEffectProfile SecondaryEffectProfile => secondaryEffectProfile;
    public string DisplayName => displayName;
    public string Description => description;
    public Color DisplayColor => displayColor;
    public Sprite BallSprite => ballSprite;
    public float Size => size;
    public float MovementSpeed => movementSpeed;
    public int Damage => damage;
    public int Bounces => bounces;
    public bool PassThroughBricks => passThroughBricks;
    public bool PassThroughBalls => passThroughBalls;
    public DirectionRestraint MovementRestraint => directionRestraint;
    public bool DestroyOnWall => destroyOnWall;
    public bool AppliesBurn => appliesBurn;
    public int BurnDamage => burnDamage;
    public float BurnTickInterval => burnTickInterval;
    public int BurnHitCount => burnHitCount;
    public bool LightningBurst => lightningBurst;
    public int LightningBurstTargetCount => lightningBurstTargetCount;
    public int LightningBurstDamage => lightningBurstDamage;
    public float LightningBurstRadius => lightningBurstRadius;
    public bool CreatesLightningSnake => createsLightningSnake;
    public int LightningSnakeBounceCount => lightningSnakeBounceCount;
    public int LightningSnakeDamage => lightningSnakeDamage;
    public float LightningSnakeRadius => lightningSnakeRadius;
    public int LightningSnakeWaterSplitCount => lightningSnakeWaterSplitCount;
    public float LightningSnakeBounceDelay => lightningSnakeBounceDelay;
    public bool EarthCrack => earthCrack;
    public int ShatterDamage => shatterDamage;
    public float ShatterRadius => shatterRadius;
    public bool CreatesTremor => createsTremor;
    public int TremorCrackDamage => tremorCrackDamage;
    public float TremorCrackRadius => tremorCrackRadius;
    public bool CreatesAbrasion => createsAbrasion;
    public float AbrasionWeakenDuration => abrasionWeakenDuration;
    public bool CreatesCyclone => createsCyclone;
    public int CycloneFollowUpHitCount => cycloneFollowUpHitCount;
    public float CycloneHitDelay => cycloneHitDelay;
    public float CycloneCurveStrength => cycloneCurveStrength;
    public bool AppliesRoot => appliesRoot;
    public float RootDuration => rootDuration;
    public float RootSpeedMultiplier => rootSpeedMultiplier;
    public bool CreatesSeed => createsSeed;
    public float SeedRootDuration => seedRootDuration;
    public float SeedRootSpeedMultiplier => seedRootSpeedMultiplier;
    public float SeedSpreadRadius => seedSpreadRadius;
    public int SeedSpreadCount => seedSpreadCount;
    public int SeedSpreadGenerations => seedSpreadGenerations;
    public bool CreatesCombustion => createsCombustion;
    public int CombustionBurnDamage => combustionBurnDamage;
    public float CombustionBurnTickInterval => combustionBurnTickInterval;
    public int CombustionBurnHitCount => combustionBurnHitCount;
    public int CombustionExplosionDamage => combustionExplosionDamage;
    public float CombustionExplosionRadius => combustionExplosionRadius;
    public bool CreatesFireSpread => createsFireSpread;
    public float FireSpreadRadius => fireSpreadRadius;
    public float FireSpreadCooldown => fireSpreadCooldown;
    public int FireSpreadBonusBurnDamage => fireSpreadBonusBurnDamage;
    public float FireSpreadBurnSpeedMultiplier => fireSpreadBurnSpeedMultiplier;
    public int FireSpreadBurnHitCountBonus => fireSpreadBurnHitCountBonus;
    public bool CreatesForestFire => createsForestFire;
    public int ForestFireBurnDamage => forestFireBurnDamage;
    public float ForestFireBurnTickInterval => forestFireBurnTickInterval;
    public int ForestFireBurnHitCount => forestFireBurnHitCount;
    public int ForestFireSpreadGenerations => forestFireSpreadGenerations;
    public bool CreatesWaterDrops => createsWaterDrops;
    public BallTypeData WaterDropletType => waterDropletType;
    public float WaterDropCooldown => waterDropCooldown;
    public bool CreatesFlameTrail => createsFlameTrail;
    public Sprite FlameTrailSprite => flameTrailSprite;
    public Color FlameTrailColor => flameTrailColor;
    public float FlameTrailSizeMultiplier => flameTrailSizeMultiplier;
    public float FlameTrailSpawnInterval => flameTrailSpawnInterval;
    public float FlameTrailRiseSpeed => flameTrailRiseSpeed;
    public float FlameTrailLifetime => flameTrailLifetime;
    public int FlameTrailImpactDamage => flameTrailImpactDamage;
    public int FlameTrailBurnDamage => flameTrailBurnDamage;
    public float FlameTrailBurnTickInterval => flameTrailBurnTickInterval;
    public int FlameTrailBurnHitCount => flameTrailBurnHitCount;
    public bool CreatesFertileLand => createsFertileLand;
    public Sprite FertilePatchSprite => fertilePatchSprite;
    public Color FertilePatchColor => fertilePatchColor;
    public float FertilePatchSizeMultiplier => fertilePatchSizeMultiplier;
    public float FertilePatchSpawnInterval => fertilePatchSpawnInterval;
    public float FertilePatchRiseSpeed => fertilePatchRiseSpeed;
    public float FertilePatchLifetime => fertilePatchLifetime;
    public int FertilePatchCrackShatterDamage => fertilePatchCrackShatterDamage;
    public float FertilePatchCrackShatterRadius => fertilePatchCrackShatterRadius;
    public float FertilePatchRootRadius => fertilePatchRootRadius;
    public float FertilePatchRootDuration => fertilePatchRootDuration;
    public float FertilePatchRootSpeedMultiplier => fertilePatchRootSpeedMultiplier;
    public float TimedEffectInitialDelay => timedEffectInitialDelay;
    public bool CreatesSteamBurst => createsSteamBurst;
    public BallTypeData SteamBurstBallType => steamBurstBallType;
    public int SteamBurstBallCount => steamBurstBallCount;
    public float SteamBurstMinInterval => steamBurstMinInterval;
    public float SteamBurstMaxInterval => steamBurstMaxInterval;
    public float SteamBurstSpawnRadius => steamBurstSpawnRadius;
    public float SteamBurstSpeedMultiplier => steamBurstSpeedMultiplier;
    public float SteamBurstSpeedLerpDuration => steamBurstSpeedLerpDuration;
    public bool ImpactBurst => impactBurst;
    public int ImpactBurstDamage => impactBurstDamage;
    public float ImpactBurstRadius => impactBurstRadius;
    public bool CreatesCollapse => createsCollapse;
    public float CollapseRadius => collapseRadius;
    public float CollapseDuration => collapseDuration;
    public bool CreatesLinearProjectile => createsLinearProjectile;
    public BallTypeData LinearProjectileType => linearProjectileType;
    public bool LinearProjectileIncludesTopWall => linearProjectileIncludesTopWall;
    public bool CreatesBlackout => createsBlackout;
    public int BlackoutDamage => blackoutDamage;
    public float BlackoutInterval => blackoutInterval;
    public bool CreatesFirstAid => createsFirstAid;
    public int FirstAidHealPerHit => firstAidHealPerHit;
    public int FirstAidHealThreshold => firstAidHealThreshold;
    public int FirstAidExplosionDamage => firstAidExplosionDamage;
    public float FirstAidExplosionRadius => firstAidExplosionRadius;
    public bool CreatesElectricCascade => createsElectricCascade;
    public int ElectricCascadeShockDamage => electricCascadeShockDamage;
    public float ElectricCascadeConductiveDuration => electricCascadeConductiveDuration;
    public bool CreatesRollingThunder => createsRollingThunder;
    public float RollingThunderStartScaleMultiplier => rollingThunderStartScaleMultiplier;
    public float RollingThunderMaxScaleMultiplier => rollingThunderMaxScaleMultiplier;
    public float RollingThunderGrowthAmount => rollingThunderGrowthAmount;
    public BallTypeData RollingThunderSpawnBallType => rollingThunderSpawnBallType;
    public float RollingThunderMinLaunchAngle => rollingThunderMinLaunchAngle;
    public float RollingThunderMaxLaunchAngle => rollingThunderMaxLaunchAngle;
    public bool CreatesShockTherapy => createsShockTherapy;
    public int ShockTherapyMinTargets => shockTherapyMinTargets;
    public int ShockTherapyMaxTargets => shockTherapyMaxTargets;
    public int ShockTherapyDamage => shockTherapyDamage;
    public int ShockTherapyHealAmount => shockTherapyHealAmount;
    public bool CreatesPressurizedSplash => createsPressurizedSplash;
    public int PressurePerHit => pressurePerHit;
    public int MaxPressure => maxPressure;
    public BallTypeData SplashDropletType => splashDropletType;
    public int SplashDropletCount => splashDropletCount;
    public bool IsCompound => isCompound;
    public BallElement[] Elements => elements;
    public BallElement[] StrongAgainst => strongAgainst;

    public bool MatchesRecipe(BallElement firstElement, BallElement secondElement)
    {
        return (primarySourceElement == firstElement && secondarySourceElement == secondElement)
            || (primarySourceElement == secondElement && secondarySourceElement == firstElement);
    }

    public bool IsStrongAgainst(BallElement brickType)
    {
        for (int i = 0; i < strongAgainst.Length; i++)
        {
            if (strongAgainst[i] == brickType)
            {
                return true;
            }
        }

        return false;
    }

    // Creates a runtime compound that inherits the abilities of both source balls.
    public void InitializeAsCompound(BallTypeData a, BallTypeData b)
    {
        displayName = $"{a.DisplayName} + {b.DisplayName}";
        description = $"A compound of {a.DisplayName} and {b.DisplayName}. Inherits the abilities of both.";
        displayColor = Color.Lerp(a.DisplayColor, b.DisplayColor, 0.5f);
        ballSprite = a.BallSprite;
        size = (a.Size + b.Size) * 0.5f;
        movementSpeed = (a.MovementSpeed + b.MovementSpeed) * 0.5f;
        damage = Mathf.Max(a.Damage, b.Damage);
        // -1 means infinite; if either has infinite bounces the compound does too
        bounces = (a.Bounces < 0 || b.Bounces < 0) ? -1 : Mathf.Max(a.Bounces, b.Bounces);
        passThroughBricks = a.PassThroughBricks || b.PassThroughBricks;
        passThroughBalls = a.PassThroughBalls || b.PassThroughBalls;
        directionRestraint = a.MovementRestraint != DirectionRestraint.None ? a.MovementRestraint : b.MovementRestraint;
        destroyOnWall = a.DestroyOnWall || b.DestroyOnWall;

        appliesBurn = a.AppliesBurn || b.AppliesBurn;
        if (appliesBurn)
        {
            burnDamage = Mathf.Max(a.AppliesBurn ? a.BurnDamage : 0, b.AppliesBurn ? b.BurnDamage : 0);
            burnTickInterval = Mathf.Min(
                a.AppliesBurn ? a.BurnTickInterval : float.MaxValue,
                b.AppliesBurn ? b.BurnTickInterval : float.MaxValue);
            burnHitCount = Mathf.Max(a.AppliesBurn ? a.BurnHitCount : 0, b.AppliesBurn ? b.BurnHitCount : 0);
        }

        lightningBurst = a.LightningBurst || b.LightningBurst;
        if (lightningBurst)
        {
            lightningBurstTargetCount = Mathf.Max(
                a.LightningBurst ? a.LightningBurstTargetCount : 0,
                b.LightningBurst ? b.LightningBurstTargetCount : 0);
            lightningBurstDamage = Mathf.Max(
                a.LightningBurst ? a.LightningBurstDamage : 0,
                b.LightningBurst ? b.LightningBurstDamage : 0);
            lightningBurstRadius = Mathf.Max(
                a.LightningBurst ? a.LightningBurstRadius : 0f,
                b.LightningBurst ? b.LightningBurstRadius : 0f);
        }

        createsLightningSnake = a.CreatesLightningSnake || b.CreatesLightningSnake;
        if (createsLightningSnake)
        {
            lightningSnakeBounceCount = Mathf.Max(
                a.CreatesLightningSnake ? a.LightningSnakeBounceCount : 0,
                b.CreatesLightningSnake ? b.LightningSnakeBounceCount : 0);
            lightningSnakeDamage = Mathf.Max(
                a.CreatesLightningSnake ? a.LightningSnakeDamage : 0,
                b.CreatesLightningSnake ? b.LightningSnakeDamage : 0);
            lightningSnakeRadius = Mathf.Max(
                a.CreatesLightningSnake ? a.LightningSnakeRadius : 0f,
                b.CreatesLightningSnake ? b.LightningSnakeRadius : 0f);
            lightningSnakeWaterSplitCount = Mathf.Max(
                a.CreatesLightningSnake ? a.LightningSnakeWaterSplitCount : 0,
                b.CreatesLightningSnake ? b.LightningSnakeWaterSplitCount : 0);
            lightningSnakeBounceDelay = Mathf.Min(
                a.CreatesLightningSnake ? a.LightningSnakeBounceDelay : float.MaxValue,
                b.CreatesLightningSnake ? b.LightningSnakeBounceDelay : float.MaxValue);
        }

        earthCrack = a.EarthCrack || b.EarthCrack;
        if (earthCrack)
        {
            shatterDamage = Mathf.Max(
                a.EarthCrack ? a.ShatterDamage : 0,
                b.EarthCrack ? b.ShatterDamage : 0);
            shatterRadius = Mathf.Max(
                a.EarthCrack ? a.ShatterRadius : 0f,
                b.EarthCrack ? b.ShatterRadius : 0f);
        }

        createsTremor = a.CreatesTremor || b.CreatesTremor;
        if (createsTremor)
        {
            tremorCrackDamage = Mathf.Max(
                a.CreatesTremor ? a.TremorCrackDamage : 0,
                b.CreatesTremor ? b.TremorCrackDamage : 0);
            tremorCrackRadius = Mathf.Max(
                a.CreatesTremor ? a.TremorCrackRadius : 0f,
                b.CreatesTremor ? b.TremorCrackRadius : 0f);
        }

        createsAbrasion = a.CreatesAbrasion || b.CreatesAbrasion;
        if (createsAbrasion)
        {
            abrasionWeakenDuration = Mathf.Max(
                a.CreatesAbrasion ? a.AbrasionWeakenDuration : 0f,
                b.CreatesAbrasion ? b.AbrasionWeakenDuration : 0f);
        }

        createsCyclone = a.CreatesCyclone || b.CreatesCyclone;
        if (createsCyclone)
        {
            cycloneFollowUpHitCount = Mathf.Max(
                a.CreatesCyclone ? a.CycloneFollowUpHitCount : 0,
                b.CreatesCyclone ? b.CycloneFollowUpHitCount : 0);
            cycloneHitDelay = Mathf.Min(
                a.CreatesCyclone ? a.CycloneHitDelay : float.MaxValue,
                b.CreatesCyclone ? b.CycloneHitDelay : float.MaxValue);
            cycloneCurveStrength = Mathf.Max(
                a.CreatesCyclone ? a.CycloneCurveStrength : 0f,
                b.CreatesCyclone ? b.CycloneCurveStrength : 0f);
        }

        appliesRoot = a.AppliesRoot || b.AppliesRoot;
        if (appliesRoot)
        {
            rootDuration = Mathf.Max(
                a.AppliesRoot ? a.RootDuration : 0f,
                b.AppliesRoot ? b.RootDuration : 0f);
            rootSpeedMultiplier = Mathf.Min(
                a.AppliesRoot ? a.RootSpeedMultiplier : 1f,
                b.AppliesRoot ? b.RootSpeedMultiplier : 1f);
        }

        createsSeed = a.CreatesSeed || b.CreatesSeed;
        if (createsSeed)
        {
            seedRootDuration = Mathf.Max(
                a.CreatesSeed ? a.SeedRootDuration : 0f,
                b.CreatesSeed ? b.SeedRootDuration : 0f);
            seedRootSpeedMultiplier = Mathf.Min(
                a.CreatesSeed ? a.SeedRootSpeedMultiplier : 1f,
                b.CreatesSeed ? b.SeedRootSpeedMultiplier : 1f);
            seedSpreadRadius = Mathf.Max(
                a.CreatesSeed ? a.SeedSpreadRadius : 0f,
                b.CreatesSeed ? b.SeedSpreadRadius : 0f);
            seedSpreadCount = Mathf.Max(
                a.CreatesSeed ? a.SeedSpreadCount : 0,
                b.CreatesSeed ? b.SeedSpreadCount : 0);
            seedSpreadGenerations = Mathf.Max(
                a.CreatesSeed ? a.SeedSpreadGenerations : 0,
                b.CreatesSeed ? b.SeedSpreadGenerations : 0);
        }

        createsCombustion = a.CreatesCombustion || b.CreatesCombustion;
        if (createsCombustion)
        {
            combustionBurnDamage = Mathf.Max(
                a.CreatesCombustion ? a.CombustionBurnDamage : 0,
                b.CreatesCombustion ? b.CombustionBurnDamage : 0);
            combustionBurnTickInterval = Mathf.Min(
                a.CreatesCombustion ? a.CombustionBurnTickInterval : float.MaxValue,
                b.CreatesCombustion ? b.CombustionBurnTickInterval : float.MaxValue);
            combustionBurnHitCount = Mathf.Max(
                a.CreatesCombustion ? a.CombustionBurnHitCount : 0,
                b.CreatesCombustion ? b.CombustionBurnHitCount : 0);
            combustionExplosionDamage = Mathf.Max(
                a.CreatesCombustion ? a.CombustionExplosionDamage : 0,
                b.CreatesCombustion ? b.CombustionExplosionDamage : 0);
            combustionExplosionRadius = Mathf.Max(
                a.CreatesCombustion ? a.CombustionExplosionRadius : 0f,
                b.CreatesCombustion ? b.CombustionExplosionRadius : 0f);
        }

        createsFireSpread = a.CreatesFireSpread || b.CreatesFireSpread;
        if (createsFireSpread)
        {
            fireSpreadRadius = Mathf.Max(
                a.CreatesFireSpread ? a.FireSpreadRadius : 0f,
                b.CreatesFireSpread ? b.FireSpreadRadius : 0f);
            fireSpreadCooldown = Mathf.Max(
                a.CreatesFireSpread ? a.FireSpreadCooldown : 0f,
                b.CreatesFireSpread ? b.FireSpreadCooldown : 0f);
            fireSpreadBonusBurnDamage = Mathf.Max(
                a.CreatesFireSpread ? a.FireSpreadBonusBurnDamage : 0,
                b.CreatesFireSpread ? b.FireSpreadBonusBurnDamage : 0);
            fireSpreadBurnSpeedMultiplier = Mathf.Max(
                a.CreatesFireSpread ? a.FireSpreadBurnSpeedMultiplier : 1f,
                b.CreatesFireSpread ? b.FireSpreadBurnSpeedMultiplier : 1f);
            fireSpreadBurnHitCountBonus = Mathf.Max(
                a.CreatesFireSpread ? a.FireSpreadBurnHitCountBonus : 0,
                b.CreatesFireSpread ? b.FireSpreadBurnHitCountBonus : 0);
        }

        createsForestFire = a.CreatesForestFire || b.CreatesForestFire;
        if (createsForestFire)
        {
            forestFireBurnDamage = Mathf.Max(
                a.CreatesForestFire ? a.ForestFireBurnDamage : 0,
                b.CreatesForestFire ? b.ForestFireBurnDamage : 0);
            forestFireBurnTickInterval = Mathf.Min(
                a.CreatesForestFire ? a.ForestFireBurnTickInterval : float.MaxValue,
                b.CreatesForestFire ? b.ForestFireBurnTickInterval : float.MaxValue);
            forestFireBurnHitCount = Mathf.Max(
                a.CreatesForestFire ? a.ForestFireBurnHitCount : 0,
                b.CreatesForestFire ? b.ForestFireBurnHitCount : 0);
            forestFireSpreadGenerations = Mathf.Max(
                a.CreatesForestFire ? a.ForestFireSpreadGenerations : 0,
                b.CreatesForestFire ? b.ForestFireSpreadGenerations : 0);
        }

        createsWaterDrops = a.CreatesWaterDrops || b.CreatesWaterDrops;
        if (createsWaterDrops)
        {
            BallTypeData waterSource = a.CreatesWaterDrops ? a : b;
            waterDropletType = waterSource.WaterDropletType;
            waterDropCooldown = Mathf.Min(
                a.CreatesWaterDrops ? a.WaterDropCooldown : float.MaxValue,
                b.CreatesWaterDrops ? b.WaterDropCooldown : float.MaxValue);
        }

        createsFlameTrail = a.CreatesFlameTrail || b.CreatesFlameTrail;
        if (createsFlameTrail)
        {
            BallTypeData flameSource = a.CreatesFlameTrail ? a : b;
            flameTrailSprite = flameSource.FlameTrailSprite != null ? flameSource.FlameTrailSprite : flameSource.BallSprite;
            flameTrailColor = Color.Lerp(a.FlameTrailColor, b.FlameTrailColor, 0.5f);
            flameTrailSizeMultiplier = Mathf.Max(
                a.CreatesFlameTrail ? a.FlameTrailSizeMultiplier : 0f,
                b.CreatesFlameTrail ? b.FlameTrailSizeMultiplier : 0f);
            flameTrailSpawnInterval = Mathf.Min(
                a.CreatesFlameTrail ? a.FlameTrailSpawnInterval : float.MaxValue,
                b.CreatesFlameTrail ? b.FlameTrailSpawnInterval : float.MaxValue);
            flameTrailRiseSpeed = Mathf.Max(
                a.CreatesFlameTrail ? a.FlameTrailRiseSpeed : 0f,
                b.CreatesFlameTrail ? b.FlameTrailRiseSpeed : 0f);
            flameTrailLifetime = Mathf.Max(
                a.CreatesFlameTrail ? a.FlameTrailLifetime : 0f,
                b.CreatesFlameTrail ? b.FlameTrailLifetime : 0f);
            flameTrailImpactDamage = Mathf.Max(
                a.CreatesFlameTrail ? a.FlameTrailImpactDamage : 0,
                b.CreatesFlameTrail ? b.FlameTrailImpactDamage : 0);
            flameTrailBurnDamage = Mathf.Max(
                a.CreatesFlameTrail ? a.FlameTrailBurnDamage : 0,
                b.CreatesFlameTrail ? b.FlameTrailBurnDamage : 0);
            flameTrailBurnTickInterval = Mathf.Min(
                a.CreatesFlameTrail ? a.FlameTrailBurnTickInterval : float.MaxValue,
                b.CreatesFlameTrail ? b.FlameTrailBurnTickInterval : float.MaxValue);
            flameTrailBurnHitCount = Mathf.Max(
                a.CreatesFlameTrail ? a.FlameTrailBurnHitCount : 0,
                b.CreatesFlameTrail ? b.FlameTrailBurnHitCount : 0);
        }

        createsFertileLand = a.CreatesFertileLand || b.CreatesFertileLand;
        if (createsFertileLand)
        {
            BallTypeData fertileSource = a.CreatesFertileLand ? a : b;
            fertilePatchSprite = fertileSource.FertilePatchSprite != null ? fertileSource.FertilePatchSprite : fertileSource.BallSprite;
            fertilePatchColor = Color.Lerp(a.FertilePatchColor, b.FertilePatchColor, 0.5f);
            fertilePatchSizeMultiplier = Mathf.Max(
                a.CreatesFertileLand ? a.FertilePatchSizeMultiplier : 0f,
                b.CreatesFertileLand ? b.FertilePatchSizeMultiplier : 0f);
            fertilePatchSpawnInterval = Mathf.Min(
                a.CreatesFertileLand ? a.FertilePatchSpawnInterval : float.MaxValue,
                b.CreatesFertileLand ? b.FertilePatchSpawnInterval : float.MaxValue);
            fertilePatchRiseSpeed = Mathf.Max(
                a.CreatesFertileLand ? a.FertilePatchRiseSpeed : 0f,
                b.CreatesFertileLand ? b.FertilePatchRiseSpeed : 0f);
            fertilePatchLifetime = Mathf.Max(
                a.CreatesFertileLand ? a.FertilePatchLifetime : 0f,
                b.CreatesFertileLand ? b.FertilePatchLifetime : 0f);
            fertilePatchCrackShatterDamage = Mathf.Max(
                a.CreatesFertileLand ? a.FertilePatchCrackShatterDamage : 0,
                b.CreatesFertileLand ? b.FertilePatchCrackShatterDamage : 0);
            fertilePatchCrackShatterRadius = Mathf.Max(
                a.CreatesFertileLand ? a.FertilePatchCrackShatterRadius : 0f,
                b.CreatesFertileLand ? b.FertilePatchCrackShatterRadius : 0f);
            fertilePatchRootRadius = Mathf.Max(
                a.CreatesFertileLand ? a.FertilePatchRootRadius : 0f,
                b.CreatesFertileLand ? b.FertilePatchRootRadius : 0f);
            fertilePatchRootDuration = Mathf.Max(
                a.CreatesFertileLand ? a.FertilePatchRootDuration : 0f,
                b.CreatesFertileLand ? b.FertilePatchRootDuration : 0f);
            fertilePatchRootSpeedMultiplier = Mathf.Min(
                a.CreatesFertileLand ? a.FertilePatchRootSpeedMultiplier : 1f,
                b.CreatesFertileLand ? b.FertilePatchRootSpeedMultiplier : 1f);
        }

        createsSteamBurst = a.CreatesSteamBurst || b.CreatesSteamBurst;
        if (createsSteamBurst)
        {
            steamBurstBallCount = Mathf.Max(
                a.CreatesSteamBurst ? a.SteamBurstBallCount : 0,
                b.CreatesSteamBurst ? b.SteamBurstBallCount : 0);
            steamBurstBallType = a.CreatesSteamBurst ? a.SteamBurstBallType : b.SteamBurstBallType;
            if (steamBurstBallType == null)
            {
                steamBurstBallType = a.SteamBurstBallType != null ? a.SteamBurstBallType : b.SteamBurstBallType;
            }
            steamBurstMinInterval = Mathf.Min(
                a.CreatesSteamBurst ? a.SteamBurstMinInterval : float.MaxValue,
                b.CreatesSteamBurst ? b.SteamBurstMinInterval : float.MaxValue);
            steamBurstMaxInterval = Mathf.Max(
                a.CreatesSteamBurst ? a.SteamBurstMaxInterval : 0f,
                b.CreatesSteamBurst ? b.SteamBurstMaxInterval : 0f);
            steamBurstSpawnRadius = Mathf.Max(
                a.CreatesSteamBurst ? a.SteamBurstSpawnRadius : 0f,
                b.CreatesSteamBurst ? b.SteamBurstSpawnRadius : 0f);
            steamBurstSpeedMultiplier = Mathf.Max(
                a.CreatesSteamBurst ? a.SteamBurstSpeedMultiplier : 1f,
                b.CreatesSteamBurst ? b.SteamBurstSpeedMultiplier : 1f);
            steamBurstSpeedLerpDuration = Mathf.Max(
                a.CreatesSteamBurst ? a.SteamBurstSpeedLerpDuration : 0f,
                b.CreatesSteamBurst ? b.SteamBurstSpeedLerpDuration : 0f);

            if (steamBurstMaxInterval < steamBurstMinInterval)
            {
                steamBurstMaxInterval = steamBurstMinInterval;
            }
        }

        impactBurst = a.ImpactBurst || b.ImpactBurst;
        if (impactBurst)
        {
            impactBurstDamage = Mathf.Max(
                a.ImpactBurst ? a.ImpactBurstDamage : 0,
                b.ImpactBurst ? b.ImpactBurstDamage : 0);
            impactBurstRadius = Mathf.Max(
                a.ImpactBurst ? a.ImpactBurstRadius : 0f,
                b.ImpactBurst ? b.ImpactBurstRadius : 0f);
        }

        createsCollapse = a.CreatesCollapse || b.CreatesCollapse;
        if (createsCollapse)
        {
            collapseRadius = Mathf.Max(
                a.CreatesCollapse ? a.CollapseRadius : 0f,
                b.CreatesCollapse ? b.CollapseRadius : 0f);
            collapseDuration = Mathf.Max(
                a.CreatesCollapse ? a.CollapseDuration : 0f,
                b.CreatesCollapse ? b.CollapseDuration : 0f);
        }

        createsLinearProjectile = a.CreatesLinearProjectile || b.CreatesLinearProjectile;
        if (createsLinearProjectile)
        {
            linearProjectileType = a.LinearProjectileType != null ? a.LinearProjectileType : b.LinearProjectileType;
            linearProjectileIncludesTopWall = a.LinearProjectileIncludesTopWall || b.LinearProjectileIncludesTopWall;
        }

        createsBlackout = a.CreatesBlackout || b.CreatesBlackout;
        if (createsBlackout)
        {
            blackoutDamage = Mathf.Max(
                a.CreatesBlackout ? a.BlackoutDamage : 0,
                b.CreatesBlackout ? b.BlackoutDamage : 0);
            blackoutInterval = Mathf.Min(
                a.CreatesBlackout ? a.BlackoutInterval : float.MaxValue,
                b.CreatesBlackout ? b.BlackoutInterval : float.MaxValue);
        }

        createsFirstAid = a.CreatesFirstAid || b.CreatesFirstAid;
        if (createsFirstAid)
        {
            firstAidHealPerHit = Mathf.Max(
                a.CreatesFirstAid ? a.FirstAidHealPerHit : 0,
                b.CreatesFirstAid ? b.FirstAidHealPerHit : 0);
            firstAidHealThreshold = Mathf.Min(
                a.CreatesFirstAid ? a.FirstAidHealThreshold : int.MaxValue,
                b.CreatesFirstAid ? b.FirstAidHealThreshold : int.MaxValue);
            firstAidExplosionDamage = Mathf.Max(
                a.CreatesFirstAid ? a.FirstAidExplosionDamage : 0,
                b.CreatesFirstAid ? b.FirstAidExplosionDamage : 0);
            firstAidExplosionRadius = Mathf.Max(
                a.CreatesFirstAid ? a.FirstAidExplosionRadius : 0f,
                b.CreatesFirstAid ? b.FirstAidExplosionRadius : 0f);
        }

        createsElectricCascade = a.CreatesElectricCascade || b.CreatesElectricCascade;
        if (createsElectricCascade)
        {
            electricCascadeShockDamage = Mathf.Max(
                a.CreatesElectricCascade ? a.ElectricCascadeShockDamage : 0,
                b.CreatesElectricCascade ? b.ElectricCascadeShockDamage : 0);
            electricCascadeConductiveDuration = Mathf.Max(
                a.CreatesElectricCascade ? a.ElectricCascadeConductiveDuration : 0f,
                b.CreatesElectricCascade ? b.ElectricCascadeConductiveDuration : 0f);
        }

        createsRollingThunder = a.CreatesRollingThunder || b.CreatesRollingThunder;
        if (createsRollingThunder)
        {
            rollingThunderStartScaleMultiplier = Mathf.Min(
                a.CreatesRollingThunder ? a.RollingThunderStartScaleMultiplier : float.MaxValue,
                b.CreatesRollingThunder ? b.RollingThunderStartScaleMultiplier : float.MaxValue);
            rollingThunderMaxScaleMultiplier = Mathf.Max(
                a.CreatesRollingThunder ? a.RollingThunderMaxScaleMultiplier : 0f,
                b.CreatesRollingThunder ? b.RollingThunderMaxScaleMultiplier : 0f);
            rollingThunderGrowthAmount = Mathf.Max(
                a.CreatesRollingThunder ? a.RollingThunderGrowthAmount : 0f,
                b.CreatesRollingThunder ? b.RollingThunderGrowthAmount : 0f);
            rollingThunderSpawnBallType = a.RollingThunderSpawnBallType != null
                ? a.RollingThunderSpawnBallType
                : b.RollingThunderSpawnBallType;
            rollingThunderMinLaunchAngle = Mathf.Min(
                a.CreatesRollingThunder ? a.RollingThunderMinLaunchAngle : float.MaxValue,
                b.CreatesRollingThunder ? b.RollingThunderMinLaunchAngle : float.MaxValue);
            rollingThunderMaxLaunchAngle = Mathf.Max(
                a.CreatesRollingThunder ? a.RollingThunderMaxLaunchAngle : float.MinValue,
                b.CreatesRollingThunder ? b.RollingThunderMaxLaunchAngle : float.MinValue);

            if (rollingThunderMaxScaleMultiplier < rollingThunderStartScaleMultiplier)
            {
                rollingThunderMaxScaleMultiplier = rollingThunderStartScaleMultiplier;
            }

            if (rollingThunderMaxLaunchAngle < rollingThunderMinLaunchAngle)
            {
                rollingThunderMaxLaunchAngle = rollingThunderMinLaunchAngle;
            }
        }

        createsShockTherapy = a.CreatesShockTherapy || b.CreatesShockTherapy;
        if (createsShockTherapy)
        {
            shockTherapyMinTargets = Mathf.Min(
                a.CreatesShockTherapy ? a.ShockTherapyMinTargets : int.MaxValue,
                b.CreatesShockTherapy ? b.ShockTherapyMinTargets : int.MaxValue);
            shockTherapyMaxTargets = Mathf.Max(
                a.CreatesShockTherapy ? a.ShockTherapyMaxTargets : 0,
                b.CreatesShockTherapy ? b.ShockTherapyMaxTargets : 0);
            shockTherapyDamage = Mathf.Max(
                a.CreatesShockTherapy ? a.ShockTherapyDamage : 0,
                b.CreatesShockTherapy ? b.ShockTherapyDamage : 0);
            shockTherapyHealAmount = Mathf.Max(
                a.CreatesShockTherapy ? a.ShockTherapyHealAmount : 0,
                b.CreatesShockTherapy ? b.ShockTherapyHealAmount : 0);

            if (shockTherapyMaxTargets < shockTherapyMinTargets)
            {
                shockTherapyMaxTargets = shockTherapyMinTargets;
            }
        }

        createsPressurizedSplash = a.CreatesPressurizedSplash || b.CreatesPressurizedSplash;
        if (createsPressurizedSplash)
        {
            pressurePerHit = Mathf.Max(
                a.CreatesPressurizedSplash ? a.PressurePerHit : 0,
                b.CreatesPressurizedSplash ? b.PressurePerHit : 0);
            maxPressure = Mathf.Max(
                a.CreatesPressurizedSplash ? a.MaxPressure : 0,
                b.CreatesPressurizedSplash ? b.MaxPressure : 0);
            splashDropletCount = Mathf.Max(
                a.CreatesPressurizedSplash ? a.SplashDropletCount : 0,
                b.CreatesPressurizedSplash ? b.SplashDropletCount : 0);
            splashDropletType = a.SplashDropletType != null ? a.SplashDropletType : b.SplashDropletType;
        }

        isCompound = true;

        // Elements: union of both
        var elementSet = new System.Collections.Generic.HashSet<BallElement>();
        if (a.Elements != null)
            foreach (var e in a.Elements) elementSet.Add(e);
        if (b.Elements != null)
            foreach (var e in b.Elements) elementSet.Add(e);
        elements = new BallElement[elementSet.Count];
        elementSet.CopyTo(elements);

        // StrongAgainst: union of both
        var strongSet = new System.Collections.Generic.HashSet<BallElement>();
        if (a.StrongAgainst != null)
            foreach (var e in a.StrongAgainst) strongSet.Add(e);
        if (b.StrongAgainst != null)
            foreach (var e in b.StrongAgainst) strongSet.Add(e);
        strongAgainst = new BallElement[strongSet.Count];
        strongSet.CopyTo(strongAgainst);

        primarySourceElement = a.Elements is { Length: > 0 } ? a.Elements[0] : BallElement.Basic;
        secondarySourceElement = b.Elements is { Length: > 0 } ? b.Elements[0] : BallElement.Basic;
    }
}
