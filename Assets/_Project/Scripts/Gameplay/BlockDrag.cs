using System;
using System.Collections.Generic;
using CBJ.Core;
using UnityEngine;

namespace CBJ.Gameplay
{
    /// <summary>
    /// The drag of one held block, in plain C# so it runs in EditMode tests. The block moves freely in two dimensions:
    /// each frame the finger's lead is applied along X, then along Y, each clamped by the nearest obstacle (the unit
    /// cells of every other block, and the wall cells around the board except those of a door of the block's colour
    /// whose span covers the block's side where it stands).
    /// The model origin is the rounded position: whenever it changes the model steps there cell by cell (ADR-002).
    /// Because the block never overlaps an obstacle, every cell its rounded footprint covers is free. A block whose
    /// rounded position touches an edge it can leave through leaves at once (auto-exit).
    /// </summary>
    public sealed class BlockDrag
    {
        // Boxes closer than this count as touching, not overlapping, so float noise never wedges a block.
        private const float Contact = 1e-4f;
        private const int InitialObstacleCapacity = 256;
        private const int Edges = 4;

        private static readonly Direction[] AllDirections = { Direction.Up, Direction.Down, Direction.Left, Direction.Right };

        private readonly LevelSession _session;
        private readonly List<Vector2Int> _obstacles = new List<Vector2Int>(InitialObstacleCapacity);
        private readonly List<Vector2Int> _walls = new List<Vector2Int>(InitialObstacleCapacity);
        private readonly List<Direction> _wallEdges = new List<Direction>(InitialObstacleCapacity);
        private readonly List<int> _wallSlots = new List<int>(InitialObstacleCapacity);

        // Per edge, indexed by Direction: the slots [start, end) of the door the block fits through right now.
        private readonly int[] _openStart = new int[Edges];
        private readonly int[] _openEnd = new int[Edges];
        private Vector2 _grabOffset;
        private Direction _lastDirection;

        /// <summary>Creates a drag on <paramref name="session"/>.</summary>
        public BlockDrag(LevelSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        /// <summary>The block being dragged; null when nothing is held or the held block has left the board.</summary>
        public Block Held { get; private set; }

        /// <summary>Continuous cell-space position of the held block's origin; its rounded value is the model origin.</summary>
        public Vector2 Position { get; private set; }

        /// <summary>Where the held block's view sits relative to its model cell, in cells; at most half a cell per axis.</summary>
        public Vector2 Offset => Held != null ? Position - Held.Origin : Vector2.zero;

        /// <summary>
        /// Holds <paramref name="block"/>, grabbed at the cell-space <paramref name="point"/>, and records the obstacles
        /// it can collide with; the other blocks do not move while it is held.
        /// </summary>
        public void Grab(Block block, Vector2 point)
        {
            Held = block ?? throw new ArgumentNullException(nameof(block));
            Position = block.Origin;
            _grabOffset = point - block.Origin;
            _lastDirection = Direction.Right;
            CollectObstacles();
        }

        /// <summary>Lets go of the held block.</summary>
        public void Release()
        {
            Held = null;
            Position = Vector2.zero;
            _obstacles.Clear();
            _walls.Clear();
            _wallEdges.Clear();
            _wallSlots.Clear();
        }

        /// <summary>
        /// Moves the held block towards the finger at the cell-space <paramref name="point"/>, along X then Y, as far
        /// as obstacles allow, stepping the model whenever the rounded position changes. Releases the block when it
        /// leaves the board. Nothing moves while the attempt is not playing, an ice block never moves and an arrow
        /// block moves only along its axis.
        /// </summary>
        public void Drag(Vector2 point)
        {
            if (Held == null || _session.State != LevelState.Playing || Held.IceLayers > 0)
            {
                return;
            }

            Vector2 lead = point - _grabOffset - Position;
            bool arrow = Held.Kind == BlockKind.Arrow;
            if ((!arrow || Held.ArrowAxis == Axis.Horizontal) && lead.x != 0f)
            {
                Position += new Vector2(SweepX(lead.x), 0f);
                if (Commit(false))
                {
                    return;
                }
            }

            if ((!arrow || Held.ArrowAxis == Axis.Vertical) && lead.y != 0f)
            {
                Position += new Vector2(0f, SweepY(lead.y));
                if (Commit(true))
                {
                    return;
                }
            }

            // A block resting by a door it can now leave through (its blocker has gone) goes on the first move.
            if (lead != Vector2.zero && TryAutoExit())
            {
                Release();
            }
        }

        /// <summary>
        /// The edge <paramref name="block"/> can leave through right now, trying <paramref name="preferred"/> first;
        /// false when it touches no edge with a door that lets it out.
        /// </summary>
        public static bool FindExit(LevelSession session, Block block, Direction preferred, out Direction exit)
        {
            exit = preferred;
            if (session.Probe(block, preferred) == StepResult.Exited)
            {
                return true;
            }

            for (int i = 0; i < AllDirections.Length; i++)
            {
                Direction direction = AllDirections[i];
                if (direction != preferred && session.Probe(block, direction) == StepResult.Exited)
                {
                    exit = direction;
                    return true;
                }
            }

            return false;
        }

        // Steps the model towards the rounded position along one axis. True when the block left the board.
        private bool Commit(bool vertical)
        {
            int target = vertical ? Round(Position.y) : Round(Position.x);
            while (true)
            {
                int origin = vertical ? Held.Origin.y : Held.Origin.x;
                if (origin == target)
                {
                    return false;
                }

                Direction direction = vertical
                    ? (target > origin ? Direction.Up : Direction.Down)
                    : (target > origin ? Direction.Right : Direction.Left);
                _lastDirection = direction;
                StepResult result = _session.TryStep(Held, direction);
                if (result == StepResult.Exited)
                {
                    Release();
                    return true;
                }

                if (result == StepResult.Blocked)
                {
                    // Cannot happen while the sweep keeps the block clear of obstacles; keep the model authoritative.
                    Position = vertical ? new Vector2(Position.x, origin) : new Vector2(origin, Position.y);
                    return false;
                }

                if (TryAutoExit())
                {
                    Release();
                    return true;
                }
            }
        }

        private bool TryAutoExit()
        {
            return FindExit(_session, Held, _lastDirection, out Direction exit) &&
                _session.TryStep(Held, exit) == StepResult.Exited;
        }

        // Signed distance along X the block can travel towards dx before touching an obstacle.
        private float SweepX(float dx)
        {
            UpdateOpenDoors();
            float limit = dx;
            IReadOnlyList<Vector2Int> cells = Held.Shape.Cells;
            for (int c = 0; c < cells.Count; c++)
            {
                float minX = Position.x + cells[c].x;
                float minY = Position.y + cells[c].y;
                for (int o = 0; o < _obstacles.Count; o++)
                {
                    limit = ClampX(limit, dx, _obstacles[o], minX, minY);
                }

                for (int w = 0; w < _walls.Count; w++)
                {
                    if (!IsOpen(w))
                    {
                        limit = ClampX(limit, dx, _walls[w], minX, minY);
                    }
                }
            }

            return limit;
        }

        private static float ClampX(float limit, float dx, Vector2Int obstacle, float minX, float minY)
        {
            if (!Overlaps(obstacle.y, minY))
            {
                return limit;
            }

            return dx > 0f
                ? ClampAhead(limit, obstacle.x - (minX + 1f))
                : ClampBehind(limit, obstacle.x + 1f - minX);
        }

        // Signed distance along Y the block can travel towards dy before touching an obstacle.
        private float SweepY(float dy)
        {
            UpdateOpenDoors();
            float limit = dy;
            IReadOnlyList<Vector2Int> cells = Held.Shape.Cells;
            for (int c = 0; c < cells.Count; c++)
            {
                float minX = Position.x + cells[c].x;
                float minY = Position.y + cells[c].y;
                for (int o = 0; o < _obstacles.Count; o++)
                {
                    limit = ClampY(limit, dy, _obstacles[o], minX, minY);
                }

                for (int w = 0; w < _walls.Count; w++)
                {
                    if (!IsOpen(w))
                    {
                        limit = ClampY(limit, dy, _walls[w], minX, minY);
                    }
                }
            }

            return limit;
        }

        private static float ClampY(float limit, float dy, Vector2Int obstacle, float minX, float minY)
        {
            if (!Overlaps(obstacle.x, minX))
            {
                return limit;
            }

            return dy > 0f
                ? ClampAhead(limit, obstacle.y - (minY + 1f))
                : ClampBehind(limit, obstacle.y + 1f - minY);
        }

        private bool IsOpen(int wall)
        {
            int edge = (int)_wallEdges[wall];
            int slot = _wallSlots[wall];
            return slot >= _openStart[edge] && slot < _openEnd[edge];
        }

        // A door opens its wall cells only while its span covers the block's side, measured where the block stands;
        // a shape whose arm would fit a narrower door stays outside, like the exit rule in MoveResolver.
        private void UpdateOpenDoors()
        {
            for (int edge = 0; edge < Edges; edge++)
            {
                _openStart[edge] = 0;
                _openEnd[edge] = 0;
            }

            IReadOnlyList<Door> doors = _session.Board.Doors;
            for (int i = 0; i < doors.Count; i++)
            {
                Door door = doors[i];
                if (door.ColorId != Held.ColorId)
                {
                    continue;
                }

                bool vertical = door.Edge.IsVertical();
                float start = vertical ? Position.x : Position.y;
                float end = start + (vertical ? Held.Shape.Width : Held.Shape.Height);
                if (door.Offset <= start + Contact && end - Contact <= door.Offset + door.Length)
                {
                    _openStart[(int)door.Edge] = door.Offset;
                    _openEnd[(int)door.Edge] = door.Offset + door.Length;
                }
            }
        }

        // A gap below -Contact means the obstacle lies behind the moving face, so it cannot stop the move.
        private static float ClampAhead(float limit, float gap)
        {
            return gap >= -Contact && gap < limit ? Mathf.Max(0f, gap) : limit;
        }

        private static float ClampBehind(float limit, float gap)
        {
            return gap <= Contact && gap > limit ? Mathf.Min(0f, gap) : limit;
        }

        // Whether a unit box starting at the integer obstacle coordinate overlaps one starting at min, on one axis.
        private static bool Overlaps(int obstacle, float min)
        {
            return obstacle < min + 1f - Contact && obstacle + 1f > min + Contact;
        }

        private static int Round(float value)
        {
            return Mathf.FloorToInt(value + 0.5f);
        }

        private void CollectObstacles()
        {
            _obstacles.Clear();
            BoardModel board = _session.Board;
            IReadOnlyList<Block> blocks = board.Blocks;
            for (int b = 0; b < blocks.Count; b++)
            {
                Block block = blocks[b];
                if (block == Held || block.IsExited)
                {
                    continue;
                }

                IReadOnlyList<Vector2Int> cells = block.Shape.Cells;
                for (int c = 0; c < cells.Count; c++)
                {
                    _obstacles.Add(block.Origin + cells[c]);
                }
            }

            // The ring of wall cells around the board, corners included (their slots lie outside every door).
            _walls.Clear();
            _wallEdges.Clear();
            _wallSlots.Clear();
            for (int x = -1; x <= board.Width; x++)
            {
                AddWall(new Vector2Int(x, -1), Direction.Down, x);
                AddWall(new Vector2Int(x, board.Height), Direction.Up, x);
            }

            for (int y = 0; y < board.Height; y++)
            {
                AddWall(new Vector2Int(-1, y), Direction.Left, y);
                AddWall(new Vector2Int(board.Width, y), Direction.Right, y);
            }
        }

        private void AddWall(Vector2Int cell, Direction edge, int slot)
        {
            _walls.Add(cell);
            _wallEdges.Add(edge);
            _wallSlots.Add(slot);
        }
    }
}
