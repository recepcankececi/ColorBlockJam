using UnityEngine;

namespace CBJ.UI
{
    /// <summary>
    /// Answers whether a screen point lies on interactive UI.
    /// </summary>
    public interface IUIHitTest
    {
        /// <summary>True when the UI would receive a press at <paramref name="screenPosition"/>.</summary>
        bool IsOverUI(Vector2 screenPosition);
    }
}
