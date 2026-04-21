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

    [Header("Remove Behavior")]
    [SerializeField] private bool refundBuildingToInventoryOnRemove = true;

    [Header("Hover Highlight")]
    [SerializeField] private Transform hoverHighlight;
    [SerializeField] private Color validHoverColor = new Color(0.25f, 1f, 0.45f, 0.5f);
    [SerializeField] private Color blockedHoverColor = new Color(1f, 0.3f, 0.3f, 0.5f);
    [SerializeField] private float hoverZOffset = -0.1f;

    private readonly Dictionary<Vector2Int, PlacedBuildingRecord> spawnedByCell = new();
    private SpriteRenderer hoverHighlightRenderer;
    private bool suppressHoverUntilTileChange;
    private Vector2Int suppressedHoverTile;
    private bool hasPointerTile;
    private Vector2Int pointerGridPosition;
    private Vector3 pointerWorldPoint;

    public int SelectedBuildingIndex => selectedBuildingIndex;
    public BuildingDefinition SelectedBuildingDefinition => GetSelectedBuildingDefinition();
    public GameObject SelectedBuildingPrefab => GetSelectedBuildingDefinition()?.BehaviorPrefab;

    private class PlacedBuildingRecord
    {
        public readonly GameObject SpawnedObject;
        public readonly BuildingDefinition Definition;

        public PlacedBuildingRecord(GameObject spawnedObject, BuildingDefinition definition)
        {
            SpawnedObject = spawnedObject;
            Definition = definition;
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
        }
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            hasPointerTile = false;
            SetHoverHighlightVisible(false);
            return;
        }

        HandleBuildingSelectionInput();

        RefreshPointerState(mouse);
        UpdateHoverHighlight();

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

        Vector3 center = tileManager.GridToWorld(pointerGridPosition);
        center.z = tileManager.GridPlaneZ + hoverZOffset;

        hoverHighlight.position = center;
        hoverHighlight.localScale = new Vector3(tileManager.TileSize, tileManager.TileSize, 1f);

        if (hoverHighlightRenderer != null)
        {
            hoverHighlightRenderer.color = CanPlaceSelectedBuildingAt(pointerGridPosition)
                ? validHoverColor
                : blockedHoverColor;
        }

        SetHoverHighlightVisible(true);
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

        Vector2Int gridPosition = pointerGridPosition;
        if (spawnedByCell.TryGetValue(gridPosition, out PlacedBuildingRecord existing) && existing != null && existing.SpawnedObject != null)
        {
            inventoryManager.AddBuilding(selectedBuildingDefinition, 1);
            return false;
        }

        if (existing == null)
        {
            spawnedByCell.Remove(gridPosition);
        }

        if (!tileManager.TryOccupyTile(gridPosition, selectedBuildingDefinition.name))
        {
            inventoryManager.AddBuilding(selectedBuildingDefinition, 1);
            return false;
        }

        Vector3 spawnPosition = tileManager.GridToWorld(gridPosition);
        GameObject spawned = Instantiate(selectedBuildingPrefab, spawnPosition, Quaternion.identity);

        BuildingInstance buildingInstance = spawned.GetComponent<BuildingInstance>();
        if (buildingInstance != null)
        {
            buildingInstance.Initialize(selectedBuildingDefinition);
        }

        spawnedByCell[gridPosition] = new PlacedBuildingRecord(spawned, selectedBuildingDefinition);
        return true;
    }

    private void TryRemoveAtPointer()
    {
        if (!CanInteractAtPointer())
        {
            return;
        }

        Vector2Int gridPosition = pointerGridPosition;
        if (!spawnedByCell.TryGetValue(gridPosition, out PlacedBuildingRecord record) || record == null)
        {
            return;
        }

        if (!tileManager.ClearTile(gridPosition))
        {
            return;
        }

        if (record.SpawnedObject != null)
        {
            Destroy(record.SpawnedObject);
        }

        spawnedByCell.Remove(gridPosition);

        if (refundBuildingToInventoryOnRemove && inventoryManager != null && record.Definition != null)
        {
            inventoryManager.AddBuilding(record.Definition, 1);
        }

        suppressHoverUntilTileChange = false;
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

        if (tileManager.IsOccupied(gridPosition))
        {
            return false;
        }

        BuildingDefinition selectedBuildingDefinition = GetSelectedBuildingDefinition();
        if (selectedBuildingDefinition == null || selectedBuildingDefinition.BehaviorPrefab == null)
        {
            return false;
        }

        return inventoryManager.HasBuilding(selectedBuildingDefinition, 1);
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
