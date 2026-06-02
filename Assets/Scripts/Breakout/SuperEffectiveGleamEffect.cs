using UnityEngine;

[DisallowMultipleComponent]
public class SuperEffectiveGleamEffect : MonoBehaviour
{
    private const int GleamRayCount = 4;

    private static Material sharedMaterial;

    private Color gleamColor;
    private float maxRadius;
    private float lifetime;
    private float elapsed;
    private LineRenderer[] rays;

    public static SuperEffectiveGleamEffect Spawn(Vector3 position, Color color, float size, float width, float lifetime)
    {
        GameObject go = new GameObject("SuperEffectiveGleam");
        go.transform.position = position;
        SuperEffectiveGleamEffect effect = go.AddComponent<SuperEffectiveGleamEffect>();
        effect.Initialize(color, size, width, lifetime);
        return effect;
    }

    private void Initialize(Color color, float size, float width, float effectLifetime)
    {
        gleamColor = color;
        maxRadius = Mathf.Max(0.01f, size);
        lifetime = Mathf.Max(0.05f, effectLifetime);
        elapsed = 0f;

        float clampedWidth = Mathf.Max(0.005f, width);
        rays = new LineRenderer[GleamRayCount];
        for (int i = 0; i < GleamRayCount; i++)
        {
            rays[i] = CreateRayRenderer(clampedWidth);
        }
    }

    private LineRenderer CreateRayRenderer(float width)
    {
        GameObject child = new GameObject("GleamRay");
        child.transform.SetParent(transform, false);

        LineRenderer lr = child.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.numCapVertices = 2;
        lr.numCornerVertices = 0;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.sortingOrder = 24;
        lr.material = BreakoutEffectUtility.GetOrCreateSharedLineMaterial(ref sharedMaterial);
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

        float pulse = Mathf.Sin(t * Mathf.PI);
        float radius = Mathf.Lerp(maxRadius * 0.2f, maxRadius, pulse);
        float alpha = gleamColor.a * (1f - t);
        Color current = new Color(gleamColor.r, gleamColor.g, gleamColor.b, alpha);

        Vector3 origin = transform.position;
        for (int i = 0; i < GleamRayCount; i++)
        {
            LineRenderer lr = rays[i];
            if (lr == null)
            {
                continue;
            }

            float angle = 45f * i * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            lr.SetPosition(0, origin - dir * radius);
            lr.SetPosition(1, origin + dir * radius);
            lr.startColor = current;
            lr.endColor = current;
        }
    }
}
