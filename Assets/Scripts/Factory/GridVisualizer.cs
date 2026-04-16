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

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        meshRenderer.material = CreateLineMaterial();
        meshRenderer.sortingLayerName = "Default";
        meshRenderer.sortingOrder = 1;

        GameObject borderChild = new GameObject("GridBorder");
        borderChild.transform.SetParent(transform, false);
        borderMeshFilter = borderChild.AddComponent<MeshFilter>();
        borderMeshRenderer = borderChild.AddComponent<MeshRenderer>();
        borderMeshRenderer.material = CreateBorderMaterial();
        borderMeshRenderer.sortingLayerName = "Default";
        borderMeshRenderer.sortingOrder = 1;

        BuildGridMesh();
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

        Mesh mesh = new Mesh();
        mesh.name = "GridMesh";
        mesh.vertices = vertices;
        mesh.triangles = indices;
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;

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

        Mesh borderMesh = new Mesh();
        borderMesh.name = "GridBorderMesh";
        borderMesh.vertices = vertices;
        borderMesh.triangles = indices;
        borderMesh.RecalculateBounds();
        borderMeshFilter.mesh = borderMesh;

        if (borderMeshRenderer != null && borderMeshRenderer.material != null)
        {
            borderMeshRenderer.material.color = borderColor;
        }
    }

    private void UpdateLineColor()
    {
        if (meshRenderer != null && meshRenderer.material != null)
        {
            meshRenderer.material.color = lineColor;
        }
    }

    private Material CreateLineMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("UI/Default");
        }

        Material mat = new Material(shader);
        mat.color = lineColor;
        return mat;
    }

    private Material CreateBorderMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("UI/Default");
        }

        Material mat = new Material(shader);
        mat.color = borderColor;
        return mat;
    }
}
