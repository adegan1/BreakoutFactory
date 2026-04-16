using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GridVisualizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TileManager tileManager;

    [Header("Appearance")]
    [SerializeField] private Color lineColor = new Color(1f, 1f, 1f, 0.15f);
    [SerializeField] private float lineWidth = 0.04f;
    [SerializeField] private float zOffset = -0.05f;

    [Header("Border")]
    [SerializeField] private Color borderColor = new Color(1f, 1f, 1f, 0.6f);
    [SerializeField] private float borderWidth = 0.1f;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshFilter borderMeshFilter;
    private MeshRenderer borderMeshRenderer;
    private Mesh gridMesh;
    private Mesh borderMesh;
    private Material lineMaterial;
    private Material borderMaterial;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        lineMaterial = CreateMaterial(lineColor);
        meshRenderer.sharedMaterial = lineMaterial;
        meshRenderer.sortingLayerName = "Default";
        meshRenderer.sortingOrder = 1;

        GameObject borderChild = new GameObject("GridBorder");
        borderChild.transform.SetParent(transform, false);
        borderMeshFilter = borderChild.AddComponent<MeshFilter>();
        borderMeshRenderer = borderChild.AddComponent<MeshRenderer>();
        borderMaterial = CreateMaterial(borderColor);
        borderMeshRenderer.sharedMaterial = borderMaterial;
        borderMeshRenderer.sortingLayerName = "Default";
        borderMeshRenderer.sortingOrder = 1;

        BuildGridMesh();
    }

    private void OnDestroy()
    {
        if (gridMesh != null)
        {
            Destroy(gridMesh);
        }

        if (borderMesh != null)
        {
            Destroy(borderMesh);
        }

        if (lineMaterial != null)
        {
            Destroy(lineMaterial);
        }

        if (borderMaterial != null)
        {
            Destroy(borderMaterial);
        }
    }

    [ContextMenu("Rebuild Grid Mesh")]
    public void BuildGridMesh()
    {
        if (tileManager == null)
        {
            Debug.LogWarning("GridVisualizer: No TileManager assigned.", this);
            return;
        }

        int width = tileManager.GridWidth;
        int height = tileManager.GridHeight;
        float tileSize = tileManager.TileSize;
        Vector3 origin = tileManager.GridOrigin;
        float z = origin.z + zOffset;

        int hLines = height + 1;
        int vLines = width + 1;
        int totalLines = hLines + vLines;

        Vector3[] vertices = new Vector3[totalLines * 4];
        int[] indices = new int[totalLines * 6];

        float half = lineWidth * 0.5f;
        int vi = 0;
        int ii = 0;

        // Horizontal lines (rows)
        for (int row = 0; row <= height; row++)
        {
            float y = origin.y + row * tileSize;
            float xStart = origin.x;
            float xEnd = origin.x + width * tileSize;

            vertices[vi + 0] = new Vector3(xStart, y - half, z);
            vertices[vi + 1] = new Vector3(xStart, y + half, z);
            vertices[vi + 2] = new Vector3(xEnd, y + half, z);
            vertices[vi + 3] = new Vector3(xEnd, y - half, z);

            indices[ii + 0] = vi + 0;
            indices[ii + 1] = vi + 1;
            indices[ii + 2] = vi + 2;
            indices[ii + 3] = vi + 0;
            indices[ii + 4] = vi + 2;
            indices[ii + 5] = vi + 3;

            vi += 4;
            ii += 6;
        }

        // Vertical lines (columns)
        for (int col = 0; col <= width; col++)
        {
            float x = origin.x + col * tileSize;
            float yStart = origin.y;
            float yEnd = origin.y + height * tileSize;

            vertices[vi + 0] = new Vector3(x - half, yStart, z);
            vertices[vi + 1] = new Vector3(x - half, yEnd, z);
            vertices[vi + 2] = new Vector3(x + half, yEnd, z);
            vertices[vi + 3] = new Vector3(x + half, yStart, z);

            indices[ii + 0] = vi + 0;
            indices[ii + 1] = vi + 1;
            indices[ii + 2] = vi + 2;
            indices[ii + 3] = vi + 0;
            indices[ii + 4] = vi + 2;
            indices[ii + 5] = vi + 3;

            vi += 4;
            ii += 6;
        }

        EnsureMeshesCreated();
        gridMesh.Clear();
        gridMesh.vertices = vertices;
        gridMesh.triangles = indices;
        gridMesh.RecalculateBounds();

        meshFilter.sharedMesh = gridMesh;

        UpdateLineColor();
        BuildBorderMesh(width, height, tileSize, origin);
    }

    private void BuildBorderMesh(int width, int height, float tileSize, Vector3 origin)
    {
        if (borderMeshFilter == null)
        {
            return;
        }

        float z = origin.z + zOffset;
        float half = borderWidth * 0.5f;

        float left   = origin.x;
        float right  = origin.x + width * tileSize;
        float bottom = origin.y;
        float top    = origin.y + height * tileSize;

        Vector3[] vertices = new Vector3[4 * 4];
        int[] indices = new int[4 * 6];

        int vi = 0;
        int ii = 0;

        void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            vertices[vi + 0] = a; vertices[vi + 1] = b;
            vertices[vi + 2] = c; vertices[vi + 3] = d;
            indices[ii + 0] = vi; indices[ii + 1] = vi + 1; indices[ii + 2] = vi + 2;
            indices[ii + 3] = vi; indices[ii + 4] = vi + 2; indices[ii + 5] = vi + 3;
            vi += 4; ii += 6;
        }

        // Bottom edge
        AddQuad(
            new Vector3(left  - half, bottom - half, z),
            new Vector3(left  - half, bottom + half, z),
            new Vector3(right + half, bottom + half, z),
            new Vector3(right + half, bottom - half, z));

        // Top edge
        AddQuad(
            new Vector3(left  - half, top - half, z),
            new Vector3(left  - half, top + half, z),
            new Vector3(right + half, top + half, z),
            new Vector3(right + half, top - half, z));

        // Left edge (inner extents to avoid corner overlap)
        AddQuad(
            new Vector3(left - half, bottom + half, z),
            new Vector3(left - half, top    - half, z),
            new Vector3(left + half, top    - half, z),
            new Vector3(left + half, bottom + half, z));

        // Right edge
        AddQuad(
            new Vector3(right - half, bottom + half, z),
            new Vector3(right - half, top    - half, z),
            new Vector3(right + half, top    - half, z),
            new Vector3(right + half, bottom + half, z));

        EnsureMeshesCreated();
        borderMesh.Clear();
        borderMesh.vertices = vertices;
        borderMesh.triangles = indices;
        borderMesh.RecalculateBounds();
        borderMeshFilter.sharedMesh = borderMesh;

        if (borderMaterial != null)
        {
            borderMaterial.color = borderColor;
        }
    }

    private void UpdateLineColor()
    {
        if (lineMaterial != null)
        {
            lineMaterial.color = lineColor;
        }
    }

    private void EnsureMeshesCreated()
    {
        if (gridMesh == null)
        {
            gridMesh = new Mesh
            {
                name = "GridMesh"
            };
        }

        if (borderMesh == null)
        {
            borderMesh = new Mesh
            {
                name = "GridBorderMesh"
            };
        }
    }

    private Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("UI/Default");
        }

        Material mat = new Material(shader);
        mat.color = color;
        return mat;
    }
}
