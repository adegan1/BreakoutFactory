using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BreakoutInventoryPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Transform slotContainer;
    [SerializeField] private BreakoutInventorySlot slotPrefab;
    [SerializeField] private GameObject shopModal;

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
            spawnedSlots.Add(slot);
            slotIndex++;
        }
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
