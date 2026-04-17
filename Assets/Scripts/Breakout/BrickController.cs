using UnityEngine;

public class BrickController : MonoBehaviour
{
    [SerializeField] private BrickTypeData typeData;
    [SerializeField] private bool moveDownward;
    [SerializeField] private float downwardSpeed;

    private int currentHitPoints;
    private SpriteRenderer spriteRenderer;

    public int CurrentHitPoints => currentHitPoints;
    public BrickTypeData TypeData => typeData;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        ApplyTypeData();
    }

    private void Update()
    {
        if (!moveDownward || downwardSpeed <= 0f)
        {
            return;
        }

        transform.position += Vector3.down * downwardSpeed * Time.deltaTime;
    }

    public void SetDownwardMotion(bool enabled, float speed)
    {
        moveDownward = enabled;
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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.TryGetComponent<BallController>(out _))
        {
            return;
        }

        HandleBallHit(collision);
    }

    protected virtual void HandleBallHit(Collision2D collision)
    {
        ApplyDamage(1);
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
