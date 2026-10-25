using System;
using CBJ.Core;
using UnityEngine;

namespace CBJ.Gameplay
{
    /// <summary>
    /// The drag of one held block, in plain C# so it runs in EditMode tests. The finger's lead over the block's model
    /// cell is projected onto one axis and clamped to where the model lets the block go. Each time the lead passes
    /// half a cell the model steps one cell (ADR-002); after every step, a block that can leave through a door on an
    /// edge it touches leaves at once (auto-exit), so the player never has to push past the edge.
    /// </summary>
    public sealed class BlockDrag
    {
        /// <summary>Lead in cells at which the block's centre passes into the next cell and the model steps.</summary>
        public const float CommitLead = 0.5f;

        private const int MaxStepsPerDrag = 8;

        private static readonly Direction[] AllDirections = { Direction.Up, Direction.Down, Direction.Left, Direction.Right };

        private readonly LevelSession _session;
        private readonly float _axisLock;
        private Vector2 _grabOffset;
        private bool _hasAxis;
        private bool _vertical;

        /// <summary>
        /// Creates a drag on <paramref name="session"/>. The finger has to lead the block by
        /// <paramref name="axisLock"/> cells before an axis is chosen; the axis can change again only while the block
        /// sits within that distance of its cell.
        /// </summary>
        public BlockDrag(LevelSession session, float axisLock)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _axisLock = axisLock;
        }

        /// <summary>The block being dragged; null when nothing is held or the held block has left the board.</summary>
        public Block Held { get; private set; }

        /// <summary>
        /// Where the held block's view sits relative to its model cell, in cells: along one axis, never further than
        /// <see cref="CommitLead"/>, and zero towards anything the block cannot move into.
        /// </summary>
        public Vector2 Offset { get; private set; }

        /// <summary>Holds <paramref name="block"/>, grabbed at the cell-space <paramref name="point"/>.</summary>
        public void Grab(Block block, Vector2 point)
        {
            Held = block ?? throw new ArgumentNullException(nameof(block));
            _grabOffset = point - block.Origin;
            _hasAxis = false;
            Offset = Vector2.zero;
        }

        /// <summary>Lets go of the held block.</summary>
        public void Release()
        {
            Held = null;
            _hasAxis = false;
            Offset = Vector2.zero;
        }

        /// <summary>
        /// Follows the finger at the cell-space <paramref name="point"/>: steps the model while the lead passes half
        /// a cell, then updates <see cref="Offset"/>. Releases the block when it leaves the board.
        /// </summary>
        public void Drag(Vector2 point)
        {
            if (Held == null)
            {
                return;
            }

            Vector2 lead = point - _grabOffset - Held.Origin;
            float current = _vertical ? Offset.y : Offset.x;
            if (!_hasAxis || Mathf.Abs(current) <= _axisLock)
            {
                ChooseAxis(lead);
            }

            if (!_hasAxis)
            {
                return;
            }

            float along = _vertical ? lead.y : lead.x;
            for (int i = 0; i < MaxStepsPerDrag && Mathf.Abs(along) >= CommitLead; i++)
            {
                Direction direction = Toward(along);
                if (_session.Probe(Held, direction) == StepResult.Blocked)
                {
                    break;
                }

                if (_session.TryStep(Held, direction) == StepResult.Exited || TryAutoExit(direction))
                {
                    Release();
                    return;
                }

                along -= Mathf.Sign(along);
            }

            float max = _session.Probe(Held, Toward(1f)) == StepResult.Blocked ? 0f : CommitLead;
            float min = _session.Probe(Held, Toward(-1f)) == StepResult.Blocked ? 0f : -CommitLead;
            along = Mathf.Clamp(along, min, max);
            Offset = _vertical ? new Vector2(0f, along) : new Vector2(along, 0f);
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

        private bool TryAutoExit(Direction lastStep)
        {
            return FindExit(_session, Held, lastStep, out Direction exit) &&
                _session.TryStep(Held, exit) == StepResult.Exited;
        }

        // A diagonal lead tries the dominant axis; when that way is blocked the other axis takes over.
        private void ChooseAxis(Vector2 lead)
        {
            int axes = DragDirection.Resolve(lead, _axisLock, out Direction first, out Direction second);
            if (axes == 0)
            {
                return;
            }

            Direction chosen = axes == 2 && _session.Probe(Held, first) == StepResult.Blocked ? second : first;
            _vertical = chosen.IsVertical();
            _hasAxis = true;
        }

        private Direction Toward(float sign)
        {
            if (_vertical)
            {
                return sign > 0f ? Direction.Up : Direction.Down;
            }

            return sign > 0f ? Direction.Right : Direction.Left;
        }
    }
}
