using System;
using CBJ.Core;
using UnityEngine;

namespace CBJ.Gameplay
{
    /// <summary>
    /// Maps board cells to the board view's local space. The board lies in the XZ plane centred on the view's origin;
    /// board y (up) is local +Z. "Cell space" is board coordinates in cells, (0,0) being the bottom-left corner.
    /// </summary>
    public sealed class BoardLayout
    {
        /// <summary>World size of one cell. The supplied art builds a cell from 2×2 one-unit pieces.</summary>
        public const float CellSize = 2f;

        /// <summary>Thickness of the wall ring around the board: one supplied wall segment.</summary>
        public const float WallThickness = 1f;

        /// <summary>Creates the layout of a board of the given size in cells.</summary>
        public BoardLayout(int width, int height)
        {
            if (width < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be at least one cell.");
            }

            if (height < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be at least one cell.");
            }

            Width = width;
            Height = height;
            HalfWidth = width * CellSize * 0.5f;
            HalfDepth = height * CellSize * 0.5f;
        }

        /// <summary>Board width in cells.</summary>
        public int Width { get; }

        /// <summary>Board height in cells.</summary>
        public int Height { get; }

        /// <summary>Half the board's extent along local X, walls excluded.</summary>
        public float HalfWidth { get; }

        /// <summary>Half the board's extent along local Z, walls excluded.</summary>
        public float HalfDepth { get; }

        /// <summary>Local position of a cell-space point, on the floor.</summary>
        public Vector3 ToLocal(Vector2 cellPoint)
        {
            return new Vector3(cellPoint.x * CellSize - HalfWidth, 0f, cellPoint.y * CellSize - HalfDepth);
        }

        /// <summary>Cell-space point under a local position; the height is ignored.</summary>
        public Vector2 ToCellSpace(Vector3 local)
        {
            return new Vector2((local.x + HalfWidth) / CellSize, (local.z + HalfDepth) / CellSize);
        }

        /// <summary>Local position of a cell's centre, on the floor.</summary>
        public Vector3 CellCenter(Vector2Int cell)
        {
            return ToLocal(new Vector2(cell.x + 0.5f, cell.y + 0.5f));
        }

        /// <summary>
        /// Centre of the wall ring outside <paramref name="edge"/>, at <paramref name="along"/> cells along the edge
        /// (counted like <see cref="Door.Offset"/>: from the left, or from the bottom).
        /// </summary>
        public Vector3 EdgePoint(Direction edge, float along)
        {
            const float ring = WallThickness * 0.5f;
            switch (edge)
            {
                case Direction.Up: return new Vector3(along * CellSize - HalfWidth, 0f, HalfDepth + ring);
                case Direction.Down: return new Vector3(along * CellSize - HalfWidth, 0f, -HalfDepth - ring);
                case Direction.Left: return new Vector3(-HalfWidth - ring, 0f, along * CellSize - HalfDepth);
                case Direction.Right: return new Vector3(HalfWidth + ring, 0f, along * CellSize - HalfDepth);
                default: throw new ArgumentOutOfRangeException(nameof(edge), edge, null);
            }
        }

        /// <summary>Centre of the wall ring's corner square on the side given by the signs of x and z.</summary>
        public Vector3 CornerPoint(int signX, int signZ)
        {
            const float ring = WallThickness * 0.5f;
            return new Vector3(Math.Sign(signX) * (HalfWidth + ring), 0f, Math.Sign(signZ) * (HalfDepth + ring));
        }

        /// <summary>Quarter turns (clockwise seen from above) that point local +Z towards <paramref name="direction"/>.</summary>
        public static int QuarterTurns(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up: return 0;
                case Direction.Right: return 1;
                case Direction.Down: return 2;
                case Direction.Left: return 3;
                default: throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }
    }
}
