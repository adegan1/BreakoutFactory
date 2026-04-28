using UnityEngine;

public static class FactoryGridDirectionUtility
{
    public static Vector2Int GetBaseDirection(GeneratorBuilding.OutputSide side)
    {
        switch (side)
        {
            case GeneratorBuilding.OutputSide.Up:
                return Vector2Int.up;
            case GeneratorBuilding.OutputSide.Left:
                return Vector2Int.left;
            case GeneratorBuilding.OutputSide.Down:
                return Vector2Int.down;
            default:
                return Vector2Int.right;
        }
    }

    public static Vector2Int RotateDirection(Vector2Int direction, int quarterTurns)
    {
        Vector2Int rotated = direction;
        int normalizedQuarterTurns = Mathf.Abs(quarterTurns) % 4;

        for (int i = 0; i < normalizedQuarterTurns; i++)
        {
            rotated = new Vector2Int(-rotated.y, rotated.x);
        }

        return rotated;
    }

    public static Vector2Int GetSideOffset(Vector2Int direction, Vector2Int footprintSize)
    {
        if (direction == Vector2Int.right)
        {
            return new Vector2Int(footprintSize.x, (footprintSize.y - 1) / 2);
        }

        if (direction == Vector2Int.left)
        {
            return new Vector2Int(-1, (footprintSize.y - 1) / 2);
        }

        if (direction == Vector2Int.up)
        {
            return new Vector2Int((footprintSize.x - 1) / 2, footprintSize.y);
        }

        return new Vector2Int((footprintSize.x - 1) / 2, -1);
    }

    public static int DirectionToQuarterTurns(Vector2Int direction)
    {
        if (direction == Vector2Int.up)
        {
            return 1;
        }

        if (direction == Vector2Int.left)
        {
            return 2;
        }

        if (direction == Vector2Int.down)
        {
            return 3;
        }

        return 0;
    }
}