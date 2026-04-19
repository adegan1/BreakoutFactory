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
    private int maxHitPoints;
    private SpriteRenderer spriteRenderer;
    private Vector3 targetScale;
    private bool isGrowing;
    private bool isBurning;
    private int burnDamage;
    private float burnTickInterval;
    private float burnTickTimer;
    private int burnHitsRemaining;

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

        UpdateBurning();

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
            maxHitPoints = 1;
            currentHitPoints = 1;
            UpdateHealthAlpha();
            return;
        }

        maxHitPoints = Mathf.Max(1, typeData.HitPoints);
        currentHitPoints = maxHitPoints;
        ClearBurn();

        if (spriteRenderer != null)
        {
            spriteRenderer.color = typeData.DisplayColor;
        }

        UpdateHealthAlpha();
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

        if (ball.TypeData != null && !ball.TypeData.CollideWithBricks)
        {
            return;
        }

        HandleBallHit(ball);
    }

    protected virtual void HandleBallHit(BallController ball)
    {
        int damage = GetDamageFromBall(ball);
        ApplyDamage(damage);

        if (ball != null && ball.TypeData != null && ball.TypeData.AppliesBurn)
        {
            ApplyBurn(ball.TypeData.BurnDamage, ball.TypeData.BurnTickInterval, ball.TypeData.BurnHitCount);
        }
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

        UpdateHealthAlpha();

        if (currentHitPoints <= 0)
        {
            OnBrickDestroyed();
            Destroy(gameObject);
        }
    }

    private void UpdateHealthAlpha()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        float ratio = Mathf.Clamp01((float)currentHitPoints / Mathf.Max(1, maxHitPoints));
        Color color = spriteRenderer.color;
        color.a = ratio;
        spriteRenderer.color = color;
    }

    protected virtual void OnBrickDestroyed()
    {
    }

    private void UpdateBurning()
    {
        if (!isBurning || burnHitsRemaining <= 0)
        {
            return;
        }

        burnTickTimer -= Time.deltaTime;
        if (burnTickTimer > 0f)
        {
            return;
        }

        burnHitsRemaining--;
        burnTickTimer = burnTickInterval;
        ApplyDamage(GetBurnDamage());

        if (currentHitPoints <= 0)
        {
            return;
        }

        if (burnHitsRemaining <= 0)
        {
            ClearBurn();
        }
    }

    private void ApplyBurn(int damagePerTick, float tickInterval, int hitCount)
    {
        if (typeData != null && typeData.FireResistant)
        {
            return;
        }

        if (hitCount <= 0)
        {
            return;
        }

        isBurning = true;
        burnDamage = Mathf.Max(1, damagePerTick);
        burnTickInterval = Mathf.Max(0.01f, tickInterval);
        burnTickTimer = burnTickInterval;
        burnHitsRemaining = hitCount;
    }

    private void ClearBurn()
    {
        isBurning = false;
        burnDamage = 0;
        burnTickInterval = 0f;
        burnTickTimer = 0f;
        burnHitsRemaining = 0;
    }

    private int GetBurnDamage()
    {
        if (typeData != null && typeData.Flammable)
        {
            return burnDamage * 2;
        }

        return burnDamage;
    }
}
