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
    private Sprite[] animSprites;
    private float animFrameRate;
    private int animCurrentFrame;
    private float animFrameTimer;

    public static FlameTrailProjectile Spawn(
        Vector3 position,
        Collider2D ignoredCollider,
        Sprite sprite,
        Sprite[] animSprites,
        float animFrameRate,
        Color color,
        float scale,
        float riseSpeed,
        float lifetime,
        int impactDamage,
        int burnDamage,
        float burnTickInterval,
        int burnHitCount)
    {
        GameObject projectileObject = new GameObject("Flame Trail Projectile");
        projectileObject.transform.position = position;

        FlameTrailProjectile projectile = projectileObject.AddComponent<FlameTrailProjectile>();
        projectile.Initialize(sprite, animSprites, animFrameRate, color, scale, riseSpeed, lifetime, impactDamage, burnDamage, burnTickInterval, burnHitCount, ignoredCollider);
        return projectile;
    }

    private void Awake()
    {
        BreakoutEffectUtility.EnsureProjectileComponents(gameObject, out spriteRenderer, out rb, out hitbox, 0.5f);
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

        UpdateSpriteAnimation();
        transform.position += Vector3.up * Mathf.Max(0f, riseSpeed) * Time.deltaTime;
    }

    public void StopMovement()
    {
        isMovementLocked = true;
    }

    public void ApplyLevelCompletePauseVisual(float grayscaleBlend, float alphaMultiplier)
    {
        BreakoutEffectUtility.ApplyPauseVisual(spriteRenderer, grayscaleBlend, alphaMultiplier);
    }

    private void Initialize(
        Sprite sprite,
        Sprite[] animFrames,
        float frameRate,
        Color color,
        float scale,
        float riseSpeedValue,
        float lifetime,
        int impactDamageValue,
        int burnDamageValue,
        float burnTickIntervalValue,
        int burnHitCountValue,
        Collider2D ignoredCollider)
    {
        riseSpeed = Mathf.Max(0f, riseSpeedValue);
        lifetimeRemaining = Mathf.Max(MinimumLifetimeSeconds, lifetime);
        impactDamage = Mathf.Max(0, impactDamageValue);
        burnDamage = Mathf.Max(1, burnDamageValue);
        burnTickInterval = Mathf.Max(MinimumLifetimeSeconds, burnTickIntervalValue);
        burnHitCount = Mathf.Max(1, burnHitCountValue);

        if (animFrames != null && animFrames.Length > 1)
        {
            animSprites = animFrames;
            animFrameRate = Mathf.Max(0.01f, frameRate);
        }

        if (spriteRenderer != null)
        {
            Sprite resolvedSprite = (animSprites != null && animSprites.Length > 0 && animSprites[0] != null)
                ? animSprites[0]
                : sprite;
            spriteRenderer.sprite = resolvedSprite;
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = 2;
        }

        transform.localScale = Vector3.one * Mathf.Max(MinimumSize, scale);

        if (ignoredCollider != null && hitbox != null)
        {
            Physics2D.IgnoreCollision(hitbox, ignoredCollider, true);
        }
    }

    private void UpdateSpriteAnimation()
    {
        BreakoutEffectUtility.AdvanceSpriteAnimation(
            spriteRenderer,
            animSprites,
            animFrameRate,
            ref animFrameTimer,
            ref animCurrentFrame);
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

        brick.ApplyExternalBurn(burnDamage, burnTickInterval, burnHitCount);

        Destroy(gameObject);
    }
}