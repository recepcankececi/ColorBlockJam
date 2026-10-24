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
        [SerializeField] private string _shapeId = ShapeLibrary.Ids[0];
        [SerializeField] private int _colorId;
        [SerializeField] private int _doorLength = 1;

        /// <summary>Active tool.</summary>
        public LevelEditorTool Tool
        {
            get => _tool;
            set => _tool = value;
        }

        /// <summary>Shape the block tool places.</summary>
        public string ShapeId
        {
            get => _shapeId;
            set => _shapeId = value;
        }

        /// <summary>Palette colour of placed blocks and doors.</summary>
        public int ColorId
        {
            get => _colorId;
            set => _colorId = value;
        }

        /// <summary>Length in cells of placed doors.</summary>
        public int DoorLength
        {
            get => _doorLength;
            set => _doorLength = value;
        }
    }
}
