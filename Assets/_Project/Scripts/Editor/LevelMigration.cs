using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using CBJ.Core;
using CBJ.Data;
using UnityEditor;
using UnityEngine;

namespace CBJ.Editor
{
    /// <summary>
    /// Converts levels saved before ADR-007, whose blocks named a <see cref="ShapeLibrary"/> id, to the cell format.
    /// Unity drops the unknown <c>_shapeId</c> field when it loads such an asset, so the ids are read from the asset
    /// file as text; the converted level is written back through the serializer.
    /// </summary>
    public static class LevelMigration
    {
        private const string MenuPath = "Tools/Color Block Jam/Migrate Levels To Cells";

        // Matches the YAML form (`- _shapeId: O2`) and the JSON form (`"_shapeId":"O2"`).
        private static readonly Regex LegacyShapeId = new Regex("_shapeId\"?\\s*:\\s*\"?([^\"\\s,}]+)");

        /// <summary>Shape ids of the blocks in a legacy level text, in block order.</summary>
        public static List<string> ReadLegacyShapeIds(string levelText)
        {
            var ids = new List<string>();
            foreach (Match match in LegacyShapeId.Matches(levelText))
            {
                ids.Add(match.Groups[1].Value);
            }

            return ids;
        }

        /// <summary>
        /// Gives each block of <paramref name="level"/> the cells of its legacy shape id, keeping origin and colour.
        /// Returns false and leaves the level unchanged when the ids do not match the blocks or one is unknown.
        /// </summary>
        public static bool Migrate(LevelData level, IReadOnlyList<string> shapeIds)
        {
            if (shapeIds.Count != level.Blocks.Count)
            {
                return false;
            }

            var shapes = new BlockShape[shapeIds.Count];
            for (int i = 0; i < shapes.Length; i++)
            {
                if (!ShapeLibrary.TryGet(shapeIds[i], out shapes[i]))
                {
                    return false;
                }
            }

            for (int i = 0; i < shapes.Length; i++)
            {
                BlockData block = level.Blocks[i];
                level.ReplaceBlockAt(i, new BlockData(shapes[i].Cells, block.Origin, block.ColorId));
            }

            return true;
        }

        /// <summary>Migrates every level asset that still stores shape ids and reports the result in the console.</summary>
        [MenuItem(MenuPath)]
        public static void MigrateAllLevels()
        {
            int migrated = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(LevelDefinition)))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                List<string> ids = ReadLegacyShapeIds(File.ReadAllText(path));
                var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
                if (ids.Count == 0)
                {
                    // Re-saved before migration: the ids are gone, so the loss is reported instead of hidden.
                    if (HasEmptyBlock(level.Level))
                    {
                        Debug.LogError($"{path} has blocks without cells and no shape ids left to migrate; repaint them.", level);
                    }

                    continue;
                }

                if (!Migrate(level.Level, ids))
                {
                    Debug.LogError($"Could not migrate {path}: its shape ids do not match its blocks.", level);
                    continue;
                }

                EditorUtility.SetDirty(level);
                AssetDatabase.SaveAssetIfDirty(level);
                migrated++;
            }

            Debug.Log($"Level migration: {migrated} level(s) converted to cells.");
        }

        private static bool HasEmptyBlock(LevelData level)
        {
            for (int i = 0; i < level.Blocks.Count; i++)
            {
                if (level.Blocks[i].Cells.Count == 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
