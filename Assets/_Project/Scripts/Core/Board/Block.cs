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
        /// <summary>Creates a normal block whose shape's bottom-left corner sits at <paramref name="origin"/>.</summary>
        public Block(BlockShape shape, int colorId, Vector2Int origin)
            : this(shape, colorId, origin, BlockTraits.Normal)
        {
        }

        /// <summary>Creates a block of the kind given by <paramref name="traits"/>.</summary>
        public Block(BlockShape shape, int colorId, Vector2Int origin, BlockTraits traits)
        {
            Shape = shape ?? throw new ArgumentNullException(nameof(shape));
            ColorId = colorId;
            Origin = origin;
            Kind = traits.Kind;
            ArrowAxis = traits.ArrowAxis;
            IceLayers = traits.IceLayers;
        }

        /// <summary>Footprint of the block.</summary>
        public BlockShape Shape { get; }

        /// <summary>Palette index; must match a door's colour to exit through it.</summary>
        public int ColorId { get; }

        /// <summary>How the block moves.</summary>
        public BlockKind Kind { get; }

        /// <summary>Axis an <see cref="BlockKind.Arrow"/> block moves along.</summary>
        public Axis ArrowAxis { get; }

        /// <summary>Ice layers left; the block cannot move while this is above zero.</summary>
        public int IceLayers { get; internal set; }

        /// <summary>Board cell of the shape's bottom-left corner.</summary>
        public Vector2Int Origin { get; internal set; }

        /// <summary>True once the block has left the board, through a door or destroyed.</summary>
        public bool IsExited { get; internal set; }

        internal BoardModel Board { get; set; }
    }
}
