using System.Collections.Generic;
using CBJ.Core;
using CBJ.Editor;
using NUnit.Framework;
using UnityEngine;

namespace CBJ.Tests.EditMode.LevelEditor
{
    public class LevelMigrationTests
    {
        private const string LegacyYaml =
            "  _level:\n    _width: 4\n    _blocks:\n    - _shapeId: O2\n      _origin: {x: 0, y: 1}\n      _colorId: 4\n" +
            "    - _shapeId: L3-180\n      _origin: {x: 2, y: 2}\n      _colorId: 1\n";

        [Test]
        public void ReadLegacyShapeIds_UnityYaml_ReturnsIdsInBlockOrder()
        {
            Assert.That(LevelMigration.ReadLegacyShapeIds(LegacyYaml), Is.EqualTo(new[] { "O2", "L3-180" }));
        }

        [Test]
        public void ReadLegacyShapeIds_CurrentFormat_FindsNone()
        {
            var level = new LevelData(4, 4, 30);
            level.AddBlock(new BlockData(ShapeLibrary.Get("I1").Cells, Vector2Int.zero, 0));

            Assert.That(LevelMigration.ReadLegacyShapeIds(JsonUtility.ToJson(level)), Is.Empty);
        }

        [Test]
        public void Migrate_EveryLibraryId_GivesBlockTheShapeCellsAndKeepsOriginAndColour()
        {
            var mismatches = new List<string>();
            foreach (string id in ShapeLibrary.Ids)
            {
                LevelData level = LegacyLevel(id, new Vector2Int(1, 2), 3);

                bool migrated = LevelMigration.Migrate(level, LevelMigration.ReadLegacyShapeIds(LegacyJson(id, new Vector2Int(1, 2), 3)));

                BlockData block = level.Blocks[0];
                var expected = new BlockData(ShapeLibrary.Get(id).Cells, new Vector2Int(1, 2), 3);
                if (!migrated || !block.Equals(expected))
                {
                    mismatches.Add(id);
                }
            }

            Assert.That(mismatches, Is.Empty);
        }

        [Test]
        public void Migrate_MigratedLevel_BuildsABoard()
        {
            LevelData level = LegacyLevel("T4-90", Vector2Int.zero, 0);

            LevelMigration.Migrate(level, new[] { "T4-90" });

            Assert.DoesNotThrow(() => level.BuildBoard());
        }

        [Test]
        public void Migrate_UnknownId_LeavesLevelUnchanged()
        {
            LevelData level = LegacyLevel("I1", Vector2Int.zero, 0);

            bool migrated = LevelMigration.Migrate(level, new[] { "no-such-shape" });

            Assert.That((migrated, level.Blocks[0].Cells.Count), Is.EqualTo((false, 0)));
        }

        [Test]
        public void Migrate_IdCountDiffersFromBlocks_ReturnsFalse()
        {
            LevelData level = LegacyLevel("I1", Vector2Int.zero, 0);

            Assert.That(LevelMigration.Migrate(level, new[] { "I1", "O2" }), Is.False);
        }

        // Loading legacy text the way Unity does: the unknown _shapeId is dropped, origin and colour survive.
        private static LevelData LegacyLevel(string shapeId, Vector2Int origin, int colorId)
        {
            return JsonUtility.FromJson<LevelData>(LegacyJson(shapeId, origin, colorId));
        }

        private static string LegacyJson(string shapeId, Vector2Int origin, int colorId)
        {
            return "{\"_width\":6,\"_height\":6,\"_timerSeconds\":30,\"_doors\":[],\"_blocks\":[{\"_shapeId\":\"" + shapeId +
                   "\",\"_origin\":{\"x\":" + origin.x + ",\"y\":" + origin.y + "},\"_colorId\":" + colorId + "}]}";
        }
    }
}
