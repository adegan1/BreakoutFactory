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

    private void Awake()
    {
        InitializeGrid();
    }

    [ContextMenu("Rebuild Grid")]
    public void InitializeGrid()
    {
        if (gridWidth <= 0 || gridHeight <= 0)
        {
            Debug.LogWarning("Grid size must be greater than zero.", this);
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
        if (!IsInBounds(gridPosition))
        {
            return false;
        }

        return tiles[gridPosition.x, gridPosition.y].IsOccupied;
    }

    public bool TryOccupyTile(Vector2Int gridPosition, string occupantId)
    {
        if (!IsInBounds(gridPosition))
        {
            return false;
        }

        TileCell tile = tiles[gridPosition.x, gridPosition.y];
        if (tile.IsOccupied)
        {
            return false;
        }

        tile.SetOccupant(occupantId);
        return true;
    }

    public bool ClearTile(Vector2Int gridPosition)
    {
        if (!IsInBounds(gridPosition))
        {
            return false;
        }

        TileCell tile = tiles[gridPosition.x, gridPosition.y];
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
        Vector3 localPosition = worldPosition - gridOrigin;
        int x = Mathf.FloorToInt(localPosition.x / tileSize);
        int y = Mathf.FloorToInt(localPosition.y / tileSize);
        return new Vector2Int(x, y);
    }

    public bool TryGetTile(Vector2Int gridPosition, out TileCell tile)
    {
        tile = null;

        if (!IsInBounds(gridPosition))
        {
            return false;
        }

        tile = tiles[gridPosition.x, gridPosition.y];
        return true;
    }

    private void OnDrawGizmos()
    {
        if (!drawGridGizmos || gridWidth <= 0 || gridHeight <= 0 || tileSize <= 0f)
        {
            return;
        }

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2Int gridPosition = new Vector2Int(x, y);
                Vector3 center = GridToWorld(gridPosition);
                Vector3 size = new Vector3(tileSize, tileSize, 0.02f);

                bool occupied = tiles != null
                    && x < tiles.GetLength(0)
                    && y < tiles.GetLength(1)
                    && tiles[x, y] != null
                    && tiles[x, y].IsOccupied;

                Gizmos.color = occupied ? occupiedTileColor : emptyTileColor;
                Gizmos.DrawCube(center, size);

                Gizmos.color = Color.black;
                Gizmos.DrawWireCube(center, size);
            }
        }
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
