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
    /// block is one draw call; an arrow block's arrow rides in the same mesh. Drag input lifts, offsets and drops a
    /// block's view here; the view never changes the model. All pieces and views are pooled across attempts.
    /// </summary>
    public sealed class BoardView : MonoBehaviour
    {
        // Lifts the shadow and the block bottoms off the floor tiles so they never z-fight.
        private const float ShadowClearance = 0.02f;
        private const float BlockClearance = 0.02f;

        // How far the underside filler stays inside each outer side of a cell, clear of the pieces' rounded corners.
        private const float FillerInset = 0.3f;
        private const float QuarterTurnDegrees = 90f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        // The supplied block pieces are modelled lying in XY with their thickness along -Z.
        private static readonly Quaternion BlockPartUpright = Quaternion.Euler(90f, 0f, 0f);

        // The supplied wall, door and arrow pieces are modelled upside down: their capped side faces -Y.
        private static readonly Quaternion WallPieceUpright = Quaternion.Euler(0f, 0f, 180f);

        // The supplied quad faces -Z; turned, it lies on the floor facing up.
        private static readonly Quaternion ShadowFlat = Quaternion.Euler(90f, 0f, 0f);

        // Quadrant pieces have their pivot on a footprint corner; the inner corner is centred on its pivot.
        private static readonly Vector3 QuadrantPivotOffset = new Vector3(-0.5f, 0f, -0.5f);

        private readonly PieceSet _boardPieces = new PieceSet();
        private readonly List<BlockView> _blockViews = new List<BlockView>();
        private readonly List<DoorView> _doorViews = new List<DoorView>();
        private readonly Dictionary<Block, BlockView> _viewsByBlock = new Dictionary<Block, BlockView>();
        private readonly Dictionary<Door, DoorView> _viewsByDoor = new Dictionary<Door, DoorView>();
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
        private Material _arrowMaterial;
        private Material _iceMaterial;
        private MeshPiecePool _floorPool;
        private MeshPiecePool _wallPool;
        private MeshPiecePool _cornerPool;
        private MeshPiecePool _doorPool;
        private MeshPiecePool _arrowPool;
        private MeshPiecePool _shadowPool;
        private MeshRenderer _shadow;
        private Color _shadowColor;
        private BlockView _shadowOwner;
        private ObjectPool<BlockView> _blockViewPool;
        private ObjectPool<DoorView> _doorViewPool;
        private Action<BlockView> _onExitFinished;
        private Action<BlockView> _onDropped;
        private Vector3 _shadowOffset;
        private float _doorDepth;
        private float _blockRaise;
        private float _blockBottom;
        private float _pickHeight;
        private Vector3 _wallScale;
        private Vector3 _wallLift;
        private Vector3 _cornerLift;
        private Vector3 _doorLift;
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
            _onDropped = OnDropped;

            // Heights come from the supplied meshes, measured after the pieces are stood up. The block pieces reach
            // below their pivot, so the merged block is raised until its lowest point rests on the floor tiles; it is
            // picked on its top face (-Z as modelled). A wall's cap stands just above the floor while its outer side
            // runs far below it: the wall pieces are scaled down in height around their cap, which keeps its place.
            // Arrows sit on top of the door frame.
            float floorTop = skin.FloorTile.bounds.max.y;
            float blockLowest = -Mathf.Max(skin.BlockEdge.bounds.max.z,
                Mathf.Max(skin.BlockOuterCorner.bounds.max.z, skin.BlockInnerCorner.bounds.max.z));
            _blockRaise = floorTop + BlockClearance - blockLowest;
            _blockBottom = floorTop + BlockClearance;
            _pickHeight = -skin.BlockCenter.bounds.min.z + _blockRaise;
            float scaleY = skin.WallScaleY;
            _wallScale = new Vector3(1f, scaleY, 1f);
            _wallLift = CapLift(skin.Wall, scaleY);
            _cornerLift = CapLift(skin.WallCorner, scaleY);
            _doorLift = CapLift(skin.Door, scaleY);
            float wallCap = -skin.Wall.bounds.min.y;
            _boundsBottom = wallCap - skin.Wall.bounds.size.y * scaleY;
            _boundsTop = Mathf.Max(wallCap, _pickHeight);
            _arrowOffset = new Vector3(0f, skin.DoorArrow.bounds.max.y - skin.Door.bounds.min.y, 0f);
            _doorDepth = -skin.Door.bounds.min.y;
            _shadowOffset = new Vector3(0f, floorTop + ShadowClearance, 0f);
            _shadowColor = skin.ShadowMaterial.GetColor(BaseColorId);

            _floorRoot = CreateGroup("Floor");
            _wallRoot = CreateGroup("Walls");
            _doorRoot = CreateGroup("Doors");
            _blockRoot = CreateGroup("Blocks");
            _poolRoot = CreateGroup("Pool");

            _blockMesh = new BlockMeshBuilder(skin);
            _blockMaterial = skin.BlockMaterial;
            _arrowMaterial = skin.BlockArrowMaterial;
            _iceMaterial = skin.IceMaterial;
            _floorPool = new MeshPiecePool("FloorTile", skin.FloorTile, skin.FloorMaterial, _poolRoot);
            _wallPool = new MeshPiecePool("Wall", skin.Wall, skin.WallMaterial, _poolRoot);
            _cornerPool = new MeshPiecePool("WallCorner", skin.WallCorner, skin.WallMaterial, _poolRoot);
            _doorPool = new MeshPiecePool("DoorFrame", skin.Door, skin.DoorMaterial, _poolRoot);
            _arrowPool = new MeshPiecePool("DoorArrow", skin.DoorArrow, skin.ArrowMaterial, _poolRoot);
            _shadowPool = new MeshPiecePool("BlockShadow", skin.ShadowMesh, skin.ShadowMaterial, _poolRoot);

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
            ReleaseShadow();
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
            _viewsByDoor.Clear();
            _boardPieces.ReleaseAll();
        }

        /// <summary>Lifts the view of <paramref name="block"/> off the board and casts its shadow.</summary>
        public void Lift(Block block)
        {
            BlockView view = _viewsByBlock[block];
            ReleaseShadow();
            _shadow = _shadowPool.Get(_blockRoot, Vector3.zero, ShadowFlat);
            _shadowOwner = view;
            float spread = _config.ShadowSpread * BoardLayout.CellSize;
            var scale = new Vector3(block.Shape.Width * spread, block.Shape.Height * spread, 1f);
            view.Lift(_shadow, _shadowColor, scale, _shadowOffset, _onDropped);
        }

        /// <summary>Shows the held <paramref name="block"/> <paramref name="offsetCells"/> away from its model cell.</summary>
        public void Hold(Block block, Vector2 offsetCells)
        {
            _viewsByBlock[block].Hold(new Vector3(offsetCells.x, 0f, offsetCells.y) * BoardLayout.CellSize);
        }

        /// <summary>Lets the view of <paramref name="block"/> settle back onto its cell and drop.</summary>
        public void Drop(Block block)
        {
            if (_viewsByBlock.TryGetValue(block, out BlockView view))
            {
                view.Drop();
            }
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
            _board.BlockDestroyed += OnBlockDestroyed;
            _board.IceLayerRemoved += OnIceLayerRemoved;
        }

        private void Unsubscribe()
        {
            if (_board == null)
            {
                return;
            }

            _board.BlockMoved -= OnBlockMoved;
            _board.BlockExited -= OnBlockExited;
            _board.BlockDestroyed -= OnBlockDestroyed;
            _board.IceLayerRemoved -= OnIceLayerRemoved;
        }

        private void OnBlockMoved(Block block, Vector2Int from, Vector2Int to)
        {
            _viewsByBlock[block].MoveTo(Layout.ToLocal(to));
        }

        // The block keeps its shadow while the door swallows it; the shadow goes back to the pool when the exit ends.
        // The door sinks at once and stays down until the block is gone, a settle before the exit included.
        private void OnBlockExited(Block block, Door door)
        {
            BlockView view = _viewsByBlock[block];
            _viewsByBlock.Remove(block);
            Vector2Int step = door.Edge.ToOffset();
            float doorPlane = door.Edge.IsVertical() ? Layout.HalfDepth : Layout.HalfWidth;
            float seconds = view.Exit(new Vector3(step.x, 0f, step.y), doorPlane, _onExitFinished);
            _viewsByDoor[door].Open(_doorDepth, _config.DoorSinkDuration, seconds);
        }

        // A destroyed block shrinks away where it stands.
        private void OnBlockDestroyed(Block block)
        {
            BlockView view = _viewsByBlock[block];
            _viewsByBlock.Remove(block);
            if (_shadowOwner == view)
            {
                ReleaseShadow();
            }

            view.Vanish(_onExitFinished);
        }

        private void OnIceLayerRemoved(Block block, int remaining)
        {
            if (remaining == 0)
            {
                _viewsByBlock[block].Thaw();
            }
        }

        private void OnExitFinished(BlockView view)
        {
            if (_shadowOwner == view)
            {
                ReleaseShadow();
            }

            _blockViews.Remove(view);
            _blockViewPool.Release(view);
        }

        private void OnDropped(BlockView view)
        {
            if (_shadowOwner == view)
            {
                ReleaseShadow();
            }
        }

        private void ReleaseShadow()
        {
            if (_shadow == null)
            {
                return;
            }

            _shadowOwner.DetachShadow();
            _shadowPool.Release(_shadow);
            _shadow = null;
            _shadowOwner = null;
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
                _boardPieces.Add(_wallPool, _wallRoot, Layout.EdgePoint(edge, slot + firstHalf) + _wallLift, rotation,
                    _wallScale, null);
                _boardPieces.Add(_wallPool, _wallRoot, Layout.EdgePoint(edge, slot + secondHalf) + _wallLift, rotation,
                    _wallScale, null);
            }
        }

        private void AddCorner(int signX, int signZ, int quarterTurns)
        {
            _boardPieces.Add(_cornerPool, _wallRoot, Layout.CornerPoint(signX, signZ) + _cornerLift,
                Turn(quarterTurns) * WallPieceUpright, _wallScale, null);
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
            _viewsByDoor.Add(door, view);

            // One frame piece covers one cell along the edge; stretched, it spans the whole merged door.
            SetTint(door.ColorId);
            Quaternion facing = Turn(BoardLayout.QuarterTurns(door.Edge)) * WallPieceUpright;
            Vector3 center = Layout.EdgePoint(door.Edge, door.Offset + door.Length * 0.5f);
            view.Pieces.Add(_doorPool, view.transform, center + _doorLift, facing,
                new Vector3(door.Length, _wallScale.y, 1f), _tint);
            view.Pieces.Add(_arrowPool, view.transform, center + _arrowOffset, facing, null);
        }

        private void BuildBlock(Block block)
        {
            BlockView view = _blockViewPool.Get();
            view.transform.SetParent(_blockRoot, false);
            view.gameObject.SetActive(true);
            var pivot = new Vector3(block.Shape.Width, 0f, block.Shape.Height) * (BoardLayout.CellSize * 0.5f);
            view.Show(Layout.ToLocal(block.Origin), pivot);
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

                position.y += _blockRaise;
                _blockMesh.Add(part.Kind, position, turn * BlockPartUpright);
            }

            AddFillers(block.Shape);

            if (block.Kind == BlockKind.Arrow)
            {
                AddArrow(block);
            }

            _blockMesh.WriteTo(view.Mesh);
            view.SetKind(_blockMesh.HasDecal, block.IceLayers > 0);

            // Only the block material is tinted; the arrow keeps its own colour.
            SetTint(block.ColorId);
            view.Renderer.SetPropertyBlock(_tint, 0);
        }

        // One quad per cell closes the block's open underside, kept inside the outer sides where the pieces round off.
        private void AddFillers(BlockShape shape)
        {
            IReadOnlyList<Vector2Int> cells = shape.Cells;
            for (int i = 0; i < cells.Count; i++)
            {
                Vector2Int cell = cells[i];
                float left = HasCell(cells, cell + Vector2Int.left) ? 0f : FillerInset;
                float right = HasCell(cells, cell + Vector2Int.right) ? 0f : FillerInset;
                float down = HasCell(cells, cell + Vector2Int.down) ? 0f : FillerInset;
                float up = HasCell(cells, cell + Vector2Int.up) ? 0f : FillerInset;
                var min = new Vector3(cell.x * BoardLayout.CellSize + left, _blockBottom, cell.y * BoardLayout.CellSize + down);
                var max = new Vector3((cell.x + 1) * BoardLayout.CellSize - right, _blockBottom,
                    (cell.y + 1) * BoardLayout.CellSize - up);
                _blockMesh.AddFiller(min, max);
            }
        }

        private static bool HasCell(IReadOnlyList<Vector2Int> cells, Vector2Int cell)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i] == cell)
                {
                    return true;
                }
            }

            return false;
        }

        // Scaled in height around its pivot, a flipped wall piece would lower its cap; this lift puts the cap back.
        private static Vector3 CapLift(Mesh piece, float scaleY)
        {
            return new Vector3(0f, -piece.bounds.min.y * (1f - scaleY), 0f);
        }

        // The arrow sits on the block's top face, sunk in so only its relief shows; the supplied arrow points along
        // the board's vertical axis once stood up like the block pieces.
        private void AddArrow(Block block)
        {
            ArrowPlacement.Find(block.Shape, block.ArrowAxis, out Vector2 center, out int length);
            var position = new Vector3(center.x * BoardLayout.CellSize, _pickHeight - _config.ArrowSink, center.y * BoardLayout.CellSize);
            Quaternion rotation = block.ArrowAxis == Axis.Horizontal ? Turn(1) * BlockPartUpright : BlockPartUpright;
            _blockMesh.AddArrow(length, position, rotation);
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
            view.Initialize(_blockMaterial, _arrowMaterial, _iceMaterial, _config);
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
