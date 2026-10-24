using System;
using UnityEngine;

namespace CBJ.Core
{
    /// <summary>
    /// A coloured block on a board. Its position changes only through <see cref="MoveResolver"/>.
    /// A block belongs to one <see cref="BoardModel"/> for its lifetime; each attempt creates new blocks.
    /// </summary>
    public sealed class Block
    {
        /// <summary>Creates a block whose shape's bottom-left corner sits at <paramref name="origin"/>.</summary>
        public Block(BlockShape shape, int colorId, Vector2Int origin)
        {
            Shape = shape ?? throw new ArgumentNullException(nameof(shape));
            ColorId = colorId;
            Origin = origin;
        }

        /// <summary>Footprint of the block.</summary>
        public BlockShape Shape { get; }

        /// <summary>Palette index; must match a door's colour to exit through it.</summary>
        public int ColorId { get; }

        /// <summary>Board cell of the shape's bottom-left corner.</summary>
        public Vector2Int Origin { get; internal set; }

        /// <summary>True once the block has left the board through a door.</summary>
        public bool IsExited { get; internal set; }

        internal BoardModel Board { get; set; }
    }
}
