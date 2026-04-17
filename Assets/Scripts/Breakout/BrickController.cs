using UnityEngine;

public class BrickController : MonoBehaviour
{
    [SerializeField] private BrickTypeData typeData;

    [Header("Spawn Animation")]
    [SerializeField] private float growthSpeed = 6f;

    [Header("Movement")]
    [SerializeField] private bool moveDownward;
    [SerializeField] private float downwardSpeed;

    private int currentHitPoints;
    private SpriteRenderer spriteRenderer;
    private Vector3 targetScale;
    private bool isGrowing;

    public int CurrentHitPoints => currentHitPoints;
    public BrickTypeData TypeData => typeData;

    private void Awake()
    {
        targetScale = transform.localScale;
        transform.localScale = new Vector3(0f, 0f, 1f);
        isGrowing = true;

        spriteRenderer = GetComponent<SpriteRenderer>();
        ApplyTypeData();
    }

    private void Update()
    {
        UpdateSpawnGrowth();

        if (!moveDownward || downwardSpeed <= 0f)
        {
            return;
        }

        transform.position += Vector3.down * downwardSpeed * Time.deltaTime;
    }

    private void UpdateSpawnGrowth()
    {
        if (!isGrowing)
        {
            return;
        }

        if (growthSpeed <= 0f)
        {
            transform.localScale = targetScale;
            isGrowing = false;
            return;
        }

        transform.localScale = Vector3.MoveTowards(transform.localScale, targetScale, growthSpeed * Time.deltaTime);
        if (transform.localScale == targetScale)
        {
            isGrowing = false;
        }
    }

    public void SetDownwardMotion(bool enabled, float speed)
    {
        moveDownward = enabled;
        SetDownwardSpeed(speed);
    }

    public void SetDownwardSpeed(float speed)
    {
        downwardSpeed = Mathf.Max(0f, speed);
    }

    public void SetTypeData(BrickTypeData newTypeData)
    {
        typeData = newTypeData;
        ApplyTypeData();
    }

    private void ApplyTypeData()
    {
        if (typeData == null)
        {
            Debug.LogWarning("BrickTypeData not assigned on " + gameObject.name);
            currentHitPoints = 1;
            return;
        }

        currentHitPoints = typeData.HitPoints;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = typeData.DisplayColor;
        }
    }

    public void ApplyBallHit(BallController ball)
    {
        if (ball == null)
        {
            return;
        }

        HandleBallHit(ball);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.TryGetComponent<BallController>(out BallController ball))
        {
            return;
        }

        HandleBallHit(ball);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent<BallController>(out BallController ball))
        {
            return;
        }

        HandleBallHit(ball);
    }

    protected virtual void HandleBallHit(BallController ball)
    {
        int damage = GetDamageFromBall(ball);
        ApplyDamage(damage);
    }

    protected virtual int GetDamageFromBall(BallController ball)
    {
        if (typeData == null || ball == null || ball.TypeData == null)
        {
            return 1;
        }

        return ball.TypeData.IsStrongAgainst(typeData.Type) ? 2 : 1;
    }

    protected virtual void ApplyDamage(int amount)
    {
        currentHitPoints -= Mathf.Max(0, amount);
        if (currentHitPoints <= 0)
        {
            OnBrickDestroyed();
            Destroy(gameObject);
        }
    }

    protected virtual void OnBrickDestroyed()
    {
    }
}
