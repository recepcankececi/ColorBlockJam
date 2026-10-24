using UnityEngine;

namespace CBJ.Gameplay
{
    /// <summary>
    /// Draws one <see cref="CBJ.Core.Door"/>: a tinted frame and an arrow per covered cell.
    /// </summary>
    public sealed class DoorView : MonoBehaviour
    {
        private readonly PieceSet _pieces = new PieceSet();

        internal PieceSet Pieces => _pieces;

        internal void ResetForPool()
        {
            _pieces.ReleaseAll();
        }
    }
}
