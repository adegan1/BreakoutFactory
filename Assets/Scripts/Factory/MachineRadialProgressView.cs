using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MachineRadialProgressView : MonoBehaviour
{
    private const float VisibleAlphaThreshold = 0.001f;

    [Header("References")]
    [SerializeField] private Image radialFillImage;
    [SerializeField] private Image statusIconImage;
    [SerializeField] private Sprite questionMarkSprite;
    [SerializeField] private bool resolveProviderFromParents = true;
    [SerializeField] private TileManager tileManager;

    [Header("Display")]
    [SerializeField] private bool smoothFill = true;
    [SerializeField, Min(0f)] private float fillLerpSpeed = 6f;
    [SerializeField] private bool smoothVisibility = true;
    [SerializeField, Min(0f)] private float visibilityLerpSpeed = 10f;
    [SerializeField] private bool hideWhenNoProvider = true;
    [SerializeField] private bool hideWhenZero = false;
    [SerializeField] private bool onlyShowWhenHoveredOrSelected = true;

    [Header("Billboard")]
    [SerializeField] private bool keepDefaultRotation = true;
    [SerializeField] private bool keepDefaultWorldScale = true;
    [SerializeField] private bool faceWorldCamera = true;
    [SerializeField] private Camera worldCamera;

    private IMachineResourceProgressProvider provider;
    private IMachineProgressDisplayInfo progressDisplayInfo;
    private BuildingInstance machineInstance;
    private int machineInstanceId = -1;
    private float visibilityAlpha;
    private Vector3 defaultLocalScale = Vector3.one;
    private Vector3 baselineParentLossyScale = Vector3.one;
    private bool hasScaleBaseline;
    private Quaternion defaultLocalRotation = Quaternion.identity;
    private Quaternion defaultWorldRotation = Quaternion.identity;
    private RectTransform statusIconRectTransform;

    private struct DisplayState
    {
        public bool HasProvider;
        public float TargetFill;
        public Color TargetColor;
        public float TargetAlpha;
        public bool HasStatusIcon;
        public Sprite TargetStatusSprite;
        public Color TargetStatusColor;
        public float StatusTargetAlpha;
    }

    private void Reset()
    {
        radialFillImage = GetComponentInChildren<Image>();
        ResolveStatusIconIfNeeded();
    }

    private void Awake()
    {
        defaultLocalRotation = transform.localRotation;
        defaultLocalScale = transform.localScale;
        defaultWorldRotation = FactoryGridDirectionUtility.CalculateUnrotatedWorldRotation(transform);
        ResolveReferencesIfNeeded();
        ResolveProviderIfNeeded();
        ResolveProgressDisplayInfoIfNeeded();
        ResolveMachineInstanceIfNeeded();
        ApplyImmediateState();
    }

    private void LateUpdate()
    {
        ResolveReferencesIfNeeded();
        ResolveProviderIfNeeded();
        ResolveProgressDisplayInfoIfNeeded();
        ResolveMachineInstanceIfNeeded();
        UpdateFill();
        UpdateBillboard();
    }

    private void ResolveReferencesIfNeeded()
    {
        if (radialFillImage == null)
        {
            radialFillImage = GetComponentInChildren<Image>();
        }

        ResolveStatusIconIfNeeded();

        if (tileManager == null)
        {
            tileManager = FindFirstObjectByType<TileManager>();
        }

        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }
    }

    private void ResolveStatusIconIfNeeded()
    {
        if (statusIconImage != null)
        {
            statusIconRectTransform = statusIconImage.rectTransform;
            return;
        }

        if (radialFillImage == null)
        {
            return;
        }

        Image[] images = GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null && images[i] != radialFillImage)
            {
                statusIconImage = images[i];
                statusIconRectTransform = statusIconImage.rectTransform;
                return;
            }
        }
    }

    private void ResolveProgressDisplayInfoIfNeeded()
    {
        if (progressDisplayInfo != null)
        {
            MonoBehaviour displayBehaviour = progressDisplayInfo as MonoBehaviour;
            if (displayBehaviour != null)
            {
                return;
            }

            progressDisplayInfo = null;
        }

        if (resolveProviderFromParents)
        {
            progressDisplayInfo = GetComponentInParent<IMachineProgressDisplayInfo>();
        }
        else
        {
            progressDisplayInfo = GetComponent<IMachineProgressDisplayInfo>();
        }

        if (progressDisplayInfo == null)
        {
            progressDisplayInfo = provider as IMachineProgressDisplayInfo;
        }
    }

    private void ResolveProviderIfNeeded()
    {
        if (provider != null)
        {
            MonoBehaviour providerBehaviour = provider as MonoBehaviour;
            if (providerBehaviour != null)
            {
                return;
            }

            provider = null;
        }

        if (resolveProviderFromParents)
        {
            provider = GetComponentInParent<IMachineResourceProgressProvider>();
        }
        else
        {
            provider = GetComponent<IMachineResourceProgressProvider>();
        }
    }

    private void ResolveMachineInstanceIfNeeded()
    {
        if (machineInstance == null)
        {
            machineInstance = GetComponentInParent<BuildingInstance>();
        }

        machineInstanceId = machineInstance != null
            ? machineInstance.gameObject.GetInstanceID()
            : -1;
    }

    private void ApplyImmediateState()
    {
        if (radialFillImage == null)
        {
            return;
        }

        DisplayState state = BuildDisplayState();
        radialFillImage.fillAmount = state.TargetFill;
        visibilityAlpha = state.TargetAlpha;
        ApplyTintWithVisibility(state.TargetColor, visibilityAlpha);
        SetFillVisible(visibilityAlpha > VisibleAlphaThreshold);
        ApplyStatusIcon(state);
    }

    private void UpdateFill()
    {
        if (radialFillImage == null)
        {
            return;
        }

        DisplayState state = BuildDisplayState();

        if (state.HasProvider)
        {
            if (smoothFill)
            {
                float speed = Mathf.Max(0f, fillLerpSpeed);
                if (speed > 0f)
                {
                    float t = 1f - Mathf.Exp(-speed * Time.deltaTime);
                    radialFillImage.fillAmount = Mathf.Lerp(radialFillImage.fillAmount, state.TargetFill, t);
                }
                else
                {
                    radialFillImage.fillAmount = state.TargetFill;
                }
            }
            else
            {
                radialFillImage.fillAmount = state.TargetFill;
            }
        }

        if (smoothVisibility)
        {
            float speed = Mathf.Max(0f, visibilityLerpSpeed);
            visibilityAlpha = speed > 0f
                ? Mathf.MoveTowards(visibilityAlpha, state.TargetAlpha, speed * Time.deltaTime)
                : state.TargetAlpha;
        }
        else
        {
            visibilityAlpha = state.TargetAlpha;
        }

        ApplyTintWithVisibility(state.TargetColor, visibilityAlpha);
        SetFillVisible(visibilityAlpha > VisibleAlphaThreshold);
        ApplyStatusIcon(state);
    }

    private DisplayState BuildDisplayState()
    {
        bool hasProvider = provider != null;
        float targetFill = hasProvider ? provider.NormalizedResourceAmount : 0f;
        Color targetColor = hasProvider ? provider.ResourceTint : Color.white;
        bool hasStatusIconState = progressDisplayInfo != null && progressDisplayInfo.HasProgressDisplay;
        bool shouldBeVisible = hasProvider
            ? IsVisibleByInteractionContext() && (!(hideWhenZero && targetFill <= 0f) || hasStatusIconState)
            : !hideWhenNoProvider;

        DisplayState state = new DisplayState
        {
            HasProvider = hasProvider,
            TargetFill = targetFill,
            TargetColor = targetColor,
            TargetAlpha = shouldBeVisible ? 1f : 0f
        };

        bool hasStatusIcon = hasStatusIconState;
        Sprite targetStatusSprite = null;
        Color targetStatusColor = Color.white;
        if (hasStatusIcon)
        {
            targetStatusSprite = progressDisplayInfo.UseQuestionMarkSprite
                ? questionMarkSprite
                : progressDisplayInfo.ProgressDisplaySprite;

            if (targetStatusSprite == null)
            {
                targetStatusSprite = questionMarkSprite;
            }

            targetStatusColor = progressDisplayInfo.ProgressDisplayTint;
        }

        state.HasStatusIcon = hasStatusIcon && targetStatusSprite != null;
        state.TargetStatusSprite = targetStatusSprite;
        state.TargetStatusColor = targetStatusColor;
        state.StatusTargetAlpha = state.TargetAlpha;

        return state;
    }

    private void ApplyStatusIcon(DisplayState state)
    {
        if (statusIconImage == null)
        {
            return;
        }

        if (!state.HasStatusIcon)
        {
            if (statusIconImage.enabled)
            {
                statusIconImage.enabled = false;
            }

            return;
        }

        if (!statusIconImage.gameObject.activeSelf)
        {
            statusIconImage.gameObject.SetActive(true);
        }

        if (statusIconImage.sprite != state.TargetStatusSprite)
        {
            statusIconImage.sprite = state.TargetStatusSprite;
        }

        Color output = state.TargetStatusColor;
        output.a *= Mathf.Clamp01(state.StatusTargetAlpha);
        statusIconImage.color = output;

        if (statusIconImage.enabled != state.StatusTargetAlpha > VisibleAlphaThreshold)
        {
            statusIconImage.enabled = state.StatusTargetAlpha > VisibleAlphaThreshold;
        }
    }

    private void ApplyTintWithVisibility(Color tint, float alpha)
    {
        if (radialFillImage == null)
        {
            return;
        }

        Color output = tint;
        output.a *= Mathf.Clamp01(alpha);
        radialFillImage.color = output;
    }

    private bool IsVisibleByInteractionContext()
    {
        if (!onlyShowWhenHoveredOrSelected)
        {
            return true;
        }

        if (FactoryBuildingPlacer.AreMachineProgressBarsPinnedVisible)
        {
            return true;
        }

        if (IsMouseHoveringThisBuilding())
        {
            return true;
        }

        if (machineInstanceId < 0)
        {
            return false;
        }

        return machineInstanceId == FactoryBuildingPlacer.HoveredMachineInstanceId
            || machineInstanceId == FactoryBuildingPlacer.SelectedMachineInstanceId;
    }

    private bool IsMouseHoveringThisBuilding()
    {
        if (machineInstance == null || tileManager == null || worldCamera == null || Mouse.current == null)
        {
            return false;
        }

        Vector2Int footprint = machineInstance.FootprintSize;
        if (footprint.x <= 0 || footprint.y <= 0)
        {
            return false;
        }

        Ray mouseRay = worldCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane gridPlane = new Plane(Vector3.forward, new Vector3(0f, 0f, tileManager.GridPlaneZ));
        if (!gridPlane.Raycast(mouseRay, out float hitDistance))
        {
            return false;
        }

        Vector3 hitPoint = mouseRay.GetPoint(hitDistance);
        Vector2Int hoverTile = tileManager.WorldToGrid(hitPoint);

        Vector2Int topLeft = machineInstance.GridPosition;
        return hoverTile.x >= topLeft.x
            && hoverTile.x < topLeft.x + footprint.x
            && hoverTile.y >= topLeft.y
            && hoverTile.y < topLeft.y + footprint.y;
    }

    private void SetFillVisible(bool isVisible)
    {
        if (radialFillImage == null)
        {
            return;
        }

        if (isVisible && !radialFillImage.gameObject.activeSelf)
        {
            radialFillImage.gameObject.SetActive(true);
        }

        if (radialFillImage.enabled != isVisible)
        {
            radialFillImage.enabled = isVisible;
        }
    }

    private void UpdateBillboard()
    {
        if (keepDefaultRotation)
        {
            transform.rotation = defaultWorldRotation;
            ApplyScaleCompensationIfNeeded();
            ApplyStatusIconDefaultRotationIfNeeded();
            return;
        }

        if (!faceWorldCamera || worldCamera == null)
        {
            ApplyScaleCompensationIfNeeded();
            ApplyStatusIconDefaultRotationIfNeeded();
            return;
        }

        Vector3 cameraForward = worldCamera.transform.forward;
        transform.rotation = Quaternion.LookRotation(cameraForward, Vector3.up);
        ApplyScaleCompensationIfNeeded();
        ApplyStatusIconDefaultRotationIfNeeded();
    }


    private void ApplyStatusIconDefaultRotationIfNeeded()
    {
        if (statusIconImage == null || statusIconRectTransform == null)
        {
            return;
        }

        statusIconRectTransform.rotation = transform.rotation;
    }

    private void ApplyScaleCompensationIfNeeded()
    {
        if (!keepDefaultWorldScale)
        {
            return;
        }

        Transform parent = transform.parent;
        if (parent == null)
        {
            transform.localScale = defaultLocalScale;
            return;
        }

        Vector3 parentLossyScale = parent.lossyScale;
        if (!hasScaleBaseline)
        {
            baselineParentLossyScale = parentLossyScale;
            hasScaleBaseline = true;
        }

        transform.localScale = new Vector3(
            defaultLocalScale.x * SafeDivide(baselineParentLossyScale.x, parentLossyScale.x),
            defaultLocalScale.y * SafeDivide(baselineParentLossyScale.y, parentLossyScale.y),
            defaultLocalScale.z * SafeDivide(baselineParentLossyScale.z, parentLossyScale.z));
    }

    private static float SafeDivide(float value, float divisor)
    {
        return Mathf.Abs(divisor) > 0.0001f ? value / divisor : value;
    }

}