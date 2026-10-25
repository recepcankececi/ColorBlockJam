using CBJ.UI;
using NUnit.Framework;
using UnityEngine;

namespace CBJ.Tests.EditMode.UI
{
    public class CurveTweenTests
    {
        private const float Duration = 0.2f;
        private const float Tolerance = 1e-4f;

        [Test]
        public void Advance_HalfTheDuration_MovesHalfWay()
        {
            var tween = new CurveTween();

            tween.Advance(1f, Duration * 0.5f, Duration);

            Assert.That(tween.Progress, Is.EqualTo(0.5f).Within(Tolerance));
        }

        [Test]
        public void Advance_PastTheDuration_StopsAtTheTarget()
        {
            var tween = new CurveTween();

            tween.Advance(1f, Duration * 3f, Duration);

            Assert.That(tween.Progress, Is.EqualTo(1f));
        }

        [Test]
        public void Advance_AlreadyAtTheTarget_ReturnsFalse()
        {
            var tween = new CurveTween();
            tween.Snap(1f);

            Assert.That(tween.Advance(1f, Duration, Duration), Is.False);
        }

        [Test]
        public void Evaluate_Midway_ReadsTheCurve()
        {
            var tween = new CurveTween();
            tween.Snap(0.5f);

            float value = tween.Evaluate(AnimationCurve.Linear(0f, 0f, 1f, 2f));

            Assert.That(value, Is.EqualTo(1f).Within(Tolerance));
        }
    }
}
