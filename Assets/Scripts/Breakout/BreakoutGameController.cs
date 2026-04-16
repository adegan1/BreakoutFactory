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
    [SerializeField] private int startingBalls = 3;
    [SerializeField] private Vector2 initialLaunchDirection = new Vector2(0.6f, 1f);
    [SerializeField] private Vector2 spawnOffset = new Vector2(0f, 0.6f);

    [Header("Events")]
    [SerializeField] private UnityEvent onOutOfBalls;

    private int ballsRemaining;
    private readonly HashSet<BallController> activeBalls = new HashSet<BallController>();

    public int BallsRemaining => ballsRemaining;

    private void Start()
    {
        ballsRemaining = Mathf.Max(0, startingBalls);
    }

    private void Update()
    {
        if (ballsRemaining <= 0)
        {
            return;
        }

        if (IsDispensePressed())
        {
            DispenseBall();
        }
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
        if (ballPrefab == null)
        {
            Debug.LogError("Ball prefab is not assigned on BreakoutGameController.");
            return;
        }

        Vector3 spawnPosition = paddleTransform != null
            ? paddleTransform.position + (Vector3)spawnOffset
            : (Vector3)spawnOffset;

        BallController spawnedBall = Instantiate(ballPrefab, spawnPosition, Quaternion.identity);
        spawnedBall.BallLost += HandleBallLost;
        activeBalls.Add(spawnedBall);
        spawnedBall.Launch(initialLaunchDirection);

        ballsRemaining--;

        if (ballsRemaining <= 0 && activeBalls.Count == 0)
        {
            onOutOfBalls?.Invoke();
        }
    }

    private void HandleBallLost(BallController lostBall)
    {
        if (lostBall != null)
        {
            lostBall.BallLost -= HandleBallLost;
            activeBalls.Remove(lostBall);
        }

        if (ballsRemaining <= 0 && activeBalls.Count == 0)
        {
            onOutOfBalls?.Invoke();
        }
    }
}
