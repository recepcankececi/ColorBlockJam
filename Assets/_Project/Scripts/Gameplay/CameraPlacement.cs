using UnityEngine;

namespace CBJ.Gameplay
{
    /// <summary>
    /// Where the perspective board camera goes, as computed by <see cref="CameraFit"/>.
    /// </summary>
    public readonly struct CameraPlacement
    {
        /// <summary>Creates a placement.</summary>
        public CameraPlacement(Vector3 position, float distance)
        {
            Position = position;
            Distance = distance;
        }

        /// <summary>World position of the camera.</summary>
        public Vector3 Position { get; }

        /// <summary>Distance along the view direction from the camera to the plane through the framed box's centre.</summary>
        public float Distance { get; }
    }
}
