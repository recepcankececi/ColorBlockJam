using System;
using CBJ.Core;
using CBJ.Data;
using UnityEngine;

namespace CBJ.Gameplay
{
    /// <summary>
    /// Runs a <see cref="LevelSession"/> in the scene: ticks its countdown, shows each attempt's board, frames the
    /// camera and points drag input at the board. The game flow and UI talk to gameplay through this component,
    /// boosters included: the hammer waits for the player to tap a block, the freeze applies at once.
    /// </summary>
    public sealed class LevelPlayer : MonoBehaviour
    {
        private BoardView _boardView;
        private BoardCamera _boardCamera;
        private DragInput _dragInput;
        private GameConfig _config;
        private LevelSession _session;
        private Action<Block> _onHammerTarget;
        private bool _subscribed;
        private bool _wasPlaying;

        /// <summary>Raised whenever an attempt starts: a new level or a restart.</summary>
        public event Action AttemptStarted;

        /// <summary>Raised when the whole seconds left on the countdown change, including at attempt start.</summary>
        public event Action<int> DisplaySecondsChanged;

        /// <summary>Raised when the current attempt is won.</summary>
        public event Action Won;

        /// <summary>Raised when the current attempt runs out of time.</summary>
        public event Action Failed;

        /// <summary>Raised when <see cref="IsPlaying"/> changes: pause, resume, win, fail, stop or a new attempt.</summary>
        public event Action<bool> RunningChanged;

        /// <summary>Raised when a freeze stops the countdown (true) and when it runs again (false).</summary>
        public event Action<bool> TimerFrozenChanged;

        /// <summary>Raised when the booster uses left change, including at attempt start.</summary>
        public event Action BoostersChanged;

        /// <summary>Raised when <see cref="IsAimingHammer"/> changes.</summary>
        public event Action<bool> HammerAimChanged;

        /// <summary>Sets the scene collaborators; call once before <see cref="Play"/>.</summary>
        public void Initialize(BoardView boardView, BoardCamera boardCamera, DragInput dragInput, GameConfig config)
        {
            _boardView = boardView;
            _boardCamera = boardCamera;
            _dragInput = dragInput;
            _config = config;
            _onHammerTarget = OnHammerTarget;
        }

        /// <summary>Starts the first attempt of <paramref name="level"/>, replacing any level in progress.</summary>
        public void Play(LevelData level)
        {
            _session?.EndFreeze();
            Unsubscribe();
            _session = new LevelSession(level, _config.BoosterUsesPerAttempt, _config.FreezeSeconds);
            Subscribe();
            StartAttempt();
        }

        /// <summary>True while the current attempt is running (not paused, won, failed or stopped).</summary>
        public bool IsPlaying => _session != null && _session.State == LevelState.Playing;

        /// <summary>True while a freeze stops the countdown.</summary>
        public bool IsTimerFrozen => _session != null && _session.IsTimerFrozen;

        /// <summary>Hammer uses left in the current attempt.</summary>
        public int HammerUses => _session != null ? _session.HammerUses : 0;

        /// <summary>Freeze uses left in the current attempt.</summary>
        public int FreezeUses => _session != null ? _session.FreezeUses : 0;

        /// <summary>True while the next tap on the board picks the block the hammer destroys.</summary>
        public bool IsAimingHammer { get; private set; }

        /// <summary>
        /// Waits for the player to tap a block to destroy; a tap on an empty cell, a pause or the end of the attempt
        /// cancels. Does nothing unless playing with a hammer use left.
        /// </summary>
        public void AimHammer()
        {
            if (IsPlaying && _session.HammerUses > 0 && !IsAimingHammer)
            {
                _dragInput.PickNext(_onHammerTarget);
                SetHammerAim(true);
            }
        }

        /// <summary>Stops waiting for a hammer target.</summary>
        public void CancelHammer()
        {
            if (IsAimingHammer)
            {
                _dragInput.CancelPick();
                SetHammerAim(false);
            }
        }

        /// <summary>Spends a freeze use; false when none is left or the attempt is not playing.</summary>
        public bool UseFreeze()
        {
            return _session != null && _session.UseFreeze();
        }

        /// <summary>Ends the level in progress without a result and clears the board.</summary>
        public void Stop()
        {
            CancelHammer();
            _session?.EndFreeze();
            Unsubscribe();
            _session = null;
            _dragInput.Unbind();
            _boardView.Clear();
            NotifyRunning();
        }

        /// <summary>Starts the current level again from its saved start state.</summary>
        public void Restart()
        {
            if (_session != null)
            {
                StartAttempt();
            }
        }

        /// <summary>Freezes the countdown and input.</summary>
        public void Pause()
        {
            CancelHammer();
            _session?.Pause();
            NotifyRunning();
        }

        /// <summary>Continues after <see cref="Pause"/>.</summary>
        public void Resume()
        {
            _session?.Resume();
            NotifyRunning();
        }

        private void StartAttempt()
        {
            CancelHammer();
            AttemptStarted?.Invoke();
            _session.Begin();
            _boardView.Show(_session.Board);
            _boardCamera.Frame(_boardView.WorldBounds);
            _dragInput.Bind(_session, _boardView);
            NotifyRunning();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            _session?.Tick(Time.deltaTime);
        }

        private void Subscribe()
        {
            if (_session == null || _subscribed || !isActiveAndEnabled)
            {
                return;
            }

            _session.Won += OnWon;
            _session.Failed += OnFailed;
            _session.DisplaySecondsChanged += OnDisplaySecondsChanged;
            _session.TimerFrozenChanged += OnTimerFrozenChanged;
            _session.BoostersChanged += OnBoostersChanged;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (_session == null || !_subscribed)
            {
                return;
            }

            _session.Won -= OnWon;
            _session.Failed -= OnFailed;
            _session.DisplaySecondsChanged -= OnDisplaySecondsChanged;
            _session.TimerFrozenChanged -= OnTimerFrozenChanged;
            _session.BoostersChanged -= OnBoostersChanged;
            _subscribed = false;
        }

        private void OnWon()
        {
            CancelHammer();
            NotifyRunning();
            Won?.Invoke();
        }

        private void OnFailed()
        {
            CancelHammer();
            NotifyRunning();
            Failed?.Invoke();
        }

        private void NotifyRunning()
        {
            bool playing = IsPlaying;
            if (playing != _wasPlaying)
            {
                _wasPlaying = playing;
                RunningChanged?.Invoke(playing);
            }
        }

        private void OnDisplaySecondsChanged(int seconds)
        {
            DisplaySecondsChanged?.Invoke(seconds);
        }

        private void OnTimerFrozenChanged(bool frozen)
        {
            TimerFrozenChanged?.Invoke(frozen);
        }

        private void OnBoostersChanged()
        {
            BoostersChanged?.Invoke();
        }

        // A tap on an empty cell or off the board passes null: the hammer is put away unused.
        private void OnHammerTarget(Block block)
        {
            SetHammerAim(false);
            if (block != null)
            {
                _session.UseHammer(block);
            }
        }

        private void SetHammerAim(bool aiming)
        {
            IsAimingHammer = aiming;
            HammerAimChanged?.Invoke(aiming);
        }
    }
}
