using System;
using UnityEngine;

namespace CBJ.Gameplay
{
    /// <summary>
    /// Draws one <see cref="CBJ.Core.Block"/> from pooled modular pieces and glides it towards the cell the model moved it to.
    /// Idle views are disabled, so only moving blocks run an update.
    /// </summary>
    public sealed class BlockView : MonoBehaviour
    {
        private readonly PieceSet _pieces = new PieceSet();
        private Vector3 _target;
        private float _speed;
        private Action<BlockView> _exitFinished;

        internal PieceSet Pieces => _pieces;

        internal void Show(Vector3 localPosition, float speed)
        {
            _speed = speed;
            _target = localPosition;
            transform.localPosition = localPosition;
            enabled = false;
        }

        internal void MoveTo(Vector3 localPosition)
        {
            _target = localPosition;
            enabled = true;
        }

        internal void Exit(Vector3 localPosition, Action<BlockView> finished)
        {
            _exitFinished = finished;
            MoveTo(localPosition);
        }

        internal void ResetForPool()
        {
            _pieces.ReleaseAll();
            _exitFinished = null;
            enabled = false;
        }

        private void Update()
        {
            Vector3 position = Vector3.MoveTowards(transform.localPosition, _target, _speed * Time.deltaTime);
            transform.localPosition = position;
            if (position != _target)
            {
                return;
            }

            enabled = false;
            _exitFinished?.Invoke(this);
        }
    }
}
