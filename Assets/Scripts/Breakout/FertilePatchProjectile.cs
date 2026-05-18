using UnityEngine;

[DisallowMultipleComponent]
public class FertilePatchProjectile : MonoBehaviour
{
    private const float MinimumLifetimeSeconds = 0.1f;
    private const float MinimumSize = 0.05f;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private CircleCollider2D hitbox;
    private float riseSpeed;
    private float lifetimeRemaining;
    private int crackShatterDamage;
    private float crackShatterRadius;
    private bool appliesCrack;
    private float rootRadius;
    private float rootDuration;
    private float rootSpeedMultiplier;
    private bool appliesRoot;
    private bool isMovementLocked;

    public static FertilePatchProjectile Spawn(BallTypeData sourceTypeData, Vector3 position, float sourceScale, Collider2D ignoredCollider)
    {
        if (sourceTypeData == null || !sourceTypeData.CreatesFertileLand)
        {
            return null;
        }

        GameObject projectileObject = new GameObject("Fertile Patch Projectile");
        projectileObject.transform.position = position;

        FertilePatchProjectile projectile = projectileObject.AddComponent<FertilePatchProjectile>();
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
        riseSpeed = Mathf.Max(0f, sourceTypeData.FertilePatchRiseSpeed);
        lifetimeRemaining = Mathf.Max(MinimumLifetimeSeconds, sourceTypeData.FertilePatchLifetime);
        crackShatterDamage = Mathf.Max(1, sourceTypeData.FertilePatchCrackShatterDamage);
        crackShatterRadius = Mathf.Max(0.1f, sourceTypeData.FertilePatchCrackShatterRadius);
        appliesCrack = sourceTypeData.EarthCrack;
        rootRadius = Mathf.Max(0.1f, sourceTypeData.FertilePatchRootRadius);
        rootDuration = Mathf.Max(MinimumLifetimeSeconds, sourceTypeData.FertilePatchRootDuration);
        rootSpeedMultiplier = Mathf.Clamp01(sourceTypeData.FertilePatchRootSpeedMultiplier);
        appliesRoot = sourceTypeData.AppliesRoot;

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sourceTypeData.FertilePatchSprite != null ? sourceTypeData.FertilePatchSprite : sourceTypeData.BallSprite;
            spriteRenderer.color = sourceTypeData.FertilePatchColor;
            spriteRenderer.sortingOrder = 2;
        }

        float clampedScale = Mathf.Max(MinimumSize, sourceScale * Mathf.Max(MinimumSize, sourceTypeData.FertilePatchSizeMultiplier));
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

        brick.ApplyFertileLandPatch(
            appliesCrack,
            crackShatterDamage,
            crackShatterRadius,
            appliesRoot,
            rootRadius,
            rootDuration,
            rootSpeedMultiplier);
        Destroy(gameObject);
    }
}
