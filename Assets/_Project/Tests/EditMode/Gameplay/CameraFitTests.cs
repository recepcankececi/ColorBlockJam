using System;
using CBJ.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace CBJ.Tests.EditMode.Gameplay
{
    public class CameraFitTests
    {
        private const float Tolerance = 1e-4f;
        private const float Distance = 20f;
        private const float Portrait = 1080f / 1920f;
        private const float TallPortrait = 1080f / 2400f;

        private static readonly Quaternion TopDown = Quaternion.Euler(90f, 0f, 0f);

        [Test]
        public void Fit_FlatBoxLookingDown_SizeIsHalfTheLimitingSide()
        {
            var box = new Bounds(Vector3.zero, new Vector3(4f, 0f, 4f));

            CameraPlacement placement = CameraFit.Fit(box, TopDown, 1f, Distance, 0f, 0f, 0f);

            Assert.That(placement.OrthographicSize, Is.EqualTo(2f).Within(Tolerance));
        }

        [Test]
        public void Fit_WideBoxOnPortrait_IsLimitedByWidth()
        {
            var box = new Bounds(Vector3.zero, new Vector3(10f, 0f, 10f));

            CameraPlacement placement = CameraFit.Fit(box, TopDown, Portrait, Distance, 0f, 0f, 0f);

            Assert.That(placement.OrthographicSize * 2f * Portrait, Is.EqualTo(10f).Within(Tolerance));
        }

        [Test]
        public void Fit_TallerAspect_ShowsMoreHeightForTheSameWidth()
        {
            var box = new Bounds(Vector3.zero, new Vector3(10f, 0f, 10f));

            float portrait = CameraFit.Fit(box, TopDown, Portrait, Distance, 0f, 0f, 0f).OrthographicSize;
            float tall = CameraFit.Fit(box, TopDown, TallPortrait, Distance, 0f, 0f, 0f).OrthographicSize;

            Assert.That(tall, Is.GreaterThan(portrait));
        }

        [Test]
        public void Fit_Padding_AddsToEverySide()
        {
            var box = new Bounds(Vector3.zero, new Vector3(4f, 0f, 4f));

            CameraPlacement placement = CameraFit.Fit(box, TopDown, 1f, Distance, 1f, 0f, 0f);

            Assert.That(placement.OrthographicSize, Is.EqualTo(3f).Within(Tolerance));
        }

        [Test]
        public void Fit_ReservedScreen_ShrinksTheBandTheBoxMustFit()
        {
            var box = new Bounds(Vector3.zero, new Vector3(1f, 0f, 4f));

            CameraPlacement placement = CameraFit.Fit(box, TopDown, 1f, Distance, 0f, 0.25f, 0.25f);

            Assert.That(placement.OrthographicSize, Is.EqualTo(4f).Within(Tolerance));
        }

        [Test]
        public void Fit_MoreReservedAtTheBottom_PutsTheBoxAboveScreenCentre()
        {
            var box = new Bounds(Vector3.zero, new Vector3(1f, 0f, 4f));

            CameraPlacement placement = CameraFit.Fit(box, TopDown, 1f, Distance, 0f, 0f, 0.5f);

            // Looking straight down, screen up is world +Z: the camera sits below the box so the box shows higher.
            Assert.That(placement.Position.z, Is.LessThan(0f));
        }

        [Test]
        public void Fit_LookingDown_PlacesCameraAboveTheBoxAtTheDistance()
        {
            var box = new Bounds(Vector3.zero, new Vector3(4f, 0f, 4f));

            CameraPlacement placement = CameraFit.Fit(box, TopDown, 1f, Distance, 0f, 0f, 0f);

            Assert.That(Vector3.Distance(placement.Position, new Vector3(0f, Distance, 0f)), Is.LessThan(Tolerance));
        }

        [Test]
        public void Fit_ReservedScreenCoversEverything_Throws()
        {
            var box = new Bounds(Vector3.zero, Vector3.one);

            Assert.That(() => CameraFit.Fit(box, TopDown, 1f, Distance, 0f, 0.5f, 0.5f), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void ReservedBands_FullScreenSafeArea_KeepsTheUiBands()
        {
            CameraFit.ReservedBands(new Rect(0f, 0f, 1080f, 1920f), 1920f, 0.16f, 0.12f, out float top, out float bottom);

            Assert.That((top, bottom), Is.EqualTo((0.16f, 0.12f)));
        }

        [Test]
        public void ReservedBands_NotchAndGestureBar_AddTheCutOffFractions()
        {
            CameraFit.ReservedBands(new Rect(0f, 96f, 1080f, 2304f - 96f - 192f), 2304f, 0.16f, 0.12f,
                out float top, out float bottom);

            Assert.That(top, Is.EqualTo(0.16f + 192f / 2304f).Within(Tolerance));
            Assert.That(bottom, Is.EqualTo(0.12f + 96f / 2304f).Within(Tolerance));
        }
    }
}
