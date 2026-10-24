using UnityEngine;

namespace CBJ.Gameplay
{
    /// <summary>
    /// <see cref="IPointerSource"/> over the built-in input manager: one tracked finger, or the left mouse button
    /// when no finger is on the screen.
    /// </summary>
    public sealed class LegacyPointerSource : IPointerSource
    {
        private readonly TouchTracker _touches = new TouchTracker(Input.GetTouch);
        private int _updatedFrame = -1;

        /// <inheritdoc />
        public bool IsPressed
        {
            get
            {
                Refresh();
                // Touches also drive the simulated mouse, so the mouse only counts while no finger is down.
                return _touches.IsTracking || (Input.touchCount == 0 && Input.GetMouseButton(0));
            }
        }

        /// <inheritdoc />
        public Vector2 Position
        {
            get
            {
                Refresh();
                return _touches.IsTracking ? _touches.Position : (Vector2)Input.mousePosition;
            }
        }

        private void Refresh()
        {
            if (_updatedFrame != Time.frameCount)
            {
                _updatedFrame = Time.frameCount;
                _touches.Update(Input.touchCount);
            }
        }
    }
}
