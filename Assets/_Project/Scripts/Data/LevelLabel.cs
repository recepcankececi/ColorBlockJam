namespace CBJ.Data
{
    /// <summary>
    /// The name a level is shown under, taken from its play position in the <see cref="LevelCatalog"/> so labels
    /// follow the catalog order rather than asset names.
    /// </summary>
    public static class LevelLabel
    {
        /// <summary>"Level n" for the level at <paramref name="catalogIndex"/> (0-based).</summary>
        public static string For(int catalogIndex)
        {
            return $"Level {catalogIndex + 1}";
        }
    }
}
