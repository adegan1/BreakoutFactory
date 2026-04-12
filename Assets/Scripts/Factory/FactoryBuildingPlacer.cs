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

    private void Reset()
    {
        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            SetHoverHighlightVisible(false);
            return;
        }

        UpdateHoverHighlight();

        if (mouse.leftButton.wasPressedThisFrame)
        {
            bool didPlace = TryPlaceAtMouse();
            if (didPlace)
            {
                SuppressHoverAtCurrentTile();
            }
        }

        if (mouse.rightButton.wasPressedThisFrame)
        {
            TryRemoveAtMouse();
            SuppressHoverAtCurrentTile();
        }
    }

    private void UpdateHoverHighlight()
    {
        if (hoverHighlight == null)
        {
            return;
        }

        if (!TryGetMouseHitPoint(out Vector3 hitPoint))
        {
            SetHoverHighlightVisible(false);
            return;
        }

        Vector2Int gridPosition = tileManager.WorldToGrid(hitPoint);
        if (!tileManager.IsInBounds(gridPosition))
        {
            SetHoverHighlightVisible(false);
            return;
        }

        if (suppressHoverUntilTileChange)
        {
            if (gridPosition == suppressedHoverTile)
            {
                SetHoverHighlightVisible(false);
                return;
            }

            suppressHoverUntilTileChange = false;
        }

        Vector3 center = tileManager.GridToWorld(gridPosition);
        center.z = tileManager.GridPlaneZ + hoverZOffset;

        hoverHighlight.position = center;
        hoverHighlight.localScale = new Vector3(tileManager.TileSize, tileManager.TileSize, 1f);

        if (hoverHighlightRenderer == null)
        {
            hoverHighlightRenderer = hoverHighlight.GetComponent<SpriteRenderer>();
        }

        if (hoverHighlightRenderer != null)
        {
            hoverHighlightRenderer.color = tileManager.IsOccupied(gridPosition)
                ? blockedHoverColor
                : validHoverColor;
        }

        SetHoverHighlightVisible(true);
    }

    private void SuppressHoverAtCurrentTile()
    {
        if (!TryGetMouseHitPoint(out Vector3 hitPoint))
        {
            return;
        }

        Vector2Int gridPosition = tileManager.WorldToGrid(hitPoint);
        if (!tileManager.IsInBounds(gridPosition))
        {
            return;
        }

        suppressedHoverTile = gridPosition;
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

    private bool TryPlaceAtMouse()
    {
        if (!TryGetMouseHitPoint(out Vector3 hitPoint))
        {
            return false;
        }

        Vector2Int gridPosition = tileManager.WorldToGrid(hitPoint);
        if (spawnedByCell.TryGetValue(gridPosition, out GameObject existing) && existing != null)
        {
            return false;
        }

        if (existing == null)
        {
            spawnedByCell.Remove(gridPosition);
        }

        string occupantId = $"{gridPosition.x}_{gridPosition.y}";

        if (!tileManager.TryOccupyTile(gridPosition, occupantId))
        {
            return false;
        }

        Vector3 spawnPosition = tileManager.GridToWorld(gridPosition);
        GameObject spawned = Instantiate(buildingPrefab, spawnPosition, Quaternion.identity);
        spawnedByCell[gridPosition] = spawned;
        return true;
    }

    private void TryRemoveAtMouse()
    {
        if (!TryGetMouseHitPoint(out Vector3 hitPoint))
        {
            return;
        }

        Vector2Int gridPosition = tileManager.WorldToGrid(hitPoint);
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
    }

    private bool TryGetMouseHitPoint(out Vector3 hitPoint)
    {
        hitPoint = default;

        if (tileManager == null || worldCamera == null || buildingPrefab == null)
        {
            return false;
        }

        Mouse mouse = Mouse.current;
        if (mouse == null)
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
