using System;
using UnityEngine;

namespace CBJ.Core
{
    /// <summary>
    /// Serialized form of a <see cref="Door"/>.
    /// </summary>
    [Serializable]
    public struct DoorData
    {
        [SerializeField] private Direction _edge;
        [SerializeField] private int _offset;
        [SerializeField] private int _length;
        [SerializeField] private int _colorId;

        /// <summary>Creates door data; see <see cref="Door"/> for the meaning of each value.</summary>
        public DoorData(Direction edge, int offset, int length, int colorId)
        {
            _edge = edge;
            _offset = offset;
            _length = length;
            _colorId = colorId;
        }

        /// <summary>The edge the door sits on.</summary>
        public Direction Edge => _edge;

        /// <summary>First covered cell along the edge.</summary>
        public int Offset => _offset;

        /// <summary>Number of covered cells.</summary>
        public int Length => _length;

        /// <summary>Palette index a block needs to pass through.</summary>
        public int ColorId => _colorId;

        /// <summary>True when the door covers <paramref name="slot"/> on its edge.</summary>
        public bool Covers(int slot)
        {
            return slot >= _offset && slot < _offset + _length;
        }

        /// <summary>Creates the runtime door.</summary>
        public Door ToDoor()
        {
            return new Door(_edge, _offset, _length, _colorId);
        }
    }
}
