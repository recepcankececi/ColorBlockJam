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
        /// Builds a new board with new blocks for one attempt. Contiguous doors of one colour on one edge become a
        /// single door, so a block as wide as their combined span can exit. Throws when the data does not describe a
        /// legal board (block without cells, block outside the board or overlapping, door past its edge);
        /// <see cref="LevelValidator"/> reports those cases without throwing.
        /// </summary>
        public BoardModel BuildBoard()
        {
            var blocks = new Block[_blocks.Count];
            for (int i = 0; i < blocks.Length; i++)
            {
                blocks[i] = _blocks[i].ToBlock();
            }

            return new BoardModel(_width, _height, MergeDoors(), blocks);
        }

        // The editor places doors one slot at a time; adjacent same-colour slots read as one opening on screen.
        internal Door[] MergeDoors()
        {
            var sorted = new List<DoorData>(_doors);
            sorted.Sort(CompareByEdgeColourOffset);

            var doors = new List<Door>(sorted.Count);
            int i = 0;
            while (i < sorted.Count)
            {
                DoorData first = sorted[i];
                int end = first.Offset + first.Length;
                int next = i + 1;
                while (next < sorted.Count && sorted[next].Edge == first.Edge &&
                       sorted[next].ColorId == first.ColorId && sorted[next].Offset == end)
                {
                    end += sorted[next].Length;
                    next++;
                }

                doors.Add(new Door(first.Edge, first.Offset, end - first.Offset, first.ColorId));
                i = next;
            }

            return doors.ToArray();
        }

        private static int CompareByEdgeColourOffset(DoorData a, DoorData b)
        {
            int byEdge = a.Edge.CompareTo(b.Edge);
            if (byEdge != 0)
            {
                return byEdge;
            }

            int byColour = a.ColorId.CompareTo(b.ColorId);
            return byColour != 0 ? byColour : a.Offset.CompareTo(b.Offset);
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

        internal void ReplaceBlockAt(int index, BlockData block)
        {
            _blocks[index] = block;
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
