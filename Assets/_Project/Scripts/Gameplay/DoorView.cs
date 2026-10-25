using UnityEngine;

namespace CBJ.Gameplay
{
    /// <summary>
    /// Draws one <see cref="CBJ.Core.Door"/>: a tinted frame strip across its whole opening with one arrow in the
    /// middle. When a block leaves through it, the door sinks to the board surface, stays down while the block slides
    /// out, then rises again. Idle doors are disabled.
    /// </summary>
    public sealed class DoorView : MonoBehaviour
    {
        private readonly PieceSet _pieces = new PieceSet();

        private float _depth;
        private float _speed;
        private float _downLeft;
        private float _height;

        internal PieceSet Pieces => _pieces;

        internal void Open(float depth, float sinkDuration, float downSeconds)
        {
            _depth = depth;
            _speed = depth / sinkDuration;
            _downLeft = downSeconds;
            enabled = true;
        }

        internal void ResetForPool()
        {
            _pieces.ReleaseAll();
            _height = 0f;
            _downLeft = 0f;
            transform.localPosition = Vector3.zero;
            enabled = false;
        }

        private void Awake()
        {
            enabled = false;
        }

        private void Update()
        {
            _downLeft -= Time.deltaTime;
            float target = _downLeft > 0f ? -_depth : 0f;
            _height = Mathf.MoveTowards(_height, target, _speed * Time.deltaTime);
            transform.localPosition = new Vector3(0f, _height, 0f);
            if (_downLeft <= 0f && _height >= 0f)
            {
                enabled = false;
            }
        }
    }
}
