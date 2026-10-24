using UnityEngine;

namespace CBJ.Data
{
    /// <summary>
    /// Gameplay and presentation tunables: block motion, drag feel, camera framing, the pause after a win and the
    /// coin reward.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Color Block Jam/Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        [Header("Blocks")]
        [Tooltip("World units per second a block view travels while catching up with its cell.")]
        [SerializeField, Min(0.1f)] private float _blockSpeed = 30f;

        [Tooltip("How far, in cells, the finger must lead the block before it steps one cell.")]
        [SerializeField, Range(0.1f, 1f)] private float _dragStepThreshold = 0.5f;

        [Header("Camera")]
        [Tooltip("Downward tilt of the board camera in degrees; 90 looks straight down.")]
        [SerializeField, Range(30f, 90f)] private float _cameraPitch = 75f;

        [Tooltip("Distance from the board to the camera; only affects clipping for an orthographic camera.")]
        [SerializeField, Min(1f)] private float _cameraDistance = 40f;

        [Tooltip("Empty world space kept around the board on every side.")]
        [SerializeField, Min(0f)] private float _cameraPadding = 0.5f;

        [Tooltip("Fraction of the screen height kept free at the top for the HUD.")]
        [SerializeField, Range(0f, 0.4f)] private float _topReservedScreen = 0.16f;

        [Tooltip("Fraction of the screen height kept free at the bottom for the booster bar.")]
        [SerializeField, Range(0f, 0.4f)] private float _bottomReservedScreen = 0.16f;

        [Header("Flow")]
        [Tooltip("Seconds between winning a level and the next level starting.")]
        [SerializeField, Min(0f)] private float _levelEndDelay = 1f;

        [Header("Economy")]
        [Tooltip("Coins the player earns for completing a level.")]
        [SerializeField, Min(0)] private int _coinsPerWin = 10;

        /// <summary>World units per second a block view travels while catching up with its cell.</summary>
        public float BlockSpeed => _blockSpeed;

        /// <summary>How far, in cells, the finger must lead the block before it steps one cell.</summary>
        public float DragStepThreshold => _dragStepThreshold;

        /// <summary>Downward tilt of the board camera in degrees.</summary>
        public float CameraPitch => _cameraPitch;

        /// <summary>Distance from the board to the camera.</summary>
        public float CameraDistance => _cameraDistance;

        /// <summary>Empty world space kept around the board on every side.</summary>
        public float CameraPadding => _cameraPadding;

        /// <summary>Fraction of the screen height kept free at the top.</summary>
        public float TopReservedScreen => _topReservedScreen;

        /// <summary>Fraction of the screen height kept free at the bottom.</summary>
        public float BottomReservedScreen => _bottomReservedScreen;

        /// <summary>Seconds between winning a level and the next level starting.</summary>
        public float LevelEndDelay => _levelEndDelay;

        /// <summary>Coins the player earns for completing a level.</summary>
        public int CoinsPerWin => _coinsPerWin;
    }
}
