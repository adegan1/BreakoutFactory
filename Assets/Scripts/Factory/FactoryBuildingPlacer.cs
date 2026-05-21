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
    private static readonly HashSet<int> selectedMachineProgressContextIds = new();
    private static FactoryBuildingPlacer instance;

    /// <summary>
    /// Scaled delta time for factory production and movement logic.
    /// Returns 0 when the factory is paused, otherwise Time.deltaTime * current speed multiplier.
    /// </summary>
    public static float FactoryDeltaTime => instance != null
        ? (instance.factoryIsPaused ? 0f : Time.deltaTime * instance.activeFactorySpeed)
        : Time.deltaTime;

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
    [SerializeField] private Button[] disabledWhileMovingButtons;
    [SerializeField] private EventTrigger[] disabledWhileMovingEventTriggers;

    [Header("UI Anti-Stretch")]
    [SerializeField] private bool enableTaggedUiAntiStretch = true;
    [SerializeField] private string antiStretchUiTag = "AntiStretchUI";
    [SerializeField, Min(0.05f)] private float antiStretchUiRescanIntervalSeconds = 0.5f;

    [Header("Marquee Selection")]
    [SerializeField] private Color selectionBoxFillColor = new Color(0.2f, 0.6f, 1f, 0.18f);
    [SerializeField] private Color selectionBoxBorderColor = new Color(0.2f, 0.75f, 1f, 0.9f);

    [Header("Selection Highlight")]
    [SerializeField] private Sprite selectionCornerSprite;
    [SerializeField] private Color selectionCornerColor = new Color(0.95f, 0.85f, 0.15f, 0.9f);
    [SerializeField] private int selectionOverlaySortingOrder = 20;
    [SerializeField] private float selectionOverlayZOffset = -0.05f;

    [Header("Factory Speed")]
    [SerializeField, Min(0.1f)] private float normalFactorySpeed = 1f;
    [SerializeField, Min(0.1f)] private float boostedFactorySpeed = 2f;
    [SerializeField] private bool enableShiftSpeedBoost = true;
    [SerializeField] private Toggle normalSpeedToggle;
    [SerializeField] private Toggle doubleSpeedToggle;
    [SerializeField] private Toggle pauseToggle;

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
    private bool isBoxSelecting;
    private Vector2Int boxSelectionStartTile;
    private Vector2Int boxSelectionEndTile;
    private Vector2 boxSelectionStartGuiPoint;
    private Vector2 boxSelectionCurrentGuiPoint;
    private bool isConveyorDragPlacing;
    private bool hasConveyorDragTile;
    private Vector2Int lastConveyorDragTile;
    private bool isRightClickDragRemoving;
    private Vector2Int lastRightClickDragTile;
    private bool isMovingSelectedGroup;
    private Vector2Int selectedGroupPointerOffset;
    private Vector2Int selectedGroupBoundsSize = Vector2Int.one;
    private readonly List<Transform> movedGroupPreviewHighlights = new();
    private readonly List<SpriteRenderer> movedGroupPreviewRenderers = new();
    private readonly Dictionary<Button, bool> moveModeBlockedButtonStates = new();
    private readonly Dictionary<EventTrigger, bool> moveModeBlockedEventTriggerStates = new();
    private readonly List<Transform> antiStretchUiTargets = new();
    private readonly Dictionary<int, Vector3> antiStretchUiBaseLocalScales = new();
    private int selectedRotationQuarterTurns;
    private float selectedFactorySpeed = 1f;
    private float activeFactorySpeed = 1f;
    private bool isShiftSpeedOverrideActive;
    private bool factoryIsPaused;
    private bool hasWarnedMissingAntiStretchUiTag;
    private float antiStretchUiNextRescanTime;
    private readonly List<Vector2Int> reusableInputTiles = new();
    private readonly List<ItemEntity> reusableItemsOnInput = new();
    private readonly HashSet<int> selectedBuildingInstanceIds = new();
    private readonly List<GroupMoveEntry> selectedGroupMoveEntries = new();
    private readonly Dictionary<int, GameObject> selectionOverlays = new();
    private Transform selectionOverlayParent;
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

    public static bool IsMachineProgressContextSelected(int contextId)
    {
        return contextId >= 0 && selectedMachineProgressContextIds.Contains(contextId);
    }

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

    private struct GroupMoveEntry
    {
        public BuildingDefinition Definition;
        public Vector2Int RelativeTopLeft;
        public Vector2Int FootprintSize;
        public int RotationQuarterTurns;
        public bool HasStoredMachineState;
        public string StoredMachineStateId;
        public int StoredResourceAmount;
        public object SpecializedMoveState;
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

        instance = this;
        selectedFactorySpeed = Mathf.Max(0.1f, normalFactorySpeed);
        ApplyFactorySpeed(selectedFactorySpeed);
        ApplySavedSettings();

        HoveredMachineInstanceId = -1;
        SelectedMachineInstanceId = -1;

        selectionOverlayParent = new GameObject("SelectionOverlays").transform;
        selectionOverlayParent.SetParent(transform, worldPositionStays: false);
    }

    private void Start()
    {
        DetachToggleGroupFromSpeedToggles();
        if (pauseToggle != null)
            pauseToggle.onValueChanged.AddListener(SetFactoryPaused);
        SyncPauseToggleVisual();
    }

    private void OnDisable()
    {
        selectedMachineProgressContextIds.Clear();
        antiStretchUiTargets.Clear();
        antiStretchUiBaseLocalScales.Clear();
        RefreshMoveModeBlockedButtons(forceEnabled: true);
        SetMovedGroupPreviewVisible(false);
        if (instance == this) instance = null;
    }

    private void OnDestroy()
    {
        selectedMachineProgressContextIds.Clear();
        antiStretchUiTargets.Clear();
        antiStretchUiBaseLocalScales.Clear();
        RefreshMoveModeBlockedButtons(forceEnabled: true);
        DestroyMovedGroupPreviewHighlights();
        if (instance == this) instance = null;
    }

    private void LateUpdate()
    {
        ApplyTaggedUiAntiStretch();
    }

    private void Update()
    {
        EnsureInventoryManagerAssigned();
        HandleFactorySpeedInput();
        MaintainSpeedToggleSelectionVisual();
        HandleDeleteSelectedInput();

        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            hasPointerTile = false;
            HoveredMachineInstanceId = -1;
            EndBoxSelection();
            RefreshSelectedBuildingHighlights();
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

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            EndConveyorDragPlacement();
            EndBoxSelection();
        }

        if (mouse.leftButton.wasPressedThisFrame && !pointerOverUi)
        {
            Keyboard keyboard = Keyboard.current;
            bool isCtrlHeld = keyboard != null && (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed);

            if (isMovingSelectedGroup)
            {
                TryPlaceMovedSelectedGroupAtPointer();
            }
            else if (CanUseConveyorDragPlacement())
            {
                BeginConveyorDragPlacement();
                TryPlaceConveyorAlongDragPath();
            }
            else if (!hasSelectedBuilding)
            {
                if (isCtrlHeld)
                {
                    TryAddBuildingAtPointerToSelection();
                }
                else if (HasSelectedBuildings() && IsPointerOverSelectedBuilding())
                {
                    BeginMovingSelectedGroup();
                }
                else
                {
                    BeginBoxSelection(mouse);
                }
            }
            else
            {
                if (hasPointerTile
                    && spawnedByCell.TryGetValue(pointerGridPosition, out PlacedBuildingRecord clickedRecord)
                    && clickedRecord?.SpawnedObject != null)
                {
                    DeselectBuilding();
                    selectedBuildingInstanceIds.Clear();
                    selectedBuildingInstanceIds.Add(clickedRecord.SpawnedObject.GetInstanceID());
                    TrySelectMachineAtPointer();
                }
                else
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
            }
        }
        else if (isConveyorDragPlacing && mouse.leftButton.isPressed && !pointerOverUi)
        {
            TryPlaceConveyorAlongDragPath();
        }
        else if (isBoxSelecting && mouse.leftButton.isPressed)
        {
            ContinueBoxSelection(mouse);
        }

        if (isMovingSelectedGroup || isBoxSelecting)
        {
            isRightClickDragRemoving = false;
        }
        else
        {
            if (mouse.rightButton.wasPressedThisFrame && !pointerOverUi)
            {
                isRightClickDragRemoving = true;
                lastRightClickDragTile = pointerGridPosition;
                TryRemoveAtPointer();
            }
            else if (mouse.rightButton.wasPressedThisFrame && pointerOverUi)
            {
                DeselectBuilding();
            }
            else if (isRightClickDragRemoving && mouse.rightButton.isPressed && !pointerOverUi)
            {
                if (hasPointerTile && pointerGridPosition != lastRightClickDragTile)
                {
                    lastRightClickDragTile = pointerGridPosition;
                    TryRemoveDragAtPointer();
                }
            }

            if (mouse.rightButton.wasReleasedThisFrame)
            {
                isRightClickDragRemoving = false;
            }
        }

        RefreshSelectedBuildingHighlights();
    }

    private void UpdateHoverHighlight()
    {
        if (hoverHighlight == null)
        {
            SetMovedGroupPreviewVisible(false);
            return;
        }

        if (isBoxSelecting)
        {
            SetHoverHighlightVisible(false);
            SetMovedGroupPreviewVisible(false);
            return;
        }

        if (!hasPointerTile)
        {
            SetHoverHighlightVisible(false);
            SetMovedGroupPreviewVisible(false);
            return;
        }

        if (suppressHoverUntilTileChange)
        {
            if (pointerGridPosition == suppressedHoverTile)
            {
                SetHoverHighlightVisible(false);
                SetMovedGroupPreviewVisible(false);
                return;
            }

            suppressHoverUntilTileChange = false;
        }

        if (isMovingSelectedGroup)
        {
            if (!hasPointerTile)
            {
                SetHoverHighlightVisible(false);
                SetMovedGroupPreviewVisible(false);
                return;
            }

            Vector2Int anchorTopLeft = GetMovedGroupAnchorTile();
            bool canPlaceGroup = CanPlaceMovedSelectedGroup(anchorTopLeft);
            DisplayMovedGroupFootprintPreview(anchorTopLeft, canPlaceGroup);
            return;
        }

        SetMovedGroupPreviewVisible(false);

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

    private void OnGUI()
    {
        if (!isBoxSelecting)
        {
            return;
        }

        Rect selectionRect = BuildGuiSelectionRect(boxSelectionStartGuiPoint, boxSelectionCurrentGuiPoint);
        if (selectionRect.width <= 0f || selectionRect.height <= 0f)
        {
            return;
        }

        DrawGuiRect(selectionRect, selectionBoxFillColor);

        float borderThickness = 2f;
        DrawGuiRect(new Rect(selectionRect.xMin, selectionRect.yMin, selectionRect.width, borderThickness), selectionBoxBorderColor);
        DrawGuiRect(new Rect(selectionRect.xMin, selectionRect.yMax - borderThickness, selectionRect.width, borderThickness), selectionBoxBorderColor);
        DrawGuiRect(new Rect(selectionRect.xMin, selectionRect.yMin, borderThickness, selectionRect.height), selectionBoxBorderColor);
        DrawGuiRect(new Rect(selectionRect.xMax - borderThickness, selectionRect.yMin, borderThickness, selectionRect.height), selectionBoxBorderColor);
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
            if (TryGetBallMoldPrimaryInputTile(definition, topLeftGridPosition, footprintSize, rotationQuarterTurns, out Vector2Int ballMoldPrimaryInputTile, out Vector2Int ballMoldInputOutwardDirection))
            {
                primaryInputTile = ballMoldPrimaryInputTile;
                indicatorState.InputTile = primaryInputTile + ballMoldInputOutwardDirection;
            }
            else
            {
                indicatorState.InputTile = GetIndicatorTileOutsideBuilding(primaryInputTile, topLeftGridPosition, footprintSize, rotationQuarterTurns);
            }

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

            TryApplyOutputIndicatorForDefinition(
                definition,
                topLeftGridPosition,
                footprintSize,
                rotationQuarterTurns,
                ref indicatorState);

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

    private void TryApplyOutputIndicatorForDefinition(
        BuildingDefinition definition,
        Vector2Int topLeftGridPosition,
        Vector2Int footprintSize,
        int rotationQuarterTurns,
        ref IndicatorState indicatorState)
    {
        if (definition == null || definition.BehaviorPrefab == null || tileManager == null)
        {
            return;
        }

        IBuildingOutputPreview outputPreviewProvider = definition.BehaviorPrefab.GetComponent<IBuildingOutputPreview>();
        if (outputPreviewProvider == null)
        {
            return;
        }

        if (!outputPreviewProvider.TryGetOutputTile(
                topLeftGridPosition,
                footprintSize,
                rotationQuarterTurns,
                out Vector2Int outputTile,
                out Vector2Int outputDirection))
        {
            return;
        }

        if (!tileManager.IsInBounds(outputTile))
        {
            return;
        }

        indicatorState.HasOutput = true;
        indicatorState.OutputTile = outputTile;
        indicatorState.OutputQuarterTurns = FactoryGridDirectionUtility.DirectionToQuarterTurns(NormalizeCardinal(outputDirection));
    }

    private static bool TryGetBallMoldPrimaryInputTile(
        BuildingDefinition definition,
        Vector2Int topLeftGridPosition,
        Vector2Int footprintSize,
        int rotationQuarterTurns,
        out Vector2Int inputTile,
        out Vector2Int outwardDirection)
    {
        inputTile = default;
        outwardDirection = default;

        if (definition == null || definition.BehaviorPrefab == null)
        {
            return false;
        }

        BallMoldBuilding ballMold = definition.BehaviorPrefab.GetComponent<BallMoldBuilding>();
        if (ballMold == null)
        {
            return false;
        }

        Vector2Int baseInputDirection = FactoryGridDirectionUtility.DirectionFromQuarterTurns((int)ballMold.ConfiguredInputSide);
        outwardDirection = FactoryGridDirectionUtility.RotateDirection(baseInputDirection, rotationQuarterTurns);

        if (outwardDirection == Vector2Int.left)
        {
            inputTile = topLeftGridPosition + new Vector2Int(0, 0);
            return true;
        }

        if (outwardDirection == Vector2Int.up)
        {
            inputTile = topLeftGridPosition + new Vector2Int(0, footprintSize.y - 1);
            return true;
        }

        if (outwardDirection == Vector2Int.right)
        {
            inputTile = topLeftGridPosition + new Vector2Int(footprintSize.x - 1, footprintSize.y - 1);
            return true;
        }

        // Down
        inputTile = topLeftGridPosition + new Vector2Int(footprintSize.x - 1, 0);
        return true;
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

    private void DisplayMovedGroupFootprintPreview(Vector2Int anchorTopLeft, bool canPlace)
    {
        if (tileManager == null)
        {
            SetMovedGroupPreviewVisible(false);
            return;
        }

        EnsureMovedGroupPreviewHighlightCapacity(selectedGroupMoveEntries.Count);

        for (int i = 0; i < selectedGroupMoveEntries.Count; i++)
        {
            GroupMoveEntry entry = selectedGroupMoveEntries[i];
            Transform previewHighlight = movedGroupPreviewHighlights[i];
            SpriteRenderer previewRenderer = movedGroupPreviewRenderers[i];

            if (previewHighlight == null)
            {
                continue;
            }

            Vector2Int targetTopLeft = anchorTopLeft + entry.RelativeTopLeft;
            Vector3 footprintCenter = GetFootprintWorldCenter(targetTopLeft, entry.FootprintSize);
            footprintCenter.z = tileManager.GridPlaneZ + hoverZOffset;

            previewHighlight.position = footprintCenter;
            Vector2 previewScale = entry.Definition != null
                ? entry.Definition.GetVisualScale(entry.FootprintSize, entry.RotationQuarterTurns)
                : new Vector2(entry.FootprintSize.x, entry.FootprintSize.y);
            previewHighlight.localScale = new Vector3(
                previewScale.x * tileManager.TileSize,
                previewScale.y * tileManager.TileSize,
                1f);
            previewHighlight.rotation = Quaternion.Euler(0f, 0f, entry.RotationQuarterTurns * 90f);
            previewHighlight.gameObject.SetActive(true);

            if (previewRenderer != null)
            {
                Sprite previewSprite = entry.Definition != null ? entry.Definition.BuildingSprite : null;
                int previewQuarterTurns = entry.RotationQuarterTurns;

                if (entry.Definition != null && IsConveyorDefinition(entry.Definition))
                {
                    ConveyorVisualResolver.Result conveyorVisual = ConveyorVisualResolver.Resolve(
                        entry.Definition,
                        GetIncomingDirectionForMovedGroupPreviewPosition(targetTopLeft, anchorTopLeft),
                        entry.RotationQuarterTurns);

                    previewSprite = conveyorVisual.Sprite;
                    previewQuarterTurns = conveyorVisual.QuarterTurns;
                }

                previewRenderer.sprite = previewSprite != null ? previewSprite : defaultHoverSprite;

                Color previewBaseColor = entry.Definition != null ? entry.Definition.BuildingColor : Color.white;
                previewRenderer.color = BuildPreviewTint(previewBaseColor, canPlace);

                previewHighlight.rotation = Quaternion.Euler(0f, 0f, previewQuarterTurns * 90f);
            }
        }

        for (int i = selectedGroupMoveEntries.Count; i < movedGroupPreviewHighlights.Count; i++)
        {
            Transform previewHighlight = movedGroupPreviewHighlights[i];
            if (previewHighlight != null)
            {
                previewHighlight.gameObject.SetActive(false);
            }
        }

        SetHoverHighlightVisible(false);
    }

    private void EnsureMovedGroupPreviewHighlightCapacity(int count)
    {
        if (hoverHighlight == null)
        {
            return;
        }

        while (movedGroupPreviewHighlights.Count < count)
        {
            GameObject previewObject = Instantiate(hoverHighlight.gameObject, hoverHighlight.parent);
            previewObject.name = hoverHighlight.gameObject.name + "_MovedGroupPreview";
            previewObject.SetActive(false);

            movedGroupPreviewHighlights.Add(previewObject.transform);
            movedGroupPreviewRenderers.Add(previewObject.GetComponent<SpriteRenderer>());
        }
    }

    private void SetMovedGroupPreviewVisible(bool isVisible)
    {
        for (int i = 0; i < movedGroupPreviewHighlights.Count; i++)
        {
            Transform previewHighlight = movedGroupPreviewHighlights[i];
            if (previewHighlight == null)
            {
                continue;
            }

            if (previewHighlight.gameObject.activeSelf != isVisible)
            {
                previewHighlight.gameObject.SetActive(isVisible);
            }
        }
    }

    private void DestroyMovedGroupPreviewHighlights()
    {
        for (int i = 0; i < movedGroupPreviewHighlights.Count; i++)
        {
            Transform previewHighlight = movedGroupPreviewHighlights[i];
            if (previewHighlight != null)
            {
                Destroy(previewHighlight.gameObject);
            }
        }

        movedGroupPreviewHighlights.Clear();
        movedGroupPreviewRenderers.Clear();
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

        if (tileManager != null)
        {
            Vector2 previewScale = definition.GetVisualScale(definition.FootprintSize, quarterTurns);
            hoverHighlight.localScale = new Vector3(
                previewScale.x * tileManager.TileSize,
                previewScale.y * tileManager.TileSize,
                1f);
        }

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

    private bool CanUseConveyorDragPlacement()
    {
        BuildingDefinition selectedDefinition = GetSelectedBuildingDefinition();
        return selectedDefinition != null && IsConveyorDefinition(selectedDefinition);
    }

    private void HandleDeleteSelectedInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.deleteKey.wasPressedThisFrame)
        {
            return;
        }

        if (isMovingSelectedGroup)
        {
            return;
        }

        DeleteSelectedBuildings();
    }

    private bool HasSelectedBuildings()
    {
        return selectedBuildingInstanceIds.Count > 0;
    }

    private void RefreshSelectedBuildingHighlights()
    {
        RefreshSelectedMachineProgressContextIds();

        int marqueeMinX = 0;
        int marqueeMaxX = 0;
        int marqueeMinY = 0;
        int marqueeMaxY = 0;
        bool hasMarqueePreview = isBoxSelecting;

        if (hasMarqueePreview)
        {
            marqueeMinX = Mathf.Min(boxSelectionStartTile.x, boxSelectionEndTile.x);
            marqueeMaxX = Mathf.Max(boxSelectionStartTile.x, boxSelectionEndTile.x);
            marqueeMinY = Mathf.Min(boxSelectionStartTile.y, boxSelectionEndTile.y);
            marqueeMaxY = Mathf.Max(boxSelectionStartTile.y, boxSelectionEndTile.y);
        }

        foreach (PlacedBuildingRecord record in buildingsByInstanceId.Values)
        {
            if (record == null || record.SpawnedObject == null)
            {
                continue;
            }

            int instanceId = record.SpawnedObject.GetInstanceID();
            bool isSelected = selectedBuildingInstanceIds.Contains(instanceId);
            bool isMarqueePreview = hasMarqueePreview
                && DoesFootprintIntersectRect(
                    record.TopLeftGridPosition,
                    record.FootprintSize,
                    marqueeMinX,
                    marqueeMaxX,
                    marqueeMinY,
                    marqueeMaxY);

            bool shouldHighlight = isSelected || isMarqueePreview;
            SetSelectionHighlight(record.SpawnedObject, shouldHighlight);
        }
    }

    private void SetSelectionHighlight(GameObject targetObject, bool enabled)
    {
        if (targetObject == null || tileManager == null)
        {
            return;
        }

        int instanceId = targetObject.GetInstanceID();

        if (!enabled)
        {
            if (selectionOverlays.TryGetValue(instanceId, out GameObject existing))
            {
                existing.SetActive(false);
            }

            return;
        }

        if (!buildingsByInstanceId.TryGetValue(instanceId, out PlacedBuildingRecord record) || record == null)
        {
            return;
        }

        if (!selectionOverlays.TryGetValue(instanceId, out GameObject overlay) || overlay == null)
        {
            overlay = new GameObject("SelectionOverlay");
            overlay.transform.SetParent(selectionOverlayParent, worldPositionStays: false);
            SpriteRenderer sr = overlay.AddComponent<SpriteRenderer>();
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.sortingOrder = selectionOverlaySortingOrder;
            selectionOverlays[instanceId] = overlay;
        }

        Vector3 center = ComputeFootprintWorldCenter(record);
        overlay.transform.position = center;
        overlay.transform.localScale = Vector3.one;

        if (selectionCornerSprite != null)
        {
            SpriteRenderer sr = overlay.GetComponent<SpriteRenderer>();
            sr.sprite = selectionCornerSprite;
            sr.color = selectionCornerColor;
            sr.size = new Vector2(
                record.FootprintSize.x * tileManager.TileSize,
                record.FootprintSize.y * tileManager.TileSize);
        }

        overlay.SetActive(true);
    }

    private Vector3 ComputeFootprintWorldCenter(PlacedBuildingRecord record)
    {
        Vector3 topLeftCenter = tileManager.GridToWorld(record.TopLeftGridPosition);
        float tileSize = tileManager.TileSize;
        return topLeftCenter + new Vector3(
            (record.FootprintSize.x - 1) * tileSize * 0.5f,
            (record.FootprintSize.y - 1) * tileSize * 0.5f,
            selectionOverlayZOffset);
    }

    private void BeginBoxSelection(Mouse mouse)
    {
        if (!hasPointerTile)
        {
            return;
        }

        isBoxSelecting = true;
        boxSelectionStartTile = pointerGridPosition;
        boxSelectionEndTile = pointerGridPosition;

        Vector2 mousePosition = mouse.position.ReadValue();
        boxSelectionStartGuiPoint = ToGuiPoint(mousePosition);
        boxSelectionCurrentGuiPoint = boxSelectionStartGuiPoint;
    }

    private void ContinueBoxSelection(Mouse mouse)
    {
        if (!isBoxSelecting)
        {
            return;
        }

        if (hasPointerTile)
        {
            boxSelectionEndTile = pointerGridPosition;
        }

        boxSelectionCurrentGuiPoint = ToGuiPoint(mouse.position.ReadValue());
    }

    private void EndBoxSelection()
    {
        if (!isBoxSelecting)
        {
            return;
        }

        isBoxSelecting = false;
        SelectBuildingsInTileRect(boxSelectionStartTile, boxSelectionEndTile);
    }

    private void SelectBuildingsInTileRect(Vector2Int startTile, Vector2Int endTile)
    {
        selectedBuildingInstanceIds.Clear();

        int minX = Mathf.Min(startTile.x, endTile.x);
        int maxX = Mathf.Max(startTile.x, endTile.x);
        int minY = Mathf.Min(startTile.y, endTile.y);
        int maxY = Mathf.Max(startTile.y, endTile.y);

        HashSet<PlacedBuildingRecord> uniqueRecords = new HashSet<PlacedBuildingRecord>(buildingsByInstanceId.Values);
        foreach (PlacedBuildingRecord record in uniqueRecords)
        {
            if (record == null || record.SpawnedObject == null)
            {
                continue;
            }

            if (!DoesFootprintIntersectRect(record.TopLeftGridPosition, record.FootprintSize, minX, maxX, minY, maxY))
            {
                continue;
            }

            selectedBuildingInstanceIds.Add(record.SpawnedObject.GetInstanceID());
        }

        if (!HasSelectedBuildings())
        {
            SelectedMachineInstanceId = -1;
        }
    }

    private static bool DoesFootprintIntersectRect(
        Vector2Int topLeft,
        Vector2Int footprintSize,
        int minX,
        int maxX,
        int minY,
        int maxY)
    {
        int footprintMinX = topLeft.x;
        int footprintMaxX = topLeft.x + footprintSize.x - 1;
        int footprintMinY = topLeft.y;
        int footprintMaxY = topLeft.y + footprintSize.y - 1;

        bool separated = footprintMaxX < minX || footprintMinX > maxX || footprintMaxY < minY || footprintMinY > maxY;
        return !separated;
    }

    private bool IsPointerOverSelectedBuilding()
    {
        if (!hasPointerTile)
        {
            return false;
        }

        if (!spawnedByCell.TryGetValue(pointerGridPosition, out PlacedBuildingRecord record)
            || record == null
            || record.SpawnedObject == null)
        {
            return false;
        }

        return selectedBuildingInstanceIds.Contains(record.SpawnedObject.GetInstanceID());
    }

    private bool TryAddBuildingAtPointerToSelection()
    {
        if (!hasPointerTile
            || !spawnedByCell.TryGetValue(pointerGridPosition, out PlacedBuildingRecord record)
            || record?.SpawnedObject == null)
        {
            return false;
        }

        int instanceId = record.SpawnedObject.GetInstanceID();
        selectedBuildingInstanceIds.Add(instanceId);
        SelectedMachineInstanceId = GetMachineProgressContextId(record);
        return true;
    }

    private void RefreshSelectedMachineProgressContextIds()
    {
        selectedMachineProgressContextIds.Clear();

        foreach (int instanceId in selectedBuildingInstanceIds)
        {
            if (!buildingsByInstanceId.TryGetValue(instanceId, out PlacedBuildingRecord record)
                || record == null
                || record.SpawnedObject == null)
            {
                continue;
            }

            int contextId = GetMachineProgressContextId(record);
            if (contextId >= 0)
            {
                selectedMachineProgressContextIds.Add(contextId);
            }
        }
    }

    private bool BeginMovingSelectedGroup()
    {
        if (!HasSelectedBuildings())
        {
            return false;
        }

        List<PlacedBuildingRecord> selectedRecords = new List<PlacedBuildingRecord>();
        foreach (int instanceId in selectedBuildingInstanceIds)
        {
            if (!buildingsByInstanceId.TryGetValue(instanceId, out PlacedBuildingRecord record)
                || record == null
                || record.SpawnedObject == null)
            {
                continue;
            }

            selectedRecords.Add(record);
        }

        if (selectedRecords.Count == 0)
        {
            selectedBuildingInstanceIds.Clear();
            return false;
        }

        Vector2Int selectionAnchor = selectedRecords[0].TopLeftGridPosition;
        for (int i = 1; i < selectedRecords.Count; i++)
        {
            selectionAnchor.x = Mathf.Min(selectionAnchor.x, selectedRecords[i].TopLeftGridPosition.x);
            selectionAnchor.y = Mathf.Min(selectionAnchor.y, selectedRecords[i].TopLeftGridPosition.y);
        }

        selectedGroupMoveEntries.Clear();
        int maxRelativeX = 0;
        int maxRelativeY = 0;

        for (int i = 0; i < selectedRecords.Count; i++)
        {
            PlacedBuildingRecord record = selectedRecords[i];
            Vector2Int relativeTopLeft = record.TopLeftGridPosition - selectionAnchor;

            bool hasStoredMachineState = TryCaptureMachineState(
                record.SpawnedObject,
                out string storedMachineStateId,
                out int storedResourceAmount);

            object specializedMoveState = CaptureSpecializedMoveState(record.SpawnedObject);

            selectedGroupMoveEntries.Add(new GroupMoveEntry
            {
                Definition = record.Definition,
                RelativeTopLeft = relativeTopLeft,
                FootprintSize = record.FootprintSize,
                RotationQuarterTurns = record.PlacedRotationQuarterTurns,
                HasStoredMachineState = hasStoredMachineState,
                StoredMachineStateId = storedMachineStateId,
                StoredResourceAmount = storedResourceAmount,
                SpecializedMoveState = specializedMoveState
            });

            maxRelativeX = Mathf.Max(maxRelativeX, relativeTopLeft.x + record.FootprintSize.x - 1);
            maxRelativeY = Mathf.Max(maxRelativeY, relativeTopLeft.y + record.FootprintSize.y - 1);
        }

        selectedGroupBoundsSize = new Vector2Int(maxRelativeX + 1, maxRelativeY + 1);
        selectedGroupPointerOffset = hasPointerTile ? pointerGridPosition - selectionAnchor : Vector2Int.zero;

        for (int i = 0; i < selectedRecords.Count; i++)
        {
            RemovePlacedBuilding(selectedRecords[i], false, dropPendingItemToGround: ShouldDropPendingItemDuringMove(selectedRecords[i]));
        }

        selectedBuildingInstanceIds.Clear();
        isMovingSelectedGroup = selectedGroupMoveEntries.Count > 0;
        RefreshMoveModeBlockedButtons();
        RefreshAllConveyorVisuals();
        return isMovingSelectedGroup;
    }

    private Vector2Int GetMovedGroupAnchorTile()
    {
        return pointerGridPosition - selectedGroupPointerOffset;
    }

    private bool CanPlaceMovedSelectedGroup(Vector2Int anchorTopLeft)
    {
        if (tileManager == null)
        {
            return false;
        }

        for (int i = 0; i < selectedGroupMoveEntries.Count; i++)
        {
            GroupMoveEntry entry = selectedGroupMoveEntries[i];
            Vector2Int targetTopLeft = anchorTopLeft + entry.RelativeTopLeft;

            if (!tileManager.CanOccupyFootprint(targetTopLeft, entry.FootprintSize))
            {
                return false;
            }

            if (!CanPlaceWithItemExceptions(
                    entry.Definition,
                    targetTopLeft,
                    entry.FootprintSize,
                    entry.RotationQuarterTurns))
            {
                return false;
            }
        }

        return true;
    }

    private bool TryPlaceMovedSelectedGroupAtPointer()
    {
        if (!isMovingSelectedGroup || !hasPointerTile)
        {
            return false;
        }

        Vector2Int anchorTopLeft = GetMovedGroupAnchorTile();
        if (!CanPlaceMovedSelectedGroup(anchorTopLeft))
        {
            return false;
        }

        for (int i = 0; i < selectedGroupMoveEntries.Count; i++)
        {
            GroupMoveEntry entry = selectedGroupMoveEntries[i];
            Vector2Int targetTopLeft = anchorTopLeft + entry.RelativeTopLeft;

            if (!RestoreBuilding(entry.Definition, targetTopLeft, entry.RotationQuarterTurns))
            {
                return false;
            }

            if (spawnedByCell.TryGetValue(targetTopLeft, out PlacedBuildingRecord placedRecord)
                && placedRecord != null
                && placedRecord.SpawnedObject != null)
            {
                ApplySpecializedMoveState(placedRecord.SpawnedObject, entry.SpecializedMoveState);

                if (entry.HasStoredMachineState)
                {
                    ApplyCapturedMachineState(placedRecord.SpawnedObject, entry.StoredMachineStateId, entry.StoredResourceAmount);
                    RelinkItemSourceGeneratorReferences(placedRecord.SpawnedObject, entry.StoredMachineStateId);
                }
            }
        }

        selectedGroupMoveEntries.Clear();
        isMovingSelectedGroup = false;
        RefreshMoveModeBlockedButtons();
        selectedGroupPointerOffset = Vector2Int.zero;
        suppressHoverUntilTileChange = false;
        RefreshAllConveyorVisuals();
        return true;
    }

    private bool DeleteSelectedBuildings()
    {
        if (!HasSelectedBuildings())
        {
            return false;
        }

        List<PlacedBuildingRecord> selectedRecords = new List<PlacedBuildingRecord>();
        foreach (int instanceId in selectedBuildingInstanceIds)
        {
            if (!buildingsByInstanceId.TryGetValue(instanceId, out PlacedBuildingRecord record)
                || record == null
                || record.SpawnedObject == null)
            {
                continue;
            }

            selectedRecords.Add(record);
        }

        bool removedAny = false;
        for (int i = 0; i < selectedRecords.Count; i++)
        {
            removedAny |= RemovePlacedBuilding(selectedRecords[i], refundBuildingToInventoryOnRemove);
        }

        selectedBuildingInstanceIds.Clear();
        if (removedAny)
        {
            RefreshAllConveyorVisuals();
        }

        return removedAny;
    }

    private static bool TryCaptureMachineState(
        GameObject spawnedObject,
        out string machineStateId,
        out int storedResourceAmount)
    {
        machineStateId = null;
        storedResourceAmount = 0;

        if (spawnedObject == null)
        {
            return false;
        }

        IMachineStoredResourceReceiver receiver = spawnedObject.GetComponentInChildren<IMachineStoredResourceReceiver>();
        IMachineResourceProgressProvider provider = spawnedObject.GetComponentInChildren<IMachineResourceProgressProvider>();
        if (receiver == null || provider == null)
        {
            return false;
        }

        machineStateId = receiver.MachineStateId;
        if (string.IsNullOrEmpty(machineStateId))
        {
            machineStateId = Guid.NewGuid().ToString("N");
            receiver.SetMachineStateId(machineStateId);
        }

        storedResourceAmount = provider.CurrentResourceAmount;
        return true;
    }

    private static void ApplyCapturedMachineState(
        GameObject spawnedObject,
        string machineStateId,
        int storedResourceAmount)
    {
        if (spawnedObject == null)
        {
            return;
        }

        IMachineStoredResourceReceiver receiver = spawnedObject.GetComponentInChildren<IMachineStoredResourceReceiver>();
        if (receiver == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(machineStateId))
        {
            receiver.SetMachineStateId(machineStateId);
        }

        receiver.SetStoredResourceAmount(storedResourceAmount);
    }

    private static object CaptureSpecializedMoveState(GameObject spawnedObject)
    {
        if (spawnedObject == null)
        {
            return null;
        }

        BallMoldBuilding mold = spawnedObject.GetComponent<BallMoldBuilding>();
        if (mold == null)
        {
            mold = spawnedObject.GetComponentInChildren<BallMoldBuilding>();
        }

        if (mold != null)
        {
            return mold.CaptureMoveState();
        }

        return null;
    }

    private static void ApplySpecializedMoveState(GameObject spawnedObject, object specializedMoveState)
    {
        if (spawnedObject == null || specializedMoveState == null)
        {
            return;
        }

        if (specializedMoveState is BallMoldBuilding.MoveState moldState)
        {
            BallMoldBuilding mold = spawnedObject.GetComponent<BallMoldBuilding>();
            if (mold == null)
            {
                mold = spawnedObject.GetComponentInChildren<BallMoldBuilding>();
            }

            if (mold != null)
            {
                mold.ApplyMoveState(moldState);
            }
        }
    }

    private static bool ShouldDropPendingItemDuringMove(PlacedBuildingRecord record)
    {
        if (record == null || record.SpawnedObject == null)
        {
            return false;
        }

        // Fusion reactors should behave like normal removal while moving: drop internal pending output.
        return record.SpawnedObject.GetComponent<FusionReactorBuilding>() != null;
    }

    private static void RelinkItemSourceGeneratorReferences(GameObject spawnedObject, string machineStateId)
    {
        if (spawnedObject == null || string.IsNullOrEmpty(machineStateId))
        {
            return;
        }

        if (!spawnedObject.TryGetComponent<GeneratorBuilding>(out GeneratorBuilding generator) || generator == null)
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

            item.TryRebindSourceGenerator(generator, machineStateId);
        }
    }

    private static Vector2 ToGuiPoint(Vector2 screenPoint)
    {
        return new Vector2(screenPoint.x, Screen.height - screenPoint.y);
    }

    private static Rect BuildGuiSelectionRect(Vector2 start, Vector2 end)
    {
        float xMin = Mathf.Min(start.x, end.x);
        float yMin = Mathf.Min(start.y, end.y);
        float width = Mathf.Abs(end.x - start.x);
        float height = Mathf.Abs(end.y - start.y);
        return new Rect(xMin, yMin, width, height);
    }

    private static void DrawGuiRect(Rect rect, Color color)
    {
        Color previousColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previousColor;
    }

    private void BeginConveyorDragPlacement()
    {
        isConveyorDragPlacing = true;
        hasConveyorDragTile = false;
    }

    private void EndConveyorDragPlacement()
    {
        isConveyorDragPlacing = false;
        hasConveyorDragTile = false;
    }

    private void TryPlaceConveyorAlongDragPath()
    {
        if (!isConveyorDragPlacing || !hasPointerTile)
        {
            return;
        }

        if (!CanUseConveyorDragPlacement())
        {
            EndConveyorDragPlacement();
            return;
        }

        if (!hasConveyorDragTile)
        {
            if (TryPlaceAtGrid(pointerGridPosition, selectedRotationQuarterTurns))
            {
                hasConveyorDragTile = true;
                lastConveyorDragTile = pointerGridPosition;
                suppressHoverUntilTileChange = false;
            }

            return;
        }

        if (pointerGridPosition == lastConveyorDragTile)
        {
            return;
        }

        Vector2Int delta = pointerGridPosition - lastConveyorDragTile;
        Vector2Int dragDirection = NormalizeCardinal(delta);
        int steps = dragDirection.x != 0 ? Mathf.Abs(delta.x) : Mathf.Abs(delta.y);
        if (steps <= 0)
        {
            return;
        }

        int rotationQuarterTurns = FactoryGridDirectionUtility.DirectionToQuarterTurns(dragDirection);

        // Re-orient the previously placed belt so the chain points consistently along drag direction.
        TryPlaceAtGrid(lastConveyorDragTile, rotationQuarterTurns);

        for (int i = 1; i <= steps; i++)
        {
            Vector2Int nextTile = lastConveyorDragTile + (dragDirection * i);
            if (!TryPlaceAtGrid(nextTile, rotationQuarterTurns))
            {
                break;
            }

            lastConveyorDragTile = nextTile;
        }
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

    private void RefreshMoveModeBlockedButtons(bool forceEnabled = false)
    {
        if (disabledWhileMovingButtons == null || disabledWhileMovingButtons.Length == 0)
        {
            // Continue through to event-trigger handling below.
        }

        bool shouldDisable = isMovingSelectedGroup && !forceEnabled;

        for (int i = 0; i < disabledWhileMovingButtons.Length; i++)
        {
            Button button = disabledWhileMovingButtons[i];
            if (button == null)
            {
                continue;
            }

            if (shouldDisable)
            {
                if (!moveModeBlockedButtonStates.ContainsKey(button))
                {
                    moveModeBlockedButtonStates[button] = button.interactable;
                }

                button.interactable = false;
                continue;
            }

            if (moveModeBlockedButtonStates.TryGetValue(button, out bool previousInteractable))
            {
                button.interactable = previousInteractable;
                moveModeBlockedButtonStates.Remove(button);
            }
        }

        if (disabledWhileMovingEventTriggers != null && disabledWhileMovingEventTriggers.Length > 0)
        {
            for (int i = 0; i < disabledWhileMovingEventTriggers.Length; i++)
            {
                EventTrigger eventTrigger = disabledWhileMovingEventTriggers[i];
                if (eventTrigger == null)
                {
                    continue;
                }

                if (shouldDisable)
                {
                    if (!moveModeBlockedEventTriggerStates.ContainsKey(eventTrigger))
                    {
                        moveModeBlockedEventTriggerStates[eventTrigger] = eventTrigger.enabled;
                    }

                    eventTrigger.enabled = false;
                    continue;
                }

                if (moveModeBlockedEventTriggerStates.TryGetValue(eventTrigger, out bool previousEnabled))
                {
                    eventTrigger.enabled = previousEnabled;
                    moveModeBlockedEventTriggerStates.Remove(eventTrigger);
                }
            }
        }

        if (!shouldDisable)
        {
            moveModeBlockedButtonStates.Clear();
            moveModeBlockedEventTriggerStates.Clear();
        }
    }

    private void ApplyTaggedUiAntiStretch()
    {
        if (!enableTaggedUiAntiStretch || string.IsNullOrWhiteSpace(antiStretchUiTag))
        {
            return;
        }

        if (Time.unscaledTime >= antiStretchUiNextRescanTime || antiStretchUiTargets.Count == 0)
        {
            RescanAntiStretchUiTargets();
            antiStretchUiNextRescanTime = Time.unscaledTime + antiStretchUiRescanIntervalSeconds;
        }

        for (int i = antiStretchUiTargets.Count - 1; i >= 0; i--)
        {
            Transform target = antiStretchUiTargets[i];
            if (target == null)
            {
                antiStretchUiTargets.RemoveAt(i);
                continue;
            }

            ApplyAntiStretchToTarget(target);
        }
    }

    private void RescanAntiStretchUiTargets()
    {
        antiStretchUiTargets.Clear();

        GameObject[] taggedObjects;
        try
        {
            taggedObjects = GameObject.FindGameObjectsWithTag(antiStretchUiTag);
        }
        catch (UnityException)
        {
            if (!hasWarnedMissingAntiStretchUiTag)
            {
                Debug.LogWarning($"FactoryBuildingPlacer: tag '{antiStretchUiTag}' is not defined for anti-stretch UI.");
                hasWarnedMissingAntiStretchUiTag = true;
            }

            return;
        }

        hasWarnedMissingAntiStretchUiTag = false;

        for (int i = 0; i < taggedObjects.Length; i++)
        {
            GameObject taggedObject = taggedObjects[i];
            if (taggedObject == null)
            {
                continue;
            }

            Transform target = taggedObject.transform;
            antiStretchUiTargets.Add(target);

            int id = target.GetInstanceID();
            if (!antiStretchUiBaseLocalScales.ContainsKey(id))
            {
                antiStretchUiBaseLocalScales[id] = target.localScale;
            }
        }
    }

    private void ApplyAntiStretchToTarget(Transform target)
    {
        if (target == null)
        {
            return;
        }

        int id = target.GetInstanceID();
        if (!antiStretchUiBaseLocalScales.TryGetValue(id, out Vector3 baseLocalScale))
        {
            baseLocalScale = target.localScale;
            antiStretchUiBaseLocalScales[id] = baseLocalScale;
        }

        Vector3 parentLossy = target.parent != null ? target.parent.lossyScale : Vector3.one;
        float parentX = Mathf.Abs(parentLossy.x);
        float parentY = Mathf.Abs(parentLossy.y);
        if (parentX <= 0.00001f || parentY <= 0.00001f)
        {
            return;
        }

        float inheritedAxis = Mathf.Min(parentX, parentY);
        float desiredWorldX = inheritedAxis * Mathf.Abs(baseLocalScale.x);
        float desiredWorldY = inheritedAxis * Mathf.Abs(baseLocalScale.y);

        Vector3 correctedLocalScale = baseLocalScale;
        correctedLocalScale.x = Mathf.Sign(baseLocalScale.x == 0f ? 1f : baseLocalScale.x) * (desiredWorldX / parentX);
        correctedLocalScale.y = Mathf.Sign(baseLocalScale.y == 0f ? 1f : baseLocalScale.y) * (desiredWorldY / parentY);

        target.localScale = correctedLocalScale;
    }

    private bool TryPlaceAtPointer()
    {
        if (!CanInteractAtPointer())
        {
            return false;
        }

        return TryPlaceAtGrid(pointerGridPosition, selectedRotationQuarterTurns);
    }

    private bool TryPlaceAtGrid(Vector2Int gridPosition, int rotationQuarterTurns)
    {
        if (tileManager == null || inventoryManager == null || !tileManager.IsInBounds(gridPosition))
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

        Vector2Int footprintSize = GetRotatedFootprintSize(selectedBuildingDefinition.FootprintSize, rotationQuarterTurns);
        Vector2Int optimalTopLeft = CalculateOptimalPlacementPosition(gridPosition, footprintSize);

        if (!CanPlaceWithItemExceptions(selectedBuildingDefinition, optimalTopLeft, footprintSize, rotationQuarterTurns))
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
        
        float rotationDegrees = rotationQuarterTurns * 90f;
        Quaternion spawnRotation = Quaternion.Euler(0f, 0f, rotationDegrees);
        GameObject spawned = Instantiate(selectedBuildingPrefab, spawnPosition, spawnRotation);

        BuildingInstance buildingInstance = spawned.GetComponent<BuildingInstance>();
        if (buildingInstance != null)
        {
            buildingInstance.SetGridPosition(optimalTopLeft, footprintSize, rotationQuarterTurns);
            buildingInstance.Initialize(selectedBuildingDefinition, tileManager != null ? tileManager.TileSize : 1f);
        }

        ApplyStoredMachineResourceIfAvailable(spawned, selectedBuildingDefinition);

        TryFeedItemsIntoPlacedInputBuilding(spawned, selectedBuildingDefinition, optimalTopLeft, footprintSize, rotationQuarterTurns);

        PlacedBuildingRecord record = new PlacedBuildingRecord(spawned, selectedBuildingDefinition, optimalTopLeft, footprintSize, rotationQuarterTurns);
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

    // Simplified remove used while right-click dragging — skips deselect side effects.
    private void TryRemoveDragAtPointer()
    {
        if (!CanInteractAtPointer())
        {
            return;
        }

        Vector2Int gridPosition = pointerGridPosition;
        if (!spawnedByCell.TryGetValue(gridPosition, out PlacedBuildingRecord record) || record?.SpawnedObject == null)
        {
            return;
        }

        Vector2Int topLeft = record.TopLeftGridPosition;
        Vector2Int footprintSize = record.FootprintSize;

        if (!RemovePlacedBuilding(record, refundBuildingToInventoryOnRemove))
        {
            return;
        }

        RefreshConveyorVisualsAround(topLeft, footprintSize);
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

    private bool RemovePlacedBuilding(PlacedBuildingRecord record, bool refundToInventory, bool dropPendingItemToGround = true)
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

        if (dropPendingItemToGround)
        {
            IMachinePendingItemDropper pendingItemDropper = record.SpawnedObject.GetComponentInChildren<IMachinePendingItemDropper>();
            pendingItemDropper?.TryDropPendingItemToGround();
        }

        Destroy(record.SpawnedObject);
        buildingsByInstanceId.Remove(instanceId);
        selectedBuildingInstanceIds.Remove(instanceId);

        if (selectionOverlays.TryGetValue(instanceId, out GameObject overlay))
        {
            Destroy(overlay);
            selectionOverlays.Remove(instanceId);
        }

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

    // Checks if a tile position is occupied by a non-conveyor building.
    // Used by generators to determine if they can output to this position.
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

    private Vector2Int? GetIncomingDirectionForMovedGroupPreviewPosition(Vector2Int targetPosition, Vector2Int anchorTopLeft)
    {
        for (int i = 0; i < CardinalDirections.Length; i++)
        {
            Vector2Int neighborPosition = targetPosition + CardinalDirections[i];

            if (TryGetMovedGroupEntryAtWorldPosition(neighborPosition, anchorTopLeft, out GroupMoveEntry movedNeighbor)
                && movedNeighbor.Definition != null
                && IsConveyorDefinition(movedNeighbor.Definition))
            {
                Vector2Int neighborDirection = ConveyorVisualResolver.DirectionFromQuarterTurns(movedNeighbor.RotationQuarterTurns);
                if (neighborPosition + neighborDirection == targetPosition)
                {
                    return neighborDirection;
                }
            }

            if (!spawnedByCell.TryGetValue(neighborPosition, out PlacedBuildingRecord neighborRecord)
                || neighborRecord == null
                || neighborRecord.SpawnedObject == null
                || !IsConveyorDefinition(neighborRecord.Definition))
            {
                continue;
            }

            int neighborQuarterTurns = neighborRecord.PlacedRotationQuarterTurns;
            Vector2Int neighborWorldDirection = ConveyorVisualResolver.DirectionFromQuarterTurns(neighborQuarterTurns);
            if (neighborPosition + neighborWorldDirection == targetPosition)
            {
                return neighborWorldDirection;
            }
        }

        return null;
    }

    private bool TryGetMovedGroupEntryAtWorldPosition(Vector2Int worldPosition, Vector2Int anchorTopLeft, out GroupMoveEntry entry)
    {
        for (int i = 0; i < selectedGroupMoveEntries.Count; i++)
        {
            GroupMoveEntry candidate = selectedGroupMoveEntries[i];
            if (anchorTopLeft + candidate.RelativeTopLeft != worldPosition)
            {
                continue;
            }

            entry = candidate;
            return true;
        }

        entry = default;
        return false;
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
            if (isMovingSelectedGroup)
            {
                RotateMovingSelectedGroup(isShiftHeld ? 1 : 3);
            }
            else
            {
                selectedRotationQuarterTurns = isShiftHeld
                    ? (selectedRotationQuarterTurns + 1) % 4
                    : (selectedRotationQuarterTurns + 3) % 4;
            }
            suppressHoverUntilTileChange = false;
        }
    }

    private void RotateMovingSelectedGroup(int quarterTurnsDelta)
    {
        if (!isMovingSelectedGroup || selectedGroupMoveEntries.Count == 0)
        {
            return;
        }

        int normalizedDelta = ((quarterTurnsDelta % 4) + 4) % 4;
        if (normalizedDelta == 0)
        {
            return;
        }

        Vector2Int originalBounds = selectedGroupBoundsSize;
        List<GroupMoveEntry> rotatedEntries = new List<GroupMoveEntry>(selectedGroupMoveEntries.Count);

        for (int i = 0; i < selectedGroupMoveEntries.Count; i++)
        {
            GroupMoveEntry entry = selectedGroupMoveEntries[i];
            Vector2Int rotatedTopLeft = entry.RelativeTopLeft;
            Vector2Int rotatedFootprint = entry.FootprintSize;
            int rotatedQuarterTurns = entry.RotationQuarterTurns;

            if (normalizedDelta == 1)
            {
                rotatedTopLeft = new Vector2Int(
                    originalBounds.y - (entry.RelativeTopLeft.y + entry.FootprintSize.y),
                    entry.RelativeTopLeft.x);
                rotatedFootprint = new Vector2Int(entry.FootprintSize.y, entry.FootprintSize.x);
                rotatedQuarterTurns = (rotatedQuarterTurns + 1) % 4;
            }
            else if (normalizedDelta == 2)
            {
                rotatedTopLeft = new Vector2Int(
                    originalBounds.x - (entry.RelativeTopLeft.x + entry.FootprintSize.x),
                    originalBounds.y - (entry.RelativeTopLeft.y + entry.FootprintSize.y));
                rotatedQuarterTurns = (rotatedQuarterTurns + 2) % 4;
            }
            else if (normalizedDelta == 3)
            {
                rotatedTopLeft = new Vector2Int(
                    entry.RelativeTopLeft.y,
                    originalBounds.x - (entry.RelativeTopLeft.x + entry.FootprintSize.x));
                rotatedFootprint = new Vector2Int(entry.FootprintSize.y, entry.FootprintSize.x);
                rotatedQuarterTurns = (rotatedQuarterTurns + 3) % 4;
            }

            entry.RelativeTopLeft = rotatedTopLeft;
            entry.FootprintSize = rotatedFootprint;
            entry.RotationQuarterTurns = rotatedQuarterTurns;
            rotatedEntries.Add(entry);
        }

        selectedGroupMoveEntries.Clear();
        selectedGroupMoveEntries.AddRange(rotatedEntries);

        selectedGroupBoundsSize = (normalizedDelta & 1) == 0
            ? originalBounds
            : new Vector2Int(originalBounds.y, originalBounds.x);
    }

    private void HandleFactorySpeedInput()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
        {
            ToggleFactoryPause();
        }

        if (factoryIsPaused)
        {
            return;
        }

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
        if (!isOn)
        {
            SyncSpeedToggleVisualWithSelectedSpeed();
            return;
        }
        if (factoryIsPaused) SetFactoryPaused(false);
        SetFactorySpeedTo1x();
    }

    public void SetFactorySpeedTo2x()
    {
        SetFactorySpeedSelection(isDoubleSpeed: true, persistSetting: true);
    }

    public void SetFactorySpeedTo2x(bool isOn)
    {
        if (!isOn)
        {
            SyncSpeedToggleVisualWithSelectedSpeed();
            return;
        }
        if (factoryIsPaused) SetFactoryPaused(false);
        SetFactorySpeedTo2x();
    }

    public void ToggleFactoryPause()
    {
        SetFactoryPaused(!factoryIsPaused);
    }

    public void SetFactoryPaused(bool paused)
    {
        if (factoryIsPaused == paused)
        {
            return;
        }

        factoryIsPaused = paused;
        SyncPauseToggleVisual();
        ApplyFactorySpeed(selectedFactorySpeed);
    }

    private void ApplyFactorySpeed(float speed)
    {
        activeFactorySpeed = Mathf.Max(0.1f, speed);
    }

    private void ApplySavedSettings()
    {
        GameSettings settings = GameSettings.Instance;
        SetShowInfo(settings.ShowInfo, false);
        SetShowControls(settings.ShowControls, false);
        SetFactorySpeedSelection(settings.FactorySpeedIsDouble, false);

        if (settings.FactoryAutoPause)
        {
            SetFactoryPaused(true);
        }
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

    private void SyncSpeedToggleVisualWithSelectedSpeed()
    {
        bool isDoubleSelected = Mathf.Approximately(selectedFactorySpeed, Mathf.Max(0.1f, boostedFactorySpeed));
        SetSpeedToggleVisual(isDoubleSelected);
    }

    private void DetachToggleGroupFromSpeedToggles()
    {
        if (normalSpeedToggle != null) normalSpeedToggle.group = null;
        if (doubleSpeedToggle != null) doubleSpeedToggle.group = null;
        if (pauseToggle != null) pauseToggle.group = null;
    }

    private void SyncPauseToggleVisual()
    {
        if (pauseToggle != null)
            pauseToggle.SetIsOnWithoutNotify(factoryIsPaused);

        if (factoryIsPaused)
        {
            if (normalSpeedToggle != null) normalSpeedToggle.SetIsOnWithoutNotify(false);
            if (doubleSpeedToggle != null) doubleSpeedToggle.SetIsOnWithoutNotify(false);
        }
        else
        {
            SyncSpeedToggleVisualWithSelectedSpeed();
        }
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
        if (factoryIsPaused)
        {
            return pauseToggle != null ? pauseToggle : normalSpeedToggle;
        }

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

        if (isMovingSelectedGroup)
        {
            return false;
        }

        if (index < 0 || index >= InventoryManager.BuildingHotbarSlotCount)
        {
            return false;
        }

        selectedBuildingInstanceIds.Clear();
        selectedGroupMoveEntries.Clear();
        isMovingSelectedGroup = false;
        selectedBuildingIndex = index;
        hasSelectedBuilding = true;
        suppressHoverUntilTileChange = false;
        RefreshSelectedBuildingHighlights();
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
        return GetRotatedFootprintSize(baseFootprint, selectedRotationQuarterTurns);
    }

    private static Vector2Int GetRotatedFootprintSize(Vector2Int baseFootprint, int rotationQuarterTurns)
    {
        if ((rotationQuarterTurns & 1) == 0)
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
        RefreshSelectedBuildingHighlights();
        SetHoverHighlightVisible(false);
        SetMovedGroupPreviewVisible(false);
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
        selectedBuildingInstanceIds.Clear();
        selectedGroupMoveEntries.Clear();
        isMovingSelectedGroup = false;
        isBoxSelecting = false;
        RefreshSelectedBuildingHighlights();
        RefreshMoveModeBlockedButtons(forceEnabled: true);
        HoveredMachineInstanceId = -1;
        SelectedMachineInstanceId = -1;
        suppressHoverUntilTileChange = false;
        SetHoverHighlightVisible(false);
        SetMovedGroupPreviewVisible(false);
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
            buildingInstance.Initialize(definition, tileManager != null ? tileManager.TileSize : 1f);
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
