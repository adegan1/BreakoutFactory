using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class SteamBurstEffect : MonoBehaviour
{
    public const int RingSegments = 40;
    private const int RingCount = 3;

    private static Material sharedMaterial;

    // Spawns multiple staggered expanding rings that billow outward like steam.
    // Rings are slow, soft, and fully opaque at the center — unlike the sharp fire burst.
    public static void Spawn(Vector3 position, Color color, float maxRadius, float lifetime, float width)
    {
        GameObject go = new GameObject("SteamBurst");
        go.transform.position = position;
        SteamBurstEffect effect = go.AddComponent<SteamBurstEffect>();
        effect.Initialize(color, maxRadius, lifetime, width);
    }

    private void Initialize(Color color, float maxRadius, float lifetime, float width)
    {
        StartCoroutine(RunBurst(color, maxRadius, lifetime, width));
    }

    private IEnumerator RunBurst(Color color, float maxRadius, float lifetime, float width)
    {
        float staggerInterval = lifetime * 0.18f;
        float ringLifetime = lifetime * 0.75f;

        for (int ring = 0; ring < RingCount; ring++)
        {
            // Each successive ring is slightly smaller and thinner
            float radiusScale = 1f - ring * 0.15f;
            float widthScale = 1f - ring * 0.2f;
            SpawnRing(color, maxRadius * radiusScale, ringLifetime, width * Mathf.Max(0.2f, widthScale));

            if (ring < RingCount - 1)
            {
                yield return new WaitForSeconds(staggerInterval);
            }
        }

        // Keep the parent alive until all rings have certainly expired
        yield return new WaitForSeconds(ringLifetime);
        Destroy(gameObject);
    }

    private void SpawnRing(Color color, float maxRadius, float lifetime, float width)
    {
        GameObject ringObj = new GameObject("SteamRing");
        ringObj.transform.SetParent(transform, false);

        LineRenderer lr = ringObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.positionCount = RingSegments;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.numCapVertices = 2;
        lr.numCornerVertices = 4;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.sortingOrder = 20;
        lr.material = GetOrCreateMaterial();

        SteamRingAnimator animator = ringObj.AddComponent<SteamRingAnimator>();
        animator.Run(transform.position, color, maxRadius, lifetime, lr);
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

// Drives a single steam ring: ease-in-out expansion, fade out in the latter half.
[DisallowMultipleComponent]
public class SteamRingAnimator : MonoBehaviour
{
    private Vector3 origin;
    private Color ringColor;
    private float maxRadius;
    private float lifetime;
    private float elapsed;
    private LineRenderer lr;

    public void Run(Vector3 origin, Color color, float maxRadius, float lifetime, LineRenderer lr)
    {
        this.origin = origin;
        ringColor = color;
        this.maxRadius = maxRadius;
        this.lifetime = Mathf.Max(0.05f, lifetime);
        elapsed = 0f;
        this.lr = lr;
    }

    private void Update()
    {
        if (lr == null)
        {
            Destroy(gameObject);
            return;
        }

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / lifetime);

        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        // Ease-in-out expansion: slow start, fast middle, slow end
        float smoothT = t * t * (3f - 2f * t);
        float currentRadius = maxRadius * smoothT;

        // Fade only in the second half of lifetime
        float fadeT = Mathf.Clamp01((t - 0.4f) / 0.6f);
        float alpha = ringColor.a * (1f - fadeT);

        Color c = new Color(ringColor.r, ringColor.g, ringColor.b, alpha);
        lr.startColor = c;
        lr.endColor = c;

        for (int i = 0; i < SteamBurstEffect.RingSegments; i++)
        {
            float angle = (float)i / SteamBurstEffect.RingSegments * Mathf.PI * 2f;
            lr.SetPosition(i, origin + new Vector3(
                Mathf.Cos(angle) * currentRadius,
                Mathf.Sin(angle) * currentRadius,
                0f));
        }
    }
}
