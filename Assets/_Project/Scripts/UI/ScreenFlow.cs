using System;
using CBJ.Gameplay;
using UnityEngine;

namespace CBJ.UI
{
    /// <summary>
    /// Navigation between the screens and popups, and the UI's side of gameplay: pause, resume, restart and the fail
    /// popup. Choosing which level to play stays with the game flow, which listens to <see cref="PlayRequested"/>
    /// and <see cref="HomeRequested"/>.
    /// </summary>
    public sealed class ScreenFlow : MonoBehaviour
    {
        [SerializeField] private LevelPlayer _levelPlayer;
        [SerializeField] private HomeScreen _home;
        [SerializeField] private HudScreen _hud;
        [SerializeField] private SettingsPopup _settings;
        [SerializeField] private PausePopup _pause;
        [SerializeField] private FailPopup _fail;

        private readonly UIRouter _router = new UIRouter();

        /// <summary>Raised when the player asks to play from home.</summary>
        public event Action PlayRequested;

        /// <summary>Raised when the player leaves a level for home; the game flow stops the level and calls <see cref="ShowHome"/>.</summary>
        public event Action HomeRequested;

        /// <summary>Hands the settings to the settings popup; call once before showing any screen.</summary>
        public void Initialize(GameSettings settings)
        {
            _settings.Bind(settings);
        }

        /// <summary>Shows home with <paramref name="levelNumber"/> (1-based) on the level button.</summary>
        public void ShowHome(int levelNumber)
        {
            _home.SetLevelNumber(levelNumber);
            _router.ShowScreen(_home);
        }

        /// <summary>Shows the HUD for <paramref name="levelNumber"/> (1-based); call before the level starts.</summary>
        public void ShowGameplay(int levelNumber)
        {
            _hud.SetLevelNumber(levelNumber);
            _router.ShowScreen(_hud);
        }

        private void Awake()
        {
            _home.Hide();
            _hud.Hide();
            _settings.Hide();
            _pause.Hide();
            _fail.Hide();
        }

        private void OnEnable()
        {
            _home.PlayRequested += OnPlayRequested;
            _home.SettingsRequested += OnSettingsRequested;
            _settings.CloseRequested += OnPopupCloseRequested;
            _hud.PauseRequested += OnPauseRequested;
            _pause.ResumeRequested += OnResumeRequested;
            _pause.RestartRequested += OnRestartRequested;
            _pause.HomeRequested += OnHomeRequested;
            _fail.RestartRequested += OnRestartRequested;
            _fail.HomeRequested += OnHomeRequested;
            _levelPlayer.AttemptStarted += OnAttemptStarted;
            _levelPlayer.Failed += OnLevelFailed;
        }

        private void OnDisable()
        {
            _home.PlayRequested -= OnPlayRequested;
            _home.SettingsRequested -= OnSettingsRequested;
            _settings.CloseRequested -= OnPopupCloseRequested;
            _hud.PauseRequested -= OnPauseRequested;
            _pause.ResumeRequested -= OnResumeRequested;
            _pause.RestartRequested -= OnRestartRequested;
            _pause.HomeRequested -= OnHomeRequested;
            _fail.RestartRequested -= OnRestartRequested;
            _fail.HomeRequested -= OnHomeRequested;
            _levelPlayer.AttemptStarted -= OnAttemptStarted;
            _levelPlayer.Failed -= OnLevelFailed;
        }

        private void OnPlayRequested()
        {
            PlayRequested?.Invoke();
        }

        private void OnSettingsRequested()
        {
            _router.OpenPopup(_settings);
        }

        private void OnPopupCloseRequested()
        {
            _router.ClosePopup();
        }

        // Only a running attempt can be paused; after a win or fail the level is already ending.
        private void OnPauseRequested()
        {
            if (!_levelPlayer.IsPlaying)
            {
                return;
            }

            _levelPlayer.Pause();
            _router.OpenPopup(_pause);
        }

        private void OnResumeRequested()
        {
            _router.ClosePopup();
            _levelPlayer.Resume();
        }

        private void OnRestartRequested()
        {
            _levelPlayer.Restart();
        }

        private void OnHomeRequested()
        {
            HomeRequested?.Invoke();
        }

        private void OnAttemptStarted()
        {
            _router.CloseAllPopups();
        }

        private void OnLevelFailed()
        {
            _router.OpenPopup(_fail);
        }
    }
}
