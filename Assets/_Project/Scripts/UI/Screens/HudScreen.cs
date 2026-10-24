using System;
using CBJ.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace CBJ.UI
{
    /// <summary>
    /// Gameplay HUD: level number, countdown and pause button. The booster bar is a placeholder with press feedback.
    /// </summary>
    public sealed class HudScreen : ScreenView
    {
        [SerializeField] private LevelPlayer _levelPlayer;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Text _levelLabel;
        [SerializeField] private Text _timerLabel;

        /// <summary>Raised when the player taps pause.</summary>
        public event Action PauseRequested;

        /// <summary>Shows the level being played (1-based).</summary>
        public void SetLevelNumber(int levelNumber)
        {
            _levelLabel.text = $"Level {levelNumber}";
        }

        private void OnEnable()
        {
            _pauseButton.onClick.AddListener(OnPause);
            _levelPlayer.DisplaySecondsChanged += OnDisplaySecondsChanged;
        }

        private void OnDisable()
        {
            _pauseButton.onClick.RemoveListener(OnPause);
            _levelPlayer.DisplaySecondsChanged -= OnDisplaySecondsChanged;
        }

        private void OnPause()
        {
            PauseRequested?.Invoke();
        }

        private void OnDisplaySecondsChanged(int seconds)
        {
            _timerLabel.text = TimerFormat.ToClock(seconds);
        }
    }
}
