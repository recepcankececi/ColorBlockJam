using System.Linq;
using CBJ.Core;
using CBJ.Data;
using CBJ.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CBJ.Tests.EditMode.Gameplay
{
    /// <summary>
    /// Builds boards with the real skin, palette and config assets, so the tests also catch a broken asset wiring.
    /// </summary>
    public class BoardViewTests
    {
        private const string SettingsFolder = "Assets/_Project/Settings/";
        private const int Red = 0;
        private const int Blue = 1;
        private const int Width = 3;
        private const int Height = 2;
        private const float ColorTolerance = 1e-3f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private GameObject _root;
        private BoardView _view;
        private BlockPalette _palette;

        [SetUp]
        public void SetUp()
        {
            _palette = AssetDatabase.LoadAssetAtPath<BlockPalette>(SettingsFolder + "BlockPalette.asset");
            _root = new GameObject("BoardViewTest");
            _view = _root.AddComponent<BoardView>();
            _view.Initialize(
                AssetDatabase.LoadAssetAtPath<BoardSkin>(SettingsFolder + "BoardSkin.asset"),
                _palette,
                AssetDatabase.LoadAssetAtPath<GameConfig>(SettingsFolder + "GameConfig.asset"));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void Show_Board_LaysOneFloorTilePerCell()
        {
            _view.Show(SampleLevel().BuildBoard());

            Assert.That(ActiveRenderersUnder("Floor"), Is.EqualTo(Width * Height));
        }

        [Test]
        public void Show_DoorOnOneSlot_LeavesThatSlotWithoutWalls()
        {
            _view.Show(SampleLevel().BuildBoard());

            int wallSegments = 2 * 2 * (Width + Height) - 2;
            const int corners = 4;
            Assert.That(ActiveRenderersUnder("Walls"), Is.EqualTo(wallSegments + corners));
        }

        [Test]
        public void Show_Door_TintsItsFrameWithThePaletteColour()
        {
            _view.Show(SampleLevel().BuildBoard());

            MeshRenderer frame = _root.GetComponentsInChildren<MeshRenderer>().First(r => r.name == "DoorFrame");
            Assert.That(TintDistance(frame, _palette.Get(Blue).Color), Is.LessThan(ColorTolerance));
        }

        [Test]
        public void Show_Block_TintsEveryPieceWithThePaletteColour()
        {
            _view.Show(SampleLevel().BuildBoard());

            BlockView block = _root.GetComponentsInChildren<BlockView>().Single();
            Color red = _palette.Get(Red).Color;
            Assert.That(block.GetComponentsInChildren<MeshRenderer>().Max(r => TintDistance(r, red)), Is.LessThan(ColorTolerance));
        }

        [Test]
        public void Show_Block_UsesTheSharedMaterial()
        {
            _view.Show(SampleLevel().BuildBoard());

            BlockView block = _root.GetComponentsInChildren<BlockView>().Single();
            Material shared = AssetDatabase.LoadAssetAtPath<BoardSkin>(SettingsFolder + "BoardSkin.asset").BlockMaterial;
            Assert.That(block.GetComponentsInChildren<MeshRenderer>().Select(r => r.sharedMaterial).Distinct(), Is.EqualTo(new[] { shared }));
        }

        [Test]
        public void Show_SecondAttempt_ReusesThePooledPieces()
        {
            LevelData level = SampleLevel();
            _view.Show(level.BuildBoard());
            int created = _root.GetComponentsInChildren<MeshRenderer>(true).Length;

            _view.Show(level.BuildBoard());

            Assert.That(_root.GetComponentsInChildren<MeshRenderer>(true).Length, Is.EqualTo(created));
        }

        [Test]
        public void Clear_AfterShow_LeavesNothingActive()
        {
            _view.Show(SampleLevel().BuildBoard());

            _view.Clear();

            Assert.That(_root.GetComponentsInChildren<MeshRenderer>().Length, Is.Zero);
        }

        [Test]
        public void TryRaycast_StraightDownOnACell_ReturnsThatCell()
        {
            _view.Show(SampleLevel().BuildBoard());
            Vector3 above = _view.Layout.CellCenter(new Vector2Int(2, 1)) + Vector3.up * 10f;

            _view.TryRaycast(new Ray(above, Vector3.down), out Vector2 cellPoint);

            Assert.That(Vector2Int.FloorToInt(cellPoint), Is.EqualTo(new Vector2Int(2, 1)));
        }

        private int ActiveRenderersUnder(string group)
        {
            return _root.transform.Find(group).GetComponentsInChildren<MeshRenderer>().Length;
        }

        // The property block stores colours with a colour-space round trip, so compare within a tolerance.
        private static float TintDistance(Renderer renderer, Color expected)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            return Vector4.Distance(block.GetColor(BaseColorId), expected);
        }

        // 3×2 board, a red L block, a blue door on one top slot.
        private static LevelData SampleLevel()
        {
            var level = new LevelData(Width, Height, 30);
            level.AddDoor(new DoorData(Direction.Up, 1, 1, Blue));
            level.AddBlock(new BlockData("L3-0", Vector2Int.zero, Red));
            return level;
        }
    }
}
