using UnityEngine;

public class BrickController : MonoBehaviour
{
    [SerializeField] private int hitPoints = 1;
    [SerializeField] private bool moveDownward;
    [SerializeField] private float downwardSpeed;

    public int CurrentHitPoints => hitPoints;

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
        hitPoints -= Mathf.Max(0, amount);
        if (hitPoints <= 0)
        {
            OnBrickDestroyed();
            Destroy(gameObject);
        }
    }

    protected virtual void OnBrickDestroyed()
    {
    }
}
