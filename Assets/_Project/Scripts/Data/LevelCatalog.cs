using System;
using System.Collections.Generic;
using UnityEngine;

namespace CBJ.Data
{
    /// <summary>
    /// The levels of the game in play order. The level editor appends new levels; reorder them in the Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelCatalog", menuName = "Color Block Jam/Level Catalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        [SerializeField] private List<LevelDefinition> _levels = new List<LevelDefinition>();

        /// <summary>Number of levels.</summary>
        public int Count => _levels.Count;

        /// <summary>The level at play position <paramref name="index"/>.</summary>
        public LevelDefinition Get(int index)
        {
            if (index < 0 || index >= _levels.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, $"The catalog has {_levels.Count} levels.");
            }

            return _levels[index];
        }

        /// <summary>Play position of <paramref name="level"/>, or -1 when it is not in the catalog.</summary>
        public int IndexOf(LevelDefinition level)
        {
            return _levels.IndexOf(level);
        }
    }
}
