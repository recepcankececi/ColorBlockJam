using UnityEngine;

namespace CBJ.Data
{
    /// <summary>
    /// Gameplay and presentation tunables: drag feel, block and door animation, block kinds, boosters, camera framing,
    /// the pause after a win and the coin reward.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Color Block Jam/Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        [Header("Drag")]
        [Tooltip("How far, in cells, the finger must lead a held block before it picks an axis to slide along.")]
        [SerializeField, Range(0.05f, 0.45f)] private float _dragAxisLock = 0.2f;

        [Tooltip("World units a held block rises above the board.")]
        [SerializeField, Min(0f)] private float _liftHeight = 0.4f;

        [Tooltip("Scale of a held block relative to rest.")]
        [SerializeField, Range(1f, 1.2f)] private float _liftScale = 1.03f;

        [Tooltip("Seconds a block takes to rise when grabbed (and to drop when released).")]
        [SerializeField, Min(0.01f)] private float _liftDuration = 0.08f;

        [Tooltip("Seconds a released block takes to ease back onto its cell.")]
        [SerializeField, Min(0.01f)] private float _settleDuration = 0.12f;

        [Tooltip("Size of the shadow under a held block relative to the block's footprint.")]
        [SerializeField, Range(0.5f, 1.5f)] private float _shadowSpread = 1.05f;

        [Header("Exit")]
        [Tooltip("Seconds an exiting block takes to slide out one cell and shrink away.")]
        [SerializeField, Min(0.01f)] private float _exitDuration = 0.25f;

        [Tooltip("Seconds a door takes to sink into the board when a block leaves through it (and to rise again).")]
        [SerializeField, Min(0.01f)] private float _doorSinkDuration = 0.15f;

        [Header("Block kinds")]
        [Tooltip("Scale of the ice shell around an ice block relative to the block.")]
        [SerializeField, Range(1f, 1.2f)] private float _iceScale = 1.03f;

        [Tooltip("Seconds the ice shell takes to shrink away when its last layer thaws.")]
        [SerializeField, Min(0.01f)] private float _iceThawDuration = 0.2f;

        [Tooltip("World units the arrow decal of an arrow block sinks into the block's top face.")]
        [SerializeField, Min(0f)] private float _arrowSink = 0.3f;

        [Header("Boosters")]
        [Tooltip("Hammer and freeze uses the player gets in every attempt.")]
        [SerializeField, Min(0)] private int _boosterUsesPerAttempt = 1;

        [Tooltip("Seconds the freeze booster stops the countdown.")]
        [SerializeField, Min(0f)] private float _freezeSeconds = 10f;

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

        /// <summary>How far, in cells, the finger must lead a held block before it picks an axis.</summary>
        public float DragAxisLock => _dragAxisLock;

        /// <summary>World units a held block rises above the board.</summary>
        public float LiftHeight => _liftHeight;

        /// <summary>Scale of a held block relative to rest.</summary>
        public float LiftScale => _liftScale;

        /// <summary>Seconds a block takes to rise when grabbed and to drop when released.</summary>
        public float LiftDuration => _liftDuration;

        /// <summary>Seconds a released block takes to ease back onto its cell.</summary>
        public float SettleDuration => _settleDuration;

        /// <summary>Size of the shadow under a held block relative to its footprint.</summary>
        public float ShadowSpread => _shadowSpread;

        /// <summary>Seconds an exiting block takes to slide out and shrink away.</summary>
        public float ExitDuration => _exitDuration;

        /// <summary>Seconds a door takes to sink when a block leaves through it, and to rise again.</summary>
        public float DoorSinkDuration => _doorSinkDuration;

        /// <summary>Scale of the ice shell around an ice block.</summary>
        public float IceScale => _iceScale;

        /// <summary>Seconds the ice shell takes to shrink away.</summary>
        public float IceThawDuration => _iceThawDuration;

        /// <summary>World units an arrow decal sinks into its block's top face.</summary>
        public float ArrowSink => _arrowSink;

        /// <summary>Hammer and freeze uses per attempt.</summary>
        public int BoosterUsesPerAttempt => _boosterUsesPerAttempt;

        /// <summary>Seconds the freeze booster stops the countdown.</summary>
        public float FreezeSeconds => _freezeSeconds;

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
