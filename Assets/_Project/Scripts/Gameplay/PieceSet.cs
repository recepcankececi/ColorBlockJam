using System.Collections.Generic;
using UnityEngine;

namespace CBJ.Gameplay
{
    /// <summary>
    /// The pooled pieces one view is made of, remembered with their pools so the view can hand them all back.
    /// </summary>
    internal sealed class PieceSet
    {
        private readonly List<MeshPiecePool> _pools = new List<MeshPiecePool>();
        private readonly List<MeshRenderer> _pieces = new List<MeshRenderer>();

        /// <summary>Takes a piece from <paramref name="pool"/>; <paramref name="tint"/> may be null for untinted pieces.</summary>
        public void Add(MeshPiecePool pool, Transform parent, Vector3 localPosition, Quaternion localRotation,
            MaterialPropertyBlock tint)
        {
            Add(pool, parent, localPosition, localRotation, Vector3.one, tint);
        }

        /// <summary>Like the other overload, with the piece scaled by <paramref name="localScale"/>.</summary>
        public void Add(MeshPiecePool pool, Transform parent, Vector3 localPosition, Quaternion localRotation,
            Vector3 localScale, MaterialPropertyBlock tint)
        {
            MeshRenderer piece = pool.Get(parent, localPosition, localRotation, localScale);
            if (tint != null)
            {
                piece.SetPropertyBlock(tint);
            }

            _pools.Add(pool);
            _pieces.Add(piece);
        }

        public void ReleaseAll()
        {
            for (int i = 0; i < _pieces.Count; i++)
            {
                _pools[i].Release(_pieces[i]);
            }

            _pools.Clear();
            _pieces.Clear();
        }
    }
}
