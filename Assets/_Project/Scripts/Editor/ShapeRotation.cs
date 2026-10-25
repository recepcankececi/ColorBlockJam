using System.Collections.Generic;
using UnityEngine;

namespace CBJ.Editor
{
    /// <summary>
    /// Rotates stamp cells in quarter turns. Results are normalised so the bounding-box corner is (0,0).
    /// </summary>
    public static class ShapeRotation
    {
        /// <summary>Quarter turns in a full rotation.</summary>
        public const int TurnsPerCircle = 4;

        /// <summary>
        /// <paramref name="cells"/> turned clockwise (board y up) <paramref name="quarterTurns"/> times; negative turns
        /// go anticlockwise.
        /// </summary>
        public static Vector2Int[] RotateClockwise(IReadOnlyList<Vector2Int> cells, int quarterTurns)
        {
            int turns = ((quarterTurns % TurnsPerCircle) + TurnsPerCircle) % TurnsPerCircle;
            var rotated = new Vector2Int[cells.Count];
            Vector2Int min = Vector2Int.zero;
            for (int i = 0; i < cells.Count; i++)
            {
                Vector2Int cell = cells[i];
                for (int t = 0; t < turns; t++)
                {
                    cell = new Vector2Int(cell.y, -cell.x);
                }

                rotated[i] = cell;
                min = i == 0 ? cell : Vector2Int.Min(min, cell);
            }

            for (int i = 0; i < rotated.Length; i++)
            {
                rotated[i] -= min;
            }

            return rotated;
        }

        /// <summary>The cell a click lands on: the leftmost cell of the top row, which is always filled.</summary>
        public static Vector2Int Anchor(IReadOnlyList<Vector2Int> cells)
        {
            Vector2Int anchor = cells[0];
            for (int i = 1; i < cells.Count; i++)
            {
                Vector2Int cell = cells[i];
                if (cell.y > anchor.y || (cell.y == anchor.y && cell.x < anchor.x))
                {
                    anchor = cell;
                }
            }

            return anchor;
        }
    }
}
