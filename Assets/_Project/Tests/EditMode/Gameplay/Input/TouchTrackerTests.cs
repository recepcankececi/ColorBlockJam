using CBJ.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace CBJ.Tests.EditMode.Gameplay
{
    public class TouchTrackerTests
    {
        private Touch[] _touches;
        private TouchTracker _tracker;

        [SetUp]
        public void CreateTracker()
        {
            _touches = new Touch[2];
            _tracker = new TouchTracker(i => _touches[i]);
        }

        [Test]
        public void Update_FingerBegins_TracksItsPosition()
        {
            Feed(Finger(3, TouchPhase.Began, 10f));

            Assert.That((_tracker.IsTracking, _tracker.Position), Is.EqualTo((true, new Vector2(10f, 0f))));
        }

        [Test]
        public void Update_FingerAlreadyDownWithoutBegan_IsNotTracked()
        {
            Feed(Finger(3, TouchPhase.Moved, 10f));

            Assert.That(_tracker.IsTracking, Is.False);
        }

        [Test]
        public void Update_SecondFingerJoins_KeepsFollowingTheFirst()
        {
            Feed(Finger(3, TouchPhase.Began, 10f));

            Feed(Finger(7, TouchPhase.Began, 99f), Finger(3, TouchPhase.Moved, 20f));

            Assert.That(_tracker.Position, Is.EqualTo(new Vector2(20f, 0f)));
        }

        [Test]
        public void Update_TrackedFingerEnds_StopsTracking()
        {
            Feed(Finger(3, TouchPhase.Began, 10f));

            Feed(Finger(3, TouchPhase.Ended, 10f));

            Assert.That(_tracker.IsTracking, Is.False);
        }

        [Test]
        public void Update_FirstFingerLiftsWhileSecondStaysDown_DoesNotSwitchFingers()
        {
            Feed(Finger(3, TouchPhase.Began, 10f));
            Feed(Finger(3, TouchPhase.Moved, 10f), Finger(7, TouchPhase.Began, 99f));
            Feed(Finger(3, TouchPhase.Ended, 10f), Finger(7, TouchPhase.Moved, 99f));

            Feed(Finger(7, TouchPhase.Moved, 99f));

            Assert.That(_tracker.IsTracking, Is.False);
        }

        [Test]
        public void Update_TrackedFingerMissing_StopsTracking()
        {
            Feed(Finger(3, TouchPhase.Began, 10f));

            Feed();

            Assert.That(_tracker.IsTracking, Is.False);
        }

        private void Feed(params Touch[] touches)
        {
            touches.CopyTo(_touches, 0);
            _tracker.Update(touches.Length);
        }

        private static Touch Finger(int id, TouchPhase phase, float x)
        {
            return new Touch { fingerId = id, phase = phase, position = new Vector2(x, 0f) };
        }
    }
}
