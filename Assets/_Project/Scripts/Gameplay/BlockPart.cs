using UnityEngine;

namespace CBJ.Gameplay
{
    /// <summary>
    /// One modular piece of a block: which piece, where its footprint is centred and how it is turned.
    /// </summary>
    public readonly struct BlockPart
    {
        /// <summary>Creates a part.</summary>
        public BlockPart(BlockPartKind kind, Vector2 center, int quarterTurns)
        {
            Kind = kind;
            Center = center;
            QuarterTurns = quarterTurns;
        }

        /// <summary>Which supplied piece to use.</summary>
        public BlockPartKind Kind { get; }

        /// <summary>
        /// Centre of the piece's footprint in cell space relative to the block origin: a quadrant centre, or the
        /// cell corner an inner corner wraps around.
        /// </summary>
        public Vector2 Center { get; }

        /// <summary>
        /// Clockwise quarter turns seen from above. Unturned, an edge's outer side faces +y, an outer corner points to
        /// (+x,+y) and an inner corner's notch opens to (+x,+y).
        /// </summary>
        public int QuarterTurns { get; }
    }
}
