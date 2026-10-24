using System;
using System.Collections.Generic;
using UnityEngine;

namespace CBJ.Core
{
    /// <summary>
    /// Movement rules for a board: blocks advance one cell per step and stop at walls and other blocks.
    /// A block pushed off an edge leaves through a door when the colours match, the block's span along the edge
    /// fits inside the door, and nothing stands between any of its cells and the edge.
    /// </summary>
    public sealed class MoveResolver
    {
        private readonly BoardModel _board;

        /// <summary>Creates the rules for <paramref name="board"/>.</summary>
        public MoveResolver(BoardModel board)
        {
            _board = board ?? throw new ArgumentNullException(nameof(board));
        }

        /// <summary>What <see cref="TryStep"/> would do, without changing the board or raising events.</summary>
        public StepResult Probe(Block block, Direction direction)
        {
            return Resolve(block, direction, out _);
        }

        /// <summary>
        /// Moves <paramref name="block"/> one cell, or exits it when the door rule allows. Callers stepping several
        /// cells stop at <see cref="StepResult.Exited"/>: an exited block is no longer on the board.
        /// </summary>
        public StepResult TryStep(Block block, Direction direction)
        {
            StepResult result = Resolve(block, direction, out Door door);
            if (result == StepResult.Moved)
            {
                _board.Move(block, block.Origin + direction.ToOffset());
            }
            else if (result == StepResult.Exited)
            {
                _board.Remove(block, door);
            }

            return result;
        }

        private StepResult Resolve(Block block, Direction direction, out Door door)
        {
            if (block == null)
            {
                throw new ArgumentNullException(nameof(block));
            }

            if (block.Board != _board || block.IsExited)
            {
                throw new ArgumentException("The block is not on this board.", nameof(block));
            }

            door = null;
            if (_board.CanOccupy(block, block.Origin + direction.ToOffset()))
            {
                return StepResult.Moved;
            }

            if (!TouchesEdge(block, direction))
            {
                return StepResult.Blocked;
            }

            door = FindExitDoor(block, direction);
            return door != null && IsExitPathClear(block, direction) ? StepResult.Exited : StepResult.Blocked;
        }

        private bool TouchesEdge(Block block, Direction edge)
        {
            switch (edge)
            {
                case Direction.Up: return block.Origin.y + block.Shape.Height == _board.Height;
                case Direction.Down: return block.Origin.y == 0;
                case Direction.Left: return block.Origin.x == 0;
                case Direction.Right: return block.Origin.x + block.Shape.Width == _board.Width;
                default: throw new ArgumentOutOfRangeException(nameof(edge), edge, null);
            }
        }

        private Door FindExitDoor(Block block, Direction edge)
        {
            bool vertical = edge.IsVertical();
            int start = vertical ? block.Origin.x : block.Origin.y;
            int end = start + (vertical ? block.Shape.Width : block.Shape.Height);

            IReadOnlyList<Door> doors = _board.Doors;
            for (int i = 0; i < doors.Count; i++)
            {
                Door door = doors[i];
                if (door.Edge == edge && door.ColorId == block.ColorId &&
                    door.Offset <= start && end <= door.Offset + door.Length)
                {
                    return door;
                }
            }

            return null;
        }

        // Concave shapes: a cell behind the edge row still has to slide through the cells ahead of it.
        private bool IsExitPathClear(Block block, Direction edge)
        {
            Vector2Int step = edge.ToOffset();
            IReadOnlyList<Vector2Int> shape = block.Shape.Cells;
            for (int i = 0; i < shape.Count; i++)
            {
                for (Vector2Int cell = block.Origin + shape[i] + step; _board.IsInside(cell); cell += step)
                {
                    Block occupant = _board.GetBlockAt(cell);
                    if (occupant != null && occupant != block)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
