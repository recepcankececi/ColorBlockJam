#if UNITY_EDITOR
using UnityEditor;

namespace CBJ.Data
{
    /// <summary>
    /// Editor-only hand-off from the level editor's Play button to the game: which catalog level to start.
    /// Kept in <see cref="SessionState"/> so it survives the domain reload on entering Play mode.
    /// </summary>
    public static class EditorPlayRequest
    {
        private const string LevelIndexKey = "CBJ.EditorPlayRequest.LevelIndex";
        private const int None = -1;

        /// <summary>Asks the game to start the catalog level at <paramref name="levelIndex"/> on the next Play.</summary>
        public static void Set(int levelIndex)
        {
            SessionState.SetInt(LevelIndexKey, levelIndex);
        }

        /// <summary>Returns and clears the pending request, so it applies to one Play session only.</summary>
        public static bool TryConsume(out int levelIndex)
        {
            levelIndex = SessionState.GetInt(LevelIndexKey, None);
            SessionState.EraseInt(LevelIndexKey);
            return levelIndex != None;
        }
    }
}
#endif
