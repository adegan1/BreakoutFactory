using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BallController : MonoBehaviour
{
    [Header("Type")]
    [SerializeField] private BallTypeData typeData;

    [Header("Movement")]
    [SerializeField] private float speed = 8f;
    [SerializeField] private float minimumVerticalDirection = 0.2f;

    [Header("Anti-Stuck")]
    [SerializeField] private bool ignoreOtherBallCollisions = false;
    [SerializeField] private float stuckRecoveryDelay = 0.2f;
    [SerializeField] private float minimumMovementPerFixedStep = 0.001f;
    [SerializeField] private float unstuckNudgeDistance = 0.05f;

    [Header("Paddle Bounce")]
    [SerializeField] private float paddleHorizontalInfluence = 0.7f;

    [Header("Loss Rules")]
    [SerializeField] private float bottomKillY = -6f;

    private Rigidbody2D rb;
    private Collider2D ballCollider;
    private SpriteRenderer spriteRenderer;
    private bool launched;
    private bool hasBeenLost;
    private bool collideWithBricks = true;
    private Vector2 travelDirection = Vector2.up;
    private Vector2 lastVelocity;
    private Vector2 previousPosition;
    private float stagnantTime;

    public System.Action<BallController> BallLost;

    public bool IsLaunched => launched;
    public BallTypeData TypeData => typeData;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ballCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        previousPosition = rb.position;
    }

    private void Start()
    {
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        ApplyTypeData();
    }

    private void Update()
    {
        if (!hasBeenLost && transform.position.y < bottomKillY)
        {
            LoseBall();
        }
    }

    private void FixedUpdate()
    {
        if (!launched || hasBeenLost)
        {
            return;
        }

        if (!collideWithBricks)
        {
            DamageBricksAlongPath(previousPosition, rb.position);
        }

        UpdateStagnationState();

        Vector2 currentVelocity = rb.linearVelocity;

        if (currentVelocity.sqrMagnitude > 0.001f)
        {
            travelDirection = currentVelocity.normalized;
            lastVelocity = currentVelocity;
        }
        else if (travelDirection.sqrMagnitude < 0.001f)
        {
            travelDirection = Vector2.up;
        }

        ApplyVelocity();
    }

    public void Launch(Vector2 direction)
    {
        if (launched)
        {
            return;
        }

        launched = true;
        SetTravelDirection(direction, defaultYSign: 1f);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.contactCount == 0)
        {
            return;
        }

        if (ignoreOtherBallCollisions && collision.gameObject.TryGetComponent<BallController>(out _))
        {
            if (ballCollider != null)
            {
                Physics2D.IgnoreCollision(ballCollider, collision.collider, true);
            }

            return;
        }

        if (collision.gameObject.TryGetComponent<BrickController>(out BrickController brick))
        {
            if (!collideWithBricks)
            {
                brick.ApplyBallHit(this);
                if (ballCollider != null)
                {
                    Physics2D.IgnoreCollision(ballCollider, collision.collider, true);
                }

                return;
            }
        }

        if (collision.gameObject.CompareTag("Paddle"))
        {
            ContactPoint2D paddleContact = collision.GetContact(0);
            float halfWidth = Mathf.Max(paddleContact.collider.bounds.extents.x, 0.01f);
            float offset = transform.position.x - paddleContact.collider.bounds.center.x;
            float normalizedOffset = Mathf.Clamp(offset / halfWidth, -1f, 1f);
            float horizontal = normalizedOffset * paddleHorizontalInfluence;
            Vector2 bounceDirection = new Vector2(horizontal, 1f);

            if (Mathf.Abs(bounceDirection.y) < minimumVerticalDirection)
            {
                bounceDirection.y = minimumVerticalDirection;
            }

            SetTravelDirection(bounceDirection, defaultYSign: 1f);
            return;
        }

        ContactPoint2D contact = collision.GetContact(0);
        Vector2 incoming = lastVelocity.sqrMagnitude > 0.001f ? lastVelocity.normalized : travelDirection;
        Vector2 reflected = Vector2.Reflect(incoming, contact.normal);

        if (Mathf.Abs(reflected.y) < minimumVerticalDirection)
        {
            float ySign = Mathf.Sign(reflected.y == 0f ? -contact.normal.y : reflected.y);
            reflected.y = ySign * minimumVerticalDirection;
        }

        SetTravelDirection(reflected, defaultYSign: -contact.normal.y);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("BottomBoundary"))
        {
            LoseBall();
            return;
        }

        if (!collideWithBricks && other.TryGetComponent<BrickController>(out BrickController brick))
        {
            brick.ApplyBallHit(this);
        }
    }

    private void LoseBall()
    {
        if (hasBeenLost)
        {
            return;
        }

        hasBeenLost = true;
        BallLost?.Invoke(this);
        Destroy(gameObject);
    }

    private void SetTravelDirection(Vector2 direction, float defaultYSign)
    {
        travelDirection = NormalizeDirection(direction, defaultYSign);
        ApplyVelocity();
    }

    private Vector2 NormalizeDirection(Vector2 direction, float defaultYSign)
    {
        Vector2 normalizedDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.up;

        if (Mathf.Abs(normalizedDirection.y) < minimumVerticalDirection)
        {
            float ySign = Mathf.Sign(normalizedDirection.y == 0f ? defaultYSign : normalizedDirection.y);
            if (Mathf.Approximately(ySign, 0f))
            {
                ySign = 1f;
            }

            normalizedDirection.y = ySign * minimumVerticalDirection;
            normalizedDirection.Normalize();
        }

        return normalizedDirection;
    }

    private void ApplyVelocity()
    {
        rb.linearVelocity = travelDirection * speed;
        lastVelocity = rb.linearVelocity;
    }

    private void UpdateStagnationState()
    {
        float movedDistance = (rb.position - previousPosition).magnitude;
        if (movedDistance <= minimumMovementPerFixedStep)
        {
            stagnantTime += Time.fixedDeltaTime;
            if (stagnantTime >= stuckRecoveryDelay)
            {
                ForceUnstuck();
                stagnantTime = 0f;
            }
        }
        else
        {
            stagnantTime = 0f;
        }

        previousPosition = rb.position;
    }

    private void ForceUnstuck()
    {
        float randomHorizontal = Random.Range(-1f, 1f);
        float ySign = Mathf.Sign(travelDirection.y == 0f ? 1f : travelDirection.y);
        Vector2 recoveryDirection = new Vector2(randomHorizontal, ySign);
        Vector2 normalizedRecovery = NormalizeDirection(recoveryDirection, ySign);

        rb.position += normalizedRecovery * unstuckNudgeDistance;
        SetTravelDirection(normalizedRecovery, ySign);
    }

    public void SetTypeData(BallTypeData newTypeData)
    {
        typeData = newTypeData;
        ApplyTypeData();
    }

    private void ApplyTypeData()
    {
        if (typeData == null)
        {
            collideWithBricks = true;
            return;
        }

        speed = Mathf.Max(0f, typeData.MovementSpeed);
        collideWithBricks = typeData.CollideWithBricks;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = typeData.DisplayColor;
        }
    }

    private void DamageBricksAlongPath(Vector2 from, Vector2 to)
    {
        if ((to - from).sqrMagnitude < 0.000001f)
        {
            return;
        }

        RaycastHit2D[] brickHits = Physics2D.LinecastAll(from, to);
        for (int i = 0; i < brickHits.Length; i++)
        {
            Collider2D hitCollider = brickHits[i].collider;
            if (hitCollider == null)
            {
                continue;
            }

            if (hitCollider.TryGetComponent<BrickController>(out BrickController brick))
            {
                brick.ApplyBallHit(this);

                if (ballCollider != null)
                {
                    Physics2D.IgnoreCollision(ballCollider, hitCollider, true);
                }
            }
        }
    }
}
