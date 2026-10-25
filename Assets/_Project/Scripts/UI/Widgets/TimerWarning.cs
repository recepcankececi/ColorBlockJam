using UnityEngine;
using UnityEngine.UI;

namespace CBJ.UI
{
    /// <summary>
    /// Low-time warning on the HUD timer: below the threshold the holder turns red and pulses with its text. The
    /// pulse runs only while the attempt runs, so it stops on pause and fail. While a freeze stops the countdown the
    /// holder and its text turn blue instead and do not pulse.
    /// </summary>
    public sealed class TimerWarning : MonoBehaviour
    {
        [Tooltip("The warning starts when fewer whole seconds than this are left.")]
        [SerializeField, Min(1)] private int _thresholdSeconds = 10;

        [Tooltip("Tint of the holder while warning.")]
        [SerializeField] private Color _warningColor = new Color(1f, 0.25f, 0.25f, 1f);

        [Tooltip("Tint of the holder while a freeze stops the countdown.")]
        [SerializeField] private Color _frozenColor = new Color(0.45f, 0.75f, 1f, 1f);

        [Tooltip("Colour of the countdown text while a freeze stops it.")]
        [SerializeField] private Color _frozenLabelColor = new Color(0.55f, 0.9f, 1f, 1f);

        [Tooltip("Largest scale of the pulse; it swings between 1 and this.")]
        [SerializeField, Range(1f, 1.3f)] private float _pulseScale = 1.08f;

        [Tooltip("Pulses per second.")]
        [SerializeField, Min(0.1f)] private float _pulseFrequency = 1f;

        [SerializeField] private Image _holder;
        [SerializeField] private Text _label;

        private Color _restColor;
        private Color _restLabelColor;
        private bool _hasRestColor;
        private bool _warning;
        private bool _running;
        private bool _frozen;
        private float _phase;

        /// <summary>True when <paramref name="seconds"/> left is below <paramref name="thresholdSeconds"/>.</summary>
        public static bool IsWarning(int seconds, int thresholdSeconds)
        {
            return seconds < thresholdSeconds;
        }

        /// <summary>Pulse scale at <paramref name="phase"/> (in pulses): 1 at whole numbers, <paramref name="peak"/> halfway.</summary>
        public static float PulseScale(float phase, float peak)
        {
            float wave = 0.5f - 0.5f * Mathf.Cos(phase * 2f * Mathf.PI);
            return Mathf.LerpUnclamped(1f, peak, wave);
        }

        /// <summary>Updates the warning for the seconds now shown.</summary>
        public void SetSeconds(int seconds)
        {
            bool warning = IsWarning(seconds, _thresholdSeconds);
            if (warning == _warning)
            {
                return;
            }

            _warning = warning;
            _phase = 0f;
            Refresh();
        }

        /// <summary>Shows or clears the freeze tint.</summary>
        public void SetFrozen(bool frozen)
        {
            _frozen = frozen;
            Refresh();
        }

        /// <summary>Lets the pulse run or stops it at rest scale.</summary>
        public void SetRunning(bool running)
        {
            _running = running;
            Refresh();
        }

        private void Awake()
        {
            CaptureRestColor();
            enabled = false;
        }

        private void Update()
        {
            _phase += Time.deltaTime * _pulseFrequency;
            SetScale(PulseScale(_phase, _pulseScale));
        }

        private void Refresh()
        {
            CaptureRestColor();
            _holder.color = _frozen ? _frozenColor : _warning ? _warningColor : _restColor;
            _label.color = _frozen ? _frozenLabelColor : _restLabelColor;
            enabled = _warning && _running && !_frozen;
            if (!enabled)
            {
                SetScale(1f);
            }
        }

        // The HUD may report state before this component's Awake runs, so the authored tint is read on first use.
        private void CaptureRestColor()
        {
            if (!_hasRestColor)
            {
                _restColor = _holder.color;
                _restLabelColor = _label.color;
                _hasRestColor = true;
            }
        }

        private void SetScale(float scale)
        {
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
