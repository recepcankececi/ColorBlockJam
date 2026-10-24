using System;
using UnityEngine;
using UnityEngine.UI;

namespace CBJ.UI
{
    /// <summary>
    /// Pause: resume (also the close button), restart or go home.
    /// </summary>
    public sealed class PausePopup : ScreenView
    {
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _homeButton;

        /// <summary>Raised when the player resumes.</summary>
        public event Action ResumeRequested;

        /// <summary>Raised when the player restarts the level.</summary>
        public event Action RestartRequested;

        /// <summary>Raised when the player goes home.</summary>
        public event Action HomeRequested;

        private void OnEnable()
        {
            _resumeButton.onClick.AddListener(OnResume);
            _closeButton.onClick.AddListener(OnResume);
            _restartButton.onClick.AddListener(OnRestart);
            _homeButton.onClick.AddListener(OnHome);
        }

        private void OnDisable()
        {
            _resumeButton.onClick.RemoveListener(OnResume);
            _closeButton.onClick.RemoveListener(OnResume);
            _restartButton.onClick.RemoveListener(OnRestart);
            _homeButton.onClick.RemoveListener(OnHome);
        }

        private void OnResume()
        {
            ResumeRequested?.Invoke();
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
