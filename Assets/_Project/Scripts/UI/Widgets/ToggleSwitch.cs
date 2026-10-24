using System;
using UnityEngine;
using UnityEngine.UI;

namespace CBJ.UI
{
    /// <summary>
    /// Two-state switch (ADR-005). Only an "On" sprite is supplied, so the off state swaps in a plain sprite, tints
    /// it and shows an "Off" label. It does not flip itself: the owner changes the setting and calls <see cref="SetOn"/>.
    /// </summary>
    public sealed class ToggleSwitch : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _image;
        [SerializeField] private Sprite _onSprite;
        [SerializeField] private Sprite _offSprite;
        [SerializeField] private Color _offTint = new Color(0.55f, 0.55f, 0.6f);
        [Tooltip("Shown only while off, e.g. an \"Off\" label.")]
        [SerializeField] private GameObject _offLabel;

        /// <summary>Raised when the player taps the switch.</summary>
        public event Action Clicked;

        /// <summary>Shows the on or off state.</summary>
        public void SetOn(bool on)
        {
            _image.sprite = on ? _onSprite : _offSprite;
            _image.color = on ? Color.white : _offTint;
            _offLabel.SetActive(!on);
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(OnClick);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnClick);
        }

        private void OnClick()
        {
            Clicked?.Invoke();
        }
    }
}
