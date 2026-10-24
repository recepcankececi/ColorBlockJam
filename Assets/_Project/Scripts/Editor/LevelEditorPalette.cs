using System.Collections.Generic;
using CBJ.Core;
using CBJ.Data;
using UnityEditor;
using UnityEngine;

namespace CBJ.Editor
{
    /// <summary>
    /// The level editor's tool panel: tool choice, colour swatches, shape thumbnails and door length.
    /// </summary>
    public static class LevelEditorPalette
    {
        private const float SwatchSize = 32f;
        private const float ThumbnailSize = 44f;
        private const float ThumbnailPadding = 6f;
        private const float SelectionBorder = 3f;

        private static readonly string[] ToolLabels = { "Block", "Door", "Erase" };
        private static readonly Color SelectionColor = new Color(1f, 0.85f, 0.2f);
        private static readonly Color SelectedThumbnailBackground = new Color(0.25f, 0.25f, 0.28f);
        private static readonly int ThumbnailGridCells = LargestShapeSide();

        /// <summary>Draws the panel into the current layout area and updates <paramref name="brush"/>.</summary>
        public static void Draw(LevelEditorBrush brush, BlockPalette palette, float width, int longestEdge)
        {
            EditorGUILayout.LabelField("Tool", EditorStyles.boldLabel);
            brush.Tool = (LevelEditorTool)GUILayout.Toolbar((int)brush.Tool, ToolLabels);

            if (brush.Tool == LevelEditorTool.Erase)
            {
                EditorGUILayout.HelpBox("Click a block or door to remove it.", MessageType.None);
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Colour", EditorStyles.boldLabel);
            brush.ColorId = DrawSwatches(palette, brush.ColorId, width);

            EditorGUILayout.Space();
            if (brush.Tool == LevelEditorTool.Block)
            {
                EditorGUILayout.LabelField("Shape", EditorStyles.boldLabel);
                brush.ShapeId = DrawShapes(brush.ShapeId, palette, brush.ColorId, width);
            }
            else
            {
                EditorGUILayout.LabelField("Door", EditorStyles.boldLabel);
                brush.DoorLength = EditorGUILayout.IntSlider("Length", brush.DoorLength, 1, longestEdge);
                EditorGUILayout.HelpBox("Click a slot around the board to place a door there.", MessageType.None);
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

        private static string DrawShapes(string selected, BlockPalette palette, int colorId, float width)
        {
            IReadOnlyList<string> ids = ShapeLibrary.Ids;
            Color color = LevelEditorGrid.ColorOf(palette, colorId);
            int perRow = Mathf.Max(1, Mathf.FloorToInt(width / ThumbnailSize));
            for (int start = 0; start < ids.Count; start += perRow)
            {
                EditorGUILayout.BeginHorizontal();
                for (int i = start; i < Mathf.Min(start + perRow, ids.Count); i++)
                {
                    Rect rect = GUILayoutUtility.GetRect(ThumbnailSize, ThumbnailSize, GUILayout.Width(ThumbnailSize));
                    if (GUI.Button(rect, new GUIContent(string.Empty, ids[i])))
                    {
                        selected = ids[i];
                    }

                    if (ids[i] == selected)
                    {
                        EditorGUI.DrawRect(rect, SelectionColor);
                        EditorGUI.DrawRect(Shrink(rect, SelectionBorder), SelectedThumbnailBackground);
                    }

                    DrawThumbnail(Shrink(rect, ThumbnailPadding), ShapeLibrary.Get(ids[i]), color);
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            return selected;
        }

        // Every shape is drawn on the same cell scale, so sizes compare at a glance; row 0 at the bottom.
        private static void DrawThumbnail(Rect area, BlockShape shape, Color color)
        {
            float cell = area.width / ThumbnailGridCells;
            float left = area.center.x - shape.Width * cell * 0.5f;
            float bottom = area.center.y + shape.Height * cell * 0.5f;
            IReadOnlyList<Vector2Int> cells = shape.Cells;
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
