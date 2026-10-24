using System;
using CBJ.Core;
using UnityEngine;

namespace CBJ.Gameplay
{
    /// <summary>
    /// One level being played: the board of the current attempt, the countdown and the win/fail outcome. Plain C#
    /// ticked by its owner, so the rules run in EditMode tests. Every attempt builds a fresh board from the level
    /// data, so a restart carries nothing over.
    /// </summary>
    public sealed class LevelSession
    {
        private readonly LevelData _level;
        private MoveResolver _resolver;

        /// <summary>Creates a session for <paramref name="level"/>; call <see cref="Begin"/> to start the first attempt.</summary>
        public LevelSession(LevelData level)
        {
            _level = level ?? throw new ArgumentNullException(nameof(level));
        }

        /// <summary>Raised when the whole seconds shown on the countdown change, with the new value.</summary>
        public event Action<int> DisplaySecondsChanged;

        /// <summary>Raised once per attempt, when the last block has left the board.</summary>
        public event Action Won;

        /// <summary>Raised once per attempt, when the countdown reaches zero before the board is cleared.</summary>
        public event Action Failed;

        /// <summary>Board of the current attempt; replaced by every <see cref="Begin"/>. Null before the first.</summary>
        public BoardModel Board { get; private set; }

        /// <summary>Phase of the current attempt.</summary>
        public LevelState State { get; private set; }

        /// <summary>Time left in the current attempt.</summary>
        public float SecondsLeft { get; private set; }

        /// <summary>Whole seconds shown on the countdown (rounded up, so it reads 0 only when time is out).</summary>
        public int DisplaySeconds => Mathf.CeilToInt(SecondsLeft);

        /// <summary>
        /// Starts a fresh attempt, throwing away the current one (this is also the restart): new board, full timer,
        /// playing. A level without blocks is won immediately.
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
            State = LevelState.Playing;
            DisplaySecondsChanged?.Invoke(DisplaySeconds);

            if (Board.IsCleared)
            {
                OnBoardCleared();
            }
        }

        /// <summary>Advances the countdown while playing; fails the attempt when it reaches zero.</summary>
        public void Tick(float deltaTime)
        {
            if (State != LevelState.Playing || deltaTime <= 0f)
            {
                return;
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

        /// <summary>Steps a block one cell while playing; otherwise nothing moves and the step reports blocked.</summary>
        public StepResult TryStep(Block block, Direction direction)
        {
            return State == LevelState.Playing ? _resolver.TryStep(block, direction) : StepResult.Blocked;
        }

        private void OnBoardCleared()
        {
            State = LevelState.Won;
            Won?.Invoke();
        }
    }
}
