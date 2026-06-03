using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PaddleController : MonoBehaviour
{
    [Header("Controls")]
    [SerializeField] private bool mouseControls;
    [SerializeField] private float moveSpeed = 10f;

    [Header("Clamp")]
    [SerializeField] private float minX = -7f;
    [SerializeField] private float maxX = 7f;

    [Header("Heal Flash")]
    [SerializeField] private bool enableHealFlash = true;
    [SerializeField] private Color healFlashColor = new Color(0.7f, 1f, 0.7f, 1f);
    [SerializeField, Min(0.01f)] private float healFlashDuration = 0.16f;
    [SerializeField, Range(0f, 1f)] private float healFlashStrength = 0.9f;

    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;
    private MeshRenderer paddleMeshRenderer;
    private MaterialPropertyBlock propertyBlock;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private Color baseSpriteColor = Color.white;
    private Coroutine healFlashRoutine;
    private PlayerStats observedPlayerStats;
    private int lastKnownHealth = -1;

    public bool MouseControls
    {
        get => mouseControls;
        set => mouseControls = value;
    }

    private void Awake()
    {
        mainCamera = Camera.main;
        spriteRenderer = GetComponent<SpriteRenderer>();
        paddleMeshRenderer = GetComponentInChildren<MeshRenderer>();
        propertyBlock = new MaterialPropertyBlock();
        if (paddleMeshRenderer != null)
        {
            Material mat = paddleMeshRenderer.sharedMaterial;
            baseSpriteColor = mat != null && mat.HasProperty(BaseColorId) ? mat.GetColor(BaseColorId) : Color.white;
            if (spriteRenderer != null)
                spriteRenderer.enabled = false;
        }
        else if (spriteRenderer != null)
        {
            baseSpriteColor = spriteRenderer.color;
        }
    }

    private void OnEnable()
    {
        TrySubscribeToPlayerStats();
    }

    private void OnDisable()
    {
        UnsubscribeFromPlayerStats();

        if (healFlashRoutine != null)
        {
            StopCoroutine(healFlashRoutine);
            healFlashRoutine = null;
        }

        SetBasePaddleColor();
    }

    private void Update()
    {
        TrySubscribeToPlayerStats();

        if (Time.timeScale == 0f)
        {
            return;
        }

        if (mouseControls)
        {
            MoveWithMouse();
            return;
        }

        MoveWithKeyboard();
    }

    private void MoveWithKeyboard()
    {
        float horizontal = GetKeyboardDirection();
        if (Mathf.Approximately(horizontal, 0f))
        {
            return;
        }

        float targetX = transform.position.x + horizontal * moveSpeed * Time.deltaTime;
        SetPositionX(targetX);
    }

    private void MoveWithMouse()
    {
        if (!TryEnsureCamera())
        {
            return;
        }

        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        Vector3 mousePosition = mouse.position.ReadValue();
        mousePosition.z = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(mousePosition);
        SetPositionX(worldPosition.x);
    }

    private float GetKeyboardDirection()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return 0f;
        }

        float horizontal = 0f;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            horizontal -= 1f;
        }

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            horizontal += 1f;
        }

        return horizontal;
    }

    private bool TryEnsureCamera()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        return mainCamera != null;
    }

    private void SetPositionX(float x)
    {
        float clampedX = Mathf.Clamp(x, minX, maxX);
        Vector3 position = transform.position;
        position.x = clampedX;
        transform.position = position;
    }

    private void TrySubscribeToPlayerStats()
    {
        if (observedPlayerStats != null)
        {
            return;
        }

        if (!PlayerStats.HasInstance)
        {
            return;
        }

        observedPlayerStats = PlayerStats.Instance;
        if (observedPlayerStats == null)
        {
            return;
        }

        observedPlayerStats.HealthChanged += HandleHealthChanged;
        observedPlayerStats.HealAttempted += HandleHealAttempted;
        lastKnownHealth = observedPlayerStats.Health;
    }

    private void UnsubscribeFromPlayerStats()
    {
        if (observedPlayerStats == null)
        {
            return;
        }

        observedPlayerStats.HealthChanged -= HandleHealthChanged;
        observedPlayerStats.HealAttempted -= HandleHealAttempted;
        observedPlayerStats = null;
        lastKnownHealth = -1;
    }

    private void HandleHealthChanged(int current, int max)
    {
        if (lastKnownHealth >= 0 && current > lastKnownHealth)
        {
            TriggerHealFlash();
        }

        lastKnownHealth = current;
    }

    private void HandleHealAttempted(int requestedAmount, int appliedAmount)
    {
        if (requestedAmount > 0 && appliedAmount <= 0)
        {
            TriggerHealFlash();
        }
    }

    private void TriggerHealFlash()
    {
        if (!enableHealFlash || spriteRenderer == null)
        {
            return;
        }

        if (healFlashRoutine != null)
        {
            StopCoroutine(healFlashRoutine);
        }

        healFlashRoutine = StartCoroutine(HealFlashCoroutine());
    }

    private IEnumerator HealFlashCoroutine()
    {
        float duration = Mathf.Max(0.01f, healFlashDuration);
        float halfDuration = duration * 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float pulse01;
            if (elapsed <= halfDuration)
            {
                pulse01 = halfDuration <= 0f ? 1f : elapsed / halfDuration;
            }
            else
            {
                float downElapsed = elapsed - halfDuration;
                pulse01 = 1f - (halfDuration <= 0f ? 1f : downElapsed / halfDuration);
            }

            ApplyHealFlashColor(Mathf.Clamp01(pulse01));
            yield return null;
        }

        SetBasePaddleColor();
        healFlashRoutine = null;
    }

    private void ApplyHealFlashColor(float pulse01)
    {
        Color baseColor = GetBasePaddleColor();
        Color pulseColor = healFlashColor;
        pulseColor.a = baseColor.a;

        float weight = Mathf.Clamp01(healFlashStrength) * Mathf.Clamp01(pulse01);
        Color result = Color.Lerp(baseColor, pulseColor, weight);

        if (paddleMeshRenderer != null)
        {
            propertyBlock.SetColor(BaseColorId, result);
            paddleMeshRenderer.SetPropertyBlock(propertyBlock);
        }
        else if (spriteRenderer != null)
        {
            spriteRenderer.color = result;
        }
    }

    private void SetBasePaddleColor()
    {
        Color baseColor = GetBasePaddleColor();
        if (paddleMeshRenderer != null)
        {
            propertyBlock.SetColor(BaseColorId, baseColor);
            paddleMeshRenderer.SetPropertyBlock(propertyBlock);
        }
        else if (spriteRenderer != null)
        {
            spriteRenderer.color = baseColor;
        }
    }

    private Color GetBasePaddleColor()
    {
        return baseSpriteColor;
    }
}
