using UnityEngine;
using System.Collections.Generic;

public class LevelSettings : MonoBehaviour
{
    [System.Serializable]
    public class BrickSpawnOddsEntry
    {
        public BrickTypeData typeData;
        [Min(0f)] public float weight = 1f;
    }

    public static LevelSettings Instance { get; private set; }

    [Header("Next Level")]
    [SerializeField, Min(0)] private int nextLevelRowsToSpawn = 12;
    [SerializeField, Min(0f)] private float nextLevelBrickMoveSpeed = 0.15f;
    [SerializeField] private List<BrickSpawnOddsEntry> nextLevelBrickOdds = new List<BrickSpawnOddsEntry>();

    public int NextLevelRowsToSpawn
    {
        get => nextLevelRowsToSpawn;
        set => nextLevelRowsToSpawn = Mathf.Max(0, value);
    }

    public float NextLevelBrickMoveSpeed
    {
        get => nextLevelBrickMoveSpeed;
        set => nextLevelBrickMoveSpeed = Mathf.Max(0f, value);
    }

    public IReadOnlyList<BrickSpawnOddsEntry> NextLevelBrickOdds => nextLevelBrickOdds;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetNextLevelRowsToSpawn(int rows)
    {
        NextLevelRowsToSpawn = rows;
    }

    public void SetNextLevelBrickMoveSpeed(float speed)
    {
        NextLevelBrickMoveSpeed = speed;
    }

    public void SetBrickOdds(List<BrickSpawnOddsEntry> brickOdds)
    {
        nextLevelBrickOdds.Clear();

        if (brickOdds == null)
        {
            return;
        }

        for (int i = 0; i < brickOdds.Count; i++)
        {
            BrickSpawnOddsEntry source = brickOdds[i];
            if (source == null || source.typeData == null || source.weight <= 0f)
            {
                continue;
            }

            nextLevelBrickOdds.Add(new BrickSpawnOddsEntry
            {
                typeData = source.typeData,
                weight = source.weight
            });
        }
    }
}
