using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BreakoutInventorySlot : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Button iconButton;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private TooltipTrigger tooltipTrigger;

    [Header("Sell Controls")]
    [SerializeField] private Toggle sellToggle;
    [SerializeField] private GameObject sellControlsPanel;
    [SerializeField] private Button decrementButton;
    [SerializeField] private Button incrementButton;
    [SerializeField] private TextMeshProUGUI sellQuantityText;

    private BuildingDefinition definition;
    private int ownedQuantity;
    private int sellQuantity = 1;
    private Action onValueChanged;

    public BuildingDefinition Definition => definition;

    public int GetSellQuantity() => (sellToggle != null && sellToggle.isOn) ? sellQuantity : 0;
    public float GetSellScrapValue() => (sellToggle != null && sellToggle.isOn) ? sellQuantity * (definition != null ? definition.ScrapDropAmount : 0f) : 0f;

    public void SetOnValueChanged(Action callback)
    {
        onValueChanged = callback;
    }

    private void Awake()
    {
        if (decrementButton != null) decrementButton.onClick.AddListener(Decrement);
        if (incrementButton != null) incrementButton.onClick.AddListener(Increment);
        if (iconButton != null) iconButton.onClick.AddListener(ToggleSelection);
        if (sellToggle != null) sellToggle.onValueChanged.AddListener(UpdateSellControlsVisibility);
    }

    public void Initialize(BuildingDefinition def, int quantity)
    {
        definition = def;
        ownedQuantity = quantity;
        sellQuantity = 1;

        if (sellToggle != null) sellToggle.isOn = false;
        UpdateSellControlsVisibility(false);
        UpdateSellQuantityText();

        if (iconImage != null)
        {
            Sprite sprite = def != null ? def.BuildingSprite : null;
            iconImage.type = Image.Type.Simple;
            iconImage.preserveAspect = true;
            iconImage.useSpriteMesh = true;
            iconImage.sprite = sprite;
            iconImage.color = def != null ? def.BuildingColor : Color.white;
            iconImage.enabled = sprite != null;
        }

        if (quantityText != null)
        {
            bool showQuantity = quantity > 1;
            quantityText.gameObject.SetActive(showQuantity);
            if (showQuantity)
            {
                quantityText.text = "x" + quantity;
            }
        }

        if (tooltipTrigger != null)
        {
            tooltipTrigger.SetContent(
                def != null ? def.LocalizedDisplayName : string.Empty,
                def != null ? def.LocalizedDescription : string.Empty);
        }
    }

    private void ToggleSelection()
    {
        if (sellToggle != null)
        {
            sellToggle.isOn = !sellToggle.isOn;
        }
    }

    private void UpdateSellControlsVisibility(bool visible)
    {
        if (sellControlsPanel != null) sellControlsPanel.SetActive(visible);
        if (decrementButton != null) decrementButton.gameObject.SetActive(visible);
        if (incrementButton != null) incrementButton.gameObject.SetActive(visible);
        if (sellQuantityText != null) sellQuantityText.gameObject.SetActive(visible);
        onValueChanged?.Invoke();
    }

    private void Increment()
    {
        if (sellQuantity < ownedQuantity)
        {
            sellQuantity++;
            UpdateSellQuantityText();
            onValueChanged?.Invoke();
        }
    }

    private void Decrement()
    {
        if (sellQuantity > 1)
        {
            sellQuantity--;
            UpdateSellQuantityText();
            onValueChanged?.Invoke();
        }
    }

    private void UpdateSellQuantityText()
    {
        if (sellQuantityText != null)
        {
            sellQuantityText.text = sellQuantity.ToString();
        }
    }
}
