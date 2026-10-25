using CBJ.Editor;
using NUnit.Framework;
using UnityEngine;

namespace CBJ.Tests.EditMode.LevelEditor
{
    public class ShapeRotationTests
    {
        [Test]
        public void RotateClockwise_HorizontalBar_BecomesVertical()
        {
            Vector2Int[] rotated = ShapeRotation.RotateClockwise(ShapeLibrary.Get("I3-H").Cells, 1);

            Assert.That(rotated, Is.EquivalentTo(ShapeLibrary.Get("I3-V").Cells));
        }

        [Test]
        public void RotateClockwise_OneTurn_MatchesNextLibraryOrientation()
        {
            Vector2Int[] rotated = ShapeRotation.RotateClockwise(ShapeLibrary.Get("L3-0").Cells, 1);

            Assert.That(rotated, Is.EquivalentTo(ShapeLibrary.Get("L3-90").Cells));
        }

        [Test]
        public void RotateClockwise_FourTurns_ReturnsTheOriginal()
        {
            Vector2Int[] rotated = ShapeRotation.RotateClockwise(ShapeLibrary.Get("T4-0").Cells, 4);

            Assert.That(rotated, Is.EquivalentTo(ShapeLibrary.Get("T4-0").Cells));
        }

        [Test]
        public void RotateClockwise_NegativeTurn_GoesAnticlockwise()
        {
            Vector2Int[] rotated = ShapeRotation.RotateClockwise(ShapeLibrary.Get("L3-90").Cells, -1);

            Assert.That(rotated, Is.EquivalentTo(ShapeLibrary.Get("L3-0").Cells));
        }

        [Test]
        public void Anchor_ShapeWithEmptyTopLeft_IsLeftmostCellOfTopRow()
        {
            Assert.That(ShapeRotation.Anchor(ShapeLibrary.Get("T4-180").Cells), Is.EqualTo(new Vector2Int(1, 1)));
        }
    }
}
