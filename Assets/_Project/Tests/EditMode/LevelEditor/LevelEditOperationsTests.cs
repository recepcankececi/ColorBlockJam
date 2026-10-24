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
            bool placed = LevelEditOperations.TryPlaceBlock(_level, "I2-H", new Vector2Int(1, 1), Red);

            Assert.That((placed, _level.Blocks.Count), Is.EqualTo((true, 1)));
        }

        [Test]
        public void TryPlaceBlock_OverlapsBlock_IsRejected()
        {
            LevelEditOperations.TryPlaceBlock(_level, "I2-H", new Vector2Int(1, 1), Red);

            bool placed = LevelEditOperations.TryPlaceBlock(_level, "I2-V", new Vector2Int(2, 0), Blue);

            Assert.That((placed, _level.Blocks.Count), Is.EqualTo((false, 1)));
        }

        [Test]
        public void TryPlaceBlock_RunsPastEdge_IsRejected()
        {
            bool placed = LevelEditOperations.TryPlaceBlock(_level, "I3-H", new Vector2Int(3, 0), Red);

            Assert.That(placed, Is.False);
        }

        [Test]
        public void TryPlaceBlock_IntoNotchOfConcaveShape_IsAccepted()
        {
            LevelEditOperations.TryPlaceBlock(_level, "L3-0", Vector2Int.zero, Red);

            bool placed = LevelEditOperations.TryPlaceBlock(_level, "I1", Vector2Int.one, Blue);

            Assert.That(placed, Is.True);
        }

        [Test]
        public void OriginForClick_ShapeWithEmptyCorner_CoversClickedCell()
        {
            Vector2Int clicked = new Vector2Int(2, 3);
            Vector2Int origin = LevelEditOperations.OriginForClick("L3-180", clicked);

            LevelEditOperations.TryPlaceBlock(_level, "L3-180", origin, Red);

            Assert.That(LevelEditOperations.FindBlockAt(_level, clicked), Is.EqualTo(0));
        }

        [Test]
        public void FindBlockAt_AnyCellOfShape_FindsTheBlock()
        {
            LevelEditOperations.TryPlaceBlock(_level, "L3-0", Vector2Int.zero, Red);

            Assert.That(LevelEditOperations.FindBlockAt(_level, Vector2Int.up), Is.EqualTo(0));
        }

        [Test]
        public void RemoveBlockAt_CellOfBlock_RemovesWholeBlock()
        {
            LevelEditOperations.TryPlaceBlock(_level, "I3-V", Vector2Int.zero, Red);

            LevelEditOperations.RemoveBlockAt(_level, new Vector2Int(0, 2));

            Assert.That(_level.Blocks, Is.Empty);
        }

        [Test]
        public void RemoveBlockAt_EmptyCell_ReturnsFalse()
        {
            Assert.That(LevelEditOperations.RemoveBlockAt(_level, Vector2Int.zero), Is.False);
        }

        [Test]
        public void PlaceDoor_FreeEdge_AddsDoorAtSlot()
        {
            LevelEditOperations.PlaceDoor(_level, Direction.Up, 1, 2, Red);

            Assert.That(_level.Doors, Is.EqualTo(new[] { new DoorData(Direction.Up, 1, 2, Red) }));
        }

        [Test]
        public void PlaceDoor_NearEdgeEnd_ShiftsBackToFit()
        {
            LevelEditOperations.PlaceDoor(_level, Direction.Left, 4, 3, Red);

            Assert.That(_level.Doors[0].Offset, Is.EqualTo(2));
        }

        [Test]
        public void PlaceDoor_LongerThanEdge_IsShortenedToEdge()
        {
            LevelEditOperations.PlaceDoor(_level, Direction.Down, 0, 9, Red);

            Assert.That(_level.Doors[0].Length, Is.EqualTo(5));
        }

        [Test]
        public void PlaceDoor_OverlapsDoorOnSameEdge_ReplacesIt()
        {
            LevelEditOperations.PlaceDoor(_level, Direction.Up, 0, 2, Red);

            LevelEditOperations.PlaceDoor(_level, Direction.Up, 1, 2, Blue);

            Assert.That(_level.Doors, Is.EqualTo(new[] { new DoorData(Direction.Up, 1, 2, Blue) }));
        }

        [Test]
        public void PlaceDoor_SameSlotOnOtherEdge_KeepsBoth()
        {
            LevelEditOperations.PlaceDoor(_level, Direction.Up, 0, 1, Red);

            LevelEditOperations.PlaceDoor(_level, Direction.Down, 0, 1, Blue);

            Assert.That(_level.Doors.Count, Is.EqualTo(2));
        }

        [Test]
        public void RemoveDoorAt_SlotInsideDoor_RemovesDoor()
        {
            LevelEditOperations.PlaceDoor(_level, Direction.Right, 1, 3, Red);

            LevelEditOperations.RemoveDoorAt(_level, Direction.Right, 3);

            Assert.That(_level.Doors, Is.Empty);
        }

        [Test]
        public void Resize_Smaller_DropsBlocksThatNoLongerFit()
        {
            LevelEditOperations.TryPlaceBlock(_level, "I1", Vector2Int.zero, Red);
            LevelEditOperations.TryPlaceBlock(_level, "I2-H", new Vector2Int(3, 0), Blue);

            LevelEditOperations.Resize(_level, 4, 5);

            Assert.That(_level.Blocks, Is.EqualTo(new[] { new BlockData("I1", Vector2Int.zero, Red) }));
        }

        [Test]
        public void Resize_Smaller_DropsDoorsPastTheirEdge()
        {
            LevelEditOperations.PlaceDoor(_level, Direction.Left, 3, 2, Red);
            LevelEditOperations.PlaceDoor(_level, Direction.Up, 3, 2, Blue);

            LevelEditOperations.Resize(_level, 5, 4);

            Assert.That(_level.Doors, Is.EqualTo(new[] { new DoorData(Direction.Up, 3, 2, Blue) }));
        }

        [Test]
        public void Resize_Smaller_ReturnsNumberDropped()
        {
            LevelEditOperations.TryPlaceBlock(_level, "I1", new Vector2Int(4, 4), Red);
            LevelEditOperations.PlaceDoor(_level, Direction.Up, 4, 1, Red);

            int removed = LevelEditOperations.Resize(_level, 4, 4);

            Assert.That(removed, Is.EqualTo(2));
        }

        [Test]
        public void Resize_EditedLevel_StillBuildsABoard()
        {
            LevelEditOperations.TryPlaceBlock(_level, "O2", new Vector2Int(3, 3), Red);
            LevelEditOperations.PlaceDoor(_level, Direction.Right, 3, 2, Red);

            LevelEditOperations.Resize(_level, 4, 4);

            Assert.DoesNotThrow(() => _level.BuildBoard());
        }
    }
}
