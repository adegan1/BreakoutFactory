using System;

[Serializable]
public struct ItemStack : IEquatable<ItemStack>
{
    public ItemDefinition Item;
    public int Quantity;

    public ItemStack(ItemDefinition item, int quantity)
    {
        Item = item;
        Quantity = Math.Max(0, quantity);
    }

    public bool IsEmpty => Item == null || Quantity <= 0;

    public bool CanMerge(ItemStack other)
    {
        return !IsEmpty
            && !other.IsEmpty
            && Item == other.Item;
    }

    public int SpaceLeft
    {
        get
        {
            if (Item == null)
            {
                return 0;
            }

            return Math.Max(0, Item.MaxStackSize - Quantity);
        }
    }

    public int Add(int amount)
    {
        if (Item == null || amount <= 0)
        {
            return amount;
        }

        int accepted = Math.Min(amount, SpaceLeft);
        Quantity += accepted;
        return amount - accepted;
    }

    public int Remove(int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        int removed = Math.Min(Quantity, amount);
        Quantity -= removed;

        if (Quantity <= 0)
        {
            Quantity = 0;
            Item = null;
        }

        return removed;
    }

    public bool Equals(ItemStack other)
    {
        return Item == other.Item && Quantity == other.Quantity;
    }

    public override bool Equals(object obj)
    {
        return obj is ItemStack other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((Item != null ? Item.GetHashCode() : 0) * 397) ^ Quantity;
        }
    }

    public override string ToString()
    {
        string itemName = Item != null ? Item.DisplayName : "None";
        return $"{itemName} x{Quantity}";
    }
}
