using System.Collections.Generic;
using CBJ.Core;
using CBJ.Gameplay;
using CBJ.Tests.EditMode.Core;
using NUnit.Framework;
using UnityEngine;

namespace CBJ.Tests.EditMode.Gameplay
{
    public class LevelSessionBoosterTests
    {
        private const int Red = 0;
        private const int TimerSeconds = 30;
        private const float FreezeSeconds = 10f;

        [Test]
        public void FreezeFor_ThenTickWithinFreeze_KeepsTheCountdown()
        {
            LevelSession session = Begin();

            session.FreezeFor(FreezeSeconds);
            session.Tick(4f);

            Assert.That((session.SecondsLeft, session.FrozenSecondsLeft), Is.EqualTo(((float)TimerSeconds, 6f)));
        }

        [Test]
        public void FreezeFor_ThenTickPastFreeze_CountsDownOnlyTheRest()
        {
            LevelSession session = Begin();

            session.FreezeFor(FreezeSeconds);
            session.Tick(FreezeSeconds + 2f);

            Assert.That((session.SecondsLeft, session.IsTimerFrozen), Is.EqualTo((TimerSeconds - 2f, false)));
        }

        [Test]
        public void FreezeFor_StartAndRunOut_RaisesTimerFrozenChangedTwice()
        {
            LevelSession session = Begin();
            var changes = new List<bool>();
            session.TimerFrozenChanged += changes.Add;

            session.FreezeFor(FreezeSeconds);
            session.Tick(FreezeSeconds);

            Assert.That(changes, Is.EqualTo(new[] { true, false }));
        }

        [Test]
        public void FreezeFor_WhileFrozen_ExtendsTheFreeze()
        {
            LevelSession session = Begin();

            session.FreezeFor(FreezeSeconds);
            session.FreezeFor(FreezeSeconds);

            Assert.That(session.FrozenSecondsLeft, Is.EqualTo(FreezeSeconds * 2f));
        }

        [Test]
        public void FreezeFor_WhilePaused_DoesNothing()
        {
            LevelSession session = Begin();
            session.Pause();

            bool frozen = session.FreezeFor(FreezeSeconds);

            Assert.That((frozen, session.IsTimerFrozen), Is.EqualTo((false, false)));
        }

        [Test]
        public void Tick_WhilePausedAndFrozen_KeepsTheFreeze()
        {
            LevelSession session = Begin();
            session.FreezeFor(FreezeSeconds);
            session.Pause();

            session.Tick(4f);

            Assert.That(session.FrozenSecondsLeft, Is.EqualTo(FreezeSeconds));
        }

        [Test]
        public void Begin_WhileFrozen_EndsTheFreeze()
        {
            LevelSession session = Begin();
            session.FreezeFor(FreezeSeconds);

            session.Begin();

            Assert.That(session.IsTimerFrozen, Is.False);
        }

        [Test]
        public void UseFreeze_WithAUseLeft_FreezesForTheConfiguredTime()
        {
            LevelSession session = Begin();

            session.UseFreeze();

            Assert.That((session.FrozenSecondsLeft, session.FreezeUses), Is.EqualTo((FreezeSeconds, 0)));
        }

        [Test]
        public void UseFreeze_ThenLastBlockLeaves_EndsTheFreezeBeforeWinning()
        {
            var level = new LevelData(2, 2, TimerSeconds);
            level.AddBlock(new BlockData(TestShapes.Single.Cells, Vector2Int.zero, Red));
            var session = new LevelSession(level, 1, FreezeSeconds);
            session.Begin();
            session.UseFreeze();
            bool frozenAtWin = true;
            session.Won += () => frozenAtWin = session.IsTimerFrozen;
            var changes = new List<bool>();
            session.TimerFrozenChanged += changes.Add;

            session.UseHammer(session.Board.Blocks[0]);

            Assert.That((frozenAtWin, string.Join(",", changes)), Is.EqualTo((false, "False")));
        }

        [Test]
        public void EndFreeze_WhileFrozen_RaisesTimerFrozenChangedFalse()
        {
            LevelSession session = Begin();
            session.FreezeFor(FreezeSeconds);
            var changes = new List<bool>();
            session.TimerFrozenChanged += changes.Add;

            session.EndFreeze();

            Assert.That(changes, Is.EqualTo(new[] { false }));
        }

        [Test]
        public void UseFreeze_NoUseLeft_ReturnsFalse()
        {
            LevelSession session = Begin();
            session.UseFreeze();

            Assert.That(session.UseFreeze(), Is.False);
        }

        [Test]
        public void UseHammer_BlockOnBoard_DestroysItAndSpendsTheUse()
        {
            LevelSession session = Begin();
            Block block = session.Board.Blocks[0];

            bool used = session.UseHammer(block);

            Assert.That((used, block.IsExited, session.HammerUses), Is.EqualTo((true, true, 0)));
        }

        [Test]
        public void UseHammer_NoUseLeft_LeavesTheBlock()
        {
            LevelSession session = Begin();
            session.UseHammer(session.Board.Blocks[0]);
            Block second = session.Board.Blocks[1];

            Assert.That((session.UseHammer(second), second.IsExited), Is.EqualTo((false, false)));
        }

        [Test]
        public void UseHammer_WhilePaused_ReturnsFalse()
        {
            LevelSession session = Begin();
            session.Pause();

            Assert.That(session.UseHammer(session.Board.Blocks[0]), Is.False);
        }

        [Test]
        public void UseHammer_LastBlock_WinsTheAttempt()
        {
            var level = new LevelData(2, 2, TimerSeconds);
            level.AddBlock(new BlockData(TestShapes.Single.Cells, Vector2Int.zero, Red));
            var session = new LevelSession(level, 1, FreezeSeconds);
            session.Begin();

            session.UseHammer(session.Board.Blocks[0]);

            Assert.That(session.State, Is.EqualTo(LevelState.Won));
        }

        [Test]
        public void UseHammer_BlockFromAnEarlierAttempt_ReturnsFalse()
        {
            LevelSession session = Begin();
            Block old = session.Board.Blocks[0];
            session.Begin();

            Assert.That(session.UseHammer(old), Is.False);
        }

        [Test]
        public void Begin_AfterUsingBoosters_RefillsThem()
        {
            LevelSession session = Begin();
            session.UseFreeze();
            session.UseHammer(session.Board.Blocks[0]);

            session.Begin();

            Assert.That((session.HammerUses, session.FreezeUses), Is.EqualTo((1, 1)));
        }

        [Test]
        public void Begin_SessionWithoutBoosters_HasNoUses()
        {
            var session = new LevelSession(TwoBlockLevel());
            session.Begin();

            Assert.That((session.HammerUses, session.FreezeUses), Is.EqualTo((0, 0)));
        }

        // Two red single blocks on a 3×3 board, no doors; one use of each booster per attempt.
        private static LevelSession Begin()
        {
            var session = new LevelSession(TwoBlockLevel(), 1, FreezeSeconds);
            session.Begin();
            return session;
        }

        private static LevelData TwoBlockLevel()
        {
            var level = new LevelData(3, 3, TimerSeconds);
            level.AddBlock(new BlockData(TestShapes.Single.Cells, Vector2Int.zero, Red));
            level.AddBlock(new BlockData(TestShapes.Single.Cells, new Vector2Int(2, 2), Red));
            return level;
        }
    }
}
