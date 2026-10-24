using System;
using CBJ.Core;
using NUnit.Framework;
using UnityEngine;

namespace CBJ.Tests.EditMode.Core
{
    public class BoardModelTests
    {
        private const int Red = 0;

        [Test]
        public void Constructor_PlacesBlocks_CellsReportTheirBlock()
        {
            var block = new Block(TestShapes.Domino, Red, new Vector2Int(1, 2));
            var board = new BoardModel(4, 4, Array.Empty<Door>(), new[] { block });

            Assert.That((board.GetBlockAt(new Vector2Int(1, 2)), board.GetBlockAt(new Vector2Int(2, 2))), Is.EqualTo((block, block)));
        }

        [Test]
        public void GetBlockAt_EmptyCell_ReturnsNull()
        {
            var board = new BoardModel(3, 3, Array.Empty<Door>(), Array.Empty<Block>());

            Assert.That(board.GetBlockAt(Vector2Int.one), Is.Null);
        }

        [Test]
        public void GetBlockAt_OffBoard_ReturnsNull()
        {
            var board = new BoardModel(3, 3, Array.Empty<Door>(), Array.Empty<Block>());

            Assert.That(board.GetBlockAt(new Vector2Int(-1, 0)), Is.Null);
        }

        [Test]
        public void Constructor_NoBlocks_IsCleared()
        {
            var board = new BoardModel(3, 3, Array.Empty<Door>(), Array.Empty<Block>());

            Assert.That(board.IsCleared, Is.True);
        }

        [Test]
        public void Constructor_ZeroWidth_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BoardModel(0, 5, Array.Empty<Door>(), Array.Empty<Block>()));
        }

        [Test]
        public void Constructor_ZeroHeight_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BoardModel(5, 0, Array.Empty<Door>(), Array.Empty<Block>()));
        }

        [Test]
        public void Constructor_OverlappingBlocks_Throws()
        {
            var first = new Block(TestShapes.Domino, Red, Vector2Int.zero);
            var second = new Block(TestShapes.Single, Red, Vector2Int.right);

            Assert.Throws<ArgumentException>(() => new BoardModel(3, 3, Array.Empty<Door>(), new[] { first, second }));
        }

        [Test]
        public void Constructor_BlockOutsideBoard_Throws()
        {
            var block = new Block(TestShapes.Domino, Red, new Vector2Int(2, 0));

            Assert.Throws<ArgumentException>(() => new BoardModel(3, 3, Array.Empty<Door>(), new[] { block }));
        }

        [Test]
        public void Constructor_BlockFromAnotherBoard_Throws()
        {
            var block = new Block(TestShapes.Single, Red, Vector2Int.zero);
            new BoardModel(3, 3, Array.Empty<Door>(), new[] { block });

            Assert.Throws<ArgumentException>(() => new BoardModel(3, 3, Array.Empty<Door>(), new[] { block }));
        }

        [Test]
        public void Constructor_SameBlockTwice_Throws()
        {
            var block = new Block(TestShapes.Single, Red, Vector2Int.zero);

            Assert.Throws<ArgumentException>(() => new BoardModel(3, 3, Array.Empty<Door>(), new[] { block, block }));
        }

        [Test]
        public void Constructor_LaterBlockInvalid_EarlierBlocksReusable()
        {
            var valid = new Block(TestShapes.Single, Red, Vector2Int.zero);
            var outside = new Block(TestShapes.Single, Red, new Vector2Int(5, 5));
            Assert.Throws<ArgumentException>(() => new BoardModel(3, 3, Array.Empty<Door>(), new[] { valid, outside }));

            Assert.DoesNotThrow(() => new BoardModel(3, 3, Array.Empty<Door>(), new[] { valid }));
        }

        [Test]
        public void Constructor_DoorPastEdge_Throws()
        {
            var door = new Door(Direction.Up, 2, 2, Red);

            Assert.Throws<ArgumentException>(() => new BoardModel(3, 5, new[] { door }, Array.Empty<Block>()));
        }

        [Test]
        public void Constructor_DoorEndingAtCorner_IsAccepted()
        {
            var door = new Door(Direction.Right, 3, 2, Red);

            Assert.DoesNotThrow(() => new BoardModel(3, 5, new[] { door }, Array.Empty<Block>()));
        }

        [Test]
        public void DoorConstructor_ZeroLength_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Door(Direction.Up, 0, 0, Red));
        }

        [Test]
        public void DoorConstructor_NegativeOffset_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Door(Direction.Up, -1, 1, Red));
        }

        [Test]
        public void CanOccupy_OwnCells_ReturnsTrue()
        {
            var block = new Block(TestShapes.Domino, Red, Vector2Int.zero);
            var board = new BoardModel(3, 1, Array.Empty<Door>(), new[] { block });

            Assert.That(board.CanOccupy(block, Vector2Int.right), Is.True);
        }

        [Test]
        public void CanOccupy_OtherBlocksCell_ReturnsFalse()
        {
            var block = new Block(TestShapes.Single, Red, Vector2Int.zero);
            var other = new Block(TestShapes.Single, Red, Vector2Int.right);
            var board = new BoardModel(3, 1, Array.Empty<Door>(), new[] { block, other });

            Assert.That(board.CanOccupy(block, Vector2Int.right), Is.False);
        }
    }
}
