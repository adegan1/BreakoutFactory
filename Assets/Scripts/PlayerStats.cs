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
    [SerializeField, Min(1)] private int startingLevel = 1;
    [SerializeField, Min(0)] private int startingScrap;
    [SerializeField, Min(0)] private int startingScore;

    [Header("Runtime (Read Only)")]
    [SerializeField, Min(0)] private int lives;
    [SerializeField, Min(0)] private int health;
    [SerializeField, Min(1)] private int maxHealth;
    [SerializeField, Min(1)] private int currentLevel = 1;
    [SerializeField, Min(0)] private int scrap;
    [SerializeField, Min(0)] private int score;

    [Header("Game Over")]
    [SerializeField] private bool loadSceneOnGameOver = true;
    [SerializeField] private string gameOverSceneName = "GameoverScene";

    private bool gameOverTriggered;

    public static PlayerStats Instance => EnsureInstance();
    public static bool HasInstance => instance != null;

    public int Lives => lives;
    public int Health => health;
    public int MaxHealth => maxHealth;
    public int CurrentLevel => currentLevel;
    public int Scrap => scrap;
    public int Score => score;
    public bool IsAlive => lives > 0 && health > 0;

    public event Action<int> LivesChanged;
    public event Action<int, int> HealthChanged;  // (current, max)
    public event Action<int> CurrentLevelChanged;
    public event Action<int> ScrapChanged;
    public event Action<int> ScoreChanged;
    public event Action<int, int> HealAttempted; // (requestedAmount, appliedAmount)
    public event Action<int, int> DamageTaken; // (requestedAmount, appliedAmount)
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
        currentLevel = Mathf.Max(1, startingLevel);
        health = maxHealth;
        scrap = Mathf.Max(0, startingScrap);
        score = Mathf.Max(0, startingScore);
        NotifyLivesChanged();
        NotifyHealthChanged();
        NotifyCurrentLevelChanged();
        ScrapChanged?.Invoke(scrap);
        ScoreChanged?.Invoke(score);
    }

    public void SetCurrentLevel(int level)
    {
        int clampedLevel = Mathf.Max(1, level);
        if (currentLevel == clampedLevel)
        {
            return;
        }

        currentLevel = clampedLevel;
        NotifyCurrentLevelChanged();
    }

    public void IncrementLevel(int amount = 1)
    {
        if (amount <= 0)
        {
            return;
        }

        SetCurrentLevel(currentLevel + amount);
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

        int previousHealth = health;
        health = Mathf.Max(0, health - amount);
        int appliedAmount = Mathf.Max(0, previousHealth - health);
        NotifyHealthChanged();
        DamageTaken?.Invoke(amount, appliedAmount);

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

        int previousHealth = health;
        health = Mathf.Min(maxHealth, health + amount);
        int appliedAmount = Mathf.Max(0, health - previousHealth);
        NotifyHealthChanged();
        HealAttempted?.Invoke(amount, appliedAmount);
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

    public void AddScrap(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        scrap += amount;
        ScrapChanged?.Invoke(scrap);
    }

    public bool RemoveScrap(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (scrap < amount)
        {
            return false;
        }

        scrap -= amount;
        ScrapChanged?.Invoke(scrap);
        return true;
    }

    public void SetScrap(int amount)
    {
        scrap = Mathf.Max(0, amount);
        ScrapChanged?.Invoke(scrap);
    }

    public void AddScore(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        score += amount;
        ScoreChanged?.Invoke(score);
    }

    public void SetScore(int amount)
    {
        score = Mathf.Max(0, amount);
        ScoreChanged?.Invoke(score);
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

    private void NotifyCurrentLevelChanged()
    {
        CurrentLevelChanged?.Invoke(currentLevel);
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
