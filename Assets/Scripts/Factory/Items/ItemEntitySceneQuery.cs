using UnityEngine;

public static class ItemEntitySceneQuery
{
    private static int cachedFrame = -1;
    private static ItemEntity[] cachedItems = System.Array.Empty<ItemEntity>();

    public static ItemEntity[] GetItems()
    {
        int currentFrame = Time.frameCount;
        if (cachedFrame == currentFrame)
        {
            return cachedItems;
        }

        cachedItems = FindItems();
        cachedFrame = currentFrame;
        return cachedItems;
    }

    public static bool HasItemAtTile(TileManager tileManager, Vector2Int tile, ItemEntity ignoredItem = null)
    {
        if (tileManager == null)
        {
            return false;
        }

        ItemEntity[] items = GetItems();
        for (int i = 0; i < items.Length; i++)
        {
            ItemEntity item = items[i];
            if (item == null || item == ignoredItem)
            {
                continue;
            }

            Vector2Int itemTile = tileManager.WorldToGrid(item.transform.position);
            if (itemTile == tile)
            {
                return true;
            }
        }

        return false;
    }

    private static ItemEntity[] FindItems()
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindObjectsByType<ItemEntity>(FindObjectsSortMode.None);
#else
        return Object.FindObjectsOfType<ItemEntity>();
#endif
    }
}
