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
    private Sprite[] animSprites;
    private float animFrameRate;
    private int animCurrentFrame;
    private float animFrameTimer;

    public static FertilePatchProjectile Spawn(
        Vector3 position,
        Collider2D ignoredCollider,
        Sprite sprite,
        Sprite[] animSprites,
        float animFrameRate,
        Color color,
        float scale,
        float riseSpeed,
        float lifetime,
        bool appliesCrack,
        int crackShatterDamage,
        float crackShatterRadius,
        bool appliesRoot,
        float rootRadius,
        float rootDuration,
        float rootSpeedMultiplier)
    {
        GameObject projectileObject = new GameObject("Fertile Patch Projectile");
        projectileObject.transform.position = position;

        FertilePatchProjectile projectile = projectileObject.AddComponent<FertilePatchProjectile>();
        projectile.Initialize(sprite, animSprites, animFrameRate, color, scale, riseSpeed, lifetime, appliesCrack, crackShatterDamage, crackShatterRadius, appliesRoot, rootRadius, rootDuration, rootSpeedMultiplier, ignoredCollider);
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
        bool appliesCrackValue,
        int crackShatterDamageValue,
        float crackShatterRadiusValue,
        bool appliesRootValue,
        float rootRadiusValue,
        float rootDurationValue,
        float rootSpeedMultiplierValue,
        Collider2D ignoredCollider)
    {
        riseSpeed = Mathf.Max(0f, riseSpeedValue);
        lifetimeRemaining = Mathf.Max(MinimumLifetimeSeconds, lifetime);
        crackShatterDamage = Mathf.Max(1, crackShatterDamageValue);
        crackShatterRadius = Mathf.Max(0.1f, crackShatterRadiusValue);
        appliesCrack = appliesCrackValue;
        rootRadius = Mathf.Max(0.1f, rootRadiusValue);
        rootDuration = Mathf.Max(MinimumLifetimeSeconds, rootDurationValue);
        rootSpeedMultiplier = Mathf.Clamp01(rootSpeedMultiplierValue);
        appliesRoot = appliesRootValue;

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
