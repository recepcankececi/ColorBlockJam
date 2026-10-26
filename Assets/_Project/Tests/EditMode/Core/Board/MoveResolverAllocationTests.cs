using CBJ.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace CBJ.Tests.EditMode.Core
{
    /// <summary>
    /// <see cref="MoveResolver.TryStep"/> and <see cref="MoveResolver.Probe"/> run during drag handling, so they
    /// must not allocate. Each case runs once before measuring so JIT work is not counted.
    /// </summary>
    public class MoveResolverAllocationTests
    {
        private const int Red = 0;

        [Test]
        public void TryStep_MoveWithSubscriber_DoesNotAllocate()
        {
            (MoveResolver resolver, Block block) = CreateExitBoard();
            TestDelegate stepBackAndForth = () =>
            {
                resolver.TryStep(block, Direction.Up);
                resolver.TryStep(block, Direction.Down);
            };
            stepBackAndForth();

            Assert.That(stepBackAndForth, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void TryStep_BlockedAtEdgeWithoutDoor_DoesNotAllocate()
        {
            (MoveResolver resolver, Block block) = CreateExitBoard();
            TestDelegate stepIntoWall = () => resolver.TryStep(block, Direction.Down);
            stepIntoWall();

            Assert.That(stepIntoWall, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void Probe_ExitAvailable_DoesNotAllocate()
        {
            (MoveResolver resolver, Block block) = CreateExitBoard();
            TestDelegate probeExit = () => resolver.Probe(block, Direction.Left);
            probeExit();

            Assert.That(probeExit, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void TryStep_ExitWithSubscribers_DoesNotAllocate()
        {
            (MoveResolver warmResolver, Block warmBlock) = CreateExitBoard();
            warmResolver.TryStep(warmBlock, Direction.Left);
            (MoveResolver resolver, Block block) = CreateExitBoard();
            TestDelegate exit = () => resolver.TryStep(block, Direction.Left);

            Assert.That(exit, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void Probe_ArrowOffItsAxis_DoesNotAllocate()
        {
            var arrow = new Block(TestShapes.Single, Red, Vector2Int.one, BlockTraits.Arrow(Axis.Horizontal));
            var resolver = new MoveResolver(new BoardModel(3, 3, new Door[0], new[] { arrow }));
            TestDelegate probeUp = () => resolver.Probe(arrow, Direction.Up);
            probeUp();

            Assert.That(probeUp, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void TryStep_FrozenIce_DoesNotAllocate()
        {
            var ice = new Block(TestShapes.Single, Red, Vector2Int.one, BlockTraits.Ice(2));
            var resolver = new MoveResolver(new BoardModel(3, 3, new Door[0], new[] { ice }));
            TestDelegate step = () => resolver.TryStep(ice, Direction.Up);
            step();

            Assert.That(step, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void TryStep_ExitThawingIceWithSubscriber_DoesNotAllocate()
        {
            (MoveResolver warmResolver, Block warmBlock) = CreateExitBoardWithIce();
            warmResolver.TryStep(warmBlock, Direction.Left);
            (MoveResolver resolver, Block block) = CreateExitBoardWithIce();
            TestDelegate exit = () => resolver.TryStep(block, Direction.Left);

            Assert.That(exit, Is.Not.AllocatingGCMemory());
        }

        // The exit board plus a one-cell ice block in the far corner, listened to for thaw events.
        private static (MoveResolver, Block) CreateExitBoardWithIce()
        {
            var block = new Block(TestShapes.L, Red, Vector2Int.zero);
            var ice = new Block(TestShapes.Single, Red, new Vector2Int(2, 2), BlockTraits.Ice(2));
            var door = new Door(Direction.Left, 0, 2, Red);
            var board = new BoardModel(3, 3, new[] { door }, new[] { block, ice });
            board.BlockExited += IgnoreExit;
            board.IceLayerRemoved += IgnoreThaw;
            return (new MoveResolver(board), block);
        }

        private static void IgnoreThaw(Block block, int remaining)
        {
        }

        // L at (0,0) on a 3×3 board with a matching door on the left edge; every event has a subscriber.
        private static (MoveResolver, Block) CreateExitBoard()
        {
            var block = new Block(TestShapes.L, Red, Vector2Int.zero);
            var door = new Door(Direction.Left, 0, 2, Red);
            var board = new BoardModel(3, 3, new[] { door }, new[] { block });
            board.BlockMoved += IgnoreMove;
            board.BlockExited += IgnoreExit;
            board.Cleared += IgnoreCleared;
            return (new MoveResolver(board), block);
        }

        private static void IgnoreMove(Block block, Vector2Int from, Vector2Int to)
        {
        }

        private static void IgnoreExit(Block block, Door door)
        {
        }

        private static void IgnoreCleared()
        {
        }
    }
}
