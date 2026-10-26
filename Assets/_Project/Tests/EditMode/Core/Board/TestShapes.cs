using CBJ.Core;
using UnityEngine;

namespace CBJ.Tests.EditMode.Core
{
    /// <summary>
    /// Shapes shared by the Core tests.
    /// </summary>
    public static class TestShapes
    {
        /// <summary>1×1.</summary>
        public static readonly BlockShape Single = new BlockShape(new[] { Vector2Int.zero });

        /// <summary>2×1, horizontal.</summary>
        public static readonly BlockShape Domino = new BlockShape(new[] { Vector2Int.zero, Vector2Int.right });

        /// <summary>1×2, vertical.</summary>
        public static readonly BlockShape VerticalDomino = new BlockShape(new[] { Vector2Int.zero, Vector2Int.up });

        /// <summary>2×2.</summary>
        public static readonly BlockShape Square = new BlockShape(new[] { Vector2Int.zero, Vector2Int.right, Vector2Int.up, Vector2Int.one });

        /// <summary>3×1, horizontal.</summary>
        public static readonly BlockShape Bar3 = new BlockShape(new[] { Vector2Int.zero, new Vector2Int(1, 0), new Vector2Int(2, 0) });

        /// <summary>
        /// L in a 2×2 box with the notch top-right:
        /// <code>
        /// X.
        /// XX
        /// </code>
        /// </summary>
        public static readonly BlockShape L = new BlockShape(new[] { Vector2Int.zero, Vector2Int.right, Vector2Int.up });
    }
}
