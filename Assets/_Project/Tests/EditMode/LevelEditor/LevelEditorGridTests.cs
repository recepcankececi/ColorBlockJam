using CBJ.Core;
using CBJ.Editor;
using NUnit.Framework;
using UnityEngine;

namespace CBJ.Tests.EditMode.LevelEditor
{
    /// <summary>
    /// A 4×3 board in a 600×500 area lays out at 64 px cells with the board's top-left at (172, 154).
    /// </summary>
    public class LevelEditorGridTests
    {
        private const float Cell = 64f;
        private static readonly Vector2 BoardTopLeft = new Vector2(172f, 154f);

        private LevelEditorGrid _grid;

        [SetUp]
        public void LayOut()
        {
            _grid = new LevelEditorGrid();
            _grid.Layout(new Rect(0f, 0f, 600f, 500f), 4, 3);
        }

        [Test]
        public void HitTest_TopLeftScreenCell_IsTopRowOfBoard()
        {
            GridHit hit = _grid.HitTest(PointIn(0, 0));

            Assert.That((hit.Kind, hit.Cell), Is.EqualTo((GridHitKind.Cell, new Vector2Int(0, 2))));
        }

        [Test]
        public void HitTest_BottomRightScreenCell_IsBoardRowZero()
        {
            GridHit hit = _grid.HitTest(PointIn(3, 2));

            Assert.That(hit.Cell, Is.EqualTo(new Vector2Int(3, 0)));
        }

        [Test]
        public void HitTest_AboveBoard_IsUpSlot()
        {
            GridHit hit = _grid.HitTest(PointIn(2, -1));

            Assert.That((hit.Kind, hit.Edge, hit.Slot), Is.EqualTo((GridHitKind.DoorSlot, Direction.Up, 2)));
        }

        [Test]
        public void HitTest_LeftOfBottomRow_IsLeftSlotZero()
        {
            GridHit hit = _grid.HitTest(PointIn(-1, 2));

            Assert.That((hit.Kind, hit.Edge, hit.Slot), Is.EqualTo((GridHitKind.DoorSlot, Direction.Left, 0)));
        }

        [Test]
        public void HitTest_RightOfTopRow_IsRightSlotAtTop()
        {
            GridHit hit = _grid.HitTest(PointIn(4, 0));

            Assert.That((hit.Edge, hit.Slot), Is.EqualTo((Direction.Right, 2)));
        }

        [Test]
        public void HitTest_BelowBoard_IsDownSlot()
        {
            GridHit hit = _grid.HitTest(PointIn(1, 3));

            Assert.That((hit.Edge, hit.Slot), Is.EqualTo((Direction.Down, 1)));
        }

        [Test]
        public void HitTest_DiagonalCorner_IsNone()
        {
            GridHit hit = _grid.HitTest(PointIn(-1, -1));

            Assert.That(hit.Kind, Is.EqualTo(GridHitKind.None));
        }

        // Centre of the screen cell (column, row), counting from the board's top-left.
        private static Vector2 PointIn(int column, int row)
        {
            return BoardTopLeft + new Vector2((column + 0.5f) * Cell, (row + 0.5f) * Cell);
        }
    }
}
