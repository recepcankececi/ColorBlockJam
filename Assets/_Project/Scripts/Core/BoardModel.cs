using System;
using System.Collections.Generic;
using UnityEngine;

namespace CBJ.Core
{
    /// <summary>
    /// State of one level attempt: cell occupancy, edge doors and the blocks still on the board.
    /// Cell (0,0) is bottom-left; x grows right, y grows up. Changed only through <see cref="MoveResolver"/>;
    /// a restart builds a fresh board from fresh blocks. Event handlers must not move blocks themselves.
    /// A board built without blocks starts cleared and never raises <see cref="Cleared"/>.
    /// </summary>
    public sealed class BoardModel
    {
        private readonly Block[] _cells;
        private readonly Block[] _blocks;
        private readonly Door[] _doors;

        /// <summary>Takes ownership of <paramref name="blocks"/>; each must fit inside the board without overlap.</summary>
        public BoardModel(int width, int height, IReadOnlyList<Door> doors, IReadOnlyList<Block> blocks)
        {
            if (width < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be at least one cell.");
            }

            if (height < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be at least one cell.");
            }

            if (doors == null)
            {
                throw new ArgumentNullException(nameof(doors));
            }

            if (blocks == null)
            {
                throw new ArgumentNullException(nameof(blocks));
            }

            Width = width;
            Height = height;
            _cells = new Block[width * height];

            _doors = new Door[doors.Count];
            for (int i = 0; i < doors.Count; i++)
            {
                Door door = doors[i] ?? throw new ArgumentException($"Door {i} is null.", nameof(doors));
                if (door.Offset + door.Length > EdgeLength(door.Edge))
                {
                    throw new ArgumentException($"Door {i} runs past the {door.Edge} edge.", nameof(doors));
                }

                _doors[i] = door;
            }

            _blocks = new Block[blocks.Count];
            for (int i = 0; i < blocks.Count; i++)
            {
                Block block = blocks[i] ?? throw new ArgumentException($"Block {i} is null.", nameof(blocks));
                if (block.Board != null || Array.IndexOf(_blocks, block, 0, i) >= 0)
                {
                    throw new ArgumentException($"Block {i} already belongs to a board.", nameof(blocks));
                }

                if (!CanOccupy(block, block.Origin))
                {
                    throw new ArgumentException($"Block {i} is outside the board or overlaps another.", nameof(blocks));
                }

                SetCells(block, block.Origin, block);
                _blocks[i] = block;
            }

            // Ownership is claimed only after every block validated, so a rejected level leaves its blocks reusable.
            for (int i = 0; i < _blocks.Length; i++)
            {
                _blocks[i].Board = this;
            }

            RemainingBlocks = _blocks.Length;
        }

        /// <summary>Raised after a block advanced one cell: block, previous origin, new origin.</summary>
        public event Action<Block, Vector2Int, Vector2Int> BlockMoved;

        /// <summary>Raised after a block left the board through the given door.</summary>
        public event Action<Block, Door> BlockExited;

        /// <summary>Raised once, when the last block has exited.</summary>
        public event Action Cleared;

        /// <summary>Board width in cells.</summary>
        public int Width { get; }

        /// <summary>Board height in cells.</summary>
        public int Height { get; }

        /// <summary>All doors, in the order given.</summary>
        public IReadOnlyList<Door> Doors => _doors;

        /// <summary>All blocks, including exited ones, in the order given.</summary>
        public IReadOnlyList<Block> Blocks => _blocks;

        /// <summary>Number of blocks that have not exited yet.</summary>
        public int RemainingBlocks { get; private set; }

        /// <summary>True when every block has exited.</summary>
        public bool IsCleared => RemainingBlocks == 0;

        /// <summary>True when <paramref name="cell"/> lies on the board.</summary>
        public bool IsInside(Vector2Int cell)
        {
            return cell.x >= 0 && cell.y >= 0 && cell.x < Width && cell.y < Height;
        }

        /// <summary>The block covering <paramref name="cell"/>, or null when it is empty or off the board.</summary>
        public Block GetBlockAt(Vector2Int cell)
        {
            return IsInside(cell) ? _cells[Index(cell)] : null;
        }

        /// <summary>
        /// True when every cell of <paramref name="block"/> placed at <paramref name="origin"/> is on the board and
        /// either empty or covered by the block itself.
        /// </summary>
        public bool CanOccupy(Block block, Vector2Int origin)
        {
            if (block == null)
            {
                throw new ArgumentNullException(nameof(block));
            }

            IReadOnlyList<Vector2Int> shape = block.Shape.Cells;
            for (int i = 0; i < shape.Count; i++)
            {
                Vector2Int cell = origin + shape[i];
                if (!IsInside(cell))
                {
                    return false;
                }

                Block occupant = _cells[Index(cell)];
                if (occupant != null && occupant != block)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Length in cells of the given edge.</summary>
        public int EdgeLength(Direction edge)
        {
            return edge.IsVertical() ? Width : Height;
        }

        internal void Move(Block block, Vector2Int origin)
        {
            Vector2Int from = block.Origin;
            SetCells(block, from, null);
            SetCells(block, origin, block);
            block.Origin = origin;
            BlockMoved?.Invoke(block, from, origin);
        }

        internal void Remove(Block block, Door door)
        {
            SetCells(block, block.Origin, null);
            block.IsExited = true;
            RemainingBlocks--;
            BlockExited?.Invoke(block, door);
            if (RemainingBlocks == 0)
            {
                Cleared?.Invoke();
            }
        }

        private void SetCells(Block block, Vector2Int origin, Block value)
        {
            IReadOnlyList<Vector2Int> shape = block.Shape.Cells;
            for (int i = 0; i < shape.Count; i++)
            {
                _cells[Index(origin + shape[i])] = value;
            }
        }

        private int Index(Vector2Int cell)
        {
            return cell.y * Width + cell.x;
        }
    }
}
