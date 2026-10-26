using System;
using System.Collections.Generic;
using CBJ.Core;
using UnityEngine;

namespace CBJ.Gameplay
{
    /// <summary>
    /// Finds the block a press means: the block covering the pressed cell, or else the block whose cells come nearest
    /// to the point, as long as that is within a tolerance. A finger landing just off a block's edge still grabs it.
    /// </summary>
    public static class BlockPicker
    {
        /// <summary>
        /// Returns the block covering <paramref name="point"/> (cell space), or else the nearest block whose cell
        /// boxes lie within <paramref name="tolerance"/> cells of it; null when none does.
        /// </summary>
        public static Block Find(BoardModel board, Vector2 point, float tolerance)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            Block covering = board.GetBlockAt(Vector2Int.FloorToInt(point));
            if (covering != null)
            {
                return covering;
            }

            Block nearest = null;
            float distance = float.PositiveInfinity;
            IReadOnlyList<Block> blocks = board.Blocks;
            for (int i = 0; i < blocks.Count; i++)
            {
                Block block = blocks[i];
                if (block.IsExited)
                {
                    continue;
                }

                float blockDistance = DistanceTo(block, point);
                if (blockDistance < distance)
                {
                    distance = blockDistance;
                    nearest = block;
                }
            }

            return distance <= tolerance ? nearest : null;
        }

        private static float DistanceTo(Block block, Vector2 point)
        {
            IReadOnlyList<Vector2Int> cells = block.Shape.Cells;
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < cells.Count; i++)
            {
                Vector2Int cell = block.Origin + cells[i];
                float dx = Mathf.Max(0f, Mathf.Max(cell.x - point.x, point.x - (cell.x + 1)));
                float dy = Mathf.Max(0f, Mathf.Max(cell.y - point.y, point.y - (cell.y + 1)));
                nearest = Mathf.Min(nearest, Mathf.Sqrt(dx * dx + dy * dy));
            }

            return nearest;
        }
    }
}
