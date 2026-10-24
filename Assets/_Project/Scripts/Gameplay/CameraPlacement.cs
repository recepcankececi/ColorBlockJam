using UnityEngine;

namespace CBJ.Gameplay
{
    /// <summary>
    /// Where an orthographic camera goes and how much it shows, as computed by <see cref="CameraFit"/>.
    /// </summary>
    public readonly struct CameraPlacement
    {
        /// <summary>Creates a placement.</summary>
        public CameraPlacement(Vector3 position, float orthographicSize)
        {
            Position = position;
            OrthographicSize = orthographicSize;
        }

        /// <summary>World position of the camera.</summary>
        public Vector3 Position { get; }

        /// <summary>Half the visible height in world units.</summary>
        public float OrthographicSize { get; }
    }
}
