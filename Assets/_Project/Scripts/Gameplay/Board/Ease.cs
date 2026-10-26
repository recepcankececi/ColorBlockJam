namespace CBJ.Gameplay
{
    /// <summary>
    /// Easing functions for view animations, mapping progress 0..1 to eased progress 0..1.
    /// </summary>
    public static class Ease
    {
        /// <summary>Starts fast and slows into the end.</summary>
        public static float OutCubic(float t)
        {
            float u = 1f - t;
            return 1f - u * u * u;
        }

        /// <summary>Starts slow and speeds up towards the end.</summary>
        public static float InQuad(float t)
        {
            return t * t;
        }
    }
}
