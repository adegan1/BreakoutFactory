using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class BreakoutItemDrop : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private BreakoutGameController owningController;
    private BuildingDefinition buildingDefinition;
    private int quantity;
    private float fallSpeed;
    private float bottomKillY;
    private bool isCollected;
    private bool isMovementLocked;

    public BuildingDefinition BuildingDefinition => buildingDefinition;
    public int Quantity => quantity;

    private void Reset()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;

        Collider2D dropCollider = GetComponent<Collider2D>();
        dropCollider.isTrigger = true;
    }

    private void Update()
    {
        if (isCollected || isMovementLocked)
        {
            return;
        }

        transform.position += Vector3.down * Mathf.Max(0f, fallSpeed) * Time.deltaTime;

        if (transform.position.y < bottomKillY)
        {
            Destroy(gameObject);
        }
    }

    public void Initialize(BreakoutGameController controller, BuildingDefinition definition, int amount, float speed, float killY)
    {
        owningController = controller;
        buildingDefinition = definition;
        quantity = Mathf.Max(1, amount);
        fallSpeed = Mathf.Max(0f, speed);
        bottomKillY = killY;

        ApplyItemVisuals();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null)
        {
            return;
        }

        TryCollect(other.transform);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision == null)
        {
            return;
        }

        TryCollect(collision.transform);
    }

    private void TryCollect(Transform collectorTransform)
    {
        if (isCollected || owningController == null || !owningController.IsCollector(collectorTransform))
        {
            return;
        }

        CollectImmediately();
    }

    public void CollectImmediately()
    {
        if (isCollected || owningController == null)
        {
            return;
        }

        isCollected = true;
        owningController.HandleItemDropCollected(buildingDefinition, quantity);
        Destroy(gameObject);
    }

    public void StopMovement()
    {
        isMovementLocked = true;
    }

    public void ApplyLevelCompletePauseVisual(float grayscaleBlend, float alphaMultiplier)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        Color baseColor = spriteRenderer.color;
        float gray = baseColor.grayscale;
        Color pausedColor = new Color(gray, gray, gray, baseColor.a * Mathf.Clamp01(alphaMultiplier));
        spriteRenderer.color = Color.Lerp(baseColor, pausedColor, Mathf.Clamp01(grayscaleBlend));
    }

    private void ApplyItemVisuals()
    {
        if (spriteRenderer == null || buildingDefinition == null)
        {
            return;
        }

        if (buildingDefinition.BuildingSprite != null)
        {
            spriteRenderer.sprite = buildingDefinition.BuildingSprite;
        }

        spriteRenderer.color = buildingDefinition.BuildingColor;
    }
}
