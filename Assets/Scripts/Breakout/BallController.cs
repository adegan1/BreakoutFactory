using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BallController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 8f;
    [SerializeField] private float minimumVerticalDirection = 0.2f;

    [Header("Loss Rules")]
    [SerializeField] private float bottomKillY = -6f;

    private Rigidbody2D rb;
    private bool launched;
    private bool hasBeenLost;

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
        if (currentVelocity.sqrMagnitude < 0.001f)
        {
            currentVelocity = Vector2.up;
        }

        rb.linearVelocity = currentVelocity.normalized * speed;
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
        rb.linearVelocity = launchDirection * speed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Paddle"))
        {
            return;
        }

        ContactPoint2D contact = collision.GetContact(0);
        float offset = transform.position.x - contact.collider.bounds.center.x;
        Vector2 bounceDirection = new Vector2(offset, Mathf.Abs(rb.linearVelocity.y));

        if (Mathf.Abs(bounceDirection.y) < minimumVerticalDirection)
        {
            bounceDirection.y = minimumVerticalDirection;
        }

        rb.linearVelocity = bounceDirection.normalized * speed;
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
