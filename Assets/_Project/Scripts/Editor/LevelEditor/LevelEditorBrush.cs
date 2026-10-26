using System;
using CBJ.Core;
using UnityEngine;

namespace CBJ.Editor
{
    /// <summary>
    /// The level editor's current tool and its settings; kept by the window across domain reloads.
    /// </summary>
    [Serializable]
    public sealed class LevelEditorBrush
    {
        [SerializeField] private LevelEditorTool _tool = LevelEditorTool.Block;
        [SerializeField] private string _presetId = string.Empty;
        [SerializeField] private int _quarterTurns;
        [SerializeField] private int _colorId;
        [SerializeField] private BrushKind _kind;

        /// <summary>Active tool.</summary>
        public LevelEditorTool Tool
        {
            get => _tool;
            set => _tool = value;
        }

        /// <summary>Preset the block tool stamps, or empty when it paints freehand strokes.</summary>
        public string PresetId
        {
            get => _presetId;
            set => _presetId = value ?? string.Empty;
        }

        /// <summary>True when the block tool stamps a preset instead of painting a stroke.</summary>
        public bool HasPreset => ShapeLibrary.TryGet(_presetId, out _);

        /// <summary>Clockwise quarter turns applied to the preset.</summary>
        public int QuarterTurns
        {
            get => _quarterTurns;
            set => _quarterTurns = value;
        }

        /// <summary>Palette colour of placed blocks and doors.</summary>
        public int ColorId
        {
            get => _colorId;
            set => _colorId = value;
        }

        /// <summary>Kind of placed blocks.</summary>
        public BrushKind Kind
        {
            get => _kind;
            set => _kind = value;
        }

        /// <summary>The model traits of <see cref="Kind"/>.</summary>
        public BlockTraits Traits
        {
            get
            {
                switch (_kind)
                {
                    case BrushKind.ArrowHorizontal: return BlockTraits.Arrow(Axis.Horizontal);
                    case BrushKind.ArrowVertical: return BlockTraits.Arrow(Axis.Vertical);
                    case BrushKind.Ice1: return BlockTraits.Ice(1);
                    case BrushKind.Ice2: return BlockTraits.Ice(2);
                    case BrushKind.Ice3: return BlockTraits.Ice(3);
                    default: return BlockTraits.Normal;
                }
            }
        }

        /// <summary>Cells of the preset after rotation; only valid while <see cref="HasPreset"/>.</summary>
        public Vector2Int[] PresetCells()
        {
            return ShapeRotation.RotateClockwise(ShapeLibrary.Get(_presetId).Cells, _quarterTurns);
        }
    }
}
