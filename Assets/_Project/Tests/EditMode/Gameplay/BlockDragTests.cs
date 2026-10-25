using System.Collections.Generic;
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
        private const float Tolerance = 1e-4f;
        private const int WalkSeed = 20261025;
        private const int WalkFrames = 2000;
        private const int GrabEveryFrames = 50;
        private const float Frame = 1f / 60f;
        private const float MaxSpeed = 30f;
        private const float SmoothTime = 0.04f;
        private const float AlignTolerance = 0.08f;
        private const int SmoothFrames = 120;
        private const float SmoothArrival = 1e-3f;

        // Grabbing every block at the centre of its origin cell keeps the lead equal to the finger's travel.
        private static readonly Vector2 CellCentre = new Vector2(0.5f, 0.5f);

        [Test]
        public void Drag_LeadOnBothAxes_FollowsTheFingerWithoutAnAxisLock()
        {
            BlockDrag drag = GrabFirst(Board4x3(new Vector2Int(1, 1)), out Block block, out Vector2 grab);

            drag.Drag(grab + new Vector2(0.3f, 0.2f), Frame);

            AssertDrag(drag, block, new Vector2Int(1, 1), new Vector2(1.3f, 1.2f));
        }

        [Test]
        public void Drag_LeadUnderHalfACell_KeepsTheModelCell()
        {
            BlockDrag drag = GrabFirst(Board4x3(new Vector2Int(1, 1)), out Block block, out Vector2 grab);

            drag.Drag(grab + new Vector2(0.45f, 0f), Frame);

            AssertDrag(drag, block, new Vector2Int(1, 1), new Vector2(1.45f, 1f));
        }

        [Test]
        public void Drag_LeadPastHalfACell_CommitsTheRoundedCell()
        {
            BlockDrag drag = GrabFirst(Board4x3(new Vector2Int(1, 1)), out Block block, out Vector2 grab);

            drag.Drag(grab + new Vector2(0.6f, 0f), Frame);

            AssertDrag(drag, block, new Vector2Int(2, 1), new Vector2(1.6f, 1f));
        }

        [Test]
        public void Drag_LeadPastHalfACellOnBothAxes_CommitsTheDiagonalCell()
        {
            BlockDrag drag = GrabFirst(Board4x3(new Vector2Int(1, 0)), out Block block, out Vector2 grab);

            drag.Drag(grab + new Vector2(0.7f, 0.8f), Frame);

            AssertDrag(drag, block, new Vector2Int(2, 1), new Vector2(1.7f, 0.8f));
        }

        [Test]
        public void Drag_LeadBackUnderHalfACell_CommitsTheCellBack()
        {
            BlockDrag drag = GrabFirst(Board4x3(new Vector2Int(1, 1)), out Block block, out Vector2 grab);
            drag.Drag(grab + new Vector2(0.6f, 0f), Frame);

            drag.Drag(grab + new Vector2(0.4f, 0f), Frame);

            AssertDrag(drag, block, new Vector2Int(1, 1), new Vector2(1.4f, 1f));
        }

        [Test]
        public void Drag_TowardsABlockOnX_StopsAgainstItsFace()
        {
            BlockDrag drag = GrabFirst(Board4x3(new Vector2Int(0, 1), new Vector2Int(2, 1)), out Block block, out Vector2 grab);

            drag.Drag(grab + new Vector2(1.7f, 0f), Frame);

            AssertDrag(drag, block, new Vector2Int(1, 1), new Vector2(1f, 1f));
        }

        [Test]
        public void Drag_TowardsABlockOnY_StopsAgainstItsFace()
        {
            BlockDrag drag = GrabFirst(Board4x3(new Vector2Int(1, 0), new Vector2Int(1, 2)), out Block block, out Vector2 grab);

            drag.Drag(grab + new Vector2(0f, 1.6f), Frame);

            AssertDrag(drag, block, new Vector2Int(1, 1), new Vector2(1f, 1f));
        }

        [Test]
        public void Drag_LeadOfSeveralCells_StopsAtTheWallWithoutTunnelling()
        {
            BlockDrag drag = GrabFirst(Board4x3(new Vector2Int(1, 1)), out Block block, out Vector2 grab);

            drag.Drag(grab + new Vector2(5f, 0f), Frame);

            AssertDrag(drag, block, new Vector2Int(3, 1), new Vector2(3f, 1f));
        }

        [Test]
        public void Drag_LeadOfSeveralCells_StopsAtTheFirstBlockInTheWay()
        {
            BlockDrag drag = GrabFirst(Board4x3(new Vector2Int(0, 1), new Vector2Int(3, 1)), out Block block, out Vector2 grab);

            drag.Drag(grab + new Vector2(6f, 0f), Frame);

            AssertDrag(drag, block, new Vector2Int(2, 1), new Vector2(2f, 1f));
        }

        [Test]
        public void Drag_DiagonallyIntoTheBottomWall_SlidesAlongIt()
        {
            BlockDrag drag = GrabFirst(Board4x3(new Vector2Int(1, 0)), out Block block, out Vector2 grab);

            drag.Drag(grab + new Vector2(0.8f, -0.5f), Frame);

            AssertDrag(drag, block, new Vector2Int(2, 0), new Vector2(1.8f, 0f));
        }

        [Test]
        public void Drag_DiagonallyIntoABlock_SlidesAlongItsSide()
        {
            BlockDrag drag = GrabFirst(Board4x3(new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1)),
                out Block block, out Vector2 grab);

            drag.Drag(grab + new Vector2(1.4f, 0.6f), Frame);

            AssertDrag(drag, block, new Vector2Int(1, 0), new Vector2(1.4f, 0f));
        }

        [Test]
        public void Drag_BlockedOnXThenFreeOnY_MovesOnlyAlongY()
        {
            BlockDrag drag = GrabFirst(Board4x3(new Vector2Int(0, 0), new Vector2Int(1, 0)), out Block block, out Vector2 grab);

            drag.Drag(grab + new Vector2(0.8f, 0.3f), Frame);

            AssertDrag(drag, block, new Vector2Int(0, 0), new Vector2(0f, 0.3f));
        }

        [Test]
        public void Drag_PastAGapBetweenBlocks_SlipsThroughWhenAligned()
        {
            // Blocks at (1,0) and (1,2) leave a one-cell gap at (1,1); the held block enters it from the left.
            BlockDrag drag = GrabFirst(Board4x3(new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(1, 2)),
                out Block block, out Vector2 grab);

            drag.Drag(grab + new Vector2(2f, 0f), Frame);

            AssertDrag(drag, block, new Vector2Int(2, 1), new Vector2(2f, 1f));
        }

        [Test]
        public void Drag_IntoTheGapSlightlyOffItsRow_IsStoppedByTheBlockBeside()
        {
            BlockDrag drag = GrabFirst(Board4x3(new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(1, 2)),
                out Block block, out Vector2 grab);
            drag.Drag(grab + new Vector2(0f, 0.2f), Frame);

            drag.Drag(grab + new Vector2(2f, 0.2f), Frame);

            AssertDrag(drag, block, new Vector2Int(0, 1), new Vector2(0f, 1.2f));
        }

        [Test]
        public void Drag_WiderBlockIntoANarrowDoorOfItsColour_IsStoppedByTheWallBeside()
        {
            var level = new LevelData(4, 3, 30);
            level.AddDoor(new DoorData(Direction.Down, 1, 1, Red));
            level.AddBlock(new BlockData(TestShapes.Domino.Cells, new Vector2Int(1, 0), Red));
            BlockDrag drag = GrabFirst(level, out Block block, out Vector2 grab);

            drag.Drag(grab + new Vector2(0f, -0.4f), Frame);

            Assert.That(block.IsExited, Is.False);
            AssertDrag(drag, block, new Vector2Int(1, 0), new Vector2(1f, 0f));
        }

        [Test]
        public void Drag_LShapeWhoseArmFitsANarrowDoorOfItsColour_StopsAtTheEdge()
        {
            // The L's lower arm lines up with the one-cell door, but the whole side (two rows) does not fit it.
            var level = new LevelData(4, 3, 30);
            level.AddDoor(new DoorData(Direction.Right, 1, 1, Red));
            level.AddBlock(new BlockData(TestShapes.L.Cells, new Vector2Int(1, 1), Red));
            BlockDrag drag = GrabFirst(level, out Block block, out Vector2 grab);

            drag.Drag(grab + new Vector2(1.8f, 0f), Frame);

            Assert.That(block.IsExited, Is.False);
            AssertDrag(drag, block, new Vector2Int(2, 1), new Vector2(2f, 1f));
        }

        [Test]
        public void Drag_IntoADoorOfAnotherColour_IsStoppedLikeAWall()
        {
            BlockDrag drag = GrabFirst(DoorLevel(Direction.Down, 1, Blue, new Vector2Int(1, 0)), out Block block, out Vector2 grab);

            drag.Drag(grab + new Vector2(0f, -0.4f), Frame);

            AssertDrag(drag, block, new Vector2Int(1, 0), new Vector2(1f, 0f));
        }

        [Test]
        public void Drag_StepOntoTheEdgeOfAMatchingDoor_ExitsAtOnce()
        {
            BlockDrag drag = GrabFirst(DoorLevel(Direction.Right, 1, Red, new Vector2Int(1, 1)), out Block block, out Vector2 grab);

            drag.Drag(grab + new Vector2(0.6f, 0f), Frame);

            Assert.That((block.IsExited, drag.Held), Is.EqualTo((true, (Block)null)));
        }

        [Test]
        public void Drag_StepOntoTheEdgeOfAnotherColoursDoor_StaysOnTheBoard()
        {
            BlockDrag drag = GrabFirst(DoorLevel(Direction.Right, 1, Blue, new Vector2Int(1, 1)), out Block block, out Vector2 grab);

            drag.Drag(grab + new Vector2(0.6f, 0f), Frame);

            Assert.That((block.IsExited, drag.Held), Is.EqualTo((false, block)));
        }

        [Test]
        public void Drag_StepAlongAnEdgeToItsDoorOffItsLine_KeepsHoldingTheBlock()
        {
            BlockDrag drag = GrabFirst(DoorLevel(Direction.Down, 2, Red, new Vector2Int(1, 0)), out Block block, out Vector2 grab);

            drag.Drag(grab + new Vector2(0.6f, 0f), Frame);

            Assert.That((block.IsExited, drag.Held), Is.EqualTo((false, block)));
        }

        [Test]
        public void Drag_StepAlongAnEdgeToItsDoorWithinTheTolerance_ExitsThroughThatEdge()
        {
            BlockDrag drag = GrabFirst(DoorLevel(Direction.Down, 2, Red, new Vector2Int(1, 0)), out Block block, out Vector2 grab);

            drag.Drag(grab + new Vector2(1f + AlignTolerance * 0.5f, 0f), Frame);

            Assert.That(block.IsExited, Is.True);
        }

        [Test]
        public void Drag_SmoothedOffTheLineOfAWideDoorAndPulledOut_RestsFlushAtTheEdge()
        {
            var level = new LevelData(3, 3, 30);
            level.AddDoor(new DoorData(Direction.Down, 1, 2, Red));
            level.AddBlock(new BlockData(TestShapes.Single.Cells, new Vector2Int(1, 0), Red));
            LevelSession session = Begin(level);
            Block block = session.Board.Blocks[0];
            Vector2 grab = block.Origin + CellCentre;
            var drag = new BlockDrag(session, MaxSpeed, SmoothTime, AlignTolerance);
            drag.Grab(block, grab);
            for (int frame = 0; frame < SmoothFrames; frame++)
            {
                drag.Drag(grab + new Vector2(0.4f, 0f), Frame);
            }

            for (int frame = 0; frame < SmoothFrames; frame++)
            {
                drag.Drag(grab + new Vector2(0.4f, -2f), Frame);
                Assert.That(drag.Position.y, Is.GreaterThan(-Tolerance), $"Frame {frame}: y was {drag.Position.y}.");
            }

            Assert.That((block.IsExited, block.Origin), Is.EqualTo((false, new Vector2Int(1, 0))));
        }

        [Test]
        public void LetGo_OnTheEdgeOfItsDoorOffItsLine_ExitsThroughThatEdge()
        {
            BlockDrag drag = GrabFirst(DoorLevel(Direction.Down, 2, Red, new Vector2Int(1, 0)), out Block block, out Vector2 grab);
            drag.Drag(grab + new Vector2(0.6f, 0f), Frame);

            bool exited = drag.LetGo();

            Assert.That((exited, block.IsExited, drag.Held), Is.EqualTo((true, true, (Block)null)));
        }

        [Test]
        public void LetGo_AwayFromItsDoor_KeepsTheBlockOnItsCell()
        {
            BlockDrag drag = GrabFirst(DoorLevel(Direction.Right, 1, Red, new Vector2Int(0, 1)), out Block block, out Vector2 grab);
            drag.Drag(grab + new Vector2(0.6f, 0f), Frame);

            bool exited = drag.LetGo();

            Assert.That((exited, block.IsExited, block.Origin, drag.Held),
                Is.EqualTo((false, false, new Vector2Int(1, 1), (Block)null)));
        }

        [Test]
        public void LetGo_WhilePausedOnTheEdgeOfItsDoor_KeepsTheBlock()
        {
            LevelSession session = Begin(DoorLevel(Direction.Down, 2, Red, new Vector2Int(1, 0)));
            BlockDrag drag = GrabFirst(session, out Block block, out Vector2 grab);
            drag.Drag(grab + new Vector2(0.6f, 0f), Frame);
            session.Pause();

            bool exited = drag.LetGo();

            Assert.That((exited, block.IsExited), Is.EqualTo((false, false)));
        }

        [Test]
        public void Drag_SmoothedFingerFarAway_MovesNoFurtherThanTheSpeedCapInOneFrame()
        {
            LevelSession session = Begin(Board4x3(new Vector2Int(0, 1)));
            Block block = session.Board.Blocks[0];
            Vector2 grab = block.Origin + CellCentre;
            var drag = new BlockDrag(session, MaxSpeed, SmoothTime, AlignTolerance);
            drag.Grab(block, grab);

            drag.Drag(grab + new Vector2(3f, 0f), Frame);

            Assert.That(drag.Position.x, Is.InRange(Tolerance, MaxSpeed * Frame + Tolerance));
        }

        [Test]
        public void Drag_SmoothedBlockFreedFromAnObstacle_ReachesTheFingerOverSeveralFrames()
        {
            // The finger waits up and to the right; the block beside it holds the held block back on X until the held
            // block has risen clear of it.
            LevelSession session = Begin(Board4x3(new Vector2Int(0, 0), new Vector2Int(1, 0)));
            Block block = session.Board.Blocks[0];
            Vector2 grab = block.Origin + CellCentre;
            var drag = new BlockDrag(session, MaxSpeed, SmoothTime, AlignTolerance);
            drag.Grab(block, grab);
            Vector2 finger = grab + new Vector2(3f, 1f);
            var goal = new Vector2(3f, 1f);
            int freedFrame = -1;
            int reachedFrame = -1;
            for (int frame = 0; frame < SmoothFrames && reachedFrame < 0; frame++)
            {
                Vector2 before = drag.Position;
                drag.Drag(finger, Frame);
                Assert.That(Vector2.Distance(before, drag.Position), Is.LessThanOrEqualTo(MaxSpeed * Frame + Tolerance),
                    $"Frame {frame}.");
                if (freedFrame < 0 && drag.Position.x > 0f)
                {
                    freedFrame = frame;
                }

                if (Vector2.Distance(drag.Position, goal) < SmoothArrival)
                {
                    reachedFrame = frame;
                }
            }

            Assert.That(freedFrame, Is.GreaterThan(0), "The obstacle never held the block back.");
            Assert.That(reachedFrame - freedFrame, Is.GreaterThan(1), $"Freed at {freedFrame}, reached at {reachedFrame}.");
        }

        [Test]
        public void Drag_RestingOnTheEdgeOfItsDoor_ExitsOnTheFirstMove()
        {
            BlockDrag drag = GrabFirst(DoorLevel(Direction.Right, 1, Red, new Vector2Int(2, 1)), out Block block, out Vector2 grab);

            drag.Drag(grab + new Vector2(0.1f, 0f), Frame);

            Assert.That(block.IsExited, Is.True);
        }

        [Test]
        public void Drag_ArrowBlock_MovesOnlyAlongItsAxis()
        {
            var level = new LevelData(4, 3, 30);
            level.AddBlock(new BlockData(TestShapes.Single.Cells, new Vector2Int(1, 1), Red, BlockTraits.Arrow(Axis.Horizontal)));
            BlockDrag drag = GrabFirst(level, out Block block, out Vector2 grab);

            drag.Drag(grab + new Vector2(0.3f, 0.4f), Frame);

            AssertDrag(drag, block, new Vector2Int(1, 1), new Vector2(1.3f, 1f));
        }

        [Test]
        public void Drag_WhilePaused_LeavesTheBlockOnItsCell()
        {
            LevelSession session = Begin(Board4x3(new Vector2Int(1, 1)));
            BlockDrag drag = GrabFirst(session, out Block block, out Vector2 grab);
            session.Pause();

            drag.Drag(grab + new Vector2(0.6f, 0.3f), Frame);

            AssertDrag(drag, block, new Vector2Int(1, 1), new Vector2(1f, 1f));
        }

        [Test]
        public void Offset_AfterACommit_IsThePositionRelativeToTheModelCell()
        {
            BlockDrag drag = GrabFirst(Board4x3(new Vector2Int(1, 1)), out _, out Vector2 grab);

            drag.Drag(grab + new Vector2(0.7f, -0.2f), Frame);

            Assert.That(Vector2.Distance(drag.Offset, new Vector2(-0.3f, -0.2f)), Is.LessThan(Tolerance));
        }

        [Test]
        public void Release_WhileHolding_ClearsTheHeldBlockAndOffset()
        {
            BlockDrag drag = GrabFirst(Board4x3(new Vector2Int(1, 1)), out _, out Vector2 grab);
            drag.Drag(grab + new Vector2(0.3f, 0f), Frame);

            drag.Release();

            Assert.That((drag.Held, drag.Offset), Is.EqualTo(((Block)null, Vector2.zero)));
        }

        [Test]
        public void RoundedFootprint_PositionsThatOverlapNothing_CoverOnlyFreeCells()
        {
            // The lemma the drag rests on: a unit box that overlaps no obstacle box is at least half a cell away from
            // it on some axis, so rounding never lands the box on that obstacle.
            BoardModel board = Begin(WalkLevel()).Board;
            Block held = board.Blocks[0];
            var random = new System.Random(WalkSeed);
            int checkedPositions = 0;
            for (int i = 0; i < WalkFrames * 5; i++)
            {
                var position = new Vector2(
                    (float)random.NextDouble() * (board.Width - held.Shape.Width),
                    (float)random.NextDouble() * (board.Height - held.Shape.Height));
                if (OverlapsAnotherBlock(board, held, position))
                {
                    continue;
                }

                checkedPositions++;
                Vector2Int rounded = Vector2Int.FloorToInt(position + CellCentre);
                Assert.That(board.CanOccupy(held, rounded), Is.True, $"Position {position} rounds onto a taken cell.");
            }

            Assert.That(checkedPositions, Is.GreaterThan(0));
        }

        [Test]
        public void Drag_RandomWalkOnACrowdedBoard_NeverOverlapsAndKeepsTheModelOnTheRoundedCell()
        {
            LevelSession session = Begin(WalkLevel());
            BoardModel board = session.Board;
            var drag = Instant(session);
            var random = new System.Random(WalkSeed);
            for (int frame = 0; frame < WalkFrames; frame++)
            {
                if (frame % GrabEveryFrames == 0)
                {
                    drag.Release();
                    Block next = board.Blocks[random.Next(board.Blocks.Count)];
                    drag.Grab(next, next.Origin + CellCentre);
                }

                // Mostly small finger moves, now and then a jump across the board.
                float reach = random.Next(10) == 0 ? board.Width : 0.4f;
                var lead = new Vector2((float)(random.NextDouble() * 2.0 - 1.0), (float)(random.NextDouble() * 2.0 - 1.0)) * reach;
                drag.Drag(drag.Position + CellCentre + lead, Frame);

                Block held = drag.Held;
                Assert.That(OverlapsAnotherBlock(board, held, drag.Position), Is.False, $"Frame {frame}: overlap at {drag.Position}.");
                Assert.That(Vector2Int.FloorToInt(drag.Position + CellCentre), Is.EqualTo(held.Origin), $"Frame {frame}.");
                Assert.That(IsInsideBoard(board, held, drag.Position), Is.True, $"Frame {frame}: outside at {drag.Position}.");
            }
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
        public void Drag_MovingAndCommittingBackAndForth_DoesNotAllocate()
        {
            BlockDrag drag = GrabFirst(Board4x3(new Vector2Int(1, 1), new Vector2Int(3, 2)), out _, out Vector2 grab);
            Vector2 forth = grab + new Vector2(1.3f, 0.4f);
            Vector2 back = grab + new Vector2(-0.6f, -0.2f);
            TestDelegate drags = () =>
            {
                drag.Drag(forth, Frame);
                drag.Drag(back, Frame);
            };
            drags();

            Assert.That(drags, Is.Not.AllocatingGCMemory());
        }

        private static BlockDrag GrabFirst(LevelData level, out Block block, out Vector2 grab)
        {
            return GrabFirst(Begin(level), out block, out grab);
        }

        private static BlockDrag GrabFirst(LevelSession session, out Block block, out Vector2 grab)
        {
            block = session.Board.Blocks[0];
            grab = block.Origin + CellCentre;
            var drag = Instant(session);
            drag.Grab(block, grab);
            return drag;
        }

        // Follows the finger at once, so one frame's drag lands where the finger is.
        private static BlockDrag Instant(LevelSession session)
        {
            return new BlockDrag(session, float.PositiveInfinity, 0f, AlignTolerance);
        }

        private static void AssertDrag(BlockDrag drag, Block block, Vector2Int origin, Vector2 position)
        {
            Assert.That(block.Origin, Is.EqualTo(origin));
            Assert.That(Vector2.Distance(drag.Position, position), Is.LessThan(Tolerance), $"Position was {drag.Position}.");
        }

        private static bool OverlapsAnotherBlock(BoardModel board, Block held, Vector2 position)
        {
            const float overlap = 1e-3f;
            IReadOnlyList<Vector2Int> cells = held.Shape.Cells;
            foreach (Block other in board.Blocks)
            {
                if (other == held || other.IsExited)
                {
                    continue;
                }

                foreach (Vector2Int otherCell in other.Shape.Cells)
                {
                    Vector2Int obstacle = other.Origin + otherCell;
                    foreach (Vector2Int cell in cells)
                    {
                        Vector2 box = position + cell;
                        if (Mathf.Abs(box.x - obstacle.x) < 1f - overlap && Mathf.Abs(box.y - obstacle.y) < 1f - overlap)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool IsInsideBoard(BoardModel board, Block held, Vector2 position)
        {
            const float slack = 1e-3f;
            return position.x >= -slack && position.y >= -slack &&
                position.x + held.Shape.Width <= board.Width + slack &&
                position.y + held.Shape.Height <= board.Height + slack;
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

        // A crowded 6×6 board of mixed shapes with no doors, so nothing ever leaves.
        private static LevelData WalkLevel()
        {
            var level = new LevelData(6, 6, 30);
            level.AddBlock(new BlockData(TestShapes.L.Cells, new Vector2Int(0, 0), Red));
            level.AddBlock(new BlockData(TestShapes.Domino.Cells, new Vector2Int(3, 0), Blue));
            level.AddBlock(new BlockData(TestShapes.Square.Cells, new Vector2Int(2, 2), Red));
            level.AddBlock(new BlockData(TestShapes.VerticalDomino.Cells, new Vector2Int(5, 3), Blue));
            level.AddBlock(new BlockData(TestShapes.Bar3.Cells, new Vector2Int(0, 5), Red));
            level.AddBlock(new BlockData(TestShapes.Single.Cells, new Vector2Int(0, 3), Blue));
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
