using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BallController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 8f;
    [SerializeField] private float minimumVerticalDirection = 0.2f;

    [Header("Paddle Bounce")]
    [SerializeField] private float paddleHorizontalInfluence = 0.7f;

    [Header("Loss Rules")]
    [SerializeField] private float bottomKillY = -6f;

    private Rigidbody2D rb;
    private bool launched;
    private bool hasBeenLost;
    private Vector2 travelDirection = Vector2.up;
    private Vector2 lastVelocity;

    public System.Action<BallController> BallLost;

    public bool IsLaunched => launched;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
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

        rb.linearVelocity = travelDirection * speed;
    }

    public void Launch(Vector2 direction)
    {
        if (launched)
        {
            return;
        }

        Vector2 launchDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.up;
        if (Mathf.Abs(launchDirection.y) < minimumVerticalDirection)
        {
            launchDirection.y = Mathf.Sign(launchDirection.y == 0f ? 1f : launchDirection.y) * minimumVerticalDirection;
            launchDirection.Normalize();
        }

        launched = true;
        travelDirection = launchDirection;
        rb.linearVelocity = travelDirection * speed;
        lastVelocity = rb.linearVelocity;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.contactCount == 0)
        {
            return;
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

            travelDirection = bounceDirection.normalized;
            rb.linearVelocity = travelDirection * speed;
            lastVelocity = rb.linearVelocity;
            return;
        }

        ContactPoint2D contact = collision.GetContact(0);
        Vector2 incoming = lastVelocity.sqrMagnitude > 0.001f ? lastVelocity.normalized : travelDirection;
        Vector2 reflected = Vector2.Reflect(incoming, contact.normal);

        if (Mathf.Abs(reflected.y) < minimumVerticalDirection)
        {
            float ySign = Mathf.Sign(reflected.y == 0f ? -contact.normal.y : reflected.y);
            reflected.y = ySign * minimumVerticalDirection;
            reflected.Normalize();
        }

        travelDirection = reflected.normalized;
        rb.linearVelocity = travelDirection * speed;
        lastVelocity = rb.linearVelocity;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("BottomBoundary"))
        {
            LoseBall();
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
}
