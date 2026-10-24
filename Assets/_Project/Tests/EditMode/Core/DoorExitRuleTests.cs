using System;
using CBJ.Core;
using NUnit.Framework;
using UnityEngine;

namespace CBJ.Tests.EditMode.Core
{
    public class DoorExitRuleTests
    {
        private const int Red = 0;
        private const int Blue = 1;

        [Test]
        public void TryStep_MatchingDoorThatFits_Exits()
        {
            var block = new Block(TestShapes.Domino, Red, new Vector2Int(1, 2));
            var door = new Door(Direction.Up, 1, 2, Red);
            var resolver = new MoveResolver(new BoardModel(4, 3, new[] { door }, new[] { block }));

            Assert.That(resolver.TryStep(block, Direction.Up), Is.EqualTo(StepResult.Exited));
        }

        [Test]
        public void TryStep_Exited_FreesCellsAndMarksBlock()
        {
            var block = new Block(TestShapes.Single, Red, new Vector2Int(2, 0));
            var door = new Door(Direction.Right, 0, 1, Red);
            var board = new BoardModel(3, 1, new[] { door }, new[] { block });

            new MoveResolver(board).TryStep(block, Direction.Right);

            Assert.That((block.IsExited, board.GetBlockAt(new Vector2Int(2, 0))), Is.EqualTo((true, (Block)null)));
        }

        [Test]
        public void TryStep_Exited_RaisesBlockExitedWithDoor()
        {
            var block = new Block(TestShapes.Single, Red, Vector2Int.zero);
            var door = new Door(Direction.Down, 0, 1, Red);
            var board = new BoardModel(1, 2, new[] { door }, new[] { block });
            (Block, Door) raised = default;
            board.BlockExited += (b, d) => raised = (b, d);

            new MoveResolver(board).TryStep(block, Direction.Down);

            Assert.That(raised, Is.EqualTo((block, door)));
        }

        [Test]
        public void TryStep_DoorOfOtherColour_IsBlocked()
        {
            var block = new Block(TestShapes.Single, Red, Vector2Int.zero);
            var door = new Door(Direction.Left, 0, 1, Blue);
            var resolver = new MoveResolver(new BoardModel(2, 1, new[] { door }, new[] { block }));

            Assert.That(resolver.TryStep(block, Direction.Left), Is.EqualTo(StepResult.Blocked));
        }

        [Test]
        public void TryStep_BlockWiderThanDoor_IsBlocked()
        {
            var block = new Block(TestShapes.Bar3, Red, new Vector2Int(0, 1));
            var door = new Door(Direction.Up, 0, 2, Red);
            var resolver = new MoveResolver(new BoardModel(3, 2, new[] { door }, new[] { block }));

            Assert.That(resolver.TryStep(block, Direction.Up), Is.EqualTo(StepResult.Blocked));
        }

        [Test]
        public void TryStep_BlockHalfBesideDoor_IsBlocked()
        {
            var block = new Block(TestShapes.Domino, Red, new Vector2Int(1, 1));
            var door = new Door(Direction.Up, 0, 2, Red);
            var resolver = new MoveResolver(new BoardModel(3, 2, new[] { door }, new[] { block }));

            Assert.That(resolver.TryStep(block, Direction.Up), Is.EqualTo(StepResult.Blocked));
        }

        [Test]
        public void TryStep_MatchingDoorOnOppositeEdge_IsBlocked()
        {
            var block = new Block(TestShapes.Single, Red, new Vector2Int(0, 2));
            var door = new Door(Direction.Down, 0, 1, Red);
            var resolver = new MoveResolver(new BoardModel(1, 3, new[] { door }, new[] { block }));

            Assert.That(resolver.TryStep(block, Direction.Up), Is.EqualTo(StepResult.Blocked));
        }

        [Test]
        public void TryStep_TowardDoorButNotAtEdge_OnlyMoves()
        {
            var block = new Block(TestShapes.Single, Red, Vector2Int.zero);
            var door = new Door(Direction.Right, 0, 1, Red);
            var resolver = new MoveResolver(new BoardModel(3, 1, new[] { door }, new[] { block }));

            Assert.That(resolver.TryStep(block, Direction.Right), Is.EqualTo(StepResult.Moved));
        }

        [Test]
        public void TryStep_SecondDoorOnEdgeMatches_ExitsThroughIt()
        {
            var block = new Block(TestShapes.Single, Red, new Vector2Int(2, 0));
            var blueDoor = new Door(Direction.Down, 0, 1, Blue);
            var redDoor = new Door(Direction.Down, 2, 1, Red);
            var board = new BoardModel(3, 1, new[] { blueDoor, redDoor }, new[] { block });
            Door used = null;
            board.BlockExited += (b, d) => used = d;

            new MoveResolver(board).TryStep(block, Direction.Down);

            Assert.That(used, Is.SameAs(redDoor));
        }

        [Test]
        public void TryStep_LShapeTrailingCellPathBlocked_IsBlocked()
        {
            // L at (0,0): its (1,0) cell must slide through (1,1) to leave through the top edge.
            var block = new Block(TestShapes.L, Red, Vector2Int.zero);
            var other = new Block(TestShapes.Single, Blue, new Vector2Int(1, 1));
            var door = new Door(Direction.Up, 0, 2, Red);
            var resolver = new MoveResolver(new BoardModel(2, 2, new[] { door }, new[] { block, other }));

            Assert.That(resolver.TryStep(block, Direction.Up), Is.EqualTo(StepResult.Blocked));
        }

        [Test]
        public void TryStep_LShapeTrailingCellPathClear_Exits()
        {
            var block = new Block(TestShapes.L, Red, Vector2Int.zero);
            var door = new Door(Direction.Up, 0, 2, Red);
            var resolver = new MoveResolver(new BoardModel(2, 2, new[] { door }, new[] { block }));

            Assert.That(resolver.TryStep(block, Direction.Up), Is.EqualTo(StepResult.Exited));
        }

        [Test]
        public void TryStep_SideDoorWithOffsetOnTallBoard_Exits()
        {
            var block = new Block(TestShapes.Single, Red, new Vector2Int(1, 3));
            var door = new Door(Direction.Right, 3, 1, Red);
            var resolver = new MoveResolver(new BoardModel(2, 5, new[] { door }, new[] { block }));

            Assert.That(resolver.TryStep(block, Direction.Right), Is.EqualTo(StepResult.Exited));
        }

        [Test]
        public void TryStep_SideDoorWithOffset_BlockBelowDoorIsBlocked()
        {
            var block = new Block(TestShapes.Single, Red, new Vector2Int(1, 2));
            var door = new Door(Direction.Right, 3, 1, Red);
            var resolver = new MoveResolver(new BoardModel(2, 5, new[] { door }, new[] { block }));

            Assert.That(resolver.TryStep(block, Direction.Right), Is.EqualTo(StepResult.Blocked));
        }

        [Test]
        public void TryStep_DoorWiderThanBlockAtFarEnd_Exits()
        {
            var block = new Block(TestShapes.Domino, Red, new Vector2Int(2, 1));
            var door = new Door(Direction.Up, 0, 4, Red);
            var resolver = new MoveResolver(new BoardModel(4, 2, new[] { door }, new[] { block }));

            Assert.That(resolver.TryStep(block, Direction.Up), Is.EqualTo(StepResult.Exited));
        }

        [Test]
        public void TryStep_LShapeTrailingCellPathBlockedTowardRight_IsBlocked()
        {
            // L at (0,0): its (0,1) cell must slide through (1,1) to leave through the right edge.
            var block = new Block(TestShapes.L, Red, Vector2Int.zero);
            var other = new Block(TestShapes.Single, Blue, new Vector2Int(1, 1));
            var door = new Door(Direction.Right, 0, 2, Red);
            var resolver = new MoveResolver(new BoardModel(2, 2, new[] { door }, new[] { block, other }));

            Assert.That(resolver.TryStep(block, Direction.Right), Is.EqualTo(StepResult.Blocked));
        }

        [Test]
        public void TryStep_LastBlockExits_RaisesExitedBeforeCleared()
        {
            var block = new Block(TestShapes.Single, Red, Vector2Int.zero);
            var door = new Door(Direction.Down, 0, 1, Red);
            var board = new BoardModel(1, 1, new[] { door }, new[] { block });
            string order = string.Empty;
            board.BlockExited += (b, d) => order += "exited;";
            board.Cleared += () => order += "cleared;";

            new MoveResolver(board).TryStep(block, Direction.Down);

            Assert.That(order, Is.EqualTo("exited;cleared;"));
        }

        [Test]
        public void TryStep_LastBlockExits_RaisesClearedOnce()
        {
            var first = new Block(TestShapes.Single, Red, Vector2Int.zero);
            var second = new Block(TestShapes.Single, Red, Vector2Int.right);
            var door = new Door(Direction.Down, 0, 2, Red);
            var board = new BoardModel(2, 1, new[] { door }, new[] { first, second });
            var resolver = new MoveResolver(board);
            int cleared = 0;
            board.Cleared += () => cleared++;

            resolver.TryStep(first, Direction.Down);
            resolver.TryStep(second, Direction.Down);

            Assert.That(cleared, Is.EqualTo(1));
        }

        [Test]
        public void TryStep_BlocksRemain_IsNotCleared()
        {
            var first = new Block(TestShapes.Single, Red, Vector2Int.zero);
            var second = new Block(TestShapes.Single, Blue, Vector2Int.right);
            var door = new Door(Direction.Down, 0, 2, Red);
            var board = new BoardModel(2, 1, new[] { door }, new[] { first, second });

            new MoveResolver(board).TryStep(first, Direction.Down);

            Assert.That((board.IsCleared, board.RemainingBlocks), Is.EqualTo((false, 1)));
        }
    }
}
