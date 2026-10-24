using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace CBJ.Tests.EditMode.Project
{
    /// <summary>
    /// Enforces the module dependency direction Core ← Data ← Gameplay ← UI ← App, with Editor seeing
    /// Core, Data and Gameplay only. Reads the .asmdef files so the rule holds even for modules without code yet.
    /// </summary>
    public class AssemblyDependencyTests
    {
        private const string ScriptsRoot = "Assets/_Project/Scripts";
        private const string ProjectPrefix = "CBJ.";
        private const string GuidPrefix = "GUID:";
        private const string EditorModule = "CBJ.Editor";

        private static readonly string[] RuntimeLayers = { "CBJ.Core", "CBJ.Data", "CBJ.Gameplay", "CBJ.UI", "CBJ.App" };
        private static readonly string[] EditorAllowedReferences = { "CBJ.Core", "CBJ.Data", "CBJ.Gameplay" };

        private Dictionary<string, AsmdefFile> _modules;

        [OneTimeSetUp]
        public void LoadModules()
        {
            _modules = new Dictionary<string, AsmdefFile>();
            foreach (string guid in AssetDatabase.FindAssets("t:AssemblyDefinitionAsset", new[] { ScriptsRoot }))
            {
                AsmdefFile file = Load(AssetDatabase.GUIDToAssetPath(guid));
                _modules.Add(file.name, file);
            }
        }

        [Test]
        public void Modules_ScriptsFolder_ContainsExactlyTheExpectedAssemblies()
        {
            string[] expected = RuntimeLayers.Append(EditorModule).OrderBy(n => n).ToArray();

            Assert.That(_modules.Keys.OrderBy(n => n), Is.EqualTo(expected));
        }

        [Test]
        public void RuntimeModules_References_PointOnlyToLowerLayers()
        {
            var violations = new List<string>();
            for (int layer = 0; layer < RuntimeLayers.Length; layer++)
            {
                string module = RuntimeLayers[layer];
                foreach (string reference in ProjectReferences(_modules[module]))
                {
                    int target = Array.IndexOf(RuntimeLayers, reference);
                    if (target < 0 || target >= layer)
                    {
                        violations.Add($"{module} -> {reference}");
                    }
                }
            }

            Assert.That(violations, Is.Empty);
        }

        [Test]
        public void EditorModule_References_PointOnlyToCoreDataGameplay()
        {
            string[] references = ProjectReferences(_modules[EditorModule]).ToArray();

            Assert.That(references, Is.SubsetOf(EditorAllowedReferences));
        }

        [Test]
        public void EditorModule_Platforms_AreEditorOnly()
        {
            Assert.That(_modules[EditorModule].includePlatforms, Is.EqualTo(new[] { "Editor" }));
        }

        private static AsmdefFile Load(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(path);
            return JsonUtility.FromJson<AsmdefFile>(asset.text);
        }

        private static IEnumerable<string> ProjectReferences(AsmdefFile file)
        {
            return file.references
                .Select(ResolveName)
                .Where(name => name.StartsWith(ProjectPrefix, StringComparison.Ordinal));
        }

        // References are stored either by assembly name or as "GUID:<asmdef guid>", depending on how they were added.
        private static string ResolveName(string reference)
        {
            if (!reference.StartsWith(GuidPrefix, StringComparison.Ordinal))
            {
                return reference;
            }

            string path = AssetDatabase.GUIDToAssetPath(reference.Substring(GuidPrefix.Length));
            return Load(path).name;
        }
    }
}
