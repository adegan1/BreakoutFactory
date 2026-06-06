using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BrickController : MonoBehaviour
{
    private enum DamageSource
    {
        BallHit,
        Effect,
        ElectricCascade,
        Burn
    }

    private struct LightningSnakeNode
    {
        public Vector3 OriginPosition;
        public BrickController ExcludedBrick;
        public int RemainingBounces;

        public LightningSnakeNode(Vector3 originPosition, BrickController excludedBrick, int remainingBounces)
        {
            OriginPosition = originPosition;
            ExcludedBrick = excludedBrick;
            RemainingBounces = remainingBounces;
        }
    }

    [System.Serializable]
    public class OverlayAnimation
    {
        public Sprite[] frames;
        [Min(0.1f)] public float frameRate = 12f;
    }

    private const float MinimumRootSpeedMultiplier = 0.05f;
    private const float MinimumDurationSeconds = 0.01f;
    private const float MinimumEffectRadius = 0.1f;
    private const float RootColumnYThresholdOffset = 0.01f;
    private const float MinimumColumnTolerance = 0.05f;
    private const float DefaultColumnTolerance = 0.6f;
    private const float RowSpacingSafetyMultiplier = 1.1f;
    private const float DefaultRowSpacing = 1.2f;
    private const float FollowDistanceThresholdMultiplier = 1.05f;
    private const float MinimumFollowGap = 0.02f;
    private const int StrongAgainstDamageMultiplier = 2;
    private static int closestBrickBelowCacheVersion = 1;

    [SerializeField] private BrickTypeData typeData;

    [Header("Spawn Animation")]
    [SerializeField] private float growthSpeed = 6f;

    [Header("Movement")]
    [SerializeField] private bool moveDownward;
    [SerializeField] private float downwardSpeed;

    [Header("Damage Flash")]
    [SerializeField] private bool enableDamageFlash = true;
    [SerializeField] private Color damageFlashColor = new Color(1f, 0.9f, 0.9f, 1f);
    [SerializeField, Min(0.01f)] private float damageFlashDuration = 0.12f;
    [SerializeField, Range(0f, 1f)] private float damageFlashStrength = 0.9f;

    [Header("Effect Overlays")]
    [SerializeField] private Sprite crackedOverlaySprite;
    [SerializeField] private OverlayAnimation crackedOverlayAnimation;
    [SerializeField] private Sprite burningOverlaySprite;
    [SerializeField] private OverlayAnimation burningOverlayAnimation;
    [SerializeField] private Sprite rootedOverlaySprite;
    [SerializeField] private OverlayAnimation rootedOverlayAnimation;
    [SerializeField] private Sprite pressureOverlaySprite;
    [SerializeField] private OverlayAnimation pressureOverlayAnimation;
    [SerializeField] private Sprite weakenedOverlaySprite;
    [SerializeField] private OverlayAnimation weakenedOverlayAnimation;
    [SerializeField] private Sprite conductiveOverlaySprite;
    [SerializeField] private OverlayAnimation conductiveOverlayAnimation;
    [SerializeField] private Vector2 overlayScale = Vector2.one;
    [LayerSelector] [SerializeField] private int overlayLayer = 0;

    [Header("Crack Shatter Visual")]
    [SerializeField] private Color crackShatterEffectColor = new Color(0.65f, 0.58f, 0.44f, 1f);
    [SerializeField, Min(0.005f)] private float crackShatterEffectWidth = 0.07f;
    [SerializeField, Min(0.05f)] private float crackShatterEffectLifetime = 0.45f;
    [SerializeField, Range(3, 12)] private int crackShatterEffectCrackCount = 6;

    [Header("Super Effective Visual")]
    [SerializeField] private Color superEffectiveGleamColor = new Color(1f, 0.95f, 0.55f, 1f);
    [SerializeField, Min(0.01f)] private float superEffectiveGleamSize = 0.5f;
    [SerializeField, Min(0.005f)] private float superEffectiveGleamWidth = 0.04f;
    [SerializeField, Min(0.05f)] private float superEffectiveGleamLifetime = 0.18f;

    private int currentHitPoints;
    private int maxHitPoints;
    private int overrideHitPoints = -1;
    private SpriteRenderer spriteRenderer;
    private Sprite defaultBrickSprite;
    private Collider2D brickCollider;
    private Vector3 targetScale;
    private bool isGrowing;
    private bool isBurning;
    private bool isWeakened;
    private float weakenedTimeRemaining;
    private bool isCracked;
    private bool isRooted;
    private bool hasColumnSlow;
    private bool hasSeedRoot;
    private float seedRootTimeRemaining;
    private float seedRootSpeedMult = 1f;
    private int seedSpreadGenerationsRemaining;
    private int seedSpreadCount;
    private float seedSpreadRadius;
    private float seedRootDuration;
    private float seedRootBaseSpeedMult = 1f;
    private bool hasCombustion;
    private bool combustionExplosionTriggered;
    private int combustionExplosionDamage;
    private float combustionExplosionRadius;
    private float nextFireSpreadAllowedTime;
    private bool hasPressurizedSplash;
    private bool pressureBurstTriggered;
    private int currentPressure;
    private int pressureMaxThreshold;
    private BallTypeData splashDropletType;
    private int splashDropletCount;
    private BallController lastHittingBall;
    private bool hasForestFire;
    private bool hasConductive;
    private int conductiveShockDamage;
    private BallTypeData conductiveVisualData;
    private float conductiveTimeRemaining;
    private int forestFireSpreadGenerationsRemaining;
    private int crackShatterDamage = 1;
    private float crackShatterRadius = 1f;
    private bool hasLightningCrackField;
    private float lightningCrackFieldTimeRemaining;
    private float lightningCrackFieldTickTimer;
    private int burnDamage;
    private float burnTickInterval;
    private float burnTickTimer;
    private int burnHitsRemaining;
    private int burnInitialHitCount;
    private float rootTimeRemaining;
    private float rootInitialDuration;
    private float rootSpeedMultiplier = 1f;
    private float columnSlowTimeRemaining;
    private float columnSlowSpeedMultiplier = 1f;
    private float weakenedInitialDuration;
    private float conductiveInitialDuration;
    private Coroutine damageFlashRoutine;
    private float burnFlashTimer = -1f;
    private Coroutine dangerSequenceRoutine;
    private SpriteRenderer crackedOverlayRenderer;
    private SpriteRenderer burningOverlayRenderer;
    private SpriteRenderer rootedOverlayRenderer;
    private SpriteRenderer pressureOverlayRenderer;
    private SpriteRenderer weakenedOverlayRenderer;
    private SpriteRenderer conductiveOverlayRenderer;
    private readonly int[] overlayAnimFrames = new int[6];
    private readonly float[] overlayAnimTimers = new float[6];
    private readonly List<BrickController> nearbyBricksBuffer = new List<BrickController>();
    private readonly List<BrickController> crossLineBricksBuffer = new List<BrickController>();
    private readonly List<BrickController> seedSpreadCandidatesBuffer = new List<BrickController>();
    private bool inDangerSequence;
    private Vector3 dangerBasePosition;
    private BrickController cachedClosestBrickBelow;
    private int cachedClosestBrickBelowVersion = -1;
    private bool hasCachedClosestBrickBelow;
    private int cachedDownwardSpeedFrame = -1;
    private float cachedDownwardSpeed;

    public static event System.Action<BrickController, int> BrickDestroyed;
    public static event System.Action<BrickController> BrickRemovedByDanger;
    public static event System.Action<BrickController> SuperEffectiveHit;

    public int CurrentHitPoints => currentHitPoints;
    public BrickTypeData TypeData => typeData;
    public float DownwardSpeed => downwardSpeed;
    public bool IsPinnedInPlace => inDangerSequence
        || (isRooted && rootSpeedMultiplier <= MinimumRootSpeedMultiplier)
        || (hasColumnSlow && columnSlowSpeedMultiplier <= MinimumRootSpeedMultiplier)
        || (hasSeedRoot && seedRootSpeedMult <= MinimumRootSpeedMultiplier);
    public bool IsEffectivelyStopped => IsPinnedInPlace || IsBlockedBelowByStoppedBrick();

    private void Awake()
    {
        InvalidateClosestBrickBelowCache();

        targetScale = transform.localScale;
        transform.localScale = new Vector3(0f, 0f, 1f);
        isGrowing = true;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            defaultBrickSprite = spriteRenderer.sprite;
        }

        // Force 2D brick visuals by disabling legacy mesh renderers.
        MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            if (meshRenderers[i] != null)
            {
                meshRenderers[i].enabled = false;
            }
        }

        brickCollider = GetComponent<Collider2D>();
        InitializeEffectOverlays();
        ApplyTypeData();
    }

    private void Update()
    {
        UpdateSpawnGrowth();

        UpdateBurning();
        UpdateBurnFlash();
        UpdateCollapse();
        UpdateRooting();
        UpdateSeedRoot();
        UpdateColumnSlowing();
        UpdateConductive();
        UpdateLightningCrackField();
        UpdateEffectOverlays();

        float currentDownwardSpeed = GetCurrentDownwardSpeed();
        if (!moveDownward || currentDownwardSpeed <= 0f)
        {
            return;
        }

        transform.position += Vector3.down * currentDownwardSpeed * Time.deltaTime;
    }

    private void UpdateSpawnGrowth()
    {
        if (!isGrowing)
        {
            return;
        }

        if (growthSpeed <= 0f)
        {
            transform.localScale = targetScale;
            isGrowing = false;
            return;
        }

        transform.localScale = Vector3.MoveTowards(transform.localScale, targetScale, growthSpeed * Time.deltaTime);
        if (transform.localScale == targetScale)
        {
            isGrowing = false;
        }
    }

    public void SetDownwardMotion(bool enabled, float speed)
    {
        if (inDangerSequence)
        {
            moveDownward = false;
            downwardSpeed = 0f;
            InvalidateDownwardSpeedCache();
            return;
        }

        moveDownward = enabled;
        SetDownwardSpeed(speed);
    }

    public void SetDownwardSpeed(float speed)
    {
        downwardSpeed = Mathf.Max(0f, speed);
        InvalidateDownwardSpeedCache();
    }

    public bool BeginDangerSequence(float waitBeforeShakeSeconds, float shakeDurationSeconds, float shakeMagnitude)
    {
        if (inDangerSequence || currentHitPoints <= 0)
        {
            return false;
        }

        inDangerSequence = true;
        moveDownward = false;
        downwardSpeed = 0f;
        dangerBasePosition = transform.position;
        InvalidateDownwardSpeedCache();

        if (dangerSequenceRoutine != null)
        {
            StopCoroutine(dangerSequenceRoutine);
        }

        dangerSequenceRoutine = StartCoroutine(DangerSequenceCoroutine(
            Mathf.Max(0f, waitBeforeShakeSeconds),
            Mathf.Max(0f, shakeDurationSeconds),
            Mathf.Max(0f, shakeMagnitude)));
        return true;
    }

    public void SetTypeData(BrickTypeData newTypeData)
    {
        overrideHitPoints = -1;
        typeData = newTypeData;
        ApplyTypeData();
    }

    public void SetTypeData(BrickTypeData newTypeData, int brickHealth)
    {
        overrideHitPoints = brickHealth > 0 ? brickHealth : -1;
        typeData = newTypeData;
        ApplyTypeData();
    }

    private void ApplyTypeData()
    {
        if (typeData == null)
        {
            Debug.LogWarning("BrickTypeData not assigned on " + gameObject.name);
            maxHitPoints = 1;
            currentHitPoints = 1;
            UpdateHealthAlpha();
            return;
        }

        int configuredHitPoints = overrideHitPoints > 0 ? overrideHitPoints : typeData.HitPoints;
        maxHitPoints = Mathf.Max(1, configuredHitPoints);
        currentHitPoints = maxHitPoints;
        ClearBurn();
        ClearCollapse();
        ClearEarthCrack();
        ClearRoot();
        ClearColumnSlow();
        ClearCombustion();
        nextFireSpreadAllowedTime = 0f;
        ClearPressurizedSplash();
        ClearSeedRoot();
        ClearForestFire();
        ClearConductive();

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = typeData.BrickSprite != null ? typeData.BrickSprite : defaultBrickSprite;
            spriteRenderer.enabled = true;
            spriteRenderer.color = typeData.DisplayColor;
        }

        UpdateHealthAlpha();
    }

    public void ApplyBallHit(BallController ball)
    {
        HandleBallHit(ball);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.TryGetComponent<BallController>(out BallController ball))
        {
            return;
        }

        HandleBallHit(ball);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent<BallController>(out BallController ball))
        {
            return;
        }

        if (ball.TypeData != null && ball.TypeData.PassThroughBricks)
        {
            return;
        }

        HandleBallHit(ball);
    }

    protected virtual void HandleBallHit(BallController ball)
    {
        if (ball == null || !ball.ConsumeBrickBounce())
        {
            return;
        }

        lastHittingBall = ball;
        BallTypeData ballTypeData = ball.TypeData;
        bool wasCrackedBeforeHit = isCracked;
        bool isSuperEffectiveHit = IsSuperEffectiveHit(ball);

        int damage = GetDamageFromBall(ball);
        int finalDamage = isWeakened ? damage * 2 : damage;
        bool willDestroyBrick = currentHitPoints - finalDamage <= 0;
        ApplyDamage(damage, DamageSource.BallHit);
        if (isSuperEffectiveHit)
        {
            SuperEffectiveHit?.Invoke(this);
            BreakoutSoundController.PlaySuperEffectiveBrickHitSfx();
            SuperEffectiveGleamEffect.Spawn(
                transform.position,
                superEffectiveGleamColor,
                superEffectiveGleamSize,
                superEffectiveGleamWidth,
                superEffectiveGleamLifetime);
        }
        else if (!willDestroyBrick)
        {
            BreakoutSoundController.PlayBasicBrickHitSfx();
        }

        ball.RegisterRollingThunderBrickHit();

        ball.TrySpawnWaterDropsFromBrickHit();
        ball.NotifyFirstAidBrickHit();

        if (wasCrackedBeforeHit)
        {
            bool spreadCrackToShatterHits = ballTypeData != null && ballTypeData.CreatesTremor;
            int propagatedCrackDamage = spreadCrackToShatterHits ? ballTypeData.TremorCrackDamage : 1;
            float propagatedCrackRadius = spreadCrackToShatterHits ? ballTypeData.TremorCrackRadius : MinimumEffectRadius;
            TriggerStoredCrackShatter(spreadCrackToShatterHits, propagatedCrackDamage, propagatedCrackRadius);
        }

        ApplyBallTypeEffects(ballTypeData);

        if (typeData != null && typeData.Type == BallTypeData.BallElement.Wind && BallHasElement(ballTypeData, BallTypeData.BallElement.Fire))
        {
            TriggerWindFireBurst();
        }

        if (typeData != null && typeData.Type == BallTypeData.BallElement.Fire && BallHasElement(ballTypeData, BallTypeData.BallElement.Water))
        {
            TriggerSteamWeaken();
        }

        if (typeData != null && typeData.Type == BallTypeData.BallElement.Life && BallHasElement(ballTypeData, BallTypeData.BallElement.Water))
        {
            TriggerLifeWaterRoot();
        }

        if (typeData != null && typeData.Type == BallTypeData.BallElement.Lightning && BallHasElement(ballTypeData, BallTypeData.BallElement.Earth))
        {
            TriggerEarthLightningFireStrike();
        }

        if (typeData != null && typeData.Type == BallTypeData.BallElement.Lightning && BallHasElement(ballTypeData, BallTypeData.BallElement.Wind))
        {
            TriggerWindChainLightning(ball);
        }

        if (typeData != null && typeData.Type == BallTypeData.BallElement.Earth && BallHasElement(ballTypeData, BallTypeData.BallElement.Lightning))
        {
            TriggerLightningEarthCrackField();
        }

        if (typeData != null && typeData.Type == BallTypeData.BallElement.Earth && BallHasElement(ballTypeData, BallTypeData.BallElement.Life))
        {
            TriggerEarthLifeRoot();
        }

        ball.TryFireChainLightning(this);

        ball.FinalizeBrickHit();
    }

    protected virtual int GetDamageFromBall(BallController ball)
    {
        if (ball == null || ball.TypeData == null)
        {
            return 1;
        }

        int baseDamage = Mathf.Max(1, ball.TypeData.Damage);
        if (typeData == null)
        {
            return baseDamage;
        }

        if (IsSuperEffectiveHit(ball))
        {
            return baseDamage * StrongAgainstDamageMultiplier;
        }

        return baseDamage;
    }

    private bool IsSuperEffectiveHit(BallController ball)
    {
        return ball != null
            && ball.TypeData != null
            && typeData != null
            && ball.TypeData.IsStrongAgainst(typeData.Type);
    }

    protected virtual void ApplyDamage(int amount)
    {
        ApplyDamage(amount, DamageSource.Effect);
    }

    private void ApplyDamage(int amount, DamageSource source)
    {
        int clampedAmount = Mathf.Max(0, amount);
        if (clampedAmount <= 0)
        {
            return;
        }

        int finalDamage = isWeakened ? clampedAmount * 2 : clampedAmount;
        currentHitPoints -= finalDamage;

        UpdateHealthAlpha();
        if (source != DamageSource.Burn)
        {
            TriggerDamageFlash();
        }

        if (hasConductive && source != DamageSource.ElectricCascade)
        {
            TriggerElectricCascade();
        }

        if (currentHitPoints <= 0)
        {
            TryTriggerCombustionExplosionOnDestroyed();
            TryTriggerPressureBurstOnDestroyed();
            TrySpreadForestFireOnDestroyed();
            OnBrickDestroyed();
            BreakoutSoundController.PlayBrickDestroyedSfx();
            Destroy(gameObject);
        }
    }

    public void ApplyDirectEffectDamage(int amount)
    {
        ApplyDamage(amount, DamageSource.Effect);
    }

    public void ApplyFertileLandPatch(
        bool applyCrack,
        int crackDamage,
        float crackRadius,
        bool applyRoot,
        float rootRadius,
        float rootDuration,
        float rootSpeedMultiplier)
    {
        if (currentHitPoints <= 0)
        {
            return;
        }

        if (applyCrack)
        {
            SetCrackedState(crackDamage, crackRadius);
        }

        if (applyRoot)
        {
            ApplyRootInRadius(rootRadius, rootDuration, rootSpeedMultiplier);
        }
    }

    private void ApplyCollapse(BallTypeData ballTypeData)
    {
        if (ballTypeData == null)
        {
            return;
        }

        float weakenDuration = Mathf.Max(MinimumDurationSeconds, ballTypeData.CollapseDuration);
        ApplyWeakened(weakenDuration);

        float weakenRadius = Mathf.Max(MinimumEffectRadius, ballTypeData.CollapseRadius);
        CollectNearbyBricks(weakenRadius, nearbyBricksBuffer);
        for (int i = 0; i < nearbyBricksBuffer.Count; i++)
        {
            nearbyBricksBuffer[i].ApplyWeakened(weakenDuration);
        }
    }

    private void ApplyWeakened(float duration)
    {
        isWeakened = true;
        float clampedDuration = Mathf.Max(MinimumDurationSeconds, duration);
        weakenedTimeRemaining = Mathf.Max(weakenedTimeRemaining, clampedDuration);
        weakenedInitialDuration = Mathf.Max(weakenedInitialDuration, clampedDuration);
    }

    private void ApplyCyclone(BallTypeData ballTypeData)
    {
        if (ballTypeData == null || currentHitPoints <= 0)
        {
            return;
        }

        int followUpHitCount = Mathf.Max(1, ballTypeData.CycloneFollowUpHitCount);
        float hitDelay = Mathf.Max(MinimumDurationSeconds, ballTypeData.CycloneHitDelay);
        int hitDamage = Mathf.Max(1, ballTypeData.Damage);
        StartCoroutine(ApplyCycloneFollowUpHits(followUpHitCount, hitDelay, hitDamage));
    }

    private IEnumerator ApplyCycloneFollowUpHits(int followUpHitCount, float hitDelay, int hitDamage)
    {
        for (int i = 0; i < followUpHitCount; i++)
        {
            if (currentHitPoints <= 0)
            {
                yield break;
            }

            yield return new WaitForSeconds(hitDelay);

            if (currentHitPoints <= 0)
            {
                yield break;
            }

            ApplyDirectEffectDamage(hitDamage);
        }
    }

    private void UpdateCollapse()
    {
        if (!isWeakened)
        {
            return;
        }

        weakenedTimeRemaining -= Time.deltaTime;
        if (weakenedTimeRemaining <= 0f)
        {
            ClearCollapse();
        }
    }

    private void ClearCollapse()
    {
        isWeakened = false;
        weakenedTimeRemaining = 0f;
        weakenedInitialDuration = 0f;
    }

    private void UpdateHealthAlpha()
    {
        float ratio = Mathf.Clamp01((float)currentHitPoints / Mathf.Max(1, maxHitPoints));
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = ratio;
            spriteRenderer.color = color;
        }
    }

    protected virtual void OnBrickDestroyed()
    {
        int scoreValue = typeData != null ? Mathf.Max(0, typeData.ScoreValue) : 0;
        BrickDestroyed?.Invoke(this, scoreValue);
    }

    private IEnumerator DangerSequenceCoroutine(float waitBeforeShakeSeconds, float shakeDurationSeconds, float shakeMagnitude)
    {
        if (waitBeforeShakeSeconds > 0f)
        {
            yield return new WaitForSeconds(waitBeforeShakeSeconds);
        }

        float elapsed = 0f;
        while (elapsed < shakeDurationSeconds)
        {
            elapsed += Time.deltaTime;
            Vector2 offset = Random.insideUnitCircle * shakeMagnitude;
            transform.position = dangerBasePosition + new Vector3(offset.x, offset.y, 0f);
            yield return null;
        }

        transform.position = dangerBasePosition;
        ApplyDangerPenaltyAndRemove();
    }

    private void ApplyDangerPenaltyAndRemove()
    {
        int damageToPlayer = typeData != null ? Mathf.Max(0, typeData.DamageToPlayer) : 0;
        if (damageToPlayer > 0 && PlayerStats.HasInstance)
        {
            PlayerStats.Instance.TakeDamage(damageToPlayer);
        }

        BrickRemovedByDanger?.Invoke(this);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        InvalidateClosestBrickBelowCache();

        if (dangerSequenceRoutine != null)
        {
            StopCoroutine(dangerSequenceRoutine);
            dangerSequenceRoutine = null;
        }

        if (damageFlashRoutine != null)
        {
            StopCoroutine(damageFlashRoutine);
            damageFlashRoutine = null;
        }
    }

    private void UpdateBurning()
    {
        if (!isBurning || burnHitsRemaining <= 0)
        {
            return;
        }

        burnTickTimer -= Time.deltaTime;
        if (burnTickTimer > 0f)
        {
            return;
        }

        burnHitsRemaining--;
        burnTickTimer = burnTickInterval;
        ApplyDamage(GetBurnDamage(), DamageSource.Burn);
        burnFlashTimer = 0f;

        if (currentHitPoints <= 0)
        {
            return;
        }

        if (burnHitsRemaining <= 0)
        {
            if (hasCombustion && !combustionExplosionTriggered)
            {
                TriggerCombustionExplosion();
                return;
            }

            ClearBurn();
        }
    }

    private void ApplyBurn(int damagePerTick, float tickInterval, int hitCount)
    {
        if (typeData != null && typeData.FireResistant)
        {
            return;
        }

        if (hitCount <= 0)
        {
            return;
        }

        isBurning = true;
        burnDamage = Mathf.Max(1, damagePerTick);
        burnTickInterval = Mathf.Max(MinimumDurationSeconds, tickInterval);
        burnTickTimer = burnTickInterval;
        burnHitsRemaining = hitCount;
        burnInitialHitCount = Mathf.Max(burnInitialHitCount, burnHitsRemaining);
    }

    public void ApplyExternalBurn(int damagePerTick, float tickInterval, int hitCount)
    {
        ApplyBurn(damagePerTick, tickInterval, hitCount);
    }

    private void ClearBurn()
    {
        isBurning = false;
        burnDamage = 0;
        burnTickInterval = 0f;
        burnTickTimer = 0f;
        burnHitsRemaining = 0;
        burnInitialHitCount = 0;
        burnFlashTimer = -1f;
    }

    private void UpdateBurnFlash()
    {
        if (burnFlashTimer < 0f || !enableDamageFlash)
        {
            return;
        }

        float duration = Mathf.Max(MinimumDurationSeconds, damageFlashDuration);
        burnFlashTimer += Time.deltaTime;

        if (burnFlashTimer >= duration)
        {
            burnFlashTimer = -1f;
            SetBaseBrickColor();
            return;
        }

        float halfDuration = duration * 0.5f;
        float pulse01 = burnFlashTimer <= halfDuration
            ? burnFlashTimer / halfDuration
            : 1f - (burnFlashTimer - halfDuration) / halfDuration;

        ApplyDamageFlashColor(Mathf.Clamp01(pulse01));
    }

    private void ApplyCombustion(BallTypeData ballTypeData)
    {
        if (ballTypeData == null)
        {
            return;
        }

        hasCombustion = true;
        combustionExplosionDamage = Mathf.Max(combustionExplosionDamage, Mathf.Max(1, ballTypeData.CombustionExplosionDamage));
        combustionExplosionRadius = Mathf.Max(combustionExplosionRadius, Mathf.Max(MinimumEffectRadius, ballTypeData.CombustionExplosionRadius));

        ApplyBurn(
            Mathf.Max(1, ballTypeData.CombustionBurnDamage),
            Mathf.Max(MinimumDurationSeconds, ballTypeData.CombustionBurnTickInterval),
            Mathf.Max(1, ballTypeData.CombustionBurnHitCount));
    }

    private void ClearCombustion()
    {
        hasCombustion = false;
        combustionExplosionTriggered = false;
        combustionExplosionDamage = 0;
        combustionExplosionRadius = 0f;
    }

    private void TriggerCombustionExplosion()
    {
        if (combustionExplosionTriggered)
        {
            return;
        }

        combustionExplosionTriggered = true;

        int explosionDamage = Mathf.Max(1, combustionExplosionDamage);
        float explosionRadius = Mathf.Max(MinimumEffectRadius, combustionExplosionRadius);
        CollectNearbyBricks(explosionRadius, nearbyBricksBuffer);
        for (int i = 0; i < nearbyBricksBuffer.Count; i++)
        {
            nearbyBricksBuffer[i].ApplyDamage(explosionDamage);
        }

        if (currentHitPoints > 0)
        {
            ApplyDamage(currentHitPoints);
        }
    }

    private void TryTriggerCombustionExplosionOnDestroyed()
    {
        if (!hasCombustion || !isBurning || combustionExplosionTriggered)
        {
            return;
        }

        TriggerCombustionExplosion();
    }

    private void ApplyPressurizedSplash(BallTypeData ballTypeData)
    {
        if (ballTypeData == null)
        {
            return;
        }

        if (!hasPressurizedSplash)
        {
            hasPressurizedSplash = true;
            pressureMaxThreshold = Mathf.Max(1, ballTypeData.MaxPressure);
            splashDropletType = ballTypeData.SplashDropletType;
            splashDropletCount = Mathf.Max(1, ballTypeData.SplashDropletCount);
        }

        currentPressure += Mathf.Max(1, ballTypeData.PressurePerHit);

        if (currentPressure >= pressureMaxThreshold && !pressureBurstTriggered)
        {
            TriggerPressureBurst();
        }
    }

    private void TriggerPressureBurst()
    {
        if (pressureBurstTriggered)
        {
            return;
        }

        pressureBurstTriggered = true;

        if (lastHittingBall == null)
        {
            return;
        }

        if (splashDropletType == null)
        {
            return;
        }

        int count = Mathf.Max(1, splashDropletCount);
        Vector3 origin = transform.position;
        const float spawnOffset = 0.18f;

        for (int i = 0; i < count; i++)
        {
            float angle = 360f / count * i;
            Vector2 dir = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)).normalized;
            Vector3 spawnPos = origin + (Vector3)(dir * spawnOffset);
            lastHittingBall.SpawnDropletAt(splashDropletType, spawnPos, dir);
        }

        if (currentHitPoints > 0)
        {
            ApplyDamage(currentHitPoints);
        }
    }

    private void TryTriggerPressureBurstOnDestroyed()
    {
        if (!hasPressurizedSplash || currentPressure <= 0 || pressureBurstTriggered)
        {
            return;
        }

        TriggerPressureBurst();
    }

    private void ClearPressurizedSplash()
    {
        hasPressurizedSplash = false;
        pressureBurstTriggered = false;
        currentPressure = 0;
        pressureMaxThreshold = 0;
        splashDropletType = null;
        splashDropletCount = 0;
    }

    private void ApplyFireSpread(BallTypeData ballTypeData)
    {
        if (ballTypeData == null)
        {
            return;
        }

        if (!CanTriggerFireSpread())
        {
            return;
        }

        float cooldown = Mathf.Max(0f, ballTypeData.FireSpreadCooldown);
        if (Time.time < nextFireSpreadAllowedTime)
        {
            return;
        }

        nextFireSpreadAllowedTime = Time.time + cooldown;

        float spreadRadius = Mathf.Max(MinimumEffectRadius, ballTypeData.FireSpreadRadius);
        CollectNearbyBricks(spreadRadius, nearbyBricksBuffer);
        if (nearbyBricksBuffer.Count == 0)
        {
            return;
        }

        int bonusDamage = Mathf.Max(0, ballTypeData.FireSpreadBonusBurnDamage);
        float burnSpeedMultiplier = Mathf.Max(1f, ballTypeData.FireSpreadBurnSpeedMultiplier);
        int bonusHitCount = Mathf.Max(0, ballTypeData.FireSpreadBurnHitCountBonus);

        for (int i = 0; i < nearbyBricksBuffer.Count; i++)
        {
            nearbyBricksBuffer[i].ApplyFireSpreadBurn(ballTypeData, bonusDamage, burnSpeedMultiplier, bonusHitCount);
        }
    }

    private bool CanTriggerFireSpread()
    {
        if (isBurning)
        {
            return true;
        }

        return typeData != null && typeData.Type == BallTypeData.BallElement.Fire;
    }

    private void ApplyFireSpreadBurn(BallTypeData sourceBallTypeData, int bonusDamage, float burnSpeedMultiplier, int bonusHitCount)
    {
        bool hasBurn = sourceBallTypeData != null && sourceBallTypeData.AppliesBurn;
        int sourceDamage = hasBurn ? sourceBallTypeData.BurnDamage : 1;
        float sourceInterval = hasBurn ? sourceBallTypeData.BurnTickInterval : 0.5f;
        int sourceHits = hasBurn ? sourceBallTypeData.BurnHitCount : 1;

        int baseDamage = isBurning ? burnDamage : sourceDamage;
        float baseInterval = isBurning ? burnTickInterval : sourceInterval;
        int baseHitCount = isBurning ? burnHitsRemaining : sourceHits;

        int spreadDamage = Mathf.Max(1, baseDamage + bonusDamage);
        float spreadInterval = Mathf.Max(MinimumDurationSeconds, baseInterval / Mathf.Max(1f, burnSpeedMultiplier));
        int spreadHitCount = Mathf.Max(1, baseHitCount + bonusHitCount);
        ApplyBurn(spreadDamage, spreadInterval, spreadHitCount);
    }

    private void ApplyForestFire(BallTypeData ballTypeData)
    {
        if (ballTypeData == null)
        {
            return;
        }

        int spreadGenerations = Mathf.Max(0, ballTypeData.ForestFireSpreadGenerations);
        ApplyForestFireSource(
            Mathf.Max(1, ballTypeData.ForestFireBurnDamage),
            Mathf.Max(MinimumDurationSeconds, ballTypeData.ForestFireBurnTickInterval),
            Mathf.Max(1, ballTypeData.ForestFireBurnHitCount),
            spreadGenerations);
    }

    private void ApplyForestFireSource(int damagePerTick, float tickInterval, int hitCount, int spreadGenerations)
    {
        hasForestFire = true;
        forestFireSpreadGenerationsRemaining = Mathf.Max(forestFireSpreadGenerationsRemaining, spreadGenerations);
        ApplyBurn(damagePerTick, tickInterval, hitCount);
    }

    private void ClearForestFire()
    {
        hasForestFire = false;
        forestFireSpreadGenerationsRemaining = 0;
    }

    private void ApplyConductive(BallTypeData ballTypeData)
    {
        if (ballTypeData == null)
        {
            return;
        }

        hasConductive = true;
        conductiveShockDamage = Mathf.Max(conductiveShockDamage, Mathf.Max(1, ballTypeData.ElectricCascadeShockDamage));
        conductiveVisualData = ballTypeData;
        float clampedDuration = Mathf.Max(MinimumDurationSeconds, ballTypeData.ElectricCascadeConductiveDuration);
        conductiveTimeRemaining = Mathf.Max(conductiveTimeRemaining, clampedDuration);
        conductiveInitialDuration = Mathf.Max(conductiveInitialDuration, clampedDuration);
    }

    private void ClearConductive()
    {
        hasConductive = false;
        conductiveShockDamage = 0;
        conductiveVisualData = null;
        conductiveTimeRemaining = 0f;
        conductiveInitialDuration = 0f;
    }

    private void UpdateConductive()
    {
        if (!hasConductive)
        {
            return;
        }

        conductiveTimeRemaining -= Time.deltaTime;
        if (conductiveTimeRemaining <= 0f)
        {
            ClearConductive();
        }
    }

    private void TriggerElectricCascade()
    {
        int shockDamage = Mathf.Max(1, conductiveShockDamage);
        CollectCrossLineBricks(this, crossLineBricksBuffer);

        for (int i = 0; i < crossLineBricksBuffer.Count; i++)
        {
            BrickController target = crossLineBricksBuffer[i];
            if (target == null)
            {
                continue;
            }

            target.ApplyDamage(shockDamage, DamageSource.ElectricCascade);
        }

        if (conductiveVisualData != null)
        {
            Vector3 origin = transform.position;
            LightningBoltEffect.SpawnCascadeBeam(origin, Vector2.right, conductiveVisualData);
            LightningBoltEffect.SpawnCascadeBeam(origin, Vector2.left, conductiveVisualData);
            LightningBoltEffect.SpawnCascadeBeam(origin, Vector2.up, conductiveVisualData);
            LightningBoltEffect.SpawnCascadeBeam(origin, Vector2.down, conductiveVisualData);
        }
    }

    private void TrySpreadForestFireOnDestroyed()
    {
        if (!hasForestFire || !isBurning || forestFireSpreadGenerationsRemaining <= 0)
        {
            return;
        }

        int nextGenerations = forestFireSpreadGenerationsRemaining - 1;
        int spreadDamage = Mathf.Max(1, burnDamage);
        float spreadTickInterval = Mathf.Max(MinimumDurationSeconds, burnTickInterval);
        int spreadHitCount = Mathf.Max(1, burnHitsRemaining);

        CollectOrthogonalAdjacentBricks(this, nearbyBricksBuffer);
        for (int i = 0; i < nearbyBricksBuffer.Count; i++)
        {
            BrickController adjacent = nearbyBricksBuffer[i];
            adjacent.ApplyForestFireSource(spreadDamage, spreadTickInterval, spreadHitCount, nextGenerations);
        }
    }

    private int GetBurnDamage()
    {
        if (isRooted || (typeData != null && typeData.Flammable))
        {
            return burnDamage * 2;
        }

        return burnDamage;
    }

    private void ApplyCrackHit(int crackDamage, float crackRadius)
    {
        if (isCracked)
        {
            return;
        }

        SetCrackedState(crackDamage, crackRadius);
    }

    private void SetCrackedState(int shatterDamage, float shatterRadius)
    {
        isCracked = true;
        crackShatterDamage = Mathf.Max(1, shatterDamage);
        crackShatterRadius = Mathf.Max(MinimumEffectRadius, shatterRadius);
    }

    private void TriggerStoredCrackShatter(bool spreadCrackToShatterHits, int propagatedCrackDamage, float propagatedCrackRadius)
    {
        if (!isCracked)
        {
            return;
        }

        isCracked = false;

        CrackShatterEffect.Spawn(
            transform.position,
            Mathf.Max(MinimumEffectRadius, crackShatterRadius),
            crackShatterEffectColor,
            crackShatterEffectWidth,
            crackShatterEffectLifetime,
            crackShatterEffectCrackCount);

        TriggerShatter(crackShatterDamage, crackShatterRadius, spreadCrackToShatterHits, propagatedCrackDamage, propagatedCrackRadius);
    }

    private void TriggerShatter(int damage, float radius, bool spreadCrackToShatterHits, int propagatedCrackDamage, float propagatedCrackRadius)
    {
        int shatterDamage = Mathf.Max(1, damage);
        float shatterRadius = Mathf.Max(MinimumEffectRadius, radius);
        int clampedPropagatedCrackDamage = Mathf.Max(1, propagatedCrackDamage);
        float clampedPropagatedCrackRadius = Mathf.Max(MinimumEffectRadius, propagatedCrackRadius);
        CollectNearbyBricks(shatterRadius, nearbyBricksBuffer);
        if (nearbyBricksBuffer.Count == 0)
        {
            return;
        }

        for (int i = 0; i < nearbyBricksBuffer.Count; i++)
        {
            BrickController nearbyBrick = nearbyBricksBuffer[i];
            nearbyBrick.ApplyDamage(shatterDamage);

            if (!spreadCrackToShatterHits || nearbyBrick == null || nearbyBrick.currentHitPoints <= 0)
            {
                continue;
            }

            nearbyBrick.SetCrackedState(clampedPropagatedCrackDamage, clampedPropagatedCrackRadius);
        }
    }

    private void ClearEarthCrack()
    {
        isCracked = false;
        crackShatterDamage = 1;
        crackShatterRadius = 1f;
    }

    private void ApplyRootToBrickAndAbove(float duration, float speedMultiplier)
    {
        float clampedDuration = Mathf.Max(0f, duration);
        if (clampedDuration <= 0f)
        {
            return;
        }

        float clampedSpeedMultiplier = ClampEffectSpeedMultiplier(speedMultiplier);
        float yThreshold = transform.position.y - RootColumnYThresholdOffset;
        float xTolerance = GetColumnTolerance();
        float rowSpacing = GetRowSpacingEstimate();

        Transform parentTransform = transform.parent;
        if (parentTransform == null)
        {
            ApplyRoot(clampedDuration, clampedSpeedMultiplier);
            return;
        }

        for (int i = 0; i < parentTransform.childCount; i++)
        {
            Transform child = parentTransform.GetChild(i);
            if (child == null || child.position.y < yThreshold)
            {
                continue;
            }

            if (Mathf.Abs(child.position.x - transform.position.x) > xTolerance)
            {
                continue;
            }

            if (!child.TryGetComponent<BrickController>(out BrickController brick))
            {
                continue;
            }

            if (brick == this)
            {
                brick.ApplyRoot(clampedDuration, clampedSpeedMultiplier);
                continue;
            }

            float dy = child.position.y - transform.position.y;
            if (dy > 0f && dy <= rowSpacing)
                brick.ApplyColumnSlow(clampedDuration, clampedSpeedMultiplier);
        }
    }

    private void ApplyRoot(float duration, float speedMultiplier)
    {
        float clampedDuration = Mathf.Max(0f, duration);
        isRooted = true;
        rootTimeRemaining = clampedDuration;
        rootInitialDuration = Mathf.Max(rootInitialDuration, clampedDuration);
        rootSpeedMultiplier = ClampEffectSpeedMultiplier(speedMultiplier);
    }

    private void ApplyRootInRadius(float radius, float duration, float speedMultiplier)
    {
        float clampedDuration = Mathf.Max(0f, duration);
        if (clampedDuration <= 0f)
        {
            return;
        }

        float clampedSpeedMultiplier = ClampEffectSpeedMultiplier(speedMultiplier);
        float clampedRadius = Mathf.Max(MinimumEffectRadius, radius);

        ApplyRoot(clampedDuration, clampedSpeedMultiplier);
        CollectNearbyBricks(clampedRadius, nearbyBricksBuffer);

        for (int i = 0; i < nearbyBricksBuffer.Count; i++)
        {
            BrickController nearbyBrick = nearbyBricksBuffer[i];
            if (nearbyBrick == null || nearbyBrick.currentHitPoints <= 0)
            {
                continue;
            }

            nearbyBrick.ApplyRoot(clampedDuration, clampedSpeedMultiplier);
        }
    }

    private void ApplyColumnSlow(float duration, float speedMultiplier)
    {
        hasColumnSlow = true;
        columnSlowTimeRemaining = Mathf.Max(0f, duration);
        columnSlowSpeedMultiplier = ClampEffectSpeedMultiplier(speedMultiplier);
    }

    private void UpdateRooting()
    {
        if (!isRooted)
        {
            return;
        }

        rootTimeRemaining -= Time.deltaTime;
        if (rootTimeRemaining <= 0f)
        {
            ClearRoot();
        }
    }

    private void UpdateSeedRoot()
    {
        if (!hasSeedRoot)
        {
            return;
        }

        seedRootTimeRemaining -= Time.deltaTime;
        if (seedRootTimeRemaining <= 0f)
        {
            int generationsLeft = seedSpreadGenerationsRemaining;
            int spreadCount = seedSpreadCount;
            float spreadRadius = seedSpreadRadius;
            float nextDuration = seedRootDuration;
            float nextSpeedMult = seedRootBaseSpeedMult;
            ClearSeedRoot();

            if (generationsLeft > 0)
            {
                SpreadSeedRoot(generationsLeft, spreadCount, spreadRadius, nextDuration, nextSpeedMult);
            }
        }
    }

    private void UpdateColumnSlowing()
    {
        if (!hasColumnSlow)
        {
            return;
        }

        columnSlowTimeRemaining -= Time.deltaTime;
        if (columnSlowTimeRemaining <= 0f)
        {
            ClearColumnSlow();
        }
    }

    private void ClearRoot()
    {
        isRooted = false;
        rootTimeRemaining = 0f;
        rootInitialDuration = 0f;
        rootSpeedMultiplier = 1f;
    }

    private void ClearColumnSlow()
    {
        hasColumnSlow = false;
        columnSlowTimeRemaining = 0f;
        columnSlowSpeedMultiplier = 1f;
    }

    private float GetCurrentDownwardSpeed()
    {
        if (cachedDownwardSpeedFrame == Time.frameCount)
        {
            return cachedDownwardSpeed;
        }

        float currentSpeed = 0f;

        if (IsPinnedInPlace)
        {
            cachedDownwardSpeed = 0f;
            cachedDownwardSpeedFrame = Time.frameCount;
            return 0f;
        }

        if (IsBlockedBelowByStoppedBrick())
        {
            cachedDownwardSpeed = 0f;
            cachedDownwardSpeedFrame = Time.frameCount;
            return 0f;
        }

        currentSpeed = downwardSpeed * GetAppliedSpeedMultiplier();
        float followGapThreshold = Mathf.Max(MinimumFollowGap, GetRowSpacingEstimate() * FollowDistanceThresholdMultiplier);

        if (TryGetClosestBrickBelow(followGapThreshold, out BrickController brickAhead, out float _))
        {
            currentSpeed = Mathf.Min(currentSpeed, brickAhead.GetCurrentDownwardSpeed());
        }

        cachedDownwardSpeed = Mathf.Max(0f, currentSpeed);
        cachedDownwardSpeedFrame = Time.frameCount;
        return cachedDownwardSpeed;
    }

    private float GetColumnTolerance()
    {
        if (brickCollider != null)
        {
            return Mathf.Max(MinimumColumnTolerance, brickCollider.bounds.extents.x * 0.7f);
        }

        return DefaultColumnTolerance;

    }

    private bool IsBlockedBelowByStoppedBrick()
    {
        if (!TryGetClosestBrickBelow(GetRowSpacingEstimate(), out BrickController brickAhead, out float _))
            return false;

        return brickAhead.IsEffectivelyStopped;
    }

    private bool TryGetClosestBrickBelow(float maxGap, out BrickController closestBrick, out float verticalGap)
    {
        closestBrick = null;
        verticalGap = 0f;

        if (transform.parent == null)
            return false;

        float clampedMaxGap = Mathf.Max(MinimumFollowGap, maxGap);
        float xTolerance = GetColumnTolerance();

        if (cachedClosestBrickBelowVersion == closestBrickBelowCacheVersion)
        {
            if (!hasCachedClosestBrickBelow)
            {
                return false;
            }

            if (IsValidCachedBrickBelow(cachedClosestBrickBelow, clampedMaxGap, xTolerance, out float cachedGap))
            {
                closestBrick = cachedClosestBrickBelow;
                verticalGap = cachedGap;
                return true;
            }
        }

        float closestGap = float.PositiveInfinity;

        for (int i = 0; i < transform.parent.childCount; i++)
        {
            Transform child = transform.parent.GetChild(i);
            if (child == null || child == transform)
                continue;

            float dy = transform.position.y - child.position.y;
            if (dy <= 0f || dy > clampedMaxGap)
                continue;

            if (Mathf.Abs(child.position.x - transform.position.x) > xTolerance)
                continue;

            if (!child.TryGetComponent<BrickController>(out BrickController brick) || brick.CurrentHitPoints <= 0)
                continue;

            if (dy >= closestGap)
                continue;

            closestGap = dy;
            closestBrick = brick;
        }

        if (closestBrick == null)
        {
            cachedClosestBrickBelow = null;
            hasCachedClosestBrickBelow = false;
            cachedClosestBrickBelowVersion = closestBrickBelowCacheVersion;
            return false;
        }

        verticalGap = closestGap;
        cachedClosestBrickBelow = closestBrick;
        hasCachedClosestBrickBelow = true;
        cachedClosestBrickBelowVersion = closestBrickBelowCacheVersion;
        return true;
    }

    private bool IsValidCachedBrickBelow(BrickController candidate, float maxGap, float xTolerance, out float verticalGap)
    {
        verticalGap = 0f;

        if (candidate == null || candidate == this || candidate.currentHitPoints <= 0 || candidate.transform.parent != transform.parent)
        {
            return false;
        }

        verticalGap = transform.position.y - candidate.transform.position.y;
        if (verticalGap <= 0f || verticalGap > maxGap)
        {
            return false;
        }

        return Mathf.Abs(candidate.transform.position.x - transform.position.x) <= xTolerance;
    }

    private static void InvalidateClosestBrickBelowCache()
    {
        if (closestBrickBelowCacheVersion == int.MaxValue)
        {
            closestBrickBelowCacheVersion = 1;
            return;
        }

        closestBrickBelowCacheVersion++;
    }

    private void InvalidateDownwardSpeedCache()
    {
        cachedDownwardSpeedFrame = -1;
    }

    private float GetRowSpacingEstimate()
    {
        if (transform.parent != null && transform.parent.TryGetComponent<BrickGridSpawner>(out BrickGridSpawner spawner))
        {
            // Use the same spacing source as spawned rows so stop propagation matches the grid.
            return Mathf.Max(MinimumDurationSeconds, spawner.VerticalSpacing * RowSpacingSafetyMultiplier);
        }

        if (brickCollider != null)
            return brickCollider.bounds.size.y * 2f;
        return DefaultRowSpacing;
    }

    private void ApplyLightningBurst(BallTypeData ballTypeData)
    {
        if (ballTypeData == null)
        {
            return;
        }

        int targetCount = Mathf.Max(0, ballTypeData.LightningBurstTargetCount + GetLightningTargetBonus());
        if (targetCount <= 0)
        {
            return;
        }

        int burstDamage = Mathf.Max(1, ballTypeData.LightningBurstDamage);
        float burstRadius = Mathf.Max(0.1f, ballTypeData.LightningBurstRadius);

        CollectNearbyBricks(burstRadius, nearbyBricksBuffer);
        if (nearbyBricksBuffer.Count == 0)
        {
            return;
        }

        int burstHits = Mathf.Min(targetCount, nearbyBricksBuffer.Count);
        for (int i = 0; i < burstHits; i++)
        {
            int randomIndex = Random.Range(i, nearbyBricksBuffer.Count);
            BrickController selected = nearbyBricksBuffer[randomIndex];
            nearbyBricksBuffer[randomIndex] = nearbyBricksBuffer[i];
            nearbyBricksBuffer[i] = selected;

            selected.ApplyDamage(burstDamage);
            LightningBoltEffect.SpawnBurst(transform.position, selected.transform.position, ballTypeData);
        }
    }

    private void ApplyImpactBurst(BallTypeData ballTypeData)
    {
        if (ballTypeData == null)
        {
            return;
        }

        int burstDamage = Mathf.Max(1, ballTypeData.ImpactBurstDamage);
        float burstRadius = Mathf.Max(MinimumEffectRadius, ballTypeData.ImpactBurstRadius);
        CollectNearbyBricks(burstRadius, nearbyBricksBuffer);
        if (nearbyBricksBuffer.Count == 0)
        {
            return;
        }

        for (int i = 0; i < nearbyBricksBuffer.Count; i++)
        {
            nearbyBricksBuffer[i].ApplyDamage(burstDamage);
        }
    }

    private static bool BallHasElement(BallTypeData ballTypeData, BallTypeData.BallElement element)
    {
        if (ballTypeData == null)
        {
            return false;
        }

        BallTypeData.BallElement[] elements = ballTypeData.Elements;
        if (elements == null)
        {
            return false;
        }

        for (int i = 0; i < elements.Length; i++)
        {
            if (elements[i] == element)
            {
                return true;
            }
        }

        return false;
    }

    private void TriggerWindFireBurst()
    {
        int burstDamage = Mathf.Max(1, typeData.WindFireBurstDamage);
        float burstRadius = Mathf.Max(MinimumEffectRadius, typeData.WindFireBurstRadius);

        CollectNearbyBricks(burstRadius, nearbyBricksBuffer);
        for (int i = 0; i < nearbyBricksBuffer.Count; i++)
        {
            nearbyBricksBuffer[i].ApplyDamage(burstDamage);
        }

        SpawnWindFireBurstVisual();
    }

    private void SpawnWindFireBurstVisual()
    {
        FireBurstEffect.Spawn(
            transform.position,
            typeData.WindFireBurstColor,
            typeData.WindFireBurstWidth,
            typeData.WindFireBurstRayLength,
            typeData.WindFireBurstLifetime,
            typeData.WindFireBurstRayCount);
    }

    private void TriggerSteamWeaken()
    {
        float radius = Mathf.Max(MinimumEffectRadius, typeData.SteamWeakenRadius);
        float duration = Mathf.Max(MinimumDurationSeconds, typeData.SteamWeakenDuration);

        CollectNearbyBricks(radius, nearbyBricksBuffer);
        for (int i = 0; i < nearbyBricksBuffer.Count; i++)
        {
            nearbyBricksBuffer[i].ApplyWeakened(duration);
        }

        SteamBurstEffect.Spawn(
            transform.position,
            typeData.SteamColor,
            typeData.SteamRingRadius,
            typeData.SteamLifetime,
            typeData.SteamWidth);
    }

    private void TriggerLifeWaterRoot()
    {
        float searchRadius = Mathf.Max(MinimumEffectRadius, typeData.LifeRootSearchRadius);
        float duration = Mathf.Max(MinimumDurationSeconds, typeData.LifeRootDuration);
        float speedMult = typeData.LifeRootSpeedMultiplier;

        BrickController target = GetRandomNearbyBrick(transform.position, this, searchRadius, nearbyBricksBuffer);
        if (target == null)
        {
            return;
        }

        target.ApplyRoot(duration, speedMult);

        VineTendrilEffect.Spawn(
            transform.position,
            target.transform.position,
            typeData.VineColor,
            typeData.VineWidth,
            typeData.VineGrowDuration,
            typeData.VineHoldDuration,
            typeData.VineFadeDuration);
    }

    private void TriggerEarthLightningFireStrike()
    {
        float strikeRadius = Mathf.Max(MinimumEffectRadius, typeData.EarthLightningStrikeRadius);
        BrickController target = GetRandomNearbyBrick(transform.position, this, strikeRadius, nearbyBricksBuffer);
        if (target == null)
        {
            return;
        }

        int strikeDamage = Mathf.Max(1, typeData.EarthLightningStrikeDamage);
        target.ApplyDamage(strikeDamage);
        target.ApplyExternalBurn(
            typeData.EarthLightningStrikeBurnDamage,
            typeData.EarthLightningStrikeBurnInterval,
            typeData.EarthLightningStrikeBurnHitCount);

        LightningBoltEffect.Spawn(
            transform.position,
            target.transform.position,
            typeData.EarthLightningBoltColor,
            typeData.EarthLightningBoltWidth,
            typeData.EarthLightningBoltLifetime,
            typeData.EarthLightningBoltSegments,
            typeData.EarthLightningBoltNoise);
    }

    private void TriggerWindChainLightning(BallController ball)
    {
        ball.ApplyChainLightning(
            typeData.WindChainLightningDuration,
            typeData.WindChainLightningDamage,
            typeData.WindChainLightningRadius,
            typeData.WindChainLightningBoltColor,
            typeData.WindChainLightningBoltWidth,
            typeData.WindChainLightningBoltLifetime,
            typeData.WindChainLightningBoltSegments,
            typeData.WindChainLightningBoltNoise);
    }

    private void TriggerLightningEarthCrackField()
    {
        float duration = Mathf.Max(MinimumDurationSeconds, typeData.LightningCrackFieldDuration);
        float tickInterval = Mathf.Max(MinimumDurationSeconds, typeData.LightningCrackFieldTickInterval);
        lightningCrackFieldTimeRemaining = Mathf.Max(lightningCrackFieldTimeRemaining, duration);
        lightningCrackFieldTickTimer = tickInterval;
        hasLightningCrackField = true;
    }

    private void TriggerEarthLifeRoot()
    {
        float searchRadius = Mathf.Max(MinimumEffectRadius, typeData.EarthLifeRootSearchRadius);
        float duration = Mathf.Max(MinimumDurationSeconds, typeData.EarthLifeRootDuration);
        float speedMult = typeData.EarthLifeRootSpeedMultiplier;

        BrickController target = GetRandomNearbyBrick(transform.position, this, searchRadius, nearbyBricksBuffer);
        if (target == null)
        {
            return;
        }

        target.ApplyRoot(duration, speedMult);

        VineTendrilEffect.Spawn(
            transform.position,
            target.transform.position,
            typeData.VineColor,
            typeData.VineWidth,
            typeData.VineGrowDuration,
            typeData.VineHoldDuration,
            typeData.VineFadeDuration);
    }

    private void UpdateLightningCrackField()
    {
        if (!hasLightningCrackField || currentHitPoints <= 0)
        {
            return;
        }

        lightningCrackFieldTimeRemaining -= Time.deltaTime;
        if (lightningCrackFieldTimeRemaining <= 0f)
        {
            hasLightningCrackField = false;
            return;
        }

        lightningCrackFieldTickTimer -= Time.deltaTime;
        if (lightningCrackFieldTickTimer > 0f)
        {
            return;
        }

        lightningCrackFieldTickTimer = Mathf.Max(MinimumDurationSeconds, typeData != null
            ? typeData.LightningCrackFieldTickInterval
            : MinimumDurationSeconds);

        if (typeData == null)
        {
            return;
        }

        float searchRadius = Mathf.Max(MinimumEffectRadius, typeData.LightningCrackFieldSearchRadius);
        int targetCount = Mathf.Max(1, typeData.LightningCrackFieldTargetCount);
        int crackDamage = Mathf.Max(1, typeData.LightningCrackFieldCrackDamage);
        float crackRadius = Mathf.Max(MinimumEffectRadius, typeData.LightningCrackFieldCrackRadius);

        CollectNearbyBricks(searchRadius, nearbyBricksBuffer);
        if (nearbyBricksBuffer.Count == 0)
        {
            return;
        }

        int hits = Mathf.Min(targetCount, nearbyBricksBuffer.Count);
        for (int i = 0; i < hits; i++)
        {
            int randomIndex = Random.Range(i, nearbyBricksBuffer.Count);
            BrickController selected = nearbyBricksBuffer[randomIndex];
            nearbyBricksBuffer[randomIndex] = nearbyBricksBuffer[i];
            nearbyBricksBuffer[i] = selected;

            selected.SetCrackedState(crackDamage, crackRadius);

            LightningBoltEffect.Spawn(
                transform.position,
                selected.transform.position,
                typeData.LightningCrackFieldBoltColor,
                typeData.LightningCrackFieldBoltWidth,
                typeData.LightningCrackFieldBoltLifetime,
                typeData.LightningCrackFieldBoltSegments,
                typeData.LightningCrackFieldBoltNoise);
        }
    }

    private void ApplyLightningSnake(BallTypeData ballTypeData)
    {
        if (ballTypeData == null)
        {
            return;
        }

        int bounceCount = Mathf.Max(0, ballTypeData.LightningSnakeBounceCount);
        if (bounceCount <= 0)
        {
            return;
        }

        int snakeDamage = Mathf.Max(1, ballTypeData.LightningSnakeDamage);
        float snakeRadius = Mathf.Max(MinimumEffectRadius, ballTypeData.LightningSnakeRadius);
        int waterSplitCount = Mathf.Max(1, ballTypeData.LightningSnakeWaterSplitCount);
        float bounceDelay = Mathf.Max(0f, ballTypeData.LightningSnakeBounceDelay);

        StartCoroutine(ApplyLightningSnakeCoroutine(
            transform.position,
            this,
            bounceCount,
            snakeDamage,
            snakeRadius,
            waterSplitCount,
            bounceDelay,
            ballTypeData));
    }

    private IEnumerator ApplyLightningSnakeCoroutine(
        Vector3 startPosition,
        BrickController startExcludedBrick,
        int bounceCount,
        int snakeDamage,
        float snakeRadius,
        int waterSplitCount,
        float bounceDelay,
        BallTypeData visualData)
    {
        Queue<LightningSnakeNode> activeSnakes = new Queue<LightningSnakeNode>();
        List<BrickController> localNearbyBuffer = new List<BrickController>();
        activeSnakes.Enqueue(new LightningSnakeNode(startPosition, startExcludedBrick, bounceCount));

        while (activeSnakes.Count > 0)
        {
            int waveCount = activeSnakes.Count;
            bool spawnedNextWave = false;

            for (int i = 0; i < waveCount; i++)
            {
                LightningSnakeNode node = activeSnakes.Dequeue();
                if (node.RemainingBounces <= 0)
                {
                    continue;
                }

                BrickController nextTarget = GetRandomNearbyBrick(node.OriginPosition, node.ExcludedBrick, snakeRadius, localNearbyBuffer);
                if (nextTarget == null)
                {
                    continue;
                }

                nextTarget.ApplyDamage(snakeDamage);

                if (visualData != null)
                {
                    float boltLifetime = visualData.SnakeVisualMode == BallTypeData.LightningSnakeVisualMode.ContinuousTrail
                        ? node.RemainingBounces * bounceDelay + visualData.LightningSnakeBoltLifetime
                        : visualData.LightningSnakeBoltLifetime;
                    LightningBoltEffect.SpawnSnake(node.OriginPosition, nextTarget.transform.position, visualData, boltLifetime);
                }

                int remainingAfterHit = node.RemainingBounces - 1;
                if (remainingAfterHit <= 0)
                {
                    continue;
                }

                int branchCount = IsWaterBrick(nextTarget) ? waterSplitCount : 1;
                for (int branch = 0; branch < branchCount; branch++)
                {
                    activeSnakes.Enqueue(new LightningSnakeNode(nextTarget.transform.position, nextTarget, remainingAfterHit));
                    spawnedNextWave = true;
                }
            }

            if (spawnedNextWave && bounceDelay > 0f)
            {
                yield return new WaitForSeconds(bounceDelay);
            }
        }
    }

    private void ApplyBallTypeEffects(BallTypeData ballTypeData)
    {
        if (ballTypeData == null)
        {
            return;
        }

        if (ballTypeData.AppliesBurn)
        {
            ApplyBurn(ballTypeData.BurnDamage, ballTypeData.BurnTickInterval, ballTypeData.BurnHitCount);
        }

        if (ballTypeData.LightningBurst)
        {
            ApplyLightningBurst(ballTypeData);
        }

        if (ballTypeData.CreatesLightningSnake)
        {
            ApplyLightningSnake(ballTypeData);
        }

        if (ballTypeData.EarthCrack)
        {
            ApplyCrackHit(ballTypeData.ShatterDamage, ballTypeData.ShatterRadius);
        }

        // Tremor modifies crack-shatter propagation if a crack is already present.
        // Crack application itself is now explicit via EarthCrack/FertileLand settings.

        if (ballTypeData.CreatesAbrasion)
        {
            ApplyWeakened(ballTypeData.AbrasionWeakenDuration);
        }

        if (ballTypeData.CreatesCyclone)
        {
            ApplyCyclone(ballTypeData);
        }

        if (ballTypeData.AppliesRoot && currentHitPoints > 0)
        {
            ApplyRootToBrickAndAbove(ballTypeData.RootDuration, ballTypeData.RootSpeedMultiplier);
        }

        if (ballTypeData.CreatesCombustion)
        {
            ApplyCombustion(ballTypeData);
        }

        if (ballTypeData.CreatesFireSpread)
        {
            ApplyFireSpread(ballTypeData);
        }

        if (ballTypeData.CreatesForestFire)
        {
            ApplyForestFire(ballTypeData);
        }

        if (ballTypeData.CreatesElectricCascade)
        {
            ApplyConductive(ballTypeData);
        }

        if (ballTypeData.ImpactBurst)
        {
            ApplyImpactBurst(ballTypeData);
        }

        if (ballTypeData.CreatesCollapse)
        {
            ApplyCollapse(ballTypeData);
        }

        if (ballTypeData.CreatesPressurizedSplash)
        {
            ApplyPressurizedSplash(ballTypeData);
        }

        if (ballTypeData.CreatesSeed && ballTypeData.AppliesRoot)
        {
            ApplySeedRoot(ballTypeData, ballTypeData.SeedSpreadGenerations);
        }
    }

    private void CollectNearbyBricks(float radius, List<BrickController> results)
    {
        CollectNearbyBricks(transform.position, this, radius, results);
    }

    private void CollectNearbyBricks(Vector3 center, BrickController excludedBrick, float radius, List<BrickController> results)
    {
        results.Clear();

        Collider2D[] colliders = Physics2D.OverlapCircleAll(center, Mathf.Max(MinimumEffectRadius, radius));
        if (colliders == null || colliders.Length == 0)
        {
            return;
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D collider = colliders[i];
            if (collider == null || !collider.TryGetComponent<BrickController>(out BrickController nearbyBrick))
            {
                continue;
            }

            if (nearbyBrick == excludedBrick || nearbyBrick.CurrentHitPoints <= 0 || results.Contains(nearbyBrick))
            {
                continue;
            }

            results.Add(nearbyBrick);
        }
    }

    private BrickController GetRandomNearbyBrick(Vector3 originPosition, BrickController excludedBrick, float radius, List<BrickController> resultsBuffer)
    {
        if (resultsBuffer == null)
        {
            return null;
        }

        CollectNearbyBricks(originPosition, excludedBrick, radius, resultsBuffer);
        if (resultsBuffer.Count == 0)
        {
            return null;
        }

        int randomIndex = Random.Range(0, resultsBuffer.Count);
        return resultsBuffer[randomIndex];
    }

    private static bool IsWaterBrick(BrickController brick)
    {
        return brick != null
            && brick.typeData != null
            && brick.typeData.Type == BallTypeData.BallElement.Water;
    }

    private void CollectOrthogonalAdjacentBricks(BrickController origin, List<BrickController> results)
    {
        results.Clear();
        if (origin == null)
        {
            return;
        }

        Transform parent = origin.transform.parent;
        if (parent == null)
        {
            return;
        }

        float sourceWidth = origin.brickCollider != null ? origin.brickCollider.bounds.size.x : 1f;
        float sourceHeight = origin.brickCollider != null ? origin.brickCollider.bounds.size.y : 1f;
        float rowTolerance = Mathf.Max(MinimumColumnTolerance, sourceHeight * 0.35f);
        float columnTolerance = Mathf.Max(MinimumColumnTolerance, sourceWidth * 0.35f);
        float minHorizontalGap = sourceWidth * 0.45f;
        float maxHorizontalGap = sourceWidth * 1.6f;
        float minVerticalGap = sourceHeight * 0.45f;
        float maxVerticalGap = sourceHeight * 1.6f;

        Vector3 originPosition = origin.transform.position;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child == null || child == origin.transform)
            {
                continue;
            }

            if (!child.TryGetComponent<BrickController>(out BrickController adjacent) || adjacent.CurrentHitPoints <= 0)
            {
                continue;
            }

            float dx = Mathf.Abs(child.position.x - originPosition.x);
            float dy = Mathf.Abs(child.position.y - originPosition.y);

            bool sameRow = dy <= rowTolerance;
            bool sameColumn = dx <= columnTolerance;
            bool horizontalNeighbor = sameRow && dx >= minHorizontalGap && dx <= maxHorizontalGap;
            bool verticalNeighbor = sameColumn && dy >= minVerticalGap && dy <= maxVerticalGap;
            if (!horizontalNeighbor && !verticalNeighbor)
            {
                continue;
            }

            results.Add(adjacent);
        }
    }

    private void CollectCrossLineBricks(BrickController origin, List<BrickController> results)
    {
        results.Clear();
        if (origin == null)
        {
            return;
        }

        Transform parent = origin.transform.parent;
        if (parent == null)
        {
            return;
        }

        float sourceWidth = origin.brickCollider != null ? origin.brickCollider.bounds.size.x : 1f;
        float sourceHeight = origin.brickCollider != null ? origin.brickCollider.bounds.size.y : 1f;
        float rowTolerance = Mathf.Max(MinimumColumnTolerance, sourceHeight * 0.35f);
        float columnTolerance = Mathf.Max(MinimumColumnTolerance, sourceWidth * 0.35f);
        Vector3 originPosition = origin.transform.position;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child == null || child == origin.transform)
            {
                continue;
            }

            if (!child.TryGetComponent<BrickController>(out BrickController alignedBrick) || alignedBrick.CurrentHitPoints <= 0)
            {
                continue;
            }

            float dx = Mathf.Abs(child.position.x - originPosition.x);
            float dy = Mathf.Abs(child.position.y - originPosition.y);
            bool sameRow = dy <= rowTolerance;
            bool sameColumn = dx <= columnTolerance;

            if (!sameRow && !sameColumn)
            {
                continue;
            }

            results.Add(alignedBrick);
        }
    }

    private int GetLightningTargetBonus()
    {
        if (typeData == null || !typeData.AmplifiesLightning)
        {
            return 0;
        }

        return Mathf.Max(0, typeData.LightningTargetBonus);
    }

    private void TriggerDamageFlash()
    {
        if (!enableDamageFlash || spriteRenderer == null)
        {
            return;
        }

        if (damageFlashRoutine != null)
        {
            StopCoroutine(damageFlashRoutine);
        }

        damageFlashRoutine = StartCoroutine(DamageFlashCoroutine());
    }

    private IEnumerator DamageFlashCoroutine()
    {
        float duration = Mathf.Max(MinimumDurationSeconds, damageFlashDuration);
        float halfDuration = duration * 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float pulse01;
            if (elapsed <= halfDuration)
            {
                pulse01 = elapsed / halfDuration;
            }
            else
            {
                pulse01 = 1f - (elapsed - halfDuration) / halfDuration;
            }

            ApplyDamageFlashColor(Mathf.Clamp01(pulse01));
            yield return null;
        }

        SetBaseBrickColor();
        damageFlashRoutine = null;
    }

    private void ApplyDamageFlashColor(float pulse01)
    {
        Color baseColor = GetBaseBrickColor();
        Color pulseColor = damageFlashColor;
        pulseColor.a = baseColor.a;

        float weight = Mathf.Clamp01(damageFlashStrength) * Mathf.Clamp01(pulse01);
        Color result = Color.Lerp(baseColor, pulseColor, weight);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = result;
        }
    }

    private void SetBaseBrickColor()
    {
        Color baseColor = GetBaseBrickColor();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = baseColor;
        }
    }

    private Color GetBaseBrickColor()
    {
        Color baseColor = typeData != null
            ? typeData.DisplayColor
            : (spriteRenderer != null ? spriteRenderer.color : Color.white);
        float ratio = Mathf.Clamp01((float)currentHitPoints / Mathf.Max(1, maxHitPoints));
        baseColor.a = ratio;
        return baseColor;
    }

    private static float ClampEffectSpeedMultiplier(float speedMultiplier)
    {
        return Mathf.Clamp(speedMultiplier, MinimumRootSpeedMultiplier, 1f);
    }

    private float GetAppliedSpeedMultiplier()
    {
        float speedMultiplier = 1f;

        if (isRooted)
        {
            speedMultiplier = Mathf.Min(speedMultiplier, ClampEffectSpeedMultiplier(rootSpeedMultiplier));
        }

        if (hasColumnSlow)
        {
            speedMultiplier = Mathf.Min(speedMultiplier, ClampEffectSpeedMultiplier(columnSlowSpeedMultiplier));
        }

        if (hasSeedRoot)
        {
            speedMultiplier = Mathf.Min(speedMultiplier, ClampEffectSpeedMultiplier(seedRootSpeedMult));
        }

        return speedMultiplier;
    }

    private void ApplySeedRoot(BallTypeData ballTypeData, int generationsRemaining)
    {
        if (ballTypeData == null || !ballTypeData.AppliesRoot || generationsRemaining < 0)
        {
            return;
        }

        hasSeedRoot = true;
        seedRootTimeRemaining = Mathf.Max(MinimumDurationSeconds, ballTypeData.SeedRootDuration);
        seedRootSpeedMult = ClampEffectSpeedMultiplier(ballTypeData.SeedRootSpeedMultiplier);
        seedSpreadGenerationsRemaining = generationsRemaining;
        seedSpreadCount = Mathf.Max(1, ballTypeData.SeedSpreadCount);
        seedSpreadRadius = Mathf.Max(MinimumEffectRadius, ballTypeData.SeedSpreadRadius);
        seedRootDuration = Mathf.Max(MinimumDurationSeconds, ballTypeData.SeedRootDuration);
        seedRootBaseSpeedMult = ClampEffectSpeedMultiplier(ballTypeData.SeedRootSpeedMultiplier);
    }

    private void ApplySeedRootDirect(float duration, float speedMult, float spreadRadius, int spreadCount, int generationsRemaining)
    {
        hasSeedRoot = true;
        seedRootTimeRemaining = duration;
        seedRootSpeedMult = speedMult;
        seedSpreadGenerationsRemaining = generationsRemaining;
        seedSpreadCount = spreadCount;
        seedSpreadRadius = spreadRadius;
        seedRootDuration = duration;
        seedRootBaseSpeedMult = speedMult;
    }

    private void SpreadSeedRoot(int generationsLeft, int spreadCount, float spreadRadius, float duration, float speedMult)
    {
        seedSpreadCandidatesBuffer.Clear();
        CollectNearbyBricks(spreadRadius, seedSpreadCandidatesBuffer);

        // Remove bricks already seed-rooted so spread always reaches new bricks.
        for (int i = seedSpreadCandidatesBuffer.Count - 1; i >= 0; i--)
        {
            if (seedSpreadCandidatesBuffer[i].hasSeedRoot)
            {
                seedSpreadCandidatesBuffer.RemoveAt(i);
            }
        }

        int toSpread = Mathf.Min(spreadCount, seedSpreadCandidatesBuffer.Count);
        for (int i = 0; i < toSpread; i++)
        {
            int randomIndex = Random.Range(i, seedSpreadCandidatesBuffer.Count);
            BrickController target = seedSpreadCandidatesBuffer[randomIndex];
            seedSpreadCandidatesBuffer[randomIndex] = seedSpreadCandidatesBuffer[i];
            seedSpreadCandidatesBuffer[i] = target;

            target.ApplySeedRootDirect(duration, speedMult, spreadRadius, spreadCount, generationsLeft - 1);
        }
    }

    private void ClearSeedRoot()
    {
        hasSeedRoot = false;
        seedRootTimeRemaining = 0f;
        seedRootSpeedMult = 1f;
        seedSpreadGenerationsRemaining = 0;
        seedSpreadCount = 0;
        seedSpreadRadius = 0f;
        seedRootDuration = 0f;
        seedRootBaseSpeedMult = 1f;
    }

    private void InitializeEffectOverlays()
    {
        Transform overlayContainer;
        if (transform.childCount > 1)
        {
            overlayContainer = transform.GetChild(1);
        }
        else
        {
            GameObject containerObj = new GameObject("Overlays");
            containerObj.transform.SetParent(transform, false);
            containerObj.transform.localPosition = Vector3.zero;
            containerObj.transform.localRotation = Quaternion.identity;
            containerObj.transform.localScale = Vector3.one;
            overlayContainer = containerObj.transform;
        }

        int baseSortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder : 0;
        crackedOverlayRenderer = CreateOverlayRenderer(overlayContainer, "Cracked Overlay", baseSortingOrder + 1);
        burningOverlayRenderer = CreateOverlayRenderer(overlayContainer, "Burning Overlay", baseSortingOrder + 2);
        rootedOverlayRenderer = CreateOverlayRenderer(overlayContainer, "Rooted Overlay", baseSortingOrder + 3);
        pressureOverlayRenderer = CreateOverlayRenderer(overlayContainer, "Pressure Overlay", baseSortingOrder + 4);
        weakenedOverlayRenderer = CreateOverlayRenderer(overlayContainer, "Weakened Overlay", baseSortingOrder + 5);
        conductiveOverlayRenderer = CreateOverlayRenderer(overlayContainer, "Conductive Overlay", baseSortingOrder + 6);
    }

    private SpriteRenderer CreateOverlayRenderer(Transform parent, string objectName, int sortingOrder)
    {
        Transform overlayTransform = parent.Find(objectName);
        GameObject overlayObject;

        if (overlayTransform == null)
        {
            overlayObject = new GameObject(objectName);
            overlayObject.transform.SetParent(parent, false);
        }
        else
        {
            overlayObject = overlayTransform.gameObject;
        }

        overlayObject.layer = overlayLayer;

        overlayObject.transform.localPosition = Vector3.zero;
        overlayObject.transform.localRotation = Quaternion.identity;
        overlayObject.transform.localScale = new Vector3(overlayScale.x, overlayScale.y, 1f);

        SpriteRenderer overlayRenderer = overlayObject.GetComponent<SpriteRenderer>();
        if (overlayRenderer == null)
        {
            overlayRenderer = overlayObject.AddComponent<SpriteRenderer>();
        }

        overlayRenderer.sortingOrder = sortingOrder;
        overlayRenderer.enabled = false;
        return overlayRenderer;
    }

    private Sprite StepOverlayAnim(int slot, OverlayAnimation anim, Sprite fallback, bool isActive)
    {
        if (anim == null || anim.frames == null || anim.frames.Length == 0)
        {
            return fallback;
        }

        if (isActive)
        {
            overlayAnimTimers[slot] += Time.deltaTime;
            float frameDuration = 1f / Mathf.Max(0.1f, anim.frameRate);
            if (overlayAnimTimers[slot] >= frameDuration)
            {
                overlayAnimTimers[slot] -= frameDuration;
                overlayAnimFrames[slot] = (overlayAnimFrames[slot] + 1) % anim.frames.Length;
            }
        }

        return anim.frames[overlayAnimFrames[slot]];
    }

    private void UpdateEffectOverlays()
    {
        UpdateOverlay(crackedOverlayRenderer, StepOverlayAnim(0, crackedOverlayAnimation, crackedOverlaySprite, isCracked), isCracked ? 1f : 0f);

        float burnAlpha = 0f;
        if (isBurning && burnHitsRemaining > 0 && burnInitialHitCount > 0)
        {
            float intervalProgress = burnTickInterval > 0f ? Mathf.Clamp01(burnTickTimer / burnTickInterval) : 0f;
            float normalizedBurnRemaining = ((burnHitsRemaining - 1) + intervalProgress) / burnInitialHitCount;
            burnAlpha = Mathf.Clamp01(normalizedBurnRemaining);
        }

        UpdateOverlay(burningOverlayRenderer, StepOverlayAnim(1, burningOverlayAnimation, burningOverlaySprite, isBurning), burnAlpha);

        float rootedAlpha = 0f;
        if (isRooted && rootInitialDuration > 0f)
        {
            rootedAlpha = Mathf.Clamp01(rootTimeRemaining / rootInitialDuration);
        }

        UpdateOverlay(rootedOverlayRenderer, StepOverlayAnim(2, rootedOverlayAnimation, rootedOverlaySprite, isRooted), rootedAlpha);

        float pressureAlpha = 0f;
        if (hasPressurizedSplash && currentPressure > 0 && pressureMaxThreshold > 0)
        {
            float pressureRatio = Mathf.Clamp01((float)currentPressure / pressureMaxThreshold);
            pressureAlpha = Mathf.Lerp(0.25f, 1f, pressureRatio);
        }

        UpdateOverlay(pressureOverlayRenderer, StepOverlayAnim(3, pressureOverlayAnimation, pressureOverlaySprite, hasPressurizedSplash), pressureAlpha);

        float weakenedAlpha = 0f;
        if (isWeakened && weakenedInitialDuration > 0f)
        {
            weakenedAlpha = Mathf.Clamp01(weakenedTimeRemaining / weakenedInitialDuration);
        }

        float conductiveAlpha = 0f;
        if (hasConductive && conductiveInitialDuration > 0f)
        {
            conductiveAlpha = Mathf.Clamp01(conductiveTimeRemaining / conductiveInitialDuration);
        }

        UpdateOverlay(weakenedOverlayRenderer, StepOverlayAnim(4, weakenedOverlayAnimation, weakenedOverlaySprite, isWeakened), weakenedAlpha);
        UpdateOverlay(conductiveOverlayRenderer, StepOverlayAnim(5, conductiveOverlayAnimation, conductiveOverlaySprite, hasConductive), conductiveAlpha);
    }

    private static void UpdateOverlay(SpriteRenderer overlayRenderer, Sprite overlaySprite, float normalizedAlpha)
    {
        if (overlayRenderer == null)
        {
            return;
        }

        if (overlaySprite == null || normalizedAlpha <= 0f)
        {
            overlayRenderer.enabled = false;
            return;
        }

        overlayRenderer.enabled = true;
        overlayRenderer.sprite = overlaySprite;
        Color color = Color.white;
        color.a = Mathf.Clamp01(normalizedAlpha);
        overlayRenderer.color = color;
    }
}
