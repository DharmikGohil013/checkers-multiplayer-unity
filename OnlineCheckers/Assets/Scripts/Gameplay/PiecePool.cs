// Assets/Scripts/Gameplay/PiecePool.cs
using UnityEngine;

namespace Checkers.Gameplay
{
    /// <summary>
    /// Object pool for CheckersPiece GameObjects.
    /// Add this component to GameManagers in the Inspector.
    /// </summary>
    public class PiecePool : PoolBase
    {
        public CheckersPiece GetPiece()
        {
            var go = Get();
            return go != null ? go.GetComponent<CheckersPiece>() : null;
        }

        public void ReturnPiece(CheckersPiece piece)
        {
            if (piece != null) Return(piece.gameObject);
        }
    }
}