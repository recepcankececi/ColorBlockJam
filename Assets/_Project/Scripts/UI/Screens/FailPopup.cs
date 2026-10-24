using System;
using UnityEngine;
using UnityEngine.UI;

namespace CBJ.UI
{
    /// <summary>
    /// Shown when time runs out: restart the level or go home.
    /// </summary>
    public sealed class FailPopup : ScreenView
    {
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _homeButton;

        /// <summary>Raised when the player restarts the level.</summary>
        public event Action RestartRequested;

        /// <summary>Raised when the player goes home.</summary>
        public event Action HomeRequested;

        private void OnEnable()
        {
            _restartButton.onClick.AddListener(OnRestart);
            _homeButton.onClick.AddListener(OnHome);
        }

        private void OnDisable()
        {
            _restartButton.onClick.RemoveListener(OnRestart);
            _homeButton.onClick.RemoveListener(OnHome);
        }

        private void OnRestart()
        {
            RestartRequested?.Invoke();
        }

        private void OnHome()
        {
            HomeRequested?.Invoke();
        }
    }
}
