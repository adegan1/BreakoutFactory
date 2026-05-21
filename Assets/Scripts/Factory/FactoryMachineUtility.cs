using UnityEngine;

public static class FactoryMachineUtility
{
    public static bool CanAcceptIntoSlot(
        ItemDefinition incoming,
        ItemDefinition slotDefinition,
        int currentAmount,
        int incomingAmount,
        int maxPerSlot)
    {
        if (incoming == null || incomingAmount <= 0)
        {
            return false;
        }

        if (slotDefinition != null && slotDefinition != incoming)
        {
            return false;
        }

        return currentAmount + incomingAmount <= maxPerSlot;
    }

    public static void AcceptIntoSlot(
        ItemDefinition incoming,
        int amount,
        ref ItemDefinition slotDefinition,
        ref int slotAmount)
    {
        slotDefinition = incoming;
        slotAmount += amount;
    }

    public static void RemoveFromSlot(
        ItemDefinition definition,
        int amount,
        ref ItemDefinition slotDefinition,
        ref int slotAmount)
    {
        if (definition == null || slotDefinition != definition || amount <= 0)
        {
            return;
        }

        slotAmount = Mathf.Max(0, slotAmount - amount);
        if (slotAmount <= 0)
        {
            slotDefinition = null;
        }
    }

    public static void RollbackAcceptedInput(
        ItemDefinition definition,
        int amount,
        bool acceptedIntoA,
        ref ItemDefinition slotADefinition,
        ref int slotAAmount,
        ref ItemDefinition slotBDefinition,
        ref int slotBAmount)
    {
        if (acceptedIntoA)
        {
            RemoveFromSlot(definition, amount, ref slotADefinition, ref slotAAmount);
            return;
        }

        RemoveFromSlot(definition, amount, ref slotBDefinition, ref slotBAmount);
    }

    public static void ClearPendingOutput(
        ref bool hasItem,
        ref ItemDefinition pendingOutputDefinition,
        ref int pendingOutputQuantity)
    {
        hasItem = false;
        pendingOutputDefinition = null;
        pendingOutputQuantity = 0;
    }

    /// <summary>
    /// Scans for any unclaimed ItemEntity sitting on the given tile and offers it to the receiver.
    /// Intended to absorb items that were snapped to a tile center after their conveyor was removed.
    /// Returns true if an item was absorbed.
    /// </summary>
    public static bool TryScanAndAbsorbItemAtTile(IItemInputReceiver receiver, Vector2Int tile, TileManager tileManager)
    {
        if (tileManager == null)
        {
            return false;
        }

        ItemEntity[] items = ItemEntitySceneQuery.GetItems();
        for (int i = 0; i < items.Length; i++)
        {
            ItemEntity item = items[i];
            if (item == null || item.IsClaimed)
            {
                continue;
            }

            if (tileManager.WorldToGrid(item.transform.position) != tile)
            {
                continue;
            }

            if (receiver.TryAcceptItem(item, tile))
            {
                return true;
            }
        }

        return false;
    }
}