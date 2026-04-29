using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MachineRadialProgressView : MonoBehaviour
{
    private const float VisibleAlphaThreshold = 0.001f;

    [Header("References")]
    [SerializeField] private Image radialFillImage;
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
    [SerializeField] private bool faceWorldCamera = true;
    [SerializeField] private Camera worldCamera;

    private IMachineResourceProgressProvider provider;
    private BuildingInstance machineInstance;
    private int machineInstanceId = -1;
    private float visibilityAlpha;

    private struct DisplayState
    {
        public bool HasProvider;
        public float TargetFill;
        public Color TargetColor;
        public float TargetAlpha;
    }

    private void Reset()
    {
        radialFillImage = GetComponentInChildren<Image>();
    }

    private void Awake()
    {
        ResolveReferencesIfNeeded();
        ResolveProviderIfNeeded();
        ResolveMachineInstanceIfNeeded();
        ApplyImmediateState();
    }

    private void LateUpdate()
    {
        ResolveReferencesIfNeeded();
        ResolveProviderIfNeeded();
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

        if (tileManager == null)
        {
            tileManager = FindFirstObjectByType<TileManager>();
        }

        if (worldCamera == null)
        {
            worldCamera = Camera.main;
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
    }

    private DisplayState BuildDisplayState()
    {
        bool hasProvider = provider != null;
        float targetFill = hasProvider ? provider.NormalizedResourceAmount : 0f;
        Color targetColor = hasProvider ? provider.ResourceTint : Color.white;
        bool shouldBeVisible = hasProvider
            ? IsVisibleByInteractionContext() && !(hideWhenZero && targetFill <= 0f)
            : !hideWhenNoProvider;

        DisplayState state = new DisplayState
        {
            HasProvider = hasProvider,
            TargetFill = targetFill,
            TargetColor = targetColor,
            TargetAlpha = shouldBeVisible ? 1f : 0f
        };

        return state;
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
        if (radialFillImage != null && radialFillImage.enabled != isVisible)
        {
            radialFillImage.enabled = isVisible;
        }
    }

    private void UpdateBillboard()
    {
        if (!faceWorldCamera || worldCamera == null)
        {
            return;
        }

        Vector3 cameraForward = worldCamera.transform.forward;
        transform.rotation = Quaternion.LookRotation(cameraForward, Vector3.up);
    }
}