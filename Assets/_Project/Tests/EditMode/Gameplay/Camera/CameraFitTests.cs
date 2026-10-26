using System;
using CBJ.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace CBJ.Tests.EditMode.Gameplay
{
    public class CameraFitTests
    {
        private const float Tolerance = 1e-3f;
        private const float RightAngleFov = 90f;
        private const float GameFov = 45f;
        private const float GamePitch = 55f;
        private const float Portrait = 1080f / 1920f;
        private const float TallPortrait = 1080f / 2400f;
        private const float HudBand = 0.16f;

        private static readonly Quaternion TopDown = Quaternion.Euler(90f, 0f, 0f);
        private static readonly Quaternion Tilted = Quaternion.Euler(GamePitch, 0f, 0f);

        // About the size of a 7×9 board with its wall ring: 2 units per cell plus 1 per wall side.
        private static readonly Bounds Board = new Bounds(new Vector3(0f, -0.2f, 0f), new Vector3(16f, 1.6f, 20f));

        [Test]
        public void Fit_FlatBoxLookingDown_DistanceFitsTheLimitingSide()
        {
            var box = new Bounds(Vector3.zero, new Vector3(4f, 0f, 4f));

            CameraPlacement placement = CameraFit.Fit(box, TopDown, RightAngleFov, 1f, 0f, 0f, 0f);

            Assert.That(placement.Distance, Is.EqualTo(2f).Within(Tolerance));
        }

        [Test]
        public void Fit_WideBoxOnPortrait_IsLimitedByWidth()
        {
            var box = new Bounds(Vector3.zero, new Vector3(10f, 0f, 10f));

            CameraPlacement placement = CameraFit.Fit(box, TopDown, RightAngleFov, Portrait, 0f, 0f, 0f);

            Assert.That(placement.Distance * Portrait, Is.EqualTo(5f).Within(Tolerance));
        }

        [Test]
        public void Fit_TallerAspect_MovesTheCameraFurtherForTheSameWidth()
        {
            var box = new Bounds(Vector3.zero, new Vector3(10f, 0f, 10f));

            float portrait = CameraFit.Fit(box, TopDown, GameFov, Portrait, 0f, 0f, 0f).Distance;
            float tall = CameraFit.Fit(box, TopDown, GameFov, TallPortrait, 0f, 0f, 0f).Distance;

            Assert.That(tall, Is.GreaterThan(portrait));
        }

        [Test]
        public void Fit_Padding_AddsToEverySideOnTheBoardPlane()
        {
            var box = new Bounds(Vector3.zero, new Vector3(4f, 0f, 4f));

            CameraPlacement placement = CameraFit.Fit(box, TopDown, RightAngleFov, 1f, 1f, 0f, 0f);

            Assert.That(placement.Distance, Is.EqualTo(3f).Within(Tolerance));
        }

        [Test]
        public void Fit_ReservedScreen_ShrinksTheBandTheBoxMustFit()
        {
            var box = new Bounds(Vector3.zero, new Vector3(1f, 0f, 4f));

            CameraPlacement placement = CameraFit.Fit(box, TopDown, RightAngleFov, 1f, 0f, 0.25f, 0.25f);

            Assert.That(placement.Distance, Is.EqualTo(4f).Within(Tolerance));
        }

        [Test]
        public void Fit_MoreReservedAtTheBottom_PutsTheBoxAboveScreenCentre()
        {
            var box = new Bounds(Vector3.zero, new Vector3(1f, 0f, 4f));

            CameraPlacement placement = CameraFit.Fit(box, TopDown, RightAngleFov, 1f, 0f, 0f, 0.5f);

            // Looking straight down, screen up is world +Z: the camera sits below the box so the box shows higher.
            Assert.That(placement.Position.z, Is.LessThan(0f));
        }

        [Test]
        public void Fit_LookingDown_PlacesCameraAboveTheBoxAtTheDistance()
        {
            var box = new Bounds(Vector3.zero, new Vector3(4f, 0f, 4f));

            CameraPlacement placement = CameraFit.Fit(box, TopDown, RightAngleFov, 1f, 0f, 0f, 0f);

            Assert.That(Vector3.Distance(placement.Position, new Vector3(0f, 2f, 0f)), Is.LessThan(Tolerance));
        }

        [Test]
        public void Fit_TiltedBoardOn9By16_EveryCornerProjectsInsideTheFreeBand()
        {
            AssertCornersInsideBand(Portrait);
        }

        [Test]
        public void Fit_TiltedBoardOn20By9_EveryCornerProjectsInsideTheFreeBand()
        {
            AssertCornersInsideBand(TallPortrait);
        }

        [Test]
        public void Fit_TiltedBoard_TouchesTheFrustumOnTheLimitingSide()
        {
            CameraPlacement placement = CameraFit.Fit(Board, Tilted, GameFov, Portrait, 0f, HudBand, HudBand);

            Vector4 extremes = ProjectedExtremes(placement.Position, Portrait);
            float low = 2f * HudBand - 1f;
            float high = 1f - 2f * HudBand;
            float slack = Mathf.Min(1f - Mathf.Max(-extremes.x, extremes.y), Mathf.Min(extremes.z - low, high - extremes.w));
            Assert.That(slack, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void Fit_ReservedScreenCoversEverything_Throws()
        {
            var box = new Bounds(Vector3.zero, Vector3.one);

            Assert.That(() => CameraFit.Fit(box, TopDown, GameFov, 1f, 0f, 0.5f, 0.5f), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Fit_FieldOfViewOutOfRange_Throws()
        {
            var box = new Bounds(Vector3.zero, Vector3.one);

            Assert.That(() => CameraFit.Fit(box, TopDown, 0f, 1f, 0f, 0f, 0f), Throws.TypeOf<ArgumentOutOfRangeException>());
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

        private static void AssertCornersInsideBand(float aspect)
        {
            CameraPlacement placement = CameraFit.Fit(Board, Tilted, GameFov, aspect, 0f, HudBand, HudBand);

            Vector4 extremes = ProjectedExtremes(placement.Position, aspect);
            Assert.That(extremes.x, Is.GreaterThanOrEqualTo(-1f - Tolerance), "Left edge.");
            Assert.That(extremes.y, Is.LessThanOrEqualTo(1f + Tolerance), "Right edge.");
            Assert.That(extremes.z, Is.GreaterThanOrEqualTo(2f * HudBand - 1f - Tolerance), "Bottom band.");
            Assert.That(extremes.w, Is.LessThanOrEqualTo(1f - 2f * HudBand + Tolerance), "Top band.");
        }

        // Normalised device coordinates of the board corners seen from the camera: (min x, max x, min y, max y).
        private static Vector4 ProjectedExtremes(Vector3 cameraPosition, float aspect)
        {
            float tanY = Mathf.Tan(GameFov * 0.5f * Mathf.Deg2Rad);
            Vector3 right = Tilted * Vector3.right;
            Vector3 up = Tilted * Vector3.up;
            Vector3 forward = Tilted * Vector3.forward;
            var extremes = new Vector4(float.MaxValue, float.MinValue, float.MaxValue, float.MinValue);
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 extents = Board.extents;
                Vector3 point = Board.center + new Vector3(
                    (corner & 1) == 0 ? -extents.x : extents.x,
                    (corner & 2) == 0 ? -extents.y : extents.y,
                    (corner & 4) == 0 ? -extents.z : extents.z);
                Vector3 view = point - cameraPosition;
                float depth = Vector3.Dot(view, forward);
                float x = Vector3.Dot(view, right) / (depth * tanY * aspect);
                float y = Vector3.Dot(view, up) / (depth * tanY);
                extremes = new Vector4(Mathf.Min(extremes.x, x), Mathf.Max(extremes.y, x), Mathf.Min(extremes.z, y),
                    Mathf.Max(extremes.w, y));
            }

            return extremes;
        }
    }
}
