using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BreakoutInventoryPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Transform slotContainer;
    [SerializeField] private BreakoutInventorySlot slotPrefab;
    [SerializeField] private GameObject shopModal;
    [SerializeField] private Button sellSelectedButton;
    [SerializeField] private GameObject ballMoldErrorMessage;
    [SerializeField] private TextMeshProUGUI scrapValueText;
    [SerializeField] private TextMeshProUGUI selectedValueText;

    [Header("Events")]
    [SerializeField] private UnityEvent onPanelOpened;
    [SerializeField] private UnityEvent onPanelClosed;

    private readonly List<BreakoutInventorySlot> spawnedSlots = new List<BreakoutInventorySlot>();
    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        if (sellSelectedButton != null)
        {
            sellSelectedButton.onClick.AddListener(SellSelected);
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromInventory();
    }

    public void Open()
    {
        isOpen = true;

        if (shopModal != null)
        {
            shopModal.SetActive(false);
        }

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        Refresh();
        SubscribeToInventory();
        onPanelOpened?.Invoke();
    }

    public void Close()
    {
        isOpen = false;
        UnsubscribeFromInventory();
        ClearSlots();

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        if (shopModal != null)
        {
            shopModal.SetActive(true);
        }

        HideBallMoldError();
        onPanelClosed?.Invoke();
    }

    private void Refresh()
    {
        ClearSlots();

        if (slotPrefab == null || slotContainer == null || !InventoryManager.HasInstance)
        {
            return;
        }

        IReadOnlyList<InventoryManager.InventoryEntry> items = InventoryManager.Instance.BuildingItems;
        int slotIndex = 0;
        for (int i = 0; i < items.Count; i++)
        {
            if (slotIndex >= slotContainer.childCount)
            {
                break;
            }

            InventoryManager.InventoryEntry entry = items[i];
            if (entry == null || entry.BuildingDefinition == null || entry.Quantity <= 0)
            {
                continue;
            }

            Transform slotParent = slotContainer.GetChild(slotIndex);
            BreakoutInventorySlot slot = Instantiate(slotPrefab, slotParent);
            slot.Initialize(entry.BuildingDefinition, entry.Quantity);
            slot.SetOnValueChanged(UpdateSelectedValueText);
            spawnedSlots.Add(slot);
            slotIndex++;
        }

        UpdateScrapText();
        UpdateSelectedValueText();
    }

    private void SellSelected()
    {
        if (!InventoryManager.HasInstance) return;

        HideBallMoldError();

        var toSell = new List<(BuildingDefinition def, int qty)>();
        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            int qty = spawnedSlots[i].GetSellQuantity();
            if (qty > 0 && spawnedSlots[i].Definition != null)
            {
                toSell.Add((spawnedSlots[i].Definition, qty));
            }
        }

        if (toSell.Count == 0) return;

        if (!ValidateBallMoldConstraint(toSell))
        {
            if (ballMoldErrorMessage != null)
            {
                ballMoldErrorMessage.SetActive(true);
            }
            return;
        }

        // Unsubscribe to avoid a Refresh() call per removal; we'll do one manual refresh.
        UnsubscribeFromInventory();

        int totalScrap = 0;
        for (int i = 0; i < toSell.Count; i++)
        {
            if (toSell[i].def != null)
            {
                totalScrap += toSell[i].def.ScrapDropAmount * toSell[i].qty;
            }
        }

        for (int i = 0; i < toSell.Count; i++)
        {
            InventoryManager.Instance.RemoveBuilding(toSell[i].def, toSell[i].qty);
        }

        if (totalScrap > 0)
        {
            PlayerStats.Instance.AddScrap(totalScrap);
        }

        Refresh();
        SubscribeToInventory();
    }

    private bool ValidateBallMoldConstraint(List<(BuildingDefinition def, int qty)> toSell)
    {
        int totalInInventory = 0;
        int totalSelling = 0;

        IReadOnlyList<InventoryManager.InventoryEntry> items = InventoryManager.Instance.BuildingItems;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null && IsBallMoldDefinition(items[i].BuildingDefinition))
            {
                totalInInventory += items[i].Quantity;
            }
        }

        for (int i = 0; i < toSell.Count; i++)
        {
            if (IsBallMoldDefinition(toSell[i].def))
            {
                totalSelling += toSell[i].qty;
            }
        }

        if (totalSelling == 0) return true;

        int placed = 0;
        foreach (KeyValuePair<BuildingDefinition, int> kvp in InventoryManager.Instance.PlacedBuildingCounts)
        {
            if (kvp.Value > 0 && IsBallMoldDefinition(kvp.Key))
            {
                placed += kvp.Value;
            }
        }

        return (totalInInventory - totalSelling + placed) >= 1;
    }

    private static bool IsBallMoldDefinition(BuildingDefinition definition)
    {
        if (definition == null || definition.BehaviorPrefab == null)
        {
            return false;
        }

        return definition.BehaviorPrefab.GetComponent<BallMoldBuilding>() != null
            || definition.BehaviorPrefab.GetComponentInChildren<BallMoldBuilding>(true) != null;
    }

    private void HideBallMoldError()
    {
        if (ballMoldErrorMessage != null)
        {
            ballMoldErrorMessage.SetActive(false);
        }
    }

    private void UpdateScrapText()
    {
        if (scrapValueText != null)
        {
            scrapValueText.text = PlayerStats.HasInstance ? PlayerStats.Instance.Scrap.ToString() : "0";
        }
    }

    private void UpdateSelectedValueText()
    {
        if (selectedValueText == null) return;

        int total = 0;
        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            if (spawnedSlots[i] != null)
            {
                total += spawnedSlots[i].GetSellScrapValue();
            }
        }
        selectedValueText.text = total.ToString();
    }

    private void ClearSlots()
    {
        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            if (spawnedSlots[i] != null)
            {
                Destroy(spawnedSlots[i].gameObject);
            }
        }

        spawnedSlots.Clear();
    }

    private void SubscribeToInventory()
    {
        if (InventoryManager.HasInstance)
        {
            InventoryManager.Instance.InventoryChanged += HandleInventoryChanged;
        }
    }

    private void UnsubscribeFromInventory()
    {
        if (InventoryManager.HasInstance)
        {
            InventoryManager.Instance.InventoryChanged -= HandleInventoryChanged;
        }
    }

    private void HandleInventoryChanged()
    {
        if (isOpen)
        {
            Refresh();
        }
    }
}
