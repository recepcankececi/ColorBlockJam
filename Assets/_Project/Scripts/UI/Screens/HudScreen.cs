using System;
using CBJ.Data;
using CBJ.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace CBJ.UI
{
    /// <summary>
    /// Gameplay HUD: level badge, countdown with its low-time warning, restart and pause buttons and the coin count.
    /// The booster bar is a placeholder with press feedback.
    /// </summary>
    public sealed class HudScreen : ScreenView
    {
        [SerializeField] private LevelPlayer _levelPlayer;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Text _levelLabel;
        [SerializeField] private Text _timerLabel;
        [SerializeField] private Text _coinsLabel;
        [SerializeField] private TimerWarning _timerWarning;

        /// <summary>Raised when the player taps pause.</summary>
        public event Action PauseRequested;

        /// <summary>Raised when the player taps restart.</summary>
        public event Action RestartRequested;

        /// <summary>Shows the level being played, by catalog position.</summary>
        public void SetLevel(int catalogIndex)
        {
            _levelLabel.text = LevelLabel.For(catalogIndex);
        }

        /// <summary>Shows the player's coin count.</summary>
        public void SetCoins(int coins)
        {
            _coinsLabel.text = coins.ToString();
        }

        private void OnEnable()
        {
            _pauseButton.onClick.AddListener(OnPause);
            _restartButton.onClick.AddListener(OnRestart);
            _levelPlayer.DisplaySecondsChanged += OnDisplaySecondsChanged;
            _levelPlayer.RunningChanged += _timerWarning.SetRunning;
            _timerWarning.SetRunning(_levelPlayer.IsPlaying);
        }

        private void OnDisable()
        {
            _pauseButton.onClick.RemoveListener(OnPause);
            _restartButton.onClick.RemoveListener(OnRestart);
            _levelPlayer.DisplaySecondsChanged -= OnDisplaySecondsChanged;
            _levelPlayer.RunningChanged -= _timerWarning.SetRunning;
        }

        private void OnPause()
        {
            PauseRequested?.Invoke();
        }

        private void OnRestart()
        {
            RestartRequested?.Invoke();
        }

        private void OnDisplaySecondsChanged(int seconds)
        {
            _timerLabel.text = TimerFormat.ToClock(seconds);
            _timerWarning.SetSeconds(seconds);
        }
    }
}
