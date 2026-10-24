using System;
using UnityEngine;
using UnityEngine.UI;

namespace CBJ.UI
{
    /// <summary>
    /// Settings: vibration, sound and music switches bound to <see cref="GameSettings"/>, and a close button. The
    /// other buttons are placeholders with press feedback.
    /// </summary>
    public sealed class SettingsPopup : ScreenView
    {
        [SerializeField] private Button _closeButton;
        [SerializeField] private ToggleSwitch _vibration;
        [SerializeField] private ToggleSwitch _sound;
        [SerializeField] private ToggleSwitch _music;

        private GameSettings _settings;

        /// <summary>Raised when the player closes the popup.</summary>
        public event Action CloseRequested;

        /// <summary>Sets the settings the switches show and change; call before the popup is first shown.</summary>
        public void Bind(GameSettings settings)
        {
            _settings = settings;
        }

        private void OnEnable()
        {
            _closeButton.onClick.AddListener(OnClose);
            _vibration.Clicked += OnVibrationClicked;
            _sound.Clicked += OnSoundClicked;
            _music.Clicked += OnMusicClicked;
            _settings.Changed += OnSettingChanged;
            Refresh(Setting.Vibration);
            Refresh(Setting.Sound);
            Refresh(Setting.Music);
        }

        private void OnDisable()
        {
            _closeButton.onClick.RemoveListener(OnClose);
            _vibration.Clicked -= OnVibrationClicked;
            _sound.Clicked -= OnSoundClicked;
            _music.Clicked -= OnMusicClicked;
            _settings.Changed -= OnSettingChanged;
        }

        private void OnClose()
        {
            CloseRequested?.Invoke();
        }

        private void OnVibrationClicked()
        {
            _settings.Toggle(Setting.Vibration);
        }

        private void OnSoundClicked()
        {
            _settings.Toggle(Setting.Sound);
        }

        private void OnMusicClicked()
        {
            _settings.Toggle(Setting.Music);
        }

        private void OnSettingChanged(Setting setting, bool on)
        {
            Refresh(setting);
        }

        private void Refresh(Setting setting)
        {
            SwitchFor(setting).SetOn(_settings.IsOn(setting));
        }

        private ToggleSwitch SwitchFor(Setting setting)
        {
            switch (setting)
            {
                case Setting.Vibration:
                    return _vibration;
                case Setting.Sound:
                    return _sound;
                default:
                    return _music;
            }
        }
    }
}
