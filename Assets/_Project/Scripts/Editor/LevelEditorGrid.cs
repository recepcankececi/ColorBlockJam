using System.Collections.Generic;
using CBJ.Core;
using CBJ.Data;
using CBJ.Gameplay;
using UnityEditor;
using UnityEngine;

namespace CBJ.Editor
{
    /// <summary>
    /// Draws a level as the designer sees it — board cells, blocks in their colours with arrows on arrow blocks and
    /// hatching plus a layer count on ice blocks, doors in the edge slots — and maps mouse positions back to cells and
    /// slots. Board row 0 is drawn at the bottom, as in the game.
    /// </summary>
    public sealed class LevelEditorGrid
    {
        private const float MinCellPixels = 18f;
        private const float MaxCellPixels = 64f;
        private const float CellGap = 2f;
        private const float DoorThickness = 0.45f;
        private const float WallThickness = 0.12f;
        private const float OutlineWidth = 2f;
        private const float ArrowThickness = 0.14f;
        private const float ArrowHeadLength = 0.3f;
        private const float ArrowHeadWidth = 0.22f;
        private const float ArrowEndInset = 0.18f;
        private const int HatchLinesPerCell = 4;

        private static readonly Color BoardColor = new Color(0.17f, 0.17f, 0.20f);
        private static readonly Color CellColor = new Color(0.30f, 0.30f, 0.34f);
        private static readonly Color WallColor = new Color(0.42f, 0.42f, 0.46f);
        private static readonly Color ValidPreview = new Color(1f, 1f, 1f, 0.45f);
        private static readonly Color InvalidPreview = new Color(1f, 0.15f, 0.15f, 0.45f);
        private static readonly Color EraseHighlight = new Color(1f, 1f, 1f, 0.9f);
        private static readonly Color MissingColor = Color.magenta;
        private static readonly Color ArrowColor = new Color(1f, 1f, 1f, 0.95f);
        private static readonly Color HatchColor = new Color(0.85f, 0.95f, 1f, 0.8f);
        private static readonly Vector2Int[] SingleCell = { Vector2Int.zero };

        private readonly Vector3[] _triangle = new Vector3[3];
        private GUIStyle _layerStyle;

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
        /// Draws the level and the brush preview at <paramref name="hover"/> or along the active
        /// <paramref name="stroke"/>; <paramref name="erasing"/> shows what a click would remove instead.
        /// </summary>
        public void Draw(LevelData level, BlockPalette palette, LevelEditorBrush brush, GridHit hover, bool erasing, BlockStroke stroke)
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
                DrawKind(blocks[i]);
            }

            IReadOnlyList<DoorData> doors = level.Doors;
            for (int i = 0; i < doors.Count; i++)
            {
                DrawDoor(level, doors[i], ColorOf(palette, doors[i].ColorId));
            }

            DrawPreview(level, palette, brush, hover, erasing, stroke);
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
            IReadOnlyList<Vector2Int> cells = block.Cells;
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

        private void DrawKind(BlockData block)
        {
            BlockTraits traits = block.Traits;
            if (block.Cells.Count == 0 || traits.Kind == BlockKind.Normal)
            {
                return;
            }

            if (traits.Kind == BlockKind.Arrow)
            {
                ArrowPlacement.Find(new BlockShape(block.Cells), traits.ArrowAxis, out Vector2 center, out int length);
                DrawArrow(block.Origin + center, traits.ArrowAxis, length);
                return;
            }

            IReadOnlyList<Vector2Int> cells = block.Cells;
            for (int i = 0; i < cells.Count; i++)
            {
                DrawHatch(Inset(CellRect(block.Origin + cells[i])));
            }

            if (_layerStyle == null)
            {
                _layerStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
                _layerStyle.normal.textColor = Color.white;
            }

            _layerStyle.fontSize = Mathf.RoundToInt(_cell * 0.5f);
            GUI.Label(CellRect(block.Origin + cells[0]), traits.IceLayers.ToString(), _layerStyle);
        }

        // Double-headed like the in-game decal: a bar along the axis with a head at each end.
        private void DrawArrow(Vector2 cellCenter, Axis axis, int length)
        {
            Vector2 center = ToGui(cellCenter);
            Vector2 along = axis == Axis.Horizontal ? Vector2.right : Vector2.down;
            Vector2 across = new Vector2(-along.y, along.x);
            float half = (length * 0.5f - ArrowEndInset) * _cell;
            float head = ArrowHeadLength * _cell;
            float barHalf = Mathf.Max(0f, half - head);
            float thickness = ArrowThickness * _cell;
            Vector2 barSize = axis == Axis.Horizontal ? new Vector2(barHalf * 2f, thickness) : new Vector2(thickness, barHalf * 2f);
            EditorGUI.DrawRect(new Rect(center - barSize * 0.5f, barSize), ArrowColor);

            Handles.color = ArrowColor;
            for (int sign = -1; sign <= 1; sign += 2)
            {
                Vector2 tip = center + along * (half * sign);
                Vector2 baseCenter = tip - along * (head * sign);
                _triangle[0] = tip;
                _triangle[1] = baseCenter + across * (ArrowHeadWidth * _cell);
                _triangle[2] = baseCenter - across * (ArrowHeadWidth * _cell);
                Handles.DrawAAConvexPolygon(_triangle);
            }
        }

        // Diagonal lines across the cell, each clipped to it: points where (x - left) + (y - top) is constant.
        private static void DrawHatch(Rect rect)
        {
            float side = rect.width;
            float step = side * 2f / (HatchLinesPerCell + 1);
            Handles.color = HatchColor;
            for (int i = 1; i <= HatchLinesPerCell; i++)
            {
                float sum = step * i;
                var from = new Vector3(rect.x + Mathf.Min(sum, side), rect.y + Mathf.Max(0f, sum - side));
                var to = new Vector3(rect.x + Mathf.Max(0f, sum - side), rect.y + Mathf.Min(sum, side));
                Handles.DrawLine(from, to);
            }
        }

        private Vector2 ToGui(Vector2 cellPoint)
        {
            return new Vector2(_boardTopLeft.x + cellPoint.x * _cell, _boardTopLeft.y + (_height - cellPoint.y) * _cell);
        }

        // Doors are painted one cell at a time; the gap to a same-colour neighbour is bridged so they read as one opening.
        private void DrawDoor(LevelData level, DoorData door, Color color)
        {
            Rect rect = DoorRect(door);
            EditorGUI.DrawRect(rect, color);
            int next = LevelEditOperations.FindDoorAt(level, door.Edge, door.Offset + door.Length);
            if (next < 0 || level.Doors[next].ColorId != door.ColorId)
            {
                return;
            }

            Rect bridge = door.Edge.IsVertical()
                ? new Rect(rect.xMax, rect.y, CellGap * 2f, rect.height)
                : new Rect(rect.x, rect.y - CellGap * 2f, rect.width, CellGap * 2f);
            EditorGUI.DrawRect(bridge, color);
        }

        private void DrawPreview(LevelData level, BlockPalette palette, LevelEditorBrush brush, GridHit hover, bool erasing, BlockStroke stroke)
        {
            if (stroke.IsActive)
            {
                DrawStrokePreview(level, palette, brush, stroke);
                return;
            }

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
                IReadOnlyList<Vector2Int> cells = brush.HasPreset ? brush.PresetCells() : SingleCell;
                Vector2Int origin = LevelEditOperations.OriginForClick(cells, hover.Cell);
                bool fits = LevelEditOperations.CanPlaceBlock(level, cells, origin);
                Color tint = fits ? ColorOf(palette, brush.ColorId) * ValidPreview : InvalidPreview;
                for (int i = 0; i < cells.Count; i++)
                {
                    DrawPreviewCell(origin + cells[i], tint);
                }
            }
            else if (brush.Tool == LevelEditorTool.Door && hover.Kind == GridHitKind.DoorSlot)
            {
                var door = new DoorData(hover.Edge, hover.Slot, 1, brush.ColorId);
                EditorGUI.DrawRect(DoorRect(door), ColorOf(palette, brush.ColorId) * ValidPreview);
            }
        }

        // Stroke cells that will become the block are tinted; cells that are taken or do not connect show red.
        private void DrawStrokePreview(LevelData level, BlockPalette palette, LevelEditorBrush brush, BlockStroke stroke)
        {
            List<Vector2Int> kept = LevelEditOperations.StrokeBlockCells(level, stroke.Cells, stroke.ExtendIndex, out _);
            int colorId = stroke.ExtendIndex >= 0 ? level.Blocks[stroke.ExtendIndex].ColorId : brush.ColorId;
            Color tint = ColorOf(palette, colorId) * ValidPreview;
            IReadOnlyList<Vector2Int> cells = stroke.Cells;
            for (int i = 0; i < cells.Count; i++)
            {
                DrawPreviewCell(cells[i], kept.Contains(cells[i]) ? tint : InvalidPreview);
            }
        }

        private void DrawPreviewCell(Vector2Int cell, Color tint)
        {
            if (cell.x >= 0 && cell.y >= 0 && cell.x < _width && cell.y < _height)
            {
                EditorGUI.DrawRect(Inset(CellRect(cell)), tint);
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
                    IReadOnlyList<Vector2Int> cells = block.Cells;
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
                    DrawOutline(DoorRect(new DoorData(hover.Edge, hover.Slot, 1, level.Doors[index].ColorId)), EraseHighlight);
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
