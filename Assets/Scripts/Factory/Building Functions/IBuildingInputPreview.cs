using System.Collections.Generic;
using UnityEngine;

public interface IBuildingInputPreview
{
    void GetInputTiles(
        Vector2Int topLeftGridPosition,
        Vector2Int footprintSize,
        int rotationQuarterTurns,
        List<Vector2Int> inputTiles);
}
