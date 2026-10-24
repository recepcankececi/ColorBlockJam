using System;
using UnityEngine;

namespace CBJ.Core
{
    /// <summary>
    /// Serialized form of a <see cref="Block"/>: a <see cref="ShapeLibrary"/> id, a colour and a start cell.
    /// </summary>
    [Serializable]
    public struct BlockData
    {
        [SerializeField] private string _shapeId;
        [SerializeField] private Vector2Int _origin;
        [SerializeField] private int _colorId;

        /// <summary>Creates block data; <paramref name="origin"/> is the shape's bottom-left corner.</summary>
        public BlockData(string shapeId, Vector2Int origin, int colorId)
        {
            _shapeId = shapeId;
            _origin = origin;
            _colorId = colorId;
        }

        /// <summary>Id of the shape in <see cref="ShapeLibrary"/>.</summary>
        public string ShapeId => _shapeId;

        /// <summary>Board cell of the shape's bottom-left corner at level start.</summary>
        public Vector2Int Origin => _origin;

        /// <summary>Palette index of the block.</summary>
        public int ColorId => _colorId;

        /// <summary>The shape this block uses; throws for an unknown id.</summary>
        public BlockShape Shape => ShapeLibrary.Get(_shapeId);

        /// <summary>Creates a fresh runtime block at the start cell.</summary>
        public Block ToBlock()
        {
            return new Block(Shape, _colorId, _origin);
        }
    }
}
