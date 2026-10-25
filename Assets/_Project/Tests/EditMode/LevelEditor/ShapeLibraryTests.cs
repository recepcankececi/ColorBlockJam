using System;
using System.Collections.Generic;
using System.Linq;
using CBJ.Core;
using CBJ.Editor;
using NUnit.Framework;
using UnityEngine;

namespace CBJ.Tests.EditMode.LevelEditor
{
    public class ShapeLibraryTests
    {
        [Test]
        public void Ids_EveryId_Resolves()
        {
            foreach (string id in ShapeLibrary.Ids)
            {
                Assert.That(ShapeLibrary.TryGet(id, out _), Is.True, id);
            }
        }

        [Test]
        public void Ids_NoDuplicates_AreUnique()
        {
            Assert.That(ShapeLibrary.Ids, Is.Unique);
        }

        [Test]
        public void Get_L3Zero_HasNotchTopRight()
        {
            BlockShape shape = ShapeLibrary.Get("L3-0");

            Assert.That(shape.Cells, Is.EquivalentTo(new[] { Vector2Int.zero, Vector2Int.right, Vector2Int.up }));
        }

        [Test]
        public void Get_VerticalBar_IsOneWideThreeHigh()
        {
            BlockShape shape = ShapeLibrary.Get("I3-V");

            Assert.That((shape.Width, shape.Height), Is.EqualTo((1, 3)));
        }

        [Test]
        public void StampIds_WithRotation_CoverEveryId()
        {
            var uncovered = ShapeLibrary.Ids.Where(id => !ShapeLibrary.StampIds.Any(stamp =>
                Enumerable.Range(0, ShapeRotation.TurnsPerCircle).Any(turns =>
                    new HashSet<Vector2Int>(ShapeRotation.RotateClockwise(ShapeLibrary.Get(stamp).Cells, turns))
                        .SetEquals(ShapeLibrary.Get(id).Cells))));

            Assert.That(uncovered, Is.Empty);
        }

        [Test]
        public void Get_UnknownId_Throws()
        {
            Assert.Throws<ArgumentException>(() => ShapeLibrary.Get("no-such-shape"));
        }

        [Test]
        public void TryGet_NullId_ReturnsFalse()
        {
            Assert.That(ShapeLibrary.TryGet(null, out _), Is.False);
        }
    }
}
