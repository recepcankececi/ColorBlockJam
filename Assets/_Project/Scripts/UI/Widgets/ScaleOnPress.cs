using UnityEngine;
using UnityEngine.EventSystems;

namespace CBJ.UI
{
    /// <summary>
    /// Press feedback (ADR-005): shrinks the object while a pointer holds it and springs back on release. Works on
    /// any raycast target, so placeholder buttons give feedback without a function.
    /// </summary>
    public sealed class ScaleOnPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [Tooltip("Scale relative to rest while fully pressed.")]
        [SerializeField, Range(0.5f, 1.2f)] private float _pressedScale = 0.9f;

        [Tooltip("Seconds to go from rest to fully pressed (and back). Unscaled time, so it works while paused.")]
        [SerializeField, Min(0.01f)] private float _duration = 0.08f;

        [Tooltip("Shape of the press over time: 0 = rest, 1 = pressed.")]
        [SerializeField] private AnimationCurve _curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private Vector3 _restScale;
        private float _progress;
        private bool _pressed;

        /// <inheritdoc />
        public void OnPointerDown(PointerEventData eventData)
        {
            _pressed = true;
        }

        /// <inheritdoc />
        public void OnPointerUp(PointerEventData eventData)
        {
            _pressed = false;
        }

        /// <inheritdoc />
        public void OnPointerExit(PointerEventData eventData)
        {
            _pressed = false;
        }

        private void Awake()
        {
            _restScale = transform.localScale;
        }

        private void OnDisable()
        {
            _pressed = false;
            _progress = 0f;
            transform.localScale = _restScale;
        }

        private void Update()
        {
            float target = _pressed ? 1f : 0f;
            if (Mathf.Approximately(_progress, target))
            {
                return;
            }

            _progress = Mathf.MoveTowards(_progress, target, Time.unscaledDeltaTime / _duration);
            transform.localScale = _restScale * Mathf.LerpUnclamped(1f, _pressedScale, _curve.Evaluate(_progress));
        }
    }
}
