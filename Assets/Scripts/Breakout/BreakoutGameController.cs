using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class BreakoutGameController : MonoBehaviour
{
    [Serializable]
    private class BuildingDropTableEntry
    {
        [SerializeField] private BuildingDefinition buildingDefinition;
        [SerializeField, Min(0f)] private float weight = 1f;
        [SerializeField, Min(1)] private int minQuantity = 1;
        [SerializeField, Min(1)] private int maxQuantity = 1;

        public BuildingDefinition BuildingDefinition => buildingDefinition;
        public float Weight => Mathf.Max(0f, weight);
        public int MinQuantity => Mathf.Max(1, minQuantity);
        public int MaxQuantity => Mathf.Max(MinQuantity, maxQuantity);
    }

    [Header("References")]
    [SerializeField] private BallController ballPrefab;
    [SerializeField] private Transform paddleTransform;

    [Header("Ball Dispense")]
    [SerializeField] private List<BallTypeData> ballsToDispense = new List<BallTypeData>();
    [SerializeField] private Vector2 initialLaunchDirection = new Vector2(0.6f, 1f);
    [SerializeField] private Vector2 spawnOffset = new Vector2(0f, 0.6f);

    [Header("Brick Item Drops")]
    [SerializeField] private bool enableBrickDrops = true;
    [SerializeField, Range(0f, 1f)] private float brickDropChance = 0.2f;
    [SerializeField, Min(0)] private int minimumDropsPerLevel = 1;
    [SerializeField] private BreakoutItemDrop itemDropPrefab;
    [SerializeField, Min(0f)] private float itemDropFallSpeed = 1.8f;
    [SerializeField] private float itemDropBottomKillY = -6f;
    [SerializeField] private List<BuildingDropTableEntry> weightedBuildingDrops = new List<BuildingDropTableEntry>();

    [Header("Events")]
    [SerializeField] private UnityEvent onOutOfBalls;

    private int nextBallIndex;
    private int score;
    private int dropsSpawnedThisLevel;
    private readonly HashSet<BallController> activeBalls = new HashSet<BallController>();
    private bool outOfBallsInvoked;

    public int BallsRemaining => Mathf.Max(0, ballsToDispense.Count - nextBallIndex);
    public int Score => score;
    public event Action<int> ScoreChanged;
    public event Action BallsQueueChanged;

    private void OnEnable()
    {
        BrickController.BrickDestroyed += HandleBrickDestroyed;
    }

    private void OnDisable()
    {
        BrickController.BrickDestroyed -= HandleBrickDestroyed;
    }

    private void Start()
    {
        LoadBallQueueFromInventory();
        nextBallIndex = 0;
        score = 0;
        dropsSpawnedThisLevel = 0;
        outOfBallsInvoked = false;
        NotifyScoreChanged();
        NotifyBallsQueueChanged();
        TryInvokeOutOfBalls();
    }

    private void LoadBallQueueFromInventory()
    {
        if (!InventoryManager.HasInstance)
        {
            return;
        }

        List<BallTypeData> transferredBalls = InventoryManager.Instance.ConsumeCraftedBalls();
        if (transferredBalls.Count == 0)
        {
            return;
        }

        ballsToDispense.Clear();
        for (int i = 0; i < transferredBalls.Count; i++)
        {
            if (transferredBalls[i] != null)
            {
                ballsToDispense.Add(transferredBalls[i]);
            }
        }
    }

    private void Update()
    {
        if (!CanDispenseBall())
        {
            return;
        }

        if (IsDispensePressed())
        {
            DispenseBall();
        }
    }

    private void OnDestroy()
    {
        foreach (BallController activeBall in activeBalls)
        {
            if (activeBall != null)
            {
                activeBall.BallLost -= HandleBallLost;
            }
        }
    }

    private bool CanDispenseBall()
    {
        return ballPrefab != null && nextBallIndex < ballsToDispense.Count;
    }

    private bool IsDispensePressed()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        bool keyboardPressed = keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
        bool mousePressed = mouse != null && mouse.leftButton.wasPressedThisFrame;

        return keyboardPressed || mousePressed;
    }

    private void DispenseBall()
    {
        if (!CanDispenseBall())
        {
            if (ballPrefab == null)
            {
                Debug.LogError("Ball prefab is not assigned on BreakoutGameController.");
            }

            return;
        }

        Vector3 spawnPosition = paddleTransform != null
            ? paddleTransform.position + (Vector3)spawnOffset
            : (Vector3)spawnOffset;

        BallTypeData nextBallType = ballsToDispense[nextBallIndex];
        nextBallIndex++;
        NotifyBallsQueueChanged();

        BallController spawnedBall = Instantiate(ballPrefab, spawnPosition, Quaternion.identity);
        spawnedBall.BallLost += HandleBallLost;
        activeBalls.Add(spawnedBall);
        outOfBallsInvoked = false;

        if (nextBallType != null)
        {
            spawnedBall.SetTypeData(nextBallType);
        }

        spawnedBall.Launch(initialLaunchDirection);

        TryInvokeOutOfBalls();
    }

    public List<BallTypeData> GetUpcomingBallsSnapshot()
    {
        List<BallTypeData> remainingBalls = new List<BallTypeData>();
        for (int i = nextBallIndex; i < ballsToDispense.Count; i++)
        {
            remainingBalls.Add(ballsToDispense[i]);
        }

        return remainingBalls;
    }

    private void HandleBrickDestroyed(BrickController destroyedBrick, int awardedScore)
    {
        if (awardedScore > 0)
        {
            score += awardedScore;
            NotifyScoreChanged();
        }

        TrySpawnItemDropFromBrick(destroyedBrick);
    }

    private void TrySpawnItemDropFromBrick(BrickController destroyedBrick)
    {
        if (!enableBrickDrops || destroyedBrick == null || itemDropPrefab == null)
        {
            return;
        }

        int dropsStillOwed = minimumDropsPerLevel - dropsSpawnedThisLevel;
        bool mustDrop = dropsStillOwed > 0 && CountRemainingBricks() <= dropsStillOwed;

        if (!mustDrop && (brickDropChance <= 0f || UnityEngine.Random.value > brickDropChance))
        {
            return;
        }

        if (!TrySelectWeightedDrop(out BuildingDropTableEntry selectedDrop))
        {
            return;
        }

        int quantity = UnityEngine.Random.Range(selectedDrop.MinQuantity, selectedDrop.MaxQuantity + 1);
        if (quantity <= 0)
        {
            return;
        }

        BreakoutItemDrop droppedItem = Instantiate(itemDropPrefab, destroyedBrick.transform.position, Quaternion.identity);
        droppedItem.Initialize(this, selectedDrop.BuildingDefinition, quantity, itemDropFallSpeed, itemDropBottomKillY);
        dropsSpawnedThisLevel++;
    }

    private static int CountRemainingBricks()
    {
        return FindObjectsByType<BrickController>(FindObjectsSortMode.None).Length;
    }

    private bool TrySelectWeightedDrop(out BuildingDropTableEntry selectedDrop)
    {
        selectedDrop = null;

        if (weightedBuildingDrops == null || weightedBuildingDrops.Count == 0)
        {
            return false;
        }

        float totalWeight = 0f;
        for (int i = 0; i < weightedBuildingDrops.Count; i++)
        {
            BuildingDropTableEntry entry = weightedBuildingDrops[i];
            if (entry == null || entry.BuildingDefinition == null || entry.Weight <= 0f)
            {
                continue;
            }

            totalWeight += entry.Weight;
        }

        if (totalWeight <= 0f)
        {
            return false;
        }

        float pick = UnityEngine.Random.value * totalWeight;
        float cumulative = 0f;
        for (int i = 0; i < weightedBuildingDrops.Count; i++)
        {
            BuildingDropTableEntry entry = weightedBuildingDrops[i];
            if (entry == null || entry.BuildingDefinition == null || entry.Weight <= 0f)
            {
                continue;
            }

            cumulative += entry.Weight;
            if (pick <= cumulative)
            {
                selectedDrop = entry;
                return true;
            }
        }

        return false;
    }

    public bool IsCollector(Transform collectorTransform)
    {
        if (collectorTransform == null)
        {
            return false;
        }

        if (paddleTransform != null)
        {
            return collectorTransform == paddleTransform || collectorTransform.IsChildOf(paddleTransform);
        }

        return collectorTransform.CompareTag("Paddle");
    }

    public void HandleItemDropCollected(BuildingDefinition buildingDefinition, int quantity)
    {
        if (buildingDefinition == null || quantity <= 0)
        {
            return;
        }

        InventoryManager.Instance.AddBuilding(buildingDefinition, quantity);
    }

    private void NotifyScoreChanged()
    {
        ScoreChanged?.Invoke(score);
    }

    private void NotifyBallsQueueChanged()
    {
        BallsQueueChanged?.Invoke();
    }

    private void HandleBallLost(BallController lostBall)
    {
        if (lostBall != null)
        {
            lostBall.BallLost -= HandleBallLost;
            activeBalls.Remove(lostBall);
        }

        TryInvokeOutOfBalls();
    }

    private void TryInvokeOutOfBalls()
    {
        CleanupInactiveBalls();

        bool isOutOfBalls = BallsRemaining <= 0 && activeBalls.Count == 0;
        if (!isOutOfBalls)
        {
            outOfBallsInvoked = false;
            return;
        }

        if (!outOfBallsInvoked)
        {
            outOfBallsInvoked = true;
            onOutOfBalls?.Invoke();
        }
    }

    private void CleanupInactiveBalls()
    {
        if (activeBalls.Count == 0)
        {
            return;
        }

        List<BallController> staleBalls = null;
        foreach (BallController activeBall in activeBalls)
        {
            if (activeBall != null)
            {
                continue;
            }

            staleBalls ??= new List<BallController>();
            staleBalls.Add(activeBall);
        }

        if (staleBalls == null)
        {
            return;
        }

        for (int i = 0; i < staleBalls.Count; i++)
        {
            activeBalls.Remove(staleBalls[i]);
        }
    }
}
