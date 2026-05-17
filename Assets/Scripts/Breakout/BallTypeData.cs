using UnityEngine;

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
        Chain,
        LightningSnake,
        Crack,
        FlameTrail,
        FireSpread,
        SteamBurst,
        Root,
        WaterDrops,
        Pierce
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
    [SerializeField] private bool appliesRoot = false;
    [SerializeField, Min(0.1f)] private float rootDuration = 2f;
    [SerializeField, Range(0f, 1f)] private float rootSpeedMultiplier = 0.6f;
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
    public bool AppliesRoot => appliesRoot;
    public float RootDuration => rootDuration;
    public float RootSpeedMultiplier => rootSpeedMultiplier;
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

            // Fire spread needs pierce-like traversal to trigger across multiple bricks.
            passThroughBricks = true;
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
