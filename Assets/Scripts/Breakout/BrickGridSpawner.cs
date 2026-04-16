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

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                Vector3 position = transform.position + new Vector3(startOffset.x + col * spacing.x, startOffset.y - row * spacing.y, 0f);
                Instantiate(brickPrefab, position, Quaternion.identity, transform);
            }
        }
    }
}
