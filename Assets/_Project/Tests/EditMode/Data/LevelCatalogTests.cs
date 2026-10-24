using System;
using CBJ.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CBJ.Tests.EditMode.Data
{
    public class LevelCatalogTests
    {
        private LevelCatalog _catalog;
        private LevelDefinition _first;
        private LevelDefinition _second;

        [SetUp]
        public void CreateCatalog()
        {
            _first = ScriptableObject.CreateInstance<LevelDefinition>();
            _second = ScriptableObject.CreateInstance<LevelDefinition>();
            _catalog = ScriptableObject.CreateInstance<LevelCatalog>();

            var serialized = new SerializedObject(_catalog);
            SerializedProperty levels = serialized.FindProperty("_levels");
            levels.arraySize = 2;
            levels.GetArrayElementAtIndex(0).objectReferenceValue = _first;
            levels.GetArrayElementAtIndex(1).objectReferenceValue = _second;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void DestroyCatalog()
        {
            Object.DestroyImmediate(_catalog);
            Object.DestroyImmediate(_first);
            Object.DestroyImmediate(_second);
        }

        [Test]
        public void Get_Index_ReturnsLevelInPlayOrder()
        {
            Assert.That(_catalog.Get(1), Is.SameAs(_second));
        }

        [Test]
        public void Get_IndexPastEnd_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _catalog.Get(2));
        }

        [Test]
        public void IndexOf_ListedLevel_ReturnsItsPosition()
        {
            Assert.That(_catalog.IndexOf(_second), Is.EqualTo(1));
        }

        [Test]
        public void IndexOf_UnlistedLevel_ReturnsMinusOne()
        {
            var other = ScriptableObject.CreateInstance<LevelDefinition>();

            int index = _catalog.IndexOf(other);

            Object.DestroyImmediate(other);
            Assert.That(index, Is.EqualTo(-1));
        }
    }
}
