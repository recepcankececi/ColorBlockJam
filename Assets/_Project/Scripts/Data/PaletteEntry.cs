using System;
using UnityEngine;

namespace CBJ.Data
{
    /// <summary>
    /// One named colour of the <see cref="BlockPalette"/>.
    /// </summary>
    [Serializable]
    public struct PaletteEntry
    {
        [SerializeField] private string _name;
        [SerializeField] private Color _color;

        /// <summary>Creates a palette entry.</summary>
        public PaletteEntry(string name, Color color)
        {
            _name = name;
            _color = color;
        }

        /// <summary>Name shown to designers.</summary>
        public string Name => _name;

        /// <summary>Colour of blocks and doors with this id.</summary>
        public Color Color => _color;
    }
}
