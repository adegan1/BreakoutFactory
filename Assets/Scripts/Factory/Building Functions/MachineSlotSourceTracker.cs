using System.Collections.Generic;
using UnityEngine;

// Shared helpers used by fusion / compound / ball-mold machines to track the per-unit
// origin generator IDs of items deposited into their input slots so that, on removal,
// the stored resources can be refunded to the originating generators (transitively, for
// compound/fusion items that themselves were built from other inputs).
public static class MachineSlotSourceTracker
{
    // Append one entry per accepted unit. Each entry holds the per-unit origin id list
    // taken from the incoming item: for a basic generator item that's a single id; for
    // a compound/fusion output it's every contributing input id, divided evenly across
    // the item's quantity (so a stack of 2 compounds yields 2 entries of N/2 ids each).
    public static void Append(List<List<string>> slot, ItemEntity item, int unitsAccepted)
    {
        if (slot == null || item == null || unitsAccepted <= 0)
        {
            return;
        }

        IReadOnlyList<string> ids = item.OriginSourceIds;
        int itemQuantity = Mathf.Max(1, item.Quantity);
        int idsPerUnit = ids != null && ids.Count > 0 ? Mathf.Max(1, ids.Count / itemQuantity) : 0;

        for (int unit = 0; unit < unitsAccepted; unit++)
        {
            List<string> perUnit = new();
            if (idsPerUnit > 0)
            {
                int start = unit * idsPerUnit;
                for (int i = 0; i < idsPerUnit && (start + i) < ids.Count; i++)
                {
                    perUnit.Add(ids[start + i]);
                }
            }

            slot.Add(perUnit);
        }
    }

    // Remove the first N units from the slot, accumulating their origin ids into the
    // provided flattened output list (used to attach to the produced output item).
    public static void TakeFromFront(List<List<string>> slot, int units, List<string> consumedFlatOut)
    {
        if (slot == null || units <= 0)
        {
            return;
        }

        int removeCount = Mathf.Min(units, slot.Count);
        for (int i = 0; i < removeCount; i++)
        {
            List<string> perUnit = slot[i];
            if (perUnit == null)
            {
                continue;
            }

            for (int j = 0; j < perUnit.Count; j++)
            {
                consumedFlatOut?.Add(perUnit[j]);
            }
        }

        if (removeCount > 0)
        {
            slot.RemoveRange(0, removeCount);
        }
    }

    // Remove the last N units (used for defensive rollback after a rejected accept).
    public static void TrimFromEnd(List<List<string>> slot, int units)
    {
        if (slot == null || units <= 0)
        {
            return;
        }

        int removeCount = Mathf.Min(units, slot.Count);
        if (removeCount > 0)
        {
            slot.RemoveRange(slot.Count - removeCount, removeCount);
        }
    }

    // Refund every tracked unit to its originating generator. Any unit whose generator
    // has been destroyed (or whose origin id is empty) is instead spawned as a ground
    // item at dropPos so it can be picked up by the player. Returns true if any unit
    // was either refunded or dropped.
    public static bool RefundOrDropAll(
        List<List<string>> slot,
        ItemDefinition definition,
        Vector3 dropPos,
        ItemEntity itemEntityPrefab,
        Transform spawnedItemParent)
    {
        if (slot == null || slot.Count == 0 || definition == null)
        {
            return false;
        }

        bool didAnything = false;

        for (int unitIndex = 0; unitIndex < slot.Count; unitIndex++)
        {
            List<string> perUnit = slot[unitIndex];
            bool unitFullyRefunded = false;

            if (perUnit != null && perUnit.Count > 0)
            {
                unitFullyRefunded = true;
                for (int i = 0; i < perUnit.Count; i++)
                {
                    string id = perUnit[i];
                    if (string.IsNullOrEmpty(id) || !GeneratorBuilding.TryRefundByMachineStateId(id, 1))
                    {
                        unitFullyRefunded = false;
                    }
                }
            }

            if (!unitFullyRefunded && itemEntityPrefab != null)
            {
                // Fall back to dropping the unit as an item. Carry the still-recoverable
                // origin ids so a subsequent pickup can still try to refund them.
                ItemEntity dropped = Object.Instantiate(itemEntityPrefab, dropPos, Quaternion.identity, spawnedItemParent);
                dropped.Initialize(definition, 1);
                if (perUnit != null && perUnit.Count > 0)
                {
                    dropped.SetOriginSourceIds(perUnit);
                }
            }

            didAnything = true;
        }

        slot.Clear();
        return didAnything;
    }
}
