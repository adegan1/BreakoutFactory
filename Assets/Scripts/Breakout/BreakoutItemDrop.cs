using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class BreakoutItemDrop : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Drop Animation")]
    [SerializeField] private float rotationSpeed = 360f; // degrees per second
    [SerializeField] private float bobAmplitude = 0.3f; // how far up/down to bob
    [SerializeField] private float bobFrequency = 2f; // how fast to bob
    [SerializeField] private float scalePulseAmplitude = 0.1f; // scale pulse intensity

    private BreakoutGameController owningController;
    private BuildingDefinition buildingDefinition;
    private int quantity;
    private float fallSpeed;
    private float bottomKillY;
    private bool isCollected;
    private bool isMovementLocked;
    private Vector3 startPosition;
    private float elapsedTime;
    private Vector3 initialScale;
    private SpriteStacker spriteStacker;

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

        startPosition = transform.position;
        elapsedTime = 0f;
        initialScale = transform.localScale;
        spriteStacker = GetComponent<SpriteStacker>();
    }

    private void Update()
    {
        if (isCollected || isMovementLocked)
        {
            return;
        }

        elapsedTime += Time.deltaTime;

        // Spin
        transform.rotation *= Quaternion.Euler(0f, 0f, rotationSpeed * Time.deltaTime);

        // Fall with bob and scale pulse
        float fallDistance = Mathf.Max(0f, fallSpeed) * elapsedTime;
        float bobOffset = Mathf.Sin(elapsedTime * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
        float scalePulse = (1f - scalePulseAmplitude) + Mathf.Sin(elapsedTime * bobFrequency * Mathf.PI * 2f - Mathf.PI / 2f) * scalePulseAmplitude;

        transform.position = startPosition + new Vector3(0f, -fallDistance + bobOffset, 0f);
        transform.localScale = initialScale * scalePulse;

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
        transform.rotation = Quaternion.identity; // Reset rotation when paused
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

        spriteStacker?.ApplyPauseVisual(grayscaleBlend, alphaMultiplier);
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

        spriteStacker?.Refresh(spriteRenderer.sprite, spriteRenderer.color);
    }
}
