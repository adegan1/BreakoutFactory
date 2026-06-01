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
}
