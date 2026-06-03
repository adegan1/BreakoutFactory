using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class BreakoutScrapDrop : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Drop Animation")]
    [SerializeField] private float rotationSpeed = 360f;
    [SerializeField] private float bobAmplitude = 0.3f;
    [SerializeField] private float bobFrequency = 2f;
    [SerializeField] private float scalePulseAmplitude = 0.1f;

    private BreakoutGameController owningController;
    private int scrapAmount;
    private float fallSpeed;
    private float bottomKillY;
    private bool isCollected;
    private bool isMovementLocked;
    private Vector3 startPosition;
    private float elapsedTime;
    private Vector3 initialScale;

    public int ScrapAmount => scrapAmount;

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
    }

    private void Update()
    {
        if (isCollected || isMovementLocked)
        {
            return;
        }

        elapsedTime += Time.deltaTime;

        transform.rotation *= Quaternion.Euler(0f, 0f, rotationSpeed * Time.deltaTime);

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

    public void Initialize(BreakoutGameController controller, int amount, float speed, float killY)
    {
        owningController = controller;
        scrapAmount = Mathf.Max(1, amount);
        fallSpeed = Mathf.Max(0f, speed);
        bottomKillY = killY;
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
        if (isCollected)
        {
            return;
        }

        isCollected = true;
        BreakoutSoundController.PlayScrapPickupSfx();

        if (PlayerStats.HasInstance)
        {
            PlayerStats.Instance.AddScrap(scrapAmount);
        }

        if (owningController != null)
        {
            owningController.HandleScrapDropCollected(scrapAmount, transform.position);
        }

        Destroy(gameObject);
    }

    public void StopMovement()
    {
        isMovementLocked = true;
        transform.rotation = Quaternion.identity;
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
}
