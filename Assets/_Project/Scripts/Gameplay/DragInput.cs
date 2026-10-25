using System;
using CBJ.Core;
using CBJ.Data;
using UnityEngine;

namespace CBJ.Gameplay
{
    /// <summary>
    /// Drags blocks with one pointer: the block under the press is lifted and its view follows the finger along one
    /// axis, as far as the model allows; <see cref="BlockDrag"/> steps the model cell by cell (ADR-002) and lets a
    /// block that reaches its door leave on its own. Releasing settles the view back onto its model cell. After
    /// <see cref="PickNext"/>, the next press picks a block instead of dragging it (the hammer booster).
    /// </summary>
    public sealed class DragInput : MonoBehaviour
    {
        private IPointerSource _pointer;
        private Camera _camera;
        private float _axisLock;
        private LevelSession _session;
        private BoardView _board;
        private BlockDrag _drag;
        private Action<Block> _pick;
        private bool _wasPressed;

        /// <summary>Sets the input source and camera; call once before <see cref="Bind"/>.</summary>
        public void Initialize(IPointerSource pointer, Camera boardCamera, GameConfig config)
        {
            _pointer = pointer;
            _camera = boardCamera;
            _axisLock = config.DragAxisLock;
        }

        /// <summary>Drags blocks of the session's current board as shown by <paramref name="board"/>.</summary>
        public void Bind(LevelSession session, BoardView board)
        {
            _session = session;
            _board = board;
            _drag = new BlockDrag(session, _axisLock);
            _wasPressed = false;
        }

        /// <summary>Stops dragging until the next <see cref="Bind"/>.</summary>
        public void Unbind()
        {
            _session = null;
            _board = null;
            _drag = null;
            _pick = null;
            _wasPressed = false;
        }

        /// <summary>
        /// Hands the block under the next press to <paramref name="picked"/> instead of dragging it; null when the
        /// press lands on an empty cell or off the board. Picking ends after that one press.
        /// </summary>
        public void PickNext(Action<Block> picked)
        {
            Drop();
            _pick = picked;
        }

        /// <summary>Drops a pending <see cref="PickNext"/>.</summary>
        public void CancelPick()
        {
            _pick = null;
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
                Drop();
                return;
            }

            bool onBoard = _board.TryRaycast(_camera.ScreenPointToRay(_pointer.Position), out Vector2 point);
            if (began && _pick != null)
            {
                Pick(onBoard ? _session.Board.GetBlockAt(Vector2Int.FloorToInt(point)) : null);
                return;
            }

            if (!onBoard)
            {
                return;
            }

            if (began)
            {
                Grab(point);
            }
            else if (_drag.Held != null)
            {
                Follow(point);
            }
        }

        // A frozen ice block is not picked up: lifting it would suggest it can move.
        private void Grab(Vector2 point)
        {
            Block block = _session.Board.GetBlockAt(Vector2Int.FloorToInt(point));
            if (block != null && block.IceLayers == 0)
            {
                _drag.Grab(block, point);
                _board.Lift(block);
            }
        }

        // An exited block is already animating out through BoardView's model events; nothing more to show here.
        private void Follow(Vector2 point)
        {
            Block block = _drag.Held;
            _drag.Drag(point);
            if (_drag.Held != null)
            {
                _board.Hold(block, _drag.Offset);
            }
        }

        private void Pick(Block block)
        {
            Action<Block> picked = _pick;
            _pick = null;
            picked(block);
        }

        private void Drop()
        {
            if (_drag != null && _drag.Held != null)
            {
                _board.Drop(_drag.Held);
                _drag.Release();
            }
        }
    }
}
