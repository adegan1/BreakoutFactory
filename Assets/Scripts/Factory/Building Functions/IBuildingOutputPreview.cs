using UnityEngine;

public interface IBuildingOutputPreview
{
    bool TryGetOutputTile(
        Vector2Int topLeftGridPosition,
        Vector2Int footprintSize,
        int rotationQuarterTurns,
        out Vector2Int outputTile,
        out Vector2Int outputDirection);
}