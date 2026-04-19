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

    // Visual
    [SerializeField] private Color displayColor = Color.white;
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
    [SerializeField, Range(0.1f, 1f)] private float rootSpeedMultiplier = 0.6f;
    [SerializeField] private bool createsWaterDrops = false;
    [SerializeField] private BallTypeData waterDropletType;
    [SerializeField, Min(0.01f)] private float waterDropCooldown = 0.08f;

    // Elements
    [SerializeField] private BallElement[] elements = new BallElement[] { BallElement.Basic };

    // Strong Against...
    [SerializeField] private BallElement[] strongAgainst = new BallElement[0];

    public Color DisplayColor => displayColor;
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
    public BallElement[] Elements => elements;
    public BallElement[] StrongAgainst => strongAgainst;

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
}
