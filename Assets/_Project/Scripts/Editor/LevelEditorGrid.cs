using System.Collections.Generic;
using CBJ.Core;
using CBJ.Data;
using UnityEditor;
using UnityEngine;

namespace CBJ.Editor
{
    /// <summary>
    /// Draws a level as the designer sees it — board cells, blocks in their colours, doors in the edge slots —
    /// and maps mouse positions back to cells and slots. Board row 0 is drawn at the bottom, as in the game.
    /// </summary>
    public sealed class LevelEditorGrid
    {
        private const float MinCellPixels = 18f;
        private const float MaxCellPixels = 64f;
        private const float CellGap = 2f;
        private const float DoorThickness = 0.45f;
        private const float WallThickness = 0.12f;
        private const float OutlineWidth = 2f;

        private static readonly Color BoardColor = new Color(0.17f, 0.17f, 0.20f);
        private static readonly Color CellColor = new Color(0.30f, 0.30f, 0.34f);
        private static readonly Color WallColor = new Color(0.42f, 0.42f, 0.46f);
        private static readonly Color ValidPreview = new Color(1f, 1f, 1f, 0.45f);
        private static readonly Color InvalidPreview = new Color(1f, 0.15f, 0.15f, 0.45f);
        private static readonly Color EraseHighlight = new Color(1f, 1f, 1f, 0.9f);
        private static readonly Color MissingColor = Color.magenta;

        private Vector2 _boardTopLeft;
        private float _cell;
        private int _width;
        private int _height;

        /// <summary>Fits a board of the given size, plus one slot ring for doors, centred in <paramref name="area"/>.</summary>
        public void Layout(Rect area, int width, int height)
        {
            _width = width;
            _height = height;
            float fit = Mathf.Min(area.width / (width + 2), area.height / (height + 2));
            _cell = Mathf.Clamp(Mathf.Floor(fit), MinCellPixels, MaxCellPixels);
            _boardTopLeft = new Vector2(
                Mathf.Floor(area.center.x - width * _cell * 0.5f),
                Mathf.Floor(area.center.y - height * _cell * 0.5f));
        }

        /// <summary>The cell or door slot under <paramref name="mouse"/>, using the last <see cref="Layout"/>.</summary>
        public GridHit HitTest(Vector2 mouse)
        {
            int column = Mathf.FloorToInt((mouse.x - _boardTopLeft.x) / _cell);
            int row = Mathf.FloorToInt((mouse.y - _boardTopLeft.y) / _cell);
            bool inColumns = column >= 0 && column < _width;
            bool inRows = row >= 0 && row < _height;

            if (inColumns && inRows)
            {
                return GridHit.AtCell(new Vector2Int(column, _height - 1 - row));
            }

            if (inColumns && row == -1)
            {
                return GridHit.AtSlot(Direction.Up, column);
            }

            if (inColumns && row == _height)
            {
                return GridHit.AtSlot(Direction.Down, column);
            }

            if (inRows && column == -1)
            {
                return GridHit.AtSlot(Direction.Left, _height - 1 - row);
            }

            if (inRows && column == _width)
            {
                return GridHit.AtSlot(Direction.Right, _height - 1 - row);
            }

            return GridHit.None;
        }

        /// <summary>
        /// Draws the level and the brush preview at <paramref name="hover"/>; <paramref name="erasing"/> shows what a
        /// click would remove instead.
        /// </summary>
        public void Draw(LevelData level, BlockPalette palette, LevelEditorBrush brush, GridHit hover, bool erasing)
        {
            EditorGUI.DrawRect(BoardRect(), BoardColor);
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    EditorGUI.DrawRect(Inset(CellRect(new Vector2Int(x, y))), CellColor);
                }
            }

            DrawWalls();

            IReadOnlyList<BlockData> blocks = level.Blocks;
            for (int i = 0; i < blocks.Count; i++)
            {
                DrawBlock(blocks[i], ColorOf(palette, blocks[i].ColorId));
            }

            IReadOnlyList<DoorData> doors = level.Doors;
            for (int i = 0; i < doors.Count; i++)
            {
                EditorGUI.DrawRect(DoorRect(doors[i]), ColorOf(palette, doors[i].ColorId));
            }

            DrawPreview(level, palette, brush, hover, erasing);
        }

        /// <summary>Palette colour for <paramref name="colorId"/>; magenta when the palette lacks it.</summary>
        public static Color ColorOf(BlockPalette palette, int colorId)
        {
            return colorId >= 0 && colorId < palette.Count ? palette.Get(colorId).Color : MissingColor;
        }

        private void DrawWalls()
        {
            EditorGUI.DrawRect(OutsideEdge(Direction.Up, 0, _width, WallThickness), WallColor);
            EditorGUI.DrawRect(OutsideEdge(Direction.Down, 0, _width, WallThickness), WallColor);
            EditorGUI.DrawRect(OutsideEdge(Direction.Left, 0, _height, WallThickness), WallColor);
            EditorGUI.DrawRect(OutsideEdge(Direction.Right, 0, _height, WallThickness), WallColor);
        }

        // Neighbouring cells of one block are bridged so each block reads as one piece, separate from its neighbours.
        private void DrawBlock(BlockData block, Color color)
        {
            IReadOnlyList<Vector2Int> cells = block.Shape.Cells;
            for (int i = 0; i < cells.Count; i++)
            {
                Vector2Int cell = block.Origin + cells[i];
                Rect rect = Inset(CellRect(cell));
                EditorGUI.DrawRect(rect, color);

                if (Contains(cells, cells[i] + Vector2Int.right))
                {
                    EditorGUI.DrawRect(new Rect(rect.xMax, rect.y, CellGap * 2f, rect.height), color);
                }

                if (Contains(cells, cells[i] + Vector2Int.up))
                {
                    EditorGUI.DrawRect(new Rect(rect.x, rect.y - CellGap * 2f, rect.width, CellGap * 2f), color);
                }
            }
        }

        private void DrawPreview(LevelData level, BlockPalette palette, LevelEditorBrush brush, GridHit hover, bool erasing)
        {
            if (hover.Kind == GridHitKind.None)
            {
                return;
            }

            if (erasing)
            {
                DrawEraseHighlight(level, hover);
            }
            else if (brush.Tool == LevelEditorTool.Block && hover.Kind == GridHitKind.Cell)
            {
                Vector2Int origin = LevelEditOperations.OriginForClick(brush.ShapeId, hover.Cell);
                bool fits = LevelEditOperations.CanPlaceBlock(level, brush.ShapeId, origin);
                Color tint = fits ? ColorOf(palette, brush.ColorId) * ValidPreview : InvalidPreview;
                IReadOnlyList<Vector2Int> cells = ShapeLibrary.Get(brush.ShapeId).Cells;
                for (int i = 0; i < cells.Count; i++)
                {
                    Vector2Int cell = origin + cells[i];
                    if (cell.x >= 0 && cell.y >= 0 && cell.x < _width && cell.y < _height)
                    {
                        EditorGUI.DrawRect(Inset(CellRect(cell)), tint);
                    }
                }
            }
            else if (brush.Tool == LevelEditorTool.Door && hover.Kind == GridHitKind.DoorSlot)
            {
                DoorData door = LevelEditOperations.FitDoor(level, hover.Edge, hover.Slot, brush.DoorLength, brush.ColorId);
                EditorGUI.DrawRect(DoorRect(door), ColorOf(palette, brush.ColorId) * ValidPreview);
            }
        }

        private void DrawEraseHighlight(LevelData level, GridHit hover)
        {
            if (hover.Kind == GridHitKind.Cell)
            {
                int index = LevelEditOperations.FindBlockAt(level, hover.Cell);
                if (index >= 0)
                {
                    BlockData block = level.Blocks[index];
                    IReadOnlyList<Vector2Int> cells = block.Shape.Cells;
                    for (int i = 0; i < cells.Count; i++)
                    {
                        DrawOutline(Inset(CellRect(block.Origin + cells[i])), EraseHighlight);
                    }
                }
            }
            else
            {
                int index = LevelEditOperations.FindDoorAt(level, hover.Edge, hover.Slot);
                if (index >= 0)
                {
                    DrawOutline(DoorRect(level.Doors[index]), EraseHighlight);
                }
            }
        }

        private Rect BoardRect()
        {
            return new Rect(_boardTopLeft.x, _boardTopLeft.y, _width * _cell, _height * _cell);
        }

        private Rect CellRect(Vector2Int cell)
        {
            return new Rect(_boardTopLeft.x + cell.x * _cell, _boardTopLeft.y + (_height - 1 - cell.y) * _cell, _cell, _cell);
        }

        private Rect DoorRect(DoorData door)
        {
            return OutsideEdge(door.Edge, door.Offset, door.Length, DoorThickness);
        }

        // A strip just outside the board along an edge; thickness is a fraction of a cell.
        private Rect OutsideEdge(Direction edge, int offset, int length, float thickness)
        {
            float depth = _cell * thickness;
            float span = length * _cell - CellGap * 2f;
            Rect board = BoardRect();
            switch (edge)
            {
                case Direction.Up:
                    return new Rect(board.x + offset * _cell + CellGap, board.y - depth, span, depth);
                case Direction.Down:
                    return new Rect(board.x + offset * _cell + CellGap, board.yMax, span, depth);
                case Direction.Left:
                    return new Rect(board.x - depth, board.yMax - (offset + length) * _cell + CellGap, depth, span);
                default:
                    return new Rect(board.xMax, board.yMax - (offset + length) * _cell + CellGap, depth, span);
            }
        }

        private static Rect Inset(Rect rect)
        {
            return new Rect(rect.x + CellGap, rect.y + CellGap, rect.width - CellGap * 2f, rect.height - CellGap * 2f);
        }

        private static void DrawOutline(Rect rect, Color color)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, OutlineWidth), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - OutlineWidth, rect.width, OutlineWidth), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, OutlineWidth, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - OutlineWidth, rect.y, OutlineWidth, rect.height), color);
        }

        private static bool Contains(IReadOnlyList<Vector2Int> cells, Vector2Int cell)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i] == cell)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
