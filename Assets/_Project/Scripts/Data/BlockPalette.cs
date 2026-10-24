using System;
using UnityEngine;

namespace CBJ.Data
{
    /// <summary>
    /// Maps the colour ids stored in levels to display colours. A colour id is an index into this palette.
    /// </summary>
    [CreateAssetMenu(fileName = "BlockPalette", menuName = "Color Block Jam/Block Palette")]
    public sealed class BlockPalette : ScriptableObject
    {
        [SerializeField] private PaletteEntry[] _entries = Array.Empty<PaletteEntry>();

        /// <summary>Number of colours.</summary>
        public int Count => _entries.Length;

        /// <summary>The entry for <paramref name="colorId"/>.</summary>
        public PaletteEntry Get(int colorId)
        {
            if (colorId < 0 || colorId >= _entries.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(colorId), colorId, $"The palette has {_entries.Length} colours.");
            }

            return _entries[colorId];
        }
    }
}
