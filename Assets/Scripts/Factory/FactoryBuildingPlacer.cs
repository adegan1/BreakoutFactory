using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;
using System;

public class FactoryBuildingPlacer : MonoBehaviour
{
    public static bool AreMachineProgressBarsPinnedVisible { get; private set; }
    public static int HoveredMachineInstanceId { get; private set; } = -1;
    public static int SelectedMachineInstanceId { get; private set; } = -1;

    [SerializeField] private TileManager tileManager;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private InventoryManager inventoryManager;
    [FormerlySerializedAs("buildingPrefab")]
    [SerializeField] private GameObject defaultBuildingPrefab;
    [SerializeField] private LayerMask placementSurfaceMask = ~0;

    [Header("Building Selection")]
    [SerializeField] private int selectedBuildingIndex;
    [SerializeField] private bool hasSelectedBuilding = true;
    [SerializeField] private bool enableNumberKeySelection = true;
    [SerializeField] private bool enableRotationInput = true;

    [Header("Remove Behavior")]
    [SerializeField] private bool refundBuildingToInventoryOnRemove = true;

    [Header("Hover Highlight")]
    [SerializeField] private Transform hoverHighlight;
    [SerializeField] private Color validHoverColor = new Color(0.25f, 1f, 0.45f, 0.5f);
    [SerializeField] private Color blockedHoverColor = new Color(1f, 0.3f, 0.3f, 0.5f);
    [SerializeField] private float hoverZOffset = -0.1f;

    [Header("Placement Indicators")]
    [FormerlySerializedAs("conveyorDirectionIndicator")]
    [SerializeField] private Transform directionIndicator;
    [SerializeField] private Transform inputIndicator;
    [SerializeField] private Transform inputIndicatorSecondary;
    [SerializeField] private Transform outputIndicator;
    [FormerlySerializedAs("conveyorIndicatorColor")]
    [SerializeField] private Color indicatorColor = new Color(1f, 1f, 1f, 0.85f);

    [Header("Debug Gizmos")]
    [SerializeField] private bool drawSelectedInputTileGizmo = true;
    [SerializeField] private Color inputTileGizmoColor = new Color(1f, 0.85f, 0.15f, 0.9f);
    [SerializeField, Min(0.01f)] private float inputTileGizmoRadius = 0.12f;

    [Header("UI Interaction")]
    [SerializeField] private LayerMask blockingUiLayers = 1 << 5;

    [Header("Factory Speed")]
    [SerializeField, Min(0.1f)] private float normalFactorySpeed = 1f;
    [SerializeField, Min(0.1f)] private float boostedFactorySpeed = 2f;
    [SerializeField] private bool enableShiftSpeedBoost = true;
    [SerializeField] private Toggle normalSpeedToggle;
    [SerializeField] private Toggle doubleSpeedToggle;

    [Header("Panels")]
    [SerializeField] private GameObject controlsPanelRoot;
    [SerializeField] private FactorySettingsPanelController settingsPanelController;

    private readonly Dictionary<Vector2Int, PlacedBuildingRecord> spawnedByCell = new();
    private readonly Dictionary<int, PlacedBuildingRecord> buildingsByInstanceId = new();
    private SpriteRenderer hoverHighlightRenderer;
    private SpriteRenderer directionIndicatorRenderer;
    private SpriteRenderer inputIndicatorRenderer;
    private SpriteRenderer inputIndicatorSecondaryRenderer;
    private SpriteRenderer outputIndicatorRenderer;
    private Sprite defaultHoverSprite;
    private Quaternion defaultHoverRotation = Quaternion.identity;
    private bool suppressHoverUntilTileChange;
    private Vector2Int suppressedHoverTile;
    private bool hasPointerTile;
    private Vector2Int pointerGridPosition;
    private Vector3 pointerWorldPoint;
    private int selectedRotationQuarterTurns;
    private float selectedFactorySpeed = 1f;
    private float defaultFixedDeltaTime = 0.02f;
    private bool isShiftSpeedOverrideActive;
    private readonly List<Vector2Int> reusableInputTiles = new();
    private readonly List<ItemEntity> reusableItemsOnInput = new();
    private static readonly List<RaycastResult> reusableUiRaycastResults = new();
    private static readonly Vector2Int[] CardinalDirections =
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left
    };

    public int SelectedBuildingIndex => selectedBuildingIndex;
    public bool HasSelectedBuilding => hasSelectedBuilding;
    public BuildingDefinition SelectedBuildingDefinition => GetSelectedBuildingDefinition();
    public GameObject SelectedBuildingPrefab => GetSelectedBuildingDefinition()?.BehaviorPrefab;
    public int SelectedRotationQuarterTurns => selectedRotationQuarterTurns;

    private class PlacedBuildingRecord
    {
        public readonly GameObject SpawnedObject;
        public readonly BuildingDefinition Definition;
        public readonly Vector2Int TopLeftGridPosition;
        public readonly Vector2Int FootprintSize;
        public readonly int PlacedRotationQuarterTurns;

        public PlacedBuildingRecord(GameObject spawnedObject, BuildingDefinition definition, Vector2Int topLeft, Vector2Int footprintSize, int placedRotationQuarterTurns)
        {
            SpawnedObject = spawnedObject;
            Definition = definition;
            TopLeftGridPosition = topLeft;
            FootprintSize = footprintSize;
            PlacedRotationQuarterTurns = placedRotationQuarterTurns;
        }
    }

    private void Reset()
    {
        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        if (inventoryManager == null)
        {
            inventoryManager = FindFirstObjectByType<InventoryManager>();
        }
    }

    private void Awake()
    {
        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        EnsureInventoryManagerAssigned();
        if (inventoryManager == null || inventoryManager.BuildingItems.Count == 0)
        {
            hasSelectedBuilding = false;
        }
        ClampSelectedBuildingIndex();

        if (hoverHighlight != null)
        {
            hoverHighlightRenderer = hoverHighlight.GetComponent<SpriteRenderer>();
            defaultHoverRotation = hoverHighlight.rotation;
            if (hoverHighlightRenderer != null)
            {
                defaultHoverSprite = hoverHighlightRenderer.sprite;
            }
        }

        if (directionIndicator != null)
        {
            directionIndicatorRenderer = directionIndicator.GetComponent<SpriteRenderer>();
        }

        if (inputIndicator != null)
        {
            inputIndicatorRenderer = inputIndicator.GetComponent<SpriteRenderer>();
        }

        if (inputIndicatorSecondary != null)
        {
            inputIndicatorSecondaryRenderer = inputIndicatorSecondary.GetComponent<SpriteRenderer>();
        }

        if (outputIndicator != null)
        {
            outputIndicatorRenderer = outputIndicator.GetComponent<SpriteRenderer>();
        }

        defaultFixedDeltaTime = Time.fixedDeltaTime;
        selectedFactorySpeed = Mathf.Max(0.1f, normalFactorySpeed);
        ApplyFactorySpeed(selectedFactorySpeed);
        ApplySavedSettings();

        HoveredMachineInstanceId = -1;
        SelectedMachineInstanceId = -1;
    }

    private void OnDisable()
    {
        RevertGlobalTimeToNormal();
    }

    private void OnDestroy()
    {
        RevertGlobalTimeToNormal();
    }

    private void Update()
    {
        EnsureInventoryManagerAssigned();
        HandleFactorySpeedInput();
        MaintainSpeedToggleSelectionVisual();

        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            hasPointerTile = false;
            HoveredMachineInstanceId = -1;
            SetHoverHighlightVisible(false);
            SetAllIndicatorsVisible(false);
            return;
        }

        HandleBuildingSelectionInput();
        HandleRotationInput();
        RefreshSelectionAvailability();

        RefreshPointerState(mouse);
        UpdateMachineProgressVisibilityContext();

        bool pointerOverUi = IsPointerOverBlockingUi();

        if (pointerOverUi)
        {
            SetHoverHighlightVisible(false);
            SetAllIndicatorsVisible(false);
        }
        else
        {
            UpdateHoverHighlight();
            UpdateBuildingIndicators();
        }

        if (mouse.leftButton.wasPressedThisFrame && !pointerOverUi)
        {
            bool didPlace = TryPlaceAtPointer();
            if (didPlace)
            {
                SuppressHoverAtPointerTile();
            }
            else
            {
                TrySelectMachineAtPointer();
            }
        }

        if (mouse.rightButton.wasPressedThisFrame)
        {
            if (pointerOverUi)
            {
                DeselectBuilding();
            }
            else
            {
                TryRemoveAtPointer();
            }
        }
    }

    private void UpdateHoverHighlight()
    {
        if (hoverHighlight == null)
        {
            return;
        }

        if (!hasPointerTile)
        {
            SetHoverHighlightVisible(false);
            return;
        }

        if (suppressHoverUntilTileChange)
        {
            if (pointerGridPosition == suppressedHoverTile)
            {
                SetHoverHighlightVisible(false);
                return;
            }

            suppressHoverUntilTileChange = false;
        }

        BuildingDefinition selectedDef = GetSelectedBuildingDefinition();
        Vector2Int footprintSize = selectedDef != null ? GetRotatedFootprintSize(selectedDef.FootprintSize) : Vector2Int.one;
        Vector2Int optimalTopLeft = selectedDef != null ? CalculateOptimalPlacementPosition(pointerGridPosition, footprintSize) : pointerGridPosition;

        // Check if there's a building at the pointer (for removal)
        if (spawnedByCell.TryGetValue(pointerGridPosition, out PlacedBuildingRecord buildingAtPointer) && buildingAtPointer != null)
        {
            bool isReplacement = selectedDef != null && CanReplaceConveyorAt(optimalTopLeft, footprintSize, selectedDef);
            if (!isReplacement)
            {
                DisplayFootprintHighlight(buildingAtPointer.TopLeftGridPosition, buildingAtPointer.FootprintSize, blockedHoverColor);
                ApplyDefaultHoverVisual(blockedHoverColor);
                return;
            }
            // Conveyor replacement: fall through to show placement preview
        }

        if (selectedDef != null)
        {
            bool canPlace = CanPlaceSelectedBuildingAt(pointerGridPosition);
            Color previewColor = canPlace ? validHoverColor : blockedHoverColor;
            DisplayFootprintHighlight(optimalTopLeft, footprintSize, previewColor);
            ApplyBuildingHoverVisual(selectedDef, optimalTopLeft, canPlace);
            return;
        }

        SetHoverHighlightVisible(false);
    }

    private void UpdateBuildingIndicators()
    {
        if (!hasPointerTile || suppressHoverUntilTileChange && pointerGridPosition == suppressedHoverTile)
        {
            SetAllIndicatorsVisible(false);
            return;
        }

        IndicatorState indicatorState;

        if (spawnedByCell.TryGetValue(pointerGridPosition, out PlacedBuildingRecord record)
            && record != null
            && TryBuildIndicatorState(
                record.Definition,
                record.TopLeftGridPosition,
                record.FootprintSize,
                record.PlacedRotationQuarterTurns,
                out indicatorState))
        {
            ApplyIndicatorState(indicatorState);
            return;
        }

        BuildingDefinition selectedDef = GetSelectedBuildingDefinition();
        if (selectedDef == null)
        {
            SetAllIndicatorsVisible(false);
            return;
        }

        Vector2Int footprintSize = GetRotatedFootprintSize(selectedDef.FootprintSize);
        Vector2Int optimalTopLeft = CalculateOptimalPlacementPosition(pointerGridPosition, footprintSize);

        if (!TryBuildIndicatorState(
                selectedDef,
                optimalTopLeft,
                footprintSize,
                selectedRotationQuarterTurns,
                out indicatorState))
        {
            SetAllIndicatorsVisible(false);
            return;
        }

        ApplyIndicatorState(indicatorState);
    }

    private void ApplyIndicatorState(IndicatorState state)
    {
        ApplyIndicator(
            directionIndicator,
            directionIndicatorRenderer,
            state.HasDirection,
            state.DirectionWorldPosition,
            state.DirectionQuarterTurns,
            indicatorColor);

        ApplyIndicator(
            inputIndicator,
            inputIndicatorRenderer,
            state.HasInput,
            state.InputTile,
            state.InputQuarterTurns,
            indicatorColor);

        ApplyIndicator(
            inputIndicatorSecondary,
            inputIndicatorSecondaryRenderer,
            state.HasSecondaryInput,
            state.SecondaryInputTile,
            state.SecondaryInputQuarterTurns,
            indicatorColor);

        ApplyIndicator(
            outputIndicator,
            outputIndicatorRenderer,
            state.HasOutput,
            state.OutputTile,
            state.OutputQuarterTurns,
            indicatorColor);
    }

    private bool TryBuildIndicatorState(
        BuildingDefinition definition,
        Vector2Int topLeftGridPosition,
        Vector2Int footprintSize,
        int rotationQuarterTurns,
        out IndicatorState indicatorState)
    {
        indicatorState = default;

        if (definition == null || tileManager == null)
        {
            return false;
        }

        int normalizedQuarterTurns = Mathf.Abs(rotationQuarterTurns) % 4;

        if (IsConveyorDefinition(definition))
        {
            Vector2Int outputDirection = ConveyorVisualResolver.DirectionFromQuarterTurns(normalizedQuarterTurns);
            Vector2Int outputTile = topLeftGridPosition + outputDirection;
            if (!tileManager.IsInBounds(outputTile))
            {
                return false;
            }

            indicatorState.HasDirection = true;
            indicatorState.DirectionWorldPosition = tileManager.GridToWorld(outputTile);
            indicatorState.DirectionQuarterTurns = normalizedQuarterTurns;
            return true;
        }

        if (TryGetInputTilesForDefinition(definition, topLeftGridPosition, footprintSize, rotationQuarterTurns, reusableInputTiles)
            && reusableInputTiles.Count > 0)
        {
            indicatorState.HasInput = true;
            
            // Position indicator outside the building, one tile away from the input
            Vector2Int primaryInputTile = reusableInputTiles[0];
            indicatorState.InputTile = GetIndicatorTileOutsideBuilding(primaryInputTile, topLeftGridPosition, footprintSize, rotationQuarterTurns);
            
            Vector2Int inputDirection = primaryInputTile - indicatorState.InputTile;
            indicatorState.InputQuarterTurns = FactoryGridDirectionUtility.DirectionToQuarterTurns(NormalizeCardinal(inputDirection));

            if (reusableInputTiles.Count > 1)
            {
                indicatorState.HasSecondaryInput = true;
                
                Vector2Int secondaryInputTile = reusableInputTiles[1];
                indicatorState.SecondaryInputTile = GetIndicatorTileOutsideBuilding(secondaryInputTile, topLeftGridPosition, footprintSize, rotationQuarterTurns);
                
                Vector2Int secondaryInputDirection = secondaryInputTile - indicatorState.SecondaryInputTile;
                indicatorState.SecondaryInputQuarterTurns = FactoryGridDirectionUtility.DirectionToQuarterTurns(NormalizeCardinal(secondaryInputDirection));
            }

            return true;
        }

        GeneratorBuildingSettings generatorSettings = definition.GeneratorSettings;
        if (generatorSettings == null)
        {
            return false;
        }

        Vector2Int baseDirection = FactoryGridDirectionUtility.GetBaseDirection(generatorSettings.OutputSide);
        Vector2Int worldDirection = FactoryGridDirectionUtility.RotateDirection(baseDirection, rotationQuarterTurns);
        Vector2Int baseOutputOffset = FactoryGridDirectionUtility.GetSideOffset(baseDirection, footprintSize);
        Vector2Int rotatedOutputOffset = FactoryGridDirectionUtility.RotateOffsetAroundFootprintCenter(
            baseOutputOffset,
            footprintSize,
            rotationQuarterTurns);
        Vector2Int generatorOutputTile = topLeftGridPosition + rotatedOutputOffset;

        if (tileManager.IsInBounds(generatorOutputTile))
        {
            indicatorState.HasOutput = true;
            indicatorState.OutputTile = generatorOutputTile;
            indicatorState.OutputQuarterTurns = FactoryGridDirectionUtility.DirectionToQuarterTurns(worldDirection);
        }

        return indicatorState.HasOutput;
    }

    private bool TryGetInputTilesForDefinition(
        BuildingDefinition definition,
        Vector2Int topLeftGridPosition,
        Vector2Int footprintSize,
        int rotationQuarterTurns,
        List<Vector2Int> inputTiles)
    {
        inputTiles.Clear();

        if (definition == null || definition.BehaviorPrefab == null || tileManager == null)
        {
            return false;
        }

        IBuildingInputPreview inputPreviewProvider = definition.BehaviorPrefab.GetComponent<IBuildingInputPreview>();
        if (inputPreviewProvider == null)
        {
            return false;
        }

        inputPreviewProvider.GetInputTiles(topLeftGridPosition, footprintSize, rotationQuarterTurns, inputTiles);

        for (int i = inputTiles.Count - 1; i >= 0; i--)
        {
            if (!tileManager.IsInBounds(inputTiles[i]))
            {
                inputTiles.RemoveAt(i);
            }
        }

        return inputTiles.Count > 0;
    }

    private void SetAllIndicatorsVisible(bool isVisible)
    {
        SetIndicatorVisible(directionIndicator, isVisible);
        SetIndicatorVisible(inputIndicator, isVisible);
        SetIndicatorVisible(inputIndicatorSecondary, isVisible);
        SetIndicatorVisible(outputIndicator, isVisible);
    }

    private void ApplyIndicator(
        Transform indicator,
        SpriteRenderer renderer,
        bool shouldShow,
        Vector2Int tile,
        int quarterTurns,
        Color color)
    {
        if (!shouldShow || tileManager == null)
        {
            SetIndicatorVisible(indicator, false);
            return;
        }

        Vector3 worldPosition = tileManager.GridToWorld(tile);
        worldPosition.z = tileManager.GridPlaneZ + hoverZOffset;
        ApplyIndicator(indicator, renderer, shouldShow, worldPosition, quarterTurns, color);
    }

    private void ApplyIndicator(
        Transform indicator,
        SpriteRenderer renderer,
        bool shouldShow,
        Vector3 worldPosition,
        int quarterTurns,
        Color color)
    {
        if (indicator == null)
        {
            return;
        }

        if (!shouldShow || tileManager == null)
        {
            SetIndicatorVisible(indicator, false);
            return;
        }

        worldPosition.z = tileManager.GridPlaneZ + hoverZOffset;
        indicator.position = worldPosition;
        indicator.localScale = new Vector3(tileManager.TileSize, tileManager.TileSize, 1f);
        indicator.rotation = Quaternion.Euler(0f, 0f, quarterTurns * 90f);

        if (renderer != null)
        {
            renderer.color = color;
        }

        SetIndicatorVisible(indicator, true);
    }

    private void SetIndicatorVisible(Transform indicator, bool isVisible)
    {
        if (indicator == null)
        {
            return;
        }

        if (indicator.gameObject.activeSelf != isVisible)
        {
            indicator.gameObject.SetActive(isVisible);
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawSelectedInputTileGizmo || tileManager == null || !hasPointerTile)
        {
            return;
        }

        BuildingDefinition selectedDef = GetSelectedBuildingDefinition();
        if (selectedDef == null)
        {
            return;
        }

        Vector2Int footprintSize = GetRotatedFootprintSize(selectedDef.FootprintSize);
        Vector2Int optimalTopLeft = CalculateOptimalPlacementPosition(pointerGridPosition, footprintSize);

        if (!TryGetInputTilesForDefinition(selectedDef, optimalTopLeft, footprintSize, selectedRotationQuarterTurns, reusableInputTiles)
            || reusableInputTiles.Count == 0)
        {
            return;
        }

        float radius = Mathf.Max(0.01f, inputTileGizmoRadius) * tileManager.TileSize;
        Gizmos.color = inputTileGizmoColor;

        for (int i = 0; i < reusableInputTiles.Count; i++)
        {
            Vector3 worldPos = tileManager.GridToWorld(reusableInputTiles[i]);
            worldPos.z = tileManager.GridPlaneZ;
            Gizmos.DrawSphere(worldPos, radius);
        }
    }

    private struct IndicatorState
    {
        public bool HasDirection;
        public Vector3 DirectionWorldPosition;
        public int DirectionQuarterTurns;

        public bool HasInput;
        public Vector2Int InputTile;
        public int InputQuarterTurns;

        public bool HasSecondaryInput;
        public Vector2Int SecondaryInputTile;
        public int SecondaryInputQuarterTurns;

        public bool HasOutput;
        public Vector2Int OutputTile;
        public int OutputQuarterTurns;
    }

    private Vector3 GetFootprintWorldCenter(Vector2Int topLeft, Vector2Int footprintSize)
    {
        Vector3 center = tileManager.GridToWorld(topLeft);
        center.x += (footprintSize.x - 1) * tileManager.TileSize * 0.5f;
        center.y += (footprintSize.y - 1) * tileManager.TileSize * 0.5f;
        return center;
    }

    private void DisplayFootprintHighlight(Vector2Int topLeftGridPosition, Vector2Int footprintSize, Color color)
    {
        Vector3 footprintCenter = GetFootprintWorldCenter(topLeftGridPosition, footprintSize);
        footprintCenter.z = tileManager.GridPlaneZ + hoverZOffset;

        hoverHighlight.position = footprintCenter;
        hoverHighlight.localScale = new Vector3(
            footprintSize.x * tileManager.TileSize,
            footprintSize.y * tileManager.TileSize,
            1f
        );

        if (hoverHighlightRenderer != null)
        {
            hoverHighlightRenderer.color = color;
        }

        SetHoverHighlightVisible(true);
    }

    private void ApplyDefaultHoverVisual(Color tint)
    {
        if (hoverHighlight == null || hoverHighlightRenderer == null)
        {
            return;
        }

        hoverHighlightRenderer.sprite = defaultHoverSprite;
        hoverHighlightRenderer.color = tint;
        hoverHighlight.rotation = defaultHoverRotation;
    }

    private void ApplyBuildingHoverVisual(BuildingDefinition definition, Vector2Int topLeftGridPosition, bool canPlace)
    {
        if (hoverHighlight == null || hoverHighlightRenderer == null || definition == null)
        {
            return;
        }

        Sprite selectedSprite = definition.BuildingSprite;
        int quarterTurns = selectedRotationQuarterTurns;

        if (IsConveyorDefinition(definition))
        {
            ConveyorVisualResolver.Result conveyorVisual = ConveyorVisualResolver.Resolve(
                definition,
                GetIncomingDirectionForPosition(topLeftGridPosition),
                selectedRotationQuarterTurns);

            selectedSprite = conveyorVisual.Sprite;
            quarterTurns = conveyorVisual.QuarterTurns;
        }

        hoverHighlightRenderer.sprite = selectedSprite != null ? selectedSprite : defaultHoverSprite;
        hoverHighlightRenderer.color = BuildPreviewTint(definition.BuildingColor, canPlace);
        hoverHighlight.rotation = Quaternion.Euler(0f, 0f, quarterTurns * 90f);
    }

    private Color BuildPreviewTint(Color baseColor, bool canPlace)
    {
        Color tintAnchor = canPlace ? validHoverColor : blockedHoverColor;
        Color tint = Color.Lerp(baseColor, tintAnchor, canPlace ? 0.25f : 0.6f);
        tint.a = tintAnchor.a;
        return tint;
    }

    private void SuppressHoverAtPointerTile()
    {
        if (!hasPointerTile)
        {
            return;
        }

        suppressedHoverTile = pointerGridPosition;
        suppressHoverUntilTileChange = true;
        SetHoverHighlightVisible(false);
        SetAllIndicatorsVisible(false);
    }

    private void SetHoverHighlightVisible(bool isVisible)
    {
        if (hoverHighlight == null)
        {
            return;
        }

        if (hoverHighlight.gameObject.activeSelf != isVisible)
        {
            hoverHighlight.gameObject.SetActive(isVisible);
        }
    }

    private bool TryPlaceAtPointer()
    {
        if (!CanInteractAtPointer())
        {
            return false;
        }

        BuildingDefinition selectedBuildingDefinition = GetSelectedBuildingDefinition();
        if (selectedBuildingDefinition == null)
        {
            return false;
        }

        GameObject selectedBuildingPrefab = selectedBuildingDefinition.BehaviorPrefab;
        if (selectedBuildingPrefab == null)
        {
            return false;
        }

        if (inventoryManager == null || !inventoryManager.RemoveBuilding(selectedBuildingDefinition, 1))
        {
            return false;
        }

        Vector2Int footprintSize = GetRotatedFootprintSize(selectedBuildingDefinition.FootprintSize);
        Vector2Int optimalTopLeft = CalculateOptimalPlacementPosition(pointerGridPosition, footprintSize);

        if (!CanPlaceWithItemExceptions(selectedBuildingDefinition, optimalTopLeft, footprintSize, selectedRotationQuarterTurns))
        {
            inventoryManager.AddBuilding(selectedBuildingDefinition, 1);
            return false;
        }

        if (!tileManager.CanOccupyFootprint(optimalTopLeft, footprintSize))
        {
            if (!CanReplaceConveyorAt(optimalTopLeft, footprintSize, selectedBuildingDefinition)
                || !spawnedByCell.TryGetValue(optimalTopLeft, out PlacedBuildingRecord existingConveyor)
                || !RemovePlacedBuilding(existingConveyor, true))
            {
                inventoryManager.AddBuilding(selectedBuildingDefinition, 1);
                return false;
            }
        }

        string occupantId = selectedBuildingPrefab.GetInstanceID().ToString();
        if (!tileManager.TryOccupyFootprint(optimalTopLeft, footprintSize, occupantId))
        {
            inventoryManager.AddBuilding(selectedBuildingDefinition, 1);
            return false;
        }

        Vector3 spawnPosition = GetFootprintWorldCenter(optimalTopLeft, footprintSize);
        
        float rotationDegrees = selectedRotationQuarterTurns * 90f;
        Quaternion spawnRotation = Quaternion.Euler(0f, 0f, rotationDegrees);
        GameObject spawned = Instantiate(selectedBuildingPrefab, spawnPosition, spawnRotation);

        BuildingInstance buildingInstance = spawned.GetComponent<BuildingInstance>();
        if (buildingInstance != null)
        {
            buildingInstance.SetGridPosition(optimalTopLeft, footprintSize, selectedRotationQuarterTurns);
            buildingInstance.Initialize(selectedBuildingDefinition);
        }

        ApplyStoredMachineResourceIfAvailable(spawned, selectedBuildingDefinition);

        TryFeedItemsIntoPlacedInputBuilding(spawned, selectedBuildingDefinition, optimalTopLeft, footprintSize, selectedRotationQuarterTurns);

        PlacedBuildingRecord record = new PlacedBuildingRecord(spawned, selectedBuildingDefinition, optimalTopLeft, footprintSize, selectedRotationQuarterTurns);
        buildingsByInstanceId[spawned.GetInstanceID()] = record;

        for (int x = 0; x < footprintSize.x; x++)
        {
            for (int y = 0; y < footprintSize.y; y++)
            {
                Vector2Int tilePos = optimalTopLeft + new Vector2Int(x, y);
                spawnedByCell[tilePos] = record;
            }
        }

        RefreshConveyorVisualsAround(optimalTopLeft, footprintSize);

        if (inventoryManager != null && !inventoryManager.HasBuilding(selectedBuildingDefinition, 1))
        {
        }

        return true;
    }

    private void TryRemoveAtPointer()
    {
        if (!CanInteractAtPointer())
        {
            return;
        }

        Vector2Int gridPosition = pointerGridPosition;
        if (!spawnedByCell.TryGetValue(gridPosition, out PlacedBuildingRecord record) || record?.SpawnedObject == null)
        {
            if (TryRemoveLooseItemUnderPointer())
            {
                return;
            }

            DeselectBuilding();
            return;
        }

        Vector2Int topLeft = record.TopLeftGridPosition;
        Vector2Int footprintSize = record.FootprintSize;

        if (!RemovePlacedBuilding(record, refundBuildingToInventoryOnRemove))
        {
            return;
        }

        RefreshConveyorVisualsAround(topLeft, footprintSize);
        suppressHoverUntilTileChange = false;
    }

    private bool TryRemoveLooseItemUnderPointer()
    {
        if (tileManager == null || !hasPointerTile)
        {
            return false;
        }

        Vector2Int tile = pointerGridPosition;
        if (spawnedByCell.ContainsKey(tile))
        {
            // Items on machine tiles should not be deleted by ground cleanup.
            return false;
        }

        ItemEntity[] items = ItemEntitySceneQuery.GetItems();
        for (int i = 0; i < items.Length; i++)
        {
            ItemEntity item = items[i];
            if (item == null)
            {
                continue;
            }

            if (tileManager.WorldToGrid(item.transform.position) != tile)
            {
                continue;
            }

            if (!item.ContainsWorldPoint(pointerWorldPoint))
            {
                continue;
            }

            item.TryRefundToSourceGenerator(1);
            Destroy(item.gameObject);
            return true;
        }

        return false;
    }

    private bool RemovePlacedBuilding(PlacedBuildingRecord record, bool refundToInventory)
    {
        if (!tileManager.ClearFootprint(record.TopLeftGridPosition, record.FootprintSize))
        {
            return false;
        }

        int instanceId = record.SpawnedObject.GetInstanceID();
        int progressContextId = GetMachineProgressContextId(record);

        if (SelectedMachineInstanceId == instanceId || SelectedMachineInstanceId == progressContextId)
        {
            SelectedMachineInstanceId = -1;
        }

        if (HoveredMachineInstanceId == instanceId || HoveredMachineInstanceId == progressContextId)
        {
            HoveredMachineInstanceId = -1;
        }

        // Store machine state BEFORE destroying so components are still accessible
        if (refundToInventory && inventoryManager != null && record.Definition != null)
        {
            StoreMachineResourceForInventory(record.SpawnedObject, record.Definition);
        }

        Destroy(record.SpawnedObject);
        buildingsByInstanceId.Remove(instanceId);

        for (int x = 0; x < record.FootprintSize.x; x++)
        {
            for (int y = 0; y < record.FootprintSize.y; y++)
            {
                spawnedByCell.Remove(record.TopLeftGridPosition + new Vector2Int(x, y));
            }
        }

        if (refundToInventory && inventoryManager != null && record.Definition != null)
        {
            inventoryManager.AddBuilding(record.Definition, 1);
        }

        return true;
    }

    private void RefreshConveyorVisualsAround(Vector2Int topLeftGridPosition, Vector2Int footprintSize)
    {
        HashSet<PlacedBuildingRecord> uniqueConveyorRecords = new HashSet<PlacedBuildingRecord>();

        for (int x = 0; x < footprintSize.x; x++)
        {
            for (int y = 0; y < footprintSize.y; y++)
            {
                Vector2Int tilePos = topLeftGridPosition + new Vector2Int(x, y);
                TryCollectConveyorRecordAt(tilePos, uniqueConveyorRecords);

                for (int i = 0; i < CardinalDirections.Length; i++)
                {
                    TryCollectConveyorRecordAt(tilePos + CardinalDirections[i], uniqueConveyorRecords);
                }
            }
        }

        foreach (PlacedBuildingRecord conveyorRecord in uniqueConveyorRecords)
        {
            ApplyConveyorVisualForRecord(conveyorRecord);
        }
    }

    private void StoreMachineResourceForInventory(GameObject spawnedObject, BuildingDefinition definition)
    {
        if (inventoryManager == null || spawnedObject == null || definition == null)
        {
            return;
        }

        IMachineResourceProgressProvider provider = spawnedObject.GetComponentInChildren<IMachineResourceProgressProvider>();
        IMachineStoredResourceReceiver receiver = spawnedObject.GetComponentInChildren<IMachineStoredResourceReceiver>();
        if (provider == null || receiver == null)
        {
            return;
        }

        EnsureMachineHasStateId(receiver);
        inventoryManager.PushStoredMachineResource(definition, receiver.MachineStateId, provider.CurrentResourceAmount);
    }

    private void ApplyStoredMachineResourceIfAvailable(GameObject spawnedObject, BuildingDefinition definition)
    {
        if (inventoryManager == null || spawnedObject == null || definition == null)
        {
            return;
        }

        IMachineStoredResourceReceiver receiver = spawnedObject.GetComponentInChildren<IMachineStoredResourceReceiver>();
        if (receiver == null)
        {
            return;
        }

        if (!inventoryManager.TryPopStoredMachineResource(definition, out string storedMachineStateId, out int storedAmount))
        {
            EnsureMachineHasStateId(receiver);
            return;
        }

        receiver.SetMachineStateId(storedMachineStateId);
        receiver.SetStoredResourceAmount(storedAmount);
    }

    private static void EnsureMachineHasStateId(IMachineStoredResourceReceiver receiver)
    {
        if (receiver == null || !string.IsNullOrEmpty(receiver.MachineStateId))
        {
            return;
        }

        receiver.SetMachineStateId(Guid.NewGuid().ToString("N"));
    }

    private void TryCollectConveyorRecordAt(Vector2Int gridPosition, HashSet<PlacedBuildingRecord> records)
    {
        if (!spawnedByCell.TryGetValue(gridPosition, out PlacedBuildingRecord record) || record == null)
        {
            return;
        }

        if (!IsConveyorDefinition(record.Definition))
        {
            return;
        }

        if (record.SpawnedObject == null)
        {
            return;
        }

        records.Add(record);
    }

    private bool IsConveyorDefinition(BuildingDefinition definition)
    {
        return definition != null && definition.IsConveyor;
    }

    /// <summary>
    /// Checks if a tile position is occupied by a non-conveyor building.
    /// Used by generators to determine if they can output to this position.
    /// </summary>
    public bool IsPositionBlockedByNonConveyorBuilding(Vector2Int gridPosition)
    {
        if (!spawnedByCell.TryGetValue(gridPosition, out PlacedBuildingRecord record) || record == null)
        {
            return false;
        }

        return !IsConveyorDefinition(record.Definition);
    }

    private bool CanReplaceConveyorAt(Vector2Int optimalTopLeft, Vector2Int footprintSize, BuildingDefinition incomingDefinition)
    {
        if (!IsConveyorDefinition(incomingDefinition))
        {
            return false;
        }

        if (!spawnedByCell.TryGetValue(optimalTopLeft, out PlacedBuildingRecord existing) || existing == null)
        {
            return false;
        }

        return IsConveyorDefinition(existing.Definition)
            && existing.FootprintSize == footprintSize
            && existing.TopLeftGridPosition == optimalTopLeft;
    }

    private void ApplyConveyorVisualForRecord(PlacedBuildingRecord conveyorRecord)
    {
        if (conveyorRecord == null || conveyorRecord.Definition == null || conveyorRecord.SpawnedObject == null)
        {
            return;
        }

        ConveyorVisualResolver.Result conveyorVisual = ConveyorVisualResolver.Resolve(
            conveyorRecord.Definition,
            GetIncomingDirectionForRecord(conveyorRecord),
            conveyorRecord.PlacedRotationQuarterTurns);

        int visualQuarterTurns = conveyorVisual.QuarterTurns;
        int logicalQuarterTurns = conveyorRecord.PlacedRotationQuarterTurns;
        Sprite selectedSprite = conveyorVisual.Sprite;

        conveyorRecord.SpawnedObject.transform.rotation = Quaternion.Euler(0f, 0f, visualQuarterTurns * 90f);

        BuildingInstance buildingInstance = conveyorRecord.SpawnedObject.GetComponent<BuildingInstance>();
        if (buildingInstance != null)
        {
            buildingInstance.SetGridPosition(
                conveyorRecord.TopLeftGridPosition,
                conveyorRecord.FootprintSize,
                logicalQuarterTurns);
        }

        SpriteRenderer spriteRenderer = conveyorRecord.SpawnedObject.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null && selectedSprite != null)
        {
            spriteRenderer.sprite = selectedSprite;
            spriteRenderer.color = conveyorRecord.Definition.BuildingColor;
        }
    }

    private Vector2Int? GetIncomingDirectionForRecord(PlacedBuildingRecord targetRecord)
    {
        if (targetRecord == null)
        {
            return null;
        }

        return GetIncomingDirectionForPosition(targetRecord.TopLeftGridPosition);
    }

    private Vector2Int? GetIncomingDirectionForPosition(Vector2Int targetPosition)
    {
        for (int i = 0; i < CardinalDirections.Length; i++)
        {
            Vector2Int neighborPosition = targetPosition + CardinalDirections[i];
            if (!spawnedByCell.TryGetValue(neighborPosition, out PlacedBuildingRecord neighborRecord)
                || neighborRecord == null
                || neighborRecord.SpawnedObject == null
                || !IsConveyorDefinition(neighborRecord.Definition))
            {
                continue;
            }

            int neighborQuarterTurns = neighborRecord.PlacedRotationQuarterTurns;
            Vector2Int neighborDirection = ConveyorVisualResolver.DirectionFromQuarterTurns(neighborQuarterTurns);
            if (neighborPosition + neighborDirection == targetPosition)
            {
                return neighborDirection;
            }
        }

        return null;
    }

    private bool CanInteractAtPointer()
    {
        return hasPointerTile && tileManager != null && inventoryManager != null;
    }

    private void RefreshPointerState(Mouse mouse)
    {
        hasPointerTile = TryGetPointerHitPoint(mouse, out pointerWorldPoint)
            && tileManager != null;

        if (!hasPointerTile)
        {
            return;
        }

        pointerGridPosition = tileManager.WorldToGrid(pointerWorldPoint);
        hasPointerTile = tileManager.IsInBounds(pointerGridPosition);
    }

    private void UpdateMachineProgressVisibilityContext()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.leftAltKey.wasPressedThisFrame || keyboard.rightAltKey.wasPressedThisFrame))
        {
            SetShowInfo(!AreMachineProgressBarsPinnedVisible);
        }

        if (!TryGetPointerGridPositionForMachineHover(out Vector2Int hoverGridPosition)
            || !spawnedByCell.TryGetValue(hoverGridPosition, out PlacedBuildingRecord hoveredRecord)
            || hoveredRecord?.SpawnedObject == null)
        {
            HoveredMachineInstanceId = -1;
            return;
        }

        HoveredMachineInstanceId = GetMachineProgressContextId(hoveredRecord);
    }

    private bool TryGetPointerGridPositionForMachineHover(out Vector2Int gridPosition)
    {
        gridPosition = default;

        if (Mouse.current == null || tileManager == null || worldCamera == null)
        {
            return false;
        }

        float distanceToGridPlane = Mathf.Abs(tileManager.GridPlaneZ - worldCamera.transform.position.z);
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector3 mouseScreenPoint = new Vector3(mousePosition.x, mousePosition.y, distanceToGridPlane);
        Vector3 mouseWorldPoint = worldCamera.ScreenToWorldPoint(mouseScreenPoint);
        mouseWorldPoint.z = tileManager.GridPlaneZ;

        gridPosition = tileManager.WorldToGrid(mouseWorldPoint);
        return tileManager.IsInBounds(gridPosition);
    }

    private bool TrySelectMachineAtPointer()
    {
        if (!hasPointerTile
            || !spawnedByCell.TryGetValue(pointerGridPosition, out PlacedBuildingRecord hoveredRecord)
            || hoveredRecord?.SpawnedObject == null)
        {
            SelectedMachineInstanceId = -1;
            return false;
        }

        SelectedMachineInstanceId = GetMachineProgressContextId(hoveredRecord);
        return true;
    }

    private static int GetMachineProgressContextId(PlacedBuildingRecord record)
    {
        if (record?.SpawnedObject == null)
        {
            return -1;
        }

        BuildingInstance buildingInstance = record.SpawnedObject.GetComponentInChildren<BuildingInstance>();
        if (buildingInstance != null)
        {
            return buildingInstance.gameObject.GetInstanceID();
        }

        return record.SpawnedObject.GetInstanceID();
    }

    private bool TryGetPointerHitPoint(Mouse mouse, out Vector3 hitPoint)
    {
        hitPoint = default;

        if (tileManager == null || worldCamera == null)
        {
            return false;
        }

        float distanceToGridPlane = Mathf.Abs(tileManager.GridPlaneZ - worldCamera.transform.position.z);
        Vector2 mousePosition = mouse.position.ReadValue();
        Vector3 mouseScreenPoint = new Vector3(mousePosition.x, mousePosition.y, 0f);
        mouseScreenPoint.z = distanceToGridPlane;

        Vector3 mouseWorldPoint = worldCamera.ScreenToWorldPoint(mouseScreenPoint);
        mouseWorldPoint.z = tileManager.GridPlaneZ;

        Collider2D hitCollider = Physics2D.OverlapPoint(mouseWorldPoint, placementSurfaceMask);
        if (hitCollider == null)
        {
            return false;
        }

        hitPoint = mouseWorldPoint;
        return true;
    }

    private bool IsPointerOverBlockingUi()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return false;
        }

        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return eventSystem.IsPointerOverGameObject();
        }

        PointerEventData pointerEventData = new PointerEventData(eventSystem)
        {
            position = mouse.position.ReadValue()
        };

        reusableUiRaycastResults.Clear();
        eventSystem.RaycastAll(pointerEventData, reusableUiRaycastResults);

        for (int i = 0; i < reusableUiRaycastResults.Count; i++)
        {
            GameObject hitObject = reusableUiRaycastResults[i].gameObject;
            if (hitObject == null)
            {
                continue;
            }

            if (IsLayerInMask(hitObject.layer, blockingUiLayers))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsLayerInMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    private void HandleBuildingSelectionInput()
    {
        if (!enableNumberKeySelection)
        {
            return;
        }

        ClampSelectedBuildingIndex();

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || inventoryManager == null)
        {
            return;
        }

        for (int i = 0; i < 9; i++)
        {
            if (keyboard[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame)
            {
                TrySelectBuildingByIndex(i);
                break;
            }
        }

        if (keyboard.digit0Key.wasPressedThisFrame)
        {
            TrySelectBuildingByIndex(InventoryManager.BuildingHotbarSlotCount - 1);
        }
    }

    private void HandleRotationInput()
    {
        if (!enableRotationInput)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.rKey.wasPressedThisFrame)
        {
            bool isShiftHeld = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            selectedRotationQuarterTurns = isShiftHeld
                ? (selectedRotationQuarterTurns + 1) % 4
                : (selectedRotationQuarterTurns + 3) % 4;
            suppressHoverUntilTileChange = false;
        }
    }

    private void HandleFactorySpeedInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (!enableShiftSpeedBoost || keyboard == null)
        {
            if (isShiftSpeedOverrideActive)
            {
                isShiftSpeedOverrideActive = false;
                SyncSpeedToggleVisualWithSelectedSpeed();
            }

            ApplyFactorySpeed(selectedFactorySpeed);
            return;
        }

        bool shiftHeld = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        float boostedSpeed = Mathf.Max(0.1f, boostedFactorySpeed);
        bool isAlreadyOnBoostedSpeed = Mathf.Approximately(selectedFactorySpeed, boostedSpeed);

        // If base speed is already 2x, Shift should not change state or toggles.
        if (isAlreadyOnBoostedSpeed)
        {
            if (isShiftSpeedOverrideActive)
            {
                isShiftSpeedOverrideActive = false;
                SyncSpeedToggleVisualWithSelectedSpeed();
            }

            ApplyFactorySpeed(selectedFactorySpeed);
            return;
        }

        if (shiftHeld)
        {
            if (!isShiftSpeedOverrideActive)
            {
                isShiftSpeedOverrideActive = true;
                SetSpeedToggleVisual(isDoubleSelected: true);
            }

            ApplyFactorySpeed(boostedSpeed);
            return;
        }

        if (isShiftSpeedOverrideActive)
        {
            isShiftSpeedOverrideActive = false;
            SyncSpeedToggleVisualWithSelectedSpeed();
        }

        ApplyFactorySpeed(selectedFactorySpeed);
    }

    public void SetFactorySpeedTo1x()
    {
        SetFactorySpeedSelection(isDoubleSpeed: false, persistSetting: true);
    }

    public void SetFactorySpeedTo1x(bool isOn)
    {
        if (isOn)
        {
            SetFactorySpeedTo1x();
        }
    }

    public void SetFactorySpeedTo2x()
    {
        SetFactorySpeedSelection(isDoubleSpeed: true, persistSetting: true);
    }

    public void SetFactorySpeedTo2x(bool isOn)
    {
        if (isOn)
        {
            SetFactorySpeedTo2x();
        }
    }

    private void ApplyFactorySpeed(float speed)
    {
        float clampedSpeed = Mathf.Max(0.1f, speed);

        float targetFixedDeltaTime = defaultFixedDeltaTime * clampedSpeed;
        if (Mathf.Approximately(Time.timeScale, clampedSpeed)
            && Mathf.Approximately(Time.fixedDeltaTime, targetFixedDeltaTime))
        {
            return;
        }

        Time.timeScale = clampedSpeed;
        Time.fixedDeltaTime = targetFixedDeltaTime;
    }

    private void ApplySavedSettings()
    {
        GameSettings settings = GameSettings.Instance;
        SetShowInfo(settings.ShowInfo, false);
        SetShowControls(settings.ShowControls, false);
        SetFactorySpeedSelection(settings.FactorySpeedIsDouble, false);
    }

    public void SetShowInfo(bool isVisible)
    {
        SetShowInfo(isVisible, true);
    }

    private void SetShowInfo(bool isVisible, bool persistSetting)
    {
        AreMachineProgressBarsPinnedVisible = isVisible;

        if (persistSetting)
        {
            GameSettings.Instance.SetShowInfo(isVisible);
        }
    }

    public void SetShowControls(bool isVisible)
    {
        SetShowControls(isVisible, true);
    }

    private void SetShowControls(bool isVisible, bool persistSetting)
    {
        if (settingsPanelController != null)
        {
            settingsPanelController.SetExpanded(isVisible, false);
        }
        else if (controlsPanelRoot != null && controlsPanelRoot.activeSelf != isVisible)
        {
            controlsPanelRoot.SetActive(isVisible);
        }

        if (persistSetting)
        {
            GameSettings.Instance.SetShowControls(isVisible);
        }
    }

    private void SetFactorySpeedSelection(bool isDoubleSpeed, bool persistSetting)
    {
        selectedFactorySpeed = isDoubleSpeed
            ? Mathf.Max(0.1f, boostedFactorySpeed)
            : Mathf.Max(0.1f, normalFactorySpeed);

        isShiftSpeedOverrideActive = false;
        SetSpeedToggleVisual(isDoubleSpeed);
        ApplyFactorySpeed(selectedFactorySpeed);

        if (persistSetting)
        {
            GameSettings.Instance.SetFactorySpeedIsDouble(isDoubleSpeed);
        }
    }

    private void RevertGlobalTimeToNormal()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDeltaTime;
    }

    private void SyncSpeedToggleVisualWithSelectedSpeed()
    {
        bool isDoubleSelected = Mathf.Approximately(selectedFactorySpeed, Mathf.Max(0.1f, boostedFactorySpeed));
        SetSpeedToggleVisual(isDoubleSelected);
    }

    private void SetSpeedToggleVisual(bool isDoubleSelected)
    {
        if (normalSpeedToggle != null)
        {
            normalSpeedToggle.SetIsOnWithoutNotify(!isDoubleSelected);
        }

        if (doubleSpeedToggle != null)
        {
            doubleSpeedToggle.SetIsOnWithoutNotify(isDoubleSelected);
        }
    }

    private void MaintainSpeedToggleSelectionVisual()
    {
        Toggle activeToggle = GetActiveSpeedToggleForVisual();
        if (activeToggle == null)
        {
            return;
        }

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return;
        }

        if (eventSystem.currentSelectedGameObject != activeToggle.gameObject)
        {
            eventSystem.SetSelectedGameObject(activeToggle.gameObject);
        }
    }

    private Toggle GetActiveSpeedToggleForVisual()
    {
        bool isDoubleSelected = isShiftSpeedOverrideActive
            || Mathf.Approximately(selectedFactorySpeed, Mathf.Max(0.1f, boostedFactorySpeed));

        if (isDoubleSelected)
        {
            return doubleSpeedToggle != null ? doubleSpeedToggle : normalSpeedToggle;
        }

        return normalSpeedToggle != null ? normalSpeedToggle : doubleSpeedToggle;
    }

    public bool TrySelectBuildingByIndex(int index)
    {
        if (inventoryManager == null)
        {
            return false;
        }

        if (index < 0 || index >= InventoryManager.BuildingHotbarSlotCount)
        {
            return false;
        }

        selectedBuildingIndex = index;
        hasSelectedBuilding = true;
        suppressHoverUntilTileChange = false;
        return true;
    }

    private BuildingDefinition GetSelectedBuildingDefinition()
    {
        if (!hasSelectedBuilding || inventoryManager == null)
        {
            return null;
        }

        ClampSelectedBuildingIndex();
        if (!inventoryManager.TryGetBuildingAtHotbarSlot(selectedBuildingIndex, out BuildingDefinition selectedDefinition, out int selectedQuantity)
            || selectedDefinition == null
            || selectedQuantity <= 0)
        {
            return null;
        }

        return selectedDefinition;
    }

    private bool CanPlaceSelectedBuildingAt(Vector2Int gridPosition)
    {
        if (tileManager == null || inventoryManager == null || !tileManager.IsInBounds(gridPosition))
        {
            return false;
        }

        BuildingDefinition selectedBuildingDefinition = GetSelectedBuildingDefinition();
        if (selectedBuildingDefinition == null || selectedBuildingDefinition.BehaviorPrefab == null)
        {
            return false;
        }

        Vector2Int footprintSize = GetRotatedFootprintSize(selectedBuildingDefinition.FootprintSize);
        Vector2Int optimalTopLeft = CalculateOptimalPlacementPosition(gridPosition, footprintSize);

        return (tileManager.CanOccupyFootprint(optimalTopLeft, footprintSize)
            || CanReplaceConveyorAt(optimalTopLeft, footprintSize, selectedBuildingDefinition))
            && CanPlaceWithItemExceptions(selectedBuildingDefinition, optimalTopLeft, footprintSize, selectedRotationQuarterTurns)
            && inventoryManager.HasBuilding(selectedBuildingDefinition, 1);
    }

    private bool CanPlaceWithItemExceptions(
        BuildingDefinition definition,
        Vector2Int topLeftGridPosition,
        Vector2Int footprintSize,
        int rotationQuarterTurns)
    {
        if (tileManager == null)
        {
            return true;
        }

        bool hasBlockingTiles = false;

        if (definition != null && definition.IsConveyor)
        {
            return true;
        }

        bool hasInputPreview = TryGetInputTilesForDefinition(definition, topLeftGridPosition, footprintSize, rotationQuarterTurns, reusableInputTiles);

        for (int x = 0; x < footprintSize.x; x++)
        {
            for (int y = 0; y < footprintSize.y; y++)
            {
                Vector2Int tilePos = topLeftGridPosition + new Vector2Int(x, y);

                bool hasActualItem = ItemEntitySceneQuery.TryGetFirstItemAtTile(tileManager, tilePos, out _);
                bool hasReservedOnly = ItemEntitySceneQuery.HasReservedAtTile(tileManager, tilePos) && !hasActualItem;

                if (!hasActualItem && !hasReservedOnly)
                {
                    continue;
                }

                hasBlockingTiles = true;

                if (!hasInputPreview)
                {
                    return false;
                }

                if (hasReservedOnly)
                {
                    return false;
                }

                if (!reusableInputTiles.Contains(tilePos))
                {
                    return false;
                }
            }
        }

        return !hasBlockingTiles || hasInputPreview;
    }

    private void TryFeedItemsIntoPlacedInputBuilding(
        GameObject spawnedBuilding,
        BuildingDefinition definition,
        Vector2Int topLeftGridPosition,
        Vector2Int footprintSize,
        int rotationQuarterTurns)
    {
        if (spawnedBuilding == null)
        {
            return;
        }

        IItemInputReceiver inputReceiver = spawnedBuilding.GetComponent<IItemInputReceiver>();
        if (inputReceiver == null)
        {
            return;
        }

        if (!TryGetInputTilesForDefinition(definition, topLeftGridPosition, footprintSize, rotationQuarterTurns, reusableInputTiles)
            || reusableInputTiles.Count == 0)
        {
            return;
        }

        for (int i = 0; i < reusableInputTiles.Count; i++)
        {
            CollectItemsAtTile(reusableInputTiles[i], reusableItemsOnInput);

            for (int itemIndex = 0; itemIndex < reusableItemsOnInput.Count; itemIndex++)
            {
                ItemEntity item = reusableItemsOnInput[itemIndex];
                if (item == null)
                {
                    continue;
                }

                inputReceiver.TryAcceptItem(item, reusableInputTiles[i]);
            }
        }
    }

    private void CollectItemsAtTile(Vector2Int tile, List<ItemEntity> result)
    {
        result.Clear();

        if (tileManager == null)
        {
            return;
        }

        ItemEntity[] items = ItemEntitySceneQuery.GetItems();
        for (int i = 0; i < items.Length; i++)
        {
            ItemEntity item = items[i];
            if (item == null)
            {
                continue;
            }

            if (tileManager.WorldToGrid(item.transform.position) == tile)
            {
                result.Add(item);
            }
        }
    }

    private static Vector2Int GetFootprintWorldCenterAsGrid(Vector2Int topLeftGridPosition, Vector2Int footprintSize)
    {
        return topLeftGridPosition + new Vector2Int((footprintSize.x - 1) / 2, (footprintSize.y - 1) / 2);
    }

    private static Vector2Int GetIndicatorTileOutsideBuilding(
        Vector2Int inputTile,
        Vector2Int topLeftGridPosition,
        Vector2Int footprintSize,
        int rotationQuarterTurns)
    {
        Vector2Int bottomRightGridPosition = topLeftGridPosition + new Vector2Int(footprintSize.x - 1, footprintSize.y - 1);
        Vector2Int indicatorTile = inputTile;

        bool isOnLeft = inputTile.x == topLeftGridPosition.x;
        bool isOnRight = inputTile.x == bottomRightGridPosition.x;
        bool isOnBottom = inputTile.y == topLeftGridPosition.y;
        bool isOnTop = inputTile.y == bottomRightGridPosition.y;

        // Corner inputs touch two edges. Resolve which edge to extend from using rotation
        // so the indicator tracks predictably across all quarter turns.
        bool isCorner = (isOnLeft || isOnRight) && (isOnBottom || isOnTop);
        if (isCorner)
        {
            int normalizedQuarterTurns = Mathf.Abs(rotationQuarterTurns) % 4;
            bool preferHorizontal = (normalizedQuarterTurns & 1) == 0;

            if (preferHorizontal)
            {
                indicatorTile.x += isOnLeft ? -1 : 1;
            }
            else
            {
                indicatorTile.y += isOnBottom ? -1 : 1;
            }

            return indicatorTile;
        }

        // Determine which single edge the input is on and extend outward in that direction only
        if (isOnLeft)
        {
            // Left edge - extend further left
            indicatorTile.x--;
        }
        else if (isOnRight)
        {
            // Right edge - extend further right
            indicatorTile.x++;
        }
        else if (isOnBottom)
        {
            // Bottom edge - extend further down
            indicatorTile.y--;
        }
        else if (isOnTop)
        {
            // Top edge - extend further up
            indicatorTile.y++;
        }

        return indicatorTile;
    }

    private static Vector2Int NormalizeCardinal(Vector2Int direction)
    {
        if (direction == Vector2Int.zero)
        {
            return Vector2Int.right;
        }

        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
        {
            return direction.x >= 0 ? Vector2Int.right : Vector2Int.left;
        }

        return direction.y >= 0 ? Vector2Int.up : Vector2Int.down;
    }

    private Vector2Int GetRotatedFootprintSize(Vector2Int baseFootprint)
    {
        if ((selectedRotationQuarterTurns & 1) == 0)
        {
            return baseFootprint;
        }

        return new Vector2Int(baseFootprint.y, baseFootprint.x);
    }

    private Vector2Int CalculateOptimalPlacementPosition(Vector2Int cursorGridPosition, Vector2Int footprintSize)
    {
        if (tileManager == null)
        {
            return cursorGridPosition;
        }

        // Calculate the offset from top-left to center of the building
        float centerOffsetX = (footprintSize.x - 1) * 0.5f;
        float centerOffsetY = (footprintSize.y - 1) * 0.5f;

        // For even dimensions, bias towards placing more tiles to the right (X) and above (Y)
        // For odd dimensions, round to nearest for true centering
        int topLeftX;
        int topLeftY;

        if (footprintSize.x % 2 == 0)
        {
            // Even width: bias right by using ceil
            topLeftX = Mathf.CeilToInt(cursorGridPosition.x - centerOffsetX);
        }
        else
        {
            // Odd width: round to nearest for true centering
            topLeftX = Mathf.RoundToInt(cursorGridPosition.x - centerOffsetX);
        }

        if (footprintSize.y % 2 == 0)
        {
            // Even height: bias up by using ceil
            topLeftY = Mathf.CeilToInt(cursorGridPosition.y - centerOffsetY);
        }
        else
        {
            // Odd height: round to nearest for true centering
            topLeftY = Mathf.RoundToInt(cursorGridPosition.y - centerOffsetY);
        }

        Vector2Int topLeft = new Vector2Int(topLeftX, topLeftY);

        // Clamp to grid bounds
        topLeft.x = Mathf.Clamp(topLeft.x, 0, tileManager.GridWidth - footprintSize.x);
        topLeft.y = Mathf.Clamp(topLeft.y, 0, tileManager.GridHeight - footprintSize.y);

        return topLeft;
    }

    private void EnsureInventoryManagerAssigned()
    {
        if (inventoryManager == null)
        {
            inventoryManager = FindFirstObjectByType<InventoryManager>();
        }

        if (inventoryManager == null)
        {
            inventoryManager = InventoryManager.Instance;
        }
    }

    private void ClampSelectedBuildingIndex()
    {
        if (inventoryManager == null)
        {
            selectedBuildingIndex = 0;
            hasSelectedBuilding = false;
            return;
        }

        if (!HasAnyHotbarBuildingDefinitions())
        {
            selectedBuildingIndex = 0;
            hasSelectedBuilding = false;
            return;
        }

        selectedBuildingIndex = Mathf.Clamp(selectedBuildingIndex, 0, InventoryManager.BuildingHotbarSlotCount - 1);
    }

    private void RefreshSelectionAvailability()
    {
        if (inventoryManager == null)
        {
            return;
        }

        if (!HasAnyHotbarBuildingDefinitions())
        {
            DeselectBuilding();
            return;
        }

    }

    private bool HasAnyHotbarBuildingDefinitions()
    {
        if (inventoryManager == null)
        {
            return false;
        }

        for (int i = 0; i < InventoryManager.BuildingHotbarSlotCount; i++)
        {
            if (inventoryManager.TryGetBuildingAtHotbarSlot(i, out BuildingDefinition definition, out _) && definition != null)
            {
                return true;
            }
        }

        return false;
    }

    private bool TrySelectFirstAvailableHotbarSlot()
    {
        for (int i = 0; i < InventoryManager.BuildingHotbarSlotCount; i++)
        {
            if (TrySelectBuildingByIndex(i))
            {
                return true;
            }
        }

        return false;
    }

    private void DeselectBuilding()
    {
        hasSelectedBuilding = false;
        SelectedMachineInstanceId = -1;
        suppressHoverUntilTileChange = false;
        SetHoverHighlightVisible(false);
        SetAllIndicatorsVisible(false);
    }

    public void ClearFactoryAndRefundAll()
    {
        EnsureInventoryManagerAssigned();
        if (inventoryManager != null)
        {
            // Factory reset should remove crafted balls because no molds remain on the field.
            inventoryManager.SetCraftedBalls(null);
        }

        ClearAllPlacedBuildings(refundToInventory: true, clearLooseItems: true, resetMachineResourcesToFull: true);
    }

    public void ClearAllPlacedBuildings(
        bool refundToInventory = false,
        bool clearLooseItems = true,
        bool resetMachineResourcesToFull = false)
    {
        EnsureInventoryManagerAssigned();

        if (resetMachineResourcesToFull && inventoryManager != null)
        {
            inventoryManager.ClearStoredMachineResources();
        }

        var uniqueRecords = new HashSet<PlacedBuildingRecord>(buildingsByInstanceId.Values);
        foreach (PlacedBuildingRecord record in uniqueRecords)
        {
            if (record == null || record.SpawnedObject == null)
            {
                continue;
            }

            // Store machine resource state BEFORE destroying so components are accessible
            if (!resetMachineResourcesToFull && refundToInventory && inventoryManager != null && record.Definition != null)
            {
                StoreMachineResourceForInventory(record.SpawnedObject, record.Definition);
            }

            Destroy(record.SpawnedObject);
        }

        if (clearLooseItems)
        {
            ItemEntity[] looseItems = ItemEntitySceneQuery.GetItems();
            for (int i = 0; i < looseItems.Length; i++)
            {
                if (looseItems[i] != null)
                {
                    if (!resetMachineResourcesToFull)
                    {
                        looseItems[i].TryRefundToSourceGenerator(looseItems[i].Quantity);
                    }

                    Destroy(looseItems[i].gameObject);
                }
            }
        }

        // Refund all buildings to inventory
        if (refundToInventory && inventoryManager != null)
        {
            foreach (PlacedBuildingRecord record in uniqueRecords)
            {
                if (record != null && record.Definition != null)
                {
                    inventoryManager.AddBuilding(record.Definition, 1);
                }
            }
        }

        if (tileManager != null)
        {
            tileManager.InitializeGrid();
        }

        spawnedByCell.Clear();
        buildingsByInstanceId.Clear();
        HoveredMachineInstanceId = -1;
        SelectedMachineInstanceId = -1;
        suppressHoverUntilTileChange = false;
        SetHoverHighlightVisible(false);
        SetAllIndicatorsVisible(false);
    }

    // ── Persistence ──────────────────────────────────────────────────────────

    [System.Serializable]
    public struct FactoryBuildingEntry
    {
        public string definitionName;
        public int topLeftX;
        public int topLeftY;
        public int rotationQuarterTurns;
    }

    public List<FactoryBuildingEntry> GetSaveData()
    {
        var entries = new List<FactoryBuildingEntry>();
        var seen = new HashSet<PlacedBuildingRecord>();

        foreach (PlacedBuildingRecord record in buildingsByInstanceId.Values)
        {
            if (record == null || record.Definition == null || seen.Contains(record))
            {
                continue;
            }

            seen.Add(record);
            entries.Add(new FactoryBuildingEntry
            {
                definitionName = record.Definition.name,
                topLeftX = record.TopLeftGridPosition.x,
                topLeftY = record.TopLeftGridPosition.y,
                rotationQuarterTurns = record.PlacedRotationQuarterTurns
            });
        }

        return entries;
    }

    public bool RestoreBuilding(BuildingDefinition definition, Vector2Int topLeftGridPosition, int rotationQuarterTurns)
    {
        if (definition == null || definition.BehaviorPrefab == null || tileManager == null)
        {
            return false;
        }

        Vector2Int baseFootprint = definition.FootprintSize;
        Vector2Int footprintSize = (rotationQuarterTurns & 1) == 0
            ? baseFootprint
            : new Vector2Int(baseFootprint.y, baseFootprint.x);

        if (!tileManager.CanOccupyFootprint(topLeftGridPosition, footprintSize))
        {
            return false;
        }

        string occupantId = definition.BehaviorPrefab.GetInstanceID().ToString();
        if (!tileManager.TryOccupyFootprint(topLeftGridPosition, footprintSize, occupantId))
        {
            return false;
        }

        Vector3 spawnPosition = GetFootprintWorldCenter(topLeftGridPosition, footprintSize);
        Quaternion spawnRotation = Quaternion.Euler(0f, 0f, rotationQuarterTurns * 90f);
        GameObject spawned = Instantiate(definition.BehaviorPrefab, spawnPosition, spawnRotation);

        BuildingInstance buildingInstance = spawned.GetComponent<BuildingInstance>();
        if (buildingInstance != null)
        {
            buildingInstance.SetGridPosition(topLeftGridPosition, footprintSize, rotationQuarterTurns);
            buildingInstance.Initialize(definition);
        }

        PlacedBuildingRecord record = new PlacedBuildingRecord(spawned, definition, topLeftGridPosition, footprintSize, rotationQuarterTurns);
        buildingsByInstanceId[spawned.GetInstanceID()] = record;

        for (int x = 0; x < footprintSize.x; x++)
        {
            for (int y = 0; y < footprintSize.y; y++)
            {
                spawnedByCell[topLeftGridPosition + new Vector2Int(x, y)] = record;
            }
        }

        return true;
    }

    public void RefreshAllConveyorVisuals()
    {
        var seen = new HashSet<PlacedBuildingRecord>();
        foreach (PlacedBuildingRecord record in buildingsByInstanceId.Values)
        {
            if (record != null && IsConveyorDefinition(record.Definition) && seen.Add(record))
            {
                ApplyConveyorVisualForRecord(record);
            }
        }
    }
}
