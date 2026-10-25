using CBJ.Core;
using CBJ.Gameplay;
using CBJ.Tests.EditMode.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace CBJ.Tests.EditMode.Gameplay
{
    public class BlockDragTests
    {
        private const int Red = 0;
        private const int Blue = 1;
        private const float AxisLock = 0.2f;
        private const float Tolerance = 1e-4f;

        // Grabbing every block at the centre of its origin cell keeps the lead equal to the finger's travel.
        private static readonly Vector2 CellCentre = new Vector2(0.5f, 0.5f);

        [Test]
        public void Drag_LeadUnderHalfACell_FollowsWithoutStepping()
        {
            BlockDrag drag = GrabFirst(Board4x3(new Vector2Int(1, 1)), out Block block);

            drag.Drag(Finger(block, 0.3f, 0f));

            AssertDrag(drag, block, new Vector2Int(1, 1), new Vector2(0.3f, 0f));
        }

        [Test]
        public void Drag_LeadPastHalfACell_StepsTheModelOnce()
        {
            BlockDrag drag = GrabFirst(Board4x3(new Vector2Int(1, 1)), out Block block);

            drag.Drag(Finger(block, 0.6f, 0f));

            AssertDrag(drag, block, new Vector2Int(2, 1), new Vector2(-0.4f, 0f));
        }

        [Test]
        public void Drag_LeadOfSeveralCells_StepsUpToTheWallAndRestsOnTheCell()
        {
            BlockDrag drag = GrabFirst(Board4x3(new Vector2Int(1, 1)), out Block block);

            drag.Drag(Finger(block, 5f, 0f));

            AssertDrag(drag, block, new Vector2Int(3, 1), Vector2.zero);
        }

        [Test]
        public void Drag_TowardsAWall_KeepsTheBlockOnItsCell()
        {
            BlockDrag drag = GrabFirst(Board4x3(new Vector2Int(0, 1)), out Block block);

            drag.Drag(Finger(block, -0.3f, 0f));

            AssertDrag(drag, block, new Vector2Int(0, 1), Vector2.zero);
        }

        [Test]
        public void Drag_TowardsAnotherBlock_KeepsTheBlockOnItsCell()
        {
            BlockDrag drag = GrabFirst(Board4x3(new Vector2Int(1, 1), new Vector2Int(2, 1)), out Block block);

            drag.Drag(Finger(block, 0.4f, 0f));

            AssertDrag(drag, block, new Vector2Int(1, 1), Vector2.zero);
        }

        [Test]
        public void Drag_BackAwayFromAWall_FollowsTheFinger()
        {
            BlockDrag drag = GrabFirst(Board4x3(new Vector2Int(0, 1)), out Block block);

            drag.Drag(Finger(block, 0.3f, 0f));

            AssertDrag(drag, block, new Vector2Int(0, 1), new Vector2(0.3f, 0f));
        }

        [Test]
        public void Drag_DiagonalLead_FollowsOnlyTheDominantAxis()
        {
            BlockDrag drag = GrabFirst(Board4x3(new Vector2Int(1, 1)), out Block block);

            drag.Drag(Finger(block, 0.3f, 0.25f));

            AssertDrag(drag, block, new Vector2Int(1, 1), new Vector2(0.3f, 0f));
        }

        [Test]
        public void Drag_DominantAxisBlocked_SlidesAlongTheOtherAxis()
        {
            BlockDrag drag = GrabFirst(Board4x3(new Vector2Int(0, 1)), out Block block);

            drag.Drag(Finger(block, -0.4f, 0.3f));

            AssertDrag(drag, block, new Vector2Int(0, 1), new Vector2(0f, 0.3f));
        }

        [Test]
        public void Drag_AwayFromTheCell_KeepsItsAxis()
        {
            BlockDrag drag = GrabFirst(Board4x3(new Vector2Int(1, 1)), out Block block);
            drag.Drag(Finger(block, 0.4f, 0f));

            drag.Drag(Finger(block, 0.4f, 0.45f));

            AssertDrag(drag, block, new Vector2Int(1, 1), new Vector2(0.4f, 0f));
        }

        [Test]
        public void Drag_BackNearTheCell_CanChangeAxis()
        {
            BlockDrag drag = GrabFirst(Board4x3(new Vector2Int(1, 1)), out Block block);
            drag.Drag(Finger(block, 0.4f, 0f));
            drag.Drag(Finger(block, 0.1f, 0f));

            drag.Drag(Finger(block, 0.1f, 0.4f));

            AssertDrag(drag, block, new Vector2Int(1, 1), new Vector2(0f, 0.4f));
        }

        [Test]
        public void Drag_StepOntoTheEdgeOfAMatchingDoor_ExitsWithoutPushingPastTheEdge()
        {
            LevelSession session = Begin(DoorLevel(Direction.Right, 1, Red, new Vector2Int(1, 1)));
            BlockDrag drag = GrabFirst(session, out Block block);

            drag.Drag(Finger(block, 0.6f, 0f));

            Assert.That((block.IsExited, drag.Held), Is.EqualTo((true, (Block)null)));
        }

        [Test]
        public void Drag_StepOntoTheEdgeOfAnotherColoursDoor_StaysOnTheBoard()
        {
            LevelSession session = Begin(DoorLevel(Direction.Right, 1, Blue, new Vector2Int(1, 1)));
            BlockDrag drag = GrabFirst(session, out Block block);

            drag.Drag(Finger(block, 0.6f, 0f));

            Assert.That((block.IsExited, drag.Held), Is.EqualTo((false, block)));
        }

        [Test]
        public void Drag_StepAlongAnEdgeToItsDoor_ExitsThroughThatEdge()
        {
            LevelSession session = Begin(DoorLevel(Direction.Down, 2, Red, new Vector2Int(1, 0)));
            BlockDrag drag = GrabFirst(session, out Block block);

            drag.Drag(Finger(block, 0.6f, 0f));

            Assert.That(block.IsExited, Is.True);
        }

        [Test]
        public void Drag_WhilePaused_LeavesTheBlockOnItsCell()
        {
            LevelSession session = Begin(Board4x3(new Vector2Int(1, 1)));
            BlockDrag drag = GrabFirst(session, out Block block);
            session.Pause();

            drag.Drag(Finger(block, 0.6f, 0f));

            AssertDrag(drag, block, new Vector2Int(1, 1), Vector2.zero);
        }

        [Test]
        public void Release_WhileHolding_ClearsTheHeldBlockAndOffset()
        {
            BlockDrag drag = GrabFirst(Board4x3(new Vector2Int(1, 1)), out Block block);
            drag.Drag(Finger(block, 0.3f, 0f));

            drag.Release();

            Assert.That((drag.Held, drag.Offset), Is.EqualTo(((Block)null, Vector2.zero)));
        }

        [Test]
        public void FindExit_BlockAwayFromTheEdges_ReturnsFalse()
        {
            LevelSession session = Begin(DoorLevel(Direction.Right, 1, Red, new Vector2Int(1, 1)));

            bool found = BlockDrag.FindExit(session, session.Board.Blocks[0], Direction.Right, out _);

            Assert.That(found, Is.False);
        }

        [Test]
        public void FindExit_TwoDoorsOpen_PrefersTheGivenEdge()
        {
            var level = new LevelData(3, 3, 30);
            level.AddDoor(new DoorData(Direction.Left, 0, 1, Red));
            level.AddDoor(new DoorData(Direction.Down, 0, 1, Red));
            level.AddBlock(new BlockData(TestShapes.Single.Cells, Vector2Int.zero, Red));
            LevelSession session = Begin(level);

            BlockDrag.FindExit(session, session.Board.Blocks[0], Direction.Down, out Direction exit);

            Assert.That(exit, Is.EqualTo(Direction.Down));
        }

        [Test]
        public void Drag_SteppingBackAndForth_DoesNotAllocate()
        {
            BlockDrag drag = GrabFirst(Board4x3(new Vector2Int(1, 1)), out Block block);
            TestDelegate drags = () =>
            {
                drag.Drag(new Vector2(2.1f, 1.5f));
                drag.Drag(new Vector2(1.4f, 1.5f));
            };
            drags();

            Assert.That(drags, Is.Not.AllocatingGCMemory());
        }

        private static BlockDrag GrabFirst(LevelData level, out Block block)
        {
            return GrabFirst(Begin(level), out block);
        }

        private static BlockDrag GrabFirst(LevelSession session, out Block block)
        {
            block = session.Board.Blocks[0];
            var drag = new BlockDrag(session, AxisLock);
            drag.Grab(block, block.Origin + CellCentre);
            return drag;
        }

        // The finger point that leads the block's current cell by (x, y) cells.
        private static Vector2 Finger(Block block, float x, float y)
        {
            return block.Origin + CellCentre + new Vector2(x, y);
        }

        private static void AssertDrag(BlockDrag drag, Block block, Vector2Int origin, Vector2 offset)
        {
            Assert.That(block.Origin, Is.EqualTo(origin));
            Assert.That(Vector2.Distance(drag.Offset, offset), Is.LessThan(Tolerance), $"Offset was {drag.Offset}.");
        }

        // A 4×3 board without doors and a red 1×1 block at each origin, the first one being dragged.
        private static LevelData Board4x3(params Vector2Int[] origins)
        {
            var level = new LevelData(4, 3, 30);
            foreach (Vector2Int origin in origins)
            {
                level.AddBlock(new BlockData(TestShapes.Single.Cells, origin, Red));
            }

            return level;
        }

        // A 3×3 board with one door of one cell and a red 1×1 block.
        private static LevelData DoorLevel(Direction edge, int offset, int doorColor, Vector2Int origin)
        {
            var level = new LevelData(3, 3, 30);
            level.AddDoor(new DoorData(edge, offset, 1, doorColor));
            level.AddBlock(new BlockData(TestShapes.Single.Cells, origin, Red));
            return level;
        }

        private static LevelSession Begin(LevelData level)
        {
            var session = new LevelSession(level);
            session.Begin();
            return session;
        }
    }
}
