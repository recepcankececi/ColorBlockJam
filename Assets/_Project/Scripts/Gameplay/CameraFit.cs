using System;
using UnityEngine;

namespace CBJ.Gameplay
{
    /// <summary>
    /// Frames a box with an orthographic camera of fixed rotation inside the screen band left free by the UI, so the
    /// board fits any portrait aspect (1080×1920, 20:9, ...).
    /// </summary>
    public static class CameraFit
    {
        /// <summary>
        /// Position and orthographic size that show the whole box with <paramref name="padding"/> around it, between
        /// the top and bottom screen fractions kept for the UI.
        /// </summary>
        public static CameraPlacement Fit(Bounds box, Quaternion rotation, float aspect, float distance,
            float padding, float topReserved, float bottomReserved)
        {
            if (aspect <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(aspect), aspect, "Aspect must be positive.");
            }

            float band = 1f - topReserved - bottomReserved;
            if (band <= 0f)
            {
                throw new ArgumentException("The reserved screen fractions leave no room for the board.");
            }

            Vector3 right = rotation * Vector3.right;
            Vector3 up = rotation * Vector3.up;
            Vector3 forward = rotation * Vector3.forward;

            float minRight = float.MaxValue, maxRight = float.MinValue;
            float minUp = float.MaxValue, maxUp = float.MinValue;
            Vector3 extents = box.extents;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 offset = new Vector3(
                    (corner & 1) == 0 ? -extents.x : extents.x,
                    (corner & 2) == 0 ? -extents.y : extents.y,
                    (corner & 4) == 0 ? -extents.z : extents.z);
                float r = Vector3.Dot(offset, right);
                float u = Vector3.Dot(offset, up);
                minRight = Mathf.Min(minRight, r);
                maxRight = Mathf.Max(maxRight, r);
                minUp = Mathf.Min(minUp, u);
                maxUp = Mathf.Max(maxUp, u);
            }

            float width = maxRight - minRight + 2f * padding;
            float height = maxUp - minUp + 2f * padding;
            float size = Mathf.Max(height / band, width / aspect) * 0.5f;

            // The box's centre has to land in the middle of the free band, not the middle of the screen.
            float bandCenterFromScreenCenter = (bottomReserved + band * 0.5f - 0.5f) * 2f * size;
            float centerRight = (minRight + maxRight) * 0.5f;
            float centerUp = (minUp + maxUp) * 0.5f - bandCenterFromScreenCenter;
            Vector3 position = box.center + right * centerRight + up * centerUp - forward * distance;
            return new CameraPlacement(position, size);
        }
    }
}
