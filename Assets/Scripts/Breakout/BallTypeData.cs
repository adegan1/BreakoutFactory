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
        Crack,
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
    [SerializeField] private bool earthCrack = false;
    [SerializeField, Min(1)] private int shatterDamage = 2;
    [SerializeField, Min(0.1f)] private float shatterRadius = 1.4f;
    [SerializeField] private bool appliesRoot = false;
    [SerializeField, Min(0.1f)] private float rootDuration = 2f;
    [SerializeField, Range(0f, 1f)] private float rootSpeedMultiplier = 0.6f;
    [SerializeField] private bool createsWaterDrops = false;
    [SerializeField] private BallTypeData waterDropletType;
    [SerializeField, Min(0.01f)] private float waterDropCooldown = 0.08f;
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
    public bool EarthCrack => earthCrack;
    public int ShatterDamage => shatterDamage;
    public float ShatterRadius => shatterRadius;
    public bool AppliesRoot => appliesRoot;
    public float RootDuration => rootDuration;
    public float RootSpeedMultiplier => rootSpeedMultiplier;
    public bool CreatesWaterDrops => createsWaterDrops;
    public BallTypeData WaterDropletType => waterDropletType;
    public float WaterDropCooldown => waterDropCooldown;
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

        createsWaterDrops = a.CreatesWaterDrops || b.CreatesWaterDrops;
        if (createsWaterDrops)
        {
            BallTypeData waterSource = a.CreatesWaterDrops ? a : b;
            waterDropletType = waterSource.WaterDropletType;
            waterDropCooldown = Mathf.Min(
                a.CreatesWaterDrops ? a.WaterDropCooldown : float.MaxValue,
                b.CreatesWaterDrops ? b.WaterDropCooldown : float.MaxValue);
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
