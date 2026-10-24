using CBJ.Core;
using UnityEngine;

namespace CBJ.Data
{
    /// <summary>
    /// Asset that holds one level's <see cref="LevelData"/>. Written by the level editor, read by the game.
    /// </summary>
    [CreateAssetMenu(fileName = "Level", menuName = "Color Block Jam/Level Definition")]
    public sealed class LevelDefinition : ScriptableObject
    {
        /// <summary>Board width of a newly created level.</summary>
        public const int DefaultWidth = 6;

        /// <summary>Board height of a newly created level.</summary>
        public const int DefaultHeight = 8;

        /// <summary>Time limit of a newly created level.</summary>
        public const int DefaultTimerSeconds = 60;

        [SerializeField] private LevelData _level = new LevelData(DefaultWidth, DefaultHeight, DefaultTimerSeconds);

        /// <summary>The level's saved data.</summary>
        public LevelData Level => _level;
    }
}
