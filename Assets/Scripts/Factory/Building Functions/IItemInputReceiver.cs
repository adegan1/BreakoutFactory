using UnityEngine;

public interface IItemInputReceiver
{
    bool CanAcceptItemAtTile(Vector2Int tile, ItemEntity item);

    // Returns true only if the receiver consumed the item.
    bool TryAcceptItem(ItemEntity item, Vector2Int tile);
}
