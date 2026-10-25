using CBJ.Core;
using CBJ.Data;
using CBJ.Tests.EditMode.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CBJ.Tests.EditMode.Data
{
    public class LevelDefinitionTests
    {
        private LevelDefinition _source;
        private LevelDefinition _target;

        [SetUp]
        public void CreateAssets()
        {
            _source = ScriptableObject.CreateInstance<LevelDefinition>();
            _target = ScriptableObject.CreateInstance<LevelDefinition>();
        }

        [TearDown]
        public void DestroyAssets()
        {
            Object.DestroyImmediate(_source);
            Object.DestroyImmediate(_target);
        }

        [Test]
        public void NewInstance_Level_HasDefaultSizeAndTimer()
        {
            LevelData level = _source.Level;

            Assert.That((level.Width, level.Height, level.TimerSeconds),
                Is.EqualTo((LevelDefinition.DefaultWidth, LevelDefinition.DefaultHeight, LevelDefinition.DefaultTimerSeconds)));
        }

        [Test]
        public void UnitySerializerRoundTrip_EditedLevel_GameReadsTheSameData()
        {
            _source.Level.SetSize(4, LevelDefinition.DefaultHeight);
            _source.Level.AddDoor(new DoorData(Direction.Right, 2, 1, 3));
            _source.Level.AddBlock(new BlockData(TestShapes.Square.Cells, new Vector2Int(1, 1), 3));

            EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(_source), _target);

            Assert.That(JsonUtility.ToJson(_target.Level), Is.EqualTo(JsonUtility.ToJson(_source.Level)));
        }
    }
}
