using System;

namespace Match3Core
{
    [Flags]
    public enum Direction
    {
        None = 0,
        Left = 1,
        Right = 2,
        Up = 4,
        Down = 8
    }

    public enum Line
    {
        None = 0,
        Horizontal = 1,
        Vertical = 2,
        Cross = 3
    }

    public class DirectionHelper
    {
        public static Direction GetOppositDirection(Direction direction)
        {
            switch (direction)
            {
                case Direction.Left:
                    return Direction.Right;
                case Direction.Right:
                    return Direction.Left;
                case Direction.Up:
                    return Direction.Down;
                case Direction.Down:
                    return Direction.Up;
                default:
                    return Direction.None;
            }
        }

        public static Line GetLineByDirection(Direction direction)
        {
            if (direction == Direction.None)
            {
                return Line.None;
            }
            if ((int)direction < 4)
            {
                return Line.Horizontal;
            }
            else
            {
                return Line.Vertical;
            }
        }

        public static Direction GetDirectionByPosition(Position a, Position b)
        {
            int diffx = b.x - a.x;
            int diffy = b.y - a.y;
            if (diffx == 0 && diffy == 0)
            {
                return Direction.None;
            }
            if (Math.Abs(diffx) > Math.Abs(diffy))
            {
                return diffx > 0 ? Direction.Right : Direction.Left;
            }
            else
            {
                return diffy > 0 ? Direction.Up : Direction.Down;
            }
        }
    }
}