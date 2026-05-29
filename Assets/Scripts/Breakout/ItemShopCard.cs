using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemShopCard : MonoBehaviour
{
    [SerializeField] private GameObject cardContent;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button buyButton;
    [SerializeField] private TooltipTrigger tooltipTrigger;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI quantityText;

    private BuildingDefinition definition;
    private int price;
    private int quantity;
    private Action<BuildingDefinition, int, int> onBuy;

    private void Awake()
    {
        if (buyButton != null)
        {
            buyButton.onClick.AddListener(OnBuyClicked);
        }
    }

    private void OnDestroy()
    {
        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(OnBuyClicked);
        }
    }

    public void Initialize(BuildingDefinition def, int itemPrice, int itemQuantity, Action<BuildingDefinition, int, int> buyCallback)
    {
        definition = def;
        price = itemPrice;
        quantity = itemQuantity;
        onBuy = buyCallback;

        if (iconImage != null)
        {
            Sprite sprite = def?.BuildingSprite;
            iconImage.type = Image.Type.Simple;
            iconImage.preserveAspect = true;
            iconImage.useSpriteMesh = true;
            iconImage.sprite = sprite;
            iconImage.color = def != null ? def.BuildingColor : Color.white;
            iconImage.enabled = sprite != null;
        }

        if (priceText != null)
        {
            priceText.text = "x" + itemPrice.ToString();
        }

        if (quantityText != null)
        {
            bool showQty = itemQuantity > 1;
            quantityText.gameObject.SetActive(showQty);
            if (showQty)
            {
                quantityText.text = "x" + itemQuantity.ToString();
            }
        }

        if (tooltipTrigger != null)
        {
            tooltipTrigger.SetContent(
                def != null ? def.DisplayName : string.Empty,
                def != null ? def.Description : string.Empty);
        }
    }

    public void Hide()
    {
        if (cardContent != null)
        {
            cardContent.SetActive(false);
        }
    }

    private void OnBuyClicked()
    {
        onBuy?.Invoke(definition, price, quantity);
    }
}

