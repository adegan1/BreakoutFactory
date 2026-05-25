using System;
using UnityEngine;
using UnityEngine.UI;

public class ItemShopCard : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Button buyButton;
    [SerializeField] private TooltipTrigger tooltipTrigger;

    private BuildingDefinition definition;
    private Action<BuildingDefinition> onBuy;

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

    public void Initialize(BuildingDefinition def, Action<BuildingDefinition> buyCallback)
    {
        definition = def;
        onBuy = buyCallback;

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

        if (tooltipTrigger != null)
        {
            tooltipTrigger.SetContent(
                def != null ? def.DisplayName : string.Empty,
                def != null ? def.Description : string.Empty);
        }
    }

    private void OnBuyClicked()
    {
        onBuy?.Invoke(definition);
    }
}

