using System.Collections.Generic;
using System.Linq;
using CBJ.Core;
using NUnit.Framework;
using UnityEngine;

namespace CBJ.Tests.EditMode.Core
{
    public class LevelValidatorTests
    {
        private const int Red = 0;
        private const int Blue = 1;

        [Test]
        public void Validate_SolvableLevel_FindsNothing()
        {
            Assert.That(LevelValidator.Validate(ValidLevel()), Is.Empty);
        }

        [Test]
        public void Validate_NoBlocks_IsError()
        {
            var level = new LevelData(4, 4, 30);

            Assert.That(Severities(level), Is.EqualTo(new[] { IssueSeverity.Error }));
        }

        [Test]
        public void Validate_NoDoorOfBlockColour_IsWarning()
        {
            var level = new LevelData(4, 4, 30);
            level.AddDoor(new DoorData(Direction.Up, 0, 1, Blue));
            level.AddBlock(new BlockData(TestShapes.Single.Cells, Vector2Int.zero, Red));

            Assert.That(Severities(level), Is.EqualTo(new[] { IssueSeverity.Warning }));
        }

        [Test]
        public void Validate_DoorNarrowerThanBlock_IsWarning()
        {
            var level = new LevelData(4, 4, 30);
            level.AddDoor(new DoorData(Direction.Up, 0, 1, Red));
            level.AddBlock(new BlockData(TestShapes.Domino.Cells, Vector2Int.zero, Red));

            Assert.That(Severities(level), Is.EqualTo(new[] { IssueSeverity.Warning }));
        }

        [Test]
        public void Validate_AdjacentOneCellDoorsAsWideAsBlock_FindsNothing()
        {
            var level = new LevelData(4, 4, 30);
            level.AddDoor(new DoorData(Direction.Up, 1, 1, Red));
            level.AddDoor(new DoorData(Direction.Up, 2, 1, Red));
            level.AddBlock(new BlockData(TestShapes.Domino.Cells, Vector2Int.zero, Red));

            Assert.That(LevelValidator.Validate(level), Is.Empty);
        }

        [Test]
        public void Validate_SideDoorAsTallAsBlock_FindsNothing()
        {
            var level = new LevelData(4, 4, 30);
            level.AddDoor(new DoorData(Direction.Left, 0, 1, Red));
            level.AddBlock(new BlockData(TestShapes.Domino.Cells, Vector2Int.zero, Red));

            Assert.That(LevelValidator.Validate(level), Is.Empty);
        }

        [Test]
        public void Validate_DisconnectedBlockCells_IsError()
        {
            LevelData level = ValidLevel();
            level.AddBlock(new BlockData(new[] { new Vector2Int(0, 2), new Vector2Int(2, 3) }, Vector2Int.zero, Red));

            Assert.That(Severities(level), Is.EqualTo(new[] { IssueSeverity.Error }));
        }

        [Test]
        public void Validate_BlockWithoutCells_IsError()
        {
            LevelData level = ValidLevel();
            level.AddBlock(default);

            Assert.That(Severities(level), Is.EqualTo(new[] { IssueSeverity.Error }));
        }

        [Test]
        public void Validate_BlockOutsideBoard_IsError()
        {
            LevelData level = ValidLevel();
            level.AddBlock(new BlockData(TestShapes.Domino.Cells, new Vector2Int(3, 3), Red));

            Assert.That(Severities(level), Is.EqualTo(new[] { IssueSeverity.Error }));
        }

        [Test]
        public void Validate_OverlappingBlocks_IsError()
        {
            LevelData level = ValidLevel();
            level.AddBlock(new BlockData(TestShapes.Single.Cells, Vector2Int.zero, Red));

            Assert.That(Severities(level), Is.EqualTo(new[] { IssueSeverity.Error }));
        }

        [Test]
        public void Validate_DoorPastItsEdge_IsError()
        {
            LevelData level = ValidLevel();
            level.AddDoor(new DoorData(Direction.Right, 3, 2, Blue));

            Assert.That(Severities(level), Is.EqualTo(new[] { IssueSeverity.Error }));
        }

        [Test]
        public void Validate_DoorWithNegativeOffset_IsError()
        {
            LevelData level = ValidLevel();
            level.AddDoor(new DoorData(Direction.Down, -1, 1, Blue));

            Assert.That(Severities(level), Is.EqualTo(new[] { IssueSeverity.Error }));
        }

        [Test]
        public void Validate_DoorOfZeroLength_IsError()
        {
            LevelData level = ValidLevel();
            level.AddDoor(new DoorData(Direction.Down, 0, 0, Blue));

            Assert.That(Severities(level), Is.EqualTo(new[] { IssueSeverity.Error }));
        }

        [Test]
        public void Validate_DoorOnUnknownEdge_IsError()
        {
            LevelData level = ValidLevel();
            level.AddDoor(new DoorData((Direction)9, 0, 1, Blue));

            Assert.That(Severities(level), Is.EqualTo(new[] { IssueSeverity.Error }));
        }

        [Test]
        public void Validate_OverlappingDoorsOnOneEdge_IsError()
        {
            LevelData level = ValidLevel();
            level.AddDoor(new DoorData(Direction.Up, 1, 1, Blue));

            Assert.That(Severities(level), Is.EqualTo(new[] { IssueSeverity.Error }));
        }

        [Test]
        public void Validate_CellsAwayFromOrigin_IsError()
        {
            LevelData level = JsonUtility.FromJson<LevelData>(
                "{\"_width\":4,\"_height\":4,\"_timerSeconds\":30,\"_doors\":[{\"_edge\":0,\"_offset\":0,\"_length\":1,\"_colorId\":0}]," +
                "\"_blocks\":[{\"_cells\":[{\"x\":1,\"y\":1}],\"_origin\":{\"x\":0,\"y\":0},\"_colorId\":0}]}");

            Assert.That(Severities(level), Is.EqualTo(new[] { IssueSeverity.Error }));
        }

        [Test]
        public void Validate_ZeroTimer_IsError()
        {
            LevelData level = ValidLevel();
            level.TimerSeconds = 0;

            Assert.That(Severities(level), Is.EqualTo(new[] { IssueSeverity.Error }));
        }

        [Test]
        public void HasErrors_WarningsOnly_IsFalse()
        {
            var issues = new[] { new LevelIssue(IssueSeverity.Warning, "warning") };

            Assert.That(LevelValidator.HasErrors(issues), Is.False);
        }

        [Test]
        public void Validate_LevelWithoutErrors_BuildsABoard()
        {
            LevelData level = ValidLevel();
            level.AddBlock(new BlockData(TestShapes.L.Cells, new Vector2Int(2, 2), Blue));
            Assume.That(LevelValidator.HasErrors(LevelValidator.Validate(level)), Is.False);

            Assert.DoesNotThrow(() => level.BuildBoard());
        }

        // 4x4, a red domino at the bottom left, a red two-cell door on the top edge.
        private static LevelData ValidLevel()
        {
            var level = new LevelData(4, 4, 30);
            level.AddDoor(new DoorData(Direction.Up, 0, 2, Red));
            level.AddBlock(new BlockData(TestShapes.Domino.Cells, Vector2Int.zero, Red));
            return level;
        }

        private static IEnumerable<IssueSeverity> Severities(LevelData level)
        {
            return LevelValidator.Validate(level).Select(i => i.Severity);
        }
    }
}
