using System.Collections.Generic;
using CBJ.Core;
using CBJ.Data;
using UnityEditor;
using UnityEngine;

namespace CBJ.Editor
{
    /// <summary>
    /// The level editor's tool panel: tool choice, colour swatches, block kind, freehand or preset stamp, and per-tool
    /// hints.
    /// </summary>
    public static class LevelEditorPalette
    {
        private const float SwatchSize = 32f;
        private const float ThumbnailSize = 44f;
        private const float ThumbnailPadding = 6f;
        private const float SelectionBorder = 3f;

        private const int KindColumns = 3;

        private static readonly string[] ToolLabels = { "Block", "Door", "Erase" };
        private static readonly string[] KindLabels = { "Normal", "Arrow H", "Arrow V", "Ice 1", "Ice 2", "Ice 3" };
        private static readonly Color SelectionColor = new Color(1f, 0.85f, 0.2f);
        private static readonly Color SelectedThumbnailBackground = new Color(0.25f, 0.25f, 0.28f);
        private static readonly int ThumbnailGridCells = LargestShapeSide();

        /// <summary>Draws the panel into the current layout area and updates <paramref name="brush"/>.</summary>
        public static void Draw(LevelEditorBrush brush, BlockPalette palette, float width)
        {
            EditorGUILayout.LabelField("Tool", EditorStyles.boldLabel);
            brush.Tool = (LevelEditorTool)GUILayout.Toolbar((int)brush.Tool, ToolLabels);

            if (brush.Tool == LevelEditorTool.Erase)
            {
                EditorGUILayout.HelpBox("Click or drag over blocks and door cells to remove them.", MessageType.None);
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Colour", EditorStyles.boldLabel);
            brush.ColorId = DrawSwatches(palette, brush.ColorId, width);

            EditorGUILayout.Space();
            if (brush.Tool == LevelEditorTool.Block)
            {
                EditorGUILayout.LabelField("Kind", EditorStyles.boldLabel);
                brush.Kind = (BrushKind)GUILayout.SelectionGrid((int)brush.Kind, KindLabels, KindColumns, EditorStyles.miniButton);
                EditorGUILayout.HelpBox(KindHint(brush.Kind), MessageType.None);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Shape", EditorStyles.boldLabel);
                if (GUILayout.Toggle(!brush.HasPreset, "Freehand", EditorStyles.miniButton) && brush.HasPreset)
                {
                    brush.PresetId = string.Empty;
                }

                DrawPresets(brush, palette, width);
                EditorGUILayout.HelpBox(brush.HasPreset
                        ? "Click to stamp the preset. R rotates it 90°."
                        : "Drag over cells to paint one block. Shift+drag from a block to add cells to it.",
                    MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox("Click or drag over the slots around the board to paint door cells. " +
                                        "Neighbouring cells of one colour form one door.", MessageType.None);
            }
        }

        private static string KindHint(BrushKind kind)
        {
            switch (kind)
            {
                case BrushKind.ArrowHorizontal: return "Arrow block: slides and exits only left or right.";
                case BrushKind.ArrowVertical: return "Arrow block: slides and exits only up or down.";
                case BrushKind.Normal: return "Normal block: slides in every direction.";
                default: return "Ice block: frozen until this many other blocks have left the board.";
            }
        }

        private static int DrawSwatches(BlockPalette palette, int selected, float width)
        {
            int perRow = Mathf.Max(1, Mathf.FloorToInt(width / SwatchSize));
            for (int start = 0; start < palette.Count; start += perRow)
            {
                EditorGUILayout.BeginHorizontal();
                for (int id = start; id < Mathf.Min(start + perRow, palette.Count); id++)
                {
                    PaletteEntry entry = palette.Get(id);
                    Rect rect = GUILayoutUtility.GetRect(SwatchSize, SwatchSize, GUILayout.Width(SwatchSize));
                    if (GUI.Button(rect, new GUIContent(string.Empty, entry.Name)))
                    {
                        selected = id;
                    }

                    if (id == selected)
                    {
                        EditorGUI.DrawRect(rect, SelectionColor);
                    }

                    EditorGUI.DrawRect(Shrink(rect, SelectionBorder), entry.Color);
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            return selected;
        }

        private static void DrawPresets(LevelEditorBrush brush, BlockPalette palette, float width)
        {
            IReadOnlyList<string> ids = ShapeLibrary.StampIds;
            Color color = LevelEditorGrid.ColorOf(palette, brush.ColorId);
            int perRow = Mathf.Max(1, Mathf.FloorToInt(width / ThumbnailSize));
            for (int start = 0; start < ids.Count; start += perRow)
            {
                EditorGUILayout.BeginHorizontal();
                for (int i = start; i < Mathf.Min(start + perRow, ids.Count); i++)
                {
                    Rect rect = GUILayoutUtility.GetRect(ThumbnailSize, ThumbnailSize, GUILayout.Width(ThumbnailSize));
                    if (GUI.Button(rect, new GUIContent(string.Empty, ids[i])) && ids[i] != brush.PresetId)
                    {
                        brush.PresetId = ids[i];
                        brush.QuarterTurns = 0;
                    }

                    bool selected = ids[i] == brush.PresetId;
                    if (selected)
                    {
                        EditorGUI.DrawRect(rect, SelectionColor);
                        EditorGUI.DrawRect(Shrink(rect, SelectionBorder), SelectedThumbnailBackground);
                    }

                    IReadOnlyList<Vector2Int> cells = selected ? brush.PresetCells() : ShapeLibrary.Get(ids[i]).Cells;
                    DrawThumbnail(Shrink(rect, ThumbnailPadding), cells, color);
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }

        // Every shape is drawn on the same cell scale, so sizes compare at a glance; row 0 at the bottom.
        private static void DrawThumbnail(Rect area, IReadOnlyList<Vector2Int> cells, Color color)
        {
            Vector2Int size = Vector2Int.zero;
            for (int i = 0; i < cells.Count; i++)
            {
                size = Vector2Int.Max(size, cells[i] + Vector2Int.one);
            }

            float cell = area.width / ThumbnailGridCells;
            float left = area.center.x - size.x * cell * 0.5f;
            float bottom = area.center.y + size.y * cell * 0.5f;
            for (int i = 0; i < cells.Count; i++)
            {
                var rect = new Rect(left + cells[i].x * cell, bottom - (cells[i].y + 1) * cell, cell, cell);
                EditorGUI.DrawRect(Shrink(rect, 1f), color);
            }
        }

        private static int LargestShapeSide()
        {
            int largest = 1;
            foreach (string id in ShapeLibrary.Ids)
            {
                BlockShape shape = ShapeLibrary.Get(id);
                largest = Mathf.Max(largest, Mathf.Max(shape.Width, shape.Height));
            }

            return largest;
        }

        private static Rect Shrink(Rect rect, float amount)
        {
            return new Rect(rect.x + amount, rect.y + amount, rect.width - amount * 2f, rect.height - amount * 2f);
        }
    }
}
