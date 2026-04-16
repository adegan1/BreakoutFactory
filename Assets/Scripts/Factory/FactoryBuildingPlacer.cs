using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class FactoryBuildingPlacer : MonoBehaviour
{
    [SerializeField] private TileManager tileManager;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private GameObject buildingPrefab;
    [SerializeField] private LayerMask placementSurfaceMask = ~0;

    [Header("Hover Highlight")]
    [SerializeField] private Transform hoverHighlight;
    [SerializeField] private Color validHoverColor = new Color(0.25f, 1f, 0.45f, 0.5f);
    [SerializeField] private Color blockedHoverColor = new Color(1f, 0.3f, 0.3f, 0.5f);
    [SerializeField] private float hoverZOffset = -0.1f;

    private readonly Dictionary<Vector2Int, GameObject> spawnedByCell = new();
    private SpriteRenderer hoverHighlightRenderer;
    private bool suppressHoverUntilTileChange;
    private Vector2Int suppressedHoverTile;
    private bool hasPointerTile;
    private Vector2Int pointerGridPosition;
    private Vector3 pointerWorldPoint;

    private void Reset()
    {
        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }
    }

    private void Awake()
    {
        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

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
            hoverHighlightRenderer.color = tileManager.IsOccupied(pointerGridPosition)
                ? blockedHoverColor
                : validHoverColor;
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

        Vector2Int gridPosition = pointerGridPosition;
        if (spawnedByCell.TryGetValue(gridPosition, out GameObject existing) && existing != null)
        {
            return false;
        }

        if (existing == null)
        {
            spawnedByCell.Remove(gridPosition);
        }

        if (!tileManager.TryOccupyTile(gridPosition, buildingPrefab.name))
        {
            return false;
        }

        Vector3 spawnPosition = tileManager.GridToWorld(gridPosition);
        GameObject spawned = Instantiate(buildingPrefab, spawnPosition, Quaternion.identity);
        spawnedByCell[gridPosition] = spawned;
        return true;
    }

    private void TryRemoveAtPointer()
    {
        if (!CanInteractAtPointer())
        {
            return;
        }

        Vector2Int gridPosition = pointerGridPosition;
        if (!spawnedByCell.TryGetValue(gridPosition, out GameObject spawned))
        {
            return;
        }

        if (!tileManager.ClearTile(gridPosition))
        {
            return;
        }

        if (spawned != null)
        {
            Destroy(spawned);
        }

        spawnedByCell.Remove(gridPosition);
        suppressHoverUntilTileChange = false;
    }

    private bool CanInteractAtPointer()
    {
        return hasPointerTile && tileManager != null && buildingPrefab != null;
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

        if (tileManager == null || worldCamera == null || buildingPrefab == null)
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
}
