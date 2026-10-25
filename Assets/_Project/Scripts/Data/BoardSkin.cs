using System.Collections.Generic;
using UnityEngine;

namespace CBJ.Data
{
    /// <summary>
    /// The supplied meshes and the shared materials the board is built from. Block and door colours come from the
    /// <see cref="BlockPalette"/> per renderer, so each material here is shared by every piece that uses it.
    /// </summary>
    [CreateAssetMenu(fileName = "BoardSkin", menuName = "Color Block Jam/Board Skin")]
    public sealed class BoardSkin : ScriptableObject
    {
        [Header("Block quadrants (BlockParts.fbx)")]
        [SerializeField] private Mesh _blockCenter;
        [SerializeField] private Mesh _blockEdge;
        [SerializeField] private Mesh _blockOuterCorner;
        [SerializeField] private Mesh _blockInnerCorner;

        [Header("Board (GroundGrid.fbx, WallAndDoor.fbx)")]
        [SerializeField] private Mesh _floorTile;
        [SerializeField] private Mesh _wall;
        [SerializeField] private Mesh _wallCorner;
        [SerializeField] private Mesh _door;
        [SerializeField] private Mesh _doorArrow;

        [Tooltip("Height scale of the wall, corner and door pieces. Their cap keeps its height above the floor; only the " +
            "outer face below it gets shorter, so the rim reads low from a tilted camera.")]
        [SerializeField, Range(0.1f, 1f)] private float _wallScaleY = 0.28f;

        [Header("Shared materials")]
        [SerializeField] private Material _blockMaterial;
        [SerializeField] private Material _floorMaterial;
        [SerializeField] private Material _wallMaterial;
        [SerializeField] private Material _doorMaterial;
        [SerializeField] private Material _arrowMaterial;

        [Header("Block kinds (Arrows.fbx, icetexture.jpg)")]
        [Tooltip("Arrow decals by length: one, two and three cells. Must be imported readable.")]
        [SerializeField] private Mesh[] _blockArrows = new Mesh[0];
        [SerializeField] private Material _blockArrowMaterial;
        [SerializeField] private Material _iceMaterial;

        [Header("Held block shadow")]
        [SerializeField] private Mesh _shadowMesh;
        [SerializeField] private Material _shadowMaterial;

        /// <summary>Block quadrant with no outer side.</summary>
        public Mesh BlockCenter => _blockCenter;

        /// <summary>Block quadrant with one outer side.</summary>
        public Mesh BlockEdge => _blockEdge;

        /// <summary>Block quadrant on a convex corner.</summary>
        public Mesh BlockOuterCorner => _blockOuterCorner;

        /// <summary>Three block quadrants around a concave corner.</summary>
        public Mesh BlockInnerCorner => _blockInnerCorner;

        /// <summary>Floor tile of one cell.</summary>
        public Mesh FloorTile => _floorTile;

        /// <summary>Wall segment of half a cell.</summary>
        public Mesh Wall => _wall;

        /// <summary>Wall piece on a board corner.</summary>
        public Mesh WallCorner => _wallCorner;

        /// <summary>Door frame of one cell.</summary>
        public Mesh Door => _door;

        /// <summary>Arrow drawn above a door, pointing out of the board.</summary>
        public Mesh DoorArrow => _doorArrow;

        /// <summary>Height scale of the wall, corner and door pieces; their cap stays at its modelled height.</summary>
        public float WallScaleY => _wallScaleY;

        /// <summary>Shared material of every block piece; tinted per renderer.</summary>
        public Material BlockMaterial => _blockMaterial;

        /// <summary>Material of the floor tiles.</summary>
        public Material FloorMaterial => _floorMaterial;

        /// <summary>Material of walls and corners.</summary>
        public Material WallMaterial => _wallMaterial;

        /// <summary>Shared material of door frames; tinted per renderer.</summary>
        public Material DoorMaterial => _doorMaterial;

        /// <summary>Material of door arrows.</summary>
        public Material ArrowMaterial => _arrowMaterial;

        /// <summary>
        /// Arrow decals of arrow blocks, index 0 one cell long, each longer by one cell; modelled like the block
        /// pieces (lying in XY, thickness along -Z) and pointing along +Y.
        /// </summary>
        public IReadOnlyList<Mesh> BlockArrows => _blockArrows;

        /// <summary>Shared material of the arrow decals; not tinted.</summary>
        public Material BlockArrowMaterial => _blockArrowMaterial;

        /// <summary>Shared translucent material of the shell around an ice block.</summary>
        public Material IceMaterial => _iceMaterial;

        /// <summary>Flat quad (facing -Z as modelled) drawn under a held block.</summary>
        public Mesh ShadowMesh => _shadowMesh;

        /// <summary>Shared translucent material of the held-block shadow.</summary>
        public Material ShadowMaterial => _shadowMaterial;
    }
}
