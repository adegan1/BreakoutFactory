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

    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private bool isHovered;

    public string TooltipTitle => tooltipTitle;
    public string TooltipDescription => tooltipDescription;

    // Override tooltip content at runtime (e.g. when slot data changes).
    public void SetContent(string title, string description)
    {
        tooltipTitle = title;
        tooltipDescription = description;
    }

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        parentCanvas = GetComponentInParent<Canvas>();
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
        HideTooltip();
    }

    private void ShowTooltip()
    {
        if (string.IsNullOrEmpty(tooltipTitle) && string.IsNullOrEmpty(tooltipDescription))
        {
            return;
        }

        isHovered = true;
        TooltipUI.Show(tooltipTitle, tooltipDescription);
    }

    private void HideTooltip()
    {
        isHovered = false;
        TooltipUI.Hide();
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
