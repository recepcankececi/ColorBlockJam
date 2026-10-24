using System;

namespace CBJ.UI
{
    /// <summary>
    /// Countdown text for the HUD.
    /// </summary>
    public static class TimerFormat
    {
        private const int SecondsPerMinute = 60;

        /// <summary>Whole seconds as <c>m:ss</c>, e.g. 75 → "1:15"; negative values read as 0.</summary>
        public static string ToClock(int seconds)
        {
            int clamped = Math.Max(0, seconds);
            return $"{clamped / SecondsPerMinute}:{clamped % SecondsPerMinute:00}";
        }
    }
}
