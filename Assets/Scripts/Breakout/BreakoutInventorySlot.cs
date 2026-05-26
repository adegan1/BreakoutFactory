using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BreakoutInventorySlot : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private TooltipTrigger tooltipTrigger;

    public void Initialize(BuildingDefinition def, int quantity)
    {
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
                def != null ? def.DisplayName : string.Empty,
                def != null ? def.Description : string.Empty);
        }
    }
}
