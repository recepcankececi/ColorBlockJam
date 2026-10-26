using CBJ.Core;
using CBJ.Gameplay;
using CBJ.Tests.EditMode.Core;
using NUnit.Framework;
using UnityEngine;

namespace CBJ.Tests.EditMode.Gameplay
{
    public class BlockPickerTests
    {
        private const int Red = 0;
        private const float GrabTolerance = 0.35f;
        private const float Tolerance = 1e-4f;

        [Test]
        public void Find_PointOnABlockCell_ReturnsThatBlockAtDistanceZero()
        {
            BoardModel board = Board(new Vector2Int(1, 1));

            Block block = BlockPicker.Find(board, new Vector2(1.5f, 1.5f), GrabTolerance, out float distance);

            Assert.That(block, Is.SameAs(board.Blocks[0]));
            Assert.That(distance, Is.EqualTo(0f));
        }

        [Test]
        public void Find_PointJustOffAnEdgeWithinTolerance_ReturnsThatBlock()
        {
            BoardModel board = Board(new Vector2Int(1, 1));

            Block block = BlockPicker.Find(board, new Vector2(2.3f, 1.5f), GrabTolerance, out float distance);

            Assert.That(block, Is.SameAs(board.Blocks[0]));
            Assert.That(distance, Is.EqualTo(0.3f).Within(Tolerance));
        }

        [Test]
        public void Find_PointBeyondTolerance_ReturnsNullAndTheNearestDistance()
        {
            BoardModel board = Board(new Vector2Int(0, 0));

            Block block = BlockPicker.Find(board, new Vector2(1.5f, 0.5f), GrabTolerance, out float distance);

            Assert.That(block, Is.Null);
            Assert.That(distance, Is.EqualTo(0.5f).Within(Tolerance));
        }

        [Test]
        public void Find_PointNearACorner_MeasuresTheDiagonalToTheCellBox()
        {
            BoardModel board = Board(new Vector2Int(0, 0));

            Block block = BlockPicker.Find(board, new Vector2(1.3f, 1.3f), GrabTolerance, out float distance);

            Assert.That(block, Is.Null);
            Assert.That(distance, Is.EqualTo(Mathf.Sqrt(0.18f)).Within(Tolerance));
        }

        [Test]
        public void Find_TwoBlocksWithinTolerance_ReturnsTheNearest()
        {
            BoardModel board = Board(new Vector2Int(0, 0), new Vector2Int(1, 1));

            Block block = BlockPicker.Find(board, new Vector2(1.2f, 0.9f), GrabTolerance, out _);

            Assert.That(block, Is.SameAs(board.Blocks[1]));
        }

        [Test]
        public void Find_PointOutsideTheBoardNearAnEdgeBlock_ReturnsThatBlock()
        {
            BoardModel board = Board(new Vector2Int(0, 1));

            Block block = BlockPicker.Find(board, new Vector2(-0.2f, 1.5f), GrabTolerance, out _);

            Assert.That(block, Is.SameAs(board.Blocks[0]));
        }

        [Test]
        public void Find_NearAnEmptyCellOfAConcaveShape_MeasuresToTheBlocksCells()
        {
            var level = new LevelData(4, 3, 30);
            level.AddBlock(new BlockData(TestShapes.L.Cells, Vector2Int.zero, Red));
            BoardModel board = level.BuildBoard();

            Block block = BlockPicker.Find(board, new Vector2(1.5f, 1.5f), GrabTolerance, out float distance);

            Assert.That(block, Is.Null);
            Assert.That(distance, Is.EqualTo(0.5f).Within(Tolerance));
        }

        [Test]
        public void Find_DestroyedBlock_IsNotPicked()
        {
            BoardModel board = Board(new Vector2Int(0, 0), new Vector2Int(3, 0));
            board.Destroy(board.Blocks[0]);

            Block block = BlockPicker.Find(board, new Vector2(1.2f, 0.5f), GrabTolerance, out _);

            Assert.That(block, Is.Null);
        }

        // A 4×3 board without doors and a red 1×1 block at each origin.
        private static BoardModel Board(params Vector2Int[] origins)
        {
            var level = new LevelData(4, 3, 30);
            foreach (Vector2Int origin in origins)
            {
                level.AddBlock(new BlockData(TestShapes.Single.Cells, origin, Red));
            }

            return level.BuildBoard();
        }
    }
}
