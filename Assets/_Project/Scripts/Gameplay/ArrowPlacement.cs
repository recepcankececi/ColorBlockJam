using System;
using System.Collections.Generic;
using CBJ.Core;
using UnityEngine;

namespace CBJ.Gameplay
{
    /// <summary>
    /// Where the arrow decal of an arrow block goes: along the longest straight run of the block's cells on its axis,
    /// so the arrow never hangs over an empty cell of a concave shape. Equal runs go to the one nearest the middle.
    /// </summary>
    public static class ArrowPlacement
    {
        /// <summary>
        /// Finds the run for <paramref name="shape"/> on <paramref name="axis"/>: <paramref name="center"/> is its
        /// middle in cells from the shape's corner, <paramref name="length"/> its length in cells.
        /// </summary>
        public static void Find(BlockShape shape, Axis axis, out Vector2 center, out int length)
        {
            if (shape == null)
            {
                throw new ArgumentNullException(nameof(shape));
            }

            Vector2Int step = axis == Axis.Horizontal ? Vector2Int.right : Vector2Int.up;
            var middle = new Vector2(shape.Width * 0.5f, shape.Height * 0.5f);
            IReadOnlyList<Vector2Int> cells = shape.Cells;
            center = Vector2.zero;
            length = 0;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < cells.Count; i++)
            {
                if (Contains(cells, cells[i] - step))
                {
                    continue;
                }

                int run = 1;
                while (Contains(cells, cells[i] + step * run))
                {
                    run++;
                }

                Vector2 runCenter = cells[i] + (Vector2)step * (run * 0.5f) + (Vector2.one - step) * 0.5f;
                float distance = (runCenter - middle).sqrMagnitude;
                if (run > length || (run == length && distance < bestDistance))
                {
                    length = run;
                    center = runCenter;
                    bestDistance = distance;
                }
            }
        }

        private static bool Contains(IReadOnlyList<Vector2Int> cells, Vector2Int cell)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i] == cell)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
