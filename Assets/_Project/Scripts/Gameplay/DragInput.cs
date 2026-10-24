using CBJ.Core;
using CBJ.Data;
using UnityEngine;

namespace CBJ.Gameplay
{
    /// <summary>
    /// Drags blocks with one pointer (ADR-002): the block under the press is held, and each frame it steps cell by
    /// cell towards the finger, dominant axis first, until it catches up, hits something or leaves through a door.
    /// </summary>
    public sealed class DragInput : MonoBehaviour
    {
        private const int MaxStepsPerFrame = 8;

        private IPointerSource _pointer;
        private Camera _camera;
        private float _stepThreshold;
        private LevelSession _session;
        private BoardView _board;
        private Block _held;
        private Vector2 _grabOffset;
        private bool _wasPressed;

        /// <summary>Sets the input source and camera; call once before <see cref="Bind"/>.</summary>
        public void Initialize(IPointerSource pointer, Camera boardCamera, GameConfig config)
        {
            _pointer = pointer;
            _camera = boardCamera;
            _stepThreshold = config.DragStepThreshold;
        }

        /// <summary>Drags blocks of the session's current board as shown by <paramref name="board"/>.</summary>
        public void Bind(LevelSession session, BoardView board)
        {
            _session = session;
            _board = board;
            _held = null;
            _wasPressed = false;
        }

        /// <summary>Stops dragging until the next <see cref="Bind"/>.</summary>
        public void Unbind()
        {
            _session = null;
            _board = null;
            _held = null;
            _wasPressed = false;
        }

        private void Update()
        {
            if (_session == null)
            {
                return;
            }

            bool pressed = _pointer.IsPressed;
            bool began = pressed && !_wasPressed;
            _wasPressed = pressed;
            if (!pressed || _session.State != LevelState.Playing)
            {
                // A pause or the end of the attempt drops the block; the finger has to press again.
                _held = null;
                return;
            }

            if (!_board.TryRaycast(_camera.ScreenPointToRay(_pointer.Position), out Vector2 point))
            {
                return;
            }

            if (began)
            {
                Grab(point);
            }
            else if (_held != null)
            {
                Follow(point);
            }
        }

        private void Grab(Vector2 point)
        {
            _held = _session.Board.GetBlockAt(Vector2Int.FloorToInt(point));
            if (_held != null)
            {
                _grabOffset = point - _held.Origin;
            }
        }

        private void Follow(Vector2 point)
        {
            for (int i = 0; i < MaxStepsPerFrame; i++)
            {
                Vector2 lead = point - _grabOffset - _held.Origin;
                int axes = DragDirection.Resolve(lead, _stepThreshold, out Direction first, out Direction second);
                if (axes == 0)
                {
                    return;
                }

                StepResult result = _session.TryStep(_held, first);
                if (result == StepResult.Blocked && axes == 2)
                {
                    result = _session.TryStep(_held, second);
                }

                if (result != StepResult.Moved)
                {
                    if (result == StepResult.Exited)
                    {
                        _held = null;
                    }

                    return;
                }
            }
        }
    }
}
