using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BrickController : MonoBehaviour
{
    private const float MinimumRootSpeedMultiplier = 0.05f;

    [SerializeField] private BrickTypeData typeData;

    [Header("Spawn Animation")]
    [SerializeField] private float growthSpeed = 6f;

    [Header("Movement")]
    [SerializeField] private bool moveDownward;
    [SerializeField] private float downwardSpeed;

    [Header("Lightning VFX")]
    [SerializeField] private bool enableLightningPulse = true;
    [SerializeField] private Color lightningPulseColor = new Color(0.7f, 0.95f, 1f, 1f);
    [SerializeField, Min(0.01f)] private float lightningPulseDuration = 0.12f;
    [SerializeField, Range(0f, 1f)] private float lightningPulseStrength = 0.9f;

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
    private Coroutine lightningPulseRoutine;
    private readonly List<BrickController> nearbyBricksBuffer = new List<BrickController>();

    public static event System.Action<BrickController, int> BrickDestroyed;

    public int CurrentHitPoints => currentHitPoints;
    public BrickTypeData TypeData => typeData;
    public float DownwardSpeed => downwardSpeed;

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
        moveDownward = enabled;
        SetDownwardSpeed(speed);
    }

    public void SetDownwardSpeed(float speed)
    {
        downwardSpeed = Mathf.Max(0f, speed);
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
        currentHitPoints -= Mathf.Max(0, amount);

        UpdateHealthAlpha();

        if (currentHitPoints <= 0)
        {
            OnBrickDestroyed();
            Destroy(gameObject);
        }
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
        burnTickInterval = Mathf.Max(0.01f, tickInterval);
        burnTickTimer = burnTickInterval;
        burnHitsRemaining = hitCount;
    }

    private void ClearBurn()
    {
        isBurning = false;
        burnDamage = 0;
        burnTickInterval = 0f;
        burnTickTimer = 0f;
        burnHitsRemaining = 0;
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
        float shatterRadius = Mathf.Max(0.1f, radius);
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

        float clampedSpeedMultiplier = Mathf.Clamp(speedMultiplier, MinimumRootSpeedMultiplier, 1f);
        float yThreshold = transform.position.y - 0.01f;
        float xTolerance = GetColumnTolerance();

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

            brick.ApplyColumnSlow(clampedDuration, clampedSpeedMultiplier);
        }
    }

    private void ApplyRoot(float duration, float speedMultiplier)
    {
        isRooted = true;
        rootTimeRemaining = Mathf.Max(0f, duration);
        rootSpeedMultiplier = Mathf.Clamp(speedMultiplier, MinimumRootSpeedMultiplier, 1f);
    }

    private void ApplyColumnSlow(float duration, float speedMultiplier)
    {
        hasColumnSlow = true;
        columnSlowTimeRemaining = Mathf.Max(0f, duration);
        columnSlowSpeedMultiplier = Mathf.Clamp(speedMultiplier, MinimumRootSpeedMultiplier, 1f);
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
        float speedMultiplier = 1f;
        if (isRooted)
        {
            speedMultiplier = Mathf.Min(speedMultiplier, Mathf.Clamp(rootSpeedMultiplier, MinimumRootSpeedMultiplier, 1f));
        }

        if (hasColumnSlow)
        {
            speedMultiplier = Mathf.Min(speedMultiplier, Mathf.Clamp(columnSlowSpeedMultiplier, MinimumRootSpeedMultiplier, 1f));
        }

        return downwardSpeed * speedMultiplier;
    }

    private float GetColumnTolerance()
    {
        if (brickCollider != null)
        {
            return Mathf.Max(0.05f, brickCollider.bounds.extents.x * 0.7f);
        }

        return 0.6f;
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
        TriggerLightningPulse();

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

            selected.TriggerLightningPulse();
            selected.ApplyDamage(burstDamage);
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

        if (ballTypeData.EarthCrack)
        {
            ApplyEarthCrackHit(ballTypeData);
        }

        if (ballTypeData.AppliesRoot)
        {
            ApplyRootToBrickAndAbove(ballTypeData.RootDuration, ballTypeData.RootSpeedMultiplier);
        }
    }

    private void CollectNearbyBricks(float radius, List<BrickController> results)
    {
        results.Clear();

        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, Mathf.Max(0.1f, radius));
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

            if (nearbyBrick == this || nearbyBrick.CurrentHitPoints <= 0 || results.Contains(nearbyBrick))
            {
                continue;
            }

            results.Add(nearbyBrick);
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

    private void TriggerLightningPulse()
    {
        if (!enableLightningPulse || spriteRenderer == null)
        {
            return;
        }

        if (lightningPulseRoutine != null)
        {
            StopCoroutine(lightningPulseRoutine);
        }

        lightningPulseRoutine = StartCoroutine(LightningPulseCoroutine());
    }

    private IEnumerator LightningPulseCoroutine()
    {
        float duration = Mathf.Max(0.01f, lightningPulseDuration);
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

            ApplyLightningPulseColor(Mathf.Clamp01(pulse01));
            yield return null;
        }

        SetBaseBrickColor();
        lightningPulseRoutine = null;
    }

    private void ApplyLightningPulseColor(float pulse01)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        Color baseColor = GetBaseBrickColor();
        Color pulseColor = lightningPulseColor;
        pulseColor.a = baseColor.a;

        float weight = Mathf.Clamp01(lightningPulseStrength) * Mathf.Clamp01(pulse01);
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
}
