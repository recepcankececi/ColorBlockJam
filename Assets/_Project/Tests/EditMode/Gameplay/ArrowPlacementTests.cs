using CBJ.Core;
using CBJ.Gameplay;
using CBJ.Tests.EditMode.Core;
using NUnit.Framework;
using UnityEngine;

namespace CBJ.Tests.EditMode.Gameplay
{
    public class ArrowPlacementTests
    {
        [Test]
        public void Find_HorizontalBarAlongItsLength_SpansTheBar()
        {
            ArrowPlacement.Find(TestShapes.Bar3, Axis.Horizontal, out Vector2 center, out int length);

            Assert.That((center, length), Is.EqualTo((new Vector2(1.5f, 0.5f), 3)));
        }

        [Test]
        public void Find_HorizontalBarAcrossIt_IsOneCellLong()
        {
            ArrowPlacement.Find(TestShapes.Bar3, Axis.Vertical, out Vector2 center, out int length);

            Assert.That((center, length), Is.EqualTo((new Vector2(1.5f, 0.5f), 1)));
        }

        [Test]
        public void Find_LShapeVertical_UsesTheFullColumn()
        {
            ArrowPlacement.Find(TestShapes.L, Axis.Vertical, out Vector2 center, out int length);

            Assert.That((center, length), Is.EqualTo((new Vector2(0.5f, 1f), 2)));
        }

        [Test]
        public void Find_SquareVertical_PicksAColumnNextToTheMiddle()
        {
            ArrowPlacement.Find(TestShapes.Square, Axis.Vertical, out Vector2 center, out int length);

            Assert.That((Mathf.Abs(center.x - 1f), center.y, length), Is.EqualTo((0.5f, 1f, 2)));
        }
    }
}
