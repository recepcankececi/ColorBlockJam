using System.Collections.Generic;
using CBJ.Core;
using CBJ.Editor;
using NUnit.Framework;
using UnityEngine;

namespace CBJ.Tests.EditMode.LevelEditor
{
    public class LevelEditOperationsTests
    {
        private const int Red = 0;
        private const int Blue = 1;

        private LevelData _level;

        [SetUp]
        public void CreateLevel()
        {
            _level = new LevelData(5, 5, 60);
        }

        [Test]
        public void TryPlaceBlock_FreeCells_AddsBlock()
        {
            bool placed = LevelEditOperations.TryPlaceBlock(_level, Cells("I2-H"), new Vector2Int(1, 1), Red);

            Assert.That((placed, _level.Blocks.Count), Is.EqualTo((true, 1)));
        }

        [Test]
        public void TryPlaceBlock_OverlapsBlock_IsRejected()
        {
            LevelEditOperations.TryPlaceBlock(_level, Cells("I2-H"), new Vector2Int(1, 1), Red);

            bool placed = LevelEditOperations.TryPlaceBlock(_level, Cells("I2-V"), new Vector2Int(2, 0), Blue);

            Assert.That((placed, _level.Blocks.Count), Is.EqualTo((false, 1)));
        }

        [Test]
        public void TryPlaceBlock_RunsPastEdge_IsRejected()
        {
            bool placed = LevelEditOperations.TryPlaceBlock(_level, Cells("I3-H"), new Vector2Int(3, 0), Red);

            Assert.That(placed, Is.False);
        }

        [Test]
        public void TryPlaceBlock_IntoNotchOfConcaveShape_IsAccepted()
        {
            LevelEditOperations.TryPlaceBlock(_level, Cells("L3-0"), Vector2Int.zero, Red);

            bool placed = LevelEditOperations.TryPlaceBlock(_level, Cells("I1"), Vector2Int.one, Blue);

            Assert.That(placed, Is.True);
        }

        [Test]
        public void OriginForClick_ShapeWithEmptyCorner_CoversClickedCell()
        {
            Vector2Int clicked = new Vector2Int(2, 3);
            Vector2Int origin = LevelEditOperations.OriginForClick(Cells("L3-180"), clicked);

            LevelEditOperations.TryPlaceBlock(_level, Cells("L3-180"), origin, Red);

            Assert.That(LevelEditOperations.FindBlockAt(_level, clicked), Is.EqualTo(0));
        }

        [Test]
        public void FindBlockAt_AnyCellOfShape_FindsTheBlock()
        {
            LevelEditOperations.TryPlaceBlock(_level, Cells("L3-0"), Vector2Int.zero, Red);

            Assert.That(LevelEditOperations.FindBlockAt(_level, Vector2Int.up), Is.EqualTo(0));
        }

        [Test]
        public void RemoveBlockAt_CellOfBlock_RemovesWholeBlock()
        {
            LevelEditOperations.TryPlaceBlock(_level, Cells("I3-V"), Vector2Int.zero, Red);

            LevelEditOperations.RemoveBlockAt(_level, new Vector2Int(0, 2));

            Assert.That(_level.Blocks, Is.Empty);
        }

        [Test]
        public void RemoveBlockAt_EmptyCell_ReturnsFalse()
        {
            Assert.That(LevelEditOperations.RemoveBlockAt(_level, Vector2Int.zero), Is.False);
        }

        [Test]
        public void ApplyStroke_ConnectedCells_AddsOneBlockCoveringThem()
        {
            var stroke = new[] { new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(2, 2) };

            LevelEditOperations.ApplyStroke(_level, stroke, -1, Blue, out _);

            Assert.That(_level.Blocks, Is.EqualTo(new[] { new BlockData(stroke, Vector2Int.zero, Blue) }));
        }

        [Test]
        public void ApplyStroke_CellNotConnectedToStart_IsDropped()
        {
            var stroke = new[] { Vector2Int.zero, Vector2Int.right, new Vector2Int(3, 3) };

            LevelEditOperations.ApplyStroke(_level, stroke, -1, Red, out int dropped);

            Assert.That((_level.Blocks[0].Cells.Count, dropped), Is.EqualTo((2, 1)));
        }

        [Test]
        public void ApplyStroke_ThroughAnotherBlock_KeepsOnlyCellsConnectedToStart()
        {
            LevelEditOperations.TryPlaceBlock(_level, Cells("I1"), new Vector2Int(1, 0), Blue);
            var stroke = new[] { Vector2Int.zero, new Vector2Int(1, 0), new Vector2Int(2, 0) };

            LevelEditOperations.ApplyStroke(_level, stroke, -1, Red, out int dropped);

            Assert.That((_level.Blocks[1].Cells.Count, dropped), Is.EqualTo((1, 1)));
        }

        [Test]
        public void ApplyStroke_OnlyTakenCells_ChangesNothing()
        {
            LevelEditOperations.TryPlaceBlock(_level, Cells("I1"), Vector2Int.zero, Blue);

            bool changed = LevelEditOperations.ApplyStroke(_level, new[] { Vector2Int.zero }, -1, Red, out _);

            Assert.That((changed, _level.Blocks.Count), Is.EqualTo((false, 1)));
        }

        [Test]
        public void ApplyStroke_ExtendingBlock_AddsCellsAndKeepsItsColour()
        {
            LevelEditOperations.TryPlaceBlock(_level, Cells("I1"), Vector2Int.zero, Blue);

            LevelEditOperations.ApplyStroke(_level, new[] { Vector2Int.zero, Vector2Int.up }, 0, Red, out _);

            BlockData grown = _level.Blocks[0];
            Assert.That((grown.Cells.Count, grown.ColorId, _level.Blocks.Count), Is.EqualTo((2, Blue, 1)));
        }

        [Test]
        public void ApplyStroke_ExtendingWithLooseCell_DropsIt()
        {
            LevelEditOperations.TryPlaceBlock(_level, Cells("I1"), Vector2Int.zero, Blue);

            LevelEditOperations.ApplyStroke(_level, new[] { Vector2Int.zero, Vector2Int.up, new Vector2Int(3, 3) }, 0, Blue, out int dropped);

            Assert.That((_level.Blocks[0].Cells.Count, dropped), Is.EqualTo((2, 1)));
        }

        [Test]
        public void PlaceDoor_FreeSlot_AddsOneCellDoor()
        {
            LevelEditOperations.PlaceDoor(_level, Direction.Up, 1, Red);

            Assert.That(_level.Doors, Is.EqualTo(new[] { new DoorData(Direction.Up, 1, 1, Red) }));
        }

        [Test]
        public void PlaceDoor_SameColourAgain_ReturnsFalse()
        {
            LevelEditOperations.PlaceDoor(_level, Direction.Up, 1, Red);

            Assert.That(LevelEditOperations.PlaceDoor(_level, Direction.Up, 1, Red), Is.False);
        }

        [Test]
        public void PlaceDoor_InsideLongerDoorOfOtherColour_RepaintsOnlyThatCell()
        {
            _level.AddDoor(new DoorData(Direction.Up, 0, 3, Red));

            LevelEditOperations.PlaceDoor(_level, Direction.Up, 1, Blue);

            Assert.That(_level.Doors, Is.EquivalentTo(new[]
            {
                new DoorData(Direction.Up, 0, 1, Red), new DoorData(Direction.Up, 2, 1, Red), new DoorData(Direction.Up, 1, 1, Blue)
            }));
        }

        [Test]
        public void PlaceDoor_SameSlotOnOtherEdge_KeepsBoth()
        {
            LevelEditOperations.PlaceDoor(_level, Direction.Up, 0, Red);

            LevelEditOperations.PlaceDoor(_level, Direction.Down, 0, Blue);

            Assert.That(_level.Doors.Count, Is.EqualTo(2));
        }

        [Test]
        public void PlaceDoor_SlotPastEdge_IsRejected()
        {
            Assert.That(LevelEditOperations.PlaceDoor(_level, Direction.Left, 5, Red), Is.False);
        }

        [Test]
        public void RemoveDoorAt_MiddleOfLongerDoor_KeepsCellsOnEitherSide()
        {
            _level.AddDoor(new DoorData(Direction.Right, 1, 3, Red));

            LevelEditOperations.RemoveDoorAt(_level, Direction.Right, 2);

            Assert.That(_level.Doors, Is.EquivalentTo(new[] { new DoorData(Direction.Right, 1, 1, Red), new DoorData(Direction.Right, 3, 1, Red) }));
        }

        [Test]
        public void RemoveDoorAt_OneCellDoor_RemovesIt()
        {
            LevelEditOperations.PlaceDoor(_level, Direction.Right, 3, Red);

            LevelEditOperations.RemoveDoorAt(_level, Direction.Right, 3);

            Assert.That(_level.Doors, Is.Empty);
        }

        [Test]
        public void Resize_Smaller_DropsBlocksThatNoLongerFit()
        {
            LevelEditOperations.TryPlaceBlock(_level, Cells("I1"), Vector2Int.zero, Red);
            LevelEditOperations.TryPlaceBlock(_level, Cells("I2-H"), new Vector2Int(3, 0), Blue);

            LevelEditOperations.Resize(_level, 4, 5);

            Assert.That(_level.Blocks, Is.EqualTo(new[] { new BlockData(Cells("I1"), Vector2Int.zero, Red) }));
        }

        [Test]
        public void Resize_Smaller_DropsDoorsPastTheirEdge()
        {
            _level.AddDoor(new DoorData(Direction.Left, 4, 1, Red));
            _level.AddDoor(new DoorData(Direction.Up, 3, 2, Blue));

            LevelEditOperations.Resize(_level, 5, 4);

            Assert.That(_level.Doors, Is.EqualTo(new[] { new DoorData(Direction.Up, 3, 2, Blue) }));
        }

        [Test]
        public void Resize_Smaller_ShortensLongerDoorToItsEdge()
        {
            _level.AddDoor(new DoorData(Direction.Left, 2, 3, Red));

            LevelEditOperations.Resize(_level, 5, 4);

            Assert.That(_level.Doors, Is.EqualTo(new[] { new DoorData(Direction.Left, 2, 2, Red) }));
        }

        [Test]
        public void Resize_Smaller_ReturnsNumberDropped()
        {
            LevelEditOperations.TryPlaceBlock(_level, Cells("I1"), new Vector2Int(4, 4), Red);
            LevelEditOperations.PlaceDoor(_level, Direction.Up, 4, Red);

            int removed = LevelEditOperations.Resize(_level, 4, 4);

            Assert.That(removed, Is.EqualTo(2));
        }

        [Test]
        public void Resize_EditedLevel_StillBuildsABoard()
        {
            LevelEditOperations.TryPlaceBlock(_level, Cells("O2"), new Vector2Int(3, 3), Red);
            LevelEditOperations.PlaceDoor(_level, Direction.Right, 3, Red);

            LevelEditOperations.Resize(_level, 4, 4);

            Assert.DoesNotThrow(() => _level.BuildBoard());
        }

        private static IReadOnlyList<Vector2Int> Cells(string presetId)
        {
            return ShapeLibrary.Get(presetId).Cells;
        }
    }
}
