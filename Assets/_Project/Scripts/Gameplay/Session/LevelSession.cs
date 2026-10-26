using System;
using CBJ.Core;
using UnityEngine;

namespace CBJ.Gameplay
{
    /// <summary>
    /// One level being played: the board of the current attempt, the countdown, the boosters and the win/fail
    /// outcome. Plain C# ticked by its owner, so the rules run in EditMode tests. Every attempt builds a fresh board
    /// from the level data and refills the boosters, so a restart carries nothing over.
    /// </summary>
    public sealed class LevelSession
    {
        private readonly LevelData _level;
        private readonly int _usesPerBooster;
        private readonly float _freezeSeconds;
        private MoveResolver _resolver;

        /// <summary>Creates a session for <paramref name="level"/> without boosters; call <see cref="Begin"/> to start.</summary>
        public LevelSession(LevelData level)
            : this(level, 0, 0f)
        {
        }

        /// <summary>
        /// Creates a session whose attempts each get <paramref name="usesPerBooster"/> hammer and freeze uses; a freeze
        /// stops the countdown for <paramref name="freezeSeconds"/>.
        /// </summary>
        public LevelSession(LevelData level, int usesPerBooster, float freezeSeconds)
        {
            _level = level ?? throw new ArgumentNullException(nameof(level));
            if (usesPerBooster < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(usesPerBooster), usesPerBooster, "Uses cannot be negative.");
            }

            if (freezeSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(freezeSeconds), freezeSeconds, "Freeze time cannot be negative.");
            }

            _usesPerBooster = usesPerBooster;
            _freezeSeconds = freezeSeconds;
        }

        /// <summary>Raised when the whole seconds shown on the countdown change, with the new value.</summary>
        public event Action<int> DisplaySecondsChanged;

        /// <summary>Raised once per attempt, when the last block has left the board.</summary>
        public event Action Won;

        /// <summary>Raised once per attempt, when the countdown reaches zero before the board is cleared.</summary>
        public event Action Failed;

        /// <summary>Raised when the countdown stops for a freeze (true) and when it runs again (false).</summary>
        public event Action<bool> TimerFrozenChanged;

        /// <summary>Raised when <see cref="HammerUses"/> or <see cref="FreezeUses"/> change, including at attempt start.</summary>
        public event Action BoostersChanged;

        /// <summary>Board of the current attempt; replaced by every <see cref="Begin"/>. Null before the first.</summary>
        public BoardModel Board { get; private set; }

        /// <summary>Phase of the current attempt.</summary>
        public LevelState State { get; private set; }

        /// <summary>Time left in the current attempt.</summary>
        public float SecondsLeft { get; private set; }

        /// <summary>Whole seconds shown on the countdown (rounded up, so it reads 0 only when time is out).</summary>
        public int DisplaySeconds => Mathf.CeilToInt(SecondsLeft);

        /// <summary>Seconds the countdown stays stopped by a freeze.</summary>
        public float FrozenSecondsLeft { get; private set; }

        /// <summary>True while a freeze stops the countdown.</summary>
        public bool IsTimerFrozen => FrozenSecondsLeft > 0f;

        /// <summary>Hammer uses left in this attempt.</summary>
        public int HammerUses { get; private set; }

        /// <summary>Freeze uses left in this attempt.</summary>
        public int FreezeUses { get; private set; }

        /// <summary>
        /// Starts a fresh attempt, throwing away the current one (this is also the restart): new board, full timer,
        /// full boosters, no freeze, playing. A level without blocks is won immediately.
        /// </summary>
        public void Begin()
        {
            if (Board != null)
            {
                Board.Cleared -= OnBoardCleared;
            }

            Board = _level.BuildBoard();
            Board.Cleared += OnBoardCleared;
            _resolver = new MoveResolver(Board);
            SecondsLeft = _level.TimerSeconds;
            HammerUses = _usesPerBooster;
            FreezeUses = _usesPerBooster;
            State = LevelState.Playing;
            EndFreeze();
            DisplaySecondsChanged?.Invoke(DisplaySeconds);
            BoostersChanged?.Invoke();

            if (Board.IsCleared)
            {
                OnBoardCleared();
            }
        }

        /// <summary>
        /// Advances the countdown while playing; fails the attempt when it reaches zero. A running freeze uses up the
        /// time first and the countdown only gets what is left over.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (State != LevelState.Playing || deltaTime <= 0f)
            {
                return;
            }

            if (IsTimerFrozen)
            {
                float frozen = Mathf.Min(FrozenSecondsLeft, deltaTime);
                FrozenSecondsLeft -= frozen;
                deltaTime -= frozen;
                if (!IsTimerFrozen)
                {
                    TimerFrozenChanged?.Invoke(false);
                }

                if (deltaTime <= 0f)
                {
                    return;
                }
            }

            int shown = DisplaySeconds;
            SecondsLeft = Mathf.Max(0f, SecondsLeft - deltaTime);
            if (DisplaySeconds != shown)
            {
                DisplaySecondsChanged?.Invoke(DisplaySeconds);
            }

            if (SecondsLeft <= 0f)
            {
                State = LevelState.Failed;
                Failed?.Invoke();
            }
        }

        /// <summary>Ends a running freeze at once, raising <see cref="TimerFrozenChanged"/>; nothing when not frozen.</summary>
        public void EndFreeze()
        {
            if (IsTimerFrozen)
            {
                FrozenSecondsLeft = 0f;
                TimerFrozenChanged?.Invoke(false);
            }
        }

        /// <summary>Freezes the countdown and input until <see cref="Resume"/>.</summary>
        public void Pause()
        {
            if (State == LevelState.Playing)
            {
                State = LevelState.Paused;
            }
        }

        /// <summary>Continues a paused attempt.</summary>
        public void Resume()
        {
            if (State == LevelState.Paused)
            {
                State = LevelState.Playing;
            }
        }

        /// <summary>
        /// Stops the countdown for <paramref name="seconds"/> more while playing; a running freeze is extended.
        /// Returns whether the freeze started or grew.
        /// </summary>
        public bool FreezeFor(float seconds)
        {
            if (State != LevelState.Playing || seconds <= 0f)
            {
                return false;
            }

            bool wasFrozen = IsTimerFrozen;
            FrozenSecondsLeft += seconds;
            if (!wasFrozen)
            {
                TimerFrozenChanged?.Invoke(true);
            }

            return true;
        }

        /// <summary>Spends a freeze use on <see cref="FreezeFor"/>; false when none is left or the attempt is not playing.</summary>
        public bool UseFreeze()
        {
            if (FreezeUses <= 0 || !FreezeFor(_freezeSeconds))
            {
                return false;
            }

            FreezeUses--;
            BoostersChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Spends a hammer use destroying <paramref name="block"/> (<see cref="BoardModel.Destroy"/>); false when none
        /// is left, the attempt is not playing or the block is not on the current board.
        /// </summary>
        public bool UseHammer(Block block)
        {
            if (State != LevelState.Playing || HammerUses <= 0 || block == null || block.IsExited ||
                Board.GetBlockAt(block.Origin + block.Shape.Cells[0]) != block)
            {
                return false;
            }

            // The use is spent before the board changes: destroying the last block wins the attempt right away.
            HammerUses--;
            Board.Destroy(block);
            BoostersChanged?.Invoke();
            return true;
        }

        /// <summary>Steps a block one cell while playing; otherwise nothing moves and the step reports blocked.</summary>
        public StepResult TryStep(Block block, Direction direction)
        {
            return State == LevelState.Playing ? _resolver.TryStep(block, direction) : StepResult.Blocked;
        }

        /// <summary>What <see cref="TryStep"/> would do now, without moving anything.</summary>
        public StepResult Probe(Block block, Direction direction)
        {
            return State == LevelState.Playing ? _resolver.Probe(block, direction) : StepResult.Blocked;
        }

        // A freeze cannot outlive the attempt, or the HUD would carry its tint into the next level.
        private void OnBoardCleared()
        {
            EndFreeze();
            State = LevelState.Won;
            Won?.Invoke();
        }
    }
}
