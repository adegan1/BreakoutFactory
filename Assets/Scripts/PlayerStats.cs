using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class PlayerStats : MonoBehaviour
{
    private static PlayerStats instance;

    [Header("Starting Values")]
    [SerializeField, Min(1)] private int startingLives = 3;
    [SerializeField, Min(1)] private int startingMaxHealth = 3;

    [Header("Runtime (Read Only)")]
    [SerializeField, Min(0)] private int lives;
    [SerializeField, Min(0)] private int health;
    [SerializeField, Min(1)] private int maxHealth;

    [Header("Game Over")]
    [SerializeField] private bool loadSceneOnGameOver = true;
    [SerializeField] private string gameOverSceneName = "GameoverScene";

    private bool gameOverTriggered;

    public static PlayerStats Instance => EnsureInstance();
    public static bool HasInstance => instance != null;

    public int Lives => lives;
    public int Health => health;
    public int MaxHealth => maxHealth;
    public bool IsAlive => lives > 0 && health > 0;

    public event Action<int> LivesChanged;
    public event Action<int, int> HealthChanged;  // (current, max)
    public event Action LifeLost;
    public event Action GameOver;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        ResetToStartingValues();
    }

    public void ResetToStartingValues()
    {
        gameOverTriggered = false;
        maxHealth = Mathf.Max(1, startingMaxHealth);
        lives = Mathf.Max(1, startingLives);
        health = maxHealth;
        NotifyLivesChanged();
        NotifyHealthChanged();
    }

    public void ResetHealthForNewLife()
    {
        health = maxHealth;
        NotifyHealthChanged();
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || !IsAlive)
        {
            return;
        }

        health = Mathf.Max(0, health - amount);
        NotifyHealthChanged();

        if (health <= 0)
        {
            LoseLife();
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || !IsAlive)
        {
            return;
        }

        health = Mathf.Min(maxHealth, health + amount);
        NotifyHealthChanged();
    }

    public void SetMaxHealth(int newMaxHealth, bool refillHealth = false)
    {
        maxHealth = Mathf.Max(1, newMaxHealth);

        if (refillHealth)
        {
            health = maxHealth;
        }
        else
        {
            health = Mathf.Min(health, maxHealth);
        }

        NotifyHealthChanged();
    }

    public void AddLives(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        lives += amount;
        NotifyLivesChanged();
    }

    public void SetLives(int amount)
    {
        lives = Mathf.Max(0, amount);
        NotifyLivesChanged();

        if (lives <= 0)
        {
            TriggerGameOver();
        }
    }

    private void LoseLife()
    {
        lives = Mathf.Max(0, lives - 1);
        LifeLost?.Invoke();
        NotifyLivesChanged();

        if (lives <= 0)
        {
            TriggerGameOver();
        }
    }

    private void TriggerGameOver()
    {
        if (gameOverTriggered)
        {
            return;
        }

        gameOverTriggered = true;
        GameOver?.Invoke();

        if (!loadSceneOnGameOver)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(gameOverSceneName))
        {
            Debug.LogWarning("PlayerStats cannot load a game-over scene because the scene name is empty.", this);
            return;
        }

        SceneManager.LoadScene(gameOverSceneName.Trim());
    }

    private void NotifyLivesChanged()
    {
        LivesChanged?.Invoke(lives);
    }

    private void NotifyHealthChanged()
    {
        HealthChanged?.Invoke(health, maxHealth);
    }

    private static PlayerStats EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        instance = FindFirstObjectByType<PlayerStats>();
        if (instance == null)
        {
            GameObject statsObject = new GameObject("PlayerStats");
            instance = statsObject.AddComponent<PlayerStats>();
        }

        return instance;
    }
}
