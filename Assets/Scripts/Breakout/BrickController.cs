using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BrickController : MonoBehaviour
{
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
    private SpriteRenderer spriteRenderer;
    private Vector3 targetScale;
    private bool isGrowing;
    private bool isBurning;
    private int burnDamage;
    private float burnTickInterval;
    private float burnTickTimer;
    private int burnHitsRemaining;
    private Coroutine lightningPulseRoutine;

    public int CurrentHitPoints => currentHitPoints;
    public BrickTypeData TypeData => typeData;

    private void Awake()
    {
        targetScale = transform.localScale;
        transform.localScale = new Vector3(0f, 0f, 1f);
        isGrowing = true;

        spriteRenderer = GetComponent<SpriteRenderer>();
        ApplyTypeData();
    }

    private void Update()
    {
        UpdateSpawnGrowth();

        UpdateBurning();

        if (!moveDownward || downwardSpeed <= 0f)
        {
            return;
        }

        transform.position += Vector3.down * downwardSpeed * Time.deltaTime;
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

        maxHitPoints = Mathf.Max(1, typeData.HitPoints);
        currentHitPoints = maxHitPoints;
        ClearBurn();

        if (spriteRenderer != null)
        {
            spriteRenderer.color = typeData.DisplayColor;
        }

        UpdateHealthAlpha();
    }

    public void ApplyBallHit(BallController ball)
    {
        if (ball == null)
        {
            return;
        }

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

        if (ball.TypeData != null && !ball.TypeData.CollideWithBricks)
        {
            return;
        }

        HandleBallHit(ball);
    }

    protected virtual void HandleBallHit(BallController ball)
    {
        int damage = GetDamageFromBall(ball);
        ApplyDamage(damage);

        if (ball != null && ball.TypeData != null && ball.TypeData.AppliesBurn)
        {
            ApplyBurn(ball.TypeData.BurnDamage, ball.TypeData.BurnTickInterval, ball.TypeData.BurnHitCount);
        }

        if (ball != null && ball.TypeData != null && ball.TypeData.LightningBurst)
        {
            ApplyLightningBurst(ball.TypeData);
        }
    }

    protected virtual int GetDamageFromBall(BallController ball)
    {
        if (typeData == null || ball == null || ball.TypeData == null)
        {
            return 1;
        }

        return ball.TypeData.IsStrongAgainst(typeData.Type) ? 2 : 1;
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
        if (typeData != null && typeData.Flammable)
        {
            return burnDamage * 2;
        }

        return burnDamage;
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

        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, burstRadius);
        if (colliders == null || colliders.Length == 0)
        {
            return;
        }

        List<BrickController> candidates = new List<BrickController>(colliders.Length);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D collider = colliders[i];
            if (collider == null || !collider.TryGetComponent<BrickController>(out BrickController nearbyBrick))
            {
                continue;
            }

            if (nearbyBrick == this || nearbyBrick.CurrentHitPoints <= 0 || candidates.Contains(nearbyBrick))
            {
                continue;
            }

            candidates.Add(nearbyBrick);
        }

        if (candidates.Count == 0)
        {
            return;
        }

        int burstHits = Mathf.Min(targetCount, candidates.Count);
        for (int i = 0; i < burstHits; i++)
        {
            int randomIndex = Random.Range(i, candidates.Count);
            BrickController selected = candidates[randomIndex];
            candidates[randomIndex] = candidates[i];
            candidates[i] = selected;

            selected.TriggerLightningPulse();
            selected.ApplyDamage(burstDamage);
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
