using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ItemRegistry : MonoBehaviour
{
    [SerializeField] private List<ItemDefinition> itemDefinitions = new();

    private readonly Dictionary<string, ItemDefinition> itemsById = new();
    private bool isInitialized;

    public IReadOnlyList<ItemDefinition> ItemDefinitions => itemDefinitions;

    private void Awake()
    {
        RebuildLookup();
    }

    public bool TryGetItemById(string itemId, out ItemDefinition itemDefinition)
    {
        EnsureInitialized();

        if (string.IsNullOrWhiteSpace(itemId))
        {
            itemDefinition = null;
            return false;
        }

        return itemsById.TryGetValue(itemId, out itemDefinition);
    }

    public bool Contains(ItemDefinition itemDefinition)
    {
        EnsureInitialized();
        return itemDefinition != null && itemDefinitions.Contains(itemDefinition);
    }

    [ContextMenu("Rebuild Item Lookup")]
    public void RebuildLookup()
    {
        itemsById.Clear();

        for (int i = 0; i < itemDefinitions.Count; i++)
        {
            ItemDefinition definition = itemDefinitions[i];
            if (definition == null)
            {
                continue;
            }

            string itemId = definition.ItemId;
            if (string.IsNullOrWhiteSpace(itemId))
            {
                Debug.LogWarning($"Item definition '{definition.name}' has an empty ItemId and was skipped.", this);
                continue;
            }

            if (itemsById.ContainsKey(itemId))
            {
                Debug.LogWarning($"Duplicate ItemId '{itemId}' found. Keeping first definition and skipping '{definition.name}'.", this);
                continue;
            }

            itemsById[itemId] = definition;
        }

        isInitialized = true;
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            RebuildLookup();
        }
    }

    private void EnsureInitialized()
    {
        if (!isInitialized)
        {
            RebuildLookup();
        }
    }
}
