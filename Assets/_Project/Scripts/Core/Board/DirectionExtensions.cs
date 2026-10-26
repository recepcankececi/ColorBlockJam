using System;
using UnityEngine;

namespace CBJ.Core
{
    /// <summary>
    /// Grid helpers for <see cref="Direction"/>.
    /// </summary>
    public static class DirectionExtensions
    {
        /// <summary>The one-cell grid offset of a step in this direction (y grows up).</summary>
        public static Vector2Int ToOffset(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Up: return Vector2Int.up;
                case Direction.Down: return Vector2Int.down;
                case Direction.Left: return Vector2Int.left;
                case Direction.Right: return Vector2Int.right;
                default: throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        /// <summary>True for <see cref="Direction.Up"/> and <see cref="Direction.Down"/>.</summary>
        public static bool IsVertical(this Direction direction)
        {
            return direction == Direction.Up || direction == Direction.Down;
        }

        /// <summary>The axis a step in this direction runs along.</summary>
        public static Axis ToAxis(this Direction direction)
        {
            return direction.IsVertical() ? Axis.Vertical : Axis.Horizontal;
        }
    }
}
