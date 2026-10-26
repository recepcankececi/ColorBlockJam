using System;
using System.Collections.Generic;
using UnityEngine;

namespace CBJ.Core
{
    /// <summary>
    /// Static checks on a <see cref="LevelData"/> before it is saved or played (O1). Errors are data
    /// <see cref="LevelData.BuildBoard"/> would reject or the editor never writes (an unknown block kind, ice layers
    /// outside 1 to <see cref="BlockTraits.MaxIceLayers"/>); warnings are levels that build but cannot be won, such as
    /// a block with no door of its colour wide enough to leave through, an arrow block with no such door on its axis,
    /// or an ice block that runs out of other blocks to thaw it. Solvability beyond that is not searched.
    /// </summary>
    public static class LevelValidator
    {
        /// <summary>Every issue in <paramref name="level"/>, errors first in the order they were found.</summary>
        public static List<LevelIssue> Validate(LevelData level)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            var issues = new List<LevelIssue>();
            if (level.Width < 1 || level.Height < 1)
            {
                issues.Add(Error($"The board size {level.Width}x{level.Height} is empty."));
                return issues;
            }

            if (level.TimerSeconds < 1)
            {
                issues.Add(Error("The timer must be at least one second."));
            }

            if (level.Blocks.Count == 0)
            {
                issues.Add(Error("The level has no blocks."));
            }

            bool doorsValid = CheckDoors(level, issues);
            bool blocksValid = CheckBlocks(level, issues);
            if (doorsValid && blocksValid)
            {
                CheckExits(level, issues);
                CheckThaw(level, issues);
            }

            return issues;
        }

        /// <summary>True when any issue is an <see cref="IssueSeverity.Error"/>.</summary>
        public static bool HasErrors(IReadOnlyList<LevelIssue> issues)
        {
            if (issues == null)
            {
                throw new ArgumentNullException(nameof(issues));
            }

            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].Severity == IssueSeverity.Error)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CheckDoors(LevelData level, List<LevelIssue> issues)
        {
            int before = issues.Count;
            IReadOnlyList<DoorData> doors = level.Doors;
            var inRange = new List<int>(doors.Count);
            for (int i = 0; i < doors.Count; i++)
            {
                DoorData door = doors[i];
                if (!Enum.IsDefined(typeof(Direction), door.Edge) || door.Offset < 0 || door.Length < 1 ||
                    door.Offset + door.Length > level.EdgeLength(door.Edge))
                {
                    issues.Add(Error($"Door {i + 1} lies outside its edge (edge {door.Edge}, cell {door.Offset}, length {door.Length})."));
                    continue;
                }

                foreach (int j in inRange)
                {
                    DoorData other = doors[j];
                    if (other.Edge == door.Edge && other.Offset < door.Offset + door.Length && door.Offset < other.Offset + other.Length)
                    {
                        issues.Add(Error($"Doors {j + 1} and {i + 1} overlap on the {door.Edge} edge."));
                    }
                }

                inRange.Add(i);
            }

            return issues.Count == before;
        }

        private static bool CheckBlocks(LevelData level, List<LevelIssue> issues)
        {
            int before = issues.Count;
            IReadOnlyList<BlockData> blocks = level.Blocks;
            var occupied = new Dictionary<Vector2Int, int>();
            for (int i = 0; i < blocks.Count; i++)
            {
                IReadOnlyList<Vector2Int> cells = blocks[i].Cells;
                if (cells.Count == 0)
                {
                    issues.Add(Error($"Block {i + 1} has no cells."));
                    continue;
                }

                if (new HashSet<Vector2Int>(cells).Count != cells.Count)
                {
                    issues.Add(Error($"Block {i + 1} lists a cell twice."));
                }
                else if (!CellGroup.IsConnected(cells))
                {
                    issues.Add(Error($"Block {i + 1} has cells that do not touch the rest of the block."));
                }
                else if (!IsNormalised(cells))
                {
                    issues.Add(Error($"Block {i + 1} stores cells away from its origin; the game would place it elsewhere."));
                }

                CheckKind(blocks[i].Traits, i, issues);

                bool reportedOverlap = false;
                bool reportedOutside = false;
                for (int c = 0; c < cells.Count; c++)
                {
                    Vector2Int cell = blocks[i].Origin + cells[c];
                    if (!IsInside(level, cell))
                    {
                        if (!reportedOutside)
                        {
                            issues.Add(Error($"Block {i + 1} lies partly outside the board."));
                            reportedOutside = true;
                        }
                    }
                    else if (occupied.TryGetValue(cell, out int owner) && owner != i)
                    {
                        if (!reportedOverlap)
                        {
                            issues.Add(Error($"Blocks {owner + 1} and {i + 1} overlap."));
                            reportedOverlap = true;
                        }
                    }
                    else
                    {
                        occupied[cell] = i;
                    }
                }
            }

            return issues.Count == before;
        }

        private static void CheckKind(BlockTraits traits, int index, List<LevelIssue> issues)
        {
            if (!Enum.IsDefined(typeof(BlockKind), traits.Kind))
            {
                issues.Add(Error($"Block {index + 1} has an unknown kind ({(int)traits.Kind})."));
            }
            else if (traits.Kind == BlockKind.Arrow && !Enum.IsDefined(typeof(Axis), traits.ArrowAxis))
            {
                issues.Add(Error($"Arrow block {index + 1} has an unknown axis ({(int)traits.ArrowAxis})."));
            }
            else if (traits.Kind == BlockKind.Ice && (traits.IceLayers < 1 || traits.IceLayers > BlockTraits.MaxIceLayers))
            {
                issues.Add(Error($"Ice block {index + 1} has {traits.IceLayers} layers; it needs 1 to {BlockTraits.MaxIceLayers}."));
            }
        }

        // A block leaves through a door only when its whole width along that edge fits the door's span (ADR-002); an
        // arrow block can only use the two edges of its axis (ADR-008).
        private static void CheckExits(LevelData level, List<LevelIssue> issues)
        {
            Door[] doors = level.MergeDoors();
            IReadOnlyList<BlockData> blocks = level.Blocks;
            for (int i = 0; i < blocks.Count; i++)
            {
                BlockTraits traits = blocks[i].Traits;
                bool arrow = traits.Kind == BlockKind.Arrow;
                Vector2Int size = blocks[i].Size;
                bool canExit = false;
                for (int d = 0; d < doors.Length && !canExit; d++)
                {
                    bool vertical = doors[d].Edge.IsVertical();
                    int needed = vertical ? size.x : size.y;
                    canExit = doors[d].ColorId == blocks[i].ColorId && doors[d].Length >= needed &&
                              (!arrow || doors[d].Edge.ToAxis() == traits.ArrowAxis);
                }

                if (!canExit && arrow)
                {
                    issues.Add(Warning(traits.ArrowAxis == Axis.Vertical
                        ? $"Arrow block {i + 1} has no door of its colour {size.x} cells wide on the top or bottom edge."
                        : $"Arrow block {i + 1} has no door of its colour {size.y} cells wide on the left or right edge."));
                }
                else if (!canExit)
                {
                    issues.Add(Warning($"Block {i + 1} has no door of its colour wide enough to exit " +
                                       $"({size.x} cells on the top or bottom edge, {size.y} on the left or right)."));
                }
            }
        }

        // Only blocks that can move thaw ice, and a frozen block cannot move: assume every non-ice block leaves, then
        // the ice blocks leave in order of their layers. The ice block at position i in that order has the non-ice
        // blocks and the i ice blocks before it to thaw it; from the first one that runs short, none can thaw.
        private static void CheckThaw(LevelData level, List<LevelIssue> issues)
        {
            IReadOnlyList<BlockData> blocks = level.Blocks;
            var ice = new List<int>();
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i].Traits.Kind == BlockKind.Ice)
                {
                    ice.Add(i);
                }
            }

            ice.Sort((a, b) => blocks[a].Traits.IceLayers.CompareTo(blocks[b].Traits.IceLayers));
            int thawers = blocks.Count - ice.Count;
            bool stuck = false;
            for (int order = 0; order < ice.Count; order++)
            {
                int index = ice[order];
                stuck |= blocks[index].Traits.IceLayers > thawers + order;
                if (stuck)
                {
                    issues.Add(Warning($"Ice block {index + 1} can never thaw: it has {blocks[index].Traits.IceLayers} " +
                                       "layers but not enough other blocks can leave before it."));
                }
            }
        }

        // BlockShape re-normalises cells, so data whose corner is not (0,0) would move when the game builds the board.
        private static bool IsNormalised(IReadOnlyList<Vector2Int> cells)
        {
            Vector2Int min = cells[0];
            for (int i = 1; i < cells.Count; i++)
            {
                min = Vector2Int.Min(min, cells[i]);
            }

            return min == Vector2Int.zero;
        }

        private static bool IsInside(LevelData level, Vector2Int cell)
        {
            return cell.x >= 0 && cell.y >= 0 && cell.x < level.Width && cell.y < level.Height;
        }

        private static LevelIssue Error(string message)
        {
            return new LevelIssue(IssueSeverity.Error, message);
        }

        private static LevelIssue Warning(string message)
        {
            return new LevelIssue(IssueSeverity.Warning, message);
        }
    }
}
