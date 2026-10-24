using System;
using CBJ.Core;
using NUnit.Framework;
using UnityEngine;

namespace CBJ.Tests.EditMode.Core
{
    public class BlockShapeTests
    {
        [Test]
        public void Constructor_OffsetCells_NormalisesToBottomLeftOrigin()
        {
            var shape = new BlockShape(new[] { new Vector2Int(3, 5), new Vector2Int(4, 5), new Vector2Int(3, 6) });

            Assert.That(shape.Cells, Is.EqualTo(new[] { Vector2Int.zero, Vector2Int.right, Vector2Int.up }));
        }

        [Test]
        public void Constructor_LShape_ReportsBoundingBoxSize()
        {
            var shape = new BlockShape(new[] { Vector2Int.zero, new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(0, 1) });

            Assert.That((shape.Width, shape.Height), Is.EqualTo((3, 2)));
        }

        [Test]
        public void Constructor_NoCells_Throws()
        {
            Assert.Throws<ArgumentException>(() => new BlockShape(Array.Empty<Vector2Int>()));
        }

        [Test]
        public void Constructor_DuplicateCell_Throws()
        {
            Assert.Throws<ArgumentException>(() => new BlockShape(new[] { Vector2Int.one, Vector2Int.one }));
        }
    }
}
