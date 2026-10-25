using System;
using CBJ.Data;
using UnityEngine;

namespace CBJ.Gameplay
{
    /// <summary>
    /// Draws one <see cref="CBJ.Core.Block"/> as a single merged mesh. While held it lifts, casts a shadow and
    /// follows the drag offset on top of its model cell; released, it eases back onto the cell and drops. Exiting,
    /// it slides towards its door and is swallowed: it shrinks only along the exit axis, pinned at the door plane,
    /// while its shadow follows and fades. A destroyed block shrinks away where it stands. An ice block wears a
    /// translucent shell, a slightly larger copy of its mesh, that shrinks away when the ice thaws. The view keeps its
    /// mesh while pooled and refills it for the next block. Idle views are disabled, so only animating or held blocks
    /// run an update.
    /// </summary>
    public sealed class BlockView : MonoBehaviour
    {
        private static readonly int ShadowColorId = Shader.PropertyToID("_BaseColor");

        private Mesh _mesh;
        private MeshRenderer _renderer;
        private Transform _ice;
        private Material[] _plainMaterials;
        private Material[] _decalMaterials;
        private GameConfig _config;
        private float _thaw;
        private bool _thawing;
        private Vector3 _cell;
        private Vector3 _offset;
        private Vector3 _pivot;
        private Vector3 _settleFrom;
        private float _settle;
        private float _lift;
        private bool _held;
        private bool _exiting;
        private bool _swallowed;
        private Vector3 _exitFrom;
        private float _exitLift;
        private float _exitProgress;
        private bool _exitVertical;
        private float _exitSign;
        private float _exitPlane;
        private MeshRenderer _shadow;
        private MaterialPropertyBlock _shadowTint;
        private Color _shadowColor;
        private Vector3 _shadowScale;
        private Vector3 _shadowOffset;
        private Action<BlockView> _dropped;
        private Action<BlockView> _exitFinished;

        internal Mesh Mesh => _mesh;

        internal MeshRenderer Renderer => _renderer;

        internal void Initialize(Material material, Material decalMaterial, Material iceMaterial, GameConfig config)
        {
            _config = config;
            _mesh = new Mesh { name = "BlockMesh" };
            gameObject.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _renderer = gameObject.AddComponent<MeshRenderer>();
            _plainMaterials = new[] { material };
            _decalMaterials = new[] { material, decalMaterial };
            _renderer.sharedMaterials = _plainMaterials;
            _shadowTint = new MaterialPropertyBlock();

            var ice = new GameObject("IceShell");
            ice.SetActive(false);
            _ice = ice.transform;
            _ice.SetParent(transform, false);
            ice.AddComponent<MeshFilter>().sharedMesh = _mesh;
            ice.AddComponent<MeshRenderer>().sharedMaterial = iceMaterial;
        }

        // Call after the mesh is written: a decal block draws its second sub-mesh with the decal material.
        internal void SetKind(bool decal, bool iced)
        {
            _renderer.sharedMaterials = decal ? _decalMaterials : _plainMaterials;
            _thawing = false;
            _ice.gameObject.SetActive(iced);
            ScaleIce(_config.IceScale);
        }

        internal void Thaw()
        {
            if (_ice.gameObject.activeSelf && !_thawing)
            {
                _thawing = true;
                _thaw = 0f;
                enabled = true;
            }
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

        // The shadow fades through a property block while the block exits; its pool clears the block on release.
        internal void Lift(MeshRenderer shadow, Color shadowColor, Vector3 shadowScale, Vector3 shadowOffset,
            Action<BlockView> dropped)
        {
            _shadowOffset = shadowOffset;
            _held = true;
            _settle = 1f;
            _shadow = shadow;
            _shadowColor = shadowColor;
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

        // The block leaves along the local unit vector direction; doorPlane is the door's coordinate along it.
        internal void Exit(Vector3 direction, float doorPlane, Action<BlockView> finished)
        {
            BeginExit(finished);
            _swallowed = true;
            _exitVertical = direction.z != 0f;
            _exitSign = Mathf.Sign(_exitVertical ? direction.z : direction.x);
            _exitPlane = doorPlane;
        }

        internal void Vanish(Action<BlockView> finished)
        {
            BeginExit(finished);
            _swallowed = false;
        }

        internal void ResetForPool()
        {
            _mesh.Clear();
            _renderer.SetPropertyBlock(null, 0);
            _renderer.sharedMaterials = _plainMaterials;
            _ice.gameObject.SetActive(false);
            _thawing = false;
            _shadow = null;
            _dropped = null;
            _exitFinished = null;
            _exiting = false;
            _swallowed = false;
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
            if (_thawing)
            {
                _thaw = Mathf.MoveTowards(_thaw, 1f, deltaTime / _config.IceThawDuration);
                ScaleIce(Mathf.LerpUnclamped(_config.IceScale, 0f, Ease.InQuad(_thaw)));
                if (_thaw >= 1f)
                {
                    _thawing = false;
                    _ice.gameObject.SetActive(false);
                }
            }

            if (_exiting)
            {
                _exitProgress = Mathf.MoveTowards(_exitProgress, 1f, deltaTime / _config.ExitDuration);
                float eased = Ease.InQuad(_exitProgress);
                if (_swallowed)
                {
                    ApplySwallow(eased);
                }
                else
                {
                    Apply(_exitFrom + Vector3.up * (_config.LiftHeight * _exitLift), 1f - eased);
                }

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

            float easedLift = Ease.OutCubic(_lift);
            Vector3 rest = _cell + _offset;
            Apply(rest + Vector3.up * (_config.LiftHeight * easedLift), 1f + (_config.LiftScale - 1f) * easedLift);
            if (_shadow != null)
            {
                Transform shadow = _shadow.transform;
                shadow.localPosition = rest + _pivot + _shadowOffset;
                shadow.localScale = _shadowScale * easedLift;
            }

            if (!_held && !_thawing && _lift <= 0f && _settle >= 1f)
            {
                enabled = false;
                _dropped?.Invoke(this);
            }
        }

        private void BeginExit(Action<BlockView> finished)
        {
            _held = false;
            _exiting = true;
            _exitFrom = _cell + _offset;
            _exitLift = Ease.OutCubic(_lift);
            _exitProgress = 0f;
            _exitFinished = finished;
            enabled = true;
        }

        // Along the exit axis the block spans [trailing, leading]. The trailing face slides to the door plane; the
        // leading face slides with it until it meets the plane and stays there, so the door swallows the block
        // instead of the block passing through it. Across the axis the lifted size eases back to rest around the
        // footprint centre, and the lift settles to the floor.
        private void ApplySwallow(float eased)
        {
            float liftScale = 1f + (_config.LiftScale - 1f) * _exitLift;
            float across = Mathf.LerpUnclamped(liftScale, 1f, eased);
            float rest = 2f * (_exitVertical ? _pivot.z : _pivot.x);
            float length = rest * liftScale;
            float start = (_exitVertical ? _exitFrom.z + _pivot.z : _exitFrom.x + _pivot.x) - length * 0.5f;
            float trailing0 = _exitSign > 0f ? start : -(start + length);
            float leading0 = trailing0 + length;
            float plane = Mathf.Max(_exitPlane, leading0);
            float travel = (plane - trailing0) * eased;
            float trailing = trailing0 + travel;
            float leading = Mathf.Min(leading0 + travel, plane);
            float scale = (leading - trailing) / rest;
            float min = _exitSign > 0f ? trailing : -leading;

            Vector3 position = _exitFrom + _pivot * (1f - across);
            position.y = _exitFrom.y + _config.LiftHeight * _exitLift * (1f - eased);
            var scales = new Vector3(across, across, across);
            if (_exitVertical)
            {
                position.z = min;
                scales.z = scale;
            }
            else
            {
                position.x = min;
                scales.x = scale;
            }

            transform.localPosition = position;
            transform.localScale = scales;
            if (_shadow != null)
            {
                FollowWithShadow(position, scales, scale, 1f - eased);
            }
        }

        private void FollowWithShadow(Vector3 position, Vector3 scales, float axisScale, float alpha)
        {
            Transform shadow = _shadow.transform;
            Vector3 center = position + Vector3.Scale(_pivot, scales);
            center.y = _exitFrom.y;
            shadow.localPosition = center + _shadowOffset;

            // The shadow quad lies flat: its local X runs along the board's X, its local Y along the board's Z.
            Vector3 shadowScale = _shadowScale * _exitLift;
            if (_exitVertical)
            {
                shadowScale.y *= axisScale;
            }
            else
            {
                shadowScale.x *= axisScale;
            }

            shadow.localScale = shadowScale;
            Color color = _shadowColor;
            color.a *= alpha;
            _shadowTint.SetColor(ShadowColorId, color);
            _shadow.SetPropertyBlock(_shadowTint);
        }

        private void BeginSettle()
        {
            _settleFrom = _offset;
            _settle = 0f;
        }

        // The shell scales around the footprint centre like the block, so it wraps the block evenly.
        private void ScaleIce(float scale)
        {
            _ice.localPosition = _pivot * (1f - scale);
            _ice.localScale = new Vector3(scale, scale, scale);
        }

        // Scales around the footprint centre on the floor, so a lifted or exiting block grows and shrinks in place.
        private void Apply(Vector3 position, float scale)
        {
            transform.localPosition = position + _pivot * (1f - scale);
            transform.localScale = new Vector3(scale, scale, scale);
        }
    }
}
