using UnityEngine;
using UnityEngine.Pool;

namespace CBJ.Gameplay
{
    /// <summary>
    /// Pool of renderers that all draw one mesh with one shared material. A released piece is deactivated, loses its
    /// per-renderer tint and goes back under the pool root.
    /// </summary>
    internal sealed class MeshPiecePool
    {
        private readonly string _name;
        private readonly Mesh _mesh;
        private readonly Material _material;
        private readonly Transform _root;
        private readonly ObjectPool<MeshRenderer> _pool;

        public MeshPiecePool(string name, Mesh mesh, Material material, Transform root)
        {
            _name = name;
            _mesh = mesh;
            _material = material;
            _root = root;
            _pool = new ObjectPool<MeshRenderer>(Create, actionOnRelease: OnRelease, actionOnDestroy: OnDestroy);
        }

        public MeshRenderer Get(Transform parent, Vector3 localPosition, Quaternion localRotation)
        {
            MeshRenderer piece = _pool.Get();
            Transform pieceTransform = piece.transform;
            pieceTransform.SetParent(parent, false);
            pieceTransform.SetLocalPositionAndRotation(localPosition, localRotation);
            piece.gameObject.SetActive(true);
            return piece;
        }

        public void Release(MeshRenderer piece)
        {
            _pool.Release(piece);
        }

        private MeshRenderer Create()
        {
            var piece = new GameObject(_name);
            piece.SetActive(false);
            piece.transform.SetParent(_root, false);
            piece.AddComponent<MeshFilter>().sharedMesh = _mesh;
            var meshRenderer = piece.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = _material;
            return meshRenderer;
        }

        private void OnRelease(MeshRenderer piece)
        {
            piece.SetPropertyBlock(null);
            piece.gameObject.SetActive(false);
            piece.transform.SetParent(_root, false);
        }

        private static void OnDestroy(MeshRenderer piece)
        {
            if (piece != null)
            {
                Object.Destroy(piece.gameObject);
            }
        }
    }
}
