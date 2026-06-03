using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BreakoutScorePopup : MonoBehaviour
{
    [System.Serializable]
    private class PopupStyleSettings
    {
        [SerializeField] private bool useScoreValue;
        [SerializeField] private string staticText = string.Empty;
        [SerializeField] private Color color = Color.white;
        [SerializeField, Min(1f)] private float fontSize = 36f;
        [SerializeField, Min(0.05f)] private float lifetimeSeconds = 0.8f;
        [SerializeField] private float riseDistance = 0.75f;
        [SerializeField, Min(0f)] private float horizontalDriftRange = 0.3f;

        public bool UseScoreValue => useScoreValue;
        public string StaticText => staticText;
        public Color Color => color;
        public float FontSize => fontSize;
        public float LifetimeSeconds => lifetimeSeconds;
        public float RiseDistance => riseDistance;
        public float HorizontalDriftRange => horizontalDriftRange;

        public static PopupStyleSettings CreateScoreDefaults()
        {
            return new PopupStyleSettings
            {
                useScoreValue = true,
                staticText = string.Empty,
                color = new Color(1f, 0.95f, 0.5f, 1f),
                fontSize = 36f,
                lifetimeSeconds = 0.8f,
                riseDistance = 0.75f,
                horizontalDriftRange = 0.3f
            };
        }

        public static PopupStyleSettings CreateSuperEffectiveDefaults()
        {
            return new PopupStyleSettings
            {
                useScoreValue = false,
                staticText = "SUPER!",
                color = new Color(1f, 0.55f, 0.2f, 1f),
                fontSize = 44f,
                lifetimeSeconds = 0.9f,
                riseDistance = 1.0f,
                horizontalDriftRange = 0.45f
            };
        }

        public static PopupStyleSettings CreateHealingDefaults()
        {
            return new PopupStyleSettings
            {
                useScoreValue = true,
                staticText = string.Empty,
                color = new Color(0.45f, 1f, 0.45f, 1f),
                fontSize = 34f,
                lifetimeSeconds = 0.8f,
                riseDistance = 0.7f,
                horizontalDriftRange = 0.25f
            };
        }

        public static PopupStyleSettings CreateDamageDefaults()
        {
            return new PopupStyleSettings
            {
                useScoreValue = true,
                staticText = string.Empty,
                color = new Color(1f, 0.45f, 0.45f, 1f),
                fontSize = 34f,
                lifetimeSeconds = 0.8f,
                riseDistance = 0.7f,
                horizontalDriftRange = 0.25f
            };
        }

        public static PopupStyleSettings CreateItemPickupDefaults()
        {
            return new PopupStyleSettings
            {
                useScoreValue = false,
                staticText = string.Empty,
                color = Color.white,
                fontSize = 32f,
                lifetimeSeconds = 0.8f,
                riseDistance = 0.7f,
                horizontalDriftRange = 0.25f
            };
        }

        public static PopupStyleSettings CreateScrapPickupDefaults()
        {
            return new PopupStyleSettings
            {
                useScoreValue = false,
                staticText = string.Empty,
                color = new Color(0.9f, 0.9f, 0.9f, 1f),
                fontSize = 32f,
                lifetimeSeconds = 0.8f,
                riseDistance = 0.7f,
                horizontalDriftRange = 0.25f
            };
        }
    }

    [Header("References")]
    [SerializeField] private TMP_Text scoreText;

    [Header("Popup Styles")]
    [SerializeField] private PopupStyleSettings scoreStyle = PopupStyleSettings.CreateScoreDefaults();
    [SerializeField] private PopupStyleSettings superEffectiveStyle = PopupStyleSettings.CreateSuperEffectiveDefaults();
    [SerializeField] private PopupStyleSettings healingStyle = PopupStyleSettings.CreateHealingDefaults();
    [SerializeField] private PopupStyleSettings damageStyle = PopupStyleSettings.CreateDamageDefaults();
    [SerializeField] private PopupStyleSettings itemPickupStyle = PopupStyleSettings.CreateItemPickupDefaults();
    [SerializeField] private PopupStyleSettings scrapPickupStyle = PopupStyleSettings.CreateScrapPickupDefaults();

    private Vector3 startPosition;
    private Color baseColor = Color.white;
    private float activeLifetimeSeconds = 0.8f;
    private float activeRiseDistance = 0.75f;
    private float elapsed;
    private float horizontalDriftOffset;
    private bool isInitialized;

    private void Awake()
    {
        if (scoreText == null)
        {
            scoreText = GetComponentInChildren<TMP_Text>();
        }
    }

    public void InitializeScore(int awardedScore, Vector3 sourceWorldPosition)
    {
        InitializeWithStyle(scoreStyle, awardedScore, sourceWorldPosition);
    }

    public void InitializeSuperEffective(Vector3 sourceWorldPosition)
    {
        InitializeWithStyle(superEffectiveStyle, 0, sourceWorldPosition);
    }
    
    public void InitializeHealing(int healedAmount, Vector3 sourceWorldPosition)
    {
        InitializeWithStyle(healingStyle, healedAmount, sourceWorldPosition);
    }

    public void InitializeDamage(int damageAmount, Vector3 sourceWorldPosition)
    {
        InitializeWithStyle(damageStyle, damageAmount, sourceWorldPosition, "-");
    }

    public void InitializeItemPickup(string itemName, int quantity, Color itemColor, Vector3 sourceWorldPosition)
    {
        string pickupText = BuildPickupText(itemName, quantity);
        InitializeTextPopup(itemPickupStyle, pickupText, itemColor, sourceWorldPosition);
    }

    public void InitializeScrapPickup(int scrapAmount, Vector3 sourceWorldPosition)
    {
        string pickupText = BuildPickupText("Scrap", scrapAmount);
        InitializeTextPopup(scrapPickupStyle, pickupText, scrapPickupStyle != null ? scrapPickupStyle.Color : Color.white, sourceWorldPosition);
    }

    private void InitializeWithStyle(PopupStyleSettings style, int awardedScore, Vector3 sourceWorldPosition)
    {
        InitializeWithStyle(style, awardedScore, sourceWorldPosition, "+");
    }

    private void InitializeWithStyle(PopupStyleSettings style, int awardedScore, Vector3 sourceWorldPosition, string valuePrefix)
    {
        if (scoreText == null)
        {
            scoreText = GetComponentInChildren<TMP_Text>();
        }

        if (scoreText == null)
        {
            Destroy(gameObject);
            return;
        }

        startPosition = ResolveSpawnPosition(sourceWorldPosition);
        transform.position = startPosition;
        activeLifetimeSeconds = Mathf.Max(0.05f, style != null ? style.LifetimeSeconds : 0.8f);
        activeRiseDistance = style != null ? style.RiseDistance : 0.75f;
        elapsed = 0f;
        float driftRange = Mathf.Max(0f, style != null ? style.HorizontalDriftRange : 0.3f);
        horizontalDriftOffset = Random.Range(-driftRange, driftRange);
        isInitialized = true;

        int displayedScore = Mathf.Max(0, awardedScore);
        if (style != null && !style.UseScoreValue && !string.IsNullOrWhiteSpace(style.StaticText))
        {
            scoreText.text = style.StaticText;
        }
        else
        {
            scoreText.text = valuePrefix + displayedScore;
        }

        float popupFontSize = Mathf.Max(1f, style != null ? style.FontSize : 36f);
        scoreText.enableAutoSizing = false;
        scoreText.fontSizeMin = popupFontSize;
        scoreText.fontSizeMax = popupFontSize;
        scoreText.fontSize = popupFontSize;
        scoreText.ForceMeshUpdate();

        baseColor = style != null ? style.Color : Color.white;
        scoreText.color = baseColor;
    }

    private void InitializeTextPopup(PopupStyleSettings style, string text, Color textColor, Vector3 sourceWorldPosition)
    {
        if (scoreText == null)
        {
            scoreText = GetComponentInChildren<TMP_Text>();
        }

        if (scoreText == null)
        {
            Destroy(gameObject);
            return;
        }

        startPosition = ResolveSpawnPosition(sourceWorldPosition);
        transform.position = startPosition;
        activeLifetimeSeconds = Mathf.Max(0.05f, style != null ? style.LifetimeSeconds : 0.8f);
        activeRiseDistance = style != null ? style.RiseDistance : 0.75f;
        elapsed = 0f;
        float driftRange = Mathf.Max(0f, style != null ? style.HorizontalDriftRange : 0.3f);
        horizontalDriftOffset = Random.Range(-driftRange, driftRange);
        isInitialized = true;

        scoreText.text = text;

        float popupFontSize = Mathf.Max(1f, style != null ? style.FontSize : 36f);
        scoreText.enableAutoSizing = false;
        scoreText.fontSizeMin = popupFontSize;
        scoreText.fontSizeMax = popupFontSize;
        scoreText.fontSize = popupFontSize;
        scoreText.ForceMeshUpdate();

        baseColor = textColor;
        scoreText.color = baseColor;
    }

    private static string BuildPickupText(string label, int quantity)
    {
        string safeLabel = string.IsNullOrWhiteSpace(label) ? string.Empty : label.Trim();
        int amount = Mathf.Max(1, quantity);
        return amount > 1 ? safeLabel + " x" + amount : safeLabel;
    }

    private Vector3 ResolveSpawnPosition(Vector3 worldPosition)
    {
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null || parentCanvas.renderMode == RenderMode.WorldSpace)
        {
            return worldPosition;
        }

        RectTransform parentRect = transform.parent as RectTransform;
        if (parentRect == null)
        {
            return worldPosition;
        }

        Camera screenCamera = null;
        if (parentCanvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            screenCamera = parentCanvas.worldCamera;
        }

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, worldPosition);
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(parentRect, screenPoint, screenCamera, out Vector3 uiWorldPoint))
        {
            return uiWorldPoint;
        }

        return worldPosition;
    }

    private void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        float lifetime = Mathf.Max(0.05f, activeLifetimeSeconds);
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / lifetime);

        Vector3 nextPosition = startPosition;
        nextPosition.x += horizontalDriftOffset * t;
        nextPosition.y += activeRiseDistance * t;
        transform.position = nextPosition;

        Color nextColor = baseColor;
        nextColor.a = Mathf.Lerp(baseColor.a, 0f, t);
        scoreText.color = nextColor;

        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
