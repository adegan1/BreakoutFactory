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
    [SerializeField, Min(1)] private int nextLevelBrickHealth = 1;
    [SerializeField] private List<BrickSpawnOddsEntry> nextLevelBrickOdds = new List<BrickSpawnOddsEntry>();

    [Header("Level Scaling")]
    [SerializeField] private bool scaleRowsByCurrentLevel = true;
    [SerializeField, Min(0f)] private float rowsPerLevelMultiplier = 0.5f;
    [SerializeField] private bool scaleBrickHealthByCurrentLevel = true;
    [SerializeField, Min(0f)] private float brickHealthPerLevelMultiplier = 0.25f;

    private int defaultNextLevelRowsToSpawn;
    private float defaultNextLevelBrickMoveSpeed;
    private int defaultNextLevelBrickHealth;
    private readonly List<BrickSpawnOddsEntry> defaultNextLevelBrickOdds = new List<BrickSpawnOddsEntry>();

    public int NextLevelRowsToSpawn
    {
        get => ResolveRowsToSpawn();
        set => nextLevelRowsToSpawn = Mathf.Max(0, value);
    }

    public float NextLevelBrickMoveSpeed
    {
        get => nextLevelBrickMoveSpeed;
        set => nextLevelBrickMoveSpeed = Mathf.Max(0f, value);
    }

    public int NextLevelBrickHealth
    {
        get => ResolveBrickHealth();
        set => nextLevelBrickHealth = Mathf.Max(1, value);
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
        CaptureDefaults();
        DontDestroyOnLoad(gameObject);
    }

    public void ResetToDefaults()
    {
        nextLevelRowsToSpawn = Mathf.Max(0, defaultNextLevelRowsToSpawn);
        nextLevelBrickMoveSpeed = Mathf.Max(0f, defaultNextLevelBrickMoveSpeed);
        nextLevelBrickHealth = Mathf.Max(1, defaultNextLevelBrickHealth);
        nextLevelBrickOdds.Clear();

        for (int i = 0; i < defaultNextLevelBrickOdds.Count; i++)
        {
            BrickSpawnOddsEntry source = defaultNextLevelBrickOdds[i];
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

    public void SetNextLevelRowsToSpawn(int rows)
    {
        NextLevelRowsToSpawn = rows;
    }

    public void SetNextLevelBrickMoveSpeed(float speed)
    {
        NextLevelBrickMoveSpeed = speed;
    }

    public void SetNextLevelBrickHealth(int health)
    {
        NextLevelBrickHealth = health;
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

    private void CaptureDefaults()
    {
        defaultNextLevelRowsToSpawn = nextLevelRowsToSpawn;
        defaultNextLevelBrickMoveSpeed = nextLevelBrickMoveSpeed;
        defaultNextLevelBrickHealth = nextLevelBrickHealth;
        defaultNextLevelBrickOdds.Clear();

        for (int i = 0; i < nextLevelBrickOdds.Count; i++)
        {
            BrickSpawnOddsEntry source = nextLevelBrickOdds[i];
            if (source == null || source.typeData == null || source.weight <= 0f)
            {
                continue;
            }

            defaultNextLevelBrickOdds.Add(new BrickSpawnOddsEntry
            {
                typeData = source.typeData,
                weight = source.weight
            });
        }
    }

    private int ResolveRowsToSpawn()
    {
        int baseRows = Mathf.Max(0, nextLevelRowsToSpawn);
        if (!scaleRowsByCurrentLevel)
        {
            return baseRows;
        }

        int level = GetCurrentPlayerLevel();
        float scaledRows = baseRows * level * Mathf.Max(0f, rowsPerLevelMultiplier);
        return Mathf.Max(0, Mathf.FloorToInt(scaledRows));
    }

    private int ResolveBrickHealth()
    {
        int baseHealth = Mathf.Max(1, nextLevelBrickHealth);
        if (!scaleBrickHealthByCurrentLevel)
        {
            return baseHealth;
        }

        int level = GetCurrentPlayerLevel();
        float scaledHealth = baseHealth * level * Mathf.Max(0f, brickHealthPerLevelMultiplier);
        return Mathf.Max(1, Mathf.CeilToInt(scaledHealth));
    }

    private static int GetCurrentPlayerLevel()
    {
        if (!PlayerStats.HasInstance)
        {
            return 1;
        }

        return Mathf.Max(1, PlayerStats.Instance.CurrentLevel);
    }
}
