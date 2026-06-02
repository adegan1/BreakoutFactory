using UnityEngine;

public static class BreakoutEffectUtility
{
    private const string PrimaryLineShader = "Sprites/Default";
    private const string UrpFallbackLineShader = "Universal Render Pipeline/Particles/Unlit";
    private const string LegacyFallbackLineShader = "Unlit/Color";

    public static Material GetOrCreateSharedLineMaterial(ref Material sharedMaterial)
    {
        if (sharedMaterial != null)
        {
            return sharedMaterial;
        }

        Shader shader = Shader.Find(PrimaryLineShader)
            ?? Shader.Find(UrpFallbackLineShader)
            ?? Shader.Find(LegacyFallbackLineShader);

        if (shader == null)
        {
            return null;
        }

        sharedMaterial = new Material(shader);
        return sharedMaterial;
    }

    public static void EnsureProjectileComponents(
        GameObject target,
        out SpriteRenderer spriteRenderer,
        out Rigidbody2D rb,
        out CircleCollider2D hitbox,
        float hitboxRadius)
    {
        spriteRenderer = target.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = target.AddComponent<SpriteRenderer>();
        }

        rb = target.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = target.AddComponent<Rigidbody2D>();
        }

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.freezeRotation = true;

        hitbox = target.GetComponent<CircleCollider2D>();
        if (hitbox == null)
        {
            hitbox = target.AddComponent<CircleCollider2D>();
        }

        hitbox.isTrigger = true;
        hitbox.radius = Mathf.Max(0f, hitboxRadius);
    }

    public static void ApplyPauseVisual(SpriteRenderer spriteRenderer, float grayscaleBlend, float alphaMultiplier)
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

    public static void AdvanceSpriteAnimation(
        SpriteRenderer spriteRenderer,
        Sprite[] frames,
        float frameRate,
        ref float frameTimer,
        ref int frameIndex)
    {
        if (spriteRenderer == null || frames == null || frames.Length <= 1 || frameRate <= 0f)
        {
            return;
        }

        frameTimer += Time.deltaTime;
        float frameDuration = 1f / frameRate;
        if (frameTimer < frameDuration)
        {
            return;
        }

        frameTimer -= frameDuration;
        frameIndex = (frameIndex + 1) % frames.Length;
        Sprite nextFrame = frames[frameIndex];
        if (nextFrame != null)
        {
            spriteRenderer.sprite = nextFrame;
        }
    }
}
