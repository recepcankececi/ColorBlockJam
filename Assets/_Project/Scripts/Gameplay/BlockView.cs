using System;
using UnityEngine;

namespace CBJ.Gameplay
{
    /// <summary>
    /// Draws one <see cref="CBJ.Core.Block"/> as a single merged mesh and glides it towards the cell the model moved it
    /// to. The view keeps its mesh while pooled and refills it for the next block. Idle views are disabled, so only
    /// moving blocks run an update.
    /// </summary>
    public sealed class BlockView : MonoBehaviour
    {
        private Mesh _mesh;
        private MeshRenderer _renderer;
        private Vector3 _target;
        private float _speed;
        private Action<BlockView> _exitFinished;

        internal Mesh Mesh => _mesh;

        internal MeshRenderer Renderer => _renderer;

        internal void Initialize(Material material)
        {
            _mesh = new Mesh { name = "BlockMesh" };
            gameObject.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _renderer = gameObject.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = material;
        }

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
            _mesh.Clear();
            _renderer.SetPropertyBlock(null);
            _exitFinished = null;
            enabled = false;
        }

        private void OnDestroy()
        {
            Destroy(_mesh);
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
