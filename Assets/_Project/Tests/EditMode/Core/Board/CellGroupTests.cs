using CBJ.Core;
using NUnit.Framework;
using UnityEngine;

namespace CBJ.Tests.EditMode.Core
{
    public class CellGroupTests
    {
        [Test]
        public void IsConnected_EdgeNeighbours_IsTrue()
        {
            Assert.That(CellGroup.IsConnected(TestShapes.L.Cells), Is.True);
        }

        [Test]
        public void IsConnected_DiagonalOnly_IsFalse()
        {
            Assert.That(CellGroup.IsConnected(new[] { Vector2Int.zero, Vector2Int.one }), Is.False);
        }

        [Test]
        public void IsConnected_NoCells_IsFalse()
        {
            Assert.That(CellGroup.IsConnected(new Vector2Int[0]), Is.False);
        }

        [Test]
        public void ConnectedTo_TwoGroups_ReturnsOnlyTheStartGroup()
        {
            var cells = new[] { Vector2Int.zero, Vector2Int.right, new Vector2Int(3, 0), new Vector2Int(3, 1) };

            Assert.That(CellGroup.ConnectedTo(cells, new Vector2Int(3, 1)), Is.EquivalentTo(new[] { new Vector2Int(3, 0), new Vector2Int(3, 1) }));
        }

        [Test]
        public void ConnectedTo_StartNotInSet_IsEmpty()
        {
            Assert.That(CellGroup.ConnectedTo(new[] { Vector2Int.zero }, Vector2Int.one), Is.Empty);
        }
    }
}
