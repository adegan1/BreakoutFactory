using UnityEngine;

public static class BuildingInstanceSceneQuery
{
    private static int cachedFrame = -1;
    private static BuildingInstance[] cachedBuildings = System.Array.Empty<BuildingInstance>();

    public static BuildingInstance[] GetBuildings()
    {
        int currentFrame = Time.frameCount;
        if (cachedFrame == currentFrame)
        {
            return cachedBuildings;
        }

        cachedBuildings = FindBuildings();
        cachedFrame = currentFrame;
        return cachedBuildings;
    }

    public static bool TryGetBuildingAtTile(Vector2Int tile, out BuildingInstance building)
    {
        BuildingInstance[] buildings = GetBuildings();
        for (int i = 0; i < buildings.Length; i++)
        {
            BuildingInstance candidate = buildings[i];
            if (candidate == null)
            {
                continue;
            }

            if (ContainsTile(candidate, tile))
            {
                building = candidate;
                return true;
            }
        }

        building = null;
        return false;
    }

    private static bool ContainsTile(BuildingInstance building, Vector2Int tile)
    {
        Vector2Int topLeft = building.GridPosition;
        Vector2Int footprint = building.FootprintSize;
        if (footprint.x <= 0 || footprint.y <= 0)
        {
            return false;
        }

        return tile.x >= topLeft.x
            && tile.y >= topLeft.y
            && tile.x < topLeft.x + footprint.x
            && tile.y < topLeft.y + footprint.y;
    }

    private static BuildingInstance[] FindBuildings()
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None);
#else
        return Object.FindObjectsOfType<BuildingInstance>();
#endif
    }
}
