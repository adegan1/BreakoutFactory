using UnityEngine;

[CreateAssetMenu(fileName = "New Brick Type", menuName = "Breakout/Brick Type Data")]
public class BrickTypeData : ScriptableObject
{
    // Core Properties
    [SerializeField] private int hitPoints = 1;
    [SerializeField] private Color displayColor = Color.white;
    [SerializeField] private int scoreValue = 10;
    [SerializeField] private bool flammable = false;
    [SerializeField] private bool fireResistant = false;
    [SerializeField] private bool amplifiesLightning = false;
    [SerializeField, Min(0)] private int lightningTargetBonus = 1;
    [SerializeField, Min(0)] private int damageToPlayer = 1;

    // Wind Interaction
    [SerializeField] private float windFireBurstRadius = 2f;
    [SerializeField, Min(1)] private int windFireBurstDamage = 1;
    [SerializeField] private Color windFireBurstColor = new Color(1f, 0.4f, 0.05f, 1f);
    [SerializeField] private float windFireBurstWidth = 0.06f;
    [SerializeField] private float windFireBurstRayLength = 1.5f;
    [SerializeField] private float windFireBurstLifetime = 0.35f;
    [SerializeField, Min(1)] private int windFireBurstRayCount = 8;

    // Fire Interaction
    [SerializeField] private float steamWeakenRadius = 2f;
    [SerializeField] private float steamWeakenDuration = 3f;
    [SerializeField] private Color steamColor = new Color(0.75f, 0.9f, 1f, 0.9f);
    [SerializeField] private float steamWidth = 0.08f;
    [SerializeField] private float steamRingRadius = 1.8f;
    [SerializeField] private float steamLifetime = 0.8f;

    // Life Interaction
    [SerializeField] private float lifeRootSearchRadius = 3f;
    [SerializeField] private float lifeRootDuration = 3f;
    [SerializeField, Range(0f, 1f)] private float lifeRootSpeedMultiplier = 0.1f;
    [SerializeField] private Color vineColor = new Color(0.2f, 0.8f, 0.25f, 1f);
    [SerializeField] private float vineWidth = 0.07f;
    [SerializeField] private float vineGrowDuration = 0.35f;
    [SerializeField] private float vineHoldDuration = 0.25f;
    [SerializeField] private float vineFadeDuration = 0.3f;

    // Earth+Lightning Interaction (Lightning brick hit by Earth ball — strikes one random nearby brick on fire)
    [SerializeField] private float earthLightningStrikeRadius = 3f;
    [SerializeField] private int earthLightningStrikeDamage = 2;
    [SerializeField] private int earthLightningStrikeBurnDamage = 1;
    [SerializeField] private float earthLightningStrikeBurnInterval = 0.5f;
    [SerializeField] private int earthLightningStrikeBurnHitCount = 5;
    [SerializeField] private Color earthLightningBoltColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private float earthLightningBoltWidth = 0.07f;
    [SerializeField] private float earthLightningBoltLifetime = 0.35f;
    [SerializeField, Min(2)] private int earthLightningBoltSegments = 12;
    [SerializeField, Min(0f)] private float earthLightningBoltNoise = 0.15f;

    // Wind+Lightning Interaction (Lightning brick hit by Wind ball — grants chain lightning to the ball)
    [SerializeField] private float windChainLightningDuration = 5f;
    [SerializeField] private int windChainLightningDamage = 1;
    [SerializeField] private float windChainLightningRadius = 3f;
    [SerializeField] private Color windChainLightningBoltColor = new Color(0.7f, 0.88f, 1f, 1f);
    [SerializeField] private float windChainLightningBoltWidth = 0.05f;
    [SerializeField] private float windChainLightningBoltLifetime = 0.2f;
    [SerializeField, Min(2)] private int windChainLightningBoltSegments = 8;
    [SerializeField, Min(0f)] private float windChainLightningBoltNoise = 0.1f;

    // Lightning+Earth Interaction (Earth brick hit by Lightning ball — periodically cracks random nearby bricks)
    [SerializeField] private float lightningCrackFieldDuration = 4f;
    [SerializeField] private float lightningCrackFieldTickInterval = 0.6f;
    [SerializeField] private int lightningCrackFieldCrackDamage = 1;
    [SerializeField] private float lightningCrackFieldCrackRadius = 1.5f;
    [SerializeField] private float lightningCrackFieldSearchRadius = 3f;
    [SerializeField, Min(1)] private int lightningCrackFieldTargetCount = 2;
    [SerializeField] private Color lightningCrackFieldBoltColor = new Color(0.6f, 0.85f, 0.5f, 1f);
    [SerializeField] private float lightningCrackFieldBoltWidth = 0.06f;
    [SerializeField] private float lightningCrackFieldBoltLifetime = 0.3f;
    [SerializeField, Min(2)] private int lightningCrackFieldBoltSegments = 10;
    [SerializeField, Min(0f)] private float lightningCrackFieldBoltNoise = 0.18f;

    // Earth+Life Interaction (Earth brick hit by Life ball — roots a random nearby brick)
    [SerializeField] private float earthLifeRootSearchRadius = 3f;
    [SerializeField] private float earthLifeRootDuration = 3f;
    [SerializeField, Range(0f, 1f)] private float earthLifeRootSpeedMultiplier = 0.1f;

    // Type
    [SerializeField] private BallTypeData.BallElement type = BallTypeData.BallElement.Basic;

    public int HitPoints => hitPoints;
    public Color DisplayColor => displayColor;
    public int ScoreValue => scoreValue;
    public bool Flammable => flammable;
    public bool FireResistant => fireResistant;
    public bool AmplifiesLightning => amplifiesLightning;
    public int LightningTargetBonus => lightningTargetBonus;
    public int DamageToPlayer => damageToPlayer;
    public BallTypeData.BallElement Type => type;

    public float WindFireBurstRadius => windFireBurstRadius;
    public int WindFireBurstDamage => windFireBurstDamage;
    public Color WindFireBurstColor => windFireBurstColor;
    public float WindFireBurstWidth => windFireBurstWidth;
    public float WindFireBurstRayLength => windFireBurstRayLength;
    public float WindFireBurstLifetime => windFireBurstLifetime;
    public int WindFireBurstRayCount => windFireBurstRayCount;

    public float SteamWeakenRadius => steamWeakenRadius;
    public float SteamWeakenDuration => steamWeakenDuration;
    public Color SteamColor => steamColor;
    public float SteamWidth => steamWidth;
    public float SteamRingRadius => steamRingRadius;
    public float SteamLifetime => steamLifetime;

    public float LifeRootSearchRadius => lifeRootSearchRadius;
    public float LifeRootDuration => lifeRootDuration;
    public float LifeRootSpeedMultiplier => lifeRootSpeedMultiplier;
    public Color VineColor => vineColor;
    public float VineWidth => vineWidth;
    public float VineGrowDuration => vineGrowDuration;
    public float VineHoldDuration => vineHoldDuration;
    public float VineFadeDuration => vineFadeDuration;

    public float EarthLightningStrikeRadius => earthLightningStrikeRadius;
    public int EarthLightningStrikeDamage => earthLightningStrikeDamage;
    public int EarthLightningStrikeBurnDamage => earthLightningStrikeBurnDamage;
    public float EarthLightningStrikeBurnInterval => earthLightningStrikeBurnInterval;
    public int EarthLightningStrikeBurnHitCount => earthLightningStrikeBurnHitCount;
    public Color EarthLightningBoltColor => earthLightningBoltColor;
    public float EarthLightningBoltWidth => earthLightningBoltWidth;
    public float EarthLightningBoltLifetime => earthLightningBoltLifetime;
    public int EarthLightningBoltSegments => earthLightningBoltSegments;
    public float EarthLightningBoltNoise => earthLightningBoltNoise;

    public float WindChainLightningDuration => windChainLightningDuration;
    public int WindChainLightningDamage => windChainLightningDamage;
    public float WindChainLightningRadius => windChainLightningRadius;
    public Color WindChainLightningBoltColor => windChainLightningBoltColor;
    public float WindChainLightningBoltWidth => windChainLightningBoltWidth;
    public float WindChainLightningBoltLifetime => windChainLightningBoltLifetime;
    public int WindChainLightningBoltSegments => windChainLightningBoltSegments;
    public float WindChainLightningBoltNoise => windChainLightningBoltNoise;

    public float LightningCrackFieldDuration => lightningCrackFieldDuration;
    public float LightningCrackFieldTickInterval => lightningCrackFieldTickInterval;
    public int LightningCrackFieldCrackDamage => lightningCrackFieldCrackDamage;
    public float LightningCrackFieldCrackRadius => lightningCrackFieldCrackRadius;
    public float LightningCrackFieldSearchRadius => lightningCrackFieldSearchRadius;
    public int LightningCrackFieldTargetCount => lightningCrackFieldTargetCount;
    public Color LightningCrackFieldBoltColor => lightningCrackFieldBoltColor;
    public float LightningCrackFieldBoltWidth => lightningCrackFieldBoltWidth;
    public float LightningCrackFieldBoltLifetime => lightningCrackFieldBoltLifetime;
    public int LightningCrackFieldBoltSegments => lightningCrackFieldBoltSegments;
    public float LightningCrackFieldBoltNoise => lightningCrackFieldBoltNoise;

    public float EarthLifeRootSearchRadius => earthLifeRootSearchRadius;
    public float EarthLifeRootDuration => earthLifeRootDuration;
    public float EarthLifeRootSpeedMultiplier => earthLifeRootSpeedMultiplier;
}
