using CBJ.Core;
using CBJ.Editor;
using NUnit.Framework;
using UnityEngine;

namespace CBJ.Tests.EditMode.LevelEditor
{
    public class BlockKindEditTests
    {
        private const int Red = 0;

        private static readonly Vector2Int[] Domino = { Vector2Int.zero, Vector2Int.right };

        [Test]
        public void TryPlaceBlock_WithIceTraits_StoresThem()
        {
            var level = new LevelData(5, 5, 60);

            LevelEditOperations.TryPlaceBlock(level, Domino, Vector2Int.zero, Red, BlockTraits.Ice(2));

            Assert.That(level.Blocks[0].Traits, Is.EqualTo(BlockTraits.Ice(2)));
        }

        [Test]
        public void ApplyStroke_NewBlockWithArrowTraits_StoresThem()
        {
            var level = new LevelData(5, 5, 60);

            LevelEditOperations.ApplyStroke(level, Domino, -1, Red, BlockTraits.Arrow(Axis.Vertical), out _);

            Assert.That(level.Blocks[0].Traits, Is.EqualTo(BlockTraits.Arrow(Axis.Vertical)));
        }

        [Test]
        public void ApplyStroke_GrowingABlock_KeepsItsKind()
        {
            var level = new LevelData(5, 5, 60);
            LevelEditOperations.TryPlaceBlock(level, Domino, Vector2Int.zero, Red, BlockTraits.Ice(3));

            LevelEditOperations.ApplyStroke(level, new[] { new Vector2Int(1, 0), new Vector2Int(2, 0) }, 0, Red, BlockTraits.Normal, out _);

            Assert.That((level.Blocks[0].Cells.Count, level.Blocks[0].Traits), Is.EqualTo((3, BlockTraits.Ice(3))));
        }

        [Test]
        public void Traits_ArrowVerticalBrush_IsVerticalArrow()
        {
            var brush = new LevelEditorBrush { Kind = BrushKind.ArrowVertical };

            Assert.That(brush.Traits, Is.EqualTo(BlockTraits.Arrow(Axis.Vertical)));
        }

        [Test]
        public void Traits_IceTwoBrush_IsTwoLayerIce()
        {
            var brush = new LevelEditorBrush { Kind = BrushKind.Ice2 };

            Assert.That(brush.Traits, Is.EqualTo(BlockTraits.Ice(2)));
        }

        [Test]
        public void Traits_NewBrush_IsNormal()
        {
            Assert.That(new LevelEditorBrush().Traits, Is.EqualTo(BlockTraits.Normal));
        }
    }
}
