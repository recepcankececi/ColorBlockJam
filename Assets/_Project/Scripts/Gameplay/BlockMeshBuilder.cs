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
    /// the block material uses. The supplied block meshes must be imported readable.
    /// </summary>
    internal sealed class BlockMeshBuilder
    {
        private readonly Vector3[][] _pieceVertices;
        private readonly Vector3[][] _pieceNormals;
        private readonly Vector2[][] _pieceUvs;
        private readonly int[][] _pieceIndices;
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Vector3> _normals = new List<Vector3>();
        private readonly List<Vector2> _uvs = new List<Vector2>();
        private readonly List<int> _indices = new List<int>();

        public BlockMeshBuilder(BoardSkin skin)
        {
            int kinds = (int)BlockPartKind.InnerCorner + 1;
            _pieceVertices = new Vector3[kinds][];
            _pieceNormals = new Vector3[kinds][];
            _pieceUvs = new Vector2[kinds][];
            _pieceIndices = new int[kinds][];
            Copy(BlockPartKind.Center, skin.BlockCenter);
            Copy(BlockPartKind.Edge, skin.BlockEdge);
            Copy(BlockPartKind.OuterCorner, skin.BlockOuterCorner);
            Copy(BlockPartKind.InnerCorner, skin.BlockInnerCorner);
        }

        /// <summary>Starts a new block.</summary>
        public void Begin()
        {
            _vertices.Clear();
            _normals.Clear();
            _uvs.Clear();
            _indices.Clear();
        }

        /// <summary>Adds one piece placed at <paramref name="position"/> with <paramref name="rotation"/> in block space.</summary>
        public void Add(BlockPartKind kind, Vector3 position, Quaternion rotation)
        {
            int k = (int)kind;
            int first = _vertices.Count;
            Vector3[] vertices = _pieceVertices[k];
            Vector3[] normals = _pieceNormals[k];
            for (int i = 0; i < vertices.Length; i++)
            {
                _vertices.Add(position + rotation * vertices[i]);
                _normals.Add(rotation * normals[i]);
            }

            _uvs.AddRange(_pieceUvs[k]);
            int[] indices = _pieceIndices[k];
            for (int i = 0; i < indices.Length; i++)
            {
                _indices.Add(first + indices[i]);
            }
        }

        /// <summary>Replaces the contents of <paramref name="mesh"/> with the pieces added since <see cref="Begin"/>.</summary>
        public void WriteTo(Mesh mesh)
        {
            mesh.Clear();
            mesh.indexFormat = _vertices.Count > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(_vertices);
            mesh.SetNormals(_normals);
            mesh.SetUVs(0, _uvs);
            mesh.SetTriangles(_indices, 0);
        }

        private void Copy(BlockPartKind kind, Mesh piece)
        {
            int k = (int)kind;
            _pieceVertices[k] = piece.vertices;
            _pieceNormals[k] = piece.normals;
            _pieceUvs[k] = piece.uv;
            _pieceIndices[k] = piece.triangles;
        }
    }
}
