using System.Collections.Generic;
using CBJ.Core;
using UnityEngine;

namespace CBJ.Editor
{
    /// <summary>
    /// The edits the level editor makes to a <see cref="LevelData"/>. Every edit keeps the level a legal board:
    /// blocks stay inside, never overlap and stay 4-connected; doors stay on their edge and never overlap.
    /// </summary>
    public static class LevelEditOperations
    {
        /// <summary>Index of the block covering <paramref name="cell"/>, or -1.</summary>
        public static int FindBlockAt(LevelData level, Vector2Int cell)
        {
            IReadOnlyList<BlockData> blocks = level.Blocks;
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i].Covers(cell))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Origin that puts the stamp's top-left filled cell on <paramref name="clicked"/>, so a click always lands on
        /// the block it places, even for shapes whose bounding-box corner is empty.
        /// </summary>
        public static Vector2Int OriginForClick(IReadOnlyList<Vector2Int> cells, Vector2Int clicked)
        {
            return clicked - ShapeRotation.Anchor(cells);
        }

        /// <summary>True when the cells fit inside the board at <paramref name="origin"/> without overlapping.</summary>
        public static bool CanPlaceBlock(LevelData level, IReadOnlyList<Vector2Int> cells, Vector2Int origin)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                if (!IsFree(level, origin + cells[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Adds a block of the given kind when it fits; returns whether it was added.</summary>
        public static bool TryPlaceBlock(LevelData level, IReadOnlyList<Vector2Int> cells, Vector2Int origin, int colorId,
            BlockTraits traits = default)
        {
            if (!CanPlaceBlock(level, cells, origin))
            {
                return false;
            }

            level.AddBlock(new BlockData(cells, origin, colorId, traits));
            return true;
        }

        /// <summary>
        /// Board cells of the block a stroke would leave: free stroke cells 4-connected to the first free one, or, when
        /// <paramref name="extendIndex"/> names a block, that block's cells plus the free stroke cells connected to it.
        /// <paramref name="dropped"/> counts free stroke cells left out because they do not connect. Empty when the
        /// stroke paints nothing.
        /// </summary>
        public static List<Vector2Int> StrokeBlockCells(LevelData level, IReadOnlyList<Vector2Int> stroke, int extendIndex, out int dropped)
        {
            var free = new List<Vector2Int>(stroke.Count);
            for (int i = 0; i < stroke.Count; i++)
            {
                if (IsFree(level, stroke[i]))
                {
                    free.Add(stroke[i]);
                }
            }

            if (extendIndex < 0)
            {
                List<Vector2Int> painted = free.Count == 0 ? free : CellGroup.ConnectedTo(free, free[0]);
                dropped = free.Count - painted.Count;
                return painted;
            }

            BlockData block = level.Blocks[extendIndex];
            var candidates = new List<Vector2Int>(free);
            for (int i = 0; i < block.Cells.Count; i++)
            {
                candidates.Add(block.Origin + block.Cells[i]);
            }

            List<Vector2Int> grown = CellGroup.ConnectedTo(candidates, block.Origin + block.Cells[0]);
            dropped = candidates.Count - grown.Count;
            return grown;
        }

        /// <summary>Like the other overload, painting a normal block.</summary>
        public static bool ApplyStroke(LevelData level, IReadOnlyList<Vector2Int> stroke, int extendIndex, int colorId, out int dropped)
        {
            return ApplyStroke(level, stroke, extendIndex, colorId, BlockTraits.Normal, out dropped);
        }

        /// <summary>
        /// Applies a finished stroke: paints a new block of <paramref name="colorId"/> and kind
        /// <paramref name="traits"/>, or grows the block named by <paramref name="extendIndex"/> (it keeps its colour
        /// and kind). Returns whether the level changed.
        /// </summary>
        public static bool ApplyStroke(LevelData level, IReadOnlyList<Vector2Int> stroke, int extendIndex, int colorId,
            BlockTraits traits, out int dropped)
        {
            List<Vector2Int> cells = StrokeBlockCells(level, stroke, extendIndex, out dropped);
            if (extendIndex < 0)
            {
                if (cells.Count == 0)
                {
                    return false;
                }

                level.AddBlock(new BlockData(cells, Vector2Int.zero, colorId, traits));
                return true;
            }

            BlockData block = level.Blocks[extendIndex];
            if (cells.Count == block.Cells.Count)
            {
                return false;
            }

            level.ReplaceBlockAt(extendIndex, new BlockData(cells, Vector2Int.zero, block.ColorId, block.Traits));
            return true;
        }

        /// <summary>Removes the block covering <paramref name="cell"/>; returns whether there was one.</summary>
        public static bool RemoveBlockAt(LevelData level, Vector2Int cell)
        {
            int index = FindBlockAt(level, cell);
            if (index < 0)
            {
                return false;
            }

            level.RemoveBlockAt(index);
            return true;
        }

        /// <summary>Index of the door covering <paramref name="slot"/> on <paramref name="edge"/>, or -1.</summary>
        public static int FindDoorAt(LevelData level, Direction edge, int slot)
        {
            IReadOnlyList<DoorData> doors = level.Doors;
            for (int i = 0; i < doors.Count; i++)
            {
                if (doors[i].Edge == edge && doors[i].Covers(slot))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Paints one door cell of <paramref name="colorId"/> at <paramref name="slot"/>, replacing whatever door covered
        /// that cell; returns whether the level changed.
        /// </summary>
        public static bool PlaceDoor(LevelData level, Direction edge, int slot, int colorId)
        {
            if (slot < 0 || slot >= level.EdgeLength(edge))
            {
                return false;
            }

            int index = FindDoorAt(level, edge, slot);
            if (index >= 0 && level.Doors[index].ColorId == colorId)
            {
                return false;
            }

            RemoveDoorAt(level, edge, slot);
            level.AddDoor(new DoorData(edge, slot, 1, colorId));
            return true;
        }

        /// <summary>
        /// Removes the door cell at <paramref name="slot"/>; a longer door keeps its cells on either side. Returns
        /// whether there was a door there.
        /// </summary>
        public static bool RemoveDoorAt(LevelData level, Direction edge, int slot)
        {
            int index = FindDoorAt(level, edge, slot);
            if (index < 0)
            {
                return false;
            }

            DoorData door = level.Doors[index];
            level.RemoveDoorAt(index);
            if (slot > door.Offset)
            {
                level.AddDoor(new DoorData(edge, door.Offset, slot - door.Offset, door.ColorId));
            }

            int end = door.Offset + door.Length;
            if (slot + 1 < end)
            {
                level.AddDoor(new DoorData(edge, slot + 1, end - slot - 1, door.ColorId));
            }

            return true;
        }

        /// <summary>Removes every block and door.</summary>
        public static void Clear(LevelData level)
        {
            level.Clear();
        }

        /// <summary>Sets the time limit of one attempt.</summary>
        public static void SetTimer(LevelData level, int seconds)
        {
            level.TimerSeconds = seconds;
        }

        /// <summary>
        /// Changes the board size and drops the blocks and doors that no longer fit; returns how many were dropped.
        /// </summary>
        public static int Resize(LevelData level, int width, int height)
        {
            level.SetSize(width, height);
            int removed = 0;

            IReadOnlyList<BlockData> blocks = level.Blocks;
            for (int i = blocks.Count - 1; i >= 0; i--)
            {
                Vector2Int end = blocks[i].Origin + blocks[i].Size;
                if (end.x > width || end.y > height)
                {
                    level.RemoveBlockAt(i);
                    removed++;
                }
            }

            // Door cells past the new edge go; a longer door keeps the cells that still fit.
            IReadOnlyList<DoorData> doors = level.Doors;
            for (int i = doors.Count - 1; i >= 0; i--)
            {
                DoorData door = doors[i];
                int edgeLength = level.EdgeLength(door.Edge);
                if (door.Offset + door.Length <= edgeLength)
                {
                    continue;
                }

                level.RemoveDoorAt(i);
                removed++;
                if (door.Offset < edgeLength)
                {
                    level.AddDoor(new DoorData(door.Edge, door.Offset, edgeLength - door.Offset, door.ColorId));
                }
            }

            return removed;
        }

        private static bool IsFree(LevelData level, Vector2Int cell)
        {
            bool inside = cell.x >= 0 && cell.y >= 0 && cell.x < level.Width && cell.y < level.Height;
            return inside && FindBlockAt(level, cell) < 0;
        }
    }
}
