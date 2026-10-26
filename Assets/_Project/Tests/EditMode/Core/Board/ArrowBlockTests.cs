using System;
using CBJ.Core;
using NUnit.Framework;
using UnityEngine;

namespace CBJ.Tests.EditMode.Core
{
    public class ArrowBlockTests
    {
        private const int Red = 0;

        [Test]
        public void TryStep_HorizontalArrowSteppingUp_IsBlocked()
        {
            (MoveResolver resolver, Block arrow) = Board(Axis.Horizontal, Array.Empty<Door>());

            Assert.That(resolver.TryStep(arrow, Direction.Up), Is.EqualTo(StepResult.Blocked));
        }

        [Test]
        public void TryStep_HorizontalArrowSteppingRight_Moves()
        {
            (MoveResolver resolver, Block arrow) = Board(Axis.Horizontal, Array.Empty<Door>());

            Assert.That(resolver.TryStep(arrow, Direction.Right), Is.EqualTo(StepResult.Moved));
        }

        [Test]
        public void Probe_VerticalArrowSteppingLeft_IsBlocked()
        {
            (MoveResolver resolver, Block arrow) = Board(Axis.Vertical, Array.Empty<Door>());

            Assert.That(resolver.Probe(arrow, Direction.Left), Is.EqualTo(StepResult.Blocked));
        }

        [Test]
        public void Probe_VerticalArrowSteppingDown_Moves()
        {
            (MoveResolver resolver, Block arrow) = Board(Axis.Vertical, Array.Empty<Door>());

            Assert.That(resolver.Probe(arrow, Direction.Down), Is.EqualTo(StepResult.Moved));
        }

        [Test]
        public void TryStep_HorizontalArrowAtMatchingLeftDoor_Exits()
        {
            var door = new Door(Direction.Left, 0, 3, Red);
            (MoveResolver resolver, Block arrow) = Board(Axis.Horizontal, new[] { door }, Vector2Int.zero);

            Assert.That(resolver.TryStep(arrow, Direction.Left), Is.EqualTo(StepResult.Exited));
        }

        [Test]
        public void TryStep_HorizontalArrowAtMatchingBottomDoor_IsBlocked()
        {
            var door = new Door(Direction.Down, 0, 3, Red);
            (MoveResolver resolver, Block arrow) = Board(Axis.Horizontal, new[] { door }, Vector2Int.zero);

            Assert.That(resolver.TryStep(arrow, Direction.Down), Is.EqualTo(StepResult.Blocked));
        }

        // One-cell arrow block in the middle of a 3×3 board (or at the given cell).
        private static (MoveResolver, Block) Board(Axis axis, Door[] doors, Vector2Int? origin = null)
        {
            var arrow = new Block(TestShapes.Single, Red, origin ?? Vector2Int.one, BlockTraits.Arrow(axis));
            var board = new BoardModel(3, 3, doors, new[] { arrow });
            return (new MoveResolver(board), arrow);
        }
    }
}
