using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class BrickGridSpawner : MonoBehaviour
{
    [System.Serializable]
    private class WeightedBrickEntry
    {
        public BrickTypeData typeData;
        [Min(0f)] public float weight = 1f;
    }

    [Header("References")]
    [SerializeField] private BrickController brickPrefab;

    [Header("Layout")]
    [SerializeField] private int columns = 8;
    [SerializeField] private int initialRows = 5;
    [SerializeField] private Vector2 spacing = new Vector2(1.2f, 0.6f);
    [SerializeField] private Vector2 startOffset = new Vector2(-4.2f, 3f);

    [Header("Row Spawning")]
    [SerializeField] private bool spawnRowsOverTime = true;
    [SerializeField] private bool autoSpawnTriggerFromSpacing = true;
    [SerializeField] private float topRowSpawnTriggerY = 2.4f;
    [SerializeField, Min(0)] private int totalRowsToSpawn = 12;
    [SerializeField] private bool randomizeEmptySlots;
    [SerializeField, Range(0f, 1f)] private float emptySlotChance = 0f;

    [Header("External Settings")]
    [SerializeField] private LevelSettings levelSettings;

    [Header("Downward Movement")]
    [SerializeField] private bool moveDownward = true;
    [SerializeField] private float downwardSpeed = 0.15f;
    [SerializeField] private float bottomDangerY = -4.5f;

    [Header("Events")]
    [SerializeField] private UnityEvent onBricksReachedBottom;

    private List<WeightedBrickEntry> weightedBrickPrefabs = new List<WeightedBrickEntry>();
    private bool bottomEventFired;
    private int rowsSpawned;

    public int RowsSpawned => rowsSpawned;
    public int TotalRowsToSpawn => totalRowsToSpawn;

    private void Start()
    {
        rowsSpawned = 0;

        if (brickPrefab == null)
        {
            Debug.LogError("Brick prefab is not assigned on BrickGridSpawner.");
            return;
        }

        ResolveAndApplyLevelSettings();

        if (!HasSpawnableBrickType())
        {
            Debug.LogError("No valid weighted brick types assigned on BrickGridSpawner.");
            return;
        }

        if (columns <= 0 || initialRows < 0)
        {
            return;
        }

        ConfigureSpawnTrigger();

        SpawnInitialRows();
    }

    private void Update()
    {
        TrySpawnNextRowByTopPosition();

        CheckBottomDanger();
    }

    private void SpawnInitialRows()
    {
        for (int row = 0; row < initialRows && CanSpawnMoreRows(); row++)
        {
            SpawnSingleRow(row);
            rowsSpawned++;
        }
    }

    private bool CanSpawnMoreRows()
    {
        return rowsSpawned < totalRowsToSpawn;
    }

    private void TrySpawnNextRowByTopPosition()
    {
        if (!spawnRowsOverTime || !CanSpawnMoreRows())
        {
            return;
        }

        if (transform.childCount == 0)
        {
            SpawnSingleRow(0);
            rowsSpawned++;
            return;
        }

        float topRowY = GetTopMostBrickY();
        if (topRowY <= topRowSpawnTriggerY)
        {
            SpawnSingleRow(0);
            rowsSpawned++;
        }
    }

    private void SpawnSingleRow(int rowIndex)
    {
        Vector3 origin = transform.position + (Vector3)startOffset;
        float rowY = origin.y - rowIndex * spacing.y;

        for (int col = 0; col < columns; col++)
        {
            if (randomizeEmptySlots && Random.value < emptySlotChance)
            {
                continue;
            }

            BrickTypeData chosenType = ChooseBrickType();
            if (chosenType == null)
            {
                continue;
            }

            Vector3 position = new Vector3(origin.x + col * spacing.x, rowY, origin.z);
            BrickController spawnedBrick = Instantiate(brickPrefab, position, Quaternion.identity, transform);
            if (spawnedBrick != null)
            {
                spawnedBrick.SetDownwardMotion(moveDownward, downwardSpeed);
                spawnedBrick.SetTypeData(chosenType);
            }
        }
    }

    private BrickTypeData ChooseBrickType()
    {
        float totalWeight = 0f;
        for (int i = 0; i < weightedBrickPrefabs.Count; i++)
        {
            WeightedBrickEntry entry = weightedBrickPrefabs[i];
            if (entry != null && entry.typeData != null && entry.weight > 0f)
            {
                totalWeight += entry.weight;
            }
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float roll = Random.value * totalWeight;
        float cumulative = 0f;
        for (int i = 0; i < weightedBrickPrefabs.Count; i++)
        {
            WeightedBrickEntry entry = weightedBrickPrefabs[i];
            if (entry == null || entry.typeData == null || entry.weight <= 0f)
            {
                continue;
            }

            cumulative += entry.weight;
            if (roll <= cumulative)
            {
                return entry.typeData;
            }
        }

        return null;
    }

    private bool HasSpawnableBrickType()
    {
        for (int i = 0; i < weightedBrickPrefabs.Count; i++)
        {
            WeightedBrickEntry entry = weightedBrickPrefabs[i];
            if (entry != null && entry.typeData != null && entry.weight > 0f)
            {
                return true;
            }
        }

        return false;
    }

    private void CheckBottomDanger()
    {
        if (bottomEventFired)
        {
            return;
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.position.y <= bottomDangerY)
            {
                bottomEventFired = true;
                onBricksReachedBottom?.Invoke();
                return;
            }
        }
    }

    private void ResolveAndApplyLevelSettings()
    {
        if (levelSettings == null)
        {
            levelSettings = LevelSettings.Instance;
        }

        if (levelSettings == null)
        {
            levelSettings = FindAnyObjectByType<LevelSettings>();
        }

        if (levelSettings == null)
        {
            return;
        }

        totalRowsToSpawn = Mathf.Max(0, levelSettings.NextLevelRowsToSpawn);
        downwardSpeed = Mathf.Max(0f, levelSettings.NextLevelBrickMoveSpeed);
        ApplyOddsFromSettings(levelSettings.NextLevelBrickOdds);
    }

    private void ConfigureSpawnTrigger()
    {
        if (!autoSpawnTriggerFromSpacing)
        {
            return;
        }

        float topSpawnY = transform.position.y + startOffset.y;
        topRowSpawnTriggerY = topSpawnY - Mathf.Abs(spacing.y);
    }

    private float GetTopMostBrickY()
    {
        float highestY = float.NegativeInfinity;
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.position.y > highestY)
            {
                highestY = child.position.y;
            }
        }

        return highestY;
    }

    private void ApplyOddsFromSettings(IReadOnlyList<LevelSettings.BrickSpawnOddsEntry> odds)
    {
        if (odds == null || odds.Count == 0)
        {
            return;
        }

        List<WeightedBrickEntry> mappedEntries = new List<WeightedBrickEntry>();
        for (int i = 0; i < odds.Count; i++)
        {
            LevelSettings.BrickSpawnOddsEntry odd = odds[i];
            if (odd == null || odd.typeData == null || odd.weight <= 0f)
            {
                continue;
            }

            mappedEntries.Add(new WeightedBrickEntry
            {
                typeData = odd.typeData,
                weight = odd.weight
            });
        }

        if (mappedEntries.Count == 0)
        {
            return;
        }

        weightedBrickPrefabs = mappedEntries;
    }
}
