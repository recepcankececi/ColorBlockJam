using UnityEngine;

namespace CBJ.UI
{
    /// <summary>
    /// Entrance animation of a screen or popup (ADR-005, no tween library): fades the whole screen in while its
    /// content grows from a slightly smaller scale. Unscaled time, so it also plays while the game is paused.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ScreenTransition : MonoBehaviour
    {
        [Tooltip("What grows into place: a popup's panel, or a screen's content.")]
        [SerializeField] private RectTransform _scaleTarget;

        [Tooltip("Scale of the content when the animation starts.")]
        [SerializeField, Range(0.5f, 1f)] private float _startScale = 0.9f;

        [Tooltip("Seconds the entrance takes.")]
        [SerializeField, Min(0.01f)] private float _duration = 0.18f;

        [Tooltip("Shape of the entrance over time: 0 = start, 1 = in place.")]
        [SerializeField] private AnimationCurve _curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private CanvasGroup _group;
        private CurveTween _tween;

        /// <summary>Starts the entrance from the beginning.</summary>
        public void Play()
        {
            _tween.Snap(0f);
            Apply();
            enabled = true;
        }

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
        }

        // A screen hidden mid-entrance must come back fully shown if it is shown without a transition.
        private void OnDisable()
        {
            _tween.Snap(1f);
            Apply();
        }

        private void Update()
        {
            if (!_tween.Advance(1f, Time.unscaledDeltaTime, _duration))
            {
                enabled = false;
                return;
            }

            Apply();
        }

        private void Apply()
        {
            float eased = _tween.Evaluate(_curve);
            _group.alpha = eased;

            // Nothing half-faded can be tapped: a quick double tap must not hit a button nobody can see yet.
            _group.blocksRaycasts = _tween.Progress >= 1f;
            float scale = Mathf.LerpUnclamped(_startScale, 1f, eased);
            _scaleTarget.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
