using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class ItemShop : MonoBehaviour
{
    [Serializable]
    private class ShopEntry
    {
        [SerializeField] private BuildingDefinition buildingDefinition;
        [SerializeField, Min(0f)] private float weight = 1f;

        public BuildingDefinition BuildingDefinition => buildingDefinition;
        public float Weight => Mathf.Max(0f, weight);
    }

    [Header("Shop Settings")]
    [SerializeField, Min(1)] private int selectionCount = 3;
    [SerializeField, Range(0f, 1f)] private float duplicateChance = 0f;
    [SerializeField] private List<ShopEntry> shopEntries = new List<ShopEntry>();

    [Header("UI References")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Transform cardContainer;
    [SerializeField] private ItemShopCard cardPrefab;
    [SerializeField] private TextMeshProUGUI scrapValueText;

    [Header("Events")]
    [SerializeField] private UnityEvent onShopClosed;

    public event Action ShopClosed;

    private readonly List<ItemShopCard> spawnedCards = new List<ItemShopCard>();
    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }
    }

    public void Open()
    {
        List<BuildingDefinition> offers = PickOffers();

        if (offers.Count == 0)
        {
            NotifyClosed();
            return;
        }

        isOpen = true;
        BuildCards(offers);

        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
        }

        UpdateScrapText();
        if (PlayerStats.HasInstance)
        {
            PlayerStats.Instance.ScrapChanged += HandleScrapChanged;
        }
    }

    public void Skip()
    {
        Close();
    }

    private void Close()
    {
        isOpen = false;
        ClearCards();

        if (PlayerStats.HasInstance)
        {
            PlayerStats.Instance.ScrapChanged -= HandleScrapChanged;
        }

        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }

        NotifyClosed();
    }

    private void NotifyClosed()
    {
        onShopClosed?.Invoke();
        ShopClosed?.Invoke();
    }

    private void HandleCardBought(BuildingDefinition definition)
    {
        if (definition != null && InventoryManager.HasInstance)
        {
            InventoryManager.Instance.AddBuilding(definition, 1);
        }

        Close();
    }

    private List<BuildingDefinition> PickOffers()
    {
        List<BuildingDefinition> result = new List<BuildingDefinition>();

        // Build a mutable pool of valid entries.
        List<ShopEntry> pool = new List<ShopEntry>();
        for (int i = 0; i < shopEntries.Count; i++)
        {
            ShopEntry entry = shopEntries[i];
            if (entry != null && entry.BuildingDefinition != null && entry.Weight > 0f)
            {
                pool.Add(entry);
            }
        }

        int picks = Mathf.Min(selectionCount, pool.Count);
        for (int pick = 0; pick < picks; pick++)
        {
            float totalWeight = 0f;
            for (int i = 0; i < pool.Count; i++)
            {
                totalWeight += pool[i].Weight;
            }

            if (totalWeight <= 0f)
            {
                break;
            }

            float rand = UnityEngine.Random.value * totalWeight;
            float cumulative = 0f;
            for (int i = 0; i < pool.Count; i++)
            {
                cumulative += pool[i].Weight;
                if (rand <= cumulative)
                {
                    result.Add(pool[i].BuildingDefinition);
                    ShopEntry picked = pool[i];
                    pool.RemoveAt(i);

                    // Optionally re-add so this definition can appear again.
                    if (duplicateChance > 0f && UnityEngine.Random.value < duplicateChance)
                    {
                        pool.Add(picked);
                    }

                    break;
                }
            }
        }

        return result;
    }

    private void BuildCards(List<BuildingDefinition> offers)
    {
        ClearCards();

        if (cardPrefab == null || cardContainer == null)
        {
            return;
        }

        for (int i = 0; i < offers.Count; i++)
        {
            ItemShopCard card = Instantiate(cardPrefab, cardContainer);
            card.Initialize(offers[i], HandleCardBought);
            spawnedCards.Add(card);
        }
    }

    private void ClearCards()
    {
        for (int i = 0; i < spawnedCards.Count; i++)
        {
            if (spawnedCards[i] != null)
            {
                Destroy(spawnedCards[i].gameObject);
            }
        }

        spawnedCards.Clear();
    }

    private void UpdateScrapText()
    {
        if (scrapValueText != null)
        {
            scrapValueText.text = PlayerStats.HasInstance ? PlayerStats.Instance.Scrap.ToString() : "0";
        }
    }

    private void HandleScrapChanged(int _) => UpdateScrapText();
}

