using System;

namespace CBJ.Core
{
    /// <summary>
    /// An opening on one board edge. Covers <see cref="Length"/> cells starting at <see cref="Offset"/>, counted
    /// along the edge from the left (top/bottom edges) or from the bottom (left/right edges).
    /// </summary>
    public sealed class Door
    {
        /// <summary>Creates a door; the board checks that it fits its edge.</summary>
        public Door(Direction edge, int offset, int length, int colorId)
        {
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), offset, "Offset must not be negative.");
            }

            if (length < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(length), length, "Length must be at least one cell.");
            }

            Edge = edge;
            Offset = offset;
            Length = length;
            ColorId = colorId;
        }

        /// <summary>The edge the door sits on, named by the direction a block moves to pass through it.</summary>
        public Direction Edge { get; }

        /// <summary>First covered cell along the edge.</summary>
        public int Offset { get; }

        /// <summary>Number of covered cells.</summary>
        public int Length { get; }

        /// <summary>Palette index a block needs to pass through.</summary>
        public int ColorId { get; }
    }
}
