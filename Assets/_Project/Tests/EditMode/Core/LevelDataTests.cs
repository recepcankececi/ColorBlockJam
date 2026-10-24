using System;
using System.Linq;
using System.Reflection;
using CBJ.Core;
using NUnit.Framework;
using UnityEngine;

namespace CBJ.Tests.EditMode.Core
{
    public class LevelDataTests
    {
        private const int Red = 0;
        private const int Blue = 1;

        [Test]
        public void JsonRoundTrip_FullLevel_KeepsSizeAndTimer()
        {
            LevelData copy = RoundTrip(SampleLevel());

            Assert.That((copy.Width, copy.Height, copy.TimerSeconds), Is.EqualTo((5, 6, 45)));
        }

        [Test]
        public void JsonRoundTrip_FullLevel_KeepsDoors()
        {
            LevelData level = SampleLevel();

            LevelData copy = RoundTrip(level);

            Assert.That(copy.Doors, Is.EqualTo(level.Doors));
        }

        [Test]
        public void JsonRoundTrip_FullLevel_KeepsBlocks()
        {
            LevelData level = SampleLevel();

            LevelData copy = RoundTrip(level);

            Assert.That(copy.Blocks, Is.EqualTo(level.Blocks));
        }

        [Test]
        public void BuildBoard_FullLevel_PlacesBlocksAtTheirOrigins()
        {
            BoardModel board = SampleLevel().BuildBoard();

            Assert.That(board.GetBlockAt(new Vector2Int(3, 4)), Is.SameAs(board.Blocks[1]));
        }

        [Test]
        public void BuildBoard_FullLevel_KeepsBlockColours()
        {
            BoardModel board = SampleLevel().BuildBoard();

            Assert.That((board.Blocks[0].ColorId, board.Blocks[1].ColorId), Is.EqualTo((Red, Blue)));
        }

        [Test]
        public void BuildBoard_FullLevel_CreatesDoorsFromData()
        {
            BoardModel board = SampleLevel().BuildBoard();
            Door door = board.Doors[0];

            Assert.That((door.Edge, door.Offset, door.Length, door.ColorId), Is.EqualTo((Direction.Up, 1, 2, Red)));
        }

        [Test]
        public void BuildBoard_CalledTwice_ReturnsIndependentBoards()
        {
            LevelData level = SampleLevel();
            BoardModel first = level.BuildBoard();
            new MoveResolver(first).TryStep(first.Blocks[0], Direction.Right);

            BoardModel second = level.BuildBoard();

            Assert.That(second.Blocks[0].Origin, Is.EqualTo(Vector2Int.zero));
        }

        [Test]
        public void BuildBoard_UnknownShape_Throws()
        {
            var level = new LevelData(4, 4, 30);
            level.AddBlock(new BlockData("no-such-shape", Vector2Int.zero, Red));

            Assert.Throws<ArgumentException>(() => level.BuildBoard());
        }

        [Test]
        public void BuildBoard_AdjacentOneCellDoors_LetTwoWideBlockExit()
        {
            var level = new LevelData(4, 3, 30);
            level.AddDoor(new DoorData(Direction.Up, 1, 1, Red));
            level.AddDoor(new DoorData(Direction.Up, 2, 1, Red));
            level.AddBlock(new BlockData("I2-H", new Vector2Int(1, 2), Red));
            BoardModel board = level.BuildBoard();

            Assert.That(new MoveResolver(board).TryStep(board.Blocks[0], Direction.Up), Is.EqualTo(StepResult.Exited));
        }

        [Test]
        public void BuildBoard_AdjacentDoorsOfDifferentColours_StaySeparate()
        {
            var level = new LevelData(4, 3, 30);
            level.AddDoor(new DoorData(Direction.Up, 1, 1, Red));
            level.AddDoor(new DoorData(Direction.Up, 2, 1, Blue));

            Assert.That(level.BuildBoard().Doors.Count, Is.EqualTo(2));
        }

        [Test]
        public void BuildBoard_SameColourDoorsWithGap_StaySeparate()
        {
            var level = new LevelData(4, 3, 30);
            level.AddDoor(new DoorData(Direction.Up, 0, 1, Red));
            level.AddDoor(new DoorData(Direction.Up, 2, 1, Red));

            Assert.That(level.BuildBoard().Doors.Count, Is.EqualTo(2));
        }

        [Test]
        public void BuildBoard_AdjacentDoorsOnLeftEdge_MergeIntoOneDoor()
        {
            var level = new LevelData(3, 4, 30);
            level.AddDoor(new DoorData(Direction.Left, 1, 1, Red));
            level.AddDoor(new DoorData(Direction.Left, 2, 1, Red));
            Door door = level.BuildBoard().Doors.Single();

            Assert.That((door.Edge, door.Offset, door.Length, door.ColorId), Is.EqualTo((Direction.Left, 1, 2, Red)));
        }

        [Test]
        public void BuildBoard_AdjacentDoorsOnRightEdge_LetTwoTallBlockExit()
        {
            var level = new LevelData(3, 4, 30);
            level.AddDoor(new DoorData(Direction.Right, 1, 1, Red));
            level.AddDoor(new DoorData(Direction.Right, 2, 1, Red));
            level.AddBlock(new BlockData("I2-V", new Vector2Int(2, 1), Red));
            BoardModel board = level.BuildBoard();

            Assert.That(new MoveResolver(board).TryStep(board.Blocks[0], Direction.Right), Is.EqualTo(StepResult.Exited));
        }

        [Test]
        public void BuildBoard_ContiguousDoorsListedOutOfOrder_MergeIntoOneDoor()
        {
            var level = new LevelData(5, 3, 30);
            level.AddDoor(new DoorData(Direction.Up, 3, 1, Red));
            level.AddDoor(new DoorData(Direction.Up, 1, 1, Red));
            level.AddDoor(new DoorData(Direction.Up, 2, 1, Red));
            Door door = level.BuildBoard().Doors.Single();

            Assert.That((door.Offset, door.Length), Is.EqualTo((1, 3)));
        }

        [Test]
        public void PublicSurface_GameCode_CannotWriteLevelData()
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            bool hasPublicSetter = typeof(LevelData).GetProperties(flags).Any(p => p.GetSetMethod() != null);
            bool exposesMutableList = typeof(LevelData).GetProperties(flags)
                .Any(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(System.Collections.Generic.List<>));

            Assert.That((hasPublicSetter, exposesMutableList), Is.EqualTo((false, false)));
        }

        private static LevelData SampleLevel()
        {
            var level = new LevelData(5, 6, 45);
            level.AddDoor(new DoorData(Direction.Up, 1, 2, Red));
            level.AddDoor(new DoorData(Direction.Left, 0, 1, Blue));
            level.AddBlock(new BlockData("I2-H", Vector2Int.zero, Red));
            level.AddBlock(new BlockData("L3-0", new Vector2Int(3, 4), Blue));
            return level;
        }

        private static LevelData RoundTrip(LevelData level)
        {
            return JsonUtility.FromJson<LevelData>(JsonUtility.ToJson(level));
        }
    }
}
