using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

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
    [SerializeField, Min(0f)] private float priceMarkupPercent = 50f;

    [Header("Reroll Settings")]
    [SerializeField, Min(0)] private int rerollBaseCost = 5;
    [SerializeField, Min(0)] private int rerollCostIncrease = 5;

    [Header("UI References")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Transform cardContainer;
    [SerializeField] private ItemShopCard cardPrefab;
    [SerializeField] private TextMeshProUGUI scrapValueText;
    [SerializeField] private TextMeshProUGUI rerollPriceText;
    [SerializeField] private Button rerollButton;
    [SerializeField] private GameObject insufficientScrapMessage;

    [Header("Events")]
    [SerializeField] private UnityEvent onShopClosed;

    public event Action ShopClosed;

    private readonly List<ItemShopCard> spawnedCards = new List<ItemShopCard>();
    private bool isOpen;
    private int rerollCount;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }

        ConfigureDynamicTextLocalizationExclusions();

        if (rerollButton != null)
        {
            rerollButton.onClick.AddListener(OnRerollClicked);
        }
    }

    private void OnDestroy()
    {
        if (rerollButton != null)
        {
            rerollButton.onClick.RemoveListener(OnRerollClicked);
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

        rerollCount = 0;
        isOpen = true;
        BuildCards(offers);
        HideInsufficientScrapMessage();

        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
        }

        UpdateScrapText();
        UpdateRerollPriceText();
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
        HideInsufficientScrapMessage();

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

    private void HandleCardBought(ItemShopCard card, BuildingDefinition definition, int price, int quantity)
    {
        if (definition == null) return;

        if (price > 0)
        {
            if (!PlayerStats.HasInstance || PlayerStats.Instance.Scrap < price)
            {
                ShowInsufficientScrapMessage();
                return;
            }

            PlayerStats.Instance.RemoveScrap(price);
        }

        if (InventoryManager.HasInstance)
        {
            InventoryManager.Instance.AddBuilding(definition, quantity);
        }

        BreakoutSoundController.PlayItemBoughtSfx();
        HideInsufficientScrapMessage();

        spawnedCards.Remove(card);
        if (card != null)
        {
            Destroy(card.gameObject);
        }
    }

    private List<BuildingDefinition> PickOffers()
    {
        List<BuildingDefinition> result = new List<BuildingDefinition>();

        // Build a mutable pool of valid entries.
        List<ShopEntry> pool = new List<ShopEntry>();
        float totalWeight = 0f;
        for (int i = 0; i < shopEntries.Count; i++)
        {
            ShopEntry entry = shopEntries[i];
            if (entry != null && entry.BuildingDefinition != null && entry.Weight > 0f)
            {
                pool.Add(entry);
                totalWeight += entry.Weight;
            }
        }

        int picks = Mathf.Min(selectionCount, pool.Count);
        for (int pick = 0; pick < picks; pick++)
        {
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
                    totalWeight -= picked.Weight;
                    pool.RemoveAt(i);

                    // Optionally re-add so this definition can appear again.
                    if (duplicateChance > 0f && UnityEngine.Random.value < duplicateChance)
                    {
                        pool.Add(picked);
                        totalWeight += picked.Weight;
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

        float priceMultiplier = 1f + priceMarkupPercent / 100f;
        for (int i = 0; i < offers.Count; i++)
        {
            BuildingDefinition def = offers[i];
            int qty = UnityEngine.Random.Range(def.MinShopBuyAmount, def.MaxShopBuyAmount + 1);
            int price = Mathf.CeilToInt(qty * def.ScrapDropAmount * priceMultiplier);
            ItemShopCard card = Instantiate(cardPrefab, cardContainer);
            card.Initialize(def, price, qty, (d, p, q) => HandleCardBought(card, d, p, q));
            spawnedCards.Add(card);
        }
    }

    private void ClearCards()
    {
        foreach (ItemShopCard card in spawnedCards)
        {
            if (card != null)
            {
                Destroy(card.gameObject);
            }
        }

        spawnedCards.Clear();
    }

    private void UpdateScrapText()
    {
        if (scrapValueText != null)
        {
            scrapValueText.text = PlayerStats.HasInstance ? "x" + PlayerStats.Instance.Scrap.ToString() : "x0";
        }
    }

    private void OnRerollClicked()
    {
        int cost = GetCurrentRerollCost();
        if (cost > 0)
        {
            if (!PlayerStats.HasInstance || PlayerStats.Instance.Scrap < cost)
            {
                ShowInsufficientScrapMessage();
                return;
            }

            PlayerStats.Instance.RemoveScrap(cost);
        }

        rerollCount++;
        List<BuildingDefinition> offers = PickOffers();
        BuildCards(offers);
        BreakoutSoundController.PlayShopRerollSfx();
        HideInsufficientScrapMessage();
        UpdateRerollPriceText();
    }

    private int GetCurrentRerollCost()
    {
        return rerollBaseCost + rerollCount * rerollCostIncrease;
    }

    private void UpdateRerollPriceText()
    {
        if (rerollPriceText != null)
        {
            rerollPriceText.text = "x" + GetCurrentRerollCost().ToString();
        }
    }

    private void HandleScrapChanged(int _)
    {
        UpdateScrapText();
    }

    private void HideInsufficientScrapMessage()
    {
        if (insufficientScrapMessage != null)
        {
            insufficientScrapMessage.SetActive(false);
        }
    }

    private void ShowInsufficientScrapMessage()
    {
        if (insufficientScrapMessage != null)
        {
            insufficientScrapMessage.SetActive(true);
        }
    }

    private void ConfigureDynamicTextLocalizationExclusions()
    {
        ConfigureTextExclusion(scrapValueText);
        ConfigureTextExclusion(rerollPriceText);
    }

    private static void ConfigureTextExclusion(TextMeshProUGUI text)
    {
        if (text == null)
        {
            return;
        }

        LocalizationTextExclusion exclusion = text.GetComponent<LocalizationTextExclusion>();
        if (exclusion == null)
        {
            exclusion = text.gameObject.AddComponent<LocalizationTextExclusion>();
        }

        // Dynamic numeric labels are assigned directly at runtime.
        // Keep translation disabled while still allowing language-based font swap.
        exclusion.Configure(excludeTranslationValue: true, excludeFontSwapValue: false);
    }
}

