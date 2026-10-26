using System;
using System.Collections.Generic;
using CBJ.Core;
using UnityEngine;

namespace CBJ.Gameplay
{
    /// <summary>
    /// Tiles a block shape with the supplied modular pieces. Every cell is split into four quadrants; a quadrant's
    /// piece depends on whether its two side neighbours and its diagonal neighbour belong to the same block.
    /// </summary>
    public static class BlockPartLayout
    {
        private const float QuadrantOffset = 0.25f;
        private const float CornerOffset = 0.5f;

        private static readonly Vector2Int[] QuadrantSigns =
        {
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1), new Vector2Int(-1, 1)
        };

        /// <summary>Clears <paramref name="parts"/> and fills it with the pieces that draw <paramref name="shape"/>.</summary>
        public static void Build(BlockShape shape, List<BlockPart> parts)
        {
            if (shape == null)
            {
                throw new ArgumentNullException(nameof(shape));
            }

            if (parts == null)
            {
                throw new ArgumentNullException(nameof(parts));
            }

            parts.Clear();
            IReadOnlyList<Vector2Int> cells = shape.Cells;
            for (int i = 0; i < cells.Count; i++)
            {
                Vector2Int cell = cells[i];
                for (int q = 0; q < QuadrantSigns.Length; q++)
                {
                    AddQuadrant(cells, cell, QuadrantSigns[q], parts);
                }
            }
        }

        private static void AddQuadrant(IReadOnlyList<Vector2Int> cells, Vector2Int cell, Vector2Int sign, List<BlockPart> parts)
        {
            bool side = Contains(cells, cell + new Vector2Int(sign.x, 0));
            bool front = Contains(cells, cell + new Vector2Int(0, sign.y));
            bool diagonal = Contains(cells, cell + sign);
            Vector2 cellCenter = new Vector2(cell.x + 0.5f, cell.y + 0.5f);
            Vector2 quadrant = cellCenter + (Vector2)sign * QuadrantOffset;

            if (!side && !front)
            {
                parts.Add(new BlockPart(BlockPartKind.OuterCorner, quadrant, CornerTurns(sign)));
            }
            else if (side && front)
            {
                parts.Add(diagonal
                    ? new BlockPart(BlockPartKind.Center, quadrant, 0)
                    : new BlockPart(BlockPartKind.InnerCorner, cellCenter + (Vector2)sign * CornerOffset, CornerTurns(sign)));
            }
            else if (!diagonal)
            {
                Direction outer = side
                    ? (sign.y > 0 ? Direction.Up : Direction.Down)
                    : (sign.x > 0 ? Direction.Right : Direction.Left);
                parts.Add(new BlockPart(BlockPartKind.Edge, quadrant, BoardLayout.QuarterTurns(outer)));
            }

            // One side neighbour plus the diagonal: this quadrant borders a concave corner and the inner corner piece
            // emitted by the cell that owns both sides of that corner already covers it.
        }

        private static int CornerTurns(Vector2Int sign)
        {
            if (sign.y > 0)
            {
                return sign.x > 0 ? 0 : 3;
            }

            return sign.x > 0 ? 1 : 2;
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
