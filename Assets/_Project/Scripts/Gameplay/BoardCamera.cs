using CBJ.Data;
using UnityEngine;

namespace CBJ.Gameplay
{
    /// <summary>
    /// Orthographic board camera: frames the board between the HUD and the booster bar inside the safe area and
    /// re-frames when the screen size or safe area changes (the editor Game view and foldables do change it).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class BoardCamera : MonoBehaviour
    {
        private Camera _camera;
        private GameConfig _config;
        private Bounds _target;
        private bool _hasTarget;
        private int _framedWidth;
        private int _framedHeight;
        private Rect _framedSafeArea;

        /// <summary>The camera that renders the board; set by <see cref="Initialize"/>.</summary>
        public Camera Camera => _camera;

        /// <summary>Sets the framing rules and switches the camera to orthographic; call once before <see cref="Frame"/>.</summary>
        public void Initialize(GameConfig config)
        {
            _config = config;
            _camera = GetComponent<Camera>();
            _camera.orthographic = true;
        }

        /// <summary>Frames <paramref name="worldBounds"/> now and whenever the screen size changes.</summary>
        public void Frame(Bounds worldBounds)
        {
            _target = worldBounds;
            _hasTarget = true;
            Apply();
        }

        private void LateUpdate()
        {
            if (_hasTarget && (Screen.width != _framedWidth || Screen.height != _framedHeight
                || Screen.safeArea != _framedSafeArea))
            {
                Apply();
            }
        }

        private void Apply()
        {
            Rect safeArea = Screen.safeArea;
            CameraFit.ReservedBands(safeArea, Screen.height, _config.TopReservedScreen, _config.BottomReservedScreen,
                out float top, out float bottom);
            Quaternion rotation = Quaternion.Euler(_config.CameraPitch, 0f, 0f);
            CameraPlacement placement = CameraFit.Fit(_target, rotation, _camera.aspect, _config.CameraDistance,
                _config.CameraPadding, top, bottom);
            transform.SetPositionAndRotation(placement.Position, rotation);
            _camera.orthographicSize = placement.OrthographicSize;
            _framedWidth = Screen.width;
            _framedHeight = Screen.height;
            _framedSafeArea = safeArea;
        }
    }
}
