using UnityEngine;

[DisallowMultipleComponent]
public class FireBurstEffect : MonoBehaviour
{
    private const int RingSegments = 32;

    private static Material sharedMaterial;

    private Color burstColor;
    private float rayLength;
    private float lifetime;
    private float elapsed;

    private LineRenderer ringRenderer;
    private LineRenderer[] rayRenderers;

    /// <summary>
    /// Spawns an expanding fire burst visual: a smooth ring and radial rays that
    /// ease out and fade, with no jagged noise — looks like fire, not lightning.
    /// </summary>
    public static FireBurstEffect Spawn(Vector3 position, Color color, float width, float rayLength, float lifetime, int rayCount)
    {
        GameObject go = new GameObject("FireBurst");
        go.transform.position = position;
        FireBurstEffect effect = go.AddComponent<FireBurstEffect>();
        effect.Initialize(color, width, rayLength, lifetime, rayCount);
        return effect;
    }

    private void Initialize(Color color, float width, float rayLength, float lifetime, int rayCount)
    {
        burstColor = color;
        this.rayLength = Mathf.Max(0.1f, rayLength);
        this.lifetime = Mathf.Max(0.05f, lifetime);
        elapsed = 0f;

        // Expanding ring (thin, same color)
        ringRenderer = CreateLineRenderer(width * 0.4f);
        ringRenderer.loop = true;
        ringRenderer.positionCount = RingSegments;

        // Radial rays (thicker at origin, taper to nothing)
        rayCount = Mathf.Max(1, rayCount);
        rayRenderers = new LineRenderer[rayCount];
        for (int i = 0; i < rayCount; i++)
        {
            LineRenderer lr = CreateLineRenderer(width);
            lr.positionCount = 2;
            rayRenderers[i] = lr;
        }
    }

    private LineRenderer CreateLineRenderer(float width)
    {
        GameObject child = new GameObject("BurstLine");
        child.transform.SetParent(transform, false);

        LineRenderer lr = child.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.startWidth = width;
        lr.endWidth = 0f;
        lr.numCapVertices = 2;
        lr.numCornerVertices = 0;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.sortingOrder = 20;
        lr.material = GetOrCreateMaterial();
        return lr;
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

        // Ease-out expansion: fast at first, decelerates
        float expand = 1f - (1f - t) * (1f - t);
        float currentRadius = rayLength * expand;

        // Fade out linearly over lifetime
        float alpha = burstColor.a * (1f - t);
        Color full = new Color(burstColor.r, burstColor.g, burstColor.b, alpha);
        Color transparent = new Color(burstColor.r, burstColor.g, burstColor.b, 0f);

        Vector3 origin = transform.position;

        // Ring
        if (ringRenderer != null)
        {
            for (int i = 0; i < RingSegments; i++)
            {
                float angle = (float)i / RingSegments * Mathf.PI * 2f;
                ringRenderer.SetPosition(i, origin + new Vector3(
                    Mathf.Cos(angle) * currentRadius,
                    Mathf.Sin(angle) * currentRadius,
                    0f));
            }
            ringRenderer.startColor = full;
            ringRenderer.endColor = full;
        }

        // Rays
        if (rayRenderers != null)
        {
            for (int i = 0; i < rayRenderers.Length; i++)
            {
                LineRenderer lr = rayRenderers[i];
                if (lr == null) continue;
                float angle = (float)i / rayRenderers.Length * Mathf.PI * 2f;
                Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                lr.SetPosition(0, origin);
                lr.SetPosition(1, origin + dir * currentRadius);
                lr.startColor = full;
                lr.endColor = transparent;
            }
        }
    }

    private static Material GetOrCreateMaterial()
    {
        return BreakoutEffectUtility.GetOrCreateSharedLineMaterial(ref sharedMaterial);
    }
}
