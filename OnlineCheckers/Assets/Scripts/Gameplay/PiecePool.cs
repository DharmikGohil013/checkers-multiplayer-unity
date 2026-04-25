using UnityEngine;
namespace Checkers.Gameplay
{
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