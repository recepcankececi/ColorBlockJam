using System;
using UnityEngine;
using UnityEngine.UI;

namespace CBJ.UI
{
    /// <summary>
    /// Home: level button, settings button, top bar and tab bar. Tabs, profile, coins and lives are placeholders
    /// that only give press feedback.
    /// </summary>
    public sealed class HomeScreen : ScreenView
    {
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Text _levelLabel;

        /// <summary>Raised when the player taps the level button.</summary>
        public event Action PlayRequested;

        /// <summary>Raised when the player taps the settings button.</summary>
        public event Action SettingsRequested;

        /// <summary>Shows the level the level button will start (1-based).</summary>
        public void SetLevelNumber(int levelNumber)
        {
            _levelLabel.text = $"Level {levelNumber}";
        }

        private void OnEnable()
        {
            _playButton.onClick.AddListener(OnPlay);
            _settingsButton.onClick.AddListener(OnSettings);
        }

        private void OnDisable()
        {
            _playButton.onClick.RemoveListener(OnPlay);
            _settingsButton.onClick.RemoveListener(OnSettings);
        }

        private void OnPlay()
        {
            PlayRequested?.Invoke();
        }

        private void OnSettings()
        {
            SettingsRequested?.Invoke();
        }
    }
}
