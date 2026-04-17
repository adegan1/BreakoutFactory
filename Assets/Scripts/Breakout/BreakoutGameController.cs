using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class BreakoutGameController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BallController ballPrefab;
    [SerializeField] private Transform paddleTransform;

    [Header("Ball Dispense")]
    [SerializeField] private List<BallTypeData> ballsToDispense = new List<BallTypeData>();
    [SerializeField] private Vector2 initialLaunchDirection = new Vector2(0.6f, 1f);
    [SerializeField] private Vector2 spawnOffset = new Vector2(0f, 0.6f);

    [Header("Events")]
    [SerializeField] private UnityEvent onOutOfBalls;

    private int nextBallIndex;
    private readonly HashSet<BallController> activeBalls = new HashSet<BallController>();
    private bool outOfBallsInvoked;

    public int BallsRemaining => Mathf.Max(0, ballsToDispense.Count - nextBallIndex);

    private void Start()
    {
        nextBallIndex = 0;
        outOfBallsInvoked = false;
        TryInvokeOutOfBalls();
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
