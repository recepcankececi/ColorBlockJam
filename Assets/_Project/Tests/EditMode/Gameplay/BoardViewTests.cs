using System.Collections.Generic;
using System.Linq;
using CBJ.Core;
using CBJ.Data;
using CBJ.Gameplay;
using CBJ.Tests.EditMode.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

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
        private BoardSkin _skin;

        [SetUp]
        public void SetUp()
        {
            _palette = AssetDatabase.LoadAssetAtPath<BlockPalette>(SettingsFolder + "BlockPalette.asset");
            _skin = AssetDatabase.LoadAssetAtPath<BoardSkin>(SettingsFolder + "BoardSkin.asset");
            _root = new GameObject("BoardViewTest");
            _view = _root.AddComponent<BoardView>();
            _view.Initialize(
                _skin,
                _palette,
                AssetDatabase.LoadAssetAtPath<GameConfig>(SettingsFolder + "GameConfig.asset"));
        }

        [TearDown]
        public void TearDown()
        {
            // Block views own a runtime mesh, and OnDestroy does not run in edit mode.
            foreach (BlockView block in _root.GetComponentsInChildren<BlockView>(true))
            {
                Object.DestroyImmediate(block.GetComponent<MeshFilter>().sharedMesh);
            }

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
        public void Show_DoorOfThreeCells_DrawsOneFrameAndOneArrow()
        {
            _view.Show(WideDoorLevel().BuildBoard());

            string[] pieces = _root.transform.Find("Doors").GetComponentsInChildren<MeshRenderer>().Select(r => r.name).ToArray();
            Assert.That(pieces, Is.EquivalentTo(new[] { "DoorFrame", "DoorArrow" }));
        }

        [Test]
        public void Show_DoorOfThreeCells_StretchesTheFrameAcrossIt()
        {
            _view.Show(WideDoorLevel().BuildBoard());

            Transform frame = _root.GetComponentsInChildren<MeshRenderer>().First(r => r.name == "DoorFrame").transform;
            Assert.That(frame.localScale.x, Is.EqualTo(3f));
        }

        [Test]
        public void Lift_Block_CastsOneShadow()
        {
            BoardModel board = SampleLevel().BuildBoard();
            _view.Show(board);

            _view.Lift(board.Blocks[0]);

            Assert.That(ActiveRenderersUnder("Blocks", "BlockShadow"), Is.EqualTo(1));
        }

        [Test]
        public void Clear_WhileABlockIsLifted_PutsTheShadowAway()
        {
            BoardModel board = SampleLevel().BuildBoard();
            _view.Show(board);
            _view.Lift(board.Blocks[0]);

            _view.Clear();

            Assert.That(ActiveRenderersUnder("Blocks", "BlockShadow"), Is.Zero);
        }

        [Test]
        public void Show_Block_TintsItsBlockMaterialWithThePaletteColour()
        {
            _view.Show(SampleLevel().BuildBoard());

            BlockView block = _root.GetComponentsInChildren<BlockView>().Single();
            Assert.That(TintDistance(block.GetComponent<MeshRenderer>(), _palette.Get(Red).Color, 0), Is.LessThan(ColorTolerance));
        }

        [Test]
        public void Show_ArrowBlock_DrawsTheArrowAsASecondSubMeshWithItsOwnMaterial()
        {
            _view.Show(KindLevel(BlockTraits.Arrow(Axis.Horizontal)).BuildBoard());

            MeshRenderer block = _root.GetComponentsInChildren<BlockView>().Single().GetComponent<MeshRenderer>();
            Assert.That(
                (block.GetComponent<MeshFilter>().sharedMesh.subMeshCount, block.sharedMaterials[1]),
                Is.EqualTo((2, _skin.BlockArrowMaterial)));
        }

        [Test]
        public void Show_ArrowBlock_LeavesTheArrowUntinted()
        {
            _view.Show(KindLevel(BlockTraits.Arrow(Axis.Vertical)).BuildBoard());

            MeshRenderer block = _root.GetComponentsInChildren<BlockView>().Single().GetComponent<MeshRenderer>();
            Assert.That(HasTint(block, 1), Is.False);
        }

        [Test]
        public void Show_NormalBlock_HasOneSubMesh()
        {
            _view.Show(SampleLevel().BuildBoard());

            MeshFilter block = _root.GetComponentsInChildren<BlockView>().Single().GetComponent<MeshFilter>();
            Assert.That(block.sharedMesh.subMeshCount, Is.EqualTo(1));
        }

        [Test]
        public void Show_IceBlock_ShowsTheIceShell()
        {
            _view.Show(KindLevel(BlockTraits.Ice(1)).BuildBoard());

            Transform shell = _root.GetComponentsInChildren<BlockView>().Single().transform.Find("IceShell");
            Assert.That(shell.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void Show_NormalBlockFromAPooledIceView_HidesTheIceShell()
        {
            _view.Show(KindLevel(BlockTraits.Ice(1)).BuildBoard());

            _view.Show(SampleLevel().BuildBoard());

            Transform shell = _root.GetComponentsInChildren<BlockView>().Single().transform.Find("IceShell");
            Assert.That(shell.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void Show_PooledArrowViewReusedForNormalBlock_DropsTheArrowMaterial()
        {
            _view.Show(KindLevel(BlockTraits.Arrow(Axis.Horizontal)).BuildBoard());

            _view.Show(SampleLevel().BuildBoard());

            MeshRenderer block = _root.GetComponentsInChildren<BlockView>().Single().GetComponent<MeshRenderer>();
            Assert.That(block.sharedMaterials.Length, Is.EqualTo(1));
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
        public void Show_Block_DrawsWithOneRenderer()
        {
            _view.Show(SampleLevel().BuildBoard());

            BlockView block = _root.GetComponentsInChildren<BlockView>().Single();
            Assert.That(block.GetComponentsInChildren<MeshRenderer>().Length, Is.EqualTo(1));
        }

        [Test]
        public void Show_Block_MergesEveryPieceIntoItsMesh()
        {
            _view.Show(SampleLevel().BuildBoard());

            Mesh merged = _root.GetComponentInChildren<BlockView>().GetComponent<MeshFilter>().sharedMesh;
            Assert.That(merged.vertexCount, Is.EqualTo(PieceVertexCount(TestShapes.L)));
        }

        [Test]
        public void Show_BlockBeyondSixteenBitVertices_UsesThirtyTwoBitIndices()
        {
            const int side = 12;
            var cells = new List<Vector2Int>();
            for (int y = 0; y < side; y++)
            {
                for (int x = 0; x < side; x++)
                {
                    cells.Add(new Vector2Int(x, y));
                }
            }

            var level = new LevelData(side, side, 30);
            level.AddBlock(new BlockData(cells, Vector2Int.zero, Red));
            _view.Show(level.BuildBoard());

            Mesh merged = _root.GetComponentInChildren<BlockView>().GetComponent<MeshFilter>().sharedMesh;
            Assert.That(merged.vertexCount, Is.GreaterThan(ushort.MaxValue));
            Assert.That(merged.indexFormat, Is.EqualTo(IndexFormat.UInt32));
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

        private int PieceVertexCount(BlockShape shape)
        {
            var parts = new List<BlockPart>();
            BlockPartLayout.Build(shape, parts);
            Mesh[] pieces = { _skin.BlockCenter, _skin.BlockEdge, _skin.BlockOuterCorner, _skin.BlockInnerCorner };
            return parts.Sum(part => pieces[(int)part.Kind].vertexCount);
        }

        private int ActiveRenderersUnder(string group)
        {
            return _root.transform.Find(group).GetComponentsInChildren<MeshRenderer>().Length;
        }

        private int ActiveRenderersUnder(string group, string pieceName)
        {
            return _root.transform.Find(group).GetComponentsInChildren<MeshRenderer>().Count(r => r.name == pieceName);
        }

        // The property block stores colours with a colour-space round trip, so compare within a tolerance.
        private static float TintDistance(Renderer renderer, Color expected)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            return Vector4.Distance(block.GetColor(BaseColorId), expected);
        }

        // Blocks tint only their first material, so the arrow decal keeps its own colour.
        private static float TintDistance(Renderer renderer, Color expected, int materialIndex)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block, materialIndex);
            return Vector4.Distance(block.GetColor(BaseColorId), expected);
        }

        private static bool HasTint(Renderer renderer, int materialIndex)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block, materialIndex);
            return !block.isEmpty;
        }

        // 3×2 board, one red 3-cell bar of the given kind, a red door on the left edge.
        private static LevelData KindLevel(BlockTraits traits)
        {
            var level = new LevelData(Width, Height, 30);
            level.AddDoor(new DoorData(Direction.Left, 0, 1, Red));
            level.AddBlock(new BlockData(TestShapes.Bar3.Cells, Vector2Int.zero, Red, traits));
            return level;
        }

        // 4×2 board, a red L block, three one-cell blue doors side by side on the top edge (merged into one door).
        private static LevelData WideDoorLevel()
        {
            var level = new LevelData(4, Height, 30);
            for (int slot = 0; slot < 3; slot++)
            {
                level.AddDoor(new DoorData(Direction.Up, slot, 1, Blue));
            }

            level.AddBlock(new BlockData(TestShapes.L.Cells, Vector2Int.zero, Red));
            return level;
        }

        // 3×2 board, a red L block, a blue door on one top slot.
        private static LevelData SampleLevel()
        {
            var level = new LevelData(Width, Height, 30);
            level.AddDoor(new DoorData(Direction.Up, 1, 1, Blue));
            level.AddBlock(new BlockData(TestShapes.L.Cells, Vector2Int.zero, Red));
            return level;
        }
    }
}
