using UnityEngine;

public interface IItemInputReceiver
{
    bool CanAcceptItemAtTile(Vector2Int tile, ItemEntity item);

    // Returns true only if the receiver consumed the item.
    bool TryAcceptItem(ItemEntity item, Vector2Int tile);

    // Optional: Returns the cardinal direction items should come from (as quarter-turns: 0=Right, 1=Up, 2=Left, 3=Down)
    // Return -1 to indicate no directional restriction.
    int GetRequiredInputDirectionQuarterTurns()
    {
        return -1;
    }
}
