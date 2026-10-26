using System;
using UnityEngine;

namespace CBJ.Gameplay
{
    /// <summary>
    /// Follows one finger from its first touch until it lifts. Other fingers are ignored, and a finger already on the
    /// screen never takes over: dragging needs a fresh touch.
    /// </summary>
    public sealed class TouchTracker
    {
        private const int NoFinger = -1;

        private readonly Func<int, Touch> _getTouch;
        private int _fingerId = NoFinger;

        /// <summary>Reads touches through <paramref name="getTouch"/> (e.g. <see cref="Input.GetTouch"/>).</summary>
        public TouchTracker(Func<int, Touch> getTouch)
        {
            _getTouch = getTouch ?? throw new ArgumentNullException(nameof(getTouch));
        }

        /// <summary>True while the followed finger is down.</summary>
        public bool IsTracking => _fingerId != NoFinger;

        /// <summary>Screen position of the followed finger at the last <see cref="Update"/>.</summary>
        public Vector2 Position { get; private set; }

        /// <summary>Reads this frame's <paramref name="touchCount"/> touches; call once per frame.</summary>
        public void Update(int touchCount)
        {
            if (_fingerId != NoFinger)
            {
                Follow(touchCount);
                return;
            }

            for (int i = 0; i < touchCount; i++)
            {
                Touch touch = _getTouch(i);
                if (touch.phase == TouchPhase.Began)
                {
                    _fingerId = touch.fingerId;
                    Position = touch.position;
                    return;
                }
            }
        }

        private void Follow(int touchCount)
        {
            for (int i = 0; i < touchCount; i++)
            {
                Touch touch = _getTouch(i);
                if (touch.fingerId != _fingerId)
                {
                    continue;
                }

                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    _fingerId = NoFinger;
                }
                else
                {
                    Position = touch.position;
                }

                return;
            }

            _fingerId = NoFinger;
        }
    }
}
