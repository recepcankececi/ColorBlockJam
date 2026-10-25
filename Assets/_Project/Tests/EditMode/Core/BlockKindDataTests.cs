using System;
using CBJ.Core;
using NUnit.Framework;
using UnityEngine;

namespace CBJ.Tests.EditMode.Core
{
    public class BlockKindDataTests
    {
        private const int Red = 0;

        [Test]
        public void Constructor_WithoutTraits_IsNormal()
        {
            var block = new BlockData(TestShapes.Single.Cells, Vector2Int.zero, Red);

            Assert.That(block.Traits, Is.EqualTo(BlockTraits.Normal));
        }

        [Test]
        public void FromJson_SavedBeforeKindsExisted_LoadsAsNormal()
        {
            const string oldFormat = "{\"_cells\":[{\"x\":0,\"y\":0}],\"_origin\":{\"x\":1,\"y\":2},\"_colorId\":3}";

            var block = JsonUtility.FromJson<BlockData>(oldFormat);

            Assert.That((block.Traits, block.Origin, block.ColorId), Is.EqualTo((BlockTraits.Normal, new Vector2Int(1, 2), 3)));
        }

        [Test]
        public void JsonRoundTrip_ArrowBlock_KeepsKindAndAxis()
        {
            var block = new BlockData(TestShapes.Domino.Cells, Vector2Int.zero, Red, BlockTraits.Arrow(Axis.Vertical));

            var copy = JsonUtility.FromJson<BlockData>(JsonUtility.ToJson(block));

            Assert.That(copy.Traits, Is.EqualTo(BlockTraits.Arrow(Axis.Vertical)));
        }

        [Test]
        public void JsonRoundTrip_IceBlock_KeepsLayers()
        {
            var block = new BlockData(TestShapes.Domino.Cells, Vector2Int.zero, Red, BlockTraits.Ice(3));

            var copy = JsonUtility.FromJson<BlockData>(JsonUtility.ToJson(block));

            Assert.That(copy.Traits, Is.EqualTo(BlockTraits.Ice(3)));
        }

        [Test]
        public void Equals_SameCellsDifferentKind_IsFalse()
        {
            var normal = new BlockData(TestShapes.Single.Cells, Vector2Int.zero, Red);
            var ice = new BlockData(TestShapes.Single.Cells, Vector2Int.zero, Red, BlockTraits.Ice(1));

            Assert.That(normal.Equals(ice), Is.False);
        }

        [Test]
        public void ToBlock_IceData_StartsWithItsLayers()
        {
            var data = new BlockData(TestShapes.Single.Cells, Vector2Int.zero, Red, BlockTraits.Ice(2));

            Block block = data.ToBlock();

            Assert.That((block.Kind, block.IceLayers), Is.EqualTo((BlockKind.Ice, 2)));
        }

        [Test]
        public void ToBlock_ArrowData_KeepsItsAxis()
        {
            var data = new BlockData(TestShapes.Single.Cells, Vector2Int.zero, Red, BlockTraits.Arrow(Axis.Vertical));

            Block block = data.ToBlock();

            Assert.That((block.Kind, block.ArrowAxis), Is.EqualTo((BlockKind.Arrow, Axis.Vertical)));
        }

        [Test]
        public void Ice_ZeroLayers_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BlockTraits.Ice(0));
        }

        [Test]
        public void Ice_MoreThanMaxLayers_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BlockTraits.Ice(BlockTraits.MaxIceLayers + 1));
        }

        [Test]
        public void Arrow_HasNoIceLayers()
        {
            Assert.That(BlockTraits.Arrow(Axis.Horizontal).IceLayers, Is.Zero);
        }
    }
}
