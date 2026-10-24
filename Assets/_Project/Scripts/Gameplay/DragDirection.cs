using CBJ.Core;
using UnityEngine;

namespace CBJ.Gameplay
{
    /// <summary>
    /// Turns how far the finger leads a block into the directions to try stepping, dominant axis first (ADR-002).
    /// </summary>
    public static class DragDirection
    {
        /// <summary>
        /// Returns how many axes of <paramref name="leadCells"/> reach <paramref name="threshold"/> (0, 1 or 2) and
        /// the step direction along each, the larger lead first.
        /// </summary>
        public static int Resolve(Vector2 leadCells, float threshold, out Direction first, out Direction second)
        {
            float absX = Mathf.Abs(leadCells.x);
            float absY = Mathf.Abs(leadCells.y);
            Direction horizontal = leadCells.x > 0f ? Direction.Right : Direction.Left;
            Direction vertical = leadCells.y > 0f ? Direction.Up : Direction.Down;
            bool xReached = absX >= threshold;
            bool yReached = absY >= threshold;
            bool xFirst = absX >= absY;

            first = xFirst ? horizontal : vertical;
            second = xFirst ? vertical : horizontal;
            if (xReached && yReached)
            {
                return 2;
            }

            if (xReached || yReached)
            {
                first = xReached ? horizontal : vertical;
                return 1;
            }

            return 0;
        }
    }
}
