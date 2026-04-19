using UnityEngine;

[CreateAssetMenu(fileName = "New Brick Type", menuName = "Breakout/Brick Type Data")]
public class BrickTypeData : ScriptableObject
{
    // Core Properties
    [SerializeField] private int hitPoints = 1;
    [SerializeField] private Color displayColor = Color.white;
    [SerializeField] private int scoreValue = 10;
    [SerializeField] private bool flammable = false;
    [SerializeField] private bool fireResistant = false;

    // Type
    [SerializeField] private BallTypeData.BallElement type = BallTypeData.BallElement.Basic;

    public int HitPoints => hitPoints;
    public Color DisplayColor => displayColor;
    public int ScoreValue => scoreValue;
    public bool Flammable => flammable;
    public bool FireResistant => fireResistant;
    public BallTypeData.BallElement Type => type;
}
