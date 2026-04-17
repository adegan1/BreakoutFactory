using UnityEngine;

[CreateAssetMenu(fileName = "New Ball Type", menuName = "Breakout/Ball Type Data")]
public class BallTypeData : ScriptableObject
{
    public enum BallElement
    {
        Basic,
        Fire,
        Water,
        Lightning,
        Life,
        Earth,
        Wind
    }

    [Header("Visual")]
    [SerializeField] private Color displayColor = Color.white;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float movementSpeed = 8f;

    [Header("Brick Interaction")]
    [SerializeField] private bool collideWithBricks = true;

    [Header("Elements")]
    [SerializeField] private BallElement[] elements = new BallElement[] { BallElement.Basic };

    public Color DisplayColor => displayColor;
    public float MovementSpeed => movementSpeed;
    public bool CollideWithBricks => collideWithBricks;
    public BallElement[] Elements => elements;
}
