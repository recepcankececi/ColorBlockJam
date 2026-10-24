using UnityEngine;

namespace CBJ.Gameplay
{
    /// <summary>
    /// <see cref="IPointerSource"/> over the built-in input manager: the first touch, or the left mouse button.
    /// </summary>
    public sealed class LegacyPointerSource : IPointerSource
    {
        /// <inheritdoc />
        public bool IsPressed => Input.touchCount > 0 || Input.GetMouseButton(0);

        /// <inheritdoc />
        public Vector2 Position => Input.touchCount > 0 ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;
    }
}
