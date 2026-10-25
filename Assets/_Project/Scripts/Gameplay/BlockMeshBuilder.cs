using System.Collections.Generic;
using CBJ.Data;
using UnityEngine;
using UnityEngine.Rendering;

namespace CBJ.Gameplay
{
    /// <summary>
    /// Merges the modular pieces of one block into a single mesh, so a block is one renderer and one draw call however
    /// many cells it has. The piece geometry is copied once; the buffers are reused, so rebuilding allocates nothing
    /// once they have grown to the largest block. Only positions, normals and the first UV set are copied, which is all
    /// the block material uses. An arrow block's arrow goes into a second sub-mesh, so it keeps its own material on
    /// the same renderer. The supplied block and arrow meshes must be imported readable.
    /// </summary>
    internal sealed class BlockMeshBuilder
    {
        private const int PartPieces = (int)BlockPartKind.InnerCorner + 1;

        private readonly Vector3[][] _pieceVertices;
        private readonly Vector3[][] _pieceNormals;
        private readonly Vector2[][] _pieceUvs;
        private readonly int[][] _pieceIndices;
        private readonly int _arrowPieces;
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Vector3> _normals = new List<Vector3>();
        private readonly List<Vector2> _uvs = new List<Vector2>();
        private readonly List<int> _indices = new List<int>();
        private readonly List<int> _decalIndices = new List<int>();

        public BlockMeshBuilder(BoardSkin skin)
        {
            _arrowPieces = skin.BlockArrows.Count;
            int pieces = PartPieces + _arrowPieces;
            _pieceVertices = new Vector3[pieces][];
            _pieceNormals = new Vector3[pieces][];
            _pieceUvs = new Vector2[pieces][];
            _pieceIndices = new int[pieces][];
            Copy((int)BlockPartKind.Center, skin.BlockCenter);
            Copy((int)BlockPartKind.Edge, skin.BlockEdge);
            Copy((int)BlockPartKind.OuterCorner, skin.BlockOuterCorner);
            Copy((int)BlockPartKind.InnerCorner, skin.BlockInnerCorner);
            for (int i = 0; i < _arrowPieces; i++)
            {
                Copy(PartPieces + i, skin.BlockArrows[i]);
            }
        }

        /// <summary>True when the block being built has an arrow, so its mesh has a second sub-mesh.</summary>
        public bool HasDecal => _decalIndices.Count > 0;

        /// <summary>Starts a new block.</summary>
        public void Begin()
        {
            _vertices.Clear();
            _normals.Clear();
            _uvs.Clear();
            _indices.Clear();
            _decalIndices.Clear();
        }

        /// <summary>Adds one piece placed at <paramref name="position"/> with <paramref name="rotation"/> in block space.</summary>
        public void Add(BlockPartKind kind, Vector3 position, Quaternion rotation)
        {
            Append((int)kind, position, rotation, _indices);
        }

        /// <summary>
        /// Adds the arrow decal that best covers <paramref name="lengthCells"/> cells (the longest supplied arrow when
        /// none is that long) to the decal sub-mesh.
        /// </summary>
        public void AddArrow(int lengthCells, Vector3 position, Quaternion rotation)
        {
            if (_arrowPieces == 0)
            {
                Debug.LogError("BoardSkin has no arrow meshes; arrow blocks are drawn without their arrow.");
                return;
            }

            int arrow = Mathf.Clamp(lengthCells, 1, _arrowPieces) - 1;
            Append(PartPieces + arrow, position, rotation, _decalIndices);
        }

        /// <summary>Replaces the contents of <paramref name="mesh"/> with the pieces added since <see cref="Begin"/>.</summary>
        public void WriteTo(Mesh mesh)
        {
            mesh.Clear();
            mesh.indexFormat = _vertices.Count > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(_vertices);
            mesh.SetNormals(_normals);
            mesh.SetUVs(0, _uvs);
            mesh.subMeshCount = HasDecal ? 2 : 1;
            mesh.SetTriangles(_indices, 0);
            if (HasDecal)
            {
                mesh.SetTriangles(_decalIndices, 1);
            }
        }

        private void Append(int piece, Vector3 position, Quaternion rotation, List<int> target)
        {
            int first = _vertices.Count;
            Vector3[] vertices = _pieceVertices[piece];
            Vector3[] normals = _pieceNormals[piece];
            for (int i = 0; i < vertices.Length; i++)
            {
                _vertices.Add(position + rotation * vertices[i]);
                _normals.Add(rotation * normals[i]);
            }

            _uvs.AddRange(_pieceUvs[piece]);
            int[] indices = _pieceIndices[piece];
            for (int i = 0; i < indices.Length; i++)
            {
                target.Add(first + indices[i]);
            }
        }

        private void Copy(int piece, Mesh mesh)
        {
            _pieceVertices[piece] = mesh.vertices;
            _pieceNormals[piece] = mesh.normals;
            _pieceUvs[piece] = mesh.uv;
            _pieceIndices[piece] = mesh.triangles;
        }
    }
}
