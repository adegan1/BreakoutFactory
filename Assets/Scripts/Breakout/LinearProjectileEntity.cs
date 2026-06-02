using UnityEngine;

[DisallowMultipleComponent]
public class LinearProjectileEntity : MonoBehaviour
{
    private const float MinimumSize = 0.05f;
    private const float DefaultLifetime = 10f;
    private const float RootOnlyRadius = 0.5f;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private CircleCollider2D hitbox;
    private float moveSpeed;
    private Vector2 moveDirection;
    private float lifetimeRemaining;
    private int impactDamage;
    private bool appliesBurn;
    private int burnDamage;
    private float burnTickInterval;
    private int burnHitCount;
    private bool appliesCrack;
    private int crackShatterDamage;
    private float crackShatterRadius;
    private bool appliesRoot;
    private float rootDuration;
    private float rootSpeedMultiplier;
    private int hitsRemaining;
    private Sprite[] animSprites;
    private float animFrameRate;
    private int animCurrentFrame;
    private float animFrameTimer;

    public static LinearProjectileEntity Spawn(
        Vector3 position,
        Vector2 direction,
        Collider2D ignoredCollider,
        Sprite sprite,
        Sprite[] animSprites,
        float animFrameRate,
        Color color,
        float size,
        float moveSpeed,
        int impactDamage,
        bool appliesBurn,
        int burnDamage,
        float burnTickInterval,
        int burnHitCount,
        bool appliesCrack,
        int crackShatterDamage,
        float crackShatterRadius,
        bool appliesRoot,
        float rootDuration,
        float rootSpeedMultiplier,
        int hitsBeforeDestroy)
    {
        GameObject obj = new GameObject("Linear Projectile");
        obj.transform.position = position;

        LinearProjectileEntity entity = obj.AddComponent<LinearProjectileEntity>();
        entity.Initialize(sprite, animSprites, animFrameRate, color, size, direction, moveSpeed, impactDamage, appliesBurn, burnDamage, burnTickInterval, burnHitCount, appliesCrack, crackShatterDamage, crackShatterRadius, appliesRoot, rootDuration, rootSpeedMultiplier, hitsBeforeDestroy, ignoredCollider);
        return entity;
    }

    private void Awake()
    {
        BreakoutEffectUtility.EnsureProjectileComponents(gameObject, out spriteRenderer, out rb, out hitbox, 0.5f);
    }

    private void Update()
    {
        lifetimeRemaining -= Time.deltaTime;
        if (lifetimeRemaining <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        UpdateSpriteAnimation();
        transform.position += (Vector3)(moveDirection * moveSpeed * Time.deltaTime);
    }

    public void StopMovement()
    {
        moveSpeed = 0f;
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
        float size,
        Vector2 direction,
        float moveSpeedValue,
        int impactDamageValue,
        bool appliesBurnValue,
        int burnDamageValue,
        float burnTickIntervalValue,
        int burnHitCountValue,
        bool appliesCrackValue,
        int crackShatterDamageValue,
        float crackShatterRadiusValue,
        bool appliesRootValue,
        float rootDurationValue,
        float rootSpeedMultiplierValue,
        int hitsBeforeDestroyValue,
        Collider2D ignoredCollider)
    {
        moveSpeed = Mathf.Max(0f, moveSpeedValue);
        moveDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.up;
        lifetimeRemaining = DefaultLifetime;

        impactDamage = Mathf.Max(0, impactDamageValue);

        appliesBurn = appliesBurnValue;
        if (appliesBurn)
        {
            burnDamage = Mathf.Max(1, burnDamageValue);
            burnTickInterval = Mathf.Max(0.01f, burnTickIntervalValue);
            burnHitCount = Mathf.Max(1, burnHitCountValue);
        }

        appliesCrack = appliesCrackValue;
        if (appliesCrack)
        {
            crackShatterDamage = Mathf.Max(1, crackShatterDamageValue);
            crackShatterRadius = Mathf.Max(0.1f, crackShatterRadiusValue);
        }

        appliesRoot = appliesRootValue;
        if (appliesRoot)
        {
            rootDuration = Mathf.Max(0.1f, rootDurationValue);
            rootSpeedMultiplier = rootSpeedMultiplierValue;
        }

        hitsRemaining = Mathf.Max(1, hitsBeforeDestroyValue);

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

        transform.localScale = Vector3.one * Mathf.Max(MinimumSize, size);

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
        if (other == null)
        {
            return;
        }

        if (other.CompareTag("SideWall") || other.CompareTag("TopWall") || other.CompareTag("BottomBoundary"))
        {
            Destroy(gameObject);
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

        if (appliesBurn)
        {
            brick.ApplyExternalBurn(burnDamage, burnTickInterval, burnHitCount);
        }

        if (appliesCrack || appliesRoot)
        {
            brick.ApplyFertileLandPatch(
                appliesCrack,
                crackShatterDamage,
                crackShatterRadius,
                appliesRoot,
                RootOnlyRadius,
                rootDuration,
                rootSpeedMultiplier);
        }

        hitsRemaining--;
        if (hitsRemaining <= 0)
        {
            Destroy(gameObject);
        }
    }
}
