using System;
using CBJ.Data;
using UnityEngine;

namespace CBJ.Gameplay
{
    /// <summary>
    /// Draws one <see cref="CBJ.Core.Block"/> as a single merged mesh. While held it lifts, casts a shadow and follows
    /// the drag offset on top of its model cell; released, it eases back onto the cell and drops; exiting, it slides
    /// out and shrinks away. The view keeps its mesh while pooled and refills it for the next block. Idle views are
    /// disabled, so only animating or held blocks run an update.
    /// </summary>
    public sealed class BlockView : MonoBehaviour
    {
        private Mesh _mesh;
        private MeshRenderer _renderer;
        private GameConfig _config;
        private Vector3 _cell;
        private Vector3 _offset;
        private Vector3 _pivot;
        private Vector3 _settleFrom;
        private float _settle;
        private float _lift;
        private bool _held;
        private bool _exiting;
        private Vector3 _exitFrom;
        private Vector3 _exitTo;
        private float _exitProgress;
        private Transform _shadow;
        private Vector3 _shadowScale;
        private Vector3 _shadowOffset;
        private Action<BlockView> _dropped;
        private Action<BlockView> _exitFinished;

        internal Mesh Mesh => _mesh;

        internal MeshRenderer Renderer => _renderer;

        internal void Initialize(Material material, GameConfig config)
        {
            _config = config;
            _mesh = new Mesh { name = "BlockMesh" };
            gameObject.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _renderer = gameObject.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = material;
        }

        // The pivot is the footprint centre on the floor; lift and exit scale around it.
        internal void Show(Vector3 cellPosition, Vector3 pivot)
        {
            _cell = cellPosition;
            _pivot = pivot;
            _offset = Vector3.zero;
            _settle = 1f;
            _lift = 0f;
            _held = false;
            _exiting = false;
            Apply(_cell, 1f);
            enabled = false;
        }

        // The view keeps its place on screen across a model step: the offset takes up the change. A held view gets its
        // next offset from the drag (or exits from here); a free one eases onto the new cell.
        internal void MoveTo(Vector3 cellPosition)
        {
            _offset += _cell - cellPosition;
            if (!_held)
            {
                BeginSettle();
            }

            _cell = cellPosition;
            enabled = true;
        }

        internal void Lift(Transform shadow, Vector3 shadowScale, Vector3 shadowOffset, Action<BlockView> dropped)
        {
            _shadowOffset = shadowOffset;
            _held = true;
            _settle = 1f;
            _shadow = shadow;
            _shadowScale = shadowScale;
            _dropped = dropped;
            enabled = true;
        }

        internal void Hold(Vector3 offset)
        {
            _offset = offset;
        }

        internal void Drop()
        {
            _held = false;
            BeginSettle();
        }

        internal void DetachShadow()
        {
            _shadow = null;
            _dropped = null;
        }

        internal void Exit(Vector3 exitPosition, Action<BlockView> finished)
        {
            _held = false;
            _exiting = true;
            _exitFrom = _cell + _offset;
            _exitTo = exitPosition;
            _exitProgress = 0f;
            _exitFinished = finished;
            enabled = true;
        }

        internal void ResetForPool()
        {
            _mesh.Clear();
            _renderer.SetPropertyBlock(null);
            _shadow = null;
            _dropped = null;
            _exitFinished = null;
            _exiting = false;
            _held = false;
            transform.localScale = Vector3.one;
            enabled = false;
        }

        private void OnDestroy()
        {
            Destroy(_mesh);
        }

        // LateUpdate, so a model step and the drag offset that compensates it land in the same frame.
        private void LateUpdate()
        {
            float deltaTime = Time.deltaTime;
            float easedLift = Ease.OutCubic(_lift);
            float liftScale = 1f + (_config.LiftScale - 1f) * easedLift;
            if (_exiting)
            {
                _exitProgress = Mathf.MoveTowards(_exitProgress, 1f, deltaTime / _config.ExitDuration);
                float eased = Ease.InQuad(_exitProgress);
                Vector3 position = Vector3.LerpUnclamped(_exitFrom, _exitTo, eased) + Vector3.up * (_config.LiftHeight * easedLift);
                Apply(position, Mathf.LerpUnclamped(liftScale, 0f, eased));
                if (_exitProgress >= 1f)
                {
                    enabled = false;
                    _exitFinished?.Invoke(this);
                }

                return;
            }

            _lift = Mathf.MoveTowards(_lift, _held ? 1f : 0f, deltaTime / _config.LiftDuration);
            if (_settle < 1f)
            {
                _settle = Mathf.MoveTowards(_settle, 1f, deltaTime / _config.SettleDuration);
                _offset = Vector3.LerpUnclamped(_settleFrom, Vector3.zero, Ease.OutCubic(_settle));
            }

            easedLift = Ease.OutCubic(_lift);
            Vector3 rest = _cell + _offset;
            Apply(rest + Vector3.up * (_config.LiftHeight * easedLift), 1f + (_config.LiftScale - 1f) * easedLift);
            if (_shadow != null)
            {
                _shadow.localPosition = rest + _pivot + _shadowOffset;
                _shadow.localScale = _shadowScale * easedLift;
            }

            if (!_held && _lift <= 0f && _settle >= 1f)
            {
                enabled = false;
                _dropped?.Invoke(this);
            }
        }

        private void BeginSettle()
        {
            _settleFrom = _offset;
            _settle = 0f;
        }

        // Scales around the footprint centre on the floor, so a lifted or exiting block grows and shrinks in place.
        private void Apply(Vector3 position, float scale)
        {
            transform.localPosition = position + _pivot * (1f - scale);
            transform.localScale = new Vector3(scale, scale, scale);
        }
    }
}
