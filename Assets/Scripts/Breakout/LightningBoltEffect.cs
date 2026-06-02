using UnityEngine;

[DisallowMultipleComponent]
public class LightningBoltEffect : MonoBehaviour
{
    private const float MinLifetime = 0.05f;
    private const float MinWidth = 0.005f;

    private static Material sharedBoltMaterial;

    private LineRenderer lineRenderer;
    private float lifetime;
    private float elapsed;
    private Color startColorFull;
    private Color endColorFull;

    // Spawns a burst-style bolt using BallTypeData's lightning burst visual settings.
    public static LightningBoltEffect SpawnBurst(Vector3 start, Vector3 end, BallTypeData sourceTypeData)
    {
        if (sourceTypeData == null)
        {
            return null;
        }

        return Spawn(
            start, end,
            sourceTypeData.LightningBurstBoltColor,
            sourceTypeData.LightningBurstBoltWidth,
            sourceTypeData.LightningBurstBoltLifetime,
            sourceTypeData.LightningBurstBoltSegments,
            sourceTypeData.LightningBurstBoltNoise,
            sourceTypeData.LightningBurstBoltMaterial);
    }

    // Spawns a blackout bolt, picking a random color from BallTypeData's blackout color array.
    public static LightningBoltEffect SpawnBlackout(Vector3 start, Vector3 end, BallTypeData sourceTypeData)
    {
        if (sourceTypeData == null)
        {
            return null;
        }

        Color[] colors = sourceTypeData.BlackoutBoltColors;
        Color color = (colors != null && colors.Length > 0)
            ? colors[Random.Range(0, colors.Length)]
            : Color.white;

        return Spawn(
            start, end,
            color,
            sourceTypeData.BlackoutBoltWidth,
            sourceTypeData.BlackoutBoltLifetime,
            sourceTypeData.BlackoutBoltSegments,
            sourceTypeData.BlackoutBoltNoise,
            sourceTypeData.BlackoutBoltMaterial);
    }

    // Spawns a shock therapy bolt using BallTypeData's shock therapy visual settings.
    public static LightningBoltEffect SpawnShockTherapy(Vector3 start, Vector3 end, BallTypeData sourceTypeData)
    {
        if (sourceTypeData == null)
        {
            return null;
        }

        return Spawn(
            start, end,
            sourceTypeData.ShockTherapyBoltColor,
            sourceTypeData.ShockTherapyBoltWidth,
            sourceTypeData.ShockTherapyBoltLifetime,
            sourceTypeData.ShockTherapyBoltSegments,
            sourceTypeData.ShockTherapyBoltNoise,
            sourceTypeData.ShockTherapyBoltMaterial);
    }

    // Spawns a cardinal-direction electric cascade beam from a world position.
    public static LightningBoltEffect SpawnCascadeBeam(Vector3 origin, Vector2 direction, BallTypeData sourceTypeData)
    {
        if (sourceTypeData == null)
        {
            return null;
        }

        Vector3 end = origin + (Vector3)(direction.normalized * Mathf.Max(0.1f, sourceTypeData.ElectricCascadeBeamLength));
        return Spawn(
            origin, end,
            sourceTypeData.ElectricCascadeBeamColor,
            sourceTypeData.ElectricCascadeBeamWidth,
            sourceTypeData.ElectricCascadeBeamLifetime,
            sourceTypeData.ElectricCascadeBeamSegments,
            sourceTypeData.ElectricCascadeBeamNoise,
            sourceTypeData.ElectricCascadeBeamMaterial);
    }

    // Spawns a snake-style bolt using BallTypeData's lightning snake visual settings and an explicit lifetime.
    public static LightningBoltEffect SpawnSnake(Vector3 start, Vector3 end, BallTypeData sourceTypeData, float overrideLifetime)
    {
        if (sourceTypeData == null)
        {
            return null;
        }

        return Spawn(
            start, end,
            sourceTypeData.LightningSnakeBoltColor,
            sourceTypeData.LightningSnakeBoltWidth,
            overrideLifetime,
            sourceTypeData.LightningSnakeBoltSegments,
            sourceTypeData.LightningSnakeBoltNoise,
            sourceTypeData.LightningSnakeBoltMaterial);
    }

    public static LightningBoltEffect Spawn(
        Vector3 start,
        Vector3 end,
        Color color,
        float width,
        float lifetime,
        int segments,
        float noise,
        Material material = null)
    {
        GameObject go = new GameObject("Lightning Bolt");
        go.transform.position = start;
        LightningBoltEffect effect = go.AddComponent<LightningBoltEffect>();
        effect.Initialize(start, end, color, width, lifetime, segments, noise, material);
        return effect;
    }

    private void Initialize(
        Vector3 start,
        Vector3 end,
        Color color,
        float width,
        float lifetime,
        int segments,
        float noise,
        Material material)
    {
        this.lifetime = Mathf.Max(MinLifetime, lifetime);
        elapsed = 0f;

        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = Mathf.Max(MinWidth, width);
        lineRenderer.endWidth = Mathf.Max(MinWidth, width * 0.5f);
        lineRenderer.numCornerVertices = 0;
        lineRenderer.numCapVertices = 2;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.sortingOrder = 20;

        if (material != null)
        {
            lineRenderer.material = material;
        }
        else
        {
            Material boltMaterial = GetOrCreateBoltMaterial();
            if (boltMaterial != null)
            {
                lineRenderer.material = boltMaterial;
            }
        }

        int count = Mathf.Max(2, segments);
        lineRenderer.positionCount = count;

        Vector3 dir = end - start;
        Vector3 perp = dir.sqrMagnitude > 0.0001f
            ? new Vector3(-dir.y, dir.x, 0f).normalized
            : Vector3.right;

        float noiseStrength = Mathf.Max(0f, noise);

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / (count - 1);
            Vector3 pos = Vector3.Lerp(start, end, t);
            if (i > 0 && i < count - 1)
            {
                pos += perp * Random.Range(-noiseStrength, noiseStrength);
            }
            pos.z = 0f;
            lineRenderer.SetPosition(i, pos);
        }

        startColorFull = color;
        endColorFull = new Color(color.r, color.g, color.b, color.a * 0.5f);
        lineRenderer.startColor = startColorFull;
        lineRenderer.endColor = endColorFull;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float alpha = 1f - Mathf.Clamp01(elapsed / lifetime);

        Color sc = startColorFull;
        sc.a = startColorFull.a * alpha;
        lineRenderer.startColor = sc;

        Color ec = endColorFull;
        ec.a = endColorFull.a * alpha;
        lineRenderer.endColor = ec;

        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    private static Material GetOrCreateBoltMaterial()
    {
        return BreakoutEffectUtility.GetOrCreateSharedLineMaterial(ref sharedBoltMaterial);
    }
}
