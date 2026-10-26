using System;
using CBJ.Core;
using NUnit.Framework;
using UnityEngine;

namespace CBJ.Tests.EditMode.Core
{
    public class MoveResolverTests
    {
        private const int Red = 0;

        [Test]
        public void TryStep_EmptyCellAhead_MovesOneCell()
        {
            var block = new Block(TestShapes.Single, Red, Vector2Int.one);
            var resolver = new MoveResolver(new BoardModel(3, 3, Array.Empty<Door>(), new[] { block }));

            StepResult result = resolver.TryStep(block, Direction.Up);

            Assert.That((result, block.Origin), Is.EqualTo((StepResult.Moved, new Vector2Int(1, 2))));
        }

        [Test]
        public void TryStep_Moved_FreesOldCellAndFillsNewCell()
        {
            var block = new Block(TestShapes.Domino, Red, Vector2Int.zero);
            var board = new BoardModel(3, 1, Array.Empty<Door>(), new[] { block });

            new MoveResolver(board).TryStep(block, Direction.Right);

            Assert.That((board.GetBlockAt(Vector2Int.zero), board.GetBlockAt(new Vector2Int(2, 0))), Is.EqualTo(((Block)null, block)));
        }

        [Test]
        public void TryStep_Moved_RaisesBlockMovedWithBothOrigins()
        {
            var block = new Block(TestShapes.Single, Red, Vector2Int.zero);
            var board = new BoardModel(3, 1, Array.Empty<Door>(), new[] { block });
            (Block, Vector2Int, Vector2Int) raised = default;
            board.BlockMoved += (b, from, to) => raised = (b, from, to);

            new MoveResolver(board).TryStep(block, Direction.Right);

            Assert.That(raised, Is.EqualTo((block, Vector2Int.zero, Vector2Int.right)));
        }

        [Test]
        public void TryStep_OtherBlockAhead_IsBlockedAndStays()
        {
            var block = new Block(TestShapes.Single, Red, Vector2Int.zero);
            var other = new Block(TestShapes.Single, Red, Vector2Int.right);
            var resolver = new MoveResolver(new BoardModel(3, 1, Array.Empty<Door>(), new[] { block, other }));

            StepResult result = resolver.TryStep(block, Direction.Right);

            Assert.That((result, block.Origin), Is.EqualTo((StepResult.Blocked, Vector2Int.zero)));
        }

        [Test]
        public void TryStep_Blocked_RaisesNoEvent()
        {
            var block = new Block(TestShapes.Single, Red, Vector2Int.zero);
            var board = new BoardModel(1, 1, Array.Empty<Door>(), new[] { block });
            bool raised = false;
            board.BlockMoved += (b, from, to) => raised = true;

            new MoveResolver(board).TryStep(block, Direction.Left);

            Assert.That(raised, Is.False);
        }

        [Test]
        public void TryStep_WallWithoutDoor_IsBlocked()
        {
            var block = new Block(TestShapes.Single, Red, new Vector2Int(2, 0));
            var resolver = new MoveResolver(new BoardModel(3, 1, Array.Empty<Door>(), new[] { block }));

            Assert.That(resolver.TryStep(block, Direction.Right), Is.EqualTo(StepResult.Blocked));
        }

        [Test]
        public void TryStep_IntoOwnCells_Moves()
        {
            var block = new Block(TestShapes.Bar3, Red, Vector2Int.zero);
            var resolver = new MoveResolver(new BoardModel(4, 1, Array.Empty<Door>(), new[] { block }));

            Assert.That(resolver.TryStep(block, Direction.Right), Is.EqualTo(StepResult.Moved));
        }

        [Test]
        public void TryStep_LShapeNotchAroundOtherBlock_MovesPastIt()
        {
            var block = new Block(TestShapes.L, Red, Vector2Int.zero);
            var other = new Block(TestShapes.Single, Red, new Vector2Int(2, 1));
            var resolver = new MoveResolver(new BoardModel(3, 2, Array.Empty<Door>(), new[] { block, other }));

            Assert.That(resolver.TryStep(block, Direction.Right), Is.EqualTo(StepResult.Moved));
        }

        [Test]
        public void TryStep_LShapeArmHitsOtherBlock_IsBlocked()
        {
            var block = new Block(TestShapes.L, Red, Vector2Int.zero);
            var other = new Block(TestShapes.Single, Red, new Vector2Int(2, 0));
            var resolver = new MoveResolver(new BoardModel(3, 2, Array.Empty<Door>(), new[] { block, other }));

            Assert.That(resolver.TryStep(block, Direction.Right), Is.EqualTo(StepResult.Blocked));
        }

        [Test]
        public void Probe_EmptyCellAhead_ReturnsMovedWithoutMoving()
        {
            var block = new Block(TestShapes.Single, Red, Vector2Int.zero);
            var resolver = new MoveResolver(new BoardModel(2, 1, Array.Empty<Door>(), new[] { block }));

            StepResult result = resolver.Probe(block, Direction.Right);

            Assert.That((result, block.Origin), Is.EqualTo((StepResult.Moved, Vector2Int.zero)));
        }

        [Test]
        public void Probe_MatchingDoor_ReturnsExitedWithoutRemoving()
        {
            var block = new Block(TestShapes.Single, Red, Vector2Int.zero);
            var door = new Door(Direction.Left, 0, 1, Red);
            var board = new BoardModel(2, 1, new[] { door }, new[] { block });

            StepResult result = new MoveResolver(board).Probe(block, Direction.Left);

            Assert.That((result, board.GetBlockAt(Vector2Int.zero)), Is.EqualTo((StepResult.Exited, block)));
        }

        [Test]
        public void TryStep_ExitedBlock_Throws()
        {
            var block = new Block(TestShapes.Single, Red, Vector2Int.zero);
            var door = new Door(Direction.Left, 0, 1, Red);
            var resolver = new MoveResolver(new BoardModel(2, 1, new[] { door }, new[] { block }));
            resolver.TryStep(block, Direction.Left);

            Assert.Throws<ArgumentException>(() => resolver.TryStep(block, Direction.Right));
        }

        [Test]
        public void TryStep_BlockFromAnotherBoard_Throws()
        {
            var foreign = new Block(TestShapes.Single, Red, Vector2Int.zero);
            new BoardModel(2, 1, Array.Empty<Door>(), new[] { foreign });
            var resolver = new MoveResolver(new BoardModel(2, 1, Array.Empty<Door>(), Array.Empty<Block>()));

            Assert.Throws<ArgumentException>(() => resolver.TryStep(foreign, Direction.Right));
        }
    }
}
