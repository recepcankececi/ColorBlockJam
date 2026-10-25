using System;
using CBJ.Data;
using CBJ.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace CBJ.UI
{
    /// <summary>
    /// Gameplay HUD: level badge, countdown with its low-time and freeze tints, restart and pause buttons, the coin
    /// count and the booster bar. Hammer and freeze work (one tap arms the hammer, a second tap puts it away); the
    /// other two boosters are locked placeholders with press feedback.
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
        [SerializeField] private BoosterButton _hammerBooster;
        [SerializeField] private BoosterButton _freezeBooster;
        [SerializeField] private GameObject _hammerHint;

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
            _levelPlayer.RunningChanged += OnRunningChanged;
            _levelPlayer.TimerFrozenChanged += _timerWarning.SetFrozen;
            _levelPlayer.BoostersChanged += RefreshBoosters;
            _levelPlayer.HammerAimChanged += OnHammerAimChanged;
            _hammerBooster.Pressed += OnHammerPressed;
            _freezeBooster.Pressed += OnFreezePressed;
            _timerWarning.SetRunning(_levelPlayer.IsPlaying);
            _timerWarning.SetFrozen(_levelPlayer.IsTimerFrozen);
            OnHammerAimChanged(_levelPlayer.IsAimingHammer);
            RefreshBoosters();
        }

        private void OnDisable()
        {
            _pauseButton.onClick.RemoveListener(OnPause);
            _restartButton.onClick.RemoveListener(OnRestart);
            _levelPlayer.DisplaySecondsChanged -= OnDisplaySecondsChanged;
            _levelPlayer.RunningChanged -= OnRunningChanged;
            _levelPlayer.TimerFrozenChanged -= _timerWarning.SetFrozen;
            _levelPlayer.BoostersChanged -= RefreshBoosters;
            _levelPlayer.HammerAimChanged -= OnHammerAimChanged;
            _hammerBooster.Pressed -= OnHammerPressed;
            _freezeBooster.Pressed -= OnFreezePressed;
        }

        private void OnPause()
        {
            PauseRequested?.Invoke();
        }

        private void OnRestart()
        {
            RestartRequested?.Invoke();
        }

        private void OnRunningChanged(bool running)
        {
            _timerWarning.SetRunning(running);
            RefreshBoosters();
        }

        private void OnHammerPressed()
        {
            if (_levelPlayer.IsAimingHammer)
            {
                _levelPlayer.CancelHammer();
            }
            else
            {
                _levelPlayer.AimHammer();
            }
        }

        private void OnFreezePressed()
        {
            _levelPlayer.UseFreeze();
        }

        private void OnHammerAimChanged(bool aiming)
        {
            _hammerHint.SetActive(aiming);
            _hammerBooster.SetSelected(aiming);
        }

        // Boosters only respond while the attempt runs, so a pause or the level end greys nothing but blocks taps.
        private void RefreshBoosters()
        {
            bool playing = _levelPlayer.IsPlaying;
            _hammerBooster.Show(_levelPlayer.HammerUses, playing);
            _freezeBooster.Show(_levelPlayer.FreezeUses, playing);
        }

        private void OnDisplaySecondsChanged(int seconds)
        {
            _timerLabel.text = TimerFormat.ToClock(seconds);
            _timerWarning.SetSeconds(seconds);
        }
    }
}
