using UnityEngine;

[DisallowMultipleComponent]
public class SpriteStacker : MonoBehaviour
{
    [SerializeField] private int layers = 4;
    [SerializeField] private float totalDepth = 0.08f;
    [SerializeField] private Vector2 direction = new Vector2(1f, -1f);
    [SerializeField, Range(0f, 1f)] private float edgeBrightness = 0.4f;

    private SpriteRenderer sourceRenderer;
    private SpriteRenderer[] layerRenderers;
    private Sprite lastSprite;
    private Color lastColor;

    private void Awake()
    {
        sourceRenderer = GetComponent<SpriteRenderer>();
        Build();
        SyncLayersToSource(force: true);
    }

    private void LateUpdate()
    {
        SyncLayersToSource(force: false);
    }

    private void Build()
    {
        ClearLayers();

        if (layers <= 0 || sourceRenderer == null)
        {
            layerRenderers = null;
            return;
        }

        layerRenderers = new SpriteRenderer[layers];
        Vector2 dir = direction == Vector2.zero ? Vector2.right : direction.normalized;
        float stepSize = totalDepth / layers;

        for (int i = 0; i < layers; i++)
        {
            GameObject obj = new GameObject($"StackLayer_{i}");
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = new Vector3(dir.x * stepSize * (i + 1), dir.y * stepSize * (i + 1), 0f);
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sortingLayerID = sourceRenderer.sortingLayerID;
            sr.sortingOrder = sourceRenderer.sortingOrder - (i + 1);
            sr.sprite = sourceRenderer.sprite;

            Color main = sourceRenderer.color;
            sr.color = new Color(main.r * edgeBrightness, main.g * edgeBrightness, main.b * edgeBrightness, main.a);

            layerRenderers[i] = sr;
        }
    }

    private void ClearLayers()
    {
        if (layerRenderers == null) return;
        foreach (SpriteRenderer sr in layerRenderers)
        {
            if (sr != null)
                Destroy(sr.gameObject);
        }
        layerRenderers = null;
    }

    /// <summary>
    /// Rebuilds stack layers. Call this if layers, totalDepth, or direction are changed at runtime.
    /// </summary>
    public void Rebuild()
    {
        Build();
    }

    /// <summary>
    /// Updates all stack layers to match the given sprite and color.
    /// </summary>
    public void Refresh(Sprite sprite, Color mainColor)
    {
        if (layerRenderers == null) return;
        foreach (SpriteRenderer sr in layerRenderers)
        {
            if (sr == null) continue;
            sr.sprite = sprite;
            sr.color = new Color(mainColor.r * edgeBrightness, mainColor.g * edgeBrightness, mainColor.b * edgeBrightness, mainColor.a);
        }

        lastSprite = sprite;
        lastColor = mainColor;
    }

    /// <summary>
    /// Applies a grayscale/alpha blend to all stack layers (e.g. for pause or level-complete visuals).
    /// </summary>
    public void ApplyPauseVisual(float grayscaleBlend, float alphaMultiplier)
    {
        if (layerRenderers == null) return;
        foreach (SpriteRenderer sr in layerRenderers)
        {
            if (sr == null) continue;
            Color c = sr.color;
            float g = c.grayscale;
            Color paused = new Color(g, g, g, c.a * Mathf.Clamp01(alphaMultiplier));
            sr.color = Color.Lerp(c, paused, Mathf.Clamp01(grayscaleBlend));
        }
    }

    private void SyncLayersToSource(bool force)
    {
        if (sourceRenderer == null || layerRenderers == null)
        {
            return;
        }

        Sprite currentSprite = sourceRenderer.sprite;
        Color currentColor = sourceRenderer.color;
        if (!force && currentSprite == lastSprite && currentColor == lastColor)
        {
            return;
        }

        Refresh(currentSprite, currentColor);
    }

    private void OnDestroy()
    {
        ClearLayers();
    }
}
