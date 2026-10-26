using System.Collections.Generic;
using System.Linq;
using CBJ.Core;
using CBJ.Editor;
using CBJ.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace CBJ.Tests.EditMode.Gameplay
{
    public class BlockPartLayoutTests
    {
        private const int QuadrantsPerCell = 4;
        private const int QuadrantsPerInnerCorner = 3;

        [Test]
        public void Build_SingleCell_UsesFourOuterCorners()
        {
            List<BlockPart> parts = Build("I1");

            Assert.That(parts.Select(p => p.Kind), Is.EqualTo(Enumerable.Repeat(BlockPartKind.OuterCorner, 4)));
        }

        [Test]
        public void Build_SingleCell_TurnsEachCornerToItsQuadrant()
        {
            List<BlockPart> parts = Build("I1");

            int TurnsAt(float x, float y) => parts.Single(p => p.Center == new Vector2(x, y)).QuarterTurns;
            Assert.That(new[] { TurnsAt(0.75f, 0.75f), TurnsAt(0.75f, 0.25f), TurnsAt(0.25f, 0.25f), TurnsAt(0.25f, 0.75f) },
                Is.EqualTo(new[] { 0, 1, 2, 3 }));
        }

        [Test]
        public void Build_Square_HasCornersEdgesAndCentres()
        {
            List<BlockPart> parts = Build("O2");

            Assert.That((Count(parts, BlockPartKind.OuterCorner), Count(parts, BlockPartKind.Edge), Count(parts, BlockPartKind.Center)),
                Is.EqualTo((4, 8, 4)));
        }

        [Test]
        public void Build_HorizontalDomino_EdgesFaceUpAndDown()
        {
            List<BlockPart> edges = Build("I2-H").Where(p => p.Kind == BlockPartKind.Edge).ToList();

            Assert.That(edges.Select(p => p.QuarterTurns).OrderBy(t => t),
                Is.EqualTo(new[] { BoardLayout.QuarterTurns(Direction.Up), BoardLayout.QuarterTurns(Direction.Up),
                    BoardLayout.QuarterTurns(Direction.Down), BoardLayout.QuarterTurns(Direction.Down) }.OrderBy(t => t)));
        }

        [Test]
        public void Build_LWithNotchTopRight_PlacesOneInnerCornerAtTheNotch()
        {
            List<BlockPart> inner = Build("L3-0").Where(p => p.Kind == BlockPartKind.InnerCorner).ToList();

            Assert.That(inner.Select(p => (p.Center, p.QuarterTurns)), Is.EqualTo(new[] { (new Vector2(1f, 1f), 0) }));
        }

        [Test]
        public void Build_EveryLibraryShape_CoversEachQuadrantExactlyOnce()
        {
            var mismatches = new List<string>();
            foreach (string id in ShapeLibrary.Ids)
            {
                List<BlockPart> parts = Build(id);
                int covered = parts.Sum(p => p.Kind == BlockPartKind.InnerCorner ? QuadrantsPerInnerCorner : 1);
                int expected = ShapeLibrary.Get(id).Cells.Count * QuadrantsPerCell;
                if (covered != expected)
                {
                    mismatches.Add($"{id}: {covered}/{expected}");
                }
            }

            Assert.That(mismatches, Is.Empty);
        }

        [Test]
        public void Build_ReusedList_ReplacesPreviousParts()
        {
            var parts = new List<BlockPart>();
            BlockPartLayout.Build(ShapeLibrary.Get("O2"), parts);

            BlockPartLayout.Build(ShapeLibrary.Get("I1"), parts);

            Assert.That(parts.Count, Is.EqualTo(4));
        }

        private static List<BlockPart> Build(string shapeId)
        {
            var parts = new List<BlockPart>();
            BlockPartLayout.Build(ShapeLibrary.Get(shapeId), parts);
            return parts;
        }

        private static int Count(IEnumerable<BlockPart> parts, BlockPartKind kind)
        {
            return parts.Count(p => p.Kind == kind);
        }
    }
}
