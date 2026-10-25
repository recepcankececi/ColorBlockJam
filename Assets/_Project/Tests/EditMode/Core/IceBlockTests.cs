using System.Collections.Generic;
using CBJ.Core;
using NUnit.Framework;
using UnityEngine;

namespace CBJ.Tests.EditMode.Core
{
    public class IceBlockTests
    {
        private const int Red = 0;
        private const int Blue = 1;

        [Test]
        public void Probe_IceWithLayers_IsBlocked()
        {
            Setup setup = Create(1, 1);

            Assert.That(setup.Resolver.Probe(setup.Ice, Direction.Up), Is.EqualTo(StepResult.Blocked));
        }

        [Test]
        public void TryStep_IceWithLayersAtItsDoor_IsBlocked()
        {
            Setup setup = Create(1, 1);

            Assert.That(setup.Resolver.TryStep(setup.Ice, Direction.Left), Is.EqualTo(StepResult.Blocked));
        }

        [Test]
        public void TryStep_OtherBlockExits_IceLosesOneLayer()
        {
            Setup setup = Create(2, 1);

            setup.Resolver.TryStep(setup.Others[0], Direction.Right);

            Assert.That(setup.Ice.IceLayers, Is.EqualTo(1));
        }

        [Test]
        public void TryStep_OtherBlockExits_RaisesIceLayerRemovedWithLayersLeft()
        {
            Setup setup = Create(2, 1);
            var raised = new List<(Block, int)>();
            setup.Board.IceLayerRemoved += (block, remaining) => raised.Add((block, remaining));

            setup.Resolver.TryStep(setup.Others[0], Direction.Right);

            Assert.That(raised, Is.EqualTo(new[] { (setup.Ice, 1) }));
        }

        [Test]
        public void TryStep_IceThawedToZero_CanExit()
        {
            Setup setup = Create(1, 1);
            setup.Resolver.TryStep(setup.Others[0], Direction.Right);

            Assert.That(setup.Resolver.TryStep(setup.Ice, Direction.Left), Is.EqualTo(StepResult.Exited));
        }

        [Test]
        public void TryStep_IceAlreadyThawed_RaisesNoMoreLayerEvents()
        {
            Setup setup = Create(1, 2);
            setup.Resolver.TryStep(setup.Others[0], Direction.Right);
            int raised = 0;
            setup.Board.IceLayerRemoved += (block, remaining) => raised++;

            setup.Resolver.TryStep(setup.Others[1], Direction.Right);

            Assert.That((raised, setup.Ice.IceLayers), Is.EqualTo((0, 0)));
        }

        [Test]
        public void Destroy_OtherBlock_IceLosesOneLayer()
        {
            Setup setup = Create(2, 1);

            setup.Board.Destroy(setup.Others[0]);

            Assert.That(setup.Ice.IceLayers, Is.EqualTo(1));
        }

        [Test]
        public void Destroy_IceBlockItself_RaisesNoLayerEvent()
        {
            Setup setup = Create(2, 1);
            int raised = 0;
            setup.Board.IceLayerRemoved += (block, remaining) => raised++;

            setup.Board.Destroy(setup.Ice);

            Assert.That(raised, Is.Zero);
        }

        private readonly struct Setup
        {
            public Setup(BoardModel board, Block ice, Block[] others)
            {
                Board = board;
                Resolver = new MoveResolver(board);
                Ice = ice;
                Others = others;
            }

            public BoardModel Board { get; }

            public MoveResolver Resolver { get; }

            public Block Ice { get; }

            public Block[] Others { get; }
        }

        // 3-wide board, one row per block: the red ice block at (0,0) next to a red left door; each blue block at the
        // right edge of its own row, next to a blue right door, so one step right takes it out.
        private static Setup Create(int iceLayers, int otherBlocks)
        {
            int height = otherBlocks + 1;
            var ice = new Block(TestShapes.Single, Red, Vector2Int.zero, BlockTraits.Ice(iceLayers));
            var blocks = new List<Block> { ice };
            var others = new Block[otherBlocks];
            for (int i = 0; i < otherBlocks; i++)
            {
                others[i] = new Block(TestShapes.Single, Blue, new Vector2Int(2, i + 1));
                blocks.Add(others[i]);
            }

            var doors = new[] { new Door(Direction.Left, 0, 1, Red), new Door(Direction.Right, 1, otherBlocks, Blue) };
            return new Setup(new BoardModel(3, height, doors, blocks), ice, others);
        }
    }
}
