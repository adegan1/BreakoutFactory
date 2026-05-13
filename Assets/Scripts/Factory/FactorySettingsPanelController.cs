using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class FactorySettingsPanelController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private Image arrowImage;

    [Header("Motion")]
    [SerializeField, Min(0f)] private float slideSpeed = 1400f;
    [SerializeField] private Vector2 hideDirection = Vector2.left;

    [Header("Arrow")]
    [SerializeField] private bool arrowPointsRightByDefault = true;

    private Vector2 expandedAnchoredPosition;
    private Vector2 collapsedAnchoredPosition;
    private bool hasCachedPositions;
    private bool isExpanded = true;
    private bool targetExpanded = true;

    public bool IsExpanded => targetExpanded;

    private void Awake()
    {
        ResolveReferences();
        targetExpanded = GameSettings.HasInstance ? GameSettings.Instance.ShowControls : true;
    }

    private void Start()
    {
        EnsureCachedPositions();
        SnapToTargetState();
    }

    private void Update()
    {
        if (!hasCachedPositions || panelRoot == null)
        {
            return;
        }

        Vector2 targetPosition = targetExpanded ? expandedAnchoredPosition : collapsedAnchoredPosition;
        if (slideSpeed <= 0f)
        {
            panelRoot.anchoredPosition = targetPosition;
            return;
        }

        panelRoot.anchoredPosition = Vector2.MoveTowards(
            panelRoot.anchoredPosition,
            targetPosition,
            slideSpeed * Time.unscaledDeltaTime);
    }

    public void ToggleExpanded()
    {
        SetExpanded(!targetExpanded);
    }

    public void SetExpanded(bool expanded)
    {
        SetExpanded(expanded, true);
    }

    public void SetExpanded(bool expanded, bool persistSetting)
    {
        ResolveReferences();
        EnsureCachedPositions();

        targetExpanded = expanded;
        isExpanded = expanded;
        UpdateArrowVisual();

        if (panelRoot != null && !Application.isPlaying)
        {
            SnapToTargetState();
        }

        if (persistSetting)
        {
            GameSettings.Instance.SetShowControls(expanded);
        }
    }

    private void ResolveReferences()
    {
        if (panelRoot == null)
        {
            panelRoot = transform as RectTransform;
        }
    }

    private void EnsureCachedPositions()
    {
        if (hasCachedPositions || panelRoot == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRoot);

        if (panelRoot.rect.width <= Mathf.Epsilon && panelRoot.rect.height <= Mathf.Epsilon)
        {
            return;
        }

        expandedAnchoredPosition = panelRoot.anchoredPosition;
        collapsedAnchoredPosition = expandedAnchoredPosition + GetHideOffset();
        hasCachedPositions = true;
    }

    private Vector2 GetHideOffset()
    {
        Vector2 direction = hideDirection.sqrMagnitude > 0.0001f
            ? hideDirection.normalized
            : Vector2.left;

        Vector2 offset = Vector2.zero;
        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
        {
            offset.x = Mathf.Sign(direction.x) * panelRoot.rect.width;
        }
        else
        {
            offset.y = Mathf.Sign(direction.y) * panelRoot.rect.height;
        }

        return offset;
    }

    private void SnapToTargetState()
    {
        if (panelRoot == null)
        {
            return;
        }

        panelRoot.anchoredPosition = targetExpanded ? expandedAnchoredPosition : collapsedAnchoredPosition;
        UpdateArrowVisual();
    }

    private void UpdateArrowVisual()
    {
        if (arrowImage == null)
        {
            return;
        }

        bool panelHidesLeft = hideDirection.x < 0f && Mathf.Abs(hideDirection.x) >= Mathf.Abs(hideDirection.y);
        bool shouldPointRight = panelHidesLeft ? !targetExpanded : targetExpanded;

        float arrowRotation = shouldPointRight == arrowPointsRightByDefault ? 0f : 180f;
        arrowImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, arrowRotation);
    }
}