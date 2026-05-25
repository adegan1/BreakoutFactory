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

    public static bool HasItemAtOrReservedTile(TileManager tileManager, Vector2Int tile, ItemEntity ignoredItem = null)
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

            if (item.TryGetReservedDestination(out Vector2Int reservedTile) && reservedTile == tile)
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasReservedAtTile(TileManager tileManager, Vector2Int tile, ItemEntity ignoredItem = null)
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

            if (item.TryGetReservedDestination(out Vector2Int reservedTile) && reservedTile == tile)
            {
                return true;
            }
        }

        return false;
    }

    public static bool TryGetFirstItemAtTile(TileManager tileManager, Vector2Int tile, out ItemEntity foundItem)
    {
        foundItem = null;

        if (tileManager == null)
        {
            return false;
        }

        ItemEntity[] items = GetItems();
        for (int i = 0; i < items.Length; i++)
        {
            ItemEntity item = items[i];
            if (item == null)
            {
                continue;
            }

            Vector2Int itemTile = tileManager.WorldToGrid(item.transform.position);
            if (itemTile == tile)
            {
                foundItem = item;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Clears the frame cache so the next query does a fresh scene scan.
    /// Call this whenever an item is instantiated or a reservation is made
    /// mid-frame so subsequent checks in the same frame see the new state.
    /// </summary>
    public static void InvalidateCache()
    {
        cachedFrame = -1;
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
