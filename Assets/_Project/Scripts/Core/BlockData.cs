using System;
using System.Collections.Generic;
using UnityEngine;

namespace CBJ.Core
{
    /// <summary>
    /// Serialized form of a <see cref="Block"/>: its cells, a colour and a start cell (ADR-007). Cells are stored
    /// normalised like <see cref="BlockShape"/> (bounding-box corner at (0,0)) and sorted, so equal blocks serialize
    /// identically.
    /// </summary>
    [Serializable]
    public struct BlockData : IEquatable<BlockData>
    {
        [SerializeField] private List<Vector2Int> _cells;
        [SerializeField] private Vector2Int _origin;
        [SerializeField] private int _colorId;

        /// <summary>
        /// Creates block data covering <c>origin + cells[i]</c>. Cells may be in any position; they are normalised and
        /// the origin moves with them, so the covered board cells stay the same.
        /// </summary>
        public BlockData(IReadOnlyList<Vector2Int> cells, Vector2Int origin, int colorId)
        {
            if (cells == null || cells.Count == 0)
            {
                throw new ArgumentException("A block needs at least one cell.", nameof(cells));
            }

            Vector2Int min = cells[0];
            for (int i = 1; i < cells.Count; i++)
            {
                min = Vector2Int.Min(min, cells[i]);
            }

            _cells = new List<Vector2Int>(cells.Count);
            for (int i = 0; i < cells.Count; i++)
            {
                _cells.Add(cells[i] - min);
            }

            _cells.Sort(CompareRowThenColumn);
            _origin = origin + min;
            _colorId = colorId;
        }

        /// <summary>Normalised cell offsets from <see cref="Origin"/>; empty only for corrupt data.</summary>
        public IReadOnlyList<Vector2Int> Cells => _cells ?? (IReadOnlyList<Vector2Int>)Array.Empty<Vector2Int>();

        /// <summary>Board cell of the block's bottom-left bounding-box corner at level start.</summary>
        public Vector2Int Origin => _origin;

        /// <summary>Palette index of the block.</summary>
        public int ColorId => _colorId;

        /// <summary>Bounding-box size in cells.</summary>
        public Vector2Int Size
        {
            get
            {
                IReadOnlyList<Vector2Int> cells = Cells;
                Vector2Int max = Vector2Int.zero;
                for (int i = 0; i < cells.Count; i++)
                {
                    max = Vector2Int.Max(max, cells[i]);
                }

                return cells.Count == 0 ? Vector2Int.zero : max + Vector2Int.one;
            }
        }

        /// <summary>True when the block covers board cell <paramref name="cell"/>.</summary>
        public bool Covers(Vector2Int cell)
        {
            IReadOnlyList<Vector2Int> cells = Cells;
            Vector2Int local = cell - _origin;
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i] == local)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Creates a fresh runtime block at the start cell; throws when the data has no or duplicate cells.</summary>
        public Block ToBlock()
        {
            return new Block(new BlockShape(Cells), _colorId, _origin);
        }

        /// <inheritdoc />
        public bool Equals(BlockData other)
        {
            if (_origin != other._origin || _colorId != other._colorId || Cells.Count != other.Cells.Count)
            {
                return false;
            }

            for (int i = 0; i < Cells.Count; i++)
            {
                if (Cells[i] != other.Cells[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is BlockData other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(_origin, _colorId, Cells.Count);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Block(colour {_colorId}, origin {_origin}, {Cells.Count} cells)";
        }

        private static int CompareRowThenColumn(Vector2Int a, Vector2Int b)
        {
            return a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x);
        }
    }
}
