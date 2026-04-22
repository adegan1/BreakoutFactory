using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class FactoryBuildingPlacer : MonoBehaviour
{
    [SerializeField] private TileManager tileManager;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private InventoryManager inventoryManager;
    [FormerlySerializedAs("buildingPrefab")]
    [SerializeField] private GameObject defaultBuildingPrefab;
    [SerializeField] private LayerMask placementSurfaceMask = ~0;

    [Header("Building Selection")]
    [SerializeField] private int selectedBuildingIndex;
    [SerializeField] private bool enableNumberKeySelection = true;
    [SerializeField] private bool enableRotationInput = true;

    [Header("Remove Behavior")]
    [SerializeField] private bool refundBuildingToInventoryOnRemove = true;

    [Header("Hover Highlight")]
    [SerializeField] private Transform hoverHighlight;
    [SerializeField] private Color validHoverColor = new Color(0.25f, 1f, 0.45f, 0.5f);
    [SerializeField] private Color blockedHoverColor = new Color(1f, 0.3f, 0.3f, 0.5f);
    [SerializeField] private float hoverZOffset = -0.1f;

    [Header("Conveyor Direction Indicator")]
    [SerializeField] private Transform conveyorDirectionIndicator;
    [SerializeField] private Color conveyorIndicatorColor = new Color(1f, 1f, 1f, 0.85f);

    private readonly Dictionary<Vector2Int, PlacedBuildingRecord> spawnedByCell = new();
    private readonly Dictionary<int, PlacedBuildingRecord> buildingsByInstanceId = new();
    private SpriteRenderer hoverHighlightRenderer;
    private SpriteRenderer conveyorDirectionIndicatorRenderer;
    private Sprite defaultHoverSprite;
    private Quaternion defaultHoverRotation = Quaternion.identity;
    private bool suppressHoverUntilTileChange;
    private Vector2Int suppressedHoverTile;
    private bool hasPointerTile;
    private Vector2Int pointerGridPosition;
    private Vector3 pointerWorldPoint;
    private int selectedRotationQuarterTurns;
    private static readonly Vector2Int[] CardinalDirections =
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left
    };

    public int SelectedBuildingIndex => selectedBuildingIndex;
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

        if (conveyorDirectionIndicator != null)
        {
            conveyorDirectionIndicatorRenderer = conveyorDirectionIndicator.GetComponent<SpriteRenderer>();
        }
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            hasPointerTile = false;
            SetHoverHighlightVisible(false);
            SetConveyorDirectionIndicatorVisible(false);
            return;
        }

        HandleBuildingSelectionInput();
        HandleRotationInput();

        RefreshPointerState(mouse);
        UpdateHoverHighlight();
        UpdateConveyorDirectionIndicator();

        if (mouse.leftButton.wasPressedThisFrame)
        {
            bool didPlace = TryPlaceAtPointer();
            if (didPlace)
            {
                SuppressHoverAtPointerTile();
            }
        }

        if (mouse.rightButton.wasPressedThisFrame)
        {
            TryRemoveAtPointer();
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

        // Check if there's a building at the pointer (for removal)
        if (spawnedByCell.TryGetValue(pointerGridPosition, out PlacedBuildingRecord buildingAtPointer) && buildingAtPointer != null)
        {
            BuildingDefinition selectedDef = GetSelectedBuildingDefinition();
            Vector2Int fp = selectedDef != null ? GetRotatedFootprintSize(selectedDef.FootprintSize) : Vector2Int.zero;
            Vector2Int tl = selectedDef != null ? CalculateOptimalPlacementPosition(pointerGridPosition, fp) : default;
            bool isReplacement = selectedDef != null && CanReplaceConveyorAt(tl, fp, selectedDef);

            if (!isReplacement)
            {
                DisplayFootprintHighlight(buildingAtPointer.TopLeftGridPosition, buildingAtPointer.FootprintSize, blockedHoverColor);
                ApplyDefaultHoverVisual(blockedHoverColor);
                return;
            }
            // Conveyor replacement: fall through to show placement preview
        }

        // Show placement preview for selected building
        BuildingDefinition selectedBuildingDefinition = GetSelectedBuildingDefinition();
        if (selectedBuildingDefinition != null)
        {
            Vector2Int footprintSize = GetRotatedFootprintSize(selectedBuildingDefinition.FootprintSize);
            Vector2Int optimalTopLeft = CalculateOptimalPlacementPosition(pointerGridPosition, footprintSize);
            bool canPlace = CanPlaceSelectedBuildingAt(pointerGridPosition);
            Color previewColor = canPlace ? validHoverColor : blockedHoverColor;

            DisplayFootprintHighlight(optimalTopLeft, footprintSize, previewColor);
            ApplyBuildingHoverVisual(selectedBuildingDefinition, optimalTopLeft, canPlace);
            return;
        }

        SetHoverHighlightVisible(false);
    }

    private void UpdateConveyorDirectionIndicator()
    {
        if (conveyorDirectionIndicator == null)
        {
            return;
        }

        if (!hasPointerTile || suppressHoverUntilTileChange && pointerGridPosition == suppressedHoverTile)
        {
            SetConveyorDirectionIndicatorVisible(false);
            return;
        }

        BuildingDefinition selectedDef = GetSelectedBuildingDefinition();
        if (selectedDef == null || !IsConveyorDefinition(selectedDef))
        {
            SetConveyorDirectionIndicatorVisible(false);
            return;
        }

        Vector2Int footprintSize = GetRotatedFootprintSize(selectedDef.FootprintSize);
        Vector2Int optimalTopLeft = CalculateOptimalPlacementPosition(pointerGridPosition, footprintSize);

        // The output direction is always the belt's facing direction regardless of turn type.
        // Turns only affect which side input is received from, not where items exit.
        Vector2Int outputDirection = ConveyorVisualResolver.DirectionFromQuarterTurns(selectedRotationQuarterTurns);
        Vector2Int outputTile = optimalTopLeft + outputDirection;

        if (spawnedByCell.TryGetValue(outputTile, out PlacedBuildingRecord outputOccupant)
            && IsConveyorDefinition(outputOccupant?.Definition))
        {
            SetConveyorDirectionIndicatorVisible(false);
            return;
        }

        Vector3 indicatorPos = tileManager.GridToWorld(outputTile);
        indicatorPos.z = tileManager.GridPlaneZ + hoverZOffset;

        conveyorDirectionIndicator.position = indicatorPos;
        conveyorDirectionIndicator.localScale = new Vector3(tileManager.TileSize, tileManager.TileSize, 1f);
        conveyorDirectionIndicator.rotation = Quaternion.Euler(0f, 0f, selectedRotationQuarterTurns * 90f);

        if (conveyorDirectionIndicatorRenderer != null)
        {
            conveyorDirectionIndicatorRenderer.color = conveyorIndicatorColor;
        }

        SetConveyorDirectionIndicatorVisible(true);
    }

    private void SetConveyorDirectionIndicatorVisible(bool isVisible)
    {
        if (conveyorDirectionIndicator == null)
        {
            return;
        }

        if (conveyorDirectionIndicator.gameObject.activeSelf != isVisible)
        {
            conveyorDirectionIndicator.gameObject.SetActive(isVisible);
        }
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

    private bool RemovePlacedBuilding(PlacedBuildingRecord record, bool refundToInventory)
    {
        if (!tileManager.ClearFootprint(record.TopLeftGridPosition, record.FootprintSize))
        {
            return false;
        }

        int instanceId = record.SpawnedObject.GetInstanceID();
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

        int quarterTurns = conveyorVisual.QuarterTurns;
        Sprite selectedSprite = conveyorVisual.Sprite;

        conveyorRecord.SpawnedObject.transform.rotation = Quaternion.Euler(0f, 0f, quarterTurns * 90f);

        BuildingInstance buildingInstance = conveyorRecord.SpawnedObject.GetComponent<BuildingInstance>();
        if (buildingInstance != null)
        {
            buildingInstance.SetGridPosition(
                conveyorRecord.TopLeftGridPosition,
                conveyorRecord.FootprintSize,
                quarterTurns);
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

    private void HandleBuildingSelectionInput()
    {
        if (!enableNumberKeySelection)
        {
            return;
        }

        ClampSelectedBuildingIndex();

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || inventoryManager == null || inventoryManager.BuildingItems.Count == 0)
        {
            return;
        }

        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            TrySelectBuildingByIndex(0);
        }
        else if (keyboard.digit2Key.wasPressedThisFrame)
        {
            TrySelectBuildingByIndex(1);
        }
        else if (keyboard.digit3Key.wasPressedThisFrame)
        {
            TrySelectBuildingByIndex(2);
        }
        else if (keyboard.digit4Key.wasPressedThisFrame)
        {
            TrySelectBuildingByIndex(3);
        }
        else if (keyboard.digit5Key.wasPressedThisFrame)
        {
            TrySelectBuildingByIndex(4);
        }
        else if (keyboard.digit6Key.wasPressedThisFrame)
        {
            TrySelectBuildingByIndex(5);
        }
        else if (keyboard.digit7Key.wasPressedThisFrame)
        {
            TrySelectBuildingByIndex(6);
        }
        else if (keyboard.digit8Key.wasPressedThisFrame)
        {
            TrySelectBuildingByIndex(7);
        }
        else if (keyboard.digit9Key.wasPressedThisFrame)
        {
            TrySelectBuildingByIndex(8);
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
            selectedRotationQuarterTurns = (selectedRotationQuarterTurns + 3) % 4;
            suppressHoverUntilTileChange = false;
        }
    }

    public bool TrySelectBuildingByIndex(int index)
    {
        if (inventoryManager == null)
        {
            return false;
        }

        IReadOnlyList<InventoryManager.InventoryEntry> buildingItems = inventoryManager.BuildingItems;
        if (index < 0 || index >= buildingItems.Count)
        {
            return false;
        }

        if (buildingItems[index] == null || buildingItems[index].BuildingDefinition == null)
        {
            return false;
        }

        selectedBuildingIndex = index;
        return true;
    }

    private BuildingDefinition GetSelectedBuildingDefinition()
    {
        if (inventoryManager == null)
        {
            return null;
        }

        IReadOnlyList<InventoryManager.InventoryEntry> buildingItems = inventoryManager.BuildingItems;
        if (buildingItems.Count == 0)
        {
            return null;
        }

        ClampSelectedBuildingIndex();
        InventoryManager.InventoryEntry selectedEntry = buildingItems[selectedBuildingIndex];
        return selectedEntry?.BuildingDefinition;
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
            && inventoryManager.HasBuilding(selectedBuildingDefinition, 1);
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
            return;
        }

        int buildingCount = inventoryManager.BuildingItems.Count;
        if (buildingCount == 0)
        {
            selectedBuildingIndex = 0;
            return;
        }

        selectedBuildingIndex = Mathf.Clamp(selectedBuildingIndex, 0, buildingCount - 1);
    }
}
