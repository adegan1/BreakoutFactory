using UnityEngine;

[CreateAssetMenu(fileName = "New Brick Type", menuName = "Breakout/Brick Type Data")]
public class BrickTypeData : ScriptableObject
{
    [Header("Core Properties")]
    [SerializeField] private int hitPoints = 1;
    [SerializeField] private Color displayColor = Color.white;
    [SerializeField] private int scoreValue = 10;

    [Header("Weaknesses (Ball Elements)")]
    [SerializeField] private string[] weaknessElements = new string[0];

    public int HitPoints => hitPoints;
    public Color DisplayColor => displayColor;
    public int ScoreValue => scoreValue;
    public string[] WeaknessElements => weaknessElements;

    public bool IsWeakToElement(string element)
    {
        if (string.IsNullOrEmpty(element))
        {
            return false;
        }

        for (int i = 0; i < weaknessElements.Length; i++)
        {
            if (weaknessElements[i] == element)
            {
                return true;
            }
        }

        return false;
    }
}
