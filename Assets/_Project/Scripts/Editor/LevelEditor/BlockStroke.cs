using System;
using System.Collections.Generic;
using UnityEngine;

namespace CBJ.Editor
{
    /// <summary>
    /// The cells one mouse-down to mouse-up stroke of the block tool has passed over, in order. A fast mouse skips
    /// cells between events, so each new cell is joined to the last one by a 4-connected staircase.
    /// </summary>
    public sealed class BlockStroke
    {
        private readonly List<Vector2Int> _cells = new List<Vector2Int>();

        /// <summary>True between <see cref="Begin"/> and <see cref="End"/>.</summary>
        public bool IsActive { get; private set; }

        /// <summary>Index of the block this stroke grows, or -1 when it paints a new block.</summary>
        public int ExtendIndex { get; private set; } = -1;

        /// <summary>Visited cells, each once, in the order they were reached.</summary>
        public IReadOnlyList<Vector2Int> Cells => _cells;

        /// <summary>Starts a stroke at <paramref name="cell"/>; <paramref name="extendIndex"/> is -1 for a new block.</summary>
        public void Begin(Vector2Int cell, int extendIndex)
        {
            _cells.Clear();
            _cells.Add(cell);
            ExtendIndex = extendIndex;
            IsActive = true;
        }

        /// <summary>Adds <paramref name="cell"/> and the cells on a staircase path to it from the last cell.</summary>
        public void ExtendTo(Vector2Int cell)
        {
            Vector2Int current = _cells[_cells.Count - 1];
            while (current != cell)
            {
                Vector2Int delta = cell - current;
                current += Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
                    ? new Vector2Int(Math.Sign(delta.x), 0)
                    : new Vector2Int(0, Math.Sign(delta.y));
                if (!_cells.Contains(current))
                {
                    _cells.Add(current);
                }
            }
        }

        /// <summary>Finishes the stroke and forgets its cells.</summary>
        public void End()
        {
            _cells.Clear();
            ExtendIndex = -1;
            IsActive = false;
        }
    }
}
