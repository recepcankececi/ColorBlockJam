using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace CBJ.Core
{
    /// <summary>
    /// Immutable footprint of a block. Cells are normalised so the bottom-left corner of the bounding box is (0,0).
    /// </summary>
    public sealed class BlockShape
    {
        private readonly ReadOnlyCollection<Vector2Int> _cells;

        /// <summary>Creates a shape from cell offsets in any position; duplicates are rejected.</summary>
        public BlockShape(IReadOnlyList<Vector2Int> cells)
        {
            if (cells == null || cells.Count == 0)
            {
                throw new ArgumentException("A shape needs at least one cell.", nameof(cells));
            }

            Vector2Int min = cells[0];
            Vector2Int max = cells[0];
            for (int i = 1; i < cells.Count; i++)
            {
                min = Vector2Int.Min(min, cells[i]);
                max = Vector2Int.Max(max, cells[i]);
            }

            var normalised = new Vector2Int[cells.Count];
            for (int i = 0; i < cells.Count; i++)
            {
                normalised[i] = cells[i] - min;
                if (Array.IndexOf(normalised, normalised[i], 0, i) >= 0)
                {
                    throw new ArgumentException($"Duplicate cell {cells[i]}.", nameof(cells));
                }
            }

            _cells = Array.AsReadOnly(normalised);
            Width = max.x - min.x + 1;
            Height = max.y - min.y + 1;
        }

        /// <summary>Bounding-box width in cells.</summary>
        public int Width { get; }

        /// <summary>Bounding-box height in cells.</summary>
        public int Height { get; }

        /// <summary>Normalised cell offsets relative to the block origin.</summary>
        public IReadOnlyList<Vector2Int> Cells => _cells;
    }
}
