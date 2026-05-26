using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class FactoryInventoryHotbarUI : MonoBehaviour
{
    [Serializable]
    private class HotbarSlotView
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI quantityText;
        [SerializeField] private TextMeshProUGUI keybindText;
        [SerializeField] private Graphic selectionHighlight;
        [SerializeField] private TooltipTrigger tooltipTrigger;
        [SerializeField] private Button selectionButton;
        [SerializeField] private RectTransform clickTarget;

        public Image IconImage => iconImage;
        public TextMeshProUGUI QuantityText => quantityText;
        public TextMeshProUGUI KeybindText => keybindText;
        public Graphic SelectionHighlight => selectionHighlight;
        public TooltipTrigger TooltipTrigger => tooltipTrigger;
        public Button SelectionButton => selectionButton;
        public RectTransform ClickTarget => clickTarget;

        public GameObject ResolveClickTarget()
        {
            if (selectionButton != null)
            {
                return selectionButton.gameObject;
            }

            if (clickTarget != null && clickTarget.gameObject.activeInHierarchy)
            {
                return clickTarget.gameObject;
            }

            if (iconImage != null && iconImage.gameObject.activeInHierarchy)
            {
                return iconImage.gameObject;
            }

            if (quantityText != null && quantityText.gameObject.activeInHierarchy)
            {
                return quantityText.gameObject;
            }

            if (keybindText != null && keybindText.gameObject.activeInHierarchy)
            {
                return keybindText.gameObject;
            }

            if (selectionHighlight != null && selectionHighlight.gameObject.activeInHierarchy)
            {
                return selectionHighlight.gameObject;
            }

            if (clickTarget != null)
            {
                return clickTarget.gameObject;
            }

            if (iconImage != null)
            {
                return iconImage.gameObject;
            }

            if (quantityText != null)
            {
                return quantityText.gameObject;
            }

            if (keybindText != null)
            {
                return keybindText.gameObject;
            }

            if (selectionHighlight != null)
            {
                return selectionHighlight.gameObject;
            }

            return null;
        }
    }

    private class HotbarSlotPointerClickForwarder : MonoBehaviour, IPointerClickHandler
    {
        private Action onLeftClick;

        public void SetOnLeftClick(Action callback)
        {
            onLeftClick = callback;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            onLeftClick?.Invoke();
        }
    }

    [Header("References")]
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private FactoryBuildingPlacer buildingPlacer;

    [Header("Slot Views (1..9,0)")]
    [SerializeField] private List<HotbarSlotView> slotViews = new List<HotbarSlotView>(InventoryManager.BuildingHotbarSlotCount);

    [Header("Visuals")]
    [SerializeField] private Color unavailableTint = new Color(1f, 1f, 1f, 0.35f);

    private bool hasBoundSlotClickHandlers;

    private void Awake()
    {
        EnsureReferences();
        InitializeKeybindLabels();
        BindSlotClickHandlersIfNeeded();
    }

    private void OnEnable()
    {
        EnsureReferences();

        if (InventoryManager.HasInstance)
        {
            InventoryManager.Instance.InventoryChanged += HandleInventoryChanged;
        }

        Refresh();
    }

    private IEnumerator Start()
    {
        // Wait one frame so the persistent InventoryManager finishes any cross-scene merging
        // before we read hotbar state. Handles the Breakout→Factory transition case.
        yield return null;

        EnsureReferences();

        if (InventoryManager.HasInstance)
        {
            InventoryManager.Instance.InventoryChanged -= HandleInventoryChanged;
            InventoryManager.Instance.CompactHotbarSlots();
            InventoryManager.Instance.InventoryChanged += HandleInventoryChanged;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (InventoryManager.HasInstance)
        {
            InventoryManager.Instance.InventoryChanged -= HandleInventoryChanged;
        }
    }

    private void LateUpdate()
    {
        RefreshSelectionHighlightOnly();
    }

    private void BindSlotClickHandlersIfNeeded()
    {
        if (hasBoundSlotClickHandlers)
        {
            return;
        }

        hasBoundSlotClickHandlers = true;

        for (int i = 0; i < InventoryManager.BuildingHotbarSlotCount; i++)
        {
            HotbarSlotView slot = GetSlotView(i);
            if (slot == null)
            {
                continue;
            }

            int slotIndex = i;
            if (slot.SelectionButton != null)
            {
                slot.SelectionButton.onClick.AddListener(() => TrySelectSlot(slotIndex));
                continue;
            }

            GameObject clickTarget = slot.ResolveClickTarget();
            if (clickTarget == null)
            {
                continue;
            }

            EnsureRaycastableClickTarget(clickTarget);

            HotbarSlotPointerClickForwarder forwarder = clickTarget.GetComponent<HotbarSlotPointerClickForwarder>();
            if (forwarder == null)
            {
                forwarder = clickTarget.AddComponent<HotbarSlotPointerClickForwarder>();
            }

            forwarder.SetOnLeftClick(() => TrySelectSlot(slotIndex));
        }
    }

    private static void EnsureRaycastableClickTarget(GameObject clickTarget)
    {
        if (clickTarget == null)
        {
            return;
        }

        Graphic graphic = clickTarget.GetComponent<Graphic>();
        if (graphic != null)
        {
            graphic.raycastTarget = true;
            return;
        }

        RectTransform rectTransform = clickTarget.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            return;
        }

        Image image = clickTarget.GetComponent<Image>();
        if (image == null)
        {
            image = clickTarget.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
        }

        image.raycastTarget = true;
    }

    private void TrySelectSlot(int slotIndex)
    {
        EnsureReferences();
        if (buildingPlacer == null)
        {
            return;
        }

        if (buildingPlacer.TrySelectBuildingByIndex(slotIndex))
        {
            RefreshSelectionHighlightOnly();
        }
    }

    private void HandleInventoryChanged()
    {
        Refresh();
    }

    public void Refresh()
    {
        EnsureReferences();

        for (int i = 0; i < InventoryManager.BuildingHotbarSlotCount; i++)
        {
            HotbarSlotView slot = GetSlotView(i);
            if (slot == null)
            {
                continue;
            }

            if (inventoryManager != null && inventoryManager.TryGetBuildingAtHotbarSlot(i, out BuildingDefinition definition, out int quantity) && definition != null && quantity > 0)
            {
                ApplyPopulatedSlot(slot, definition, quantity);
            }
            else
            {
                ApplyEmptySlot(slot);
            }

            bool isSelected = buildingPlacer != null
                && buildingPlacer.HasSelectedBuilding
                && buildingPlacer.SelectedBuildingIndex == i;
            if (slot.SelectionHighlight != null)
            {
                slot.SelectionHighlight.enabled = isSelected;
            }
        }
    }

    private void RefreshSelectionHighlightOnly()
    {
        if (buildingPlacer == null)
        {
            EnsureReferences();
        }

        for (int i = 0; i < InventoryManager.BuildingHotbarSlotCount; i++)
        {
            HotbarSlotView slot = GetSlotView(i);
            if (slot?.SelectionHighlight == null)
            {
                continue;
            }

            slot.SelectionHighlight.enabled = buildingPlacer != null
                && buildingPlacer.HasSelectedBuilding
                && buildingPlacer.SelectedBuildingIndex == i;
        }
    }

    private void ApplyPopulatedSlot(HotbarSlotView slot, BuildingDefinition definition, int quantity)
    {
        if (slot.IconImage != null)
        {
            slot.IconImage.sprite = definition.BuildingSprite;
            slot.IconImage.color = definition.BuildingColor;
            slot.IconImage.preserveAspect = true;
            slot.IconImage.enabled = true;
            slot.IconImage.gameObject.SetActive(true);
        }

        if (slot.QuantityText != null)
        {
            slot.QuantityText.text = quantity.ToString();
        }

        if (slot.TooltipTrigger != null)
        {
            slot.TooltipTrigger.SetContent(definition.DisplayName, definition.Description);
        }
    }

    private void ApplyEmptySlot(HotbarSlotView slot)
    {
        if (slot.IconImage != null)
        {
            slot.IconImage.sprite = null;
            slot.IconImage.gameObject.SetActive(false);
        }

        if (slot.QuantityText != null)
        {
            slot.QuantityText.text = string.Empty;
        }

        if (slot.TooltipTrigger != null)
        {
            slot.TooltipTrigger.SetContent(string.Empty, string.Empty);
        }
    }

    private void InitializeKeybindLabels()
    {
        for (int i = 0; i < InventoryManager.BuildingHotbarSlotCount; i++)
        {
            HotbarSlotView slot = GetSlotView(i);
            if (slot?.KeybindText == null)
            {
                continue;
            }

            slot.KeybindText.text = i == InventoryManager.BuildingHotbarSlotCount - 1
                ? "0"
                : (i + 1).ToString();
        }
    }

    private HotbarSlotView GetSlotView(int index)
    {
        if (index < 0 || index >= slotViews.Count)
        {
            return null;
        }

        return slotViews[index];
    }

    private void EnsureReferences()
    {
        if (inventoryManager == null)
        {
            inventoryManager = InventoryManager.HasInstance ? InventoryManager.Instance : FindFirstObjectByType<InventoryManager>();
        }

        if (buildingPlacer == null)
        {
            buildingPlacer = FindFirstObjectByType<FactoryBuildingPlacer>();
        }
    }
}
