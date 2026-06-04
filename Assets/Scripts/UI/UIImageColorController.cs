using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIImageColorController : MonoBehaviour
{
    private enum TargetColorMode
    {
        ChangeToNormalColor,
        ChangeToSelectedColor
    }

    [System.Serializable]
    private struct TargetGraphicEntry
    {
        [SerializeField] private Graphic targetGraphic;
        [SerializeField] private TargetColorMode colorMode;

        public Graphic TargetGraphic => targetGraphic;
        public TargetColorMode ColorMode => colorMode;
    }

    [Header("References")]
    [SerializeField] private TargetGraphicEntry[] targetGraphics;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.gray;
    [SerializeField] private bool applyInitialColorOnAwake = true;

    private void Awake()
    {
        if (applyInitialColorOnAwake)
        {
            ApplyConfiguredColors();
        }
    }

    public void ApplyConfiguredColors()
    {
        if (targetGraphics == null || targetGraphics.Length == 0)
        {
            return;
        }

        for (int i = 0; i < targetGraphics.Length; i++)
        {
            Graphic graphic = targetGraphics[i].TargetGraphic;
            if (graphic == null)
            {
                continue;
            }

            graphic.color = ResolveConfiguredColor(targetGraphics[i].ColorMode);
        }
    }

    public void ApplyInverseConfiguredColors()
    {
        if (targetGraphics == null || targetGraphics.Length == 0)
        {
            return;
        }

        for (int i = 0; i < targetGraphics.Length; i++)
        {
            Graphic graphic = targetGraphics[i].TargetGraphic;
            if (graphic == null)
            {
                continue;
            }

            graphic.color = ResolveInverseColor(targetGraphics[i].ColorMode);
        }
    }

    public void ApplyAllNormalColors()
    {
        ApplyUniformColor(normalColor);
    }

    public void ApplyAllSelectedColors()
    {
        ApplyUniformColor(selectedColor);
    }

    private void ApplyUniformColor(Color color)
    {
        if (targetGraphics == null || targetGraphics.Length == 0)
        {
            return;
        }

        for (int i = 0; i < targetGraphics.Length; i++)
        {
            Graphic graphic = targetGraphics[i].TargetGraphic;
            if (graphic != null)
            {
                graphic.color = color;
            }
        }
    }

    private Color ResolveConfiguredColor(TargetColorMode colorMode)
    {
        return colorMode == TargetColorMode.ChangeToSelectedColor ? selectedColor : normalColor;
    }

    private Color ResolveInverseColor(TargetColorMode colorMode)
    {
        return colorMode == TargetColorMode.ChangeToSelectedColor ? normalColor : selectedColor;
    }
}