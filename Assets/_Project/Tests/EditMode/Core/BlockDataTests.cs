using System;
using CBJ.Core;
using NUnit.Framework;
using UnityEngine;

namespace CBJ.Tests.EditMode.Core
{
    public class BlockDataTests
    {
        private const int Red = 0;

        [Test]
        public void Constructor_OffsetCells_NormalisesToCorner()
        {
            var block = new BlockData(new[] { new Vector2Int(3, 2), new Vector2Int(4, 2) }, Vector2Int.zero, Red);

            Assert.That(block.Cells, Is.EqualTo(new[] { Vector2Int.zero, Vector2Int.right }));
        }

        [Test]
        public void Constructor_OffsetCells_MovesOriginSoBoardCellsStay()
        {
            var block = new BlockData(new[] { new Vector2Int(3, 2), new Vector2Int(4, 2) }, Vector2Int.one, Red);

            Assert.That(block.Origin, Is.EqualTo(new Vector2Int(4, 3)));
        }

        [Test]
        public void Constructor_CellsInAnyOrder_StoresTheSameBlock()
        {
            var first = new BlockData(new[] { Vector2Int.up, Vector2Int.zero, Vector2Int.right }, Vector2Int.zero, Red);
            var second = new BlockData(new[] { Vector2Int.right, Vector2Int.up, Vector2Int.zero }, Vector2Int.zero, Red);

            Assert.That(first, Is.EqualTo(second));
        }

        [Test]
        public void Constructor_NoCells_Throws()
        {
            Assert.Throws<ArgumentException>(() => new BlockData(Array.Empty<Vector2Int>(), Vector2Int.zero, Red));
        }

        [Test]
        public void JsonRoundTrip_ConcaveBlock_KeepsCellsOriginAndColour()
        {
            var block = new BlockData(TestShapes.L.Cells, new Vector2Int(2, 1), 5);

            var copy = JsonUtility.FromJson<BlockData>(JsonUtility.ToJson(block));

            Assert.That(copy, Is.EqualTo(block));
        }

        [Test]
        public void ToJson_SavedFormat_HasNoShapeId()
        {
            string json = JsonUtility.ToJson(new BlockData(TestShapes.Single.Cells, Vector2Int.zero, Red));

            Assert.That(json, Does.Not.Contain("shapeId"));
        }

        [Test]
        public void Size_LShape_IsBoundingBox()
        {
            var block = new BlockData(TestShapes.L.Cells, Vector2Int.zero, Red);

            Assert.That(block.Size, Is.EqualTo(new Vector2Int(2, 2)));
        }

        [Test]
        public void Covers_NotchOfLShape_IsFalse()
        {
            var block = new BlockData(TestShapes.L.Cells, new Vector2Int(1, 1), Red);

            Assert.That((block.Covers(new Vector2Int(1, 2)), block.Covers(new Vector2Int(2, 2))), Is.EqualTo((true, false)));
        }

        [Test]
        public void ToBlock_FreeformCells_KeepsTheShape()
        {
            var block = new BlockData(TestShapes.L.Cells, Vector2Int.one, Red);

            Assert.That(block.ToBlock().Shape.Cells, Is.EquivalentTo(TestShapes.L.Cells));
        }
    }
}
