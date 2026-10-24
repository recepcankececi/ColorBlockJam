using System;
using CBJ.Core;
using UnityEngine;

namespace CBJ.Gameplay
{
    /// <summary>
    /// Runs a <see cref="LevelSession"/> in the scene: ticks its countdown, shows each attempt's board, frames the
    /// camera and points drag input at the board. The game flow and UI talk to gameplay through this component.
    /// </summary>
    public sealed class LevelPlayer : MonoBehaviour
    {
        private BoardView _boardView;
        private BoardCamera _boardCamera;
        private DragInput _dragInput;
        private LevelSession _session;
        private bool _subscribed;

        /// <summary>Raised whenever an attempt starts: a new level or a restart.</summary>
        public event Action AttemptStarted;

        /// <summary>Raised when the whole seconds left on the countdown change, including at attempt start.</summary>
        public event Action<int> DisplaySecondsChanged;

        /// <summary>Raised when the current attempt is won.</summary>
        public event Action Won;

        /// <summary>Raised when the current attempt runs out of time.</summary>
        public event Action Failed;

        /// <summary>Sets the scene collaborators; call once before <see cref="Play"/>.</summary>
        public void Initialize(BoardView boardView, BoardCamera boardCamera, DragInput dragInput)
        {
            _boardView = boardView;
            _boardCamera = boardCamera;
            _dragInput = dragInput;
        }

        /// <summary>Starts the first attempt of <paramref name="level"/>, replacing any level in progress.</summary>
        public void Play(LevelData level)
        {
            Unsubscribe();
            _session = new LevelSession(level);
            Subscribe();
            StartAttempt();
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
            _session?.Pause();
        }

        /// <summary>Continues after <see cref="Pause"/>.</summary>
        public void Resume()
        {
            _session?.Resume();
        }

        private void StartAttempt()
        {
            AttemptStarted?.Invoke();
            _session.Begin();
            _boardView.Show(_session.Board);
            _boardCamera.Frame(_boardView.WorldBounds);
            _dragInput.Bind(_session, _boardView);
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
            _subscribed = false;
        }

        private void OnWon()
        {
            Won?.Invoke();
        }

        private void OnFailed()
        {
            Failed?.Invoke();
        }

        private void OnDisplaySecondsChanged(int seconds)
        {
            DisplaySecondsChanged?.Invoke(seconds);
        }
    }
}
