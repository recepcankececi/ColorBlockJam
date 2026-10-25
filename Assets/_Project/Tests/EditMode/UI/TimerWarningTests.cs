using CBJ.UI;
using NUnit.Framework;

namespace CBJ.Tests.EditMode.UI
{
    public class TimerWarningTests
    {
        private const int Threshold = 10;
        private const float Peak = 1.08f;
        private const float Tolerance = 1e-4f;

        [Test]
        public void IsWarning_AtTheThreshold_ReturnsFalse()
        {
            Assert.That(TimerWarning.IsWarning(Threshold, Threshold), Is.False);
        }

        [Test]
        public void IsWarning_BelowTheThreshold_ReturnsTrue()
        {
            Assert.That(TimerWarning.IsWarning(Threshold - 1, Threshold), Is.True);
        }

        [Test]
        public void PulseScale_WholePulse_IsAtRest()
        {
            Assert.That(TimerWarning.PulseScale(3f, Peak), Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void PulseScale_HalfPulse_IsAtThePeak()
        {
            Assert.That(TimerWarning.PulseScale(0.5f, Peak), Is.EqualTo(Peak).Within(Tolerance));
        }
    }
}
