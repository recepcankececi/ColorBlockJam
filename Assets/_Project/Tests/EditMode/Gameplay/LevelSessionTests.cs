using CBJ.Core;
using CBJ.Gameplay;
using CBJ.Tests.EditMode.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace CBJ.Tests.EditMode.Gameplay
{
    public class LevelSessionTests
    {
        private const int Red = 0;
        private const int TimerSeconds = 30;

        [Test]
        public void Begin_NewSession_PlaysWithTheFullTimer()
        {
            LevelSession session = Begin(OneStepLevel());

            Assert.That((session.State, session.SecondsLeft), Is.EqualTo((LevelState.Playing, (float)TimerSeconds)));
        }

        [Test]
        public void Begin_LevelWithoutBlocks_WinsImmediately()
        {
            var session = new LevelSession(new LevelData(3, 3, TimerSeconds));
            bool won = false;
            session.Won += () => won = true;

            session.Begin();

            Assert.That((won, session.State), Is.EqualTo((true, LevelState.Won)));
        }

        [Test]
        public void Tick_WhilePlaying_CountsDown()
        {
            LevelSession session = Begin(OneStepLevel());

            session.Tick(2.5f);

            Assert.That(session.SecondsLeft, Is.EqualTo(TimerSeconds - 2.5f).Within(1e-4f));
        }

        [Test]
        public void Tick_PastZero_FailsOnceAndStopsAtZero()
        {
            LevelSession session = Begin(OneStepLevel());
            int failures = 0;
            session.Failed += () => failures++;

            session.Tick(TimerSeconds + 1f);
            session.Tick(1f);

            Assert.That((failures, session.State, session.SecondsLeft), Is.EqualTo((1, LevelState.Failed, 0f)));
        }

        [Test]
        public void Tick_CrossingWholeSecond_RaisesDisplaySecondsOnce()
        {
            LevelSession session = Begin(OneStepLevel());
            int raised = 0;
            int shown = -1;
            session.DisplaySecondsChanged += seconds => { raised++; shown = seconds; };

            session.Tick(0.4f);
            session.Tick(0.4f);
            session.Tick(0.4f);

            Assert.That((raised, shown), Is.EqualTo((1, TimerSeconds - 1)));
        }

        [Test]
        public void Tick_WhilePaused_KeepsTheTime()
        {
            LevelSession session = Begin(OneStepLevel());
            session.Pause();

            session.Tick(5f);

            Assert.That(session.SecondsLeft, Is.EqualTo((float)TimerSeconds));
        }

        [Test]
        public void Resume_AfterPause_CountsDownAgain()
        {
            LevelSession session = Begin(OneStepLevel());
            session.Pause();

            session.Resume();
            session.Tick(1f);

            Assert.That(session.SecondsLeft, Is.EqualTo(TimerSeconds - 1f).Within(1e-4f));
        }

        [Test]
        public void TryStep_WhilePaused_DoesNotMove()
        {
            LevelSession session = Begin(OneStepLevel());
            Block block = session.Board.Blocks[0];
            session.Pause();

            StepResult result = session.TryStep(block, Direction.Up);

            Assert.That((result, block.Origin), Is.EqualTo((StepResult.Blocked, Vector2Int.zero)));
        }

        [Test]
        public void Probe_WhilePlaying_ReportsTheStepWithoutMoving()
        {
            LevelSession session = Begin(OneStepLevel());
            Block block = session.Board.Blocks[0];

            StepResult result = session.Probe(block, Direction.Down);

            Assert.That((result, block.IsExited), Is.EqualTo((StepResult.Exited, false)));
        }

        [Test]
        public void Probe_WhilePaused_ReportsBlocked()
        {
            LevelSession session = Begin(OneStepLevel());
            session.Pause();

            StepResult result = session.Probe(session.Board.Blocks[0], Direction.Up);

            Assert.That(result, Is.EqualTo(StepResult.Blocked));
        }

        [Test]
        public void TryStep_LastBlockExits_WinsAndStopsTheTimer()
        {
            LevelSession session = Begin(OneStepLevel());
            bool won = false;
            session.Won += () => won = true;

            session.TryStep(session.Board.Blocks[0], Direction.Down);
            session.Tick(TimerSeconds + 1f);

            Assert.That((won, session.State, session.SecondsLeft), Is.EqualTo((true, LevelState.Won, (float)TimerSeconds)));
        }

        [Test]
        public void TryStep_AfterFail_DoesNotMove()
        {
            LevelSession session = Begin(OneStepLevel());
            session.Tick(TimerSeconds);

            StepResult result = session.TryStep(session.Board.Blocks[0], Direction.Down);

            Assert.That(result, Is.EqualTo(StepResult.Blocked));
        }

        [Test]
        public void Begin_AgainAfterMoves_RestoresTheStartLayoutOnANewBoard()
        {
            LevelSession session = Begin(OneStepLevel());
            BoardModel first = session.Board;
            session.TryStep(first.Blocks[0], Direction.Up);

            session.Begin();

            Assert.That((session.Board == first, session.Board.Blocks[0].Origin), Is.EqualTo((false, Vector2Int.zero)));
        }

        [Test]
        public void Begin_AgainAfterFail_PlaysWithTheFullTimer()
        {
            LevelSession session = Begin(OneStepLevel());
            session.Tick(TimerSeconds);

            session.Begin();

            Assert.That((session.State, session.SecondsLeft), Is.EqualTo((LevelState.Playing, (float)TimerSeconds)));
        }

        [Test]
        public void Begin_AgainAfterWin_PutsTheBlocksBack()
        {
            LevelSession session = Begin(OneStepLevel());
            session.TryStep(session.Board.Blocks[0], Direction.Down);

            session.Begin();

            Assert.That((session.State, session.Board.RemainingBlocks), Is.EqualTo((LevelState.Playing, 1)));
        }

        [Test]
        public void Begin_Again_OldBoardNoLongerWinsTheSession()
        {
            LevelSession session = Begin(OneStepLevel());
            BoardModel old = session.Board;
            session.Begin();
            int wins = 0;
            session.Won += () => wins++;

            new MoveResolver(old).TryStep(old.Blocks[0], Direction.Down);

            Assert.That((wins, session.State), Is.EqualTo((0, LevelState.Playing)));
        }

        [Test]
        public void Tick_WhilePlaying_DoesNotAllocate()
        {
            LevelSession session = Begin(OneStepLevel());
            session.DisplaySecondsChanged += _ => { };
            TestDelegate tick = () => session.Tick(0.6f);
            tick();

            Assert.That(tick, Is.Not.AllocatingGCMemory());
        }

        // A red 1×1 block in the bottom-left corner of a 2×2 board, one step from a red door on the bottom edge.
        private static LevelData OneStepLevel()
        {
            var level = new LevelData(2, 2, TimerSeconds);
            level.AddDoor(new DoorData(Direction.Down, 0, 1, Red));
            level.AddBlock(new BlockData(TestShapes.Single.Cells, Vector2Int.zero, Red));
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
