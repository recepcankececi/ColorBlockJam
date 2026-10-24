using CBJ.UI;
using NUnit.Framework;

namespace CBJ.Tests.EditMode.UI
{
    public class TimerFormatTests
    {
        [Test]
        public void ToClock_UnderAMinute_PadsSeconds()
        {
            Assert.That(TimerFormat.ToClock(7), Is.EqualTo("0:07"));
        }

        [Test]
        public void ToClock_OverAMinute_SplitsMinutesAndSeconds()
        {
            Assert.That(TimerFormat.ToClock(75), Is.EqualTo("1:15"));
        }

        [Test]
        public void ToClock_Negative_ReadsZero()
        {
            Assert.That(TimerFormat.ToClock(-3), Is.EqualTo("0:00"));
        }
    }
}
