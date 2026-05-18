using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.InputSystem;

public class BreakoutGameController : MonoBehaviour
{
    public enum LevelEndReason
    {
        LevelComplete,
        OutOfBalls,
        OutOfHealth
    }

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
    [SerializeField] private UnityEvent onAllBricksCleared;
    [SerializeField, Min(0f)] private float allBricksClearedDelaySeconds = 0.75f;

    [Header("Life Lost UI")]
    [SerializeField] private GameObject lifeLostTextObject;

    [Header("Level Complete Pause Visuals")]
    [SerializeField, Range(0f, 1f)] private float pauseGrayscaleBlend = 0.6f;
    [SerializeField, Range(0f, 1f)] private float pauseAlphaMultiplier = 0.7f;
    [SerializeField, Min(0f)] private float brickSlowStopDuration = 0.4f;
    [SerializeField, Min(1)] private int forcedBallStopFrames = 12;

    private int nextBallIndex;
    private int score;
    private int dropsSpawnedThisLevel;
    private int initialBrickCount;
    private int destroyedBrickCount;
    private readonly List<BuildingDefinition> collectedMachinesThisLevel = new List<BuildingDefinition>();
    private readonly HashSet<BallController> activeBalls = new HashSet<BallController>();
    private bool outOfBallsInvoked;
    private bool allBricksClearedInvoked;
    private bool levelEndTriggered;
    private bool outOfHealthEndQueued;
    private bool isLevelCompleteLocked;
    private Coroutine allBricksClearedRoutine;
    private Coroutine brickSlowStopRoutine;
    private Coroutine forceStopBallsRoutine;
    private PaddleController cachedPaddleController;

    public int BallsRemaining => Mathf.Max(0, ballsToDispense.Count - nextBallIndex);
    public int Score => score;
    public event Action<int> ScoreChanged;
    public event Action BallsQueueChanged;
    public event Action MachinesCollectedChanged;
    public event Action AllBricksCleared;
    public event Action<LevelEndReason> LevelEnded;

    public LevelEndReason LastLevelEndReason { get; private set; }

    private void OnEnable()
    {
        BrickController.BrickDestroyed += HandleBrickDestroyed;
        BrickController.BrickRemovedByDanger += HandleBrickRemovedByDanger;

        if (PlayerStats.HasInstance)
        {
            PlayerStats.Instance.HealthChanged += HandlePlayerHealthChanged;
        }
    }

    private void OnDisable()
    {
        BrickController.BrickDestroyed -= HandleBrickDestroyed;
        BrickController.BrickRemovedByDanger -= HandleBrickRemovedByDanger;

        if (PlayerStats.HasInstance)
        {
            PlayerStats.Instance.HealthChanged -= HandlePlayerHealthChanged;
        }
    }

    private void Start()
    {
        LoadBallQueueFromInventory();
        SetLifeLostTextActive(false);
        nextBallIndex = 0;
        score = 0;
        dropsSpawnedThisLevel = 0;
        initialBrickCount = 0;
        destroyedBrickCount = 0;
        collectedMachinesThisLevel.Clear();
        outOfBallsInvoked = false;
        allBricksClearedInvoked = false;
        levelEndTriggered = false;
        outOfHealthEndQueued = false;
        isLevelCompleteLocked = false;
        CachePaddleController();
        NotifyScoreChanged();
        NotifyBallsQueueChanged();
        NotifyMachinesCollectedChanged();
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
        if (isLevelCompleteLocked)
        {
            return;
        }

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
        StopAndClearCoroutine(ref allBricksClearedRoutine);
        StopAndClearCoroutine(ref brickSlowStopRoutine);
        StopAndClearCoroutine(ref forceStopBallsRoutine);

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
        return ballsToDispense.GetRange(nextBallIndex, ballsToDispense.Count - nextBallIndex);
    }

    public List<BuildingDefinition> GetCollectedMachinesSnapshot()
    {
        return new List<BuildingDefinition>(collectedMachinesThisLevel);
    }

    public void ClearCollectedMachinesThisLevel(bool notifyListeners = true)
    {
        if (collectedMachinesThisLevel.Count == 0)
        {
            return;
        }

        collectedMachinesThisLevel.Clear();
        if (notifyListeners)
        {
            NotifyMachinesCollectedChanged();
        }
    }

    private void HandleBrickDestroyed(BrickController destroyedBrick, int awardedScore)
    {
        destroyedBrickCount++;

        if (awardedScore > 0)
        {
            score += awardedScore;
            NotifyScoreChanged();
        }

        TrySpawnItemDropFromBrick(destroyedBrick);
        TryInvokeAllBricksCleared();
    }

    private void HandleBrickRemovedByDanger(BrickController removedBrick)
    {
        // Destroy() is end-of-frame, so check completion next frame after removals are finalized.
        StartCoroutine(TryInvokeAllBricksClearedNextFrame());
    }

    private IEnumerator TryInvokeAllBricksClearedNextFrame()
    {
        yield return null;
        TryInvokeAllBricksCleared();
    }

    private void TrySpawnItemDropFromBrick(BrickController destroyedBrick)
    {
        if (!enableBrickDrops || destroyedBrick == null || itemDropPrefab == null)
        {
            return;
        }

        bool mustDrop = ShouldForceGuaranteedDropByProgress();

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

    private bool ShouldForceGuaranteedDropByProgress()
    {
        if (minimumDropsPerLevel <= 0)
        {
            return false;
        }

        // Keep the total-brick baseline in sync with runtime spawning/order.
        int totalBricksSeenThisLevel = destroyedBrickCount + CountLivingBricks();
        initialBrickCount = Mathf.Max(initialBrickCount, totalBricksSeenThisLevel);

        if (initialBrickCount <= 0)
        {
            return false;
        }

        int requiredDropsByNow = Mathf.FloorToInt((float)destroyedBrickCount * minimumDropsPerLevel / initialBrickCount);
        requiredDropsByNow = Mathf.Clamp(requiredDropsByNow, 0, minimumDropsPerLevel);
        return dropsSpawnedThisLevel < requiredDropsByNow;
    }

    private static int CountLivingBricks()
    {
        BrickController[] bricks = FindObjectsByType<BrickController>(FindObjectsSortMode.None);
        int aliveCount = 0;
        for (int i = 0; i < bricks.Length; i++)
        {
            BrickController brick = bricks[i];
            if (brick != null && brick.CurrentHitPoints > 0)
            {
                aliveCount++;
            }
        }

        return aliveCount;
    }

    private void TryInvokeAllBricksCleared()
    {
        if (allBricksClearedInvoked || levelEndTriggered)
        {
            return;
        }

        if (outOfHealthEndQueued || (PlayerStats.HasInstance && PlayerStats.Instance.Health <= 0))
        {
            return;
        }

        if (CountLivingBricks() > 0)
        {
            return;
        }

        allBricksClearedInvoked = true;
        BeginLevelEnd(LevelEndReason.LevelComplete);
    }

    private void BeginLevelEnd(LevelEndReason reason)
    {
        if (levelEndTriggered)
        {
            return;
        }

        levelEndTriggered = true;
        outOfBallsInvoked = true;
        LastLevelEndReason = reason;
        SetLifeLostTextActive(reason == LevelEndReason.OutOfBalls || reason == LevelEndReason.OutOfHealth);

        if (reason == LevelEndReason.OutOfBalls)
        {
            ConsumeLifeFromOutOfBalls();
        }

        EnterLevelCompleteLock();

        StopAndClearCoroutine(ref allBricksClearedRoutine);

        float delay = reason == LevelEndReason.LevelComplete ? Mathf.Max(0f, allBricksClearedDelaySeconds) : 0f;
        allBricksClearedRoutine = StartCoroutine(InvokeLevelEnded(reason, delay));
    }

    private void SetLifeLostTextActive(bool isActive)
    {
        if (lifeLostTextObject == null)
        {
            return;
        }

        if (lifeLostTextObject.activeSelf != isActive)
        {
            lifeLostTextObject.SetActive(isActive);
        }
    }

    private IEnumerator InvokeLevelEnded(LevelEndReason reason, float delaySeconds)
    {
        if (delaySeconds > 0f)
        {
            yield return new WaitForSeconds(delaySeconds);
        }

        CollectAllMachineDropsOnField();

        if (reason == LevelEndReason.OutOfBalls)
        {
            onOutOfBalls?.Invoke();
        }

        if (reason == LevelEndReason.LevelComplete)
        {
            if (PlayerStats.HasInstance)
            {
                PlayerStats.Instance.IncrementLevel();
            }

            onAllBricksCleared?.Invoke();
            AllBricksCleared?.Invoke();
        }

        LevelEnded?.Invoke(reason);
        allBricksClearedRoutine = null;
    }

    private void ConsumeLifeFromOutOfBalls()
    {
        if (!PlayerStats.HasInstance)
        {
            return;
        }

        int remainingLives = Mathf.Max(0, PlayerStats.Instance.Lives - 1);
        PlayerStats.Instance.SetLives(remainingLives);
    }

    private void HandlePlayerHealthChanged(int current, int max)
    {
        if (current > 0 || levelEndTriggered || outOfHealthEndQueued)
        {
            return;
        }

        outOfHealthEndQueued = true;
        StartCoroutine(BeginLevelEndNextFrame(LevelEndReason.OutOfHealth));
    }

    private IEnumerator BeginLevelEndNextFrame(LevelEndReason reason)
    {
        yield return null;
        outOfHealthEndQueued = false;
        BeginLevelEnd(reason);
    }

    private void CollectAllMachineDropsOnField()
    {
        BreakoutItemDrop[] remainingDrops = FindObjectsByType<BreakoutItemDrop>(FindObjectsSortMode.None);
        for (int i = 0; i < remainingDrops.Length; i++)
        {
            BreakoutItemDrop drop = remainingDrops[i];
            if (drop == null)
            {
                continue;
            }

            drop.CollectImmediately();
        }
    }

    private void EnterLevelCompleteLock()
    {
        isLevelCompleteLocked = true;
        CachePaddleController();

        if (cachedPaddleController != null)
        {
            cachedPaddleController.enabled = false;
        }

        ApplyPauseVisualToPaddle();
        FreezeAndDimMachineDrops();
        DisableBrickSpawners();
        StartBrickSlowStop();

        ForceStopAllBallsOnScreen();
        StopAndClearCoroutine(ref forceStopBallsRoutine);

        forceStopBallsRoutine = StartCoroutine(ForceStopBallsForFrames(Mathf.Max(1, forcedBallStopFrames)));
    }

    private void ForceStopAllBallsOnScreen()
    {
        CleanupInactiveBalls();
        foreach (BallController activeBall in activeBalls)
        {
            if (activeBall == null)
            {
                continue;
            }

            activeBall.StopMovement();
            activeBall.ApplyLevelCompletePauseVisual(pauseGrayscaleBlend, pauseAlphaMultiplier);
        }

        BallController[] sceneBalls = FindObjectsByType<BallController>(FindObjectsSortMode.None);
        for (int i = 0; i < sceneBalls.Length; i++)
        {
            BallController sceneBall = sceneBalls[i];
            if (sceneBall == null)
            {
                continue;
            }

            sceneBall.StopMovement();
            sceneBall.ApplyLevelCompletePauseVisual(pauseGrayscaleBlend, pauseAlphaMultiplier);
        }
    }

    private IEnumerator ForceStopBallsForFrames(int frameCount)
    {
        for (int i = 0; i < frameCount; i++)
        {
            ForceStopAllBallsOnScreen();
            yield return null;
        }

        forceStopBallsRoutine = null;
    }

    private void DisableBrickSpawners()
    {
        BrickGridSpawner[] spawners = FindObjectsByType<BrickGridSpawner>(FindObjectsSortMode.None);
        for (int i = 0; i < spawners.Length; i++)
        {
            BrickGridSpawner spawner = spawners[i];
            if (spawner != null)
            {
                spawner.enabled = false;
            }
        }
    }

    private void StartBrickSlowStop()
    {
        StopAndClearCoroutine(ref brickSlowStopRoutine);

        BrickController[] bricks = FindObjectsByType<BrickController>(FindObjectsSortMode.None);
        if (bricks.Length == 0)
        {
            return;
        }

        float duration = Mathf.Max(0f, brickSlowStopDuration);
        if (duration <= 0f)
        {
            for (int i = 0; i < bricks.Length; i++)
            {
                BrickController brick = bricks[i];
                if (brick != null)
                {
                    brick.SetDownwardMotion(false, 0f);
                }
            }

            return;
        }

        float[] initialSpeeds = new float[bricks.Length];
        for (int i = 0; i < bricks.Length; i++)
        {
            BrickController brick = bricks[i];
            initialSpeeds[i] = brick != null ? Mathf.Max(0f, brick.DownwardSpeed) : 0f;
        }

        brickSlowStopRoutine = StartCoroutine(SlowStopBricks(bricks, initialSpeeds, duration));
    }

    private IEnumerator SlowStopBricks(BrickController[] bricks, float[] initialSpeeds, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            for (int i = 0; i < bricks.Length; i++)
            {
                BrickController brick = bricks[i];
                if (brick == null)
                {
                    continue;
                }

                brick.SetDownwardSpeed(Mathf.Lerp(initialSpeeds[i], 0f, t));
            }

            yield return null;
        }

        for (int i = 0; i < bricks.Length; i++)
        {
            BrickController brick = bricks[i];
            if (brick != null)
            {
                brick.SetDownwardMotion(false, 0f);
            }
        }

        brickSlowStopRoutine = null;
    }

    private void FreezeAndDimMachineDrops()
    {
        BreakoutItemDrop[] drops = FindObjectsByType<BreakoutItemDrop>(FindObjectsSortMode.None);
        for (int i = 0; i < drops.Length; i++)
        {
            BreakoutItemDrop drop = drops[i];
            if (drop == null)
            {
                continue;
            }

            drop.StopMovement();
            drop.ApplyLevelCompletePauseVisual(pauseGrayscaleBlend, pauseAlphaMultiplier);
        }

        FlameTrailProjectile[] flames = FindObjectsByType<FlameTrailProjectile>(FindObjectsSortMode.None);
        for (int i = 0; i < flames.Length; i++)
        {
            FlameTrailProjectile flame = flames[i];
            if (flame == null)
            {
                continue;
            }

            flame.StopMovement();
            flame.ApplyLevelCompletePauseVisual(pauseGrayscaleBlend, pauseAlphaMultiplier);
        }

        FertilePatchProjectile[] fertilePatches = FindObjectsByType<FertilePatchProjectile>(FindObjectsSortMode.None);
        for (int i = 0; i < fertilePatches.Length; i++)
        {
            FertilePatchProjectile fertilePatch = fertilePatches[i];
            if (fertilePatch == null)
            {
                continue;
            }

            fertilePatch.StopMovement();
            fertilePatch.ApplyLevelCompletePauseVisual(pauseGrayscaleBlend, pauseAlphaMultiplier);
        }
    }

    private void ApplyPauseVisualToPaddle()
    {
        if (paddleTransform == null)
        {
            return;
        }

        SpriteRenderer[] paddleRenderers = paddleTransform.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < paddleRenderers.Length; i++)
        {
            SpriteRenderer paddleRenderer = paddleRenderers[i];
            if (paddleRenderer == null)
            {
                continue;
            }

            Color baseColor = paddleRenderer.color;
            float gray = baseColor.grayscale;
            Color pausedColor = new Color(gray, gray, gray, baseColor.a * Mathf.Clamp01(pauseAlphaMultiplier));
            paddleRenderer.color = Color.Lerp(baseColor, pausedColor, Mathf.Clamp01(pauseGrayscaleBlend));
        }
    }

    private void CachePaddleController()
    {
        if (cachedPaddleController != null)
        {
            return;
        }

        if (paddleTransform != null)
        {
            cachedPaddleController = paddleTransform.GetComponent<PaddleController>();
            if (cachedPaddleController == null)
            {
                cachedPaddleController = paddleTransform.GetComponentInParent<PaddleController>();
            }

            if (cachedPaddleController == null)
            {
                cachedPaddleController = paddleTransform.GetComponentInChildren<PaddleController>();
            }
        }

        if (cachedPaddleController == null)
        {
            cachedPaddleController = FindAnyObjectByType<PaddleController>();
        }
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
            if (!IsValidDropEntry(entry))
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
            if (!IsValidDropEntry(entry))
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

    private static bool IsValidDropEntry(BuildingDropTableEntry entry)
    {
        return entry != null && entry.BuildingDefinition != null && entry.Weight > 0f;
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

        for (int i = 0; i < quantity; i++)
        {
            collectedMachinesThisLevel.Add(buildingDefinition);
        }

        NotifyMachinesCollectedChanged();
    }

    private void NotifyScoreChanged()
    {
        ScoreChanged?.Invoke(score);
    }

    private void NotifyBallsQueueChanged()
    {
        BallsQueueChanged?.Invoke();
    }

    private void NotifyMachinesCollectedChanged()
    {
        MachinesCollectedChanged?.Invoke();
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
        if (levelEndTriggered)
        {
            return;
        }

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
            BeginLevelEnd(LevelEndReason.OutOfBalls);
        }
    }

    private void CleanupInactiveBalls()
    {
        activeBalls.RemoveWhere(b => b == null);
    }

    private void StopAndClearCoroutine(ref Coroutine routine)
    {
        if (routine == null)
        {
            return;
        }

        StopCoroutine(routine);
        routine = null;
    }
}
