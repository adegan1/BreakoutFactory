using UnityEngine;

public class TileManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 20;
    [SerializeField] private int gridHeight = 20;
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private Vector3 gridOrigin = Vector3.zero;

    [Header("Debug View")]
    [SerializeField] private bool drawGridGizmos = true;
    [SerializeField] private Color emptyTileColor = new Color(1f, 1f, 1f, 0.2f);
    [SerializeField] private Color occupiedTileColor = new Color(1f, 0.25f, 0.25f, 0.35f);

    private TileCell[,] tiles;

    public float GridPlaneZ => gridOrigin.z;
    public float TileSize => tileSize;
    public int GridWidth => gridWidth;
    public int GridHeight => gridHeight;
    public Vector3 GridOrigin => gridOrigin;

    private void Awake()
    {
        InitializeGrid();
    }

    [ContextMenu("Rebuild Grid")]
    public void InitializeGrid()
    {
        if (gridWidth <= 0 || gridHeight <= 0 || tileSize <= 0f)
        {
            Debug.LogWarning("Grid dimensions and tile size must be greater than zero.", this);
            tiles = null;
            return;
        }

        tiles = new TileCell[gridWidth, gridHeight];

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                tiles[x, y] = new TileCell(new Vector2Int(x, y));
            }
        }
    }

    public bool IsInBounds(Vector2Int gridPosition)
    {
        return gridPosition.x >= 0
            && gridPosition.y >= 0
            && gridPosition.x < gridWidth
            && gridPosition.y < gridHeight;
    }

    public bool IsOccupied(Vector2Int gridPosition)
    {
        if (!TryGetTileCell(gridPosition, out TileCell tile))
        {
            return false;
        }

        return tile.IsOccupied;
    }

    public bool TryOccupyTile(Vector2Int gridPosition, string occupantId)
    {
        if (!TryGetTileCell(gridPosition, out TileCell tile))
        {
            return false;
        }

        if (tile.IsOccupied)
        {
            return false;
        }

        tile.SetOccupant(occupantId);
        return true;
    }

    public bool ClearTile(Vector2Int gridPosition)
    {
        if (!TryGetTileCell(gridPosition, out TileCell tile))
        {
            return false;
        }

        if (!tile.IsOccupied)
        {
            return false;
        }

        tile.Clear();
        return true;
    }

    public Vector3 GridToWorld(Vector2Int gridPosition)
    {
        return gridOrigin + new Vector3(
            (gridPosition.x + 0.5f) * tileSize,
            (gridPosition.y + 0.5f) * tileSize,
            0f);
    }

    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        if (tileSize <= 0f)
        {
            return new Vector2Int(-1, -1);
        }

        Vector3 localPosition = worldPosition - gridOrigin;
        int x = Mathf.FloorToInt(localPosition.x / tileSize);
        int y = Mathf.FloorToInt(localPosition.y / tileSize);
        return new Vector2Int(x, y);
    }

    public bool TryGetTile(Vector2Int gridPosition, out TileCell tile)
    {
        return TryGetTileCell(gridPosition, out tile);
    }

    public bool CanOccupyFootprint(Vector2Int topLeftGridPosition, Vector2Int footprintSize)
    {
        if (footprintSize.x <= 0 || footprintSize.y <= 0)
        {
            return false;
        }

        for (int x = 0; x < footprintSize.x; x++)
        {
            for (int y = 0; y < footprintSize.y; y++)
            {
                Vector2Int tilePos = topLeftGridPosition + new Vector2Int(x, y);
                if (!IsInBounds(tilePos) || IsOccupied(tilePos))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public bool TryOccupyFootprint(Vector2Int topLeftGridPosition, Vector2Int footprintSize, string occupantId)
    {
        if (!CanOccupyFootprint(topLeftGridPosition, footprintSize))
        {
            return false;
        }

        for (int x = 0; x < footprintSize.x; x++)
        {
            for (int y = 0; y < footprintSize.y; y++)
            {
                Vector2Int tilePos = topLeftGridPosition + new Vector2Int(x, y);
                if (!TryOccupyTile(tilePos, occupantId))
                {
                    // Rollback on partial failure
                    ClearFootprint(topLeftGridPosition, new Vector2Int(x, y));
                    return false;
                }
            }
        }

        return true;
    }

    public bool ClearFootprint(Vector2Int topLeftGridPosition, Vector2Int footprintSize)
    {
        bool anyCleared = false;

        for (int x = 0; x < footprintSize.x; x++)
        {
            for (int y = 0; y < footprintSize.y; y++)
            {
                Vector2Int tilePos = topLeftGridPosition + new Vector2Int(x, y);
                if (ClearTile(tilePos))
                {
                    anyCleared = true;
                }
            }
        }

        return anyCleared;
    }

    public string GetOccupantAtPosition(Vector2Int gridPosition)
    {
        if (!TryGetTileCell(gridPosition, out TileCell tile))
        {
            return string.Empty;
        }

        return tile.OccupantId;
    }

    private void OnDrawGizmos()
    {
        if (!drawGridGizmos || gridWidth <= 0 || gridHeight <= 0 || tileSize <= 0f)
        {
            return;
        }

        int tileColumns = tiles?.GetLength(0) ?? 0;
        int tileRows = tiles?.GetLength(1) ?? 0;
        float halfTileSize = tileSize * 0.5f;
        Vector3 size = new Vector3(tileSize, tileSize, 0.02f);

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector3 center = new Vector3(
                    gridOrigin.x + x * tileSize + halfTileSize,
                    gridOrigin.y + y * tileSize + halfTileSize,
                    gridOrigin.z);

                bool occupied = x < tileColumns
                    && y < tileRows
                    && tiles[x, y] != null
                    && tiles[x, y].IsOccupied;

                Gizmos.color = occupied ? occupiedTileColor : emptyTileColor;
                Gizmos.DrawCube(center, size);

                Gizmos.color = Color.black;
                Gizmos.DrawWireCube(center, size);
            }
        }
    }

    private bool TryGetTileCell(Vector2Int gridPosition, out TileCell tile)
    {
        tile = null;

        if (tiles == null || !IsInBounds(gridPosition))
        {
            return false;
        }

        tile = tiles[gridPosition.x, gridPosition.y];
        return tile != null;
    }

    [System.Serializable]
    public class TileCell
    {
        public Vector2Int GridPosition { get; }
        public bool IsOccupied { get; private set; }
        public string OccupantId { get; private set; }

        public TileCell(Vector2Int gridPosition)
        {
            GridPosition = gridPosition;
            IsOccupied = false;
            OccupantId = string.Empty;
        }

        public void SetOccupant(string occupantId)
        {
            IsOccupied = true;
            OccupantId = occupantId;
        }

        public void Clear()
        {
            IsOccupied = false;
            OccupantId = string.Empty;
        }
    }
}
