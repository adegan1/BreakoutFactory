using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Singleton tooltip panel. Place on a Canvas GameObject with a title TMP label,
// description TMP label, and a background panel. The panel follows the cursor.
//
// Setup:
//   1. Create a Canvas (Sort Order: high, e.g. 100) named "TooltipCanvas".
//   2. Add a child Panel with a VerticalLayoutGroup + ContentSizeFitter (preferred size both axes).
//   3. Add two TMP labels inside it: one for the title, one for the description.
//   4. Add this component to the Panel and wire up the fields.
//   5. Disable the Panel GameObject by default (it starts hidden).
[DisallowMultipleComponent]
public class TooltipUI : MonoBehaviour
{
    private static TooltipUI instance;

    [Header("References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private RectTransform descriptionContainer;
    [SerializeField] private RectTransform panelRoot;

    [Header("Settings")]
    [SerializeField] private Vector2 cursorOffset = new Vector2(16f, -16f);
    [SerializeField] private float edgePaddingLeft = 8f;
    [SerializeField] private float edgePaddingRight = 8f;
    [SerializeField] private float edgePaddingTop = 8f;
    [SerializeField] private float edgePaddingBottom = 8f;

    [Header("Width Expansion")]
    [SerializeField] private float defaultDescriptionContainerWidth = 200f;
    [SerializeField] private float expandedDescriptionContainerWidth = 320f;
    [SerializeField] private float heightThresholdForExpansion = 80f;

    private Canvas parentCanvas;
    private RectTransform canvasRect;

    public static bool HasInstance => instance != null;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public static void Show(string title, string description)
    {
        if (!EnsureInstance())
        {
            return;
        }

        instance.ShowInternal(title, description);
    }

    public static void Hide()
    {
        if (!EnsureInstance())
        {
            return;
        }

        if (instance.panelRoot != null)
        {
            instance.panelRoot.gameObject.SetActive(false);
        }
    }

    private void ShowInternal(string title, string description)
    {
        if (titleText != null)
        {
            titleText.text = title;
            titleText.gameObject.SetActive(!string.IsNullOrEmpty(title));
        }

        if (descriptionText != null)
        {
            descriptionText.text = description;
            descriptionText.gameObject.SetActive(!string.IsNullOrEmpty(description));
        }

        if (panelRoot != null)
        {
            panelRoot.gameObject.SetActive(true);

            // Pre-check how tall the description would be at the default width.
            // If it exceeds the threshold, widen the description container before rebuilding so
            // TMP can reflow to fewer lines in a single layout pass.
            float targetWidth = defaultDescriptionContainerWidth;
            if (descriptionText != null && !string.IsNullOrEmpty(descriptionText.text))
            {
                Vector2 textSize = descriptionText.GetPreferredValues(
                    descriptionText.text, defaultDescriptionContainerWidth, float.PositiveInfinity);
                if (textSize.y > heightThresholdForExpansion)
                    targetWidth = expandedDescriptionContainerWidth;
            }

            RectTransform widthTarget = descriptionContainer != null ? descriptionContainer : panelRoot;
            widthTarget.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(panelRoot);
        }

        UpdatePosition();
    }

    private void LateUpdate()
    {
        if (panelRoot != null && panelRoot.gameObject.activeSelf)
        {
            UpdatePosition();
        }
    }

    private void UpdatePosition()
    {
        if (panelRoot == null)
        {
            return;
        }

        EnsureCanvasReference();
        if (parentCanvas == null)
        {
            return;
        }

        if (!TryGetPointerScreenPosition(out Vector2 screenPoint))
        {
            return;
        }

        // Convert screen point to canvas local position.
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera,
            out Vector2 localPoint);

        Vector2 targetPos = localPoint + cursorOffset;

        // Clamp so the panel stays within the canvas bounds.
        Vector2 canvasSize = canvasRect.rect.size;
        Vector2 panelSize = panelRoot.rect.size;

        // Anchor the bottom of the description text to the cursor.
        // Compute where the description text's bottom edge sits relative to the panel's pivot,
        // then shift the panel so that edge lands at the cursor position.
        if (descriptionText != null && descriptionText.gameObject.activeSelf)
        {
            RectTransform descRect = descriptionText.rectTransform;
            // Bottom edge of description text in panel-local space (relative to panel pivot).
            float descBottomLocal = descRect.localPosition.y - descRect.pivot.y * descRect.rect.height;
            targetPos.y -= descBottomLocal;
        }
        else
        {
            // No description visible; fall back to anchoring the panel bottom to the cursor.
            targetPos.y += panelSize.y * (1f - panelRoot.pivot.y);
        }

        float minX = -canvasSize.x * 0.5f + edgePaddingLeft;
        float maxX = canvasSize.x * 0.5f - panelSize.x - edgePaddingRight;
        float minY = -canvasSize.y * 0.5f + panelSize.y + edgePaddingBottom;
        float maxY = canvasSize.y * 0.5f - edgePaddingTop;

        targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
        targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);

        panelRoot.anchoredPosition = targetPos;
    }

    private void EnsureCanvasReference()
    {
        if (parentCanvas != null)
        {
            return;
        }

        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            canvasRect = parentCanvas.GetComponent<RectTransform>();
        }
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

    private static bool EnsureInstance()
    {
        if (instance != null)
        {
            return true;
        }

        TooltipUI[] candidates = Resources.FindObjectsOfTypeAll<TooltipUI>();
        for (int i = 0; i < candidates.Length; i++)
        {
            TooltipUI candidate = candidates[i];
            if (candidate == null)
            {
                continue;
            }

            if (!candidate.gameObject.scene.IsValid())
            {
                continue;
            }

            instance = candidate;
            return true;
        }

        return false;
    }
}
