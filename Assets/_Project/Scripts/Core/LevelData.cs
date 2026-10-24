using System;
using System.Collections.Generic;
using UnityEngine;

namespace CBJ.Core
{
    /// <summary>
    /// The one saved format of a level (ADR-003). The level editor writes it, the game reads it, and every attempt
    /// builds a fresh <see cref="BoardModel"/> from it, so nothing carries over between attempts. Read-only outside
    /// the editor, so game code cannot change a level asset by accident.
    /// </summary>
    [Serializable]
    public sealed class LevelData
    {
        [SerializeField] private int _width;
        [SerializeField] private int _height;
        [SerializeField] private int _timerSeconds;
        [SerializeField] private List<DoorData> _doors = new List<DoorData>();
        [SerializeField] private List<BlockData> _blocks = new List<BlockData>();

        /// <summary>Creates an empty level of the given size and time limit.</summary>
        public LevelData(int width, int height, int timerSeconds)
        {
            _width = width;
            _height = height;
            _timerSeconds = timerSeconds;
        }

        /// <summary>Board width in cells.</summary>
        public int Width => _width;

        /// <summary>Board height in cells.</summary>
        public int Height => _height;

        /// <summary>Time limit of one attempt.</summary>
        public int TimerSeconds
        {
            get => _timerSeconds;
            internal set => _timerSeconds = value;
        }

        /// <summary>Doors of the level.</summary>
        public IReadOnlyList<DoorData> Doors => _doors;

        /// <summary>Blocks at level start.</summary>
        public IReadOnlyList<BlockData> Blocks => _blocks;

        /// <summary>Length in cells of the given edge.</summary>
        public int EdgeLength(Direction edge)
        {
            return edge.IsVertical() ? _width : _height;
        }

        /// <summary>
        /// Builds a new board with new blocks for one attempt. Throws when the data does not describe a legal board
        /// (unknown shape, block outside the board or overlapping, door past its edge).
        /// </summary>
        public BoardModel BuildBoard()
        {
            var doors = new Door[_doors.Count];
            for (int i = 0; i < doors.Length; i++)
            {
                doors[i] = _doors[i].ToDoor();
            }

            var blocks = new Block[_blocks.Count];
            for (int i = 0; i < blocks.Length; i++)
            {
                blocks[i] = _blocks[i].ToBlock();
            }

            return new BoardModel(_width, _height, doors, blocks);
        }

        internal void SetSize(int width, int height)
        {
            _width = width;
            _height = height;
        }

        internal void AddDoor(DoorData door)
        {
            _doors.Add(door);
        }

        internal void RemoveDoorAt(int index)
        {
            _doors.RemoveAt(index);
        }

        internal void AddBlock(BlockData block)
        {
            _blocks.Add(block);
        }

        internal void RemoveBlockAt(int index)
        {
            _blocks.RemoveAt(index);
        }

        internal void Clear()
        {
            _doors.Clear();
            _blocks.Clear();
        }
    }
}
