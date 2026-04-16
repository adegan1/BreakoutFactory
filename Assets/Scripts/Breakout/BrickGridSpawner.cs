using UnityEngine;

public class BrickGridSpawner : MonoBehaviour
{
    [SerializeField] private BrickController brickPrefab;
    [SerializeField] private int rows = 5;
    [SerializeField] private int columns = 8;
    [SerializeField] private Vector2 spacing = new Vector2(1.2f, 0.6f);
    [SerializeField] private Vector2 startOffset = new Vector2(-4.2f, 3f);

    private void Start()
    {
        if (brickPrefab == null)
        {
            Debug.LogError("Brick prefab is not assigned on BrickGridSpawner.");
            return;
        }

        if (rows <= 0 || columns <= 0)
        {
            return;
        }

        Transform parent = transform;
        Vector3 origin = parent.position + (Vector3)startOffset;

        for (int row = 0; row < rows; row++)
        {
            float rowY = origin.y - row * spacing.y;

            for (int col = 0; col < columns; col++)
            {
                Vector3 position = new Vector3(origin.x + col * spacing.x, rowY, origin.z);
                Instantiate(brickPrefab, position, Quaternion.identity, parent);
            }
        }
    }
}
