using CBJ.Core;
using UnityEngine;

namespace CBJ.Editor
{
    /// <summary>
    /// What the mouse is over in the level editor grid: nothing, a board cell, or a door slot on an edge.
    /// </summary>
    public readonly struct GridHit
    {
        /// <summary>The mouse is outside the board and its door slots.</summary>
        public static readonly GridHit None = new GridHit(GridHitKind.None, Vector2Int.zero, Direction.Up, 0);

        private GridHit(GridHitKind kind, Vector2Int cell, Direction edge, int slot)
        {
            Kind = kind;
            Cell = cell;
            Edge = edge;
            Slot = slot;
        }

        /// <summary>Which of the fields is meaningful.</summary>
        public GridHitKind Kind { get; }

        /// <summary>Board cell, for <see cref="GridHitKind.Cell"/>.</summary>
        public Vector2Int Cell { get; }

        /// <summary>Board edge, for <see cref="GridHitKind.DoorSlot"/>.</summary>
        public Direction Edge { get; }

        /// <summary>Position along the edge, for <see cref="GridHitKind.DoorSlot"/>.</summary>
        public int Slot { get; }

        /// <summary>A board cell hit.</summary>
        public static GridHit AtCell(Vector2Int cell)
        {
            return new GridHit(GridHitKind.Cell, cell, Direction.Up, 0);
        }

        /// <summary>A door slot hit.</summary>
        public static GridHit AtSlot(Direction edge, int slot)
        {
            return new GridHit(GridHitKind.DoorSlot, Vector2Int.zero, edge, slot);
        }

        /// <summary>True when both hits name the same cell or slot.</summary>
        public bool SameAs(GridHit other)
        {
            return Kind == other.Kind && Cell == other.Cell && Edge == other.Edge && Slot == other.Slot;
        }
    }
}
