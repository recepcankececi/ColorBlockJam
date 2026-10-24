using System.Collections.Generic;
using CBJ.Core;
using UnityEngine;

namespace CBJ.Editor
{
    /// <summary>
    /// The edits the level editor makes to a <see cref="LevelData"/>. Every edit keeps the level a legal board:
    /// blocks stay inside and never overlap, doors stay on their edge and never overlap on the same edge.
    /// </summary>
    public static class LevelEditOperations
    {
        /// <summary>Index of the block covering <paramref name="cell"/>, or -1.</summary>
        public static int FindBlockAt(LevelData level, Vector2Int cell)
        {
            IReadOnlyList<BlockData> blocks = level.Blocks;
            for (int i = 0; i < blocks.Count; i++)
            {
                if (Covers(blocks[i], cell))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Origin that puts the shape's top-left filled cell on <paramref name="clicked"/>, so a click always lands on
        /// the block it places, even for shapes whose bounding-box corner is empty.
        /// </summary>
        public static Vector2Int OriginForClick(string shapeId, Vector2Int clicked)
        {
            return clicked - ShapeLibrary.Get(shapeId).Cells[0];
        }

        /// <summary>True when the shape fits inside the board at <paramref name="origin"/> without overlapping.</summary>
        public static bool CanPlaceBlock(LevelData level, string shapeId, Vector2Int origin)
        {
            IReadOnlyList<Vector2Int> cells = ShapeLibrary.Get(shapeId).Cells;
            for (int i = 0; i < cells.Count; i++)
            {
                Vector2Int cell = origin + cells[i];
                if (!IsInside(level, cell) || FindBlockAt(level, cell) >= 0)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Adds a block when it fits; returns whether it was added.</summary>
        public static bool TryPlaceBlock(LevelData level, string shapeId, Vector2Int origin, int colorId)
        {
            if (!CanPlaceBlock(level, shapeId, origin))
            {
                return false;
            }

            level.AddBlock(new BlockData(shapeId, origin, colorId));
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
        /// The door <see cref="PlaceDoor"/> would add: starts at <paramref name="slot"/>, shifted back and shortened
        /// as needed so it fits the edge.
        /// </summary>
        public static DoorData FitDoor(LevelData level, Direction edge, int slot, int length, int colorId)
        {
            int edgeLength = level.EdgeLength(edge);
            int fitted = Mathf.Clamp(length, 1, edgeLength);
            int offset = Mathf.Clamp(slot, 0, edgeLength - fitted);
            return new DoorData(edge, offset, fitted, colorId);
        }

        /// <summary>Adds the fitted door and removes any door it overlaps on the same edge.</summary>
        public static void PlaceDoor(LevelData level, Direction edge, int slot, int length, int colorId)
        {
            DoorData door = FitDoor(level, edge, slot, length, colorId);
            IReadOnlyList<DoorData> doors = level.Doors;
            for (int i = doors.Count - 1; i >= 0; i--)
            {
                if (doors[i].Edge == edge && Overlaps(doors[i], door))
                {
                    level.RemoveDoorAt(i);
                }
            }

            level.AddDoor(door);
        }

        /// <summary>Removes the door covering <paramref name="slot"/>; returns whether there was one.</summary>
        public static bool RemoveDoorAt(LevelData level, Direction edge, int slot)
        {
            int index = FindDoorAt(level, edge, slot);
            if (index < 0)
            {
                return false;
            }

            level.RemoveDoorAt(index);
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
                BlockShape shape = blocks[i].Shape;
                Vector2Int origin = blocks[i].Origin;
                if (origin.x + shape.Width > width || origin.y + shape.Height > height)
                {
                    level.RemoveBlockAt(i);
                    removed++;
                }
            }

            IReadOnlyList<DoorData> doors = level.Doors;
            for (int i = doors.Count - 1; i >= 0; i--)
            {
                if (doors[i].Offset + doors[i].Length > level.EdgeLength(doors[i].Edge))
                {
                    level.RemoveDoorAt(i);
                    removed++;
                }
            }

            return removed;
        }

        private static bool IsInside(LevelData level, Vector2Int cell)
        {
            return cell.x >= 0 && cell.y >= 0 && cell.x < level.Width && cell.y < level.Height;
        }

        private static bool Covers(BlockData block, Vector2Int cell)
        {
            IReadOnlyList<Vector2Int> cells = block.Shape.Cells;
            Vector2Int local = cell - block.Origin;
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i] == local)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Overlaps(DoorData a, DoorData b)
        {
            return a.Offset < b.Offset + b.Length && b.Offset < a.Offset + a.Length;
        }
    }
}
