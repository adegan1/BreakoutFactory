using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
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

        public Image IconImage => iconImage;
        public TextMeshProUGUI QuantityText => quantityText;
        public TextMeshProUGUI KeybindText => keybindText;
        public Graphic SelectionHighlight => selectionHighlight;
    }

    [Header("References")]
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private FactoryBuildingPlacer buildingPlacer;

    [Header("Slot Views (1..9,0)")]
    [SerializeField] private List<HotbarSlotView> slotViews = new List<HotbarSlotView>(InventoryManager.BuildingHotbarSlotCount);

    [Header("Visuals")]
    [SerializeField] private Color unavailableTint = new Color(1f, 1f, 1f, 0.35f);

    private void Awake()
    {
        EnsureReferences();
        InitializeKeybindLabels();
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

            if (inventoryManager != null && inventoryManager.TryGetBuildingAtHotbarSlot(i, out BuildingDefinition definition, out int quantity) && definition != null)
            {
                ApplyPopulatedSlot(slot, definition, quantity);
            }
            else
            {
                ApplyEmptySlot(slot);
            }

            bool isSelected = buildingPlacer != null && buildingPlacer.SelectedBuildingIndex == i;
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

            slot.SelectionHighlight.enabled = buildingPlacer != null && buildingPlacer.SelectedBuildingIndex == i;
        }
    }

    private void ApplyPopulatedSlot(HotbarSlotView slot, BuildingDefinition definition, int quantity)
    {
        if (slot.IconImage != null)
        {
            slot.IconImage.sprite = definition.BuildingSprite;
            slot.IconImage.color = quantity > 0 ? definition.BuildingColor : unavailableTint;
            slot.IconImage.enabled = true;
            slot.IconImage.gameObject.SetActive(true);
        }

        if (slot.QuantityText != null)
        {
            slot.QuantityText.text = quantity.ToString();
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
