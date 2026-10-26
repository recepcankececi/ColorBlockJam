using System.IO;
using CBJ.Data;
using UnityEditor;
using UnityEngine;

namespace CBJ.Editor
{
    /// <summary>
    /// Locates and creates the assets the level editor works on.
    /// </summary>
    public static class LevelEditorAssets
    {
        /// <summary>Folder that holds level assets and the catalog.</summary>
        public const string LevelsFolder = "Assets/_Project/Levels";

        /// <summary>Scene the game starts from.</summary>
        public const string BootScenePath = "Assets/_Project/Scenes/Boot.unity";

        private const string LevelsParentFolder = "Assets/_Project";
        private const string LevelsFolderName = "Levels";
        private const string CatalogPath = LevelsFolder + "/LevelCatalog.asset";
        private const string PalettePath = "Assets/_Project/Settings/BlockPalette.asset";
        private const string LevelFileFormat = LevelsFolder + "/Level_{0:00}.asset";

        private static readonly PaletteEntry[] DefaultPalette =
        {
            new PaletteEntry("Red", new Color(0.91f, 0.26f, 0.26f)),
            new PaletteEntry("Blue", new Color(0.22f, 0.49f, 0.93f)),
            new PaletteEntry("Green", new Color(0.30f, 0.78f, 0.36f)),
            new PaletteEntry("Yellow", new Color(0.98f, 0.80f, 0.20f)),
            new PaletteEntry("Purple", new Color(0.62f, 0.36f, 0.88f)),
            new PaletteEntry("Orange", new Color(0.98f, 0.55f, 0.18f)),
            new PaletteEntry("Pink", new Color(0.96f, 0.45f, 0.71f)),
            new PaletteEntry("Cyan", new Color(0.25f, 0.82f, 0.86f))
        };

        /// <summary>The first asset of type <typeparamref name="T"/> in the project, or null.</summary>
        public static T FindFirst<T>() where T : Object
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            return guids.Length == 0 ? null : AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        /// <summary>Creates an empty catalog in the levels folder.</summary>
        public static LevelCatalog CreateCatalog()
        {
            EnsureLevelsFolder();
            var catalog = ScriptableObject.CreateInstance<LevelCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            return catalog;
        }

        /// <summary>Creates a palette with the default eight colours.</summary>
        public static BlockPalette CreatePalette()
        {
            var palette = ScriptableObject.CreateInstance<BlockPalette>();
            var serialized = new SerializedObject(palette);
            SerializedProperty entries = serialized.FindProperty("_entries");
            entries.arraySize = DefaultPalette.Length;
            for (int i = 0; i < DefaultPalette.Length; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_name").stringValue = DefaultPalette[i].Name;
                entry.FindPropertyRelative("_color").colorValue = DefaultPalette[i].Color;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(palette, PalettePath);
            return palette;
        }

        /// <summary>
        /// Saves <paramref name="source"/>'s level as a new <c>Level_NN</c> asset and appends it to the catalog.
        /// </summary>
        public static LevelDefinition CreateLevel(LevelCatalog catalog, LevelDefinition source)
        {
            EnsureLevelsFolder();
            string path = NextFreeLevelPath(catalog.Count + 1);
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            CopyLevel(source, level);
            AssetDatabase.CreateAsset(level, path);
            AddToCatalog(catalog, level);
            return level;
        }

        /// <summary>Appends <paramref name="level"/> to the end of the play order and saves the catalog.</summary>
        public static void AddToCatalog(LevelCatalog catalog, LevelDefinition level)
        {
            var serialized = new SerializedObject(catalog);
            SerializedProperty levels = serialized.FindProperty("_levels");
            levels.arraySize++;
            levels.GetArrayElementAtIndex(levels.arraySize - 1).objectReferenceValue = level;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(catalog);
        }

        /// <summary>Overwrites <paramref name="target"/>'s level with <paramref name="source"/>'s and saves it.</summary>
        public static void SaveLevel(LevelDefinition source, LevelDefinition target)
        {
            CopyLevel(source, target);
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssetIfDirty(target);
        }

        /// <summary>Copies only the level data, so asset names and identities stay as they are.</summary>
        public static void CopyLevel(LevelDefinition source, LevelDefinition target)
        {
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(source.Level), target.Level);
        }

        // Names follow the play order where possible, so a new level lands at the end as Level_<count + 1>.
        private static string NextFreeLevelPath(int firstNumber)
        {
            for (int number = firstNumber; ; number++)
            {
                string path = string.Format(LevelFileFormat, number);
                if (!File.Exists(path))
                {
                    return path;
                }
            }
        }

        private static void EnsureLevelsFolder()
        {
            if (!AssetDatabase.IsValidFolder(LevelsFolder))
            {
                AssetDatabase.CreateFolder(LevelsParentFolder, LevelsFolderName);
            }
        }
    }
}
