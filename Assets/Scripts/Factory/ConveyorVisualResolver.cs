using UnityEngine;

// Pure stateless logic for resolving which conveyor sprite and rotation quarter-turns to use based on the building definition and the incoming conveyor direction.
public static class ConveyorVisualResolver
{
    public readonly struct Result
    {
        public readonly Sprite Sprite;
        public readonly int QuarterTurns;

        public Result(Sprite sprite, int quarterTurns)
        {
            Sprite = sprite;
            QuarterTurns = quarterTurns;
        }
    }

    // Returns the grid direction vector for a given number of clockwise quarter-turns from right.
    public static Vector2Int DirectionFromQuarterTurns(int quarterTurns)
    {
        switch (Mathf.Abs(quarterTurns) % 4)
        {
            case 0:  return Vector2Int.right;
            case 1:  return Vector2Int.up;
            case 2:  return Vector2Int.left;
            default: return Vector2Int.down;
        }
    }

    // Resolves the conveyor sprite for a conveyor tile given its facing direction and the direction of an adjacent upstream conveyor feeding into it.
    // Rotation quarter-turns remain the logical placement direction; turn sprites are visual-only.
    public static Result Resolve(
        BuildingDefinition definition,
        Vector2Int? incomingDirection,
        int defaultQuarterTurns)
    {
        int quarterTurns = defaultQuarterTurns;
        Vector2Int currentDirection = DirectionFromQuarterTurns(quarterTurns);
        Sprite selectedSprite = definition.ConveyorStraightSprite != null
            ? definition.ConveyorStraightSprite
            : definition.BuildingSprite;

        if (!incomingDirection.HasValue)
        {
            return new Result(selectedSprite, quarterTurns);
        }

        Vector2Int incoming = incomingDirection.Value;
        int cross = incoming.x * currentDirection.y - incoming.y * currentDirection.x;
        bool isColinear = incoming == currentDirection || incoming == -currentDirection;

        if (isColinear)
        {
            return new Result(selectedSprite, quarterTurns);
        }

        if (cross < 0)
        {
            selectedSprite = definition.ConveyorTurnRightSprite != null
                ? definition.ConveyorTurnRightSprite
                : (definition.ConveyorTurnLeftSprite != null
                    ? definition.ConveyorTurnLeftSprite
                    : selectedSprite);
        }
        else
        {
            selectedSprite = definition.ConveyorTurnLeftSprite != null
                ? definition.ConveyorTurnLeftSprite
                : (definition.ConveyorTurnRightSprite != null
                    ? definition.ConveyorTurnRightSprite
                    : selectedSprite);
        }

        return new Result(selectedSprite, quarterTurns);
    }
}
