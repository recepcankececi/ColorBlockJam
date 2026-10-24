namespace CBJ.App
{
    /// <summary>
    /// Which catalog level to play: where a saved game resumes and which level follows a win.
    /// </summary>
    public static class LevelOrder
    {
        /// <summary>
        /// <paramref name="savedIndex"/> when it is still a level of a catalog with <paramref name="count"/> levels,
        /// otherwise the first level (the catalog may have shrunk since the save).
        /// </summary>
        public static int Resume(int savedIndex, int count)
        {
            return savedIndex >= 0 && savedIndex < count ? savedIndex : 0;
        }

        /// <summary>The level after <paramref name="index"/>; after the last level the game starts over at the first.</summary>
        public static int Next(int index, int count)
        {
            return count > 0 ? (index + 1) % count : 0;
        }
    }
}
