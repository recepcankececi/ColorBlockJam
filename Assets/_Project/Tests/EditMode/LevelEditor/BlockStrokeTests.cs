using CBJ.Core;
using CBJ.Editor;
using NUnit.Framework;
using UnityEngine;

namespace CBJ.Tests.EditMode.LevelEditor
{
    public class BlockStrokeTests
    {
        [Test]
        public void ExtendTo_SkippedCells_FillsAConnectedPath()
        {
            var stroke = new BlockStroke();
            stroke.Begin(Vector2Int.zero, -1);

            stroke.ExtendTo(new Vector2Int(3, 2));

            Assert.That((stroke.Cells.Count, CellGroup.IsConnected(stroke.Cells)), Is.EqualTo((6, true)));
        }

        [Test]
        public void ExtendTo_BackOverVisitedCells_KeepsEachCellOnce()
        {
            var stroke = new BlockStroke();
            stroke.Begin(Vector2Int.zero, -1);
            stroke.ExtendTo(new Vector2Int(2, 0));

            stroke.ExtendTo(Vector2Int.zero);

            Assert.That(stroke.Cells, Is.Unique);
        }

        [Test]
        public void End_AfterStroke_ClearsCellsAndTarget()
        {
            var stroke = new BlockStroke();
            stroke.Begin(Vector2Int.zero, 2);

            stroke.End();

            Assert.That((stroke.IsActive, stroke.Cells.Count, stroke.ExtendIndex), Is.EqualTo((false, 0, -1)));
        }
    }
}
