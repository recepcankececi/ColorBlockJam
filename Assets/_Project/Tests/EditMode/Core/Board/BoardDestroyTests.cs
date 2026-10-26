using System;
using CBJ.Core;
using NUnit.Framework;
using UnityEngine;

namespace CBJ.Tests.EditMode.Core
{
    public class BoardDestroyTests
    {
        private const int Red = 0;

        [Test]
        public void Destroy_Block_FreesItsCells()
        {
            (BoardModel board, Block first, _) = TwoBlocks();

            board.Destroy(first);

            Assert.That(board.GetBlockAt(Vector2Int.zero), Is.Null);
        }

        [Test]
        public void Destroy_Block_CountsAsLeftTheBoard()
        {
            (BoardModel board, Block first, _) = TwoBlocks();

            board.Destroy(first);

            Assert.That((board.RemainingBlocks, first.IsExited), Is.EqualTo((1, true)));
        }

        [Test]
        public void Destroy_Block_RaisesBlockDestroyed()
        {
            (BoardModel board, Block first, _) = TwoBlocks();
            Block destroyed = null;
            board.BlockDestroyed += block => destroyed = block;

            board.Destroy(first);

            Assert.That(destroyed, Is.SameAs(first));
        }

        [Test]
        public void Destroy_Block_RaisesNoExit()
        {
            (BoardModel board, Block first, _) = TwoBlocks();
            bool exited = false;
            board.BlockExited += (block, door) => exited = true;

            board.Destroy(first);

            Assert.That(exited, Is.False);
        }

        [Test]
        public void Destroy_LastBlock_RaisesClearedOnce()
        {
            (BoardModel board, Block first, Block second) = TwoBlocks();
            int cleared = 0;
            board.Cleared += () => cleared++;

            board.Destroy(first);
            board.Destroy(second);

            Assert.That((cleared, board.IsCleared), Is.EqualTo((1, true)));
        }

        [Test]
        public void Destroy_BlockAlreadyGone_Throws()
        {
            (BoardModel board, Block first, _) = TwoBlocks();
            board.Destroy(first);

            Assert.Throws<ArgumentException>(() => board.Destroy(first));
        }

        [Test]
        public void Destroy_BlockOfAnotherBoard_Throws()
        {
            (BoardModel board, _, _) = TwoBlocks();
            (_, Block stranger, _) = TwoBlocks();

            Assert.Throws<ArgumentException>(() => board.Destroy(stranger));
        }

        private static (BoardModel, Block, Block) TwoBlocks()
        {
            var first = new Block(TestShapes.Single, Red, Vector2Int.zero);
            var second = new Block(TestShapes.Single, Red, new Vector2Int(2, 2));
            return (new BoardModel(3, 3, Array.Empty<Door>(), new[] { first, second }), first, second);
        }
    }
}
