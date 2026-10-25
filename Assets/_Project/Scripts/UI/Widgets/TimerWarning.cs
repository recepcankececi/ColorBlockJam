using UnityEngine;
using UnityEngine.UI;

namespace CBJ.UI
{
    /// <summary>
    /// Low-time warning on the HUD timer: below the threshold the holder turns red and pulses with its text. The
    /// pulse runs only while the attempt runs, so it stops on pause and fail.
    /// </summary>
    public sealed class TimerWarning : MonoBehaviour
    {
        [Tooltip("The warning starts when fewer whole seconds than this are left.")]
        [SerializeField, Min(1)] private int _thresholdSeconds = 10;

        [Tooltip("Tint of the holder while warning.")]
        [SerializeField] private Color _warningColor = new Color(1f, 0.25f, 0.25f, 1f);

        [Tooltip("Largest scale of the pulse; it swings between 1 and this.")]
        [SerializeField, Range(1f, 1.3f)] private float _pulseScale = 1.08f;

        [Tooltip("Pulses per second.")]
        [SerializeField, Min(0.1f)] private float _pulseFrequency = 1f;

        [SerializeField] private Image _holder;

        private Color _restColor;
        private bool _warning;
        private bool _running;
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
            _holder.color = warning ? _warningColor : _restColor;
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
            _restColor = _holder.color;
            enabled = false;
        }

        private void Update()
        {
            _phase += Time.deltaTime * _pulseFrequency;
            SetScale(PulseScale(_phase, _pulseScale));
        }

        private void Refresh()
        {
            enabled = _warning && _running;
            if (!enabled)
            {
                SetScale(1f);
            }
        }

        private void SetScale(float scale)
        {
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
