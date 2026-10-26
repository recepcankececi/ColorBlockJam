using System;
using System.Collections.Generic;
using CBJ.Core;
using UnityEngine;

namespace CBJ.Gameplay
{
    /// <summary>
    /// The drag of one held block, in plain C# so it runs in EditMode tests. The block moves freely in two dimensions:
    /// each frame it eases towards the finger, no faster than the speed cap, and that lead is applied along X, then
    /// along Y, each clamped by the nearest obstacle (the unit cells of every other block, and the wall cells around
    /// the board except those of a door of the block's colour whose span covers the block's side where it stands).
    /// Easing from the block's own position means a block freed from an obstacle catches up over several frames.
    /// The model origin is the rounded position: whenever it changes the model steps there cell by cell (ADR-002).
    /// Because the block never overlaps an obstacle, every cell its rounded footprint covers is free. A held block
    /// whose rounded position touches an edge it can leave through leaves at once only while it is in line with the
    /// door; otherwise it leaves when let go (<see cref="LetGo"/>).
    /// </summary>
    public sealed class BlockDrag
    {
        // Boxes closer than this count as touching, not overlapping, so float noise never wedges a block.
        private const float Contact = 1e-4f;
        private const float MinLead = 1e-5f;
        private const float HalfCell = 0.5f;
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
        private readonly float _maxSpeed;
        private readonly float _smoothTime;
        private readonly float _alignTolerance;
        private Vector2 _grabOffset;
        private Vector2 _velocity;
        private Direction _lastDirection;

        /// <summary>
        /// Creates a drag on <paramref name="session"/>. The block follows the finger at most
        /// <paramref name="maxSpeed"/> cells per second, easing in over about <paramref name="smoothTime"/> seconds
        /// (0 follows at once), and leaves while held only within <paramref name="alignTolerance"/> cells of its
        /// door's line.
        /// </summary>
        public BlockDrag(LevelSession session, float maxSpeed, float smoothTime, float alignTolerance)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            if (maxSpeed <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(maxSpeed));
            }

            _maxSpeed = maxSpeed;
            _smoothTime = Mathf.Max(0f, smoothTime);
            _alignTolerance = Mathf.Max(0f, alignTolerance);
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
            Grab(block, point, Vector2.zero);
        }

        /// <summary>
        /// Holds <paramref name="block"/> like <see cref="Grab(Block, Vector2)"/>, starting
        /// <paramref name="startOffset"/> cells away from its model cell, where its view still is (e.g. mid-settle).
        /// The offset is kept under half a cell per axis so the model cell stays the rounded position.
        /// </summary>
        public void Grab(Block block, Vector2 point, Vector2 startOffset)
        {
            Held = block ?? throw new ArgumentNullException(nameof(block));
            const float limit = HalfCell - Contact;
            Position = block.Origin + new Vector2(Mathf.Clamp(startOffset.x, -limit, limit),
                Mathf.Clamp(startOffset.y, -limit, limit));
            _grabOffset = point - Position;
            _velocity = Vector2.zero;
            _lastDirection = Direction.Right;
            CollectObstacles();
        }

        /// <summary>
        /// Lets go of the held block. If its model cell touches an edge it can leave through, it leaves now and the
        /// result is true; its view settles onto that cell before sliding out.
        /// </summary>
        public bool LetGo()
        {
            if (Held == null)
            {
                return false;
            }

            bool exited = FindExit(_session, Held, _lastDirection, out Direction exit) &&
                _session.TryStep(Held, exit) == StepResult.Exited;
            Release();
            return exited;
        }

        /// <summary>Lets go of the held block without trying to leave.</summary>
        public void Release()
        {
            Held = null;
            Position = Vector2.zero;
            _velocity = Vector2.zero;
            _obstacles.Clear();
            _walls.Clear();
            _wallEdges.Clear();
            _wallSlots.Clear();
        }

        /// <summary>
        /// Moves the held block, over <paramref name="deltaTime"/> seconds, towards the finger at the cell-space
        /// <paramref name="point"/>: eased and speed-capped, then along X and Y as far as obstacles allow, stepping the
        /// model whenever the rounded position changes. Releases the block when it leaves the board. Nothing moves
        /// while the attempt is not playing, an ice block never moves and an arrow block moves only along its axis.
        /// </summary>
        public void Drag(Vector2 point, float deltaTime)
        {
            if (Held == null || _session.State != LevelState.Playing || Held.IceLayers > 0 || deltaTime <= 0f)
            {
                return;
            }

            Vector2 goal = point - _grabOffset;
            if (Held.Kind == BlockKind.Arrow)
            {
                // Pin the locked axis so the easing never builds speed along it.
                goal = Held.ArrowAxis == Axis.Horizontal ? new Vector2(goal.x, Position.y) : new Vector2(Position.x, goal.y);
            }

            // Easing only approaches the finger; a lead this small is a finger at rest, so nothing is swept or probed.
            Vector2 lead = Follow(goal, deltaTime);
            if (lead.sqrMagnitude < MinLead * MinLead)
            {
                return;
            }

            if (lead.x != 0f)
            {
                float moved = SweepX(lead.x);
                Position += new Vector2(moved, 0f);
                if (moved != lead.x)
                {
                    StopAxis(false);
                }

                if (Commit(false))
                {
                    return;
                }
            }

            if (lead.y != 0f)
            {
                float moved = SweepY(lead.y);
                Position += new Vector2(0f, moved);
                if (moved != lead.y)
                {
                    StopAxis(true);
                }

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

        // The lead towards goal this frame. Easing starts from the block's own position and an axis an obstacle stopped
        // loses its velocity, so a block freed from an obstacle accelerates again instead of jumping to the finger.
        private Vector2 Follow(Vector2 goal, float deltaTime)
        {
            if (_smoothTime <= 0f)
            {
                return Vector2.ClampMagnitude(goal - Position, _maxSpeed * deltaTime);
            }

            Vector2 eased = Vector2.SmoothDamp(Position, goal, ref _velocity, _smoothTime, float.PositiveInfinity, deltaTime);
            _velocity = Vector2.ClampMagnitude(_velocity, _maxSpeed);
            return Vector2.ClampMagnitude(eased - Position, _maxSpeed * deltaTime);
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
                if (!IsAligned(direction) && _session.Probe(Held, direction) == StepResult.Exited)
                {
                    // Off the door's line the block waits just short of the next cell; it leaves once in line, or
                    // when let go. Stopping where it is, not snapping back, keeps a held block from shaking.
                    float sign = target > origin ? 1f : -1f;
                    float wait = origin + sign * (HalfCell - Contact);
                    Position = vertical ? new Vector2(Position.x, wait) : new Vector2(wait, Position.y);
                    StopAxis(vertical);
                    return false;
                }

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
                    StopAxis(vertical);
                    return false;
                }

                if (TryAutoExit())
                {
                    Release();
                    return true;
                }
            }
        }

        private void StopAxis(bool vertical)
        {
            _velocity = vertical ? new Vector2(_velocity.x, 0f) : new Vector2(0f, _velocity.y);
        }

        private bool TryAutoExit()
        {
            return FindExit(_session, Held, _lastDirection, out Direction exit) && IsAligned(exit) &&
                _session.TryStep(Held, exit) == StepResult.Exited;
        }

        // Whether the held block sits on its model cell across the exit axis, within the tolerance.
        private bool IsAligned(Direction exit)
        {
            float across = exit.IsVertical() ? Position.x - Held.Origin.x : Position.y - Held.Origin.y;
            return Mathf.Abs(across) <= _alignTolerance;
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
        // a shape whose arm would fit a narrower door stays outside, like the exit rule in MoveResolver. It also opens
        // only to a block in line with it (or already reaching into it), so an off-line block rests flush at the edge.
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
                if (door.Offset <= start + Contact && end - Contact <= door.Offset + door.Length &&
                    (IsAligned(door.Edge) || ReachesPast(door.Edge)))
                {
                    _openStart[(int)door.Edge] = door.Offset;
                    _openEnd[(int)door.Edge] = door.Offset + door.Length;
                }
            }
        }

        // Whether the held block's footprint already reaches past the board edge, into that edge's wall row.
        private bool ReachesPast(Direction edge)
        {
            BoardModel board = _session.Board;
            switch (edge)
            {
                case Direction.Down:
                    return Position.y < -Contact;
                case Direction.Up:
                    return Position.y + Held.Shape.Height > board.Height + Contact;
                case Direction.Left:
                    return Position.x < -Contact;
                default:
                    return Position.x + Held.Shape.Width > board.Width + Contact;
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
