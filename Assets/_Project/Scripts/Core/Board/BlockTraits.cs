using System;

namespace CBJ.Core
{
    /// <summary>
    /// A block's kind and its kind-specific setting (ADR-008): the axis an arrow block slides along, or the ice layers
    /// that keep an ice block in place. The default value is a normal block.
    /// </summary>
    public readonly struct BlockTraits : IEquatable<BlockTraits>
    {
        /// <summary>Most ice layers a block can start with.</summary>
        public const int MaxIceLayers = 3;

        // Unchecked: serialized data reaches the model as stored and LevelValidator reports out-of-range values.
        internal BlockTraits(BlockKind kind, Axis arrowAxis, int iceLayers)
        {
            Kind = kind;
            ArrowAxis = kind == BlockKind.Arrow ? arrowAxis : default;
            IceLayers = kind == BlockKind.Ice ? iceLayers : 0;
        }

        /// <summary>A block that moves in every direction.</summary>
        public static BlockTraits Normal => default;

        /// <summary>Kind of the block.</summary>
        public BlockKind Kind { get; }

        /// <summary>Axis an <see cref="BlockKind.Arrow"/> block moves along; <see cref="Axis.Horizontal"/> otherwise.</summary>
        public Axis ArrowAxis { get; }

        /// <summary>Layers an <see cref="BlockKind.Ice"/> block starts with; 0 otherwise.</summary>
        public int IceLayers { get; }

        /// <summary>A block that only moves, and exits, along <paramref name="axis"/>.</summary>
        public static BlockTraits Arrow(Axis axis)
        {
            if (!Enum.IsDefined(typeof(Axis), axis))
            {
                throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
            }

            return new BlockTraits(BlockKind.Arrow, axis, 0);
        }

        /// <summary>A block held in place until <paramref name="layers"/> other blocks have left the board.</summary>
        public static BlockTraits Ice(int layers)
        {
            if (layers < 1 || layers > MaxIceLayers)
            {
                throw new ArgumentOutOfRangeException(nameof(layers), layers, $"Ice layers run from 1 to {MaxIceLayers}.");
            }

            return new BlockTraits(BlockKind.Ice, default, layers);
        }

        /// <inheritdoc />
        public bool Equals(BlockTraits other)
        {
            return Kind == other.Kind && ArrowAxis == other.ArrowAxis && IceLayers == other.IceLayers;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is BlockTraits other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(Kind, ArrowAxis, IceLayers);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            switch (Kind)
            {
                case BlockKind.Arrow: return $"Arrow {ArrowAxis}";
                case BlockKind.Ice: return $"Ice {IceLayers}";
                default: return Kind.ToString();
            }
        }
    }
}
