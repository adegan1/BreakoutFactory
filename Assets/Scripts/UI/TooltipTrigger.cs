using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;


// Add to any UI GameObject to make it show a tooltip on hover.
// Wire up via the Inspector (static content) or call SetContent() at runtime
[DisallowMultipleComponent]
public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private string tooltipTitle;
    [SerializeField, TextArea(2, 4)] private string tooltipDescription;

    private Func<string> tooltipTitleProvider;
    private Func<string> tooltipDescriptionProvider;

    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private bool isHovered;

    public string TooltipTitle => tooltipTitle;
    public string TooltipDescription => tooltipDescription;

    // Override tooltip content at runtime (e.g. when slot data changes).
    public void SetContent(string title, string description)
    {
        tooltipTitleProvider = null;
        tooltipDescriptionProvider = null;
        tooltipTitle = title;
        tooltipDescription = description;

        if (isHovered)
        {
            ShowTooltip();
        }
    }

    public void SetContentProviders(Func<string> titleProvider, Func<string> descriptionProvider)
    {
        tooltipTitleProvider = titleProvider;
        tooltipDescriptionProvider = descriptionProvider;
        tooltipTitle = string.Empty;
        tooltipDescription = string.Empty;

        if (isHovered)
        {
            ShowTooltip();
        }
    }

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        parentCanvas = GetComponentInParent<Canvas>();
    }

    private void OnEnable()
    {
        GameSettings.LanguageChanged += HandleLanguageChanged;
    }

    private void Update()
    {
        if (rectTransform == null)
        {
            return;
        }

        if (!TryGetPointerScreenPosition(out Vector2 pointerScreenPosition))
        {
            return;
        }

        Camera eventCamera = null;
        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            eventCamera = parentCanvas.worldCamera;
        }

        bool isPointerOver = RectTransformUtility.RectangleContainsScreenPoint(rectTransform, pointerScreenPosition, eventCamera);
        if (isPointerOver && !isHovered)
        {
            ShowTooltip();
        }
        else if (!isPointerOver && isHovered)
        {
            HideTooltip();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }

    private void OnMouseEnter()
    {
        ShowTooltip();
    }

    private void OnMouseExit()
    {
        HideTooltip();
    }

    private void OnDisable()
    {
        GameSettings.LanguageChanged -= HandleLanguageChanged;
        HideTooltip();
    }

    private void ShowTooltip()
    {
        string resolvedTitle = ResolveTitle();
        string resolvedDescription = ResolveDescription();

        if (string.IsNullOrEmpty(resolvedTitle) && string.IsNullOrEmpty(resolvedDescription))
        {
            return;
        }

        isHovered = true;
        TooltipUI.Show(resolvedTitle, resolvedDescription);
    }

    private void HideTooltip()
    {
        isHovered = false;
        TooltipUI.Hide();
    }

    private void HandleLanguageChanged(GameSettings.Language _)
    {
        if (!isHovered)
        {
            return;
        }

        ShowTooltip();
    }

    private string ResolveTitle()
    {
        if (tooltipTitleProvider != null)
        {
            return tooltipTitleProvider.Invoke() ?? string.Empty;
        }

        return tooltipTitle;
    }

    private string ResolveDescription()
    {
        if (tooltipDescriptionProvider != null)
        {
            return tooltipDescriptionProvider.Invoke() ?? string.Empty;
        }

        return tooltipDescription;
    }

    private static bool TryGetPointerScreenPosition(out Vector2 screenPosition)
    {
        Pointer pointer = Pointer.current;
        if (pointer == null)
        {
            screenPosition = default;
            return false;
        }

        screenPosition = pointer.position.ReadValue();
        return true;
    }
}
