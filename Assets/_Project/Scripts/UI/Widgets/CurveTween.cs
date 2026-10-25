using UnityEngine;

namespace CBJ.UI
{
    /// <summary>
    /// Progress from 0 to 1 that moves towards a target at a fixed duration and is read through an
    /// <see cref="AnimationCurve"/>. Its owner advances it with unscaled time, so UI animates while the game is paused.
    /// </summary>
    public struct CurveTween
    {
        /// <summary>Linear progress, 0..1.</summary>
        public float Progress { get; private set; }

        /// <summary>Jumps to <paramref name="progress"/> (clamped to 0..1).</summary>
        public void Snap(float progress)
        {
            Progress = Mathf.Clamp01(progress);
        }

        /// <summary>
        /// Moves towards <paramref name="target"/> by <paramref name="deltaTime"/> over a full run of
        /// <paramref name="duration"/> seconds; false when it was already there.
        /// </summary>
        public bool Advance(float target, float deltaTime, float duration)
        {
            if (Mathf.Approximately(Progress, target))
            {
                return false;
            }

            Progress = Mathf.MoveTowards(Progress, target, deltaTime / duration);
            return true;
        }

        /// <summary>The progress shaped by <paramref name="curve"/>.</summary>
        public float Evaluate(AnimationCurve curve)
        {
            return curve.Evaluate(Progress);
        }
    }
}
