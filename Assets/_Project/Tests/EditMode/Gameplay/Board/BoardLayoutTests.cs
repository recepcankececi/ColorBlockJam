using System;
using CBJ.Core;
using CBJ.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace CBJ.Tests.EditMode.Gameplay
{
    public class BoardLayoutTests
    {
        [Test]
        public void CellCenter_MiddleCellOfOddBoard_IsLocalOrigin()
        {
            var layout = new BoardLayout(3, 3);

            Assert.That(layout.CellCenter(Vector2Int.one), Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void CellCenter_BottomLeftCell_SitsHalfACellInsideTheCorner()
        {
            var layout = new BoardLayout(2, 3);

            Assert.That(layout.CellCenter(Vector2Int.zero), Is.EqualTo(new Vector3(-1f, 0f, -2f)));
        }

        [Test]
        public void ToLocal_BoardUp_IsLocalForward()
        {
            var layout = new BoardLayout(2, 2);

            Vector3 step = layout.ToLocal(Vector2.up) - layout.ToLocal(Vector2.zero);

            Assert.That(step, Is.EqualTo(Vector3.forward * BoardLayout.CellSize));
        }

        [Test]
        public void ToCellSpace_OfToLocal_ReturnsTheSamePoint()
        {
            var layout = new BoardLayout(5, 7);
            var point = new Vector2(3.25f, 1.5f);

            Assert.That(layout.ToCellSpace(layout.ToLocal(point)), Is.EqualTo(point));
        }

        [Test]
        public void EdgePoint_UpEdgeSlotCentre_SitsInTheMiddleOfTheWallRing()
        {
            var layout = new BoardLayout(2, 2);

            Vector3 point = layout.EdgePoint(Direction.Up, 0.5f);

            Assert.That(point, Is.EqualTo(new Vector3(-1f, 0f, 2f + BoardLayout.WallThickness * 0.5f)));
        }

        [Test]
        public void EdgePoint_LeftEdge_CountsFromTheBottom()
        {
            var layout = new BoardLayout(2, 2);

            Vector3 point = layout.EdgePoint(Direction.Left, 0.5f);

            Assert.That(point, Is.EqualTo(new Vector3(-2f - BoardLayout.WallThickness * 0.5f, 0f, -1f)));
        }

        [Test]
        public void CornerPoint_TopRight_IsOutsideBothEdges()
        {
            var layout = new BoardLayout(2, 3);
            float ring = BoardLayout.WallThickness * 0.5f;

            Assert.That(layout.CornerPoint(1, 1), Is.EqualTo(new Vector3(2f + ring, 0f, 3f + ring)));
        }

        [Test]
        public void QuarterTurns_Right_IsOneClockwiseTurnFromUp()
        {
            Assert.That(BoardLayout.QuarterTurns(Direction.Right), Is.EqualTo(1));
        }

        [Test]
        public void Constructor_ZeroWidth_Throws()
        {
            Assert.That(() => new BoardLayout(0, 3), Throws.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}
