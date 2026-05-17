using UnityEngine;

[DisallowMultipleComponent]
public class FlameTrailProjectile : MonoBehaviour
{
    private const float MinimumLifetimeSeconds = 0.1f;
    private const float MinimumSize = 0.05f;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private CircleCollider2D hitbox;
    private float riseSpeed;
    private float lifetimeRemaining;
    private float burnTickInterval;
    private int impactDamage;
    private int burnDamage;
    private int burnHitCount;
    private bool isMovementLocked;

    public static FlameTrailProjectile Spawn(BallTypeData sourceTypeData, Vector3 position, float sourceScale, Collider2D ignoredCollider)
    {
        if (sourceTypeData == null || !sourceTypeData.CreatesFlameTrail)
        {
            return null;
        }

        GameObject projectileObject = new GameObject("Flame Trail Projectile");
        projectileObject.transform.position = position;

        FlameTrailProjectile projectile = projectileObject.AddComponent<FlameTrailProjectile>();
        projectile.Initialize(sourceTypeData, sourceScale, ignoredCollider);
        return projectile;
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.freezeRotation = true;

        hitbox = GetComponent<CircleCollider2D>();
        if (hitbox == null)
        {
            hitbox = gameObject.AddComponent<CircleCollider2D>();
        }

        hitbox.isTrigger = true;
        hitbox.radius = 0.5f;
    }

    private void Update()
    {
        if (isMovementLocked)
        {
            return;
        }

        lifetimeRemaining -= Time.deltaTime;
        if (lifetimeRemaining <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += Vector3.up * Mathf.Max(0f, riseSpeed) * Time.deltaTime;
    }

    public void StopMovement()
    {
        isMovementLocked = true;
    }

    public void ApplyLevelCompletePauseVisual(float grayscaleBlend, float alphaMultiplier)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        Color baseColor = spriteRenderer.color;
        float gray = baseColor.grayscale;
        Color pausedColor = new Color(gray, gray, gray, baseColor.a * Mathf.Clamp01(alphaMultiplier));
        spriteRenderer.color = Color.Lerp(baseColor, pausedColor, Mathf.Clamp01(grayscaleBlend));
    }

    private void Initialize(BallTypeData sourceTypeData, float sourceScale, Collider2D ignoredCollider)
    {
        riseSpeed = Mathf.Max(0f, sourceTypeData.FlameTrailRiseSpeed);
        lifetimeRemaining = Mathf.Max(MinimumLifetimeSeconds, sourceTypeData.FlameTrailLifetime);
        impactDamage = Mathf.Max(0, sourceTypeData.FlameTrailImpactDamage);
        burnDamage = Mathf.Max(1, sourceTypeData.FlameTrailBurnDamage);
        burnTickInterval = Mathf.Max(MinimumLifetimeSeconds, sourceTypeData.FlameTrailBurnTickInterval);
        burnHitCount = Mathf.Max(1, sourceTypeData.FlameTrailBurnHitCount);

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sourceTypeData.FlameTrailSprite != null ? sourceTypeData.FlameTrailSprite : sourceTypeData.BallSprite;
            spriteRenderer.color = sourceTypeData.FlameTrailColor;
            spriteRenderer.sortingOrder = 2;
        }

        float clampedScale = Mathf.Max(MinimumSize, sourceScale * Mathf.Max(MinimumSize, sourceTypeData.FlameTrailSizeMultiplier));
        transform.localScale = Vector3.one * clampedScale;

        if (ignoredCollider != null && hitbox != null)
        {
            Physics2D.IgnoreCollision(hitbox, ignoredCollider, true);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isMovementLocked || other == null)
        {
            return;
        }

        if (!other.TryGetComponent<BrickController>(out BrickController brick))
        {
            return;
        }

        if (impactDamage > 0)
        {
            brick.ApplyDirectEffectDamage(impactDamage);
        }

        if (brick != null)
        {
            brick.ApplyExternalBurn(burnDamage, burnTickInterval, burnHitCount);
        }

        Destroy(gameObject);
    }
}