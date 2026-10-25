using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CBJ.Core;
using UnityEngine;

namespace CBJ.Editor
{
    /// <summary>
    /// Preset shapes for the level editor's stamps (ADR-007), addressed by a stable id. Level data stores cells, not
    /// these ids.
    /// </summary>
    public static class ShapeLibrary
    {
        private const char FilledCell = 'X';

        private static readonly Dictionary<string, BlockShape> Shapes = new Dictionary<string, BlockShape>();
        private static readonly List<string> OrderedIds = new List<string>();
        private static readonly List<string> OrderedStampIds = new List<string>();

        static ShapeLibrary()
        {
            Stamp("I1", "X");
            Stamp("I2-H", "XX");
            Define("I2-V", "X", "X");
            Stamp("I3-H", "XXX");
            Define("I3-V", "X", "X", "X");
            Stamp("O2", "XX", "XX");
            Stamp("L3-0", "X.", "XX");
            Define("L3-90", "XX", "X.");
            Define("L3-180", "XX", ".X");
            Define("L3-270", ".X", "XX");
            Stamp("L4-0", "X.", "X.", "XX");
            Define("L4-90", "XXX", "X..");
            Define("L4-180", "XX", ".X", ".X");
            Define("L4-270", "..X", "XXX");
            Stamp("T4-0", "XXX", ".X.");
            Define("T4-90", ".X", "XX", ".X");
            Define("T4-180", ".X.", "XXX");
            Define("T4-270", "X.", "XX", "X.");
            Ids = OrderedIds.AsReadOnly();
            StampIds = OrderedStampIds.AsReadOnly();
        }

        /// <summary>Every shape id, each rotation of a stamp included.</summary>
        public static ReadOnlyCollection<string> Ids { get; }

        /// <summary>One id per shape, in palette order; the stamp's R key gives the other orientations.</summary>
        public static ReadOnlyCollection<string> StampIds { get; }

        /// <summary>Finds the shape stored under <paramref name="id"/>.</summary>
        public static bool TryGet(string id, out BlockShape shape)
        {
            if (id == null)
            {
                shape = null;
                return false;
            }

            return Shapes.TryGetValue(id, out shape);
        }

        /// <summary>The shape stored under <paramref name="id"/>; throws for an unknown id.</summary>
        public static BlockShape Get(string id)
        {
            if (!TryGet(id, out BlockShape shape))
            {
                throw new ArgumentException($"Unknown shape id '{id}'.", nameof(id));
            }

            return shape;
        }

        private static void Stamp(string id, params string[] rows)
        {
            Define(id, rows);
            OrderedStampIds.Add(id);
        }

        // Rows are written top to bottom as they read on screen; board y grows up.
        private static void Define(string id, params string[] rows)
        {
            var cells = new List<Vector2Int>();
            for (int row = 0; row < rows.Length; row++)
            {
                int y = rows.Length - 1 - row;
                for (int x = 0; x < rows[row].Length; x++)
                {
                    if (rows[row][x] == FilledCell)
                    {
                        cells.Add(new Vector2Int(x, y));
                    }
                }
            }

            Shapes.Add(id, new BlockShape(cells));
            OrderedIds.Add(id);
        }
    }
}
