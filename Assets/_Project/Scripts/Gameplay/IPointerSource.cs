using UnityEngine;

namespace CBJ.Gameplay
{
    /// <summary>
    /// One pointer (a tracked finger or the mouse) as drag input reads it, so the input backend can be swapped.
    /// </summary>
    public interface IPointerSource
    {
        /// <summary>True while the pointer is down.</summary>
        bool IsPressed { get; }

        /// <summary>Screen position of the pointer in pixels.</summary>
        Vector2 Position { get; }
    }
}
