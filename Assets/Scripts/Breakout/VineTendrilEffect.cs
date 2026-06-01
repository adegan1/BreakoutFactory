using UnityEngine;

// Spawns an animated vine tendril from a source position to a target position.
// The line draws itself progressively (like a vine growing), then fades out.
// Used for the Life brick + Water ball synergy to show which brick gets rooted.
[DisallowMultipleComponent]
public class VineTendrilEffect : MonoBehaviour
{
    private const int Segments = 16;

    private static Material sharedMaterial;

    private Vector3 start;
    private Vector3 end;
    private Color vineColor;
    private float width;
    private float growDuration;
    private float holdDuration;
    private float fadeDuration;
    private float elapsed;
    private LineRenderer lr;

    public static VineTendrilEffect Spawn(Vector3 from, Vector3 to, Color color, float width, float growDuration, float holdDuration, float fadeDuration)
    {
        GameObject go = new GameObject("VineTendril");
        go.transform.position = from;
        VineTendrilEffect effect = go.AddComponent<VineTendrilEffect>();
        effect.Initialize(from, to, color, width, growDuration, holdDuration, fadeDuration);
        return effect;
    }

    private void Initialize(Vector3 from, Vector3 to, Color color, float width, float growDuration, float holdDuration, float fadeDuration)
    {
        start = from;
        end = to;
        vineColor = color;
        this.width = Mathf.Max(0.005f, width);
        this.growDuration = Mathf.Max(0.05f, growDuration);
        this.holdDuration = Mathf.Max(0f, holdDuration);
        this.fadeDuration = Mathf.Max(0.05f, fadeDuration);
        elapsed = 0f;

        lr = gameObject.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = Segments;
        lr.startWidth = width;
        lr.endWidth = width * 0.35f;
        lr.numCapVertices = 2;
        lr.numCornerVertices = 4;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.sortingOrder = 20;
        lr.material = GetOrCreateMaterial();

        // Initialise all points at the start so nothing is visible yet
        for (int i = 0; i < Segments; i++)
            lr.SetPosition(i, start);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float totalLifetime = growDuration + holdDuration + fadeDuration;

        if (elapsed >= totalLifetime)
        {
            Destroy(gameObject);
            return;
        }

        float alpha;
        float drawProgress;

        if (elapsed < growDuration)
        {
            // Growing phase: ease-in-out draw
            float t = elapsed / growDuration;
            drawProgress = t * t * (3f - 2f * t);
            alpha = 1f;
        }
        else if (elapsed < growDuration + holdDuration)
        {
            // Hold phase: fully drawn
            drawProgress = 1f;
            alpha = 1f;
        }
        else
        {
            // Fade phase
            drawProgress = 1f;
            float fadeT = (elapsed - growDuration - holdDuration) / fadeDuration;
            alpha = 1f - Mathf.Clamp01(fadeT);
        }

        // Build a slightly curved path (arc toward the right of the midpoint for a vine feel)
        Vector3 mid = Vector3.Lerp(start, end, 0.5f);
        Vector3 perp = Vector3.Cross((end - start).normalized, Vector3.forward).normalized;
        float arcBulge = Vector3.Distance(start, end) * 0.2f;
        Vector3 controlPoint = mid + perp * arcBulge;

        int drawnSegments = Mathf.Max(2, Mathf.CeilToInt(drawProgress * Segments));

        for (int i = 0; i < Segments; i++)
        {
            float segT = (float)i / (Segments - 1);
            if (i >= drawnSegments)
            {
                // Undrawn — collapse to last drawn point
                float lastT = (float)(drawnSegments - 1) / (Segments - 1);
                lr.SetPosition(i, QuadraticBezier(start, controlPoint, end, lastT));
            }
            else
            {
                lr.SetPosition(i, QuadraticBezier(start, controlPoint, end, segT));
            }
        }

        Color c = new Color(vineColor.r, vineColor.g, vineColor.b, vineColor.a * alpha);
        Color cFaded = new Color(vineColor.r, vineColor.g, vineColor.b, vineColor.a * alpha * 0.4f);
        lr.startColor = c;
        lr.endColor = cFaded;
    }

    private static Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float u = 1f - t;
        return u * u * p0 + 2f * u * t * p1 + t * t * p2;
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
