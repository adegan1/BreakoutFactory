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
    [SerializeField, Min(0f)] private float rowsMaxBonusMultiplier = 1.5f;
    [SerializeField, Min(0f)] private float rowsGrowthRate = 0.14f;
    [SerializeField] private bool scaleBrickHealthByCurrentLevel = true;
    [SerializeField, Min(0f)] private float brickHealthMaxBonusMultiplier = 2f;
    [SerializeField, Min(0f)] private float brickHealthGrowthRate = 0.1f;
    [SerializeField] private bool scaleBrickSpeedByCurrentLevel = true;
    [SerializeField, Min(0f)] private float brickSpeedMaxBonusMultiplier = 1f;
    [SerializeField, Min(0f)] private float brickSpeedGrowthRate = 0.12f;

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
        get => ResolveBrickMoveSpeed();
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
        CopyBrickOdds(defaultNextLevelBrickOdds, nextLevelBrickOdds);
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
        CopyBrickOdds(brickOdds, nextLevelBrickOdds);
    }

    private void CaptureDefaults()
    {
        defaultNextLevelRowsToSpawn = nextLevelRowsToSpawn;
        defaultNextLevelBrickMoveSpeed = nextLevelBrickMoveSpeed;
        defaultNextLevelBrickHealth = nextLevelBrickHealth;
        CopyBrickOdds(nextLevelBrickOdds, defaultNextLevelBrickOdds);
    }

    private static void CopyBrickOdds(List<BrickSpawnOddsEntry> source, List<BrickSpawnOddsEntry> destination)
    {
        destination.Clear();
        if (source == null)
        {
            return;
        }

        for (int i = 0; i < source.Count; i++)
        {
            BrickSpawnOddsEntry entry = source[i];
            if (!IsValidBrickOddsEntry(entry))
            {
                continue;
            }

            destination.Add(new BrickSpawnOddsEntry
            {
                typeData = entry.typeData,
                weight = entry.weight
            });
        }
    }

    private static bool IsValidBrickOddsEntry(BrickSpawnOddsEntry entry)
    {
        return entry != null && entry.typeData != null && entry.weight > 0f;
    }

    private int ResolveRowsToSpawn()
    {
        int baseRows = Mathf.Max(0, nextLevelRowsToSpawn);
        if (!scaleRowsByCurrentLevel)
        {
            return baseRows;
        }

        int level = GetCurrentPlayerLevel();
        float scaledRows = baseRows * EvaluateExponentialAssociationMultiplier(
            level,
            rowsMaxBonusMultiplier,
            rowsGrowthRate);
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
        float scaledHealth = baseHealth * EvaluateExponentialAssociationMultiplier(
            level,
            brickHealthMaxBonusMultiplier,
            brickHealthGrowthRate);
        return Mathf.Max(1, Mathf.FloorToInt(scaledHealth));
    }

    private float ResolveBrickMoveSpeed()
    {
        float baseSpeed = Mathf.Max(0f, nextLevelBrickMoveSpeed);
        if (!scaleBrickSpeedByCurrentLevel)
        {
            return baseSpeed;
        }

        int level = GetCurrentPlayerLevel();
        float scaledSpeed = baseSpeed * EvaluateExponentialAssociationMultiplier(
            level,
            brickSpeedMaxBonusMultiplier,
            brickSpeedGrowthRate);
        return Mathf.Max(0f, scaledSpeed);
    }

    private static float EvaluateExponentialAssociationMultiplier(int level, float maxBonusMultiplier, float growthRate)
    {
        int clampedLevel = Mathf.Max(1, level);
        float clampedBonus = Mathf.Max(0f, maxBonusMultiplier);
        float clampedGrowth = Mathf.Max(0f, growthRate);

        // Exponential association curve: starts at 1x and asymptotically approaches (1 + maxBonus).
        // multiplier = 1 + maxBonus * (1 - e^(-growth * (level - 1)))
        float association = 1f - Mathf.Exp(-clampedGrowth * (clampedLevel - 1));
        return 1f + clampedBonus * association;
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
