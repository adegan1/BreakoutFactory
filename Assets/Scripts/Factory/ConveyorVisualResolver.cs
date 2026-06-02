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
        int defaultQuarterTurns,
        int animationFrameIndex = -1)
    {
        int quarterTurns = defaultQuarterTurns;
        Vector2Int currentDirection = DirectionFromQuarterTurns(quarterTurns);
        int visualQuarterTurns = Mathf.Abs(quarterTurns) % 4;
        Sprite straightSprite = GetAnimatedFrame(
            definition.ConveyorStraightAnimationSprites,
            definition.ConveyorStraightSprite != null ? definition.ConveyorStraightSprite : definition.BuildingSprite,
            animationFrameIndex);

        Sprite turnLeftSprite = GetAnimatedFrame(
            definition.ConveyorTurnLeftAnimationSprites,
            definition.ConveyorTurnLeftSprite,
            animationFrameIndex);

        Sprite turnRightSprite = GetAnimatedFrame(
            definition.ConveyorTurnRightAnimationSprites,
            definition.ConveyorTurnRightSprite,
            animationFrameIndex);

        Sprite selectedSprite = straightSprite;

        if (!incomingDirection.HasValue)
        {
            return new Result(selectedSprite, visualQuarterTurns);
        }

        Vector2Int incoming = incomingDirection.Value;
        int cross = incoming.x * currentDirection.y - incoming.y * currentDirection.x;
        bool isColinear = incoming == currentDirection || incoming == -currentDirection;

        if (isColinear)
        {
            return new Result(selectedSprite, visualQuarterTurns);
        }

        if (cross < 0)
        {
            selectedSprite = turnRightSprite != null
                ? turnRightSprite
                : (turnLeftSprite != null
                    ? turnLeftSprite
                    : selectedSprite);
        }
        else
        {
            selectedSprite = turnLeftSprite != null
                ? turnLeftSprite
                : (turnRightSprite != null
                    ? turnRightSprite
                    : selectedSprite);

            // Left-turn art needs an extra 180 degrees from its current adjusted facing.
            visualQuarterTurns = (visualQuarterTurns + 3) % 4;
        }

        return new Result(selectedSprite, visualQuarterTurns);
    }

    private static Sprite GetAnimatedFrame(Sprite[] frames, Sprite fallback, int animationFrameIndex)
    {
        if (animationFrameIndex < 0 || frames == null || frames.Length == 0)
        {
            return fallback;
        }

        int frame = Mathf.Abs(animationFrameIndex) % frames.Length;
        Sprite selected = frames[frame];
        return selected != null ? selected : fallback;
    }
}
