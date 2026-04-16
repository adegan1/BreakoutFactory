using UnityEngine;

public class BrickController : MonoBehaviour
{
    [SerializeField] private int hitPoints = 1;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.TryGetComponent<BallController>(out _))
        {
            return;
        }

        hitPoints--;
        if (hitPoints <= 0)
        {
            Destroy(gameObject);
        }
    }
}
