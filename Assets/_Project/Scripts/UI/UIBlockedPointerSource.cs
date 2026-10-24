using System;
using CBJ.Gameplay;
using UnityEngine;

namespace CBJ.UI
{
    /// <summary>
    /// Hides from drag input every press that starts on UI, for as long as it lasts, so tapping a HUD or booster
    /// button never grabs the block behind it.
    /// </summary>
    public sealed class UIBlockedPointerSource : IPointerSource
    {
        private readonly IPointerSource _inner;
        private readonly IUIHitTest _hitTest;
        private bool _wasPressed;
        private bool _blocked;

        /// <summary>Filters <paramref name="inner"/> with <paramref name="hitTest"/>.</summary>
        public UIBlockedPointerSource(IPointerSource inner, IUIHitTest hitTest)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _hitTest = hitTest ?? throw new ArgumentNullException(nameof(hitTest));
        }

        /// <inheritdoc />
        public bool IsPressed
        {
            get
            {
                bool pressed = _inner.IsPressed;
                if (pressed && !_wasPressed)
                {
                    _blocked = _hitTest.IsOverUI(_inner.Position);
                }

                _wasPressed = pressed;
                return pressed && !_blocked;
            }
        }

        /// <inheritdoc />
        public Vector2 Position => _inner.Position;
    }
}
