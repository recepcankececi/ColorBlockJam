using System.Collections.Generic;
using UnityEngine;

namespace CBJ.Core
{
    /// <summary>
    /// 4-connectivity of a set of cells: two cells are connected when they share an edge, directly or through
    /// other cells of the set. A block's cells must form one connected group.
    /// </summary>
    public static class CellGroup
    {
        private static readonly Vector2Int[] Neighbours = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        /// <summary>True when <paramref name="cells"/> is non-empty and forms one 4-connected group.</summary>
        public static bool IsConnected(IReadOnlyList<Vector2Int> cells)
        {
            return cells.Count > 0 && ConnectedTo(cells, cells[0]).Count == new HashSet<Vector2Int>(cells).Count;
        }

        /// <summary>
        /// The cells of <paramref name="cells"/> reachable from <paramref name="start"/>, in reach order; empty when
        /// <paramref name="start"/> is not in the set.
        /// </summary>
        public static List<Vector2Int> ConnectedTo(IReadOnlyList<Vector2Int> cells, Vector2Int start)
        {
            var remaining = new HashSet<Vector2Int>(cells);
            var reached = new List<Vector2Int>();
            if (!remaining.Remove(start))
            {
                return reached;
            }

            reached.Add(start);
            for (int i = 0; i < reached.Count; i++)
            {
                foreach (Vector2Int offset in Neighbours)
                {
                    Vector2Int next = reached[i] + offset;
                    if (remaining.Remove(next))
                    {
                        reached.Add(next);
                    }
                }
            }

            return reached;
        }
    }
}
