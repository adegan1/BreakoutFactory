using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TintHighlightController : MonoBehaviour
{
    [SerializeField] private bool includeChildSpriteRenderers = true;
    [SerializeField] private bool includeInactiveChildren = false;
    [SerializeField] private List<SpriteRenderer> explicitTargetRenderers = new List<SpriteRenderer>();

    private readonly List<RendererState> rendererStates = new List<RendererState>();
    private bool isHighlighted;
    private bool hadHighlightAppliedLastFrame;
    private Color highlightTint = Color.cyan;
    private float highlightStrength = 0.2f;

    private struct RendererState
    {
        public SpriteRenderer Renderer;
        public Color BaseColor;
    }

    private void Awake()
    {
        RebuildRendererStates();
    }

    private void OnEnable()
    {
        RebuildRendererStates();
    }

    private void OnDisable()
    {
        RestoreBaseColors();
        hadHighlightAppliedLastFrame = false;
    }

    private void LateUpdate()
    {
        if (!isHighlighted)
        {
            if (hadHighlightAppliedLastFrame)
            {
                RestoreBaseColors();
                hadHighlightAppliedLastFrame = false;
            }

            CaptureCurrentColorsAsBase();
            return;
        }

        if (!hadHighlightAppliedLastFrame)
        {
            CaptureCurrentColorsAsBase();
        }

        ApplyHighlightColor();
        hadHighlightAppliedLastFrame = true;
    }

    public void SetHighlight(bool highlighted, Color tint, float strength)
    {
        isHighlighted = highlighted;
        highlightTint = tint;
        highlightStrength = Mathf.Clamp01(strength);

        if (!isHighlighted)
        {
            RestoreBaseColors();
            hadHighlightAppliedLastFrame = false;
        }
    }

    public void RefreshTargets()
    {
        RebuildRendererStates();
    }

    private void RebuildRendererStates()
    {
        rendererStates.Clear();

        if (explicitTargetRenderers != null && explicitTargetRenderers.Count > 0)
        {
            for (int i = 0; i < explicitTargetRenderers.Count; i++)
            {
                SpriteRenderer target = explicitTargetRenderers[i];
                if (target == null)
                {
                    continue;
                }

                rendererStates.Add(new RendererState
                {
                    Renderer = target,
                    BaseColor = target.color
                });
            }

            return;
        }

        if (includeChildSpriteRenderers)
        {
            SpriteRenderer[] childRenderers = GetComponentsInChildren<SpriteRenderer>(includeInactiveChildren);
            for (int i = 0; i < childRenderers.Length; i++)
            {
                SpriteRenderer childRenderer = childRenderers[i];
                if (childRenderer == null)
                {
                    continue;
                }

                rendererStates.Add(new RendererState
                {
                    Renderer = childRenderer,
                    BaseColor = childRenderer.color
                });
            }

            return;
        }

        SpriteRenderer selfRenderer = GetComponent<SpriteRenderer>();
        if (selfRenderer != null)
        {
            rendererStates.Add(new RendererState
            {
                Renderer = selfRenderer,
                BaseColor = selfRenderer.color
            });
        }
    }

    private void CaptureCurrentColorsAsBase()
    {
        for (int i = 0; i < rendererStates.Count; i++)
        {
            RendererState state = rendererStates[i];
            if (state.Renderer == null)
            {
                continue;
            }

            state.BaseColor = state.Renderer.color;
            rendererStates[i] = state;
        }
    }

    private void ApplyHighlightColor()
    {
        for (int i = 0; i < rendererStates.Count; i++)
        {
            RendererState state = rendererStates[i];
            if (state.Renderer == null)
            {
                continue;
            }

            Color targetColor = Color.Lerp(state.BaseColor, highlightTint, highlightStrength);
            targetColor.a = state.BaseColor.a;
            state.Renderer.color = targetColor;
        }
    }

    private void RestoreBaseColors()
    {
        for (int i = 0; i < rendererStates.Count; i++)
        {
            RendererState state = rendererStates[i];
            if (state.Renderer == null)
            {
                continue;
            }

            state.Renderer.color = state.BaseColor;
        }
    }
}
