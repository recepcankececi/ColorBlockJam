using System;
using UnityEngine;
using UnityEngine.UI;

namespace CBJ.UI
{
    /// <summary>
    /// One booster slot on the HUD: its uses left on a badge, greyed out once spent, untappable while the booster is
    /// unavailable, and highlighted while the booster waits for a target.
    /// </summary>
    public sealed class BoosterButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _background;
        [SerializeField] private Image _icon;
        [SerializeField] private Text _count;

        [Tooltip("Tint of the background and icon once every use is spent.")]
        [SerializeField] private Color _spentColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        [Tooltip("Tint of the background while the booster waits for a target.")]
        [SerializeField] private Color _selectedColor = new Color(1f, 0.85f, 0.35f, 1f);

        private bool _spent;
        private bool _selected;

        /// <summary>Raised when the player taps the booster.</summary>
        public event Action Pressed;

        /// <summary>Shows <paramref name="uses"/> left; the button only responds while usable and <paramref name="available"/>.</summary>
        public void Show(int uses, bool available)
        {
            _count.text = uses.ToString();
            _spent = uses <= 0;
            _button.interactable = available && !_spent;
            Refresh();
        }

        /// <summary>Highlights the booster while it waits for the player to pick a target.</summary>
        public void SetSelected(bool selected)
        {
            _selected = selected;
            Refresh();
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
            Pressed?.Invoke();
        }

        private void Refresh()
        {
            _background.color = _spent ? _spentColor : _selected ? _selectedColor : Color.white;
            _icon.color = _spent ? _spentColor : Color.white;
        }
    }
}
