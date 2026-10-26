using System;
using UnityEngine;

namespace CBJ.Gameplay
{
    /// <summary>
    /// Frames a box with a perspective camera of fixed rotation and field of view inside the screen band left free by
    /// the UI, so the board fits any portrait aspect (1080×1920, 20:9, ...).
    /// </summary>
    public static class CameraFit
    {
        private const int Corners = 8;

        /// <summary>
        /// Screen fractions to keep free at the top and bottom: the UI bands plus whatever the safe area cuts off
        /// (notch, rounded corners, gesture bar).
        /// </summary>
        public static void ReservedBands(Rect safeArea, float screenHeight, float topUi, float bottomUi,
            out float top, out float bottom)
        {
            if (screenHeight <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(screenHeight), screenHeight, "Screen height must be positive.");
            }

            top = topUi + Mathf.Max(0f, screenHeight - safeArea.yMax) / screenHeight;
            bottom = bottomUi + Mathf.Max(0f, safeArea.yMin) / screenHeight;
        }

        /// <summary>
        /// The closest camera position that shows the whole box, grown by <paramref name="padding"/> on the board
        /// plane (X and Z), between the top and bottom screen fractions kept for the UI.
        /// <paramref name="verticalFieldOfView"/> is in degrees, as on <see cref="Camera.fieldOfView"/>.
        /// </summary>
        public static CameraPlacement Fit(Bounds box, Quaternion rotation, float verticalFieldOfView, float aspect,
            float padding, float topReserved, float bottomReserved)
        {
            if (aspect <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(aspect), aspect, "Aspect must be positive.");
            }

            if (verticalFieldOfView <= 0f || verticalFieldOfView >= 180f)
            {
                throw new ArgumentOutOfRangeException(nameof(verticalFieldOfView), verticalFieldOfView,
                    "Field of view must lie between 0 and 180 degrees.");
            }

            // Visible band in normalised device coordinates (-1 bottom .. 1 top).
            float low = 2f * bottomReserved - 1f;
            float high = 1f - 2f * topReserved;
            if (high <= low)
            {
                throw new ArgumentException("The reserved screen fractions leave no room for the board.");
            }

            Vector3 right = rotation * Vector3.right;
            Vector3 up = rotation * Vector3.up;
            Vector3 forward = rotation * Vector3.forward;
            float tanY = Mathf.Tan(verticalFieldOfView * 0.5f * Mathf.Deg2Rad);
            float tanX = tanY * aspect;

            // Corner i sits at (r, u, f) in the camera basis around the box centre. The camera goes to
            // centre + a*right + b*up - d*forward; a corner shows when |r - a| <= tanX (f + d) and
            // low tanY (f + d) <= u - b <= high tanY (f + d). Every pair of corners gives a lower bound on d.
            Vector3 extents = box.extents + new Vector3(padding, 0f, padding);
            float distance = 0f;
            for (int i = 0; i < Corners; i++)
            {
                Vector3 ci = Corner(extents, i);
                float ri = Vector3.Dot(ci, right), ui = Vector3.Dot(ci, up), fi = Vector3.Dot(ci, forward);
                for (int j = 0; j < Corners; j++)
                {
                    Vector3 cj = Corner(extents, j);
                    float rj = Vector3.Dot(cj, right), uj = Vector3.Dot(cj, up), fj = Vector3.Dot(cj, forward);
                    distance = Mathf.Max(distance, ((ri - rj) / tanX - fi - fj) * 0.5f);
                    distance = Mathf.Max(distance, ((ui - uj) / tanY - high * fi + low * fj) / (high - low));
                }
            }

            // At that distance the binding axis has exactly one offset left; the other is centred in its range.
            float minA = float.MinValue, maxA = float.MaxValue, minB = float.MinValue, maxB = float.MaxValue;
            for (int i = 0; i < Corners; i++)
            {
                Vector3 c = Corner(extents, i);
                float r = Vector3.Dot(c, right), u = Vector3.Dot(c, up), depth = Vector3.Dot(c, forward) + distance;
                minA = Mathf.Max(minA, r - tanX * depth);
                maxA = Mathf.Min(maxA, r + tanX * depth);
                minB = Mathf.Max(minB, u - high * tanY * depth);
                maxB = Mathf.Min(maxB, u - low * tanY * depth);
            }

            float a = (minA + maxA) * 0.5f;
            float b = (minB + maxB) * 0.5f;
            return new CameraPlacement(box.center + right * a + up * b - forward * distance, distance);
        }

        private static Vector3 Corner(Vector3 extents, int corner)
        {
            return new Vector3(
                (corner & 1) == 0 ? -extents.x : extents.x,
                (corner & 2) == 0 ? -extents.y : extents.y,
                (corner & 4) == 0 ? -extents.z : extents.z);
        }
    }
}
