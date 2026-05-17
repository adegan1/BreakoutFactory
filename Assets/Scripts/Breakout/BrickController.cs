using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BrickController : MonoBehaviour
{
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

    private const float MinimumRootSpeedMultiplier = 0.05f;
    private const float MinimumDurationSeconds = 0.01f;
    private const float MinimumEffectRadius = 0.1f;
    private const float RootColumnYThresholdOffset = 0.01f;
    private const float MinimumColumnTolerance = 0.05f;
    private const float DefaultColumnTolerance = 0.6f;
    private const float RowSpacingSafetyMultiplier = 1.1f;
    private const float DefaultRowSpacing = 1.2f;

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

    private int currentHitPoints;
    private int maxHitPoints;
    private int overrideHitPoints = -1;
    private SpriteRenderer spriteRenderer;
    private Collider2D brickCollider;
    private Vector3 targetScale;
    private bool isGrowing;
    private bool isBurning;
    private bool isCracked;
    private bool isRooted;
    private bool hasColumnSlow;
    private bool hasCombustion;
    private bool combustionExplosionTriggered;
    private int combustionExplosionDamage;
    private float combustionExplosionRadius;
    private float nextFireSpreadAllowedTime;
    private bool hasForestFire;
    private int forestFireSpreadGenerationsRemaining;
    private int crackShatterDamage = 1;
    private float crackShatterRadius = 1f;
    private int burnDamage;
    private float burnTickInterval;
    private float burnTickTimer;
    private int burnHitsRemaining;
    private float rootTimeRemaining;
    private float rootSpeedMultiplier = 1f;
    private float columnSlowTimeRemaining;
    private float columnSlowSpeedMultiplier = 1f;
    private Coroutine damageFlashRoutine;
    private Coroutine dangerSequenceRoutine;
    private readonly List<BrickController> nearbyBricksBuffer = new List<BrickController>();
    private bool inDangerSequence;
    private Vector3 dangerBasePosition;

    public static event System.Action<BrickController, int> BrickDestroyed;
    public static event System.Action<BrickController> BrickRemovedByDanger;

    public int CurrentHitPoints => currentHitPoints;
    public BrickTypeData TypeData => typeData;
    public float DownwardSpeed => downwardSpeed;
    public bool IsPinnedInPlace => inDangerSequence
        || (isRooted && rootSpeedMultiplier <= MinimumRootSpeedMultiplier)
        || (hasColumnSlow && columnSlowSpeedMultiplier <= MinimumRootSpeedMultiplier);
    public bool IsEffectivelyStopped => IsPinnedInPlace || IsBlockedBelowByStoppedBrick();

    private void Awake()
    {
        targetScale = transform.localScale;
        transform.localScale = new Vector3(0f, 0f, 1f);
        isGrowing = true;

        spriteRenderer = GetComponent<SpriteRenderer>();
        brickCollider = GetComponent<Collider2D>();
        ApplyTypeData();
    }

    private void Update()
    {
        UpdateSpawnGrowth();

        UpdateBurning();
        UpdateRooting();
        UpdateColumnSlowing();

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
            return;
        }

        moveDownward = enabled;
        SetDownwardSpeed(speed);
    }

    public void SetDownwardSpeed(float speed)
    {
        downwardSpeed = Mathf.Max(0f, speed);
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
        ClearEarthCrack();
        ClearRoot();
        ClearColumnSlow();
        ClearCombustion();
        nextFireSpreadAllowedTime = 0f;
        ClearForestFire();

        if (spriteRenderer != null)
        {
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

        BallTypeData ballTypeData = ball.TypeData;
        bool wasCrackedBeforeHit = isCracked;

        int damage = GetDamageFromBall(ball);
        ApplyDamage(damage);

        ball.TrySpawnWaterDropsFromBrickHit();

        if (wasCrackedBeforeHit)
        {
            TriggerStoredCrackShatter();
        }

        ApplyBallTypeEffects(ballTypeData);

        ball.FinalizeBrickHit();
    }

    protected virtual int GetDamageFromBall(BallController ball)
    {
        if (ball == null || ball.TypeData == null)
        {
            return 1;
        }

        return Mathf.Max(1, ball.TypeData.Damage);
    }

    protected virtual void ApplyDamage(int amount)
    {
        int clampedAmount = Mathf.Max(0, amount);
        if (clampedAmount <= 0)
        {
            return;
        }

        currentHitPoints -= clampedAmount;

        UpdateHealthAlpha();
        TriggerDamageFlash();

        if (currentHitPoints <= 0)
        {
            TryTriggerCombustionExplosionOnDestroyed();
            TrySpreadForestFireOnDestroyed();
            OnBrickDestroyed();
            Destroy(gameObject);
        }
    }

    public void ApplyDirectEffectDamage(int amount)
    {
        ApplyDamage(amount);
    }

    private void UpdateHealthAlpha()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        float ratio = Mathf.Clamp01((float)currentHitPoints / Mathf.Max(1, maxHitPoints));
        Color color = spriteRenderer.color;
        color.a = ratio;
        spriteRenderer.color = color;
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
        ApplyDamage(GetBurnDamage());

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
        int sourceDamage = sourceBallTypeData != null && sourceBallTypeData.AppliesBurn
            ? sourceBallTypeData.BurnDamage
            : 1;
        float sourceInterval = sourceBallTypeData != null && sourceBallTypeData.AppliesBurn
            ? sourceBallTypeData.BurnTickInterval
            : 0.5f;
        int sourceHits = sourceBallTypeData != null && sourceBallTypeData.AppliesBurn
            ? sourceBallTypeData.BurnHitCount
            : 1;

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

    private void ApplyEarthCrackHit(BallTypeData ballTypeData)
    {
        if (ballTypeData == null)
        {
            return;
        }

        if (isCracked)
        {
            return;
        }

        isCracked = true;
        crackShatterDamage = Mathf.Max(1, ballTypeData.ShatterDamage);
        crackShatterRadius = Mathf.Max(0.1f, ballTypeData.ShatterRadius);
    }

    private void TriggerStoredCrackShatter()
    {
        if (!isCracked)
        {
            return;
        }

        isCracked = false;
        TriggerShatter(crackShatterDamage, crackShatterRadius);
    }

    private void TriggerShatter(int damage, float radius)
    {
        int shatterDamage = Mathf.Max(1, damage);
        float shatterRadius = Mathf.Max(MinimumEffectRadius, radius);
        CollectNearbyBricks(shatterRadius, nearbyBricksBuffer);
        if (nearbyBricksBuffer.Count == 0)
        {
            return;
        }

        for (int i = 0; i < nearbyBricksBuffer.Count; i++)
        {
            BrickController nearbyBrick = nearbyBricksBuffer[i];
            nearbyBrick.ApplyDamage(shatterDamage);
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
        isRooted = true;
        rootTimeRemaining = Mathf.Max(0f, duration);
        rootSpeedMultiplier = ClampEffectSpeedMultiplier(speedMultiplier);
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
        if (IsPinnedInPlace)
            return 0f;

        if (IsBlockedBelowByStoppedBrick())
            return 0f;

        return downwardSpeed * GetAppliedSpeedMultiplier();
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
        if (transform.parent == null)
            return false;

        float maxGap = GetRowSpacingEstimate();
        float xTolerance = GetColumnTolerance();

        for (int i = 0; i < transform.parent.childCount; i++)
        {
            Transform child = transform.parent.GetChild(i);
            if (child == null || child == transform)
                continue;

            float dy = transform.position.y - child.position.y;
            if (dy <= 0f || dy > maxGap)
                continue;

            if (Mathf.Abs(child.position.x - transform.position.x) > xTolerance)
                continue;

            if (child.TryGetComponent<BrickController>(out BrickController brick) && brick.IsEffectivelyStopped)
                return true;
        }

        return false;
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
            bounceDelay));
    }

    private IEnumerator ApplyLightningSnakeCoroutine(
        Vector3 startPosition,
        BrickController startExcludedBrick,
        int bounceCount,
        int snakeDamage,
        float snakeRadius,
        int waterSplitCount,
        float bounceDelay)
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
            ApplyEarthCrackHit(ballTypeData);
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

        if (ballTypeData.ImpactBurst)
        {
            ApplyImpactBurst(ballTypeData);
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

    private BrickController GetRandomNearbyBrick(BrickController origin, float radius)
    {
        if (origin == null)
        {
            return null;
        }

        return GetRandomNearbyBrick(origin.transform.position, origin, radius, nearbyBricksBuffer);
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
                pulse01 = halfDuration <= 0f ? 1f : elapsed / halfDuration;
            }
            else
            {
                float downElapsed = elapsed - halfDuration;
                pulse01 = 1f - (halfDuration <= 0f ? 1f : downElapsed / halfDuration);
            }

            ApplyDamageFlashColor(Mathf.Clamp01(pulse01));
            yield return null;
        }

        SetBaseBrickColor();
        damageFlashRoutine = null;
    }

    private void ApplyDamageFlashColor(float pulse01)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        Color baseColor = GetBaseBrickColor();
        Color pulseColor = damageFlashColor;
        pulseColor.a = baseColor.a;

        float weight = Mathf.Clamp01(damageFlashStrength) * Mathf.Clamp01(pulse01);
        spriteRenderer.color = Color.Lerp(baseColor, pulseColor, weight);
    }

    private void SetBaseBrickColor()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.color = GetBaseBrickColor();
    }

    private Color GetBaseBrickColor()
    {
        Color baseColor = typeData != null ? typeData.DisplayColor : spriteRenderer.color;
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

        return speedMultiplier;
    }
}
