using System;
using System.Collections.Generic;
using CBJ.Core;
using CBJ.Data;
using UnityEngine;
using UnityEngine.Pool;

namespace CBJ.Gameplay
{
    /// <summary>
    /// Builds the board of one attempt from the supplied pieces (floor, wall ring, doors, blocks) and keeps the block
    /// views in step with the model through its events (ADR-001). Each block's pieces are merged into one mesh, so a
    /// block is one draw call. All pieces and views are pooled across attempts.
    /// </summary>
    public sealed class BoardView : MonoBehaviour
    {
        private const int ExitClearanceCells = 1;
        private const float QuarterTurnDegrees = 90f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        // The supplied block pieces are modelled lying in XY with their thickness along -Z.
        private static readonly Quaternion BlockPartUpright = Quaternion.Euler(90f, 0f, 0f);

        // The supplied wall, door and arrow pieces are modelled upside down: their capped side faces -Y.
        private static readonly Quaternion WallPieceUpright = Quaternion.Euler(0f, 0f, 180f);

        // Quadrant pieces have their pivot on a footprint corner; the inner corner is centred on its pivot.
        private static readonly Vector3 QuadrantPivotOffset = new Vector3(-0.5f, 0f, -0.5f);

        private readonly PieceSet _boardPieces = new PieceSet();
        private readonly List<BlockView> _blockViews = new List<BlockView>();
        private readonly List<DoorView> _doorViews = new List<DoorView>();
        private readonly Dictionary<Block, BlockView> _viewsByBlock = new Dictionary<Block, BlockView>();
        private readonly List<BlockPart> _partBuffer = new List<BlockPart>();

        private BlockPalette _palette;
        private GameConfig _config;
        private MaterialPropertyBlock _tint;
        private Transform _floorRoot;
        private Transform _wallRoot;
        private Transform _doorRoot;
        private Transform _blockRoot;
        private Transform _poolRoot;
        private BlockMeshBuilder _blockMesh;
        private Material _blockMaterial;
        private MeshPiecePool _floorPool;
        private MeshPiecePool _wallPool;
        private MeshPiecePool _cornerPool;
        private MeshPiecePool _doorPool;
        private MeshPiecePool _arrowPool;
        private ObjectPool<BlockView> _blockViewPool;
        private ObjectPool<DoorView> _doorViewPool;
        private Action<BlockView> _onExitFinished;
        private float _pickHeight;
        private float _boundsBottom;
        private float _boundsTop;
        private Vector3 _arrowOffset;
        private BoardModel _board;

        /// <summary>Cell-to-local mapping of the board on show; null before the first <see cref="Show"/>.</summary>
        public BoardLayout Layout { get; private set; }

        /// <summary>World-space box around the board and its walls, for framing the camera; valid after <see cref="Show"/>.</summary>
        public Bounds WorldBounds
        {
            get
            {
                float height = _boundsTop - _boundsBottom;
                Vector3 size = new Vector3(
                    2f * (Layout.HalfWidth + BoardLayout.WallThickness),
                    height,
                    2f * (Layout.HalfDepth + BoardLayout.WallThickness));
                return new Bounds(transform.TransformPoint(new Vector3(0f, _boundsBottom + height * 0.5f, 0f)), size);
            }
        }

        /// <summary>Sets up the pools; call once before <see cref="Show"/>.</summary>
        public void Initialize(BoardSkin skin, BlockPalette palette, GameConfig config)
        {
            _palette = palette;
            _config = config;
            _tint = new MaterialPropertyBlock();
            _onExitFinished = OnExitFinished;

            // Heights come from the supplied meshes, the wall pieces measured after their flip: blocks are picked on
            // their top face (-Z as modelled), a wall's cap stands just above the floor while its outer side runs far
            // below it, and arrows sit on top of the door frame.
            _pickHeight = -skin.BlockCenter.bounds.min.z;
            _boundsBottom = -skin.Wall.bounds.max.y;
            _boundsTop = Mathf.Max(-skin.Wall.bounds.min.y, _pickHeight);
            _arrowOffset = new Vector3(0f, skin.DoorArrow.bounds.max.y - skin.Door.bounds.min.y, 0f);

            _floorRoot = CreateGroup("Floor");
            _wallRoot = CreateGroup("Walls");
            _doorRoot = CreateGroup("Doors");
            _blockRoot = CreateGroup("Blocks");
            _poolRoot = CreateGroup("Pool");

            _blockMesh = new BlockMeshBuilder(skin);
            _blockMaterial = skin.BlockMaterial;
            _floorPool = new MeshPiecePool("FloorTile", skin.FloorTile, skin.FloorMaterial, _poolRoot);
            _wallPool = new MeshPiecePool("Wall", skin.Wall, skin.WallMaterial, _poolRoot);
            _cornerPool = new MeshPiecePool("WallCorner", skin.WallCorner, skin.WallMaterial, _poolRoot);
            _doorPool = new MeshPiecePool("DoorFrame", skin.Door, skin.DoorMaterial, _poolRoot);
            _arrowPool = new MeshPiecePool("DoorArrow", skin.DoorArrow, skin.ArrowMaterial, _poolRoot);

            _blockViewPool = new ObjectPool<BlockView>(CreateBlockView, actionOnRelease: OnBlockViewRelease);
            _doorViewPool = new ObjectPool<DoorView>(CreateDoorView, actionOnRelease: OnDoorViewRelease);
        }

        /// <summary>Replaces whatever is shown with <paramref name="board"/> and follows its events.</summary>
        public void Show(BoardModel board)
        {
            Clear();
            _board = board;
            Layout = new BoardLayout(board.Width, board.Height);

            BuildFloor();
            BuildWalls();
            for (int i = 0; i < board.Doors.Count; i++)
            {
                BuildDoor(board.Doors[i]);
            }

            for (int i = 0; i < board.Blocks.Count; i++)
            {
                if (!board.Blocks[i].IsExited)
                {
                    BuildBlock(board.Blocks[i]);
                }
            }

            Subscribe();
        }

        /// <summary>Returns every piece and view to the pools and stops following the board.</summary>
        public void Clear()
        {
            Unsubscribe();
            _board = null;
            for (int i = 0; i < _blockViews.Count; i++)
            {
                _blockViewPool.Release(_blockViews[i]);
            }

            for (int i = 0; i < _doorViews.Count; i++)
            {
                _doorViewPool.Release(_doorViews[i]);
            }

            _blockViews.Clear();
            _doorViews.Clear();
            _viewsByBlock.Clear();
            _boardPieces.ReleaseAll();
        }

        /// <summary>
        /// Cell-space point where <paramref name="ray"/> meets the plane of the block tops; false when it misses.
        /// The point may lie outside the board.
        /// </summary>
        public bool TryRaycast(Ray ray, out Vector2 cellPoint)
        {
            cellPoint = default;
            if (Layout == null)
            {
                return false;
            }

            var plane = new Plane(transform.up, transform.TransformPoint(new Vector3(0f, _pickHeight, 0f)));
            if (!plane.Raycast(ray, out float distance))
            {
                return false;
            }

            cellPoint = Layout.ToCellSpace(transform.InverseTransformPoint(ray.GetPoint(distance)));
            return true;
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_board == null || !isActiveAndEnabled)
            {
                return;
            }

            // Remove first so OnEnable after Show never subscribes twice.
            Unsubscribe();
            _board.BlockMoved += OnBlockMoved;
            _board.BlockExited += OnBlockExited;
        }

        private void Unsubscribe()
        {
            if (_board == null)
            {
                return;
            }

            _board.BlockMoved -= OnBlockMoved;
            _board.BlockExited -= OnBlockExited;
        }

        private void OnBlockMoved(Block block, Vector2Int from, Vector2Int to)
        {
            _viewsByBlock[block].MoveTo(Layout.ToLocal(to));
        }

        private void OnBlockExited(Block block, Door door)
        {
            BlockView view = _viewsByBlock[block];
            _viewsByBlock.Remove(block);
            int extent = door.Edge.IsVertical() ? block.Shape.Height : block.Shape.Width;
            Vector2 exit = (Vector2)(block.Origin + door.Edge.ToOffset() * (extent + ExitClearanceCells));
            view.Exit(Layout.ToLocal(exit), _onExitFinished);
        }

        private void OnExitFinished(BlockView view)
        {
            _blockViews.Remove(view);
            _blockViewPool.Release(view);
        }

        private void BuildFloor()
        {
            for (int y = 0; y < Layout.Height; y++)
            {
                for (int x = 0; x < Layout.Width; x++)
                {
                    _boardPieces.Add(_floorPool, _floorRoot, Layout.CellCenter(new Vector2Int(x, y)), Quaternion.identity, null);
                }
            }
        }

        private void BuildWalls()
        {
            BuildWallEdge(Direction.Up);
            BuildWallEdge(Direction.Down);
            BuildWallEdge(Direction.Left);
            BuildWallEdge(Direction.Right);

            // Unturned, the corner piece rounds the top-left corner.
            AddCorner(-1, 1, 0);
            AddCorner(1, 1, 1);
            AddCorner(1, -1, 2);
            AddCorner(-1, -1, 3);
        }

        private void BuildWallEdge(Direction edge)
        {
            const float firstHalf = 0.25f;
            const float secondHalf = 0.75f;
            Quaternion rotation = Turn(BoardLayout.QuarterTurns(edge)) * WallPieceUpright;
            int slots = _board.EdgeLength(edge);
            for (int slot = 0; slot < slots; slot++)
            {
                if (HasDoorAt(edge, slot))
                {
                    continue;
                }

                // A wall segment is half a cell long.
                _boardPieces.Add(_wallPool, _wallRoot, Layout.EdgePoint(edge, slot + firstHalf), rotation, null);
                _boardPieces.Add(_wallPool, _wallRoot, Layout.EdgePoint(edge, slot + secondHalf), rotation, null);
            }
        }

        private void AddCorner(int signX, int signZ, int quarterTurns)
        {
            _boardPieces.Add(_cornerPool, _wallRoot, Layout.CornerPoint(signX, signZ), Turn(quarterTurns) * WallPieceUpright, null);
        }

        private bool HasDoorAt(Direction edge, int slot)
        {
            IReadOnlyList<Door> doors = _board.Doors;
            for (int i = 0; i < doors.Count; i++)
            {
                Door door = doors[i];
                if (door.Edge == edge && slot >= door.Offset && slot < door.Offset + door.Length)
                {
                    return true;
                }
            }

            return false;
        }

        private void BuildDoor(Door door)
        {
            DoorView view = _doorViewPool.Get();
            view.transform.SetParent(_doorRoot, false);
            view.gameObject.SetActive(true);
            _doorViews.Add(view);

            SetTint(door.ColorId);
            Quaternion facing = Turn(BoardLayout.QuarterTurns(door.Edge)) * WallPieceUpright;
            for (int slot = door.Offset; slot < door.Offset + door.Length; slot++)
            {
                Vector3 position = Layout.EdgePoint(door.Edge, slot + 0.5f);
                view.Pieces.Add(_doorPool, view.transform, position, facing, _tint);
                view.Pieces.Add(_arrowPool, view.transform, position + _arrowOffset, facing, null);
            }
        }

        private void BuildBlock(Block block)
        {
            BlockView view = _blockViewPool.Get();
            view.transform.SetParent(_blockRoot, false);
            view.gameObject.SetActive(true);
            view.Show(Layout.ToLocal(block.Origin), _config.BlockSpeed);
            _blockViews.Add(view);
            _viewsByBlock.Add(block, view);

            BlockPartLayout.Build(block.Shape, _partBuffer);
            _blockMesh.Begin();
            for (int i = 0; i < _partBuffer.Count; i++)
            {
                BlockPart part = _partBuffer[i];
                Quaternion turn = Turn(part.QuarterTurns);
                Vector3 position = new Vector3(part.Center.x, 0f, part.Center.y) * BoardLayout.CellSize;
                if (part.Kind != BlockPartKind.InnerCorner)
                {
                    position += turn * QuadrantPivotOffset;
                }

                _blockMesh.Add(part.Kind, position, turn * BlockPartUpright);
            }

            _blockMesh.WriteTo(view.Mesh);
            SetTint(block.ColorId);
            view.Renderer.SetPropertyBlock(_tint);
        }

        private void SetTint(int colorId)
        {
            _tint.Clear();
            _tint.SetColor(BaseColorId, _palette.Get(colorId).Color);
        }

        private Transform CreateGroup(string groupName)
        {
            var group = new GameObject(groupName).transform;
            group.SetParent(transform, false);
            return group;
        }

        private BlockView CreateBlockView()
        {
            var view = CreatePooledView("Block").AddComponent<BlockView>();
            view.Initialize(_blockMaterial);
            return view;
        }

        private DoorView CreateDoorView()
        {
            return CreatePooledView("Door").AddComponent<DoorView>();
        }

        private GameObject CreatePooledView(string viewName)
        {
            var view = new GameObject(viewName);
            view.SetActive(false);
            view.transform.SetParent(_poolRoot, false);
            return view;
        }

        private void OnBlockViewRelease(BlockView view)
        {
            view.ResetForPool();
            view.gameObject.SetActive(false);
            view.transform.SetParent(_poolRoot, false);
        }

        private void OnDoorViewRelease(DoorView view)
        {
            view.ResetForPool();
            view.gameObject.SetActive(false);
            view.transform.SetParent(_poolRoot, false);
        }

        private static Quaternion Turn(int quarterTurns)
        {
            return Quaternion.Euler(0f, quarterTurns * QuarterTurnDegrees, 0f);
        }
    }
}
