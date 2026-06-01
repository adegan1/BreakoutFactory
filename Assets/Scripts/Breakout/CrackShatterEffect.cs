using UnityEngine;

// Spawns an expanding crack-shatter visual: a ring with jagged crack lines instead of
// straight rays. Crack patterns are generated once on spawn and scale outward with the ring.
// Used whenever a cracked brick shatters and damages its neighbours.
[DisallowMultipleComponent]
public class CrackShatterEffect : MonoBehaviour
{
    private const int RingSegments = 32;
    private const int KnotsPerCrack = 6; // intermediate zigzag points per crack line

    private static Material sharedMaterial;

    private Color shatterColor;
    private float maxRadius;
    private float lifetime;
    private float elapsed;

    private LineRenderer ringRenderer;
    private LineRenderer[] crackRenderers;
    private Vector2[][] crackPatterns; // normalized crack shapes, scaled each frame

    /// <summary>
    /// Spawns a crack-shatter visual: an expanding ring with jagged radiating crack lines.
    /// </summary>
    /// <param name="position">World-space spawn point (centre of the shattered brick).</param>
    /// <param name="radius">How far the ring and cracks expand to.</param>
    /// <param name="color">Tint colour of the whole effect.</param>
    /// <param name="lineWidth">Width of the crack lines (ring is slightly thinner).</param>
    /// <param name="lifetime">Total duration in seconds before the effect is destroyed.</param>
    /// <param name="crackCount">Number of radiating crack lines.</param>
    public static CrackShatterEffect Spawn(Vector3 position, float radius, Color color, float lineWidth, float lifetime, int crackCount)
    {
        GameObject go = new GameObject("CrackShatter");
        go.transform.position = position;
        CrackShatterEffect effect = go.AddComponent<CrackShatterEffect>();
        effect.Initialize(radius, color, lineWidth, lifetime, crackCount);
        return effect;
    }

    private void Initialize(float radius, Color color, float lineWidth, float lifetime, int crackCount)
    {
        shatterColor = color;
        maxRadius = Mathf.Max(0.1f, radius);
        this.lifetime = Mathf.Max(0.05f, lifetime);
        elapsed = 0f;

        lineWidth = Mathf.Max(0.005f, lineWidth);

        // Ring (same as FireBurstEffect: thin loop)
        ringRenderer = CreateLineRenderer(lineWidth * 0.4f);
        ringRenderer.loop = true;
        ringRenderer.positionCount = RingSegments;

        // Crack lines
        crackCount = Mathf.Max(1, crackCount);
        crackRenderers = new LineRenderer[crackCount];
        crackPatterns = new Vector2[crackCount][];

        for (int c = 0; c < crackCount; c++)
        {
            crackRenderers[c] = CreateLineRenderer(lineWidth);
            crackRenderers[c].positionCount = KnotsPerCrack;
            crackPatterns[c] = GenerateCrackPattern(c, crackCount);
        }
    }

    // Generates a normalized crack shape: Vector2 positions in the range [-1,1]
    // representing offsets from centre at unit radius.
    private static Vector2[] GenerateCrackPattern(int crackIndex, int totalCracks)
    {
        float angle = (float)crackIndex / totalCracks * Mathf.PI * 2f;
        Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        Vector2 perp = new Vector2(-dir.y, dir.x);

        Vector2[] pattern = new Vector2[KnotsPerCrack];
        pattern[0] = Vector2.zero; // always starts at centre

        // Intermediate knots: each randomly wobbles perpendicular to the radial direction,
        // with the wobble magnitude tapering toward the tip.
        Random.State savedState = Random.state;
        Random.InitState(crackIndex * 7919 + totalCracks * 1327); // deterministic per index

        for (int k = 1; k < KnotsPerCrack - 1; k++)
        {
            float t = (float)k / (KnotsPerCrack - 1);
            float wobbleRange = 0.18f * (1f - t * 0.6f); // larger wobble near base
            float wobble = Random.Range(-wobbleRange, wobbleRange);
            pattern[k] = dir * t + perp * wobble;
        }

        Random.state = savedState;

        pattern[KnotsPerCrack - 1] = dir; // tip sits exactly at the radial edge
        return pattern;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / lifetime);

        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        // Ease-out expansion (same curve as FireBurstEffect)
        float expand = 1f - (1f - t) * (1f - t);
        float currentRadius = maxRadius * expand;

        // Linear fade
        float alpha = shatterColor.a * (1f - t);
        Color full = new Color(shatterColor.r, shatterColor.g, shatterColor.b, alpha);

        Vector3 origin = transform.position;

        // Ring
        if (ringRenderer != null)
        {
            for (int i = 0; i < RingSegments; i++)
            {
                float a = (float)i / RingSegments * Mathf.PI * 2f;
                ringRenderer.SetPosition(i, origin + new Vector3(
                    Mathf.Cos(a) * currentRadius,
                    Mathf.Sin(a) * currentRadius,
                    0f));
            }
            ringRenderer.startColor = full;
            ringRenderer.endColor = full;
        }

        // Crack lines
        if (crackRenderers != null)
        {
            Color tipColor = new Color(full.r, full.g, full.b, 0f);
            for (int c = 0; c < crackRenderers.Length; c++)
            {
                LineRenderer lr = crackRenderers[c];
                if (lr == null) continue;
                Vector2[] pattern = crackPatterns[c];
                for (int k = 0; k < KnotsPerCrack; k++)
                {
                    lr.SetPosition(k, origin + (Vector3)(pattern[k] * currentRadius));
                }
                lr.startColor = full;
                lr.endColor = tipColor;
            }
        }
    }

    private LineRenderer CreateLineRenderer(float width)
    {
        GameObject child = new GameObject("CrackLine");
        child.transform.SetParent(transform, false);

        LineRenderer lr = child.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.startWidth = width;
        lr.endWidth = 0f;
        lr.numCapVertices = 2;
        lr.numCornerVertices = 2;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.sortingOrder = 20;
        lr.material = GetOrCreateMaterial();
        return lr;
    }

    private static Material GetOrCreateMaterial()
    {
        if (sharedMaterial != null)
            return sharedMaterial;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        if (shader == null)
            return null;

        sharedMaterial = new Material(shader);
        return sharedMaterial;
    }
}
